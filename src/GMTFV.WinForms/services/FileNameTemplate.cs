using System;
using GMTFV.Core;
using GMTFV.tools;

namespace GMTFV.services {
    internal static class FileNameTemplate {
        public static string Build(VideoInfo video, string template, int listNumber) {
            if (video == null) throw new ArgumentNullException(nameof(video));
            return WindowsFileName.Build(video.Title, video.Author, video.ID, video.TypeSave?.SubType ?? "mp4", template, listNumber);
        }
    }
}
