using cgi.shaders.gerstner;
using cgi.shaders.tessellation.Gerstner;
using cgimin.engine.camera;
using Examples.Tutorial;
using OpenTK.Mathematics;

namespace cgi.control
{
    public class GerstnerControlWindow
    {      
        public static void ShowControlWindow()
        {
            Thread uiThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                GerstnerWaves gerstner = GerstnerWaves.Instance;

                OceanControlWindow.controlPanel = new ControlWindow(0, 0);

                updateControlWindowControlsGerstner(gerstner);

                Application.Run(OceanControlWindow.controlPanel);
            });

            uiThread.IsBackground = true;
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
        }

        public static void updateControlWindowControlsGerstner(GerstnerWaves gerstner)
        {
            OceanControlWindow.controlPanel.AddSlider(
                    "Steepness",
                    0,
                    100,
                    1,
                    v => gerstner.steepness = v,
                    0,
                    5.0f,
                    gerstner.steepness
                );

            OceanControlWindow.controlPanel.AddSlider(
                "NoiseSize",
                0,
                100,
                1,
                v => gerstner.noiseSize = v,
                0.0005f,
                0.4f,
                gerstner.noiseSize
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Scaling",
                0,
                100,
                1,
                v => gerstner.scaling = v,
                0.00001f,
                0.1f,
                gerstner.scaling
            );

            OceanControlWindow.controlPanel.AddSlider(
                "FoamBias",
                0,
                100,
                1,
                v => gerstner.foamBias = v,
                0,
                2,
                gerstner.foamBias
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Playback-Speed",
                0,
                100,
                1,
                v => gerstner.playbackSpeed = v,
                0.1f,
                3,
                gerstner.playbackSpeed
            );

            OceanControlWindow.controlPanel.AddSlider(
                "strengthAmplifier",
                0,
                100,
                1,
                v => { gerstner.strengthAmplifier = v; },
                0.5f,
                10,
                gerstner.strengthAmplifier
            );

            OceanControlWindow.controlPanel.AddDisplay(
                "FPS",
                () => $"{ExampleProject._fps:0.0} FPS"
            );

            OceanControlWindow.controlPanel.AddDisplay(
                "Position",
                () =>
                {
                    Vector3 pos = Camera.position;
                    return $"Position: ({pos.X:0.00}, {pos.Y:0.00}, {pos.Z:0.00})";
                }
            );

            OceanControlWindow.controlPanel.AddDisplay(
                "Speed",
                () => $"Speed: {Camera.moveSpeed:0.0} Units"
            );

            OceanControlWindow.controlPanel.AddDisplay(
                "NoiseSize",
                () => $"NoiseSize: {gerstner.noiseSize:0.0}"
            );

            OceanControlWindow.controlPanel.AddButton("Toggle Normal Calc", () => GerstnerTessellationMaterial.calcFragmentNormalAndJacobian = !GerstnerTessellationMaterial.calcFragmentNormalAndJacobian);
        }
    }
}
