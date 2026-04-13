# Form1.Dimensions.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Dimensions.cs` (약 2,178 라인)

**책임**: 체인 치수 표시/삭제/필터, 축별 뷰 전환, 풍선 조정, Clash 연동 자동 선택, 현재 뷰 기반 치수 추출.

---

## 주요 핸들러

| 핸들러 | 라인 | 트리거 | 흐름 문서 |
|---|---|---|---|
| <a id="btnDimensionShowSelected_Click"></a>`btnDimensionShowSelected_Click` | L17 | 버튼 | [show-selected](../features/dimensions/show-selected.md) |
| <a id="btnDimensionDelete_Click"></a>`btnDimensionDelete_Click` | L134 | 버튼 | [delete](../features/dimensions/delete.md) |
| <a id="btnShowAxisX_Click"></a>`btnShowAxisX_Click` | L206 | 버튼 | [show-axis-x](../features/dimensions/show-axis-x.md) |
| <a id="btnShowAxisY_Click"></a>`btnShowAxisY_Click` | L214 | 버튼 | [show-axis-y](../features/dimensions/show-axis-y.md) |
| <a id="btnShowAxisZ_Click"></a>`btnShowAxisZ_Click` | L222 | 버튼 | [show-axis-z](../features/dimensions/show-axis-z.md) |
| <a id="btnShowISO_Click"></a>`btnShowISO_Click` | L230 | 버튼 | [show-iso](../features/dimensions/show-iso.md) |
| <a id="btnBalloonAdjust_Click"></a>`btnBalloonAdjust_Click` | L238 | 버튼 | [balloon-adjust](../features/dimensions/balloon-adjust.md) |
| <a id="LvClash_SelectedIndexChanged"></a>`LvClash_SelectedIndexChanged` | L1551 | 이벤트 | [lvclash-selected](../features/dimensions/lvclash-selected.md) |
| <a id="btnExtractDimension_Click"></a>`btnExtractDimension_Click` | L1735 | 버튼 | [extract-dimension](../features/dimensions/extract-dimension.md) |

---

## 내부 헬퍼

| 메서드 | 역할 |
|---|---|
| `AddChainDimensionByAxis` | 축별 체인 치수 생성 |
| `MergeCoordinates` | tolerance 내 Osnap 좌표 병합 |
| `ShowAllDimensions(axis?)` | `chainDimensionList` 전체를 SDK Measure에 추가 (axis로 필터 가능) |
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
- 흐름 문서: [features/dimensions/](../features/dimensions/_index.md)
