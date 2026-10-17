using System;
using System.Collections.Generic;
using System.Drawing;

namespace GMTFV.tools {
    /// <summary>
    /// 유튜브 영상 정보를 담는 모델 클래스
    /// </summary>
    public class VideoInfo {
        /// <summary>
        /// 영상 ID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// 썸네일 이미지
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// 영상 제목
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 채널(작성자) 이름
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 영상 길이
        /// </summary>
        public TimeSpan VideoLength { get; set; }

        /// <summary>
        /// 업로드 날짜
        /// </summary>
        public DateTimeOffset UploadDate { get; set; }

        /// <summary>
        /// 사용 가능한 영상 화질 목록
        /// </summary>
        public List<VideoQuality> VideoQualities { get; set; } = new List<VideoQuality>();

        /// <summary>
        /// 저장 타입 (영상 / 오디오 + 포맷)
        /// </summary>
        public TypeSaveVideo TypeSave { get; set; }

        public string Tag { get; set; }
    }
}
