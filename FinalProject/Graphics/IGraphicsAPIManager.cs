using System;
namespace FinalProject.Graphics
{
	/* An interface that any graphics API manager class should implement
	 */
	public interface IGraphicsAPIManager
	{
		void Draw();
		void Present();
	}
}
