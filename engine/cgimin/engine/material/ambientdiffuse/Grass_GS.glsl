#version 330
precision highp float;

layout (triangles) in;
layout (triangle_strip, max_vertices = 3) out;

out vec3 fragNormal;
out vec2 fragTexcoord;
out vec2 blade_pos;

in DATA
{
	vec3 fragNormal;
	vec2 fragTexcoord;
	mat4 modelview_projection_matrix;
	vec2 blade_pos;
	vec2 world_position;
	vec2 camera_position;
} data_in[];

float vector_length(vec2 v){
	return sqrt(v.x*v.x + v.y*v.y);
}

void main()
{
	float render_distance = 30;

	int s = 0;
	for(int i=0;i<3;i++){
		vec2 individual_grass_position = data_in[i].world_position + data_in[i].blade_pos.yx * 5.0f;
		vec2 camToBlade = (-data_in[i].camera_position) - individual_grass_position;
		float len = vector_length(camToBlade);
		if(len < render_distance)s++;
		s++;
	}

	if(s>=3){
	for(int i=0;i<3;i++){
	/*
	
	if(cameraPos -> gl_Position * cameraDir >= 0)
	{
		...
	}
	else{
		//DiscardVertex
	}
	
	*/
		gl_Position = gl_in[i].gl_Position;
		fragNormal = data_in[i].fragNormal;
		fragTexcoord = data_in[i].fragTexcoord;
		blade_pos = data_in[i].blade_pos;
		EmitVertex();
	}

	EndPrimitive();
	}
}