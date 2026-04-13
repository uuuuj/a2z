---
feature_id: DRW2D-004
feature_name: Clash 더블클릭 → 간섭 부재 표시
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#LvClash_DoubleClick
---

# Clash 더블클릭 → 간섭 부재 표시

## 1. 개요
`lvClash`에서 간섭 행을 더블클릭하면 해당 쌍(Index1, Index2)을 동시 선택하고 화면을 맞춘다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `lvClash` 더블클릭 |
| 위치 | 메인 폼 > Clash 리스트 |

## 3. 사전 조건
- [ ] Clash 결과 채워짐
- [ ] `lvClash` 선택 있음

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | 없으면 return |
| 2 | 색상 복원 | SDK | `Color.RestoreColorAll()` |
| 3 | 두 노드 선택 | SDK | `Select([Index1, Index2], true, true)` |
| 4 | 카메라 이동 | SDK | `FlyToObject3d(indices, 1.2f)` |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch | MessageBox "Clash 표시 중 오류: {msg}" | 부분 표시 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK Color | 이전 | 전체 복원 |
| SDK Selection | 이전 | Index1, Index2만 |
| 카메라 | 이전 | 간섭 쌍 확대 |

## 8. 후행 기능 (Chained)
- [Clash 부재 강조 표시](./clash-show-selected.md) (수동 버튼)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L151](/docs/code-reference/form1-drawing2d.md#LvClash_DoubleClick)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
