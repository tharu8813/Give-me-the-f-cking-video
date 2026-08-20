namespace GMTFV.tools {
    /// <summary>
    /// 영상 / 음원 저장 타입 정보를 담는 클래스
    /// </summary>
    public class TypeSaveVideo {
        /// <summary>
        /// true  = 영상
        /// false = 오디오
        /// </summary>
        public bool IsTypeVideo { get; set; }

        /// <summary>
        /// mp4, mp3 등 세부 포맷
        /// </summary>
        public string SubType { get; set; }
    }
}
