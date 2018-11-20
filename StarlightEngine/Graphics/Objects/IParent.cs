using System;
using StarlightEngine.Math;

namespace StarlightEngine.Graphics.Objects
{
    /// <summary>
    /// An object which contains child objects
    /// </summary>
    public interface IParent : IGameObject
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

        /// <summary>
        /// Add an object to the parent
        /// </summary>
        void AddObject(IGameObject obj);

        /// <summary>
        /// Remove an object from the parent
        /// </summary>
        void RemoveObject(IGameObject obj);

        /// <summary>
        /// Notifies the parent that one of its children has been modified
        /// </summary>
        void ChildUpdated(IGameObject child);

        /// <summary>
        /// Get all objects under this parent of a given type (recursive - i.e. also includes children's children)
        /// </summary>
        T[] GetChildren<T>() where T : IGameObject;
    }
}