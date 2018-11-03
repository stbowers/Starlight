using System;
using System.Runtime.InteropServices;

namespace StarlightEngine.Interop.glfw3
{
    /// <summary>
    /// Direct function calls into the native glfw3 library
    /// </summary>
    public static class GLFWNativeFunctions
    {
        static NativeLibrary m_nativelib;

        #region Member delegates
        static PFglfwInit m_glfwInit;
        static PFglfwTerminate m_glfwTerminate;

        static PFglfwWindowHint m_glfwWindowHint;
        static PFglfwCreateWindow m_glfwCreateWindow;
        static PFglfwDestroyWindow m_glfwDestroyWindow;

        static PFglfwSetKeyCallback m_glfwSetKeyCallback;
        static PFglfwSetMouseButtonCallback m_glfwSetMouseButtonCallback;
        static PFglfwSetCursorPosCallback m_glfwSetCursorPosCallback;
        static PFglfwSetScrollCallback m_glfwSetScrollCallback;

        static PFglfwGetCursorPos m_glfwGetCursorPos;
        static PFglfwGetMouseButton m_glfwGetMouseButton;

        static PFglfwPollEvents m_glfwPollEvents;

        static PFglfwSetWindowShouldClose m_glfwSetWindowShouldClose;
        static PFglfwWindowShouldClose m_glfwWindowShouldClose;

        static PFglfwCreateWindowSurface m_glfwCreateWindowSurface;
        static PFglfwGetRequiredInstanceExtensions m_glfwGetRequiredInstanceExtensions;
        #endregion

        static GLFWNativeFunctions()
        {
            m_nativelib = new NativeLibrary("glfw3");

            m_glfwInit = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwInit>("glfwInit");
            m_glfwTerminate = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwTerminate>("glfwTerminate");

            m_glfwWindowHint = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwWindowHint>("glfwWindowHint");
            m_glfwCreateWindow = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwCreateWindow>("glfwCreateWindow");
            m_glfwDestroyWindow = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwDestroyWindow>("glfwDestroyWindow");

            m_glfwSetKeyCallback = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwSetKeyCallback>("glfwSetKeyCallback");
            m_glfwSetMouseButtonCallback = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwSetMouseButtonCallback>("glfwSetMouseButtonCallback");
            m_glfwSetCursorPosCallback = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwSetCursorPosCallback>("glfwSetCursorPosCallback");
            m_glfwSetScrollCallback = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwSetScrollCallback>("glfwSetScrollCallback");

            m_glfwGetCursorPos = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwGetCursorPos>("glfwGetCursorPos");
            m_glfwGetMouseButton = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwGetMouseButton>("glfwGetMouseButton");

            m_glfwPollEvents = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwPollEvents>("glfwPollEvents");

            m_glfwSetWindowShouldClose = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwSetWindowShouldClose>("glfwSetWindowShouldClose");
            m_glfwWindowShouldClose = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwWindowShouldClose>("glfwWindowShouldClose");

            m_glfwCreateWindowSurface = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwCreateWindowSurface>("glfwCreateWindowSurface");
            m_glfwGetRequiredInstanceExtensions = m_nativelib.GetDelegateForUnmanagedFunction<PFglfwGetRequiredInstanceExtensions>("glfwGetRequiredInstanceExtensions");
        }

        public static bool glfwInit()
        {
            return m_glfwInit();
        }

        public static bool glfwTerminate()
        {
            return m_glfwTerminate();
        }

        public static void glfwWindowHint(int hint, int value)
        {
            m_glfwWindowHint(hint, value);
        }

        public static IntPtr glfwCreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share)
        {
            return m_glfwCreateWindow(width, height, title, monitor, share);
        }

        public static void glfwDestroyWindow(IntPtr window)
        {
            m_glfwDestroyWindow(window);
        }

        public static IntPtr glfwSetKeyCallback(IntPtr window, IntPtr cbfun)
        {
            return m_glfwSetKeyCallback(window, cbfun);
        }

        public static IntPtr glfwSetMouseButtonCallback(IntPtr window, IntPtr cbfun)
        {
            return m_glfwSetMouseButtonCallback(window, cbfun);
        }

        public static IntPtr glfwSetCursorPosCallback(IntPtr window, IntPtr cbfun)
        {
            return m_glfwSetCursorPosCallback(window, cbfun);
        }

        public static IntPtr glfwSetScrollCallback(IntPtr window, IntPtr cbfun)
        {
            return m_glfwSetScrollCallback(window, cbfun);
        }

        public static IntPtr glfwGetCursorPos(IntPtr window, out double xpos, out double ypos)
        {
            return m_glfwGetCursorPos(window, out xpos, out ypos);
        }

        public static int glfwGetMouseButton(IntPtr window, int button)
        {
            return m_glfwGetMouseButton(window, button);
        }

        public static void glfwPollEvents()
        {
            m_glfwPollEvents();
        }

        public static void glfwSetWindowShouldClose(IntPtr window, int value)
        {
            m_glfwSetWindowShouldClose(window, value);
        }

        public static bool glfwWindowShouldClose(IntPtr window)
        {
            return m_glfwWindowShouldClose(window);
        }

        // Vulkan functions
        public static int glfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr allocator, out IntPtr surface)
        {
            return m_glfwCreateWindowSurface(instance, window, allocator, out surface);
        }

        public static string[] glfwGetRequiredInstanceExtensions()
        {
            string[] output;

            unsafe
            {
                UInt32 nExtensions;
                char** pExtensions = m_glfwGetRequiredInstanceExtensions(out nExtensions);

                output = new string[nExtensions];
                for (int i = 0; i < nExtensions; i++)
                {
                    output[i] = Marshal.PtrToStringAnsi((IntPtr)pExtensions[i]);
                }
            }

            return output;
        }

        #region Delegate Definitions
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool PFglfwInit();
        private delegate bool PFglfwTerminate();

        private delegate void PFglfwWindowHint(int hint, int value);
        private delegate IntPtr PFglfwCreateWindow(int width, int height, [MarshalAs(UnmanagedType.LPStr)] string title, IntPtr monitor, IntPtr share);
        private delegate void PFglfwDestroyWindow(IntPtr window);

        private delegate IntPtr PFglfwSetKeyCallback(IntPtr window, IntPtr cbfun);
        private delegate IntPtr PFglfwSetMouseButtonCallback(IntPtr window, IntPtr cbfun);
        private delegate IntPtr PFglfwSetCursorPosCallback(IntPtr window, IntPtr cbfun);
        private delegate IntPtr PFglfwSetScrollCallback(IntPtr window, IntPtr cbfun);

        private delegate IntPtr PFglfwGetCursorPos(IntPtr window, out double xpos, out double ypos);
        private delegate int PFglfwGetMouseButton(IntPtr window, int button);

        private delegate void PFglfwPollEvents();

        private delegate void PFglfwSetWindowShouldClose(IntPtr window, int value);
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool PFglfwWindowShouldClose(IntPtr window);

        // Vulkan functions
        private delegate int PFglfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr allocator, out IntPtr surface);
        private unsafe delegate char** PFglfwGetRequiredInstanceExtensions(out UInt32 count);
        #endregion
    }
}