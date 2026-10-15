using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class ColorWheel : Control
{
    public event EventHandler ColorChanged;

    private Bitmap wheelBitmap;

    private Color selectedColor = Color.Red;
    public Color SelectedColor => selectedColor;

    private Point selectorPoint;

    public float hue = 1.0f;
    public double _saturation = 1.0f;
    public double _angle = 1.0f;

    public ColorWheel()
    {
        DoubleBuffered = true;
        Width = 250;
        Height = 250;

        GenerateWheel();

        MouseDown += WheelMouse;
        MouseMove += WheelMouse;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        int size = Math.Min(Width, Height);

        if (size <= 1)
            return;

        GenerateWheel();
        Invalidate();
    }

    private void WheelMouse(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        int size = Math.Min(Width, Height);

        int offsetX = (Width - size) / 2;
        int offsetY = (Height - size) / 2;

        float radius = (size - 1) / 2f;

        float centerX = offsetX + radius;
        float centerY = offsetY + radius;

        double dx = e.X - centerX;
        double dy = e.Y - centerY;

        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > radius)
            return;

        double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

        if (angle < 0)
            angle += 360;

        double saturation = distance / radius;
        _saturation = saturation;
        _angle = angle;
        selectedColor = ColorFromHSV(angle, saturation, hue);

        selectorPoint = e.Location;

        Invalidate();

        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GenerateWheel()
    {
        wheelBitmap?.Dispose();

        int size = Math.Min(Width, Height);

        if (size <= 1)
            return;

        wheelBitmap = new Bitmap(size, size);

        float radius = (size - 1) / 2f;

        float center = radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - center;
                double dy = y - center;

                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

                    if (angle < 0)
                        angle += 360;

                    double saturation = distance / radius;

                    saturation = Math.Max(0, Math.Min(1, saturation));

                    Color color = ColorFromHSV(angle, saturation, hue);

                    wheelBitmap.SetPixel(x, y, color);
                }
                else
                {
                    wheelBitmap.SetPixel(x, y, Color.Transparent);
                }
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (wheelBitmap == null)
            return;

        int size = Math.Min(Width, Height);

        int offsetX = (Width - size) / 2;
        int offsetY = (Height - size) / 2;

        e.Graphics.DrawImage(wheelBitmap, offsetX, offsetY);

        // Draw selector
        if (selectorPoint != Point.Empty)
        {
            e.Graphics.DrawEllipse(
                Pens.White,
                selectorPoint.X - 5,
                selectorPoint.Y - 5,
                10,
                10);

            e.Graphics.DrawEllipse(
                Pens.Black,
                selectorPoint.X - 6,
                selectorPoint.Y - 6,
                12,
                12);
        }
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        return hi switch
        {
            0 => Color.FromArgb(255, v, t, p),
            1 => Color.FromArgb(255, q, v, p),
            2 => Color.FromArgb(255, p, v, t),
            3 => Color.FromArgb(255, p, q, v),
            4 => Color.FromArgb(255, t, p, v),
            _ => Color.FromArgb(255, v, p, q),
        };
    }

    public void SetColor(Color color)
    {
        selectedColor = color;

        // Convert RGB -> HSV
        double h, s, v;
        ColorToHSV(color, out h, out s, out v);

        _angle = h;
        _saturation = s;
        hue = (float)v;

        int size = Math.Min(Width, Height);

        int offsetX = (Width - size) / 2;
        int offsetY = (Height - size) / 2;

        float radius = (size - 1) / 2f;

        float centerX = offsetX + radius;
        float centerY = offsetY + radius;

        // Convert polar back to cartesian
        double radians = h * Math.PI / 180.0;

        float x = (float)(centerX + Math.Cos(radians) * (s * radius));
        float y = (float)(centerY + Math.Sin(radians) * (s * radius));

        selectorPoint = new Point((int)x, (int)y);

        Invalidate();

        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = color.GetHue();

        saturation = max == 0
            ? 0
            : 1d - (1d * min / max);

        value = max / 255d;
    }

}