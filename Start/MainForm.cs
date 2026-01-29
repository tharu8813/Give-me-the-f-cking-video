using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMTFV.Properties;
using GMTFV.tools;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace GMTFV.Start {
    public partial class MainFrom : DevForm {
        private bool isDownloading;
        private CancellationTokenSource downloadCancellationTokenSource;
        private Color borderColor = Color.White;
        private List<VideoInfo> AllList = new List<VideoInfo>();
        private readonly object allListLock = new object();
        private HashSet<string> loadingUrls = new HashSet<string>();
        private readonly object loadingUrlsLock = new object();

        public MainFrom() {
            InitializeComponent();
        }

        private async Task AddVideoAsync(string url) {
            object obj = this.loadingUrlsLock;
            lock (obj) {
                if (this.loadingUrls.Contains(url)) {
                    MessageBox.Show("이미 추가 중인 URL입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }
                this.loadingUrls.Add(url);
            }

            int tempRowIndex = -1;
            bool hasPartialData = false;
            VideoInfo videoInfo = null;
            CancellationTokenSource cts = new CancellationTokenSource();

            try {
                Console.WriteLine("===== AddVideoAsync 시작 =====");
                Console.WriteLine("입력 URL: " + url);
                cts.CancelAfter(TimeSpan.FromSeconds(30.0));

                if (InvokeRequired) {
                    Invoke(new Action(() => {
                        tempRowIndex = dataGridView1.Rows.Add(new object[]
                        {
                            null,
                            false,
                            AllList.Count + 1,
                            null,
                            "영상 정보 불러오는 중...\n" + url,
                            "로딩 중...",
                            TimeSpan.Zero,
                            null,
                            ""
                        });
                        dataGridView1.Rows[tempRowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                    }));
                } else {
                    tempRowIndex = dataGridView1.Rows.Add(new object[]
                    {
                        null,
                        false,
                        AllList.Count + 1,
                        null,
                        "영상 정보 불러오는 중...\n" + url,
                        "로딩 중...",
                        TimeSpan.Zero,
                        null,
                        ""
                    });
                    dataGridView1.Rows[tempRowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                }

                YoutubeClient youtube = new YoutubeClient();
                Video video = await youtube.Videos.GetAsync(url, cts.Token);
                StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id, cts.Token);

                Console.WriteLine("제목: " + video.Title);
                Console.WriteLine(string.Format("ID: {0}", video.Id));
                Console.WriteLine("업로더: " + video.Author.ChannelTitle);
                Console.WriteLine(string.Format("업로드일: {0}", video.UploadDate));
                Console.WriteLine(string.Format("영상 길이: {0}", video.Duration));

                Image thumbnailImage = null;
                try {
                    using (HttpClient client = new HttpClient()) {
                        client.Timeout = TimeSpan.FromSeconds(10.0);
                        Console.WriteLine("썸네일 다운로드 시작...");
                        byte[] imageBytes = await client.GetByteArrayAsync(string.Format("https://i.ytimg.com/vi/{0}/maxresdefault.jpg", video.Id));

                        if (imageBytes == null || imageBytes.Length == 0) {
                            imageBytes = await client.GetByteArrayAsync(string.Format("https://i.ytimg.com/vi/{0}/0.jpg", video.Id));
                        }

                        using (MemoryStream ms = new MemoryStream(imageBytes)) {
                            thumbnailImage = System.Drawing.Image.FromStream(ms);
                        }
                        Console.WriteLine("썸네일 다운로드 완료");
                    }
                } catch (Exception ex) {
                    Console.WriteLine("썸네일 다운로드 실패: " + ex.Message);
                }

                videoInfo = new VideoInfo {
                    Title = video.Title ?? "제목 없음",
                    ID = video.Id,
                    Author = video.Author?.ChannelTitle ?? "알 수 없음",
                    UploadDate = video.UploadDate,
                    VideoLength = video.Duration ?? TimeSpan.Zero,
                    Image = thumbnailImage,
                    TypeSave = new TypeSaveVideo {
                        IsTypeVideo = Settings.Default.IsTypeVideo,
                        SubType = Settings.Default.SubType
                    }
                };

                hasPartialData = true;

                try {
                    var orderedStreams = streamManifest.GetVideoStreams()
                        .OrderByDescending(s => s.VideoQuality.MaxHeight)
                        .ThenByDescending(s => s.VideoQuality.Framerate);

                    if (!orderedStreams.Any()) {
                        throw new Exception("사용 가능한 비디오 스트림이 없습니다.");
                    }

                    int index = 0;
                    Console.WriteLine("스트림 목록:");
                    foreach (var stream in orderedStreams) {
                        videoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                            Quality = stream.VideoQuality.Label,
                            Fps = stream.VideoQuality.Framerate,
                            IsSelected = (index == 0)
                        });

                        Console.WriteLine(string.Format(" - {0} / {1}fps / {2} / {3}",
                            stream.VideoQuality.Label,
                            stream.VideoQuality.Framerate,
                            stream.Size,
                            (index == 0) ? "기본 선택됨" : ""));
                        index++;
                    }
                } catch (Exception ex) {
                    Console.WriteLine("스트림 정보 가져오기 실패: " + ex.Message);
                    if (!videoInfo.VideoQualities.Any()) {
                        videoInfo.VideoQualities.Add(new GMTFV.tools.VideoQuality {
                            Quality = "기본 화질",
                            Fps = 30,
                            IsSelected = true
                        });
                    }
                }

                lock (allListLock) {
                    if (AllList.Any(v => v.ID == video.Id)) {
                        throw new Exception("이미 목록에 추가된 영상입니다.");
                    }
                    AllList.Add(videoInfo);
                }

                if (InvokeRequired) {
                    Invoke(new Action(() => {
                        UpdateRowWithVideoInfo(tempRowIndex, videoInfo, thumbnailImage);
                    }));
                } else {
                    UpdateRowWithVideoInfo(tempRowIndex, videoInfo, thumbnailImage);
                }

                Console.WriteLine("VideoInfo 객체 생성 완료, AllList에 추가됨.");
                Console.WriteLine("===== AddVideoAsync 완료 =====");
            } catch (OperationCanceledException) {
                Console.WriteLine("작업 시간 초과!");
                HandleVideoLoadFailure(tempRowIndex, url, "작업 시간이 초과되었습니다. 네트워크 연결을 확인해주세요.");
            } catch (Exception ex) {
                Console.WriteLine("오류 발생!");
                Console.WriteLine(ex.ToString());

                if (!hasPartialData || videoInfo == null) {
                    HandleVideoLoadFailure(tempRowIndex, url, ex.Message);
                } else {
                    lock (allListLock) {
                        AllList.Remove(videoInfo);
                    }

                    if (InvokeRequired) {
                        Invoke(new Action(() => {
                            MessageBox.Show("일부 정보를 가져오는데 실패했습니다.\n\n제목: " + videoInfo?.Title + "\n오류: " + ex.Message,
                                "경고", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }));
                    } else {
                        MessageBox.Show("일부 정보를 가져오는데 실패했습니다.\n\n제목: " + videoInfo?.Title + "\n오류: " + ex.Message,
                            "경고", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            } finally {
                lock (loadingUrlsLock) {
                    loadingUrls.Remove(url);
                }
                cts?.Dispose();
            }
        }

        private void UpdateRowWithVideoInfo(int rowIndex, VideoInfo videoInfo, Image thumbnailImage) {
            if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count) {
                DataGridViewRow row = dataGridView1.Rows[rowIndex];
                row.Cells[0].Value = videoInfo;
                row.Cells[1].Value = false;
                row.Cells[2].Value = AllList.IndexOf(videoInfo) + 1;
                row.Cells[3].Value = videoInfo.Image;
                row.Cells[4].Value = videoInfo.Title + (thumbnailImage == null ? " ⚠️" : "");
                row.Cells[5].Value = videoInfo.Author;
                row.Cells[6].Value = videoInfo.VideoLength;
                row.Cells[7].Value = videoInfo.UploadDate;
                row.Cells[8].Value = "보기";
                row.DefaultCellStyle.BackColor = dataGridView1.DefaultCellStyle.BackColor;
            }
        }

        private void HandleVideoLoadFailure(int rowIndex, string url, string errorMessage) {
            Action action = () => {
                if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count) {
                    DataGridViewRow row = dataGridView1.Rows[rowIndex];
                    row.Cells[0].Value = null;
                    row.Cells[4].Value = "⚠️ 정보 불러오기 실패\n" + url;
                    row.Cells[5].Value = "오류 발생";
                    row.DefaultCellStyle.BackColor = Color.LightCoral;

                    MessageBox.Show($"영상 정보를 가져오는 중 오류가 발생했습니다.\n\nURL: {url}\n오류: {errorMessage}\n\n해당 영상은 목록에서 제거됩니다.",
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);

                    dataGridView1.Rows.RemoveAt(rowIndex);
                }
            };

            if (InvokeRequired) {
                Invoke(action);
            } else {
                action();
            }
        }

        private void UpdataList() {
            dataGridView1.Rows.Clear();
            lock (allListLock) {
                foreach (VideoInfo videoInfo in AllList) {
                    dataGridView1.Rows.Add(new object[]
                    {
                        videoInfo,
                        false,
                        AllList.IndexOf(videoInfo) + 1,
                        videoInfo.Image,
                        videoInfo.Title,
                        videoInfo.Author,
                        videoInfo.VideoLength,
                        videoInfo.UploadDate,
                        "보기"
                    });
                }
            }
        }

        private void MainFrom_Load(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(Settings.Default.Path)) {
                Settings.Default.Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                Settings.Default.Save();
            }

            if (!Directory.Exists(Settings.Default.Path)) {
                try {
                    Directory.CreateDirectory(Settings.Default.Path);
                } catch {
                    MessageBox.Show("기본 저장 경로를 생성할 수 없습니다. 설정에서 경로를 변경해주세요.",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (isDownloading) {
                if (MessageBox.Show("다운로드 중입니다. 정말 종료하시겠습니까?", "종료 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes) {
                    downloadCancellationTokenSource?.Cancel();

                    foreach (Process process in Process.GetProcessesByName("ffmpeg")) {
                        try {
                            process.Kill();
                            process.WaitForExit(3000);
                        } catch (Exception ex) {
                            Console.WriteLine("프로세스 종료 오류: " + ex.Message);
                        }
                    }
                } else {
                    e.Cancel = true;
                }
            }

            lock (loadingUrlsLock) {
                if (loadingUrls.Count > 0) {
                    if (MessageBox.Show($"{loadingUrls.Count}개의 영상 정보를 불러오는 중입니다. 정말 종료하시겠습니까?",
                        "종료 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No) {
                        e.Cancel = true;
                    }
                }
            }

            base.OnFormClosing(e);
        }

        private void button1_Click(object sender, EventArgs e) {
            new Setting().ShowDialog();
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText)) {
                string url = (string)e.Data.GetData(DataFormats.Text);
                if (Tol.IsYouTubeUrl(url)) {
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
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.Text)) {
                string url = (string)e.Data.GetData(DataFormats.Text);
                if (Tol.IsYouTubeUrl(url)) {
                    AddVideoAsync(url);
                } else {
                    MessageBox.Show("유튜브 URL만 허용됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            borderColor = Color.White;
            panel2.Invalidate();
        }

        private void panel2_Paint(object sender, PaintEventArgs e) {
            e.Graphics.DrawRectangle(new Pen(borderColor, 2f),
                new Rectangle(1, 1, panel2.ClientRectangle.Width - 2, panel2.ClientRectangle.Height - 2));
        }

        private void dataGridView1_DragLeave(object sender, EventArgs e) {
            borderColor = Color.White;
            panel2.Invalidate();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Info" && e.RowIndex >= 0) {
                VideoInfo videoInfo = (VideoInfo)dataGridView1.Rows[e.RowIndex].Cells[0].Value;
                if (videoInfo == null) {
                    MessageBox.Show("영상 정보를 불러오는 중입니다. 잠시만 기다려주세요.",
                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }

                try {
                    new VideoInfoForm(videoInfo).ShowDialog();
                } catch (Exception ex) {
                    MessageBox.Show("영상 정보를 표시하는 중 오류가 발생했습니다:\n" + ex.Message,
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            try {
                AddURL addURL = new AddURL();
                if (addURL.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(addURL.Result)) {
                    AddVideoAsync(addURL.Result.Trim());
                }
            } catch (Exception ex) {
                MessageBox.Show("URL 추가 중 오류가 발생했습니다:\n" + ex.Message,
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            List<DataGridViewRow> checkedRows = GetCheckedRows();
            if (checkedRows.Count == 0) {
                MessageBox.Show("삭제할 행을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            if (MessageBox.Show($"선택한 {checkedRows.Count}개의 행을 삭제하시겠습니까?",
                "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes) {
                return;
            }

            foreach (DataGridViewRow row in checkedRows) {
                VideoInfo videoInfo = (VideoInfo)row.Cells[0].Value;
                if (videoInfo != null) {
                    lock (allListLock) {
                        AllList.Remove(videoInfo);
                    }
                }
                dataGridView1.Rows.Remove(row);
            }
        }

        private List<DataGridViewRow> GetCheckedRows() {
            List<DataGridViewRow> checkedRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                if (!row.IsNewRow) {
                    try {
                        if (Convert.ToBoolean(row.Cells["Select"].Value)) {
                            checkedRows.Add(row);
                        }
                    } catch {
                    }
                }
            }
            return checkedRows;
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
                MessageBox.Show("아직 정보를 불러오는 중인 영상이 있습니다. 모든 영상 정보를 불러온 후 다운로드해주세요.",
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string savePath = Settings.Default.Path;
            if (string.IsNullOrEmpty(savePath) || !Directory.Exists(savePath)) {
                MessageBox.Show("저장 경로가 유효하지 않습니다. 설정에서 경로를 확인해주세요.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            YoutubeClient youtube = new YoutubeClient();
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

                foreach (VideoInfo videoInfo in AllList) {
                    if (downloadCancellationTokenSource.Token.IsCancellationRequested) {
                        MessageBox.Show("다운로드가 취소되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        break;
                    }

                    try {
                        Console.WriteLine($"Downloading file {currentFileNumber}/{total}: {videoInfo.Title}");

                        var selectedQuality = videoInfo.VideoQualities.FirstOrDefault(vq => vq.IsSelected);
                        if (selectedQuality == null) {
                            Console.WriteLine($"영상 '{videoInfo.Title}' 에서 선택된 화질이 없습니다. 건너뜁니다.");
                            failedVideos.Add(videoInfo.Title + " - 선택된 화질 없음");
                            currentFileNumber++;
                            continue;
                        }

                        string sanitizedTitle = string.Concat(videoInfo.Title.Split(Path.GetInvalidFileNameChars()));
                        if (string.IsNullOrWhiteSpace(sanitizedTitle)) {
                            sanitizedTitle = "video_" + videoInfo.ID;
                        }

                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fileExtension = videoInfo.TypeSave?.SubType ?? "mp4";
                        string videoFile = Path.Combine(savePath, $"{sanitizedTitle}_{timestamp}.{fileExtension}");

                        if (videoFile.Length > 260) {
                            sanitizedTitle = sanitizedTitle.Substring(0, Math.Min(sanitizedTitle.Length, 50));
                            videoFile = Path.Combine(savePath, $"{sanitizedTitle}_{timestamp}.{fileExtension}");
                        }

                        StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.ID);

                        List<IStreamInfo> streamInfos = new List<IStreamInfo>();

                        if (videoInfo.TypeSave.IsTypeVideo) {
                            var videoStream = streamManifest.GetVideoOnlyStreams()
                                .Where(s => s.VideoQuality.Label == selectedQuality.Quality &&
                                           s.VideoQuality.Framerate == selectedQuality.Fps)
                                .OrderByDescending(s => s.Bitrate)
                                .FirstOrDefault();

                            if (videoStream != null) {
                                streamInfos.Add(videoStream);
                                var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                                if (audioStream != null) {
                                    streamInfos.Add(audioStream);
                                }
                            } else {
                                var muxedStream = streamManifest.GetMuxedStreams()
                                    .FirstOrDefault(s => s.VideoQuality.Label == selectedQuality.Quality &&
                                                        s.VideoQuality.Framerate == selectedQuality.Fps);
                                if (muxedStream != null) {
                                    streamInfos.Add(muxedStream);
                                }
                            }
                        } else {
                            var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                            if (audioStream != null) {
                                streamInfos.Add(audioStream);
                            }
                        }

                        if (!streamInfos.Any() || streamInfos.Contains(null)) {
                            Console.WriteLine($"영상 '{videoInfo.Title}' 에서 요청한 형식/화질의 스트림을 찾을 수 없습니다.");
                            failedVideos.Add(videoInfo.Title + " - 스트림 없음");
                            currentFileNumber++;
                            continue;
                        }

                        var conversionRequestBuilder = new ConversionRequestBuilder(videoFile);
                        conversionRequestBuilder.SetContainer(fileExtension);
                        conversionRequestBuilder.SetFFmpegPath("tools/ffmpeg.exe");
                        conversionRequestBuilder.SetPreset(ConversionPreset.UltraFast);
                        var conversionRequest = conversionRequestBuilder.Build();

                        var progress = new Progress<double>(p => {
                            int percentage = (int)(p * 100.0);
                            progressBar1.Value = Math.Min(percentage, 100);
                            label8.Text = $"{percentage}% ({currentFileNumber}/{total}) - {videoInfo.Title}";
                        });

                        await youtube.Videos.DownloadAsync(streamInfos, conversionRequest, progress,
                            downloadCancellationTokenSource.Token);

                        successCount++;
                        Console.WriteLine("다운로드 완료: " + videoInfo.Title);
                    } catch (OperationCanceledException) {
                        Console.WriteLine("다운로드가 사용자에 의해 취소되었습니다.");
                        break;
                    } catch (Exception ex) {
                        Console.WriteLine($"다운로드 오류: {videoInfo.Title} - {ex.Message}");
                        failedVideos.Add($"{videoInfo.Title} - {ex.Message}");
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

                MessageBox.Show(message, "다운로드 완료", MessageBoxButtons.OK,
                    failedVideos.Count > 0 ? MessageBoxIcon.Exclamation : MessageBoxIcon.Asterisk);
            } catch (Exception ex) {
                MessageBox.Show("다운로드 중 오류가 발생했습니다:\n" + ex.Message,
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            } finally {
                isDownloading = false;
                downloadCancellationTokenSource?.Dispose();
                downloadCancellationTokenSource = null;
                Tol.DisableAllControls(this, true);
                progressBar1.Value = 0;
                label8.Text = "";
            }
        }
    }
}