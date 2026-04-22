# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**으로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

---

## 2026-04-23 — T-036 추가 보강: BeginUpdate 감싸기 + Z90 FitToView

**유형**: fix
**커밋**: `pending`
**관련 TASK**: T-036
**배경**: 사용자 재보고 "가로로 누워있다가 카메라 재조정/fit 과정 중 갑자기 세로로 변함 (Z축 세운 모델들에서)". 진단: `ExecuteMfgDrawing` 내부 `MoveCamera`·`FitToView`·`RotateCameraByScreenAxis` 여러 단계가 즉시 반영되어 **중간 상태가 화면 깜빡임으로 노출**. `BeginUpdate/EndUpdate` 없이 구현되어 있었음
**변경 사항**:
- [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs) 전체를 `vizcore3d.BeginUpdate()` / `finally { vizcore3d.EndUpdate(); }` 로 감쌈 → 중간 카메라 회전 단계가 화면에 노출되지 않고 **최종 상태만** 반영
- L532 Z 최장축 90° 회전 **직후** `vizcore3d.View.FitToView()` 호출 추가 — 회전 후 화면 중앙·스케일 재조정 누락되어 있던 부분 보강
- docs `mfg-drawing.md` 변경 이력 갱신
- MSBuild Debug 통과

**영향 범위**: 가공도 시트 선택 시 화면 전환 부드러움 + Z 최장축 90° 회전 후 화면 정합. 회전 로직 자체는 무변경

**추가 확인 필요**: 이 수정으로 "가로→세로 깜빡"이 사라지는데, 만약 **최종 결과가 여전히 세로**라면 `DiagLog T-036 MfgDrawing bom=... longestAxis=... use180=...` 로그 공유 필요. 회전 순서 자체를 재설계해야 할 수 있음

---

## 2026-04-23 — T-036 재해석: 가공도 시트 ISO 뷰 느낌 해결

**유형**: fix
**커밋**: `b0f8802`
**관련 TASK**: T-036
**배경**: 직전 커밋(`537f07c`)은 "Z 최장축 세로 배치"로 해석해 L215 180° 스킵 가드 추가. 사용자 실기 재보고 "45도 대각 ISO 뷰로 보게 된다" → Z 축 방향이 아닌 **카메라 방향 자체가 ISO**라는 다른 증상 확인

**원인 확정**: [LvDrawingSheet_SelectedIndexChanged](../features/drawing-sheets/lv-sheet-selected.md) 공통부의 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 직전 글로벌 ISO 버튼 상태)을 **그대로 유지한 채 객체로 이동**. 그 후 호출되는 `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 SDK 비동기 렌더 사이에 묻혀 덮어쓰지 못하는 현상으로 추정

**변경 사항**:
- [Form1.DrawingSheets.cs `LvDrawingSheet_SelectedIndexChanged` L542~](../../A2Z/Form1.DrawingSheets.cs): 가공도(-3) 시트일 땐 `FlyToObject3d` **스킵**. `ExecuteMfgDrawing`이 자체 카메라·FitToView·visibility를 모두 세팅하므로 충돌 제거
- [Form1.MfgDrawing.cs L254](../../A2Z/Form1.MfgDrawing.cs): 직전 커밋의 `if (use1803d && longestAxis != "Z")` 가드 **원복** → 원래 `if (use1803d)`. ISO 원인과 무관한 수정이고 Z 최장축 수직 뒤집기 효과를 잃게만 했기 때문. `use1803d` 변수의 블록 바깥 스코프 승격은 유지 (DiagLog 가시성)
- docs: `lv-sheet-selected.md` 변경 이력(T-036 재조정), `mfg-drawing.md` 변경 이력(재해석·원복)
- MSBuild Debug 통과

**영향 범위**: 가공도 시트 선택 시 카메라 동작만. 일반 시트·설치도는 기존 `FlyToObject3d` 호출 유지

---

## 2026-04-23 — T-034/T-036 후속 패치 (사용자 실기 피드백 반영)

**유형**: fix
**커밋**: `537f07c`
**관련 TASK**: T-034 (후속), T-036 (수정)
**사용자 피드백**:
- T-033 ✓ 통과 / T-034 ✓ 통과 (단 BOM 테이블 선택 → 글로벌 ISO 시 **은선 복귀** 발견) / T-036 "가공도 선택 시 세로축이 더 길게 나옴, 가로여야 하는데"

**변경 사항**:
- **T-034 후속** [Form1.DrawingSheets.cs `ApplyDrawingSheetView`](../../A2Z/Form1.DrawingSheets.cs): 내부 2곳(L702 ISO / L735 X·Y·Z) `SetRenderMode(DASH_LINE)` → `SMOOTH`
  - 사용자가 BOM 테이블에서 행을 선택한 상태로 글로벌 ISO/X/Y/Z 버튼을 누르면 `ApplyGlobalView`의 첫 분기(`tabPageDrawing + lvDrawingSheet 선택됨`) 통과 → `ApplyDrawingSheetView`로 진입 → 여기 DASH_LINE 잔존으로 은선 복귀
  - L1433(2D 캡처 경로)은 건드리지 않음
- **T-036 수정** [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs): L215 `if (use1803d)` → `if (use1803d && longestAxis != "Z")` 가드 추가
  - 원인 확정: Z 최장축 + `use180` 조합에서 180° + 90° = 270° 회전 → Z축이 수평 아닌 세로로 뒤집힘
  - 수정: Z 최장축일 때 180° 스킵 → 뒤에 이어지는 L532 90° 회전만 적용 → Z 수평 배치 보장
  - 트레이드오프: Z 최장축일 때 "수직 뒤집기" 효과 잃음 (부재의 비대칭 방향 조정 부분 상실). 가로 배치 우선이 사용자 의도와 일치하므로 수용. 재현 데이터 더 모이면 축 기반으로 180° 재설계 예정

- docs: `global-iso.md` 변경 이력(T-034 후속) / `mfg-drawing.md` 변경 이력(T-036 수정)
- MSBuild Debug 통과

**영향 범위**: 글로벌 뷰 전환 시 은선 복귀 / 가공도 Z 최장축 부재 세로 배치 두 케이스. T-035(선택 해제)는 그대로 작동

---

## 2026-04-22 — T-033/T-034/T-035/T-036 UX 후속 개선 4건

**유형**: feat + fix
**커밋**: `230e45f`
**관련 TASK**: T-033, T-034, T-035, T-036
**사용자 피드백 반영**:
- T-033 "자동 처리 완료 팝업 후에도 치수 계산 중 창이 2초 더 떠있음"
- T-034 "ISO/X/Y/Z 글로벌 버튼에서도 은선 처리되는 거 같아 잘 보이게"
- T-035 "글로벌 뷰 버튼 누르면 특정 부재가 빨간색으로 되어있을 때가 있어서 선택 안 되게"
- T-036 "가공도 눌러도 가장 긴 부분이 가로로 배치되고 fit하게 안 나오는 경우 / 선택 안 되게"

**변경 사항**:
- **T-033** [Form1.BOM.cs `CompleteMainDimensionPostClash`](../../A2Z/Form1.BOM.cs): 순서 재배치
  - 기존: `Osnap → 치수 → MessageBox → GenerateDrawingSheets → finally HideBusyOverlay`
  - 신: `Osnap → 치수 → GenerateDrawingSheets → HideBusyOverlay → MessageBox`
  - 팝업 뜰 때 오버레이 없음, 팝업 닫힌 후 추가 처리 없음. finally HideBusyOverlay는 예외 안전망 유지 (중복 호출 OK)
- **T-034** [Form1.GlobalViews.cs](../../A2Z/Form1.GlobalViews.cs): L100 `ApplySelectedNodesView` + L150 `ApplyFullModelView` 의 `SetRenderMode(DASH_LINE)` → `SetRenderMode(SMOOTH)` 실선 모드로 교체. `ApplyDrawingSheetView` 쪽은 추가 조사 필요로 미변경
- **T-035** [Form1.GlobalViews.cs](../../A2Z/Form1.GlobalViews.cs): `ApplyFullModelView`·`ApplySelectedNodesView` 시작부에 `Object3D.Select(Object3dSelectionModes.DESELECT_ALL)` 추가. 글로벌 뷰 전환 시 T-022로 생긴 빨간 하이라이트 해제. `ApplyDrawingSheetView`는 시트 선택 맥락이라 T-022 유지
- **T-036** [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs): 진입부에 `DESELECT_ALL` 추가. 말미 `DiagLog T-036 MfgDrawing bom=N sizeXYZ=... longestAxis=X/Y/Z isPadOrPlate=bool viewDir=...` 추가 — 사용자 재현 시 "최장축 가로 배치 안 되는 경우" 분석용. 회전 로직 자체는 재현 데이터 확보 후 수정 예정
- docs 갱신: `main-dimension.md` T-033 변경 이력 / `global-iso.md` T-034·T-035 상태 변화·이력 / `mfg-drawing.md` T-036 변경 이력
- MSBuild Debug 통과

**영향 범위**: UX 후속 튜닝 4건 묶음. 치수 추출 플로우 타이밍, 글로벌 뷰 시각 스타일, 선택상태 일관성, 가공도 진단 로그

---

## 2026-04-22 — T-032 치수 계산 성능 최적화 (Osnap 맵 재사용)

**유형**: perf
**커밋**: `6113a16`
**관련 TASK**: T-032
**배경**: 사용자 피드백 "치수 계산 중 창이 오래 떠있음". 원인은 `CollectAllOsnap`과 `ComputeViewDimensionsForMembers`가 각 부재의 `GetOsnapPoint`를 **이중 호출**하던 것 (데이터 구조 차이로 재사용 안 됨)
**선택한 방식**: 옵션 A — CollectAllOsnap이 수집 중 부재별 맵도 같이 구축, ComputeViewDimensionsForMembers가 재사용
**변경 사항**:
- **Form1.cs**: `_lastCollectedNodeOsnapMap` 필드 추가 (`Dictionary<int, List<(Vertex3D, string)>>`)
- **Form1.BOM.cs `CollectAllOsnap`**: 각 부재의 Osnap을 플랫 리스트(`osnapPointsWithNames`)에 넣는 동시에 부재별 맵도 `_lastCollectedNodeOsnapMap`에 적재. 호출 초반 `Clear()` 추가로 이전 호출 잔존 방지
- **Form1.Dimensions.cs `ComputeViewDimensionsForMembers`**: `preBuiltNodeOsnapMap` optional 파라미터 추가
  - 있으면: `memberIndices` 부분만 필터해 재사용 (**GetOsnapPoint 호출 없음**)
  - 없으면: 기존대로 내부에서 `GetOsnapPoint`로 구축 (시트 선택 자동 경로는 다른 부재 집합이라 null 전달)
- **Form1.BOM.cs `CompleteMainDimensionPostClash`**: `_lastCollectedNodeOsnapMap` 전달 → GetOsnapPoint 중복 호출 제거. `Stopwatch` 측정으로 `DiagLog T-032 치수 계산: ... ComputeViewDimensionsForMembers=Xms` 기록
- docs `main-dimension.md` 단계 12·13 재기술 + 변경 이력
- MSBuild Debug 통과

**영향 범위**: 치수추출 버튼 경로의 Osnap 중복 호출 제거로 계산 시간 감소 (대략 절반 수준 예상, 측정 필요). 시트 선택 자동 경로·기타 `ComputeViewDimensionsForMembers` 호출자는 무영향

---

## 2026-04-22 — T-030 시트 선택 시 3D 뷰 치수 렌더링 제거 (T-029 정책 확장)

**유형**: feat
**커밋**: `a01cddb`
**관련 TASK**: T-030
**배경**: T-029로 치수추출 버튼의 3D 뷰 치수 렌더링을 제거했지만, 시트 선택 자동 치수는 여전히 렌더링됨. 사용자 피드백 "시트 눌렀을 때 치수가 나오는데 왜 나오는지 모르겠음"
**결정**: (a) 채택 — 일반 시트 분기에서도 같은 정책 적용
**변경 사항**:
- `LvDrawingSheet_SelectedIndexChanged` 일반 시트 분기에서 `ShowAllDimensions()` 호출 제거
- 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()`로 3D 뷰를 **치수선 없는 깨끗한 상태**로 마감
- `chainDimensionList` · `lvDimension`은 그대로 채움 → 2D 출력·글로벌 뷰 버튼(`ShowAllDimensions(viewDir)`)에서 자동 활용
- `DiagLog "T-030 시트 선택 자동 치수: sheet#=N members=M chain=K (3D 미렌더)"` 기록
- 설치도(-2) 시트는 `ExtractInstallationDimensions`가 이미 3D 미렌더라 그대로 유지 (BBox 기반 데이터만 채움)
- docs `lv-sheet-selected.md` 분기 A 재기술 + 변경 이력

**영향 범위**: 시트 선택 시 UX. 치수 데이터·시트 생성·2D 출력 모두 그대로. 글로벌 뷰 버튼을 눌러야 치수가 보이는 일관된 2단계 UX (T-029 ↔ T-030)

---

## 2026-04-22 — T-031 가공도 시트 선택 시 은선 처리 제거 (SMOOTH 실선)

**유형**: feat
**커밋**: `2812b80`
**관련 TASK**: T-031
**배경**: 사용자 피드백 "가공도 눌렀을 때 은선 처리 안되게 하고 싶어"
**변경 사항**:
- [Form1.MfgDrawing.cs L142](../../A2Z/Form1.MfgDrawing.cs) `ExecuteMfgDrawing` 내 `SetRenderMode(DASH_LINE)` → `SetRenderMode(SMOOTH)` 교체
- 가공도 시트 선택 시 3D 뷰가 **실선 모드**로 표시됨
- 2D 캡처·PDF 출력 내부 경로(L820, L1582)의 DASH_LINE은 그대로 유지 — 이쪽은 2D 도면의 내부 상세 은선용이라 구분
- docs `mfg-drawing.md` 상태 변화 표(`View.RenderMode` 행) + 변경 이력 갱신

**영향 범위**: 가공도 시트 선택 시 3D 뷰 시각 스타일만. 2D 도면 출력은 영향 없음

---

## 2026-04-22 — T-029 치수추출 버튼의 3D 뷰 치수 렌더링 제거

**유형**: feat
**커밋**: `f2bfb1a`
**관련 TASK**: T-029
**배경**: T-028로 chainDimensionList가 6조합까지 채워지니 치수추출 직후 3D 뷰 치수가 과밀. 사용자 피드백: "글로벌 뷰 버튼 누르면 보여주는 것으로 충분"
**변경 사항**:
- `CompleteMainDimensionPostClash` 치수 블록 끝에서 `ShowAllDimensions()` 호출 **제거**
- 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()` 호출 — 이전 렌더 잔존 제거로 "치수선 없는 깨끗한 상태" 마감
- `chainDimensionList`·`lvDimension`은 T-028대로 채움 → 글로벌 X/Y/Z 뷰 버튼이나 2D 출력 시 `ShowAllDimensions(viewDirection)`이 해당 뷰 치수만 필터해 렌더
- docs `main-dimension.md` 단계 14.5(3D 뷰 정리) 추가, 상태 변화에 `Review.Measure` 행 갱신, 변경 이력

**영향 범위**: 치수추출 UX만 변경. 치수 데이터·시트 생성·2D 출력 모두 그대로. 사용자가 뷰 버튼을 눌러야 치수가 나오는 2단계 UX

---

## 2026-04-22 — T-028 치수 로직 4경로 통합 (2D 출력 엔진 기준)

**유형**: refactor + feat
**커밋**: `375d66f`
**관련 TASK**: T-028 (T-027 대체)
**배경**: 4개 경로(치수추출·글로벌 X/Y/Z 버튼·2D 출력·시트 선택 자동)의 치수 로직이 각기 달라 결과 불일치. 사용자 요구: "2D 출력에서 사용하는 Osnap·로직 기준으로 모두 통일"
**변경 사항**:
- **`ChainDimensionData.ViewDirection` 필드 추가** ([Models.cs](../../A2Z/Models.cs)) — 이 치수가 보이는 뷰("X"/"Y"/"Z" 또는 콤마 구분 "X,Y"). 글로벌 뷰 버튼 필터링용
- **`AddChainDimensionByAxis` 반환 `ChainDimensionData`에 `ViewDirection = viewDirection` 기록** — 체인·전체 치수 양쪽
- **공용 헬퍼 `ComputeViewDimensionsForMembers(memberIndices, viewDirection, tolerance)`** 신설 ([Form1.Dimensions.cs](../../A2Z/Form1.Dimensions.cs))
  - 2D 출력 엔진(`nodeOsnapMap` + `FilterOsnapForDimAxis` + `AddChainDimensionByAxis(axis, viewDirection)`) 완전 재사용
  - `viewDirection == null` → 3뷰 × 2축 = 6조합 모두 / `"X"/"Y"/"Z"` → 해당 뷰 2축만
  - 중복 제거: `(Axis, Start, End)` 3자리 반올림 기준, `ViewDirection`은 콤마 누적 (같은 치수가 여러 뷰에 속하면 "X,Y" 식)
- **`ShowAllDimensions` 대폭 단순화** — 내부 분기 ①(Osnap 재추출)·②(nodeOsnapMap 재계산)·③(그대로) 제거. `chainDimensionList`에서 `ViewDirection.Split(',').Contains(viewDirection)` 필터링 + 스마트 필터링만. `isInstallationMode`·`useDirectChain` 변수 제거, 오프셋 분기 단일화
- **`FilterOsnapByViewDimensionUsage`(T-027) 제거** — 2D 출력 로직과 달라 혼동 유발. 대체는 `ComputeViewDimensionsForMembers`
- **`CompleteMainDimensionPostClash` 간소화** ([Form1.BOM.cs](../../A2Z/Form1.BOM.cs)) — visible 부재 계산 후 공용 헬퍼 1회 호출로 대체. `DiagLog T-028 치수 계산: visibleMembers=N chain=M`
- **`LvDrawingSheet_SelectedIndexChanged` 분기 재작성** ([Form1.DrawingSheets.cs](../../A2Z/Form1.DrawingSheets.cs)) — 가공도(-3) `ExecuteMfgDrawing` / **설치도(-2) `ExtractInstallationDimensions`(BBox 유지, 추후 A 전환 여지)** / 그 외 공용 헬퍼
- docs 갱신: `main-dimension.md` 단계 13·변경 이력 / `lv-sheet-selected.md` 분기 A 재작성·변경 이력
- MSBuild Debug 통과

**영향 범위**: 치수 로직 대폭 통합. 4경로가 같은 Osnap 엔진(`nodeOsnapMap` + `FilterOsnapForDimAxis`) 공유. 설치도 시트만 BBox 기반 유지 — 사용자가 나중에 "완전 Osnap 통일(A)"로 전환 가능하도록 분리된 구조

---

## 2026-04-22 — T-027 치수추출 Osnap 선별 (뷰×축 필터 endpoint 합집합)

**유형**: feat
**커밋**: `bb48a16`
**관련 TASK**: T-027
**배경**: 치수 추출 결과 3D 뷰에 체인 치수가 과밀(로그상 chain=32~52). 사용자 의도는 "도면 뷰별 치수 계산에서 살아남는 Osnap만 남기고 나머지는 치수선 생성에 쓰지 말자"
**변경 사항**:
- **선택한 방식**: (a) 체인 치수만 축소 / β — endpoint 합집합 1회 산출 후 축별 1벌 체인 생성
- **`FilterOsnapByViewDimensionUsage(mergedPoints, tolerance)`** 신설 ([Form1.Dimensions.cs](../../A2Z/Form1.Dimensions.cs))
  - X·Y·Z 뷰 × X·Y·Z 치수축 중 뷰≠치수축인 **6개 조합** 각각에서 `AddChainDimensionByAxis` 1차 필터(같은 치수축 값 중 필터축 최소) 로직을 재현해 endpoint 수집
  - 6개 endpoint 집합의 **합집합**(좌표 3자리 반올림 기준 중복 제거)을 반환. 원 순서 보존
- **`CompleteMainDimensionPostClash`** 수정 ([Form1.BOM.cs](../../A2Z/Form1.BOM.cs))
  - `MergeCoordinates` 직후 `FilterOsnapByViewDimensionUsage` 호출로 `filteredPoints` 산출
  - `AddChainDimensionByAxis(filteredPoints, axis, tolerance)` 3회(X/Y/Z) 호출해 `chainDimensionList` 생성 — 기존 뷰 방향 없는 축별 1벌 구조 그대로
  - `DiagLog "T-027 Osnap filter: merged=N → filtered=M"` 기록으로 감소량 정량화
- **보존 대상**: `osnapPointsWithNames`, `lvOsnap`(왼쪽 Osnap 목록) — 제작도·가공도 등 다른 기능이 공유하므로 **원본 유지**
- docs `main-dimension.md` 단계 13.5 추가, 변경 이력 1건
- MSBuild Debug 통과 (경고 0)

**영향 범위**: 치수추출 결과의 체인 치수 개수. 뷰별로 의미 있는 점만 체인의 endpoint로 쓰이므로 3D 뷰 치수선이 깔끔해짐. 2D 도면(`GenerateSheetDrawing2D`) 등 후행은 `chainDimensionList`를 그대로 사용해 자동 반영됨

---

## 2026-04-22 — T-023 v3: Clash 기반 연결성 판정 + 파이프라인 재배치

**유형**: refactor + feat
**커밋**: `cc72e94`
**관련 TASK**: T-023 (v3, 3차 재재설계)
**배경**: 사용자 2차 지시(STRU 단위) 재교정 → "물리적 연결성(Clash 인접) 1덩어리" 기준 확정. 정확성 우선 (방식 A 채택)
**변경 사항**:
- **사전 정리**: 직전 `2a216b5`의 STRU 주석 블록 2개(btnMainDimension 호출부 + 파일 하단 헬퍼) 완전 제거
- **파이프라인 재배치**:
  - 기존: `btnMainDimension` 안에서 BOM → Osnap → 치수 → Clash(비동기) → 이벤트에서 시트 생성
  - 신: `btnMainDimension` 안에서 BOM → Clash(비동기) → 즉시 반환 / Osnap·치수·요약·시트 전부 `Clash_OnClashTestFinishedEvent` → `CompleteMainDimensionPostClash`로 이동
  - 치수 생성 시점이 Clash 결과 수신 후로 미뤄져 **판정 실패 시 치수가 아예 만들어지지 않음** (롤백 불필요)
- **`CompleteMainDimensionPostClash(bool isSingleMember, int clashTestCount)`** 공용 메서드 신설 (Form1.BOM.cs)
  - Osnap 수집 → `MergeCoordinates` → X/Y/Z 체인 → `lvDimension` → `ShowAllDimensions` → 요약 MessageBox → `GenerateDrawingSheets` → `HideBusyOverlay`(finally)
- **`IsSingleConnectedComponent(out int componentCount)`** 헬퍼 신설 (Form1.Clash.cs)
  - Part→Body 역매핑(`bodyToPartIndexMap`) 후 `clashList`로 양방향 인접 그래프 구축
  - BFS로 연결 성분 수 계산, ≥2 발견 즉시 early exit (성능)
  - `bomList.Count == 1`은 항상 통과 (단일 부재)
- **`Clash_OnClashTestFinishedEvent`** 확장: clashList 수집 후 판정 → 실패면 MessageBox + `HideBusyOverlay` + return, 성공이면 `CompleteMainDimensionPostClash(false, testCount)` 호출. 기존 요약 MessageBox는 Post 메서드로 이동
- **T-024 fallback 통합**: 단일 부재(clashStarted=false)는 Clash 이벤트 미발동이므로 `btnMainDimension`에서 직접 `CompleteMainDimensionPostClash(true, 0)` 호출 — 판정 스킵하고 동일 파이프라인 재사용
- **차단 메시지**: "치수 추출은 모든 부재가 하나의 덩어리로 연결되어 있을 때만 가능합니다. 현재: 연결되지 않은 부재 그룹 N개 발견. 해결: 떨어진 부재를 숨기거나 한 덩어리만 선택"
- docs 3종 전면 갱신 — `main-dimension.md` 흐름도 재작성 + 단계표 3섹션(btn/Clash 이벤트/Post) + 분기 C·D + E03/E04 / `clash-finished-event.md` 개요·단계 9·10·분기 B·E03·상태 변화 2열 / 사용자 매뉴얼 `치수 추출.md` 내부 흐름·분기·에러 ③
- MSBuild Debug 통과

**영향 범위**: 치수추출 핵심 흐름 재구성. 치수 생성 타이밍이 Clash 결과 수신 후로 변경. 단일 부재 / 떨어진 부재 / 연결된 다중 부재 세 케이스 모두 같은 Post 메서드를 타는 통합 구조

---

## 2026-04-22 — T-025 BOM 테이블 자동 출력 + T-026 xray 잔존 버그 fix

**유형**: feat + fix
**커밋**: `7614417`
**관련 TASK**: T-025, T-026
**변경 사항**:
- **T-025 (feat)**: `GenerateDrawingSheets()` 끝에 `CollectBOMInfo(false, drawingSheetList[0])` 호출 추가
  - 치수추출 완료 직후 Sheet 1(전체) 기준 BOM 정보가 `lvDrawingBOMInfo`에 자동 표시
  - try/catch로 감싸 SDK 예외 시 `DiagLog`만 기록하고 앱 흐름 보호
  - visibility·카메라는 건드리지 않음 (시트 선택 이벤트의 부수효과 회피)
  - 사용자가 시트를 별도로 클릭하지 않아도 BOM 테이블이 즉시 채워짐
- **T-026 (fix)**: `btnMainDimension_Click` 진입부에 `xraySelectedNodeIndices.Clear()` 추가
  - **증상**: 부재 1개 띄우고 치수추출 → 전체 띄우고 치수추출 → **1개 기준 결과 재현** (`chain=32` 동일)
  - **원인**: `LvDrawingSheet_SelectedIndexChanged`가 시트 선택 시 설정하는 `xraySelectedNodeIndices` 값이 잔존, `CollectBOMData` L591의 X-Ray 우선 필터에 계속 걸려 "그 부재만" 수집
  - **로그 근거**: `[10:58:25] sheet#=1 members=1 → xray=1 설정` → `[10:58:34] btnMainDimension ENTER xray=1 → EXIT chain=32` (전체 띄운 뒤에도 1개 기준)
  - **원칙 확립**: "치수추출 버튼은 항상 현재 visible 기준". 특정 부재 치수는 시트/BOM 행 선택 경로가 담당
- docs: `main-dimension.md` 단계 1.3 (xray clear) / `generate-sheets.md` 단계 9.5 (BOM 자동 수집) 추가, 변경 이력 각 1건
- MSBuild Debug 통과

**영향 범위**: 치수추출 정상 흐름. T-016(3회 누적 간헐 버그)과 별개의 잔존 상태 버그 해결

---

## 2026-04-22 — T-023 재설계: STRU 단위 가드로 변경 (현재 비활성)

**유형**: refactor
**커밋**: `2a216b5`
**관련 TASK**: T-023
**변경 사항**:
- 사용자 의도 재확인: "부재 개수 1"이 아니라 **"STRU(모델트리 상위 UDA 단위) 1개"** 기준
- 직전 `1620289`의 "visible/selected == 1" 가드 **제거** (사용자 의도와 불일치)
- 새 `FindAncestorByUda(startIndex, key, value, maxDepth)` + `CheckSingleStruCondition()` 헬퍼를 **완성 형태 + 블록 주석**(`/* */`)으로 `Form1.BOM.cs` 하단에 보존
  - 선택 기반 → visible 기반 순서로 평가, 공통 조상 STRU 집합 크기 1일 때만 통과
  - 부모 탐색은 `CollectBOMInfo`의 UDA 순회 패턴 재사용
  - `Object3dFilter.SELECTED`로 프로그래매틱 선택 상태까지 포함
  - 실패 시 MessageBox + `DiagLog BLOCKED visibleStru=N selectedStru=M`
- `btnMainDimension_Click` 진입부의 호출도 `/* */` 주석 처리
- 상수 `STRU_UDA_KEY="UNIT_TYPE"`, `STRU_UDA_VALUE="STRU"`는 임시 placeholder (`TODO:` 주석). UDA 확정 시 이 두 상수 교체 + 주석 제거만으로 활성화
- docs 원복: `main-dimension.md` 단계 1.5 · 분기 D · E04를 "비활성" 표기로 교체, 사용자 매뉴얼 `치수 추출.md` 에러 ③/단계 1-2 삭제 후 "향후 추가 예정" 예고 문구로 치환
- TASKS T-023 상태: `IN_PROGRESS` → `BLOCKED` (UDA 키·값 확정 대기)
- MSBuild Debug 통과 (주석 블록이라 컴파일 영향 없음)

**영향 범위**: 치수 추출 가드 일시 비활성 — 현재는 기존처럼 모델 로드 + 예외만 검사. STRU 가드는 UDA 확정 시 활성화

---

## 2026-04-22 — T-023 치수추출 사전조건 가드 (단일 부재)

**유형**: feat
**커밋**: `1620289`
**관련 TASK**: T-023
**변경 사항**:
- `btnMainDimension_Click` 진입부에 단일 부재 가드 추가
  - `GetPartialNode(false,false,true)` 순회로 visible 부재 카운트
  - `Object3D.FromFilter(Object3dFilter.SELECTED_TOP)`로 selected 카운트 (T-022로 확보한 선택상태 API 활용)
  - 둘 다 ≠ 1이면 MessageBox로 차단 + `DiagLog BLOCKED visible=N selected=M` 기록
  - 허용 케이스: 시트/BOM 행 선택 → T-022로 selected==1 / 모델트리 체크박스로 visible==1 / 3D 뷰 단일 클릭
- 개발자 문서 `main-dimension.md`: 사전조건 항목 1건·단계 1.5·에러 E03 추가 (기존 E03은 E04로 재번호)
- 사용자 매뉴얼 `치수 추출.md`: 선결조건·단계 1-2·에러 ③ 추가
- MSBuild Debug 통과

**영향 범위**: 자동 치수 추출 진입 조건. 다중 부재 상태에서 실행은 이제 차단됨 (안전망). 기존에 전체 보기 상태에서 치수 추출을 쓰던 흐름은 단일화 절차가 필요 — UX 전환

---

## 2026-04-22 — T-024 단일 부재 치수추출 시 시트 목록 미갱신 버그 수정

**유형**: fix
**커밋**: `06a1395`
**관련 TASK**: T-024
**변경 사항**:
- **원인**: `DetectClash` 내부 루프가 `targetNodes.Count == 1`이면 쌍 없어 `clashCount == 0` → return false → `PerformInterferenceCheck` 미호출 → `Clash_OnClashTestFinishedEvent` 미발동 → 이벤트에서 호출되던 `GenerateDrawingSheets` 미실행 → 시트 목록 갱신 안 됨. 부가적으로 간섭 없는 다중 부재도 이벤트 내 `if (clashList.Count > 0)` 조건에 걸려 시트 안 생기던 숨은 버그 공존
- **수정 1**: `btnMainDimension_Click` — `bool clashStarted = DetectClash()` 반환값 수신. false면 `GenerateDrawingSheets()` + 요약 MessageBox 직접 호출 (fallback 경로)
- **수정 2**: `Clash_OnClashTestFinishedEvent` — `if (clashList.Count > 0)` 조건 제거하고 `GenerateDrawingSheets()`를 **항상** 호출. 내부 `bomList.Count > 0` 가드로 안전
- docs: `main-dimension.md` 단계표 10→13 재번호 + 분기 C 신설, `clash-finished-event.md` 단계 10 재기술 + 분기 A 수정
- MSBuild Debug 통과

**영향 범위**: 단일 부재 / 간섭 없는 다중 부재의 자동 처리 경로. 간섭 있는 다중 부재는 기존 동작 그대로

---

## 2026-04-22 — T-022 시트/BOM 선택 시 3D View 선택상태 동기화

**유형**: feat
**커밋**: `ab8313e`
**관련 TASK**: T-022
**변경 사항**:
- `vizcore3d.Object3D.Select(List<int>, true, false)` + `Select(DESELECT_ALL)` 조합으로 "선택상태(빨간 하이라이트)" 구현
- `LvDrawingSheet_SelectedIndexChanged` — 시트의 **기준부재** 하이라이트
  - Sheet 1(-1)·설치도(-2) 생략 (기준부재 개념 없음)
  - 가공도(-3) → `MemberIndices[0]` / Sheet 2+ → `BaseMemberIndex`
- `LvDrawingBOMInfo_SelectedIndexChanged` — **단일 부재** 하이라이트 + 카메라 fit (visibility 유지)
- `pivot=false`로 회전 피봇 간섭 방지, `DESELECT_ALL` 선행으로 누적 방지
- **피드백 루프 분석**: `Object3D_OnObject3DSelected`는 `dgvAttributes`만 갱신하고 ListView는 건드리지 않아 안전. 부수효과로 **부재 정보 탭이 자동 갱신**되어 UX 향상
- SDK 확정 경로: `sdk-verifier` 서브에이전트로 `VIZCore3D.NET.xml` L51882~51946 검증
- docs 2종(`lv-sheet-selected.md`, `lv-bom-info-selected.md`) 단계표·상태 변화·변경 이력 갱신

**영향 범위**: 도면정보 탭 UX (시트·BOM 행 선택). 기존 카메라 fit·visibility 동작은 그대로, 선택상태만 추가

---

## 2026-04-22 — T-018 장시간 작업 진행 오버레이 (1차: 치수 추출)

**유형**: feat
**커밋**: `ccb9cb4`
**관련 TASK**: T-018
**변경 사항**:
- 공통 헬퍼 `ShowBusyOverlay(msg)` / `HideBusyOverlay()` 신설 ([Form1.cs](../../A2Z/Form1.cs) L183~L222)
  - 3D 뷰어(`panelViewer`) 중앙에 "처리 중..." 반투명 Label(맑은 고딕 14pt Bold, 260×70, 배경 #2D2D30)
  - 최초 호출 시 지연 생성 → 이후 재사용. 크기는 panelViewer 기준 자동 센터링
  - `Application.DoEvents()`로 즉시 화면 반영
- `btnMainDimension_Click`에 try/finally 구조로 오버레이 적용
  - 각 장시간 단계 진입 시 메시지 갱신: 치수 추출 중 → Osnap 수집 중 → 치수 계산 중 → 간섭검사 실행 중
  - finally에서 `HideBusyOverlay()` 호출 — 정상·예외 모두 해제
  - Clash는 비동기라 해제 후에도 완료 콜백의 MessageBox 정상 동작
- 문서 `main-dimension.md` 단계표를 10→12단계로 확장 (2·12단계에 오버레이 표시·해제 추가), 변경 이력 1건

**영향 범위**: 치수 추출 UX. 다른 장시간 작업(2D 생성·가공도·PDF·시트 생성)은 1차 반응 보고 2차에서 확장 검토. 기능 로직 무변경

---

## 2026-04-22 — T-017 라이선스 코드 Form1.License.cs로 분리

**유형**: refactor
**커밋**: `d849663`
**관련 TASK**: T-017
**변경 사항**:
- `Form1.BOM.cs`에 섞여 있던 라이선스 관련 코드 전부를 신규 partial `Form1.License.cs`로 이동
  - 이동 대상: `StartLicenseRefreshTimer`, `LicenseRefreshTimer_Tick`, `licenseRefreshTimer` 필드, `Vizcore3d_OnInitializedVIZCore3D`의 `License.LicenseServer("127.0.0.1", 8901)` 초기 호출 2줄
  - 새 진입점 `InitializeLicense()` — 서버 연결 실패 시 MessageBox + `false`, 성공 시 갱신 타이머 시작 + `true`
  - `Vizcore3d_OnInitializedVIZCore3D` 진입 블록 10줄 → `if (!InitializeLicense()) return;` 한 줄로 축약
- `A2Z.csproj`에 `Form1.License.cs` Compile 항목 추가 (`DependentUpon=Form1.cs`, `SubType=Form`)
- `Form1.cs`에서 `licenseRefreshTimer` 필드 선언 제거 (License.cs로 이동)
- docs: `code-reference/form1-bom.md` 라이선스 항목 5곳(헤더 라인 수·핸들러 설명·헬퍼 표·필드 표·API 사용) 정리, `code-reference/form1-license.md` 신설, `features/bom/vizcore3d-initialized.md` 단계표·E01 에러·관련 링크·변경 이력 갱신
- 기능 변경 없음 (순수 리팩토링). MSBuild Debug 통과, 경고 0. 사용자 실기에서 앱 기동 정상 확인

**영향 범위**: 라이선스 로직 파일 경계만. 호출 규약은 동일 — 다른 핸들러/모듈 무영향

---

## 2026-04-22 — T-014 시트 목록 item 번호 표시 + T-021 BOM 행 카메라 fit

**유형**: feat
**커밋**: `9b99b8c`
**관련 TASK**: T-014, T-021
**변경 사항**:
- **T-014 (`lvDrawingSheet` 표시 포맷)**: 기준부재/포함부재 컬럼을 부재 이름 대신 **item 번호**(= `bomList` 순서 i+1 = ISO 풍선 번호 = BOM 정보 탭 No.)로 표시
  - Sheet 1 → "전체 / 전체"
  - Sheet 2+ → `{기준번호} / {포함 번호 오름차순 콤마}` (예: `1 / 1, 3, 5`)
  - 설치도 → "설치도 / {전체 item 번호}"
  - 가공도 → `{MemberIndices[0]의 item 번호} / 공란`
  - 시트 생성 로직은 T-015 그대로 유지 (표시 전용 변경)
  - `bomIndexToItemNo` Dictionary 신설 후 ListView 채우기 블록 전면 재작성 (Form1.DrawingSheets.cs L215~281, +약 50줄)
  - 빌드 오류 1건 수정: 상단 `int mfgNo=1`(가공도 번호)과 변수명 충돌 → `mfgBomIdx`/`mfgItemNo`로 리네임
  - 문서 `generate-sheets.md` 단계 10 설명·상태 변화 섹션·변경 이력 갱신
- **T-021 (`lvDrawingBOMInfo` 행 선택 핸들러)**: BOM 테이블 행 선택 시 해당 부재로 카메라만 fit
  - 가시성은 그대로 두고 `vizcore3d.View.FlyToObject3d(new List<int>{bodyIdx}, 1.2f)` — 현재 시트 맥락 유지
  - No. 컬럼 파싱 → `bomList[No-1].Index` Body 조회 (CollectBOMInfo의 `partIndexToBomNo` = `bi+1` 매핑과 일치)
  - 요약행(Row 0) · No 파싱 실패 · 범위 초과는 조용히 return, SDK 예외는 `DiagLog`로 기록
  - Form1.cs L166에 `lvDrawingBOMInfo.SelectedIndexChanged += LvDrawingBOMInfo_SelectedIndexChanged` 등록
  - 신규 문서 `lv-bom-info-selected.md` (SHT-010) + `_index.md` 등록 추가

**영향 범위**: 도면정보 탭 UI(시트 목록 + BOM 테이블) 상호작용. 시트 생성·가공도·설치도 내부 로직은 변화 없음. 사용자 실기 테스트 통과 (2026-04-22)

---

## 2026-04-21 — T-015 Sheet 생성 로직 재설계 (모든 부재가 기준부재)

**유형**: feat (기능 변경)
**커밋**: `9b870a0`
**관련 TASK**: T-015
**변경 사항**:
- **문제**: `GenerateDrawingSheets` L105-142의 `appearedAsIncluded` 스킵 로직이 "부재가 이미 다른 시트에 포함부재로 등장하면 기준부재가 될 수 없음"을 강제 → 1-2-3-4 연쇄 Clash 시 Sheet 2(기준 1, {1,2}) + Sheet 3(기준 3, {3,2,4}) 2개만 생성. 사용자 의도(각 부재가 자기 기준 시트를 가짐)와 불일치
- **수정**: Form1.DrawingSheets.cs에서 `HashSet<int> appearedAsIncluded` 선언, `Contains` 스킵 조건, `Add` 호출 3곳 전부 제거. 주석도 T-015 결정 배경으로 교체
- **결과**: 모든 부재가 각자 기준부재로 등장하며 자기 + 1-hop 이웃 시트 생성. 1-2-3-4 연쇄 Clash → Sheet 2(1), 3(2), 4(3), 5(4) 4개. 단계 9 Sheet 1 중복 제거는 유지되어 과잉 정리 자동
- 문서 `generate-sheets.md` 전면 갱신: flowchart 재작성, 단계표 11단계로 확장(Part↔Body 매핑·인접 리스트·가공도·중복 제거 추가), 분기 B·C 재정의, 상태 변화 시트 수 공식, 변경 이력 한 줄
- **부수 정리**: 기존 문서의 E03(clashList 비어있을 때 return) 서술은 실제 코드에 없어 삭제. 대신 `clashList` 공백 시 "일반 시트들이 자기 자신만 포함"이라는 실제 동작 주석 추가
- 사용자 빌드·실기 검증은 본인 기기에서

**영향 범위**: 시트 생성 로직 전체 + 대응 문서. 설치도·가공도·Sheet 1 로직은 변경 없음

---

## 2026-04-21 — T-013 OPT-B2 진단 로그 확장 (MoveObject 유효성 검증)

**유형**: chore
**커밋**: `7688905`
**관련 TASK**: T-013
**변경 사항**:
- OPT-B2 구현 후 사용자 보고: 6.21mm 이동이 계산됐는데 시각적으로 "위치 전혀 안 바뀜"
- 진단 로그 확장 — `MoveObject` 직후 `objId`의 실제 최종 상태 기록:
  - `objFinal=(x,y)` — 이동 후 실제 중심 (target과 일치하는지 검증)
  - `objFinalSize=(w,h)` — 렌더된 실제 크기 (obj가 너무 작아 보이는지 확인)
  - `move=(dx,dy)` — 실제 호출된 이동량
- 이전 커밋(ebef55d)에서 DiagLog 메시지에 `objFinalCX/CY/W/H` 참조를 넣었으나 변수 선언이 누락된 상태였음 → 이번에 선언 + 계산 추가로 컴파일 건전성 복구
- 판정 기준: `objFinal ≈ target`이면 이동 정상이고 `objFinalSize`가 작아 체감이 적은 것; `objFinal ≠ target`이면 `MoveObject` 자체 무효화 의심

**영향 범위**: 진단 로깅만. 흐름 무변화

---

## 2026-04-21 — T-013 옵션 B2 재수정 — bg BBox 꼭지점 8개 투영 기반 비율

**유형**: fix
**커밋**: `ebef55d`
**관련 TASK**: T-013
**변경 사항**:
- 1차 보정(`* bgFinalScale`) 결과 여전히 부정확 (실측 `offsetRatio.Z=-0.244` → 7.3mm 이동이 정답인데 5.9mm 계산)
- **근본 원인 확정**: `bgFinalScale`은 "객체 원본 좌표 → 현재 표시 크기" 비율, `WorldToScreen`은 "3D → 원본 캔버스" 좌표. 두 변환 체인이 서로 다른데 한 스케일로 퉁치면 오차 발생
- **정확한 공식**:
  1. bg의 3D BBox 8개 꼭지점을 모두 `WorldToScreen`으로 변환
  2. 결과 8개 점의 X/Y min·max로 **원본 캔버스상 bg의 BBox 폭/높이** (`bgScreenW/H`) 계산
  3. bg의 현재 렌더 크기(`GetObjectSize` → `bgCanvasW/H`) 대비 비율 `ratio = bgCanvasSize / bgScreenBBox`
  4. `target = bgCanvas + dScreen × ratio`
- 실측 검증: `dScreen.Y=195.97 × (30.0/bgScreenH) ≈ 7.3mm` = `offsetRatio.Z × bgCanvasH = 0.244 × 30 = 7.3mm` ✅
- DiagLog 라벨 `OPT-B` → `OPT-B2`, `bgScreenBBox`/`ratio` 필드 추가
- A2Z.exe 실행 중이라 빌드 자동 검증 생략, 사용자 빌드에 맡김

**영향 범위**: Sheet2+ ISO objId 위치만. 다른 로직 무영향

---

## 2026-04-21 — T-013 옵션 B 스케일 보정

**유형**: fix
**커밋**: `2d5fb5f`
**관련 TASK**: T-013
**변경 사항**:
- 옵션 B 1차 시도 결과: obj가 "엄청 멀리" 생김 (사용자 실측 11:06:29)
  ```
  bg3D=(26368.5, -5824.0, 17673.0)   obj3D=(26368.5, -5824.0, 17391.0)   (Z -282mm 차이)
  bgScreen=(163.00, 166.01)          objScreen=(163.00, 361.98)          (dScreen.Y=195.97)
  ```
- **진단**: `WorldToScreen`은 **원본 캔버스 좌표**(스케일 적용 전) 반환. 그런데 `bgObjId`는 이미 `RescaleObject(bgFinalScale=0.0301)`로 축소된 상태 → 두 좌표계 불일치 → `dScreen` 그대로 더하면 195mm 이동(A4 세로 210mm 거의 끝)
- **수정**: `target = bgCanvas + dScreen * bgFinalScale`
  - 검증값: `195.97 × 0.0301 = 5.90 mm` → 셀(95mm) 내부에서 Z축 3D 차이(-282mm)를 반영한 자연스러운 위치
- 변경 분량: 2줄 (`targetX`, `targetY`에 `* bgFinalScaleB` 추가)
- 빌드 검증: A2Z.exe 실행 중이라 DLL 잠금으로 이번 세션 자동 검증 불가. 사용자 빌드에 맡김

**영향 범위**: Sheet2+ ISO 뷰 objId 위치만. 다른 로직 무영향

---

## 2026-04-21 — T-013 옵션 B: WorldToScreen 기반 objId 위치 보정

**유형**: fix
**커밋**: `705613a`
**관련 TASK**: T-013
**변경 사항**:
- **옵션 A 실패 확정** (사용자 실측 11:00:06 로그):
  ```
  bgScale=0.0301 objScale=0.0050
  bgCenter=(49.50,157.50) objCenter=(0.00,0.00)
  ```
  objId가 원점 (0,0)에 0.005 스케일로 남아 사실상 보이지 않음 → SDK 자동 매핑 없음 확인
- **옵션 B 구현** (Form1.DrawingSheets.cs `RenderSheetViewForDrawing` isIsoFullView 분기):
  - 전체 BOM 3D BBox 중심 + 시트 부재 3D BBox 중심 계산 (`bomList.MinX/MaxX/...`)
  - 각 중심을 `vizcore3d.View.WorldToScreen(Vertex3D, true)`로 캔버스 좌표 변환
  - objId를 bgFinalScale과 동기 스케일링 (`RescaleObject`)
  - objId 중심을 `bgCanvas + (objScreen - bgScreen)`로 이동 (`MoveObject`)
- DiagLog `OPT-B` 라벨로 3D 중심 / 화면 좌표 / 이동량 / 최종 스케일 모두 기록 — 다음 테스트 결과 즉시 검증 가능
- SDK API 근거: [VIZCore3D.NET.xml:63853](../../VIZCore3D.NET.xml) `ViewManager.WorldToScreen`

**영향 범위**: Sheet2 이상 시트의 ISO 뷰 렌더링만. 비-ISO / Sheet1 미영향

---

## 2026-04-21 — T-020 파일 열기·치수 추출을 탭 밖 공용 패널로 이동

**유형**: feat (UX)
**커밋**: `29e177f`
**관련 TASK**: T-020
**변경 사항**:
- `panelGlobalActions` 신설 (splitContainer1.Panel1, Dock.Top, 438×60)
  - 위치: panelGlobalViewButtons 아래, tabControlLeft 위
  - 배경색 통일 (`45,45,48` — 글로벌 뷰 버튼 패널과 같음)
- `btnOpen`(파일 열기), `btnMainDimension`(치수 추출) 이관
  - 기존: `tabPageWork > groupBox1` (작업/데이터 탭에서만 보임)
  - 신규: `splitContainer1.Panel1 > panelGlobalActions` (모든 탭 공통)
  - Location (x, 25) → (x, 5)
- `groupBox1` 후속 정리: Size 110→55, 작은 버튼 6개(BOM/Clash/Osnap/치수/2D 생성/PDF 내보내기) Y=78→20 위로 당김
- 자동화된 사용자 흐름(파일 → 치수 추출 → 2D 도면 → 가공도) 중 첫 2단계를 항상 한 손에 접근 가능하게 함 (담당자 목표 = 자동화)
- 사용자 직접 빌드·실행 확인 완료

**영향 범위**: UI 레이아웃만. 핸들러 흐름·이벤트 핸들러 참조 영향 없음

---

## 2026-04-21 — T-019 탭 순서 재배열 (도면정보를 첫 번째로)

**유형**: feat (UX)
**커밋**: `3f51a02`
**관련 TASK**: T-019
**변경 사항**:
- `tabControlLeft.Controls.Add` 순서 변경: 도면정보 → 작업/데이터 → 부재 정보
- `tabPageDrawing.TabIndex = 0`, `tabPageWork.TabIndex = 1`, `tabPageAttribute.TabIndex = 2`
- 앱 실행 시 `SelectedIndex = 0`에 의해 **도면정보 탭이 기본 선택**됨 — 사용자(담당자) 최종 목표가 제작도 출력이라 즉시 작업 화면 노출
- 프로그래밍 위험 전수 검증: `SelectedTab == tabPageDrawing` 등 모든 참조가 **탭 객체 기반**이라 순서 변경 안전
- 런타임 로직/이벤트 핸들러/핸들러 흐름 영향 **0** (Designer 메타데이터만 변경)

**영향 범위**: UI 탭 순서. 기존 기능·핸들러 영향 없음

---

## 2026-04-21 — T-013 옵션 A 시도 (Sheet2+ ISO 위치 정합)

**유형**: fix (시도)
**커밋**: `cac4eb3`
**관련 TASK**: T-013
**변경 사항**:
- **원인 확정**: `RenderSheetViewForDrawing`의 `isIsoFullView` 분기에서 bgObjId/objId 모두 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin`로 캔버스 원점에 생성 → `GetObjectCenter`가 둘 다 (0,0) 반환 → 기존 위치 보정 공식 `(objCX0 - bgCX0) * scale`이 0에 가까워져 obj가 bg 중심으로 이동하던 현상
- **옵션 A 시도**: Form1.DrawingSheets.cs L1430~1468 범위의 objId 변환 로직 전체(RescaleObject + GetObjectCenter 보정 + MoveObject + 디버그 출력) 제거
- SDK가 동일 카메라·동일 원점에서 만든 두 객체를 동일 좌표계로 자동 매핑하는지 검증
- DiagLog로 bgObjId/objId의 스케일·중심·원본좌표 실측 기록 (다음 테스트 시 로그로 결과 판정)
- 실패 시 옵션 B(`WorldToScreen` 기반 3D→2D 좌표 변환)로 전환 예정 — SDK API 이미 확인됨

**영향 범위**: Sheet2 이상 시트의 ISO 뷰 렌더링. 비-ISO 뷰(X/Y/Z) 및 Sheet1(전체) 미영향

---

## 2026-04-21 — T-016 진단 로그 파일 저장 방식 전환

**유형**: chore
**커밋**: `53c6245`
**관련 TASK**: T-016
**변경 사항**:
- Form1.cs에 `DiagLog` 헬퍼 신설 — 파일(`{exe}/logs/diag-{YYYY-MM-DD}.log`) + VS 출력창 병행 기록
- 기존 T-016 진단용 `Debug.WriteLine` 13곳 → `DiagLog`로 일괄 교체 (Python 스크립트)
  * `Form1.BOM.cs btnMainDimension_Click` 3곳
  * `Form1.Dimensions.cs btnExtractDimension_Click` 3곳
  * `Form1.DrawingSheets.cs LvDrawingSheet_SelectedIndexChanged` 5곳
  * `Form1.GlobalViews.cs ExtractInstallationDimensions` 2곳
- Release 빌드 + 다른 기기 실행에서도 로그 파일 생성되어 T-016 재현 진단 가능
- `.gitignore`의 기존 `[Ll]ogs/` 패턴으로 로그 파일 자동 제외

**영향 범위**: 진단 로깅만. 기능·흐름 변경 없음

---

## 2026-04-20 — T-016 진단 로그 인프라 추가 (간헐 버그 추적용)

**유형**: chore
**커밋**: `0b5731c`
**관련 TASK**: T-016 (BLOCKED 전환)
**변경 사항**:
- 치수 추출 흐름의 4개 핵심 지점에 `Debug.WriteLine` 진단 로그 추가
  - `Form1.BOM.cs btnMainDimension_Click` ENTER/EXIT (xray·chain·osnap·bom 카운트)
  - `Form1.Dimensions.cs btnExtractDimension_Click` ENTER/EXIT
  - `Form1.DrawingSheets.cs LvDrawingSheet_SelectedIndexChanged` ENTER/SKIP/EXIT/FAIL (sheet#, prevXray, prevChain)
  - `Form1.GlobalViews.cs ExtractInstallationDimensions` ENTER/EXIT (members, chain)
- `LvDrawingSheet_SelectedIndexChanged`의 silent catch (`Debug.WriteLine($"도면 시트 표시 중 오류: {ex.Message}")`)에 **stack trace 추가**
- 모든 로그에 `[T-016 진단 로그]` prefix 또는 `HH:mm:ss.fff` 시각으로 필터링·시계열 분석 가능
- 다음 재현 시 Visual Studio 출력창 로그를 사용자가 공유하면 즉시 원인 특정 가능
- T-016 상태 `IN_PROGRESS → BLOCKED (재현 조건 수집 중)`로 이동 + 의심 가설 4개 보존

**영향 범위**: 치수/시트 흐름 4개 핸들러에 로깅만. 기능·흐름 변경 없음 (R9 기준 docs 갱신 불필요)

---

## 2026-04-20 — 시드 서브에이전트 2개 도입 (sdk-verifier, md-link-checker)

**유형**: feat
**커밋**: `92d0488`
**관련 TASK**: T-011
**변경 사항**:
- `.claude/agents/sdk-verifier.md` 신설 — VIZCore3D.NET.xml 선행 검색으로 SDK API 존재·시그니처·공식 사용 패턴 반환
- `.claude/agents/md-link-checker.md` 신설 — `docs/**/*.md` 링크 공백·파일 부재 검증 + Python 치환 스크립트 제안
- `CLAUDE.md` R10, R11 추가 — 각 에이전트 호출 트리거 주소
- 배경: 이번 세션에서 드러난 반복 실수(`RenderModes.SOLID` 존재 가정, `Model.Close` 누락, 링크 공백 133건) 방지
- 오케스트레이터 프로토콜(동적 생성·합병·삭제)은 사용 패턴 축적 후 재평가 — 중간 도입 경로 채택

**영향 범위**: 개발 워크플로우. 코드 변경 없음.

---

## 2026-04-20 — T-006/T-009 빌드 테스트 후속 + T-010 링크 치환 + 자동 push 활성화

**유형**: fix + chore
**커밋**: `10c7d8c`
**관련 TASK**: T-006, T-009, T-010
**변경 사항**:
- **T-006 후속** (템플릿 폭 재조정): BOM/tableInfo 열 너비 합 81→**77mm** 추가 축소. BOM: ITEM 19→17, MATERIAL/SIZE 12→11. tableInfo: 32/49→30/47. (RenderTemplateOnGridStructure가 셀 92.3mm 내부에 추가 패딩 존재)
- **T-009 후속** (Clear2DView 시점 수정): `Clear2DView()` 호출을 `Model.Open` 성공 이후로 이동. 기존엔 Open 직전에 호출했는데 Open이 2D 뷰를 자동 복원하여 효과 없었고 번쩍임 4회 발생. 이제 Open 성공 분기 내부에서 마지막 단계로 실행
- **T-010** (링크 공백 일괄 치환): `docs/**/*.md` 전체 마크다운 링크 `]( ... )` 내부 공백을 `%20`으로 치환. Python 스크립트로 30파일 147건. 외부 URL(http/https/mailto), 앵커(#), 공백 없는 링크는 제외 처리
- **chore** (/commit 자동 push 통합): CLAUDE.md R5 개정, `.claude/commands/commit.md`의 단계 9에 자동 push 추가, 메모리에 `Commit Auto-Push` feedback 기록. 다중 기기 테스트 환경 지원

**영향 범위**: BOM 카테고리 (Form1.BOM.cs `ResetToInitialState`), DrawingSheets 카테고리 (BOM/tableInfo 폭), docs/ 전체 링크, 개발 워크플로우 (자동 push)

---

## 2026-04-20 — 초기화 버튼 추가 + 같은 파일 재Open 버그 수정

**유형**: feat + fix
**커밋**: `45d17dd`
**관련 TASK**: T-008
**변경 사항**:
- `btnResetToInitial` ("초기화", 회색) 신설 — 3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽
- `ResetToInitialState()` — 누적 상태 전면 초기화 후 `currentFilePath` 동일 경로로 재로드
  - 정리 대상: bomList/clashList/osnapPoints/osnapPointsWithNames/chainDimensionList/xraySelectedNodeIndices/drawingSheetList/bodyToPartNameMap/balloonOverrides + lv* ListView 5종 + SDK Review.Measure/ShapeDrawing/Review.Note
  - `balloonOverrides.Clear()` 포함 (btnOpen이 누락했던 항목)
- **버그 수정**: VIZCore3D는 같은 경로 중복 `Model.Open()`을 거부 (false 반환)
  - `ResetToInitialState()` 및 `btnOpen_Click` 양쪽에 `if (Model.IsOpen()) Model.Close();` 선행 호출 추가
  - 근거: VIZCore3D.NET.xml 공식 예제 L47297, L60261 패턴
- **UI 너비 축소**: 5개 글로벌 뷰 버튼 Size 105→80, Location 재배치(8/93/178/263/348), 패널 Size 558→438
- 문서 신규:
  - `docs/features/bom/reset-to-initial.md` (BOM-005)
  - `docs/사용자-매뉴얼/1.기본-작업/초기화.md`
- 문서 갱신:
  - `docs/features/bom/open-model.md` — Close 단계 추가, flowchart·step table·변경 이력
  - `docs/features/bom/_index.md` — BOM-005 항목 + 의존성 다이어그램 재로드 화살표
  - `docs/code-reference/form1-bom.md` — 새 핸들러 섹션 + 라인 번호 shift 반영
  - `docs/사용자-매뉴얼/README.md` — 1.기본 작업에 [초기화] 링크

**영향 범위**: BOM 카테고리 (Form1.BOM.cs + Form1.Designer.cs) + 대응 문서. 핸들러 흐름 변경 있음 (btnOpen 포함 2개 흐름에 Close 단계 삽입)

---

## 2026-04-14 — 사용자 매뉴얼 전면 작성 (39개 버튼 문서)

**유형**: docs
**커밋**: `74fe209`
**관련 TASK**: T-003
**관련 REQUEST**: REQ-001
**변경 사항**:
- `docs/사용자-매뉴얼/` 신규 폴더 생성 — 40개 파일 (README + 39 버튼 문서)
  - `1.기본-작업/` 2개 (파일 열기, 치수 추출)
  - `2.작업-데이터 탭/` 12개
  - `3.부재 정보 탭/` 7개
  - `4.도면정보 탭/` 6개
  - `5.목록 조작/` 12개
- 7섹션 표준 템플릿 적용 (한 줄로 / 버튼 위치 / 사전 조건 / 누르면 순서 / 분기 / 에러 / 이어지는 작업 / 자세히 보기 / 변경 이력)
- 실제 UI 라벨(Form1.Designer.cs `.Text = "..."` 원본)을 파일명·위치 표기에 사용
- SDK 용어 전면 번역 적용 (`DASH_LINE` → "은선(점선) 모드", `bomList` → "BOM 목록" 등)
- 에러 메시지는 실제 MessageBox 팝업 문구 원문 그대로 수록
- `docs/README.md` 상단에 "개발자용 / 사용자용" 분기 카드 추가
- 개발자 문서(`docs/features/`, `docs/code-reference/`)는 건드리지 않음

**실행 방식**: 멀티 에이전트 (인벤토리 W-D 선행 → Writer W-A/B/C 병렬 작성 → Reviewer 전수 검토)
**검토 결과**: 템플릿 0위반 / 용어 0위반 / 깨진 링크 0건 / 에러 메시지 샘플 3건 전부 일치

**영향 범위**: 신규 문서 생성만 (코드 변경 없음)

---

## 2026-04-13 — 워크플로우 자동화 확장 (REQUESTS + /checkpoint + docs-sync 훅)

**유형**: chore
**커밋**: `ac14c86`
**관련 TASK**: T-002
**변경 사항**:
- `docs/tracking/REQUESTS.md` 신규 — 본인 수정 요청 inbox (REQ-xxx, 우선순위/배경/기대효과 필드)
- `.claude/commands/checkpoint.md` 신규 — 세션 요약 저장 슬래시 커맨드
  - 주제 kebab-case 변환, 중복 시 suffix
  - 필수 섹션: "이어갈 지점" (다음 세션 복원용)
  - git 미커밋 변경 있으면 ⚠️ 경고 자동 추가
- `.claude/settings.json` 신규 — PostToolUse 훅 등록 (Edit|Write 매처)
- `.claude/hooks/docs-sync-reminder.sh` 신규 — `Form1.*.cs` 수정 시 docs 동기화 리마인더 주입. jq 불필요 (순수 bash + grep/sed)
- `CLAUDE.md` 수정
  - R2 확장: TASKS.md `IN_PROGRESS` + sessions/ 최신 + FEEDBACK OPEN + REQUESTS OPEN 4개 자동 훑기
  - R4에 `/checkpoint` 커맨드 명시
  - R8 신규: 본인 요청은 맥락 중심 기록
  - R9 신규: 훅 리마인더는 신호일 뿐 맹목 추종 금지
  - 파일 구조 개요에 REQUESTS/hooks/checkpoint 반영
- `.claude/commands/commit.md` 수정 — REQ-xxx 처리 추가 (단계 4·5·6)
- `docs/tracking/README.md` 수정 — 파일 테이블 5행, ID 체계에 REQ- 추가, 워크플로우 Mermaid에 REQUESTS/checkpoint 반영
- `docs/README.md` 수정 — tracking 섹션에 REQUESTS.md + sessions/ 링크 추가

**영향 범위**: 개발 워크플로우 자동화만 (코드 변경 없음)

---

## 2026-04-13 — 프로젝트 초기 셋업 + 로직 흐름 문서화

**유형**: chore + docs
**커밋**: `0000000` (초기 커밋)
**관련 TASK**: T-001
**변경 사항**:
- git 저장소 초기화 및 원격 연결 (github.com/uuuuj/a2z)
- 기존 원격 `HYI` 브랜치를 `X_HYI`로 아카이브
- 현재 로컬 상태를 새 `HYI` 브랜치로 업로드 (초기 커밋 97개 파일)
- `docs/` 로직 흐름 문서 72개 작성
  - 카테고리 8개 (bom/clash/dimensions/drawing2d/drawing-sheets/global-views/mfg-drawing/attribute)
  - 핸들러 문서 48개 (버튼/이벤트 단위 Step-by-step 흐름)
  - 코드 레퍼런스 9개 (Form1.*.cs + Models.cs)
  - 최상위 가이드 5개 (README/용어집/파이프라인/템플릿/작성가이드)
- `.gitignore` 보강 (VS/.NET/NuGet/Claude Code 로컬 설정 등)
- `CLAUDE.md` — Claude Code 작업 규칙 R1~R7
- `docs/tracking/` — FEEDBACK/TASKS/CHANGELOG/sessions 4축 구조
- `.claude/commands/commit.md` — `/commit` 슬래시 커맨드 (docs 동기화 + CHANGELOG/TASKS 갱신 + 커밋)

**영향 범위**: 전체 저장소 구조 (코드 변경 없음)
