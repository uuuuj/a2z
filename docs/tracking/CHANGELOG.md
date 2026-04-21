# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**으로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

---

## 2026-04-21 — T-013 옵션 A 시도 (Sheet2+ ISO 위치 정합)

**유형**: fix (시도)
**커밋**: `pending`
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
