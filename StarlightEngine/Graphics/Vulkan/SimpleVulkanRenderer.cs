using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
	public class SimpleVulkanRenderer : IRenderer
	{
		private VulkanAPIManager m_apiManager;
		private SortedDictionary<int, List<IVulkanObject>> objects = new SortedDictionary<int, List<IVulkanObject>>();
		VulkanPipeline m_clearPipeline;

		public SimpleVulkanRenderer(VulkanAPIManager apiManager, VulkanPipeline clearPipeline)
		{
			m_apiManager = apiManager;
			m_clearPipeline = clearPipeline;
		}

		public void AddObject(int layer, IGraphicsObject obj)
		{
			// make sure the object is a Vulkan object
			if (!(obj is IVulkanObject))
			{
				throw new ArgumentException();
			}
			IVulkanObject vulkanObject = (IVulkanObject)obj;
			List<IVulkanObject> list;
			if (objects.ContainsKey(layer))
			{
				list = objects[layer];
			}
			else
			{
				list = new List<IVulkanObject>();
				objects.Add(layer, list);
			}

			list.Add(vulkanObject);
		}

		public void RemoveObject(int layer, IGraphicsObject obj)
		{
			// make sure the object is a Vulkan object
			if (!(obj is IVulkanObject))
			{
				// silently fail if obj is not a Vulkan object (effect is same, since obj won't be in our collection anyway)
				return;
			}
			IVulkanObject vulkanObject = (IVulkanObject)obj;

			if (objects.ContainsKey(layer))
			{
				objects[layer].Remove(vulkanObject);
			}
		}

		public void Update()
		{
			int currentFrame;
			CommandBuffer commandBuffer = m_apiManager.StartRecordingSwapchainCommandBuffer(out currentFrame);

			RenderPassBeginInfo renderPassInfo = new RenderPassBeginInfo();
			renderPassInfo.RenderPass = m_clearPipeline.GetRenderPass();
			renderPassInfo.Framebuffer = m_clearPipeline.GetFramebuffer(currentFrame);
			renderPassInfo.RenderArea = new Rect2D();
			renderPassInfo.RenderArea.Offset = new Offset2D();
			renderPassInfo.RenderArea.Offset.X = 0;
			renderPassInfo.RenderArea.Offset.Y = 0;
			renderPassInfo.RenderArea.Extent = m_apiManager.GetSwapchainImageExtent();

			ClearValue colorClearValue = new ClearValue();
			colorClearValue.Color = new ClearColorValue();
			colorClearValue.Color.Float4.R = 0.0f;
			colorClearValue.Color.Float4.G = 0.0f;
			colorClearValue.Color.Float4.B = 0.0f;
			colorClearValue.Color.Float4.A = 1.0f;

			ClearValue depthClearValue = new ClearValue();
			depthClearValue.DepthStencil = new ClearDepthStencilValue();
			depthClearValue.DepthStencil.Depth = 1.0f;

			renderPassInfo.ClearValues = new[] { colorClearValue, depthClearValue };

			commandBuffer.CmdBeginRenderPass(renderPassInfo);
			commandBuffer.CmdEndRenderPass();

			foreach (var graphicsObjectList in objects)
			{
				foreach (IVulkanObject graphicsObject in graphicsObjectList.Value)
				{
					// Call object's update function
					graphicsObject.Update();

					if (graphicsObject is IVulkanDrawableObject)
					{
						IVulkanDrawableObject drawableObject = graphicsObject as IVulkanDrawableObject;
						for (int renderPassIndex = 0; renderPassIndex < drawableObject.RenderPasses.Length; renderPassIndex++)
						{
							// Start render pass and bind pipeline
							VulkanPipeline pipeline = drawableObject.Pipelines[renderPassIndex];

							renderPassInfo.RenderPass = drawableObject.RenderPasses[renderPassIndex];
							renderPassInfo.Framebuffer = pipeline.GetFramebuffer(currentFrame);

							commandBuffer.CmdBeginRenderPass(renderPassInfo);
							commandBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline.GetPipeline());

							IVulkanBindableComponent[] bindings = drawableObject.BindableComponents[renderPassIndex];
							List<int> boundSets = new List<int>();
							foreach (IVulkanBindableComponent binding in bindings)
							{
								binding.BindComponent(commandBuffer, pipeline, pipeline.GetRenderPass(), boundSets);
							}
							drawableObject.Draw(commandBuffer, pipeline, pipeline.GetRenderPass(), boundSets, renderPassIndex);

							commandBuffer.CmdEndRenderPass();
						}
					}
				}
			}

			m_apiManager.FinalizeSwapchainCommandBuffer(commandBuffer);
		}

		public void Render()
		{
			m_apiManager.Draw();
		}

		public void Present()
		{
			m_apiManager.Present();
		}
	}
}
