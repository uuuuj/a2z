---
feature_id: CLS-001
feature_name: BOM 정보 수집 (Clash 탭)
category: Clash
trigger_type: User Action
owner_module: Form1.Clash.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-clash.md#btnCollectBOMInfo_Click
---

# BOM 정보 수집 (Clash 탭)

## 1. 개요
도면 정보 탭(lvDrawingBOMInfo) 표시용 BOM 정보를 UDA(SPREF, MATREF, GWEI)에서 파싱하여 수집한다. `btnCollectBOM_Click`과 달리 **Part 레벨 UDA**를 조회하며, 선택 시트가 있으면 해당 시트 부재로 필터링한다.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnCollectBOMInfo` 버튼 클릭 |
| 위치 | 메인 폼 > 도면 정보 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | ListView 클리어 | UI | `lvDrawingBOMInfo.Items.Clear()` |
| 2 | Part 노드 획득 | SDK | `GetPartialNode(false, true, false)` → 없으면 Body로 Fallback |
| 3 | 시트 필터링 | Form1 | `sheetOverride` 또는 `lvDrawingSheet.SelectedItems[0]` → [분기 A] |
| 4 | UDA 키 목록 조회 | SDK | `UDA.Keys` (한번만) |
| 5 | 노드별 UDA 파싱 | Form1 | SPREF(Item:Size), MATREF, GWEI 추출 |
| 6 | 그룹화 | Form1 | Item+Size+Material 동일 부재 합산 (Count, TotalWeight) |
| 7 | ListView 추가 | UI | 그룹별 행 표시 |

> 구현 상세는 [코드 레퍼런스](../../code-reference/form1-clash.md#CollectBOMInfo) 참고

## 5. 주요 분기 처리

### [분기 A] 시트 필터링
| 조건 | 처리 |
|---|---|
| `sheetOverride != null` | 해당 시트 부재만 대상 |
| 매개변수 null + 시트 선택 있음 | 선택 시트 부재만 대상 |
| 둘 다 없음 | 전체 Part 노드 대상 |

### [분기 B] Part / Body Fallback
| 조건 | 처리 |
|---|---|
| Part 노드 존재 | Part 레벨에서 UDA 조회 |
| Part 노드 없음 | Body 노드로 Fallback |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 노드 없음 | return | MessageBox "로드된 모델이 없거나 노드를 찾을 수 없습니다." (showAlert=true일 때만) | `lvDrawingBOMInfo` 빈 상태 |
| E02 | 처리 중 예외 | catch | MessageBox "BOM 정보 수집 오류: {msg}" | 부분 채워짐 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `lvDrawingBOMInfo` | 이전 행 | Item/Size/Material/Count/TotalWeight 그룹 행 |

## 8. 후행 기능 (Chained)
- [시트별 2D 생성](../도면시트/시트 2D 렌더.md) — 내부적으로 `CollectBOMInfo(false, sheet)` 호출

## 9. 관련 링크
- 코드 구현: [Form1.Clash.cs:L15](../../code-reference/form1-clash.md#btnCollectBOMInfo_Click)
- 용어집: [UDA](../../_glossary.md#uda-user-defined-attribute), [BOM](../../_glossary.md#bom-bill-of-materials)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
