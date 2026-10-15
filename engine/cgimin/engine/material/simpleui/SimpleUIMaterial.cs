using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using cgimin.engine.object2d;

namespace cgimin.engine.material.simpleui
{
    public class SimpleUIMaterial : BaseMaterial
    {
        private int projectionMatrixLocation;
        private int transformationMatrixLocation;
        private int multiplierLocation;
        private int offsetXLocation;
        private int offsetYLocation;

        public Matrix4 Projection;
        

        public SimpleUIMaterial(int screenWidth, int screenHeight)
        {
            // create shader program
            CreateShaderProgram(
                "cgimin/engine/material/simpleui/UI_VS.glsl",
                "cgimin/engine/material/simpleui/UI_FS.glsl");

            // attribute bindings
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_uv");

            // link shader
            GL.LinkProgram(Program);

            // uniform locations
            projectionMatrixLocation =
                GL.GetUniformLocation(Program, "projection_matrix");

            transformationMatrixLocation =
                GL.GetUniformLocation(Program, "transformation_matrix");

            multiplierLocation = GL.GetUniformLocation(Program, "multiplier");

            offsetXLocation = GL.GetUniformLocation(Program, "offsetX");
            offsetYLocation = GL.GetUniformLocation(Program, "offsetY");

            // orthographic projection for screen-space rendering
            Projection = Matrix4.CreateOrthographicOffCenter(
                0,
                screenWidth,
                screenHeight,
                0,
                -1,
                1);
        }

        public void Draw(Object2D object2d, int textureID, float multiplier, float offsetX, float offsetY)
        {
            // disable depth test so UI is always in front
            GL.Disable(EnableCap.DepthTest);

            // enable alpha blending for png transparency
            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha);

            // bind texture
            GL.ActiveTexture(TextureUnit.Texture0);

            GL.BindTexture(TextureTarget.Texture2D, textureID);
            //Disable filtering
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            

            // bind vao
            GL.BindVertexArray(object2d.Vao);

            // use shader
            GL.UseProgram(Program);

            // projection matrix
            GL.UniformMatrix4(
                projectionMatrixLocation,
                false,
                ref Projection);

            // object transform
            GL.UniformMatrix4(
                transformationMatrixLocation,
                false,
                ref object2d.Transformation);

            GL.Uniform1(multiplierLocation, multiplier);

            GL.Uniform1(offsetXLocation, offsetX);
            GL.Uniform1(offsetYLocation, offsetY);

            // draw
            GL.DrawElements(
                PrimitiveType.Triangles,
                object2d.Indices.Count,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero);

            //Enable Filtering again
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear
            );
            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );

            // cleanup
            GL.BindVertexArray(0);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}