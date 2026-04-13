---
feature_id: DRW2D-012
feature_name: Osnap 풍선·구 마커 전체 삭제
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnOsnapClearBalloon_Click
---

# Osnap 풍선·구 마커 전체 삭제

## 1. 개요
`Review.Note`와 `ShapeDrawing`을 모두 Clear하여 Osnap 풍선과 구 마커를 한번에 제거한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnOsnapClearBalloon` 클릭 |
| 위치 | 메인 폼 > Osnap 탭 |

## 3. 사전 조건
없음 (빈 상태에서도 호출 가능).

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | Note Clear | SDK | `Review.Note.Clear()` |
| 2 | ShapeDrawing Clear | SDK | 구 마커 제거 |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch(무시) | 없음 | 조용히 실패 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `Review.Note` | N개 풍선 | 비어있음 |
| `ShapeDrawing` | 구 마커 등 | 비어있음 |

## 8. 후행 기능 (Chained)
- 필요 시 재표시 [`btnOsnapShowSelected`](./osnap-show-selected.md)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L913](/docs/code-reference/form1-drawing2d.md#btnOsnapClearBalloon_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
