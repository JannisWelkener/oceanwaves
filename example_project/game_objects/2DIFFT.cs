using cgi.utils;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL4;
using System.Numerics;

namespace cgi.game_objects
{
    public class _2DIFFT
    {
        private int count = 0;

        public int precomputedTwiddleTexture;
        private int ifftPermuteShader;
        private int ifftHorizontalShader;
        private int ifftVerticalShader;
        private int twiddlePrecomputeShader;
        private int size;

        private int bufferTexture;

        private int stepLoc;
        private int pingPongLoc;

        private int buffer0hLocation;
        private int buffer0vLocation;
        //private int sizeLoc;
        public _2DIFFT(int _size) {    
            ifftHorizontalShader = ShaderUtils.CompileComputeShader("shaders/IFFT/2DIFFT_Horizontal.glsl");
            ifftVerticalShader = ShaderUtils.CompileComputeShader("shaders/IFFT/2DIFFT_Vertical.glsl");
            ifftPermuteShader = ShaderUtils.CompileComputeShader("shaders/IFFT/2DIFFT_Permute.glsl");
            twiddlePrecomputeShader = ShaderUtils.CompileComputeShader("shaders/IFFT/TwiddlePrecompute.glsl");
            size = _size;
            PrecomputeTwiddleFactorsAndInputIndices(size,twiddlePrecomputeShader);

            bufferTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, bufferTexture);
            GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);
            //Disable filtering
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);

            stepLoc = GL.GetUniformLocation(ifftHorizontalShader, "Step");
            pingPongLoc = GL.GetUniformLocation(ifftHorizontalShader, "PingPong");

            buffer0hLocation = GL.GetUniformLocation(ifftHorizontalShader, "Buffer0");
            buffer0vLocation = GL.GetUniformLocation(ifftVerticalShader, "Buffer0");
        }

        //Twiddle-Texture probably fine!
        public void PrecomputeTwiddleFactorsAndInputIndices(int size, int twiddleProgram)
        {
            // Compute log2(size)    
            int logSize = BitOperations.Log2((uint)size);

            // --- Create texture ---
            precomputedTwiddleTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, precomputedTwiddleTexture);
            GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, logSize, size);

            //Disable filtering
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);

            // --- Bind compute shader ---
            GL.UseProgram(twiddleProgram);


            // Set uniforms
            int sizeLoc = GL.GetUniformLocation(twiddleProgram, "Size");
            GL.Uniform1(sizeLoc, (uint)size);

            GL.BindImageTexture(0, precomputedTwiddleTexture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

            GL.DispatchCompute(logSize, size / 2 / 8, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        
        public void IFFT2D(int inputTexture)
        {

            int query;
            GL.GenQueries(1, out query);
            GL.BeginQuery(QueryTarget.TimeElapsed, query);

            int local_size = 16;

            int logSize = BitOperations.Log2((uint)size);
            bool pingPong = false;

            GL.UseProgram(ifftHorizontalShader);

            // --- Horizontal Step IFFT ---
            GL.BindTextureUnit(0, precomputedTwiddleTexture);

            for (uint i = 0; i < logSize; i++)
            {
                pingPong = !pingPong;
                GL.Uniform1(stepLoc, i);

                if (pingPong) {
                    GL.BindTextureUnit(1, inputTexture);
                    GL.Uniform1(buffer0hLocation, 1);
                    GL.BindImageTexture(2, bufferTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
                } else {
                    GL.BindTextureUnit(1, bufferTexture);
                    GL.BindImageTexture(2, inputTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
                }

                GL.DispatchCompute(size / local_size, size / local_size, 1);
                GL.MemoryBarrier(
                    MemoryBarrierFlags.ShaderImageAccessBarrierBit
                );
            }
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.UseProgram(ifftVerticalShader); 

            // --- Vertical Step IFFT ---
            GL.BindTextureUnit(0, precomputedTwiddleTexture);

            for (uint i = 0; i < logSize; i++)
            {
                pingPong = !pingPong;
                GL.Uniform1(stepLoc, i);

                if (pingPong)
                {
                    GL.BindTextureUnit(1, inputTexture);
                    GL.Uniform1(buffer0vLocation, 1);
                    GL.BindImageTexture(2, bufferTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
                }
                else
                {
                    GL.BindTextureUnit(1, bufferTexture);
                    GL.BindImageTexture(2, inputTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
                }

                GL.DispatchCompute(size / local_size, size / local_size, 1);
                GL.MemoryBarrier(
                    MemoryBarrierFlags.ShaderImageAccessBarrierBit
                );
            }
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            // --- Permute ---
            GL.UseProgram(ifftPermuteShader);
            GL.BindImageTexture(1, inputTexture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.DispatchCompute(size / local_size, size / local_size, 1);
            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.UseProgram(0);

            GL.EndQuery(QueryTarget.TimeElapsed);

            //Only show the first 10 ffts
            if (count < 10)
            {
                count++;
                long elapsedNs;
                GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out elapsedNs);

                double ms = elapsedNs / 1_000_000.0;
                Console.WriteLine($"IFFT GPU time: {ms} ms");
            }
            
        }
    }
}
