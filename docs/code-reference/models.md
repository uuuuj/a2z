# Models.cs — 코드 레퍼런스

**경로**: `A2Z/Models.cs`

**책임**: 앱 전반에서 사용되는 도메인 데이터 모델 정의.

---

## 데이터 클래스

### ChainDimensionData (L9~L56)
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

### BOMData (L61~L89)
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

### HoleInfo (L94~L102)
원형 홀.

| 필드 | 타입 |
|---|---|
| `Diameter` | float |
| `CenterX/Y/Z` | float |
| `CylinderBodyIndex` | int |

### SlotHoleInfo (L106~L114)
슬롯(장공) 홀.

| 필드 | 타입 |
|---|---|
| `Radius` | float |
| `SlotLength` | float |
| `Depth` | float |
| `CenterX/Y/Z` | float |

### ClashData (L119~L126)
간섭 쌍.

| 필드 | 타입 | 용도 |
|---|---|---|
| `Index1`, `Index2` | int | 간섭 부재 Index |
| `Name1`, `Name2` | string | 간섭 부재명 |
| `ZValue` | float | HotPoint Z (정렬 기준) |

### DrawingSheetData (L165~L197)
도면 시트.

| 필드 | 타입 | 용도 |
|---|---|---|
| `SheetNumber` | int | 시트 번호 |
| `BaseMemberName` | string | 기준 부재명 |
| `BaseMemberIndex` | int | -1(전체), -3(가공도), 양수(기준) |
| `MemberIndices` | List&lt;int&gt; | 포함 부재 Index 리스트 |
| `MemberNames` | List&lt;string&gt; | 포함 부재명 |
| `MfgDrawingNo` | int | 가공도 번호 |
| `PaintCode` | string | 같은 STRU에서 생성된 제작도·조립도·설치도·가공도가 공유하는 PNT 계열 UDA 값. `null`은 미조회, 빈 문자열은 조회 결과 없음 |
| `PreparedDimensions` / `DimensionsPrepared` | List / bool | 목록 표시 전에 준비한 시트 치수와 준비 상태 |
| `PreparedBomRows` / `BomPrepared` | List / bool | 목록 표시 전에 준비한 BOM 행과 준비 상태 |
| `PreparedBomNodeGroupMap` | Dictionary | 시트 기준 Body→BOM 그룹 번호 |

### DrawingBomRowData (L219~L235)
도면정보 탭 BOM 한 행의 문자열 데이터. `ListViewItem`을 직접 보관하지 않고 시트 전환 때 안전하게 UI 행을 생성한다.

### RevisionEntry (L236~L249)
표제부 REV 이력 표 한 행. 6칸이 그대로 필드가 된다.

| 필드 | 타입 | 용도 |
|---|---|---|
| `Rev` | string | REV. 번호 (현재는 `0` 고정) |
| `Date` | string | 출력일 `yyyy-mm-dd` |
| `Description` | string | 변경 사유 (기본 문구 미정 → 현재 공백) |
| `Drawn` / `Checked` / `Approved` | string | 작성자·검도자·승인자 (입력 수단 미정 → 현재 공백) |

빈 값은 `FillRevisionTable`이 `" "`(공백 1칸)로 바꿔 그 칸 괘선을 남긴다. 자세한 슬롯·괘선 규칙은 [FillRevisionTable](./form1-drawing-sheets.md#FillRevisionTable) 참고.

---

## 관련 문서
- [Form1 공유 필드](./form1-bom.md#주요-공유-필드-form1-멤버)
- [BOM 수집 흐름](../기능/BOM/BOM%20수집.md)
- [Clash 완료 콜백](../기능/간섭검사/간섭검사%20완료%20이벤트.md)
- [시트 분할 알고리즘](../기능/도면시트/시트%20자동%20생성.md)
