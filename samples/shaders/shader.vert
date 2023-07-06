#version 460

layout(binding = 0) uniform ModelViewProjection
{
    mat4 model;
    mat4 view;
    mat4 proj;
} mvp;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec2 texcoord;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragTexcoord;

/*layout(push_constant) uniform Constants
{
    mat4 model;
} push;*/

void main()
{
    gl_Position = mvp.proj * mvp.view * mvp.model * vec4(position, 1.0);

    fragTexcoord = texcoord;
    fragColor = normal;
}
