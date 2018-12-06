using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Interop.glfw3;

namespace StarlightEngine.Graphics.GLFW
{
    public class GraphicsWindowGLFW : IWindowManager
    {
        IntPtr m_window;
        int m_width, m_height;

        // transformation matrix from GLFW's window space coordinates into Vulkan's screen space coordinates
        FMat4 m_windowSpaceToScreenSpace;

        // event delegate for GLFW window pointer
        static Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate> m_keyboardEventDelegate = new Dictionary<IntPtr, WindowManagerCallbacks.KeyboardEventDelegate>();
        static Dictionary<IntPtr, WindowManagerCallbacks.MouseEventDelegate> m_mouseEventDelegate = new Dictionary<IntPtr, WindowManagerCallbacks.MouseEventDelegate>();

        GLFWKeyFun m_keyCallbackDelegate;
        GLFWMouseButtonFun m_mouseButtonDelegate;
        GLFWCursorPosFun m_cursorPosDelegate;
        GLFWScrollFun m_scrollDelegate;

        public GraphicsWindowGLFW(int width, int height, string name)
        {
            GLFWNativeFunctions.glfwInit();

            m_width = width;
            m_height = height;

            GLFWNativeFunctions.glfwWindowHint(GLFWConstants.GLFW_CLIENT_API, GLFWConstants.GLFW_NO_API);
            GLFWNativeFunctions.glfwWindowHint(GLFWConstants.GLFW_RESIZABLE, GLFWConstants.GLFW_FALSE);

            m_window = GLFWNativeFunctions.glfwCreateWindow(width, height, name, IntPtr.Zero, IntPtr.Zero);

            m_windowSpaceToScreenSpace = new FMat4(1.0f);
            m_windowSpaceToScreenSpace *= FMat4.Scale(new FVec3(2.0f / (float)m_width, 2.0f / (float)m_height, 0.0f));
            m_windowSpaceToScreenSpace *= FMat4.Translate(new FVec3(-(float)m_width / 2.0f, -(float)m_height / 2, 0.0f));

            // set up delegates
            m_keyCallbackDelegate = KeyCallback;
            m_mouseButtonDelegate = MouseButtonCallback;
            m_cursorPosDelegate = CursorPosCallback;
            m_scrollDelegate = ScrollCallback;

            GLFWNativeFunctions.glfwSetKeyCallback(m_window, Marshal.GetFunctionPointerForDelegate(m_keyCallbackDelegate));
            GLFWNativeFunctions.glfwSetMouseButtonCallback(m_window, Marshal.GetFunctionPointerForDelegate(m_mouseButtonDelegate));
            GLFWNativeFunctions.glfwSetCursorPosCallback(m_window, Marshal.GetFunctionPointerForDelegate(m_cursorPosDelegate));
            GLFWNativeFunctions.glfwSetScrollCallback(m_window, Marshal.GetFunctionPointerForDelegate(m_scrollDelegate));

            // don't render mouse
            GLFWNativeFunctions.glfwSetInputMode(m_window, GLFWConstants.GLFW_CURSOR, GLFWConstants.GLFW_CURSOR_HIDDEN);
        }

        private delegate void GLFWKeyFun(IntPtr window, int key, int scancode, int action, int mods);
        private void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
        {
            (StarlightEngine.Events.Key _key, KeyAction _action, KeyModifiers _modifiers) = GLFWEventFunctions.GetKeyboardEventDetails(key, scancode, action, mods);
            m_keyboardEventDelegate[window](_key, _action, _modifiers);
        }

        private delegate void GLFWMouseButtonFun(IntPtr window, int button, int action, int mods);
        private void MouseButtonCallback(IntPtr window, int button, int action, int mods)
        {
            (MouseButton _button, MouseAction _action, KeyModifiers _modifiers) = GLFWEventFunctions.GetMouseEventDetails(button, action, mods);
            double mouseXPos = 0.0f;
            double mouseYPos = 0.0f;
            GLFWNativeFunctions.glfwGetCursorPos(m_window, out mouseXPos, out mouseYPos);
            FVec2 mousePosition = (m_windowSpaceToScreenSpace * new FVec4((float)mouseXPos, (float)mouseYPos, 0.0f, 1.0f)).XY();
            m_mouseEventDelegate[window](_button, _action, _modifiers, mousePosition, 0.0f, 0.0f);
        }

        private delegate void GLFWCursorPosFun(IntPtr window, double xPos, double yPos);
        private void CursorPosCallback(IntPtr window, double xPos, double yPos)
        {
            FVec2 mousePosition = (m_windowSpaceToScreenSpace * new FVec4((float)xPos, (float)yPos, 0.0f, 1.0f)).XY();
            m_mouseEventDelegate[window](MouseButton.None, MouseAction.None, KeyModifiers.None, mousePosition, 0.0f, 0.0f);
        }

        private delegate void GLFWScrollFun(IntPtr window, double xOffset, double yOffset);
        private void ScrollCallback(IntPtr window, double xOffset, double yOffset)
        {
            double mouseXPos = 0.0f;
            double mouseYPos = 0.0f;
            GLFWNativeFunctions.glfwGetCursorPos(m_window, out mouseXPos, out mouseYPos);
            FVec2 mousePosition = (m_windowSpaceToScreenSpace * new FVec4((float)mouseXPos, (float)mouseYPos, 0.0f, 1.0f)).XY();
            m_mouseEventDelegate[window](MouseButton.None, MouseAction.None, KeyModifiers.None, mousePosition, (float)xOffset, (float)yOffset);
        }

        ~GraphicsWindowGLFW()
        {
            GLFWNativeFunctions.glfwDestroyWindow(m_window);
            GLFWNativeFunctions.glfwTerminate();
        }

        string[] IWindowManager.GetVulkanExtensions()
        {
            return GLFWNativeFunctions.glfwGetRequiredInstanceExtensions();
        }

        unsafe VulkanCore.Khr.SurfaceKhr IWindowManager.GetVulkanSurface(VulkanCore.Instance instance)
        {
            IntPtr handle;
            int result = GLFWNativeFunctions.glfwCreateWindowSurface(instance.Handle, m_window, IntPtr.Zero, out handle);

            if (result != (int)VulkanCore.Result.Success)
            {
                throw new SystemException();
            }
            System.Nullable<VulkanCore.AllocationCallbacks> nullAllocator = null;
            return new VulkanCore.Khr.SurfaceKhr(instance, ref nullAllocator, handle.ToInt64());
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

        public IntPtr getWindow()
        {
            return m_window;
        }

        public bool ShouldWindowClose()
        {
            return GLFWNativeFunctions.glfwWindowShouldClose(m_window);
        }

        public void CloseWindow()
        {
            GLFWNativeFunctions.glfwSetWindowShouldClose(m_window, (int)GLFWConstants.GLFW_TRUE);
        }

        public FVec2 GetMousePosition()
        {
            FMat4 translation = new FMat4(1.0f);
            translation *= FMat4.Translate(new FVec3(-1.0f, -1.0f, 0.0f));
            translation *= FMat4.Scale(new FVec3(2.0f, 2.0f, 0.0f));

            double xPos = 0.0f;
            double yPos = 0.0f;
            GLFWNativeFunctions.glfwGetCursorPos(m_window, out xPos, out yPos);
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
                case MouseButton.Left:
                    glfwButtonID = GLFWConstants.GLFW_MOUSE_BUTTON_LEFT;
                    break;
                case MouseButton.Right:
                    glfwButtonID = GLFWConstants.GLFW_MOUSE_BUTTON_RIGHT;
                    break;
                case MouseButton.Middle:
                    glfwButtonID = GLFWConstants.GLFW_MOUSE_BUTTON_MIDDLE;
                    break;
            }

            int result = GLFWNativeFunctions.glfwGetMouseButton(m_window, glfwButtonID);
            if (result == GLFWConstants.GLFW_PRESS)
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
            GLFWNativeFunctions.glfwPollEvents();
        }

        public void SetKeyboardEventDelegate(WindowManagerCallbacks.KeyboardEventDelegate keyboardEventDelegate)
        {
            m_keyboardEventDelegate[m_window] = keyboardEventDelegate;
        }

        public void SetMouseEventDelegate(WindowManagerCallbacks.MouseEventDelegate mouseEventDelegate)
        {
            m_mouseEventDelegate[m_window] = mouseEventDelegate;
        }
    }
}
