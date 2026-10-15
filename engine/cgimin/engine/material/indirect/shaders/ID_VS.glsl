#version 430
precision highp float;

layout(location = 0) in vec4 in_position; // Vertex position
layout(location = 1) in vec2 in_uv; // Vertex uv

layout(std430, binding = 5) buffer IntArrayBuffer {
    int mappingBuffer[]; // Mapping
};

uniform mat4 modelview_projection_matrix;

flat out int id;
out float y_pos;
flat out float time;
flat out vec2 blade_world_position;
flat out vec2 chunkWorldPosition;

uniform float elapsed_time;

out vec2 frag_uv;
flat out vec2 grassPosition;
flat out vec3 camPos;
out vec4 wNormal;

uniform sampler2D sampler;
uniform sampler2D normal_map;
uniform sampler2D track;

in vec3 in_normal; 

uniform vec2 chunkLocation;
uniform vec3 cameraWorldPosition;

float CHUNK_SIZE = 250;

float rand(int seed) {
    seed = (seed << 13) ^ seed;
    return 1.0 - float((seed * (seed * seed * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0;
}

float remap(float value, float inMin, float inMax, float outMin, float outMax) {
    float t = clamp((value - inMin) / (inMax - inMin), 0.0, 1.0);
    return outMin + t * (outMax - outMin);
}

float atan2(in float y, in float x)
{
    bool s = (abs(x) > abs(y));
    return mix(3.1415026/2.0 - atan(x,y), atan(y,x), s);
}

void main()
{
	int id = mappingBuffer[gl_InstanceID];
	frag_uv = in_uv;

	float random = rand(id);
	float random2 = rand(id+1000);
	float angle = rand(id+2000);

	float randomLean = rand(id+1231);

	float chunkOffsetX = chunkLocation.x / 250;
	float chunkOffsetY = chunkLocation.y / 250;

	vec2 blade_position = vec2((int(id / 64)) / 64.0f,((id % 64)) / 64.0f);
	grassPosition = vec2(random,random2);
	
	vec4 noise_sample = texture(sampler, vec2(((id % 64) * 8) / 512.0f + elapsed_time * 0.02f,(int(id / 64) * 8) / 512.0f + elapsed_time * 0.02f));

	const float wind_speed = 0.02;

	vec2 windTexturePosition = vec2(((id % 64) * 8) / 5120.0f + elapsed_time * wind_speed,(int(id / 64) * 8) / 5120.0f  + elapsed_time * wind_speed);

	windTexturePosition = vec2(windTexturePosition.x + chunkOffsetX * 0.1,windTexturePosition.y + chunkOffsetY * 0.1);

	vec4 wind_sample = texture(sampler, windTexturePosition);

	vec3 track_lean = texture(track, blade_position).rgb;

	mat4 rotationMatrix = mat4(
		cos(angle), 0.0, sin(angle), 0.0,
		0.0, 1.0, 0.0, 0.0,
		-sin(angle), 0.0, cos(angle), 0.0,
		0.0, 0.0, 0.0, 1.0
	);

	float wind_direction = remap(wind_sample.x,-1,1,0.25,1.0);

	float rot = wind_sample.x * 2f;

	mat4 wind_direction_matrix = mat4(
		cos(wind_direction), 0.0, sin(wind_direction), 0.0,
		0.0, 1.0, 0.0, 0.0,
		-sin(wind_direction), 0.0, cos(wind_direction), 0.0,
		0.0, 0.0, 0.0, 1.0
	);

	float track_intensity = 2*3.1415f-1f;
	mat4 track_rot = mat4(
		1.0, 0.0, 0.0, 0.0,
		0.0, cos(track_intensity), -sin(track_intensity), 0.0,
		0.0, sin(track_intensity), cos(track_intensity), 0.0,
		0.0, 0.0, 0.0, 1.0
	);
	
	mat4 rotationX = mat4(
		1.0, 0.0, 0.0, 0.0,
		0.0, cos(rot), -sin(rot), 0.0,
		0.0, sin(rot), cos(rot), 0.0,
		0.0, 0.0, 0.0, 1.0
	);


	float rot2 = radians(mod(10.0 * elapsed_time, 360.0));
	mat4 rotationZ = mat4(
		cos(rot2), -sin(rot2), 0.0, 0.0,
		sin(rot2), cos(rot2), 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		0.0, 0.0, 0.0, 1.0
	);


	float curveAmount = randomLean * noise_sample.x * (1.0f - in_uv.y);

	mat4 curveMatrix = mat4(
		1.0, 0.0, 0.0, 0.0,
		0.0, cos(curveAmount), -sin(curveAmount), 0.0,
		0.0, sin(curveAmount), cos(curveAmount), 0.0,
		0.0, 0.0, 0.0, 1.0
	);

	int rowLen = 32 * 2;
	float space = CHUNK_SIZE / rowLen;

	float jitterX = (space * random) - (space / 2.0f);
	float jitterY = (space * random2) - (space / 2.0f);

	vec4 curved = curveMatrix * in_position;

	vec4 rotated = rotationMatrix * curved;

	rotated = rotationX * rotated;

	rotated = wind_direction_matrix * rotated;
	
	float track_rgb = track_lean.r + track_lean.g + track_lean.b;

	mat4 normal_matrix = curveMatrix * rotationMatrix * rotationX * wind_direction_matrix;

	mat4 full_matrix = rotationMatrix * rotationX * wind_direction_matrix;

	if(track_rgb > 0){
		float track_strength = atan2(track_lean.x * 2.0 - 1,track_lean.z * 2.0 - 1);
		track_strength = 3.1415 + track_strength;
		mat4 track_direction = mat4(
			cos(track_strength), 0.0, sin(track_strength), 0.0,
			0.0, 1.0, 0.0, 0.0,
			-sin(track_strength), 0.0, cos(track_strength), 0.0,
			0.0, 0.0, 0.0, 1.0
		);

		rotated *= track_rot;
		rotated *= track_direction;

		normal_matrix *= track_rot * track_direction;

		normal_matrix = wind_direction_matrix * rotationX * rotationMatrix * curveMatrix * track_rot * track_direction;
	}else{
		normal_matrix = wind_direction_matrix * rotationX * rotationMatrix * curveMatrix;
	}
	
	wNormal = vec4(0,0,1,1) * transpose(normal_matrix);

	mat4 randSize = mat4(1.0,0.0,0.0,0.0,
						 0.0,1.0,0.0,0.0,
						 0.0,0.0,2.0,0.0,
						 0.0,0.0,0.0,1.0);
	
	vec3 instPos = vec3(rotated.x + id % rowLen * space + space/2.0f + jitterX,rotated.y,rotated.z + int(id / rowLen) * space + space/2.0f + jitterY);

	gl_Position = modelview_projection_matrix * vec4(instPos,1);


	vec2 world_pos = vec2(id % rowLen * space + space/2.0f + jitterX,int(id / rowLen) * space + space/2.0f + jitterY);
	
	blade_world_position = vec2(blade_position.x,blade_position.y);

	camPos = cameraWorldPosition;
	chunkWorldPosition = chunkLocation;
}




