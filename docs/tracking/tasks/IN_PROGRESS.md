# 작업 목록 — IN_PROGRESS

> ⬅ [TASKS 인덱스](../TASKS.md)  ·  [TODO](./TODO.md) · [IN_PROGRESS](./IN_PROGRESS.md) · [BLOCKED](./BLOCKED.md) · [DONE](./DONE.md)

> 대부분 '구현 완료, 사내 PC 실기 검증 대기' 상태 — 출장 검증 체크리스트.

---

### T-043 — 제작도·가공도·설치도 최종 출력 및 설명 자료 생성
- **생성일**: 2026-04-28
- **상태**: 개발 중 (최종 산출물 생성 중)
- **관련**: GitHub issue #9, Excel No.67
- **현황**:
  - [ ] 제작도 최종 생산 PDF 생성·점검
  - [ ] 가공도 최종 생산 PDF 생성·점검
  - [ ] 설치도 최종 생산 PDF 생성·점검
  - [ ] 메뉴얼·룰북 구조의 설명 자료와 프로젝트 이미지 정리
- **비고**: 앱 기능 개발보다는 최종 도면과 설명 자료를 생성하는 단계.

### T-091 — 장시간 도면 작업 취소 응답성 개선
- **생성일/착수일**: 2026-07-24
- **상태**: 실기 확인 (구현·Debug/Release 빌드 완료, 사내 대용량 모델 검증 대기)
- **관련**: 사용자 직접 지시, GitHub issue #51
- **구현**:
  - [x] 처리 오버레이 버튼을 `취소`로 단순화하고 현재 SDK 호출 뒤 가장 가까운 안전 지점에서 중단됨을 안내
  - [x] 메인 치수 추출의 전체 BODY 대상 스캔과 매 부재 BOM·홀 정보·Osnap, 목록 구성 루프에 진행 수와 `Application.DoEvents` 체크포인트 추가
  - [x] 대상 BODY 5,000개 이상이면 예상 장시간과 STRU 격리를 안내하고 기본 `취소`인 사전 확인 표시
  - [x] 취소 시 부분 BOM·시트·2D·3D·치수·Osnap 상태를 정리하고 관련 도면 출력 컨트롤 원상 복원
  - [x] 제작도·조립도·설치도 2D 생성의 엣지·템플릿·ISO/Z/X/Y 뷰·최종 렌더 전후 취소 확인
  - [x] 수동/일괄 가공도의 페이지·템플릿·행별 장면·주/보조 뷰·최종 렌더·PDF 저장 전후 취소 확인
  - [x] 가공도 취소 시 미완성 Canvas 정리, 저장 완료 PDF 수와 중단 위치 유지
  - [x] Debug·Release 빌드 오류 0개 (기존 경고만 유지)
- **사용자 확인 필요**:
  - [ ] 구역 전체가 보이는 상태에서 치수 추출 시 BODY 진행 수가 갱신되고 5,000개 이상 경고가 먼저 뜨는지
  - [ ] BOM 수집 중 취소 후 수초 내 종료되고 BOM·시트·2D·3D 임시 결과가 남지 않는지
  - [ ] 제작도·조립도·설치도 종류별 출력에서 현재 뷰 완료 직후 다음 뷰/시트로 넘어가지 않는지
  - [ ] 수동 가공도에서 행·EA 보조 뷰 사이 취소가 동작하고 저장 완료 PDF만 유지되는지
  - [ ] 도면 일괄 출력 취소 요약의 PDF 수와 실제 파일 수가 일치하는지
- **영향 파일**: `A2Z/Form1.cs`, `A2Z/Form1.BOM.cs`, `A2Z/Form1.DrawingSheets.cs`, `A2Z/Form1.MfgDrawing.cs`, `A2Z/Form1.Stru.cs`, 관련 BOM·도면시트·가공도 흐름과 코드 레퍼런스

### T-089 — 가공도 풍선 종이 절대 정규화·EA 뷰별 독립 배정
- **생성일/착수일**: 2026-07-24
- **상태**: 실기 확인 (구현·Debug/Release 빌드 완료, 사내 PDF 검증 대기)
- **관련**: 사용자 직접 지시, GitHub issue #46
- **구현**:
  - [x] Hole/SlotHole 관통축을 `ThicknessCenterTo - ThicknessCenterFrom` 우선으로 추출하고, 원형 홀 `CircleCenter`·SDK 로컬축 폴백을 교차검증
  - [x] EA 첫 번째·두 번째 실제 깊이축과 관통축을 비교해 뷰에 먼저 배정한 뒤 뷰별 규격·개수 그룹화
  - [x] 첫 번째·두 번째 `PendingNotes`를 독립 관리하고 각 캡처 직전 Note를 격리
  - [x] 모델 span 비례 EarthBoss 4분면 배치 제거, EarthBoss를 첫 번째 뷰 지연 생성으로 통합
  - [x] 캡처 후 확정된 `newScale`로 치수 외곽→풍선 6mm·풍선 행 8mm를 역산
  - [x] 풍선과 같은 화면 위·아래 쪽 치수선·문자 여백을 모델 fit 전에 예약해 잘림과 EA 반대 뷰 침범 방지
  - [x] EA 첫 번째·두 번째 풍선 목록의 최대 행 수와 최대 치수 외곽을 공통 예약해 한쪽 목록이 0건이어도 두 뷰의 모델 fit 높이를 동일하게 유지
  - [x] 풍선 글자 6mm·치수 글자 10mm는 2D 종이 절대값으로 유지
  - [x] Debug·Release 빌드 오류 0개 (기존 경고만 유지)
- **사용자 확인 필요**:
  - [ ] 크기 차가 큰 부재 5개를 한 페이지에 배치해 풍선 거리·행 간격·글자 크기가 같은지
  - [ ] EA 첫 번째 목록이 비고 두 번째 목록에만 Hole/SlotHole이 있는 부재에서 두 번째 뷰에만 표시되는지
  - [ ] 같은 EA 부재의 두 `[MfgAnnotationBudget]` 로그에서 `requested`·`reserved`·`fitHeight`가 같고 화면상 모델 배율도 같은지
  - [ ] 같은 규격의 홀이 두 플랜지에 나뉜 경우 뷰별 개수가 각각 맞는지
  - [ ] 세로 가로화·ORIENTATION·상하 미러 EA와 EarthBoss 위치에 회귀가 없는지
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, `A2Z/Models.cs`, `A2Z/Models/MfgViewPose.cs`, 가공도 시트·미리보기 흐름과 코드 레퍼런스

### T-064 — STRU 일괄 도면 출력 (P1 STRU 목록·강조 완료)
- **생성일**: 2026-05-13
- **착수일**: 2026-05-13
- **상태**: IN_PROGRESS (P1 구현 완료, 사용자 사내 PC 실기 검증 대기)
- **관련**: 사용자 직접 — STRU 단위 다중 선택 → 4종 도면(제작/조립/설치/가공) PDF 일괄 출력 흐름 도입
- **배경**: HYI-STRU 브랜치(2f024d1 외 4커밋)에서 STRU 흐름 시도했으나 간섭검사 폐기(1d17cc6) 범위가 과도해 HYI로 회귀. "리스트 뽑기까지만" 출발로 토론 → 전체 계획 합의
- **계획 합의 사항**:
  - 4종 도면 매핑: 제작도=Sheet1(-1) / **조립도=Sheet 2~N(≥0, 1-hop Clash 이웃)** / 설치도=Sheet(-2) / 가공도=Sheet(-3)
  - 버튼 구성: 2버튼 — `[도면 리스트 뽑기]`(간섭검사+시트목록 미리보기) / `[STRU 도면 자동 생성]`(즉시 4종 PDF)
  - 간섭검사 격리: **방법 A — 가시성 토글** (STRU별 Visible 토글 후 DetectClash 호출. SDK가 부재 단위 인자 미지원 — 사용자 통찰 "전체 1회도 어차피 오래걸려"로 N회 호출 감수)
  - HYI-STRU 자산: 구조만 참조 + `6bc89cf` NodePath fallback 패턴만 cherry-pick
- **Phase 분할**:
  - [x] **P1** — STRU 목록 표시 + 행 선택 시 3D 강조+카메라 fit (이번 커밋)
  - [ ] **P2** — `[도면 리스트 뽑기]` 버튼 (체크된 STRU별 가시성 토글 → DetectClash → GenerateDrawingSheets → lvDrawingSheet 누적, STRU 컬럼 추가)
  - [ ] **P3** — `[STRU 도면 자동 생성]` 단일 STRU 4종 PDF (파일명 `{STRU}_{종류}_{HHmmss}.pdf`)
  - [ ] **P4** — 다중 STRU 배치 + 진행률 + 실패 정책 + 메모리 강화
- **P1 구현**:
  - 신규 `A2Z/Form1.Stru.cs` — `CollectStruList` / `RuleByFrameworkChildParent` / `PopulateStruCheckList` / `btnSelectAllStru_Click` / `ClbStruList_SelectedIndexChanged` (3D 강조+fit)
  - **STRU 식별 룰 재설계 (사용자 모델트리 분석 기반)**: STRU = 자식 중 NodeName이 "FRMWORK " 시작 어셈블리가 있는 어셈블리. 모수를 `LEAF_ASSEMBLY` → `ASSEMBLY`로 확대 + `RuleByFrameworkChildParent`에서 `ParentIndex`로 1단계 부모 추출. 향후 룰 추가 가능한 union HashSet 구조.
  - **재귀 강조 명시화**: `GetChildObject3d(idx, NodeFilterKind.BODY)` → `GetChildObject3d(idx, Object3DChildOption.ALL_CHILDREN, true)` 후 `Where(b => b.Kind == NodeKind.BODY)` 필터. SDK xml line 4877/4583 검증.
  - SDK 정정 적용: `Object3D.Color.RestoreColorAll` (View.Color 아님), `View.FlyToObject3d` (View.Camera 아님). sdk-verifier 사전 검증.
  - `BeginUpdate/EndUpdate`는 try/finally로 감싸 예외 시에도 UI 잠금 해제 보장
  - CheckedListBox 의미 분리: 선택(`SelectedIndex`)=강조용 / 체크(`CheckedItems`)=출력 대상용. `CheckOnClick=true`로 클릭 1회에 동시 트리거
  - Designer.cs: `groupBoxStru` (Dock=Top 240px) + clbStruList + 라벨 + 전체선택 버튼만 (P2/P3 버튼 미포함)
  - BOM.cs: `BuildBodyToPartNameMap()` 직후 `PopulateStruCheckList()` 호출 1줄
- **위험 관리**:
  - 가시성 토글 try/finally 복원 (P2에서 도입 필요)
  - 묶음 GC (P3에서 도입)
  - 가공도 vs 제작/조립/설치 함수 시그니처 차이 — P2 진입 전 SDK 매핑 점검
- **P2 본진 진행 (2026-05-14)**:
  - [x] 가시성 격리·CollectBOMData·DetectClash·시트 자동 생성 흐름 (직전 커밋들)
  - [x] 가공도 P3 분기 제거 → 옛 GridStructure 8×3 흐름 유지 (`a2427c4`)
  - [x] **엑셀 분기에 치수 그리기 이식** (이번 커밋) — `GenerateSheetDrawing2D_WithExcelTemplate` 루프 본문을 옛 `RenderSheetViewForDrawing` 패턴으로 교체. ISO 풍선 + X/Y/Z `ShowAllDimensions` + `Add2DObjectFromShapeDrawing`/`Add2DMeasureFrom3DMeasure`. 모델 shrink Z=0.65/X·Y·ISO=0.70 (사용자 사양). 새 헬퍼 `EstimateFitScaleForViewArea` 추가.
  - [ ] 사용자 사내 PC 실기 검증 — 일반 시트 PDF에 치수+풍선 표시, 모델 영역 적정 (라벨·보조선 침범 없음) 확인
- **다음 단계**: 사용자 사내 PC에서 모델 열기 → 좌측 STRU 패널 표시 → 행 클릭 시 3D 빨강+fit 확인 → 도면 리스트 뽑기 → 일반 시트 치수 확인

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
  - [x] docs `메인 치수 추출.md` 단계 12·13 재기술 + 변경 이력
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 — DiagLog의 `ComputeViewDimensionsForMembers=Xms` 수치 개선 비교
- **후속 검토 여지**:
  - 오버레이 메시지 세분화 (예: "Osnap 수집 중 {n/N}") — 체감 시간 개선용
  - `GetOsnapPoint` 자체가 병목이면 Part 단위 배치 API 검토
- **영향 파일**: A2Z/Form1.cs (+1 필드), A2Z/Form1.BOM.cs (CollectAllOsnap 루프, CompleteMainDimensionPostClash), A2Z/Form1.Dimensions.cs (ComputeViewDimensionsForMembers 파라미터 추가)
- **연관**: T-018 (오버레이 UX), T-028 (치수 엔진 통합)

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
  - [x] docs 2종 갱신: `메인 치수 추출.md` 파이프라인 재기술, `시트 선택.md` 분기 A 재작성
  - [x] **2026-05-11 (사용자 요청)**: `GenerateSheetDrawing2D` L1242도 `ExtractInstallationDimensions` → `ComputeViewDimensionsForMembers(null, 0.5f)` 교체 — 2D 출력 후 작업데이터 탭 = 도면 측 측정 데이터 1:1 일치
  - [x] **2026-05-11 (사용자 요청)**: `ExtractInstallationDimensions`의 "개별 부재 전체 길이" 블록 제거 (Form1.GlobalViews.cs L287~346) — 비인접 점 쌍처럼 보이는 부작용 해소. 시트 선택 -2 분기도 자동 영향
  - [ ] 설치도(-2) 분기 완전 폐기 검토 (옵션 A 전환) — 현재 BBox 유지 중. 사용자 확인 필요
  - [ ] 사용자 실기 확인 (4경로 일관성, 중복 제거 효과, 설치도 BBox 유지 확인, 2026-05-11 변경 반영)
- **영향 파일**:
  - `A2Z/Models.cs` (+1 필드)
  - `A2Z/Form1.Dimensions.cs` (공용 헬퍼 +80줄, ShowAllDimensions -70줄, FilterOsnapByViewDimensionUsage -45줄)
  - `A2Z/Form1.BOM.cs` (CompleteMainDimensionPostClash 치수 블록 -15줄)
  - `A2Z/Form1.DrawingSheets.cs` (LvDrawingSheet_SelectedIndexChanged 분기 +10줄)
  - docs: `메인 치수 추출.md`, `시트 자동 생성.md`, `시트 선택.md`

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
  - [x] docs/기능/도면시트/시트 2D 렌더.md 1차 갱신 (단계표 7~9 추가, 분기 C 추가, 변경 이력 3건)
- **세부** (2차 — 추가 실험):
  - [ ] SDK 조사: 뷰 셀 내부 clip / 치수선 경계 제어 API (`sdk-verifier` 서브에이전트)
  - [ ] 치수선 렌더링 경로 추적 — 현재 치수선이 어디서 그려지며 왜 셀을 벗어나는지
  - [ ] `targetHeight=40f` 하드코드 → 셀 크기 기반 동적 계산
  - [ ] 풍선 예약 영역 설계 + 적용
  - [ ] ISO/X/Y/Z 라벨 위치 고정 (하단)
  - [ ] 빌드 + 실기 테스트 (치수선 셀 이탈 여부, 모델 크기, 풍선 겹침, 라벨 위치 모두 확인)
  - [ ] docs/기능/도면시트/시트 2D 렌더.md 2차 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D, RenderSheetViewForDrawing, CreateIsoBalloonNotes)
  - `docs/기능/도면시트/시트 2D 렌더.md`
- **참고**: T-007은 본 항목에 흡수되어 제거됨 (2026-04-22)

### T-037 — 2D 출력 BOM 테이블 줄바꿈 방지 + ITEM 열 분리 기준 확장
- **생성일**: 2026-04-24
- **착수일**: 2026-05-10
- **상태**: IN_PROGRESS (2차 — 한 번 고정 폭 + 폰트 축소 시도, 사용자 실기 검증 대기)
- **관련**: — (사용자 직접 지시, T-006/FB-003 심화)
- **배경**: 2D 출력 시 BOM 셀에 긴 텍스트가 들어가면 `IsTextWrapped=true`로 wrap되면서 행 높이가 늘어나 14행 레이아웃이 깨짐. ITEM 열 값은 UDA `SPREF`에서 "/" 제거 후 ":" split로 추출 — 사용자 요구로 추가 split 기준(`-` / `/` 등) 포함 필요
- **사용자 방침** (2026-05-11 확정): **테이블 열 너비는 한 번 정해서 고정** (콘텐츠 변동 따라 매번 바꾸는 거 지양). 폭 미세조정 + 폰트 전체 축소 조합 OK
- **사용자 확인 필요**:
  - [ ] **실제 SPREF 값 예시 2~3건 공유** (UDA 원본과 원하는 ITEM 결과 표기)
  - [ ] split 우선순위 확정 (`:` → `-` → `/` 순서? 가장 짧은 유효 토큰 택일?)
- **세부**:
  - [x] sdk-verifier (2026-05-10): `TemplateTableData.FontSize`/`AutoFit`/`CellFontHeight` **모두 부재**. `Set2DViewCreateObjectItemTextHeight(float)`는 일반 2D 객체 텍스트용으로 명시 — Template/Table 적용 보장 X (실기 시도로만 최종 확인 가능)
  - [ ] 옵션 A: `IsTextWrapped=false` + 셀 폭 초과분 "..." 말줄임 — 미채택 (정보 손실 위험)
  - [x] 옵션 B 변형 (2026-05-11): `Set2DViewCreateObjectItemTextHeight(4f)`로 BOM 렌더 직전 폰트 축소 시도 — **빌드 결과로 SDK 적용 가부 최종 판정 예정**
  - [ ] 옵션 C: ITEM 추가 split 구현 (사용자 답변 후 확정)
  - [x] 열 너비 1차 재분배 (2026-05-10, c635978) — 사용자 방침에 따라 revert (97c1cba)
  - [x] 열 너비 2차 고정 (2026-05-11): No 5, ITEM 20, MATERIAL 12, SIZE 14, Q'TY 7, T/W 8, MA 5, FA 6 — 합 77mm 유지, **콘텐츠 맞춤 X 한 번 고정**
  - [ ] docs/기능/도면시트/시트 2D 렌더.md 갱신
- **잔여 옵션 (폰트 축소 안 먹을 경우)**:
  - [ ] 헤더 약자화 ("MATERIAL"→"MAT", "Q'TY"→"Q", "T/W"→"TW") — 도면 표준 허용 여부 사용자 결정 필요
  - [ ] Drawing2D 원시 API로 셀 자체 그리기 (별도 큰 작업)
- **영향 파일**:
  - `A2Z/Form1.Clash.cs` (CollectBOMInfo — SPREF 파싱)
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D BOM 테이블 블록 L1218~1318)

### T-038 — 2D 출력 셀 크기 기반 모델 스케일 + 여백 예산
- **생성일**: 2026-04-24
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (step B 완료 — `targetH = 0f` 적용. step C 대기: 동적 마진)
- **사용자 사양 (2026-05-12)**: 모델 셀 가득 + 보조선 영역 확보 (단계별 — B 모델부터, C 동적 마진)
- **step B (2026-05-12)**: `targetH = 40f → 0f` (Form1.DrawingSheets.cs:1372). `FitObjectToGridCellAspect`만 사용. 결과: 셀 100% 가득이지만 잘림
- **step B-2 (2026-05-12)**: 사용자 사양 "15프로 줄여보자". `targetH = 0f` 분기에 `else { RescaleObject(*, scale * 0.85f) }` 추가 (L1704, L1879). 결과: 85% 차지, 15% 안전 마진
- **step C 계획 (필요 시)**: 셀 가용 높이 = cellH - 라벨박스H(약 10~15mm) - 풍선 영역(약 10~12mm) - 보조선 영역(보조선 max 길이 + 텍스트 마진). 동적 targetH 계산
- **관련**: — (사용자 직접 지시, T-006 2차 실험 흡수)
- **배경**: 현재 `targetH=40f` 하드코드. 셀 높이 ≈ 95mm이므로 58% 여유 공간 낭비. 모델을 키우고 싶지만 그리드 이탈·풍선/라벨/치수선 겹침 위험
- **제안 여백 예산** (사용자 승인 필요):
  - 셀 95×92mm 기준: 상단 라벨 8mm + 풍선 영역 12mm + 하단 치수 15mm + 모델 60mm
  - 좌우: 치수 영역 10×2mm + 모델 72mm
- **사용자 확인 필요**:
  - [ ] 위 예산 수용 여부 (모델 60×72mm 영역 OK인지)
  - [ ] 뷰별 스케일 통일(모든 뷰 동일 비율) vs. 뷰별 개별 최대화 선호
- **세부**:
  - [ ] sdk-verifier: `GridStructure.GetGridCellSize(row,col)` / `GetCellBounds` 류 API 존재 확인
  - [ ] `RenderSheetViewForDrawing`의 `targetHeight` 파라미터를 예산 기반 동적 계산으로 교체
  - [ ] Sheet 2+ ISO의 `bgObjId`·`objId` 공통 스케일 유지 (현재 따로 놀 위험)
  - [ ] 그리드 이탈 감지 — `GetObjectBounds(id)` 호출 후 셀 영역과 비교
  - [ ] docs 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L1298~1311, RenderSheetViewForDrawing L1430~)
- **선행**: sdk-verifier 조사 먼저
- **연관**: T-039(치수 offset 동기화)는 이 작업 완료 후 진행

### T-036 — 가공도 시트: 선택상태 해제 + ISO 뷰 느낌 해결
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 재확인 대기)
- **관련**: — (사용자 피드백)
- **경로 전개**:
  1. 1차 진입: 선택상태 해제(DESELECT_ALL) + DiagLog 추가 (커밋 `230e45f`)
  2. 1차 해석 시도: "Z 최장축인데 세로로 배치" → L215 `use1803d && longestAxis!="Z"` 가드 추가 (커밋 `537f07c`)
  3. **사용자 재보고 (2026-04-23)**: "45도 대각 ISO 뷰로 보게 된다" → Z 방향이 아닌 **카메라 방향 자체가 ISO로 잔존**하는 증상
  4. **원인 재확정**: [LvDrawingSheet_SelectedIndexChanged](../../../A2Z/Form1.DrawingSheets.cs) 공통부 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 글로벌 ISO) 유지한 채 이동 → `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 덮어쓰지 못함
  5. 수정 (커밋 `b0f8802`): 가공도 시트(-3) 분기 앞에서 `FlyToObject3d` 스킵 + L215 180° 스킵 가드 **원복** (수직 뒤집기 의도 복원)
- **세부 (완료)**:
  - [x] `ExecuteMfgDrawing` 진입부 `Object3D.Select(DESELECT_ALL)` 추가
  - [x] 회전 진단 `DiagLog T-036 MfgDrawing bom=... sizeXYZ=... longestAxis=... isPadOrPlate=... viewDir=... use180=... useMinus=... Z90Applied=... R180Applied=...`
  - [x] 1차 해석 L215 가드 → 원복 (ISO 원인 아님)
  - [x] `LvDrawingSheet_SelectedIndexChanged`에서 가공도 분기 시 `FlyToObject3d` 스킵
  - [x] `use1803d` 바깥 스코프 승격 (DiagLog 가시성 — 유지)
  - [x] docs/기능/도면시트/시트 선택.md / 가공도 단일.md 갱신
  - [x] MSBuild Debug 통과
  - [x] **후속 (2026-04-23)**: 사용자 "카메라 재조정 중 가로→세로 깜빡" 관찰 → `ExecuteMfgDrawing` 전체를 `BeginUpdate/EndUpdate`로 감싸 중간 상태 노출 차단 + Z 최장축 90° 회전 직후 누락됐던 `FitToView` 추가
  - [x] **재수정 (2026-04-23)**: 사용자 DiagLog 공유 → "누르는 순간 가로 → 0.5초 뒤 FitToView로 세로" 확정 → **직전 커밋의 FitToView가 바로 원인**. 제거. 원본 주석 경고 "LockZAxis false 유지 — true로 복원하면 렌더링 엔진이 회전을 리셋"이 FitToView에도 동일 적용
  - [x] **3차 수정 (2026-04-23, sdk-verifier 기반)**: 내부 FitToView 제거만으론 세로 복귀 여전. `LockZAxis`는 키보드용이라 무관 확정. SDK 정공법 `GetCameraData()` + `SetCameraData(data, false)` 스냅샷 복원 패턴 도입. Form1.cs에 `_mfgDrawingCameraSnapshot` 필드 추가, ExecuteMfgDrawing Z 90° 직후 `GetCameraData()` 저장, `LvDrawingSheet_SelectedIndexChanged` 말미에 가공도(-3) 확인 후 `SetCameraData(snapshot, false)` 복원
  - [x] **사용자 실기 재보고 (2026-04-24)**: "아직도 세로로 출력되는 부재들이 있음" → 3차 수정으로 일부는 해결됐으나 **여전히 세로 잔존 부재 존재**. 새 가설 필요
  - [ ] 사용자 정보 수집 필요: 어떤 부재가 세로로 남는지 DiagLog (`T-036 MfgDrawing` 라인 + `T-036 카메라 스냅샷 복원` 라인) 비교 — Z 최장축 케이스인지 / non-Z 케이스인지 / 스냅샷이 저장됐는지 / SetCameraData 호출됐는지
  - [ ] **새 가설 후보**:
    1. Z 최장축이 아닌 X·Y 최장축 부재가 카메라 회전이 아예 안 적용된 채 가공도 진입 (스냅샷은 Z 케이스에만 저장됨)
    2. 가공도 시트가 처음 선택될 때만 스냅샷 적용 — 다른 시트 거쳤다 다시 같은 가공도로 돌아오면 스냅샷이 다른 가공도 것으로 덮어써졌을 가능성 (가공도가 여러 개일 때)
    3. SetCameraData(false) 후에도 외부 어딘가가 카메라 재변경
  - [ ] 위 가설 검증 위해 `_mfgDrawingCameraSnapshot`을 **Dictionary<int, CameraData>** (가공도 번호 키)로 확장 검토
- **영향 파일**: A2Z/Form1.MfgDrawing.cs, A2Z/Form1.DrawingSheets.cs, docs/기능/가공도/가공도 단일.md, docs/기능/도면시트/시트 선택.md

### T-012 — 엑셀 템플릿 하이브리드 실험 (PoC)
- **생성일**: 2026-04-20
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (Step 1 코드 빌드 통과, 사용자 사내 PC 실기 검증 대기)
- **관련**: REQ-002
- **배경**: SDK가 `ImportExcel`, `ImportExcelWithData`, `Draw2DViewTemplate(path, x, y, w, h)`, `RenderTemplateOnGridStructure`를 제공 ([VIZCore3D.NET.xml:29219](../../../lib/VIZCore3D.NET.xml:29219)). 담당자가 엑셀로 양식을 관리할 수 있는지 **실험만** (프로덕션 전환은 별개). 과거 Phase 18(`790a02a`)에서 BOM 동적 행수 문제로 수동 구성으로 되돌린 이력 있음 — 하이브리드로 재도전
- **사용자 결정 (2026-05-12)**: 옵션 A — 기존 `GenerateSheetDrawing2D` 유지 + 별도 partial class `Form1.ExcelTemplate.cs`에 PoC 핸들러 신설. 새 디버그 버튼 "엑셀 PoC" 추가. Step 1 시각 검증 후 단계별 진행.
- **사전 자료**: `사용자템플릿_엑셀_Rev_01.xlsx` (사용자 작성) — A4 가로 비율(W/H ≈ 1.41), 55컬럼 × 40행, 4뷰(ISO/Z/X/Y) + BOM + NOTE + 도면정보 + TAG NO + 이미지 슬롯 4개
- **세부**:
  - [x] **Step 1 (2026-05-12)**: `btnExcelTemplatePoC` 핸들러 + `vizcore3d.Drawing2D.Template.ImportExcel(path)` 단독 호출. 빌드 통과. 사용자 사내 PC 시각 검증 대기
  - [x] **확정**: `Drawing2DTemplateManager.templateDatas`는 private/internal 필드 외부 접근 불가 (빌드 시 확정)
  - [ ] **Step 2**: 셀 좌표 수집 — `ParseJson` 등 다른 public API 탐색. placeholder(`{Image}`, `ISO`, `LOOKING "X/Y/Z"`, `BILL OF MATERIAL`) → Row/Column 매핑
  - [ ] **Step 3**: 셀 영역에 `AddModel(viewIndex)` + 이미지/BOM/풍선/치수 배치
  - [ ] 결과 리포트: `docs/기술 노트/excel-template-experiment.md` 신설
  - [ ] T-057(검토자 Excel 일치 검증)과 통합 — Rev_01이 검토자 Excel과 같은 양식인지 확인
- **영향 파일**: A2Z/Form1.ExcelTemplate.cs (신규), A2Z/Form1.Designer.cs(+버튼 1개, groupBox1 너비 +87px), A2Z/A2Z.csproj(+Compile Include), 사용자템플릿_엑셀_Rev_01.xlsx (신규)
- **관련 docs**: [엑셀 템플릿 PoC.md](../../기능/도면시트/엑셀%20템플릿%20PoC.md) (Step 1 흐름)
