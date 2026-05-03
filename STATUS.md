# Status

> PC 간 작업 상태 동기화 파일.
> 작업 끝낼 때 `/wrapup` 입력하면 자동 갱신됨.
> 작업 시작할 때 SessionStart hook이 이 내용을 자동으로 보여줌.

## 마지막 작업
**2026-05-02 — 빌드 환경 정비 (PC 간 동기화 사고)**

데스크톱에서 빌드 실패 → 매니지드 dll 1.0.26.130 → 1.0.26.325 교체 + Interop dll 보강 → 빌드 성공.
재발 방지로 `docs/setup/build-environment.md` 가이드 + `scripts/check-build-env.ps1` 자가 진단 스크립트 신설.

세션 요약: [docs/tracking/sessions/2026-05-02-build-environment-recovery.md](./docs/tracking/sessions/2026-05-02-build-environment-recovery.md)

## 진행 중 (WIP)
- **빌드 산출물 런타임 검증 대기** — `A2Z.exe` 짧은 실행 테스트 필요. Interop dll을 다른 사본에서 가져왔기에 매니지드 1.0.26.325와 ABI 호환된다는 보장은 없음. 런타임 에러(EntryPointNotFoundException / DllNotFoundException) 시 노트북의 정상 짝을 데스크톱 a2z 루트로 가져오기

## 다음에 할 것
1. 데스크톱에서 `A2Z.exe` 실행 테스트 (모델 1개 열고 닫기)
2. 정상 동작 확인 후 `/commit`으로 환경 정비 변경사항 커밋·push
3. 노트북에서 `git pull` 후 `.\scripts\check-build-env.ps1` 실행 → 환경 일치 확인
4. (선택) `a2z/lib/` 잔재 폴더 처리 결정 — 백업 유지 vs 삭제

