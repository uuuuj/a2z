# A2Z Development Log

> 3D CAD 모델 자동 분석 및 제조용 2D 도면 생성 시스템
> VIZCore3D.NET + C# WinForms (.NET Framework 4.8)
> 최종 업데이트: 2026-02-23

---

## 1. Project Overview

### 핵심 목적

삼성중공업 조선/해양 3D CAD 모델(.vizx/.viz)을 로드하여, **BOM 추출 → Osnap 좌표 수집 → 체인 치수 자동 생성 → 간섭(Clash) 검사 → 2D 제조 도면 출력**까지의 전 과정을 자동화하는 데스크톱 애플리케이션.

### 프로젝트 구조

```
A2Z/
├── Form1.cs              # 전체 비즈니스 로직 (약 3,440줄)
├── Form1.Designer.cs     # UI 레이아웃 (WinForms Designer)
├── Form1.resx            # 리소스
├── Program.cs            # 진입점
├── A2Z.csproj            # .NET Framework 4.8, VIZCore3D.NET 참조
├── Properties/           # AssemblyInfo, Resources, Settings
└── bin/Debug/            # VIZCore3D DLL 및 빌드 산출물
```

### 개발 진행 예정 사항

| No  | 기능                         | 상태    | 설명                                                                                               |
| --- | -------------------------- | ----- | ------------------------------------------------------------------------------------------------ |
| 2   | BOM 정보 탭 - 선택 부재 BOM 정보 표시 | 미구현   | 도면 정보 탭에서 선택된 부재의 BOM 정보를 표시                                                                     |
| 4   | 가공도 출력 - BOM정보 기준 4개씩 묶기   | 미구현   | BOM정보 탭 기준으로 가공도를 4개씩 묶어 출력. 가공도1 기준부재를 나열 후 도면 번호 매칭하여 4개씩 그룹핑                                  |
| 5   | 풍선 위치 개선                   | 미구현   | 풍선이 부재/치수와 겹치지 않으면서도 모델 가까이에 배치되도록 개선 (현재 부재 밖으로 너무 멀리 나감)                                       |
| 6   | BOM 수집 시점 변경 + 활성화 모델 기준   | 진행중   | 파일 열기 시 자동 BOM 수집 → 치수 추출 버튼 클릭 시 수집으로 변경. 전체 모델이 아닌 뷰어에 보이는 모델(트리 선택 기준) 대상으로 수집. 미선택 시 예외처리 필요 |

### 현재 구현 완료된 기능

| 기능                      | 상태 | 설명                                                 |
| ------------------------- | ---- | ---------------------------------------------------- |
| 파일 열기 + BOM 수집      | 완료 | 모델 로드 시 자동 BOM 수집, 새 파일 시 전체 초기화   |
| Osnap 좌표 수집           | 완료 | LINE/CIRCLE/POINT 유형 자동 수집 + 수동 추가/삭제    |
| 체인 치수 자동 추출       | 완료 | X/Y/Z축별 순차치수 + 전체치수 자동 생성              |
| Smart Dimension Filtering | 완료 | 우선순위 기반 필터링, 텍스트 겹침 방지, 레벨별 배치  |
| 간섭(Clash) 검사          | 완료 | 전체 부재 쌍 대상, Z값 기준 정렬, 비동기 실행        |
| X-Ray 선택 보기           | 완료 | Clash 부재만 X-Ray 표시, 자동 Osnap/치수 추출        |
| X/Y/Z축 방향 보기         | 완료 | 은선점선 모드 + 카메라 방향 전환 + 해당 축 치수 표시 |
| 2D 도면 생성              | 완료 | 4면도(ISO+평면+정면+측면) + BOM표 + 타이틀블록       |
| PDF/이미지 출력           | 완료 | PNG/JPEG 저장 + Microsoft Print to PDF               |
| 글로벌 뷰 버튼            | 완료 | 탭 공통 ISO/X/Y/Z 버튼, 줌 누적 문제 해결 (Phase 12) |
| ISO 풍선 BOM정보 기준     | 완료 | BOM정보 탭 그룹 기준 풍선 표시, 같은 그룹 대표 1개만 (Phase 11) |
| 도면 시트 선택 연동       | 완료 | 도면정보 탭 시트 선택 시 기준부재+Clash 연결부재만 X-Ray 표시 (Phase 13) |
| 치수 번호 동기화          | 완료 | ListView No.와 ChainDimensionData.No 동기화 (Phase 13) |

---

## 2. Core Development Rules

### 2.1 VIZCore3D.NET API 활용 규칙

**초기화 순서 (필수)**

```
ModuleInitializer.Run() → new VIZCore3DControl() → OnInitializedVIZCore3D 이벤트에서 라이선스/설정
```

**카메라 제어**

- `FlyToObject3d(indices, ratio)` — 특정 부재로 카메라 이동 (ratio 1.2f 권장)
- `MoveCamera(CameraDirection)` — 카메라 방향 전환
- `FitToView()` — 전체 모델 화면 맞춤
- 주의: `FitToView()`는 치수선도 포함하여 계산하므로 **치수 그리기 전에 호출**해야 progressive zoom 방지

**카메라 방향 설정값**

- X축 보기: `CameraDirection.X_MINUS` (모델 정면에서 봄)
- Y축 보기: `CameraDirection.Y_MINUS`
- Z축 보기: `CameraDirection.Z_PLUS` (위에서 내려다봄)

**렌더 모드**

```csharp
vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE); // 은선점선 (치수 검사용)
// enum: WIRE(0), FLAT(1), SMOOTH(2), EDGE(3), SMOOTH_EDGE(4), HIDDEN_LINE_REMOVAL(5), DASH_LINE(6)
```

**치수(Measure) API**

- `AddCustomAxisDistance(Axis, Vertex3D, Vertex3D)` → int ID 반환, 축방향 거리 측정
- `AddDistance(Vector3D, Vector3D)` — 2점 거리, 텍스트 위치는 사용자 수동 선택
- `AddDistance()` — 파라미터 없음, 인터랙티브 모드
- 주의: `SetTextPosition` 메서드는 존재하지 않음
- 주의: `AddDistance`에 3개 파라미터(position 포함) 오버로드 없음
- `UpdatePosition(id, ReviewPosition, Vertex3D)` — 존재하지만 ReviewPosition 클래스 구성이 복잡

**MeasureStyle 주요 설정**

```csharp
measureStyle.Prefix = false;              // 접두사 제거
measureStyle.Unit = false;                // 단위 제거
measureStyle.NumberOfDecimalPlaces = 0;   // 정수 표시
measureStyle.DX_DY_DZ = false;           // 축별 분해 제거
measureStyle.Frame = false;              // 프레임 제거
measureStyle.BackgroundTransparent = true;
measureStyle.FontColor = Color.Blue;
measureStyle.FontSize = FontSizeKind.SIZE12;
measureStyle.FontBold = true;
measureStyle.LineColor = Color.Blue;
measureStyle.LineWidth = 2;
measureStyle.ArrowSize = 10;
measureStyle.AssistantLine = false;       // 내장 보조선 비활성화 (ShapeDrawing 사용 시)
measureStyle.AlignDistanceText = true;
```

**ShapeDrawing API**

- `AddLine(List<Vertex3DItemCollection>, groupId, Color, width, visible)` — 보조선/확장선 그리기
- `AddSphere(List<Vertex3D>, groupId, Color, radius, visible)` — 좌표 마커 표시
- 주의: `AddLine`은 단일 `Vertex3DItemCollection`이 아닌 `List<Vertex3DItemCollection>`을 받음

**선택/하이라이트**

- `Object3D.Select(List<int>, true, false)` — 부재 선택 (빨간 하이라이트)
- `Object3D.Color.RestoreColorAll()` — 선택 해제 시도 (일부 상황에서 미동작)
- 주의: Clash 더블클릭 시 이전 선택 색상이 해제되지 않는 이슈 미해결

**Edge 데이터 (2D 도면용)**

```csharp
vizcore3d.Model.GenerateEdgeData = true;  // 파일 열기 전 설정 필수
vizcore3d.Model.LoadEdgeData = true;
```

### 2.2 WinForms 패턴

**UI 레이아웃 구조**

```
Form1 (1600x1000)
└── SplitContainer (좌: 457px 패널 / 우: 3D 뷰어)
    ├── Panel1 (좌측)
    │   ├── groupBox1 "작업" (Top, 165px) — 메인버튼 + 축버튼 + 서브버튼
    │   ├── groupBox2 "BOM" (Top, 170px) — BOM ListView
    │   ├── groupBox4 "Osnap" (Top, 217px) — Osnap ListView + 추가/삭제/선택보기
    │   ├── groupBox5 "치수" (Top, 188px) — 치수 ListView + 선택보기/삭제
    │   └── groupBox3 "Clash" (Fill) — Clash ListView + 선택보기/전체보기
    └── Panel2 (우측)
        └── panelViewer (Fill) — VIZCore3DControl
```

**Dock 순서 규칙**

- `Controls.Add()` 순서가 Dock 레이아웃에 영향 — Fill은 마지막에 Add
- groupBox3(Clash)는 Fill로 나머지 공간 차지, 다른 그룹은 Top으로 위에서 아래 순서

**버튼 계층 구조**

```
메인 버튼 (190x40, Bold 11pt, 색상 강조):  [파일 열기] [치수 추출]
축 버튼   (120x30, 중간 크기):             [X축] [Y축] [Z축]
서브 버튼 (55~65x25, 기본 폰트):           [BOM] [Clash] [Osnap] [치수] [2D] [PDF]
```

### 2.3 코딩 컨벤션

- **언어**: C# / .NET Framework 4.8
- **네이밍**: PascalCase (메서드/프로퍼티), camelCase (지역변수/필드)
- **버튼 핸들러**: `btn[기능명]_Click` 패턴
- **데이터 클래스**: 파일 하단에 선언 (BOMData, ClashData, ChainDimensionData)
- **에러 처리**: 모든 주요 작업은 try-catch, `Debug.WriteLine`으로 로깅
- **UI 업데이트**: `BeginUpdate()`/`EndUpdate()` 쌍으로 깜빡임 방지
- **주석**: 한국어 사용, XML 문서 주석 `///` 적용

---

## 3. Key Prompt History (시간순)

> 개발 과정에서 사용자가 요청한 프롬프트와 그에 따른 결정/결과를 시간순으로 기록.
> 각 항목은 [요청] → [분석/시도] → [최종 결과] 구조.

---

### Pre-Phase A: 2D-Dimm 프로젝트 — 동료 코드 기반 기능 통합 (세션 abaeeb7d)

> 프로젝트: `C:\Users\duddl\Desktop\2D-Dimm\2D-Dimm`
> 동료가 만든 코드 폴더에서 작업. 개인 프로젝트(`C:\Users\duddl\source\2D_Dimm`)에서 기능을 가져옴.

**[요청 1]** "동료가 만든 코드 폴더에서 작업할건데, 내가 개발하던 코드에서 두 가지를 가져오고 싶어:

1. 모델 윤곽선 표시 기능
2. 치수 표시할 때 한쪽 끝에서 반대쪽 끝까지 순차적으로 거리 표시"

**[구현 결과]**

- Osnap(Object Snap Point) 수집 시스템 구축
  - `vizcore3d.Object3D.GetOsnapPoint(node.Index)` — LINE/CIRCLE/POINT 유형 수집, SURFACE 제외
  - LINE: StartPoint/EndPoint, CIRCLE/POINT: Center 추출
- 체인 치수(Chain Dimension) 자동 생성 로직
  - `MergeCoordinates()` — 0.5mm tolerance 내 좌표 병합
  - `AddChainDimensionByAxis()` — 축별 순차치수 + 전체치수 생성
- Clash 검사 통합
  - `vizcore3d.Clash.Add(pairClash)` — 부재 쌍 간섭 테스트
  - `vizcore3d.Clash.GetResultItem()` — PART 단위 결과 수집
  - HotPoint Z값 기준 정렬
- X-Ray 선택 보기 모드
  - `vizcore3d.View.XRay.Select()` — 특정 부재만 표시
  - Clash 선택 부재 대상 Osnap/치수 재추출

**[핵심 데이터 구조 확립]**

```csharp
ChainDimensionData { Axis, StartPoint, EndPoint, Distance, ViewName }
BOMData { Name, Index, MinX/Y/Z, MaxX/Y/Z }
ClashData { Index1, Index2, Name1, Name2, ZValue }
```

**[치수 표시 방식 결정]**

- `AddCustomAxisDistance(Axis, Vertex3D, Vertex3D)` — 축방향 거리 측정
- MeasureStyle: SIZE10 폰트, 검정색, Bold, 정수표시, 프레임 없음, 투명 배경
- 카메라 방향별 표시할 축 필터링:
  - X방향 보기 → Y, Z축 치수만
  - Y방향 보기 → X, Z축 치수만
  - Z방향 보기 → X, Y축 치수만

**[ShapeDrawing 보조선 첫 구현]**

- `ShapeDrawing.AddLine()` — 확장선/보조선 그리기
- `ShapeDrawing.Clear()` — 새 작업 전 초기화
- `Vertex3DItemCollection`으로 라인 데이터 구성

---

### Pre-Phase B: 2D 제조도면 기능 병합 계획 (세션 08d0e54e)

> 프로젝트: `C:\Users\duddl\Desktop\2D-Dimm\2D-Dimm` (동일)
> 동료 버전에 2D 제조도면 기능을 병합하는 상세 계획 수립.

**[요청]** 동료 프로젝트 버전에 2D 제조도면 기능을 병합하는 구현 계획

**[설계 사항]**

- SilhouetteEdge 렌더링 — 흰색 오브젝트 + 검정 실루엣 엣지로 라인 도면 스타일
- BackgroundRenderingMode — 4방향 직교 뷰 캡처 (400x300)
  - ISO (등각투영, 치수 없음)
  - Z+ 평면도 (X, Y 치수)
  - Y+ 정면도 (X, Z 치수)
  - X+ 측면도 (Y, Z 치수)
- WorldToScreen 좌표 변환 — BOM 번호 위치 계산

**[신규 메서드 계획]**
| 메서드 | 역할 |
|--------|------|
| `DrawPartNumbersOnIsoView()` | ISO 뷰에 원형 번호 + 지시선 표시 |
| `DrawBOMTable()` | BOM 테이블 (No/이름/형식/수량) |
| `DrawTitleBlock()` | 타이틀 블록 (회사/도면명/축척/날짜/작성/승인) |
| `PrintToPDF()` | PDF 출력 |
| `TruncateString()` | 텍스트 말줄임 헬퍼 |
| `AddDimensionsForView()` | 뷰별 치수 추가 (2D 도면용, 검정색) |

**[신규 필드]**

```csharp
private string currentFilePath = "";           // 열린 파일 경로 추적
private Bitmap lastGeneratedDrawing = null;    // 생성된 도면 캐시 (PDF용)
```

**[유지할 기존 기능]**

- AutoProcessModel, ShowAllDimensions (멀티레벨), DrawDimension
- IsTotal 전체치수 플래그, Blue/Red 색상 코딩

---

### Pre-Phase C: ClashTest 교육 프로젝트 — 코드 리뷰 및 파일 구조 확인 (세션 4e3b384c)

> 프로젝트: `C:\Users\duddl\Desktop\삼성중공업\1. 소프트힐스\교육\3. ClashTest\WindowsFormsApp1`
> 교육용 ClashTest 예제 프로젝트 파일 구조 파악.

**[요청]** "지금 내 파일 읽어줄래?" — 프로젝트 파일 전체 리뷰

**[확인된 코드 패턴]**

- VIZCore3D 초기화 패턴 확립:

```csharp
VIZCore3DX.NET.ModuleInitializer.Run();
vizcore3dx = new VIZCore3DX.NET.VIZCore3DXControl();
vizcore3dx.Dock = DockStyle.Fill;
panel1.Controls.Add(vizcore3dx);
vizcore3dx.OnInitializedVIZCore3DX += handler;
```

- 라이선스 인증: `vizcore3dx.License.LicenseServer("127.0.0.1", 8901)`
- Program.cs: 표준 WinForms 진입점 구조

---

### Pre-Phase D: ClashTest — API 문서 참조 개발 + Clash 구현 (세션 ba65803c)

> 프로젝트: 동일 ClashTest 교육 프로젝트
> API 문서 URL을 참조하여 코드 작성.

**[요청 1]** "내가 특정 Document 문서가 작성된 API URL 주소를 주면 너는 그걸 확인하고 코드를 작성해줄 수 있어?"

- URL: `softhills.net/SHDC/VIZCore3D.NET/Help/html/T_VIZCore3D_NET_VIZCore3DControl.htm`
- **결과**: API 문서 확인 후 코드 작성 가능 확인

**[요청 2]** "코드가 어떤건지 주석에 너가 작성했다는 표시를 추가해서 주석 작성할 수 있어?"

- **결과**: `[Claude 작성 주석]` 마크로 XML 문서 주석 추가

**[요청 3]** "code.txt를 참고해서 150번째 줄 이후를 code.txt와 동일하게 작성해달라"

- **결과**: `BtnAddClashtask_Click()` 구현
  - txtGroupA/txtGroupB 입력 검증
  - `ClashTest` 객체 생성
  - `QuickSearch()` 로 3D 오브젝트 그룹 할당
  - Clash 테스트 설정 및 실행

**[발생 오류 및 해결]**
| 오류 | 원인 | 해결 |
|------|------|------|
| CS4033: 'await' operator | async 메서드 밖에서 await 사용 | async 한정자 추가 |
| Line 74 에러 | 모델 로딩 시점 문제 | `vizcore3dx.Model.OpenFileDialog()` 호출 확인 |
| lvTask 컨트롤 누락 | Designer.cs에 ListView 미등록 | Designer 확인 후 추가 |

**[사용 순서 정리]**

1. GUI 실행 → 모델 파일 열기
2. GroupA/GroupB 텍스트 입력
3. Clash 테스트 추가 버튼 클릭
4. 결과 ListView에서 확인

---

### Phase 1: UI 구조 변경 — 발표용 워크플로우 강조

**[요청]** 발표 시 "파일 열기 → 치수 추출" 2단계 흐름을 직관적으로 보여줄 수 있도록 UI 재구성

**[결정]**

- 기존: 7개 버튼이 동일 크기로 나열 → 주요 워크플로우 불명확
- 변경: 3단 계층 구조 도입

**[결과]**

```
메인 버튼 (190x40, Bold 11pt, 색상 강조):  [파일 열기(SteelBlue)] [치수 추출(SeaGreen)]
축 버튼   (120x30, 중간 크기):              [X축] [Y축] [Z축]
서브 버튼 (55~65x25, 기본 폰트):            [BOM] [Clash] [Osnap] [치수] [2D] [PDF]
```

- groupBox1 높이 130→165px 확장
- `btnMainDimension` 신규 버튼 추가 (Osnap→치수→Clash 원클릭 실행)
- `btnOpen_Click` 수정: 파일 열기 → `CollectBOMData()` 만 호출 (이전엔 AutoProcessModel 전체 실행)

---

### Phase 2: 4가지 기능 요청 동시 처리

**[요청 1]** "선택항목만 보기 눌렀다가 전체보기 눌렀을 때 모든 치수가 다시 나오기"

- **결과**: `btnClashShowAll_Click`에 `ShowAllDimensions()` 호출 추가

**[요청 2]** "X축 Y축 Z축 버튼은 메인 버튼 바로 아래로 옮겨서 중간 크기로"

- **결과**: panelDimensionButtons → groupBox1로 이동, y=68, 120x30 크기

**[요청 3]** "모델을 새로 선택했을 때 리스트 다 초기화 하기"

- **결과**: `btnOpen_Click`에 Model.Open() 전 전체 초기화 추가
  - bomList, clashList, osnapPoints, osnapPointsWithNames, chainDimensionList 클리어
  - xraySelectedNodeIndices 클리어
  - 모든 ListView 클리어
  - Review.Measure.Clear(), ShapeDrawing.Clear()

**[요청 4]** "선택좌표보기 버튼 어떻게 작동하는지 확인해서 알려주기"

- **분석 결과**: 좌표에 심볼 표시 + 카메라 이동하는 기능이지만, 둘 다 미작동 상태

---

### Phase 3: 코드 분석 후 6가지 버그 수정

**[요청]** 6개 코드 영역을 분석하고 각각 수정

**1. FitToView 버그 (LvBOM_DoubleClick, LvClash_DoubleClick)**

- 문제: `FitToView()`가 전체 모델로 이동함 (선택 부재가 아님)
- 수정: `FlyToObject3d(indices, 1.2f)` 로 교체 (ratio 2→1.2f, 2는 너무 가까움)

**2. ShowResultSymbol 미작동 (LvClash_DoubleClick)**

- 문제: 오렌지 심볼이 전혀 표시되지 않음
- 수정: 해당 호출 완전 삭제

**3. 선택좌표보기 구현 (btnOsnapShowSelected_Click)**

- 1차: RGB 십자 마커(ShapeDrawing.AddLine) → 크기가 너무 큼
- 2차: `ShapeDrawing.AddSphere(빨간색, radius 5)` 로 변경
- 카메라: `FitToView()` → 가장 가까운 노드 찾아서 `FlyToObject3d(index, 1.2f)` 로 이동

**4. chkMinDimension 삭제**

- 문제: "제작용 최소 치수만 표시" 체크박스가 완전히 미구현
- 수정: UI(Designer) + 필드 선언 + 관련 코드 전부 삭제

**5. ShowResultSymbol 미작동 (LvClash_SelectedIndexChanged)**

- 문제: 오렌지 심볼 표시 코드가 여기서도 작동 안 함
- 수정: 해당 호출 삭제, Osnap/치수 자동 연동 로직만 유지

**6. 빈 이벤트 핸들러 삭제**

- `LvOsnap_SelectedIndexChanged`, `LvDimension_SelectedIndexChanged` — 빈 메서드 + 이벤트 등록 삭제
- `SelectRelatedOsnapItems`/`SelectRelatedDimensionItems`에서 불필요한 이벤트 해제/재등록 제거

---

### Phase 4: X/Y/Z 축 카메라 문제 해결 (4차례 반복)

**[요청]** "X축, Y축, Z축 버튼 누를 떄마다 모델이 점점 멀어지거나 가까워지는 문제"

**시도 1**: `FitToView()` → `MoveCamera()` 순서 변경

- 결과: progressive zoom은 해결, 카메라 방향 안 바뀜

**시도 2**: `MoveCamera()` → `FitToView()` 순서로 변경

- 결과: 카메라 방향은 바뀌지만 progressive zoom 재발

**시도 3**: ShowAllDimensions 내부에서 `Clear → MoveCamera → FitToView` 후 치수 그리기

- 결과: FitToView가 치수선 포함 계산하여 여전히 progressive zoom

**최종 해결**: 카메라 로직을 버튼 핸들러로 완전 분리

```csharp
// 버튼 핸들러
vizcore3d.Review.Measure.Clear();
vizcore3d.ShapeDrawing.Clear();
vizcore3d.View.SetRenderMode(DASH_LINE);
vizcore3d.View.MoveCamera(방향);
vizcore3d.View.FitToView();
ShowAllDimensions("축");  // 여기서는 치수만 그림 (카메라 조작 없음)
```

**핵심 원칙**: `FitToView()`는 치수선도 포함하여 범위를 계산하므로, 반드시 치수 그리기 전에 호출

---

### Phase 5: Clash 더블클릭 선택 해제 문제

**[요청]** "다른 크래시 리스트를 더블클릭하면 기존 선택 부재들의 빨간색이 풀려야 하는데 안 풀림"

**시도 1**: `Object3D.Select(emptyList, false, false)` → 미작동
**시도 2**: `Object3D.Color.RestoreColorAll()` 추가 → 미작동
**상태**: **미해결** — VIZCore3D API 제약으로 추정, 대안 탐색 필요

---

### Phase 6: UI 레이아웃 재배치

**[요청]** "간섭검사 리스트가 제일 아래에 위치해야 함"

**수정**:

- groupBox3 (Clash): `Dock = Fill` → 나머지 공간 모두 차지 (최하단)
- groupBox5 (치수): `Dock = Top`, Height = 150 → 188
- `Controls.Add()` 순서 변경 (Fill을 마지막에 Add)

---

### Phase 7: Git 브랜치 관리

**[요청]** "깃 HE0IN 브랜치에 커밋해줘" → "아 HYI에 넣어달라는 말이였어"

**경과**:

1. HE0IN 브랜치 생성 및 커밋 (실수)
2. HYI 브랜치로 전환
3. HE0IN 브랜치 삭제
4. HYI에 재커밋 + 푸시

**교훈**: 브랜치 이름 확인 후 작업 시작

---

### Phase 8: 카메라 방향 반전 + 렌더 모드

**[요청 1]** "X/Y/Z축에서 바라보는거 반대 방향에서 보게 바꾸는 설정값이 있어?"

- 확인: `CameraDirection` enum에 `X_PLUS/X_MINUS/Y_PLUS/Y_MINUS/Z_PLUS/Z_MINUS` 존재
- **결정**: "X랑 Y만 마이너스로 바꾸자"
- X: `X_PLUS → X_MINUS`, Y: `Y_PLUS → Y_MINUS`, Z: `Z_PLUS` 유지

**[요청 2]** "치수검사하면 은선점선으로 모델링 표현옵션값을 바꾸는 API 확인해볼래?"

- 확인: `SetRenderMode(RenderModes.DASH_LINE)` — enum값 6
- **적용**: X/Y/Z 축 버튼 클릭 시 DASH_LINE 모드로 전환

```
RenderModes: WIRE(0), FLAT(1), SMOOTH(2), EDGE(3), SMOOTH_EDGE(4), HIDDEN_LINE_REMOVAL(5), DASH_LINE(6)
```

---

### Phase 9: 보조선(Extension Line) 문제 — 다수 시도

**[요청]** "X Y Z 축 버튼을 눌렀을 때 보조선이 다 사라지는데... 단일치수가 포인트에서 쭉 치수를 옆으로 뺄 수 있더라고"

**문제**: ShapeDrawing으로 그린 보조선이 축 버튼 클릭 시 사라짐

**시도 1**: ShapeDrawing 보조선 제거 → 원본 좌표로 AddCustomAxisDistance + AssistantLine=true

- 결과: 내장 AssistantLine이 표시되지 않음, 치수가 모델에 붙음

**시도 2**: AddDistance(start, end, position) 3파라미터 사용 제안

- API 문서 확인 결과: 해당 오버로드 존재하지 않음
- AddDistance는 2파라미터(Vector3D, Vector3D) 또는 0파라미터만 있음

**시도 3**: AddCustomDistanceUserAxis(Vertex3D, Vertex3D, Vertex3D) 확인

- axis가 방향 벡터이지 position이 아님 → 치수선 위치 제어 불가

**시도 4**: SetTextPosition(id, position) 사용 제안

- API 문서 확인 결과: MeasureManager에 SetTextPosition 메서드 없음

**시도 5**: UpdatePosition(id, ReviewPosition, Vertex3D) 확인

- 존재하지만 ReviewPosition 클래스가 복잡하여 활용 어려움

**시도 6 (현재)**: 원래 방식으로 복귀

- AddCustomAxisDistance에 오프셋 좌표 전달 + ShapeDrawing.AddLine으로 보조선
- baseOffset: 100→500, levelSpacing: 60→200 으로 증가
- AssistantLine = false (ShapeDrawing 사용 시 비활성화)
- **상태**: 오프셋이 적용되지 않는 문제 디버깅 중
- **가설**: AddCustomAxisDistance가 비측정축 좌표를 무시할 가능성

---

### Phase 10: 데드코드 대규모 정리 (~560줄)

**삭제 항목 상세**:

- `LvOsnap_SelectedIndexChanged` — 빈 메서드 + 이벤트 등록
- `LvDimension_SelectedIndexChanged` — 빈 메서드 + 이벤트 등록
- `ShowResultSymbol` 호출 2건 — LvClash_DoubleClick, LvClash_SelectedIndexChanged
- `chkMinDimension` — 체크박스 UI + 필드 선언 + 관련 로직 (완전 미구현)
- `SelectRelatedOsnapItems`/`SelectRelatedDimensionItems` 내 불필요한 이벤트 해제/재등록

---

### Phase 11: ISO 풍선을 BOM정보 탭 그룹 기준으로 변경

**[요청]** "ISO 버튼 클릭 시 풍선 번호를 작업/데이터 탭 BOM이 아닌 BOM정보 탭의 그룹핑된 데이터 기준으로 표시. 같은 그룹은 대표 1개만 풍선 표시"

**[분석]**

- 기존: `ShowBalloonNumbers()`가 `bomList` 순회하며 각 부재마다 개별 풍선(순번 i+1) 표시
- 목표: `btnCollectBOMInfo_Click()`에서 Item+Size+Matl+Weight로 그룹핑한 번호를 풍선에 사용, 같은 그룹은 첫 번째 부재에만 풍선 표시

**[구현 — 3곳 수정]**

1. **필드 추가** — `bomInfoNodeGroupMap` (Dictionary<int,int>)
   - key: nodeIndex, value: BOM정보 탭 그룹 No
   - `ShowBalloonNumbers`와 `btnCollectBOMInfo_Click` 간 데이터 연결 역할

2. **btnCollectBOMInfo_Click 수정**
   - `rawBomItems` 튜플: `Tuple<string,string,string,string>` → `Tuple<string,string,string,string,int>` (node.Index 추가)
   - 그룹핑 시 `NodeIndices = g.Select(x => x.Item5).ToList()` 추출
   - 그룹핑 후 `bomInfoNodeGroupMap`에 각 nodeIndex → groupNo 매핑 저장

3. **ShowBalloonNumbers 수정**
   - 루프 전에 `balloonDisplayNumbers` (Dictionary<int,int>) 구성
   - `bomInfoNodeGroupMap`에 데이터 있으면: 같은 그룹 중 첫 번째만 등록 (그룹 No)
   - 비어있으면: 기존처럼 `i+1` 순번 (하위호환)
   - 루프 내에서 `balloonDisplayNumbers`에 없는 항목은 `continue`로 스킵
   - `style.SymbolText = balloonDisplayNumbers[i].ToString()` 으로 그룹 번호 표시

**[버그 수정 — 풍선 미표시]**

- 원인: `btnCollectBOMInfo_Click`은 **Part** 노드 인덱스(`GetPartialNode(false, true, false)`)를 저장하는데, `bomList`는 **Body** 노드 인덱스(`GetPartialNode(false, false, true)`)를 사용. Part Index ≠ Body Index이므로 `TryGetValue`가 항상 실패 → 풍선 0개
- 수정: 기존 `bodyToPartNameMap` 구축에 사용된 이진탐색 패턴을 활용하여, Body 노드마다 부모 Part를 찾고 Part의 groupNo를 Body 인덱스에 매핑

**[하위호환]**

- BOM정보 미수집 상태에서 ISO → 기존처럼 개별 순번 풍선 표시
- 풍선 위치 조정(btnBalloonAdjust) 기존 동작 유지

---

### Phase 12: 글로벌 뷰 버튼 (탭 공통 ISO/X/Y/Z)

**[요청]** "ISO, X축, Y축, Z축 버튼을 탭 위에 글로벌하게 배치하여 모든 탭에서 공통으로 사용. 축 버튼 반복 클릭 시 줌 누적 문제 해결. 도면정보 탭에서 시트 선택 후 축 버튼 클릭 시 정상 동작하도록 수정"

**[문제점 분석]**

1. 기존에 축 버튼이 탭별로 분산되어 있음
   - 작업/데이터 탭: `btnShowISO`, `btnShowAxisX/Y/Z` (groupBox1 내부)
   - 도면정보 탭: `btnDrawingISO`, `btnDrawingAxisX/Y/Z` (panelDrawingButtons 내부)
2. 탭 이동 시 축 버튼 동작이 일관되지 않음
3. `FitToView()` + `ZoomRatio` + `ZoomIn()` 조합이 반복 호출될 때마다 줌이 누적됨

**[구현 — UI 변경]**

1. **글로벌 뷰 버튼 패널 추가** (`panelGlobalViewButtons`)
   - 위치: `splitContainer1.Panel1` 최상단 (탭 컨트롤 위)
   - 크기: 457 x 50px, 배경색 `#3C3C3C` (어두운 회색)
   - 버튼 4개: `btnGlobalISO`, `btnGlobalAxisX`, `btnGlobalAxisY`, `btnGlobalAxisZ`
   - 각 버튼 크기 100x34px, FlatStyle, Bold 폰트

2. **기존 탭별 축 버튼 제거**
   - `groupBox1`에서 `btnShowISO/X/Y/Z` 제거, 높이 175→130px로 축소
   - `panelDrawingButtons`에서 `btnDrawingISO/X/Y/Z` 제거, 높이 80→40px로 축소

**[구현 — 로직 변경]**

1. **글로벌 뷰 핸들러 추가** (`ApplyGlobalView`)
   ```csharp
   private void ApplyGlobalView(string viewDirection)
   {
       // 1. 도면정보 탭 + 시트 선택됨 → ApplyDrawingSheetView() 호출
       // 2. X-Ray 선택된 부재 있음 → ApplySelectedNodesView() 호출
       // 3. 그 외 → ApplyFullModelView() 호출 (전체 모델 기준)
   }
   ```

2. **줌 누적 문제 해결**
   - 기존: `FitToView()` → `ZoomRatio=105f` → `ZoomIn()` (3단계, 반복 시 누적)
   - 변경: `FlyToObject3d(indices, 1.0f)` 또는 `FitToView()` 단독 호출 (1단계, 매번 동일)

3. **ShowAllDimensions 수정**
   - 카메라 방향만 설정, 줌 관련 코드 제거 (호출하는 쪽에서 담당)

4. **ApplyDrawingSheetView 수정**
   - X/Y/Z 뷰에서도 `FlyToObject3d(sheet.MemberIndices, 1.0f)` 호출하여 선택 부재에 맞춤

**[파일 변경]**

| 파일 | 변경 내용 |
| ---- | --------- |
| `Form1.Designer.cs` | panelGlobalViewButtons 추가, 기존 축 버튼 제거, groupBox1/panelDrawingButtons 크기 조정 |
| `Form1.cs` | `btnGlobalISO/X/Y/Z_Click`, `ApplyGlobalView`, `ApplySelectedNodesView`, `ApplyFullModelView` 메서드 추가 |

**[동작 흐름]**

```
[글로벌 뷰 버튼 클릭] → ApplyGlobalView(direction)
        │
        ├─ 도면정보 탭 + 시트 선택됨?
        │       └─ Yes → ApplyDrawingSheetView(direction)
        │
        ├─ X-Ray 선택 부재 있음?
        │       └─ Yes → ApplySelectedNodesView(direction)
        │               → FlyToObject3d(선택 부재, 1.0f)
        │               → ISO면 풍선, X/Y/Z면 치수 표시
        │
        └─ 기본 (전체 모델)
                └─ ApplyFullModelView(direction)
                        → FitToView() 한 번만 호출
                        → ISO면 풍선, X/Y/Z면 치수 표시
```

**[기대 효과]**

- 어떤 탭에 있든 상단 글로벌 버튼으로 뷰 전환 가능
- 반복 클릭해도 줌 크기 일정 유지
- 도면정보 탭에서 시트 선택 후 X/Y/Z 뷰 정상 동작

---

### Phase 13: UI 정리 + 도면 시트 로직 개선 + 치수 동기화

**[요청]** "글로벌 뷰가 위자리를 차지해서 탭이 안보임. 치수 목록과 화면 치수 불일치. 도면정보 탭에서 시트 선택 시 기준부재+Clash 연결부재만 보이도록 수정"

**[문제점 분석]**

1. **WinForms Dock 순서 오류**: `panelGlobalViewButtons`(Top)를 먼저 Add하고 `tabControlLeft`(Fill)를 나중에 Add → Fill이 전체를 차지하고 Top이 덮어씀
2. **치수 SmartFiltering**: 축당 5개로 필터링하여 ListView와 화면 표시 개수 불일치
3. **도면 시트 Name 기반 매칭**: Clash.Name과 BOM.Name이 다른 경우 연결 실패

**[해결 — 1. Dock 순서 수정]**

```csharp
// 수정 전 (잘못됨 - Top이 먼저)
this.splitContainer1.Panel1.Controls.Add(this.panelGlobalViewButtons);  // Dock=Top
this.splitContainer1.Panel1.Controls.Add(this.tabControlLeft);          // Dock=Fill

// 수정 후 (올바름 - Fill 먼저, Top 나중 = Top이 먼저 처리됨)
this.splitContainer1.Panel1.Controls.Add(this.tabControlLeft);          // Dock=Fill
this.splitContainer1.Panel1.Controls.Add(this.panelGlobalViewButtons);  // Dock=Top
```

**WinForms Dock 핵심 원칙**: 나중에 Add된 컨트롤이 먼저 Dock 처리됨

**[해결 — 2. SmartFiltering 제거]**

```csharp
// 수정 전 - 필터링 적용
var filteredDims = ApplySmartFiltering(displayList, maxDimensionsPerAxis: 5, minTextSpace: 30.0f);

// 수정 후 - 모든 치수 표시
var filteredDims = displayList;
foreach (var dim in filteredDims)
{
    dim.IsVisible = true;
    dim.DisplayLevel = 0;
}
```

**[해결 — 3. 치수 번호 동기화]**

`ChainDimensionData` 클래스에 `No` 속성 추가:

```csharp
public class ChainDimensionData
{
    public int No { get; set; }  // ListView 번호와 일치
    // ... 기존 속성
}
```

치수 추가 시 번호 설정:
```csharp
int no = 1;
foreach (var dim in chainDimensionList)
{
    dim.No = no;  // 치수 데이터에 번호 저장
    ListViewItem lvi = new ListViewItem(no.ToString());
    // ...
    no++;
}
```

삭제 후 재번호 처리:
```csharp
for (int i = 0; i < lvDimension.Items.Count; i++)
{
    lvDimension.Items[i].Text = (i + 1).ToString();
    if (i < chainDimensionList.Count)
        chainDimensionList[i].No = i + 1;
}
```

**[해결 — 4. 도면 시트 Index 기반 매칭]**

```csharp
// 수정 전 (이름 기반 - 매칭 실패 가능)
Dictionary<string, HashSet<string>> adjacency;
foreach (var clash in clashList)
{
    adjacency[clash.Name1].Add(clash.Name2);  // Name 불일치 시 실패
}

// 수정 후 (Index 기반 - 확실한 매칭)
Dictionary<int, HashSet<int>> adjacencyByIndex;
foreach (var clash in clashList)
{
    if (!bomIndexSet.Contains(clash.Index1) || !bomIndexSet.Contains(clash.Index2))
        continue;  // BOM에 없는 인덱스 무시
    adjacencyByIndex[clash.Index1].Add(clash.Index2);
    adjacencyByIndex[clash.Index2].Add(clash.Index1);
}
```

**[해결 — 5. UI 버튼 배치 정리]**

groupBox1 높이 130→110px 축소, 버튼 재배치:

```
┌─────────────────────────────────────────────────┐
│ [ISO] [X축] [Y축] [Z축]          ← 글로벌 뷰 (42px) │
├─────────────────────────────────────────────────┤
│ 작업/데이터 | 부재정보 | 도면정보 | BOM정보        │
├─────────────────────────────────────────────────┤
│ ┌──────────────┐  ┌──────────────┐              │
│ │  파일 열기    │  │   치수 추출   │              │
│ └──────────────┘  └──────────────┘              │
│ [2D생성] [PDF]    [BOM][Clash][Osnap][치수]      │
└─────────────────────────────────────────────────┘
```

**[파일 변경]**

| 파일 | 변경 내용 |
| ---- | --------- |
| `Form1.Designer.cs` | Dock 순서 수정, groupBox1 높이 110px, 버튼 재배치, 글로벌뷰 패널 42px |
| `Form1.cs` | ChainDimensionData.No 추가, SmartFiltering 제거, Index 기반 도면 시트 생성, 치수 번호 동기화 |

**[도면정보 탭 동작 흐름]**

```
1. 도면 생성 버튼 클릭
   └→ GenerateDrawingSheets()
       ├→ Sheet 1: 전체 BOM 부재
       ├→ Sheet 2~N: 각 부재 + Clash 연결부재 (Index 기반 adjacency)
       └→ 마지막: 설치도 (BFS로 전체 연결)

2. 도면 시트 선택 (lvDrawingSheet)
   └→ LvDrawingSheet_SelectedIndexChanged()
       ├→ X-Ray 모드 활성화
       ├→ sheet.MemberIndices만 X-Ray.Select()
       ├→ xraySelectedNodeIndices에 저장
       └→ Osnap/치수 자동 추출

3. 글로벌 뷰 버튼 클릭 (X/Y/Z)
   └→ ApplyGlobalView()
       └→ xraySelectedNodeIndices 있음?
           └→ ApplySelectedNodesView()
               └→ FlyToObject3d(선택 부재만)
```

**[기대 효과]**

- 글로벌 뷰 버튼 아래에 탭 정상 표시
- ListView의 모든 치수가 화면에 표시됨
- 도면 시트 선택 시 정확한 부재만 X-Ray 표시
- 치수 번호 ListView ↔ 데이터 ↔ 화면 일치

---

### 확인된 API 문서 URL

| API                       | URL                                                                                                               |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| MeasureManager 메서드     | `softhills.net/SHDC/VIZCore3D.NET/Help/html/Methods_T_VIZCore3D_NET_Manager_MeasureManager.htm`                   |
| AddDistance 오버로드      | `softhills.net/SHDC/VIZCore3D.NET/Help/html/Overload_VIZCore3D_NET_Manager_MeasureManager_AddDistance.htm`        |
| AddCustomAxisDistance     | `softhills.net/SHDC/VIZCore3D.NET/Help/html/M_VIZCore3D_NET_Manager_MeasureManager_AddCustomAxisDistance.htm`     |
| AddCustomDistanceUserAxis | `softhills.net/SHDC/VIZCore3D.NET/Help/html/M_VIZCore3D_NET_Manager_MeasureManager_AddCustomDistanceUserAxis.htm` |
| ShapeDrawing.AddLine      | `softhills.net/SHDC/VIZCore3D.NET/Help/html/Overload_VIZCore3D_NET_Manager_ShapeDrawingManager_AddLine.htm`       |
| UpdatePosition            | `softhills.net/SHDC/VIZCore3D.NET/Help/html/M_VIZCore3D_NET_Manager_MeasureManager_UpdatePosition.htm`            |
| MarineAxisManager         | `softhills.net/SHDC/VIZCore3D.NET/Help/html/T_VIZCore3D_NET_Manager_MarineAxisManager.htm`                        |

---

## 4. Business Logic & Workflows

### 4.1 전체 워크플로우

```
[파일 열기] ──→ 모델 로드 + BOM 자동 수집
                    │
[치수 추출] ──→ Osnap 수집 → 체인 치수 생성 → 치수 표시 → Clash 검사
                    │
[X/Y/Z 축] ──→ Clear → 은선점선 모드 → 카메라 방향 → FitToView → 해당 축 치수만 표시
                    │
[Clash 선택보기] → X-Ray 모드 → 해당 부재만 Osnap/치수 재추출 → 치수 표시
                    │
[2D 도면] ──→ 4면도 캡처 → 치수 오버레이 → BOM표 + 타이틀블록 → PNG/PDF 출력
```

### 4.2 BOM 추출 (CollectBOMData)

```
1. vizcore3d.Object3D.FromFilter("BODY") 로 모든 Body 노드 가져오기
2. 각 노드별:
   a. Name, Index 추출
   b. BoundBox → MinX/Y/Z, MaxX/Y/Z 계산
   c. Center = (Min + Max) / 2
   d. BOMData 객체 생성 → bomList 추가
3. ListView에 표시 (이름, 각도, 좌표)
```

### 4.3 Osnap 좌표 수집 (CollectAllOsnap / btnCollectOsnap_Click)

```
1. Body 노드 목록 가져오기 (전체 또는 X-Ray 선택 노드)
2. 각 노드의 Osnap 점 조회:
   - LINE 유형 → Start/End 포인트 수집
   - CIRCLE 유형 → Center 포인트 수집
   - POINT 유형 → Center 포인트 수집
   - SURFACE 유형 → 제외
3. (포인트, 부재이름) 튜플로 저장
4. ListView에 No/부재명/X/Y/Z 표시
```

### 4.4 체인 치수 추출 (AddChainDimensionByAxis)

```
1. Osnap 좌표를 tolerance(0.5mm) 내에서 병합 (MergeCoordinates)
2. 축별(X/Y/Z) 처리:
   a. 필터 축의 최소값만 유지 (중복 제거)
      - X치수: 각 X값에서 최소 Z만 유지
      - Y치수: 각 Y값에서 최소 X만 유지
      - Z치수: 각 Z값에서 최소 Y만 유지
   b. 측정 축 기준 내림차순 정렬
   c. 인접 포인트 쌍으로 순차 치수 생성
   d. 처음~마지막 포인트로 전체 치수 생성 (IsTotal=true)
3. chainDimensionList에 추가 → ListView 표시
```

### 4.5 치수 표시 (ShowAllDimensions → DrawDimension)

```
1. viewDirection에 따라 표시할 축 필터링
   - "X" 방향 보기 → Y, Z축 치수만
   - "Y" 방향 보기 → X, Z축 치수만
   - "Z" 방향 보기 → X, Y축 치수만
2. Smart Filtering (ApplySmartFiltering)
   - 축당 최대 5개, 텍스트 간격 30mm
3. MeasureStyle 설정 (파란색, Bold, 정수표시)
4. globalMin 기준선 계산 (X-Ray 선택 노드 또는 치수 좌표)
5. Level-Based Layout으로 DrawDimension 호출:
   - Level 1 (안쪽): baseOffset (500mm)
   - Level 2 (중간): baseOffset + levelSpacing (700mm)
   - Level 0 전체치수 (바깥): baseOffset + levelSpacing * maxLevel
6. DrawDimension 내부:
   a. 오프셋 좌표 계산 (globalMin - offset)
   b. AddCustomAxisDistance로 치수 생성
   c. 보조선 데이터 수집 (extensionLines)
7. ShapeDrawing.AddLine으로 보조선 일괄 렌더링
```

### 4.6 Clash 검사 (DetectClash → Clash_OnClashTestFinishedEvent)

```
1. Body 노드 전체 가져오기
2. N*(N-1)/2 쌍으로 Clash 테스트 등록
3. 파라미터: Clearance=1mm, Range=1mm, Penetration=1mm
4. 비동기 실행 → OnClashTestFinishedEvent 콜백
5. 결과 수집: 충돌 부재쌍 이름 + Z값
6. Z값 기준 오름차순 정렬 → clashList 저장
7. ListView 표시 + 요약 MessageBox
```

### 4.7 X-Ray 선택 보기 (btnClashShowSelected_Click)

```
1. 선택된 Clash 항목에서 부재 인덱스 추출
2. xraySelectedNodeIndices에 저장
3. X-Ray 모드 활성화 (해당 부재만 표시)
4. 충돌 지점에 노란 삼각형 심볼 표시
5. 선택 부재만 대상으로 Osnap 재수집
6. 선택 부재만 대상으로 치수 재추출
7. ShowAllDimensions()로 치수 표시
```

### 4.8 2D 도면 생성 (btnGenerate2D_Click)

```
1. 렌더 설정: 검정색, DASH_LINE, 실루엣 엣지
2. 4방향 캡처 (400x300):
   - ISO (등각투영, 치수 없음)
   - Z+ (평면도, X/Y 치수)
   - Y+ (정면도, X/Z 치수)
   - X+ (측면도, Y/Z 치수)
3. 1155x615 캔버스 구성:
   - 2x2 뷰 배치 + 치수 오버레이
   - BOM 테이블 (No/이름/형식/수량)
   - 타이틀 블록 (회사/도면명/축척/날짜/작성/승인)
4. 별도 Form에서 미리보기 (저장/인쇄 버튼)
5. PNG/JPEG/PDF 내보내기
```

---

## 5. Pending Tasks & Known Issues

### 5.1 미해결 이슈

| 이슈                   | 상태      | 설명                                                                                                                                     |
| ---------------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| 치수 오프셋 미적용     | 디버깅 중 | AddCustomAxisDistance에 오프셋 좌표를 넘기지만 모델에 붙어서 표시됨. API가 비측정축 좌표를 무시할 가능성                                 |
| 보조선(Extension Line) | 디버깅 중 | ShapeDrawing.AddLine으로 보조선 그리기 시도 중. 오프셋 문제와 연동                                                                       |
| Clash 선택 색상 미해제 | 미해결    | 더블클릭으로 다른 Clash 선택 시 이전 빨간색 하이라이트가 해제 안됨. `RestoreColorAll()` 미동작, `Select(emptyList, false, false)` 미동작 |

### 5.2 기술적 제약사항

**VIZCore3D.NET API 제약**

- `AddDistance`에 position 파라미터 오버로드 없음 — 치수선 위치를 코드로 지정 불가
- `SetTextPosition` 메서드 존재하지 않음
- `AssistantLine` 활성화해도 내장 보조선이 표시되지 않는 경우 있음
- `AddCustomAxisDistance`가 비측정축 좌표를 무시할 가능성 있음 (검증 필요)
- `RestoreColorAll()`이 Select로 적용된 하이라이트를 해제하지 못하는 경우 있음

**ShapeDrawing 제약**

- `AddLine`은 `List<Vertex3DItemCollection>` 타입만 받음 (단일 항목 불가)
- DASH_LINE 렌더 모드에서 ShapeDrawing 표시 여부 확인 필요

**라이선스**

- 로컬 라이선스 서버 필요: `127.0.0.1:8901`

### 5.3 향후 개선 가능 항목

- 치수 오프셋 해결 후 보조선 완성
- Clash 선택 해제 대안 방법 탐색
- 2D 도면 치수 자동 배치 개선
- 모델 회전각도 자동 계산

---

## 6. Data Class Reference

### ChainDimensionData (Line ~3376)

```csharp
string Axis;           // "X", "Y", "Z"
string ViewName;       // "측면도", "정면도", "평면도"
float Distance;        // 거리값 (mm)
Vector3D StartPoint;   // 시작 좌표
Vector3D EndPoint;     // 끝 좌표
string StartPointStr;  // 포맷된 시작 좌표 문자열
string EndPointStr;    // 포맷된 끝 좌표 문자열
bool IsTotal;          // 전체 치수 여부
int Priority;          // 1~10 (10=전체, 8=대형, 5=중형, 3=소형, 1=극소)
int DisplayLevel;      // 레이어 (0=기본, 1+=보조)
bool IsVisible;        // 필터링 후 표시 여부
bool IsMerged;         // 병합 치수 여부
```

### BOMData (Line ~3412)

```csharp
int Index;             // 노드 인덱스
string Name;           // 부재 이름
float RotationAngle;   // 회전 각도 (기본 0.0f)
float CenterX/Y/Z;    // 중심 좌표
float MinX/Y/Z;       // 바운딩박스 최소값
float MaxX/Y/Z;       // 바운딩박스 최대값
```

### ClashData (Line ~3431)

```csharp
int Index1, Index2;    // 충돌 부재 인덱스 쌍
string Name1, Name2;  // 충돌 부재 이름 쌍
float ZValue;          // 충돌 지점 Z좌표
```

---

## 7. Method Index (주요 메서드 라인 번호)

| 라인 | 메서드                             | 역할                                     |
| ---- | ---------------------------------- | ---------------------------------------- |
| 57   | `Form1()`                          | 생성자: UI 초기화, VIZCore3D 컨트롤 생성 |
| 102  | `Vizcore3d_OnInitializedVIZCore3D` | 라이선스, Edge 설정, Clash 이벤트 등록   |
| 128  | `btnOpen_Click`                    | 파일 열기 + 전체 초기화 + BOM 수집       |
| 202  | `btnMainDimension_Click`           | 메인 워크플로우 (Osnap→치수→Clash)       |
| 365  | `CollectBOMData`                   | BOM 데이터 수집                          |
| 452  | `DetectClash`                      | Clash 검사 실행                          |
| 525  | `Clash_OnClashTestFinishedEvent`   | Clash 결과 수집/표시                     |
| 627  | `btnGenerate2D_Click`              | 2D 도면 생성                             |
| 912  | `AddDimensionsForView`             | 2D 뷰에 치수 추가                        |
| 1082 | `Show2DDrawingForm`                | 2D 도면 미리보기 폼                      |
| 1323 | `PrintToPDF`                       | PDF 출력                                 |
| 1403 | `LvBOM_DoubleClick`                | BOM 부재 확대 이동                       |
| 1431 | `LvClash_DoubleClick`              | Clash 부재 확대 이동 + 하이라이트        |
| 1459 | `btnCollectOsnap_Click`            | Osnap 수집                               |
| 1681 | `btnClashShowSelected_Click`       | X-Ray 선택 보기                          |
| 1979 | `btnClashShowAll_Click`            | 전체 보기 복원                           |
| 2128 | `btnOsnapShowSelected_Click`       | 선택 Osnap 빨간 구 마커 표시             |
| 2382 | `btnShowAxisX_Click`               | X축 방향 보기 (Y,Z 치수)                 |
| 2396 | `btnShowAxisY_Click`               | Y축 방향 보기 (X,Z 치수)                 |
| 2409 | `btnShowAxisZ_Click`               | Z축 방향 보기 (X,Y 치수)                 |
| 2430 | `ShowAllDimensions`                | 치수 필터링 + 레벨 배치 + 렌더링         |
| 2572 | `DrawDimension`                    | 단일 치수 + 오프셋 + 보조선              |
| 2647 | `AssignDimensionPriorities`        | 치수 우선순위 점수 할당                  |
| 2715 | `ApplySmartFiltering`              | Greedy 필터링 (겹침 방지)                |
| 2947 | `LvClash_SelectedIndexChanged`     | Clash 선택 시 Osnap/치수 자동 연동       |
| 3109 | `btnExtractDimension_Click`        | 체인 치수 추출                           |
| 3234 | `AddChainDimensionByAxis`          | 축별 순차/전체 치수 생성                 |

---

## 8. Git Branch Strategy

| 브랜치 | 용도                    |
| ------ | ----------------------- |
| `main` | 메인 브랜치             |
| `HYI`  | 현재 개발 브랜치 (활성) |

커밋 시 `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>` 포함.
