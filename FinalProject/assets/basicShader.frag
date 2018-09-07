#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(location = 0) in vec2 texCoord;
layout(location = 1) in vec3 toLight;
layout(location = 2) in vec3 toCamera;
layout(location = 3) in vec3 normal;
layout(location = 4) in mat4 model;

layout(set = 1, binding = 1) uniform sampler2D texSampler;

layout(location = 0) out vec4 outColor;

vec3 lightColor = vec3(1.0f, 1.0f, 1.0f);
float shineDamper = 50;
float reflectivity = 1;

void main() {
    float brightness = dot(normalize(toCamera), normalize((model * vec4(normal, 1.0f)).xyz));

    vec3 reflectedLightDirection = reflect(-toLight, normal);
    reflectedLightDirection = normalize(reflectedLightDirection);

    float specFactor = dot(toCamera, reflectedLightDirection);
	vec3 diffuse = max(brightness, 0.15f) * lightColor;

    float specBrightness = pow(max(specFactor, 0.0f), shineDamper);
    vec3 finalSpecular = reflectivity * specBrightness * lightColor;

    outColor = vec4(diffuse, 1.0f) * texture(texSampler, texCoord).rgba + vec4(finalSpecular, 1.0f);
}