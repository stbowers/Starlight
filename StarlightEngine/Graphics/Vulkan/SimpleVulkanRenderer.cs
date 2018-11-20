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
        int m_lastSceneHashCode;
        bool m_sceneInvalidated;
        IGraphicsObject[] m_sceneGraphicsObjects;
        VulkanCore.CommandBuffer m_sceneCommands;

        // special objects
        IRendererSpecialObjectRefs m_specialObjectRefs;
        IRendererSpecialObjectFlags m_specialObjectFlags;

        public SimpleVulkanRenderer(VulkanAPIManager apiManager, VulkanPipeline clearPipeline)
        {
            m_apiManager = apiManager;
            m_clearPipeline = clearPipeline;
            //m_sceneCommands = m_apiManager.CreateGraphicsCommandBuffers(1, CommandBufferLevel.Secondary)[0];
        }

        public void DisplayScene(Scene scene)
        {
            m_currentScene = scene;
            m_sceneInvalidated = true;
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

            // Create render pass info
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

            // Clear the screen
            commandBuffer.CmdBeginRenderPass(renderPassInfo);
            commandBuffer.CmdSetScissor(renderPassInfo.RenderArea);
            commandBuffer.CmdEndRenderPass();

            // Update scene
            m_currentScene.Update();

            // if the scene changed, rerecord buffers
            m_sceneInvalidated = true;
            if (m_lastSceneHashCode != m_currentScene.GetHashCode() || m_sceneInvalidated)
            {
                m_lastSceneHashCode = m_currentScene.GetHashCode();
                m_sceneInvalidated = true;

                // TODO: This is a bit of a hack since we can't start a render pass in secondary buffers, so therefore the secondary scene commands buffer is never actually used
                m_sceneCommands = commandBuffer;

                // Get new list of scene objects
                m_sceneGraphicsObjects = m_currentScene.GetChildren<IGraphicsObject>();

                // Render scene
                foreach (IGraphicsObject graphicsObject in m_sceneGraphicsObjects)
                {
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

                            m_sceneCommands.CmdBeginRenderPass(renderPassInfo);
                            m_sceneCommands.CmdSetScissor(renderPassInfo.RenderArea);
                            m_sceneCommands.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline.GetPipeline());

                            IVulkanBindableComponent[] bindings = drawableObject.BindableComponents[renderPassIndex];
                            List<int> boundSets = new List<int>();
                            foreach (IVulkanBindableComponent binding in bindings)
                            {
                                binding.BindComponent(m_sceneCommands, currentFrame);
                            }
                            drawableObject.Draw(m_sceneCommands, currentFrame);

                            m_sceneCommands.CmdEndRenderPass();
                        }
                    }
                }
            }

            // Call scene command buffer
            //commandBuffer.CmdExecuteCommand(m_sceneCommands);

            renderPassInfo.RenderArea.Offset.X = 0;
            renderPassInfo.RenderArea.Offset.Y = 0;
            renderPassInfo.RenderArea.Extent = m_apiManager.GetSwapchainImageExtent();
            // Render special objects
            if (m_specialObjectFlags.HasFlag(IRendererSpecialObjectFlags.RenderDebugOverlay))
            {
                List<IGraphicsObject> debugOverlayObjects = new List<IGraphicsObject>();
                if (m_specialObjectRefs.DebugOverlay is IGraphicsObject)
                {
                    debugOverlayObjects.Add(m_specialObjectRefs.DebugOverlay as IGraphicsObject);
                }
                if (m_specialObjectRefs.DebugOverlay is IParent)
                {
                    debugOverlayObjects.AddRange((m_specialObjectRefs.DebugOverlay as IParent).GetChildren<IGraphicsObject>());
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
