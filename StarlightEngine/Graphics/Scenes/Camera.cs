using System;
using StarlightEngine.Math;

namespace StarlightEngine.Graphics.Scenes
{
    public class Camera
    {
        #region Private Members
        // The position of the camera in world space
        FVec3 m_position;

        // The world space point this camera is focused on
        FVec3 m_focus;

        // The which way in world space is up for this camera
        FVec3 m_up;

        // The view matrix for this camera (transfom world space into camera space)
        FMat4 m_view;
        #endregion

        #region Constructor
        public Camera(FVec3 position, FVec3 focus, FVec3 up)
        {
            m_position = position;
            m_focus = focus;
            m_up = up;

            m_view = FMat4.LookAt(position, focus, up);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// This camera's position in world space
        /// </summary>
        public FVec3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                m_view = FMat4.LookAt(m_position, m_focus, m_up);
            }
        }

        /// <summary>
        /// This camera's focus in world space
        /// </summary>
        public FVec3 Focus
        {
            get
            {
                return m_focus;
            }
            set
            {
                m_focus = value;
                m_view = FMat4.LookAt(m_position, m_focus, m_up);
            }
        }

        /// <summary>
        /// This camera's up direction in world space
        /// </summary>
        public FVec3 Up
        {
            get
            {
                return m_up;
            }
            set
            {
                m_up = value;
                m_view = FMat4.LookAt(m_position, m_focus, m_up);
            }
        }

        /// <summary>
        /// The view matrix to transform world space into this camera's local space
        /// </summary>
        public FMat4 View
        {
            get
            {
                return m_view;
            }
        }
        #endregion
    }
}