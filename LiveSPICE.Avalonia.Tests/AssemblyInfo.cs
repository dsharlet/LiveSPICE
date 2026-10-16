using Avalonia;
using Avalonia.Headless;
using LiveSPICE.Avalonia;
using LiveSPICE.Avalonia.Tests;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// One headless Avalonia application for the whole test process. AppBuilder.Setup can only run
// once per process, so per-class setup helpers ("Setup was already called") and desktop
// SetupWithoutStarting (no dispatcher loop, so Dispatcher.UIThread.Invoke hangs) both fail when
// the suite runs together. Tests that touch controls or windows use [AvaloniaFact], which runs
// them on the headless dispatcher thread.
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace LiveSPICE.Avalonia.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
