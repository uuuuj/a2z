---
feature_id: SHT-003
feature_name: 시트 ISO 뷰 + 풍선 노트
category: DrawingSheets
trigger_type: User Action
owner_module: Form1.DrawingSheets.cs
last_updated: 2026-08-05 (#63 연결 서포트 전체 표시)
code_reference: /docs/code-reference/form1-drawing-sheets.md#btnDrawingISO_Click
---

# 시트 ISO 뷰 + 풍선 노트

## 1. 개요
선택된 도면 시트를 ISO 방향으로 보여주고 ISO 전용 풍선 노트(`CreateIsoBalloonNotes`)만 생성한다. 설치도 3D 미리보기는 선택 STRU와 연결 서포트 STRU 전체를 함께 표시하되, 화면 맞춤은 선택 STRU 기준이라 카메라에 안 들어오는 연결부재는 잘린다 (#63). 설치 위치 치수 데이터는 X/Y/Z와 2D 도면용으로 유지하지만 ISO 3D 화면에는 그리지 않는다. PDF의 ISO 부재번호 풍선과 연결부재 이름 라벨은 2D 변환 후 실제 표시 객체 외곽에서 도면 기준 20mm 떨어진 영역으로 함께 자동 정렬한다. 설치도 PDF는 선택 STRU를 기준으로 맞추고 연결 서포트 STRU 전체를 점선으로 남기며(#63, CropFit이 선택 STRU ± 여백만 남기므로 밖은 절단), 연결 Part당 `STRU` 이름(연결부재의 STRU 단위까지만, 접합 기호 `A.`·`/ Part` 미표기)을 접합측 실제 모서리에 한 번 표시한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnDrawingISO` 클릭 |
| 위치 | 메인 폼 > 도면 시트 탭 |

## 3. 사전 조건
- [ ] 도면 시트 선택됨

## 4. 전체 동작 흐름
| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 공용 호출 | Form1 | `ApplyDrawingSheetView("ISO")` |

### ApplyDrawingSheetView("ISO") 내부
1. 시트 선택 확인 → [E01]
2. `Clear3DDimensionAnnotations()`로 직전 X/Y/Z의 `Review.Measure`·`ShapeDrawing` 제거
3. X-Ray 활성화 + 선택 STRU 부재 Select
4. 표시 대상 구성: 일반=`MemberIndices`, 설치도=`MemberIndices + InstallationContextIndices`(연결 서포트 STRU 전체). 카메라 맞춤은 설치도도 `MemberIndices` 기준 (#63)
5. 표시 대상 Show + `xraySelectedNodeIndices` 갱신 + 전체 대상 fit
6. 심볼 제거
7. 설치도이면 `ExtractInstallationDimensions(sheet)`로 X/Y/Z·2D용 준비 치수 데이터 적용
8. `SetRenderMode(SMOOTH)`
9. `MoveCamera(ISO_PLUS)` + 표시 대상 전체 `FlyToObject3d`
10. 풍선 Clear → `CreateIsoBalloonNotes(members)`

## 5. 주요 분기 처리
| 조건 | 처리 |
|---|---|
| 일반/제작/조립 시트 | `MemberIndices`만 표시하고 기존 풍선 생성 |
| 설치도 | 선택 STRU와 연결 서포트 STRU 전체를 표시하고 끝단→모서리 위치 치수 데이터만 준비. ISO 3D 화면은 선택 STRU 풍선만, PDF는 연결 Part당 이름 1개 표시 |
| PDF ISO 리뷰 라벨 | 부재번호 풍선과 연결 이름을 모두 3D 표면 노트에서 2D로 변환한 뒤 `Set2DViewAlignAreaReviewsPositionByOffset`으로 함께 정렬. 실제 2D 객체 AABB를 도면 기준 20mm 확장한 영역을 사용하고 SDK 추가 offset은 0. 지시 대상점은 유지하고 라벨만 모델 밖으로 이동. 각 라벨이 상하좌우 중 어느 쪽으로 갈지는 SDK 내부 자동 배치이며 공개된 방향 지정 옵션·우선순위 규칙은 없음 |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 시트 미선택 | return | MessageBox "도면 시트를 선택해주세요." | 변화 없음 |
| E02 | 예외 | catch | MessageBox "도면 시트 뷰 표시 중 오류: {msg}" | 부분 표시 |

## 7. 상태 변화
| 대상 | Before | After |
|---|---|---|
| 카메라 | 이전 | `ISO_PLUS` |
| `XRay.Enable` | 이전 | true |
| `xraySelectedNodeIndices` | 이전 | 실제 표시 대상. **설치도는 선택 STRU만** — 치수 baseline·배율 기준이라 연결 서포트를 넣으면 보조선이 길어진다 (#63) |
| `chainDimensionList` | 이전 | 설치도일 때 Target Body 끝단→Connected Body 모서리 연결 거리만 적용 |
| `Review.Note` | 이전 | 풍선 노트 |
| `Review.Measure` / `ShapeDrawing` | 이전 X/Y/Z 치수 | 비움(ISO에는 치수 없음) |
| RenderMode | 이전 | SMOOTH |

## 8. 후행 기능 (Chained)
- [시트 2D 생성](./시트%202D%20렌더.md)
- 다른 축 뷰로 전환

## 9. 관련 링크
- 코드 구현: [Form1.DrawingSheets.cs](../../code-reference/form1-drawing-sheets.md#btnDrawingISO_Click)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-07-23 | 연결부재 이름 라벨 폰트를 7f → 10.5f로 부재번호 풍선(BOM 숫자)과 동일하게 (#44). 연결 노트는 원형 스냅박스 없는 텍스트라 별도 원 크기 변경 없음 | Claude |
| 2026-08-05 | #63: 설치도 점선 표시 대상을 접합 Part → 연결 서포트 STRU 전체로 확대. 배율·화면 맞춤 기준은 선택 STRU 고정 | Claude |
| 2026-07-23 | 설치도 연결 이름에서 접합 기호 `A.` 접두사 제거 — STRU 이름만 표시 (#45) | Claude |
| 2026-07-23 | 연결부재 이름을 가장 가까운 상위 어셈블리 → **STRU 단위**로 변경(`FindParentStru`)하고 설치도 라벨의 `/ Part` 제거 (#45). STRU 조상 없으면 기존 어셈블리 폴백 | Claude |
| 2026-07-23 | 부재번호 풍선·연결 이름의 실제 2D 객체 외곽 간격을 20mm로 확대. 개별 연결 이름의 상/하 방향 강제는 SDK 직접 지원이 없어 별도 향후 요청으로 분리하고 현재 자동 정렬 유지 | Codex |
| 2026-07-23 | 사용자 조정에 따라 실제 2D 객체 외곽과 풍선·연결 이름 사이의 도면 고정 간격을 5mm에서 10mm로 확대. SDK 영역 정렬 offset은 0 유지 | Codex |
| 2026-07-23 | 실기 이미지에서 fit 70% 사각형 때문에 풍선이 멀어지고 월드 좌표 직접 생성 연결 이름이 이동하지 않는 현상 확인. 실제 2D 객체 외곽 + 도면 고정 5mm로 변경하고 연결 이름도 3D 표면 노트 변환 경로로 통일 | Codex |
| 2026-07-23 | 연결부재 이름 노트를 풍선과 동일한 모델 fit 70% 영역 정렬에 포함. 실제 접합점 Target은 유지하고 이름 라벨만 모델 외곽으로 이동 | Codex |
| 2026-07-23 | PDF ISO 부재번호 풍선을 SDK 1.0.26.723의 영역 정렬 API로 후처리. 전체 View를 기준으로 주면 템플릿 밖으로 밀리는 동작을 반영해 실제 모델 중심·fit 70% 영역과 10px offset을 사용하고 연결 이름 노트는 정렬 대상에서 분리 | Codex |
| 2026-07-23 | 설치도 준비 치수에서 선택 STRU 전체 범위를 제거하고 실제 연결 거리만 유지. ISO 화면에는 기존처럼 치수를 표시하지 않음 | Codex |
| 2026-07-22 | 설치도 PDF 이름 노트를 접합 중심별 A1/A2에서 연결 Part당 1개로 통합하고 접합측 실제 모서리에 지시 | Codex |
| 2026-07-22 | 설치도 표시 문맥을 직접 연결 Part로 축소하고, PDF ISO는 선택 STRU 기준 fit·Crop 후 연결 Part만 점선으로 표시하도록 변경 | Codex |
| 2026-07-22 | ISO 진입 시 직전 X/Y/Z의 3D 측정선·보조선을 먼저 제거해 풍선만 표시하도록 명시. 설치도 접합 치수 데이터는 X/Y/Z·2D용으로 유지 | Codex |
| 2026-07-21 | 설치도 ISO 미리보기 대상을 선택 STRU+직접 연결 외부 Assembly 전체로 확장하고, 실제 접합 영역·Osnap 기반 준비 치수를 적용하도록 변경 | Codex |
| 2026-04-13 | 초안 작성 | — |
