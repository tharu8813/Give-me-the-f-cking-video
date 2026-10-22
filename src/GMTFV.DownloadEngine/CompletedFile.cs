using System;
using System.IO;
using System.Text;
using System.Linq;

namespace GMTFV.DownloadEngine {
    public static class CompletedFile {
        public static void Validate(string path, string workDirectory, string extension) {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("yt-dlp 완료 파일 경로가 없습니다.");
            string full = Path.GetFullPath(path);
            string root = Path.GetFullPath(workDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string actualExtension = Path.GetExtension(full).TrimStart('.').ToLowerInvariant();
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                (extension == "original" ? !new[] { "mp3", "m4a", "aac", "opus", "ogg", "wav", "flac", "webm", "weba", "mka", "mp4" }.Contains(actualExtension) :
                !string.Equals(actualExtension, extension, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("완료 파일 경로 또는 확장자가 일치하지 않습니다.");
            for (string current = full; current.Length >= root.Length; current = Path.GetDirectoryName(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("완료 파일에 링크 경로를 사용할 수 없습니다.");
            byte[] header = new byte[12];
            using (var file = File.OpenRead(full)) {
                if (file.Length < 12 || file.Read(header, 0, header.Length) != header.Length) throw new InvalidDataException("완료 파일이 비어 있거나 손상되었습니다.");
            }
            string first = Encoding.ASCII.GetString(header, 0, 4);
            string type = Encoding.ASCII.GetString(header, 8, 4);
            bool valid;
            switch (actualExtension) {
                case "mp4": case "mov": case "m4a": valid = Encoding.ASCII.GetString(header, 4, 4) == "ftyp"; break;
                case "mkv": case "webm": case "mka": case "weba": valid = header[0] == 0x1a && header[1] == 0x45 && header[2] == 0xdf && header[3] == 0xa3; break;
                case "wav": valid = first == "RIFF" && type == "WAVE"; break;
                case "avi": valid = first == "RIFF" && type == "AVI "; break;
                case "flac": valid = first == "fLaC"; break;
                case "opus": case "ogg": valid = first == "OggS"; break;
                case "mp3": valid = first.StartsWith("ID3") || (header[0] == 0xff && (header[1] & 0xe0) == 0xe0); break;
                case "aac": valid = header[0] == 0xff && (header[1] & 0xf6) == 0xf0; break;
                case "flv": valid = first.StartsWith("FLV"); break;
                default: valid = false; break;
            }
            if (!valid) throw new InvalidDataException("완료 파일의 컨테이너가 요청한 형식과 다릅니다.");
        }

        public static string Publish(string source, string target) {
            string directory = Path.GetDirectoryName(target);
            string stem = Path.GetFileNameWithoutExtension(target);
            string extension = Path.GetExtension(target);
            for (int index = 0; index < 10000; index++) {
                string candidate = index == 0 ? target : Path.Combine(directory, stem + " (" + index + ")" + extension);
                try { File.Move(source, candidate); return candidate; }
                catch (IOException) when (File.Exists(candidate) || Directory.Exists(candidate)) { }
            }
            throw new IOException("동일 이름 파일이 너무 많습니다. 파일명을 변경하세요.");
        }
    }
}
