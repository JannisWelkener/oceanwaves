using cgimin.engine.object3d;
using OpenTK.Mathematics;

namespace cgi.game_objects.Buoyancy
{
    public abstract class CubeBuoy : BaseObject3D
    {
        protected CubeBuoy(Vector2 startPos, float size, int voxelsPerAxis) {
            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector2>();
            Indices = new List<int>();
            startPosition = startPos;
            Position = new Vector3(startPos.X, Position.Y, startPos.Y);
            VoxelsPerAxis = voxelsPerAxis;
            cubeSize = size;
            AddCube(cubeSize);

            CreateVAO();

            GenerateVoxels();

            Transformation = Matrix4.CreateTranslation(Position);

            SetVoxelPositionAccordingToTransform();
        }

        public Vector3 Position = new Vector3(0, 3, 0); // start above water
        protected Vector3 Velocity = Vector3.Zero;
        protected Vector3 AngularVelocity = Vector3.Zero;
        protected Quaternion Orientation = Quaternion.Identity;

        public float cubeSize = 5f; // cube edge length
        protected Vector2 startPosition;

        protected float Mass = 200f;

        protected Vector3 horizontalVelocity = new Vector3(0);

        public List<Vector3> voxelLocalPositions = new();
        public List<Vector4> voxelGlobalPositions = new();

        public int VoxelsPerAxis;

        public void FixedUpdate<T>(float fixedRate, List<T> buoys = null) where T : CubeBuoy
        {
            SimulateBuoyancy(fixedRate);

            if(buoys != null) 
                DoCollision(buoys);

            SetVoxelPositionAccordingToTransform();
        }

        public abstract void SimulateBuoyancy(float fixedRate);

        public void DoCollision<T>(List<T> buoys) where T : CubeBuoy
        {
            float selfRadius = cubeSize * MathF.Sqrt(3f) * 0.5f;

            foreach (var buoy in buoys)
            {
                if (buoy == this)
                    continue;

                float otherRadius = buoy.cubeSize * MathF.Sqrt(3f) * 0.5f;

                Vector3 delta = Position - buoy.Position;
                float distance = delta.Length;

                float minDistance = selfRadius + otherRadius;

                if (distance < minDistance)
                {
                    // Prevent division by zero if they're in exactly the same position
                    Vector3 normal;
                    if (distance > 0.0001f)
                    {
                        normal = delta / distance;
                    }
                    else
                    {
                        normal = Vector3.UnitY;
                        distance = 0f;
                    }

                    float penetration = minDistance - distance;

                    // Push each cube half the penetration distance
                    Vector3 correction = normal * (penetration * 0.5f);

                    Position += correction;
                    buoy.Position -= correction;
                }
            }

            Transformation =
                Matrix4.CreateFromQuaternion(Orientation) *
                Matrix4.CreateTranslation(Position);

        }

        private void SetVoxelPositionAccordingToTransform()
        {
            voxelGlobalPositions.Clear();
            foreach (var localVoxel in voxelLocalPositions)
            {
                Vector3 worldVoxel = Vector3.TransformPosition(localVoxel, Transformation);
                voxelGlobalPositions.Add(new Vector4(worldVoxel, 1));
            }
        }

        protected static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private void AddCube(float size)
        {
            float h = size * 0.5f;

            Vector3[] verts = new Vector3[]
            {
                // Front face
                new Vector3(-h,-h,-h), new Vector3( h,-h,-h), new Vector3( h, h,-h), new Vector3(-h, h,-h),
                // Back face
                new Vector3(-h,-h, h), new Vector3( h,-h, h), new Vector3( h, h, h), new Vector3(-h, h, h)
            };

            Vector3[] faceNormals = new Vector3[]
            {
                new Vector3( 0, 0,-1), // front
                new Vector3( 0, 0, 1), // back
                new Vector3(-1, 0, 0), // left
                new Vector3( 1, 0, 0), // right
                new Vector3( 0, 1, 0), // top
                new Vector3( 0,-1, 0)  // bottom
            };

            Vector2[] uvs = new Vector2[]
            {
        new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
            };

            int[][] faceIndices = new int[][]
            {
                new int[]{0,1,2,3}, // front
                new int[]{5,4,7,6}, // back
                new int[]{4,0,3,7}, // left
                new int[]{1,5,6,2}, // right
                new int[]{3,2,6,7}, // top
                new int[]{4,5,1,0}  // bottom
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

        private void GenerateVoxels()
        {
            voxelLocalPositions.Clear();

            float half = cubeSize * 0.5f;
            float step = cubeSize / VoxelsPerAxis;

            for (int x = 0; x < VoxelsPerAxis; x++)
            {
                for (int y = 0; y < VoxelsPerAxis; y++)
                {
                    for (int z = 0; z < VoxelsPerAxis; z++)
                    {
                        Vector3 localPos = new Vector3(
                            -half + step * (x + 0.5f),
                            -half + step * (y + 0.5f),
                            -half + step * (z + 0.5f)
                        );

                        voxelLocalPositions.Add(localPos);
                    }
                }
            }
        }
    }
}
