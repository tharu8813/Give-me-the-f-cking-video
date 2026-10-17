<p align="center"><img src="assets/branding/app-icon.png" alt="GMTFV — 재생 모양을 품은 다운로드 화살표" width="128"></p>

<h1 align="center">GMTFV 4.0</h1>
<p align="center">링크를 넣고, 옵션을 고르고, 내 폴더에 저장하세요.</p>
<p align="center"><a href="https://github.com/tharu8813/Give-me-the-f-cking-video/releases/latest">최신 릴리즈 다운로드</a> · <a href="docs/architecture.md">구조 안내</a> · <a href="THIRD-PARTY-NOTICES.md">제3자 라이선스</a></p>

GMTFV (Give Me The F*cking Video)는 [yt-dlp](https://github.com/yt-dlp/yt-dlp) 기반 YouTube 영상·오디오 다운로드 앱입니다. **4.0.0.0부터 .NET 10 / WPF가 정식 앱**이며 WinForms는 제거했습니다. 살구색 타일과 재생 모양을 품은 다운로드 화살표가 새 아이콘입니다. 앱·설치 프로그램·README에 같은 [아이콘 원본](assets/branding)을 사용합니다.

## 설치와 실행

**Windows 10/11 x64** 및 인터넷 연결이 필요합니다. 공식 설치본과 ZIP에는 .NET 10 Desktop Runtime이 포함되어 있어 별도 런타임 설치가 필요하지 않습니다.

- **설치본:** [GMTFV-4.0.0.0-setup.exe](https://github.com/tharu8813/Give-me-the-f-cking-video/releases/download/4.0.0.0/GMTFV-4.0.0.0-setup.exe)를 실행합니다.
- **ZIP:** [GMTFV-4.0.0.0-win-x64.zip](https://github.com/tharu8813/Give-me-the-f-cking-video/releases/download/4.0.0.0/GMTFV-4.0.0.0-win-x64.zip)을 새 폴더에 모두 풀고 `GMTFV.Wpf.exe`를 실행합니다.
- 첫 실행에 도구가 없으면 **도구 및 진단 → 도구 준비**를 선택합니다. yt-dlp·FFmpeg/ffprobe·Deno를 공식 배포처에서 받아 SHA-256을 검증합니다. 설치 실패 시 앱을 계속 사용할 수 있고 다시 준비할 수 있습니다.

기존 3.x 설치본과 같은 설치 식별자를 사용하며 바로가기는 WPF로 연결됩니다. 사용자의 설정·도구·다운로드 파일을 삭제하지 않습니다. 기존 WinForms 설정을 WPF로 가져오는 기능은 제공하지 않습니다. 구버전이 필요하면 [3.6.0.0 릴리즈](https://github.com/tharu8813/Give-me-the-f-cking-video/releases/tag/3.6.0.0)를 이용하세요.

## 사용법

1. 위쪽 입력란에 YouTube 링크를 붙여넣고 Enter 또는 **영상 추가**를 누릅니다. 여러 링크·텍스트 드롭·JSON/TXT 목록 파일 드롭을 지원하며 중복을 제거합니다.
2. 같은 화면에서 **영상/오디오·형식·화질·저장 위치**를 고릅니다. 각 영상의 **옵션·자막**에서 실제 제공 해상도/FPS, 영상별 설정, 일반·자동 자막을 선택합니다.
3. **다운로드 시작**을 누릅니다. 대기·다운로드·병합·변환·복구·검증 상태를 표시하며, 계산할 수 없는 진행률이나 ETA를 만들지 않습니다.
4. 완료된 행에서 파일을 열거나 폴더에서 확인합니다. 실패한 행의 문제 해결 안내에서 재시도와 진단 정보를 확인합니다.

목록 왼쪽 번호는 현재 순서입니다. 항목을 선택한 뒤 **위·아래 버튼**, 우클릭 메뉴 또는 **Alt+↑/↓**로 순서를 바꿉니다. Ctrl/Shift 다중 선택도 내부 순서를 유지하며 함께 이동합니다. 다운로드 중에는 순서 변경을 잠급니다. Esc는 열린 패널을 닫거나 선택을 해제하고, Delete는 선택 항목을 목록에서만 제거합니다. 파일은 삭제하지 않습니다.

**목록 저장/열기**로 순서를 보관합니다. JSON은 영상별 옵션도 저장하고 TXT는 URL만 저장합니다. 일반 종료 시 목록을 자동 저장하지 않으므로 필요한 목록은 직접 저장하세요.

## 설정과 모션

설정은 일반·다운로드·형식·고급·도구로 나뉩니다. 기본값을 따르는 영상에는 공통 설정을 적용하고 영상별 지정은 유지합니다. 변경한 기본 설정은 **설정 저장**으로 보관합니다.

- 형식·해상도·FPS, 오디오 비트레이트, AVI/FLV 인코더, 동시 다운로드 수, 품질 폴백과 파일명 템플릿
- `%title%`, `%author%`, `%id%`, `%date%`, `%num%`, `%num2%`, `%num3%`, `%ext%` 템플릿과 파일명 미리보기
- 전체·선택 작업 취소, 실패 재시도, 제한된 자동 복구, 실제 완료 파일 검증과 같은 이름 파일 보존
- 상태를 잇는 진행 모션, 카드 추가·제거·이동, 패널 전환, 부드러운 스크롤과 대량 목록 가상화
- Windows 애니메이션 설정 반영 및 앱의 **모션 줄이기**

MP4/MKV/MOV/WebM은 불필요한 영상 재인코딩을 피합니다. AVI/FLV 변환에만 선택한 영상 인코더를 사용합니다. 품질 폴백은 요청 해상도를 넘지 않으며, 끄면 제공되지 않는 해상도/FPS는 실패로 표시합니다. 같은 이름은 `(1)` 등의 번호로 보존합니다. 실패·취소 시 `.gmtfv-*` 작업 폴더를 남겨 같은 작업의 자동 복구에서 이어받으며, 수동 재시도는 새 작업으로 시작합니다.

## 앱 자동 업데이트

시작할 때 공식 GitHub의 최신 정식 릴리즈를 확인합니다. 새 버전이 있으면 상단 앱 정보 버튼에 표시합니다. **앱 정보 → 업데이트 다운로드 및 설치**를 선택하면 공식 설치본의 크기와 SHA-256을 검증한 뒤 설치 프로그램을 엽니다. 체크섬 누락·손상·다른 저장소의 주소·이전 버전은 설치하지 않습니다. 확인·다운로드 중 취소와 네트워크 오류 후 재시도를 지원합니다.

다운로드·영상 정보 조회·자막·도구 작업 중에는 설치할 수 없습니다. 설치 직전에 현재 설정과 목록의 링크·옵션을 저장하고 앱을 종료합니다. 다음 실행에서 목록을 복원하고 영상 정보를 다시 조회합니다. 완료·실패 상태나 진행 중 작업을 이어서 복원하는 기능은 아닙니다. 관리자 승인이 필요할 수 있으며, ZIP 실행도 현재 실행 폴더를 대상으로 설치 프로그램을 사용합니다.

설정 → 일반의 **시작할 때 앱 업데이트 확인**을 끄고 저장하면 자동 확인을 중단합니다. 수동 확인은 계속 사용할 수 있습니다. 외부 도구의 설치·업데이트는 **도구 및 진단**에서 별도로 관리합니다.

## 데이터와 진단

- 기존 WPF 설정·로그 경로 `%LocalAppData%/GMTFV.Preview`는 호환성을 위해 유지합니다. 로그는 그 아래 `Logs/`에 기록하며 URL·인증값·사용자 홈 경로를 마스킹합니다.
- 업데이트 전 목록은 같은 경로의 `update-queue.json`에 저장하고 복원 후 `update-queue.json.restored`로 보관합니다.
- 기본 도구 경로는 기존 `%AppData%/GMTFV`입니다. 앱에서 설치하는 새 도구 묶음은 WPF 전용 `Tools/`에 별도로 만들고 공유 도구 실행 파일을 덮어쓰지 않습니다.
- 네트워크 오류는 연결과 서비스 상태, 포맷 오류는 형식·화질, 병합 오류는 FFmpeg/ffprobe, 파일 오류는 권한·공간을 확인하세요. 앱의 실패 안내와 **도구 및 진단 → 로그**에서 자세한 정보를 볼 수 있습니다.

## 빌드·테스트·배포

Windows x64와 .NET 10 SDK를 사용합니다. Core/DownloadEngine은 `net472` 호환 타깃도 유지하므로 **전체 솔루션** 빌드에는 .NET Framework 4.7.2 Developer Pack이 추가로 필요합니다. WPF 앱만 빌드하면 .NET 10 SDK로 충분합니다.

```powershell
dotnet build GMTFV.slnx -c Debug -m:1 -nr:false -p:UseSharedCompilation=false
.\tests\GMTFV.Tests\bin\Debug\net10.0-windows\GMTFV.Tests.exe
.\tests\GMTFV.Wpf.Tests\bin\Debug\net10.0-windows\GMTFV.Wpf.Tests.exe
```

[Inno Setup 6](https://jrsoftware.org/isinfo.php)를 설치한 뒤 아래 명령으로 런타임 포함 ZIP·설치본·SHA-256 목록을 생성합니다. 앱 버전은 WPF 프로젝트에서 읽습니다.

```powershell
.\publish.ps1
# 다른 위치에 설치했다면: .\publish.ps1 -InnoCompiler 'D:\Inno Setup 6\ISCC.exe'
```

출력은 `artifacts/GMTFV-4.0.0.0-win-x64.zip`, `output-setup/GMTFV-4.0.0.0-setup.exe`, `artifacts/GMTFV-4.0.0.0-SHA256SUMS.txt`입니다. 실제 도구가 있으면 `python tests/media_smoke.py --tools-dir <도구 폴더>`로 자체 생성 미디어와 로컬 서버를 사용하는 13개 형식 검사를 실행할 수 있습니다. YouTube 다운로드 성공을 대신 검증하는 테스트는 아닙니다.

## 현재 범위

Chrome 탭 가져오기와 앱 내 영상 재생은 4.0에 포함되지 않습니다. URL 입력·파일 목록 가져오기와 브라우저에서 영상 열기를 사용할 수 있습니다. 설계·기능별 구현 범위는 [리워크 보고서](docs/wpf-rework.md), 4.0 전환과 배포 검증은 [릴리즈 기록](docs/release-4.0.0.0.md)을 참고하세요.

## 라이선스

GMTFV는 [GPL-3.0](LICENSE.txt)으로 배포합니다. 외부 도구·라이브러리와 포함된 .NET 런타임에는 각각의 [라이선스와 고지](THIRD-PARTY-NOTICES.md)가 적용됩니다. 다운로드 권한이 있는 콘텐츠에 사용하세요.