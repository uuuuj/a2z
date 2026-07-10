# Form1.DrawingSheets.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.DrawingSheets.cs` (약 2,597 라인)

**책임**: Clash 그래프 기반 BFS 시트 분할, 시트 선택 시 X-Ray + 치수 표시, 시트별 2D 생성, PDF 내보내기 (단일/배치), ISO 풍선 노트 생성.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="btnGenerateSheets_Click"></a>`btnGenerateSheets_Click` | L398 | [generate-sheets](../기능/도면시트/시트 자동 생성.md) |
| <a id="LvDrawingSheet_SelectedIndexChanged"></a>`LvDrawingSheet_SelectedIndexChanged` | L425 | [lv-sheet-selected](../기능/도면시트/시트 선택.md) |
| <a id="btnDrawingISO_Click"></a>`btnDrawingISO_Click` | L755 | [drawing-iso](../기능/도면시트/ISO 도면.md) |
| <a id="btnDrawingAxisX_Click"></a>`btnDrawingAxisX_Click` | L760 | [drawing-axis-x](../기능/도면시트/X축 도면.md) |
| <a id="btnDrawingAxisY_Click"></a>`btnDrawingAxisY_Click` | L765 | [drawing-axis-y](../기능/도면시트/Y축 도면.md) |
| <a id="btnDrawingAxisZ_Click"></a>`btnDrawingAxisZ_Click` | L770 | [drawing-axis-z](../기능/도면시트/Z축 도면.md) |
| <a id="btnGenerateSheet2D_Click"></a>`btnGenerateSheet2D_Click` | L778 | [generate-sheet-2d](../기능/도면시트/시트 2D 렌더.md) |
| <a id="btnExportSheet2DPDF_Click"></a>`btnExportSheet2DPDF_Click` | L806 | [export-sheet-2d-pdf](../기능/도면시트/시트 PDF 출력.md) |
| <a id="btnExportAllPDF_Click"></a>`btnExportAllPDF_Click` | L847 | [export-all-pdf](../기능/도면시트/전체 PDF 출력.md) |

---

## 핵심 내부 메서드

### <a id="GenerateDrawingSheets"></a>GenerateDrawingSheets
- **라인**: L18~L396
- **알고리즘**:
  1. **Sheet 1**: 전체 BOM 부재 묶음
  2. **Sheet 2~N**: 각 BOM 부재 시작점 BFS, Clash 인접 리스트 확장
  3. **마지막 Sheet**: 전체 설치도 (전역 BFS)
  - `appearedAsIncluded` HashSet으로 중복 방지

### <a id="ApplyDrawingSheetView"></a>ApplyDrawingSheetView(string viewDirection)
- **라인**: L499~L590
- **ISO 경로**: X-Ray 활성 → `ExtractInstallationDimensions` → ISO 카메라 → `CreateIsoBalloonNotes`
- **X/Y/Z 경로**: X-Ray 유지 → 측정/노트 Clear → 해당 축 카메라 → `ShowAllDimensions(axis)`

### <a id="CreateIsoBalloonNotes"></a>CreateIsoBalloonNotes(memberIndices, forDrawing2D=false)
- **라인**: L845+
- **핵심**: ISO_PLUS 등각 투영 2D 근사 (0.707f, 0.408f, 0.816f) → 3D 방향 계산 → 2D AABB 겹침 검사 → 36회 회전 시도 → `Review.Note.AddNoteSurface`
- **번호 매핑**: `bomNameToTableNo`로 `lvDrawingBOMInfo`의 # 번호와 동기화

### <a id="GenerateSheetDrawing2D"></a>GenerateSheetDrawing2D(sheet)
- **라인**: L1263+
- **단계**: Hidden Line → 풍선/충돌회피 → 보조선 → BOM 테이블 → 도면정보 테이블

### <a id="GenerateSheetDrawing2D_WithExcelTemplate"></a>GenerateSheetDrawing2D_WithExcelTemplate(sheet)
- **라인**: L1613+
- **단계**: `Set2DViewTemplateMark(Logo.png)` 로고 등록 → 엑셀(`제작도_도면_1.xlsx`) `{Input_N}` 치환 (BOM 8열×20행, 열별 20연속 슬롯 4~163 + 비-BOM 164~199) → `{View_1~7}` 영역 파싱 → `View_5`·`View_7` 고정 이미지 배치 (`View_6` 예약) → 모델 4면도·치수·풍선 렌더

### <a id="ResolveDrawingAssetPath"></a>ResolveDrawingAssetPath(fileName)
- **라인**: L2016+
- **역할**: 실행 폴더의 이미지 파일을 우선 사용하고, 개발 환경에서는 솔루션 루트 파일로 fallback

### <a id="PlaceImageInTemplateArea"></a>PlaceImageInTemplateArea(imagePath, area, margin=1)
- **라인**: L2032+
- **역할**: `TemplateTableData` 이미지 셀을 영역 크기에 종횡비 유지 fit 후 중앙 좌표에 직접 렌더링. 실패 시 절대경로 로그 후 도면 출력 계속

### <a id="SanitizeFileName"></a>SanitizeFileName
- **라인**: L1245
- **역할**: `Path.GetInvalidFileNameChars()`의 문자를 `_`로 치환

---

## `DrawingSheetData.BaseMemberIndex` 특수값

| 값 | 의미 |
|---|---|
| `-1` | Sheet 1 (전체 BOM) 또는 임시 시트 |
| `-3` | 가공도 시트 |
| 그 외 양수 | 기준 부재의 BOM Index |

---

## VIZCore3D API 사용

- `vizcore3d.Drawing2D.View.SetCanvasSize(w, h)`, `GetCanvasSize(ref w, ref h)`
- `vizcore3d.Drawing2D.View.SetSelectCanvas(idx)`, `RemoveCanvasBy2DView()`
- `vizcore3d.Drawing2D.GridStructure.AddGridStructure(rows, cols, w, h)`, `SetMargins`
- `vizcore3d.Drawing2D.Template.CrateTemplateBorder()`, `RenderTemplate(tableData)`
- `vizcore3d.Drawing2D.Object2D.ModelLineThickness`, `Set2DViewCreateObjectItemMeasure*`
- `vizcore3d.View.XRay.*`, `View.SetRenderMode(DASH_LINE)`, `View.MoveCamera(CameraDirection)`
- `vizcore3d.Review.Note.AddNoteSurface(text, textPos, arrowPos)`

---

## 관련 문서
- 흐름 문서: [기능/도면시트/](../기능/도면시트/_인덱스.md)
