---
feature_id: DIM-005
feature_name: Z축 뷰 + 치수 표시
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-dimensions.md#btnShowAxisZ_Click
---

# Z축 뷰 + 치수 표시

## 1. 개요
Z축 방향 뷰로 전환. 공용 함수 `ApplyGlobalView("Z")` 호출.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnShowAxisZ` 클릭 |
| 위치 | 메인 폼 > 치수 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 함수 호출 | Form1 | `ApplyGlobalView("Z")` |

## 5. 주요 분기 처리
[X축 뷰](./show-axis-x.md)와 동일.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 공용 함수 예외 | catch | MessageBox "뷰 전환 중 오류" | 부분 전환 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `CameraDirection.Z_PLUS` |
| RenderMode | 이전 | DASH_LINE |
| Measure/Note/ShapeDrawing | 이전 | Clear 후 Z축 치수 |

## 8. 후행 기능 (Chained)
- 다른 축/ISO 뷰

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L222](/docs/code-reference/form1-dimensions.md#btnShowAxisZ_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
