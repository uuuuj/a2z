# 문서 작성 가이드

신규 기능(버튼·이벤트) 문서를 추가할 때 반드시 지켜야 하는 규칙과 절차입니다.

---

## 절차

1. `docs/_template.md`를 해당 카테고리 디렉터리로 복사
2. 파일명은 **핸들러 이름을 kebab-case**로 변환 (예: `btnExportPDF_Click` → `export-pdf.md`)
3. 프론트매터(YAML) 메타데이터 채우기
4. 11개 섹션(개요~변경 이력) 채우기
5. 해당 카테고리 `_index.md`에 링크 추가
6. 필요시 `code-reference/` 문서의 핸들러 앵커도 작성/업데이트
7. `_pipeline.md`에 흐름 영향이 있으면 반영

---

## 작성 규칙

| ID | 규칙 | 이유 |
|---|---|---|
| **R1** | 한 문서 = 한 트리거 | 한 버튼이 여러 흐름을 호출하면 흐름별로 문서 분리 — 로직 흐름 추적 가능성 확보 |
| **R2** | 코드 스니펫 금지. `file.cs:L123-L150` 앵커 링크만 허용 | 코드 변경 시 문서 동기화 부담 제거, 실구현은 code-reference로 |
| **R3** | Mermaid는 단계 5개 이상 또는 분기 2개 이상일 때만 | 단순 흐름은 번호 리스트가 더 명확, 유지보수 비용 절감 |
| **R4** | 상태 변화 표는 공유 필드(클래스 멤버)만. 로컬 변수 제외 | 외부 영향(부수효과) 추적이 목적 |
| **R5** | 예외 ID는 문서 내 `E01`부터 일관 번호. 다른 문서와 중복 허용 | 문서 내 참조가 명확하고, 전역 번호 관리 부담 회피 |
| **R6** | `trigger_type`은 반드시 3종 중 하나: User Action / Event Callback / Chained | 트리거 성격이 디버깅·QA에 직접 영향 |
| **R7** | `last_updated` + `변경 이력`은 PR 머지 시 필수 업데이트 | 문서 최신성 추적 |

---

## 트리거 유형 정의

| 유형 | 설명 | 예시 |
|---|---|---|
| **User Action** | 사용자가 UI를 직접 조작하여 시작 | 버튼 클릭, 더블클릭, 셀렉션 변경 |
| **Event Callback** | SDK 또는 시스템이 비동기로 호출 | `Clash_OnClashTestFinishedEvent`, `Object3D_OnObject3DSelected` |
| **Chained** | 다른 기능이 내부적으로 호출 | `GenerateSheetDrawing2D` (상위 버튼이 호출) |

---

## 예외 처리 섹션 작성 팁

- 예외를 "코드의 catch 블록"이 아니라 **"사용자가 겪을 수 있는 실패 시나리오"** 관점으로 작성
- 결과 상태가 "부분 반영"일 경우 반드시 **복구 방법**을 별도 서브섹션으로 명시
- SDK 내부 예외는 최상위 catch에서 MessageBox로 표시되므로 `E99 - 알 수 없는 SDK 오류`로 수렴 가능

---

## 상태 변화 섹션 작성 팁

- 공유 필드 = `Form1.cs`의 private/public 멤버 (bomList, clashList, vizcore3d 등)
- 로컬 변수, 임시 리스트는 기록하지 않음
- "변화 없음" 항목은 생략 가능
- VIZCore3D SDK 내부 상태 변화(RenderMode, Camera 등)는 "SDK 상태"로 분류

---

## 파일명 규칙

| 핸들러 패턴 | 파일명 예시 |
|---|---|
| `btnXxxYyy_Click` | `xxx-yyy.md` |
| `LvXxx_DoubleClick` | `lvxxx-doubleclick.md` |
| `LvXxx_SelectedIndexChanged` | `lvxxx-selected.md` |
| `Xxx_OnYyyEvent` | `yyy-event.md` 또는 `xxx-event.md` |

---

## 카테고리 결정

| 카테고리 | 소속 파일 |
|---|---|
| BOM | Form1.BOM.cs |
| Clash | Form1.Clash.cs |
| Dimensions | Form1.Dimensions.cs |
| Drawing2D | Form1.Drawing2D.cs |
| DrawingSheets | Form1.DrawingSheets.cs |
| GlobalViews | Form1.GlobalViews.cs |
| MfgDrawing | Form1.MfgDrawing.cs |
| Attribute | Form1.Attribute.cs |

> 하나의 핸들러가 여러 카테고리에 걸치는 경우, **소속 파일의 카테고리**를 우선합니다.
