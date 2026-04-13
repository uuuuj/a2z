# BOM 기능

`Form1.BOM.cs` 소속. 3D 모델에서 부재 목록(BOM)을 추출하고 UDA/홀 정보를 수집합니다.

## 기능 목록
| ID | 기능 | 트리거 | 유형 | 문서 |
|---|---|---|---|---|
| BOM-001 | VIZCore3D 초기화 완료 | Vizcore3d_OnInitializedVIZCore3D | Event Callback | [vizcore3d-initialized](./vizcore3d-initialized.md) |
| BOM-002 | 모델 파일 열기 | btnOpen_Click | User Action | [open-model](./open-model.md) |
| BOM-003 | BOM 수집 (홀 감지 포함) | btnCollectBOM_Click | User Action | [collect-bom](./collect-bom.md) |
| BOM-004 | 메인 체인 치수 계산 | btnMainDimension_Click | User Action | [main-dimension](./main-dimension.md) |

## 데이터 의존성
```mermaid
flowchart LR
    BOM-001[SDK 초기화] --> BOM-002[모델 로드]
    BOM-002 --> BOM-003[BOM 수집]
    BOM-003 --> BOM-004[치수 계산]
    BOM-003 --> Clash[Clash 검사]
    BOM-003 --> Drawing2D[2D 도면]
```

## 주요 생성 상태
- `bomList : List<BOMData>`
- `bodyToPartNameMap`, `bodyToPartIndexMap`
- `bomInfoNodeGroupMap`
- `chainDimensionList : List<ChainDimensionData>`
