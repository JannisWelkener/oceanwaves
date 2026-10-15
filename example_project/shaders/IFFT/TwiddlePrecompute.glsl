#version 450

layout(local_size_x = 1, local_size_y = 8, local_size_z = 1) in;

const float PI = 3.1415926;

// Texture bindings
layout(binding = 0, rgba32f) uniform image2D PrecomputeBuffer;

uniform uint Size;

vec2 ComplexExp(vec2 a) {
    return vec2(cos(a.y), sin(a.y)) * exp(a.x);
}

// ---------- Kernels ----------
void PrecomputeTwiddleFactorsAndInputIndices() {
    uint x = gl_GlobalInvocationID.x;
    uint y = gl_GlobalInvocationID.y;

    uint b = Size >> (x + 1);
    vec2 mult = 2.0 * PI * vec2(0.0, 1.0) / float(Size);
    uint i = (2u * b * (y / b) + (y % b)) % Size;
    vec2 twiddle = ComplexExp(-mult * float(y / b * b));

    imageStore(PrecomputeBuffer, ivec2(x, y), vec4(twiddle, float(i), float(i + b)));
    imageStore(PrecomputeBuffer, ivec2(x, y + Size / 2), vec4(-twiddle, float(i), float(i + b)));
}

// ---------- Main dispatch ----------
void main() {
    PrecomputeTwiddleFactorsAndInputIndices();
}