using System;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics
{
	/* Classes implementing this interface can be used to render IGraphicsObjects
	 */
	public interface IRenderer
	{
		// add an object to be rendered
		void AddObject(int layer, IGraphicsObject obj);
		// remove an object from this renderer
		void RemoveObject(int layer, IGraphicsObject obj);

		// Update objects and/or caches to speed up rendering
		void Update();

		// Render all objects
		void Render();

		// Present the rendered image
		void Present();
	}
}
