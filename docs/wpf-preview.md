> **4.0.0.0 전환 후 안내:** 아래는 이전 개발 단계의 기록입니다. 현재는 WinForms를 제거하고 WPF를 정식 앱으로 배포하며 앱 자동 업데이트를 제공합니다. 최신 구성과 검증은 [4.0 릴리즈 기록](release-4.0.0.0.md)과 [README](../README.md)를 참고하세요.

# GMTFV WPF

> 이 문서의 최초 구현·후속 점검 기록은 리워크 이전 기록입니다. 현재 화면, 도구 설치 및 최신 검증은 [WPF 리워크 보고서](wpf-rework.md)를 기준으로 확인하세요.
## 목적과 구조

WinForms 안정판과 함께 제공하는 별도 WPF 앱이다. 실행 이름은 `GMTFV.Wpf.exe`, 대상 런타임은 `net10.0-windows`다. WinForms 코드는 수정하지 않는다. 제품과 화면에서 Preview 표기를 제거했으며 기존 WPF 설정·로그 경로는 호환성을 위해 유지한다.

```text
src/GMTFV.Wpf/
  App.xaml(.cs)              앱 시작과 의존성 조립
  Themes/                   Colors / Typography / Spacing / Controls / Animations / Theme
  Views/                    MainWindow / QueueView / DownloadItemView
                            SettingsView / ItemSettingsView / DiagnosticsView / AboutView
  ViewModels/               MainWindowViewModel / DownloadItemViewModel / ItemSettingsViewModel
  Models/                   PreviewSettings / ItemDownloadOptions / QualityChoice / SubtitleTrack
  Services/                 DownloadBackend / SettingsStore / Motion / SmoothScroll
  Commands/                 RelayCommand / AsyncCommand
  Converters/               썸네일 / 표시 상태 변환
tests/GMTFV.Wpf.Tests/       STA Dispatcher 기반 기능·바인딩·렌더링 검사
publish-wpf-preview.ps1      별도 ZIP 배포
```

Core는 `net472;net10.0`, DownloadEngine은 `net472;net10.0-windows`로 멀티 타깃한다. 엔진은 Windows Job Object로 자식 프로세스를 관리하므로 Windows 타깃을 유지한다. net472 산출물 경로와 Newtonsoft.Json 13.0.4 버전은 유지한다. 기존 테스트와 WinForms가 계속 net472 라이브러리를 참조한다.

## 재사용 범위와 경계

WPF ViewModel → `IDownloadBackend` → `VideoMetadataService` / `DownloadService` / `SubtitleService` 순서다. 인터페이스는 메타데이터·다운로드와 자막 조회·저장을 제공하며 테스트에서 대역으로 교체한다. UI에서는 yt-dlp 인수나 FFmpeg 프로세스를 만들지 않는다. 명령 생성, 포맷 선택, 재시도, 진행 파싱, 파일 검증과 충돌 없는 게시, 프로세스 트리 취소는 기존 엔진을 사용한다.

메타데이터와 자막 서비스에 DiagnosticLog 생성자를 추가했다. 기존 생성자는 유지한다. WPF의 조회·다운로드·자막 로그는 별도 디렉터리에 기록한다.

WinForms에 남아 있는 도구 설치/업데이트, System.Drawing 썸네일, Chrome 확장 수신기, Properties.Settings와 기존 폼을 WPF에서 참조하지 않는다. 자막 UI는 WPF로 구성하고 공유 엔진에 연결했다. 썸네일은 제한된 YouTube CDN URI로 WPF BitmapImage가 비동기 로딩하고 240px로 디코딩한다. 로딩 실패 시 플레이스홀더를 유지한다.

## 디자인과 모션

잉크색 배경, 불투명한 패널, 얇은 구조선, 라임색 작업 버튼, 고정 폭 상태 열을 사용한다. 중앙에 큐를 크게 배치하고 보조 기능은 오른쪽 패널로 연다. 본문은 Segoe UI / 맑은 고딕, 수치와 작업 레이블은 Cascadia Code / Consolas다. 패딩은 페이지 24×16, 패널 24, 행 12, 기본 버튼 높이는 40이다.

색상은 ResourceDictionary 토큰으로 분리했다. 진행은 파랑, 성공은 민트, 실패는 코랄, 복구는 앰버로 표시하고 모든 상태에 텍스트를 함께 제공한다. 라이트 테마 전환 UI는 아직 없지만 색상 토큰 교체가 가능하다.

공유 Motion 동작은 진입 220ms, 닫힘 140ms, 행 제거 대기 160ms, hover/press 120ms, 진행률 보간 180ms를 사용한다. 진입/닫힘은 opacity와 translate, 버튼은 hover/press 피드백, 메타데이터는 skeleton pulse, 큐 빈 상태·토스트·실패 상세·설정 패널은 같은 표시 전환을 사용한다. blur와 실시간 그림자는 사용하지 않는다. Windows 애니메이션 설정과 앱의 모션 줄이기를 반영한다. 모션을 끈 뒤 다시 표시할 때는 남아 있는 투명도·이동 애니메이션을 해제한다. 공통 ScrollViewer는 180ms 휠 보간을 사용하며 모션 줄이기에서는 즉시 스크롤한다. 목록은 픽셀 단위 Recycling 가상화를 사용하며, 대량 변경의 집계 알림은 Dispatcher에서 한 번으로 묶는다. 900×600 창에서 큐 두 행 이상을 온전히 표시한다.

## 구현한 흐름

- 여러 URL 입력, Enter 추가, Shift+Enter 줄바꿈, 메인 Ctrl+V, Ctrl+N 입력 포커스, 텍스트 및 JSON/TXT 목록 파일 드롭. 입력은 최대 72 높이에서 스크롤한다.
- Core URL 검증/정규화와 중복 제거. 잘못된 주소·중복 개수를 토스트로 알림.
- 동시 최대 3개의 메타데이터 조회, 썸네일/채널/길이/제목 표시.
- 정보 확인 후 다운로드 시작. 영상/오디오, 엔진이 지원하는 전체 확장자, 최대 해상도, 저장 폴더, 동시 1~16개, 비트레이트, AVI/FLV 인코더, 파일명 템플릿, 품질 폴백.
- 시작 시 설정 스냅샷을 사용한다. 다운로드 중 설정은 잠근다. 추가된 URL은 정보 조회 후 다음 시작 대상으로 대기한다.
- 개별/전체 취소, 다운로드 실패 재시도, 메타데이터 실패 재조회, 실패 항목만 재시도, 행 제거. 제거는 출력 파일을 삭제하지 않는다.
- 상세 오류와 복구 안내, 로그 폴더, 도구 버전, 저장 폴더 열기, 전체 진행률.
- 각 행의 설정 버튼에서 공통 기본값 상속 또는 영상별 영상/오디오·형식·실제 제공 화질/FPS를 지정한다. 제목·채널·업로드일·URL과 주소 복사/열기를 제공한다.
- 일반/자동 자막을 조회해 언어를 선택하고 SRT로 저장한다. 취소와 오류를 표시하며 창을 닫거나 앱을 종료하면 자막 작업 정리를 기다린다.
- URL 목록 JSON/TXT 저장·불러오기. 임시 파일을 완전히 기록한 뒤 교체하며, 취소나 교체 실패 시 이전 목록을 보존한다. JSON은 영상별 옵션과 기본값 상속을 보관하고 기존 WinForms JSON 옵션도 읽는다. TXT는 URL만 저장한다. 진행 상태는 복원하지 않는다.
- 설정을 일반·다운로드·형식·고급·도구 탭으로 분류하고 전용 JSON을 임시 파일 교체 방식으로 저장한다. WinForms 설정 가져오기는 제공하지 않는다.
- 앱 종료 시 수명 토큰으로 작업을 취소하고 정리가 끝난 뒤 닫는다.

## 저장·배포 정책

| 항목 | WinForms 안정판 | WPF |
| --- | --- | --- |
| 실행 파일 | GMTFV.exe | GMTFV.Wpf.exe |
| 설정 | 기존 Properties.Settings / user.config | LocalAppData/GMTFV.Preview/settings.json |
| 로그 | LocalAppData/GMTFV/Logs | LocalAppData/GMTFV.Preview/Logs |
| 도구 | AppData/GMTFV | 기본값은 같은 도구 폴더를 읽음, 변경 가능 |
| 배포 | 기존 Inno Setup | 별도 framework-dependent ZIP |

WPF는 도구를 업데이트하지 않는다. 도구 공유 중 안정판에서 업데이트할 때는 양쪽 다운로드를 먼저 끝내야 한다. 출력 폴더를 함께 써도 엔진이 기존 파일을 덮어쓰지 않는다. 실패/취소의 `.gmtfv-*` 임시 폴더는 기존 엔진 정책대로 보존한다. 배포 ZIP은 `artifacts/GMTFV-Wpf.zip`이다. 배포 스크립트와 이 문서의 기존 파일명은 유지한다.

## 검증

루트에서 WPF 테스트 실행 파일을 실행하면 `artifacts/wpf-preview`에 실제 WPF 렌더링 PNG를 만든다. 빈 큐, 진행/실패/준비 행, 설정, 진단, 정보, 실패 상세, 최소 창 크기를 렌더링하고 TraceListener로 바인딩 오류를 수집한다. 1,000개 항목에서 실제 ListBoxItem 수가 제한되는지도 확인한다. 작업 화면은 화면 밖에서 렌더링하며 실제 사용자 설정을 수정하지 않는다.

기능 검사는 설정 저장/거부, URL 검증과 중복, 조회 성공/실패/재시도, 다운로드 옵션 전달, 동시성, 완료/실패/재시도, 대기와 실행 중 취소, 종료 중 메타데이터 취소를 검사한다. 엔진 대역 검사는 실제 YouTube 성공을 증명하지 않는다. 기존 `GMTFV.Tests`는 net472 엔진·프로세스·WinForms 회귀 검증을 계속 담당한다.

### 최초 구현 시 검증 기록 (2026-10-18)

| 검증 | 결과 |
| --- | --- |
| 전체 솔루션 Debug / Release 빌드 | 성공, WinForms 공존 확인 |
| 기존 GMTFV.Tests Debug / Release | 각각 91 passed, 0 failed |
| WPF 자동 검사 | 26개 통과, 바인딩 오류 0개 |
| 1,000개 큐 항목 | 기본 창에서 5개 행 컨트롤만 생성 |
| .NET 10 ProcessRunner / Job 취소 | 실제 자식 프로세스로 통과 |
| .NET 10 DownloadService | 대역 프로세스 출력 검증·게시·동일 이름 보존 통과 |
| 실제 yt-dlp / FFmpeg 로컬 통합 | 영상 6종 + 오디오 7종, 총 13개 통과 |
| 배포 | 별도 ZIP 생성, 실제 실행 파일 시작 / 정상 종료 확인 |
| 화면 | 빈 큐 / 진행 / 실패 / 설정 / 진단 / 정보 / 최소 크기 PNG 확인 |

네트워크 제한으로 최초 NuGet 전체 복원과 취약성 데이터 조회에서 DNS 오류/NU1900 경고가 발생했다. 컴파일과 배포는 로컬 패키지로 성공했다. 로컬 미디어 통합 테스트는 최초 샌드박스 루프백 차단(10013) 후 허용된 샌드박스 외 실행에서 통과했다. 외부 YouTube 성공률을 이 결과에 포함하지 않는다.

제목 표시줄은 Windows 11에서 [공식 DWM 캡션 색상 속성](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute)으로 어둡게 맞춘다. Windows 10에서는 시스템 제목 표시줄을 유지한다.

### 후속 점검 및 수정 (2026-10-18)

WinForms 설정 가져오기 UI, ViewModel 명령, XML 파서와 관련 검사를 제거했다. 기존 Preview 설정 파일은 계속 읽고 저장한다. 안정판 user.config나 사용자 데이터는 수정하지 않는다.

실패 재시도는 명령 실행 시 실패했던 항목만 대상으로 삼는다. 메타데이터를 다시 조회한 경우에도 무관한 Ready 항목을 실행하지 않는다. 영상/오디오 모드 전환은 형식 목록을 먼저 갱신한 뒤 선택을 복원해, 접힌 ComboBox에서도 MP3/MP4가 표시되도록 했다.

회귀 검사에는 실제 형식 표시 문자열, 모션 줄이기 적용 후 실패 상세 재진입/닫기, 실패 재시도 대상 분리, 목록 저장 성공/취소/교체 실패, 대량 변경의 집계 알림 묶기, 최소 창 크기의 큐 표시를 추가했다. WPF 검사는 Debug/Release에서 각각 38개, 기존 WinForms/Core/Engine Debug 검사는 91개가 통과했다. 바인딩 오류는 0개다. 전체 솔루션 Debug 빌드와 별도 산출물 경로의 WPF Release 빌드도 성공했다. 사용 중인 기존 Release 앱은 종료하지 않았다.

동일한 바인딩 연결 검사에서 5,000개 행 추가 시간은 수정 전 937ms에서 수정 후 34ms로 줄었다. 이는 현재 PC의 컬렉션 추가 구간 측정이며, 실제 메타데이터 조회 시간이나 프레임 성능 측정은 아니다.

### 안정판 기능 대조 및 UI 반복 조작 점검 (2026-10-18)

메뉴를 열 때 비활성 ListBox가 기본 테마의 흰색 배경으로 바뀌는 문제를 공통 큐 템플릿에서 수정했다. 닫기 중복 실행 예외, 패널 닫힘 중 빈 내용 표시, hover 이후 비활성 버튼 표현, 외부 URL 추가 시 입력 손실, Escape 계층과 패널 뒤 드롭 처리도 수정했다.

JSON/TXT 목록 파일 드롭과 Ctrl+N 입력 포커스를 복원했다. 기본 창은 1180×720, 최소 900×600이며 시작 시 실제 작업 영역에 맞춘다. 중복 홍보 문구·고정 버전 장식·큰 빈 큐 장식과 미사용 리소스를 제거했다. 안정판 대비 아직 빠진 기능과 전체 검증 범위는 [기능 대조·UI 안정화 기록](wpf-feature-parity.md)에 정리했다.

## 남은 범위와 다음 단계

### 영상별 설정 복원 및 화면 정리 (2026-10-18)

제품명·창·화면의 Preview 표기를 제거하고 실행 파일을 `GMTFV.Wpf.exe`로 변경했다. 사용하지 않는 햄버거 버튼을 제거하고 창·사이드바에 기존 icon.ico를 적용했다. 영상별 기본값 상속/개별 형식·화질/FPS, 자막, URL 작업과 JSON 옵션 보존을 복원했다. 설정 5개 탭과 부드러운 휠 스크롤을 적용했다. 모션 줄이기는 저장 전에도 바로 반영한다.

WPF Debug/Release 각각 81개 검사, 기존 WinForms/Core/Engine Debug 91개 검사가 통과했다. 바인딩 오류는 0개다. 영상별 옵션의 독립 전달·기존 JSON 호환·잘못된 가져오기의 원자성·자막 취소/오류·연속 휠 입력·가상화를 포함한다. .NET 10 별도 배포 빌드는 공유 라이브러리도 artifacts 아래로 분리해 기존 net472 산출물과 섞이지 않게 했다.

아직 없는 안정판 기능은 Chrome 탭 수신, 앱 자동 업데이트, 도구 설치/업데이트, 앱 내 영상 재생, 다중 선택 취소·제거와 Delete, 설정 기본값 복원·파일명 샘플·닫기 시 미저장 확인이다. 큐 검색과 라이트 테마 선택도 없지만 현재 WinForms 안정판의 기능 누락으로 세지 않는다. URL 목록을 저장하지 않고 앱을 종료하면 큐는 보존되지 않는다.

실제 YouTube 네트워크 환경, 다양한 DPI/다중 모니터, 보조 기술, GPU 인코더, 사용자 하드웨어에서의 프레임 시간, ZIP을 다른 PC에서 실행하는 검증이 남아 있다.
