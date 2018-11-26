using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using StarlightEngine.Graphics;
using StarlightEngine.Math;
using StarlightEngine.Threadding;
using StarlightEngine.Graphics.Scenes;

namespace StarlightEngine.Events
{
    /// <summary>
    /// Manages events for the engine
    /// </summary>
    public class EventManager
    {
        IWindowManager m_windowManager;
        Scene m_currentScene;

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

        public void SetScene(Scene currentScene)
        {
            m_currentScene = currentScene;
        }

        public void Notify(string id, object sender, IEvent e, float delay = 0.0f)
        {
            EventHandler[] callbacks =
            (
                from pair in m_globalSubscribers
                let subscription = (pair.Key, pair.Value)
                where subscription.Item1 == id
                select subscription.Item2
            ).Union(
                from subscription in m_currentScene.GetEventSubscriptions()
                where subscription.Item1 == id
                select subscription.Item2
            ).ToArray();
            if (callbacks.Length > 0)
            {
                ThreadPool.QueueUserWorkItem((object o) =>
                {
                    Thread.Sleep((int)(delay * 1000));
                    foreach (EventHandler callback in callbacks)
                    {
                        callback.Invoke(sender, e);
                    }
                });
            }
        }
    }
}
