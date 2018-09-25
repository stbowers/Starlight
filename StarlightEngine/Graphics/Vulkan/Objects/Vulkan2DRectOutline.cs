using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Vulkan.Memory;
using StarlightEngine.Graphics.Math;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class Vulkan2DRectOutline: IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;

		RenderPass[] m_renderPasses;
		VulkanPipeline[] m_pipelines;

		byte[] m_meshData;
		byte[] m_mvpData;
		int m_numIndices;

		VulkanManagedBuffer m_objectBuffer;

		DescriptorSet m_mvpDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanUniformBufferComponent m_mvpUniform;
		IVulkanBindableComponent[][] m_components;

		public Vulkan2DRectOutline(VulkanAPIManager apiManager, FVec2 position, FVec2 size, FVec4 color)
		{
			m_apiManager = apiManager;
			m_pipeline = StaticPipelines.pipeline_colorLine;

			m_renderPasses = new RenderPass[] { m_pipeline.GetRenderPass() };
			m_pipelines = new VulkanPipeline[] { m_pipeline };

			this.Visible = true;

			// Create mesh data
			FVec2 topLeft = new FVec2(position.X, position.Y);
			FVec2 topRight = new FVec2(position.X + size.X, position.Y);
			FVec2 bottomLeft = new FVec2(position.X, position.Y + size.Y);
			FVec2 bottomRight = new FVec2(position.X + size.X, position.Y + size.Y);
			int[] indices = { 0, 1, 1, 3, 3, 2, 2, 0 };
			m_numIndices = 8;

			m_meshData = new byte[(4 * 2 * 4) + (4 * 4 * 4) + (8 * 4)];
			System.Buffer.BlockCopy(topLeft.Bytes, 0, m_meshData, 0, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_meshData, 8, 16);
			System.Buffer.BlockCopy(topRight.Bytes, 0, m_meshData, 24, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_meshData, 32, 16);
			System.Buffer.BlockCopy(bottomLeft.Bytes, 0, m_meshData, 48, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_meshData, 56, 16);
			System.Buffer.BlockCopy(bottomRight.Bytes, 0, m_meshData, 72, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_meshData, 80, 16);
			System.Buffer.BlockCopy(indices, 0, m_meshData, 96, 8 * 4);

			// Create mvp data
			FMat4 mvp = new FMat4(1.0f);
			float depth = 0.0f;

			m_mvpData = new byte[(1 * 4 * 4 * 4) + (1 * 4)];
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			m_objectBuffer = new VulkanManagedBuffer(m_apiManager, bufferAlignment, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.None, MemoryProperties.DeviceLocal);

			// Allocate descriptor sets
			m_mvpDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(0, 1)[0];

			// Create mesh component
			m_mesh = new VulkanMeshComponent(apiManager, m_pipeline, m_meshData, 0, m_meshData.Length - (8 * 4), m_objectBuffer);

			// Create mvp uniform component
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_mvpDescriptorSet, 0, 0);

			m_objectBuffer.WriteBuffer();

			m_components = new[] { new IVulkanBindableComponent[] { m_mesh, m_mvpUniform } };
		}

		public void Update()
		{
		}

		public RenderPass[] RenderPasses
		{
			get
			{
				return m_renderPasses;
			}
		}

		public VulkanPipeline[] Pipelines
		{
			get
			{
				return m_pipelines;
			}
		}

		public IVulkanBindableComponent[][] BindableComponents
		{
			get
			{
				return m_components;
			}
		}

		public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets, int renderPassIndex)
		{
			commandBuffer.CmdDrawIndexed(m_numIndices);
		}

		public bool Visible { get; set; }
	}
}
