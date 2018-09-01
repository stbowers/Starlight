using System;
namespace FinalProject.Graphics
{
	public interface Window
	{
		string[] GetVulkanExtensions();
		Vulkan.VkSurfaceKHR GetVulkanSurface(Vulkan.VkInstance instance);

		int GetWidth();
		int GetHeight();
	}
}
