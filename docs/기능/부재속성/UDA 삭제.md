---
feature_id: ATR-006
feature_name: UDA 삭제
category: Attribute
trigger_type: User Action
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#btnUdaDelete_Click
---

# UDA 삭제

## 1. 개요
선택한 UDA 행의 Key를 삭제한다. 삭제 전 Yes/No 확인 다이얼로그 표시.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnUdaDelete` 클릭 |
| 위치 | 메인 폼 > 부재 정보 탭 |

## 3. 사전 조건
- [ ] 부재 선택됨
- [ ] UDA 섹션 행 1개 선택

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 부재 선택 확인 | Form1 | → [E01] |
| 2 | 행 선택 확인 | Form1 | → [E02] |
| 3 | UDA 행 판별 | Form1 | `IsUdaRow` → [E03] |
| 4 | Key 추출 | Form1 | 선택 행의 Key 셀 |
| 5 | 확인 다이얼로그 | UI | MessageBox YesNo |
| 6 | 삭제 확인 | Form1 | No 선택 시 return |
| 7 | SDK 삭제 | SDK | `UDA.Delete(nodeIndex, key, true)` |
| 8 | 테이블 갱신 | Form1 | `UpdateAttributeTable` |
| 9 | 완료 알림 | UI | MessageBox |

## 5. 주요 분기 처리

### [분기 A] 사용자 확인
| 조건 | 처리 |
|---|---|
| Yes | 삭제 실행 |
| No / Cancel | return |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 부재 미선택 | return | MessageBox "부재를 먼저 선택하세요." | 변화 없음 |
| E02 | 행 미선택 | return | MessageBox "삭제할 UDA 행을 선택하세요." | 변화 없음 |
| E03 | UDA 행 아님 | return | MessageBox "UDA 항목만 삭제할 수 있습니다..." | 변화 없음 |
| E04 | SDK 예외 | catch | MessageBox "UDA 삭제 오류: {msg}" | 상태 미변경 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| SDK UDA(node) | 해당 키 존재 | 해당 키 제거 |
| `dgvAttributes` | 이전 | 갱신됨 |

## 8. 후행 기능 (Chained)
- 다른 UDA 작업

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L443](../../code-reference/form1-attribute.md#btnUdaDelete_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
