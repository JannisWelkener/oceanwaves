#version 330
precision highp float;

uniform sampler2D texture1;

// "model_matrix" Matrix
uniform mat4 model_matrix;

// parameter for direktional light
uniform vec3 light_direction;
uniform vec4 light_ambient_color;
uniform vec4 light_diffuse_color;

// input from Vertex-Shader
in vec2 fragTexcoord;
in vec3 fragNormal;

// die finale Farbe
out vec4 outputColor;

void main()
{	
	// get rotation of our models transform matrix
	mat3 normalMatrix = mat3(model_matrix);

	// calculate normal
    vec3 normal = normalize(normalMatrix * fragNormal);

	// calculate brightness for diffuse light
	float brightness = clamp(dot(normal, light_direction), 0, 1);

	// surfaceColor is color from the texture
	vec4 surfaceColor = texture(texture1, fragTexcoord);

    //				 Ambiente color						  + Diffuse color
    // outputColor = (surfaceColor * light_ambient_color) + (surfaceColor * brightness * light_diffuse_color);
	// obere Zeile surfaceColor ausgeklammert
	outputColor = surfaceColor * (light_ambient_color + brightness * light_diffuse_color);

}