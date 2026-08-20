using GMTFV.models;
using GMTFV.services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMTFV.tools {
    /// <summary>
    /// 다운로드 플레이리스트 목록의 저장(Export) 및 복원(Import) 관리를 전담하는 모듈
    /// </summary>
public static class PlaylistManager {
        // 메타데이터 요청을 과도하게 동시에 보내면 YouTube가 요청을 제한할 수 있습니다.
        private const int ImportConcurrency = 3;

        private sealed class ImportEntry {
            public string Url { get; set; }
            public ExportVideoData Configuration { get; set; }
        }
        /// <summary>
        /// 현재 영상 목록을 경량 JSON 또는 TXT 파일로 저장합니다.
        /// </summary>
        public static void ExportList(List<VideoInfo> videoList) {
            try {
                if (videoList == null || videoList.Count == 0) {
                    MessageBox.Show("내보낼 영상 목록이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog()) {
                    sfd.Filter = "JSON 목록 파일 (*.json)|*.json|URL 텍스트 파일 (*.txt)|*.txt";
                    sfd.Title = "다운로드 목록 내보내기";
                    sfd.FileName = $"GMTFV_Playlist_{DateTime.Now:yyyyMMdd_HHmmss}";

                    if (sfd.ShowDialog() == DialogResult.OK) {
                        if (sfd.FilterIndex == 1) { // JSON
                            var exportItems = videoList.Select(PlaylistDataMapper.CreateExportVideoData).ToList();

                            string json = JsonConvert.SerializeObject(exportItems, Formatting.Indented);
                            WriteAtomically(sfd.FileName, json);
                        } else { // TXT
                            var urls = videoList.Select(v => string.IsNullOrEmpty(v.ID) ? v.Title : $"https://www.youtube.com/watch?v={v.ID}");
                            WriteAtomically(sfd.FileName, string.Join(Environment.NewLine, urls));
                        }

                        MessageBox.Show("다운로드 목록을 성공적으로 내보냈습니다!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"목록 내보내기 실패:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 지정한 경로의 파일(.json 또는 .txt)에서 영상 목록을 읽어옵니다.
        /// </summary>
        public static async Task ImportListFromFileAsync(
            string filePath,
            Form parentForm,
            Func<string, CancellationToken, Task<VideoInfo>> addVideoFunc,
            Action<VideoInfo, ExportVideoData> applyVideoConfigAction) {

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            bool originalEnabled = parentForm.Enabled;
            try {
                parentForm.Enabled = false; // 불러오는 동안 부모 폼 상호작용 차단

                string content = await Task.Run(() => File.ReadAllText(filePath, Encoding.UTF8));
                var entriesToLoad = new List<ImportEntry>();

                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
                    try {
                        List<ExportVideoData> jsonItems = JsonConvert.DeserializeObject<List<ExportVideoData>>(content);
                        if (jsonItems != null) {
                            entriesToLoad = jsonItems
                                .Where(x => !string.IsNullOrWhiteSpace(x.Url) && Tol.IsYouTubeUrl(x.Url))
                                .Select(x => new ImportEntry { Url = x.Url, Configuration = x })
                                .ToList();
                        }
                    } catch { }
                }

                if (entriesToLoad.Count == 0) {
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    entriesToLoad = lines
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l) && Tol.IsYouTubeUrl(l))
                        .Select(url => new ImportEntry { Url = url })
                        .ToList();
                }

                if (entriesToLoad.Count == 0) {
                    MessageBox.Show("불러올 유효한 유튜브 URL이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int addedCount = 0;
                int completedCount = 0;
                using (var progressForm = new Start.ImportProgressForm(entriesToLoad.Count)) {
                    progressForm.StartPosition = FormStartPosition.Manual;
                    progressForm.Location = new Point(
                        parentForm.Location.X + (parentForm.Width - progressForm.Width) / 2,
                        parentForm.Location.Y + (parentForm.Height - progressForm.Height) / 2
                    );

                    progressForm.Show(parentForm);
                    progressForm.Refresh();

                    int nextIndex = -1;
                    Func<Task> worker = async () => {
                        while (!progressForm.Cts.Token.IsCancellationRequested) {
                            int index = Interlocked.Increment(ref nextIndex);
                            if (index >= entriesToLoad.Count) return;

                            ImportEntry entry = entriesToLoad[index];
                            try {
                                VideoInfo addedVideo = await addVideoFunc(entry.Url, progressForm.Cts.Token);
                                if (addedVideo != null) {
                                    if (entry.Configuration != null) {
                                        applyVideoConfigAction?.Invoke(addedVideo, entry.Configuration);
                                    }
                                    Interlocked.Increment(ref addedCount);
                                }
                            } catch (OperationCanceledException) {
                                return;
                            } catch (Exception ex) {
                                Console.WriteLine($"URL 불러오기 예외 [{entry.Url}]: {ex.Message}");
                            } finally {
                                int completed = Interlocked.Increment(ref completedCount);
                                progressForm.UpdateProgress(completed, entriesToLoad.Count, entry.Url);
                            }
                        }
                    };

                    int workerCount = Math.Min(ImportConcurrency, entriesToLoad.Count);
                    await Task.WhenAll(Enumerable.Range(0, workerCount).Select(_ => worker()));

                    bool wasCancelled = progressForm.Cts.Token.IsCancellationRequested;
                    progressForm.Close();

                    if (wasCancelled) {
                        MessageBox.Show($"불러오기가 취소되었습니다. (성공적으로 불러온 항목: {addedCount}/{entriesToLoad.Count})", "취소 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } else {
                        MessageBox.Show($"{addedCount}개의 영상 URL 및 설정을 성공적으로 불러왔습니다!", "불러오기 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"파일 불러오기 실패:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                parentForm.Enabled = originalEnabled;
            }
        }

        private static void WriteAtomically(string filePath, string content) {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = filePath + ".tmp";
            try {
                File.WriteAllText(temporaryPath, content, Encoding.UTF8);
                File.Copy(temporaryPath, filePath, true);
            } finally {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
