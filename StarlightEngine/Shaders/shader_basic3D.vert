#version 450
#extension GL_ARB_separate_shader_objects : enable

// Vertex inputs
layout(location = 0) in vec3 inPos;
layout(location = 1) in vec2 inTexCoord;
layout(location = 2) in vec3 normal;

// Uniform inputs
layout(set = 0, binding = 0) uniform MVPBuffer{
	mat4 model;
	mat4 view;
	mat4 proj;
} ubo;

// Vertex output
out gl_PerVertex {
    vec4 gl_Position;
};

// Outputs passed to fragment shader
layout(location = 0) out vec2 texCoord;
layout(location = 1) out vec3 worldPosition;
layout(location = 2) out vec3 cameraPosition;
layout(location = 3) out vec3 normalVector;
layout(location = 4) out mat4 modelMatrix;

void main() {
	worldPosition = (ubo.model * vec4(inPos, 1.0f)).xyz;
    gl_Position = ubo.proj * ubo.view * vec4(worldPosition, 1.0f);

    texCoord = inTexCoord;
	cameraPosition = (inverse(ubo.view) * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;
    normalVector = (ubo.model * vec4(normal, 0.0f)).xyz;
    normalVector = normalize(normal);
    modelMatrix = ubo.model;
}