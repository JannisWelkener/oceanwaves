#version 330
precision highp float;

uniform float uTime;

out vec4 FragColor;

void main()
{
    float r = abs(sin(uTime));
    float g = abs(cos(uTime));
    float b = abs(sin(uTime * 0.5));
    //FragColor = vec4(r, g, b, 1.0);
    FragColor = vec4(1, 1, 1, 1);
}

