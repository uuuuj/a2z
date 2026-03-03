# A2Z 모듈별 기능 분석서

> 3D CAD 모델 자동 분석 및 제조용 2D 도면 생성 시스템
> VIZCore3D.NET + C# WinForms (.NET Framework 4.8)
> 분석일: 2026-02-23

---

## 1. 모듈 요약 테이블

| No | 모듈명 | 설명 | 주요 메서드 수 | 상태 |
|----|--------|------|----------------|------|
| 1 | 파일/모델 관리 | 3D 모델 파일 열기 및 초기화 | 2 | 완료 |
| 2 | BOM 수집 | 부재 목록 및 바운딩박스 정보 수집 | 3 | 완료 |
| 3 | Osnap 좌표 수집 | 객체 스냅 좌표 수집/추가/삭제 | 6 | 완료 |
| 4 | 치수 추출/표시 | 체인 치수 생성 및 화면 표시 | 7 | 완료 |
| 5 | Clash 간섭 검사 | 부재 간 충돌 검사 및 결과 표시 | 4 | 완료 |
| 6 | 뷰 제어 | 카메라 방향 전환 및 렌더 모드 | 5 | 완료 |
| 7 | 2D 도면 생성 | 4면도 캡처 및 도면 구성 | 6 | 완료 |
| 8 | 부재 정보 (Attribute) | 선택 부재 속성 조회 | 7 | 완료 |
| 9 | UDA 관리 | 사용자 정의 속성 CRUD | 4 | 완료 |
| 10 | PDF/이미지 출력 | PNG/JPEG/PDF 내보내기 | 2 | 완료 |
| 11 | 도면 시트 관리 | 부재별 도면 시트 생성 | 4 | 완료 |
| 12 | 풍선 번호 표시 | BOM 풍선 번호 오버레이 | 3 | 완료 |

---

## 2. 모듈별 상세 기능

### 2.1 파일/모델 관리 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 파일 열기 | `btnOpen_Click` | 260 | .vizx/.viz 파일 열기, 기존 데이터 초기화, BOM 자동 수집 |
| VIZCore3D 초기화 | `Vizcore3d_OnInitializedVIZCore3D` | 231 | 라이선스 설정, Edge 데이터 활성화, 이벤트 등록 |

**주요 기능:**
- VIZCore3D.NET 라이선스 서버 연결 (127.0.0.1:8901)
- 파일 열기 시 모든 데이터 자동 초기화
- Edge 데이터 생성/로드 활성화 (2D 도면용)
- 실루엣 엣지 활성화 (외곽선 표시)

---

### 2.2 BOM 수집 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| BOM 데이터 수집 | `CollectBOMData` | - | Body 노드에서 이름, 인덱스, 바운딩박스 추출 |
| BOM 버튼 | `btnCollectBOM_Click` | 1448 | BOM 수집 실행 |
| BOM 정보 버튼 | `btnCollectBOMInfo_Click` | 1464 | BOM 상세 정보 수집 |
| Body→Part 매핑 | `BuildBodyToPartNameMap` | 138 | Body 인덱스를 Part 이름으로 매핑 |
| Part 이름 조회 | `GetPartNameFromBodyIndex` | 200 | Body 인덱스로 Part 이름 조회 |

**BOMData 구조:**
```
Index, Name, RotationAngle, CenterX/Y/Z, MinX/Y/Z, MaxX/Y/Z
```

---

### 2.3 Osnap 좌표 수집 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 전체 Osnap 수집 | `CollectAllOsnap` | 414 | 모든 Body 노드의 Osnap 좌표 수집 |
| Osnap 버튼 | `btnCollectOsnap_Click` | 2616 | Osnap 수집 실행 및 ListView 표시 |
| 좌표 수동 추가 | `btnOsnapAdd_Click` | 3187 | 사용자가 직접 좌표 선택하여 추가 |
| 픽킹 이벤트 | `GeometryUtility_OnOsnapPickingItem` | 3209 | Osnap 픽킹 완료 이벤트 처리 |
| 좌표 삭제 | `btnOsnapDelete_Click` | 3251 | 선택된 Osnap 좌표 삭제 |
| 선택 좌표 표시 | `btnOsnapShowSelected_Click` | 3300 | 선택 좌표에 빨간 구 마커 표시 |
| 풍선 지우기 | `btnOsnapClearBalloon_Click` | 3406 | Osnap 마커 클리어 |

**수집 좌표 유형:**
- LINE: StartPoint, EndPoint
- CIRCLE: Center
- POINT: Center
- SURFACE: 제외

---

### 2.4 치수 추출/표시 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 메인 치수 추출 | `btnMainDimension_Click` | 341 | Osnap → 치수 → Clash 원클릭 실행 |
| 치수 추출 버튼 | `btnExtractDimension_Click` | 5070 | 체인 치수 데이터 생성 |
| 축별 치수 생성 | `AddChainDimensionByAxis` | - | X/Y/Z축별 순차치수 + 전체치수 생성 |
| 치수 표시 | `ShowAllDimensions` | 4048 | 필터링 + 레벨 배치 + 화면 렌더링 |
| 치수 그리기 | `DrawDimension` | 4488 | 단일 치수선 + 오프셋 + 보조선 그리기 |
| 우선순위 할당 | `AssignDimensionPriorities` | 4607 | 치수 크기별 우선순위 점수 할당 |
| 선택 치수 표시 | `btnDimensionShowSelected_Click` | 3419 | 선택된 치수만 화면에 표시 |
| 치수 삭제 | `btnDimensionDelete_Click` | 3536 | 선택된 치수 삭제 |

**ChainDimensionData 구조:**
```
Axis, ViewName, Distance, StartPoint, EndPoint, IsTotal, Priority, DisplayLevel, IsVisible
```

**치수 표시 규칙:**
- X방향 보기 → Y, Z축 치수만
- Y방향 보기 → X, Z축 치수만
- Z방향 보기 → X, Y축 치수만

---

### 2.5 Clash 간섭 검사 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| Clash 검사 실행 | `DetectClash` | - | 전체 부재 쌍 간섭 테스트 (비동기) |
| Clash 버튼 | `btnClashDetection_Click` | 1646 | Clash 검사 수동 실행 |
| 검사 완료 이벤트 | `Clash_OnClashTestFinishedEvent` | 1662 | 결과 수집, Z값 정렬, ListView 표시 |
| Clash 더블클릭 | `LvClash_DoubleClick` | 2588 | 충돌 부재로 카메라 이동 + 하이라이트 |
| 선택 항목 보기 | `btnClashShowSelected_Click` | 2842 | X-Ray 모드로 선택 부재만 표시 |
| 전체 보기 복원 | `btnClashShowAll_Click` | 3144 | X-Ray 해제, 전체 치수 재표시 |
| 선택 변경 이벤트 | `LvClash_SelectedIndexChanged` | 4907 | Osnap/치수 자동 연동 |

**ClashData 구조:**
```
Index1, Index2, Name1, Name2, ZValue
```

**검사 파라미터:**
- Clearance: 1mm
- Range: 1mm
- Penetration: 1mm

---

### 2.6 뷰 제어 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| X축 방향 보기 | `btnShowAxisX_Click` | 3604 | X_MINUS 방향 + Y,Z 치수 |
| Y축 방향 보기 | `btnShowAxisY_Click` | 3613 | Y_MINUS 방향 + X,Z 치수 |
| Z축 방향 보기 | `btnShowAxisZ_Click` | 3622 | Z_PLUS 방향 + X,Y 치수 |
| ISO 보기 | `btnShowISO_Click` | 3631 | 등각투영 뷰 |
| 선택 노드 Osnap 수집 | `CollectOsnapForSelectedNodes` | 2960 | X-Ray 선택 노드만 Osnap 수집 |
| 선택 노드 치수 추출 | `ExtractDimensionForSelectedNodes` | 3084 | X-Ray 선택 노드만 치수 추출 |

**렌더 모드:**
- DASH_LINE (은선점선): 치수 검사용
- SMOOTH_EDGE: 기본 렌더링

---

### 2.7 2D 도면 생성 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 2D 도면 생성 | `btnGenerate2D_Click` | 1770 | 4면도 캡처 + 치수 오버레이 + BOM표 |
| 뷰별 치수 추가 | `AddDimensionsForView` | 2055 | 2D 뷰에 치수 오버레이 |
| 도면 미리보기 | `Show2DDrawingForm` | 2239 | 별도 폼에서 미리보기 (저장/인쇄) |
| 부재 번호 표시 | `DrawPartNumbersOnIsoView` | 2289 | ISO 뷰에 원형 번호 + 지시선 |
| BOM 테이블 | `DrawBOMTable` | 2367 | No/이름/형식/수량 테이블 |
| 타이틀 블록 | `DrawTitleBlock` | 2441 | 회사/도면명/축척/날짜/작성/승인 |
| 홀 검출 | `DetectHoles` | 653 | 원형 부재 자동 검출 |

**도면 구성:**
- 캔버스: 1155x615 픽셀
- 4방향 뷰: ISO, 평면도(Z+), 정면도(Y+), 측면도(X+)
- 각 뷰: 400x300 픽셀

---

### 2.8 부재 정보 (Attribute) 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 속성 테이블 설정 | `SetupAttributeColumns` | 210 | DataGridView 컬럼 구성 |
| 선택 이벤트 | `Object3D_OnObject3DSelected` | 5338 | 3D 객체 선택 시 속성 갱신 |
| 속성 테이블 갱신 | `UpdateAttributeTable` | 5363 | 선택 노드 속성 표시 |
| 기본 노드 정보 | `AddBasicNodeInfo` | 5390 | 이름, 인덱스, 타입 등 |
| 바운딩박스 정보 | `AddBoundingBoxInfo` | 5420 | Min/Max/Center 좌표 |
| UDA 정보 | `AddUDAInfo` | 5458 | 사용자 정의 속성 |
| 형상 속성 정보 | `AddGeometryPropertyInfo` | 5499 | 면적, 부피, 무게 등 |
| 섹션 헤더 추가 | `AddSectionHeader` | 5546 | 테이블 섹션 구분선 |
| 테이블 클리어 | `ClearAttributeTable` | 5557 | 속성 테이블 초기화 |
| 선택 해제 | `btnClearSelection_Click` | 5567 | 선택 및 속성 클리어 |
| CSV 내보내기 | `btnExportAttributeCSV_Click` | 5576 | 속성 데이터 CSV 저장 |

---

### 2.9 UDA 관리 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| UDA 추가 | `btnUdaAdd_Click` | 5683 | 새 사용자 정의 속성 추가 |
| UDA 편집 | `btnUdaEdit_Click` | 5709 | 기존 UDA 값 수정 |
| UDA 삭제 | `btnUdaDelete_Click` | 5762 | UDA 삭제 |
| UDA CSV 가져오기 | `btnUdaImportCSV_Click` | 5804 | CSV에서 UDA 일괄 가져오기 |

---

### 2.10 PDF/이미지 출력 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| PDF 내보내기 | `btnExportPDF_Click` | 2793 | 현재 뷰 PDF 저장 |
| PDF 인쇄 | `PrintToPDF` | 2480 | Microsoft Print to PDF 사용 |

**지원 형식:**
- PNG
- JPEG
- PDF (Microsoft Print to PDF)

---

### 2.11 도면 시트 관리 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 제조도면 생성 | `btnMfgDrawing_Click` | 5943 | 부재별 제조도면 생성 |
| 부재 가시성 복원 | `RestoreAllPartsVisibility` | 6277 | 모든 부재 표시 |
| 도면 시트 생성 | `GenerateDrawingSheets` | 6294 | 시트 데이터 생성 |
| 시트 생성 버튼 | `btnGenerateSheets_Click` | 6443 | 도면 시트 수동 생성 |
| 시트 선택 변경 | `LvDrawingSheet_SelectedIndexChanged` | 6470 | 시트 선택 시 뷰 갱신 |
| 시트 뷰 적용 | `ApplyDrawingSheetView` | 6536 | 시트 방향별 뷰 적용 |
| 도면 ISO 보기 | `btnDrawingISO_Click` | 6606 | 도면 시트 ISO 뷰 |
| 도면 X축 보기 | `btnDrawingAxisX_Click` | 6611 | 도면 시트 X축 뷰 |
| 도면 Y축 보기 | `btnDrawingAxisY_Click` | 6616 | 도면 시트 Y축 뷰 |
| 도면 Z축 보기 | `btnDrawingAxisZ_Click` | 6621 | 도면 시트 Z축 뷰 |
| 설치 치수 추출 | `ExtractInstallationDimensions` | 6630 | 부재별 설치 치수 |

---

### 2.12 풍선 번호 표시 모듈

| 기능 | 메서드명 | 라인 | 설명 |
|------|----------|------|------|
| 풍선 번호 표시 | `ShowBalloonNumbers` | 3650 | BOM 풍선 번호 오버레이 |
| 풍선 위치 조정 | `btnBalloonAdjust_Click` | 3912 | 풍선 위치 수동 조정 |

---

## 3. 데이터 클래스

### BOMData
```csharp
int Index;             // 노드 인덱스
string Name;           // 부재 이름
float RotationAngle;   // 회전 각도
float CenterX/Y/Z;     // 중심 좌표
float MinX/Y/Z;        // 바운딩박스 최소값
float MaxX/Y/Z;        // 바운딩박스 최대값
```

### ClashData
```csharp
int Index1, Index2;    // 충돌 부재 인덱스 쌍
string Name1, Name2;   // 충돌 부재 이름 쌍
float ZValue;          // 충돌 지점 Z좌표
```

### ChainDimensionData
```csharp
string Axis;           // "X", "Y", "Z"
string ViewName;       // "측면도", "정면도", "평면도"
float Distance;        // 거리값 (mm)
Vector3D StartPoint;   // 시작 좌표
Vector3D EndPoint;     // 끝 좌표
bool IsTotal;          // 전체 치수 여부
int Priority;          // 1~10 우선순위
int DisplayLevel;      // 레이어 레벨
bool IsVisible;        // 표시 여부
```

### DrawingSheetData
```csharp
// 부재별 도면 시트 데이터
```

---

## 4. UI 구조

```
Form1 (1600x1000)
└── SplitContainer (좌: 457px 패널 / 우: 3D 뷰어)
    ├── Panel1 (좌측)
    │   ├── groupBox1 "작업" — 메인버튼 + 축버튼 + 서브버튼
    │   ├── groupBox2 "BOM" — BOM ListView
    │   ├── groupBox4 "Osnap" — Osnap ListView + 추가/삭제/선택보기
    │   ├── groupBox5 "치수" — 치수 ListView + 선택보기/삭제
    │   ├── groupBox3 "Clash" — Clash ListView + 선택보기/전체보기
    │   └── Tab: 부재 정보, UDA, 도면 시트
    └── Panel2 (우측)
        └── panelViewer — VIZCore3DControl
```

---

## 5. 워크플로우

```
[파일 열기] → 모델 로드 + BOM 자동 수집
      │
[치수 추출] → Osnap 수집 → 체인 치수 생성 → 치수 표시 → Clash 검사
      │
[X/Y/Z 축] → Clear → 은선점선 모드 → 카메라 방향 → FitToView → 해당 축 치수만 표시
      │
[Clash 선택보기] → X-Ray 모드 → 해당 부재만 Osnap/치수 재추출 → 치수 표시
      │
[2D 도면] → 4면도 캡처 → 치수 오버레이 → BOM표 + 타이틀블록 → PNG/PDF 출력
```

---

## 6. 미해결 이슈

| 이슈 | 상태 | 설명 |
|------|------|------|
| 치수 오프셋 미적용 | 디버깅 중 | AddCustomAxisDistance에 오프셋 좌표를 넘기지만 모델에 붙어서 표시됨 |
| 보조선(Extension Line) | 디버깅 중 | ShapeDrawing.AddLine으로 보조선 그리기 시도 중 |
| Clash 선택 색상 미해제 | 미해결 | 더블클릭으로 다른 Clash 선택 시 이전 빨간색 하이라이트가 해제 안됨 |

---

## 7. 기술 스택

- **언어**: C# / .NET Framework 4.8
- **UI**: Windows Forms
- **3D 엔진**: VIZCore3D.NET
- **라이선스**: 로컬 라이선스 서버 (127.0.0.1:8901)
- **출력**: PNG, JPEG, PDF (Microsoft Print to PDF)
