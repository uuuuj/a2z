# A2Z - 3D CAD 모델 자동 분석 및 2D 제조 도면 생성 시스템

> 조선/해양 3D CAD 모델(.vizx/.viz)을 로드하여 BOM 추출, 홀/슬롯홀 자동 검출, 체인 치수 자동 생성, 간섭검사, 2D 도면 출력까지 전 과정을 자동화하는 데스크톱 애플리케이션

---

## 1. 기술 스택

| 항목 | 기술 |
|------|------|
| 언어 | C# |
| 프레임워크 | .NET Framework 4.8 / Windows Forms |
| 3D 엔진 | VIZCore3D.NET (SOFTHILLS Co., Ltd.) |
| 2D 렌더링 | GDI+ (System.Drawing / System.Drawing.Drawing2D) |
| 출력 형식 | PDF (Microsoft Print to PDF) / PNG / JPEG |

---

## 2. 전체 워크플로우

```
[파일 열기 (.vizx/.viz)]
    │
    ▼
[BOM 자동 수집] ── Body 노드 순회, 바운딩박스/중심좌표/UDA 수집
    │
    ▼
[홀/슬롯홀 자동 검출] ── 원기둥-판재 매칭 + Osnap 기반 2단계 검출
    │
    ▼
[치수 추출] ── Osnap 좌표 수집 → 좌표 병합 → 축별 체인 치수 생성
    │
    ▼
[치수 표시] ── Smart Dimension Filtering → 멀티레벨 레이아웃 → 풍선 배치
    │
    ▼
[간섭(Clash) 검사] ── 전체 Body 쌍 비동기 간섭검사 → 도면 시트 자동 생성(BFS)
    │
    ▼
[2D 도면 출력] ── 4분할 뷰 캡처 + BOM표 + 타이틀블록 → PDF/PNG 내보내기
```

---

## 3. 기능 상세

### 3.1 파일 열기 및 BOM 수집

**기능**: 3D 모델 파일을 로드하고 모든 Body 노드의 정보를 자동 수집한다.

**수집 데이터**:
- 노드 이름 및 인덱스
- 바운딩박스 (Min/Max XYZ)
- 중심 좌표 (Center XYZ)
- 원형 반지름 (CircleRadius)
- UDA (User Defined Attributes): PURPOSE, ITEM, SIZE, MATL, WEIGHT

**Body-Part 매핑**: Part 인덱스를 정렬한 뒤 이진탐색(Binary Search)으로 Body 인덱스의 부모 Part 이름을 결정한다.

**BOM 정보 수집**: UDA에서 Item/Size/Matl/Weight를 읽어 동일 사양별로 그룹핑하고 수량(Q'ty)을 자동 카운팅한다.

---

### 3.2 홀(Hole) 자동 검출 — 2단계 알고리즘

3D 모델에서 원형 홀을 자동으로 찾아내는 알고리즘이다.

#### 1단계: 원기둥-판재 매칭

```
1. 모든 Body를 원기둥 / 판재로 분류
   - 원기둥: CircleRadius > 0이고, 바운딩박스 2축 이상이 지름과 일치
   - 판재: 나머지 Body

2. 원기둥의 높이가 판재의 최소 두께 이하인지 확인

3. 원기둥의 중심이 판재 바운딩박스 내부에 포함되는지 확인

4. 조건을 모두 만족하면 홀로 확정
   - 복수의 판재가 후보인 경우, 가장 작은 부피의 판재에 할당
```

#### 2단계: Osnap 기반 보조 검출

별도 원기둥 Body 없이 판재 자체에 있는 홀을 검출한다.

```
1. 판재의 Osnap CIRCLE 데이터에서 동축 쌍을 탐색
   - 같은 반지름, 1축만 좌표가 다른 원 쌍

2. IsCompleteCircle() 검증
   - 포인트 8개 이상이 360도에 고르게 분포하는지 확인
   - 최대 각도 간격 90도 미만 (필렛/라운드 코너 제외)

3. 검증 통과 시 홀로 확정
```

---

### 3.3 슬롯홀(Slot Hole) 자동 검출

장공(긴 홀) 형태의 슬롯홀을 자동 인식한다. 슬롯홀은 **반원기둥 2개 + 사각기둥 1개** 구조로 구성된다.

```
1. 확정된 홀 근처 원을 제외한 Osnap CIRCLE에서 동축 쌍 탐색

2. 완전한 원이 아닌 부분 호(arc) 쌍만 후보로 선정

3. 후보 쌍 2개가 다음 조건을 만족하는지 확인:
   - 같은 반지름 / 같은 축 / 같은 깊이
   - 횡방향 오프셋 존재 (두 반원 사이 거리 = 슬롯 길이)

4. HasSlotConnectingLines() 검증:
   - 두 반원기둥 사이에 LINE Osnap이 존재하여 사각기둥 구조 확인

5. 모든 조건 만족 시 슬롯홀로 확정
   - 각 원(circle index)은 한 번만 사용 (중복 방지)
```

---

### 3.4 Osnap 좌표 수집

3D 모델의 기하학적 특징점(Object Snap Point)을 자동 수집한다.

| Osnap 유형 | 수집 데이터 |
|------------|------------|
| LINE | StartPoint, EndPoint |
| CIRCLE | Center |
| POINT | Center |
| SURFACE | 제외 |

- 전체 Body 또는 X-Ray 선택된 부재만 대상으로 수집 가능
- 마우스 클릭으로 수동 좌표 추가 지원

---

### 3.5 체인 치수 자동 생성

Osnap 좌표를 기반으로 X/Y/Z 축별 순차 치수와 전체 치수를 자동 생성한다.

```
1. 좌표 병합 (MergeCoordinates)
   - 허용 오차 0.5mm 이내의 좌표를 동일 좌표로 병합
   - 중복 제거

2. 축별 처리 (AddChainDimensionByAxis)
   - 필터 축의 최소값만 유지 (한 줄로 정렬)
     · X치수: 각 X값에서 최소 Z만 유지
     · Y치수: 각 Y값에서 최소 X만 유지
     · Z치수: 각 Z값에서 최소 Y만 유지
   - 측정 축 기준 정렬

3. 치수 생성
   - 인접 포인트 쌍 → 순차 치수
   - 처음~마지막 포인트 → 전체 치수 (IsTotal = true)
```

---

### 3.6 Smart Dimension Filtering 알고리즘

치수가 많을 때 가독성을 확보하기 위한 지능형 필터링 및 배치 알고리즘이다.

#### 우선순위 할당 (Priority 1~10)

```
- 전체 치수 (IsTotal)     → Priority 10
- 상위 30% 거리값         → Priority 8
- 중간 40%                → Priority 5
- 하위 20%                → Priority 3
- 최하위 10%              → Priority 1
```

#### Greedy Label Placement

```
1. 우선순위 높은 순으로 배치 시도
2. 텍스트 간 최소 간격(minTextSpace) 확보 여부 확인
3. 간격 미확보 시 다음 레벨(Level)로 배정
4. 최대 2레벨까지 사용
```

#### 짧은 치수 병합

```
- 연속된 짧은 치수들 (minTextSpace 미만)을 누적 치수 1개로 병합
- 병합된 치수는 IsMerged = true
```

#### 멀티레벨 레이아웃

```
Level 0 (가장 바깥): 전체 치수
Level 1 (안쪽):     기본 순차 치수
Level 2 (중간):     겹치는 치수 (Level 1에서 배치 실패한 치수)
```

---

### 3.7 풍선(Balloon) 자동 배치

부재 번호 마커를 다른 부재 및 풍선과 겹치지 않게 자동 배치한다.

#### ISO 뷰 풍선 배치

```
1. 부재 중심에서 모델 중심 반대 방향으로 초기 위치 설정
2. 충돌 검사 (다른 부재/풍선과 겹침)
3. 충돌 시 15도씩 회전 + 거리 확대
4. 최대 36회 시도 (360도 탐색)
```

#### 정사영 뷰 풍선 배치

```
1. 2D H-V 평면 기반 배치
2. 모델 중심 반대 방향에서 10도씩 회전 + 거리 확대
```

#### 치수선 뷰 풍선 (홀/슬롯홀)

```
- 모델 Osnap 범위 바깥 (좌/우 자동 판단)에 배치
- 텍스트 간 30mm 최소 간격 유지
```

---

### 3.8 간섭(Clash) 검사

모든 Body 쌍에 대해 기하학적 간섭을 자동 검사한다.

```
1. N개 Body → N*(N-1)/2 쌍 등록
2. 파라미터: Clearance=1mm, Range=1mm, Penetration=1mm
3. 비동기 실행 (PerformInterferenceCheck)
4. 완료 이벤트에서 결과 수집
5. 중복 제거 후 Z값 기준 오름차순 정렬
6. 도면 시트 자동 생성 트리거
```

---

### 3.9 도면 시트 자동 생성 — BFS 알고리즘

Clash 인접 관계를 기반으로 관련 부재들을 자동 그룹핑하여 도면 시트를 생성한다.

```
1. Clash 결과로 인접 리스트(Adjacency List) 구축
   - 부재 A와 B가 간섭 → 서로 인접

2. BOM 순서대로 기준 부재 선정
   - 이미 다른 시트에 포함된 부재는 스킵

3. 기준 부재에서 BFS(너비 우선 탐색) 수행
   - 인접한 모든 부재를 같은 시트에 포함

4. 마지막 시트는 전체 BFS 탐색으로 설치도 생성
```

---

### 3.10 2D 도면 생성

3D 모델을 4방향에서 캡처하여 제조용 2D 도면을 자동 생성한다.

#### 4분할 뷰 구성

| 위치 | 뷰 | 표시 치수 |
|------|-----|----------|
| 좌상 | ISO (등각투영) | 부재 번호 풍선 |
| 우상 | Z+ (평면도) | X, Y축 치수 |
| 좌하 | Y+ (정면도) | X, Z축 치수 |
| 우하 | X+ (측면도) | Y, Z축 치수 |

#### 도면 구성 요소

- **BOM 테이블**: No / Name / Type / Qty
- **타이틀 블록**: Company / Drawing Name / DWG No / Scale / Date
- **렌더링**: 흰색 오브젝트 + 검정 실루엣 엣지 (DASH_LINE 모드)

---

### 3.11 가공도 출력

단일 부재의 제조 가공 정보를 상세하게 표시한다.

```
1. 선택된 단일 부재만 표시 (나머지 숨김)
2. 최장축 기준으로 카메라 방향 자동 결정
3. 해당 부재의 Osnap 수집 및 치수 표시
4. 홀 사이즈 풍선 표시 (지름)
5. 슬롯홀 사이즈 풍선 표시 (폭 x 길이)
```

---

### 3.12 부재 정보 조회 및 UDA 관리

- **3D 객체 클릭**: 노드 정보, 바운딩박스, UDA, 지오메트리 속성 자동 표시
- **UDA CRUD**: 추가 / 편집 / 삭제
- **CSV 일괄 가져오기**: CSV 파일에서 UDA 일괄 등록
- **X-Ray 모드**: 선택 부재만 불투명, 나머지 반투명 처리

---

## 4. 데이터 구조

### BOMData

| 필드 | 타입 | 설명 |
|------|------|------|
| Index | int | 노드 인덱스 |
| Name | string | 부재 이름 |
| Center X/Y/Z | float | 중심 좌표 |
| Min/Max X/Y/Z | float | 바운딩박스 |
| CircleRadius | float | 원형 반지름 |
| Purpose | string | UDA PURPOSE 값 |
| Holes[] | HoleInfo[] | 검출된 홀 목록 |
| SlotHoles[] | SlotHoleInfo[] | 검출된 슬롯홀 목록 |

### HoleInfo

| 필드 | 타입 | 설명 |
|------|------|------|
| Diameter | float | 홀 지름 |
| Center X/Y/Z | float | 홀 중심 좌표 |
| CylinderBodyIndex | int | 원기둥 Body 인덱스 |

### SlotHoleInfo

| 필드 | 타입 | 설명 |
|------|------|------|
| Radius | float | 반원 반지름 |
| SlotLength | float | 슬롯 길이 |
| Depth | float | 깊이 |
| Center X/Y/Z | float | 슬롯홀 중심 좌표 |

### ChainDimensionData

| 필드 | 타입 | 설명 |
|------|------|------|
| Axis | string | "X", "Y", "Z" |
| Distance | float | 거리값 (mm) |
| Start/EndPoint | Vector3D | 시작/끝 좌표 |
| IsTotal | bool | 전체 치수 여부 |
| Priority | int | 1~10 우선순위 |
| DisplayLevel | int | 배치 레벨 (0/1/2) |
| IsVisible | bool | 필터링 후 표시 여부 |
| IsMerged | bool | 병합 치수 여부 |

### ClashData

| 필드 | 타입 | 설명 |
|------|------|------|
| Index1, Index2 | int | 충돌 부재 인덱스 쌍 |
| Name1, Name2 | string | 충돌 부재 이름 쌍 |
| ZValue | float | 충돌 지점 Z좌표 |

### DrawingSheetData

| 필드 | 타입 | 설명 |
|------|------|------|
| SheetNumber | int | 시트 번호 |
| BaseMemberName | string | 기준 부재 이름 |
| MemberIndices[] | int[] | 포함 부재 인덱스 목록 |
| MemberNames[] | string[] | 포함 부재 이름 목록 |

---

## 5. UI 구조

```
┌─────────────────────────────────────────────────────┐
│  Form1 (1600 x 1000)                                │
│  ┌──────────────┬──────────────────────────────────┐ │
│  │  좌측 패널    │  3D 뷰어 (VIZCore3D)             │ │
│  │  (457px)     │                                  │ │
│  │              │                                  │ │
│  │ ┌──────────┐ │                                  │ │
│  │ │ 작업     │ │                                  │ │
│  │ │[파일열기] │ │                                  │ │
│  │ │[치수추출] │ │                                  │ │
│  │ │[X][Y][Z] │ │                                  │ │
│  │ │[BOM][2D] │ │                                  │ │
│  │ │ ...      │ │                                  │ │
│  │ ├──────────┤ │                                  │ │
│  │ │ BOM      │ │                                  │ │
│  │ ├──────────┤ │                                  │ │
│  │ │ Osnap    │ │                                  │ │
│  │ ├──────────┤ │                                  │ │
│  │ │ 치수     │ │                                  │ │
│  │ ├──────────┤ │                                  │ │
│  │ │ Clash    │ │                                  │ │
│  │ │ (Fill)   │ │                                  │ │
│  │ └──────────┘ │                                  │ │
│  └──────────────┴──────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### 버튼 계층

| 계층 | 크기 | 예시 |
|------|------|------|
| 메인 버튼 | 190x40, Bold 11pt, 색상 강조 | [파일 열기] [치수 추출] |
| 축 버튼 | 120x30 | [X축] [Y축] [Z축] |
| 서브 버튼 | 55~65x25 | [BOM] [Clash] [Osnap] [치수] [2D] [PDF] |

---

## 6. 외부 의존성

### VIZCore3D.NET 주요 API

| API | 용도 |
|-----|------|
| `Model.Open()` | 3D 모델 파일 로드 |
| `Object3D.GetPartialNode()` | Assembly/Part/Body 노드 조회 |
| `Object3D.GetBoundBox()` | 바운딩박스 조회 |
| `Object3D.GetOsnapPoint()` | Osnap 특징점 조회 |
| `Object3D.UDA` | User Defined Attributes CRUD |
| `GeometryUtility.GetCircleData()` | 원형 데이터 (지름, 중심) 조회 |
| `Clash.*` | 간섭검사 (Add/Perform/GetResult) |
| `Review.Measure.AddCustomAxisDistance()` | 축방향 거리 측정 |
| `Review.Note.AddNoteSurface()` | 풍선/노트 표시 |
| `ShapeDrawing.AddLine()` | 보조선/확장선 그리기 |
| `View.XRay.Select()` | X-Ray 모드 |
| `View.SetRenderMode()` | 렌더링 모드 전환 (DASH_LINE 등) |
| `View.BackgroundRenderingMode` | 4방향 뷰 캡처 |

---

## 7. 프로젝트 구조

```
A2Z/
├── Form1.cs              # 전체 비즈니스 로직 (~6,900줄)
├── Form1.Designer.cs     # UI 레이아웃 (WinForms Designer)
├── Form1.resx            # 리소스
├── Program.cs            # 진입점
├── A2Z.csproj            # .NET Framework 4.8, VIZCore3D.NET 참조
└── Properties/           # AssemblyInfo, Resources, Settings
```
