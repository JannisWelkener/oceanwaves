#version 330
precision highp float;

// input from VAO-Datenstruktur
in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

// "modelview_projection_matrix" parameter of type Matrix4
uniform mat4 modelview_projection_matrix;

// "modelview_matrix" parameter of type Matrix4
uniform mat4 modelview_matrix;

// "uv_parameter" is given to the fragment-shader, declared as "out"
out vec2 uv_parameter;

void main()
{
	// "uv_parameter" (texture coordinate) is given to the fragment-shader
	uv_parameter = (modelview_matrix * vec4(in_normal, 0)).xy * 0.5 + 0.5;

	uv_parameter.y = 1 - uv_parameter.y;

	// final position of vertex ("modelview_projection_matrix" * "in_position")
	gl_Position = modelview_projection_matrix * vec4(in_position, 1);
}


