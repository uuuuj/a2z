# Form1.MfgDrawing.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.MfgDrawing.cs` (약 3,475 라인)

**책임**: 가공도 3D 미리보기, 엑셀 템플릿 기반 PDF 출력, ORIENTATION 로컬 참조축 카메라, Osnap 치수와 최종 화면 기준 풍선 생성, EA 앵글 상하 2뷰 배치(가로화·스왑·미러).

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---:|---|
| <a id="btnMfgDrawingSheet_Click"></a>`btnMfgDrawingSheet_Click` | L2523 | [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md) |

옛 `btnMfgDrawing_Click` 핸들러는 제거됐다. 시트 선택 미리보기는 `LvDrawingSheet_SelectedIndexChanged`가 `ExecuteMfgDrawing`을 호출한다.

## 활성 핵심 메서드

### <a id="GenerateMfgDrawingManual"></a>GenerateMfgDrawingManual

- **라인**: L2268
- 전체 가공도 BOM을 수집하고 5개씩 페이지로 나눈다(`SplitMfgIntoPages`).
- 제작도·조립도·설치도와 같은 `DrawingSheetData.PaintCode` 공용 캐시를 사용하며, 아직 조회 전이면 안전한 출력 시점에 한 번 확정해 모든 페이지 PAINT CODE `Input_166`에 재사용한다.
- `가공도_도면_1.xlsx`(2026-07-12 전환, 제작도와 동일 슬롯 체계)를 가져와 각 View 영역을 렌더한다(`RenderMfgRowToViewArea`). CONTRACTOR 로고는 `{Image_3}` + mfgImageMapping으로 Import 단계 처리(2026-07-21, 옛 `Set2DViewTemplateMark` 등록 폐기).
- 템플릿 적용은 `ImportExcelWithData` 직행 — 소형 템플릿(~4천 셀)이라 수백 ms. JSON 사전변환·캐시는 2026-07-19 제거(변환 290초·태그 미보존·stale 좌표 버그). 템플릿은 엑셀에서만 수정(openpyxl 저장본은 네이티브 크래시).
- 북쪽 화살표는 `{Image_1}`(N, AT3)·`{Image_2}`(ISO, C3) 태그 + `ImportExcelWithData(경로, data, mfgImageMapping)` 3인자로 Import 단계에서 배치한다. `EnsureViewAreasCache`는 중복 View 태그를 방어한다. SDK 1.0.26.723에서 Input 200+ 문제가 수정되어 BOM 21~25행에 Input 201~240을 사용한다.
- Import 직후 `RemoveEmptyTemplateBorders(0.1f, RowAndColumn)`로 내용 없는 공백 셀의 괘선을 제거한다. 전역 동작이라 `BuildMfgPageData` 선초기화가 보존 칸(PAINT/DP/TAG 165~169·REV. 칸 194)만 공백(`" "`)으로 위장하고, BOM(4~163·201~240)·Note(164)·Rev 위 4행(170~193)·부재명(195~199)은 빈 문자열로 둔다.
- 페이지별 PDF를 실행 파일 하위 `Drawings`에 저장한다.
- 선택적 `shouldCancel` 콜백이 전달된 STRU 일괄 출력 경로는 각 페이지 시작 전에 `Application.DoEvents()` 후 요청을 확인하고 다음 페이지부터 중단한다. 수동 가공도 출력은 콜백을 생략해 기존 흐름을 유지한다.
- 출력 후 BOM UI와 선택 시트 가시성을 복원한다.

### <a id="RenderMfgRowToViewArea"></a>RenderMfgRowToViewArea

- **라인**: L1099
- 일반 부재는 View 영역 전체에 한 뷰를 배치한다.
- EA 부재는 View 영역을 위·아래로 분할하고, 코어(`BuildMfgSceneCore`)와 2차 뷰(`BuildEaSecondaryScene`)를 각각 캡처한다.
- 첫 번째 뷰는 최장축 치수를 예약하고, 두 번째 뷰가 해당 치수를 담당한다.
- 세로로 잡히는 부재는 `ProbeAndRollLandscape`로 가로화한다.
- 공통 코어가 Hole/SlotHole 정보를 대기시키고, 가로화 직후 `AddMfgPendingHoleNotes`가 최종 화면 하단에 Note를 만든다. EarthBoss Note와 함께 2D로 변환하므로 PDF 풍선은 유지된다.
- 두 번째 뷰 실패 시 불완전한 객체를 삭제하고 첫 번째 뷰를 유지한다.
- 행 시작과 `finally`에서 측정·보조선과 활성 가공도 ReferenceAxis를 정리해 다음 행의 카메라 상태와 섞이지 않게 한다.

### <a id="BuildMfgSceneCore"></a>BuildMfgSceneCore

- **라인**: L1328
- 대상 부재 격리, BBox 최장축 판정, PAD/PLATE 카메라 선택을 수행한다.
- `ORIENTATION`이 1도를 초과하면 원문의 로컬축을 직교 X/Y/Z 프레임으로 복원하고 SDK ReferenceAxis를 활성화한 뒤 그 축 기준 카메라를 적용한다. 정상 부재는 기존 월드축 카메라, 참조축 실패는 기존 화면 roll 폴백을 사용한다.
- `ORIENTATION` UDA를 우선해 1도 초과를 틀어짐으로 판정하고, 값이 없을 때만 LINE Osnap 길이 합 주축을 폴백으로 사용한다. `[참조축판정]`에는 두 결과와 최종 판정 출처를 함께 남긴다.
- LINE/POINT Osnap 수집, 뒷면 필터, 체인 치수(그릴 목록 `pose.PendingDims`)를 수집한다 — 세로(폭) 축은 전체 치수를 생략하고 체인 1단만(2026-07-03).
- Hole/SlotHole은 `pose.PendingHoleNotes`에 수집하고, UDA `PURPOSE=EBOS`인 EarthBoss만 즉시 생성한다.
- ISO 부재번호와 원형 부재 반지름 풍선은 생성하지 않는다.
- `reserveLongestAxisForSecondary=true`이면 EA 첫 번째 뷰에서 최장축 치수를 생략한다.
- EA 접힘 모서리(코너)를 판정해 두 뷰 상하 스왑 여부(`pose.SwapViews`)를 결정한다.

### <a id="BuildEaSecondaryScene"></a>BuildEaSecondaryScene

- **라인**: L863
- EA 두 번째 뷰를 독립 카메라에서 생성한다.
- 기울어진 부재는 1차 ReferenceAxis를 정리한 뒤 같은 로컬 프레임으로 2차 뷰 전용 ReferenceAxis를 다시 생성한다.
- 최장축 Z는 `X_PLUS`(가로화는 호출자의 probe 실측), 나머지는 `Z_MINUS`를 사용한다.
- 길이(최장축) 치수는 위 슬롯일 때만 체인+전체를 수집하고, 폭(높이) 치수는 체인 1단만 수집한다(2026-07-03).
- 코너가 가운데를 향하지 않으면 상하 미러(`pose.MirrorVertical`)를 예약한다 — 모델은 SDK 2D 미러(`SetSelected3DMirrorBy2DView`)로 뒤집고, 치수·보조선 매핑은 SDK 미러가 함께 처리한다(별도 3D 좌표 반전 없음).
- 과거 T자형 원인이던 추가 정렬 회전은 사용하지 않는다.

### <a id="ProbeAndRollLandscape"></a>ProbeAndRollLandscape

- **라인**: L510
- 임시 캡처(probe)로 실제 투영 크기(objW/objH)를 측정해 세로면이면 화면축 90도 회전으로 가로화한다.
- 판정은 축 규약 추측이 아닌 **임시 캡처의 실측(ground truth)** 으로만 한다.
- 2D 캡처는 카메라 ±방향·up-vector를 무시하므로 화면축 회전(`RotateCameraByScreenAxis`)만 반영된다.

### <a id="AddMfgPendingHoleNotes"></a>AddMfgPendingHoleNotes

- **라인**: L559
- `BuildMfgSceneCore`가 수집한 Hole/SlotHole 정보를 최종 카메라 방향이 확정된 뒤 Review Note로 생성한다.
- ReferenceAxis 부재는 ORIENTATION 로컬 X/Y/Z, 정상 부재는 기존 월드축 화면 매핑을 기저로 사용한다.
- ReferenceAxis 실패 폴백의 ORIENTATION 화면 roll과 `ProbeAndRollLandscape`의 추가 90도 roll을 합산해 최종 화면 수평·수직 벡터를 계산한다.
- 월드 BBox 8개 꼭짓점의 최종 화면 수직 투영 최솟값으로 모델 하단을 구하고, 홀 중심의 화면 수평 위치는 유지해 리더가 최종 화면에서 수직이 되게 한다.
- 회전이 없는 정상 부재는 기존 `modelMinV - span*0.6` 하단 공식과 동일하다.

### <a id="DrawMfgDimsAtScale"></a>DrawMfgDimsAtScale

- **라인**: L681
- 모델 2D 캡처 + `RescaleObject` 직후 확정된 실측 배율(`newScale`)로 `pose.PendingDims`의 치수·보조선을 그린다.
- 보조선 오프셋(9/18mm)과 시작 gap(2mm)을 모두 캔버스 절대값으로 역산해 부재·뷰 무관하게 일정하게 유지한다(gap 통일 2026-07-03, 오프셋 1.5배 2026-07-06).
- 치수 텍스트 위치는 수동 배치 없이 SDK 자동 정렬에 위임한다(가로=치수선 위, 세로=왼쪽 회사 표준).
- ReferenceAxis를 사용하는 부재만 동일한 로컬 축 벡터를 `DrawDimension`의 UserAxis 경로로 전달한다. 정상 부재는 null을 전달해 기존 월드축 치수 경로를 그대로 사용한다.
- `Review.Measure.Clear()`는 활성 참조축도 삭제하므로 이 메서드에서는 초기화하지 않고, 호출 전 장면 초기화 단계가 참조축 생성보다 먼저 처리한다.

### <a id="CaptureMfgSceneToViewArea"></a>CaptureMfgSceneToViewArea

- **라인**: L362
- 현재 안정화 경로는 `DASH_LINE` 렌더모드와 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` 조합으로 2D 캡처한다. 은선 없는 캡처 API는 신 템플릿에서 SDK AccessViolation이 발생해 벤더 수정 대기 중이다.
- 지정된 View 영역에 맞게 스케일을 계산하고 중앙 배치한다.
- ShapeDrawing(`pose.ShapeDrawingIds`), Note, Measure를 2D 객체로 변환한다.

### <a id="ExecuteMfgDrawing"></a>ExecuteMfgDrawing

- **라인**: L2140
- 가공도 시트 선택 시 단일 3D 미리보기를 만든다.
- 진입 시 직전 화면 roll과 활성 ReferenceAxis를 원복하고, 기울어진 부재는 공통 코어가 만든 로컬 참조축 상태로 표시한다.
- 공통 코어 호출 직후 `Review.Note.Clear()`로 EarthBoss 풍선을 제거하고, `PendingHoleNotes`는 미리보기에서 생성하지 않는다. PDF 렌더 경로만 최종 화면에서 Hole/SlotHole을 생성한다.
- `BuildMfgSceneCore` 결과의 Z90/R180 회전을 적용한다.
- 결과 카메라 정보를 `_lastMfgViewPose`에 저장한다.

### <a id="IsAngleFromSpref"></a>IsAngleFromSpref

- **라인**: L2647
- 부모 방향으로 최대 10단계 탐색해 `SPREF`를 읽는다.
- `/` 제거 후 `:` 앞 ITEM이 `EA`로 시작하면 앵글 부재로 판정한다.

### <a id="FilterHiddenLineOsnap"></a>FilterHiddenLineOsnap

- **라인**: L2667
- 카메라 깊이축의 뒤쪽 15% 영역에 있는 Osnap을 제외한다.
- 뒷면 가시축 극점 복원 예외는 은선 폐지와 함께 제거됐다 — 단면 osnap만 치수화(2026-07-03).
- PLUS/MINUS 카메라에 따라 제거 방향을 반대로 적용한다.

### <a id="ApplyOrientationRotation"></a>ApplyOrientationRotation

- **라인**: L2846
- 정상 부재의 기존 경로와 ReferenceAxis 실패 폴백에서 `ORIENTATION` UDA 각도를 화면 Z축 회전으로 적용한다.

### TryBuildMfgOrientationReferenceFrame / ActivateMfgReferenceAxis

- **라인**: L2927~L3164
- `Y is E 25 S and Z is U` 같은 ORIENTATION 원문에서 로컬 축 방향을 파싱하고 Gram-Schmidt 직교화와 외적으로 오른손 X/Y/Z 프레임을 만든다.
- BBox 중심을 원점으로 `ReferenceAxis.Create` → `Activate` → `MoveCamera` 순서로 적용한다.
- 실패 시 참조축을 정리하고 기존 `ApplyOrientationRotation`으로 복귀한다.

### ClearMfgViewAnnotations / ReleaseActiveMfgReferenceAxis

- **라인**: L2893~L2925
- SDK의 `Review.Measure.Clear()`가 ReferenceAxis까지 삭제하는 동작에 맞춰, 먼저 `ReferenceAxis.Reset()`과 `Review.Delete(id)`로 활성 축을 해제한 뒤 측정·보조선을 지운다.
- PDF 행·EA 뷰·3D 미리보기 선택 간 ReferenceAxis 수명주기를 격리하고 `[MfgRefAxis]` 로그를 남긴다.

## 보조 메서드

| 메서드 | 라인 | 상태 |
|---|---:|---|
| `RestoreAllPartsVisibility` | L23 | 활성, 출력 후 가시성 복원 |
| `SplitMfgIntoPages` | L69 | 활성, 페이지당 5부재 분할 |
| `BuildMfgPageData` | L122 | 활성, 페이지 엑셀 슬롯 데이터 구성 (PAINT CODE Input_166, 부재명 Input_195~199, BOM 8×25 Input_4~240, 선초기화 1..240 — SDK 1.0.26.723의 Input 200+ 수정 반영) |
| `GetSprefValue` | L2579 | 활성 |
| `ParseOrientation` | L2797 | 활성 |
| `GetMfgOrientationAxisVector` | L3184 | 활성 ReferenceAxis와 같은 로컬 X/Y/Z 축 벡터를 UserAxis 치수용 `Vertex3D`로 반환 |
| `DetectMfgAxis` | L3284 | ORIENTATION 부재 시 사용할 LINE Osnap 5도 방향군·길이 합 폴백 판정 |
| `LogMfgAxisDetection` | L3378 | ORIENTATION 우선 최종 판정과 기하 대조 결과 진단 로그 |

옛 그리드 8×3 일괄 출력 경로(`GenerateMfgDrawing2DAll`)와 그 셀 렌더 어댑터(`RenderMfgViewForDrawing`)는 2026-07-03 제거됐다. 현재 PDF 출력은 `GenerateMfgDrawingManual` → `RenderMfgRowToViewArea` 경로로 일원화됐다.

## 관련 문서

- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
- [가공도 시트 3D 미리보기](../기능/가공도/가공도%20단일.md)
