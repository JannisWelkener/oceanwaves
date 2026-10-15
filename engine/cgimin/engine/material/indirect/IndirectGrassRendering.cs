using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using cgimin.engine.camera;
using cgimin.engine.texture;
using System.Drawing;
using System.Drawing.Imaging;
using cgi.model;
using engine.cgimin.engine.material.ambientdiffuse;
using static engine.cgimin.engine.material.indirect.IndirectVariables;
using engine.cgimin.engine.object3d;
using cgimin.engine.material.simpletexture;

namespace engine.cgimin.engine.material.indirect
{

    [StructLayout(LayoutKind.Sequential)]
    struct DrawElementsIndirectCommand
    {
        public uint count;
        public uint instanceCount;
        public uint firstIndex;
        public uint baseVertex;
        public uint baseInstance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexAndUV
    {
        public Vector4 Position; // 4 floats (x, y, z, w)
        public Vector2 UV;       // 2 floats (u, v)

        public VertexAndUV(Vector4 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }
    }

    public class IndirectGrassRendering
    {
        public Vector2 position;

        public static FakeCamera fakeCam = new FakeCamera();
        public static bool disableGPUCulling = false;

        private static int computeShader;
        private static int vertexShader;
        private static int fragmentShader;
        private static int program;

        private static int vaoId;
        
        static int noiseMapTextureID;
        static int normalMapTextureID;
        private int chunkTrackTextureID;

        private static PlainObject3D plain = new PlainObject3D(5);
        private static SimpleTextureMaterial material = new SimpleTextureMaterial();
        private static int dirtTexture = TextureManager.LoadTexture("data/textures/grass.jpg");

        Bitmap bitmap;

        public IndirectGrassRendering()
        {
            position = new Vector2(0, 0);
        }
        
        public IndirectGrassRendering(int x, int y)
        {
            position = new Vector2(x, y);
        }

        public static void Load()
        {
            computeShader = LoadComputeShader("cgimin/engine/material/indirect/shaders/ID_CS.glsl");
            LoadVertexAndFragmentShader("cgimin/engine/material/indirect/shaders/ID_VS.glsl", "cgimin/engine/material/indirect/shaders/ID_FS.glsl");

            BufferVariables.vertexBuffer = GL.GenBuffer();
            BufferVariables.indexBuffer = GL.GenBuffer();
            BufferVariables.indirectBuffer = GL.GenBuffer();
            BufferVariables.visibleCountBuffer = GL.GenBuffer();
            BufferVariables.workGroupBuffer = GL.GenBuffer();
            BufferVariables.mappingBuffer = GL.GenBuffer();

            BufferVariables.uvBuffer = GL.GenBuffer();

            int vertexCount = 15;
            int indexCount = 39;

            //int a = UniformVariables.abc;
            //int b = BufferVariables.xyz;

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferVariables.indexBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, indexCount * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, BufferVariables.indexBuffer);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferVariables.indirectBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, Marshal.SizeOf<DrawElementsIndirectCommand>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, BufferVariables.indirectBuffer);

            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, BufferVariables.visibleCountBuffer);
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 3, BufferVariables.visibleCountBuffer);

            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, BufferVariables.workGroupBuffer);
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 4, BufferVariables.workGroupBuffer);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferVariables.mappingBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * 64 * 64, IntPtr.Zero, BufferUsageHint.DynamicCopy);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, BufferVariables.mappingBuffer);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferVariables.vertexBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, vertexCount * Vector4.SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, BufferVariables.vertexBuffer); // Binding index 0 for vertices

            // Bind and allocate buffer memory for UVs
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferVariables.uvBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, vertexCount * Vector2.SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, BufferVariables.uvBuffer); // Binding index 1 for UVs

            vaoId = GL.GenVertexArray();
            BindVertexArray();

            LoadUniforms();

            noiseMapTextureID = TextureManager.LoadTexture("data/textures/perlin_noise.jpg");

            normalMapTextureID = TextureManager.LoadTexture("data/textures/grass_blade.png");



            /*
            bitmap = new Bitmap(100, 100);
            chunkTrackTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, chunkTrackTextureID);

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

            bitmap.UnlockBits(bmpData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);



            UniformLocations.texture0Location = GL.GetUniformLocation(program, "sampler");
            UniformLocations.texture1Location = GL.GetUniformLocation(program, "normal_map");
            UniformLocations.texture2Location = GL.GetUniformLocation(program, "track");
            */
        }

        public void LoadInstance() {
            bitmap = new Bitmap(100, 100);
            chunkTrackTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, chunkTrackTextureID);

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

            bitmap.UnlockBits(bmpData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);



            UniformLocations.texture0Location = GL.GetUniformLocation(program, "sampler");
            UniformLocations.texture1Location = GL.GetUniformLocation(program, "normal_map");
            UniformLocations.texture2Location = GL.GetUniformLocation(program, "track");
        }

        private static void BindVertexArray()
        {
            GL.BindVertexArray(vaoId);

            //Bind positions to vertex-attrib 0
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVariables.vertexBuffer);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);

            //Bind uv-buffer to vertex-attrib 1
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVariables.uvBuffer);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
            GL.EnableVertexAttribArray(1);

            //Bind vertex indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, BufferVariables.indexBuffer);

            GL.BindVertexArray(0);
        }

        private static void LoadUniforms()
        {
            UniformLocations.modelviewProjectionMatrixLocation = GL.GetUniformLocation(program, "modelview_projection_matrix");
            UniformLocations.cameraPositionLocation = GL.GetUniformLocation(computeShader, "cameraPosition");
            UniformLocations.cameraDirectionLocation = GL.GetUniformLocation(computeShader, "cameraDirection");
            UniformLocations.modelviewViewMatrixLocation = GL.GetUniformLocation(computeShader, "view_projection_matrix");
            UniformLocations.elapsedTimeLocation = GL.GetUniformLocation(program, "elapsed_time");

            UniformLocations.chunkPositionLocation = GL.GetUniformLocation(program, "chunkLocation");
            UniformLocations.cameraPosition2Location = GL.GetUniformLocation(program, "cameraWorldPosition");

            UniformLocations.chunkPosLocation = GL.GetUniformLocation(computeShader, "chunkPos");

            UniformLocations.GPUCullingBoolLocation = GL.GetUniformLocation(computeShader, "disableGPUCulling");
        }

        private void BindDrawShaderVariables(Vector2 chunkPosition)
        {
            Matrix4 modelViewProjection = Matrix4.CreateTranslation(chunkPosition.X,/*-5*/-20, chunkPosition.Y) * Matrix4.CreateScale(0.02f) * Camera.Transformation * Camera.PerspectiveProjection;
            //modelViewProjection = Matrix4.Identity * Camera.Transformation * Camera.PerspectiveProjection;
            GL.UniformMatrix4(UniformLocations.modelviewProjectionMatrixLocation, false, ref modelViewProjection);
            GL.Uniform2(UniformLocations.chunkPositionLocation, chunkPosition);

            //int texture1Location = GL.GetUniformLocation(program, "sampler");
            GL.Uniform1(UniformLocations.texture0Location, 0);  // Texture unit 0

            //int texture2Location = GL.GetUniformLocation(program, "normal_map");
            GL.Uniform1(UniformLocations.texture1Location, 1);  // Texture unit 1

            //int texture3Location = GL.GetUniformLocation(program, "track");
            GL.Uniform1(UniformLocations.texture2Location, 2);  // Texture unit 2

            GL.Uniform1(UniformLocations.elapsedTimeLocation, ElapsedTime.elapsedTime);

            GL.Uniform3(UniformLocations.cameraPosition2Location, fakeCam.Position);

            /*
             * 
             * Cam.Transformation * Camera.PerspectiveProjection
             * Chunk Position
             * Scale
             *
             */

            GL.ActiveTexture(TextureUnit.Texture0); //use texture-space 0 for first texture
            GL.BindTexture(TextureTarget.Texture2D, noiseMapTextureID);

            GL.ActiveTexture(TextureUnit.Texture1); //use texture-space 0 for first texture
            GL.BindTexture(TextureTarget.Texture2D, normalMapTextureID);

            GL.ActiveTexture(TextureUnit.Texture2); //use texture-space 2 for next texture
            GL.BindTexture(TextureTarget.Texture2D, chunkTrackTextureID);

            GL.ActiveTexture(TextureUnit.Texture0);
        }
        
        public void Draw()
        {
            ComputeShader(position);

            GL.Disable(EnableCap.CullFace);

            GL.UseProgram(program);

            BindDrawShaderVariables(position);

            GL.BindVertexArray(vaoId);

            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, BufferVariables.indirectBuffer);

            GL.DrawElementsIndirect(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.BindVertexArray(0);

            GL.Enable(EnableCap.CullFace);

            plain.Transformation = Matrix4.CreateTranslation(position.X / 50,-0.5f, position.Y / 50);
            material.Draw(plain, dirtTexture);

        }

        private static int LoadComputeShader(string computeShaderPath)
        {
            string computeShaderSource = File.ReadAllText(computeShaderPath);

            // Create the compute shader object
            int computeShader = GL.CreateShader(ShaderType.ComputeShader);

            // Attach the shader source code to the shader object
            GL.ShaderSource(computeShader, computeShaderSource);

            // Compile the shader
            GL.CompileShader(computeShader);

            // Check for compile errors
            GL.GetShader(computeShader, ShaderParameter.CompileStatus, out int compileStatus);
            if (compileStatus != (int)All.True)
            {
                // Get the compile error message
                string infoLog = GL.GetShaderInfoLog(computeShader);
                Console.WriteLine($"Error compiling compute shader: {infoLog}");
                throw new Exception($"Compute shader compilation failed: {infoLog}");
            }

            // Create the shader program and attach the compiled compute shader to it
            int program = GL.CreateProgram();
            GL.AttachShader(program, computeShader);

            // Link the program
            GL.LinkProgram(program);

            // Check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus != (int)All.True)
            {
                // Get the link error message
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"Error linking compute shader program: {infoLog}");
                throw new Exception($"Compute shader program linking failed: {infoLog}");
            }

            // Clean up the shader object
            GL.DeleteShader(computeShader);
            return program;
        }

        private static void LoadVertexAndFragmentShader(string pathvs, string pathfs)
        {
            // Load and compile vertex and fragment shaders
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            string VertexShaderSource = File.ReadAllText(pathvs);
            GL.ShaderSource(vertexShader, VertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompileStatus(vertexShader);

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            string FragmentShaderSource = File.ReadAllText(pathfs);
            GL.ShaderSource(fragmentShader, FragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompileStatus(fragmentShader);

            // Create shader program and link shaders
            program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            CheckProgramLinkStatus(program);
        }

        private static void ComputeShader(Vector2 chunkPos)
        {
            int zero = 0;

            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 3, BufferVariables.visibleCountBuffer);   // Bind atomic counter buffer
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, BufferVariables.visibleCountBuffer);
            GL.BufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), ref zero);

            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 4, BufferVariables.workGroupBuffer);   // Bind atomic counter buffer
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, BufferVariables.workGroupBuffer);
            GL.BufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), ref zero);


            GL.UseProgram(computeShader);

            Matrix4 viewMatrix = fakeCam.Transformation;

            Matrix4 modelViewProjection = viewMatrix * fakeCam.PerspectiveProjection;

            GL.Uniform3(UniformLocations.cameraPositionLocation, fakeCam.Position.X, fakeCam.Position.Y, fakeCam.Position.Z);
            GL.Uniform3(UniformLocations.cameraDirectionLocation, fakeCam.Rotation);
            GL.UniformMatrix4(UniformLocations.modelviewViewMatrixLocation, false, ref modelViewProjection);

            GL.Uniform2(UniformLocations.chunkPosLocation, chunkPos);

            GL.Uniform1(UniformLocations.GPUCullingBoolLocation, disableGPUCulling ? 1 : 0);

            GL.DispatchCompute(64, 64, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit | MemoryBarrierFlags.AtomicCounterBarrierBit);
        }

        public static void OnUnload()
        {
            GL.DeleteBuffers(1, ref BufferVariables.vertexBuffer);
            GL.DeleteBuffers(1, ref BufferVariables.indirectBuffer);

            GL.DeleteProgram(computeShader);
            GL.DeleteProgram(vertexShader);
            GL.DeleteProgram(fragmentShader);
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

        private static void CheckProgramLinkStatus(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"Error linking program: {infoLog}");
            }
        }



        public void Update(float deltaTime, Vector3 TankPosition, Vector3 direction, Vector3 cameraPosition, int chunkLevel)
        {
            Vector3 chunkPosition = new Vector3(this.position.X / 50, 0, this.position.Y / 50);
            //Console.WriteLine(this.position.X /50+ " , "+this.position.Y/50);

            Vector3 chunkCenter = chunkPosition + new Vector3(2.5f, 0, 2.5f);
            float distance = (chunkCenter - TankPosition).LengthFast;

            updateWorldPosition(chunkCenter, cameraPosition, chunkLevel);

            /*
             * Only update close chunks; Reduce writing to GPU
             */
            if (distance < 5)
            {
                //brush = new SolidBrush(Color.FromArgb((int)rgb.X, (int)rgb.Y, (int)rgb.Z));

                direction = Vector3.Normalize(direction);
                //Console.WriteLine(direction);
                //Console.WriteLine((int)(((1 + direction.X) / 2.0f) * 255) + " " +  (int)(direction.Y * 255) + " " +  (int)(((1 + direction.Z) / 2.0f) * 255));

                Brush brush = new SolidBrush(Color.FromArgb((int)(((1 + direction.X) / 2.0f) * 255), (int)(direction.Y * 255), (int)(((1 + direction.Z) / 2.0f) * 255)));

                Vector3 relativePos = TankPosition - chunkPosition;

                float projX = Vector3.Dot(relativePos, new Vector3(0, 0, 1));  // Project onto plane's U axis
                float projY = Vector3.Dot(relativePos, new Vector3(1, 0, 0));

                float UVx = projX * 2;
                float UVy = projY * 2;


                int positionX = (int)(UVx * 10) - 14;
                int positionY = (int)(UVy * 10) - 14;

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    //g.TranslateTransform(UVx * 10, UVy * 10);
                    //g.RotateTransform(45);


                    g.FillRectangle(brush, positionX, positionY, 28, 28);
                    g.ResetTransform();
                }

                updateTextureToGPU();
            }
            //}
        }

        private void resetTrackTexture() {
            Brush brush = new SolidBrush(Color.FromArgb(0,0,0));

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(brush, 0, 0, bitmap.Width, bitmap.Height);
                g.ResetTransform();
            }

            updateTextureToGPU();
        }
        private void updateWorldPosition(Vector3 chunkCenter, Vector3 cameraPosition,int chunkLevel)
        {
            float moveBy = 15f;
            float moveAfterDist = 7.5f;

            moveBy = 5f * (1 + 2f * chunkLevel);
            moveAfterDist = moveBy / 2f;

            Vector3 chunkPosition = new Vector3(this.position.X / 50, 0,this.position.Y / 50);

            float xDistance = Math.Abs(chunkCenter.Z - cameraPosition.Z);
            if (xDistance > moveAfterDist)
            {
                Vector3 newChunkPosition0 = new Vector3(chunkPosition.X, chunkPosition.Y, chunkPosition.Z - moveBy);
                Vector3 newChunkPosition1 = new Vector3(chunkPosition.X, chunkPosition.Y, chunkPosition.Z + moveBy);


                float distance0 = Math.Abs(newChunkPosition0.Z + 2.5f - cameraPosition.Z);
                float distance1 = Math.Abs(newChunkPosition1.Z + 2.5f - cameraPosition.Z);
                if (distance0 < distance1)
                {
                    chunkPosition = newChunkPosition0;
                }
                else
                {
                    chunkPosition = newChunkPosition1;
                }
                resetTrackTexture();
            }

            float yDistance = Math.Abs(chunkCenter.X - cameraPosition.X);

            if (yDistance > moveAfterDist)
            {
                Vector3 newChunkPosition0 = new Vector3(chunkPosition.X - moveBy, chunkPosition.Y, chunkPosition.Z);
                Vector3 newChunkPosition1 = new Vector3(chunkPosition.X + moveBy, chunkPosition.Y, chunkPosition.Z);

                float distance0 = Math.Abs(newChunkPosition0.X + 2.5f - cameraPosition.X);
                float distance1 = Math.Abs(newChunkPosition1.X + 2.5f - cameraPosition.X);

                if (distance0 < distance1)
                {
                    chunkPosition = newChunkPosition0;
                }
                else
                {
                    chunkPosition = newChunkPosition1;
                }

                resetTrackTexture();
            }

            this.position = new Vector2(chunkPosition.X * 50, chunkPosition.Z * 50);

        }

        private void updateTextureToGPU()
        {
            GL.BindTexture(TextureTarget.Texture2D, chunkTrackTextureID);

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            bitmap.UnlockBits(bmpData);
        }


    }
}
