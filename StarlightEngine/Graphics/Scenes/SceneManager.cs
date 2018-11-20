using System.Collections.Generic;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Scenes
{
    public class SceneManager
    {
        IRenderer m_renderer;
        EventManager m_eventManager;

        Stack<Scene> m_scenes = new Stack<Scene>();

        /* Stack based scene manager, renderer will always display the scene at the top of the stack
		 */
        public SceneManager(IRenderer renderer, EventManager eventManager)
        {
            m_renderer = renderer;
            m_eventManager = eventManager;
        }

        /* Display the given scene, and push it onto the stack
		 */
        public void PushScene(Scene scene)
        {
            // remove current event subscribers
            m_eventManager.PopSubscribers();

            // Push new scene on stack
            m_scenes.Push(scene);

            // add new scene event subscribers
            m_eventManager.PushSubscribers(scene.GetEventSubscriptions());

            // display scene
            m_renderer.DisplayScene(scene);
        }

        /* Pop the top scene off the stack and display the scene below it
		 */
        public void PopScene()
        {
            // pop scene
            Scene scene = m_scenes.Pop();

            // remove scene's listeners
            m_eventManager.PopSubscribers();

            if (m_scenes.Count > 0)
            {
                // add listeners for the scene at the top of the stack
                m_eventManager.PushSubscribers(m_scenes.Peek().GetEventSubscriptions());

                // display the scene at the top of the stack
                m_renderer.DisplayScene(m_scenes.Peek());
            }
        }

        /// <summary>
        /// Returns the scene currently being displayed
        /// </summary>
        public Scene PeekScene()
        {
            return m_scenes.Peek();
        }
    }
}
