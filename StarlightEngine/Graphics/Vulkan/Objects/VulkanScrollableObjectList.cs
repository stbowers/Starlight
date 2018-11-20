using System;
using System.Collections.Generic;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    /// <summary>
    /// Varient of a canvas which forms a list which can be scrolled
    /// </summary>
    public class VulkanScrollableObjectList : VulkanCanvas, ISubscriberObject
    {
        EventManager m_eventManager;
        List<(IVulkanObject, float)> m_objectsInList = new List<(IVulkanObject, float)>();
        float m_listHeight;
        float m_maxHeight;
        float m_scrollBias;
        bool m_scrollEnabled;

        (string, EventManager.EventHandler)[] m_subscribers;

        public VulkanScrollableObjectList(FVec2 position, FVec2 size) :
        base(position, size, new FVec2(2, 2), false)
        {
            m_maxHeight = size.Y();

            m_subscribers = new(string, EventManager.EventHandler)[] { (MouseEvent.ID, MouseEventHandler) };
        }

        public (string, EventManager.EventHandler)[] Subscribers
        {
            get
            {
                return m_subscribers;
            }
        }

        public void MouseEventHandler(object sender, IEvent e)
        {
            MouseEvent mouseEvent = e as MouseEvent;

            if (mouseEvent.ScrollY != 0.0f)
            {
                float minScrollBias = 0.0f;
                float maxScrollBias = m_listHeight - m_maxHeight;

                m_scrollBias = Functions.Clamp(m_scrollBias + (0.1f * mouseEvent.ScrollY), minScrollBias, maxScrollBias);

                float yOffset = -m_scrollBias;
                foreach ((IVulkanObject obj, float height) in m_objectsInList)
                {
                    obj.UpdateMVPData(Projection, View, Model * FMat4.Translate(new FVec3(0.0f, yOffset, 0.0f)));
                    yOffset += height;
                }
            }
        }

        /// <summary>
        /// Adds an object to the scrollable list; note: adding an object by the method
        /// AddObject will not add it to the scrollable list, but will simply render it
        /// as a normal canvas would
        /// </summary>
        public void AddToList(IVulkanObject obj, float height)
        {
            m_objectsInList.Add((obj, height));
            base.AddObject(obj);
            obj.UpdateMVPData(Projection, View, Model * FMat4.Translate(new FVec3(0.0f, m_listHeight, 0.0f)));
            m_listHeight += height;
        }

        public void RemoveFromList(IVulkanObject obj)
        {
            m_objectsInList.Remove(m_objectsInList.Find(listObject => listObject.Item1 == obj));
            base.RemoveObject(obj);
            m_listHeight = 0.0f;
            m_objectsInList.ForEach(listObject => m_listHeight += listObject.Item2);
        }

        public void ClearList()
        {
            foreach ((IVulkanObject obj, float height) in m_objectsInList.ToArray())
            {
                RemoveFromList(obj);
            }
        }

        public override void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            base.UpdateMVPData(projection, view, modelTransform);

            float yOffset = 0;
            foreach ((IVulkanObject obj, float height) in m_objectsInList)
            {
                obj.UpdateMVPData(Projection, View, Model * FMat4.Translate(new FVec3(0.0f, yOffset, 0.0f)));
                yOffset += height;
            }
        }
    }
}