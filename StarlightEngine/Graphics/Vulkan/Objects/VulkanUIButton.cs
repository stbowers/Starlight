using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Fonts;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanUIButton: IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_highlightPipeline;
		VulkanPipeline m_textPipeline;

		FVec2 m_position;
		FVec2 m_size;
		FVec4 m_highlightColor;

		byte[] m_highlightMeshData;
        byte[] m_textMeshData;
		byte[] m_mvpData;
        int m_numHighlightIndices;
        int m_numTextIndices;

		VulkanCore.Buffer m_objectBuffer;
		VmaAllocation m_objectBufferAllocation;
		int m_highlightMeshDataOffset;
        int m_textMeshDataOffset;
		int m_mvpDataOffset;

		DescriptorSet m_highlightMeshDescriptorSet;
        DescriptorSet m_textMeshDescriptorSet;
        DescriptorSet m_textMaterialDescriptorSet;

		VulkanMeshComponent m_highlightMesh;
		VulkanMeshComponent m_textMesh;
		VulkanUniformBufferComponent m_mvpUniform;
		IVulkanBindableComponent[] m_highlightComponents;
		IVulkanBindableComponent[] m_textComponents;

		public VulkanUIButton(VulkanAPIManager apiManager, VulkanPipeline highlightPipeline, VulkanPipeline textPipeline, FVec2 position, FVec2 size, FVec4 highlightColor, AngelcodeFont font, string text)
		{
			m_apiManager = apiManager;
			m_highlightPipeline = highlightPipeline;
			m_textPipeline = textPipeline;

			m_position = position;
			m_size = size;
			m_highlightColor = highlightColor;

			// Create mesh data
			FVec2 topLeftHighlight = new FVec2(position.X, position.Y);
			FVec2 topRightHighlight = new FVec2(position.X + size.X, position.Y);
			FVec2 bottomLeftHighlight = new FVec2(position.X, position.Y + size.Y);
			FVec2 bottomRightHighlight = new FVec2(position.X + size.X, position.Y + size.Y);
			int[] highlightIndices = new[] { 0, 1, 3, 3, 2, 0 };
			m_numHighlightIndices = 6;

			m_highlightMeshData = new byte[(4 * 2 * 4) + (4 * 4 * 4) + (6 * 4)];
			System.Buffer.BlockCopy(topLeftHighlight.Bytes, 0, m_highlightMeshData, 0, 8);
			System.Buffer.BlockCopy(m_highlightColor.Bytes, 0, m_highlightMeshData, 8, 16);
			System.Buffer.BlockCopy(topLeftHighlight.Bytes, 0, m_highlightMeshData, 24, 8);
			System.Buffer.BlockCopy(m_highlightColor.Bytes, 0, m_highlightMeshData, 32, 16);
			System.Buffer.BlockCopy(topLeftHighlight.Bytes, 0, m_highlightMeshData, 48, 8);
			System.Buffer.BlockCopy(m_highlightColor.Bytes, 0, m_highlightMeshData, 56, 16);
			System.Buffer.BlockCopy(topLeftHighlight.Bytes, 0, m_highlightMeshData, 72, 8);
			System.Buffer.BlockCopy(m_highlightColor.Bytes, 0, m_highlightMeshData, 80, 16);
			System.Buffer.BlockCopy(highlightIndices, 0, m_highlightMeshData, 96, 6 * 4);

			// Create mvp data
			FMat4 mvp = new FMat4(1.0f);
			float depth = 1.0f;

			m_mvpData = new byte[(1 * 4 * 4 * 4) + (1 * 4)];
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);

			// Create buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int highlightMeshDataSize = m_highlightMeshData.Length;
            int textMeshDataSize = m_textMeshData.Length;
			int mvpDataSize = m_mvpData.Length;
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { highlightMeshDataSize, textMeshDataSize, mvpDataSize },
				bufferAlignment,
				BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer,
				MemoryProperties.HostVisible,
				MemoryProperties.DeviceLocal,
				out m_objectBuffer,
				out m_objectBufferAllocation,
				out objectBufferOffsets
			);
			m_highlightMeshDataOffset = objectBufferOffsets[0];
			m_textMeshDataOffset = objectBufferOffsets[1];
			m_mvpDataOffset = objectBufferOffsets[2];

			// Allocate descriptor sets
			m_highlightMeshDescriptorSet = m_highlightPipeline.GetShader().AllocateDescriptorSets(0, 1)[0];

			// Create mesh components
			m_outlineMesh = new VulkanMeshComponent(m_apiManager, m_outlinePipeline, ReallocateObjectBuffer, m_outlineMeshData, 0, m_outlineMeshData.Length - (8 * 4), m_objectBuffer, m_objectBufferAllocation, m_outlineMeshDataOffset);
			m_fillMesh = new VulkanMeshComponent(m_apiManager, m_fillPipeline, ReallocateObjectBuffer, m_fillMeshData, 0, m_fillMeshData.Length - (6 * 4), m_objectBuffer, m_objectBufferAllocation, m_fillMeshDataOffset);

			// Create mvp uniform component
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_fillPipeline, m_mvpData, m_objectBuffer, m_objectBufferAllocation, m_mvpDataOffset, m_mvpDescriptorSet, 0, 0);

			m_outlineComponents = new IVulkanBindableComponent[] { m_outlineMesh, m_mvpUniform };
			m_fillComponents = new IVulkanBindableComponent[] { m_fillMesh, m_mvpUniform };
		}

		public void ReallocateObjectBuffer(VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int newSize)
		{
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int outlineMeshDataSize = m_outlineMeshData.Length;
			int fillMeshDataSize = m_fillMeshData.Length;
			int mvpDataSize = m_mvpData.Length;
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { outlineMeshDataSize, fillMeshDataSize, mvpDataSize },
				bufferAlignment,
				BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer,
				MemoryProperties.HostVisible,
				MemoryProperties.DeviceLocal,
				out m_objectBuffer,
				out m_objectBufferAllocation,
				out objectBufferOffsets
			);
			m_outlineMeshDataOffset = objectBufferOffsets[0];
			m_fillMeshDataOffset = objectBufferOffsets[1];
			m_mvpDataOffset = objectBufferOffsets[2];
		}

		public void Update()
		{
		}

		public void UpdatePercentage(float newPercentage)
		{
			float p = Functions.Clamp(newPercentage, 0.0f, 1.0f);
			FVec2 fillSize = new FVec2(p * m_size.X, m_size.Y);
			FVec2 topLeftFill = new FVec2(m_position.X, m_position.Y);
			FVec2 topRightFill = new FVec2(m_position.X + fillSize.X, m_position.Y);
			FVec2 bottomLeftFill = new FVec2(m_position.X, m_position.Y + fillSize.Y);
			FVec2 bottomRightFill = new FVec2(m_position.X + fillSize.X, m_position.Y + fillSize.Y);

			System.Buffer.BlockCopy(topLeftFill.Bytes, 0, m_fillMeshData, 0, 8);
			System.Buffer.BlockCopy(topRightFill.Bytes, 0, m_fillMeshData, 24, 8);
			System.Buffer.BlockCopy(bottomLeftFill.Bytes, 0, m_fillMeshData, 48, 8);
			System.Buffer.BlockCopy(bottomRightFill.Bytes, 0, m_fillMeshData, 72, 8);

			m_fillMesh.UpdateMesh(m_fillMeshData, 0, m_fillMeshData.Length - (6 * 4));
		}

		public RenderPass[] RenderPasses
		{
			get
			{
				return new[] { m_fillPipeline.GetRenderPass(), m_outlinePipeline.GetRenderPass() };
			}
		}

		public VulkanPipeline[] Pipelines
		{
			get
			{
				return new[] { m_fillPipeline, m_outlinePipeline };
			}
		}

		public IVulkanBindableComponent[][] BindableComponents
		{
			get
			{
				return new[] { m_fillComponents, m_outlineComponents };
			}
		}

		public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSet, int renderPassIndex)
		{
			if (renderPassIndex == 0)
			{
				commandBuffer.CmdDrawIndexed(m_numFillIndices);
			}
			else if (renderPassIndex == 1)
			{
				commandBuffer.CmdDrawIndexed(m_numOutlineIndices);
			}
		}
	}
}
