using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Objects;
using System.Collections.Generic;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    public class VulkanUIButton : ICollectionObject, IVulkanObject
    {
        VulkanAPIManager m_apiManager;

        // Button state
        bool m_selected = false;
        bool m_clicked = false;

        // objects
        VulkanTextObject m_text;
        Vulkan2DRect m_mouseOverHighlight;
        Vulkan2DRect m_mouseClickHighlight;
        IVulkanObject[] m_objects;

        FMat4 m_projectionMatrix = new FMat4(1.0f);

        // collider
        VulkanBoxCollider m_collider;

        // delegates
        public delegate void OnClickDelegate();
        public delegate void OnSelectDelegate();
        OnClickDelegate m_onClickDelegate;
        OnSelectDelegate m_onSelectDelegate;
        (EventManager.HandleEventDelegate, EventType)[] m_eventListeners;

        IParent m_parent;

        public VulkanUIButton(VulkanAPIManager apiManager, AngelcodeFont font, string text, int fontSize, FVec2 location, FVec2 size, OnClickDelegate onClickDelegate = null, OnSelectDelegate onSelectDelegate = null)
        {
            m_apiManager = apiManager;
            m_onClickDelegate = onClickDelegate;
            m_onSelectDelegate = onSelectDelegate;

            // set up event listeners
            m_eventListeners = new(EventManager.HandleEventDelegate, EventType)[] { (MouseEventListener, EventType.Mouse) };

            // center text
            float textWidth = AngelcodeFontLoader.GetWidthOfString(font, fontSize, text) / 640.0f;
            float textHeight = AngelcodeFontLoader.GetHeightOfString(font, fontSize, text, size.X()) / 360.0f;
            FVec2 textOffset = new FVec2(location.X() + ((size.X() - textWidth) / 2.0f), location.Y() + ((size.Y() - textHeight) / 2.0f));

            m_text = new VulkanTextObject(apiManager, font, text, fontSize, textOffset, size.X());
            m_mouseOverHighlight = new Vulkan2DRect(apiManager, location, size, new FVec4(1.0f, 1.0f, 1.0f, .2f));
            m_mouseOverHighlight.Visible = false;
            m_mouseClickHighlight = new Vulkan2DRect(apiManager, location, size, new FVec4(.8f, .8f, .8f, .2f));
            m_mouseClickHighlight.Visible = false;
            m_objects = new IVulkanObject[] { m_text, m_mouseOverHighlight, m_mouseClickHighlight };

            m_collider = new VulkanBoxCollider(location, size);
        }

        public void Update()
        {
            if (m_clicked)
            {
                m_mouseClickHighlight.Visible = true;
            }
            else
            {
                m_mouseClickHighlight.Visible = false;
            }
            if (m_selected)
            {
                m_mouseOverHighlight.Visible = true;
            }
            else
            {
                m_mouseOverHighlight.Visible = false;
            }
        }

        FMat4 m_view;

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            m_projectionMatrix = projection;
            m_view = view;
            m_text.UpdateMVPData(projection, view, modelTransform);
            m_mouseOverHighlight.UpdateMVPData(projection, view, modelTransform);
            m_mouseClickHighlight.UpdateMVPData(projection, view, modelTransform);
            m_collider.UpdateMVPData(projection, view, modelTransform);
        }

        public void SetParent(IParent parent)
        {
            m_parent = parent;
            m_text.SetParent(parent);
            m_mouseOverHighlight.SetParent(parent);
            m_mouseClickHighlight.SetParent(parent);
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

        public FMat4 UIScale
        {
            get
            {
                return m_parent.UIScale;
            }
        }

        public void AddObject(IGraphicsObject obj)
        {

        }

        public VulkanBoxCollider GetCollider()
        {
            return m_collider;
        }

        public void MouseEventListener(IEvent e)
        {
            // cast e to MouseEvent
            MouseEvent mouseEvent = e as MouseEvent;

            // Get ray from camera to mouse position on screen
            FVec3 start = FMat4.UnProject(m_projectionMatrix, m_view, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            FVec3 end = FMat4.UnProject(m_projectionMatrix, m_view, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));
            //end = new FVec3(0, 0, 0);
            Ray mouseRay = new Ray(start, end - start);
            //Ray mouseRay = new Ray(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), -1.0f), new FVec3(0.0f, 0.0f, 1.0f));

            // Determine if mouse position is inside our collider, and select if it is
            //bool selected = m_collider.IsPointInside(new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            bool selected = m_collider.DoesIntersect(mouseRay);
            if (!m_selected && selected)
            {
                m_onSelectDelegate?.Invoke();
            }
            m_selected = selected;

            // If this is a mouse click event, call the click delegate
            if (m_selected && mouseEvent.Action == MouseAction.Down)
            {
                m_onClickDelegate?.Invoke();
                m_clicked = true;
            }
            else if (mouseEvent.Action == MouseAction.Up)
            {
                m_clicked = false;
            }
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
