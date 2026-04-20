# 본인 수정 요청 (REQUESTS)

개발자 **본인**이 생각한 수정/개선 아이디어를 기록합니다. 담당자 피드백과 구분하여 결정 맥락·우선순위를 명확히 보관.

> **상태 값**: `OPEN` (아이디어 접수) / `IN_REVIEW` (고려 중) / `ACCEPTED` (작업 결정, TASKS.md로 이동) / `REJECTED` (기각) / `DONE` (완료)

> **우선순위**: `HIGH` (긴급/필수) / `MEDIUM` (중요하지만 대기 가능) / `LOW` (여유될 때)

---

## 접수 대기 / 진행 중

### REQ-002 — 2D 도면 템플릿의 엑셀 외부화 (하이브리드)
- **생성일**: 2026-04-20
- **상태**: ACCEPTED
- **우선순위**: MEDIUM
- **배경**:
  현재 `tableInfo`(로고·프로젝트명·제작자)와 `BOM` 테이블 헤더/열너비/스타일이 `Form1.DrawingSheets.cs`에 하드코딩. 회사·프로젝트·고객별로 양식이 다르고, 변경 시 재빌드·배포가 필요해 담당자가 직접 수정 불가. 과거 Phase 18(`790a02a`)에서 한 번 엑셀 기반→수동 구성으로 되돌린 이력은 **BOM의 동적 행수 문제** 때문. tableInfo는 정적이라 문제 없음
- **기대효과**:
  - 담당자가 엑셀 파일에서 양식(로고·헤더·테두리·폰트) 직접 편집 → 재빌드 없이 반영
  - 프로젝트별 템플릿 파일 스위칭 (사내 표준 / 고객 A / 고객 B …)
  - SDK가 `ImportExcelWithData(path, Dict)`와 `Draw2DViewTemplate(path, x, y, w, h)`를 공식 제공하므로 라이브러리 추가 없음 (Aspose.Cells는 이미 포함)
  - 현재 4분할 뷰 + 우측 BOM/tableInfo 구조는 **하이브리드로 유지** (시나리오 2)
- **관련 기능**:
  - [2D 출력](../사용자-매뉴얼/4.도면정보%20탭/2D%20출력.md)
  - 개발자 문서: [GenerateSheetDrawing2D](../features/drawing-sheets/generate-sheet-2d.md)
- **분해된 작업**: T-012 (PoC 실험) → 결과에 따라 Phase B1(tableInfo)·B2(BOM 스타일) 후속

---

## 완료 (DONE)

### REQ-001 — 실사용자(담당자)가 읽을 수 있는 사용자 매뉴얼 작성
- **생성일**: 2026-04-14
- **완료일**: 2026-04-14
- **상태**: DONE
- **우선순위**: HIGH
- **배경**:
  기존 `docs/features/*`는 개발자용 로직 문서라 실 담당자가 "도면 생성 버튼은 어떤 버튼이야?" 질문에 답하기 어려웠음. 파일명(`generate-2d.md`)·본문 용어(`RenderMode=DASH_LINE`)가 비개발자에게 난해.
- **기대효과**:
  실제 UI 라벨로 폴더·파일명을 구성한 사용자 매뉴얼 신설. 담당자가 앱 UI를 보며 즉시 문서를 찾고 로직을 이해 가능.
- **분해된 작업**: T-003

---

## 기각 (REJECTED)

_이력 없음_

---

## 형식 예시

```
### REQ-001 — PDF 저장 경로 설정 가능하게
- **생성일**: 2026-04-14
- **상태**: OPEN
- **우선순위**: MEDIUM
- **배경**:
  현재 `btnExportAllPDF`가 `c:\` 루트에 하드코딩되어 저장됨.
  사용자마다 원하는 저장 폴더가 다르고, 버전 관리도 어려움.
- **기대효과**:
  - 저장 경로 설정 UI 또는 App.config 옵션
  - 마지막 사용한 경로 기억
  - 프로젝트별 분리된 디렉터리 구조 가능
- **관련 기능**:
  - [PDF 배치 내보내기](../features/drawing-sheets/export-all-pdf.md)
  - [단일 PDF](../features/drawing2d/export-pdf.md)
- **분해된 작업**: (ACCEPTED 전환 시 기록) T-xxx, T-yyy
```

---

## FEEDBACK.md와의 차이

| 항목 | FEEDBACK.md | REQUESTS.md |
|---|---|---|
| 출처 | 담당자 (외부) | 본인 (내부) |
| 핵심 | **원문 보존** (뉘앙스 유실 방지) | **결정 맥락** (왜/언제/우선순위) |
| ID | `FB-xxx` | `REQ-xxx` |
| TASK 연결 | `T-xxx (from FB-xxx)` | `T-xxx (from REQ-xxx)` |
