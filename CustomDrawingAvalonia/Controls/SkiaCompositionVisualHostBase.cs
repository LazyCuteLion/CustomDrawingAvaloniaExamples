#nullable enable
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using CustomDrawingAvalonia.Rendering;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls;

public abstract class SkiaCompositionVisualHostBase<TDrawable> : Control
    where TDrawable : ISkiaCompositionDrawable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private CompositionCustomVisual? _visual;
    private Handler? _handler;

    protected SkiaCompositionVisualHostBase(TDrawable drawable)
    {
        Drawable = drawable;
        ClipToBounds = true;
    }

    protected TDrawable Drawable { get; }

    protected TimeSpan Elapsed => _stopwatch.Elapsed;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureVisual();
        if (Drawable.RequiresAnimation)
        {
            _visual?.SendHandlerMessage(Handler.StartMessage);
        }
        else
        {
            _visual?.SendHandlerMessage(Handler.RefreshMessage);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _visual?.SendHandlerMessage(Handler.StopMessage);
        ElementComposition.SetElementChildVisual(this, null);
        _visual = null;
        _handler = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        if (_visual != null)
        {
            _visual.Size = new Vector(arranged.Width, arranged.Height);
        }

        return arranged;
    }

    protected void RequestManualRefresh()
    {
        EnsureVisual();
        _visual?.SendHandlerMessage(Handler.RefreshMessage);
    }

    private void EnsureVisual()
    {
        var visual = ElementComposition.GetElementVisual(this);
        if (visual == null)
        {
            return;
        }

        if (_visual != null && _visual.Compositor == visual.Compositor)
        {
            return;
        }

        _handler = new Handler(this);
        _visual = visual.Compositor.CreateCustomVisual(_handler);
        _visual.Size = new Vector(Bounds.Width, Bounds.Height);
        ElementComposition.SetElementChildVisual(this, _visual);
    }

    private sealed class Handler : CompositionCustomVisualHandler
    {
        public static readonly object StartMessage = new();
        public static readonly object StopMessage = new();
        public static readonly object RefreshMessage = new();

        private readonly SkiaCompositionVisualHostBase<TDrawable> _owner;
        private bool _running;

        public Handler(SkiaCompositionVisualHostBase<TDrawable> owner)
        {
            _owner = owner;
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            var bounds = GetRenderBounds();
            var leaseFeature = drawingContext.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null)
            {
                _owner.Drawable.RenderFallback(drawingContext, bounds);
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            if (canvas == null)
            {
                _owner.Drawable.RenderFallback(drawingContext, bounds);
                return;
            }

            canvas.Save();
            var clip = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
            canvas.ClipRect(clip);
            _owner.Drawable.Render(canvas, bounds.Size, _owner.Elapsed);
            canvas.Restore();
        }

        public override void OnAnimationFrameUpdate()
        {
            if (!_running)
            {
                return;
            }

            Invalidate();
            RegisterForNextAnimationFrameUpdate();
        }

        public override void OnMessage(object message)
        {
            if (message == StartMessage)
            {
                _running = true;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == StopMessage)
            {
                _running = false;
            }
            else if (message == RefreshMessage)
            {
                _owner.Drawable.OnRefreshRequested();
                Invalidate();
                if (_owner.Drawable.RequiresAnimation && !_running)
                {
                    _running = true;
                    RegisterForNextAnimationFrameUpdate();
                }
            }
        }
    }
}
