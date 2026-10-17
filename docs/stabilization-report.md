# GMTFV 안정화 작업 결과

검증일: 2026-10-18 (한국 시간). 기준 커밋: `064a87b`, 작업 브랜치: `codex/stabilize-winforms-20261018`. 시작 시 Git 작업 트리는 깨끗했다. 검증은 커밋 전 작업 트리에서 수행했으며 태그·릴리스 업로드는 수행하지 않았다.

## 1. 전체 작업 요약

WinForms 사용자 흐름과 .NET Framework 4.7.2를 유지하면서 Core/DownloadEngine을 분리했다. 정보 조회·자막·다운로드의 yt-dlp 실행 경로를 통합하고, 오류별 복구, 프로세스 트리 취소, 실제 완료 파일 검증, 충돌 없는 저장, 진단 로그를 구현했다. 기존 설정 키와 도구 폴더, JSON/TXT 목록 스키마는 유지했다.

관련 없는 대규모 소스 이동, DI 프레임워크, 신규 외부 패키지, 새 UI 프로젝트는 추가하지 않았다. 불필요한 NuGet 패키지 11개를 제거하고, 리소스 로딩 및 기존 UI에 필요한 패키지는 같은 버전으로 유지했다.

## 2. WPF 삭제 결과

- `src/GMTFV.Wpf/`의 추적 파일 14개, 솔루션 프로젝트 항목, WinForms의 WebView2 WPF 참조와 Preview 안내를 제거했다.
- 이 기준 커밋에는 `setup_wpf.iss`, `docs/wpf-migration/`, `PoTokenProviderService.cs`, 기존 `GMTFV.Core`가 없었다. 없는 파일을 삭제했다고 기록하지 않는다.
- Core를 새로 만들었으며 향후 UI가 참조할 수 있는 독립 엔진을 구성했다. 기존 공유 라이브러리나 Git 기록·과거 릴리스는 삭제하지 않았다.
- 실행·빌드·배포 소스에서 남은 WPF 참조를 검색했다. 이 보고서의 과거 파일명 및 NuGet 캐시 안의 공급자 패키지 구성 요소는 활성 프로젝트 참조가 아니다.

## 3. 주요 문제와 수정

P0로 확정한 결함은 없다. 다음은 실제 소스 및 테스트로 확인한 P1/P2 항목이다.

| 우선순위 | 문제·원인 | 수정 |
| --- | --- | --- |
| P1 | 같은 FFmpeg 키의 postprocessor 인자를 여러 번 생성하여 인코더와 진행 옵션이 충돌 | 명령 생성 통합, 필요한 변환에만 PP/실행 파일을 특정한 인수 사용 |
| P1 | 출력 파일명을 추측해 기존 MKV/WebM 등의 존재를 성공으로 인정 | 전용 작업 폴더와 after_move 경로·종료 코드·확장자·헤더 검사 |
| P1 | 다운로드와 메타데이터 실행기가 분리되어 취소·타임아웃·출력 처리 불일치 | 공통 ProcessRunner/YtDlpClient, stdout/stderr 동시 비동기 수집 |
| P1 | 부모 프로세스만 Kill하여 FFmpeg/Deno 자식이 남을 수 있음 | 소유한 Windows Job Object로 트리 종료, 미사용 동명 전체 종료 함수 제거 |
| P1 | Task.Run에서 UI 목록·상태 맵을 함께 변경하고 슬롯 반환·Dispose가 느슨함 | UI 스레드에서 작업 편성, 임대 슬롯 검사·대기 취소·유휴 Dispose |
| P1 | 설치 시 구버전 제거 프로그램 강제 실행 및 설치 폴더 전체 삭제 | 동일 AppId와 Inno 기본 업그레이드·제거 사용, 강제 종료/전체 삭제 코드 제거 |
| P1 | 도구 업데이트에서 기존 exe를 먼저 삭제하면 교체 실패 때 도구가 사라짐 | 검증 파일의 원자적 교체와 교체 실패 시 현재 파일 보존 |
| P1 | FFmpeg 준비 시 ffprobe 미복사 | 같은 검증 배포본에서 두 실행 파일 준비 및 라이선스 문서 보관 |
| P1 | 목록 조회 실패·중복 추가 후 직전 영상에 설정을 적용하거나 JSON 필터링 후 인덱스 불일치 | 유효 레코드를 정렬된 상태로 유지, 새로 추가된 동일 영상 확인 후 적용 |
| P1 | 실제 AVI 변환에서 yt-dlp의 XVID 태그와 H.264 인코더 충돌 | AVI 변환 시 H264 태그 명시. 실제 FFmpeg로 회귀 검증 |
| P1 | 실제 AAC 요청이 M4A 컨테이너로 남는 yt-dlp 동작 | 오디오 추출 후 필요할 때 AAC 컨테이너로 remux |
| P2 | `%ext%` 템플릿 확장자 중복, Windows 예약 이름·마지막 점/공백·긴 경로 문제 | 공통 파일명 규칙 및 경로 예산, 확장자 한 번만 부착 |
| P2 | 저장 폴더의 `%`가 yt-dlp 템플릿으로 해석될 수 있음 | 작업 출력 경로의 리터럴 `%` 이스케이프 |
| P2 | 느슨한 URL 정규식 및 누락 ID의 임의 GUID 생성 | URI 호스트/경로/11자리 ID 검증, 정규화와 중복 판정 공유 |
| P2 | 병합·변환 중 임의 95/98/99% 및 파일 검증 전 완료 표시 | 실제 다운로드 수치만 표시, 미확인 단계는 marquee, 검증 후 100% |
| P2 | Chrome 확장은 ZIP만 복사되어 설치 안내 경로에 manifest가 없음 | 빌드 시 해제해 manifest와 스크립트를 설치본에 포함 |
| P2 | 업데이트 파일을 setup.exe라는 넓은 조건으로 선택 | 정확한 WinForms 릴리스 파일명과 SHA-256 검증 유지 |

## 4. 다운로드 처리 변경

이전에는 UI가 직접 프로세스를 실행하고 파일명 후보를 검색했다. 현재는 검증된 DownloadRequest를 엔진에 전달한다.

`준비 → 다운로드 → 필요 시 제한된 복구 → 완료 경로 검증 → 저장 폴더 게시 → 완료`

- 기본 YouTube 클라이언트를 사용한다. 쿠키·인증정보를 자동 수집하지 않는다.
- 네트워크/응답 지연은 최대 총 3회, 첫 403·PO 공급자·허용된 포맷 폴백·GPU 오류는 한 번 복구한다. 429·권한·디스크·접근 제한·알 수 없는 오류는 자동 반복하지 않는다.
- MP4/MKV/MOV/WebM은 병합/remux하고 AVI/FLV에 필요한 변환에만 인코더를 적용한다. MP4/MOV는 해상도·FPS를 우선하고 같은 조건에서 코덱 호환성을 선호한다.
- 최종 파일과 요청 확장자·작업 폴더 경계·최소 크기·컨테이너 헤더를 확인한다. 매번 전체 디코딩하지는 않는다.
- 같은 이름은 번호를 붙여 보존한다. 자동 재시도는 같은 작업 폴더에서 이어받고, 실패·취소 시 임시 폴더를 보존한다. 수동 재시도는 새 폴더에서 시작한다.

## 5. 아키텍처와 핵심 파일

- `src/GMTFV.Core/`: DownloadRequest/Progress/State/Error, YouTubeUrl, WindowsFileName, DownloadQueueService. UI·프로세스 의존성이 없다.
- `src/GMTFV.DownloadEngine/ProcessRunner.cs`, `ProcessJob.cs`, `CommandLine.cs`: 프로세스·파이프·인수·취소.
- `src/GMTFV.DownloadEngine/YtDlpClient.cs`, `YtDlpDownloadCommandFactory.cs`: 공통 실행 옵션과 명령 생성.
- `src/GMTFV.DownloadEngine/DownloadService.cs`, `RecoveryPolicy.cs`, `CompletedFile.cs`: 실행·복구·최종 파일 판정.
- `src/GMTFV.DownloadEngine/VideoMetadataService.cs`, `SubtitleService.cs`: 조회와 자막.
- `src/GMTFV.DownloadEngine/DiagnosticLog.cs`, `ToolDiagnostics.cs`: 진단·도구 호환성.
- `Start/MainForm.cs`, `services/`: 기존 모델과 UI의 어댑터. 상세 구조는 `docs/architecture.md` 참고.
- `tools/YtDlpTool.cs`, `DenoTool.cs`, `Tol.cs`, `GitHubUpdater.cs`: 도구·앱 업데이트와 부트스트랩.

Core와 엔진도 net472를 사용한다. .NET Standard 2.0 타깃을 추가하거나 다른 런타임 호환성을 검증하지는 않았다. 새 의존성 패키지는 없다.

## 6. UI/UX

기존 화면 배치와 버튼 의미를 유지했다. URL 입력에 여러 줄과 Enter 추가, 메인 Ctrl+V의 여러 URL 처리, 메타데이터 조회 재시도, 실패 항목만 재시도, 선택 취소와 로그 열기를 연결했다. 다운로드 중 새로 받은 Chrome/클립보드 URL은 중복 제거 후 종료 시 추가한다.

목록에는 화질/형식과 상태 열을 추가했고, 기존 열 너비를 조정해 제목 공간을 확보했다. 고급 설정에 화질 하향 허용을 추가했다. GPU는 AVI/FLV 변환에 적용됨을 표시했다. 화면 렌더링은 시작 다운로드와 실제 설정 저장을 해제한 오프스크린 테스트로 확인했다.

## 7. 테스트 결과

| 검사 | 결과 |
| --- | --- |
| Debug 자동 테스트 | 86 통과, 0 실패 |
| Release 자동 테스트 | 86 통과, 0 실패 |
| 실제 yt-dlp/FFmpeg 로컬 형식 통합 | 13 통과, 0 실패 |
| diff 공백 오류 검사 | 통과 |

자동 테스트는 URL·중복·파일명·예약 이름·경로·명령·화질·오류 분류·복구 종료·취소·슬롯·상태 순서·종료 코드·출력 경로·충돌·JSON·누락 도구·타임아웃·로그·체크섬·교체 실패 보존·기존 목록 FPS·Chrome 자산·WinForms 렌더링을 포함한다.

가짜 프로세스도 실제 OS에서 실행하며 stdout/stderr 동시 배출, 잘못된 종료 코드, 지연, 자식 생성, 취소를 재현했다. 무관한 프로세스가 살아 있는지도 확인했다.

로컬 통합은 직접 생성한 1초 160x120 영상 및 음원을 루프백 서버로 제공했다. C# 명령 팩토리의 실제 옵션을 사용하되 입력 metadata만 자체 생성 fixture로 대체했다. MP4/MKV/AVI/MOV/WebM/FLV 및 MP3/M4A/AAC/Opus/OGG/WAV/FLAC을 실제 도구로 생성하고 FFmpeg로 전체 디코딩했다. MP4/MKV/MOV는 선택된 원본과 디코딩 프레임 해시가 같았다. 사용한 yt-dlp는 `2026.07.04`, FFmpeg는 `8.1.1`이었다.

샌드박스가 루프백 접속을 차단해 이 로컬 테스트만 제한 밖에서 실행했다. YouTube 서버에는 접속하지 않았다. 이 결과를 실제 YouTube 다운로드 성공률 개선 실측으로 해석하면 안 된다.

실행 로그는 작업 트리의 `artifacts/Debug-tests.log`, `Release-tests.log`, `media-smoke.log`에 있다. 스크린샷은 `tests/GMTFV.Tests/bin/Debug/ui-*.png`에 있다. 테스트 출력은 Git에 포함하지 않는다.

## 8. 빌드·배포

- Visual Studio MSBuild로 net472 WinForms/Core/Engine 및 테스트 프로젝트의 Debug/Release 빌드를 수행했다.
- 최종 빌드: 경고 0, 오류 0. 리소스 로딩·DLLs probing·엔진 DLL 및 WebView2 native 런타임 포함을 확인했다.
- NuGet 복원은 종료 코드 0이었다. 다만 샌드박스에서 NuGet 취약성 정보 서버에 접속하지 못해 NU1900 경고가 있었다. 온라인 취약점 검사는 완료로 간주하지 않는다.
- Inno Setup 6.7.3으로 실제 컴파일했다. `output-setup/GMTFV-3.4.0.0-setup.exe`를 생성했다. 버전과 AppId를 임의 변경하지 않았다.
- 설치본에는 관리 DLL, 설정, Chrome manifest/스크립트, 라이선스 고지가 포함된다. 외부 도구는 사용자 폴더에 별도로 준비하는 기존 배포 정책을 따른다.
- 설치 스크립트의 전체 폴더 삭제·구버전 제거 강제 실행·강제 프로세스 종료 코드는 제거했다. 실제 시스템 설치·업그레이드·제거는 실행하지 않았다.

## 9. 잔여 검증과 제한

- 실제 권한이 있는 YouTube 영상의 4K/고프레임·인증·지역별 응답·네트워크 중단과 성공률은 미측정이다.
- NVIDIA/AMD/Intel 실제 GPU 드라이버와 CPU 폴백의 하드웨어 실측은 하지 않았다. 정책과 생성 명령은 자동 테스트로 확인했다.
- Deno 공식 배포 다운로드·설치와 yt-dlp/FFmpeg의 온라인 자동 업데이트는 사용자 환경에서 추가 확인해야 한다. 체크섬과 교체 보존 로직은 오프라인 테스트로 확인했다.
- Chrome의 실제 UI에서 확장을 설치하고 탭을 전송하는 E2E는 미실행이다. manifest/배포 자산·URL 파서는 검증했다.
- 설치·제거·기존 설치본 업그레이드의 실제 OS 검증은 미실행이다.
- 실패·취소 임시 폴더는 자동 전체 삭제하지 않는다. 필요하면 작업 종료 후 사용자가 정리해야 한다.
- 운영 시 완료 검증은 가벼운 컨테이너 서명 검사이며 전체 미디어 손상을 모두 검출하는 검사는 아니다.

## 10. 추가 권장 사항

릴리스 전 별도 Windows 테스트 계정 또는 VM에서 실제 설치/기존 설정 승계/제거를 확인하고, 사용 권한이 명확한 영상 세트로 해상도·FPS·형식별 성공/실패를 기록하는 것을 권장한다. Deno 첫 설치, 도구 온라인 업데이트, Chrome E2E, 실제 GPU 매트릭스 및 NuGet 온라인 취약점 검사도 그때 확인해야 한다. 측정 전에는 다운로드 성공률이 높아졌다고 수치로 홍보하지 않는다.

공식 옵션 근거: [yt-dlp README](https://github.com/yt-dlp/yt-dlp/blob/master/README.md), [EJS](https://github.com/yt-dlp/yt-dlp/wiki/EJS), [PO Token Guide](https://github.com/yt-dlp/yt-dlp/wiki/PO-Token-Guide). 실제 설치 버전의 `--version`, `--help`도 확인했다.

## 커밋·푸시 시점의 원격 상태

사용자의 커밋·푸시 요청 후 fetch한 원격 `master`는 `482063b`였다. 기준 커밋 이후 WinForms 경로 이동과 별도 Core 및 배포 변경이 추가되어 있으므로 이 검증본을 원격 master에 덮어쓰거나 임의 병합하지 않고 별도 작업 브랜치로 보존한다. 최신 master와의 통합은 충돌 해결 및 재검증이 필요한 별도 작업이다.
