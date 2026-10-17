# 제3자 구성 요소

GMTFV 소스의 라이선스는 GPL-3.0입니다. 각 도구와 라이브러리는 해당 저작권 및 라이선스가 적용됩니다. 패키지 버전은 `packages.config`에 고정되어 있습니다.

| 구성 요소 | 용도 | 라이선스·원본 |
| --- | --- | --- |
| yt-dlp Windows 실행 파일 | 영상 조회·다운로드·후처리 조립 | [Unlicense 및 번들 GPLv3+ 구성 요소](https://github.com/yt-dlp/yt-dlp/blob/master/THIRD_PARTY_LICENSES.txt) |
| yt-dlp-ejs | YouTube JavaScript 처리, 공식 yt-dlp 실행 파일에 포함 | [yt-dlp-ejs](https://github.com/yt-dlp/ejs) |
| FFmpeg / ffprobe | 스트림 병합·컨테이너 변경·오디오 추출·변환 | [빌드별 LGPL/GPL 조건](https://ffmpeg.org/legal.html), [Gyan 배포](https://www.gyan.dev/ffmpeg/builds/) |
| Deno | yt-dlp JavaScript 런타임 | [MIT 및 제3자 고지](https://github.com/denoland/deno/blob/main/LICENSE.md) |
| Newtonsoft.Json | JSON | [MIT](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md) |
| Microsoft.Web.WebView2 | 선택적인 웹 미리보기 | [Microsoft 라이선스](https://www.nuget.org/packages/Microsoft.Web.WebView2/) |
| WindowsAPICodePack | 폴더 선택 UI | [패키지 라이선스](https://www.nuget.org/packages/WindowsAPICodePack/) |
| System.Resources.Extensions 및 의존 패키지 | WinForms 리소스 로딩 | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |

기본 설치 파일에는 관리 라이브러리와 이 고지 파일이 포함됩니다. yt-dlp, FFmpeg/ffprobe, Deno는 사용자의 도구 폴더에 별도로 준비됩니다. 실행 파일을 설치본에 직접 번들할 경우 각 배포본의 라이선스·저작권 고지·해당 소스 제공 의무를 함께 충족해야 합니다.

다운로드한 FFmpeg 배포본의 라이선스와 문서는 도구 폴더의 `ffmpeg-notices/`에 보관합니다. 원본 라이선스는 각 배포본 및 패키지에도 포함됩니다.
