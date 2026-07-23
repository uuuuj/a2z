---
feature_id: SHT-004
feature_name: 시트 X축 뷰 + 치수
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-07-23
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingAxisX_Click
---

# 시트 X축 뷰 + 치수

## 1. 개요
선택 시트를 X축 방향(YZ 평면)으로 보여주고 치수를 표시한다. 3D 미리보기와 2D 도면은 모두 현재 시트의 `chainDimensionList`를 `ShowAllDimensions("X")`로 필터링하는 동일 경로를 사용한다. 설치도는 선택 STRU와 직접 연결된 외부 Part를 함께 표시하고, 실제 접촉한 Target Body의 길이축이 Z 또는 Y일 때 가까운 끝단→Connected Body 접합측 모서리 위치 치수만 사용한다. PDF 보조선은 최종 2D 객체의 실제 배율로 생성해 다른 직교 뷰와 같은 종이 길이를 유지한다.

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
1. 표시 대상 구성: 일반=`MemberIndices`, 설치도=`MemberIndices + InstallationContextIndices`
2. X-Ray 유지 + 선택 STRU 부재 Select, 표시 대상 Show
3. 심볼·풍선·측정 Clear
4. `SetRenderMode(SMOOTH)`
5. `MoveCamera(X_PLUS)`
6. 일반 시트는 표시 대상 전체, 설치도는 선택 STRU 기준 `FlyToObject3d(..., 1.0f)`
7. `ShowAllDimensions("X")` — Y/Z 축 치수 표시. 설치도는 Target Body 끝단↔Connected Body 모서리 치수만 표시

## 5. 주요 분기 처리
| 조건 | 처리 |
|---|---|
| 설치도 | 선택 STRU+직접 연결 Part 표시, 길이축이 Z/Y인 끝단→모서리 치수. A/A1 접합점 기호 없음. PDF는 선택 STRU 기준 fit·Crop |
| 그 외 | 시트 MemberIndices와 기존 준비 치수 표시 |

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
- 코드 구현: [Form1.DrawingSheets.cs:L1118](../../code-reference/form1-drawing-sheets.md#btnDrawingAxisX_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-07-23 | 설치도 전체 범위 치수를 제거하고 연결 거리만 표시. 2D 출력은 최종 실측 배율로 보조선 종이 길이를 통일 | Codex |
| 2026-07-22 | 설치도 접합점 체인을 실제 Target Body 가까운 끝단→Connected Body 접합측 모서리 치수로 교체하고 A/A1 기호 제거 | Codex |
| 2026-07-22 | 설치도 연결 Assembly 전체 표시·범위 치수를 제거하고 직접 연결 Part 점선, 선택 STRU 기준 fit·Crop 및 STRU/Part 접합 치수로 변경 | Codex |
| 2026-07-22 | X/Y/Z 3D 미리보기와 2D 도면이 같은 `chainDimensionList`·`ShowAllDimensions` 경로를 사용하며 설치도 연결 위치 필수 치수도 포함함을 명시 | Codex |
| 2026-07-21 | 설치도 X뷰를 선택 STRU+외부 연결 Assembly 전체 표시와 Z/Y 접합 위치 치수 기준으로 갱신 | Codex |
| 2026-04-13 | 초안 작성 | — |
