# Dimensions 기능

`Form1.Dimensions.cs` 소속. 체인 치수 표시/삭제/필터링 UI 흐름입니다.

## 기능 목록
| ID | 기능 | 트리거 | 유형 | 문서 |
|---|---|---|---|---|
| DIM-001 | 선택 치수 3D 표시 | btnDimensionShowSelected_Click | User Action | [show-selected](./show-selected.md) |
| DIM-002 | 치수 삭제 | btnDimensionDelete_Click | User Action | [delete](./delete.md) |
| DIM-003 | X축 뷰 + 치수 표시 | btnShowAxisX_Click | User Action | [show-axis-x](./show-axis-x.md) |
| DIM-004 | Y축 뷰 + 치수 표시 | btnShowAxisY_Click | User Action | [show-axis-y](./show-axis-y.md) |
| DIM-005 | Z축 뷰 + 치수 표시 | btnShowAxisZ_Click | User Action | [show-axis-z](./show-axis-z.md) |
| DIM-006 | ISO 뷰 + 치수 표시 | btnShowISO_Click | User Action | [show-iso](./show-iso.md) |
| DIM-007 | 풍선 위치 수동 조정 | btnBalloonAdjust_Click | User Action | [balloon-adjust](./balloon-adjust.md) |
| DIM-008 | Clash 선택 시 치수 필터 | LvClash_SelectedIndexChanged | Event Callback | [lvclash-selected](./lvclash-selected.md) |
| DIM-009 | 설치도 치수 추출 | btnExtractDimension_Click | User Action | [extract-dimension](./extract-dimension.md) |

## 주요 조작 대상
- `chainDimensionList` (Priority/DisplayLevel/IsVisible/IsMerged 필드 조작)
- `vizcore3d.Review.Measure.*`
- `balloonOverrides`
