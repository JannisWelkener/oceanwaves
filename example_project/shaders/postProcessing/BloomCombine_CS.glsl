#version 430

layout(local_size_x = 16, local_size_y = 16) in;

layout(binding = 0) uniform sampler2D colorTexture;
layout(binding = 1) uniform sampler2D bloomTexture;

layout(rgba16f, binding = 2) writeonly uniform image2D outputTexture;

uniform float bloomStrength = 1.0;

void main()
{
    ivec2 pixel = ivec2(gl_GlobalInvocationID.xy);

    ivec2 size = textureSize(colorTexture, 0);

    if(pixel.x >= size.x || pixel.y >= size.y)
        return;

    vec3 sceneColor = texelFetch(colorTexture, pixel, 0).rgb;
    vec3 bloomColor = texelFetch(bloomTexture, pixel, 0).rgb;

    vec3 finalColor = sceneColor + bloomColor * bloomStrength;

    imageStore(outputTexture, pixel, vec4(finalColor, 1.0));
}