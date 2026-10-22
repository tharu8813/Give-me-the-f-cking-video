# 빌드 및 설치본

현재 앱은 `src/GMTFV.Wpf/GMTFV.Wpf.csproj`(.NET 10 / WPF)이며 버전은 4.0.1.0이다. Core/Engine의 .NET Framework 4.7.2 호환 타깃도 유지한다.

```powershell
dotnet build GMTFV.slnx -c Debug -m:1 -nr:false -p:UseSharedCompilation=false
dotnet build GMTFV.slnx -c Release -m:1 -nr:false -p:UseSharedCompilation=false
.\tests\GMTFV.Tests\bin\Debug\net10.0-windows\GMTFV.Tests.exe
.\tests\GMTFV.Wpf.Tests\bin\Debug\net10.0-windows\GMTFV.Wpf.Tests.exe
.\publish.ps1
```

Inno Setup 6이 필요하다. `publish.ps1`은 프로젝트의 네 자리 버전을 읽어 Windows x64 자체 포함 앱, 런타임·제3자 라이선스, ZIP, 설치본과 SHA-256 목록을 만든다. 4.0.1.0 출력은 다음과 같다.

- `artifacts/GMTFV-win-x64/`
- `artifacts/GMTFV-4.0.1.0-win-x64.zip`
- `output-setup/GMTFV-4.0.1.0-setup.exe`
- `artifacts/GMTFV-4.0.1.0-SHA256SUMS.txt`

설치 AppId와 사용자 데이터 경로는 유지한다. 구 WinForms 실행 파일만 설치 위치에서 제거하며 설정·다운로드·외부 도구는 제거하지 않는다. 외부 도구는 설치본에 포함하지 않고 `%LocalAppData%/GMTFV.Preview/Tools`에 일반 사용자 권한으로 준비한다. 앱 설치는 설치 위치에 따라 관리자 권한이 필요할 수 있고 현재 사용자 설치도 지원한다.

`tests/installer_smoke.ps1`은 이전 4.0.0.0 ZIP과 체크섬이 로컬에 있을 때 별도 임의 AppId와 작업 폴더로 설치 → 4.0.1.0 업그레이드 → 제거를 검사한다. 사용자의 실제 GMTFV 설치 등록과 설정은 대상으로 삼지 않는다. 테스트 파일은 `artifacts/stability-installer-*`에 보관한다.

로컬 패키지 생성은 GitHub 공개 배포가 아니다. 자동 업데이터는 더 높은 정식 버전, 정확한 설치 파일명, 공식 저장소 자산 URL과 SHA-256 digest가 있는 릴리즈를 공개한 후에 새 설치본을 발견한다. 이번 검증 범위는 [안정성 개선 보고서](stability-4.0.1.0.md)를 참고한다.
