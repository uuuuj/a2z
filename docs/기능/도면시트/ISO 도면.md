---
feature_id: SHT-003
feature_name: 시트 ISO 뷰 + 풍선 노트
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-07-21
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingISO_Click
---

# 시트 ISO 뷰 + 풍선 노트

## 1. 개요
선택된 도면 시트를 ISO 방향으로 보여주고 ISO 전용 풍선 노트(`CreateIsoBalloonNotes`)를 생성한다. 설치도는 선택 STRU와 직접 연결된 외부 Assembly 전체를 함께 fit하고, 준비된 접합 영역 치수를 적용한다.

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
2. X-Ray 활성화 + 선택 STRU 부재 Select
3. 표시 대상 구성: 일반=`MemberIndices`, 설치도=`MemberIndices + InstallationContextIndices`
4. 표시 대상 Show + `xraySelectedNodeIndices` 갱신 + 전체 대상 fit
5. 심볼 제거
6. 설치도이면 `ExtractInstallationDimensions(sheet)`로 준비 치수 적용
7. `SetRenderMode(SMOOTH)`
8. `MoveCamera(ISO_PLUS)` + 표시 대상 전체 `FlyToObject3d`
9. 풍선 Clear → `CreateIsoBalloonNotes(members)`

## 5. 주요 분기 처리
| 조건 | 처리 |
|---|---|
| 일반/제작/조립 시트 | `MemberIndices`만 표시하고 기존 풍선 생성 |
| 설치도 | 선택 STRU와 직접 연결된 외부 Assembly 전체를 표시하고 접합 영역 준비 치수 적용. 풍선 번호는 선택 STRU 부재 기준 |

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
| `xraySelectedNodeIndices` | 이전 | 실제 표시 대상(설치도는 STRU+외부 연결 Assembly) |
| `chainDimensionList` | 이전 | 설치도일 때 접합 영역·Assembly Osnap 치수 적용 |
| `Review.Note` | 이전 | 풍선 노트 |
| RenderMode | 이전 | SMOOTH |

## 8. 후행 기능 (Chained)
- [시트 2D 생성](./시트 2D 렌더.md)
- 다른 축 뷰로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs](../../code-reference/form1-drawing-sheets.md#btnDrawingISO_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-07-21 | 설치도 ISO 미리보기 대상을 선택 STRU+직접 연결 외부 Assembly 전체로 확장하고, 실제 접합 영역·Osnap 기반 준비 치수를 적용하도록 변경 | Codex |
| 2026-04-13 | 초안 작성 | — |
