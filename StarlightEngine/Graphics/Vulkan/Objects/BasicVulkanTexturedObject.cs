using System;
using System.Diagnostics;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Math;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    /* Loads an object from a .obj file with a texture
     * An example of creating a basic object using the component system
     */
    public class BasicVulkanTexturedObject: IVulkanObject, IVulkanDrawableObject
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;

        byte[] m_meshData;
        byte[] m_uboData;
        DescriptorSet m_meshDescriptorSet;
        DescriptorSet m_materialDescriptorSet;

		Stopwatch timer = new Stopwatch();

        BasicVulkanMaterial m_material;
        BasicVulkanMesh m_mesh;

        int m_numVertices;

        VulkanCore.Buffer m_uboBuffer;
        VmaAllocation m_uboBufferAllocation;
        int m_uboOffset;

        VulkanCore.Buffer m_lightingSettingsBuffer;
        VmaAllocation m_lightingBufferAllocation;

        public BasicVulkanTexturedObject(VulkanAPIManager apiManager, string objFile, string textureFile, VulkanPipeline pipeline)
        {
			timer.Start();

            m_apiManager = apiManager;
            WavefrontObject loadedObject = WavefrontModelLoader.LoadFile(objFile);
            m_numVertices = loadedObject.Indices.Length;

			m_pipeline = pipeline;

			// ubo
			Mat4 model = Mat4.Rotate((timer.ElapsedMilliseconds / 1000.0f) * ((float)System.Math.PI / 4), new Vec3(0.0f, 1.0f, 0.0f));
            Mat4 view = Mat4.LookAt(new Vec3(1.0f, 0.0f, 0.0f), new Vec3(0.0f, 0.0f, 0.0f), new Vec3(0.0f, 1.0f, 0.0f));
            Mat4 proj = Mat4.Perspective((float)System.Math.PI / 2, m_apiManager.GetSwapchainImageExtent().Width / m_apiManager.GetSwapchainImageExtent().Height, 0.1f, 10.0f);
            proj[1, 1] *= -1f;

			m_uboData = new byte[3 * model.PrimativeSizeOf];
			model.Bytes.CopyTo(m_uboData, 0);
			view.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf);
			proj.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf + view.PrimativeSizeOf);

            m_meshData = new byte[loadedObject.VertexData.Length + (loadedObject.Indices.Length * 4)];
            System.Buffer.BlockCopy(loadedObject.VertexData, 0, m_meshData, 0, loadedObject.VertexData.Length);
            System.Buffer.BlockCopy(loadedObject.Indices, 0, m_meshData, loadedObject.VertexData.Length, loadedObject.Indices.Length * 4);

            m_meshDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(0, 1)[0];
            m_materialDescriptorSet = m_pipeline.GetShader().AllocateDescriptorSets(1, 1)[0];

            m_material = new BasicVulkanMaterial(m_apiManager, m_pipeline, textureFile, m_materialDescriptorSet, 1, 2);

            int[] uboOffsets;
            m_mesh = new BasicVulkanMesh(m_apiManager, m_pipeline, m_meshData, 0, loadedObject.VertexData.Length, new[] { m_uboData }, new[] { m_meshDescriptorSet }, new[] { 0 }, out m_uboBuffer, out m_uboBufferAllocation, out uboOffsets);
            m_uboOffset = uboOffsets[0];

            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
            bufferInfo.Buffer = m_uboBuffer;
            bufferInfo.Offset = m_uboOffset;
            bufferInfo.Range = m_uboData.Length;

            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
            descriptorWrite.DstSet = m_meshDescriptorSet;
            descriptorWrite.DstBinding = 0;
            descriptorWrite.DstArrayElement = 0;
            descriptorWrite.DescriptorCount = 1;
            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
            descriptorWrite.BufferInfo = new[] { bufferInfo };

            m_meshDescriptorSet.Parent.UpdateSets(new[] { descriptorWrite });

            long lightingBufferSize = 11 * 4;
            apiManager.CreateBuffer(lightingBufferSize, BufferUsages.UniformBuffer, MemoryProperties.HostVisible, MemoryProperties.DeviceLocal, out m_lightingSettingsBuffer, out m_lightingBufferAllocation);

            byte[] lightingBuffer = new byte[lightingBufferSize];
            Vec4 lightPosition = new Vec4(1.0f, 1.0f, 1.0f, 0.0f);
            Vec4 lightColor = new Vec4(1.0f, 1.0f, 1.0f, 0.0f);
            float[] settings = { 0.15f, 50.0f, 1.0f };
            System.Buffer.BlockCopy(lightPosition.Bytes, 0, lightingBuffer, 0, 4 * 4);
            System.Buffer.BlockCopy(lightColor.Bytes, 0, lightingBuffer, 4 * 4, 4 * 4);
            System.Buffer.BlockCopy(settings, 0, lightingBuffer, 8 * 4, 3 * 4);

            IntPtr mappedMemory = m_lightingBufferAllocation.memory.Map(m_lightingBufferAllocation.offset, m_lightingBufferAllocation.size);
            Marshal.Copy(lightingBuffer, 0, mappedMemory, (int)lightingBufferSize);
            m_lightingBufferAllocation.memory.Unmap();

            DescriptorBufferInfo lightingBufferInfo = new DescriptorBufferInfo();
            lightingBufferInfo.Buffer = m_lightingSettingsBuffer;
            lightingBufferInfo.Offset = 0;
            lightingBufferInfo.Range = lightingBufferSize;

            WriteDescriptorSet lightingDescriptorUpdate = new WriteDescriptorSet();
            lightingDescriptorUpdate.DstSet = m_materialDescriptorSet;
            lightingDescriptorUpdate.DstBinding = 1;
            lightingDescriptorUpdate.DstArrayElement = 0;
            lightingDescriptorUpdate.DescriptorCount = 1;
            lightingDescriptorUpdate.DescriptorType = DescriptorType.UniformBuffer;
            lightingDescriptorUpdate.BufferInfo = new[] { lightingBufferInfo };

			m_materialDescriptorSet.Parent.UpdateSets(new[] { lightingDescriptorUpdate });
        }

		public void Update()
		{
			// update ubo
			Mat4 model = Mat4.Rotate((timer.ElapsedMilliseconds / 1000.0f) * ((float)System.Math.PI / 4), new Vec3(0.0f, 1.0f, 0.0f));
			Mat4 view = Mat4.LookAt(new Vec3(10.0f, 10.0f, 10.0f), new Vec3(0.0f, 4.0f, 0.0f), new Vec3(0.0f, 1.0f, 0.0f));
			Mat4 proj = Mat4.Perspective( (float)(75 * System.Math.PI) / 180, m_apiManager.GetSwapchainImageExtent().Width / m_apiManager.GetSwapchainImageExtent().Height, 0.1f, 20.0f);
			proj[1, 1] *= -1f;

			m_uboData = new byte[3 * model.PrimativeSizeOf];
			model.Bytes.CopyTo(m_uboData, 0);
			view.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf);
			proj.Bytes.CopyTo(m_uboData, model.PrimativeSizeOf + view.PrimativeSizeOf);

            IntPtr mappedMemory = m_uboBufferAllocation.memory.Map(m_uboBufferAllocation.offset, m_uboBufferAllocation.size);
            Marshal.Copy(m_uboData, 0, (mappedMemory + m_uboOffset), m_uboData.Length);
            m_uboBufferAllocation.memory.Unmap();

            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
            bufferInfo.Buffer = m_uboBuffer;
            bufferInfo.Offset = m_uboOffset;
            bufferInfo.Range = m_uboData.Length;

            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
            descriptorWrite.DstSet = m_meshDescriptorSet;
            descriptorWrite.DstBinding = 0;
            descriptorWrite.DstArrayElement = 0;
            descriptorWrite.DescriptorCount = 1;
            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
            descriptorWrite.BufferInfo = new[] { bufferInfo };

            m_meshDescriptorSet.Parent.UpdateSets(new[] { descriptorWrite });
		}

        public VulkanPipeline Pipeline
        {
            get
            {
                return m_pipeline;
            }
        }

        public IVulkanBindableComponent[] BindableComponents
        {
            get
            {
                return new IVulkanBindableComponent[] { m_material, m_mesh };
            }
        }

        public void Draw(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets)
        {
            commandBuffer.CmdDrawIndexed(m_numVertices);
        }
    }
}
