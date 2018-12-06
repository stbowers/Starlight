using System;
using System.Linq;
using System.Collections.Generic;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.Graphics.Objects
{
    public class HyperlaneOverlay : IVulkanObject, IParent
    {
        const float X_OFFSET = .025f;
        const float Y_OFFSET = .025f;
        VulkanAPIManager m_apiManager;

        List<Vulkan3DLine> m_lines = new List<Vulkan3DLine>();

        IParent m_parent;
        bool m_visible;
        public HyperlaneOverlay(VulkanAPIManager apiManager, StarSystem[] systems)
        {
            m_apiManager = apiManager;
            // create line for each connection in systems
            foreach (StarSystem system in systems)
            {
                foreach (StarSystem neighbor in system.Neighbors)
                {
                    Vulkan3DLine hyperlane = new Vulkan3DLine(apiManager, new FVec3(system.Location.X() + X_OFFSET, system.Location.Y() + Y_OFFSET, 0.0f), new FVec3(neighbor.Location.X() + X_OFFSET, neighbor.Location.Y() + Y_OFFSET, 0.0f), new FVec4(0.0f, 0.15f, 1.0f, 1.0f), FMat4.Identity, FMat4.Identity, FMat4.Identity);
                    m_lines.Add(hyperlane);
                    hyperlane.SetParent(this);
                }
            }
        }

        public void AddObject(IGameObject obj) { }
        public void RemoveObject(IGameObject obj) { }

        public void ChildUpdated(IGameObject child)
        {
            m_parent?.ChildUpdated(this);
        }

        public void Update()
        {

        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 model)
        {
            foreach (Vulkan3DLine line in m_lines)
            {
                line.UpdateMVPData(projection, view, model);
            }
        }

        public void SetParent(IParent parent)
        {
            m_parent = parent;
            if (m_parent != null)
            {
                UpdateMVPData(m_parent.Projection, m_parent.View, m_parent.Model);
            }
        }

        public VulkanCore.Rect2D ClipArea { get; set; }

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
                foreach (Vulkan3DLine line in m_lines)
                {
                    line.Visible = value;
                }
            }
        }

        public FMat4 Projection
        {
            get
            {
                return m_parent?.Projection;
            }
        }

        public FMat4 View
        {
            get
            {
                return m_parent?.View;
            }
        }

        public FMat4 Model
        {
            get
            {
                return m_parent?.Model;
            }
        }

        public FMat4 UIScale
        {
            get
            {
                return m_parent?.UIScale;
            }
        }

        public T[] GetChildren<T>()
        where T : IGameObject
        {
            return
            (
                from line in m_lines
                where line is T
                select (T)(IGameObject)line
            ).ToArray();
        }
    }
}