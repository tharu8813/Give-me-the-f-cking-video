using GMTFV.tools;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GMTFV.services {
    /// <summary>다운로드한 파일이 배포처가 제공한 SHA-256 값과 일치하는지 검증합니다.</summary>
    internal static class FileIntegrityVerifier {
        public static async Task VerifyRemoteSha256Async(string filePath, Uri checksumUri, string expectedFileName = null) {
            if (checksumUri == null) throw new ArgumentNullException(nameof(checksumUri));
            string checksumDocument = await Tol.SharedHttpClient.GetStringAsync(checksumUri);
            string expectedHash = FindSha256(checksumDocument, expectedFileName ?? Path.GetFileName(filePath));
            VerifySha256(filePath, expectedHash);
        }

        public static void VerifySha256(string filePath, string expectedHash) {
            if (string.IsNullOrWhiteSpace(expectedHash))
                throw new InvalidOperationException("배포처가 SHA-256 체크섬을 제공하지 않았습니다.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("검증할 파일을 찾을 수 없습니다.", filePath);

            string actualHash;
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(filePath)) {
                actualHash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
            }

            if (!string.Equals(actualHash, NormalizeHash(expectedHash), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("다운로드 파일의 SHA-256 검증에 실패했습니다.");
        }

        private static string FindSha256(string document, string expectedFileName) {
            if (string.IsNullOrWhiteSpace(document))
                throw new InvalidOperationException("체크섬 파일이 비어 있습니다.");

            foreach (string rawLine in document.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                string line = rawLine.Trim();
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                bool hasFileName = parts.Length == 1 || string.IsNullOrWhiteSpace(expectedFileName) ||
                    line.EndsWith(expectedFileName, StringComparison.OrdinalIgnoreCase);
                if (hasFileName && NormalizeHash(parts[0]).Length == 64)
                    return parts[0];
            }

            throw new InvalidOperationException("체크섬 파일에서 대상 파일의 SHA-256 값을 찾을 수 없습니다.");
        }

        private static string NormalizeHash(string value) {
            return (value ?? string.Empty).Trim().Replace("sha256:", string.Empty).Replace("SHA256:", string.Empty);
        }
    }
}
