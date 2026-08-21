---
feature_id: DIM-004
feature_name: Y축 뷰 + 치수 표시
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-08-21
code_reference: /docs/code-reference/form1-dimensions.md#btnShowAxisY_Click
---

# Y축 뷰 + 치수 표시

## 1. 개요
Y축 방향 뷰로 전환. 공용 함수 `ApplyGlobalView("Y")` 호출.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnShowAxisY` 클릭 |
| 위치 | 메인 폼 > 치수 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 함수 호출 | Form1 | `ApplyGlobalView("Y")` |

내부 흐름은 [X축 뷰 문서](./X축%20치수%20표시.md) 및 [공용 ApplyGlobalView](../글로벌뷰/_인덱스.md) 참고.

## 5. 주요 분기 처리
[X축 뷰](./X축%20치수%20표시.md)와 동일 (축만 다름).

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 공용 함수 예외 | catch | MessageBox "뷰 전환 중 오류" | 부분 전환 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `CameraDirection.Y_MINUS` (카메라 −Y, 시선 +Y) |
| RenderMode | 이전 | DASH_LINE |
| Measure/Note/ShapeDrawing | 이전 | Clear 후 Y축 치수 |

Hole, SlotHole, EarthBoss 형상 풍선은 가공도 전용이므로 Y축 뷰에는 표시하지 않는다.

## 8. 후행 기능 (Chained)
- 다른 축/ISO 뷰

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L214](../../code-reference/form1-dimensions.md#btnShowAxisY_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-08-21 | 정면도 카메라를 `Y_MINUS`로 바꾸고 화면 right=+X 기준으로 치수 배치 부호를 통일 | Codex |
| 2026-04-13 | 초안 작성 | — |
| 2026-06-15 | 관련: T-044, T-047 — Hole, SlotHole, EarthBoss 형상 풍선을 가공도 전용으로 변경 | Codex |
