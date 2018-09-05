using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using FinalProject.Graphics.Vulkan;
using FinalProject.Graphics.Vulkan.Objects;
using FinalProject.Graphics.Math;
using VulkanCore;
using GlmNet;

namespace FinalProject
{
	public class TestVulkanObject: IVulkanObject, IComponentExplicitVulkanMesh, IComponentVulkanMaterial
	{
		private VulkanAPIManager m_apiManager;
		private VulkanPipeline m_pipeline;
		private VulkanCore.Buffer m_meshBuffer;
		private VmaAllocation m_meshBufferAllocation;
		private long m_vboOffset;
		private long m_vboSize;
		private long m_iboOffset;
		private long m_iboSize;
		private long m_uboOffset;
		private long m_uboSize;
		private DescriptorSet m_uboDescriptorSet;

		// a wrapper which provides the IComponentExplicitVulkanMaterial implementation, since we don't want to do anything special
		private DefaultComponentVulkanMaterial m_defaultMaterialWrapper;

		public TestVulkanObject(VulkanAPIManager apiManager)
		{
			m_apiManager = apiManager;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineInfo.topology = VulkanCore.PrimitiveTopology.TriangleList;
			pipelineInfo.primitiveRestartEnable = false;
			pipelineInfo.frontFaceCCW = false;
			pipelineInfo.depthTestEnable = false;
			pipelineInfo.depthWriteEnable = false;
			pipelineInfo.clearColorAttachment = true;
			pipelineInfo.clearDepthAttachment = true;

			VulkanShader.ShaderCreateInfo vertexInfo = new VulkanShader.ShaderCreateInfo();
			vertexInfo.shaderFile = "./assets/basicShader.vert.spv";
			vertexInfo.entryPoint = "main";
			vertexInfo.stage = ShaderStages.Vertex;
			vertexInfo.inputs = new[] { new[] { ShaderTypes.vec2, ShaderTypes.vec2 } };
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

			List<float> verts = new List<float>();
			verts.AddRange(new vec2(-.5f, .5f).to_array());
			verts.AddRange(new vec2(1.0f, 0.0f).to_array());
			verts.AddRange(new vec2(.5f, .5f).to_array());
			verts.AddRange(new vec2(0.0f, 0.0f).to_array());
			verts.AddRange(new vec2(.5f, -.5f).to_array());
			verts.AddRange(new vec2(0.0f, 1.0f).to_array());
			verts.AddRange(new vec2(-.5f, -.5f).to_array());
			verts.AddRange(new vec2(1.0f, 1.0f).to_array());

			int[] indices = {
				0, 1, 2, 2, 3, 0
			};

			Mat4 mvp = new Mat4(1.0f);

			mvp[1, 1] *= -1.0f;

			byte[] ubo = mvp.Bytes;

			// calculate size and offsets for vbo and ibo
			m_vboOffset = 0;
			m_vboSize = 8 * 2 * 4;
			m_iboOffset = m_vboOffset + m_vboSize;
			m_iboSize = indices.Length * 4;

			// calculate size and offset for ubo (must be alligned properly)
			long uboAlignment = m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
			long padding = uboAlignment - ((m_iboOffset + m_iboSize) % uboAlignment);
			m_uboOffset = m_iboOffset + m_iboSize + padding;
			m_uboSize = 16 * 4;

			long meshBufferSize = m_vboSize + m_iboSize + padding + m_uboSize;
			byte[] meshBufferData = new byte[meshBufferSize];
			m_apiManager.CreateBuffer(meshBufferSize, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.HostVisible, MemoryProperties.DeviceLocal, out m_meshBuffer, out m_meshBufferAllocation);

			System.Buffer.BlockCopy(verts.ToArray(), 0, meshBufferData, 0, (int)m_vboSize);
			System.Buffer.BlockCopy(indices, 0, meshBufferData, (int)m_iboOffset, (int)m_iboSize);
			System.Buffer.BlockCopy(ubo, 0, meshBufferData, (int)m_uboOffset, (int)m_uboSize);

			IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
			Marshal.Copy(meshBufferData, 0, mappedMemory, (int)meshBufferSize);

			/*
			MappedMemoryRange mappedMemoryRange = new MappedMemoryRange();
			mappedMemoryRange.Memory = m_meshBufferAllocation.memory;
			mappedMemoryRange.Offset = m_meshBufferAllocation.offset;
			mappedMemoryRange.Size = m_meshBufferAllocation.size;
			m_apiManager.GetDevice().FlushMappedMemoryRange(mappedMemoryRange);
			*/

			m_meshBufferAllocation.memory.Unmap();

			m_uboDescriptorSet = m_pipeline.GetVertexShader().AllocateDescriptorSets(0, 1)[0];

			DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
			bufferInfo.Buffer = m_meshBuffer;
			bufferInfo.Offset = m_uboOffset;
			bufferInfo.Range = m_uboSize;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = m_uboDescriptorSet;
			descriptorWrite.DstBinding = 0;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			descriptorWrite.BufferInfo = new[] { bufferInfo };

			m_uboDescriptorSet.Parent.UpdateSets(new[] { descriptorWrite });


			// set up default material implementation
			m_defaultMaterialWrapper = new DefaultComponentVulkanMaterial(this, m_apiManager, m_pipeline.GetFragmentShader(), "./assets/texture.jpg");
		}

		public VulkanCore.Buffer MeshBuffer
		{
			get
			{
				return m_meshBuffer;
			}
		}

		public int VBOOffset
		{
			get
			{
				return (int)m_vboOffset;
			}
		}

		public int IBOOffset
		{
			get
			{
				return (int)m_iboOffset;
			}
		}

		public int UBOOffset
		{
			get
			{
				return (int)m_uboOffset;
			}
		}

		public VulkanPipeline Pipeline
		{
			get
			{
				return m_pipeline;
			}
		}

		public DescriptorSet UBODescriptorSet
		{
			get
			{
				return m_uboDescriptorSet;
			}
		}

		public int UBODescriptorSetIndex
		{
			get
			{
				return 0;
			}
		}

		public int NumVertices
		{
			get
			{
				return 6;
			}
		}

		public IComponentExplicitVulkanMaterial ExplicitMaterial
		{
			get
			{
				return m_defaultMaterialWrapper;
			}
		}
	}
}
