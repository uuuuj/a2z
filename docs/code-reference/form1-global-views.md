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
- **라인**: L725~L900
- **핵심**:
  1. 같은 Target Body↔Connected Body의 여러 접합영역을 하나로 병합
  2. Target Body LINE Osnap 방향을 5도 이내로 군집화하고 길이 합 최대 방향을 길이축(주축)으로 선택
  3. Target 끝단면 MIN/MAX 중 연결 모서리에 가까운 쪽과, Connected Body에서 접합영역에 가장 가까운 LINE/POINT Osnap을 선택
  4. **접합 기하 기반 축 선택** (2026-07-23 재설계, issue #12): 위치 기준을 연결부재 osnap이 아니라 **접합선(`GetObjectCollisionLine`) 대표점(centroid)**으로 잡는다 — 접합점은 정의상 기준부재 위라 위치가 부재를 벗어날 수 없어 크기 임계·상한이 불필요. 축 선택은 크기 대신 **접합 덮음**: 각 축 `coverage = 접합 extent / 부재 extent`가 `InstallationContactCrossCoverage(0.5)` 이상이면 연결부재가 그 축으로 부재를 가로지름(관통) → 생략, 미만이면 국소적 → `SelectInstallationTargetEndForAxis`로 끝단 재선정 후 `AxisComponents`에 추가. **치수 끝점은 접합 한가운데(centroid)가 아니라 접합측 모서리**: 각 축 접합 구간(min/max) 중 부재 끝단에 가까운 쪽을 `ConnectionCoord`에 저장(연결부재가 닿기 시작하는 모서리 — 실기값 29·31 복원). centroid는 끝단·덮음 판정·로그용, ConnectionCoord는 실제 치수 끝점. `[설치치수축]` 로그에 부재·접합 extent, 축별 덮음%·모서리거리, 위치축/가로지름제외 기록
  5. Osnap이 없을 때만 해당 Body BBox로 fallback하고 `[설치위치]` 로그에 ID·축·좌표·거리·fallback 기록

### SelectInstallationTargetEndForAxis(targetPoints, connectedCorner, worldAxis)
- **라인**: L764~L791 부근
- **핵심**: 주축 끝단 선정 로직을 임의 월드 축으로 일반화. 축 좌표 MIN/MAX 중 연결 모서리에 가까운 끝단면을 잡고, 동률 허용오차(`InstallationPlacementTieTolerance`) 내 후보 중 축 직교 평면 거리(`PerpendicularDistanceInPlane`)가 최소인 점 반환

**뷰 배정** (`viewAxes`, 2026-07-23 실기 PDF): 위치 성분을 세 직교 뷰 모두에 배정한다(`{X:[X,Z,Y], Y:[Y,Z,X], Z:[Z,Y,X]}`). 설치도는 형강을 각 뷰에서 가로로 눕혀 -X 뷰에서도 길이축(X)이 화면에 보이는데, 옛 월드축 필터(-X는 Z·Y만)가 그런 뷰에서 길이 치수를 빠뜨렸다. 성분 축정렬 투영으로 안 보이는 뷰에선 점으로 접혀 부작용 없음.

### ComputeInstallationDimensions(DrawingSheetData sheet)
- **라인**: L955~L1030 부근
- **핵심**: 같은 Body 쌍을 병합한 뒤 실제 접촉 Target Body의 축별 끝단→Connected Body 접합측 모서리 필수 치수를 생성. 앵커의 `AxisComponents`(축 게이트가 채택한 긴 축들)만 순회하고 각 직교 뷰는 그 축이 보이는 뷰에만 성분 치수 표시 (2026-07-23 — 판 두께·법선 축·가는 부재 단면축을 배제해 1mm 틈 치수·연결부재 옆거리 성분 제거, 판형 평면도 폭 방향 치수는 유지. 성분별 `[설치치수]` 로그). 선택 STRU·연결 Assembly 전체 범위와 접합 중심·A1/A2는 생성하지 않음
- **성분 축정렬 투영** (2026-07-23): 각 성분은 기준 끝단·연결 모서리 두 점이 여러 축에서 벌어져 있어(예: X 147·Z 30) 끝점을 그 성분 축으로만 벌어지게 투영(나머지 두 좌표는 기준 끝단 공유). 투영 안 하면 보조선이 부재를 가로질러 큰 공백 발생
- **미소 성분 가드**: `AddInstallationDimension`이 축 성분 `≤ InstallationMinComponent(3mm)`를 제외 (끝단 근접 연결·어셈블리 틈 잔여 이중 차단)
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
