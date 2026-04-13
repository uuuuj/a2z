---
feature_id: DIM-007
feature_name: 풍선 위치 수동 조정
category: Dimensions
trigger_type: User Action
owner_module: Form1.Dimensions.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-dimensions.md#btnBalloonAdjust_Click
---

# 풍선 위치 수동 조정

## 1. 개요
ISO/축 뷰에서 자동 배치된 풍선이 겹치거나 가려질 때, 사용자가 특정 부재의 풍선을 직접 이동시킬 수 있는 다이얼로그를 연다. 조정값은 `balloonOverrides`에 저장된다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnBalloonAdjust` 클릭 |
| 위치 | 메인 폼 > 치수 탭 |

## 3. 사전 조건
- [ ] `bomList.Count > 0`
- [ ] `currentBalloonView` 설정됨 (뷰 버튼을 먼저 눌러 풍선이 생성된 상태)

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | BOM 확인 | Form1 | `bomList` 비어있음 → [E01] |
| 2 | 풍선 뷰 확인 | Form1 | `currentBalloonView == null/empty` → [E02] |
| 3 | 선택 부재 파악 | Form1 | `lvBOM.SelectedItems` 인덱스 추출 |
| 4 | 다이얼로그 구성 | UI | 위치 조정 입력 Form 표시 |
| 5 | 사용자 입력 반영 | Form1 | 확인 시 `balloonOverrides`에 (bomIndex → offset) 저장 |
| 6 | 풍선 재배치 | SDK | 현재 뷰에 맞춰 풍선 다시 생성 |

> 구현 상세는 [코드 레퍼런스](/docs/code-reference/form1-dimensions.md#btnBalloonAdjust_Click) 참고

## 5. 주요 분기 처리

### [분기 A] BOM 선택 상태
| 조건 | 처리 |
|---|---|
| 선택 있음 | 해당 부재 기본 선택 |
| 선택 없음 | 첫 번째 부재 기본 선택 |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | BOM 비어있음 | return | MessageBox "BOM 데이터가 없습니다..." | 변화 없음 |
| E02 | `currentBalloonView` 미설정 | return | MessageBox "먼저 뷰(ISO/X/Y/Z) 버튼을 클릭하여 풍선을 표시하세요." | 변화 없음 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `balloonOverrides` | 이전 | 사용자 지정 오프셋 추가/갱신 |
| 풍선 위치 | 자동 위치 | 오버라이드 반영 |

## 8. 후행 기능 (Chained)
- [PDF 내보내기](../drawing2d/export-pdf.md) — 조정된 위치 반영

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L238](/docs/code-reference/form1-dimensions.md#btnBalloonAdjust_Click)
- 용어집: [balloonOverrides](../../_glossary.md#balloonoverrides)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
