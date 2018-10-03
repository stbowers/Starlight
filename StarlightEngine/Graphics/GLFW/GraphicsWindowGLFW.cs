using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Graphics.Math;
using glfw3;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.GLFW
{
    public class GraphicsWindowGLFW : IWindowManager
    {
        GLFWwindow m_window;
        int m_width, m_height;

        // transformation matrix from GLFW's window space coordinates into Vulkan's screen space coordinates
        FMat4 m_windowSpaceToScreenSpace;

        // event delegate for GLFW window pointer
        static Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate> m_keyboardEventDelegate = new Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate>();
        static Dictionary<IntPtr, WindowManagerCallbacks.MouseEventDelegate> m_mouseEventDelegate = new Dictionary<IntPtr, WindowManagerCallbacks.MouseEventDelegate>();

        GLFWkeyfun m_keyCallbackDelegate;
        GLFWmousebuttonfun m_mouseButtonDelegate;
        GLFWcursorposfun m_cursorPosDelegate;
        GLFWscrollfun m_scrollDelegate;

        public GraphicsWindowGLFW(int width, int height, string name)
        {
            Glfw.Init();

            m_width = width;
            m_height = height;

            Glfw.WindowHint((int)State.ClientApi, (int)State.NoApi);
            Glfw.WindowHint((int)State.Resizable, (int)State.False);

            m_window = Glfw.CreateWindow(width, height, name, null, null);

            m_windowSpaceToScreenSpace = new FMat4(1.0f);
            m_windowSpaceToScreenSpace *= FMat4.Scale(new FVec3(2.0f / (float)m_width, 2.0f / (float)m_height, 0.0f));
            m_windowSpaceToScreenSpace *= FMat4.Translate(new FVec3(-(float)m_width / 2.0f, -(float)m_height / 2, 0.0f));

            // set up delegates
            m_keyCallbackDelegate = KeyCallback;
            Glfw.SetKeyCallback(m_window, m_keyCallbackDelegate);

            Glfw.SetMouseButtonCallback(m_window, m_mouseButtonDelegate);
            Glfw.SetCursorPosCallback(m_window, m_cursorPosDelegate);
            Glfw.SetScrollCallback(m_window, m_scrollDelegate);
        }

        private void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
        {
            (StarlightEngine.Events.Key _key, KeyAction _action, KeyModifiers _modifiers) = GLFWEventFunctions.GetKeyboardEventDetails(key, scancode, action, mods);
            m_keyboardEventDelegate[window](_key, _action, _modifiers);
        }

        private void MouseButtonCallback(IntPtr window, int button, int action, int mods)
        {
            (MouseButton _button, MouseAction _action, KeyModifiers _modifiers) = GLFWEventFunctions.GetMouseEventDetails(button, action, mods);
            double mouseXPos = 0.0f;
            double mouseYPos = 0.0f;
            Glfw.GetCursorPos(m_window, ref mouseXPos, ref mouseYPos);
            FVec2 mousePosition = (m_windowSpaceToScreenSpace * new FVec4((float)mouseXPos, (float)mouseYPos, 0.0f, 1.0f)).XY();
            m_mouseEventDelegate[window](_button, _action, _modifiers, mousePosition, 0.0f);
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

            return mousePos.XY();
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

        public void SetMouseEventDelegate(WindowManagerCallbacks.MouseEventDelegate mouseEventDelegate)
        {
            m_mouseEventDelegate[m_window.__Instance] = mouseEventDelegate;
        }
    }
}
