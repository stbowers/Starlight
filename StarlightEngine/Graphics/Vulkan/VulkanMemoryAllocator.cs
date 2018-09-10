using System;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
	public enum VmaMemoryUsage
	{
		VMA_MEMORY_USAGE_UNKNOWN,
	}

	public struct VmaAllocationCreateInfo
	{
		public VmaMemoryUsage usage;
		public MemoryProperties requiredFlags;
		public MemoryProperties preferredFlags;
	}

	public struct VmaAllocation
	{
		public DeviceMemory memory;
		public long offset;
		public long size;
	}

	// Helper class for managing Vulkan memory allocations, based roughly on a c library by the same name (https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator/blob/master/src/vk_mem_alloc.h)
	public class VulkanMemoryAllocator
	{
		// class to represent blocks allocated in device memory
		private class VmaDeviceMemoryBlock
		{
			public DeviceMemory memoryBlock;
			public uint memoryTypeIndex;
			public ulong blockSize;
			public ulong freeSpace;
			public ulong newAllocOffset = 0;
			private PhysicalDevice physicalDevice;
			private Device device;
			private List<VmaAllocation> allocations = new List<VmaAllocation>();

			public unsafe VmaDeviceMemoryBlock(PhysicalDevice physicalDevice, Device device, uint memoryTypeIndex, ulong blockSize)
			{
				this.physicalDevice = physicalDevice;
				this.device = device;
				this.memoryTypeIndex = memoryTypeIndex;
				this.blockSize = blockSize;
				this.freeSpace = blockSize;

				MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
				allocInfo.AllocationSize = (long)blockSize;
				allocInfo.MemoryTypeIndex = (int)memoryTypeIndex;

				memoryBlock = device.AllocateMemory(allocInfo);
			}

			public unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment)
			{
				ulong padding = (ulong)(alignment - ((long)newAllocOffset % alignment));
				if (!((ulong)allocInfo.AllocationSize + padding <= freeSpace))
				{
					allocation = new VmaAllocation();
					return Result.ErrorOutOfDeviceMemory;
				}

				allocation = new VmaAllocation();
				allocation.memory = memoryBlock;
				allocation.offset = (long)(newAllocOffset + padding);
				allocation.size = allocInfo.AllocationSize;
				allocations.Add(allocation);

				newAllocOffset += (uint)(allocInfo.AllocationSize + (long)padding);
				freeSpace -= (uint)(allocInfo.AllocationSize + (long)padding);

				return Result.Success;
			}
		}

		// class to represent heaps in device memory (and a list of any blocks allocted on that heap)
		private class VmaHeap
		{
			public uint memoryTypeIndex;
			public uint heapIndex;
			public ulong preferredBlockSize;
			public ulong heapSize;
			public ulong freeSpace;
			private PhysicalDevice physicalDevice;
			private Device device;
			private PhysicalDeviceMemoryProperties memProperties;
			public List<VmaDeviceMemoryBlock> blocks = new List<VmaDeviceMemoryBlock>();

			public VmaHeap(PhysicalDeviceMemoryProperties memProperties, PhysicalDevice physicalDevice, Device device, uint memoryTypeIndex, uint heapIndex)
			{
				this.memoryTypeIndex = memoryTypeIndex;
				this.heapIndex = heapIndex;
				this.memProperties = memProperties;
				this.physicalDevice = physicalDevice;
				this.device = device;
				heapSize = (ulong)memProperties.MemoryHeaps[heapIndex].Size;
				freeSpace = heapSize;

				bool isSmallHeap = heapSize <= SMALL_HEAP_MAX_SIZE;
				preferredBlockSize = isSmallHeap ? (heapSize / 8) : LARGE_HEAP_BLOCK_SIZE;
			}

			public unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment)
			{
				// check any blocks which have already been allocated to see if they have enough space for the new allocation
				foreach (VmaDeviceMemoryBlock block in blocks)
				{
					Result result = block.AllocateMemory(allocInfo, out allocation, alignment);
					if (result == Result.Success)
					{
						return result;
					}
				}

				// if no blocks have enough room, allocate another block
				ulong newBlockSize = System.Math.Max(preferredBlockSize, (ulong)allocInfo.AllocationSize);
				if (newBlockSize > freeSpace)
				{
					allocation = new VmaAllocation();
					return Result.ErrorOutOfDeviceMemory;
				}
				VmaDeviceMemoryBlock newBlock = new VmaDeviceMemoryBlock(physicalDevice, device, memoryTypeIndex, newBlockSize);
				blocks.Add(newBlock);
				return newBlock.AllocateMemory(allocInfo, out allocation, alignment);
			}
		}

		private PhysicalDevice physicalDevice;
		private Device device;
		private PhysicalDeviceMemoryProperties memProperties;
		private Dictionary<int, VmaHeap> heaps = new Dictionary<int, VmaHeap>();

		// How large can a heap be and still be considered 'small' (1024^3 taken from original vma library, 1Gb heap size)
		private const UInt64 SMALL_HEAP_MAX_SIZE = (1024ul * 1024 * 1024);

		// How large should blocks on 'large' heaps be? (256*1024*1024 from original vma library, 256Mb block size)
		private const UInt64 LARGE_HEAP_BLOCK_SIZE = (256ul * 1024 * 1024);

		public unsafe VulkanMemoryAllocator(PhysicalDevice physicalDevice, Device device)
		{
			this.physicalDevice = physicalDevice;
			this.device = device;

			memProperties = physicalDevice.GetMemoryProperties();
		}

		public unsafe Result CreateImage(ImageCreateInfo imageCreateInfo, VmaAllocationCreateInfo imageAllocInfo, out Image image, out VmaAllocation allocation)
		{
			try
			{
				image = device.CreateImage(imageCreateInfo);
			}
			catch(VulkanException e)
			{
				image = null;
				allocation = new VmaAllocation();
				return e.Result;
			}

			MemoryRequirements memoryRequirements = image.GetMemoryRequirements();

			MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
			allocInfo.AllocationSize = memoryRequirements.Size;
			int memoryTypeIndex = findMemoryType(memoryRequirements.MemoryTypeBits, imageAllocInfo.preferredFlags | imageAllocInfo.requiredFlags);
			if (memoryTypeIndex == -1)
			{
				memoryTypeIndex = findMemoryType(memoryRequirements.MemoryTypeBits, imageAllocInfo.requiredFlags);
			}
			if (memoryTypeIndex == -1)
			{
				allocation = new VmaAllocation();
				return Result.ErrorOutOfDeviceMemory;
			}
			allocInfo.MemoryTypeIndex = memoryTypeIndex;

			Result result = AllocateMemory(allocInfo, out allocation, memoryRequirements.Alignment);
			if (result != Result.Success)
			{
				return result;
			}

			image.BindMemory(allocation.memory, (long)allocation.offset);
			return Result.Success;
		}

		public unsafe Result CreateBuffer(BufferCreateInfo bufferCreateInfo, VmaAllocationCreateInfo bufferAllocInfo, out VulkanCore.Buffer buffer, out VmaAllocation allocation)
		{
			try
			{
				buffer = device.CreateBuffer(bufferCreateInfo);
			}
			catch (VulkanException e)
			{
				buffer = null;
				allocation = new VmaAllocation();
				return e.Result;
			}

			MemoryRequirements memoryRequirements = buffer.GetMemoryRequirements();

			MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
			allocInfo.AllocationSize = memoryRequirements.Size;
			int memoryTypeIndex = findMemoryType(memoryRequirements.MemoryTypeBits, bufferAllocInfo.preferredFlags | bufferAllocInfo.requiredFlags);
			if (memoryTypeIndex == -1)
			{
				memoryTypeIndex = findMemoryType(memoryRequirements.MemoryTypeBits, bufferAllocInfo.requiredFlags);
			}
			if (memoryTypeIndex == -1)
			{
				allocation = new VmaAllocation();
				return Result.ErrorOutOfDeviceMemory;
			}
			allocInfo.MemoryTypeIndex = memoryTypeIndex;

			Result result = AllocateMemory(allocInfo, out allocation, memoryRequirements.Alignment);
			if (result != Result.Success)
			{
				return result;
			}

			buffer.BindMemory(allocation.memory, (long)allocation.offset);
			return Result.Success;
		}

		private unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment)
		{
            int heapIndex = HeapIndexForMemoryTypeIndex(allocInfo.MemoryTypeIndex);
			if (!heaps.ContainsKey(allocInfo.MemoryTypeIndex))
			{
				heaps[allocInfo.MemoryTypeIndex] = new VmaHeap(memProperties, physicalDevice, device, (uint)allocInfo.MemoryTypeIndex, (uint)heapIndex);
			}
			VmaHeap heap = heaps[allocInfo.MemoryTypeIndex];

			return heap.AllocateMemory(allocInfo, out allocation, alignment);
		}

		// Returns the memory type index of a heap which supports all types and flags required
		private unsafe int findMemoryType(int typeFilter, MemoryProperties properties)
		{
			for (int i = 0; i < memProperties.MemoryTypes.Length; i++)
			{
				if ((typeFilter & (1 << i)) != 0 &&
				    (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
				{
					return i;
				}
			}

            return -1;
		}

		private int HeapIndexForMemoryTypeIndex(int memoryTypeIndex)
		{
			return memProperties.MemoryTypes[memoryTypeIndex].HeapIndex;
		}
	}
}
