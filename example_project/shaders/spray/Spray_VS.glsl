#version 430
precision highp float;

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv; 

struct Particle
{
      vec3 position;
      float lifetime;
      vec3 direction;
      float size;
      float full_lifetime;
      float rotation;
      float type;
      float rotationSpeed;
};

layout(std430, binding = 7) buffer PositionBuffer
{
    Particle positions[];
};

uniform mat4 modelview_projection_matrix;

out vec2 texcoord;
out float transparancy;
out float type;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform vec3 cameraPosition;

uniform float particleLifetime;

void main()
{
    uint id = gl_InstanceID;

    vec3 center = positions[id].position;
    float lifetime = positions[id].lifetime;
    float size = positions[id].size;
    float full_lifetime = positions[id].full_lifetime;

    transparancy = lifetime / full_lifetime;
    type = positions[id].type;

    // camera-facing basis
    vec3 right = vec3(viewMatrix[0][0], viewMatrix[1][0], viewMatrix[2][0]);
    vec3 up    = vec3(viewMatrix[0][1], viewMatrix[1][1], viewMatrix[2][1]);

    // local quad vertex (-0.5 .. 0.5 expected from mesh)
    vec3 local = in_position;

    float angle = positions[id].rotation;
    float c = cos(angle);
    float s = sin(angle);

    // Rotate quad in billboard space
    vec2 rotated;
    rotated.x = local.x * c - local.y * s;
    rotated.y = local.x * s + local.y * c;

    vec3 worldPos =
        center +
        right * (rotated.x * size) +
        up    * (rotated.y * size);

    texcoord = in_uv;

    gl_Position = modelview_projection_matrix * vec4(worldPos, 1.0);
}

