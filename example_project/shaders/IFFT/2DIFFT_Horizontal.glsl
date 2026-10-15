#version 450

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

uniform sampler2D PrecomputedData;
uniform sampler2D Buffer0;
layout(binding = 2, rgba32f) uniform writeonly image2D Buffer1;

uniform uint Step;

// ---------- Complex math helpers ----------
vec2 ComplexMult(vec2 a, vec2 b) {
    return vec2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

vec4 complexMul2(vec2 w, vec4 v) {
    float ax = w.x;
    float ay = w.y;

    return vec4(
        fma(ax, v.x, -ay * v.y),
        fma(ax, v.y,  ay * v.x),
        fma(ax, v.z, -ay * v.w),
        fma(ax, v.w,  ay * v.z)
    );
}

shared vec4 sharedTwiddles[gl_WorkGroupSize.x];

void HorizontalStepInverseFFT() {
    uint x = gl_GlobalInvocationID.x;
    uint y = gl_GlobalInvocationID.y;

    
    uint lx = gl_LocalInvocationID.x;
    uint ly = gl_LocalInvocationID.y;

    // Load PrecomputedData into shared memory (one row per workgroup)
    if (ly == 0) {
       sharedTwiddles[lx] = texelFetch(PrecomputedData, ivec2(Step, x),0);
    }

    barrier(); 

    // Read from shared memory instead of image
    vec4 data = sharedTwiddles[lx];

    uvec2 inputIndices = uvec2(data.ba);
    vec2 twiddle = vec2(data.r, -data.g);

    ivec2 coordX = ivec2(inputIndices.x, y);
    ivec2 coordY = ivec2(inputIndices.y, y);

    vec4 buf0x = texelFetch(Buffer0, coordX, 0);
    vec4 buf0y = texelFetch(Buffer0, coordY, 0);

    vec4 result = buf0x + complexMul2(twiddle, buf0y);
    imageStore(Buffer1, ivec2(x, y), result);
}



// ---------- Main dispatch ----------
void main() {
    HorizontalStepInverseFFT();
}