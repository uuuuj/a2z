---
feature_id: DRW2D-002
feature_name: 2D 도면 PDF 내보내기
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnExportPDF_Click
---

# 2D 도면 PDF 내보내기

## 1. 개요
생성된 2D 도면을 VIZCore3D 내장 API(`Export2PDFBy2DView`)로 **벡터 PDF**로 저장한다. CAD 호환성을 위해 노란 선택 테두리는 내보내기 전에 제거한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnExportPDF` 버튼 클릭 |
| 위치 | 메인 폼 > Drawing2D 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨
- [ ] 2D 도면 생성됨 (`vizcore3d.ViewMode == ViewKind.Both`)

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 모델 확인 | Form1 | `IsOpen()` → [E01] |
| 2 | 2D 생성 여부 확인 | Form1 | `ViewMode != Both` → [E02] |
| 3 | 저장 다이얼로그 | UI | `SaveFileDialog`, 기본 파일명 `2D_Drawing_yyyyMMdd_HHmmss` |
| 4 | 사용자 확인 | UI | Cancel 시 종료 |
| 5 | 선택 테두리 제거 | SDK | `UnselectAllObjectBy2DView()`, `UnselectCurrentWorkObjectBy2DView()` |
| 6 | PDF 내보내기 | SDK | `Export2PDFBy2DView(filePath)` |
| 7 | 완료 알림 | UI | MessageBox "PDF 파일로 저장되었습니다." |

> 구현 상세는 [코드 레퍼런스](../../code-reference/form1-drawing2d.md#btnExportPDF_Click) 참고

## 5. 주요 분기 처리

### [분기 A] 저장 다이얼로그 결과
| 조건 | 처리 |
|---|---|
| `DialogResult.OK` | Step 5로 진행 |
| 취소/닫기 | 조용히 종료 |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 모델 미로드 | return | MessageBox "먼저 모델을 열어주세요." | 상태 변화 없음 |
| E02 | 2D 미생성 (ViewMode ≠ Both) | return | MessageBox "먼저 '2D 생성' 버튼으로 2D 도면을 생성해주세요." | 상태 변화 없음 |
| E03 | `Export2PDFBy2DView` 실패 | catch | MessageBox "PDF 저장 중 오류: {msg}" | 파일이 생성되지 않거나 손상된 부분 파일 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| 디스크 PDF 파일 | 없음 | 저장됨 (벡터) |
| SDK 선택 상태 | 일부 선택 가능 | 전체 해제 |

## 8. 후행 기능 (Chained)
- 이후 다른 시트/모델 작업 자유

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L82](../../code-reference/form1-drawing2d.md#btnExportPDF_Click)
- 선행: [전체 2D 생성](./2D 생성.md)
- 용어집: [PDF (벡터)](../../_glossary.md#pdf-벡터)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
