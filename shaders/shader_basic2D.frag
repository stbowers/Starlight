#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_KHR_vulkan_glsl: enable

// Inputs from vertex shader
layout(location = 0) in vec2 texCoord;

// Uniform inputs
layout(set = 1, binding = 1) uniform sampler2D texSampler;

// Fragment output
layout(location = 0) out vec4 outColor;

void main() {
    vec4 textureColor = texture(texSampler, texCoord);
    if (textureColor.a < 0.01f){
        discard;
    }
    outColor = textureColor;
}