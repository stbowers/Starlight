using System;
using FinalProject.Graphics.Objects;
namespace FinalProject.Graphics
{
	public interface Renderer
	{
		void AddObject(int layer, GraphicsObject obj);
		void Update();
	}
}
