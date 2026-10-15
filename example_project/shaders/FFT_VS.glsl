#version 420
precision highp float;

// input aus der VAO-Datenstruktur
in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

out vec3 normal;

uniform mat4 modelview_projection_matrix;

layout (binding = 1) uniform sampler2D offsetTexture; 

uniform float offsetHeight;

uniform float uvScale;

out vec2 texcoord;

void main()
{
	texcoord = in_uv * uvScale;

	float offsetMult = 40;
	
	vec3 offset = texture(offsetTexture,texcoord).xyz;
	vec3 pos = vec3(in_position.x + offset.x * offsetMult,in_position.y + offset.y * offsetMult,in_position.z+offset.z * offsetMult);

	gl_Position = modelview_projection_matrix * vec4(pos, 1);
}


