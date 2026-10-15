#version 430

layout (local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout (rgba32f, binding = 0) uniform image2D offset;
layout (rgba32f, binding = 1) uniform readonly image2D normalAndFoam;

// 3x3 Gaussian kernel
const float kernel[3][3] = float[3][3](
    float[3](1, 2, 1),
    float[3](2, 4, 2),
    float[3](1, 2, 1)
);

const float kernelSum = 16.0;

void main()
{
    ivec2 id = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(normalAndFoam);

    float result = 0.0;

    // Apply 3x3 Gaussian blur on alpha channel
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            ivec2 coord = clamp(id + ivec2(x, y), ivec2(0), size - 1);
            float a = imageLoad(normalAndFoam, coord).a;
            result += a * kernel[y + 1][x + 1];
        }
    }

    result /= kernelSum;

    vec4 current = imageLoad(offset, id);
    current.a = result;

    imageStore(offset, id, current);
}