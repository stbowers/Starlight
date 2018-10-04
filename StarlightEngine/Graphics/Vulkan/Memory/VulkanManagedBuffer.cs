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

        VulkanAPIManager m_apiManager;
        int m_sectionAlignment;
        BufferUsages m_usage;
        MemoryProperties m_requiredFlags;
        MemoryProperties m_preferredFlags;

        List<ManagedBufferSection> m_sections = new List<ManagedBufferSection>();

        int m_numBuffers;
        VulkanCore.Buffer[] m_buffers;
        VmaAllocation[] m_bufferAllocations;

        bool m_transient;

        ReaderWriterLockSlim m_managedReaderWriterLock = new ReaderWriterLockSlim();

        public VulkanManagedBuffer(VulkanAPIManager apiManager, int sectionAlignment, BufferUsages usage, MemoryProperties requiredFlags, MemoryProperties preferredFlags, bool transient = false)
        {
            m_apiManager = apiManager;
            m_sectionAlignment = sectionAlignment;
            m_usage = usage;
            m_requiredFlags = requiredFlags;
            m_preferredFlags = preferredFlags;

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

        /// <summary>
        /// Gets the ammount of space needed to store all buffer sections
        /// MUST HAVE READ LOCK FOR MANAGED BUFFER
        /// <summary>
        private int GetRequiredSpace()
        {
            int requiredSpace = 0;

            foreach (ManagedBufferSection section in m_sections)
            {
                int spaceNeededForSection = section.Offset + section.Size;
                if (spaceNeededForSection > requiredSpace)
                {
                    requiredSpace = spaceNeededForSection;
                }
            }

            return requiredSpace;
        }

        /// <summary>
        /// Creates a managed buffer section with the given data, and optionally a descriptor set to update when the buffer is changed
        /// </summary>
        public ManagedBufferSection AddSection(
            byte[] data,
            DescriptorType? descriptorType = null, VulkanDescriptorSet set = null, int setBinding = -1
            )
        {
            m_managedReaderWriterLock.EnterUpgradeableReadLock();

            int offset = GetRequiredSpace();
            // get padding required to append this buffer to end
            int padding = Functions.Mod((m_sectionAlignment - GetRequiredSpace()), m_sectionAlignment);
            offset += padding;

            m_managedReaderWriterLock.EnterWriteLock();
            ManagedBufferSection newSection = new ManagedBufferSection(data, offset, padding, m_numBuffers, descriptorType, set, setBinding);

            m_sections.Add(newSection);

            m_managedReaderWriterLock.ExitWriteLock();
            m_managedReaderWriterLock.ExitUpgradeableReadLock();

            return newSection;
        }

        /* Update a section with new data
		 */
        public void UpdateSection(ManagedBufferSection section, byte[] data)
        {
            // call the override with 0 offset
            UpdateSection(section, data, 0);
        }

        /* Update a section's data, and move it by the given offset
		 */
        private void UpdateSection(ManagedBufferSection section, byte[] data, int offset)
        {
            m_managedReaderWriterLock.EnterUpgradeableReadLock();
            // set to true if the buffer's offset is moved, or if the size changes
            bool memoryFootprintChanged = (data.Length != section.Size) || (offset != 0);

            // get start and end values for section before changing
            int oldEnd = section.Offset + section.Size;
            int oldStart = section.Offset - section.Padding;

            m_managedReaderWriterLock.EnterWriteLock();
            // update this section's data
            section.Data = data;

            // calculate new offset
            int newStart = oldStart + offset;
            int padding = Functions.Mod((m_sectionAlignment - newStart), m_sectionAlignment);
            int newOffset = newStart + padding;
            section.Offset = newOffset;

            // calculate new end
            int newEnd = newOffset + section.Size; // new last index of this section

            // determine if the memory footprint has changed
            memoryFootprintChanged |= oldEnd != newEnd;

            m_managedReaderWriterLock.ExitWriteLock();
            m_managedReaderWriterLock.ExitUpgradeableReadLock();

            // update following sections if our memory footprint has changed
            if (memoryFootprintChanged)
            {
                // how much should the next section be shifted by
                int followingOffset = newEnd - oldEnd;

                // get next section if it exists
                int thisIndex = m_sections.IndexOf(section);
                if (thisIndex < m_sections.Count - 1)
                {
                    ManagedBufferSection nextSection = m_sections[thisIndex + 1];

                    // update next section
                    UpdateSection(nextSection, nextSection.Data, followingOffset);
                }
            }
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
                writeBufferThreads[i].Name = "Write Buffer " + i;
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
            m_managedReaderWriterLock.EnterWriteLock();

            // Lock the swapchain
            if (m_transient)
            {
                // only lock the one index if this is a transient buffer
                m_apiManager.WaitForSwapchainBufferIdleAndLock(swapchainIndex);
            }
            else
            {
                // lock whole swapchain if not a transient buffer
                m_apiManager.WaitForDeviceIdleAndLock();
            }

            // get space required for buffer
            int requiredSpace = GetRequiredSpace();

            // Get buffer
            VulkanCore.Buffer mainBuffer;
            VmaAllocation mainBufferAllocation;
            // if this buffer is not host visible, we'll need to make sure the buffer can be a transfer destination
            BufferUsages usage = m_usage | (m_requiredFlags.HasFlag(MemoryProperties.HostVisible) ? 0 : BufferUsages.TransferDst);
            if (m_buffers[swapchainIndex] == null)
            {
                // Create new buffer if we don't already have one
                m_apiManager.CreateBuffer(requiredSpace, usage, m_requiredFlags, m_preferredFlags, out m_buffers[swapchainIndex], out m_bufferAllocations[swapchainIndex]);

                mainBuffer = m_buffers[swapchainIndex];
                mainBufferAllocation = m_bufferAllocations[swapchainIndex];
            }
            else
            {
                // If we already have a buffer, check if there is enough space for our usage
                if (m_bufferAllocations[swapchainIndex].size >= requiredSpace)
                {
                    // If there is use it
                    mainBuffer = m_buffers[swapchainIndex];
                    mainBufferAllocation = m_bufferAllocations[swapchainIndex];
                }
                else
                {
                    // If there isn't create a new buffer
                    // free old buffer
                    m_apiManager.FreeAllocation(m_bufferAllocations[swapchainIndex]);
                    m_buffers[swapchainIndex].Dispose();

                    // make new buffer
                    m_apiManager.CreateBuffer(requiredSpace, usage, m_requiredFlags, m_preferredFlags, out m_buffers[swapchainIndex], out m_bufferAllocations[swapchainIndex]);
                    mainBuffer = m_buffers[swapchainIndex];
                    mainBufferAllocation = m_bufferAllocations[swapchainIndex];
                }
            }

            // Get the staging buffer (the buffer we actually write to - is different from main buffer if main buffer is not host visible)
            VulkanCore.Buffer stagingBuffer;
            VmaAllocation stagingBufferAllocation;
            if (m_requiredFlags.HasFlag(MemoryProperties.HostVisible))
            {
                stagingBuffer = mainBuffer;
                stagingBufferAllocation = mainBufferAllocation;
            }
            else
            {
                m_apiManager.CreateBuffer(requiredSpace, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.None, out stagingBuffer, out stagingBufferAllocation);
            }

            // write sections
            foreach (ManagedBufferSection section in m_sections)
            {
                section.GetRawBufferSection(swapchainIndex).WriteSection(mainBuffer, stagingBuffer, stagingBufferAllocation, swapchainIndex, m_apiManager.GetSwapchainImageCount(), m_transient, false);
            }

            // Copy staging buffer if it is different from the main buffer
            if (stagingBuffer != mainBuffer)
            {
                // copy staging buffer
                m_apiManager.CopyBufferToBuffer(stagingBuffer, 0, m_buffers[swapchainIndex], 0, requiredSpace);

                // free staging buffer
                m_apiManager.FreeAllocation(stagingBufferAllocation);
                stagingBuffer.Dispose();
            }

            // release swapchain lock
            if (m_transient)
            {
                m_apiManager.ReleaseSwapchainBufferLock(swapchainIndex);
            }
            else
            {
                m_apiManager.ReleaseDeviceIdleLock();
            }

            m_managedReaderWriterLock.ExitWriteLock();
        }

        #region Buffer Section Class Definition
        /// <summary>
        /// Contains the data for a section in a vulkan buffer
        /// </summary>
        public class BufferSection
        {
            #region Private Members
            // Descriptor set info
            DescriptorType? m_descriptorType;
            VulkanDescriptorSet m_descriptorSet;
            int m_setBinding;

            // target values for data, offset
            byte[] m_data;
            int m_offset;
            int m_size;

            // cached values for offset and size, and which buffer they refer to
            VulkanCore.Buffer m_cachedBuffer;
            int m_cachedOffset;
            int m_cachedSize;

            // set to true if the above cache doesn't match the target values (and so buffer should be rewritten)
            bool m_cacheInvalidated;

            ReaderWriterLockSlim m_readerWriterLock = new ReaderWriterLockSlim();
            #endregion

            #region Constructor
            public BufferSection(DescriptorType? descriptorType = null, VulkanDescriptorSet descriptorSet = null, int setBinding = -1)
            {
                m_descriptorType = descriptorType;
                m_descriptorSet = descriptorSet;
                m_setBinding = setBinding;
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Writes this section to a vulkan buffer
            /// </summary>
            /// <param name="targetBuffer">The final target buffer which this section should refer to</param>
            /// <param name="stagingBuffer">The buffer to which is actually written (may or may not be the same as targetBuffer)</param>
            /// <param name="stagingBufferAllocation">Memory allocation for stagingBuffer</param>
            /// <param name="swapchainIndex">The swapchainIndex for the buffer being written</param>
            /// <param name="numSwapchainImages">The number of images in the swapchain</param>
            /// <param name="transient">Is the managed buffer transient (has a buffer for each swapchainIndex)</param>
            /// <param name="forceWrite">Force this section to write to the buffer, instead of trying to skip unrequired writes</param>
            public void WriteSection(VulkanCore.Buffer targetBuffer, VulkanCore.Buffer stagingBuffer, VmaAllocation stagingBufferAllocation, int swapchainIndex, int numSwapchainImages, bool transient, bool forceWrite)
            {
                m_readerWriterLock.EnterUpgradeableReadLock();
                // Write to the buffer if any of the following conditions are met:
                // 1) the target buffer is not the same as the cached buffer - the buffer has changed and we need to rewrite our data to it
                // 2) the target buffer is not the same as the staging buffer - the staging buffer will replace the data in targetBuffer, so we need to write to it regardless
                // 3) the cache has been invalidated (i.e. data is upated, and we need to rewrite buffer)
                // 4) forceWrite is true
                if (targetBuffer != m_cachedBuffer || targetBuffer != stagingBuffer || m_cacheInvalidated || forceWrite)
                {
                    // copy m_data to staging buffer
                    IntPtr mappedMemory = stagingBufferAllocation.MapAllocation(m_offset, m_size);
                    Marshal.Copy(m_data, 0, mappedMemory, m_size);
                    stagingBufferAllocation.UnmapAllocation();

                    // Reset cache values
                    m_readerWriterLock.EnterWriteLock();
                    m_cachedBuffer = targetBuffer;
                    m_cachedOffset = m_offset;
                    m_cachedSize = m_size;
                    m_cacheInvalidated = false;

                    // Write descriptor set
                    if (m_descriptorSet != null)
                    {
                        DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
                        bufferInfo.Buffer = m_cachedBuffer;
                        bufferInfo.Offset = m_cachedOffset;
                        bufferInfo.Range = m_cachedSize;

                        if (transient)
                        {
                            // if this is a transient buffer, update just the one set
                            m_descriptorSet.UpdateBuffer(m_setBinding, bufferInfo, m_descriptorType.Value, swapchainIndex);
                        }
                        else
                        {
                            // Otherwise update all sets
                            for (int index = 0; index < numSwapchainImages; index++)
                            {
                                m_descriptorSet.UpdateBuffer(m_setBinding, bufferInfo, m_descriptorType.Value, false);
                            }
                        }
                    }

                    m_readerWriterLock.ExitWriteLock();
                }

                m_readerWriterLock.ExitUpgradeableReadLock();
            }
            #endregion

            #region Properties
            /// <summary>
            /// The data contained in this buffer section
            /// </summary>
            public byte[] Data
            {
                set
                {
                    m_readerWriterLock.EnterWriteLock();
                    m_data = value;
                    m_size = value.Length;
                    m_cacheInvalidated = true;
                    m_readerWriterLock.ExitWriteLock();
                }

                get
                {
                    m_readerWriterLock.EnterReadLock();
                    byte[] data = m_data;
                    m_readerWriterLock.ExitReadLock();
                    return data;
                }
            }

            /// <summary>
            /// The offset into this buffer
            /// Note: this will always corrospond to the proper offset into the buffer returned by Buffer,
            /// and thus changes to Offset may not be seen until this section is written
            /// </summary>
            public int Offset
            {
                set
                {
                    m_readerWriterLock.EnterWriteLock();
                    m_offset = value;
                    m_cacheInvalidated = true;
                    m_readerWriterLock.ExitWriteLock();
                }

                get
                {
                    m_readerWriterLock.EnterReadLock();
                    int offset = m_cachedOffset;
                    m_readerWriterLock.ExitReadLock();
                    return offset;
                }
            }

            /// <summary>
            /// The size of this section in the buffer returned by Buffer
            /// </summary>
            public int Size
            {
                get
                {
                    m_readerWriterLock.EnterReadLock();
                    int size = m_cachedSize;
                    m_readerWriterLock.ExitReadLock();
                    return size;
                }
            }

            /// <summary>
            /// The Vulkan buffer this section is written to
            /// </summary>
            public VulkanCore.Buffer Buffer
            {
                get
                {
                    m_readerWriterLock.EnterReadLock();
                    VulkanCore.Buffer buffer = m_cachedBuffer;
                    m_readerWriterLock.ExitReadLock();
                    return buffer;
                }
            }
            #endregion
        }

        /// <summary>
        /// A section in the managed buffer
        /// </summary>
        public class ManagedBufferSection
        {
            #region Private members
            BufferSection[] m_sections;

            byte[] m_data;
            int m_offset;
            int m_padding;

            ReaderWriterLockSlim m_bufferReaderWriterLock = new ReaderWriterLockSlim();
            #endregion

            #region Constructor
            /// <summary>
            /// Creates a managed buffer section with the given data, offset and padding
            /// </summary>
            /// <param name="data">Data to store in this section<param/>
            /// <param name="offset">Offset to the start of aligned memory for this section<param/>
            /// <param name="padding">The ammount of padding space before offset<param/>
            /// <param name="numBuffers">The number of raw vulkan buffers to manage<param/>
            public ManagedBufferSection(
                byte[] data, int offset, int padding, int numBuffers,
                DescriptorType? descriptorType = null, VulkanDescriptorSet set = null, int setBinding = -1
                )
            {
                m_data = data;
                m_offset = offset;
                m_padding = padding;

                // Create buffer sections
                m_sections = new BufferSection[numBuffers];
                for (int i = 0; i < numBuffers; i++)
                {
                    m_sections[i] = new BufferSection(descriptorType, set, setBinding);
                    m_sections[i].Data = data;
                    m_sections[i].Offset = offset;
                }
            }
            #endregion

            #region Public methods
            /// <summary>
            /// Get the details required to bind this buffer section during drawing
            /// </summary>
            /// <returns> Returns:
            /// (buffer, offset)
            /// </returns>
            public (VulkanCore.Buffer, int) GetBindingDetails(int swapchainIndex)
            {
                BufferSection section = m_sections[swapchainIndex % m_sections.Length];
                return (section.Buffer, section.Offset);
            }

            public BufferSection GetRawBufferSection(int swapchainIndex)
            {
                return m_sections[swapchainIndex % m_sections.Length];
            }
            #endregion

            #region Public properties
            public int Size
            {
                get
                {
                    m_bufferReaderWriterLock.EnterReadLock();
                    int size = m_data.Length;
                    m_bufferReaderWriterLock.ExitReadLock();
                    return size;
                }
            }

            public int Offset
            {
                set
                {
                    m_bufferReaderWriterLock.EnterWriteLock();
                    m_offset = value;
                    foreach (BufferSection section in m_sections)
                    {
                        section.Offset = value;
                    }
                    m_bufferReaderWriterLock.ExitWriteLock();
                }

                get
                {
                    m_bufferReaderWriterLock.EnterReadLock();
                    int offset = m_offset;
                    m_bufferReaderWriterLock.ExitReadLock();
                    return offset;
                }
            }

            public int Padding
            {
                set
                {
                    m_bufferReaderWriterLock.EnterWriteLock();
                    m_padding = value;
                    m_bufferReaderWriterLock.ExitWriteLock();
                }

                get
                {
                    m_bufferReaderWriterLock.EnterReadLock();
                    int padding = m_padding;
                    m_bufferReaderWriterLock.ExitReadLock();
                    return padding;
                }
            }

            public byte[] Data
            {
                set
                {
                    m_bufferReaderWriterLock.EnterWriteLock();
                    m_data = value;
                    foreach (BufferSection section in m_sections)
                    {
                        section.Data = value;
                    }
                    m_bufferReaderWriterLock.ExitWriteLock();
                }

                get
                {
                    m_bufferReaderWriterLock.EnterReadLock();
                    byte[] data = m_data;
                    m_bufferReaderWriterLock.ExitReadLock();
                    return data;
                }
            }
            #endregion
        }
        #endregion
    }
}
