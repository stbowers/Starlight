using System;
using System.Threading;
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
            // The data held in the buffer
            public byte[] Data;

            // Offsets and sizes for each of the parent managed buffer's buffers
            public int[] Offsets;
            public int[] Sizes;

            // written if the buffer was changed, so we can keep track of which buffers have written the change and which haven't
            public bool[] HasChanged;
            public int NewOffset;
            public int NewSize;

            // if this section has a descriptor attached, the following members will be filled (otherwise they'll be null/uninitialized)
            public VulkanDescriptorSet DescriptorSet;
            public DescriptorType DescriptorType;
            public int DescriptorSetBinding;

            // access lock
            public ReaderWriterLockSlim SectionLock = new ReaderWriterLockSlim();
        }

        VulkanAPIManager m_apiManager;
        int m_sectionAlignment;
        BufferUsages m_usage;
        MemoryProperties m_requiredFlags;
        MemoryProperties m_preferredFlags;

        List<VulkanManagedBufferSection> m_sections = new List<VulkanManagedBufferSection>();
        int m_usedSpace;

        int m_numBuffers;
        VulkanCore.Buffer[] m_buffers;
        VmaAllocation[] m_bufferAllocations;

        bool m_transient;

        public VulkanManagedBuffer(VulkanAPIManager apiManager, int sectionAlignment, BufferUsages usage, MemoryProperties requiredFlags, MemoryProperties preferredFlags, bool transient = false)
        {
            m_apiManager = apiManager;
            m_sectionAlignment = sectionAlignment;
            m_usage = usage;
            m_requiredFlags = requiredFlags;
            m_preferredFlags = preferredFlags;

            m_usedSpace = 0;

            m_transient = transient;

            if (m_transient)
            {
                // if this buffer is transient, we will need buffers for each swapchain image (so it can be updated quickly)
                m_numBuffers = m_apiManager.GetSwapchainImageCount();
                m_buffers = new VulkanCore.Buffer[m_numBuffers];
                m_bufferAllocations = new VmaAllocation[m_numBuffers];
            }
            else
            {
                // if buffer is not transient (won't be updated often if at all, it can be allocated on just one buffer)
                m_numBuffers = 1;
                m_buffers = new VulkanCore.Buffer[m_numBuffers];
                m_bufferAllocations = new VmaAllocation[m_numBuffers];
            }
        }

        public VulkanCore.Buffer GetBuffer(int swapchainIndex)
        {
            if (m_transient)
            {
                return m_buffers[swapchainIndex];
            }
            else
            {
                return m_buffers[0];
            }
        }

        /* Creates a new section in the buffer, with a given size, filled with data.
		 */
        public VulkanManagedBufferSection AddSection(int size, byte[] data)
        {
            int padding = m_sectionAlignment - (m_usedSpace % m_sectionAlignment);

            VulkanManagedBufferSection newSection = new VulkanManagedBufferSection();
            newSection.Offsets = new int[m_numBuffers];
            newSection.Sizes = new int[m_numBuffers];
            newSection.HasChanged = new bool[m_numBuffers];

            newSection.Data = data;
            for (int i = 0; i < m_numBuffers; i++)
            {
                newSection.Offsets[i] = m_usedSpace + padding;
                newSection.Sizes[i] = size;
                newSection.HasChanged[i] = true;
            }
            newSection.NewOffset = m_usedSpace + padding;
            newSection.NewSize = size;

            m_usedSpace += size + padding;

            m_sections.Add(newSection);
            return newSection;
        }

        /* Creates a new section in the buffer along with a descriptor for it
		 */
        public VulkanManagedBufferSection AddSection(int size, byte[] data, DescriptorType descriptorType, VulkanDescriptorSet set, int setBinding)
        {
            VulkanManagedBufferSection newSection = AddSection(size, data);

            newSection.DescriptorSet = set;
            newSection.DescriptorType = descriptorType;
            newSection.DescriptorSetBinding = setBinding;

            return newSection;
        }

        /* Update a section with new data
		 */
        public void UpdateSection(VulkanManagedBufferSection section, int size, byte[] data)
        {
            // call the override with 0 offset
            UpdateSection(section, size, data, 0);
        }

        /* Update a section's data, and move it by the given offset
		 */
        private void UpdateSection(VulkanManagedBufferSection section, int size, byte[] data, int offset)
        {
            // get writer lock
            section.SectionLock.EnterWriteLock();

            // set to true if the buffer's offset is moved, or if the size changes
            bool memoryFootprintChanged = (size != section.NewSize) || (offset != 0);

            // update this section
            section.Data = data;
            int oldEnd = section.NewOffset + section.NewSize; // last index of this section before moving/resizing
            section.NewSize = size;
            section.NewOffset += offset;
            int padding = (m_sectionAlignment - (section.NewOffset % m_sectionAlignment)) % m_sectionAlignment;
            section.NewOffset += padding;
            int newEnd = section.NewOffset + section.NewSize; // new last index of this section
            m_usedSpace += (newEnd - oldEnd);
            memoryFootprintChanged |= oldEnd != newEnd;
			for (int i = 0; i < m_numBuffers; i++){
				section.HasChanged[i] = true;
			}

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
                    UpdateSection(nextSection, nextSection.NewSize, nextSection.Data, followingOffset);
                }
            }

            // release writer lock
            section.SectionLock.ExitWriteLock();
        }

        /* Write data to all buffers; if block is true waits until all buffers are written
		 */
        public void WriteAllBuffers(bool block)
        {
            // Spawn threads for each buffer
            Thread[] writeBufferThreads = new Thread[m_numBuffers];
            for (int i = 0; i < m_numBuffers; i++)
            {
                writeBufferThreads[i] = new Thread(WriteBufferThreadStart);
                writeBufferThreads[i].Start(i);
            }

            if (block || !m_transient)
            {
                foreach (Thread thread in writeBufferThreads)
                {
                    thread.Join();
                }
            }
        }

        public void WriteBufferThreadStart(Object obj)
        {
            int swapchainIndex = (int)obj;
            WriteBuffer(swapchainIndex);
        }

        /* Writes the sections to a Vulkan buffer (or updates the buffer if changes were made), blocks until buffer is written
		 */
        public void WriteBuffer(int swapchainIndex)
        {
            // get locks
            if (m_transient)
            {
                m_apiManager.WaitForSwapchainBufferIdleAndLock(swapchainIndex);
            }
            else
            {
                m_apiManager.WaitForDeviceIdleAndLock();
            }

            // Get buffer
            if (m_buffers[swapchainIndex] == null)
            {
                // Create new buffer if we don't already have one
                // if this buffer is not host visible, we'll need to make sure the buffer can be a transfer destination
                if (!m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
                {
                    m_apiManager.CreateBuffer(m_usedSpace, m_usage | BufferUsages.TransferDst, m_requiredFlags, m_preferredFlags, out m_buffers[swapchainIndex], out m_bufferAllocations[swapchainIndex]);
                }
            }
            else
            {
                // If we already have a buffer, check if there is enough space for our usage
                if (m_bufferAllocations[swapchainIndex].size <= m_usedSpace)
                {
                    // If there isn't create a new buffer
                    // free old buffer
                    m_apiManager.FreeAllocation(m_bufferAllocations[swapchainIndex]);
                    m_buffers[swapchainIndex].Dispose();

                    // make new buffer
                    // if this buffer is not host visible, we'll need to make sure the buffer can be a transfer destination
                    if (!m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
                    {
                        m_apiManager.CreateBuffer(m_usedSpace, m_usage | BufferUsages.TransferDst, m_requiredFlags, m_preferredFlags, out m_buffers[swapchainIndex], out m_bufferAllocations[swapchainIndex]);
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
                mappedMemory = m_bufferAllocations[swapchainIndex].MapAllocation();
            }
            else
            {
                m_apiManager.CreateBuffer(m_usedSpace, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.None, out stagingBuffer, out stagingBufferAllocation);
                mappedMemory = stagingBufferAllocation.MapAllocation();
            }

            foreach (VulkanManagedBufferSection section in m_sections)
            {
                // Get read lock
                section.SectionLock.EnterReadLock();

                // Write section to mapped memory
                if (section.HasChanged[swapchainIndex])
                {
                    section.Offsets[swapchainIndex] = section.NewOffset;
                    section.Sizes[swapchainIndex] = section.NewSize;
                    Marshal.Copy(section.Data, 0, mappedMemory + section.Offsets[swapchainIndex], section.Data.Length);
                }

                // Get read lock
                section.SectionLock.ExitReadLock();
            }

            // Unmap memory and copy staging buffer if used
            if (m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
            {
                // unmap buffer
                m_bufferAllocations[swapchainIndex].UnmapAllocation();
            }
            else
            {
                // unmap staging buffer
                stagingBufferAllocation.UnmapAllocation();

                // copy staging buffer
                m_apiManager.CopyBufferToBuffer(stagingBuffer, 0, m_buffers[swapchainIndex], 0, m_usedSpace);

                // free staging buffer
                m_apiManager.FreeAllocation(stagingBufferAllocation);
                stagingBuffer.Dispose();
            }

            // update descriptor sets
            foreach (VulkanManagedBufferSection section in m_sections)
            {
                if (section.DescriptorSet != null)
                {
                    DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                    bufferInfo.Buffer = m_buffers[swapchainIndex];
                    bufferInfo.Offset = section.Offsets[swapchainIndex];
                    bufferInfo.Range = section.Sizes[swapchainIndex];

                    if (m_transient)
                    {
                        // if this is a transient buffer, update just the one set
                        section.DescriptorSet.UpdateBuffer(section.DescriptorSetBinding, bufferInfo, section.DescriptorType, swapchainIndex);
                    }
                    else
                    {
                        // Otherwise update all sets
                        for (int index = 0; index < m_apiManager.GetSwapchainImageCount(); index++)
                        {
                            section.DescriptorSet.UpdateBuffer(section.DescriptorSetBinding, bufferInfo, section.DescriptorType, index);
                        }
                    }
                }
            }

            // release buffer lock
            if (m_transient)
            {
                m_apiManager.ReleaseSwapchainBufferLock(swapchainIndex);
            }
            else
            {
                m_apiManager.ReleaseDeviceIdleLock();
            }
        }
    }
}
