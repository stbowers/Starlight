using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using VulkanCore;

namespace StarlightEngine
{
	/* A component used to manage the memory and descriptor for a uniform buffer
	 */
	public class VulkanUniformBufferComponent: IVulkanBindableComponent
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;
		RenderPass m_renderPass;

		byte[] m_bufferData;
		VulkanCore.Buffer m_buffer;
		VmaAllocation m_bufferAllocation;
		int m_bufferOffset;

		DescriptorSet m_descriptorSet;
		int m_setIndex;
		int m_binding;

		public VulkanUniformBufferComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] bufferData, VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int bufferOffset, DescriptorSet descriptorSet, int setIndex, int binding)
		{
			m_apiManager = apiManager;
			m_pipeline = pipeline;
			m_renderPass = m_pipeline.GetRenderPass();

			m_bufferData = bufferData;
			m_buffer = buffer;
			m_bufferAllocation = bufferAllocation;
			m_bufferOffset = bufferOffset;

			m_descriptorSet = descriptorSet;
			m_setIndex = setIndex;
			m_binding = binding;

			// Copy data to buffer
			IntPtr mappedMemory = m_bufferAllocation.memory.Map(m_bufferAllocation.offset, m_bufferAllocation.size);
			Marshal.Copy(m_bufferData, 0, mappedMemory + m_bufferOffset, m_bufferData.Length);
			m_bufferAllocation.memory.Unmap();

			// Update descriptor
			DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
			bufferInfo.Buffer = m_buffer;
			bufferInfo.Offset = m_bufferOffset;
			bufferInfo.Range = m_bufferData.Length;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = m_descriptorSet;
			descriptorWrite.DstBinding = m_binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			descriptorWrite.BufferInfo = new[] { bufferInfo };

			m_descriptorSet.Parent.UpdateSets(new[] { descriptorWrite });
		}

		public void UpdateUniformBuffer(byte[] newData)
		{
			if (m_bufferData.Length != newData.Length)
			{
				throw new ApplicationException("Updated uniform data must be the same length");
			}

			// Update data
			m_bufferData = newData;

			// Copy data to buffer
			IntPtr mappedMemory = m_bufferAllocation.memory.Map(m_bufferAllocation.offset, m_bufferAllocation.size);
			Marshal.Copy(m_bufferData, 0, mappedMemory + m_bufferOffset, m_bufferData.Length);
			m_bufferAllocation.memory.Unmap();

			// Update descriptor
			DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
			bufferInfo.Buffer = m_buffer;
			bufferInfo.Offset = m_bufferOffset;
			bufferInfo.Range = m_bufferData.Length;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = m_descriptorSet;
			descriptorWrite.DstBinding = m_binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			descriptorWrite.BufferInfo = new[] { bufferInfo };

			m_apiManager.GetDevice().WaitIdle();
			m_descriptorSet.Parent.UpdateSets(new[] { descriptorWrite });
		}

		// Move where the buffer is stored, and re-copy data to it
		public void ChangeBuffer(VulkanCore.Buffer newBuffer, VmaAllocation newBufferAllocation, int newBufferOffset)
		{
			m_buffer = newBuffer;
			m_bufferAllocation = newBufferAllocation;
			m_bufferOffset = newBufferOffset;

			// Copy data to buffer
			IntPtr mappedMemory = m_bufferAllocation.memory.Map(m_bufferAllocation.offset, m_bufferAllocation.size);
			Marshal.Copy(m_bufferData, 0, mappedMemory, m_bufferData.Length);
			m_bufferAllocation.memory.Unmap();
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
			if (!boundSets.Contains(m_setIndex))
			{
				commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, boundPipeline.GetPipelineLayout(), m_setIndex, new[] { m_descriptorSet });
				boundSets.Add(m_setIndex);
			}
		}
	}
}
