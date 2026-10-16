using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LiveSPICE.Avalonia;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    public List<string> RecentFiles { get; set; } = new List<string>();

    public double WindowWidth { get; set; } = 1200;

    public double WindowHeight { get; set; } = 800;

    public string AudioDriver { get; set; } = string.Empty;

    public string AudioDevice { get; set; } = string.Empty;

    public List<string> AudioInputs { get; set; } = new List<string>();

    public List<string> AudioOutputs { get; set; } = new List<string>();

    public static string SettingsPath
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "LiveSPICE", "avalonia-settings.json");
        }
    }

    public static AppSettings Load()
    {
        return Load(SettingsPath);
    }

    internal static AppSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch
        {
        }

        return new AppSettings();
    }

    public void Save()
    {
        Save(SettingsPath);
    }

    internal void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
    }

    public void MarkUsed(string path)
    {
        string fullPath = Path.GetFullPath(path);
        RecentFiles.RemoveAll(i => string.Equals(Path.GetFullPath(i), fullPath, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, fullPath);
        if (RecentFiles.Count > 20)
            RecentFiles.RemoveRange(20, RecentFiles.Count - 20);
    }

    public IEnumerable<string> ExistingRecentFiles()
    {
        return RecentFiles.Where(File.Exists);
    }
}