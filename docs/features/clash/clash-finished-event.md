---
feature_id: CLS-003
feature_name: 간섭 검사 완료 콜백
category: Clash
trigger_type: Event Callback
owner_module: Form1.Clash.cs
last_updated: 2026-04-22 (T-023 v3 연결성 판정 + Osnap/치수 파이프라인 인수)
code_reference: /docs/code-reference/form1-clash.md#Clash_OnClashTestFinishedEvent
---

# 간섭 검사 완료 콜백

## 1. 개요
VIZCore3D가 모든 ClashTest 실행을 완료했을 때 호출된다. 결과를 수집·중복 제거·Z값 정렬하고, `clashList`를 완성한다. **T-023 v3 (2026-04-22)부터는 `btnMainDimension` 파이프라인의 중심 분기점** — Clash 인접 그래프 기반 **연결성 판정**을 수행해 떨어진 부재가 없을 때만 `CompleteMainDimensionPostClash`(Osnap → 치수 → 요약 → 시트)를 호출한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | Event Callback |
| 입력 | `vizcore3d.Clash.OnClashTestFinishedEvent` |
| 위치 | 앱 초기화 시 구독 ([BOM-001](../bom/vizcore3d-initialized.md)) |

## 3. 사전 조건
- [ ] `btnClashDetection_Click` 또는 `btnMainDimension_Click`에서 `DetectClash()` 실행됨
- [ ] `vizcore3d.Clash.ClashTestCount > 0`

## 4. 전체 동작 흐름 (Happy Path)

```mermaid
flowchart TD
    A[이벤트 수신] --> B[clashList·lvClash Clear]
    B --> C[ClashTest 개수 조회]
    C --> D[결과 순회 PART 레벨 그룹화]
    D --> E[ClashData 생성]
    E --> F[중복 검사 A-B / B-A]
    F --> G[clashList 추가]
    G --> H{clashList > 0?}
    H -- 예 --> I[Z값 내림차순 정렬]
    I --> J[lvClash 갱신]
    J --> K[요약 MessageBox]
    K --> L[GenerateDrawingSheets 호출]
    H -- 아니오 --> K
```

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 결과 컨테이너 리셋 | Form1 | `clashList.Clear()`, `lvClash.Items.Clear()` |
| 2 | 테스트 순회 | Form1 | `for i in 0..ClashTestCount` |
| 3 | 결과 조회 | SDK | `GetResultItem(test, ResultGroupingOptions.PART)` |
| 4 | ClashData 생성 | Form1 | Index1/2, Name1/2, HotPoint.Z |
| 5 | 중복 검사 | Form1 | 양방향 (A-B, B-A) 체크 |
| 6 | 리스트 추가 | Form1 | 중복 아니면 `clashList.Add` |
| 7 | 정렬 | Form1 | `clashList.Sort((a,b) => b.ZValue.CompareTo(a.ZValue))` |
| 8 | ListView 표시 | UI | Name1 / Name2 / Z(F2) |
| 9 | **연결성 판정** (T-023 v3) | Form1 | `IsSingleConnectedComponent(out componentCount)` — Clash 인접 그래프(Part→Body 역매핑) BFS로 연결 성분 수 계산. ≠ 1이면 MessageBox + return (Osnap·치수·시트 모두 미생성) |
| 10 | 후속 파이프라인 호출 | Form1 | 판정 통과 시 `CompleteMainDimensionPostClash(isSingleMember: false, clashTestCount: testCount)` — Osnap 수집 → 체인 치수 → 요약 MessageBox → `GenerateDrawingSheets()`(+ Sheet 1 BOM 자동 수집) → 오버레이 해제 |

> 구현 상세는 [코드 레퍼런스](/docs/code-reference/form1-clash.md#Clash_OnClashTestFinishedEvent) 참고

## 5. 주요 분기 처리

### [분기 A] 결과 존재 여부
| 조건 | 처리 |
|---|---|
| `clashList.Count > 0` | 정렬·`lvClash` 표시 + 연결성 판정 진행 |
| 비어있음 | `lvClash` 비움 유지. 연결 성분은 부재 개수 그대로 → `bomList.Count > 1`이면 무조건 [E03] 차단 (간섭 0개 = 모든 부재가 떨어져 있음) |

### [분기 B] 연결성 (T-023 v3 — 핵심)
| 조건 | 처리 |
|---|---|
| `componentCount == 1` | `CompleteMainDimensionPostClash(false, testCount)` 호출 → Osnap·치수·요약·시트 |
| `componentCount ≥ 2` | MessageBox "연결되지 않은 부재 그룹 N개 발견" + `DiagLog BLOCKED components=N` + `HideBusyOverlay` + return. Osnap·치수·시트 모두 **미생성** |

> 본 경로는 "Clash가 실제로 돌았을 때"만 호출. **Clash 시작 자체가 실패**(단일 부재 등 쌍 0개) 시는 본 콜백이 아예 호출되지 않고 [`btnMainDimension_Click`의 fallback](../bom/main-dimension.md) 경로가 `CompleteMainDimensionPostClash(true, 0)`을 직접 호출해 같은 Post 메서드를 탄다 (판정은 스킵, 연결 성분 1개로 간주).

### [분기 B] HotPoint 유효성
| 조건 | 처리 |
|---|---|
| `result.HotPoint != null` | ZValue 저장 |
| null | ZValue 기본값 0 |

### [분기 C] Osnap 수집 결과 반영
| 조건 | 처리 |
|---|---|
| `_autoProcessOsnapSuccess == false` | 요약 메시지에 "* Osnap 수집 실패" 추가 |
| true | 기본 요약만 |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | `vizcore3d.Clash.Items[i] == null` | continue | 해당 테스트 건너뜀 | 일부 결과 유실 가능 |
| E02 | 처리 중 예외 | catch | `HideBusyOverlay` + MessageBox "간섭검사 결과 처리 중 오류: {msg}\nStack Trace: ..." | `clashList` 부분 채워짐, 오버레이 안전 해제 |
| E03 | **연결 성분 ≥ 2** (T-023 v3) | return | MessageBox "치수 추출은 모든 부재가 하나의 덩어리로 연결되어 있을 때만 가능합니다. 현재: 연결되지 않은 부재 그룹 N개 발견" | Osnap·치수·시트 모두 **미생성**. `DiagLog`에 `BLOCKED components=N (T-023 v3)` |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After (판정 통과) | After (판정 실패) |
|---|---|---|---|
| `clashList` | 비어있음 | ClashData 리스트, Z값 내림차순 정렬 | 동일 |
| `lvClash` | 비어있음 | 표시 | 표시됨 (판정 전 이미 채움) |
| `osnapPoints*` / `chainDimensionList` | 이전 | 재계산 | **그대로** |
| `drawingSheetList` | 이전 | `GenerateDrawingSheets()` 결과 | **그대로** |
| 요약 MessageBox | — | 표시 (Post 메서드) | **미표시** (차단 MessageBox로 대체) |
| 오버레이 | 유지 중 | Post 메서드 finally에서 해제 | 즉시 해제 + 차단 MessageBox |

## 8. 후행 기능 (Chained)
- [시트 자동 분할](../drawing-sheets/generate-sheets.md) (내부 `GenerateDrawingSheets()` 호출)
- [LvClash 더블클릭](../drawing2d/lvclash-doubleclick.md) — 사용자가 결과 클릭 시
- [Clash 선택 시 치수 필터](../dimensions/lvclash-selected.md)

## 9. 관련 링크
- 코드 구현: [Form1.Clash.cs:L397](/docs/code-reference/form1-clash.md#Clash_OnClashTestFinishedEvent)
- 용어집: [Clash](../../_glossary.md#clash-간섭), [BFS 기반 시트 분할](../../_glossary.md#bfs-기반-시트-분할)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
| 2026-04-22 | T-024: `clashList.Count > 0` 분기 제거 → `GenerateDrawingSheets()`를 **항상** 호출. 간섭 없는 다중 부재도 Sheet 1/설치도/가공도 생성되도록 보장. 단일 부재 케이스의 Clash 시작 실패는 본 콜백 미발동이라 `btnMainDimension_Click` fallback이 담당 (참조 링크 추가) | Claude |
| 2026-04-22 | **T-023 v3 대폭 확장** — 본 콜백이 `btnMainDimension` 파이프라인의 중심 분기점으로 승격. 단계 9 연결성 판정 + 단계 10 `CompleteMainDimensionPostClash` 호출로 변경. 분기 B(연결성)·E03(차단)·상태 변화 2열 추가. 사용자 의도: 떨어진 부재가 있으면 치수추출 무의미 → 원천 차단 | Claude |
