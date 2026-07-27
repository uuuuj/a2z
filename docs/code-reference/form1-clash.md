# Form1.Clash.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Clash.cs` (약 1,269 라인)

**책임**: Clash 탭용 BOM 정보 수집(UDA 그룹화), 간섭 검사 수행(ClashManager), 완료 이벤트 처리.

---

## 주요 핸들러 · 메서드

### <a id="btnCollectBOMInfo_Click"></a>btnCollectBOMInfo_Click
- **라인**: L21~L24
- **트리거**: `btnCollectBOMInfo` 버튼 클릭
- **핵심**: `CollectBOMInfo(true)` 위임
- **흐름 문서**: [기능/간섭검사/BOM 정보 수집.md](../기능/간섭검사/BOM%20정보%20수집.md)

### <a id="CollectBOMInfo"></a>CollectBOMInfo (내부 공용)
- **라인**: L51~L96
- **시그니처**: `CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)`
- **핵심**: 준비된 시트 BOM 스냅샷이 있으면 즉시 적용. 없으면 관련 Part만 UDA 파싱해 스냅샷 생성 후 표시
- **특기**: `sheetOverride` 지원, 시트 생성 단계에서 모든 시트 BOM을 일괄 준비

### PrepareDrawingSheetBomCaches / BuildDrawingBomPreparationContext
- **라인**: L102~L229
- **핵심**: 모델 로드 때 만든 Body→Part 매핑 재사용 → 관련 Part의 SPREF/MATREF/GWEI/POSSTART/POSEND/STRU를 Part별 1회 조회 → 시트별 행·Body 그룹 맵 메모리 생성
- **STRU GWEI memo**: 조상 노드는 부재끼리 공유하므로 `struGweiByNode`로 노드당 1회만 판정·조회한다

### ReadDrawingBomPartData
- **라인**: L231~L330
- **핵심**: 현재 Part부터 부모 10단계까지 UDA를 조회해 BOM 문자열을 구성. SPREF의 유효한 ITEM은 기존 파싱을 유지하고, SPREF 키 없음·null·빈 문자열·공백 또는 빈 ITEM은 `unset`으로 저장
- **요약행 T/W(#67)**: 같은 walk-up에서 조상 STRU 노드를 만나면 **그 노드 자체의** `GWEI`를 `StruWeightDisplay`에 담는다. 부재용 `gweiVal` walk-up은 첫 비어있지 않은 값에서 멈추므로 요약행에 그대로 쓸 수 없다. STRU를 찾기 전에는 조기 break 하지 않는다
- **공통 적용**: 결과가 `DrawingBomSnapshot`을 거쳐 제작도·조립도·설치도·가공도 BOM 표에 동일하게 사용됨. 스냅샷 데이터 행 생성 시 MATERIAL·SIZE·Q'TY·T/W·MA·FA의 null·빈 문자열·공백은 셀별로 `-` 처리하며, ITEM이 `unset`이면 No·ITEM을 제외한 뒤쪽 열 전체를 `-`로 마스킹

### FormatDrawingBomWeight
- **라인**: L332~L349
- **핵심**: GWEI 원문에서 숫자만 남겨 소수 2자리로 정규화. 부재 무게와 요약행 STRU 무게가 같은 규칙을 쓰도록 공용화 (#67)

### BuildDrawingBomSnapshot
- **라인**: L371~L473
- **핵심**: 요약행 1행 + 데이터행으로 스냅샷 구성. 요약행은 No.=`00`, ITEM=`Support&Seat`(배관/전장 구분 확정 전 기본값), MATERIAL·SIZE·Q'TY=빈칸, MA·FA=`F`
- **요약행 T/W**: `StruWeightDisplay` 우선, 없을 때만 `parts.Sum(p => p.Weight)` 폴백 + `DiagLog` 기록
- **정렬 무영향**: `dataRows`만 정렬하고 요약행은 그 전에 `snapshot.Rows`에 들어가므로 No.=`00`이 정렬 순서를 바꾸지 않는다

### <a id="DetectClash"></a>DetectClash (내부)
- **라인**: L862~L969
- **시그니처**: `bool DetectClash(bool includeOutsideNeighbors = false)`
- **핵심**: 대상 Body 수집 → `Clash.Clear()` → N×(N-1)/2 내부 쌍 `ClashTest` 생성. 옵션이 켜지면 전체 Body BBox 캐시에서 대상과 3mm 이내인 후보만 선별해 제작도 점선용 `대상 vs 근접 후보` 그룹 검사 1건 추가 → 등록 ID 큐를 `PerformInterferenceCheck(id, false)`로 무창 직렬 실행
- **파라미터**: ClearanceValue=3.0, RangeValue=3.0, PenetrationTolerance=1.0 (mm)

### 무창 Clash ID 큐
- **라인**: L706~L860
- **핵심**: `StartSilentClashSequence`가 등록된 검사 ID를 큐에 저장하고 첫 항목을 progress form 없이 시작. `AdvanceSilentClashSequence`는 개별 완료 이벤트에서 다음 실행을 UI 메시지 큐로 넘기고, `StartNextSilentClashTestAfterEvent`가 SDK Busy 해제와 후속 시작 성공을 50ms 간격·최대 2초 재시도. 완료 이벤트와 예약 실행 대기에서 취소 요청을 확인해 다음 ID를 시작하지 않는다
- **실패 정책**: 후속 ID 시작 실패 시 큐·오버레이를 정리하고 일반 경로는 재실행 안내, STRU 일괄 경로는 기존 최소 시트 fallback 적용. 취소 요청이면 실패 fallback 대신 상위 작업의 취소 정리로 복귀

### 제작도 연결 후보 광역 필터
- **라인**: L524~L704
- **핵심**: 모델 Body별 Bounding Box와 실제 부모 Part를 첫 실행에 캐시 → 대상 통합 BBox → 대상 개별 BBox의 2단계 겹침 검사 → 근접 후보만 반환
- **캐시**: 같은 모델에서는 재사용하고 `BuildBodyToPartNameMap` 호출(모델 열기·재로드) 시 초기화

### <a id="btnClashDetection_Click"></a>btnClashDetection_Click
- **라인**: L971~L985
- **트리거**: `btnClashDetection` 버튼 클릭
- **핵심**: `DetectClash()` 위임 + 시작 알림
- **흐름 문서**: [기능/간섭검사/간섭검사 실행.md](../기능/간섭검사/간섭검사%20실행.md)

### <a id="Clash_OnClashTestFinishedEvent"></a>Clash_OnClashTestFinishedEvent
- **라인**: L987~L1165
- **트리거**: `vizcore3d.Clash.OnClashTestFinishedEvent`
- **핵심**: 취소 요청이면 현재 검사 완료 뒤 큐를 리셋하고 다음 ID를 차단. 계속할 때만 무창 큐의 중간 완료 이벤트는 다음 실행을 예약 → 마지막 완료에서 `GetResultItem(test, ResultGroupingOptions.PART)` → 대상 내부·제작도 연결 결과 분리 → 연결성 판정 후 자동 파이프라인 계속
- **흐름 문서**: [기능/간섭검사/간섭검사 완료 이벤트.md](../기능/간섭검사/간섭검사%20완료%20이벤트.md)

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
