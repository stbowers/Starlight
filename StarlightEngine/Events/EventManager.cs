using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using StarlightEngine.Graphics;
using StarlightEngine.Math;
using StarlightEngine.Threadding;

namespace StarlightEngine.Events
{
    /// <summary>
    /// Manages events for the engine
    /// </summary>
    public class EventManager
    {
        #region Private Helper Classes
        /// <summary>
        /// Holds details of a registered event listener
        /// </summary>
        private class EventListener
        {
            #region Private Members
            HandleEventDelegate m_handler;
            EventType m_filter;
            #endregion

            #region Constructors
            public EventListener(HandleEventDelegate handler, EventType filter)
            {
                m_handler = handler;
                m_filter = filter;
            }
            #endregion

            #region Public Accessors
            public HandleEventDelegate Handler
            {
                get
                {
                    return m_handler;
                }
            }
            public EventType Filter
            {
                get
                {
                    return m_filter;
                }
            }
            #endregion
        }
        #endregion

        #region Private Constants
        const float EVENT_THREAD_PERIOD = 1.0f / 100.0f; // run event thread at 100 Hz
        #endregion

        #region Private Members
        IWindowManager m_windowManager;

        // Thread to manage events
        Thread m_eventThread;
        Stopwatch m_eventThreadTimer;

        // Synchronize/protect event manager settings
        ThreadLock m_eventManagerSettingsLock = new ThreadLock(EngineConstants.THREADLEVEL_EVENTMANAGER);

        bool m_shouldEventManagerAbort;

        Queue<IEvent> m_queuedEvents;
        List<EventListener> m_listeners;
        #endregion

        #region Constructors
        public EventManager(IWindowManager windowManager)
        {
            m_windowManager = windowManager;
            m_windowManager.SetKeyboardEventDelegate(KeyboardEventHandler);
            m_windowManager.SetMouseEventDelegate(MouseEventHandler);

            // create thread
            m_eventThread = new Thread(EventThread);
            m_eventThread.Name = "Event Thread";
            m_eventThreadTimer = new Stopwatch();

            // initialize settings
            m_shouldEventManagerAbort = false;

            m_queuedEvents = new Queue<IEvent>();
            m_listeners = new List<EventListener>();

            // start thread
            m_eventThread.Start();
        }
        #endregion

        #region Thread Functions
        public void EventThread()
        {
            while (true)
            {
                // Lock
                m_eventManagerSettingsLock.EnterLock();

                // check if we should exit
                if (m_shouldEventManagerAbort)
                {
                    break;
                }

                // Start stopwatch
                m_eventThreadTimer.Start();

                // Unlock, before polling events
                m_eventManagerSettingsLock.ExitLock();

                // poll events
                m_windowManager.PollEvents();

                // Get lock again
                m_eventManagerSettingsLock.EnterLock();

                // Get list of events and clear the queue
                IEvent[] queuedEvents = m_queuedEvents.ToArray();
                (EventType, HandleEventDelegate)[] listeners = m_listeners.ConvertAll(x => (x.Filter, x.Handler)).ToArray();
                m_queuedEvents.Clear();

                // Release lock before calling event listeners
                m_eventManagerSettingsLock.ExitLock();

                // Handle events
                foreach (IEvent @event in queuedEvents)
                {
                    // loop through copy of event listeners, as calling some listeners may
                    // modify the list, which should only take effect on the next loop
                    foreach ((EventType filter, HandleEventDelegate handler) in listeners)
                    {
                        if (filter == @event.Type)
                        {
                            handler(@event);
                        }
                    }
                }

                // Wait until EVENT_THREAD_PERIOD has passed
                float timeLeft = EVENT_THREAD_PERIOD - (m_eventThreadTimer.ElapsedMilliseconds / 1000.0f);
                timeLeft = Functions.Clamp(timeLeft, 0.0f, float.MaxValue);
                m_eventThreadTimer.Stop();
                m_eventThreadTimer.Reset();
                Thread.Sleep((int)(timeLeft * 1000));
            }
        }
        #endregion

        #region Public Methods
        public void AddListener(HandleEventDelegate handler, EventType filter)
        {
            // Lock
            m_eventManagerSettingsLock.EnterLock();

			// Add listener
            m_listeners.Add(new EventListener(handler, filter));

            // Unlock
            m_eventManagerSettingsLock.ExitLock();
        }

        public void RemoveListener(HandleEventDelegate handler)
        {
            // Lock
            m_eventManagerSettingsLock.EnterLock();

			// Remove any listeners that match this handler function
			m_listeners.RemoveAll(listener => listener.Handler == handler);

            // Unlock
            m_eventManagerSettingsLock.ExitLock();
        }

        /* terminates the event manager thread and joins it
		 */
        public void TerminateEventManagerAndJoin()
        {
            // get lock
            m_eventManagerSettingsLock.EnterLock();

            // tell thread to abort
            m_shouldEventManagerAbort = true;

            // release lock
            m_eventManagerSettingsLock.ExitLock();

            // join thread
            m_eventThread.Join();
        }
        #endregion

        #region Delegate Definitions
        public delegate void HandleEventDelegate(IEvent @event);
        #endregion

        #region Callback Functions
        private void KeyboardEventHandler(Key key, KeyAction action, KeyModifiers modifiers)
        {
            // Lock event queue
            m_eventManagerSettingsLock.EnterLock();

            // Create new key event, and push onto the event queue
            KeyboardEvent keyEvent = new KeyboardEvent(key, action, modifiers);
            m_queuedEvents.Enqueue(keyEvent);

            // Unlock event queue
            m_eventManagerSettingsLock.ExitLock();
        }

        private void MouseEventHandler(MouseButton button, MouseAction action, KeyModifiers modifiers, FVec2 mousePosition, float scrollX, float scrollY){
            // Lock event queue
            m_eventManagerSettingsLock.EnterLock();

            // Create new mouse event, and push onto the event queue
            MouseEvent mouseEvent = new MouseEvent(button, action, modifiers, mousePosition, scrollX, scrollY);
            m_queuedEvents.Enqueue(mouseEvent);

            // Unlock event queue
            m_eventManagerSettingsLock.ExitLock();
        }
        #endregion
    }
}
