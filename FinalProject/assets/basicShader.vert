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
layout(location = 1) out vec3 toLight;
layout(location = 2) out vec3 toCamera;
layout(location = 3) out vec3 normalVector;
layout(location = 4) out mat4 modelMatrix;

out gl_PerVertex {
    vec4 gl_Position;
};

vec3 lightPosition = vec3(1.0f, 1.0f, 1.0f);

void main() {
	vec3 worldPosition = (ubo.model * vec4(inPos, 1.0f)).xyz;
    gl_Position = ubo.proj * ubo.view * vec4(worldPosition, 1.0f);

    texCoord = inTexCoord;
    toLight = lightPosition - worldPosition.xyz;
    toLight = normalize(toLight);
    toCamera = (inverse(ubo.view) * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xyz - worldPosition.xyz;
    toCamera = normalize(toCamera);
    normalVector = (ubo.model * vec4(normal, 0.0f)).xyz;
    normalVector = normalize(normal);
    modelMatrix = ubo.model;
}