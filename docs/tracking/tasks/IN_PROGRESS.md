# 작업 목록 — IN_PROGRESS

> ⬅ [TASKS 인덱스](../TASKS.md)  ·  [TODO](./TODO.md) · [IN_PROGRESS](./IN_PROGRESS.md) · [BLOCKED](./BLOCKED.md) · [DONE](./DONE.md)

> 대부분 '구현 완료, 사내 PC 실기 검증 대기' 상태 — 출장 검증 체크리스트.

---


### T-075 — 설치도 외부 연결 Assembly·실제 접합영역 위치 치수
- **생성일/착수일**: 2026-07-21
- **상태**: IN_PROGRESS (구현·컴파일 완료, 사내 PDF 실기 검증 대기)
- **관련**: 사용자 직접 지시, GitHub issue #12
- **배경**: 설치도에서 선택 STRU와 직접 연결된 외부 Assembly의 부착 위치를 보여줘야 한다. Clash HotPoint 하나만으로는 면접촉의 양 끝과 같은 Part/Body의 분리된 접합 영역을 표현할 수 없다.
- **구현**:
  - [x] 선택 STRU 실선 + 직접 연결 외부 Assembly 전체 점선을 ISO/Z/X/Y 전 뷰에 적용
  - [x] Clash PART 쌍 하위 BODY 조합에서 `GetObjectCollisionLine`, `GetJunctionMesh`로 실제 접합 영역 산출
  - [x] 이어진 선분 1mm 영역화, 분리 영역 A1/A2 라벨, LINE/POINT Osnap 3mm 스냅
  - [x] 선택 STRU·연결 Assembly 주축/보조축 전체 Osnap 범위 치수
  - [x] 연결 Part MIN → 접합 시작 → 접합 끝 → Part MAX 필수 체인 치수
  - [x] 접합 형상 없는 Clearance/Proximity는 HotPoint fallback + 로그
  - [x] Debug 별도 출력 폴더 빌드 통과
  - [ ] 사내 모델로 접합선/면접촉 A1/A2·4개 뷰 점선·치수 PDF 확인
- **영향 파일**: `A2Z/Form1.GlobalViews.cs`, `A2Z/Form1.DrawingSheets.cs`, `A2Z/Form1.Dimensions.cs`, `A2Z/Models.cs`, 설치도/Osnap 문서

### T-013 — ISO 뷰 점선·실선 분리와 3D 위치 정합
- **생성일**: 2026-04-20
- **착수일**: 2026-04-21
- **재개일**: 2026-07-21
- **상태**: IN_PROGRESS (연결 어셈블리 이름 표시까지 구현·컴파일 완료, 사내 PDF 실기 검증 대기)
- **관련**: 사용자 피드백, GitHub issue #7
- **배경**: 조립도는 전체 구조 중 기준부재만 실선, 제작도는 시트 부재 실선과 붙어 있는 시트 밖 주변 부재를 점선으로 표시해야 한다. 옛 수동 정합 방식은 캡처 원점·좌표계 불일치로 실패했다.
- **구현**:
  - [x] `Match2DObjectsTo3DObjectPosition(실선, 점선)`으로 옛 WorldToScreen 수동 정합 교체
  - [x] 조립도: 전체−기준부재 LONG_DASHED 점선 + 기준부재 실선
  - [x] 제작도: 전체 Body BBox·실제 부모 Part 1회 캐시 → 3mm 근접 후보 선별 → 후보만 전용 그룹 간섭검사
  - [x] 제작도: 연결 결과를 내부 연결성 `clashList`와 분리하고 `lvClash`에 `[연결]` 목록 표시
  - [x] 제작도: Crop 대상 2D 객체에 시트 부재+연결 부재를 함께 넣고 시트 부재 노드 기준으로 긴 연결 부재 절단
  - [x] 제작도: 이웃 캡처 → CropFit → LONG_DASHED → 점선 fit → 실선 캡처 → Match 순서 적용
  - [x] 제작도: Clash HotPoint XYZ를 보존하고 연결 Part의 가장 가까운 부모 Assembly 이름을 `Add2DNoteFromWorldCoordinate`로 접촉점에 표시
  - [x] 조립도: 실기 정상인 기존 캡처·배치 순서 유지(제작도 전용 순서 변경에서 제외)
  - [x] C# Compile 오류 0건 (기존 경고 7건)
- **사용자 확인 필요**:
  - [ ] 제작도 ISO에 붙어 있는 주변 부재가 점선으로 표시되는지
  - [ ] 진단 로그의 BBox 캐시 시간·근접 후보 수·원본 Clash 결과·상대 Part 목록 확인
  - [ ] Crop 범위가 붙은 부위 주변만 남기며 너무 좁거나 넓지 않은지
  - [ ] 연결 어셈블리 이름이 실제 접촉점을 가리키고 중복·겹침 없이 표시되는지
  - [ ] 실선과 점선의 3D 위치가 맞고 PDF에서도 LONG_DASHED로 출력되는지
- **영향 파일**: `A2Z/Form1.Clash.cs`, `A2Z/Form1.BOM.cs`, `A2Z/Form1.Stru.cs`, `A2Z/Form1.DrawingSheets.cs`, 관련 흐름·코드 레퍼런스 문서

### T-048 — 가공도 EA 앵글 모델 T자형 잘못 찍힘 수정
- **생성일**: 2026-04-28
- **착수일**: 2026-06-15
- **상태**: IN_PROGRESS (구현·빌드 완료, 사내 EA 모델 PDF 실기 검증 대기)
- **회사 매핑**: 확인 중 / 긴급중 2
- **관련**: 사용자 직접 지시. T-036 가공도 회전과 별개 이슈
- **회사 원문**:
  > 가공도에서 EA관련 모델은 위 아래로 한 번 붙이게 되는데 그거에 대한 2D View에 잘못 찍히는게 있어서 변경 필요. (잘못 찍힌다는게 XYZ축이 있으면 지금 어떤 방식인지는 모르겠지만 엣지가 판명되고 한쪽에서 찍으면 그대로 카메라를 위로 올리던가, 부재를 아래로 돌리던가 해서 한 번 더 찍고 붙일텐데, 몇몇 부재는 X뷰에서 찍고 Z축인 위에서 찍을 때 Y에서 Z방향으로 회전을 한 번 더해버려서 가로로 길게 위아래로 찍혀야 할 모델이 T자 모양으로 찍힘)
- **구현**:
  - [x] `IsAngleFromSpref` 기반 EA 카메라 열린 방향 보정 재활성화
  - [x] 엑셀 템플릿 View 영역을 위·아래 두 뷰로 분할
  - [x] 첫 번째 뷰에서 최장축 치수를 제외하고 두 번째 뷰로 분리
  - [x] 두 번째 뷰를 독립 `Z_MINUS` 또는 `X_MINUS + Z90` 카메라로 생성
  - [x] 과거 T자형 원인의 추가 정렬 회전 제외
  - [x] 두 번째 뷰 실패 객체 정리와 첫 번째 뷰 유지
  - [x] Debug 빌드 오류 0건
- **사용자 확인 필요**:
  - [ ] EA 부재 여러 방향에서 PDF 상하 뷰가 모두 가로 정렬되는지 확인
  - [ ] 길이축 체인/전체 치수가 두 번째 뷰에만 표시되는지 확인
- **영향 파일**:
  - `A2Z/Form1.MfgDrawing.cs`
  - `A2Z/Models/MfgViewPose.cs`
  - `docs/기능/가공도/가공도 시트.md`
  - `docs/기능/가공도/가공도 단일.md`

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

