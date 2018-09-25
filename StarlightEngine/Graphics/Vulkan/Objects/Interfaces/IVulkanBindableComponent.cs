using System;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
    /* Represents a component which can be bound while drawing a vulkan scene. Must specify a pipeline and render pass which should be bound before this component
     */
    public interface IVulkanBindableComponent
    {
        VulkanPipeline Pipeline { get; }
        RenderPass RenderPass { get; }

        // the current pipeline and render pass are specified, since they may not be the exact pipeline and render pass asked for, but rather a compatible pipeline and render pass
        void BindComponent(CommandBuffer commandBuffer, int swapchainIndex);
    }
}
