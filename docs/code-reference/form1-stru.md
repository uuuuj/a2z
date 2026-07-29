# Form1.Stru.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Stru.cs` (약 1,076 라인)

**책임**: STRU 목록 수집·검색·선택 가시성, 검색된 STRU 격리·화면 맞춤, 체크 STRU의 4종 도면 PDF 일괄 출력.

## 주요 핸들러

### <a id="BtnStruSearch_Click"></a>BtnStruSearch_Click

- **라인**: L238~L242
- **트리거**: STRU 검색 영역의 `검색` 버튼 클릭
- **핵심**: 입력된 이름을 `SearchStruByName`에 전달한다. Enter 키 이벤트는 연결하지 않으며 치수 추출도 호출하지 않는다.
- **흐름 문서**: [기능/치수/STRU 검색 치수 추출.md](../기능/치수/STRU%20검색%20치수%20추출.md)

### <a id="btnExtractDrawingList_Click"></a>btnExtractDrawingList_Click

- **라인**: L499~L785
- **트리거**: `btnExtractDrawingList` 버튼 클릭
- **핵심**: 체크 STRU 순회 → STRU별 `ProcessSingleStruFull` → **STRU 처리 직후 `FlushPendingMergedPdf`로 묶음 PDF 1개 저장** → 전체 BODY·UI 복원
- **저장 위치가 여기인 이유 (#119)**: 취소는 `ProcessSingleStruFull` 중간에서 예외로 튀어나온다. 저장을 그 함수 안에 두면 취소 시 그때까지 그린 페이지를 잃는다. STRU별 `try/catch` **바로 뒤**에 두면 정상 완료·실패·취소 세 경로가 모두 이 지점을 지난다.
- **취소**: 공용 처리 오버레이의 취소 요청을 STRU 시작·종료 경계에서 확인하고, 부분 2D·3D·시트·치수 상태를 정리한 뒤 완료/전체·PDF 수를 표시. PDF 수는 곧 저장까지 도달한 STRU 수다.
- **흐름 문서**: [기능/도면시트/도면 일괄 출력.md](../기능/도면시트/도면%20일괄%20출력.md)

## 내부 핵심 메서드

### <a id="InitStruSearchUI"></a>InitStruSearchUI

- **라인**: L187~L236
- STRU 목록 하단에 검색 입력창과 `검색` 버튼을 코드로 생성한다.
- 자동완성은 유지하지만 `KeyDown` 이벤트를 등록하지 않아 Enter만으로 검색되지 않는다.

### <a id="SearchStruByName"></a>SearchStruByName

- **라인**: L248~L315
- 모델·검색어·STRU 목록을 검증한 뒤 완전일치, 부분일치 순으로 STRU를 찾는다.
- 전체 BODY를 숨기고 검색된 STRU의 후손 BODY만 표시한 뒤 목록 선택과 카메라 fit을 수행한다.
- 같은 STRU 재검색 시 `SelectedIndexChanged`가 발생하지 않으므로 `PerformFlyToSelectedStru`를 직접 호출한다.
- 기존 `btnMainDimension_Click` 자동 호출은 제거되어 검색만으로 BOM·치수·시트 데이터가 바뀌지 않는다.

### <a id="FindStruIndexByName"></a>FindStruIndexByName

- **라인**: L317~L332
- 대소문자를 구분하지 않는 완전일치를 우선하고, 없으면 첫 부분일치 항목을 반환한다.

### <a id="PerformFlyToSelectedStru"></a>ClbStruList_SelectedIndexChanged / PerformFlyToSelectedStru

- **라인**: L425~L489
- 선택 STRU의 후손 BODY만 보이게 한 뒤 카메라를 해당 BODY에 맞춘다.
- 목록 선택 이벤트와 같은 STRU 재검색이 이 공통 동작을 사용한다.

### <a id="ProcessSingleStruFull"></a>ProcessSingleStruFull

- **라인**: L787~L1080
- **시그니처**: `int ProcessSingleStruFull(Node struNode, string saveDir, Action<int> reportPdfSaved = null)`
- STRU 후손 BODY 격리 → BOM 재수집 → 비동기 간섭검사·자동 시트 생성 → **페이지 누적 시작 + 저장 경로 예약** → 일반 시트를 캔버스에 쌓기 → 가공도를 같은 누적에 이어붙이기 순으로 처리한다. **저장은 하지 않는다** — 호출부가 마무리한다.
- 누적 시작은 `BeginPdfPageAccumulation`, 저장 경로 예약은 `_pendingMergedPdfPath = BuildMergedDrawingPdfPath(struSubDir, $"전체도면_{safeStruName}", timeStamp)`.
- 가공도는 `GenerateMfgDrawingManual`을 그대로 부르되, 그 함수가 이미 열린 누적을 감지해 저장을 넘기고 페이지만 얹는다. 그래서 일반 도면과 가공도가 한 파일에 들어간다.
- BOM 수집은 매 부재·홀마다 진행/취소를 확인하고, 일반 도면은 엣지·템플릿·각 뷰, 가공도는 페이지·행·주/보조 뷰 경계에서 요청을 확인한다.
- 간섭검사가 이미 실행 중이면 `Clash.IsBusy` 해제와 완료 이벤트 처리를 기다린 뒤 `OperationCanceledException`으로 상위 반복을 종료한다.
- 한 시트 생성이 실패하면 `DiscardCurrentPdfPage`로 그 캔버스만 버리고 다음 시트로 계속한다.
- 반환값은 이 함수가 저장한 PDF 수(항상 0)다. 실제 집계는 호출부의 `FlushPendingMergedPdf` 결과로 이뤄진다.

## 공유 취소 상태

공용 필드는 `Form1.cs`에 있으며 치수 추출과 일괄 출력이 함께 사용한다.

| 필드/메서드 | 역할 |
|---|---|
| `_cancelableOperationInProgress` | 취소 버튼 표시 여부와 작업 소유 상태 |
| `_cancelRequested` | 사용자가 누른 취소 요청 |
| `BeginCancelableOperation` / `EndCancelableOperation` | 요청 상태 초기화·해제 |
| `IsCancellationRequested` | 체크포인트 진단 로그와 요청 조회 |
| `ThrowIfCancellationRequested` | 동기 일괄 출력 흐름을 `OperationCanceledException`으로 탈출 |
| `ProcessCancelableUiCheckpoint` | 진행 문구 갱신 → `Application.DoEvents()` → 안전 지점 취소 예외 발생 |

## 관련 문서

- [STRU 도면 일괄 출력](../기능/도면시트/도면%20일괄%20출력.md)
- [간섭검사 완료 이벤트](../기능/간섭검사/간섭검사%20완료%20이벤트.md)
- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
