# GMTFV 4.0 아키텍처

## 프로젝트

```text
src/GMTFV.Wpf (net10.0-windows, MVVM)
  ├─ GMTFV.Core (net472;net10.0)
  └─ GMTFV.DownloadEngine (net472;net10.0-windows)
       ├─ GMTFV.Core
       └─ Newtonsoft.Json 13.0.4 (PackageReference)
tests/GMTFV.Tests (net10.0-windows, Core/Engine 회귀 검사)
tests/GMTFV.Wpf.Tests (net10.0-windows, STA Dispatcher 검사)
publish.ps1 → Windows x64 자체 포함 ZIP / setup.iss 설치본 / SHA-256 목록
```

4.0부터 WinForms와 전용 배포 스크립트·Chrome 확장을 제거했다. 기존 엔진의 .NET Framework 호환 타깃은 유지한다. 구버전 소스는 Git 이력과 3.x 릴리즈에서 확인할 수 있다.

## 책임과 경계

| 구성 요소 | 역할 |
| --- | --- |
| Core.YouTubeUrl / WindowsFileName | URL 정규화·검증, 파일명 템플릿·경로·금지 문자 |
| Core.DownloadQueueService / DownloadModels | 동시 슬롯·대기 취소, UI 독립 요청·상태·오류 |
| Engine.ProcessRunner / ProcessJob | 비동기 stdout/stderr, 종료 코드·시간 초과·취소·소유 프로세스 트리 정리 |
| Engine.VideoMetadataService / SubtitleService | 메타데이터·실제 포맷·자막 조회와 저장 |
| Engine.YtDlpDownloadCommandFactory / YtDlpClient | 공통 도구 옵션, 영상·오디오·화질·인코더 인수 생성 |
| Engine.AudioProcessor / MediaProbe | 실제 파일의 코덱·컨테이너 조사, Direct Save / Stream Copy / Transcode, 직접 관리 FFmpeg의 out_time_us 진행률 |
| Engine.DownloadService / CompletedFile / RecoveryPolicy | 다운로드·제한된 복구·최종 검증·덮어쓰기 없는 파일 게시 |
| Engine.DiagnosticLog / ToolDiagnostics | 마스킹된 로그·회전, 외부 도구 버전·호환성 확인 |
| WPF.MainWindowViewModel / DownloadItemViewModel | 큐·선택 작업·번호·순서·화면 상태·엔진 요청 조립 |
| WPF.ItemSettingsViewModel / SettingsStore | 영상별 옵션·자막과 기존 WPF 설정의 안전한 저장 |
| WPF.QueueListStore | 크기 제한·JSON/레거시 목록 해석, 비동기·원자적 목록 저장, 취소 |
| WPF.ToolTransfer / ToolSetupDiagnostics | 연결·무응답·전체 제한 시간, HTTP/Windows 오류 진단, Retry-After와 제한된 지수 재시도 |
| WPF.ToolWorkspaceViewModel / ToolSetupService | 공식 배포 도구 검증, 별도 묶음 준비·취소·전환 |
| WPF.AppUpdateViewModel / AppUpdateService | 시작 시 정식 앱 릴리즈 확인, 설치본 다운로드·검증·취소 |
| WPF.Motion / QueueMotion / SmoothScroll / DownloadJourney | 상태 모션·실제 생성 행의 이동·가상화 스크롤·진행 단계 표현 |

UI → ViewModel → IDownloadBackend → Core/Engine 순서다. 컨트롤이 다운로드 프로세스나 엔진을 직접 제어하지 않는다. 네트워크와 파일 I/O는 비동기로 실행하며, 종료 시 앱 수명 토큰을 취소하고 진행 중 작업이 정리될 때까지 기다린다.

## 다운로드·파일 정책

- watch/shorts/live/짧은 URL을 정규화한다. 쿠키 자동 수집이나 접근 제한 우회를 제공하지 않는다.
- 정보·자막·다운로드는 동일한 yt-dlp 계층과 FFmpeg/Deno 경로를 사용한다.
- MP4/MKV/MOV/WebM은 병합·remux, AVI/FLV만 영상 인코더를 적용한다. OGG는 실제 Vorbis 원본을 보존하며 다른 코덱만 Vorbis로 변환한다. 오디오는 yt-dlp가 다운로드한 뒤 FFprobe 정보로 처리 방식을 결정한다. 원본 선택은 bestaudio 전용이며 실제 확장자로 게시한다.
- 품질 폴백은 요청 해상도 이하로 제한한다. 끄면 해상도/FPS를 정확히 요구한다.
- 네트워크 복구는 최대 총 3회이며 403·PO 공급자·화질·GPU 복구는 첫 실패에서 한 번만 시도한다. 429·권한·공간·접근 제한·알 수 없는 오류는 반복하지 않는다.
- 저장 폴더의 `.gmtfv-<작업ID>/`에서 실행한다. 종료 코드·실제 완료 경로·작업 폴더 경계·확장자·파일 서명을 검증한 뒤 최종 폴더에 게시한다. 기존 파일은 `(1)` 등의 이름으로 보존한다.
- 실패·취소 작업 파일은 남긴다. 자동 복구는 같은 작업에서 이어받고 수동 재시도는 새 폴더에서 시작한다.
- 완료는 헤더·경로·FFprobe 코덱/컨테이너/스트림·길이 검사 후에만 100%다. 직접 실행한 FFmpeg는 out_time_us/실제 길이 비율을 사용하고, yt-dlp 내부 병합·후처리는 퍼센트 없이 현재 단계와 경과 시간을 표시한다. 진행 이벤트는 같은 스트림·단계 내에서 200ms로 제한한다.
- 스트리밍 stdout/stderr는 64KiB 꼬리만 보존한다. 메타데이터 stdout은 전체 JSON을 16MiB까지 보존하고 넘으면 명시적으로 실패한다. 줄 하나가 매우 길어도 스트리밍 줄 버퍼를 제한한다.
- 큐 순서 변경은 활성 다운로드와 도구 준비·제거 중에 잠근다. 선택 항목 간 상대 순서와 목록 저장 순서를 유지한다.

## 설정·도구·앱 업데이트

설정과 로그는 기존 `%LocalAppData%/GMTFV.Preview`를 유지한다. WinForms 설정 파일이나 다운로드 파일을 제거하거나 가져오지 않는다. 기존 `%AppData%/GMTFV` 도구를 읽을 수 있으며 새 도구 묶음은 WPF 전용 Tools 하위에 별도로 준비한다. 도구는 경로·쓰기·이동 권한·최소 1GiB 여유 공간을 사전 검사한다. SHA-256을 검증하고 실행 확인을 마친 새 폴더로 전환한다. 검증 캐시는 digest별 파일을 다시 해시한 뒤 재사용하며 이전 묶음과 함께 보존한다. Range/ETag가 검증되지 않은 부분 전송은 재사용하지 않는다. 설치 전송은 연결 15초, 응답 헤더 30초, 무응답 30초, 전체 15분이며 최대 3회다. Retry-After가 60초를 넘으면 자동 대기를 중단하고 재시도 가능 시간을 안내한다. 사용 중 공유 실행 파일을 교체하지 않는다.

앱 자동 업데이트는 공식 저장소의 GitHub `releases/latest`만 조회한다. 정식 새 버전, 정확한 설치 파일명·저장소 주소, 양의 제한된 크기, SHA-256 digest를 확인한다. 전송 후 크기와 해시를 검증하고 실행 직전에 다시 검증한다. 메타데이터는 2MiB, 설치본은 512MiB, 확인은 20초, 다운로드는 15분으로 제한한다. 실패·취소 시 해당 작업의 GUID staging 폴더만 정리한다. API digest 형식은 [GitHub 공식 문서](https://docs.github.com/en/rest/releases/assets)를 따른다.

업데이트 발견은 비차단 표시이며 설치는 사용자가 선택한다. 미디어·메타데이터·자막·도구 작업 중에는 설치하지 않고, 다운로드 중 작업이 시작돼도 실행 직전에 다시 확인한다. 설치 전 설정과 큐 URL·옵션을 저장하고 `/DIR`에 현재 실행 위치를 전달해 설치 프로그램을 연 뒤 정상 종료 흐름을 사용한다. 다음 실행에 `update-queue.json`을 복원하고 `.restored` 백업을 남긴다. 큐의 완료·실패·진행 상태는 복원하지 않는다.

설치 AppId는 3.x와 동일하다. 기존 WinForms 실행 파일과 앱 구성 파일만 설치 위치에서 정리한다. 사용자 설정·도구·다운로드 폴더를 삭제하는 제거 규칙은 없다. 배포본은 .NET 10/Windows Desktop Runtime과 해당 라이선스를 포함한다.

## 검증

Core/Engine 콘솔 검사는 .NET 10에서 URL·명령·취소·프로세스·복구·출력 검증을 실행한다. WPF 검사는 실제 오프스크린 창과 바인딩·Dispatcher·가상화·모션·업데이트 대역을 사용한다. `tests/media_smoke.py`는 실제 yt-dlp/FFmpeg와 자체 생성 미디어로 형식을 확인한다. 실서비스 YouTube 성공률, GPU, 다양한 DPI, 물리 입력과 관리자/UAC 환경은 별도 검증 대상이다. 이번 배포 결과는 [4.0.0.0 기록](release-4.0.0.0.md)에 구분한다.
4.0.1.0 검증과 남은 환경 제약은 [안정성 개선 보고서](stability-4.0.1.0.md)에 기록한다.
