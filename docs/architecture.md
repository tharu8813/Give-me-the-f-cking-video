# GMTFV 구조 안내

## 계층

- `src/GMTFV.WinForms/`: 기존 안정판입니다. `Start`, `services`, `tools`, `models`, `Properties`를 포함합니다.
- `src/GMTFV.Core/`: WinForms와 WPF가 함께 참조하는 UI 독립 계약 및 동시성·URL 규칙입니다.
- `src/GMTFV.Wpf/`: 최신 .NET 기반의 새 UI이며 현재 Preview로 배포합니다.
- 저장소 루트: 로컬 배포 준비 시 두 UI가 함께 사용하는 yt-dlp, FFmpeg, Deno, PO 공급자를 배치합니다. 대용량 실행 파일은 Git에 포함하지 않으며 Chrome 확장, 아이콘과 라이선스 자료만 추적합니다.
- `setup_winform.iss`, `setup_wpf.iss`: 안정판과 Preview를 별도 제품으로 패키징합니다.

## 변경 원칙

1. WinForms 이벤트 핸들러에는 화면 제어와 서비스 조립만 둡니다.
2. 문자열 조합, 설정 검증, 파일명·명령 생성 규칙은 `services/`로 분리합니다.
3. 프로세스 실행, 파일 시스템, HTTP 호출은 `tools/`에서만 수행합니다.
4. 새 기능은 먼저 입력과 출력이 명확한 서비스 클래스로 작성하고, 마지막에 Form에서 연결합니다.
5. 두 UI에서 재사용할 규칙과 계약은 `GMTFV.Core`로 이동하고 UI 프레임워크 형식은 Core에 넣지 않습니다.

## 현재 분리된 서비스

- `FileNameTemplate`: 다운로드 파일명 템플릿을 해석하고 파일명으로 변환합니다.
- `YtDlpDownloadCommandFactory`: `VideoInfo`와 설정을 검증해 yt-dlp 다운로드 명령을 생성합니다.
- `GMTFV.Core`: YouTube URL 정규화, 다운로드 진행 계약, 목록 항목 계약, 다운로드 슬롯 큐를 제공합니다.

## 다음 분리 후보

- 영상 메타데이터 조회 및 JSON 파싱 (`MainForm.AddVideoAsync`)
- 다운로드 큐/동시성 관리 (`MainForm.button4_Click`)
- FFmpeg·yt-dlp 설치과 업데이트를 담당하는 도구 부트스트래퍼

## WPF 전환 기준

WPF 전환은 WinForms를 대체하는 단계적 작업으로 진행합니다. 현재 기능·화면·수동 검증 기준은 아래 P1 문서를 따릅니다. P2 이후의 구조 변경과 WPF 구현은 이 기준을 충족해야 하며, WinForms 버전은 전환 검증이 끝날 때까지 유지합니다.

- [P1 기능 동등성 기준선](wpf-migration/P1-function-parity-baseline.md)
- [P1 화면 및 UX 기준선](wpf-migration/P1-screen-ux-inventory.md)
- [P1 수동 동등성 검증 목록](wpf-migration/P1-manual-test-checklist.md)
- [P2 공유 코어 경계](wpf-migration/P2-shared-core-boundary.md)
- [P3 WPF 프로젝트 뼈대](wpf-migration/P3-wpf-bootstrap.md)
- [P4 디자인 시스템과 앱 셸](wpf-migration/P4-design-system-and-shell.md)
- [P5 가상화 다운로드 목록](wpf-migration/P5-virtualized-download-list.md)
- [P6 URL 및 메타데이터 흐름](wpf-migration/P6-url-and-metadata-flow.md)
- [P7 다운로드·병합·진행률](wpf-migration/P7-download-and-progress.md)
- [P8 보조 화면과 설정](wpf-migration/P8-secondary-screens-and-settings.md)
- [P9 Chrome 탭 가져오기](wpf-migration/P9-chrome-tab-import.md)
- [P10 동등성 및 배포 전 검증 보고서](wpf-migration/P10-validation-report.md)
- [P11 배포 정책 및 전환 결정](wpf-migration/P11-release-decision.md)
- [최종 런타임 점검 기록](final-runtime-audit.md)
