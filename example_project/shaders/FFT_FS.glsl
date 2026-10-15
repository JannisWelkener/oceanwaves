#version 420
precision highp float;

layout (binding = 1) uniform sampler2D offsetTexture; 
layout (binding = 2) uniform sampler2D normalAndFoamTexture; 
layout (binding = 3) uniform sampler2D foamSampler;

uniform mat4 model_matrix;

in vec2 texcoord;

out vec4 outputColor;

in vec3 normal;

uniform float offsetHeight;

uniform vec3 light_direction;          // sun direction (world space, normalized)
uniform vec4 light_ambient_color;
uniform vec4 light_diffuse_color;

uniform vec3 chunkPosition;
uniform float chunkSize;
uniform vec3 cameraPosition;

uniform float elapsed_time;

void main()
{
 
    vec3 surfaceNormal =  texture(normalAndFoamTexture,texcoord).xyz;
    float foamSample = texture(offsetTexture,texcoord).a;

    vec2 worldPos = chunkPosition.xz + texcoord * chunkSize;
    float distanceTexelToCamera = length(cameraPosition.xz - worldPos);

    float foamThreshold = 1f;


    mat3 normalMatrix = mat3(model_matrix);
    vec3 N = normalize(normalMatrix * surfaceNormal);

    vec3 L = normalize(light_direction);
    vec3 V = normalize(cameraPosition - vec3(worldPos.x,0,worldPos.y));

    // --------------------------------------------------
    // LIGHTING TERMS
    // --------------------------------------------------
    float NdotL = max(dot(N, L), 0.0);

    vec3 H = normalize(L + V);
    float specular = pow(max(dot(N, H), 0.0), 128.0);

    float fresnel = pow(1.0 - max(dot(N, V), 0.0), 5.0);
    fresnel = mix(0.02, 1.0, fresnel);

    // --------------------------------------------------
    // WATER COLOR
    // --------------------------------------------------
    vec3 deepWater    = vec3(0.0, 0.933, 1);
    vec3 shallowWater = vec3(0.0, 0.37, 0.41);

    float depthFactor = clamp(N.y * 0.5 + 0.5, 0.0, 1.0);
    vec3 waterColor = mix(deepWater, shallowWater, depthFactor);

    waterColor = mix(deepWater, shallowWater, 0.8);
    // --------------------------------------------------
    // JACOBIAN CREST MASK (NOT FINAL FOAM)
    // --------------------------------------------------
    //float crest = clamp(1.0 - fragJacobian, 0.0, 1.0);

    //crest = smoothstep(0.35, 0.7, crest);

    float crest = foamSample;
    float invFoamThreshold = 1.0f - foamThreshold;
    crest = (crest - invFoamThreshold) / (1.0f - invFoamThreshold);
    crest = clamp(crest, 0.0, 1.0);

    crest = foamSample;

    // --------------------------------------------------
    // FOAM BREAKUP (HIGH FREQUENCY)
    // --------------------------------------------------
    vec2 foamUV = texcoord * 10;/* * noiseSize * 4.0*/// * 0.001f;

    // approximate flow direction from surface normal
    vec2 flowDir = normalize(surfaceNormal.xz + vec2(0.0001));
    foamUV += flowDir * elapsed_time * 0.25 * 0.001f;

    float foamNoise = texture2D(foamSampler, foamUV).x;
    foamNoise = smoothstep(0.35, 0.65, foamNoise);

    float foam = crest * foamNoise;


    foam = crest * 1;
    // grazing-angle boost
    //foam *= mix(0.6, 1.0, fresnel);

    foam = clamp(foam, 0.0, 1.0);

    // --------------------------------------------------
    // FOAM COLOR (NOT PURE WHITE)
    // --------------------------------------------------
    vec3 foamThin  = vec3(0.75, 0.85, 0.9);
    vec3 foamThick = vec3(0.95, 0.97, 1.0);
    vec3 foamColor = mix(foamThin, foamThick, foam);

    // --------------------------------------------------
    // SUN REFLECTION
    // --------------------------------------------------
    vec3 R = reflect(-L, N);
    float sunSpec = pow(max(dot(R, V), 0.0), 512.0);
    sunSpec *= smoothstep(0.0, 0.1, L.y);
    sunSpec *= fresnel;
    sunSpec *= mix(1.0, 1.8, foam);

    vec3 sunColor = light_diffuse_color.rgb * vec3(1.0, 0.95, 0.85);
    vec3 sunReflection = sunColor * sunSpec * 2.0;

    // --------------------------------------------------
    // FINAL LIGHTING
    // --------------------------------------------------
    vec3 ambient = waterColor * light_ambient_color.rgb;
    vec3 diffuse = waterColor * NdotL * light_diffuse_color.rgb;
    vec3 spec    = vec3(1.0) * specular * 0.6;

    vec3 color = ambient + diffuse + spec + sunReflection;

    // subtle sky reflection
    color = mix(color, vec3(0.6, 0.8, 1.0), fresnel * 0.25);

    // foam on top
    color = mix(color, foamColor, foam);

    outputColor = vec4(color, 1.0);
}

