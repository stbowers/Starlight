using System.Collections.Generic;
using System.Linq;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightEngine.Graphics.Scenes
{
    public class Scene : IParent
    {
        // list of objects belonging to this scene
        List<IGameObject> m_objects = new List<IGameObject>();

        // Camera
        Camera m_camera;

        // Projection matrix - transforms camera space into screen space
        FMat4 m_projectionMatrix;

        // Scale matrix for UI elements in this scene
        FMat4 m_uiScale;

        // Caches for children, event subscribers and graphics objects
        List<IGameObject> m_children;
        List<IGraphicsObject> m_childGraphicsObjects;
        List<ISubscriberObject> m_childSubscriptionObjects;

        // Hash code - updated if any object in the scene is updated, tells the renderer to redraw the scene
        int m_hashCode = 0;

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
        public void AddObject(IGameObject obj)
        {
            m_objects.Add(obj);
            obj.SetParent(this);
            UpdateChildrenList();
            m_hashCode++;
        }

        public void RemoveObject(IGameObject obj)
        {
            m_objects.Remove(obj);
            obj.SetParent(null);
            UpdateChildrenList();
            m_hashCode++;
        }

        public void Update()
        {
            // update direct children
            foreach (IGameObject obj in m_objects)
            {
                obj.Update();
            }
        }

        public void SetParent(IParent parent)
        {

        }

        public void ChildUpdated(IGameObject child)
        {
            UpdateChildrenList();
            m_hashCode++;
        }

        private void UpdateChildrenList()
        {
            m_children = new List<IGameObject>();
            m_childGraphicsObjects = new List<IGraphicsObject>();
            m_childSubscriptionObjects = new List<ISubscriberObject>();
            foreach (IGameObject obj in m_objects)
            {
                m_children.Add(obj);

                if (obj is IParent)
                {
                    m_children.AddRange((obj as IParent).GetChildren<IGameObject>());
                }
            }

            foreach (IGameObject child in m_children)
            {
                IGraphicsObject graphicsObject = child as IGraphicsObject;
                ISubscriberObject subscriberObject = child as ISubscriberObject;

                if (graphicsObject != null)
                {
                    if (graphicsObject.Visible)
                    {
                        m_childGraphicsObjects.Add(graphicsObject);
                    }
                }

                if (subscriberObject != null)
                {
                    m_childSubscriptionObjects.Add(subscriberObject);
                }
            }
        }

        public T[] GetChildren<T>()
        where T : IGameObject
        {
            if (typeof(T) == typeof(IGameObject))
            {
                return (from obj in m_children select (T)obj).ToArray();
            }
            else if (typeof(T) == typeof(IGraphicsObject))
            {
                return (from obj in m_childGraphicsObjects select (T)obj).ToArray();
            }
            else if (typeof(T) == typeof(ISubscriberObject))
            {
                return (from obj in m_childSubscriptionObjects select (T)obj).ToArray();
            }
            else
            {
                return (from obj in m_children where obj is T select (T)obj).ToArray();
            }
        }

        public (string, EventManager.EventHandler)[] GetEventSubscriptions()
        {
            return (
                from subscriberObject in m_childSubscriptionObjects
                from eventSubscription in subscriberObject.Subscribers
                select eventSubscription
            ).ToArray();
        }

        public override int GetHashCode()
        {
            return m_hashCode;
        }

        public bool Visible
        {
            get
            {
                return true;
            }
            set
            { }
        }
    }
}