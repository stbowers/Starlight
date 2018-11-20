using System;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Objects
{
    /// <summary>
    /// An object which subscribes to events
    /// </summary>
    public interface ISubscriberObject
    {
        /// <summary>
        /// list of event ids this object subscribes to and the callback to call when an event occurs
        /// </summary>
        (string, EventManager.EventHandler)[] Subscribers { get; }
    }
}