#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public sealed class CustomSkiaLeaseControl : SkiaLeaseDrawableControl<CustomSkiaLeaseControl.AnimatedSkiaLeaseDrawable>
{
    public CustomSkiaLeaseControl()
        : base(new AnimatedSkiaLeaseDrawable())
    {
    }

    public sealed class AnimatedSkiaLeaseDrawable : ISkiaLeaseDrawable
    {
        private readonly GlyphRun _fallbackGlyphRun;
        private readonly IImmutableGlyphRunReference? _fallbackGlyphReference;

        public AnimatedSkiaLeaseDrawable()
        {
            var message = "Skia lease unavailable";
            var glyphIndices = message.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
            _fallbackGlyphRun = new GlyphRun(Typeface.Default.GlyphTypeface, 16, message.AsMemory(), glyphIndices);
            _fallbackGlyphReference = _fallbackGlyphRun.TryCreateImmutableGlyphRunReference();
        }

        public bool RequiresAnimation => true;

        public void Render(SKCanvas canvas, Rect bounds, TimeSpan elapsed, ISkiaSharpApiLease lease)
        {
            canvas.Clear(new SKColor(12, 16, 32));

            var center = new SKPoint((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
            var radius = (float)(Math.Min(bounds.Width, bounds.Height) / 2);
            var time = (float)elapsed.TotalSeconds;

            DrawAnimatedGradient(canvas, center, radius, time);
            DrawOrbitingDot(canvas, center, radius, time);
        }

        public void RenderFallback(ImmediateDrawingContext context, Rect bounds)
        {
            context.FillRectangle(Brushes.DarkSlateGray, bounds);
            if (_fallbackGlyphReference != null)
            {
                context.DrawGlyphRun(Brushes.White, _fallbackGlyphReference);
            }
        }

        private static void DrawAnimatedGradient(SKCanvas canvas, SKPoint center, float radius, float time)
        {
            var hue1 = (time * 40f) % 360f;
            using var shader = SKShader.CreateSweepGradient(
                center,
                new[]
                {
                    SKColor.FromHsv(hue1, 70f, 100f, 200),
                    SKColor.FromHsv((hue1 + 120f) % 360f, 70f, 100f, 200),
                    SKColor.FromHsv((hue1 + 240f) % 360f, 70f, 100f, 200),
                    SKColor.FromHsv(hue1, 70f, 100f, 200)
                });

            using var paint = new SKPaint
            {
                Shader = shader,
                IsAntialias = true
            };

            canvas.DrawCircle(center, radius * 0.9f, paint);
        }

        private static void DrawOrbitingDot(SKCanvas canvas, SKPoint center, float radius, float time)
        {
            var orbitRadius = radius * 0.65f;
            var x = center.X + (float)Math.Cos(time) * orbitRadius;
            var y = center.Y + (float)Math.Sin(time * 1.35f) * orbitRadius;
            using var glowPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(90),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, radius * 0.1f)
            };
            using var dotPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            var dotCenter = new SKPoint(x, y);
            canvas.DrawCircle(dotCenter, radius * 0.2f, glowPaint);
            canvas.DrawCircle(dotCenter, radius * 0.08f, dotPaint);
        }
    }
}
