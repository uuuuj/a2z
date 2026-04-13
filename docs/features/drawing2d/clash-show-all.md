---
feature_id: DRW2D-007
feature_name: 전체 보기 복원
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnClashShowAll_Click
---

# 전체 보기 복원

## 1. 개요
X-Ray 모드·가공도 모드·색상 변경·Clash 심볼 등 모든 강조 상태를 **초기화하고 전체 뷰로 복원**한다. 모든 치수도 다시 표시한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnClashShowAll` 클릭 |
| 위치 | 메인 폼 > Clash 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | BeginUpdate | SDK | 일괄 렌더 |
| 2 | 부재 복원 | Form1 | `RestoreAllPartsVisibility()` |
| 3 | X-Ray 해제 | SDK | `XRay.Enable = false` |
| 4 | xray 리스트 Clear | Form1 | `xraySelectedNodeIndices.Clear()` |
| 5 | 색상 복원 | SDK | `Color.RestoreColorAll()` |
| 6 | Clash 심볼 제거 | SDK | `Clash.ClearResultSymbol()` |
| 7 | 실루엣 엣지 복원 | SDK | Green |
| 8 | 전체 화면 맞춤 | SDK | `FitToView()` |
| 9 | EndUpdate | SDK | 렌더 재개 |
| 10 | 모든 치수 다시 표시 | Form1 | `ShowAllDimensions()` |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch | MessageBox "전체 보기 중 오류: {msg}" | 부분 복원 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `XRay.Enable` | true/false | false |
| `xraySelectedNodeIndices` | 이전 | 비어있음 |
| 부재 가시성 | 일부 숨김 | 전체 표시 |
| Clash 심볼 | 있음 | 없음 |
| 치수 | 필터됨 | 전체 표시 |
| 카메라 | 이전 | 전체 Fit |

## 8. 후행 기능 (Chained)
- 다른 축 뷰, ISO 뷰로 자유 전환 가능

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L651](/docs/code-reference/form1-drawing2d.md#btnClashShowAll_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
