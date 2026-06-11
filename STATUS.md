# Status

> PC 간 작업 상태 동기화 파일.
> 작업 끝낼 때 `/wrapup` 입력하면 자동 갱신됨.
> 작업 시작할 때 SessionStart hook이 이 내용을 자동으로 보여줌.

## 마지막 작업 (PC: He0in-PC / 2026-05-22 17:20)

**Phase A 죽은 코드 청소 → Step B 가공도 통합 → C-Mfg 가공도 재설계** (당시 브랜치 `refactor/dead-code` → 2026-06-11 `main`·`HYI`에 통합 후 삭제)

이번 세션 — 사용자가 시작한 "리팩토링 4 이슈"(자동/수동 출력 차이 + 죽은 코드 + 버벅임 + 최적화)에서 출발해 가공도 엑셀 슬롯 명명까지 도달. 매우 길고 큰 세션.

### 누적 commit (이번 세션, 약 25개)
- **Phase A** (`4c69696`): 죽은 코드 청소 (-227줄)
- **hygiene + Step A 시리즈** (`6a923a3`~`5c882ca`): .gitignore, D1, E1, Step A(ApplySheetSelection), 모서리 fix, 엑셀 파일명 변경, 자동 진입부 초기화, STRU 활성화, 라벨 변경
- **Step B 가공도 1차 통합** (`22c653b`~`78d2887`): MfgViewPose 8필드 + BuildMfgSceneCore 663줄 + ExecuteMfgDrawing/RenderMfgViewForDrawing 어댑터 축소 (-1500줄)
- **C-Mfg 재설계** (`230c1a0`~`0edcbbc`): 가공도 사양 변경(T-064 폐기) → v2.1 계획서 + codex 6라운드 검토 "수렴" → 엑셀 슬롯 명명 완성

### 핵심 결정·메모 (메모리 갱신 완료)
- `feedback_isea_preservation.md`: isEA/isEA3d/isEAUse180 보존 (재활성 예정)
- `feedback_mfg_balloon_2026-05-19.md`: T-064 가공도 풍선 비활성 사양 폐기 — 수동 3D 뷰·자동 PDF 모두 풍선 표시 + 4분면 알고리즘 통일

### Codex 견제 사이클 (이 세션)
- 출력 통합 계획 v1→v2→v2.1→v3→v3.1: 5라운드 (총 16건 수정)
- 가공도 재설계 v1→v2→v2.1: 3라운드 (총 16건 수정) + "수렴" 선언

## 진행 중 (WIP)

- **가공도 페이지 배치 본진 시작 전**:
  - C-Mfg-1 (`0edcbbc`) — 엑셀 슬롯 명명 완성 (좌측 5행 BOM이름·View_1~5, 우측 8컬럼 BOM 표, 도면정보)
  - C-Mfg-2 (`230c1a0`) — helper PoC 4행 (5행 사양 갱신 필요)
- **사용자 사내 검증 대기 누적**:
  - D1 + E1 + Step A 묶음 (G1 게이트)
  - Step B 가공도 통합 (B1a~B3, ExecuteMfgDrawing·RenderMfgViewForDrawing 어댑터 동작)
  - C-Mfg-1 엑셀 슬롯 (사내 Excel에서 열어 한글 라벨 보강 + 디자인 미세 조정)

## 다음에 할 것 (새 세션 1번째)

1. **C-Mfg-2 helper 갱신** (~15분)
   - `Form1.MfgDrawing.cs`의 `SplitMfgIntoPages`: rowsPerPage 4 → **5**
   - `BuildMfgPageData` 슬롯 매핑:
     - BOM이름: Input_4~7 → **Input_5~9**
     - BOM 표 8컬럼: Input_10~129 (NO/ITEM/MATERIAL/SIZE/Q'TY/T/W/MA/FA × 15행)
     - 페이지번호: `Input_3 = "가공도 (N/M)"` 합침
2. **C-Mfg-3** (~20분) — `GetViewAreasFromExcel` 캐시 + 페이지 초기화 helper
3. **C-Mfg-4** (~30분) — **가공도 배치 본진** `RenderMfgRowToViewArea` 일반 부재 단일 뷰
4. **C-Mfg-5** (~30분) — `GenerateMfgDrawing2DAll_v2` 통합 + 진입 8단계 초기화 + PDF + 자동·수동 경로 재배선
5. **G2-a 사내 검증** — 5행 페이지 PDF + 양쪽 결과 동일 확인

## 메모

- **브랜치**: 작업 브랜치 = `HYI` (2026-06-11 정리). 기존 `refactor/dead-code`는 `main`에 통합 후 삭제 → `main` = `HYI` = `db12114` 동기화. 백업·실험 브랜치(`main-backup-2026-05-29`·`X_HYI`·`HYI-backup-T038`·`HYI-STRU`) 삭제. **앞으로 push는 HYI에만.** 원격 유지: `main`·`CJH`·`KSH`·`HYI`
- **계획서 위치**: `docs/리팩토링/도면-출력-통합.md` (v3.1), `docs/리팩토링/가공도-템플릿-설계.md` (v2.1)
- **백업 파일**: `사용자템플릿_엑셀_가공도_백업_2026-05-19.xlsx` (원본 제작도 카피), `_백업2_5행이전.xlsx` (View_1~4만 추가) — `.gitignore` 안 처리, 로컬 유지
- **남은 위험 commit** (C-Mfg-7 EA 두 뷰): codex 3차에서 "최고 위험"으로 분류, isEA 재활성 필요. C-Mfg-4·5 후 별 commit으로 격리
- **사용자가 사내에서 할 것**: 가공도 엑셀의 영문 라벨(BOM TABLE 등) 한글로 보강, 우측 BOM 표 5행 사양 vs 15행 유지 결정
- **이전 누적 작업 (이전 세션부터)**: 회사 doc 13건 + 사용자 정리 11건 + 본인 개선 11건 — 가공도 재설계 후 다시 우선순위 정리 예정
