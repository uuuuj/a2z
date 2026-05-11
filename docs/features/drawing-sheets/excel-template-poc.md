---
title: 엑셀 템플릿 PoC (Step 1)
category: drawing-sheets
handler: btnExcelTemplatePoC_Click
file: A2Z/Form1.ExcelTemplate.cs
related: REQ-002, T-012, T-057
status: PoC Step 1
last_updated: 2026-05-12
---

# 엑셀 템플릿 PoC — Step 1 (`btnExcelTemplatePoC_Click`)

## 목적

REQ-002 / T-012 PoC. 외부 엑셀 파일(`사용자템플릿_엑셀_Rev_01.xlsx`)을 SDK `ImportExcel`로 2D View 캔버스에 적용 가능한지 **시각 검증만** 하는 단계.

기존 `GenerateSheetDrawing2D` (GridStructure + RenderTemplateOnGridStructure 기반)와는 별도 경로. 결과에 따라 Step 2(셀 좌표 매핑 → 모델 배치) 진행 여부 결정.

## 흐름

1. **엑셀 경로 검색** — 실행 폴더 / 솔루션 루트에서 `사용자템플릿_엑셀_Rev_01.xlsx` 자동 탐색. 없으면 `OpenFileDialog`로 사용자 선택.
2. **`vizcore3d.Drawing2D.Template.ImportExcel(path)` 호출** — SDK가 엑셀 셀 구조를 2D 캔버스에 그리는지 확인.
3. **DiagLog** — `logs/diag-yyyy-mm-dd.log`에 호출 시작·완료 기록 (예외 시 stacktrace).
4. **결과 안내 MessageBox** — 사용자가 2D View 캔버스를 직접 보고 다음 항목 확인:
   - 엑셀 셀 구조(테두리·텍스트·라벨)가 그려졌는지
   - 아무것도 안 보이면 별도 표시 호출 (예: `RenderTemplate`)이 필요할 수 있음

## 단계별 검증 결과

| Step | 내용 | 상태 |
|---|---|---|
| 1 | `ImportExcel` 단독 호출 — 시각 결과 확인 | **사용자 실기 검증 대기 (사내 PC)** |
| 2 | `templateDatas` 또는 `ParseJson` 등으로 셀 좌표 수집, placeholder(`{Image}`, `ISO`, `LOOKING "X/Y/Z"`, `BILL OF MATERIAL` 등) → Row/Column 매핑 | Step 1 결과 보고 결정 |
| 3 | 셀 영역에 `AddModel(viewIndex)` 호출 + 이미지/BOM 데이터 배치 | Step 2 후 진행 |

## 사전 확정 정보

- 사용자 엑셀 (`사용자템플릿_엑셀_Rev_01.xlsx`) 구조:
  - 단일 시트 "SHI", 55컬럼 × 40행, paperSize=8 (A3 landscape)
  - 컬럼 width 3.43, 행 ht 16.5pt
  - 비율 W/H ≈ 1.41 (A4 가로 비율 1.414에 거의 정확)
  - 4개 뷰 영역: ISO(E~T / 4~22행), LOOKING "Z"(V~AK / 4~22행), LOOKING "X"(E~T / 25~37행), LOOKING "Y"(V~AK / 25~37행)
  - BOM 영역: AN~BC / 4~18행
  - NOTE: AN19:BC21
  - 도면정보: 27~28행, REV/DATE/DESCRIPTION/DRAWN/CHECKED/APPROVED/PAINT CODE/DP NO
  - 이미지 슬롯 4개: B2:D5, AL2:AM6, AN31:AR34, AN35:AR38
  - TAG NO: AN39:BC40

- SDK API ([VIZCore3D.NET.xml:29219](../../../lib/VIZCore3D.NET.xml:29219)):
  - `Drawing2DTemplateManager.ImportExcel(string path)` — 도면 Excel 파일 불러오기
  - `Drawing2DTemplateManager.templateDatas` — **private/internal 필드, 외부 접근 불가** (Step 1 빌드 검증으로 확정)
  - `Drawing2DObjectManager.AddModel(int viewIndex)` — Step 2 이후 사용 예정

## UI

- 위치: 도면정보 탭 / "작업" 그룹박스(groupBox1) 끝 — `btnExtractDimension` 옆
- 텍스트: "엑셀 PoC"
- 크기: 78 × 25 px

## 변경 이력

| 날짜 | 작업 | 커밋 |
|---|---|---|
| 2026-05-12 | PoC Step 1 신설 — `btnExcelTemplatePoC` + `Form1.ExcelTemplate.cs` partial class | (이번 커밋) |
