---
title: 엑셀 템플릿 PoC (Step 3 — JSON 직접 파싱 + 우리 렌더)
category: drawing-sheets
handler: btnExcelTemplatePoC_Click
file: A2Z/Form1.ExcelTemplate.cs
related: REQ-002, T-012, T-057
status: PoC Step 3 (옵션 A 본진)
last_updated: 2026-05-12
---

# 엑셀 템플릿 PoC — Step 3 (`btnExcelTemplatePoC_Click`)

## 목적

REQ-002 / T-012 PoC **옵션 A 본진**. Step 1/2의 SDK 자동 적용·reflection 우회가 모두 실패함이 확정되어 (SDK dll obfuscation 보호 + internal 메서드 silent fail), **SDK가 ImportExcel로 변환·저장한 JSON을 우리가 직접 파싱하고 SDK 공개 API로 셀을 캔버스에 그린다**. 엑셀 외부 관리 가치는 그대로 유지 (사용자가 엑셀 편집 → SDK 분석 → 우리 렌더 3단계).

## SDK Reflection 분석 결과

`lib/VIZCore3D+.NET.dll` 직접 검사로 발견한 internal 멤버:

| 멤버 | 가시성 | 의미 |
|---|---|---|
| `Set2DViewDefaultTemplate(string filePath)` | INTERNAL | string 오버로드 — 사용자 추가 적용 후보 |
| `Draw2DViewTemplate(string filePath)` | INTERNAL | **핵심 후보** — "Draw 2D View Template" 이름 |
| `Draw2DViewTemplate(string, int, int)` | INTERNAL | 좌표 지정 오버로드 |
| `Draw2DViewTemplate(string, int, int, int, int)` | INTERNAL | anchor 포함 |
| `ParseJson(string json)` | INTERNAL | SDK JSON 직접 파싱 |
| `ReadJson()` | INTERNAL | 내부 JSON 읽기 |
| `get_TemplatePath()` | INTERNAL | SDK 데이터 폴더 경로 |
| `get_TemplateDic()` / `set_TemplateDic()` | INTERNAL | 템플릿 사전 |

## 흐름 (Step 3)

1. **JSON 경로 자동 검색** — `%APPDATA%\SOFTHILLS\VIZCore3D+.NET\Template\Template_0\사용자템플릿_엑셀_Rev_01.json` 우선. 없으면 가장 오래된 `Template_*` 폴더의 `*.json` 자동 선택. 그래도 없으면 `OpenFileDialog`.
2. **JSON 파싱** — `System.Web.Script.Serialization.JavaScriptSerializer` (MaxJsonLength = int.MaxValue) 로 Dictionary 파싱. Line/Text/Image 리스트 분리.
3. **InputBox 모드 선택 (Step 3는 Line만)**:
   - `1` — Line 10개만 (시각 검증, 좌표·렌더 동작 확인용)
   - `2` — Line 전체 (1539개 추정)
   - `0` — 기존 ShapeDrawing 모두 제거 (clear)
4. **Line 그리기** — JSON Line 좌표(mm)를 `Vertex3DItemCollection` 세그먼트로 변환 (Z=0 평면), 한 번에 `vizcore3d.ShapeDrawing.AddLine(allLines, -1, Color.Black, 0.3f, true)` 호출 → `shapeId` 반환.
5. **3D → 2D 캔버스 변환** — `vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(new List<int> { shapeId })`.
6. **Text는 Step 4로 미룸** — `vizcore3d.Note` 가 VIZCore3DControl에 노출 안 됨 확인. `TextDrawing.Add(Vertex3D, ...)` 3D 텍스트 또는 다른 경로 별도 탐색 필요.
7. **DiagLog + 결과 MessageBox** — Line 추가 개수, shapeId, 다음 결정 안내.

## 핵심 SDK API

| 메서드 | 가시성 | 용도 |
|---|---|---|
| `ShapeDrawing.AddLine(List<Vertex3DItemCollection>, int groupId, Color, float thickness, bool visible) → int` | PUBLIC | 3D 공간에 라인 세그먼트들 추가, ID 반환 |
| `Drawing2D.Object2D.Add2DObjectFromShapeDrawing(List<int> shapeIDs)` | PUBLIC | 3D ShapeDrawing을 2D 캔버스 객체로 변환 |
| `ShapeDrawing.Clear()` | PUBLIC | 기존 ShapeDrawing 모두 제거 |
| `TextDrawing.Add(Vertex3D center, Vector3D dir, Vector3D upDir, float height, Color, string text)` | PUBLIC | 3D 텍스트 추가 (Step 4 후보) |
| `NoteManager.AddNote2D(...)` | PUBLIC (단 VIZCore3DControl에 노출 X) | Step 4에서 별도 인스턴스 경로 탐색 필요 |

## 단계별 검증 결과

| Step | 내용 | 상태 |
|---|---|---|
| 1 | `ImportExcel` 단독 호출 — SDK "사용자 템플릿" 등록만 됨, 2D View에는 적용 X | **완료** — 설정 창 트리에 "SHI" 등장, 미리보기 정상. 메인 캔버스 비어있음 |
| 1.5 | `Set2DViewDefaultTemplate(int)` public 오버로드 호출 — 인덱스 0~5+ 시도 | **완료 (실패)** — 0/1/2 = DSME 내장만 정상, 3+ = 흰 박스+노란 박스만(빈 페이지 outline). 줌/팬/F키도 효과 X. SDK UI "확인" 적용도 동일 실패 |
| 1.6 | SDK dll reflection 메타데이터 분석 | **완료** — `Draw2DViewTemplate(string)` / `Set2DViewDefaultTemplate(string)` 모두 internal 확인. 사용자 추가는 internal API로 호출되는 게 거의 확정 |
| 2 | **internal API reflection 우회 호출** — `Draw2DViewTemplate(filePath)` | **사용자 실기 검증 대기 (사내 PC)** |
| 3 | 위 실패 시 — JSON 직접 파싱 후 우리가 Drawing2D API로 셀 그리기 (옵션 A 본진) | Step 2 결과 보고 결정 |
| 4 | 셀 영역에 `AddModel(viewIndex)` 호출 + 이미지/BOM 데이터 배치 | Step 2/3 후 진행 |

## SDK export 분석 결과 (`C:\Users\duddl\Desktop\Template`)

ImportExcel 호출 시 SDK가 생성한 데이터:
- `TemplateManagement.json` — 관리 메타. Template_0(SHI 원본), Template_1(SHI_20260512004053), Template_2(SHI_20260512010729) 매핑. `"index": "22"` 마지막 인덱스
- 각 Template 폴더에 `사용자템플릿_엑셀_Rev_01.json` (458KB) + `ExcelImage_N.png` (썸네일)
- JSON 안 인식 데이터: **Line 1539, Text 2201, Image 4** (단위 mm, X 0~355.6 / Y 0~227.3, W/H 1.565)
- Text 일부가 Line 최대값 초과(409 vs 355) — 약간의 좌표 이상 있으나 전체 구조는 완전

## 사전 확정 정보

- 사용자 엑셀 (`사용자템플릿_엑셀_Rev_01.xlsx`) 구조:
  - 단일 시트 "SHI", 55컬럼 × 40행, paperSize=8 (A3 landscape)
  - 컬럼 width 3.43, 행 ht 16.5pt
  - 비율 W/H ≈ 1.41 (A4 가로 비율 1.414에 거의 정확)
  - 4개 뷰 영역: ISO(E~T / 4~22행), LOOKING "Z"(V~AK / 4~22행), LOOKING "X"(E~T / 25~37행), LOOKING "Y"(V~AK / 25~37행)
  - BOM 영역: AN~BC / 4~18행
  - NOTE: AN19:BC21
  - 도면정보: 27~28행, REV/DATE/DESCRIPTION/DRAWN/CHECKED/APPROVED/PAINT CODE/DP NO
  - 이미지 슬롯 4개: B2:D5, AL2:AM6, AN31:AR34, AN35:AR38
  - TAG NO: AN39:BC40

- SDK API ([VIZCore3D.NET.xml:29155](../../../lib/VIZCore3D.NET.xml:29155), [:29169](../../../lib/VIZCore3D.NET.xml:29169), [:29219](../../../lib/VIZCore3D.NET.xml:29219)):
  - `Drawing2DTemplateManager.ImportExcel(string path)` — 도면 Excel 파일 불러오기 (등록만, 캔버스 적용 X)
  - `Drawing2DTemplateManager.Set2DViewDefaultTemplate(string)` — 등록된 템플릿을 2D View에 적용. xml 명시는 DSME 기본 템플릿 예시("SPA3L-AC11" 등)뿐이지만 사용자 등록 이름("SHI")도 시도 (Step 1.5)
  - `Drawing2DTemplateManager.Set2DViewDefaultTemplate(int)` — 인덱스로 적용. ("SHI") 실패 시 fallback 후보
  - `Drawing2DTemplateManager.templateDatas` — **private/internal 필드, 외부 접근 불가** (Step 1 빌드 검증으로 확정)
  - `Drawing2DObjectManager.AddModel(int viewIndex)` — Step 2 이후 사용 예정

## UI

- 위치: 도면정보 탭 / "작업" 그룹박스(groupBox1) 끝 — `btnExtractDimension` 옆
- 텍스트: "엑셀 PoC"
- 크기: 78 × 25 px

## 변경 이력

| 날짜 | 작업 | 커밋 |
|---|---|---|
| 2026-05-12 | PoC Step 1 신설 — `btnExcelTemplatePoC` + `Form1.ExcelTemplate.cs` partial class | `702ae85` |
| 2026-05-12 | Step 1 검증 후 1.5로 격상 — `Set2DViewDefaultTemplate(int)` 추가 호출 + InputBox로 인덱스 입력 (string 오버로드 외부 호출 불가 확인). 사용자 사내 PC에서 Step 1 결과: 설정 트리 등장만, 캔버스 빔 확인. csproj `Microsoft.VisualBasic` 참조 추가 | `af9fbd9` |
| 2026-05-12 | Step 1.5 검증 후 Step 2로 격상 — int 오버로드 0~5+ 모두 실패(0/1/2=DSME 정상, 3+=빈 outline만). SDK dll reflection으로 internal `Draw2DViewTemplate(string)` / `Set2DViewDefaultTemplate(string)` 존재 확인. Reflection 우회 호출 PoC. InputBox로 filePath 후보 입력 + TemplatePath 진단 로그 | `a7ab4c4` |
| 2026-05-12 | Step 2 검증 후 Step 3로 격상 — reflection 호출 모두 "성공"이지만 캔버스 비어있음(silent fail). SDK dll obfuscation 보호 확인. **옵션 A 본진 진입**: JSON 직접 파싱 + ShapeDrawing.AddLine + Add2DObjectFromShapeDrawing + Note.AddNote2D 로 우리가 렌더. csproj `System.Web.Extensions` 참조 추가 | (이번 커밋) |
