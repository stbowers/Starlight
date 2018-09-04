using System;
using VulkanCore;

namespace FinalProject.Graphics.Vulkan
{
	/* Component that can be added to IGraphicsObjects 
	 */
	public interface IComponentVulkanMesh
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
}
