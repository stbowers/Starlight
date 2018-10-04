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
			// remove listeners from current top scene
			if (m_scenes.Count > 0){
				foreach ((EventManager.HandleEventDelegate listenerDelegate, EventType type) in m_scenes.Peek().GetEventListeners()){
					m_eventManager.RemoveListener(listenerDelegate);
				}
			}

			// Push new scene on stack
			m_scenes.Push(scene);

			// add new scene's listeners
			foreach ((EventManager.HandleEventDelegate listenerDelegate, EventType type) in scene.GetEventListeners()){
				m_eventManager.AddListener(listenerDelegate, type);
			}

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
			foreach ((EventManager.HandleEventDelegate listenerDelegate, EventType type) in scene.GetEventListeners()){
				m_eventManager.RemoveListener(listenerDelegate);
			}

			if (m_scenes.Count > 0){
				// add listeners for the scene at the top of the stack
				foreach ((EventManager.HandleEventDelegate listenerDelegate, EventType type) in m_scenes.Peek().GetEventListeners()){
					m_eventManager.AddListener(listenerDelegate, type);
				}

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
