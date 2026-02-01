using GMTFV.tools;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class GitHubUpdater {
    public static async Task CheckAndUpdateAsync(
        string owner,
        string repo,
        Version currentVersion,
        ProgressBar progressBar,
        Label statusLabel) {
        try {
            progressBar.Value = 0;
            statusLabel.Text = "GitHub 릴리즈 확인 중...";

            using (HttpClient client = new HttpClient()) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WinForms-Updater");

                string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                string json = await client.GetStringAsync(apiUrl);

                JObject release = JObject.Parse(json);

                string tag = release["tag_name"]?.ToString().TrimStart('v', 'V');
                Version latestVersion = new Version(tag);
                if (latestVersion == currentVersion) {
                    statusLabel.Text = "이미 최신 버전입니다.";
                    progressBar.Value = 100;
                    return;
                } else if (latestVersion < currentVersion) {
                    Tol.ShowWarning($"현재 버전이 GitHub 릴리즈에 업로드된 버전보다 높습니다.\n현재: {currentVersion}\n최신: {latestVersion}\n\n알 수 없는 오류가 발생할 수 있습니다. 주의하세요.");
                    statusLabel.Text = "현재 버전이 최신 버전보다 높음";
                    progressBar.Value = 100;
                    return;
                }
                DialogResult ask = MessageBox.Show(
                    $"새 버전 발견!\n\n현재: {currentVersion}\n최신: {latestVersion}\n\n다운로드 하시겠습니까?",
                    "업데이트",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (ask != DialogResult.Yes) {
                    statusLabel.Text = "업데이트 취소됨";
                    return;
                }

                JArray assets = (JArray)release["assets"];
                JToken asset = assets.FirstOrDefault(a => {
                    string name = a["name"]?.ToString().ToLower();
                    return name != null &&
                           name.Contains("setup") &&
                           name.EndsWith(".exe");
                });


                var tamp = asset.FirstOrDefault();
                if (asset == null) {
                    Tol.ShowError("릴리즈 파일이 없습니다.");
                    return;
                }

                string downloadUrl = asset["browser_download_url"].ToString();
                string fileName = asset["name"].ToString();
                string savePath = Path.Combine(Tol.AppdataPath, fileName);

                statusLabel.Text = "다운로드 중...";
                progressBar.Value = 0;

                using (HttpResponseMessage response =
                    await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)) {
                    response.EnsureSuccessStatusCode();

                    long total = response.Content.Headers.ContentLength ?? -1;
                    long received = 0;

                    using (Stream netStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(savePath, FileMode.Create)) {
                        byte[] buffer = new byte[8192];
                        int read;

                        while ((read = await netStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                            await fileStream.WriteAsync(buffer, 0, read);
                            received += read;

                            if (total > 0) {
                                int percent = (int)(received * 100 / total);
                                progressBar.Value = Math.Min(percent, 100);
                                statusLabel.Text = $"다운로드 중... {percent}%";
                            }
                        }
                    }
                }

                progressBar.Value = 100;
                statusLabel.Text = "다운로드 완료! 실행중...";

                System.Diagnostics.Process.Start(savePath);
                Application.Exit();
            }
        } catch (Exception ex) {
            statusLabel.Text = "업데이트 실패";
            Tol.ShowError("업데이트 중 오류가 발생했습니다:\n" + ex.Message);
        }
    }
}
