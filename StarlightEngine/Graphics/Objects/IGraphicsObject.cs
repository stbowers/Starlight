using System;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Objects
{
    /// <summary>
    /// Interface used to signal that a class can be rendered as a graphics object by any class implementing the IRenderer interface
    /// </summary>
    public interface IGraphicsObject : IGameObject
    {
    }
}
