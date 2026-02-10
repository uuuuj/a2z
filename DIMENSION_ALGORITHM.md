# Smart Dimension Filtering Algorithm
## 오스냅 치수 가독성 향상 알고리즘 문서

**버전**: 1.0
**작성일**: 2026-02-10
**적용 프로젝트**: A2Z (VIZCore3D.NET 기반 3D 모델 뷰어)

---

## 1. 문제 정의

### 기존 문제점
- 모든 순차 치수를 표시하여 **치수선이 다닥다닥 붙어** 가독성 저하
- 짧은 치수들이 많을 때 레벨이 과도하게 많아짐
- 중요도 구분 없이 모든 치수를 동일하게 표시
- 텍스트 겹침으로 인한 판독 어려움

### 해결 목표
- 제조/설계에 **필수적인 치수만** 선별하여 표시
- 치수 간 **적절한 간격** 유지
- **레벨 기반 정렬**로 깔끔한 배치
- **20년 베테랑 설계사** 수준의 가독성 확보

---

## 2. 적용된 알고리즘

### 2.1 Priority-Based Filtering (우선순위 기반 필터링)

치수의 상대적 크기와 중요도에 따라 1~10점의 우선순위를 할당합니다.

```
Priority 계산 로직:
┌─────────────────────────────────────────────────────────┐
│ 치수 유형              │ 우선순위 │ 설명                 │
├─────────────────────────────────────────────────────────┤
│ 전체 길이 (IsTotal)    │    10    │ 최고 우선순위        │
│ 상위 30% 크기          │     8    │ 주요 구간            │
│ 중간 30% 크기          │     5    │ 중간 구간            │
│ 하위 25% 크기          │     3    │ 작은 구간            │
│ 최하위 15% 크기        │     1    │ 매우 작은 구간       │
│ 병합된 치수            │     6    │ 여러 짧은 치수 통합  │
└─────────────────────────────────────────────────────────┘
```

**참고 문헌**: [Automatic Label Placement - Wikipedia](https://en.wikipedia.org/wiki/Automatic_label_placement)

### 2.2 Greedy Label Placement (탐욕적 레이블 배치)

우선순위가 높은 치수부터 순서대로 배치하면서 겹침을 방지합니다.

```
알고리즘 순서:
1. 전체 치수(IsTotal) 무조건 포함
2. 순차 치수를 우선순위 내림차순 정렬
3. 각 치수에 대해:
   - 기배치된 치수들과 텍스트 중심 거리 계산
   - 최소 간격(30mm) 이상이면 Level 0에 배치
   - 겹치면 Level 1로 배정 (Priority >= 5인 경우만)
   - 그 외는 표시하지 않음
```

**핵심 원리**: 가장 중요한 정보부터 배치하여 정보 손실 최소화

### 2.3 Smart Grouping (스마트 그룹화)

연속된 짧은 치수들을 하나의 누적 치수로 병합합니다.

```
병합 조건:
- 치수 길이 < 최소 텍스트 공간 (25~30mm)
- 연속된 2개 이상의 짧은 치수

병합 결과:
- 시작점: 첫 번째 짧은 치수의 시작점
- 끝점: 마지막 짧은 치수의 끝점
- 우선순위: 6 (중간 높음)
```

**예시**:
```
기존: 5mm + 3mm + 4mm + 8mm (4개 치수)
병합: 20mm (1개 누적 치수)
```

### 2.4 Multi-Level Layout (멀티레벨 레이아웃)

치수를 중요도에 따라 레벨별로 배치하여 시각적 계층 구조를 형성합니다.

```
┌────────────────────────────────────────────────────────┐
│                                                        │
│     [모델]                                             │
│                                                        │
│ ├──────────────────────────────────────────────────┤   │
│ │              Level 1: 주요 치수                    │   │
│ ├──────────────────────────────────────────────────┤   │
│ │              Level 2: 보조 치수                    │   │
│ ├──────────────────────────────────────────────────┤   │
│ │              Level 0: 전체 길이                    │   │
│ └──────────────────────────────────────────────────┘   │
│                                                        │
└────────────────────────────────────────────────────────┘

레벨 간격:
- 기본 오프셋: 100mm (모델에서 첫 치수선까지)
- 레벨 간격: 60mm
```

---

## 3. 설정 파라미터

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| `maxDimensionsPerAxis` | 5 | 축당 최대 표시 치수 개수 |
| `minTextSpace` | 30.0mm | 치수 텍스트 간 최소 간격 |
| `baseOffset` | 100.0mm | 모델에서 첫 치수선까지 거리 |
| `levelSpacing` | 60.0mm | 치수선 레벨 간 간격 |

---

## 4. 구현된 메서드

### 핵심 메서드

| 메서드 | 역할 |
|--------|------|
| `AssignDimensionPriorities()` | 치수 우선순위 계산 및 할당 |
| `ApplySmartFiltering()` | 스마트 필터링 적용 (메인) |
| `MergeShortDimensions()` | 짧은 치수 병합 |
| `CreateMergedDimension()` | 병합 치수 객체 생성 |
| `ShowAllDimensions()` | 치수 표시 (개선됨) |

### ChainDimensionData 확장 속성

```csharp
public int Priority { get; set; }      // 우선순위 (1~10)
public int DisplayLevel { get; set; }  // 표시 레벨 (0, 1, 2)
public bool IsVisible { get; set; }    // 표시 여부
public bool IsMerged { get; set; }     // 병합 치수 여부
```

---

## 5. 개선 효과

### Before (기존)
- 모든 치수 표시 → 복잡하고 읽기 어려움
- 짧은 치수마다 별도 레벨 → 레벨 과다
- 고정 오프셋 → 일관성 없는 배치

### After (개선)
- 중요 치수만 선별 → 깔끔하고 명확함
- 짧은 치수 병합 → 레벨 최소화
- 동적 레이아웃 → 일관된 배치

---

## 6. 참고 자료

### 학술 자료
- [Automatic Label Placement (Wikipedia)](https://en.wikipedia.org/wiki/Automatic_label_placement)
- [Labeling Algorithms - Graph Drawing Handbook](https://cs.brown.edu/people/rtamassi/gdhandbook/chapters/labeling.pdf)
- [Fast Point-Feature Label Placement (ResearchGate)](https://www.researchgate.net/publication/220586600_Fast_Point-Feature_Label_Placement_for_Dynamic_Visualizations)

### CAD 표준
- [10 Rules for Accurate CAD Dimensioning (BIM Heroes)](https://bimheroes.com/10-rules-for-accurate-cad-dimensioning/)
- [AutoCAD Annotative Objects (RobotechCAD)](https://robotechcad.com/blog/autocad-tips-mastering-annotative-objects-for-text-and-dimensions-in-autocad/)

### VIZCore3D.NET API
- [MeasureManager Class](https://softhills.net/SHDC/VIZCore3D.NET/Help/html/T_VIZCore3D_NET_Manager_MeasureManager.htm)
- [VIZCore3D.NET API Document](https://softhills.net/SHDC/VIZCore3D.NET/Help/html/72096ccb-07bf-4d48-a351-53f5b3d4ac10.htm)

---

## 7. 향후 개선 사항

1. **Occupancy Bitmap 기반 겹침 감지**: 더 정밀한 텍스트 영역 충돌 감지
2. **사용자 설정 UI**: 파라미터를 UI에서 조정 가능하도록
3. **치수 스타일 프리셋**: 제조용/검토용/프레젠테이션용 프리셋
4. **AI 기반 치수 선택**: 부품 유형에 따른 자동 치수 선별

---

*이 문서는 A2Z 프로젝트의 오스냅 치수 표시 개선 작업을 위해 작성되었습니다.*
