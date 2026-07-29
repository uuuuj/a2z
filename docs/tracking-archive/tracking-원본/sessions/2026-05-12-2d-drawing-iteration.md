# 2026-05-12 — 2D 도면 출력 다회 반복 튜닝 (엑셀 PoC + T-005/T-038/T-039)

## 주제
하루 만에 **20개 commit** 마라톤 반복. 엑셀 템플릿 PoC(Step 1~3.5) → 결국 SDK 한계로 일시 보류 → T-005(외곽 방향) + T-038/T-039(모델 스케일·보조선 길이) 본격 구현 → 사용자 사양 v1~v10 단계별 조정 + ISO 풍선 좌/우·외곽 절반 + BOM·tableInfo 미세 이동 + 라벨 영역 명시 차단(SetGridCellMargins).

## 배경
직전 세션(2026-05-04)에 회사 doc 13건 + 사용자 정리 11건 매핑 후 트래킹 재구조화 완료. 이번 세션 시작 시 사용자 우선순위 3가지:
1. 엑셀 템플릿을 2D View에 적용
2. 보조선 길이 고정
3. 치수 텍스트 위치 변경

엑셀 PoC가 SDK 한계로 막힘 → 사용자 결정에 따라 T-005/T-038/T-039로 전환 + 반복 조정.

## 한 일

### 엑셀 템플릿 PoC (REQ-002 / T-012) — 5 commit
| Step | 결과 |
|---|---|
| Step 1 (`702ae85`) | `ImportExcel` 단독 — SDK 트리에 등록만, 캔버스 빔 |
| Step 1.5 (`af9fbd9`) | `Set2DViewDefaultTemplate(int)` 인덱스 시도 — DSME 0/1/2만 정상, 3+ 빈 outline |
| Step 2 (`a7ab4c4`) | Reflection으로 internal `Draw2DViewTemplate(string)` 호출 — 예외 없이 "성공"이지만 silent fail (SDK obfuscation 보호) |
| Step 3 (`6ec73a4`) | JSON 직접 파싱 + `ShapeDrawing.AddLine` + `Add2DObjectFromShapeDrawing` — Line 10/1539 추가 OK이지만 캔버스 빔 |
| Step 3.5 (`be2a920`) | 사용자 지적 — `ToolbarDrawing2D.Visible / ViewMode=Both / SetCanvasSize / SetSelectCanvas / CrateTemplateBorder` 시퀀스 추가 — **여전히 적용 안 됨** |

**결론**: SDK 사용자 추가 템플릿 자동 적용은 외부에서 호출 불가. 옵션 A(JSON 직접 파싱) 본진 가능하나 *모델·BOM·풍선 좌표 매핑 작업 추가 필요*. **사용자 결정으로 일시 보류**.

### T-005 외곽 방향 자동 판정 (`4ec47c8`)
- 헬퍼 `ComputePositiveOffsetByOsnapExtreme(values, modelCenter)` 신설
- 알고리즘: 부호 있는 거리 비교 `(omax - center) >= (center - omin)`
- 5곳 패턴 교체: Dimensions.cs L499, MfgDrawing.cs L335·1057·1192·1707
- `AddChainDimensionByAxis` 시그니처 무변경

### T-038+T-039 보조선 길이 + 모델 스케일 — **9 commit (v1~v10)**

| 커밋 | 변경 |
|---|---|
| `4ec47c8` (v1) | 1단=50mm, 2단=100mm 고정. `EstimateFitScaleForCell` 헬퍼 + `canvasScaleOverride` 전달 |
| `ffea5e8` (v2) | 사용자 사양: max>1000 → 10/20, ≤1000 → 20/40 |
| `60a5ee6` (B-2) | `targetH 0f → FitObjectToGridCellAspect`만 → 너무 컸음 → 추가 0.85배 |
| `8380b70` (v3) | 짧은 축(1/3 이하) 보조선 절반 + 모델 0.75 + 텍스트 5배 |
| `b57f1be` | T-044 시초 (X/Y/Z 셀 풍선 차단) + 텍스트 5배→2배 |
| `6f38e1b` (v4) | 모델 이동 — 보조선 반대 방향, 화면 H/V 매핑 + `_lastModelShiftCanvasX/Y` |
| `9df4c63` (v5) | 위아래 무조건 절반 + 이동량 × 0.5 |
| `a538a4e` (v6) | Y뷰 dx 부호 반전(`hSign=-1`) + ShiftScale 0.5→0.25 (라벨 침범 방지) |
| `7081f84` (step C) | **SetGridCellMargins로 라벨 영역 12mm 명시 차단** — 핵심 해결책 |
| `40fdc94` (v7) | BOM/tableInfo 1mm 이동 + 짧은 축 기준 1/3→1/2 + V 비대칭 ShiftScale |
| `74310da` (v8) | 뷰별 차등 vShiftScale: Z=0.5 / X·Y=0.75 |
| `79401cc` (v9) | **버그 차단** — `RenderSheetViewForDrawing` 진입부 `_lastModelShift` 초기화 (ISO 잔존 이동 버그) + Z뷰 0.70 |
| `67d6f4f` | ISO 풍선 외곽 부재(normalizedDist>0.5) 거리 절반 |
| `aa417b2` | ISO 풍선 좌/우만 + BOM·tableInfo 누적 이동 (위 1+오른쪽 1 / 위 2+오른쪽 1) |
| `09515ff` (v10) | **단순화** — 모든 보조선 5/10mm 고정, 모든 분기 폐기 |

### 주요 결정 포인트
- **엑셀 PoC 보류 결정** — SDK obfuscation으로 외부 호출 불가 확정. Softhills 질의 또는 옵션 A 본진 후속
- **라벨 침범 명시 차단** — 사용자 *"명시적으로 침범하지 못하게는 못하려나?"* → `SetGridCellMargins(row, col, l, r, t, bottom=12)` SDK API 활용. *FitObjectToGridCellAspect가 마진 제외 fit*
- **이동량 비대칭 (Z vs X/Y)** — Z뷰는 0.5 OK, X/Y는 0.75 필요 (사용자 결과 보고)
- **단순화 결정 (v10)** — 사용자 *"전부 다 짧은 보조선으로 고정"* → 5/10mm 고정, 분기 모두 폐기

## 영향 범위
- **코드 변경**: 20 commits, 14 files
  - 주요: `Form1.Dimensions.cs` (헬퍼·v1~v10 동적 분기), `Form1.DrawingSheets.cs` (RenderSheetViewForDrawing·SetGridCellMargins), `Form1.MfgDrawing.cs` (T-005 5곳)
  - 신규: `Form1.ExcelTemplate.cs` (PoC), `Form1.cs` (`_lastModelShift*` 멤버)
  - csproj: `System.Web.Extensions`, `Microsoft.VisualBasic` 참조 추가
- **문서 변경**: CHANGELOG +934줄, TASKS.md +71줄, REQUESTS.md +4줄, 엑셀 템플릿 PoC.md +128줄 신설
- **신규 파일**: 사용자템플릿_엑셀.xlsx, 사용자템플릿_엑셀_Rev_01.xlsx, scripts/analyze-xlsx.py
- **외부 의존성**: VIZCore3D SDK 동작 가정 (`SetGridCellMargins`, `MoveObject`, `Drawing2D.Object2D.GetObjectScale` 등)

## 이어갈 지점 ⭐ (다음 세션 복원용)

**현재 상태**: v10(`09515ff`) push 완료. 모든 보조선 5/10mm 고정 + 모델 스케일 매트릭스 안정 + ISO 풍선 좌/우 + 라벨 영역 명시 차단. 사용자 사내 PC 실기 검증 대기.

**다음 작업 후보** (우선순위):

1. **사용자 v10 검증 결과 받기** (가장 빠른 마무리)
   - 모든 보조선 동일 5/10mm 시각 확인
   - 모델 이동 + ISO 풍선 + BOM/tableInfo 위치 안정성
   - 결과 따라 미세 조정 또는 가공도 적용

2. **T-038+T-039 가공도(MfgDrawing) 적용** (큰 작업)
   - `Form1.MfgDrawing.cs`에 동일 패턴 적용
   - 4지점 (메인 L365, 별도 모드 L1108, MULTI L1229, EA newDims L1746)
   - 가공도 별도 캔버스 + 모델 단일

3. **엑셀 PoC 결정** (외부 입력 의존)
   - 옵션 1: Softhills 정식 질의 (T-051 합치기)
   - 옵션 2: 옵션 A 본진 (JSON 파싱 + 우리 렌더, 100~150줄)
   - 옵션 3: 스코프 축소 (BOM/tableInfo 데이터만 외부화)

4. **T-040 (13mm 토글 미작동) 진단**
   - 사용자에게 "어느 버튼·경로에서 안 됨" 질문 후 가설 좁히기
   - Agent 진단 보고서 활용 (가설 A 75%: SetStyle 글로벌 setter 한계)

5. **Test 버튼 시각 비교 (T-005)** — 사용자가 v10에서 외곽 방향 효과 검증 위해 *비교 가능 도구* 원하면

**주의할 점**:
- **R12 적용 중**: 코드 변경 → 자동 commit + push (사용자 검증 흐름 보장)
- **A2Z.exe 잠금 빈발**: 사내 PC에서 검증 중일 때 빌드 실패. 닫고 재시도 패턴 익숙
- **vSign 매핑**: v6에서 Y뷰만 dx 반전 적용. X/Z뷰는 정상 가정 — 사용자 확정은 받지 않음
- **풍선 충돌 회피 (수평 전용)**: `aa417b2`에서 4단계 패턴. Y 슬롯이 좁으면 풍선 겹침 가능 — 검증 필요
- **`Microsoft.VisualBasic.Interaction.InputBox`** — 엑셀 PoC 잔재. csproj 참조 유지

**관련 TASK/FEEDBACK/REQUEST**:
- T-005 (FB-002): IN_PROGRESS (외곽 방향 자동, 실기 검증 대기)
- T-006/FB-003·FB-004: 진행 중 (그리드·라벨 영역·풍선)
- T-012 (REQ-002): IN_PROGRESS (PoC 일시 보류, Softhills/옵션A 결정 대기)
- T-038/T-039: IN_PROGRESS (모델 스케일·보조선 고정, v10까지 적용)
- T-040: IN_PROGRESS (13mm 토글 미작동, 경로 확인 대기)
- T-044 시초: 진행 중 (홀/슬롯/EarthBoss 풍선 가공도만 — X/Y/Z 셀 차단 적용)
- T-057: TODO (검토자 Excel 일치 검증, Excel 수신 대기)

## 참고 링크
- 관련 문서:
  - [엑셀 템플릿 PoC.md](../../기능/도면시트/엑셀%20템플릿%20PoC.md) — PoC Step 1~3.5 흐름 + SDK reflection 분석
  - [메인 치수 추출.md](../../기능/BOM/메인%20치수%20추출.md) — 4경로 통합 (T-028)
- 관련 커밋 (최근 20):
  - v10: `09515ff` — 모든 보조선 5/10mm 고정
  - 라벨 차단: `7081f84` — SetGridCellMargins 12mm
  - 모델 이동: `6f38e1b` — `_lastModelShift*` 도입
  - T-005: `4ec47c8` — ComputePositiveOffsetByOsnapExtreme 헬퍼
- 정책:
  - [CLAUDE.md](../../../CLAUDE.md) R12 (검증 사이클 push 마감)
  - 사용자 메모리: `feedback_no_blind_pattern_transplant.md` (4경로 컨텍스트 주의)
- 직전 세션: [2026-05-04-tracking-categorization.md](./2026-05-04-tracking-categorization.md)
