using OpenTK.Graphics.OpenGL;
using cgimin.engine.object3d;
using cgimin.engine.camera;
using OpenTK.Mathematics;
using cgimin.engine.light;
using engine.cgimin.engine.material.ambientdiffuse;
using cgimin.engine.texture;
using cgi.shaders.gerstner;
using Examples.Tutorial;
using cgimin.engine.material;
using System.Runtime.InteropServices;
using cgi.settings;

namespace cgi.shaders.tessellation.Gerstner
{
    public class GerstnerTessellationMaterial : BaseMaterial
    {
        private int modelLocation;
        private int viewLocation;
        private int projectionLocation;

        private int screenSizeLocation;
        private int nearPlaneLocation;
        private int farPlaneLocation;

        private int lightDirectionLocation;
        private int lightAmbientLocation;
        private int lightDiffuseLocation;

        int lSunLocation;

        private GerstnerWaves gerstner = GerstnerWaves.Instance;

        private int timeLocation;
        private int steepnessLocation;
        private int scalingLocation;

        private int camPositionLocation;
        private int noiseSizeLocation;
        private int foamThresholdLocation;

        private int foamTexture;

        private int waveCountLocation;

        private int WavePeakScatterStrengthLocation;
        private int envStrengthLocation;
        private int roughnessLocation;
        private int strengthAmplifierLocation;

        int scatterStrengthLocation;
        int scatterShadowStrengthLocation;
        int bubbleDensityLocation;
        int foamRoughnessLocation;

        int scatterColorLocation;
        int bubbleColorLocation;

        private int foamBiasLocation;

        private int calcFragmentNormalAndJacobianLocation;
        public static bool calcFragmentNormalAndJacobian;

        int minDistanceLocation;
        int maxDistanceLocation;
        int minTessLevelLocation;
        int maxTessLevelLocation;

        int wavesSSBO;
        int arrayLen = 0;

        protected int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);

            // Attach source code
            GL.ShaderSource(shader, source);

            // Compile
            GL.CompileShader(shader);

            // Check compile status
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);

            if (status == (int)All.False)
            {
                // Get error log
                string log = GL.GetShaderInfoLog(shader);

                string shaderTypeName = type.ToString();

                throw new Exception(
                    $"[OpenGL] {shaderTypeName} compilation failed:\n{log}"
                );
            }

            return shader;
        }

        protected void CreateShaderProgram(
            string vsPath,
            string tcsPath,
            string tesPath,
            string fsPath)
        {
            int vs = CompileShader(ShaderType.VertexShader, File.ReadAllText(vsPath));
            int tcs = CompileShader(ShaderType.TessControlShader, File.ReadAllText(tcsPath));
            int tes = CompileShader(ShaderType.TessEvaluationShader, File.ReadAllText(tesPath));
            int fs = CompileShader(ShaderType.FragmentShader, File.ReadAllText(fsPath));

            Program = GL.CreateProgram();

            GL.AttachShader(Program, vs);
            GL.AttachShader(Program, tcs);
            GL.AttachShader(Program, tes);
            GL.AttachShader(Program, fs);
        }

        public GerstnerTessellationMaterial()
        {

            // Create full tessellation shader pipeline
            CreateShaderProgram(
                "shaders/tessellation/Tess_VS.glsl",
                "shaders/tessellation/Tess_TCS.glsl",
                "shaders/tessellation/Gerstner/Gerstner_Tess_TES.glsl",
                "shaders/tessellation/Gerstner/Gerstner_Tess_FS.glsl"
            );

            // Attribute bindings (optional but consistent with your engine)
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_normal");
            GL.BindAttribLocation(Program, 2, "in_uv");

            // Link AFTER attaching shaders + binding attributes
            GL.LinkProgram(Program);

            // Get uniform locations (USED IN TES)
            modelLocation = GL.GetUniformLocation(Program, "model");
            viewLocation = GL.GetUniformLocation(Program, "view");
            projectionLocation = GL.GetUniformLocation(Program, "projection");

            screenSizeLocation = GL.GetUniformLocation(Program, "screenSize");
            nearPlaneLocation = GL.GetUniformLocation(Program, "nearPlane");
            farPlaneLocation = GL.GetUniformLocation(Program, "farPlane");

            minDistanceLocation = GL.GetUniformLocation(Program, "MIN_DISTANCE");
            maxDistanceLocation = GL.GetUniformLocation(Program, "MAX_DISTANCE");

            minTessLevelLocation = GL.GetUniformLocation(Program, "MIN_TESS_LEVEL");
            maxTessLevelLocation = GL.GetUniformLocation(Program, "MAX_TESS_LEVEL");

            steepnessLocation = GL.GetUniformLocation(Program, "steepness");
            scalingLocation = GL.GetUniformLocation(Program, "scaling");

            lightDirectionLocation = GL.GetUniformLocation(Program, "light_direction");
            lightAmbientLocation = GL.GetUniformLocation(Program, "light_ambient_color");
            lightDiffuseLocation = GL.GetUniformLocation(Program, "light_diffuse_color");

            lSunLocation = GL.GetUniformLocation(Program, "Lsun");

            camPositionLocation = GL.GetUniformLocation(Program, "camera_position");
            noiseSizeLocation = GL.GetUniformLocation(Program, "noiseSize");
            foamThresholdLocation = GL.GetUniformLocation(Program, "foamThreshold");

            waveCountLocation = GL.GetUniformLocation(Program, "waveCount");

            foamBiasLocation = GL.GetUniformLocation(Program, "foamBias");
            WavePeakScatterStrengthLocation = GL.GetUniformLocation(Program, "WavePeakScatterStrength");
            envStrengthLocation = GL.GetUniformLocation(Program, "envStrength");
            roughnessLocation = GL.GetUniformLocation(Program, "roughness");

            strengthAmplifierLocation = GL.GetUniformLocation(Program, "strengthAmplifier");

            calcFragmentNormalAndJacobianLocation = GL.GetUniformLocation(Program, "calcFragmentNormalAndJacobian");


            scatterStrengthLocation = GL.GetUniformLocation(Program, "scatterStrength");
            scatterShadowStrengthLocation = GL.GetUniformLocation(Program, "scatterShadowStrength");
            bubbleDensityLocation = GL.GetUniformLocation(Program, "bubbleDensity");
            foamRoughnessLocation = GL.GetUniformLocation(Program, "foamRoughness");

            scatterColorLocation = GL.GetUniformLocation(Program, "scatterColor");
            bubbleColorLocation = GL.GetUniformLocation(Program, "bubbleColor");

            timeLocation = GL.GetUniformLocation(Program, "elapsed_time");

            foamTexture = TextureManager.LoadTexture("shaders/foam/foam03.jpg");


            wavesSSBO = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, wavesSSBO);
            
            arrayLen = gerstner.wavesArray.Length;//Save initial length to detect length changes
            GL.BufferData(
                BufferTarget.ShaderStorageBuffer,
                gerstner.wavesArray.Length * Marshal.SizeOf<Wave>(),
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw
            );

        }

        public void Draw(BaseQuadObject3D object3d, Vector2 screenSize, int skybox, int depthTexture)
        {
            // Use shader
            GL.UseProgram(Program);

            // Bind VAO
            GL.BindVertexArray(object3d.Vao);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, foamTexture);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureCubeMap, skybox);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);

            // --- Set matrices ---
            Matrix4 model = object3d.Transformation;
            Matrix4 view = Camera.Transformation;
            Matrix4 projection = Camera.PerspectiveProjection;

            GL.UniformMatrix4(modelLocation, false, ref model);
            GL.UniformMatrix4(viewLocation, false, ref view);
            GL.UniformMatrix4(projectionLocation, false, ref projection);

            GL.Uniform2(screenSizeLocation, GlobalSettings.screenSize);
            GL.Uniform1(nearPlaneLocation, Camera.nearPlane);
            GL.Uniform1(farPlaneLocation, Camera.farPlane);

            GL.Uniform1(minDistanceLocation, GlobalSettings.tessMinDistance);
            GL.Uniform1(maxDistanceLocation, GlobalSettings.tessMaxDistance);

            GL.Uniform1(minTessLevelLocation, GlobalSettings.minTessLevel);
            GL.Uniform1(maxTessLevelLocation, GlobalSettings.maxTessLevel);



            GL.Uniform3(lightDirectionLocation, Light.lightDirection);
            GL.Uniform4(lightAmbientLocation, Light.lightAmbient);
            GL.Uniform4(lightDiffuseLocation, Light.lightDiffuse);

            GL.Uniform3(lSunLocation, PBRSettings.Lsun);
            

            GL.Uniform1(foamBiasLocation, gerstner.foamBias);
            
            GL.Uniform1(scalingLocation, gerstner.scaling);
            GL.Uniform1(timeLocation, ElapsedTime.elapsedTime * gerstner.playbackSpeed);
            GL.Uniform1(steepnessLocation, gerstner.steepness);

            GL.Uniform3(camPositionLocation, Camera.position);
            GL.Uniform1(noiseSizeLocation, gerstner.noiseSize);
            GL.Uniform1(foamThresholdLocation, gerstner.foamThreshold);

            GL.Uniform1(waveCountLocation, gerstner.waves.Count);

            GL.Uniform1(WavePeakScatterStrengthLocation, PBRSettings.WavePeakScatterStrength);
            GL.Uniform1(envStrengthLocation, PBRSettings.envStrength);
            GL.Uniform1(roughnessLocation, PBRSettings.roughness);

            GL.Uniform1(strengthAmplifierLocation, gerstner.strengthAmplifier);




            GL.Uniform1(scatterStrengthLocation, PBRSettings.scatterStrength);
            GL.Uniform1(scatterShadowStrengthLocation, PBRSettings.scatterShadowStrength);
            GL.Uniform1(bubbleDensityLocation, PBRSettings.bubbleDensity);
            GL.Uniform1(foamRoughnessLocation, PBRSettings.foamRoughness);

            GL.Uniform3(scatterColorLocation, PBRSettings.scatterColor);
            GL.Uniform3(bubbleColorLocation, PBRSettings.bubbleColor);






            float x = 0;
            if (calcFragmentNormalAndJacobian) x = 1;
            GL.Uniform1(calcFragmentNormalAndJacobianLocation, x);

            


            //If array length changes, reallocate buffer
            if (arrayLen != gerstner.wavesArray.Length)
            {
                Console.WriteLine("New array length, was: " + arrayLen + " , new: " + gerstner.wavesArray.Length);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, wavesSSBO);

                GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    gerstner.wavesArray.Length * Marshal.SizeOf<Wave>(),
                    gerstner.wavesArray,
                    BufferUsageHint.DynamicDraw
                );

                arrayLen = gerstner.wavesArray.Length;
            }
            else
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, wavesSSBO);

                GL.BufferSubData(
                    BufferTarget.ShaderStorageBuffer,
                    IntPtr.Zero,
                    gerstner.wavesArray.Length * Marshal.SizeOf<Wave>(),
                    gerstner.wavesArray
                );
            }

            GL.BindBufferBase(
                BufferRangeTarget.ShaderStorageBuffer,
                0,
                wavesSSBO
            );



            GL.Enable(EnableCap.Blend); //TRANSPARANCY!! Added later
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);//TRANSPARANCY!! Added later




            // --- Enable wireframe mode ---
            if (ExampleProject.wireframe)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            // --- Tell OpenGL we are drawing patches (triangles) ---
            GL.PatchParameter(PatchParameterInt.PatchVertices, 4);

            // --- Draw ---
            GL.DrawElements(
                PrimitiveType.Patches,
                object3d.Indices.Count,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero
            );

            // --- Restore default polygon mode (IMPORTANT!) ---
            if (ExampleProject.wireframe)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }
    }
}