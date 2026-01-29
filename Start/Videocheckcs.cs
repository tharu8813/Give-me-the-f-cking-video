using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace GMTFV.Start {
    public partial class VideoCheckcs : DevForm {
        private readonly string url;

        public VideoCheckcs(string url) {
            InitializeComponent();
            this.url = url;
        }

        public static string GetYouTubeVideoId(string url) {
            string pattern = @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|shorts\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})";
            Match match = Regex.Match(url, pattern);

            if (match.Success) {
                return match.Groups[1].Value;
            }

            return null;
        }

        private async void VideoCheckcs_Load(object sender, EventArgs e) {
            await webView21.EnsureCoreWebView2Async(null);

            string videoId = GetYouTubeVideoId(url);
            string embedUrl = $"https://www.youtube.com/embed/{videoId}?autoplay=1&controls=1&modestbranding=1";

            webView21.Source = new Uri(embedUrl);
        }

        private void VideoCheckcs_FormClosing(object sender, FormClosingEventArgs e) {
            webView21.Dispose();
        }
    }
}