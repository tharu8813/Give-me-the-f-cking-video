<p align="center">
  <img src="icon.ico" alt="icon" width="180">
</p>

> [!WARNING]
> 해당 저장소에 일부 파일은 Codex로 인해 생성되었습니다.

# GMTFV (야발아 동영상 내와)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2%20WinForms-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Windows](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-GPL3.0-green.svg)](LICENSE.txt)
[![Latest Release](https://img.shields.io/github/v/release/tharu8813/Give-me-the-f-cking-video?label=Download)](
https://github.com/tharu8813/Give-me-the-f-cking-video/releases/latest)

GMTFV는 [yt-dlp](https://github.com/yt-dlp/yt-dlp) 기반으로 한
유튜브 고화질 동영상 다운로드 프로그램입니다.

개발·유지보수를 위한 코드 구조 안내는 [architecture.md](docs/architecture.md)를 참고하세요.

현재 일반 배포본은 WinForms 버전입니다. `src/GMTFV.Wpf`의 WPF 버전은 기능 동등성 및 설치 검증이 완료되기 전까지 개발·제한 테스트용 Preview로 유지합니다. 전환 조건은 [WPF 배포 정책](docs/wpf-migration/P11-release-decision.md)을 참고하세요.

세 프로젝트는 `src/GMTFV.WinForms`, `src/GMTFV.Core`, `src/GMTFV.Wpf`에 나란히 배치되어 있습니다. Release 빌드와 설치 파일 생성 방법, 두 배포본을 같은 GitHub 릴리스에 올릴 때 지켜야 할 파일명 규칙은 [배포 안내](docs/release-and-installers.md)를 참고하세요.

## 오픈소스 도구 및 라이선스

배포본에는 yt-dlp, FFmpeg, Deno 및 bgutil PO Token 공급자가 포함됩니다. 각 도구의 버전, 라이선스, 대응 소스 제공 의무와 bgutil 공급자 빌드 절차는 [제3자 도구 라이선스 안내](docs/third-party-licenses.md)에 정리되어 있습니다. 영상의 저작권 및 플랫폼 이용약관은 사용자가 준수해야 합니다.

### 소스 빌드용 외부 도구 준비

외부 실행 파일은 GitHub의 파일 크기 제한 때문에 이 소스 저장소에 포함하지 않습니다. 프로그램 소스를 빌드하거나 설치 파일을 만들려면 아래 파일을 내려받아 **저장소 최상위 폴더**에 배치하세요.

| 루트에 배치할 파일 | 공식 다운로드 및 준비 방법 |
| --- | --- |
| `yt-dlp.exe` | [yt-dlp Windows 최신 실행 파일](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe)을 그대로 저장합니다. |
| `ffmpeg.exe` | [Gyan FFmpeg Release Essentials ZIP](https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip)을 내려받아 압축 파일의 `bin/ffmpeg.exe`만 꺼냅니다. |
| `deno.exe` | [Deno Windows x64 ZIP](https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip)을 내려받아 `deno.exe`를 꺼냅니다. ARM64 PC는 Deno 릴리스 페이지에서 ARM64 파일을 선택하세요. |
| `pot-provider.exe` | bgutil은 공식 Windows EXE를 제공하지 않습니다. [bgutil-ytdlp-pot-provider 소스](https://github.com/Brainicism/bgutil-ytdlp-pot-provider)를 받아 [재현 절차](docs/third-party-licenses.md#bgutil-공급자-재현-절차)에 따라 생성합니다. |

네 파일은 `.gitignore`에 등록되어 로컬 빌드와 설치 패키지에는 포함되지만 Git 커밋에는 들어가지 않습니다. 일반 소스 빌드는 파일이 없어도 가능하지만 다운로드 기능은 실행되지 않으며, 두 Inno Setup 스크립트는 네 파일이 들어 있는 Release 빌드 출력이 있어야 컴파일할 수 있습니다.

## Chrome에서 열린 YouTube 탭 가져오기

설치 후 `Chrome 탭` 버튼을 누르면 확장 프로그램 폴더가 열리고 `chrome://extensions/` 주소가 클립보드에 복사됩니다. Chrome 주소창에서 붙여넣어 이동한 뒤 개발자 모드를 켜고 **압축해제된 확장 프로그램을 로드합니다**를 눌러 열린 `chrome-extension` 폴더를 선택하세요. 이후 Chrome 도구 모음의 GMTFV 확장 아이콘에서 **YouTube 탭 가져오기**를 누르면, 현재 열려 있는 YouTube 탭이 실행 중인 GMTFV 목록에 추가됩니다. 탭 주소는 외부 서버가 아닌 내 컴퓨터의 GMTFV에만 전달됩니다.


## 책임 제한

본 소프트웨어를 사용함으로 인해 발생하는  
모든 문제, 손해, 데이터 손실 등에 대해  
개발자는 어떠한 책임도 지지 않습니다.
