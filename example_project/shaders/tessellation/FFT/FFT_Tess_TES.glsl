#version 420 core

layout (quads, fractional_odd_spacing, ccw) in;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform float uvScale0;
uniform float uvScale1;
uniform float uvScale2;

uniform bool isActive0;
uniform bool isActive1;
uniform bool isActive2;

uniform vec3 cameraPosition;

layout (binding = 0) uniform sampler2D offsetTexture0;
layout (binding = 2) uniform sampler2D offsetTexture1; 
layout (binding = 4) uniform sampler2D offsetTexture2; 

in vec2 tcs_uv[];
out vec2 texcoord0;
out vec2 texcoord1;
out vec2 texcoord2;
out vec2 fragTexcoord;
out vec3 vertexPosition;
out vec3 worldPosition;

void applyFFTWaves(inout vec4 position, vec4 worldPos);

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

    vec4 worldPos = model * position;
    applyFFTWaves(position,worldPos);

    vec4 newWorldPos = model * position;
    worldPosition = newWorldPos.xyz;
    
    gl_Position = projection * view * model * position;
}

void applyFFTWaves(inout vec4 position, vec4 worldPos){
    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    texcoord0 = worldPos.xz * 0.003f / uvScale0;
    texcoord1 = worldPos.xz * 0.003f / uvScale1;
    texcoord2 = worldPos.xz * 0.003f / uvScale2;

    //Read offset based on scaling
    vec3 offset0 = texture(offsetTexture0,texcoord0).xyz * offsetMult0;
    vec3 offset1 = texture(offsetTexture1,texcoord1).xyz * offsetMult1;
    vec3 offset2 = texture(offsetTexture2,texcoord2).xyz * offsetMult2;

    vec3 offset = vec3(0);
    if(isActive0)offset += offset0;
    if(isActive1)offset += offset1;
    if(isActive2)offset += offset2;

    //Flatten Horizon smoothly
    vec4 wPos = vec4(position.x + offset.x,position.y,position.z+offset.z,1);
    wPos = model * wPos;
  
    float xyDistanceToCamera = length(cameraPosition.xz - wPos.xz);
    float horizonStart = 200.0 * 2;
    float horizonEnd   = 400.0 * 2;
    float verticalOffsetFactor = (xyDistanceToCamera - horizonStart) / (horizonEnd - horizonStart);
    verticalOffsetFactor = clamp(verticalOffsetFactor, 0.0, 1.0);

    offset.y *= (1 - verticalOffsetFactor);

    position = vec4(position.x + offset.x,position.y + offset.y,position.z+offset.z,1);
}