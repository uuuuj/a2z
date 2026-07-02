# Status

> PC 간 작업 상태 동기화 파일.
> 작업 끝낼 때 `/wrapup` 입력하면 자동 갱신됨.
> 작업 시작할 때 SessionStart hook이 이 내용을 자동으로 보여줌.

## 마지막 작업 (2026-07-03)

**가공도 EA 스틴트 죽은 코드 정리 + 문서 동기화** (브랜치 `HYI`, 커밋 `a7031cb`)

다중 에이전트 워크플로(파인더 2 + 후보별 3-렌즈 적대 검증)로 2026-06-30~07-02 EA 작업 잔재를 탐지. 1차는 fable ultracode로 돌려 82에이전트·237만 토큰 소모 중 세션 한도 도달 → **Opus로 전환**해 만장일치 확정 **7건만** 수술적 제거(정밀도 우선). 빌드 통과(에러 0)·push 완료. 상세: `docs/tracking/sessions/2026-07-03-가공도-EA-죽은코드정리.md`.

제거: `PushMfgDimTextOutside`, `GenerateMfgDrawing2DAll`+`RenderMfgViewForDrawing`(구형 그리드 경로), `DrawDimension` mfg 텍스트 파라미터+`[DimTextOut]` 블록, `MfgViewPose.MirrorAxis`(write-only)·`RequiresPostSelectionRotation`(미배선). 보존: `ApplyParallelTextShift`(제작도 공유)·`MirrorVertical`·`ShapeDrawingIds`·`isEA`류·DiagLog.

### 직전 스틴트 (2026-06-30~07-02, 커밋 `b9daa22`~`40c60ec`)
가공도 EA 부재 가로 배치 + 두 뷰 상하 스왑·미러 + 보조선/치수/풍선 제작도 수준 정합. **사내 실기 검증 대기.** 상세: `docs/tracking/sessions/2026-06-30-가공도-EA-치수배치.md`.

## 진행 중 (WIP)

- **가공도 EA 두 뷰 사내 실기 검증 대기**: 가로화·상하 스왑·미러·보조선·치수 텍스트가 PDF에서 의도대로 나오는지. (현 PDF 경로 = `GenerateMfgDrawingManual`→`RenderMfgRowToViewArea` 일원화 완료)
- **미검증 죽은 코드 후보** (이번엔 세션 한도로 3-렌즈 검증 중단 → 보존): 다음 세션 저비용 grep 검증 후 판단. 목록은 2026-07-03 세션 파일 §이어갈 지점 참조.

## 다음에 할 것 (새 세션 1번째)

1. **가공도 EA 두 뷰 실기 검증 결과 반영** — 필요 시 변경→commit→push 사이클 반복.
2. **미검증 죽은 코드 후보 저비용 재검증** — `mfgTotalOff`, `availW`/`availH` 파라미터, `OrientationAxis`/`OrientationAngle`, `pose.CameraData` 스냅샷 등 (2026-07-03 세션 파일 목록).
3. **누적 우선순위 재정리** — 회사 doc 13건 + 사용자 정리 11건 + 본인 개선 11건 (TASKS.md).

## 메모

- **브랜치**: 작업 브랜치 = `HYI` (2026-06-11 정리). 기존 `refactor/dead-code`는 `main`에 통합 후 삭제 → `main` = `HYI` = `db12114` 동기화. 백업·실험 브랜치(`main-backup-2026-05-29`·`X_HYI`·`HYI-backup-T038`·`HYI-STRU`) 삭제. **앞으로 push는 HYI에만.** 원격 유지: `main`·`CJH`·`KSH`·`HYI`
- **계획서 위치**: `docs/리팩토링/도면-출력-통합.md` (v3.1), `docs/리팩토링/가공도-템플릿-설계.md` (v2.1)
- **백업 파일**: `사용자템플릿_엑셀_가공도_백업_2026-05-19.xlsx` (원본 제작도 카피), `_백업2_5행이전.xlsx` (View_1~4만 추가) — `.gitignore` 안 처리, 로컬 유지
- **가공도 EA 두 뷰**: 2026-06-30~07-02 스틴트에서 구현(가로화·스왑·미러). 사내 실기 검증 대기. `isEA`류 플래그는 여전히 보존(재활성 여지)
- **사용자가 사내에서 할 것**: 가공도 엑셀의 영문 라벨(BOM TABLE 등) 한글로 보강, 우측 BOM 표 5행 사양 vs 15행 유지 결정
- **이전 누적 작업 (이전 세션부터)**: 회사 doc 13건 + 사용자 정리 11건 + 본인 개선 11건 — 가공도 재설계 후 다시 우선순위 정리 예정
