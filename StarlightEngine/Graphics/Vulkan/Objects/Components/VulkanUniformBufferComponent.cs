using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Memory;
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
		VulkanManagedBuffer m_buffer;
		VulkanManagedBuffer.ManagedBufferSection m_uniformBufferSection;

		VulkanDescriptorSet m_descriptorSet;
		int m_binding;

		public VulkanUniformBufferComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] bufferData, VulkanManagedBuffer buffer, VulkanDescriptorSet descriptorSet, int binding)
		{
			m_apiManager = apiManager;
			m_pipeline = pipeline;
			m_renderPass = m_pipeline.GetRenderPass();


			m_descriptorSet = descriptorSet;
			m_binding = binding;

			m_bufferData = bufferData;
			m_buffer = buffer;
			m_uniformBufferSection = m_buffer.AddSection(m_bufferData, DescriptorType.UniformBuffer, m_descriptorSet, m_binding);
		}

		public void UpdateUniformBuffer(byte[] newData)
		{
			m_buffer.UpdateSection(m_uniformBufferSection, newData);

			m_buffer.WriteAllBuffers(false);
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
			commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, m_pipeline.GetPipelineLayout(), m_descriptorSet.GetSetIndex(), new[] { m_descriptorSet.GetSet(swapchainIndex) });
		}
	}
}
