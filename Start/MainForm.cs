using GMTFV.models;
using GMTFV.Properties;
using GMTFV.services;
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
       private DownloadQueueService downloadQueue;
       private readonly VideoMetadataService videoMetadataService = new VideoMetadataService();
       private readonly ChromeTabImportService chromeTabImportService = new ChromeTabImportService();
       private readonly Button chromeTabsButton = new Button();
       private bool isChromeTabImportAvailable;

        private bool isClosing = false;
        private readonly DownloadProcessTracker processTracker = new DownloadProcessTracker();
        private readonly Label emptyStateLabel = new Label();
        private readonly Dictionary<Button, Color> buttonBaseColors = new Dictionary<Button, Color>();

       // 동적 프로그레스 바 관리용
       private List<ProgressBar> dynamicProgressBars = new List<ProgressBar>();
       private List<System.Windows.Forms.Label> dynamicStatusLabels = new List<System.Windows.Forms.Label>();

        public enum DownloadStatus {
            None,
            Success,
            Failed
        }

        private enum DownloadPhase {
            Video,
            Audio,
            Merging,
            Processing,
            Finishing,
            Completed,
            Downloading
        }

        public MainForm() {
            InitializeComponent();
            ApplyUserExperience();
            chromeTabImportService.TabsReceived += ChromeTabImportService_TabsReceived;
            isChromeTabImportAvailable = chromeTabImportService.Start();
        }

        /// <summary>디자이너로 설정할 수 없는 런타임 초기화를 한 곳에서 관리합니다.</summary>
        private void ApplyUserExperience() {
            // buttonBaseColors 등록 (Button_MouseEnter/Leave에서 사용)
            buttonBaseColors[button1] = button1.BackColor;
            buttonBaseColors[button2] = button2.BackColor;
            buttonBaseColors[button3] = button3.BackColor;
            buttonBaseColors[button4] = button4.BackColor;

            // 툴팁 설정
            toolTip1.SetToolTip(button1, "저장 형식, 폴더, 동시 다운로드 설정");
            toolTip1.SetToolTip(button2, "YouTube URL을 추가합니다");
            toolTip1.SetToolTip(button3, "선택한 항목을 목록에서 제거합니다");
            toolTip1.SetToolTip(button4, "목록의 영상을 다운로드합니다");

            ConfigureChromeTabsButton();

            // 빈 목록 안내 레이블 (동적 생성)
            emptyStateLabel.Text = "아직 다운로드 목록이 비어 있습니다\n\nURL 추가 · Chrome 탭 가져오기 · 드래그 앤 드롭 중 편한 방법을 선택하세요.";
            emptyStateLabel.Font = new Font("맑은 고딕", 10.5F);
            emptyStateLabel.ForeColor = Color.FromArgb(100, 116, 139);
            emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyStateLabel.Cursor = Cursors.Hand;
            emptyStateLabel.AllowDrop = true;
            emptyStateLabel.BackColor = Color.White;
            emptyStateLabel.BorderStyle = BorderStyle.FixedSingle;
            emptyStateLabel.Click += (_, __) => button2.PerformClick();
            emptyStateLabel.DragEnter += dataGridView1_DragEnter;
            emptyStateLabel.DragLeave += dataGridView1_DragLeave;
            emptyStateLabel.DragDrop += dataGridView1_DragDrop;
            panel2.Controls.Add(emptyStateLabel);

            dataGridView1.RowsAdded += (_, __) => RefreshEmptyState();
            dataGridView1.RowsRemoved += (_, __) => RefreshEmptyState();
            panel2.Resize += (_, __) => RefreshEmptyState();
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            RefreshEmptyState();
        }

        private void ConfigureChromeTabsButton() {
            chromeTabsButton.Text = "◉ Chrome 탭";
            chromeTabsButton.Dock = DockStyle.Right;
            chromeTabsButton.Width = 130;
            chromeTabsButton.Cursor = Cursors.Hand;
            chromeTabsButton.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            ConfigureButton(chromeTabsButton, Color.FromArgb(67, 56, 202), Color.White, "열린 YouTube 탭을 한 번에 추가합니다");
            chromeTabsButton.Click += ChromeTabsButton_Click;
            panel1.Controls.Add(chromeTabsButton);
            panel1.Controls.SetChildIndex(chromeTabsButton, 0);
        }

        private void ChromeTabsButton_Click(object sender, EventArgs e) {
            if (!isChromeTabImportAvailable) {
                MessageBox.Show("Chrome 탭 가져오기 통신 포트(43128)를 사용할 수 없습니다. 다른 GMTFV 창 또는 해당 포트를 사용 중인 프로그램을 종료한 뒤 다시 실행해 주세요.", "Chrome 탭 가져오기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            const string chromeExtensionsUrl = "chrome://extensions/";
            try { Clipboard.SetText(chromeExtensionsUrl); } catch { }
            string extensionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome-extension");
            if (Directory.Exists(extensionPath)) {
                try { Process.Start("explorer.exe", extensionPath); } catch { }
            }
            MessageBox.Show("Chrome 확장 관리 주소가 클립보드에 복사되었습니다.\nChrome 주소창에서 Ctrl+V를 눌러 바로 이동하세요.\n\n처음 한 번만 설치하세요.\n1. chrome://extensions/ 페이지에서 우측 상단의 개발자 모드를 켭니다.\n2. '압축해제된 확장 프로그램을 로드합니다'를 누릅니다.\n3. 방금 열린 chrome-extension 폴더를 선택합니다.\n\n설치 후 Chrome 툴바의 GMTFV 확장 아이콘에서 'YouTube 탭 가져오기'를 누르세요.", "Chrome 탭 가져오기", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ChromeTabImportService_TabsReceived(object sender, IReadOnlyList<string> urls) {
            if (IsDisposed || isClosing) return;
            await InvokeAsync(() => label8.Text = $"Chrome에서 {urls.Count}개 YouTube 탭을 받았습니다. 목록에 추가하는 중…");
            foreach (string url in urls) if (!isClosing) await AddVideoAsync(url);
            if (!IsDisposed && !isClosing) label8.Text = $"Chrome 탭 {urls.Count}개 가져오기 완료";
        }

        private async void MainForm_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.N) {
                e.SuppressKeyPress = true;
                button2.PerformClick();
                return;
            }

            if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsText()) {
                string url = Clipboard.GetText().Trim();
                if (Tol.IsYouTubeUrl(url)) {
                    e.SuppressKeyPress = true;
                    await AddVideoAsync(url);
                }
            }
        }

        private void ConfigureButton(Button button, Color background, Color foreground, string tooltip) {
            buttonBaseColors[button] = background;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            toolTip1.SetToolTip(button, tooltip);
        }

        private void RefreshEmptyState() {
            if (emptyStateLabel.IsDisposed) return;

            bool isEmpty = dataGridView1.Rows.Count == 0;
            emptyStateLabel.Visible = isEmpty;
            if (isEmpty) {
                emptyStateLabel.Size = new Size(Math.Min(420, Math.Max(260, panel2.ClientSize.Width - 80)), 116);
                emptyStateLabel.Location = new Point(
                    Math.Max(20, (panel2.ClientSize.Width - emptyStateLabel.Width) / 2),
                    Math.Max(30, (panel2.ClientSize.Height - emptyStateLabel.Height) / 2));
                emptyStateLabel.BringToFront();
            }
        }

        private static string GetPhaseIndicator(DownloadPhase phase) {
            const string video = "① 영상";
            const string audio = "② 오디오";
            const string merge = "③ 병합";
            const string process = "④ 변환";
            const string finish = "⑤ 완료";

            switch (phase) {
                case DownloadPhase.Video: return $"[{video}] → {audio} → {merge} → {process} → {finish}";
                case DownloadPhase.Audio: return $"{video} → [{audio}] → {merge} → {process} → {finish}";
                case DownloadPhase.Merging: return $"{video} → {audio} → [{merge}] → {process} → {finish}";
                case DownloadPhase.Processing: return $"{video} → {audio} → {merge} → [{process}] → {finish}";
                case DownloadPhase.Finishing: return $"{video} → {audio} → {merge} → {process} → [{finish}]";
                case DownloadPhase.Completed: return $"{video} → {audio} → {merge} → {process} → [✓ 완료]";
                default: return $"[{video}/{audio}] → {merge} → {process} → {finish}";
            }
        }

        private static bool TryGetFfmpegProgressPercent(string line, TimeSpan duration, out int percent) {
            percent = 0;
            if (duration <= TimeSpan.Zero || string.IsNullOrWhiteSpace(line)) return false;

            string[] parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2 || (parts[0] != "out_time_ms" && parts[0] != "out_time_us")) return false;
            if (!long.TryParse(parts[1], out long rawTime) || rawTime < 0) return false;

            // FFmpeg 버전별 out_time_ms의 밀리초/마이크로초 표기 차이를 모두 처리합니다.
            double elapsedSeconds = parts[0] == "out_time_us" || rawTime > duration.TotalMilliseconds * 10
                ? rawTime / 1000000d
                : rawTime / 1000d;
            percent = Math.Min(100, Math.Max(0, (int)Math.Round(elapsedSeconds / duration.TotalSeconds * 100)));
            return true;
        }

        private void ReportMergeProgress(int progressBarIndex, int currentFile, int totalFiles, string displayTitle, int mergePercent) {
            if (isClosing) return;
            try {
                Action update = () => {
                    string title = displayTitle ?? "영상";
                    if (title.Length > 28) title = title.Substring(0, 25) + "...";
                    int overallPercent = 90 + (mergePercent * 9 / 100);
                    string message = $"🔗 영상+오디오 병합 중... {mergePercent}%";
                    string phase = GetPhaseIndicator(DownloadPhase.Merging);

                    if (progressBarIndex >= 0 && progressBarIndex < dynamicProgressBars.Count) {
                        UpdateDynamicProgressBar(progressBarIndex, overallPercent,
                            $"{phase}\n{title} · {message}", ProgressBarStyle.Continuous);
                    } else {
                        progressBar1.Style = ProgressBarStyle.Continuous;
                        progressBar1.Value = Math.Min(overallPercent, 99);
                        label8.Text = $"{phase}\n[{currentFile}/{totalFiles}] {message} · {title}";
                    }
                };

                if (InvokeRequired) BeginInvoke(update); else update();
            } catch (ObjectDisposedException) {
            } catch (InvalidOperationException) {
            }
        }

        private async Task AddVideoAsync(string url, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(url)) {
                MessageBox.Show("URL이 비어있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string normalizedUrl;
            try {
                normalizedUrl = videoMetadataService.NormalizeYouTubeUrl(url);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lock (loadingUrlsLock) {
                if (loadingUrls.Contains(normalizedUrl)) {
                    MessageBox.Show("이미 추가 중인 URL입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }
                loadingUrls.Add(normalizedUrl);
                // 설정 버튼 상태 업데이트
                try {
                    Invoke(new Action(() => button2.Enabled = false));
                } catch { }
            }

            bool semaphoreAcquired = false;
            int tempRowIndex = -1;
            bool hasPartialData = false;
            VideoInfo videoInfo = null;
            CancellationTokenSource cts = null;

            try {
                await loadingSemaphore.WaitAsync();
                semaphoreAcquired = true;

                url = normalizedUrl;

                Console.WriteLine("===== AddVideoAsync 시작 =====");
                Console.WriteLine("입력 URL: " + url);

                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                tempRowIndex = await AddTemporaryRow(url);

                var videoData = await videoMetadataService.GetAsync(url, Tol.AppdataPath, cts.Token);

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
                    thumbnailImage = await videoMetadataService.DownloadThumbnailAsync(videoData.Id, cts.Token);
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
                    // 로딩 중인 URL이 모두 제거되었으면 설정 버튼 활성화
                    if (loadingUrls.Count == 0) {
                        try {
                            Invoke(new Action(() => button2.Enabled = true));
                        } catch { }
                    }
                }
            } finally {
                lock (loadingUrlsLock) {
                    loadingUrls.Remove(normalizedUrl);
                    // 로딩 중인 URL이 모두 제거되었으면 설정 버튼 활성화
                    if (loadingUrls.Count == 0) {
                        try {
                            Invoke(new Action(() => button2.Enabled = true));
                        } catch { }
                    }
                }
                cts?.Dispose();
                if (semaphoreAcquired) {
                    loadingSemaphore.Release();
                }
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

                    // yt-dlp 및 ffmpeg 프로세스 즉시 종결
                    processTracker.KillAllActiveProcesses();
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
            chromeTabImportService.Dispose();

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
                // 현재 다운로드 목록 스냅샷 전달
                List<VideoInfo> currentList;
                lock (allListLock) {
                    currentList = AllList.ToList();
                }

                using (var settingForm = new Setting(currentList, this)) {
                    if (settingForm.ShowDialog() == DialogResult.OK && settingForm.FormatChanged) {
                        lock (allListLock) {
                            foreach (var item in AllList) {
                                if (item.TypeSave == null) {
                                    item.TypeSave = new TypeSaveVideo();
                                }
                                item.TypeSave.IsTypeVideo = Settings.Default.IsTypeVideo;
                                item.TypeSave.SubType = Settings.Default.SubType;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"설정 창을 여는 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e) {
            try {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) {
                        string ext = Path.GetExtension(files[0]).ToLower();
                        if (ext == ".json" || ext == ".txt") {
                            e.Effect = DragDropEffects.Copy;
                            borderColor = Color.Green;
                            panel2.Invalidate();
                            return;
                        }
                    }
                }

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
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0) {
                        string ext = Path.GetExtension(files[0]).ToLower();
                        if (ext == ".json" || ext == ".txt") {
                            await ImportListFromFileAsync(files[0]);
                            return;
                        }
                    }
                }

                if (e.Data.GetDataPresent(DataFormats.Text)) {
                    string url = (string)e.Data.GetData(DataFormats.Text);
                    if (!string.IsNullOrWhiteSpace(url) && Tol.IsYouTubeUrl(url)) {
                        await AddVideoAsync(url.Trim());
                    } else {
                        MessageBox.Show("유튜브 URL 또는 .json/.txt 파일만 허용됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
            // 다운로드 중이면 취소 처리
            if (isDownloading) {
                try {
                    button4.Enabled = false;
                    button4.Text = "⏳ 취소 중...";
                    button4.Refresh();

                    label8.Text = "🛑 다운로드를 중단하는 중입니다. 잠시만 기다려주세요...";
                    label8.ForeColor = Color.Red;
                    label8.Refresh();

                    if (downloadCancellationTokenSource != null && !downloadCancellationTokenSource.Token.IsCancellationRequested) {
                        downloadCancellationTokenSource.Cancel();
                    }

                    // 활성 프로세스 및 실행 중인 CLI 프로세스 강제 종결
                    processTracker.KillAllActiveProcesses();
                } catch (Exception ex) {
                    Console.WriteLine($"취소 처리 중 오류: {ex.Message}");
                }
                return;
            }

            // 다운로드 시작 로직
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

            // 동시 다운로드 수 설정
            int maxConcurrent = Settings.Default.MaxConcurrentDownloads;
            downloadQueue = new DownloadQueueService(maxConcurrent);

            // 다운로드 중에는 button4를 제외한 다른 버튼들만 비활성화
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            dataGridView1.Enabled = false;

            // button4를 활성화하고 텍스트/색상 변경 (취소 버튼으로 표시)
            button4.Enabled = true;
            button4.Text = "⏹ 다운로드 취소";
            button4.BackColor = Color.FromArgb(220, 38, 38);
            button4.ForeColor = Color.White;

            button4.Refresh();  // 화면 갱신

            // 동적 프로그레스 바 생성
            CreateDynamicProgressBars(maxConcurrent);

            progressBar1.Value = 0;
            progressBar1.Maximum = 100;

            List<string> failedVideos = new List<string>();
            int successCount = 0;
            int completedCount = 0;

            try {
                int total = AllList.Count;
                int currentFileNumber = 1;

                // 토탈 진행률 초기 표시
                UpdateTotalProgress(0, total, 0, 0);

                // 동시 다운로드 구현: Task 목록으로 병렬 다운로드
                var downloadTasks = new List<Task>();
                foreach (VideoInfo videoInfo in AllList.ToList()) {
                    if (downloadCancellationTokenSource.Token.IsCancellationRequested) {
                        MessageBox.Show("다운로드가 취소되었습니다. 현재까지 다운로드한 영상은 유지됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        break;
                    }

                    int fileNumber = currentFileNumber;

                    // 각 비디오마다 Task 생성하여 동시 다운로드 시작
                    Task downloadTask = Task.Run(async () => {
                        int barIndex = await downloadQueue.AcquireSlotAsync(downloadCancellationTokenSource.Token);

                        try {
                            int currentRowIndex = -1;
                            try {
                                if (!isClosing && dataGridView1.IsHandleCreated) {
                                    Invoke((Action)(() => {
                                        for (int i = 0; i < dataGridView1.Rows.Count; i++) {
                                            if (dataGridView1.Rows[i].Cells[0].Value == videoInfo) {
                                                currentRowIndex = i;
                                                break;
                                            }
                                        }
                                    }));
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"행 찾기 오류: {ex.Message}");
                            }

                            try {
                                Console.WriteLine($"Downloading file {fileNumber}/{total}: {videoInfo.Title} (ProgressBar: {barIndex})");

                                // 행 색상을 진행 중(연한 노랑)으로 표시
                                if (currentRowIndex >= 0 && !isClosing && dataGridView1.IsHandleCreated) {
                                    Invoke((Action)(() => {
                                        if (currentRowIndex < dataGridView1.Rows.Count) {
                                            dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                                        }
                                    }));
                                }

                                // 프로그래스바 초기화
                                if (barIndex >= 0) {
                                    UpdateDynamicProgressBar(barIndex, 0, $"준비중... ({fileNumber}/{total})");
                                }

                                // 설정의 파일명 템플릿을 적용하여 파일명 생성 (목록 순번 반영)
                                string fileName = FileNameTemplate.Build(
                                    videoInfo,
                                    Settings.Default.FileNameTemplate,
                                    fileNumber);
                                string videoFile = Path.Combine(savePath, fileName);

                                // Windows 경로 최대 길이(260) 초과 시 단축
                                if (videoFile.Length > 250) {
                                    string ext2 = Path.GetExtension(fileName);
                                    string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                                    nameOnly  = nameOnly.Substring(0, Math.Min(nameOnly.Length, 50));
                                    videoFile = Path.Combine(savePath, nameOnly + ext2);
                                }

                                // yt-dlp로 다운로드 (프로그래스바 인덱스와 콜백 전달)
                                bool downloadSuccess = await DownloadWithYtDlp(
                                    videoInfo,
                                    videoFile,
                                    fileNumber,
                                    total,
                                    downloadCancellationTokenSource.Token,
                                    barIndex);

                                if (downloadSuccess) {
                                    lock (failedVideos) {
                                        successCount++;
                                        completedCount++;
                                    }
                                    Console.WriteLine($"다운로드 완료: {videoInfo.Title}");

                                    // 완료 상태 표시 및 토탈 현황 갱신
                                    if (barIndex >= 0) {
                                        UpdateDynamicProgressBar(barIndex, 100, $"✅ 완료! ({fileNumber}/{total})");
                                    }
                                    UpdateTotalProgress(completedCount, total, successCount, failedVideos.Count);

                                    // 성공 상태 저장 및 색상 변경 (UI 스레드 가공 - 연두색)
                                    if (!string.IsNullOrEmpty(videoInfo.ID)) {
                                        downloadStatusMap[videoInfo.ID] = DownloadStatus.Success;
                                    }
                                    if (currentRowIndex >= 0 && !isClosing && dataGridView1.IsHandleCreated) {
                                        Invoke((Action)(() => {
                                            if (currentRowIndex < dataGridView1.Rows.Count) {
                                                dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                                            }
                                        }));
                                    }
                                } else {
                                    throw new Exception("다운로드 실패");
                                }
                            } catch (OperationCanceledException) {
                                Console.WriteLine("다운로드가 사용자에 의해 취소되었습니다.");
                                if (barIndex >= 0) {
                                    UpdateDynamicProgressBar(barIndex, 0, $"❌ 취소됨 ({fileNumber}/{total})");
                                }
                                if (currentRowIndex >= 0 && !isClosing && dataGridView1.IsHandleCreated) {
                                    Invoke((Action)(() => {
                                        if (currentRowIndex < dataGridView1.Rows.Count) {
                                            dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.White;
                                        }
                                    }));
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"다운로드 오류: {videoInfo.Title} - {ex.Message}");
                                lock (failedVideos) {
                                    failedVideos.Add($"{videoInfo.Title} - {ex.Message}");
                                    completedCount++;
                                }

                                if (barIndex >= 0) {
                                    UpdateDynamicProgressBar(barIndex, 0, $"❌ 실패 ({fileNumber}/{total})");
                                }
                                UpdateTotalProgress(completedCount, total, successCount, failedVideos.Count);

                                // 실패 상태 저장 및 색상 변경 (UI 스레드 가공)
                                if (!string.IsNullOrEmpty(videoInfo.ID)) {
                                    downloadStatusMap[videoInfo.ID] = DownloadStatus.Failed;
                                }
                                if (currentRowIndex >= 0 && !isClosing && dataGridView1.IsHandleCreated) {
                                    Invoke((Action)(() => {
                                        if (currentRowIndex < dataGridView1.Rows.Count) {
                                            dataGridView1.Rows[currentRowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                                        }
                                    }));
                                }
                            }
                        } finally {
                            downloadQueue.ReleaseSlot(barIndex);
                        }
                    }, downloadCancellationTokenSource.Token);

                    downloadTasks.Add(downloadTask);
                    currentFileNumber++;
                }

                // 모든 다운로드 Task가 완료될 때까지 대기
                try {
                    await Task.WhenAll(downloadTasks);
                } catch (OperationCanceledException) {
                    Console.WriteLine("전체 다운로드가 취소되었습니다.");
                } catch (Exception ex) {
                    Console.WriteLine($"Task.WhenAll 예외 발생: {ex.Message}");
                }

                if (downloadCancellationTokenSource != null && downloadCancellationTokenSource.Token.IsCancellationRequested) {
                    label8.Text = "🛑 다운로드가 취소되었습니다.";
                    label8.ForeColor = Color.OrangeRed;
                    MessageBox.Show("다운로드가 취소되었습니다. 작업 중이던 임시 파일은 정리되었습니다.", "취소 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else {
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
                }
            } catch (Exception ex) {
                MessageBox.Show(
                    $"다운로드 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            } finally {
                isDownloading = false;
                try { downloadCancellationTokenSource?.Dispose(); } catch { }
                downloadCancellationTokenSource = null;
                try { downloadQueue?.Dispose(); } catch { }
                downloadQueue = null;

                // 컨트롤 상태 100% 복원
                Invoke((Action)(() => {
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    dataGridView1.Enabled = true;

                    button4.Enabled = true;
                    button4.Text = "⬇️ 다운로드 시작";
                    button4.BackColor = buttonBaseColors[button4];
                    button4.ForeColor = Color.White;
                    button4.Refresh();
                }));

                // 동적 프로그레스 바 정리
                ClearDynamicProgressBars();
            }
        }

        private async Task<bool> DownloadWithYtDlp(VideoInfo videoInfo, string outputPath, int currentFile, int totalFiles, CancellationToken cancellationToken, int progressBarIndex = -1) {
            string ytdlpPath = Path.Combine(Tol.AppdataPath, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath)) {
                throw new Exception("yt-dlp.exe를 찾을 수 없습니다.");
            }

            YtDlpDownloadCommand command = YtDlpDownloadCommandFactory.Create(
                videoInfo,
                outputPath,
                Tol.AppdataPath,
                GetVideoEncoder(),
                Settings.Default.AudioBitrate);
            string desiredExtension = command.DesiredExtension;
            string arguments = command.Arguments;

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
            DateTime lastUiReportTime = DateTime.MinValue;
            bool isMerging = false;
            int lastMergePercent = -1;

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
                            ProgressBarStyle statusStyle = ProgressBarStyle.Continuous;
                            DownloadPhase phase = DownloadPhase.Downloading;

                            // 다운로드 속도 파싱 (예: 3.25MiB/s, 500KiB/s)
                            string speedText = "";
                            Match speedMatch = Regex.Match(e.Data, @"(\d+(?:\.\d+)?\s*[KMGT]?i?B/s)", RegexOptions.IgnoreCase);
                            if (speedMatch.Success) {
                                speedText = $" ⚡ {speedMatch.Groups[1].Value}";
                            }

                            // 1. 다운로드 진행률 파싱
                            if (e.Data.Contains("[download]") && e.Data.Contains("%")) {
                                statusStyle = ProgressBarStyle.Continuous;
                                int percentIndex = e.Data.IndexOf("%");
                                if (percentIndex > 0) {
                                    string percentStr = e.Data.Substring(0, percentIndex);
                                    int lastSpaceIndex = percentStr.LastIndexOf(' ');
                                    if (lastSpaceIndex > 0) {
                                        percentStr = percentStr.Substring(lastSpaceIndex + 1).Trim();
                                        if (double.TryParse(percentStr, out double percent)) {
                                            currentPercent = (int)percent;

                                            if (e.Data.Contains("video") || e.Data.Contains("webm") || e.Data.Contains("mp4")) {
                                                statusMessage = $"🎬 영상 다운로드 중... {currentPercent}%{speedText}";
                                                phase = DownloadPhase.Video;
                                            } else if (e.Data.Contains("audio") || e.Data.Contains("m4a") || e.Data.Contains("opus")) {
                                                statusMessage = $"🎵 오디오 다운로드 중... {currentPercent}%{speedText}";
                                                phase = DownloadPhase.Audio;
                                            } else {
                                                statusMessage = $"⬇️ 다운로드 중... {currentPercent}%{speedText}";
                                            }
                                        }
                                    }
                                }
                            }
                            // 2. 병합 작업 (Marquee 블록 형태 적용)
                            else if (e.Data.Contains("[Merger]") || e.Data.Contains("Merging formats")) {
                                isMerging = true;
                                statusMessage = "🔗 영상+오디오 병합 중...";
                                currentPercent = 95;
                                statusStyle = ProgressBarStyle.Marquee;
                                phase = DownloadPhase.Merging;
                            }
                            // 3. 오디오 변환 (AAC 인코딩 등)
                            else if (e.Data.Contains("[ExtractAudio]") || e.Data.Contains("Destination:") ||
                                     e.Data.Contains("Correcting container") || e.Data.Contains("Post-process")) {
                                statusMessage = "🔄 오디오 변환 중...";
                                currentPercent = 98;
                                statusStyle = ProgressBarStyle.Marquee;
                                phase = DownloadPhase.Processing;
                            }
                            // 4. 임시 파일 정리
                            else if (e.Data.Contains("Deleting original file")) {
                                statusMessage = "🗑️ 임시 파일 정리 중...";
                                currentPercent = 99;
                                statusStyle = ProgressBarStyle.Marquee;
                                phase = DownloadPhase.Finishing;
                            }
                            // 5. 완료
                            else if (e.Data.Contains("has already been downloaded")) {
                                statusMessage = "✅ 이미 다운로드된 파일";
                                currentPercent = 100;
                                statusStyle = ProgressBarStyle.Continuous;
                                phase = DownloadPhase.Completed;
                            }

                            if (!string.IsNullOrEmpty(statusMessage) && !isClosing) {
                                DateTime now = DateTime.Now;
                                // 100% 완료이거나 마지막 UI 업데이트 후 200ms 초과 시만 갱신 (쓰로틀링)
                                bool isFinalState = currentPercent.HasValue && currentPercent.Value >= 100;
                                if (isFinalState || statusStyle == ProgressBarStyle.Marquee || (now - lastUiReportTime).TotalMilliseconds >= 200) {
                                    lastUiReportTime = now;
                                    Invoke(new Action(() => {
                                        if (progressBarIndex >= 0 && progressBarIndex < dynamicProgressBars.Count) {
                                            // 동적 프로그레스바 모드: 개별 프로그레스바만 갱신
                                            UpdateDynamicProgressBar(progressBarIndex, currentPercent ?? 0,
                                                $"{GetPhaseIndicator(phase)}\n{displayTitle} · {statusMessage}", statusStyle);
                                        } else {
                                            // 단일 다운로드 모드: 메인 상단 프로그레스바 갱신
                                            if (statusStyle == ProgressBarStyle.Marquee) {
                                                progressBar1.Style = ProgressBarStyle.Marquee;
                                                progressBar1.MarqueeAnimationSpeed = 10;
                                            } else {
                                                progressBar1.Style = ProgressBarStyle.Continuous;
                                                if (currentPercent.HasValue) {
                                                    progressBar1.Value = Math.Min(currentPercent.Value, 100);
                                                }
                                            }
                                            label8.Text = $"{GetPhaseIndicator(phase)}\n[{currentFile}/{totalFiles}] {statusMessage} · {displayTitle}";
                                        }
                                    }));
                                }
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

                        if (isMerging && TryGetFfmpegProgressPercent(e.Data, videoInfo.VideoLength, out int mergePercent) &&
                            mergePercent != lastMergePercent) {
                            lastMergePercent = mergePercent;
                            ReportMergeProgress(progressBarIndex, currentFile, totalFiles, displayTitle: videoInfo.Title, mergePercent);
                        }
                    }
                };

                process.Start();
                processTracker.RegisterProcess(process);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool isCancelled = false;
                await Task.Run(() => {
                    while (!process.WaitForExit(300)) {
                        if (cancellationToken.IsCancellationRequested) {
                            try {
                                if (!process.HasExited)
                                    process.Kill();
                            } catch { }
                            isCancelled = true;
                            break;
                        }
                    }

                    if (!isCancelled) {
                        process.WaitForExit();
                    }
                });

                if (isCancelled || cancellationToken.IsCancellationRequested) {
                    throw new OperationCanceledException(cancellationToken);
                }

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
                try {
                    if (process != null && !process.HasExited) {
                        process.Kill();
                    }
                } catch { }
                throw;
            } finally {
                processTracker.UnregisterProcess(process);
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

                // 하단 도구(yt-dlp, FFmpeg) 버전 정보 표시
                await DisplayToolVersionsAsync();
            } catch (OperationCanceledException) {
                Console.WriteLine("초기화가 사용자에 의해 취소되었습니다.");
                Close();
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

        /// <summary>
        /// 현재 사용 중인 yt-dlp 및 FFmpeg 버전을 비동기로 확인하여 하단 라벨에 표시합니다.
        /// </summary>
        private async Task DisplayToolVersionsAsync() {
            string baseDir = Tol.AppdataPath;
            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");
            string ffmpegPath = Path.Combine(baseDir, "ffmpeg.exe");

            string ytdlpVersion = "확인 중...";
            string ffmpegVersion = "확인 중...";

            // 1. yt-dlp 버전 확인
            if (File.Exists(ytdlpPath)) {
                try {
                    string verOutput = await YtDlpTool.RunYtDlpCommandAsync(ytdlpPath, "--version", timeout: 10);
                    if (!string.IsNullOrWhiteSpace(verOutput)) {
                        ytdlpVersion = verOutput.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"yt-dlp 버전 확인 실패: {ex.Message}");
                    ytdlpVersion = "설치됨";
                }
            } else {
                ytdlpVersion = "미설치";
            }

            // 2. FFmpeg 버전 확인
            if (File.Exists(ffmpegPath)) {
                try {
                    await Task.Run(() => {
                        var startInfo = new ProcessStartInfo {
                            FileName = ffmpegPath,
                            Arguments = "-version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        };

                        using (var p = new Process { StartInfo = startInfo }) {
                            var output = new StringBuilder();
                            p.OutputDataReceived += (_, e) => {
                                if (!string.IsNullOrEmpty(e.Data)) output.AppendLine(e.Data);
                            };
                            p.Start();
                            p.BeginOutputReadLine();
                            p.WaitForExit(3000);

                            string firstLine = output.ToString().Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(firstLine)) {
                                var match = Regex.Match(firstLine, @"ffmpeg\s+version\s+([^\s]+)");
                                if (match.Success) {
                                    ffmpegVersion = match.Groups[1].Value;
                                } else {
                                    ffmpegVersion = firstLine;
                                }
                            }
                        }
                    });
                } catch (Exception ex) {
                    Console.WriteLine($"FFmpeg 버전 확인 실패: {ex.Message}");
                    ffmpegVersion = "설치됨";
                }
            } else {
                ffmpegVersion = "미설치";
            }

            try {
                Invoke(new Action(() => {
                    lblToolVersions.Text = $"yt-dlp: {ytdlpVersion}  |  FFmpeg: {ffmpegVersion}";
                }));
            } catch { }
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
                    if (btn == button3) btn.FlatAppearance.BorderSize = 1;

                    if (btn == button4 && isDownloading) {
                        btn.BackColor = Color.FromArgb(220, 38, 38);
                    } else if (buttonBaseColors.TryGetValue(btn, out Color baseColor)) {
                        btn.BackColor = baseColor;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Button_MouseLeave 오류: {ex.Message}");
            }
        }

        private int originalPanel3Height = 140;
        private int originalFormHeight = 0;
        private int originalButtonTop = 76;
        private DockStyle originalButton4Dock = DockStyle.Fill;
        private DockStyle originalButton3Dock = DockStyle.Right;
        private DockStyle originalButton2Dock = DockStyle.Right;

        /// <summary>
        /// 맨 상단 메인 프로그레스 바와 라벨에 전체 토탈 다운로드 진행 현황을 표시합니다.
        /// </summary>
        private void UpdateTotalProgress(int completedCount, int totalCount, int successCount, int failedCount) {
            if (isClosing) return;
            try {
                if (InvokeRequired) {
                    Invoke(new Action(() => UpdateTotalProgress(completedCount, totalCount, successCount, failedCount)));
                    return;
                }

                int percent = totalCount > 0 ? (completedCount * 100) / totalCount : 0;
                progressBar1.Value = Math.Min(Math.Max(percent, 0), 100);

                if (completedCount >= totalCount && totalCount > 0) {
                    label8.Text = $"🎉 전체 다운로드 완료! [{successCount}/{totalCount} 성공] (100%)";
                    label8.ForeColor = Color.FromArgb(39, 174, 96);
                } else {
                    label8.Text = $"📊 토탈 진행 현황: [{completedCount}/{totalCount} 완료] ({percent}%)   |   ✅ 성공: {successCount}   |   ❌ 실패: {failedCount}";
                    label8.ForeColor = Color.FromArgb(41, 128, 185);
                }
            } catch { }
        }

        /// <summary>
        /// 동시 다운로드 수에 따라 프로그레스 바와 상태 라벨을 동적으로 생성합니다.
        /// </summary>
        private void CreateDynamicProgressBars(int count) {
            try {
                if (InvokeRequired) {
                    Invoke(new Action(() => CreateDynamicProgressBars(count)));
                    return;
                }

                panel3.SuspendLayout();
                this.SuspendLayout();

                // 기존 동적 UI 정리
                ClearDynamicProgressBars();

                if (originalFormHeight == 0) {
                    originalFormHeight = this.Height;
                    originalPanel3Height = panel3.Height;
                    originalButtonTop = button4.Top;
                    originalButton4Dock = button4.Dock;
                    originalButton3Dock = button3.Dock;
                    originalButton2Dock = button2.Dock;
                }

                // count가 1 이하이면 개별 동적 프로그레스 바를 생성할 필요 없음
                if (count <= 1) {
                    panel3.ResumeLayout(true);
                    this.ResumeLayout(true);
                    return;
                }

                // 버튼의 Dock 속성을 None으로 해제하여 잔상 및 겹침 방지
                button4.Dock = DockStyle.None;
                button3.Dock = DockStyle.None;
                button2.Dock = DockStyle.None;

                int startY = label8.Bottom + 5;
                int itemHeight = 64;

                for (int i = 0; i < count; i++) {
                    int currentTop = startY + (i * itemHeight);

                    // 프로그레스 바 생성
                    ProgressBar pb = new ProgressBar {
                        Name = $"dynamicProgressBar{i}",
                        Left = progressBar1.Left,
                        Top = currentTop,
                        Width = progressBar1.Width,
                        Height = 18,
                        Value = 0,
                        Maximum = 100,
                        Style = ProgressBarStyle.Continuous
                    };

                    // 상태 라벨 생성
                    System.Windows.Forms.Label lbl = new System.Windows.Forms.Label {
                        Name = $"dynamicLabel{i}",
                        Left = label8.Left,
                        Top = pb.Bottom + 2,
                        Width = progressBar1.Width,
                        Height = 40,
                        Text = $"대기중... ({i + 1}/{count})",
                        ForeColor = Color.FromArgb(127, 140, 141),
                        Font = new Font("맑은 고딕", 8.5f),
                        AutoSize = false
                    };

                    panel3.Controls.Add(pb);
                    panel3.Controls.Add(lbl);

                    pb.BringToFront();
                    lbl.BringToFront();

                    dynamicProgressBars.Add(pb);
                    dynamicStatusLabels.Add(lbl);
                }

                // 버튼 위치 및 너비 조정 (Dock=None 상태)
                int newButtonTop = startY + (count * itemHeight) + 10;
                int rightMargin = 20;

                button2.Left = panel3.Width - rightMargin - button2.Width;
                button2.Top = newButtonTop;

                button3.Left = button2.Left - 10 - button3.Width;
                button3.Top = newButtonTop;

                button4.Left = progressBar1.Left;
                button4.Top = newButtonTop;
                button4.Width = button3.Left - 10 - button4.Left;

                // panel3 및 Form 크기 조정
                int addedHeight = (count * itemHeight) + 10;
                panel3.Height = originalPanel3Height + addedHeight;
                this.Height = originalFormHeight + addedHeight;

                panel3.ResumeLayout(true);
                this.ResumeLayout(true);

                panel3.Invalidate(true);
                panel3.Update();
                this.Refresh();

                Console.WriteLine($"[동적 UI] {count}개의 프로그레스 바가 panel3에 성공적으로 생성됨 (새 패널 높이: {panel3.Height})");
            } catch (Exception ex) {
                Console.WriteLine($"동적 UI 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 동적으로 생성된 프로그레스 바와 라벨을 모두 제거하고 패널 크기와 버튼 배치를 복원합니다.
        /// </summary>
        private void ClearDynamicProgressBars() {
            try {
                if (InvokeRequired) {
                    Invoke(new Action(ClearDynamicProgressBars));
                    return;
                }

                panel3.SuspendLayout();
                this.SuspendLayout();

                foreach (var pb in dynamicProgressBars) {
                    try {
                        panel3.Controls.Remove(pb);
                        pb.Dispose();
                    } catch { }
                }
                dynamicProgressBars.Clear();

                foreach (var lbl in dynamicStatusLabels) {
                    try {
                        panel3.Controls.Remove(lbl);
                        lbl.Dispose();
                    } catch { }
                }
                dynamicStatusLabels.Clear();

                // 위치 및 Dock 크기 복원
                if (originalFormHeight > 0) {
                    button4.Dock = originalButton4Dock;
                    button3.Dock = originalButton3Dock;
                    button2.Dock = originalButton2Dock;

                    button4.Top = originalButtonTop;
                    button3.Top = originalButtonTop;
                    button2.Top = originalButtonTop;

                    panel3.Height = originalPanel3Height;
                    this.Height = originalFormHeight;
                }

                panel3.ResumeLayout(true);
                this.ResumeLayout(true);

                panel3.Invalidate(true);
                panel3.Update();
                this.Refresh();

                Console.WriteLine("[동적 UI] 프로그레스 바 정리 완료");
            } catch (Exception ex) {
                Console.WriteLine($"동적 UI 정리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 동적 프로그레스 바의 진행상황을 업데이트합니다.
        /// </summary>
        private void UpdateDynamicProgressBar(int barIndex, int value, string status, ProgressBarStyle style = ProgressBarStyle.Continuous) {
            try {
                if (InvokeRequired) {
                    Invoke(new Action(() => UpdateDynamicProgressBar(barIndex, value, status, style)));
                    return;
                }

                if (barIndex >= 0 && barIndex < dynamicProgressBars.Count) {
                    var pb = dynamicProgressBars[barIndex];
                    if (pb.Style != style) {
                        pb.Style = style;
                    }

                    if (style == ProgressBarStyle.Marquee) {
                        pb.MarqueeAnimationSpeed = 30;
                    } else {
                        pb.Value = Math.Min(Math.Max(value, 0), 100);
                    }

                    dynamicStatusLabels[barIndex].Text = status;
                }
            } catch (Exception ex) {
                Console.WriteLine($"프로그레스 바 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 다운로드 목록을 경량 JSON 또는 TXT 파일로 내보냅니다. (PlaylistManager 전담)
        /// </summary>
        public void ExportList() {
            List<VideoInfo> snapshot;
            lock (allListLock) {
                snapshot = AllList.ToList();
            }
            PlaylistManager.ExportList(snapshot);
        }

        /// <summary>
        /// 파일 선택 창을 통해 경량 JSON 또는 TXT 파일에서 영상 목록을 불러옵니다.
        /// </summary>
        public async Task ImportListAsync() {
            try {
                using (OpenFileDialog ofd = new OpenFileDialog()) {
                    ofd.Filter = "GMTFV 목록 및 텍스트 파일 (*.json;*.txt)|*.json;*.txt|모든 파일 (*.*)|*.*";
                    ofd.Title = "다운로드 목록 불러오기";

                    if (ofd.ShowDialog() == DialogResult.OK) {
                        await ImportListFromFileAsync(ofd.FileName);
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"목록 불러오기 실패:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 지정한 경로의 .json 또는 .txt 파일로부터 영상 목록 및 설정을 복원합니다. (PlaylistManager 위임)
        /// </summary>
        public async Task ImportListFromFileAsync(string filePath) {
            await PlaylistManager.ImportListFromFileAsync(
                filePath,
                this,
                async (url, token) => await AddVideoAsync(url, token),
                () => {
                    lock (allListLock) {
                        return AllList.LastOrDefault();
                    }
                },
                (addedVideo, config) => {
                    lock (allListLock) {
                        if (addedVideo.TypeSave == null) addedVideo.TypeSave = new TypeSaveVideo();
                        addedVideo.TypeSave.IsTypeVideo = config.IsTypeVideo;
                        addedVideo.TypeSave.SubType = config.SubType;

                        if (!string.IsNullOrEmpty(config.Quality) && addedVideo.VideoQualities != null) {
                            foreach (var q in addedVideo.VideoQualities) {
                                q.IsSelected = (q.Quality == config.Quality);
                            }
                        }
                    }
                }
            );
        }

        /// <summary>
        /// 설정된 GPU 가속기에 따라 적절한 비디오 인코더를 반환합니다.
        /// </summary>
        private string GetVideoEncoder() {
            string gpuAccelerator = Settings.Default.GPUAccelerator;
            string encoder = "-c:v libx264";  // CPU 기본값

            if (gpuAccelerator == "NVIDIA") {
                encoder = "-c:v h264_nvenc";   // NVIDIA CUDA 가속
            } else if (gpuAccelerator == "AMD") {
                encoder = "-c:v h264_amf";     // AMD HIP 가속
            } else if (gpuAccelerator == "Intel") {
                encoder = "-c:v h264_qsv";     // Intel QuickSync 가속
            }

            Console.WriteLine($"[비디오 인코더] GPU: {gpuAccelerator}, Encoder: {encoder}");
            return encoder;
        }
    }

}
