# GMTFV 4.0.0.0 전환과 배포 검증

## 배포 구성

4.0.0.0부터 WPF를 정식 앱으로 배포한다. 앱 어셈블리·실행 파일·설치 프로그램 버전은 모두 `4.0.0.0`이다. 새 살구색 다운로드/재생 아이콘을 실행 파일, 창, 헤더, 설치본과 README에 적용했다.

- `GMTFV-4.0.0.0-setup.exe`: Windows x64 설치본, 기존 3.x AppId 유지, WPF 바로가기와 실행 파일로 전환.
- `GMTFV-4.0.0.0-win-x64.zip`: 압축을 모두 풀고 `GMTFV.Wpf.exe` 실행.
- `GMTFV-4.0.0.0-SHA256SUMS.txt`: 두 파일의 SHA-256.
- .NET 10.0.12 / Windows Desktop Runtime과 해당 라이선스, Newtonsoft.Json 라이선스를 포함한다. yt-dlp·FFmpeg·Deno는 앱의 도구 관리에서 별도로 준비한다.

## WinForms 제거와 호환성

WinForms 프로젝트 45개 추적 파일, 솔루션 참조, 전용 설치/게시 스크립트, 사용되지 않는 Chrome 확장과 구 아이콘을 제거했다. 사용자 폴더에 있는 설정·도구·다운로드 파일은 제거하지 않는다. 설치본은 설치 위치의 기존 `GMTFV.exe`와 `GMTFV.exe.config`만 정리하며 다운로드 폴더나 사용자 설정 폴더를 삭제하지 않는다.

Core/DownloadEngine의 `net472`와 .NET 10 멀티 타깃은 유지한다. 엔진의 Newtonsoft.Json 13.0.4 참조는 두 타깃 모두 PackageReference로 통일해 제거된 WinForms의 packages.config 복원에 의존하지 않게 했다. 엔진 구현은 변경하지 않았다.

Core/Engine 검사 프로젝트를 .NET 10 SDK 프로젝트로 옮기고 자신의 프로세스 실행 경로를 apphost로 변경했다. 기존 91개 검사 중 WinForms 전용 8개(구 도구 교체/해시 2개, 폼 렌더링/종료 2개, 구 PlaylistManager 2개, Chrome 전용 2개)는 해당 제품 코드와 함께 제거했다. 나머지 **83개 엔진/Core 검사는 유지**한다. WPF의 도구 묶음·해시·목록 호환성은 WPF 검사에서 별도로 검증한다.

이전 개발 문서에는 과거 구현·검증 수치를 남기고 4.0 전환 안내를 덧붙였다. 제거된 WinForms 소스 링크는 제거 전 커밋으로 고정했다. WPF 설정/로그 경로 `%LocalAppData%/GMTFV.Preview`와 기존 도구 경로는 보존한다. WinForms 설정 가져오기, Chrome 탭 가져오기와 앱 내 영상 재생은 제공하지 않는다.

## 자동 업데이트

`AppUpdateService`는 공식 저장소의 최신 정식 릴리즈만 조회한다. 새 버전과 정확한 설치 파일명을 확인하며 공식 다운로드 URL·크기 제한·SHA-256 digest가 없거나 잘못되면 설치를 차단한다. 다운로드 결과를 검증하고 실행 직전 다시 검증한다. 네트워크·해시·크기·취소 오류에서 기존 앱 파일을 교체하지 않는다. digest 형식은 [GitHub 공식 API 문서](https://docs.github.com/en/rest/releases/assets)를 기준으로 구현했다.

`AppUpdateViewModel`은 확인·전송·검증·취소·재시도 상태와 명령을 담당한다. 시작 시 확인하고 새 버전을 상단 앱 정보 버튼에 표시한다. 앱 정보에서 수동 확인·설치·취소·공식 릴리즈 페이지 열기를 제공한다. 설정의 시작 시 확인 옵션은 저장 후 다음 실행에 반영된다.

설치는 사용자가 직접 선택한다. 다운로드·메타데이터·자막·도구 작업·제거 중에는 설치할 수 없다. 전송 도중 다른 작업이 시작돼도 실행 전에 다시 막고, 작업 종료 후 검증된 설치본을 재사용한다. 설치 직전에 설정과 현재 URL/옵션 목록을 저장하고 현재 실행 폴더를 설치 대상으로 지정해 설치 프로그램을 연 뒤 정상 종료한다. 다음 실행에서 목록을 다시 조회하고 원본을 `.restored`로 남긴다. 완료·실패 상태나 미완료 다운로드 자체를 세션으로 복원하지는 않는다.

## 실행한 검증

| 검사 | 결과 | 로컬 증거 |
| --- | --- | --- |
| NuGet 복원·감사 | 정상, 취약성 경고 없음 | `artifacts/v4-restore.txt`, `v4-release-restore.txt` |
| 전체 솔루션 Debug / Release | 모두 경고 0, 오류 0. net472 라이브러리 포함 | `v4-debug-build.txt`, `v4-release-build.txt` |
| WPF Debug / Release | 각각 157개 통과, 바인딩 오류 0 | `v4-wpf-debug.txt`, `v4-wpf-release.txt` |
| Core/Engine Debug / Release | 각각 83개 통과, 실패 0 | `v4-engine-debug.txt`, `v4-engine-release.txt` |
| 실제 yt-dlp / FFmpeg | 자체 생성 미디어의 13개 영상/오디오 형식 통과 | `v4-media-smoke.txt` |
| 설치본/ZIP | 4.0.0.0 버전, WPF·런타임·라이선스 파일 확인 | `v4-publish.txt`, `GMTFV-4.0.0.0-SHA256SUMS.txt` |
| 실제 설치·제거 | 별도 AppId·작업 폴더·CURRENTUSER·NOICONS로 검증. 설치 앱 해시 일치, 구 실행 파일 정리, 미리 둔 파일과 다운로드를 제거 후에도 보존 | `v4-installer-smoke.txt`, `v4-install.txt`, `v4-uninstall.txt` |

업데이트 대역 검사는 새 버전·동일 버전·다운그레이드 방지, draft/prerelease 무시, 잘못된 URL·해시·파일 크기·중복 자산·버전, 손상/잘린 전송·오프라인·취소, 빠른 중복 클릭, 전송 중 작업 시작, 작업 후 재사용, 시작 시 확인 끄기와 업데이트 전 목록 복원을 포함한다. 실제 오프스크린 앱 정보 화면은 `artifacts/wpf-preview/app-update.png`다. 표시한 4.1.0.0은 검사 대역 버전이다.

공개 릴리즈의 실제 메타데이터 조회와 설치본 다운로드·해시 검사는 테스트 실행 파일의 `--app-update-smoke`로 별도로 수행할 수 있다. 이 모드는 설치 프로그램을 실행하지 않는다.

## 한계

- 실제 YouTube 영상 파일 다운로드·실서비스 실패율을 새로 검증한 것이 아니다. 실제 도구 검사는 로컬 미디어 기반이다.
- 별도 AppId의 사용자 단위 무인 설치·제거를 검사했다. 기존 실사용 3.x 설치를 관리자/UAC 환경에서 직접 업그레이드하는 시나리오와 다음 실제 버전으로의 전체 업데이트 UI 경로는 미검증이다.
- 설치본은 코드 서명되지 않았다. Windows의 게시자 신뢰/SmartScreen은 해당 PC 정책에 따라 별도 확인이 필요할 수 있다.
- 물리 입력, 다양한 DPI/GPU/다중 모니터, 장시간 사용과 실제 디스플레이 프레임 성능은 이전 리워크의 미검증 범위를 유지한다.
