using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Math;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Vulkan.Memory;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    /* Loads an object from a .obj file with a texture
     * An example of creating a basic object using the component system
     */
    public class VulkanTexturedMesh: IVulkanObject, IVulkanDrawableObject
    {
		VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;

		WavefrontObject m_loadedObject;

		byte[] m_meshData;
		byte[] m_mvpData;
		byte[] m_lightingData;
		int m_numIndices;

		VulkanManagedBuffer m_objectBuffer;

		VulkanDescriptorSet m_meshDescriptorSet;
		VulkanDescriptorSet m_materialDescriptorSet;

		VulkanMeshComponent m_mesh;
		VulkanTextureComponent m_texture;
		VulkanUniformBufferComponent m_mvpUniform;
		VulkanUniformBufferComponent m_lightingUniform;

		public VulkanTexturedMesh(VulkanAPIManager apiManager, string objFile, string textureFile, FMat4 model, FMat4 view, FMat4 proj, FVec4 lightPosition, FVec4 lightColor, float ambientLight, float shineDamper, float reflectivity)
        {
			m_apiManager = apiManager;
			m_pipeline = StaticPipelines.pipeline_basic3D;

			this.Visible = true;

			// Load object
			m_loadedObject = WavefrontModelLoader.LoadFile(objFile);
			m_numIndices = m_loadedObject.Indices.Length;

			// Create object buffer
			int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			m_objectBuffer = new VulkanManagedBuffer(m_apiManager, bufferAlignment, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.None, MemoryProperties.DeviceLocal);

			// Create descriptor sets
			m_meshDescriptorSet = m_pipeline.CreateDescriptorSet(0);
			m_materialDescriptorSet = m_pipeline.CreateDescriptorSet(1);

			// Create mesh component
			m_meshData = new byte[m_loadedObject.VertexData.Length + (m_loadedObject.Indices.Length * 4)];
			System.Buffer.BlockCopy(m_loadedObject.VertexData, 0, m_meshData, 0, m_loadedObject.VertexData.Length);
			System.Buffer.BlockCopy(m_loadedObject.Indices, 0, m_meshData, m_loadedObject.VertexData.Length, m_loadedObject.Indices.Length * 4);
			m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, m_meshData, 0, m_loadedObject.VertexData.Length, m_objectBuffer);

			// Create texture component
			m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, textureFile, true, Filter.Linear, Filter.Linear, m_materialDescriptorSet, 2);

			// Create mvp uniform buffer
			m_mvpData = new byte[(3 * 4 * 4 * 4)];
			System.Buffer.BlockCopy(model.Bytes, 0, m_mvpData, 0 * 4 * 4 * 4, 4 * 4 * 4);
			System.Buffer.BlockCopy(view.Bytes, 0, m_mvpData, 1 * 4 * 4 * 4, 4 * 4 * 4);
			System.Buffer.BlockCopy(proj.Bytes, 0, m_mvpData, 2 * 4 * 4 * 4, 4 * 4 * 4);
			m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_meshDescriptorSet, 0);

			// Create lighting uniform buffer
			m_lightingData = new byte[(2 * 4 * 4) + (3 * 4)];
			System.Buffer.BlockCopy(lightPosition.Bytes, 0, m_lightingData, 0, 4 * 4);
			System.Buffer.BlockCopy(lightColor.Bytes, 0, m_lightingData, 4 * 4, 4 * 4);
			System.Buffer.BlockCopy(new[] { ambientLight }, 0, m_lightingData, 2 * 4 * 4, 4);
			System.Buffer.BlockCopy(new[] { shineDamper }, 0, m_lightingData, 2 * 4 * 4 + (4), 4);
			System.Buffer.BlockCopy(new[] { reflectivity }, 0, m_lightingData, 2 * 4 * 4 + (8), 4);
			m_lightingUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_lightingData, m_objectBuffer, m_materialDescriptorSet, 1);

			m_objectBuffer.WriteAllBuffers(true);
        }

		public virtual void Update()
		{
		}

		public void UpdateModelMatrix(FMat4 newModel)
		{
			System.Buffer.BlockCopy(newModel.Bytes, 0, m_mvpData, 0 * 4 * 4 * 4, 4 * 4 * 4);
			m_mvpUniform.UpdateUniformBuffer(m_mvpData);
		}

		public void UpdateViewMatrix(FMat4 newView)
		{
			System.Buffer.BlockCopy(newView.Bytes, 0, m_mvpData, 1 * 4 * 4 * 4, 4 * 4 * 4);
			m_mvpUniform.UpdateUniformBuffer(m_mvpData);
		}

		public void UpdateProjectionMatrix(FMat4 newProj)
		{
			System.Buffer.BlockCopy(newProj.Bytes, 0, m_mvpData, 0 * 4 * 4 * 4, 4 * 4 * 4);
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
				return new IVulkanBindableComponent[][] { new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform, m_lightingUniform } };
            }
        }

        public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets, int renderPassIndex)
        {
			commandBuffer.CmdDrawIndexed(m_numIndices);
        }

		public bool Visible { get; set; }
    }
}
