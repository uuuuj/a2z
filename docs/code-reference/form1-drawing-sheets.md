# Form1.DrawingSheets.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.DrawingSheets.cs` (약 2,580 라인)

**책임**: Clash 그래프 기반 BFS 시트 분할, 시트 선택 시 X-Ray + 치수 표시, 시트별 2D 생성, 단일 PDF 내보내기, ISO 풍선 노트 생성.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="LvDrawingSheet_SelectedIndexChanged"></a>`LvDrawingSheet_SelectedIndexChanged` | L570 | [lv-sheet-selected](../기능/도면시트/시트 선택.md) |
| <a id="btnDrawingISO_Click"></a>`btnDrawingISO_Click` | L1084 | [drawing-iso](../기능/도면시트/ISO 도면.md) |
| <a id="btnDrawingAxisX_Click"></a>`btnDrawingAxisX_Click` | L1089 | [drawing-axis-x](../기능/도면시트/X축 도면.md) |
| <a id="btnDrawingAxisY_Click"></a>`btnDrawingAxisY_Click` | L1094 | [drawing-axis-y](../기능/도면시트/Y축 도면.md) |
| <a id="btnDrawingAxisZ_Click"></a>`btnDrawingAxisZ_Click` | L1099 | [drawing-axis-z](../기능/도면시트/Z축 도면.md) |
| <a id="btnGenerateSheet2D_Click"></a>`btnGenerateSheet2D_Click` | L1107 | [generate-sheet-2d](../기능/도면시트/시트 2D 렌더.md) |
| <a id="btnExportSheet2DPDF_Click"></a>`btnExportSheet2DPDF_Click` | L1135 | [export-sheet-2d-pdf](../기능/도면시트/시트 PDF 출력.md) |

---

## 핵심 내부 메서드

### <a id="GenerateDrawingSheets"></a>GenerateDrawingSheets
- **라인**: L18~L481
- **알고리즘**:
  1. **Sheet 1**: 전체 BOM 부재 묶음
  2. **Sheet 2~N**: 각 BOM 부재 시작점 BFS, Clash 인접 리스트 확장
  3. **마지막 Sheet**: 전체 설치도 (전역 BFS)
  4. 중복 구성 일반 시트 제거·재채번
  5. 일반·설치 치수와 모든 시트 BOM을 사전 준비한 뒤 ListView 표시

### PrepareDrawingSheetDimensionCaches / ApplyPreparedDimensionsToUi
- **라인**: L483~L567
- **핵심**: Sheet 1 기존 치수 결과 재사용 → 일반 시트 Osnap 치수 선계산 → 설치도 BBox 치수 선계산. 선택 시에는 준비 목록을 `chainDimensionList`·`lvDimension`에 복사

### ApplySheetSelection
- **라인**: L588~L769
- **핵심**: 애니메이션 없는 모델 전환 → 일반·설치 시트 준비 치수 적용 → 준비 BOM 적용. 로그에 장면·치수·BOM·전체 시간을 분리 기록

### <a id="ApplyDrawingSheetView"></a>ApplyDrawingSheetView(string viewDirection)
- **라인**: L810~L908
- **ISO 경로**: X-Ray 활성 → `ExtractInstallationDimensions` → ISO 카메라 → `CreateIsoBalloonNotes`
- **X/Y/Z 경로**: X-Ray 유지 → 측정/노트 Clear → 해당 축 카메라 → `ShowAllDimensions(axis)`

### <a id="CreateIsoBalloonNotes"></a>CreateIsoBalloonNotes(memberIndices, forDrawing2D=false)
- **라인**: L910+
- **핵심**: ISO_PLUS 등각 투영 2D 근사 (0.707f, 0.408f, 0.816f) → 3D 방향 계산 → 2D AABB 겹침 검사 → 36회 회전 시도 → `Review.Note.AddNoteSurface`
- **번호 매핑**: `bomNameToTableNo`로 `lvDrawingBOMInfo`의 # 번호와 동기화

### <a id="GenerateSheetDrawing2D"></a>GenerateSheetDrawing2D(sheet)
- **라인**: L1198+
- **단계**: Hidden Line → 풍선/충돌회피 → 보조선 → BOM 테이블 → 도면정보 테이블

### <a id="GenerateSheetDrawing2D_WithExcelTemplate"></a>GenerateSheetDrawing2D_WithExcelTemplate(sheet)
- **라인**: L1548+
- **단계**: 엑셀(`제작도_도면_1.xlsx`) `{Input_N}` 치환 + `{Image_1~3}` 이미지 매핑(N·ISO 화살표, CONTRACTOR 로고 — 2026-07-20/21 Image_N 전환, 옛 `Set2DViewTemplateMark` 로고 등록·View 수동 배치 폐기) → 빈 칸 괘선 제거(`RemoveEmptyTemplateBorders`, 2026-07-21 — 슬롯 초기값으로 선별: BOM·Note·Rev 위 4행만 제거, PAINT/DP/TAG·Rev 첫 기재행은 공백 위장 보존) → `{View_1~4}` 영역 파싱(`View_6` CLIENT 예약) → 모델 4면도·치수·풍선 렌더. ISO는 두 겹 — 조립도: 전체−기준 LONG_DASHED 점선+기준부재 실선 / 제작도: `시트 부재+연결 부재` 점선 배경 캡처 → 시트 부재 노드 기준 CropFit → LONG_DASHED → 점선 fit → 시트 부재 실선 캡처 → `Match2DObjectsTo3DObjectPosition(실선, 점선)` 정합

### <a id="ResolveDrawingAssetPath"></a>ResolveDrawingAssetPath(fileName)
- **라인**: L2103+
- **역할**: 실행 폴더의 이미지 파일을 우선 사용하고, 개발 환경에서는 솔루션 루트 파일로 fallback

### <a id="PlaceImageInTemplateArea"></a>PlaceImageInTemplateArea(imagePath, area, margin=1)
- **라인**: L2119+
- **역할**: `TemplateTableData` 이미지 셀을 영역 크기에 종횡비 유지 fit 후 중앙 좌표에 직접 렌더링. 실패 시 절대경로 로그 후 도면 출력 계속

### <a id="SanitizeFileName"></a>SanitizeFileName
- **라인**: L1176
- **역할**: `Path.GetInvalidFileNameChars()`의 문자를 `_`로 치환

---

## `DrawingSheetData.BaseMemberIndex` 특수값

| 값 | 의미 |
|---|---|
| `-1` | Sheet 1 (전체 BOM) 또는 임시 시트 |
| `-2` | 설치도 시트 |
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
