#version 330
precision highp float;

in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform mat4 modelview_projection_matrix;

uniform float wobbleValue;

out vec2 texcoord;

void main()
{
    texcoord = in_uv;

	vec4 v = vec4(in_position, 1);
	v.z = v.z + sin(5.0 * v.x + wobbleValue) * 0.5;

	gl_Position = modelview_projection_matrix * v;
}
