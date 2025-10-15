#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace CustomDrawingAvalonia.Rendering;

public interface ISkiaLeaseDrawable
{
    bool RequiresAnimation { get; }
    void Render(SKCanvas canvas, Rect bounds, TimeSpan elapsed, ISkiaSharpApiLease lease);
    void RenderFallback(ImmediateDrawingContext context, Rect bounds);
}

public interface ISkiaCompositionDrawable
{
    bool RequiresAnimation { get; }
    void Render(SKCanvas canvas, Size size, TimeSpan elapsed);
    void RenderFallback(ImmediateDrawingContext context, Rect bounds);
    void OnRefreshRequested();
}
