# Form1.Drawing2D.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Drawing2D.cs` (약 1190 라인)

**책임**: 전체 BOM 2D 생성 위임, PDF 내보내기, 다중 페이지 PDF 누적 장치(#119), BOM/Clash 리스트 더블클릭 네비게이션, Osnap 수집/픽킹/추가/삭제/표시/클리어.

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
| `GetSolutionPath` | L16 | `.sln`을 위로 탐색해 레포 루트 반환. **개발 편의용 폴백 전용** — 도면 리소스 경로는 `ResolveDrawingResourcePath`(실행 폴더 우선)를 거치며, 이 함수는 실행 폴더에서 못 찾았을 때만 호출된다 |
| `CollectOsnapForSelectedNodes` | L472 | 선택된 노드만 대상 Osnap 수집 (자동 호출용) |
| `ExtractDimensionForSelectedNodes` | (헬퍼) | Osnap 기반 체인 치수 재추출 |
| `Clear2DView` | L1164 | 2D 뷰 완전 초기화 — ViewMode를 `Both`로 보장한 뒤 2D 객체·비객체·캔버스 전체 삭제 + `Render`. 2026-07-22 `5a0df44`에서 `Model3D ↔ Both` 왕복 토글과 `Sleep(150)`을 제거(출력 시 깜빡임·600ms 단축) |

---

## 다중 페이지 PDF 누적 (#119)

도면 한 장을 그릴 때마다 캔버스를 비우면 PDF도 한 장씩 따로 떨어진다. 누적 중에는 장마다 캔버스를 덧붙여 쌓아두고 마지막에 한 번만 저장한다. 저장 API에 캔버스 번호를 주지 않으면 쌓인 캔버스 전체가 한 PDF의 페이지가 된다 (`Export2PDFBy2DView(path)` vs `Export2PDFBy2DView(path, canvasIdx)` — 후자가 "지정한 캔버스만"으로 명시됨).

| 메서드 | 라인 | 역할 |
|---|---|---|
| `BeginPdfPageAccumulation` | L990 | 누적 시작. 이전 도면 잔재는 여기서 한 번만 제거. **이미 누적 중이면 중첩하지 않고 `false`** — 도면 일괄 출력이 가공도 묶음을 품는 구조라 바깥이 끝까지 주인 |
| `PrepareDrawingCanvas` | L1011 | 도면 한 장을 그리기 직전 캔버스 준비. 누적이 아니면 종전대로 `Clear2DView` + 1번 캔버스, 누적이면 `AddCanvasBy2DView`로 덧붙임 |
| `DiscardCurrentPdfPage` | L1036 | 방금 그린 페이지를 버림 (도면 생성 실패 시). 반쪽짜리 캔버스가 PDF 페이지로 남지 않게 함 |
| `CleanupBetweenPdfPages` | L1055 | 페이지 사이 정리. 누적 중에는 쌓아둔 캔버스를 지우면 안 되므로 GC만 돌림 |
| `EndPdfPageAccumulation` | L1073 | 쌓인 페이지를 PDF 1개로 저장하고 누적 종료. 페이지 0건이면 저장 생략 |
| `FlushPendingMergedPdf` | L1110 | 예약해둔 경로로 저장. 취소가 STRU 처리 중간에서 예외로 튀는 도면 일괄 출력용 — 호출부가 예외를 받은 직후 호출 |
| `BuildMergedDrawingPdfPath` | L1133 | `{폴더}\{종류}_{모델명}_{시각}.pdf`. 이름 충돌 시 `_1`, `_2` … / 경로 240자 초과 시 MAX_PATH 경고 로그 |

관련 필드: `_pdfPageAccumulating`(누적 중 여부), `_pdfPageCount`(쌓인 페이지 수), `_activeDrawingCanvasIdx`(현재 그리는 캔버스 번호 — 누적 시 1번이 아닐 수 있어 `SetSelectCanvas(1)` 하드코딩 자리를 이 값으로 교체), `_pendingMergedPdfPath`(일괄 출력이 예약한 저장 경로).

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
