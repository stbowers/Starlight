using System;
using System.Drawing;
using System.Drawing.Imaging;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Memory;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.Vulkan.Objects.Components
{
    class VulkanTextureComponent : IVulkanBindableComponent
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;
        RenderPass m_renderPass;

        VulkanTexture m_texture;

        VulkanDescriptorSet m_textureSamplerSet;
        int m_binding;

        public VulkanTextureComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, VulkanTexture texture, VulkanDescriptorSet descriptorSet, int binding)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_texture = texture;
            m_renderPass = pipeline.GetRenderPass();
            m_textureSamplerSet = descriptorSet;
            m_binding = binding;

            DescriptorImageInfo imageInfo = new DescriptorImageInfo();
            imageInfo.Sampler = m_texture.Sampler;
            imageInfo.ImageView = m_texture.ImageView;
            imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;

            m_apiManager.WaitForDeviceIdleAndLock();
            m_textureSamplerSet.UpdateSetBinding(binding, null, imageInfo, DescriptorType.CombinedImageSampler);
            m_apiManager.ReleaseDeviceIdleLock();
        }

        public VulkanPipeline Pipeline
        {
            get
            {
                return m_pipeline;
            }
        }

        public RenderPass RenderPass
        {
            get
            {
                return m_renderPass;
            }
        }

        public void BindComponent(CommandBuffer commandBuffer, int swapchainIndex)
        {
            commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, m_pipeline.GetPipelineLayout(), m_textureSamplerSet.GetSetIndex(), new[] { m_textureSamplerSet.GetSet(swapchainIndex) });
        }
    }
}
