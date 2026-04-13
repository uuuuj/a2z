---
feature_id: DIM-003
feature_name: X축 뷰 + 치수 표시
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-dimensions.md#btnShowAxisX_Click
---

# X축 뷰 + 치수 표시

## 1. 개요
X축 방향 뷰로 카메라를 전환하고, X축 치수만 표시한다. 내부적으로 공용 함수 `ApplyGlobalView("X")` 호출 — `btnGlobalAxisX`와 동일 동작.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnShowAxisX` 버튼 클릭 |
| 위치 | 메인 폼 > 치수 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름 (Happy Path)
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 함수 호출 | Form1 | `ApplyGlobalView("X")` |

내부 흐름은 [공용 ApplyGlobalView](../global-views/_index.md#공통-동작-요약) 참고.

## 5. 주요 분기 처리
공용 함수 내부 분기 참고:
- 도면시트 선택 상태 → `ApplyDrawingSheetView("X")`
- X-Ray 부재 선택 상태 → `ApplySelectedNodesView("X")`
- 기본 → `ApplyFullModelView("X")`

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 공용 함수 예외 | catch | MessageBox "뷰 전환 중 오류: {msg}" | 부분 전환 가능 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `CameraDirection.X_PLUS` |
| `vizcore3d.View.RenderMode` | 이전 | DASH_LINE |
| `Review.Measure / Note / ShapeDrawing` | 이전 | Clear 후 X축 치수 |

## 8. 후행 기능 (Chained)
- [Y축 뷰](./show-axis-y.md), [Z축 뷰](./show-axis-z.md), [ISO 뷰](./show-iso.md)

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L206](/docs/code-reference/form1-dimensions.md#btnShowAxisX_Click)
- 공용 함수: [ApplyGlobalView](/docs/code-reference/form1-global-views.md#ApplyGlobalView)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
