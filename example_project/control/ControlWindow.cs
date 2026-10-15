
using cgi.settings;
using OpenTK.Mathematics;

namespace cgi.control
{

    public class ControlWindow : Form
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
        {
            Interval = 100
        };

        public ControlWindow(int x, int y)
        {
            Text = " ";
            TopMost = true;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            Location = new Point(x, y);
            ControlBox = false;
            //Size = new Size(300, 300);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(300,100);

            timer.Start();
        }

        public Action<Color, Vector3> AddColorWheel(Action<Color, Vector3> setValue)
        {
            bool firstTime = true;

            ColorWheel wheel = new ColorWheel();
            wheel.Dock = DockStyle.Top;
            wheel.ColorChanged += (s, e) =>
            {
                setValue(wheel.SelectedColor,new Vector3((float)wheel._angle,(float) wheel._saturation, wheel.hue));
            };

                TrackBar slider = AddSlider("Hue", 0, 100, 1, 
                v => {
                    //Keep default color on startup
                    if (firstTime) {
                        firstTime = false;
                        return;
                    }
                    wheel.hue = v;

                    setValue(wheel.SelectedColor, new Vector3((float)wheel._angle, (float)wheel._saturation, wheel.hue));
                }, 
                0, 1, 1);

            Action<Color, Vector3> SetColorAndHue = delegate (Color color, Vector3 angleSatHue) {
                float hue = angleSatHue.Z;
                wheel.SetColor(color);
                wheel.hue = hue;
                slider.Value = (int) (hue * 100);
            };

            Controls.Add(wheel);

            AddDisplay(
                "Selected Value",
                () => $"{PBRSettings.selectedColorName}"
            );

            return SetColorAndHue;
        }

        public void AddDropdown(string title, List<string> content, Action<string> onChanged)
        {
            ComboBox comboBox = new ComboBox();
            comboBox.Dock = DockStyle.Top;

            foreach(string item in content) comboBox.Items.Add(item);

            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.SelectedIndex = 2;

            comboBox.SelectedIndexChanged += (sender, e) =>
            {
                string selected = comboBox.SelectedItem?.ToString();

                if (selected != null)
                    onChanged?.Invoke(selected);
            };

            Controls.Add(comboBox);

            Label valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Text = title
            };
            Controls.Add(valueLabel);
        }

        public TrackBar AddSlider(
            string title,
            int start,
            int end,
            int freq,
            Action<float> setValue,
            float min,
            float max,
            float initial)
        {
            if (end <= start)
                throw new ArgumentException("end must be > start");

            initial = Math.Clamp(initial, min, max);

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true
            };

            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font(Font, FontStyle.Bold)
            };

            var slider = new TrackBar
            {
                Minimum = start,
                Maximum = end,
                TickFrequency = freq,
                Dock = DockStyle.Top
            };

            float t = (initial - min) / (max - min);
            int sliderValue = start + (int)MathF.Round(t * (end - start));
            slider.Value = Math.Clamp(sliderValue, start, end);

            setValue(initial);

            slider.ValueChanged += (_, _) =>
            {
                float tt = (slider.Value - start) / (float)(end - start);
                float value = min + tt * (max - min);
                setValue(value);
            };

            panel.Controls.Add(slider);
            panel.Controls.Add(titleLabel);

            Controls.Add(panel);

            return slider;
        }



        public void AddDisplay(
            string label,
            Func<string> getText
        )
        {
            Label valueLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Text = label
            };

            timer.Tick += (_, _) =>
            {
                valueLabel.Text = getText();
            };

            Controls.Add(valueLabel);
        }

        public void AddButton(string title, Action onClick)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true
            };

            var button = new Button
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            button.Click += (_, _) =>
            {
                onClick?.Invoke();
            };

            panel.Controls.Add(button);
            Controls.Add(panel);
        }

        private static FlowLayoutPanel CreateButtonWithIndicator(
            string text,
            out Button button,
            out Panel indicator)
        {
            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(3)
            };

            indicator = new Panel
            {
                Width = 80,
                Height = 10,
                Margin = new Padding(0)
            };

            button = new Button
            {
                Text = text,
                AutoSize = true
            };

            container.Controls.Add(indicator);
            container.Controls.Add(button);

            return container;
        }

        public void AddSelectionButtons(
            string b1Title, Action b1onClick, Func<Vector3> getb1Color,
            string b2Title, Action b2onClick, Func<Vector3> getb2Color,
            string b3Title, Action b3onClick, Func<Vector3> getb3Color)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };



            var c1 = CreateButtonWithIndicator(
                b1Title,
                out var button1,
                out var colorIndicator1);

            var c2 = CreateButtonWithIndicator(
                b2Title,
                out var button2,
                out var colorIndicator2);

            var c3 = CreateButtonWithIndicator(
                b3Title,
                out var button3,
                out var colorIndicator3);

            panel.Controls.Add(c1);
            panel.Controls.Add(c2);
            panel.Controls.Add(c3);

            void SelectButton(Button selected)
            {
                button1.Enabled = button1 != selected;
                button2.Enabled = button2 != selected;
                button3.Enabled = button3 != selected;
            }

            button1.Click += (_, _) =>
            {
                SelectButton(button1);
                b1onClick?.Invoke();
            };

            button2.Click += (_, _) =>
            {
                SelectButton(button2);
                b2onClick?.Invoke();
            };

            button3.Click += (_, _) =>
            {
                SelectButton(button3);
                b3onClick?.Invoke();
            };

            timer.Tick += (_, _) =>
            {
                var c1 = getb1Color();
                colorIndicator1.BackColor = Color.FromArgb(
                    (int)(c1.X * 255),
                    (int)(c1.Y * 255),
                    (int)(c1.Z * 255));

                var c2 = getb2Color();
                colorIndicator2.BackColor = Color.FromArgb(
                    (int)(c2.X * 255),
                    (int)(c2.Y * 255),
                    (int)(c2.Z * 255));

                var c3 = getb3Color();
                colorIndicator3.BackColor = Color.FromArgb(
                    (int)(c3.X * 255),
                    (int)(c3.Y * 255),
                    (int)(c3.Z * 255));
            };

            button1.Enabled = false;

            Controls.Add(panel);
        }

        public void AddCheckbox(Action<bool> onCheckedChanged,bool startValue)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true
            };

            var checkBox = new CheckBox
            {
                Checked = startValue,
                AutoSize = true
            };

            checkBox.CheckedChanged += (_, _) =>
            {
                onCheckedChanged?.Invoke(checkBox.Checked);
            };

            panel.Controls.Add(checkBox);
            Controls.Add(panel);
        }

    }
}
