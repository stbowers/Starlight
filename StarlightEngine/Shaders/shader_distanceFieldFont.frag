#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_KHR_vulkan_glsl: enable

// Inputs from vertex shader
layout(location = 0) in vec2 texCoord;

// Uniform inputs
layout(set = 1, binding = 1) uniform FontRenderSettings{
	vec4 textColor;
	vec4 outlineColor;
	vec2 outlineShift;
	float textWidth;
	float outlineWidth;
	float edge;
} fontSettings;

layout(set = 1, binding = 2) uniform sampler2D texSampler;

// Fragment output
layout(location = 0) out vec4 outColor;

void main() {
	float textDistance = 1.0f - texture(texSampler, texCoord).a;
	float outlineDistance = 1.0f - texture(texSampler, texCoord + fontSettings.outlineShift).a;

	float textAlpha = 1.0f - smoothstep(fontSettings.textWidth, fontSettings.textWidth + fontSettings.edge, textDistance);
	float outlineAlpha = 1.0f - smoothstep(fontSettings.outlineWidth, fontSettings.outlineWidth + fontSettings.edge, outlineDistance);

	float overallAplha = textAlpha + ((1.0f - textAlpha) * outlineAlpha);
	if (overallAplha == 0){
		discard;	
	}

	outColor = vec4(mix(fontSettings.outlineColor.xyz, fontSettings.textColor.xyz, textAlpha / overallAplha), overallAplha);
}