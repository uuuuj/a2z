# Form1.MfgDrawing.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.MfgDrawing.cs` (약 2,185 라인)

**책임**: 단일 부재 가공도 생성 (선택 부재 격리 + 최장축/ORIENTATION 회전 + Osnap 치수 + Hidden Line + 풍선), 전체 가공도 시트 배치 2D 출력.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="btnMfgDrawing_Click"></a>`btnMfgDrawing_Click` | L19 | [mfg-drawing](../features/mfg-drawing/mfg-drawing.md) |
| <a id="btnMfgDrawingSheet_Click"></a>`btnMfgDrawingSheet_Click` | L548 | [mfg-drawing-sheet](../features/mfg-drawing/mfg-drawing-sheet.md) |

---

## 핵심 내부 메서드

### <a id="ExecuteMfgDrawing"></a>ExecuteMfgDrawing(int bomIndex)
- **라인**: L52~L547
- **단계**:
  1. Measure/ShapeDrawing/Note Clear
  2. X-Ray 끄기
  3. 전체 Hide → 선택 부재 Show
  4. BBox → 최장축·최단축 결정
  5. SPREF UDA로 PAD/PLATE 판별 (L82)
  6. 뷰 방향 설정 (최장축=좌우, 두께=Z+)
  7. ORIENTATION UDA 있으면 `ApplyOrientationRotation` (L2158) 호출
  8. `GetOsnapPoint` → 치수 자동 생성
  9. Hidden Line + 풍선 + 보조선

### <a id="RestoreAllPartsVisibility"></a>RestoreAllPartsVisibility
- **라인**: L37~L46
- **역할**: 모든 BOM 부재 Show(true) — BOM 더블클릭·축 버튼·전체보기 등에서 호출

### <a id="GenerateMfgDrawing2DAll"></a>GenerateMfgDrawing2DAll(List&lt;DrawingSheetData&gt; mfgSheets)
- **라인**: L574+
- **배치 사양**:
  - A4 297x210 (가로)
  - 외곽 `GridStructure 1x1` + Margins 10
  - 모델 배치 `GridStructure 8x6` (2~7행, 라벨+모델 3그룹 = 18슬롯)
  - 도면정보 테이블 5x4, Anchor Right/Bottom (bInfo.MaxX, MinY)
  - ModelLineThickness=2.0f, MeasureLineWidth=0.3f, MeasureTextHeight=5f
  - 메모리 정리: 시트 간 2D Delete + Canvas Remove + GC.Collect x2

### <a id="ApplyOrientationRotation"></a>ApplyOrientationRotation
- **라인**: L2158
- **역할**: UDA ORIENTATION 키 파싱 → 축별 각도 카메라 회전

---

## 도면정보 테이블 기본 내용

| 필드 | 값 |
|---|---|
| 작성 일자 | `DateTime.Now` |
| 소속 | 삼성중공업 |
| 담당자 | 홍길동 |
| 검수자 | 홍길동 |
| Image | `{GetSolutionPath()}\Logo.png` (ImageHeight=50) |

---

## VIZCore3D API 사용

- `vizcore3d.Drawing2D.Template.CrateTemplateBorder()` → `TemplateBorderInfo`
- `vizcore3d.Drawing2D.Template.RenderTemplate(TemplateTableData)`
- `vizcore3d.Drawing2D.GridStructure.AddGridStructure(rows, cols, w, h)`
- `vizcore3d.Drawing2D.Object2D.ModelLineThickness`
- `vizcore3d.View.EnableAnimation`
- `vizcore3d.Object3D.Show(indices, visible)`
- `vizcore3d.Object3D.GetOsnapPoint(idx)`
- `vizcore3d.Review.Measure.*`

---

## 관련 문서
- 흐름 문서: [features/mfg-drawing/](../features/mfg-drawing/_index.md)
- 용어집: [PAD / PLATE](../_glossary.md#pad--plate)
