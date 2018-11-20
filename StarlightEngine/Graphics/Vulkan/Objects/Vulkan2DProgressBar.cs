using System.Linq;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    public class Vulkan2DProgressBar : IVulkanObject, IParent
    {
        VulkanAPIManager m_apiManager;
        IParent m_parent;

        FVec2 m_size;

        // Objects
        Vulkan2DRect m_fill;
        Vulkan2DRectOutline m_outline;
        IGraphicsObject[] m_objects;

        public Vulkan2DProgressBar(VulkanAPIManager apiManager, FVec2 position, FVec2 size, float percentFilled, FVec4 color)
        {
            m_apiManager = apiManager;
            m_size = size;

            float fill = Functions.Clamp(percentFilled, 0.0f, 1.0f);
            FVec2 fillSize = new FVec2(fill * size.X(), size.Y());
            m_fill = new Vulkan2DRect(apiManager, position, fillSize, color);
            m_outline = new Vulkan2DRectOutline(apiManager, position, size, color);

            m_objects = new IGraphicsObject[] { m_fill, m_outline };
            m_fill.Visible = true;
            m_outline.Visible = true;
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
            UpdateMVPData(m_parent.Projection, m_parent.View, m_parent.Model);
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

        public void AddObject(IGameObject obj)
        {

        }

        public void RemoveObject(IGameObject obj)
        {

        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            m_fill.UpdateMVPData(projection, view, modelTransform);
            m_outline.UpdateMVPData(projection, view, modelTransform);
        }

        public void UpdatePercentage(float newPercentage)
        {
            float fill = Functions.Clamp(newPercentage, 0.0f, 1.0f);
            FVec2 fillSize = new FVec2(fill * m_size.X(), m_size.Y());

            m_fill.UpdateSize(fillSize);
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

        public void ChildUpdated(IGameObject child)
        {
            if (m_parent != null)
            {
                m_parent.ChildUpdated(this);
            }
        }

        public IGraphicsObject[] Children
        {
            get
            {
                return m_objects;
            }
        }

        public bool m_visible;
        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
                m_fill.Visible = value;
                m_outline.Visible = value;
            }
        }
    }
}
