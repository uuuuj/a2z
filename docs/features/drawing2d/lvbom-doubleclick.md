---
feature_id: DRW2D-003
feature_name: BOM 더블클릭 → 부재 포커스
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#LvBOM_DoubleClick
---

# BOM 더블클릭 → 부재 포커스

## 1. 개요
`lvBOM`에서 부재 항목을 더블클릭하면 해당 부재를 선택하고 카메라를 이동시킨다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `lvBOM` 더블클릭 |
| 위치 | 메인 폼 > BOM 리스트 |

## 3. 사전 조건
- [ ] BOM 수집됨
- [ ] `lvBOM` 항목 1개 이상 선택

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | `SelectedItems.Count == 0` → return |
| 2 | BOMData 추출 | Form1 | `Tag as BOMData` → null이면 return |
| 3 | 노드 선택 | SDK | `Object3D.Select([bom.Index], true, true)` (기존 해제) |
| 4 | 카메라 이동 | SDK | `View.FlyToObject3d([bom.Index], 1.2f)` |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 처리 중 예외 | catch | MessageBox "노드 이동 중 오류: {msg}" | 일부 선택 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK Selection | 이전 | 해당 부재만 선택 |
| 카메라 | 이전 | 해당 부재 확대 |

## 8. 후행 기능 (Chained)
- [선택 부재 가공도](../mfg-drawing/mfg-drawing.md)
- [속성 조회](../attribute/object-selected-event.md) — SDK가 자동 호출

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L123](/docs/code-reference/form1-drawing2d.md#LvBOM_DoubleClick)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
