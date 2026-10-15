#version 330
precision highp float;

uniform sampler2D sampler; 

in vec2 uv_parameter;

out vec4 outputColor;

void main()
{
    outputColor = texture(sampler, uv_parameter);
}