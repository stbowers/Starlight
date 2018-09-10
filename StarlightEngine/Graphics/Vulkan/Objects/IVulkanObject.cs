using System;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	/* Component of IGraphicsObjects which can be rendered by a Vulkan renderer. Gives access to any objects that might
	 * be required to render the object
	 */
	public interface IVulkanObject: IGraphicsObject
	{
		// A reference to the pipeline that should be bound when rendering this object
		VulkanPipeline Pipeline { get; }
	}
}
