using System;
using Vulkan;
using System.Collections.Generic;

namespace FinalProject.Graphics.VK
{
	public enum VmaMemoryUsage
	{
		VMA_MEMORY_USAGE_UNKNOWN,
	}

	public struct VmaAllocationCreateInfo
	{
		public VmaMemoryUsage usage;
		public Vulkan.VkMemoryPropertyFlags requiredFlags;
		public Vulkan.VkMemoryPropertyFlags preferredFlags;
	}

	public struct VmaAllocation
	{
		public VkDeviceMemory memory;
		public ulong offset;
		public ulong size;
	}

	// Helper class for managing Vulkan memory allocations, based roughly on a c library by the same name (https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator/blob/master/src/vk_mem_alloc.h)
	public class VulkanMemoryAllocator
	{
		// class to represent blocks allocated in device memory
		private class VmaDeviceMemoryBlock
		{
			public VkDeviceMemory memoryBlock;
			public uint memoryTypeIndex;
			public ulong blockSize;
			public ulong freeSpace;
			public ulong newAllocOffset = 0;
			private VkPhysicalDevice physicalDevice;
			private VkDevice device;
			private List<VmaAllocation> allocations;

			public unsafe VmaDeviceMemoryBlock(VkPhysicalDevice physicalDevice, VkDevice device, uint memoryTypeIndex, ulong blockSize)
			{
				this.physicalDevice = physicalDevice;
				this.device = device;
				this.memoryTypeIndex = memoryTypeIndex;
				this.blockSize = blockSize;
				this.freeSpace = blockSize;

				VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo();
				allocInfo.sType = RawConstants.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
				allocInfo.allocationSize = blockSize;
				allocInfo.memoryTypeIndex = memoryTypeIndex;

				fixed (VkDeviceMemory* pMemoryBlock = &memoryBlock)
				{
					VkResult result = VulkanNative.vkAllocateMemory(device, &allocInfo, null, pMemoryBlock);
				}
			}

			public unsafe VkResult AllocateMemory(VkMemoryAllocateInfo* pAllocInfo, VmaAllocation* pAllocation)
			{
				if (!(pAllocInfo->allocationSize <= freeSpace))
				{
					return RawConstants.VK_ERROR_OUT_OF_DEVICE_MEMORY;
				}

				pAllocation->memory = memoryBlock;
				pAllocation->offset = newAllocOffset;

				newAllocOffset += pAllocInfo->allocationSize;
				freeSpace -= pAllocInfo->allocationSize;

				return RawConstants.VK_SUCCESS;
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
			private Vulkan.VkPhysicalDevice physicalDevice;
			private Vulkan.VkDevice device;
			private VkPhysicalDeviceMemoryProperties memProperties;
			public List<VmaDeviceMemoryBlock> blocks = new List<VmaDeviceMemoryBlock>();

			public VmaHeap(VkPhysicalDeviceMemoryProperties memProperties, VkPhysicalDevice physicalDevice, VkDevice device, uint memoryTypeIndex, uint heapIndex)
			{
				this.memoryTypeIndex = memoryTypeIndex;
				this.heapIndex = heapIndex;
				this.memProperties = memProperties;
				this.physicalDevice = physicalDevice;
				this.device = device;
				heapSize = VulkanMemoryAllocator.GetMemoryHeap(memProperties, heapIndex).size;
				freeSpace = heapSize;

				bool isSmallHeap = heapSize <= SMALL_HEAP_MAX_SIZE;
				preferredBlockSize = isSmallHeap ? (heapSize / 8) : LARGE_HEAP_BLOCK_SIZE;
			}

			public unsafe VkResult AllocateMemory(VkMemoryAllocateInfo* pAllocInfo, VmaAllocation* pAllocation)
			{
				// check any blocks which have already been allocated to see if they have enough space for the new allocation
				foreach (VmaDeviceMemoryBlock block in blocks)
				{
					VkResult result = block.AllocateMemory(pAllocInfo, pAllocation);
					if (result == RawConstants.VK_SUCCESS)
					{
						return result;
					}
				}

				// if no blocks have enough room, allocate another block
				ulong newBlockSize = Math.Max(preferredBlockSize, pAllocInfo->allocationSize);
				if (newBlockSize > freeSpace)
				{
					return RawConstants.VK_ERROR_OUT_OF_DEVICE_MEMORY;
				}
				VmaDeviceMemoryBlock newBlock = new VmaDeviceMemoryBlock(physicalDevice, device, memoryTypeIndex, newBlockSize);
				blocks.Add(newBlock);
				return newBlock.AllocateMemory(pAllocInfo, pAllocation);
			}
		}

		private Vulkan.VkPhysicalDevice physicalDevice;
		private Vulkan.VkDevice device;
		private VkPhysicalDeviceMemoryProperties memProperties;
		private Dictionary<uint, VmaHeap> heaps;

		// How large can a heap be and still be considered 'small' (1024^3 taken from original vma library, 1Gb heap size)
		private const UInt64 SMALL_HEAP_MAX_SIZE = (1024ul * 1024 * 1024);

		// How large should blocks on 'large' heaps be? (256*1024*1024 from original vma library, 256Mb block size)
		private const UInt64 LARGE_HEAP_BLOCK_SIZE = (256ul * 1024 * 1024);

		public unsafe VulkanMemoryAllocator(Vulkan.VkPhysicalDevice physicalDevice, Vulkan.VkDevice device)
		{
			this.physicalDevice = physicalDevice;
			this.device = device;

			fixed(VkPhysicalDeviceMemoryProperties* pMemProperties = &memProperties)
			{
				VulkanNative.vkGetPhysicalDeviceMemoryProperties(physicalDevice, pMemProperties);
			}
		}

		public unsafe Vulkan.VkResult CreateImage(Vulkan.VkImageCreateInfo* pImageInfo, VmaAllocationCreateInfo* pAllocInfo, Vulkan.VkImage* pImage, VmaAllocation* pAllocation)
		{
			Vulkan.VkResult result;
			result = Vulkan.VulkanNative.vkCreateImage(device, pImageInfo, null, pImage);
			if (result != Vulkan.RawConstants.VK_SUCCESS) return result;

			Vulkan.VkMemoryRequirements memoryRequirements;
			Vulkan.VulkanNative.vkGetImageMemoryRequirements(device, *pImage, &memoryRequirements);

			Vulkan.VkMemoryAllocateInfo allocInfo = new Vulkan.VkMemoryAllocateInfo();
			allocInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
			allocInfo.allocationSize = memoryRequirements.size;
			int memoryTypeIndex = findMemoryType(memoryRequirements.memoryTypeBits, pAllocInfo->preferredFlags | pAllocInfo->requiredFlags);
			if (memoryTypeIndex == -1)
			{
				memoryTypeIndex = findMemoryType(memoryRequirements.memoryTypeBits, pAllocInfo->requiredFlags);
			}
			if (memoryTypeIndex == -1)
			{
				return Vulkan.RawConstants.VK_ERROR_OUT_OF_DEVICE_MEMORY;
			}
			allocInfo.memoryTypeIndex = (uint)memoryTypeIndex;

			result = AllocateMemory(&allocInfo, pAllocation);
			if (result != RawConstants.VK_SUCCESS)
			{
				return result;
			}

			return VulkanNative.vkBindImageMemory(device, *pImage, pAllocation->memory, pAllocation->offset);
		}

		private unsafe VkResult AllocateMemory(VkMemoryAllocateInfo* pAllocInfo, VmaAllocation* pAllocation)
		{
			// Get heap for the memoryTypeIndex
			uint heapIndex = HeapIndexForMemoryTypeIndex(pAllocInfo->memoryTypeIndex);
			if (!heaps.ContainsKey(heapIndex))
			{
				heaps[heapIndex] = new VmaHeap(memProperties, physicalDevice, device, pAllocInfo->memoryTypeIndex, heapIndex);
			}
			VmaHeap heap = heaps[heapIndex];

			return heap.AllocateMemory(pAllocInfo, pAllocation);
		}

		// Returns the memory type index of a heap which supports all types and flags required
		private unsafe int findMemoryType(uint typeFilter, Vulkan.VkMemoryPropertyFlags properties)
		{
			for (int i = 0; i < memProperties.memoryTypeCount; i++)
			{
				if ((typeFilter & (1 << i)) != 0 &&
				    (GetMemoryType(memProperties, (uint)i).propertyFlags & properties) == properties)
				{
					return i;
				}
			}

			throw new ApplicationException();
		}

		private uint HeapIndexForMemoryTypeIndex(uint memoryTypeIndex)
		{
			return GetMemoryType(memProperties, memoryTypeIndex).heapIndex;
		}

		private static VkMemoryType GetMemoryType(VkPhysicalDeviceMemoryProperties memProperties, uint i)
		{
			switch (i)
			{
				default:
					throw new IndexOutOfRangeException();
				case 0:
					return memProperties.memoryTypes_0;
				case 1:
					return memProperties.memoryTypes_1;
				case 2:
					return memProperties.memoryTypes_2;
				case 3:
					return memProperties.memoryTypes_3;
				case 4:
					return memProperties.memoryTypes_4;
				case 5:
					return memProperties.memoryTypes_5;
				case 6:
					return memProperties.memoryTypes_6;
				case 7:
					return memProperties.memoryTypes_7;
				case 8:
					return memProperties.memoryTypes_8;
				case 9:
					return memProperties.memoryTypes_9;
				case 10:
					return memProperties.memoryTypes_10;
				case 11:
					return memProperties.memoryTypes_11;
				case 12:
					return memProperties.memoryTypes_12;
				case 13:
					return memProperties.memoryTypes_13;
				case 14:
					return memProperties.memoryTypes_14;
				case 15:
					return memProperties.memoryTypes_15;
				case 16:
					return memProperties.memoryTypes_16;
				case 17:
					return memProperties.memoryTypes_17;
				case 18:
					return memProperties.memoryTypes_18;
				case 19:
					return memProperties.memoryTypes_19;
				case 20:
					return memProperties.memoryTypes_20;
				case 21:
					return memProperties.memoryTypes_21;
				case 22:
					return memProperties.memoryTypes_22;
				case 23:
					return memProperties.memoryTypes_23;
				case 24:
					return memProperties.memoryTypes_24;
				case 25:
					return memProperties.memoryTypes_25;
				case 26:
					return memProperties.memoryTypes_26;
				case 27:
					return memProperties.memoryTypes_27;
				case 28:
					return memProperties.memoryTypes_28;
				case 29:
					return memProperties.memoryTypes_29;
				case 30:
					return memProperties.memoryTypes_30;
				case 31:
					return memProperties.memoryTypes_31;
			}
		}

		private static VkMemoryHeap GetMemoryHeap(VkPhysicalDeviceMemoryProperties memProperties, uint i)
		{
			switch (i)
			{
				default:
					throw new IndexOutOfRangeException();
				case 0:
					return memProperties.memoryHeaps_0;
				case 1:
					return memProperties.memoryHeaps_1;
				case 2:
					return memProperties.memoryHeaps_2;
				case 3:
					return memProperties.memoryHeaps_3;
				case 4:
					return memProperties.memoryHeaps_4;
				case 5:
					return memProperties.memoryHeaps_5;
				case 6:
					return memProperties.memoryHeaps_6;
				case 7:
					return memProperties.memoryHeaps_7;
				case 8:
					return memProperties.memoryHeaps_8;
				case 9:
					return memProperties.memoryHeaps_9;
				case 10:
					return memProperties.memoryHeaps_10;
				case 11:
					return memProperties.memoryHeaps_11;
				case 12:
					return memProperties.memoryHeaps_12;
				case 13:
					return memProperties.memoryHeaps_13;
				case 14:
					return memProperties.memoryHeaps_14;
				case 15:
					return memProperties.memoryHeaps_15;
			}
		}
	}
}
