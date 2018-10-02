#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_KHR_vulkan_glsl: enable

// Inputs from vertex shader
layout(location = 0) in vec2 texCoord;
layout(location = 1) in vec3 worldPosition;
layout(location = 2) in vec3 cameraPosition;
layout(location = 3) in vec3 normalIn;
layout(location = 4) in mat4 model;

// Uniform inputs
layout(set = 1, binding = 1) uniform LightingSettings{
	vec4 lightPosition;
	vec4 lightColor;
	float ambientLight;
	float shineDamper;
	float reflectivity;
} lightingSettings;

layout(set = 1, binding = 2) uniform sampler2D texSampler;

// Fragment output
layout(location = 0) out vec4 outColor;

void main() {
	vec3 toLight = normalize(lightingSettings.lightPosition.xyz - worldPosition);
	vec3 toCamera = normalize(cameraPosition - worldPosition);
	vec3 normal = normalize((model * vec4(normalIn, 0.0f)).xyz);

    float brightness = dot(toLight, normal);

    vec3 reflectedLightDirection = reflect(-toLight, normal);
    reflectedLightDirection = normalize(reflectedLightDirection);

    float specFactor = dot(toCamera, reflectedLightDirection);
	vec3 diffuse = max(brightness, lightingSettings.ambientLight) * lightingSettings.lightColor.xyz;

    float specBrightness = pow(max(specFactor, 0.0f), lightingSettings.shineDamper);
    vec3 finalSpecular = lightingSettings.reflectivity * specBrightness * lightingSettings.lightColor.xyz;

    outColor = vec4(diffuse, 1.0f) * texture(texSampler, texCoord).rgba + vec4(finalSpecular, 1.0f);
}