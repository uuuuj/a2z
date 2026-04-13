---
feature_id: DRW2D-006
feature_name: 선택 Clash 부재 X-Ray 강조
category: Drawing2D
trigger_type: User Action
owner_module: Form1.Drawing2D.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-drawing2d.md#btnClashShowSelected_Click
---

# 선택 Clash 부재 X-Ray 강조

## 1. 개요
`lvClash` 선택 행의 부재들만 X-Ray 모드로 강조 표시하고, BBox 중심에 삼각형 심볼을 띄운 뒤 해당 부재만 대상으로 Osnap·치수를 자동 재추출한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnClashShowSelected` 클릭 |
| 위치 | 메인 폼 > Clash 탭 |

## 3. 사전 조건
- [ ] Clash 결과 채워짐
- [ ] `lvClash` 선택 있음
- [ ] `bomList` 존재 (BBox 계산용)

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | → [E01] |
| 2 | BeginUpdate | SDK | 일괄 렌더 |
| 3 | X-Ray 활성화 | SDK | ColorType, SelectionType 설정 |
| 4 | 이전 선택 해제 | SDK | `XRay.Clear()` |
| 5 | 선택 부재 수집 | Form1 | Index1, Index2, Name1/2 집계 |
| 6 | X-Ray 선택 적용 | SDK | `XRay.Select(indices, true)` |
| 7 | xray 상태 저장 | Form1 | `xraySelectedNodeIndices = new List(indices)` |
| 8 | 카메라 이동 | SDK | `FlyToObject3d(indices, 1.2f)` |
| 9 | Clash 심볼 재표시 | SDK | BBox 교집합 중심에 Triangle 노란색 |
| 10 | EndUpdate | SDK | 렌더 재개 |
| 11 | Osnap 자동 수집 | Form1 | `CollectOsnapForSelectedNodes(indices)` |
| 12 | 치수 자동 추출 | Form1 | `ExtractDimensionForSelectedNodes()` |

## 5. 주요 분기 처리

### [분기 A] bom1/bom2 매칭
| 조건 | 처리 |
|---|---|
| 둘 다 매칭 | Clash 심볼 중심 계산, BBox 결합 저장 |
| 매칭 실패 | 해당 Clash 심볼 스킵 |

## 6. 예외 / 에러 처리
| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 선택 없음 | return | MessageBox "Clash 항목을 선택해주세요." | 변화 없음 |
| E02 | 처리 중 예외 | catch | MessageBox "Clash 표시 중 오류: {msg}" | 부분 반영 |

## 7. 상태 변화 (Before / After)
| 대상 | Before | After |
|---|---|---|
| `vizcore3d.View.XRay.Enable` | 이전 | true |
| `vizcore3d.View.SilhouetteEdge` | 이전 | true (Green) |
| `xraySelectedNodeIndices` | 이전 | 선택 Clash 부재 인덱스 |
| Clash 심볼 | 이전 | Triangle 노란색 5mm |
| `osnapPointsWithNames`, `chainDimensionList` | 이전 | 선택 부재 기준 재계산 |

## 8. 후행 기능 (Chained)
- [전체 보기](./clash-show-all.md)로 복귀
- [체인 치수 표시](../dimensions/show-selected.md)

## 9. 관련 링크
- 코드 구현: [Form1.Drawing2D.cs:L354](/docs/code-reference/form1-drawing2d.md#btnClashShowSelected_Click)
- 용어집: [X-Ray 모드](../../_glossary.md#x-ray-모드)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
