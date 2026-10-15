#version 330
precision highp float;

uniform sampler2D sampler;
uniform sampler2D normal_map;

uniform sampler2D track;

// "model_matrix" Matrix
uniform mat4 model_matrix;

// parameter for direktional light
uniform vec3 light_direction;
uniform vec4 light_ambient_color;
uniform vec4 light_diffuse_color;

// input from Vertex-Shader
in vec2 fragTexcoord;
in vec3 fragNormal;
in vec2 blade_pos;

uniform float elapsed_time;

// die finale Farbe
out vec4 outputColor;

float easeInSine(float x) {
  return 1 - cos((x * 3.1415) / 2);
}

void main()
{	
	// get rotation of our models transform matrix
	mat3 normalMatrix = mat3(model_matrix);

	// calculate normal
    vec3 normal = texture(normal_map, fragTexcoord).rgb;

	// calculate brightness for diffuse light
	float brightness = clamp(dot(normal, light_direction), 0, 1);

	// surfaceColor is color from the texture
	//vec4 surfaceColor = texture(sampler, fragTexcoord);

	vec3 baseColor = vec3(0.05,0.2,0.01);
	vec3 tipColor = vec3(0.5,0.5,0.1);
	vec3 diffuseColor = mix(tipColor,baseColor,easeInSine(fragTexcoord.y));

	vec3 phong = vec3(1.5,1.5,1.5) * diffuseColor * brightness;



    //				 Ambiente color						  + Diffuse color
    // outputColor = (surfaceColor * light_ambient_color) + (surfaceColor * brightness * light_diffuse_color);
	// obere Zeile surfaceColor ausgeklammert


	vec4 surfaceColor = texture(normal_map, fragTexcoord);

	outputColor = surfaceColor;

	
	outputColor = vec4(phong,1);
	//outputColor = vec4(elapsed_time,0,0,1.0f);

	vec3 track = texture(track, blade_pos).rgb; //incorrect location, but cool as fuck

	float track_rgb = track.r + track.g + track.b;

	vec3 phongtrack = phong * 0.5f + track * 0.5f;
	//outputColor = vec4(phongtrack,1);

	//outputColor = vec4(vec3(track_rgb,0,0),1);
	}