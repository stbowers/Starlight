using System;
namespace FinalProject.Graphics
{
	/* Interface that any window manager class should implement
	 */
	public interface IWindowManager
	{
		/* Returns a list of extensions a vulkan instance will need to load in order to present to this window's surface
		 */
		string[] GetVulkanExtensions();

		/* Returns the vulkan surface for the window
		 */
		VulkanCore.Khr.SurfaceKhr GetVulkanSurface(VulkanCore.Instance instance);

		/* getters for width and height
		 */
		int Width { get; }
		int Height { get; }
	}
}
