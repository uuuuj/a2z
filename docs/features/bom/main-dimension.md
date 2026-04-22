---
feature_id: BOM-004
feature_name: 메인 체인 치수 추출 (자동 파이프라인)
category: BOM
trigger_type: User Action
owner_module: Form1.BOM.cs
last_updated: 2026-04-22 (T-032 Osnap 맵 재사용으로 치수 계산 성능 개선)
code_reference: /docs/code-reference/form1-bom.md#btnMainDimension_Click
---

# 메인 체인 치수 추출 (자동 파이프라인)

## 1. 개요
**원클릭 통합 처리 버튼**: BOM 수집 → **Clash 검사 → 연결성 판정** → Osnap 수집 → X/Y/Z 체인 치수 계산 → `lvDimension` 목록 갱신 → 요약 → 시트 생성. Clash가 먼저 수행되고 **모든 부재가 한 덩어리로 연결되어 있어야** Osnap·치수 파이프라인이 이어진다 (T-023 v3, 2026-04-22 재배치).

> **T-029 이후**: 본 버튼은 `chainDimensionList`·`lvDimension` 데이터만 채우고 **3D 뷰에 치수선은 그리지 않는다**. 실제 3D 뷰 치수 렌더링은 사용자가 글로벌 X/Y/Z 뷰 버튼(`ApplyGlobalView`)을 눌러 `ShowAllDimensions(viewDirection)`이 호출될 때 수행. 치수·시트가 실제로 만들어지는 시점은 `Clash_OnClashTestFinishedEvent`(비동기).

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnMainDimension` 버튼 클릭 |
| 위치 | 메인 폼 > 자동 처리 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨 ([BOM-002](./open-model.md))
- [ ] **모든 visible 부재가 Clash 인접 그래프 기준 한 덩어리로 연결** (T-023 v3) — 떨어진 부재가 하나라도 있으면 [E03] 차단. 판정은 Clash 결과가 나온 뒤 `Clash_OnClashTestFinishedEvent`에서 수행

## 4. 전체 동작 흐름 (Happy Path)

```mermaid
flowchart TD
    A[btnMainDimension 클릭] --> B[모델 로드 확인]
    B --> B2[xray 잔존 클리어 T-026]
    B2 --> C[CollectBOMData]
    C --> D{bomList > 0?}
    D -- 아니오 --> E01[알림 후 종료]
    D -- 예 --> K[DetectClash 비동기]
    K --> K2{clashStarted?}
    K2 -- 아니오 단일 부재 --> POST1[CompleteMainDimensionPostClash<br/>isSingleMember=true]
    K2 -- 예 --> WAIT([Clash_OnClashTestFinishedEvent 대기])
    WAIT --> CLASH[clashList 수집·정렬·표시]
    CLASH --> CONN{연결 성분 1개?<br/>T-023 v3}
    CONN -- 아니오 --> E03[MessageBox 차단]
    CONN -- 예 --> POST2[CompleteMainDimensionPostClash<br/>isSingleMember=false]
    POST1 --> OS[CollectAllOsnap]
    POST2 --> OS
    OS --> G[MergeCoordinates + X/Y/Z 체인]
    G --> SHOW[ShowAllDimensions]
    SHOW --> SUM[요약 MessageBox]
    SUM --> GEN[GenerateDrawingSheets<br/>+ Sheet 1 BOM 자동 T-025]
```

### `btnMainDimension_Click` (동기, Clash 시작까지)
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 모델 확인 | Form1 | `vizcore3d.Model.IsOpen()` → [E01] |
| 2 | **xray 잔존 클리어** (T-026) | Form1 | `xraySelectedNodeIndices.Clear()` — 이전 시트 선택 잔존으로 "이전 부재 기준" 결과가 반복되던 버그 방지 |
| 3 | **오버레이 표시** (T-018) | UI | `ShowBusyOverlay("BOM 수집 중...")` |
| 4 | BOM 재수집 | Form1 | `CollectBOMData()` — 가시성 반영 위해 매번 재수집 |
| 5 | BOM 확인 | Form1 | `bomList.Count == 0` → [E02] (오버레이 해제 후 return) |
| 6 | Clash 시작 | Form1 | `ShowBusyOverlay("간섭검사 실행 중...")` → `bool clashStarted = DetectClash()` |
| 7 | 단일 부재 fallback (T-024) | Form1 | `!clashStarted` → `CompleteMainDimensionPostClash(isSingleMember: true, clashTestCount: 0)` 직접 호출. Clash 이벤트 미발동이지만 연결 성분 1개로 간주하고 나머지 파이프라인 수행 |
| 8 | 진입 종료 | Form1 | 오버레이는 유지. 실제 치수·시트 생성은 `Clash_OnClashTestFinishedEvent`가 맡음 |

### `Clash_OnClashTestFinishedEvent` (비동기, 판정 + 후속 파이프라인)
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 9 | Clash 결과 수집 | Form1 | `clashList.Clear` → `ClashTest` 순회 → `GetResultItem(PART)` → Index1/2·Name1/2·HotPoint.Z → 중복 제거 → 정렬 → `lvClash` 채움 |
| 10 | **연결성 판정** (T-023 v3) | Form1 | `IsSingleConnectedComponent(out componentCount)` — Clash 인접 그래프 BFS로 연결 성분 수 계산. ≠ 1이면 [E03] 차단 |
| 11 | `CompleteMainDimensionPostClash` 호출 | Form1 | `isSingleMember=false`, `clashTestCount=testCount` 전달 |

### `CompleteMainDimensionPostClash` (공용, 판정 통과 이후)
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 12 | Osnap 수집 | Form1 | `ShowBusyOverlay("Osnap 수집 중...")` → `CollectAllOsnap()` → `_autoProcessOsnapSuccess` 저장 (`osnapPointsWithNames`·`lvOsnap` + **T-032**: 부재별 맵 `_lastCollectedNodeOsnapMap`도 같이 채움) |
| 13 | **치수 계산 통합** (T-028 + T-032 최적화) | Form1 | `ShowBusyOverlay("치수 계산 중...")` → visible 부재 목록 산출 → `ComputeViewDimensionsForMembers(visibleMembers, null, 0.5f, _lastCollectedNodeOsnapMap)` 호출. **`_lastCollectedNodeOsnapMap` 전달로 GetOsnapPoint 중복 호출 제거** (T-032). 3뷰 × 2축 = 6조합 치수 생성 + 중복 제거. 2D 출력·글로벌 X/Y/Z·시트 선택 자동 모두 이 공용 헬퍼 재사용. `Stopwatch`로 소요 시간 측정, `DiagLog T-032 치수 계산: visibleMembers=N osnapMapNodes=K chain=M ComputeViewDimensionsForMembers=Xms` |
| 14 | `lvDimension` 갱신 | UI | 번호·축·뷰이름·거리·좌표 표시 |
| 14.5 | **3D 뷰 정리** (T-029) | SDK | `Review.Measure.Clear()` + `ShapeDrawing.Clear()` — 이전 렌더 잔존 제거. **3D 뷰 치수선은 그리지 않음**. 사용자가 글로벌 X/Y/Z 뷰 버튼 눌러야 `ShowAllDimensions(viewDir)`가 `chainDimensionList`에서 해당 뷰 필터링해 렌더링 |
| 15 | ListView 갱신 | UI | No/Axis/ViewName/Distance/Start/End |
| 16 | 치수 3D 표시 | SDK | `ShowAllDimensions()` |
| 17 | 요약 MessageBox | UI | BOM/Osnap/치수/Clash 개수 통합 (단일 부재 시 "간섭검사 건너뜀") |
| 18 | 시트 생성 (T-025) | Form1 | `GenerateDrawingSheets()` — 내부에서 Sheet 1(전체) 기준 `CollectBOMInfo` 자동 수집 |
| 19 | 오버레이 해제 | UI | `finally { HideBusyOverlay(); }` — 정상·예외 모두 |

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
| `clashStarted == true` | `Clash_OnClashTestFinishedEvent`가 비동기로 결과 수집 → **연결성 판정** → 통과 시 `CompleteMainDimensionPostClash(false, testCount)` |
| `clashStarted == false` (단일 부재, 쌍 0개, SDK 예외) | 본 핸들러에서 즉시 `CompleteMainDimensionPostClash(true, 0)` 호출 — 단일 부재는 연결 성분 1개로 간주, 판정 생략 |

### [분기 D] 연결성 판정 (T-023 v3)
| 조건 | 처리 |
|---|---|
| `IsSingleConnectedComponent(out n)` true (n == 1) | Osnap·치수·시트 생성 계속 |
| false (n ≥ 2) | MessageBox "서로 연결되지 않은 부재 그룹 N개 발견" → return. 치수도 시트도 만들지 않음 |

> 판정 기준: `clashList`의 Part 쌍을 Body 기반 양방향 인접 리스트로 구축 → BFS로 연결 성분 수 계산. `bomList.Count == 1`은 항상 통과.

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 모델 미로드 | return | MessageBox "먼저 파일을 열어주세요." | 상태 변화 없음 |
| E02 | `bomList.Count == 0` | return | MessageBox "BOM 데이터를 수집할 수 없습니다." | BOM 재수집만 시도됨 |
| E03 | **연결 성분 ≥ 2** (T-023 v3) | `Clash_OnClashTestFinishedEvent`에서 return | MessageBox "치수 추출은 모든 부재가 하나의 덩어리로 연결되어 있을 때만 가능합니다. 현재: 서로 연결되지 않은 부재 그룹 N개 발견. 해결: 떨어진 부재를 숨기거나 한 덩어리만 선택" | Osnap·치수·시트 모두 **미생성**. `DiagLog`에 `BLOCKED components=N (T-023 v3)` |
| E04 | 처리 중 예외 | catch | MessageBox "치수 추출 중 오류: {msg}" | 부분 반영 가능 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `bomList` | 이전 상태 | 재수집 완료 |
| `osnapPoints`, `osnapPointsWithNames` | 이전 | 현재 모델 Osnap (LINE/POINT만, CIRCLE 제외) |
| `chainDimensionList` | 이전 | 6조합 치수 + 중복 제거 (T-028) |
| `_autoProcessOsnapSuccess` | 이전 | 현재 수집 성공 여부 |
| `clashList` | 이전 | (비동기 완료 후 갱신) |
| `lvDimension` | 이전 | 갱신된 치수 행 |
| **3D 뷰 치수(`Review.Measure`)** (T-029) | 이전 렌더 | **비어 있음**. 글로벌 뷰 버튼 클릭 시 `ShowAllDimensions(viewDir)`이 렌더 |

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
| 2026-04-22 | T-026: 진입부에 `xraySelectedNodeIndices.Clear()` 추가 — 이전 시트 선택이 `CollectBOMData` X-Ray 필터(L591)에 계속 반영되어 "1개 부재 기준" 결과가 반복되던 잔존 상태 버그. 로그 근거: `btnMainDimension ENTER xray=1 ... EXIT chain=32`가 부재 1개 띄웠을 때와 동일 재현. 단계 1.3 추가 | Claude |
| 2026-04-22 | **T-023 v3 재재설계** — "STRU 단위"에서 **"Clash 기반 물리적 연결성 1덩어리"** 로 변경. 사용자 결정(정확성 우선). STRU 주석 블록 2개 제거. 파이프라인 순서 재배치 — Osnap/치수 로직을 `btnMainDimension_Click`에서 **`CompleteMainDimensionPostClash`** 공용 메서드로 분리하고, `Clash_OnClashTestFinishedEvent`에서 `IsSingleConnectedComponent` 판정 후 호출. 단일 부재(clashStarted=false)는 판정 생략하고 Post 메서드 직접 호출(T-024와 통합). 흐름도·단계표(3섹션)·분기 C·D·E03·E04·last_updated 모두 갱신 | Claude |
| 2026-04-22 | T-027: 치수 계산 단계(13) 직후 `FilterOsnapByViewDimensionUsage` 호출 추가(단계 13.5) — 도면 뷰×치수축 6개 조합의 1차 필터 endpoint 합집합만 `AddChainDimensionByAxis` 입력으로 사용. 3D 뷰 체인 치수 개수 감소(필터 전/후 DiagLog 기록). osnap 원본 리스트는 보존해 다른 기능(제작도·가공도) 영향 없음. 방식: (a) 체인 치수만 / β (endpoint 합집합 1회 산출 후 축별 1벌) | Claude |
| 2026-04-22 | **T-028**: 4경로(치수추출·글로벌 X/Y/Z·2D 출력·시트 선택 자동) 치수 엔진 통합. 2D 출력 엔진(`nodeOsnapMap` + `FilterOsnapForDimAxis` + `AddChainDimensionByAxis(viewDirection)`)을 `ComputeViewDimensionsForMembers` 공용 헬퍼로 추출. 본 핸들러는 단계 13에서 visible 부재를 이 헬퍼에 넘겨 3뷰 × 2축 = 6조합 치수를 한 번에 생성(중복 제거 + `ViewDirection` 콤마 누적). T-027 `FilterOsnapByViewDimensionUsage` 제거. `ShowAllDimensions` 내부 분기 ①②③ 제거되어 표시 전용으로 단순화. 단계표 12·13·14 재번호 | Claude |
| 2026-04-22 | **T-029**: 치수추출 버튼 완료 직후 `ShowAllDimensions()` 호출 **제거**. 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()`로 이전 렌더 잔존 정리. 3D 뷰는 "치수선 없는 깨끗한 상태"로 종료되고, 사용자가 글로벌 X/Y/Z 뷰 버튼을 눌러야 실제 치수선이 그려짐. 단계 14.5 추가, 상태 변화에 `Review.Measure` 행 갱신 | Claude |
| 2026-04-22 | **T-032**: `CollectAllOsnap` 내부에 **부재별 Osnap 맵**(`_lastCollectedNodeOsnapMap`) 병행 구축. `ComputeViewDimensionsForMembers`에 `preBuiltNodeOsnapMap` 파라미터 추가해 치수추출 버튼 경로에서 `GetOsnapPoint` 중복 호출 제거. `Stopwatch`로 소요 시간 측정, `DiagLog T-032` 기록. 시트 선택 자동 경로(다른 부재 집합)는 null 전달해 내부 재구축 유지. 단계 12·13 재기술 | Claude |
