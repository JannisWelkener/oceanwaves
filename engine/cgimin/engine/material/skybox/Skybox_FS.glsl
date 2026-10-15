#version 330
precision highp float;

uniform samplerCube skybox; 
uniform bool whiteOut;

in vec3 vDirection;
in vec2 texcoord;
flat in vec3 out_normal;

out vec4 outputColor;

void main()
{
    vec4 color = texture(skybox, normalize(vDirection));
    outputColor = color;

    if(whiteOut)
        outputColor = vec4(1,1,1,1);
}