# End-to-End 파이프라인

A2Z-HYI 앱의 전형적인 사용 흐름입니다. 개별 단계의 상세는 링크된 기능 문서를 참고하세요.

---

## 전체 흐름

```mermaid
flowchart TD
    A[앱 시작] --> B[VIZCore3D 초기화]
    B --> C[btnOpen_Click<br/>모델 로드]
    C --> D[btnCollectBOM_Click<br/>BOM 수집 + 홀 감지]
    D --> E[btnCollectOsnap_Click<br/>Osnap 수집]
    E --> F[btnMainDimension_Click<br/>체인 치수 계산]
    F --> G[btnClashDetection_Click<br/>간섭 검사]
    G --> H{{Clash_OnClashTestFinishedEvent}}
    H --> I[btnGenerateSheets_Click<br/>시트 자동 분할 BFS]
    I --> J[btnGenerateSheet2D_Click<br/>시트별 2D 생성]
    J --> K[btnExportSheet2DPDF_Click<br/>PDF 출력]

    D -.단일 부재.-> M[btnMfgDrawing_Click<br/>가공도 생성]
    M --> N[btnMfgDrawingSheet_Click<br/>배치 가공도]

    B --> O[Object3D_OnObject3DSelected<br/>속성 패널 갱신]
    O --> P[UDA CRUD]
```

---

## 단계별 설명

| # | 단계 | 트리거 | 결과 상태 | 문서 |
|---|---|---|---|---|
| 1 | VIZCore3D 초기화 | 앱 시작 → 이벤트 | SDK 이벤트 핸들러 구독 완료 | [vizcore3d-initialized](./features/bom/vizcore3d-initialized.md) |
| 2 | 모델 로드 | btnOpen | `vizcore3d.Model.IsOpened = true` | [open-model](./features/bom/open-model.md) |
| 3 | BOM 수집 | btnCollectBOM | `bomList` 채워짐, 홀/슬롯 감지 | [collect-bom](./features/bom/collect-bom.md) |
| 4 | Osnap 수집 | btnCollectOsnap | `osnapPointsWithNames` 채워짐 | [collect-osnap](./features/drawing2d/collect-osnap.md) |
| 5 | 체인 치수 | btnMainDimension | `chainDimensionList` 생성 | [main-dimension](./features/bom/main-dimension.md) |
| 6 | 간섭 검사 | btnClashDetection | 비동기 검사 시작 | [detect-clash](./features/clash/detect-clash.md) |
| 7 | 간섭 완료 콜백 | Event | `clashList` 정렬·완성 | [clash-finished-event](./features/clash/clash-finished-event.md) |
| 8 | 시트 분할 | btnGenerateSheets | BFS로 `drawingSheets` 생성 | [generate-sheets](./features/drawing-sheets/generate-sheets.md) |
| 9 | 시트별 2D | btnGenerateSheet2D | 2D 도면 렌더 | [generate-sheet-2d](./features/drawing-sheets/generate-sheet-2d.md) |
| 10 | PDF 출력 | btnExportSheet2DPDF / btnExportAllPDF | 벡터 PDF 파일 | [export-sheet-2d-pdf](./features/drawing-sheets/export-sheet-2d-pdf.md) |

---

## 병렬/독립 흐름

- **속성 조회 / UDA 편집**: 모델 로드 이후 언제든 가능. [features/attribute/](./features/attribute/_index.md)
- **가공도 생성**: BOM 수집 이후 단일 부재 선택으로 독립 실행. [features/mfg-drawing/](./features/mfg-drawing/_index.md)
- **뷰 전환 (ISO/X/Y/Z)**: 모델 로드 이후 언제든 가능. [features/global-views/](./features/global-views/_index.md)

---

## 공유 상태 (Form1 멤버)

| 필드 | 생성 단계 | 소비 단계 |
|---|---|---|
| `bomList` | BOM 수집 | 모든 도면 생성 기능 |
| `osnapPointsWithNames` | Osnap 수집 | 치수·풍선·보조선 |
| `chainDimensionList` | 체인 치수 | 2D/가공도 |
| `clashList` | 간섭 검사 | 시트 분할 BFS |
| `drawingSheets` | 시트 분할 | 시트별 2D·PDF |
| `balloonOverrides` | 2D 생성 중 | PDF 출력 |
| `xraySelectedNodeIndices` | 뷰 조작 | 글로벌 뷰 |
| `bodyToPartNameMap` | BOM 수집 | 성능 최적화 전반 |
