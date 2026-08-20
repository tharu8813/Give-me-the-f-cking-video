# 최종 런타임 점검 기록

점검일: 2026-08-21  
범위: WinForms 안정판, WPF Preview, 동봉 도구, 설정·목록 파일, Chrome 연동 및 설치 구성

## 자동 점검 결과

| 항목 | 결과 | 확인 내용 |
| --- | --- | --- |
| WinForms 빌드 | 통과 | `dotnet build src/GMTFV.WinForms/GMTFV.WinForms.csproj -c Release --no-restore` — 경고 0, 오류 0 |
| WPF/Core 빌드 | 통과 | `dotnet build src/GMTFV.Wpf/GMTFV.Wpf.csproj -c Release --no-restore` — 경고 0, 오류 0 |
| 의존 DLL | 통과 | WinForms 출력에 `System.Resources.Extensions.dll` 포함 확인 |
| 동봉 도구 실행 | 통과 | yt-dlp 2026.07.04, FFmpeg 2026-08-17, Deno 2.9.5, PO 공급자 도움말 실행 확인 |
| PO 플러그인 경로 | 통과 | WinForms/WPF 출력의 `yt-dlp-plugins`에서 공식 `bgutil-ytdlp-pot-provider.zip`을 로드해 `bgutil:http-1.3.1` 공급자 등록 확인 |
| Chrome 확장·라이선스 | 통과 | WPF 출력에 확장 소스와 라이선스 고지 포함 확인 |
| 공백 오류 | 통과 | `git diff --check`에서 공백 오류 없음 |

## 이번 점검에서 보완한 위험

1. WPF 메타데이터·자막 요청에 30초 제한을 추가했습니다. 네트워크 또는 yt-dlp가 멈추더라도 UI가 무한 대기하지 않습니다.
2. 메타데이터·자막 조회에도 PO 공급자 없이 가능한 YouTube 클라이언트 폴백을 적용했습니다.
3. WPF 설정을 저장하기 전에 출력 경로, 컨테이너, GPU 옵션, 비트레이트, 동시 다운로드 수를 검증합니다. 손상되거나 수동 편집된 설정 파일도 다운로드 명령에 그대로 전달되지 않습니다.
4. WinForms/WPF의 목록 및 설정 파일 저장을 임시 파일 후 교체 방식으로 변경했습니다. 저장 중 앱이 종료되어도 기존 파일이 깨질 가능성을 줄입니다.
5. 목록 파일 읽기는 백그라운드에서 수행하도록 바꿔 큰 JSON/TXT 선택 시 UI 멈춤을 줄였습니다.
6. yt-dlp의 과도하게 긴 오류 출력을 WPF 화면에 그대로 누적하지 않도록 제한했습니다.
7. 풀린 Python 파일은 현재 yt-dlp에서 공급자로 등록되지 않는 문제를 확인해 공식 bgutil ZIP 배치로 교체했습니다. 토큰 공급자 오류 또는 HTTP 403이 발생하면 `android_vr` 단일 클라이언트, 이어서 `web_safari` HLS 순서로 자동 재시도합니다.

## 배포 전 반드시 실제 환경에서 확인할 항목

자동 점검만으로 YouTube, Chrome, Windows 권한 정책의 모든 변수를 보장할 수는 없습니다. 다음은 실제 설치 환경에서 수행해야 합니다.

- Inno Setup으로 설치 파일을 실제 생성하고, 새 Windows 사용자 또는 Program Files 설치 경로에서 설치·실행·제거·업그레이드를 확인합니다. 이 점검 환경에는 Inno Setup 컴파일러가 없어 설치 EXE 생성은 실행하지 못했습니다.
- 권한 있는 영상으로 영상·오디오·병합·취소·자막·HTTP 403/연령 제한을 각각 시험합니다. YouTube 정책과 서버 응답은 수시로 달라질 수 있습니다.
- Chrome에서 확장 프로그램을 설치한 뒤 현재 탭·선택 탭·전체 탭 전송을 검증합니다. WinForms와 WPF를 동시에 실행하면 localhost 포트가 충돌할 수 있으므로 함께 실행하지 않습니다.
- 1,000개 이상 목록에서 스크롤과 진행률 갱신을 실측하고, 긴 경로·권한 없는 폴더·디스크 부족·동일 제목 파일·백신 격리 상황도 확인합니다.

## 배포 판단

WinForms는 현재 안정 배포 대상으로 유지할 수 있습니다. WPF는 별도 GUID와 설치 경로를 사용하는 전용 설치 스크립트를 갖췄지만 [P11 배포 정책](wpf-migration/P11-release-decision.md)에 따라 여전히 Preview입니다. 설정 이전, 남은 기능 동등성 및 위 수동 검증이 끝나기 전에는 기본 배포본으로 전환하지 않습니다.
