#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_KHR_vulkan_glsl: enable

// Inputs from vertex shader
layout(location = 0) in vec4 color;

// Fragment output
layout(location = 0) out vec4 outColor;

void main() {
	outColor = color;
}