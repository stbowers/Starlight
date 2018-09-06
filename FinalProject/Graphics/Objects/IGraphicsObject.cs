using System;
namespace FinalProject.Graphics.Objects
{
	/* Interface used to signal that a class can be rendered as a graphics object by any class implementing the IRenderer interface
	 */
	public interface IGraphicsObject
	{
		void Update();
	}
}
