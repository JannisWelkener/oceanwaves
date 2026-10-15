using cgimin.engine.object3d;
using OpenTK.Mathematics;

namespace cgi.game_objects
{
    public class OceanChunk : BaseObject3D
    {
        public OceanChunk(float size, int gridSize)
        {
            //int gridSize = 64 * 4;
            //float size = 64 * 4;

            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector2>();
            Indices = new List<int>();

            // --- Generate vertices ---
            for (int z = 0; z <= gridSize; z++)
            {
                for (int x = 0; x <= gridSize; x++)
                {
                    float fx = (float)x / gridSize;
                    float fz = (float)z / gridSize;

                    Positions.Add(new Vector3(
                        fx * size,
                        0.0f,
                        fz * size
                    ));

                    Normals.Add(new Vector3(0, 1, 0));
                    UVs.Add(new Vector2(fx, fz));
                }
            }

            // --- Generate indices ---
            int vertsPerRow = gridSize + 1;

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    int topLeft = z * vertsPerRow + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * vertsPerRow + x;
                    int bottomRight = bottomLeft + 1;

                    // Triangle 1
                    Indices.Add(topLeft);
                    Indices.Add(bottomLeft);
                    Indices.Add(topRight);

                    // Triangle 2
                    Indices.Add(topRight);
                    Indices.Add(bottomLeft);
                    Indices.Add(bottomRight);
                }
            }

            CreateVAO();

            //this.Transformation = Matrix4.CreateScale(scale);
        }
    }
}
