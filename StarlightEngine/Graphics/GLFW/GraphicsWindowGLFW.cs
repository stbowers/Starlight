using System;
using glfw3;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.GLFW
{
	public class GraphicsWindowGLFW : IWindowManager
	{
		private GLFWwindow m_window;
		int m_width, m_height;

		// event delegate for GLFW window pointer
		static Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate> m_keyboardEventDelegate = new Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate>();

		public GraphicsWindowGLFW(int width, int height, string name)
		{
			Glfw.Init();

			m_width = width;
			m_height = height;

			Glfw.WindowHint((int)State.ClientApi, (int)State.NoApi);
			Glfw.WindowHint((int)State.Resizable, (int)State.False);

			m_window = Glfw.CreateWindow(width, height, name, null, null);
			Glfw.SetKeyCallback(m_window, KeyCallback);
		}

		public void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
		{
			List<KeyModifier> modifiers = Glfw.GetKeyModifiers(mods);

			KeyAction keyAction = KeyAction.Release;
			switch (action)
			{
				case 0:
					keyAction = KeyAction.Release;
					break;
				case 1:
					keyAction = KeyAction.Press;
					break;
				case 2:
					keyAction = KeyAction.Repeat;
					break;
			}

			m_keyboardEventDelegate[window]((Key)key, keyAction, modifiers);
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

		public void SetKeyboardEventDelegate(WindowManagerCallbacks.KeyboardEventDelegate keyboardEventDelegate)
		{
			m_keyboardEventDelegate[m_window.__Instance] = keyboardEventDelegate;
		}
	}
}
