using Microsoft.Maui.Graphics;

namespace FraudGuardAI
{
    public class WaveformDrawable : IDrawable
    {
        public bool IsActive { get; set; } = true;

        private float _time = 0f;
        private readonly float[] _heights;
        private readonly float[] _speeds;
        private readonly float[] _offsets;
        private const int BAR_COUNT = 32;

        public WaveformDrawable()
        {
            var rnd = new Random(42);
            _heights = Enumerable.Range(0, BAR_COUNT)
                .Select(_ => (float)(rnd.NextDouble() * 0.7 + 0.3)).ToArray();
            _speeds = Enumerable.Range(0, BAR_COUNT)
                .Select(_ => (float)(rnd.NextDouble() * 1.5 + 0.8)).ToArray();
            _offsets = Enumerable.Range(0, BAR_COUNT)
                .Select(i => i * 0.22f).ToArray();
        }

        public void Advance(float deltaSeconds)
        {
            _time += deltaSeconds;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;
            float barWidth = 3f;
            float barGap = 3f;
            float totalWidth = BAR_COUNT * (barWidth + barGap) - barGap;
            float startX = (dirtyRect.Width - totalWidth) / 2f;
            float centerY = dirtyRect.Height / 2f;
            float maxH = dirtyRect.Height * 0.82f;

            for (int i = 0; i < BAR_COUNT; i++)
            {
                float x = startX + i * (barWidth + barGap);
                float h;

                if (IsActive)
                {
                    float wave = (float)Math.Sin(_time * _speeds[i] * 4f + _offsets[i]);
                    h = (wave * 0.5f + 0.5f) * _heights[i] * maxH + 3f;
                    canvas.FillColor = Color.FromRgba(0.078f, 0.722f, 0.651f, 0.75f + (wave * 0.25f));
                }
                else
                {
                    h = 3.5f;
                    canvas.FillColor = Color.FromArgb("#80EF4444");
                }

                float y = centerY - h / 2f;
                canvas.FillRoundedRectangle(x, y, barWidth, h, barWidth / 2f);
            }
        }
    }
}
