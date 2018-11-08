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
        /// Adds an object to the parent
        /// <summary>
        void AddObject(IGraphicsObject obj);

        /// <summary>
        /// Removes an object from the parent
        /// <summary>
        void RemoveObject(IGraphicsObject obj);

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
    }
}