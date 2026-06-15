---
feature_id: DIM-003
feature_name: X축 뷰 + 치수 표시
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-06-15
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

내부 흐름은 [공용 ApplyGlobalView](../글로벌뷰/_인덱스.md#공통-동작-요약) 참고.

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

Hole, SlotHole, EarthBoss 형상 풍선은 가공도 전용이므로 X축 뷰에는 표시하지 않는다.

## 8. 후행 기능 (Chained)
- [Y축 뷰](./Y축 치수 표시.md), [Z축 뷰](./Z축 치수 표시.md), [ISO 뷰](./ISO 풍선 표시.md)

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L206](../../code-reference/form1-dimensions.md#btnShowAxisX_Click)
- 공용 함수: [ApplyGlobalView](../../code-reference/form1-global-views.md#ApplyGlobalView)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
| 2026-05-11 | **T-040v: Level 1 치수 offset i%2 토글** (사용자 요청). 같은 축 내 측정축 좌표 순 정렬 후 짝수 i=`level1Offset(100mm)`, 홀수 i=`level1Offset*0.5(50mm)`. 인접 치수 텍스트를 두 라인에 분산해 짧은 치수 숫자 충돌 회피. Y/Z 뷰도 동일 (`ShowAllDimensions` 공유) | Claude |
| 2026-05-11 | **T-040v 토글 취소** (사용자 결정: *"수치는 부재간의 연쇄치수가 첫번째, 전체 치수가 두번째로 2줄만 생성되어야 한다"*). Level 1 foreach를 원래 단순 형태로 복원 (모든 dim에 `level1Offset` 단일 적용). level2 적응형 충돌 회피(`ApplySmartFiltering`이 텍스트 폭 초과 시 자동 밀어내기)는 그대로 유지 — 별도 결정 시 폐기 가능 | Claude |
| 2026-05-11 | **치수 텍스트 위치 13mm 임계 토글** (사용자 결정). `AlignDistanceTextPosition` 글로벌 옵션이라 측정 추가 직전에 `dim.Distance` 검사 후 SetStyle 동적 토글. `≤13mm → 2(보조선 바깥)`, `>13mm → 1(위)`. Level 1/2/0 세 그룹 모두 동일 적용. T-058에서 모든 치수 일괄 2(바깥) 였던 것을 거리 기반 분기로 변경 | Claude |
| 2026-05-11 | `ApplySmartFiltering` 진단 DiagLog 추가 (axis별 level0/level1/total/hidden 카운트, `result.AddRange` 직전). 실제 분리 발생 여부 검증용 | Claude |
| 2026-06-15 | 관련: T-044, T-047 — Hole, SlotHole, EarthBoss 형상 풍선을 가공도 전용으로 변경 | Codex |
