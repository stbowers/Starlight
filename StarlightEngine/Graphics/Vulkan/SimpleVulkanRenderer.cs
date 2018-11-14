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
            commandBuffer.CmdSetScissor(renderPassInfo.RenderArea);
            commandBuffer.CmdEndRenderPass();

            // Render scene
            IGraphicsObject[] sceneObjects = m_currentScene.Children;
            foreach (IGraphicsObject graphicsObject in sceneObjects)
            {
                // Call object's update function
                graphicsObject.Update();

                if (graphicsObject is IVulkanDrawableObject && graphicsObject.Visible)
                {
                    IVulkanDrawableObject drawableObject = graphicsObject as IVulkanDrawableObject;
                    for (int renderPassIndex = 0; renderPassIndex < drawableObject.RenderPasses.Length; renderPassIndex++)
                    {
                        // Start render pass and bind pipeline
                        VulkanPipeline pipeline = drawableObject.Pipelines[renderPassIndex];

                        renderPassInfo.RenderPass = drawableObject.RenderPasses[renderPassIndex];
                        renderPassInfo.Framebuffer = pipeline.GetFramebuffer(currentFrame);

                        renderPassInfo.RenderArea = drawableObject.ClipArea;

                        commandBuffer.CmdBeginRenderPass(renderPassInfo);
                        commandBuffer.CmdSetScissor(renderPassInfo.RenderArea);
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

            renderPassInfo.RenderArea.Offset.X = 0;
            renderPassInfo.RenderArea.Offset.Y = 0;
            renderPassInfo.RenderArea.Extent = m_apiManager.GetSwapchainImageExtent();
            // Render special objects
            if (m_specialObjectFlags.HasFlag(IRendererSpecialObjectFlags.RenderDebugOverlay))
            {
                List<IGraphicsObject> debugOverlayObjects = new List<IGraphicsObject>();
                debugOverlayObjects.Add(m_specialObjectRefs.DebugOverlay);
                if (m_specialObjectRefs.DebugOverlay is IParent)
                {
                    debugOverlayObjects.AddRange((m_specialObjectRefs.DebugOverlay as IParent).Children);
                }
                foreach (IGraphicsObject obj in debugOverlayObjects)
                {
                    if (obj is IVulkanDrawableObject)
                    {
                        if (((IVulkanDrawableObject)obj).Visible)
                        {
                            IVulkanDrawableObject drawableObject = obj as IVulkanDrawableObject;
                            for (int renderPassIndex = 0; renderPassIndex < drawableObject.RenderPasses.Length; renderPassIndex++)
                            {
                                // Start render pass and bind pipeline
                                VulkanPipeline pipeline = drawableObject.Pipelines[renderPassIndex];

                                renderPassInfo.RenderPass = drawableObject.RenderPasses[renderPassIndex];
                                renderPassInfo.Framebuffer = pipeline.GetFramebuffer(currentFrame);

                                commandBuffer.CmdBeginRenderPass(renderPassInfo);
                                commandBuffer.CmdSetScissor(renderPassInfo.RenderArea);
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
