using System;
using System.Collections.Generic;
using FinalProject.Graphics.Objects;
using FinalProject.Graphics;
using FinalProject.Graphics.VK;
namespace FinalProject.Graphics.SimpleRenderer
{
	public class SimpleVulkanRenderer: Renderer
	{
		VulkanDriver driver;

		// list of objects to render, grouped and sorted by layer
		private SortedDictionary<int, List<GraphicsObject>> objects = new SortedDictionary<int, List<GraphicsObject>>();

		public SimpleVulkanRenderer(VulkanDriver driver)
		{
			this.driver = driver;
		}

		void Renderer.AddObject(int layer, GraphicsObject obj)
		{
			if (!objects.ContainsKey(layer))
			{
				objects.Add(layer, new List<GraphicsObject>());
			}

			objects[layer].Add(obj);
		}

		void Renderer.Update()
		{
			Vulkan.VkCommandBuffer renderCommandBuffer = driver.RecordRenderCommandBuffer();

			foreach (List<GraphicsObject> layerObjects in objects.Values)
			{
				foreach (GraphicsObject graphicsObject in layerObjects)
				{
					// Draw object
					graphicsObject.DrawVK(renderCommandBuffer);
				}
			}

			driver.FinalizeRenderCommandBuffer();
		}
	}
}
