#version 430

layout(local_size_x = 16, local_size_y = 16) in;

layout(binding = 0) uniform sampler2D colorTexture;

layout(rgba16f, binding = 1) writeonly uniform image2D brightPassTexture;

uniform float threshold = 1.0;

void main()
{
    ivec2 pixel = ivec2(gl_GlobalInvocationID.xy);

    vec3 color = texelFetch(colorTexture, pixel, 0).rgb;

    // Standard luminance calculation
    float brightness = dot(color, vec3(0.2126, 0.7152, 0.0722));

    vec4 result = vec4(0.0);

    if (brightness > threshold)
    {
        result = vec4(color, 1.0);
    }

    imageStore(brightPassTexture, pixel, result);
}