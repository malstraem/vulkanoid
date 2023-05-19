#version 460

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec2 fragTexxoord;

layout(binding = 1) uniform sampler2D texSampler;

layout(location = 0) out vec4 outColor;

void main()
{
    //outColor = texture(texSampler, fragTexxoord);
	outColor = vec4(fragColor, 1);
}