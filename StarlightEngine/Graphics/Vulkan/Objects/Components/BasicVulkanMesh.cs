using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Components
{
    public class BasicVulkanMesh : IVulkanBindableComponent
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;
        RenderPass m_renderPass;

        byte[] m_meshData;
        int m_vboOffset;
        int m_iboOffset;

        DescriptorSet[] m_uniformSets;
        int[] m_uniformSetIndices;

        VulkanCore.Buffer m_meshBuffer;
        VmaAllocation m_meshBufferAllocation;

        /* Create a buffer for the mesh data and all uniform buffers. Writes to the out variables the buffer and offset for each uniform passed in, so that the caller can update the descriptor set for them
         */
        public BasicVulkanMesh(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] meshData, int vboOffset, int iboOffset, byte[][] uniformDatas, DescriptorSet[] uniformSets, int[] uniformSetIndices, out VulkanCore.Buffer uniformBuffer, out VmaAllocation uniformBufferAllocation, out int[] uniformOffsets)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_renderPass = pipeline.GetRenderPass();

            m_meshData = meshData;
            m_vboOffset = vboOffset;
            m_iboOffset = iboOffset;

            m_uniformSets = uniformSets;
            m_uniformSetIndices = uniformSetIndices;

            // calculate size and offsets for uniforms (must be properly aligned)
            int uboAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;

            uniformOffsets = new int[uniformDatas.Length];
            int bufferSize = meshData.Length;
            int offsetIndex = 0;
            foreach (byte[] uniformData in uniformDatas)
            {
                int padding = uboAlignment - (bufferSize % uboAlignment);
                int uniformSize = uniformData.Length;
                int uniformOffset = bufferSize + padding;

                uniformOffsets[offsetIndex] = uniformOffset;
                bufferSize += padding + uniformSize;
                offsetIndex++;
            }

            byte[] meshBufferData = new byte[bufferSize];
            m_apiManager.CreateBuffer(bufferSize, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.HostVisible, MemoryProperties.DeviceLocal, out m_meshBuffer, out m_meshBufferAllocation);

            uniformBuffer = m_meshBuffer;
            uniformBufferAllocation = m_meshBufferAllocation;

            System.Buffer.BlockCopy(meshData, 0, meshBufferData, 0, meshData.Length);
            offsetIndex = 0;
            foreach (byte[] uniformData in uniformDatas)
            {
                System.Buffer.BlockCopy(uniformData, 0, meshBufferData, uniformOffsets[offsetIndex], uniformData.Length);
                offsetIndex++;
            }

            IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
            Marshal.Copy(meshBufferData, 0, mappedMemory, (int)bufferSize);
            m_meshBufferAllocation.memory.Unmap();
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

        public void BindComponent(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets)
        {
            commandBuffer.CmdBindVertexBuffer(m_meshBuffer, m_vboOffset);
            commandBuffer.CmdBindIndexBuffer(m_meshBuffer, m_iboOffset);
            for (int i = 0; i < m_uniformSets.Length; i++)
            {
                if (!boundSets.Contains(m_uniformSetIndices[i]))
                {
                    commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, boundPipeline.GetPipelineLayout(), m_uniformSetIndices[i], new[] { m_uniformSets[i] });
                    boundSets.Add(m_uniformSetIndices[i]);
                }
            }
        }
    }
}
