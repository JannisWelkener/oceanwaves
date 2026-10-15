using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace cgimin.engine.object3d
{
    public class BaseQuadObject3D
    {
        public Matrix4 Transformation = Matrix4.Identity;

        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector2> UVs = new List<Vector2>();
        public List<int> Indices = new List<int>();

        public int Vao;

        public void CreateVAO()
        {
            List<float> allData = new List<float>();

            for (int i = 0; i < Positions.Count; i++)
            {
                // Position
                allData.Add(Positions[i].X);
                allData.Add(Positions[i].Y);
                allData.Add(Positions[i].Z);

                // UV
                allData.Add(UVs[i].X);
                allData.Add(UVs[i].Y);
            }

            int vbo;
            GL.GenBuffers(1, out vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                allData.Count * sizeof(float),
                allData.ToArray(),
                BufferUsageHint.StaticDraw);

            int ebo;
            GL.GenBuffers(1, out ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                Indices.Count * sizeof(int),
                Indices.ToArray(),
                BufferUsageHint.StaticDraw);

            GL.GenVertexArrays(1, out Vao);
            GL.BindVertexArray(Vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            int stride = Vector3.SizeInBytes + Vector2.SizeInBytes;

            // Position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            // UV
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, Vector3.SizeInBytes);

            GL.BindVertexArray(0);
        }

        public void AddQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            Vector3 n1, Vector3 n2, Vector3 n3, Vector3 n4,
            Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            int index = Positions.Count;

            Positions.Add(v1);
            Positions.Add(v2);
            Positions.Add(v3);
            Positions.Add(v4);

            UVs.Add(uv1);
            UVs.Add(uv2);
            UVs.Add(uv3);
            UVs.Add(uv4);

            Indices.Add(index);
            Indices.Add(index + 1);
            Indices.Add(index + 2);
            Indices.Add(index + 3);
        }

        public void UnLoad()
        {
            // TODO: delete buffers
        }
    }
}