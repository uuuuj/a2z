# Form1.Clash.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Clash.cs` (약 502 라인)

**책임**: Clash 탭용 BOM 정보 수집(UDA 그룹화), 간섭 검사 수행(ClashManager), 완료 이벤트 처리.

---

## 주요 핸들러 · 메서드

### <a id="btnCollectBOMInfo_Click"></a>btnCollectBOMInfo_Click
- **라인**: L15~L18
- **트리거**: `btnCollectBOMInfo` 버튼 클릭
- **핵심**: `CollectBOMInfo(true)` 위임
- **흐름 문서**: [features/clash/collect-bom-info.md](../features/clash/collect-bom-info.md)

### <a id="CollectBOMInfo"></a>CollectBOMInfo (내부 공용)
- **라인**: L20~L302
- **시그니처**: `CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)`
- **핵심**: Part 노드 → UDA(SPREF, MATREF, GWEI) 파싱 → (Item+Size+Material) 그룹화 → `lvDrawingBOMInfo` 표시
- **특기**: sheetOverride 지원 (선택 시트의 부재만 필터)

### <a id="DetectClash"></a>DetectClash (내부)
- **라인**: L307~L376
- **시그니처**: `bool DetectClash()`
- **핵심**: 가시 Body 수집 → `Clash.Clear()` → N×(N-1)/2 쌍 `ClashTest` 생성 → `PerformInterferenceCheck()` (비동기)
- **파라미터**: ClearanceValue=1.0, RangeValue=1.0, PenetrationTolerance=1.0 (mm)

### <a id="btnClashDetection_Click"></a>btnClashDetection_Click
- **라인**: L381~L392
- **트리거**: `btnClashDetection` 버튼 클릭
- **핵심**: `DetectClash()` 위임 + 시작 알림
- **흐름 문서**: [features/clash/detect-clash.md](../features/clash/detect-clash.md)

### <a id="Clash_OnClashTestFinishedEvent"></a>Clash_OnClashTestFinishedEvent
- **라인**: L397~L500
- **트리거**: `vizcore3d.Clash.OnClashTestFinishedEvent`
- **핵심**: `GetResultItem(test, ResultGroupingOptions.PART)` → ClashData 변환 → 중복 제거(A-B/B-A) → Z값 내림차순 정렬 → 요약 MessageBox → `GenerateDrawingSheets()` 자동 호출
- **흐름 문서**: [features/clash/clash-finished-event.md](../features/clash/clash-finished-event.md)

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
| `ClearanceValue` | 1.0f |
| `RangeValue` | 1.0f |
| `PenetrationTolerance` | 1.0f |
| `UseRangeValue` | true |
| `UsePenetrationTolerance` | true |
| `VisibleOnly` | false |
| `BottomLevel` | 0 |

---

## 관련 문서
- 흐름 문서: [features/clash/](../features/clash/_index.md)
