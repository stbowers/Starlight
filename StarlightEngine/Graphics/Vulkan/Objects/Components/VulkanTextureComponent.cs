using System;
using System.Drawing;
using System.Drawing.Imaging;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
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
        DescriptorSet m_textureSamplerSet;
        int m_setIndex;
        int m_binding;

        public VulkanTextureComponent(VulkanAPIManager apiManager, VulkanPipeline pipeline, string textureFile, bool useMipmaps, Filter magFilter, Filter minFilter, DescriptorSet textureSamplerSet, int setIndex, int binding)
        {
            m_apiManager = apiManager;
            m_pipeline = pipeline;
            m_renderPass = pipeline.GetRenderPass();
            m_textureSamplerSet = textureSamplerSet;
            m_setIndex = setIndex;
            m_binding = binding;

			System.Drawing.Image texture = System.Drawing.Image.FromFile(textureFile);
			Bitmap bitmap = new Bitmap(texture);

			BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

			int mipLevels = (useMipmaps) ? (int)System.Math.Floor(System.Math.Log(System.Math.Max(bitmap.Width, bitmap.Height), 2)) : 1;

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

			WriteDescriptorSet descriptorSetUpdate = new WriteDescriptorSet();
			descriptorSetUpdate.DstSet = m_textureSamplerSet;
			descriptorSetUpdate.DstBinding = binding;
			descriptorSetUpdate.DstArrayElement = 0;
			descriptorSetUpdate.DescriptorCount = 1;
			descriptorSetUpdate.DescriptorType = DescriptorType.CombinedImageSampler;
			descriptorSetUpdate.ImageInfo = new[] { imageInfo };

			m_textureSamplerSet.Parent.UpdateSets(new[] { descriptorSetUpdate });
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

        public void BindComponent(CommandBuffer commandBuffer, VulkanPipeline boundPipeline, RenderPass currentRenderPass, List<int> boundSets)
        {
            if (!boundSets.Contains(m_setIndex))
            {
                commandBuffer.CmdBindDescriptorSets(PipelineBindPoint.Graphics, boundPipeline.GetPipelineLayout(), m_setIndex, new[] { m_textureSamplerSet });
                boundSets.Add(m_setIndex);
            }
        }
    }
}
