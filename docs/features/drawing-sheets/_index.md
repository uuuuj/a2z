# DrawingSheets 기능

`Form1.DrawingSheets.cs` 소속. Clash 그래프 기반 BFS로 도면 시트를 자동 분할하고 시트별 2D·PDF를 생성합니다.

## 기능 목록
| ID | 기능 | 트리거 | 유형 | 문서 |
|---|---|---|---|---|
| SHT-001 | 시트 자동 분할 (BFS) | btnGenerateSheets_Click | User Action | [generate-sheets](./generate-sheets.md) |
| SHT-002 | 시트 선택 시 뷰 포커스 | LvDrawingSheet_SelectedIndexChanged | Event Callback | [lv-sheet-selected](./lv-sheet-selected.md) |
| SHT-003 | 시트 ISO 뷰 | btnDrawingISO_Click | User Action | [drawing-iso](./drawing-iso.md) |
| SHT-004 | 시트 X축 뷰 | btnDrawingAxisX_Click | User Action | [drawing-axis-x](./drawing-axis-x.md) |
| SHT-005 | 시트 Y축 뷰 | btnDrawingAxisY_Click | User Action | [drawing-axis-y](./drawing-axis-y.md) |
| SHT-006 | 시트 Z축 뷰 | btnDrawingAxisZ_Click | User Action | [drawing-axis-z](./drawing-axis-z.md) |
| SHT-007 | 선택 시트 2D 생성 | btnGenerateSheet2D_Click | User Action | [generate-sheet-2d](./generate-sheet-2d.md) |
| SHT-008 | 선택 시트 PDF 내보내기 | btnExportSheet2DPDF_Click | User Action | [export-sheet-2d-pdf](./export-sheet-2d-pdf.md) |
| SHT-009 | 전체 시트 PDF 배치 내보내기 | btnExportAllPDF_Click | User Action | [export-all-pdf](./export-all-pdf.md) |
| SHT-010 | BOM 정보 행 선택 시 부재 카메라 fit | LvDrawingBOMInfo_SelectedIndexChanged | Event Callback | [lv-bom-info-selected](./lv-bom-info-selected.md) |

## 주요 생성 상태
- `drawingSheets : List<DrawingSheetData>`
