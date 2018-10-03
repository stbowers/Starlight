using System;

namespace StarlightEngine.Events
{

    /// <summary>
    /// Specififes an event type
    /// </summary>
    public enum EventType
    {
        Keyboard,
        Mouse,
    }

    /// <summary>
    /// Common interface for engine events
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// The event type
        /// <summary>
        EventType Type { get; }
    }
}