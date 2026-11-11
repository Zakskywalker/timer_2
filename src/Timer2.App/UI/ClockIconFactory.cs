using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Timer2.App.UI
{
    public static class ClockIconFactory
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        /// <summary>
        /// Generates the circular clock icon matching the uploaded screenshots.
        /// Blue/Cyan when paused or idle, Red when recording time.
        /// </summary>
        public static Icon CreateClockIcon(bool isRecording, int size = 32)
        {
            using var bmp = CreateClockBitmap(isRecording, size);
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var tempIcon = Icon.FromHandle(hIcon);
                // Clone icon so we can safely destroy the native handle
                return (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        public static Bitmap CreateClockBitmap(bool isRecording, int size = 32)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float margin = size * 0.06f;
            float diameter = size - (margin * 2);

            // Clock face colors
            // Idle / paused: Cyan blue (#46b4d2 or #38bdf8 as seen in the taskbar image)
            // Recording: Vivid red (#dc2626)
            Color faceColor = isRecording 
                ? Color.FromArgb(220, 38, 38) 
                : Color.FromArgb(70, 185, 210);

            Color borderColor = Color.FromArgb(20, 20, 20);
            Color tickColor = Color.FromArgb(30, 30, 30);
            Color handColor = Color.FromArgb(15, 15, 15);

            // Outer drop ring / shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, margin + 1, margin + 1, diameter, diameter);
            }

            // Outer border
            using (var borderPen = new Pen(borderColor, size * 0.08f))
            {
                g.DrawEllipse(borderPen, margin, margin, diameter, diameter);
            }

            // Inner face fill
            using (var faceBrush = new SolidBrush(faceColor))
            {
                g.FillEllipse(faceBrush, margin + (size * 0.04f), margin + (size * 0.04f), diameter - (size * 0.08f), diameter - (size * 0.08f));
            }

            float cx = size / 2.0f;
            float cy = size / 2.0f;
            float radius = (diameter / 2.0f) - (size * 0.06f);

            // 12 tick marks around the clock perimeter
            using (var tickPen = new Pen(tickColor, Math.Max(1.0f, size * 0.04f)))
            {
                for (int i = 0; i < 12; i++)
                {
                    double angle = (i * 30.0) * Math.PI / 180.0;
                    float tickLen = (i % 3 == 0) ? size * 0.12f : size * 0.07f;
                    float x1 = cx + (float)(Math.Sin(angle) * (radius - tickLen));
                    float y1 = cy - (float)(Math.Cos(angle) * (radius - tickLen));
                    float x2 = cx + (float)(Math.Sin(angle) * radius);
                    float y2 = cy - (float)(Math.Cos(angle) * radius);
                    g.DrawLine(tickPen, x1, y1, x2, y2);
                }
            }

            // Clock hands (pointing to roughly 10:10 like classic clock faces)
            using (var handPen = new Pen(handColor, Math.Max(1.5f, size * 0.06f)))
            {
                handPen.StartCap = LineCap.Round;
                handPen.EndCap = LineCap.Round;

                // Hour hand (shorter, pointing to 10 o'clock)
                double hourAngle = 300.0 * Math.PI / 180.0;
                float hx = cx + (float)(Math.Sin(hourAngle) * (radius * 0.52f));
                float hy = cy - (float)(Math.Cos(hourAngle) * (radius * 0.52f));
                g.DrawLine(handPen, cx, cy, hx, hy);

                // Minute hand (longer, pointing to 2 o'clock)
                double minAngle = 60.0 * Math.PI / 180.0;
                float mx = cx + (float)(Math.Sin(minAngle) * (radius * 0.75f));
                float my = cy - (float)(Math.Cos(minAngle) * (radius * 0.75f));
                g.DrawLine(handPen, cx, cy, mx, my);
            }

            // Center pin
            using (var pinBrush = new SolidBrush(handColor))
            {
                float pinR = size * 0.05f;
                g.FillEllipse(pinBrush, cx - pinR, cy - pinR, pinR * 2, pinR * 2);
            }

            return bmp;
        }
    }
}
