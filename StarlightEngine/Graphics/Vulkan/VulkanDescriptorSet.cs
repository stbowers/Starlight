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
					// make list of descriptor updates
                    List<WriteDescriptorSet> descriptorWrites = new List<WriteDescriptorSet>();
					foreach ((int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType descriptorType) in bindings){
						WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
						descriptorWrite.DstSet = m_sets[index];
						descriptorWrite.DstBinding = binding;
						descriptorWrite.DstArrayElement = 0;
						descriptorWrite.DescriptorCount = 1;
						descriptorWrite.DescriptorType = descriptorType;
						descriptorWrite.BufferInfo = bufferInfo.HasValue? new[] { bufferInfo.Value } : null;
						descriptorWrite.ImageInfo = imageInfo.HasValue? new[] { imageInfo.Value } : null;

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
		/// </summary>
		public void UpdateSetBinding(int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType type){
			// For each set, get the lock and then add this binding to list of updates
			for (int i = 0; i < m_sets.Length; i++){
				UpdateSetBindingForSwapchainIndex(binding, bufferInfo, imageInfo, type, i);
			}
		}

		/// <summary>
		/// Updates this descriptor set with the given buffer/image info for the given binding, for only the descriptor set for the given swapchain index
		/// </summary>
		public void UpdateSetBindingForSwapchainIndex(int binding, DescriptorBufferInfo? bufferInfo, DescriptorImageInfo? imageInfo, DescriptorType type, int swapchainIndex){
			m_setLocks[swapchainIndex].EnterLock();

			m_updateInfo[swapchainIndex].Item1 = true;
			m_updateInfo[swapchainIndex].Item2.Add((binding, bufferInfo, imageInfo, type));

			m_setLocks[swapchainIndex].ExitLock();
		}
    }
}
