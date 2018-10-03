using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Math;

namespace StarlightEngine.Events
{
    /// <summary>
    /// Mouse button (none if this is a mouse motion event)
    /// </summary>
    public enum MouseButton
    {
        Button1 = 0,
        Button2 = 1,
        Button3 = 2,
        Button4 = 3,
        Button5 = 4,
        Button6 = 5,
        Button7 = 6,
        Button8 = 7,
        Left = Button1,
        Right = Button2,
        Middle = Button3,
        None,
    }

    /// <summary>
    /// button action (or none if mouse motion event)
    /// </summary>
    public enum MouseAction
    {
        Up,
        Down,
        None
    }

    /// <summary>
    /// An event for the mouse moving or a mouse button being pressed
    /// </summary>
    public class MouseEvent : IEvent
    {
        #region Private Members
        MouseButton m_button;
        MouseAction m_action;
        KeyModifiers m_modifiers;
        FVec2 m_position;
        float m_scrollX;
        float m_scrollY;
        #endregion

        #region Constructors
        public MouseEvent(MouseButton button, MouseAction action, KeyModifiers modifiers, FVec2 mousePosition, float scrollX, float scrollY)
        {
            m_button = button;
            m_action = action;
            m_modifiers = modifiers;
            m_position = mousePosition;
            m_scrollX = scrollX;
            m_scrollY = scrollY;
        }
        #endregion

        #region IEvent Implementation
        public EventType Type
        {
            get
            {
                return EventType.Mouse;
            }
        }
        #endregion

        #region Public Accessors
        /// <summary>
        /// The button that has changed (or none if this is a motion event)
        /// </summary>
        public MouseButton Button
        {
            get
            {
                return m_button;
            }
        }

        /// <summary>
        /// The mouse button action (up, down, or none if this is a motion event)
        /// </summary>
        public MouseAction Action
        {
            get
            {
                return m_action;
            }
        }

        /// <summary>
        /// Modifier keys pressed during event
        /// </summary>
        public KeyModifiers KeyModifiers
        {
            get
            {
                return m_modifiers;
            }
        }
        /// <summary>
        /// The mouse position
        /// </summary>
        public FVec2 MousePosition
        {
            get
            {
                return m_position;
            }
        }

        /// <summary>
        /// The scroll ammount in the x direction
        /// </summary>
        public float ScrollX
        {
            get
            {
                return m_scrollX;
            }
        }

        /// <summary>
        /// The scroll ammount in the y direction
        /// </summary>
        public float ScrollY
        {
            get
            {
                return m_scrollY;
            }
        }
        #endregion
    }
}