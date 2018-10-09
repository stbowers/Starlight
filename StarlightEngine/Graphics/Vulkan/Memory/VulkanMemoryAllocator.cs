using System;
using System.Collections.Generic;
using System.Threading;
using VulkanCore;
using StarlightEngine.Threadding;

namespace StarlightEngine.Graphics.Vulkan.Memory
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

	public class VmaAllocation
	{
		private DeviceMemory memory;
		public long offset;
		public long size;
		public bool isImage; // is this memory used to back an image

		// Thread safe functions for managing the alloction
		public ThreadLock memoryLock;

		public VmaAllocation(){
			memory = null;
		}
		public VmaAllocation(DeviceMemory memory){
			this.memory = memory;
		}

		public IntPtr MapAllocation(){
			return MapAllocation(0, size);
		}

		public IntPtr MapAllocation(long mapOffset, long mapSize){
			// get memory lock
			memoryLock.EnterLock();
			IntPtr mappedPtr = memory.Map(offset + mapOffset, mapSize);
			return mappedPtr;
		}

		public void UnmapAllocation(){
			memory.Unmap();
			memoryLock.ExitLock();
		}

		public void LockMemory(){
			memoryLock.EnterLock();
		}

		public void UnlockMemory(){
			memoryLock.ExitLock();
		}

		public DeviceMemory GetMemory(){
			return memory;
		}
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
			private PhysicalDevice physicalDevice;
			private Device device;
			private List<VmaAllocation> allocations = new List<VmaAllocation>();
			private VmaHeap parent;
			ThreadLock memoryLock = new ThreadLock(EngineConstants.THREADLEVEL_DIRECTAPI);

			public unsafe VmaDeviceMemoryBlock(VmaHeap parent, PhysicalDevice physicalDevice, Device device, uint memoryTypeIndex, ulong blockSize)
			{
				this.physicalDevice = physicalDevice;
				this.device = device;
				this.memoryTypeIndex = memoryTypeIndex;
				this.blockSize = blockSize;
				this.freeSpace = blockSize;
				this.parent = parent;

				MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
				allocInfo.AllocationSize = (long)blockSize;
				allocInfo.MemoryTypeIndex = (int)memoryTypeIndex;

				memoryBlock = device.AllocateMemory(allocInfo);
			}

			public unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment, bool isImage)
			{
				// get allocation lock for read/write access
				memoryLock.EnterLock();

				// Check if we have enough free space
				if (freeSpace < (ulong)allocInfo.AllocationSize)
				{
					allocation = new VmaAllocation();
					memoryLock.ExitLock();
					return Result.ErrorOutOfDeviceMemory;
				}

				// get minimum distance needed between images and buffers
				long bufferImageGranularity = physicalDevice.GetProperties().Limits.BufferImageGranularity;

				// if we have enough free space try to find a big enough contiguous block with the right padding
				// start at begining
				long offset = 0;
				foreach (VmaAllocation alloc in allocations)
				{
					// if this allocation overlaps with the proposed new allocation, move to after this allocation
					if (
						(offset >= alloc.offset && offset <= alloc.offset + alloc.size) || // start is within this allocation
						(offset + allocInfo.AllocationSize > alloc.offset) || // end is after this allocation (allocations overlap)
						(alloc.offset - (offset + allocInfo.AllocationSize) < ((isImage != alloc.isImage) ? bufferImageGranularity : 0)) // allocations are not seperated by enough space for image/buffer granularity
					   )
					{
						offset = alloc.offset + alloc.size;
						// add padding for buffer/image granularity if they're are different types
						if (isImage != alloc.isImage)
						{
							offset += bufferImageGranularity;
						}

						// calculate padding for the given offset
						long padding = alignment - (offset % alignment);

						// add padding to offset
						offset += padding;
					}
				}

				// if the resulting memory location is within the bounds of this block, use it, otherwise report out of memory
				if ((ulong)(offset + allocInfo.AllocationSize) > blockSize)
				{
					allocation = new VmaAllocation();
					memoryLock.ExitLock();
					return Result.ErrorOutOfDeviceMemory;
				}

				// otherwise use the given offset
				allocation = new VmaAllocation(memoryBlock);
				//allocation.memory = memoryBlock;
				allocation.offset = offset;
				allocation.size = allocInfo.AllocationSize;
				allocation.isImage = isImage;
				allocation.memoryLock = memoryLock;

				/* Add allocation */
				// try to insert at correct position
				for (int i = 0; i < allocations.Count; i++)
				{
					// if allocation at i comes before this allocation, continue
					if (allocations[i].offset < allocation.offset)
					{
						continue;
					}

					// otherwise, insert here and break
					allocations.Insert(i, allocation);
					break;
				}
				// if allocation wasn't inserted, add it to end
				if (!allocations.Contains(allocation))
				{
					allocations.Add(allocation);
				}

				freeSpace -= (uint)allocInfo.AllocationSize;

				memoryLock.ExitLock();
				return Result.Success;
			}

			public bool FreeMemory(VmaAllocation allocation)
			{
				// Get read/write access to allocations
				memoryLock.EnterLock();

				// if allocation is in our list of allocations, then free it
				if (allocations.Contains(allocation))
				{
					allocations.Remove(allocation);
					freeSpace += (ulong)(allocation.size);

					// if we don't have any more allocations, delete this block
					if (allocations.Count == 0)
					{
						// currently commented out, since freeing the block can create a race condition, and we don't really need to free the block, as long as the memory can be reallocated
						//parent.FreeBlock(this);
						//memoryBlock.Dispose();
					}

					memoryLock.ExitLock();
					return true;
				}
				else
				{
					memoryLock.ExitLock();
					return false;
				}
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

			public unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment, bool isImage)
			{
				// check any blocks which have already been allocated to see if they have enough space for the new allocation
				foreach (VmaDeviceMemoryBlock block in blocks)
				{
					Result result = block.AllocateMemory(allocInfo, out allocation, alignment, isImage);
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
				VmaDeviceMemoryBlock newBlock = new VmaDeviceMemoryBlock(this, physicalDevice, device, memoryTypeIndex, newBlockSize);
				blocks.Add(newBlock);
				return newBlock.AllocateMemory(allocInfo, out allocation, alignment, isImage);
			}

			public bool FreeMemory(VmaAllocation allocation)
			{
				// go through blocks attempting to free the memory
				foreach (VmaDeviceMemoryBlock block in blocks.ToArray())
				{
					if (block.FreeMemory(allocation))
					{
						return true;
					}
				}

				// If none of our blocks could free it, return false
				return false;
			}

			public void FreeBlock(VmaDeviceMemoryBlock block)
			{
				// free the block if we own it
				if (blocks.Contains(block))
				{
					blocks.Remove(block);
				}
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

			Console.WriteLine("Memory summary for \"{0}\":", physicalDevice.GetProperties().DeviceName);
			Console.WriteLine("  Max memory allocations: {0}", physicalDevice.GetProperties().Limits.MaxMemoryAllocationCount);
			Console.WriteLine("  Available heaps: {0}", memProperties.MemoryHeaps.Length);
			string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB" };
			for (int i = 0; i < memProperties.MemoryHeaps.Length; i++)
			{
				MemoryHeap heap = memProperties.MemoryHeaps[i];
				int pow = (int)System.Math.Floor(System.Math.Log(heap.Size, 1024));
				string sizeSuffix = sizeSuffixes[pow];

				string flags = "(";
				if (heap.Flags.HasFlag(MemoryHeaps.DeviceLocal) && heap.Flags.HasFlag(MemoryHeaps.MultiInstanceKhx))
				{
					flags += "Device Local, MultiInstanceKHX";
				}
				else if (heap.Flags.HasFlag(MemoryHeaps.DeviceLocal))
				{
					flags += "Device Local";
				}
				else if (heap.Flags.HasFlag(MemoryHeaps.MultiInstanceKhx))
				{
					flags += "MultiInstanceKHX";
				}
				else
				{
					flags += "No Flags";
				}
				flags += ")";

				Console.WriteLine("    Heap {0}: {1:0.#} {2}, {3}", i, (float)((heap.Size) / (System.Math.Pow(1024, pow))), sizeSuffix, flags);
			}
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

			Result result = AllocateMemory(allocInfo, out allocation, memoryRequirements.Alignment, true);
			if (result != Result.Success)
			{
				return result;
			}

			allocation.LockMemory();
			image.BindMemory(allocation.GetMemory(), (long)allocation.offset);
			allocation.UnlockMemory();
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

			Result result = AllocateMemory(allocInfo, out allocation, memoryRequirements.Alignment, false);
			if (result != Result.Success)
			{
				return result;
			}

			allocation.LockMemory();
			buffer.BindMemory(allocation.GetMemory(), (long)allocation.offset);
			allocation.UnlockMemory();
			return Result.Success;
		}

		public void FreeAllocation(VmaAllocation allocation)
		{
			foreach (var heap in heaps)
			{
				if (heap.Value.FreeMemory(allocation))
				{
					// free was sucessful, we can return
					return;
				}
				// else that heap didn't allocate the memory, keep trying
			}

			// if no heap was found that allocated the memory throw an exception
			throw new Exception("Could not free memory, heap not found");
		}

		private unsafe Result AllocateMemory(MemoryAllocateInfo allocInfo, out VmaAllocation allocation, long alignment, bool isImage)
		{
            int heapIndex = HeapIndexForMemoryTypeIndex(allocInfo.MemoryTypeIndex);
			if (!heaps.ContainsKey(allocInfo.MemoryTypeIndex))
			{
				heaps[allocInfo.MemoryTypeIndex] = new VmaHeap(memProperties, physicalDevice, device, (uint)allocInfo.MemoryTypeIndex, (uint)heapIndex);
			}
			VmaHeap heap = heaps[allocInfo.MemoryTypeIndex];

			return heap.AllocateMemory(allocInfo, out allocation, alignment, isImage);
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
