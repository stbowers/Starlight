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
    public class VulkanCanvas : ICollectionObject, IVulkanObject, IParent
    {
        #region Private Members
        List<IVulkanObject> m_objects = new List<IVulkanObject>();

        FMat4 m_modelMatrix;
        FMat4 m_modelTransform;
        FMat4 m_viewMatrix;
        FMat4 m_projectionMatrix;
        FMat4 m_uiScale;

        IParent m_parent;

        VulkanCore.Rect2D m_clipArea;

        bool m_lockToScreen = false;
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
            m_modelTransform = FMat4.Identity;
            m_modelMatrix = modelMatrix;
            m_viewMatrix = view;
            m_projectionMatrix = projection;
            m_uiScale = FMat4.Identity;
        }

        /// <summary>
        /// Creates a 2d canvas in 3d space
        /// </summary>
        /// <param name="topLeft">The top-left point of the canvas on the screen</param>
        /// <param name="size">The size of the canvas in world space</param>
        /// <param name="internalSize">The size of canvas space</param>
        public VulkanCanvas(FVec3 position, Quaternion rotation, FMat4 projection, FMat4 view) :
        this(FMat4.Translate(position) * rotation.GetRotationMatrix(), new FVec2(2.0f, 2.0f), projection, view)
        {
            // Flip 180 degrees along x axis, since Y is flipped
            m_modelMatrix = FMat4.Rotate((float)System.Math.PI, FVec3.Right) * m_modelMatrix;
        }

        /// <summary>
        /// Creates a 2d canvas in 2d space
        /// </summary>
        /// <param name="topLeft">The top-left point of the canvas on the screen</param>
        /// <param name="size">The size of the canvas in world space</param>
        /// <param name="internalSize">The size of canvas space</param>
        public VulkanCanvas(FVec2 topLeft, FVec2 size, FVec2 internalSize) :
        this(FMat4.Translate(new FVec3(topLeft.X(), topLeft.Y(), 0)) * FMat4.Scale(new FVec3(size.X() / internalSize.X(), size.Y() / internalSize.Y(), 1)) * FMat4.Translate(new FVec3(1, 1, 0)), internalSize, new FMat4(1.0f), new FMat4(1.0f))
        {
            // Lock the projection and view matricies from being updated
            m_lockToScreen = true;
            m_uiScale = FMat4.Scale(new FVec3(internalSize.X() / size.X(), internalSize.Y() / size.Y(), 1.0f));
        }

        public void Update()
        {
        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            if (!m_lockToScreen)
            {
                m_projectionMatrix = projection;
                m_viewMatrix = view;
                m_modelTransform = modelTransform;
            }

            // recalculate clip coordinates
            FMat4 mvpMatrix = m_projectionMatrix * m_viewMatrix * m_modelTransform * m_modelMatrix;
            FVec4 topLeft = mvpMatrix * new FVec4(-1.0f, -1.0f, 0.0f, 1.0f);
            FVec4 extent = mvpMatrix * new FVec4(2.0f, 2.0f, 0.0f, 0.0f);
            m_clipArea.Offset.X = (int)(((topLeft.X() / topLeft.W()) + 1) * (1280.0f / 2.0f));
            m_clipArea.Offset.Y = (int)(((topLeft.Y() / topLeft.W()) + 1) * (720.0f / 2.0f));
            m_clipArea.Extent.Width = (int)(extent.X() * (1280.0f / 2.0f));
            m_clipArea.Extent.Height = (int)(extent.Y() * (720.0f / 2.0f));

            foreach (IVulkanObject child in m_objects)
            {
                child.UpdateMVPData(m_projectionMatrix, m_viewMatrix, m_modelTransform * m_modelMatrix);
                if (child is IVulkanDrawableObject)
                {
                    (child as IVulkanDrawableObject).ClipArea = m_clipArea;
                }
            }
        }

        public void AddObject(IGraphicsObject obj)
        {
            if (obj is IVulkanObject)
            {
                AddObject(obj as IVulkanObject);
            }
            else
            {
                throw new Exception("Can't add a non-vulkan object to a vulkan canvas");
            }
        }

        public void AddObject(IVulkanObject obj)
        {
            obj.UpdateMVPData(m_projectionMatrix, m_viewMatrix, m_modelTransform * m_modelMatrix);
            m_objects.Add(obj);
            obj.SetParent(this);
            IVulkanDrawableObject drawableObject = obj as IVulkanDrawableObject;
            if (drawableObject != null)
            {
                drawableObject.ClipArea = m_clipArea;
            }
        }

        public FMat4 UIScale
        {
            get
            {
                return (m_parent != null ? m_parent.UIScale : FMat4.Identity) * m_uiScale;
            }
            set
            {
                m_uiScale = value;
            }
        }

        public void SetParent(IParent parent)
        {
            m_parent = parent;
            foreach (IGraphicsObject child in m_objects)
            {
                child.SetParent(this);
            }
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