---
feature_id: DRW2D-010
feature_name: Osnap 좌표 삭제
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnOsnapDelete_Click
---

# Osnap 좌표 삭제

## 1. 개요
`lvOsnap`에서 선택된 Osnap 좌표를 `osnapPoints`·`osnapPointsWithNames`에서 제거하고 번호를 재정렬한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnOsnapDelete` 클릭 |
| 위치 | 메인 폼 > Osnap 탭 |

## 3. 사전 조건
- [ ] `lvOsnap` 선택 1개 이상

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | → [E01] |
| 2 | 인덱스 역순 수집 | Form1 | 리스트 변동 방지 |
| 3 | 데이터·UI 제거 | Form1 | osnapPoints/WithNames/lvOsnap RemoveAt |
| 4 | 번호 재정렬 | UI | 남은 항목 #/1..N |
| 5 | 완료 알림 | UI | MessageBox 삭제 개수 |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 선택 없음 | return | MessageBox "삭제할 Osnap 좌표를 선택해주세요." | 변화 없음 |
| E02 | 예외 | catch | MessageBox "Osnap 삭제 중 오류: {msg}" | 부분 삭제 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `osnapPoints` | N | N - 선택수 |
| `osnapPointsWithNames` | N | N - 선택수 |
| `lvOsnap` | N행 | N - 선택수 행, 번호 재정렬 |

## 8. 후행 기능 (Chained)
- 필요 시 재수집 [`btnCollectOsnap`](./Osnap 수집.md)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L758](../../code-reference/form1-drawing2d.md#btnOsnapDelete_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
