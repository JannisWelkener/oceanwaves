#version 430 core

layout(local_size_x = 16) in;

struct BuoyancyResult
{
    vec4 HeightAndVelocity;
};

layout(std430, binding = 0) readonly buffer InputPoints
{
    vec4 samplePoints[];
};

layout(std430, binding = 1) writeonly buffer OutputResults
{
    BuoyancyResult results[];
};

layout (binding = 0) uniform sampler2D offsetTexture0;
layout (binding = 1) uniform sampler2D normalAndFoamTexture0;
layout (binding = 2) uniform sampler2D offsetTexture1; 
layout (binding = 3) uniform sampler2D normalAndFoamTexture1;
layout (binding = 4) uniform sampler2D offsetTexture2; 
layout (binding = 5) uniform sampler2D normalAndFoamTexture2;

layout (binding = 6) uniform sampler2D flow0;
layout (binding = 7) uniform sampler2D flow1;
layout (binding = 8) uniform sampler2D flow2;

uniform float uvScale0;
uniform float uvScale1;
uniform float uvScale2;

uniform bool isActive0;
uniform bool isActive1;
uniform bool isActive2;

uniform int sampleCount;

vec4 sampleOcean(
    vec2 samplePoint,
    out vec3 normal
);

float SampleAccurateOceanPosition(
    vec3 targetPosition,
    int iterations,
    out vec3 normal
);

float sampleOceanHeight(
    vec2 samplePoint
);

void main()
{
    uint id = gl_GlobalInvocationID.x;

    if (id >= sampleCount)
        return;

    vec3 samplePosition = samplePoints[id].rgb;

    vec3 sampledNormal;

    float oceanHeight = SampleAccurateOceanPosition(samplePosition.xyz,8,sampledNormal);




    vec2 samplePoint = vec2(samplePosition.x,samplePosition.z);
    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    vec2 texcoord0 = samplePoint * 0.003f / uvScale0;
    vec2 texcoord1 = samplePoint * 0.003f / uvScale1;
    vec2 texcoord2 = samplePoint * 0.003f / uvScale2;

    vec2 v0 = texture(flow0,texcoord0).xy * offsetMult0;
    vec2 v1 = texture(flow1,texcoord1).xy * offsetMult1;
    vec2 v2 = texture(flow2,texcoord2).xy * offsetMult2;

    vec2 flow = vec2(0);
    if(isActive0)flow+=v0;
    if(isActive1)flow+=v1;
    if(isActive2)flow+=v2;

    //vec2 flow = v0 + v1 + v2;
    vec3 velocity = vec3(flow.y,0,-flow.x);

    results[id].HeightAndVelocity = vec4(oceanHeight,velocity.x, velocity.y, velocity.z/*sampledNormal*/);
}

float SampleAccurateOceanPosition(
    vec3 targetPosition,
    int iterations,
    out vec3 outNormal
)
{
    // initial guess in undisplaced domain
    vec2 guess = targetPosition.xz;

    vec3 displaced;
    vec3 normal;

    for (int i = 0; i < iterations; i++)
    {
        // Sample your FFT/Gerstner ocean
        displaced = sampleOcean(guess,normal).rgb;

        // displaced.xz is where the wave moved the point to
        vec2 error = displaced.xz - targetPosition.xz;

        // convergence threshold
        if (dot(error, error) < 0.0001)
            break;

        // inverse mapping correction
        guess -= error;
    }

    // final accurate sample
    vec4 outpos = sampleOcean(guess,normal);
    outNormal = normal;
    return sampleOceanHeight(guess);;
}

vec4 sampleOcean(
    vec2 samplePoint,
    out vec3 normal
){
    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    vec2 texcoord0 = samplePoint * 0.003f / uvScale0;
    vec2 texcoord1 = samplePoint * 0.003f / uvScale1;
    vec2 texcoord2 = samplePoint * 0.003f / uvScale2;

    vec3 offset0 = texture(offsetTexture0,texcoord0).xyz * offsetMult0;
    vec3 offset1 = texture(offsetTexture1,texcoord1).xyz * offsetMult1;
    vec3 offset2 = texture(offsetTexture2,texcoord2).xyz * offsetMult2;

    vec3 offset = offset0 + offset1 + offset2;

    vec3 position = vec3(samplePoint.x,0,samplePoint.y);
    float height = position.y + offset.y;
    
    vec3 surfaceNormal0 = texture(normalAndFoamTexture0,texcoord0).rgb;
    vec3 surfaceNormal1 = texture(normalAndFoamTexture1,texcoord1).rgb;
    vec3 surfaceNormal2 = texture(normalAndFoamTexture2,texcoord2).rgb;
    vec3 surfaceNormal = normalize(normalize(surfaceNormal0) + normalize(surfaceNormal1) + normalize(surfaceNormal2));
    
    normal = surfaceNormal;

    return vec4(position.x + offset.x,position.y + offset.y,position.z+offset.z,1);
}

float sampleOceanHeight(
    vec2 samplePoint
){
    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    vec2 texcoord0 = samplePoint * 0.003f / uvScale0;
    vec2 texcoord1 = samplePoint * 0.003f / uvScale1;
    vec2 texcoord2 = samplePoint * 0.003f / uvScale2;

    float offset0f = texture(offsetTexture0,texcoord0).y * offsetMult0;
    float offset1f = texture(offsetTexture1,texcoord1).y * offsetMult1;
    float offset2f = texture(offsetTexture2,texcoord2).y * offsetMult2;

    float height0 = 0;
    if(isActive0)height0+=offset0f;
    if(isActive1)height0+=offset1f;
    if(isActive2)height0+=offset2f;
    //float height0 = offset0f + offset1f + offset2f;
    
    return height0;
}