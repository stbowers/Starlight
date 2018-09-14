using System;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Math;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanTextObject: IVulkanObject, IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;

		TextMesh m_textMesh;

		byte[] m_meshData;
		byte[] m_mvpData;
		byte[] m_fontSettingsData;
		int m_numIndices;

		VulkanCore.Buffer m_objectBuffer;
		VmaAllocation m_objectBufferAllocation;
		int m_meshDataOffset;
		int m_mvpDataOffset;
		int m_fontSettingsDataOffset;

		DescriptorSet m_meshDescriptorSet;
		DescriptorSet m_materialDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanTextureComponent m_texture;
		VulkanUniformBufferComponent m_mvpUniform;
		VulkanUniformBufferComponent m_fontSettingsUniform;
		IVulkanBindableComponent[] m_bindableComponents;

		public VulkanTextObject(VulkanAPIManager apiManager, VulkanPipeline pipeline, AngelcodeFont font, string text, int size, FVec2 location, float width)
		{
			m_apiManager = apiManager;
			m_pipeline = pipeline;

			// Create text mesh
			m_textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, text, location, width);
			m_numIndices = m_textMesh.numVertices;

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int meshDataSize = m_textMesh.meshBufferData.Length;
			int mvpDataSize = (4 * 4 * 4) + (4);
			int fontSettingsDataSize = (2 * 4 * 4) + (1 * 2 * 4) + (3 * 4);
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { meshDataSize, mvpDataSize, fontSettingsDataSize },
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
			m_fontSettingsDataOffset = objectBufferOffsets[2];

			// Create descriptor sets
			m_meshDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(0, 1)[0];
			m_materialDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(1, 1)[0];

			// Create mesh component
			m_meshData = new byte[meshDataSize];
			System.Buffer.BlockCopy(m_textMesh.meshBufferData, 0, m_meshData, 0, m_textMesh.meshBufferData.Length);
			m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, ReallocateObjectBuffer, m_meshData, m_textMesh.vboOffset, m_textMesh.iboOffset, m_objectBuffer, m_objectBufferAllocation, m_meshDataOffset);

			// Create texture component
			m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, "./assets/" + font.pages[0].file, false, m_materialDescriptorSet, 1, 2);

			// Create mvp uniform buffer
			m_mvpData = new byte[mvpDataSize];
			FMat4 mvp = new FMat4(1.0f);
			mvp[0, 0] = 1.0f / 640.0f;
			mvp[1, 1] = 1.0f / 360.0f;
			float depth = 1.0f;
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, (int)mvp.PrimativeSizeOf);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_objectBufferAllocation, m_mvpDataOffset, m_meshDescriptorSet, 0, 0);

			// Create font settings uniform buffer
			m_fontSettingsData = new byte[fontSettingsDataSize];
			FVec4 textColor = new FVec4(0.0f, 0.0f, 0.0f, 0.0f);
			FVec4 outlineColor = new FVec4(1.0f, 1.0f, 1.0f, 0.0f);
			FVec2 outlineShift = new FVec2(0.0f, 0.0f);
			float textWidth = .5f;
			float outlineWidth = 0.65f;
			float edge = .2f;
			System.Buffer.BlockCopy(textColor.Bytes, 0, m_fontSettingsData, 0, 4 * 4);
			System.Buffer.BlockCopy(outlineColor.Bytes, 0, m_fontSettingsData, 4 * 4, 4 * 4);
			System.Buffer.BlockCopy(outlineShift.Bytes, 0, m_fontSettingsData, 8 * 4, 2 * 4);
			System.Buffer.BlockCopy(new[] { textWidth }, 0, m_fontSettingsData, 10 * 4, 1 * 4);
			System.Buffer.BlockCopy(new[] { outlineWidth }, 0, m_fontSettingsData, 11 * 4, 1 * 4);
			System.Buffer.BlockCopy(new[] { edge }, 0, m_fontSettingsData, 12 * 4, 1 * 4);
			m_fontSettingsUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_fontSettingsData, m_objectBuffer, m_objectBufferAllocation, m_fontSettingsDataOffset, m_materialDescriptorSet, 1, 1);

			m_bindableComponents = new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform, m_fontSettingsUniform };
		}

		public void ReallocateObjectBuffer(VulkanCore.Buffer buffer, VmaAllocation bufferAllocation, int newSize)
		{
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			int meshDataSize = newSize;
			int mvpDataSize = (4 * 4 * 4) + (4);
			int fontSettingsDataSize = (2 * 4 * 4) + (1 * 2 * 4) + (3 * 4);
			int[] objectBufferOffsets;
			m_apiManager.CreateSectionedBuffer(
				new int[] { meshDataSize, mvpDataSize, fontSettingsDataSize },
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
			m_fontSettingsDataOffset = objectBufferOffsets[2];

			m_mesh.ChangeBuffer(m_objectBuffer, m_objectBufferAllocation, m_meshDataOffset);
			m_mvpUniform.ChangeBuffer(m_objectBuffer, m_objectBufferAllocation, m_mvpDataOffset);
			m_fontSettingsUniform.ChangeBuffer(m_objectBuffer, m_objectBufferAllocation, m_fontSettingsDataOffset);
		}

		public void UpdateText(AngelcodeFont font, string newText, int size)
		{
			TextMesh textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, newText, new FVec2(-638.0f, -357.0f), 640.0f);
			m_textMesh = textMesh;
			m_numIndices = textMesh.numVertices;
			int meshDataSize = m_textMesh.meshBufferData.Length;
			m_meshData = new byte[meshDataSize];
			System.Buffer.BlockCopy(m_textMesh.meshBufferData, 0, m_meshData, 0, m_textMesh.meshBufferData.Length);
			m_mesh.UpdateMesh(m_meshData, m_textMesh.vboOffset, m_textMesh.iboOffset);
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
				return new[] { m_bindableComponents };
			}
		}


		public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets, int renderPassIndex)
		{
			commandBuffer.CmdDrawIndexed(m_numIndices);
		}
	}
}
