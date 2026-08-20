# P2 — 공유 코어 경계

## 완료한 분리

`src/GMTFV.Core`는 `netstandard2.0` 대상의 UI 독립 라이브러리다. 따라서 현재 .NET Framework 4.7.2 WinForms와 이후 최신 .NET WPF가 같은 어셈블리를 참조할 수 있다.

| 구성 요소 | 책임 | 현재 WinForms 사용 위치 |
| --- | --- | --- |
| `YouTubeUrl` | 단일 YouTube 영상 URL의 검증·정규화 | URL 입력, 메타데이터 조회, Chrome 확장 수신 |
| `DownloadQueueService` | 동시 다운로드 수와 진행 슬롯 할당 | `MainForm` 다운로드 큐 |
| `DownloadProgress`, `DownloadPhase` | 프레임워크 독립 진행 상태 계약 | P3 이후 WPF ViewModel 및 P6/P7 실행 서비스의 기준 |
| `DownloadProfile`, `PlaylistItem` | 출력 설정 및 목록 교환 계약 | `PlaylistDataMapper`가 WinForms 모델을 변환 |

## 현재 앱과의 연결

- `src/GMTFV.WinForms/GMTFV.WinForms.csproj`는 `GMTFV.Core`를 프로젝트 참조한다.
- 기존 `services/DownloadQueueService.cs`는 제거했고, 같은 동시성 로직을 Core로 이동했다. WinForms의 사용자 동작은 유지된다.
- `VideoMetadataService`, `Tol.IsYouTubeUrl`, `ChromeTabImportService`는 하나의 URL 정규화 규칙을 공유한다.
- 목록 내보내기의 `VideoInfo → ExportVideoData` 변환은 `PlaylistDataMapper`로 옮겨, 파일 대화상자 처리와 데이터 변환을 분리했다.

## 의도적으로 남긴 경계

아래 항목은 현재 WinForms와 외부 도구에 의존하므로 P2에서 무리하게 옮기지 않았다. P3~P7에서 Core 계약을 입력/출력으로 사용해 별도 구현한다.

| 영역 | 현재 위치 | 이후 단계 |
| --- | --- | --- |
| yt-dlp 프로세스 실행과 출력 해석 | `MainForm.DownloadWithYtDlp`, `YtDlpTool` | P6/P7 |
| 메타데이터 JSON·썸네일 네트워크 접근 | `VideoMetadataService` | P6 |
| WinForms 설정 저장소 | `Properties.Settings`, `Setting` | P8에서 호환 어댑터 |
| 파일 선택, 메시지 상자, 가져오기 진행 창 | `PlaylistManager`, WinForms Form | P8 |
| PO Token 프로세스와 Chrome 로컬 수신기 | `tools`, `services` | P9 이후 플랫폼 어댑터 |

## 규칙

1. `GMTFV.Core`에는 `System.Windows.Forms`, WPF, `System.Drawing`, 파일 대화상자, `Process`, `MessageBox` 의존성을 넣지 않는다.
2. Core에서 노출하는 형식은 `public`으로, UI 전용 어댑터는 각 앱 프로젝트 내부 형식으로 둔다.
3. 외부 도구의 원문 로그는 플랫폼 어댑터가 Core의 `DownloadProgress` 또는 오류 계약으로 변환한다.
4. 신규 WPF는 WinForms 프로젝트를 참조하지 않고 `GMTFV.Core`와 이후 분리될 도구 어댑터만 참조한다.
