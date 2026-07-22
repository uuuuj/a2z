# 작업 목록 — IN_PROGRESS

> ⬅ [TASKS 인덱스](../TASKS.md)  ·  [TODO](./TODO.md) · [IN_PROGRESS](./IN_PROGRESS.md) · [BLOCKED](./BLOCKED.md) · [DONE](./DONE.md)

> 대부분 '구현 완료, 사내 PC 실기 검증 대기' 상태 — 출장 검증 체크리스트.

---

### T-077 — 가공도 3D 미리보기 형상 풍선 제거
- **생성일/착수일**: 2026-07-22
- **상태**: IN_PROGRESS (구현·컴파일 후 사내 미리보기/PDF 실기 검증 대기)
- **관련**: 사용자 직접 지시, GitHub issue #18
- **배경**: 도면번호 목록에서 가공도 부재를 선택할 때 3D 화면에 Hole·SlotHole·EarthBoss 풍선이 표시돼 형상 확인을 방해한다. PDF에는 기존 가공 정보 풍선이 계속 필요하다.
- **구현**:
  - [x] 공통 `BuildMfgSceneCore`의 풍선 생성 유지
  - [x] 3D 미리보기 전용 `ExecuteMfgDrawing`에서 코어 호출 직후 Review Note 제거
  - [x] PDF `RenderMfgRowToViewArea` 경로 무변경으로 풍선 유지
  - [ ] 사내 모델에서 일반/EA 3D 미리보기 풍선 미표시 확인
  - [ ] 가공도 PDF의 Hole·SlotHole·EarthBoss 풍선 유지 확인
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, 가공도 미리보기/PDF 흐름 문서, 가공도 코드 레퍼런스

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

### T-039 — 치수 생성 타이밍 재설계 + offset 고정 (2D 공간 기준)
- **생성일**: 2026-04-24
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (T-038과 결합 — 일반 시트 보조선 50/100mm 고정 PoC 1차, 가공도 별도)
- **사용자 사양 v1 (2026-05-12 초)**: 1단=50mm / 2단=100mm 고정 (캔버스 절대). 기준=보조선 끝점. 텍스트 마진 보정 X
- **사용자 사양 v2 (2026-05-12)**: 각 뷰의 치수 max 기준 동적 분기
  - max > 1000mm → 보조선 1단=10mm / 2단=20mm (캔버스 절대)
  - max ≤ 1000mm → 보조선 1단=20mm / 2단=40mm (≤500 포함)
  - 큰 치수일수록 보조선 짧게 (시각 균형)
- **구현 핵심 (v2)**: `ShowAllDimensions` 내부에서 `filteredDims.Max(d => d.Distance)` 계산 후 분기. `ShowAllDimensions` 시그니처 단순화 — 두 override(`baseOffsetOverride`, `levelSpacingOverride`) 제거, `canvasScaleOverride` 하나로 통합. 호출자는 scale 추정만 전달, 분기 로직은 내부 책임.
- **세부 (v2)**:
  - [x] `ShowAllDimensions` 시그니처 단순화 — `canvasScaleOverride = -1f` (Form1.Dimensions.cs:378)
  - [x] 내부 분기 — `maxDist > 1000` 기준 canvasBase/canvasLvl 결정 후 `/ scale`로 모델좌표 변환 (Form1.Dimensions.cs:497~)
  - [x] `EstimateFitScaleForCell` 헬퍼 그대로 사용 (Form1.DrawingSheets.cs:1498)
  - [x] `RenderSheetViewForDrawing` L1603 호출 — `estScale`만 전달 (분기 로직 호출자 제거)
  - [ ] **빌드 통과 후 사용자 사내 PC 실기 — 큰 치수 시트(>1000) 보조선 10/20mm, 작은 시트(≤1000) 20/40mm 도달 확인. DiagLog `T-038+039 v2 maxDist=N` 값 비교**
- **잔여 (2차 — 가공도 적용)** → 별도 계획서로 분리 진행: `docs/리팩토링/가공도-보조선-제작도통일.md` v2 (2026-06-03, Codex 1차 반영):
  - [x] 공용 헬퍼 `ComputeCanvasAbsoluteOffsets` 추출 + 제작도 교체 (동작 보존, `1aba8c7`)
  - [x] 가공도 `BuildMfgSceneCore(availW, availH)` + 캔버스 절대 5/10mm 분기 (`EstimateFitScaleForViewArea` fitFactor=1.0 추정). 빌드 통과
  - [ ] **사내 검증 — 가공도 보조선 부재 크기 무관 일정 + 모델 정합. 회전(Z90) 부재 추정 오차 확인. 부족 시 실측 newScale 2차**
  - [ ] EA 두 뷰·MULTI·`:1693`(FitObjectToGridCellAspect) 경로는 범위 외 (별도)
- **잔여 (3차 — 정확도 향상)**:
  - [ ] 사전 추정 vs 실제 RescaleObject scale 차이 측정 → 오차 분석
  - [ ] 큰 경우 2단계 렌더 (모델 먼저 → 실제 scale → 치수) 재설계
- **영향 파일**: A2Z/Form1.Dimensions.cs (시그니처+변수), A2Z/Form1.DrawingSheets.cs (헬퍼+호출)
- **선행**: T-038 (셀 크기 기반 모델 스케일)과 결합으로 진행 중

### T-040 — 치수 텍스트 ↔ 치수선 겹침 감지·회피 (가시성)
- **생성일**: 2026-04-24
- **착수일**: 2026-05-11
- **상태**: IN_PROGRESS (1차 — Level 1 offset i%2 토글 적용, 사용자 실기 검증 대기)
- **관련**: — (사용자 직접 지시)
- **배경**: 치수 숫자와 치수선/보조선이 겹쳐 숫자가 안 보이는 가시성 문제. "어떻게 감지하고 어떻게 회피할지 고민 필요" — 사용자 지시
- **감지 전략** (2D 공간 기준):
  - 각 치수 텍스트의 bounding box (중앙점 + 폰트 높이 × 예상 문자 폭)
  - 같은 뷰의 다른 치수선 segment들과 AABB ↔ 선분 충돌 테스트
  - 보조선·모델 라인도 충돌 대상 포함 여부 결정 필요
- **회피 전략 3단**:
  | Tier | 방법 | 구현 비용 | 근본 해결 |
  |---|---|---|---|
  | T1 | 치수 텍스트 뒤 **흰색 배경 마스크** | 낮음 | X (시각만) |
  | T2 | 평행 치수 **층별 오프셋** (동일 축 N번째 = +N×8mm) | 중간 | 부분 |
  | T3 | **Leader line + 자유 배치** (겹치면 텍스트만 측면으로 빼고 지시선 연결) | 높음 | O |
- **사용자 확인 필요**:
  - [ ] 우선순위 T1만 먼저 → T2 추가 → T3는 PoC 후 판단 수용 여부
  - [ ] 실기 겹침 사례 스크린샷 2~3건 (패턴 분석용)
- **세부**:
  - [x] sdk-verifier (2026-05-10): `Set2DMeasureTextBackground` 등 텍스트 배경색·마스크 API **부재**. T1 흰 마스크 SDK 직접 지원 X 확정
  - [ ] T1 구현 — SDK 미지원으로 폐기 (자체 흰 사각형 그리기 옵션 별도 검토 가능)
  - [ ] 겹침 감지 유틸 신설 (Form1.Dimensions.cs) — `ApplySmartFiltering`이 이미 텍스트 간격 검사로 부분 구현. 2026-05-11 진단 DiagLog 추가 (axis별 level0/level1 분포 검증용)
  - [x] **T2 변형 (2026-05-11)**: 사용자 요청 — `level1Offset` i%2 토글. 짝수 i=100mm, 홀수 i=50mm. 같은 축 내 측정축 좌표 순 정렬 후 인접 쌍 두 라인 분산
  - [ ] **T2 변형 취소 (2026-05-11)**: 사용자 결정 *"수치는 2줄만 생성 — 부재간 연쇄치수 + 전체치수"*. 토글 폐기, Level 1 foreach 원복. level2 적응형(`ApplySmartFiltering` 충돌 회피)은 유지. 별도 결정 시 level2도 폐기 가능
  - [x] **텍스트 위치 13mm 임계 (2026-05-11)**: 사용자 결정 *"치수 ≤13mm면 바깥, >13mm면 기본 위치 1, 기준 통일"*. `AlignDistanceTextPosition` 글로벌 옵션을 측정 추가 직전에 dim별 토글. `btnDimensionShowSelected_Click` foreach + `ShowAllDimensions` Level 1/2/0 세 그룹 모두 적용
  - [x] **AlignDistanceTextPosition 토글 폐기 (2026-05-13)**: 실기에서 토글이 작동 안 함을 사용자 보고. Softhills 담당자 예제 기반 `Drawing2D.Measure.SetMeasureItemDistanceTextPos(int, Vector3D)`로 전환. ≤13mm 측정 텍스트를 화면 오른쪽 캔버스 30mm 시프트 (모델 mm 환산 = 30/GetObjectScale). 일반 시트 + 가공도 메인 2경로. ISO 뷰·EA·MULTI 제외. 거리는 `MeasureItem.Position` MAIN 두 좌표로 추정 (옵션 A — `MeasureItem.Distance` 속성 부재). 빌드 통과로 SDK 메서드 실재 확정 (XML 미문서)
  - [x] **v2 (2026-05-13)**: v1 실기 보고 — 가로 보조선 10mm 미시프트(시프트 방향이 항상 H라 따라감). 치수축별 시프트 분기(H면 up / V면 right) + 뷰 max≤100mm skip + 거리 30→3mm
  - [x] **v3 (2026-05-13)**: v2 사용자 보고 — "반대로 적용". 시프트 방향 분기 스왑(가로→right / 세로→up), 부호 유지
  - [x] **v4 (2026-05-13)**: v3 보고 — (1) Z뷰 세로 치수 up 부호 -Y→+Y 보정 (2) 새 문제 "제작도 보조선이 내부 Osnap에서 시작해 모델 관통". 외곽 Osnap 복귀 알고리즘 도입 — `_osnapPool` 보존 + `ResolveExtensionOrigin` 헬퍼로 P 대신 offset 축 직선상 *치수선 쪽 외곽 Osnap* Q에서 보조선 시작. `axisPositiveOffset` 재사용. 일반 시트만 적용, 가공도는 다음 라운드
  - [x] **v5 (2026-05-13)**: v4 보고 — 외곽 Osnap 복귀가 반대 방향 결과. 부호 반전 시도도 효과 없음. 사용자 결정으로 전체 롤백 + 대안 *모델 라인 굵기 2.0→3.0* (보조선보다 진하게 → 시각 우선순위로 통과 거슬림 완화)
  - [x] **v6 (2026-05-13)**: 보조선 굵기 0.1 통일 (DrawingSheets 0.3→0.1, MfgDrawing 0.5→0.1 두 곳). 모델 vs 보조선 비율 30배. 치수선(MeasureLineWidth)은 그대로
  - [x] **v7 (2026-05-13)**: 직각 시프트 완전 폐기 + 평행 시프트 도입. 임계 maxEstDist/26 (예 1326→51), 시프트 거리 캔버스 3mm 유지. 인접 큰 dim 쪽 측정축 평행 슬라이드. 양쪽 같음→오른쪽, 한쪽만→반대(체인 바깥). ApplyParallelTextShift + FindMeasureByDimCoords 헬퍼 신설. SDK measure 매칭은 옵션 A(측정축 좌표 일치). 일반 시트만 적용(chainDimensionList 사용 경로). BOM bottom 11→10(1단위 아래로)
  - [x] **v8 (2026-05-13)**: v7 실기 — 시프트 미작동 보고 (좌표 매칭 실패 유력). XML로 `AddCustomAxisDistance`가 ID 반환 확인 → 옵션 C 전환. `ChainDimensionData.MeasureId` 필드 신설, `DrawDimension` 시그니처 `void→int`, ShowAllDimensions 3곳에서 dim.MeasureId 저장. ApplyParallelTextShift는 dim.MeasureId 직접 사용 (좌표 매칭 폐기). MfgDrawing의 DrawDimension 호출 9곳은 반환값 무시(컴파일 OK)
  - [x] **v9 (2026-05-13)**: v8 시프트 작동 OK이나 방향이 측정선 직각(90° 회전). SDK가 텍스트 평행 슬라이드 불가능 추정 → 시프트 축을 측정축에서 offset 축으로 교체. 인접 비교는 부호(±)만 결정. 결과: 측정선 직각으로 시프트 (사용자 사양 A)
  - [x] **v10 (2026-05-13)**: v9 보고 — offsetAxis 매핑이 사용자 시각과 반대(가로 치수가 좌·우 시프트). "가로/세로 오프셋 교환" = 측정축(axis) 직접 사용으로 복귀(v8 패턴). switch 인자 offsetAxis → axis
  - [x] **v11 (2026-05-13)**: 사용자 "1aaf85c(v6) 시프트 방법이 제일 잘됐다 — 그때로 복귀". ApplyParallelTextShift 헬퍼 내부 통째 교체 → v6 시점 직각 시프트(13mm 고정, SDK measureItem 직접, 가로→right/세로→up). 인접 비교/chainDimensionList 의존/maxEstDist/26 모두 폐기. 가공도에도 헬퍼 호출 복귀
  - [x] **v12 (2026-05-13)**: v11 베이스 + 임계 maxEstDist/26 + 인접 비교 부호 결정. SDK measureItem을 측정축별 그룹 후 dimCenter 정렬 → 좌·우 인접 estDist 비교로 shiftDir(±1) 결정. 직각 시프트는 v11 매핑 그대로
  - [x] **v13 (2026-07-21)**: 작은 치수 승격 후 2단 텍스트 슬라이드를 제작도·가공도 모두 종이 5mm → 2.5mm로 절반 축소. 단 간격·보조선 위치는 유지
  - [ ] **v12 실기 검증 대기** — 작은 치수가 인접 큰 dim 방향에 맞게 시프트되는지 / 부호 매핑이 사용자 시각과 일치하는지
  - [ ] **잔여**: 가공도 EA 두 번째 뷰(L1905) / 가공도 MULTI 경로 — 카메라 식별 별도
  - [ ] docs 갱신 (실기 검증 후 기능/치수/* + 기능/도면시트/* + 기능/가공도/* 별도 라운드)
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (AddChainDimensionByAxis, 겹침 검사 유틸 신설)
  - `A2Z/Form1.DrawingSheets.cs` (RenderSheetViewForDrawing 치수 후처리)
- **선행**: T-039 완료 후 (치수 배치 기준 확정돼야 겹침 판정 유의미)

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

### T-005 — 치수 배치를 Osnap 외곽 방향으로
- **생성일**: 2026-04-15
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (구현 완료, 사용자 사내 PC 실기 검증 대기)
- **관련**: FB-002
- **사용자 사양 (2026-05-12)**: 모델 전체 뷰 중앙 기준 4분면 — 중앙에서 가장 먼 Osnap이 있는 방향으로 치수. 상/하·좌/우 각각 max·min 거리 비교로 외곽 판정
- **구현 핵심**: 헬퍼 `ComputePositiveOffsetByOsnapExtreme(values, modelCenter)` 신설. `omax - center` vs `center - omin` 부호 있는 거리 비교 → 큰 쪽이 positive. 기존 `avg >= center` 5곳 전부 교체. 한쪽 쏠림(omin/omax 모두 center 한쪽)도 부호 자동 처리
- **세부**:
  - [x] 헬퍼 `ComputePositiveOffsetByOsnapExtreme` 신설 — Form1.Dimensions.cs GetAxisValue 옆
  - [x] 5곳 적용 — Form1.Dimensions.cs:499(메인, 치수추출+2D 출력 공용) / Form1.MfgDrawing.cs:335(가공도 메인) / :1057(가공도 보조) / :1192(MULTI) / :1707(EA newDims 비길이축, longestAxis 오버라이드 유지)
  - [ ] 빌드 통과 후 사용자 사내 PC에서 실기 — 부재가 모델 중앙 한쪽에 치우친 케이스에서 치수가 *그 반대쪽*(외곽)으로 빠지는지 확인
  - [x] docs/기능/치수/메인 치수 추출.md 갱신 (외곽 판정 알고리즘 섹션)
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (헬퍼 추가 + L499 패턴 교체)
  - `A2Z/Form1.MfgDrawing.cs` (4곳 패턴 교체)

### T-012 — 엑셀 템플릿 하이브리드 실험 (PoC)
- **생성일**: 2026-04-20
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (Step 1 코드 빌드 통과, 사용자 사내 PC 실기 검증 대기)
- **관련**: REQ-002
- **배경**: SDK가 `ImportExcel`, `ImportExcelWithData`, `Draw2DViewTemplate(path, x, y, w, h)`, `RenderTemplateOnGridStructure`를 제공 ([VIZCore3D.NET.xml:29219](../../lib/VIZCore3D.NET.xml:29219)). 담당자가 엑셀로 양식을 관리할 수 있는지 **실험만** (프로덕션 전환은 별개). 과거 Phase 18(`790a02a`)에서 BOM 동적 행수 문제로 수동 구성으로 되돌린 이력 있음 — 하이브리드로 재도전
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
- **관련 docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) (Step 1 흐름)

