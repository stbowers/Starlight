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
        VulkanManagedBuffer m_buffer;

        RenderPass m_renderPass;
        VulkanManagedBuffer.ManagedBufferSection m_meshSection;

        /* Create a buffer for the mesh data and all uniform buffers. Writes to the out variables the buffer and offset for each uniform passed in, so that the caller can update the descriptor set for them
         */
        public VulkanMeshComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] meshData, int vboOffset, int iboOffset, int numIndices, VulkanManagedBuffer buffer)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_meshData = meshData;
            m_buffer = buffer;

            // Get render pass
            m_renderPass = pipeline.GetRenderPass();

            // Create section in buffer for mesh data
            m_meshSection = buffer.AddSection(m_meshData);
            m_meshSection.SetUserData((vboOffset, iboOffset, numIndices));
        }

        public void UpdateMesh(byte[] newMeshData, int newVBOOffset, int newIBOOffset, int newNumIndices)
        {
            m_meshData = newMeshData;

            // Update the mesh section of the buffer
            m_buffer.UpdateSection(m_meshSection, m_meshData);
            m_meshSection.SetUserData((newVBOOffset, newIBOOffset, newNumIndices));

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
            (int vboOffset, int iboOffset, int numIndices) = (ValueTuple<int, int, int>)m_meshSection.GetUserData(swapchainIndex);
            commandBuffer.CmdBindVertexBuffer(buffer, meshOffset + vboOffset);
            commandBuffer.CmdBindIndexBuffer(buffer, meshOffset + iboOffset);
        }

        /// <summary>
        /// Helper method to draw mesh with correct number of verticies for the bound buffer (may change across updates)
        /// </summary>
        public void DrawMesh(CommandBuffer commandBuffer, int swapchainIndex)
        {
            // number of indices is stored as user data in the raw buffer section
            (int vboOffset, int iboOffset, int numIndices) = (ValueTuple<int, int, int>)m_meshSection.GetUserData(swapchainIndex);
            commandBuffer.CmdDrawIndexed(numIndices);
        }
    }
}
