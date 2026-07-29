# Form1.Attribute.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.Attribute.cs` (약 618 라인)

**책임**: 3D 선택 이벤트 처리, 속성 테이블(dgvAttributes) 구성, UDA CRUD, CSV 가져오기/내보내기.

---

## 주요 핸들러 · 이벤트

| 핸들러 | 라인 | 흐름 문서 |
|---|---|---|
| <a id="Object3D_OnObject3DSelected"></a>`Object3D_OnObject3DSelected` | L19 | [object-selected-event](../기능/부재속성/객체%20선택%20이벤트.md) |
| <a id="btnClearSelection_Click"></a>`btnClearSelection_Click` | L248 | [clear-selection](../기능/부재속성/선택%20해제.md) |
| <a id="btnExportAttributeCSV_Click"></a>`btnExportAttributeCSV_Click` | L257 | [export-csv](../기능/부재속성/CSV%20내보내기.md) |
| <a id="btnUdaAdd_Click"></a>`btnUdaAdd_Click` | L364 | [uda-add](../기능/부재속성/UDA%20추가.md) |
| <a id="btnUdaEdit_Click"></a>`btnUdaEdit_Click` | L390 | [uda-edit](../기능/부재속성/UDA%20편집.md) |
| <a id="btnUdaDelete_Click"></a>`btnUdaDelete_Click` | L443 | [uda-delete](../기능/부재속성/UDA%20삭제.md) |
| <a id="btnUdaImportCSV_Click"></a>`btnUdaImportCSV_Click` | L485 | [uda-import-csv](../기능/부재속성/CSV%20가져오기.md) |

---

## 내부 헬퍼

| 메서드 | 라인 | 역할 |
|---|---|---|
| <a id="UpdateAttributeTable"></a>`UpdateAttributeTable(nodeIdx)` | L44 | 4개 섹션 순차 추가 |
| `AddBasicNodeInfo` | L71 | Index/Name/Kind/ParentPath |
| `AddBoundingBoxInfo` | L101 | MinXYZ/MaxXYZ/SizeXYZ/Center |
| `AddUDAInfo` | L139 | UDA.Keys 순회 + FromIndex로 값 조회 |
| `AddGeometryPropertyInfo` | L180 | 리플렉션으로 GeometryProperty 순회 |
| `AddSectionHeader` | L227 | 회색 배경 + 볼드 섹션 헤더 |
| `ClearAttributeTable` | L238 | 행 Clear + 라벨 리셋 + index=-1 |
| `ShowUdaInputDialog` | L305 | Key/Value 입력 Form 생성 (Size 350x180) |
| `IsUdaRow(rowIdx)` | L344 | 위로 탐색하여 UDA 섹션 헤더 찾기 |
| <a id="ParseCsvLine"></a>`ParseCsvLine(line)` | L589 | 따옴표 상태 관리 + 쉼표 분리 |

---

## 섹션 스타일

| 섹션 | 표시 형식 |
|---|---|
| "━━ 기본 정보 ━━" | 배경 `RGB(230,230,230)`, Bold |
| "━━ 바운딩 박스 (Bounding Box) ━━" | 동일 |
| "━━ 사용자 정의 속성 (UDA) ━━" | 동일 |
| "━━ 지오메트리 속성 (Geometry) ━━" | 동일 |

---

## CSV 내보내기 포맷

```
No,Key,Value
1,Node Index,123
2,Node Name,Beam_001
...
```

- 인코딩: UTF-8 (BOM 없이)
- 쉼표 이스케이프: `"value"` 형식

---

## CSV 가져오기 포맷

```
Key,Value          ← 첫 줄이 "key"/"value"/"속성" 포함 시 헤더로 자동 감지
SPREF,HSB:100x100
MATREF,S275JR
```

- 인코딩: UTF-8
- Add 실패 시 Update Fallback (이미 존재하는 키)
- 오류는 최대 10개까지 MessageBox에 표시

---

## VIZCore3D API 사용

- `vizcore3d.Object3D.FromFilter(Object3dFilter.SELECTED_TOP)` → `List<Node>`
- `vizcore3d.Object3D.FromIndex(idx)` → `Node`
- `vizcore3d.Object3D.GetBoundBox(indices, useLocal)`
- `vizcore3d.Object3D.UDA.Keys`, `FromIndex(idx, key)`, `Add/Update/UpdateKey/Delete(idx, key, [value], true)`
- `vizcore3d.Object3D.GeometryProperty.FromIndex(idx)`
- `vizcore3d.Object3D.Select(List<int>, exclusive, zoom)`

---

## 관련 문서
- 흐름 문서: [기능/부재속성/](../기능/부재속성/_인덱스.md)
- 용어집: [UDA](../_glossary.md#uda-user-defined-attribute)
