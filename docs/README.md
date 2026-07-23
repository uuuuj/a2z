# A2Z-HYI 문서

VIZCore3D.NET SDK 기반 3D→2D 도면 자동화 WinForms 앱의 **로직 흐름 문서**입니다.

## 누구를 위한 문서인가요?

| 당신이 이런 분이라면 | 여기부터 읽으세요 |
|---|---|
| **담당자·실제 사용자** — "이 버튼 누르면 뭐가 일어나지?" | **[사용자 매뉴얼](./사용자-매뉴얼/README.md)** |
| **개발자·유지보수자** — 코드 흐름·구현 세부 | 아래 [기능 카테고리](#기능-카테고리) 및 [코드 레퍼런스](#코드-레퍼런스) |
| **사양·기준값**을 찾는 분 — 보조선·치수·Osnap 규칙 | 아래 [기준·사양](#기준사양) |

---

> 이 문서는 "코드가 어떻게 생겼는지"가 아니라 **"사용자가 버튼을 눌렀을 때 무슨 순서로 무엇이 일어나는지"** 를 설명합니다. 코드 레벨 설명은 [code-reference/](./code-reference/)를 참고하세요.

---

## 문서 내비게이션

### 개괄
- [End-to-End 파이프라인](./_pipeline.md) — 모델 로드부터 PDF 출력까지 전체 흐름
- [용어집](./_glossary.md) — UDA, Osnap, BOM, Clash, Hidden Line 등
- [문서 작성 가이드](./_authoring-guide.md) — 신규 기능 문서 작성 규칙 (R1~R7)
- [빈 템플릿](./_template.md) — 새 핸들러 문서 생성 시 복사
- [빌드 환경 셋업](./setup/build-environment.md) — 신규 PC 구성 / vendor dll 의존성 / 빌드 실패 트러블슈팅

### 기준·사양

**보조선 굵기·길이, 치수 위치, Osnap 추출 임계값** 등 "도면이 왜 이렇게 그려지는가"의 근거를 모은 곳입니다. 코드 안 상수가 아니라 **사양·결정 이력**을 찾을 때 우선 들르세요.

| 문서 | 다루는 내용 |
|---|---|
| [치수 보조선 사양](./기술%20노트/치수%20보조선%20사양.md) | 보조선 선 종류·gap·굵기·길이 통합 사양 (가공도·일반 시트·글로벌·치수 추출 4경로) |
| [치수 텍스트 위치](./기술%20노트/치수%20텍스트%20위치.md) | 치수 텍스트 정렬·시프트 규칙 (`AlignDistanceTextPosition`, 평행 시프트) |
| [Osnap 기준](./기술%20노트/Osnap%20기준.md) | Osnap 추출 임계값·필터링·축별 분기 |
| [Sheet1 명명 기준](./기술%20노트/Sheet1%20명명%20기준.md) | Sheet1 기준부재 이름 부여 규칙 (Z-MAX 정렬 등) |

### 개발 현황 추적
- [tracking/](./tracking/README.md) — FEEDBACK / REQUESTS / TASKS / CHANGELOG / sessions 5축 관리
- [FEEDBACK.md](./tracking/FEEDBACK.md) — 담당자 피드백 inbox (FB-xxx)
- [REQUESTS.md](./tracking/REQUESTS.md) — 본인 수정 요청 inbox (REQ-xxx)
- [TASKS.md](./tracking/TASKS.md) — 현재 할 일 (TODO/IN_PROGRESS/DONE)
- [CHANGELOG.md](./tracking/CHANGELOG.md) — 완료 이력 (날짜 역순)
- [sessions/](./tracking/sessions/) — 세션별 작업 요약 (`/checkpoint`로 저장)
- Claude Code 작업 규칙: 루트의 [CLAUDE.md](../CLAUDE.md)

### 기능 카테고리

버튼·이벤트 단위 흐름 문서. **"이 버튼 누르면 코드 어디로 가나"** 찾을 때.

| 카테고리 | 주요 역할 | 소속 파일 | 링크 |
|---|---|---|---|
| BOM | 부재 목록 수집, UDA 파싱, 홀 감지 | Form1.BOM.cs | [기능/BOM/](./기능/BOM/_인덱스.md) |
| 간섭검사 | 3D 간섭 검사, 결과 그룹화 | Form1.Clash.cs | [기능/간섭검사/](./기능/간섭검사/_인덱스.md) |
| 치수 | 체인 치수 계산·표시 | Form1.Dimensions.cs | [기능/치수/](./기능/치수/_인덱스.md) |
| 2D 도면 | 2D 도면 생성, PDF 출력, Osnap 관리 | Form1.Drawing2D.cs | [기능/2D도면/](./기능/2D도면/_인덱스.md) |
| 도면 시트 | 도면 시트 자동 분할 (BFS) | Form1.DrawingSheets.cs | [기능/도면시트/](./기능/도면시트/_인덱스.md) |
| 글로벌 뷰 | 글로벌 뷰 전환 (ISO/X/Y/Z) | Form1.GlobalViews.cs | [기능/글로벌뷰/](./기능/글로벌뷰/_인덱스.md) |
| 가공도 | 단일 부재 가공도 | Form1.MfgDrawing.cs | [기능/가공도/](./기능/가공도/_인덱스.md) |
| 부재 속성 | 부재 속성 조회·UDA CRUD | Form1.Attribute.cs | [기능/부재속성/](./기능/부재속성/_인덱스.md) |

### 코드 레퍼런스

코드 파일 1:1 매핑. **파일명 영어 유지** (`Form1.BOM.cs` ↔ `form1-bom.md` 그대로 매칭).

| 파일 | 링크 |
|---|---|
| Form1.BOM.cs | [code-reference/form1-bom.md](./code-reference/form1-bom.md) |
| Form1.Clash.cs | [code-reference/form1-clash.md](./code-reference/form1-clash.md) |
| Form1.Dimensions.cs | [code-reference/form1-dimensions.md](./code-reference/form1-dimensions.md) |
| Form1.Drawing2D.cs | [code-reference/form1-drawing2d.md](./code-reference/form1-drawing2d.md) |
| Form1.DrawingSheets.cs | [code-reference/form1-drawing-sheets.md](./code-reference/form1-drawing-sheets.md) |
| Form1.GlobalViews.cs | [code-reference/form1-global-views.md](./code-reference/form1-global-views.md) |
| Form1.MfgDrawing.cs | [code-reference/form1-mfg-drawing.md](./code-reference/form1-mfg-drawing.md) |
| Form1.Stru.cs | [code-reference/form1-stru.md](./code-reference/form1-stru.md) |
| Form1.Attribute.cs | [code-reference/form1-attribute.md](./code-reference/form1-attribute.md) |
| Models.cs | [code-reference/models.md](./code-reference/models.md) |

---

## 프로젝트 기본 정보

| 항목 | 값 |
|---|---|
| 언어/프레임워크 | C# / .NET Framework 4.8 |
| UI | Windows Forms |
| 핵심 SDK | VIZCore3D.NET v1.0.26.325 |
| 진입점 | `A2Z/Program.cs` → `Form1` |
| 솔루션 파일 | `A2Z.sln` |

## 목적

철골 구조물/플랜트 부재의 3D CAD 모델(.vizx/.viz)로부터 제조용 2D 도면(가공도·설치도)을 자동 생성하여 수작업 도면 작성을 제거합니다.
