#version 420 core

layout (binding = 0) uniform sampler2D offsetTexture0; 
layout (binding = 1) uniform sampler2D normalAndFoamTexture0; 
layout (binding = 2) uniform sampler2D offsetTexture1; 
layout (binding = 3) uniform sampler2D normalAndFoamTexture1; 
layout (binding = 4) uniform sampler2D offsetTexture2; 
layout (binding = 5) uniform sampler2D normalAndFoamTexture2; 
layout (binding = 6) uniform samplerCube skybox; 
layout (binding = 7) uniform sampler2D foamTexture0; 
layout (binding = 8) uniform sampler2D foamTexture1; 
layout (binding = 9) uniform sampler2D depthTexture; 

uniform float foamMult0;
uniform float foamMult1;
uniform float foamMult2;

uniform float uvScale0;
uniform float uvScale1;
uniform float uvScale2;

uniform bool isActive0;
uniform bool isActive1;
uniform bool isActive2;

uniform bool toggleGrid;

uniform vec3 cameraPosition;

uniform float leadr;
uniform float WavePeakScatterStrength;
uniform float envStrength;
uniform float roughness;

uniform float scatterStrength;// = 1;
uniform float scatterShadowStrength;// = 0.5;
uniform float bubbleDensity;// = 1;
uniform float foamRoughness;//= 0;

uniform vec3 light_direction;
uniform vec3 Lsun;

uniform vec3 scatterColor;
uniform vec3 bubbleColor;// = vec3(0,0.1,0.0159);

uniform vec2 screenSize = vec2(1920,1080);
uniform float nearPlane = 0.1f;
uniform float farPlane = 5000.0f;


in vec2 texcoord0;
in vec2 texcoord1;
in vec2 texcoord2;

in vec2 fragTexcoord;
in vec3 vertexPosition;

in vec3 worldPosition;

out vec4 FragColor;

float LinearizeDepth(float d, float near, float far)
{
    float z = d * 2.0 - 1.0;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

vec3 sampleEnvironmentFromSlopes(
    vec3 viewDir,                 // should point from surface -> camera
    samplerCube environmentMap,

    float meanSlopeX,
    float meanSlopeY,
    float varianceX,
    float varianceY,
    float covarianceXY
) {
    // --- 1. Precompute distribution parameters ---
    float stdDevX = sqrt(max(varianceX, 1e-6));
    float stdDevY = sqrt(max(varianceY, 1e-6));

    float correlation = covarianceXY / (stdDevX * stdDevY + 1e-6);
    correlation = clamp(correlation, -0.999, 0.999);

    float sqrtTerm = sqrt(1.0 - correlation * correlation);

    // --- 2. 3x3 Gaussian sample pattern ---
    const int N = 3;
    float baseSamples[3] = float[](-1.0, 0.0, 1.0);

    vec3 accumulatedColor = vec3(0.0);
    float totalWeight = 0.0;

    // --- 3. Loop over samples ---
    for (int i = 0; i < N; i++) {
        for (int j = 0; j < N; j++) {

            float baseX = baseSamples[i];
            float baseY = baseSamples[j];

            // --- 3a. Gaussian weight (separable is more stable) ---
            float weightX = exp(-0.5 * baseX * baseX);
            float weightY = exp(-0.5 * baseY * baseY);
            float weight = weightX * weightY;

            // --- 3b. Transform to anisotropic slope distribution ---
            float slopeX = baseX * stdDevX + meanSlopeX;

            float slopeY =
                (correlation * baseX + sqrtTerm * baseY)
                * stdDevY + meanSlopeY;

            // --- 3c. Convert slope -> normal ---
            vec3 sampleNormal = normalize(vec3(
                -slopeX,   
                 1.0,
                 -slopeY
            ));

            // --- 3d. Reflect view direction ---
            vec3 reflectedDir = reflect(-viewDir, sampleNormal);

            // --- 3e. Sample environment ---
            vec3 envColor = texture(environmentMap, reflectedDir).rgb;

            accumulatedColor += envColor * weight;
            totalWeight += weight;
        }
    }

    return accumulatedColor / max(totalWeight, 1e-6);
}


float A(vec3 H, vec3 S, float roughness) {
    float hdots = max(0.001f, max(dot(H, S),0.0));
	float a = hdots / (roughness * sqrt(1 - hdots * hdots));
	float a2 = a * a;

	return a < 1.6f ? (1.0f - 1.259f * a + 0.396f * a2) / (3.535f * a + 2.181 * a2) : 0.0f;
}

float P22(float ndoth, float a) {
    float a2 = a * a;
	float exp_arg = (ndoth * ndoth - 1) / (a2 * ndoth * ndoth);
    float PI = 3.1415926535;
	return exp(exp_arg) / (PI * a2 * ndoth * ndoth * ndoth * ndoth);
}

void drawUVGrid(vec2 coord,vec3 color){
    vec2 uvTile = fract(coord);
    vec2 edgeDist = min(uvTile, 1.0 - uvTile);
    float lineWidth = 0.005;
    float edge = step(min(edgeDist.x, edgeDist.y), lineWidth);
    if(edge>lineWidth)FragColor = vec4(color,1);
}

vec3 clampLuminance(vec3 color)
{
    float Y = dot(color, vec3(0.2126, 0.7152, 0.0722));
    return color / max(Y, 1.0);
}

void main()
{
    vec3 surfaceNormal0 = texture(normalAndFoamTexture0,texcoord0).rgb;
    float foam0 = texture(offsetTexture0,texcoord0).a;

    vec3 surfaceNormal1 = texture(normalAndFoamTexture1,texcoord1).rgb;
    float foam1 = texture(offsetTexture1,texcoord1).a;

    vec3 surfaceNormal2 = texture(normalAndFoamTexture2,texcoord2).rgb;
    float foam2 = texture(offsetTexture2,texcoord2).a;

    float slopeX = 0;
    float slopeY = 0;

    float activeCount = 0;
    if(isActive0){
        slopeX+=-surfaceNormal0.x;
        slopeY+=-surfaceNormal0.z;
        activeCount++;
    }
    if(isActive1){
        slopeX+=-surfaceNormal1.x;
        slopeY+=-surfaceNormal1.z;
        activeCount++;
    }
    if(isActive2){
        slopeX+=-surfaceNormal2.x;
        slopeY+=-surfaceNormal2.z;
        activeCount++;
    }

    if(activeCount != 0){
        slopeX/=activeCount;
        slopeY/=activeCount;
    }

    vec3 surfaceNormal = vec3(0);
    if(isActive0)surfaceNormal+=normalize(surfaceNormal0);
    if(isActive1)surfaceNormal+=normalize(surfaceNormal1);
    if(isActive2)surfaceNormal+=normalize(surfaceNormal2);
    surfaceNormal = normalize(surfaceNormal);

    float foam = 0;

    if(isActive0)foam += foam0 * foamMult0;
    if(isActive1)foam += foam1 * foamMult1;
    if(isActive2)foam += foam2 * foamMult2;

    foam = clamp(foam,0,1);



    //approx scatter based on:
    //https://gdcvault.com/play/1025819/Advanced-Graphics-Techniques-Tutorial-Wakes

    //Lscatter = (k1H(wi * w0)^4(0.5 - 0.5(wi*wn))^3+k2(w0*wn)^2)Css Lsun*(1/(1+A(wi)))
    //Lscatter += k3(wi*wn)Css Lsun + k4Pf Cf Lsun

    //H - max(0,waveheight), wi,w0,wh - sun/eye/half
    //k1,k2,k3,k4 - tweaking multipliers
    //Css,Cf - water scatter color and air bubbles color
    //Pf - density of air bubbles spread in water
    //(wa,wb) - max(0,(wa*wb))

    //A(w) -> SmithMaskingBeckmann
    //if a<1.6
    //(1-1.259a+0.39a^2)/(3.535a+2.181a^2)
    //else 0

    float offsetMult0 = 10 * uvScale0;
    float offsetMult1 = 10 * uvScale1;
    float offsetMult2 = 10 * uvScale2;

    float vertDisp = 0;
    if(isActive0)vertDisp+=texture(offsetTexture0,texcoord0).y * offsetMult0;
    if(isActive1)vertDisp+=texture(offsetTexture1,texcoord1).y * offsetMult1;
    if(isActive2)vertDisp+=texture(offsetTexture2,texcoord2).y * offsetMult2;

    float waveHeight = max(0.0f, vertDisp);

    vec3 mesoNormal = normalize(surfaceNormal);

    vec3 lightDir = normalize(light_direction);

    vec3 viewDir = normalize(cameraPosition - worldPosition);
    float NdotL = max(dot(mesoNormal, lightDir), 0.0);

    float k1 = WavePeakScatterStrength * waveHeight * pow(max(dot(lightDir, -viewDir), 0.0), 4.0f) * pow(0.5f - 0.5f * dot(lightDir, mesoNormal), 3.0f);
    float k2 = scatterStrength * pow(max(dot(viewDir, mesoNormal),0.0), 2.0f);
    float k3 = scatterShadowStrength * NdotL;
    float k4 = bubbleDensity;


    vec3 halfwayDir = normalize(lightDir + viewDir);

    //sqrt(ax^2cos^2phi+ay^2sin^2phi) ??
    float _Roughness = 0.075;
    _Roughness = roughness;
    float _FoamRoughness = 0;
    _FoamRoughness = foamRoughness;
    float a = _Roughness + foam * _FoamRoughness;

    //A(wi)
    float lightMask = A(halfwayDir, lightDir, a);
    //A(w0)
    float viewMask = A(halfwayDir, viewDir, a);

    float k1_k2 = (k1 + k2);
    vec3 Lscatter = k1_k2 * scatterColor * Lsun * 1 / (1 + lightMask); //correct
	Lscatter += k3 * scatterColor * Lsun + k4 * bubbleColor * Lsun;

    

    //(1-cos(thetaV)^(5*exp(-2.69av)))
    float eta = 1.33f;
	float R = ((eta - 1) * (eta - 1)) / ((eta + 1) * (eta + 1));
    float numerator = pow(1 - dot(mesoNormal, viewDir), 5 * exp(-2.69 * a));


    /*
    F = R + (1 - R) * (1-cos(thetaV)^(5*exp(-2.69av))) / (1 + 22.7av^1.5)
    */

    float F = R + (1 - R) * numerator / (1.0f + 22.7f * pow(a, 1.5f));
    F = clamp(F,0f,1.0f);

    //float G = 1/(1 + viewMask + lightMask);// 1/(1+A(wi)+A(w0)

    float ndoth = max(0.0001f, dot(mesoNormal, halfwayDir));
    //vec3 macroNormal = vec3(0, 1, 0);



                                        /*p22(wh)*/ /*
    vec3 Lsun_fr_cos = Lsun * F * G * P22(ndoth, a);

    //Lsun_fr_cos /=4 * (wn*weye)*(1+A(wsun)+A/weye))
	Lsun_fr_cos /= 4.0f * max(0.001f, max(dot(macroNormal, lightDir),0));//weye??
	Lsun_fr_cos *= max(dot(mesoNormal, lightDir),0);
    */

    vec3 Lsun_fr_cos = Lsun * F * P22(ndoth, a);
    Lsun_fr_cos /= (4.0f * max(0.001f, max(dot(mesoNormal, viewDir),0)) * (1 + viewMask + lightMask));//weye??


    vec3 Lenv = sampleEnvironmentFromSlopes(viewDir,skybox,slopeX,slopeY,leadr,leadr,0);//covarianceXY


    Lenv *= envStrength;


    vec3 Lto_eye = (1 - F) * Lscatter + Lsun_fr_cos + F * Lenv;


    
    vec3 foamTex0 = texture(foamTexture1,texcoord0 * 3).rgb * 1.4;
    vec3 foamTex1 = texture(foamTexture1,texcoord0 * 10).rgb;

    vec3 foamColor = mix(foamTex1,foamTex0,waveHeight);

    if(foam>1)foam=1;

    foamColor = clampLuminance(foamColor);
	Lto_eye = mix(Lto_eye, foamColor, foam);


    FragColor = vec4(Lto_eye,1);


    float xyDistanceToCamera = length(cameraPosition.xz - worldPosition.xz);
    float fogStart = 200.0 * 2;
    float fogEnd   = 400.0 * 2;
    float fogFactor = (xyDistanceToCamera - fogStart) / (fogEnd - fogStart);
    fogFactor = clamp(fogFactor, 0.0, 1.0);


    FragColor = vec4(FragColor.rgb, 1.0 - fogFactor);
    

      
    //FragColor = vec4(avgEnvironment,1);

    if(toggleGrid){
        if(isActive0)drawUVGrid(texcoord0,vec3(1,0,0));
        if(isActive1)drawUVGrid(texcoord1,vec3(0,1,0));
        if(isActive2)drawUVGrid(texcoord2,vec3(0,0,1));
    }




    vec2 uv = gl_FragCoord.xy / screenSize;
    float sceneDepth = texture(depthTexture, uv).r;
    float waterDepth = gl_FragCoord.z;

    float sceneZ = LinearizeDepth(sceneDepth, nearPlane, farPlane);
    float waterZ = LinearizeDepth(waterDepth, nearPlane, farPlane);
    float diff = sceneZ - waterZ;

    float boxfoam = smoothstep(2.0f, 0.0, diff);

    //fixes depth-bug with skybox
    if(xyDistanceToCamera<fogStart)
        FragColor = mix(FragColor,vec4(foamTex1,1),boxfoam);
}