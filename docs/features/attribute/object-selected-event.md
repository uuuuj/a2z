---
feature_id: ATR-001
feature_name: 3D 객체 선택 이벤트
category: Attribute
trigger_type: Event Callback
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#Object3D_OnObject3DSelected
---

# 3D 객체 선택 이벤트

## 1. 개요
3D 뷰어에서 객체가 선택되면 호출되어, 첫 번째 선택 노드의 **기본 정보/바운딩박스/UDA/지오메트리 속성**을 `dgvAttributes`에 자동 표시한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | Event Callback |
| 입력 | `vizcore3d.Object3D.OnObject3DSelected` |
| 위치 | 앱 초기화 시 구독 ([BOM-001](../bom/vizcore3d-initialized.md)) |

## 3. 사전 조건
- [ ] VIZCore3D 초기화 완료

## 4. 전체 동작 흐름

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 노드 획득 | SDK | `FromFilter(Object3dFilter.SELECTED_TOP)` |
| 2 | 빈 선택 처리 | Form1 | `selectedNodes == null/0` → `ClearAttributeTable()` |
| 3 | 첫 노드 저장 | Form1 | `selectedAttributeNodeIndex = node.Index` |
| 4 | 라벨 갱신 | UI | `lblSelectedNode.Text = "[Index] NodeName"` |
| 5 | 속성 테이블 갱신 | Form1 | `UpdateAttributeTable(node.Index)` |

### UpdateAttributeTable 내부 순서
1. `AddBasicNodeInfo` — Index, Name, Kind, ParentPath
2. `AddBoundingBoxInfo` — Min/Max XYZ, Size, Center
3. `AddUDAInfo` — UDA 키 순회, 값 존재만 표시
4. `AddGeometryPropertyInfo` — 리플렉션으로 GeometryProperty 순회

## 5. 주요 분기 처리

### [분기 A] 선택 존재 여부
| 조건 | 처리 |
|---|---|
| 있음 | 첫 노드만 표시 (다중 선택 시에도) |
| 없음 | 테이블 Clear, 라벨 초기화 |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | UpdateAttributeTable 내부 예외 | catch + Debug.WriteLine | 없음 | 부분 표시 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `selectedAttributeNodeIndex` | 이전 | 첫 선택 노드 Index (없으면 -1) |
| `lblSelectedNode.Text` | 이전 | "[Index] Name" 또는 "3D 뷰어에서 부재를 선택하세요" |
| `dgvAttributes` | 이전 | 4개 섹션(기본/BBox/UDA/Geometry) 또는 빈 상태 |

## 8. 후행 기능 (Chained)
- [UDA 추가](./uda-add.md) / [편집](./uda-edit.md) / [삭제](./uda-delete.md)
- [CSV 내보내기](./export-csv.md)

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L19](/docs/code-reference/form1-attribute.md#Object3D_OnObject3DSelected)
- 용어집: [UDA](../../_glossary.md#uda-user-defined-attribute)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
