#nullable enable
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public abstract class SkiaLeaseDrawableControl<TDrawable> : Control
    where TDrawable : ISkiaLeaseDrawable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private bool _isActive;

    protected SkiaLeaseDrawableControl(TDrawable drawable)
    {
        Drawable = drawable;
        ClipToBounds = true;
    }

    protected TDrawable Drawable { get; }

    protected TimeSpan Elapsed => _stopwatch.Elapsed;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isActive = true;
        if (Drawable.RequiresAnimation)
        {
            ScheduleNextFrame();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isActive = false;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        context.Custom(new SkiaLeaseDrawOperation(bounds, Drawable, Elapsed));

        if (_isActive && Drawable.RequiresAnimation)
        {
            ScheduleNextFrame();
        }
    }

    protected void RequestRedraw() => InvalidateVisual();

    private void ScheduleNextFrame()
    {
        if (!_isActive)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.RequestAnimationFrame(_ =>
            {
                if (_isActive)
                {
                    InvalidateVisual();
                }
            });
        }
    }

    private sealed class SkiaLeaseDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly TDrawable _drawable;
        private readonly TimeSpan _elapsed;

        public SkiaLeaseDrawOperation(Rect bounds, TDrawable drawable, TimeSpan elapsed)
        {
            _bounds = bounds;
            _drawable = drawable;
            _elapsed = elapsed;
        }

        public void Dispose()
        {
        }

        public Rect Bounds => _bounds;

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null)
            {
                _drawable.RenderFallback(context, _bounds);
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            if (canvas == null)
            {
                _drawable.RenderFallback(context, _bounds);
                return;
            }

            var clip = new SKRect((float)_bounds.X, (float)_bounds.Y, (float)_bounds.Right, (float)_bounds.Bottom);
            canvas.Save();
            canvas.ClipRect(clip);
            _drawable.Render(canvas, _bounds, _elapsed, lease);
            canvas.Restore();
        }
    }
}
