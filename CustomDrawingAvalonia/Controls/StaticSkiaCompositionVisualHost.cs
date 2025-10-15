#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public sealed class StaticSkiaCompositionVisualHost : SkiaCompositionVisualHostBase<StaticSkiaCompositionVisualHost.StaticCompositionDrawable>
{
    public StaticSkiaCompositionVisualHost()
        : base(new StaticCompositionDrawable())
    {
    }

    public void Refresh() => RequestManualRefresh();

    public sealed class StaticCompositionDrawable : ISkiaCompositionDrawable
    {
        private readonly Random _random = new();
        private readonly List<Ring> _rings = new();
        private readonly List<Dot> _dots = new();
        private SKColor _background = new(248, 250, 252);
        private float _baseHue;

        private readonly record struct Ring(float RadiusFactor, float Thickness, SKColor Color, float Start, float Sweep);
        private readonly record struct Dot(float RadiusFactor, float Angle, float SizeFactor, SKColor Color);

        public bool RequiresAnimation => false;

        public void Render(SKCanvas canvas, Size size, TimeSpan elapsed)
        {
            if (_rings.Count == 0)
            {
                GenerateRings(size);
            }

            canvas.Clear(_background);

            var center = new SKPoint((float)size.Width / 2f, (float)size.Height / 2f);
            var maxRadius = (float)Math.Min(size.Width, size.Height) / 2f;

            using var ringPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round
            };

            foreach (var ring in _rings)
            {
                ringPaint.Color = ring.Color;
                ringPaint.StrokeWidth = ring.Thickness * maxRadius;
                var radius = ring.RadiusFactor * maxRadius;
                var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
                canvas.DrawArc(rect, ring.Start, ring.Sweep, false, ringPaint);
            }

            using var fillPaint = new SKPaint { IsAntialias = true };
            foreach (var dot in _dots)
            {
                var radius = dot.RadiusFactor * maxRadius;
                var point = new SKPoint(
                    center.X + MathF.Cos(dot.Angle) * radius,
                    center.Y + MathF.Sin(dot.Angle) * radius);

                fillPaint.Color = dot.Color;
                canvas.DrawCircle(point, Math.Max(1f, dot.SizeFactor * maxRadius), fillPaint);
            }
        }

        public void RenderFallback(ImmediateDrawingContext context, Rect bounds)
        {
            var fallbackColor = Color.FromRgb(_background.Red, _background.Green, _background.Blue);
            context.FillRectangle(new ImmutableSolidColorBrush(fallbackColor), bounds);
        }

        public void OnRefreshRequested()
        {
            _rings.Clear();
            _dots.Clear();
        }

        private void GenerateRings(Size size)
        {
            var ringCount = _random.Next(3, 6);
            _rings.Clear();
            _dots.Clear();

            _baseHue = (float)(_random.NextDouble() * 360.0);
            _background = SKColor.FromHsv((_baseHue + 210f) % 360f, 18f, 98f);

            for (var i = 0; i < ringCount; i++)
            {
                var radiusFactor = (float)(0.2 + i * 0.15 + _random.NextDouble() * 0.1);
                radiusFactor = Math.Clamp(radiusFactor, 0.15f, 0.95f);
                var thickness = (float)(0.02 + _random.NextDouble() * 0.04);
                var start = (float)(_random.NextDouble() * 360.0);
                var sweep = (float)(120 + _random.NextDouble() * 180.0);
                var color = SKColor.FromHsv((_baseHue + i * 25f) % 360f, 65f, 95f, 230);
                _rings.Add(new Ring(radiusFactor, thickness, color, start, sweep));
            }

            var dotCount = ringCount * 5 + 12;
            for (var i = 0; i < dotCount; i++)
            {
                var radiusFactor = (float)(0.15 + _random.NextDouble() * 0.8);
                radiusFactor = Math.Clamp(radiusFactor, 0.1f, 0.95f);
                var angle = (float)(_random.NextDouble() * MathF.PI * 2);
                var sizeFactor = (float)(0.015 + _random.NextDouble() * 0.045);
                var color = SKColor.FromHsv((_baseHue + 90f + i * 7f) % 360f, 55f, 100f, 200);
                _dots.Add(new Dot(radiusFactor, angle, sizeFactor, color));
            }
        }
    }
}
