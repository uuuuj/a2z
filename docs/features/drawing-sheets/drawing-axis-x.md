---
feature_id: SHT-004
feature_name: 시트 X축 뷰 + 치수
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisX_Click
---

# 시트 X축 뷰 + 치수

## 1. 개요
선택 시트를 X축 방향으로 보여주고 치수를 표시한다. 공용 `ApplyDrawingSheetView("X")` 호출.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnDrawingAxisX` 클릭 |
| 위치 | 메인 폼 > 도면 시트 탭 |

## 3. 사전 조건
- [ ] 시트 선택됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 호출 | Form1 | `ApplyDrawingSheetView("X")` |

### 내부 흐름 (X/Y/Z 공통)
1. X-Ray 유지 + 시트 부재 Select
2. 심볼·풍선·측정 Clear
3. `SetRenderMode(DASH_LINE)`
4. `MoveCamera(X_PLUS)`
5. `FlyToObject3d(members, 1.0f)`
6. `ShowAllDimensions("X")` — X축 치수만 표시

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 시트 미선택 | return | MessageBox "도면 시트를 선택해주세요." | 변화 없음 |
| E02 | 예외 | catch | MessageBox | 부분 표시 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `X_PLUS` |
| `XRay.Enable` | 이전 | true |
| Measure/Note/ShapeDrawing | 이전 | Clear 후 X축 치수 |

## 8. 후행 기능 (Chained)
- Y/Z/ISO 뷰로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs:L760](/docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisX_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
