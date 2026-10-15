
using cgi.utils;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using cgimin.engine.camera;
using engine.cgimin.engine.material.ambientdiffuse;
using cgimin.engine.texture;

namespace cgi.game_objects.particles
{
    public class SprayParticleSystem
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DrawElementsIndirectCommand
        {
            public uint count;
            public uint instanceCount;
            public uint firstIndex;
            public uint baseVertex;
            public uint baseInstance;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Particle
        {
            public Vector3 position;
            public float lifetime;
            public Vector3 direction;
            public float size;
            public float full_lifetime;
            public float rotation;
            public float rotationSpeed;
            public float type;
        }

        public static int yThreadAmount = 47;
        public static float sprayGenerationRadius = 295f;
        public static float foamThreshold = 0.6f / 3f;
        public static float particleLifetime = 0.76f;
        public static float verticalSpeedDampening = 0.42f;
        public static float particleSpeed = 2.0f;
        public static float particleSizeMin = 3.145f;
        public static float particleSizeMax = 6.472f;

        public static float mistPercentage = 324f;
        public static float mistTransparancy = 0.3f;
        public static float mistSize = 0.233f;
        public static float verticalSpeedDampeningMist = 0.35f;

        public static float colorCorrection = 1.54f;
        public static float sprayTransparancy = 0.75f;

        public static float windScaling = 1.0f;

        int renderParticlesIndirectShader;
        int sprayAccumulationShader;
        int sprayUpdateAndDeleteShader;

        int Program;
        int vaoId;

        int vertexBuffer;
        int indexBuffer;
        int indirectBuffer;
        int newParticleCountBuffer;
        int oldParticleCountBuffer;
        int uvBuffer;


        int newParticlesBuffer;
        int oldParticlesBuffer;


        int modelviewViewMatrixLocation;
        int viewMatrixLocation;
        int projectionMatrixLocation;
        int cameraPositionVSLocation;

        int cameraPositionLocation;

        int frameCounterLocation;
        int viewProjectionMatrixLocation;

        int uvScale0Location;
        int uvScale1Location;
        int uvScale2Location;

        int foamMult0Location;
        int foamMult1Location;
        int foamMult2Location;

        int isActive0Location;
        int isActive1Location;
        int isActive2Location;

        int yThreadAmountLocation;
        int sprayGenerationRadiusLocation;

        int offsetFoamTexture0;
        int offsetFoamTexture1;
        int offsetFoamTexture2;

        int foamThresholdLocation;
        int particleLifetimeLocation;
        int particleLifetimeVSLocation;

        int colorCorrectionLocation;
        int sprayTransparancyLocation;

        int verticalSpeedDampeningLocation;
        int particleSpeedLocation;
        int particleSizeMinLocation;
        int particleSizeMaxLocation;

        int mistPercentageLocation;
        int mistTransparancyLocation;
        int mistSizeLocation;
        int verticalSpeedDampeningMistLocation;

        int elapsedTimeLocation;
        int dominantWindLocation;
        int windScalingLocation;

        int splashTexture0;
        int splashTexture1;
        int splashTexture2;
        int splashTexture3;
        int mistTexture;
        int noiseTexture;

        JONSWAP_SpectrumParameters init0, init1, init2;

        public void Load(int offset0, int offset1, int offset2, JONSWAP_SpectrumParameters _init0, JONSWAP_SpectrumParameters _init1, JONSWAP_SpectrumParameters _init2) {
            init0 = _init0;
            init1 = _init1;
            init2 = _init2;

            sprayAccumulationShader = ShaderUtils.CompileComputeShader("shaders/spray/accumulateSpray.glsl");

            sprayUpdateAndDeleteShader = ShaderUtils.CompileComputeShader("shaders/spray/UpdateDeleteSpray.glsl");

            renderParticlesIndirectShader = ShaderUtils.CompileComputeShader("shaders/spray/Spray_CS.glsl");
            Program = ShaderUtils.LoadVertexAndFragmentShader("shaders/spray/Spray_VS.glsl", "shaders/spray/Spray_FS.glsl");

            offsetFoamTexture0 = offset0;
            offsetFoamTexture1 = offset1;
            offsetFoamTexture2 = offset2;

            vertexBuffer = GL.GenBuffer();//0
            indexBuffer = GL.GenBuffer();//1
            indirectBuffer = GL.GenBuffer();//2
            newParticleCountBuffer = GL.GenBuffer();//3
                                                                //workGroupBuffer = GL.GenBuffer();//4
            oldParticleCountBuffer = GL.GenBuffer();//5
            uvBuffer = GL.GenBuffer();//6


            newParticlesBuffer = GL.GenBuffer();//7
            oldParticlesBuffer = GL.GenBuffer();//8


            int vertexCount = 4;
            int indexCount = 6;


            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, vertexCount * Vector4.SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, vertexBuffer); // Binding index 0 for vertices

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, indexCount * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, indexBuffer);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, indirectBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, Marshal.SizeOf<DrawElementsIndirectCommand>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, indirectBuffer);

            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, newParticleCountBuffer);
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 3, newParticleCountBuffer);

            //4 unused, TODO: Lower all of the later ones by 1

            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, oldParticleCountBuffer);
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 5, oldParticleCountBuffer);

            // Bind and allocate buffer memory for UVs
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, uvBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, vertexCount * Vector2.SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, uvBuffer); // Binding index 1 for UVs

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newParticlesBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer,64*64 * 64 * Marshal.SizeOf<Particle>(), IntPtr.Zero,BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer,7,newParticlesBuffer);


            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, oldParticlesBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 64 * 64 * 64 * Marshal.SizeOf<Particle>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, oldParticlesBuffer);


            splashTexture0 = TextureManager.LoadTexture("shaders/sprayTexture00.png");
            splashTexture1 = TextureManager.LoadTexture("shaders/sprayTexture01.png");
            splashTexture2 = TextureManager.LoadTexture("shaders/sprayTexture02.png");
            splashTexture3 = TextureManager.LoadTexture("shaders/sprayTexture03.png");
            mistTexture = TextureManager.LoadTexture("shaders/mist_particle.png");

            noiseTexture = TextureManager.LoadTexture("shaders/perlin.png");


            vaoId = GL.GenVertexArray();
            BindVertexArray();

            modelviewViewMatrixLocation = GL.GetUniformLocation(Program, "modelview_projection_matrix");
            viewMatrixLocation = GL.GetUniformLocation(Program, "viewMatrix");
            projectionMatrixLocation = GL.GetUniformLocation(Program, "projectionMatrix");
            cameraPositionVSLocation = GL.GetUniformLocation(Program, "cameraPosition");
            particleLifetimeVSLocation = GL.GetUniformLocation(Program, "particleLifetime");
            mistTransparancyLocation = GL.GetUniformLocation(Program, "mistTransparancy");
            colorCorrectionLocation = GL.GetUniformLocation(Program, "colorCorrection");
            sprayTransparancyLocation = GL.GetUniformLocation(Program, "sprayTransparancy");

            cameraPositionLocation = GL.GetUniformLocation(sprayAccumulationShader, "cameraPosition");
            frameCounterLocation = GL.GetUniformLocation(sprayAccumulationShader, "frameCounter");
            viewProjectionMatrixLocation = GL.GetUniformLocation(sprayAccumulationShader, "view_projection_matrix");

            uvScale0Location = GL.GetUniformLocation(sprayAccumulationShader, "uvScale0");
            uvScale1Location = GL.GetUniformLocation(sprayAccumulationShader, "uvScale1");
            uvScale2Location = GL.GetUniformLocation(sprayAccumulationShader, "uvScale2");

            foamMult0Location = GL.GetUniformLocation(sprayAccumulationShader, "foamMult0");
            foamMult1Location = GL.GetUniformLocation(sprayAccumulationShader, "foamMult1");
            foamMult2Location = GL.GetUniformLocation(sprayAccumulationShader, "foamMult2");

            isActive0Location = GL.GetUniformLocation(sprayAccumulationShader, "isActive0");
            isActive1Location = GL.GetUniformLocation(sprayAccumulationShader, "isActive1");
            isActive2Location = GL.GetUniformLocation(sprayAccumulationShader, "isActive2");

            yThreadAmountLocation = GL.GetUniformLocation(sprayAccumulationShader, "yThreadAmount");
            sprayGenerationRadiusLocation = GL.GetUniformLocation(sprayAccumulationShader, "sprayGenerationRadius");
            foamThresholdLocation = GL.GetUniformLocation(sprayAccumulationShader, "foamThreshold");
            particleLifetimeLocation = GL.GetUniformLocation(sprayAccumulationShader, "particleLifetime");
            verticalSpeedDampeningLocation = GL.GetUniformLocation(sprayAccumulationShader, "verticalSpeedDampening");
            particleSpeedLocation = GL.GetUniformLocation(sprayAccumulationShader, "particleSpeed");
            particleSizeMinLocation = GL.GetUniformLocation(sprayAccumulationShader, "particleSizeMin");
            particleSizeMaxLocation = GL.GetUniformLocation(sprayAccumulationShader, "particleSizeMax");
            mistPercentageLocation = GL.GetUniformLocation(sprayAccumulationShader, "mistPercentage");
            mistSizeLocation = GL.GetUniformLocation(sprayAccumulationShader, "mistSize");
            verticalSpeedDampeningMistLocation = GL.GetUniformLocation(sprayAccumulationShader, "verticalSpeedDampeningMist");

            elapsedTimeLocation = GL.GetUniformLocation(sprayUpdateAndDeleteShader, "elapsedTime");
            dominantWindLocation = GL.GetUniformLocation(sprayUpdateAndDeleteShader, "dominantWind");
            windScalingLocation = GL.GetUniformLocation(sprayUpdateAndDeleteShader, "windScaling");
        }

        private void BindBase() { 
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, vertexBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, indexBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, indirectBuffer);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 3, newParticleCountBuffer);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 5, oldParticleCountBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, uvBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, newParticlesBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, oldParticlesBuffer);
        }

        public void FixedUpdate(float uvScale0, float uvScale1, float uvScale2, float foamMult0, float foamMult1, float foamMult2, bool isActive0, bool isActive1, bool isActive2)
        {
            BindBase();
            //Create new spray
            AccumulateSpray(uvScale0, uvScale1, uvScale2, foamMult0, foamMult1, foamMult2,isActive0,isActive1,isActive2);

            GL.MemoryBarrier(
                MemoryBarrierFlags.ShaderStorageBarrierBit |
                MemoryBarrierFlags.CommandBarrierBit
            );

            //Update and delete spray
            UpdateAndDeleteSpray();

            //Prepare indirect render buffer
            CreateIndirectRenderCommand();

            //Swap new->old
            SwapBuffers();
        }

        public void Render() {

            GL.UseProgram(Program);

            BindDrawShaderVariables();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha,
                         BlendingFactor.OneMinusSrcAlpha);

            GL.DepthMask(false);

            GL.BindVertexArray(vaoId);
            
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);

            GL.DrawElementsIndirect(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.BindVertexArray(0);

            GL.UseProgram(0);

            GL.DepthMask(true);
        }

        void SwapBuffers() {
            GL.BindBuffer(BufferTarget.CopyReadBuffer, newParticlesBuffer);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, oldParticlesBuffer);

            GL.CopyBufferSubData(
                BufferTarget.CopyReadBuffer,
                BufferTarget.CopyWriteBuffer,
                IntPtr.Zero, // read offset
                IntPtr.Zero, // write offset
                64 * 64 * 64 * Marshal.SizeOf<Particle>()
            );

            GL.BindBuffer(BufferTarget.CopyReadBuffer, newParticleCountBuffer);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, oldParticleCountBuffer);

            GL.CopyBufferSubData(
                BufferTarget.CopyReadBuffer,
                BufferTarget.CopyWriteBuffer,
                IntPtr.Zero,
                IntPtr.Zero,
                sizeof(uint)
            );
        }

        private void UpdateAndDeleteSpray()
        {
            GL.UseProgram(sprayUpdateAndDeleteShader);

            Vector3 dominantWind = ComputeDominantWindDirection();
            GL.Uniform3(dominantWindLocation, dominantWind);

            GL.Uniform1(elapsedTimeLocation, ElapsedTime.elapsedTime);

            GL.Uniform1(windScalingLocation, windScaling);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);

            GL.DispatchCompute(64, 64, 64);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit | MemoryBarrierFlags.AtomicCounterBarrierBit);
        }

        private void BindDrawShaderVariables()
        {
            Matrix4 modelViewProjection = Camera.Transformation * Camera.PerspectiveProjection;
            //modelViewProjection = Matrix4.Identity * Camera.Transformation * Camera.PerspectiveProjection;
            GL.UniformMatrix4(modelviewViewMatrixLocation, false, ref modelViewProjection);


            Matrix4 viewMatrix = Camera.GetViewMatrix();
            GL.UniformMatrix4(viewMatrixLocation, false, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref Camera.perspectiveProjection);
            GL.Uniform3(cameraPositionVSLocation, Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            GL.Uniform1(particleLifetimeVSLocation, particleLifetime);
            GL.Uniform1(mistTransparancyLocation, mistTransparancy);
            GL.Uniform1(colorCorrectionLocation, colorCorrection);
            GL.Uniform1(sprayTransparancyLocation, sprayTransparancy);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, splashTexture0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, splashTexture1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, splashTexture2);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, splashTexture3);

            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, mistTexture);
        }


        private void AccumulateSpray(float uvScale0, float uvScale1, float uvScale2, float foamMult0, float foamMult1, float foamMult2,bool isActive0,bool isActive1,bool isActive2)
        {
            int zero = 0;

            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 3, newParticleCountBuffer);
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, newParticleCountBuffer);
            GL.BufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), ref zero);

            GL.UseProgram(sprayAccumulationShader);

            Matrix4 modelviewProjection = Camera.Transformation * Camera.PerspectiveProjection;

            GL.Uniform3(cameraPositionLocation, Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            GL.UniformMatrix4(viewProjectionMatrixLocation, false, ref modelviewProjection);
            GL.Uniform1(frameCounterLocation, ElapsedTime.frameIndex);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, offsetFoamTexture0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, offsetFoamTexture1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, offsetFoamTexture2);

            GL.Uniform1(uvScale0Location, uvScale0);
            GL.Uniform1(uvScale1Location, uvScale1);
            GL.Uniform1(uvScale2Location, uvScale2);

            GL.Uniform1(foamMult0Location, foamMult0);
            GL.Uniform1(foamMult1Location, foamMult1);
            GL.Uniform1(foamMult2Location, foamMult2);

            GL.Uniform1(isActive0Location, isActive0 ? 1 : 0);
            GL.Uniform1(isActive1Location, isActive1 ? 1 : 0);
            GL.Uniform1(isActive2Location, isActive2 ? 1 : 0);

            GL.Uniform1(yThreadAmountLocation, yThreadAmount);
            GL.Uniform1(sprayGenerationRadiusLocation, sprayGenerationRadius);
            GL.Uniform1(foamThresholdLocation, foamThreshold);
            GL.Uniform1(particleLifetimeLocation, particleLifetime);
            GL.Uniform1(verticalSpeedDampeningLocation, verticalSpeedDampening);
            GL.Uniform1(particleSpeedLocation, particleSpeed);

            GL.Uniform1(particleSizeMinLocation, particleSizeMin);
            GL.Uniform1(particleSizeMaxLocation, particleSizeMax);

            GL.Uniform1(mistPercentageLocation, mistPercentage);
            GL.Uniform1(mistSizeLocation, mistSize);
            GL.Uniform1(verticalSpeedDampeningMistLocation, verticalSpeedDampeningMist);

            GL.DispatchCompute(yThreadAmount, yThreadAmount, yThreadAmount);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit | MemoryBarrierFlags.AtomicCounterBarrierBit);
        }


        private void CreateIndirectRenderCommand()
        {
            GL.UseProgram(renderParticlesIndirectShader);
            GL.DispatchCompute(1, 1, 1);
        }


        private void BindVertexArray()
        {
            GL.BindVertexArray(vaoId);

            //Bind positions to vertex-attrib 0
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);

            //Bind uv-buffer to vertex-attrib 1
            GL.BindBuffer(BufferTarget.ArrayBuffer, uvBuffer);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
            GL.EnableVertexAttribArray(1);

            //Bind vertex indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);

            GL.BindVertexArray(0);
        }

        Vector3 ComputeDominantWindDirection()
        {
            Vector2 wind0 = new(
                MathF.Cos(init0.angleRadian),
                MathF.Sin(init0.angleRadian));

            Vector2 wind1 = new(
                MathF.Cos(init1.angleRadian),
                MathF.Sin(init1.angleRadian));

            Vector2 wind2 = new(
                MathF.Cos(init2.angleRadian),
                MathF.Sin(init2.angleRadian));

            float weight0 = init0.windSpeed * init0.scale;
            float weight1 = init1.windSpeed * init1.scale;
            float weight2 = init2.windSpeed * init2.scale;

            Vector2 wind =
                wind0 * weight0 +
                wind1 * weight1 +
                wind2 * weight2;

            wind = Vector2.Normalize(wind);

            return new Vector3(wind.X,0, wind.Y);
        }
    }
}
