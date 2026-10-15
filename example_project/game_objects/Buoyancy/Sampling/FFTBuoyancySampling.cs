using cgi.utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace cgi.game_objects.Buoyancy.Sampling
{
    [StructLayout(LayoutKind.Sequential)]
    struct BuoyancyResult
    {
        public Vector4 HeightAndVelocity;
    }

    public readonly struct FFTWaveData
    {
        public readonly float waveHeight;
        public readonly Vector3 velocity;
        public FFTWaveData(float p, Vector3 v)
        {
            waveHeight = p;
            velocity = v;
        }
    }

    public class FFTBuoyancySampling
    {
        const int BUFFER_COUNT = 3;

        int[] outputSSBOs = new int[BUFFER_COUNT];
        int inputSSBO = 0;
        IntPtr[] fences = new IntPtr[BUFFER_COUNT];

        int frameIndex = 0;
        int shaderGroups = 16;

        int sampleAmount = 0;

        WaveShaders shaders = WaveShaders.Instance;

        List<CubeBuoyFFT> buoys = new List<CubeBuoyFFT>();

        public FFTBuoyancySampling(){
            //The first 2 dont work, this is a workaround. I have no idea why, but any number after those work perfectly fine...
            //Do not change or the world will burn!!!!
            Register(new CubeBuoyFFT(new Vector2(0, 0), 0f, 1));
            Register(new CubeBuoyFFT(new Vector2(0, 0), 0f, 1));
        }

        public void Register(CubeBuoyFFT fftBuoy)
        {
            int voxelCount = fftBuoy.VoxelsPerAxis * fftBuoy.VoxelsPerAxis * fftBuoy.VoxelsPerAxis;
            sampleAmount += voxelCount;
            buoys.Add(fftBuoy);
        }

        public void Init()
        {
            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                outputSSBOs[i] = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, outputSSBOs[i]);

                GL.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    sampleAmount * Marshal.SizeOf<BuoyancyResult>(),
                    IntPtr.Zero,
                    BufferUsageHint.DynamicCopy);
            }

            inputSSBO = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, inputSSBO);

            GL.BufferData(
                BufferTarget.ShaderStorageBuffer,
                sampleAmount * Vector4.SizeInBytes,
                IntPtr.Zero,
                BufferUsageHint.StreamDraw);

            Console.WriteLine("Samples FFT: "+sampleAmount);
        }
        public void Update(
            JonswapSpectrumGenerator spectrum0,
            JonswapSpectrumGenerator spectrum1,
            JonswapSpectrumGenerator spectrum2,
            JONSWAP_SpectrumParameters p0,
            JONSWAP_SpectrumParameters p1,
            JONSWAP_SpectrumParameters p2
            )
        {
            List<Vector4> samplePoints = new List<Vector4>();
            foreach (var b in buoys)
            {
                samplePoints.AddRange(b.voxelGlobalPositions);
            }
            

            GL.UseProgram(shaders.buoyancySampleShader);

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
            GL.BindTexture(TextureTarget.Texture2D, spectrum0.flowTex);

            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, spectrum1.flowTex);

            GL.ActiveTexture(TextureUnit.Texture8);
            GL.BindTexture(TextureTarget.Texture2D, spectrum2.flowTex);

            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "uvScale0"), p0.uvScale);
            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "uvScale1"), p1.uvScale);
            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "uvScale2"), p2.uvScale);

            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "isActive0"), p0.isActive ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "isActive1"), p1.isActive ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "isActive2"), p2.isActive ? 1 : 0);

            GL.Uniform1(GL.GetUniformLocation(shaders.buoyancySampleShader, "sampleCount"), sampleAmount);


            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, inputSSBO);

            GL.BufferSubData(
                BufferTarget.ShaderStorageBuffer,
                IntPtr.Zero,
                sampleAmount * Vector4.SizeInBytes,
                samplePoints.ToArray());

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, inputSSBO);




            int bufferIndex = frameIndex % BUFFER_COUNT;

            //output
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, outputSSBOs[bufferIndex]);
            int groupCount = (sampleAmount + (shaderGroups - 1)) / shaderGroups;
            GL.DispatchCompute(groupCount, 1, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            fences[bufferIndex] = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);





            int readIndex = (frameIndex + 1) % BUFFER_COUNT;

            if (fences[readIndex] != IntPtr.Zero)
            {
                var result = GL.ClientWaitSync(
                    fences[readIndex],
                    ClientWaitSyncFlags.SyncFlushCommandsBit,
                    0);

                if (result == WaitSyncStatus.AlreadySignaled ||
                    result == WaitSyncStatus.ConditionSatisfied)
                {
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, outputSSBOs[readIndex]);
                    int resultSize = sampleAmount * Marshal.SizeOf<BuoyancyResult>();

                    IntPtr ptr = GL.MapBufferRange(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        resultSize,
                        BufferAccessMask.MapReadBit);

                    unsafe
                    {
                        BuoyancyResult* data = (BuoyancyResult*)ptr;

                        int buoyCounter = 0;

                        foreach (var b in buoys)
                        {
                            int voxelCount = b.VoxelsPerAxis * b.VoxelsPerAxis * b.VoxelsPerAxis;
                            b.sampledData.Clear();
                            for (int j = 0; j < voxelCount; j++)
                            {
                                Vector4 heightAndVelocityBUOY = data[buoyCounter + j].HeightAndVelocity;
                                float waterHeightBUOY = heightAndVelocityBUOY.X;
                                Vector3 velocityBUOY = heightAndVelocityBUOY.Yzw;

                                FFTWaveData waveData = new FFTWaveData(waterHeightBUOY, velocityBUOY);
                                b.sampledData.Add(waveData);
                            }
                            buoyCounter += voxelCount;
                        }
                    }

                    GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

                    GL.DeleteSync(fences[readIndex]);
                    fences[readIndex] = IntPtr.Zero;
                }
            }




            frameIndex++;
        }
    }
}
