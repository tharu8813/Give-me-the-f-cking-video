using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GMTFV.DownloadEngine {
    public sealed class DiagnosticLog {
        private static readonly object Sync = new object();
        public string DirectoryPath { get; }
        public DiagnosticLog(string directory = null) {
            DirectoryPath = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTFV", "Logs");
        }

        public static string Redact(string value) {
            string text = value ?? string.Empty;
            text = Regex.Replace(text, @"https?://[^\s\""<>]+", "[URL]", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"(?i)(cookie|authorization|po_token|token|visitor_data|password)\s*[:=]\s*[^\r\n]+", "$1=[REDACTED]");
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(profile)) text = text.Replace(profile, "[USER]");
            text = Regex.Replace(text, @"\[USER\][\\/][^\r\n]*", "[USER_PATH]");
            text = Regex.Replace(text, @"(?i)\b[a-z]:[\\/][^\r\n]*", "[PATH]");
            return text;
        }

        public void Write(string jobId, string message) {
            try {
                lock (Sync) {
                    Directory.CreateDirectory(DirectoryPath);
                    string path = Path.Combine(DirectoryPath, "download.log");
                    if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024) {
                        string previous = path + ".1";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(path, previous);
                    }
                    File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " job=" + jobId + " " + Redact(message) + Environment.NewLine, Encoding.UTF8);
                }
            } catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
