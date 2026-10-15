#version 330
precision highp float;

// --------------------------------------------------
// UNIFORMS
// --------------------------------------------------
uniform mat4 model;

uniform vec3 light_direction;          // sun direction (world space, normalized)
uniform vec4 light_ambient_color;
uniform vec4 light_diffuse_color;

uniform vec3 camera_position;          // world-space camera position
uniform float elapsed_time;

uniform float foamThreshold;

uniform sampler2D normalSampler;
uniform sampler2D foamSampler;

uniform float noiseSize;

// --------------------------------------------------
// INPUTS FROM VERTEX SHADER
// --------------------------------------------------
in vec2 fragTexcoord;
in vec3 fragNormal;                    // world-space normal
in float fragJacobian;                 // Gerstner Jacobian
in vec3 vertexPosition;                // world-space position

// --------------------------------------------------
out vec4 outputColor;

// --------------------------------------------------
// MULTI-SCALE NOISE
// --------------------------------------------------
vec4 getNoise(vec2 uv)
{
    vec2 uv0 = (uv / 103.0) + vec2(elapsed_time / 17.0, elapsed_time / 29.0);
    vec2 uv1 = (uv / 107.0) - vec2(elapsed_time / -19.0, elapsed_time / 31.0);
    vec2 uv2 = uv / vec2(890.70, 980.30) + vec2(elapsed_time / 101.0, elapsed_time / 97.0);
    vec2 uv3 = uv / vec2(1091.0, 1027.0) - vec2(elapsed_time / 109.0, elapsed_time / -113.0);

    vec4 noise =
        texture2D(normalSampler, uv0) +
        texture2D(normalSampler, uv1) +
        texture2D(normalSampler, uv2) +
        texture2D(normalSampler, uv3);

    return noise * 0.5 - 1.0;
}

void main()
{
    // --------------------------------------------------
    // SURFACE NORMAL WITH NOISE
    // --------------------------------------------------
    vec4 noise = getNoise(vertexPosition.xz * noiseSize);
    vec3 surfaceNormal = normalize(
        fragNormal + noise.xzy * vec3(1.5, 1.0, 1.5)
    );

    mat3 normalMatrix = mat3(model);
    vec3 N = normalize(normalMatrix * surfaceNormal);

    vec3 L = normalize(light_direction);
    vec3 V = normalize(camera_position - vertexPosition);

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

    // --------------------------------------------------
    // JACOBIAN CREST MASK (NOT FINAL FOAM)
    // --------------------------------------------------
    //float crest = clamp(1.0 - fragJacobian, 0.0, 1.0);

    //crest = smoothstep(0.35, 0.7, crest);

    float crest = 1.0 - fragJacobian;
    float invFoamThreshold = 1.0f - foamThreshold;
    crest = (crest - invFoamThreshold) / (1.0f - invFoamThreshold);
    crest = clamp(crest, 0.0, 1.0);

    // --------------------------------------------------
    // FOAM BREAKUP (HIGH FREQUENCY)
    // --------------------------------------------------
    vec2 foamUV = vertexPosition.xz/* * noiseSize * 4.0*/ * 0.001f;

    // approximate flow direction from surface normal
    vec2 flowDir = normalize(surfaceNormal.xz + vec2(0.0001));
    foamUV += flowDir * elapsed_time * 0.25 * 0.001f;

    float foamNoise = texture2D(foamSampler, foamUV).x;
    foamNoise = smoothstep(0.35, 0.65, foamNoise);

    float foam = crest * foamNoise;

    // grazing-angle boost
    foam *= mix(0.6, 1.0, fresnel);

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

    // --------------------------------------------------
    // DISTANCE FOG
    // --------------------------------------------------
    /*
    vec3 fogColor = vec3(1,0,0);
    float fogDensity = 0.005;
    float distanceToCamera = length(camera_position - vertexPosition);

    // exponential fog (stable, scale-independent)
    float fogFactor = 1.0 - exp(-distanceToCamera * fogDensity);

    // optional clamp for safety
    fogFactor = clamp(fogFactor, 0.0, 1.0);
    */
     
    // apply fog
    //color = mix(color, fogColor, fogFactor);
    
    float fogStart = 200.0 * 2;
    float fogEnd   = 400.0 * 2;

    float distanceToCamera = length(camera_position - vertexPosition);

    // 0 at fogStart, 1 at fogEnd
    float fogFactor = (distanceToCamera - fogStart) / (fogEnd - fogStart);
    fogFactor = clamp(fogFactor, 0.0, 1.0);


    outputColor = vec4(color, 1.0 - fogFactor);
}
