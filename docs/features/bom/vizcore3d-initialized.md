---
feature_id: BOM-001
feature_name: VIZCore3D 초기화 완료
category: BOM
trigger_type: Event Callback
owner_module: Form1.BOM.cs
last_updated: 2026-04-22 (T-017 라이선스 코드 분리)
code_reference: /docs/code-reference/form1-bom.md#vizcore3d-oninitialized
---

# VIZCore3D 초기화 완료

## 1. 개요
VIZCore3D 컨트롤이 완전히 초기화된 직후 발생하는 이벤트. 라이선스 등록, 이벤트 구독, 엣지 데이터 생성 옵션 등 **앱 전체 라이프사이클의 전제 조건**을 여기서 설정한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | Event Callback |
| 입력 | `vizcore3d.OnInitializedVIZCore3D` 이벤트 |
| 위치 | 앱 기동 직후 Form1 생성자에서 구독 |

## 3. 사전 조건
- [ ] Form1 생성자에서 `VIZCore3D.NET.ModuleInitializer.Run()` 호출 완료
- [ ] `panelViewer`에 VIZCore3DControl 추가 완료
- [ ] 라이선스 서버(`127.0.0.1:8901`) 접근 가능

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 라이선스 초기화 | Form1.License | `InitializeLicense()` 위임 — `License.LicenseServer("127.0.0.1", 8901)` + 실패 시 MessageBox·return, 성공 시 30분 갱신 타이머 시작 (→ [E01], [Form1.License.cs](/docs/code-reference/form1-license.md)) |
| 2 | 2D 툴바 표시 | SDK | `vizcore3d.ToolbarDrawing2D.Visible = true` |
| 3 | 모델 트리 표시 | SDK | `vizcore3d.ModelTreeVisible = true` |
| 4 | Clash 콜백 구독 | Form1 | `Clash.OnClashTestFinishedEvent += Clash_OnClashTestFinishedEvent` |
| 5 | 선택 이벤트 구독 | Form1 | `Object3D.OnObject3DSelected += Object3D_OnObject3DSelected` |
| 6 | 엣지 데이터 활성화 | SDK | `Model.GenerateEdgeData = true`, `LoadEdgeData = true` |
| 7 | 기존 객체 엣지 생성 | SDK | `Object3D.GenerateEdgeData()` |

> 구현 상세는 [코드 레퍼런스](/docs/code-reference/form1-bom.md#vizcore3d-oninitialized) 참고

## 5. 주요 분기 처리
없음 (순차 설정만 수행)

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 라이선스 결과 != SUCCESS | `InitializeLicense()`에서 MessageBox 후 false 반환, 호출처에서 `if (!InitializeLicense()) return;`로 핸들러 전체 종료 | MessageBox "License Error: {code}" | 뷰어 사용 불가, 이후 모든 SDK 호출 실패 가능 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `vizcore3d.License` | 미등록 | 서버 등록 완료 |
| `licenseRefreshTimer` | null | Running (30분 주기) |
| `vizcore3d.ToolbarDrawing2D.Visible` | false | true |
| `vizcore3d.ModelTreeVisible` | false | true |
| `vizcore3d.Clash.OnClashTestFinishedEvent` | 구독자 0 | Form1 구독 |
| `vizcore3d.Object3D.OnObject3DSelected` | 구독자 0 | Form1 구독 |
| `vizcore3d.Model.GenerateEdgeData` | false | true |
| `vizcore3d.Model.LoadEdgeData` | false | true |

## 8. 후행 기능 (Chained)
이 이벤트 완료 이후 사용 가능:
- [모델 파일 열기](./open-model.md)
- [3D 객체 선택 이벤트](../attribute/object-selected-event.md) (이후 자동 수신)

## 9. 관련 링크
- 코드 구현: [Form1.BOM.cs `Vizcore3d_OnInitializedVIZCore3D`](/docs/code-reference/form1-bom.md#vizcore3d-oninitialized)
- 라이선스 전담: [Form1.License.cs](/docs/code-reference/form1-license.md)
- 용어집: [VIZCore3D](../../_glossary.md#vizcore3d)
- 상위 파이프라인: [전체 파이프라인](../../_pipeline.md)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
| 2026-04-22 | T-017: 라이선스 설정·갱신 타이머 로직을 `Form1.License.cs` partial로 분리. 본 핸들러는 `InitializeLicense()` 한 줄로 대체. 단계표·에러 테이블·관련 링크 갱신 | Claude |
