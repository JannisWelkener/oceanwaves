using cgi.game_objects;
using cgi.settings;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;


namespace cgi.shaders.gerstner
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Wave
    {
        public float A;
        public float k;
        public float speed;
        public float phase;
        public Vector2 D;
    }

    public sealed class GerstnerWaves
    {
        private static Random rand = new Random();

        private static  GerstnerWaves _instance;
        private GerstnerWaves() {
            wavesArray = waves.ToArray();
        }
        public static GerstnerWaves Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GerstnerWaves();
                }
                return _instance;
            }
        }

        public volatile float steepness = 0.4f;
        public volatile float scaling = /*1.0f*/ 0.01f;//0.00001f
        public volatile float noiseSize = 0.05f;
        public volatile float foamThreshold = 0.5f;

        public volatile float foamBias = 1.0f;
        public volatile float playbackSpeed = 0.1f;

        public volatile float strengthAmplifier = 2f;

        public Wave[] wavesArray;

        public List<Wave> waves = new()
        { 
            // Large dominant waves
            new Wave { A = 0.4f *5, k = 0.4f * 0.2f, speed = 1.0f, D = new Vector2(1.0f, 0.0f), phase = 0f },
            new Wave { A = 0.35f *5, k = 0.35f * 0.2f, speed = 0.9f, D = new Vector2(0.8f, 0.6f), phase = 0f },
            new Wave { A = 0.3f *2, k = 0.3f, speed = 1.1f, D = new Vector2(-0.6f, 0.8f), phase = 0f },
            new Wave { A = 0.25f *2, k = 0.25f, speed = 0.8f, D = new Vector2(-0.8f, -0.5f), phase = 0f },

            // Medium waves
            new Wave { A = 0.2f, k = 0.6f, speed = 1.4f, D = new Vector2(0.5f, 0.9f), phase = 0f },
            new Wave { A = 0.18f, k = 0.7f, speed = 1.2f, D = new Vector2(-0.3f, 1.0f), phase = 0f },
            new Wave { A = 0.15f, k = 0.8f, speed = 1.6f, D = new Vector2(0.7f, -0.7f), phase = 0f },
            new Wave { A = 0.14f, k = 0.9f, speed = 1.3f, D = new Vector2(-0.5f, -0.8f), phase = 0f },

            // Small, choppy waves
            new Wave { A = 0.1f, k = 1.2f, speed = 2.0f, D = new Vector2(0.2f, 1.0f), phase = 0f },
            new Wave { A = 0.09f, k = 1.5f, speed = 2.2f, D = new Vector2(-0.7f, 0.7f), phase = 0f },
            new Wave { A = 0.08f, k = 1.8f, speed = 2.5f, D = new Vector2(1.0f, 0.2f), phase = 0f },
            new Wave { A = 0.07f, k = 2.0f, speed = 2.1f, D = new Vector2(-0.6f, -0.8f), phase = 0f },

            // Extra fine detail waves
            new Wave { A = 0.05f, k = 2.5f, speed = 3.0f, D = new Vector2(0.8f, 0.6f), phase = 0f },
            new Wave { A = 0.04f, k = 3.0f, speed = 3.2f, D = new Vector2(-0.9f, 0.3f), phase = 0f },
            new Wave { A = 0.03f, k = 3.5f, speed = 3.5f, D = new Vector2(0.5f, -0.9f), phase = 0f },
            new Wave { A = 0.02f, k = 4.0f, speed = 4.0f, D = new Vector2(-0.2f, 1.0f), phase = 0f },
        };
        private static float Frequency(float k, float g, float depth)
        {
            return (float) Math.Sqrt(g * k * Math.Tanh(Math.Min(k * depth, 20)));
        }

        private static float TMACorrection(float omega, float g, float depth)
        {
            float omegaH = (float) (omega * Math.Sqrt(depth / g));
            if (omegaH <= 1)
                return (float) (0.5 * omegaH * omegaH);
            if (omegaH < 2)
                return (float) (1.0 - 0.5 * (2.0 - omegaH) * (2.0 - omegaH));
            return 1;
        }
        private static float JONSWAP(float omega, float g, float depth, JONSWAP_SpectrumParameters pars)
        {
            float sigma;
            if (omega <= pars.peakOmega)
                sigma = 0.07f;
            else
                sigma = 0.09f;
            float r = (float) Math.Exp(-(omega - pars.peakOmega) * (omega - pars.peakOmega)
                / 2 / sigma / sigma / pars.peakOmega / pars.peakOmega);

            float oneOverOmega = 1 / omega;
            float peakOmegaOverOmega = pars.peakOmega / omega;
            return (float) (pars.scale * TMACorrection(omega, g, depth) * pars.alpha * g * g
                * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega
                * Math.Exp(-1.25 * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega)
                * Math.Pow(Math.Abs(pars.gamma), r));
        }

        private static float SpreadPower(float omega, float peakOmega)
        {
            if (omega > peakOmega)
            {
                return (float) (9.77 * Math.Pow(Math.Abs(omega / peakOmega), -2.5));
            }
            else
            {
                return (float) (6.97 * Math.Pow(Math.Abs(omega / peakOmega), 5));
            }
        }

        private static float NormalisationFactor(float s)
        {
            float s2 = s * s;
            float s3 = s2 * s;
            float s4 = s3 * s;
            if (s < 5)
                return -0.000564f * s4 + 0.00776f * s3 - 0.044f * s2 + 0.192f * s + 0.163f;
            else
                return -4.80e-08f * s4 + 1.07e-05f * s3 - 9.53e-04f * s2 + 5.90e-02f * s + 3.93e-01f;
        }

        private static float Cosine2s(float theta, float s)
        {
            return (float) (NormalisationFactor(s) * Math.Pow(Math.Abs(Math.Cos(0.5 * theta)), 2 * s));
        }

        private static float Mix(float x,float y,float a) {
            return x * (1.0f - a) + y * a;
        }
        private static float DirectionSpectrum(float theta, float omega, JONSWAP_SpectrumParameters pars)
        {
            float s = (float) (SpreadPower(omega, pars.peakOmega)
                + 16 * Math.Tanh(Math.Min(omega / pars.peakOmega, 20)) * pars.swell * pars.swell);
            return Mix((float) (2 / 3.1415 * Math.Cos(theta) * Math.Cos(theta)), Cosine2s(theta - pars.angleRadian, s), pars.spreadBlend);
        }

        private static float ShortWavesFade(float kLength, JONSWAP_SpectrumParameters pars)
        {
            return (float) Math.Exp(-pars.shortWavesFade * pars.shortWavesFade * kLength * kLength);
        }

        public static float GaussianRandom(float mean = 0f, float stdDev = 1f)
        {
            // Uniform random values in (0,1]
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();

            // Standard normal distributed value
            double randStdNormal =
                Math.Sqrt(-2.0 * Math.Log(u1)) *
                Math.Sin(2.0 * Math.PI * u2);

            // Scale and shift
            return (float)(mean + stdDev * randStdNormal);
        }

        public void CalculateWavesFromJONSWAP(JONSWAP_SpectrumParameters parameters)
        {
            float PI = 3.1415926f;

            //16 x 16
            int size = 8;// 16;
            int waveCount = size * size;//max: 2048, Warnung: Starke PC-Auslastung!!
            

            waves.Clear();
            int count = 0;
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++)
                {
                    int nx = i - size / 2;
                    int nz = j - size / 2;

                    float kMin = parameters.cutoffLow;
                    float kMax = parameters.cutoffHigh;

                    float deltaK = (kMax - kMin) / waveCount;

                    //Vector2 k = new Vector2(nx, nz) * deltaK;
                    //float kLength = k.Length;

                    //Console.WriteLine(kMin + " "+kMax);
                    float kMag = kMin + (float)rand.NextDouble() * (kMax - kMin);

                    float theta = (float)rand.NextDouble() * 2f * PI;

                    Vector2 k = new Vector2(
                        MathF.Cos(theta),
                        MathF.Sin(theta)
                    ) * kMag;
                    float kLength = k.Length;


                    float angle = (float)Math.Atan2(k.Y, k.X);
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                    float omega = Frequency(kLength, parameters.gravity, parameters.depth);

                    float spectrum = JONSWAP(omega, parameters.gravity, parameters.depth, parameters) *
                                     DirectionSpectrum(angle, omega, parameters) *
                                     ShortWavesFade(kLength, parameters);

                    float deltaTheta = 2 * PI / waveCount;
                    //float A = (float)Math.Sqrt(2 * spectrum * deltaK * deltaK /*deltaTheta*/);

                    
                    float A = MathF.Sqrt(2f * spectrum * kLength * deltaK * deltaTheta);


                    float phase = GaussianRandom() * 2 * PI;

                    waves.Add(new Wave
                    {
                        A = A,
                        k = kLength, 
                        phase = phase,
                        speed = omega,
                        D = dir
                    });

                    if(GlobalSettings.advancedDebug)
                        Console.WriteLine("COUNT: " + count + " A: " + A + " K: " + kLength + " speed: " + omega / kLength + " D: " + dir.ToString());

                    count++;
                }
            }
            Console.WriteLine("Gerstner Waves updated");
            wavesArray = waves.ToArray();
        }
    }
}
