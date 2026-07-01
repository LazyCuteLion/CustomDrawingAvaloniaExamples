using Avalonia;

namespace CustomDrawingAvaloniaExamples
{
    public class Program
    {
        static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .LogToTrace();
    }
}
