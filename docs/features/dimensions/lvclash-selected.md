---
feature_id: DIM-008
feature_name: Clash 선택 시 Osnap·치수 자동 필터
category: Dimensions
trigger_type: Event Callback
owner_module: Form1.Dimensions.cs
last_updated: 2026-05-11
code_reference: /docs/code-reference/form1-dimensions.md#LvClash_SelectedIndexChanged
---

# Clash 선택 시 Osnap·치수 자동 필터

## 1. 개요
`lvClash`에서 선택이 변경되면, 선택된 간섭 쌍에 관련된 부재의 BBox 합집합을 기준으로 **Osnap ListView와 Dimension ListView에서 관련 항목을 자동 선택**한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | Event Callback |
| 입력 | `lvClash.SelectedIndexChanged` |
| 위치 | 메인 폼 > Clash 리스트 |

## 3. 사전 조건
- [ ] `clashList`, `bomList` 채워짐
- [ ] `lvOsnap`, `lvDimension` 표시 상태

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 선택 확인 | Form1 | `SelectedItems.Count == 0` → return |
| 2 | 관련 노드 이름 수집 | Form1 | Clash.Name1, Name2 → HashSet |
| 3 | BBox 결합 | Form1 | bom1, bom2의 Min/Max 결합 BBox |
| 4 | Osnap 자동 선택 | Form1 | `SelectRelatedOsnapItems()` 호출 |
| 5 | Dimension 자동 선택 | Form1 | `SelectRelatedDimensionItems()` 호출 |

### 내부 선택 로직
- 부재명 일치 → 즉시 선택
- BBox 내 좌표 포함 (tolerance 1.0mm) → 선택
- 첫 선택 항목 `EnsureVisible()`로 스크롤

## 5. 주요 분기 처리

### [분기 A] bom1/bom2 매칭
| 조건 | 처리 |
|---|---|
| 둘 다 매칭 | BBox 결합 후 저장 |
| 매칭 실패 | 해당 Clash 스킵 (이름만 사용) |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 예외 발생 | catch(무시) | 없음 | 선택이 반영되지 않을 수 있음 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `lvOsnap.SelectedItems` | 이전 | 관련 항목으로 교체 |
| `lvDimension.SelectedItems` | 이전 | 관련 항목으로 교체 |

## 8. 후행 기능 (Chained)
- [선택 치수 표시](./show-selected.md)
- [선택 Osnap 풍선 표시](../drawing2d/osnap-show-selected.md)

## 9. 관련 링크
- 코드 구현: [Form1.Dimensions.cs:L1551](/docs/code-reference/form1-dimensions.md#LvClash_SelectedIndexChanged)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
| 2026-05-11 | **REQ-D: 선택 시 3D 강조 + 카메라 fit 추가** (사용자 요청). 단일 선택일 때 `Object3D.Color.RestoreColorAll()` + `Object3D.Select([Index1, Index2])` + `View.FlyToObject3d(idxs, 1.2f)` 적용. `LvClash_DoubleClick` 동일 패턴 차용. 기존 자동 Osnap·Dimension 필터링은 그대로 유지 | Claude |
