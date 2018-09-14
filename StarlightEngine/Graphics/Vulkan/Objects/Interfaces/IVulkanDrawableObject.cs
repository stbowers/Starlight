using System;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
	/* Interface for objects which can be drawn with Vulkan
	 * RenderPasses should be a list of all render passes during which this object makes draw calls,
	 * Pipelines should have the same size as RenderPasses, each entry of Pipelines should be the pipeline to be bound for the corrosponding entry in RenderPasses
	 * BindableComponents: For each renderpass the same index in BindableComponents should be a list of components to bind before calling the draw function
	 * Draw: called to draw the object, given the current commandbuffer, pipeline, renderpass, and bound sets draw the geometry for the renderpass at drawIndex
	 */
	public interface IVulkanDrawableObject: IVulkanObject
    {
		RenderPass[] RenderPasses { get; }
		VulkanPipeline[] Pipelines { get; }
        IVulkanBindableComponent[][] BindableComponents { get; }
        void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets, int renderPassIndex);
    }
}
