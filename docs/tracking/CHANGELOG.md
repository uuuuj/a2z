# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**으로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

---

## 2026-05-10 — T-037 1차: BOM 테이블 열 너비 재분배 (헤더·데이터 폭 기준)

**유형**: fix
**커밋**: `pending`
**관련 TASK**: T-037 (IN_PROGRESS, 1차)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 2D 출력 BOM 테이블에서 ITEM 열 "Support Seat" (12자) → "Suppor tS\neat" 줄바꿈, SIZE 열 "75x75x9" (7자) → "75x75x\n9" 줄바꿈으로 행 높이 폭주. 사용자 요청 *"열 이름 너비에 맞춰 테이블 너비를 정하고 줄바꿈 값 폰트 축소 가능?"*.

**SDK 검증 결과** (sdk-verifier 2026-05-10):
- `TemplateTableData.FontSize` / `AutoFit` / `CellFontHeight` **모두 부재** — 셀 단위 폰트 직접 X
- 우회: `Set2DViewObjectItemTextHeight(objID, fHeight)` 객체 단위 일괄 축소만 가능
- 텍스트 배경 마스크(2D 도면 측정용) **부재** (T-040 T1 SDK 직접 X 확정)
- 그리드 셀 좌표(`GetGridCellSize`) **부재** — 캔버스 크기 / 분할로 직접 계산 필요
- → 이번 라운드 폰트 자동 축소 옵션 채택 보류, **열 너비 재분배만** 적용

**코드 변경** ([A2Z/Form1.DrawingSheets.cs:1301](A2Z/Form1.DrawingSheets.cs:1301)):
| 열 | 이전 | 변경 후 | 사유 |
|---|---|---|---|
| No | 6 | 5 | 헤더 "No" 2자 |
| ITEM | 17 | **20** | 데이터 "Support Seat" 12자 |
| MATERIAL | 11 | **12** | 헤더 "MATERIAL" 8자 |
| SIZE | 11 | **14** | 데이터 "75x75x9" 7자 |
| Q'TY | 8 | 7 | 헤더 "Q'TY" 4자 |
| T/W | 12 | 8 | 헤더 "T/W" 3자 |
| MA | 6 | 5 | 헤더 "MA" 2자 |
| FA | 6 | 6 | 유지 (합 77mm 균형) |

합계 77mm 유지 (셀 폭 변경 없음).

**미진행 (T-037 잔여)**:
- BOM 객체 일괄 폰트 축소(`Set2DViewObjectItemTextHeight`) — 객체 ID 추적·회귀 위험 검토 필요
- ITEM SPREF split 기준 확장 (`:` → `-` → `/`) — 사용자 답변 대기
- docs/features/drawing-sheets/generate-sheet-2d.md 갱신

**영향 범위**: 2D 출력 시 BOM 테이블만. 흐름 변경 없는 상수 조정 → R1 docs 갱신 생략.

**다음 검증 예상**: 사용자 실기에서 (1) ITEM "Support Seat" 한 줄 표시 (2) SIZE "75x75x9" 한 줄 표시 (3) 다른 부재명·재질에서 회귀 없는지

---

## 2026-05-06 — T-058 치수 Text 보조선 바깥 배치 (회사 doc 개발 요청 — 상 5)

**유형**: feat (회사 doc 사양 반영)
**커밋**: `pending`
**관련 TASK**: T-058 (DONE)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 회사 doc "개발 요청 — 상 5" 사양 *"치수 Text가 치수 보조선을 넘어 설 경우 → 오른쪽 배치로 협의 했으나 아직 반영 안됨"*. 좁은 치수에서 텍스트가 보조선 사이를 침범하는 문제 회피. T-039 선행 권장이었으나 sdk-verifier 결과 글로벌 옵션 1줄로 가능해 선행 무관하게 진행.

**SDK 검증 결과** (sdk-verifier):
- `MeasureStyle.AlignDistanceTextPosition` enum 등재 확인 (`VIZCore3D.NET.xml:9298`) — 0:아래 / 1:위 / **2:바깥쪽**
- 치수별 개별 위치 옵션은 SDK 미지원 — 글로벌 옵션만 가능
- 텍스트 폭 측정 API 부재 — 좁은 치수 선별 적용은 .NET `Graphics.MeasureString` + 수동 좌표 계산 필요 (옵션 B, 복잡)
- → **옵션 A 채택**: 모든 치수 일괄 바깥쪽

**코드 변경 (5곳, `= 0` → `= 2`)**:
- `Form1.Dimensions.cs:51` — `btnDimensionShowSelected_Click` 선택 치수 표시
- `Form1.Dimensions.cs:448` — `ShowAllDimensions` (T-028 4경로 본진: 글로벌 X/Y/Z + 시트 선택 + 2D 출력)
- `Form1.MfgDrawing.cs:325` — 가공도 메인
- `Form1.MfgDrawing.cs:1050` — 가공도 sub
- `Form1.MfgDrawing.cs:1703` — 가공도 EA

**docs**:
- 신설: [docs/technical-notes/dimension-text-position.md](../technical-notes/dimension-text-position.md) (T-058 통합 사양)
- 변경 이력: [show-selected.md](../features/dimensions/show-selected.md), [main-dimension.md](../features/bom/main-dimension.md), [mfg-drawing.md](../features/mfg-drawing/mfg-drawing.md)
- TASKS.md 머릿주석 회사 doc 표 — 상 5 행을 DONE으로 표시
- TASKS.md DONE 섹션에 T-058 항목 추가

**회사 사양과의 차이**: 원문 *"초과할 때만"* vs 구현 *"항상 바깥쪽"*. SDK 치수별 옵션 부재로 글로벌 적용. 핵심 의도(침범 회피)는 충족, 넓은 치수에서도 바깥 배치라 시각적으로 다소 차이 있을 수 있음. 필요 시 옵션 B(선별)로 후속 가능.

**영향 범위**: 5곳 코드 1줄씩 + technical-note 신설 + 3개 features doc 변경 이력 추가

---

## 2026-05-05 (정정) — 검토 대기 11건 원상 복구 + 본인 개선 카테고리 정의 명확화

**유형**: chore (tracking, 직전 커밋 `f6f8f35` 분류 정정)
**커밋**: `pending`
**관련 TASK**: T-016, T-023, T-029
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 직전 `f6f8f35`에서 검토 대기 중 회사 doc 매핑 없는 3건(T-016/T-023/T-029)을 본인 개선으로 옮겼으나, 사용자 의도와 정반대였음. 사용자 의도는 "**회사 doc 13건 + Softhills 4건 + 검토 대기 11건 = 외부에서 해달라는 우선 처리 명단**, 그 명단에 없으면서 진행 중인 것만 본인 개선으로 분리". 검토 대기 11건은 매핑 ID가 회사 doc이든 사용자 본인이든 무관하게 *사용자가 외부 답변·검증 받아야 하는 명단*.

**원상 복구 (3건)**: 본인 개선 → 검토 대기
- T-016 (치수 추출 3회 이상 간헐)
- T-023 v3 (단일 부재 + 연결성 1덩어리)
- T-029 (치수추출 후 3D 뷰 깨끗, T-049와 묶음 복원)

**머릿주석 표 갱신**:
- 검토 대기 표 머릿글 — "회사 doc 매핑 있는 항목만 유지" 문구 제거, 11건 원본으로 복구. 사용자 표현 요지를 사용자 메시지 그대로 반영 (서브 항목 포함)
- 본인 개선 사항 표 머릿글 — 정의 명확화: "회사 doc 13건 / Softhills 4건 / 검토 대기 11건 어디에도 포함되지 않으면서 진행 중인 작업"
- 본인 개선 11건 (변동 없음): T-004 / T-005 / T-006 / T-012 / T-028 / T-032 / T-036 / T-037 / T-038 / T-041 / T-060

**카테고리 합계 (확정)**:
- 회사 doc 개발 요청 상: 11건 / 중: 2건 (총 13건)
- Softhills API 확인: 4건 (외부 추적)
- 검토 대기: 11건 (사용자 외부 답변·검증 대기)
- 본인 개선 사항: 11건 (위 명단 외)

**영향 범위**: 코드 변경 없음, 추적 문서만

---

## 2026-05-05 — 검토 대기 카테고리 재분류 (회사 doc 매핑 없는 3건 본인 개선으로 이동) [정정됨]

이 변경은 같은 날 정정 커밋으로 원상 복구됨. 기록만 보존.

**유형**: chore (tracking)
**커밋**: `f6f8f35`
**관련 TASK**: T-016, T-023, T-029

(상세는 위 정정 항목 참조)

---

## 2026-05-05 — Z-MAX 정렬 출처 결정 (BBox 유지)

**유형**: chore (tracking)
**커밋**: `pending`
**관련 TASK**: T-056 (DONE)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**결정**: 사용자 BBox.MaxZ 현행 유지. Osnap 기준 변경하지 않음.

**이유**: A2Z 일반 데이터셋(직립 H빔·플레이트·앵글)에서 `BBox.MaxZ == max(Osnap.Z)`가 성립해 정렬 결과 동등. 차이 발생 케이스(경사·곡면 Body)도 정렬 1~2칸 변동 수준으로 실용 영향 작음. 회사 회신은 [sheet1-naming-criteria.md](../technical-notes/sheet1-naming-criteria.md) § 7 단답을 그대로 사용 — "BBox 기준이지만 일반 형상에선 명세와 동일 결과"임을 설명. 차후 회사가 Osnap 자체를 강하게 요구하면 그때 신규 작업으로 변경(`Form1.BOM.cs:688` osnapList 1줄 교체) 진행.

**Tracking 갱신**:
- TASKS.md 검토 대기 항목 2 + T-056 본문 — 결정 반영
- sheet1-naming-criteria.md § 6 최종 결정 + § 9 변경 이력 추가

**영향 범위**: 코드 변경 없음, 추적·기술 문서만

---

## 2026-05-04 (저녁 3차) — 사용자 결정 4건 반영 + T-060 신규 + 카테고리 재명명

**유형**: chore (tracking)
**커밋**: `pending`
**관련 TASK**: T-060 신규, T-042 / T-016 / T-054 메모 갱신
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**사용자 결정 반영**:

1. **항목 2 (Z-MAX 정렬 출처)**: 사용자가 현재 구현(BBox.MaxZ) 확인 — 결정 보류 상태. T-056 검증 보고서 그대로 회신 vs Osnap 변경(`:688`의 osnapList 활용 1줄 교체) 결정 대기
2. **항목 6 (보조선 모델 겹침)**: 사용자 본인 발견 개선사항으로 분류 → **T-060 신규 등록**:
   - 보조선 시작점이 다른 모델 표면과 겹쳐 시각적 혼동 발생
   - 우려 시나리오: 치수선 모델 안쪽 배치, gap 방향이 다른 부재 가로지름, 복잡 형상 단위벡터 부정확
   - 해결 후보: 양방향 분기, 거리 기반 gap 비율, BBox 침범 점검
   - 재현 케이스 대기
3. **항목 3 (T-016 3회 누적)**: 검토 대기 카테고리 그대로 유지 (사용자 재현 정상이지만 간헐 버그라 BLOCKED 유지하며 다음 발생 대기)
4. **항목 7 (Sheet 1 표기)**: 사용자 새 아이디어 — **LCA(Least Common Ancestor) 노드 이름** 채택 가능성. 모든 기준부재를 포함하는 모델트리 최저 공통 조상. 사양 재정리 동안 T-042 현행 유지, 검토 대기 #7에 메모

**카테고리 재명명**:
- "회사 doc 외 잔여 작업" → **"본인 개선 사항"**
- 사용자 일관 분류: 회사 doc(13건) / 검토자 검토 대기(사용자 11건) / **본인 발견(11건, T-060 추가)**

**Tracking 갱신**:
- TASKS.md 머릿주석 — 검토 대기 #7 메모(LCA 재정리 중), 본인 개선 사항 카테고리 이름 + T-060 추가 (10→11건)
- TASKS.md 본문 — T-060 등록 (TODO 섹션, T-059 다음)
- STATUS.md 마지막 작업 갱신

**영향 범위**: 코드 변경 없음, 추적 문서만

---

## 2026-05-04 (저녁 2차) — 사용자 정리 "수정완료 확인 대기" 11건 매핑·검토 대기 카테고리 신설

**유형**: chore (tracking)
**커밋**: `pending`
**관련 TASK**: T-054 / T-016 (잔여 → 검토 대기 이동), 사용자 11건 매핑
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 사용자가 11건 직접 정리해 전달 — 회사 doc과 별개의 "수정완료 확인 대기" 항목들. 검토자·회사 답변 받아야 마무리.

**팀에이전트 2개 활용**:
- Agent A (general-purpose): 사용자 11건과 우리 코드/작업 매핑 정확도 검증, 차이·틀린 사항 발견
- Agent B (Explore): 잔여 12건 중 사용자 11건과 겹치는 항목 식별 (검토 대기 분류 안)

**매핑 결과 (11건)**:
| 우리 상태 | 항목 |
|---|---|
| DONE 일치 | #4(연결성), #5(가공도 보조선), #8(Sheet1 포함부재), #9(시트 재채번), #10(3D 뷰 깨끗), #11(축 표시) |
| DONE 부분 일치/주의 | #2(Z-MAX, BBox vs Osnap), #6(보조선 모델 진입 가능성), #7(Sheet 1 표기 모순) |
| TODO/BLOCKED | #1(T-054 풍선·심볼), #3(T-016 3회 누적) |

**검토 대기 카테고리 신설**:
- TASKS.md 머릿주석에 "검토 대기 (사용자 정리 11건)" 표 추가 — 11건 매핑·차이 정리
- T-054 / T-016을 잔여 카테고리에서 검토 대기로 이동 (직접 겹침)
- Agent B가 부정확 매핑 제안한 T-005, T-037은 작업 주제가 달라 제외

**사용자에게 결정 요청 4건**:
1. **항목 2 Z-MAX**: T-056 보고서 그대로 회신 vs Osnap 기반 재구현 (1줄 변경)
2. **항목 6 보조선 gap**: 치수선이 모델 안쪽에 배치되는 케이스 재현 받아 분기 추가할지
3. **항목 3 T-016**: 사용자 재현 정상이라 CLOSE할지 BLOCKED 유지하며 다음 발생 대기할지
4. **항목 7 Sheet 1 표기**: 직전 "전체 유지" 결정 vs 새 doc "전체(BOM이름)" 표기. 결정 번복 의도 확인

**Tracking 갱신**:
- TASKS.md 머릿주석 — 검토 대기 표 신설, 잔여 12 → 10건
- STATUS.md 마지막 작업 / WIP 갱신

**영향 범위**:
- 코드 변경 없음, 추적 문서만
- 빌드 영향 없음

---

## 2026-05-04 (저녁 1차) — 회사 doc 새 우선순위 매핑·재정리 + 신규 T-057/T-058/T-059 등록

**유형**: chore (tracking)
**커밋**: `pending`
**관련 TASK**: 신규 T-057 / T-058 / T-059, 매핑 갱신 13건
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 회사가 새 개발 우선순위 doc 전달 — 개발 요청 상 11건 + 중 2건 = 13건 (Softhills API 4건은 외부 추적 별도). 사용자 지시: 전체 작업 상황을 회사 새 doc 기준으로 재정리.

**팀에이전트 2개 활용**:
- Agent A (general-purpose): 회사 13건을 기존 T-XXX와 매핑 + 신규 ID 제안
- Agent B (Explore): 회사 doc 13건에 매핑 안 되는 잔여 TODO 추출

**매핑 결과**:

| 구분 | 매핑 | 비고 |
|---|---|---|
| 회사 상 1·2 → T-043 | 기존 | 산출물 형식 결정 대기 |
| 회사 상 3 → **T-057 (신규)** | 신규 | 검토자 Excel 파일 대기 |
| 회사 상 4 → T-044 | 기존 | 실기 확인 선행 |
| 회사 상 5 → **T-058 (신규)** | 신규 | T-039 선행, sdk-verifier 후 단순 구현 |
| 회사 상 6 → T-040 | 기존 | T-039 선행 |
| 회사 상 7 → T-013 | 기존 (BLOCKED) | 옵션 A·B·B2 모두 실패, 새 접근 필요 |
| 회사 상 8·9 → T-039 | 기존 | T-038 선행 |
| 회사 상 10 → T-045 | 기존 | 결합 형식 결정 대기 |
| 회사 상 11 → **T-059 (신규)** | 신규 | 재현 케이스(부재 Index + 스크린샷) 대기 |
| 회사 중 1 → T-047 | 기존 | T-044와 짝, 요구사항 명확화 |
| 회사 중 2 → T-048 | 기존 | 재현 부재 대기 |

**Softhills API 확인요청 4건** (외부 추적, 우리 작업 아님):
- Osnap 기준 → T-051 (이미 등록, 추가 답변 대기)
- 점선이 PDF에서 굵은 실선 → SDK-003 (외부)
- 2D ISO 모서리·홀 누락 → SDK-004 (외부)
- 모델 트리 Body/Node/Part 구분 → SDK-005 (외부)

**회사 doc 외 잔여 12건** (FB·REQ·사용자 직접):
- TODO: T-004 / T-005 / T-012 / T-037 / T-038 / T-041 / T-054
- IN_PROGRESS: T-006 (2차) / T-028 / T-032 / T-036
- BLOCKED: T-016

**Tracking 갱신**:
- TASKS.md 머릿주석 — 회사 13건 매핑 표 + Softhills 4건 + 잔여 12건 표 + 즉시 진행 가능 3건으로 재구성 (이전 매핑은 본 CHANGELOG에 보관)
- TASKS.md 본문에 신규 T-057 / T-058 / T-059 추가 (TODO 섹션 끝)
- STATUS.md 마지막 작업 / WIP / 다음 할 것 갱신

**즉시 진행 가능 (외부 입력 불필요)**:
- T-058 (sdk-verifier 후 단순 구현, T-039 선행 권장)
- T-006 2차 (SDK 셀 clip API 조사부터)
- T-038 (sdk-verifier로 GridStructure API 조사)

**영향 범위**:
- 코드 변경 없음, 추적 문서만
- 빌드 영향 없음

---

## 2026-05-04 (오후) — 치수 추출 매뉴얼 보강 (T-029 + T-023 v3 정책 반영)

**유형**: docs
**커밋**: `pending`
**관련 TASK**: — (T-029, T-023 v3 후속 사용자 매뉴얼 보강)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 사용자 지시 — 치수 추출 후 3D 뷰가 깨끗하게 유지되는 동작(T-029)과, 활성화된 부재가 서로 연결 안 된 경우 에러 처리(T-023 v3)에 대한 사용자 매뉴얼 정리 필요. 코드는 이미 적용됐으나 사용자 매뉴얼이 옛 동작 그대로였음.

**T-029 매뉴얼 반영**:
- "누르면 이런 순서로" 단계 8 "3D 뷰어에 치수 표시" → "**3D 뷰는 깨끗하게 유지**"로 변경. 글로벌 X/Y/Z 버튼이 입구임을 명시
- 새 섹션 *"💡 치수는 어디에 저장되나요?"* 추가 — 6조합 캐시(`chainDimensionList`) 라이프사이클 + 사용자 동작별 표시 양상 표 + "왜 이렇게 동작하는지" 풀이
- 내부 흐름 단계 6도 "3D 뷰에 그리지 않음" 명시

**T-023 v3 매뉴얼 보강**:
- 분기 ② 연결성 판정 섹션에 *"왜 이런 검사가 필요한가요?"* 풀이 추가
- 사용자가 "활성화(add)"라고 표현한 개념 = 3D 뷰에 보이는 부재 = 검사 대상
- 한 무리만 작업하려면 모델트리 체크박스 또는 X-Ray 선택으로 다른 무리 분리 안내

**관련 docs**:
- 갱신: `docs/사용자-매뉴얼/1.기본-작업/치수 추출.md` (last_updated 2026-05-04, 흐름 단계 8·내부 흐름 6, 새 섹션 신설, 분기 ② 풀이 추가, 변경 이력 1줄)

**영향 범위**:
- 코드 변경 없음, 사용자 매뉴얼 1개 파일 보강
- 빌드 영향 없음
- 사용자 가시 효과: 매뉴얼만 봐도 "치수 추출 후 왜 3D 뷰가 깨끗한지" / "왜 떨어진 부재가 있으면 에러가 뜨는지" 이해 가능
- 회사 doc 답변용으로도 활용 가능 (T-029 정책 + T-023 v3 동작 풀이)

---

## 2026-05-04 — 시트 중복 제거 확장 + 기준부재 BOM이름 병기 (T-053 v2 + T-042 부분)

**유형**: feat + docs
**커밋**: `pending`
**관련 TASK**: T-053(v2 확장), T-042(부분 적용, IN_PROGRESS 유지)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**T-053 v2 — 시트 중복 제거 범위 확장**:
- 사용자 피드백: *"Sheet 번호를 다시 재 할당하라는 거였지, 포함부재가 같은 (시트들은) 그대로 놔두란 말은 아니였는데"* → 동일 부재 구성 시트는 모두 정리되어야 함을 명확화
- 자동 제거 알고리즘 확장: "Sheet 1 동일 구성 한정" → **모든 일반 시트 쌍에서 `MemberIndices` 정렬 키 동일 시 첫 등장만 살리기**
- 구현: `HashSet<string> seenMemberKey` + `RemoveAll(s => ... seen 검사)` 한 패스로 처리. 이후 기존 Sheet 1 동일 구성 제거 로직도 그대로 두어 Sheet 1 ↔ 일반 시트 동일 케이스 보강
- Sheet 1(-1) / 설치도(-2) / 가공도(-3)는 의미가 다른 시트라 검사 대상에서 제외하여 보존
- T-053 SheetNumber 재채번은 v1 그대로 유지 — 확장 자동 제거 후에도 빈틈없이 1, 2, 3, ...

**T-042 부분 적용 — 기준부재 BOM이름 병기**:
- 사용자 결정: 표기 포맷 `"1 (BOM이름)"` (공백 + 괄호) 확정
- 일반 시트(`>=0`) + 가공도(-3) 기준부재 셀: `"1"` → `"1 (BOM이름)"` 으로 BOM이름 병기. 매핑 실패 시 `sheet.BaseMemberName` fallback
- Sheet 1·설치도는 의미 다른 시트(전체·설치도 안내)라 그대로 유지 — 회사 원문 "Sheet1 : 전체Item(Item Node 이름)" 부분은 사용자 추가 결정 대기
- T-042는 IN_PROGRESS 유지 (Sheet 1 표기 결정 후 완료)

**Tracking**:
- TASKS.md T-042 IN_PROGRESS 표시 + 부분 적용 메모, T-053 DONE 항목에 v2 확장 정보 추가
- STATUS.md 갱신

**관련 docs (갱신)**:
- generate-sheets.md 단계 9·10 재기술, 분기 C 갱신, mermaid 그대로(흐름 자체는 동일), 변경 이력 2건

**영향 범위**:
- 코드 변경: `Form1.DrawingSheets.cs` 두 블록 (자동 제거 + ListView 갱신)
- 빌드: 0 errors, A2Z.exe 산출 ✅
- 사용자 실기 검증: 일반 시트끼리 부재 구성 같은 케이스에서 한 시트만 남는지 / 기준부재 셀 `"item번호 (BOM이름)"` 정상 표기

---

## 2026-05-02 (오후) — 회사 doc 동기화 추가 3건 (T-049 + T-050 + T-052)

**유형**: feat + docs
**커밋**: `79876e2`
**관련 TASK**: T-049(완료), T-050(완료), T-052(완료)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**T-049 — 치수 캐시 라이프사이클 문서화 (회사 doc 긴급중 3)**:
- main-dimension.md Section 7.5 신설 — 회사 doc "치수추출 버튼 앞뒤 로직" 의문 답변
- `chainDimensionList`를 단일 진실 공급원으로, 4경로(치수추출 / 글로벌 X/Y/Z / 2D 출력 / 일반 시트 / 가공도) + 캐시 사용 양상을 mermaid + 표로 명시
- 사용자 시각 단계별 흐름 + T-032 성능 최적화 연계 포함
- 코드 변경 없음, docs만

**T-050 — 3D View 축 표시기 (회사 doc 긴급하 1)**:
- sdk-verifier 결과 `vizcore3d.View.MarineAxis` 공식 지원 확인 (`MarineAxisManager`, XML L43019)
- Form1.BOM.cs `Vizcore3d_OnInitializedVIZCore3D` 단계 3.5에 `vizcore3d.View.MarineAxis.Visible = true` 한 줄 추가
- 결과: 3D 뷰 좌측하단에 ISO X/Y/Z triad 표시
- 추가 미세 조정(Length / Position / SetText) 필요 시 같은 위치에 보강 가능

**T-052 — Sheet1 포함부재 표기 (회사 doc 긴급하 3)**:
- Form1.DrawingSheets.cs ListView 단계의 `BaseMemberIndex == -1` 분기 제거 → 일반 시트와 동일 로직으로 통합
- 결과: Sheet 1 포함부재 셀이 "전체" → "1, 2, 3, ..., N"
- BOM 14건 초과 시 ListView 컬럼 폭 처리는 사용자 실기 후 후속 조정

**Tracking**:
- TASKS.md TODO → DONE 3건 이동
- STATUS.md 마지막 작업 / WIP / 다음 할 것 갱신

**관련 docs (갱신)**:
- main-dimension.md (Section 7.5 "치수 캐시 라이프사이클" 신설)
- vizcore3d-initialized.md (단계 3.5 + 상태 변화 + 변경 이력, T-050)
- generate-sheets.md (변경 이력, T-052)

**영향 범위**:
- 코드 변경: 초기화 1줄(T-050) + ListView 분기 통합(T-052). 컴파일 영향 무
- 빌드 검증: A2Z.exe 실행 중이라 bin/Debug dll 복사만 잠금으로 실패. 컴파일 자체는 통과 — exe 닫고 재빌드 필요
- 사용자 실기 검증: 3D 뷰 축 표시기 가시성 / Sheet 1 포함부재 셀 표기

---

## 2026-05-02 — 회사 doc 동기화 잔여 4건 (T-046 확장 + T-053 + T-055/T-056)

**유형**: feat + fix + docs
**커밋**: `8081688`
**관련 TASK**: T-046(확장 완료), T-053(완료), T-055(완료), T-056(완료)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**T-046 확장 — 모든 보조선 가는 실선 + 모델 표면 10mm gap**:
- 회사 doc "긴급상 10" 원문은 가공도 보조선만 명시했으나 사용자 확장 지시로 4경로(가공도 메인/EA + 일반 시트 2D 출력 + 글로벌 X/Y/Z + 치수추출) 일괄 적용
- (1) `Form1.MfgDrawing.cs:1542, 1900` LineType `DASHED_DOUBLEDOTTED` → `SOLID` 통일 + 토글 패턴(`SOLID` 복원 호출) 제거로 단순화
- (2) `Form1.Dimensions.cs`에 헬퍼 `OffsetTowardLineEnd(from, to, distance)` + 상수 `ExtensionLineGap = 10.0f` 신설. `DrawDimension` 보조선 시작점이 모델 표면에서 10mm 외향 이동 후 시작
- 우아한 발견: 4경로 보조선이 모두 `DrawDimension` 단일 함수를 거치므로 한 곳 변경으로 자동 적용
- 사용자 실기 후 1mm → 10mm 상향 (1mm는 시각적으로 식별 어려움)
- SDK 조사 결과 보조선 offset 직접 옵션 미지원 → ShapeDrawing 좌표 보정 우회로

**T-053 — 중복 Sheet 삭제 후 SheetNumber 재채번**:
- `GenerateDrawingSheets` 단계 9(Sheet 1 동일 구성 제거) 직후에 `drawingSheetList` 전체 순회하며 `SheetNumber = i + 1` 일괄 재할당 (Form1.DrawingSheets.cs:215~221)
- 순서(Sheet 1 → 일반 → 설치도 → 가공도) 보존, 번호만 1부터 빈틈없이 정합
- 가공도는 sheetLabel이 `MfgDrawingNo` 기반이라 표시 영향 없음 (데이터 일관성 목적)
- `generate-sheets.md` 단계 9.3 + mermaid + 변경 이력 갱신

**T-055 — Osnap 기준점 검증 보고서 (회사 "완료 3" 의문 답변)**:
- 4경로 보조선 데이터 흐름 + 부재별/전체 풀 동시 적재 + X/Y/Z 뷰별 primary/secondary 매핑 + 4단 dedup(부재 → 전역 dimAxis → MergeCoordinates 0.5mm → keyToDim) 코드 트레이스 완료
- 결론 **부분 일치** — 핵심 의도(코너 우선 + 중복 제거 + 부재/전체 분리)는 모두 구현되었으나, 부재 단위에서 4코너가 아니라 1점만 남기는 점이 명세 문구와 다름
- 산출물: `docs/technical-notes/osnap-criteria.md` (회사 doc 갱신용 단답 포함)

**T-056 — Sheet1 Z-MAX 정렬 검증 보고서 (회사 "완료 5" + "수정 후 확인 필요 2" 의문 답변)**:
- 현재 코드는 `BBox.MaxZ`(Form1.BOM.cs:735) 기준 정렬, 회사 명세는 `max(Osnap.Z)` 기준 — 데이터 출처 차이
- 직립 H빔·평판 등 일반 철골 형상에선 두 값이 동등하여 정렬 결과 같음. 경사 부재·곡면 Body에서 수 mm 차이로 정렬 1~2칸 흔들림 가능
- 결론 **부분 일치** — 회사 답변에 따라 후속 작업(Form1.BOM.cs:688 osnapList 활용 한 줄 변경) 신설 가능
- 산출물: `docs/technical-notes/sheet1-naming-criteria.md`

**Tracking**:
- `TASKS.md` TODO → DONE 4건 이동
- `STATUS.md` 마지막 작업 / WIP / 다음 할 것 갱신
- `CHANGELOG.md` 본 항목 추가

**관련 docs (신규/갱신)**:
- 신규: `docs/technical-notes/dimension-extension-line.md`, `osnap-criteria.md`, `sheet1-naming-criteria.md`
- 갱신: `docs/features/drawing-sheets/generate-sheets.md`, `docs/features/mfg-drawing/mfg-drawing.md`

**영향 범위**:
- 코드 변경: 보조선 시각·`SheetNumber` 데이터만. 컴파일·런타임 핵심 로직 영향 없음
- 사용자 실기 검증 권장: 보조선 SOLID + 10mm gap 4경로 일관성, SheetNumber ListView 정합

---

## 2026-04-24 — T-036 4차 (1~5단계) + 7건 일괄 DONE 정리 + T-037~041 신규 등록

**유형**: fix + chore
**커밋**: `cb0a779`
**관련 TASK**: T-036(4차 1~5단계), T-018/T-029/T-030/T-031/T-033/T-034/T-035 DONE, T-037~T-041 신규
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**T-036 4차 — 가공도 시트 ScreenAxisRotation 보존 (5단계 진행)**:
- **1단계** (Form1.MfgDrawing.cs): R180 회전 직후 `FitToView()` 제거 (Z90의 교훈 동일 적용). 스냅샷 저장 조건을 `longestAxis=="Z"`에서 `Z || use1803d || isMinusCamera3d`로 확장. → 사용자 로그에서 첫 1~2번 클릭 Z 케이스 세로 잔존 확인
- **2단계** (Form1.MfgDrawing.cs): 스냅샷 캡처를 try/finally 밖, EndUpdate 직후로 이동. `shouldSnapshotMfgCamera` 플래그 + `Application.DoEvents()` 추가 (BeginUpdate 안에선 ScreenAxisRotation commit 전 상태 캡처 우려)
- **3단계 (근본 원인 발견)** (Form1.cs + Form1.DrawingSheets.cs): sdk-verifier로 `CameraData` 명세 재확인 → **ScreenAxisRotation은 CameraData에 미포함**(XML L2552-2606). `_mfgDrawingZ90Applied`/`_mfgDrawingR180Applied` bool 필드 신설, ExecuteMfgDrawing이 추적, 복원 블록에서 SetCameraData 후 `RotateCameraByScreenAxis` 재호출
- **4단계 (시각 정돈)** (Form1.DrawingSheets.cs): 사용자 "카메라 이동 후 회전 2단계 시각 잔존" 보고 → 복원 블록 전체를 BeginUpdate/EndUpdate로 감쌈. DoEvents 제거
- **5단계 (SetCameraData 제거)** (Form1.DrawingSheets.cs): 4단계로도 첫 클릭 2단계 시각 잔존 → 가설은 SetCameraData가 ScreenAxisRotation 동기 리셋 + paint 트리거로 BeginUpdate 우회. 외부 카메라 변경 경로(FlyToObject3d 가공도 분기 스킵 + R180 FitToView 제거)가 모두 차단됐으므로 SetCameraData 자체 불필요. 회전 재적용만 유지

**Tracking 정리**:
- `TASKS.md` IN_PROGRESS → DONE 이동 7건: T-018(오버레이 라벨), T-029(치수추출 후 3D 깨끗), T-030(시트 선택 후 3D 깨끗), T-031(가공도 SMOOTH), T-033(오버레이 해제 타이밍), T-034(글로벌뷰 SMOOTH), T-035(글로벌뷰 선택 해제) — 사용자 실기 확인 완료분
- `TASKS.md` 신규 5건 등록: T-037(BOM 줄바꿈+ITEM split), T-038(셀 크기 기반 모델 스케일), T-039(치수 offset 재설계), T-040(치수 텍스트 겹침 감지·회피), T-041(Leader line PoC)
- `TASKS.md` T-036에 4차 5단계 진행·가설 모두 기록

**관련 docs**:
- `mfg-drawing.md` 4차 1~5단계 변경 이력 추가
- `lv-sheet-selected.md` 4차 3·4·5단계 변경 이력 추가

**영향 범위**: 가공도 시트 선택 시 카메라 회전 보존 메커니즘만. 일반 시트·설치도·치수추출·글로벌뷰 영향 없음.

---

## 2026-04-23 — T-036 3차: CameraData 스냅샷 복원으로 외부 FitToView 리셋 방어

**유형**: fix
**커밋**: `acc359d`
**관련 TASK**: T-036
**배경**: 직전 커밋(`e9547a1`)으로 ExecuteMfgDrawing 내부 FitToView는 제거했지만 사용자 실기 "세로로 안 되거든" 재보고. 즉 **외부 경로**에서 FitToView가 0.5초 뒤 호출되어 ScreenAxisRotation 회전을 리셋. 사용자 힌트 "Z축 고정 푸는 API"
**SDK 조사** (sdk-verifier):
- `LockZAxis` = 키보드 방향키 회전용. 회전 유지와 **무관**
- `EnableAutoFit` = 자동 fit만 차단. 명시적 `FitToView` 호출은 못 막음
- **`GetCameraData()` / `SetCameraData(data, animation)`** = 스냅샷·복원 (XML L63141/63154~63166). **SDK 정공법**
- 회전 유지 전용 `FreezeRotation`·`PinRotation` 등은 **존재하지 않음** 확인

**변경 사항**:
- [Form1.cs](../../A2Z/Form1.cs): `_mfgDrawingCameraSnapshot` 필드 추가 (`VIZCore3D.NET.Data.CameraData`)
- [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs): Z 최장축 90° 회전 직후 `_mfgDrawingCameraSnapshot = vizcore3d.View.GetCameraData()` 저장. non-Z 케이스는 null로 리셋(오염 방지)
- [Form1.DrawingSheets.cs `LvDrawingSheet_SelectedIndexChanged`](../../A2Z/Form1.DrawingSheets.cs): 말미 `CollectBOMInfo(false)` 직후에 가공도(-3) + 스냅샷 존재 시 `vizcore3d.View.SetCameraData(_mfgDrawingCameraSnapshot, false)` 복원. `animation=false`로 즉시 적용. try/catch로 예외 보호, `DiagLog T-036 카메라 스냅샷 복원` 기록
- docs: `mfg-drawing.md` / `lv-sheet-selected.md` 변경 이력 각 1건
- MSBuild Debug 통과

**영향 범위**: Z 최장축 가공도 시트 선택 시 카메라 상태 보존만. 다른 시트(일반·설치도)·다른 축 케이스는 스냅샷 null로 영향 없음

---

## 2026-04-23 — T-036 재수정: Z90 FitToView 제거 (직전 커밋 부분 되돌림)

**유형**: fix
**커밋**: `e9547a1`
**관련 TASK**: T-036
**배경**: 직전 커밋(`e08cb5c`)에서 Z 최장축 90° 회전 직후 `FitToView()` 추가. 사용자 DiagLog 공유:
```
T-036 MfgDrawing bom=11 sizeXYZ=(65,65,1050) longestAxis=Z
  use180=False useMinus=True Z90Applied=True R180Applied=False
```
**사용자 관찰 "누르는 순간 가로로 변하고 치수 보임 → 0.5초 뒤 FitToView로 세로로 변함"** → **직전 커밋의 FitToView가 바로 그 리셋의 주범** 확정

**원인**: ExecuteMfgDrawing 원본 코드의 L532 근처 주석이 이미 경고:
> "반드시 모든 drawing 완료 후 마지막에 적용해야 유지됨. LockZAxis를 false로 유지 (true로 복원하면 렌더링 엔진이 회전을 리셋)"

즉 `ScreenAxisRotation`으로 적용한 회전은 후속 카메라 동작(특히 FitToView)이 리셋시키는 SDK 동작이 있음. 내가 추가한 FitToView가 이 케이스에 정확히 해당

**변경 사항**:
- L538 `vizcore3d.View.FitToView();` **제거**
- 주석 강화: "이 회전 직후 FitToView 호출 절대 금지 — ScreenAxisRotation 회전을 리셋해 Z가 다시 세로로 복구됨"
- `BeginUpdate/EndUpdate` 감싸기는 그대로 유지 (중간 깜빡임 차단 역할은 부작용 없음)

**영향 범위**: Z 최장축 부재의 가공도 렌더만. 다른 longestAxis(X/Y) 케이스는 영향 없음

---

## 2026-04-23 — T-036 추가 보강: BeginUpdate 감싸기 + Z90 FitToView

**유형**: fix
**커밋**: `e08cb5c`
**관련 TASK**: T-036
**배경**: 사용자 재보고 "가로로 누워있다가 카메라 재조정/fit 과정 중 갑자기 세로로 변함 (Z축 세운 모델들에서)". 진단: `ExecuteMfgDrawing` 내부 `MoveCamera`·`FitToView`·`RotateCameraByScreenAxis` 여러 단계가 즉시 반영되어 **중간 상태가 화면 깜빡임으로 노출**. `BeginUpdate/EndUpdate` 없이 구현되어 있었음
**변경 사항**:
- [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs) 전체를 `vizcore3d.BeginUpdate()` / `finally { vizcore3d.EndUpdate(); }` 로 감쌈 → 중간 카메라 회전 단계가 화면에 노출되지 않고 **최종 상태만** 반영
- L532 Z 최장축 90° 회전 **직후** `vizcore3d.View.FitToView()` 호출 추가 — 회전 후 화면 중앙·스케일 재조정 누락되어 있던 부분 보강
- docs `mfg-drawing.md` 변경 이력 갱신
- MSBuild Debug 통과

**영향 범위**: 가공도 시트 선택 시 화면 전환 부드러움 + Z 최장축 90° 회전 후 화면 정합. 회전 로직 자체는 무변경

**추가 확인 필요**: 이 수정으로 "가로→세로 깜빡"이 사라지는데, 만약 **최종 결과가 여전히 세로**라면 `DiagLog T-036 MfgDrawing bom=... longestAxis=... use180=...` 로그 공유 필요. 회전 순서 자체를 재설계해야 할 수 있음

---

## 2026-04-23 — T-036 재해석: 가공도 시트 ISO 뷰 느낌 해결

**유형**: fix
**커밋**: `b0f8802`
**관련 TASK**: T-036
**배경**: 직전 커밋(`537f07c`)은 "Z 최장축 세로 배치"로 해석해 L215 180° 스킵 가드 추가. 사용자 실기 재보고 "45도 대각 ISO 뷰로 보게 된다" → Z 축 방향이 아닌 **카메라 방향 자체가 ISO**라는 다른 증상 확인

**원인 확정**: [LvDrawingSheet_SelectedIndexChanged](../features/drawing-sheets/lv-sheet-selected.md) 공통부의 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 직전 글로벌 ISO 버튼 상태)을 **그대로 유지한 채 객체로 이동**. 그 후 호출되는 `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 SDK 비동기 렌더 사이에 묻혀 덮어쓰지 못하는 현상으로 추정

**변경 사항**:
- [Form1.DrawingSheets.cs `LvDrawingSheet_SelectedIndexChanged` L542~](../../A2Z/Form1.DrawingSheets.cs): 가공도(-3) 시트일 땐 `FlyToObject3d` **스킵**. `ExecuteMfgDrawing`이 자체 카메라·FitToView·visibility를 모두 세팅하므로 충돌 제거
- [Form1.MfgDrawing.cs L254](../../A2Z/Form1.MfgDrawing.cs): 직전 커밋의 `if (use1803d && longestAxis != "Z")` 가드 **원복** → 원래 `if (use1803d)`. ISO 원인과 무관한 수정이고 Z 최장축 수직 뒤집기 효과를 잃게만 했기 때문. `use1803d` 변수의 블록 바깥 스코프 승격은 유지 (DiagLog 가시성)
- docs: `lv-sheet-selected.md` 변경 이력(T-036 재조정), `mfg-drawing.md` 변경 이력(재해석·원복)
- MSBuild Debug 통과

**영향 범위**: 가공도 시트 선택 시 카메라 동작만. 일반 시트·설치도는 기존 `FlyToObject3d` 호출 유지

---

## 2026-04-23 — T-034/T-036 후속 패치 (사용자 실기 피드백 반영)

**유형**: fix
**커밋**: `537f07c`
**관련 TASK**: T-034 (후속), T-036 (수정)
**사용자 피드백**:
- T-033 ✓ 통과 / T-034 ✓ 통과 (단 BOM 테이블 선택 → 글로벌 ISO 시 **은선 복귀** 발견) / T-036 "가공도 선택 시 세로축이 더 길게 나옴, 가로여야 하는데"

**변경 사항**:
- **T-034 후속** [Form1.DrawingSheets.cs `ApplyDrawingSheetView`](../../A2Z/Form1.DrawingSheets.cs): 내부 2곳(L702 ISO / L735 X·Y·Z) `SetRenderMode(DASH_LINE)` → `SMOOTH`
  - 사용자가 BOM 테이블에서 행을 선택한 상태로 글로벌 ISO/X/Y/Z 버튼을 누르면 `ApplyGlobalView`의 첫 분기(`tabPageDrawing + lvDrawingSheet 선택됨`) 통과 → `ApplyDrawingSheetView`로 진입 → 여기 DASH_LINE 잔존으로 은선 복귀
  - L1433(2D 캡처 경로)은 건드리지 않음
- **T-036 수정** [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs): L215 `if (use1803d)` → `if (use1803d && longestAxis != "Z")` 가드 추가
  - 원인 확정: Z 최장축 + `use180` 조합에서 180° + 90° = 270° 회전 → Z축이 수평 아닌 세로로 뒤집힘
  - 수정: Z 최장축일 때 180° 스킵 → 뒤에 이어지는 L532 90° 회전만 적용 → Z 수평 배치 보장
  - 트레이드오프: Z 최장축일 때 "수직 뒤집기" 효과 잃음 (부재의 비대칭 방향 조정 부분 상실). 가로 배치 우선이 사용자 의도와 일치하므로 수용. 재현 데이터 더 모이면 축 기반으로 180° 재설계 예정

- docs: `global-iso.md` 변경 이력(T-034 후속) / `mfg-drawing.md` 변경 이력(T-036 수정)
- MSBuild Debug 통과

**영향 범위**: 글로벌 뷰 전환 시 은선 복귀 / 가공도 Z 최장축 부재 세로 배치 두 케이스. T-035(선택 해제)는 그대로 작동

---

## 2026-04-22 — T-033/T-034/T-035/T-036 UX 후속 개선 4건

**유형**: feat + fix
**커밋**: `230e45f`
**관련 TASK**: T-033, T-034, T-035, T-036
**사용자 피드백 반영**:
- T-033 "자동 처리 완료 팝업 후에도 치수 계산 중 창이 2초 더 떠있음"
- T-034 "ISO/X/Y/Z 글로벌 버튼에서도 은선 처리되는 거 같아 잘 보이게"
- T-035 "글로벌 뷰 버튼 누르면 특정 부재가 빨간색으로 되어있을 때가 있어서 선택 안 되게"
- T-036 "가공도 눌러도 가장 긴 부분이 가로로 배치되고 fit하게 안 나오는 경우 / 선택 안 되게"

**변경 사항**:
- **T-033** [Form1.BOM.cs `CompleteMainDimensionPostClash`](../../A2Z/Form1.BOM.cs): 순서 재배치
  - 기존: `Osnap → 치수 → MessageBox → GenerateDrawingSheets → finally HideBusyOverlay`
  - 신: `Osnap → 치수 → GenerateDrawingSheets → HideBusyOverlay → MessageBox`
  - 팝업 뜰 때 오버레이 없음, 팝업 닫힌 후 추가 처리 없음. finally HideBusyOverlay는 예외 안전망 유지 (중복 호출 OK)
- **T-034** [Form1.GlobalViews.cs](../../A2Z/Form1.GlobalViews.cs): L100 `ApplySelectedNodesView` + L150 `ApplyFullModelView` 의 `SetRenderMode(DASH_LINE)` → `SetRenderMode(SMOOTH)` 실선 모드로 교체. `ApplyDrawingSheetView` 쪽은 추가 조사 필요로 미변경
- **T-035** [Form1.GlobalViews.cs](../../A2Z/Form1.GlobalViews.cs): `ApplyFullModelView`·`ApplySelectedNodesView` 시작부에 `Object3D.Select(Object3dSelectionModes.DESELECT_ALL)` 추가. 글로벌 뷰 전환 시 T-022로 생긴 빨간 하이라이트 해제. `ApplyDrawingSheetView`는 시트 선택 맥락이라 T-022 유지
- **T-036** [Form1.MfgDrawing.cs `ExecuteMfgDrawing`](../../A2Z/Form1.MfgDrawing.cs): 진입부에 `DESELECT_ALL` 추가. 말미 `DiagLog T-036 MfgDrawing bom=N sizeXYZ=... longestAxis=X/Y/Z isPadOrPlate=bool viewDir=...` 추가 — 사용자 재현 시 "최장축 가로 배치 안 되는 경우" 분석용. 회전 로직 자체는 재현 데이터 확보 후 수정 예정
- docs 갱신: `main-dimension.md` T-033 변경 이력 / `global-iso.md` T-034·T-035 상태 변화·이력 / `mfg-drawing.md` T-036 변경 이력
- MSBuild Debug 통과

**영향 범위**: UX 후속 튜닝 4건 묶음. 치수 추출 플로우 타이밍, 글로벌 뷰 시각 스타일, 선택상태 일관성, 가공도 진단 로그

---

## 2026-04-22 — T-032 치수 계산 성능 최적화 (Osnap 맵 재사용)

**유형**: perf
**커밋**: `6113a16`
**관련 TASK**: T-032
**배경**: 사용자 피드백 "치수 계산 중 창이 오래 떠있음". 원인은 `CollectAllOsnap`과 `ComputeViewDimensionsForMembers`가 각 부재의 `GetOsnapPoint`를 **이중 호출**하던 것 (데이터 구조 차이로 재사용 안 됨)
**선택한 방식**: 옵션 A — CollectAllOsnap이 수집 중 부재별 맵도 같이 구축, ComputeViewDimensionsForMembers가 재사용
**변경 사항**:
- **Form1.cs**: `_lastCollectedNodeOsnapMap` 필드 추가 (`Dictionary<int, List<(Vertex3D, string)>>`)
- **Form1.BOM.cs `CollectAllOsnap`**: 각 부재의 Osnap을 플랫 리스트(`osnapPointsWithNames`)에 넣는 동시에 부재별 맵도 `_lastCollectedNodeOsnapMap`에 적재. 호출 초반 `Clear()` 추가로 이전 호출 잔존 방지
- **Form1.Dimensions.cs `ComputeViewDimensionsForMembers`**: `preBuiltNodeOsnapMap` optional 파라미터 추가
  - 있으면: `memberIndices` 부분만 필터해 재사용 (**GetOsnapPoint 호출 없음**)
  - 없으면: 기존대로 내부에서 `GetOsnapPoint`로 구축 (시트 선택 자동 경로는 다른 부재 집합이라 null 전달)
- **Form1.BOM.cs `CompleteMainDimensionPostClash`**: `_lastCollectedNodeOsnapMap` 전달 → GetOsnapPoint 중복 호출 제거. `Stopwatch` 측정으로 `DiagLog T-032 치수 계산: ... ComputeViewDimensionsForMembers=Xms` 기록
- docs `main-dimension.md` 단계 12·13 재기술 + 변경 이력
- MSBuild Debug 통과

**영향 범위**: 치수추출 버튼 경로의 Osnap 중복 호출 제거로 계산 시간 감소 (대략 절반 수준 예상, 측정 필요). 시트 선택 자동 경로·기타 `ComputeViewDimensionsForMembers` 호출자는 무영향

---

## 2026-04-22 — T-030 시트 선택 시 3D 뷰 치수 렌더링 제거 (T-029 정책 확장)

**유형**: feat
**커밋**: `a01cddb`
**관련 TASK**: T-030
**배경**: T-029로 치수추출 버튼의 3D 뷰 치수 렌더링을 제거했지만, 시트 선택 자동 치수는 여전히 렌더링됨. 사용자 피드백 "시트 눌렀을 때 치수가 나오는데 왜 나오는지 모르겠음"
**결정**: (a) 채택 — 일반 시트 분기에서도 같은 정책 적용
**변경 사항**:
- `LvDrawingSheet_SelectedIndexChanged` 일반 시트 분기에서 `ShowAllDimensions()` 호출 제거
- 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()`로 3D 뷰를 **치수선 없는 깨끗한 상태**로 마감
- `chainDimensionList` · `lvDimension`은 그대로 채움 → 2D 출력·글로벌 뷰 버튼(`ShowAllDimensions(viewDir)`)에서 자동 활용
- `DiagLog "T-030 시트 선택 자동 치수: sheet#=N members=M chain=K (3D 미렌더)"` 기록
- 설치도(-2) 시트는 `ExtractInstallationDimensions`가 이미 3D 미렌더라 그대로 유지 (BBox 기반 데이터만 채움)
- docs `lv-sheet-selected.md` 분기 A 재기술 + 변경 이력

**영향 범위**: 시트 선택 시 UX. 치수 데이터·시트 생성·2D 출력 모두 그대로. 글로벌 뷰 버튼을 눌러야 치수가 보이는 일관된 2단계 UX (T-029 ↔ T-030)

---

## 2026-04-22 — T-031 가공도 시트 선택 시 은선 처리 제거 (SMOOTH 실선)

**유형**: feat
**커밋**: `2812b80`
**관련 TASK**: T-031
**배경**: 사용자 피드백 "가공도 눌렀을 때 은선 처리 안되게 하고 싶어"
**변경 사항**:
- [Form1.MfgDrawing.cs L142](../../A2Z/Form1.MfgDrawing.cs) `ExecuteMfgDrawing` 내 `SetRenderMode(DASH_LINE)` → `SetRenderMode(SMOOTH)` 교체
- 가공도 시트 선택 시 3D 뷰가 **실선 모드**로 표시됨
- 2D 캡처·PDF 출력 내부 경로(L820, L1582)의 DASH_LINE은 그대로 유지 — 이쪽은 2D 도면의 내부 상세 은선용이라 구분
- docs `mfg-drawing.md` 상태 변화 표(`View.RenderMode` 행) + 변경 이력 갱신

**영향 범위**: 가공도 시트 선택 시 3D 뷰 시각 스타일만. 2D 도면 출력은 영향 없음

---

## 2026-04-22 — T-029 치수추출 버튼의 3D 뷰 치수 렌더링 제거

**유형**: feat
**커밋**: `f2bfb1a`
**관련 TASK**: T-029
**배경**: T-028로 chainDimensionList가 6조합까지 채워지니 치수추출 직후 3D 뷰 치수가 과밀. 사용자 피드백: "글로벌 뷰 버튼 누르면 보여주는 것으로 충분"
**변경 사항**:
- `CompleteMainDimensionPostClash` 치수 블록 끝에서 `ShowAllDimensions()` 호출 **제거**
- 대신 `Review.Measure.Clear()` + `ShapeDrawing.Clear()` 호출 — 이전 렌더 잔존 제거로 "치수선 없는 깨끗한 상태" 마감
- `chainDimensionList`·`lvDimension`은 T-028대로 채움 → 글로벌 X/Y/Z 뷰 버튼이나 2D 출력 시 `ShowAllDimensions(viewDirection)`이 해당 뷰 치수만 필터해 렌더
- docs `main-dimension.md` 단계 14.5(3D 뷰 정리) 추가, 상태 변화에 `Review.Measure` 행 갱신, 변경 이력

**영향 범위**: 치수추출 UX만 변경. 치수 데이터·시트 생성·2D 출력 모두 그대로. 사용자가 뷰 버튼을 눌러야 치수가 나오는 2단계 UX

---

## 2026-04-22 — T-028 치수 로직 4경로 통합 (2D 출력 엔진 기준)

**유형**: refactor + feat
**커밋**: `375d66f`
**관련 TASK**: T-028 (T-027 대체)
**배경**: 4개 경로(치수추출·글로벌 X/Y/Z 버튼·2D 출력·시트 선택 자동)의 치수 로직이 각기 달라 결과 불일치. 사용자 요구: "2D 출력에서 사용하는 Osnap·로직 기준으로 모두 통일"
**변경 사항**:
- **`ChainDimensionData.ViewDirection` 필드 추가** ([Models.cs](../../A2Z/Models.cs)) — 이 치수가 보이는 뷰("X"/"Y"/"Z" 또는 콤마 구분 "X,Y"). 글로벌 뷰 버튼 필터링용
- **`AddChainDimensionByAxis` 반환 `ChainDimensionData`에 `ViewDirection = viewDirection` 기록** — 체인·전체 치수 양쪽
- **공용 헬퍼 `ComputeViewDimensionsForMembers(memberIndices, viewDirection, tolerance)`** 신설 ([Form1.Dimensions.cs](../../A2Z/Form1.Dimensions.cs))
  - 2D 출력 엔진(`nodeOsnapMap` + `FilterOsnapForDimAxis` + `AddChainDimensionByAxis(axis, viewDirection)`) 완전 재사용
  - `viewDirection == null` → 3뷰 × 2축 = 6조합 모두 / `"X"/"Y"/"Z"` → 해당 뷰 2축만
  - 중복 제거: `(Axis, Start, End)` 3자리 반올림 기준, `ViewDirection`은 콤마 누적 (같은 치수가 여러 뷰에 속하면 "X,Y" 식)
- **`ShowAllDimensions` 대폭 단순화** — 내부 분기 ①(Osnap 재추출)·②(nodeOsnapMap 재계산)·③(그대로) 제거. `chainDimensionList`에서 `ViewDirection.Split(',').Contains(viewDirection)` 필터링 + 스마트 필터링만. `isInstallationMode`·`useDirectChain` 변수 제거, 오프셋 분기 단일화
- **`FilterOsnapByViewDimensionUsage`(T-027) 제거** — 2D 출력 로직과 달라 혼동 유발. 대체는 `ComputeViewDimensionsForMembers`
- **`CompleteMainDimensionPostClash` 간소화** ([Form1.BOM.cs](../../A2Z/Form1.BOM.cs)) — visible 부재 계산 후 공용 헬퍼 1회 호출로 대체. `DiagLog T-028 치수 계산: visibleMembers=N chain=M`
- **`LvDrawingSheet_SelectedIndexChanged` 분기 재작성** ([Form1.DrawingSheets.cs](../../A2Z/Form1.DrawingSheets.cs)) — 가공도(-3) `ExecuteMfgDrawing` / **설치도(-2) `ExtractInstallationDimensions`(BBox 유지, 추후 A 전환 여지)** / 그 외 공용 헬퍼
- docs 갱신: `main-dimension.md` 단계 13·변경 이력 / `lv-sheet-selected.md` 분기 A 재작성·변경 이력
- MSBuild Debug 통과

**영향 범위**: 치수 로직 대폭 통합. 4경로가 같은 Osnap 엔진(`nodeOsnapMap` + `FilterOsnapForDimAxis`) 공유. 설치도 시트만 BBox 기반 유지 — 사용자가 나중에 "완전 Osnap 통일(A)"로 전환 가능하도록 분리된 구조

---

## 2026-04-22 — T-027 치수추출 Osnap 선별 (뷰×축 필터 endpoint 합집합)

**유형**: feat
**커밋**: `bb48a16`
**관련 TASK**: T-027
**배경**: 치수 추출 결과 3D 뷰에 체인 치수가 과밀(로그상 chain=32~52). 사용자 의도는 "도면 뷰별 치수 계산에서 살아남는 Osnap만 남기고 나머지는 치수선 생성에 쓰지 말자"
**변경 사항**:
- **선택한 방식**: (a) 체인 치수만 축소 / β — endpoint 합집합 1회 산출 후 축별 1벌 체인 생성
- **`FilterOsnapByViewDimensionUsage(mergedPoints, tolerance)`** 신설 ([Form1.Dimensions.cs](../../A2Z/Form1.Dimensions.cs))
  - X·Y·Z 뷰 × X·Y·Z 치수축 중 뷰≠치수축인 **6개 조합** 각각에서 `AddChainDimensionByAxis` 1차 필터(같은 치수축 값 중 필터축 최소) 로직을 재현해 endpoint 수집
  - 6개 endpoint 집합의 **합집합**(좌표 3자리 반올림 기준 중복 제거)을 반환. 원 순서 보존
- **`CompleteMainDimensionPostClash`** 수정 ([Form1.BOM.cs](../../A2Z/Form1.BOM.cs))
  - `MergeCoordinates` 직후 `FilterOsnapByViewDimensionUsage` 호출로 `filteredPoints` 산출
  - `AddChainDimensionByAxis(filteredPoints, axis, tolerance)` 3회(X/Y/Z) 호출해 `chainDimensionList` 생성 — 기존 뷰 방향 없는 축별 1벌 구조 그대로
  - `DiagLog "T-027 Osnap filter: merged=N → filtered=M"` 기록으로 감소량 정량화
- **보존 대상**: `osnapPointsWithNames`, `lvOsnap`(왼쪽 Osnap 목록) — 제작도·가공도 등 다른 기능이 공유하므로 **원본 유지**
- docs `main-dimension.md` 단계 13.5 추가, 변경 이력 1건
- MSBuild Debug 통과 (경고 0)

**영향 범위**: 치수추출 결과의 체인 치수 개수. 뷰별로 의미 있는 점만 체인의 endpoint로 쓰이므로 3D 뷰 치수선이 깔끔해짐. 2D 도면(`GenerateSheetDrawing2D`) 등 후행은 `chainDimensionList`를 그대로 사용해 자동 반영됨

---

## 2026-04-22 — T-023 v3: Clash 기반 연결성 판정 + 파이프라인 재배치

**유형**: refactor + feat
**커밋**: `cc72e94`
**관련 TASK**: T-023 (v3, 3차 재재설계)
**배경**: 사용자 2차 지시(STRU 단위) 재교정 → "물리적 연결성(Clash 인접) 1덩어리" 기준 확정. 정확성 우선 (방식 A 채택)
**변경 사항**:
- **사전 정리**: 직전 `2a216b5`의 STRU 주석 블록 2개(btnMainDimension 호출부 + 파일 하단 헬퍼) 완전 제거
- **파이프라인 재배치**:
  - 기존: `btnMainDimension` 안에서 BOM → Osnap → 치수 → Clash(비동기) → 이벤트에서 시트 생성
  - 신: `btnMainDimension` 안에서 BOM → Clash(비동기) → 즉시 반환 / Osnap·치수·요약·시트 전부 `Clash_OnClashTestFinishedEvent` → `CompleteMainDimensionPostClash`로 이동
  - 치수 생성 시점이 Clash 결과 수신 후로 미뤄져 **판정 실패 시 치수가 아예 만들어지지 않음** (롤백 불필요)
- **`CompleteMainDimensionPostClash(bool isSingleMember, int clashTestCount)`** 공용 메서드 신설 (Form1.BOM.cs)
  - Osnap 수집 → `MergeCoordinates` → X/Y/Z 체인 → `lvDimension` → `ShowAllDimensions` → 요약 MessageBox → `GenerateDrawingSheets` → `HideBusyOverlay`(finally)
- **`IsSingleConnectedComponent(out int componentCount)`** 헬퍼 신설 (Form1.Clash.cs)
  - Part→Body 역매핑(`bodyToPartIndexMap`) 후 `clashList`로 양방향 인접 그래프 구축
  - BFS로 연결 성분 수 계산, ≥2 발견 즉시 early exit (성능)
  - `bomList.Count == 1`은 항상 통과 (단일 부재)
- **`Clash_OnClashTestFinishedEvent`** 확장: clashList 수집 후 판정 → 실패면 MessageBox + `HideBusyOverlay` + return, 성공이면 `CompleteMainDimensionPostClash(false, testCount)` 호출. 기존 요약 MessageBox는 Post 메서드로 이동
- **T-024 fallback 통합**: 단일 부재(clashStarted=false)는 Clash 이벤트 미발동이므로 `btnMainDimension`에서 직접 `CompleteMainDimensionPostClash(true, 0)` 호출 — 판정 스킵하고 동일 파이프라인 재사용
- **차단 메시지**: "치수 추출은 모든 부재가 하나의 덩어리로 연결되어 있을 때만 가능합니다. 현재: 연결되지 않은 부재 그룹 N개 발견. 해결: 떨어진 부재를 숨기거나 한 덩어리만 선택"
- docs 3종 전면 갱신 — `main-dimension.md` 흐름도 재작성 + 단계표 3섹션(btn/Clash 이벤트/Post) + 분기 C·D + E03/E04 / `clash-finished-event.md` 개요·단계 9·10·분기 B·E03·상태 변화 2열 / 사용자 매뉴얼 `치수 추출.md` 내부 흐름·분기·에러 ③
- MSBuild Debug 통과

**영향 범위**: 치수추출 핵심 흐름 재구성. 치수 생성 타이밍이 Clash 결과 수신 후로 변경. 단일 부재 / 떨어진 부재 / 연결된 다중 부재 세 케이스 모두 같은 Post 메서드를 타는 통합 구조

---

## 2026-04-22 — T-025 BOM 테이블 자동 출력 + T-026 xray 잔존 버그 fix

**유형**: feat + fix
**커밋**: `7614417`
**관련 TASK**: T-025, T-026
**변경 사항**:
- **T-025 (feat)**: `GenerateDrawingSheets()` 끝에 `CollectBOMInfo(false, drawingSheetList[0])` 호출 추가
  - 치수추출 완료 직후 Sheet 1(전체) 기준 BOM 정보가 `lvDrawingBOMInfo`에 자동 표시
  - try/catch로 감싸 SDK 예외 시 `DiagLog`만 기록하고 앱 흐름 보호
  - visibility·카메라는 건드리지 않음 (시트 선택 이벤트의 부수효과 회피)
  - 사용자가 시트를 별도로 클릭하지 않아도 BOM 테이블이 즉시 채워짐
- **T-026 (fix)**: `btnMainDimension_Click` 진입부에 `xraySelectedNodeIndices.Clear()` 추가
  - **증상**: 부재 1개 띄우고 치수추출 → 전체 띄우고 치수추출 → **1개 기준 결과 재현** (`chain=32` 동일)
  - **원인**: `LvDrawingSheet_SelectedIndexChanged`가 시트 선택 시 설정하는 `xraySelectedNodeIndices` 값이 잔존, `CollectBOMData` L591의 X-Ray 우선 필터에 계속 걸려 "그 부재만" 수집
  - **로그 근거**: `[10:58:25] sheet#=1 members=1 → xray=1 설정` → `[10:58:34] btnMainDimension ENTER xray=1 → EXIT chain=32` (전체 띄운 뒤에도 1개 기준)
  - **원칙 확립**: "치수추출 버튼은 항상 현재 visible 기준". 특정 부재 치수는 시트/BOM 행 선택 경로가 담당
- docs: `main-dimension.md` 단계 1.3 (xray clear) / `generate-sheets.md` 단계 9.5 (BOM 자동 수집) 추가, 변경 이력 각 1건
- MSBuild Debug 통과

**영향 범위**: 치수추출 정상 흐름. T-016(3회 누적 간헐 버그)과 별개의 잔존 상태 버그 해결

---

## 2026-04-22 — T-023 재설계: STRU 단위 가드로 변경 (현재 비활성)

**유형**: refactor
**커밋**: `2a216b5`
**관련 TASK**: T-023
**변경 사항**:
- 사용자 의도 재확인: "부재 개수 1"이 아니라 **"STRU(모델트리 상위 UDA 단위) 1개"** 기준
- 직전 `1620289`의 "visible/selected == 1" 가드 **제거** (사용자 의도와 불일치)
- 새 `FindAncestorByUda(startIndex, key, value, maxDepth)` + `CheckSingleStruCondition()` 헬퍼를 **완성 형태 + 블록 주석**(`/* */`)으로 `Form1.BOM.cs` 하단에 보존
  - 선택 기반 → visible 기반 순서로 평가, 공통 조상 STRU 집합 크기 1일 때만 통과
  - 부모 탐색은 `CollectBOMInfo`의 UDA 순회 패턴 재사용
  - `Object3dFilter.SELECTED`로 프로그래매틱 선택 상태까지 포함
  - 실패 시 MessageBox + `DiagLog BLOCKED visibleStru=N selectedStru=M`
- `btnMainDimension_Click` 진입부의 호출도 `/* */` 주석 처리
- 상수 `STRU_UDA_KEY="UNIT_TYPE"`, `STRU_UDA_VALUE="STRU"`는 임시 placeholder (`TODO:` 주석). UDA 확정 시 이 두 상수 교체 + 주석 제거만으로 활성화
- docs 원복: `main-dimension.md` 단계 1.5 · 분기 D · E04를 "비활성" 표기로 교체, 사용자 매뉴얼 `치수 추출.md` 에러 ③/단계 1-2 삭제 후 "향후 추가 예정" 예고 문구로 치환
- TASKS T-023 상태: `IN_PROGRESS` → `BLOCKED` (UDA 키·값 확정 대기)
- MSBuild Debug 통과 (주석 블록이라 컴파일 영향 없음)

**영향 범위**: 치수 추출 가드 일시 비활성 — 현재는 기존처럼 모델 로드 + 예외만 검사. STRU 가드는 UDA 확정 시 활성화

---

## 2026-04-22 — T-023 치수추출 사전조건 가드 (단일 부재)

**유형**: feat
**커밋**: `1620289`
**관련 TASK**: T-023
**변경 사항**:
- `btnMainDimension_Click` 진입부에 단일 부재 가드 추가
  - `GetPartialNode(false,false,true)` 순회로 visible 부재 카운트
  - `Object3D.FromFilter(Object3dFilter.SELECTED_TOP)`로 selected 카운트 (T-022로 확보한 선택상태 API 활용)
  - 둘 다 ≠ 1이면 MessageBox로 차단 + `DiagLog BLOCKED visible=N selected=M` 기록
  - 허용 케이스: 시트/BOM 행 선택 → T-022로 selected==1 / 모델트리 체크박스로 visible==1 / 3D 뷰 단일 클릭
- 개발자 문서 `main-dimension.md`: 사전조건 항목 1건·단계 1.5·에러 E03 추가 (기존 E03은 E04로 재번호)
- 사용자 매뉴얼 `치수 추출.md`: 선결조건·단계 1-2·에러 ③ 추가
- MSBuild Debug 통과

**영향 범위**: 자동 치수 추출 진입 조건. 다중 부재 상태에서 실행은 이제 차단됨 (안전망). 기존에 전체 보기 상태에서 치수 추출을 쓰던 흐름은 단일화 절차가 필요 — UX 전환

---

## 2026-04-22 — T-024 단일 부재 치수추출 시 시트 목록 미갱신 버그 수정

**유형**: fix
**커밋**: `06a1395`
**관련 TASK**: T-024
**변경 사항**:
- **원인**: `DetectClash` 내부 루프가 `targetNodes.Count == 1`이면 쌍 없어 `clashCount == 0` → return false → `PerformInterferenceCheck` 미호출 → `Clash_OnClashTestFinishedEvent` 미발동 → 이벤트에서 호출되던 `GenerateDrawingSheets` 미실행 → 시트 목록 갱신 안 됨. 부가적으로 간섭 없는 다중 부재도 이벤트 내 `if (clashList.Count > 0)` 조건에 걸려 시트 안 생기던 숨은 버그 공존
- **수정 1**: `btnMainDimension_Click` — `bool clashStarted = DetectClash()` 반환값 수신. false면 `GenerateDrawingSheets()` + 요약 MessageBox 직접 호출 (fallback 경로)
- **수정 2**: `Clash_OnClashTestFinishedEvent` — `if (clashList.Count > 0)` 조건 제거하고 `GenerateDrawingSheets()`를 **항상** 호출. 내부 `bomList.Count > 0` 가드로 안전
- docs: `main-dimension.md` 단계표 10→13 재번호 + 분기 C 신설, `clash-finished-event.md` 단계 10 재기술 + 분기 A 수정
- MSBuild Debug 통과

**영향 범위**: 단일 부재 / 간섭 없는 다중 부재의 자동 처리 경로. 간섭 있는 다중 부재는 기존 동작 그대로

---

## 2026-04-22 — T-022 시트/BOM 선택 시 3D View 선택상태 동기화

**유형**: feat
**커밋**: `ab8313e`
**관련 TASK**: T-022
**변경 사항**:
- `vizcore3d.Object3D.Select(List<int>, true, false)` + `Select(DESELECT_ALL)` 조합으로 "선택상태(빨간 하이라이트)" 구현
- `LvDrawingSheet_SelectedIndexChanged` — 시트의 **기준부재** 하이라이트
  - Sheet 1(-1)·설치도(-2) 생략 (기준부재 개념 없음)
  - 가공도(-3) → `MemberIndices[0]` / Sheet 2+ → `BaseMemberIndex`
- `LvDrawingBOMInfo_SelectedIndexChanged` — **단일 부재** 하이라이트 + 카메라 fit (visibility 유지)
- `pivot=false`로 회전 피봇 간섭 방지, `DESELECT_ALL` 선행으로 누적 방지
- **피드백 루프 분석**: `Object3D_OnObject3DSelected`는 `dgvAttributes`만 갱신하고 ListView는 건드리지 않아 안전. 부수효과로 **부재 정보 탭이 자동 갱신**되어 UX 향상
- SDK 확정 경로: `sdk-verifier` 서브에이전트로 `VIZCore3D.NET.xml` L51882~51946 검증
- docs 2종(`lv-sheet-selected.md`, `lv-bom-info-selected.md`) 단계표·상태 변화·변경 이력 갱신

**영향 범위**: 도면정보 탭 UX (시트·BOM 행 선택). 기존 카메라 fit·visibility 동작은 그대로, 선택상태만 추가

---

## 2026-04-22 — T-018 장시간 작업 진행 오버레이 (1차: 치수 추출)

**유형**: feat
**커밋**: `ccb9cb4`
**관련 TASK**: T-018
**변경 사항**:
- 공통 헬퍼 `ShowBusyOverlay(msg)` / `HideBusyOverlay()` 신설 ([Form1.cs](../../A2Z/Form1.cs) L183~L222)
  - 3D 뷰어(`panelViewer`) 중앙에 "처리 중..." 반투명 Label(맑은 고딕 14pt Bold, 260×70, 배경 #2D2D30)
  - 최초 호출 시 지연 생성 → 이후 재사용. 크기는 panelViewer 기준 자동 센터링
  - `Application.DoEvents()`로 즉시 화면 반영
- `btnMainDimension_Click`에 try/finally 구조로 오버레이 적용
  - 각 장시간 단계 진입 시 메시지 갱신: 치수 추출 중 → Osnap 수집 중 → 치수 계산 중 → 간섭검사 실행 중
  - finally에서 `HideBusyOverlay()` 호출 — 정상·예외 모두 해제
  - Clash는 비동기라 해제 후에도 완료 콜백의 MessageBox 정상 동작
- 문서 `main-dimension.md` 단계표를 10→12단계로 확장 (2·12단계에 오버레이 표시·해제 추가), 변경 이력 1건

**영향 범위**: 치수 추출 UX. 다른 장시간 작업(2D 생성·가공도·PDF·시트 생성)은 1차 반응 보고 2차에서 확장 검토. 기능 로직 무변경

---

## 2026-04-22 — T-017 라이선스 코드 Form1.License.cs로 분리

**유형**: refactor
**커밋**: `d849663`
**관련 TASK**: T-017
**변경 사항**:
- `Form1.BOM.cs`에 섞여 있던 라이선스 관련 코드 전부를 신규 partial `Form1.License.cs`로 이동
  - 이동 대상: `StartLicenseRefreshTimer`, `LicenseRefreshTimer_Tick`, `licenseRefreshTimer` 필드, `Vizcore3d_OnInitializedVIZCore3D`의 `License.LicenseServer("127.0.0.1", 8901)` 초기 호출 2줄
  - 새 진입점 `InitializeLicense()` — 서버 연결 실패 시 MessageBox + `false`, 성공 시 갱신 타이머 시작 + `true`
  - `Vizcore3d_OnInitializedVIZCore3D` 진입 블록 10줄 → `if (!InitializeLicense()) return;` 한 줄로 축약
- `A2Z.csproj`에 `Form1.License.cs` Compile 항목 추가 (`DependentUpon=Form1.cs`, `SubType=Form`)
- `Form1.cs`에서 `licenseRefreshTimer` 필드 선언 제거 (License.cs로 이동)
- docs: `code-reference/form1-bom.md` 라이선스 항목 5곳(헤더 라인 수·핸들러 설명·헬퍼 표·필드 표·API 사용) 정리, `code-reference/form1-license.md` 신설, `features/bom/vizcore3d-initialized.md` 단계표·E01 에러·관련 링크·변경 이력 갱신
- 기능 변경 없음 (순수 리팩토링). MSBuild Debug 통과, 경고 0. 사용자 실기에서 앱 기동 정상 확인

**영향 범위**: 라이선스 로직 파일 경계만. 호출 규약은 동일 — 다른 핸들러/모듈 무영향

---

## 2026-04-22 — T-014 시트 목록 item 번호 표시 + T-021 BOM 행 카메라 fit

**유형**: feat
**커밋**: `9b99b8c`
**관련 TASK**: T-014, T-021
**변경 사항**:
- **T-014 (`lvDrawingSheet` 표시 포맷)**: 기준부재/포함부재 컬럼을 부재 이름 대신 **item 번호**(= `bomList` 순서 i+1 = ISO 풍선 번호 = BOM 정보 탭 No.)로 표시
  - Sheet 1 → "전체 / 전체"
  - Sheet 2+ → `{기준번호} / {포함 번호 오름차순 콤마}` (예: `1 / 1, 3, 5`)
  - 설치도 → "설치도 / {전체 item 번호}"
  - 가공도 → `{MemberIndices[0]의 item 번호} / 공란`
  - 시트 생성 로직은 T-015 그대로 유지 (표시 전용 변경)
  - `bomIndexToItemNo` Dictionary 신설 후 ListView 채우기 블록 전면 재작성 (Form1.DrawingSheets.cs L215~281, +약 50줄)
  - 빌드 오류 1건 수정: 상단 `int mfgNo=1`(가공도 번호)과 변수명 충돌 → `mfgBomIdx`/`mfgItemNo`로 리네임
  - 문서 `generate-sheets.md` 단계 10 설명·상태 변화 섹션·변경 이력 갱신
- **T-021 (`lvDrawingBOMInfo` 행 선택 핸들러)**: BOM 테이블 행 선택 시 해당 부재로 카메라만 fit
  - 가시성은 그대로 두고 `vizcore3d.View.FlyToObject3d(new List<int>{bodyIdx}, 1.2f)` — 현재 시트 맥락 유지
  - No. 컬럼 파싱 → `bomList[No-1].Index` Body 조회 (CollectBOMInfo의 `partIndexToBomNo` = `bi+1` 매핑과 일치)
  - 요약행(Row 0) · No 파싱 실패 · 범위 초과는 조용히 return, SDK 예외는 `DiagLog`로 기록
  - Form1.cs L166에 `lvDrawingBOMInfo.SelectedIndexChanged += LvDrawingBOMInfo_SelectedIndexChanged` 등록
  - 신규 문서 `lv-bom-info-selected.md` (SHT-010) + `_index.md` 등록 추가

**영향 범위**: 도면정보 탭 UI(시트 목록 + BOM 테이블) 상호작용. 시트 생성·가공도·설치도 내부 로직은 변화 없음. 사용자 실기 테스트 통과 (2026-04-22)

---

## 2026-04-21 — T-015 Sheet 생성 로직 재설계 (모든 부재가 기준부재)

**유형**: feat (기능 변경)
**커밋**: `9b870a0`
**관련 TASK**: T-015
**변경 사항**:
- **문제**: `GenerateDrawingSheets` L105-142의 `appearedAsIncluded` 스킵 로직이 "부재가 이미 다른 시트에 포함부재로 등장하면 기준부재가 될 수 없음"을 강제 → 1-2-3-4 연쇄 Clash 시 Sheet 2(기준 1, {1,2}) + Sheet 3(기준 3, {3,2,4}) 2개만 생성. 사용자 의도(각 부재가 자기 기준 시트를 가짐)와 불일치
- **수정**: Form1.DrawingSheets.cs에서 `HashSet<int> appearedAsIncluded` 선언, `Contains` 스킵 조건, `Add` 호출 3곳 전부 제거. 주석도 T-015 결정 배경으로 교체
- **결과**: 모든 부재가 각자 기준부재로 등장하며 자기 + 1-hop 이웃 시트 생성. 1-2-3-4 연쇄 Clash → Sheet 2(1), 3(2), 4(3), 5(4) 4개. 단계 9 Sheet 1 중복 제거는 유지되어 과잉 정리 자동
- 문서 `generate-sheets.md` 전면 갱신: flowchart 재작성, 단계표 11단계로 확장(Part↔Body 매핑·인접 리스트·가공도·중복 제거 추가), 분기 B·C 재정의, 상태 변화 시트 수 공식, 변경 이력 한 줄
- **부수 정리**: 기존 문서의 E03(clashList 비어있을 때 return) 서술은 실제 코드에 없어 삭제. 대신 `clashList` 공백 시 "일반 시트들이 자기 자신만 포함"이라는 실제 동작 주석 추가
- 사용자 빌드·실기 검증은 본인 기기에서

**영향 범위**: 시트 생성 로직 전체 + 대응 문서. 설치도·가공도·Sheet 1 로직은 변경 없음

---

## 2026-04-21 — T-013 OPT-B2 진단 로그 확장 (MoveObject 유효성 검증)

**유형**: chore
**커밋**: `7688905`
**관련 TASK**: T-013
**변경 사항**:
- OPT-B2 구현 후 사용자 보고: 6.21mm 이동이 계산됐는데 시각적으로 "위치 전혀 안 바뀜"
- 진단 로그 확장 — `MoveObject` 직후 `objId`의 실제 최종 상태 기록:
  - `objFinal=(x,y)` — 이동 후 실제 중심 (target과 일치하는지 검증)
  - `objFinalSize=(w,h)` — 렌더된 실제 크기 (obj가 너무 작아 보이는지 확인)
  - `move=(dx,dy)` — 실제 호출된 이동량
- 이전 커밋(ebef55d)에서 DiagLog 메시지에 `objFinalCX/CY/W/H` 참조를 넣었으나 변수 선언이 누락된 상태였음 → 이번에 선언 + 계산 추가로 컴파일 건전성 복구
- 판정 기준: `objFinal ≈ target`이면 이동 정상이고 `objFinalSize`가 작아 체감이 적은 것; `objFinal ≠ target`이면 `MoveObject` 자체 무효화 의심

**영향 범위**: 진단 로깅만. 흐름 무변화

---

## 2026-04-21 — T-013 옵션 B2 재수정 — bg BBox 꼭지점 8개 투영 기반 비율

**유형**: fix
**커밋**: `ebef55d`
**관련 TASK**: T-013
**변경 사항**:
- 1차 보정(`* bgFinalScale`) 결과 여전히 부정확 (실측 `offsetRatio.Z=-0.244` → 7.3mm 이동이 정답인데 5.9mm 계산)
- **근본 원인 확정**: `bgFinalScale`은 "객체 원본 좌표 → 현재 표시 크기" 비율, `WorldToScreen`은 "3D → 원본 캔버스" 좌표. 두 변환 체인이 서로 다른데 한 스케일로 퉁치면 오차 발생
- **정확한 공식**:
  1. bg의 3D BBox 8개 꼭지점을 모두 `WorldToScreen`으로 변환
  2. 결과 8개 점의 X/Y min·max로 **원본 캔버스상 bg의 BBox 폭/높이** (`bgScreenW/H`) 계산
  3. bg의 현재 렌더 크기(`GetObjectSize` → `bgCanvasW/H`) 대비 비율 `ratio = bgCanvasSize / bgScreenBBox`
  4. `target = bgCanvas + dScreen × ratio`
- 실측 검증: `dScreen.Y=195.97 × (30.0/bgScreenH) ≈ 7.3mm` = `offsetRatio.Z × bgCanvasH = 0.244 × 30 = 7.3mm` ✅
- DiagLog 라벨 `OPT-B` → `OPT-B2`, `bgScreenBBox`/`ratio` 필드 추가
- A2Z.exe 실행 중이라 빌드 자동 검증 생략, 사용자 빌드에 맡김

**영향 범위**: Sheet2+ ISO objId 위치만. 다른 로직 무영향

---

## 2026-04-21 — T-013 옵션 B 스케일 보정

**유형**: fix
**커밋**: `2d5fb5f`
**관련 TASK**: T-013
**변경 사항**:
- 옵션 B 1차 시도 결과: obj가 "엄청 멀리" 생김 (사용자 실측 11:06:29)
  ```
  bg3D=(26368.5, -5824.0, 17673.0)   obj3D=(26368.5, -5824.0, 17391.0)   (Z -282mm 차이)
  bgScreen=(163.00, 166.01)          objScreen=(163.00, 361.98)          (dScreen.Y=195.97)
  ```
- **진단**: `WorldToScreen`은 **원본 캔버스 좌표**(스케일 적용 전) 반환. 그런데 `bgObjId`는 이미 `RescaleObject(bgFinalScale=0.0301)`로 축소된 상태 → 두 좌표계 불일치 → `dScreen` 그대로 더하면 195mm 이동(A4 세로 210mm 거의 끝)
- **수정**: `target = bgCanvas + dScreen * bgFinalScale`
  - 검증값: `195.97 × 0.0301 = 5.90 mm` → 셀(95mm) 내부에서 Z축 3D 차이(-282mm)를 반영한 자연스러운 위치
- 변경 분량: 2줄 (`targetX`, `targetY`에 `* bgFinalScaleB` 추가)
- 빌드 검증: A2Z.exe 실행 중이라 DLL 잠금으로 이번 세션 자동 검증 불가. 사용자 빌드에 맡김

**영향 범위**: Sheet2+ ISO 뷰 objId 위치만. 다른 로직 무영향

---

## 2026-04-21 — T-013 옵션 B: WorldToScreen 기반 objId 위치 보정

**유형**: fix
**커밋**: `705613a`
**관련 TASK**: T-013
**변경 사항**:
- **옵션 A 실패 확정** (사용자 실측 11:00:06 로그):
  ```
  bgScale=0.0301 objScale=0.0050
  bgCenter=(49.50,157.50) objCenter=(0.00,0.00)
  ```
  objId가 원점 (0,0)에 0.005 스케일로 남아 사실상 보이지 않음 → SDK 자동 매핑 없음 확인
- **옵션 B 구현** (Form1.DrawingSheets.cs `RenderSheetViewForDrawing` isIsoFullView 분기):
  - 전체 BOM 3D BBox 중심 + 시트 부재 3D BBox 중심 계산 (`bomList.MinX/MaxX/...`)
  - 각 중심을 `vizcore3d.View.WorldToScreen(Vertex3D, true)`로 캔버스 좌표 변환
  - objId를 bgFinalScale과 동기 스케일링 (`RescaleObject`)
  - objId 중심을 `bgCanvas + (objScreen - bgScreen)`로 이동 (`MoveObject`)
- DiagLog `OPT-B` 라벨로 3D 중심 / 화면 좌표 / 이동량 / 최종 스케일 모두 기록 — 다음 테스트 결과 즉시 검증 가능
- SDK API 근거: [VIZCore3D.NET.xml:63853](../../VIZCore3D.NET.xml) `ViewManager.WorldToScreen`

**영향 범위**: Sheet2 이상 시트의 ISO 뷰 렌더링만. 비-ISO / Sheet1 미영향

---

## 2026-04-21 — T-020 파일 열기·치수 추출을 탭 밖 공용 패널로 이동

**유형**: feat (UX)
**커밋**: `29e177f`
**관련 TASK**: T-020
**변경 사항**:
- `panelGlobalActions` 신설 (splitContainer1.Panel1, Dock.Top, 438×60)
  - 위치: panelGlobalViewButtons 아래, tabControlLeft 위
  - 배경색 통일 (`45,45,48` — 글로벌 뷰 버튼 패널과 같음)
- `btnOpen`(파일 열기), `btnMainDimension`(치수 추출) 이관
  - 기존: `tabPageWork > groupBox1` (작업/데이터 탭에서만 보임)
  - 신규: `splitContainer1.Panel1 > panelGlobalActions` (모든 탭 공통)
  - Location (x, 25) → (x, 5)
- `groupBox1` 후속 정리: Size 110→55, 작은 버튼 6개(BOM/Clash/Osnap/치수/2D 생성/PDF 내보내기) Y=78→20 위로 당김
- 자동화된 사용자 흐름(파일 → 치수 추출 → 2D 도면 → 가공도) 중 첫 2단계를 항상 한 손에 접근 가능하게 함 (담당자 목표 = 자동화)
- 사용자 직접 빌드·실행 확인 완료

**영향 범위**: UI 레이아웃만. 핸들러 흐름·이벤트 핸들러 참조 영향 없음

---

## 2026-04-21 — T-019 탭 순서 재배열 (도면정보를 첫 번째로)

**유형**: feat (UX)
**커밋**: `3f51a02`
**관련 TASK**: T-019
**변경 사항**:
- `tabControlLeft.Controls.Add` 순서 변경: 도면정보 → 작업/데이터 → 부재 정보
- `tabPageDrawing.TabIndex = 0`, `tabPageWork.TabIndex = 1`, `tabPageAttribute.TabIndex = 2`
- 앱 실행 시 `SelectedIndex = 0`에 의해 **도면정보 탭이 기본 선택**됨 — 사용자(담당자) 최종 목표가 제작도 출력이라 즉시 작업 화면 노출
- 프로그래밍 위험 전수 검증: `SelectedTab == tabPageDrawing` 등 모든 참조가 **탭 객체 기반**이라 순서 변경 안전
- 런타임 로직/이벤트 핸들러/핸들러 흐름 영향 **0** (Designer 메타데이터만 변경)

**영향 범위**: UI 탭 순서. 기존 기능·핸들러 영향 없음

---

## 2026-04-21 — T-013 옵션 A 시도 (Sheet2+ ISO 위치 정합)

**유형**: fix (시도)
**커밋**: `cac4eb3`
**관련 TASK**: T-013
**변경 사항**:
- **원인 확정**: `RenderSheetViewForDrawing`의 `isIsoFullView` 분기에서 bgObjId/objId 모두 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin`로 캔버스 원점에 생성 → `GetObjectCenter`가 둘 다 (0,0) 반환 → 기존 위치 보정 공식 `(objCX0 - bgCX0) * scale`이 0에 가까워져 obj가 bg 중심으로 이동하던 현상
- **옵션 A 시도**: Form1.DrawingSheets.cs L1430~1468 범위의 objId 변환 로직 전체(RescaleObject + GetObjectCenter 보정 + MoveObject + 디버그 출력) 제거
- SDK가 동일 카메라·동일 원점에서 만든 두 객체를 동일 좌표계로 자동 매핑하는지 검증
- DiagLog로 bgObjId/objId의 스케일·중심·원본좌표 실측 기록 (다음 테스트 시 로그로 결과 판정)
- 실패 시 옵션 B(`WorldToScreen` 기반 3D→2D 좌표 변환)로 전환 예정 — SDK API 이미 확인됨

**영향 범위**: Sheet2 이상 시트의 ISO 뷰 렌더링. 비-ISO 뷰(X/Y/Z) 및 Sheet1(전체) 미영향

---

## 2026-04-21 — T-016 진단 로그 파일 저장 방식 전환

**유형**: chore
**커밋**: `53c6245`
**관련 TASK**: T-016
**변경 사항**:
- Form1.cs에 `DiagLog` 헬퍼 신설 — 파일(`{exe}/logs/diag-{YYYY-MM-DD}.log`) + VS 출력창 병행 기록
- 기존 T-016 진단용 `Debug.WriteLine` 13곳 → `DiagLog`로 일괄 교체 (Python 스크립트)
  * `Form1.BOM.cs btnMainDimension_Click` 3곳
  * `Form1.Dimensions.cs btnExtractDimension_Click` 3곳
  * `Form1.DrawingSheets.cs LvDrawingSheet_SelectedIndexChanged` 5곳
  * `Form1.GlobalViews.cs ExtractInstallationDimensions` 2곳
- Release 빌드 + 다른 기기 실행에서도 로그 파일 생성되어 T-016 재현 진단 가능
- `.gitignore`의 기존 `[Ll]ogs/` 패턴으로 로그 파일 자동 제외

**영향 범위**: 진단 로깅만. 기능·흐름 변경 없음

---

## 2026-04-20 — T-016 진단 로그 인프라 추가 (간헐 버그 추적용)

**유형**: chore
**커밋**: `0b5731c`
**관련 TASK**: T-016 (BLOCKED 전환)
**변경 사항**:
- 치수 추출 흐름의 4개 핵심 지점에 `Debug.WriteLine` 진단 로그 추가
  - `Form1.BOM.cs btnMainDimension_Click` ENTER/EXIT (xray·chain·osnap·bom 카운트)
  - `Form1.Dimensions.cs btnExtractDimension_Click` ENTER/EXIT
  - `Form1.DrawingSheets.cs LvDrawingSheet_SelectedIndexChanged` ENTER/SKIP/EXIT/FAIL (sheet#, prevXray, prevChain)
  - `Form1.GlobalViews.cs ExtractInstallationDimensions` ENTER/EXIT (members, chain)
- `LvDrawingSheet_SelectedIndexChanged`의 silent catch (`Debug.WriteLine($"도면 시트 표시 중 오류: {ex.Message}")`)에 **stack trace 추가**
- 모든 로그에 `[T-016 진단 로그]` prefix 또는 `HH:mm:ss.fff` 시각으로 필터링·시계열 분석 가능
- 다음 재현 시 Visual Studio 출력창 로그를 사용자가 공유하면 즉시 원인 특정 가능
- T-016 상태 `IN_PROGRESS → BLOCKED (재현 조건 수집 중)`로 이동 + 의심 가설 4개 보존

**영향 범위**: 치수/시트 흐름 4개 핸들러에 로깅만. 기능·흐름 변경 없음 (R9 기준 docs 갱신 불필요)

---

## 2026-04-20 — 시드 서브에이전트 2개 도입 (sdk-verifier, md-link-checker)

**유형**: feat
**커밋**: `92d0488`
**관련 TASK**: T-011
**변경 사항**:
- `.claude/agents/sdk-verifier.md` 신설 — VIZCore3D.NET.xml 선행 검색으로 SDK API 존재·시그니처·공식 사용 패턴 반환
- `.claude/agents/md-link-checker.md` 신설 — `docs/**/*.md` 링크 공백·파일 부재 검증 + Python 치환 스크립트 제안
- `CLAUDE.md` R10, R11 추가 — 각 에이전트 호출 트리거 주소
- 배경: 이번 세션에서 드러난 반복 실수(`RenderModes.SOLID` 존재 가정, `Model.Close` 누락, 링크 공백 133건) 방지
- 오케스트레이터 프로토콜(동적 생성·합병·삭제)은 사용 패턴 축적 후 재평가 — 중간 도입 경로 채택

**영향 범위**: 개발 워크플로우. 코드 변경 없음.

---

## 2026-04-20 — T-006/T-009 빌드 테스트 후속 + T-010 링크 치환 + 자동 push 활성화

**유형**: fix + chore
**커밋**: `10c7d8c`
**관련 TASK**: T-006, T-009, T-010
**변경 사항**:
- **T-006 후속** (템플릿 폭 재조정): BOM/tableInfo 열 너비 합 81→**77mm** 추가 축소. BOM: ITEM 19→17, MATERIAL/SIZE 12→11. tableInfo: 32/49→30/47. (RenderTemplateOnGridStructure가 셀 92.3mm 내부에 추가 패딩 존재)
- **T-009 후속** (Clear2DView 시점 수정): `Clear2DView()` 호출을 `Model.Open` 성공 이후로 이동. 기존엔 Open 직전에 호출했는데 Open이 2D 뷰를 자동 복원하여 효과 없었고 번쩍임 4회 발생. 이제 Open 성공 분기 내부에서 마지막 단계로 실행
- **T-010** (링크 공백 일괄 치환): `docs/**/*.md` 전체 마크다운 링크 `]( ... )` 내부 공백을 `%20`으로 치환. Python 스크립트로 30파일 147건. 외부 URL(http/https/mailto), 앵커(#), 공백 없는 링크는 제외 처리
- **chore** (/commit 자동 push 통합): CLAUDE.md R5 개정, `.claude/commands/commit.md`의 단계 9에 자동 push 추가, 메모리에 `Commit Auto-Push` feedback 기록. 다중 기기 테스트 환경 지원

**영향 범위**: BOM 카테고리 (Form1.BOM.cs `ResetToInitialState`), DrawingSheets 카테고리 (BOM/tableInfo 폭), docs/ 전체 링크, 개발 워크플로우 (자동 push)

---

## 2026-04-20 — 초기화 버튼 추가 + 같은 파일 재Open 버그 수정

**유형**: feat + fix
**커밋**: `45d17dd`
**관련 TASK**: T-008
**변경 사항**:
- `btnResetToInitial` ("초기화", 회색) 신설 — 3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽
- `ResetToInitialState()` — 누적 상태 전면 초기화 후 `currentFilePath` 동일 경로로 재로드
  - 정리 대상: bomList/clashList/osnapPoints/osnapPointsWithNames/chainDimensionList/xraySelectedNodeIndices/drawingSheetList/bodyToPartNameMap/balloonOverrides + lv* ListView 5종 + SDK Review.Measure/ShapeDrawing/Review.Note
  - `balloonOverrides.Clear()` 포함 (btnOpen이 누락했던 항목)
- **버그 수정**: VIZCore3D는 같은 경로 중복 `Model.Open()`을 거부 (false 반환)
  - `ResetToInitialState()` 및 `btnOpen_Click` 양쪽에 `if (Model.IsOpen()) Model.Close();` 선행 호출 추가
  - 근거: VIZCore3D.NET.xml 공식 예제 L47297, L60261 패턴
- **UI 너비 축소**: 5개 글로벌 뷰 버튼 Size 105→80, Location 재배치(8/93/178/263/348), 패널 Size 558→438
- 문서 신규:
  - `docs/features/bom/reset-to-initial.md` (BOM-005)
  - `docs/사용자-매뉴얼/1.기본-작업/초기화.md`
- 문서 갱신:
  - `docs/features/bom/open-model.md` — Close 단계 추가, flowchart·step table·변경 이력
  - `docs/features/bom/_index.md` — BOM-005 항목 + 의존성 다이어그램 재로드 화살표
  - `docs/code-reference/form1-bom.md` — 새 핸들러 섹션 + 라인 번호 shift 반영
  - `docs/사용자-매뉴얼/README.md` — 1.기본 작업에 [초기화] 링크

**영향 범위**: BOM 카테고리 (Form1.BOM.cs + Form1.Designer.cs) + 대응 문서. 핸들러 흐름 변경 있음 (btnOpen 포함 2개 흐름에 Close 단계 삽입)

---

## 2026-04-14 — 사용자 매뉴얼 전면 작성 (39개 버튼 문서)

**유형**: docs
**커밋**: `74fe209`
**관련 TASK**: T-003
**관련 REQUEST**: REQ-001
**변경 사항**:
- `docs/사용자-매뉴얼/` 신규 폴더 생성 — 40개 파일 (README + 39 버튼 문서)
  - `1.기본-작업/` 2개 (파일 열기, 치수 추출)
  - `2.작업-데이터 탭/` 12개
  - `3.부재 정보 탭/` 7개
  - `4.도면정보 탭/` 6개
  - `5.목록 조작/` 12개
- 7섹션 표준 템플릿 적용 (한 줄로 / 버튼 위치 / 사전 조건 / 누르면 순서 / 분기 / 에러 / 이어지는 작업 / 자세히 보기 / 변경 이력)
- 실제 UI 라벨(Form1.Designer.cs `.Text = "..."` 원본)을 파일명·위치 표기에 사용
- SDK 용어 전면 번역 적용 (`DASH_LINE` → "은선(점선) 모드", `bomList` → "BOM 목록" 등)
- 에러 메시지는 실제 MessageBox 팝업 문구 원문 그대로 수록
- `docs/README.md` 상단에 "개발자용 / 사용자용" 분기 카드 추가
- 개발자 문서(`docs/features/`, `docs/code-reference/`)는 건드리지 않음

**실행 방식**: 멀티 에이전트 (인벤토리 W-D 선행 → Writer W-A/B/C 병렬 작성 → Reviewer 전수 검토)
**검토 결과**: 템플릿 0위반 / 용어 0위반 / 깨진 링크 0건 / 에러 메시지 샘플 3건 전부 일치

**영향 범위**: 신규 문서 생성만 (코드 변경 없음)

---

## 2026-04-13 — 워크플로우 자동화 확장 (REQUESTS + /checkpoint + docs-sync 훅)

**유형**: chore
**커밋**: `ac14c86`
**관련 TASK**: T-002
**변경 사항**:
- `docs/tracking/REQUESTS.md` 신규 — 본인 수정 요청 inbox (REQ-xxx, 우선순위/배경/기대효과 필드)
- `.claude/commands/checkpoint.md` 신규 — 세션 요약 저장 슬래시 커맨드
  - 주제 kebab-case 변환, 중복 시 suffix
  - 필수 섹션: "이어갈 지점" (다음 세션 복원용)
  - git 미커밋 변경 있으면 ⚠️ 경고 자동 추가
- `.claude/settings.json` 신규 — PostToolUse 훅 등록 (Edit|Write 매처)
- `.claude/hooks/docs-sync-reminder.sh` 신규 — `Form1.*.cs` 수정 시 docs 동기화 리마인더 주입. jq 불필요 (순수 bash + grep/sed)
- `CLAUDE.md` 수정
  - R2 확장: TASKS.md `IN_PROGRESS` + sessions/ 최신 + FEEDBACK OPEN + REQUESTS OPEN 4개 자동 훑기
  - R4에 `/checkpoint` 커맨드 명시
  - R8 신규: 본인 요청은 맥락 중심 기록
  - R9 신규: 훅 리마인더는 신호일 뿐 맹목 추종 금지
  - 파일 구조 개요에 REQUESTS/hooks/checkpoint 반영
- `.claude/commands/commit.md` 수정 — REQ-xxx 처리 추가 (단계 4·5·6)
- `docs/tracking/README.md` 수정 — 파일 테이블 5행, ID 체계에 REQ- 추가, 워크플로우 Mermaid에 REQUESTS/checkpoint 반영
- `docs/README.md` 수정 — tracking 섹션에 REQUESTS.md + sessions/ 링크 추가

**영향 범위**: 개발 워크플로우 자동화만 (코드 변경 없음)

---

## 2026-04-13 — 프로젝트 초기 셋업 + 로직 흐름 문서화

**유형**: chore + docs
**커밋**: `0000000` (초기 커밋)
**관련 TASK**: T-001
**변경 사항**:
- git 저장소 초기화 및 원격 연결 (github.com/uuuuj/a2z)
- 기존 원격 `HYI` 브랜치를 `X_HYI`로 아카이브
- 현재 로컬 상태를 새 `HYI` 브랜치로 업로드 (초기 커밋 97개 파일)
- `docs/` 로직 흐름 문서 72개 작성
  - 카테고리 8개 (bom/clash/dimensions/drawing2d/drawing-sheets/global-views/mfg-drawing/attribute)
  - 핸들러 문서 48개 (버튼/이벤트 단위 Step-by-step 흐름)
  - 코드 레퍼런스 9개 (Form1.*.cs + Models.cs)
  - 최상위 가이드 5개 (README/용어집/파이프라인/템플릿/작성가이드)
- `.gitignore` 보강 (VS/.NET/NuGet/Claude Code 로컬 설정 등)
- `CLAUDE.md` — Claude Code 작업 규칙 R1~R7
- `docs/tracking/` — FEEDBACK/TASKS/CHANGELOG/sessions 4축 구조
- `.claude/commands/commit.md` — `/commit` 슬래시 커맨드 (docs 동기화 + CHANGELOG/TASKS 갱신 + 커밋)

**영향 범위**: 전체 저장소 구조 (코드 변경 없음)
