# CustomDrawingAvaloniaExamples

CustomDrawingAvaloniaExamples is a .NET 9 desktop solution that demonstrates advanced custom drawing techniques in Avalonia using SkiaSharp. The sample application highlights two complementary rendering approaches:

- **Skia API Lease pipeline** – Integrates SkiaSharp drawing directly into Avalonia’s main render pass via `ICustomDrawOperation`.
- **Composition Custom Visual pipeline** – Hosts Skia rendering inside Avalonia’s compositor using `CompositionCustomVisualHandler`.

The project is intended for engineers who are extending Avalonia’s rendering capabilities or prototyping rich, animated visuals with SkiaSharp.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Project Layout](#project-layout)
- [Getting Started](#getting-started)
- [Scenario Walkthroughs](#scenario-walkthroughs)
  - [Dynamic Custom Draw (ICustomDrawOperation)](#dynamic-custom-draw-icustomdrawoperation)
  - [GPU Surface Tiling (GRContext)](#gpu-surface-tiling-grcontext)
  - [Static Custom Draw](#static-custom-draw)
  - [Dynamic Composition Visual](#dynamic-composition-visual)
  - [Static Composition Visual](#static-composition-visual)
- [Skia API Lease Deep Dive](#skia-api-lease-deep-dive)
- [Composition Custom Visual Deep Dive](#composition-custom-visual-deep-dive)
- [API Reference Summary](#api-reference-summary)
- [Extending the Sample](#extending-the-sample)
- [Troubleshooting](#troubleshooting)
- [Performance Checklist](#performance-checklist)
- [Additional Resources](#additional-resources)

## Overview

Avalonia offers multiple integration points for SkiaSharp rendering. This repository provides reusable base types and realistic visuals that illustrate how to:

- Acquire and safely release Skia canvases during Avalonia render passes.
- Create GPU-backed `SKSurface` instances via `GRContext` and composite them into the scene.
- Schedule animations using both the main UI loop and the compositor’s animation system.
- Provide deterministic fallback rendering when Skia resources are unavailable.
- Structure custom controls so they play nicely with Avalonia layout, hit-testing, and styling.

Each sample view is fully encapsulated in a control so you can copy the code into your own project with minimal changes.

## Prerequisites

To build and run the sample you need:

- [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later.
- A desktop OS supported by Avalonia Desktop (Windows, macOS, or Linux).
- A modern IDE or editor with C# support (Visual Studio 2022, JetBrains Rider, or VS Code with the C# extension).
- Basic familiarity with Avalonia layout and XAML.

No additional native dependencies are required; all packages are restored from NuGet.

## Project Layout

```
CustomDrawingAvaloniaExamples.sln
├── CustomDrawingAvaloniaExamples/              # Desktop host application
│   ├── App.axaml / App.axaml.cs                # Application bootstrap
│   ├── Program.cs                              # Entry point
│   └── Views/
│       └── MainWindow.axaml(.cs)               # Sample host with four scenarios
├── CustomDrawingAvalonia/                      # Reusable controls library
│   ├── CustomDrawingAvalonia.csproj
│   ├── Controls/
│   │   ├── SkiaLeaseDrawableControl.cs         # Base control for API lease rendering
│   │   ├── CustomSkiaLeaseControl.cs           # Animated lease-driven sample
│   │   ├── StaticCustomSkiaControl.cs          # Static lease-driven sample
│   │   ├── SkiaGpuSurfaceControl.cs            # GRContext / GPU tiling sample
│   │   ├── SkiaCompositionVisualHostBase.cs    # Base control for composition visuals
│   │   ├── SkiaCompositionVisualHost.cs        # Animated composition sample
│   │   └── StaticSkiaCompositionVisualHost.cs  # Refreshable composition sample
│   └── Rendering/
│       └── SkiaRenderingContracts.cs           # Shared drawable interfaces
└── global.json                                 # Pins SDK version (optional)
```

The desktop project consumes the reusable controls from the `CustomDrawingAvalonia` class library.

## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/wieslawsoltes/CustomDrawingAvaloniaExamples.git
   cd CustomDrawingAvaloniaExamples
   ```

2. **Restore packages and build**
   ```bash
   dotnet build CustomDrawingAvaloniaExamples.sln
   ```

3. **Run the sample application**
   ```bash
   dotnet run --project CustomDrawingAvaloniaExamples/CustomDrawingAvaloniaExamples.csproj
   ```

4. **Explore the scenarios**
   - Each tab in the main window focuses on a specific integration technique.
   - Use the “Regenerate” buttons in the static samples to trigger manual refresh logic.

## Scenario Walkthroughs

### Dynamic Custom Draw (ICustomDrawOperation)

**File:** `CustomDrawingAvalonia/Controls/CustomSkiaLeaseControl.cs`

This scenario renders an animated gradient “portal” and orbiting particles using Skia shaders and blur effects.

- Control inheritance: `CustomSkiaLeaseControl : SkiaLeaseDrawableControl<AnimatedSkiaLeaseDrawable>`.
- Animation cadence: `TopLevel.RequestAnimationFrame` schedules frames when the control is attached and the drawable requests animation.
- Fallback behavior: Glyph-based message rendered with Avalonia primitives when the Skia lease is unavailable.
- Key API usage: `ISkiaSharpApiLeaseFeature.Lease()`, `SKShader.CreateSweepGradient`, `SKMaskFilter.CreateBlur`.

### GPU Surface Tiling (GRContext)

**File:** `CustomDrawingAvalonia/Controls/SkiaGpuSurfaceControl.cs`

Highlights how to obtain the leased `GRContext` and create a GPU-backed `SKSurface` for off-screen rendering.

- Control inheritance: `SkiaGpuSurfaceControl : SkiaLeaseDrawableControl<GpuSurfaceDrawable>`.
- Surface creation: Calls `SKSurface.Create(grContext, true, SKImageInfo)` once the lease provides a GPU context.
- Composition: Renders animated arcs and particles into the off-screen surface, snapshots it, then tiles the image into the control output with a repeating shader.
- HUD overlay: Reads `lease.CurrentOpacity` and the `GRContext` identifier to confirm GPU usage at runtime.
- Fallback behavior: Draws a software radial gradient when the lease does not expose a `GRContext`.

### Static Custom Draw

**File:** `CustomDrawingAvalonia/Controls/StaticCustomSkiaControl.cs`

Displays deterministic rings and accent dots generated from pseudo-random seeds.

- Manual refresh: `StaticCustomSkiaControl.Regenerate()` updates internal state and calls `RequestRedraw()` on the base control.
- Rendering: `SKCanvas.DrawArc` and gradient shaders create the layered visuals.
- Fallback: Uses Avalonia immutable brushes to match the Skia colour palette.

### Dynamic Composition Visual

**File:** `CustomDrawingAvalonia/Controls/SkiaCompositionVisualHost.cs`

Showcases composition-hosted animation with concentric arcs and a rotating grid.

- Control inheritance: `SkiaCompositionVisualHost : SkiaCompositionVisualHostBase<AnimatedCompositionDrawable>`.
- Animation cadence: `CompositionCustomVisualHandler.RegisterForNextAnimationFrameUpdate` continuously schedules frames while the drawable is active.
- Composition integration: The handler pushes a custom visual into the compositor graph via `ElementComposition.SetElementChildVisual`.
- Fallback: Clears the background with Avalonia brushes if no Skia lease is provided.

### Static Composition Visual

**File:** `CustomDrawingAvalonia/Controls/StaticSkiaCompositionVisualHost.cs`

Generates ring and dot layouts on demand; ideal for data-driven visuals that update sporadically.

- Refresh flow: `StaticSkiaCompositionVisualHost.Refresh()` calls `RequestManualRefresh()` on the base class, which sends a message to the handler.
- Drawable behavior: `OnRefreshRequested()` regenerates ring and dot collections before the next render call.
- Composition benefits: Rendering is offloaded to the compositor, reducing pressure on the main UI thread.

## Skia API Lease Deep Dive

The Skia API lease pipeline lets you render with SkiaSharp during Avalonia’s main render pass without managing Skia surfaces yourself.

1. **Custom draw operation registration**
   ```csharp
   context.Custom(new SkiaLeaseDrawOperation(bounds, Drawable, Elapsed));
   ```

2. **Lease acquisition**
   ```csharp
   var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
   using var lease = leaseFeature?.Lease();
   var canvas = lease?.SkCanvas;
   ```

3. **Rendering flow**
   - If the lease or canvas is null, the drawable’s `RenderFallback` is invoked.
   - Otherwise, the drawable receives the shared `SKCanvas` *and* the `ISkiaSharpApiLease` instance, clipped to the control bounds.
   - From the lease you can access the `GRContext`, `CurrentOpacity`, and optionally lease platform graphics APIs.
   - `RequiresAnimation` determines whether the control schedules additional frames.

4. **Animation scheduling**
   ```csharp
   TopLevel?.RequestAnimationFrame(_ => InvalidateVisual());
   ```
   The base control throttles requests to avoid invalidating while detached from the visual tree.

5. **Best practices**
   - Always clip to the control bounds to respect layout constraints.
   - Keep rendering stateless or store state in the drawable; avoid retaining the leased canvas.
   - Provide high-quality fallbacks for headless or software-only scenarios.
   - Dispose any temporary `SKSurface`, `SKImage`, or `SKShader` instances each frame to prevent GPU resource leaks.

### Creating GPU Surfaces with `GRContext`

When Avalonia renders using a GPU-backed Skia backend, the lease provides a populated `GRContext`. You can use it to create custom render targets or textures:

```csharp
var grContext = lease.GrContext;
if (grContext != null)
{
    var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var surface = SKSurface.Create(grContext, true, info);
    DrawToSurface(surface.Canvas);
    using var image = surface.Snapshot();
    canvas.DrawImage(image, destRect);
}
```

`SkiaGpuSurfaceControl` contains a complete example that renders animated content into the off-screen surface, then tiles the resulting texture across the control output.

## Composition Custom Visual Deep Dive

Composition custom visuals leverage Avalonia’s composition engine for high-performance drawing, especially when combined with other compositor features.

1. **Visual creation**
   ```csharp
   _visual = visual.Compositor.CreateCustomVisual(_handler);
   ElementComposition.SetElementChildVisual(this, _visual);
   ```

2. **Handler responsibilities**
   - `OnRender`: Attempts to lease an `SKCanvas` and delegates to the drawable.
   - `OnAnimationFrameUpdate`: Invalidates the visual and re-registers for animation updates when `_running` is `true`.
   - `OnMessage`: Reacts to start, stop, and refresh messages, enabling fine-grained control over animation lifecycle.

3. **Manual refresh pattern**
   ```csharp
   _visual?.SendHandlerMessage(Handler.RefreshMessage);
   ```
   Drawables can regenerate state in `OnRefreshRequested()` before the next render call executes.

4. **Advantages**
   - Rendering occurs on the composition thread, freeing the main UI thread.
   - Visuals can be combined with compositor effects, transforms, and animations.
   - Suitable for dashboards or visualizations that require smooth, persistent animation.

5. **Considerations**
   - Composition visuals do not participate in Avalonia hit-testing by default; add overlay elements if interaction is needed.
   - Always clean up child visuals in `OnDetachedFromVisualTree` to avoid orphaned compositor resources.

## API Reference Summary

The sample exercises several Avalonia rendering APIs. Use the following overview to locate deeper documentation and understand how each component fits within the rendering pipeline.

| API | Location in Sample | Description | Key Members |
| --- | --- | --- | --- |
| `ISkiaSharpApiLeaseFeature` | `CustomDrawingAvalonia/Controls/SkiaLeaseDrawableControl.cs`, `CustomDrawingAvalonia/Controls/SkiaCompositionVisualHostBase.cs` | Provides access to a leased SkiaSharp drawing context during an Avalonia render pass. Defined in `Avalonia.Skia`. | `Lease()` → `ISkiaSharpApiLease`. |
| `ISkiaSharpApiLease` | `CustomDrawingAvalonia/Controls/CustomSkiaLeaseControl.cs`, `CustomDrawingAvalonia/Controls/SkiaGpuSurfaceControl.cs`, `CustomDrawingAvalonia/Controls/StaticCustomSkiaControl.cs` | Represents the scoped lease returned by the feature; exposes GPU resources when available. | `SkCanvas`, `SKSurface`, `GRContext`, `CurrentOpacity`, `TryLeasePlatformGraphicsApi()`. |
| `ICustomDrawOperation` | Nested `SkiaLeaseDrawOperation` in `CustomDrawingAvalonia/Controls/SkiaLeaseDrawableControl.cs` | Allows custom drawing using immediate mode rendering. Avalonia invokes `Render(ImmediateDrawingContext)` for each draw pass. | `Bounds`, `Render(...)`, `HitTest(...)`, `Dispose()`. |
| `ImmediateDrawingContext` | Passed to drawable `RenderFallback` methods and `CompositionCustomVisualHandler.OnRender` | Encapsulates the current render state. Enables feature discovery via `TryGetFeature`, clipping, and issuing Avalonia drawing commands. | `TryGetFeature(Type)`, `FillRectangle(...)`, `DrawGlyphRun(...)`, `PushClip(...)`. |
| `TopLevel.RequestAnimationFrame` | `SkiaLeaseDrawableControl.ScheduleNextFrame` | Schedules work on Avalonia’s main render loop when animation is required. | Accepts callback with frame timestamp; only valid when control is attached to a `TopLevel`. |
| `CompositionCustomVisual` / `CompositionCustomVisualHandler` | `SkiaCompositionVisualHostBase.cs` | Hosts custom drawing inside Avalonia’s compositor. The handler orchestrates render callbacks, animation frame updates, and message passing. | `OnRender(...)`, `OnAnimationFrameUpdate()`, `OnMessage(object)`, `RegisterForNextAnimationFrameUpdate()`, `Invalidate()`, `GetRenderBounds()`. |
| `ElementComposition.SetElementChildVisual` | `SkiaCompositionVisualHostBase.EnsureVisual` | Attaches a composition visual to an Avalonia control. Required so the control participates in the compositor graph. | Accepts `IVisual` owner and `CompositionVisual` child. |
| `ImmediateDrawingContext.TryGetFeature<T>` | Both base classes | Generic helper to retrieve rendering features, including `ISkiaSharpApiLeaseFeature`. | Returns `bool` for availability and out parameter for the feature instance. |
| `ISkiaSharpApiLeaseFeature` implementation | Avalonia source: `/src/Skia/Avalonia.Skia/DrawingContextImpl.cs` | Created per render pass; the lease ensures SkiaSharp resources are released after drawing completes. Access to `GRContext` enables advanced GPU operations when needed. | `Lease()` returns an object implementing `IDisposable` — always dispose in a `using` block. |
| `CompositionCustomVisualHandler` implementation | Avalonia source: `/src/Avalonia.Base/Rendering/Composition/CompositionCustomVisualHandler.cs` | Manages server-side compositor integration, including render bounds and animation scheduling. | `EffectiveSize`, `CompositionNow`, `RenderClipContains(Point)`, `RegisterForNextAnimationFrameUpdate()`. |

> For the complete interface and class definitions, review the Avalonia source code in `/Users/wieslawsoltes/GitHub/Avalonia` or refer to the generated API reference when available.

## Extending the Sample

1. **Choose a pipeline**
   - Use `SkiaLeaseDrawableControl<T>` for controls that should render within the main render pass and interact with Avalonia layout directly.
   - Use `SkiaCompositionVisualHostBase<T>` when you need compositor integration or independent animation timing.

2. **Implement a drawable**
   ```csharp
   public sealed class MyDrawable : ISkiaLeaseDrawable
   {
       public bool RequiresAnimation => true;
       public void Render(SKCanvas canvas, Rect bounds, TimeSpan elapsed, ISkiaSharpApiLease lease) { /* draw */ }
       public void RenderFallback(ImmediateDrawingContext context, Rect bounds) { /* fallback */ }
   }
   ```

3. **Wrap the drawable in a control**
   ```csharp
   public sealed class MyControl : SkiaLeaseDrawableControl<MyDrawable>
   {
       public MyControl() : base(new MyDrawable()) { }
   }
   ```

4. **Integrate in XAML**
   ```xml
   xmlns:controls="clr-namespace:CustomDrawingAvalonia.Controls;assembly=CustomDrawingAvalonia"
   <controls:MyControl Height="300" Width="300"/>
   ```

5. **Add refresh hooks**
   - Call `RequestRedraw()` (lease pipeline) or `RequestManualRefresh()` (composition pipeline) whenever state changes.
   - Use `RequiresAnimation` to toggle animations at runtime based on user input or data.

## Troubleshooting

- **Build fails with SkiaSharp not found**  
  Ensure the NuGet restore step succeeds. Required packages live in both `CustomDrawingAvaloniaExamples.csproj` (desktop host) and `CustomDrawingAvalonia.csproj` (controls library), including `Avalonia`, `Avalonia.Desktop`, `Avalonia.Skia`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, and `Avalonia.Diagnostics`.

- **Fallback render path is triggered**  
  This occurs when Avalonia cannot provide a Skia lease (for example, in software rendering modes). Confirm your runtime environment supports Skia; otherwise, enhance the fallback visuals to provide useful feedback.

- **GRContext reported as n/a**  
  The GPU surface sample detects whether `lease.GrContext` is populated. If it is null, Avalonia is using the software backend or a platform that does not expose GPU access. Run on a GPU-capable configuration to see the off-screen tiling effect.

- **Animation stops after tab switch**  
  Both base controls stop scheduling frames when detached from the visual tree. Re-attaching the control restarts animation automatically. If you host controls in virtualized panels, ensure they remain attached while animation should run.

- **High CPU usage**  
  Verify that `RequiresAnimation` returns `false` for static drawables. For animated content, consider reducing frame complexity or throttling updates using elapsed time deltas.

- **Hit-testing does not work on composition visuals**  
  Composition visuals bypass Avalonia’s hit-test tree. Wrap the control in an overlay panel or forward pointer events through companion controls if interaction is required.

## Performance Checklist

- Clamp all drawing to the provided bounds; avoid allocating large temporary bitmaps.
- Reuse `SKPaint`, `SKShader`, and other Skia objects where possible.
- Leverage `Stopwatch.Elapsed` already provided by the base classes instead of creating additional timers.
- For composition visuals, keep message handling lightweight—expensive work should occur during render.
- Profile with the Avalonia Diagnostics overlay to monitor frame timing and GPU usage (`dotnet add package Avalonia.Diagnostics` already included).

## Additional Resources

- [Avalonia documentation](https://docs.avaloniaui.net/) – Official guides, tutorials, and conceptual material.
- [Avalonia source (Skia integration)](https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Skia) – Review the implementation of the Skia backend and lease feature.
- [SkiaSharp API documentation](https://learn.microsoft.com/dotnet/api/skiasharp) – General API reference for SkiaSharp types, including GPU surfaces and `GRContext`.

Use this repository as a foundation for your own custom rendering controls. The base types are intentionally lightweight so you can adapt them for dashboards, data visualizations, design tools, and other Skia-powered experiences on Avalonia.
