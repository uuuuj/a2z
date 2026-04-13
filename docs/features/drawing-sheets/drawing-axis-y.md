---
feature_id: SHT-005
feature_name: 시트 Y축 뷰 + 치수
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisY_Click
---

# 시트 Y축 뷰 + 치수

## 1. 개요
선택 시트를 Y축 방향으로 표시. 공용 `ApplyDrawingSheetView("Y")` 호출. 세부 동작은 [X축 뷰](./drawing-axis-x.md)와 동일 (축만 Y).

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnDrawingAxisY` 클릭 |
| 위치 | 메인 폼 > 도면 시트 탭 |

## 3. 사전 조건
- [ ] 시트 선택됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 호출 | Form1 | `ApplyDrawingSheetView("Y")` |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 시트 미선택 | return | MessageBox | 변화 없음 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `Y_PLUS` |
| Measure | 이전 | Y축 치수만 |

## 8. 후행 기능 (Chained)
- 다른 축/ISO로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs:L765](/docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisY_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
