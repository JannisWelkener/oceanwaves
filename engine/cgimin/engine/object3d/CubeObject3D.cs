using OpenTK.Mathematics;

namespace cgimin.engine.object3d
{
    public class CubeObject3D : BaseObject3D
    {

        public CubeObject3D(float size)
        {
            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector2>();
            Indices = new List<int>();

            AddCube(size);

            CreateVAO(); // your method to upload to GPU
        }

        private void AddCube(float size)
        {
            float h = size * 0.5f;

            Vector3[] verts =
            {
        // Front
        new Vector3(-h, -h, -h),
        new Vector3( h, -h, -h),
        new Vector3( h,  h, -h),
        new Vector3(-h,  h, -h),

        // Back
        new Vector3(-h, -h,  h),
        new Vector3( h, -h,  h),
        new Vector3( h,  h,  h),
        new Vector3(-h,  h,  h)
    };

            Vector3[] faceNormals =
            {
        new Vector3(0,0,-1), // front
        new Vector3(0,0,1),  // back
        new Vector3(-1,0,0), // left
        new Vector3(1,0,0),  // right
        new Vector3(0,1,0),  // top
        new Vector3(0,-1,0)  // bottom
    };

            Vector2[] uvs =
            {
        new Vector2(0,0),
        new Vector2(1,0),
        new Vector2(1,1),
        new Vector2(0,1)
    };

            int[][] faceIndices =
            {
        new[] {0,1,2,3}, // front
        new[] {5,4,7,6}, // back
        new[] {4,0,3,7}, // left
        new[] {1,5,6,2}, // right
        new[] {3,2,6,7}, // top
        new[] {4,5,1,0}  // bottom
    };

            for (int f = 0; f < 6; f++)
            {
                int[] fi = faceIndices[f];
                Vector3 normal = faceNormals[f];

                AddVertex(verts[fi[0]], normal, uvs[0]);
                AddVertex(verts[fi[1]], normal, uvs[1]);
                AddVertex(verts[fi[2]], normal, uvs[2]);

                AddVertex(verts[fi[0]], normal, uvs[0]);
                AddVertex(verts[fi[2]], normal, uvs[2]);
                AddVertex(verts[fi[3]], normal, uvs[3]);
            }
        }

        private void AddVertex(Vector3 pos, Vector3 normal, Vector2 uv)
        {
            Positions.Add(pos);
            Normals.Add(normal);
            UVs.Add(uv);
            Indices.Add(Positions.Count - 1);
        }

        public void Update(float dt)
        {

        }


    }
}
