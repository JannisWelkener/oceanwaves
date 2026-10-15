#version 330
precision highp float;

uniform sampler2D sampler;

uniform sampler2D normal_map;

uniform sampler2D track;

in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform vec2 chunkLocation;
uniform vec2 cameraPosition;

uniform float elapsed_time;

uniform mat4 modelview_projection_matrix;

out DATA
{
	vec3 fragNormal;
	vec2 fragTexcoord;
	mat4 modelview_projection_matrix;
	vec2 blade_pos;
	vec2 world_position;
	vec2 camera_position;
} data_out;

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
	int id = gl_InstanceID;
	float random = rand(id);
	float random2 = rand(id+1000);
	float angle = rand(id+2000);

	float randomLean = rand(id+1231);

	//vec2 blade_position = vec2(((gl_InstanceID % 64)) / 64.0f,(int(gl_InstanceID / 64)) / 64.0f); // <->
	vec2 blade_position = vec2((int(id / 64)) / 64.0f,((id % 64)) / 64.0f);
	
	vec4 noise_sample = texture(sampler, vec2(((id % 64) * 8) / 512.0f + elapsed_time * 0.02f,(int(id / 64) * 8) / 512.0f + elapsed_time * 0.02f));

	vec4 noise_sample2 = texture(sampler, vec2(((id % 64) * 8) / 512.0f + elapsed_time * 0.03f,(int(id / 64) * 8) / 512.0f + elapsed_time * 0.03f));

	//data_out.world_position = chunkLocation + blade_position * 5.0f;
	data_out.camera_position = cameraPosition;
	data_out.world_position = chunkLocation;

	
	vec3 track_lean = texture(track, blade_position).rgb;
	
	mat4 rotationMatrix = mat4(
		cos(angle), 0.0, sin(angle), 0.0,
		0.0, 1.0, 0.0, 0.0,
		-sin(angle), 0.0, cos(angle), 0.0,
		0.0, 0.0, 0.0, 1.0
	);
	
	data_out.blade_pos = blade_position;


	float wind_direction = remap(noise_sample2.x,-1,1,0.25,1.0);

	float wdir = noise_sample2.x * 2.0f*3.1415f; //..............................TODO


	float rot = noise_sample2.x * 1.2f;

	mat4 wind_dir = mat4(
		cos(wind_direction), 0.0, sin(wind_direction), 0.0,
		0.0, 1.0, 0.0, 0.0,
		-sin(wind_direction), 0.0, cos(wind_direction), 0.0,
		0.0, 0.0, 0.0, 1.0
	);

	//float track_intensity = 1.5f;
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

	data_out.fragTexcoord = in_uv;

	data_out.fragNormal = in_normal;

	int rowLen = 32 * 2;
	float space = CHUNK_SIZE / rowLen;

	data_out.modelview_projection_matrix = modelview_projection_matrix;

	float jitterX = (space * random) - (space / 2.0f);
	float jitterY = (space * random2) - (space / 2.0f);

	vec4 curved = curveMatrix * vec4(in_position,1);

	vec4 rotated = rotationMatrix * curved;

	rotated = rotationX * rotated;

	rotated = wind_dir * rotated;
	
	float track_rgb = track_lean.r + track_lean.g + track_lean.b;

	if(track_rgb > 0){
		float track_strength = atan2(track_lean.x * 2.0 - 1,track_lean.z * 2.0 - 1);
		//track_strength = elapsed_time;
		track_strength = 3.1415 + track_strength;
		mat4 track_direction = mat4(
			cos(track_strength), 0.0, sin(track_strength), 0.0,
			0.0, 1.0, 0.0, 0.0,
			-sin(track_strength), 0.0, cos(track_strength), 0.0,
			0.0, 0.0, 0.0, 1.0
		);


		
		rotated *= track_rot;
		rotated *= track_direction;
	}
	
	vec3 instPos = vec3(rotated.x + gl_InstanceID % rowLen * space + space/2.0f + jitterX,rotated.y,rotated.z + int(gl_InstanceID / rowLen) * space + space/2.0f + jitterY);

	gl_Position = modelview_projection_matrix * vec4(instPos,1);
}


