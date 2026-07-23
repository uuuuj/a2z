# Form1.GlobalViews.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.GlobalViews.cs` (약 980 라인)

**책임**: 글로벌 뷰(ISO/X/Y/Z) 버튼 핸들러, 3가지 경로(시트/X-Ray/전체) 공용 분기 함수, 설치도 치수 추출.

---

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="btnGlobalISO_Click"></a>`btnGlobalISO_Click` | L17 | [global-iso](../기능/글로벌뷰/글로벌 ISO.md) |
| <a id="btnGlobalAxisX_Click"></a>`btnGlobalAxisX_Click` | L25 | [global-axis-x](../기능/글로벌뷰/글로벌 X축.md) |
| <a id="btnGlobalAxisY_Click"></a>`btnGlobalAxisY_Click` | L33 | [global-axis-y](../기능/글로벌뷰/글로벌 Y축.md) |
| <a id="btnGlobalAxisZ_Click"></a>`btnGlobalAxisZ_Click` | L41 | [global-axis-z](../기능/글로벌뷰/글로벌 Z축.md) |

모든 핸들러는 `ApplyGlobalView(direction)`으로 위임.

---

## 핵심 공용 함수

### <a id="ApplyGlobalView"></a>ApplyGlobalView(string viewDirection)
- **라인**: L49~L74
- **분기**:
  1. `SelectedTab == tabPageDrawing` + 시트 선택 → `ApplyDrawingSheetView(direction)` (DrawingSheets.cs 소속)
  2. `xraySelectedNodeIndices.Count > 0` → `ApplySelectedNodesView(direction)`
  3. 그 외 → `ApplyFullModelView(direction)`

### <a id="ApplySelectedNodesView"></a>ApplySelectedNodesView(string viewDirection)
- **라인**: L79~L131
- **핵심**: X-Ray 활성 → Review.* Clear → DASH_LINE → `MoveCamera(direction_PLUS)` → `FlyToObject3d(xraySelectedNodeIndices, 1.0f)`
- **ISO 분기**: `CreateIsoBalloonNotes(xraySelectedNodeIndices)` — 치수 대신 풍선

### <a id="ApplyFullModelView"></a>ApplyFullModelView(string viewDirection)
- **라인**: L136~L185
- **핵심**: X-Ray 해제 + `xraySelectedNodeIndices.Clear()` → `RestoreAllPartsVisibility()` → `FitToView()`
- **ISO 분기**: 전체 `bomList` 인덱스로 `CreateIsoBalloonNotes`

### <a id="ExtractInstallationDimensions"></a>ExtractInstallationDimensions(DrawingSheetData sheet)
- **라인**: L225~L260
- **핵심**: 사전 계산된 설치 치수를 `chainDimensionList`·`lvDimension`에 적용하고, 글로벌 뷰용 선택 대상을 STRU+직접 연결 외부 Part로 갱신

### PrepareInstallationConnectionData(DrawingSheetData sheet)
- **라인**: L266~L389
- **알고리즘**:
  1. 제작 대상과 설치 시트 BODY 집합 일치 확인
  2. 외부 연결 Clash의 Part 쌍별 하위 BODY 조합 생성
  3. BBox 0.5mm 필터 후 `GeometryUtility.GetObjectCollisionLine(bodyA, bodyB)` 조회
  4. 이어진 선분은 1mm tolerance로 한 접합 영역, 떨어진 선분은 별도 영역으로 분리
  5. 접합선이 없으면 `GetJunctionMesh`, 그마저 없으면 HotPoint 근접 fallback
  6. 접합점은 BODY LINE/POINT Osnap 3mm 이내 스냅하고, 최종 표시는 연결 Part별 A/B/C 라벨 부여
  7. 점선 문맥은 부모 Assembly 전체가 아니라 직접 연결된 외부 Part 인덱스만 저장. Assembly는 이름 노트용 메타데이터로 유지

### BuildInstallationPlacementAnchor
- **라인**: L725~L864
- **핵심**:
  1. 같은 Target Body↔Connected Body의 여러 접합영역을 하나로 병합
  2. Target Body LINE Osnap 방향을 5도 이내로 군집화하고 길이 합 최대 방향을 길이축으로 선택
  3. Target 끝단면 MIN/MAX 중 연결 모서리에 가까운 쪽과, Connected Body에서 접합영역에 가장 가까운 LINE/POINT Osnap을 선택
  4. Osnap이 없을 때만 해당 Body BBox로 fallback하고 `[설치위치]` 로그에 ID·축·좌표·거리·fallback 기록

### ComputeInstallationDimensions(DrawingSheetData sheet)
- **라인**: L879~L949
- **핵심**: 같은 Body 쌍을 병합한 뒤 실제 접촉 Target Body의 가까운 끝단→Connected Body 접합측 모서리 필수 치수를 생성. 끝단↔모서리 벡터의 X/Y/Z 세 축 성분을 모두 만들고 각 직교 뷰는 화면에 보이는 두 성분만 표시 (2026-07-23 — 주축 1성분만 만들면 주축과 직교하는 뷰가 빈 뷰였음. 성분별 `[설치치수]` 로그). 선택 STRU·연결 Assembly 전체 범위와 접합 중심·A1/A2는 생성하지 않음
- **Osnap 정책**: LINE Start/End + POINT Center만 사용, CIRCLE 제외. Osnap이 전혀 없을 때만 BBox 꼭짓점 fallback

---

## CameraDirection 매핑

| viewDirection | CameraDirection |
|---|---|
| "ISO" | ISO_PLUS |
| "X" | X_PLUS |
| "Y" | Y_PLUS |
| "Z" | Z_PLUS |

---

## X-Ray 설정 표준

| 속성 | 값 |
|---|---|
| `ColorType` | `XRayColorTypes.OBJECT_COLOR` |
| `SelectionObject3DType` | `SelectionObject3DTypes.OPAQUE_OBJECT3D` |
| `SilhouetteEdge` | true |
| `SilhouetteEdgeColor` | Green |

---

## VIZCore3D API 사용

- `vizcore3d.View.XRay.Enable / Select / Clear / ColorType / SelectionObject3DType`
- `vizcore3d.View.MoveCamera(CameraDirection)`
- `vizcore3d.View.FlyToObject3d(indices, zoomFactor)`, `FitToView()`
- `vizcore3d.View.SetRenderMode(RenderModes.DASH_LINE)`
- `vizcore3d.Review.Note.Clear()`, `Measure.Clear()`, `ShapeDrawing.Clear()`
- `vizcore3d.GeometryUtility.GetObjectCollisionLine(bodyA, bodyB)`
- `vizcore3d.GeometryUtility.GetJunctionMesh(bodyA, bodyB, false)`
- `vizcore3d.Object3D.GetOsnapPoint(bodyIndex)`, `GetChildObject3d(...)`, `GetBoundBox(...)`

---

## 관련 문서
- 흐름 문서: [기능/글로벌뷰/](../기능/글로벌뷰/_인덱스.md)
- 관련 공용 함수: [ApplyDrawingSheetView](./form1-drawing-sheets.md#ApplyDrawingSheetView)
