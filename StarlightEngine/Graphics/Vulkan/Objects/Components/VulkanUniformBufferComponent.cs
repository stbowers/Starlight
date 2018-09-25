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
		VulkanManagedBuffer.VulkanManagedBufferSection m_uniformBufferSection;

		DescriptorSet m_descriptorSet;
		int m_setIndex;
		int m_binding;

		public VulkanUniformBufferComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, byte[] bufferData, VulkanManagedBuffer buffer, DescriptorSet descriptorSet, int setIndex, int binding)
		{
			m_apiManager = apiManager;
			m_pipeline = pipeline;
			m_renderPass = m_pipeline.GetRenderPass();


			m_descriptorSet = descriptorSet;
			m_setIndex = setIndex;
			m_binding = binding;

			m_bufferData = bufferData;
			m_buffer = buffer;
			m_uniformBufferSection = m_buffer.AddSection(m_bufferData.Length, m_bufferData, DescriptorType.UniformBuffer, m_descriptorSet, m_binding);
		}

		public void UpdateUniformBuffer(byte[] newData)
		{
			m_buffer.UpdateSection(m_uniformBufferSection, newData.Length, newData);
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
