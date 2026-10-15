using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace cgimin.engine.object2d
{
    public class Object2D
    {
        public Object2D(float _uiSize) {
            uiSize = _uiSize;

            CreateQuad();
            CreateVAO();
        }
        public float uiSize;
        // transformation for screen-space movement
        public Matrix4 Transformation = Matrix4.Identity;

        // x,y positions
        public List<Vector2> Positions = new List<Vector2>();

        // uv coordinates
        public List<Vector2> UVs = new List<Vector2>();

        // indices
        public List<int> Indices = new List<int>();

        // vertex array object
        public int Vao;

        // creates the vao
        public void CreateVAO()
        {
            List<float> allData = new List<float>();

            // interleaved: x,y,u,v
            for (int i = 0; i < Positions.Count; i++)
            {
                // position
                allData.Add(Positions[i].X);
                allData.Add(Positions[i].Y);

                // uv
                allData.Add(UVs[i].X);
                allData.Add(UVs[i].Y);
            }

            // create vertex buffer
            int vertexBuffer;
            GL.GenBuffers(1, out vertexBuffer);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(allData.Count * sizeof(float)),
                allData.ToArray(),
                BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // create index buffer
            int indexBuffer;
            GL.GenBuffers(1, out indexBuffer);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);

            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(Indices.Count * sizeof(int)),
                Indices.ToArray(),
                BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // create vao
            GL.GenVertexArrays(1, out Vao);

            GL.BindVertexArray(Vao);

            // bind buffers into vao
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

            // enable attributes
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            // position (x,y)
            GL.VertexAttribPointer(
                0,
                2,
                VertexAttribPointerType.Float,
                false,
                Vector2.SizeInBytes + Vector2.SizeInBytes,
                0);

            // uv
            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                Vector2.SizeInBytes + Vector2.SizeInBytes,
                Vector2.SizeInBytes);

            GL.BindVertexArray(0);
        }

        // creates a quad
        public void CreateQuad()
        {
            float size = uiSize;
            Positions.Clear();
            UVs.Clear();
            Indices.Clear();

            // top-left
            Positions.Add(new Vector2(0,0));
            UVs.Add(new Vector2(0, 0));

            // top-right
            Positions.Add(new Vector2(0 + size, 0));
            UVs.Add(new Vector2(1, 0));

            // bottom-right
            Positions.Add(new Vector2(0 + size, 0 + size));
            UVs.Add(new Vector2(1, 1));

            // bottom-left
            Positions.Add(new Vector2(0,0 + size));
            UVs.Add(new Vector2(0, 1));

            // triangle 1
            Indices.Add(0);
            Indices.Add(1);
            Indices.Add(2);

            // triangle 2
            Indices.Add(2);
            Indices.Add(3);
            Indices.Add(0);
        }

        // unload gpu data
        public void UnLoad()
        {
            GL.DeleteVertexArray(Vao);
        }
    }
}