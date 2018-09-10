using System;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;
using VulkanCore;
using StarlightEngine.Graphics.Fonts;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace StarlightGame
{
    public class BasicVulkanTextObject : IVulkanObject, IComponentVulkanMesh, IComponentExplicitVulkanMaterial
    {
        VulkanPipeline m_pipeline;
        DefaultComponentVulkanMesh m_meshComponent;
        DescriptorSet m_descriptorSet;

        VulkanCore.Image m_textureImage;
        ImageView m_textureImageView;
        VmaAllocation m_textureImageAllocation;
        Sampler m_textureImageSampler;

        VulkanCore.Buffer m_fontSettingsBuffer;
        VmaAllocation m_fontSettingsBufferAllocation;

        VulkanAPIManager m_apiManager;
        byte[] ubo;
        DescriptorSet m_uboSet;

        public BasicVulkanTextObject(VulkanAPIManager apiManager, VulkanPipeline pipeline, AngelcodeFont font, string text, int size)
        {
            m_pipeline = pipeline;
            m_apiManager = apiManager;

            TextMesh textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, text, new Vec2(-638.0f, -357.0f), 640.0f);
            
            Mat4 mvp = new Mat4(1.0f);
            mvp[0, 0] = 1.0f / 640.0f;
            mvp[1, 1] = 1.0f / 360.0f;
            float depth = 1.0f;
            ubo = new byte[(4 * 4 * 4) + 4];
            System.Buffer.BlockCopy(mvp.Bytes, 0, ubo, 0, (int)mvp.PrimativeSizeOf);
            System.Buffer.BlockCopy(new[] { depth }, 0, ubo, (int)mvp.PrimativeSizeOf, 4);

            Vec4 textColor = new Vec4(0.0f, 0.0f, 0.0f, 0.0f);
            Vec4 outlineColor = new Vec4(1.0f, 1.0f, 1.0f, 0.0f);
            Vec2 outlineShift = new Vec2(0.0f, 0.0f);
            float textWidth = .5f;
            float outlineWidth = 0.65f;
            float edge = .2f;
            byte[] fontSettings = new byte[(2 * 4 * 4) + (1 * 2 * 4) + (3 * 4)];
            System.Buffer.BlockCopy(textColor.Bytes, 0, fontSettings, 0, 4 * 4);
            System.Buffer.BlockCopy(outlineColor.Bytes, 0, fontSettings, 4 * 4, 4 * 4);
            System.Buffer.BlockCopy(outlineShift.Bytes, 0, fontSettings, 8 * 4, 2 * 4);
            System.Buffer.BlockCopy(new[] { textWidth }, 0, fontSettings, 10 * 4, 1 * 4);
            System.Buffer.BlockCopy(new[] { outlineWidth }, 0, fontSettings, 11 * 4, 1 * 4);
            System.Buffer.BlockCopy(new[] { edge }, 0, fontSettings, 12 * 4, 1 * 4);

            DescriptorSet uboSet = pipeline.GetShader().AllocateDescriptorSets(0, 1)[0];
            m_uboSet = uboSet;

            m_meshComponent = new DefaultComponentVulkanMesh(apiManager, textMesh.meshBufferData, ubo, textMesh.vboOffset, textMesh.iboOffset, textMesh.numVertices, uboSet, 0, 0);

            m_descriptorSet = pipeline.GetShader().AllocateDescriptorSets(1, 1)[0];

            // load texture and font settings into descriptor
			System.Drawing.Image texture = System.Drawing.Image.FromFile("./assets/Arial.png");
			Bitmap bitmap = new Bitmap(texture);

			BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

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

			apiManager.CreateImage2D(imageData.Width, imageData.Height, 1, Format.B8G8R8A8UNorm, ImageTiling.Optimal, ImageUsages.TransferSrc | ImageUsages.TransferDst | ImageUsages.Sampled, MemoryProperties.None, MemoryProperties.DeviceLocal, out m_textureImage, out m_textureImageAllocation);

			apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 1);
			apiManager.CopyBufferToImage(stagingBuffer, m_textureImage, imageData.Width, imageData.Height);
			apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, 1);

            long lightingBufferSize = 10 * 4;
            apiManager.CreateBuffer(lightingBufferSize, BufferUsages.UniformBuffer, MemoryProperties.HostVisible, MemoryProperties.DeviceLocal, out m_fontSettingsBuffer, out m_fontSettingsBufferAllocation);

            IntPtr mappedMemory = m_fontSettingsBufferAllocation.memory.Map(m_fontSettingsBufferAllocation.offset, m_fontSettingsBufferAllocation.size);
            Marshal.Copy(fontSettings, 0, mappedMemory, fontSettings.Length);
            m_fontSettingsBufferAllocation.memory.Unmap();

			ImageViewCreateInfo viewInfo = new ImageViewCreateInfo();
			viewInfo.ViewType = ImageViewType.Image2D;
			viewInfo.Format = Format.B8G8R8A8UNorm;
			viewInfo.SubresourceRange.AspectMask = ImageAspects.Color;
			viewInfo.SubresourceRange.BaseMipLevel = 0;
			viewInfo.SubresourceRange.LevelCount = 1;
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
			samplerInfo.MaxLod = 0.0f;

			m_textureImageSampler = apiManager.GetDevice().CreateSampler(samplerInfo);

			DescriptorImageInfo imageInfo = new DescriptorImageInfo();
			imageInfo.Sampler = m_textureImageSampler;
			imageInfo.ImageView = m_textureImageView;
			imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;

			WriteDescriptorSet descriptorSetUpdate = new WriteDescriptorSet();
			descriptorSetUpdate.DstSet = m_descriptorSet;
			descriptorSetUpdate.DstBinding = 2;
			descriptorSetUpdate.DstArrayElement = 0;
			descriptorSetUpdate.DescriptorCount = 1;
			descriptorSetUpdate.DescriptorType = DescriptorType.CombinedImageSampler;
			descriptorSetUpdate.ImageInfo = new[] { imageInfo };

            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo();
            bufferInfo.Buffer = m_fontSettingsBuffer;
            bufferInfo.Offset = 0;
            bufferInfo.Range = fontSettings.Length;

            WriteDescriptorSet fontSettingsDescriptorUpdate = new WriteDescriptorSet();
            fontSettingsDescriptorUpdate.DstSet = m_descriptorSet;
            fontSettingsDescriptorUpdate.DstBinding = 1;
            fontSettingsDescriptorUpdate.DstArrayElement = 0;
            fontSettingsDescriptorUpdate.DescriptorCount = 1;
            fontSettingsDescriptorUpdate.DescriptorType = DescriptorType.UniformBuffer;
            fontSettingsDescriptorUpdate.BufferInfo = new[] { bufferInfo };

			m_descriptorSet.Parent.UpdateSets(new[] { descriptorSetUpdate, fontSettingsDescriptorUpdate });
        }

        public void UpdateText(AngelcodeFont font, string newText, int size)
        {
            TextMesh textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, newText, new Vec2(-638.0f, -357.0f), 640.0f);
            m_meshComponent = new DefaultComponentVulkanMesh(m_apiManager, textMesh.meshBufferData, ubo, textMesh.vboOffset, textMesh.iboOffset, textMesh.numVertices, m_uboSet, 0, 0);
        }

        public VulkanPipeline Pipeline => m_pipeline;

        public IComponentExplicitVulkanMesh ExplicitMesh => m_meshComponent;

        public DescriptorSet DescriptorSet => m_descriptorSet;

        public int DescriptorSetIndex => 1;

        public IComponentExplicitVulkanMaterial ExplicitMaterial => this;

        public void Update()
        {
            // do update
        }
    }
}
