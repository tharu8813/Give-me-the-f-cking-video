using System.IO;
using System.Text.Json;
using GMTFV.Wpf.Models;
namespace GMTFV.Wpf.Services;

public sealed class SettingsStore(string? directory = null)
{
    public string DirectoryPath { get; } = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTFV.Preview");
    public PreviewSettings Load()
    {
        string path = Path.Combine(DirectoryPath, "settings.json");
        if (!File.Exists(path)) return new PreviewSettings();
        var settings = JsonSerializer.Deserialize<PreviewSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("설정 파일이 비어 있습니다.");
        settings.Validate();
        return settings;
    }
    public void Save(PreviewSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, "settings.json");
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
