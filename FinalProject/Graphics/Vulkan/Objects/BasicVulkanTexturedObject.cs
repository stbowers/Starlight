using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FinalProject.Graphics.Objects;
using FinalProject.Graphics.Math;
using VulkanCore;

namespace FinalProject.Graphics.Vulkan.Objects
{
    /* Loads an object from a .obj file with a texture
     * An example of creating a basic object using the component system
     */
    class BasicVulkanTexturedObject: IVulkanObject, IComponentVulkanMesh, IComponentVulkanMaterial
    {
        VulkanAPIManager m_apiManager;
        DefaultComponentVulkanMesh m_meshComponent;
        DefaultComponentVulkanMaterial m_materialComponent;
        VulkanPipeline m_pipeline;

        byte[] m_meshData;
        byte[] m_uboData;
        DescriptorSet m_meshDescriptorSet;

		Stopwatch timer = new Stopwatch();

        public BasicVulkanTexturedObject(VulkanAPIManager apiManager, string objFile, string textureFile)
        {
			timer.Start();

            m_apiManager = apiManager;
            WavefrontObject loadedObject = WavefrontModelLoader.LoadFile(objFile);

            VulkanPipeline.VulkanPipelineCreateInfo pipelineInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineInfo.topology = VulkanCore.PrimitiveTopology.TriangleList;
			pipelineInfo.primitiveRestartEnable = false;
			pipelineInfo.frontFaceCCW = true;
			pipelineInfo.depthTestEnable = true;
			pipelineInfo.depthWriteEnable = true;
			pipelineInfo.clearColorAttachment = true;
			pipelineInfo.clearDepthAttachment = true;

			VulkanShader.ShaderCreateInfo vertexInfo = new VulkanShader.ShaderCreateInfo();
			vertexInfo.shaderFile = "./assets/basicShader.vert.spv";
			vertexInfo.entryPoint = "main";
			vertexInfo.stage = ShaderStages.Vertex;
			vertexInfo.inputs = new[] { new[] { ShaderTypes.vec3, ShaderTypes.vec2, ShaderTypes.vec3 } };
			ShaderUniformInputInfo uboInfo = new ShaderUniformInputInfo();
			uboInfo.binding = 0;
			uboInfo.set = 0;
			uboInfo.type = DescriptorType.UniformBuffer;
			vertexInfo.uniformInputInfos = new[] { uboInfo };

			VulkanShader.ShaderCreateInfo fragInfo = new VulkanShader.ShaderCreateInfo();
			fragInfo.shaderFile = "./assets/basicShader.frag.spv";
			fragInfo.entryPoint = "main";
			fragInfo.stage = ShaderStages.Fragment;
			ShaderUniformInputInfo texSamplerInfo = new ShaderUniformInputInfo();
			texSamplerInfo.binding = 1;
			texSamplerInfo.set = 1;
			texSamplerInfo.type = DescriptorType.CombinedImageSampler;
			fragInfo.uniformInputInfos = new[] { texSamplerInfo };

			pipelineInfo.vertexShader = apiManager.GetShader(vertexInfo);
			pipelineInfo.fragmentShader = apiManager.GetShader(fragInfo);
			m_pipeline = apiManager.GetPipeline(pipelineInfo);

			// ubo
			Mat4 model = Mat4.Rotate((timer.ElapsedMilliseconds / 1000.0f) * ((float)System.Math.PI / 4), new Vec3(0.0f, 1.0f, 0.0f));
            Mat4 view = Mat4.LookAt(new Vec3(1.0f, 1.0f, 1.0f), new Vec3(0.0f, 0.0f, 0.0f), new Vec3(0.0f, 1.0f, 0.0f));
            Mat4 proj = Mat4.Perspective((float)System.Math.PI / 2, m_apiManager.GetSwapchainImageExtent().Width / m_apiManager.GetSwapchainImageExtent().Height, 0.1f, 10.0f);
            proj[1, 1] *= -1f;

			m_uboData = new byte[3 * model.PrimativeSizeOf];
			model.Bytes.CopyTo(m_uboData, 0);
			view.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf);
			proj.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf + view.PrimativeSizeOf);

            m_meshData = new byte[loadedObject.VertexData.Length + (loadedObject.Indices.Length * 4)];
            System.Buffer.BlockCopy(loadedObject.VertexData, 0, m_meshData, 0, loadedObject.VertexData.Length);
            System.Buffer.BlockCopy(loadedObject.Indices, 0, m_meshData, loadedObject.VertexData.Length, loadedObject.Indices.Length * 4);

            m_meshDescriptorSet = m_pipeline.GetVertexShader().AllocateDescriptorSets(0, 1)[0];

            m_meshComponent = new DefaultComponentVulkanMesh(m_apiManager, m_meshData, m_uboData, 0, loadedObject.VertexData.Length, loadedObject.Indices.Length, m_meshDescriptorSet, 0, 0);

            m_materialComponent = new DefaultComponentVulkanMaterial(this, m_apiManager, m_pipeline.GetFragmentShader(), textureFile);
        }

		public void Update()
		{
			// update ubo
			Mat4 model = Mat4.Rotate((timer.ElapsedMilliseconds / 1000.0f) * ((float)System.Math.PI / 4), new Vec3(0.0f, 1.0f, 0.0f));
			Mat4 view = Mat4.LookAt(new Vec3(6.0f, 6.0f, 6.0f), new Vec3(0.0f, 4.0f, 0.0f), new Vec3(0.0f, 1.0f, 0.0f));
			Mat4 proj = Mat4.Perspective(((float)System.Math.PI * 2 )/ 3, m_apiManager.GetSwapchainImageExtent().Width / m_apiManager.GetSwapchainImageExtent().Height, 0.1f, 20.0f);
			proj[1, 1] *= -1f;

			m_uboData = new byte[3 * model.PrimativeSizeOf];
			model.Bytes.CopyTo(m_uboData, 0);
			view.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf);
			proj.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf + view.PrimativeSizeOf);

			m_meshComponent.UpdateUBO(m_uboData);
		}

        public VulkanPipeline Pipeline
        {
            get
            {
                return m_pipeline;
            }
        }

        public IComponentExplicitVulkanMesh ExplicitMesh
        {
            get
            {
                return m_meshComponent;
            }
        }

        public IComponentExplicitVulkanMaterial ExplicitMaterial
        {
            get
            {
                return m_materialComponent;
            }
        }
    }
}
