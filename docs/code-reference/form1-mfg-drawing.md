# Form1.MfgDrawing.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.MfgDrawing.cs` (약 2,419 라인)

**책임**: 가공도 3D 미리보기, 엑셀 템플릿 기반 PDF 출력, 카메라 방향 결정, Osnap 치수와 풍선 생성, EA 앵글 상하 2뷰 배치(가로화·스왑·미러).

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---:|---|
| <a id="btnMfgDrawingSheet_Click"></a>`btnMfgDrawingSheet_Click` | L2087 | [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md) |

옛 `btnMfgDrawing_Click` 핸들러는 제거됐다. 시트 선택 미리보기는 `LvDrawingSheet_SelectedIndexChanged`가 `ExecuteMfgDrawing`을 호출한다.

## 활성 핵심 메서드

### <a id="GenerateMfgDrawingManual"></a>GenerateMfgDrawingManual

- **라인**: L1878
- 전체 가공도 BOM을 수집하고 5개씩 페이지로 나눈다(`SplitMfgIntoPages`).
- `가공도_도면_1.xlsx`(2026-07-12 전환, 제작도와 동일 슬롯 체계)를 가져와 각 View 영역을 렌더한다(`RenderMfgRowToViewArea`). 템플릿 적용 전 `Set2DViewTemplateMark(Logo.png)` 1회 등록({Image} 슬롯 치환).
- 템플릿 적용은 `ImportExcelWithData` 직행 — 소형 템플릿(~4천 셀)이라 수백 ms. JSON 사전변환·캐시는 2026-07-19 제거(변환 290초·태그 미보존·stale 좌표 버그). 템플릿은 엑셀에서만 수정(openpyxl 저장본은 네이티브 크래시).
- 페이지마다 북쪽 화살표 배치(2026-07-20): `View_8`=N 화살표·`View_7`=ISO 화살표를 `PlaceImageInTemplateArea`(제작도 공용, RenderTemplate 캘리브레이션 포함)로 배치, 슬롯 없으면 생략. `EnsureViewAreasCache`는 중복 View 태그 방어(첫 위치 사용+경고).
- 페이지별 PDF를 실행 파일 하위 `Drawings`에 저장한다.
- 출력 후 BOM UI와 선택 시트 가시성을 복원한다.

### <a id="RenderMfgRowToViewArea"></a>RenderMfgRowToViewArea

- **라인**: L787
- 일반 부재는 View 영역 전체에 한 뷰를 배치한다.
- EA 부재는 View 영역을 위·아래로 분할하고, 코어(`BuildMfgSceneCore`)와 2차 뷰(`BuildEaSecondaryScene`)를 각각 캡처한다.
- 첫 번째 뷰는 최장축 치수를 예약하고, 두 번째 뷰가 해당 치수를 담당한다.
- 세로로 잡히는 부재는 `ProbeAndRollLandscape`로 가로화한다.
- 두 번째 뷰 실패 시 불완전한 객체를 삭제하고 첫 번째 뷰를 유지한다.

### <a id="BuildMfgSceneCore"></a>BuildMfgSceneCore

- **라인**: L1006
- 대상 부재 격리, BBox 최장축 판정, PAD/PLATE 카메라 선택을 수행한다.
- `ORIENTATION` UDA와 EA 열린 방향 보정을 적용한다.
- LINE/POINT Osnap 수집, 뒷면 필터, 체인 치수(그릴 목록 `pose.PendingDims`)를 수집한다 — 세로(폭) 축은 전체 치수를 생략하고 체인 1단만(2026-07-03).
- 풍선은 Hole, SlotHole, UDA `PURPOSE=EBOS`인 EarthBoss만 생성한다.
- ISO 부재번호와 원형 부재 반지름 풍선은 생성하지 않는다.
- `reserveLongestAxisForSecondary=true`이면 EA 첫 번째 뷰에서 최장축 치수를 생략한다.
- EA 접힘 모서리(코너)를 판정해 두 뷰 상하 스왑 여부(`pose.SwapViews`)를 결정한다.

### <a id="BuildEaSecondaryScene"></a>BuildEaSecondaryScene

- **라인**: L564
- EA 두 번째 뷰를 독립 카메라에서 생성한다.
- 최장축 Z는 `X_PLUS`(가로화는 호출자의 probe 실측), 나머지는 `Z_MINUS`를 사용한다.
- 길이(최장축) 치수는 위 슬롯일 때만 체인+전체를 수집하고, 폭(높이) 치수는 체인 1단만 수집한다(2026-07-03).
- 코너가 가운데를 향하지 않으면 상하 미러(`pose.MirrorVertical`)를 예약한다 — 모델은 SDK 2D 미러(`SetSelected3DMirrorBy2DView`)로 뒤집고, 치수·보조선 매핑은 SDK 미러가 함께 처리한다(별도 3D 좌표 반전 없음).
- 과거 T자형 원인이던 추가 정렬 회전은 사용하지 않는다.

### <a id="ProbeAndRollLandscape"></a>ProbeAndRollLandscape

- **라인**: L387
- 임시 캡처(probe)로 실제 투영 크기(objW/objH)를 측정해 세로면이면 화면축 90도 회전으로 가로화한다.
- 판정은 축 규약 추측이 아닌 **임시 캡처의 실측(ground truth)** 으로만 한다.
- 2D 캡처는 카메라 ±방향·up-vector를 무시하므로 화면축 회전(`RotateCameraByScreenAxis`)만 반영된다.

### <a id="DrawMfgDimsAtScale"></a>DrawMfgDimsAtScale

- **라인**: L432
- 모델 2D 캡처 + `RescaleObject` 직후 확정된 실측 배율(`newScale`)로 `pose.PendingDims`의 치수·보조선을 그린다.
- 보조선 오프셋(9/18mm)과 시작 gap(2mm)을 모두 캔버스 절대값으로 역산해 부재·뷰 무관하게 일정하게 유지한다(gap 통일 2026-07-03, 오프셋 1.5배 2026-07-06).
- 치수 텍스트 위치는 수동 배치 없이 SDK 자동 정렬에 위임한다(가로=치수선 위, 세로=왼쪽 회사 표준).

### <a id="CaptureMfgSceneToViewArea"></a>CaptureMfgSceneToViewArea

- **라인**: L242
- 모델을 은선 없이(단면 외곽선만) 현재 카메라에서 2D로 캡처한다(`Create2DViewObjectWithModelAtCanvasOrigin`, 2026-07-03 은선 폐지).
- 지정된 View 영역에 맞게 스케일을 계산하고 중앙 배치한다.
- ShapeDrawing(`pose.ShapeDrawingIds`), Note, Measure를 2D 객체로 변환한다.

### <a id="ExecuteMfgDrawing"></a>ExecuteMfgDrawing

- **라인**: L1792
- 가공도 시트 선택 시 단일 3D 미리보기를 만든다.
- `BuildMfgSceneCore` 결과의 Z90/R180 회전을 적용한다.
- 결과 카메라 정보를 `_lastMfgViewPose`에 저장한다.

### <a id="IsAngleFromSpref"></a>IsAngleFromSpref

- **라인**: L2202
- 부모 방향으로 최대 10단계 탐색해 `SPREF`를 읽는다.
- `/` 제거 후 `:` 앞 ITEM이 `EA`로 시작하면 앵글 부재로 판정한다.

### <a id="FilterHiddenLineOsnap"></a>FilterHiddenLineOsnap

- **라인**: L2222
- 카메라 깊이축의 뒤쪽 15% 영역에 있는 Osnap을 제외한다.
- 뒷면 가시축 극점 복원 예외는 은선 폐지와 함께 제거됐다 — 단면 osnap만 치수화(2026-07-03).
- PLUS/MINUS 카메라에 따라 제거 방향을 반대로 적용한다.

### <a id="ApplyOrientationRotation"></a>ApplyOrientationRotation

- **라인**: L2392
- `ORIENTATION` UDA 각도를 화면 Z축 회전으로 적용한다.

## 보조 메서드

| 메서드 | 라인 | 상태 |
|---|---:|---|
| `RestoreAllPartsVisibility` | L23 | 활성, 출력 후 가시성 복원 |
| `SplitMfgIntoPages` | L69 | 활성, 페이지당 5부재 분할 |
| `BuildMfgPageData` | L122 | 활성, 페이지 엑셀 슬롯 데이터 구성 (부재명 Input_200~204, BOM 8×20 Input_4~163, 선초기화 1..204) |
| `GetSprefValue` | L2143 | 활성 |
| `ParseOrientation` | L2343 | 활성 |

옛 그리드 8×3 일괄 출력 경로(`GenerateMfgDrawing2DAll`)와 그 셀 렌더 어댑터(`RenderMfgViewForDrawing`)는 2026-07-03 제거됐다. 현재 PDF 출력은 `GenerateMfgDrawingManual` → `RenderMfgRowToViewArea` 경로로 일원화됐다.

## 관련 문서

- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
- [가공도 시트 3D 미리보기](../기능/가공도/가공도%20단일.md)
