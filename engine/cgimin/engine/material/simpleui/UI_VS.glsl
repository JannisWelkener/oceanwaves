#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_uv;

out vec2 uv;

uniform mat4 projection_matrix;
uniform mat4 transformation_matrix;
uniform float offsetX;
uniform float offsetY;

void main()
{
    gl_Position =
        projection_matrix *
        transformation_matrix *
        vec4(in_position.xy + vec2(offsetX,offsetY), 0.0, 1.0);

    uv = in_uv;
}