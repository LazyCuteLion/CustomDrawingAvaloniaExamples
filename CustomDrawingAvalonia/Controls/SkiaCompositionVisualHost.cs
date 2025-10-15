#nullable enable
using Avalonia;
using Avalonia.Media;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public sealed class SkiaCompositionVisualHost : SkiaCompositionVisualHostBase<SkiaCompositionVisualHost.AnimatedCompositionDrawable>
{
    public SkiaCompositionVisualHost()
        : base(new AnimatedCompositionDrawable())
    {
    }

    public sealed class AnimatedCompositionDrawable : ISkiaCompositionDrawable
    {
        public bool RequiresAnimation => true;

        public void Render(SKCanvas canvas, Size size, TimeSpan elapsed)
        {
            canvas.Clear(new SKColor(6, 10, 24));

            var angle = (float)(elapsed.TotalSeconds * 90.0 % 360.0);
            var center = new SKPoint((float)size.Width / 2f, (float)size.Height / 2f);
            var maxRadius = (float)Math.Min(size.Width, size.Height) / 2.2f;

            DrawConcentricArcs(canvas, center, maxRadius, angle);
            DrawRotatingGrid(canvas, size, angle);
        }

        public void RenderFallback(ImmediateDrawingContext context, Rect bounds)
        {
            context.FillRectangle(Brushes.Black, bounds);
        }

        public void OnRefreshRequested()
        {
        }

        private static void DrawConcentricArcs(SKCanvas canvas, SKPoint center, float maxRadius, float angle)
        {
            const int RingCount = 4;
            using var strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };

            for (var i = 0; i < RingCount; i++)
            {
                var progress = i / (float)RingCount;
                var radius = maxRadius * (1f - progress * 0.2f);
                var hue = (angle + i * 45f) % 360f;
                strokePaint.Color = SKColor.FromHsv(hue, 70f, 100f, (byte)(220 - i * 40));
                strokePaint.StrokeWidth = maxRadius * 0.06f * (1.1f - progress);

                using var path = new SKPath();
                var sweep = 270 + MathF.Sin((angle + i * 15f) * ((float)Math.PI / 180f)) * 45f;
                path.AddArc(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius),
                    angle + i * 10f, sweep);
                canvas.DrawPath(path, strokePaint);
            }
        }

        private static void DrawRotatingGrid(SKCanvas canvas, Size bounds, float angle)
        {
            using var gridPaint = new SKPaint
            {
                Color = SKColor.FromHsv((angle + 180f) % 360f, 50f, 40f, 80),
                IsAntialias = true,
                StrokeWidth = 1.5f
            };

            var matrix = SKMatrix.CreateRotationDegrees(angle, (float)bounds.Width / 2f, (float)bounds.Height / 2f);
            canvas.Save();
            canvas.SetMatrix(matrix);

            var spacing = Math.Max((float)bounds.Width, (float)bounds.Height) / 12f;
            for (var x = -spacing * 6f; x < bounds.Width + spacing * 6f; x += spacing)
            {
                canvas.DrawLine(x, (float)(-bounds.Height), x, (float)bounds.Height * 2f, gridPaint);
            }
            for (var y = -spacing * 6f; y < bounds.Height + spacing * 6f; y += spacing)
            {
                canvas.DrawLine((float)(-bounds.Width), y, (float)bounds.Width * 2f, y, gridPaint);
            }

            canvas.Restore();
        }
    }
}
