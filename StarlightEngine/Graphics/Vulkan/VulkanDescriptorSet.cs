using System;
using VulkanCore;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using StarlightEngine.Threadding;

namespace StarlightEngine.Graphics.Vulkan
{
    public class VulkanDescriptorSet
    {
        VulkanAPIManager m_apiManager;
        DescriptorSet[] m_sets;
        ThreadLock[] m_setLocks;
        int m_setIndex;

        Thread[] m_updateThreads;
        // should update?, list for each binding to update in set: (binding, bufferInfo (/null), imageInfo (/null), descriptor type)
        (bool, List<(int, DescriptorBufferInfo?, DescriptorImageInfo?, DescriptorType)>)[] m_updateInfo;

        public VulkanDescriptorSet(VulkanAPIManager apiManager, DescriptorSet[] sets, int setIndex)
        {
            m_apiManager = apiManager;
            m_sets = sets;
            m_setLocks = new ThreadLock[sets.Length];
            m_updateThreads = new Thread[sets.Length];
            m_updateInfo = new(bool, List<(int, DescriptorBufferInfo?, DescriptorImageInfo?, DescriptorType)>)[sets.Length];
            for (int i = 0; i < m_sets.Length; i++)
            {
                m_setLocks[i] = new ThreadLock(EngineConstants.THREADLEVEL_DIRECTMANAGEDCOLLECTION);
                m_updateInfo[i] = (false, new List<(int, DescriptorBufferInfo?, DescriptorImageInfo?, DescriptorType)>());
                m_updateThreads[i] = new Thread(UpdateThread);
                m_updateThreads[i].Name = String.Format("Managed descriptor set 0x{0:X} update thread {1}", this.GetHashCode(), i);
                m_updateThreads[i].Start(i);
            }
            m_setIndex = setIndex;
        }

        public void UpdateThread(object args)
        {
            int index = (int)args;
            Stopwatch timer = new Stopwatch();

            // Continually try to update descriptors
            while (true)
            {
                // Get lock
                m_setLocks[index].EnterLock();
                timer.Restart();

                // Get update info 
                (bool update, List<(int, DescriptorBufferInfo?, DescriptorImageInfo?, DescriptorType)> bindings) = m_updateInfo[index];

                // If we need to update, do update
                if (update)
                {
                    Console.WriteLine("Updating descriptor set for index {0}", index);
                    // make list of descriptor updates
                    List<WriteDescriptorSet> descriptorWrites = new List<WriteDescriptorSet>();
                    foreach ((int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType descriptorType) in bindings)
                    {
                        WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
                        descriptorWrite.DstSet = m_sets[index];
                        descriptorWrite.DstBinding = binding;
                        descriptorWrite.DstArrayElement = 0;
                        descriptorWrite.DescriptorCount = 1;
                        descriptorWrite.DescriptorType = descriptorType;
                        descriptorWrite.BufferInfo = bufferInfo.HasValue ? new[] { bufferInfo.Value } : null;
                        descriptorWrite.ImageInfo = imageInfo.HasValue ? new[] { imageInfo.Value } : null;
                        if (bufferInfo != null){
                            Console.WriteLine("[{0:X}] descriptor buffer: 0x{1:X}", Thread.CurrentThread.ManagedThreadId, bufferInfo.Value.Buffer);
                        }

                        descriptorWrites.Add(descriptorWrite);
                    }

                    // clear the bindings list and set update to false
                    bindings.Clear();
                    m_updateInfo[index].Item1 = false;

                    // update descriptor
                    m_apiManager.WaitForSwapchainBufferIdleAndLock(index);
                    m_sets[index].Parent.UpdateSets(descriptorWrites.ToArray());
                    m_apiManager.ReleaseSwapchainBufferLock(index);
                }

                // release lock, and yield to other threads for at least 10ms
                m_setLocks[index].ExitLock();
                Thread.Sleep(System.Math.Abs((int)(10 - timer.ElapsedMilliseconds)));
            }
        }

        public DescriptorSet GetSet(int swapchainIndex)
        {
            return m_sets[swapchainIndex];
        }

        public int GetSetIndex()
        {
            return m_setIndex;
        }

        /// <summary>
        /// Updates this descriptor set with the given buffer/image info for the given binding
        /// Caller MUST own lock for given swapchain index
        /// </summary>
        public void UpdateSetBinding(int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType type, bool block = true)
        {
            // update each set
            for (int i = 0; i < m_sets.Length; i++)
            {
                UpdateSetBindingForSwapchainIndex(binding, bufferInfo, imageInfo, type, i);
            }
        }

        /// <summary>
        /// Updates this descriptor set with the given buffer/image info for the given binding, for only the descriptor set for the given swapchain index
        /// Calling thread MUST have locked the swapchain for the given index
        /// </summary>
        public void UpdateSetBindingForSwapchainIndex(int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType type, int swapchainIndex)
        {
            if (!m_apiManager.DoesThreadOwnSwapchainLock(swapchainIndex)){
                throw new ApplicationException("Caller of UpdateSetBindingForSwapchainIndex must own the lock for the given swapchain index");
            }


            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
            descriptorWrite.DstSet = m_sets[swapchainIndex];
            descriptorWrite.DstBinding = binding;
            descriptorWrite.DstArrayElement = 0;
            descriptorWrite.DescriptorCount = 1;
            descriptorWrite.DescriptorType = type;
            descriptorWrite.BufferInfo = bufferInfo.HasValue ? new[] { bufferInfo.Value } : null;
            descriptorWrite.ImageInfo = imageInfo.HasValue ? new[] { imageInfo.Value } : null;

            // update descriptor
            m_sets[swapchainIndex].Parent.UpdateSets(new[] { descriptorWrite });
        }
    }
}
