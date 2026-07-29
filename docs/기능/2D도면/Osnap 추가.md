---
feature_id: DRW2D-008
feature_name: Osnap 수동 추가 (픽킹 모드)
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnOsnapAdd_Click
---

# Osnap 수동 추가 (픽킹 모드)

## 1. 개요
VIZCore3D GeometryUtility의 Osnap **픽킹 이벤트**를 등록하고 모드를 활성화한다. 이후 사용자가 뷰어를 클릭할 때마다 해당 좌표가 `osnapPoints`에 추가된다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnOsnapAdd` 클릭 |
| 위치 | 메인 폼 > Osnap 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 이벤트 재등록 | Form1 | `OnOsnapPickingItem -=` 후 `+=` (중복 방지) |
| 2 | Osnap 모드 활성화 | SDK | `GeometryUtility.ShowOsnap(false, true, true, true)` — 선/원/점 |
| 3 | 안내 메시지 | UI | MessageBox 사용법 표시 |

> 픽킹 발생 시 동작은 [Osnap 픽킹 이벤트](./Osnap%20피킹%20이벤트.md) 참고

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 | catch | MessageBox "Osnap 추가 모드 활성화 중 오류: {msg}" | 모드 비활성 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| `GeometryUtility.OnOsnapPickingItem` | 구독 해제/이전 | Form1 구독 |
| `GeometryUtility` ShowOsnap | 비활성 | 활성 (선/원/점) |

## 8. 후행 기능 (Chained)
- [Osnap 픽킹 이벤트](./Osnap%20피킹%20이벤트.md) — 사용자 클릭 시마다 호출

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L694](../../code-reference/form1-drawing2d.md#btnOsnapAdd_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
