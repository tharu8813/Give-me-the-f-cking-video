# GMTFV 구조 안내

## 계층

- `Start/`: WinForms 화면과 사용자 상호작용. 화면 상태를 갱신하고 서비스 호출 결과를 표시합니다.
- `services/`: UI와 독립적인 애플리케이션 로직. 파일명 생성, yt-dlp 인자 생성처럼 단위 테스트 가능한 규칙을 둡니다.
- `tools/`: 외부 도구(yt-dlp, FFmpeg), 네트워크, 운영체제 리소스와의 통합 코드입니다.
- `models/`: 화면과 서비스가 공유하는 데이터 모델입니다.
- `Properties/`: 애플리케이션 리소스와 사용자 설정입니다.

## 변경 원칙

1. WinForms 이벤트 핸들러에는 화면 제어와 서비스 조립만 둡니다.
2. 문자열 조합, 설정 검증, 파일명·명령 생성 규칙은 `services/`로 분리합니다.
3. 프로세스 실행, 파일 시스템, HTTP 호출은 `tools/`에서만 수행합니다.
4. 새 기능은 먼저 입력과 출력이 명확한 서비스 클래스로 작성하고, 마지막에 Form에서 연결합니다.

## 현재 분리된 서비스

- `FileNameTemplate`: 다운로드 파일명 템플릿을 해석하고 파일명으로 변환합니다.
- `YtDlpDownloadCommandFactory`: `VideoInfo`와 설정을 검증해 yt-dlp 다운로드 명령을 생성합니다.

## 다음 분리 후보

- 영상 메타데이터 조회 및 JSON 파싱 (`MainForm.AddVideoAsync`)
- 다운로드 큐/동시성 관리 (`MainForm.button4_Click`)
- FFmpeg·yt-dlp 설치과 업데이트를 담당하는 도구 부트스트래퍼
