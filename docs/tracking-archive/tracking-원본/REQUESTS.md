# 본인 수정 요청 (REQUESTS)

개발자 **본인**이 생각한 수정/개선 아이디어를 기록합니다. 담당자 피드백과 구분하여 결정 맥락·우선순위를 명확히 보관.

> **상태 값**: `OPEN` (아이디어 접수) / `IN_REVIEW` (고려 중) / `ACCEPTED` (작업 결정, TASKS.md로 이동) / `REJECTED` (기각) / `DONE` (완료)

> **우선순위**: `HIGH` (긴급/필수) / `MEDIUM` (중요하지만 대기 가능) / `LOW` (여유될 때)

---

## 접수 대기 / 진행 중

### REQ-007 — 앱 시작 시간 단축 (라이브러리 매번 추출 → 조건부)
- **생성일**: 2026-07-03
- **상태**: OPEN
- **우선순위**: MEDIUM
- **배경**: 빌드 후 실행 또는 배포된 exe 실행 시 메인 창이 뜨기까지 오래 걸림. 원인 분석 결과 `Form1()` 생성자의 `VIZCore3D.NET.ModuleInitializer.Run()`이 SHDC 공식 문서의 "항상 추출(Case 2)" 방식이라 매 실행마다 네이티브 라이브러리(ShdCore.dll 등)를 디스크에 재추출. 라이선스 서버(127.0.0.1:8901) 동기 연결과 무거운 초기화가 모두 생성자·OnInitialized에서 UI 스레드를 붙잡아 창 표시 전까지 블로킹.
- **기대효과**:
  - `ModuleInitializer` 조건부 추출(`ExistLibrary()`/`CompareVersion()` = 공식 Case 1)로 두 번째 실행부터 startup 단축
  - Stopwatch 측정으로 실제 병목(추출 vs 라이선스 연결 vs 엔진 생성) 확정
  - (선택) 스플래시 또는 빈 창 먼저 띄우기로 체감 개선
- **관련**: A2Z/Form1.cs:197 (`ModuleInitializer.Run`), A2Z/Form1.License.cs:24 (`LicenseServer`)
- **분해된 작업**: 미부여 (착수 시 T-xxx)

### REQ-006 — Clash Detection 결과 행 선택 시 두 부재 3D 강조 + 카메라 fit
- **생성일**: 2026-05-11
- **완료일**: 2026-05-11 (commit `66ac0bb`)
- **상태**: ACCEPTED (구현 완료, 사용자 실기 검증 대기)
- **우선순위**: MEDIUM
- **배경**: 사용자 *"Clash Detection 눌렀을 때도 그 간섭검사 한 두 개의 부재 강조할 수 있는지도 궁금. 강조는 3D View에서 빨간색으로 선택 후 카메라로 fit 하는거야 마치 BOM 테이블에서 행 눌렀을 때처럼"*. `lvClash`는 더블클릭 시 강조+fit이 이미 있었으나 단일 클릭(SelectedIndexChanged)엔 없음
- **기대효과**: 디버깅 효율 향상. 행 클릭 즉시 두 간섭 부재 빨간 강조 + 카메라 자동 회전 → 어떤 부재끼리 충돌인지 시각 즉시 식별
- **관련 기능**: [Clash 선택 시 자동 필터](../기능/치수/Clash%20선택%20시%20치수%20필터.md)
- **분해된 작업**: 별도 T 미부여 (commit `66ac0bb`로 직접 처리)

### REQ-005 — 체인치수 ListView 행 선택 시 두 부재 3D 강조 + 카메라 fit
- **생성일**: 2026-05-11
- **완료일**: 2026-05-11 (commit `21bed37`)
- **상태**: ACCEPTED (구현 완료, 사용자 실기 검증 대기)
- **우선순위**: MEDIUM
- **배경**: 사용자 *"체인치수 목록에서 선택했을 때 치수나 Osnap을 강조할 수 있는지도 궁금"*. lvDimension 행 클릭 시 해당 치수의 두 점이 속한 부재를 3D에서 즉시 강조 + fit
- **기대효과**: lvDimension을 디버깅 도구로 활용 — 어느 행이 어느 부재 사이 거리인지 즉시 시각 매칭. T-028 본진(데이터 소스 통일) 후속의 디버깅 인프라 완성
- **구현 핵심**: `ChainDimensionData.MemberIndices` 필드 신규 (Models.cs). `ExtractInstallationDimensions`에서 정확히 채움, `ComputeViewDimensionsForMembers`는 좌표↔nodeIdx 사후 매핑(`coordKeyToMembers` 사전 + 결과 dim에 사후 채움)
- **관련 기능**: [치수 추출](../기능/치수/설치도%20치수%20추출.md)
- **분해된 작업**: 별도 T 미부여 (commit `21bed37`로 직접 처리)

### REQ-004 — Osnap 좌표목록 행 선택 시 부재 3D 강조 + 카메라 fit
- **생성일**: 2026-05-11
- **완료일**: 2026-05-11 (commit `86a533d`)
- **상태**: ACCEPTED (구현 완료, 사용자 실기 검증 대기)
- **우선순위**: MEDIUM
- **배경**: 사용자 *"Osnap이랑 체인치수 목록에서 선택했을 때 치수나 Osnap을 강조할 수 있는지도 궁금"*. lvOsnap 행 클릭 시 해당 osnap의 부재를 즉시 강조+fit
- **기대효과**: Osnap 분석 디버깅 효율 향상. 부재이름 → bomList 매핑으로 다중 선택도 지원
- **구현 핵심**: `LvOsnap_SelectedIndexChanged` 신설. `_suppressOsnapSelChanged` 가드로 LvClash 자동 선택 흐름 시 카메라 흔들림 방지
- **관련 기능**: [모든 Osnap 수집](../기능/2D도면/Osnap%20수집.md)
- **분해된 작업**: 별도 T 미부여 (commit `86a533d`로 직접 처리)

### REQ-003 — Osnap 좌표목록 컬럼을 6개로 축소 (No, 축, 부재이름, X, Y, Z만)
- **생성일**: 2026-05-11
- **완료일**: 2026-05-11 (commit `86a533d`)
- **상태**: ACCEPTED (구현 완료, 사용자 실기 검증 대기)
- **우선순위**: MEDIUM
- **배경**: 사용자 *"osnap 정리는 Osnap 좌표목록을 실제 사용하는 OSnap만 남기자는 의미. No, 축, 부재이름, X, Y, Z만 남기면 될 거 같은데"*. 기존 7컬럼 (No/부재이름/X/Y/Z/홀사이즈/슬롯홀)에서 홀사이즈·슬롯홀 제거 + **축** 신규 추가
- **기대효과**: 사용자가 LINE osnap의 축 정보를 ListView에서 즉시 식별. 불필요한 정보 제거로 가독성 향상
- **구현 핵심**: `osnapPointsWithNames` 튜플 `(Vertex3D, string, string axis)` 확장. LINE osnap 축 추정(`EstimateOsnapLineAxis`: start→end 벡터 최대 성분), POINT/수동은 빈 문자열. `nodeOsnapPts`/`_lastCollectedNodeOsnapMap`은 2원소 유지(ComputeView 영향 차단)
- **관련 기능**: [모든 Osnap 수집](../기능/2D도면/Osnap%20수집.md)
- **분해된 작업**: 별도 T 미부여 (commit `86a533d`로 직접 처리)

### REQ-002 — 2D 도면 템플릿의 엑셀 외부화 (하이브리드)
- **생성일**: 2026-04-20
- **상태**: ACCEPTED
- **우선순위**: MEDIUM
- **배경**:
  현재 `tableInfo`(로고·프로젝트명·제작자)와 `BOM` 테이블 헤더/열너비/스타일이 `Form1.DrawingSheets.cs`에 하드코딩. 회사·프로젝트·고객별로 양식이 다르고, 변경 시 재빌드·배포가 필요해 담당자가 직접 수정 불가. 과거 Phase 18(`790a02a`)에서 한 번 엑셀 기반→수동 구성으로 되돌린 이력은 **BOM의 동적 행수 문제** 때문. tableInfo는 정적이라 문제 없음
- **기대효과**:
  - 담당자가 엑셀 파일에서 양식(로고·헤더·테두리·폰트) 직접 편집 → 재빌드 없이 반영
  - 프로젝트별 템플릿 파일 스위칭 (사내 표준 / 고객 A / 고객 B …)
  - SDK가 `ImportExcelWithData(path, Dict)`와 `Draw2DViewTemplate(path, x, y, w, h)`를 공식 제공하므로 라이브러리 추가 없음 (Aspose.Cells는 이미 포함)
  - 현재 4분할 뷰 + 우측 BOM/tableInfo 구조는 **하이브리드로 유지** (시나리오 2)
- **관련 기능**:
  - [2D 출력](../사용자-매뉴얼/4.도면정보%20탭/2D%20출력.md)
  - 개발자 문서: [GenerateSheetDrawing2D](../기능/도면시트/시트%202D%20렌더.md)
- **분해된 작업**: T-012 (PoC 실험) → 결과에 따라 Phase B1(tableInfo)·B2(BOM 스타일) 후속

---

## 완료 (DONE)

### REQ-001 — 실사용자(담당자)가 읽을 수 있는 사용자 매뉴얼 작성
- **생성일**: 2026-04-14
- **완료일**: 2026-04-14
- **상태**: DONE
- **우선순위**: HIGH
- **배경**:
  기존 `docs/기능/*`는 개발자용 로직 문서라 실 담당자가 "도면 생성 버튼은 어떤 버튼이야?" 질문에 답하기 어려웠음. 파일명(`2D 생성.md`)·본문 용어(`RenderMode=DASH_LINE`)가 비개발자에게 난해.
- **기대효과**:
  실제 UI 라벨로 폴더·파일명을 구성한 사용자 매뉴얼 신설. 담당자가 앱 UI를 보며 즉시 문서를 찾고 로직을 이해 가능.
- **분해된 작업**: T-003

---

## 기각 (REJECTED)

_이력 없음_

---

## 형식 예시

```
### REQ-001 — PDF 저장 경로 설정 가능하게
- **생성일**: 2026-04-14
- **상태**: OPEN
- **우선순위**: MEDIUM
- **배경**:
  현재 도면 일괄 출력의 저장 위치가 자동 결정됨.
  사용자마다 원하는 저장 폴더가 다르고, 버전 관리도 어려움.
- **기대효과**:
  - 저장 경로 설정 UI 또는 App.config 옵션
  - 마지막 사용한 경로 기억
  - 프로젝트별 분리된 디렉터리 구조 가능
- **관련 기능**:
  - [단일 PDF](../기능/2D도면/PDF 출력.md)
- **분해된 작업**: (ACCEPTED 전환 시 기록) T-xxx, T-yyy
```

---

## FEEDBACK.md와의 차이

| 항목 | FEEDBACK.md | REQUESTS.md |
|---|---|---|
| 출처 | 담당자 (외부) | 본인 (내부) |
| 핵심 | **원문 보존** (뉘앙스 유실 방지) | **결정 맥락** (왜/언제/우선순위) |
| ID | `FB-xxx` | `REQ-xxx` |
| TASK 연결 | `T-xxx (from FB-xxx)` | `T-xxx (from REQ-xxx)` |
