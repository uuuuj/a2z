# Form1.Clash.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Clash.cs` (약 1,170 라인)

**책임**: Clash 탭용 BOM 정보 수집(UDA 그룹화), 간섭 검사 수행(ClashManager), 완료 이벤트 처리.

---

## 주요 핸들러 · 메서드

### <a id="btnCollectBOMInfo_Click"></a>btnCollectBOMInfo_Click
- **라인**: L21~L24
- **트리거**: `btnCollectBOMInfo` 버튼 클릭
- **핵심**: `CollectBOMInfo(true)` 위임
- **흐름 문서**: [기능/간섭검사/BOM 정보 수집.md](../기능/간섭검사/BOM 정보 수집.md)

### <a id="CollectBOMInfo"></a>CollectBOMInfo (내부 공용)
- **라인**: L49~L95
- **시그니처**: `CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)`
- **핵심**: 준비된 시트 BOM 스냅샷이 있으면 즉시 적용. 없으면 관련 Part만 UDA 파싱해 스냅샷 생성 후 표시
- **특기**: `sheetOverride` 지원, 시트 생성 단계에서 모든 시트 BOM을 일괄 준비

### PrepareDrawingSheetBomCaches / BuildDrawingBomPreparationContext
- **라인**: L100~L243
- **핵심**: 모델 로드 때 만든 Body→Part 매핑 재사용 → 관련 Part의 SPREF/MATREF/GWEI/POSSTART/POSEND를 Part별 1회 조회 → 시트별 행·Body 그룹 맵 메모리 생성

### ReadDrawingBomPartData
- **라인**: L224~L310
- **핵심**: 현재 Part부터 부모 10단계까지 UDA를 조회해 BOM 문자열을 구성. SPREF의 유효한 ITEM은 기존 파싱을 유지하고, SPREF 키 없음·null·빈 문자열·공백 또는 빈 ITEM은 `unset`으로 저장
- **공통 적용**: 결과가 `DrawingBomSnapshot`을 거쳐 제작도·조립도·설치도·가공도 BOM 표에 동일하게 사용됨. 스냅샷 데이터 행 생성 시 MATERIAL·SIZE·Q'TY·T/W·MA·FA의 null·빈 문자열·공백은 셀별로 `-` 처리하며, ITEM이 `unset`이면 No·ITEM을 제외한 뒤쪽 열 전체를 `-`로 마스킹

### <a id="DetectClash"></a>DetectClash (내부)
- **라인**: L783~L886
- **시그니처**: `bool DetectClash(bool includeOutsideNeighbors = false)`
- **핵심**: 대상 Body 수집 → `Clash.Clear()` → N×(N-1)/2 내부 쌍 `ClashTest` 생성. 옵션이 켜지면 전체 Body BBox 캐시에서 대상과 3mm 이내인 후보만 선별해 제작도 점선용 `대상 vs 근접 후보` 그룹 검사 1건 추가 → 등록 ID 큐를 `PerformInterferenceCheck(id, false)`로 무창 직렬 실행
- **파라미터**: ClearanceValue=3.0, RangeValue=3.0, PenetrationTolerance=1.0 (mm)

### 무창 Clash ID 큐
- **라인**: L646~L777
- **핵심**: `StartSilentClashSequence`가 등록된 검사 ID를 큐에 저장하고 첫 항목을 progress form 없이 시작. `AdvanceSilentClashSequence`는 개별 완료 이벤트에서 다음 실행을 UI 메시지 큐로 넘기고, `StartNextSilentClashTestAfterEvent`가 SDK Busy 해제와 후속 시작 성공을 50ms 간격·최대 2초 재시도. 마지막 완료일 때만 전체 결과 처리 허용
- **실패 정책**: 후속 ID 시작 실패 시 큐·오버레이를 정리하고 일반 경로는 재실행 안내, STRU 일괄 경로는 기존 최소 시트 fallback 적용

### 제작도 연결 후보 광역 필터
- **라인**: L464~L641
- **핵심**: 모델 Body별 Bounding Box와 실제 부모 Part를 첫 실행에 캐시 → 대상 통합 BBox → 대상 개별 BBox의 2단계 겹침 검사 → 근접 후보만 반환
- **캐시**: 같은 모델에서는 재사용하고 `BuildBodyToPartNameMap` 호출(모델 열기·재로드) 시 초기화

### <a id="btnClashDetection_Click"></a>btnClashDetection_Click
- **라인**: L892~L906
- **트리거**: `btnClashDetection` 버튼 클릭
- **핵심**: `DetectClash()` 위임 + 시작 알림
- **흐름 문서**: [기능/간섭검사/간섭검사 실행.md](../기능/간섭검사/간섭검사 실행.md)

### <a id="Clash_OnClashTestFinishedEvent"></a>Clash_OnClashTestFinishedEvent
- **라인**: L908~L1067
- **트리거**: `vizcore3d.Clash.OnClashTestFinishedEvent`
- **핵심**: 무창 큐의 중간 완료 이벤트는 다음 실행을 예약하고 즉시 반환 → SDK Busy 해제 후 다음 ID 시작 → 마지막 완료에서 `GetResultItem(test, ResultGroupingOptions.PART)` → HotPoint XYZ·유효 여부를 `ClashData`에 보존 → 대상 내부 `clashList`와 제작도 연결 전용 리스트·Part 집합으로 분리 → 각각 중복 제거·Z값 정렬 → 연결 결과는 `lvClash`에 `[연결]` 접두어 표시 → 내부 연결성 판정 후 자동 파이프라인 계속
- **흐름 문서**: [기능/간섭검사/간섭검사 완료 이벤트.md](../기능/간섭검사/간섭검사 완료 이벤트.md)

---

## VIZCore3D API 사용

- `vizcore3d.Clash.Clear()`
- `vizcore3d.Clash.Add(ClashTest)` → bool
- `vizcore3d.Clash.PerformInterferenceCheck(int id, bool progressForm)` → bool (비동기, 현재 `progressForm=false`)
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
