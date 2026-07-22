# Form1.BOM.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.BOM.cs` (약 860 라인)

**책임**: 3D 모델 로드, BOM 수집 + 홀/슬롯 감지, VIZCore3D 초기화 이벤트 핸들러, 메인 치수 추출 통합 파이프라인, 초기 상태 복원(모델 재로드). **라이선스 관리는 [Form1.License.cs](./form1-license.md)로 분리** (T-017, 2026-04-22).

---

## 주요 핸들러 · 메서드

### <a id="vizcore3d-oninitialized"></a>Vizcore3d_OnInitializedVIZCore3D
- **라인**: L136~L166
- **트리거**: `vizcore3d.OnInitializedVIZCore3D`
- **핵심**: `InitializeLicense()` 위임(Form1.License.cs) → ToolbarDrawing2D·ModelTree 표시 → Clash/Object3D 이벤트 구독 → 엣지 데이터 생성 활성화
- **흐름 문서**: [기능/BOM/VIZCore3D 초기화.md](../기능/BOM/VIZCore3D 초기화.md)

### <a id="btnOpen_Click"></a>btnOpen_Click
- **라인**: L168~L248
- **트리거**: `btnOpen` 버튼 클릭
- **핵심**: OpenFileDialog → 상태 완전 초기화 → `Model.Open` → FitToView + SilhouetteEdge + BuildBodyToPartNameMap
- **흐름 문서**: [기능/BOM/모델 열기.md](../기능/BOM/모델 열기.md)

### <a id="btnResetToInitial_Click"></a>btnResetToInitial_Click
- **라인**: L250~L270
- **트리거**: `btnResetToInitial` 버튼 클릭 (3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽, 회색)
- **핵심**: 가드 체크(`currentFilePath` + `Model.IsOpen`) → 확인 다이얼로그 → `ResetToInitialState()` 위임
- **흐름 문서**: [기능/BOM/초기화.md](../기능/BOM/초기화.md)

### <a id="btnMainDimension_Click"></a>btnMainDimension_Click
- **라인**: L332~L398
- **트리거**: `btnMainDimension` 버튼 클릭
- **핵심**: BOM 재수집 → `DetectClash(includeOutsideNeighbors: true)` 비동기 시작(대상 내부 연결성 + 제작도 주변 Part 결과) → 완료 이벤트에서 Osnap·치수·시트 생성
- **흐름 문서**: [기능/BOM/메인 치수 추출.md](../기능/BOM/메인 치수 추출.md)

### <a id="btnCollectBOM_Click"></a>btnCollectBOM_Click
- **라인**: L844~L857
- **트리거**: `btnCollectBOM` 버튼 클릭
- **핵심**: `CollectBOMData()` 위임 + 결과 알림
- **흐름 문서**: [기능/BOM/BOM 수집.md](../기능/BOM/BOM 수집.md)

---

## 내부 헬퍼 메서드

| 메서드 | 라인 | 역할 |
|---|---|---|
| `ResetToInitialState` | L272~L330 | btnOpen의 초기화 블록 + `balloonOverrides.Clear()` + 동일 경로 `Model.Open` 재로드. btnResetToInitial_Click에서 호출 |
| `CollectAllOsnap` | L514 | 전체 Osnap 수집 (LINE/POINT만), X-Ray 모드 반영, 같은 원본으로 가공도 주축 판정 캐시 적재 |
| `CollectBOMInfo` | L20 (Clash.cs) | 도면정보 탭용 그룹 수집 |
| `CollectBOMData` | (BOM 수집 내부) | bomList 채우는 핵심 로직 |
| `DetectHoles` | (홀 감지 내부) | 원형/슬롯형 홀 자동 인식 |
| `BuildBodyToPartNameMap` | L37 | Body↔Part 캐시 구축 + 제작도 연결 후보 BBox 캐시 초기화 |
| `GetPartNameFromBodyIndex` | L105 | Body Index → Part 풀네임 역조회 |
| `DetectHoles` | L785 | BOM 부재의 원형·슬롯형 홀 감지 |

---

## 주요 공유 필드 (Form1 멤버)

| 필드 | 타입 | 용도 |
|---|---|---|
| `vizcore3d` | VIZCore3DControl | 3D 뷰어 |
| `bomList` | List&lt;BOMData&gt; | 부재 목록 |
| `clashList` | List&lt;ClashData&gt; | 간섭 결과 |
| `osnapPoints` | List&lt;Vertex3D&gt; | Osnap 좌표 |
| `osnapPointsWithNames` | List&lt;(Vertex3D, string)&gt; | 좌표 + 노드명 |
| `chainDimensionList` | List&lt;ChainDimensionData&gt; | 체인 치수 |
| `xraySelectedNodeIndices` | List&lt;int&gt; | X-Ray 선택 부재 |
| `bodyToPartNameMap` / `bodyToPartIndexMap` | Dict | Body→Part 캐시 |
| 제작도 연결 후보 캐시 | Dict / HashSet | 전체 Body BBox·실제 부모 Part 캐시와 근접 후보 Clash 결과 |
| `currentFilePath` | string | 현재 로드된 파일 경로 |
| `_autoProcessOsnapSuccess` | bool | 자동 파이프라인 Osnap 성공 플래그 |

> `licenseRefreshTimer` 필드는 [Form1.License.cs](./form1-license.md)로 이동 (T-017).

---

## VIZCore3D API 사용

- `vizcore3d.Model.Open(path)`, `vizcore3d.Model.IsOpen()`
- `vizcore3d.Object3D.GetPartialNode(bool, bool, bool)` — Top/Part/Body 필터
- `vizcore3d.Object3D.UDA.FromIndex(idx, key)`, `UDA.Keys`
- `vizcore3d.Object3D.GetBoundBox(indices, useLocal)`
- `vizcore3d.Object3D.GetOsnapPoint(idx)` — OsnapKind 분기
- `vizcore3d.Clash.ClearResultSymbol()`
- `vizcore3d.View.FitToView()`, `View.SilhouetteEdge`
- `vizcore3d.Review.Measure.AddCustomAxisDistance(axis, start, end)`

---

## 관련 문서
- 흐름 문서: [기능/BOM/](../기능/BOM/_인덱스.md)
- 상위 파이프라인: [../_pipeline.md](../_pipeline.md)
