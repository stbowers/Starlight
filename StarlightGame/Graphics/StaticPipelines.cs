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
         * Clear color: yes
         * Clear depth: yes
         */
        public static VulkanPipeline pipeline_basic3D;
        public static void LoadPipelineBasic3D(VulkanAPIManager apiManager)
        {
            VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();

            // make sure shader is loaded
            StaticShaders.LoadShaderBasic3D(apiManager);
            pipelineCreateInfo.shader = StaticShaders.shader_basic3D;

            pipelineCreateInfo.topology = PrimitiveTopology.TriangleList;
            pipelineCreateInfo.primitiveRestartEnable = false;
            pipelineCreateInfo.polygonMode = PolygonMode.Fill;

            pipelineCreateInfo.frontFaceCCW = true;

            pipelineCreateInfo.depthTestEnable = true;
            pipelineCreateInfo.depthWriteEnable = true;

            pipelineCreateInfo.clearColorAttachment = true;
            pipelineCreateInfo.clearDepthAttachment = true;

            pipeline_basic3D = apiManager.CreatePipeline(pipelineCreateInfo);
        }

        /* A basic 2d font renderer for distance field fonts
         * Shader: distanceFieldFont
         * Primatives: triangle list
         * Primative restart: no
         * Polygon mode: fill
         * Depth test: yes
         * Depth writes: yes
         * Clear color: yes
         * Clear depth: yes
         */
        public static VulkanPipeline pipeline_distanceFieldFont;
        public static void LoadPipelineDistanceFieldFont(VulkanAPIManager apiManager)
        {
            VulkanPipeline.VulkanPipelineCreateInfo pipelineCreateInfo = new VulkanPipeline.VulkanPipelineCreateInfo();

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

        /* Load the pipelines that can be loaded fairly quickly. This is useful for loading pipelines required to make a loading screen, so that can be shown almost instantly
         * while the other pipelines load in (as pipeline creation for some of the more complex pipelines can be very resource intensive and slow)
         */
        public static void LoadLowImpactPipelines(VulkanAPIManager apiManager)
        {

        }

        /* Load all pipelines. This might take some time to complete, depending on the complexity of the pipelines being loaded, and more importantly the complexity
         * of the shaders being used, since Vulkan compiles and uploads the shaders during the pipeline creation process.
         */
        public static void LoadAllPipelines(VulkanAPIManager apiManager)
        {
            LoadPipelineBasic3D(apiManager);
            LoadPipelineDistanceFieldFont(apiManager);
        }
    }
}
