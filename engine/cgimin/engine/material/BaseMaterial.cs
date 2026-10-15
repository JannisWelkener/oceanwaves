using OpenTK.Graphics.OpenGL;

namespace cgimin.engine.material
{
    public class BaseMaterial
    {

        private int VertexObject;
        private int FragmentObject;
        private int GeometryObject;

        public int Program;

        public void CreateShaderProgram(string pathVS, string pathFS)
        {

            // shader files are read (text)
            string vs = File.ReadAllText(pathVS);
            string fs = File.ReadAllText(pathFS);

            int status_code;
            string info;

            // vertex and fragment shaders are created
            VertexObject = GL.CreateShader(ShaderType.VertexShader);
            FragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // compiling vertex-shader 
            GL.ShaderSource(VertexObject, vs);
            GL.CompileShader(VertexObject);
            GL.GetShaderInfoLog(VertexObject, out info);
            GL.GetShader(VertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // compiling fragment shader
            GL.ShaderSource(FragmentObject, fs);
            GL.CompileShader(FragmentObject);
            GL.GetShaderInfoLog(FragmentObject, out info);
            GL.GetShader(FragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // final shader program is created using rhw fragment and the vertex program
            //GL.DeleteProgram(shaderProgram);

            Program = GL.CreateProgram();
            GL.AttachShader(Program, FragmentObject);
            GL.AttachShader(Program, VertexObject);

            // hint: "Program" is not linked yet
        }

        public void CreateShaderProgram(string pathVS, string pathFS,string pathGS)
        {

            // shader files are read (text)
            string vs = File.ReadAllText(pathVS);
            string fs = File.ReadAllText(pathFS);
            string gs = File.ReadAllText(pathGS);

            int status_code;
            string info;

            // vertex and fragment shaders are created
            VertexObject = GL.CreateShader(ShaderType.VertexShader);
            FragmentObject = GL.CreateShader(ShaderType.FragmentShader);
            GeometryObject = GL.CreateShader(ShaderType.GeometryShader);

            // compiling vertex-shader 
            GL.ShaderSource(VertexObject, vs);
            GL.CompileShader(VertexObject);
            GL.GetShaderInfoLog(VertexObject, out info);
            GL.GetShader(VertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // compiling fragment shader
            GL.ShaderSource(FragmentObject, fs);
            GL.CompileShader(FragmentObject);
            GL.GetShaderInfoLog(FragmentObject, out info);
            GL.GetShader(FragmentObject, ShaderParameter.CompileStatus, out status_code);

            // compiling geometry shader
            GL.ShaderSource(GeometryObject, gs);
            GL.CompileShader(GeometryObject);
            GL.GetShaderInfoLog(GeometryObject, out info);
            GL.GetShader(GeometryObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // final shader program is created using rhw fragment and the vertex program
            //GL.DeleteProgram(shaderProgram);

            Program = GL.CreateProgram();
            GL.AttachShader(Program, FragmentObject);
            GL.AttachShader(Program, VertexObject);
            GL.AttachShader(Program, GeometryObject);

            // hint: "Program" is not linked yet
        }



    }
}
