# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**으로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

---

## 2026-04-20 — 초기화 버튼 추가 + 같은 파일 재Open 버그 수정

**유형**: feat + fix
**커밋**: `pending`
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
