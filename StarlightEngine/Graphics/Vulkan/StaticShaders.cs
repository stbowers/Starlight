using System;
using StarlightEngine.Graphics.Vulkan;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
    /* Static class to keep track of shaders our program uses, so they only need to be allocated once, and can be loaded at the start of the program
     */
    public static class StaticShaders
    {
        /* A basic 3d renderer with per-fragment lighting
         * Vertex inputs:
         *      vec3 vertPosition
         *      vec2 textureCoordinate
         *      vec3 vertNormal
         *      
         * Uniforms:
         *      (set 0, binding 0) mvp buffer (vertex)
         *          mat4 model
         *          mat4 view
         *          mat4 projection
         *          
         *      (set 1, binding 1) lighting settings (vertex, fragment)
         *          vec4 lightPosition (only .xyz is used)
         *          vec4 lightColor (only .xyz is used)
         *          float ambientLight
         *          float shineDamper
         *          float reflectivity
         *          
         *      (set 1, binding 2) texture sampler (fragment)
         */
        public static VulkanShader shader_basic3D = null;
        public static void LoadShaderBasic3D(VulkanAPIManager apiManager)
        {
            if (shader_basic3D != null) return;

            VulkanShader.ShaderCreateInfo shaderCreateInfo = new VulkanShader.ShaderCreateInfo();
            shaderCreateInfo.vertexShaderFile = "./Shaders/shader_basic3D.vert.spv";
            shaderCreateInfo.vertexEntryPoint = "main";
            shaderCreateInfo.fragmentShaderFile = "./Shaders/shader_basic3D.frag.spv";
            shaderCreateInfo.fragmentEntryPoint = "main";

            shaderCreateInfo.inputs = new[] {
                new[] {
                    ShaderTypes.vec3,
                    ShaderTypes.vec2,
                    ShaderTypes.vec3
                }
            };

            ShaderUniformInputInfo mvpBufferInfo = new ShaderUniformInputInfo();
            mvpBufferInfo.set = 0;
            mvpBufferInfo.binding = 0;
            mvpBufferInfo.type = DescriptorType.UniformBuffer;
            mvpBufferInfo.stage = ShaderStages.Vertex;

            ShaderUniformInputInfo lightingInfo = new ShaderUniformInputInfo();
            lightingInfo.set = 1;
            lightingInfo.binding = 1;
            lightingInfo.type = DescriptorType.UniformBuffer;
            lightingInfo.stage = ShaderStages.Fragment;

            ShaderUniformInputInfo textureSamplerInfo = new ShaderUniformInputInfo();
            textureSamplerInfo.set = 1;
            textureSamplerInfo.binding = 2;
            textureSamplerInfo.type = DescriptorType.CombinedImageSampler;
            textureSamplerInfo.stage = ShaderStages.Fragment;

            shaderCreateInfo.uniformInputInfos = new[] {
                mvpBufferInfo,
                lightingInfo,
                textureSamplerInfo,
            };

            shader_basic3D = apiManager.CreateShader(shaderCreateInfo);
        }

        /* A basic shader to render text using a distance field font atlas
         * Vertex inputs:
         *      vec2 vertexPosition
         *      vec2 textureCoordinate
         *      
         * Uniforms:
         *      (set 0, binding 0) mvp buffer (vertex)
         *          mat4 mvp
         *          float depth
         *          
         *      (set 1, binding 1) font settings buffer (fragment)
         *          vec4 textColor (only .xyz is used)
         *          vec4 outlineColor (only .xyz is used)
         *          vec2 outlineShift
         *          float textwidth
         *          float outlineWidth
         *          float edge
         *          
         *      (set 1, binding 2) texture sampler (fragment)
         */
        public static VulkanShader shader_distanceFieldFont = null;
        public static void LoadShaderDistanceFieldFont(VulkanAPIManager apiManager)
        {
            if (shader_distanceFieldFont != null) return;

            VulkanShader.ShaderCreateInfo shaderCreateInfo = new VulkanShader.ShaderCreateInfo();
            shaderCreateInfo.vertexShaderFile = "./Shaders/shader_distanceFieldFont.vert.spv";
            shaderCreateInfo.vertexEntryPoint = "main";
            shaderCreateInfo.fragmentShaderFile = "./Shaders/shader_distanceFieldFont.frag.spv";
            shaderCreateInfo.fragmentEntryPoint = "main";

            shaderCreateInfo.inputs = new[] {
                new[] {
                    ShaderTypes.vec2,
                    ShaderTypes.vec2,
                }
            };

            ShaderUniformInputInfo mvpBufferInfo = new ShaderUniformInputInfo();
            mvpBufferInfo.set = 0;
            mvpBufferInfo.binding = 0;
            mvpBufferInfo.type = DescriptorType.UniformBuffer;
            mvpBufferInfo.stage = ShaderStages.Vertex;

            ShaderUniformInputInfo fontSettingsBufferInfo = new ShaderUniformInputInfo();
            fontSettingsBufferInfo.set = 1;
            fontSettingsBufferInfo.binding = 1;
            fontSettingsBufferInfo.type = DescriptorType.UniformBuffer;
            fontSettingsBufferInfo.stage = ShaderStages.Fragment;

            ShaderUniformInputInfo textureSamplerInfo = new ShaderUniformInputInfo();
            textureSamplerInfo.set = 1;
            textureSamplerInfo.binding = 2;
            textureSamplerInfo.type = DescriptorType.CombinedImageSampler;
            textureSamplerInfo.stage = ShaderStages.Fragment;

            shaderCreateInfo.uniformInputInfos = new[] {
                mvpBufferInfo,
                fontSettingsBufferInfo,
                textureSamplerInfo,
            };

            shader_distanceFieldFont = apiManager.CreateShader(shaderCreateInfo);
        }

		/* A basic shader to render 2d sprites
         * Vertex inputs:
         *      vec2 vertexPosition
         *      vec2 textureCoordinate
         *      
         * Uniforms:
         *      (set 0, binding 0) mvp buffer (vertex)
         *          mat4 mvp
         *          float depth
         *          
         *      (set 1, binding 1) texture sampler (fragment)
         */
		public static VulkanShader shader_basic2D = null;
		public static void LoadShaderBasic2D(VulkanAPIManager apiManager)
		{
			if (shader_basic2D != null) return;

			VulkanShader.ShaderCreateInfo shaderCreateInfo = new VulkanShader.ShaderCreateInfo();
			shaderCreateInfo.vertexShaderFile = "./Shaders/shader_basic2D.vert.spv";
			shaderCreateInfo.vertexEntryPoint = "main";
			shaderCreateInfo.fragmentShaderFile = "./Shaders/shader_basic2D.frag.spv";
			shaderCreateInfo.fragmentEntryPoint = "main";

			shaderCreateInfo.inputs = new[] {
				new[] {
					ShaderTypes.vec2,
					ShaderTypes.vec2,
				}
			};

			ShaderUniformInputInfo mvpBufferInfo = new ShaderUniformInputInfo();
			mvpBufferInfo.set = 0;
			mvpBufferInfo.binding = 0;
			mvpBufferInfo.type = DescriptorType.UniformBuffer;
			mvpBufferInfo.stage = ShaderStages.Vertex;

			ShaderUniformInputInfo textureSamplerInfo = new ShaderUniformInputInfo();
			textureSamplerInfo.set = 1;
			textureSamplerInfo.binding = 1;
			textureSamplerInfo.type = DescriptorType.CombinedImageSampler;
			textureSamplerInfo.stage = ShaderStages.Fragment;

			shaderCreateInfo.uniformInputInfos = new[] {
				mvpBufferInfo,
				textureSamplerInfo,
			};

			shader_basic2D = apiManager.CreateShader(shaderCreateInfo);
		}

		/* A basic shader to render 2d sprites, but with an additional uniform buffer to swap out up to two colors in the texture
         * Vertex inputs:
         *      vec2 vertexPosition
         *      vec2 textureCoordinate
         *      
         * Uniforms:
         *      (set 0, binding 0) mvp buffer (vertex)
         *          mat4 mvp
         *          float depth
         *
         *      (set 1, binding 1) recolor settings (fragment)
         *          vec4 fromColor1 (only .xyz used)
         *          vec4 toColor1 (only .xyz used)
         *          vec4 fromColor2 (only .xyz used)
         *          vec4 toColor2 (only .xyz used)
         *          
         *      (set 1, binding 2) texture sampler (fragment)
         */
		public static VulkanShader shader_recolor2D = null;
		public static void LoadShaderRecolor2D(VulkanAPIManager apiManager)
		{
			if (shader_recolor2D != null) return;

			VulkanShader.ShaderCreateInfo shaderCreateInfo = new VulkanShader.ShaderCreateInfo();
			shaderCreateInfo.vertexShaderFile = "./Shaders/shader_recolor2D.vert.spv";
			shaderCreateInfo.vertexEntryPoint = "main";
			shaderCreateInfo.fragmentShaderFile = "./Shaders/shader_recolor2D.frag.spv";
			shaderCreateInfo.fragmentEntryPoint = "main";

			shaderCreateInfo.inputs = new[] {
				new[] {
					ShaderTypes.vec2,
					ShaderTypes.vec2,
				}
			};

			ShaderUniformInputInfo mvpBufferInfo = new ShaderUniformInputInfo();
			mvpBufferInfo.set = 0;
			mvpBufferInfo.binding = 0;
			mvpBufferInfo.type = DescriptorType.UniformBuffer;
			mvpBufferInfo.stage = ShaderStages.Vertex;

			ShaderUniformInputInfo recolorSettingsBufferInfo = new ShaderUniformInputInfo();
			recolorSettingsBufferInfo.set = 1;
			recolorSettingsBufferInfo.binding = 1;
			recolorSettingsBufferInfo.type = DescriptorType.UniformBuffer;
			recolorSettingsBufferInfo.stage = ShaderStages.Fragment;

			ShaderUniformInputInfo textureSamplerInfo = new ShaderUniformInputInfo();
			textureSamplerInfo.set = 1;
			textureSamplerInfo.binding = 2;
			textureSamplerInfo.type = DescriptorType.CombinedImageSampler;
			textureSamplerInfo.stage = ShaderStages.Fragment;

			shaderCreateInfo.uniformInputInfos = new[] {
				mvpBufferInfo,
                recolorSettingsBufferInfo,
				textureSamplerInfo,
			};

			shader_recolor2D = apiManager.CreateShader(shaderCreateInfo);
		}

		/* A basic shader to render colored 2d geometry
         * Vertex inputs:
         *      vec2 vertexPosition
         *      vec4 color
         *      
         * Uniforms:
         *      (set 0, binding 0) mvp buffer (vertex)
         *          mat4 mvp
         *          float depth
         */
		public static VulkanShader shader_color2D;
		public static void LoadShaderColor2D(VulkanAPIManager apiManager)
		{
			if (shader_color2D != null) return;

			VulkanShader.ShaderCreateInfo shaderCreateInfo = new VulkanShader.ShaderCreateInfo();
			shaderCreateInfo.vertexShaderFile = "./Shaders/shader_color2D.vert.spv";
			shaderCreateInfo.vertexEntryPoint = "main";
			shaderCreateInfo.fragmentShaderFile = "./Shaders/shader_color2D.frag.spv";
			shaderCreateInfo.fragmentEntryPoint = "main";

			shaderCreateInfo.inputs = new[] {
				new[] {
					ShaderTypes.vec2,
					ShaderTypes.vec4,
				}
			};

			ShaderUniformInputInfo mvpBufferInfo = new ShaderUniformInputInfo();
			mvpBufferInfo.set = 0;
			mvpBufferInfo.binding = 0;
			mvpBufferInfo.type = DescriptorType.UniformBuffer;
			mvpBufferInfo.stage = ShaderStages.Vertex;

			shaderCreateInfo.uniformInputInfos = new[] {
				mvpBufferInfo,
			};

			shader_color2D = apiManager.CreateShader(shaderCreateInfo);
		}

        /* Creates all shaders, which might take a while
         * (though usually not too long, the actually intensive part of loading the shaders will be done during pipeline creation)
         */
        public static void LoadAllShaders(VulkanAPIManager apiManager)
        {
            LoadShaderBasic3D(apiManager);
            LoadShaderDistanceFieldFont(apiManager);
			LoadShaderBasic2D(apiManager);
			LoadShaderColor2D(apiManager);
            LoadShaderRecolor2D(apiManager);
        }
    }
}
