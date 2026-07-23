# Form1.MfgDrawing.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.MfgDrawing.cs` (약 2,985 라인)

**책임**: 가공도 3D 미리보기, 엑셀 템플릿 기반 PDF 출력, 카메라 방향 결정, Osnap 치수와 풍선 생성, EA 앵글 상하 2뷰 배치(가로화·스왑·미러).

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---:|---|
| <a id="btnMfgDrawingSheet_Click"></a>`btnMfgDrawingSheet_Click` | L2354 | [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md) |

옛 `btnMfgDrawing_Click` 핸들러는 제거됐다. 시트 선택 미리보기는 `LvDrawingSheet_SelectedIndexChanged`가 `ExecuteMfgDrawing`을 호출한다.

## 활성 핵심 메서드

### <a id="GenerateMfgDrawingManual"></a>GenerateMfgDrawingManual

- **라인**: L2105
- 전체 가공도 BOM을 수집하고 5개씩 페이지로 나눈다(`SplitMfgIntoPages`).
- 첫 가공도 부재에서 STRU 부모 방향의 PNT 계열 UDA를 출력당 한 번만 조회하고, 모든 페이지 PAINT CODE `Input_166`에 재사용한다.
- `가공도_도면_1.xlsx`(2026-07-12 전환, 제작도와 동일 슬롯 체계)를 가져와 각 View 영역을 렌더한다(`RenderMfgRowToViewArea`). CONTRACTOR 로고는 `{Image_3}` + mfgImageMapping으로 Import 단계 처리(2026-07-21, 옛 `Set2DViewTemplateMark` 등록 폐기).
- 템플릿 적용은 `ImportExcelWithData` 직행 — 소형 템플릿(~4천 셀)이라 수백 ms. JSON 사전변환·캐시는 2026-07-19 제거(변환 290초·태그 미보존·stale 좌표 버그). 템플릿은 엑셀에서만 수정(openpyxl 저장본은 네이티브 크래시).
- 북쪽 화살표는 `{Image_1}`(N, AT3)·`{Image_2}`(ISO, C3) 태그 + `ImportExcelWithData(경로, data, mfgImageMapping)` 3인자(SDK 1.0.26.716)로 Import 단계에서 배치 — 옛 View 기반 수동 배치 폐기 (2026-07-20). `EnsureViewAreasCache`는 중복 View 태그 방어(첫 위치 사용+경고). ⚠ **태그 번호 한계**: View 1~7·Input 1~199만 — 초과 시 import에서 SDK 메모리 손상 → 직후 캡처 AccessViolation (2026-07-20 실측).
- Import 직후 `RemoveEmptyTemplateBorders(0.1f, RowAndColumn)`(SDK 1.0.26.716)로 내용 없는 공백 셀(미기재 BOM 행)의 괘선 제거 (2026-07-21, 제작도와 동일). 전역 동작이라 `BuildMfgPageData` 선초기화가 보존 칸(PAINT/DP/TAG 165~169·REV. 칸 194)만 공백(`" "`)으로 위장 — BOM(4~163)·Note(164)·Rev 위 4행(170~193)·부재명(195~199)은 빈 문자열(괘선 제거 대상, 제작도와 동일 정책).
- 페이지별 PDF를 실행 파일 하위 `Drawings`에 저장한다.
- 출력 후 BOM UI와 선택 시트 가시성을 복원한다.

### <a id="RenderMfgRowToViewArea"></a>RenderMfgRowToViewArea

- **라인**: L938
- 일반 부재는 View 영역 전체에 한 뷰를 배치한다.
- EA 부재는 View 영역을 위·아래로 분할하고, 코어(`BuildMfgSceneCore`)와 2차 뷰(`BuildEaSecondaryScene`)를 각각 캡처한다.
- 첫 번째 뷰는 최장축 치수를 예약하고, 두 번째 뷰가 해당 치수를 담당한다.
- 세로로 잡히는 부재는 `ProbeAndRollLandscape`로 가로화한다.
- 공통 코어가 만든 Hole/SlotHole/EarthBoss Note를 2D로 변환하므로 PDF 풍선은 유지된다.
- 두 번째 뷰 실패 시 불완전한 객체를 삭제하고 첫 번째 뷰를 유지한다.

### <a id="BuildMfgSceneCore"></a>BuildMfgSceneCore

- **라인**: L1167
- 대상 부재 격리, BBox 최장축 판정, PAD/PLATE 카메라 선택을 수행한다.
- `ORIENTATION` UDA와 EA 열린 방향 보정을 적용한다.
- LINE Osnap을 5도 방향군으로 묶고 길이 합 주축·월드축 편차(1도 임계값)·단일 최장선·ORIENTATION 비교를 `[참조축판정]` 로그에 남긴다. 1단계에서는 ReferenceAxis와 출력 동작을 변경하지 않는다.
- LINE/POINT Osnap 수집, 뒷면 필터, 체인 치수(그릴 목록 `pose.PendingDims`)를 수집한다 — 세로(폭) 축은 전체 치수를 생략하고 체인 1단만(2026-07-03).
- 풍선은 Hole, SlotHole, UDA `PURPOSE=EBOS`인 EarthBoss만 생성한다.
- ISO 부재번호와 원형 부재 반지름 풍선은 생성하지 않는다.
- `reserveLongestAxisForSecondary=true`이면 EA 첫 번째 뷰에서 최장축 치수를 생략한다.
- EA 접힘 모서리(코너)를 판정해 두 뷰 상하 스왑 여부(`pose.SwapViews`)를 결정한다.

### <a id="BuildEaSecondaryScene"></a>BuildEaSecondaryScene

- **라인**: L715
- EA 두 번째 뷰를 독립 카메라에서 생성한다.
- 최장축 Z는 `X_PLUS`(가로화는 호출자의 probe 실측), 나머지는 `Z_MINUS`를 사용한다.
- 길이(최장축) 치수는 위 슬롯일 때만 체인+전체를 수집하고, 폭(높이) 치수는 체인 1단만 수집한다(2026-07-03).
- 코너가 가운데를 향하지 않으면 상하 미러(`pose.MirrorVertical`)를 예약한다 — 모델은 SDK 2D 미러(`SetSelected3DMirrorBy2DView`)로 뒤집고, 치수·보조선 매핑은 SDK 미러가 함께 처리한다(별도 3D 좌표 반전 없음).
- 과거 T자형 원인이던 추가 정렬 회전은 사용하지 않는다.

### <a id="ProbeAndRollLandscape"></a>ProbeAndRollLandscape

- **라인**: L495
- 임시 캡처(probe)로 실제 투영 크기(objW/objH)를 측정해 세로면이면 화면축 90도 회전으로 가로화한다.
- 판정은 축 규약 추측이 아닌 **임시 캡처의 실측(ground truth)** 으로만 한다.
- 2D 캡처는 카메라 ±방향·up-vector를 무시하므로 화면축 회전(`RotateCameraByScreenAxis`)만 반영된다.

### <a id="DrawMfgDimsAtScale"></a>DrawMfgDimsAtScale

- **라인**: L546
- 모델 2D 캡처 + `RescaleObject` 직후 확정된 실측 배율(`newScale`)로 `pose.PendingDims`의 치수·보조선을 그린다.
- 보조선 오프셋(9/18mm)과 시작 gap(2mm)을 모두 캔버스 절대값으로 역산해 부재·뷰 무관하게 일정하게 유지한다(gap 통일 2026-07-03, 오프셋 1.5배 2026-07-06).
- 치수 텍스트 위치는 수동 배치 없이 SDK 자동 정렬에 위임한다(가로=치수선 위, 세로=왼쪽 회사 표준).

### <a id="CaptureMfgSceneToViewArea"></a>CaptureMfgSceneToViewArea

- **라인**: L347
- 현재 안정화 경로는 `DASH_LINE` 렌더모드와 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` 조합으로 2D 캡처한다. 은선 없는 캡처 API는 신 템플릿에서 SDK AccessViolation이 발생해 벤더 수정 대기 중이다.
- 지정된 View 영역에 맞게 스케일을 계산하고 중앙 배치한다.
- ShapeDrawing(`pose.ShapeDrawingIds`), Note, Measure를 2D 객체로 변환한다.

### <a id="ExecuteMfgDrawing"></a>ExecuteMfgDrawing

- **라인**: L1975
- 가공도 시트 선택 시 단일 3D 미리보기를 만든다.
- 공통 코어 호출 직후 `Review.Note.Clear()`로 Hole/SlotHole/EarthBoss 풍선만 미리보기에서 제거한다. PDF 렌더 경로는 이 메서드를 거치지 않아 풍선을 유지한다.
- `BuildMfgSceneCore` 결과의 Z90/R180 회전을 적용한다.
- 결과 카메라 정보를 `_lastMfgViewPose`에 저장한다.

### <a id="IsAngleFromSpref"></a>IsAngleFromSpref

- **라인**: L2478
- 부모 방향으로 최대 10단계 탐색해 `SPREF`를 읽는다.
- `/` 제거 후 `:` 앞 ITEM이 `EA`로 시작하면 앵글 부재로 판정한다.

### <a id="FilterHiddenLineOsnap"></a>FilterHiddenLineOsnap

- **라인**: L2498
- 카메라 깊이축의 뒤쪽 15% 영역에 있는 Osnap을 제외한다.
- 뒷면 가시축 극점 복원 예외는 은선 폐지와 함께 제거됐다 — 단면 osnap만 치수화(2026-07-03).
- PLUS/MINUS 카메라에 따라 제거 방향을 반대로 적용한다.

### <a id="ApplyOrientationRotation"></a>ApplyOrientationRotation

- **라인**: L2677
- `ORIENTATION` UDA 각도를 화면 Z축 회전으로 적용한다.

## 보조 메서드

| 메서드 | 라인 | 상태 |
|---|---:|---|
| `RestoreAllPartsVisibility` | L23 | 활성, 출력 후 가시성 복원 |
| `SplitMfgIntoPages` | L69 | 활성, 페이지당 5부재 분할 |
| `BuildMfgPageData` | L122 | 활성, 페이지 엑셀 슬롯 데이터 구성 (PAINT CODE Input_166, 부재명 Input_195~199, BOM 8×20 Input_4~163, 선초기화 1..199 — **Input 200 이상 금지**: SDK 메모리 손상) |
| `GetSprefValue` | L2410 | 활성 |
| `ParseOrientation` | L2628 | 활성 |
| `DetectMfgAxis` | L2803 | LINE Osnap 5도 방향군·길이 합 기반 주축 판정 |
| `LogMfgAxisDetection` | L2897 | 월드축 편차·정상/틀어짐·단일 최장선·ORIENTATION 진단 로그 |

옛 그리드 8×3 일괄 출력 경로(`GenerateMfgDrawing2DAll`)와 그 셀 렌더 어댑터(`RenderMfgViewForDrawing`)는 2026-07-03 제거됐다. 현재 PDF 출력은 `GenerateMfgDrawingManual` → `RenderMfgRowToViewArea` 경로로 일원화됐다.

## 관련 문서

- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
- [가공도 시트 3D 미리보기](../기능/가공도/가공도%20단일.md)
