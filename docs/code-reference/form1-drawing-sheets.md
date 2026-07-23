# Form1.DrawingSheets.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.DrawingSheets.cs` (약 3,218 라인)

**책임**: Clash 그래프 기반 BFS 시트 분할, 시트 선택 시 X-Ray + 치수 표시, 시트별 2D 생성, 단일 PDF 내보내기, ISO 풍선 노트 생성.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="LvDrawingSheet_SelectedIndexChanged"></a>`LvDrawingSheet_SelectedIndexChanged` | L608 | [lv-sheet-selected](../기능/도면시트/시트%20선택.md) |
| <a id="btnDrawingISO_Click"></a>`btnDrawingISO_Click` | L1113 | [drawing-iso](../기능/도면시트/ISO%20도면.md) |
| <a id="btnDrawingAxisX_Click"></a>`btnDrawingAxisX_Click` | L1118 | [drawing-axis-x](../기능/도면시트/X축%20도면.md) |
| <a id="btnDrawingAxisY_Click"></a>`btnDrawingAxisY_Click` | L1123 | [drawing-axis-y](../기능/도면시트/Y축%20도면.md) |
| <a id="btnDrawingAxisZ_Click"></a>`btnDrawingAxisZ_Click` | L1128 | [drawing-axis-z](../기능/도면시트/Z축%20도면.md) |
| <a id="btnGenerateSheet2D_Click"></a>`btnGenerateSheet2D_Click` | L1136 | [generate-sheet-2d](../기능/도면시트/시트%202D%20렌더.md) |
| <a id="btnExportSheet2DPDF_Click"></a>`btnExportSheet2DPDF_Click` | L1196 | [export-sheet-2d-pdf](../기능/도면시트/시트%20PDF%20출력.md) |

---

## 핵심 내부 메서드

### <a id="GenerateDrawingSheets"></a>GenerateDrawingSheets
- **라인**: L18~L460
- **알고리즘**:
  1. **Sheet 1**: 전체 BOM 부재 묶음
  2. **Sheet 2~N**: 각 BOM 부재 시작점 BFS, Clash 인접 리스트 확장
  3. **설치도**: 선택 STRU 전체 + 직접 연결 외부 Part와 실제 BODY 접합영역 준비. 부모 Assembly는 이름 문맥으로만 유지
  4. 중복 구성 일반 시트 제거·재채번
  5. 일반·설치 치수와 모든 시트 BOM을 사전 준비한 뒤 ListView 표시

### <a id="CreateFullDrawingSheetData"></a>CreateFullDrawingSheetData
- **라인**: L462~L492
- **핵심**: Sheet 1과 미선택 임시 출력이 같은 규칙을 사용하도록 `SheetNumber = 1`, `BaseMemberIndex = -1`, BOM 전체 부재와 기준 이름을 구성

### PrepareDrawingSheetDimensionCaches / ApplyPreparedDimensionsToUi / GetDrawingSheetDimensionsFor2D
- **라인**: L494~L605
- **핵심**: Sheet 1 기존 치수 결과 재사용 → 일반 시트 Osnap 치수 선계산 → 설치도 Target Body 끝단→Connected Body 모서리 치수만 선계산. 선택·3D 미리보기·옛 2D·엑셀 2D 모두 같은 설치도 `PreparedDimensions`를 사용해 공용 Osnap 계산이 덮어쓰지 않음

### ApplySheetSelection
- **라인**: L626~L784
- **핵심**: 애니메이션 없는 모델 전환 → 일반 시트는 MemberIndices, 설치도는 STRU+직접 연결 외부 Part 표시 → 준비 치수·BOM 적용. 로그에 장면·치수·BOM·전체 시간을 분리 기록

### <a id="ApplyDrawingSheetView"></a>ApplyDrawingSheetView(string viewDirection)
- **라인**: L821~L936
- **공통 진입**: `Clear3DDimensionAnnotations()`로 직전 3D 측정선·보조선을 제거
- **ISO 경로**: 설치도 표시 대상(STRU+직접 연결 외부 Part) 활성 → 설치 준비 치수 데이터 적용 → ISO 카메라 → 선택 STRU 풍선만 표시
- **X/Y/Z 경로**: X-Ray 유지 → 노트 Clear → 해당 축 카메라 → 2D 도면과 같은 `chainDimensionList`를 `ShowAllDimensions(axis)`로 표시. 설치도는 선택 STRU BBox·fit을 기준으로 연결 위치 치수만 표시

### <a id="CreateIsoBalloonNotes"></a>CreateIsoBalloonNotes(memberIndices, forDrawing2D=false)
- **라인**: L939+
- **핵심**: ISO_PLUS 등각 투영 2D 근사 (0.707f, 0.408f, 0.816f) → 3D 방향 계산 → 2D AABB 겹침 검사 → 36회 회전 시도 → `Review.Note.AddNoteSurface`
- **번호 매핑**: `bomNameToTableNo`로 `lvDrawingBOMInfo`의 # 번호와 동기화

### <a id="GenerateSheetDrawing2D"></a>GenerateSheetDrawing2D(sheet)
- **라인**: L1272+
- **단계**: Hidden Line → 풍선/충돌회피 → 보조선 → BOM 테이블 → 도면정보 테이블 → 공통 `finally`에서 3D `Review.Measure`·`ShapeDrawing` 제거

### <a id="GenerateSheetDrawing2D_WithExcelTemplate"></a>GenerateSheetDrawing2D_WithExcelTemplate(sheet)
- **라인**: L1633+
- **단계**: 엑셀(`제작도_도면_1.xlsx`) 데이터·이미지 치환 → 같은 도면 목록의 제작도·조립도·설치도·가공도가 공유하는 PAINT CODE를 `Input_166`에 적용 → 빈 칸 괘선 제거 → `{View_1~4}` 영역 파싱 → 모델 4면도·치수·풍선 렌더. ISO는 조립도/제작도 두 겹 표현을 유지하고, 실선 투영 객체를 선택해 부재번호 풍선을 2D 변환한 뒤 SDK 1.0.26.723의 영역 정렬 API로 실제 모델 fit 70% 범위 바깥·View 안쪽에 배치한다. 연결 Assembly/Part 이름은 정렬 후 별도 생성한다. 설치도는 ISO/Z/X/Y 모두 선택 STRU 실선 + 직접 연결 외부 Part LONG_DASHED 점선으로 캡처하고 선택 STRU 기준 CropFit·축척·Match를 적용한다. 직교 뷰는 A/A1·전체 범위 없이 Target Body 끝단→Connected Body 모서리 치수만 투영하며, 모델 배치·Match 후 `GetObjectScale` 실측값으로 `ShowAllDimensions`를 호출해 보조선 종이 길이를 통일한다.

### GetDrawingSheetDisplayIndices
- **라인**: L2380+
- **역할**: 일반 시트는 `MemberIndices`, 설치도는 `MemberIndices + InstallationContextIndices`를 반환한다. 설치도 3D 미리보기 가시성에는 양쪽을 쓰지만, 2D fit·Crop·보조선 축척은 선택 STRU `MemberIndices`만 기준으로 사용

### GetFabricationNeighborAssemblyNotes / FindNearestParentAssembly
- **라인**: L2428~L2511
- **역할**: 제작도 연결 Clash의 상대 Part를 가장 가까운 부모 Assembly 단위로 묶어 중복 제거하고, 이름과 대표 HotPoint XYZ를 월드 좌표 노트 입력으로 반환

### GetStruPntUdaValue
- **라인**: L2614~L2691
- **역할**: 기준부재에서 부모로 최대 10단계 이동하며 이름에 `PNT`가 포함된 UDA 키를 조회. 한 노드의 복수 후보 키·값을 모두 로그에 남기고 첫 비어 있지 않은 값을 PAINT CODE로 반환

### GetOrCacheDrawingPaintCode
- **라인**: L2692~L2725
- **역할**: `UDA.Keys`를 `BeginUpdate` 밖인 출력 데이터 구성 시점에만 호출하고, 첫 조회 결과를 같은 도면 목록의 모든 `DrawingSheetData.PaintCode`에 저장한다. 빈 문자열도 조회 완료 상태로 캐시해 제작도·조립도·설치도·가공도가 같은 값 또는 같은 빈 상태를 사용한다.

### <a id="ResolveDrawingAssetPath"></a>ResolveDrawingAssetPath(fileName)
- **라인**: L2726+
- **역할**: 실행 폴더의 이미지 파일을 우선 사용하고, 개발 환경에서는 솔루션 루트 파일로 fallback

### <a id="PlaceImageInTemplateArea"></a>PlaceImageInTemplateArea(imagePath, area, margin=1)
- **라인**: L2742+
- **역할**: `TemplateTableData` 이미지 셀을 영역 크기에 종횡비 유지 fit 후 중앙 좌표에 직접 렌더링. 실패 시 절대경로 로그 후 도면 출력 계속

### <a id="SanitizeFileName"></a>SanitizeFileName
- **라인**: L1237
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
- `vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(ids)`
- `vizcore3d.Drawing2D.Object2D.Set2DViewAlignAreaReviewsPositionByOffset(rectMin, rectMax, offset)`
- `vizcore3d.Drawing2D.View.Add2DNoteFromWorldCoordinate(title, target, label)`
- `Node.Kind`, `Node.ParentIndex`, `Object3D.FromIndex` (연결 Part의 가장 가까운 부모 Assembly 이름 탐색)
- `DrawingSheetData.PaintCode` (같은 STRU 도면 목록의 제작도·조립도·설치도·가공도 공용 PNT 값)
- `DrawingSheetData.InstallationContextIndices`, `InstallationConnections` (설치도 직접 연결 외부 Part·접합영역 스냅샷, Assembly는 노트 문맥)

---

## 관련 문서
- 흐름 문서: [기능/도면시트/](../기능/도면시트/_인덱스.md)
