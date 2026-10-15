#version 330
precision highp float;

uniform sampler2D sampler; 
uniform bool renderDebugColor;

in vec2 texcoord;

out vec4 outputColor;

void main()
{
    /*
    vec4 texSample = texture(sampler, texcoord);
    outputColor = vec4(texSample.xyz * 10000,1);
    if(outputColor.xyz == vec3(0,0,0))outputColor = vec4(1,0,0,1);
    */
    outputColor = texture(sampler, texcoord);
    if(renderDebugColor)
        outputColor = vec4(1,0,0,1);
    //outputColor = vec4(texture(sampler, texcoord).x * 10,texture(sampler, texcoord).x * 10,texture(sampler, texcoord).x * 10,1);
}