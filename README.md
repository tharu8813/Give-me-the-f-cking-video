<p align="center"><img src="icon.ico" alt="GMTFV" width="150"></p>

# GMTFV (야발아 동영상 내와)

[yt-dlp](https://github.com/yt-dlp/yt-dlp) 기반 YouTube 영상·오디오 다운로드 프로그램. **WinForms (.NET Framework 4.7.2)**를 공식 애플리케이션으로 유지한다.

[최신 릴리스](https://github.com/tharu8813/Give-me-the-f-cking-video/releases/latest) · [구조 안내](docs/architecture.md) · [제3자 라이선스](THIRD-PARTY-NOTICES.md)

## 주요 기능

- 여러 URL 붙여넣기, Enter로 추가, 드래그 앤 드롭, 중복 영상 감지
- 영상/오디오·화질/FPS·형식·파일명 템플릿·동시 다운로드 설정
- 썸네일·채널·길이·선택 옵션·상태 표시, 자막 다운로드, JSON/TXT 목록 저장·복원
- 전체/선택 작업 취소, 실패 항목만 재시도, 오류별 제한된 자동 복구
- 불필요한 영상 재인코딩 방지, 완료 파일 검증, 동일 이름 파일 보존, 진단 로그

## 요구 사항과 도구

Windows 10/11 x64 및 .NET Framework 4.7.2 이상, 인터넷 연결이 필요하다. 선택적인 웹 미리보기에는 Microsoft Edge WebView2 Runtime이 필요하다.

도구는 기본적으로 `%AppData%/GMTFV`에서 관리한다. 기존 위치·설정·목록 형식을 유지한다.

| 도구 | 용도 |
| --- | --- |
| 공식 yt-dlp Windows x64 실행 파일 | 영상·포맷·자막 조회, 다운로드. EJS 번들 포함 |
| FFmpeg + ffprobe | 스트림 병합·컨테이너 변경·오디오 추출·변환 |
| Deno 2.3.0 이상 | YouTube JavaScript 실행. 도구 폴더 또는 PATH에서 검색 |

첫 실행 시 FFmpeg/ffprobe 설치를 안내하고 yt-dlp와 누락된 Deno를 공식 배포처에서 준비한다. 다운로드는 체크섬 검증 후 반영한다. 기존 yt-dlp가 있으면 먼저 사용할 수 있게 하고 버전 확인은 뒤에서 한다. 도구 준비 실패로 앱을 강제 종료하지 않는다.

## 사용법

1. 설정에서 저장 폴더와 영상/오디오·형식·화질을 고른다. 기본값은 MP4, CPU, 동시 1개다.
2. `새 영상`에 URL을 붙여넣고 Enter를 누른다. 여러 URL은 줄바꿈으로 구분한다. 메인 화면 Ctrl+V와 드래그 앤 드롭도 지원한다.
3. `다운로드 시작`을 누른다. 다운로드·병합·변환·검증 상태를 확인한다. 측정할 수 없는 진행률은 움직이는 막대로 표시한다.
4. 목록 우클릭에서 실패 항목 재시도, 정보 조회 재시도, 선택 항목 취소, 로그 폴더 열기를 사용한다. 실패 이유는 행 오류 표시와 툴팁에서도 볼 수 있다.

고급 설정에는 GPU(AVI/FLV 재인코딩에만 적용), 오디오 비트레이트, 동시 다운로드 수, 화질 폴백 허용이 있다. 폴백은 요청 해상도를 넘지 않으며 끄면 선택 해상도/FPS가 없을 때 실패로 표시한다.

파일명은 기존 `%title%`, `%author%`, `%id%`, `%date%`, `%num%`, `%num2%`, `%num3%`, `%ext%` 등을 지원한다. `%title%.%ext%`는 확장자를 한 번만 붙인다. 동일 파일명은 `(1)`처럼 번호를 붙여 보존한다.

실패·취소 시 저장 폴더의 `.gmtfv-*` 작업 폴더를 보존한다. 같은 작업의 자동 재시도에서는 이어받으며 수동 재시도는 새 작업으로 시작한다. 작업 종료 후 필요 없는 임시 폴더를 직접 정리할 수 있다.

## Chrome 탭 가져오기

`Chrome 탭` 버튼을 누르면 확장 폴더가 열리고 `chrome://extensions/`가 복사된다. Chrome 주소창에 붙여넣고 개발자 모드를 켠 다음 **압축해제된 확장 프로그램을 로드합니다**에서 열린 `chrome-extension` 폴더를 선택한다. 확장 아이콘의 **YouTube 탭 가져오기**를 누르면 실행 중인 GMTFV에 추가된다. 주소는 루프백 포트 43128로만 전달한다. 다운로드 중 받은 새 URL은 중복을 제거해 대기시킨 뒤 작업 종료 후 추가한다.

## 빌드·테스트·설치본

Visual Studio의 .NET 데스크톱 개발 도구와 .NET Framework 4.7.2 targeting pack을 설치한다. `.slnx`를 지원하는 MSBuild로 루트에서 실행한다.

```powershell
msbuild GMTFV.slnx /t:Restore /p:RestorePackagesConfig=true /p:RestoreRepositoryPath=packages
msbuild GMTFV.slnx /t:Build /p:Configuration=Debug
.\tests\GMTFV.Tests\bin\Debug\GMTFV.Tests.exe
msbuild GMTFV.slnx /t:Build /p:Configuration=Release
.\tests\GMTFV.Tests\bin\Release\GMTFV.Tests.exe
ISCC.exe setup.iss
```

WinForms 실행 파일은 `bin/Release/GMTFV.exe`, 관리 라이브러리는 `bin/Release/DLLs/`에 생성된다. `.exe.config`, `runtimes/`, 확장 ZIP과 라이선스 고지를 함께 배포한다. 확장 폴더는 빌드 시 동봉 ZIP에서 풀어 설치본에 포함한다. 설치본은 `output-setup/GMTFV-<버전>-setup.exe`로 생성된다. Core/DownloadEngine도 같은 .NET Framework를 사용한다.

실제 도구가 준비되면 자체 생성 미디어로 로컬 통합 테스트를 할 수 있다. YouTube에 접속하지 않는다.

```powershell
python tests/media_smoke.py --tools-dir "$env:APPDATA\GMTFV"
```

## 실패 시 확인

- 네트워크/시간 초과: 제한된 자동 재시도 이후에도 실패하면 연결을 확인한다.
- 403: 스트림 경로를 한 번 갱신한다. 계속 실패하면 yt-dlp 업데이트와 서비스 상태를 확인한다.
- 429: 자동 반복을 중단한다. 충분히 기다린 뒤 다시 시도한다.
- JavaScript/클라이언트: yt-dlp·Deno 호환 버전과 경로를 확인한다. [공식 EJS 안내](https://github.com/yt-dlp/yt-dlp/wiki/EJS)를 참고한다.
- PO Token: 외부 플러그인 장애 시 플러그인 없는 기본 경로로 한 번 시도한다. 공급자 서버를 새로 실행하지 않는다.
- 포맷: 다른 형식이나 화질을 고르거나 화질 폴백을 허용한다.
- 병합/변환: ffmpeg.exe와 ffprobe.exe를 함께 준비한다. MKV를 선택해 볼 수 있다. GPU 변환 실패는 CPU로 한 번 복구한다.
- 파일: 쓰기 권한·디스크 공간·폴더 길이·파일 사용 여부를 확인한다.
- 비공개/삭제/접근 제한: 본인의 접근 권한과 영상 상태를 확인한다. 쿠키 자동 수집이나 접근 제한 우회는 제공하지 않는다.

로그는 `%LocalAppData%/GMTFV/Logs`에 저장하며 URL·인증값·사용자 홈 경로를 마스킹한다. 현재 로그와 이전 로그 하나만 보관한다.

## 라이선스

GMTFV는 [GPL-3.0](LICENSE.txt)으로 배포한다. 외부 도구에는 각각의 [라이선스와 고지](THIRD-PARTY-NOTICES.md)가 적용된다. 다운로드 권한이 있는 콘텐츠에 사용한다.
