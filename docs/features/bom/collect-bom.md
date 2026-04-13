---
feature_id: BOM-003
feature_name: BOM 수집 (홀 감지 포함)
category: BOM
trigger_type: User Action
owner_module: Form1.BOM.cs
last_updated: 2026-04-13
code_reference: /docs/code-reference/form1-bom.md#btnCollectBOM_Click
---

# BOM 수집 (홀 감지 포함)

## 1. 개요
로드된 모델에서 모든 Body 노드를 순회하여 BOM 데이터(Index, Name, 바운딩박스, 회전각, 홀·슬롯홀 정보)를 수집한다. 이후 모든 도면 기능의 전제 데이터.

## 2. 트리거
| 항목 | 값 |
|---|---|
| 유형 | User Action |
| 입력 | `btnCollectBOM` 버튼 클릭 |
| 위치 | 메인 폼 > BOM 탭 |

## 3. 사전 조건
- [ ] 3D 모델 로드됨 ([BOM-002](./open-model.md) 완료)

## 4. 전체 동작 흐름 (Happy Path)

| # | 단계 | 주체 | 설명 |
|---|---|---|---|
| 1 | 내부 수집 위임 | Form1 | `CollectBOMData()` 호출 |
| 2 | 결과 판정 | Form1 | bool 반환값 확인 |
| 3 | 결과 알림 | UI | MessageBox로 수집 개수 표시 또는 실패 알림 |

### CollectBOMData 내부 세부 흐름
1. 모든 Body 노드 획득 (`Object3D.GetPartialNode(false, false, true)`)
2. 노드별 BoundingBox 계산 (`GetBoundBox`)
3. 원형/슬롯형 홀 감지 (`DetectHoles`) — 내부 실린더 Body 분석
4. BOMData 인스턴스 생성 및 `bomList`에 추가
5. `lvBOM` ListView 갱신

> 구현 상세는 [코드 레퍼런스](/docs/code-reference/form1-bom.md#CollectBOMData) 참고

## 5. 주요 분기 처리

### [분기 A] 홀 판정
| 조건 | 처리 |
|---|---|
| 실린더 Body가 부재 내부에 존재 | 원형 홀로 등록 (`HoleInfo`) |
| 슬롯 형태 (장공) 감지 | 슬롯홀로 등록 (`SlotHoleInfo`) |
| 실린더 없음 | Holes·SlotHoles 빈 리스트 |

## 6. 예외 / 에러 처리

| ID | 조건 | 동작 | 사용자 피드백 | 결과 상태 |
|---|---|---|---|---|
| E01 | 모델 미로드 또는 노드 없음 | success=false 반환 | MessageBox "로드된 모델이 없거나 BOM 수집에 실패했습니다." | `bomList` 비어있음 |
| E02 | 수집 중 예외 | catch로 흡수 | 내부 에러 메시지 | 부분 수집 가능성 |

## 7. 상태 변화 (Before / After)

| 대상 | Before | After |
|---|---|---|
| `bomList` | 비어있음 또는 이전 데이터 | 현재 모델의 BOMData 리스트 |
| `bodyToPartNameMap` | 부분 또는 빈 매핑 | Body → Part 이름 완전 매핑 |
| `bodyToPartIndexMap` | 부분 또는 빈 매핑 | Body → Part Index 완전 매핑 |
| `bomInfoNodeGroupMap` | 이전 상태 | BOM 그룹 번호 갱신 |
| `lvBOM` | 이전 또는 빈 항목 | 수집된 BOM 행 표시 |

## 8. 후행 기능 (Chained)
- [메인 치수 추출](./main-dimension.md)
- [전체 2D 생성](../drawing2d/generate-2d.md)
- [선택 부재 가공도](../mfg-drawing/mfg-drawing.md)
- [Clash 검사](../clash/detect-clash.md)

## 9. 관련 링크
- 코드 구현: [Form1.BOM.cs:L1418](/docs/code-reference/form1-bom.md#btnCollectBOM_Click)
- 용어집: [BOM](../../_glossary.md#bom-bill-of-materials), [UDA](../../_glossary.md#uda-user-defined-attribute)
- 상위 파이프라인: [전체 파이프라인](../../_pipeline.md)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-04-13 | 초안 작성 | — |
