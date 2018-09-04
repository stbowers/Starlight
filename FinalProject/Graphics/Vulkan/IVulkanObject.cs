using System;
namespace FinalProject.Graphics.Vulkan
{
	/* Component of IGraphicsObjects which can be rendered by a Vulkan renderer. Gives access to any objects that might
	 * be required to render the object
	 */
	public interface IVulkanObject
	{
		// A reference to the pipeline that should be bound when rendering this object
		VulkanPipeline Pipeline { get; }
	}
}
