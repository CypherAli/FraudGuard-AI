using Microsoft.Maui.Graphics;

namespace FraudGuardAI
{
    public class RadarDrawable : IDrawable
    {
        public bool IsActive { get; set; } = true;
        public float Angle { get; set; } = 0f;

        private const int SWEEP_STEPS = 60;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Width / 2f;
            float cy = dirtyRect.Height / 2f;
            float R = Math.Min(cx, cy) - 6f;
            if (R <= 0) return;

            canvas.Antialias = true;

            // ── Background: dark filled circle with subtle tint ──
            canvas.FillColor = IsActive
                ? Color.FromRgba(0.008f, 0.10f, 0.10f, 0.96f)   // very dark teal
                : Color.FromRgba(0.10f, 0.02f, 0.02f, 0.96f);    // very dark red
            canvas.FillCircle(cx, cy, R - 1f);

            // ── Outer glow ring (outside main circle) ──
            canvas.StrokeColor = IsActive
                ? Color.FromArgb("#4014B8A6")    // teal glow 25%
                : Color.FromArgb("#30EF4444");   // red glow 19%
            canvas.StrokeSize = 2.5f;
            canvas.DrawCircle(cx, cy, R + 2f);

            // ── Outer circle ──
            canvas.StrokeColor = IsActive
                ? Color.FromArgb("#5914B8A6")   // teal 35%
                : Color.FromArgb("#40EF4444");   // red  25%
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(cx, cy, R);

            // ── Inner rings ──
            canvas.StrokeColor = IsActive
                ? Color.FromArgb("#2614B8A6")   // teal 15%
                : Color.FromArgb("#1EEF4444");   // red  12%
            canvas.StrokeSize = 1f;
            canvas.DrawCircle(cx, cy, R * 0.65f);
            canvas.DrawCircle(cx, cy, R * 0.38f);

            // ── Crosshair ──
            canvas.StrokeColor = IsActive
                ? Color.FromArgb("#1E14B8A6")
                : Color.FromArgb("#19EF4444");
            canvas.StrokeSize = 0.8f;
            canvas.DrawLine(cx, cy - R, cx, cy + R);
            canvas.DrawLine(cx - R, cy, cx + R, cy);

            // ── Sweep gradient (filled triangles from center) ──
            float sweepSpan = (float)(Math.PI / 1.8);
            for (int i = 0; i < SWEEP_STEPS; i++)
            {
                float ratio = (float)i / SWEEP_STEPS;
                float alpha = IsActive
                    ? (1f - ratio) * 0.42f
                    : (1f - ratio) * 0.28f;

                float a1 = Angle - sweepSpan * ratio;
                float a2 = Angle - sweepSpan * (ratio + 1f / SWEEP_STEPS);

                float x1 = cx + (R - 2f) * (float)Math.Cos(a1);
                float y1 = cy + (R - 2f) * (float)Math.Sin(a1);
                float x2 = cx + (R - 2f) * (float)Math.Cos(a2);
                float y2 = cy + (R - 2f) * (float)Math.Sin(a2);

                var path = new PathF();
                path.MoveTo(cx, cy);
                path.LineTo(x1, y1);
                path.LineTo(x2, y2);
                path.Close();

                canvas.FillColor = IsActive
                    ? Color.FromRgba(0.078f, 0.722f, 0.651f, alpha)
                    : Color.FromRgba(0.937f, 0.267f, 0.267f, alpha);
                canvas.FillPath(path);
            }

            // ── Sweep arm ──
            float endX = cx + (R - 2f) * (float)Math.Cos(Angle);
            float endY = cy + (R - 2f) * (float)Math.Sin(Angle);
            canvas.StrokeColor = IsActive
                ? Color.FromArgb("#14B8A6")
                : Color.FromArgb("#EF4444");
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(cx, cy, endX, endY);

            // ── Center dot glow ──
            canvas.FillColor = IsActive
                ? Color.FromArgb("#3014B8A6")
                : Color.FromArgb("#30EF4444");
            canvas.FillCircle(cx, cy, 7f);

            // ── Center dot ──
            canvas.FillColor = IsActive
                ? Color.FromArgb("#2DD4BF")
                : Color.FromArgb("#EF4444");
            canvas.FillCircle(cx, cy, 3.5f);
        }
    }
}
