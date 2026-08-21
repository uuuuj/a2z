---
feature_id: SHT-005
feature_name: 시트 Y축 뷰 + 치수
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-08-21
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisY_Click
---

# 시트 Y축 뷰 + 치수

## 1. 개요
선택 시트를 Y축 방향(XZ 평면)으로 표시한다. 설치도는 선택 STRU+직접 연결 외부 Part를 표시하고 Target Body 길이축이 Z 또는 X일 때 가까운 끝단→Connected Body 접합측 모서리 치수만 표시한다. A/A1 접합점 기호와 선택 STRU 전체 범위 치수는 표시하지 않는다. PDF는 선택 STRU 기준으로 맞추고 연결 Part만 점선으로 남기며, 최종 2D 객체의 실제 배율로 보조선 종이 길이를 통일한다. 공통 가시성·치수 흐름은 [X축 뷰](./X축%20도면.md)와 동일하다.

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
설치도이면 표시 대상을 `MemberIndices + InstallationContextIndices`로 확장하고 Z/X 설치 치수를 사용한다.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 시트 미선택 | return | MessageBox | 변화 없음 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `Y_MINUS` (카메라 −Y, 시선 +Y) |
| Measure | 이전 | Y축 치수만 |

## 8. 후행 기능 (Chained)
- 다른 축/ISO로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs:L1123](../../code-reference/form1-drawing-sheets.md#btnDrawingAxisY_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-08-21 | 3D 미리보기와 2D 출력의 정면도 카메라를 `Y_MINUS`로 통일하고 Y뷰 전용 수평 부호 보정을 제거 | Codex |
| 2026-07-23 | 설치도 전체 범위 치수를 제거하고 연결 거리만 표시. 2D 출력은 최종 실측 배율로 보조선 종이 길이를 통일 | Codex |
| 2026-07-22 | 설치도 접합점 체인을 실제 Target Body 가까운 끝단→Connected Body 접합측 모서리 치수로 교체하고 A/A1 기호 제거 | Codex |
| 2026-07-22 | 설치도 연결 Assembly 전체 대신 직접 연결 Part만 표시하고, 선택 STRU 기준 fit·Crop 및 STRU/Part 접합 치수로 변경 | Codex |
| 2026-07-22 | 3D 미리보기와 2D 도면의 치수 원본·뷰 필터가 동일함을 명시 | Codex |
| 2026-07-21 | 설치도 Y뷰에 외부 연결 Assembly 전체와 Z/X 접합 위치 치수 기준 반영 | Codex |
| 2026-04-13 | 초안 작성 | — |
