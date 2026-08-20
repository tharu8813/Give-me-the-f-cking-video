# GMTFV 빌드 및 설치 파일 배포 안내

## 프로젝트 배치

세 프로젝트는 저장소의 `src` 아래에 같은 깊이로 배치합니다.

```text
src/
  GMTFV.WinForms/   기존 안정판 (.NET Framework 4.7.2)
  GMTFV.Core/       공용 UI 독립 코드 (netstandard2.0)
  GMTFV.Wpf/        새 WPF Preview (.NET 10)
```

두 UI가 공용으로 사용하는 실행 도구는 README 안내에 따라 저장소 루트에 별도로 준비합니다. 실행 파일은 Git에 포함하지 않으며, 프로젝트는 로컬에 존재하는 도구만 링크된 콘텐츠로 빌드 출력에 복사합니다. Chrome 확장, 라이선스 자료와 `icon.ico`는 저장소에서 관리합니다.

## Release 빌드

저장소 루트에서 다음 명령을 실행합니다.

```powershell
dotnet build src/GMTFV.WinForms/GMTFV.WinForms.csproj -c Release
dotnet build src/GMTFV.Wpf/GMTFV.Wpf.csproj -c Release
```

WinForms 출력은 `src/GMTFV.WinForms/bin/Release`, WPF 출력은 `src/GMTFV.Wpf/bin/Release/net10.0-windows`에 생성됩니다.

설치본을 만들기 전에는 루트에 `yt-dlp.exe`, `ffmpeg.exe`, `deno.exe`, `pot-provider.exe`가 모두 있는지 확인한 후 Release를 다시 빌드해야 합니다. 실행 파일이 없는 상태에서도 코드 빌드는 가능하지만 다운로드 기능이 포함된 배포본은 만들 수 없습니다.

## 설치 파일 생성

Inno Setup 6에서 저장소 루트의 스크립트를 각각 컴파일합니다.

- `setup_winform.iss` → `output/GMTFV-<버전>-setup.exe`
- `setup_wpf.iss` → `output/GMTFV-WPF-Preview-0.0.0.1-installer.exe`

두 스크립트는 같은 루트 `icon.ico`를 사용합니다. `output`은 생성 산출물이라 Git에 포함하지 않습니다.

## 기존 사용자와 Preview 공존 정책

- WinForms는 기존 AppId `d27d3702-a216-4b19-aea8-d5367acee59f`, 제품명 `GMTFV`, 설치 경로를 유지합니다. 따라서 기존 설치를 정상 업그레이드합니다.
- WPF Preview는 별도 AppId `9c2c5dc4-42ea-4f7d-b4b5-4b3ee3fd0d61`, 제품명 `GMTFV WPF Preview`, 별도 설치 경로를 사용합니다. WinForms를 제거하거나 덮어쓰지 않습니다.
- WPF의 버전은 현재 `0.0.0.1`이며 실행 파일 제품명과 창 제목에도 Preview가 표시됩니다.
- 두 버전은 설치상 공존할 수 있지만 로컬 Chrome/PO 공급자 포트를 공유하므로 동시에 실행하지 않는 것을 권장합니다.

## 같은 GitHub 릴리스에 올릴 때

두 설치 파일을 한 릴리스에 함께 올려도 WinForms 자동 업데이트가 WPF를 선택하지 않도록 이름을 구분했습니다.

1. WinForms 파일명은 반드시 `GMTFV-<버전>-setup.exe` 규칙을 유지합니다.
2. WPF 파일명에는 기존 WinForms 업데이터가 탐색하는 `setup` 문자열을 넣지 않고 현재 `GMTFV-WPF-Preview-<버전>-installer.exe` 규칙을 유지합니다.
3. 새 WinForms 업데이터는 정확한 안정판 파일명을 우선 선택하고 `WPF`, `Preview` 자산을 제외합니다.

두 번째 규칙은 이미 사용자 PC에 설치되어 있어 수정할 수 없는 구형 업데이터까지 보호하기 위한 호환 장치입니다. WinForms 지원을 완전히 종료하고 기존 사용자의 전환이 끝날 때까지 유지해야 합니다.
