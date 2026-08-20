namespace GMTFV.tools {
    /// <summary>
    /// 영상 화질 정보를 표현하는 클래스
    /// </summary>
    public class VideoQuality {
        /// <summary>
        /// 화질 (예: 1080p, 720p)
        /// </summary>
        public string Quality { get; set; }

        /// <summary>
        /// 프레임 레이트
        /// </summary>
        public int Fps { get; set; }

        /// <summary>
        /// 선택 여부
        /// </summary>
        public bool IsSelected { get; set; }

        public override string ToString() {
            return $"{Quality} / {Fps}fps";
        }
    }
}
