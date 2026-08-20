# P6 — URL 및 메타데이터 흐름

## 구현 내용

1. 사용자는 메인 입력창에 YouTube URL을 붙여넣고 `Enter` 또는 `URL 추가`를 누른다.
2. `YouTubeUrl.Normalize`이 단일 영상 URL을 검증하고 정규화한다.
3. 동일한 URL, 동일한 영상 ID, 이미 조회 중인 URL을 모두 중복으로 막는다.
4. WPF 전용 `YtDlpMetadataService`가 포함된 `yt-dlp.exe`로 JSON 메타데이터를 조회한다.
5. 성공하면 제목, 채널, 길이, 썸네일 URL, 사용 가능한 화질을 `DownloadItemViewModel`에 보관하고 가상화 목록에 추가한다.
6. 실패하면 목록을 손상시키지 않고 입력 영역의 상태 메시지에 사용자용 오류를 표시한다.

## 도구 경계

P6에서는 WPF 출력 폴더에 `yt-dlp.exe`를 복사해 메타데이터 조회만 연결한다. FFmpeg, PO Token 공급자, 실제 다운로드·병합은 P7에서 같은 원칙으로 연결한다.

## P7 연결점

- `DownloadItemViewModel.AvailableQualities`는 다운로드 형식 선택에 사용한다.
- `Phase`, `ProgressPercent`, `StatusText`는 `DownloadProgress`를 받아 실제 진행 상태로 갱신한다.
