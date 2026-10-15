using cgimin.engine.camera;
using cgimin.engine.object3d;
using cgimin.engine.light;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using engine.cgimin.engine.material.ambientdiffuse;
using cgimin.engine.texture;
using cgi.game_objects;
using cgi.shaders.gerstner;

namespace cgimin.engine.material.ambientdiffuse
{
    public class GerstnerWaveMaterial : BaseMaterial
    {
        public static volatile bool wireframe = false;
        //public static float steepness = 3f;
        //public static volatile float noiseSize = 10.0f;
        //public static volatile float foamThreshold = 0.5f;
        //public static volatile float scaling = 1.0f;

        private int modelMatrixLocation;
        private int modelviewProjectionMatrixLocation;

        private int lightDirectionLocation;
        private int lightAmbientLocation;
        private int lightDiffuseLocation;

        private int timeLocation;
        private int steepnessLocation;

        private int chunkOffsetLocation;

        int waveCountLocation;
        int waveALocation;
        int waveKLocation;
        int waveSpeedLocation;
        int wavePhaseLocation;
        int waveDLocation;

        int noiseSizeLocation;
        int foamThresholdLocation;

        int waterNormalTexture;
        int foamTexture;

        int camPositionLocation;
        int scalingLocation;

        GerstnerWaves gerstner = GerstnerWaves.Instance;

        public void updateWaves()
        {
            //JONSWAP_SpectrumParameters jonswap = JONSWAP_SpectrumParameters.Instance;
            //gerstner.waves = GenerateWaves(100, /*jonswap.windSpeed, jonswap.fetch, */jonswap.gravity, jonswap.scale, jonswap.degree, jonswap.spreadBlend, jonswap.swell, jonswap.gamma, jonswap.alpha, jonswap.peakOmega);
        }

        public GerstnerWaveMaterial()
        {
            // shader-programm is loaded
            CreateShaderProgram("shaders/Gerstner_VS.glsl",
                                "shaders/Gerstner_FS.glsl");

            // GL.BindAttribLocation, defines which index of the data-structure is assigned to which "in" parameter 
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_normal");
            GL.BindAttribLocation(Program, 2, "in_uv");

            // ...has to be done before the final "link" of the shader-program
            GL.LinkProgram(Program);

            // the location of the "uniform"-paramter "modelview_projection_matrix" on the shader is saved to modelviewProjectionMatrixLocation
            modelviewProjectionMatrixLocation = GL.GetUniformLocation(Program, "modelview_projection_matrix");

            // the location of the "uniform"-paramter for the model matrix on the shader is saved to modelviewMatrixLocation
            modelMatrixLocation = GL.GetUniformLocation(Program, "model");

            /// the location of the "uniform"-paramters of the light parameters
            lightDirectionLocation = GL.GetUniformLocation(Program, "light_direction");
            lightAmbientLocation = GL.GetUniformLocation(Program, "light_ambient_color");
            lightDiffuseLocation = GL.GetUniformLocation(Program, "light_diffuse_color");

            timeLocation = GL.GetUniformLocation(Program, "elapsed_time");
            steepnessLocation = GL.GetUniformLocation(Program, "steepness");

            waveCountLocation = GL.GetUniformLocation(Program, "waveCount");

            waveALocation = GL.GetUniformLocation(Program, "waveA[0]");
            waveKLocation = GL.GetUniformLocation(Program, "waveK[0]");
            waveSpeedLocation = GL.GetUniformLocation(Program, "waveSpeed[0]");
            wavePhaseLocation = GL.GetUniformLocation(Program, "wavePhase[0]");
            waveDLocation = GL.GetUniformLocation(Program, "waveD[0]");

            noiseSizeLocation = GL.GetUniformLocation(Program, "noiseSize");
            foamThresholdLocation = GL.GetUniformLocation(Program, "foamThreshold");

            chunkOffsetLocation = GL.GetUniformLocation(Program, "chunkOffset");

            camPositionLocation = GL.GetUniformLocation(Program, "camera_position");

            scalingLocation = GL.GetUniformLocation(Program, "scaling");

            waterNormalTexture = TextureManager.LoadTexture("shaders/waternormals.jpg");
            foamTexture = TextureManager.LoadTexture("shaders/perlin-worly.jpg");

            //JONSWAP_SpectrumParameters jonswap = JONSWAP_SpectrumParameters.Instance;
            //waves = GenerateWaves(100, jonswap.windSpeed, jonswap.fetch, jonswap.gravity, jonswap.scale, jonswap.degree, jonswap.spreadBlend, jonswap.swell, jonswap.gamma, jonswap.alpha,jonswap.peakOmega);
            //foreach(Wave w in waves) { 
            //    Console.WriteLine(w.A + ", "+w.k+", "+w.speed+", "+w.D.ToString());
            //}

        }

        public void Draw(BaseObject3D object3d)
        {

            if (wireframe)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);


            // set the texture
            //GL.ActiveTexture(TextureUnit.Texture0); //reset to texture-location 0
            //GL.BindTexture(TextureTarget.Texture2D, textureID); //write to 0 for this draw-call

            // using the Vertex-Array-Object of out object
            GL.BindVertexArray(object3d.Vao);

           

            // using our shader
            GL.UseProgram(Program);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, waterNormalTexture);
            GL.Uniform1(GL.GetUniformLocation(Program, "normalSampler"), 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, foamTexture);
            GL.Uniform1(GL.GetUniformLocation(Program, "foamSampler"), 0);

            // The matrix which we give as "modelview_projection_matrix" is assembled:
            // object-transformation * camera-transformation * perspective projection of the camera
            // on the shader each vertex-position is multiplied by this matrix. The result is the final position on the screen
            Matrix4 modelViewProjection = object3d.Transformation * Camera.Transformation * Camera.PerspectiveProjection;

            // modelViewProjection is passed to the shader
            GL.UniformMatrix4(modelviewProjectionMatrixLocation, false, ref modelViewProjection);

            // The model matrix (just the transformation of the object) is also given to the shader. We want to multiply our normals with this matrix, to have them in world space.
            Matrix4 model = object3d.Transformation;
            GL.UniformMatrix4(modelMatrixLocation, false, ref model);

            // Die Licht Parameter werden übergeben
            GL.Uniform3(lightDirectionLocation, Light.lightDirection);
            GL.Uniform4(lightAmbientLocation, Light.lightAmbient);
            GL.Uniform4(lightDiffuseLocation, Light.lightDiffuse);

            GL.Uniform1(timeLocation, ElapsedTime.elapsedTime);
            GL.Uniform1(steepnessLocation, gerstner.steepness);

            GL.Uniform1(waveCountLocation, gerstner.waves.Count);

            GL.Uniform1(scalingLocation, gerstner.scaling);
            

            Vector3 pos = object3d.Transformation.ExtractTranslation();
            GL.Uniform2(chunkOffsetLocation, new Vector2(pos.X, pos.Z));
            
            GL.Uniform3(camPositionLocation, Camera.position);

            GL.Uniform1(noiseSizeLocation, gerstner.noiseSize);
            GL.Uniform1(foamThresholdLocation, gerstner.foamThreshold);


            float[] A = new float[gerstner.waves.Count];
            float[] K = new float[gerstner.waves.Count];
            float[] speed = new float[gerstner.waves.Count];
            float[] phase = new float[gerstner.waves.Count];
            Vector2[] D = new Vector2[gerstner.waves.Count];

            for (int i = 0; i < gerstner.waves.Count; i++)
            {
                A[i] = gerstner.waves[i].A;
                K[i] = gerstner.waves[i].k; 
                speed[i] = gerstner.waves[i].speed;
                phase[i] = gerstner.waves[i].phase;
                D[i] = gerstner.waves[i].D;
            }
            GL.Uniform1(waveALocation, gerstner.waves.Count, A);
            GL.Uniform1(waveKLocation, gerstner.waves.Count, K);
            GL.Uniform1(waveSpeedLocation, gerstner.waves.Count, speed);
            GL.Uniform1(wavePhaseLocation, gerstner.waves.Count,phase);
            GL.Uniform2(waveDLocation, gerstner.waves.Count, ref D[0].X);

            GL.Enable(EnableCap.Blend); //TRANSPARANCY!! Added later
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);//TRANSPARANCY!! Added later

            // Das Objekt wird gezeichnet
            GL.DrawElements(PrimitiveType.Triangles, object3d.Indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

















        public static List<Wave> GenerateWaves(
       int numWaves,
       float gravity,
       float scale,
       float mainDirectionDegrees,
       float spreadBlend,
       float swell,
       float gamma,
       float alpha,
       float peakOmega,
       float depth = 1000f)
        {
            List<Wave> waves = new List<Wave>(numWaves);
            Random rng = new Random();

            float deg2rad = MathF.PI / 180f;
            float mainDirection = mainDirectionDegrees * deg2rad;

            // Peak wave number
            float kPeak = (peakOmega * peakOmega) / gravity;

            // Log-distributed k range
            float kMin = 0.25f * kPeak;
            float kMax = 4f * kPeak;

            float previousK = kMin;

            // Energy normalization (prevents explosion with more waves)
            float energyNorm = 1f / MathF.Sqrt(numWaves);

            for (int i = 0; i < numWaves; i++)
            {
                float t = (float)i / (numWaves - 1);

                // --- Logarithmic sampling ---
                float k = kMin * MathF.Pow(kMax / kMin, t);

                float deltaK = (i == 0) ? (k - kMin) : (k - previousK);
                previousK = k;

                // --- Dispersion relation (finite depth) ---
                float kh = k * depth;
                float tanh_kh = MathF.Tanh(kh);
                float omega = MathF.Sqrt(gravity * k * tanh_kh);

                // --- Correct derivative dω/dk ---
                float sech_kh = 1f / MathF.Cosh(kh);
                float dOmegadk =
                    0.5f / omega *
                    gravity *
                    (tanh_kh + k * depth * sech_kh * sech_kh);

                // --- JONSWAP spectrum ---
                float sigma = (omega <= peakOmega) ? 0.07f : 0.09f;

                float diff = omega - peakOmega;
                float r = MathF.Exp(-(diff * diff) /
                    (2f * sigma * sigma * peakOmega * peakOmega));

                float peak = MathF.Pow(MathF.Abs(gamma), r);

                float basePart =
                    alpha * scale * gravity * gravity *
                    MathF.Pow(omega, -5f) *
                    MathF.Exp(-1.25f * MathF.Pow(peakOmega / omega, 4f));

                float S = basePart * peak;

                // --- Optional swell shaping (stable) ---
                float swellFactor = 1f + swell *
                    MathF.Exp(-MathF.Pow(omega / peakOmega, 2f));

                S *= swellFactor;

                // --- Directional spreading ---
                float spreadExponent = Lerp(1f, 50f, spreadBlend);

                // Uniform sampling (robust)
                float thetaOffset =
                    (float)((rng.NextDouble() * 2.0 - 1.0) * MathF.PI);

                float theta = mainDirection + thetaOffset;

                // Directional energy weight
                float spreadWeight = DirectionalSpread(thetaOffset, spreadExponent);

                // --- Angular interval approximation ---
                float deltaTheta = 2f * MathF.PI / numWaves;

                // --- Correct amplitude conversion ---
                float amplitude = MathF.Sqrt(
                    2f * S *
                    MathF.Abs(dOmegadk) *
                    deltaK *
                    deltaTheta
                );

                // Apply directional weighting
                amplitude *= MathF.Sqrt(spreadWeight);

                // Normalize total energy
                amplitude *= energyNorm;

                // --- Phase speed ---
                float phaseSpeed = omega / k;

                Vector2 direction = new Vector2(
                    MathF.Cos(theta),
                    MathF.Sin(theta)
                );

                // --- Steepness clamp (prevents loops) ---
                float steepness = k * amplitude;
                if (steepness > 0.8f)
                    amplitude *= 0.8f / steepness;

                waves.Add(new Wave
                {
                    A = amplitude * 400,
                    k = k,
                    speed = phaseSpeed,
                    D = Vector2.Normalize(direction)
                });
            }

            return waves;
        }

        static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        static float DirectionalSpread(float theta, float s)
        {
            float cosTheta = MathF.Cos(theta);
            return MathF.Pow(MathF.Max(cosTheta, 0f), s);
        }
    }
}

