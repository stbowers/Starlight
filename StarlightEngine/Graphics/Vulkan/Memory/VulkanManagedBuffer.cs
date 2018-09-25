using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Memory
{
	/* A wrapper class around the Vulkan buffer, which:
	 * 	- Manages calls to the memory allocator
	 * 	- Manages buffer sections easily
	 * 	- Helps with bining calls and filling buffer memory
	 * 	- Automatically allocates multiple buffers and synchronizes their updates for access when drawing to multiple framebuffers
	 */
	public class VulkanManagedBuffer
	{
		public class VulkanManagedBufferSection
		{
			public int Offset;
			public int Size;
			public byte[] Data;

			// if this section has a descriptor attached, the following members will be filled (otherwise they'll be null/uninitialized)
			public DescriptorSet DescriptorSet;
			public DescriptorType DescriptorType;
			public int DescriptorSetBinding;
			public WriteDescriptorSet? DescriptorUpdate;

			// Has this section changed since the last write?
			public bool HasChanged;
		}

		VulkanAPIManager m_apiManager;
		int m_sectionAlignment;
		BufferUsages m_usage;
		MemoryProperties m_requiredFlags;
		MemoryProperties m_preferredFlags;

		List<VulkanManagedBufferSection> m_sections = new List<VulkanManagedBufferSection>();
		int m_usedSpace;

		VulkanCore.Buffer m_buffer;
		VmaAllocation m_bufferAllocation;

		public VulkanManagedBuffer(VulkanAPIManager apiManager, int sectionAlignment, BufferUsages usage, MemoryProperties requiredFlags, MemoryProperties preferredFlags)
		{
			m_apiManager = apiManager;
			m_sectionAlignment = sectionAlignment;
			m_usage = usage;
			m_requiredFlags = requiredFlags;
			m_preferredFlags = preferredFlags;

			m_usedSpace = 0;
		}

		public VulkanCore.Buffer GetBuffer()
		{
			return m_buffer;
		}

		/* Creates a new section in the buffer, with a given size, filled with data.
		 */
		public VulkanManagedBufferSection AddSection(int size, byte[] data)
		{
			int padding = m_sectionAlignment - (m_usedSpace % m_sectionAlignment);

			VulkanManagedBufferSection newSection = new VulkanManagedBufferSection();
			newSection.Offset = m_usedSpace + padding;
			newSection.Size = size;
			newSection.Data = data;
			newSection.HasChanged = true;

			m_usedSpace += size + padding;

			m_sections.Add(newSection);
			return newSection;
		}

		/* Creates a new section in the buffer along with a descriptor for it
		 */
		public VulkanManagedBufferSection AddSection(int size, byte[] data, DescriptorType descriptorType, DescriptorSet set, int setBinding) {
			VulkanManagedBufferSection newSection = AddSection(size, data);

			newSection.DescriptorSet = set;
			newSection.DescriptorType = descriptorType;
			newSection.DescriptorSetBinding = setBinding;

			DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
			bufferInfo.Buffer = m_buffer;
			bufferInfo.Offset = newSection.Offset;
			bufferInfo.Range = newSection.Size;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = set;
			descriptorWrite.DstBinding = setBinding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = descriptorType;
			descriptorWrite.BufferInfo = new[] { bufferInfo };

			newSection.DescriptorUpdate = descriptorWrite;

			return newSection;
		}

		/* Update a section with new data
		 */
		public void UpdateSection(VulkanManagedBufferSection section, int size, byte[] data)
		{
			// call the override with 0 offset
			UpdateSection(section, size, data, 0);

			// Write the buffer after updating
			WriteBuffer();
		}

		/* Update a section's data, and move it by the given offset
		 */
		private void UpdateSection(VulkanManagedBufferSection section, int size, byte[] data, int offset)
		{
			// set to true if the buffer's offset is moved, or if the size changes
			bool memoryFootprintChanged = (size != section.Size) || (offset != 0);

			// update this section
			int oldEnd = section.Offset + section.Size; // last index of this section before moving/resizing
			section.Size = size;
			section.Data = data;
			section.Offset += offset;
			int padding = (m_sectionAlignment - (section.Offset % m_sectionAlignment)) % m_sectionAlignment;
			section.Offset += padding;
			section.HasChanged = true;
			int newEnd = section.Offset + section.Size; // new last index of this section
			m_usedSpace += (newEnd - oldEnd) - offset;
			memoryFootprintChanged |= oldEnd != newEnd;

			// update any following section if our memory footprint has changed
			if (memoryFootprintChanged)
			{
				// how much should the next section be shifted by
				int followingOffset = newEnd - oldEnd;

				// get next section if it exists
				int thisIndex = m_sections.IndexOf(section);
				if (thisIndex < m_sections.Count - 1)
				{
					VulkanManagedBufferSection nextSection = m_sections[thisIndex + 1];

					// update next section
					UpdateSection(nextSection, nextSection.Size, nextSection.Data, followingOffset);
				}
			}

			if (memoryFootprintChanged && section.DescriptorSet != null)
			{
				// if the buffer has moved in memory, we need to rewrite the descriptor
				DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
				bufferInfo.Buffer = m_buffer;
				bufferInfo.Offset = section.Offset;
				bufferInfo.Range = section.Size;

				WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
				descriptorWrite.DstSet = section.DescriptorSet;
				descriptorWrite.DstBinding = section.DescriptorSetBinding;
				descriptorWrite.DstArrayElement = 0;
				descriptorWrite.DescriptorCount = 1;
				descriptorWrite.DescriptorType = section.DescriptorType;
				descriptorWrite.BufferInfo = new[] { bufferInfo };

				section.DescriptorUpdate = descriptorWrite;
			}
		}

		/* Writes the sections to a Vulkan buffer (or updates the buffer if changes were made)
		 */
		public void WriteBuffer()
		{
			// Get buffer
			if (m_buffer == null)
			{
				// Create new buffer if we don't already have one
				// if this buffer is not host visible, we'll need to make sure the buffer can be a transfer destination
				if (!m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
				{
					m_apiManager.CreateBuffer(m_usedSpace, m_usage | BufferUsages.TransferDst, m_requiredFlags, m_preferredFlags, out m_buffer, out m_bufferAllocation);
				}
			}
			else
			{
				// If we already have a buffer, check if there is enough space for our usage
				if (m_bufferAllocation.size <= m_usedSpace)
				{
					// If there isn't create a new buffer
					// free old buffer
					m_apiManager.FreeAllocation(m_bufferAllocation);
					m_buffer.Dispose();

					// make new buffer
					// if this buffer is not host visible, we'll need to make sure the buffer can be a transfer destination
					if (!m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
					{
						m_apiManager.CreateBuffer(m_usedSpace, m_usage | BufferUsages.TransferDst, m_requiredFlags, m_preferredFlags, out m_buffer, out m_bufferAllocation);
					}
				}
			}

			// We should now have a valid buffer with enough space for all sections
			// If this buffer is host visible, simply map it, otherwise create a staging buffer
			IntPtr mappedMemory;
			VulkanCore.Buffer stagingBuffer = null; // may not be used
			VmaAllocation stagingBufferAllocation = null; // may not be used
			if (m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
			{
				mappedMemory = m_bufferAllocation.memory.Map(m_bufferAllocation.offset, m_bufferAllocation.size);
			}
			else
			{
				m_apiManager.CreateBuffer(m_usedSpace, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.None, out stagingBuffer, out stagingBufferAllocation);
				mappedMemory = stagingBufferAllocation.memory.Map(stagingBufferAllocation.offset, stagingBufferAllocation.size);
			}

			foreach (VulkanManagedBufferSection section in m_sections)
			{
				// Write section to mapped memory
				if (section.HasChanged)
				{
					Marshal.Copy(section.Data, 0, mappedMemory + section.Offset, section.Data.Length);
					section.HasChanged = false;
				}
			}

			// Unmap memory and copy staging buffer if used
			if (m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
			{
				// unmap buffer
				m_bufferAllocation.memory.Unmap();
			}
			else
			{
				// unmap staging buffer
				stagingBufferAllocation.memory.Unmap();

				// copy staging buffer
				m_apiManager.CopyBufferToBuffer(stagingBuffer, 0, m_buffer, 0, m_usedSpace);

				// free staging buffer
				m_apiManager.FreeAllocation(stagingBufferAllocation);
				stagingBuffer.Dispose();
			}

			// update any descriptor sets if needed
			foreach (VulkanManagedBufferSection section in m_sections)
			{
				if (section.DescriptorUpdate != null)
				{
					// update buffer
					section.DescriptorUpdate.Value.BufferInfo[0].Buffer = m_buffer;

					// write set
					section.DescriptorSet.Parent.UpdateSets(new[] { section.DescriptorUpdate.Value });
					section.DescriptorUpdate = null;
				}
			}
		}
	}
}
