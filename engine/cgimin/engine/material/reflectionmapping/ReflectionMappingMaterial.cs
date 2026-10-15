using OpenTK.Graphics.OpenGL;
using cgimin.engine.object3d;
using cgimin.engine.camera;
using OpenTK.Mathematics;

namespace cgimin.engine.material.simpletexture
{
    public class ReflectionMappingMaterial : BaseMaterial
    {

        private int modelviewProjectionMatrixLocation;
        private int modelviewMatrixLocation;

        public ReflectionMappingMaterial()
        {
            // Shader program is generated from extenal text-files 
            CreateShaderProgram("cgimin/engine/material/reflectionmapping/ReflectionMapping_VS.glsl",
                                "cgimin/engine/material/reflectionmapping/ReflectionMapping_FS.glsl");

            // GL.BindAttribLocation, assignes an index to the "in" parameter on our shader
            // the following has to be done...
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_normal");
            GL.BindAttribLocation(Program, 2, "in_uv");
            
            // ...before our program is "linked".
            GL.LinkProgram(Program);

            // gets the location of the "uniform" input-parameters
            modelviewProjectionMatrixLocation = GL.GetUniformLocation(Program, "modelview_projection_matrix");
            modelviewMatrixLocation = GL.GetUniformLocation(Program, "modelview_matrix");

        }

        public void Draw(BaseObject3D object3d, int textureID)
        {
            // Texture is "binded"
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            // Binds the "Vertex-Array-Object" of our object (see base class)
            GL.BindVertexArray(object3d.Vao);

            // Sets the shader program (see base class)
            GL.UseProgram(Program);

            // The matrix, which we are giving as "modelview_projection_matrix" is created:
            // Object-transformation * Camera-transformation * Perspective-projection of the camera.
            // On the vertex-shader, every vertex is multilpied by this matrix. Result is the correct position on the screen.
            Matrix4 modelviewProjectionMatrix = object3d.Transformation * Camera.Transformation * Camera.PerspectiveProjection;

            Matrix4 modelviewMatrix = object3d.Transformation * Camera.Transformation;

            // Give the complete modelviewProjection-matrix to the shader.
            GL.UniformMatrix4(modelviewProjectionMatrixLocation, false, ref modelviewProjectionMatrix);

            // Give the complete modelview-matrix to the shader.
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);

            // The object is drawn.
            GL.DrawElements(PrimitiveType.Triangles, object3d.Indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

        }



    }
}
