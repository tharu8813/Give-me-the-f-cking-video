using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GMTFV.Core {
    public static class WindowsFileName {
        public static string Sanitize(string value, string replacement = "_") {
            var name = new StringBuilder();
            foreach (char c in value ?? string.Empty)
                name.Append(c < 32 || "<>:\"/\\|?*".IndexOf(c) >= 0 ? replacement : c.ToString());
            string result = name.ToString().Trim().TrimEnd('.', ' ');
            if (result.Length == 0) result = "unnamed";
            if (Regex.IsMatch(result, @"\A(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)", RegexOptions.IgnoreCase)) result = "_" + result;
            if (result.Length > 180) result = result.Substring(0, char.IsHighSurrogate(result[179]) ? 179 : 180).TrimEnd('.', ' ');
            return result;
        }

        public static string Build(string title, string author, string id, string extension, string template, int number) {
            if (!Regex.IsMatch(extension ?? "", @"\A[a-zA-Z0-9]{1,8}\z")) throw new ArgumentException("파일 확장자가 올바르지 않습니다.");
            if (string.IsNullOrWhiteSpace(template)) template = "%title%_%date%";
            string name = template.Replace("%num3%", number.ToString("D3")).Replace("%num2%", number.ToString("D2"))
                .Replace("%num%", number.ToString()).Replace("%no%", number.ToString()).Replace("%index%", number.ToString())
                .Replace("%title%", Sanitize(title)).Replace("%author%", Sanitize(author)).Replace("%id%", Sanitize(id))
                .Replace("%date%", DateTime.Now.ToString("yyyy-MM-dd")).Replace("%ext%", extension);
            name = Sanitize(name);
            return name.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase) ? name : name + "." + extension;
        }

        public static string FitPath(string path) {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            string extension = Path.GetExtension(full);
            // Leave room for collision suffixes and yt-dlp's temporary files on legacy Windows paths.
            int budget = Math.Min(180, 230 - directory.Length - extension.Length - 1);
            if (budget < 16) throw new PathTooLongException("저장 경로가 너무 깁니다. 더 짧은 폴더를 선택하세요.");
            string stem = Path.GetFileNameWithoutExtension(full);
            if (stem.Length > budget) stem = stem.Substring(0, char.IsHighSurrogate(stem[budget - 1]) ? budget - 1 : budget);
            return Path.Combine(directory, Sanitize(stem) + extension);
        }
    }
}
