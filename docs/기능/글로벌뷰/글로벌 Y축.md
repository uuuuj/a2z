---
feature_id: GV-003
feature_name: 글로벌 Y축 뷰
category: GlobalViews
trigger_type: User Action
owner_module: Form1.GlobalViews.cs
last_updated: 2026-08-21
code_reference: /docs/code-reference/form1-global-views.md#btnGlobalAxisY_Click
---

# 글로벌 Y축 뷰

## 1. 개요
공용 함수 `ApplyGlobalView("Y")` 호출. 동작은 [X축 뷰](./글로벌%20X축.md)와 동일하되 축만 Y.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnGlobalAxisY` 클릭 |
| 위치 | 메인 툴바 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 호출 | Form1 | `ApplyGlobalView("Y")` |

## 5. 주요 분기 처리
[글로벌 ISO 뷰 §5](./글로벌%20ISO.md#5-주요-분기-처리)와 동일.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch | MessageBox "뷰 전환 중 오류" | 부분 전환 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `Y_MINUS` (카메라 −Y, 시선 +Y) |
| Measure | 이전 | Y축 치수 |

## 8. 후행 기능 (Chained)
- 다른 축/ISO

## 9. 관련 링크
- 코드 구현: [Form1.GlobalViews.cs:L33](../../code-reference/form1-global-views.md#btnGlobalAxisY_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-08-21 | 정면도 사양에 맞춰 Y뷰 카메라를 `Y_MINUS`로 정정 | Codex |
| 2026-04-13 | 초안 작성 | — |
