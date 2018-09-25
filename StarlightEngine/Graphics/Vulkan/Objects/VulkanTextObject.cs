using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Vulkan.Memory;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Math;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanTextObject: IVulkanObject, IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;

		FVec2 m_position;
		float m_width;

		TextMesh m_textMesh;

		byte[] m_meshData;
		byte[] m_mvpData;
		byte[] m_fontSettingsData;
		int m_numIndices;

		VulkanManagedBuffer m_objectBuffer;

		VulkanDescriptorSet m_meshDescriptorSet;
		VulkanDescriptorSet m_materialDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanTextureComponent m_texture;
		VulkanUniformBufferComponent m_mvpUniform;
		VulkanUniformBufferComponent m_fontSettingsUniform;
		IVulkanBindableComponent[] m_bindableComponents;

		public VulkanTextObject(VulkanAPIManager apiManager, AngelcodeFont font, string text, int size, FVec2 location, float width)
		{
			m_apiManager = apiManager;
			m_pipeline = StaticPipelines.pipeline_distanceFieldFont;

			this.Visible = true;

			// Create text mesh
			m_position = new FVec2(location.X * (apiManager.GetSwapchainImageExtent().Width / 2.0f), location.Y * (apiManager.GetSwapchainImageExtent().Height / 2.0f));
			m_width = width * (apiManager.GetSwapchainImageExtent().Width / 2);
			m_textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, text, m_position, m_width);
			m_numIndices = m_textMesh.numVertices;

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			m_objectBuffer = new VulkanManagedBuffer(m_apiManager, bufferAlignment, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.None, MemoryProperties.DeviceLocal);

			// Create descriptor sets
			m_meshDescriptorSet = m_pipeline.CreateDescriptorSet(0);
			m_materialDescriptorSet = m_pipeline.CreateDescriptorSet(1);

			// Create mesh component
			m_meshData = new byte[m_textMesh.meshBufferData.Length];
			System.Buffer.BlockCopy(m_textMesh.meshBufferData, 0, m_meshData, 0, m_textMesh.meshBufferData.Length);
			m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, m_meshData, m_textMesh.vboOffset, m_textMesh.iboOffset, m_objectBuffer);

			// Create texture component
			m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, "./assets/" + font.pages[0].file, false, Filter.Linear, Filter.Linear, m_materialDescriptorSet, 2);

			// Create mvp uniform buffer
			m_mvpData = new byte[(4 * 4 * 4) + (4)];
			FMat4 mvp = new FMat4(1.0f);
			mvp[0, 0] = 1.0f / 640.0f;
			mvp[1, 1] = 1.0f / 360.0f;
			float depth = 1.0f;
			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, (int)mvp.PrimativeSizeOf);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_meshDescriptorSet, 0);

			// Create font settings uniform buffer
			m_fontSettingsData = new byte[(2 * 4 * 4) + (2 * 4) + (3 * 4)];
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
			m_fontSettingsUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_fontSettingsData, m_objectBuffer, m_materialDescriptorSet, 1);

			m_objectBuffer.WriteBuffer();

			m_bindableComponents = new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform, m_fontSettingsUniform };
		}

		public void UpdateText(AngelcodeFont font, string newText, int size)
		{
			TextMesh textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, newText, m_position, m_width);
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

		public bool Visible { get; set; }
	}
}
