using System;

namespace StarlightEngine.Interop.glfw3
{
    public static class GLFWConstants
    {
        public const int GLFW_TRUE = 1;
        public const int GLFW_FALSE = 0;

        public const int GLFW_CLIENT_API = 0x00022001;
        public const int GLFW_RESIZABLE = 0x00020003;

        public const int GLFW_NO_API = 0;


        #region Key and button actions
        public const int GLFW_RELEASE = 0;
        public const int GLFW_PRESS = 1;
        public const int GLFW_REPEAT = 2;
        #endregion

        #region Mouse
        public const int GLFW_MOUSE_BUTTON_1 = 0;
        public const int GLFW_MOUSE_BUTTON_2 = 1;
        public const int GLFW_MOUSE_BUTTON_3 = 2;
        public const int GLFW_MOUSE_BUTTON_4 = 3;
        public const int GLFW_MOUSE_BUTTON_5 = 4;
        public const int GLFW_MOUSE_BUTTON_6 = 5;
        public const int GLFW_MOUSE_BUTTON_7 = 6;
        public const int GLFW_MOUSE_BUTTON_8 = 7;
        public const int GLFW_MOUSE_BUTTON_LAST = GLFW_MOUSE_BUTTON_8;
        public const int GLFW_MOUSE_BUTTON_LEFT = GLFW_MOUSE_BUTTON_1;
        public const int GLFW_MOUSE_BUTTON_RIGHT = GLFW_MOUSE_BUTTON_2;
        public const int GLFW_MOUSE_BUTTON_MIDDLE = GLFW_MOUSE_BUTTON_3;
        #endregion
    }
}