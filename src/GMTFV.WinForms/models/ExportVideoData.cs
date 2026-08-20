namespace GMTFV.models {
    /// <summary>
    /// 다운로드 목록 내보내기/불러오기용 경량 데이터 모델
    /// </summary>
    public class ExportVideoData {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public string VideoId { get; set; }
        public bool IsTypeVideo { get; set; }
        public string SubType { get; set; }
        public string Quality { get; set; }
        public int Fps { get; set; }
    }
}
