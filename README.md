<p align="center">
  <img src="icon.ico" alt="icon" width="180">
</p>

# GMTFV (야발아 동영상 내와)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2%20WinForms-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Windows](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-GPL3.0-green.svg)](LICENSE)
[![Latest Release](https://img.shields.io/github/v/release/tharu8813/Give-me-the-f-cking-video?label=Download)](
https://github.com/tharu8813/Give-me-the-f-cking-video/releases/latest)

GMTFV는 [yt-dlp](https://github.com/yt-dlp/yt-dlp) 기반으로 한
유튜브 고화질 동영상 다운로드 프로그램입니다.

## WPF 전환 진행 중

새 UI는 `src/GMTFV.Wpf`의 .NET 10 WPF 프로젝트에서 개발합니다. 기존 WinForms 프로젝트는 다운로드 엔진을 안전하게 이전·검증할 때까지 호환성 기준으로 유지합니다. Visual Studio에서는 `GMTFV.slnx`를 열고 **GMTFV.Wpf**를 시작 프로젝트로 지정해 새 화면을 실행할 수 있습니다.

개발·유지보수를 위한 코드 구조 안내는 [architecture.md](docs/architecture.md)를 참고하세요.

## Chrome에서 열린 YouTube 탭 가져오기

설치 후 `Chrome 탭` 버튼을 누르면 확장 프로그램 폴더가 열리고 `chrome://extensions/` 주소가 클립보드에 복사됩니다. Chrome 주소창에서 붙여넣어 이동한 뒤 개발자 모드를 켜고 **압축해제된 확장 프로그램을 로드합니다**를 눌러 열린 `chrome-extension` 폴더를 선택하세요. 이후 Chrome 도구 모음의 GMTFV 확장 아이콘에서 **YouTube 탭 가져오기**를 누르면, 현재 열려 있는 YouTube 탭이 실행 중인 GMTFV 목록에 추가됩니다. 탭 주소는 외부 서버가 아닌 내 컴퓨터의 GMTFV에만 전달됩니다.


## 책임 제한

본 소프트웨어를 사용함으로 인해 발생하는  
모든 문제, 손해, 데이터 손실 등에 대해  
개발자는 어떠한 책임도 지지 않습니다.
