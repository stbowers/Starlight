using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightEngine.Graphics.Scenes
{
    public class Scene
    {
        SortedDictionary<int, List<IGraphicsObject>> m_objects = new SortedDictionary<int, List<IGraphicsObject>>();
        List<(EventManager.HandleEventDelegate, EventType)> m_eventListeners = new List<(EventManager.HandleEventDelegate, EventType)>();

        // Camera
        Camera m_camera;

        // Projection matrix - transforms camera space into screen space
        FMat4 m_projectionMatrix;

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
        #endregion

        /// <summary>
        /// Adds an object to this scene
        /// </summary>
        public void AddObject(int layer, IGraphicsObject obj){
            AddObject(layer, obj, true);
        }

        private void AddObject(int layer, IGraphicsObject obj, bool directAdd)
        {
            List<IGraphicsObject> list;
            if (m_objects.ContainsKey(layer))
            {
                list = m_objects[layer];
            }
            else
            {
                list = new List<IGraphicsObject>();
                m_objects.Add(layer, list);
            }

            // add object
            list.Add(obj);

            // if this is a vulkan object, update it's mvp
            if (directAdd && obj is IVulkanObject){
                (obj as IVulkanObject).UpdateMVPData(m_projectionMatrix, m_camera.View, FMat4.Identity);
            }

            // add listeners
            if (obj.EventListeners != null)
            {
                m_eventListeners.AddRange(obj.EventListeners);
            }

            // If it's a collection, add the other objects too
            if (obj is ICollectionObject)
            {
                foreach (IGraphicsObject child in (obj as ICollectionObject).Objects)
                {
                    AddObject(layer, child, false);
                }
            }
        }

        public void RemoveObject(IGraphicsObject obj)
        {
            foreach (var objList in m_objects)
            {
                if (objList.Value.Contains(obj))
                {
                    // remove object
                    objList.Value.Remove(obj);

                    // remove listeners
                    foreach ((EventManager.HandleEventDelegate, EventType) listener in obj.EventListeners)
                    {
                        m_eventListeners.Remove(listener);
                    }
                }
            }
        }

        public SortedDictionary<int, List<IGraphicsObject>> GetObjects()
        {
            return m_objects;
        }

        public List<(EventManager.HandleEventDelegate, EventType)> GetEventListeners()
        {
            return m_eventListeners;
        }
    }
}
