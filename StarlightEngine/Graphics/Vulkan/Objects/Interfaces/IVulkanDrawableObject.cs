using System;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
    public interface IVulkanDrawableObject
    {
        IVulkanBindableComponent[] BindableComponents{ get; }
        void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets);
    }
}
