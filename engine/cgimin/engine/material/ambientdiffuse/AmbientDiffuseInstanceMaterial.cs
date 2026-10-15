using cgimin.engine.camera;
using cgimin.engine.object3d;
using cgimin.engine.light;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using engine.cgimin.engine.material.ambientdiffuse;

namespace cgimin.engine.material.ambientdiffuse
{
    public class AmbientDiffuseInstanceMaterial : BaseMaterial
    {
        private int modelMatrixLocation;
        private int modelviewProjectionMatrixLocation;

        private int lightDirectionLocation;
        private int lightAmbientLocation;
        private int lightDiffuseLocation;
        private int elapsedTimeLocation;
        private int chunkLocation;
        private int cameraLocation;

        private int culledInstanceBuffer; // SSBO for culled instance IDs
        private int visibleCountBuffer; // Atomic counter buffer
        private int computeShaderProgram;
        int indirectBuffer;

        public AmbientDiffuseInstanceMaterial()
        {
            // shader-programm is loaded
            CreateShaderProgram("cgimin/engine/material/ambientdiffuse/Grass_VS.glsl",
                                "cgimin/engine/material/ambientdiffuse/Grass_FS.glsl",
                                "cgimin/engine/material/ambientdiffuse/Grass_GS.glsl");

            GL.Uniform1(GL.GetUniformLocation(Program, "sampler"), 0);
            GL.Uniform1(GL.GetUniformLocation(Program, "normal_map"), 1);
            GL.Uniform1(GL.GetUniformLocation(Program, "track"), 2);

            // GL.BindAttribLocation, defines which index of the data-structure is assigned to which "in" parameter 
            GL.BindAttribLocation(Program, 0, "in_position");
            GL.BindAttribLocation(Program, 1, "in_normal");
            GL.BindAttribLocation(Program, 2, "in_uv");

            // ...has to be done before the final "link" of the shader-program
            GL.LinkProgram(Program);

            // the location of the "uniform"-paramter "modelview_projection_matrix" on the shader is saved to modelviewProjectionMatrixLocation
            modelviewProjectionMatrixLocation = GL.GetUniformLocation(Program, "modelview_projection_matrix");

            // the location of the "uniform"-paramter for the model matrix on the shader is saved to modelviewMatrixLocation
            modelMatrixLocation = GL.GetUniformLocation(Program, "model_matrix");

            /// the location of the "uniform"-paramters of the light parameters
            lightDirectionLocation = GL.GetUniformLocation(Program, "light_direction");
            lightAmbientLocation = GL.GetUniformLocation(Program, "light_ambient_color");
            lightDiffuseLocation = GL.GetUniformLocation(Program, "light_diffuse_color");

            elapsedTimeLocation = GL.GetUniformLocation(Program, "elapsed_time");

            chunkLocation = GL.GetUniformLocation(Program, "chunkLocation");
            cameraLocation = GL.GetUniformLocation(Program, "cameraPosition");

            GL.GenBuffers(1, out indirectBuffer);
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);
            /*
            // Indirect command format: count (how many instances), instance count, first index, base vertex, base instance
            DrawElementsIndirectCommand drawCommand = new DrawElementsIndirectCommand
            {
                Count = 36,          // Assume 36 indices for drawing one instance (e.g., a cube)
                InstanceCount = 0,   // This will be modified by the compute shader
                FirstIndex = 0,
                BaseVertex = 0,
                BaseInstance = 0
            };
            */

            //GL.BufferData(BufferTarget.DrawIndirectBuffer, 66, ref drawCommand, BufferUsageHint.DynamicDraw);

            SetupComputeShader(64 * 64);
        }

        public void Draw(BaseObject3D object3d, int textureID, int textureID2, int textureID3, Vector2 chunkPosition)
        {
            DispatchComputeShader(64*64, new Vector3(chunkPosition.X, 0.0f, chunkPosition.Y), Camera.Transformation * Camera.PerspectiveProjection);
            int culledInstanceCount = 0;
            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, visibleCountBuffer);
            //GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(int), ref culledInstanceCount);
            //THIS IS THE PROBLEM!!! TODO

            culledInstanceCount = 64 * 64;



            GL.Disable(EnableCap.CullFace);
            // set the texture
            GL.ActiveTexture(TextureUnit.Texture0); //use texture-space 0 for first texture
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.ActiveTexture(TextureUnit.Texture1); //use texture-space 1 for next texture
            GL.BindTexture(TextureTarget.Texture2D, textureID2);

            GL.ActiveTexture(TextureUnit.Texture2); //use texture-space 2 for next texture
            GL.BindTexture(TextureTarget.Texture2D, textureID3);

            // using the Vertex-Array-Object of out object
            GL.BindVertexArray(object3d.Vao);

            // using our shader
            GL.UseProgram(Program);

            int texture1Location = GL.GetUniformLocation(Program, "sampler");
            GL.Uniform1(texture1Location, 1);  // Texture unit 0

            // Tell the shader that texture2 corresponds to texture unit 1
            int texture2Location = GL.GetUniformLocation(Program, "normal_map");
            GL.Uniform1(texture2Location, 0);  // Texture unit 1

            int texture3Location = GL.GetUniformLocation(Program, "track");
            GL.Uniform1(texture3Location, 2);  // Texture unit 1


            // The matrix which we give as "modelview_projection_matrix" is assembled:
            // object-transformation * camera-transformation * perspective projection of the camera
            // on the shader each vertex-position is multiplied by this matrix. The result is the final position on the screen
            Matrix4 modelViewProjection = object3d.Transformation * Camera.Transformation * Camera.PerspectiveProjection;

            // modelViewProjection is passed to the shader
            GL.UniformMatrix4(modelviewProjectionMatrixLocation, false, ref modelViewProjection);

            // The model matrix (just the transformation of the object) is also given to the shader. We want to multiply our normals with this matrix, to have them in world space.
            Matrix4 model = object3d.Transformation;
            GL.UniformMatrix4(modelMatrixLocation, false, ref model);

            // Die Licht Parameter werden übergeben
            GL.Uniform3(lightDirectionLocation, Light.lightDirection);
            GL.Uniform4(lightAmbientLocation, Light.lightAmbient);
            GL.Uniform4(lightDiffuseLocation, Light.lightDiffuse);

            GL.Uniform1(elapsedTimeLocation, ElapsedTime.elapsedTime);
            GL.Uniform2(chunkLocation, chunkPosition.X, chunkPosition.Y);
            GL.Uniform2(cameraLocation, Camera.position.X, Camera.position.Z);

            //Console.WriteLine(culledInstanceCount);
            //INSTANCING
            GL.DrawElementsInstanced(PrimitiveType.Triangles, object3d.Indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, culledInstanceCount);
            //GL.DrawElements(PrimitiveType.Triangles, object3d.Indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);


            //GL.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);
            //GL.MultiDrawElementsIndirect(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, IntPtr.Zero, 1, 0);



            GL.Enable(EnableCap.CullFace);
        }

        private void SetupComputeShader(int numInstances)
        {
            // Create the buffer for culled instance IDs
            ////culledInstanceBuffer = GL.GenBuffer();
            ////GL.BindBuffer(BufferTarget.ShaderStorageBuffer, culledInstanceBuffer);
            ////GL.BufferData(BufferTarget.ShaderStorageBuffer, numInstances * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            ////GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, culledInstanceBuffer); // Bind to binding point 0

            // Create the buffer for the atomic counter
            visibleCountBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, visibleCountBuffer);
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 0, visibleCountBuffer); // Bind to binding point 0

            // Load and compile the compute shader (implement LoadComputeShader yourself)
            computeShaderProgram = LoadComputeShader("cgimin/engine/material/ambientdiffuse/Grass_CS.glsl");
        }

        private void DispatchComputeShader(int numInstances, Vector3 startPosition, Matrix4 viewProjectionMatrix)
        {
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);
            uint zero = 0;
            GL.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, sizeof(uint), ref zero);


            // Bind the necessary buffers
            ////GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, culledInstanceBuffer); // Bind SSBO for culled instance IDs
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 0, visibleCountBuffer);   // Bind atomic counter buffer

            // Reset the atomic counter
            //uint zero = 0;
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, visibleCountBuffer);
            GL.BufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), ref zero);

            // Use the compute shader
            GL.UseProgram(computeShaderProgram);

            // Set uniforms for number of instances, starting position, and view-projection matrix
            GL.Uniform1(GL.GetUniformLocation(computeShaderProgram, "numInstances"), numInstances);
            GL.Uniform3(GL.GetUniformLocation(computeShaderProgram, "startPosition"), ref startPosition);
            GL.UniformMatrix4(GL.GetUniformLocation(computeShaderProgram, "viewProjectionMatrix"), false, ref viewProjectionMatrix);

            // Dispatch the compute shader work groups
            int workGroupSize = (numInstances + 63) / 64; // Divide into work groups of 64 threads
            //workGroupSize = 1; ??
            GL.DispatchCompute(workGroupSize, 1, 1);

            // Wait for compute shader to finish before rendering
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit | MemoryBarrierFlags.AtomicCounterBarrierBit); //not affecting performance
        }

        private int LoadComputeShader(string computeShaderPath)
        {
            // Load the compute shader source code from file
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

            // Clean up the shader object (it's no longer needed after linking)
            GL.DeleteShader(computeShader);

            // Return the program ID
            return program;
        }
    }
}
