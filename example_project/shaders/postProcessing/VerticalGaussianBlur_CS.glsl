#version 430

layout(local_size_x = 16, local_size_y = 16) in;

layout(binding = 0) uniform sampler2D inputTexture;
layout(rgba16f, binding = 1) writeonly uniform image2D outputTexture;

const float weights[5] = float[](
    0.227027,
    0.1945946,
    0.1216216,
    0.054054,
    0.016216
);

float getLuma(ivec2 p)
{
    vec3 c = texelFetch(inputTexture, p, 0).rgb;
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

float isBrightVertical(ivec2 p, ivec2 size)
{
    float l =
        getLuma(p) +
        getLuma(ivec2(p.x, min(p.y + 1, size.y - 1))) +
        getLuma(ivec2(p.x, max(p.y - 1, 0)));

    return l * 0.3333;
}

void main()
{
    ivec2 pixel = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size  = textureSize(inputTexture, 0);

    if(pixel.x >= size.x || pixel.y >= size.y)
        return;

    if(isBrightVertical(pixel, size) < 0.01)
    {
        imageStore(outputTexture, pixel, vec4(0.0));
        return;
    }

    vec3 result = texelFetch(inputTexture, pixel, 0).rgb * weights[0];

    for(int i = 1; i < 5; ++i)
    {
        ivec2 up   = ivec2(pixel.x, max(pixel.y - i, 0));
        ivec2 down = ivec2(pixel.x, min(pixel.y + i, size.y - 1));

        result += texelFetch(inputTexture, up, 0).rgb   * weights[i];
        result += texelFetch(inputTexture, down, 0).rgb * weights[i];
    }

    imageStore(outputTexture, pixel, vec4(result, 1.0));
}