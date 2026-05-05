---
title: 치수 텍스트 위치 (보조선 바깥 배치) 통합 사양
last_updated: 2026-05-06
related_task: T-058
type: technical-note
---

# 치수 텍스트 위치 통합 사양 (T-058)

회사 doc "개발 요청 — 상 5" 요구로, 치수 Text가 보조선 사이 간격을 초과해 보조선을 침범하는 케이스를 회피하기 위한 위치 정책 정리.

## 1. 회사 요구 (원문)

> 치수 Text가 치수 보조선을 넘어 설 경우 → 오른쪽 배치로 협의 했으나 아직 반영 안됨

좁은 치수에서 텍스트가 두 보조선 사이를 침범하지 않도록 보조선 바깥(우측)으로 이동.

## 2. 결론

**SDK 글로벌 옵션 1줄로 처리** — `MeasureStyle.AlignDistanceTextPosition = 2 (바깥쪽)`. 5곳 동시 적용으로 4경로 일관성 확보.

| 항목 | 값 |
|---|---|
| SDK enum | `MeasureStyle.AlignDistanceTextPosition` (`VIZCore3D.NET.xml:9298`) |
| 값 의미 | 0: 아래 / 1: 위 / **2: 바깥쪽** ← 채택 |
| 마진 | `AlignDistanceTextMargine = 3` (보조선 끝에서 3mm 떨어짐, 기존 유지) |
| 적용 범위 | 글로벌 (모든 치수에 일괄). 좁은 치수에서만 선별 적용 옵션은 SDK 미지원 |

## 3. 적용 위치 (5곳 단일 정책)

| # | 파일 | 라인 | 컨텍스트 | 경로 |
|---|---|---|---|---|
| 1 | `A2Z/Form1.Dimensions.cs` | 51 | `btnDimensionShowSelected_Click` measureStyle | 선택 치수 표시 |
| 2 | `A2Z/Form1.Dimensions.cs` | 448 | `ShowAllDimensions` measureStyle (4경로 본진, T-028 통합) | 글로벌 X/Y/Z + 시트 선택 + 2D 출력 |
| 3 | `A2Z/Form1.MfgDrawing.cs` | 325 | `ExecuteMfgDrawing` 메인 mfgStyle | 가공도 메인 |
| 4 | `A2Z/Form1.MfgDrawing.cs` | 1050 | `ExecuteMfgDrawing` 두 번째 분기 mfgStyle | 가공도 sub |
| 5 | `A2Z/Form1.MfgDrawing.cs` | 1703 | `ExecuteMfgDrawing` EA 분기 eaStyle | 가공도 EA Type |

## 4. 회사 사양과의 차이

회사 원문은 *"치수 보조선을 넘어 설 경우"* — 좁은 치수에서만. 본 구현은 *모든* 치수가 항상 바깥쪽 배치.

**이유**:
1. SDK가 치수별 개별 텍스트 위치 옵션을 제공하지 않음 (`AlignDistanceTextPosition`은 글로벌 `MeasureStyle` 옵션)
2. 텍스트 폭 측정 API 부재 — 좁은 치수 선별 판정에 폰트 메트릭 추정(`Graphics.MeasureString`) + 수동 좌표 계산 필요. 옵션 B(선별)는 복잡도 큼
3. 넓은 치수에서도 텍스트가 보조선 바깥에 있어도 시각적으로 자연스럽고, 좁은 치수에서의 침범 회피라는 본 사양의 핵심 의도는 충족

**대체 옵션 (필요 시)**:
- 옵션 A+거리 임계값: `ChainDimensionData.Distance < threshold`인 치수만 별도 스타일 적용 후 다른 measureStyle 분기 — 5곳 분기 확장 필요
- 옵션 B: ID 회수(`AddCustomAxisDistance` 반환) + `MeasureItem.UpdatePosition`으로 좁은 치수만 수동 우측 좌표 이동

## 5. 검증 자료 (sdk-verifier 결과)

`VIZCore3D.NET.xml` 등재 확인:
- `MeasureStyle.AlignDistanceTextPosition` (xml:9298) — 0/1/2 enum
- `MeasureStyle.AlignDistanceText` (xml:9293) — bool, 정렬 사용
- `MeasureStyle.AlignDistanceTextMargine` (xml:9304) — float, 여백

미지원 (포기됨):
- `Set2DViewCreateObjectItemMeasureTextPosition` — 가족 메서드 미존재
- `GetMeasureTextWidth` / `Object2D.GetTextBounds` — 텍스트 폭 측정 0건
- 치수별 개별 위치 옵션 — `MeasureItem.UpdatePosition`만 가능, 글로벌 옵션은 GroupSetStyle만

## 6. 관련 작업

- **T-040** (회사 상 6, 치수 Text-치수선 겹침) — 별개 작업. T-058은 보조선 침범 회피, T-040은 치수선과의 겹침 회피
- **T-039** (회사 상 8+9, offset 고정) — T-040 선행, 본 작업과 독립
- **T-046 확장** (보조선 gap, DONE) — 보조선 모델 표면 gap. 본 작업은 텍스트 위치라 별개

## 7. 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-05-06 | 최초 작성 — T-058 5곳 글로벌 옵션 적용 |
