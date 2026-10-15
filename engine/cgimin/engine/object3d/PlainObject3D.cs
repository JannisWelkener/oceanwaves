using cgimin.engine.object3d;
using OpenTK.Mathematics;

namespace engine.cgimin.engine.object3d
{
    public class PlainObject3D : BaseObject3D
    {
        private int textureID;
        public PlainObject3D(float scale)
        {

            Positions = new List<Vector3>();
            UVs = new List<Vector2>();
            Normals = new List<Vector3>();
            Indices = new List<int>();

           // addTriangle(new Vector3(-scale, 0, scale), new Vector3(scale, 0, scale), new Vector3(-scale, 0, -scale), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
           //             new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1));


           // addTriangle(new Vector3(scale, 0, scale), new Vector3(scale, 0, -scale), new Vector3(-scale, 0, -scale), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
           //             new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));



            addTriangle(new Vector3(0, 0, 0), new Vector3(0, 0, scale), new Vector3(scale, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
                        new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1));


            addTriangle(new Vector3(0, 0, scale), new Vector3(scale, 0, scale), new Vector3(scale, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
                        new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));

            CreateVAO();

            //this.textureID = TextureManager.LoadTexture("data/textures/grid.png");
            //this.Transformation *= Matrix4.CreateScale(scale);

        }

        public int Texture
        {
            get { return textureID; }
        }
    }
}
