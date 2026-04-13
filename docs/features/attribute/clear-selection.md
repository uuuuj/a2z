---
feature_id: ATR-002
feature_name: 선택 해제
category: Attribute
trigger_type: User Action
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#btnClearSelection_Click
---

# 선택 해제

## 1. 개요
3D 뷰어의 선택과 속성 테이블을 모두 초기화한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnClearSelection` 클릭 |
| 위치 | 메인 폼 > 부재 정보 탭 |

## 3. 사전 조건
없음.

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | SDK 선택 해제 | SDK | `Object3D.Select(new List<int>(), false, false)` |
| 2 | 속성 테이블 Clear | Form1 | `ClearAttributeTable()` |
| 3 | 라벨 초기화 | UI | "3D 뷰어에서 부재를 선택하세요" |
| 4 | 인덱스 초기화 | Form1 | `selectedAttributeNodeIndex = -1` |

## 5. 주요 분기 처리
없음.

## 6. 예외 / 에러 처리
없음.

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK Selection | 이전 | 비어있음 |
| `selectedAttributeNodeIndex` | 이전 | -1 |
| `dgvAttributes` | 이전 | 빈 상태 |
| `lblSelectedNode` | 이전 | 기본 안내 |

## 8. 후행 기능 (Chained)
- 새 부재 선택 시 [객체 선택 이벤트](./object-selected-event.md) 자동 호출

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L248](/docs/code-reference/form1-attribute.md#btnClearSelection_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
