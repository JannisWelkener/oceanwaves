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

layout(std430, binding = 7) buffer NewParticlesBuffer
{
    Particle newParticles[];
};

layout(std430, binding = 8) buffer OldParticlesBuffer
{
    Particle oldParticles[];
};

layout(binding = 3, offset = 0) 
uniform atomic_uint visibleCount;

layout(binding = 5, offset = 0) 
uniform atomic_uint oldParticleCount;

uniform float elapsedTime;
uniform vec3 dominantWind;
uniform float windScaling;
uniform sampler2D noiseTexture;

Particle updateParticle(Particle p){
    float dt = 0.02f;
    float speed = 0.1;

    p.lifetime -= dt;

    vec3 gravity = vec3(0.0f, -9.81f, 0.0f);
    p.direction += gravity * dt;

    // Wind gust
    vec2 noiseUV = p.position.xz * 0.2 + dominantWind.xz * elapsedTime;
    float noise = texture(noiseTexture, noiseUV).r;

    float gust = mix(0.5f, 20.0f, noise);
    vec3 wind = -dominantWind * gust * dt * windScaling;
    //p.direction += wind * gust * dt;

    //vec3 wind = vec3(0.997171,0,-0.07516644);

    p.rotation = p.rotation + dt * p.rotationSpeed;
    p.position = p.position + p.direction * speed + wind;

    return p;
}

void addParticle(Particle particle){
    int current = int(atomicCounterIncrement(visibleCount));

    newParticles[current].position = particle.position;
    newParticles[current].lifetime = particle.lifetime;
    newParticles[current].direction = particle.direction;
    newParticles[current].size = particle.size;
    newParticles[current].full_lifetime = particle.full_lifetime;
    newParticles[current].rotation = particle.rotation;
    newParticles[current].type = particle.type;
    newParticles[current].rotationSpeed = particle.rotationSpeed;
}


void main() {
    uint id = gl_GlobalInvocationID.x+64*gl_GlobalInvocationID.y + 64*64*gl_GlobalInvocationID.z;

    int oldParticleSize = int(atomicCounter(oldParticleCount));

    if(id<oldParticleSize){
        Particle p = oldParticles[id];
        if(p.lifetime>0){
            addParticle(updateParticle(p));
        }
    }

}
