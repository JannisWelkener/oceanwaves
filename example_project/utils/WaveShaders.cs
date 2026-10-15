using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cgi.utils
{
    public sealed class WaveShaders
    {
        private static WaveShaders instance = null;

        public int initializeSpectrumShader;
        public int timeEvolutionShader;
        public int textureJoinShader;
        public int textureBlurShader;

        public int buoyancySampleShader;

        private WaveShaders()
        {
            initializeSpectrumShader = ShaderUtils.CompileComputeShader("shaders/InitializeSpectrum.glsl");
            timeEvolutionShader = ShaderUtils.CompileComputeShader("shaders/TimeEvolution.glsl");
            textureJoinShader = ShaderUtils.CompileComputeShader("shaders/TextureJoin.glsl");
            textureBlurShader = ShaderUtils.CompileComputeShader("shaders/BlurShader.glsl");
            buoyancySampleShader = ShaderUtils.CompileComputeShader("shaders/buoyancy/BuoyancySampleShader.glsl");
        }

        public static WaveShaders Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WaveShaders();
                }
                return instance;
            }
        }
    }
}
