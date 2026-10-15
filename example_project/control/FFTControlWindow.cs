using cgi.game_objects;
using cgimin.engine.camera;
using Examples.Tutorial;
using OpenTK.Mathematics;

namespace cgi.control
{
    public class FFTControlWindow
    {
        public static void buildFFTControlPanel(JONSWAP_SpectrumParameters fft)
        {
            OceanControlWindow.controlPanel.Controls.Clear();

            OceanControlWindow.controlPanel.AddSlider(
                "Wind-direction",
                0,
                360,
                1,
                v => fft.degree = v,
                0,
                360,
                fft.degree
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Windspeed",
                0,
                100,
                1,
                v => fft.windSpeed = v,
                1,
                20,
                fft.windSpeed
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Fetch",
                1,
                100,
                1,
                v => fft.fetch = v,
                100,
                500000,
                fft.fetch
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Spread Blend",
                1,
                100,
                1,
                v => fft.spreadBlend = v,
                0,
                1,
                fft.spreadBlend
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Swell",
                1,
                100,
                1,
                v => fft.swell = v,
                0,
                1,
                fft.swell
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Scale",
                1,
                100,
                1,
                v => fft.scale = v,
                0.5f,
                5,
                fft.scale
            );

            OceanControlWindow.controlPanel.AddSlider(
                "UV-Scaling",
                1,
                100,
                1,
                v => fft.uvScale = v,
                0.2f,
                5,
                fft.uvScale
            );

            OceanControlWindow.controlPanel.AddSlider(
                "lengthScale",
                1,
                100,
                1,
                v => fft.lengthScale = v,
                1,
                20,
                fft.lengthScale
            );

            OceanControlWindow.controlPanel.AddSlider(
                "cutoffLow",
                1,
                100,
                1,
                v => fft.cutoffLow = v,
                1,
                15,
                fft.cutoffLow
            );

            OceanControlWindow.controlPanel.AddSlider(
                "cutoffHigh",
                1,
                100,
                1,
                v => fft.cutoffHigh = v,
                15,
                100,
                fft.cutoffHigh
            );

            OceanControlWindow.controlPanel.AddSlider(
                "playbackSpeed",
                1,
                100,
                1,
                v => fft.playbackSpeed = v,
                0.1f,
                5f,
                fft.playbackSpeed
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Foam Multiplier",
                1,
                100,
                1,
                v => fft.foamMult = v,
                0f,
                5f,
                fft.foamMult
            );

            OceanControlWindow.controlPanel.AddSlider(
                "Lambda",
                1,
                100,
                1,
                v => fft.lambda = v,
                0f,
                2f,
                fft.lambda
            );

            OceanControlWindow.controlPanel.AddSlider(
                "FoamBias",
                1,
                100,
                1,
                v => fft.foamBias = v,
                0f,
                2f,
                fft.foamBias
            );

            OceanControlWindow.controlPanel.AddSlider(
                "FoamDecay",
                1,
                100,
                1,
                v => fft.foamDecayRate = v,
                0f,
                20f,
                fft.foamDecayRate
            );

            OceanControlWindow.controlPanel.AddSlider(
                "FoamAdd",
                1,
                100,
                1,
                v => fft.foamAdd = v,
                0f,
                20f,
                fft.foamAdd
            );

            OceanControlWindow.controlPanel.AddCheckbox(
                b => fft.isActive = b,
                fft.isActive
            );

            OceanControlWindow.controlPanel.AddButton("Save", () => fft.saveToFile("../../../settings/" + fft.name));

            OceanControlWindow.controlPanel.AddButton("Load", () => {
                fft.loadFromFile("../../../settings/" + fft.name);
                OceanControlWindow.controlPanel.Invoke(() =>
                {
                    buildFFTControlPanel(fft);
                });
            });

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

            Label valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Text = fft.name
            };
            OceanControlWindow.controlPanel.Controls.Add(valueLabel);
        }
    }
}
