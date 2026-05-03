# Status

> PC 간 작업 상태 동기화 파일.
> 작업 끝낼 때 `/wrapup` 입력하면 자동 갱신됨.
> 작업 시작할 때 SessionStart hook이 이 내용을 자동으로 보여줌.

## 마지막 작업
**2026-05-02 — 회사 doc 동기화 잔여 4건 (T-046 확장 + T-053 + T-055/T-056)**

- **T-046 확장** (긴급상 10 + 사용자 확장) — **모든** 치수 보조선을 `DASHED_DOUBLEDOTTED` → `SOLID`(가는 실선) + 모델 표면에서 **10mm gap** (사용자 실기 후 1mm → 10mm 상향). `DrawDimension` 단일 지점에 헬퍼(`OffsetTowardLineEnd`) + 상수(`ExtensionLineGap = 10.0f`) 추가로 가공도/일반시트/글로벌/치수추출 4경로 자동 적용. 가공도 LineType 토글 2곳 단순화(`Form1.MfgDrawing.cs:1542, 1900`) → 통합 사양 `docs/technical-notes/dimension-extension-line.md`
- **T-053** (긴급하 4) — 중복 Sheet 삭제 후 `SheetNumber` 1부터 전체 재채번 (`Form1.DrawingSheets.cs:215~221`)
- **T-055** (회사 완료 3 의문 답변) — Osnap 기준점 코드 동작 검증 보고서. 결론: **부분 일치** (코너 의도는 구현, 부재 단위 4코너는 1점만 남김) → `docs/technical-notes/osnap-criteria.md`
- **T-056** (완료 5 + 수정 후 확인 2) — Sheet1 부재 이름 Z-MAX 정렬 검증 보고서. 결론: **부분 일치** (BBox.MaxZ vs Osnap.Z 차이, 일반 철골 형상에선 동등) → `docs/technical-notes/sheet1-naming-criteria.md`
- 직전 세션 산출물 보존: `docs/setup/build-environment.md`, `scripts/check-build-env.ps1`

## 진행 중 (WIP)
- **빌드 산출물 런타임 검증 대기** (이전 세션 이월) — `A2Z.exe` 짧은 실행 테스트 필요. Interop dll ABI 호환 미검증
- **회사 doc 답변 송부 대기** — T-055/T-056 검증 보고서를 회사에 회신 후 결과에 따라 후속 조치 (Osnap 기준 변경 vs 현행 유지)

## 다음에 할 것
1. 데스크톱에서 `A2Z.exe` 실행 테스트 (모델 1개 열고 닫기)
2. 정상 동작 확인 후 `/commit`으로 변경사항 커밋·push (T-053/T-055/T-056 + 직전 빌드 환경 정비 묶음)
3. 노트북에서 `git pull` 후 `.\scripts\check-build-env.ps1` 실행 → 환경 일치 확인
4. 회사에 T-055/T-056 검증 결과 회신 → 회사 답변 받으면 후속 작업 (Osnap 기준 변경 필요시 신규 T-057 등록)
5. 다음 진행 후보: T-046 (보조선 LineType, 긴급상) / T-052 (Sheet1 포함부재 표기, 긴급하) / T-049 (치수 백엔드 문서화, 긴급중)
6. (선택) `a2z/lib/` 잔재 폴더 처리 결정

