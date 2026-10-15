#version 430

layout (local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout (rgba32f, binding = 0) uniform readonly image2D DxDz_DyDxz;
layout (rgba32f, binding = 1) uniform readonly image2D Dyx_Dyz_Dxx_Dzz;

layout (rgba32f, binding = 2) uniform writeonly image2D offset;
layout (rgba32f, binding = 3) uniform image2D normalAndFoam;

uniform float deltaTime;
uniform float lambda; //horizontal offset strength

uniform float foamBias;
uniform float foamDecayRate;
uniform float foamAdd;

void main()
{
    ivec2 id = ivec2(gl_GlobalInvocationID.xy);

    vec2 dxdz = imageLoad(DxDz_DyDxz, id).xy;
    vec2 dydxz = imageLoad(DxDz_DyDxz, id).ba;

    imageStore(offset, id, vec4(dxdz.x * lambda, dydxz.x, dxdz.y * lambda,0));

    vec2 dyxdyz = imageLoad(Dyx_Dyz_Dxx_Dzz,id).xy;
    vec2 dxxdzz = imageLoad(Dyx_Dyz_Dxx_Dzz,id).ba;


    
    vec4 derivatives = vec4(dyxdyz,dxxdzz * lambda);

    vec2 slope = vec2(derivatives.x / (1 + derivatives.z),derivatives.y / (1 + derivatives.w));

    vec3 surfaceNormal = /*normalize*/(vec3(-slope.x, 1.0, -slope.y));


    float jacobian =
        (1.0 + lambda * dxxdzz.x) *
        (1.0 + lambda * dxxdzz.y) -
        lambda * lambda * dydxz.y * dydxz.y;


    float dTime = deltaTime;

    vec4 prev = imageLoad(normalAndFoam,id);

    float foam = prev.a;
    foam *= exp(-foamDecayRate * deltaTime);
    foam = clamp(foam,0,1);


    float _FoamThreshold = 0;

    float biasedJacobian = max(0.0f, -(jacobian - foamBias));

    if (biasedJacobian > _FoamThreshold)
       foam += foamAdd * biasedJacobian * deltaTime;


    imageStore(normalAndFoam, id, vec4(surfaceNormal,foam));

}