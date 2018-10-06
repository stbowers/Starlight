using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Memory;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Components
{
    public class VulkanMeshComponent : IVulkanBindableComponent
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;
        byte[] m_meshData;
        int m_vboOffset;
        int m_iboOffset;
        VulkanManagedBuffer m_buffer;

        RenderPass m_renderPass;
        VulkanManagedBuffer.ManagedBufferSection m_meshSection;

        /* Create a buffer for the mesh data and all uniform buffers. Writes to the out variables the buffer and offset for each uniform passed in, so that the caller can update the descriptor set for them
         */
        public VulkanMeshComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] meshData, int vboOffset, int iboOffset, VulkanManagedBuffer buffer)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_meshData = meshData;
            m_vboOffset = vboOffset;
            m_iboOffset = iboOffset;
            m_buffer = buffer;

            // Get render pass
            m_renderPass = pipeline.GetRenderPass();

            // Create section in buffer for mesh data
            m_meshSection = buffer.AddSection(m_meshData);
        }

        public void UpdateMesh(byte[] newMeshData, int newVBOOffset, int newIBOOffset)
        {
            m_meshData = newMeshData;
            m_vboOffset = newVBOOffset;
            m_iboOffset = newIBOOffset;

            // Update the mesh section of the buffer
            m_buffer.UpdateSection(m_meshSection, m_meshData);

            // Write changes to buffer
            m_buffer.WriteAllBuffers();
        }

        public VulkanPipeline Pipeline
        {
            get
            {
                return m_pipeline;
            }
        }

        public RenderPass RenderPass
        {
            get
            {
                return m_renderPass;
            }
        }

        public void BindComponent(CommandBuffer commandBuffer, int swapchainIndex)
        {
            (VulkanCore.Buffer buffer, int meshOffset) = m_meshSection.GetBindingDetails(swapchainIndex);
            commandBuffer.CmdBindVertexBuffer(buffer, meshOffset + m_vboOffset);
            commandBuffer.CmdBindIndexBuffer(buffer, meshOffset + m_iboOffset);
        }
    }
}
