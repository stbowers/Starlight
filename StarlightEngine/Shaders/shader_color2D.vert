#version 450
#extension GL_ARB_separate_shader_objects : enable

// Vertex inputs
layout(location = 0) in vec2 inPos;
layout(location = 1) in vec4 color;

// Uniform inputs
layout(set = 0, binding = 0) uniform MVPBuffer{
	mat4 mvp;
	float depth;
} ubo;

// Vertex output
out gl_PerVertex {
    vec4 gl_Position;
};

// Outputs passed to fragment shader
layout(location = 0) out vec4 outColor;

void main() {
    gl_Position = ubo.mvp * vec4(inPos, ubo.depth, 1.0f);

    outColor = color;
}