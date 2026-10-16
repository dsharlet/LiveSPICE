using LiveSPICE.Avalonia;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void SaveAndLoadRoundTripsWindowRecentAndAudioSettings()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        AppSettings settings = new AppSettings
        {
            WindowWidth = 1400,
            WindowHeight = 900,
            AudioDriver = "Virtual Audio",
            AudioDevice = "Managed loopback",
            AudioInputs = new List<string> { "Input 1" },
            AudioOutputs = new List<string> { "Output 1" }
        };
        settings.MarkUsed("Tests/Examples/MXR Phase 90.schx");

        settings.Save(path);
        AppSettings loaded = AppSettings.Load(path);

        Assert.Equal(1400, loaded.WindowWidth);
        Assert.Equal(900, loaded.WindowHeight);
        Assert.Equal("Virtual Audio", loaded.AudioDriver);
        Assert.Equal("Managed loopback", loaded.AudioDevice);
        Assert.Equal("Input 1", Assert.Single(loaded.AudioInputs));
        Assert.Equal("Output 1", Assert.Single(loaded.AudioOutputs));
        Assert.EndsWith("MXR Phase 90.schx", Assert.Single(loaded.RecentFiles));
    }

    [Fact]
    public void MarkUsedDeduplicatesAndCapsRecentFiles()
    {
        AppSettings settings = new AppSettings();

        for (int i = 0; i < 25; i++)
            settings.MarkUsed($"file-{i}.schx");
        settings.MarkUsed("file-10.schx");

        Assert.Equal(20, settings.RecentFiles.Count);
        Assert.EndsWith("file-10.schx", settings.RecentFiles[0]);
        Assert.Equal(settings.RecentFiles.Count, settings.RecentFiles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ExistingRecentFilesFiltersMissingEntries()
    {
        string existing = Path.GetTempFileName();
        AppSettings settings = new AppSettings();
        settings.MarkUsed("missing-file.schx");
        settings.MarkUsed(existing);

        Assert.Equal(existing, Assert.Single(settings.ExistingRecentFiles()));
    }
}