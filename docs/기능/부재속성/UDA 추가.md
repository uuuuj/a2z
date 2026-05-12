---
feature_id: ATR-004
feature_name: UDA 추가
category: Attribute
trigger_type: User Action
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#btnUdaAdd_Click
---

# UDA 추가

## 1. 개요
현재 선택된 부재에 새로운 UDA 키/값을 추가한다. 입력 다이얼로그 → `UDA.Add` 호출 → 속성 테이블 갱신.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnUdaAdd` 클릭 |
| 위치 | 메인 폼 > 부재 정보 탭 |

## 3. 사전 조건
- [ ] `selectedAttributeNodeIndex >= 0` (부재 선택됨)

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | → [E01] |
| 2 | 다이얼로그 | UI | `ShowUdaInputDialog("UDA 추가", "", "")` |
| 3 | 취소 처리 | Form1 | `result == null` → return |
| 4 | Key 검증 | Form1 | 다이얼로그 내부: 빈 키면 경고 후 null 반환 |
| 5 | SDK 추가 | SDK | `UDA.Add(nodeIndex, key, value, true)` |
| 6 | 테이블 갱신 | Form1 | `UpdateAttributeTable(nodeIndex)` |
| 7 | 완료 알림 | UI | MessageBox |

## 5. 주요 분기 처리

### [분기 A] 다이얼로그 결과
| 조건 | 처리 |
|---|---|
| OK + Key 유효 | Step 5 진행 |
| OK + Key 빈 값 | 내부 경고 + return |
| Cancel | return |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 부재 미선택 | return | MessageBox "부재를 먼저 선택하세요." | 변화 없음 |
| E02 | `UDA.Add` 예외 | catch | MessageBox "UDA 추가 오류: {msg}" | UDA 없음 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK UDA(node) | 이전 키 집합 | 새 키 추가됨 |
| `dgvAttributes` | 이전 | 갱신됨 (UDA 섹션에 새 행) |

## 8. 후행 기능 (Chained)
- [UDA 편집](./UDA 편집.md) / [삭제](./UDA 삭제.md)
- [CSV 내보내기](./CSV 내보내기.md)

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L364](../../code-reference/form1-attribute.md#btnUdaAdd_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
