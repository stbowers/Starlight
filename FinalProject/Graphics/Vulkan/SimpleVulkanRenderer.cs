using System;
using System.Collections.Generic;
using FinalProject.Graphics.Objects;
using FinalProject.Graphics.Vulkan.Objects;
using VulkanCore;

namespace FinalProject.Graphics.Vulkan
{
	public class SimpleVulkanRenderer : IRenderer
	{
		private VulkanAPIManager m_apiManager;
		private SortedDictionary<int, List<IVulkanObject>> objects = new SortedDictionary<int, List<IVulkanObject>>();

		public SimpleVulkanRenderer(VulkanAPIManager apiManager)
		{
			m_apiManager = apiManager;
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

			foreach (var graphicsObjectList in objects)
			{
				foreach (IVulkanObject graphicsObject in graphicsObjectList.Value)
				{
					// Start render pass and bind pipeline
					VulkanPipeline pipeline = graphicsObject.Pipeline;
					RenderPassBeginInfo renderPassInfo = new RenderPassBeginInfo();
					renderPassInfo.RenderPass = pipeline.GetRenderPass();
					renderPassInfo.Framebuffer = pipeline.GetFramebuffer(currentFrame);
					renderPassInfo.RenderArea = new Rect2D();
					renderPassInfo.RenderArea.Offset = new Offset2D();
					renderPassInfo.RenderArea.Offset.X = 0;
					renderPassInfo.RenderArea.Offset.Y = 0;
					renderPassInfo.RenderArea.Extent = m_apiManager.GetSwapchainImageExtent();

					ClearValue colorClearValue = new ClearValue();
					colorClearValue.Color = new ClearColorValue();
					colorClearValue.Color.Float4.R = 0.0f;
					colorClearValue.Color.Float4.G = 0.0f;
					colorClearValue.Color.Float4.B = 1.0f;
					colorClearValue.Color.Float4.A = 1.0f;

					ClearValue depthClearValue = new ClearValue();
					depthClearValue.DepthStencil = new ClearDepthStencilValue();
					depthClearValue.DepthStencil.Depth = 1.0f;

					renderPassInfo.ClearValues = new[] { colorClearValue, depthClearValue };

					commandBuffer.CmdBeginRenderPass(renderPassInfo);
					commandBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline.GetPipeline());

					if (graphicsObject is IComponentVulkanMaterial)
					{
						IComponentExplicitVulkanMaterial material = (graphicsObject as IComponentVulkanMaterial).ExplicitMaterial;
						commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, graphicsObject.Pipeline.GetPipelineLayout(), material.DescriptorSetIndex, new[] { material.DescriptorSet });
					}

					if (graphicsObject is IComponentVulkanMesh)
					{
						IComponentExplicitVulkanMesh mesh = (graphicsObject as IComponentVulkanMesh).ExplicitMesh;
						commandBuffer.CmdBindVertexBuffer(mesh.MeshBuffer, mesh.VBOOffset);
						commandBuffer.CmdBindIndexBuffer(mesh.MeshBuffer, mesh.IBOOffset);
						commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, graphicsObject.Pipeline.GetPipelineLayout(), mesh.UBODescriptorSetIndex, new[] { mesh.UBODescriptorSet });
						commandBuffer.CmdDrawIndexed(mesh.NumVertices);
					}

					commandBuffer.CmdEndRenderPass();
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
