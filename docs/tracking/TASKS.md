# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

### T-036 — 가공도 시트: 선택상태 해제 + ISO 뷰 느낌 해결
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 재확인 대기)
- **관련**: — (사용자 피드백)
- **경로 전개**:
  1. 1차 진입: 선택상태 해제(DESELECT_ALL) + DiagLog 추가 (커밋 `230e45f`)
  2. 1차 해석 시도: "Z 최장축인데 세로로 배치" → L215 `use1803d && longestAxis!="Z"` 가드 추가 (커밋 `537f07c`)
  3. **사용자 재보고 (2026-04-23)**: "45도 대각 ISO 뷰로 보게 된다" → Z 방향이 아닌 **카메라 방향 자체가 ISO로 잔존**하는 증상
  4. **원인 재확정**: [LvDrawingSheet_SelectedIndexChanged](../../A2Z/Form1.DrawingSheets.cs) 공통부 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 글로벌 ISO) 유지한 채 이동 → `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 덮어쓰지 못함
  5. 수정 (커밋 `b0f8802`): 가공도 시트(-3) 분기 앞에서 `FlyToObject3d` 스킵 + L215 180° 스킵 가드 **원복** (수직 뒤집기 의도 복원)
- **세부 (완료)**:
  - [x] `ExecuteMfgDrawing` 진입부 `Object3D.Select(DESELECT_ALL)` 추가
  - [x] 회전 진단 `DiagLog T-036 MfgDrawing bom=... sizeXYZ=... longestAxis=... isPadOrPlate=... viewDir=... use180=... useMinus=... Z90Applied=... R180Applied=...`
  - [x] 1차 해석 L215 가드 → 원복 (ISO 원인 아님)
  - [x] `LvDrawingSheet_SelectedIndexChanged`에서 가공도 분기 시 `FlyToObject3d` 스킵
  - [x] `use1803d` 바깥 스코프 승격 (DiagLog 가시성 — 유지)
  - [x] docs/features/drawing-sheets/lv-sheet-selected.md / mfg-drawing.md 갱신
  - [x] MSBuild Debug 통과
  - [x] **후속 (2026-04-23)**: 사용자 "카메라 재조정 중 가로→세로 깜빡" 관찰 → `ExecuteMfgDrawing` 전체를 `BeginUpdate/EndUpdate`로 감싸 중간 상태 노출 차단 + Z 최장축 90° 회전 직후 누락됐던 `FitToView` 추가
  - [x] **재수정 (2026-04-23)**: 사용자 DiagLog 공유 → "누르는 순간 가로 → 0.5초 뒤 FitToView로 세로" 확정 → **직전 커밋의 FitToView가 바로 원인**. 제거. 원본 주석 경고 "LockZAxis false 유지 — true로 복원하면 렌더링 엔진이 회전을 리셋"이 FitToView에도 동일 적용
  - [x] **3차 수정 (2026-04-23, sdk-verifier 기반)**: 내부 FitToView 제거만으론 세로 복귀 여전. `LockZAxis`는 키보드용이라 무관 확정. SDK 정공법 `GetCameraData()` + `SetCameraData(data, false)` 스냅샷 복원 패턴 도입. Form1.cs에 `_mfgDrawingCameraSnapshot` 필드 추가, ExecuteMfgDrawing Z 90° 직후 `GetCameraData()` 저장, `LvDrawingSheet_SelectedIndexChanged` 말미에 가공도(-3) 확인 후 `SetCameraData(snapshot, false)` 복원
  - [ ] 사용자 실기 재재재확인 — Z 최장축 부재가 0.5초 뒤에도 가로 유지되는지
- **영향 파일**: A2Z/Form1.MfgDrawing.cs, A2Z/Form1.DrawingSheets.cs, docs/features/mfg-drawing/mfg-drawing.md, docs/features/drawing-sheets/lv-sheet-selected.md

### T-035 — 글로벌 ISO/X/Y/Z 뷰 버튼 클릭 시 부재 선택상태 해제
- **생성일**: 2026-04-22
- **상태**: TODO
- **관련**: — (사용자 피드백)
- **배경**: T-022로 시트/BOM 행 선택 시 빨간 하이라이트 적용. 글로벌 뷰 버튼 누르면 이 상태 잔존. 글로벌 뷰는 "전체 관찰" 모드이므로 선택 없는 상태 기대
- **세부**:
  - [x] `ApplyFullModelView`·`ApplySelectedNodesView` 시작부에 `Object3D.Select(DESELECT_ALL)` 추가
  - [x] `ApplyDrawingSheetView`는 T-022 기준부재 하이라이트 유지 (건드리지 않음)
  - [x] docs/features/global-views/global-iso.md 상태 변화·이력 갱신
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인
- **영향 파일**: A2Z/Form1.GlobalViews.cs

### T-034 — 글로벌 ISO/X/Y/Z 뷰 버튼: 은선(DASH_LINE) → 실선(SMOOTH)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백 "글로벌 뷰에서도 은선 처리되는 거 같아 잘 보이게")
- **해당 위치**: [Form1.GlobalViews.cs L100](../../A2Z/Form1.GlobalViews.cs) `ApplySelectedNodesView` + L150 `ApplyFullModelView` 두 곳의 `SetRenderMode(DASH_LINE)`
- **세부**:
  - [x] 두 호출 `RenderModes.SMOOTH`로 교체
  - [x] docs/features/global-views/global-iso.md 상태 변화·이력 갱신
  - [x] MSBuild Debug 통과
  - [x] **후속 (2026-04-23)**: 사용자 실기 "BOM 테이블 선택 → 글로벌 ISO" 시 은선 복귀 확인. `ApplyDrawingSheetView`(Form1.DrawingSheets.cs L702 ISO / L735 X/Y/Z) DASH_LINE → SMOOTH 교체. L1433(2D 캡처용)은 유지
  - [ ] 사용자 실기 확인 (후속 패치 적용 후)
- **영향 파일**: A2Z/Form1.GlobalViews.cs, A2Z/Form1.DrawingSheets.cs (ApplyDrawingSheetView 내부 2곳)

### T-033 — 오버레이 해제 타이밍 (MessageBox 전에 해제)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백 "자동 처리 완료 팝업 후에도 치수 계산 중 창이 2초 더 떠있음")
- **배경**: 현재 순서 `Osnap → 치수 → 요약 MessageBox → GenerateDrawingSheets → finally HideBusyOverlay` → 팝업 뜰 때 오버레이 잔존, 팝업 닫힌 후 시트 생성까지 오버레이 지속
- **세부**:
  - [x] 순서 재배치: `Osnap → 치수 → GenerateDrawingSheets → HideBusyOverlay → MessageBox`
  - [x] finally의 HideBusyOverlay는 예외 안전망으로 유지 (중복 호출 OK)
  - [x] docs/features/bom/main-dimension.md 변경 이력 갱신
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 (팝업 뜰 때 오버레이 없는 상태인지)
- **영향 파일**: A2Z/Form1.BOM.cs (CompleteMainDimensionPostClash)

### T-004 — ALL 출력 후 시트별 도면 즉시 미리보기
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-001
- **세부**:
  - [ ] ALL 일괄 출력이 만든 PDF 파일 경로를 시트별로 매핑(DrawingSheetData에 저장 or 별도 Dict)
  - [ ] `LvDrawingSheet_SelectedIndexChanged`에서 해당 시트의 저장된 PDF가 있으면 2D 뷰에 로드·표시
  - [ ] PDF가 없는 시트는 기존 동작(X-Ray + 치수) 유지
  - [ ] docs/features/drawing-sheets/lv-sheet-selected.md + export-all-pdf.md 동기화
  - [ ] 사용자-매뉴얼/5.목록 조작/시트 선택 시 화면 전환.md + 4.도면정보 탭/ALL 일괄 출력.md 동기화
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (LvDrawingSheet_SelectedIndexChanged, btnExportAllPDF_Click)
  - `A2Z/Models.cs` (DrawingSheetData에 PdfPath 필드 추가 가능)

### T-005 — 치수 배치를 Osnap 외곽 방향으로
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-002
- **세부**:
  - [ ] 각 체인 치수의 "바깥 방향" 판정 로직 구현 (Osnap 무게중심 반대 방향)
  - [ ] `ShowAllDimensions` 및 `btnDimensionShowSelected_Click`의 축별 오프셋을 외곽 방향으로 변경
  - [ ] 기존 축별 오프셋(50.0f 고정)을 부재 BBox 근처로 조정
  - [ ] docs/features/dimensions/show-selected.md + main-dimension.md 동기화
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (btnDimensionShowSelected_Click L17, ShowAllDimensions)
  - `A2Z/Form1.BOM.cs` (btnMainDimension_Click 내부 AddChainDimensionByAxis 영향 가능)

### T-012 — 엑셀 템플릿 하이브리드 실험 (PoC)
- **생성일**: 2026-04-20
- **상태**: TODO
- **관련**: REQ-002
- **배경**: SDK가 `ImportExcel`, `ImportExcelWithData`, `Draw2DViewTemplate(path, x, y, w, h)`, `RenderTemplateOnGridStructure`를 제공 ([VIZCore3D.NET.xml:31152, 31099](../../VIZCore3D.NET.xml)). 담당자가 엑셀로 양식을 관리할 수 있는지 **실험만** (프로덕션 전환은 별개). 과거 Phase 18(`790a02a`)에서 BOM 동적 행수 문제로 수동 구성으로 되돌린 이력 있음 — 하이브리드로 재도전
- **세부**:
  - [ ] 시나리오 2 (하이브리드 추천안): tableInfo만 엑셀 외부화 PoC (Aspose.Cells로 엑셀 파싱 → TemplateTableData 구성 → `RenderTemplateOnGridStructure(table, 2, 3)`)
  - [ ] 시나리오 3 (JSON 경유): `Draw2DViewTemplate(path, x, y, w, h)`로 우측 영역만 배치 실험
  - [ ] `ImportExcel(path)` + 기존 GridStructure 공존 가능성 확인 (시나리오 1 평가)
  - [ ] BOM 헤더/열너비/스타일 엑셀 외부화 가능성 평가 (데이터 행은 런타임 채움)
  - [ ] 결과 리포트: `docs/technical-notes/excel-template-experiment.md` 신설
- **영향 파일**: 실험용 별도 메서드만 (기존 GenerateSheetDrawing2D 변경 없음)

---

## IN_PROGRESS

### T-032 — 치수 계산 성능 최적화 (Osnap 맵 재사용)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (A 옵션 구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백 "치수 계산 중 창이 오래 떠있음")
- **원인 확정**: `CompleteMainDimensionPostClash`에서 Osnap 수집이 이중 호출
  1. `CollectAllOsnap()` — 전체 visible 부재 `GetOsnapPoint(idx)`
  2. `ComputeViewDimensionsForMembers` 내부 `nodeOsnapMap` 구축 시 **다시** `GetOsnapPoint(idx)`
  - 같은 SDK 왕복을 부재 수만큼 반복 → 전체 시간의 절반 가까이가 이 중복
- **선택한 방식**: **옵션 A** — `CollectAllOsnap`이 수집하는 동안 `nodeOsnapMap`도 같이 구축, `ComputeViewDimensionsForMembers`가 재사용
- **구현**:
  - [x] Form1.cs에 `_lastCollectedNodeOsnapMap` 필드 추가 (`Dictionary<int, List<(Vertex3D, string)>>`)
  - [x] `CollectAllOsnap` 내부에서 각 부재의 Osnap을 플랫 리스트(`osnapPointsWithNames`)에 추가하면서 동시에 부재별 맵에도 적재
  - [x] `ComputeViewDimensionsForMembers`에 `preBuiltNodeOsnapMap` optional 파라미터 추가 — 있으면 `memberIndices` 부분만 필터해 재사용, 없으면 기존대로 내부에서 `GetOsnapPoint` 호출해 구축 (시트 선택 자동 경로용)
  - [x] `CompleteMainDimensionPostClash`가 `_lastCollectedNodeOsnapMap`을 전달 → 치수추출 버튼 경로의 `GetOsnapPoint` 중복 호출 제거
  - [x] `Stopwatch`로 `ComputeViewDimensionsForMembers` 소요 시간 측정, `DiagLog T-032 치수 계산: visibleMembers=N osnapMapNodes=K chain=M ComputeViewDimensionsForMembers=Xms` 기록
  - [x] docs `main-dimension.md` 단계 12·13 재기술 + 변경 이력
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 — DiagLog의 `ComputeViewDimensionsForMembers=Xms` 수치 개선 비교
- **후속 검토 여지**:
  - 오버레이 메시지 세분화 (예: "Osnap 수집 중 {n/N}") — 체감 시간 개선용
  - `GetOsnapPoint` 자체가 병목이면 Part 단위 배치 API 검토
- **영향 파일**: A2Z/Form1.cs (+1 필드), A2Z/Form1.BOM.cs (CollectAllOsnap 루프, CompleteMainDimensionPostClash), A2Z/Form1.Dimensions.cs (ComputeViewDimensionsForMembers 파라미터 추가)
- **연관**: T-018 (오버레이 UX), T-028 (치수 엔진 통합)

### T-030 — 시트 선택 시 3D 뷰 치수 렌더링 제거 (T-029 정책 확장)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백 "시트 눌렀을 때 치수가 나오는데 왜 나오는지 모르겠음")
- **결정**: (a) T-029 정책 확장 — 일반 시트 분기에서 `ShowAllDimensions()` 제거, 3D 뷰는 깨끗하게. 글로벌 뷰 버튼 눌러야 치수 등장. 설치도(-2)는 `ExtractInstallationDimensions`가 이미 3D 미렌더라 그대로 유지
- **구현**:
  - [x] `Form1.DrawingSheets.cs` `LvDrawingSheet_SelectedIndexChanged` 일반 시트 분기에서 `ShowAllDimensions()` 호출 제거
  - [x] `Review.Measure.Clear()` + `ShapeDrawing.Clear()` 추가로 "깨끗한 상태" 마감
  - [x] `chainDimensionList`·`lvDimension`은 계속 채움 (2D 출력·글로벌 뷰 버튼에서 자동 활용)
  - [x] `DiagLog T-030 시트 선택 자동 치수: ... (3D 미렌더)` 기록
  - [x] docs/features/drawing-sheets/lv-sheet-selected.md 분기 A 재기술·변경 이력
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 (시트 선택 시 3D 뷰 깨끗, 글로벌 X/Y/Z 누르면 해당 뷰 치수 등장)
- **영향 파일**: A2Z/Form1.DrawingSheets.cs (LvDrawingSheet_SelectedIndexChanged 일반 시트 분기 +4줄), docs/features/drawing-sheets/lv-sheet-selected.md

### T-031 — 가공도 시트 선택 시 은선(DASH_LINE) 처리 비활성화
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 직접 지시)
- **구현**:
  - [x] `ExecuteMfgDrawing` L142: `SetRenderMode(DASH_LINE)` → `SetRenderMode(SMOOTH)` 실선 모드로 교체
  - [x] 2D 캡처·PDF 출력 내부(L820, L1582)는 DASH_LINE 유지 (2D 도면의 내부 상세 은선용)
  - [x] docs/features/mfg-drawing/mfg-drawing.md 상태 변화 + 변경 이력 갱신
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 (가공도 시트 선택 시 실선으로 나오는지)
- **영향 파일**: A2Z/Form1.MfgDrawing.cs (1줄), docs/features/mfg-drawing/mfg-drawing.md

### T-029 — 치수추출 버튼은 3D 뷰 치수 렌더링하지 않음 (글로벌 뷰 버튼으로 지연)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 직접 지시)
- **배경**: 치수추출 버튼 누르면 3D 뷰에 6조합 치수(T-028)가 모두 렌더링되어 과밀. 사용자 의견: "글로벌 뷰 버튼 누르면 보여주는 것으로 충분"
- **구현**:
  - [x] `CompleteMainDimensionPostClash`에서 `ShowAllDimensions()` 호출 제거
  - [x] 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()` 추가 — 이전 렌더 잔존 제거로 "깨끗한 상태" 마감
  - [x] `chainDimensionList`·`lvDimension`은 그대로 채움 → 글로벌 뷰 버튼·2D 출력에서 자동 활용
  - [x] MSBuild Debug 통과
  - [x] docs main-dimension.md 단계 14.5·상태 변화·변경 이력 갱신
  - [ ] 사용자 실기 확인 (치수추출 후 3D 뷰 깨끗, 글로벌 X/Y/Z 누르면 치수 등장)
- **영향 파일**: `A2Z/Form1.BOM.cs` (CompleteMainDimensionPostClash 4줄), `docs/features/bom/main-dimension.md`

### T-028 — 치수 로직 통합 (2D 출력 기준 + 설치도 BBox 분기)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 중, 나중에 A로 전환 여지 열어둠)
- **관련**: — (사용자 직접 지시)
- **배경**: 4개 경로(치수추출 / 글로벌 X/Y/Z / 2D 출력 / 시트 선택 자동)의 치수 로직이 각기 다름. 사용자 요구: "2D 출력에서 사용하는 Osnap·로직을 기준으로 모두 통일"
- **확정 사항**:
  1. **엔진 기준**: `ShowAllDimensions(viewDirection)` 분기 ② = `nodeOsnapMap` + `FilterOsnapForDimAxis` + `AddChainDimensionByAxis(axis, viewDirection)`
  2. **중복 제거**: 같은 `(Axis, StartPoint, EndPoint)` 3자리 반올림 기준 병합. ViewDirection은 콤마 구분으로 누적 (예: "X,Y")
  3. **설치도(-2) 분기 유지 (옵션 B)**: 설치도 시트에서만 `ExtractInstallationDimensions`(BBox) 유지, 나머지 시트는 Osnap 엔진. 추후 A(완전 폐기)로 전환 가능
  4. **T-027 `FilterOsnapByViewDimensionUsage` 폐기**: 새 엔진이 `FilterOsnapForDimAxis`로 일원화
- **공용 헬퍼** (신설):
  - `ComputeViewDimensionsForMembers(memberIndices, viewDirection, tolerance) → List<ChainDimensionData>`
  - 내부: `nodeOsnapMap` 구축 → (뷰×축 조합 루프) → `FilterOsnapForDimAxis` → `MergeCoordinates` → `AddChainDimensionByAxis(axis, view)` → 중복 제거
  - `viewDirection == null` → 3뷰 × 2축 = 6조합 (치수추출·시트 선택용)
  - `viewDirection == "X"` → X뷰 2축만 (글로벌 버튼·2D 출력용)
- **데이터 변경**: `ChainDimensionData`에 `ViewDirection` 필드 추가 (어느 뷰에서 보이는 치수인지 "X,Y,Z" 콤마 구분)
- **4개 경로 재배선**:
  | 경로 | 변경 전 | 변경 후 |
  |---|---|---|
  | 치수추출 (`CompleteMainDimensionPostClash`) | `FilterOsnapByViewDimensionUsage` + `AddChainDimensionByAxis × 3` | `ComputeViewDimensionsForMembers(visibleMembers, null)` |
  | 글로벌 X/Y/Z | `ShowAllDimensions(viewDirection)` 내부 분기 ①②③ 재계산 | chainDimensionList에서 `ViewDirection.Contains(viewDirection)` 필터링 표시만 |
  | 2D 출력 (`GenerateSheetDrawing2D`) | `ShowAllDimensions(viewDirection, true)` 재계산 | 단순화된 ShowAllDimensions 재사용 (chainDimensionList 필터링) |
  | 시트 선택 자동 (`LvDrawingSheet_SelectedIndexChanged`) | 가공도(-3) 제외 모든 시트 `ExtractInstallationDimensions`(BBox) | -3 `ExecuteMfgDrawing` / **-2 `ExtractInstallationDimensions`(BBox 유지)** / 그 외 `chainDimensionList = ComputeViewDimensionsForMembers(sheet.MemberIndices, null)` |
- **세부**:
  - [x] `Models.cs`에 `ChainDimensionData.ViewDirection` 필드 추가
  - [x] `Form1.Dimensions.cs` `AddChainDimensionByAxis`에서 `ViewDirection = viewDirection` 기록 추가 (체인·전체 치수 두 곳)
  - [x] `Form1.Dimensions.cs` `ComputeViewDimensionsForMembers` 신설 (nodeOsnapMap 구축 + 뷰×축 루프 + 중복 제거 + ViewDirection 콤마 병합)
  - [x] `Form1.Dimensions.cs` `ShowAllDimensions` 단순화 — 내부 분기 ①②③ 제거, chainDimensionList 필터링 + 스마트 필터링만
  - [x] `Form1.Dimensions.cs` `FilterOsnapByViewDimensionUsage`(T-027) 제거 + placeholder 주석 유지
  - [x] `Form1.Dimensions.cs` `isInstallationMode`·`useDirectChain` 변수 제거, 오프셋 단일화
  - [x] `Form1.BOM.cs` `CompleteMainDimensionPostClash` 간소화 — `ComputeViewDimensionsForMembers` 호출
  - [x] `Form1.DrawingSheets.cs` `LvDrawingSheet_SelectedIndexChanged` 분기 재작성 (가공도-3 / 설치도-2 / 일반)
  - [x] MSBuild Debug 통과
  - [x] docs 2종 갱신: `main-dimension.md` 파이프라인 재기술, `lv-sheet-selected.md` 분기 A 재작성
  - [ ] 사용자 실기 확인 (4경로 일관성, 중복 제거 효과, 설치도 BBox 유지 확인)
- **영향 파일**:
  - `A2Z/Models.cs` (+1 필드)
  - `A2Z/Form1.Dimensions.cs` (공용 헬퍼 +80줄, ShowAllDimensions -70줄, FilterOsnapByViewDimensionUsage -45줄)
  - `A2Z/Form1.BOM.cs` (CompleteMainDimensionPostClash 치수 블록 -15줄)
  - `A2Z/Form1.DrawingSheets.cs` (LvDrawingSheet_SelectedIndexChanged 분기 +10줄)
  - docs: `main-dimension.md`, `generate-sheets.md`, `lv-sheet-selected.md`

### T-018 — 장시간 작업 진행 UX 표시 (치수 추출 5초 공백 개선)
- **생성일**: 2026-04-21
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (1차 구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백)
- **선택 옵션**: **(b) 오버레이 라벨** — 3D 뷰어 중앙에 "처리 중..." 반투명 라벨 (위험 최소 + 시각 효과 충분)
- **세부** (1차 완료):
  - [x] 공통 헬퍼 `ShowBusyOverlay(msg)` / `HideBusyOverlay()` 신설 → [Form1.cs](../../A2Z/Form1.cs) L183~L222
  - [x] `busyOverlay` 필드(Label) 추가 — 최초 호출 시 지연 생성, panelViewer 중앙 자동 배치
  - [x] `btnMainDimension_Click` try/finally 구조 + 각 단계 진입 시 메시지 갱신 ("치수 추출 중..." → "Osnap 수집 중..." → "치수 계산 중..." → "간섭검사 실행 중...")
  - [x] `finally`에서 `HideBusyOverlay()` 호출 — 정상·예외 모두 해제
  - [x] MSBuild Debug 통과 (경고 0)
  - [x] docs/features/bom/main-dimension.md 단계표·변경 이력 갱신
  - [ ] 사용자 실기 확인 (오버레이 보이고 처리 완료 시 사라지는지)
- **2차 확장 (검토)**:
  - [ ] 다른 장시간 작업 적용 여부 — 2D 도면 생성(`GenerateSheetDrawing2D`), 가공도(`ExecuteMfgDrawing`), PDF 배치(`btnExportAllPDF`), 시트 생성(`GenerateDrawingSheets`). 1차 UX 반응 보고 결정
- **영향 파일**:
  - `A2Z/Form1.cs` (busyOverlay 필드 + 헬퍼 2개, +40줄)
  - `A2Z/Form1.BOM.cs` (btnMainDimension_Click try/finally 구조 + 오버레이 4곳 호출)
  - `docs/features/bom/main-dimension.md`
- **우선순위**: MEDIUM

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정 + 뷰 내부 clip (T-007 흡수)
- **생성일**: 2026-04-15
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS (1차 레이아웃 구현 완료, **치수선 clip·모델 최대화 추가 실험 필요**)
- **관련**: FB-003, FB-004 (T-007 내용 흡수 — 2026-04-22 사용자 지시)
- **확정 스펙** (옵션 A — 1차):
  - A4 가로 297×210 / 마진 10 / 그리드 2×3 (셀 ≈ 92.3×95 mm) — 현재 유지
  - 뷰 4개: (1,1)ISO / (1,2)Z / (2,1)Y / (2,2)X — 현재 유지
  - **BOM → (1,3) 셀 이관** (`RenderTemplateOnGridStructure(table1, 1, 3)`)
  - **tableInfo → (2,3) 셀 이관** (`RenderTemplateOnGridStructure(tableInfo, 2, 3)`)
  - BOM 열 너비 합 82 → **92 mm로 조정** (ITEM 28→38, 그 외 유지)
  - BOM 최대 데이터 행 **14행**, 초과 시 마지막 행에 "…" + "+N건 생략" 표시 (옵션 2-a)
  - Anchor/X/Y 절대좌표 제거 → 셀 정렬(`SetGridCell*Alignment`)로 대체
- **추가 요구사항 (2026-04-22)**:
  - **치수선 clip 필수** — 뷰 셀 안에서 모델뿐 아니라 **치수선도 셀 경계를 벗어나지 않고** 그리드 내부에서만 표현되어야 함. 현재 치수선이 인접 셀로 늘어남
  - **뷰 내부 모델 최대화** (T-007 흡수) — `RenderSheetViewForDrawing`의 `targetHeight=40f` 하드코드를 셀 크기 기반 동적 계산으로 교체
  - **풍선 예약 영역 확보** (T-007 흡수) — 상단/측면에 일정 여백 확보해 번호 풍선이 겹치지 않게
  - **ISO/X/Y/Z 라벨 하단 고정** (T-007 흡수) — 셀 하단 같은 Y 좌표에 고정 배치
  - **실험 심화** — 1차 구현이 레이아웃만 고정시켰을 뿐 위 4개 요구를 충족 못 함. SDK의 `SetGridCellClipping` 류 API, `Create2DViewObject` 계열 파라미터, 그리드 셀 내부 렌더링 경계 제어 옵션 전수 조사 필요
- **세부** (1차 완료):
  - [x] Form1.DrawingSheets.cs L1020~1080 수정 — bInfo 절대좌표 제거, BOM/tableInfo `RenderTemplateOnGridStructure` 이관
  - [x] BOM `BOM_MAX_DATA_ROWS = 14` 상수 + "…+N건 생략" 행 렌더링
  - [x] BOM 열 너비 2차 축소: ITEM 28→38→17, MATERIAL/SIZE 8→12→11 (합 82→92→81→**77mm**)
  - [x] tableInfo 2차 축소: 60→57→47, 35→35→30 (합 95→92→81→**77mm**)
  - [x] 셀 정렬: BOM (1,3) Top/Center, tableInfo (2,3) Bottom/Center
  - [x] docs/features/drawing-sheets/generate-sheet-2d.md 1차 갱신 (단계표 7~9 추가, 분기 C 추가, 변경 이력 3건)
- **세부** (2차 — 추가 실험):
  - [ ] SDK 조사: 뷰 셀 내부 clip / 치수선 경계 제어 API (`sdk-verifier` 서브에이전트)
  - [ ] 치수선 렌더링 경로 추적 — 현재 치수선이 어디서 그려지며 왜 셀을 벗어나는지
  - [ ] `targetHeight=40f` 하드코드 → 셀 크기 기반 동적 계산
  - [ ] 풍선 예약 영역 설계 + 적용
  - [ ] ISO/X/Y/Z 라벨 위치 고정 (하단)
  - [ ] 빌드 + 실기 테스트 (치수선 셀 이탈 여부, 모델 크기, 풍선 겹침, 라벨 위치 모두 확인)
  - [ ] docs/features/drawing-sheets/generate-sheet-2d.md 2차 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D, RenderSheetViewForDrawing, CreateIsoBalloonNotes)
  - `docs/features/drawing-sheets/generate-sheet-2d.md`
- **참고**: T-007은 본 항목에 흡수되어 제거됨 (2026-04-22)

---

## BLOCKED

### T-013 — Sheet2+ ISO 뷰 배경·선택 부재 위치 정합
- **생성일**: 2026-04-20
- **착수일**: 2026-04-21
- **차단일**: 2026-04-22
- **상태**: BLOCKED (옵션 A·B·B2 모두 실패, 새 접근 필요)
- **관련**: — (사용자 피드백)
- **배경**: Sheet2 이상에서 ISO 뷰는 "전체 모델 점선(bgObj) + 선택 부재 실선(obj)"으로 그려지는데, 선택 부재가 **원본 위치가 아니라 전체 모델의 중심**으로 이동됨
- **원인 분석 (기존)**: 두 객체 모두 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin`로 **캔버스 원점에** 생성됨 → `GetObjectCenter`가 둘 다 (0,0) 근처 반환 → `(objCX0 - bgCX0) ≈ 0` → 위치 보정 공식이 무력화되어 obj가 bg 중심으로 이동
- **시도한 접근** (모두 실패):
  - [x] 옵션 A — SDK 자동 매핑 기대: objId의 `RescaleObject`/`MoveObject` 제거 → objId가 원점에 매우 작게 남음
  - [x] 옵션 B — `WorldToScreen` + `bgFinalScale` 단일 스케일: 오차 발생 (7.3mm 정답 대비 5.9mm 계산)
  - [x] 옵션 B2 — bg BBox 8꼭지점 투영 → ratio 계산: 이동량 자체는 계산됐지만 시각적 변화 없음. 사용자 실측 2026-04-22 실패 확정
- **재개 시 고려할 방향**:
  - `WorldToScreen` 반환 단위 재검증 (캔버스 / 픽셀 / 월드 어느 기준인지)
  - SDK의 다른 API 탐색 — `Create2DViewObject*` 계열에 "원본 월드 좌표 유지 모드" 파라미터 존재 여부
  - 근본 설계 전환: Sheet2+ 렌더링에서 bgObj+obj 분리 구조 자체를 폐기하고 **단일 객체 + 컬러/라인 스타일 분기**로 처리
- **진단 로그**: `OPT-B` / `OPT-B2` 라벨로 3D/화면/이동량 실측 출력 중 (Form1.DrawingSheets.cs `RenderSheetViewForDrawing` L1327~)
- **영향 파일**: A2Z/Form1.DrawingSheets.cs, docs/features/drawing-sheets/generate-sheet-2d.md

### T-016 — 치수 추출 3회 이상 시 반복 누적 버그
- **생성일**: 2026-04-20
- **상태**: BLOCKED (재현 조건 수집 중)
- **관련**: — (사용자 피드백)
- **현황**: 사용자 재현 시도 중 다시 정상 동작. **간헐 버그(intermittent)**로 분류
- **이번 세션 진행**:
  - [x] 코드 분석으로 영향 가능 영역 좁힘 (4개 메서드)
  - [x] **로그 인프라 추가** — 다음 발생 시 즉시 진단 가능
    - `btnMainDimension_Click` ENTER/EXIT (xray·chain·osnap·bom 카운트)
    - `btnExtractDimension_Click` ENTER/EXIT
    - `LvDrawingSheet_SelectedIndexChanged` ENTER/SKIP/EXIT/FAIL (sheet#, prevXray, prevChain)
    - `ExtractInstallationDimensions` ENTER/EXIT (members, chain)
    - `LvDrawingSheet_SelectedIndexChanged`의 silent catch에 stack trace 추가
  - [ ] **다음 재현 시 사용자가 Visual Studio 출력창 로그 공유** → 즉시 진단
- **의심 가설 4개** (다음 재현 시 우선 검증):
  1. **Silent catch 무력화** — `LvDrawingSheet_SelectedIndexChanged` (Form1.DrawingSheets.cs:487~) 의 try-catch가 SDK 예외를 삼키면서 `xraySelectedNodeIndices = new List<int>(sheet.MemberIndices)` (L460) 또는 `ExtractInstallationDimensions` (L484)이 도달 못해 이전 값 유지
  2. **WinForms 이벤트 중복 발생** — `ListView.SelectedIndexChanged`는 선택 해제·선택 활성화 시 각각 발생. 3회째 두 이벤트가 race로 꼬여 새 시트의 갱신이 무효화될 가능성
  3. **xraySelectedNodeIndices 비동기 race** — `vizcore3d.BeginUpdate/EndUpdate` 사이에서 SDK 호출 도중 또 다른 핸들러가 같은 필드 수정
  4. **chainDimensionList 갱신 실패** — `ExtractInstallationDimensions` 진입 자체가 누락되거나 (L209 `if (members.Count == 0) return;`) early return으로 Clear만 되고 새로 채워지지 않음 — 그러나 Clear는 됐으므로 "이전 치수 반복"과는 직접 매치 X
- **재현 시 사용자에게 요청할 정보**:
  - 정확한 UI 조작 순서 (시트 선택? 부재 클릭? 어떤 버튼?)
  - Visual Studio 출력창 로그 (`[T-016 진단 로그]` prefix로 필터)
  - lvDimension(좌측 치수 목록)의 행 수 변화
- **영향 파일** (로그 추가):
  - A2Z/Form1.BOM.cs (btnMainDimension_Click)
  - A2Z/Form1.Dimensions.cs (btnExtractDimension_Click)
  - A2Z/Form1.DrawingSheets.cs (LvDrawingSheet_SelectedIndexChanged)
  - A2Z/Form1.GlobalViews.cs (ExtractInstallationDimensions)

---

## DONE (최근 20개)

### T-026 — 치수추출 진입 시 이전 xray 선택 잔존 클리어
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 피드백 + 로그 근거)
- **커밋**: `7614417`
- **요약**:
  - 증상: 부재 1개 띄우고 치수추출 → 전체 띄우고 다시 치수추출 → 1개 기준 결과 반복
  - 원인: `LvDrawingSheet_SelectedIndexChanged`가 설정한 `xraySelectedNodeIndices` 값이 잔존 → `CollectBOMData` L591의 X-Ray 우선 필터에 걸려 "그 부재만" 수집
  - 수정: `btnMainDimension_Click` 진입부에 `xraySelectedNodeIndices.Clear()` 추가 — "치수추출 버튼은 항상 현재 visible 기준"

### T-025 — 치수추출 직후 Sheet 1 기준 BOM 테이블 자동 출력
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 피드백)
- **커밋**: `7614417`
- **요약**:
  - 치수추출 완료 후 `lvDrawingBOMInfo`(도면정보 탭 BOM 테이블)가 빈 상태로 남던 문제
  - `GenerateDrawingSheets()` ListView 갱신 직전에 `CollectBOMInfo(false, drawingSheetList[0])` 호출 추가 → Sheet 1(전체) 기준 BOM 테이블 자동 채움
  - visibility·카메라 무영향 (try/catch로 SDK 예외 보호)

### T-023 — 치수추출 사전조건 (연결성 1덩어리 판정)
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시, 3차 재설계 후 확정)
- **커밋**: `1620289`(v1 visible/selected==1, 의도 불일치로 원복) → `2a216b5`(v2 STRU UDA 주석 블록, 보류 후 원복) → **`cc72e94`**(v3 Clash 기반 연결성 — 최종 채택)
- **최종 판정**: Clash 인접 그래프 기준 연결 성분 == 1일 때만 치수추출 허용. 떨어진 부재가 있으면 차단
- **요약**:
  - `IsSingleConnectedComponent(out int)` 헬퍼 신설 (Part→Body 역매핑 양방향 인접 그래프 + BFS + early exit)
  - `Clash_OnClashTestFinishedEvent`에서 clashList 수집 후 판정, n≠1이면 차단 MessageBox
  - 파이프라인 재배치: `btnMainDimension_Click`은 BOM+Clash 시작까지만, Osnap/치수/요약/시트는 `CompleteMainDimensionPostClash`로 분리
  - 단일 부재(clashStarted=false)는 판정 스킵하고 Post 메서드 직접 호출 (T-024 fallback과 통합)
  - docs 3종(main-dimension.md / clash-finished-event.md / 사용자 매뉴얼 치수 추출.md) 전면 재작성
  - v1/v2 주석 블록·헬퍼 모두 제거 완료

### T-024 — 단일 부재 치수추출 결과가 도면 시트 목록에 반영 안 됨
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시)
- **커밋**: `06a1395` → `cc72e94` (T-023 v3 구현에서 fallback 경로 통합)
- **원인 + 수정**:
  - 원인: `DetectClash`가 targetNodes=1이면 `clashCount=0` → return false → `PerformInterferenceCheck` 미호출 → Clash 이벤트 미발동 → `GenerateDrawingSheets` 미호출
  - 부가: 간섭 없는 다중 부재도 이벤트 내 `if (clashList.Count > 0)` 조건에 걸려 시트 생성 안 되던 숨은 버그 공존
  - 수정 1 (06a1395): `btnMainDimension_Click`이 `DetectClash` 반환값을 받아 false면 시트+요약 직접 수행
  - 수정 2 (06a1395): `Clash_OnClashTestFinishedEvent` 조건부 호출 → 무조건 호출
  - 통합 (cc72e94): 단일 부재 fallback이 `CompleteMainDimensionPostClash(isSingleMember=true, 0)`으로 통일

### T-022 — 시트/BOM 선택 시 3D View 부재 "선택상태" 동기화
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시)
- **커밋**: `ab8313e`
- **SDK**: `vizcore3d.Object3D.Select(List<int>, true, false)` + `Select(DESELECT_ALL)` (sdk-verifier로 VIZCore3D.NET.xml L51882~51946 검증)
- **요약**:
  - `LvDrawingSheet_SelectedIndexChanged`에 기준부재 빨간 하이라이트 (Sheet 1·설치도 제외, 가공도는 MemberIndices[0], 일반은 BaseMemberIndex)
  - `LvDrawingBOMInfo_SelectedIndexChanged`에 단일 부재 하이라이트 + FlyToObject3d
  - 피드백 루프 분석 완료 — `Object3D_OnObject3DSelected`가 속성탭만 갱신하므로 안전. 부수효과로 부재 정보 탭 자동 갱신 (UX 향상)

### T-027 — 치수추출 Osnap 선별 (T-028로 대체됨)
- **완료일**: 2026-04-22 (REPLACED BY T-028)
- **관련**: — (사용자 직접 지시)
- **커밋**: `bb48a16` (신설 → 체인 치수 수 감소) → `375d66f` (T-028 엔진 통합에서 제거)
- **요약**:
  - `FilterOsnapByViewDimensionUsage` 헬퍼로 뷰×축 6조합 필터 endpoint 합집합 구현 → 체인 치수 수 감소
  - 하지만 2D 출력 엔진(`FilterOsnapForDimAxis`)과 로직이 달라 4경로 일관성 문제 발생 → 사용자 재피드백 "같은 Osnap·같은 로직"
  - **T-028**에서 엔진 통합(`ComputeViewDimensionsForMembers`)하며 본 T-027 헬퍼 제거. 당시 기능은 T-028의 6조합 중복 제거에 흡수됨

### T-017 — 라이선스 인증 코드를 Form1.BOM.cs에서 분리
- **완료일**: 2026-04-22 (사용자 실기 테스트 통과)
- **관련**: — (사용자 직접 지시, 코드 정리)
- **커밋**: `d849663`
- **요약**:
  - 옵션 (A) 채택 — `Form1.License.cs` 신규 partial (71줄)로 라이선스 로직 이동
  - `InitializeLicense()` 공용 진입점 — 실패 시 MessageBox + false, 성공 시 30분 갱신 타이머 기동 후 true
  - `Form1.BOM.cs` `Vizcore3d_OnInitializedVIZCore3D` 앞 10줄 → `if (!InitializeLicense()) return;` 한 줄로 축약
  - `Form1.BOM.cs`에서 `StartLicenseRefreshTimer`·`LicenseRefreshTimer_Tick` 제거 (약 -30줄)
  - `Form1.cs`에서 `licenseRefreshTimer` 필드 선언 제거
  - `A2Z.csproj`에 `Form1.License.cs` Compile 항목 추가 (`DependentUpon=Form1.cs`)
  - docs: `form1-bom.md` 라이선스 항목 5곳 정리, `form1-license.md` 신설, `features/bom/vizcore3d-initialized.md` 단계표·에러표·링크·이력 갱신
  - MSBuild Debug 통과, 사용자 실기에서 앱 기동 정상 확인

### T-021 — BOM 정보 행 선택 시 부재 카메라 fit
- **완료일**: 2026-04-22
- **관련**: — (사용자 직접 지시)
- **커밋**: `9b99b8c`
- **요약**:
  - `lvDrawingBOMInfo`(도면정보 탭 BOM 테이블) 행 선택 시 카메라 fit 동작 신설
  - 가시성은 그대로 두고 `vizcore3d.View.FlyToObject3d(new List<int>{bodyIdx}, 1.2f)`로 카메라만 이동 — 현재 시트 맥락 유지
  - No. 컬럼 파싱 → `bomList[No-1].Index` Body 조회 (CollectBOMInfo의 `partIndexToBomNo` 매핑 = `bi+1`과 동일)
  - 요약행(Row 0) · No 파싱 실패 · 범위 초과는 조용히 return
  - 이벤트 등록 위치: [Form1.cs:166](../../A2Z/Form1.cs:166)
  - 새 핸들러: [Form1.DrawingSheets.cs `LvDrawingBOMInfo_SelectedIndexChanged`](../../A2Z/Form1.DrawingSheets.cs)
  - 신규 문서: [lv-bom-info-selected.md (SHT-010)](../features/drawing-sheets/lv-bom-info-selected.md), `_index.md` 등록 추가
  - 사용자 실기 테스트 통과 (2026-04-22)

### T-014 — 도면 시트 목록의 "기준부재/포함부재" 컬럼을 item 번호로 표시
- **완료일**: 2026-04-22
- **관련**: — (사용자 피드백)
- **커밋**: `9b99b8c`
- **요약**:
  - `lvDrawingSheet` 표시 포맷 변경: 부재 이름 대신 **item 번호**(= `bomList` 순서 i+1 = ISO 풍선 번호 = BOM 정보 탭 No.)
    - Sheet 1 → "전체 / 전체"
    - Sheet 2+ → `{기준번호}` / `{포함 번호 오름차순 콤마}` (예: `1 / 1, 3, 5`)
    - 설치도 → "설치도 / {전체 item 번호}"
    - 가공도 → `{MemberIndices[0]의 item 번호}` / 공란
  - 사용자 결정 확정: (1) 시트 생성 로직은 T-015 그대로 유지, 표시만 변경 (2) 접두사 `item` 없이 숫자만 (3) 가공도도 번호로
  - 구현: [Form1.DrawingSheets.cs:215~281](../../A2Z/Form1.DrawingSheets.cs:215) `bomIndexToItemNo` Dictionary + ListView 갱신 블록
  - 빌드 오류 1건 수정: 외부 `int mfgNo=1`(가공도 번호)과 변수명 충돌 → `mfgBomIdx`/`mfgItemNo`로 리네임
  - 문서: `generate-sheets.md` 단계 10·상태 섹션·변경 이력 갱신
  - 사용자 실기 테스트 통과 (2026-04-22)

### T-009 — 초기화 버튼 누락 항목 보강
- **완료일**: 2026-04-22 (사용자 실기 테스트 통과)
- **관련**: T-008 후속 (사용자 피드백)
- **커밋**: `45d17dd` (본체) + `10c7d8c` (후속 — `Clear2DView()` 호출 시점 `Model.Open` 이후로 이동, 4번 번쩍임 해결)
- **요약**:
  - `ResetToInitialState()` 정리 블록에 3줄 추가 — `lvDrawingBOMInfo.Items.Clear()`, `vizcore3d.View.SetRenderMode(RenderModes.SMOOTH)` (DASH_LINE 해제), `Clear2DView()`
  - `Clear2DView()` 호출 시점을 `Model.Open` 성공 이후로 재배치 (SDK가 Open 시 2D 뷰 자동 복원하는 이슈)
  - docs/features/bom/reset-to-initial.md 갱신
  - SDK 참고: `RenderModes.SOLID`는 존재하지 않음 → `SMOOTH` 사용
  - 다른 기기 실기 테스트 통과 (2026-04-22)

### T-015 — Sheet 생성 로직 재설계 (모든 부재가 기준부재)
- **완료일**: 2026-04-21
- **관련**: — (사용자 피드백)
- **커밋**: `9b870a0`
- **요약**:
  - **기존 문제**: `GenerateDrawingSheets`의 `appearedAsIncluded` 스킵 로직 — "다른 시트의 포함부재로 등장한 부재는 기준부재가 될 수 없음". 결과: 1-2-3-4 연쇄 Clash에서 Sheet 2(기준 1, {1,2}) + Sheet 3(기준 3, {3,2,4})만 생성. 기준부재 2·4 시트 누락
  - **사용자 의도**: 모든 부재가 각자 기준부재 시트를 가지며, 포함부재는 1-hop 이웃
  - **수정**: [Form1.DrawingSheets.cs:105~142](../../A2Z/Form1.DrawingSheets.cs:105) `appearedAsIncluded` HashSet 선언·검사·추가 3곳 모두 제거. 주석도 T-015 결정 배경으로 교체
  - **결과 예**: 1-2-3-4 연쇄 Clash → Sheet 2(기준 1), 3(기준 2), 4(기준 3), 5(기준 4) 4개 생성. 단계 9의 Sheet 1 중복 제거 유지 (과잉 시트 자동 정리)
  - `docs/features/drawing-sheets/generate-sheets.md` 전면 갱신 — 이전 문서가 실제 코드와 불일치(BFS 서술·E03 오류·가공도/중복제거 누락)된 부분까지 교정
  - 빌드 검증은 사용자 기기에서 (A2Z.exe 실행 중이라 자동 빌드 불가)

### T-020 — 파일 열기·치수 추출을 탭 밖 공용 패널로 이동
- **완료일**: 2026-04-21
- **관련**: — (사용자 직접 지시, UX)
- **커밋**: `29e177f`
- **요약**:
  - `panelGlobalActions` 신설 — `splitContainer1.Panel1` 내 Dock.Top
    - 위치: panelGlobalViewButtons 아래, tabControlLeft 위
    - 배경색 `FromArgb(45,45,48)` — panelGlobalViewButtons와 통일
    - Size 438×60, Padding 5
  - `btnOpen`·`btnMainDimension`을 `groupBox1` → `panelGlobalActions`로 이관
    - 결과: 도면정보/작업·데이터/부재정보 **어떤 탭에서도 접근 가능**
    - 버튼 Location (x, 25) → (x, 5)로 조정
  - **groupBox1 후속 정리** (두 버튼 빠져 생긴 빈 공간 제거)
    - Size 110 → 55
    - 작은 버튼 6개 (BOM/Clash/Osnap/치수/2D 생성/PDF 내보내기) Y=78 → 20
  - 사용자 직접 빌드 확인 완료
  - R9 판단: UI 레이아웃 변경만이라 features/code-reference 갱신 불필요

### T-019 — 도면정보 탭을 첫 번째로 이동
- **완료일**: 2026-04-21
- **관련**: — (사용자 직접 지시)
- **커밋**: `3f51a02`
- **요약**:
  - 앱의 최종 목표가 **제작도 출력**이라 도면정보 탭을 첫 번째로 배치
  - 프로그래밍 위험 전수 검증 — 모두 안전
    - `SelectedIndex = 0` 하드코딩 (Designer L192): 탭 재배열 후 도면정보가 자동 기본 선택 (=원하는 동작)
    - `SelectedTab == tabPageDrawing` (GlobalViews.cs:54): 탭 **객체** 비교, 순서 무관
    - 다른 탭 인덱스 하드코딩 없음
  - Form1.Designer.cs 4곳 수정
    - L186~188: `Controls.Add` 순서 Drawing → Work → Attribute
    - TabIndex 재매김: Drawing=0, Work=1, Attribute=2
  - 런타임 로직 영향 0 (Designer 메타데이터만)

### T-011 — 시드 서브에이전트 2개 도입 (sdk-verifier, md-link-checker)
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백, 반복 실수 방지)
- **커밋**: `92d0488`
- **요약**:
  - 이번 대화에서 드러난 반복 실수 (`RenderModes.SOLID` 가정, `Model.Close` 누락, 링크 공백 133건 등) 방지용 시드 에이전트 2개 신설
  - `.claude/agents/sdk-verifier.md` — `VIZCore3D.NET.xml` 선행 검색으로 API 존재·시그니처·공식 예제 패턴 반환. SDK 새 멤버 처음 쓸 때 호출
  - `.claude/agents/md-link-checker.md` — `docs/**/*.md` 링크 공백·파일 부재 검증 + Python 치환 스크립트 제안. 대량 문서 수정 후 호출
  - `CLAUDE.md` R10, R11 추가 — 각 에이전트 호출 트리거 주소
  - **제외**: 오케스트레이터 프로토콜(동적 에이전트 생성·합병·삭제)은 사용 패턴 축적 후 재평가. 현 프로젝트 규모에 오버 엔지니어링 우려
  - "중간" 도입 경로 채택 (사용자 합의)

### T-010 — 문서 내부 링크 공백 문제 일괄 수정
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백)
- **커밋**: `10c7d8c`
- **요약**:
  - `docs/**/*.md` 전체 마크다운 링크 `]( ... )` 내부 공백을 **`%20`**으로 일괄 치환 (Python 스크립트)
  - **30파일, 147건 치환**. 상위: `사용자-매뉴얼/README.md`(44), `FEEDBACK.md`(8), 글로벌뷰 시리즈(6~7)
  - 외부 URL(`http://`, `https://`, `mailto:`, `#`로 시작)과 공백 없는 링크는 제외 처리
  - 대안(파일명 공백 제거 / `<path>` 각괄호)은 가독성·호환성 이유로 기각
  - 사용자 샘플 확인 통과

### T-008 — 초기화 버튼 + 같은 파일 재Open 버그 수정
- **완료일**: 2026-04-20
- **관련**: —  (FB/REQ 없음, 사용자 직접 지시)
- **커밋**: `45d17dd`
- **요약**:
  - 3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽에 `btnResetToInitial` ("초기화", 회색) 신설
  - `ResetToInitialState()` 헬퍼 — 누적 상태(List 9종 + UI ListView 5종 + SDK Clear 3종) 전면 초기화 후 동일 파일 재로드
  - `balloonOverrides.Clear()` 포함 (btnOpen이 누락했던 항목)
  - 확인 다이얼로그 + 가드 체크(`currentFilePath` + `Model.IsOpen`)
  - **버그 수정**: VIZCore3D는 같은 경로 중복 `Model.Open()`을 거부 → `Model.Open` 전 `if (IsOpen()) Close();` 패턴 적용 (공식 예제 L47297/L60261)
  - **btnOpen_Click 동반 수정**: 같은 파일 재선택 시 동일 버그 발생 소지 → 같은 패턴 적용
  - **UI 너비 축소**: 5개 버튼 Size 105→80, Location 재배치 (8/93/178/263/348), 패널 Size 558→438
  - 문서: `docs/features/bom/reset-to-initial.md` 신설 (BOM-005), `docs/사용자-매뉴얼/1.기본-작업/초기화.md` 신설, `open-model.md`에 Close 단계 추가, `_index.md`·`code-reference/form1-bom.md`·`사용자-매뉴얼/README.md` 갱신
  - 사용자 실기 테스트 통과 (부재 일부 숨기고 치수 추출 → 초기화 → 정상 복원)

### T-003 — 사용자 매뉴얼 전면 작성 (39개 버튼 문서)
- **완료일**: 2026-04-14
- **관련**: REQ-001
- **커밋**: `74fe209`
- **요약**:
  - `docs/사용자-매뉴얼/` 신규 폴더 + 39개 버튼 문서 + README
  - 실제 UI 라벨 기반 폴더·파일명 (`2.작업-데이터 탭/2D 생성.md` 등)
  - 7섹션 표준 템플릿 (요약/위치/사전조건/순서/분기/에러/이어지는작업)
  - SDK 용어 → 사용자 언어 번역 (에러는 실제 팝업 문구 그대로)
  - 멀티 에이전트 협업 (인벤토리 W-D → Writer W-A/B/C 병렬 → Reviewer 전수검사)
  - Reviewer 통과: 템플릿 0위반, 용어 0위반, 깨진 링크 0, 에러 메시지 일치
  - `docs/README.md` 상단에 개발자/사용자 분기 카드 추가
  - 개발자 문서(`docs/features/`) 영향 없음

### T-002 — 개발 워크플로우 자동화 확장
- **완료일**: 2026-04-13
- **관련**: —
- **커밋**: `ac14c86`
- **요약**:
  - REQUESTS.md (본인 요청 inbox, REQ-xxx) 추가
  - /checkpoint 슬래시 커맨드 (세션 요약 + 이어갈 지점)
  - PostToolUse 훅 (Form1.*.cs Edit/Write 시 docs 동기화 리마인더)
  - CLAUDE.md R2 확장 (4파일 자동 훑기), R8·R9 추가
  - /commit에 REQ-xxx 처리 통합

### T-001 — 프로젝트 초기 셋업 + 로직 흐름 문서화
- **완료일**: 2026-04-13
- **관련**: —
- **커밋**: `0000000` (초기 커밋)
- **요약**:
  - git 원격 연결 (github.com/uuuuj/a2z, HYI 브랜치)
  - 기존 HYI → X_HYI 로 아카이브
  - docs/ 로직 흐름 문서 72개 작성 (48개 핸들러 전수)
  - .gitignore 보강, CLAUDE.md, tracking 폴더 구조화
  - /commit 슬래시 커맨드 추가

---

## 형식 예시

```
### T-034 — 풍선 충돌 회피 로직 개선
- **생성일**: 2026-04-14
- **상태**: IN_PROGRESS
- **관련**: FB-012
- **세부**:
  - [ ] balloonOverrides Dict 사용 방식 개선
  - [ ] AABB 회전 시도 횟수 조정 (현재 36회 → 조절)
  - [ ] docs/features/drawing-sheets/drawing-iso.md 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (CreateIsoBalloonNotes)
  - `docs/features/drawing-sheets/drawing-iso.md`
```
