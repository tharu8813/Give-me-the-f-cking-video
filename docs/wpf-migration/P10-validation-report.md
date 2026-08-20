# P10 — WPF 동등성 및 배포 전 검증 보고서

검증일: 2026-08-21  
기준: [P1 기능 동등성 기준선](P1-function-parity-baseline.md)

## 자동 검증 결과

| 항목 | 결과 | 근거 |
| --- | --- | --- |
| WinForms 빌드 | 통과 | `dotnet build src/GMTFV.WinForms/GMTFV.WinForms.csproj -c Release --no-restore` — 경고 0, 오류 0 |
| WPF/Core 빌드 | 통과 | `dotnet build src/GMTFV.Wpf/GMTFV.Wpf.csproj -c Release --no-restore` — 경고 0, 오류 0 |
| WPF 도구 포함 | 통과 | `yt-dlp.exe`, `ffmpeg.exe`, `deno.exe`, `pot-provider.exe`를 WPF 출력 폴더에서 확인 |
| 도구 무결성 | 통과 | 원본과 WPF 출력본의 SHA-256이 각 도구별 일치 |
| PO 플러그인 포함 | 통과 | `yt-dlp-plugins/bgutil-ytdlp-pot-provider.zip` 포함 및 yt-dlp 공급자 등록 확인 |
| Chrome 확장 포함 | 통과 | `manifest.json`, `popup.html`, `popup.js` 확인 및 manifest JSON 파싱 성공 |
| 라이선스 고지 포함 | 통과 | `LICENSE.txt`, 제3자 라이선스 안내, Deno/yt-dlp 고지, bgutil 패치 확인 |
| WPF 설치 프로그램 | 스크립트 구현 | `setup_wpf.iss`가 WPF Preview를 별도 GUID·설치 경로로 패키징. 실제 설치 EXE 생성 및 새 PC 검증은 필요 |

## 기능 동등성 판정

| ID | 판정 | 근거와 남은 검증 |
| --- | --- | --- |
| F01 | 부분 | WPF는 도구 누락 시 오류를 보이지만, 시작 시 전체 도구 사전 점검·버전 표시는 없다. |
| F02 | 부분 | URL 입력, Enter, 입력창 붙여넣기는 가능하다. 전역 `Ctrl+N`과 목록 드래그 앤 드롭은 아직 없다. |
| F03 | 통과(코드) | `YouTubeUrl` 정규화와 URL/영상 ID/조회 중 중복 방지를 사용한다. |
| F04 | 부분 | 비동기 메타데이터·썸네일·오류 처리는 있으나, 개별 임시 로딩 행은 없다. |
| F05 | 부분 | 선택과 상세 보기는 있으나, WPF 목록에서 항목 삭제 기능이 없다. |
| F06 | 부분 | WPF 설정 항목은 구현됐지만 기존 WinForms 사용자 설정을 자동 이전하지 않는다. |
| F07 | 통과(코드) | 취소 토큰과 프로세스 트리 종료, 창 종료 정리를 구현했다. 실제 장시간 다운로드 취소는 수동 검증 필요. |
| F08 | 통과(코드) | yt-dlp 다운로드 단계와 FFmpeg 병합 시간을 항목·전체 퍼센트로 변환한다. 실제 병합 영상 검증 필요. |
| F09 | 부분 | 항목별 성공/실패/취소 원인은 보이나, WinForms와 같은 완료 요약 창은 없다. |
| F10 | 통과(코드) | 상세 화면에서 자막 목록 조회·선택 SRT 저장을 구현했다. 실제 영상 검증 필요. |
| F11 | 부분 | JSON/TXT 호환 및 설정 보존은 구현했지만, WinForms의 최대 3개 병렬 가져오기·전용 취소 진행 창은 없다. |
| F12 | 통과(코드) | 기존 localhost 프로토콜을 유지하고 현재/선택/전체 탭 전송을 제공한다. Chrome 실기기 검증 필요. |
| F13 | 부분 | 도구/플러그인은 포함되고 PO 공급자 폴백이 있다. 다만 공급자는 앱 시작이 아닌 다운로드 시작 때 준비한다. |
| F14 | 미구현 | WPF 앱/도구 버전 표시 및 업데이트 확인 기능이 아직 없다. |

## 성능 및 실제 환경 검증 상태

- 가상화 목록은 `VirtualizingStackPanel`과 재활용 모드로 구성되어 있다. 대량 항목(1,000개 이상) 실측 스크롤 성능은 아직 수행하지 않았다.
- YouTube 서버 응답, 로그인·연령 제한, HTTP 403, PO Token, 실제 FFmpeg 병합, 자막 저장은 네트워크 및 권한 있는 영상으로 수동 검증해야 한다.
- 설치 폴더가 쓰기 금지인 Program Files 환경, 기존 WinForms 설정 보유 환경, Chrome 실제 확장 설치 환경도 수동 검증이 필요하다.

## P11 결정 전 필수 보완

1. WPF 전용 설치/배포 산출물을 만든다. WinForms 설치 프로그램을 WPF로 바꾸지 않는다.
2. F02, F05, F14를 구현한다: 전역 단축키/드래그 앤 드롭, 항목 삭제, 도구·업데이트 정보.
3. WinForms 사용자 설정 이전 또는 명시적 가져오기 흐름을 제공한다.
4. JSON 병렬 가져오기와 취소 진행 UX를 WinForms 기준으로 맞춘다.
5. P1 수동 체크리스트 T01~T22를 권한 있는 테스트 영상과 실제 Chrome에서 기록한다.

## 결론

**WPF를 기본 배포본으로 전환하는 것은 현재 승인하지 않는다.** WinForms는 계속 기본 안정 버전으로 유지한다. WPF는 기능 미리보기(Preview)로만 판단할 수 있으며, P11에서는 위 보완 및 수동 검증 기록을 근거로 배포 정책을 결정한다.
