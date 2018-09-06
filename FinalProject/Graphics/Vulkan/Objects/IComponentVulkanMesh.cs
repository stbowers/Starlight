using System;
using VulkanCore;
using System.Runtime.InteropServices;

namespace FinalProject.Graphics.Vulkan.Objects
{
    /* Component that can be added to IGraphicsObjects, signalling that this object has a mesh to be rendered.
	 */
    public interface IComponentVulkanMesh
    {
        /* Reference to an explicit vulkan mesh, so that a basic object can return an instnace of a wrapper for default functionality
         */
        IComponentExplicitVulkanMesh ExplicitMesh { get; }
    }

    /* Interface defines accessors for all info needed to render a mesh
     */
	public interface IComponentExplicitVulkanMesh
	{
		/* MeshBuffer should be a buffer allocated for all data in the mesh, including:
		 * VBO - Vertex data
		 * IBO - Index data
		 * UBO - Uniform data
		 * each section having an associated offset to access it
		 */
		VulkanCore.Buffer MeshBuffer { get; }
		int VBOOffset { get; }
		int IBOOffset { get; }
		int UBOOffset { get; }

		/* Descriptor set for the UBO
		 */
		DescriptorSet UBODescriptorSet { get; }
		int UBODescriptorSetIndex { get; }

		/* Number of vertices to draw from the mesh buffer
		 */
		int NumVertices { get; }
	}

    public class DefaultComponentVulkanMesh: IComponentExplicitVulkanMesh
    {
        VulkanAPIManager m_apiManager;
        VulkanCore.Buffer m_meshBuffer;
        VmaAllocation m_meshBufferAllocation;
        int m_vboOffset;
        int m_iboOffset;
        int m_uboOffset;
        int m_numVertices;
        DescriptorSet m_meshDescriptorSet;
        int m_meshDescriptorSetIndex;
        int m_uboDescriptorBinding;

        public DefaultComponentVulkanMesh(VulkanAPIManager apiManager, byte[] vertexData, byte[] uboData, int vboOffset, int iboOffset, int numVertices, DescriptorSet meshDescriptorSet, int meshDescriptorSetIndex, int uboDescriptorBinding)
        {
            m_apiManager = apiManager;
            m_meshDescriptorSet = meshDescriptorSet;
            m_meshDescriptorSetIndex = meshDescriptorSetIndex;
            m_uboDescriptorBinding = uboDescriptorBinding;

            // calculate size and offsets for vbo and ibo
            m_vboOffset = vboOffset;
            m_iboOffset = iboOffset;
            m_numVertices = numVertices;

            // calculate size and offset for ubo (must be alligned properly)
            int uboAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
            int padding = uboAlignment - ((vertexData.Length) % uboAlignment);
            m_uboOffset = vertexData.Length + padding;

            long meshBufferSize = vertexData.Length + uboData.Length + padding;
            byte[] meshBufferData = new byte[meshBufferSize];
            m_apiManager.CreateBuffer(meshBufferSize, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.HostVisible, MemoryProperties.DeviceLocal, out m_meshBuffer, out m_meshBufferAllocation);

            System.Buffer.BlockCopy(vertexData, 0, meshBufferData, 0, vertexData.Length);
            System.Buffer.BlockCopy(uboData, 0, meshBufferData, vertexData.Length + padding, uboData.Length);

            IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
            Marshal.Copy(meshBufferData, 0, mappedMemory, (int)meshBufferSize);
            m_meshBufferAllocation.memory.Unmap();

            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
            bufferInfo.Buffer = m_meshBuffer;
            bufferInfo.Offset = m_uboOffset;
            bufferInfo.Range = uboData.Length;

            WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
            descriptorWrite.DstSet = m_meshDescriptorSet;
            descriptorWrite.DstBinding = m_uboDescriptorBinding;
            descriptorWrite.DstArrayElement = 0;
            descriptorWrite.DescriptorCount = 1;
            descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
            descriptorWrite.BufferInfo = new[] { bufferInfo };

            m_meshDescriptorSet.Parent.UpdateSets(new[] { descriptorWrite });
        }

		public void UpdateUBO(byte[] newUBO)
		{
            IntPtr mappedMemory = m_meshBufferAllocation.memory.Map(m_meshBufferAllocation.offset, m_meshBufferAllocation.size);
			Marshal.Copy(newUBO, 0, (mappedMemory + m_uboOffset), newUBO.Length);
			m_meshBufferAllocation.memory.Unmap();
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
                return m_vboOffset;
            }
        }

        public int IBOOffset
        {
            get
            {
                return m_iboOffset;
            }
        }

        public int UBOOffset
        {
            get
            {
                return m_uboOffset;
            }
        }

        public DescriptorSet UBODescriptorSet
        {
            get
            {
                return m_meshDescriptorSet;
            }
        }

        public int UBODescriptorSetIndex
        {
            get
            {
                return m_meshDescriptorSetIndex;
            }
        }

        public int NumVertices
        {
            get
            {
                return m_numVertices;
            }
        }
    }
}
