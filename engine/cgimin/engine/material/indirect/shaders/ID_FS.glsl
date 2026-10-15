#version 330 core

flat in int id;

in vec2 frag_uv;
in vec4 wNormal;
flat in float time;
flat in vec2 grassPosition;
flat in vec3 camPos;
flat in vec2 blade_world_position;
flat in vec2 chunkWorldPosition;

uniform sampler2D sampler;
uniform sampler2D normal_map;
uniform sampler2D track;

out vec4 FragColor;

float easeInSine(float x) {
  return cos((x * 3.1415) / 2);
}

float rand(int seed) {
    seed = (seed << 13) ^ seed;
    return 1.0 - float((seed * (seed * seed * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0;
}

float remap(float value, float inMin, float inMax, float outMin, float outMax) {
    float t = clamp((value - inMin) / (inMax - inMin), 0.0, 1.0);
    return outMin + t * (outMax - outMin);
}

void main() {

	//Base color
	float randomColorX = grassPosition.x;
	randomColorX = remap(randomColorX,0,1,-0.01,0.01);

	float randomColorY = grassPosition.y;
	randomColorY = remap(randomColorY,0,1,-0.02,0.02);

	float randomColorZ = (grassPosition.x + grassPosition.y) / 2;
	randomColorZ = remap(randomColorY,0,1,-0.005,0.01);

	vec3 baseColor = vec3(0.05 + randomColorX,0.2 + randomColorY,0.01 + randomColorZ);
	vec3 tipColor = vec3(0.5 + randomColorX,0.5 + randomColorY,0.1 + randomColorZ);
	vec3 diffuseColor = mix(tipColor,baseColor,easeInSine(frag_uv.y));

	//Shading
	vec2 normuv = normalize(frag_uv);
	vec4 normals = texture(normal_map,frag_uv);
	
	vec3 light_direction = vec3(-0.57735026, 0.57735026, 0.57735026);

	float brightness = clamp(dot(normals.xyz, light_direction), 0, 1);

	vec3 phong = vec3(1.5,1.5,1.5) * diffuseColor * brightness;

	//Fog
	vec2 bposition = vec2(chunkWorldPosition.x / 50 + blade_world_position.y * 5,chunkWorldPosition.y / 50 + blade_world_position.x * 5);
	float d = abs(distance(vec2(camPos.xz),bposition)) / 50;

	float uFogStart = 0.3;
	float uFogEnd = 1.5;

	vec3 uFogColor = vec3(0.451f,0.608f,0.706f);

	float fogFactor = clamp((d - uFogStart) / (uFogEnd - uFogStart), 0.0, 1.0);

	FragColor = vec4(mix(phong.xyz, uFogColor, fogFactor),1);

}