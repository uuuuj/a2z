---
feature_id: ATR-003
feature_name: 속성 테이블 CSV 내보내기
category: Attribute
trigger_type: User Action
owner_module: Form1.Attribute.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-attribute.md#btnExportAttributeCSV_Click
---

# 속성 테이블 CSV 내보내기

## 1. 개요
`dgvAttributes`에 표시된 모든 행(기본 정보/BBox/UDA/Geometry)을 CSV 파일로 저장한다. 쉼표가 포함된 값은 쌍따옴표로 이스케이프.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnExportAttributeCSV` 클릭 |
| 위치 | 메인 폼 > 부재 정보 탭 |

## 3. 사전 조건
- [ ] `dgvAttributes.Rows.Count > 0` (부재 1개 이상 선택하여 속성 표시된 상태)

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 행 존재 확인 | Form1 | → [E01] |
| 2 | SaveFileDialog | UI | 기본 `Attributes_{nodeIndex}_{timestamp}.csv` |
| 3 | UTF-8 StreamWriter | Form1 | `using` 블록 |
| 4 | 헤더 작성 | Form1 | "No,Key,Value" |
| 5 | 행 순회 | Form1 | No/Key/Value 셀 읽기 + 쉼표 이스케이프 |
| 6 | 완료 알림 | UI | MessageBox 파일 경로 |

## 5. 주요 분기 처리

### [분기 A] 셀 값의 쉼표
| 조건 | 처리 |
|---|---|
| 쉼표 포함 | `"value"` 형식으로 감쌈 |
| 없음 | 그대로 |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 행 없음 | return | MessageBox "내보낼 속성이 없습니다. 부재를 먼저 선택하세요." | 변화 없음 |
| E02 | 저장 예외 | catch | MessageBox "CSV 저장 오류: {msg}" | 파일 없음/부분 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 디스크 CSV | 없음 | 생성됨 (UTF-8) |

## 8. 후행 기능 (Chained)
- 역방향: [UDA CSV 가져오기](./uda-import-csv.md) (단 가져오기는 UDA만)

## 9. 관련 링크
- 코드 구현: [Form1.Attribute.cs:L257](/docs/code-reference/form1-attribute.md#btnExportAttributeCSV_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
