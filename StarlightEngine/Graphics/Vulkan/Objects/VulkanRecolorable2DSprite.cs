using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanRecolorable2DSprite : IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;

		byte[] m_meshData;
		byte[] m_mvpData;
        byte[] m_recolorData;
		int m_numIndices;

		VulkanCore.Buffer m_objectBuffer;
		VmaAllocation m_objectBufferAllocation;
		int m_meshDataOffset;
		int m_mvpDataOffset;
        int m_recolorDataOffset;

		DescriptorSet m_meshDescriptorSet;
		DescriptorSet m_materialDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanTextureComponent m_texture;
		VulkanUniformBufferComponent m_mvpUniform;
        VulkanUniformBufferComponent m_recolorUniform;
		IVulkanBindableComponent[] m_bindableComponents;

		public VulkanRecolorable2DSprite(VulkanAPIManager apiManager, string textureFile, FVec2 position, FVec2 scale, FVec4 fromColor1, FVec4 toColor1, FVec4 fromColor2, FVec4 toColor2)
		{
			m_apiManager = apiManager;
			m_pipeline = StaticPipelines.pipeline_recolor2D;

			this.Visible = true;

			// Create mesh data
			FVec4 topLeft = new FVec4(position.X, position.Y, 0.0f, 0.0f);
			FVec4 topRight = new FVec4(position.X + scale.X, position.Y, 1.0f, 0.0f);
			FVec4 bottomLeft = new FVec4(position.X, position.Y + scale.Y, 0.0f, 1.0f);
			FVec4 bottomRight = new FVec4(position.X + scale.X, position.Y + scale.Y, 1.0f, 1.0f);
			int[] indices = { 0, 1, 3, 3, 2, 0 };
			m_numIndices = 6;

			m_meshData = new byte[(4 * 4 * 4) + (6 * 4)];
			System.Buffer.BlockCopy(topLeft.Bytes, 0, m_meshData, 0, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(topRight.Bytes, 0, m_meshData, (int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(bottomLeft.Bytes, 0, m_meshData, 2*(int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(bottomRight.Bytes, 0, m_meshData, 3*(int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(indices, 0, m_meshData, 4*(int)topLeft.PrimativeSizeOf, 6 * 4);

			// Create mvp data
			FMat4 mvp = new FMat4(1.0f);
			float depth = 1.0f;

			m_mvpData = new byte[(1 * 4 * 4 * 4) + (1 * 4)];
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);

            // Create recolor data
            m_recolorData = new byte[4 * 4 * 4];
            System.Buffer.BlockCopy(fromColor1.Bytes, 0, m_recolorData, 0, 4 * 4);
            System.Buffer.BlockCopy(toColor1.Bytes, 0, m_recolorData, 1 * (4 * 4), 4 * 4);
            System.Buffer.BlockCopy(fromColor2.Bytes, 0, m_recolorData, 2 * (4 * 4), 4 * 4);
            System.Buffer.BlockCopy(toColor2.Bytes, 0, m_recolorData, 3 * (4 * 4), 4 * 4);

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int meshDataSize = m_meshData.Length;
			int mvpDataSize = m_mvpData.Length;
            int recolorDataSize = m_recolorData.Length;
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { meshDataSize, mvpDataSize, recolorDataSize },
				bufferAlignment,
				BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer,
				MemoryProperties.HostVisible,
				MemoryProperties.DeviceLocal,
				out m_objectBuffer,
				out m_objectBufferAllocation,
				out objectBufferOffsets
			);
			m_meshDataOffset = objectBufferOffsets[0];
			m_mvpDataOffset = objectBufferOffsets[1];
            m_recolorDataOffset = objectBufferOffsets[2];

			// Create descriptor sets
			m_meshDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(0, 1)[0];
			m_materialDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(1, 1)[0];

			// Create mesh component
			m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, ReallocateObjectBuffer, m_meshData, 0, 4 * 4 * 4, m_objectBuffer, m_objectBufferAllocation, m_meshDataOffset);

			// Create texture component
			m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, textureFile, true, Filter.Nearest, Filter.Nearest, m_materialDescriptorSet, 1, 2);

			// Create mvp uniform buffer
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_objectBufferAllocation, m_mvpDataOffset, m_meshDescriptorSet, 0, 0);

            // Create recolor settings buffer
            m_recolorUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_recolorData, m_objectBuffer, m_objectBufferAllocation, m_recolorDataOffset, m_materialDescriptorSet, 1, 1);

            m_bindableComponents = new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform, m_recolorUniform };
		}

		public void ReallocateObjectBuffer(VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int newSize)
		{
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int meshDataSize = m_meshData.Length;
			int mvpDataSize = m_mvpData.Length;
            int recolorDataSize = m_recolorData.Length;
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { meshDataSize, mvpDataSize, recolorDataSize },
				bufferAlignment,
				BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer,
				MemoryProperties.HostVisible,
				MemoryProperties.DeviceLocal,
				out m_objectBuffer,
				out m_objectBufferAllocation,
				out objectBufferOffsets
			);
			m_meshDataOffset = objectBufferOffsets[0];
			m_mvpDataOffset = objectBufferOffsets[1];
            m_recolorDataOffset = objectBufferOffsets[2];
		}

		public void Update()
		{
		}

		public RenderPass[] RenderPasses
		{
			get
			{
				return new[] { m_pipeline.GetRenderPass() };
			}
		}

		public VulkanPipeline[] Pipelines
		{
			get
			{
				return new[] { m_pipeline };
			}
		}

		public IVulkanBindableComponent[][] BindableComponents
		{
			get
			{
				return new IVulkanBindableComponent[][] { m_bindableComponents };
			}
		}

		public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets, int renderPassIndex)
		{
			commandBuffer.CmdDrawIndexed(m_numIndices);
		}

		public bool Visible { get; set; }
	}
}
