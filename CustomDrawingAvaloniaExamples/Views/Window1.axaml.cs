using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CustomDrawingAvalonia.Controls;

namespace CustomDrawingAvaloniaExamples;

public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
        this.RendererDiagnostics.DebugOverlays = Avalonia.Rendering.RendererDebugOverlays.Fps;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SKRuntimeEffectHelper.SetSource(sender as Control, SKRuntimeEffectHelper.RippleSksl);
    }
}
