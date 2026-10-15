
using cgi.settings;
using OpenTK.Mathematics;

namespace cgi.control
{
    public class BuoyancyControlWindow
    {
        private static ControlWindow buoyancyControlPanel;


        public static void ShowBuoyancyControlWindow()
        {
            Thread uiShaderThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                buoyancyControlPanel = new ControlWindow(900, 0);

                buoyancyControlPanel.AddSlider(
                    "Flow Strength",
                    1,
                    100,
                    1,
                    v => BuoyancySettings.flowStrength = v,
                    0.005f,
                    0.04f,
                    BuoyancySettings.flowStrength
                );

                buoyancyControlPanel.AddSlider(
                    "Damping",
                    1,
                    100,
                    1,
                    v => BuoyancySettings.damping = v,
                    0.8f,
                    0.99f,
                    BuoyancySettings.damping
                );

                buoyancyControlPanel.AddSlider(
                    "Gravity",
                    1,
                    100,
                    1,
                    v => BuoyancySettings.gravity = new Vector3(0, v, 0),
                    -15,
                    -6,
                    BuoyancySettings.gravity.Y
                );

                buoyancyControlPanel.AddSlider(
                    "Mass",
                    1,
                    100,
                    1,
                    v => BuoyancySettings.mass = v,
                    1,
                    200,
                    BuoyancySettings.mass
                );

                buoyancyControlPanel.AddSlider(
                    "Buoyancy Strenght",
                    1,
                    100,
                    1,
                    v => BuoyancySettings.buoyancyStrenght = v,
                    1,
                    10,
                    BuoyancySettings.buoyancyStrenght
                );

                buoyancyControlPanel.AddButton(
                    "Collision", () => BuoyancySettings.doCollision = !BuoyancySettings.doCollision
                );

                buoyancyControlPanel.AddDisplay("Collision",() => BuoyancySettings.doCollision ? "Collision: On" : "Collision: OFF");

                Application.Run(buoyancyControlPanel);
            });

            uiShaderThread.IsBackground = true;
            uiShaderThread.SetApartmentState(ApartmentState.STA);
            uiShaderThread.Start();
        }

        public static void Hide()
        {
            buoyancyControlPanel.Hide();
        }

        public static void Show()
        {
            buoyancyControlPanel.Show();
        }
    }
}
