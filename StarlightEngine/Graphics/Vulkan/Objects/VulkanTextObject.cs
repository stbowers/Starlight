using VulkanCore;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Vulkan.Objects.Components;
using StarlightEngine.Graphics.Vulkan.Memory;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Math;
using System.Collections.Generic;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    public class VulkanTextObject : IVulkanObject, IVulkanDrawableObject
    {
        VulkanAPIManager m_apiManager;
        VulkanPipeline m_pipeline;

        FVec2 m_position;
        float m_width;
        FMat4 m_modelMatrix;

        TextMesh m_textMesh;

        byte[] m_meshData;
        byte[] m_mvpData;
        byte[] m_fontSettingsData;

        VulkanManagedBuffer m_objectBuffer;

        VulkanDescriptorSet m_meshDescriptorSet;
        VulkanDescriptorSet m_materialDescriptorSet;

        VulkanMeshComponent m_mesh;
        VulkanTextureComponent m_texture;
        VulkanUniformBufferComponent m_mvpUniform;
        VulkanUniformBufferComponent m_fontSettingsUniform;
        IVulkanBindableComponent[] m_bindableComponents;

        IParent m_parent;

        // set if the text will be updated often, so the memory should be made transient
        bool m_transient;

        public VulkanTextObject(VulkanAPIManager apiManager, AngelcodeFont font, string text, int size, FVec2 location, float width, bool transient = false)
        {
            m_apiManager = apiManager;
            m_pipeline = StaticPipelines.pipeline_distanceFieldFont;

            this.Visible = true;
            m_transient = transient;
            m_transient = false;

            // Create text mesh
            //m_position = new FVec2(location.X() * (apiManager.GetSwapchainImageExtent().Width / 2.0f), location.Y() * (apiManager.GetSwapchainImageExtent().Height / 2.0f));
            m_position = location;
            m_width = width * (apiManager.GetSwapchainImageExtent().Width / 2);
            m_textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, text, new FVec2(0.0f, 0.0f), m_width);

            // Create object buffer
            int bufferAlignment = (int)m_apiManager.GetPhysicalDevice().GetProperties().Limits.MinUniformBufferOffsetAlignment;
            m_objectBuffer = new VulkanManagedBuffer(m_apiManager, bufferAlignment, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer | BufferUsages.UniformBuffer, MemoryProperties.None, MemoryProperties.DeviceLocal, m_transient);

            // Create descriptor sets
            m_meshDescriptorSet = m_pipeline.CreateDescriptorSet(0);
            m_materialDescriptorSet = m_pipeline.CreateDescriptorSet(1);

            // Create mesh component
            m_meshData = new byte[m_textMesh.meshBufferData.Length];
            System.Buffer.BlockCopy(m_textMesh.meshBufferData, 0, m_meshData, 0, m_textMesh.meshBufferData.Length);
            m_mesh = new VulkanMeshComponent(m_apiManager, m_pipeline, m_meshData, m_textMesh.vboOffset, m_textMesh.iboOffset, m_textMesh.numVertices, m_objectBuffer);

            // Create texture component
            VulkanTextureCreateInfo textureInfo = new VulkanTextureCreateInfo();
            textureInfo.APIManager = m_apiManager;
            textureInfo.FileName = "./assets/" + font.pages[0].file;
            textureInfo.EnableMipmap = false;
            textureInfo.MagFilter = Filter.Linear;
            textureInfo.MinFilter = Filter.Linear;
            VulkanTexture fontAtlas = VulkanTextureCache.GetTexture(font.face, textureInfo);
            m_texture = new VulkanTextureComponent(m_apiManager, m_pipeline, fontAtlas, m_materialDescriptorSet, 2);

            // Create mvp uniform buffer
            m_modelMatrix = FMat4.Translate(new FVec3(m_position.X(), m_position.Y(), 0.0f));
            //m_modelMatrix = FMat4.Identity;
            float depth = 1.0f;

            m_mvpData = new byte[(4 * 4 * 4) + (4)];
            System.Buffer.BlockCopy(m_modelMatrix.Bytes, 0, m_mvpData, 0, (int)m_modelMatrix.PrimativeSizeOf);
            System.Buffer.BlockCopy(new[] { depth }, 0, m_mvpData, 4 * 4 * 4, 4);
            m_mvpUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_mvpData, m_objectBuffer, m_meshDescriptorSet, 0);

            // Create font settings uniform buffer
            m_fontSettingsData = new byte[(2 * 4 * 4) + (2 * 4) + (3 * 4)];
            FVec4 textColor = new FVec4(1.0f, 1.0f, 1.0f, 1.0f);
            FVec4 outlineColor = new FVec4(0.0f, 0.0f, 0.0f, 0.0f);
            FVec2 outlineShift = new FVec2(0.0f, 0.0f);
            float textWidth = .5f;
            float outlineWidth = 0.6f;
            float edge = .2f;
            System.Buffer.BlockCopy(textColor.Bytes, 0, m_fontSettingsData, 0, 4 * 4);
            System.Buffer.BlockCopy(outlineColor.Bytes, 0, m_fontSettingsData, 4 * 4, 4 * 4);
            System.Buffer.BlockCopy(outlineShift.Bytes, 0, m_fontSettingsData, 8 * 4, 2 * 4);
            System.Buffer.BlockCopy(new[] { textWidth }, 0, m_fontSettingsData, 10 * 4, 1 * 4);
            System.Buffer.BlockCopy(new[] { outlineWidth }, 0, m_fontSettingsData, 11 * 4, 1 * 4);
            System.Buffer.BlockCopy(new[] { edge }, 0, m_fontSettingsData, 12 * 4, 1 * 4);
            m_fontSettingsUniform = new VulkanUniformBufferComponent(m_apiManager, m_pipeline, m_fontSettingsData, m_objectBuffer, m_materialDescriptorSet, 1);

            m_objectBuffer.WriteAllBuffers(true);

            m_bindableComponents = new IVulkanBindableComponent[] { m_mesh, m_texture, m_mvpUniform, m_fontSettingsUniform };

            Rect2D clipArea;
            clipArea.Offset.X = 0;
            clipArea.Offset.Y = 0;
            clipArea.Extent = m_apiManager.GetSwapchainImageExtent();
            ClipArea = clipArea;
        }

        public Rect2D ClipArea { get; set; }

        public void UpdateText(AngelcodeFont font, string newText, int size)
        {
            m_textMesh = AngelcodeFontLoader.CreateTextMesh(font, size, newText, m_position, m_width);
            int meshDataSize = m_textMesh.meshBufferData.Length;
            m_meshData = new byte[meshDataSize];
            System.Buffer.BlockCopy(m_textMesh.meshBufferData, 0, m_meshData, 0, meshDataSize);
            m_mesh.UpdateMesh(m_meshData, m_textMesh.vboOffset, m_textMesh.iboOffset, m_textMesh.numVertices);
        }

        public void Update()
        {
        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
            FMat4 mvp = projection * view * modelTransform * m_modelMatrix * (m_parent != null ? m_parent.UIScale : FMat4.Identity);

            System.Buffer.BlockCopy(mvp.Bytes, 0, m_mvpData, 0, (int)mvp.PrimativeSizeOf);
            m_mvpUniform.UpdateUniformBuffer(m_mvpData);
        }

        public void SetParent(IParent parent)
        {
            m_parent = parent;
            UpdateMVPData(m_parent.Projection, m_parent.View, m_parent.Model);
        }

        public RenderPass[] RenderPasses
        {
            get
            {
                return new[] { m_pipeline.GetRenderPass() };
            }
        }

        public VulkanPipeline[] Pipelines
        {
            get
            {
                return new[] { m_pipeline };
            }
        }

        public IVulkanBindableComponent[][] BindableComponents
        {
            get
            {
                return new[] { m_bindableComponents };
            }
        }

        public void Draw(CommandBuffer commandBuffer, int swapchainIndex)
        {
            m_mesh.DrawMesh(commandBuffer, swapchainIndex);
        }

        public bool Visible { get; set; }

        public (EventManager.HandleEventDelegate, EventType)[] EventListeners
        {
            get
            {
                return new(EventManager.HandleEventDelegate, EventType)[] { };
            }
        }
    }
}
