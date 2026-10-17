using System;

namespace GMTFV.Core {
    public enum DownloadState { Queued, Metadata, Ready, Downloading, Merging, Converting, Retrying, Validating, Completed, Failed, Cancelled }
    public enum DownloadError { Unknown, Network, Timeout, Forbidden, RateLimited, Client, PoToken, Format, FFmpeg, Permission, DiskFull, Unavailable, MissingTool, InvalidOutput, Cancelled }

    public sealed class DownloadRequest {
        public string Url { get; set; }
        public string OutputPath { get; set; }
        public bool IsVideo { get; set; } = true;
        public string Extension { get; set; } = "mp4";
        public int? Height { get; set; }
        public int? Fps { get; set; }
        public bool AllowQualityFallback { get; set; } = true;
        public string VideoEncoder { get; set; } = "libx264";
        public int AudioBitrate { get; set; } = 192;
    }

    public sealed class DownloadProgress {
        public DownloadState State { get; set; }
        public int? Percent { get; set; }
        public string Message { get; set; }
    }

    public static class MediaFormats {
        public static readonly string[] Video = { "mp4", "mkv", "avi", "mov", "webm", "flv" };
        public static readonly string[] Audio = { "mp3", "m4a", "aac", "opus", "ogg", "wav", "flac" };
    }
}
