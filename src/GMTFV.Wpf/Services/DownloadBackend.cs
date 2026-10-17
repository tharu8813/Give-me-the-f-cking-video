using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace GMTFV.Wpf.Services;

public interface IDownloadBackend
{
    Task<VideoMetadata> GetMetadataAsync(string url, string toolFolder, CancellationToken token);
    Task<string> DownloadAsync(DownloadRequest request, string toolFolder, IProgress<DownloadProgress> progress, CancellationToken token);
    Task<IReadOnlyList<SubtitleTrack>> GetSubtitlesAsync(string url, string toolFolder, CancellationToken token);
    Task<string> DownloadSubtitleAsync(string url, SubtitleTrack track, string target, string toolFolder, CancellationToken token);
}
public sealed class DownloadBackend(DiagnosticLog log) : IDownloadBackend
{
    // Engine preparation includes synchronous file/process calls before its first await.
    public Task<VideoMetadata> GetMetadataAsync(string url, string toolFolder, CancellationToken token) => Task.Run(() => new VideoMetadataService(log).GetAsync(url, toolFolder, token), token);
    public Task<string> DownloadAsync(DownloadRequest request, string toolFolder, IProgress<DownloadProgress> progress, CancellationToken token) => Task.Run(() => new DownloadService(toolFolder, log: log).DownloadAsync(request, progress, token), token);
    public Task<IReadOnlyList<SubtitleTrack>> GetSubtitlesAsync(string url, string toolFolder, CancellationToken token) => Task.Run(async () =>
        ParseSubtitles(await new SubtitleService(toolFolder, log).GetMetadataAsync(url, token)), token);
    public Task<string> DownloadSubtitleAsync(string url, SubtitleTrack track, string target, string toolFolder, CancellationToken token) =>
        Task.Run(() => new SubtitleService(toolFolder, log).DownloadAsync(url, track.Language, track.Automatic, target, token), token);
    public static IReadOnlyList<SubtitleTrack> ParseSubtitles(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var tracks = new List<SubtitleTrack>();
        foreach (string kind in new[] { "subtitles", "automatic_captions" }) {
            if (!doc.RootElement.TryGetProperty(kind, out var languages) || languages.ValueKind != JsonValueKind.Object) continue;
            foreach (var language in languages.EnumerateObject()) {
                if (!Regex.IsMatch(language.Name, @"\A[A-Za-z0-9_-]+\z") || language.Value.ValueKind != JsonValueKind.Array || language.Value.GetArrayLength() == 0) continue;
                string name = language.Name;
                try { name = CultureInfo.GetCultureInfo(language.Name).DisplayName; } catch (CultureNotFoundException) { }
                tracks.Add(new SubtitleTrack(language.Name, kind == "automatic_captions", name));
            }
        }
        return tracks.OrderBy(t => t.Automatic).ThenBy(t => !t.Language.StartsWith("ko", StringComparison.OrdinalIgnoreCase)).ThenBy(t => t.Name).ToArray();
    }
}
