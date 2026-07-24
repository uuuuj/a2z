# Form1.Drawing2D.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Drawing2D.cs` (약 974 라인)

**책임**: 전체 BOM 2D 생성 위임, PDF 내보내기, BOM/Clash 리스트 더블클릭 네비게이션, Osnap 수집/픽킹/추가/삭제/표시/클리어.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="btnGenerate2D_Click"></a>`btnGenerate2D_Click` | L37 | [generate-2d](../기능/2D도면/2D%20생성.md) |
| <a id="btnExportPDF_Click"></a>`btnExportPDF_Click` | L82 | [export-pdf](../기능/2D도면/PDF%20출력.md) |
| <a id="LvBOM_DoubleClick"></a>`LvBOM_DoubleClick` | L123 | [lvbom-doubleclick](../기능/2D도면/BOM%20목록%20더블클릭.md) |
| <a id="LvClash_DoubleClick"></a>`LvClash_DoubleClick` | L151 | [lvclash-doubleclick](../기능/2D도면/간섭%20목록%20더블클릭.md) |
| <a id="btnCollectOsnap_Click"></a>`btnCollectOsnap_Click` | L179 | [collect-osnap](../기능/2D도면/Osnap%20수집.md) |
| <a id="btnClashShowSelected_Click"></a>`btnClashShowSelected_Click` | L354 | [clash-show-selected](../기능/2D도면/간섭%20선택%20보기.md) |
| <a id="btnClashShowAll_Click"></a>`btnClashShowAll_Click` | L651 | [clash-show-all](../기능/2D도면/간섭%20전체%20보기.md) |
| <a id="btnOsnapAdd_Click"></a>`btnOsnapAdd_Click` | L694 | [osnap-add](../기능/2D도면/Osnap%20추가.md) |
| <a id="GeometryUtility_OnOsnapPickingItem"></a>`GeometryUtility_OnOsnapPickingItem` | L716 | [osnap-picking-event](../기능/2D도면/Osnap%20피킹%20이벤트.md) |
| <a id="btnOsnapDelete_Click"></a>`btnOsnapDelete_Click` | L758 | [osnap-delete](../기능/2D도면/Osnap%20삭제.md) |
| <a id="btnOsnapShowSelected_Click"></a>`btnOsnapShowSelected_Click` | L807 | [osnap-show-selected](../기능/2D도면/Osnap%20선택%20보기.md) |
| <a id="btnOsnapClearBalloon_Click"></a>`btnOsnapClearBalloon_Click` | L913 | [osnap-clear-balloon](../기능/2D도면/Osnap%20풍선%20초기화.md) |

---

## 내부 헬퍼

| 메서드 | 라인 | 역할 |
|---|---|---|
| `GetSolutionPath` | ~L20 | 솔루션 경로 탐색 (로고 이미지용) |
| `CollectOsnapForSelectedNodes` | L472 | 선택된 노드만 대상 Osnap 수집 (자동 호출용) |
| `ExtractDimensionForSelectedNodes` | (헬퍼) | Osnap 기반 체인 치수 재추출 |
| `Clear2DView` | L926 | 2D 뷰 완전 초기화 (Model3D ↔ Both 토글 + 2회 삭제 + GC) |

---

## OsnapKind 처리 매트릭스

| Kind | 처리 |
|---|---|
| LINE | Start, End 두 좌표 추가 |
| POINT | Center 좌표 추가 |
| CIRCLE | 스킵 (곡면) |
| SURFACE | 스킵 (표면) |

---

## 풍선 NoteStyle 기본값 (btnOsnapShowSelected_Click)

| 속성 | 값 |
|---|---|
| `UseSymbol` | false |
| `BackgroudTransparent` | true |
| `FontBold` | true |
| `FontSize` | SIZE10 |
| `FontColor` / `LineColor` | DarkBlue |
| `LineWidth` | 1 |
| `ArrowColor` | Red |
| `ArrowWidth` | 3 |

---

## VIZCore3D API 사용

- `vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(path)`
- `vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView()`, `UnselectCurrentWorkObjectBy2DView()`
- `vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView()`, `DeleteAllNonObjectBy2DView()`
- `vizcore3d.Drawing2D.View.RemoveCanvasBy2DView()`
- `vizcore3d.Drawing2D.Render()`
- `vizcore3d.Object3D.Select(indices, exclusive, zoom)`, `Color.RestoreColorAll()`
- `vizcore3d.View.FlyToObject3d(indices, zoomFactor)`
- `vizcore3d.View.XRay.Enable / Select / Clear / ColorType / SelectionObject3DType`
- `vizcore3d.Clash.ShowResultSymbol(points, symbols, size, selectable, color, bypassFilter)`
- `vizcore3d.GeometryUtility.ShowOsnap(vertex, line, circle, point)`
- `vizcore3d.ShapeDrawing.AddSphere(points, index, color, radius, selectable)`
- `vizcore3d.Review.Note.AddNoteSurface(text, textPos, arrowPos, style)`

---

## 관련 문서
- 흐름 문서: [기능/2D도면/](../기능/2D도면/_인덱스.md)
