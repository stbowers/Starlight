using System;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightEngine.Events;
using StarlightEngine.Math;

namespace StarlightGame.Graphics.Objects
{
    /// <summary>
    /// A graphical representaiton of a star system for the map screen
    /// </summary>
    public class Star : ICollectionObject, IVulkanObject
    {
        #region Private members
        VulkanAPIManager m_apiManager;
        IParent m_parent;

        // System
        StarSystem m_system;

        // Objects
        Vulkan2DSprite m_sprite;
        VulkanBoxCollider m_collider;
        IGraphicsObject[] m_objects;

        // Callbacks
        MouseDelegate m_mouseClickDelegate;
        MouseDelegate m_mouseOverDelegate;
        MouseDelegate m_mouseExitDelegate;

        // button state
        bool m_selected;
        bool m_clicked;

        // Event listeners
        (EventManager.HandleEventDelegate, EventType)[] m_eventListeners;

        // Object matrices
        FMat4 m_projectionMatrix;
        FMat4 m_viewMatrix;
        #endregion

        #region Delegates
        public delegate void MouseDelegate(Star forObject);
        #endregion

        public Star(VulkanAPIManager apiManager, StarSystem system, MouseDelegate clickDelegate = null, MouseDelegate mouseOverDelegate = null, MouseDelegate mouseExitDelegate = null)
        {
            m_apiManager = apiManager;

            m_system = system;

            VulkanTextureCreateInfo createInfo = new VulkanTextureCreateInfo();
            createInfo.APIManager = m_apiManager;
            createInfo.EnableMipmap = false;
            createInfo.MagFilter = VulkanCore.Filter.Nearest;
            createInfo.MinFilter = VulkanCore.Filter.Nearest;

            createInfo.FileName = "./assets/Star.png";
            VulkanTexture starTexture = VulkanTextureCache.GetTexture(createInfo.FileName, createInfo);

            m_sprite = new Vulkan2DSprite(m_apiManager, starTexture, system.Location, new FVec2(.03f, .03f));
            m_collider = new VulkanBoxCollider(system.Location, new FVec2(.03f, .03f));

            m_objects = new IGraphicsObject[] { m_sprite, m_collider };

            m_mouseClickDelegate = clickDelegate;
            m_mouseOverDelegate = mouseOverDelegate;
            m_mouseExitDelegate = mouseExitDelegate;

            m_eventListeners = new(EventManager.HandleEventDelegate, EventType)[] { (MouseEventListener, EventType.Mouse) };
        }

        public void Update()
        {

        }

        public FMat4 UIScale
        {
            get
            {
                return m_parent.UIScale;
            }
        }

        public void SetParent(IParent parent)
        {
            m_parent = parent;
        }

        public FMat4 Projection
        {
            get
            {
                return m_parent.Projection;
            }
        }

        public FMat4 View
        {
            get
            {
                return m_parent.View;
            }
        }

        public FMat4 Model
        {
            get
            {
                return m_parent.Model;
            }
        }

        public void AddObject(IGraphicsObject obj)
        {

        }

        public void MouseEventListener(IEvent e)
        {
            // e is mouse event
            MouseEvent mouseEvent = e as MouseEvent;

            // Get ray from camera to mouse position on screen
            FVec3 start = FMat4.UnProject(m_projectionMatrix, m_viewMatrix, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            FVec3 end = FMat4.UnProject(m_projectionMatrix, m_viewMatrix, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));
            //end = new FVec3(0, 0, 0);
            Ray mouseRay = new Ray(start, end - start);
            //Ray mouseRay = new Ray(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), -1.0f), new FVec3(0.0f, 0.0f, 1.0f));

            // Determine if mouse position is inside our collider, and select if it is
            //bool selected = m_collider.IsPointInside(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            bool selected = m_collider.DoesIntersect(mouseRay);
            if (!m_selected && selected)
            {
                m_mouseOverDelegate?.Invoke(this);
            }
            else if (m_selected && !selected)
            {
                m_mouseExitDelegate?.Invoke(this);
            }
            m_selected = selected;

            // If this is a mouse click event, call the click delegate
            if (m_selected && mouseEvent.Action == MouseAction.Down)
            {
                m_mouseClickDelegate?.Invoke(this);
                m_clicked = true;
            }
            else if (mouseEvent.Action == MouseAction.Up)
            {
                m_clicked = false;
            }
        }

        public StarSystem System
        {
            get
            {
                return m_system;
            }
        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            m_projectionMatrix = projection;
            m_viewMatrix = view;

            m_sprite.UpdateMVPData(projection, view, modelTransform);
            m_collider.UpdateMVPData(projection, view, modelTransform);
        }

        public IGraphicsObject[] Objects
        {
            get
            {
                return m_objects;
            }
        }

        public (EventManager.HandleEventDelegate, EventType)[] EventListeners
        {
            get
            {
                return m_eventListeners;
            }
        }
    }
}