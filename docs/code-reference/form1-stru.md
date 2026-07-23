# Form1.Stru.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Stru.cs` (약 1,065 라인)

**책임**: STRU 목록 수집·검색·선택 가시성, 선택 STRU의 치수 추출, 체크 STRU의 4종 도면 PDF 일괄 출력.

## 주요 핸들러

### <a id="btnExtractDrawingList_Click"></a>btnExtractDrawingList_Click

- **라인**: L497~L761
- **트리거**: `btnExtractDrawingList` 버튼 클릭
- **핵심**: 체크 STRU 순회 → STRU별 `ProcessSingleStruFull` → 생성 PDF 수 집계 → 전체 BODY·UI 복원
- **취소**: 공용 처리 오버레이의 취소 요청을 STRU 시작·종료 경계에서 확인하고, 부분 2D·3D·시트·치수 상태를 정리한 뒤 완료/전체·PDF 수를 표시
- **흐름 문서**: [기능/도면시트/도면 일괄 출력.md](../기능/도면시트/도면%20일괄%20출력.md)

## 내부 핵심 메서드

### <a id="ProcessSingleStruFull"></a>ProcessSingleStruFull

- **라인**: L775~L1048
- **시그니처**: `int ProcessSingleStruFull(Node struNode, string saveDir, Action<int> reportPdfSaved = null)`
- STRU 후손 BODY 격리 → BOM 재수집 → 비동기 간섭검사·자동 시트 생성 → 일반 시트 2D/PDF → 가공도 묶음 PDF 순으로 처리한다.
- `ThrowIfCancellationRequested`를 BOM·간섭검사·시트 선택·2D 생성·PDF 저장·가공도 경계에서 호출한다.
- 간섭검사가 이미 실행 중이면 `Clash.IsBusy` 해제와 완료 이벤트 처리를 기다린 뒤 `OperationCanceledException`으로 상위 반복을 종료한다.
- PDF 저장 직후 `reportPdfSaved`를 호출하므로 STRU가 중간 취소되어도 실제 저장된 PDF 수가 상위 요약에 남는다.

## 공유 취소 상태

공용 필드는 `Form1.cs`에 있으며 치수 추출과 일괄 출력이 함께 사용한다.

| 필드/메서드 | 역할 |
|---|---|
| `_cancelableOperationInProgress` | 취소 버튼 표시 여부와 작업 소유 상태 |
| `_cancelRequested` | 사용자가 누른 취소 요청 |
| `BeginCancelableOperation` / `EndCancelableOperation` | 요청 상태 초기화·해제 |
| `IsCancellationRequested` | 체크포인트 진단 로그와 요청 조회 |
| `ThrowIfCancellationRequested` | 동기 일괄 출력 흐름을 `OperationCanceledException`으로 탈출 |

## 관련 문서

- [STRU 도면 일괄 출력](../기능/도면시트/도면%20일괄%20출력.md)
- [간섭검사 완료 이벤트](../기능/간섭검사/간섭검사%20완료%20이벤트.md)
- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
