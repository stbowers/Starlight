using System;
using StarlightEngine.Math;

namespace StarlightEngine.Graphics.Objects
{
    public interface IParent
    {
        /// <summary>
        /// Scales UI elements in the parent's space.
        /// </summary>
        FMat4 UIScale { get; }

        /// <summary>
        /// Gets the parent's projection transform
        /// </summary>
        FMat4 Projection { get; }

        /// <summary>
        /// Gets the parent's view transform
        /// </summary>
        FMat4 View { get; }

        /// <summary>
        /// Gets the parent's model transform
        /// </summary>
        FMat4 Model { get; }

        void AddObject(IGraphicsObject obj);
        void RemoveObject(IGraphicsObject obj);
        IGraphicsObject[] Children { get; }
    }
}