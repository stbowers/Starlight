using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightEngine.Graphics.Scenes
{
    public class Scene : IParent
    {
        List<IGraphicsObject> m_objects = new List<IGraphicsObject>();
        List<(EventManager.HandleEventDelegate, EventType)> m_eventListeners = new List<(EventManager.HandleEventDelegate, EventType)>();

        // Camera
        Camera m_camera;

        // Projection matrix - transforms camera space into screen space
        FMat4 m_projectionMatrix;

        // Scale matrix for UI elements in this scene
        FMat4 m_uiScale;

        /// <summary>
        /// Create a scene with a camera and specified fov
        /// </summary>
        /// <param name="camera">The camera for this scene</param>
        /// <param name="fov">The fov for this scene in radians</param>
        /// <param name="screenWidth">The width of the screen (or drawing surface) in pixels</param>
        /// <param name="screenHeight">The height of the screen (or drawing surface) in pixels</param>
        /// <param name="znear">The z value of the near clipping plane</param>
        /// <param name="zfar">The z value of the far clipping plane</param>
        public Scene(Camera camera, float fov, int screenWidth, int screenHeight, float znear, float zfar)
        {
            m_camera = camera;

            // Create projection matrix
            m_projectionMatrix = FMat4.Perspective(fov, screenWidth / screenHeight, znear, zfar);

            // Flip Y axis for Vulkan
            m_projectionMatrix[1, 1] *= -1.0f;

            // Set UI scale
            m_uiScale = FMat4.Scale(new FVec3(2.0f / (float)screenWidth, 2.0f / (float)screenHeight, 1));
        }

        #region Public Properties
        /// <summary>
        /// This scene's camera
        /// </summary>
        public Camera Camera
        {
            get
            {
                return m_camera;
            }
        }

        /// <summary>
        /// This scene's projection matrix - transforms camera space into screen space
        /// </summary>
        public FMat4 Projection
        {
            get
            {
                return m_projectionMatrix;
            }
        }

        public FMat4 View
        {
            get
            {
                return m_camera.View;
            }
        }

        public FMat4 Model
        {
            get
            {
                return FMat4.Identity;
            }
        }

        /// <summary>
        /// Get the scale for UI elements in this scene
        /// </summary>
        public FMat4 UIScale
        {
            get
            {
                return m_uiScale;
            }
        }
        #endregion

        /// <summary>
        /// Adds an object to this scene
        /// </summary>
        public void AddObject(IGraphicsObject obj)
        {
            m_objects.Add(obj);
            obj.SetParent(this);
        }

        public void RemoveObject(IGraphicsObject obj)
        {
            m_objects.Remove(obj);
            obj.SetParent(null);
        }

        public IGraphicsObject[] Children
        {
            get
            {
                List<IGraphicsObject> objects = new List<IGraphicsObject>();
                foreach (IGraphicsObject obj in m_objects)
                {
                    if (obj.Visible)
                    {
                        objects.Add(obj);
                        if (obj is IParent)
                        {
                            objects.AddRange((obj as IParent).Children);
                        }
                    }
                }
                return objects.ToArray();
            }
        }

        public List<(EventManager.HandleEventDelegate, EventType)> GetEventListeners()
        {
            return m_eventListeners;
        }
    }
}
