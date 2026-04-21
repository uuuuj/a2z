# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

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

### T-017 — 라이선스 인증 코드를 Form1.BOM.cs에서 분리
- **생성일**: 2026-04-21
- **상태**: TODO
- **관련**: — (사용자 직접 지시, 코드 정리)
- **배경**: `Form1.BOM.cs`에 라이선스 인증 관련 코드가 들어있는데 BOM(부재 목록 수집) 기능과 책임이 다름. 파일 가독성·단일 책임 원칙 측면에서 분리 필요
- **현재 위치** (분리 대상):
  - `Form1.BOM.cs:172` `StartLicenseRefreshTimer` (30분 주기 타이머 시작)
  - `Form1.BOM.cs:183` `LicenseRefreshTimer_Tick` (실제 갱신 로직, 예외 시 Debug.WriteLine)
  - `Form1.BOM.cs` 내 `licenseRefreshTimer` 필드 (Form1.cs:99 선언)
  - `Vizcore3d_OnInitializedVIZCore3D` 내부의 `License.LicenseServer(127.0.0.1, 8901)` 호출
- **분리 옵션**:
  - (A) **`Form1.License.cs` 신규 partial 파일** — 최소 변경, 같은 클래스 유지. 가장 저위험
  - (B) **독립 `LicenseManager` 클래스** — 구조 개선, Form1 의존성 제거 (VIZCore3DControl 주입). 향후 MVP 재설계 준비용
- **세부**:
  - [ ] 라이선스 관련 메서드·필드·이벤트 호출 전수 식별
  - [ ] (A)/(B) 중 선택 — 지금 단계에선 **(A) 추천** (리팩토링 충격 최소)
  - [ ] 코드 이동 + 기존 호출부 그대로 유지
  - [ ] docs/code-reference/form1-bom.md — 라이선스 관련 항목 제거
  - [ ] docs/code-reference/form1-license.md (신규) — 새 partial 설명
  - [ ] 빌드 + 라이선스 갱신 동작 확인
- **영향 파일**:
  - `A2Z/Form1.BOM.cs` → 라이선스 관련 메서드 제거
  - `A2Z/Form1.License.cs` (신규)
  - `docs/code-reference/form1-bom.md`, `form1-license.md`
- **우선순위**: LOW — 기능 변경 없음, 현재 버그 수정(T-013 등)·기능 추가 작업 완료 후 착수 권장

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

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-003
- **세부**:
  - [ ] 현재 `GenerateSheetDrawing2D`의 2x3 그리드(ISO+3축 / 라벨) 셀 크기 상수화
  - [ ] BOM 테이블(table1) · 도면정보 테이블(tableInfo)의 폭·높이 고정값 정의
  - [ ] A4 297x210 기준 영역 레이아웃 스펙 명시
  - [ ] docs/features/drawing-sheets/generate-sheet-2d.md 동기화
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L957+)

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

### T-013 — Sheet2+ ISO 뷰 배경·선택 부재 위치 정합
- **생성일**: 2026-04-20
- **착수일**: 2026-04-21
- **상태**: IN_PROGRESS (옵션 A 시도 중, 사용자 테스트 대기)
- **관련**: — (사용자 피드백)
- **배경**: Sheet2 이상에서 ISO 뷰는 "전체 모델 점선(bgObj) + 선택 부재 실선(obj)"으로 그려지는데, 선택 부재가 **원본 위치가 아니라 전체 모델의 중심**으로 이동됨
- **원인 분석**: 두 객체 모두 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin`로 **캔버스 원점에** 생성됨 → `GetObjectCenter`가 둘 다 (0,0) 근처 반환 → `(objCX0 - bgCX0) ≈ 0` → 위치 보정 공식이 무력화되어 obj가 bg 중심으로 이동
- **세부**:
  - [x] Form1.DrawingSheets.cs `RenderSheetViewForDrawing` L1327~L1430 `isIsoFullView` 분기 분석
  - [x] 원인 가설 확정 (원점 생성 + GetObjectCenter 한계)
  - [x] **옵션 A 시도**: objId의 `RescaleObject`/`MoveObject`/보정 계산 모두 제거 → SDK 자동 처리에 맡김. DiagLog로 bgObjId/objId 중심·스케일 실측 기록
  - [ ] 사용자 빌드+테스트 결과 확인 (Release 빌드 + 다른 기기)
  - [ ] 실패 시 옵션 B(WorldToScreen 기반 위치 계산)로 전환
  - [ ] 성공 시 docs/features/drawing-sheets/generate-sheet-2d.md 본문 갱신
- **옵션 B 준비**: `vizcore3d.View.WorldToScreen(Vertex3D, bool)` 확인됨 ([VIZCore3D.NET.xml:63853](../../VIZCore3D.NET.xml)). 3D 중심 2개를 화면 좌표로 변환 → 차이를 obj 이동량으로 사용
- **영향 파일**: A2Z/Form1.DrawingSheets.cs, docs/features/drawing-sheets/generate-sheet-2d.md

### T-014 — 도면정보 테이블의 "기준부재/포함부재" 정의 + 풍선 번호 매칭
- **생성일**: 2026-04-20
- **상태**: TODO
- **관련**: — (사용자 피드백)
- **배경**: `lvDrawingBOMInfo`의 "기준부재"·"포함부재" 컬럼 값의 의미가 불분명. **최종 목표**는 두 컬럼 값을 ISO 뷰 풍선 번호와 동일하게 표시해 도면 가독성 향상
- **세부**:
  - [ ] Form1.Clash.cs `CollectBOMInfo` (L20~) 분석 — 두 컬럼에 현재 매핑되는 값 문서화
  - [ ] Form1.DrawingSheets.cs `CreateIsoBalloonNotes` 출력(부재 Index ↔ 풍선 번호) 매핑 추출
  - [ ] 두 컬럼 값을 풍선 번호로 교체 (또는 별도 컬럼 추가)
  - [ ] docs/features/clash/collect-bom-info.md 갱신
- **영향 파일**: A2Z/Form1.Clash.cs + A2Z/Form1.DrawingSheets.cs

### T-015 — Sheet 생성 알고리즘 스펙 문서화
- **생성일**: 2026-04-20
- **상태**: TODO
- **관련**: — (사용자 피드백)
- **배경**: `GenerateDrawingSheets`는 Clash 인접 리스트 기반 BFS로 부재 군집화해 시트를 생성. "겹치는 시트 제외" 등 정확한 기준이 문서화되지 않음
- **세부**:
  - [ ] Form1.DrawingSheets.cs `GenerateDrawingSheets` L18~L398 단계별 분석
  - [ ] Sheet1(전체) 생성 기준, Sheet2~N(군집) 전이 조건 정리
  - [ ] clashList 인접 리스트 구축 알고리즘 문서화
  - [ ] 중복/겹침 제외 로직 (포함 관계 판정) 명시
  - [ ] docs/features/drawing-sheets/generate-sheets.md 확장
- **영향 파일**: 문서만 (코드 변경 없음)


### T-007 — 뷰 내부 모델 최대화 + 라벨·풍선 영역 확보
- **생성일**: 2026-04-15
- **상태**: BLOCKED (T-006 선행 권장)
- **관련**: FB-004
- **세부**:
  - [ ] T-006 완료 후 각 뷰 셀의 "모델 배치 가능 영역"(라벨·풍선 제외) 좌표 계산
  - [ ] `RenderSheetViewForDrawing`의 targetHeight(현재 40f) → 셀 최대 크기 동적 계산으로 변경
  - [ ] 풍선 예약 영역(상단 또는 측면) 일정 크기 확보
  - [ ] ISO/X/Y/Z 라벨 하단 위치 고정
  - [ ] docs/features/drawing-sheets/generate-sheet-2d.md 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (RenderSheetViewForDrawing, CreateIsoBalloonNotes)
- **참고**: FB-003·004는 같은 2D 도면 레이아웃. T-006 구조 확정 후 T-007 진행하면 수정 충돌 최소화

---

## IN_PROGRESS

### T-009 — 초기화 버튼 누락 항목 보강
- **생성일**: 2026-04-20
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS
- **관련**: T-008 후속 (사용자 피드백)
- **배경**: 초기화 버튼 실기 테스트 결과, 도면정보 탭 BOM 목록·3D 렌더모드(DASH_LINE)·2D 캔버스가 남아 있는 상태 발견
- **세부**:
  - [x] Form1.BOM.cs `ResetToInitialState()` 정리 블록에 3줄 추가
    - `lvDrawingBOMInfo.Items.Clear()` (도면정보 탭 BOM)
    - `vizcore3d.View.SetRenderMode(RenderModes.SMOOTH)` (DASH_LINE 해제, 기본 모드 복귀)
    - `Clear2DView()` (2D 캔버스 정리)
  - [x] docs/features/bom/reset-to-initial.md — 단계 3 설명/상태 변화 섹션/변경 이력 갱신
  - [x] **후속**: `Clear2DView()` 호출 시점을 `Model.Open` 성공 이후로 이동 (SDK가 Open 시 2D 뷰 자동 복원하는 이슈, 4번 번쩍임 해결)
  - [ ] 빌드 + 다른 기기 실기 테스트 (사용자, 푸시 후)
- **영향 파일**:
  - `A2Z/Form1.BOM.cs` (ResetToInitialState)
  - `docs/features/bom/reset-to-initial.md`
- **SDK 참고**: `RenderModes.SOLID`는 존재하지 않음. 기본 실선 모드는 `SMOOTH` ([VIZCore3D.NET.xml:4534](../../VIZCore3D.NET.xml))

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정
- **생성일**: 2026-04-15
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS
- **관련**: FB-003
- **확정 스펙** (옵션 A):
  - A4 가로 297×210 / 마진 10 / 그리드 2×3 (셀 ≈ 92.3×95 mm) — 현재 유지
  - 뷰 4개: (1,1)ISO / (1,2)Z / (2,1)Y / (2,2)X — 현재 유지
  - **BOM → (1,3) 셀 이관** (`RenderTemplateOnGridStructure(table1, 1, 3)`)
  - **tableInfo → (2,3) 셀 이관** (`RenderTemplateOnGridStructure(tableInfo, 2, 3)`)
  - BOM 열 너비 합 82 → **92 mm로 조정** (ITEM 28→38, 그 외 유지)
  - BOM 최대 데이터 행 **14행**, 초과 시 마지막 행에 "…" + "+N건 생략" 표시 (옵션 2-a)
  - Anchor/X/Y 절대좌표 제거 → 셀 정렬(`SetGridCell*Alignment`)로 대체
  - 하드캡 30mm는 이번 범위 밖 → T-007에서 처리
- **세부**:
  - [x] Form1.DrawingSheets.cs L1020~1080 수정 — bInfo 절대좌표 제거, BOM/tableInfo `RenderTemplateOnGridStructure` 이관
  - [x] BOM `BOM_MAX_DATA_ROWS = 14` 상수 + "…+N건 생략" 행 렌더링
  - [x] BOM 열 너비 2차 축소: ITEM 28→38→17, MATERIAL/SIZE 8→12→11 (합 82→92→81→**77mm**)
  - [x] tableInfo 2차 축소: 60→57→47, 35→35→30 (합 95→92→81→**77mm**)
  - [x] 셀 정렬: BOM (1,3) Top/Center, tableInfo (2,3) Bottom/Center
  - [x] docs/features/drawing-sheets/generate-sheet-2d.md 갱신 (단계표 7~9 추가, 분기 C 추가, 변경 이력 3건)
  - [ ] 빌드 + 다른 기기 실기 테스트 (사용자, 푸시 후)
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L1020~1080, 약 +18줄)
  - `docs/features/drawing-sheets/generate-sheet-2d.md`

---

## BLOCKED

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

### T-020 — 파일 열기·치수 추출을 탭 밖 공용 패널로 이동
- **완료일**: 2026-04-21
- **관련**: — (사용자 직접 지시, UX)
- **커밋**: `pending`
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
