#version 430 core

struct Wave
{
    float A;
    float k;
    float speed;
    float phase;
    vec2 D;
};

uniform int waveCount;
layout(std430, binding = 0) buffer Waves
{
    Wave waves[];
};

uniform float elapsed_time;
uniform float steepness;
uniform float scaling;

uniform vec3 light_direction;
uniform vec3 Lsun;

uniform vec3 camera_position; 

uniform float foamBias;

uniform float noiseSize;
uniform float roughness;

uniform float envStrength;

uniform float strengthAmplifier;

layout (binding = 1) uniform samplerCube skybox;

uniform float scatterStrength;// = 1;
uniform float scatterShadowStrength;// = 0.5;
uniform float bubbleDensity;// = 1;
uniform float foamRoughness;//= 0;

uniform float WavePeakScatterStrength;
uniform vec3 scatterColor;
uniform vec3 bubbleColor;// = vec3(0,0.1,0.0159);

uniform vec2 screenSize = vec2(1920,1080);
uniform float nearPlane = 0.1f;
uniform float farPlane = 5000.0f;

layout (binding = 0) uniform sampler2D foamTexture;
layout (binding = 2) uniform sampler2D depthTexture;

uniform bool calcFragmentNormalAndJacobian;

out vec4 FragColor;

in vec2 fragTexcoord;
in vec3 fragNormal;                    // world-space normal
in float fragJacobian;                 // Gerstner Jacobian
in vec3 vertexPosition;
in vec4 localPosition;

float LinearizeDepth(float d, float near, float far)
{
    float z = d * 2.0 - 1.0;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

float P22(float ndoth, float a) {
    float a2 = a * a;
	float exp_arg = (ndoth * ndoth - 1) / (a2 * ndoth * ndoth);
    float PI = 3.1415926535;
	return exp(exp_arg) / (PI * a2 * ndoth * ndoth * ndoth * ndoth);
}

float A(vec3 H, vec3 S, float roughness) {
    float hdots = max(0.001f, max(dot(H, S),0.0));
	float a = hdots / (roughness * sqrt(1 - hdots * hdots));
	float a2 = a * a;

	return a < 1.6f ? (1.0f - 1.259f * a + 0.396f * a2) / (3.535f * a + 2.181 * a2) : 0.0f;
}

vec3 clampLuminance(vec3 color)
{
    float Y = dot(color, vec3(0.2126, 0.7152, 0.0722));
    return color / max(Y, 1.0);
}

void gerstnerWave(
    in vec2 pos,
    in float A,
    in float k,
    in float speed,
    in vec2 D,
    in float wPhase,
    inout vec4 position,
    inout vec3 tangentX,
    inout vec3 tangentZ,
    inout float dxdx,
    inout float dxdz,
    inout float dzdx,
    inout float dzdz
) {
    A = A / scaling;
    A = A * strengthAmplifier;

    float phase = k * dot(D, pos * scaling) + speed * elapsed_time + wPhase;
    float sinP = sin(phase);
    float cosP = cos(phase);

    float QA = steepness / k;

    position.x += D.x * QA * cosP;
    position.y += A * sinP;
    position.z += D.y * QA * cosP;

    float com = QA * sinP * k * scaling;

    dxdx += -D.x * D.x * com;
    dxdz += -D.x * D.y * com;
    dzdx += -D.y * D.x * com;
    dzdz += -D.y * D.y * com;

    float derivK = k * scaling;

    tangentX += vec3(
        -D.x * D.x * QA * sinP * derivK,
         D.x * A * cosP * derivK,
        -D.x * D.y * QA * sinP * derivK
    );

    tangentZ += vec3(
        -D.x * D.y * QA * sinP * derivK,
         D.y * A * cosP * derivK,
        -D.y * D.y * QA * sinP * derivK
    );
}

void calculateNormalAndJacobian(inout vec3 normal,inout float jacobian, inout vec3 pos){
    vec3 tangentX = vec3(1, 0, 0);
    vec3 tangentZ = vec3(0, 0, 1);
    float dxdx = 1.0;
    float dxdz = 0.0;
    float dzdx = 0.0;
    float dzdz = 1.0;

    vec4 localPos = localPosition;
    for (int i = 0; i < waveCount; ++i)
    {
        Wave w = waves[i];

        gerstnerWave(
            vertexPosition.xz,
            w.A,
            w.k,
            w.speed,
            normalize(w.D),
            w.phase,
            localPos,
            tangentX,
            tangentZ,
            dxdx,
            dxdz,
            dzdx,
            dzdz
        );
    }
    normal = normalize(cross(tangentZ, tangentX));
    jacobian = dxdx * dzdz - dxdz * dzdx; 
    pos = localPos.xyz;
}

void main()
{
    
    vec3 viewDir = normalize(camera_position - vertexPosition);

    vec3 normal;
    float jacobian;
    vec3 pos;

    if(calcFragmentNormalAndJacobian){
        calculateNormalAndJacobian(normal,jacobian,pos);
    }else{
        normal = fragNormal;
        jacobian = fragJacobian;
        pos = vertexPosition;
    }

    //float foamBias = 1;
    float foam = max(0.0f, -(jacobian - foamBias));
    float foamMask = pow(foam, 3.0);
    vec3 //foamColor = vec3(1,1,1);
    foamColor = texture(foamTexture,vertexPosition.xz * noiseSize).rgb;

    //FragColor = vec4(mix(normal,foamColor,foam),1);




    //float WavePeakScatterStrength = 1;
    float waveHeight = max(0.0f,pos.y);
    vec3 lightDir = normalize(light_direction);
    vec3 mesoNormal = normalize(normal);
    float NdotL = max(dot(mesoNormal, lightDir), 0.0);

    //float _ScatterStrength = 1;
    //float _ScatterShadowStrength = 0.5;
    //float bubbleDensity = 1;

    float k1 = WavePeakScatterStrength * waveHeight * pow(max(dot(lightDir, -viewDir), 0.0), 4.0f) * pow(0.5f - 0.5f * dot(lightDir, mesoNormal), 3.0f);
    float k2 = scatterStrength * pow(max(dot(viewDir, mesoNormal),0.0), 2.0f);
    float k3 = scatterShadowStrength * NdotL;
    float k4 = bubbleDensity;

    //vec3 //scatterColor = vec3(0.016,0.15,0.16) * 2;
    //scatterColor = scatterColor;
    //vec3 Lsun = vec3(1,1,1);//vec3(1,0.694, 0.32);
    //vec3 bubbleColor = vec3(0,0.1,0.0159);


    vec3 halfwayDir = normalize(lightDir + viewDir);

    //sqrt(ax^2cos^2phi+ay^2sin^2phi) ??
    float _Roughness = 0.075;
    _Roughness = roughness;
    float _FoamRoughness = 0;
    _FoamRoughness = foamRoughness;
    float a = _Roughness + foamMask * _FoamRoughness;

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



    //Lsun_fr_cos /=4 * (wn*weye)*(1+A(wsun)+A/weye))
    /*
	Lsun_fr_cos /= 4.0f * max(0.001f, max(dot(macroNormal, lightDir),0));//weye??
	Lsun_fr_cos *= max(dot(mesoNormal, lightDir),0);
    */

    vec3 Lsun_fr_cos = Lsun * F * P22(ndoth, a);
    Lsun_fr_cos /= (4.0f * max(0.001f, max(dot(mesoNormal, viewDir),0)) * (1 + viewMask + lightMask));//weye??




    vec3 Lenv = texture(skybox,reflect(-viewDir,mesoNormal)).xyz;// vec3(0.5,0.5,0.5);//sampleEnvironmentFromSlopes(viewDir,skybox,slopeX,slopeY,leadr,leadr,0);//covarianceXY

    //float envStrength = 0.5;
    Lenv *= envStrength;

    vec3 Lto_eye = (1 - F) * Lscatter + Lsun_fr_cos + F * Lenv;


    if(foamMask>1)foamMask=1;

    foamColor = clampLuminance(foamColor);

    FragColor = vec4(mix(Lto_eye,foamColor,foamMask),1);

    //float greyNormal = normal.x+normal.y+normal.z;
    //FragColor = vec4(greyNormal,greyNormal,greyNormal,1);

    //FragColor = vec4(F,F,F,1); //NORMAL CORRECT!!!


                                        //FragColor = vec4(Lto_eye,1);


    //FragColor = vec4(jacobian,jacobian,jacobian,1);






    float xyDistanceToCamera = length(camera_position.xz - vertexPosition.xz);
    float fogStart = 200.0 * 2;
    float fogEnd   = 400.0 * 2;
    float fogFactor = (xyDistanceToCamera - fogStart) / (fogEnd - fogStart);
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    FragColor = vec4(FragColor.rgb, 1.0 - fogFactor);

    vec2 uv = gl_FragCoord.xy / screenSize;
    float sceneDepth = texture(depthTexture, uv).r;
    float waterDepth = gl_FragCoord.z;

    float sceneZ = LinearizeDepth(sceneDepth, nearPlane, farPlane);
    float waterZ = LinearizeDepth(waterDepth, nearPlane, farPlane);
    float diff = sceneZ - waterZ;

    float boxfoam = smoothstep(2.0f, 0.0, diff);

    //fixes depth-bug with skybox
    if(xyDistanceToCamera<fogStart)
        FragColor = mix(FragColor,vec4(foamColor,1),boxfoam);



}