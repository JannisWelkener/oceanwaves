using cgi.settings;
using cgimin.engine.light;
using OpenTK.Mathematics;

namespace cgi.control
{
    public class PBRControlWindow
    {
        private static ControlWindow shaderControlPanel;

        private static Action<Color, Vector3> SetColorAndHue;
        public static void ShowShaderControlWindow()
        {
            Thread uiShaderThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                shaderControlPanel = new ControlWindow(300, 0);

                shaderControlPanel.AddSlider(
                    "HDR Strenght",
                    1,
                    100,
                    1,
                    v => PBRSettings.hdrStrenght = v,
                    0,
                    1,
                    PBRSettings.hdrStrenght
                );

                shaderControlPanel.AddSlider(
                "maxDistance",
                1,
                100,
                1,
                v => GlobalSettings.tessMaxDistance = v,
                50,
                500,
                GlobalSettings.tessMaxDistance
                );

                shaderControlPanel.AddSlider(
               "minDistance",
               1,
               100,
               1,
               v => GlobalSettings.tessMinDistance = v,
               0,
               250,
               GlobalSettings.tessMinDistance
               );

                shaderControlPanel.AddSlider(
             "maxLevel",
             1,
             100,
             1,
             v => GlobalSettings.maxTessLevel = (int)v,
             1,
             50,
             GlobalSettings.maxTessLevel
             );

                shaderControlPanel.AddSlider(
              "minLevel",
              1,
              100,
              1,
              v => GlobalSettings.minTessLevel = (int)v,
              1,
              10,
              GlobalSettings.minTessLevel
              );

                shaderControlPanel.AddSlider(
                "Leadr",
                1,
                100,
                1,
                v => PBRSettings.leadr = v,
                0f,
                2f,
                PBRSettings.leadr
            );

                shaderControlPanel.AddSlider(
                    "WavePeakScatterStrength",
                    1,
                    100,
                    1,
                    v => PBRSettings.WavePeakScatterStrength = v,
                    0f,
                    50f,
                    PBRSettings.WavePeakScatterStrength
                );

                shaderControlPanel.AddSlider(
                    "Environment Light Strength",
                    1,
                    100,
                    1,
                    v => PBRSettings.envStrength = v,
                    0f,
                    5f,
                    PBRSettings.envStrength
                );

                shaderControlPanel.AddSlider(
                   "Roughness",
                   1,
                   100,
                   1,
                   v => PBRSettings.roughness = v,
                   0f,
                   0.2f,
                   PBRSettings.roughness
                );

                shaderControlPanel.AddSlider(
                  "FoamRoughness",
                  1,
                  100,
                  1,
                  v => PBRSettings.foamRoughness = v,
                  0f,
                  0.4f,
                  PBRSettings.foamRoughness
                );

                shaderControlPanel.AddSlider(
                   "ScatterStrength",
                   1,
                   100,
                   1,
                   v => PBRSettings.scatterStrength = v,
                   0f,
                   2f,
                   PBRSettings.scatterStrength
                );

                shaderControlPanel.AddSlider(
                   "ScatterShadowStrength",
                   1,
                   100,
                   1,
                   v => PBRSettings.scatterShadowStrength = v,
                   0f,
                   2f,
                   PBRSettings.scatterShadowStrength
                );

                shaderControlPanel.AddSlider(
                   "BubbleDensity",
                   1,
                   100,
                   1,
                   v => PBRSettings.bubbleDensity = v,
                   0f,
                   2f,
                   PBRSettings.bubbleDensity
                );               

                shaderControlPanel.AddSlider(
                    "Vertical Skybox Offset",
                    1,
                    100,
                    1,
                    v => PBRSettings.verticalSkyboxOffset = v,
                    0,
                    100,
                    0
                );

                shaderControlPanel.AddSlider(
                    "LightDir horizontal",
                    1,
                    100,
                    1,
                    angle =>
                    {
                        lightYaw = angle;
                        UpdateLightDirection();
                    },
                    0,
                    360,
                    0
                );

                shaderControlPanel.AddSlider(
                    "LightDir vertical",
                    1,
                    100,
                    1,
                    angle =>
                    {
                        lightPitch = angle;
                        UpdateLightDirection();
                    },
                    0,
                    89,
                    45
                );

                List<string> list = new List<string>();
                for (int i = 1; i < 10; i++)
                    list.Add("Cubemap_Sky_0" + i + "-512x512");
                for (int i = 10; i <= 25; i++)
                    list.Add("Cubemap_Sky_" + i + "-512x512");

                list.Add("Cubemap4k");
                list.Add("Epic4k");
                list.Add("Sunrise4k");
                list.Add("Clear4k");
                list.Add("Cloudy4k");

                shaderControlPanel.AddDropdown("x", list, v => PBRSettings.PendingSkybox = v);

                shaderControlPanel.AddSelectionButtons(
                    "ScatterColor", () => {
                        PBRSettings.selectedColorName = "ScatterColor";
                        SetColorAndHue(PBRSettings.scatterColor_, PBRSettings.scatterColor_ash);
                    },
                    () => PBRSettings.scatterColor,
                    "BubbleColor", () => {
                        PBRSettings.selectedColorName = "BubbleColor";
                        SetColorAndHue(PBRSettings.bubbleColor_, PBRSettings.bubbleColor_ash);
                    },
                    () => PBRSettings.bubbleColor,
                    "Lsun", () => {
                        PBRSettings.selectedColorName = "Lsun";
                        SetColorAndHue(PBRSettings.Lsun_, PBRSettings.Lsun_ash);
                    },
                    () => PBRSettings.Lsun

                );

                SetColorAndHue = shaderControlPanel.AddColorWheel((c, ash) => {
                    Color color255 = ColorWheel.ColorFromHSV(ash.X, ash.Y, ash.Z);

                    Vector3 color = new Vector3(color255.R / 255f, color255.G / 255f, color255.B / 255f);
                    if (GlobalSettings.advancedDebug) Console.WriteLine("[ColorWheel] " + color);
                    switch (PBRSettings.selectedColorName)
                    {
                        case "ScatterColor":
                            {
                                PBRSettings.scatterColor = color;

                                PBRSettings.scatterColor_ = c;
                                PBRSettings.scatterColor_ash = ash;
                                break;
                            }
                        case "BubbleColor":
                            {
                                PBRSettings.bubbleColor = color;

                                PBRSettings.bubbleColor_ = c;
                                PBRSettings.bubbleColor_ash = ash;
                                break;
                            }
                        case "Lsun":
                            {
                                PBRSettings.Lsun = color;

                                PBRSettings.Lsun_ = c;
                                PBRSettings.Lsun_ash = ash;
                                break;
                            }
                        default: Console.WriteLine("This should not happen!!"); break;
                    }
                });
                SetColorAndHue(PBRSettings.scatterColor_, PBRSettings.scatterColor_ash);

                shaderControlPanel.AddButton("Toggle Grid", () => GlobalSettings.toggleGrid = !GlobalSettings.toggleGrid);

                Application.Run(shaderControlPanel);
            });

            uiShaderThread.IsBackground = true;
            uiShaderThread.SetApartmentState(ApartmentState.STA);
            uiShaderThread.Start();
        }

        static float lightPitch = 45f;
        static float lightYaw = 0f;

        private static void UpdateLightDirection()
        {
            float pitch = MathHelper.DegreesToRadians(lightPitch);
            float yaw = MathHelper.DegreesToRadians(lightYaw);

            float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = (float)Math.Sin(pitch);
            float z = (float)(Math.Cos(pitch) * Math.Sin(yaw));

            Light.lightDirection = Vector3.Normalize(new Vector3(x, y, z));
        }

        public static void Hide()
        {
            shaderControlPanel.Hide();
        }

        public static void Show()
        {
            shaderControlPanel.Show();
        }
    }
}
