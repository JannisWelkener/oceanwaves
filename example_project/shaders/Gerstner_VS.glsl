#version 330
precision highp float;

// input from VAO-Datenstruktur
in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform float elapsed_time;

uniform float steepness;

// modelview_projection_matrix as uniform parameter
uniform mat4 modelview_projection_matrix;

#define MAX_WAVES 16

uniform int waveCount;
uniform float waveA[MAX_WAVES];
uniform float waveK[MAX_WAVES];
uniform float waveSpeed[MAX_WAVES];
uniform float wavePhase[MAX_WAVES];
uniform vec2  waveD[MAX_WAVES];

uniform vec3 camera_position;

uniform vec2 chunkOffset;

uniform float scaling;
// "fragTexcoord" is passed to the fragment shader
out vec2 fragTexcoord;

out float fragJacobian;
out vec3 vertexPosition;


// die Normale wird ebenfalls an den Fragment-Shader übergeben
out vec3 fragNormal;

void gerstnerWave(
    in vec2 pos,
    in float A,
    in float k,
    in float speed,
    in vec2 D,
    inout vec3 position,
    inout vec3 tangentX,
    inout vec3 tangentZ,
    inout float dxdx,
    inout float dxdz,
    inout float dzdx,
    inout float dzdz
) {
    float phase = k * dot(D, pos * scaling) + speed * elapsed_time;
    float sinP = sin(phase);
    float cosP = cos(phase);

    float QA = steepness * A;

    position.x += D.x * QA * cosP;
    position.y += A * sinP;
    position.z += D.y * QA * cosP;

    float com = QA * sinP * k;

    // Jacobian partial derivatives
    dxdx += -D.x * D.x * com;
    dxdz += -D.x * D.y * com;
    dzdx += -D.y * D.x * com;
    dzdz += -D.y * D.y * com;

    // Tangents (unchanged)
    tangentX += vec3(
        -D.x * D.x * QA * sinP * k,
         D.x * A * cosP * k,
        -D.x * D.y * QA * sinP * k
    );

    tangentZ += vec3(
        -D.x * D.y * QA * sinP * k,
         D.y * A * cosP * k,
        -D.y * D.y * QA * sinP * k
    );
}



void main()
{
    fragTexcoord = in_uv;

    vec3 position = in_position;
    vec3 tangentX = vec3(1, 0, 0);
    vec3 tangentZ = vec3(0, 0, 1);

    float dxdx = 1.0;
    float dxdz = 0.0;
    float dzdx = 0.0;
    float dzdz = 1.0;
    
    for (int i = 0; i < waveCount; ++i)
    {
        vec2 worldPos = position.xz + chunkOffset;
        float distToCam = distance(camera_position.xz, worldPos);
        /*
        float filterStart = 300;
        float filterEnd = 600;
        //float attenuationFilter = 1.0 - smoothstep(filterStart, filterEnd, distToCam);
        float PI = 3.1415;
        float maxWavelength = 1;
        float wavelength = 2.0 * PI / waveK[i];
        float freqBias = clamp(wavelength / maxWavelength, 0.0, 1.0);

        float attenuationFilter = 1.0 - smoothstep(
            filterStart * freqBias,
            filterEnd   * freqBias,
            distToCam
        );
        */
        float phase = wavePhase[i];
        gerstnerWave(
            worldPos,
            waveA[i],// * attenuationFilter,
            waveK[i],
            waveSpeed[i],
            normalize(waveD[i]),
            position,
            tangentX,
            tangentZ,
            dxdx,
            dxdz,
            dzdx,
            dzdz
        );
    }
    

    fragNormal = normalize(cross(tangentZ, tangentX));

    float J = dxdx * dzdz - dxdz * dzdx;
    fragJacobian = J;


    // transform normal to world space if needed
    //fragNormal = normalize(mat3(model_matrix) * fragNormal);
    vertexPosition = vec3(in_position.x + chunkOffset.x,position.y,in_position.z + chunkOffset.y);

    gl_Position = modelview_projection_matrix * vec4(position, 1.0);
}



