using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Math;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class Vulkan2DProgressBar: IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_outlinePipeline;
		VulkanPipeline m_fillPipeline;

		FVec2 m_position;
		FVec2 m_size;
		FVec4 m_color;

		byte[] m_outlineMeshData;
		byte[] m_fillMeshData;
		byte[] m_mvpData;
		int m_numOutlineIndices;
		int m_numFillIndices;

		VulkanCore.Buffer m_objectBuffer;
		VmaAllocation m_objectBufferAllocation;
		int m_outlineMeshDataOffset;
		int m_fillMeshDataOffset;
		int m_mvpDataOffset;

		DescriptorSet m_mvpDescriptorSet;

		VulkanMeshComponent m_outlineMesh;
		VulkanMeshComponent m_fillMesh;
		VulkanUniformBufferComponent m_mvpUniform;
		IVulkanBindableComponent[] m_outlineComponents;
		IVulkanBindableComponent[] m_fillComponents;

		public Vulkan2DProgressBar(VulkanAPIManager apiManager, VulkanPipeline outlinePipeline, VulkanPipeline fillPipeline, FVec2 position, FVec2 size, float percentFilled, FVec4 color)
		{
			m_apiManager = apiManager;
			m_outlinePipeline = outlinePipeline;
			m_fillPipeline = fillPipeline;

			m_position = position;
			m_size = size;
			m_color = color;

			float p = Functions.Clamp(percentFilled, 0.0f, 1.0f);

			// Create mesh data
			FVec2 topLeftOutline = new FVec2(position.X, position.Y);
			FVec2 topRightOutline = new FVec2(position.X + size.X, position.Y);
			FVec2 bottomLeftOutline = new FVec2(position.X, position.Y + size.Y);
			FVec2 bottomRightOutline = new FVec2(position.X + size.X, position.Y + size.Y);
			int[] outlineIndices = new[] { 0, 1, 1, 3, 3, 2, 2, 0 };
			m_numOutlineIndices = 8;

			FVec2 fillSize = new FVec2(p * size.X, size.Y);
			FVec2 topLeftFill = new FVec2(position.X, position.Y);
			FVec2 topRightFill = new FVec2(position.X + fillSize.X, position.Y);
			FVec2 bottomLeftFill = new FVec2(position.X, position.Y + fillSize.Y);
			FVec2 bottomRightFill = new FVec2(position.X + fillSize.X, position.Y + fillSize.Y);
			int[] fillIndices = new[] { 0, 1, 3, 3, 2, 0 };
			m_numFillIndices = 6;

			m_outlineMeshData = new byte[(4 * 2 * 4) + (4 * 4 * 4) + (8 * 4)];
			System.Buffer.BlockCopy(topLeftOutline.Bytes, 0, m_outlineMeshData, 0, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_outlineMeshData, 8, 16);
			System.Buffer.BlockCopy(topRightOutline.Bytes, 0, m_outlineMeshData, 24, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_outlineMeshData, 32, 16);
			System.Buffer.BlockCopy(bottomLeftOutline.Bytes, 0, m_outlineMeshData, 48, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_outlineMeshData, 56, 16);
			System.Buffer.BlockCopy(bottomRightOutline.Bytes, 0, m_outlineMeshData, 72, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_outlineMeshData, 80, 16);
			System.Buffer.BlockCopy(outlineIndices, 0, m_outlineMeshData, 96, 8 * 4);

			m_fillMeshData = new byte[(4 * 2 * 4) + (4 * 4 * 4) + (6 * 4)];
			System.Buffer.BlockCopy(topLeftFill.Bytes, 0, m_fillMeshData, 0, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_fillMeshData, 8, 16);
			System.Buffer.BlockCopy(topRightFill.Bytes, 0, m_fillMeshData, 24, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_fillMeshData, 32, 16);
			System.Buffer.BlockCopy(bottomLeftFill.Bytes, 0, m_fillMeshData, 48, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_fillMeshData, 56, 16);
			System.Buffer.BlockCopy(bottomRightFill.Bytes, 0, m_fillMeshData, 72, 8);
			System.Buffer.BlockCopy(color.Bytes, 0, m_fillMeshData, 80, 16);
			System.Buffer.BlockCopy(fillIndices, 0, m_fillMeshData, 96, 6 * 4);

			// Create mvp data
			FMat4 mvp = new FMat4(1.0f);
			float depth = 1.0f;

			m_mvpData = new byte[(1 * 4 * 4 * 4) + (1 * 4)];
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);

			// Create buffer
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

			// Allocate descriptor sets
			m_mvpDescriptorSet = m_fillPipeline.GetShader().AllocateDescriptorSets(0, 1)[0];

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
