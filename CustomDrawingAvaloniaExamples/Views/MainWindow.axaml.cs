#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using CustomDrawingAvalonia.Controls;

namespace CustomDrawingAvaloniaExamples.Views;

public partial class MainWindow : Window
{
    private readonly StaticCustomSkiaControl? _staticCustomDrawControl;
    private readonly StaticSkiaCompositionVisualHost? _staticCompositionControl;

    public MainWindow()
    {
        InitializeComponent();
        _staticCustomDrawControl = this.FindControl<StaticCustomSkiaControl>("StaticCustomDrawControl");
        _staticCompositionControl = this.FindControl<StaticSkiaCompositionVisualHost>("StaticCompositionVisualControl");
        this.RendererDiagnostics.DebugOverlays = Avalonia.Rendering.RendererDebugOverlays.Fps;

    }



    private void OnStaticCustomDrawRefresh(object? sender, RoutedEventArgs e)
    {
        _staticCustomDrawControl?.Regenerate();
    }

    private void OnStaticCompositionRefresh(object? sender, RoutedEventArgs e)
    {
        _staticCompositionControl?.Refresh();
    }

    
}
