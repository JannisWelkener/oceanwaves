#version 430

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in; 

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


layout(binding = 3, offset = 0) 
uniform atomic_uint visibleCount;

uniform vec3 cameraPosition;
uniform uint frameCounter;

uniform mat4 view_projection_matrix;

layout (binding = 0) uniform sampler2D offsetAndFoam0;
layout (binding = 1) uniform sampler2D offsetAndFoam1;
layout (binding = 2) uniform sampler2D offsetAndFoam2;

uniform float uvScale0;
uniform float uvScale1;
uniform float uvScale2;

uniform float foamMult0;
uniform float foamMult1;
uniform float foamMult2;

uniform bool isActive0;
uniform bool isActive1;
uniform bool isActive2;

uniform int yThreadAmount;
uniform float sprayGenerationRadius;
uniform float foamThreshold;
uniform float particleLifetime;
uniform float particleSpeed;
uniform float verticalSpeedDampening;
uniform float verticalSpeedDampeningMist;

uniform float particleSizeMin;
uniform float particleSizeMax;

uniform float mistPercentage;
uniform float mistSize;

const float PI = 3.14159265359;

uint pcg(uint v)
{
    v = v * 747796405u + 2891336453u;
    v = ((v >> ((v >> 28u) + 4u)) ^ v) * 277803737u;
    return (v >> 22u) ^ v;
}

vec2 GetRandomPositionAroundCamera(
    vec3 cameraPos,
    float radius,
    uint frameCount,
    uint objectId)
{
    // combine object identity + frame
    uint seed = objectId * 9781u ^ frameCount * 6271u;

    // generate two random values
    uint r1 = pcg(seed);
    uint r2 = pcg(seed + 1u);

    // normalize to [0,1]
    float u = float(r1) / 4294967295.0;
    float v = float(r2) / 4294967295.0;

    // uniform disk sampling
    float theta = 2.0 * PI * u;
    float r = radius * sqrt(v);

    return cameraPos.xz + vec2(cos(theta), sin(theta)) * r;
}

vec2 calculateRandomPointAroundCamera(float radius){
    uint threadID = gl_GlobalInvocationID.x+yThreadAmount*gl_GlobalInvocationID.y + yThreadAmount*yThreadAmount*gl_GlobalInvocationID.z;

    return GetRandomPositionAroundCamera(cameraPosition,radius,frameCounter,threadID);
}

float random(){
    uint threadID = gl_GlobalInvocationID.x+yThreadAmount*gl_GlobalInvocationID.y + yThreadAmount*yThreadAmount*gl_GlobalInvocationID.z + yThreadAmount*yThreadAmount*yThreadAmount;

    uint seed = threadID * 9781u ^ frameCounter * 6271u;
    uint r1 = pcg(seed);
    float u = float(r1) / 4294967295.0;
    return u;
}

void addParticle(vec4 position){
    int current = int(atomicCounterIncrement(visibleCount));

    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    vec2 texcoord0 = position.xz * 0.003f / uvScale0;
    vec2 texcoord1 = position.xz * 0.003f / uvScale1;
    vec2 texcoord2 = position.xz * 0.003f / uvScale2;

    vec3 offset0 = texture(offsetAndFoam0,texcoord0).xyz * offsetMult0;
    vec3 offset1 = texture(offsetAndFoam1,texcoord1).xyz * offsetMult1;
    vec3 offset2 = texture(offsetAndFoam2,texcoord2).xyz * offsetMult2;
    //vec3 offset = offset0 + offset1 + offset2;

    vec3 offset = vec3(0);
    if(isActive0)offset += offset0;
    if(isActive1)offset += offset1;
    if(isActive2)offset += offset2;

    //Spawn under wave and shoot out (no popping)
    float spawnAdjustment = 0.2f;
    vec3 wPos = vec3(position.x + offset.x,position.y+offset.y - spawnAdjustment,position.z+offset.z);

    vec3 waveDirection = normalize(offset);

    float rand = random();

    float speed = (2 + rand*5) * particleSpeed;
    vec3 speedMultiplier = vec3(speed,speed * verticalSpeedDampening,speed);

    waveDirection = waveDirection * speedMultiplier;

    float lifetime = 0.5 * particleLifetime + rand * particleLifetime;

    float type = rand * (mistPercentage+4);

    float size = particleSizeMin + rand * (particleSizeMax-particleSizeMin);

    //Mist rules
    if(type >= 4){
        size = mistSize;

        vec3 speedMultiplierMist = vec3(speed,speed * verticalSpeedDampeningMist,speed);
        waveDirection = normalize(offset0) * speedMultiplierMist;
    }

    positions[current].position = wPos;
    positions[current].lifetime = lifetime;
    positions[current].direction = waveDirection;
    positions[current].size = size;
    positions[current].full_lifetime = lifetime;
    positions[current].rotation = rand * 2 * PI;
    positions[current].type = type;
    positions[current].rotationSpeed = 1 + rand * 10; 
}

bool isInsideFrustum(vec3 position, float threshold) {
    vec4 clipSpacePos = view_projection_matrix * vec4(position, 1.0);

    float expandedW = clipSpacePos.w + threshold;

    if (clipSpacePos.x < -expandedW || clipSpacePos.x > expandedW) return false;
    if (clipSpacePos.y < -expandedW || clipSpacePos.y > expandedW) return false;
    if (clipSpacePos.z < -expandedW || clipSpacePos.z > expandedW) return false;

    return true;
}

bool isWithinFoam(vec4 pos){
    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    vec2 texcoord0 = pos.xz * 0.003f / uvScale0;
    vec2 texcoord1 = pos.xz * 0.003f / uvScale1;
    vec2 texcoord2 = pos.xz * 0.003f / uvScale2;

    float foam0 = texture(offsetAndFoam0,texcoord0).a;
    float foam1 = texture(offsetAndFoam1,texcoord1).a;
    float foam2 = texture(offsetAndFoam2,texcoord2).a;

    //float foam = clamp(foam0 * foamMult0 + foam1 * foamMult1 + foam2 * foamMult2,0,1);

    float foam = 0;

    if(isActive0)foam += foam0 * foamMult0;
    if(isActive1)foam += foam1 * foamMult1;
    if(isActive2)foam += foam2 * foamMult2;

    foam = clamp(foam,0,1);

    if(foam>foamThreshold)return true;
        return false;
}

void main() {

    float radius = sprayGenerationRadius;
    vec2 randomPos = calculateRandomPointAroundCamera(radius);
    vec4 pos = vec4(randomPos.x,0,randomPos.y,1);

    if(isInsideFrustum(pos.xyz,1)){
        if(isWithinFoam(pos))
            addParticle(pos);
    }
    
}
