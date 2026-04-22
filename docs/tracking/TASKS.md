# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

### T-024 — 단일 부재 치수추출 결과가 도면 시트 목록에 반영 안 됨
- **생성일**: 2026-04-22
- **상태**: TODO
- **관련**: — (사용자 직접 지시)
- **배경**: 3D View에 부재 **1개만** 띄운 상태에서 치수 추출 → 치수 자체는 산출되는데 `lvDrawingSheet`(도면 시트 목록)에 새 시트가 생성·갱신되지 않음
- **가설**:
  - `GenerateDrawingSheets`는 `Clash_OnClashTestFinishedEvent`에서 자동 호출되고, 단일 부재는 Clash 대상 쌍이 없음 → 시트 생성 트리거 미발동 가능성
  - 혹은 Sheet 1(전체)만 생성되고 가공도/설치도가 안 나와 사용자 체감상 "반영 안 됨"
  - `DetectClash`가 부재 쌍이 0이면 return false → Clash 이벤트 자체가 발생하지 않아 `GenerateDrawingSheets`가 호출되지 않을 수 있음
- **세부**:
  - [ ] 단일 부재 로드 → 치수추출 시나리오 재현 + Visual Studio 출력창 확인 (clashCount, drawingSheetList.Count)
  - [ ] `btnMainDimension_Click` → `DetectClash` → `Clash_OnClashTestFinishedEvent` → `GenerateDrawingSheets` 체인 추적
  - [ ] 단일 부재도 Sheet 1(전체) + 가공도 1개는 항상 생성되도록 보장 — Clash 의존성 분리 검토
  - [ ] 필요 시 `btnMainDimension_Click` 또는 별도 경로에서 "부재 1개 이상이면 시트 생성" 직접 호출
  - [ ] docs/features/drawing-sheets/generate-sheets.md 갱신 (단일 부재 케이스 분기 명시)
- **영향 파일**: A2Z/Form1.BOM.cs (btnMainDimension_Click), A2Z/Form1.Clash.cs (DetectClash, Clash_OnClashTestFinishedEvent), A2Z/Form1.DrawingSheets.cs (GenerateDrawingSheets)
- **연관**: T-023 (사전조건 강화 후 단일 부재 케이스가 정식 경로가 될 가능성)

### T-023 — 치수추출 사전조건 강화 (단일 모델 또는 선택상태만 허용)
- **생성일**: 2026-04-22
- **상태**: TODO
- **관련**: — (사용자 직접 지시)
- **배경**: 현재 `btnMainDimension_Click`은 모델이 로드되어 있으면 무조건 실행 → 여러 부재가 보이는 상태에서도 치수 추출 시도 → 결과가 의미 없거나 혼란스러울 수 있음. 사용자 요구: "치수추출의 기준은 3D View에 모델 하나만 띄워져 있을 때"
- **허용 조건 (후보)**:
  - (A) 3D View에 visible 부재가 **정확히 1개**일 때 (기본 안전망)
  - (B) T-022 구현 후, "선택상태(빨간색)" 부재가 1개일 때 해당 부재만으로 추출 (대안)
  - (A)를 기본으로 하고 (B)는 선택 기능 확정 시 추가
- **세부**:
  - [ ] `btnMainDimension_Click` 시작 지점에 visible 부재 카운트 계산 로직 추가 (`bomList` 중 `Object3D.FromIndex(idx).Visible == true` 개수)
  - [ ] visible != 1이면 MessageBox "치수 추출은 단일 부재만 표시된 상태에서 가능합니다" 안내 후 return
  - [ ] T-022 통과 시 "selected == 1"을 대안 허용 조건으로 병행
  - [ ] docs/features/bom/main-dimension.md 사전조건 섹션·예외 섹션 갱신
  - [ ] docs/사용자-매뉴얼/1.기본-작업/치수 추출.md 선결조건 명시
- **영향 파일**: A2Z/Form1.BOM.cs (btnMainDimension_Click), docs/features/bom/main-dimension.md, docs/사용자-매뉴얼/1.기본-작업/치수%20추출.md
- **연관**: T-022

### T-022 — 시트/BOM 선택 시 3D View 부재 "선택상태" 동기화
- **생성일**: 2026-04-22
- **상태**: TODO
- **관련**: — (사용자 직접 지시)
- **배경**: 현재 `lvDrawingSheet`·`lvDrawingBOMInfo` 선택 시 `FlyToObject3d`로 카메라만 이동. 3D View의 "선택상태(빨간색 하이라이트)"는 설정되지 않음. 사용자는 모델트리 이름 클릭·3D View 부재 클릭과 동일한 시각 피드백을 원함
- **"선택상태" 정의** (사용자 설명):
  - 3D View에서 부재 클릭 → 빨간색 변환
  - 모델트리 이름 클릭(체크박스 아님) → 3D View에서 빨간색
  - 체크박스는 visibility ON/OFF (별개 개념)
- **세부**:
  - [ ] SDK에 "프로그래밍 방식 선택상태 설정" API 존재 여부 확인 (`sdk-verifier` 호출 — `Object3D.Select*`, `Selection.Set`, `Highlight` 등 후보 전수)
  - [ ] 확인 결과에 따라 `LvDrawingSheet_SelectedIndexChanged`에 기준부재(BaseMemberIndex) 선택상태 동기화 추가
  - [ ] `LvDrawingBOMInfo_SelectedIndexChanged`에 해당 행 부재 선택상태 동기화 추가
  - [ ] 시트 변경·초기화·Open 시 선택상태 해제 보장
  - [ ] 빌드 + 실기 테스트 (빨간 하이라이트 확인)
  - [ ] docs/features/drawing-sheets/lv-sheet-selected.md, lv-bom-info-selected.md 갱신
- **영향 파일**: A2Z/Form1.DrawingSheets.cs, docs/features/drawing-sheets/*
- **선결 조건**: 없음 (SDK API 조회가 우선)
- **연관**: T-023 (선택상태 기반 치수추출)

### T-018 — 장시간 작업 진행 UX 표시 (치수 추출 5초 공백 개선)
- **생성일**: 2026-04-21
- **상태**: TODO
- **관련**: — (사용자 피드백)
- **배경**: `btnMainDimension_Click` 누르면 Clash 검사 **이전** 단계(BOM 수집 → Osnap 수집 → 치수 계산 → 표시)에서 약 **5초간 화면 반응이 없음**. 사용자가 앱이 멈춘 건지 처리 중인지 판단할 수 없어 UX 저하
- **해당 구간**:
  - `Form1.BOM.cs:370~430` `btnMainDimension_Click`
    - `CollectBOMData()` (bomList 재수집)
    - `CollectAllOsnap()` (모든 부재 Osnap 수집)
    - `MergeCoordinates` + `AddChainDimensionByAxis(X/Y/Z)`
    - `ShowAllDimensions()`
- **UX 개선 옵션** (난이도·효과 순):
  - (a) **`Cursor.Current = Cursors.WaitCursor`** — 1~2줄, 매우 쉬움, 최소 효과 (마우스 모양만 바뀜)
  - (b) **3D 뷰어 위 오버레이 라벨 "처리 중..."** — 10줄 수준, 쉬움, 시각 효과 큼 (이미 `txtMemberNameOverlay` 패턴 존재 — 참고 가능)
  - (c) **진행 다이얼로그 팝업** — 30줄 수준, 중간, 단계별 % 표시 가능
  - (d) **`BackgroundWorker` / `async Task` 비동기화** — 50~100줄, 어려움, UX 가장 자연스러움 (SDK가 메인 스레드 외에서 안전 호출되는지 검증 필요)
- **세부**:
  - [ ] 옵션 (a)~(d) 중 선택 (추천: **(b) 오버레이** — 최소 위험 + 충분한 효과)
  - [ ] 구현 범위 결정: 치수 추출만 / 다른 장시간 작업(2D 도면 생성, 가공도, PDF 출력, 시트 생성)도 포함
  - [ ] 공통 헬퍼 `ShowBusyOverlay(msg)` / `HideBusyOverlay()` 신설 검토
  - [ ] 구현 후 실기 확인 (다른 기기 포함)
  - [ ] docs/features/bom/main-dimension.md 갱신 (진행 표시 단계 추가)
- **영향 파일**: A2Z/Form1.BOM.cs + 공통 헬퍼 추가 시 A2Z/Form1.cs 또는 신규 파일
- **우선순위**: MEDIUM — 실사용 UX 직결, 특히 담당자 테스트 시 오해 방지
- **확장 가능성**: 동일 패턴을 다른 장시간 작업에도 적용 가능 (2D 도면 생성, ALL PDF 출력 등)


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

### T-017 — 라이선스 인증 코드를 Form1.BOM.cs에서 분리
- **완료일**: 2026-04-22 (사용자 실기 테스트 통과)
- **관련**: — (사용자 직접 지시, 코드 정리)
- **커밋**: `pending`
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
