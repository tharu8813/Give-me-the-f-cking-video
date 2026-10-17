# GMTFV 아키텍처

## 프로젝트

```text
src/GMTFV.WinForms/GMTFV.WinForms.csproj (WinForms, net472)
  ├─ src/GMTFV.Core (net472, UI·프로세스 의존성 없음)
  └─ src/GMTFV.DownloadEngine (net472)
       ├─ GMTFV.Core
       └─ Newtonsoft.Json (기존 고정 버전)
tests/GMTFV.Tests (콘솔 테스트, 추가 패키지 없음)
```

모든 소비자가 Windows/.NET Framework이므로 타깃을 유지했다. .NET Standard나 다른 런타임 호환성을 이번 빌드에서 검증한 것은 아니다. 기존 `Start/`, `Properties/` 경로와 사용자 설정 키, JSON/TXT 목록 스키마를 유지한다. `services/` 어댑터는 썸네일 Image를 포함한 기존 VideoInfo를 UI 독립 DownloadRequest로 변환한다.

## 책임

| 구성 요소 | 역할 |
| --- | --- |
| Core.YouTubeUrl | 호스트·경로·11자리 영상 ID 검증, watch/shorts/live/짧은 URL 정규화 |
| Core.WindowsFileName | 템플릿·확장자·Windows 금지 문자·예약 이름·경로 길이 |
| Core.DownloadQueueService | 동시 슬롯 임대, 대기 취소, 중복 반환 방지, 마지막 사용자 종료 후 Dispose |
| Core.DownloadModels | 요청·오류·상태·nullable 진행률 |
| Engine.ProcessRunner / ProcessJob | stdout/stderr 동시 비동기 처리, 종료 코드, 타임아웃, 취소, 소유한 프로세스 트리 종료 |
| Engine.CommandLine | Windows CRT 따옴표·공백·역슬래시 인코딩 |
| Engine.YtDlpClient | 조회·자막·다운로드의 공통 실행 옵션과 Deno/FFmpeg 경로 |
| Engine.VideoMetadataService / SubtitleService | JSON 파싱·소수 FPS·자막 조회 및 저장 |
| Engine.YtDlpDownloadCommandFactory | 인수 목록 생성, 형식·화질·인코더 검증 |
| Engine.RecoveryPolicy | 오류 분류, 제한된 복구·백오프, 사용자 메시지 |
| Engine.DownloadService | 준비→실행→선택적 재시도→검증→게시→완료 |
| Engine.CompletedFile | 실제 완료 경로·경계·확장자·서명 검증, 덮어쓰기 없는 게시 |
| Engine.DiagnosticLog / ToolDiagnostics | 버전·상태·전략·시도·오류·종료 코드, 민감값 마스킹·로그 회전 |
| tools.YtDlpTool / DenoTool / Tol | 사용자 도구 폴더 부트스트랩 및 검증된 업데이트 |

WinForms는 입력·목록·표시를 담당한다. 프로세스 실행 문법과 파일 성공 판정은 엔진에 있다. MainForm의 작업 편성은 UI 스레드에서 실행하고, 슬롯과 프로세스 I/O는 별도 서비스가 관리한다. 단일 작업 토큰을 전체 작업 및 앱 수명 토큰에 연결한다.

## 다운로드 정책

- 특정 YouTube 클라이언트를 강제하지 않는다. `--ignore-config`로 사용자 전역 옵션 간섭을 차단한다. 쿠키 자동 수집이나 접근 제한 우회 옵션은 없다.
- 정보 조회·자막·다운로드 모두 같은 yt-dlp 실행 계층, 소켓 타임아웃, Deno/FFmpeg 경로를 사용한다.
- MP4/MKV/MOV/WebM은 병합·remux만 수행한다. AVI/FLV에 필요한 변환에만 인코더를 적용한다. 오디오는 공식 추출 옵션을 사용하고 OGG를 vorbis에 대응한다.
- 기본 화질 폴백은 요청 해상도를 넘지 않는다. 고급 설정에서 끄면 선택 해상도/FPS를 정확히 요구한다.
- 네트워크/응답 지연은 최대 총 3회, 403·PO 공급자·화질·GPU 복구는 첫 실패에 한 번만 시도한다. 백오프는 2/4초, 계산 상한은 30초다. 429·권한·디스크 부족·접근 제한·알 수 없는 오류는 반복하지 않는다.
- GPU 변환 실패 시 불완전한 변환 목적 파일만 정리하고 원본을 유지해 CPU로 복구한다.
- 진행률 100%는 최종 파일 검증 이후에만 표시한다. 측정되지 않은 병합·변환 진행률과 ETA를 만들지 않는다.

## 파일·취소

저장 폴더 아래 `.gmtfv-<작업ID>/media.%(ext)s`에서 실행한다. 종료 코드 0과 `after_move` 경로, 작업 폴더 경계, 요청 확장자, 최소 크기, 컨테이너 헤더 서명을 확인한 뒤 최종 폴더로 이동한다. 매 파일의 전체 디코딩이나 무거운 미디어 분석은 하지 않는다. 동일 이름은 `File.Move`의 덮어쓰기 금지를 이용해 `(1)` 등으로 번호를 붙인다. 기존 파일을 성공 판정에 사용하지 않는다.

같은 작업의 자동 재시도는 `.part`를 이어받는다. 실패·취소 시 작업 폴더를 보존한다. 수동 재시도는 새 폴더에서 시작하며, 앱 종료 후 필요 없는 `.gmtfv-*` 폴더는 사용자가 정리할 수 있다. 성공 시 빈 폴더만 삭제한다. 자막도 별도 작업 폴더에서 새로 생성된 파일만 게시한다.

Windows Job Object를 닫아 직접 생성한 자식까지 종료한다. 프로세스 이름으로 다른 앱을 종료하지 않는다. 폼 종료 시 수명 토큰을 취소하며, 작업이 자기 CTS와 슬롯을 정리한다. 대기 중 취소로 슬롯을 잘못 반환하지 않는다.

## 도구·설정·배포

기존 `%AppData%/GMTFV` 도구 폴더와 사용자 Settings 키를 유지하고 `AllowQualityFallback`만 기본 true로 추가했다. 기존 yt-dlp는 즉시 준비 상태로 사용하고, 버전 확인·업데이트는 뒤에서 한다. 변경된 릴리스만 SHA-256 검증 후 원자적으로 교체한다. 실패하면 현재 실행 파일을 보존한다.

Deno는 도구 폴더 또는 PATH에서 찾고 최소 2.3.0을 확인한다. 없거나 호환되지 않으면 공식 Windows x64 배포본의 GitHub digest를 검증한 뒤 준비한다. 실패를 영구 캐시하지 않는다. 준비 실패로 앱을 강제 종료하지 않는다. EJS는 공식 yt-dlp 실행 파일의 번들을 사용하며 원격 스크립트 옵션은 추가하지 않았다. 기본 배포본은 별도 PO Token 공급자 서버를 시작하지 않는다. 저장소의 선택적 플러그인 자료는 기본 설치본에 포함하지 않는다.

로그는 `%LocalAppData%/GMTFV/Logs`에 기록한다. 작업 ID, UTC 시각, 앱·도구 버전, 단계, 전략, 시도, 오류, 종료 코드, 제한된 stderr를 남긴다. URL·인증값·사용자 홈 경로를 마스킹한다. 2MiB 로그와 이전 파일 하나를 보관한다.

설치 AppId와 DLLs probing 구조는 유지한다. Inno Setup의 기본 업그레이드·제거를 사용하고 설치 폴더 전체 삭제나 구버전 제거 프로그램 강제 실행은 하지 않는다. 외부 도구는 사용자 폴더에 별도로 준비하며 라이선스 고지를 동봉한다.

## 검증

`GMTFV.slnx` Debug/Release 빌드 후 `tests/GMTFV.Tests/bin/<Configuration>/GMTFV.Tests.exe`를 실행한다. 가짜 프로세스 테스트도 실제 OS 파이프·종료 코드·프로세스 트리·타임아웃을 사용한다. `tests/media_smoke.py`는 자체 생성 미디어와 루프백 서버로 실제 도구를 검사한다. 외부 YouTube 성공률, GPU 하드웨어, 실제 설치·제거, Chrome 브라우저 조작은 별도 실환경 검증 대상이다.
