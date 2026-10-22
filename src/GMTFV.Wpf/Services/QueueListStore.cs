using System.IO;
using System.Text;
using System.Text.Json;
using GMTFV.Core;
using GMTFV.Wpf.Models;
namespace GMTFV.Wpf.Services;

public sealed record QueueEntry(string Url, ItemDownloadOptions? Options);
public static class QueueListStore
{
    public static async Task SaveAsync(string path, QueueEntry[] entries, CancellationToken token)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) {
                if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) {
                    await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                    foreach (var entry in entries) { token.ThrowIfCancellationRequested(); await writer.WriteLineAsync(entry.Url.AsMemory(), token).ConfigureAwait(false); }
                } else await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions { WriteIndented = true }, token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static async Task<QueueEntry[]> LoadAsync(string path, CancellationToken token)
    {
        const int limit = 2 * 1024 * 1024;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        if (stream.Length > limit) throw new InvalidDataException("목록 파일은 2MB 이하로 선택하세요.");
        using var buffer = new MemoryStream();
        var bytes = new byte[81920]; int count;
        while ((count = await stream.ReadAsync(bytes, token).ConfigureAwait(false)) > 0) {
            if (buffer.Length + count > limit) throw new InvalidDataException("목록 파일은 2MB 이하로 선택하세요.");
            buffer.Write(bytes, 0, count);
        }
        buffer.Position = 0;
        using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string text = await reader.ReadToEndAsync(token).ConfigureAwait(false);
        return await Task.Run(() => Parse(text, path.EndsWith(".json", StringComparison.OrdinalIgnoreCase), token), token).ConfigureAwait(false);
    }
    private static QueueEntry[] Parse(string text, bool json, CancellationToken token)
    {
        if (!json) return System.Text.RegularExpressions.Regex.Split(text.Trim(), @"[\s,]+").Where(v => v.Length > 0).Select(url => new QueueEntry(url, null)).ToArray();
        using var doc = JsonDocument.Parse(text);
        var entries = new List<QueueEntry>();
        foreach (var entry in doc.RootElement.EnumerateArray()) {
            token.ThrowIfCancellationRequested();
            string url = (entry.ValueKind == JsonValueKind.String ? entry.GetString() : entry.GetProperty("Url").GetString()) ?? "";
            ItemDownloadOptions? value = null;
            if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("Options", out var saved) && saved.ValueKind != JsonValueKind.Null)
                value = saved.Deserialize<ItemDownloadOptions>() ?? throw new InvalidDataException("영상별 설정이 비어 있습니다.");
            else if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("IsTypeVideo", out var legacyMode)) {
                string quality = entry.TryGetProperty("Quality", out var q) ? q.GetString() ?? "" : "";
                int? height = int.TryParse(quality.TrimEnd('p'), out int h) && h > 0 ? h : null;
                value = new ItemDownloadOptions { IsVideo = legacyMode.GetBoolean(), Format = entry.GetProperty("SubType").GetString() ?? "",
                    Height = height, Fps = height != null && entry.TryGetProperty("Fps", out var fps) && fps.GetInt32() > 0 ? fps.GetInt32() : null };
            }
            value?.Validate(); entries.Add(new(url, value));
        }
        return entries.ToArray();
    }
}
