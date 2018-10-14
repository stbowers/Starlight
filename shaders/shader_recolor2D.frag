#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_KHR_vulkan_glsl: enable

// Inputs from vertex shader
layout(location = 0) in vec2 texCoord;

// Uniform inputs
layout(set = 1, binding = 1) uniform RecolorSettings{
	vec4 fromColor1;
	vec4 toColor1;
	vec4 fromColor2;
	vec4 toColor2;
} recolorSettings;

layout(set = 1, binding = 2) uniform sampler2D texSampler;

// Fragment output
layout(location = 0) out vec4 outColor;

bool areVectorsSame(vec3 a, vec3 b, float maxDistance){
	bool same = true;

	if (abs(a.x - b.x) > maxDistance){
		same = false;
	}
	if (abs(a.y - b.y) > maxDistance){
		same = false;
	}
	if (abs(a.z - b.z) > maxDistance){
		same = false;
	}

	return same;
}

void main() {
	vec4 inColor = texture(texSampler, texCoord);
	if (areVectorsSame(inColor.xyz, recolorSettings.fromColor1.xyz, .01)){
		outColor = vec4(recolorSettings.toColor1.xyz, inColor.a);
	} else if (areVectorsSame(inColor.xyz, recolorSettings.fromColor2.xyz, .01)){
		outColor = vec4(recolorSettings.toColor2.xyz, inColor.a);
	} else {
		outColor = inColor;
	}
}