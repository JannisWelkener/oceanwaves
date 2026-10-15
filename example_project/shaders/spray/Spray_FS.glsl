#version 420
precision highp float;

layout (binding = 0) uniform sampler2D splashTexture0; 
layout (binding = 1) uniform sampler2D splashTexture1; 
layout (binding = 2) uniform sampler2D splashTexture2; 
layout (binding = 3) uniform sampler2D splashTexture3; 
layout (binding = 4) uniform sampler2D mistTexture; 

uniform float mistTransparancy;
uniform float colorCorrection;
uniform float sprayTransparancy;

in vec2 texcoord;
in float transparancy;
in float type;

out vec4 outputColor;

void main()
{

    vec4 particleTexture;

    if(type < 1){
        particleTexture = texture(splashTexture0, texcoord);
    }
    else if(type < 2){
        particleTexture = texture(splashTexture1, texcoord);
    }
    else if(type < 3){
        particleTexture = texture(splashTexture1, texcoord);
        //particleTexture = texture(splashTexture2, texcoord); //Texture looks bad for this, replace later
    }
    else if(type < 4){
        particleTexture = texture(splashTexture3, texcoord);
    }
    else{
        particleTexture = texture(mistTexture, texcoord);
    }

    particleTexture = vec4(mix(particleTexture.xyz,vec3(1,1,1),colorCorrection),particleTexture.a);

    if(type>=4){
        outputColor = vec4(particleTexture.rgb,particleTexture.a * transparancy * mistTransparancy);
    }else{
        outputColor = vec4(particleTexture.rgb,particleTexture.a * transparancy * sprayTransparancy);
    }

    
}