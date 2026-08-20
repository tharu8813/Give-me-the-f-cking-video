# P5 — 가상화 다운로드 목록

## 구현 내용

- `MainWindowViewModel.DownloadItems`를 목록의 단일 데이터 원본으로 만들었다.
- `ListBox`는 `VirtualizingStackPanel`과 `VirtualizationMode="Recycling"`을 사용한다.
- 스크롤은 논리 스크롤(`CanContentScroll`)을 사용해 많은 항목에서도 컨테이너를 재활용한다.
- `SelectedDownload`은 선택 상태와 하단 요약을 연결한다.
- 항목 ViewModel은 제목, 채널, 길이, 단계, 상태 문자열, 퍼센트를 갖는다. P7이 `DownloadProgress`를 이 속성으로 갱신한다.
- 목록이 비어 있으면 이전의 안내 화면이 유지된다.

## 성능 규칙

1. P6 이후 항목 추가/삭제는 `DownloadItems`에서만 수행한다. 컨트롤을 직접 생성하거나 `Children`에 추가하지 않는다.
2. P7의 진행률 갱신은 항목 속성만 바꾸며, 전체 목록을 다시 할당하지 않는다.
3. 썸네일은 P6에서 비동기로 준비하고, 화면에 보이는 항목만 표시할 수 있게 별도 이미지 캐시를 둔다.
4. 목록 행의 높이를 안정적으로 유지해 진행률 변화가 스크롤 위치를 흔들지 않게 한다.
