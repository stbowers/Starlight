using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
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
        IWindowManager m_windowManager;

        Stack<Dictionary<string, EventHandler>> m_subscribers = new Stack<Dictionary<string, EventHandler>>();
        Dictionary<string, EventHandler> m_globalSubscribers = new Dictionary<string, EventHandler>();
        public delegate void EventHandler(object sender, IEvent e);

        public EventManager(IWindowManager windowManager)
        {
            windowManager.SetKeyboardEventDelegate(OnKeyboardEvent);
            windowManager.SetMouseEventDelegate(OnMouseEvent);
            m_windowManager = windowManager;
        }

        private void OnKeyboardEvent(Key key, KeyAction action, KeyModifiers modifiers)
        {
            KeyboardEvent keyEvent = new KeyboardEvent(key, action, modifiers);
            Notify(KeyboardEvent.ID, m_windowManager, keyEvent);
        }

        private void OnMouseEvent(MouseButton button, MouseAction action, KeyModifiers modifiers, FVec2 mousePosition, float scrollX, float scrollY)
        {
            MouseEvent mouseEvent = new MouseEvent(button, action, modifiers, mousePosition, scrollX, scrollY);
            Notify(MouseEvent.ID, m_windowManager, mouseEvent);
        }

        public void Subscribe(string id, EventHandler handler)
        {
            if (m_globalSubscribers.ContainsKey(id))
            {
                m_globalSubscribers[id] += handler;
            }
            else
            {
                m_globalSubscribers.Add(id, handler);
            }
        }

        public void PushSubscribers((string, EventHandler)[] subscribers)
        {
            Dictionary<string, EventHandler> newSubscribers = new Dictionary<string, EventHandler>();
            foreach ((string id, EventHandler handler) in subscribers)
            {
                if (newSubscribers.ContainsKey(id))
                {
                    newSubscribers[id] += handler;
                }
                else
                {
                    newSubscribers.Add(id, handler);
                }
            }
            m_subscribers.Push(newSubscribers);
        }

        public void PopSubscribers()
        {
            if (m_subscribers.Count > 0)
            {
                m_subscribers.Pop();
            }
        }

        public void Notify(string id, object sender, IEvent e, float delay = 0.0f)
        {
            Thread.Sleep((int)(delay * 1000));
            if (m_subscribers.Count() != 0 && m_subscribers.Peek().ContainsKey(id))
            {
                ThreadPool.QueueUserWorkItem((object o) =>
                {
                    if (m_globalSubscribers.ContainsKey(id))
                    {

                        m_globalSubscribers[id].Invoke(sender, e);
                    }
                    if (m_subscribers.Peek().ContainsKey(id))
                    {
                        m_subscribers.Peek()[id].Invoke(sender, e);
                    }
                });
            }
        }
    }
}
