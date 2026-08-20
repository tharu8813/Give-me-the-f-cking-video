# P3 — WPF 프로젝트 뼈대

## 구성

- 프로젝트: `src/GMTFV.Wpf/GMTFV.Wpf.csproj`
- 대상: `.NET 10` / `net10.0-windows`
- UI: WPF
- 상태 패턴: `ViewModelBase`를 기반으로 한 MVVM
- 공통 코드: `src/GMTFV.Core`만 참조한다. WinForms 프로젝트는 참조하지 않는다.

## 의도적으로 아직 넣지 않은 기능

P3은 이관 준비 단계이므로 URL 입력, 다운로드, 설정, JSON 가져오기, Chrome 수신, 외부 도구 실행은 아직 WPF에서 수행하지 않는다. 기존 WinForms가 유일한 기능 완성 버전이다.

| 다음 단계 | WPF에 추가할 내용 |
| --- | --- |
| P4 | 색상·타이포그래피·컨트롤 스타일·앱 셸 |
| P5 | 가상화된 다운로드 목록과 선택 상태 |
| P6 | URL 입력·메타데이터·목록 작업 |
| P7 | 다운로드·병합 진행 상태 |
| P8~P9 | 보조 화면·설정·Chrome 연동 |

## 실행·검증

`dotnet build src/GMTFV.Wpf/GMTFV.Wpf.csproj --no-restore`로 빌드한다. 앱 실행은 WPF 창이 열리고 `공통 Core 연결 완료` 상태를 표시하는 것으로 P3 범위를 검증한다.
