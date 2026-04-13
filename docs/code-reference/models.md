# Models.cs — 코드 레퍼런스

**경로**: `A2Z/Models.cs`

**책임**: 앱 전반에서 사용되는 도메인 데이터 모델 정의.

---

## 데이터 클래스

### ChainDimensionData (L9~L44)
체인 치수 데이터. 축별 오프셋 표시, 우선순위 필터, 병합/표시 레벨 플래그.

| 필드 | 타입 | 용도 |
|---|---|---|
| `No` | int | 치수 번호 (ListView 표시용) |
| `Priority` | int (1~10) | 필터링 우선순위 |
| `DisplayLevel` | int | 표시 계층 |
| `IsVisible` | bool | 표시 여부 |
| `IsMerged` | bool | 병합 여부 |
| `IsTotal` | bool | 전체 조립 치수 여부 |
| `Axis` | string | "X"/"Y"/"Z" |
| `ViewName` | string | "정면도"/"평면도" 등 |
| `Distance` | float | 거리 (mm) |
| `StartPoint` / `EndPoint` | Vector3D | 양 끝점 |
| `StartPointStr` / `EndPointStr` | string | 포맷된 좌표 문자열 |

### BOMData (L49~L77)
부재 목록 단위. BBox, 홀 정보, 회전각 포함.

| 필드 | 타입 | 용도 |
|---|---|---|
| `Index` | int | 부재 Index |
| `Name` | string | 부재명 |
| `RotationAngle` | float | 회전각 |
| `CenterX/Y/Z` | float | 중심 |
| `MinX/Y/Z`, `MaxX/Y/Z` | float | BBox |
| `CircleRadius` | float | 원형 단면 반경 |
| `Purpose` | string | 용도 |
| `HoleSize` | string | 대표 홀 사이즈 |
| `Holes` | List&lt;HoleInfo&gt; | 원형 홀 리스트 |
| `SlotHoleSize` | string | 대표 슬롯홀 사이즈 |
| `SlotHoles` | List&lt;SlotHoleInfo&gt; | 슬롯홀 리스트 |

### HoleInfo (L82~L89)
원형 홀.

| 필드 | 타입 |
|---|---|
| `Diameter` | float |
| `CenterX/Y/Z` | float |
| `CylinderBodyIndex` | int |

### SlotHoleInfo (L94~L102)
슬롯(장공) 홀.

| 필드 | 타입 |
|---|---|
| `Radius` | float |
| `SlotLength` | float |
| `Depth` | float |
| `CenterX/Y/Z` | float |

### ClashData (L107~L114)
간섭 쌍.

| 필드 | 타입 | 용도 |
|---|---|---|
| `Index1`, `Index2` | int | 간섭 부재 Index |
| `Name1`, `Name2` | string | 간섭 부재명 |
| `ZValue` | float | HotPoint Z (정렬 기준) |

### DrawingSheetData (L119~L133)
도면 시트.

| 필드 | 타입 | 용도 |
|---|---|---|
| `SheetNumber` | int | 시트 번호 |
| `BaseMemberName` | string | 기준 부재명 |
| `BaseMemberIndex` | int | -1(전체), -3(가공도), 양수(기준) |
| `MemberIndices` | List&lt;int&gt; | 포함 부재 Index 리스트 |
| `MemberNames` | List&lt;string&gt; | 포함 부재명 |
| `MfgDrawingNo` | int | 가공도 번호 |

---

## 관련 문서
- [Form1 공유 필드](./form1-bom.md#주요-공유-필드-form1-멤버)
- [BOM 수집 흐름](../features/bom/collect-bom.md)
- [Clash 완료 콜백](../features/clash/clash-finished-event.md)
- [시트 분할 알고리즘](../features/drawing-sheets/generate-sheets.md)
