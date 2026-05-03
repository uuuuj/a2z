---
title: Osnap 기준점 코드 동작 검증 보고서
last_updated: 2026-05-02
related_task: T-055
type: technical-note
---

# Osnap 기준점 코드 동작 검증 보고서 (T-055)

회사 doc "확인 중 — 완료 3"의 의문에 답하기 위한 코드 트레이스 결과.

## 1. 회사 의문 (원문)

> 치수 생성 기준 Osnap — X/Y/Z 시점별 부재의 기준점과 전체의 기준점 작성. 전체에서 제일 위 제일 오른쪽, 전체에서 오른쪽 끝 제일 위, ... 부재에서 오른쪽 끝 제일 위 — 이렇게 남겨서 중복 제거 (제거하나? 확인해야함). 코드 보고 확인해야됨 이게 실제로 맞는지.

## 2. 결론 (한 줄)

**부분 일치** — 핵심 의도(부재 단위 + 전체 단위 코너 우선 + 중복 제거)는 모두 구현되어 있으나, **부재 단위에서는 코너 4점이 아니라 1점만 남기는 점**이 명세 문구와 다름. 도면 결과물 측면에서는 회사 의도에 가깝게 동작.

## 3. 데이터 흐름

```
부재별 GetOsnapPoint(index)
  ├─ 전역 풀  osnapPointsWithNames     (부재 구분 없음, 평면 리스트)
  └─ 부재별 맵 _lastCollectedNodeOsnapMap[index]
                    ↓
ComputeViewDimensionsForMembers (뷰 X/Y/Z × 축 2개 루프)
                    ↓
FilterOsnapForDimAxis
   Step 4: 부재별 1점 (primary MAX, 동률 시 secondary MAX tiebreak)
   Step 5: 전역 dimAxis 값 dedup (보조축 큰 쪽 우선)
   Step 6: 코너 4점 강제 — A=primary MAX, B=primary MIN, C=secondary MAX, D=secondary MIN
                    ↓
MergeCoordinates (RoundToTolerance 0.5mm → |Δ| < tol 검사로 누적 dedup)
                    ↓
AddChainDimensionByAxis (필터축 그룹핑 → 인접 거리 + 전체 거리)
                    ↓
keyToDim 최종 dedup (키: Axis|Start_F1|End_F1, ViewDirection 콤마 누적)
```

## 4. 부재 vs 전체 기준점

**둘 다 수집되어 같은 풀로 흐릅니다 — 분리 보존되지 않고 동시 적재.**

- `Form1.BOM.cs:540~588` `CollectAllOsnap`이 `bodyNodes` 순회하며 각 노드의 `OsnapVertex3D`를 두 컨테이너에 동시 적재:
  - 전역 풀: `osnapPoints` / `osnapPointsWithNames` (`Form1.cs:44, 49`)
  - 부재별 맵: `_lastCollectedNodeOsnapMap[node.Index]` (`Form1.cs:108~113`)
- `OsnapKind.LINE` → Start/End 두 정점, `OsnapKind.POINT` → Center 1점, `OsnapKind.CIRCLE` → 제외 (`Form1.BOM.cs:571~573`)

**부재 vs 전체 구분은 다운스트림 시점에 발생**: `FilterOsnapForDimAxis` Step 4가 부재별 맵을 사용해 "부재당 1점"을 산출하고, Step 5+6이 전역 풀에서 "전체 기준점(코너 4점)"을 산출.

## 5. 뷰별 필터링

X/Y/Z 뷰별로 다른 Osnap이 선택됩니다.

`ComputeViewDimensionsForMembers`(`Form1.Dimensions.cs:1949~`)가 `viewsToProcess`를 ["X","Y","Z"] 또는 단일 뷰로 루프하며, 각 뷰에서 보이는 2축만 dimAxis로 처리:

| 뷰 | dimAxis 조합 | primary축 | secondary축 |
|---|---|---|---|
| X뷰 | Y축 + Z축 치수 | Z (수직) | Y (수평) |
| Y뷰 | X축 + Z축 치수 | Z (수직) | X (수평) |
| Z뷰 | X축 + Y축 치수 | Y (수직) | X (수평) |

primary가 보통 "위쪽", secondary가 "오른쪽"으로 해석되어 코너 우선순위가 결정됨 (`Form1.Dimensions.cs:2092~2097`).

## 6. 중복 제거 (3단)

| 단계 | 위치 | 알고리즘 | Tolerance |
|---|---|---|---|
| (a) Step 4 부재 dedup | `Form1.Dimensions.cs:2138~2157` | `nodeName`별 primary 최댓값 1점, 동률 시 secondary 최댓값 | — |
| (b) Step 5 전역 dimAxis dedup | `Form1.Dimensions.cs:2162~2179` | 같은 dimAxis 값(F1 반올림)이면 1개만 유지, 보조축 큰 쪽 우선 | F1 (0.1mm) |
| (c) `MergeCoordinates` | `Form1.Dimensions.cs:1750~1774` | `RoundToTolerance` → `Any(|Δ| < tol)` 검사로 누적 dedup | **0.5mm** |
| (d) 최종 `keyToDim` | `Form1.Dimensions.cs:2040~2058` | `{Axis}|{Start_F1}|{End_F1}` 키로 dedup, 중복 시 `ViewDirection` 콤마 누적 (예: `"X,Y"`) | F1 |

**Tolerance 0.5mm는** `btnExtractDimension_Click`(`Form1.Dimensions.cs:1686`), `ComputeViewDimensionsForMembers` 기본값(`:1950`), `Form1.DrawingSheets.cs:589`, `Form1.Drawing2D.cs`, `Form1.MfgDrawing.cs` 전부에서 동일 사용 — 일관성 확보됨.

**예시**: 좌표 (1234.7, 0, 500.2)와 (1234.6, 0, 500.4) → `RoundToTolerance`로 둘 다 X=1234.5, Z=500.0 → `MergeCoordinates`에서 1개만 남음.

## 7. 회사 명세 일치도

| 회사 명세 항목 | 코드 구현 | 일치도 |
|---|---|---|
| 부재 기준점 / 전체 기준점 분리 | Step 4(부재)·Step 5+6(전체) 단계 분리 ✅ | 일치 |
| "제일 위 제일 오른쪽" 등 코너 우선 | Step 6 코너 4점 강제 (A/B/C/D) ✅ | 일치 |
| 중복 제거 | 4단(부재→전역→좌표→키) 다중 dedup ✅ | 일치 |
| 뷰별 다른 기준점 | X/Y/Z 뷰별 primary·secondary 매핑 ✅ | 일치 |
| **부재별 4코너** ("부재에서 오른쪽 끝 제일 위") | 부재별 1점만 남김 (primary MAX + secondary tiebreak) | **부분 일치** |

부재 단위에서 코너 4점 보존이 필요하다면 Step 4의 `nodeName`별 그룹화 후 4코너 강제 포함 로직을 추가해야 하지만, 후속 Step 6에서 전역 코너 4점이 강제되므로 도면 가시성 측면에서는 큰 차이가 없을 가능성이 큽니다.

## 8. 회사 doc 갱신용 단답

> Osnap은 부재별 풀과 전역 풀 양쪽에 동시 적재됩니다. 치수 생성 시 X/Y/Z 뷰별로 다른 축 조합(X뷰→Y·Z, Y뷰→X·Z, Z뷰→X·Y)을 처리하며, `FilterOsnapForDimAxis`가 부재당 primary축 최대 1점만 남기고(부재 기준점), 전역에서는 같은 dimAxis 값을 1개로 dedup한 뒤 코너 4점(primary MAX/MIN, secondary MAX/MIN)을 강제 포함합니다(전체 기준점). 중복 제거는 0.5mm tolerance로 4단(부재 dedup → 전역 dimAxis dedup → MergeCoordinates 좌표 dedup → 최종 keyToDim) 적용됩니다. 회사 명세의 "코너 우선" 의도는 코드에 반영되어 있으며, 다만 **부재 단위에서는 4코너가 아니라 1점만 남긴다**는 점이 명세 문구와 부분 일치 상태입니다 (전체 단위 코너 4점이 강제되므로 도면 결과는 의도에 부합).

## 9. 인용 코드

| 위치 | 역할 |
|---|---|
| `Form1.cs:44` | `osnapPoints` 전역 풀 선언 |
| `Form1.cs:49` | `osnapPointsWithNames` 전역 풀 (이름 포함) |
| `Form1.cs:108~113` | `_lastCollectedNodeOsnapMap` 부재별 맵 |
| `Form1.BOM.cs:503~619` | `CollectAllOsnap` 본문 |
| `Form1.BOM.cs:540~588` | 부재 루프 + 양 컨테이너 동시 적재 |
| `Form1.BOM.cs:571~573` | `OsnapKind.CIRCLE` 제외 |
| `Form1.Dimensions.cs:1686` | tolerance = 0.5f |
| `Form1.Dimensions.cs:1750~1774` | `MergeCoordinates` 좌표 dedup |
| `Form1.Dimensions.cs:1779~1782` | `RoundToTolerance` |
| `Form1.Dimensions.cs:1819~1928` | `AddChainDimensionByAxis` |
| `Form1.Dimensions.cs:1949~2062` | `ComputeViewDimensionsForMembers` |
| `Form1.Dimensions.cs:2024~2029` | 뷰별 visibleAxes 결정 |
| `Form1.Dimensions.cs:2040~2058` | 최종 `keyToDim` dedup + ViewDirection 콤마 병합 |
| `Form1.Dimensions.cs:2085~2205` | `FilterOsnapForDimAxis` 본문 |
| `Form1.Dimensions.cs:2092~2097` | 뷰별 primary/secondary 매핑 |
| `Form1.Dimensions.cs:2116~2134` | 필수점 A/B/C/D 4코너 결정 |
| `Form1.Dimensions.cs:2138~2157` | Step 4 부재별 1점 필터 |
| `Form1.Dimensions.cs:2162~2179` | Step 5 전역 dimAxis dedup |
| `Form1.Dimensions.cs:2183~2194` | Step 6 코너 강제 포함 |
| `Models.cs:9~49` | `ChainDimensionData` (`ViewDirection` 포함) |

## 10. 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-05-02 | 최초 작성 — 회사 doc 의문 답변용 (T-055) |
