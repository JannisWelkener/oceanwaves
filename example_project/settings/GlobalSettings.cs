using cgi.game_objects;
using cgimin.engine.material.skyboxmaterial;
using OpenTK.Mathematics;

namespace cgi.settings
{
    public class GlobalSettings
    {
        public static Vector2 screenSize = new Vector2(2560, 1440); //new Vector2(1920, 1080);
        public static bool toggleGrid = false;
        public static SkyboxMaterial skyboxMaterial;
        public static int uiSize = (int)screenSize.Y / 5;

        public static float tessMinDistance = 10;
        public static float tessMaxDistance = 350;
        public static int minTessLevel = 4;
        public static int maxTessLevel = 35;

        public static bool showSettings = true;

        public static JONSWAP_SpectrumParameters parameters0 = new JONSWAP_SpectrumParameters("parameter0");
        public static JONSWAP_SpectrumParameters parameters1 = new JONSWAP_SpectrumParameters("parameter1");
        public static JONSWAP_SpectrumParameters parameters2 = new JONSWAP_SpectrumParameters("parameter2");

        public static JONSWAP_SpectrumParameters selectedParameter = parameters0;

        public static bool advancedDebug = false;

        public static void Init()
        {
            skyboxMaterial = new SkyboxMaterial();
        }
        public static void nextParameters()
        {
            if (selectedParameter == parameters0)
            {
                selectedParameter = parameters1;
                //selectedSpectrum = init1;
                Console.WriteLine(selectedParameter.name);
                return;
            }

            if (selectedParameter == parameters1)
            {
                selectedParameter = parameters2;
                //selectedSpectrum = init2;
                Console.WriteLine(selectedParameter.name);
                return;
            }

            if (selectedParameter == parameters2)
            {
                selectedParameter = parameters0;
                //selectedSpectrum = init0;
                Console.WriteLine(selectedParameter.name);
                return;
            }
        }
    }
}
