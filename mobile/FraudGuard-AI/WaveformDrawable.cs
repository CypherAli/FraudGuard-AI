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

        // ── Real PCM audio state ──────────────────────────────────────────
        // Normalised [0..1] amplitude per bar from actual microphone PCM data.
        // Smoothed toward the raw target values each Advance() tick for a
        // natural-looking decay rather than instant jump-to-zero.
        private readonly float[] _pcmLevels    = new float[BAR_COUNT];
        private readonly float[] _pcmTargets   = new float[BAR_COUNT];
        // volatile: written from audio thread, read from UI thread — ensures visibility without lock
        private volatile bool _hasPcmData = false;
        private const float SMOOTH_RISE  = 0.6f;  // fast rise on loud signal
        private const float SMOOTH_DECAY = 0.12f; // slow decay for smooth visual

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

        /// <summary>
        /// Feed raw PCM-16 audio bytes so the waveform reflects actual caller audio.
        /// Thread-safe: may be called from the audio thread.
        /// </summary>
        public void UpdateFromPcm(byte[] data, int length)
        {
            if (data == null || length < 2) return;

            int totalSamples = length / 2; // 16-bit = 2 bytes per sample
            int samplesPerBar = Math.Max(1, totalSamples / BAR_COUNT);

            for (int bar = 0; bar < BAR_COUNT; bar++)
            {
                int startSample = bar * samplesPerBar;
                int endSample   = Math.Min(startSample + samplesPerBar, totalSamples);
                long sum = 0;
                for (int s = startSample; s < endSample; s++)
                {
                    int idx = s * 2;
                    if (idx + 1 < length)
                    {
                        short sample = (short)(data[idx] | (data[idx + 1] << 8));
                        sum += Math.Abs(sample);
                    }
                }
                float avg = endSample > startSample ? (float)(sum / (endSample - startSample)) : 0f;
                // Normalise: 16-bit max is 32768
                _pcmTargets[bar] = Math.Min(1f, avg / 32768f);
            }

            _hasPcmData = true;
        }

        public void Advance(float deltaSeconds)
        {
            _time += deltaSeconds;

            if (_hasPcmData)
            {
                // Smooth each bar toward its target
                for (int i = 0; i < BAR_COUNT; i++)
                {
                    float target = _pcmTargets[i];
                    float current = _pcmLevels[i];
                    float alpha = target > current ? SMOOTH_RISE : SMOOTH_DECAY;
                    _pcmLevels[i] = current + (target - current) * alpha;
                }
            }
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
                    if (_hasPcmData)
                    {
                        // Real PCM: actual caller audio amplitude
                        float level = _pcmLevels[i];
                        h = (level * 0.88f + 0.05f) * maxH; // min ~5% so bars always visible
                        float alpha = 0.45f + level * 0.55f;
                        canvas.FillColor = Color.FromRgba(0.078f, 0.722f, 0.651f, alpha);
                    }
                    else
                    {
                        // Fallback animated sine wave until first PCM chunk arrives
                        float wave = (float)Math.Sin(_time * _speeds[i] * 4f + _offsets[i]);
                        h = (wave * 0.5f + 0.5f) * _heights[i] * maxH + 3f;
                        canvas.FillColor = Color.FromRgba(0.078f, 0.722f, 0.651f, 0.75f + (wave * 0.25f));
                    }
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
