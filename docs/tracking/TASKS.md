# 작업 목록 (TASKS) — 인덱스

실행 가능한 개발 작업을 **상태별 파일로 분리** 관리합니다. (2026-07-22 — 파일이 1000줄을 넘어 `tasks/` 폴더로 상태별 분할. `/commit` 자동화·기존 링크는 이 인덱스로 계속 유효.)

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 상태 이동·완료 기록은 `/commit`이 자동 관리.

## 상태별 작업 (`tasks/`)

| 상태 | 파일 | 작업 수 | 내용 |
|---|---|---|---|
| 🟡 IN_PROGRESS | [tasks/IN_PROGRESS.md](./tasks/IN_PROGRESS.md) | 22 | 진행 중 — 대부분 사내 PC 실기 검증 대기 |
| ⬜ TODO | [tasks/TODO.md](./tasks/TODO.md) | 9 | 대기 — 외부 답변·재현 케이스 대기 다수 |
| 🔴 BLOCKED | [tasks/BLOCKED.md](./tasks/BLOCKED.md) | 1 | 차단됨 (재현 조건 수집 중) |
| ✅ DONE | [tasks/DONE.md](./tasks/DONE.md) | 52 | 완료 이력 |

- ID 체계(`T-`/`FB-`/`REQ-`)·전체 워크플로우: [README.md](./README.md)
- 입력: [FEEDBACK.md](./FEEDBACK.md)(담당자) · [REQUESTS.md](./REQUESTS.md)(본인) / 출력: [CHANGELOG.md](./CHANGELOG.md)

> 2026-07-22 재배치: 상태가 IN_PROGRESS인데 TODO에 있던 7건(T-037·T-038·T-039·T-040·T-036·T-005·T-012)을 IN_PROGRESS.md로 이동. TODO.md에는 실제 대기 항목만 남음. (`T-039 (구)`는 격상된 tombstone이라 TODO에 잔존)

## 형식 예시

```
### T-034 — 풍선 충돌 회피 로직 개선
- **생성일**: 2026-04-14
- **상태**: IN_PROGRESS
- **관련**: FB-012
- **세부**:
  - [ ] balloonOverrides Dict 사용 방식 개선
  - [ ] AABB 회전 시도 횟수 조정 (현재 36회 → 조절)
  - [ ] docs/기능/도면시트/ISO 도면.md 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (CreateIsoBalloonNotes)
  - `docs/기능/도면시트/ISO 도면.md`
```
