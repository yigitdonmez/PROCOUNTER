using Microsoft.Maui.Graphics;

namespace CalorieTracker.Mobile;

public class EqualizerDrawable : IDrawable
{
    private const int BarCount = 3;
    private const float Gap = 8f;

    private readonly float[] _current = new float[BarCount];
    private readonly float[] _from = new float[BarCount];
    private readonly float[] _to = new float[BarCount];
    private readonly float[] _progress = new float[BarCount];
    private readonly float[] _duration = new float[BarCount];
    private readonly float[] _pause = new float[BarCount];
    private readonly Random _rng = new();

    private LinearGradientPaint? _paint;
    private Color _barColor = Colors.Transparent;

    public double Level { get; set; }

    public Color BarColor
    {
        get => _barColor;
        set
        {
            _barColor = value;
            _paint = new LinearGradientPaint
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(0, 0),
                GradientStops = new[]
                {
                    new PaintGradientStop(0f, value),
                    new PaintGradientStop(1f, value.WithAlpha(0.25f))
                }
            };
        }
    }

    public EqualizerDrawable()
    {
        for (int i = 0; i < BarCount; i++)
        {
            _progress[i] = 1f;
            _duration[i] = 0.3f;
            _pause[i] = (float)(_rng.NextDouble() * 0.5);
        }
    }

    public void Tick(float dt)
    {
        for (int i = 0; i < BarCount; i++)
        {
            if (_pause[i] > 0f)
            {
                _pause[i] -= dt;
                continue;
            }

            _progress[i] += dt / _duration[i];

            if (_progress[i] >= 1f)
            {
                _current[i] = _to[i];
                _from[i] = _to[i];
                _to[i] = (float)(Level * (0.1 + _rng.NextDouble() * 0.9));
                _duration[i] = 0.2f + (float)(_rng.NextDouble() * 0.45);
                _pause[i] = (float)(_rng.NextDouble() * 0.12);
                _progress[i] = 0f;
            }
            else
            {
                float eased = 0.5f - 0.5f * MathF.Cos(MathF.PI * _progress[i]);
                _current[i] = _from[i] + (_to[i] - _from[i]) * eased;
            }
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_paint == null) return;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        float barWidth = (width - Gap * (BarCount - 1)) / BarCount;

        canvas.SetFillPaint(_paint, new RectF(0, 0, width, height));

        for (int i = 0; i < BarCount; i++)
        {
            float h = Math.Max(1f, _current[i] * height);
            float x = i * (barWidth + Gap);
            canvas.FillRoundedRectangle(x, height - h, barWidth, h, 5, 5, 0, 0);
        }
    }
}