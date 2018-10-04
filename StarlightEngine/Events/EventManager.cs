using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using StarlightEngine.Graphics;
using StarlightEngine.Graphics.Math;

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

        // Synchronize event manager settings on a reader/writer lock
        ReaderWriterLockSlim m_eventManagerSettingsLock;

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
            m_eventThreadTimer = new Stopwatch();

            // create lock
            m_eventManagerSettingsLock = new ReaderWriterLockSlim();

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
                m_eventManagerSettingsLock.EnterReadLock();

                if (m_shouldEventManagerAbort)
                {
                    break;
                }

                // Start stopwatch
                m_eventThreadTimer.Start();

                // Unlock
                m_eventManagerSettingsLock.ExitReadLock();

                // poll events
                m_windowManager.PollEvents();

                // Lock, with option to elivate to writer lock
                m_eventManagerSettingsLock.EnterUpgradeableReadLock();

                // Handle events
                foreach (IEvent @event in m_queuedEvents)
                {
                    // loop through copy of event listeners, as calling some listeners may
                    // modify the list, which should only take effect on the next loop
                    foreach (EventListener listener in m_listeners.ToArray())
                    {
                        if (listener.Filter == @event.Type)
                        {
                            listener.Handler(@event);
                        }
                    }
                }
                m_queuedEvents.Clear();

                // Unlock
                m_eventManagerSettingsLock.ExitUpgradeableReadLock();

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
            bool exitWithReadLock = false;
            if (m_eventManagerSettingsLock.IsUpgradeableReadLockHeld){
                // The thread holds an upgradable read lock, so upgrade to write lock
                m_eventManagerSettingsLock.EnterWriteLock();
            } else if (!m_eventManagerSettingsLock.IsReadLockHeld && !m_eventManagerSettingsLock.IsWriteLockHeld) {
                // The thread does not hold any lock, so enter write lock
                m_eventManagerSettingsLock.EnterWriteLock();
            } else if (m_eventManagerSettingsLock.IsWriteLockHeld){
                // the thread already holds the write lock, continue
            } else {
                // the thread is holding a read lock
                m_eventManagerSettingsLock.ExitReadLock();
                m_eventManagerSettingsLock.EnterWriteLock();
                exitWithReadLock = true;
            }

			// Add listener
            m_listeners.Add(new EventListener(handler, filter));

            // Unlock
            m_eventManagerSettingsLock.ExitWriteLock();
            if (exitWithReadLock){
                m_eventManagerSettingsLock.EnterReadLock();
            }
        }

        public void RemoveListener(HandleEventDelegate handler)
        {
            // Lock
            bool exitWithReadLock = false;
            if (m_eventManagerSettingsLock.IsUpgradeableReadLockHeld){
                // The thread holds an upgradable read lock, so upgrade to write lock
                m_eventManagerSettingsLock.EnterWriteLock();
            } else if (!m_eventManagerSettingsLock.IsReadLockHeld && !m_eventManagerSettingsLock.IsWriteLockHeld) {
                // The thread does not hold any lock, so enter write lock
                m_eventManagerSettingsLock.EnterWriteLock();
            } else if (m_eventManagerSettingsLock.IsWriteLockHeld){
                // the thread already holds the write lock, continue
            } else {
                // the thread is holding a read lock
                m_eventManagerSettingsLock.ExitReadLock();
                m_eventManagerSettingsLock.EnterWriteLock();
                exitWithReadLock = true;
            }

			// Remove any listeners that match this handler function
			m_listeners.RemoveAll(listener => listener.Handler == handler);

            // Unlock
            m_eventManagerSettingsLock.ExitWriteLock();
            if (exitWithReadLock){
                m_eventManagerSettingsLock.EnterReadLock();
            }
        }

        /* terminates the event manager thread and joins it
		 */
        public void TerminateEventManagerAndJoin()
        {
            // get lock
            m_eventManagerSettingsLock.EnterWriteLock();

            // tell thread to abort
            m_shouldEventManagerAbort = true;

            // release lock
            m_eventManagerSettingsLock.ExitWriteLock();

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
            m_eventManagerSettingsLock.EnterWriteLock();

            // Create new key event, and push onto the event queue
            KeyboardEvent keyEvent = new KeyboardEvent(key, action, modifiers);
            m_queuedEvents.Enqueue(keyEvent);

            // Unlock event queue
            m_eventManagerSettingsLock.ExitWriteLock();
        }

        private void MouseEventHandler(MouseButton button, MouseAction action, KeyModifiers modifiers, FVec2 mousePosition, float scrollX, float scrollY){
            // Lock event queue
            m_eventManagerSettingsLock.EnterWriteLock();

            // Create new mouse event, and push onto the event queue
            MouseEvent mouseEvent = new MouseEvent(button, action, modifiers, mousePosition, scrollX, scrollY);
            m_queuedEvents.Enqueue(mouseEvent);

            // Unlock event queue
            m_eventManagerSettingsLock.ExitWriteLock();
        }
        #endregion
    }
}
