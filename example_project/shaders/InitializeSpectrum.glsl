#version 430
precision highp float;

#define PI 3.1415926

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout (binding = 0, rgba32f) uniform image2D H0;
layout (binding = 1, rgba32f) uniform image2D WavesData;

uniform bool Initial;
uniform int Size;

struct SpectrumParameters
{
	float scale;
	float angle;
	float spreadBlend;
	float swell;
	float alpha;
	float peakOmega;
	float gamma;
	float shortWavesFade;
	float LengthScale;
    float GravityAcceleration;
    float Depth;
    float CutoffHigh;
    float CutoffLow;
};

layout(std430, binding = 0) buffer Spectrums {
    SpectrumParameters spectrumParameters;
};

// Simple hash-based RNG
float rand(inout uint state)
{
    state ^= state << 13;
    state ^= state >> 17;
    state ^= state << 5;
    return float(state) * (1.0 / 4294967296.0); // [0,1)
}

// Generate a seed from invocation ID + external seed
uint initSeed(uvec3 id, uint seed)
{
    uint s = seed;
    s ^= id.x * 374761393u;
    s ^= id.y * 668265263u;
    s ^= id.z * 2147483647u;
    return s;
}

vec2 gaussianNoise(inout uint state)
{
    float u1 = 1.0 - rand(state);
    float u2 = 1.0 - rand(state);

    float r = sqrt(-2.0 * log(u1));
    float theta = 2.0 * 3.14159265359 * u2;

    return vec2(
        r * cos(theta), // first Gaussian
        r * sin(theta)  // second Gaussian
    );
}

float Frequency(float k, float g, float depth)
{
	return sqrt(g * k * tanh(min(k * depth, 20)));
}

float FrequencyDerivative(float k, float g, float depth)
{
	float th = tanh(min(k * depth, 20));
	float ch = cosh(k * depth);
	return g * (depth * k / ch / ch + th) / Frequency(k, g, depth) / 2;
}

float TMACorrection(float omega, float g, float depth)
{
	float omegaH = omega * sqrt(depth / g);
	if (omegaH <= 1)
		return 0.5 * omegaH * omegaH;
	if (omegaH < 2)
		return 1.0 - 0.5 * (2.0 - omegaH) * (2.0 - omegaH);
	return 1;
}

float JONSWAP(float omega, float g, float depth, SpectrumParameters pars)
{
	float sigma;
	if (omega <= pars.peakOmega)
		sigma = 0.07;
	else
		sigma = 0.09;
	float r = exp(-(omega - pars.peakOmega) * (omega - pars.peakOmega)
		/ 2 / sigma / sigma / pars.peakOmega / pars.peakOmega);
	
	float oneOverOmega = 1 / omega;
	float peakOmegaOverOmega = pars.peakOmega / omega;
	return pars.scale * TMACorrection(omega, g, depth) * pars.alpha * g * g
		* oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega
		* exp(-1.25 * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega)
		* pow(abs(pars.gamma), r);
}

float SpreadPower(float omega, float peakOmega)
{
	if (omega > peakOmega)
	{
		return 9.77 * pow(abs(omega / peakOmega), -2.5);
	}
	else
	{
		return 6.97 * pow(abs(omega / peakOmega), 5);
	}
}

float NormalisationFactor(float s)
{
	float s2 = s * s;
	float s3 = s2 * s;
	float s4 = s3 * s;
	if (s < 5)
		return -0.000564 * s4 + 0.00776 * s3 - 0.044 * s2 + 0.192 * s + 0.163;
	else
		return -4.80e-08 * s4 + 1.07e-05 * s3 - 9.53e-04 * s2 + 5.90e-02 * s + 3.93e-01;
}

float Cosine2s(float theta, float s)
{
	return NormalisationFactor(s) * pow(abs(cos(0.5 * theta)), 2 * s);
}

float DirectionSpectrum(float theta, float omega, SpectrumParameters pars)
{
	float s = SpreadPower(omega, pars.peakOmega)
		+ 16 * tanh(min(omega / pars.peakOmega, 20)) * pars.swell * pars.swell;
	return mix(2 / 3.1415 * cos(theta) * cos(theta), Cosine2s(theta - pars.angle, s), pars.spreadBlend);
}

float ShortWavesFade(float kLength, SpectrumParameters pars)
{
	return exp(-pars.shortWavesFade * pars.shortWavesFade * kLength * kLength);
}

void CalculateInitialSpectrum(){
	ivec2 id = ivec2(gl_GlobalInvocationID.xy);
	float deltaK = 2 * PI / spectrumParameters.LengthScale;

	int nx = int(gl_GlobalInvocationID.x) - Size / 2; /* 0 -> (Size / 2) */
	int nz = int(gl_GlobalInvocationID.y) - Size / 2;


	vec2 k = vec2(nx, nz) * deltaK;
	float kLength = length(k);

	if (kLength <= spectrumParameters.CutoffHigh && kLength >= spectrumParameters.CutoffLow)
	{

	   float kAngle = atan(k.y, k.x);
	   float omega = Frequency(kLength, spectrumParameters.GravityAcceleration, spectrumParameters.Depth);

	   imageStore(WavesData,id, vec4(k.x, 1 / kLength, k.y, omega));
   

		float dOmegadk = FrequencyDerivative(kLength, spectrumParameters.GravityAcceleration, spectrumParameters.Depth);

		float spectrum = JONSWAP(omega, spectrumParameters.GravityAcceleration, spectrumParameters.Depth, spectrumParameters)

			* DirectionSpectrum(kAngle, omega, spectrumParameters) * ShortWavesFade(kLength, spectrumParameters);

		uint seed = initSeed(gl_GlobalInvocationID, 12345u);
		vec2 noiseSample = gaussianNoise(seed);

		

		vec2 h0kVal = noiseSample * sqrt(2 * spectrum * abs(dOmegadk) / kLength * deltaK * deltaK);

		imageStore(H0,id, vec4(h0kVal,0,0));
	}
	else
	{
		//Zero Wave
		imageStore(H0,id, vec4(0,0,0,0));
		imageStore(WavesData,id, vec4(k.x, 1, k.y, 0));
	}
}

void CalculateConjugatedSpectrum()
{
	ivec2 id = ivec2(gl_GlobalInvocationID.xy);
	vec2 h0K = imageLoad(H0, id.xy).rg;

	vec2 h0MinusK = imageLoad(H0,ivec2((Size - id.x) % Size, (Size - id.y) % Size)).rg;

	imageStore(H0,id.xy,vec4(h0K.x, h0K.y, h0MinusK.x, -h0MinusK.y));
}

void main()
{
	if(Initial == true){
		CalculateInitialSpectrum();
	}
	else{
		CalculateConjugatedSpectrum();
	}
}

