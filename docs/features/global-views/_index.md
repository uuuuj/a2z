# GlobalViews 기능

`Form1.GlobalViews.cs` 소속. 전체 모델/선택 부재 기준 뷰 방향 전환 기능입니다.

## 기능 목록
| ID | 기능 | 트리거 | 유형 | 문서 |
|---|---|---|---|---|
| GV-001 | ISO 뷰 | btnGlobalISO_Click | User Action | [global-iso](./global-iso.md) |
| GV-002 | X축 뷰 | btnGlobalAxisX_Click | User Action | [global-axis-x](./global-axis-x.md) |
| GV-003 | Y축 뷰 | btnGlobalAxisY_Click | User Action | [global-axis-y](./global-axis-y.md) |
| GV-004 | Z축 뷰 | btnGlobalAxisZ_Click | User Action | [global-axis-z](./global-axis-z.md) |

## 공통 동작 요약
4개 버튼 모두 공통 함수 `ApplyGlobalView(CameraDirection)`을 호출합니다. 내부적으로 다음 분기:
- 탭 상태 + X-Ray 모드 여부 판정
- 선택 노드 존재 → `ApplySelectedNodesView()`
- 전체 모델 → `ApplyFullModelView()`
- ISO 뷰인 경우 풍선 노트 함께 생성
