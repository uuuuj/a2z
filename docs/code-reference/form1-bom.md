# Form1.BOM.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.BOM.cs` (약 1,431 라인)

**책임**: 3D 모델 로드, BOM 수집 + 홀/슬롯 감지, VIZCore3D 초기화 이벤트 핸들러, 라이선스 관리, 메인 치수 추출 통합 파이프라인.

---

## 주요 핸들러 · 메서드

### <a id="vizcore3d-oninitialized"></a>Vizcore3d_OnInitializedVIZCore3D
- **라인**: L135~L167
- **트리거**: `vizcore3d.OnInitializedVIZCore3D`
- **핵심**: 라이선스 서버 등록(`127.0.0.1:8901`), 30분 갱신 타이머, Clash/Object3D 이벤트 구독, 엣지 데이터 생성 활성화
- **흐름 문서**: [features/bom/vizcore3d-initialized.md](../features/bom/vizcore3d-initialized.md)

### <a id="btnOpen_Click"></a>btnOpen_Click
- **라인**: L209~L278
- **트리거**: `btnOpen` 버튼 클릭
- **핵심**: OpenFileDialog → 상태 완전 초기화 → `Model.Open` → FitToView + SilhouetteEdge + BuildBodyToPartNameMap
- **흐름 문서**: [features/bom/open-model.md](../features/bom/open-model.md)

### <a id="btnMainDimension_Click"></a>btnMainDimension_Click
- **라인**: L283~L351
- **트리거**: `btnMainDimension` 버튼 클릭
- **핵심**: BOM → Osnap → MergeCoordinates → X/Y/Z AddChainDimensionByAxis → ShowAllDimensions → DetectClash(비동기)
- **흐름 문서**: [features/bom/main-dimension.md](../features/bom/main-dimension.md)

### <a id="btnCollectBOM_Click"></a>btnCollectBOM_Click
- **라인**: L1418~L1429
- **트리거**: `btnCollectBOM` 버튼 클릭
- **핵심**: `CollectBOMData()` 위임 + 결과 알림
- **흐름 문서**: [features/bom/collect-bom.md](../features/bom/collect-bom.md)

---

## 내부 헬퍼 메서드

| 메서드 | 라인 | 역할 |
|---|---|---|
| `StartLicenseRefreshTimer` | L172 | 30분 주기 라이선스 갱신 타이머 |
| `LicenseRefreshTimer_Tick` | L183 | 실제 갱신 로직 (예외 시 Debug.WriteLine만) |
| `CollectAllOsnap` | L358 | 전체 Osnap 수집 (LINE/POINT만), X-Ray 모드 반영 |
| `CollectBOMInfo` | L20 (Clash.cs) | 도면정보 탭용 그룹 수집 |
| `CollectBOMData` | (BOM 수집 내부) | bomList 채우는 핵심 로직 |
| `DetectHoles` | (홀 감지 내부) | 원형/슬롯형 홀 자동 인식 |
| `BuildBodyToPartNameMap` | (모델 로드 후) | Body↔Part 캐시 구축 |
| `GetPartNameFromBodyIndex` | L104 | Body Index → Part 풀네임 역조회 |
| `GetHoleOrSlotForPoint` | L1395 | Osnap 좌표에 대응하는 홀/슬롯홀 사이즈 찾기 |

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
| `currentFilePath` | string | 현재 로드된 파일 경로 |
| `licenseRefreshTimer` | Timer | 라이선스 갱신 |
| `_autoProcessOsnapSuccess` | bool | 자동 파이프라인 Osnap 성공 플래그 |

---

## VIZCore3D API 사용

- `vizcore3d.License.LicenseServer(ip, port)`
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
- 흐름 문서: [features/bom/](../features/bom/_index.md)
- 상위 파이프라인: [../_pipeline.md](../_pipeline.md)
