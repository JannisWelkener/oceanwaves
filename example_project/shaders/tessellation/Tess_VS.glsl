#version 410 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv; 
out vec2 vs_uv;

void main()
{
    vs_uv = in_uv;
    gl_Position = vec4(in_position, 1.0);
}