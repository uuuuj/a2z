---
description: 변경사항을 검토하여 docs 동기화 + CHANGELOG/TASKS 갱신 + 커밋 + 자동 push 수행
---

# /commit — 통합 커밋 워크플로우

## 너의 임무
현재 작업 트리의 변경사항을 커밋하되, 다음 규칙을 **반드시** 지켜라.

## 절차

### 1. 변경 내용 파악
- `git status --short` + `git diff` (스테이지 포함/미포함 모두) 확인
- 변경이 전혀 없으면 사용자에게 알리고 종료

### 2. 문서 동기화 확인 (R1 준수)
수정된 파일 중 `A2Z/Form1.*.cs`가 있으면:
- **핸들러 흐름이 바뀐 경우**: 대응하는 `docs/기능/{카테고리}/{기능}.md` 문서를 업데이트
  - 흐름 표, 분기, 예외, 상태 변화 섹션 중 해당 부분만
  - `last_updated` 프론트매터 갱신
  - 하단 **변경 이력** 표에 오늘 날짜 + 변경 요약 추가
- **라인 번호가 크게 바뀐 경우**: `docs/code-reference/form1-{category}.md`의 앵커 라인 정보 갱신
- 단순 리팩토링/포매팅/로깅이면 문서 갱신 불필요 (사용자에게 확인 후 스킵)

판단 애매하면 사용자에게 물어봐라.

### 3. TASKS 갱신 (상태별 파일 — `docs/tracking/tasks/`)
- 작업은 상태별 파일로 분리됨: `tasks/TODO.md` · `tasks/IN_PROGRESS.md` · `tasks/BLOCKED.md` · `tasks/DONE.md`. 인덱스는 `docs/tracking/TASKS.md`.
- 이번 커밋으로 완료된 작업이 있으면 해당 `T-xxx` 항목 **블록 전체**를:
  - `tasks/IN_PROGRESS.md` 또는 `tasks/TODO.md`에서 **잘라내** `tasks/DONE.md` 최상단으로 이동
  - 완료일 + 커밋 해시(일단 `pending`, 커밋 후 실제 해시로 업데이트) 기입
  - 이동으로 파일별 작업 수가 바뀌면 인덱스 `TASKS.md`의 "작업 수" 표도 갱신
- 부분 완료면 원래 파일에서 체크박스만 업데이트하고 이동 안 함
- 커밋과 무관한 TASK는 건드리지 마라

### 4. FEEDBACK.md / REQUESTS.md 갱신 (해당 시)
- TASK가 `FB-xxx`에서 유래했고 이번 커밋으로 해당 FEEDBACK이 전체 해결된 경우:
  - FEEDBACK.md에서 `FB-xxx` 항목을 `DONE` 섹션으로 이동
- TASK가 `REQ-xxx`에서 유래했고 이번 커밋으로 해당 REQUEST가 전체 해결된 경우:
  - REQUESTS.md에서 `REQ-xxx` 항목을 `DONE` 섹션으로 이동
- 부분 해결이면 이동하지 말고 상태만 `ACCEPTED` 유지

### 5. CHANGELOG.md 항목 추가
상단에 새 섹션 추가 (기존 내용 위에):
```
## YYYY-MM-DD — {한 줄 요약}

**유형**: {feat/fix/docs/refactor/chore/style}
**커밋**: `pending` (커밋 후 업데이트)
**관련 TASK**: T-xxx
**관련 FEEDBACK**: FB-xxx (있을 때만)
**관련 REQUEST**: REQ-xxx (있을 때만)
**변경 사항**:
- 항목 1
- 항목 2

**영향 범위**: {간단히}
```

### 6. 커밋 메시지 작성
CLAUDE.md의 커밋 메시지 형식을 따른다:
```
{type}: {한 줄 요약 50자 이내}

- 변경 상세 1
- 변경 상세 2
- 관련: FB-xxx, REQ-xxx, T-xxx

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
```

### 7. 커밋 실행
- `git add -A` (또는 명시적 파일)
- `git commit -m "..."` (HEREDOC 사용)
- 사전 훅 실패 시 원인 파악 후 수정 → 재커밋 (amend 금지, 새 커밋)

### 8. 커밋 해시 반영
커밋 성공 직후:
- CHANGELOG.md의 `pending` 부분을 실제 짧은 해시(`git rev-parse --short HEAD`)로 교체
- `tasks/DONE.md`의 해당 항목 커밋 해시도 마찬가지로 교체
- **이 수정분은 다음 커밋에 포함시키지 말고**, 사용자에게 보고 후 다음 `/commit` 호출 시 함께 반영되도록 둔다 (누적 방지)

### 9. 자동 push (CLAUDE.md R5)
커밋 성공 직후 `git push` 실행:
- 일반 HYI 브랜치 push는 자동 진행
- push 실패 시 (원격 변경 충돌 등) 사용자에게 보고 후 지시 대기
- 예외 — 사용자에게 먼저 확인:
  - `--force` / `-f` 가 필요한 상황
  - `main` / `master` 브랜치에 직접 push
  - 파괴적 히스토리 변경 동반

### 10. 보고
사용자에게 요약:
- 커밋 해시 + 메시지 첫 줄
- 갱신한 tracking 파일 목록
- push 결과 (성공 시 원격 경로, 실패 시 원인)

## 금지 사항
- **커밋 amend 금지**: 훅 실패 등은 새 커밋으로 해결
- **pre-commit hook 건너뛰기 금지**: `--no-verify` 사용 금지
- **메모리/무관한 파일 자동 스테이징 금지**: `.env`, `*.key`, 대용량 바이너리가 새로 생겼으면 사용자에게 먼저 알림
- **위험 push 자동 실행 금지**: `--force` / main·master 직접 push 는 명시 승인 필요
