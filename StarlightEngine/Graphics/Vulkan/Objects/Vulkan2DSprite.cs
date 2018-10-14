using StarlightEngine.Math;
using StarlightEngine.Graphics.Vulkan.Memory;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using System.Collections.Generic;
using VulkanCore;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class Vulkan2DSprite : IVulkanDrawableObject
	{
		VulkanAPIManager m_apiManager;
		VulkanPipeline m_pipeline;

		FMat4 m_modelMatrix;
		float m_depth;

		byte[] m_meshData;
		byte[] m_mvpData;

		VulkanManagedBuffer m_objectBuffer;

		VulkanDescriptorSet m_meshDescriptorSet;
		VulkanDescriptorSet m_materialDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanTextureComponent m_texture;
		VulkanUniformBufferComponent m_mvpUniform;
		IVulkanBindableComponent[] m_bindableComponents;

		public Vulkan2DSprite(VulkanAPIManager apiManager, string textureFile, FVec2 position, FVec2 scale, float depth = 0.0f)
		{
			m_apiManager = apiManager;
			m_pipeline = StaticPipelines.pipeline_basic2D;

			this.Visible = true;

			// Create mesh data
			FVec4 topLeft = new FVec4(position.X(), position.Y(), 0.0f, 0.0f);
			FVec4 topRight = new FVec4(position.X() + scale.X(), position.Y(), 1.0f, 0.0f);
			FVec4 bottomLeft = new FVec4(position.X(), position.Y() + scale.Y(), 0.0f, 1.0f);
			FVec4 bottomRight = new FVec4(position.X() + scale.X(), position.Y() + scale.Y(), 1.0f, 1.0f);
			int[] indices = { 0, 1, 3, 3, 2, 0 };

			m_meshData = new byte[(4 * 4 * 4) + (6 * 4)];
			System.Buffer.BlockCopy(topLeft.Bytes, 0, m_meshData, 0, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(topRight.Bytes, 0, m_meshData, (int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(bottomLeft.Bytes, 0, m_meshData, 2 * (int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(bottomRight.Bytes, 0, m_meshData, 3 * (int)topLeft.PrimativeSizeOf, (int)topLeft.PrimativeSizeOf);
			System.Buffer.BlockCopy(indices, 0, m_meshData, 4 * (int)topLeft.PrimativeSizeOf, 6 * 4);

			// Create mvp data
			m_modelMatrix = new FMat4(1.0f);
			m_depth = depth;

			m_mvpData = new byte[(1 * 4 * 4 * 4) + (1 * 4)];
			System.Buffer.BlockCopy(m_modelMatrix.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			m_objectBuffer = new VulkanManagedBuffer(m_apiManager, bufferAlignment, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.None, MemoryProperties.DeviceLocal);

			// Create descriptor sets
			m_meshDescriptorSet = m_pipeline.CreateDescriptorSet(0);
			m_materialDescriptorSet = m_pipeline.CreateDescriptorSet(1);

			// Create mesh component
			m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, m_meshData, 0, 4 * 4 * 4, 6, m_objectBuffer);

			// Create texture component
			m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, textureFile, true, Filter.Linear, Filter.Linear, m_materialDescriptorSet, 1);

			// Create mvp uniform buffer
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_meshDescriptorSet, 0);

			m_objectBuffer.WriteAllBuffers(true);

			m_bindableComponents = new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform };
		}

		public void Update()
		{
		}

		public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform){
			FMat4 mvp = projection * view * modelTransform * m_modelMatrix;

			System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, 4 * 4 * 4);
			m_mvpUniform.UpdateUniformBuffer(m_mvpData);
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

		public void Draw(CommandBuffer commandBuffer, int swapchainIndex)
		{
			m_mesh.DrawMesh(commandBuffer, swapchainIndex);
		}

		public bool Visible { get; set; }
		
        public (EventManager.HandleEventDelegate, EventType)[] EventListeners
        {
            get
            {
                return new(EventManager.HandleEventDelegate, EventType)[] { };
            }
        }
	}
}
