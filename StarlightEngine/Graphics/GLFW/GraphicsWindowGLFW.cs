using System;
using glfw3;
using System.Runtime.InteropServices;

namespace StarlightEngine.Graphics.GLFW
{
	public class GraphicsWindowGLFW : IWindowManager
	{
		private GLFWwindow m_window;
		int m_width, m_height;

		public GraphicsWindowGLFW(int width, int height, string name)
		{
			Glfw.Init();

			m_width = width;
			m_height = height;

			Glfw.WindowHint((int)State.ClientApi, (int)State.NoApi);
			Glfw.WindowHint((int)State.Resizable, (int)State.False);

			m_window = Glfw.CreateWindow(width, height, name, null, null);
		}

		~GraphicsWindowGLFW()
		{
			Glfw.DestroyWindow(m_window);
			Glfw.Terminate();
		}



		string[] IWindowManager.GetVulkanExtensions()
		{
			return Glfw.GetRequiredInstanceExtensions();
		}

		unsafe VulkanCore.Khr.SurfaceKhr IWindowManager.GetVulkanSurface(VulkanCore.Instance instance)
		{
			long handle;
			glfw3.VkResult result = Glfw.CreateWindowSurface(instance.Handle, m_window.__Instance, (IntPtr)null, (long)&handle);

			if (result != glfw3.VkResult.VK_SUCCESS)
			{
				throw new SystemException();
			}
			System.Nullable<VulkanCore.AllocationCallbacks> nullAllocator = null;
			return new VulkanCore.Khr.SurfaceKhr(instance, ref nullAllocator, handle);
		}

		public int Width
		{
			get
			{
				return m_width;
			}
		}

		public int Height
		{
			get
			{
				return m_height;
			}
		}

		public GLFWwindow getWindow()
		{
			return m_window;
		}

        public bool ShouldWindowClose()
        {
            return Glfw.WindowShouldClose(m_window) != 0;
        }

        public void PollEvents()
        {
            Glfw.PollEvents();
        }
    }
}
