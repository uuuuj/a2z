---
feature_id: DIM-006
feature_name: ISO 뷰 + 풍선 표시
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-dimensions.md#btnShowISO_Click
---

# ISO 뷰 + 풍선 표시

## 1. 개요
ISO(등각 투영) 뷰로 전환한다. ISO 뷰에서는 치수가 아닌 **풍선 노트**가 표시된다(`CreateIsoBalloonNotes` 호출).

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnShowISO` 클릭 |
| 위치 | 메인 폼 > 치수 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 함수 호출 | Form1 | `ApplyGlobalView("ISO")` |
| 2 | 내부: 풍선 생성 | Form1 | `CreateIsoBalloonNotes(indices)` (ISO일 때만) |

## 5. 주요 분기 처리
[X축 뷰](./X축%20치수%20표시.md)와 동일하되, "ISO"인 경우 풍선 생성 경로로 분기.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 공용 함수 예외 | catch | MessageBox "뷰 전환 중 오류" | 부분 전환 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `CameraDirection.ISO_PLUS` |
| RenderMode | 이전 | DASH_LINE |
| Measure | 이전 | Clear (치수 미표시) |
| Note | 이전 | 풍선 노트 생성 |
| `currentBalloonView` | 이전 | "ISO" |

## 8. 후행 기능 (Chained)
- [풍선 위치 조정](./풍선%20위치%20조정.md)

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L230](../../code-reference/form1-dimensions.md#btnShowISO_Click)
- 용어집: [풍선](../../_glossary.md#풍선-balloon-note)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
