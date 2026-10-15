using cgi.game_objects;
using cgi.shaders.gerstner;

namespace cgi.control
{
    public class OceanControlWindow
    {
        public static ControlWindow controlPanel;

        public static void updateControlWindowGerstner()
        {
            //if (toggleWaveModel == 0) return;

            GerstnerWaves gerstner = GerstnerWaves.Instance;

            controlPanel.Invoke(() =>
            {
                controlPanel.Controls.Clear();

                GerstnerControlWindow.updateControlWindowControlsGerstner(gerstner);
            });
        }

        public static void updateControlWindowFFT(JONSWAP_SpectrumParameters parameters)
        {
            //if (toggleWaveModel == 1) return;

            JONSWAP_SpectrumParameters fft = parameters;

            controlPanel.Invoke(() => FFTControlWindow.buildFFTControlPanel(fft));
        }

        public static void Hide()
        {
            controlPanel.Hide();
        }

        public static void Show()
        {
            controlPanel.Show();
        }
    }
}
