#version 450

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

layout(binding = 1, rgba32f) uniform image2D Buffer;

void Permute() {
    uint x = gl_GlobalInvocationID.x;
    uint y = gl_GlobalInvocationID.y;

    vec4 result = imageLoad(Buffer, ivec2(x, y));

    //float _sign = 1.0 - 2.0 * float((x + y) % 2);
    //float _sign = 1.0 - 2.0 * float((x + y) & 1);
    float _sign = (( (x + y) & 1u ) == 0u) ? 1.0 : -1.0;

    result *= _sign;

    imageStore(Buffer, ivec2(x, y), result);
}


// ---------- Main dispatch ----------
void main() {
    Permute();
}