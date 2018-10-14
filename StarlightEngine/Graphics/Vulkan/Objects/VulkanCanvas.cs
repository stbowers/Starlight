using System;
using System.Collections.Generic;
using StarlightEngine.Events;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    /// <summary>
    /// Displays children objects as 2d objects on a flat plane in 3d space
    /// </summary>
    public class VulkanCanvas : ICollectionObject, IVulkanObject
    {
        #region Private Members
        List<IVulkanObject> m_objects = new List<IVulkanObject>();

        FMat4 m_modelMatrix;
        FMat4 m_modelTransform;
        FMat4 m_viewMatrix;
        FMat4 m_projectionMatrix;
        #endregion

        /// <summary>
        /// Create a canvas with a given size and transform into world space
        /// </summary>
        /// <param name="modelMatrix">
        /// The matrix used to transform canvas space to world space.
        /// NOTE: the vertices of the canvas are (-1, -1) to (1, 1)
        /// </param>
        /// <param name="internalSize">The size of canvas space</param>
        public VulkanCanvas(FMat4 modelMatrix, FVec2 internalSize, FMat4 projection, FMat4 view)
        {
            m_modelTransform = new FMat4(1.0f);
            m_modelMatrix = modelMatrix;
            m_viewMatrix = view;
            m_projectionMatrix = projection;
        }

        /// <summary>
        /// Creates a canvas in 2d space
        /// </summary>
        /// <param name="topLeft">The top-left point of the canvas on the screen</param>
        /// <param name="size">The size of the canvas in world space</param>
        /// <param name="internalSize">The size of canvas space</param>
        public VulkanCanvas(FVec2 topLeft, FVec2 size, FVec2 internalSize) :
        this(FMat4.Translate(new FVec3(topLeft.X(), topLeft.Y(), 0)) * FMat4.Scale(new FVec3(size.X() / internalSize.X(), size.Y() / internalSize.Y(), 1)) * FMat4.Translate(new FVec3(1, 1, 0)), internalSize, new FMat4(1.0f), new FMat4(1.0f))
        {
        }

        public void Update()
        {
        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            m_projectionMatrix = projection;
            m_viewMatrix = view;
            m_modelTransform = modelTransform;

            foreach (IVulkanObject child in m_objects)
            {
                child.UpdateMVPData(m_projectionMatrix, m_viewMatrix, m_modelTransform * m_modelMatrix);
            }
        }

        public void AddObject(IVulkanObject obj)
        {
            obj.UpdateMVPData(m_projectionMatrix, m_viewMatrix, m_modelTransform * m_modelMatrix);
            m_objects.Add(obj);
        }

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
                return m_viewMatrix;
            }
        }

        public FMat4 Model
        {
            get
            {
                return m_modelTransform * m_modelMatrix;
            }
        }

        public IGraphicsObject[] Objects
        {
            get
            {
                return m_objects.ToArray();
            }
        }

        public (EventManager.HandleEventDelegate, EventType)[] EventListeners
        {
            get
            {
                return null;
            }
        }
    }
}