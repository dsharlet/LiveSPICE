using Avalonia;

namespace LiveSPICE.Avalonia;

internal static class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode = new[] { X11RenderingMode.Software }
            })
            .LogToTrace();
    }
}