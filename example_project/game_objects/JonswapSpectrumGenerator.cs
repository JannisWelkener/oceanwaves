using cgi.game_objects;
using cgi.utils;
using engine.cgimin.engine.material.ambientdiffuse;
using Examples.Tutorial;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
struct SpectrumParameters
{
    public float scale;
    public float angle;
    public float spreadBlend;
    public float swell;
    public float alpha;
    public float peakOmega;
    public float gamma;
    public float shortWavesFade;
    public float LengthScale;
    public float GravityAcceleration;
    public float Depth;
    public float CutoffHigh;
    public float CutoffLow;
}

public class JonswapSpectrumGenerator
{
    private WaveShaders shaders;

    public int H0Tex;
    private int wavesDataTex;

    private int size;

    private int spectrumsBuffer;

    public int DxDz_DyDxzTex;
    public int DyxDyz_DxxDzzTex;

    public int flowTex;

    public int DxDz_DyDxzCOPYTex;

    public int offsetTex;
    public int normalAndFoamTex;

    private _2DIFFT _ifft;
    private JONSWAP_SpectrumParameters _parameters;

    private float prevTime = 0;

    public JonswapSpectrumGenerator(JONSWAP_SpectrumParameters parameters, _2DIFFT ifft, int fftSize)
    {
        size = fftSize;
        _ifft = ifft;
        _parameters = parameters;
    }

    private void CreateTextures()
    {
        H0Tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, H0Tex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        wavesDataTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, wavesDataTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        //--------------------------------------------------------

        DxDz_DyDxzTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DxDz_DyDxzTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        DxDz_DyDxzCOPYTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DxDz_DyDxzCOPYTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        DyxDyz_DxxDzzTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DyxDyz_DxxDzzTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);





        flowTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, flowTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);



        //--------------------------------------------------------

        offsetTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, offsetTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        normalAndFoamTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, normalAndFoamTex);
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, size, size);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void InitTexturesAndShaders()
    {
        Console.WriteLine("Initializing Spectrum..");
        // -------------------------------
        // 1. Create textures
        // -------------------------------
        CreateTextures();

        // -------------------------------
        // 2. Compile compute shaders
        // -------------------------------
        shaders = WaveShaders.Instance;
        
    }

    private void setUniforms()
    {
        GL.UseProgram(shaders.initializeSpectrumShader);

        // -------------------------------
        // 3. Bind textures to image units
        // -------------------------------
        GL.BindImageTexture(0, H0Tex, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(1, wavesDataTex, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

        // -------------------------------
        // 4. Set uniforms
        // -------------------------------
        GL.Uniform1(GL.GetUniformLocation(shaders.initializeSpectrumShader, "Size"), size);

        InitSpectrumBuffer();
    }

    private void InitSpectrumBuffer()
    {
        SpectrumParameters data = new SpectrumParameters
        {
            scale = _parameters.scale,
            angle = _parameters.angleRadian,
            spreadBlend = _parameters.spreadBlend,
            swell = _parameters.swell,
            alpha = _parameters.alpha,
            peakOmega = _parameters.peakOmega,
            gamma = _parameters.gamma,
            shortWavesFade = _parameters.shortWavesFade,

              LengthScale = _parameters.lengthScale,
              GravityAcceleration = _parameters.gravity,
              Depth = _parameters.depth,
              CutoffHigh = _parameters.cutoffHigh,
              CutoffLow = _parameters.cutoffLow
        };

        // Generate buffer
        spectrumsBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, spectrumsBuffer);

        // Upload single struct
        GL.BufferData(
            BufferTarget.ShaderStorageBuffer,
            Marshal.SizeOf<SpectrumParameters>(),
            ref data,
            BufferUsageHint.DynamicDraw
        );

        // Bind to binding point 0
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, spectrumsBuffer);

        // Unbind
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }


    public void GenerateInitialSpectrum()
    {
        this.setUniforms();
        
        int localSize = 8;
        int groupCount = size / localSize;

        GL.UseProgram(shaders.initializeSpectrumShader);

        // -------------------------------
        // Dispatch initial spectrum
        // -------------------------------
        GL.Uniform1(GL.GetUniformLocation(shaders.initializeSpectrumShader, "Initial"), 1);
        GL.DispatchCompute(groupCount, groupCount, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

        /*
         H0K confirmed correct!!! :)
         */

        // -------------------------------
        // Dispatch conjugated spectrum
        // -------------------------------
        GL.Uniform1(GL.GetUniformLocation(shaders.initializeSpectrumShader, "Initial"), 0);

        GL.DispatchCompute(groupCount, groupCount, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

        /*
         WavesData confirmed correct!!! :)
         */
    }

    public void Update(float deltaTime)
    {
        //Console.WriteLine("Running Compute Stage 3..");
        //float time = deltaTime;
        // Bind compute shader
        GL.UseProgram(shaders.timeEvolutionShader);

        int timeLocation = GL.GetUniformLocation(shaders.timeEvolutionShader, "Time");
        GL.Uniform1(timeLocation, ElapsedTime.elapsedTime * _parameters.playbackSpeed);

        /*
         
         TODO

         Make GL.GetUniformLocation persistent!!
         
         
         
         */


        // Bind output images (must match shader bindings 0–3)
        GL.BindImageTexture(0, DxDz_DyDxzTex, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(1, DyxDyz_DxxDzzTex, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

        GL.BindImageTexture(2, flowTex, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

        // Bind input textures to texture units 4 and 5 (matching shader bindings)
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.Texture2D, H0Tex);
        GL.Uniform1(GL.GetUniformLocation(shaders.timeEvolutionShader, "H0"), 3);

        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2D, wavesDataTex);
        GL.Uniform1(GL.GetUniformLocation(shaders.timeEvolutionShader, "WavesData"), 4);

        // Dispatch compute
        int groupsX = (size + 7) / 8;
        int groupsY = (size + 7) / 8;

        /*
         Time evolution
         */
        GL.DispatchCompute(groupsX, groupsY, 1);

        // Ensure writes are finished before using textures
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

        GL.UseProgram(0);


        GL.CopyImageSubData(
        DxDz_DyDxzTex,
        ImageTarget.Texture2D,
        0,
        0, 0, 0,

        DxDz_DyDxzCOPYTex,
        ImageTarget.Texture2D,
        0,
        0, 0, 0,

        size,
        size,
        1);

        /*
         Everthing until here is correct!!
         */

        //Texture packing!
        _ifft.IFFT2D(DxDz_DyDxzTex);
        _ifft.IFFT2D(DyxDyz_DxxDzzTex);
        _ifft.IFFT2D(flowTex);


        /*
         
        DxDzTex
        DyDxzTex
        DyxDyzTex
        DxxDzzTex

        to

        offset x,y,z
        normal x,y,z
        foam
         
        */
        GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

        GL.UseProgram(shaders.textureJoinShader);

        GL.BindImageTexture(0, DxDz_DyDxzTex, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(1, DyxDyz_DxxDzzTex, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(2, offsetTex, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(3, normalAndFoamTex, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

        float dTime = ElapsedTime.elapsedTime * _parameters.playbackSpeed - prevTime;
        if (ExampleProject.isSceneStatic) dTime = 0;
        GL.Uniform1(GL.GetUniformLocation(shaders.textureJoinShader, "deltaTime"), dTime);
        GL.Uniform1(GL.GetUniformLocation(shaders.textureJoinShader, "lambda"), _parameters.lambda);
        GL.Uniform1(GL.GetUniformLocation(shaders.textureJoinShader, "foamBias"), _parameters.foamBias);

        GL.Uniform1(GL.GetUniformLocation(shaders.textureJoinShader, "foamDecayRate"), _parameters.foamDecayRate);
        GL.Uniform1(GL.GetUniformLocation(shaders.textureJoinShader, "foamAdd"), _parameters.foamAdd);

        GL.DispatchCompute(groupsX, groupsY, 1);

        GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);





        GL.UseProgram(0);

        GL.UseProgram(shaders.textureBlurShader);

        GL.BindImageTexture(0, offsetTex, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
        GL.BindImageTexture(1, normalAndFoamTex, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);

        GL.DispatchCompute(groupsX, groupsY, 1);

        GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

        GL.UseProgram(0);

        prevTime = ElapsedTime.elapsedTime * _parameters.playbackSpeed;
    }

}