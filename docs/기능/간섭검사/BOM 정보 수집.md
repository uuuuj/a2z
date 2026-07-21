---
feature_id: CLS-001
feature_name: BOM 정보 수집 (Clash 탭)
category: Clash
trigger_type: User Action
owner_module: Form1.Clash.cs
last_updated: 2026-07-21 (시트별 BOM 사전 준비 및 캐시 적용)
code_reference: /docs/code-reference/form1-clash.md#btnCollectBOMInfo_Click
---

# BOM 정보 수집 (Clash 탭)

## 1. 개요
도면 정보 탭(lvDrawingBOMInfo) 표시용 BOM 정보를 Part 레벨 UDA에서 파싱한다. 도면 시트 생성 때는 관련 Part의 UDA를 한 번만 읽어 모든 시트 결과를 준비하고, 이후 시트 선택·2D 출력에서는 준비된 행을 즉시 적용한다.

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
| 1 | 대상 시트 결정 | Form1 | `sheetOverride` 우선, 없으면 현재 선택 시트 |
| 2 | 준비 캐시 확인 | Form1 | 대상 시트의 `BomPrepared`가 true면 SDK 조회 없이 단계 7로 이동 |
| 3 | 관련 Part 결정 | Form1 | `bodyToPartIndexMap`으로 대상 Body를 Part로 매핑. 대상 시트가 없을 때만 전체 Part 조회 |
| 4 | 필요한 UDA 키 선별 | SDK | `UDA.Keys`에서 SPREF/MATREF/GWEI/POSSTART/POSEND 5개만 선별 |
| 5 | Part별 UDA 파싱 | Form1 | 현재 Part부터 부모 10단계까지 필요한 빈 값만 조회. Part별 한 번 수행 |
| 6 | 시트 스냅샷 구성 | Form1 | BOM 행과 Body→그룹 번호를 메모리에서 생성해 시트에 저장 |
| 7 | ListView 적용 | UI | 준비된 문자열 행과 그룹 맵을 즉시 복사 |

> 구현 상세는 [코드 레퍼런스](../../code-reference/form1-clash.md#CollectBOMInfo) 참고

## 5. 주요 분기 처리

### [분기 A] 시트 필터링
| 조건 | 처리 |
|---|---|
| `sheetOverride != null` | 해당 시트 부재만 대상 |
| 매개변수 null + 시트 선택 있음 | 선택 시트 부재만 대상 |
| 둘 다 없음 | 전체 Part 노드 대상 |

### [분기 B] 준비 캐시
| 조건 | 처리 |
|---|---|
| 시트 생성 단계에서 준비 완료 | `PreparedBomRows`·`PreparedBomNodeGroupMap`만 UI에 적용 |
| 임시/합성 시트 또는 준비 전 직접 호출 | 해당 시트 관련 Part만 조회해 결과를 저장한 뒤 적용 |

### [분기 C] Part / Body Fallback
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
| `DrawingSheetData` BOM 캐시 | 미준비 | 행 목록·Body 그룹 맵·준비 플래그 |

## 8. 후행 기능 (Chained)
- [시트별 2D 생성](../도면시트/시트 2D 렌더.md) — 내부적으로 `CollectBOMInfo(false, sheet)` 호출

## 9. 관련 링크
- 코드 구현: [Form1.Clash.cs:L15](../../code-reference/form1-clash.md#btnCollectBOMInfo_Click)
- 용어집: [UDA](../../_glossary.md#uda-user-defined-attribute), [BOM](../../_glossary.md#bom-bill-of-materials)

## 10. 변경 이력
| 날짜 | 변경 내용 | 작성자 |
|---|---|---|
| 2026-07-21 | 시트별 BOM 캐시 추가. 모델 로드 때 만든 Body→Part 매핑을 재사용하고 관련 Part의 UDA 5개를 한 번만 읽어 모든 시트 행을 준비. 시트 선택·2D 출력의 `CollectBOMInfo`는 준비 데이터 즉시 적용 경로로 전환 | Codex |
| 2026-04-13 | 초안 작성 | — |
