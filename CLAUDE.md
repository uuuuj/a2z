# A2Z-HYI 프로젝트 — Claude Code 작업 규칙

이 파일은 Claude Code가 이 프로젝트에서 작업할 때 **항상 지켜야 할 규칙**을 담습니다. 대화 시작 시 자동으로 읽힙니다.

---

## 프로젝트 요약
VIZCore3D.NET SDK 기반 **3D→2D 도면 자동화 WinForms 앱** (C# / .NET Framework 4.8).
Form1 partial class 8개 + Models.cs 구조. 상세는 [`docs/README.md`](./docs/README.md), [`docs/_pipeline.md`](./docs/_pipeline.md) 참고.

---

## 핵심 규칙

### R1. 코드 변경 시 문서 동기화 (필수)
`A2Z/Form1.*.cs`의 **버튼 핸들러·이벤트 핸들러의 흐름을 수정**한 경우:
1. 해당 핸들러의 흐름 문서(`docs/features/{category}/{feature}.md`) 갱신
2. 라인 번호가 바뀐 경우 `docs/code-reference/form1-{category}.md`의 앵커 라인 정보 갱신
3. `last_updated` 프론트매터 + 하단 **변경 이력** 표에 항목 추가

단순 리팩토링/포매팅/로깅만 추가한 경우는 예외 (흐름 변화 없음).

### R2. 작업 시작 시 컨텍스트 로드
새 작업 시작 전 반드시 다음을 확인:
- [`docs/tracking/TASKS.md`](./docs/tracking/TASKS.md) — 현재 진행/대기 작업
- [`docs/tracking/FEEDBACK.md`](./docs/tracking/FEEDBACK.md) — 담당자 피드백 (OPEN 상태만)

관련 작업이 있으면 사용자에게 언급.

### R3. 작업 완료 시 `/commit` 사용
코드·문서 변경 후 `/commit` 슬래시 커맨드로 커밋. 이 커맨드가:
- `docs/tracking/CHANGELOG.md` 항목 자동 추가
- `docs/tracking/TASKS.md`에서 완료 항목을 `DONE` 섹션으로 이동
- 커밋 메시지 생성

### R4. 세션 종료 시 요약 저장
비자명한 작업을 한 세션(파일 여러 개 수정, 새 기능 추가, 버그 수정 등)은 종료 전 요약 저장:
- 파일: `docs/tracking/sessions/YYYY-MM-DD-주제.md`
- 내용: 무엇을 왜 했는지, 영향 범위, 다음 작업 제안

짧은 질의응답/탐색 세션은 생략.

### R5. Push는 명시적 허락 시에만
`git push`는 사용자가 **"푸시"** 또는 **"push"** 단어를 사용하여 명시적으로 요청할 때만 실행. `/commit`은 push를 포함하지 않음.

### R6. 언어 · 스타일
- 대화·커밋 메시지·문서: **한국어**
- 변수명·함수명: 기존 C# 코드 컨벤션(PascalCase/camelCase) 유지
- 주석: 필요한 곳만 (CLAUDE.md 기본 방침 준수)

### R7. 담당자 피드백은 원문 보존
사용자가 담당자 메시지를 공유하면 `docs/tracking/FEEDBACK.md`에 **ID 부여 + 원문 그대로** 기록 후 `TASKS.md`로 분해. 피드백 요약·의역 금지 (뉘앙스 유실 방지).

---

## 파일 구조 개요

```
a2z-HYI/
├── A2Z/                 # 앱 소스 (Form1 partial 8개 + Models.cs)
├── docs/
│   ├── README.md        # 문서 진입점
│   ├── features/        # 버튼 단위 흐름 문서 (48+)
│   ├── code-reference/  # 파일별 코드 레퍼런스 (9)
│   └── tracking/        # 개발 현황 추적
│       ├── FEEDBACK.md  # 담당자 피드백 inbox
│       ├── TASKS.md     # 할 일 (TODO/IN_PROGRESS/DONE)
│       ├── CHANGELOG.md # 완료 이력 (날짜 역순)
│       └── sessions/    # 세션별 작업 요약
├── .claude/
│   └── commands/
│       └── commit.md    # /commit 슬래시 커맨드
└── CLAUDE.md            # 이 파일
```

---

## 커밋 메시지 형식

```
{type}: {한 줄 요약, 50자 이내}

- 변경 상세 1
- 변경 상세 2
- 관련: FB-xxx, T-xxx (있다면)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
```

**type**: `feat` (신규 기능) / `fix` (버그 수정) / `docs` (문서만) / `refactor` (리팩토링) / `chore` (설정·빌드) / `style` (포매팅)
