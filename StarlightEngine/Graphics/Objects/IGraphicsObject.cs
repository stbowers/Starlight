using System;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Objects
{
    /* Interface used to signal that a class can be rendered as a graphics object by any class implementing the IRenderer interface
	 */
    public interface IGraphicsObject
    {
        /// <summary>
        /// Called once per frame to update object before drawing
        /// </summary>
        void Update();

        /// <summary>
        /// Returns listeners for this object
        /// </summary>
        (EventManager.HandleEventDelegate, EventType)[] EventListeners { get; }

        /// <summary>
        /// Sets this object's parent
        /// </summary>
        void SetParent(IParent parent);

        bool Visible { get; set; }
    }
}
