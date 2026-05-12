---
feature_id: SHT-008
feature_name: 선택 시트 PDF 내보내기
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnExportSheet2DPDF_Click
---

# 선택 시트 PDF 내보내기

## 1. 개요
선택된 시트의 2D 도면을 벡터 PDF로 저장한다. 동작은 [Drawing2D의 PDF 내보내기](../2D도면/PDF 출력.md)와 동일(파일명 prefix만 `Sheet2D_`).

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnExportSheet2DPDF` 클릭 |
| 위치 | 메인 폼 > 도면 시트 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨
- [ ] 2D 도면 생성됨 (`ViewMode == Both`)

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 모델 확인 | Form1 | → [E01] |
| 2 | ViewMode 확인 | Form1 | `!= Both` → [E02] |
| 3 | SaveFileDialog | UI | 기본 파일명 `Sheet2D_yyyyMMdd_HHmmss` |
| 4 | 선택 테두리 제거 | SDK | Unselect* 2회 |
| 5 | PDF 내보내기 | SDK | `Export2PDFBy2DView(filePath)` |
| 6 | 완료 알림 | UI | MessageBox |

## 5. 주요 분기 처리
없음 (다이얼로그 Cancel 시 조용히 종료).

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 모델 미로드 | return | MessageBox "먼저 모델을 열어주세요." | 변화 없음 |
| E02 | 2D 미생성 | return | MessageBox "먼저 '2D 출력' 버튼으로 2D 도면을 생성해주세요." | 변화 없음 |
| E03 | Export 예외 | catch | MessageBox "PDF 저장 중 오류: {msg}" | 파일 없음/부분 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 디스크 PDF | 없음 | 생성됨 |
| SDK 선택 | 일부 | 전체 해제 |

## 8. 후행 기능 (Chained)
- 다른 시트 반복
- [전체 PDF 배치](./전체 PDF 출력.md)

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs:L806](../../code-reference/form1-drawing-sheets.md#btnExportSheet2DPDF_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
