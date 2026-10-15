using OpenTK.Graphics.OpenGL;
using cgimin.engine.object3d;
using cgimin.engine.camera;
using OpenTK.Mathematics;
using cgimin.engine.light;
using Examples.Tutorial;
using cgi.game_objects;
using cgimin.engine.texture;
using cgimin.engine.material;
using cgi.settings;

namespace cgi.shaders.tessellation.FFT
{
    public class FFTTessellationMaterial : BaseMaterial
    {
        private int modelLocation;
        private int viewLocation;
        private int projectionLocation;

        private int screenSizeLocation;
        private int farPlaneLocation;
        private int nearPlaneLocation;

        private int lightDirectionLocation;
        private int lSunLocation;

        private int uvScale0Location;
        private int uvScale1Location;
        private int uvScale2Location;

        private int isActive0Location;
        private int isActive1Location;
        private int isActive2Location;

        private int foamMult0Location;
        private int foamMult1Location;
        private int foamMult2Location;

        private int toggleGridLocation;

        int camPositionLocation;

        int leadrLocation;
        int WavePeakScatterStrengthLocation;
        int envStrengthLocation;
        int roughnessLocation;

        int scatterStrengthLocation;
        int scatterShadowStrengthLocation;
        int bubbleDensityLocation;
        int foamRoughnessLocation;

        int scatterColorLocation;
        int bubbleColorLocation;

        int minDistanceLocation;
        int maxDistanceLocation;
        int minTessLevelLocation;
        int maxTessLevelLocation;

        public int foamTexture0;
        int foamTexture1;

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

        public FFTTessellationMaterial()
        {

            // Create full tessellation shader pipeline
            CreateShaderProgram(
                "shaders/tessellation/Tess_VS.glsl",
                "shaders/tessellation/Tess_TCS.glsl",
                "shaders/tessellation/FFT/FFT_Tess_TES.glsl",
                "shaders/tessellation/FFT/FFT_Tess_FS.glsl"
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

            lightDirectionLocation = GL.GetUniformLocation(Program, "light_direction");
            lSunLocation = GL.GetUniformLocation(Program, "Lsun");

            uvScale0Location = GL.GetUniformLocation(Program, "uvScale0");
            uvScale1Location = GL.GetUniformLocation(Program, "uvScale1");
            uvScale2Location = GL.GetUniformLocation(Program, "uvScale2");

            isActive0Location = GL.GetUniformLocation(Program, "isActive0");
            isActive1Location = GL.GetUniformLocation(Program, "isActive1");
            isActive2Location = GL.GetUniformLocation(Program, "isActive2");

            foamMult0Location = GL.GetUniformLocation(Program, "foamMult0");
            foamMult1Location = GL.GetUniformLocation(Program, "foamMult1");
            foamMult2Location = GL.GetUniformLocation(Program, "foamMult2");

            toggleGridLocation = GL.GetUniformLocation(Program, "toggleGrid");

            camPositionLocation = GL.GetUniformLocation(Program, "cameraPosition");


            leadrLocation = GL.GetUniformLocation(Program, "leadr");
            WavePeakScatterStrengthLocation = GL.GetUniformLocation(Program, "WavePeakScatterStrength");
            envStrengthLocation = GL.GetUniformLocation(Program, "envStrength");
            roughnessLocation = GL.GetUniformLocation(Program, "roughness");

            scatterStrengthLocation = GL.GetUniformLocation(Program, "scatterStrength");
            scatterShadowStrengthLocation = GL.GetUniformLocation(Program, "scatterShadowStrength");
            bubbleDensityLocation = GL.GetUniformLocation(Program, "bubbleDensity");
            foamRoughnessLocation = GL.GetUniformLocation(Program, "foamRoughness");

            scatterColorLocation = GL.GetUniformLocation(Program, "scatterColor");
            bubbleColorLocation = GL.GetUniformLocation(Program, "bubbleColor");

            foamTexture0 = TextureManager.LoadTexture("shaders/foam/foam04.jpg");
            foamTexture1 = TextureManager.LoadTexture("shaders/foam/foam03.jpg");
        }

        public void Draw(BaseQuadObject3D object3d, Vector2 screenSize, JonswapSpectrumGenerator spectrum0, JonswapSpectrumGenerator spectrum1, JonswapSpectrumGenerator spectrum2, JONSWAP_SpectrumParameters p0, JONSWAP_SpectrumParameters p1, JONSWAP_SpectrumParameters p2, bool toggleGrid, int skyboxTexture,int depthTexture)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, spectrum0.offsetTex);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, spectrum0.normalAndFoamTex);



            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, spectrum1.offsetTex);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, spectrum1.normalAndFoamTex);



            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, spectrum2.offsetTex);

            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, spectrum2.normalAndFoamTex);


            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);

            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, foamTexture0);

            GL.ActiveTexture(TextureUnit.Texture8);
            GL.BindTexture(TextureTarget.Texture2D, foamTexture1);

            GL.ActiveTexture(TextureUnit.Texture9);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);

            // Use shader
            GL.UseProgram(Program);

            // Bind VAO
            GL.BindVertexArray(object3d.Vao);

            GL.Uniform1(GL.GetUniformLocation(Program, "offsetTexture0"), 0);
            GL.Uniform1(GL.GetUniformLocation(Program, "normalAndFoamTexture0"), 1);

            GL.Uniform1(GL.GetUniformLocation(Program, "offsetTexture1"), 2);
            GL.Uniform1(GL.GetUniformLocation(Program, "normalAndFoamTexture1"), 3);

            GL.Uniform1(GL.GetUniformLocation(Program, "offsetTexture2"), 4);
            GL.Uniform1(GL.GetUniformLocation(Program, "normalAndFoamTexture2"), 5);

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
            GL.Uniform3(lSunLocation, PBRSettings.Lsun);

            GL.Uniform1(uvScale0Location, p0.uvScale);
            GL.Uniform1(uvScale1Location, p1.uvScale);
            GL.Uniform1(uvScale2Location, p2.uvScale);

            GL.Uniform1(isActive0Location, p0.isActive ? 1 : 0);
            GL.Uniform1(isActive1Location, p1.isActive ? 1 : 0);
            GL.Uniform1(isActive2Location, p2.isActive ? 1 : 0);

            GL.Uniform1(foamMult0Location, p0.foamMult);
            GL.Uniform1(foamMult1Location, p1.foamMult);
            GL.Uniform1(foamMult2Location, p2.foamMult);

            GL.Uniform1(leadrLocation, PBRSettings.leadr);
            GL.Uniform1(WavePeakScatterStrengthLocation, PBRSettings.WavePeakScatterStrength);
            GL.Uniform1(envStrengthLocation, PBRSettings.envStrength);
            GL.Uniform1(roughnessLocation, PBRSettings.roughness);

            GL.Uniform3(scatterColorLocation, PBRSettings.scatterColor);
            GL.Uniform3(bubbleColorLocation, PBRSettings.bubbleColor);
            
            //if render

            GL.Uniform1(scatterStrengthLocation, PBRSettings.scatterStrength);
            GL.Uniform1(scatterShadowStrengthLocation, PBRSettings.scatterShadowStrength);
            GL.Uniform1(bubbleDensityLocation, PBRSettings.bubbleDensity);
            GL.Uniform1(foamRoughnessLocation, PBRSettings.foamRoughness);

            GL.Uniform3(camPositionLocation, Camera.position);

            float x = 0;
            if (toggleGrid) x = 1;
            GL.Uniform1(toggleGridLocation, x);

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