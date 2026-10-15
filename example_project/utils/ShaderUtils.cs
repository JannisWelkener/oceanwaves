using cgi.settings;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL4;

namespace cgi.utils
{
    public class ShaderUtils
    {

        public static int GenerateDepthTexture()
        {
            int depthTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.DepthComponent24,
                (int)GlobalSettings.screenSize.X,
                (int)GlobalSettings.screenSize.Y,
                0,
                PixelFormat.DepthComponent,
                PixelType.Float,
                IntPtr.Zero
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return depthTexture;
        }

        public static int GenerateScreenTexture()
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba16f,
                (int)GlobalSettings.screenSize.X,
                (int)GlobalSettings.screenSize.Y,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return texture;
        }

        public static int CompileComputeShader(string path)
        {
            string source = File.ReadAllText(path);
            int shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string log = GL.GetShaderInfoLog(shader);
                throw new Exception($"Compute shader compile error: {log}");
            }

            int program = GL.CreateProgram();
            GL.AttachShader(program, shader);
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                string log = GL.GetProgramInfoLog(program);
                throw new Exception($"Shader program link error: {log}");
            }

            GL.DetachShader(program, shader);
            GL.DeleteShader(shader);

            return program;
        }

        public static int LoadVertexAndFragmentShader(string pathvs, string pathfs)
        {
            // Load and compile vertex and fragment shaders
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            string VertexShaderSource = File.ReadAllText(pathvs);
            GL.ShaderSource(vertexShader, VertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompileStatus(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            string FragmentShaderSource = File.ReadAllText(pathfs);
            GL.ShaderSource(fragmentShader, FragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompileStatus(fragmentShader);

            // Create shader program and link shaders
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            CheckProgramLinkStatus(program);
            return program;
        }

        private static void CheckProgramLinkStatus(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"Error linking program: {infoLog}");
            }
        }

        private static void CheckShaderCompileStatus(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"Error compiling shader: {infoLog}");
            }
        }

    }
}
