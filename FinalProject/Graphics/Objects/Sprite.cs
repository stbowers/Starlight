using System;
using Vulkan;

namespace FinalProject.Graphics.Objects
{
	public class Sprite: GraphicsObject
	{
		public Sprite(GlmSharp.vec3 position, string texture)
		{
		}

		void GraphicsObject.DrawVK(Vulkan.VkCommandBuffer primaryBuffer)
		{
			throw new NotImplementedException();
		}
	}
}
