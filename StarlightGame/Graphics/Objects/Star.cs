using System;
using System.Linq;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightEngine.Events;
using StarlightEngine.Math;
using StarlightGame.GameCore;

namespace StarlightGame.Graphics.Objects
{
    /// <summary>
    /// A graphical representaiton of a star system for the map screen
    /// </summary>
    public class Star : IVulkanObject, ISubscriberObject, IParent
    {
        #region Private members
        VulkanAPIManager m_apiManager;
        IParent m_parent;

        // System
        StarSystem m_system;

        // Objects
        VulkanRecolorable2DSprite m_sprite;
        VulkanBoxCollider m_collider;
        IVulkanObject[] m_objects;

        // Callbacks
        MouseDelegate m_mouseClickDelegate;
        MouseDelegate m_mouseOverDelegate;
        MouseDelegate m_mouseExitDelegate;

        // button state
        bool m_selected;
        bool m_clicked;

        // Event listeners
        (string, EventManager.EventHandler)[] m_eventSubscribers;

        VulkanCore.Rect2D m_clipArea;
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

            createInfo.FileName = "./assets/Star-recolor.png";
            VulkanTexture starTexture = VulkanTextureCache.GetTexture(createInfo.FileName, createInfo);

            // get colors of owner (or grey if no owner)
            FVec4 primaryColor = (system.Owner != null) ? system.Owner.PrimaryColor : new FVec4(0.7f, 0.7f, 0.7f, 1.0f);
            FVec4 secondaryColor = (system.Owner != null) ? system.Owner.SecondaryColor : new FVec4(0.2f, 0.2f, 0.2f, 1.0f);
            m_sprite = new VulkanRecolorable2DSprite(m_apiManager, starTexture, system.Location, new FVec2(.05f, .05f),
                new FVec4(0.0f, 0.0f, 0.0f, 1.0f), primaryColor,
                new FVec4(1.0f, 0.0f, 0.0f, 1.0f), secondaryColor
            );
            m_collider = new VulkanBoxCollider(system.Location, new FVec2(.05f, .05f));

            m_objects = new IVulkanObject[] { m_sprite, m_collider };

            m_mouseClickDelegate = clickDelegate;
            m_mouseOverDelegate = mouseOverDelegate;
            m_mouseExitDelegate = mouseExitDelegate;

            m_eventSubscribers = new(string, EventManager.EventHandler)[] { (MouseEvent.ID, MouseEventListener) };

            Visible = true;
        }

        public void UpdateOwner(Empire owner)
        {
            // get colors of owner (or grey if no owner)
            FVec4 primaryColor = (owner != null) ? owner.PrimaryColor : new FVec4(0.7f, 0.7f, 0.7f, 1.0f);
            FVec4 secondaryColor = (owner != null) ? owner.SecondaryColor : new FVec4(0.2f, 0.2f, 0.2f, 1.0f);
            m_sprite.UpdateRecolorSettings(
                new FVec4(0.0f, 0.0f, 0.0f, 1.0f), primaryColor, // black to empire primary
                new FVec4(1.0f, 0.0f, 0.0f, 1.0f), secondaryColor // red to empire secondary
            );
        }

        public void Update()
        {
            foreach (IGameObject obj in m_objects)
            {
                obj.Update();
            }
        }

        public void ChildUpdated(IGameObject child)
        {
            if (m_parent != null)
            {
                m_parent.ChildUpdated(this);
            }
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

        public VulkanCore.Rect2D ClipArea
        {
            get
            {
                return m_clipArea;
            }
            set
            {
                m_clipArea = value;
                foreach (IVulkanObject obj in m_objects)
                {
                    obj.ClipArea = value;
                }
            }
        }

        public void AddObject(IGameObject obj)
        {

        }

        public void RemoveObject(IGameObject obj)
        {

        }

        public void MouseEventListener(object sender, IEvent e)
        {
            // e is mouse event
            MouseEvent mouseEvent = e as MouseEvent;

            // Get ray from camera to mouse position on screen
            FVec3 start = FMat4.UnProject(m_parent.Projection, m_parent.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            FVec3 end = FMat4.UnProject(m_parent.Projection, m_parent.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));
            //end = new FVec3(0, 0, 0);
            Ray mouseRay = new Ray(start, end - start);
            //Ray mouseRay = new Ray(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), -1.0f), new FVec3(0.0f, 0.0f, 1.0f));

            // Determine if mouse position is inside our collider, and select if it is
            //bool selected = m_collider.IsPointInside(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            bool selected = m_collider.DoesIntersect(mouseRay) && Visible;
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
                m_clicked = true;
            }
            else if (m_clicked && mouseEvent.Action == MouseAction.Up)
            {
                m_mouseClickDelegate?.Invoke(this);
                m_clicked = false;
            }
            else
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
            m_sprite.UpdateMVPData(projection, view, modelTransform);
            m_collider.UpdateMVPData(projection, view, modelTransform);
        }

        public T[] GetChildren<T>()
        where T : IGameObject
        {
            return
            (
                from obj in m_objects
                where obj is T
                select (T)obj
            ).ToArray();
        }

        bool m_visible;
        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
                foreach (IGameObject obj in m_objects)
                {
                    obj.Visible = value;
                }
            }
        }

        public (string, EventManager.EventHandler)[] Subscribers
        {
            get
            {
                return m_eventSubscribers;
            }
        }
    }
}