using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Scenes;

namespace StarlightEngine.Graphics
{
	/* Classes implementing this interface can be used to render IGraphicsObjects
	 */
	public interface IRenderer
	{
		// display a scene
		void DisplayScene(Scene scene);

		// Update objects and/or caches to speed up rendering
		void Update();

		// Render all objects
		void Render();

		// Present the rendered image
		void Present();
	}
}
