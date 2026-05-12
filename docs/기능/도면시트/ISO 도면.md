---
feature_id: SHT-003
feature_name: 시트 ISO 뷰 + 풍선 노트
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingISO_Click
---

# 시트 ISO 뷰 + 풍선 노트

## 1. 개요
선택된 도면 시트를 ISO 방향으로 보여주고, 설치도 치수 추출 + ISO 전용 풍선 노트(`CreateIsoBalloonNotes`)를 자동 생성한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnDrawingISO` 클릭 |
| 위치 | 메인 폼 > 도면 시트 탭 |

## 3. 사전 조건
- [ ] 도면 시트 선택됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 호출 | Form1 | `ApplyDrawingSheetView("ISO")` |

### ApplyDrawingSheetView("ISO") 내부
1. 시트 선택 확인 → [E01]
2. X-Ray 활성화 + 시트 부재 Select
3. `xraySelectedNodeIndices = MemberIndices`
4. `FlyToObject3d(members, 1.2f)`
5. 심볼 제거
6. `ExtractInstallationDimensions(members)` — 설치도 치수
7. `SetRenderMode(DASH_LINE)`
8. `MoveCamera(ISO_PLUS)` + `FlyToObject3d(members, 1.0f)`
9. 풍선 Clear → `CreateIsoBalloonNotes(members)`

## 5. 주요 분기 처리
없음 (ISO 전용 경로).

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 시트 미선택 | return | MessageBox "도면 시트를 선택해주세요." | 변화 없음 |
| E02 | 예외 | catch | MessageBox "도면 시트 뷰 표시 중 오류: {msg}" | 부분 표시 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `ISO_PLUS` |
| `XRay.Enable` | 이전 | true |
| `xraySelectedNodeIndices` | 이전 | `MemberIndices` |
| `chainDimensionList` | 이전 | 설치도 치수로 재계산 |
| `Review.Note` | 이전 | 풍선 노트 |
| RenderMode | 이전 | DASH_LINE |

## 8. 후행 기능 (Chained)
- [시트 2D 생성](./시트 2D 렌더.md)
- 다른 축 뷰로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs:L755](../../code-reference/form1-drawing-sheets.md#btnDrawingISO_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
