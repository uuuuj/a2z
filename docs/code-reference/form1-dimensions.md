# Form1.Dimensions.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Dimensions.cs` (약 2,810 라인)

**책임**: 체인 치수 표시/삭제/필터, 축별 뷰 전환, 풍선 조정, Clash 연동 자동 선택, 현재 뷰 기반 치수 추출.

---

## 주요 핸들러

| 핸들러 | 라인 | 트리거 | 흐름 문서 |
|---|---|---|---|
| <a id="btnDimensionShowSelected_Click"></a>`btnDimensionShowSelected_Click` | L17 | 버튼 | [show-selected](../기능/치수/선택%20치수%20표시.md) |
| <a id="btnDimensionDelete_Click"></a>`btnDimensionDelete_Click` | L133 | 버튼 | [delete](../기능/치수/치수%20삭제.md) |
| <a id="btnShowAxisX_Click"></a>`btnShowAxisX_Click` | L205 | 버튼 | [show-axis-x](../기능/치수/X축%20치수%20표시.md) |
| <a id="btnShowAxisY_Click"></a>`btnShowAxisY_Click` | L213 | 버튼 | [show-axis-y](../기능/치수/Y축%20치수%20표시.md) |
| <a id="btnShowAxisZ_Click"></a>`btnShowAxisZ_Click` | L221 | 버튼 | [show-axis-z](../기능/치수/Z축%20치수%20표시.md) |
| <a id="btnShowISO_Click"></a>`btnShowISO_Click` | L229 | 버튼 | [show-iso](../기능/치수/ISO%20풍선%20표시.md) |
| <a id="btnBalloonAdjust_Click"></a>`btnBalloonAdjust_Click` | L237 | 버튼 | [balloon-adjust](../기능/치수/풍선%20위치%20조정.md) |
| <a id="LvClash_SelectedIndexChanged"></a>`LvClash_SelectedIndexChanged` | L1748 | 이벤트 | [lvclash-selected](../기능/치수/Clash%20선택%20시%20치수%20필터.md) |
| <a id="btnExtractDimension_Click"></a>`btnExtractDimension_Click` | L1955 | 버튼 | [extract-dimension](../기능/치수/현재%20뷰%20기반%20체인%20치수%20추출.md) |

---

## 내부 헬퍼

| 메서드 | 역할 |
|---|---|
| `AddChainDimensionByAxis` | 축별 체인 치수 생성 |
| `MergeCoordinates` | tolerance 내 Osnap 좌표 병합 |
| `ShowAllDimensions(axis?)` | `chainDimensionList`를 뷰별 필터링한 뒤 SDK Measure에 추가. 설치도 2D 출력은 선택 STRU BBox를 기준선·축척 기준으로 사용하고 연결 Part는 점선 맥락으로만 처리 |
| `AssignDimensionPriorities` / `ApplySmartFiltering` | 전체 치수 우선순위·겹침 필터. `IsRequired` 설치 접합 치수는 개수·겹침 제한보다 먼저 보존 |
| `SelectRelatedOsnapItems` | Clash 기반 관련 Osnap ListView 자동 선택 |
| `SelectRelatedDimensionItems` | Clash BBox 기반 관련 치수 ListView 자동 선택 |
| `ShowMemberNameOverlay` | 부재명 TextBox 오버레이 (panelViewer 위) |
| `GetViewNameByAxis` | "X" → "정면도" 등 변환 |

---

## MeasureStyle 표준 설정 (btnDimensionShowSelected_Click)

| 속성 | 값 |
|---|---|
| `Prefix` / `Unit` / `DX_DY_DZ` / `Frame` | false |
| `NumberOfDecimalPlaces` | 0 |
| `BackgroundColor` | White |
| `FontColor` / `LineColor` / `ArrowColor` | Blue |
| `FontSize` | SIZE14 |
| `FontBold` | true |
| `ArrowSize` | 8 |
| `AlignDistanceText` | true, margin=3 |

---

## VIZCore3D API 사용

- `vizcore3d.Review.Measure.SetStyle(MeasureStyle)`, `.Clear()`, `.AddCustomAxisDistance(axis, start, end)`
- `vizcore3d.View.SetPivotPosition(vertex)`
- `vizcore3d.BeginUpdate()` / `EndUpdate()`

---

## 관련 문서
- 흐름 문서: [기능/치수/](../기능/치수/_인덱스.md)
