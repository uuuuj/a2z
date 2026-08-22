---
파일: A2Z/Form1.Stru.cs
줄수: 1,110
작성: 2026-08-22 (코드 전수 정독)
---

# Form1.Stru.cs — STRU 탐색·강조와 도면 일괄 출력

**한 줄**: 모델 트리 규칙으로 STRU를 찾아 선택·검색하게 하고, 체크된 STRU마다 다른 BODY를 숨긴 뒤 기존 **BOM → Clash → 치수 → 시트 → 2D → 가공도** 경로를 반복해 STRU당 PDF 하나로 묶는다.

---

## 1. 진입점 — 언제 도는가

### 앱 시작과 모델 열기

| 경로 | 메서드 | 위치 |
|---|---|---:|
| 앱 생성자 | `InitStruSearchUI` | L187 |
| 모델 열기 성공 | `PopulateStruCheckList` | L149 |

검색창과 검색 버튼은 Designer가 아니라 코드로 생성한다. 모델을 열면 STRU 후보를 다시 추출해 체크 목록과 자동완성 소스를 채운다.

### 화면에서 직접 시작

| 화면 동작 | 핸들러 | 위치 | 결과 |
|---|---|---:|---|
| **전체 선택/해제** | `btnSelectAllStru_Click` | L342 | 모두 체크돼 있으면 전부 해제, 아니면 전부 체크 |
| **도면 일괄 출력** | `btnExtractDrawingList_Click` | L499 | 체크된 STRU의 도면 4종을 순차 생성해 PDF로 저장 |
| 코드 생성 **검색** 버튼 | `BtnStruSearch_Click` | L246 | 이름으로 STRU를 찾아 그 BODY만 보이게 격리 |
| STRU 체크박스 변경 | `ClbStruList_ItemCheck` | L357 | 체크될 STRU BODY 합집합을 3D에서 선택 강조 |
| STRU 행 이름 선택 | `ClbStruList_SelectedIndexChanged` | L433 | 강조는 유지하고 그 STRU BODY를 보이게 한 뒤 화면 맞춤 |

버튼 둘과 목록 이벤트는 Designer에서, 검색 버튼은 `InitStruSearchUI`에서 직접 배선된다.

다른 파일에서는 `GetSheetKindLabel`(L1078)과 `GetSheetKindLabelWithSequence`(L1095)를 도면 목록·표제부 종류명에 사용한다.

---

## 2. 실행 순서 — 무엇이 어떤 순서로

### 2-1. STRU 목록 구성

1. **`CollectStruList`** (L62) — SDK에서 모든 Assembly와 NodePath를 가져온다.
2. **`RuleByFrameworkChildParent`** (L128) — 이름이 `FRMWORK `로 시작하는 Assembly의 바로 위 부모 인덱스를 STRU로 채택한다.
3. 중복 부모를 제거하고 NodeName 오름차순으로 정렬한다 (L85~97).
4. 한 건도 못 찾으면 이름이 `/`로 시작하고 공백이 없는 Assembly 전체를 fallback 목록으로 쓴다 (L99~110).
5. **`PopulateStruCheckList`** (L149) — `_struNodeCache`, 체크 목록, 검색 자동완성, 개수 라벨을 함께 갱신한다.

**그래서 화면에는** 모델링 컨벤션상 STRU로 보이는 Assembly 목록이 이름순으로 나타난다.

### 2-2. 검색·체크·행 선택

1. **`SearchStruByName`** (L256) — 완전일치를 먼저 찾고, 없으면 이름순 목록의 첫 부분일치를 고른다.
2. 선택 STRU의 모든 하위 BODY를 구한 뒤 전체 BODY를 숨기고 선택 STRU BODY만 보인다 (L282~305).
3. 체크 목록의 같은 행을 선택하고 **`PerformFlyToSelectedStru`** (L442)로 화면을 맞춘다.
4. 체크박스는 **`ItemCheckCore`** (L375)에서 변경 후의 미래 체크 집합을 계산하고, 그 STRU들의 하위 BODY 합집합을 선택 강조한다.
5. 행 이름 선택은 **`PerformFlyToSelectedStru`** (L442)에서 해당 BODY를 보이게 하고 `FlyToObject3d(..., 1.2f)`를 호출한다. 다른 BODY 가시성·체크 강조색은 건드리지 않는다.

체크 클릭도 WinForms 행 선택 이벤트를 만들기 때문에 `_suppressStruSelChanged`와 `BeginInvoke`로 같은 클릭의 카메라 이동을 막는다.

### 2-3. 도면 일괄 출력 준비

1. **`btnExtractDrawingList_Click`** (L499) — 재진입, 모델, 체크 STRU를 확인한다.
2. 출력 폴더를 실행 파일 아래 `Drawings`로 고정하고 없으면 만든다. 여러 STRU면 한 번 확인을 받는다 (L527~550).
3. `_p2aInProgress`와 취소 가능 상태를 켜고 일괄 출력·치수 추출 버튼을 비활성화한다 (L553~558).
4. 전체 BODY를 다시 보이고, 기존 2D 객체·3D 풍선·치수·보조선·시트·치수·BOM 표·X-Ray·Osnap/UDA 캐시를 정리한다 (L560~604).
5. 로고 파일이 있으면 SDK 템플릿의 일반/배경반전 이미지로 같은 파일을 등록한다 (L616~631).
6. 체크 목록 순서대로 **`ProcessSingleStruFull`** (L788)을 호출한다. 한 STRU가 실패해도 다음 STRU를 계속한다.

### 2-4. STRU 하나의 BOM·Clash·시트 생성

1. **`ProcessSingleStruFull`** (L788) — STRU의 모든 하위 BODY를 수집한다.
2. 모델의 전체 BODY를 숨기고 현재 STRU BODY만 보인다. 부모 Part/Assembly는 건드리지 않는다 (L812~838).
3. X-Ray 목록을 비우고 **`CollectBOMData`** (`Form1.BOM.cs` L829)를 호출해 보이는 STRU BODY만 `bomList`에 넣는다.
4. **`DetectClash(includeOutsideNeighbors: true)`** (`Form1.Clash.cs` L1004)를 시작한다.
5. 시작하지 못하면 **`GenerateDrawingSheets`** (`Form1.DrawingSheets.cs` L20)를 직접 시도한다.
6. 시작했으면 300ms 기다린 뒤, `Clash.IsBusy == false`이고 `drawingSheetList.Count > 0`이 될 때까지 50ms 간격으로 최대 60초 폴링한다. 완료 후 다시 300ms 기다린다 (L877~920).
7. 시트가 하나도 없으면 해당 STRU를 실패 처리한다 (L924~926).

**그래서 이 시점에는** 현재 STRU 전용 BOM·간섭·치수·도면 시트 목록이 준비된다.

### 2-5. 시트별 2D 생성과 PDF 누적

1. STRU 이름을 파일명에 안전한 문자열로 바꾸고 `Drawings/<STRU>/` 폴더와 `<STRU>_생산제작도.pdf` 경로를 만든다 (L930~949).
2. **`BeginPdfPageAccumulation`** (`Form1.Drawing2D.cs` L990)으로 여러 캔버스를 한 PDF로 모으는 상태를 시작한다.
3. 일반 시트(제작·조립·설치)는 목록 행을 선택하고 **`ApplySheetSelection`** (`Form1.DrawingSheets.cs` L636)으로 3D 범위·카메라·치수·BOM을 적용한다.
4. **`GenerateSheetDrawing2D`** (`Form1.DrawingSheets.cs` L1707)로 2D 캔버스를 만들고 선택 표시를 해제한다. 성공 캔버스는 저장하지 않고 누적한다.
5. 한 시트가 실패하면 **`DiscardCurrentPdfPage`** (`Form1.Drawing2D.cs` L1081)로 반쪽 페이지를 버리고 다음 시트로 간다.
6. 가공도 시트는 모아서 **`GenerateMfgDrawingManual`** (`Form1.MfgDrawing.cs` L2486)에 한 번 맡기고, 생성된 페이지를 같은 누적에 더한다.
7. 호출부가 STRU 처리를 마친 직후 **`FlushPendingMergedPdf`** (`Form1.Drawing2D.cs` L1154)로 그때까지 성공한 페이지를 PDF 하나로 저장한다. 취소·실패가 중간에 발생해도 완성된 앞 페이지는 남긴다.

### 2-6. STRU 사이와 전체 종료

1. STRU마다 2D 객체를 지우고 `GC.Collect`·`DoEvents`·100ms 대기 후 다음 STRU로 간다 (L689~696).
2. 취소 시 부분 작업을 정리한다 (L705~711).
3. `finally`에서 모든 BODY를 보이게 하고, 늦은 Clash 이벤트가 도착할 시간을 500ms 둔 뒤 `_p2aInProgress`와 버튼·진행창을 복원한다 (L713~752).
4. 성공·실패·미처리 STRU와 생성 PDF 수를 한 번만 보여준다 (L755~771).

---

## 3. 상태 — 무엇을 읽고 무엇을 쓰나

### 이 파일이 선언하는 상태

| 필드 | 역할 | 파일 밖 사용 |
|---|---|---|
| `_struNodeCache` | 현재 모델에서 찾은 STRU Node 목록. 체크 목록 인덱스와 1:1 | 없음 |
| `txtStruSearch` | 코드로 만든 검색 입력창 | 없음 |
| `_suppressStruSelChanged` | 체크 클릭이 만든 행 선택 이벤트의 카메라 이동 차단 | 없음 |
| `_p2aInProgress` | STRU 일괄 출력 재진입·메시지박스·완료 이벤트 가드 | `Form1.BOM.cs`와 `Form1.Clash.cs`도 읽음 |

### Form1.cs에서 공유하는 상태

| 필드 | 읽기/쓰기 | 역할 |
|---|---|---|
| ⚠ `vizcore3d` | 읽기·쓰기 | STRU 계층, BODY 표시/선택, 카메라, Clash 상태, 2D 객체·템플릿 |
| ⚠ `bomList` | 쓰기 | STRU별 BOM으로 교체 |
| ⚠ `xraySelectedNodeIndices` | 쓰기 | STRU 격리는 가시성으로 하므로 매번 비움 |
| ⚠ `drawingSheetList` | 읽기·쓰기 | 비동기 완료 신호이자 출력 대상 시트 |
| ⚠ `chainDimensionList` | 쓰기 | 시작·취소 정리 |
| ⚠ `_lastCollectedNodeOsnapMap` / `_udaValueCache` | 쓰기 | STRU 전환 전 캐시 제거 |

진행·취소 필드 `_cancelRequested`와 공용 진행창도 사용한다. `Form1.Drawing2D.cs`에 선언된 PDF 누적 상태와 `Form1.DrawingSheets.cs`의 시트 선택 억제 상태는 메서드 호출을 통해 간접 사용한다.

### 화면과 파일 상태

- `clbStruList`: 표시 순서, 체크 집합, 현재 행 선택
- `lvDrawingSheet`: STRU별 생성 시트와 출력 순서
- 출력 루트: `Application.StartupPath/Drawings`
- 출력 단위: `Drawings/<STRU>/<STRU>_생산제작도.pdf`

---

## 4. 외부 호출

### VIZCore3D SDK API

`VIZCore3D.NET.xml`에서 아래 멤버를 확인했다.

| API | 이 파일에서 쓰는 이유 |
|---|---|
| `Object3D.FromFilter(ASSEMBLY, true)` | Assembly 전체와 NodePath 조회 |
| `Object3D.FromFilter(ALL_INCLUDE_BODY, false)` | 현재 가시성과 무관한 전체 BODY 모수 |
| `Object3D.GetChildObject3d(index, ALL_CHILDREN, true)` | STRU 아래 모든 BODY 조회 |
| `Object3D.Show(List<int>, bool)` | STRU 가시성 격리와 종료 후 전체 복원 |
| `Object3D.Select(List<int>, true, false)` | 체크 STRU BODY 합집합 강조 |
| `Object3D.Color.RestoreColorAll()` | 기존 전체 색상 초기화 |
| `View.FlyToObject3d(List<int>, 1.2f)` | 행 선택 STRU에 화면 맞춤 |
| `Clash.IsBusy` | 비동기 간섭검사의 SDK 내부 작업 상태 확인 |
| `Drawing2D.Template.Set2DViewTemplateMark(normal, reverse)` | 일반/배경반전 템플릿 이미지 등록 |
| `Drawing2D.Object2D.DeleteAllObjectBy2DView` / `DeleteAllNonObjectBy2DView` | 시작·STRU 사이 2D 메모리 정리 |
| `UnselectAllObjectBy2DView` / `UnselectCurrentWorkObjectBy2DView` | 완성 페이지의 선택 테두리 제거 |

### 다른 Form1 파일

| 메서드 | 위치 | 맡기는 일 |
|---|---|---|
| `CollectBOMData` | `Form1.BOM.cs` L829 | 현재 보이는 STRU BODY의 기본 BOM 수집 |
| `DetectClash` | `Form1.Clash.cs` L1004 | STRU 내부 연결성과 외부 접합 검사 |
| `GenerateDrawingSheets` | `Form1.DrawingSheets.cs` L20 | Clash 결과에서 도면 시트 목록 생성 |
| `ApplySheetSelection` / `GenerateSheetDrawing2D` | `Form1.DrawingSheets.cs` L636 / L1707 | 시트 상태 적용과 2D 캔버스 생성 |
| `GenerateMfgDrawingManual` | `Form1.MfgDrawing.cs` L2486 | 가공도 페이지 묶음 생성 |
| `BuildMergedDrawingPdfPath` / `BeginPdfPageAccumulation` / `FlushPendingMergedPdf` | `Form1.Drawing2D.cs` L1190 / L990 / L1154 | STRU당 PDF 경로와 페이지 누적·최종 저장 |
| `DiscardCurrentPdfPage` / `CleanupBetweenPdfPages` | `Form1.Drawing2D.cs` L1081 / L1100 | 실패 페이지 제거와 캔버스 유지 상태 정리 |
| `ResolveDrawingAssetPath` / `SanitizeFileName` | `Form1.DrawingSheets.cs` L3790 / L1640 | 로고 경로와 안전한 폴더·파일명 |
| `ClearCanceledOperationArtifacts` | `Form1.BOM.cs` L491 | 취소 시 부분 2D/3D·목록·캐시 제거 |
| `DiagLog`·`BeginCancelableOperation`·`ThrowIfCancellationRequested` | `Form1.cs` | 진단·진행창·협력적 취소 |

---

## 5. 알고리즘 — 자명하지 않은 계산

### 5-1. STRU는 모델링 이름 규칙으로 추론한다

SDK에 이 프로젝트의 “STRU” 타입이 따로 없으므로 Assembly 이름과 부모 관계를 이용한다.

```
이름이 "FRMWORK "로 시작하는 Assembly
→ ParentIndex 한 단계
→ 그 부모를 STRU로 채택
→ 여러 FRMWORK가 같은 부모를 가리키면 HashSet으로 중복 제거
```

이 규칙이 0건이면 `/`로 시작하고 공백이 없는 Assembly를 전부 후보로 보여준다. 즉 STRU 식별은 SDK의 구조 타입이 아니라 **사용자 모델링 컨벤션을 코드로 복원한 층**이다.

### 5-2. 체크 이벤트는 “변경 전”에 오므로 미래 집합을 만든다

WinForms `ItemCheck` 시점의 `CheckedIndices`에는 아직 새 상태가 반영되지 않는다. 현재 체크 인덱스를 복사한 뒤 `e.NewValue`에 따라 클릭 행을 추가/제거해 미래 집합을 만든다. 각 STRU의 하위 BODY 합집합을 구해 전체 색을 복원한 뒤 다시 선택한다.

체크 클릭은 행 선택도 발생시킨다. `ItemCheck`에서 억제 플래그를 세우고 두 이벤트의 `BeginInvoke` 순서를 이용해 체크로 인한 카메라 이동만 막는다. 이름 클릭은 억제되지 않아 `FlyToObject3d`가 실행된다.

### 5-3. 검색은 화면 선택이 아니라 실제 가시성을 격리한다

완전일치 우선, 부분일치는 이름순 첫 항목이다. 검색 성공 시 전체 BODY를 숨기고 STRU BODY만 다시 보인다. 이 상태는 유지되므로 이후 사용자가 **치수 추출**을 누르면 공용 BOM 수집의 “보이는 BODY만” 규칙으로 같은 STRU만 처리된다.

### 5-4. 기존 수동 경로를 조립해 자동 출력한다

일괄 출력은 별도 도면 알고리즘을 다시 구현하지 않는다.

```
STRU 가시성 격리
→ CollectBOMData
→ DetectClash
→ SDK 완료 이벤트가 CompleteMainDimensionPostClash 호출
→ GenerateDrawingSheets
→ ApplySheetSelection
→ GenerateSheetDrawing2D / GenerateMfgDrawingManual
→ PDF 누적 저장
```

파일이 긴 이유는 실제 도면 계산보다 **기존 비동기·UI 중심 기능들을 STRU 순차 작업으로 안전하게 연결하고 중간 상태를 정리하는 조립 코드**가 많기 때문이다.

### 5-5. SDK 비동기를 동기 흐름처럼 기다린다

`DetectClash` 반환 직후 SDK `IsBusy`가 아직 false일 수 있고, 완료 이벤트는 `IsBusy`가 false가 된 뒤에도 시트 목록을 채우는 중일 수 있다. 그래서 다음 네 조건을 겹친다.

1. 시작 뒤 300ms 고정 대기
2. `Application.DoEvents()`로 UI 메시지와 완료 이벤트 처리
3. `!Clash.IsBusy && drawingSheetList.Count > 0`를 절대 완료 조건으로 사용
4. 완료 조건 뒤 다시 300ms 고정 대기

60초가 지나면 실패한다. SDK 작업 하나를 `await`할 Task나 완료 결과로 직접 이어주는 API가 없어 이벤트와 폴링을 접착한 우회다.

### 5-6. 가시성은 BODY만 조작한다

전체 BODY를 숨긴 뒤 STRU BODY만 보인다. Part/Assembly까지 숨기면 부모가 숨은 상태에서 자식 BODY를 보이게 해도 실제로 나타나지 않는 충돌이 있어, BODY 플래그만 조작하도록 재설계됐다는 주석이 있다.

### 5-7. PDF는 “파일”이 아니라 “페이지 누적”으로 센다

일반 시트와 가공도는 각각 2D 캔버스를 만들지만 즉시 저장하지 않는다. 성공한 캔버스를 누적하고 한 STRU가 끝날 때 한 번만 내보낸다. 시트 하나가 실패하면 현재 캔버스만 버려 앞의 정상 페이지를 보존하고, 취소도 그때까지 완성된 페이지를 저장한다.

### 5-8. 시트 종류는 음수 센티넬로 구분한다

| `BaseMemberIndex` | 종류 |
|---:|---|
| `-1` | 제작도 |
| `-2` | 설치도 |
| `-3` | 가공도 |
| `>= 0` | 조립도 |

같은 종류가 여러 장이면 도면 목록 전체에서의 순서를 `조립도 (2/5)`처럼 붙인다. 가공도는 자체 페이지 번호가 있어 여기서 제외한다.

---

## 6. 의심 — 확인이 필요한 것

| 표시 | 내용 |
|---|---|
| 🔴 | 자동 출력에서 `lvi.Selected = true`는 배선된 `LvDrawingSheet_SelectedIndexChanged`를 발생시켜 이미 `ApplySheetSelection`을 호출한다. 바로 뒤 L978에서 같은 메서드를 직접 다시 호출하지만 `_suppressSheetSelectionHandler`를 켜지 않는다. 시트 적용·치수·BOM 수집이 **두 번 실행될 가능성**이 높다. |
| 🔴 | 연결되지 않은 STRU는 Clash 완료 이벤트에서 시트를 만들지 않고 반환한다. 일괄 출력은 그 사유를 직접 받지 못하고 `drawingSheetList.Count > 0`만 기다리므로, 메시지도 없이 STRU마다 **60초 타임아웃**으로 끝난다. |
| 🔴 | `DetectClash`가 false면 단일 부재·SDK 실패·테스트 시작 실패를 구분하지 않고 `GenerateDrawingSheets`를 직접 호출한다 (L863~873). 이 경로는 `CompleteMainDimensionPostClash`의 Osnap·체인 치수 계산과 연결성 판정을 건너뛴다. |
| 🔴 | `ProcessSingleStruFull`의 `pdfCount`는 0에서 바뀌지 않고, `reportPdfSaved` 콜백도 호출되지 않으며 반환값도 호출부가 사용하지 않는다. 남은 옛 카운트 경로다. |
| 🟠 | 일반 시트·가공도 내부 오류를 이 메서드 안에서 잡고 계속한 뒤 정상 반환하므로, 페이지가 하나도 없어도 바깥 `successCount`가 증가할 수 있다. 요약의 “성공 STRU”가 실제 PDF 생성 성공과 다를 수 있다. |
| 🟠 | `BeginPdfPageAccumulation`의 bool 반환값을 확인하지 않는다. 누적 시작이 실패해도 모든 시트 그리기를 계속하고 마지막 저장 실패로만 드러날 수 있다. |
| 🟠 | 일괄 출력 시작과 종료 때 이전 가시성을 저장하지 않고 각각 전체 BODY 표시를 강제한다. 사용자가 미리 숨겨 둔 부재 상태는 복원되지 않는다. 검색도 선택 STRU 외 BODY를 숨긴 상태를 계속 유지한다. |
| 🟠 | **전체 선택/해제**는 `SetItemChecked`를 N번 호출하고, 각 호출마다 현재 체크된 모든 STRU의 하위 BODY를 다시 조회·합집합·선택한다. STRU가 많으면 O(N²) SDK 트리 조회와 전체 색 초기화가 발생한다. |
| 🟠 | 한 일반 시트 실패는 잡고 페이지를 버리지만 STRU 폴더 생성 실패는 로그만 남긴 채 PDF 누적을 계속한다. 저장 경로가 없을 때 더 늦고 모호한 실패가 날 수 있다. |
| 🟠 | `GC.Collect`, 여러 `Thread.Sleep(50~500ms)`, `Application.DoEvents`가 UI 스레드에 흩어져 있다. 비동기 완료 race와 네이티브 메모리를 다루기 위한 우회지만, 고정 대기·재진입·사용자 체감 지연을 만든다. |
| 🟡 | 파일 머리의 설계 주석은 “xraySelectedNodeIndices = STRU BODY”, “FolderBrowserDialog”, “행 선택 이벤트 자동 트리거”를 현재 흐름으로 설명하지만 실제 코드는 가시성 격리+X-Ray 비움, 고정 폴더, 직접 `ApplySheetSelection` 호출로 바뀌었다. |
| 🟡 | STRU fallback은 `/` 시작·공백 없음만 검사해 파일 루트나 STRU가 아닌 Assembly도 포함할 수 있다. fallback 목록은 디버그 안전망이지만 그대로 일괄 출력 대상이 된다. |
| 🟡 | 체크 강조 해제는 `Color.RestoreColorAll`만 호출하고 명시적 `DESELECT_ALL`은 하지 않는다. SDK 선택 상태까지 풀리는지 XML 설명만으로는 확정할 수 없어 실기 확인이 필요하다 `(미확인)` . |
| 🟡 | `_p2aInProgress` 이름과 파일 전반의 옛 단계 주석이 폐기된 PoC 명칭을 유지해 현재 역할인 “STRU 일괄 출력 중”을 바로 알기 어렵다. |
