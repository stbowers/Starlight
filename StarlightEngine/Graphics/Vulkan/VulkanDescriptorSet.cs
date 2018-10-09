using System;
using VulkanCore;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using StarlightEngine.Threadding;

namespace StarlightEngine.Graphics.Vulkan
{
    /// <summary>
    /// very loosely managed wrapper around VulkanCore descriptorsets, to keep
    /// multiple sets for each swapchain index organized. Update methods must
    /// be externally synchronized by getting the swapchain lock for the given index
    /// </summary>
    public class VulkanDescriptorSet
    {
        VulkanAPIManager m_apiManager;
        DescriptorSet[] m_sets;
        int m_setIndex;

        public VulkanDescriptorSet(VulkanAPIManager apiManager, DescriptorSet[] sets, int setIndex)
        {
            m_apiManager = apiManager;
            m_sets = sets;
            m_setIndex = setIndex;
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
