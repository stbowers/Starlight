using System;
using glfw3;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Graphics.Math;

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

		public FVec2 GetMousePosition()
		{
			FMat4 translation = new FMat4(1.0f);
			translation *= FMat4.Translate(new FVec3(-1.0f, -1.0f, 0.0f));
			translation *= FMat4.Scale(new FVec3(2.0f, 2.0f, 0.0f));

			double xPos = 0.0f;
			double yPos = 0.0f;
			Glfw.GetCursorPos(m_window, ref xPos, ref yPos);
			xPos /= (float)m_width;
			yPos /= (float)m_height;

			FVec4 mousePos = translation * new FVec4((float)xPos, (float)yPos, 0.0f, 1.0f);

			return new FVec2(mousePos.X, mousePos.Y);
		}

		public bool IsMouseButtonPressed(MouseButton button)
		{
			int glfwButtonID = 0;
			switch (button)
			{
				case MouseButton.Primary:
					glfwButtonID = (int)glfw3.Mouse._Left;
					break;
				case MouseButton.Secondary:
					glfwButtonID = (int)glfw3.Mouse._Right;
					break;
				case MouseButton.Middle:
					glfwButtonID = (int)glfw3.Mouse._Middle;
					break;
			}

			int result = Glfw.GetMouseButton(m_window, glfwButtonID);
			if (result == (int)glfw3.State.Press)
			{
				return true;
			}
			else
			{
				return false;
			}
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
