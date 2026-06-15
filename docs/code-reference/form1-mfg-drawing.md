# Form1.MfgDrawing.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.MfgDrawing.cs` (약 2,357 라인)

**책임**: 가공도 3D 미리보기, 엑셀 템플릿 기반 PDF 출력, 카메라 방향 결정, Osnap 치수와 풍선 생성, EA 앵글 상하 2뷰 배치.

## 주요 핸들러

| 핸들러 | 라인 | 흐름 문서 |
|---|---:|---|
| <a id="btnMfgDrawingSheet_Click"></a>`btnMfgDrawingSheet_Click` | L1697 | [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md) |

옛 `btnMfgDrawing_Click` 핸들러는 제거됐다. 시트 선택 미리보기는 `LvDrawingSheet_SelectedIndexChanged`가 `ExecuteMfgDrawing`을 호출한다.

## 활성 핵심 메서드

### <a id="CaptureMfgSceneToViewArea"></a>CaptureMfgSceneToViewArea

- **라인**: L242
- 은선 모델을 현재 카메라에서 2D로 캡처한다.
- 지정된 View 영역에 맞게 스케일을 계산하고 중앙 배치한다.
- ShapeDrawing, Note, Measure를 2D 객체로 변환한다.

### <a id="BuildEaSecondaryScene"></a>BuildEaSecondaryScene

- **라인**: L350
- EA 두 번째 뷰를 독립 카메라에서 생성한다.
- 최장축 Z는 `X_MINUS + 화면축 90도`, 나머지는 `Z_MINUS`를 사용한다.
- 최장축의 체인 치수와 전체 치수만 생성한다.
- 과거 T자형 원인이던 추가 정렬 회전은 사용하지 않는다.

### <a id="RenderMfgRowToViewArea"></a>RenderMfgRowToViewArea

- **라인**: L542
- 일반 부재는 View 영역 전체에 한 뷰를 배치한다.
- EA 부재는 View 영역을 위·아래로 분할한다.
- 첫 번째 뷰는 최장축 치수를 예약하고, 두 번째 뷰가 해당 치수를 담당한다.
- 두 번째 뷰 실패 시 불완전한 객체를 삭제하고 첫 번째 뷰를 유지한다.

### <a id="BuildMfgSceneCore"></a>BuildMfgSceneCore

- **라인**: L673
- 대상 부재 격리, BBox 최장축 판정, PAD/PLATE 카메라 선택을 수행한다.
- `ORIENTATION` UDA와 EA 열린 방향 보정을 적용한다.
- LINE/POINT Osnap 수집, 뒷면 필터, 체인 치수, 풍선과 보조선을 생성한다.
- `reserveLongestAxisForSecondary=true`이면 EA 첫 번째 뷰에서 최장축 치수를 생략한다.

### <a id="ExecuteMfgDrawing"></a>ExecuteMfgDrawing

- **라인**: L1411
- 가공도 시트 선택 시 단일 3D 미리보기를 만든다.
- `BuildMfgSceneCore` 결과의 Z90/R180 회전을 적용한다.
- 결과 카메라 정보를 `_lastMfgViewPose`에 저장한다.

### <a id="GenerateMfgDrawingManual"></a>GenerateMfgDrawingManual

- **라인**: L1497
- 전체 가공도 BOM을 수집하고 5개씩 페이지로 나눈다.
- `사용자템플릿_엑셀_가공도.xlsx`를 가져와 각 View 영역을 렌더한다.
- 페이지별 PDF를 실행 파일 하위 `Drawings`에 저장한다.
- 출력 후 BOM UI와 선택 시트 가시성을 복원한다.

### <a id="IsAngleFromSpref"></a>IsAngleFromSpref

- **라인**: L2144
- 부모 방향으로 최대 10단계 탐색해 `SPREF`를 읽는다.
- `/` 제거 후 `:` 앞 ITEM이 `EA`로 시작하면 앵글 부재로 판정한다.

### <a id="FilterHiddenLineOsnap"></a>FilterHiddenLineOsnap

- **라인**: L2164
- 카메라 깊이축의 뒤쪽 15% 영역에 있는 Osnap을 제외한다.
- PLUS/MINUS 카메라에 따라 제거 방향을 반대로 적용한다.

### <a id="ApplyOrientationRotation"></a>ApplyOrientationRotation

- **라인**: L2330
- `ORIENTATION` UDA 각도를 화면 Z축 회전으로 적용한다.

## 보조·구형 메서드

| 메서드 | 라인 | 상태 |
|---|---:|---|
| `RestoreAllPartsVisibility` | L23 | 활성, 출력 후 가시성 복원 |
| `GenerateMfgDrawing2DAll` | L1754 | 구형 그리드 출력, 현재 호출자 없음 |
| `RenderMfgViewForDrawing` | L1948 | 구형 그리드 셀 렌더, 현재 호출자 없음 |
| `ParseOrientation` | L2281 | 활성 |

## 관련 문서

- [가공도 시트 PDF 출력](../기능/가공도/가공도%20시트.md)
- [가공도 시트 3D 미리보기](../기능/가공도/가공도%20단일.md)
