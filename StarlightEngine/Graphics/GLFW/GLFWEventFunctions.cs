using System;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.GLFW
{
    /// <summary>
    /// Helper events for converting GLFW events into engine events
    /// </summary>
    public class GLFWEventFunctions
    {
        #region Keyboard Events
        public static (Key key, KeyAction action, KeyModifiers modifiers) GetKeyboardEventDetails(int key, int scancode, int action, int mods)
        {
            Key _key = GetKey(key, scancode);
            KeyAction _action = GetKeyAction(action);
            KeyModifiers _modifiers = GetKeyModifiers(mods);

            return (_key, _action, _modifiers);
        }

        public static Key GetKey(int key, int scancode)
        {
            switch (key)
            {
                default:
                    return Key.Unknown;
            }
        }

        public static KeyAction GetKeyAction(int action)
        {
            switch (action)
            {
                case 0:
                    // GLFW_RELEASE
                    return KeyAction.Up;
                case 1:
                    // GLFW_PRESS
                    return KeyAction.Down;
                case 2:
                    // GLFW_REPEAT
                    return KeyAction.Repeat;
                default:
                    return KeyAction.Down;
            }
        }

        public static KeyModifiers GetKeyModifiers(int mods)
        {
            KeyModifiers modifiers = KeyModifiers.None;
            if ((mods & 0x0001) != 0)
            {
                modifiers |= KeyModifiers.Shift;
            }
            if ((mods & 0x0002) != 0)
            {
                modifiers |= KeyModifiers.Ctrl;
            }
            if ((mods & 0x0004) != 0)
            {
                modifiers |= KeyModifiers.Alt;
            }
            if ((mods & 0x0008) != 0)
            {
                modifiers |= KeyModifiers.Super;
            }
            return modifiers;
        }
        #endregion

        #region Mouse Events
        public static (MouseButton button, MouseAction action, KeyModifiers modifiers) GetMouseEventDetails(int button, int action, int mods)
        {
            MouseButton _button = GetMouseButton(button);
            MouseAction _action = GetMouseAction(action);
            KeyModifiers _modifiers = GetKeyModifiers(mods);

            return (_button, _action, _modifiers);
        }
        
        public static MouseButton GetMouseButton(int button){
            // our MouseButton struct is layed out the same as GLFW's mouse button definitions, so we can just cast
            return (MouseButton)button;
        }

        public static MouseAction GetMouseAction(int action)
        {
            switch (action)
            {
                case 0:
                    // GLFW_RELEASE
                    return MouseAction.Up;
                case 1:
                    // GLFW_PRESS
                    return MouseAction.Down;
                default:
                    return MouseAction.None;
            }
        }
        #endregion
    }
}