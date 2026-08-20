using GMTFV.Core;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.ViewModels;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GMTFV.Wpf.Services;

public static class PlaylistFileService {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<PlaylistFileItem> Read(string path) {
        string content = File.ReadAllText(path);
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)) {
            try {
                List<PlaylistFileItem>? items = JsonSerializer.Deserialize<List<PlaylistFileItem>>(content, JsonOptions);
                if (items is { Count: > 0 }) return items.Where(item => YouTubeUrl.TryNormalize(item.Url, out _)).ToArray();
            } catch (JsonException) { }
        }
        return content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => YouTubeUrl.TryNormalize(value, out _))
            .Select(value => new PlaylistFileItem { Url = value })
            .ToArray();
    }

    public static void WriteJson(string path, IEnumerable<DownloadItemViewModel> items, DownloadSettings fallbackSettings) {
        var export = items.Select(item => {
            DownloadProfile profile = item.OutputProfile;
            return new PlaylistFileItem {
                Title = item.Title,
                Author = item.ChannelName,
                Url = item.SourceUrl,
                VideoId = item.VideoId,
                IsTypeVideo = profile.IsVideo,
                SubType = profile.Container,
                Quality = profile.Quality,
                Fps = profile.FramesPerSecond
            };
        });
        WriteAtomically(path, JsonSerializer.Serialize(export, JsonOptions));
    }

    public static void WriteText(string path, IEnumerable<DownloadItemViewModel> items) =>
        WriteAtomically(path, string.Join(Environment.NewLine, items.Select(item => item.SourceUrl)));

    private static void WriteAtomically(string path, string content) {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp";
        try {
            File.WriteAllText(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        } finally {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

public sealed class PlaylistFileItem {
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Url { get; set; }
    public string? VideoId { get; set; }
    public bool IsTypeVideo { get; set; } = true;
    public string? SubType { get; set; }
    public string? Quality { get; set; }
    public int Fps { get; set; }

    public DownloadProfile ToProfile() => new() {
        IsVideo = IsTypeVideo,
        Container = string.IsNullOrWhiteSpace(SubType) ? (IsTypeVideo ? "mp4" : "mp3") : SubType,
        Quality = Quality ?? string.Empty,
        FramesPerSecond = Fps
    };
}
