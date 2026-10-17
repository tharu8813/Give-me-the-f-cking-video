using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMTFV.DownloadEngine {
    public static class CommandLine {
        public static string Join(IEnumerable<string> arguments) => string.Join(" ", arguments.Select(Quote));

        // Windows CRT quoting: backslashes before quotes and the closing quote must be doubled.
        public static string Quote(string value) {
            if (value == null || value.IndexOf('\0') >= 0) throw new ArgumentException("잘못된 명령 인수입니다.");
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value) {
                if (c == '\\') { slashes++; continue; }
                result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
                result.Append(c);
                slashes = 0;
            }
            result.Append('\\', slashes * 2).Append('"');
            return result.ToString();
        }
    }
}
