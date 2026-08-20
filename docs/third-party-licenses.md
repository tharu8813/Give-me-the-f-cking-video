# 제3자 도구 라이선스 및 배포 안내

이 문서는 GMTFV 배포본에 포함되는 외부 실행 파일과 플러그인의 출처 및 라이선스 의무를 기록합니다. 법률 자문이 아니므로 상용 또는 공개 배포 전에는 법률 전문가의 검토를 권장합니다.

## 포함 도구

| 구성 요소 | 현재 포함 버전 | 라이선스 | 원본/소스 |
| --- | --- | --- | --- |
| yt-dlp | 2026.07.04 | PyInstaller 배포 파일은 GPL-3.0-or-later 결합물 | https://github.com/yt-dlp/yt-dlp |
| FFmpeg (Gyan essentials) | 2026-08-17-git-426841da9d | GPL-3.0-or-later | https://www.gyan.dev/ffmpeg/builds/ |
| Deno | 2.9.5 | MIT | https://github.com/denoland/deno |
| bgutil-ytdlp-pot-provider | 1.3.1 | GPL-3.0-only | https://github.com/Brainicism/bgutil-ytdlp-pot-provider |

## 현재 배포 구조

- 대용량 외부 실행 파일은 소스 저장소와 Git LFS에 커밋하지 않습니다. README의 공식 링크와 아래 재현 절차로 로컬에 준비한 뒤 설치 파일에 포함합니다.
- `yt-dlp.exe`, `ffmpeg.exe`, `deno.exe`, `pot-provider.exe`는 설치 폴더에 포함합니다.
- `yt-dlp-plugins/bgutil-ytdlp-pot-provider.zip`에는 공식 bgutil 1.3.1 플러그인을 수정하지 않은 ZIP 형태로 포함합니다. 현재 yt-dlp 플러그인 로더가 인식할 수 있도록 압축을 풀지 않습니다.
- `pot-provider.exe`는 bgutil 1.3.1 서버를 Deno 2.9.5로 컴파일한 파일이며, 외부 노출을 막기 위해 `127.0.0.1`에서만 수신하도록 수정했습니다.
- 해당 수정은 [bgutil-ytdlp-pot-provider-1.3.1-localhost.patch](../third-party/bgutil-ytdlp-pot-provider-1.3.1-localhost.patch)에 기록되어 있습니다.

## 배포 전 필수 확인 목록

1. 설치 파일에 루트의 `LICENSE.txt`(GPLv3 전문), `licenses/MIT-Deno.txt`, `licenses/Unlicense-yt-dlp.txt` 및 이 문서를 포함합니다.
2. 배포 페이지와 프로그램의 정보 화면에 FFmpeg, yt-dlp, Deno, bgutil의 이름·라이선스·소스 위치를 표시합니다.
3. 현재 포함하는 각 GPL 구성 요소와 **정확히 일치하는 대응 소스**를 사용자에게 제공하거나, 최소 3년 동안 무상 제공할 수 있는 방식으로 명확히 안내합니다.
   - yt-dlp: 해당 릴리스의 소스 아카이브와 PyInstaller 배포에 필요한 라이선스 고지
   - FFmpeg: `2026-08-17-git-426841da9d` 빌드와 일치하는 FFmpeg 및 포함 외부 라이브러리의 소스·빌드 설정
   - bgutil: 1.3.1 소스, 이 저장소의 로컬호스트 패치 및 아래 재현 절차
4. `pot-provider.exe`에 포함된 npm 의존성의 라이선스 고지를 생성하여 함께 제공합니다. 의존성을 갱신하거나 다시 컴파일한 경우 고지도 다시 생성해야 합니다.
5. GPL 구성 요소를 교체·갱신하면 버전, SHA-256, 소스 URL 및 고지 파일을 함께 갱신합니다.

## bgutil 공급자 재현 절차

다음 절차는 배포한 `pot-provider.exe`의 대응 소스를 재현하기 위한 기록입니다.

```powershell
git clone --single-branch --branch 1.3.1 https://github.com/Brainicism/bgutil-ytdlp-pot-provider.git
Set-Location bgutil-ytdlp-pot-provider/server
# third-party/bgutil-ytdlp-pot-provider-1.3.1-localhost.patch 적용
deno cache --allow-scripts=npm:canvas --node-modules-dir=auto --frozen src/main.ts
deno compile --no-check --cached-only --allow-env --allow-net --allow-ffi --allow-read --output pot-provider.exe src/main.ts
```

사용한 Deno 버전은 2.9.5입니다. 위 과정은 Windows x64용 실행 파일을 만듭니다.

## 주의

- 현재 Gyan FFmpeg essentials 바이너리는 실행 파일 자체가 GPLv3라고 표시하며 `libx264`, `libx265` 등을 포함합니다. 단순히 FFmpeg를 별도 실행 파일로 호출한다는 사실만으로 의무가 사라지는 것은 아닙니다.
- yt-dlp의 소스 라이선스는 Unlicense이지만, Windows PyInstaller 실행 파일은 GPLv3+ 구성 요소를 포함합니다.
- 영상 다운로드 권한, 저작권 및 플랫폼 이용약관 준수는 각 사용자 책임입니다.
