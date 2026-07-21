# 작업 목록 (TASKS)

실행 가능한 단위로 분해된 개발 작업입니다. 섹션별 상태 관리.

> **원칙**: 한 작업 = 한 커밋 단위 권장. 너무 크면 분할. 세부는 `/commit` 커맨드가 자동 관리.

---

## TODO

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

---

## IN_PROGRESS

### T-013 — ISO 뷰 점선·실선 분리와 3D 위치 정합
- **생성일**: 2026-04-20
- **착수일**: 2026-04-21
- **재개일**: 2026-07-21
- **상태**: IN_PROGRESS (연결 어셈블리 이름 표시까지 구현·컴파일 완료, 사내 PDF 실기 검증 대기)
- **관련**: 사용자 피드백, GitHub issue #7
- **배경**: 조립도는 전체 구조 중 기준부재만 실선, 제작도는 시트 부재 실선과 붙어 있는 시트 밖 주변 부재를 점선으로 표시해야 한다. 옛 수동 정합 방식은 캡처 원점·좌표계 불일치로 실패했다.
- **구현**:
  - [x] `Match2DObjectsTo3DObjectPosition(실선, 점선)`으로 옛 WorldToScreen 수동 정합 교체
  - [x] 조립도: 전체−기준부재 LONG_DASHED 점선 + 기준부재 실선
  - [x] 제작도: 전체 Body BBox·실제 부모 Part 1회 캐시 → 3mm 근접 후보 선별 → 후보만 전용 그룹 간섭검사
  - [x] 제작도: 연결 결과를 내부 연결성 `clashList`와 분리하고 `lvClash`에 `[연결]` 목록 표시
  - [x] 제작도: Crop 대상 2D 객체에 시트 부재+연결 부재를 함께 넣고 시트 부재 노드 기준으로 긴 연결 부재 절단
  - [x] 제작도: 이웃 캡처 → CropFit → LONG_DASHED → 점선 fit → 실선 캡처 → Match 순서 적용
  - [x] 제작도: Clash HotPoint XYZ를 보존하고 연결 Part의 가장 가까운 부모 Assembly 이름을 `Add2DNoteFromWorldCoordinate`로 접촉점에 표시
  - [x] 조립도: 실기 정상인 기존 캡처·배치 순서 유지(제작도 전용 순서 변경에서 제외)
  - [x] C# Compile 오류 0건 (기존 경고 7건)
- **사용자 확인 필요**:
  - [ ] 제작도 ISO에 붙어 있는 주변 부재가 점선으로 표시되는지
  - [ ] 진단 로그의 BBox 캐시 시간·근접 후보 수·원본 Clash 결과·상대 Part 목록 확인
  - [ ] Crop 범위가 붙은 부위 주변만 남기며 너무 좁거나 넓지 않은지
  - [ ] 연결 어셈블리 이름이 실제 접촉점을 가리키고 중복·겹침 없이 표시되는지
  - [ ] 실선과 점선의 3D 위치가 맞고 PDF에서도 LONG_DASHED로 출력되는지
- **영향 파일**: `A2Z/Form1.Clash.cs`, `A2Z/Form1.BOM.cs`, `A2Z/Form1.Stru.cs`, `A2Z/Form1.DrawingSheets.cs`, 관련 흐름·코드 레퍼런스 문서

### T-048 — 가공도 EA 앵글 모델 T자형 잘못 찍힘 수정
- **생성일**: 2026-04-28
- **착수일**: 2026-06-15
- **상태**: IN_PROGRESS (구현·빌드 완료, 사내 EA 모델 PDF 실기 검증 대기)
- **회사 매핑**: 확인 중 / 긴급중 2
- **관련**: 사용자 직접 지시. T-036 가공도 회전과 별개 이슈
- **회사 원문**:
  > 가공도에서 EA관련 모델은 위 아래로 한 번 붙이게 되는데 그거에 대한 2D View에 잘못 찍히는게 있어서 변경 필요. (잘못 찍힌다는게 XYZ축이 있으면 지금 어떤 방식인지는 모르겠지만 엣지가 판명되고 한쪽에서 찍으면 그대로 카메라를 위로 올리던가, 부재를 아래로 돌리던가 해서 한 번 더 찍고 붙일텐데, 몇몇 부재는 X뷰에서 찍고 Z축인 위에서 찍을 때 Y에서 Z방향으로 회전을 한 번 더해버려서 가로로 길게 위아래로 찍혀야 할 모델이 T자 모양으로 찍힘)
- **구현**:
  - [x] `IsAngleFromSpref` 기반 EA 카메라 열린 방향 보정 재활성화
  - [x] 엑셀 템플릿 View 영역을 위·아래 두 뷰로 분할
  - [x] 첫 번째 뷰에서 최장축 치수를 제외하고 두 번째 뷰로 분리
  - [x] 두 번째 뷰를 독립 `Z_MINUS` 또는 `X_MINUS + Z90` 카메라로 생성
  - [x] 과거 T자형 원인의 추가 정렬 회전 제외
  - [x] 두 번째 뷰 실패 객체 정리와 첫 번째 뷰 유지
  - [x] Debug 빌드 오류 0건
- **사용자 확인 필요**:
  - [ ] EA 부재 여러 방향에서 PDF 상하 뷰가 모두 가로 정렬되는지 확인
  - [ ] 길이축 체인/전체 치수가 두 번째 뷰에만 표시되는지 확인
- **영향 파일**:
  - `A2Z/Form1.MfgDrawing.cs`
  - `A2Z/Models/MfgViewPose.cs`
  - `docs/기능/가공도/가공도 시트.md`
  - `docs/기능/가공도/가공도 단일.md`

### T-064 — STRU 일괄 도면 출력 (P1 STRU 목록·강조 완료)
- **생성일**: 2026-05-13
- **착수일**: 2026-05-13
- **상태**: IN_PROGRESS (P1 구현 완료, 사용자 사내 PC 실기 검증 대기)
- **관련**: 사용자 직접 — STRU 단위 다중 선택 → 4종 도면(제작/조립/설치/가공) PDF 일괄 출력 흐름 도입
- **배경**: HYI-STRU 브랜치(2f024d1 외 4커밋)에서 STRU 흐름 시도했으나 간섭검사 폐기(1d17cc6) 범위가 과도해 HYI로 회귀. "리스트 뽑기까지만" 출발로 토론 → 전체 계획 합의
- **계획 합의 사항**:
  - 4종 도면 매핑: 제작도=Sheet1(-1) / **조립도=Sheet 2~N(≥0, 1-hop Clash 이웃)** / 설치도=Sheet(-2) / 가공도=Sheet(-3)
  - 버튼 구성: 2버튼 — `[도면 리스트 뽑기]`(간섭검사+시트목록 미리보기) / `[STRU 도면 자동 생성]`(즉시 4종 PDF)
  - 간섭검사 격리: **방법 A — 가시성 토글** (STRU별 Visible 토글 후 DetectClash 호출. SDK가 부재 단위 인자 미지원 — 사용자 통찰 "전체 1회도 어차피 오래걸려"로 N회 호출 감수)
  - HYI-STRU 자산: 구조만 참조 + `6bc89cf` NodePath fallback 패턴만 cherry-pick
- **Phase 분할**:
  - [x] **P1** — STRU 목록 표시 + 행 선택 시 3D 강조+카메라 fit (이번 커밋)
  - [ ] **P2** — `[도면 리스트 뽑기]` 버튼 (체크된 STRU별 가시성 토글 → DetectClash → GenerateDrawingSheets → lvDrawingSheet 누적, STRU 컬럼 추가)
  - [ ] **P3** — `[STRU 도면 자동 생성]` 단일 STRU 4종 PDF (파일명 `{STRU}_{종류}_{HHmmss}.pdf`)
  - [ ] **P4** — 다중 STRU 배치 + 진행률 + 실패 정책 + 메모리 강화
- **P1 구현**:
  - 신규 `A2Z/Form1.Stru.cs` — `CollectStruList` / `RuleByFrameworkChildParent` / `PopulateStruCheckList` / `btnSelectAllStru_Click` / `ClbStruList_SelectedIndexChanged` (3D 강조+fit)
  - **STRU 식별 룰 재설계 (사용자 모델트리 분석 기반)**: STRU = 자식 중 NodeName이 "FRMWORK " 시작 어셈블리가 있는 어셈블리. 모수를 `LEAF_ASSEMBLY` → `ASSEMBLY`로 확대 + `RuleByFrameworkChildParent`에서 `ParentIndex`로 1단계 부모 추출. 향후 룰 추가 가능한 union HashSet 구조.
  - **재귀 강조 명시화**: `GetChildObject3d(idx, NodeFilterKind.BODY)` → `GetChildObject3d(idx, Object3DChildOption.ALL_CHILDREN, true)` 후 `Where(b => b.Kind == NodeKind.BODY)` 필터. SDK xml line 4877/4583 검증.
  - SDK 정정 적용: `Object3D.Color.RestoreColorAll` (View.Color 아님), `View.FlyToObject3d` (View.Camera 아님). sdk-verifier 사전 검증.
  - `BeginUpdate/EndUpdate`는 try/finally로 감싸 예외 시에도 UI 잠금 해제 보장
  - CheckedListBox 의미 분리: 선택(`SelectedIndex`)=강조용 / 체크(`CheckedItems`)=출력 대상용. `CheckOnClick=true`로 클릭 1회에 동시 트리거
  - Designer.cs: `groupBoxStru` (Dock=Top 240px) + clbStruList + 라벨 + 전체선택 버튼만 (P2/P3 버튼 미포함)
  - BOM.cs: `BuildBodyToPartNameMap()` 직후 `PopulateStruCheckList()` 호출 1줄
- **위험 관리**:
  - 가시성 토글 try/finally 복원 (P2에서 도입 필요)
  - 묶음 GC (P3에서 도입)
  - 가공도 vs 제작/조립/설치 함수 시그니처 차이 — P2 진입 전 SDK 매핑 점검
- **P2 본진 진행 (2026-05-14)**:
  - [x] 가시성 격리·CollectBOMData·DetectClash·시트 자동 생성 흐름 (직전 커밋들)
  - [x] 가공도 P3 분기 제거 → 옛 GridStructure 8×3 흐름 유지 (`a2427c4`)
  - [x] **엑셀 분기에 치수 그리기 이식** (이번 커밋) — `GenerateSheetDrawing2D_WithExcelTemplate` 루프 본문을 옛 `RenderSheetViewForDrawing` 패턴으로 교체. ISO 풍선 + X/Y/Z `ShowAllDimensions` + `Add2DObjectFromShapeDrawing`/`Add2DMeasureFrom3DMeasure`. 모델 shrink Z=0.65/X·Y·ISO=0.70 (사용자 사양). 새 헬퍼 `EstimateFitScaleForViewArea` 추가.
  - [ ] 사용자 사내 PC 실기 검증 — 일반 시트 PDF에 치수+풍선 표시, 모델 영역 적정 (라벨·보조선 침범 없음) 확인
- **다음 단계**: 사용자 사내 PC에서 모델 열기 → 좌측 STRU 패널 표시 → 행 클릭 시 3D 빨강+fit 확인 → 도면 리스트 뽑기 → 일반 시트 치수 확인

### T-032 — 치수 계산 성능 최적화 (Osnap 맵 재사용)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (A 옵션 구현 완료, 사용자 실기 확인 대기)
- **관련**: — (사용자 피드백 "치수 계산 중 창이 오래 떠있음")
- **원인 확정**: `CompleteMainDimensionPostClash`에서 Osnap 수집이 이중 호출
  1. `CollectAllOsnap()` — 전체 visible 부재 `GetOsnapPoint(idx)`
  2. `ComputeViewDimensionsForMembers` 내부 `nodeOsnapMap` 구축 시 **다시** `GetOsnapPoint(idx)`
  - 같은 SDK 왕복을 부재 수만큼 반복 → 전체 시간의 절반 가까이가 이 중복
- **선택한 방식**: **옵션 A** — `CollectAllOsnap`이 수집하는 동안 `nodeOsnapMap`도 같이 구축, `ComputeViewDimensionsForMembers`가 재사용
- **구현**:
  - [x] Form1.cs에 `_lastCollectedNodeOsnapMap` 필드 추가 (`Dictionary<int, List<(Vertex3D, string)>>`)
  - [x] `CollectAllOsnap` 내부에서 각 부재의 Osnap을 플랫 리스트(`osnapPointsWithNames`)에 추가하면서 동시에 부재별 맵에도 적재
  - [x] `ComputeViewDimensionsForMembers`에 `preBuiltNodeOsnapMap` optional 파라미터 추가 — 있으면 `memberIndices` 부분만 필터해 재사용, 없으면 기존대로 내부에서 `GetOsnapPoint` 호출해 구축 (시트 선택 자동 경로용)
  - [x] `CompleteMainDimensionPostClash`가 `_lastCollectedNodeOsnapMap`을 전달 → 치수추출 버튼 경로의 `GetOsnapPoint` 중복 호출 제거
  - [x] `Stopwatch`로 `ComputeViewDimensionsForMembers` 소요 시간 측정, `DiagLog T-032 치수 계산: visibleMembers=N osnapMapNodes=K chain=M ComputeViewDimensionsForMembers=Xms` 기록
  - [x] docs `메인 치수 추출.md` 단계 12·13 재기술 + 변경 이력
  - [x] MSBuild Debug 통과
  - [ ] 사용자 실기 확인 — DiagLog의 `ComputeViewDimensionsForMembers=Xms` 수치 개선 비교
- **후속 검토 여지**:
  - 오버레이 메시지 세분화 (예: "Osnap 수집 중 {n/N}") — 체감 시간 개선용
  - `GetOsnapPoint` 자체가 병목이면 Part 단위 배치 API 검토
- **영향 파일**: A2Z/Form1.cs (+1 필드), A2Z/Form1.BOM.cs (CollectAllOsnap 루프, CompleteMainDimensionPostClash), A2Z/Form1.Dimensions.cs (ComputeViewDimensionsForMembers 파라미터 추가)
- **연관**: T-018 (오버레이 UX), T-028 (치수 엔진 통합)

### T-028 — 치수 로직 통합 (2D 출력 기준 + 설치도 BBox 분기)
- **생성일**: 2026-04-22
- **착수일**: 2026-04-22
- **상태**: IN_PROGRESS (구현 중, 나중에 A로 전환 여지 열어둠)
- **관련**: — (사용자 직접 지시)
- **배경**: 4개 경로(치수추출 / 글로벌 X/Y/Z / 2D 출력 / 시트 선택 자동)의 치수 로직이 각기 다름. 사용자 요구: "2D 출력에서 사용하는 Osnap·로직을 기준으로 모두 통일"
- **확정 사항**:
  1. **엔진 기준**: `ShowAllDimensions(viewDirection)` 분기 ② = `nodeOsnapMap` + `FilterOsnapForDimAxis` + `AddChainDimensionByAxis(axis, viewDirection)`
  2. **중복 제거**: 같은 `(Axis, StartPoint, EndPoint)` 3자리 반올림 기준 병합. ViewDirection은 콤마 구분으로 누적 (예: "X,Y")
  3. **설치도(-2) 분기 유지 (옵션 B)**: 설치도 시트에서만 `ExtractInstallationDimensions`(BBox) 유지, 나머지 시트는 Osnap 엔진. 추후 A(완전 폐기)로 전환 가능
  4. **T-027 `FilterOsnapByViewDimensionUsage` 폐기**: 새 엔진이 `FilterOsnapForDimAxis`로 일원화
- **공용 헬퍼** (신설):
  - `ComputeViewDimensionsForMembers(memberIndices, viewDirection, tolerance) → List<ChainDimensionData>`
  - 내부: `nodeOsnapMap` 구축 → (뷰×축 조합 루프) → `FilterOsnapForDimAxis` → `MergeCoordinates` → `AddChainDimensionByAxis(axis, view)` → 중복 제거
  - `viewDirection == null` → 3뷰 × 2축 = 6조합 (치수추출·시트 선택용)
  - `viewDirection == "X"` → X뷰 2축만 (글로벌 버튼·2D 출력용)
- **데이터 변경**: `ChainDimensionData`에 `ViewDirection` 필드 추가 (어느 뷰에서 보이는 치수인지 "X,Y,Z" 콤마 구분)
- **4개 경로 재배선**:
  | 경로 | 변경 전 | 변경 후 |
  |---|---|---|
  | 치수추출 (`CompleteMainDimensionPostClash`) | `FilterOsnapByViewDimensionUsage` + `AddChainDimensionByAxis × 3` | `ComputeViewDimensionsForMembers(visibleMembers, null)` |
  | 글로벌 X/Y/Z | `ShowAllDimensions(viewDirection)` 내부 분기 ①②③ 재계산 | chainDimensionList에서 `ViewDirection.Contains(viewDirection)` 필터링 표시만 |
  | 2D 출력 (`GenerateSheetDrawing2D`) | `ShowAllDimensions(viewDirection, true)` 재계산 | 단순화된 ShowAllDimensions 재사용 (chainDimensionList 필터링) |
  | 시트 선택 자동 (`LvDrawingSheet_SelectedIndexChanged`) | 가공도(-3) 제외 모든 시트 `ExtractInstallationDimensions`(BBox) | -3 `ExecuteMfgDrawing` / **-2 `ExtractInstallationDimensions`(BBox 유지)** / 그 외 `chainDimensionList = ComputeViewDimensionsForMembers(sheet.MemberIndices, null)` |
- **세부**:
  - [x] `Models.cs`에 `ChainDimensionData.ViewDirection` 필드 추가
  - [x] `Form1.Dimensions.cs` `AddChainDimensionByAxis`에서 `ViewDirection = viewDirection` 기록 추가 (체인·전체 치수 두 곳)
  - [x] `Form1.Dimensions.cs` `ComputeViewDimensionsForMembers` 신설 (nodeOsnapMap 구축 + 뷰×축 루프 + 중복 제거 + ViewDirection 콤마 병합)
  - [x] `Form1.Dimensions.cs` `ShowAllDimensions` 단순화 — 내부 분기 ①②③ 제거, chainDimensionList 필터링 + 스마트 필터링만
  - [x] `Form1.Dimensions.cs` `FilterOsnapByViewDimensionUsage`(T-027) 제거 + placeholder 주석 유지
  - [x] `Form1.Dimensions.cs` `isInstallationMode`·`useDirectChain` 변수 제거, 오프셋 단일화
  - [x] `Form1.BOM.cs` `CompleteMainDimensionPostClash` 간소화 — `ComputeViewDimensionsForMembers` 호출
  - [x] `Form1.DrawingSheets.cs` `LvDrawingSheet_SelectedIndexChanged` 분기 재작성 (가공도-3 / 설치도-2 / 일반)
  - [x] MSBuild Debug 통과
  - [x] docs 2종 갱신: `메인 치수 추출.md` 파이프라인 재기술, `시트 선택.md` 분기 A 재작성
  - [x] **2026-05-11 (사용자 요청)**: `GenerateSheetDrawing2D` L1242도 `ExtractInstallationDimensions` → `ComputeViewDimensionsForMembers(null, 0.5f)` 교체 — 2D 출력 후 작업데이터 탭 = 도면 측 측정 데이터 1:1 일치
  - [x] **2026-05-11 (사용자 요청)**: `ExtractInstallationDimensions`의 "개별 부재 전체 길이" 블록 제거 (Form1.GlobalViews.cs L287~346) — 비인접 점 쌍처럼 보이는 부작용 해소. 시트 선택 -2 분기도 자동 영향
  - [ ] 설치도(-2) 분기 완전 폐기 검토 (옵션 A 전환) — 현재 BBox 유지 중. 사용자 확인 필요
  - [ ] 사용자 실기 확인 (4경로 일관성, 중복 제거 효과, 설치도 BBox 유지 확인, 2026-05-11 변경 반영)
- **영향 파일**:
  - `A2Z/Models.cs` (+1 필드)
  - `A2Z/Form1.Dimensions.cs` (공용 헬퍼 +80줄, ShowAllDimensions -70줄, FilterOsnapByViewDimensionUsage -45줄)
  - `A2Z/Form1.BOM.cs` (CompleteMainDimensionPostClash 치수 블록 -15줄)
  - `A2Z/Form1.DrawingSheets.cs` (LvDrawingSheet_SelectedIndexChanged 분기 +10줄)
  - docs: `메인 치수 추출.md`, `시트 자동 생성.md`, `시트 선택.md`

### T-006 — 2D 도면 템플릿 그리드 영역 크기 고정 + 뷰 내부 clip (T-007 흡수)
- **생성일**: 2026-04-15
- **착수일**: 2026-04-20
- **상태**: IN_PROGRESS (1차 레이아웃 구현 완료, **치수선 clip·모델 최대화 추가 실험 필요**)
- **관련**: FB-003, FB-004 (T-007 내용 흡수 — 2026-04-22 사용자 지시)
- **확정 스펙** (옵션 A — 1차):
  - A4 가로 297×210 / 마진 10 / 그리드 2×3 (셀 ≈ 92.3×95 mm) — 현재 유지
  - 뷰 4개: (1,1)ISO / (1,2)Z / (2,1)Y / (2,2)X — 현재 유지
  - **BOM → (1,3) 셀 이관** (`RenderTemplateOnGridStructure(table1, 1, 3)`)
  - **tableInfo → (2,3) 셀 이관** (`RenderTemplateOnGridStructure(tableInfo, 2, 3)`)
  - BOM 열 너비 합 82 → **92 mm로 조정** (ITEM 28→38, 그 외 유지)
  - BOM 최대 데이터 행 **14행**, 초과 시 마지막 행에 "…" + "+N건 생략" 표시 (옵션 2-a)
  - Anchor/X/Y 절대좌표 제거 → 셀 정렬(`SetGridCell*Alignment`)로 대체
- **추가 요구사항 (2026-04-22)**:
  - **치수선 clip 필수** — 뷰 셀 안에서 모델뿐 아니라 **치수선도 셀 경계를 벗어나지 않고** 그리드 내부에서만 표현되어야 함. 현재 치수선이 인접 셀로 늘어남
  - **뷰 내부 모델 최대화** (T-007 흡수) — `RenderSheetViewForDrawing`의 `targetHeight=40f` 하드코드를 셀 크기 기반 동적 계산으로 교체
  - **풍선 예약 영역 확보** (T-007 흡수) — 상단/측면에 일정 여백 확보해 번호 풍선이 겹치지 않게
  - **ISO/X/Y/Z 라벨 하단 고정** (T-007 흡수) — 셀 하단 같은 Y 좌표에 고정 배치
  - **실험 심화** — 1차 구현이 레이아웃만 고정시켰을 뿐 위 4개 요구를 충족 못 함. SDK의 `SetGridCellClipping` 류 API, `Create2DViewObject` 계열 파라미터, 그리드 셀 내부 렌더링 경계 제어 옵션 전수 조사 필요
- **세부** (1차 완료):
  - [x] Form1.DrawingSheets.cs L1020~1080 수정 — bInfo 절대좌표 제거, BOM/tableInfo `RenderTemplateOnGridStructure` 이관
  - [x] BOM `BOM_MAX_DATA_ROWS = 14` 상수 + "…+N건 생략" 행 렌더링
  - [x] BOM 열 너비 2차 축소: ITEM 28→38→17, MATERIAL/SIZE 8→12→11 (합 82→92→81→**77mm**)
  - [x] tableInfo 2차 축소: 60→57→47, 35→35→30 (합 95→92→81→**77mm**)
  - [x] 셀 정렬: BOM (1,3) Top/Center, tableInfo (2,3) Bottom/Center
  - [x] docs/기능/도면시트/시트 2D 렌더.md 1차 갱신 (단계표 7~9 추가, 분기 C 추가, 변경 이력 3건)
- **세부** (2차 — 추가 실험):
  - [ ] SDK 조사: 뷰 셀 내부 clip / 치수선 경계 제어 API (`sdk-verifier` 서브에이전트)
  - [ ] 치수선 렌더링 경로 추적 — 현재 치수선이 어디서 그려지며 왜 셀을 벗어나는지
  - [ ] `targetHeight=40f` 하드코드 → 셀 크기 기반 동적 계산
  - [ ] 풍선 예약 영역 설계 + 적용
  - [ ] ISO/X/Y/Z 라벨 위치 고정 (하단)
  - [ ] 빌드 + 실기 테스트 (치수선 셀 이탈 여부, 모델 크기, 풍선 겹침, 라벨 위치 모두 확인)
  - [ ] docs/기능/도면시트/시트 2D 렌더.md 2차 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (GenerateSheetDrawing2D, RenderSheetViewForDrawing, CreateIsoBalloonNotes)
  - `docs/기능/도면시트/시트 2D 렌더.md`
- **참고**: T-007은 본 항목에 흡수되어 제거됨 (2026-04-22)

---

## BLOCKED

### T-016 — 치수 추출 3회 이상 시 반복 누적 버그
- **생성일**: 2026-04-20
- **상태**: BLOCKED (재현 조건 수집 중)
- **관련**: — (사용자 피드백)
- **현황**: 사용자 재현 시도 중 다시 정상 동작. **간헐 버그(intermittent)**로 분류
- **이번 세션 진행**:
  - [x] 코드 분석으로 영향 가능 영역 좁힘 (4개 메서드)
  - [x] **로그 인프라 추가** — 다음 발생 시 즉시 진단 가능
    - `btnMainDimension_Click` ENTER/EXIT (xray·chain·osnap·bom 카운트)
    - `btnExtractDimension_Click` ENTER/EXIT
    - `LvDrawingSheet_SelectedIndexChanged` ENTER/SKIP/EXIT/FAIL (sheet#, prevXray, prevChain)
    - `ExtractInstallationDimensions` ENTER/EXIT (members, chain)
    - `LvDrawingSheet_SelectedIndexChanged`의 silent catch에 stack trace 추가
  - [ ] **다음 재현 시 사용자가 Visual Studio 출력창 로그 공유** → 즉시 진단
- **의심 가설 4개** (다음 재현 시 우선 검증):
  1. **Silent catch 무력화** — `LvDrawingSheet_SelectedIndexChanged` (Form1.DrawingSheets.cs:487~) 의 try-catch가 SDK 예외를 삼키면서 `xraySelectedNodeIndices = new List<int>(sheet.MemberIndices)` (L460) 또는 `ExtractInstallationDimensions` (L484)이 도달 못해 이전 값 유지
  2. **WinForms 이벤트 중복 발생** — `ListView.SelectedIndexChanged`는 선택 해제·선택 활성화 시 각각 발생. 3회째 두 이벤트가 race로 꼬여 새 시트의 갱신이 무효화될 가능성
  3. **xraySelectedNodeIndices 비동기 race** — `vizcore3d.BeginUpdate/EndUpdate` 사이에서 SDK 호출 도중 또 다른 핸들러가 같은 필드 수정
  4. **chainDimensionList 갱신 실패** — `ExtractInstallationDimensions` 진입 자체가 누락되거나 (L209 `if (members.Count == 0) return;`) early return으로 Clear만 되고 새로 채워지지 않음 — 그러나 Clear는 됐으므로 "이전 치수 반복"과는 직접 매치 X
- **재현 시 사용자에게 요청할 정보**:
  - 정확한 UI 조작 순서 (시트 선택? 부재 클릭? 어떤 버튼?)
  - Visual Studio 출력창 로그 (`[T-016 진단 로그]` prefix로 필터)
  - lvDimension(좌측 치수 목록)의 행 수 변화
- **영향 파일** (로그 추가):
  - A2Z/Form1.BOM.cs (btnMainDimension_Click)
  - A2Z/Form1.Dimensions.cs (btnExtractDimension_Click)
  - A2Z/Form1.DrawingSheets.cs (LvDrawingSheet_SelectedIndexChanged)
  - A2Z/Form1.GlobalViews.cs (ExtractInstallationDimensions)

---

## DONE (최근 20개)

### T-074 — 도면 리스트 첫 선택 즉시 조회
- **완료일**: 2026-07-21
- **커밋**: `e151b16` (코드 자동 동기화 `aac637f` 포함)
- **관련**: 사용자 직접 지시 (도면 리스트가 표시된 뒤에는 첫 클릭부터 즉시 조회)
- **결과**: 목록 표시 전에 일반·설치 시트 치수와 모든 시트 BOM을 준비. Sheet 1 치수와 모델 로드 시 Body→Part 매핑을 재사용하고, 관련 Part UDA를 한 번만 읽어 시트별 메모리 스냅샷 구성. 시트 클릭은 준비 데이터 적용과 애니메이션 없는 카메라 fit만 수행하며 구간별 시간을 로그로 기록.

### T-069 — 도면 생성 수동 버튼 제거
- **완료일**: 2026-07-21
- **커밋**: `1abc1c6`
- **관련**: 사용자 직접 지시 (자동 생성과 중복되는 수동 버튼 삭제)
- **결과**: `도면 생성` 버튼·클릭 핸들러를 제거하고 `GenerateDrawingSheets` 자동 호출 경로는 유지. 남은 도면 출력 버튼 재배치와 관련 문서 동기화 완료.

### T-004 — ALL 출력 후 시트별 도면 즉시 미리보기
- **완료일**: 2026-07-21
- **커밋**: `ca10699`
- **관련**: FB-001
- **결과**: 사용자 결정으로 `ALL` 버튼과 현재 시트 목록 전용 PDF 일괄 출력 기능을 삭제해 요청 대상이 소멸. 정식 `도면 일괄 출력`과 개별 `PDF 출력`은 유지.

### T-068 — 홀/슬롯홀 검출 휴리스틱 제거 → API 단일화
- **완료일**: 2026-06-23 (커밋 `pending`)
- **관련**: 사용자 직접 지시 ("휴리스틱 다 지우고 API로 대체")
- **요약**: `DetectHoles`(BOM.cs)의 원기둥·Osnap 추측 휴리스틱을 `GetNodeHoleInfo` API(`GetMfgHolesFromApi`)로 전면 교체. 휴리스틱 헬퍼(`IsCompleteCircle`·`HasSlotConnectingLines`)와 죽은 `GetHoleOrSlotForPoint` 삭제(약 790줄). `bom.Holes`/`SlotHoles`·BOM 표 홀사이즈가 API 기반. `Purpose`(EBOS)·`CircleRadius` 필드 유지. 제작도 죽은 풍선 정리는 후속.

### T-067 — 제작도 각도 표시 (부재-부재 접합 각도)
- **완료일**: 2026-06-23 (커밋 `pending` — 재설계분)
- **관련**: 사용자 직접 지시 (3개 요청 중 2번째). 사용자 정정: "한 부재 내부 각 말고, 부재끼리 수직·수평으로 안 만나면 표시"
- **요약**: `MarkNonRightAngles`를 **부재-부재 접합 각도**로 재설계(1차 '부재 내부 각'은 폐기·`0aa3ca1`에서 비활성 후 교체). 부재별 osnap 최원점쌍=길이축, 판형 제외, 부재쌍 osnap 끝점 3mm 근접=접합, 길이축 실제 3D 사잇각이 90의 배수가 아니면 `AddCustom3PointAngle`. 연결성은 osnap 근접 자체 판정(clashList 비의존). 설계·검증 워크플로우 2회(적대 검증으로 과잉수정 기각). **실측 대기**: 인자 순서(예각 모델), 표시값이 3D각 vs 투영각(로그에 둘 다 기록), 프레임 코너 라벨 겹침.

### T-066 — 가공도 보조선 오프셋 축소 (가공도 전용)
- **완료일**: 2026-06-23 (커밋 `7816bff`)
- **관련**: 사용자 직접 지시 ("가공도 보조선 줄여" → 범위 가공도만, 양 "많이" = 종이 1단 2·전체 4mm)
- **요약**: 공용 `ComputeCanvasAbsoluteOffsets`를 파라미터화(기본 5/5, 제작도 불변)하고 가공도 전용 상수 2/2(`MfgCanvasBaseOff`/`MfgCanvasLvlSp`)를 가공도 두 호출(`BuildMfgSceneCore`·EA 두번째 뷰)에서 전달해 종이 기준 1단 2·전체 4mm로 축소. 보조선 시작 gap(10mm)·역산 식은 불변.

### T-065 — 가공도 osnap 뷰 외곽 선별
- **완료일**: 2026-06-23 (커밋 `f8a292b`)
- **관련**: 사용자 직접 지시 ("가공도는 그 뷰의 외곽 osnap만 남아야" → 선별 규칙 "외곽 코너 + 홀" 선택)
- **요약**: 가공도 `BuildMfgSceneCore`에 체인 치수 생성 전 osnap 뷰 선별 단계(`FilterMfgOsnapForView`)를 추가했다. osnap을 카메라 화면 평면으로 투영해 각 열·행의 극점(외곽선)만 남기고 깊이·뒷면으로 투영되는 점은 제외한다. 제작도의 `FilterOsnapForDimAxis`에 대응하나 단일 부재·단일 뷰용 가공도 전용 규칙으로 분리. 홀 중심을 치수 기준점으로 추가하는 건 후속(홀 추출이 흐름 뒤쪽·슬롯 실측 중).

### T-044 — 홀 풍선 제작도 비표시 동작 검증·확정
- **완료일**: 2026-06-15 (커밋 `pending`)
- **관련**: 사용자 직접 지시
- **요약**: Hole, SlotHole, EarthBoss 형상 풍선을 가공도 전용으로 확정했다. `ShowAllDimensions`에서는 형상 풍선 목록을 항상 비워 일반/선택 X/Y/Z 뷰와 2D 제작도에서 표시하지 않는다. ISO 부재번호 풍선은 별도 기능이므로 유지한다.

### T-047 — SlotHole·Hole 종류 가공도 반영
- **완료일**: 2026-06-15 (커밋 `pending`)
- **관련**: 사용자 직접 지시
- **요약**: 가공도 `BuildMfgSceneCore`의 풍선을 Hole, SlotHole, UDA `PURPOSE=EBOS`인 EarthBoss 세 종류로 제한했다. 기존 원형 부재 반지름 풍선은 제거했고, 3D 미리보기와 PDF 첫 번째 뷰가 같은 공통 풍선 생성 로직을 사용한다.

### T-061 — docs 한글화 (옵션 B: 폴더·파일명 + README·용어집 정비)
- **완료일**: 2026-05-13 (커밋 `28a7f7f`)
- **관련**: — (사용자 직접 지시. 보조선/치수 사양 검색 시 영어 파일명에 막힘)
- **요약**: `docs/features/` → `docs/기능/` + 8개 카테고리 폴더 한글화(부재속성/BOM/간섭검사/치수/도면시트/2D도면/글로벌뷰/가공도) + 60개 파일 한글화. `docs/technical-notes/` → `docs/기술 노트/` + 4개 파일 한글화(치수 보조선 사양·치수 텍스트 위치·Osnap 기준·Sheet1 명명 기준). [docs/README.md](../README.md) 이모지 제거 + "기준·사양" 진입점 섹션 신규 + 카테고리 표 한글화. [docs/_glossary.md](../_glossary.md) 보조선/Osnap/Chain Dimension/Drawing Sheet 항목에 본진 링크 4개 추가. CLAUDE.md / .claude/commands/{commit,checkpoint}.md / .claude/hooks/docs-sync-reminder.sh 경로 참조 4곳 갱신. PowerShell 일괄 치환 3패스 + md-link-checker 3회 검증(잔존 0건). **`code-reference/` 영어 유지** (코드 파일 1:1 매핑 보존). 총 131개 파일 변경, 코드 변경 없음

### T-058 — 치수 Text 보조선 초과 시 우측 자동 배치
- **완료일**: 2026-05-06 (커밋 `pending`)
- **관련**: — (회사 doc 개발 요청 — 상 5)
- **요약**: sdk-verifier 검증 결과 `MeasureStyle.AlignDistanceTextPosition` enum (xml:9298, 0:아래/1:위/**2:바깥쪽**)로 글로벌 1줄 처리 가능. 기존 4곳 `=0` 설정 + EA 가공도 1곳 추가 = **5곳 동시 변경**: [Form1.Dimensions.cs:51](../../A2Z/Form1.Dimensions.cs) (선택 치수 표시), [Form1.Dimensions.cs:448](../../A2Z/Form1.Dimensions.cs) (`ShowAllDimensions` — 글로벌/시트/2D 출력 4경로 본진), [Form1.MfgDrawing.cs:325, 1050, 1703](../../A2Z/Form1.MfgDrawing.cs) (가공도 메인/sub/EA). 좁은 치수에서 텍스트가 보조선 사이를 침범하는 문제 회피. SDK가 치수별 개별 위치 옵션을 제공하지 않아 글로벌 적용(모든 치수 항상 바깥쪽), 회사 원문 "초과할 때만"과는 차이 있으나 핵심 의도(침범 회피) 충족. T-039 선행 무관(스타일 옵션은 offset 기준 결정 전에도 적용 가능). 통합 사양: [docs/기술 노트/치수 텍스트 위치.md](../기술 노트/치수 텍스트 위치.md)

### T-042 — 도면시트 목록 "기준부재" 컬럼에 부재 이름 추가 표시
- **완료일**: 2026-05-04 (코드 커밋 `e09c945`, DONE 이동 커밋 `pending`)
- **관련**: — (회사 doc 긴급최우선 1, T-014 보강)
- **요약**: 일반 시트(`>=0`) + 가공도(-3) 기준부재 셀에 `"1"` → `"1 (BOM이름)"` 병기 (`Form1.DrawingSheets.cs` ListView 갱신 단계, `$"{itemNo} ({sheet.BaseMemberName})"`). 매핑 실패 시 `sheet.BaseMemberName` fallback. **Sheet 1(`-1`)은 사용자 결정으로 `"전체"` 그대로 유지**, 설치도(`-2`)도 `"설치도"` 유지 — 회사 원문 "Sheet1 : 전체Item(Item Node 이름)" 부분은 미적용. 회사 doc "긴급최우선 1" 처리 완료

### T-049 — 치수 추출 백엔드 로직 문서화 (사전 추출 vs 즉시 추출)
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급중 3)
- **요약**: [docs/기능/BOM/메인 치수 추출.md](../기능/BOM/메인 치수 추출.md) Section 7.5 "치수 캐시 라이프사이클" 신설. `chainDimensionList`를 단일 진실 공급원으로 한 4경로(치수추출/글로벌 X/Y/Z/2D 출력/일반 시트/가공도) + 캐시 mermaid + 표 + 사용자 시각 단계별 흐름 + T-032 성능 최적화 연계. 회사 doc "치수추출 버튼 앞뒤 로직" 의문 답변. 코드 변경 없음, docs만

### T-050 — 3D View에서 X/Y/Z 축 표시 기능 추가
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급하 1)
- **요약**: sdk-verifier 결과 SDK 직접 지원 확인 — `vizcore3d.View.MarineAxis.Visible = true` 한 줄로 가능 (`MarineAxisManager`, XML L43019). [Form1.BOM.cs](../../A2Z/Form1.BOM.cs) `Vizcore3d_OnInitializedVIZCore3D` 단계 3.5에 추가. 결과: 3D 뷰 좌측하단에 ISO X/Y/Z triad 표시. 회사 doc "도면 외 3D View 축 미확인" 문제 해소. 추가 미세 조정(Length/Position/SetText) 필요 시 같은 위치에 보강 가능

### T-052 — Sheet1 포함부재 표기 "전체" → "1, 2, 3, ..." 개별 item 번호
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급하 3, T-014 보강)
- **요약**: [Form1.DrawingSheets.cs](../../A2Z/Form1.DrawingSheets.cs) ListView 단계의 `BaseMemberIndex == -1` 분기 제거 → 일반 시트(`>=0`)와 동일 로직(`bomIndexToItemNo` 매핑 + 정렬 + `string.Join`)으로 통합. 결과: Sheet 1의 포함부재 셀이 "전체" → "1, 2, 3, ..., N" 명시. 설치도(`-2`)도 같은 분기로 들어가지만 이전부터 동일 처리. BOM 14건 초과 시 ListView 컬럼 폭은 사용자 실기 후 조정

### T-046 — 가공도 치수 보조선 이중쇄선 → 가는 실선 (확장: 모든 보조선 + gap)
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc 긴급상 10 + 사용자 확장 — 모든 경로 + 모델 표면 gap)
- **요약**: 4경로(가공도 메인/EA + 일반 시트 2D 출력 + 글로벌 X/Y/Z + 치수추출)의 보조선을 `DrawDimension` 단일 지점에서 일괄 처리. (1) `Form1.MfgDrawing.cs:1542, 1900` LineType `DASHED_DOUBLEDOTTED` → `SOLID` 통일 + 토글 패턴 제거. (2) `OffsetTowardLineEnd` 헬퍼 + `ExtensionLineGap = 10.0f` 상수 신설 (Form1.Dimensions.cs) — 보조선 시작점이 모델 표면에서 10mm 떨어져 시작 (사용자 실기 후 1mm → 10mm 상향). 통합 사양: [docs/기술 노트/치수 보조선 사양.md](../기술 노트/치수 보조선 사양.md)

### T-053 — 중복 Sheet 삭제 후 Sheet 번호 자동 재채번 (v2 확장 포함)
- **완료일**: 2026-05-02 (v1 커밋 `8081688`), 2026-05-04 (v2 커밋 `pending`)
- **관련**: — (회사 doc 긴급하 4)
- **v1 요약**: `GenerateDrawingSheets` 단계 9(Sheet 1 동일 구성 제거) 직후에 `for (int i; i < drawingSheetList.Count; i++) drawingSheetList[i].SheetNumber = i + 1` 일괄 재채번. 일반 시트 빠진 자리만큼 후속 시트(설치도·가공도)도 자동 정합
- **v2 확장 (2026-05-04)**: 자동 제거 범위를 "Sheet 1 동일 구성" 한정 → **"모든 일반 시트 쌍에서 부재 구성 동일 시 첫 등장만 살림"** 으로 확장 (사용자 결정: *"포함부재가 같으면 기준부재가 달라도 같은 형상이다"*). `MemberIndices.OrderBy` 정렬 키 + `HashSet<string>`로 첫 등장 추적. Sheet 1 / 설치도 / 가공도는 의미가 다른 시트라 검사 제외하고 보존. Sheet 1과 동일 구성인 일반 시트는 별도 RemoveAll로 추가 제거. [시트 자동 생성.md](../기능/도면시트/시트 자동 생성.md) 단계 9·9.3 + 분기 C + 변경 이력 갱신

### T-055 — 검증 보고서: Osnap 기준점 코드 동작 확인
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc "완료 3" 의문 답변용)
- **요약**: 4경로 보조선 데이터 흐름 + 부재별/전체 풀 동시 적재 + X/Y/Z 뷰별 primary/secondary 매핑 + 4단 dedup(부재 → 전역 dimAxis → MergeCoordinates 0.5mm → keyToDim) 코드 트레이스 완료. 결론 **부분 일치** — 핵심 의도(코너 우선 + 중복 제거)는 모두 구현되었으나, 부재 단위에서 4코너가 아니라 1점만 남기는 점이 명세 문구와 다름. 산출물: [docs/기술 노트/Osnap 기준.md](../기술 노트/Osnap 기준.md)

### T-056 — 검증 보고서: Sheet1 부재 이름 부여 기준 (Z-MAX 정렬)
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc "완료 5" + "수정 후 확인 필요 2" 의문 답변용)
- **요약**: 현재 코드는 `BBox.MaxZ` (Form1.BOM.cs:735) 기준 정렬, 회사 명세는 `max(Osnap.Z)` 기준 — 데이터 출처 차이. 직립 H빔·평판 등 일반 철골 형상에선 두 값이 동등하므로 정렬 결과 같음. 경사 부재·곡면 Body에서 수 mm 차이 발생 가능 (정렬 1~2칸 흔들림). 결론 **부분 일치** — 회사 답변에 따라 후속 작업(Form1.BOM.cs:688 osnapList 활용) 신설 가능. 산출물: [docs/기술 노트/Sheet1 명명 기준.md](../기술 노트/Sheet1 명명 기준.md)
- **결정 (2026-05-05)**: 사용자가 **BBox 유지** 결정. A2Z 일반 데이터셋에서 정렬 결과 동등, 차이 케이스도 1~2칸 변동 수준으로 실용 영향 작음. 회사 회신은 보고서 § 7 단답 그대로 사용. 변경 없이 종결

### T-018 — 장시간 작업 진행 UX 표시 (오버레이 라벨)
- **완료일**: 2026-04-24 (사용자 묵시 OK — 오버레이 동작 정상 관찰)
- **관련**: — (사용자 피드백)
- **요약**: ShowBusyOverlay/HideBusyOverlay 헬퍼로 panelViewer 중앙 반투명 라벨. btnMainDimension_Click의 4단계("간섭검사 실행 중..." → "Osnap 수집 중..." → "치수 계산 중..." → 해제). 사용자가 동작 보고 "이 오버레이가 뭐 하는 거지" 질문할 정도로 UX 확립

### T-029 — 치수추출 버튼은 3D 뷰 치수 렌더링하지 않음
- **완료일**: 2026-04-24 (사용자 "치수추출, 시트 선택하면 깨끗해 글로벌 버튼 누르면 치수 잘나와")
- **관련**: — (사용자 직접 지시)
- **요약**: `CompleteMainDimensionPostClash`에서 `ShowAllDimensions()` 호출 제거 + `Review.Measure.Clear()` + `ShapeDrawing.Clear()`. chainDimensionList는 채워둠 → 글로벌 버튼·2D 출력에서 재사용

### T-030 — 시트 선택 시 3D 뷰 치수 렌더링 제거
- **완료일**: 2026-04-24 (T-029와 함께 확인)
- **관련**: — (사용자 피드백 "시트 눌렀을 때 치수가 나오는데 왜 나오는지 모르겠음")
- **요약**: T-029 정책의 시트 선택 분기 확장 — 일반 시트에서도 깨끗한 3D 뷰 유지

### T-031 — 가공도 시트 실선(SMOOTH) 처리
- **완료일**: 2026-04-24 (사용자 "가공도 선택 시 실선으로 잘 나와")
- **관련**: — (사용자 직접 지시)
- **요약**: `ExecuteMfgDrawing` L142 `SetRenderMode(DASH_LINE)` → `SMOOTH` 교체. 2D 캡처 내부의 DASH_LINE은 유지

### T-033 — 오버레이 해제 타이밍
- **완료일**: 2026-04-24 (사용자 "T-33은 해결됐어")
- **관련**: — (사용자 피드백)
- **요약**: `Osnap → 치수 → GenerateDrawingSheets → HideBusyOverlay → MessageBox` 순서로 재배치. 팝업 뜰 때 오버레이 깨끗

### T-034 — 글로벌 뷰 실선(SMOOTH) 처리
- **완료일**: 2026-04-24 (사용자 "T-34실선으로 잘 나와")
- **관련**: — (사용자 피드백)
- **요약**: `ApplySelectedNodesView`·`ApplyFullModelView` + `ApplyDrawingSheetView`(ISO/X/Y/Z 4곳) DASH_LINE → SMOOTH

### T-035 — 글로벌 뷰 버튼 클릭 시 부재 선택 해제
- **완료일**: 2026-04-24 (사용자 "T-35 해결")
- **관련**: — (사용자 피드백)
- **요약**: `ApplyFullModelView`·`ApplySelectedNodesView` 시작부에 `Object3D.Select(DESELECT_ALL)` 추가

### T-026 — 치수추출 진입 시 이전 xray 선택 잔존 클리어
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 피드백 + 로그 근거)
- **커밋**: `7614417`
- **요약**:
  - 증상: 부재 1개 띄우고 치수추출 → 전체 띄우고 다시 치수추출 → 1개 기준 결과 반복
  - 원인: `LvDrawingSheet_SelectedIndexChanged`가 설정한 `xraySelectedNodeIndices` 값이 잔존 → `CollectBOMData` L591의 X-Ray 우선 필터에 걸려 "그 부재만" 수집
  - 수정: `btnMainDimension_Click` 진입부에 `xraySelectedNodeIndices.Clear()` 추가 — "치수추출 버튼은 항상 현재 visible 기준"

### T-025 — 치수추출 직후 Sheet 1 기준 BOM 테이블 자동 출력
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 피드백)
- **커밋**: `7614417`
- **요약**:
  - 치수추출 완료 후 `lvDrawingBOMInfo`(도면정보 탭 BOM 테이블)가 빈 상태로 남던 문제
  - `GenerateDrawingSheets()` ListView 갱신 직전에 `CollectBOMInfo(false, drawingSheetList[0])` 호출 추가 → Sheet 1(전체) 기준 BOM 테이블 자동 채움
  - visibility·카메라 무영향 (try/catch로 SDK 예외 보호)

### T-023 — 치수추출 사전조건 (연결성 1덩어리 판정)
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시, 3차 재설계 후 확정)
- **커밋**: `1620289`(v1 visible/selected==1, 의도 불일치로 원복) → `2a216b5`(v2 STRU UDA 주석 블록, 보류 후 원복) → **`cc72e94`**(v3 Clash 기반 연결성 — 최종 채택)
- **최종 판정**: Clash 인접 그래프 기준 연결 성분 == 1일 때만 치수추출 허용. 떨어진 부재가 있으면 차단
- **요약**:
  - `IsSingleConnectedComponent(out int)` 헬퍼 신설 (Part→Body 역매핑 양방향 인접 그래프 + BFS + early exit)
  - `Clash_OnClashTestFinishedEvent`에서 clashList 수집 후 판정, n≠1이면 차단 MessageBox
  - 파이프라인 재배치: `btnMainDimension_Click`은 BOM+Clash 시작까지만, Osnap/치수/요약/시트는 `CompleteMainDimensionPostClash`로 분리
  - 단일 부재(clashStarted=false)는 판정 스킵하고 Post 메서드 직접 호출 (T-024 fallback과 통합)
  - docs 3종(메인 치수 추출.md / 간섭검사 완료 이벤트.md / 사용자 매뉴얼 치수 추출.md) 전면 재작성
  - v1/v2 주석 블록·헬퍼 모두 제거 완료

### T-024 — 단일 부재 치수추출 결과가 도면 시트 목록에 반영 안 됨
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시)
- **커밋**: `06a1395` → `cc72e94` (T-023 v3 구현에서 fallback 경로 통합)
- **원인 + 수정**:
  - 원인: `DetectClash`가 targetNodes=1이면 `clashCount=0` → return false → `PerformInterferenceCheck` 미호출 → Clash 이벤트 미발동 → `GenerateDrawingSheets` 미호출
  - 부가: 간섭 없는 다중 부재도 이벤트 내 `if (clashList.Count > 0)` 조건에 걸려 시트 생성 안 되던 숨은 버그 공존
  - 수정 1 (06a1395): `btnMainDimension_Click`이 `DetectClash` 반환값을 받아 false면 시트+요약 직접 수행
  - 수정 2 (06a1395): `Clash_OnClashTestFinishedEvent` 조건부 호출 → 무조건 호출
  - 통합 (cc72e94): 단일 부재 fallback이 `CompleteMainDimensionPostClash(isSingleMember=true, 0)`으로 통일

### T-022 — 시트/BOM 선택 시 3D View 부재 "선택상태" 동기화
- **완료일**: 2026-04-22 (사용자 "처리된 것 같아" 확인)
- **관련**: — (사용자 직접 지시)
- **커밋**: `ab8313e`
- **SDK**: `vizcore3d.Object3D.Select(List<int>, true, false)` + `Select(DESELECT_ALL)` (sdk-verifier로 VIZCore3D.NET.xml L51882~51946 검증)
- **요약**:
  - `LvDrawingSheet_SelectedIndexChanged`에 기준부재 빨간 하이라이트 (Sheet 1·설치도 제외, 가공도는 MemberIndices[0], 일반은 BaseMemberIndex)
  - `LvDrawingBOMInfo_SelectedIndexChanged`에 단일 부재 하이라이트 + FlyToObject3d
  - 피드백 루프 분석 완료 — `Object3D_OnObject3DSelected`가 속성탭만 갱신하므로 안전. 부수효과로 부재 정보 탭 자동 갱신 (UX 향상)

### T-027 — 치수추출 Osnap 선별 (T-028로 대체됨)
- **완료일**: 2026-04-22 (REPLACED BY T-028)
- **관련**: — (사용자 직접 지시)
- **커밋**: `bb48a16` (신설 → 체인 치수 수 감소) → `375d66f` (T-028 엔진 통합에서 제거)
- **요약**:
  - `FilterOsnapByViewDimensionUsage` 헬퍼로 뷰×축 6조합 필터 endpoint 합집합 구현 → 체인 치수 수 감소
  - 하지만 2D 출력 엔진(`FilterOsnapForDimAxis`)과 로직이 달라 4경로 일관성 문제 발생 → 사용자 재피드백 "같은 Osnap·같은 로직"
  - **T-028**에서 엔진 통합(`ComputeViewDimensionsForMembers`)하며 본 T-027 헬퍼 제거. 당시 기능은 T-028의 6조합 중복 제거에 흡수됨

### T-017 — 라이선스 인증 코드를 Form1.BOM.cs에서 분리
- **완료일**: 2026-04-22 (사용자 실기 테스트 통과)
- **관련**: — (사용자 직접 지시, 코드 정리)
- **커밋**: `d849663`
- **요약**:
  - 옵션 (A) 채택 — `Form1.License.cs` 신규 partial (71줄)로 라이선스 로직 이동
  - `InitializeLicense()` 공용 진입점 — 실패 시 MessageBox + false, 성공 시 30분 갱신 타이머 기동 후 true
  - `Form1.BOM.cs` `Vizcore3d_OnInitializedVIZCore3D` 앞 10줄 → `if (!InitializeLicense()) return;` 한 줄로 축약
  - `Form1.BOM.cs`에서 `StartLicenseRefreshTimer`·`LicenseRefreshTimer_Tick` 제거 (약 -30줄)
  - `Form1.cs`에서 `licenseRefreshTimer` 필드 선언 제거
  - `A2Z.csproj`에 `Form1.License.cs` Compile 항목 추가 (`DependentUpon=Form1.cs`)
  - docs: `form1-bom.md` 라이선스 항목 5곳 정리, `form1-license.md` 신설, `기능/BOM/VIZCore3D 초기화.md` 단계표·에러표·링크·이력 갱신
  - MSBuild Debug 통과, 사용자 실기에서 앱 기동 정상 확인

### T-021 — BOM 정보 행 선택 시 부재 카메라 fit
- **완료일**: 2026-04-22
- **관련**: — (사용자 직접 지시)
- **커밋**: `9b99b8c`
- **요약**:
  - `lvDrawingBOMInfo`(도면정보 탭 BOM 테이블) 행 선택 시 카메라 fit 동작 신설
  - 가시성은 그대로 두고 `vizcore3d.View.FlyToObject3d(new List<int>{bodyIdx}, 1.2f)`로 카메라만 이동 — 현재 시트 맥락 유지
  - No. 컬럼 파싱 → `bomList[No-1].Index` Body 조회 (CollectBOMInfo의 `partIndexToBomNo` 매핑 = `bi+1`과 동일)
  - 요약행(Row 0) · No 파싱 실패 · 범위 초과는 조용히 return
  - 이벤트 등록 위치: [Form1.cs:166](../../A2Z/Form1.cs:166)
  - 새 핸들러: [Form1.DrawingSheets.cs `LvDrawingBOMInfo_SelectedIndexChanged`](../../A2Z/Form1.DrawingSheets.cs)
  - 신규 문서: [BOM 정보 선택.md (SHT-010)](../기능/도면시트/BOM 정보 선택.md), `_인덱스.md` 등록 추가
  - 사용자 실기 테스트 통과 (2026-04-22)

### T-014 — 도면 시트 목록의 "기준부재/포함부재" 컬럼을 item 번호로 표시
- **완료일**: 2026-04-22
- **관련**: — (사용자 피드백)
- **커밋**: `9b99b8c`
- **요약**:
  - `lvDrawingSheet` 표시 포맷 변경: 부재 이름 대신 **item 번호**(= `bomList` 순서 i+1 = ISO 풍선 번호 = BOM 정보 탭 No.)
    - Sheet 1 → "전체 / 전체"
    - Sheet 2+ → `{기준번호}` / `{포함 번호 오름차순 콤마}` (예: `1 / 1, 3, 5`)
    - 설치도 → "설치도 / {전체 item 번호}"
    - 가공도 → `{MemberIndices[0]의 item 번호}` / 공란
  - 사용자 결정 확정: (1) 시트 생성 로직은 T-015 그대로 유지, 표시만 변경 (2) 접두사 `item` 없이 숫자만 (3) 가공도도 번호로
  - 구현: [Form1.DrawingSheets.cs:215~281](../../A2Z/Form1.DrawingSheets.cs:215) `bomIndexToItemNo` Dictionary + ListView 갱신 블록
  - 빌드 오류 1건 수정: 외부 `int mfgNo=1`(가공도 번호)과 변수명 충돌 → `mfgBomIdx`/`mfgItemNo`로 리네임
  - 문서: `시트 자동 생성.md` 단계 10·상태 섹션·변경 이력 갱신
  - 사용자 실기 테스트 통과 (2026-04-22)

### T-009 — 초기화 버튼 누락 항목 보강
- **완료일**: 2026-04-22 (사용자 실기 테스트 통과)
- **관련**: T-008 후속 (사용자 피드백)
- **커밋**: `45d17dd` (본체) + `10c7d8c` (후속 — `Clear2DView()` 호출 시점 `Model.Open` 이후로 이동, 4번 번쩍임 해결)
- **요약**:
  - `ResetToInitialState()` 정리 블록에 3줄 추가 — `lvDrawingBOMInfo.Items.Clear()`, `vizcore3d.View.SetRenderMode(RenderModes.SMOOTH)` (DASH_LINE 해제), `Clear2DView()`
  - `Clear2DView()` 호출 시점을 `Model.Open` 성공 이후로 재배치 (SDK가 Open 시 2D 뷰 자동 복원하는 이슈)
  - docs/기능/BOM/초기화.md 갱신
  - SDK 참고: `RenderModes.SOLID`는 존재하지 않음 → `SMOOTH` 사용
  - 다른 기기 실기 테스트 통과 (2026-04-22)

### T-015 — Sheet 생성 로직 재설계 (모든 부재가 기준부재)
- **완료일**: 2026-04-21
- **관련**: — (사용자 피드백)
- **커밋**: `9b870a0`
- **요약**:
  - **기존 문제**: `GenerateDrawingSheets`의 `appearedAsIncluded` 스킵 로직 — "다른 시트의 포함부재로 등장한 부재는 기준부재가 될 수 없음". 결과: 1-2-3-4 연쇄 Clash에서 Sheet 2(기준 1, {1,2}) + Sheet 3(기준 3, {3,2,4})만 생성. 기준부재 2·4 시트 누락
  - **사용자 의도**: 모든 부재가 각자 기준부재 시트를 가지며, 포함부재는 1-hop 이웃
  - **수정**: [Form1.DrawingSheets.cs:105~142](../../A2Z/Form1.DrawingSheets.cs:105) `appearedAsIncluded` HashSet 선언·검사·추가 3곳 모두 제거. 주석도 T-015 결정 배경으로 교체
  - **결과 예**: 1-2-3-4 연쇄 Clash → Sheet 2(기준 1), 3(기준 2), 4(기준 3), 5(기준 4) 4개 생성. 단계 9의 Sheet 1 중복 제거 유지 (과잉 시트 자동 정리)
  - `docs/기능/도면시트/시트 자동 생성.md` 전면 갱신 — 이전 문서가 실제 코드와 불일치(BFS 서술·E03 오류·가공도/중복제거 누락)된 부분까지 교정
  - 빌드 검증은 사용자 기기에서 (A2Z.exe 실행 중이라 자동 빌드 불가)

### T-020 — 파일 열기·치수 추출을 탭 밖 공용 패널로 이동
- **완료일**: 2026-04-21
- **관련**: — (사용자 직접 지시, UX)
- **커밋**: `29e177f`
- **요약**:
  - `panelGlobalActions` 신설 — `splitContainer1.Panel1` 내 Dock.Top
    - 위치: panelGlobalViewButtons 아래, tabControlLeft 위
    - 배경색 `FromArgb(45,45,48)` — panelGlobalViewButtons와 통일
    - Size 438×60, Padding 5
  - `btnOpen`·`btnMainDimension`을 `groupBox1` → `panelGlobalActions`로 이관
    - 결과: 도면정보/작업·데이터/부재정보 **어떤 탭에서도 접근 가능**
    - 버튼 Location (x, 25) → (x, 5)로 조정
  - **groupBox1 후속 정리** (두 버튼 빠져 생긴 빈 공간 제거)
    - Size 110 → 55
    - 작은 버튼 6개 (BOM/Clash/Osnap/치수/2D 생성/PDF 내보내기) Y=78 → 20
  - 사용자 직접 빌드 확인 완료
  - R9 판단: UI 레이아웃 변경만이라 기능/code-reference 갱신 불필요

### T-019 — 도면정보 탭을 첫 번째로 이동
- **완료일**: 2026-04-21
- **관련**: — (사용자 직접 지시)
- **커밋**: `3f51a02`
- **요약**:
  - 앱의 최종 목표가 **제작도 출력**이라 도면정보 탭을 첫 번째로 배치
  - 프로그래밍 위험 전수 검증 — 모두 안전
    - `SelectedIndex = 0` 하드코딩 (Designer L192): 탭 재배열 후 도면정보가 자동 기본 선택 (=원하는 동작)
    - `SelectedTab == tabPageDrawing` (GlobalViews.cs:54): 탭 **객체** 비교, 순서 무관
    - 다른 탭 인덱스 하드코딩 없음
  - Form1.Designer.cs 4곳 수정
    - L186~188: `Controls.Add` 순서 Drawing → Work → Attribute
    - TabIndex 재매김: Drawing=0, Work=1, Attribute=2
  - 런타임 로직 영향 0 (Designer 메타데이터만)

### T-011 — 시드 서브에이전트 2개 도입 (sdk-verifier, md-link-checker)
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백, 반복 실수 방지)
- **커밋**: `92d0488`
- **요약**:
  - 이번 대화에서 드러난 반복 실수 (`RenderModes.SOLID` 가정, `Model.Close` 누락, 링크 공백 133건 등) 방지용 시드 에이전트 2개 신설
  - `.claude/agents/sdk-verifier.md` — `VIZCore3D.NET.xml` 선행 검색으로 API 존재·시그니처·공식 예제 패턴 반환. SDK 새 멤버 처음 쓸 때 호출
  - `.claude/agents/md-link-checker.md` — `docs/**/*.md` 링크 공백·파일 부재 검증 + Python 치환 스크립트 제안. 대량 문서 수정 후 호출
  - `CLAUDE.md` R10, R11 추가 — 각 에이전트 호출 트리거 주소
  - **제외**: 오케스트레이터 프로토콜(동적 에이전트 생성·합병·삭제)은 사용 패턴 축적 후 재평가. 현 프로젝트 규모에 오버 엔지니어링 우려
  - "중간" 도입 경로 채택 (사용자 합의)

### T-010 — 문서 내부 링크 공백 문제 일괄 수정
- **완료일**: 2026-04-20
- **관련**: — (사용자 피드백)
- **커밋**: `10c7d8c`
- **요약**:
  - `docs/**/*.md` 전체 마크다운 링크 `]( ... )` 내부 공백을 **`%20`**으로 일괄 치환 (Python 스크립트)
  - **30파일, 147건 치환**. 상위: `사용자-매뉴얼/README.md`(44), `FEEDBACK.md`(8), 글로벌뷰 시리즈(6~7)
  - 외부 URL(`http://`, `https://`, `mailto:`, `#`로 시작)과 공백 없는 링크는 제외 처리
  - 대안(파일명 공백 제거 / `<path>` 각괄호)은 가독성·호환성 이유로 기각
  - 사용자 샘플 확인 통과

### T-008 — 초기화 버튼 + 같은 파일 재Open 버그 수정
- **완료일**: 2026-04-20
- **관련**: —  (FB/REQ 없음, 사용자 직접 지시)
- **커밋**: `45d17dd`
- **요약**:
  - 3D 뷰어 상단 글로벌 뷰 버튼 줄 제일 왼쪽에 `btnResetToInitial` ("초기화", 회색) 신설
  - `ResetToInitialState()` 헬퍼 — 누적 상태(List 9종 + UI ListView 5종 + SDK Clear 3종) 전면 초기화 후 동일 파일 재로드
  - `balloonOverrides.Clear()` 포함 (btnOpen이 누락했던 항목)
  - 확인 다이얼로그 + 가드 체크(`currentFilePath` + `Model.IsOpen`)
  - **버그 수정**: VIZCore3D는 같은 경로 중복 `Model.Open()`을 거부 → `Model.Open` 전 `if (IsOpen()) Close();` 패턴 적용 (공식 예제 L47297/L60261)
  - **btnOpen_Click 동반 수정**: 같은 파일 재선택 시 동일 버그 발생 소지 → 같은 패턴 적용
  - **UI 너비 축소**: 5개 버튼 Size 105→80, Location 재배치 (8/93/178/263/348), 패널 Size 558→438
  - 문서: `docs/기능/BOM/초기화.md` 신설 (BOM-005), `docs/사용자-매뉴얼/1.기본-작업/초기화.md` 신설, `모델 열기.md`에 Close 단계 추가, `_인덱스.md`·`code-reference/form1-bom.md`·`사용자-매뉴얼/README.md` 갱신
  - 사용자 실기 테스트 통과 (부재 일부 숨기고 치수 추출 → 초기화 → 정상 복원)

### T-003 — 사용자 매뉴얼 전면 작성 (39개 버튼 문서)
- **완료일**: 2026-04-14
- **관련**: REQ-001
- **커밋**: `74fe209`
- **요약**:
  - `docs/사용자-매뉴얼/` 신규 폴더 + 39개 버튼 문서 + README
  - 실제 UI 라벨 기반 폴더·파일명 (`2.작업-데이터 탭/2D 생성.md` 등)
  - 7섹션 표준 템플릿 (요약/위치/사전조건/순서/분기/에러/이어지는작업)
  - SDK 용어 → 사용자 언어 번역 (에러는 실제 팝업 문구 그대로)
  - 멀티 에이전트 협업 (인벤토리 W-D → Writer W-A/B/C 병렬 → Reviewer 전수검사)
  - Reviewer 통과: 템플릿 0위반, 용어 0위반, 깨진 링크 0, 에러 메시지 일치
  - `docs/README.md` 상단에 개발자/사용자 분기 카드 추가
  - 개발자 문서(`docs/기능/`) 영향 없음

### T-002 — 개발 워크플로우 자동화 확장
- **완료일**: 2026-04-13
- **관련**: —
- **커밋**: `ac14c86`
- **요약**:
  - REQUESTS.md (본인 요청 inbox, REQ-xxx) 추가
  - /checkpoint 슬래시 커맨드 (세션 요약 + 이어갈 지점)
  - PostToolUse 훅 (Form1.*.cs Edit/Write 시 docs 동기화 리마인더)
  - CLAUDE.md R2 확장 (4파일 자동 훑기), R8·R9 추가
  - /commit에 REQ-xxx 처리 통합

### T-001 — 프로젝트 초기 셋업 + 로직 흐름 문서화
- **완료일**: 2026-04-13
- **관련**: —
- **커밋**: `0000000` (초기 커밋)
- **요약**:
  - git 원격 연결 (github.com/uuuuj/a2z, HYI 브랜치)
  - 기존 HYI → X_HYI 로 아카이브
  - docs/ 로직 흐름 문서 72개 작성 (48개 핸들러 전수)
  - .gitignore 보강, CLAUDE.md, tracking 폴더 구조화
  - /commit 슬래시 커맨드 추가

---

## 형식 예시

```
### T-034 — 풍선 충돌 회피 로직 개선
- **생성일**: 2026-04-14
- **상태**: IN_PROGRESS
- **관련**: FB-012
- **세부**:
  - [ ] balloonOverrides Dict 사용 방식 개선
  - [ ] AABB 회전 시도 횟수 조정 (현재 36회 → 조절)
  - [ ] docs/기능/도면시트/ISO 도면.md 갱신
- **영향 파일**:
  - `A2Z/Form1.DrawingSheets.cs` (CreateIsoBalloonNotes)
  - `docs/기능/도면시트/ISO 도면.md`
```
