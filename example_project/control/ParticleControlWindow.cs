using cgi.game_objects.particles;
using cgi.settings;

namespace cgi.control
{
    public class ParticleControlWindow
    {
        private static ControlWindow particleControlPanel;


        public static void ShowParticleControlWindow()
        {
            Thread uiShaderThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                particleControlPanel = new ControlWindow(600, 0);

                particleControlPanel.AddSlider(
                    "Particle Amount",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.yThreadAmount = (int)v,
                    1,
                    64,
                    SprayParticleSystem.yThreadAmount
                );

                particleControlPanel.AddSlider(
                    "SprayGenerationRadius",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.sprayGenerationRadius = v,
                    50,
                    500,
                    SprayParticleSystem.sprayGenerationRadius
                );

                particleControlPanel.AddSlider(
                    "FoamThreshold",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.foamThreshold = v,
                    0.01f,
                    1,
                    SprayParticleSystem.foamThreshold
                );

                particleControlPanel.AddSlider(
                    "ParticleLifetime",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.particleLifetime = v,
                    0.02f,
                    2,
                    SprayParticleSystem.particleLifetime
                );

                particleControlPanel.AddSlider(
                    "VerticalSpeedDampening",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.verticalSpeedDampening = v,
                    0.02f,
                    2,
                    SprayParticleSystem.verticalSpeedDampening
                );

                particleControlPanel.AddSlider(
                    "ParticleSpeed",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.particleSpeed = v,
                    0.02f,
                    2,
                    SprayParticleSystem.particleSpeed
                );

                particleControlPanel.AddSlider(
                    "ParticleSizeMin",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.particleSizeMin = v,
                    0.02f,

                    10,
                    SprayParticleSystem.particleSizeMin
                );

                particleControlPanel.AddSlider(
                    "ParticleSizeMax",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.particleSizeMax = v,
                    0.02f,
                    10,
                    SprayParticleSystem.particleSizeMax
                );

                particleControlPanel.AddSlider(
                    "Mist Amount",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.mistPercentage = v,
                    1,
                    2000,
                    SprayParticleSystem.mistPercentage
                );

                particleControlPanel.AddSlider(
                    "Mist Opacity",
                    1,
                    100,
                    1,
                    v => SprayParticleSystem.mistTransparancy = v,
                    0.001f,
                    1,
                    SprayParticleSystem.mistTransparancy
                );

                particleControlPanel.AddSlider(
                   "Mist Size",
                   1,
                   100,
                   1,
                   v => SprayParticleSystem.mistSize = v,
                   0.001f,
                   1,
                   SprayParticleSystem.mistSize
               );

                particleControlPanel.AddSlider(
                  "Color Correction",
                  1,
                  100,
                  1,
                  v => SprayParticleSystem.colorCorrection = v,
                  0.02f,
                  2,
                  SprayParticleSystem.colorCorrection
              );

                particleControlPanel.AddSlider(
                  "VerticalSpeedDampening Mist",
                  1,
                  100,
                  1,
                  v => SprayParticleSystem.verticalSpeedDampeningMist = v,
                  0f,
                  1,
                  SprayParticleSystem.verticalSpeedDampeningMist
              );

                particleControlPanel.AddSlider(
                  "SprayTransparancy",
                  1,
                  100,
                  1,
                  v => SprayParticleSystem.sprayTransparancy = v,
                  0f,
                  1,
                  SprayParticleSystem.sprayTransparancy
              );

                particleControlPanel.AddSlider(
                  "Wind Strength",
                  1,
                  100,
                  1,
                  v => SprayParticleSystem.windScaling = v,
                  1f,
                  10,
                  SprayParticleSystem.windScaling
              );

                Application.Run(particleControlPanel);
            });

            uiShaderThread.IsBackground = true;
            uiShaderThread.SetApartmentState(ApartmentState.STA);
            uiShaderThread.Start();
        }

        public static void Hide()
        {
            particleControlPanel.Hide();
        }

        public static void Show()
        {
            particleControlPanel.Show();
        }
    }
}
