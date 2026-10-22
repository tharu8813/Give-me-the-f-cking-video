# GMTFV 4.0.1.0 안정성 개선 및 검증 보고서

검증일: 2026-10-22 (KST). 작업 시작 시 원격 `master`와 로컬 HEAD는 모두 `1e064c26d6b62f2bf96bf01b8b5cd9f1f635c397`이었다. WPF / MVVM / .NET 10을 유지했고 Core/Engine의 net472 빌드 호환성도 유지했다. 사용자 설정·다운로드·기존 도구 묶음은 대상으로 삭제하지 않았다.

## A. 발견한 문제

| 파일/구성 요소 | 문제 | 원인/확인 범위 | 위험도 | 수정 결과 |
|---|---|---|---|---|
| ToolSetupService | 실패 원인·재시도·실제 전송 피드백 부족 | 코드 확인. 특정 신규 PC에서 발생한 원래 예외·로그는 제공되지 않아 그 PC의 원인은 미확정 | P0 | HTTP/Windows 코드·예외 체인 진단, 사전 쓰기/이동/공간 검사, 제한된 재시도, 전송량·속도·ETA |
| 도구 설치 재시도 | 뒤쪽 도구 실패 후 앞 도구도 다시 전송 | 매번 GUID 작업 폴더를 지우고 다시 전부 받는 기존 구조 | P0 | SHA-256별 캐시를 재검증하여 재사용. 손상 캐시는 재수신, 기존 활성 묶음 보존 |
| ProcessRunner | 정상 장시간 다운로드가 stdout 16MiB 초과로 실패 | 기존 누적 출력 상한과 verbose 조합. 회귀 검사에서 18MB 출력 사용 | P0 | 스트리밍 출력 64KiB 꼬리 보존, 긴 단일 줄도 제한, 메타데이터 전체 JSON의 별도 16MiB 상한 유지 |
| 오디오 명령/후처리 | 원본 저장 옵션이 없고 처리 결정을 yt-dlp 추출에 일임 | 기존 코덱 호환 Stream Copy는 이미 [yt-dlp 공식 후처리](https://github.com/yt-dlp/yt-dlp/blob/master/yt_dlp/postprocessor/ffmpeg.py)가 지원했음. 모든 기존 형식이 무조건 재인코딩됐다는 주장은 하지 않음 | P0 | 원본 옵션, 실제 FFprobe 코덱·컨테이너에 따른 저장/Stream Copy/재인코딩 정책 |
| CompletedFile | 고정 확장자·헤더 검사만으로 완료 | 실제 스트림·코덱 조사 없음 | P0 | 동적 원본 확장자, 경로/링크/헤더 및 FFprobe 구조·길이·컨테이너/코덱 검사 후 게시 |
| 진행률 | 병합/변환 구분과 생존 피드백 부족 | 후처리 상태만 표시하고 경과 시간 없음. 동일 상태 내 스트림 전환도 200ms 제한 대상 | P0 | 영상/오디오 스트림·병합·remux·인코딩 구분, 직접 FFmpeg의 측정 진행률, 내부 후처리의 불확정 막대·경과 시간 |
| 메인/기본/개별 설정 | ComboBox와 체크박스의 모드 표현 불일치 | 같은 IsVideo를 다른 방식으로 표현 | P1 | 세 영역의 라디오 버튼, 키보드 포커스 스타일, 유효 형식 전환·상속 유지 |
| MainWindowViewModel | 퍼센트마다 전체 큐 요약 무효화, 공개 시작 메서드의 종료 추적 누락 | 개수 요약에 필요 없는 Percent 구독과 명령 래퍼에 의존하는 작업 추적 | P1 | 100회 퍼센트 변경 시 요약 갱신 0회. 직접 시작/재시도/목록 작업도 종료 시 대기 |
| 목록/설정 저장 | UI에서 JSON 생성·해석/동기 설정 쓰기 | 기존 ViewModel의 파일/직렬화 처리 | P1 | QueueListStore 분리, 비동기/원자적 저장, 백그라운드 JSON 해석, 목록 상태·취소·중복 실행 방지 |
| 배포 문서 | 제거된 WinForms 설치 경로를 안내 | 과거 문서 잔존 | P1 | WPF 4.0.1.0 빌드·패키지·격리 설치 검증 문서로 교체 |

DNS/TLS/프록시/Defender/잠금/디스크 부족은 실제로 모두 발생시켰다는 뜻이 아니다. 예외·응답·스트림 대역으로 구분 동작을 재현했다. 백신 판단은 Windows 225/226 코드가 있을 때만 하고 단순 접근 거부를 백신으로 단정하지 않는다.

## B. 실제 수정 내역

### 도구 설치와 복구

`ToolSetupService.cs`, `ToolTransfer.cs`, `ToolSetupDiagnostics.cs`, `ToolWorkspaceViewModel.cs`, `DiagnosticsView.xaml`을 수정·추가했다.

- 일반 사용자 데이터 아래 별도 도구 루트에서 디렉터리 생성, 쓰기·이동, 최소 1GiB 여유 공간을 검사한다. 루트별 설치 잠금으로 중복 준비를 막는다.
- 공식 GitHub 저장소와 [FFmpeg가 안내하는 Gyan 배포](https://ffmpeg.org/download.html)를 유지한다. SHA-256을 확인한 파일만 실행하며 TLS·해시 검증을 우회하지 않는다.
- 연결 제한 15초, 응답 헤더 30초, 데이터 무응답 30초, 전체 전송 15분. 서버 5xx, 일시 연결 실패 등에 최대 3회 지수 Backoff/Jitter를 적용한다. 403·인증서·파일 권한·해시 오류는 일반 반복 대상으로 삼지 않는다.
- HTTP 429의 Retry-After를 따른다. 60초보다 긴 대기 지시와 장시간 GitHub API 제한은 자동 대기를 멈추고 기술 상세에 대기 정보를 남긴다. API 제한의 원래 HTTP 403 코드도 보존한다.
- Range/ETag를 확인하지 않은 부분 전송은 재사용하지 않는다. 재시도는 해당 전송을 0바이트부터 시작한다. 이미 완전히 검증된 다른 도구는 digest 캐시에서 재사용한다.
- 다운로드 → SHA → 안전한 선택 압축 해제 → 실행/버전/Deno 호환성 확인 → 새 묶음 반환 → 설정 저장/활성화 순서를 유지한다. 기존 정상 묶음과 실패 시 설정은 보존한다.
- 도구 이름·8단계·바이트·전송 퍼센트·속도·계산 가능한 ETA를 표시한다. 검증·압축 해제·실행 검사에서는 다운로드 퍼센트를 지우고 불확정 상태를 사용한다. 취소·재시도·상세 로그가 연결된다.

### 오디오 및 완료 파일

`AudioProcessor.cs`, `YtDlpDownloadCommandFactory.cs`, `DownloadService.cs`, `CompletedFile.cs`, `DownloadModels.cs`를 수정·추가했다.

| 요청 | 실제 처리 정책 |
|---|---|
| original | bestaudio 전용. 실제 원본 컨테이너·확장자·바이트 보존 |
| M4A | AAC+M4A면 직접 저장. AAC+다른 컨테이너면 Stream Copy. 다른 코덱이면 AAC 인코딩 |
| AAC | AAC+ADTS면 직접 저장. AAC+M4A 등은 ADTS로 Stream Copy |
| Opus | Opus+Ogg Opus면 직접 저장. WebM의 Opus는 Ogg Opus로 Stream Copy |
| OGG | Vorbis만 직접 저장/Stream Copy. Opus 등은 Vorbis로 인코딩 |
| MP3 / FLAC | 같은 실제 코덱·컨테이너면 보존. 다른 코덱은 요청 형식으로 변환 |
| WAV | 호환 PCM/WAV는 보존. 다른 원본은 PCM WAV로 변환 |

확장자가 같아도 실제 코덱이 다르면 직접 저장하지 않는다. 비트레이트는 손실 오디오를 재인코딩할 때만 FFmpeg에 전달한다. 원본 저장·Stream Copy·FLAC/WAV에는 목표 비트레이트를 적용하지 않는다. 변환 원본은 검증·게시 성공 후에만 작업 폴더에서 정리하고 실패·취소 시 보존한다. 기존 최종 파일은 덮어쓰지 않는다.

완료 검사는 경로 경계·링크 여부·파일 서명뿐 아니라 FFprobe의 실제 스트림, 길이, 컨테이너, 오디오 코덱을 확인한다. 앱의 런타임 검사는 구조 검사이며 모든 파일을 끝까지 디코딩하는 전수 검사는 아니다. 통합 테스트에서는 실제 출력 파일을 끝까지 디코딩했다.

### 진행률·UI·비동기

`DownloadItemViewModel.cs`, `MainWindowViewModel.cs`, `ItemSettingsViewModel.cs`, `MediaFormatChoice.cs`, `QueueListStore.cs`, `UiConverters.cs`, `Controls.xaml`, `Theme.xaml`과 관련 View XAML을 수정했다.

- yt-dlp 구조화 이벤트에 스트림 ID와 코덱을 포함한다. 스트림·단계가 바뀌면 즉시 전달하고 같은 단계의 빈번한 이벤트만 200ms로 제한한다.
- 직접 관리하는 오디오 FFmpeg는 `-progress pipe:1`의 out_time_us와 실제 미디어 길이로 처리 퍼센트·경과·ETA를 계산한다. yt-dlp 내부 FFmpeg의 세부 퍼센트를 얻는다고 가정하지 않는다.
- 내부 병합·remux·영상 변환은 불확정 막대와 초 단위 경과 시간을 표시한다. 알 수 없는 후처리나 파일 이동을 영상 인코딩으로 잘못 표시하지 않는다. 큐 전체는 검증을 끝낸 항목 개수 비율이다.
- 상태 변경·재시도에서 이전 퍼센트를 초기화하며 완료·실패·취소 뒤에 도착한 진행 이벤트를 무시한다. 100% 완료는 검증·최종 게시 후에만 사용한다.
- 세 화면의 모드 선택은 같은 IsVideo에 바인딩한 라디오 버튼이다. unchecked 이벤트로 반대 모드를 덮어쓰지 않는다. 새 모델의 형식 ID는 기존 JSON 문자열과 호환된다.
- 원본/FLAC/WAV 선택 시 비트레이트 입력을 비활성화하고 적용 조건·손실 음질 복구 불가를 설명한다. 원본 파일명 미리보기는 확정되지 않은 확장자를 명시한다.
- 목록 저장·열기·해석은 별도 서비스에서 수행하며 2MiB 입력 상한, 임시 파일 후 원자적 교체, 기존/레거시 JSON과 TXT, 개별 옵션 상속을 보존한다. 목록 취소는 실행/대기 중 메타데이터 작업까지 전달된다.
- 기존 ProcessJob의 소유 자식 프로세스 정리, 도구 버전 캐시, 3개 메타데이터 슬롯, 큐 동시 슬롯, 가상화, 모션/모션 줄이기, 자막·자동 업데이트 기능은 유지했다.

## C. 테스트 및 빌드

검증 호스트는 Windows 11 x64(build 26200)이다. 사용자 프로필·실제 설치본과 분리한 작업 경로를 사용했다.

```powershell
dotnet build GMTFV.slnx -c Debug --no-restore -m:1 -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false
dotnet build GMTFV.slnx -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false
.\tests\GMTFV.Tests\bin\Debug\net10.0-windows\GMTFV.Tests.exe
.\tests\GMTFV.Tests\bin\Release\net10.0-windows\GMTFV.Tests.exe
.\tests\GMTFV.Wpf.Tests\bin\Debug\net10.0-windows\GMTFV.Wpf.Tests.exe
.\tests\GMTFV.Wpf.Tests\bin\Release\net10.0-windows\GMTFV.Wpf.Tests.exe
python tests/media_smoke.py --tools-dir <실제 검증한 도구 묶음>
.\tests\GMTFV.Wpf.Tests\bin\Debug\net10.0-windows\GMTFV.Wpf.Tests.exe --tool-setup-smoke
.\tests\GMTFV.Wpf.Tests\bin\Debug\net10.0-windows\GMTFV.Wpf.Tests.exe --live-metadata-smoke
.\publish.ps1
pwsh -NoProfile -File tests/installer_smoke.ps1
```

| 검사 | 결과/근거 |
|---|---|
| Debug / Release 전체 솔루션 | 성공, 컴파일 오류 0. net472 포함. NU1900 경고는 아래 제약 참조 |
| Core/Engine | Debug/Release 각각 116개 성공 |
| WPF | Debug/Release 각각 203개 성공. 바인딩 오류 0, 1,000행 가상화·모션·상속·라디오·종료·설치 장애 대역 검사 포함 |
| 실제 미디어 | 한글·공백 경로에서 34개 성공. 영상 6형식, 오디오 8선택, 직접 저장·Stream Copy·인코딩, 결합/분리 스트림, 실제 FFmpeg 변환/yt-dlp 병합 취소, 인코더 오류 후 수동 복구 |
| 실제 공식 도구 설치 | 한글·공백 도구 경로에서 SHA-256/압축 해제/버전/실제 실행 성공. 같은 릴리즈의 검증 캐시 재사용 및 새 ToolWorkspace 재검사 성공 |
| 실제 YouTube | 공개 샘플 `jNQXAC9IVRw` 메타데이터를 실제 WPF 백엔드로 조회 성공. 실제 YouTube 스트림 다운로드 성공률 검사는 하지 않음 |
| 설치·업그레이드·제거 | 별도 AppId로 현재 사용자 4.0.0.0 설치 → 4.0.1.0 업그레이드 → 제거. 버전·바이너리 해시·런타임/라이선스·사용자 파일 보존·저장소 폴더 제외 검사 |

설치 장애 대역은 DNS/TLS/프록시/403/429/5xx, Retry-After, 헤더/전체·무응답 타임아웃, 전송 중단·크기 불일치, 취소, 해시 불일치, 손상/잘못된 압축 구성, 캐시 손상과 재수신, Windows 권한/잠금/공간/백신 오류 코드 등을 확인했다. 실제 디스크를 가득 채우거나 백신을 의도적으로 작동시키지는 않았다.

미디어 테스트는 파일 존재 여부에 그치지 않고 FFprobe 코덱·컨테이너, 전체 디코딩, 직접 저장의 SHA-256 동일성, 영상 remux의 프레임 해시를 검사한다. 초기 샌드박스의 loopback 연결은 WinError 10013으로 차단되어 권한을 확장한 동일 검사로 완료했다.

실행 로그: `artifacts/stability-build-{debug,release}.txt`, `stability-engine-{debug,release}.txt`, `stability-wpf-{debug,release}.txt`, `stability-media.txt`, `stability-live-install.txt`, `stability-live-metadata.txt`, `stability-publish.txt`, `stability-installer.txt`. 실제 WPF 렌더: `artifacts/wpf-preview/`.

## D. 측정한 결과와 성능 범위

| 동일 조건의 비교 | 측정값 |
|---|---|
| 동일 공식 릴리즈를 새로 다운로드·설치 | 18.224초 |
| 같은 릴리즈를 SHA 재검증 캐시로 새 묶음 준비 | 4.978초 |
| 퍼센트 변경 100회에 따른 큐 개수 요약 갱신 | 0회 |
| 정상 스트리밍 stdout 약 18MB | 출력량 때문에 실패하지 않음. 보존 꼬리 상한 64KiB |

설치 시간은 **동일 코드에서 캐시 없음/있음** 비교다. 이전 버전 대비 설치 속도 개선율이 아니다. 1초짜리 생성 미디어의 엔진 전 과정 시간은 로그에 기록하지만 실제 긴 영상의 속도로 일반화하지 않는다. CPU/최대 메모리, 실서비스 다운로드 처리량, 장시간 작업·GPU의 전후 비교는 측정하지 않았으므로 수치를 제시하지 않는다.

## E. 배포 준비와 남은 제약

- 앱/어셈블리/파일 버전과 설치 기본 버전을 4.0.1.0으로 맞췄다. 기존 AppId, 사용자 데이터 경로, IsVideo/UseDefaults와 JSON 구조를 유지한다. 4.0.0.0 설정 및 이전/문자열/레거시 목록과 새 원본 옵션의 저장·복원을 회귀 검사했다.
- 자체 포함 Windows x64 ZIP과 Inno 설치본을 생성하고 최종 산출물의 체크섬을 다시 확인했다. `AppSourcePath` 전용 매크로로 앱 출력만 포함하며 테스트용 원본 경로 오버라이드를 지원한다. 처음 검증 중 발견된 Inno 기본 변수 이름 충돌은 수정하고 저장소 폴더 포함 방지 검사를 추가했다.
- 로컬 설치본: `output-setup/GMTFV-4.0.1.0-setup.exe`. ZIP: `artifacts/GMTFV-4.0.1.0-win-x64.zip`. 체크섬: `artifacts/GMTFV-4.0.1.0-SHA256SUMS.txt`.
- 이 보고서의 초기 로컬 검증 단계에서는 GitHub 업로드·공개 릴리즈·push를 수행하지 않았다. 이후 UI/UX 보완을 포함한 배포 안내는 [4.0.1.0 릴리즈 안내](release-4.0.1.0.md)를 참고한다. 자동 업데이트 배포에는 정확한 설치 자산과 SHA-256 digest가 필요하다. 현재 사용자 격리 설치 업그레이드는 별도 AppId로 검증하며 실제 사용자 설치 등록을 바꾸지 않는다.
- **직접 검증하지 못한 환경:** 깨끗한 Windows 10/11 VM, 실제 한글 Windows 계정, 여러 종류의 기업 프록시/백신/방화벽, Program Files/UAC 업그레이드, GPU 하드웨어 인코딩, 수 시간의 대형 파일 병합·장시간 UI/동시 부하.
- **환경 제약:** NuGet 취약성 데이터 소스 접근 실패로 NU1900 경고가 남는다. 취약성 검사를 끄지 않았으며 새 의존성을 추가하지 않았다. 빌드 성공을 취약성 조회 성공으로 해석하지 않는다.
- **운영 범위:** 검증 캐시와 이전 도구 묶음은 자동 삭제하지 않아 버전이 쌓이면 저장 공간을 사용한다. 필요 시 사용자가 확인하고 정리해야 한다. 원본 보존은 현재 지원하는 YouTube 오디오 컨테이너를 대상으로 한다. 실제 도구 다운로드/미디어 검증 성공이 모든 신규 PC와 향후 YouTube 변경을 보장하지 않는다.

초기 안정성 검증 근거: `artifacts/stability-installer-71c472db58a248428019ae480b61858c/`. 패키지 경계·크기·체크섬 검사: `artifacts/stability-package.json`. 아래 해시는 UI/UX 추가 수정 전의 로컬 산출물 기록이며, 최종 배포본의 해시는 릴리즈의 `GMTFV-4.0.1.0-SHA256SUMS.txt`를 기준으로 확인한다.

```text
001bea3331040ce4dfdbe6348aa6558d5cb28c8f8c1bdb3ff8258355e49901ca  GMTFV-4.0.1.0-win-x64.zip
897ab5a38332107b8425c56b1de1d67984862b2376fc86034a60333a0b9435f2  GMTFV-4.0.1.0-setup.exe
```
