# 빌드 및 설치본

공식 UI는 `src/GMTFV.WinForms/GMTFV.WinForms.csproj`다. Core와 DownloadEngine을 포함해 모두 .NET Framework 4.7.2를 사용한다. WPF Preview와 전용 설치 스크립트는 제거했다.

README의 MSBuild 복원·Debug/Release 빌드·테스트 명령을 실행한다. 출력은 `src/GMTFV.WinForms/bin/<Configuration>`이며 관리 DLL은 `DLLs`에 배치한다. Chrome 확장 ZIP은 빌드 시 풀어 함께 배포한다.

Inno Setup 6에서 `setup_winform.iss`를 컴파일하면 `output-setup/GMTFV-3.6.0.0-setup.exe`가 생성된다. 앱과 설치본 버전은 3.6.0.0이며 기존 AppId 및 설치 경로를 유지한다. 설치 스크립트는 설치 폴더 전체를 삭제하거나 기존 설치 제거 프로그램을 강제 실행하지 않는다.

기본 설치본에는 앱·관리 라이브러리·Chrome 확장·라이선스 고지가 들어간다. yt-dlp, FFmpeg/ffprobe, Deno는 `%AppData%/GMTFV`에서 별도로 준비한다. 외부 도구를 설치본에 추가한다면 해당 배포본의 라이선스와 소스 제공 조건을 확인해야 한다. 자동 업데이터는 정확한 `GMTFV-<버전>-setup.exe` 자산을 선택하고 SHA-256을 검증한다.

실제 설치·기존 설치 업그레이드·제거는 아직 수동 검증 대상이다. 최신 자동 검증 범위는 [안정화 보고서](stabilization-report.md)를 참고한다.
