using GMTFV.Properties;
using GMTFV.tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMTFV.Start {
    public partial class MainForm : DevForm {
        private bool isDownloading;
        private CancellationTokenSource downloadCancellationTokenSource;
        private Color borderColor = Color.White;
        private List<VideoInfo> AllList = new List<VideoInfo>();
        private readonly object allListLock = new object();
        private HashSet<string> loadingUrls = new HashSet<string>();
        private readonly object loadingUrlsLock = new object();
        private Dictionary<string, DownloadStatus> downloadStatusMap = new Dictionary<string, DownloadStatus>();

        private readonly SemaphoreSlim loadingSemaphore = new SemaphoreSlim(3, 3);
        private HttpClient httpClient;

        private bool isClosing = false;

        public enum DownloadStatus {
            None,
            Success,
            Failed
        }

        public MainForm() {
            InitializeComponent();
            InitializeHttpClient();
        }

        private void InitializeHttpClient() {
            try {
                httpClient?.Dispose();
                httpClient = new HttpClient {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            } catch (Exception ex) {
                Console.WriteLine($"HttpClient 초기화 실패: {ex.Message}");
            }
        }

        private async Task AddVideoAsync(string url) {
            if (string.IsNullOrWhiteSpace(url)) {
                MessageBox.Show("URL이 비어있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string normalizedUrl = url;
            try {
                string RegexPattern = @"(?:youtube\.com/(?:.*[?&]v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})";
                Match match = Regex.Match(url, RegexPattern);

                if (match.Success) {
                    string videoID = match.Groups[1].Value;
                    normalizedUrl = $"https://www.youtube.com/watch?v={videoID}";
                }
            } catch (Exception ex) {
                Console.WriteLine($"URL 정규화 오류: {ex.Message}");
            }

            lock (loadingUrlsLock) {
                if (loadingUrls.Contains(normalizedUrl)) {
                    MessageBox.Show("이미 추가 중인 URL입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }
                loadingUrls.Add(normalizedUrl);
            }

            bool semaphoreAcquired = false;
            int tempRowIndex = -1;
            bool hasPartialData = false;
            VideoInfo videoInfo = null;
            CancellationTokenSource cts = null;

            try {
                await loadingSemaphore.WaitAsync();
                semaphoreAcquired = true;

                string RegexPattern = @"(?:youtube\.com/(?:.*[?&]v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})";
                Match match = Regex.Match(url, RegexPattern);

                if (!match.Success) {
                    throw new Exception("유효한 유튜브 URL이 아닙니다.");
                }

                string videoID = match.Groups[1].Value;
                url = $"https://www.youtube.com/watch?v={videoID}";

                lock (allListLock) {
                    if (AllList.Any(v => v.ID == videoID)) {
                        throw new Exception("이미 목록에 추가된 영상입니다.");
                    }
                }

                Console.WriteLine("===== AddVideoAsync 시작 =====");
                Console.WriteLine("입력 URL: " + url);

                cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                tempRowIndex = await AddTemporaryRow(url);

                var videoData = await GetVideoInfoWithYtDlp(url, cts.Token);

                if (videoData == null) {
                    throw new Exception("영상 정보를 가져올 수 없습니다.");
                }

                lock (allListLock) {
                    if (AllList.Any(v => v.ID == videoData.Id)) {
                        throw new Exception("이미 목록에 추가된 영상입니다.");
                    }
                }

                Console.WriteLine($"제목: {videoData.Title}");
                Console.WriteLine($"ID: {videoData.Id}");
                Console.WriteLine($"업로더: {videoData.Uploader}");
                Console.WriteLine($"업로드일: {videoData.UploadDate}");
                Console.WriteLine($"영상 길이: {videoData.Duration}");

                Image thumbnailImage = null;
                try {
                    thumbnailImage = await DownloadThumbnailAsync(videoData.Id);
                } catch (Exception ex) {
                    Console.WriteLine($"썸네일 다운로드 실패 (계속 진행): {ex.Message}");
                }

                videoInfo = new VideoInfo {
                    Title = videoData.Title ?? url,
                    ID = videoData.Id,
                    Author = videoData.Uploader ?? "알 수 없음",
                    UploadDate = videoData.UploadDate,
                    VideoLength = videoData.Duration,
                    Image = thumbnailImage,
                    TypeSave = new TypeSaveVideo {
                        IsTypeVideo = Settings.Default.IsTypeVideo,
                        SubType = Settings.Default.SubType
                    }
                };

                hasPartialData = true;

                if (videoData.Formats != null && videoData.Formats.Any()) {
                    Console.WriteLine("화질 목록:");

                    var videoFormats = videoData.Formats
                        .Where(f => f.Height.HasValue && f.Height.Value > 0)
                        .GroupBy(f => new { f.Height, f.Fps })
                        .Select(g => g.First())
                        .OrderByDescending(f => f.Height)
                        .ThenByDescending(f => f.Fps)
                        .ToList();

                    if (videoFormats.Any()) {
                        for (int index = 0; index < videoFormats.Count; index++) {
                            var format = videoFormats[index];
                            string qualityLabel = $"{format.Height}p";
                            int fps = format.Fps ?? 30;

                            videoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                                Quality = qualityLabel,
                                Fps = fps,
                                IsSelected = (index == 0)
                            });

                            Console.WriteLine($" - {qualityLabel} / {fps}fps / {(index == 0 ? "기본 선택됨" : "")}");
                        }
                    } else {
                        videoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                            Quality = "최고 화질 (자동)",
                            Fps = 60,
                            IsSelected = true
                        });
                        videoInfo.Tag = "USE_BEST_QUALITY";
                    }
                } else {
                    videoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                        Quality = "최고 화질 (자동)",
                        Fps = 60,
                        IsSelected = true
                    });
                    videoInfo.Tag = "USE_BEST_QUALITY";
                }

                lock (allListLock) {
                    if (!AllList.Any(v => v.ID == videoData.Id)) {
                        AllList.Add(videoInfo);
                    } else {
                        throw new Exception("이미 목록에 추가된 영상입니다.");
                    }
                }

                await UpdateRowWithVideoInfoAsync(tempRowIndex, videoInfo, thumbnailImage);

                Console.WriteLine("VideoInfo 객체 생성 완료, AllList에 추가됨.");
                Console.WriteLine("===== AddVideoAsync 완료 =====");

                lock (loadingUrlsLock) {
                    loadingUrls.Remove(normalizedUrl);
                }
            } catch (OperationCanceledException) {
                Console.WriteLine("작업 시간 초과!");
                await HandleVideoLoadFailureAsync(tempRowIndex, url, "작업 시간이 초과되었습니다.");

                lock (loadingUrlsLock) {
                    loadingUrls.Remove(normalizedUrl);
                }
            } catch (Exception ex) {
                Console.WriteLine($"오류 발생: {ex}");

                if (!hasPartialData || videoInfo == null) {
                    await HandleVideoLoadFailureAsync(tempRowIndex, url, ex.Message);
                } else {
                    try {
                        await UpdateRowWithVideoInfoAsync(tempRowIndex, videoInfo, videoInfo.Image);

                        await InvokeAsync(() => {
                            MessageBox.Show(
                                $"일부 정보를 가져오는데 실패했습니다.\n\n제목: {videoInfo?.Title}\n오류: {ex.Message}",
                                "경고",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                        });
                    } catch (Exception updateEx) {
                        Console.WriteLine($"UI 업데이트 실패: {updateEx.Message}");
                    }
                }

                lock (loadingUrlsLock) {
                    loadingUrls.Remove(normalizedUrl);
                }
            } finally {
                lock (loadingUrlsLock) {
                    loadingUrls.Remove(normalizedUrl);
                }
                cts?.Dispose();
                if (semaphoreAcquired) {
                    loadingSemaphore.Release();
                }
            }
        }

        private async Task<YtDlpVideoData> GetVideoInfoWithYtDlp(string url, CancellationToken cancellationToken) {
            string ytdlpPath = Path.Combine(Tol.AppdataPath, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath)) {
                throw new Exception("yt-dlp.exe를 찾을 수 없습니다. 설정에서 다운로드해주세요.");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = ytdlpPath,
                Arguments = $"--dump-json --no-warnings --no-playlist \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Process process = null;
            try {
                process = new Process { StartInfo = startInfo };
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 비동기 대기
                await Task.Run(() => {
                    process.WaitForExit();
                }, cancellationToken);

                if (process.ExitCode != 0) {
                    string errorMsg = error.ToString();
                    if (errorMsg.Contains("Video unavailable")) {
                        throw new Exception("영상을 사용할 수 없습니다. (비공개 또는 삭제됨)");
                    } else if (errorMsg.Contains("Private video")) {
                        throw new Exception("비공개 영상입니다.");
                    } else if (errorMsg.Contains("age")) {
                        throw new Exception("연령 제한이 있는 영상입니다.");
                    } else {
                        throw new Exception($"yt-dlp 오류: {errorMsg}");
                    }
                }

                string jsonOutput = output.ToString();
                if (string.IsNullOrWhiteSpace(jsonOutput)) {
                    throw new Exception("yt-dlp에서 데이터를 받지 못했습니다.");
                }

                // JSON 파싱
                return ParseYtDlpJson(jsonOutput);
            } catch (OperationCanceledException) {
                process?.Kill();
                throw;
            } catch (Exception ex) when (!(ex is OperationCanceledException)) {
                throw new Exception($"영상 정보 가져오기 실패: {ex.Message}", ex);
            } finally {
                process?.Dispose();
            }
        }

        private YtDlpVideoData ParseYtDlpJson(string json) {
            try {
                JObject data = JObject.Parse(json);

                var videoData = new YtDlpVideoData {
                    Title = data["title"]?.ToString() ?? "제목 없음",
                    Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                    Uploader = data["uploader"]?.ToString() ?? data["channel"]?.ToString() ?? "알 수 없음",
                    Duration = TimeSpan.FromSeconds(data["duration"]?.ToObject<double>() ?? 0),
                    Formats = new List<YtDlpFormat>()
                };

                // 업로드 날짜 파싱
                string uploadDateStr = data["upload_date"]?.ToString();
                if (!string.IsNullOrEmpty(uploadDateStr) && uploadDateStr.Length == 8) {
                    try {
                        int year = int.Parse(uploadDateStr.Substring(0, 4));
                        int month = int.Parse(uploadDateStr.Substring(4, 2));
                        int day = int.Parse(uploadDateStr.Substring(6, 2));
                        videoData.UploadDate = new DateTime(year, month, day);
                    } catch {
                        videoData.UploadDate = DateTime.Now;
                    }
                } else {
                    videoData.UploadDate = DateTime.Now;
                }

                // 포맷 정보 파싱
                JArray formats = data["formats"] as JArray;
                if (formats != null) {
                    foreach (JObject format in formats) {
                        try {
                            var formatData = new YtDlpFormat {
                                FormatId = format["format_id"]?.ToString(),
                                Height = format["height"]?.ToObject<int?>(),
                                Fps = format["fps"]?.ToObject<int?>(),
                                Vcodec = format["vcodec"]?.ToString(),
                                Acodec = format["acodec"]?.ToString(),
                                Ext = format["ext"]?.ToString()
                            };

                            videoData.Formats.Add(formatData);
                        } catch (Exception ex) {
                            Console.WriteLine($"포맷 파싱 오류 (무시): {ex.Message}");
                        }
                    }
                }

                return videoData;
            } catch (Exception ex) {
                throw new Exception($"JSON 파싱 오류: {ex.Message}");
            }
        }

        private async Task<int> AddTemporaryRow(string url) {
            int rowIndex = -1;
            await InvokeAsync(() => {
                try {
                    rowIndex = dataGridView1.Rows.Add(new object[] {
                        null,
                        false,
                        AllList.Count + 1,
                        null,
                        $"영상 정보 불러오는 중...\n{url}",
                        "로딩 중...",
                        TimeSpan.Zero,
                        null,
                        ""
                    });
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                } catch (Exception ex) {
                    Console.WriteLine($"임시 행 추가 실패: {ex.Message}");
                }
            });
            return rowIndex;
        }

        private async Task<Image> DownloadThumbnailAsync(string videoId) {
            if (httpClient == null) {
                InitializeHttpClient();
            }

            try {
                Console.WriteLine("썸네일 다운로드 시작...");

                byte[] imageBytes = null;
                try {
                    imageBytes = await httpClient.GetByteArrayAsync($"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg");
                } catch {
                    try {
                        imageBytes = await httpClient.GetByteArrayAsync($"https://i.ytimg.com/vi/{videoId}/0.jpg");
                    } catch {
                        Console.WriteLine("기본 썸네일도 다운로드 실패");
                    }
                }

                if (imageBytes != null && imageBytes.Length > 0) {
                    using (MemoryStream ms = new MemoryStream(imageBytes)) {
                        Image img = System.Drawing.Image.FromStream(ms);
                        Console.WriteLine("썸네일 다운로드 완료");
                        return img;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"썸네일 다운로드 실패: {ex.Message}");
            }
            return null;
        }

        private async Task UpdateRowWithVideoInfoAsync(int rowIndex, VideoInfo videoInfo, Image thumbnailImage) {
            await InvokeAsync(() => {
                try {
                    if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count && !dataGridView1.Rows[rowIndex].IsNewRow) {
                        DataGridViewRow row = dataGridView1.Rows[rowIndex];
                        row.Cells[0].Value = videoInfo;
                        row.Cells[1].Value = false;
                        row.Cells[2].Value = AllList.IndexOf(videoInfo) + 1;
                        row.Cells[3].Value = thumbnailImage;
                        row.Cells[4].Value = videoInfo.Title;
                        row.Cells[5].Value = videoInfo.Author;
                        row.Cells[6].Value = videoInfo.VideoLength;
                        row.Cells[7].Value = videoInfo.UploadDate;
                        row.Cells[8].Value = "보기";
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"행 업데이트 실패: {ex.Message}");
                }
            });
        }

        private async Task HandleVideoLoadFailureAsync(int rowIndex, string url, string errorMessage) {
            await InvokeAsync(() => {
                try {
                    if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count && !dataGridView1.Rows[rowIndex].IsNewRow) {
                        VideoInfo failedVideoInfo = new VideoInfo {
                            Title = url,
                            ID = "",
                            Author = "정보 불러오기 실패",
                            UploadDate = DateTime.Now,
                            VideoLength = TimeSpan.Zero,
                            Image = null,
                            TypeSave = new TypeSaveVideo {
                                IsTypeVideo = Settings.Default.IsTypeVideo,
                                SubType = Settings.Default.SubType
                            }
                        };

                        failedVideoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                            Quality = "기본 화질",
                            Fps = 30,
                            IsSelected = true
                        });

                        lock (allListLock) {
                            AllList.Add(failedVideoInfo);
                        }

                        DataGridViewRow row = dataGridView1.Rows[rowIndex];
                        row.Cells[0].Value = failedVideoInfo;
                        row.Cells[1].Value = false;
                        row.Cells[2].Value = AllList.Count;
                        row.Cells[3].Value = null;
                        row.Cells[4].Value = url;
                        row.Cells[5].Value = "정보 불러오기 실패";
                        row.Cells[6].Value = TimeSpan.Zero;
                        row.Cells[7].Value = DateTime.Now;
                        row.Cells[8].Value = "보기";
                        row.DefaultCellStyle.BackColor = Color.LightCoral;

                        MessageBox.Show(
                            $"영상 정보를 가져오는 중 오류가 발생했습니다.\n\nURL: {url}\n오류: {errorMessage}\n\n영상은 목록에 유지되지만 정보가 제한적입니다.",
                            "경고",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"실패 처리 중 오류: {ex.Message}");
                }
            });
        }

        private async Task InvokeAsync(Action action) {
            if (isClosing) return;

            try {
                if (InvokeRequired) {
                    await Task.Run(() => {
                        try {
                            Invoke(action);
                        } catch (ObjectDisposedException) {
                            // 폼이 이미 종료됨
                        } catch (InvalidOperationException) {
                            // 핸들이 생성되지 않음
                        }
                    });
                } else {
                    action();
                }
            } catch (Exception ex) {
                Console.WriteLine($"InvokeAsync 오류: {ex.Message}");
            }
        }

        private void MainFrom_Load(object sender, EventArgs e) {
            try {
                if (string.IsNullOrEmpty(Settings.Default.Path)) {
                    Settings.Default.Path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                    Settings.Default.Save();
                }

                if (!Directory.Exists(Settings.Default.Path)) {
                    try {
                        Directory.CreateDirectory(Settings.Default.Path);
                    } catch (Exception ex) {
                        Console.WriteLine($"기본 저장 경로 생성 실패: {ex.Message}");
                        MessageBox.Show(
                            "기본 저장 경로를 생성할 수 없습니다. 설정에서 경로를 변경해주세요.",
                            "경고",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"MainFrom_Load 오류: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            isClosing = true;

            if (isDownloading) {
                if (MessageBox.Show(
                    "다운로드 중입니다. 정말 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation) == DialogResult.Yes) {

                    downloadCancellationTokenSource?.Cancel();

                    // yt-dlp 프로세스 종료
                    KillProcessesSafely("yt-dlp");
                    KillProcessesSafely("ffmpeg");
                } else {
                    e.Cancel = true;
                    isClosing = false;
                    return;
                }
            }

            int actuallyLoadingCount = 0;
            try {
                foreach (DataGridViewRow row in dataGridView1.Rows) {
                    if (!row.IsNewRow && row.Cells[0].Value == null) {
                        actuallyLoadingCount++;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"로딩 중인 행 확인 오류: {ex.Message}");
            }

            if (actuallyLoadingCount > 0) {
                if (MessageBox.Show(
                    $"{actuallyLoadingCount}개의 영상 정보를 불러오는 중입니다. 정말 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation) == DialogResult.No) {
                    e.Cancel = true;
                    isClosing = false;
                    return;
                }
            }

            CleanupResources();

            base.OnFormClosing(e);
        }

        private void KillProcessesSafely(string processName) {
            try {
                foreach (Process process in Process.GetProcessesByName(processName)) {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"{processName} 프로세스 종료 오류: {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"{processName} 프로세스 검색 오류: {ex.Message}");
            }
        }

        private void CleanupResources() {
            try {
                lock (allListLock) {
                    foreach (var videoInfo in AllList) {
                        try {
                            videoInfo.Image?.Dispose();
                        } catch (Exception ex) {
                            Console.WriteLine($"Image 리소스 정리 오류: {ex.Message}");
                        }
                    }
                }

                try {
                    httpClient?.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"HttpClient 정리 오류: {ex.Message}");
                }

                try {
                    loadingSemaphore?.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"SemaphoreSlim 정리 오류: {ex.Message}");
                }

                try {
                    downloadCancellationTokenSource?.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"CancellationTokenSource 정리 오류: {ex.Message}");
                }
            } catch (Exception ex) {
                Console.WriteLine($"리소스 정리 중 오류: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            try {
                new Setting().ShowDialog();
            } catch (Exception ex) {
                MessageBox.Show($"설정 창을 여는 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e) {
            try {
                if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText)) {
                    string url = (string)e.Data.GetData(DataFormats.Text);
                    if (!string.IsNullOrWhiteSpace(url) && Tol.IsYouTubeUrl(url)) {
                        e.Effect = DragDropEffects.Copy;
                        borderColor = Color.Green;
                    } else {
                        e.Effect = DragDropEffects.None;
                        borderColor = Color.Red;
                    }
                } else {
                    e.Effect = DragDropEffects.None;
                    borderColor = Color.Red;
                }
                panel2.Invalidate();
            } catch (Exception ex) {
                Console.WriteLine($"DragEnter 오류: {ex.Message}");
                e.Effect = DragDropEffects.None;
                borderColor = Color.Red;
                panel2.Invalidate();
            }
        }

        private async void dataGridView1_DragDrop(object sender, DragEventArgs e) {
            try {
                if (e.Data.GetDataPresent(DataFormats.Text)) {
                    string url = (string)e.Data.GetData(DataFormats.Text);
                    if (!string.IsNullOrWhiteSpace(url) && Tol.IsYouTubeUrl(url)) {
                        await AddVideoAsync(url.Trim());
                    } else {
                        MessageBox.Show("유튜브 URL만 허용됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"DragDrop 오류: {ex.Message}");
                MessageBox.Show($"드래그 앤 드롭 처리 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            } finally {
                borderColor = Color.White;
                panel2.Invalidate();
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e) {
            try {
                if (borderColor == Color.Green) {
                    using (Pen pen = new Pen(Color.FromArgb(46, 204, 113), 3)) {
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen,
                            new Rectangle(15, 10, panel2.ClientRectangle.Width - 30, panel2.ClientRectangle.Height - 20));
                    }
                } else if (borderColor == Color.Red) {
                    using (Pen pen = new Pen(Color.FromArgb(231, 76, 60), 3)) {
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen,
                            new Rectangle(15, 10, panel2.ClientRectangle.Width - 30, panel2.ClientRectangle.Height - 10));
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Paint 오류: {ex.Message}");
            }
        }

        private void dataGridView1_DragLeave(object sender, EventArgs e) {
            borderColor = Color.White;
            panel2.Invalidate();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            try {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (e.RowIndex >= dataGridView1.Rows.Count) return;

                if (dataGridView1.Columns[e.ColumnIndex].Name == "Info") {
                    DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
                    if (row.IsNewRow) return;

                    VideoInfo videoInfo = row.Cells[0].Value as VideoInfo;
                    if (videoInfo == null) {
                        MessageBox.Show(
                            "영상 정보를 불러오는 중입니다. 잠시만 기다려주세요.",
                            "알림",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Asterisk);
                        return;
                    }

                    new VideoInfoForm(videoInfo).ShowDialog();
                }
            } catch (Exception ex) {
                MessageBox.Show(
                    $"영상 정보를 표시하는 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }
        }

        private async void button2_Click(object sender, EventArgs e) {
            try {
                AddURL addURL = new AddURL();
                if (addURL.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(addURL.Result)) {
                    await AddVideoAsync(addURL.Result.Trim());
                }
            } catch (Exception ex) {
                MessageBox.Show(
                    $"URL 추가 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            try {
                List<DataGridViewRow> checkedRows = Tol.GetCheckedRows(dataGridView1);
                if (checkedRows.Count == 0) {
                    MessageBox.Show("삭제할 행을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }

                if (MessageBox.Show(
                    $"선택한 {checkedRows.Count}개의 행을 삭제하시겠습니까?",
                    "삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation) != DialogResult.Yes) {
                    return;
                }

                foreach (DataGridViewRow row in checkedRows) {
                    try {
                        VideoInfo videoInfo = row.Cells[0].Value as VideoInfo;
                        if (videoInfo != null) {
                            try {
                                videoInfo.Image?.Dispose();
                            } catch (Exception ex) {
                                Console.WriteLine($"Image 리소스 정리 오류: {ex.Message}");
                            }

                            lock (allListLock) {
                                AllList.Remove(videoInfo);
                            }

                            if (!string.IsNullOrEmpty(videoInfo.ID) && downloadStatusMap.ContainsKey(videoInfo.ID)) {
                                downloadStatusMap.Remove(videoInfo.ID);
                            }
                        }
                        dataGridView1.Rows.Remove(row);
                    } catch (Exception ex) {
                        Console.WriteLine($"행 삭제 오류: {ex.Message}");
                    }
                }

                // 삭제 후 남은 행들의 색상을 흰색으로 초기화
                foreach (DataGridViewRow row in dataGridView1.Rows) {
                    if (!row.IsNewRow) {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(
                    $"삭제 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                button3.PerformClick();
            }
        }

        private async void button4_Click(object sender, EventArgs e) {
            if (AllList.Count == 0) {
                MessageBox.Show("다운로드할 영상이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            bool hasLoadingVideos = false;
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (!row.IsNewRow && row.Cells[0].Value == null) {
                    hasLoadingVideos = true;
                    break;
                }
            }

            if (hasLoadingVideos) {
                MessageBox.Show(
                    "아직 정보를 불러오는 중인 영상이 있습니다. 모든 영상 정보를 불러온 후 다운로드해주세요.",
                    "알림",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            string savePath = Settings.Default.Path;
            if (string.IsNullOrEmpty(savePath) || !Directory.Exists(savePath)) {
                MessageBox.Show(
                    "저장 경로가 유효하지 않습니다. 설정에서 경로를 확인해주세요.",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
                return;
            }

            isDownloading = true;
            downloadCancellationTokenSource = new CancellationTokenSource();
            Tol.DisableAllControls(this, false);

            progressBar1.Value = 0;
            progressBar1.Maximum = 100;

            List<string> failedVideos = new List<string>();
            int successCount = 0;

            try {
                int total = AllList.Count;
                int currentFileNumber = 1;

                foreach (VideoInfo videoInfo in AllList.ToList()) {
                    if (downloadCancellationTokenSource.Token.IsCancellationRequested) {
                        MessageBox.Show("다운로드가 취소되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        break;
                    }

                    int currentRowIndex = -1;
                    try {
                        for (int i = 0; i < dataGridView1.Rows.Count; i++) {
                            if (dataGridView1.Rows[i].Cells[0].Value == videoInfo) {
                                currentRowIndex = i;
                                break;
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"행 찾기 오류: {ex.Message}");
                    }

                    try {
                        Console.WriteLine($"Downloading file {currentFileNumber}/{total}: {videoInfo.Title}");

                        string sanitizedTitle = string.Concat(videoInfo.Title.Split(Path.GetInvalidFileNameChars()));
                        if (string.IsNullOrWhiteSpace(sanitizedTitle)) {
                            sanitizedTitle = $"video_{videoInfo.ID}";
                        }

                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fileExtension = videoInfo.TypeSave?.SubType ?? "mp4";
                        string videoFile = Path.Combine(savePath, $"{sanitizedTitle}_{timestamp}.{fileExtension}");

                        if (videoFile.Length > 250) {
                            sanitizedTitle = sanitizedTitle.Substring(0, Math.Min(sanitizedTitle.Length, 50));
                            videoFile = Path.Combine(savePath, $"{sanitizedTitle}_{timestamp}.{fileExtension}");
                        }

                        // yt-dlp로 다운로드
                        bool downloadSuccess = await DownloadWithYtDlp(
                            videoInfo,
                            videoFile,
                            currentFileNumber,
                            total,
                            downloadCancellationTokenSource.Token);

                        if (downloadSuccess) {
                            successCount++;
                            Console.WriteLine($"다운로드 완료: {videoInfo.Title}");

                            // 성공 상태 저장 및 색상 변경
                            if (!string.IsNullOrEmpty(videoInfo.ID)) {
                                downloadStatusMap[videoInfo.ID] = DownloadStatus.Success;
                            }
                            if (currentRowIndex >= 0 && currentRowIndex < dataGridView1.Rows.Count) {
                                dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                        } else {
                            throw new Exception("다운로드 실패");
                        }
                    } catch (OperationCanceledException) {
                        Console.WriteLine("다운로드가 사용자에 의해 취소되었습니다.");
                        break;
                    } catch (Exception ex) {
                        Console.WriteLine($"다운로드 오류: {videoInfo.Title} - {ex.Message}");
                        failedVideos.Add($"{videoInfo.Title} - {ex.Message}");

                        // 실패 상태 저장 및 색상 변경
                        if (!string.IsNullOrEmpty(videoInfo.ID)) {
                            downloadStatusMap[videoInfo.ID] = DownloadStatus.Failed;
                        }
                        if (currentRowIndex >= 0 && currentRowIndex < dataGridView1.Rows.Count) {
                            dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                        }
                    }

                    currentFileNumber++;
                }

                string message = $"다운로드 완료!\n성공: {successCount}/{total}";
                if (failedVideos.Count > 0) {
                    message += $"\n실패: {failedVideos.Count}\n\n실패한 영상:\n";
                    message += string.Join("\n", failedVideos.Take(5));
                    if (failedVideos.Count > 5) {
                        message += $"\n... 외 {failedVideos.Count - 5}개";
                    }
                }

                MessageBox.Show(
                    message,
                    "다운로드 완료",
                    MessageBoxButtons.OK,
                    failedVideos.Count > 0 ? MessageBoxIcon.Exclamation : MessageBoxIcon.Asterisk);
            } catch (Exception ex) {
                MessageBox.Show(
                    $"다운로드 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            } finally {
                isDownloading = false;
                downloadCancellationTokenSource?.Dispose();
                downloadCancellationTokenSource = null;
                Tol.DisableAllControls(this, true);
                progressBar1.Value = 0;
                label8.Text = "";
            }
        }

        private async Task<bool> DownloadWithYtDlp(VideoInfo videoInfo, string outputPath, int currentFile, int totalFiles, CancellationToken cancellationToken) {
            string ytdlpPath = Path.Combine(Tol.AppdataPath, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath)) {
                throw new Exception("yt-dlp.exe를 찾을 수 없습니다.");
            }

            if (string.IsNullOrEmpty(videoInfo.ID)) {
                throw new Exception("영상 ID가 없습니다.");
            }

            string url = $"https://www.youtube.com/watch?v={videoInfo.ID}";

            string desiredExtension = videoInfo.TypeSave?.SubType ?? "mp4";

            string formatArg = "";
            bool useBestQuality = (videoInfo.Tag as string == "USE_BEST_QUALITY");

            if (useBestQuality) {
                if (videoInfo.TypeSave.IsTypeVideo) {
                    formatArg = "-f bestvideo+bestaudio/best";
                } else {
                    formatArg = "-f bestaudio";
                }
            } else {
                var selectedQuality = videoInfo.VideoQualities.FirstOrDefault(vq => vq.IsSelected);
                if (selectedQuality != null) {
                    string height = selectedQuality.Quality.Replace("p", "");
                    int fps = selectedQuality.Fps;

                    if (videoInfo.TypeSave.IsTypeVideo) {
                        formatArg = $"-f \"bestvideo[height<={height}][fps<={fps}]+bestaudio/best[height<={height}]\"";
                    } else {
                        formatArg = "-f bestaudio";
                    }
                } else {
                    formatArg = "-f best";
                }
            }

            string mergeOutputFormat = "";
            string postProcessArgs = "";

            if (!videoInfo.TypeSave.IsTypeVideo) {
                string audioExt = videoInfo.TypeSave.SubType;
                if (audioExt == "mp3" || audioExt == "m4a" || audioExt == "wav") {
                    formatArg += $" --extract-audio --audio-format {audioExt}";
                }

            } else {
                mergeOutputFormat = $"--merge-output-format {desiredExtension}";

                if (desiredExtension == "mp4" || desiredExtension == "avi" || desiredExtension == "mov") {
                    postProcessArgs = "--postprocessor-args \"ffmpeg:-c:a aac -b:a 192k\"";
                }
            }

            string ffmpegPath = Path.Combine(Tol.AppdataPath, "ffmpeg.exe");
            string ffmpegArg = "";
            if (File.Exists(ffmpegPath)) {
                ffmpegArg = $"--ffmpeg-location \"{ffmpegPath}\"";
            }

            string arguments = $"{ffmpegArg} {formatArg} {mergeOutputFormat} {postProcessArgs} --no-playlist --newline -o \"{outputPath}\" \"{url}\"".Trim();

            Console.WriteLine($"[yt-dlp 명령어] {ytdlpPath} {arguments}");

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = ytdlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Process process = null;
            try {
                process = new Process { StartInfo = startInfo };
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        Console.WriteLine($"[yt-dlp] {e.Data}");

                        try {
                            string displayTitle = videoInfo.Title;
                            if (displayTitle.Length > 30) {
                                displayTitle = displayTitle.Substring(0, 27) + "...";
                            }

                            string statusMessage = "";
                            int? currentPercent = null;

                            // 1. 다운로드 진행률 파싱
                            if (e.Data.Contains("[download]") && e.Data.Contains("%")) {
                                int percentIndex = e.Data.IndexOf("%");
                                if (percentIndex > 0) {
                                    string percentStr = e.Data.Substring(0, percentIndex);
                                    int lastSpaceIndex = percentStr.LastIndexOf(' ');
                                    if (lastSpaceIndex > 0) {
                                        percentStr = percentStr.Substring(lastSpaceIndex + 1).Trim();
                                        if (double.TryParse(percentStr, out double percent)) {
                                            currentPercent = (int)percent;

                                            if (e.Data.Contains("video") || e.Data.Contains("webm") || e.Data.Contains("mp4")) {
                                                statusMessage = $"🎬 영상 다운로드 중... {currentPercent}%";
                                            } else if (e.Data.Contains("audio") || e.Data.Contains("m4a") || e.Data.Contains("opus")) {
                                                statusMessage = $"🎵 오디오 다운로드 중... {currentPercent}%";
                                            } else {
                                                statusMessage = $"⬇️ 다운로드 중... {currentPercent}%";
                                            }
                                        }
                                    }
                                }
                            }
                            // 2. 병합 작업
                            else if (e.Data.Contains("[Merger]") || e.Data.Contains("Merging formats")) {
                                statusMessage = "🔗 영상+오디오 병합 중...";
                                currentPercent = 95;
                            }
                            // 3. 오디오 변환 (AAC 인코딩)
                            else if (e.Data.Contains("[ExtractAudio]") || e.Data.Contains("Destination:") ||
                                     e.Data.Contains("Correcting container") || e.Data.Contains("Post-process")) {
                                statusMessage = "🔄 오디오 변환 중 (AAC)...";
                                currentPercent = 98;
                            }
                            // 4. 파일 정리
                            else if (e.Data.Contains("Deleting original file")) {
                                statusMessage = "🗑️ 임시 파일 정리 중...";
                                currentPercent = 99;
                            }
                            // 5. 완료
                            else if (e.Data.Contains("has already been downloaded")) {
                                statusMessage = "✅ 이미 다운로드된 파일";
                                currentPercent = 100;
                            }

                            if (!string.IsNullOrEmpty(statusMessage)) {
                                Invoke(new Action(() => {
                                    if (currentPercent.HasValue) {
                                        progressBar1.Value = Math.Min(currentPercent.Value, 100);
                                    }
                                    label8.Text = $"[{currentFile}/{totalFiles}] {statusMessage}\n{displayTitle}";
                                }));
                            }
                        } catch (ObjectDisposedException) {
                        } catch (InvalidOperationException) {
                        } catch (Exception ex) {
                            Console.WriteLine($"진행 상황 표시 오류: {ex.Message}");
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        error.AppendLine(e.Data);
                        Console.WriteLine($"[yt-dlp ERROR] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => {
                    while (!process.WaitForExit(1000)) {
                        if (cancellationToken.IsCancellationRequested) {
                            try {
                                process.Kill();
                            } catch { }
                            throw new OperationCanceledException();
                        }
                    }
                }, cancellationToken);

                if (process.ExitCode != 0) {
                    string errorMsg = error.ToString();
                    if (errorMsg.Contains("Video unavailable")) {
                        throw new Exception("영상을 사용할 수 없습니다.");
                    } else if (errorMsg.Contains("Private video")) {
                        throw new Exception("비공개 영상입니다.");
                    } else {
                        throw new Exception($"yt-dlp 다운로드 실패 (Exit Code: {process.ExitCode})\n{errorMsg}");
                    }
                }

                string directory = Path.GetDirectoryName(outputPath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(outputPath);

                List<string> possibleFiles = new List<string> {
            outputPath, // 원래 지정한 파일명
            Path.Combine(directory, $"{fileNameWithoutExt}.{desiredExtension}"),
        };

                if (!videoInfo.TypeSave.IsTypeVideo) {
                    possibleFiles.Add(Path.Combine(directory, $"{fileNameWithoutExt}.m4a"));
                    possibleFiles.Add(Path.Combine(directory, $"{fileNameWithoutExt}.webm"));
                    possibleFiles.Add(Path.Combine(directory, $"{fileNameWithoutExt}.opus"));
                }

                possibleFiles.Add(Path.Combine(directory, $"{fileNameWithoutExt}.webm"));
                possibleFiles.Add(Path.Combine(directory, $"{fileNameWithoutExt}.mkv"));

                foreach (string possibleFile in possibleFiles.Distinct()) {
                    if (File.Exists(possibleFile)) {
                        FileInfo fileInfo = new FileInfo(possibleFile);
                        if (fileInfo.Length > 0) {
                            Console.WriteLine($"✅ 다운로드 성공: {possibleFile} (크기: {fileInfo.Length:N0} bytes)");

                            if (Path.GetExtension(possibleFile).TrimStart('.') != desiredExtension && videoInfo.TypeSave.IsTypeVideo) {
                                Console.WriteLine($"⚠️ 확장자 불일치: 요청={desiredExtension}, 실제={Path.GetExtension(possibleFile)}");
                            }

                            return true;
                        }
                    }
                }

                Console.WriteLine($"❌ 다운로드 실패: 출력 파일을 찾을 수 없습니다.");
                Console.WriteLine($"   예상 경로: {outputPath}");
                Console.WriteLine($"   검색한 경로들:");
                foreach (var file in possibleFiles.Distinct()) {
                    Console.WriteLine($"     - {file}");
                }
                return false;

            } catch (OperationCanceledException) {
                process?.Kill();
                throw;
            } finally {
                process?.Dispose();
            }
        }

        void ToggleControls(bool isEnabled) {
            try {
                button1.Enabled = isEnabled;
                button2.Enabled = isEnabled;
                button3.Enabled = isEnabled;
                button4.Enabled = isEnabled;
                dataGridView1.Enabled = isEnabled;
            } catch (Exception ex) {
                Console.WriteLine($"ToggleControls 오류: {ex.Message}");
            }
        }

        private async void MainFrom_Shown(object sender, EventArgs e) {
            string baseDir = Tol.AppdataPath;
            string ffmpegPath = Path.Combine(baseDir, "ffmpeg.exe");
            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");
            Console.WriteLine($"FFmpeg 경로: {ffmpegPath}");
            Console.WriteLine($"yt-dlp 경로: {ytdlpPath}");

            try {
                ToggleControls(false);

                await GitHubUpdater.CheckAndUpdateAsync(
                    "tharu8813",
                    "Give-me-the-f-cking-video",
                    new Version(Application.ProductVersion),
                    progressBar1,
                    label8
                );

                // FFmpeg 다운로드
                label8.Text = "(최초 실행시 시도) FFmpeg 준비 중...";
                progressBar1.Value = 0;

                var ffmpegProgress = new Progress<Tol.FFmpegProgress>(p => {
                    try {
                        progressBar1.Value = p.Percentage;
                        label8.Text = p.Message;
                    } catch { }
                });

                await Tol.EnsureFFmpegAsync(baseDir, ffmpegProgress);

                // yt-dlp 다운로드
                label8.Text = "(최초 실행시 시도) yt-dlp 준비 중...";
                progressBar1.Value = 0;

                var ytdlpProgress = new Progress<Tol.FFmpegProgress>(p => {
                    try {
                        progressBar1.Value = p.Percentage;
                        label8.Text = p.Message;
                    } catch { }
                });

                await YtDlpTool.EnsureYtDlpAsync(baseDir, ytdlpProgress);

                label8.Text = "준비 완료!";
                progressBar1.Value = 0;
            } catch (Exception ex) {
                Tol.ShowError($"초기화 실패:\n{ex.Message}");
                Close();
            } finally {
                try {
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    ToggleControls(true);
                } catch { }
            }
        }

        private void Button_MouseEnter(object sender, EventArgs e) {
            try {
                if (sender is Button btn && btn.Enabled) {
                    btn.FlatAppearance.BorderSize = 0;

                    // 원래 색상을 약간 밝게 변경
                    Color originalColor = btn.BackColor;
                    int r = Math.Min(255, originalColor.R + 20);
                    int g = Math.Min(255, originalColor.G + 20);
                    int b = Math.Min(255, originalColor.B + 20);
                    btn.BackColor = Color.FromArgb(r, g, b);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Button_MouseEnter 오류: {ex.Message}");
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e) {
            try {
                if (sender is Button btn) {
                    btn.FlatAppearance.BorderSize = 0;

                    // 원래 색상으로 복원
                    if (btn.Name == "button1") {
                        btn.BackColor = Color.FromArgb(52, 73, 94);
                    } else if (btn.Name == "button2") {
                        btn.BackColor = Color.FromArgb(46, 204, 113);
                    } else if (btn.Name == "button3") {
                        btn.BackColor = Color.FromArgb(231, 76, 60);
                    } else if (btn.Name == "button4") {
                        btn.BackColor = Color.FromArgb(41, 128, 185);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Button_MouseLeave 오류: {ex.Message}");
            }
        }
    }

    // yt-dlp JSON 파싱용 데이터 클래스
    public class YtDlpVideoData {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Uploader { get; set; }
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public List<YtDlpFormat> Formats { get; set; }
    }

    public class YtDlpFormat {
        public string FormatId { get; set; }
        public int? Height { get; set; }
        public int? Fps { get; set; }
        public string Vcodec { get; set; }
        public string Acodec { get; set; }
        public string Ext { get; set; }
    }
}