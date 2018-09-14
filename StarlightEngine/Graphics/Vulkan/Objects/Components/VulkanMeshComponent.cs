using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Components
{
    public class VulkanMeshComponent : IVulkanBindableComponent
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;
        RenderPass m_renderPass;

		ReallocateBuffer m_reallocateDelegate;

        byte[] m_meshData;
        int m_vboOffset;
        int m_iboOffset;

        VulkanCore.Buffer m_meshBuffer;
        VmaAllocation m_meshBufferAllocation;
		int m_meshBufferOffset;

        /* Create a buffer for the mesh data and all uniform buffers. Writes to the out variables the buffer and offset for each uniform passed in, so that the caller can update the descriptor set for them
         */
		public VulkanMeshComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, ReallocateBuffer reallocateDelegate, byte[] meshData, int vboOffset, int iboOffset, VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int bufferOffset)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_renderPass = pipeline.GetRenderPass();

			m_reallocateDelegate = reallocateDelegate;

            m_meshData = meshData;
			m_vboOffset = vboOffset;
			m_iboOffset = iboOffset;

			m_meshBuffer = buffer;
			m_meshBufferAllocation = bufferAllocation;
			m_meshBufferOffset = bufferOffset;

			// Copy data to buffer
            IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
			Marshal.Copy(m_meshData, 0, (mappedMemory + m_meshBufferOffset), m_meshData.Length);
            m_meshBufferAllocation.memory.Unmap();
        }

		public void UpdateMesh(byte[] newMeshData, int newVBOOffset, int newIBOOffset)
		{
			if (m_meshData.Length != newMeshData.Length)
			{
				m_meshData = newMeshData;
				m_reallocateDelegate(m_meshBuffer, m_meshBufferAllocation, newMeshData.Length);
			}
			else
			{
				m_meshData = newMeshData;
			}

			m_vboOffset = newVBOOffset;
			m_iboOffset = newIBOOffset;

			// Copy data to buffer
			IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
			Marshal.Copy(m_meshData, 0, (mappedMemory + m_meshBufferOffset), m_meshData.Length);
			m_meshBufferAllocation.memory.Unmap();
		}

		public delegate void ReallocateBuffer(VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int newSize);

		// Move where the buffer is stored, and re-copy data to it
		public void ChangeBuffer(VulkanCore.Buffer newBuffer, VmaAllocation newBufferAllocation, int newBufferOffset)
		{
			m_meshBuffer = newBuffer;
			m_meshBufferAllocation = newBufferAllocation;
			m_meshBufferOffset = newBufferOffset;

			// Copy data to buffer
			IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
			Marshal.Copy(m_meshData, 0, (mappedMemory + m_meshBufferOffset), m_meshData.Length);
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
			commandBuffer.CmdBindVertexBuffer(m_meshBuffer, m_meshBufferOffset + m_vboOffset);
            commandBuffer.CmdBindIndexBuffer(m_meshBuffer, m_meshBufferOffset + m_iboOffset);
        }
    }
}
