using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace cgimin.engine.object3d
{

    public class BaseObject3D
    {

        // the transformation (position, rotation, scale) of the object
        public Matrix4 Transformation = Matrix4.Identity;

        // lists, filled with the 3d-data
        public List<Vector3> Positions;
        public List<Vector3> Normals;
        public List<Vector2> UVs;

        // the index-List
        public List<int> Indices;

        // Vartex-Array-Object "VAO"
        public int Vao;
      
        // generates the Vartex-Array-Objekt
        public void CreateVAO()
        {
            // list of the complete vertex data
            List<float> allData = new List<float>();

            // "interleaved" means position, normal and uv in one block for each vertex
            for (int i = 0; i < Positions.Count; i++) {

                allData.Add(Positions[i].X);
                allData.Add(Positions[i].Y);
                allData.Add(Positions[i].Z);

                allData.Add(Normals[i].X);
                allData.Add(Normals[i].Y);
                allData.Add(Normals[i].Z);

                allData.Add(UVs[i].X);
                allData.Add(UVs[i].Y);
            }

            // generate the VBO for the "interleaved" data
            int allBufferVBO;
            GL.GenBuffers(1, out allBufferVBO);

            // Buffer is "binded", following OpenGL commands refer to this buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, allBufferVBO);

            // Data is uploaded to graphics memory
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(allData.Count  * sizeof(float)), allData.ToArray(), BufferUsageHint.StaticDraw);

            // BindBuffer to 0, so the following commands do not overwrite the current vbo (state machine)
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            // generating the index buffer
            int IndexBuffer;
            GL.GenBuffers(1, out IndexBuffer);

            // Buffer is "binded", following OpenGL commands refer to this buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);

            // index data is uploaded to grpahics mamory
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(sizeof(uint) * Indices.Count), Indices.ToArray(), BufferUsageHint.StaticDraw);

            // BindBuffer to 0, so the following commands do not overwrite the current element buffer (state machine)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            

            // generating the Vertex-Array-Objects
            GL.GenVertexArrays(1, out Vao);

            // Vertex-Array-Objekt is "binded", following OpenGL commands refer to this VAO. Inmportant for the folling two calls of "BindBuffer"
            GL.BindVertexArray(Vao);

            // BindBuffer commands: Are saved by our VAO.
            // Element-Buffer (indices)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);

            // ... then our interleaved VBO.
            GL.BindBuffer(BufferTarget.ArrayBuffer, allBufferVBO);

            // three calls of GL.VertexAttribPointer do follow, must be first "enabled"
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            // The description of our "interleaved" data structure, the shader needs to know how tpo handle our data
            // Die assignment to the "Index", the first parameter, will be recognized by the shader
            
            // At Index 0 (so at first) we have our position data. The last parameter defines at which byte-place in the vertex block the data for the position is saved 
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes + Vector3.SizeInBytes + Vector2.SizeInBytes, 0);

            // At Index 1 we have our normal data. We have it after the position, which is a "Vector3" type, so the byte-place is "Vector3.SizeInBytes"
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes + Vector3.SizeInBytes + Vector2.SizeInBytes, Vector3.SizeInBytes);

            // At Index 2 we have our UV data. We have it after the position and the normal, which are both "Vector3" type, so the byte-place is "Vector3.SizeInBytes" * 2
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, true, Vector3.SizeInBytes + Vector3.SizeInBytes + Vector2.SizeInBytes, Vector3.SizeInBytes * 2);

            // BindBuffer to 0, so the following commands do not overwrite the current VAO
            GL.BindVertexArray(0);

            // Note: The generated VAO defines a data-structure, which must be considered by the shader regarding the index-places defined by GL.VertexAttribPointer 
            // The data-format placing 0 = position; 1 = normal and 2 = uv must be used by our materials

        }


        // Adds a triangle
        public void addTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n1, Vector3 n2, Vector3 n3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            int index = Positions.Count;

            Positions.Add(v1);
            Positions.Add(v2);
            Positions.Add(v3);

            Normals.Add(n1);
            Normals.Add(n2);
            Normals.Add(n3);

            UVs.Add(uv1);
            UVs.Add(uv2);
            UVs.Add(uv3);

            Indices.Add(index);
            Indices.Add(index + 2);
            Indices.Add(index + 1);
        }


        // unloads from graphics memory
        public void UnLoad()
        {
            // tbd.

        }






    }
}
