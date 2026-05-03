---
title: 치수 보조선(Extension Line) 통합 사양
last_updated: 2026-05-02
related_task: T-046
type: technical-note
---

# 치수 보조선(Extension Line) 통합 사양 (T-046)

회사 doc "확인 중 — 긴급상 10" 요구로, 모든 치수 보조선의 **선 종류 통일** + **모델 표면 gap 적용**을 단일 지점에서 제어하는 구조 정리.

## 1. 회사 요구 (확장 후)

원문은 가공도 보조선만 명시했지만, 사용자 확장 지시:
1. **모든 보조선** (가공도 + 일반 시트 + 글로벌 X/Y/Z + 치수 추출)을 **가는 실선**으로 통일
2. 보조선이 모델 표면에 **붙어 있지 않고 약간 떨어져서** 그어지도록

## 2. 결론

| 요구 | 가능 여부 | 적용 방식 |
|---|---|---|
| 모든 보조선 SOLID 통일 | ✅ 코드 가능 | `DASHED_DOUBLEDOTTED` → `SOLID` 교체 (가공도 2곳) |
| 보조선 모델에서 gap | ✅ 우회 가능 (SDK 직접 옵션 부재) | 보조선 시작점 좌표를 외향으로 1mm 이동 — `DrawDimension` 단일 지점에서 처리 |

## 3. 보조선 생성 구조 (4경로 단일화)

모든 경로의 보조선이 **`DrawDimension` 한 함수**를 거쳐 만들어지므로, gap 적용은 그 한 곳만 수정하면 4경로 자동 적용.

```
[치수추출] CompleteMainDimensionPostClash
              ↓
[일반 시트] LvDrawingSheet_SelectedIndexChanged → ComputeViewDimensionsForMembers
              ↓
[글로벌 X/Y/Z] ShowAllDimensions
              ↓
[가공도] ExecuteMfgDrawing (3개 위치: 메인 / 본문 / EA 두 번째 뷰)
              ↓
        ┌─────┴─────┐
        ▼           ▼
   DrawDimension  (extensionLines 채우기)
        ↓
   extLine1: OffsetTowardLineEnd(originalStart, startVertex, gap) → startVertex
   extLine2: OffsetTowardLineEnd(originalEnd,   endVertex,   gap) → endVertex
        ↓
   extensionLines (List<Vertex3DItemCollection>) → vizcore3d.ShapeDrawing.AddLine(...)
        ↓
   각 호출자가 (가공도/일반시트/치수추출) ShapeDrawing IDs 수집
        ↓
   Add2DObjectFromShapeDrawing 시 LineType=SOLID, LineWidth=0.3~0.5mm
```

## 4. 핵심 변경

### 4-1. `Form1.Dimensions.cs` — `DrawDimension` 보조선 시작점 보정

**Before** (`Form1.Dimensions.cs:1089~1099`):
```csharp
// 보조선 추가 (Osnap 위치 → 치수선 위치)
var extLine1 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
extLine1.Add(originalStart);
extLine1.Add(startVertex);
extensionLines.Add(extLine1);
// extLine2도 동일 패턴
```

**After**:
```csharp
// T-046: 모델 표면에서 ExtensionLineGap(10mm)만큼 떨어져 시작
var extLine1 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
extLine1.Add(OffsetTowardLineEnd(originalStart, startVertex, ExtensionLineGap));
extLine1.Add(startVertex);
extensionLines.Add(extLine1);
// extLine2도 동일 패턴
```

신설 헬퍼 (`Form1.Dimensions.cs` L1100 직후, DrawDimension 직후):
```csharp
private const float ExtensionLineGap = 1.0f;

private VIZCore3D.NET.Data.Vertex3D OffsetTowardLineEnd(
    VIZCore3D.NET.Data.Vertex3D from,
    VIZCore3D.NET.Data.Vertex3D to,
    float distance)
{
    float dx = to.X - from.X, dy = to.Y - from.Y, dz = to.Z - from.Z;
    float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    if (len < 1e-3f || distance >= len)
        return new VIZCore3D.NET.Data.Vertex3D(to.X, to.Y, to.Z);
    float ratio = distance / len;
    return new VIZCore3D.NET.Data.Vertex3D(
        from.X + dx * ratio, from.Y + dy * ratio, from.Z + dz * ratio);
}
```

수학적 의미:
- `to - from`이 보조선 진행 방향(외향)
- `from + (to - from) × (gap / |to - from|)` 으로 시작점을 gap만큼 외향 이동
- 길이 < gap 또는 길이 ≈ 0인 경우 `to` 반환 (역전 방지)

### 4-2. `Form1.MfgDrawing.cs` — 가공도 보조선 LineType 통일

**Before** (`L1542`, `L1900`): 토글 패턴
```csharp
Set2DViewCreateObjectItemLineType(DASHED_DOUBLEDOTTED);
Add2DObjectFromShapeDrawing(...);
Set2DViewCreateObjectItemLineType(SOLID);   // 복원
```

**After**: 단일 SOLID
```csharp
Set2DViewCreateObjectItemLineType(SOLID);   // T-046
Add2DObjectFromShapeDrawing(...);
```

복원 호출은 변경 후 `SOLID → SOLID` 무의미해서 제거.

## 5. SDK 조사 결과 요약 (sdk-verifier)

XML 위치: `lib/VIZCore3D.NET.xml`

**LineType 관련** (작업 1)
- `Drawing2DObjectManager.Set2DViewCreateObjectItemLineType(Object2D_LineTypes)` (L28722) — 생성 시 기본값 설정
- `Drawing2DObjectManager.Set2DViewObjectItemLineType(int objID, int lineType)` (L28755) — 이미 생성된 객체 변경
- `Object2D_LineTypes` enum (L3384~3427): `NONE`, **`SOLID`**, `SHORT_DASHED`, `LONG_DASHED`, `DOTTED`, `DASHED_DOTTED`, `DASHED_DOUBLEDOTTED`, `MAX`

**보조선 offset/gap** (작업 2): SDK 직접 미지원
- `ExtensionLine` / `ExtLine` / `GuideLine` 키워드 검색 결과 0건
- `Origin` / `Extend` / `Padding` / `Margin` 류 모두 다른 용도 (3D 좌표 offset, 갭 측정 데이터 입력 등)
- `MeasureStyle.AssistantLine*` 옵션은 3D Review.Measure 컨텍스트 전용 — 우리 ShapeDrawing 보조선과 무관 (이미 `AssistantLine = false`로 꺼두고 있음)
- **결론**: 우리가 직접 `vizcore3d.ShapeDrawing.AddLine(extensionLines, ...)`로 그리는 라인 컬렉션이라 시작점 좌표를 보정하는 우회로 가능

## 6. 영향 받는 4경로

| 경로 | 호출 함수 | 보조선 컬렉션 변수 | DrawDimension 거침 |
|---|---|---|---|
| 치수추출 | `CompleteMainDimensionPostClash` → `ShowAllDimensions` (forDrawing2D=false) | `extensionLines` (`Form1.Dimensions.cs:492`) | ✅ |
| 일반 시트 (2D 출력) | `LvDrawingSheet_SelectedIndexChanged` → `ShowAllDimensions(forDrawing2D=true)` | `extensionLines` | ✅ |
| 글로벌 X/Y/Z | `btnShowAxisX/Y/Z` → `ShowAllDimensions(viewDirection)` | `extensionLines` | ✅ |
| 가공도 메인 | `ExecuteMfgDrawing` (정면뷰) | `mfgExtLines` (`Form1.MfgDrawing.cs:352, 1095`) | ✅ |
| 가공도 EA 두 번째 뷰 | `ExecuteMfgDrawing` (EA L자 펼침뷰) | `eaExtLines` (`Form1.MfgDrawing.cs:1759`) | ✅ |

**모두 `DrawDimension(... extensionLines, ...)`을 거치므로 단일 변경으로 일관 적용**.

## 7. 검증 결과

- MSBuild Debug: 0 errors, A2Z.exe 산출 ✅
- 사용자 실기 확인 대기:
  - 보조선이 모델 표면에서 1mm 떨어진 채 시작하는지 시각 확인
  - 모든 경로(치수추출 / 일반 시트 2D 출력 / 글로벌 X/Y/Z / 가공도)에서 일관 적용 확인
  - 1mm가 적정한지 (너무 작아 안 보임 / 너무 커 부자연스러움 → 조정 시 `Form1.Dimensions.cs`의 `ExtensionLineGap` 상수 한 줄만 변경)

## 8. 후속 조정 포인트

- **gap 값 조정**: `private const float ExtensionLineGap = 1.0f;` 한 줄만 변경 (예: 0.5f / 2.0f)
- **상대 비율로 전환**: 모델 BBox 대비 비율(예: 0.3%)이 더 안정적이면 `OffsetTowardLineEnd` 호출부에서 동적 계산 가능
- **gap 끄기**: `ExtensionLineGap = 0.0f`로 하면 헬퍼가 from 그대로 반환 (이전 동작과 동일)

## 9. 인용 코드

| 위치 | 역할 |
|---|---|
| `Form1.Dimensions.cs:983~1100` | `DrawDimension` 본문 |
| `Form1.Dimensions.cs:1089~1099` | 보조선 추가 — gap 적용 지점 |
| `Form1.Dimensions.cs:~1102` (신설) | `ExtensionLineGap` 상수 |
| `Form1.Dimensions.cs:~1108` (신설) | `OffsetTowardLineEnd` 헬퍼 |
| `Form1.Dimensions.cs:492, 569~580` | `extensionLines` 수집 + `ShapeDrawing.AddLine` 호출 |
| `Form1.MfgDrawing.cs:1538~1545` | 가공도 메인 보조선 → 2D 변환 (SOLID) |
| `Form1.MfgDrawing.cs:1896~1903` | 가공도 EA 두 번째 뷰 보조선 → 2D 변환 (SOLID) |
| `Form1.MfgDrawing.cs:352, 1095, 1759` | 가공도 보조선 컬렉션 변수 (모두 DrawDimension 거침) |

## 10. 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-05-02 | 최초 작성 — T-046 확장 (모든 보조선 SOLID + gap 1mm) 적용 후 통합 사양 정리 |
| 2026-05-02 | gap 1mm → **10mm** 상향. 사용자 실기 결과 1mm는 시각적으로 식별 어려움. 가공도 보조선 오프셋(100~300mm) 대비 10% 수준으로 조정해 명확히 인식 가능 |
