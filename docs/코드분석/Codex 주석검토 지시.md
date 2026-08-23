# Codex 지시 — 메서드 요약 주석 검토 (4차)

2026-08-23. 전수조사·교차검증·3차 검증을 마친 뒤, **코드 자체에 메서드 요약 주석을 다는 작업**을 했다.
그 결과물을 검토해 달라. 지금까지와 같은 상호검증 프로토콜을 그대로 쓴다.

## 무슨 일이 있었나

1. **죽은 코드 삭제 완료** — 확정 8건 + 후속 #11, 실측 **1,301줄** 삭제. 코드 20,578 → **19,143줄**.
   가장 큰 건은 **#7 옛 무템플릿 도면 경로 733줄**(`GenerateSheetDrawing2DCore` 옛 본문 + `RenderSheetViewForDrawing` + `EstimateFitScaleForCell` + 스위치 `UseExcelTemplate`).
   부수 효과로 **SDK `GridStructure.*` API 호출이 22 → 0회**가 됐다 (옛 경로 전용이었음).
   커밋: `134057d` `dca8fde` `b4f76d2` — 각 건 MSBuild Debug 통과.
   → 상세는 [`판정/죽은 코드.md`](./판정/죽은%20코드.md)

2. **메서드 요약 주석 118개 추가** — 설명 주석이 없던 메서드 전부에 `/// <summary>` 1~3줄.
   초안(에이전트) → 독립 검증(다른 에이전트가 코드 본문 대조) 2단을 거쳤고, 검증에서 이미 여러 건이 정정됐다.
   **당신은 3단째 눈이다.**

## 검토 대상

`git log`에서 주석 추가 커밋의 diff를 보면 된다 (커밋 메시지: "메서드 요약 주석"). 소스는 `A2Z/*.cs`.

### ① 전수 검토 — 당신이 분석 문서를 쓴 파일 (40건)

| 파일 | 건수 |
|---|---|
| `Form1.Clash.cs` | 17 |
| `Form1.GlobalViews.cs` | 14 |
| `Form1.BOM.cs` | 6 |
| `Form1.Stru.cs` | 3 |

이 파일들은 당신이 6절 분석 문서를 직접 썼으니 맥락을 가장 잘 안다. **전부** 본다.

### ② 표본 감사 — 내가 문서를 쓴 파일에서 무작위 20건 (seed 20260823)

| 파일 | 메서드 |
|---|---|
| `Form1.Dimensions.cs` | `MarkNonRightAngles` |
| `Form1.Drawing2D.cs` | `Clear2DView` |
| `Form1.DrawingSheets.cs` | `CaptureDrawingExportControlStates` |
| `Form1.DrawingSheets.cs` | `DrawingReferenceWorldToLocal` |
| `Form1.DrawingSheets.cs` | `GetInstallationNoteLabelPoint` |
| `Form1.DrawingSheets.cs` | `ReleaseActiveDrawingReferenceAxis` |
| `Form1.DrawingSheets.cs` | `SetDrawingExportControlsEnabled` |
| `Form1.DrawingSheets.cs` | `ShowDrawingSheetExportResult` |
| `Form1.DrawingSheets.cs` | `btnExportFabricationSheets_Click` |
| `Form1.DrawingSheets.cs` | `btnExportInstallationSheets_Click` |
| `Form1.MfgDrawing.cs` | `CanonicalizeMfgAxisVector` |
| `Form1.MfgDrawing.cs` | `ClearMfgViewAnnotations` |
| `Form1.MfgDrawing.cs` | `DotMfgVector` |
| `Form1.MfgDrawing.cs` | `GetMfgEaSecondaryViewDirection` |
| `Form1.MfgDrawing.cs` | `LogMfgAxisDetection` |
| `Form1.MfgDrawing.cs` | `TryNormalizeMfgVector` |
| `Form1.cs` | `DiagLog` |
| `Form1.cs` | `IsCancellationRequested` |
| `Form1.cs` | `SendMessage` |
| `Form1.cs` | `ThrowIfCancellationRequested` |

표본이니 **오류율을 셀 수 있게** 20건 전부 판정해 달라 (일치 N / 불일치 M).

## 판정 기준

메서드 **본문을 직접 읽고** 대조한다. 주석을 믿지 말 것.

| # | 항목 | 무엇을 보나 |
|---|---|---|
| ① | **사실** | 본문이 실제로 그 일을 하는가? 반환값·null/−1 케이스·부수효과(공유 상태 쓰기, SDK 호출)가 맞는가? |
| ② | **과장·추측** | 본문에 없는 동작·목적을 적었는가? `(추정)` 표시 없이 불확실한 주장을 했는가? |
| ③ | **호출처** | "누가 부른다"고 적힌 게 맞는가? 빠진 주요 호출처가 있는가? |
| ④ | **규칙** | 첫 줄이 동작 기준 한 문장인가? 변경 이력·줄번호·변수명 나열·영어 문장이 없는가? 3줄·90자 이내인가? |
| ⑤ | **누락** | 주석이 안 붙은 메서드가 담당 파일에 남아 있는가? |

작성 규칙(참고): 한국어, 첫 줄 = "무엇을 받아 → 무엇을 하고 / 무엇을 돌려준다",
코드 용어 그대로(STRU·BOM·Osnap·체인치수·가공도/제작도/조립도/설치도·풍선·시트·viewArea·UDA·X-Ray),
이벤트 핸들러는 `[버튼 라벨] 클릭 →` 로 시작.

## 산출물 — `판정/주석 검토.md`

새 파일을 만들고 아래 표로 적는다.

| # | 파일 | 메서드 | 구분 | 지적 | 제안 문구 | 판정 |
|---|---|---|---|---|---|---|
| 1 | | | 사실/과장/호출처/규칙/누락 | 무엇이 틀렸는지 | 이렇게 고치자 | (비워둠) |

끝에 **집계**를 남긴다: ① 전수 40건 중 지적 N건 / ② 표본 20건 중 일치 N·불일치 M (오류율 %).

## 프로토콜 (지금까지와 동일)

- 🔴 **`A2Z/*.cs` 소스는 수정하지 마라.** 지적만 표에 적는다. 코드 반영은 내가 한 곳에서 한다 (충돌 방지).
- 판정 칸은 비워둔다 — 내가 채운다.
- 확신이 없으면 지적하되 "확인 필요"로 표시. 오탐이 나오는 게 놓치는 것보다 낫다.
- 다 끝나면 집계만 알려달라.

## 맥락 참고

- 분석 문서 13개: [`파일별/`](./파일별/) — 6절 구조(진입점/실행 흐름/상태/의존/알고리즘/책임)
- 버튼 라벨 대조: [`자동생성/버튼별 코드 위치.md`](./자동생성/버튼별%20코드%20위치.md)
- 죽은 코드 삭제 내역: [`판정/죽은 코드.md`](./판정/죽은%20코드.md)
- 목적: **2026-08-27(목) 코드 리뷰 발표.** 주석은 발표 때 "코드를 열면 모든 함수에 설명이 보인다"는 상태를 만들기 위한 것이다. **틀린 주석은 없느니만 못하다** — 그래서 3단 검증을 한다.
