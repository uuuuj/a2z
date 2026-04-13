# Attribute 기능

`Form1.Attribute.cs` 소속. 3D 노드 선택 시 속성 패널 갱신 및 UDA CRUD 기능입니다.

## 기능 목록
| ID | 기능 | 트리거 | 유형 | 문서 |
|---|---|---|---|---|
| ATR-001 | 3D 객체 선택 이벤트 | Object3D_OnObject3DSelected | Event Callback | [object-selected-event](./object-selected-event.md) |
| ATR-002 | 선택 해제 | btnClearSelection_Click | User Action | [clear-selection](./clear-selection.md) |
| ATR-003 | 속성 CSV 내보내기 | btnExportAttributeCSV_Click | User Action | [export-csv](./export-csv.md) |
| ATR-004 | UDA 추가 | btnUdaAdd_Click | User Action | [uda-add](./uda-add.md) |
| ATR-005 | UDA 수정 | btnUdaEdit_Click | User Action | [uda-edit](./uda-edit.md) |
| ATR-006 | UDA 삭제 | btnUdaDelete_Click | User Action | [uda-delete](./uda-delete.md) |
| ATR-007 | UDA CSV 일괄 가져오기 | btnUdaImportCSV_Click | User Action | [uda-import-csv](./uda-import-csv.md) |

## 속성 패널 구성 요소
- 기본 노드 정보 (Index, Name, Parent)
- 바운딩박스 (Min/Max/Center)
- UDA 목록 (키/값)
- 지오메트리 속성 (Volume, Area 등)
