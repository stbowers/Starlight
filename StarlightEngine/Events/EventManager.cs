using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using StarlightEngine.Graphics;
using glfw3;

namespace StarlightEngine.Events
{
	[Flags]
	public enum EventType
	{
		Keyboard
	}

	public interface IEvent
	{
		EventType Type { get; }
	}

	public class EventListener
	{
		public EventType filter;

		public delegate void HandleEvent(IEvent @event);
		public HandleEvent handler;
	}

	public class KeyboardEvent : IEvent
	{
		public EventType Type
		{
			get
			{
				return EventType.Keyboard;
			}
		}

		public Key key;
		public KeyAction action;
		public List<KeyModifier> modifiers;
	}

	/* Manages events for the engine
	 */
	public class EventManager
	{
		const float EVENT_THREAD_PERIOD = 1.0f / 100.0f; // run event thread at 100 Hz

		IWindowManager m_windowManager;

		// Thread to manage events
		Thread m_eventThread;
		Stopwatch m_eventThreadTimer;

		// Synchronize event manager settings on a reader/writer lock
		ReaderWriterLock m_eventManagerSettingsLock;

		bool m_shouldEventManagerAbort;

		Queue<IEvent> m_queuedEvents;
		List<EventListener> m_listeners;

		public EventManager(IWindowManager windowManager)
		{
			m_windowManager = windowManager;
			m_windowManager.SetKeyboardEventDelegate(KeyboardEventHandler);

			// create thread
			m_eventThread = new Thread(EventThread);
			m_eventThreadTimer = new Stopwatch();

			// create lock
			m_eventManagerSettingsLock = new ReaderWriterLock();

			// initialize settings
			m_shouldEventManagerAbort = false;

			m_queuedEvents = new Queue<IEvent>();
			m_listeners = new List<EventListener>();

			// start thread
			m_eventThread.Start();
		}

		public void EventThread()
		{
			while (true)
			{
				// Lock
				m_eventManagerSettingsLock.AcquireReaderLock(-1);

				if (m_shouldEventManagerAbort)
				{
					break;
				}

				// Start stopwatch
				m_eventThreadTimer.Start();

				// Unlock
				m_eventManagerSettingsLock.ReleaseReaderLock();

				// poll events
				m_windowManager.PollEvents();

				// Lock
				m_eventManagerSettingsLock.AcquireReaderLock(-1);

				// Handle events
				foreach (IEvent @event in m_queuedEvents)
				{
					foreach (EventListener listener in m_listeners)
					{
						if (listener.filter.HasFlag(@event.Type))
						{
							listener.handler(@event);
						}
					}
				}
				m_queuedEvents.Clear();

				// Unlock
				m_eventManagerSettingsLock.ReleaseReaderLock();

				// Wait until EVENT_THREAD_PERIOD has passed
				float timeLeft = EVENT_THREAD_PERIOD - (m_eventThreadTimer.ElapsedMilliseconds / 1000.0f);
				m_eventThreadTimer.Stop();
				m_eventThreadTimer.Reset();
				Thread.Sleep((int)(timeLeft * 1000));
			}
		}

		public void AddListener(EventListener newListener)
		{
			// Lock
			m_eventManagerSettingsLock.AcquireWriterLock(-1);

			m_listeners.Add(newListener);

			// Unlock
			m_eventManagerSettingsLock.ReleaseWriterLock();
		}

		public void RemoveListener(EventListener listener)
		{
			// Lock
			m_eventManagerSettingsLock.AcquireWriterLock(-1);

			m_listeners.Remove(listener);

			// Unlock
			m_eventManagerSettingsLock.ReleaseWriterLock();
		}

		/* terminates the event manager thread and joins it
		 */
		public void TerminateEventManagerAndJoin()
		{
			// get lock
			m_eventManagerSettingsLock.AcquireWriterLock(-1);

			// tell thread to abort
			m_shouldEventManagerAbort = true;

			// release lock
			m_eventManagerSettingsLock.ReleaseWriterLock();

			// join thread
			m_eventThread.Join();
		}

		public void KeyboardEventHandler(Key key, KeyAction action, List<KeyModifier> modifiers)
		{
			// Lock
			m_eventManagerSettingsLock.AcquireWriterLock(-1);

			KeyboardEvent keyEvent = new KeyboardEvent();
			keyEvent.key = key;
			keyEvent.action = action;
			keyEvent.modifiers = modifiers;
			m_queuedEvents.Enqueue(keyEvent);

			// Unlock
			m_eventManagerSettingsLock.ReleaseWriterLock();
		}
	}
}
