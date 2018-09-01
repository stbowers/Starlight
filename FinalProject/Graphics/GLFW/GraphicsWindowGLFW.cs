using System;
using glfw3;
using System.Runtime.InteropServices;

namespace FinalProject.Graphics.GLFW
{
	public class GraphicsWindowGLFW: Window
	{
		private GLFWwindow window;
		int width, height;

		public GraphicsWindowGLFW(int width, int height, string name)
		{
			Glfw.Init();

			Console.WriteLine("Is Vulkan supported: {0}", Glfw.VulkanSupported());

			this.width = width;
			this.height = height;

			Glfw.WindowHint((int) State.ClientApi, (int) State.NoApi);
			Glfw.WindowHint((int)State.Resizable, (int)State.False);

			window = Glfw.CreateWindow(width, height, name, null, null);
		}

		~GraphicsWindowGLFW()
		{
			Glfw.DestroyWindow(window);
			Glfw.Terminate();
		}

		public GLFWwindow getWindow()
		{
			return window;
		}

		int Window.GetWidth()
		{
			return width;
		}

		int Window.GetHeight()
		{
			return height;
		}

		string[] Window.GetVulkanExtensions()
		{
			return Glfw.GetRequiredInstanceExtensions();
		}

		unsafe Vulkan.VkSurfaceKHR Window.GetVulkanSurface(Vulkan.VkInstance instance)
		{
			ulong handle;
			glfw3.VkResult result = Glfw.CreateWindowSurface(instance.Handle, window.__Instance, (IntPtr)null, (long)&handle);

			if (result != glfw3.VkResult.VK_SUCCESS)
			{
				throw new SystemException();
			}
			return new Vulkan.VkSurfaceKHR(handle);
		}
	}
}
