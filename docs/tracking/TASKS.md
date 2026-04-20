# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

### T-004 — ALL 출력 후 시트별 도면 즉시 미리보기
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-001
- **세부**:
  - [ ] ALL 일괄 출력이 만든 PDF 파일 경로를 시트별로 매핑(DrawingSheetData에 저장 or 별도 Dict)
  - [ ] `LvDrawingSheet_SelectedIndexChanged`에서 해당 시트의 저장된 PDF가 있으면 2D 뷰에 로드·표시
  - [ ] PDF가 없는 시트는 기존 동작(X-Ray + 치수) 유지
  - [ ] docs/features/drawing-sheets/lv-sheet-selected.md + export-all-pdf.md 동기화
  - [ ] 사용자-매뉴얼/5.목록 조작/시트 선택 시 화면 전환.md + 4.도면정보 탭/ALL 일괄 출력.md 동기화
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (LvDrawingSheet_SelectedIndexChanged, btnExportAllPDF_Click)
  - `A2Z/Models.cs` (DrawingSheetData에 PdfPath 필드 추가 가능)

### T-005 — 치수 배치를 Osnap 외곽 방향으로
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-002
- **세부**:
  - [ ] 각 체인 치수의 "바깥 방향" 판정 로직 구현 (Osnap 무게중심 반대 방향)
  - [ ] `ShowAllDimensions` 및 `btnDimensionShowSelected_Click`의 축별 오프셋을 외곽 방향으로 변경
  - [ ] 기존 축별 오프셋(50.0f 고정)을 부재 BBox 근처로 조정
  - [ ] docs/features/dimensions/show-selected.md + main-dimension.md 동기화
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (btnDimensionShowSelected_Click L17, ShowAllDimensions)
  - `A2Z/Form1.BOM.cs` (btnMainDimension_Click 내부 AddChainDimensionByAxis 영향 가능)

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정
- **생성일**: 2026-04-15
- **상태**: TODO
- **관련**: FB-003
- **세부**:
  - [ ] 현재 `GenerateSheetDrawing2D`의 2x3 그리드(ISO+3축 / 라벨) 셀 크기 상수화
  - [ ] BOM 테이블(table1) · 도면정보 테이블(tableInfo)의 폭·높이 고정값 정의
  - [ ] A4 297x210 기준 영역 레이아웃 스펙 명시
  - [ ] docs/features/drawing-sheets/generate-sheet-2d.md 동기화
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L957+)

### T-007 — 뷰 내부 모델 최대화 + 라벨·풍선 영역 확보
- **생성일**: 2026-04-15
- **상태**: BLOCKED (T-006 선행 권장)
- **관련**: FB-004
- **세부**:
  - [ ] T-006 완료 후 각 뷰 셀의 "모델 배치 가능 영역"(라벨·풍선 제외) 좌표 계산
  - [ ] `RenderSheetViewForDrawing`의 targetHeight(현재 40f) → 셀 최대 크기 동적 계산으로 변경
  - [ ] 풍선 예약 영역(상단 또는 측면) 일정 크기 확보
  - [ ] ISO/X/Y/Z 라벨 하단 위치 고정
  - [ ] docs/features/drawing-sheets/generate-sheet-2d.md 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (RenderSheetViewForDrawing, CreateIsoBalloonNotes)
- **참고**: FB-003·004는 같은 2D 도면 레이아웃. T-006 구조 확정 후 T-007 진행하면 수정 충돌 최소화

---

## IN_PROGRESS

### T-009 — 초기화 버튼 누락 항목 보강
- **생성일**: 2026-04-20
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS
- **관련**: T-008 후속 (사용자 피드백)
- **배경**: 초기화 버튼 실기 테스트 결과, 도면정보 탭 BOM 목록·3D 렌더모드(DASH_LINE)·2D 캔버스가 남아 있는 상태 발견
- **세부**:
  - [x] Form1.BOM.cs `ResetToInitialState()` 정리 블록에 3줄 추가
    - `lvDrawingBOMInfo.Items.Clear()` (도면정보 탭 BOM)
    - `vizcore3d.View.SetRenderMode(RenderModes.SMOOTH)` (DASH_LINE 해제, 기본 모드 복귀)
    - `Clear2DView()` (2D 캔버스 정리)
  - [x] docs/features/bom/reset-to-initial.md — 단계 3 설명/상태 변화 섹션/변경 이력 갱신
  - [x] **후속**: `Clear2DView()` 호출 시점을 `Model.Open` 성공 이후로 이동 (SDK가 Open 시 2D 뷰 자동 복원하는 이슈, 4번 번쩍임 해결)
  - [ ] 빌드 + 다른 기기 실기 테스트 (사용자, 푸시 후)
- **영향 파일**:
  - `A2Z/Form1.BOM.cs` (ResetToInitialState)
  - `docs/features/bom/reset-to-initial.md`
- **SDK 참고**: `RenderModes.SOLID`는 존재하지 않음. 기본 실선 모드는 `SMOOTH` ([VIZCore3D.NET.xml:4534](../../VIZCore3D.NET.xml))

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정
- **생성일**: 2026-04-15
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS
- **관련**: FB-003
- **확정 스펙** (옵션 A):
  - A4 가로 297×210 / 마진 10 / 그리드 2×3 (셀 ≈ 92.3×95 mm) — 현재 유지
  - 뷰 4개: (1,1)ISO / (1,2)Z / (2,1)Y / (2,2)X — 현재 유지
  - **BOM → (1,3) 셀 이관** (`RenderTemplateOnGridStructure(table1, 1, 3)`)
  - **tableInfo → (2,3) 셀 이관** (`RenderTemplateOnGridStructure(tableInfo, 2, 3)`)
  - BOM 열 너비 합 82 → **92 mm로 조정** (ITEM 28→38, 그 외 유지)
  - BOM 최대 데이터 행 **14행**, 초과 시 마지막 행에 "…" + "+N건 생략" 표시 (옵션 2-a)
  - Anchor/X/Y 절대좌표 제거 → 셀 정렬(`SetGridCell*Alignment`)로 대체
  - 하드캡 30mm는 이번 범위 밖 → T-007에서 처리
- **세부**:
  - [x] Form1.DrawingSheets.cs L1020~1080 수정 — bInfo 절대좌표 제거, BOM/tableInfo `RenderTemplateOnGridStructure` 이관
  - [x] BOM `BOM_MAX_DATA_ROWS = 14` 상수 + "…+N건 생략" 행 렌더링
  - [x] BOM 열 너비 2차 축소: ITEM 28→38→17, MATERIAL/SIZE 8→12→11 (합 82→92→81→**77mm**)
  - [x] tableInfo 2차 축소: 60→57→47, 35→35→30 (합 95→92→81→**77mm**)
  - [x] 셀 정렬: BOM (1,3) Top/Center, tableInfo (2,3) Bottom/Center
  - [x] docs/features/drawing-sheets/generate-sheet-2d.md 갱신 (단계표 7~9 추가, 분기 C 추가, 변경 이력 3건)
  - [ ] 빌드 + 다른 기기 실기 테스트 (사용자, 푸시 후)
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L1020~1080, 약 +18줄)
  - `docs/features/drawing-sheets/generate-sheet-2d.md`

---

## BLOCKED

_차단된 작업 없음_

---

## DONE (최근 20개)

### T-011 — 시드 서브에이전트 2개 도입 (sdk-verifier, md-link-checker)
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백, 반복 실수 방지)
- **커밋**: `pending`
- **요약**:
  - 이번 대화에서 드러난 반복 실수 (`RenderModes.SOLID` 가정, `Model.Close` 누락, 링크 공백 133건 등) 방지용 시드 에이전트 2개 신설
  - `.claude/agents/sdk-verifier.md` — `VIZCore3D.NET.xml` 선행 검색으로 API 존재·시그니처·공식 예제 패턴 반환. SDK 새 멤버 처음 쓸 때 호출
  - `.claude/agents/md-link-checker.md` — `docs/**/*.md` 링크 공백·파일 부재 검증 + Python 치환 스크립트 제안. 대량 문서 수정 후 호출
  - `CLAUDE.md` R10, R11 추가 — 각 에이전트 호출 트리거 주소
  - **제외**: 오케스트레이터 프로토콜(동적 에이전트 생성·합병·삭제)은 사용 패턴 축적 후 재평가. 현 프로젝트 규모에 오버 엔지니어링 우려
  - "중간" 도입 경로 채택 (사용자 합의)

### T-010 — 문서 내부 링크 공백 문제 일괄 수정
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백)
- **커밋**: `10c7d8c`
- **요약**:
  - `docs/**/*.md` 전체 마크다운 링크 `]( ... )` 내부 공백을 **`%20`**으로 일괄 치환 (Python 스크립트)
  - **30파일, 147건 치환**. 상위: `사용자-매뉴얼/README.md`(44), `FEEDBACK.md`(8), 글로벌뷰 시리즈(6~7)
  - 외부 URL(`http://`, `https://`, `mailto:`, `#`로 시작)과 공백 없는 링크는 제외 처리
  - 대안(파일명 공백 제거 / `<path>` 각괄호)은 가독성·호환성 이유로 기각
  - 사용자 샘플 확인 통과

### T-008 — 초기화 버튼 + 같은 파일 재Open 버그 수정
- **완료일**: 2026-04-20
- **관련**: —  (FB/REQ 없음, 사용자 직접 지시)
- **커밋**: `45d17dd`
- **요약**:
  - 3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽에 `btnResetToInitial` ("초기화", 회색) 신설
  - `ResetToInitialState()` 헬퍼 — 누적 상태(List 9종 + UI ListView 5종 + SDK Clear 3종) 전면 초기화 후 동일 파일 재로드
  - `balloonOverrides.Clear()` 포함 (btnOpen이 누락했던 항목)
  - 확인 다이얼로그 + 가드 체크(`currentFilePath` + `Model.IsOpen`)
  - **버그 수정**: VIZCore3D는 같은 경로 중복 `Model.Open()`을 거부 → `Model.Open` 전 `if (IsOpen()) Close();` 패턴 적용 (공식 예제 L47297/L60261)
  - **btnOpen_Click 동반 수정**: 같은 파일 재선택 시 동일 버그 발생 소지 → 같은 패턴 적용
  - **UI 너비 축소**: 5개 버튼 Size 105→80, Location 재배치 (8/93/178/263/348), 패널 Size 558→438
  - 문서: `docs/features/bom/reset-to-initial.md` 신설 (BOM-005), `docs/사용자-매뉴얼/1.기본-작업/초기화.md` 신설, `open-model.md`에 Close 단계 추가, `_index.md`·`code-reference/form1-bom.md`·`사용자-매뉴얼/README.md` 갱신
  - 사용자 실기 테스트 통과 (부재 일부 숨기고 치수 추출 → 초기화 → 정상 복원)

### T-003 — 사용자 매뉴얼 전면 작성 (39개 버튼 문서)
- **완료일**: 2026-04-14
- **관련**: REQ-001
- **커밋**: `74fe209`
- **요약**:
  - `docs/사용자-매뉴얼/` 신규 폴더 + 39개 버튼 문서 + README
  - 실제 UI 라벨 기반 폴더·파일명 (`2.작업-데이터 탭/2D 생성.md` 등)
  - 7섹션 표준 템플릿 (요약/위치/사전조건/순서/분기/에러/이어지는작업)
  - SDK 용어 → 사용자 언어 번역 (에러는 실제 팝업 문구 그대로)
  - 멀티 에이전트 협업 (인벤토리 W-D → Writer W-A/B/C 병렬 → Reviewer 전수검사)
  - Reviewer 통과: 템플릿 0위반, 용어 0위반, 깨진 링크 0, 에러 메시지 일치
  - `docs/README.md` 상단에 개발자/사용자 분기 카드 추가
  - 개발자 문서(`docs/features/`) 영향 없음

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
