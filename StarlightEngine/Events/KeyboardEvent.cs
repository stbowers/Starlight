using System;
using System.Collections.Generic;

namespace StarlightEngine.Events
{
    /// <summary>
    /// A key on the keyboard
    /// </summary>
    public enum Key
    {
        #region Printable Keys
        #endregion

        #region Special Keys
        Unknown,
        #endregion
    }

    /// <summary>
    /// The action of the key (up, down, or repeat)
    /// </summary>
    public enum KeyAction
    {
        Up,
        Down,
        Repeat
    }

    /// <summary>
    /// Modifier keys
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift,
        Ctrl,
        Alt,
        Super,
        All = Shift | Ctrl | Alt | Super
    }

    /// <summary>
    /// An event for a key being pressed or released
    /// </summary>
    public class KeyboardEvent : IEvent
    {
        #region Private Members
        Key m_key;
        KeyAction m_action;
        KeyModifiers m_modifiers;
        #endregion

        #region Constructors
        public KeyboardEvent(Key key, KeyAction action, KeyModifiers modifiers)
        {
            m_key = key;
            m_action = action;
            m_modifiers = modifiers;
        }
        #endregion

        #region IEvent Implementation
        public EventType Type
        {
            get
            {
                return EventType.Keyboard;
            }
        }
        #endregion

        #region Public Accessors
        /// <summary>
        /// The key that is being pressed
        /// </summary>
        public Key Key
        {
            get
            {
                return m_key;
            }
        }

        /// <summary>
        /// The key action (pressed, released, or repeat)
        /// </summary>
        public KeyAction Action
        {
            get
            {
                return m_action;
            }
        }

        /// <summary>
        /// Modifier keys being pressed
        /// </summary>
        public KeyModifiers Modifiers
        {
            get
            {
                return m_modifiers;
            }
        }
        #endregion
    }
}