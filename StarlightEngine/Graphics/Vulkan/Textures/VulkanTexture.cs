using System;
using System.Drawing;
using System.Drawing.Imaging;
using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Memory;

namespace StarlightEngine.Graphics.Vulkan
{
    public struct VulkanTextureCreateInfo
    {
        public VulkanAPIManager APIManager;
        public string FileName;
        public bool EnableMipmap;

        public Filter MinFilter;
        public Filter MagFilter;

        public bool AnisotropyEnable;
        public int MaxAnisotropy;
    }

    public class VulkanTexture
    {
        #region Private Members
        VulkanAPIManager m_apiManager;

        VulkanCore.Image m_textureImage;
        VmaAllocation m_textureImageAllocation;
        ImageView m_textureImageView;
        Sampler m_textureImageSampler;
        #endregion

        public VulkanTexture(VulkanTextureCreateInfo info)
        {
            m_apiManager = info.APIManager;

            System.Drawing.Image texture = System.Drawing.Image.FromFile(info.FileName);
            Bitmap bitmap = new Bitmap(texture);

            BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

            int mipLevels = (info.EnableMipmap) ? (int)System.Math.Floor(System.Math.Log(System.Math.Max(bitmap.Width, bitmap.Height), 2)) : 1;

            VulkanCore.Buffer stagingBuffer;
            VmaAllocation stagingBufferAllocation;

            long imageSize = imageData.Width * imageData.Height * 4;

            m_apiManager.CreateBuffer(imageSize, BufferUsages.TransferSrc, MemoryProperties.HostVisible, MemoryProperties.HostCoherent, out stagingBuffer, out stagingBufferAllocation);

            IntPtr stagingBufferData = stagingBufferAllocation.MapAllocation();
            unsafe
            {
                System.Buffer.MemoryCopy(imageData.Scan0.ToPointer(), stagingBufferData.ToPointer(), imageSize, imageSize);
            }
            stagingBufferAllocation.UnmapAllocation();

            m_apiManager.CreateImage2D(imageData.Width, imageData.Height, mipLevels, Format.B8G8R8A8UNorm, ImageTiling.Optimal, ImageUsages.TransferSrc | ImageUsages.TransferDst | ImageUsages.Sampled, MemoryProperties.None, MemoryProperties.DeviceLocal, out m_textureImage, out m_textureImageAllocation);

            m_apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
            m_apiManager.CopyBufferToImage(stagingBuffer, m_textureImage, imageData.Width, imageData.Height);
            if (info.EnableMipmap)
            {
                m_apiManager.GenerateMipmaps(m_textureImage, imageData.Width, imageData.Height, (uint)mipLevels);
            }
            else
            {
                m_apiManager.TransitionImageLayout(m_textureImage, Format.B8G8R8A8UNorm, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, mipLevels);
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
            samplerInfo.MagFilter = info.MagFilter;
            samplerInfo.MinFilter = info.MinFilter;
            samplerInfo.AddressModeU = SamplerAddressMode.Repeat;
            samplerInfo.AddressModeV = SamplerAddressMode.Repeat;
            samplerInfo.AddressModeW = SamplerAddressMode.Repeat;
            samplerInfo.AnisotropyEnable = info.AnisotropyEnable;
            samplerInfo.MaxAnisotropy = info.MaxAnisotropy;
            samplerInfo.BorderColor = BorderColor.IntOpaqueBlack;
            samplerInfo.UnnormalizedCoordinates = false;
            samplerInfo.CompareEnable = false;
            samplerInfo.CompareOp = CompareOp.Always;
            samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
            samplerInfo.MipLodBias = 0.0f;
            samplerInfo.MinLod = 0.0f;
            samplerInfo.MaxLod = mipLevels;

            m_textureImageSampler = m_apiManager.GetDevice().CreateSampler(samplerInfo);
        }

        public Sampler Sampler
        {
            get
            {
                return m_textureImageSampler;
            }
        }

        public ImageView ImageView
        {
            get
            {
                return m_textureImageView;
            }
        }
    }
}