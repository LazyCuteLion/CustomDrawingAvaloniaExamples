#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Skia;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

/// <summary>
/// Demonstrates how to create an off-screen GPU-backed surface using <see cref="GRContext"/>
/// and composite the result into the control output.
/// </summary>
public sealed class SkiaGpuSurfaceControl : SkiaLeaseDrawableControl<SkiaGpuSurfaceControl.GpuSurfaceDrawable>
{
    public SkiaGpuSurfaceControl()
        : base(new GpuSurfaceDrawable())
    {
    }

    public sealed class GpuSurfaceDrawable : ISkiaLeaseDrawable
    {
        private const int TileSize = 256;
        private readonly SKColor[] _palette =
        [
            SKColor.FromHsv(205, 62, 96),
            SKColor.FromHsv(265, 58, 80),
            SKColor.FromHsv(320, 55, 88),
            SKColor.FromHsv(20, 70, 100)
        ];

        public bool RequiresAnimation => true;

        public void Render(SKCanvas canvas, Rect bounds, TimeSpan elapsed, ISkiaSharpApiLease lease)
        {
            canvas.Clear(new SKColor(8, 12, 24));

            var grContext = lease.GrContext;
            if (grContext != null)
            {
                using var surface = CreateOffscreenSurface(grContext, TileSize, TileSize);
                if (surface != null)
                {
                    DrawOffscreen(surface.Canvas, elapsed);
                    using var image = surface.Snapshot();
                    CompositeTiles(canvas, bounds, image);
                    DrawHud(canvas, bounds, lease, usingGpu: true);
                    return;
                }
            }

            DrawSoftwareFallback(canvas, bounds, elapsed);
            DrawHud(canvas, bounds, lease, usingGpu: false);
        }

        public void RenderFallback(ImmediateDrawingContext context, Rect bounds)
        {
            context.FillRectangle(
                new ImmutableSolidColorBrush(Color.FromRgb(30, 41, 59)),
                bounds);
        }

        private static SKSurface? CreateOffscreenSurface(GRContext grContext, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            return SKSurface.Create(grContext, true, info);
        }

        private void DrawOffscreen(SKCanvas canvas, TimeSpan elapsed)
        {
            canvas.Clear(new SKColor(13, 19, 35));

            using var paint = new SKPaint { IsAntialias = true };
            var center = new SKPoint(TileSize / 2f, TileSize / 2f);
            var angle = (float)(elapsed.TotalSeconds * 45.0);
            var radius = TileSize * 0.42f;

            for (var i = 0; i < _palette.Length; i++)
            {
                var sweep = 90f + 30f * MathF.Sin(angle * 0.5f + i);
                var start = angle + i * 90f;
                var localRadius = radius * (0.7f + 0.1f * i);

                paint.Color = _palette[i].WithAlpha((byte)(220 - i * 30));
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = TileSize * 0.06f;

                var rect = SKRect.Create(center.X - localRadius, center.Y - localRadius, localRadius * 2, localRadius * 2);
                canvas.DrawArc(rect, start, sweep, false, paint);
            }

            using var dotPaint = new SKPaint { IsAntialias = true };
            var orbitCount = 36;
            for (var i = 0; i < orbitCount; i++)
            {
                var t = i / (float)orbitCount;
                var orbitRadius = radius * (0.2f + t * 0.75f);
                var dotAngle = angle * 1.5f + t * MathF.PI * 4f;

                dotPaint.Color = _palette[i % _palette.Length].WithAlpha((byte)(200 - i * 4));
                var point = new SKPoint(
                    center.X + MathF.Cos(dotAngle) * orbitRadius,
                    center.Y + MathF.Sin(dotAngle * 0.9f) * orbitRadius);

                canvas.DrawCircle(point, TileSize * 0.02f + TileSize * 0.01f * MathF.Sin(angle + i), dotPaint);
            }
        }

        private static void CompositeTiles(SKCanvas canvas, Rect bounds, SKImage image)
        {
            var destRect = SKRect.Create((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
            using var tileShader = SKShader.CreateImage(image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

            using var paint = new SKPaint
            {
                Shader = tileShader,
                FilterQuality = SKFilterQuality.Medium
            };

            canvas.DrawRect(destRect, paint);
        }

        private static void DrawSoftwareFallback(SKCanvas canvas, Rect bounds, TimeSpan elapsed)
        {
            var rect = SKRect.Create((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
            using var paint = new SKPaint { IsAntialias = true };

            var hue = (float)(elapsed.TotalSeconds * 25.0 % 360.0);
            var center = new SKPoint(rect.MidX, rect.MidY);
            paint.Shader = SKShader.CreateRadialGradient(
                center,
                rect.Width * 0.6f,
                new[]
                {
                    SKColor.FromHsv(hue, 70f, 100f, 160),
                    SKColor.FromHsv((hue + 120f) % 360f, 70f, 90f, 140),
                    SKColor.FromHsv((hue + 240f) % 360f, 70f, 80f, 120)
                },
                null,
                SKShaderTileMode.Clamp);

            canvas.DrawRect(rect, paint);
        }

        private static void DrawHud(SKCanvas canvas, Rect bounds, ISkiaSharpApiLease lease, bool usingGpu)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(26, 35, 58, 210),
                Style = SKPaintStyle.Fill
            };

            var hudRect = SKRect.Create((float)bounds.Right - 260f, (float)bounds.Bottom - 90f, 240f, 70f);
            canvas.DrawRoundRect(hudRect, 16f, 16f, paint);

            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 18f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            };

            var lines = new[]
            {
                usingGpu ? "GPU surface active" : "Fallback surface",
                $"Opacity: {lease.CurrentOpacity:F2}",
                lease.GrContext != null ? $"Backend: {lease.GrContext.Backend}" : "GRContext: n/a"
            };

            var textX = hudRect.Left + 16f;
            var textY = hudRect.Top + 26f;

            foreach (var line in lines)
            {
                canvas.DrawText(line, textX, textY, textPaint);
                textY += 20f;
            }
        }
    }
}
