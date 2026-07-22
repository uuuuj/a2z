# 작업 목록 — TODO

> ⬅ [TASKS 인덱스](../TASKS.md)  ·  [TODO](./TODO.md) · [IN_PROGRESS](./IN_PROGRESS.md) · [BLOCKED](./BLOCKED.md) · [DONE](./DONE.md)

---


<!--
회사 doc 동기화 — 2026-05-04 새 우선순위 문서 기준 (전 버전은 CHANGELOG 참조)

### 개발 요청 — 상 (11건)
| # | 회사 doc 요지 | 매핑 ID | 현재 상태 / 차단 사유 |
|---|---|---|---|
| 1 | Base Template 생성기준 | T-043 | TODO — 산출물 형식(개발자 doc / 다이어그램 / 매뉴얼) 결정 대기 |
| 2 | Base Template 내 제작도 배치 기준(ISO/X/Y/Z) | T-043 | TODO — #1과 동일 작업 단위 |
| 3 | 검토자 Excel Template 일치 검증 | T-057 (신규) | TODO — Excel 파일 수신 대기 |
| 4 | Hole 풍선 제작도 X / 가공도 O 정리 | T-044 | **DONE (2026-06-15)** — 가공도 전용 확정 |
| 5 | 치수 Text 보조선 초과 시 우측 자동 배치 | T-058 | **DONE (2026-05-06)** — `AlignDistanceTextPosition = 2` 5곳 글로벌 적용 |
| 6 | 치수 Text-치수선 겹침 해결 | T-040 | TODO — T-039 선행 |
| 7 | Item별 2D 출력(Sheet2~설치도 전) 점선/실선 + 위치 정합 | T-013 | IN_PROGRESS — 신규 Match/Crop API 구현, 제작도 주변 점선 실기 검증 대기 |
| 8 | 치수선-모델 거리 X/Y/Z 뷰별 보조선 길이 + offset 고정 | T-039 | TODO — T-038 선행 |
| 9 | 치수선-치수선 offset 거리 다름 | T-039 | TODO — #8과 동일 작업 단위 |
| 10 | BOM Size 열 Spec + 길이 표기 | T-045 | TODO — 결합 형식·단위·데이터 출처 결정 대기 |
| 11 | XYZ축 치수가 다른 평면 길이 표시 검토 | T-059 (신규) | TODO — 재현 케이스(부재 Index + 스크린샷) 대기 |

### 개발 요청 — 중 (2건)
| # | 회사 doc 요지 | 매핑 ID | 현재 상태 / 차단 사유 |
|---|---|---|---|
| 1 | Slot Hole / Hole 제작도·가공도 기준 정리 | T-047 | **DONE (2026-06-15)** — Hole/SlotHole/EarthBoss만 가공도 표시 |
| 2 | 가공도 EA Type 회전 오류 | T-048 | TODO — 재현 부재 Index 대기 |

### Softhills API 확인요청 (7건, 외부 추적 — 우리 작업 아님)
| # | 항목 | 추적 ID |
|---|---|---|
| 1 | Osnap 생성 기준 (Softhills 1차 답변 불충분) | T-051 (TODO, 추가 답변 대기) |
| 2 | 점선이 PDF 출력에서 굵은 실선 표현 | SDK-003 (외부) |
| 3 | 2D ISO 모델 모서리·홀 표현 누락 | SDK-004 (외부) |
| 4 | 모델 트리 Body/Node/Part 구분 | SDK-005 (외부) |
| 5 | 곡진 부분(Round/Fillet) 반지름 추출 API — 현재 닫힌 원만 `GetCircleData`로 직경 추출 가능, 모서리 라운드·원통 곡률 전용 API 부재 (2026-06-15 사용자 개발 요청 예정) | SDK-006 (외부) |
| 6 | 엑셀 템플릿에 서로 다른 이미지 여러 개 배치 — 현재 `{Image}` 예약어 1개(로고)만 동작. `{Input_N}` 경로 주입은 텍스트로만 나오고 `RenderTemplate(TemplateTableData)`는 실제로 렌더 안 됨(SDK 문서에 이미지 전용 멤버 없음). 다중 이미지 슬롯 공식 방법 요청 (2026-06-23 사용자 개발 요청 예정) | SDK-007 (외부) |
| 7 | 부재 간 접합 각도 자동 측정·표시 API — 현재 각도 SDK는 `AddCustom3PointAngle`(3점 직접 지정)뿐이라 접합점·부재 길이축을 앱이 osnap으로 추정(사선·짧은 부재에서 오차, 접합 각도 표시 흔들림). 요청안: ① 노드 2개 지정 자동 각도 ② 노드 주방향(길이축) 벡터 반환 ③ 2D 모서리 2개 코드 지정 각도 치수 ④ 접촉 모서리(접합선) 좌표 반환 (2026-07-06 사용자 개발 요청 예정, 요청서 초안 전달 완료) | SDK-008 (외부) |

### 검토 대기 (사용자 정리 11건, 2026-05-04 추가)
사용자가 직접 정리한 "수정완료 확인 대기" 항목 — 검토자·회사 답변 또는 사용자 본인 검증 필요. **이 명단은 사용자가 외부에서 받아 처리해야 하는 우선 항목이며, 매핑 ID 출처가 회사 doc이든 사용자 본인이든 모두 우선 처리 영역에 속한다.**

| # | 사용자 표현 요지 | 매핑 ID | 상태 / 차이 |
|---|---|---|---|
| 1 | 풍선·심볼 반영 기준 정의 (풍선=모델 선+텍스트 박스, 심볼=특이사항) | T-054 | TODO. 사용자 정의 초안 보유, 검토자 확정 대기 |
| 2 | Item 기준부재 이름 부여 — GetPartialNode → Osnap → Z-MAX 내림차순 | T-056 (DONE 검증보고서) | **부분 일치**: 코드 `BBox.MaxZ` vs 명세 `Osnap.Z`. **결정 (2026-05-05)**: BBox 유지, § 7 단답으로 회신 |
| 3 | 치수추출 3회 이상 시 추가 진행 안 됨 (A선택→포함부재 제거→B선택 반복) | T-016 | BLOCKED 간헐. 사용자 재현 정상이지만 다음 발생 대기. 로그 인프라 추가 완료 |
| 4 | 치수추출 사전조건 — 부재 1덩어리(연결성)만 허용, 떨어진 부재 시 팝업 | T-023 v3 (DONE) | 일치. Clash 인접 그래프 기반 연결 성분 == 1 판정 |
| 5 | 가공도 치수 보조선 이중쇄선 → 가는 실선 | T-046 (DONE) | 완료. 4경로 통합 SOLID + 토글 패턴 제거 |
| 6 | 치수 보조선 시작점이 Osnap에서 일정 거리 띄움 | T-046 확장 (DONE) | 완료(10mm gap). **주의**: 치수선이 모델 안쪽 배치 케이스에서 gap이 모델 안으로 진입 가능. T-060(본인 개선)으로 재현 케이스 수집 중 |
| 7 | 기준부재·포함부재 표현 — Sheet1: 전체(BOM이름) / 1, 2, 3... 기준부재는 Node 이름 | T-042 (DONE) | 사양 재정리 중. 사용자 새 아이디어 LCA 노드 이름 채택 가능성, T-042 현행("전체" 유지)로 두고 후속 신규 작업 예정 |
| 8 | Sheet1 포함부재 "전체" → "1, 2, 3..." | T-052 (DONE) | 완료 |
| 9 | 중복 Sheet 삭제 후 번호 재채번 | T-053 v2 (DONE) | 완료. v2에서 "포함부재 동일 시 첫 등장만 살림"으로 확장 |
| 10 | 치수추출 후 3D View 치수 미표기 (메모리만, 4경로 그리기 정의) | T-029 + T-049 (DONE) | 완료. 4경로 그리기 정책 메인 치수 추출.md § 7.5 등록 |
| 11 | 3D View 축 정보 항상 표시 | T-050 (DONE) | 완료. `View.MarineAxis.Visible = true` 한 줄 |

### 본인 개선 사항 (회사 doc + 검토 대기 명단 외)
사용자가 직접 발견·요청한 개선 항목으로, **회사 doc 13건 / Softhills 4건 / 검토 대기 11건 어디에도 포함되지 않으면서** 진행 중인 작업. 사용자 분류 원칙: "회사·검토자가 해달라는 것부터 처리해야 하니까 그 외는 본인 개선으로 분리".

| ID | 작업 | 출처 / 상태 |
|---|---|---|
| T-004 | ALL 출력 후 시트별 도면 즉시 미리보기 | FB-001 / TODO |
| T-005 | 치수 배치를 Osnap 외곽 방향으로 | FB-002 / TODO |
| T-006 | 2D 도면 그리드 (1차 완료, 2차 SDK clip 조사 등 5건 묶음) | FB-003/004 / IN_PROGRESS |
| T-012 | 엑셀 템플릿 하이브리드 실험 PoC | REQ-002 / TODO (T-057과 시너지) |
| T-028 | 치수 로직 4경로 통합 | 사용자 직접 / IN_PROGRESS (실기 확인 대기) |
| T-032 | 치수 계산 성능 최적화 (Osnap 맵 재사용) | 사용자 피드백 / IN_PROGRESS (DiagLog 비교 대기) |
| T-036 | 가공도 시트 카메라 회전 보존 (Z90, R180, EA 등) | 사용자 피드백 / IN_PROGRESS (4차 5단계 후 새 가설 검증) |
| T-037 | 2D 출력 BOM 줄바꿈 + ITEM 열 분리 | 사용자 직접 / TODO (SPREF 예시 대기) |
| T-038 | 2D 출력 셀 크기 기반 모델 스케일 + 여백 예산 | 사용자 직접 / TODO (예산 수용 대기) |
| T-041 | 치수 Leader line PoC | T-040 후속 / TODO |
| T-060 | 보조선 시작점이 모델과 겹쳐 보이는 케이스 회피 | 사용자 본인 발견 / TODO (재현 케이스 대기) |

### 즉시 진행 가능 (외부 입력 불필요, 사용자 컨펌 없이 가능)
- T-058 (sdk-verifier 후 단순 구현) — 단, T-039 선행 권장
- T-006 2차 — SDK 조사 단계부터 시작 가능
- T-038 — sdk-verifier로 GridStructure API 조사 시작 가능

-->

### T-043 — Base Template 생성 기준 + 제작도 배치 기준 분석·문서화
- **생성일**: 2026-04-28
- **상태**: TODO (사용자 답변 대기)
- **회사 매핑**: 확인 중 / 긴급상 1+2
- **관련**: T-038 후속 분석 산출물. 사용자 직접 지시
- **회사 원문 (요약)**:
  > Base Template 생성기준(확인중) — 어떻게 2D View가 생성되고 템플릿이 만들어지는지?
  > Base Template 내의 제작도 배치 기준 — ISO/X/Y/Z뷰들이 어떤 방식으로 크기·위치 배치되는지 + BOM Table, 도면정보 테이블
- **사용자 확인 필요**:
  - [ ] 산출물 형식 — (a) 개발자 doc(기능/), (b) 다이어그램+짧은 설명, (c) 사용자 매뉴얼 보강 중 선택
- **세부 (스케치)**:
  - [ ] `GenerateSheetDrawing2D` 단계별 흐름 mermaid 다이어그램화
  - [ ] 그리드 셀 구조 (2×3, 4뷰 + BOM + 도면정보) 시각화
  - [ ] 모델/표/라벨 위치 결정 알고리즘 본문 정리
- **영향 파일**: docs/기능/도면시트/시트 2D 렌더.md (보강) 또는 신규 docs/architecture/2d-template.md

### T-045 — BOM Size 표기법 (단면 SIZE + 길이 LENGTH 모두 표현)
- **생성일**: 2026-04-28
- **상태**: TODO (사용자 답변 대기)
- **회사 매핑**: 확인 중 / 긴급상 9
- **관련**: T-037 (BOM 줄바꿈)와 같은 영역
- **회사 원문**:
  > BOM의 Size 표기법 수정 필요 (SIZE와 길이가 모두 표현되어야 함)
- **사용자 확인 필요**:
  - [ ] 결합 형식 — 한 컬럼에 합치기(`H300x150 L=2000`) vs 두 컬럼 분리(`SIZE` + `LENGTH`)
  - [ ] 길이 단위 (`mm` 표기 필요? 정수만? 소수 1자리?)
  - [ ] 길이 데이터 출처 — UDA 별도 키인지 / 부재 BBox 최장축인지
- **세부**: 사용자 답변 후 격상
- **영향 파일**: A2Z/Form1.Clash.cs (CollectBOMInfo SPREF 파싱), A2Z/Form1.DrawingSheets.cs (BOM 테이블 컬럼)

### T-051 — Osnap 생성 기준 문서화 (Softhills 답변 정리)
- **생성일**: 2026-04-28
- **상태**: TODO (자료 수집 단계)
- **회사 매핑**: 확인 중 / 긴급하 2
- **관련**: 사용자 직접 지시
- **회사 원문**:
  > Osnap 생성 기준 확인 필요 — Softhills의 답변 확인함
- **사용자 확인 필요**:
  - [ ] Softhills(SDK 벤더) 답변 원문 공유 — 그 내용 그대로 docs/기술 노트/osnap-generation.md 신규 파일에 정리
- **세부**:
  - [ ] 답변 받으면 한국어로 정리 + Osnap 종류(LINE/CIRCLE/POINT) 별 생성 조건 명기
  - [ ] 우리 코드의 `vizcore3d.Object3D.GetOsnapPoint(idx)` 사용처와 cross-ref
- **영향 파일**: docs/기술 노트/osnap-generation.md (신규)

### T-054 — 풍선·심볼 반영 기준 정의 (도메인 정의)
- **생성일**: 2026-04-28
- **상태**: TODO (정의 책임자 확인 필요)
- **회사 매핑**: 수정 후 확인 필요 1
- **관련**: 사용자 직접 지시
- **회사 원문 (정의 초안)**:
  > 풍선 : 모델에서부터 뻗어나온 선으로 이어진 텍스트 박스
  > 심볼 : 모델의 특정 위치에 특이사항을 표현하기 위한 심볼
- **사용자 확인 필요**:
  - [ ] 정의 책임자 — 담당자 / 우리 / 양쪽 협의?
  - [ ] 코드에 적용할 경계 — 어디서 풍선 쓰고 어디서 심볼 쓰는지 매핑 필요한지?
- **세부 (정의 확정 후)**:
  - [ ] 용어집(`docs/_glossary.md`)에 두 정의 등록
  - [ ] 기존 코드에서 혼용된 부분 검토 (`Note.Add*` vs `ShapeDrawing.Add*`)
- **영향 파일**: docs/_glossary.md, 코드 검토 후 추가

### T-037 — 2D 출력 BOM 테이블 줄바꿈 방지 + ITEM 열 분리 기준 확장
- **생성일**: 2026-04-24
- **착수일**: 2026-05-10
- **상태**: IN_PROGRESS (2차 — 한 번 고정 폭 + 폰트 축소 시도, 사용자 실기 검증 대기)
- **관련**: — (사용자 직접 지시, T-006/FB-003 심화)
- **배경**: 2D 출력 시 BOM 셀에 긴 텍스트가 들어가면 `IsTextWrapped=true`로 wrap되면서 행 높이가 늘어나 14행 레이아웃이 깨짐. ITEM 열 값은 UDA `SPREF`에서 "/" 제거 후 ":" split로 추출 — 사용자 요구로 추가 split 기준(`-` / `/` 등) 포함 필요
- **사용자 방침** (2026-05-11 확정): **테이블 열 너비는 한 번 정해서 고정** (콘텐츠 변동 따라 매번 바꾸는 거 지양). 폭 미세조정 + 폰트 전체 축소 조합 OK
- **사용자 확인 필요**:
  - [ ] **실제 SPREF 값 예시 2~3건 공유** (UDA 원본과 원하는 ITEM 결과 표기)
  - [ ] split 우선순위 확정 (`:` → `-` → `/` 순서? 가장 짧은 유효 토큰 택일?)
- **세부**:
  - [x] sdk-verifier (2026-05-10): `TemplateTableData.FontSize`/`AutoFit`/`CellFontHeight` **모두 부재**. `Set2DViewCreateObjectItemTextHeight(float)`는 일반 2D 객체 텍스트용으로 명시 — Template/Table 적용 보장 X (실기 시도로만 최종 확인 가능)
  - [ ] 옵션 A: `IsTextWrapped=false` + 셀 폭 초과분 "..." 말줄임 — 미채택 (정보 손실 위험)
  - [x] 옵션 B 변형 (2026-05-11): `Set2DViewCreateObjectItemTextHeight(4f)`로 BOM 렌더 직전 폰트 축소 시도 — **빌드 결과로 SDK 적용 가부 최종 판정 예정**
  - [ ] 옵션 C: ITEM 추가 split 구현 (사용자 답변 후 확정)
  - [x] 열 너비 1차 재분배 (2026-05-10, c635978) — 사용자 방침에 따라 revert (97c1cba)
  - [x] 열 너비 2차 고정 (2026-05-11): No 5, ITEM 20, MATERIAL 12, SIZE 14, Q'TY 7, T/W 8, MA 5, FA 6 — 합 77mm 유지, **콘텐츠 맞춤 X 한 번 고정**
  - [ ] docs/기능/도면시트/시트 2D 렌더.md 갱신
- **잔여 옵션 (폰트 축소 안 먹을 경우)**:
  - [ ] 헤더 약자화 ("MATERIAL"→"MAT", "Q'TY"→"Q", "T/W"→"TW") — 도면 표준 허용 여부 사용자 결정 필요
  - [ ] Drawing2D 원시 API로 셀 자체 그리기 (별도 큰 작업)
- **영향 파일**:
  - `A2Z/Form1.Clash.cs` (CollectBOMInfo — SPREF 파싱)
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D BOM 테이블 블록 L1218~1318)

### T-038 — 2D 출력 셀 크기 기반 모델 스케일 + 여백 예산
- **생성일**: 2026-04-24
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (step B 완료 — `targetH = 0f` 적용. step C 대기: 동적 마진)
- **사용자 사양 (2026-05-12)**: 모델 셀 가득 + 보조선 영역 확보 (단계별 — B 모델부터, C 동적 마진)
- **step B (2026-05-12)**: `targetH = 40f → 0f` (Form1.DrawingSheets.cs:1372). `FitObjectToGridCellAspect`만 사용. 결과: 셀 100% 가득이지만 잘림
- **step B-2 (2026-05-12)**: 사용자 사양 "15프로 줄여보자". `targetH = 0f` 분기에 `else { RescaleObject(*, scale * 0.85f) }` 추가 (L1704, L1879). 결과: 85% 차지, 15% 안전 마진
- **step C 계획 (필요 시)**: 셀 가용 높이 = cellH - 라벨박스H(약 10~15mm) - 풍선 영역(약 10~12mm) - 보조선 영역(보조선 max 길이 + 텍스트 마진). 동적 targetH 계산
- **관련**: — (사용자 직접 지시, T-006 2차 실험 흡수)
- **배경**: 현재 `targetH=40f` 하드코드. 셀 높이 ≈ 95mm이므로 58% 여유 공간 낭비. 모델을 키우고 싶지만 그리드 이탈·풍선/라벨/치수선 겹침 위험
- **제안 여백 예산** (사용자 승인 필요):
  - 셀 95×92mm 기준: 상단 라벨 8mm + 풍선 영역 12mm + 하단 치수 15mm + 모델 60mm
  - 좌우: 치수 영역 10×2mm + 모델 72mm
- **사용자 확인 필요**:
  - [ ] 위 예산 수용 여부 (모델 60×72mm 영역 OK인지)
  - [ ] 뷰별 스케일 통일(모든 뷰 동일 비율) vs. 뷰별 개별 최대화 선호
- **세부**:
  - [ ] sdk-verifier: `GridStructure.GetGridCellSize(row,col)` / `GetCellBounds` 류 API 존재 확인
  - [ ] `RenderSheetViewForDrawing`의 `targetHeight` 파라미터를 예산 기반 동적 계산으로 교체
  - [ ] Sheet 2+ ISO의 `bgObjId`·`objId` 공통 스케일 유지 (현재 따로 놀 위험)
  - [ ] 그리드 이탈 감지 — `GetObjectBounds(id)` 호출 후 셀 영역과 비교
  - [ ] docs 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D L1298~1311, RenderSheetViewForDrawing L1430~)
- **선행**: sdk-verifier 조사 먼저
- **연관**: T-039(치수 offset 동기화)는 이 작업 완료 후 진행

### T-039 — 치수 생성 타이밍 재설계 + offset 고정 (2D 공간 기준)
- **생성일**: 2026-04-24
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (T-038과 결합 — 일반 시트 보조선 50/100mm 고정 PoC 1차, 가공도 별도)
- **사용자 사양 v1 (2026-05-12 초)**: 1단=50mm / 2단=100mm 고정 (캔버스 절대). 기준=보조선 끝점. 텍스트 마진 보정 X
- **사용자 사양 v2 (2026-05-12)**: 각 뷰의 치수 max 기준 동적 분기
  - max > 1000mm → 보조선 1단=10mm / 2단=20mm (캔버스 절대)
  - max ≤ 1000mm → 보조선 1단=20mm / 2단=40mm (≤500 포함)
  - 큰 치수일수록 보조선 짧게 (시각 균형)
- **구현 핵심 (v2)**: `ShowAllDimensions` 내부에서 `filteredDims.Max(d => d.Distance)` 계산 후 분기. `ShowAllDimensions` 시그니처 단순화 — 두 override(`baseOffsetOverride`, `levelSpacingOverride`) 제거, `canvasScaleOverride` 하나로 통합. 호출자는 scale 추정만 전달, 분기 로직은 내부 책임.
- **세부 (v2)**:
  - [x] `ShowAllDimensions` 시그니처 단순화 — `canvasScaleOverride = -1f` (Form1.Dimensions.cs:378)
  - [x] 내부 분기 — `maxDist > 1000` 기준 canvasBase/canvasLvl 결정 후 `/ scale`로 모델좌표 변환 (Form1.Dimensions.cs:497~)
  - [x] `EstimateFitScaleForCell` 헬퍼 그대로 사용 (Form1.DrawingSheets.cs:1498)
  - [x] `RenderSheetViewForDrawing` L1603 호출 — `estScale`만 전달 (분기 로직 호출자 제거)
  - [ ] **빌드 통과 후 사용자 사내 PC 실기 — 큰 치수 시트(>1000) 보조선 10/20mm, 작은 시트(≤1000) 20/40mm 도달 확인. DiagLog `T-038+039 v2 maxDist=N` 값 비교**
- **잔여 (2차 — 가공도 적용)** → 별도 계획서로 분리 진행: `docs/리팩토링/가공도-보조선-제작도통일.md` v2 (2026-06-03, Codex 1차 반영):
  - [x] 공용 헬퍼 `ComputeCanvasAbsoluteOffsets` 추출 + 제작도 교체 (동작 보존, `1aba8c7`)
  - [x] 가공도 `BuildMfgSceneCore(availW, availH)` + 캔버스 절대 5/10mm 분기 (`EstimateFitScaleForViewArea` fitFactor=1.0 추정). 빌드 통과
  - [ ] **사내 검증 — 가공도 보조선 부재 크기 무관 일정 + 모델 정합. 회전(Z90) 부재 추정 오차 확인. 부족 시 실측 newScale 2차**
  - [ ] EA 두 뷰·MULTI·`:1693`(FitObjectToGridCellAspect) 경로는 범위 외 (별도)
- **잔여 (3차 — 정확도 향상)**:
  - [ ] 사전 추정 vs 실제 RescaleObject scale 차이 측정 → 오차 분석
  - [ ] 큰 경우 2단계 렌더 (모델 먼저 → 실제 scale → 치수) 재설계
- **영향 파일**: A2Z/Form1.Dimensions.cs (시그니처+변수), A2Z/Form1.DrawingSheets.cs (헬퍼+호출)
- **선행**: T-038 (셀 크기 기반 모델 스케일)과 결합으로 진행 중

### T-039 (구) — 치수 생성 타이밍 재설계 + offset 고정 (2D 공간 기준)
- **생성일**: 2026-04-24
- **상태**: 격상됨 (위 항목 참조)
- **관련**: — (사용자 직접 지시, T-038 후속)
- **배경**: 모델 `RescaleObject` 시 치수선도 같이 확대되어 offset이 `ratio`배로 폭주하고 텍스트가 과도하게 작아짐. 치수선이 셀 경계를 벗어나 인접 셀 침범 (T-006 FB 중 "치수선 셀 이탈"과 동일)
- **근본 원인**: `ShowAllDimensions`로 **3D Measure 생성 → 2D 변환 → 모델과 함께 스케일** 순서라 치수가 모델 크기의 함수로 움직임
- **해결 방향**:
  - [ ] 치수 생성 순서를 **모델 스케일 확정 이후**로 지연
  - [ ] 3D 좌표 → 2D 투영 후 **2D 공간에서 고정 offset(mm 단위)** 재배치
  - [ ] 치수 텍스트 높이를 셀 크기 비례 동적값(`cellH × 0.05` 등)으로 — 현재 `Set2DViewCreateObjectItemMeasureTextHeight(5f)` 하드코드
  - [ ] 보조선(extension line) 길이도 절대값(mm)으로 고정
- **사용자 확인 필요**:
  - [ ] 실기에서 offset 폭주·텍스트 과소화가 보이는 구체 시트 스크린샷 1~2건 공유 (재현 케이스 확정용)
- **세부**:
  - [ ] sdk-verifier: `Set2DViewCreateObjectItemMeasureTextPosition` / `Object2D.SetObjectScaleLocked` 류 API
  - [ ] 치수 ID 수집 후 모델과 **다른 스케일** 적용 가능한지 SDK 확인
  - [ ] PoC: 단일 시트로 고정 offset 방식 검증
  - [ ] docs 갱신 (단계표 재작성)
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (RenderSheetViewForDrawing L1560~1600 스케일 블록)
  - `A2Z/Form1.Dimensions.cs` (ShowAllDimensions 2D 경로)
- **선행**: T-038 필수

### T-040 — 치수 텍스트 ↔ 치수선 겹침 감지·회피 (가시성)
- **생성일**: 2026-04-24
- **착수일**: 2026-05-11
- **상태**: IN_PROGRESS (1차 — Level 1 offset i%2 토글 적용, 사용자 실기 검증 대기)
- **관련**: — (사용자 직접 지시)
- **배경**: 치수 숫자와 치수선/보조선이 겹쳐 숫자가 안 보이는 가시성 문제. "어떻게 감지하고 어떻게 회피할지 고민 필요" — 사용자 지시
- **감지 전략** (2D 공간 기준):
  - 각 치수 텍스트의 bounding box (중앙점 + 폰트 높이 × 예상 문자 폭)
  - 같은 뷰의 다른 치수선 segment들과 AABB ↔ 선분 충돌 테스트
  - 보조선·모델 라인도 충돌 대상 포함 여부 결정 필요
- **회피 전략 3단**:
  | Tier | 방법 | 구현 비용 | 근본 해결 |
  |---|---|---|---|
  | T1 | 치수 텍스트 뒤 **흰색 배경 마스크** | 낮음 | X (시각만) |
  | T2 | 평행 치수 **층별 오프셋** (동일 축 N번째 = +N×8mm) | 중간 | 부분 |
  | T3 | **Leader line + 자유 배치** (겹치면 텍스트만 측면으로 빼고 지시선 연결) | 높음 | O |
- **사용자 확인 필요**:
  - [ ] 우선순위 T1만 먼저 → T2 추가 → T3는 PoC 후 판단 수용 여부
  - [ ] 실기 겹침 사례 스크린샷 2~3건 (패턴 분석용)
- **세부**:
  - [x] sdk-verifier (2026-05-10): `Set2DMeasureTextBackground` 등 텍스트 배경색·마스크 API **부재**. T1 흰 마스크 SDK 직접 지원 X 확정
  - [ ] T1 구현 — SDK 미지원으로 폐기 (자체 흰 사각형 그리기 옵션 별도 검토 가능)
  - [ ] 겹침 감지 유틸 신설 (Form1.Dimensions.cs) — `ApplySmartFiltering`이 이미 텍스트 간격 검사로 부분 구현. 2026-05-11 진단 DiagLog 추가 (axis별 level0/level1 분포 검증용)
  - [x] **T2 변형 (2026-05-11)**: 사용자 요청 — `level1Offset` i%2 토글. 짝수 i=100mm, 홀수 i=50mm. 같은 축 내 측정축 좌표 순 정렬 후 인접 쌍 두 라인 분산
  - [ ] **T2 변형 취소 (2026-05-11)**: 사용자 결정 *"수치는 2줄만 생성 — 부재간 연쇄치수 + 전체치수"*. 토글 폐기, Level 1 foreach 원복. level2 적응형(`ApplySmartFiltering` 충돌 회피)은 유지. 별도 결정 시 level2도 폐기 가능
  - [x] **텍스트 위치 13mm 임계 (2026-05-11)**: 사용자 결정 *"치수 ≤13mm면 바깥, >13mm면 기본 위치 1, 기준 통일"*. `AlignDistanceTextPosition` 글로벌 옵션을 측정 추가 직전에 dim별 토글. `btnDimensionShowSelected_Click` foreach + `ShowAllDimensions` Level 1/2/0 세 그룹 모두 적용
  - [x] **AlignDistanceTextPosition 토글 폐기 (2026-05-13)**: 실기에서 토글이 작동 안 함을 사용자 보고. Softhills 담당자 예제 기반 `Drawing2D.Measure.SetMeasureItemDistanceTextPos(int, Vector3D)`로 전환. ≤13mm 측정 텍스트를 화면 오른쪽 캔버스 30mm 시프트 (모델 mm 환산 = 30/GetObjectScale). 일반 시트 + 가공도 메인 2경로. ISO 뷰·EA·MULTI 제외. 거리는 `MeasureItem.Position` MAIN 두 좌표로 추정 (옵션 A — `MeasureItem.Distance` 속성 부재). 빌드 통과로 SDK 메서드 실재 확정 (XML 미문서)
  - [x] **v2 (2026-05-13)**: v1 실기 보고 — 가로 보조선 10mm 미시프트(시프트 방향이 항상 H라 따라감). 치수축별 시프트 분기(H면 up / V면 right) + 뷰 max≤100mm skip + 거리 30→3mm
  - [x] **v3 (2026-05-13)**: v2 사용자 보고 — "반대로 적용". 시프트 방향 분기 스왑(가로→right / 세로→up), 부호 유지
  - [x] **v4 (2026-05-13)**: v3 보고 — (1) Z뷰 세로 치수 up 부호 -Y→+Y 보정 (2) 새 문제 "제작도 보조선이 내부 Osnap에서 시작해 모델 관통". 외곽 Osnap 복귀 알고리즘 도입 — `_osnapPool` 보존 + `ResolveExtensionOrigin` 헬퍼로 P 대신 offset 축 직선상 *치수선 쪽 외곽 Osnap* Q에서 보조선 시작. `axisPositiveOffset` 재사용. 일반 시트만 적용, 가공도는 다음 라운드
  - [x] **v5 (2026-05-13)**: v4 보고 — 외곽 Osnap 복귀가 반대 방향 결과. 부호 반전 시도도 효과 없음. 사용자 결정으로 전체 롤백 + 대안 *모델 라인 굵기 2.0→3.0* (보조선보다 진하게 → 시각 우선순위로 통과 거슬림 완화)
  - [x] **v6 (2026-05-13)**: 보조선 굵기 0.1 통일 (DrawingSheets 0.3→0.1, MfgDrawing 0.5→0.1 두 곳). 모델 vs 보조선 비율 30배. 치수선(MeasureLineWidth)은 그대로
  - [x] **v7 (2026-05-13)**: 직각 시프트 완전 폐기 + 평행 시프트 도입. 임계 maxEstDist/26 (예 1326→51), 시프트 거리 캔버스 3mm 유지. 인접 큰 dim 쪽 측정축 평행 슬라이드. 양쪽 같음→오른쪽, 한쪽만→반대(체인 바깥). ApplyParallelTextShift + FindMeasureByDimCoords 헬퍼 신설. SDK measure 매칭은 옵션 A(측정축 좌표 일치). 일반 시트만 적용(chainDimensionList 사용 경로). BOM bottom 11→10(1단위 아래로)
  - [x] **v8 (2026-05-13)**: v7 실기 — 시프트 미작동 보고 (좌표 매칭 실패 유력). XML로 `AddCustomAxisDistance`가 ID 반환 확인 → 옵션 C 전환. `ChainDimensionData.MeasureId` 필드 신설, `DrawDimension` 시그니처 `void→int`, ShowAllDimensions 3곳에서 dim.MeasureId 저장. ApplyParallelTextShift는 dim.MeasureId 직접 사용 (좌표 매칭 폐기). MfgDrawing의 DrawDimension 호출 9곳은 반환값 무시(컴파일 OK)
  - [x] **v9 (2026-05-13)**: v8 시프트 작동 OK이나 방향이 측정선 직각(90° 회전). SDK가 텍스트 평행 슬라이드 불가능 추정 → 시프트 축을 측정축에서 offset 축으로 교체. 인접 비교는 부호(±)만 결정. 결과: 측정선 직각으로 시프트 (사용자 사양 A)
  - [x] **v10 (2026-05-13)**: v9 보고 — offsetAxis 매핑이 사용자 시각과 반대(가로 치수가 좌·우 시프트). "가로/세로 오프셋 교환" = 측정축(axis) 직접 사용으로 복귀(v8 패턴). switch 인자 offsetAxis → axis
  - [x] **v11 (2026-05-13)**: 사용자 "1aaf85c(v6) 시프트 방법이 제일 잘됐다 — 그때로 복귀". ApplyParallelTextShift 헬퍼 내부 통째 교체 → v6 시점 직각 시프트(13mm 고정, SDK measureItem 직접, 가로→right/세로→up). 인접 비교/chainDimensionList 의존/maxEstDist/26 모두 폐기. 가공도에도 헬퍼 호출 복귀
  - [x] **v12 (2026-05-13)**: v11 베이스 + 임계 maxEstDist/26 + 인접 비교 부호 결정. SDK measureItem을 측정축별 그룹 후 dimCenter 정렬 → 좌·우 인접 estDist 비교로 shiftDir(±1) 결정. 직각 시프트는 v11 매핑 그대로
  - [x] **v13 (2026-07-21)**: 작은 치수 승격 후 2단 텍스트 슬라이드를 제작도·가공도 모두 종이 5mm → 2.5mm로 절반 축소. 단 간격·보조선 위치는 유지
  - [ ] **v12 실기 검증 대기** — 작은 치수가 인접 큰 dim 방향에 맞게 시프트되는지 / 부호 매핑이 사용자 시각과 일치하는지
  - [ ] **잔여**: 가공도 EA 두 번째 뷰(L1905) / 가공도 MULTI 경로 — 카메라 식별 별도
  - [ ] docs 갱신 (실기 검증 후 기능/치수/* + 기능/도면시트/* + 기능/가공도/* 별도 라운드)
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (AddChainDimensionByAxis, 겹침 검사 유틸 신설)
  - `A2Z/Form1.DrawingSheets.cs` (RenderSheetViewForDrawing 치수 후처리)
- **선행**: T-039 완료 후 (치수 배치 기준 확정돼야 겹침 판정 유의미)

### T-041 — 치수 Leader line 방식 PoC (T-040 T3 심화)
- **생성일**: 2026-04-24
- **상태**: TODO
- **관련**: — (T-040 후속, 조선 도면 표준 부합성 평가용)
- **배경**: T1·T2로 해결 안 되는 치밀 밀집 구간을 위한 leader line(지시선) 방식. 텍스트는 여유 공간으로 빼고 치수선과 가는 선으로 연결
- **세부**:
  - [ ] 사용자/담당자에게 leader line 허용 여부 확인 (조선 도면 표준 관례)
  - [ ] 단일 복잡 시트로 PoC
  - [ ] 여유 공간 탐색 알고리즘 (모델 BBox 바깥 clear area 찾기)
  - [ ] 수용 결정 후 프로덕션 적용 or 기각
- **영향 파일**: A2Z/Form1.Dimensions.cs, A2Z/Form1.DrawingSheets.cs
- **선행**: T-040 결과 후 판단

### T-036 — 가공도 시트: 선택상태 해제 + ISO 뷰 느낌 해결
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 완료, 사용자 실기 재확인 대기)
- **관련**: — (사용자 피드백)
- **경로 전개**:
  1. 1차 진입: 선택상태 해제(DESELECT_ALL) + DiagLog 추가 (커밋 `230e45f`)
  2. 1차 해석 시도: "Z 최장축인데 세로로 배치" → L215 `use1803d && longestAxis!="Z"` 가드 추가 (커밋 `537f07c`)
  3. **사용자 재보고 (2026-04-23)**: "45도 대각 ISO 뷰로 보게 된다" → Z 방향이 아닌 **카메라 방향 자체가 ISO로 잔존**하는 증상
  4. **원인 재확정**: [LvDrawingSheet_SelectedIndexChanged](../../A2Z/Form1.DrawingSheets.cs) 공통부 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 글로벌 ISO) 유지한 채 이동 → `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 덮어쓰지 못함
  5. 수정 (커밋 `b0f8802`): 가공도 시트(-3) 분기 앞에서 `FlyToObject3d` 스킵 + L215 180° 스킵 가드 **원복** (수직 뒤집기 의도 복원)
- **세부 (완료)**:
  - [x] `ExecuteMfgDrawing` 진입부 `Object3D.Select(DESELECT_ALL)` 추가
  - [x] 회전 진단 `DiagLog T-036 MfgDrawing bom=... sizeXYZ=... longestAxis=... isPadOrPlate=... viewDir=... use180=... useMinus=... Z90Applied=... R180Applied=...`
  - [x] 1차 해석 L215 가드 → 원복 (ISO 원인 아님)
  - [x] `LvDrawingSheet_SelectedIndexChanged`에서 가공도 분기 시 `FlyToObject3d` 스킵
  - [x] `use1803d` 바깥 스코프 승격 (DiagLog 가시성 — 유지)
  - [x] docs/기능/도면시트/시트 선택.md / 가공도 단일.md 갱신
  - [x] MSBuild Debug 통과
  - [x] **후속 (2026-04-23)**: 사용자 "카메라 재조정 중 가로→세로 깜빡" 관찰 → `ExecuteMfgDrawing` 전체를 `BeginUpdate/EndUpdate`로 감싸 중간 상태 노출 차단 + Z 최장축 90° 회전 직후 누락됐던 `FitToView` 추가
  - [x] **재수정 (2026-04-23)**: 사용자 DiagLog 공유 → "누르는 순간 가로 → 0.5초 뒤 FitToView로 세로" 확정 → **직전 커밋의 FitToView가 바로 원인**. 제거. 원본 주석 경고 "LockZAxis false 유지 — true로 복원하면 렌더링 엔진이 회전을 리셋"이 FitToView에도 동일 적용
  - [x] **3차 수정 (2026-04-23, sdk-verifier 기반)**: 내부 FitToView 제거만으론 세로 복귀 여전. `LockZAxis`는 키보드용이라 무관 확정. SDK 정공법 `GetCameraData()` + `SetCameraData(data, false)` 스냅샷 복원 패턴 도입. Form1.cs에 `_mfgDrawingCameraSnapshot` 필드 추가, ExecuteMfgDrawing Z 90° 직후 `GetCameraData()` 저장, `LvDrawingSheet_SelectedIndexChanged` 말미에 가공도(-3) 확인 후 `SetCameraData(snapshot, false)` 복원
  - [x] **사용자 실기 재보고 (2026-04-24)**: "아직도 세로로 출력되는 부재들이 있음" → 3차 수정으로 일부는 해결됐으나 **여전히 세로 잔존 부재 존재**. 새 가설 필요
  - [ ] 사용자 정보 수집 필요: 어떤 부재가 세로로 남는지 DiagLog (`T-036 MfgDrawing` 라인 + `T-036 카메라 스냅샷 복원` 라인) 비교 — Z 최장축 케이스인지 / non-Z 케이스인지 / 스냅샷이 저장됐는지 / SetCameraData 호출됐는지
  - [ ] **새 가설 후보**:
    1. Z 최장축이 아닌 X·Y 최장축 부재가 카메라 회전이 아예 안 적용된 채 가공도 진입 (스냅샷은 Z 케이스에만 저장됨)
    2. 가공도 시트가 처음 선택될 때만 스냅샷 적용 — 다른 시트 거쳤다 다시 같은 가공도로 돌아오면 스냅샷이 다른 가공도 것으로 덮어써졌을 가능성 (가공도가 여러 개일 때)
    3. SetCameraData(false) 후에도 외부 어딘가가 카메라 재변경
  - [ ] 위 가설 검증 위해 `_mfgDrawingCameraSnapshot`을 **Dictionary<int, CameraData>** (가공도 번호 키)로 확장 검토
- **영향 파일**: A2Z/Form1.MfgDrawing.cs, A2Z/Form1.DrawingSheets.cs, docs/기능/가공도/가공도 단일.md, docs/기능/도면시트/시트 선택.md

### T-005 — 치수 배치를 Osnap 외곽 방향으로
- **생성일**: 2026-04-15
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (구현 완료, 사용자 사내 PC 실기 검증 대기)
- **관련**: FB-002
- **사용자 사양 (2026-05-12)**: 모델 전체 뷰 중앙 기준 4분면 — 중앙에서 가장 먼 Osnap이 있는 방향으로 치수. 상/하·좌/우 각각 max·min 거리 비교로 외곽 판정
- **구현 핵심**: 헬퍼 `ComputePositiveOffsetByOsnapExtreme(values, modelCenter)` 신설. `omax - center` vs `center - omin` 부호 있는 거리 비교 → 큰 쪽이 positive. 기존 `avg >= center` 5곳 전부 교체. 한쪽 쏠림(omin/omax 모두 center 한쪽)도 부호 자동 처리
- **세부**:
  - [x] 헬퍼 `ComputePositiveOffsetByOsnapExtreme` 신설 — Form1.Dimensions.cs GetAxisValue 옆
  - [x] 5곳 적용 — Form1.Dimensions.cs:499(메인, 치수추출+2D 출력 공용) / Form1.MfgDrawing.cs:335(가공도 메인) / :1057(가공도 보조) / :1192(MULTI) / :1707(EA newDims 비길이축, longestAxis 오버라이드 유지)
  - [ ] 빌드 통과 후 사용자 사내 PC에서 실기 — 부재가 모델 중앙 한쪽에 치우친 케이스에서 치수가 *그 반대쪽*(외곽)으로 빠지는지 확인
  - [x] docs/기능/치수/메인 치수 추출.md 갱신 (외곽 판정 알고리즘 섹션)
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (헬퍼 추가 + L499 패턴 교체)
  - `A2Z/Form1.MfgDrawing.cs` (4곳 패턴 교체)

### T-012 — 엑셀 템플릿 하이브리드 실험 (PoC)
- **생성일**: 2026-04-20
- **착수일**: 2026-05-12
- **상태**: IN_PROGRESS (Step 1 코드 빌드 통과, 사용자 사내 PC 실기 검증 대기)
- **관련**: REQ-002
- **배경**: SDK가 `ImportExcel`, `ImportExcelWithData`, `Draw2DViewTemplate(path, x, y, w, h)`, `RenderTemplateOnGridStructure`를 제공 ([VIZCore3D.NET.xml:29219](../../lib/VIZCore3D.NET.xml:29219)). 담당자가 엑셀로 양식을 관리할 수 있는지 **실험만** (프로덕션 전환은 별개). 과거 Phase 18(`790a02a`)에서 BOM 동적 행수 문제로 수동 구성으로 되돌린 이력 있음 — 하이브리드로 재도전
- **사용자 결정 (2026-05-12)**: 옵션 A — 기존 `GenerateSheetDrawing2D` 유지 + 별도 partial class `Form1.ExcelTemplate.cs`에 PoC 핸들러 신설. 새 디버그 버튼 "엑셀 PoC" 추가. Step 1 시각 검증 후 단계별 진행.
- **사전 자료**: `사용자템플릿_엑셀_Rev_01.xlsx` (사용자 작성) — A4 가로 비율(W/H ≈ 1.41), 55컬럼 × 40행, 4뷰(ISO/Z/X/Y) + BOM + NOTE + 도면정보 + TAG NO + 이미지 슬롯 4개
- **세부**:
  - [x] **Step 1 (2026-05-12)**: `btnExcelTemplatePoC` 핸들러 + `vizcore3d.Drawing2D.Template.ImportExcel(path)` 단독 호출. 빌드 통과. 사용자 사내 PC 시각 검증 대기
  - [x] **확정**: `Drawing2DTemplateManager.templateDatas`는 private/internal 필드 외부 접근 불가 (빌드 시 확정)
  - [ ] **Step 2**: 셀 좌표 수집 — `ParseJson` 등 다른 public API 탐색. placeholder(`{Image}`, `ISO`, `LOOKING "X/Y/Z"`, `BILL OF MATERIAL`) → Row/Column 매핑
  - [ ] **Step 3**: 셀 영역에 `AddModel(viewIndex)` + 이미지/BOM/풍선/치수 배치
  - [ ] 결과 리포트: `docs/기술 노트/excel-template-experiment.md` 신설
  - [ ] T-057(검토자 Excel 일치 검증)과 통합 — Rev_01이 검토자 Excel과 같은 양식인지 확인
- **영향 파일**: A2Z/Form1.ExcelTemplate.cs (신규), A2Z/Form1.Designer.cs(+버튼 1개, groupBox1 너비 +87px), A2Z/A2Z.csproj(+Compile Include), 사용자템플릿_엑셀_Rev_01.xlsx (신규)
- **관련 docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) (Step 1 흐름)

### T-057 — 검토자 Excel Template과 Base Template 일치 검증
- **생성일**: 2026-05-04
- **상태**: TODO (검토자 Excel 파일 수신 대기)
- **회사 매핑**: 개발 요청 — 상 3
- **관련**: T-043 (Base Template 정리)와 짝, T-012 (엑셀 PoC)와 시너지
- **회사 원문**:
  > Base Template의 생성 기준이 검토자가 전해준 Template(엑셀로 되어있음)이랑 맞는지?
- **사용자 확인 필요**:
  - [ ] 검토자 Excel 파일 수신·공유
- **세부 (Excel 수신 후)**:
  - [ ] 셀 구조·표 헤더·치수·BOM 컬럼을 우리 Base Template과 비교
  - [ ] 차이점 표 작성 → 표준 결정 (회사 vs 우리)
  - [ ] T-012 PoC와 연계 — Excel을 정답지로 활용 가능
- **산출물**: `docs/기술 노트/excel-template-validation.md` (신규)
- **영향 파일**: A2Z/Form1.DrawingSheets.cs (차이 발견 시 GenerateSheetDrawing2D 수정)
- **선행**: T-043 동시 진행 시 효율 ↑


### T-060 — 보조선 시작점이 모델과 겹쳐 보이는 케이스 회피 (본인 개선)
- **생성일**: 2026-05-04
- **상태**: TODO (사용자 본인 발견 개선사항, 재현 케이스 수집 후 진행)
- **출처**: 사용자 본인 발견 (T-046 확장 후속)
- **배경**: T-046 확장에서 보조선 시작점을 Osnap에서 10mm 띄워 시작하도록 구현. 그러나 떨어진 시작점이 **다른 모델 표면 또는 같은 부재 다른 면과 우연히 겹치면 시각적으로 모델에서 이어진 선처럼 보임** — 사용자가 도면 검토 시 발견한 시각적 혼동
- **우려 시나리오**:
  - 치수선이 모델 안쪽에 배치되는 케이스 (역 offset 방향)
  - gap 방향(`originalStart → startVertex`)이 다른 부재 표면을 가로지르는 케이스
  - 모델이 복잡 형상이라 단위 벡터가 정확히 모델 외부를 향하지 않는 케이스
- **사용자 확인 필요**:
  - [ ] 재현 케이스 1~2건 (스크린샷 + 부재 Index)
- **세부**:
  - [ ] 재현 케이스 분석 — DiagLog로 `originalStart` / `startVertex` / `gappedStart` 좌표 + 모델 BBox 비교
  - [ ] 해결 방향 후보:
    - (a) gap 방향 양방향 분기 (positiveOffset 플래그 활용해 외향 강제)
    - (b) 치수선까지의 거리에 따라 gap 비율 조정 (절대 10mm 대신 상대값)
    - (c) gappedStart가 모델 BBox 내부 들어가는지 점검 후 회피
- **영향 파일**: A2Z/Form1.Dimensions.cs (`OffsetTowardLineEnd` 헬퍼 또는 호출처)
- **관련**: T-046 확장 (DONE)

### T-059 — XYZ축 치수 평면 필터링 검증·수정
- **생성일**: 2026-05-04
- **상태**: TODO (재현 케이스 대기)
- **회사 매핑**: 개발 요청 — 상 11
- **회사 원문**:
  > XYZ축 기준 치수가 X축에서 봤을때 YZ평면이 아니라 YX나 ZX 처럼 다른 평면의 길이가 나올 떄가 있는데 검토 필요
- **사용자 확인 필요**:
  - [ ] 재현 부재 Index 1~2개 + 잘못 나오는 뷰 스크린샷
  - [ ] 어느 축 뷰에서 어느 평면 길이가 섞이는지
- **세부**:
  - [ ] DiagLog로 각 뷰의 치수 ViewDirection·Axis 추적 (T-028 인프라 활용)
  - [ ] X뷰에 ViewDirection="X" 치수 중 Axis가 잘못된 평면 컴포넌트 섞이는지 검증
  - [ ] `FilterOsnapForDimAxis` / `ComputeViewDimensionsForMembers` 축 필터링 로직 점검
  - [ ] 차이 있으면 코드 수정 + DiagLog 보강
- **영향 파일**:
  - A2Z/Form1.Dimensions.cs (FilterOsnapForDimAxis L2085~, ComputeViewDimensionsForMembers L1949~)
  - A2Z/Models.cs (ChainDimensionData.ViewDirection 검증)
- **선행**: T-028 DONE이라 코드 위치 명확

