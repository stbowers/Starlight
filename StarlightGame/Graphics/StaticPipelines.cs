using System;
using StarlightEngine.Graphics.Vulkan;
using VulkanCore;

namespace StarlightGame.Graphics
{
    /* Static class to keep track of the pipelines available to use, so that they only need to be created once and can be loaded together (probably while on a loading screen)
     */
    public static class StaticPipelines
    {
        /* A basic 3d renderer with per-fragment lighting
         * Shader: basic_3D
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
        public static VulkanPipeline pipeline_basic3D;
        public static void LoadPipelineBasic3D(VulkanAPIManager apiManager)
        {
			if (pipeline_basic3D != null) return;

            VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "basic3D";

            // make sure shader is loaded
            StaticShaders.LoadShaderBasic3D(apiManager);
            pipelineCreateInfo.shader = StaticShaders.shader_basic3D;

            pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
            pipelineCreateInfo.primitiveRestartEnable = false;
            pipelineCreateInfo.polygonMode = PolygonMode.Fill;

            pipelineCreateInfo.frontFaceCCW = true;

            pipelineCreateInfo.depthTestEnable = true;
            pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

            pipeline_basic3D = apiManager.CreatePipeline(pipelineCreateInfo);
        }

        /* A basic 2d font renderer for distance field fonts
         * Shader: distanceFieldFont
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
        public static VulkanPipeline pipeline_distanceFieldFont;
        public static void LoadPipelineDistanceFieldFont(VulkanAPIManager apiManager)
        {
			if (pipeline_distanceFieldFont != null) return;

            VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "distanceFieldFont";

            // make sure shader is loaded
            StaticShaders.LoadShaderDistanceFieldFont(apiManager);
            pipelineCreateInfo.shader = StaticShaders.shader_distanceFieldFont;

            pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
            pipelineCreateInfo.primitiveRestartEnable = false;
            pipelineCreateInfo.polygonMode = PolygonMode.Fill;

            pipelineCreateInfo.frontFaceCCW = false;

            pipelineCreateInfo.depthTestEnable = true;
            pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

            pipeline_distanceFieldFont = apiManager.CreatePipeline(pipelineCreateInfo);
        }

		/* A basic 2d sprite render pipeline
         * Shader: basic2D
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
		public static VulkanPipeline pipeline_basic2D;
		public static void LoadPipelineBasic2D(VulkanAPIManager apiManager)
		{
			if (pipeline_basic2D != null) return;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "basic2D";

			// make sure shader is loaded
			StaticShaders.LoadShaderBasic2D(apiManager);
			pipelineCreateInfo.shader = StaticShaders.shader_basic2D;

			pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
			pipelineCreateInfo.primitiveRestartEnable = false;
			pipelineCreateInfo.polygonMode = PolygonMode.Fill;

			pipelineCreateInfo.frontFaceCCW = false;

			pipelineCreateInfo.depthTestEnable = true;
			pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

			pipeline_basic2D = apiManager.CreatePipeline(pipelineCreateInfo);
		}

		/* A basic 2d sprite render pipeline, with the option to dynamically swap out colors in the texture
         * Shader: recolor2D
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
		public static VulkanPipeline pipeline_recolor2D;
		public static void LoadPipelineRecolor2D(VulkanAPIManager apiManager)
		{
			if (pipeline_recolor2D != null) return;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "recolor2D";

			// make sure shader is loaded
			StaticShaders.LoadShaderRecolor2D(apiManager);
			pipelineCreateInfo.shader = StaticShaders.shader_recolor2D;

			pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
			pipelineCreateInfo.primitiveRestartEnable = false;
			pipelineCreateInfo.polygonMode = PolygonMode.Fill;

			pipelineCreateInfo.frontFaceCCW = false;

			pipelineCreateInfo.depthTestEnable = true;
			pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

			pipeline_recolor2D = apiManager.CreatePipeline(pipelineCreateInfo);
		}

		/* A pipeline which clears the screen
		 * Shader: basic2D
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: no
         * Depth writes: no
         * Clear color: yes
         * Clear depth: yes
         */
		public static VulkanPipeline pipeline_clear;
		public static void LoadPipelineClear(VulkanAPIManager apiManager)
		{
			if (pipeline_clear != null) return;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "clear";

			// make sure shader is loaded
			StaticShaders.LoadShaderBasic2D(apiManager);
			pipelineCreateInfo.shader = StaticShaders.shader_basic2D;

			pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
			pipelineCreateInfo.primitiveRestartEnable = false;
			pipelineCreateInfo.polygonMode = PolygonMode.Fill;

			pipelineCreateInfo.frontFaceCCW = false;

			pipelineCreateInfo.depthTestEnable = false;
			pipelineCreateInfo.depthWriteEnable = false;

			pipelineCreateInfo.clearColorAttachment = true;
			pipelineCreateInfo.clearDepthAttachment = true;

			pipeline_clear = apiManager.CreatePipeline(pipelineCreateInfo);
		}

		/* A pipeline which draws colored 2d geometry
		 * Shader: basic2D
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
		public static VulkanPipeline pipeline_color2D;
		public static void LoadPipelineColor2D(VulkanAPIManager apiManager)
		{
			if (pipeline_color2D != null) return;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "color2D";

			// make sure shader is loaded
			StaticShaders.LoadShaderColor2D(apiManager);
			pipelineCreateInfo.shader = StaticShaders.shader_color2D;

			pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
			pipelineCreateInfo.primitiveRestartEnable = false;
			pipelineCreateInfo.polygonMode = PolygonMode.Fill;

			pipelineCreateInfo.frontFaceCCW = false;

			pipelineCreateInfo.depthTestEnable = true;
			pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

			pipeline_color2D = apiManager.CreatePipeline(pipelineCreateInfo);
		}

		/* A pipeline which draws colored lines
		 * Shader: color2D
         * Primatives: line list
         * Primative restart: no
         * Polygon mode: line
         * Depth test: yes
         * Depth writes: yes
         * Clear color: no
         * Clear depth: no
         */
		public static VulkanPipeline pipeline_colorLine;
		public static void LoadPipelineColorLine(VulkanAPIManager apiManager)
		{
			if (pipeline_colorLine != null) return;

			VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();
			pipelineCreateInfo.name = "colorLine";

			// make sure shader is loaded
			StaticShaders.LoadShaderColor2D(apiManager);
			pipelineCreateInfo.shader = StaticShaders.shader_color2D;

			pipelineCreateInfo.topology = PrimitiveTopology.LineList;
			pipelineCreateInfo.primitiveRestartEnable = false;
			pipelineCreateInfo.polygonMode = PolygonMode.Line;

			pipelineCreateInfo.frontFaceCCW = false;

			pipelineCreateInfo.depthTestEnable = true;
			pipelineCreateInfo.depthWriteEnable = true;

			pipelineCreateInfo.clearColorAttachment = false;
			pipelineCreateInfo.clearDepthAttachment = false;

			pipeline_colorLine = apiManager.CreatePipeline(pipelineCreateInfo);
		}

        /* Load all pipelines. This might take some time to complete, depending on the complexity of the pipelines being loaded, and more importantly the complexity
         * of the shaders being used, since Vulkan compiles and uploads the shaders during the pipeline creation process.
         */
        public static void LoadAllPipelines(VulkanAPIManager apiManager)
        {
            LoadPipelineBasic3D(apiManager);
            LoadPipelineDistanceFieldFont(apiManager);
			LoadPipelineBasic2D(apiManager);
			LoadPipelineClear(apiManager);
			LoadPipelineColor2D(apiManager);
			LoadPipelineColorLine(apiManager);
            LoadPipelineRecolor2D(apiManager);
        }
    }
}
