# Drawing2D 기능

`Form1.Drawing2D.cs` 소속. 2D 도면 생성, PDF 출력, Osnap 관리, 목록 상호작용 기능입니다.

## 기능 목록
| ID        | 기능                    | 트리거                                | 유형             | 문서                                              |
| --------- | --------------------- | ---------------------------------- | -------------- | ----------------------------------------------- |
| DRW2D-001 | 전체 BOM 2D 도면 생성       | btnGenerate2D_Click                | User Action    | [generate-2d](./generate-2d.md)                 |
| DRW2D-002 | 2D 도면 PDF 내보내기        | btnExportPDF_Click                 | User Action    | [export-pdf](./export-pdf.md)                   |
| DRW2D-003 | BOM 더블클릭 → 부재 포커스     | LvBOM_DoubleClick                  | User Action    | [lvbom-doubleclick](./lvbom-doubleclick.md)     |
| DRW2D-004 | Clash 더블클릭 → 간섭 부재 표시 | LvClash_DoubleClick                | User Action    | [lvclash-doubleclick](./lvclash-doubleclick.md) |
| DRW2D-005 | 모든 Osnap 수집           | btnCollectOsnap_Click              | User Action    | [collect-osnap](./collect-osnap.md)             |
| DRW2D-006 | 선택 Clash 부재 강조        | btnClashShowSelected_Click         | User Action    | [clash-show-selected](./clash-show-selected.md) |
| DRW2D-007 | 모든 Clash 부재 강조        | btnClashShowAll_Click              | User Action    | [clash-show-all](./clash-show-all.md)           |
| DRW2D-008 | Osnap 수동 추가 (픽킹)      | btnOsnapAdd_Click                  | User Action    | [osnap-add](./osnap-add.md)                     |
| DRW2D-009 | Osnap 픽킹 이벤트          | GeometryUtility_OnOsnapPickingItem | Event Callback | [osnap-picking-event](./osnap-picking-event.md) |
| DRW2D-010 | Osnap 삭제              | btnOsnapDelete_Click               | User Action    | [osnap-delete](./osnap-delete.md)               |
| DRW2D-011 | 선택 Osnap 풍선 표시        | btnOsnapShowSelected_Click         | User Action    | [osnap-show-selected](./osnap-show-selected.md) |
| DRW2D-012 | Osnap 풍선 전체 삭제        | btnOsnapClearBalloon_Click         | User Action    | [osnap-clear-balloon](./osnap-clear-balloon.md) |
