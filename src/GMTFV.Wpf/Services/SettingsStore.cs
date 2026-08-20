using GMTFV.Wpf.Models;
using System.IO;
using System.Text.Json;

namespace GMTFV.Wpf.Services;

public sealed class SettingsStore {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTFV", "wpf-settings.json");

    public DownloadSettings Load() {
        try {
            if (!File.Exists(settingsPath)) return new DownloadSettings();
            return JsonSerializer.Deserialize<DownloadSettings>(File.ReadAllText(settingsPath), JsonOptions) ?? new DownloadSettings();
        } catch { return new DownloadSettings(); }
    }

    public void Save(DownloadSettings settings) {
        ArgumentNullException.ThrowIfNull(settings);
        settings.NormalizeAndValidate();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        string temporaryPath = settingsPath + ".tmp";
        try {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, settingsPath, overwrite: true);
        } finally {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
