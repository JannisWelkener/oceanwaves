using OpenTK.Graphics.OpenGL;
using cgimin.engine.object3d;
using cgimin.engine.camera;
using OpenTK.Mathematics;
using cgimin.engine.texture;

namespace cgimin.engine.material.skyboxmaterial
{
    public class SkyboxMaterial : BaseMaterial
    {

        private int modelviewProjectionMatrixLocation;
        private int whiteOutLocation;

        public int skyboxTexture;
        private string selectedSkybox;

        public static bool whiteOut = false;
        public SkyboxMaterial()
        {
            // Shader program is generated from extenal text-files 
            CreateShaderProgram("cgimin/engine/material/skybox/Skybox_VS.glsl",
                                "cgimin/engine/material/skybox/Skybox_FS.glsl");

            // GL.BindAttribLocation, assignes an index to the "in" parameter on our shader
            // the following has to be done...
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_normal");
            GL.BindAttribLocation(Program, 2, "in_uv");
            
            // ...before our program is "linked".
            GL.LinkProgram(Program);

            // gets the location of the "uniform" input-parameter "modelview_projection_matrix"
            modelviewProjectionMatrixLocation = GL.GetUniformLocation(Program, "modelview_projection_matrix");
            whiteOutLocation = GL.GetUniformLocation(Program, "whiteOut");

            //string[] faces =
            //{
            //    "data/textures/skybox/east.bmp",
            //    "data/textures/skybox/west.bmp",
            //    "data/textures/skybox/up.bmp",
            //    "data/textures/skybox/down.bmp",
            //    "data/textures/skybox/north.bmp",
            //    "data/textures/skybox/south.bmp",
            //};
            LoadSkybox("Cubemap_Sky_06-512x512");
        }

        public void LoadSkybox(string _selectedSkybox)
        {
            if(selectedSkybox == _selectedSkybox)return;

            if (skyboxTexture != 0)
            {
                GL.DeleteTexture(skyboxTexture);
            }

            selectedSkybox = _selectedSkybox;

            string path = "data/textures/skybox/" + selectedSkybox + "/";
            string[] faces =
            {
                path+"right.png",
                path+"left.png",
                path+"top.png",
                path+"bottom.png",
                path+"front.png",
                path+"back.png",
            };
            Console.WriteLine("New Skybox: "+ selectedSkybox);
            skyboxTexture = TextureManager.LoadCubemapTexture(faces);
        }

        public void Draw(BaseObject3D object3d)
        {
            GL.BindVertexArray(object3d.Vao);

            GL.UseProgram(Program);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);

            Matrix4 modelviewProjection = object3d.Transformation * Camera.Transformation * Camera.PerspectiveProjection;

            GL.UniformMatrix4(modelviewProjectionMatrixLocation, false, ref modelviewProjection);
            GL.Uniform1(whiteOutLocation, whiteOut ? 1 : 0);

            GL.DrawElements(PrimitiveType.Triangles, object3d.Indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.ActiveTexture(TextureUnit.Texture0);
        }
    }
}
