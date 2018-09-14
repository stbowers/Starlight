using System;
using System.Diagnostics;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
	public class VulkanPipeline
	{
		/* Contains info for the VulkanPipelineCreateInfo struct which can only be supplied by the api manager
		 */
		public struct ApiInfo
		{
			public Device device;
			public Extent2D extent;
			public Format swapchainImageFormat;
			public Format depthImageFormat;
			public int imageCount;
			public ImageView[] swapchainImageViews;
			public ImageView depthImageView;
		}

		public struct VulkanPipelineCreateInfo
		{
			// Should be left blank when creating a pipeline, filled in by calling apiManager.CreatePipeline()
			public ApiInfo apiInfo;

			public string name;

			public VulkanShader shader;

			public PrimitiveTopology topology;
			public bool primitiveRestartEnable;
            public PolygonMode polygonMode;

			public bool frontFaceCCW;

			public bool depthTestEnable;
			public bool depthWriteEnable;

			public bool clearColorAttachment;
			public bool clearDepthAttachment;
		}

		private ApiInfo m_apiInfo;
		private PipelineLayout m_pipelineLayout;
		private RenderPass m_renderPass;
		private Pipeline m_pipeline;
		private Framebuffer[] m_framebuffers;
		private VulkanShader m_shader;

		public VulkanPipeline(VulkanPipelineCreateInfo createInfo)
		{
			// Time how long it takes to make the pipeline
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			m_apiInfo = createInfo.apiInfo;
            m_shader = createInfo.shader;

			/* Create shader stages */
			// Programmable stages
			PipelineShaderStageCreateInfo vertShaderStageInfo = new PipelineShaderStageCreateInfo();
			vertShaderStageInfo.Stage = ShaderStages.Vertex;
			vertShaderStageInfo.Module = createInfo.shader.VertexModule;
			vertShaderStageInfo.Name = createInfo.shader.VertexEntryPoint;

			PipelineShaderStageCreateInfo fragShaderStageInfo = new PipelineShaderStageCreateInfo();
			fragShaderStageInfo.Stage = ShaderStages.Fragment;
			fragShaderStageInfo.Module = createInfo.shader.FragmentModule;
			fragShaderStageInfo.Name = createInfo.shader.FragmentEntryPoint;

			PipelineShaderStageCreateInfo[] shaderStages = { vertShaderStageInfo, fragShaderStageInfo };

			/* Configure pipeline fixed functions */
			// vertex input
			PipelineVertexInputStateCreateInfo vertexInputInfo = new PipelineVertexInputStateCreateInfo();
			vertexInputInfo.VertexBindingDescriptions = createInfo.shader.GetInputBindingDescriptions();
			vertexInputInfo.VertexAttributeDescriptions = createInfo.shader.GetInputAttributeDescriptions();

			// input assembly
			PipelineInputAssemblyStateCreateInfo inputAssembly = new PipelineInputAssemblyStateCreateInfo();
			inputAssembly.Topology = createInfo.topology;
			inputAssembly.PrimitiveRestartEnable = createInfo.primitiveRestartEnable;

			// viewport
			Viewport viewport = new Viewport();
			viewport.X = 0.0f;
			viewport.Y = 0.0f;
			viewport.Width = createInfo.apiInfo.extent.Width;
			viewport.Height = createInfo.apiInfo.extent.Height;
			viewport.MinDepth = 0.0f;
			viewport.MaxDepth = 1.0f;

			// scissor
			Rect2D scissor = new Rect2D();
			scissor.Offset = new Offset2D();
			scissor.Offset.X = 0;
			scissor.Offset.Y = 0;
			scissor.Extent = createInfo.apiInfo.extent;

			// viewport state
			PipelineViewportStateCreateInfo viewportState = new PipelineViewportStateCreateInfo();
			viewportState.Viewports = new[] { viewport };
			viewportState.Scissors = new[] { scissor };

			// rasterizer
			PipelineRasterizationStateCreateInfo rasterizer = new PipelineRasterizationStateCreateInfo();
			rasterizer.DepthClampEnable = false;
			rasterizer.RasterizerDiscardEnable = false;
			rasterizer.PolygonMode = createInfo.polygonMode;
			rasterizer.CullMode = CullModes.Back;
			rasterizer.FrontFace = (createInfo.frontFaceCCW) ? FrontFace.CounterClockwise : FrontFace.Clockwise;
			rasterizer.DepthBiasEnable = false;
			rasterizer.DepthBiasConstantFactor = 0.0f;
			rasterizer.DepthBiasSlopeFactor = 0.0f;
			rasterizer.LineWidth = 1.0f;

			// multisampling
			PipelineMultisampleStateCreateInfo multisampling = new PipelineMultisampleStateCreateInfo();
			multisampling.RasterizationSamples = SampleCounts.Count1;
			multisampling.SampleShadingEnable = false;
			multisampling.MinSampleShading = 1.0f;
			multisampling.SampleMask = null;
			multisampling.AlphaToCoverageEnable = false;
			multisampling.AlphaToOneEnable = false;

			// color blending
			PipelineColorBlendAttachmentState colorBlendAttachment = new PipelineColorBlendAttachmentState();
			colorBlendAttachment.BlendEnable = true;
			colorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
			colorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
			colorBlendAttachment.ColorBlendOp = BlendOp.Add;
			colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero;
			colorBlendAttachment.AlphaBlendOp = BlendOp.Add;
			colorBlendAttachment.ColorWriteMask = ColorComponents.All;

			PipelineColorBlendStateCreateInfo colorBlending = new PipelineColorBlendStateCreateInfo();
			colorBlending.LogicOpEnable = false;
			colorBlending.LogicOp = LogicOp.Copy;
			colorBlending.Attachments = new[] { colorBlendAttachment };
			colorBlending.BlendConstants.R = 0.0f;
			colorBlending.BlendConstants.G = 0.0f;
			colorBlending.BlendConstants.B = 0.0f;
			colorBlending.BlendConstants.A = 0.0f;

			// depth stencil
			PipelineDepthStencilStateCreateInfo depthStencil = new PipelineDepthStencilStateCreateInfo();
			depthStencil.DepthTestEnable = createInfo.depthTestEnable;
			depthStencil.DepthWriteEnable = createInfo.depthWriteEnable;
			depthStencil.DepthCompareOp = CompareOp.LessOrEqual;
			depthStencil.DepthBoundsTestEnable = false;
			depthStencil.StencilTestEnable = false;
			depthStencil.Front = new StencilOpState();
			depthStencil.Back = new StencilOpState();
			depthStencil.MinDepthBounds = 0.0f;
			depthStencil.MaxDepthBounds = 1.0f;

			/* Configure pipeline layout */
			List<long> setLayouts = new List<long>();
			setLayouts.AddRange(createInfo.shader.GetDescriptorSetLayouts());

			PipelineLayoutCreateInfo pipelineLayoutInfo = new PipelineLayoutCreateInfo();
			pipelineLayoutInfo.SetLayouts = setLayouts.ToArray();
			pipelineLayoutInfo.PushConstantRanges = null;

			m_pipelineLayout = m_apiInfo.device.CreatePipelineLayout(pipelineLayoutInfo);

			/* Create subpasses */
			// Attachment descriptions
			AttachmentDescription colorAttachment = new AttachmentDescription();
			colorAttachment.Format = m_apiInfo.swapchainImageFormat;
			colorAttachment.Samples = SampleCounts.Count1;
			colorAttachment.LoadOp = createInfo.clearColorAttachment ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			if (createInfo.clearColorAttachment)
			{
				colorAttachment.InitialLayout = ImageLayout.Undefined;
			}
			else
			{
				colorAttachment.InitialLayout = ImageLayout.PresentSrcKhr;
			}
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentReference colorAttachmentRef = new AttachmentReference();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentDescription depthAttachment = new AttachmentDescription();
			depthAttachment.Format = m_apiInfo.depthImageFormat;
			depthAttachment.Samples = SampleCounts.Count1;
			depthAttachment.LoadOp = createInfo.clearDepthAttachment ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load;
			depthAttachment.StoreOp = AttachmentStoreOp.Store;
			depthAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			if (createInfo.clearDepthAttachment)
			{
				depthAttachment.InitialLayout = ImageLayout.Undefined;
			}
			else
			{
				depthAttachment.InitialLayout = ImageLayout.DepthStencilAttachmentOptimal;
			}
			depthAttachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new AttachmentReference();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			// subpasses
			SubpassDescription subpass = new SubpassDescription();
			subpass.ColorAttachments = new[] { colorAttachmentRef };
			subpass.DepthStencilAttachment = depthAttachmentRef;

			/* Create render pass */
			SubpassDependency dependency = new SubpassDependency();
			dependency.SrcSubpass = Constant.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStages.ColorAttachmentOutput;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStages.ColorAttachmentOutput;
			dependency.DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite;

			RenderPassCreateInfo renderPassInfo = new RenderPassCreateInfo();
			renderPassInfo.Attachments = new[] { colorAttachment, depthAttachment };
			renderPassInfo.Subpasses = new[] { subpass };
			renderPassInfo.Dependencies = new[] { dependency };

			m_renderPass = m_apiInfo.device.CreateRenderPass(renderPassInfo);

			/* Create graphics pipeline */
			GraphicsPipelineCreateInfo pipelineInfo = new GraphicsPipelineCreateInfo();
			pipelineInfo.Stages = shaderStages;
			pipelineInfo.VertexInputState = vertexInputInfo;
			pipelineInfo.InputAssemblyState = inputAssembly;
			pipelineInfo.ViewportState = viewportState;
			pipelineInfo.RasterizationState = rasterizer;
			pipelineInfo.MultisampleState = multisampling;
			pipelineInfo.DepthStencilState = depthStencil;
			pipelineInfo.ColorBlendState = colorBlending;
			pipelineInfo.DynamicState = null;
			pipelineInfo.Layout = m_pipelineLayout;
			pipelineInfo.RenderPass = m_renderPass;
			pipelineInfo.Subpass = 0;
			pipelineInfo.BasePipelineHandle = null;
			pipelineInfo.BasePipelineIndex = -1;

			m_pipeline = m_apiInfo.device.CreateGraphicsPipeline(pipelineInfo);

			/* Create swapchain framebuffers */
			m_framebuffers = new Framebuffer[m_apiInfo.imageCount];
			for (int framebufferIndex = 0; framebufferIndex < m_framebuffers.Length; framebufferIndex++)
			{
				long[] attachments = {
					m_apiInfo.swapchainImageViews[framebufferIndex].Handle,
					m_apiInfo.depthImageView.Handle
				};

				FramebufferCreateInfo framebufferInfo = new FramebufferCreateInfo();
				framebufferInfo.Attachments = attachments;
				framebufferInfo.Width = m_apiInfo.extent.Width;
				framebufferInfo.Height = m_apiInfo.extent.Height;
				framebufferInfo.Layers = 1;

				m_framebuffers[framebufferIndex] = m_renderPass.CreateFramebuffer(framebufferInfo);
			}

			Console.WriteLine("Graphics pipeline created (Name: \"{0}\", {1} FBs allocated, took {2}ms)", createInfo.name, m_framebuffers.Length, stopwatch.ElapsedMilliseconds);
		}

		public RenderPass GetRenderPass()
		{
			return m_renderPass;
		}

		public Framebuffer GetFramebuffer(int i)
		{
			return m_framebuffers[i];
		}

		public Pipeline GetPipeline()
		{
			return m_pipeline;
		}

		public PipelineLayout GetPipelineLayout()
		{
			return m_pipelineLayout;
		}

		public VulkanShader GetShader()
		{
			return m_shader;
		}
	}
}
