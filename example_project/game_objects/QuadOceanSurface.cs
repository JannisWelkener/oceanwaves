using OpenTK.Mathematics;

namespace cgimin.engine.object3d
{
    public class QuadOceanSurface : BaseQuadObject3D
    {
        public float Size;
        public int GridSize;

        public QuadOceanSurface(float size, int gridSize)
        {
            Size = size;
            GridSize = gridSize;

            Generate();
        }

        private void Generate()
        {
            Positions.Clear();
            UVs.Clear();
            Indices.Clear();

            float halfSize = Size * 0.5f;
            float step = Size / (GridSize - 1);

            // Generate vertices
            for (int z = 0; z < GridSize; z++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    float posX = -halfSize + x * step;
                    float posZ = -halfSize + z * step;

                    Positions.Add(new Vector3(posX, 0f, posZ));

                    // UVs from 0 -> 1 across grid
                    UVs.Add(new Vector2(
                        (float)x / (GridSize - 1),
                        (float)z / (GridSize - 1)
                    ));
                }
            }

            // Generate quad patches (4 indices per quad)
            for (int z = 0; z < GridSize - 1; z++)
            {
                for (int x = 0; x < GridSize - 1; x++)
                {
                    int i0 = z * GridSize + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + GridSize + 1;
                    int i3 = i0 + GridSize;

                    // Quad order (CCW)
                    Indices.Add(i0);
                    Indices.Add(i1);
                    Indices.Add(i2);
                    Indices.Add(i3);
                }
            }
            CreateVAO();
        }

        public void Update(Vector3 cameraPosition)
        {
            // Distance between vertices
            float step = Size / (GridSize - 1);

            // Snap camera position to grid
            float snappedX = MathF.Floor(cameraPosition.X / step) * step;
            float snappedZ = MathF.Floor(cameraPosition.Z / step) * step;

            // Move the chunk so its center follows the snapped camera position
            this.Transformation = Matrix4.CreateTranslation(snappedX, 0f, snappedZ);
        }
    }
}