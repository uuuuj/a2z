---
feature_id: DRW2D-011
feature_name: 선택 Osnap 풍선·구 마커 표시
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnOsnapShowSelected_Click
---

# 선택 Osnap 풍선·구 마커 표시

## 1. 개요
`lvOsnap`에서 선택된 Osnap 좌표에 **빨간 구 마커**를 표시하고, 좌표·홀사이즈 정보가 포함된 **풍선 노트**를 추가한다. 첫 선택 좌표로 카메라도 이동한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnOsnapShowSelected` 클릭 |
| 위치 | 메인 폼 > Osnap 탭 |

## 3. 사전 조건
- [ ] `lvOsnap` 선택 1개 이상

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | → [E01] |
| 2 | BeginUpdate | SDK | 일괄 렌더 |
| 3 | 기존 형상 제거 | SDK | `ShapeDrawing.Clear()` |
| 4 | 좌표 수집 | Form1 | 선택 행의 osnapPoints 인덱스로 추출 |
| 5 | 구 마커 추가 | SDK | `AddSphere(points, 0, Red, 5.0f, true)` |
| 6 | 기존 풍선 Clear | SDK | `Review.Note.Clear()` |
| 7 | 풍선 순회 생성 | SDK | 텍스트: 부재명 + 좌표 + 홀사이즈 |
| 8 | 풍선 스타일 | SDK | DarkBlue, FontSize10 Bold, Red Arrow |
| 9 | 카메라 이동 | SDK | 첫 좌표 근처 BBox 부재 있으면 FlyTo, 없으면 Pivot 설정 |
| 10 | EndUpdate | SDK | 렌더 재개 |

## 5. 주요 분기 처리

### [분기 A] 카메라 이동 타겟
| 조건 | 처리 |
|---|---|
| 첫 좌표가 어느 BOMData BBox 내에 있음 | `FlyToObject3d(nearNodeIndices, 1.5f)` |
| 어느 BOM에도 포함 안 됨 | `SetPivotPosition(targetPoint)` |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 선택 없음 | return | MessageBox "Osnap 좌표를 선택해주세요." | 변화 없음 |
| E02 | 예외 | catch | MessageBox "Osnap 좌표 설정 중 오류: {msg}" | 부분 표시 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `vizcore3d.ShapeDrawing` | 이전 | Clear 후 빨간 구 |
| `vizcore3d.Review.Note` | 이전 | Clear 후 풍선 N개 |
| 카메라 | 이전 | 첫 좌표 중심 |

## 8. 후행 기능 (Chained)
- [풍선 전체 삭제](./Osnap%20풍선%20초기화.md)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L807](../../code-reference/form1-drawing2d.md#btnOsnapShowSelected_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
