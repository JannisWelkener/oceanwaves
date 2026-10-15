#version 430 core

layout (quads, fractional_odd_spacing, ccw) in;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

struct Wave
{
    float A;
    float k;
    float speed;
    float phase;
    vec2 D;
};

uniform int waveCount;
layout(std430, binding = 0) buffer Waves
{
    Wave waves[];
};

uniform float elapsed_time;
uniform float steepness;
/*
0.1f,
10.0f,
*/
uniform float scaling;

uniform float strengthAmplifier;

uniform vec3 cameraPosition;

in vec2 tcs_uv[];
out vec2 fragTexcoord;
out vec3 fragNormal;                    // world-space normal
out float fragJacobian;                 // Gerstner Jacobian
out vec3 vertexPosition;
out vec4 localPosition;

void applyGerstnerWaves(inout vec4 position,vec4 worldPos);
void gerstnerWave(
    in vec2 pos,
    in float A,
    in float k,
    in float speed,
    in vec2 D,
    float wPhase,
    inout vec4 position,
    inout vec3 tangentX,
    inout vec3 tangentZ,
    inout float dxdx,
    inout float dxdz,
    inout float dzdx,
    inout float dzdz
);

void main()
{
    float u = gl_TessCoord.x;
    float v = gl_TessCoord.y;

   vec2 uv =
    mix(
        mix(tcs_uv[0], tcs_uv[1], u),
        mix(tcs_uv[3], tcs_uv[2], u),
        v
    );
    fragTexcoord = uv;

    vec4 position =
        mix(
            mix(gl_in[0].gl_Position, gl_in[1].gl_Position, u),
            mix(gl_in[3].gl_Position, gl_in[2].gl_Position, u),
            v
        );

    localPosition = position;

    vec4 worldPos = model * position;

    float initialY = worldPos.y;

    applyGerstnerWaves(position,worldPos);

    //Flatten Horizon smoothly
    vec4 wPos = vec4(position.x,initialY,position.z,1);
    float xyDistanceToCamera = length(cameraPosition.xz - wPos.xz);
    float horizonStart = 200.0 * 2;
    float horizonEnd   = 400.0 * 2;
    float verticalOffsetFactor = (xyDistanceToCamera - horizonStart) / (horizonEnd - horizonStart);
    verticalOffsetFactor = clamp(verticalOffsetFactor, 0.0, 1.0);
    float offsetY = position.y - initialY;
    float finalY = (1 - verticalOffsetFactor) * offsetY + initialY;

    position.y = finalY;
    
    
    vertexPosition = worldPos.xyz;

    gl_Position = projection * view * model * position;
}


void applyGerstnerWaves(inout vec4 position,vec4 worldPos){

    vec3 tangentX = vec3(1, 0, 0);
    vec3 tangentZ = vec3(0, 0, 1);

    float dxdx = 1.0;
    float dxdz = 0.0;
    float dzdx = 0.0;
    float dzdz = 1.0;

    for (int i = 0; i < waveCount; ++i)
    {
        Wave w = waves[i];
        gerstnerWave(
            worldPos.xz,
            w.A,
            w.k,
            w.speed,
            normalize(w.D),
            w.phase,
            position,
            tangentX,
            tangentZ,
            dxdx,
            dxdz,
            dzdx,
            dzdz
        );
    }

    fragNormal = normalize(cross(tangentZ, tangentX));
    float J = dxdx * dzdz - dxdz * dzdx;
    fragJacobian = J;
    
}

void gerstnerWave(
    in vec2 pos,
    in float A,
    in float k,
    in float speed,
    in vec2 D,
    float wPhase,
    inout vec4 position,
    inout vec3 tangentX,
    inout vec3 tangentZ,
    inout float dxdx,
    inout float dxdz,
    inout float dzdx,
    inout float dzdz
) {
    A = A / scaling;

    A = A * strengthAmplifier;

    float phase = k * dot(D, pos * scaling) + speed * elapsed_time + wPhase;
    float sinP = sin(phase);
    float cosP = cos(phase);

    float QA = steepness / k;
    
    position.x += D.x * QA * cosP;
    position.y += A * sinP;
    position.z += D.y * QA * cosP;
    
    float com = QA * sinP * k * scaling;

    dxdx += -D.x * D.x * com;
    dxdz += -D.x * D.y * com;
    dzdx += -D.y * D.x * com;
    dzdz += -D.y * D.y * com;

    float derivK = k * scaling;

    tangentX += vec3(
        -D.x * D.x * QA * sinP * derivK,
         D.x * A * cosP * derivK,
        -D.x * D.y * QA * sinP * derivK
    );

    tangentZ += vec3(
        -D.x * D.y * QA * sinP * derivK,
         D.y * A * cosP * derivK,
        -D.y * D.y * QA * sinP * derivK
    );
}
