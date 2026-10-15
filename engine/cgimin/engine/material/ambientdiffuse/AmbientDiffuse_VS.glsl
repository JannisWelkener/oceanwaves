#version 330
precision highp float;

// input from VAO-Datenstruktur
in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

// modelview_projection_matrix as uniform parameter
uniform mat4 modelview_projection_matrix;

// "fragTexcoord" is passed to the fragment shader
out vec2 fragTexcoord;

// die Normale wird ebenfalls an den Fragment-Shader übergeben
out vec3 fragNormal;

void main()
{
	// "in_uv" (texture coordinate) is directly passed to fragment shader.
	fragTexcoord = in_uv;

	// the normal is also directly passed to fragment shader.
	fragNormal = in_normal;

	vec3 instPos = vec3(in_position.x + gl_InstanceID * 10.0f,in_position.y,in_position.z);

	// final position written to gl_Position ("modelview_projection_matrix" * "in_position")
	gl_Position = modelview_projection_matrix * vec4(instPos, 1);
}


