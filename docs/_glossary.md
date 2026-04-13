# 용어집

A2Z-HYI 문서에서 반복적으로 사용되는 개념을 정리합니다.

---

## 3D 모델 · SDK

### VIZCore3D
본 프로젝트가 사용하는 3D 뷰어·도면 엔진 SDK. `vizcore3d` 인스턴스를 통해 모델 로드, 노드 탐색, 간섭 검사, 2D 투영, PDF 출력을 수행합니다.

### Node / Part / Body
- **Node**: VIZCore3D 내부 노드. 계층 구조(어셈블리 트리)의 한 요소.
- **Part**: BOM 단위의 논리적 부재 (예: 기둥 1개).
- **Body**: Part 하위 형상. 한 Part에 여러 Body가 있을 수 있음. 홀 감지는 Body 단위.

### UDA (User Defined Attribute)
3D 노드에 부착된 사용자 정의 속성. 핵심 키:
- `SPREF` — Item:Size (부재 타입/규격, PAD·PLATE 판별에 사용)
- `MATREF` — Material (재질)
- `GWEI` — Weight (중량)
- `ORIENTATION` — 가공도 생성 시 카메라 회전 각도

### Osnap (Object Snap)
부재 경계의 **특징점 좌표**(꼭짓점, 중점, 중심 등). 치수 추출·풍선 배치의 기준점으로 사용.

---

## 도면 · 기하

### BOM (Bill of Materials)
부재 목록. `bomList`에 `BOMData` 객체로 저장. Index/Name/BoundingBox/Holes 등을 포함.

### Clash (간섭)
두 부재의 3D 충돌. `vizcore3d.Clash`로 검사, 결과는 `clashList`에 `ClashData`로 저장.

### Chain Dimension (체인 치수)
동일 축 위의 인접 부재 경계 간 거리를 연속으로 표기하는 치수. `ChainDimensionData` 사용.

### Hidden Line (은선)
2D 도면에서 가려진 면을 점선으로 표현하는 렌더 모드. `SetRenderMode(DASH_LINE)`.

### 풍선 (Balloon Note)
도면 위 부재 번호 원형 주석. 위치 충돌 시 `balloonOverrides` Dict로 오프셋 부여.

### 보조선
풍선과 부재를 잇는 지시선.

---

## 시트 · 가공도

### Drawing Sheet (도면 시트)
한 장의 도면 단위. `DrawingSheetData`로 표현. `MemberIndices` 리스트가 시트에 포함될 부재 목록.

### BFS 기반 시트 분할
Clash 관계를 인접 리스트로 보고, 각 부재를 중심으로 BFS 트래버설하여 연결 부재를 한 시트에 모으는 알고리즘. `Form1.DrawingSheets.cs`의 `GenerateDrawingSheets()` 참고.

### MfgDrawing (가공도, Manufacturing Drawing)
**단일 부재**의 제조용 상세 도면. 부재의 최장축이 가로가 되도록 회전하고, PAD/PLATE 여부에 따라 뷰 방향을 결정.

### PAD / PLATE
- **PAD**: 보강판 형태. 한 면을 기준으로 두께 방향이 명확.
- **PLATE**: 판재. SPREF UDA를 기반으로 판별.

---

## UI · 상태

### X-Ray 모드
선택 부재 외를 반투명 처리하여 강조하는 뷰 모드. `xraySelectedNodeIndices`로 대상 추적.

### balloonOverrides
풍선 위치 수동 오버라이드 딕셔너리. 충돌 회피 또는 사용자 조정 반영.

### chainDimensionList
현재 모델에서 추출된 체인 치수 집합.

### bodyToPartNameMap / bodyToPartIndexMap
Body Index → 소속 Part 이름/Index 역조회 캐시 (성능 최적화).

---

## 파일 형식

### .vizx / .viz
VIZCore3D 네이티브 3D 모델 포맷.

### PDF (벡터)
`Export2PDFBy2DView()`로 생성되는 벡터 PDF. CAD 호환.
