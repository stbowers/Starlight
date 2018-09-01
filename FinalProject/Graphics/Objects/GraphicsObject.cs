using System;
namespace FinalProject.Graphics.Objects
{
	public interface GraphicsObject
	{
		// Draw this object using Vulkan API calls
		void DrawVK(Vulkan.VkCommandBuffer primaryBuffer);
	}
}
