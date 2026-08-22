# 1회성 마이그레이션 스크립트 (실행 완료)

2026-07-28 `docs/tracking` → GitHub 이슈 전환 때 쓰고 끝난 것들이다. **다시 돌릴 일 없다.**
지우지 않고 남긴 이유는 당시 변환 규칙(무엇을 무엇으로 옮겼는지)이 여기에만 남아 있어서다.

| 스크립트 | 무엇을 했나 |
|---|---|
| `migrate_tracking.py` | tracking 문서 → GitHub 이슈 72건 생성 |
| `migrate_changelog.py` | CHANGELOG 231건 → 이슈 코멘트 300건 |
| `backfill_issues.py` | 누락분 소급 등록 |

현재 쓰는 것은 `scripts/build_status_xlsx.py` 하나 — 이슈에서 `개발 현황.xlsx` 생성.
