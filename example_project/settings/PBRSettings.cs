
using cgimin.engine.object3d;
using OpenTK.Mathematics;

namespace cgi.settings
{
    public class PBRSettings
    {
        public static float leadr = 0;
        public static float WavePeakScatterStrength = 1;
        public static float envStrength = 0.5f;
        public static float roughness = 0.075f;

        public static float scatterStrength = 1;
        public static float scatterShadowStrength = 0.5f;
        public static float bubbleDensity = 1;
        public static float foamRoughness = 0;

        public static Vector3 scatterColor = new Vector3(0.032f, 0.3f, 0.32f);
        public static Vector3 bubbleColor = new Vector3(0f, 0.1f, 0.0159f);
        public static Vector3 Lsun = new Vector3(1,1,1);

        public static float hdrStrenght = 0.8f;
        public static float verticalSkyboxOffset = 0;


        //Helper
        public static Color scatterColor_ = Color.FromArgb(25, 243, 255);
        public static Color bubbleColor_ = Color.FromArgb(0, 255, 41);
        public static Color Lsun_ = Color.FromArgb(255, 255, 255);

        public static Vector3 scatterColor_ash = new Vector3(184.11f, 0.9012f, 0.3176f);
        public static Vector3 bubbleColor_ash = new Vector3(129.6f,1.0f, 0.0980f);
        public static Vector3 Lsun_ash = new Vector3(0f, 1.0f, 1f);

        public static string selectedColorName = "ScatterColor";
        public static ObjLoaderObject3D? skybox;
        public static string? PendingSkybox = null;

        public static void Init()
        {
            skybox = new ObjLoaderObject3D("data/objects/skybox2.obj");
        }
    }
}
