using System;

namespace StarlightEngine.Graphics.Objects
{
    /// <summary>
    /// Common interface for all game objects
    /// </summary>
    public interface IGameObject
    {
        /// <summary>
        /// Called once per frame to update object before drawing
        /// </summary>
        void Update();

        /// <summary>
        /// Sets this object's parent
        /// </summary>
        void SetParent(IParent parent);

        /// <summary>
        /// Should this object be rendered?
        /// </summary>
        bool Visible { get; set; }
    }
}