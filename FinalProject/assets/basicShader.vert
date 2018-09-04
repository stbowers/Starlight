#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (location = 0) in vec2 inPos;
layout (location = 1) in vec2 inTexCoord;

layout (set = 0, binding = 0) uniform UniformBufferObject{
	mat4 mvp;
} ubo;

layout (location = 0) out vec2 texCoord;
out gl_PerVertex {
    vec4 gl_Position;
};

void main() {
    gl_Position = ubo.mvp * vec4(inPos, 0.0f, 1.0f);
    texCoord = inTexCoord;
}