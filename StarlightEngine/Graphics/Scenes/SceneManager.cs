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
            // Push new scene on stack
            m_scenes.Push(scene);

            // set event manager to use this scene
            m_eventManager.SetScene(scene);

            // display scene
            m_renderer.DisplayScene(scene);
        }

        /* Pop the top scene off the stack and display the scene below it
		 */
        public void PopScene()
        {
            // pop scene
            Scene scene = m_scenes.Pop();

            if (m_scenes.Count > 0)
            {
                // set event manager to use scene at top of stack
                m_eventManager.SetScene(m_scenes.Peek());

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
