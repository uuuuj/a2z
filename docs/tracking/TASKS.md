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

_진행 중 작업 없음_

---

## BLOCKED

_차단된 작업 없음_

---

## DONE (최근 20개)

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
