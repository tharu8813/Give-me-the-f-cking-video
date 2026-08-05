using GMTFV.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMTFV.tools {
    /// <summary>
    /// 다운로드 플레이리스트 목록의 저장(Export) 및 복원(Import) 관리를 전담하는 모듈
    /// </summary>
    public static class PlaylistManager {
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
                            var exportItems = videoList.Select(v => {
                                var selectedQualityObj = v.VideoQualities?.FirstOrDefault(q => q.IsSelected);
                                return new ExportVideoData {
                                    Url = string.IsNullOrEmpty(v.ID) ? v.Title : $"https://www.youtube.com/watch?v={v.ID}",
                                    IsTypeVideo = v.TypeSave?.IsTypeVideo ?? true,
                                    SubType = v.TypeSave?.SubType ?? "mp4",
                                    Quality = selectedQualityObj?.Quality ?? "",
                                    Fps = selectedQualityObj?.Fps ?? 0
                                };
                            }).ToList();

                            string json = JsonConvert.SerializeObject(exportItems, Formatting.Indented);
                            File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                        } else { // TXT
                            var urls = videoList.Select(v => string.IsNullOrEmpty(v.ID) ? v.Title : $"https://www.youtube.com/watch?v={v.ID}");
                            File.WriteAllLines(sfd.FileName, urls, Encoding.UTF8);
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
            Func<string, System.Threading.CancellationToken, Task> addVideoFunc,
            Func<VideoInfo> getLastAddedVideoFunc,
            Action<VideoInfo, ExportVideoData> applyVideoConfigAction) {

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            bool originalEnabled = parentForm.Enabled;
            try {
                parentForm.Enabled = false; // 불러오는 동안 부모 폼 상호작용 차단

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                List<ExportVideoData> jsonItems = null;
                List<string> urlsToLoad = new List<string>();

                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
                    try {
                        jsonItems = JsonConvert.DeserializeObject<List<ExportVideoData>>(content);
                        if (jsonItems != null) {
                            urlsToLoad = jsonItems.Where(x => !string.IsNullOrWhiteSpace(x.Url) && Tol.IsYouTubeUrl(x.Url)).Select(x => x.Url).ToList();
                        }
                    } catch { }
                }

                if (urlsToLoad.Count == 0) {
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    urlsToLoad = lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l) && Tol.IsYouTubeUrl(l)).ToList();
                }

                if (urlsToLoad.Count == 0) {
                    MessageBox.Show("불러올 유효한 유튜브 URL이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int addedCount = 0;
                using (var progressForm = new Start.ImportProgressForm(urlsToLoad.Count)) {
                    progressForm.StartPosition = FormStartPosition.Manual;
                    progressForm.Location = new Point(
                        parentForm.Location.X + (parentForm.Width - progressForm.Width) / 2,
                        parentForm.Location.Y + (parentForm.Height - progressForm.Height) / 2
                    );

                    progressForm.Show(parentForm);
                    progressForm.Refresh();

                    for (int i = 0; i < urlsToLoad.Count; i++) {
                        if (progressForm.Cts.Token.IsCancellationRequested) {
                            break;
                        }

                        string url = urlsToLoad[i];
                        progressForm.UpdateProgress(i + 1, urlsToLoad.Count, url);

                        try {
                            await addVideoFunc(url, progressForm.Cts.Token);

                            // JSON 설정 복원
                            if (jsonItems != null && i < jsonItems.Count) {
                                var item = jsonItems[i];
                                var addedVideo = getLastAddedVideoFunc?.Invoke();
                                if (addedVideo != null) {
                                    applyVideoConfigAction?.Invoke(addedVideo, item);
                                }
                            }
                            addedCount++;
                        } catch (OperationCanceledException) {
                            break;
                        } catch (Exception ex) {
                            Console.WriteLine($"URL 불러오기 예외 [{url}]: {ex.Message}");
                        }
                    }

                    bool wasCancelled = progressForm.Cts.Token.IsCancellationRequested;
                    progressForm.Close();

                    if (wasCancelled) {
                        MessageBox.Show($"불러오기가 취소되었습니다. (성공적으로 불러온 항목: {addedCount}/{urlsToLoad.Count})", "취소 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}
