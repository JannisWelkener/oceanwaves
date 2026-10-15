
using cgi.settings;
using cgi.utils;
using OpenTK.Graphics.OpenGL;

namespace cgi.game_objects
{
    public class PostProcessing
    {
        int brightPassShader;
        int horizontalBlurShader;
        int verticalBlurShader;
        int bloomCombineShader;

        int brightPassTexture;
        int blurTempTexture;
        int sceneTexture;

        public void Load(int scene)
        {
            sceneTexture = scene;

            brightPassTexture = ShaderUtils.GenerateScreenTexture();
            blurTempTexture = ShaderUtils.GenerateScreenTexture();

            brightPassShader = ShaderUtils.CompileComputeShader("shaders/postProcessing/BrightPass_CS.glsl");
            horizontalBlurShader = ShaderUtils.CompileComputeShader("shaders/postProcessing/HorizontalGaussianBlur_CS.glsl");
            verticalBlurShader = ShaderUtils.CompileComputeShader("shaders/postProcessing/VerticalGaussianBlur_CS.glsl");
            bloomCombineShader = ShaderUtils.CompileComputeShader("shaders/postProcessing/BloomCombine_CS.glsl");
        }

        public int RunPostProcessing()
        {
            
            int screenWidth = (int)GlobalSettings.screenSize.X;
            int screenHeight = (int)GlobalSettings.screenSize.Y;
            int groupsX = (screenWidth + 15) / 16;
            int groupsY = (screenHeight + 15) / 16;

            BrightPass(groupsX, groupsY);

            for (int i = 0; i < 4; i++)
                GaussianBlur(groupsX, groupsY);

            BloomCombine(groupsX, groupsY);

            return blurTempTexture;
        }

        private void BloomCombine(int groupsX, int groupsY)
        {
            GL.UseProgram(bloomCombineShader);

            GL.Uniform1(
                GL.GetUniformLocation(bloomCombineShader, "bloomStrength"),
                PBRSettings.hdrStrenght
            );

            // Scene texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, sceneTexture);

            GL.Uniform1(
                GL.GetUniformLocation(bloomCombineShader, "colorTexture"),
                0
            );

            // Blurred bloom texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, brightPassTexture);

            GL.Uniform1(
                GL.GetUniformLocation(bloomCombineShader, "bloomTexture"),
                1
            );

            // Output
            GL.BindImageTexture(
                2,
                blurTempTexture,
                0,
                false,
                0,
                TextureAccess.WriteOnly,
                SizedInternalFormat.Rgba16f
            );

            GL.DispatchCompute(groupsX, groupsY, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }

        private void GaussianBlur(int groupsX, int groupsY)
        {
            GL.UseProgram(horizontalBlurShader);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, brightPassTexture);

            GL.BindImageTexture(
                1,
                blurTempTexture,
                0,
                false,
                0,
                TextureAccess.WriteOnly,
                SizedInternalFormat.Rgba16f);

            GL.DispatchCompute(groupsX, groupsY, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.UseProgram(verticalBlurShader);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, blurTempTexture);

            GL.BindImageTexture(
                1,
                brightPassTexture,
                0,
                false,
                0,
                TextureAccess.WriteOnly,
                SizedInternalFormat.Rgba16f);

            GL.DispatchCompute(groupsX, groupsY, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }

        private void BrightPass(int groupsX,int groupsY)
        {
            GL.UseProgram(brightPassShader);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, sceneTexture);

            int colorLoc = GL.GetUniformLocation(brightPassShader, "colorTexture");
            GL.Uniform1(colorLoc, 0);

            GL.Uniform1(
                GL.GetUniformLocation(brightPassShader, "threshold"),
                1.0f
            );

            GL.BindImageTexture(
                1,                          // binding = 1
                brightPassTexture,
                0,
                false,
                0,
                TextureAccess.WriteOnly,
                SizedInternalFormat.Rgba16f
            );

            GL.DispatchCompute(groupsX, groupsY, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }
    }
}
