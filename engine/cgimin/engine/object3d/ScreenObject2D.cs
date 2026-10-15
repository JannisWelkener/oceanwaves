using OpenTK.Mathematics;

namespace cgimin.engine.object2d
{
    public class ScreenObject2D : Object2D
    {
        public Vector2 ScreenSize;

        public ScreenObject2D(Vector2 screenSize)
            : base(0) // uiSize is not used for screen quads
        {
            ScreenSize = screenSize;

            CreateScreenQuad();
            CreateVAO();
        }

        public void CreateScreenQuad()
        {
            Positions.Clear();
            UVs.Clear();
            Indices.Clear();

            float w = ScreenSize.X;
            float h = ScreenSize.Y;

            // bottom-left
            // bottom-left
            Positions.Add(new Vector2(0, 0));
            UVs.Add(new Vector2(0, 1));

            // bottom-right
            Positions.Add(new Vector2(w, 0));
            UVs.Add(new Vector2(1, 1));

            // top-right
            Positions.Add(new Vector2(w, h));
            UVs.Add(new Vector2(1, 0));

            // top-left
            Positions.Add(new Vector2(0, h));
            UVs.Add(new Vector2(0, 0));

            // two triangles
            Indices.Add(0);
            Indices.Add(1);
            Indices.Add(2);

            Indices.Add(2);
            Indices.Add(3);
            Indices.Add(0);
        }

        public void UpdateScreenSize(Vector2 newSize)
        {
            ScreenSize = newSize;

            CreateScreenQuad();

            // rebuild GPU buffers
            UnLoad();
            CreateVAO();
        }
    }
}