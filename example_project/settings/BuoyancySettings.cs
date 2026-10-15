using OpenTK.Mathematics;

namespace cgi.settings
{
    public class BuoyancySettings
    {
        public static float flowStrength = 0.02f;
        public static float damping = 0.98f;

        public static Vector3 gravity = new Vector3(0,-9.81f,0);
        public static float mass = 80f;
        public static float buoyancyStrenght = 10f;
        public static bool doCollision = true;
    }
}
