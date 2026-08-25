# 전체 흐름 — 모델 열기부터 PDF까지

> **코드를 따라가는 지도.** 파일·줄번호를 그대로 VS에 치면 그 자리로 간다.
> 기준 코드 2026-08-25. 죽은 코드 1,415줄 삭제 후.

---

## 한눈에

```
[사람]  파일 열기 → STRU 체크 → 「도면 일괄 출력」 클릭
                                      │
[프로그램]                            ▼
        STRU 하나마다 반복 ┌──────────────────────────┐
                          │ ① 가시성 격리             │
                          │ ② BOM 수집               │
                          │ ③ 간섭검사 시작 ──┐       │
                          └───────────────────│───────┘
                                              │ 함수는 끝! (비동기)
                                        ⏸ ────┘
                                        ▼ 벨② 울림
                          ┌──────────────────────────┐
                          │ ④ Osnap 수집             │
                          │ ⑤ 치수 계산              │
                          │ ⑥ 시트 분할              │
                          └──────────┬───────────────┘
                                     │ (본체는 폴링으로 기다림)
                          ┌──────────▼───────────────┐
                          │ ⑦ 시트마다 도면 그리기     │
                          │ ⑧ 가공도                 │
                          │ ⑨ PDF 저장               │
                          └──────────────────────────┘
```

---

## 0. 파일 열기 — `Form1.BOM.cs:182`

`btnOpen_Click` ← 「파일 열기」 버튼

| 줄 | 무엇 |
|---|---|
| L184~192 | `OpenFileDialog` 로 `.vizx`/`.viz` 고르기. 취소면 `return` |
| L204~226 | **공유 필드 23줄 `.Clear()`** — 옛 모델 흔적 제거 |
| L227~230 | `Model.IsOpen()` 이면 `Model.Close()` → `Model.Open(경로)` |
| L232~236 | `View.FitToView()` · `SilhouetteEdge = true` |
| **L245** | **`BuildBodyToPartNameMap()`** → `Form1.BOM.cs:46` |
| **L248** | **`PopulateStruCheckList()`** → `Form1.Stru.cs:149` |

이 시점 상태: **모델 전체가 보임 · STRU 목록은 채워졌으나 전부 체크 해제 · 격리 없음.**

## 0-1. STRU 체크 — `Form1.Stru.cs:362`

`ClbStruList_ItemCheck` → `ItemCheckCore` (L386)

**강조(`Object3D.Select`)만 한다. 숨기지 않는다.** 카메라도 안 움직인다(주석: 사용자 요청).
체크는 "이걸 처리해라" 표시일 뿐, 화면 상태는 그대로다.

---

## 1. 「도면 일괄 출력」 — `Form1.Stru.cs:514`

`btnExtractDrawingList_Click`

| 무엇 | 내용 |
|---|---|
| 재진입 가드 | `_p2aInProgress` 면 무시 |
| 사전 검사 | 모델 없음 / STRU 미체크 → 경고 후 `return` |
| 대상 수집 | `clbStruList.CheckedIndices` → `checkedStrus` |
| 확인 | STRU 2개 이상이면 "도면 4종 일괄 생성합니다. 계속?" |
| 준비 | `_p2aInProgress = true` · `BeginCancelableOperation()` · `ShowBusyOverlay(...)` |
| 초기화 | 2D/3D 잔재 제거(`DeleteAllObjectBy2DView` 등) + 모델 전체 표시로 리셋 |
| **L664~700** | **STRU 루프** |

```csharp
ShowBusyOverlay($"STRU 처리 {s+1}/{checkedStrus.Count}: {stru.NodeName}");
try   { ProcessSingleStruFull(stru, saveDir, savedCount => totalPdfCount += savedCount); }
catch (OperationCanceledException ex) { cancelled = true; }   // 취소
catch (Exception ex)                  { failCount++; }        // 한 STRU 실패해도 다음 진행
string mergedPdfPath = FlushPendingMergedPdf();               // issue #119 — STRU 1개 = PDF 1개
```

📌 **한 STRU가 실패해도 다음으로 넘어간다.** 오류를 모아뒀다 마지막에 한 번에 보고한다.

---

## 2. STRU 하나 처리 — `Form1.Stru.cs:803`

`ProcessSingleStruFull(struNode, saveDir, reportPdfSaved)` → 반환 = PDF 장수

### ① 부재 수집 + 가시성 격리 (L810~853)

```csharp
var descendants = vizcore3d.Object3D.GetChildObject3d(
    struNode.Index, Object3DChildOption.ALL_CHILDREN, true);   // 후손 전부
var memberIndices = descendants
    .Where(b => b.Kind == NodeKind.BODY).Select(b => b.Index).ToList();

var allBodies = vizcore3d.Object3D.FromFilter(
    Object3dFilter.ALL_INCLUDE_BODY, false);                   // ★ false = 숨은 것도 포함

vizcore3d.BeginUpdate();
try {
    vizcore3d.Object3D.Show(allBodyIndices, false);   // ① 전체 숨김
    vizcore3d.Object3D.Show(memberIndices,  true);    // ② 이 STRU만 표시
} finally { vizcore3d.EndUpdate(); }
```

🔑 **이 두 줄이 뒤따르는 모든 단계의 작업 범위를 정한다.**
격리를 안 하면 다른 STRU 부재까지 "붙어 있다" 판정 → 컴포넌트 > 1 → **시트 생성 실패**(T-023).

### ② X-Ray 선택 비우기 (L856)

```csharp
xraySelectedNodeIndices.Clear();   // GetBOMTargetNodes 가 "Visible 기준" 갈래로 가게
```

### ③ BOM 수집

```csharp
ShowBusyOverlay($"BOM 수집 중: {struNode.NodeName}");
bool bomCollected = CollectBOMData(...);        // → Form1.BOM.cs
```

`GetBOMTargetNodes` 가 `realNode.Visible == true` 인 것만 담는다 → **격리한 STRU 부재만.**

### ④ 간섭검사 시작 — 🔔 **여기서 흐름이 끊긴다**

```csharp
ShowBusyOverlay($"간섭검사 실행 중: {struNode.NodeName}");
bool startResult = DetectClash(includeOutsideNeighbors: true);   // → Form1.Clash.cs:1021
```

**`DetectClash`는 검사를 SDK에 맡기고 곧바로 리턴한다.** 결과는 나중에 **벨②**로 온다.
등록 지점: `Form1.BOM.cs:166` — `vizcore3d.Clash.OnClashTestFinishedEvent += Clash_OnClashTestFinishedEvent`

### ⑤ 폴링 대기

```csharp
while (sw.ElapsedMilliseconds < 60000) {      // 최대 60초
    ...  Task.Delay(50)  ...                  // 벨②가 일을 끝낼 때까지
}
```

📌 **본체는 여기서 기다린다.** 그동안 벨② 쪽이 Osnap → 치수 → 시트를 다 만든다.

---

## 3. 🔔 벨② — `Form1.Clash.cs:1146`

`Clash_OnClashTestFinishedEvent(object sender, ClashEventArgs e)`

| 무엇 | 내용 |
|---|---|
| 대상 확인 | 다른 검사면 `return` (**`e.ID`** 로 구분) |
| L1165 | `AdvanceSilentClashSequence(e.ID)` — 여러 검사를 순서대로 |
| 연결성 판정 | 부재가 한 덩어리인가? 아니면 경고 후 중단 |
| **L1308** | **`CompleteMainDimensionPostClash(isSingleMember: false, clashTestCount: testCount)`** |

연결성 판정 실패 시 뜨는 경고:
> *"치수 추출은 모든 부재가 하나의 덩어리로 연결되어 있을 때만 가능합니다."*

### 3-1. 치수 사슬 — `Form1.BOM.cs` `CompleteMainDimensionPostClash`

| 순서 | 무엇 | 호출 |
|---|---|---|
| 1 | Osnap 수집 | `CollectAllOsnap()` → `_autoProcessOsnapSuccess` |
| 2 | 치수 계산 | `ComputeViewDimensionsForMembers(visibleMembers, null, tolerance, _lastCollectedNodeOsnapMap)` — 3뷰 × 2축 = 6조합 |
| 3 | **시트 분할** | `GenerateDrawingSheets()` → `drawingSheetList` 채움 |
| 4 | 정리 | `FinishMainDimensionOperation()` |

📌 `visibleMembers` — 또 **Visible 기준**이다. 격리가 여기까지 효력을 미친다.

---

## 4. 도면 그리기 — 다시 `ProcessSingleStruFull`

폴링이 풀리면 `drawingSheetList` 가 채워져 있다. 시트마다:

```csharp
foreach (ListViewItem sel in lvDrawingSheet.SelectedItems) sel.Selected = false;
ApplySheetSelection(sheet);        // = LvDrawingSheet_SelectedIndexChanged 본체 (이벤트 시뮬 X, 직접 호출)
GenerateSheetDrawing2D(sheet);     // = btnGenerateSheet2D_Click 흐름
```

📌 **이벤트를 흉내내지 않고 메서드를 직접 부른다.** 주석: *"시트당 200ms 단축 + 이벤트 타이밍 의존 제거."*

### `GenerateSheetDrawing2D` — `Form1.DrawingSheets.cs:1690`

```csharp
try     { GenerateSheetDrawing2DCore(sheet); }
finally { Clear3DDimensionAnnotations(); }     // 임시 3D 치수·보조선만 제거
```

### `GenerateSheetDrawing2DCore` — L1700

```csharp
vizcore3d.Object3D.GenerateEdgeData();                  // 히든라인 준비
GenerateSheetDrawing2D_WithExcelTemplate(sheet);        // ★ 심장
```

### 🫀 `GenerateSheetDrawing2D_WithExcelTemplate` — `Form1.DrawingSheets.cs:1799`

| 줄 | 무엇 |
|---|---|
| L1826~1830 | **시트 부재 가시성 격리** (또 격리다) |
| L1876 | `ResolveDrawingTemplatePath("제작도_도면.xlsx")` — 실행폴더 `templates/` → 없으면 솔루션 폴더 |
| L1877~1880 | 없으면 예외 |
| L1890 | `data` 딕셔너리 구성 — `{Input_N}` 슬롯에 넣을 값 (도면정보 + BOM 8열 × 15행) |
| **L1998** | **`Template.ImportExcelWithData(xlsxPath, data, imageMapping)`** — 엑셀이 종이를 그린다 |
| **L2012** | **`Template.GetViewAreasFromExcel(xlsxPath)`** — `{View_n}` 영역 좌표 파싱 |
| L2083~ | 뷰마다: 전체 숨김 → 시트 부재만 → 카메라 회전 → `Create2DViewObjectWithModelHiddenLine` → fit → `MoveObjectTo` |
| L2145~ | ISO 뷰 풍선 |
| L2209~ | X/Y/Z 뷰 치수 |

📌 **템플릿은 시트 한 장마다 읽는다** (장당 2회: Import + GetViewAreas). 프로그램 시작 시점이 아니다.

---

## 5. 가공도 — 다시 `ProcessSingleStruFull`

```csharp
var mfgSheets = ...;                              // 부재 있는 시트 전부
var mfgResult = GenerateMfgDrawingManual(...);    // → Form1.MfgDrawing.cs:2454
pdfCount += mfgResult.SuccessPdfs;
```

**수동 경로(`btnMfgDrawingSheet_Click`)와 같은 함수를 쓴다.** 저장 위치만 STRU 폴더로 바뀐다.

## 6. PDF 저장

- 시트 도면: 2D 캔버스 → `Export2PDFBy2DView` (`Form1.Drawing2D.cs`)
- **STRU 1개 = PDF 1개** — `FlushPendingMergedPdf()` (`Form1.Stru.cs` 루프 끝, issue #119)
- 파일명: `{STRU}_생산제작도.pdf` (#49, 일괄 출력 전용)

---

## 🔑 이 흐름에서 꼭 아는 3가지

### 1. 격리가 세 번 일어난다

| 어디 | 무엇만 남기나 |
|---|---|
| `Stru.cs:840` | 이 STRU의 BODY |
| `DrawingSheets.cs:1826` | 이 시트의 부재 |
| `DrawingSheets.cs:2083 · 2268 · 2330` | 뷰마다 (점선 배경 / 실선 전경 분리) |

**`Show(전체, false)` → `Show(대상, true)` 두 줄 짝**이 계속 반복된다 → **중복 통합 후보.**

### 2. 흐름이 벨②에서 끊긴다

```
ProcessSingleStruFull  ──DetectClash──▶ 함수 진행 (폴링 대기)
                                              ⏸
                       벨② ──▶ Osnap → 치수 → 시트  (별도로 진행)
                                              ▼
ProcessSingleStruFull  ◀── 폴링 풀림 ── drawingSheetList 채워짐
```

**`Shift+F12`로는 이 연결이 안 보인다.** `DetectClash` 안에 답이 없고,
`+=` 등록 지점(`Form1.BOM.cs:166`)을 봐야 이어진다.

### 3. 작업 범위는 언제나 `Visible`

`GetBOMTargetNodes`(BOM.cs) · `CompleteMainDimensionPostClash`의 `visibleMembers` ·
`_WithExcelTemplate`의 시트 격리 — **전부 "지금 보이는 것"** 기준이다.
→ 리팩토링 「공유 상태 묶기」가 이걸 명시적 데이터(`DrawingJobContext`)로 빼려는 이유.

---

## VS에서 따라가는 순서

```
1. Form1.BOM.cs:182              btnOpen_Click                     ← 여기서 시작
2. Form1.Stru.cs:514             btnExtractDrawingList_Click
3. Form1.Stru.cs:803             ProcessSingleStruFull             ← 파이프라인 본체
4. Form1.Clash.cs:1146           Clash_OnClashTestFinishedEvent    ← 벨②
5. Form1.BOM.cs                  CompleteMainDimensionPostClash    ← 치수 사슬
6. Form1.DrawingSheets.cs:1799   GenerateSheetDrawing2D_WithExcelTemplate  ← 심장
7. Form1.MfgDrawing.cs:2454      GenerateMfgDrawingManual          ← 가공도
```

**실행하면서 로그를 같이 보면 제일 빠르다** — `exe폴더/logs/diag-날짜.log` 에
`T-064 STRU ... 가시성 격리 — allBody=5000, STRU=40` 같은 줄이 순서대로 찍힌다.
