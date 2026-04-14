# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**으로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

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
