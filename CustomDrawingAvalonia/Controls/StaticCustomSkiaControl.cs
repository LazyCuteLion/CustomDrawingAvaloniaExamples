#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Skia;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public sealed class StaticCustomSkiaControl : SkiaLeaseDrawableControl<StaticCustomSkiaControl.StaticSkiaDrawable>
{
    public StaticCustomSkiaControl()
        : base(new StaticSkiaDrawable())
    {
    }

    public void Regenerate()
    {
        Drawable.Regenerate();
        RequestRedraw();
    }

    public sealed class StaticSkiaDrawable : ISkiaLeaseDrawable
    {
        private readonly Random _random = new();
        private float _hueOffset;
        private float _scatter;

        public StaticSkiaDrawable()
        {
            Regenerate();
        }

        public bool RequiresAnimation => false;

        public void Regenerate()
        {
            _hueOffset = (float)(_random.NextDouble() * 360.0);
            _scatter = (float)_random.NextDouble();
        }

        public void Render(SKCanvas canvas, Rect bounds, TimeSpan elapsed, ISkiaSharpApiLease lease)
        {
            var clip = new SKRect((float)bounds.X, (float)bounds.Y, (float)bounds.Right, (float)bounds.Bottom);
            canvas.Clear(new SKColor(248, 250, 252));

            var center = new SKPoint(clip.MidX, clip.MidY);
            var radius = Math.Min(clip.Width, clip.Height) / 2f;

            using (var backgroundShader = SKShader.CreateRadialGradient(
                       center,
                       radius,
                       new[]
                       {
                           SKColor.FromHsv((_hueOffset + 180f) % 360f, 35f, 98f),
                           SKColor.FromHsv((_hueOffset + 200f) % 360f, 45f, 80f),
                           SKColor.FromHsv((_hueOffset + 260f) % 360f, 55f, 75f)
                       },
                       null,
                       SKShaderTileMode.Clamp))
            using (var paint = new SKPaint { Shader = backgroundShader })
            {
                canvas.DrawRect(clip, paint);
            }

            using var shapePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(2f, radius * 0.03f),
                StrokeCap = SKStrokeCap.Round
            };

            var layerCount = 5;
            var denominator = Math.Max(1, layerCount - 1);
            for (var i = 0; i < layerCount; i++)
            {
                var t = i / (float)denominator;
                var hue = (_hueOffset + t * 120f) % 360f;
                shapePaint.Color = SKColor.FromHsv(hue, 70f, 95f, (byte)(220 - i * 25));
                var sweep = 240f + (float)Math.Sin((_scatter + i * 0.37f) * Math.PI * 2) * 60f;
                var start = (_scatter * 360f + i * 27f) % 360f;
                var r = radius * (0.25f + t * 0.65f);

                var rect = new SKRect(center.X - r, center.Y - r, center.X + r, center.Y + r);
                canvas.DrawArc(rect, start, sweep, false, shapePaint);
            }

            using var dotPaint = new SKPaint { IsAntialias = true };
            var dotCount = 24;
            for (var i = 0; i < dotCount; i++)
            {
                var angle = (float)(Math.PI * 2 * i / dotCount + _scatter * Math.PI * 4);
                var distance = radius * (0.1f + (float)i / dotCount * 0.8f);
                var point = new SKPoint(
                    center.X + (float)Math.Cos(angle) * distance,
                    center.Y + (float)Math.Sin(angle) * distance);

                dotPaint.Color = SKColor.FromHsv((_hueOffset + i * 7f) % 360f, 60f, 100f, 180);
                canvas.DrawCircle(point, radius * 0.04f, dotPaint);
            }
        }

        public void RenderFallback(ImmediateDrawingContext context, Rect bounds)
        {
            context.FillRectangle(new ImmutableSolidColorBrush(Color.FromRgb(226, 232, 240)), bounds);
        }
    }
}
