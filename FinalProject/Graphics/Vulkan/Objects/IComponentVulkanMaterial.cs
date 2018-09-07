using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using VulkanCore;

namespace FinalProject.Graphics.Vulkan.Objects
{
	/* This interface provides renderers a signal that they should try to bind a material before drawing a mesh, as well
	 * as providing the renderer with a way to get any data required for binding that material
	 */
	public interface IComponentVulkanMaterial
	{
		/* All materials must define a way to get an explicit material from them. This can be achieved by returning
		 * the material wrapped in the default implementation defined below, or by returning itself if it provides an
		 * explicit material implementation
		 */
		IComponentExplicitVulkanMaterial ExplicitMaterial { get; }
	}

	public interface IComponentExplicitVulkanMaterial
	{
		DescriptorSet DescriptorSet { get; }
		int DescriptorSetIndex { get; }
	}

	/* provides default implementation of IComponentVulkanTexture methods
	 */
	public class DefaultComponentVulkanMaterial: IComponentExplicitVulkanMaterial
	{
		VulkanCore.Image m_textureImage;
		VmaAllocation m_textureImageAllocation;
		ImageView m_textureImageView;
		Sampler m_textureImageSampler;
		DescriptorSet m_textureSamplerDescriptorSet;

		public DefaultComponentVulkanMaterial(IComponentVulkanMaterial material, VulkanAPIManager apiManager, VulkanShader fragShader, string textureFile)
		{
			System.Drawing.Image texture = System.Drawing.Image.FromFile(textureFile);
			Bitmap bitmap = new Bitmap(texture);

			BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

			int mipLevels = (int)System.Math.Floor(System.Math.Log(System.Math.Max(bitmap.Width, bitmap.Height), 2));

			VulkanCore.Buffer stagingBuffer;
			VmaAllocation stagingBufferAllocation;

			long imageSize = imageData.Width * imageData.Height * 4;

			apiManager.CreateBuffer(imageSize, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.HostCoherent, out stagingBuffer, out stagingBufferAllocation);

			IntPtr stagingBufferData = stagingBufferAllocation.memory.Map(stagingBufferAllocation.offset, stagingBufferAllocation.size);
			unsafe
			{
				System.Buffer.MemoryCopy(imageData.Scan0.ToPointer(), stagingBufferData.ToPointer(), imageSize, imageSize);
			}
			stagingBufferAllocation.memory.Unmap();

			apiManager.CreateImage2D(imageData.Width, imageData.Height, mipLevels, Format.B8G8R8A8UNorm, ImageTiling.Optimal, ImageUsages.TransferSrc | ImageUsages.TransferDst | ImageUsages.Sampled, MemoryProperties.None, MemoryProperties.DeviceLocal, out m_textureImage, out m_textureImageAllocation);

			apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
			apiManager.CopyBufferToImage(stagingBuffer, m_textureImage, imageData.Width, imageData.Height);
			apiManager.GenerateMipmaps(m_textureImage, imageData.Width, imageData.Height, (uint)mipLevels);

			ImageViewCreateInfo viewInfo = new ImageViewCreateInfo();
			viewInfo.ViewType = ImageViewType.Image2D;
			viewInfo.Format = Format.B8G8R8A8UNorm;
			viewInfo.SubresourceRange.AspectMask = ImageAspects.Color;
			viewInfo.SubresourceRange.BaseMipLevel = 0;
			viewInfo.SubresourceRange.LevelCount = mipLevels;
			viewInfo.SubresourceRange.BaseArrayLayer = 0;
			viewInfo.SubresourceRange.LayerCount = 1;

			m_textureImageView = m_textureImage.CreateView(viewInfo);

			SamplerCreateInfo samplerInfo = new SamplerCreateInfo();
			samplerInfo.MagFilter = Filter.Linear;
			samplerInfo.MinFilter = Filter.Linear;
			samplerInfo.AddressModeU = SamplerAddressMode.Repeat;
			samplerInfo.AddressModeV = SamplerAddressMode.Repeat;
			samplerInfo.AddressModeW = SamplerAddressMode.Repeat;
			samplerInfo.AnisotropyEnable = true;
			samplerInfo.MaxAnisotropy = 16;
			samplerInfo.BorderColor = BorderColor.IntOpaqueBlack;
			samplerInfo.UnnormalizedCoordinates = false;
			samplerInfo.CompareEnable = false;
			samplerInfo.CompareOp = CompareOp.Always;
			samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
			samplerInfo.MipLodBias = 0.0f;
			samplerInfo.MinLod = 0.0f;
			samplerInfo.MaxLod = mipLevels;

			m_textureImageSampler = apiManager.GetDevice().CreateSampler(samplerInfo);

			m_textureSamplerDescriptorSet = fragShader.AllocateDescriptorSets(1, 1)[0];

			DescriptorImageInfo imageInfo = new DescriptorImageInfo();
			imageInfo.Sampler = m_textureImageSampler;
			imageInfo.ImageView = m_textureImageView;
			imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;

			WriteDescriptorSet descriptorSetUpdate = new WriteDescriptorSet();
			descriptorSetUpdate.DstSet = m_textureSamplerDescriptorSet;
			descriptorSetUpdate.DstBinding = 1;
			descriptorSetUpdate.DstArrayElement = 0;
			descriptorSetUpdate.DescriptorCount = 1;
			descriptorSetUpdate.DescriptorType = DescriptorType.CombinedImageSampler;
			descriptorSetUpdate.ImageInfo = new[] { imageInfo };

			m_textureSamplerDescriptorSet.Parent.UpdateSets(new[] { descriptorSetUpdate });
		}

		public DescriptorSet DescriptorSet
		{
			get
			{
				return m_textureSamplerDescriptorSet;
			}
		}

		public int DescriptorSetIndex
		{
			get
			{
				return 1;
			}
		}
	}
}
