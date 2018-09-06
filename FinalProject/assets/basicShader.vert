#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (location = 0) in vec3 inPos;
layout (location = 1) in vec2 inTexCoord;
layout (location = 2) in vec3 normal;

layout (set = 0, binding = 0) uniform UniformBufferObject{
	mat4 model;
	mat4 view;
	mat4 proj;
} ubo;

layout (location = 0) out vec2 texCoord;
layout (location = 1) out float brightness;
layout (location = 2) out float specFactor;
out gl_PerVertex {
    vec4 gl_Position;
};

vec3 lightPosition = vec3(1.0f, 1.0f, 1.0f);

void main() {
	vec3 worldPosition = (ubo.model * vec4(inPos, 1.0f)).xyz;
    gl_Position = ubo.proj * ubo.view * vec4(worldPosition, 1.0f);

    texCoord = inTexCoord;
    brightness = dot(normalize(lightPosition - worldPosition), normalize((ubo.model * vec4(normal, 1.0f)).xyz));

    vec3 toCameraVector = (inverse(ubo.view) * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xyz - worldPosition.xyz;
    toCameraVector = normalize(toCameraVector);
    vec3 toLight = lightPosition.xyz - worldPosition.xyz;
    vec3 reflectedLightDirection = reflect(-toLight, normal);
    reflectedLightDirection = normalize(reflectedLightDirection);

    specFactor = dot(toCameraVector, reflectedLightDirection);
}