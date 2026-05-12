---
feature_id: ATR-005
feature_name: UDA 편집
category: Attribute
trigger_type: User Action
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#btnUdaEdit_Click
---

# UDA 편집

## 1. 개요
`dgvAttributes`에서 UDA 섹션의 행을 선택한 뒤 편집 다이얼로그를 열어 Key/Value를 변경한다. Key 변경 시 `UpdateKey`, Value 변경 시 `Update`를 호출.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnUdaEdit` 클릭 |
| 위치 | 메인 폼 > 부재 정보 탭 |

## 3. 사전 조건
- [ ] 부재 선택됨
- [ ] `dgvAttributes`에서 **UDA 섹션 행** 1개 선택

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 부재 선택 확인 | Form1 | → [E01] |
| 2 | 행 선택 확인 | Form1 | → [E02] |
| 3 | UDA 행 판별 | Form1 | `IsUdaRow(rowIndex)` — 상위로 섹션 헤더 탐색 → [E03] |
| 4 | 기존 값 추출 | Form1 | oldKey, oldValue |
| 5 | 다이얼로그 | UI | 기존 값으로 초기화 |
| 6 | Key 변경 처리 | SDK | `UpdateKey(node, oldKey, newKey, true)` (변경됐을 때만) |
| 7 | Value 변경 처리 | SDK | `Update(node, newKey, newValue, true)` |
| 8 | 테이블 갱신 | Form1 | `UpdateAttributeTable(nodeIndex)` |
| 9 | 완료 알림 | UI | MessageBox |

## 5. 주요 분기 처리

### [분기 A] Key/Value 변경 유형
| 조건 | 처리 |
|---|---|
| Key만 변경 | `UpdateKey` + `Update` (값 동일해도 Update 호출) |
| Value만 변경 | `Update`만 |
| 둘 다 동일 | 아무 SDK 호출 없이 알림만 |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 부재 미선택 | return | MessageBox "부재를 먼저 선택하세요." | 변화 없음 |
| E02 | 행 미선택 | return | MessageBox "편집할 UDA 행을 선택하세요." | 변화 없음 |
| E03 | UDA 섹션 행이 아님 | return | MessageBox "UDA 항목만 편집할 수 있습니다..." | 변화 없음 |
| E04 | SDK 예외 | catch | MessageBox "UDA 편집 오류: {msg}" | 부분 적용 가능 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK UDA(node) | 이전 키/값 | 변경된 키/값 |
| `dgvAttributes` | 이전 | 갱신됨 |

## 8. 후행 기능 (Chained)
- [CSV 내보내기](./CSV 내보내기.md)

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L390](../../code-reference/form1-attribute.md#btnUdaEdit_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
