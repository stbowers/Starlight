using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Scenes;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
	public class SimpleVulkanRenderer : IRenderer
	{
		private VulkanAPIManager m_apiManager;
		VulkanPipeline m_clearPipeline;

		Scene m_currentScene;

		// special objects
		IRendererSpecialObjectRefs m_specialObjectRefs;
		IRendererSpecialObjectFlags m_specialObjectFlags;

		public SimpleVulkanRenderer(VulkanAPIManager apiManager, VulkanPipeline clearPipeline)
		{
			m_apiManager = apiManager;
			m_clearPipeline = clearPipeline;
		}

		public void DisplayScene(Scene scene)
		{
			m_currentScene = scene;
		}

		public void SetSpecialObjectReferences(IRendererSpecialObjectRefs refs)
		{
			m_specialObjectRefs = refs;
		}

		public void SetSpecialObjectsFlags(IRendererSpecialObjectFlags flags)
		{
			m_specialObjectFlags = flags;
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

			// Render scene
			foreach (var graphicsObjectList in m_currentScene.GetObjects())
			{
				foreach (IGraphicsObject graphicsObject in graphicsObjectList.Value)
				{
					// Call object's update function
					graphicsObject.Update();

					if (graphicsObject is IVulkanDrawableObject)
					{
						if (!(((IVulkanDrawableObject)graphicsObject).Visible))
						{
							// if the object is not visible, don't draw it
							continue;
						}
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
								binding.BindComponent(commandBuffer, currentFrame);
							}
							drawableObject.Draw(commandBuffer, currentFrame);

							commandBuffer.CmdEndRenderPass();
						}
					}
				}
			}

			// Render special objects
			if (m_specialObjectFlags.HasFlag(IRendererSpecialObjectFlags.RenderFPSCounter))
			{
				if (m_specialObjectRefs.fpsCounter is IVulkanDrawableObject)
				{
					if (((IVulkanDrawableObject)m_specialObjectRefs.fpsCounter).Visible)
					{
						IVulkanDrawableObject drawableObject = m_specialObjectRefs.fpsCounter as IVulkanDrawableObject;
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
								binding.BindComponent(commandBuffer, currentFrame);
							}
							drawableObject.Draw(commandBuffer, currentFrame);

							commandBuffer.CmdEndRenderPass();
						}
					}
				}
			}
			if (m_specialObjectFlags.HasFlag(IRendererSpecialObjectFlags.RenderMousePositionCounter))
			{
				if (m_specialObjectRefs.mousePositionCounter is IVulkanDrawableObject)
				{
					if (((IVulkanDrawableObject)m_specialObjectRefs.mousePositionCounter).Visible)
					{
						IVulkanDrawableObject drawableObject = m_specialObjectRefs.mousePositionCounter as IVulkanDrawableObject;
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
								binding.BindComponent(commandBuffer, currentFrame);
							}
							drawableObject.Draw(commandBuffer, renderPassIndex);

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
