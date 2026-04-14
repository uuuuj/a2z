# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

_대기 중인 작업 없음_

---

## IN_PROGRESS

_진행 중 작업 없음_

---

## BLOCKED

_차단된 작업 없음_

---

## DONE (최근 20개)

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
