# Form1.Clash.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Clash.cs` (약 900 라인)

**책임**: Clash 탭용 BOM 정보 수집(UDA 그룹화), 간섭 검사 수행(ClashManager), 완료 이벤트 처리.

---

## 주요 핸들러 · 메서드

### <a id="btnCollectBOMInfo_Click"></a>btnCollectBOMInfo_Click
- **라인**: L15~L18
- **트리거**: `btnCollectBOMInfo` 버튼 클릭
- **핵심**: `CollectBOMInfo(true)` 위임
- **흐름 문서**: [기능/간섭검사/BOM 정보 수집.md](../기능/간섭검사/BOM 정보 수집.md)

### <a id="CollectBOMInfo"></a>CollectBOMInfo (내부 공용)
- **라인**: L20~L329
- **시그니처**: `CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)`
- **핵심**: Part 노드 → UDA(SPREF, MATREF, GWEI) 파싱 → (Item+Size+Material) 그룹화 → `lvDrawingBOMInfo` 표시
- **특기**: sheetOverride 지원 (선택 시트의 부재만 필터)

### <a id="DetectClash"></a>DetectClash (내부)
- **라인**: L514~L618
- **시그니처**: `bool DetectClash(bool includeOutsideNeighbors = false)`
- **핵심**: 대상 Body 수집 → `Clash.Clear()` → N×(N-1)/2 내부 쌍 `ClashTest` 생성. 옵션이 켜지면 전체 Body BBox 캐시에서 대상과 3mm 이내인 후보만 선별해 제작도 점선용 `대상 vs 근접 후보` 그룹 검사 1건 추가 → `PerformInterferenceCheck()` (비동기)
- **파라미터**: ClearanceValue=3.0, RangeValue=3.0, PenetrationTolerance=1.0 (mm)

### 제작도 연결 후보 광역 필터
- **라인**: L331~L512
- **핵심**: 모델 Body별 Bounding Box와 실제 부모 Part를 첫 실행에 캐시 → 대상 통합 BBox → 대상 개별 BBox의 2단계 겹침 검사 → 근접 후보만 반환
- **캐시**: 같은 모델에서는 재사용하고 `BuildBodyToPartNameMap` 호출(모델 열기·재로드) 시 초기화

### <a id="btnClashDetection_Click"></a>btnClashDetection_Click
- **라인**: L620~L634
- **트리거**: `btnClashDetection` 버튼 클릭
- **핵심**: `DetectClash()` 위임 + 시작 알림
- **흐름 문서**: [기능/간섭검사/간섭검사 실행.md](../기능/간섭검사/간섭검사 실행.md)

### <a id="Clash_OnClashTestFinishedEvent"></a>Clash_OnClashTestFinishedEvent
- **라인**: L636~L790
- **트리거**: `vizcore3d.Clash.OnClashTestFinishedEvent`
- **핵심**: `GetResultItem(test, ResultGroupingOptions.PART)` → 대상 내부 `clashList`와 제작도 연결 전용 리스트·Part 집합으로 분리 → 각각 중복 제거·Z값 정렬 → 연결 결과는 `lvClash`에 `[연결]` 접두어 표시 → 내부 연결성 판정 후 자동 파이프라인 계속
- **흐름 문서**: [기능/간섭검사/간섭검사 완료 이벤트.md](../기능/간섭검사/간섭검사 완료 이벤트.md)

---

## VIZCore3D API 사용

- `vizcore3d.Clash.Clear()`
- `vizcore3d.Clash.Add(ClashTest)` → bool
- `vizcore3d.Clash.PerformInterferenceCheck()` → bool (비동기)
- `vizcore3d.Clash.ClashTestCount`
- `vizcore3d.Clash.Items[i]`
- `vizcore3d.Clash.GetResultItem(test, ResultGroupingOptions.PART)`
- `vizcore3d.Object3D.UDA.Keys`, `UDA.FromIndex(nodeIdx, key)`

---

## ClashTest 설정 기본값

| 속성 | 값 |
|---|---|
| `TestKind` | GROUP_VS_GROUP |
| `ClearanceValue` | 3.0f |
| `RangeValue` | 3.0f |
| `PenetrationTolerance` | 1.0f |
| `UseRangeValue` | true |
| `UsePenetrationTolerance` | true |
| `VisibleOnly` | false |
| `BottomLevel` | 0 |

---

## 관련 문서
- 흐름 문서: [기능/간섭검사/](../기능/간섭검사/_인덱스.md)
