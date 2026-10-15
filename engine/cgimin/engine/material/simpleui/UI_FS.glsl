#version 330 core

in vec2 uv;

out vec4 out_color;

uniform sampler2D tex;
uniform float multiplier;

void main()
{
    vec3 color = texture(tex, uv).xyz * multiplier;
    float r = color.r<1 ? color.r : 1;
    float g = color.g<1 ? color.g : 1;
    float b = color.b<1 ? color.b : 1;
    out_color = vec4(r,g,b,1);

    //if(color.r>1)out_color = vec4(1,0,0,1);
    //if(texture(tex, uv).x > 1)out_color = vec4(1,0,0,1);
}