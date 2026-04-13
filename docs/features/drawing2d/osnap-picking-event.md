---
feature_id: DRW2D-009
feature_name: Osnap 픽킹 이벤트
category: Drawing2D
trigger_type: Event Callback
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#GeometryUtility_OnOsnapPickingItem
---

# Osnap 픽킹 이벤트

## 1. 개요
[Osnap 수동 추가](./osnap-add.md) 모드에서 사용자가 뷰어를 클릭할 때마다 호출된다. 클릭된 좌표를 `osnapPoints`·`osnapPointsWithNames`·`lvOsnap`에 추가한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | Event Callback |
| 입력 | `vizcore3d.GeometryUtility.OnOsnapPickingItem` |
| 위치 | Osnap 추가 모드 활성화 후 |

## 3. 사전 조건
- [ ] `btnOsnapAdd_Click`으로 이벤트 구독 및 모드 활성화됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | Point 유효성 검사 | Form1 | `e.Point == null` → return |
| 2 | Vertex 생성 | Form1 | `Vertex3D(e.Point.X/Y/Z)` |
| 3 | 노드명 추출 | Form1 | 선택된 SELECTED_TOP 노드 있으면 사용, 없으면 "수동 추가" |
| 4 | 리스트 추가 | Form1 | `osnapPoints`, `osnapPointsWithNames` |
| 5 | ListView 추가 | UI | #/노드명/X/Y/Z/HoleSize/SlotHoleSize |
| 6 | 홀 매칭 | Form1 | `GetHoleOrSlotForPoint(bom, x, y, z)` |

## 5. 주요 분기 처리

### [분기 A] 노드명 결정
| 조건 | 처리 |
|---|---|
| `SELECTED_TOP` 노드 존재 | `selectedNodes[0].NodeName` |
| 없음 | "수동 추가" |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch | MessageBox "Osnap 좌표 추가 중 오류: {msg}" | 해당 클릭 무시 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `osnapPoints` | N개 | N+1개 |
| `osnapPointsWithNames` | N개 | N+1개 (노드명 포함) |
| `lvOsnap` | N행 | N+1행 |

## 8. 후행 기능 (Chained)
- [Osnap 풍선 표시](./osnap-show-selected.md)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L716](/docs/code-reference/form1-drawing2d.md#GeometryUtility_OnOsnapPickingItem)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
