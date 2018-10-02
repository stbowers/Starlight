using System.Collections.Generic;

namespace StarlightEngine.Graphics.Scenes
{
	public class SceneManager
	{
		IRenderer m_renderer;

		Stack<Scene> m_scenes = new Stack<Scene>();

		/* Stack based scene manager, renderer will always display the scene at the top of the stack
		 */
		public SceneManager(IRenderer renderer)
		{
			m_renderer = renderer;
		}

		/* Display the given scene, and push it onto the stack
		 */
		public void PushScene(Scene scene)
		{
			m_scenes.Push(scene);
			m_renderer.DisplayScene(scene);
		}

		/* Pop the top scene off the stack and display the scene below it
		 */
		public void PopScene()
		{
			m_scenes.Pop();
			m_renderer.DisplayScene(m_scenes.Peek());
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
