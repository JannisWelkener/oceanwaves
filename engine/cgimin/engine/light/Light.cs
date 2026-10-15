using OpenTK.Mathematics;

namespace cgimin.engine.light
{
    public class Light
    {

        public static Vector3 lightDirection;
        public static Vector4 lightAmbient;
        public static Vector4 lightDiffuse;
        public static Vector4 lightSpecular;

        public static void SetDirectionalLight(Vector3 direction, Vector4 ambient, Vector4 diffuse, Vector4 specular)
        {
            lightDirection = Vector3.Normalize(direction);
            lightAmbient = ambient;
            lightDiffuse = diffuse;
            lightSpecular = specular;
        }

    }
}
