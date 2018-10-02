using System;
using System.Drawing;
using System.Drawing.Imaging;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Memory;
using System.Collections.Generic;

namespace StarlightEngine.Graphics.Vulkan.Objects.Components
{
    class VulkanTextureComponent : IVulkanBindableComponent
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;
        RenderPass m_renderPass;

        VulkanCore.Image m_textureImage;
        VmaAllocation m_textureImageAllocation;
        ImageView m_textureImageView;
        Sampler m_textureImageSampler;
        VulkanDescriptorSet m_textureSamplerSet;
        int m_binding;

		public VulkanTextureComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, string textureFile, bool useMipmaps, Filter magFilter, Filter minFilter, VulkanDescriptorSet descriptorSet, int binding)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_renderPass = pipeline.GetRenderPass();
			m_textureSamplerSet = descriptorSet;
            m_binding = binding;

			System.Drawing.Image texture = System.Drawing.Image.FromFile(textureFile);
			Bitmap bitmap = new Bitmap(texture);

			BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

			int mipLevels = (useMipmaps) ? (int)System.Math.Floor(System.Math.Log(System.Math.Max(bitmap.Width, bitmap.Height), 2)) : 1;

			VulkanCore.Buffer stagingBuffer;
			VmaAllocation stagingBufferAllocation;

			long imageSize = imageData.Width * imageData.Height * 4;

			apiManager.CreateBuffer(imageSize, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.HostCoherent, out stagingBuffer, out stagingBufferAllocation);

			IntPtr stagingBufferData = stagingBufferAllocation.MapAllocation();
			unsafe
			{
				System.Buffer.MemoryCopy(imageData.Scan0.ToPointer(), stagingBufferData.ToPointer(), imageSize, imageSize);
			}
			stagingBufferAllocation.UnmapAllocation();

			apiManager.CreateImage2D(imageData.Width, imageData.Height, mipLevels, Format.B8G8R8A8UNorm, ImageTiling.Optimal, ImageUsages.TransferSrc | ImageUsages.TransferDst | ImageUsages.Sampled, MemoryProperties.None, MemoryProperties.DeviceLocal, out m_textureImage, out m_textureImageAllocation);

			apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
			apiManager.CopyBufferToImage(stagingBuffer, m_textureImage, imageData.Width, imageData.Height);
			if (useMipmaps)
			{
				apiManager.GenerateMipmaps(m_textureImage, imageData.Width, imageData.Height, (uint)mipLevels);
			}
			else
			{
				apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, mipLevels);
			}

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
			samplerInfo.MagFilter = magFilter;
			samplerInfo.MinFilter = minFilter;
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

			DescriptorImageInfo imageInfo = new DescriptorImageInfo();
			imageInfo.Sampler = m_textureImageSampler;
			imageInfo.ImageView = m_textureImageView;
			imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;

			m_textureSamplerSet.UpdateImage(binding, imageInfo, DescriptorType.CombinedImageSampler, true);
        }

        public VulkanPipeline Pipeline
        {
            get
            {
                return m_pipeline;
            }
        }

        public RenderPass RenderPass
        {
            get
            {
                return m_renderPass;
            }
        }

        public void BindComponent(CommandBuffer commandBuffer, int swapchainIndex)
        {
			commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, m_pipeline.GetPipelineLayout(), m_textureSamplerSet.GetSetIndex(), new[] { m_textureSamplerSet.GetSet(swapchainIndex) });
        }
    }
}
