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

float isBright(ivec2 p, ivec2 size)
{
    vec3 c = texelFetch(inputTexture, p, 0).rgb;
    float l = dot(c, vec3(0.2126, 0.7152, 0.0722));

    // also sample a few neighbors cheaply
    l += dot(texelFetch(inputTexture, ivec2(min(p.x+1,size.x-1), p.y), 0).rgb, vec3(0.2126,0.7152,0.0722));
    l += dot(texelFetch(inputTexture, ivec2(max(p.x-1,0), p.y), 0).rgb, vec3(0.2126,0.7152,0.0722));

    return l * 0.33;
}

void main()
{
    ivec2 pixel = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = textureSize(inputTexture, 0);

    if(pixel.x >= size.x || pixel.y >= size.y)
        return;

    if(isBright(pixel, size) < 0.01)
    {
        imageStore(outputTexture, pixel, vec4(0.0));
        return;
    }

    vec3 result = texelFetch(inputTexture, pixel, 0).rgb * weights[0];

    for(int i = 1; i < 5; ++i)
    {
        ivec2 left  = ivec2(max(pixel.x - i, 0), pixel.y);
        ivec2 right = ivec2(min(pixel.x + i, size.x - 1), pixel.y);

        result += texelFetch(inputTexture, left, 0).rgb  * weights[i];
        result += texelFetch(inputTexture, right, 0).rgb * weights[i];
    }

    imageStore(outputTexture, pixel, vec4(result, 1.0));
}