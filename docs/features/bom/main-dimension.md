---
feature_id: BOM-004
feature_name: 메인 체인 치수 추출 (자동 파이프라인)
category: BOM
trigger_type: User Action
owner_module: Form1.BOM.cs
last_updated: 2026-04-22 (T-018 오버레이 + T-024 fallback, T-023 가드는 비활성)
code_reference: /docs/code-reference/form1-bom.md#btnMainDimension_Click
---

# 메인 체인 치수 추출 (자동 파이프라인)

## 1. 개요
**원클릭 통합 처리 버튼**: BOM 수집 → Osnap 수집 → X/Y/Z 체인 치수 계산 → 치수 표시 → Clash 검사를 순차로 실행한다. Clash 결과는 비동기이며 `Clash_OnClashTestFinishedEvent`에서 최종 요약 알림이 표시된다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnMainDimension` 버튼 클릭 |
| 위치 | 메인 폼 > 자동 처리 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨 ([BOM-002](./open-model.md))

> **T-023 계획**: 향후 "하나의 STRU(UDA 기반 상위 단위)에 속하는 부재들만 치수추출" 가드 적용 예정. UDA 키·값 확정 시 `Form1.BOM.cs`의 주석 블록 활성화. 현재는 비활성 상태라 이 사전조건은 강제되지 않음.

## 4. 전체 동작 흐름 (Happy Path)

```mermaid
flowchart TD
    A[btnMainDimension 클릭] --> B[모델 로드 확인]
    B --> C[CollectBOMData]
    C --> D{bomList > 0?}
    D -- 아니오 --> E01[알림 후 종료]
    D -- 예 --> E[CollectAllOsnap]
    E --> F{osnapSuccess?}
    F -- 예 --> G[MergeCoordinates tolerance=0.5]
    G --> H[AddChainDimensionByAxis X/Y/Z]
    H --> I[lvDimension 갱신]
    I --> J[ShowAllDimensions]
    J --> K[DetectClash 비동기]
    F -- 아니오 --> K
    K --> L([Clash_OnClashTestFinishedEvent 대기])
```

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 모델 확인 | Form1 | `vizcore3d.Model.IsOpen()` → [E01] |
| 1.5 | STRU 단위 가드 (T-023, **현재 비활성**) | Form1 | UDA 키·값 확정 후 활성화 예정. `CheckSingleStruCondition()` — 선택 또는 visible 부재들의 공통 조상 STRU가 정확히 1개여야 통과. 코드는 `Form1.BOM.cs` 주석 블록에 완성 형태로 존재 |
| 2 | **오버레이 표시** (T-018) | UI | `ShowBusyOverlay("치수 추출 중...")` — 3D 뷰어 중앙에 "처리 중" 라벨 띄워 5초 공백 동안 UI 반응 표시. try 직전 호출, finally에서 `HideBusyOverlay()` |
| 3 | BOM 재수집 | Form1 | `CollectBOMData()` — 가시성 반영 위해 매번 재수집 |
| 4 | BOM 확인 | Form1 | `bomList.Count == 0` → [E02] |
| 5 | Osnap 수집 | Form1 | `ShowBusyOverlay("Osnap 수집 중...")` → `CollectAllOsnap()` → bool |
| 6 | 좌표 병합 | Form1 | `ShowBusyOverlay("치수 계산 중...")` → `MergeCoordinates(osnap, 0.5f)` — 0.5mm 허용오차 |
| 7 | 축별 체인 치수 | Form1 | X, Y, Z 각각 `AddChainDimensionByAxis()` |
| 8 | ListView 갱신 | UI | No/Axis/ViewName/Distance/Start/End 표시 |
| 9 | 치수 3D 표시 | SDK | `ShowAllDimensions()` → 축별 오프셋 적용 |
| 10 | 플래그 저장 | Form1 | `_autoProcessOsnapSuccess = osnapSuccess` |
| 11 | Clash 검사 시작 | Form1 | `ShowBusyOverlay("간섭검사 실행 중...")` → `bool clashStarted = DetectClash()` |
| 12 | **Clash fallback** (T-024) | Form1 | `!clashStarted` (단일 부재 등 쌍 0개 또는 SDK 예외)이면 `Clash_OnClashTestFinishedEvent` 미발동 → 여기서 `GenerateDrawingSheets()` + 요약 MessageBox 직접 호출 |
| 13 | **오버레이 해제** (T-018) | UI | `finally { HideBusyOverlay(); }` — 정상·예외 모두 해제. Clash가 시작된 경우는 비동기라 여기서 해제해도 UI 반응 유지 |

> 최종 결과 요약 알림은 [Clash 완료 콜백](../clash/clash-finished-event.md)에서 표시됨

> 구현 상세는 [코드 레퍼런스](/docs/code-reference/form1-bom.md#btnMainDimension_Click) 참고

## 5. 주요 분기 처리

### [분기 A] Osnap 수집 성공 여부
| 조건 | 처리 |
|---|---|
| osnapSuccess && osnapPointsWithNames.Count > 0 | 치수 추출 진행 (Step 5~8) |
| 실패 | 치수 추출 건너뛰기, Clash만 실행 |

### [분기 B] 축별 가시성 (xraySelectedNodeIndices)
| 조건 | 처리 |
|---|---|
| xraySelectedNodeIndices.Count > 0 | 선택 부재만 대상 |
| 비어있음 | `FromIndex().Visible`로 필터, 없으면 전체 |

### [분기 C] Clash 시작 성공 여부 (T-024)
| 조건 | 처리 |
|---|---|
| `clashStarted == true` | `Clash_OnClashTestFinishedEvent`가 비동기로 결과 수집 + 시트 생성 + 요약 MessageBox |
| `clashStarted == false` (단일 부재, 쌍 0개, SDK 예외) | 본 핸들러 내에서 `GenerateDrawingSheets()` + "Clash: 대상 부재가 1개 이하 (간섭검사 건너뜀)" MessageBox 직접 수행 |

### [분기 D] STRU 단위 가드 (T-023, **현재 비활성**)
| 조건 | 처리 |
|---|---|
| 선택된 부재들 또는 visible 부재들의 공통 조상 STRU가 정확히 1개 | 통과 (활성화 시) |
| 0개 또는 2개 이상 | MessageBox + return (활성화 시) |

> 현재 코드에서 호출부가 `/* */` 주석 처리되어 있어 분기 D는 무효. UDA 키·값 확정 시 활성화.

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 모델 미로드 | return | MessageBox "먼저 파일을 열어주세요." | 상태 변화 없음 |
| E02 | `bomList.Count == 0` | return | MessageBox "BOM 데이터를 수집할 수 없습니다." | BOM 재수집만 시도됨 |
| E03 | 처리 중 예외 | catch | MessageBox "치수 추출 중 오류: {msg}" | 부분 반영 가능 |
| E04 | STRU 단위 가드 실패 (T-023, **현재 비활성**) | return | MessageBox "치수 추출은 하나의 STRU 단위에서만 가능합니다. 현재: 표시 STRU N개, 선택 STRU M개. (한 STRU만 표시하거나, 한 STRU 또는 그 하위 부재들만 선택)" | 활성화 시 `DiagLog`에 `BLOCKED visibleStru=N selectedStru=M` |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `bomList` | 이전 상태 | 재수집 완료 |
| `osnapPoints`, `osnapPointsWithNames` | 이전 | 현재 모델 Osnap (LINE/POINT만, CIRCLE 제외) |
| `chainDimensionList` | 이전 | X/Y/Z 축별 체인 치수, No 부여 |
| `_autoProcessOsnapSuccess` | 이전 | 현재 수집 성공 여부 |
| `clashList` | 이전 | (비동기 완료 후 갱신) |
| `lvDimension` | 이전 | 갱신된 치수 행 |
| SDK Measure 표시 | 이전 | 모든 치수 표시 중 |

## 8. 후행 기능 (Chained)
- [Clash 완료 콜백](../clash/clash-finished-event.md) — 자동 호출
- 이후 [시트 자동 분할](../drawing-sheets/generate-sheets.md) — Clash 결과 있으면 자동
- [축별 치수 필터](../dimensions/show-axis-x.md)

## 9. 관련 링크
- 코드 구현: [Form1.BOM.cs:L283](/docs/code-reference/form1-bom.md#btnMainDimension_Click)
- 용어집: [Osnap](../../_glossary.md#osnap-object-snap), [Chain Dimension](../../_glossary.md#chain-dimension-체인-치수)
- 상위 파이프라인: [전체 파이프라인](../../_pipeline.md)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
| 2026-04-22 | T-018: 3D 뷰어 중앙 "처리 중..." 오버레이 라벨 추가 — BOM 수집 → Osnap → 치수 계산 → Clash 시작의 각 단계 진입 시 라벨 메시지 갱신. 공통 헬퍼 `ShowBusyOverlay`/`HideBusyOverlay`는 [Form1.cs](/docs/code-reference/form1-bom.md)에 신설. 핸들러는 try/finally로 감싸 예외 시에도 오버레이 해제 보장. 5초 공백 UX 문제 해결 | Claude |
| 2026-04-22 | T-024: `DetectClash()` 반환값을 받아 **`clashStarted == false`일 때 fallback 경로** 추가 — 단일 부재(쌍 0개)·SDK 예외는 `Clash_OnClashTestFinishedEvent` 미발동이므로 `GenerateDrawingSheets()` + 요약 MessageBox를 직접 호출해 시트 목록 미갱신 버그 해결. 단계표 10→13 재번호, 분기 C 신설 | Claude |
| 2026-04-22 | T-023: 단일 부재 가드 추가 — `IsOpen` 확인 직후 `GetPartialNode` + `FromFilter(SELECTED_TOP)`로 visible·selected 카운트 계산, 둘 다 ≠ 1이면 MessageBox 후 return. 사전 조건에 항목 추가, 단계 1.5 추가, E03 신설, 기존 E03은 E04로 재번호 | Claude |
| 2026-04-22 | T-023 재설계 — 사용자 의도는 "부재 개수"가 아니라 "STRU(UDA 상위 단위) 1개"로 판정. 기존 visible/selected==1 가드 **제거**. 새 `FindAncestorByUda` + `CheckSingleStruCondition` 헬퍼를 [Form1.BOM.cs 하단 주석 블록](/docs/code-reference/form1-bom.md)에 완성 형태로 보존(UDA 키·값 확정 시 `/* */` 해제만으로 활성화). 분기 D 추가, E03·E04 재정렬, 단계 1.5 "비활성" 표기. 사용자 매뉴얼의 에러 ③도 원복 | Claude |
