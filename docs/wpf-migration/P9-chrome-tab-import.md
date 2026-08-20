# P9 — Chrome 탭 가져오기

## 연결 방식

Chrome는 사용자의 기존 탭 정보를 애플리케이션이 직접 읽지 않고, 사용자가 설치한 확장이 선택한 URL만 loopback HTTP로 전송한다.

```text
Chrome 확장 → http://127.0.0.1:43128/tabs → WPF ChromeTabImportService → 메타데이터 조회 → 가상화 목록
```

## 확장 기능

- 현재 활성 탭 전송
- 체크한 YouTube 탭 전송
- 현재 Chrome 창의 모든 YouTube 탭 전송

확장은 `tabs` 권한과 localhost 수신 주소만 사용한다. 브라우저 방문 기록, 쿠키, 로그인 정보, Chrome 프로필 파일에는 접근하지 않는다.

## 설치 흐름

1. WPF 앱의 좌측 `Chrome 탭`을 누른다.
2. 앱이 `chrome://extensions/` 주소를 클립보드에 복사하고, 배포 폴더의 `chrome-extension` 소스 폴더를 연다.
3. Chrome 개발자 모드에서 `압축해제된 확장 프로그램을 로드합니다`를 누른 뒤 해당 폴더를 선택한다.
4. 확장 팝업에서 보낼 탭 범위를 선택한다.

포트 43128을 이미 다른 GMTFV 창이 사용 중이면 WPF 앱은 충돌 원인을 표시하고 탭 수신을 시작하지 않는다.
