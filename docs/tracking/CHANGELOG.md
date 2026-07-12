# 변경 이력 (CHANGELOG)

커밋·릴리즈 단위의 완료 기록입니다. **날짜 역순**로 상단에 추가합니다. `/commit` 커맨드가 자동 갱신.

> 형식: `## YYYY-MM-DD — 요약` + 세부 목록 + 커밋 해시 + 관련 ID

---

## 2026-07-12 — 템플릿 행 높이 축소 (RenderTemplate 409pt 초과 에러 해소)

**유형**: fix
**커밋**: `pending`
**관련**: 사용자 검증 보고 "RenderTemplate 실행 중 오류 : Row height must be between 0 and 409" (2026-07-12)

**증상·원인**: 신 템플릿의 큰 병합 셀이 Excel 행 높이 최대 **409pt**를 초과. 모든 행이 16.95pt인데 SDK RenderTemplate이 병합 셀을 그릴 때 '구성 행 높이 합'을 행 높이로 설정 → 제작도 View 슬롯 **84행=1424pt**, 좌표 라벨(A/B/C/D) **47행=797pt**(양 템플릿 공통)이 한계 초과로 예외. 셀 개수가 아니라 **행 높이** 문제.

**해결** (`제작도_도면_1.xlsx`·`가공도_도면_1.xlsx`, 코드 무변경):
- 모든 행(210개) 높이를 16.95 → **4.0pt** 균일 축소. 최대 병합: 제작도 336pt·가공도 188pt로 409 아래. 균일 축소라 셀 격자 비율 불변 → SDK가 캔버스(297×210) 매핑 시 출력 레이아웃 동일. 열 폭·병합 범위·`{Input_N}`·`{View_n}`·`{Image}` 태그 전부 불변(검증).
- JSON 캐시는 엑셀 mtime 비교로 자동 재변환(코드 변경 불필요) — 신 xlsx가 최신이라 구 `.tags.json` 무효화.

**부작용**: Excel에서 행이 4pt로 매우 얇아 손으로 편집하기 불편. 편집 편의가 필요하면 후속으로 **성긴 그리드(행 수 축소 → 행을 다시 높게)** 재제작이 정공법(병합이 24행 이하가 되면 16.95pt 유지 가능). 지금은 출력 정상화 우선.

**검증 (사내)**: 2D 출력 → ① 행 높이 에러 없이 렌더되는지 ② 출력 비율(뷰·표·라벨 위치)이 의도대로인지 ③ `[TplJson] hasTags=` 로그.

## 2026-07-12 — 템플릿 JSON 사전변환 본 적용 (2D 출력 멈춤 해소)

**유형**: fix / feat
**커밋**: `dc853fb`
**관련**: 사용자 검증 보고 "2D 출력 누르니 프로그램 멈춤" + 선택 "JSON 사전변환 본 적용" (2026-07-12)

**증상·원인**: 신 템플릿(제작도_도면_1·가공도_도면_1)은 1칸=1mm라 **297×210 = 약 6만 셀(구 템플릿 대비 16배)**. 출력마다 `ImportExcelWithData`(엑셀 통째 파싱, 벤더가 지목한 "5초" 병목) + `GetViewAreasFromExcel`가 각각 큰 엑셀을 파싱 → 수십 초간 UI 스레드 블록 = "응답 없음". JSON 사전변환은 임시 PoC에만 있고 본 경로엔 미적용 상태였음.

**해결** (`Form1.ExcelTemplate.cs` 공용 헬퍼 신설 + 제작도·가공도 경로 배선):
- `EnsureTemplateTagJson(xlsx)` — `ConvertExcelToJson(xlsx, null, …)`로 엑셀을 **세션 1회만** JSON으로 굽는다(inputData=null → `{Input_N}` 태그 텍스트 보존 기대). 결과 JSON에 `{Input_` 토큰이 실제 남는지 **런타임 자가 검증** → 남으면 치환 방식 사용, 안 남으면 폴백 확정.
- `TryApplyTemplateFromJson(xlsx, data)` — 캐시된 태그 JSON에서 `{Input_N}`만 문자열 치환(JSON 이스케이프) → 임시 JSON 기록 → `ApplyTemplateFromJson`(엑셀 파싱 없음)으로 그린다. 실패/불가 시 false 반환 → 호출자가 기존 `ImportExcelWithData`로 **자동 폴백**.
- `GetViewAreasCached(xlsx)` — `{View_n}` 영역도 세션 1회만 파싱(정적 좌표) → 배치 출력 재파싱 제거.
- 제작도(`GenerateSheetDrawing2D_WithExcelTemplate`)·가공도(`GenerateMfgDrawingManual` 페이지 루프·카메라 프로브) 모두 JSON 우선 + 폴백으로 전환. `[TplJson]`/`[TplTime]` 로그로 실제 경로·시간 기록.
- 중복된 임시 JSON PoC(`MfgTemplateJsonPocEnabled`) OFF — 본 경로가 대체, PoC는 매번 강제 재변환이라 오히려 느림.

**효과 기대**: 세션 첫 출력은 변환 1회(일회성 지연) 후, 2번째 출력·배치·가공도 페이지 루프는 엑셀 파싱 없이 빠름. **blind 가정 없음** — JSON에 태그가 안 남으면 자동으로 기존 경로로 폴백해 최소한 동작은 보장(느릴 뿐).

**검증 (사내)**: 2D 출력 1회 → ① 멈춤 없이 뜨는지 ② `[TplJson] hasTags=` 로그값(True면 JSON 경로, False면 폴백) ③ 표·라벨·BOM·치수가 기존과 동일한지. hasTags=False로 폴백되면 JSON 파일(`*.tags.json`) 회수해 전략 재설계.

## 2026-07-12 — 가공도 출력 신 템플릿 코드 연결 (부재명 200~204 + BOM 20행)

**유형**: feat
**커밋**: `1c19993`
**관련**: 사용자 사양 "부재명 배치 + BOM 20칸" 후속 코드 연결 (2026-07-12)

**변경 사항** (`Form1.MfgDrawing.cs`, `GenerateMfgDrawingManual`/`BuildMfgPageData`):
- 템플릿 경로 `사용자템플릿_엑셀_가공도.xlsx` → `가공도_도면_1.xlsx` (누락 오류 메시지도 동반 수정).
- `BuildMfgPageData` 슬롯 재편 — 부재명 `Input_5~9` → `Input_200~204`, BOM `Input_10~129`(8×15) → `Input_4~163`(8×20, 제작도와 동일 열별 20연속), 선초기화 1..129 → 1..204.
- BOM 한도 15 → 20 (`allMfgBomIndices.Count>20` 경고, `expectedBomRows=Min(...,20)`, snapshot 초과 문구).
- `Set2DViewTemplateMark(Logo.png)` 페이지 루프 전 1회 등록 — 신 템플릿 `{Image}`(CONTRACTOR 로고) 미치환 방지.
- 임시 카메라 ± 프로브도 선초기화 1..204 + 부재명 슬롯 200으로 정정 (태그 글자 누출 방지).
- 문서: `가공도 시트.md`(템플릿·한도·이력), `form1-mfg-drawing.md`(메서드 설명) 동기화.

**영향 범위**: 가공도 PDF 출력 전 경로(수동·자동 공통 `GenerateMfgDrawingManual`). BOM 스냅샷은 원래 전체 부재라 "전체 BOM" 요구는 한도 확대로 충족. **사내 실기 검증 대기** — 부재명 5칸/BOM 20행/CONTRACTOR 로고가 PDF에 정상 출력되는지. 북쪽 화살표(View_7/8)·CLIENT 로고(View_6)는 미배치(기존 가공도도 없었음) — 필요 시 후속.

## 2026-07-12 — 가공도 템플릿 부재명 슬롯 5칸 추가

**유형**: feat
**커밋**: `8371ae6`
**관련**: 사용자 사양 "부재명 view 왼쪽 끝에 한 칸 띄워 똑같은 간격으로, BOM표는 제작도처럼 20칸" (2026-07-12)

**변경 사항** (`가공도_도면_1.xlsx`):
- 부재명 슬롯 5개 = `Input_200~204` @ V26:CC41 / V62:CC77 / V98:CC113 / V134:CC149 / V169:CC184 — 각 60×16mm, 각 View 밴드와 동일 행(세로 정렬), View(CE~EL) 왼쪽에서 1칸(CD열) 띄움, 그림틀 좌측선(U열)에서도 1칸 여백. 폭은 View와 동일 60칸. 셀 정렬 가운데.
- BOM 표는 제작도 복제본이라 이미 20행(Input 4~163) — 추가 작업 불필요, 요구 충족 확인.
- 검증: Input 1~204 각 1회, View 1~8, Image 1개, 부재명 자리 사전 빈칸·병합 없음.

**영향 범위**: 템플릿 파일만. 코드 연결 여전히 대기 — 가공도 출력(`GenerateMfgDrawingManual`)이 이 템플릿을 읽고 `Input_200~204`에 부재명, `Input_4~163`에 전체 BOM을 채우도록 data dict 재작성 필요(현재는 구 템플릿 `사용자템플릿_엑셀_가공도.xlsx` 참조).

## 2026-07-12 — 신 가공도 템플릿(가공도_도면_1) 세로 5칸 View 슬롯 생성

**유형**: feat
**커밋**: `2d9f740`
**관련**: 사용자 요청 "가공도도 기존처럼 세로 5칸, A안으로" (2026-07-12)

**배경**: 사용자가 `제작도_도면_1.xlsx`(BOM 20행 재번호본)를 복제해 그림 구역(AE16:GL193)을 완전히 비운 `가공도_도면_1.xlsx`를 준비. 기존 가공도 템플릿 조사 결과 밴드 구분선은 원래 없음(외곽 프레임+우측 정보란 경계선뿐, View 슬롯도 상자 없음) → 신 템플릿도 선 없이 슬롯만 생성.

**변경 사항** (`가공도_도면_1.xlsx`, openpyxl 스크립트):
- 세로 5칸 View 슬롯: `View_1~5` = CE26:EL41 / CE62:EL77 / CE98:EL113 / CE134:EL149 / CE169:EL184 — 각 60×16mm, 밴드(36/36/36/35/35행) 세로 중앙·구역 가로 중앙. 구 템플릿 슬롯(약 55~58×17.5mm)과 동급이라 가공도 치수 오프셋(9/9mm) 튜닝 그대로 유효.
- 북쪽 화살표 `{View_5}`(GM11) → `{View_8}` 재번호 — 칸 5번과 인덱스 충돌 방지 (삭제 아님, 영역 보존).
- 검증: View 1~8 각 1회, Input 1~199 불변, {Image} 1개, 그림 구역 병합 = 슬롯 5개만.

**영향 범위**: 템플릿 파일만. **코드 연결은 다음 단계** — 가공도 코드는 아직 구 템플릿(`사용자템플릿_엑셀_가공도.xlsx`) 참조. 전환하려면 Input 슬롯 재편 필요(현재 태그는 제작도 의미: BOM 20행 4~163 등 / 가공도 코드는 부재명 5~9 + BOM 10~129 기대) + 우측 BOM 표 행수(5행 사양 vs 유지) 결정 대기.

## 2026-07-10 — 제작도 BOM 20행 확장 + 신 템플릿 전환

**유형**: feat
**커밋**: `a5c7621`
**관련**: 사용자 결정 "확장하자, BOM 슬롯 체계도 20행 기준으로" (2026-07-10)

**변경 사항**:
- `제작도_도면_1.xlsx` — BOM 하단 5행(16~20행)에 태그 삽입 + **전 슬롯 열별 20연속 재번호**: No=4~23, ITEM=24~43, MATERIAL=44~63, SIZE=64~83, Q'TY=84~103, T/W=104~123, MA=124~143, FA=144~163, Note=164, PAINT/DP=165~168, TAG NO=169, Rev 표=170~199. 스크립트 검증: Input 1~199 각 1회, View 1~7·{Image} 보존, 중복·누락 0.
- `Form1.DrawingSheets.cs` (`GenerateSheetDrawing2D_WithExcelTemplate`) — BOM 매핑 한도 15→20행 + 컬럼 베이스 재배열, 선초기화 1..199, 20행 초과 시 `P2 BOM n행 중 20행만 표시` 잘림 로그, **엑셀 경로를 `제작도_도면_1.xlsx`로 전환**, `Set2DViewTemplateMark(Logo.png)` 등록 추가(신 템플릿 `{Image}`=CONTRACTOR 로고 슬롯이 미등록이면 태그 글자 노출).
- 문서: `시트 2D 렌더.md`(슬롯 컨벤션·이미지 영역 표·변경 이력), `form1-drawing-sheets.md`(단계 설명 + 앵커 6곳 현행화).

**영향 범위**: 제작도 2D 출력 전 경로(수동·자동). ⚠️ 구 템플릿(`사용자템플릿_엑셀_제작도.xlsx`, 15행 체계)은 신 번호와 비호환 — 구 PoC 버튼(`btnExcelTemplatePoC`) 짝으로만 보존. 가공도는 자체 슬롯 체계라 무관.

**검증 (사내)**: 제작도 출력 1회 → ① BOM 16~20행에 값/빈칸 정상(태그 글자 노출 없어야 함) ② CONTRACTOR 위치에 Logo.png ③ 도면정보·Rev 표 위치의 미치환 태그 없음.

## 2026-07-10 — 신 제작도 템플릿(제작도_도면_1) 플레이스홀더 삽입

**유형**: feat
**커밋**: `45ca8d4`
**변경 사항**: 사용자 제작 297×210(1칸=1mm) 템플릿에 View_1~7·{Image}·Input_1~159 태그 배치(당시 BOM 4~123 15행 컨벤션, 16~20행 빈 셀) + 코드 Input 슬롯 선초기화(1..159). 템플릿 전환은 이 시점엔 대기.

## 2026-07-09 — 템플릿 JSON 사전변환 PoC (SDK 1.0.26.709 신규 API 실측, 임시)

**유형**: chore (진단 코드, 임시)
**커밋**: `702511c`
**관련**: 소프트힐스 신규 배포 (eml 2026-07-09, VIZCore3D+.NET 1.0.26.709) — 병목 ⑥ "템플릿 적용 5초" 대응

**배경**: 신규 `ConvertExcelToJson`/`ApplyTemplateFromJson`으로 엑셀 파싱을 1회로 줄일 수 있으나, **JSON에 변환 시점의 Input 데이터가 함께 구워짐**(벤더: "inputData 바뀌면 재변환 필요") — 페이지마다 값이 다른 우리 출력과 충돌. 전략(A: 고정만 굽고 가변은 얹기 / B: JSON 텍스트 치환)을 정하려면 JSON 구조·실측 시간 필요.

**변경 사항** (전부 임시, `MfgTemplateJsonPocEnabled` 스위치):
- `RunTemplateJsonPoc` — 가공도 출력 진입 시 1페이지 데이터로 ConvertExcelToJson(엑셀 옆 `.json`) → ApplyTemplateFromJson으로 별도 페이지 렌더 → `템플릿JSON검증_*.pdf` 저장. `[TplJson] Convert=/Apply=/size=` 로그.
- 페이지 루프의 `ImportExcelWithData`에 `[TplTime]` 소요 로그 (기존 방식 기준선).
- `.gitignore`에 생성물 `사용자템플릿_엑셀_*.json` 추가.
- SDK 신규 6종 API는 sdk-verifier로 시그니처 검증 완료 (XML 2026-07-09).

**검증 (사내)**: 가공도 출력 1회 → ① `[TplTime]` vs `[TplJson] Apply=` 시간 비교 ② 검증 PDF가 본 출력 1페이지와 동일한지(표·라벨·테두리·Input 값) ③ **JSON 파일 회수** — Input 값이 어떤 형태로 저장되는지 보고 전략 A/B 확정.

## 2026-07-06 — 2단 치수 텍스트 슬라이드 5mm (제작도·가공도)

**유형**: feat (치수 텍스트 배치)
**커밋**: `pending`
**관련**: 사용자 사양 — "2단(작은 치수 단)은 전부 텍스트를 오른쪽으로 도면 기준 5mm 이동"

**배경**: 작은 치수가 2단으로 승격돼도 텍스트는 짧은 치수선 중앙에 앉아 갑갑함. 사용자 사양 — 2단 치수는 텍스트를 일괄 슬라이드. 단 `SetMeasureItemDistanceTextPos`는 **치수선 방향 성분만 반영**(수직 성분은 투영·무시, 실기 확정)이라 가로 치수만 "오른쪽"이 가능하고 세로 치수는 "위"로 대체.

**변경 사항**:
- `DrawDimension`에 `textSlideModel` 파라미터(부호 포함 모델좌표) — 치수선 끝점 중점에서 측정축 방향으로 밀어 `SetMeasureItemDistanceTextPos` 호출(생성 직후·2D 변환 전). `[TextSlide]` 로그.
- **제작도**(`ShowAllDimensions`): 2단에 그려지는 치수 전부(승격 작은 치수 + 기존 2단 체인)에 종이 5mm(`Lvl2TextSlideCanvas`). 부호는 옛 시프트(v6~v12)에서 실기 검증된 뷰 매핑 재사용 — X뷰 right=+Y·up=+Z / Y뷰 right=−X·up=+Z / Z뷰 right=+X·up=+Y (음수는 'Y뷰 X축' 한 케이스뿐).
- **가공도**(`DrawMfgDimsAtScale`): Level 1(2단) 치수 전부에 종이 5mm(`MfgLvl2TextSlideCanvas`, 실측 newScale 역산). 부호는 카메라 투영 헬퍼 — 가로(길이축)=`MfgHeightToRight`, 세로(폭)=`MfgAxisUpPositive`.

**영향 범위**: 제작도·가공도 2D 출력의 2단 치수 텍스트만. 1단·전체(3단) 치수, 3D 미리보기 무영향.

**검증 (사내 실기)**: ① 2단 작은 치수 텍스트가 치수선에서 오른쪽(가로)/위(세로)로 종이 5mm 비켜 앉는지 ② 방향 반대인 뷰·부재 있는지 (`[TextSlide]` 로그로 부호 확인 — 특히 가공도는 부호 1회 교정 가능성 있음) ③ 슬라이드가 이웃 치수·보조선과 새 겹침을 만드는지.

## 2026-07-06 — 보조선 오프셋 1.5배 통일 확대 (제작도 7.5/15 · 가공도 9/18mm)

**유형**: fix (치수 텍스트 가독성)
**커밋**: `pending`
**관련**: 사용자 검증 보고 — 세로 치수 텍스트가 모델에 밀착해 보임 (가로는 적당)

**배경**: 세로 치수 텍스트는 회사 표준(SDK 강제)대로 치수선 **왼쪽 = 모델 쪽**에 앉는데, 그 틈이 1단 오프셋(제작도 5mm)뿐이라 텍스트가 모델에 붙어 보임. 세로만 키우면 가로와 통일성이 깨진다는 사용자 판단 + SDK 수정 요청은 표준(왼쪽 배치)과 충돌해 실익 없음 → **가로·세로 함께 1.5배** ("일단 조금만" — 사용자 결정).

**변경 사항**:
- 제작도: `ComputeCanvasAbsoluteOffsets` 기본값 5/5 → **7.5/7.5** (1단 7.5mm·전체 15mm)
- 가공도: `MfgCanvasBaseOff`/`MfgCanvasLvlSp` 6/6 → **9/9** (1단 9mm·전체 18mm)
- 보조선 시작 gap(2mm)·작은 치수 승격 임계는 무변경

**영향 범위**: 제작도·가공도 2D 출력 보조선/치수선 간격 전체 (통일 배율). 3D 미리보기 무영향.

**검증 (사내 실기)**: ① 세로 치수 텍스트와 모델 사이 여유 생겼는지 ② 가로·세로 간격이 통일돼 보이는지 ③ 치수선이 뷰 영역을 벗어나거나 잘리는 시트 없는지 (전체 치수선이 5mm 더 바깥으로). 부족하면 값만 재조정 (2배=10/10, 12/12 등).

## 2026-07-06 — 접합 각도 알고리즘 개정: 모서리 길이축 + 접합 평면 뷰 선택

**유형**: fix (제작도 접합 각도 표시)
**커밋**: `pending`
**관련**: 사용자 검증 보고 (사진 3건) — 실제 90° 접합이 87°로 오검출 + 각도가 꺾임이 보이는 평면(뷰)에 안 나옴

**근본 원인**:
1. **87° 오검출**: 길이축을 osnap 점군 PCA(분산 최대 방향)로 추정 — 사선(베벨) 끝단·홀 점 쏠림이 있으면 주성분이 실제 길이 방향에서 몇 도 기울어, 진짜 90° 접합이 공차(±1°) 밖으로 밀려 "직각 아님"으로 표시됨.
2. **뷰 오배치**: "두 축이 화면에 조금이라도 투영되면(≥0.05)" 아무 뷰에나 그림 — 기울어진 투영 뷰에 왜곡된 각도가 그려지고, 정작 꺾임이 실평면으로 보이는 뷰에는 안 나올 수 있었음.

**변경 사항** (`MarkNonRightAngles`):
- 길이축 = **가장 긴 모서리 방향**: osnap LINE 세그먼트를 방향별(± 동일시, 공차 cos 5°)로 길이 합산, 최대 방향 채택. PCA(공분산 멱승법) 폐기. LINE 모서리 없는 부재는 대상 제외. `[각도축]` 로그에 방향합 기록.
- 표시 뷰 = **접합 평면과 평행한 뷰 1곳**: 두 길이축의 외적(평면 법선)이 깊이축과 가장 정렬된 뷰에서만 `AddCustom3PointAngle`. 다른 뷰는 skip 로그. 두 축 평행(법선~0)이면 평면 정의 불가로 생략.

**영향 범위**: 제작도 X/Y/Z 뷰 접합 각도 표시만. 치수·풍선 무영향.

**검증 (사내 실기)**: ① 축 정렬 90° 접합에 각도가 더 이상 안 뜨는지 (사진 2 케이스) ② 진짜 기운 접합은 실값(예: 87°)으로 꺾임이 보이는 뷰에 표시되는지 (사진 3 케이스) ③ `[각도]` 로그의 3D각 vs 투영각이 최적 뷰에서 일치하는지.

## 2026-07-06 — 작은 치수 단 승격: 텍스트 시프트 폐기, 2단 승격 + 전체 3단

**유형**: feat (치수 배치 재설계 — 제작도·가공도 공통)
**커밋**: `pending`
**관련**: 사용자 사양 (사진 검증 보고 — "6" 같은 작은 치수 텍스트 처리)

**배경**: 치수가 작아 텍스트가 치수선에 안 들어가면 지금까지는 텍스트만 옆으로 시프트(`ApplyParallelTextShift` v6~v12, 이력상 방향·매칭 문제 반복)했음. 사용자 사양 — "옮기지 말고 단(段)을 올려라": 작은 치수는 치수선째 2단으로, 그 경우 전체 치수는 3단으로.

**변경 사항**:
- **제작도** (`ShowAllDimensions`): Level1 체인 중 작은 치수(뷰 최대 치수/26 이하, 뷰 max>100mm일 때만 — 옛 시프트 임계와 동일)를 `level2Offset`으로 승격. `maxLevelUsed`에 승격 반영 → 전체 치수 3단. `[SmallDimPromote]` 로그.
- **가공도** (`DrawMfgDimsAtScale`): PendingDims Level 0 중 동일 임계 이하를 Level 1로 승격 → 기존 `maxLevel` 계산이 자동으로 전체를 3단(off2)으로. `[DrawMfgDims] promoted=` 로그.
- **텍스트 시프트 폐기**: `ApplyParallelTextShift` 호출 2곳(엑셀 분기·구경로) 제거 + 함수 삭제(-146줄). 텍스트는 SDK 자동 정렬 그대로 — 승격으로 이웃 겹침이 원천 해소되므로 이동 불필요.

**영향 범위**: 제작도·가공도 2D 출력 치수 배치. 3D 미리보기(canvasScaleOverride≤0)는 승격 없음(기존 동작).

**검증 (사내 실기)**: ① 작은 치수(예: 6)가 2단에 치수선째 올라가 이웃(44 등)과 안 겹치는지 ② 그 뷰의 전체 치수가 3단으로 밀렸는지 ③ 작은 치수 없는 뷰는 기존 2단 그대로인지. 로그 `[SmallDimPromote]`/`promoted=`.

## 2026-07-03 — 제작도 보조선 시작 gap 종이 절대 2mm 통일

**유형**: fix (제작도 출력 품질)
**커밋**: `pending`
**관련**: 가공도 gap 통일(4c5dc08)의 제작도 확장 — 사용자 사양 "제작도도 모델 크기와 상관없게"

**배경**: 제작도 보조선 시작 gap이 모델좌표 고정 10mm(`ExtensionLineGap`)라 종이에서는 축척마다 들쭉날쭉 (큰 부재일수록 gap이 작게 보임). 오프셋(5/10mm)은 이미 종이 절대 목표였음.

**변경 사항**:
- `FabCanvasExtGap = 2.0f`(종이 mm) 신설 — 가공도(`MfgCanvasExtGap=2`)와 동일 정책, 값 독립 관리.
- `ShowAllDimensions`의 2D 출력 분기(canvasScaleOverride>0)에서 `2mm/estScale` 역산 → `DrawDimension`의 `extGapOverride`로 3개 레벨 호출 모두 전달. 진단 로그에 `extGap_3d` 추가.
- 3D 미리보기 경로(canvasScaleOverride≤0, 종이 배율 없음)는 기존 모델좌표 10mm 유지.

**영향 범위**: 제작도(일반 시트) 2D 출력 보조선만. 가공도는 별도 경로(이미 절대). 3D 화면 표시 무변경.

**한계**: gap도 오프셋과 같은 estScale(추정 배율) 역산이라, 추정 오차만큼의 편차는 오프셋과 동일 수준으로 존재 (완전 정확하려면 가공도식 캡처 후 실측 재설계 필요 — 별도 작업).

**검증 (사내 실기)**: 부재 크기가 다른 시트들에서 osnap-보조선 시작 간격이 종이 기준 비슷하게(약 2mm) 나오는지. DiagLog `보조선 헬퍼 ... extGap_3d=` 값 확인.

## 2026-07-03 — 가공도 카메라 ± 반영 검증 프로브 (임시)

**유형**: chore (진단 코드, 임시)
**커밋**: `pending`
**관련**: 설계 `docs/리팩토링/가공도-EA-카메라-넓은면-정규화.md` §5-1 선결 검증

**배경**: EA 카메라 '넓은 면' 정규화 설계 확정. 구현 수단이 "새 단면 캡처가 MoveCamera의 PLUS/MINUS를 반영하는가"에 갈림 (옛 은선 캡처는 무시 확정이었음) — 사내 1회 확인 필요.

**변경 사항**:
- `RunMfgCameraSignProbe`(임시) — 가공도 출력 진입 시 본 페이지들 앞에 `카메라부호검증_HHmmss.pdf` 1장 저장. 같은 부재를 위 슬롯=PLUS / 아래 슬롯=MINUS 카메라로 캡처. 홀 있는 부재 우선 선택(홀 좌우 반전이 판독 포인트). `[CamSignProbe]` DiagLog.
- `MfgCameraSignProbeEnabled` 스위치 — 검증 후 프로브째 제거 예정. 실패해도 본 출력 계속(try-catch).

**영향 범위**: 가공도 출력 시 검증 PDF 1장 추가 저장뿐 — 본 출력 페이지 무변경.

**검증 (사내)**: 두 그림 **완전 동일 → ± 무시** / **좌우 반전·선 차이 → ± 반영**. 결과에 따라 본 구현 수단 확정.

## 2026-07-03 — 가공도 보조선 gap 종이 절대 2mm 통일

**유형**: fix (가공도 출력 품질)
**커밋**: `pending`
**관련 TASK**: T-073 후속 (보조선 기준 단위 통일)

**배경**: 보조선 오프셋은 종이 절대(6/12mm, 실측 newScale 역산)인데 시작 gap만 모델좌표 고정 10mm(`ExtensionLineGap`)라, 종이에서 보이는 gap이 부재·축척마다 들쭉날쭉. 10mm는 옛 오프셋이 모델좌표 100~300mm이던 시절 기준의 잔재.

**변경 사항**:
- `DrawDimension`에 `extGapOverride` optional 파라미터 추가(모델좌표, 기본 -1 = 기존 `ExtensionLineGap` 10mm) — 제작도 호출부 무변경·무영향.
- 가공도 상수 `MfgCanvasExtGap = 2.0f`(종이 mm) 신설. `DrawMfgDimsAtScale`가 `2mm / newScale`로 역산해 전달 → 오프셋과 동일한 종이 절대 기준.
- 기존 안전장치(gap ≤ 보조선 길이의 절반) 유지. `[DrawMfgDims]` 로그에 `extGap` 추가.

**영향 범위**: 가공도 PDF 보조선만. 제작도는 기본값 경로라 무영향.

**검증 (사내 실기)**: 부재 크기가 다른 여러 행에서 osnap-보조선 시작 간격이 종이 기준 동일(2mm)한지. DiagLog `[DrawMfgDims] extGap=` 값이 `2/newScale`인지.

## 2026-07-03 — 가공도 단면 출력 전환: 은선 폐지 + 세로 치수 1단 + 뒷면 osnap 제외

**유형**: fix (가공도 출력 사양)
**커밋**: `pending`
**관련 TASK**: T-073 (가공도 EA 치수 배치) 후속 — 사용자 사양 3건

**배경**: 사용자 지시 — "오른쪽(세로) 치수는 전체 길이 2단 필요 없고, 은선 모드 필요 없이 단면만 나오면 되고, 그러면 단면 osnap만 남으니 뒤쪽 osnap 치수도 필요 없다."

**변경 사항**:
1. **세로(폭) 치수 1단만** — 1차 뷰(비길이축)·2차 뷰 폭치수 모두 `IsTotal` 생략. 4점 선별 후 세로 체인은 1구간이라 전체 치수가 같은 값으로 중복 표시되던 것 제거. 가로(길이) 축은 체인+전체 2단 유지.
2. **은선 캡처 폐지** — `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` → `Create2DViewObjectWithModelAtCanvasOrigin` (본 캡처 + probe 동일 전환, sdk-verifier로 시그니처 동일 확인·SDK 공식 예제 5곳 모두 이 버전). `DASH_LINE`(은선 점선) 렌더모드 설정 2곳 제거 (출력 후 SMOOTH 복원은 유지).
3. **뒷면 osnap 치수 제외** — `FilterHiddenLineOsnap`의 가시축 극점 복원 예외(954e4de) 폐지. 단면만 그려지므로 뒷면 점은 극점이어도 치수화하지 않음.

**영향 범위**: 가공도 PDF 출력만 (3D 미리보기는 SMOOTH 실선 경로라 무영향). 제작도 무영향.

**검증 (사내 실기)**: ① PDF 모델이 점선 은선 없이 단면 외곽선만인지 ② 오른쪽 세로 치수가 1단(중복 없음)인지 ③ EA 2번 뷰에서 뒷면 기하 치수가 사라졌는지 — **특히 ③은 옛 '2번 뷰 전체 높이 50 누락' 케이스가 이제 의도된 동작인지 사용자 확인 필요** (은선 폐지로 먼쪽 플랜지가 안 그려지면 치수도 없는 게 정합).

## 2026-07-03 — 가공도 EA 미검증 후보 검증 + 2차 정리

**유형**: refactor (죽은 코드 제거)
**커밋**: `pending`
**관련 TASK**: T-073 잔재 정리 (1차 정리 후속)

**배경**: 1차 정리 때 세션 한도로 3-렌즈 검증이 끊긴 후보들을 Opus로 직접 grep 검증. 확정 dead만 추가 제거, 나머지는 근거와 함께 보존.

**추가 제거**:
- `mfgTotalOff`(비-_m) + `maxTotalDist` 재계산 루프 — 2번 할당·읽기 0. 캡처 후 실측 offset(v2-c)로 대체된 옛 인라인 offset 잔재(`mfgTotalOff_m`는 별도 경로로 live 유지).
- `BuildMfgSceneCore`·`BuildEaSecondaryScene`의 `availW`/`availH` 파라미터 — 호출부가 값 전달하나 두 메서드 내부 참조 0. cell-fit이 `EstimateFitScaleForViewArea`로 이동한 뒤 남은 미사용 파라미터. 시그니처 + 호출부 2곳 정리.
- stale 주석 4곳: `MfgScreenRightPositive`·`ApplyAbsoluteCameraRoll`(미존재 심볼), `세로=오른쪽 조사 중`(왼쪽=회사 표준으로 종결), `btnMfgDrawing_Click`(삭제된 버튼) → 현행 심볼/정책으로 갱신.

**보존(근거)**: `isAboveWider`/`isLShape`(읽힘=live), `UsedMinusCamera`(스냅샷 판정에 읽힘), `CameraData` 스냅샷·필드(T-036 재사용 계획), `OrientationAxis`/`OrientationAngle`(8필드 설계), `alignExtToBaseline`(활성 실험 폴백), `struName`(진행 중 페이지 빌더), `zoomRatio`(내부 사용).

**검증**: MSBuild Debug 통과(에러 0). code-reference 앵커 -24 라인 드리프트 정정.

## 2026-07-03 — 가공도 EA 스틴트 죽은 코드 정리 (3-렌즈 만장일치)

**유형**: refactor (죽은 코드 제거)
**커밋**: `pending`
**관련 TASK**: T-073 (가공도 EA 치수 배치) 잔재 정리

**배경**: 2026-06-30~07-02 가공도 EA 작업(24커밋, 시행착오 다수)이 남긴 폐기 접근 잔재를 다중 에이전트 워크플로(파인더 2 + 후보별 3-렌즈 적대 검증)로 탐지. 만장일치로 확정된 7건만 제거(정밀도 우선, 미검증 후보는 보존). `isEA`/`isEA3d`/`isEAUse180`·진단 로그·제작도 공유 코드는 제외.

**제거 항목**:
- `PushMfgDimTextOutside`(가공도 치수 텍스트 역추정 수동 배치) — 호출부 0. `dc410d4` revert가 호출만 제거하고 본체를 남긴 잔재. 깨진 XML doc 포함.
- `GenerateMfgDrawing2DAll`(옛 8×3 그리드 일괄 출력) + `RenderMfgViewForDrawing`(그 셀 렌더 어댑터) — `cac4454` 재배선 후 호출부 0, 전이적 dead. `GenerateMfgDrawingManual`→`RenderMfgRowToViewArea` 경로로 일원화.
- `DrawDimension`의 `mfgTextOutwardCanvas`/`mfgCanvasScale` 파라미터 + `[DimTextOut]` 블록 — 호출부가 인자를 안 넘겨 가드 항상 false, 도달 불가.
- `MfgViewPose.MirrorAxis` 필드 — `40c60ec`가 유일 reader(치수 좌표 반전)를 제거해 write-only.
- `MfgViewPose.RequiresPostSelectionRotation` 파생 프로퍼티 — 배선된 적 없음(소비자는 `ApplyZ90`/`ApplyR180` 직접 검사).
- 관련 stale 주석(캡처 직전 텍스트 배치 등) 및 삭제 메서드 참조 doc 갱신.

**영향 범위**: 순수 죽은 코드 제거 — 사용자 대면 동작 무변경. 12 삽입 / 453 삭제. `ApplyParallelTextShift`(제작도 공유)·`MirrorVertical`·`ShapeDrawingIds`(라이브 경로) 등 살아있는 코드는 보존.

**검증**: MSBuild Debug 통과(에러 0). `PushMfgDimTextOutside`/`GenerateMfgDrawing2DAll`/`RenderMfgViewForDrawing` 활성 참조 0(tombstone 주석만 잔존).

## 2026-07-01 — 가공도 치수 텍스트: 수동 배치 제거, 제작도 자동 정렬에 위임

**유형**: fix (가공도 치수 텍스트)
**커밋**: `pending`
**관련 TASK**: T-073

**배경**: 사용자 관찰 — 제작도 텍스트는 "모델 바깥"이 아니라 **표준 관례(가로 치수=치수선 위, 세로 치수=왼쪽)**로 나온다. 이건 `AlignDistanceText=true`의 SDK 자동 정렬 결과이며 제작도는 수동 배치를 안 한다. 가공도는 그동안 수동으로 텍스트를 옮기려다(`PushMfgDimTextOutside` → `DrawDimension` 중앙 배치) **자동 정렬을 덮어써** 중앙에 앉았고, 수직 오프셋은 `SetMeasureItemDistanceTextPos`가 치수선에 투영해 무시됨(SHDC 확인).

**변경 사항**:
- 가공도 치수 텍스트 **수동 배치 제거** — `DrawMfgDimsAtScale`가 `DrawDimension`에 텍스트 오프셋 파라미터를 넘기지 않음. 제작도와 동일하게 `AlignDistanceText=true` 자동 정렬에 위임 → 가로=위, 세로=왼쪽.

**영향 범위**: 가공도 치수 텍스트만. 제작도 무영향. (`DrawDimension`의 mfg 텍스트 파라미터는 미사용 상태로 잔존 — 후속 정리)

**검증**: 가로 치수 텍스트가 치수선 위, 세로 치수 텍스트가 왼쪽으로 나오는지 (제작도와 동일).

## 2026-07-01 — 가공도 치수 텍스트 바깥 배치: 생성 시점 정확 좌표 (역추정 폐기)

**유형**: fix (가공도 치수 텍스트)
**커밋**: `pending`
**관련 TASK**: T-073

**배경**: AlignDistanceTextPosition 제거로 `SetMeasureItemDistanceTextPos`가 먹히기 시작(텍스트가 움직임)했으나, 위치가 **좌우로 밀리고** 세로 텍스트가 여전히 **모델-치수선 사이**. 사용자 지적: 두 증상이 연관됨. SHDC MCP 문서 확인 — `SetMeasureItemDistanceTextPos(reviewID, position)`의 position은 **3D 월드 절대 좌표**, `Add2DMeasureFrom3DMeasure` 이전 호출이 공식. 원인: `PushMfgDimTextOutside`가 텍스트의 치수선-방향 중심을 측정 내부점(`m.Position`)에서 **역추정**하는데 그 점들이 치수선 양끝이 아니라 중심이 어긋나 좌우로 밀렸고, 이게 세로 텍스트가 사이에 끼는 것과 동일 원인.

**변경 사항**:
- 역추정 방식(`PushMfgDimTextOutside`) 폐기. `DrawDimension`이 치수 **생성 즉시** 정확한 좌표로 텍스트 배치:
  - 치수선-방향 중심 = `startVertex`/`endVertex`(치수선 끝점) 중점.
  - 바깥 방향 = `positiveOffset`(치수선 배치와 동일한 권위값) → 치수선 너머 **캔버스 절대 6mm**(실측 `newScale` 기준).
- `DrawDimension`에 가공도 전용 파라미터(`mfgTextOutwardCanvas`·`mfgCanvasScale`) 추가 — 제작도는 기본 0이라 무영향.
- 캡처 경로에서 `ApplyParallelTextShift`·`PushMfgDimTextOutside` 호출 제거(가공도만). `[DimTextOut]` 진단 로그.

**영향 범위**: 가공도 치수 텍스트만. 제작도 무영향.

**검증**: 상단(가로)·오른쪽(세로) 치수 텍스트가 치수선 너머(바깥) 중앙에 나오는지, 좌우 밀림 없는지.

## 2026-07-01 — 가공도 치수 텍스트 바깥 배치: AlignDistanceTextPosition 제거 (제작도 일치)

**유형**: fix (가공도 치수 텍스트)
**커밋**: `pending`
**관련 TASK**: T-073

**배경**: 보조선 통일 후에도 오른쪽(세로) 치수 텍스트가 **모델-치수선 사이**에 머물고 바깥으로 안 나감(사용자 검증). SDK 문서 검증(`sdk-verifier`) + 제작도 스타일 대조 결과, `SetMeasureItemDistanceTextPos`(3D ID+Vector3D, `Add2DMeasureFrom3DMeasure` 이전) API·타이밍은 정확. 유일한 차이는 **가공도만 `MeasureStyle.AlignDistanceTextPosition = 2`(바깥쪽)를 설정** — 이 속성은 `[전체 옵션만 지원]` 제약이라 2D 출력에서 수동 좌표 지정을 무력화한 것으로 판단. 제작도(`Dimensions.cs:50~51`)는 이 속성을 폐기하고 수동 좌표만 씀.

**변경 사항**:
- 가공도 두 스타일 블록(`BuildMfgSceneCore`·`BuildEaSecondaryScene`)에서 `AlignDistanceTextPosition = 2` **제거** → 제작도와 동일하게 수동 `SetMeasureItemDistanceTextPos`만 사용.
- `PushMfgDimTextOutside`에 실측 `newScale` 전달 → 텍스트를 치수선 너머 **캔버스 절대 6mm**로 밀기(옛 `offset×0.6`은 2/4mm라 너무 작아 겹침). 배율 무관 일정.
- `[PushDimOut]` 진단 로그 추가(offAx·offVal·push·tp).

**영향 범위**: 가공도 치수 텍스트만. 제작도 무영향.

**검증**: 오른쪽(세로)·상단(가로) 치수 텍스트가 치수선 너머(바깥)로 나오는지.

## 2026-07-01 — 가공도 보조선·텍스트: 캡처 후 실측 newScale로 그리기 (구조 재설계)

**유형**: fix (가공도 출력 품질) — 직전 추정-스케일 방식 폐기·대체
**커밋**: `pending`
**관련 TASK**: T-073 / 설계: `docs/리팩토링/가공도-보조선-제작도통일.md` §4.4 v2-c

**배경**: 직전 커밋(`7c5c48f`)의 "가로 정규화"로도 보조선·텍스트가 **"그대로"**(사용자 사내 검증). 모델 굵기만 해결. 원인: 보조선 길이 = `offset × 0.75 × 실측배율`인데 `offset = 캔버스값 / 추정배율`. 추정(`EstimateFitScaleForViewArea`, BBox+fitFactor)은 **실제 2D 은선 투영 실측(`fitRatio=Min(area/objW,area/objH)`)과 원리상 달라**, W/H만 정규화해도 불일치 잔존(설계 §4.4 기술).

**변경 사항** (구조 재설계 — 설계 문서 v2-c 구현):
- 보조선·치수 **그리기**를 `BuildMfgSceneCore`·`BuildEaSecondaryScene`에서 **분리** → 데이터(그릴 목록)만 `pose.PendingDims`로 수집(offset 미적용).
- 모델 2D 캡처 + `RescaleObject` **직후** 확정된 실측 `newScale`로 `DrawMfgDimsAtScale` 호출 → `ComputeCanvasAbsoluteOffsets(newScale, 2/4mm)`로 offset 산출 후 그림. 추정 오차 0.
- 보조선 길이가 캔버스 절대 2/4mm로 **부재·뷰 무관 일정**. 텍스트 밀기(`PushMfgDimTextOutside`)도 offset 정상화로 함께 교정(같은 뿌리).
- EA 1차/2차 캡처 간 3D 측정·보조선 격리(`DrawMfgDimsAtScale` 진입 시 Clear) — 1차 치수의 2차 뷰 누출 방지.
- 직전 `normalizeLandscape`(추정 방식) 되돌림.

**영향 범위**: 가공도 PDF 라이브 경로(`RenderMfgRowToViewArea`→`CaptureMfgSceneToViewArea`)만. 제작도 무영향(`ComputeCanvasAbsoluteOffsets` 기본 5/5·`DrawDimension` 기존 호출 불변).

**검증**: ① 보조선 길이 부재·뷰 무관 일정 ② 치수 텍스트 치수선 너머(바깥) ③ 모델 굵기 유지.

## 2026-07-01 — 가공도 모델 굵기·보조선 통일·텍스트 거리 (제작도 전수 대조)

**유형**: fix (가공도 출력 품질)
**커밋**: `pending`
**관련 TASK**: T-073 (가공도 EA 치수 배치)

**배경**: 사용자 — "제작도는 모델 굵기·보조선 길이 통일·텍스트 바깥이 다 되는데 가공도만 안 된다. 전수조사해." 다중 에이전트 워크플로(제작도 작동 vs 가공도 미작동 3증상 대조 + 적대 검증)로 근본 원인 2개 확정.

**근본 원인**:
1. **모델 굵기**: 라이브 캡처(`CaptureMfgSceneToViewArea`)가 `ModelLineThickness` 미설정. 제작도(`DrawingSheets.cs:1636`)·죽은 가공도(2145)는 설정. `Set2DViewCreateObjectItemLineWidth(2.0)`는 모델 은선 두께와 별개 API라 무력. (실루엣 가설은 적대 검증으로 반증 — 제작도는 `SilhouetteEdge=true`로도 굵게 나옴)
2. **보조선 길이·텍스트 거리**(같은 뿌리): 오프셋을 `EstimateFitScaleForViewArea`(회전 전 BBox)로 추정하나, 실제 캡처는 `ProbeAndRollLandscape`가 세로 부재를 90° 회전한 가로 투영(`newScale`)으로 그림 → 회전 부재만 W/H swap으로 `estScale≠newScale` → 보조선 길이·텍스트 밀기 거리 어긋남.

**변경 사항**:
- `CaptureMfgSceneToViewArea`: `ModelLineThickness=3.0f` + `Set2DViewCreateObjectItemMeasureTextHeight(10f)` 추가 (제작도와 동일).
- `EstimateFitScaleForViewArea`: `normalizeLandscape` 옵션 추가(기본 false). true면 긴 변=W·짧은 변=H로 정규화 → 캡처(항상 landscape)와 estScale 일치. 가공도 두 호출(`BuildMfgSceneCore`·`BuildEaSecondaryScene`)만 true.

**영향 범위**: 가공도 PDF/2D 출력만. 제작도는 mfg 전용 함수 미수정 + `normalizeLandscape` 기본 false라 무영향.

**검증**: ① 모델선이 제작도처럼 굵은지 ② 부재·뷰 무관 보조선 길이 통일됐는지 ③ 치수 텍스트가 치수선 너머(바깥)로 나오는지.

## 2026-06-23 — 치수 보조선 gap 적응 (짧은 보조선 누락 해소)

**유형**: fix (치수 보조선)
**커밋**: `pending`
**관련 TASK**: T-071

**배경**: 사용자 사진 — 작은 부재(가공도)에서 한쪽(아래) 보조선이 통째로 사라짐. 원인: 보조선 시작 gap이 고정 10mm인데, 보조선 축소(가공도 2/4mm) 후 작은 부재의 osnap→치수선 거리가 10mm보다 짧아져 `OffsetTowardLineEnd`가 역전 방지로 0 길이(=안 보임)로 접음.

**변경 사항**: `DrawDimension`에서 gap을 `min(ExtensionLineGap, 보조선길이*0.5)`로 적응. 보조선이 항상 절반 이상 보이도록. 큰 오프셋(제작도)은 10mm 그대로.

**영향 범위**: 제작도·가공도 모든 치수 보조선(공용 `DrawDimension`). 큰 오프셋 불변, 짧은 오프셋만 gap 축소.

**검증**: 작은 부재 가공도에서 양쪽 보조선이 다 보이는지.

## 2026-06-23 — 가공도 EA 두 뷰 완전 밀착 + 2차 뷰 4점 + Z 회전 commit 보강

**유형**: fix (가공도 EA 레이아웃)
**커밋**: `pending`
**관련 TASK**: T-070 (후속)

**배경**: 사용자 사진 피드백 — ① EA 평면도·정면도 완전 밀착 요청 ② 1·2차 뷰 둘 다 4점 규칙 ③ Z 최장축이 여전히 세로로 박힘(재현 확정).

**변경 사항** (`RenderMfgRowToViewArea`·`BuildEaSecondaryScene`):
- **완전 밀착**: `viewGap`을 `0f`로 (기존 EA 4mm 간격 제거).
- **2차 뷰 4점**: `BuildEaSecondaryScene`에도 `FilterOsnapForDimAxis`(최장축 극점) 적용 — 1차 뷰와 동일 규칙.
- **Z 회전 commit 보강**: 두 캡처(`CaptureMfgSceneToViewArea`) 직전에 `Application.DoEvents()` + `View.GetCameraData()` 삽입. `GenerateMfgDrawingManual`의 BeginUpdate 안이라 `RotateCameraByScreenAxis(0,0,90)`이 캡처 전 미반영되던 것으로 추정 — 미리보기(`ExecuteMfgDrawing`)가 쓰는 검증 패턴.

**영향 범위**: 가공도 EA 부재 PDF/2D 출력.

**검증**: ① 두 뷰 붙었는지 ② Z 최장축이 가로로 눕는지 ③ 2차 뷰 치수도 정리됐는지.

**남은 작업(다음)**: 치수 방향(1차=위+우 / 2차=아래+우) + 1차 뷰 맨 아래 보조선 누락 — `DrawDimension` 오프셋 매핑 확인 후.

## 2026-06-23 — 가공도 osnap을 제작도 4점 알고리즘으로 통일 (치수 폭주·겹침 해소)

**유형**: refactor/fix (가공도 치수 정확도·정리)
**커밋**: `pending`
**관련 TASK**: T-070

**배경**: 사용자 사진 진단 — EA ㄱ단면 가공도에서 좌측 치수 폭주(50·33·…)·치수 대량 겹침. 원인: 가공도 전용 외곽 선별(`FilterMfgOsnapForView`)이 "같은 치수축·다른 깊이" station을 못 합쳐 중간 치수가 살아남음. 사용자 사양: "제작도 알고리즘 그대로 써라(보는 뷰의 4 극점만 + 중복·깊이겹침 제거)".

**변경 사항**:
- `BuildMfgSceneCore`의 osnap 선별을 **제작도와 동일한 `FilterOsnapForDimAxis`**(가로축 max/min + 세로축 max/min 4 극점)로 교체. 단일 부재 `nodeOsnapMap` 구성 → 가시 2축 각각 호출 → 합쳐서 `MergeCoordinates`로 중복 흡수.
- 가공도 전용 `FilterMfgOsnapForView`(약 52줄) **폐지**.
- 결과: 가공도·제작도가 **같은 알고리즘 하나로 통일**. 중간 station 폭주·치수 겹침 원천 제거. 홀 중앙점 등은 추후 이 집합에 가산.

**영향 범위**: 가공도 1차 뷰 치수 기준점. (EA 2차 뷰는 미적용 — 비대칭, 추후 결정. 비-EA 부재는 오프셋 자동방향이 새 osnap에 따라 달라질 수 있어 사내 재확인.)

**검증**: EA 가공도 출력 → 좌측 치수가 4점 기준으로 정리됐는지, 겹침 사라졌는지.

## 2026-06-23 — 도면/가공도 출력 후 3D 뷰 부드러운 음영(SMOOTH) 복원

**유형**: fix (UX)
**커밋**: `pending`
**관련 TASK**: T-069

**배경**: 치수추출·2D 도면 생성·가공도 출력·PDF 내보내기 후 3D 뷰가 은선/X-Ray 모드로 남아 불편(사용자 보고).

**변경 사항**: 액션 정상 종료 직후 `View.SetRenderMode(RenderModes.SMOOTH)` + `XRay.Enable=false` 복원 3곳 추가 —
- `GenerateSheetDrawing2D_WithExcelTemplate`(DrawingSheets.cs, 뷰 루프 종료 후)
- `GenerateMfgDrawingManual` finally(MfgDrawing.cs, EndUpdate 앞 — 격리+SMOOTH 한 번에 commit)
- `btnExportPDF_Click`(Drawing2D.cs, Export 직후). `ExecuteMfgDrawing`은 이미 SMOOTH라 변경 없음.

**영향 범위**: 3D 뷰 렌더 모드만(2D 도면 객체 무관). 출력 결과 불변.

**검증**: 각 출력 후 모델이 부드러운 음영으로 돌아오는지.

## 2026-06-23 — 홀/슬롯홀 검출 휴리스틱 전면 제거 → API 단일화

**유형**: refactor (정확도·코드 정리)
**커밋**: `pending`
**관련 TASK**: T-068

**배경**: 기존 홀 검출은 "원기둥 body를 찾아 판재에 매칭 + Osnap 파싱"하는 추측 휴리스틱이라 부정확(홀이 별도 원기둥으로 모델링 안 되면 누락). 가공도는 이미 `GetNodeHoleInfo` API로 전환했고, 사용자 지시로 BOM·제작도까지 API로 단일화.

**변경 사항**:
- `DetectHoles`(Form1.BOM.cs) 본문을 휴리스틱 → **`GetNodeHoleInfo` API**로 전면 교체. 부재별로 `GetMfgHolesFromApi` 호출해 `bom.Holes`/`SlotHoles`를 채우고, BOM 표 홀사이즈/슬롯사이즈 문자열도 API 결과로 생성.
- 휴리스틱 전용 헬퍼 `IsCompleteCircle`·`HasSlotConnectingLines` + 죽은 `GetHoleOrSlotForPoint` 제거 (Form1.BOM.cs **약 790줄 삭제**, 1593→857줄).
- `GetMfgHolesFromApi`가 가공도 풍선 + BOM/제작도 공용 단일 출처가 됨.

**영향 범위**: BOM 표 홀사이즈·가공도 EA 홀 치수점이 API 기반으로 전환. `Purpose`(EBOS 판정)·`CircleRadius`(원형) 필드는 유지.

**검증**: BOM 표 홀사이즈가 API 값으로 채워지는지. (API 일부 홀 누락은 별도 벤더 추적.)

**남은 정리**: 제작도 죽은 홀/슬롯/원형 풍선(Dimensions 740~824, 만들고 Clear됨)은 후속 삭제 예정.

## 2026-06-23 — 접합 각도 연결 판정을 Clash 표면접촉으로 (면접합 누락)

**유형**: fix
**커밋**: `pending`
**관련 TASK**: T-067

**배경**: 사용자 사내 로그(`[각도] 접합쌍=1 마킹=0`)로 진단 — 대각 브레이스 접합이 안 잡힘. 원인: 연결 판정을 osnap 끝점(부재 모서리) 3mm 근접으로 했는데, 구조 부재는 **면끼리 접합**해 중심선 osnap이 부재 폭(수십~수백 mm)만큼 벌어져 면접합을 놓침. (PCA 길이축은 로그상 정상.)

**변경 사항**:
- 연결 판정을 **간섭검사 Clash 인접(표면 접촉)** 으로 교체 — `clashList`(part 쌍)를 body 쌍으로 보고 두 부재 part가 clash하면 연결. 면접합도 포착.
- osnap 끝점 3mm 근접은 폴백으로 유지(노드 일치 joint·`clashList` 빈 경우).
- 접합점은 최근접 osnap 점쌍 중점(마커 위치 근사).
- 진단 로그 강화: 연결된 모든 쌍의 `clash=… 접합거리=… 3D각=… 직각배수=…` 출력 → 어느 쌍이 왜 제외되는지 추적.

**영향 범위**: 제작도 접합 각도만.

**검증**: `[각도] 접합쌍` 수가 늘고 대각 접합이 마킹되는지. `clash=True`로 잡히는지.

## 2026-06-23 — 접합 각도 길이축을 PCA로 수정 (대각선 버그)

**유형**: fix
**커밋**: `pending`
**관련 TASK**: T-067

**배경**: 사용자 발견 — 부재 길이축을 'osnap 최원점쌍'으로 잡으니 **박스형 부재에서 대각선이 축**이 돼 각도가 틀어졌다. (검증 워크플로우가 지적했으나 합성 단계가 '재현 없음'으로 잘못 기각한 항목.)

**변경 사항**:
- `MarkNonRightAngles`의 길이축 계산을 최원점쌍 → **PCA 주성분(분산 최대 방향)**으로 교체. 점군 공분산행렬의 최대 고유벡터를 **멱승법(32회)**으로 계산(초기값=최원점쌍 방향). 박스형도 길이방향을 정확히 잡음.
- 최원점쌍은 길이 체크·멱승법 초기값으로만 사용.
- `[각도축]` 진단 로그 추가 — 부재별 PCA 길이축 방향 출력(실측 검증용).

**영향 범위**: 제작도 접합 각도만.

**검증**: `[각도축]` 로그의 길이축이 부재 길이방향과 맞는지(대각선 아님), 각도값이 정상인지.

## 2026-06-23 — 각도 표시 = 부재-부재 접합 각도로 재설계

**유형**: fix/feat (각도 표시 방향 정정)
**커밋**: `pending`
**관련 TASK**: T-067

**배경**: 1차 구현(`MarkNonRightAngles`)은 '한 부재 내부' 모서리 각(ㄱ자 꺾임)을 표시했으나, 사용자 요구는 '서로 다른 두 부재가 수직·수평으로 만나지 않을 때'의 접합 각도였다. 호출 비활성(`0aa3ca1`) 후 접합 각도 로직으로 통째 교체.

**변경 사항**:
- `MarkNonRightAngles` 본문 교체: 부재별 osnap 점군 → 최원점쌍=길이축, 판형(`IsPadOrPlateFromSpref`) 제외 → 부재쌍 osnap 끝점 3mm 근접=접합 → 길이축 **실제 3D 사잇각** → 90의 배수(±1°) 제외 → `AddCustom3PointAngle(접합점, A방향점, B방향점)`.
- 연결성·접합점은 osnap 근접 자체 판정(간섭검사 `clashList` 세션 상태 비의존 — 더 견고).
- 깊이축 평행 뷰 생략, ID 단위 스타일, `[각도]` 로그에 3D각·투영각 병기(실측 비교).
- 설계·검증 각각 워크플로우. 적대 검증이 '90배수 필터 갭'(리뷰 오판)·PCA/octree(과잉수정)를 기각 → 코드 변경 없이 커밋.

**영향 범위**: 제작도 X/Y/Z 2D 출력 각도만.

**검증**: 비직각 접합 모델 1개를 X/Y/Z로 출력 → (a) 90/180에서 안 뜨는지 (b) 화면 각도값 ↔ `[각도]` 로그(3D각/투영각) 일치하는 쪽 (c) 인자 순서(예각 모델) (d) 프레임 코너 라벨 겹침.

## 2026-06-23 — 제작도 비-90° 각도 표시 추가 (1차, 부재 내부 각 — 폐기됨)

**유형**: feat (제작도 각도 어노테이션)
**커밋**: `f70cefa`
**관련 TASK**: T-067 (비-90° 각도 표시)

**배경**: 사용자 3개 요청 중 두 번째 — 제작도에서 90°가 아닌 모서리에 각도를 표시. 곡면 Round(업체 요청)·가공도 홀(API 전환) 뒤로 미뤄둔 기능. 사양: 화면 2D 투영 각도, 90의 배수(90/180/270/360) 제외, 제작도 X/Y/Z 전체 뷰.

**변경 사항**:
- `MarkNonRightAngles(memberIndices, viewDirection)` 신규 (Form1.Dimensions.cs) — LINE osnap 세그먼트를 **3D 꼭짓점**으로 묶어 화면 평면 투영 사잇각 계산 → 90배수(±1° 공차) 제외 → `AddCustom3PointAngle`로 표시. 정수 도·파랑, ID 단위 `SetStyle`. 꼭짓점당 최대 3개, 방향·끝점 중복 억제.
- 호출: `GenerateSheetDrawing2D_WithExcelTemplate` per-view X/Y/Z 분기, `ShowAllDimensions` 직후 → 같은 `Review.Measure`→2D 파이프라인 탑승.
- `ApplyParallelTextShift`에 각도(`RK_MEASURE_ANGLE`/`SURFACE_ANGLE`) 제외 가드 (각도는 두 점 거리 시프트 부적합).
- 워크플로우 적대적 검증 반영: 꼭짓점 2D키→3D키(화면상 겹치지만 안 만나는 모서리의 허상 각 방지), 전역 스타일→ID 단위 스타일.

**영향 범위**: 제작도 X/Y/Z 2D 출력만. 가공도·ISO·BOM 무관.

**검증**: 사내 제작도 출력 → (a) 90/180/270/360에서 각도 안 뜨는지 (b) 비직각 각도값이 화면 투영과 일치 (c) **명백한 예각 한 곳으로 인자 순서 확정**(45°로 나오면 정상, 135°면 인자 회전). `[각도]` 로그로 마킹 수 확인.

**열린 결정**: 내부 형상선 각도까지 표시할지(현재 전부 표시, 사용자 "다 표현" 사양). 난잡하면 외곽 실루엣 한정 추가.

## 2026-06-23 — 가공도 보조선 오프셋 축소 (가공도 전용)

**유형**: feat (가공도 치수 보조선 길이)
**커밋**: `7816bff`
**관련 TASK**: T-066 (가공도 보조선 축소)

**배경**: 가공도 보조선이 길어 치수선이 모델에서 멀었다. 제작도와 공용 헬퍼(5/10mm)를 써서 가공도만 줄이려면 분리 필요. 사용자 사양: 가공도만, 종이 기준 1단 2·전체 4mm.

**변경 사항**:
- `ComputeCanvasAbsoluteOffsets`에 `canvasBase`/`canvasLvl` 인자 추가 (기본 5/5 — 제작도 불변)
- 가공도 전용 상수 `MfgCanvasBaseOff`/`MfgCanvasLvlSp`(2/2) 추가, 가공도 두 호출(`BuildMfgSceneCore`·EA 두번째 뷰)이 전달 → 종이 1단 2·전체 4mm
- 보조선 시작 gap(10mm)·역산 식은 그대로, 길이 값만 가공도 축소

**영향 범위**: 가공도 2D 출력 보조선만. 제작도·3D 미리보기(모델좌표 50/100mm 경로)·BOM 무관.

**검증**: 사내 가공도 PDF → 치수선이 모델에 더 붙는지. 너무 붙어 시작 gap(10mm)과 겹치면 값 상향.

## 2026-06-23 — 가공도 osnap 뷰 외곽 선별 추가

**유형**: feat (가공도 치수 기준점 정확도)
**커밋**: `f8a292b`
**관련 TASK**: T-065 (가공도 osnap 뷰 선별)

**배경**: 가공도는 모든 osnap을 체인 치수로 이은 뒤 결과 치수만 솎아, 깊이·뒷면으로 투영되는 점까지 치수 기준점이 됐다. 제작도가 `FilterOsnapForDimAxis`로 뷰별 외곽점을 먼저 추리는 단계가 가공도엔 빠져 있었다. 사용자 사양: "그 뷰의 외곽 osnap만" (외곽 코너 + 홀).

**변경 사항**:
- `FilterMfgOsnapForView` 추가 — osnap을 카메라 화면 평면(가로·세로)으로 투영해 각 열·행 극점(상·하·좌·우)만 남겨 실루엣 외곽 구성. 내부·깊이 투영점 제외. 오목 모서리(ㄱ자 안쪽·노치) 보존.
- `BuildMfgSceneCore` 5-3 단계로 삽입 (은선 필터 직후, 체인 치수 직전)
- 선별 2단 구조 명문화: ① osnap 외곽(`FilterMfgOsnapForView`) → ② 치수 솎기(`FilterMfgDimensions`)
- 홀 중심을 치수 기준점으로 추가하는 건 분리 (홀 추출이 흐름 뒤쪽·슬롯 실측 중)

**영향 범위**: 가공도 미리보기·PDF 치수만. 제작도·BOM·설치도 무관.

**검증**: 사내 가공도 출력 → 치수 기준점이 그 뷰 외곽 코너에만 찍히는지, 깊이/뒷면 점이 빠졌는지. `[MfgOsnapView]` 로그로 선별 전후 점 개수 확인.

## 2026-06-15 — 제작도 방향 이미지 {Input_N} 경로 주입 방식 (테스트)

**유형**: feat (이미지 배치 방식 전환, 실측 테스트)
**커밋**: `87f5965`
**관련 TASK**: — (템플릿에 여러 이미지 넣기, 사용자 지시)

**배경**: North_Arrow/ISO_North_Arrow를 `{View_5/7}` 영역 + `PlaceImageInTemplateArea`로 배치하던 것을, 사용자가 엑셀에서 `{View_5/6/7}` → `{Input_124/125/126}`으로 전환. `{Input_N}` 슬롯에 이미지 경로를 주입하면 SDK가 이미지로 렌더하는지(`WriteCellData` remarks: "값이 이미지 경로면 {Image}로 처리") 검증.

**변경 사항**:
- `GenerateSheetDrawing2D_WithExcelTemplate`의 `data`에 `data[124]=North_Arrow.png`, `data[126]=ISO_North_Arrow.png` 경로 주입 (`ResolveDrawingAssetPath`, 다중 PC 호환)
- `ImportExcelWithData`가 처리 → Input 슬롯에 이미지 렌더 기대
- 기존 `PlaceImageInTemplateArea(View_5/7)`는 View 태그 사라져 `area==null`로 자동 무력화(null 안전 확인)
- 엑셀 템플릿(`사용자템플릿_엑셀_제작도.xlsx`)도 사용자가 View→Input 전환 (코드와 짝)

**검증**: 사내 제작도 출력 → North_Arrow/ISO_North_Arrow가 `{Input_124/126}` 칸에 나오는지. 나오면 `{Input_N}` 이미지 방식 확정 → 추가 이미지도 이 방식으로.

## 2026-06-15 — 가공도 홀/슬롯홀 풍선 GetNodeHoleInfo API 전환 (가공도 한정, 실측 중)

**유형**: feat (진행 중 — 슬롯 매핑 실측 대기)
**커밋**: `b2f23c6`
**관련 TASK**: — (가공도 홀/슬롯홀을 API로 골라내 풍선 표현, 사용자 지시)

**배경**: 현재 홀 추출(`DetectHoles`, 휴리스틱 565줄)이 부정확. 사용자 지시로 가공도 풍선을 SDK `GetNodeHoleInfo` API로 전환. `bom.Holes`/`SlotHoles`는 제작도·BOM표가 써서 통째 비활성화 불가 → **가공도 한정**으로만 API 사용(제작도·BOM 보호).

**변경 사항**:
- 가공도 전용 `GetMfgHolesFromApi(nodeIndex)` 신설 — `vizcore3d.GeometryUtility.GetNodeHoleInfo` → `NodeHoleItem` 매핑
- 가공도 메인 풍선(`BuildMfgSceneCore`)이 `bom.Holes` 대신 API 결과 사용
- NodeHoleItem 실제 타입 빌드 역추론 확정: **Center=Vector3D, CircleCenter=List<Vector3D>, Size=Vector3D, Radius=float**
- CIRCLE 홀: Center·Radius 정확 매핑. SLOT_HOLE: 잠정 매핑(중심=Center, 길이/폭=Size 최대/최소축, Depth=0) + **실측 로그**(`[홀API]` NodeHoleItem 원본 값)

**영향 범위**: `A2Z/Form1.MfgDrawing.cs` 가공도 메인 풍선만. 제작도·BOM표·가공도 osnap(EA 보조뷰)은 `bom.Holes` 유지.

**검증**: 사내 가공도 출력 → `[홀API]` 로그로 NodeHoleItem 실제 값 확인 → 슬롯 매핑(Size 의미·Depth) 보정.

**미해결**: ① 슬롯 SlotLength/Depth 정확 매핑(실측 후) ② Center가 홀 중심인지 확인 ③ 가공도 osnap·거리계산은 아직 `bom.Holes`.

## 2026-06-15 — 가공도 치수 선별을 전용 진입점으로 분리

**유형**: refactor (가공도 선별 규칙 독립화)
**커밋**: `ffea9ff`
**관련 TASK**: — (가공도는 규칙을 따로 만들 수 있어야, 사용자 지시)

**배경**: 직전(`200c81e`)은 가공도가 제작도 `ApplySmartFiltering`을 같은 파라미터로 직접 호출 → 제작도 규칙과 묶여 가공도만 따로 조정 불가.

**변경 사항**:
- 가공도 전용 진입점 `FilterMfgDimensions` + 전용 파라미터(`MfgMaxDimensionsPerAxis=8`, `MfgMinTextSpace=25`) 신설
- `BuildMfgSceneCore`가 `ApplySmartFiltering` 직접 호출 → `FilterMfgDimensions` 호출로 교체
- 동작·파라미터는 현재 동일(제작도 알고리즘 차용). 향후 가공도 고유 규칙(외곽 우선·특정 Osnap 보존 등)은 이 진입점에서 독립 발전
- 제작도 `ApplySmartFiltering` 미변경 (규칙 독립)

**영향 범위**: `A2Z/Form1.MfgDrawing.cs` 구조만. 동작 불변. `가공도 단일.md` 갱신.

## 2026-06-15 — 가공도 치수 점 선별 (제작도 ApplySmartFiltering 적용)

**유형**: feat (가공도 치수 선별)
**커밋**: `200c81e`
**관련 TASK**: — (가공도도 제작도처럼 점 선별, 사용자 지시 / 후속: 가공도 전용 추가 점 살리기 예정)

**배경**: 가공도는 Osnap 노이즈 제거(CIRCLE 제외·은선 필터·0.5mm 좌표병합)만 하고, 제작도의 `ApplySmartFiltering`(겹침·우선순위 선별 + 축당 개수 제한)을 안 거쳐 사실상 "남은 Osnap 점을 다 뽑는" 상태였음. 형상이 복잡한 부재는 치수 과다·겹침 발생.

**변경 사항**:
- `BuildMfgSceneCore` 치수 생성(`AddChainDimensionByAxis`) 직후 `ApplySmartFiltering(mfgDimensions, maxDimensionsPerAxis: 8, minTextSpace: 25.0f)` 추가 — 제작도와 동일 파라미터
- 가공도 그리기는 이미 3패스(`IsVisible` + `DisplayLevel==0` / `>0` / `IsTotal`) 구조라 필터 결과 자동 반영 — **그리기 로직 변경 없음**
- `ApplySmartFiltering` 내부 `AssignDimensionPriorities`가 Priority 할당 → `mfgDimensions`에 별도 작업 불필요

**영향 범위**: `A2Z/Form1.MfgDrawing.cs` 가공도 치수 선별만. 제작도·일반시트 무관. 풍선 끝단 계산(`allMfgDims`)은 별도 경로라 미변경(후속 검토 가능).

**검증**: 사내 가공도 출력 → 치수가 축당 8개로 선별 + 겹침 회피되는지 확인.

## 2026-06-15 — 제작도 고정 이미지 미표시 수정

**유형**: fix
**커밋**: `pending`
**관련 TASK**: — (제작도 North Arrow 이미지 미표시, 사용자 직접 요청)

**변경 사항**:
- `North_Arrow.png`, `ISO_North_Arrow.png`, `Logo.png`를 빌드 출력 폴더에 자동 복사
- 실행 폴더 우선 절대경로와 솔루션 루트 fallback을 제공하는 이미지 경로 해석 추가
- 클릭 입력을 기다리는 `Set2DViewCreateObjectWithImage` 경로 제거
- `TemplateTableData + RenderTemplate`로 이미지 영역 중앙에 직접 렌더링
- 원본 이미지 종횡비와 View 영역 크기를 기준으로 출력 높이 계산

**영향 범위**: 제작도 엑셀 템플릿의 North Arrow 및 ISO North Arrow 렌더링. 모델 4면도·치수·풍선 로직은 변경 없음.

**검증**: Debug 빌드 오류 0건, 이미지 3종의 `A2Z\bin\Debug` 자동 복사 확인. 실제 VIZ 2D 출력 확인 필요.

## 2026-06-15 — ISO North Arrow 배치 위치 정정

**유형**: fix
**커밋**: `pending`
**관련 TASK**: — (제작도 템플릿 이미지 위치 정정, 사용자 직접 요청)

**변경 사항**:
- 기존 `B2:D5`의 미지원 `{Image_4}` 태그를 이미지 영역용 `{View_7}`로 교체
- `ISO_North_Arrow.png` 매핑을 `View_6`에서 `View_7`로 이동
- `AO38:AW41`의 `View_6`은 향후 다른 이미지가 준비될 때 사용할 예약 영역으로 유지

**영향 범위**: 제작도 엑셀 템플릿의 ISO North Arrow 위치. 일반 North Arrow와 모델 4면도 출력은 변경 없음.

**검증**: 템플릿 `B2={View_7}`, `AO38={View_6}` 및 병합 영역 유지 확인. Debug 빌드 후 사내 출력 확인 필요.

## 2026-06-15 — 제작도 North Arrow 이미지 2종 자동 배치

**유형**: feat
**커밋**: `pending`
**관련 TASK**: — (제작도 엑셀 템플릿 다중 이미지 배치, 사용자 직접 요청)

**변경 사항**:
- 엑셀 SDK가 인식하지 않는 `{Image_3}`, `{Image_2}`를 이미지 영역용 `{View_5}`, `{View_6}`로 교체
- `View_5`에 `North_Arrow.png`, `View_6`에 `ISO_North_Arrow.png`를 배치
- 새 2D 이미지 객체를 찾아 영역 크기에 종횡비 유지 fit 후 중앙 이동하는 `PlaceImageInTemplateArea` 추가
- 기존 임시 `Logo.png` 이미지 실측 블록 제거
- 파일·영역 누락 또는 SDK 자동 생성 실패 시 로그를 남기고 도면 출력은 계속

**영향 범위**: 제작도 엑셀 템플릿 기반 2D 출력의 고정 이미지 영역. 모델 4면도·치수·풍선 로직은 변경 없음.

**검증**: Debug 빌드 오류 0건, 템플릿 `AM2={View_5}` / `AO38={View_6}` 및 병합 영역 렌더 확인. 실제 SDK 이미지 자동 생성·배치는 사내 출력 확인 필요.

## 2026-06-15 — 가공도 형상 풍선을 세 종류로 제한

**유형**: fix
**커밋**: `pending`
**관련 TASK**: T-044 (홀 풍선 제작도 비표시), T-047 (SlotHole·Hole 가공도 반영)

**변경 사항**:
- 가공도 풍선을 Hole, SlotHole, UDA `PURPOSE=EBOS`인 EarthBoss로 제한
- 원형 부재의 반지름 풍선을 가공도에서 제거
- 일반/선택 X/Y/Z 뷰와 2D 제작도에서 Hole, SlotHole, EarthBoss 형상 풍선을 비표시
- ISO 부재번호 풍선은 기존 별도 로직으로 유지

**영향 범위**: 가공도 시트 3D 미리보기, 가공도 PDF, 일반 축 뷰, 제작도 2D 출력

## 2026-06-15 — EA 가공도 상하 2뷰와 길이축 치수 분리 복원

**유형**: feat
**커밋**: `PENDING`
**관련 TASK**: T-048 (EA 가공도 T자형 잘못 촬영 수정)

**변경 사항**:
- `SPREF` ITEM이 EA로 시작하는 부재의 카메라 열린 방향 보정을 다시 활성화
- 엑셀 가공도 템플릿의 한 행을 위·아래로 나눠 정면 계열 뷰와 상면 계열 뷰를 함께 출력
- 첫 번째 뷰는 단면 방향 치수, 두 번째 뷰는 최장축 체인·전체 치수를 담당하도록 분리
- 두 번째 뷰는 독립 `Z_MINUS` 카메라를 사용하고, 최장축이 Z인 경우만 `X_MINUS + 화면축 90도` 적용
- 과거 일부 EA 부재를 T자형으로 만들던 추가 정렬 회전은 제외
- 두 번째 뷰 캡처 실패 시 불완전 객체를 삭제하고 첫 번째 뷰는 유지

**검증**:
- `dotnet build A2Z/A2Z.csproj -c Debug -nologo` 오류 0건
- VIZCore3D 모델을 사용하는 PDF 방향과 치수 위치는 사내 실기 검증 필요

## 2026-06-11 — 이미지 배치 동작 실측 코드 (도면 다중 이미지 구현 준비)

**유형**: chore (실측 로그, 임시)
**커밋**: `6746549`
**관련 TASK**: — (도면에 서로 다른 고정 이미지 여러 장 배치, 사용자 제기)

**배경**: 도면에 회사 로고·선급 마크 등 서로 다른 고정 이미지 여러 장을 넣어야 함. `{Image}` 태그는 1개·단일 로고용(`Set2DViewTemplateMark`은 일반/반전 두 버전일 뿐)이라 부적합. 엑셀 직접 삽입은 ImportExcel이 안 가져옴(사용자 확인). → `Set2DViewCreateObjectWithImage` 코드 배치가 답이나, 위치/크기/생성동작 등 6항목이 SDK 문서에 없어 실측 필요.

**변경 사항**:
- 이전 BOM 객체 열거 진단(`897b19d`) → 이미지 배치 실측으로 교체
- `ImportExcelWithData` 직후 `Logo.png` 1장을 `Set2DViewCreateObjectWithImage`로 생성 → `GetObjectAllinfoBy2DView`/`GetObjectCenter`/`GetObjectSize`로 ID·Kind·위치·크기 로그 + `MoveObjectTo(100,100)`·`RescaleObject(0.5)` 동작 확인
- 실측 항목: ①자동생성 여부 ②초기 위치/크기 ③Index/ID ④MoveObjectTo 앵커 ⑤Rescale 의미 ⑥크기 단위
- sdk-verifier 좌표계 확정: **mm, 원점 좌하단, Y-up** (엑셀 `{View}` 영역과 동일 계)

**영향 범위**: `A2Z/Form1.DrawingSheets.cs` 실측 로그 + 출력물에 테스트 로고 1장(임시). 확정 후 제거.

**검증**: 사내 출력 → `logs/diag-*.log`의 `[이미지실측]` 라인 확인.

## 2026-06-11 — 도면 2D 객체 열거 진단 로그 추가 (BOM 빈 행·Note 후처리 조사)

**유형**: chore (진단 로그, 임시)
**커밋**: `897b19d`
**관련 TASK**: — (BOM 빈 행 테두리 제거 + Note 조건부 표시, 사용자 제기 / 진단 단계)

**배경**: 엑셀 템플릿 도면 출력에서 (1) BOM 데이터가 표 15행보다 적어도 빈 행 테두리가 남고, (2) 내용 없는 `Note:` 라벨이 항상 표시됨. 원인은 `ImportExcelWithData`가 엑셀 고정 격자/라벨을 그대로 그리기 때문(제작도 템플릿 엑셀 분석으로 확인: BOM 표 = 엑셀 3~17행 고정 격자, Note = `AN24` 정적 셀, 입력 슬롯 없음). 후처리(2D 객체 삭제/숨김)로 해결 가능한지 판정하려면 그려진 결과물이 개별 2D 객체로 열거되는지 실측 필요.

**변경 사항**:
- `GenerateSheetDrawing2D_WithExcelTemplate`의 `ImportExcelWithData` 직후, `GetObjectAllinfoBy2DView()`로 캔버스 2D 객체를 열거해 각 객체의 ID/Index/Kind/Show/Text를 진단 로그로 출력
- 최대 400개 + 초과 생략, 예외 안전 처리
- 로그 전용 — 출력 결과물 불변, 판정 후 제거 예정

**영향 범위**: `A2Z/Form1.DrawingSheets.cs` `ImportExcelWithData` 직후 로깅만. 도면 출력 결과 동일.

**검증**: 사내에서 제작도 1장 출력 → `logs/diag-{날짜}.log`의 `[진단]` 라인 확인 (빈 행 선·`Note:` 텍스트 포착 여부 + Kind/Text 값).

## 2026-06-11 — 치수 보조선 죽은 코드 제거 (짧은 축 절반 로직)

**유형**: refactor
**커밋**: `b188108`
**관련 TASK**: — (사용자 즉석 정리 지시, T-038/T-039 잔재 코드)

**배경**: 과거에 넣었던 "한쪽 축이 다른 쪽의 1/3 이하로 짧으면 그 축 보조선을 절반으로" 로직(`axisShortHalf`)이, 짧은 축을 판정해 목록에 넣는 코드가 사라져 조건문만 껍데기로 남음. `axisShortHalf`가 항상 비어 `Contains`가 늘 false → 절반 분기는 한 번도 실행되지 않는 죽은 코드. 현재는 모든 축이 동일 5/10mm 적용.

**변경 사항**:
- `ShowAllDimensions`에서 `axisShortHalf` 선언 + 사용 5곳 제거 (`canvasHOff`/`canvasVOff` 절반 토글, `lv1`/`lv2`/`lv0` 절반 토글)
- 항상 else였던 조건문을 기존 결과값으로 단순화, 중간 변수 `lv1`/`lv2`/`lv0` 제거하고 offset(`level1/2/0Offset`) 직접 전달
- 무의미해진 설명 주석 정리
- 동작 100% 불변 (행동 보존 리팩토링), 빌드 오류 0개

**영향 범위**: `A2Z/Form1.Dimensions.cs` `ShowAllDimensions` 보조선 offset 계산부만. 3D 뷰·2D 출력 결과 동일. 풍선·치수 계산 로직 무관.

## 2026-06-03 — 가공도 보조선 길이 제작도와 통일 (공용 헬퍼, 추정 1차)

**유형**: refactor (가공도 보조선 길이 정책 통일)
**커밋**: `1a7e7e8`
**관련 TASK**: T-039 (가공도 적용 잔여)
**관련 계획서**: `docs/리팩토링/가공도-보조선-제작도통일.md` v2 (Codex 1차 검토 반영)

**문제**: 제작도는 보조선 화면 길이가 일정(캔버스 절대 5/10mm ÷ 추정배율)한데, 가공도는 모델좌표 고정값(50/100/250 × offFactor)이라 부재마다 들쭉날쭉. 세로 전체 길이 치수도 안 맞음.

**변경 사항**:
- 보조선 길이 계산을 공용 헬퍼 `ComputeCanvasAbsoluteOffsets(scale)`로 추출 — 제작도·가공도 단일 정책 (`Form1.Dimensions.cs`, `1aba8c7`)
- 제작도 `ShowAllDimensions` 인라인 5/10mm 계산을 헬퍼 호출로 교체 (동작 100% 보존)
- 가공도 `BuildMfgSceneCore(bomIndex, availW, availH)` — 2D 출력 경로(availW>0)는 캔버스 절대 5/10mm를 추정 fit scale로 역산. `mfgChainOff1/2/total`을 제작도 level1/2/0 구조와 1:1 매핑
- `EstimateFitScaleForViewArea`에 `fitFactorOverride` 인자 — 가공도는 1.0 (칸 100% 채움), 제작도는 기존 0.65/0.70
- 3D 미리보기 경로(`ExecuteMfgDrawing` 등 availW<=0)는 기존 offFactor 유지 — 회귀 없음

**방식**: Codex 1차 검토 후 "추정 1차". 회전(Z90) 부재는 추정 오차 가능 — 사내 검증으로 충분성 판정, 부족 시 실측 newScale 2차.

**영향 범위**: `A2Z/Form1.Dimensions.cs`(헬퍼), `Form1.MfgDrawing.cs`(보조선 분기), `Form1.DrawingSheets.cs`(fitFactor 인자). 풍선·EA·MULTI·`:1693` 범위 외.

## 2026-05-23 — P3 사용자 보고 #3 진단 2차: Osnap·DrawDimension DiagLog 추가

**유형**: debug (가공도 흐름 재배선 P3 검증 3차 — 진단용 로그 2차)
**커밋**: `9165e4f`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**진단 컨텍스트** (2026-05-23):
사용자 사진 1번 부재 BBox 확인 결과:
- Size X = 65 mm, Size Y = 65 mm, Size Z = 1050.09 mm
- **모델 좌표는 mm 단위 맞음** ("× 0.5 가설" 틀림)

그런데 로그상 measure 추출:
- Z축 measure estDist = 525 (부재 가로 1050의 **절반**)
- X축 measure 합 = 32.5 (= 29.5 + 3, 부재 X 65의 **절반**)

→ **measure가 부재 양쪽 외곽을 못 잡고 한쪽~중간 부분만 측정**.
→ 사용자 보고 #2 ("전체 치수 안 나옴")와 같은 뿌리 — Osnap 외곽 부족.

**변경**:
- `A2Z/Form1.MfgDrawing.cs` `BuildMfgSceneCore` Osnap 수집 직후:
  - 부재 BBox·rawOsnap 종류별 카운트 (LINE/POINT/CIRCLE) 로그
  - 수집된 Osnap 점 좌표 범위(min/max) vs 부재 BBox 비교 로그
- `A2Z/Form1.Dimensions.cs` `DrawDimension`:
  - axis·dist·startPoint·endPoint·startVertex·endVertex 좌표 로그
  - measure가 부재 어느 점에서 어느 점까지 측정하는지 추적

**효과**:
- 핸들러 흐름 변경 없음 — 로그만 추가
- 사내 빌드 + 가공도 출력 → Osnap이 부재 외곽 어디까지 잡혔는지, measure 좌표가 어디인지 정확히 추적 가능

**다음 (사용자)**:
- A2Z 종료 + git pull + 빌드
- 가공도 출력 (사진 1번 부재)
- 로그 `[Osnap]`, `[DrawDimension]` 키워드 검색해서 보고

---

## 2026-05-23 — P3 사용자 보고 #3 진단: ApplyParallelTextShift DiagLog 추가

**유형**: debug (가공도 흐름 재배선 P3 검증 3차 — 진단용 로그)
**커밋**: `3fe5138`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**사용자 보고** (2026-05-23):
> 사진 1번에서 부재 두께 6mm와 부분 길이 59mm가 같은 좌표에 겹쳐 그려짐 (chain dimension 65 = 6 + 59).
> "6이 원래 치수가 없는 쪽으로 이동해야 하는데 59 쪽으로 이동한 이유를 못 찾는 거지?"

**진단 분석 (코드 정적)**:
- threshold = maxEstDist/26 ≈ 40mm
- 6mm는 시프트 대상, 59mm는 threshold 초과로 skip
- 6의 인접 측정이 한쪽만 있으면 코드 로직대로 반대 방향(빈 쪽)으로 가야 정상
- 사용자가 본 결과는 반대 → 후보:
  - D. dimCenter 계산 실제와 다름 (mp0/mp1 좌표 순서·부재 좌표계)
  - E. shiftDir → 화면 방향 매핑 오류 (viewDirection × dimAxis switch)
  - F. modelShift 거리 너무 작아 시각적으로 안 보임
  - G. SDK 디폴트 텍스트 위치 자체가 잘못

**변경** (`A2Z/Form1.Dimensions.cs` `ApplyParallelTextShift`):
- 함수 진입부에 BEGIN 로그 (view, canvasScale, modelShift, threshold, maxEstDist, infoCount)
- 각 측정의 dimAxis·dimCenter·estDist·textPos 로그
- axisGrp별 sorted 순서 로그
- 각 측정의 시프트 결정 로그 (leftDist, rightDist, shiftDir, reason)
- 시프트 후 좌표 로그 (shifted vs original)
- END 로그

**효과**:
- 핸들러 흐름 변경 없음 — 진단용 로그만 추가
- 사내 빌드 + 가공도 출력 → 로그에서 어떤 측정이 어디로 시프트되었는지 정확히 추적 가능
- 진단 끝나면 로그 제거 또는 축약 예정

**다음 (사용자)**:
- A2Z 프로세스 종료 후 사내 재빌드
- 가공도 출력 (사진 1번 케이스)
- DiagLog 결과 보고 — 어떤 측정의 shiftDir·reason·shifted 좌표
- 그 결과로 4가지 후보(D/E/F/G) 좁힘 → 정확한 패치

---

## 2026-05-23 — P3 사용자 보고 #2: 출력 누르자마자 다른 부재 보임 → BeginUpdate/EndUpdate 잠금

**유형**: fix (가공도 흐름 재배선 P3 검증 2차 피드백)
**커밋**: `12590bd`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**사용자 보고** (2026-05-23):
> "지금보니까 가공도 출력 누르자 마자 다른 부재들이 보이네"

**원인**:
`GenerateMfgDrawingManual` 진입부 8단계의 `vizcore3d.Object3D.Show(Object3DKind.ALL, true)`가 BOM 정보 수집을 위해 모든 부재를 표시. 그 후 row마다 `BuildMfgSceneCore`가 `Show(ALL, false)` + `Show(target, true)`로 격리하지만, 화면 update가 안 막혀 있어 중간 상태(모든 부재 표시·격리 깜빡임)가 사용자에게 노출.

**변경** (`A2Z/Form1.MfgDrawing.cs` `GenerateMfgDrawingManual`):
- 진입부 try 직전에 `vizcore3d.BeginUpdate()` 추가
- finally 마지막(가시성 복원 후)에 `vizcore3d.EndUpdate()` 추가
- 효과: 화면 update가 EndUpdate까지 차단 → 사용자에겐 출력 끝나고 격리 복원된 최종 상태만 한 번에 보임
- PDF export(Export2PDFBy2DView)는 BeginUpdate 영향 없음 (화면 update와 별개, 파일 저장 직접)
- MessageBox도 별 윈도우라 영향 없음

**효과**:
- 빌드 green
- 출력 버튼 누름 → 화면 그대로 유지 → 완료 메시지박스 뜸 → 닫으면 선택 시트 격리 상태 (P3 #1 패치 결합)

**docs**:
- `docs/기능/가공도/가공도 시트.md` last_updated 갱신

**다음 (P3 잔여)**:
- 사용자 사내 빌드 + 재검증 (출력 누르자마자 화면 안 바뀌는지)
- 부재 크기 기준 결정 (사용자 보류 — 80% / 70% / margin / 엑셀 수정 중 택1)

---

## 2026-05-23 — P3 사용자 보고 #1: 출력 후 모든 부재 표시 → 선택 시트 격리 복원

**유형**: fix (가공도 흐름 재배선 P3 검증 1차 피드백)
**커밋**: `a9bbfc2`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**사용자 보고** (2026-05-23):
> "가공도 출력 누르면 다른 부재들이 X-Ray인지 뭔지 모르겠는데 3D View에 표시되는데 이거 확인좀 해줄래?"
> 시점: 출력이 끝난 후 (완료 메시지박스 닫은 다음)

**원인**:
`GenerateMfgDrawingManual` finally의 `RestoreAllPartsVisibility()`가 출력 끝나면 **모든 부재를 다시 표시**. 사용자가 출력 전 가공도 시트 미리보기로 그 부재만 격리해서 보던 상태였는데, 출력 후 격리가 깨지고 다른 부재들도 보임 → "X-Ray 같은 효과"로 느낌.

**변경** (`A2Z/Form1.MfgDrawing.cs` `GenerateMfgDrawingManual` finally):
- 옛: `try { RestoreAllPartsVisibility(); } catch { }` 무조건 호출
- 새: 선택 시트 있으면 그 부재만 격리 복원(`Show(ALL, false)` + `Show(previousSelected.MemberIndices, true)`), 없으면 `RestoreAll` 폴백
- 사용자가 출력 전 보던 미리보기 격리 상태가 출력 후에도 유지됨

**효과**:
- 빌드 green
- 출력 전 가공도 시트 미리보기 → 출력 → **출력 후에도 그 부재만 격리되어 보임** (사용자 의도)
- 출력 전 시트 선택 없었으면 RestoreAll (옛 동작 유지 — 폴백)

**docs**:
- `docs/기능/가공도/가공도 시트.md` last_updated 갱신

**다음**:
- 사용자 사내 빌드 + 재검증 (가공도 출력 → 완료 확인 → 다른 부재 안 보이는지)
- 사용자가 보고한 2번 항목 (메시지 잘림) 처리 예정

---

## 2026-05-23 — P2-integrate: GenerateMfgDrawingManual + btnMfgDrawingSheet_Click 재배선

**유형**: refactor (가공도 흐름 재배선 2단계 3/3 — P2 완료)
**커밋**: `cac4454`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**변경** (`A2Z/Form1.MfgDrawing.cs`):
- **`GenerateMfgDrawingManual` 통합 본체** 신설 (~180줄):
  - 시그니처: `(List<DrawingSheetData> mfgSheets, string saveDir, string struName, int struIndex = 0) → MfgDrawingResult`
  - try/finally 범위: 진입부 8단계 + UI 잠금 + BOM 채움 + snapshot + 페이지 루프 모두 보호 (Codex 6차 #1)
  - 진입부 강제 초기화 8단계: Note/Measure/ShapeDrawing.Clear + Clear2DView + XRay off + Show ALL + DESELECT + DASH_LINE
  - BOM 표 1회 채움: 가공도 전체 부재로 합성 sheet → `CollectBOMInfo(false, syntheticSheet)` → `SnapshotBomRows()` (사용자 사양: 모든 페이지 동일 BOM)
  - 15행 초과 / BOM snapshot mismatch / surplus → `result.Warnings`에 누적 (함수 내부 MessageBox 없음)
  - 페이지 루프: `ResetCanvasForMfgPage` + `BuildMfgPageData(snapshot)` + `ImportExcelWithData` + `EnsureViewAreasCache` + `RenderMfgRowToViewArea` × 5 + `Export2PDFBy2DView`
  - 모든 row 실패 시 export skip
  - finally: BOM UI 복원 (선택 시트 재호출) + UI 잠금 해제 + `RestoreAllPartsVisibility`
- **`btnMfgDrawingSheet_Click` 재배선**:
  - P1 임시 no-op 본문 제거
  - 가공도 시트 수집 → `GenerateDefaultDrawingSaveDir()` → `GenerateMfgDrawingManual` 호출
  - 결과 받아 단일 MessageBox 통합 (Codex 6차 권고):
    - 템플릿 누락 → 오류 메시지박스 + return
    - 정상: PDF 개수 + BOM 부족 카운트 (조건부) + Warnings 목록 (조건부) 합산해서 1회 표시

**효과**:
- 호출자 grep: `GenerateMfgDrawingManual` → 1건 (btnMfgDrawingSheet_Click) ✅
- 빌드 green (Debug, warning 5건 사라짐)
- **수동 가공도 출력 활성화** — 사내 검증(P3) 진입 가능
- 옛 `GenerateMfgDrawing2DAll` + `RenderMfgViewForDrawing` 본체 여전히 dead (P4b 삭제 예정)
- 자동(`ProcessSingleStruFull` §8, `ExportAllSheetsToPdfCore`)은 P1 hard skip 상태 유지 (P4a에서 같은 함수 호출로 재배선)

**docs**:
- `docs/기능/가공도/가공도 시트.md` — P1 임시 비활성 박스 → P2-integrate 완료 박스 (last_updated 갱신)

**다음 단계 (P3)**:
- 사용자 사내 검증: 14개 합격 기준 (5행/페이지·BOM 표·치수·풍선·페이지 분할·잔재 없음·파일명 충돌 없음·EA fallback·BOM 부족 알림·UI 잠금 등)
- 합격 시 P4a (자동 재배선) → P4b (dead 코드 삭제)

---

## 2026-05-23 — P2-row: RenderMfgRowToViewArea 신설

**유형**: refactor (가공도 흐름 재배선 2단계 2/3)
**커밋**: `4762e2e`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7

**변경** (`A2Z/Form1.MfgDrawing.cs`):
- `RenderMfgRowToViewArea(int rowIdx, BOMData bom, TemplateViewArea area) → bool` 신설
- 위치: `MakeUniquePdfPath` 끝 직후, `BuildMfgSceneCore` 정의 전
- 패턴 (Codex 1~7차 누적 반영):
  - 진입·종료 Note/Measure/ShapeDrawing.Clear (row 단위 cleanup)
  - `BuildMfgSceneCore(bom.Index)` 호출 (지역 pose, `_lastMfgViewPose` write 금지)
  - DASH_LINE + SilhouetteEdge + FlyToObject3d + Z90/R180 적용
  - EA 부재 시 P5 전 single-view fallback 마커 DiagLog
  - 2D 캡처 (`Create2DViewObjectWithModelHiddenLineAtCanvasOrigin`)
  - fit guard 확장: area·objW/H·fitRatio·curScale·newScale 모두 NaN/Infinity 가드
  - 절대 좌표 배치 (`MoveObjectTo(area.X+W/2, area.Y+H/2)`)
  - Shape/Note/Measure 각각 try/catch WARN (사용자 사양: 일부 실패해도 row 성공)
  - finally: partial 실패 시 `DeleteObjectBy2DView(objId)` + row 종료 cleanup

**효과**:
- 호출자 0건 (P2-integrate에서 활성화 예정)
- 빌드 green (warning 5건 동일, P2-integrate에서 해소)

**다음 단계**:
- **P2-integrate**: `GenerateMfgDrawingManual` 통합 본체 + `btnMfgDrawingSheet_Click` 단일 메시지박스 재배선

---

## 2026-05-23 — P2-helpers: 가공도 수동 새 함수용 헬퍼 일괄 추가

**유형**: refactor (가공도 흐름 재배선 2단계 1/3)
**커밋**: `efeb7de`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v7 (Codex 1~7차 검토 통과)

**배경**:
P1(`d8682b4`)에서 자동·수동 진입점 hard skip 후, P2에서 수동 새 함수 `GenerateMfgDrawingManual` 작성. P2를 3 commit으로 분할:
- **P2-helpers (본 commit)**: 헬퍼 함수 일괄 + 결과 객체 클래스
- P2-row: `RenderMfgRowToViewArea` 행 1개 렌더
- P2-integrate: `GenerateMfgDrawingManual` 통합 + `btnMfgDrawingSheet_Click` 재배선

**변경** (`A2Z/Form1.MfgDrawing.cs`):
- `MfgPage` 클래스 주석 갱신 (PoC 4행 → v7 5행)
- `SplitMfgIntoPages`: `rowsPerPage` 기본값 4 → **5**
- `BuildMfgPageData` 시그니처 + 본문 전면 갱신:
  - 사용자 사양 반영: BOM 표·도면정보 = 제작도 방식 (`CollectBOMInfo` + `lvDrawingBOMInfo`)
  - 매개변수에 `List<string[]> bomSnapshot` 추가
  - 빈 슬롯 선초기화 `data[1..129] = ""` (미치환 `{Input_N}` 노출 방지)
  - 좌측 5행 BOM 이름: `Input_5~9`
  - 우측 BOM 표 8컬럼 × 15행: `Input_10~129` (NO/ITEM/MATERIAL/SIZE/Q'TY/T/W/MA/FA × 15)
- 신규 helper 6개:
  - `MfgDrawingResult` private sealed class (Codex 6차 권고: 결과 객체 패턴) — SuccessPdfs / InsufficientBomPdfs / TemplateMissing / BomRows / ExpectedBomRows / Warnings / HasIssues
  - `SnapshotBomRows()`: `lvDrawingBOMInfo`에서 BOM 행 1회 복사 (UI race 차단)
  - `EnsureViewAreasCache(ref dict, xlsxPath)`: SDK `GetViewAreasFromExcel` 결과를 int 키 dict로 1회 캐시, View_1~5 검증 후 대입 (invalid cache 잔존 차단)
  - `ResetCanvasForMfgPage()`: 페이지 진입 시 Clear2DView + SetCanvasSize(297, 210) + SetSelectCanvas(1)
  - `GetDefaultDrawingSaveDir()`: 자동·수동 공통 `Application.StartupPath/Drawings`
  - `MakeUniquePdfPath(...)`: `SanitizeFileName` + 40자 clamp + `yyyyMMdd_HHmmss_fff` + struIndex + 충돌 시 `_N` suffix + MAX_PATH 240 임박 경고

**효과**:
- 호출자 0건 (P2-row, P2-integrate에서 활성화 예정)
- 빌드 green (Debug, warning 5건 — `MfgDrawingResult` 필드 미할당, P2-integrate에서 해소)
- 옛 `GenerateMfgDrawing2DAll` + `RenderMfgViewForDrawing` 본체는 여전히 dead 격리 (P4b 삭제)

**다음 단계**:
- **P2-row**: `RenderMfgRowToViewArea` — `BuildMfgSceneCore` 호출 + DASH_LINE + FlyTo + Z90/R180 + 2D 캡처 + fit guard (NaN·Infinity·newScale 포함) + Shape/Note/Measure 각각 try/catch + finally `DeleteObjectBy2DView` cleanup
- P2-integrate: 통합 본체 + 버튼 재배선
- Codex 7차 검토 통과 (차단급 0건, MED 1건 BOM surplus는 일반 사용에서 발생 X — 사용자 결정으로 v7 유지)

---

## 2026-05-23 — P1: btnMfgDrawing 폐기 + 자동·수동 가공도 진입점 임시 hard skip

**유형**: refactor (가공도 흐름 재배선 1단계)
**커밋**: `d8682b4`
**관련 계획서**: `docs/리팩토링/가공도-수동우선-재배선.md` v3 (Codex 1~3차 견제 통과)
**관련 TASK**: T-064 (도면리스트 뽑기 후속), feedback_mfg_balloon_2026-05-19

**배경** (사용자 사양, 2026-05-22):
> "자동 가공도 로직은 일단 폐기하고 수동으로 가공도 그리고 수동이 잘 나오면 그 수동 로직 함수 그대로 자동에서 호출"
> "작업/데이터 탭에서의 버튼들 다 없애줘"

**변경**:
- **UI 제거** (`A2Z/Form1.Designer.cs`):
  - 작업/데이터 탭 `panelBOMButtons`의 `btnMfgDrawing` 컨트롤 4곳(필드·선언·Controls.Add·정의 블록) 제거
  - 같은 패널에 남은 `btnBalloonAdjust` 위치를 (6,4)로 이동, TabIndex 0
- **핸들러 제거** (`A2Z/Form1.MfgDrawing.cs:19-31`):
  - `btnMfgDrawing_Click` 메서드 통째로 제거. `ExecuteMfgDrawing` 함수 본체는 `LvDrawingSheet_SelectedIndexChanged` 미리보기에서 그대로 유지
- **큰 버튼 임시 no-op** (`A2Z/Form1.MfgDrawing.cs`):
  - 도면정보 탭 `btnMfgDrawingSheet_Click` 본문을 `DiagLog + MessageBox("재설계 중")`으로 교체. 옛 `GenerateMfgDrawing2DAll(mfgSheets)` 호출 폐기
- **자동 진입점 hard skip** (Codex 1차 #1 — Export2PDFBy2DView 잔존 방지):
  - `A2Z/Form1.Stru.cs §8` (`ProcessSingleStruFull` 가공도 묶음): 옛 `GenerateMfgDrawing2DAll` + `Export2PDFBy2DView` 블록 전체 제거 → `mfgSkipCount` 집계 + DiagLog만. `pdfCount` 증가 X
  - `A2Z/Form1.DrawingSheets.cs:1149-1218` (`ExportAllSheetsToPdfCore` group=false): 가공도 시트를 `lvi.Selected = true` 트리거 이전에 hard skip + 옛 if/else 가공도 분기 제거. selection 이벤트가 `ExecuteMfgDrawing`을 우발 호출하는 경로 차단 (Codex 1차 #2)
  - `A2Z/Form1.DrawingSheets.cs:1220-1273` (group=true 가공도 묶음): 전체 블록을 `mfgSkipCount` DiagLog로 교체. `successCount` 증가 X
- **docs 갱신**:
  - `docs/기능/가공도/가공도 단일.md` — DEPRECATED 박스 + `last_updated`
  - `docs/기능/가공도/가공도 시트.md` — P1 임시 비활성 박스 + `last_updated`

**효과**:
- 옛 가공도 함수 호출자 0건 확인 ✅
  - `rg "GenerateMfgDrawing2DAll" A2Z/` → 정의 1곳 + 내부 호출 1곳 (모두 dead path)
  - `rg "RenderMfgViewForDrawing" A2Z/` → 정의 1곳 + 내부 호출 1곳 (dead path)
  - `rg "btnMfgDrawing_Click" A2Z/` → 0건
- 빌드 green ✅ (Debug)
- 자동 STRU 일괄 출력 + ALL PDF 출력에서 가공도 부분만 누락 (의도됨, 검증 사이클 동안)
- 옛 `GenerateMfgDrawing2DAll` (174줄) + `RenderMfgViewForDrawing` (132줄) 본체는 dead로 격리. P4b에서 일괄 삭제 예정

**영향 범위**:
- 코드: 4개 파일, 6 hunks (167 deletions, 57 insertions, 순감 110줄)
- UI: 작업/데이터 탭 가공도 버튼 사라짐. 도면정보 탭 큰 가공도 버튼은 "재설계 중" 메시지 표시
- 검증 경로: 자동 PDF에 가공도 누락 OK (사용자 사전 결정)

**다음 단계**:
- P2-helpers / P2-row / P2-integrate: 수동 새 함수 `GenerateMfgDrawingManual` 작성 (엑셀 템플릿 5행/페이지 + PDF 직접 출력)
- v4 마이크로 패치: Codex 3차 지적 5건(POSSTART/POSEND 실제 코드, EnsureViewAreasCache, BuildMfgPageData 빈 슬롯 초기화, RenderMfgRowToViewArea partial cleanup, curScale Infinity guard) 계획서 반영 — P2 진입 전 완료
- Codex 4차 검토: v4 패치 후
- P4 자동 재배선 시점에서 dead 코드 일괄 삭제

---

## 2026-05-14 — T-064 4차: 가공도 엑셀 템플릿 롤백 (옛 외곽 + table2 복귀)

**유형**: revert (가공도 엑셀 템플릿 적용 폐기)
**커밋**: `pending`
**관련 TASK**: T-064 (도면리스트 뽑기)

**사용자 사양** (2026-05-14):
> "가공도 그냥 템플릿 안쓰는 버전으로 되돌리자"

**변경**:
- [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) `GenerateMfgDrawing2DAll`의 엑셀 import 블록을 옛 외곽 + table2로 복귀:
  - 옛 (2차 `27aae40`에서 도입): `CollectBOMInfo(false)` + `ImportExcelWithData(사용자템플릿_엑셀_가공도.xlsx, mfgData)` + 파일 없을 시 fallback 분기
  - 새 (롤백): `AddGridStructure(1, 1, w, h)` + `SetMargins(10, 10, 10, 10)` + `CreateTemplateBorder()` → `bInfo` 반환 + `TemplateTableData table2` (5행×4컬럼: 작성 일자/소속/담당자/검수자/Image) + `table2.X = bInfo.MaxX`, `table2.Y = bInfo.MinY` Anchor → `RenderTemplate(table2)`

**유지되는 다른 변경 (롤백 대상 아님)**:
- 모델 크기 30% (`contentW/H * 0.30`)
- 라벨 오른쪽 이동 + 폭 25→40mm + IsTextWrapped=true
- 캔버스 리셋 (`RemoveCanvasBy2DView` 진입 시 호출)
- 그리드 4행 (`gridRows=4`, `usableRowStart=1`)
- 풍선 비활성화 (`Note.Clear()` + `noteIds.Clear()` + `eaNoteIds.Clear()`)
- 치수 텍스트 8mm
- 홀 Osnap CIRCLE 제외
- EA 회전 비활성화 (`isEA=false`, `isEA3d=false`)
- 가공도 보조선 50% 축소
- PDF 저장 경로 고정 (`Application.StartupPath/Drawings`)

**영향 범위**:
- 코드: `A2Z/Form1.MfgDrawing.cs` (엑셀 import 블록 → 옛 외곽 + table2 복귀)
- 문서: `docs/기능/가공도/가공도 시트.md` 변경 이력
- 자원: `사용자템플릿_엑셀_가공도.xlsx` git 추적 유지 (재사용 가능성)

**검증 흐름** (R12):
- 사내 PC 빌드 → 도면리스트 뽑기 → 가공도 PDF 확인:
  - 외곽 테두리 + 우측 하단 도면정보 테이블(작성일자/소속/담당자/검수자/Logo)이 옛 방식대로 표시되는지
  - BOM 표가 *없음* (엑셀 슬롯이 사라졌으므로) — 사용자 설계상 별도 표시 방식 결정 필요
  - 다른 변경(모델 30%, 라벨 오른쪽, ISO/Looking 잔존 해결 등)은 영향 없이 유지되는지

**잔여 / 후속**:
- 가공도 BOM 표시 방식 — 사용자 결정 (별도 GridStructure 셀에 RenderTemplateOnGridStructure 또는 엑셀 부분 적용 재시도)
- 5번 (BOM 라벨 칸 초과 — 본 라운드 라벨 변경으로 부분 해결)
- 6번 (재실행 초기화) 보류 유지

---

## 2026-05-14 — T-064 3차 fine-tune: 가공도 모델 30% + 라벨 오른쪽 + 캔버스 리셋 강화

**유형**: fix (사용자 검증 보고 fine-tune)
**커밋**: `1cfa193`
**관련 TASK**: T-064 (도면리스트 뽑기)

**사용자 사양** (2026-05-14):
1. *"라벨 폭을 늘리던가 오른쪽으로 이동해야할 거 같고"*
2. *"가공도 BOM이 안채워지고 있어"*
3. *"가공도 모델도 크기 키워도 될 거 같고"*
4. *"가공도에 ISO LOOKING Z X Z 라벨이 뜨는데 이건 가공도에는 없어야 돼"*

**변경 3종 (1·3·4 — BOM은 진단 대기)**:

1. **라벨 오른쪽 이동 + 폭 확대 + 줄바꿈** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) `GenerateMfgDrawing2DAll` 모델 배치 루프
   - 옛: `labelCol = modelGroup*2+1` (왼쪽), `modelCol = modelGroup*2+2` (오른쪽)
   - 새: `modelCol = modelGroup*2+1` (왼쪽), `labelCol = modelGroup*2+2` (오른쪽) — swap
   - `ColumnWidths {0, 25} → {0, 40}` (15mm 확대)
   - `IsTextWrapped false → true` (긴 BOM 이름 줄바꿈)

3. **모델 크기 4% → 30%** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) `RenderMfgViewForDrawing` L1728~1729
   - `targetW = contentW * 0.04f` → `* 0.30f`
   - `targetH = contentH * 0.04f` → `* 0.30f`
   - 옛 4% (너무 작음) → 30% (셀 컨텐츠 30% 차지)

4. **캔버스 리셋 강화 (ISO/Looking 잔존 제거 시도)** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) `GenerateMfgDrawing2DAll` 캔버스 설정부
   - 진입 시 `RemoveCanvasBy2DView()` 호출 추가
   - 직후 `SetCanvasSize(297, 210)` 재호출
   - **가설**: 메인 도면(`GenerateSheetDrawing2D_WithExcelTemplate`)이 그리드 셀에 그렸던 ISO/Looking Z/X/Y 라벨이 가공도 진입 시 캔버스 잔존. `Clear2DView`만으론 그리드 셀 데이터 잔존 가능성 → 캔버스 자체 새로 만들어 강제 클리어

**2. BOM 안 채워짐 — 진단 데이터 대기**:
- DiagLog `T-064 가공도 엑셀 템플릿 적용 — BOM N행`이 이미 출력 중 — 사용자에게 N값 + PDF 시각 확인 요청
- 가설 후보:
  - (A) `lvDrawingBOMInfo`가 빈 결과 (요약행만) — `CollectBOMInfo(false)`가 가공도 묶음 출력 시 시트 컨텍스트 없이 호출되어 0건 가능
  - (B) `mfgData` 정상 매핑 + 엑셀 슬롯 위치 정상이지만 `AddGridStructure(4,6)`이 `ImportExcelWithData` 결과를 덮어씀 (Plan 위험 항목)
- 검증 후 다음 라운드에서 (A)면 가공도 BOM 별도 수집 / (B)면 `ImportExcelWithData` 호출 위치를 모델 배치 후 마지막으로 이동

**영향 범위**:
- 코드: `A2Z/Form1.MfgDrawing.cs` (3곳: labelCol/modelCol swap + label table 속성, RenderMfgViewForDrawing 모델 fit factor, GenerateMfgDrawing2DAll 캔버스 리셋)
- 문서: `docs/기능/가공도/가공도 시트.md` 변경 이력

**검증 흐름** (R12):
- 사내 PC 빌드 → 도면리스트 뽑기 → 가공도 PDF 확인:
  - 모델이 셀 안에서 적당히 크게 보이는지 (옛 대비 7.5배)
  - 라벨이 모델 오른쪽에 표시되고 긴 이름은 줄바꿈으로 처리되는지
  - ISO/Looking Z/X/Y 라벨이 사라졌는지 (RemoveCanvasBy2DView 효과)
  - BOM 표가 채워졌는지 — **DiagLog `BOM N행` 값과 함께 보고 부탁** (가설 좁히기 위해)

**잔여 / 후속 결정**:
- 2번 BOM 진단 결과 후 다음 라운드
- 6번 (재실행 초기화) 보류 유지
- 모델 30%가 너무 크거나 작으면 fine-tune

---

## 2026-05-14 — T-064 2차 정비: PDF 경로 고정 + EA 회전 제거 + 가공도 엑셀 템플릿 + 상단 여백 제거

**유형**: feat + fix (가공도 통합 정비 2차, Plan 에이전트 토론 후)
**커밋**: `27aae40`
**관련 TASK**: T-064 (도면리스트 뽑기)

**사용자 사양** (2026-05-14):
1. *"도면 리스트 뽑기 누르면 Release나 DEBUG에 도면 생성되게 물어보지 말고 고정"*
2. *"가공도에서는 edge도 일반 부재처럼 일단 그냥 90도 두번 찍지말고 바로 보여주자"*
3. *"가공도 상단에 여백이 있는 장치가 들어가 있는 거 같은데 그거 줄이거나 없애야하고"*
4. *"가공도템플릿 사용자템플릿_엑셀_가공도.xlsx 이거 사용하면 될 거 같아... 배치는 지금 가공도랑 똑같이 해주고 템플릿만 사용 추가하는거야"*
5. *"BOM 이름이 길면 정해진 텍스트 칸을 넘어가서 이건 어떻게 할 수 있을지 생각해보자"* — 권고안만 (별도 결정)
6. *"도면 리스트 뽑기를 두 번 못누르는 거 같아..."* — 보류

**변경 4종** (1~4):

1. **PDF 저장 경로 고정** — [Form1.Stru.cs](A2Z/Form1.Stru.cs) `btnExtractDrawingList_Click`:
   - `FolderBrowserDialog` 제거
   - `string saveDir = Path.Combine(Application.StartupPath, "Drawings");` + `Directory.CreateDirectory` 자동
   - `Application.StartupPath` = 실행 중인 exe 위치 = `bin\Debug` 또는 `bin\Release` (.NET Framework 4.8 표준)

2. **EA(앵글) 회전 비활성화** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) 2변수:
   - `ExecuteMfgDrawing` `bool isEA3d = IsAngleFromSpref(bom.Index)` → `bool isEA3d = false`
   - `RenderMfgViewForDrawing` `bool isEA = IsAngleFromSpref(bom.Index)` → `bool isEA = false`
   - 효과: EA 카메라 MINUS/180° 보정 블록 + EA 신규뷰(두 번째 뷰) 모두 dead. 일반 부재처럼 viewDirection + ApplyOrientationRotation 자연 흐름

3. **그리드 5행 → 4행 (상단 여백 제거)** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) L689~693:
   - `gridRows 5 → 4`, `usableRowStart 2 → 1`, `usableRowEnd 5 → 4`, `rowsPerCol 4`(자동)
   - 효과: Row 1 빈 행 제거 → 상단 여백 사라짐, 부재 수 12 유지

4. **가공도 엑셀 템플릿 적용** — [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) L676~714:
   - 옛 외곽 1×1 그리드 + `CreateTemplateBorder` + `table2`(우측 하단 도면정보) **모두 제거**
   - 새 흐름: `CollectBOMInfo(false)` → `data[1..123]` 구성 → `ImportExcelWithData(사용자템플릿_엑셀_가공도.xlsx, data)` → `SetSelectCanvas(1)`
   - BOM 매핑은 제작도(`Form1.DrawingSheets.cs:1731~1749`) 패턴 그대로 — `{Input_1~3}` 도면정보 + `{Input_4~123}` 8컬럼×15행 BOM
   - 모델 배치 그리드 `AddGridStructure(4, 6)` 그대로 — 엑셀 import 후 *그 위에* 덮어쓰기
   - 엑셀 파일 없으면 옛 외곽 fallback (DiagLog 경고)
   - 엑셀 파일 `사용자템플릿_엑셀_가공도.xlsx` git 신규 추적

**5번 BOM 이름 라벨 칸 초과 — Plan 권고안 (별도 결정)**:
- 5가지 옵션 분석. **추천**: 옵션 (a) `labelTable.IsTextWrapped = false → true` + 임계 길이 초과 시 폰트 축소
- 코드 변경 최소, 정보 손실 없음, 행 높이가 늘어나면 같은 row 모델 셀도 함께 늘어나 균형 유지
- **별도 결정 후 적용 예정** (이번 커밋 미포함)

**6번 도면 리스트 뽑기 재실행 초기화 — 보류** (사용자 *"나중에 수정하자"*)

**영향 범위**:
- 코드: `A2Z/Form1.Stru.cs` (PDF 경로) + `A2Z/Form1.MfgDrawing.cs` (4곳: isEA3d, isEA, 그리드 4행, 엑셀 import 블록)
- 문서: `docs/기능/가공도/가공도 시트.md` 변경 이력
- 자원: `사용자템플릿_엑셀_가공도.xlsx` git 신규 추적

**검증 흐름** (R12):
- 사용자 사내 PC 빌드 → 도면리스트 뽑기 → 자동 저장 경로(`bin/Debug/Drawings/`) 확인
- 가공도 PDF 시각 검증:
  - 외곽 + BOM 테이블 + 도면정보(프로젝트명/선박/도면종류)가 엑셀 템플릿대로 표시되는지
  - EA 부재가 일반 부재처럼 한 방향만 표시되는지 (두 번째 뷰 없음)
  - 상단 여백이 줄어 모델 셀이 캔버스 상단까지 차오르는지
  - 한 페이지 12개 부재 배치 그대로

**잔여 위험 / 후속 결정**:
- (4) `AddGridStructure(4,6)`가 `ImportExcelWithData` 후에 정상 그리드 셀 생성하는지 사내 PC 1회 확인 — 실패 시 P3 dead code 패턴(엑셀 `{View_N}` 슬롯 + `GetViewAreasFromExcel`)으로 전환 (엑셀 파일에 `{View_N}` 슬롯 신규 추가 필요)
- (1) PDF 누적 폴더가 너무 커지면 `bin/Debug/Drawings/{yyyy-MM-dd}/` 일자 분리 권고
- (5) BOM 라벨 처리 사용자 결정 후 별도 커밋

---

## 2026-05-14 — T-064 가공도 정비: 풍선 비활성화 + 치수 텍스트 키움 + 홀 Osnap 제외 + 그리드 4행

**유형**: fix (가공도 사용자 사양 4종 통합 정비)
**커밋**: `718e534`
**관련 TASK**: T-064 (도면리스트 뽑기 가공도 fine-tune)

**사용자 사양** (2026-05-14):
> "가공도에서 풍선 뺴주고 텍스트 키워줘, 가공도에 홀이 있는 부재가 있으면 홀 주변의 Osnap이 모두 치수재는대 사용돼서 엄청 많은 치수가 생기는데 이것도 수정이 필요할 거 같고, 그리드도 4개의 행 으로 바꾸자"

**변경 4종** (모두 `A2Z/Form1.MfgDrawing.cs`):

1. **PDF 풍선 비활성화** — 가공도 출력 PDF에서 풍선(반지름·홀·슬롯홀) 빼기
   - `RenderMfgViewForDrawing` L1717 직후 `noteIds.Clear()` 1줄 추가 → 가공도 메인 시트 풍선 무효화
   - EA 신규뷰 L2094 직후 `eaNoteIds.Clear()` 1줄 추가 → EA 두 번째 뷰 풍선 무효화
   - `ExecuteMfgDrawing` 슬롯홀 풍선 블록 끝(L548 직후) `vizcore3d.Review.Note.Clear()` 1줄 추가 → 3D 미리보기 풍선도 비활성화
   - **복귀**: 세 Clear() 줄 주석 처리만 하면 풍선 즉시 복구

2. **치수 텍스트 크기 키움** — `GenerateMfgDrawing2DAll` L703 `MeasureTextHeight 5f → 8f` — 풍선 빠진 자리 가독성 보강

3. **홀 Osnap 제외** — `GetOsnapPoint` 결과에서 `OsnapKind.CIRCLE` 케이스 별도 분리해 `break` (POINT/LINE만 치수 추출 사용)
   - `ExecuteMfgDrawing` L162 부근 (단일 부재 가공도)
   - `RenderMfgViewForDrawing` L1060 부근 (시트 묶음 가공도)
   - 효과: 홀이 많은 부재(예: 플레이트 N개 홀)의 치수가 *홀당 N개* 폭주 → POINT·LINE Osnap 기반 부재 외곽 치수만 남음. 홀 정보는 별도 표시 (현재 풍선 비활성화로 BOM에서 확인)

4. **그리드 8행 → 5행** — `GenerateMfgDrawing2DAll` L673~678
   - `gridRows 8 → 5`
   - `usableRowEnd 7 → 5` (사용 행 6 → 4)
   - `rowsPerCol`은 계산식 (`end - start + 1`)이라 자동 6 → 4
   - `maxSlots = rowsPerCol × 3 = 18 → 12` 자동 변경
   - 효과: 한 페이지 부재 18→12로 줄여 각 부재 셀 면적 확대 (작은 부재 더 잘 보임)

**영향 범위**:
- 코드: `A2Z/Form1.MfgDrawing.cs` (~10줄 변경, 3D Note.Clear / noteIds.Clear / eaNoteIds.Clear / OsnapKind.CIRCLE 분리 2곳 / 그리드 4상수 / 치수 텍스트 1상수)
- 문서: `docs/기능/가공도/가공도 시트.md` + `docs/기능/가공도/가공도 단일.md` 변경 이력 추가, last_updated 갱신

**검증 흐름** (R12):
- 사용자 사내 PC 빌드 → 도면리스트 뽑기 → 가공도 PDF 확인:
  - 풍선이 모두 사라졌는지 (반지름·홀·슬롯홀 모두)
  - 치수 텍스트가 옛 5mm → 8mm로 확대됐는지 (가독성)
  - 홀이 많은 부재의 치수 개수가 적절히 줄었는지 (POINT/LINE Osnap만)
  - 한 페이지에 부재 12개씩 배치 + 각 부재 셀 면적이 옛 18개 분할 대비 1.5배 커졌는지

**잔여 후속**:
- 사용자 검증 후 그리드 행수 fine-tune 가능 (5행이 너무 적으면 6, 너무 많으면 4)
- 치수 텍스트 8mm가 셀 영역 대비 적정 여부
- 홀 Osnap 완전 제외가 정합한지 — 일부 홀에 치수 필요한 경우 별도 분기 검토

---

## 2026-05-14 — T-064 fine-tune: ISO 풍선 단축 + Y뷰 추가 오른쪽 + 가공도 보조선 50% 축소

**유형**: fix (사용자 검증 fine-tune 1차)
**커밋**: `74dd643`
**관련 TASK**: T-064 (도면리스트 뽑기 fine-tune), T-039 (가공도 보조선 통일 — 잔여 작업 일부)

**사용자 사양** (2026-05-14):
1. *"ISO 풍선 더 짧아도 되겠다"*
2. *"Y뷰는 오른쪽으로 더 옮겨주고"*
3. *"가공도 보조선 길이 전부 다 제작도랑 똑같이 맞춰줘"*

**변경**:

1. **ISO 풍선 거리 단축** ([Form1.DrawingSheets.cs](A2Z/Form1.DrawingSheets.cs) `CreateIsoBalloonNotes`):
   - 옛: `baseOffsetDist = Math.Max(200f, isoDiag * 0.35f)`
   - 새: `baseOffsetDist = Math.Max(100f, isoDiag * 0.22f)`
   - 최소 200→100mm, 대각 비율 0.35→0.22 → 풍선이 모델 가까이 옴

2. **엑셀 분기 Y뷰 추가 오른쪽** ([Form1.DrawingSheets.cs](A2Z/Form1.DrawingSheets.cs) `GenerateSheetDrawing2D_WithExcelTemplate` 영역 이동 블록):
   - 옛: `xOffset = (p.Index == 1 || p.Index == 4) ? 10f : 0f`
   - 새: `xOffset = (p.Index == 1) ? 10f : (p.Index == 4) ? 20f : 0f`
   - ISO(View_1) 10mm 유지, Y(View_4) 10→20mm (Z/X는 0mm 그대로)

3. **가공도 보조선 50% 축소** ([Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) 3경로):
   - 메인 ExecuteMfgDrawing(L361~377): `100/200/250/300 → 50/100/125/150`
   - MULTI 분기(L1275~1276): 동일 패턴
   - EA 신규뷰(L1929~1930 + L1926): 동일 패턴
   - `offFactor` 작은 모델 0.5x 분기는 그대로 (추가 축소 가능)
   - **근사 통일**: 제작도는 `ShowAllDimensions` v10 캔버스 절대 5/10mm, 가공도는 모델 좌표 기준이라 셀 estScale 의존이 정확. 1차 50% 단순 축소 — 부족 시 estScale 환산 도입

**영향 범위**:
- 코드: `A2Z/Form1.DrawingSheets.cs` (2곳) + `A2Z/Form1.MfgDrawing.cs` (3경로)
- 문서: `docs/기능/도면시트/시트 2D 렌더.md` + `docs/기능/가공도/가공도 단일.md` 변경 이력

**검증 흐름** (R12):
- 사용자 사내 PC 빌드 → 도면리스트 뽑기 → PDF 4종 확인:
  - ISO 풍선이 모델에 더 가까이 붙는지
  - Y뷰가 영역 중앙에서 오른쪽으로 충분히 이동했는지 (10→20mm)
  - 가공도 보조선이 제작도와 시각적으로 비슷한 길이인지

**잔여 / 후속 결정 (BOM 진단 — 별도 결정 필요)**:
- DiagLog `P2 data 구성: BOM 4행`은 정상이지만 PDF 빈 BOM 원인 진단 완료:
  - `사용자템플릿_엑셀_제작도_Rev.01.xlsx` 슬롯 컨벤션 = **2컬럼 × 5행** (AN3~AN7 + AP3~AP7, `{Input_4}~{Input_13}`만 존재)
  - 코드는 PoC 컨벤션 **8컬럼 × 15행** = `{Input_4}~{Input_123}` 매핑 → 슬롯 누락으로 ImportExcelWithData 매핑 실패
  - 헤더는 ID/ITEM/MATERIAL/SIZE 4컬럼인데 AR/AU에 슬롯이 안 들어가 있음 (사용자 미완성으로 추정)
- **방향 결정 필요** — (A) 엑셀에 슬롯 추가 / (B) 코드 매핑을 엑셀 실제 컨벤션에 맞춤

---

## 2026-05-14 — T-064 P2 본진: 엑셀 분기에 치수 그리기 + 풍선 + 모델 shrink 이식

**유형**: fix (도면리스트 뽑기 PDF 치수 누락 해결)
**커밋**: `59630c7`
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**관련**: T-038 (모델 스케일), T-039 (보조선 길이), T-028 (치수 엔진 통합)

**사용자 보고**:
> "도면리스트 뽑기에서 모델 크기 더 줄여도 될 거 같아 그리고 치수는 왜 안 그리는지 확인해볼 수 있어? 2D 출력이랑 가공도 출력 누르면 치수는 원래 추출 되는데 도면리스트 뽑기로 하니까 치수를 안 그리네?"

**원인 진단**:
- `UseExcelTemplate = true` 디폴트(`Form1.DrawingSheets.cs:1289`)로 일반 시트(제작도/조립도/설치도)는 `GenerateSheetDrawing2D` → **`GenerateSheetDrawing2D_WithExcelTemplate`**(L1639~) 로 분기.
- 그 엑셀 분기 본문은 `ComputeViewDimensionsForMembers`로 `chainDimensionList`만 채우고(L1687~1704), 4개 View 영역 루프(L1779~1821)에서는 `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` + `RescaleObject` + `MoveObjectTo`만 호출 — **`ShowAllDimensions` / `CreateIsoBalloonNotes` / `Add2DObjectFromShapeDrawing` / `Add2DMeasureFrom3DMeasure` 호출이 통째로 누락**.
- 옛 `GenerateSheetDrawing2D` 본문(L1295~1638)은 4번 `RenderSheetViewForDrawing` 호출 → 각각이 X/Y/Z 뷰에서 `ShowAllDimensions(viewDir, true, estScale)` 호출 → 치수선·보조선·풍선 모두 그림. **엑셀 분기에서는 이 단계가 누락**.
- 가공도(`GenerateMfgDrawing2DAll`)는 직전 P3 롤백(`Form1.MfgDrawing.cs:631`)으로 옛 GridStructure 8×3 흐름 유지 → 치수 정상 → 사용자가 "가공도는 추출됨"으로 인식.
- 결과: 도면리스트 뽑기 시 일반 시트만 모델·격자만 보이고 치수·풍선 누락. 메인 2D 출력 버튼도 동일 함수 사용 → 동일 증상이지만 사용자가 단일 시트만 보고 못 알아챈 것으로 추정.

**수정 (이번 커밋)**:

1. `GenerateSheetDrawing2D_WithExcelTemplate` 루프 본문(L1775~1821) 통째로 교체 — 옛 `RenderSheetViewForDrawing` 패턴을 viewArea 영역 기반으로 옮김:
   - `viewArea.Index` → `viewDirection` 문자열 매핑 (1=ISO / 2=Z / 3=X / 4=Y)
   - 매 뷰마다 `Review.Note/Measure.Clear()` + `ShapeDrawing.Clear()` + `_lastModelShiftCanvas*` 초기화
   - 시트 부재만 보이도록 가시성 격리 + `SetRenderMode(DASH_LINE)` + `MoveCamera` + `ApplyOrientationRotation`(비-ISO만) + `FlyToObject3d(1.25f)`
   - **ISO 뷰**: `CreateIsoBalloonNotes(memberIndices, true)` → `FromScreen` 가시성 필터 → `visibleNoteIds` 수집 → 2D 캡처 준비
   - **X/Y/Z 뷰**: `EstimateFitScaleForViewArea(availW, availH, viewDir, memberIndices)` → `ShowAllDimensions(viewDir, true, estScale)` → `shapeDrawingIds` 수집
   - `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` 캡처 → fit + shrink → `MoveObjectTo`
   - 보조선 `Add2DObjectFromShapeDrawing` (X/Y/Z만, line width 0.1 → 2.0 복원)
   - 풍선 `Add2DNoteFrom3DNote(visibleNoteIds)` + 원형 SnapBox (ISO만, text height 10.5 → 7 복원)
   - 치수 `ApplyParallelTextShift(viewDir, objScale, measureItems)` + `Add2DMeasureFrom3DMeasure(measureIds)` (X/Y/Z만)

2. 새 헬퍼 `EstimateFitScaleForViewArea(availW, availH, viewDirection, memberIndices)` 추가 — 옛 `EstimateFitScaleForCell`의 viewArea 영역 버전. GridStructure 셀(`GetGridCellWidth/Height/Margin`) 대신 viewArea 영역 입력.

3. **모델 shrink 사용자 사양** (2026-05-14 확정): Z=0.65 / X·Y·ISO=0.70. 옛 RenderSheetViewForDrawing의 0.70/0.75에서 추가 5% 축소. `estScale`의 `fitFactor`와 `RescaleObject`의 `shrinkFactor`가 동일 값 → 보조선 위치와 모델 fit 결과 일치.

4. ISO/Y X +10mm 오프셋 + 전 영역 Y +15mm 오프셋은 직전 커밋(`a2427c4`) 사용자 사양 그대로 유지.

**영향 범위**:
- 코드: `A2Z/Form1.DrawingSheets.cs` (+170 / -50 줄, 헬퍼 신설 + 루프 본체 교체)
- 문서: `docs/기능/도면시트/시트 2D 렌더.md` (분기 D 추가 + 변경 이력 + last_updated)

**검증 흐름** (R12):
- 사용자 사내 PC 빌드 → 도면리스트 뽑기 실행 → PDF 4종 중 일반 시트(제작/조립/설치)에 치수·풍선 표시 확인
- 모델이 라벨/보조선 영역 침범하지 않는지 시각 확인 (Z 65% / X·Y·ISO 70% 차지)
- 가공도는 영향 없음 (옛 GridStructure 경로 그대로)

**잔여 / 후속 결정** (사용자 검증 후):
- 치수 텍스트 시프트(`ApplyParallelTextShift`)가 viewArea 좌표에서도 동일 시각 효과인지 확인
- 모델 shrink 0.65/0.70이 적정한지 (더 줄이거나 늘릴 여지)
- 보조선 길이(5/10mm 캔버스 절대)가 viewArea fit과 정합한지

---

## 2026-05-13 — T-064 P2 핫픽스: DetectClash 전 CollectBOMData 호출 (T-023 연결성 통과)

**유형**: fix (bomList 사전 갱신)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `c4e09be` (P2 옵션 C — 가시성 격리 추가)

**사용자 사내 검증 결과**:
> "선택부재랑 나머지 부재랑 둘 다 사라졌다가 다시 선택부재 생기고 그다음 나머지 부재 생기면서 안돼"

= 가시성 격리는 작동 (사용자 시각 시퀀스 우리 흐름과 정합), 하지만 검사·시트 생성 *실패*.

**원인 추적 (Form1.Clash.cs L506-518, Form1.BOM.cs)**:

```csharp
// Clash_OnClashTestFinishedEvent 안의 연결성 판정 (L506):
if (!IsSingleConnectedComponent(out componentCount)) {
    MessageBox.Show("치수 추출 사전조건 / 서로 연결되지 않은 부재 그룹 ...");
    return;  // ← 여기서 return → CompleteMainDimensionPostClash 호출 안 됨 → 시트 생성 X
}
```

`IsSingleConnectedComponent`(L536-557)는 **`bomList` 기반 그래프 검사**. 우리 옵션 C 가시성 격리는 화면 표시만 변경하고 bomList는 *전체 모델 BOM 그대로* → STRU 3개 부재가 다른 부재들과 분리된 그룹 → componentCount > 1 → T-023 메시지 + return → drawingSheetList 비어있음 → 우리 ProcessSingleStruFull `throw "시트 0건"`.

**결정적 단서** (Form1.BOM.cs L345 주석):
> "xraySelectedNodeIndices가 **CollectBOMData / DetectClash에서 필터로 쓰이며**"

= `CollectBOMData()` (Form1.BOM.cs:627)를 호출하면 `xraySelectedNodeIndices` 필터 적용 → bomList에 STRU 부재만 들어감.

**수정** (1줄 추가):
```csharp
xraySelectedNodeIndices = new List<int>(memberIndices);

// ★ CollectBOMData 호출 — bomList를 STRU 부재만으로 갱신
//   (가시성 격리 + xraySelectedNodeIndices 설정 후) → IsSingleConnectedComponent가
//   STRU 3개 부재끼리만 연결성 그래프 검사 → 닿아있으면 컴포넌트 1개 → T-023 통과
bool bomCollected = CollectBOMData();
DiagLog($"T-064 STRU '{stru.NodeName}' CollectBOMData success={bomCollected}, bomList={bomList.Count}");

bool startResult = DetectClash();
```

**핵심 흐름** (수정 후):
1. STRU 후손 BODY 수집 (memberIndices)
2. 가시성 격리 (옵션 C — Show false + Show true)
3. `xraySelectedNodeIndices = memberIndices` (격리)
4. **`CollectBOMData()` 호출 → bomList = STRU 부재만**
5. DetectClash → 비동기 → OnFinished
6. IsSingleConnectedComponent: bomList=STRU 3개, clashList=STRU 페어 검사 결과 → 닿아있으면 컴포넌트 1개 → 통과
7. CompleteMainDimensionPostClash → GenerateDrawingSheets → drawingSheetList + lvDrawingSheet 채워짐
8. 시트별 Selected=true → 핸들러 자동 처리 → PDF 출력

**변경 파일** (2개, +13/-1):
- `A2Z/Form1.Stru.cs` `ProcessSingleStruFull`에 CollectBOMData 호출 1줄 + DiagLog 1줄 + 주석
- `docs/tracking/CHANGELOG.md` 항목

**빌드**: MSBuild Debug 통과

**검증 시나리오** (사내 PC):
- STRU 1개 체크 → `[도면 리스트 뽑기]` → 폴더 선택
- 다른 부재 사라짐 (옵션 C 가시성 격리 — 그대로)
- DiagLog: `T-064 STRU '/M1' CollectBOMData success=True, bomList=3`
- T-023 메시지 **안 뜸** (bomList=3개 STRU 부재 끼리 연결성 검사)
- 시트 자동 생성 + 시트별 화면 갱신 + PDF 출력
- 처리 끝나면 모든 부재 다시 표시

**잔여 위험**:
- STRU 부재 3개가 *실제로* 안 닿아있으면 여전히 componentCount > 1 → T-023. 이 경우는 사용자 평소 작업에서도 동일하게 실패할 SDK 한계.
- `CollectBOMData()`가 BOM 수집 도중 시간 소요 — 단 한 STRU 처리당 1회라 무관

---

## 2026-05-13 — T-064 P2 옵션 C (A+B 결합): STRU 시작 시 가시성 격리 추가

**유형**: fix (DetectClash 결과 정상화)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `5bde34c` (P2 옵션 B — 시트별 자동 트리거)

**사용자 사내 검증 결과**:
> "다른 부재들이 사라지지 않아 바로 간섭검사해서 떨어져있다고 안된다 팝업 나와"

DiagLog 분석:
- `T-064 STRU '/M1' bodies=3` (STRU 부재 3개 OK)
- `DetectClash startResult=True` (검사 시작 OK)
- `ERROR 시트 0건` (drawingSheetList 비어있음)
- T-023 메시지 "서로 연결되지 않은 부재 그룹 0개" 팝업

**원인 추적**:
- `Form1.Dimensions.cs:btnExtractDimension_Click` 흐름의 `CollectBOMData()`가 *보이는 부재만* bomList 수집 (Explore 사전 추적 결과)
- 사용자 평소 작업: 다른 부재 *지움(숨김)* → bomList에 STRU만 → 연결성 판정 정상 → 시트 생성
- 자동화 (옵션 B): 가시성 안 건드림 → bomList에 *전체 모델 BODY* → 다른 STRU 부재까지 포함 → STRU 부재가 다른 STRU와 분리된 그룹 → 연결성 0개 → T-023 메시지 → GenerateDrawingSheets 호출 안 됨 → 시트 0건

→ **DetectClash 시점에 가시성 격리가 필요**. 옵션 B는 시트 생성 *후* 격리라 늦음.

**옵션 C — A+B 결합**:
- 옵션 A (직접 가시성 토글) + 옵션 B (시트별 자동 트리거) 결합
- STRU 시작 시점에 우리가 가시성 격리 (옵션 A 패턴) → DetectClash 진행 시 보이는 부재만 검사 → bomList 정상 → 시트 생성
- 시트 생성 후 시트별 Selected=true 자동 트리거 (옵션 B 유지) → 시트마다 화면 갱신
- 마지막 STRU 처리 후 최종 가시성 복원 (모든 BODY 다시 표시)

**핵심 구현** (Form1.Stru.cs `ProcessSingleStruFull`):
```csharp
// ★ STRU 시작 시 가시성 격리 (DetectClash 호출 전)
var allBodies = vizcore3d.Object3D.FromFilter(
    VIZCore3D.NET.Data.Object3dFilter.ALL_INCLUDE_BODY, false);
var allBodyIndices = allBodies?.Where(n => n.Kind == NodeKind.BODY)
                              .Select(n => n.Index).ToList() ?? new List<int>();

vizcore3d.BeginUpdate();
try {
    vizcore3d.Object3D.Show(allBodyIndices, false);  // 전체 BODY 숨김
    vizcore3d.Object3D.Show(memberIndices, true);     // STRU BODY만 표시
} finally {
    vizcore3d.EndUpdate();
}

xraySelectedNodeIndices = new List<int>(memberIndices);
DetectClash();  // 보이는 부재(STRU)만 검사 → 연결성 정상 → 시트 생성
// ...
// 시트별 Selected=true (옵션 B 유지)
```

**최종 가시성 복원** (`btnExtractDrawingList_Click` finally):
```csharp
// 모든 STRU 처리 끝나면 전체 BODY 다시 표시 (사용자 일반 사용 흐름 복귀)
var allBodies = FromFilter(ALL_INCLUDE_BODY).Where(BODY);
Object3D.Show(allBodies, true);
```

**모수 선택 이유** (Object3dFilter.ALL_INCLUDE_BODY + Kind==BODY):
- 시도 1(같은 패턴)은 VisibleOnly=true와 결합돼 SDK 가시성 복원 부작용 — 핫픽스 2에서 VisibleOnly=false 해결됨
- 시도 2(`Object3dFilter.ALL` — BODY+PART+ASSEMBLY)는 부모/자식 충돌 발생
- 현재(BODY만) — 부모 PART/ASSEMBLY 안 건드림 → 충돌 없음. 기존 LvDrawingSheet 핸들러도 동일 원리(BOM BODY 모수)

**변경 파일** (2개, +50/-3):
- `A2Z/Form1.Stru.cs`:
  - `ProcessSingleStruFull` 시작부에 가시성 격리 블록 추가 (~26줄)
  - `btnExtractDrawingList_Click` finally에 최종 복원 블록 추가 (~24줄)
- `docs/tracking/CHANGELOG.md` 항목

**검증 시나리오** (사내 PC):
- STRU 체크 → `[도면 리스트 뽑기]` → 폴더 선택
- **★ 다른 부재들이 사라짐** (STRU만 보이는 상태) ← 사용자 의도와 일치
- DetectClash 진행 (시간 소요 — 페어 검사)
- 시트 자동 생성 → 시트별 화면 자동 갱신 (그 시트 부재만)
- 시트별 PDF 출력
- 처리 끝나면 모든 부재 다시 표시
- DiagLog: `T-064 STRU '/M1' 가시성 격리 — allBody=N, STRU=M`, `시트 K개 생성`, `최종 가시성 복원`

**잔여 위험**:
- 첫 STRU에서 가시성 격리 → DetectClash → OnFinished의 `CollectBOMData`가 STRU 부재만 수집. 두 번째 STRU 처리 시 우리가 다시 `FromFilter(ALL_INCLUDE_BODY)`로 전체 모수 수집 + 격리 → 정상 회복

---

## 2026-05-13 — T-064 P2 옵션 B: lvDrawingSheet 행 자동 선택으로 핸들러 트리거

**유형**: refactor (가시성 격리 패턴 교체)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `e15e1e2` (P2 본진 — GenerateSheetDrawing2D 직접 호출)

**사용자 검증 보고**:
> "이번에도 다른 부재들이 사라지지 않아 뭐가 문제일까?"

= 직전 P2 본진은 가시성 격리가 없었음 (P2a 핫픽스 3에서 가시성 토글 전체 제거 + GenerateSheetDrawing2D 직접 호출 흐름이 가시성을 안 건드림).

**사용자 결정적 단서**:
> "현재 도면목록에서 조립도나 가공도 이름을 누르면 그 조립도 부재들이나 가공도 부재만 나오게 되어 있잖아 거기서 힌트를 얻을 수는 없을까?"

= 기존 `LvDrawingSheet_SelectedIndexChanged`(Form1.DrawingSheets.cs:517-649) 패턴 활용. 시트 행 클릭 시 핸들러가 가시성·X-Ray·SilhouetteEdge·카메라·풍선·기준부재 하이라이트·치수 추출(가공도/설치도/일반 분기)을 **모두 자동 처리**.

**기존 핸들러 핵심 패턴** (잘 작동하는 가시성 격리):
```csharp
List<int> allIndices = new List<int>();
foreach (BOMData b in bomList) allIndices.Add(b.Index);  // BOM BODY만!
vizcore3d.Object3D.Show(allIndices, false);                // 모든 BOM 부재 숨김
vizcore3d.Object3D.Show(sheet.MemberIndices, true);        // 시트 부재만 표시
```

핵심 발견 — 모수가 **`bomList` BODY만**. 우리 시도 2가 부모/자식 충돌난 이유는 `Object3dFilter.ALL`로 BODY+PART+ASSEMBLY까지 토글했기 때문. BOM BODY만 토글하면 부모는 그대로라 충돌 없음.

**옵션 B 결정** (옵션 A 가시성 토글 직접 작성 대비):
- `lvDrawingSheet.Items[i].Selected = true`로 `SelectedIndexChanged` 핸들러 자동 트리거
- 핸들러가 가시성 격리·시각 효과·치수 추출·BOM 자동 수집까지 모두 자동
- 사용자 평소 시트 클릭 흐름과 100% 일치 — 시트마다 화면이 자동 갱신됨
- 우리는 PDF 출력만 별도 호출

**사전 조사 확인** (두 의문점 모두 해결):
- BOM 갱신: `Clash_OnClashTestFinishedEvent` → `CompleteMainDimensionPostClash`(Form1.BOM.cs:399) 자동 호출 → BOM·치수 자동 처리
- `GenerateDrawingSheets` (Form1.DrawingSheets.cs:18~): 매번 `drawingSheetList.Clear()` + `lvDrawingSheet.Items.Clear()` (L20-21) → STRU별 깨끗 갱신
- `lvDrawingSheet.MultiSelect` 기본값 `true` → `SelectedIndices.Clear()` 유지 필요

**ProcessSingleStruFull 본문 변경** (Form1.Stru.cs):
```csharp
// 전 (직접 호출):
foreach (var sheet in drawingSheetList.ToList()) {
    if (sheet.BaseMemberIndex == -3) GenerateMfgDrawing2DAll(new List<DrawingSheetData> { sheet });
    else GenerateSheetDrawing2D(sheet);
    Export2PDFBy2DView(...);
}

// 후 (옵션 B 자동 트리거):
for (int i = 0; i < lvDrawingSheet.Items.Count; i++) {
    var lvi = lvDrawingSheet.Items[i];
    var sheet = lvi.Tag as DrawingSheetData;
    lvDrawingSheet.SelectedIndices.Clear();  // 이전 선택 해제 (MultiSelect=true)
    lvi.Selected = true;                      // SelectedIndexChanged 자동 트리거
    lvi.EnsureVisible();                      // 화면 스크롤
    Application.DoEvents();                   // 핸들러 완료 대기
    Thread.Sleep(200);                        // 2D 렌더 안정
    
    // PDF 출력 (시트 종류는 핸들러가 자동 분기)
    Export2PDFBy2DView({STRU}_{종류}_Sheet{N}_{HHmmss}.pdf);
    DeleteAllObjectBy2DView + DeleteAllNonObjectBy2DView;  // 시트 간 메모리 정리
}
```

**핵심 제거**:
- `GenerateSheetDrawing2D(sheet)` 직접 호출 (핸들러가 자동 호출)
- `GenerateMfgDrawing2DAll(...)` 직접 호출 (핸들러의 `ExecuteMfgDrawing` 분기가 자동)
- 시트 종류별 분기 (`if BaseMemberIndex == -3 else`) — 핸들러가 자동 분기

**사용자 평소 클릭 흐름과 일치**:
- 시트마다 화면이 자동 갱신 (사용자가 한 시트 클릭하는 것과 동일)
- 가공도 시트 시 카메라가 정면(X_PLUS)으로, 일반 시트 시 ISO_PLUS fit
- 기준부재 빨간 하이라이트 자동
- 풍선·심볼 자동 정리

**변경 파일** (1개, +43/-22):
- `A2Z/Form1.Stru.cs` — `ProcessSingleStruFull` 본문 옵션 B로 교체 + 헤더 주석 갱신

**검증 시나리오** (사내 PC):
- STRU 1개 체크 → `[도면 리스트 뽑기]` → 폴더 선택
- DetectClash 진행 (화면 변화 없음, BusyOverlay 표시)
- 시트 자동 생성 후 lvDrawingSheet 채워짐
- **시트마다 화면이 자동 갱신** (시트 부재만 보임 + 카메라 fit + 기준부재 빨강) ← 사용자 의도
- 시트별 PDF 출력 (`{STRU명}_제작도_Sheet1_HHMMSS.pdf` 등)
- 다중 STRU 체크 시 각 STRU마다 동일 흐름

**의문점 검증 보류** (사용자 검증):
- `Items[i].Selected = true`가 즉시 동기 트리거인지 (WinForms 표준은 동기, `Application.DoEvents()`로 보강)
- 핸들러의 무거운 작업(`ComputeViewDimensionsForMembers`) 동기 완료 대기 시 200ms Sleep 충분한지

---

## 2026-05-13 — T-064 P2 본진: STRU 일괄 자동 도면 + PDF 출력 (P2a PoC 폐기)

**유형**: feat (P2a PoC 폐기 + 본진 도입)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `086d7d5` (P2a 핫픽스 3 — 가시성 제거)

**사용자 단순화 요청**:
> "그냥 선택되어 있는 부재들 순서 저장한 다음에 순서대로 치수추출, 제작도부터 가공도까지 출력하고 pdf 만들고 그 다음 부재 치수추출 제작도부터 가공도까지 출력 후 pdf 이렇게 하면 되는거 아니야?"

= 가시성 격리·DetectClash 별도 호출 같은 PoC 폐기. 사용자 평소 작업을 STRU별 자동 반복.

**Explore 사전 추적 결과 — 사용자 평소 흐름**:
- `xraySelectedNodeIndices` 설정 → `DetectClash()` (비동기)
- `Clash_OnClashTestFinishedEvent` 콜백에서 자동으로 `CompleteMainDimensionPostClash` → 치수 계산 + `GenerateDrawingSheets()` → `drawingSheetList` 채움 (Sheet1 제작도 + Sheet2~N 조립도 + 설치도 -2 + 가공도 -3)
- 시트별 `GenerateSheetDrawing2D` (일반) 또는 `GenerateMfgDrawing2DAll` (가공도) → `Export2PDFBy2DView` → 시트별 독립 PDF
- 핵심 발견: 기존 DetectClash가 `xraySelectedNodeIndices`로 격리 (Form1.Clash.cs:350-353) → **가시성 토글 불필요**

**P2 본진 구현**:

### 폐기 항목 (P2a PoC)
- `Form1.Stru.cs`: `_p2aClashStruNode`/`_p2aClashStartTime` 필드 제거, `P2aClash_OnFinished` 메서드 제거 (54줄)
- `Form1.Clash.cs`: `Clash_OnClashTestFinishedEvent` 진입부 P2a 가드 (`if (_p2aInProgress) return;`) 제거
  - 이유: 본진은 그 흐름의 자동 시트 생성을 *활용*해야 함

### 신설 항목 (P2 본진)
- `btnExtractDrawingList_Click` 본진 재작성:
  - CheckedIndices 순서대로 STRU 노드 수집 (사용자 "순서 저장")
  - FolderBrowserDialog로 PDF 저장 폴더 선택 (1회)
  - 다중 STRU 시 확인 팝업 ("선택된 N개 STRU의 4종 PDF 일괄 생성")
  - STRU별 루프 — `ProcessSingleStruFull` 호출
  - 진행 표시 `ShowBusyOverlay($"STRU 처리 {s+1}/{N}: {NodeName}")`
  - 실패 정책: 건너뛰고 계속 + 끝 요약 메시지박스
  - STRU 간 메모리 정리 (DeleteAllObjectBy2DView × 2 + GC.Collect × 2 + Sleep 100ms)
- `ProcessSingleStruFull(struNode, saveDir)` 신규:
  1. STRU 후손 BODY 수집 (GetChildObject3d ALL_CHILDREN + Kind==BODY)
  2. `xraySelectedNodeIndices = STRU BODY` (격리)
  3. `DetectClash()` 호출 (기존 함수) — 비동기 시작
  4. `vizcore3d.Clash.IsBusy` 폴링 + DoEvents + Sleep(50) (60초 타임아웃)
  5. OnClashTestFinishedEvent 자동 콜백이 시트 생성·치수계산까지 진행 → `drawingSheetList` 채워짐
  6. `drawingSheetList` 순회 → BaseMemberIndex로 시트 종류 식별:
     - `-3` (가공도): `GenerateMfgDrawing2DAll(new List<DrawingSheetData>{sheet})`
     - 그 외 (-1 제작도 / -2 설치도 / ≥0 조립도): `GenerateSheetDrawing2D(sheet)`
  7. `Export2PDFBy2DView({saveDir}/{STRU명}_{종류}_Sheet{N}_{HHmmss}.pdf)`
  8. 시트 간 메모리 정리
- `GetSheetKindLabel(sheet)` 신규 — BaseMemberIndex → "제작도"/"조립도"/"설치도"/"가공도" 라벨 매핑
- `_p2aInProgress` 가드 유지 — 의미 변경 (재진입 차단만, Clash 흐름 차단은 제거)

**다중 에이전트 토론**:
1. 라운드 1 (Explore 1): 사용자 평소 흐름 추적 — btnExtractDimension(동기) vs DetectClash(비동기) 분리 + GenerateDrawingSheets 한 번에 4종 + 가공도 별도 흐름
2. 라운드 2 (general-purpose 위임): P2a 폐기 + 본진 구현
3. 라운드 3 (Explore 압축 리뷰): 안정성 OK, ⚠️ 3건 모두 대형 STRU 시나리오용 (N² 페어, 60s 타임아웃) — 사용자 보통 사용에선 무관
4. 라운드 4: MSBuild Debug 통과 → commit + push

**변경 파일** (2개, +197/-179):
- `A2Z/Form1.Stru.cs` 366줄 변경 (최종 545줄)
- `A2Z/Form1.Clash.cs` 10줄 변경 (가드 제거)

**검증 시나리오** (사내 PC):
- 모델 열기 → STRU 1개 체크 → `[도면 리스트 뽑기]` → 폴더 선택 → 처리 진행
- 화면: BusyOverlay에 `"STRU 처리 1/1: /M1"` 표시. 평소 간섭검사 진행 과정 (DetectClash 비동기)
- 완료 후 메시지박스 `"STRU 일괄 도면 출력 완료 / 성공: 1개 STRU (PDF N개)"`
- 폴더에 PDF 파일들 — `M1_제작도_Sheet1_HHMMSS.pdf`, `M1_조립도_Sheet2_HHMMSS.pdf` 등 N개
- DiagLog: `T-064 STRU '/M1' bodies=N`, `DetectClash startResult=True`, `시트 N개 생성`, `PDF saved: ...`, `STRU '/M1' 완료 — PDF M개`
- 다중 STRU 체크 → 확인 팝업 → STRU별 순차 처리

**잔여 위험** (사용자 검증 후 보강 후보):
- 60초 타임아웃 부족 가능성 (STRU 부재 100개+ 대형 케이스) — DiagLog `TIMEOUT (60s)` 신호 확인 시 타임아웃 확장
- 페어 N² 폭발 (기존 DetectClash 동일 패턴) — 대형 STRU 시 검사 시간 ↑
- DetectClash startResult=false 시 fallback (GenerateDrawingSheets 직접) — 단일 부재 STRU 등 엣지 케이스 의미 검증 필요

---

## 2026-05-13 — T-064 P2a 핫픽스 3: 가시성 변경 코드 전체 제거 (그룹 한정 격리만)

**유형**: fix (가시성 토글 부작용 회피)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `20a9984` (P2a 핫픽스 2 — VisibleOnly=false)

**사용자 사내 검증 결과**:
> "이번엔 도면리스트 뽑기 누르면 체크되어있던 모델들까지 모두 한 번에 사라졌다가 다시 생겨"

= 가시성 격리 시점에 **체크된 STRU 부재까지 같이 사라짐** → finally 복원 시 모두 다시 표시. 핫픽스 1에서 `Object3dFilter.ALL` 모수 + STRU 본인+후손 표시로 확장했으나 부모/자식 가시성 상호작용 충돌로 STRU 부재까지 숨겨지는 부작용.

**원인 추정**:
- `Show(allNodes, false)` 호출 시 부모 ASSEMBLY까지 false → 자식 부재(STRU 본인 + 후손)도 SDK 표시 트리에서 사라짐
- 후속 `Show(struVisibleIndices, true)`가 부모/자식 chain을 완전 복원 못 함
- 검사 진행 중 사용자는 빈 화면 → finally `Show(allNodes, true)`로 모두 다시 표시되며 "사라졌다 다시 생김" 시퀀스

**근본 해결** — 기존 `DetectClash(Form1.Clash.cs:340~363)` 패턴과 통일:
- 기존도 **가시성을 안 건드림**. 그냥 보이는 부재(또는 xraySelectedNodeIndices)에서 targetNodes 만들어 ClashTest GroupA/B로 한정
- SDK는 ClashTest 그룹 외 검사 안 함 → 격리는 그룹으로 충분, 가시성 조작 불필요
- 우리도 동일 적용

**수정** (가시성 변경 코드 전체 제거):
- `Form1.Stru.cs:btnExtractDrawingList_Click`:
  - `allNodes`/`allNodeIndices` 수집 코드 제거
  - `struVisibleIndices` 변수 제거
  - `Show(allNodes, false)` + `Show(struVisible, true)` 호출 제거
  - try 블록 안 `BeginUpdate/EndUpdate` 묶음 제거
  - finally 안 `Show(allNodes, true)` 복원 코드 제거
- ClashTest 페어 생성 (GroupA={STRU BODY[i]}, GroupB={STRU BODY[j]}, VisibleOnly=false)로 격리만 — 사용자 시각으로 화면 변화는 없음
- 사용자 인지: `ShowBusyOverlay` 텍스트("STRU 격리·간섭검사 진행 중: {NodeName}")로 처리 상태 표시

**변경 파일** (1개, ~60줄 제거):
- `A2Z/Form1.Stru.cs` — 가시성 토글 블록 전체 제거 + 진단 로그 조정

**검증** (사내 PC):
- `[도면 리스트 뽑기]` 클릭 → 화면 변화 없음 (가시성 그대로) + BusyOverlay 진행 표시
- 검사 진행 후 DiagLog 확인:
  - `T-064 P2a 시작 STRU='/M1' struBODY=N (가시성 미변경 — ClashTest 그룹으로만 격리)`
  - `T-064 P2a Clash 페어 K개 등록`
  - `T-064 P2a 진행 중 — 기존 Clash_OnClashTestFinishedEvent 흐름 차단` (가드 작동)
  - `T-064 P2a OnFinished` + `T-064 P2a result[i] ...` + `T-064 P2a 결과 요약 STRU='/M1' totalPairs=K`
- T-023 사전조건 메시지 안 뜸

**P2a 범위 재확인**:
- PoC라 lvDrawingSheet에 도면 시트 채우지 않음 — **사용자가 "도면 리스트가 안 나온다"는 P2a 범위 외**. 도면 시트는 P2b에서 GenerateDrawingSheets 호출 시 본격
- P2a 검증 포인트는 **간섭검사가 STRU 단위로 정확히 도는지**만 (DiagLog 결과 페어 확인)

**잔여 위험** (P2b 진입 시):
- 페어 N² 등록 (STRU 부재 100개면 4950 페어) — SDK 부담. GROUP_VS_GROUP에서 GroupA=GroupB=전체 STRU BODY 1개 ClashTest로 단순화 검토
- 다중 STRU 루프 시 e.ID 검사 필수

---

## 2026-05-13 — T-064 P2a 핫픽스 2: VisibleOnly=true → false (SDK 가시성 재설정 회피)

**유형**: fix (1줄 SDK 옵션 정정)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `239880c` (P2a 핫픽스 1 — ALL 확장 + 가드)

**사용자 사내 검증 결과**:
> "다른 부재들이 잠깐 사라지는데 간섭검사할 때는 다시 사라졌던 부재가 다시 나타나고 간섭검사 해서 도면리스트가 안 나오는 거 같은데"

즉 가시성 격리 자체는 작동 (Show 호출 후 다른 부재 일시 숨김) — 그러나 `PerformInterferenceCheck()` 호출 직후 SDK가 가시성을 자동 복원. 검사가 전체 모델 대상으로 진행 → 다른 STRU·다른 모델 부재 포함 결과.

**원인 확정** (기존 `Form1.Clash.cs:DetectClash` 비교):
- 기존 코드 L348~363에서 가시성을 **코드 레벨에서 사전 필터링**해 `targetNodes` 만들고
- L381: `VisibleOnly = false` — SDK에 "가시성 건드리지 마, 등록된 그룹만 검사" 명시
- 우리 P2a만 `VisibleOnly = true`였음 → SDK가 검사 직전 가시성을 자체 재설정 (격리 무효화)

**수정** (1줄):
- `Form1.Stru.cs` P2a `pairClash.VisibleOnly = true` → **`false`**
- 격리는 ClashTest 그룹(GroupA/B = STRU 후손 BODY만)으로 *한정* → SDK는 그 그룹 외 검사 안 함
- 우리 사전 가시성 격리(Show 호출)는 시각 확인용으로 유지 (사용자가 "이 STRU를 처리 중"임을 시각 인지)

**검증** (사내 PC):
- `[도면 리스트 뽑기]` → 가시성 격리 유지 (다른 부재 사라진 상태 유지) + 검사 진행
- DiagLog: `T-064 P2a 결과 요약 STRU='/M1' totalPairs=K` — K가 의미 있는 값 (격리 작동 시 K = STRU 내부 간섭쌍만)
- T-023 사전조건 메시지 안 뜸 (P2a 가드 유효)

**P2a 범위 명확화** (사용자 "도면리스트 안 나옴" 보고 대응):
P2a는 **PoC 단계** — 결과는 DiagLog만 출력, lvDrawingSheet 도면 리스트는 채우지 않음. 도면 시트 생성은 **P2b**에서 GenerateDrawingSheets 호출 + STRU 컬럼 추가 시 본격 진행. 본 변경은 검사 자체가 STRU 격리되어 정확히 도는지 확인하는 단계.

**변경 파일** (1개, ±4줄):
- `A2Z/Form1.Stru.cs` — `VisibleOnly` 옵션 1개 + 주석

---

## 2026-05-13 — T-064 P2a 핫픽스: 가시성 격리 ALL 확장 + 기존 핸들러 가드 차단

**유형**: fix (P2a 검증 결과 두 결함 수정)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `9d2b4aa` (P2a PoC + SDK 호환 패치)

**사용자 사내 PC 검증 결과 두 결함 보고**:
1. **메시지**: `[도면 리스트 뽑기]` 누르면 "치수추출 사전조건. 치수추출은 ~ / 현재: 서로 연결되지 않은 부재 그룹 0개 발견" 메시지 발생
   - = 기존 `Clash_OnClashTestFinishedEvent` 흐름(T-023 사전조건 + 시트 생성)이 호출됨 = **P2a 핸들러 swap 실패**
2. **가시성 격리 미작동**: 사용자 가설 — "도면 리스트 뽑으면 순서대로 하나씩 가시화되고 나머지 지우는 게 맞잖아"

**원인 분석**:
- 결함 1: 핸들러 swap (`-= 기존 += P2a`) 패턴이 SDK 시그니처 미세 차이 또는 등록 메서드 참조 문제로 실패. 기존 핸들러가 그대로 살아있음 → 결과 콜백이 기존 흐름으로 흘러 T-023 메시지 표시
- 결함 2: `Object3dFilter.ALL_INCLUDE_BODY` 모수 + BODY 필터만 사용 → BODY는 숨겨도 부모 PART/ASSEMBLY는 그대로 보이는 상태 → `VisibleOnly=true` 검사 대상에 포함될 가능성

**수정 (두 가지 함께)**:

### 1) 핸들러 swap 제거 + 가드 변수 통일
- `Form1.Clash.cs:Clash_OnClashTestFinishedEvent` 진입부에 P2a 가드 추가:
  ```csharp
  if (_p2aInProgress) { DiagLog(...); return; }
  ```
- `Form1.Stru.cs:btnExtractDrawingList_Click`:
  - 기존 핸들러 `-=` 코드 제거 (swap 신뢰 X)
  - P2a 핸들러 `+=`만 등록 (가드로 기존 흐름 차단되니 충돌 없음)
  - `handlersSwapped` 플래그 → `p2aHandlerRegistered`로 의미 명확화
  - finally에서 P2a 핸들러 `-=`만 수행

### 2) 가시성 격리 ALL 노드로 확장
- 모수: `Object3dFilter.ALL_INCLUDE_BODY` → **`Object3dFilter.ALL`** (BODY/PART/ASSEMBLY 모두)
- 표시 대상: STRU 후손 BODY만 → **STRU 본인 + 모든 후손 (BODY/PART/ASSEMBLY 다)**
  ```csharp
  var struVisibleIndices = new List<int> { struNode.Index };
  struVisibleIndices.AddRange(descendants.Select(n => n.Index));
  ```
- ClashTest 페어 생성용 BODY 필터는 별도 유지

**변경 파일** (2개, ±70줄):
- `A2Z/Form1.Clash.cs` (+6) — 진입부 가드 1개
- `A2Z/Form1.Stru.cs` (~64 변경) — 가시성 격리 알고리즘 + 핸들러 swap 제거

**검증 시나리오**:
- 모델 1개에 STRU 여러 개 있는 상태에서 `[도면 리스트 뽑기]` 클릭
- 화면에 선택한 STRU 부재만 표시 + 다른 STRU는 모두 사라짐 (격리 정확)
- T-023 사전조건 메시지 안 뜸 (가드로 차단)
- `logs/diag-YYYY-MM-DD.log`:
  - `T-064 P2a 시작 STRU='...' struBODY=N struVisible=K allNodes=M` — M(전체) > K(STRU 후손)
  - `T-064 P2a 진행 중 — 기존 Clash_OnClashTestFinishedEvent 흐름 차단` (가드 작동 신호)
  - `T-064 P2a OnFinished` + 결과 페어 DiagLog
- 처리 끝나면 가시성 자동 복원 (전체 노드 다시 표시)

**잔여 위험** (P2b 진입 전 검토):
- 페어 N² 등록 — STRU 부재 100개면 4950 페어. SDK 부담 가능. GROUP_VS_GROUP 1개 ClashTest로 단순화 권고
- 다중 STRU 루프 시 `e.ID` 검사로 콜백 구분 필요

---

## 2026-05-13 — T-064 P2a PoC: STRU 격리 + DetectClash + 결과 DiagLog (+ SDK 호환 패치)

**유형**: feat (P2a — STRU 가시성 격리 + 간섭검사 PoC) + fix (SDK 시그니처 호환)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `c32bd52` (P1 UX 보강)

**P2a 범위** (PoC):
- `[도면 리스트 뽑기]` 버튼 신설 (panelDrawingButtons)
- 체크된 STRU 첫 번째 1개 대상 — 다중 체크 시 첫 번째만 처리 + DiagLog 안내 (다중은 P2c)
- 가시성 격리: 전체 BODY 숨김 → STRU 후손 BODY만 표시 (Object3D.Show bulk)
- 간섭검사 격리: P2a 전용 ClashTest 페어 직접 생성 (VisibleOnly=true 핵심) + PerformInterferenceCheck
- 결과 DiagLog만 — clashList/시트 생성에 반영 안 함 (PoC라 컨텍스트 분리)
- 가시성 복원 (try/finally)
- 기존 OnClashTestFinishedEvent 핸들러 임시 해제 + P2a 전용 P2aClash_OnFinished 등록 후 finally 복원

**SDK 사전 검증** (sdk-verifier 라운드 1):
- `Object3D.Show(List<int>, bool)` (xml 49431) — bulk 가시성
- `ClashTest.VisibleOnly` (xml 2709) — 핵심 격리 옵션
- `Clash.PerformInterferenceCheck()` (xml 40608) + `IsBusy` (40055) + `OnClashTestFinishedEvent` (40065)
- `Clash.GetResultItem(ClashTest, ResultGroupingOptions.PART)` (xml 40661)
- `Object3dFilter.ALL_INCLUDE_BODY` (xml 4733)
- `Object3DChildOption.ALL_CHILDREN` (xml 4877) + `NodeKind.BODY` (4583) — P1 패턴 재사용

**P2a 보강 3건** (위험 리뷰 발견 후 즉시 적용):
1. **진행 가드** `_p2aInProgress` — 재진입 차단. P2a 실행 중 같은 버튼·간섭검사 재호출 시 early return
2. **UI 차단** `ShowBusyOverlay`/`HideBusyOverlay` + 버튼 Enabled=false — 사용자가 도중 모델 변경·다른 흐름 트리거 방지 (격리 상태 고착 위험 ❌)
3. **`BeginUpdate`/`EndUpdate` 묶음** — Show 호출 6회를 화면에 한 번에 반영 (응답성)

**SDK 호환 패치** (외부 회귀 대응, P2a 무관):
- 사용자 SDK 업그레이드: `VIZCore3D.NET` → **`VIZCore3D+.NET`** (Vibe 3D Lab 새 SDK, csproj `HintPath` OneDrive 경로)
- 새 시그니처: `Drawing2DTemplateManager.CrateTemplateBorder(TemplateBorderInfo borderInfo)` — 옛 무인자 호출 폐기
- 3곳 패치:
  - `Form1.MfgDrawing.cs:661` — `new TemplateBorderInfo()` 인스턴스 만들어서 인자로 전달. bInfo.MaxX/MinY 후속 사용은 SDK out 패턴 가정 (사용자 검증 시 도면 결과로 확인)
  - `Form1.DrawingSheets.cs:1309` — 단순 호출, 변수 미수신
  - `Form1.ExcelTemplate.cs:82` — `var bInfo` 제거 (void 반환), DiagLog 유지

**다중 에이전트 4라운드** (P2a):
1. 라운드 1 (병렬 2): sdk-verifier 심화 검증 + 코드베이스 기존 흐름 조사 — `ClashTest.VisibleOnly=true` 발견 + DetectClash 비동기 콜백 패턴 확정
2. 라운드 2 (구현 1): general-purpose 위임 — ClashTest 재사용 포기·P2a 신규 생성 결정
3. 라운드 3 (병렬 2): 패턴 리뷰 "전 항목 ✅" + 위험 리뷰 ❌ 2건(UI 차단·진행 가드) + ⚠️ 1건(BeginUpdate) 발견 → 즉시 보강
4. 라운드 4: SDK 호환 패치(외부 회귀) → MSBuild Debug 통과 → commit + push

**변경 파일** (5개, +298/-9):
- `A2Z/Form1.Designer.cs` (+17) — btnExtractDrawingList 신설 + panelDrawingButtons 40→70px 확대
- `A2Z/Form1.Stru.cs` (+277) — 필드 3개 (P2a struNode/시작시각/inProgress) + btnExtractDrawingList_Click + P2aClash_OnFinished 핸들러
- `A2Z/Form1.DrawingSheets.cs` (+3) — SDK 호환 1줄
- `A2Z/Form1.ExcelTemplate.cs` (+5) — SDK 호환 (var 제거)
- `A2Z/Form1.MfgDrawing.cs` (+5) — SDK 호환 (new 인스턴스 + 호출 분리)

**검증 시나리오** (사용자 사내 PC):
- 모델 열기 → STRU 1개 체크 → `[도면 리스트 뽑기]` 클릭
- `logs/diag-YYYY-MM-DD.log`:
  - `T-064 P2a 시작 STRU='/M1' struBODY=N allBODY=M`
  - `T-064 P2a Clash 페어 K개 등록 (VisibleOnly=true)`
  - `T-064 P2a PerformInterferenceCheck startResult=True`
  - `T-064 P2a OnFinished STRU='/M1' ID=... elapsed=...s`
  - `T-064 P2a result[i] A_idx=... A='...' B_idx=... B='...'` (최대 50건)
  - `T-064 P2a 결과 요약 STRU='/M1' totalPairs=...`
- 버튼 클릭 중 다른 흐름 차단 (BusyOverlay 표시) + 진행 가드 작동
- 처리 끝나면 가시성 자동 복원 (전체 BODY 표시 상태로)
- **SDK 호환 검증**: 기존 도면 생성·가공도·PoC 엑셀 흐름이 빌드 통과만 확인. 실행 결과는 사용자 시각 검증 필요 (`bInfo.MaxX/MinY` 사용 부분이 새 SDK out 패턴 채워주는지)

**P2b 대비 위험 메모** (이번 ⚠️ 미해결, P2b 진입 시 처리):
- 다중 STRU 루프 시 `OnClashTestFinishedEvent` 콜백에 `e.ID` 검사 필수 (다른 ClashTest 완료 혼선 방지)
- IsBusy 동기화 지연 → OnFinished 플래그로 폴링 종료 전환 권고
- xraySelectedNodeIndices 동기화 (시트 생성 연결 시 주의)
- 다른 강조 흐름(BOM/Dimension) 색상 채널 충돌

---

## 2026-05-13 — T-064 P1 UX 보강: 행 선택 시 카메라 fit 복원 (체크 트리거와 분리)

**유형**: feat (P1 UX 보강)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `2dae22e` (P1 UX — 체크박스 트리거)

**사용자 요청** (직전 UX 변경 후 fit 누락 보고):
- 체크박스 클릭 → 강조 토글 (fit 없음) [기존 유지]
- **행 선택(이름 클릭) → 카메라 fit (강조 변경 X)** — 직전 커밋에서 SelectedIndexChanged 핸들러 삭제 시 같이 사라졌음

**의미 분리** (두 트리거):
| 트리거 | 동작 | 의미 |
|---|---|---|
| 체크박스 영역 클릭 | ItemCheck → 강조 토글 (Color.Select/Restore) | 출력 대상 / 강조 |
| 이름·행 영역 클릭 | SelectedIndexChanged → 카메라 fit (FlyToObject3d) | 시각 이동 (강조는 그대로) |

**WinForms 이벤트 race 해결**:
- 문제: 체크박스 클릭 시 WinForms가 행 선택도 동시 변경 → SelectedIndexChanged도 트리거 → 의도치 않은 fit
- 가드 패턴: `_suppressStruSelChanged` 필드
  - ItemCheck 진입부 → 가드 set → 본문 → finally `BeginInvoke(가드 clear)` (큐 끝 지연 해제)
  - SelectedIndexChanged → `BeginInvoke(PerformFlyToSelectedStru)` 지연 콜백 → 가드 검사 → on이면 return, off면 fit
- 결과: 체크박스 클릭은 fit 차단, 이름 클릭만 fit 실행

**구현 변경**:
- `A2Z/Form1.Stru.cs`:
  - 헤더 P1 범위 주석 갱신 (두 트리거 의미 분리 명시)
  - `_suppressStruSelChanged` 필드 신규
  - `ClbStruList_ItemCheck` → 가드 set + 본문 분리(`ItemCheckCore`) + finally BeginInvoke 해제
  - `ClbStruList_SelectedIndexChanged` 신규 — BeginInvoke 지연 → `PerformFlyToSelectedStru` 호출
  - `PerformFlyToSelectedStru` 신규 헬퍼 — 가드 검사 + ALL_CHILDREN 후손 BODY 수집 + `FlyToObject3d(1.2f)`만 (Select/RestoreColorAll 호출 없음 — 체크 강조 유지)
- `A2Z/Form1.Designer.cs`:
  - `clbStruList.SelectedIndexChanged += new System.EventHandler(ClbStruList_SelectedIndexChanged)` 등록 추가

**기존 4경로(BOM/lvOsnap/lvDim/lvClash) 패턴과 차이**:
- 기존: SelectedIndexChanged 하나에 강조+fit 묶임 (단일 트리거)
- STRU P1: 두 트리거 분리 — 체크=강조(누적), 행선택=fit(단발)
- 사용자 메모리 "패턴 무비판 이식 금지" — 컨텍스트 차이 명시 (체크박스 모드라 의미 분리 가능)

**변경 파일** (3개):
- `A2Z/Form1.Stru.cs` ±50줄 — `_suppressStruSelChanged` 필드 + 핸들러 가드 + 신규 SelectedIndexChanged + PerformFlyToSelectedStru 헬퍼
- `A2Z/Form1.Designer.cs` ±1줄 — SelectedIndexChanged 이벤트 등록
- `docs/tracking/CHANGELOG.md` 항목 추가

**검증 시나리오** (사용자):
1. STRU 이름 클릭 → 행 선택 → 카메라가 그 STRU로 fit 이동. 빨강 강조 상태는 그대로 유지
2. 체크박스 클릭 → 강조 토글. 카메라 시점 그대로 (fit 없음). 행이 동시 선택되더라도 가드로 fit 차단
3. 다른 STRU 이름 클릭 → fit만 이동. 체크된 STRU 빨강은 유지
4. 여러 STRU 체크 후 한 STRU 이름 클릭 → 카메라 그 STRU로, 빨강은 다중 누적 그대로
5. DiagLog: `T-064 ClbStru Select '/M1' fit BODY=N` (행 선택 fit 시) / `T-064 ItemCheck idx=N new=Checked ...` (체크 토글 시)

---

## 2026-05-13 — T-064 P1 UX: 체크박스 트리거 + 다중 강조 누적 + fit 제거

**유형**: feat (P1 UX 미세 조정)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `8d131e5` (P1 재설계 — FRMWORK 부모 룰)

**사용자 요청** (P1 식별·강조 검증 통과 후):
1. 이름(텍스트 라벨)만 클릭해도 체크되는 동작 막기 — 체크박스 영역만 토글
2. 강조는 체크 상태와 연동 — 체크 시 빨강, 해제 시 빨강 해제
3. 카메라 fit 제거 — 체크/해제 시 시점 변동 없음
4. 다중 체크 강조 유지 — 여러 STRU 동시 체크 시 모두 빨강

**구현**:
- `Form1.Designer.cs` (2줄): `CheckOnClick=true→false`, `SelectedIndexChanged → ItemCheck` 이벤트 등록
- `Form1.Stru.cs` (메서드 1개 교체):
  - `ClbStruList_SelectedIndexChanged` **삭제** (50줄)
  - `ClbStruList_ItemCheck` **신규** (54줄)
  - 헤더 P1 범위 주석 갱신

**핵심 알고리즘** (다중 강조 누적):
```
ItemCheck 이벤트는 체크 *직전* 발생 → e.NewValue가 미래 상태
  futureCheckedIdx = CheckedIndices ∪ {e.Index} (NewValue=Checked)
                    또는 CheckedIndices \ {e.Index} (NewValue=Unchecked)
  allBodyIndices = ∪{stru in futureCheckedIdx} GetChildObject3d(stru.Index, ALL_CHILDREN, true).Where(Kind==BODY)
  BeginUpdate → RestoreColorAll → Select(allBodyIndices, true, false) → EndUpdate
  // FlyToObject3d 호출 의도적 제거 — 사용자 요청
```

**기존 패턴과 의도적 차이** (사용자 메모리 "패턴 무비판 이식 금지" 준수):
- BOM/lvOsnap/lvDimension/lvClash 행 강조 = `SelectedIndexChanged + Fly`
- STRU = `ItemCheck + 다중 누적 + fit 없음` — 의미 분리 (체크박스=출력대상, 행선택=별도)

**다중 에이전트 3라운드**:
1. 라운드 1 (구현 1): general-purpose 위임
2. 라운드 2 (병렬 2): 패턴 리뷰 "병합 대기" / 위험 리뷰 "조건부 진입 가능"
3. 라운드 3: 빌드 + commit + push

**P2 진입 시 검토 권고** (위험 리뷰 ⚠️ 2건):
- `btnSelectAllStru` 전체 선택 시 ItemCheck N회 호출 → 대형 모델 N² 위험 (가드로 일괄 처리 권고)
- BOM/Dimension 등 다른 강조 흐름과 색상 채널 미분리 (RestoreColorAll 충돌) — 색상 격리 또는 합집합 유지 검토

**변경 파일** (2개, +37/-34):
- `A2Z/Form1.Designer.cs` ±4
- `A2Z/Form1.Stru.cs` 207→210줄 (+3 순증, 메서드 교체)

**검증 시나리오** (사용자):
- STRU 이름 클릭 → 행 선택만, 체크 안 됨, 강조도 안 됨
- 체크박스 클릭 → 체크 + 부재 빨강. 카메라 시점 안 바뀜
- 다른 STRU 추가 체크 → 양쪽 부재 모두 빨강 (누적 유지)
- 체크 해제 → 그 STRU만 강조 해제 (다른 체크된 STRU는 유지)
- 모든 체크 해제 → 전체 강조 사라짐

---

## 2026-05-13 — T-064 P1 재설계: STRU 식별 룰 (FRMWORK 부모) + 강조 재귀 명시

**유형**: fix (P1 검증 결과 두 문제 해결)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**이전 커밋**: `1e4d43a` (P1 초안)

**배경**: 사용자 사내 PC 실기 검증 결과 두 문제 보고
1. STRU 목록에 "MNOP /M1", "EFGH /M1" 같은 합성 NodeName이 표시됨 (사용자 의도 STRU는 `/M1` 하나)
2. STRU 행 클릭 시 일부 부재만 빨간 강조 (전체 후손 BODY 누락)

**원인 분석** (다중 에이전트 토론):
- 실수 1: `CollectStruList`의 NodeName/NodePath OR 필터에서 NodePath는 모든 노드가 `/` 시작(예: `/E1/E1/M1/MNOP`)이라 LEAF_ASSEMBLY 모수의 *모든* 노드가 통과
- 실수 2: `GetChildObject3d(idx, NodeFilterKind.BODY)` 오버로드의 재귀 여부 SDK 문서 미명시 → 직접 자식만 반환 가능성

**사용자 발견 (결정적 단서)**:
- 트리 구조: `/E1(파일) → /E1 → /E1 → /M1(STRU) → IJKL/MNOP(NodeName="FRMWORK 0 of STRUCTURE ...") → 부재들`
- **STRU = NodeName이 "FRMWORK "(대소문자 무시)로 시작하는 어셈블리의 부모 어셈블리**
- 향후 다른 STRU 식별 룰 추가 가능한 **확장 구조** 필요

**알고리즘 재설계** (옵션: 룰 union):
```
CollectStruList:
  1) FromFilter(Object3dFilter.ASSEMBLY, true) — 모든 어셈블리 모수
  2) 진단: 어셈블리 상위 30건 NodeName/parentIdx/depth 로깅
  3) struIndices = HashSet<int>() — 룰들의 union
  4) RuleByFrameworkChildParent(assemblies) 호출 → ParentIndex yield
  5) (향후) RuleByUdaMarking, RuleByDepthThreshold, RuleByNameSlashPrefix 추가 자리
  6) assemblies.Where(n => struIndices.Contains(n.Index))
  7) Fallback (룰 0건): NodeName "/" 시작 + 공백 없는 어셈블리

RuleByFrameworkChildParent:
  - assemblies 순회
  - NodeName.StartsWith("FRMWORK ", OrdinalIgnoreCase) + ParentIndex >= 0 필터
  - ParentIndex 수집 후 List 반환
  - 진단: FRMWORK 어셈블리 카운트 로깅

ClbStruList_SelectedIndexChanged (강조):
  - GetChildObject3d(idx, Object3DChildOption.ALL_CHILDREN, true) — 재귀 명시
  - allDescendants.Where(b => b.Kind == NodeKind.BODY) — BODY 필터
  - 진단: allDescendants/BODY 카운트 로깅
  - BeginUpdate/EndUpdate try/finally + RestoreColorAll + Select + FlyToObject3d
```

**SDK API 검증** (sdk-verifier, `lib\VIZCore3D.NET.xml`):
- `Object3dFilter.ASSEMBLY` (line 4728) — LEAF_ASSEMBLY 대신
- `Object3DChildOption.ALL_CHILDREN` (line 4877) — 재귀 명시 오버로드
- `NodeKind.BODY` (line 4583) — `Node.Kind` 프로퍼티(line 9711)로 비교
- `Manager.Object3DManager.GetChildObject3d(int, Object3DChildOption, bool)` (line 50132) 오버로드 사용

**변경 파일** (2개, +85/-33):
- `A2Z/Form1.Stru.cs` 158→207줄 (+49 순증)
  - `CollectStruList` 재작성: ASSEMBLY 모수 + 룰 union + fallback
  - `RuleByFrameworkChildParent` 신규: FRMWORK prefix → ParentIndex 1단계 추출
  - `ClbStruList_SelectedIndexChanged` 강조: ALL_CHILDREN 재귀 + Kind==BODY 필터
- `docs/tracking/TASKS.md` (+4) — T-064 P1 항목에 룰 재설계+재귀 명시 2줄

**다중 에이전트 4라운드 토론**:
1. 라운드 1 (병렬 2): sdk-verifier 심화 검증(Object3dFilter 전체 멤버, GetChildObject3d 오버로드, ParentIndex 패턴) + Explore(코드베이스 트리 순회·DiagLog 동작)
2. 라운드 2 (구현 1): general-purpose 에이전트 코드 작성 위임 — 두 차례 사용자 정정 후 정확한 FRMWORK 룰 알고리즘 적용
3. 라운드 3 (병렬 2): 패턴 일관성 리뷰 "병합 권장" + 위험 리뷰 "검증 진입 가능" (경미한 ⚠️ 2건 P2 진입 시 검토 권고)
4. 라운드 4: MSBuild Debug 통과 → commit + push

**검증 시나리오** (사용자 사내 PC):
- 모델 열기 → 좌측 STRU 목록에 `/M1` 1건만 표시 (이전 "MNOP /M1", "EFGH /M1" 합성형 사라짐)
- `/M1` 행 클릭 → DiagLog `T-064 ClbStru '/M1' allDescendants=N, BODY=M` (logs/diag-YYYY-MM-DD.log)
- 3D에서 QRST/UVWX/DAJS/YZAB 4개 부재 모두 빨강 + 카메라 fit
- Fallback 작동 확인: 만약 룰 매칭 0건이면 디버그 표시 + 경고 로그

**확장 인터페이스** (향후 룰 추가):
- 같은 union 패턴으로 `RuleByUdaMarking` / `RuleByDepthThreshold` / `RuleByNameSlashPrefix` 등 추가 가능
- 각 룰은 `IEnumerable<int>` 반환 (STRU 인덱스 후보) → HashSet 합집합

**다음 단계 (P2)**:
- `[도면 리스트 뽑기]` 버튼 추가 — 체크된 STRU별 가시성 토글(STRU 후손 BODY만 Visible=true) → DetectClash → GenerateDrawingSheets → lvDrawingSheet 누적 + STRU 컬럼 추가
- 리뷰 권고: P2 진입 시 `GetAllDescendantBodies(int nodeIdx)` 헬퍼 메서드 추출 검토 (ALL_CHILDREN 패턴 재사용성)

---

## 2026-05-13 — T-064 P1: STRU 일괄 도면 출력 — 목록·강조 단계

**유형**: feat (신규 흐름 P1 — STRU 추출 + UI + 행 선택 강조)
**관련 TASK**: T-064 (STRU 일괄 도면 출력)
**관련 자산**: HYI-STRU 브랜치 4커밋(2f024d1, 6bc89cf, 4f99335, 1d17cc6) 중 `6bc89cf` NodePath fallback 패턴만 가져옴. 나머지는 P2/P3로 재설계 보류.

**배경**: 사용자 요구 6항목 (STRU 목록 / 체크박스 / 자동 도면 4종 PDF / 행 강조 / 도면 리스트 뽑기 / 다중 체크 확인 팝업)을 다중 에이전트 토론으로 4 Phase로 분할. 합의 핵심 — 조립도는 Sheet 2~N(1-hop Clash 이웃, 부분조립)으로 매핑, 간섭검사 격리는 가시성 토글 방식, 버튼 2개로 분리(미리보기 / 즉시 PDF).

**P1 구현 범위**: STRU 목록 표시 + 행 선택 시 3D 부재 강조 + 카메라 fit. PDF/일괄처리 미포함.

**변경 파일**:
- 신규 `A2Z/Form1.Stru.cs` (157줄) — partial class Form1 멤버 4개
  - `CollectStruList` — `FromFilter(LEAF_ASSEMBLY, includeNodePath:true)` + NodeName/NodePath "/" 시작 필터 + 0건 fallback + 진단 DiagLog
  - `PopulateStruCheckList` — CheckedListBox 채우기 (표시: NodeName→NodePath→`(Index N)`)
  - `btnSelectAllStru_Click` — 전체 선택/해제 토글
  - `ClbStruList_SelectedIndexChanged` — 선택 행 STRU의 GetChildObject3d(BODY) → 3D 강조+fit
- `A2Z/Form1.Designer.cs` (+56) — `groupBoxStru`(Dock=Top 240px) + `clbStruList`(CheckOnClick=true) + 라벨 + 전체선택 버튼
- `A2Z/Form1.BOM.cs` (+3) — 모델 Open `BuildBodyToPartNameMap()` 직후 `PopulateStruCheckList()` 호출
- `A2Z/A2Z.csproj` (+4) — Form1.Stru.cs Compile 항목 (DependentUpon=Form1.cs)

**핵심 알고리즘 (P1 ClbStruList_SelectedIndexChanged)**:
```
SelectedIndex → _struNodeCache[idx] → GetChildObject3d(BODY) → memberIndices
BeginUpdate → try { RestoreColorAll → Select(true,false) → FlyToObject3d(1.2f) } finally { EndUpdate }
```

**SDK 정정 사항** (sdk-verifier 사전 검증):
- `vizcore3d.Object3D.Color.RestoreColorAll()` — `View.Color`가 아닌 `Object3D.Color`
- `vizcore3d.View.FlyToObject3d(indices, 1.2f)` — `View.Camera`가 아닌 `View` 직속
- `vizcore3d.Object3D.Select(indices, selection:true, pivot:false)`

**의미 분리** (CheckedListBox vs ListView 컨텍스트 차이 — 패턴 무비판 이식 금지):
- 체크(`CheckedItems`) = 출력 대상 표시용 (P2/P3에서 활용)
- 선택(`SelectedIndex`) = 시각 강조 전용
- `CheckOnClick=true` — 사용자 마우스 클릭 1회로 체크와 강조 동시 발생

**다중 에이전트 토론 흐름**:
1. 라운드 1 (병렬 3): sdk-verifier SDK 검증 + REQ-005 패턴 추출 + HYI 라인 매핑
2. 라운드 2 (구현 1): general-purpose 에이전트 코드 작성 위임
3. 라운드 3 (병렬 2): 패턴 일관성 리뷰 + 위험·정책 리뷰 → try/finally 누락 1건 발견·수정, R13 요지 병기 보강
4. 라운드 4: MSBuild Debug 빌드 통과 → commit + push

**검증 포인트** (사용자 실기):
- 모델 열기 → 좌측 STRU 패널에 N개 항목 표시 (NodeName 또는 NodePath 또는 `(Index N)`)
- "전체 선택/해제" 버튼 동작
- 행 클릭 시 (1) 체크 토글 + (2) 3D에서 해당 STRU의 모든 부재 빨간 강조 + 카메라 fit
- 모델 트리에 LEAF_ASSEMBLY가 NodeName으로 "/" 시작하지 않으면 fallback 작동 (LEAF_ASSEMBLY 전체 표시)
- DiagLog에서 `T-064 LeafAssy[i]: name='...' path='...' kind=... depth=...` 진단 가능

**다음 단계**: P2 — `[도면 리스트 뽑기]` 버튼 (체크된 STRU별 가시성 토글 → DetectClash → GenerateDrawingSheets → lvDrawingSheet 누적, STRU 컬럼 추가, 다중 체크 시 확인 팝업)

---

## 2026-05-13 — T-037 3차: BOM 열 너비 6/20/17/30/8/9/6/5 (합 101mm)

**유형**: feat (사용자 사양 BOM 열 너비 갱신)
**커밋**: `e607feb`
**관련 TASK**: T-037 (BOM 줄바꿈/ITEM 분리 시리즈)

**사용자 사양**: BOM 8개 열 너비 — No 5→6 / ITEM 20 / MATERIAL 12→17 / SIZE 14→30 (T-062 길이 추가 대응) / Q'TY 7→8 / T/W 8→9 / MA 5→6 / FA 6→5. 합 77 → 101mm.

**구현** ([Form1.DrawingSheets.cs:1350](A2Z/Form1.DrawingSheets.cs:1350)) — `ColumnWidths` Dictionary 값만 갱신.

**검증 포인트**:
- SIZE 30mm가 `300x250x12x1500.22` 길이 추가 후에도 줄바꿈 없이 표시되는지
- 합 101mm가 컨테이너 폭에 맞는지 (안 맞으면 SDK가 비례 축소 또는 잘림 — 별도 라운드)

---

## 2026-05-13 — T-062: BOM SPREF 파싱 확장 + POSSTART/POSEND 길이 SIZE 뒤 추가

**유형**: feat (BOM 테이블 데이터 보강)
**커밋**: `13c337d`
**관련 TASK**: T-062 (신규)

**사용자 사양**:
- **SPREF ITEM 파싱**: 기존은 첫 `/` 제거 후 `:` split. 일부 부재가 `/PART/ITEM/...` 형태 (이중 `/`). 새 규칙 — *첫 `/` 제거 후 `/` 또는 `:` 중 먼저 나오는 위치에서 split*
- **SIZE 뒤 길이 추가**: POSSTART/POSEND UDA에서 3개 숫자 추출 → 3D 거리 공식(`sqrt(dx²+dy²+dz²)`) → SIZE 형식 `숫자x숫자x숫자` 뒤에 `x{길이}` 추가
- UDA는 *기존 부모 탐색 패턴* 그대로 — 어셈블리 / 파트 어디든 자동 조회
- POSSTART/POSEND 없으면 길이 추가 안 함 (SIZE 그대로)
- 소수점은 의미 있는 자리까지 표현 (`0.##` 포맷)

**구현** ([Form1.Clash.cs](A2Z/Form1.Clash.cs) `btnCollectBOMInfo_Click`):
1. UDA 루프에 `POSSTART`/`POSEND` 캡처 추가 — 종료 조건도 5개 키로 확장
2. SPREF 파싱: `IndexOf('/')` + `IndexOf(':')` 중 `Math.Min`으로 split 위치 결정
3. 길이 계산: `posStartVal`/`posEndVal`이 있으면 `ParsePosString` 헬퍼로 3개 숫자 추출 → `sqrt` → `sizeVal`에 `x{length}` 추가
4. 신규 헬퍼 `ParsePosString(string raw)` — `Regex.Matches`로 음수 포함 숫자 토큰 매치 → 3개 미만이면 0으로 채움 (S/U 같은 토큰 의미 미정 — 등장 순서 그대로 매칭)

**가설/한계**:
- POSSTART와 POSEND의 *3개 숫자 순서가 일관*되어야 같은 축끼리 빼짐 (실기 검증)
- S/U 토큰이 *축 식별자*면 향후 정밀화 가능. 일단 순서 가정
- 한 축만 다른 일반 케이스도 3D 거리 공식 일률 적용 — 다른 축 차이 0 → 자동 처리

**검증 포인트**:
- 일반 시트 BOM 테이블 ITEM/SIZE 컬럼 확인
- `/PART/ITEM` 형태 부재의 ITEM이 첫 `/` 다음 ~ 두 번째 `/`까지로 추출되는지
- 한 축 차이 부재: SIZE = `300x250x12x1500` 형태
- 대각선 부재 (두/세 축 차이): SIZE에 정확한 3D 거리 값
- POSSTART/POSEND 없는 부재: SIZE 그대로 (길이 추가 X)

---

## 2026-05-13 — T-061: docs 한글화 (옵션 B — 폴더·파일명 + README·용어집 정비)

**유형**: docs
**커밋**: `28a7f7f`
**관련 TASK**: T-061 (DONE)
**관련 REQUEST**: — (사용자 직접 지시)

**변경 사항**:
- `docs/features/` → `docs/기능/` (폴더) + 8개 카테고리 폴더 한글화: `attribute→부재속성`, `bom→BOM`, `clash→간섭검사`, `dimensions→치수`, `drawing-sheets→도면시트`, `drawing2d→2D도면`, `global-views→글로벌뷰`, `mfg-drawing→가공도`
- 60개 기능 파일 한글화 (사용자-매뉴얼과 동일 패턴: 한글 + 띄어쓰기, 한영 혼용 OK)
- `docs/technical-notes/` → `docs/기술 노트/` + 4개 파일: `dimension-extension-line.md → 치수 보조선 사양.md`, `dimension-text-position.md → 치수 텍스트 위치.md`, `osnap-criteria.md → Osnap 기준.md`, `sheet1-naming-criteria.md → Sheet1 명명 기준.md`
- `_index.md` → `_인덱스.md` (8개)
- [docs/README.md](README.md) 이모지 전부 제거 + **"기준·사양" 진입점 섹션 신규** (보조선/치수/Osnap/Sheet1 4개 본진 한 클릭 접근) + 카테고리 표 한글화
- [docs/_glossary.md](_glossary.md): "보조선"/"Osnap"/"Chain Dimension"/"Drawing Sheet" 항목에 본진 문서 링크 4개 추가
- CLAUDE.md (R1 경로 + 파일 구조 트리), `.claude/commands/{commit,checkpoint}.md`, `.claude/hooks/docs-sync-reminder.sh` 경로 참조 갱신
- 일괄 치환 PowerShell 3패스 (디렉토리 → 파일 슬러그 → 절대경로·anchor·substring 충돌 복구) + md-link-checker 3회 검증 → **잔존 깨진 링크 0건**

**예외 (영어 유지)**:
- `code-reference/form1-*.md` — 코드 파일 1:1 매핑 보존 (`Form1.BOM.cs ↔ form1-bom.md`)
- `tracking/`, `setup/` — `.claude/commands` 통합 보존

**영향 범위**: 131개 docs 파일 (rename 64 + edit 67). 코드 변경 0. 한글화 후 "보조선 기준/치수 길이 기준" 한글 검색이 README 1클릭으로 도달

---

## 2026-05-13 — T-040 v12: v11 직각 시프트 + 임계 maxEstDist/26 + 인접 비교 부호

**유형**: feat (v11 베이스 + 사용자 사양 두 가지 추가)
**커밋**: `41649e8`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**사용자 사양 (v12)**:
- v11(=v6 직각 시프트) 베이스 유지
- **임계 13mm 고정 → `maxEstDist / 26`** (1326 모델 → 51mm 이하 처리)
- **인접 dim 비교로 shiftDir 부호 결정** ("연쇄치수값 보고 큰 값 옆이면 큰 값 쪽으로")

**v12 알고리즘** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs) `ApplyParallelTextShift` 통째 교체):
1. 1차 패스: 각 SDK measure의 MAIN 두 좌표 → `dimAxis`(차이 max 축), `dimCenter`(측정축 중심), `estDist`, 시프트용 `textPos` 수집
2. `maxEstDist` 계산. ≤ 100mm 시 전체 skip
3. `threshold = maxEstDist / 26f`
4. 측정축별 그룹 → `dimCenter` 순 정렬 → 좌·우 인접 식별
5. 인접 `estDist` 비교 → `shiftDir`:
   - 양쪽 인접 큰 쪽 / 같음 +1 / 한쪽만 반대(체인 바깥) / 없음 skip
6. v11 직각 시프트 패턴 × `shiftDir` (가로 → right, 세로 → up)
   - X뷰(right=+Y, up=+Z), Y뷰(right=-X, up=+Z), Z뷰(right=+X, up=+Y)

**부호 매핑**:
- 큰 인접이 +측정축 → 직각 + 방향(위 또는 오른쪽)
- 큰 인접이 -측정축 → 직각 - 방향(아래 또는 왼쪽)
- *임의 매핑이지만 일관성*. 결과 어색하면 부호 반전 라운드

**가공도**: v11에서 추가한 헬퍼 호출 그대로 — 가공도에도 동일 작동 (SDK measureItem 직접 순회라 chainDimensionList 무관)

**검증 포인트**:
- 100mm 이상 모델에서 51mm 같은 작은 치수도 시프트 (v6은 13mm만)
- 작은 dim 옆에 큰 dim이 있을 때 *큰 dim 방향*에 맞게 시프트
- 가공도에도 동일 작동

---

## 2026-05-13 — T-040 v11: v6(1aaf85c) 직각 시프트로 복귀 (사용자 최선 보고)

**유형**: revert (v7~v10 평행 시프트 시도 폐기 — 사용자 1aaf85c가 가장 잘 작동)
**커밋**: `f211fb3`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**사용자 보고**: "1aaf85c 이때 커밋할 때의 시프트 방법으로 돌아가자 이때가 제일 잘됐다"

**v11 변경** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs) `ApplyParallelTextShift` 헬퍼 내부 통째 교체):
- 임계: `maxEstDist / 26` → **고정 13mm** (v6 시점 복귀)
- 인접 비교 폐기 (v7~v10)
- chainDimensionList 의존 폐기 — **SDK `measureItem` 직접 순회** (v6 패턴)
- 직각 시프트 — *가로 치수(dimAxis = 화면 H) → right* / *세로 치수(dimAxis = V) → up*
- 뷰별 right/up 매핑: X뷰(right=+Y, up=+Z) / Y뷰(right=-X, up=+Z) / Z뷰(right=+X, up=+Y)
- 시프트 거리: 캔버스 3mm 그대로
- 뷰 max ≤ 100mm 시 전체 skip
- DiagLog 메시지 `T-040 TextShift` (v6 명칭)

**가공도 ([Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs))**: v7에서 제거된 시프트 호출 위치에 **헬퍼 호출 추가** → v6 시점처럼 가공도에도 시프트 적용
- 헬퍼는 SDK measureItem 직접 순회라 가공도(`chainDimensionList` 미사용)에서도 작동

**잔존 코드 (미사용)**:
- `ChainDimensionData.MeasureId` 필드 (Models.cs) — v8 도입, v11에서 미사용. 제거 가능하지만 보관
- `FindMeasureByDimCoords` 헬퍼 — v7 도입, v8부터 미사용
- `DrawDimension` `void → int` 시그니처 — v8 도입. ChainDimensionData.MeasureId 저장 코드도 잔존

**v7~v10 시도 결과 정리**:
- v7: 평행 시프트 — `chainDimensionList`/SDK 매칭 실패로 시프트 0건
- v8: SDK 측정 ID 직접 매칭 — 시프트 작동, 방향이 측정선 직각으로 강제 변환됨
- v9: offsetAxis 매핑 — v8과 동일 결과 (SDK 변환 가설 강해짐)
- v10: 측정축 복귀 — v9와 동일 결과
- → 사용자: v6가 제일 좋다 → 복귀

**검증 포인트**:
- v6 시점(1aaf85c) 그대로 작동
- 가로 치수가 화면 H 방향(좌·우), 세로 치수가 V 방향(위·아래)으로 시프트
- 가공도에도 시프트 적용됨

---

## 2026-05-13 — T-040 v10: 시프트 축 측정축 복귀 (v9 매핑 반대 보고)

**유형**: fix (v9 시프트 축이 사용자 시각과 반대 적용)
**커밋**: `ec37b53`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v9 실기 보고** (사용자): "가로 방향 보조선에서 가로로 이동중이라서 가로랑 세로 오프셋 방향을 바꿔줘 가로가 지금 좌·우로 시프트 되고 있어"
- 세로 치수(가로 보조선)의 텍스트가 H 방향(좌·우)으로 시프트됨
- 사용자 원함: 세로 치수 → V 방향(위·아래)

→ v9의 `offsetAxis` 매핑이 사용자 시각과 반대. 사용자 표현 *가로/세로 교환* = **측정축(`axis`) 직접 사용**으로 복귀

**v10 변경** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs) `ApplyParallelTextShift`):
- `offsetAxis = GetRemainingAxis(...)` 제거
- switch 인자 `offsetAxis → axis` (v8 패턴)
- 가로 치수(`axis="Y"` in X뷰) → `midY += shift` → Y 방향(H) — 사용자 보고 직각이긴 한데 평행과 동일 결과로 받아들임
- 세로 치수(`axis="Z"` in X뷰) → `midZ += shift` → Z 방향(V)

**검증 포인트**:
- 가로 치수 → 좌·우 (또는 위·아래 — SDK 변환 여부에 따라)
- 세로 치수 → 위·아래 (또는 좌·우)
- v8과 *동일 결과*면 SDK가 측정선 직각으로 강제 변환 확정 → 사용자 사양 *평행 슬라이드* 자체 불가능
- v9와 *다른 결과*면 SDK 변환 없음 + 매핑만 조정해서 작동

---

## 2026-05-13 — T-040 v9: 시프트 축을 offset 축으로 (SDK 직각 강제 대응)

**유형**: fix (v8 시프트 방향 90° 회전 결과 대응)
**커밋**: `079b341`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v8 실기 보고** (사용자): 시프트 작동 OK, 그러나 *방향이 측정선 직각*으로만 적용됨
- 좌·우 뻗는 보조선(세로 치수)의 텍스트가 *오른쪽으로* 이동 (좌·우 방향 — 측정선 직각)
- 위로 뻗는 보조선(가로 치수)의 텍스트가 *위·아래로* 이동 (측정선 직각)

→ 추정: `SetMeasureItemDistanceTextPos`가 `Vector3D`를 받지만 *측정선 직각 성분만* 적용 (평행 슬라이드 불가능)

**v9 변경**:
- `ApplyParallelTextShift`의 시프트 축을 **측정축 → offset 축**으로 교체:
  ```csharp
  string offsetAxis = GetRemainingAxis(viewDirection ?? "X", axis);
  switch (offsetAxis) { case "X": midX += shiftDir * modelShift; break; ... }
  ```
- 인접 비교(`shiftDir = ±1`) 로직은 그대로 — *부호*만 결정
- 시프트 결과: *측정선 직각 방향* (위/아래 또는 좌/우) — SDK 한계 안에서 작동
- 함수 이름 "ParallelShift"는 유지(작업 비용), 동작은 *Perpendicular* (주석 보강)

**적용 범위**: 일반 시트(제작도)만. 가공도 미적용 그대로.

**검증 포인트**:
- 가로 치수(좌·우 치수선) → 위 또는 아래로 시프트
- 세로 치수(위·아래 치수선) → 좌 또는 우로 시프트
- *부호*가 인접 큰 dim 방향과 *임의 매핑* — 결과 직관적이지 않으면 다음 라운드에 부호 결정 알고리즘 변경

**알려진 한계**:
- 사용자 원래 사양 "300mm 쪽으로 슬라이드"(측정선 평행)는 SDK 불가
- 직각 방향 시프트 부호 결정 — 인접 큰 dim 방향과 *논리적 직접 연관 없음*. 일관성만 유지

---

## 2026-05-13 — T-040 v8: SDK 측정 ID 직접 매칭 (좌표 매칭 폐기)

**유형**: fix (v7 매칭 실패로 시프트 미작동 — 옵션 C 전환)
**커밋**: `4ab1eb0`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v7 실기 보고** (사용자 2026-05-13): "각 뷰별로 DiagLog 메시지는 출력되지만 시프트가 아예 안 됨" → `FindMeasureByDimCoords` 매칭 실패가 유력 (MAIN 두 좌표가 시작/끝점이 아닌 케이스)

**XML 확인** ([VIZCore3D.NET.xml:43946](lib/VIZCore3D.NET.xml:43946)):
```
M:AddCustomAxisDistance(Axis, Vertex3D, Vertex3D)
<returns>측정(리뷰) 아이디(ID)</returns>
```
→ `int` 반환 확인. **옵션 C 가능**.

**v8 변경**:
1. **`ChainDimensionData.MeasureId` 필드 추가** ([Models.cs](A2Z/Models.cs)) — 기본값 -1
2. **`DrawDimension` 시그니처 `void → int`** ([Form1.Dimensions.cs:1080](A2Z/Form1.Dimensions.cs:1080))
   - 내부 `AddCustomAxisDistance` 반환값을 `measureId`로 받음
   - 함수 끝에서 `return measureId` (분기 모두 일관)
3. **`ShowAllDimensions` Level 0/1/2 호출자** ([L625/L636/L649](A2Z/Form1.Dimensions.cs:625)) — 반환 ID를 `dim.MeasureId`에 저장
4. **`ApplyParallelTextShift`** — `FindMeasureByDimCoords` 좌표 매칭 폐기 → `dim.MeasureId` 직접 사용

**`Form1.MfgDrawing.cs`의 DrawDimension 호출 9곳**: 시그니처 변경 후에도 *반환값 무시*로 컴파일 OK (C# 관례). 가공도는 평행 시프트 미적용이라 영향 없음.

**검증 포인트**:
- v7에서 시프트 0건이던 케이스가 v8에서 작동하는지
- DiagLog `T-040 ParallelShift ... shifted=N` 에서 N > 0
- 인접 큰 dim 쪽으로 슬라이드, 체인 끝은 바깥쪽

**남은 가설** (v8에서도 시프트 0건이면):
- `ApplyParallelTextShift` 호출 시점의 `chainDimensionList`가 비어있거나 v8 변경 전 시점 데이터
- `viewDims` 필터 (IsVisible / ViewDirection)에서 모두 제외
- → DiagLog 추가 진단 필요

---

## 2026-05-13 — T-040 v7: 평행 시프트 (직각 폐기) + BOM 1단위 아래로

**유형**: feat (평행 시프트 도입) + feat (BOM 위치 미세 조정)
**커밋**: `682bd75`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**사용자 사양 (v7)**:
- 임계: `maxEstDist / 26` 이하 치수만 시프트 (예: 1326 → 51, 100 → 3.8)
- 시프트 방식: **평행 시프트** (인접 큰 dim 쪽 측정축 평행 슬라이드) — 직각 시프트 완전 대체
- 시프트 거리: 캔버스 3mm 그대로
- 양쪽 인접: 큰 쪽 / 같으면 오른쪽
- 한쪽만 인접: 반대(체인 바깥) 쪽
- 인접 없음: skip
- BOM: 1단위 아래로

**구현** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs)):
- `ApplyParallelTextShift(viewDirection, canvasScale, measures)` 신설 — 메인 헬퍼
- `FindMeasureByDimCoords(measures, dim, axis)` 신설 — 측정축 좌표 일치로 SDK MeasureItem.ID 매칭 (옵션 A)
- 직각 시프트 블록(v3) 폐기:
  - [Form1.DrawingSheets.cs](A2Z/Form1.DrawingSheets.cs): 시프트 블록(L2017~L2127) → 헬퍼 호출 한 줄로 교체
  - [Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs): 시프트 블록 제거 (헬퍼 미적용 — `chainDimensionList` 미사용 경로)
- BOM 셀 마진: [Form1.DrawingSheets.cs:1302](A2Z/Form1.DrawingSheets.cs:1302) bottom `11 → 10`

**알고리즘 흐름**:
1. 1차 패스로 `maxEstDist` 계산 (모든 측정의 MAIN 두 좌표 최대 거리)
2. ≤100mm 모델은 전체 skip
3. `chainDimensionList`에서 viewDirection 필터 → 축별 그룹 → 측정축 좌표 순 정렬
4. 각 dim마다 좌·우 인접 dim 식별
5. `Distance` 임계 통과 + 인접 비교로 shiftDir 결정
6. 좌표 일치로 SDK MeasureItem.ID 찾기 → `SetMeasureItemDistanceTextPos` 호출

**적용 범위**: 일반 시트(제작도)만. 가공도는 *시프트 X 상태* (chainDimensionList 미사용 경로라 헬퍼 무효). 추후 별도 데이터 구조 필요

**검증 포인트**:
- 작은 치수(예: 10mm)가 인접 큰 치수 쪽으로 슬라이드되는지
- 양쪽 인접 같으면 오른쪽
- 체인 끝 dim이 *바깥쪽*으로 빠지는지
- 임계 1/26 적정성 (1326 모델에서 50mm는 시프트, 60mm는 안 됨)
- BOM 1mm 아래 위치 적정성

---

## 2026-05-13 — T-040 v6: 보조선 굵기 0.1 통일 (제작도 + 가공도)

**유형**: feat (보조선 극가늘게 → 모델 강조)
**커밋**: `1aaf85c`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v5 실기 보고** (사용자 2026-05-13): 모델 vs 보조선 비율 추가 강화 요청 → 보조선 0.1로 더 가늘게

**v6 변경** — `Set2DViewCreateObjectItemLineWidth`:
- [Form1.DrawingSheets.cs:1975](A2Z/Form1.DrawingSheets.cs:1975) — 일반 시트 0.3 → **0.1**
- [Form1.MfgDrawing.cs:1533](A2Z/Form1.MfgDrawing.cs:1533) — 가공도 메인 0.5 → **0.1**
- [Form1.MfgDrawing.cs:1986](A2Z/Form1.MfgDrawing.cs:1986) — 가공도 EA 0.5 → **0.1**

**모델 vs 보조선 비율**: 3.0 / 0.1 = **30배** (이전 10~6배)

**위험 시도** — PDF 또는 인쇄 시 0.1mm가 *사라질 가능성*:
- 화면 픽셀 1px 미만 → 안티에일리어싱 의존 (간헐적으로 안 보임)
- PDF 뷰어별 최소 라인 굵기 처리 다름
- 인쇄: 프린터 해상도(보통 0.05mm)면 OK이나 *너무 가는 인상*

**검증 포인트**:
- 화면에서 보조선이 *충분히 보이는지* (사라지면 0.2 또는 0.3으로 복귀)
- PDF 출력 후 보조선이 의도대로 표시되는지
- 치수선(`MeasureLineWidth` 0.3/0.5)은 그대로 — 일관성 원하면 별도 라운드

---

## 2026-05-13 — T-040 v5: 외곽 Osnap 복귀 롤백 + 모델 라인 굵기 2→3mm

**유형**: revert (v4 외곽 Osnap 알고리즘 폐기) + feat (모델 굵기 증가)
**커밋**: `90bcf97`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v4 실기 보고** (사용자 2026-05-13):
- v4 외곽 Osnap 복귀가 *반대 방향*(치수선에서 가장 먼 외곽 Osnap)에서 시작됨
- 사용자가 회사 PC에서 부호 반전 시도 (`if (positiveOffset)` → `if (!positiveOffset)`) — 그래도 동일 결과
- 사용자 결정: "외곽 Osnap 복귀 시도 취소" → 다른 방향(모델 굵기)로 전환

**v5 변경**:
1. **외곽 Osnap 알고리즘 전체 롤백** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs)):
   - `_osnapPool` 멤버 변수·`OSNAP_POOL_TOLERANCE` 상수 제거
   - `ResolveExtensionOrigin` 헬퍼 제거
   - `mergedPoints` 직후 `_osnapPool = ...` 갱신 제거
   - `ShowAllDimensions` Level 0/1/2 3곳의 startPoint/endPoint 교체 코드 제거 → v3 시점(`DrawDimension(dim.StartPoint, dim.EndPoint, ...)`)으로 복귀
2. **모델 라인 굵기 증가** (`ModelLineThickness` 2.0 → 3.0):
   - [Form1.DrawingSheets.cs:1392](A2Z/Form1.DrawingSheets.cs:1392)
   - [Form1.MfgDrawing.cs:691](A2Z/Form1.MfgDrawing.cs:691)

**근거** (사용자 사양 전환):
- 보조선이 모델 관통하는 문제 → *외곽 점에서 시작* 시도는 알고리즘 복잡 + 결과 어긋남
- 대안: *모델 라인을 보조선보다 진하게* → 시각적 우선순위 → 보조선이 덜 거슬림
- 보조선 굵기 0.3~0.5mm 그대로. 모델 3.0 → 6~10배 차이

**적용 범위**: 일반 시트 + 가공도 메인 모두

**남아있는 v3 동작** (변경 없음):
- 치수 텍스트 시프트 (≤13mm, 캔버스 3mm)
- 치수축별 시프트 방향 (가로→right / 세로→up)
- max≤100mm skip
- Z뷰 up 부호 +Y (v4에서 보정한 거 유지)

**검증 포인트**:
- 모델이 보조선보다 *눈에 띄게 진한지* (3mm 적정한지 / 더 굵게 / 덜 굵게)
- 부재 윤곽 가독성 변화

---

## 2026-05-13 — T-040 v4: 외곽 Osnap 보조선 복귀 + Z뷰 up 부호 보정

**유형**: feat (보조선 모델 관통 해소) + fix (Z뷰 up 부호)
**커밋**: `84b852e`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v3 실기 보고** (사용자 2026-05-13):
- v3 시프트 방향 OK
- Z뷰 세로 치수 up 부호가 -Y였는데 +Y로 보정 필요
- **새 문제**: 제작도 보조선이 *모델 내부 Osnap*에서 시작 → 모델 관통

**사용자 사양**:
- Osnap 필수 점만 남기고 다 지운 다음, *필수 점에 수직/수평하는 제일 먼 외곽 점*을 다시 살려 그 점에서 보조선 시작
- 수직/수평 = 현재 뷰의 화면 가로/세로 기준
- 제일 먼 = 치수선에 가장 가까운 외곽 Osnap
- 그 직선상 외곽 Osnap 없으면 P 그대로 (관통 감수 fallback)

**v4 변경**:
1. **외곽 Osnap 복귀** ([Form1.Dimensions.cs](A2Z/Form1.Dimensions.cs)):
   - `_osnapPool` 멤버 변수 신설 — `mergedPoints`(원본 Osnap 풀) 보존
   - `ResolveExtensionOrigin(p, dimAxis, offsetAxis, positiveOffset)` 헬퍼 신설
   - 알고리즘: P의 치수축·비-offset 축 좌표 고정 → offset 축 직선 위 원본 Osnap 검색 → `positiveOffset` 방향으로 P 너머 + 치수선에 가장 가까운 점 Q 선택 → 보조선 시작점을 Q로 교체
   - 기존 `axisPositiveOffset`/`GetRemainingAxis` 그대로 재사용
   - `ShowAllDimensions` Level 0/1/2 3곳에서 `DrawDimension` 호출 직전 적용
   - 풀에 없으면 P 그대로 (fallback)

2. **Z뷰 세로 치수 up 부호 보정** (DrawingSheets.cs + MfgDrawing.cs):
   - dimAxis='Y' 시프트 `Y - modelShift` → `Y + modelShift`

**적용 범위**: 일반 시트(제작도) — `_osnapPool`은 치수추출 시점에만 갱신. 가공도 별도(다음 라운드)

**검증 포인트**:
- Hole 중심 등 모델 내부 Osnap에서 시작하던 보조선이 *부재 표면*에서 시작하는지
- Z뷰 세로 치수 시프트가 *위*로 향하는지 (이전엔 아래)
- 같은 직선상 외곽 Osnap 없는 케이스에서 fallback(P 그대로) 작동

---

## 2026-05-13 — T-040 v3: 가로/세로 시프트 방향 스왑 (v2 반대)

**유형**: fix (v2 시프트 방향 반대 적용 보고)
**커밋**: `a5ca1a0`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v2 실기 보고** (사용자 2026-05-13): "지금 반대로 적용되어 있어"
- v2 가로 치수(dimAxis=H) → up 시프트했는데 사용자 사양은 right
- v2 세로 치수(dimAxis=V) → right 시프트했는데 사용자 사양은 up

**v3 변경**: 시프트 방향 분기 스왑 (일반 시트 + 가공도 메인)
- 가로 치수(dimAxis가 화면 H) → **right** 시프트
- 세로 치수(dimAxis가 화면 V) → **up** 시프트
- 부호 유지 (X뷰 right=+Y/up=+Z, Y뷰 right=-X/up=+Z, Z뷰 right=+X/up=-Y)

**검증 포인트**: 가로 보조선 수치가 오른쪽으로, 세로 보조선 수치가 위로 빠지는지

---

## 2026-05-13 — T-040 v2: 치수축별 시프트 방향 + max≤100mm skip + 30→3mm

**유형**: fix (v1 사양 보강 — 가로 보조선 미작동 해결)
**커밋**: `3f307b3`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**v1 실기 보고** (사용자 2026-05-13):
- 아래로 뻗는 10mm (세로 보조선): ✅ 옆으로 시프트
- 오른쪽으로 뻗는 10mm (가로 보조선): ❌ 보조선 안 갇힘 — 시프트 방향이 항상 화면 H라 보조선과 같은 방향으로 따라감

**v2 변경**:
1. **시프트 방향 치수축별 분기** — MAIN 두 좌표 차이가 가장 큰 축을 치수축으로 추정. 치수축이 화면 H면 **up(위)** / V면 **right(오른쪽)** 시프트. 뷰별 매핑: X뷰(H=Y,V=Z) / Y뷰(H=X,V=Z) / Z뷰(H=X,V=Y, up=-Y 가설)
2. **뷰 max estDist ≤ 100mm 시 전체 skip** — 작은 모델 시프트 불필요. 1차 패스로 maxEstDist 계산
3. **시프트 거리 30 → 3mm**

**적용 경로**: 일반 시트 + 가공도 메인

**검증 포인트** (사용자 사내 PC):
- 가로 보조선 10mm가 위로 빠지는지
- 100mm 이하 작은 모델 뷰에서 시프트 0건 (DiagLog `skip (maxEstDist=...)`) 확인
- 3mm 크기 적정성 / Z뷰 up 부호 (-Y 가설)

---

## 2026-05-13 — T-040: 치수 텍스트 SetMeasureItemDistanceTextPos 전환 PoC

**유형**: feat (Softhills 담당자 예제 기반 신규 방식 도입)
**커밋**: `63b9659`
**관련 TASK**: T-040 (IN_PROGRESS — 실기 검증 대기)

**배경**: `AlignDistanceTextPosition` 토글(1=위 / 2=바깥)이 실기에서 작동 안 함을 사용자 보고. 담당자(Softhills) 예제 코드에 따라 `Drawing2D.Measure.SetMeasureItemDistanceTextPos(int id, Vector3D pos)`로 텍스트 위치를 절대 좌표 시프트.

**구현 변경**:
- 일반 시트 ([Form1.DrawingSheets.cs](A2Z/Form1.DrawingSheets.cs) `RenderSheetViewForDrawing`): `Add2DObjectFromShapeDrawing` ~ `Add2DMeasureFrom3DMeasure` 사이에 ≤13mm 측정 텍스트를 화면 오른쪽 **캔버스 30mm** 시프트. 모델 mm 환산 = `30 / GetObjectScale(objId)`. ISO 뷰 제외.
- 가공도 메인 ([Form1.MfgDrawing.cs](A2Z/Form1.MfgDrawing.cs) `ExecuteMfgDrawing`): 동일 패턴.
- 토글 코드 폐기: Dimensions.cs 5곳 (`AlignDistanceTextPosition = 2` 초기값 2곳 + `applyTextPosition` 람다 + 호출 3곳 + 선택 치수 표시 dim별 토글), MfgDrawing.cs 3곳 (`mfgStyle`/`eaStyle` 초기값).

**SDK 확정 사실** (sdk-verifier + 빌드 시도):
- `SetMeasureItemDistanceTextPos(int, Vector3D)` 실재 — XML 미문서 / 어셈블리 존재 (빌드 통과로 확정)
- `MeasureItem.Position` = `List<ReviewPosition>`, `ReviewPosition.Position` = **`Vertex3D`** (담당자 예제 `Vector3D` 오기)
- **`MeasureItem.Distance` 속성 부재** → MAIN 두 좌표 거리로 추정 (옵션 A)
- `ReviewPosition.DataKind` enum = `MAIN`/`SUB` 두 값

**카메라 right 매핑 가설** (실기 검증 필요):
| viewDirection | 카메라 | 화면 오른쪽 3D 축 |
|---|---|---|
| `"X"` | X_PLUS | +Y |
| `"Y"` | Y_PLUS | -X |
| `"Z"` | Z_PLUS | +X |
| `"ISO"` | ISO_PLUS | — (스킵) |

**검증 포인트** (사용자 사내 PC):
- DiagLog `T-040 TextShift view=X canvasScale=0.0xxx modelShift=Nmm shifted=N` 라인 확인
- 30mm가 실기에서 시각적으로 적절한 크기인지
- 카메라 right 매핑 부호 (틀리면 반대 방향으로 시프트)
- MAIN 두 좌표로 추정한 거리가 13mm 필터에 정확히 동작하는지

**보류** (별도 라운드):
- 가공도 EA 두 번째 뷰 (L1905) — 90° 회전 후 카메라 식별 별도
- 가공도 MULTI 경로 — 별도

**영향 범위**: 4경로 중 2경로 (일반 시트 + 가공도 메인)

---

## 2026-05-12 — T-038+039 v10: 모든 보조선 5/10mm 고정 (단순화)

**유형**: refactor (사용자 사양 단순화)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**사용자 사양 (2026-05-12)**: *"전부 다 지금 제일 짧은 보조선이 몇이지? 고정값으로 해도 되겠다 제일 짧은 보조선으로 고정값"*

**답**: v7 기준 제일 짧은 보조선 = **1단 5mm / 2단 10mm** (max>1000mm + 짧은 축 조건). 이 값으로 모두 고정.

**v10 단순화**:

| 폐기 | 의미 |
|---|---|
| max>1000mm 분기 (10/20 vs 20/40) | 모두 5/10mm 고정 |
| axisShortHalf hAxis_3d 무조건 (v5) | 더 이상 의미 없음 (어차피 모두 동일) |
| axisShortHalf 짧은 축 1/2 이하 (v7) | 동일 |

**구현** ([Form1.Dimensions.cs:509~](A2Z/Form1.Dimensions.cs:509)):
```csharp
if (canvasScaleOverride > 0f && filteredDims.Count > 0)
{
    const float canvasBase = 5f;   // 고정
    const float canvasLvl  = 5f;   // 차분 → 2단 = 10mm
    canvasMaxOff = canvasBase + canvasLvl;
    baseOffset = canvasBase / canvasScaleOverride;
    levelSpacing = canvasLvl / canvasScaleOverride;
    // axisShortHalf 비어있음 → foreach 분기 모두 동일 offset
}
```

**효과**:
- 모든 dim 보조선 1단=5mm / 2단=10mm (캔버스 절대)
- 모델 이동량 canvasMaxOff = 10mm (axisShortHalf 비어있어 절반 적용 X) × ShiftScale
- ShiftScale 매트릭스 (v8) 그대로 유지

**검증 포인트** (사용자 사내 PC):
- 모든 뷰의 모든 보조선이 *동일 길이* (5/10mm 고정)
- 모델 이동량 안정 (ShiftScale 비대칭만 영향)

**잔여 (필요 시)**:
- 5/10mm 부족 → 6/12 또는 다른 고정값
- 또는 *부재 위치별* 동적 (예: 외곽 부재만 절반) — 사용자 결정

---

## 2026-05-12 — ISO 풍선 좌/우만 + BOM·도면정보 누적 이동

**유형**: feat (사용자 사양 3건)
**커밋**: (이번 커밋)
**관련 TASK**: T-006 / FB-004 / 도면정보 영역

**사용자 사양 (2026-05-12)** — 3건:
1. ISO 풍선 *좌/우만* 향하게 (위/아래 배치 X)
2. BOM 테이블 위 1 + 오른쪽 1 더 (누적 위 1, 오른쪽 2)
3. 도면정보 테이블 위 2 + 오른쪽 1 더 (누적 위 3, 오른쪽 2)

**구현**:

| # | 위치 | 변경 |
|---|---|---|
| 1a | [Form1.DrawingSheets.cs:911~](A2Z/Form1.DrawingSheets.cs:911) | `initDir` 결정 — `initDirX = sign(bom.CenterX - mCenterX)`, `initDirY = 0` (수평 강제) |
| 1b | [Form1.DrawingSheets.cs:955~](A2Z/Form1.DrawingSheets.cs:955) | 회피 알고리즘 — 회전 제거, Y 슬롯만. `attempt % 4` 패턴: 0=정위치, 1=거리+15%, 2=Y+슬롯, 3=Y-슬롯, 4=거리+30% ... |
| 2 | [Form1.DrawingSheets.cs:1289](A2Z/Form1.DrawingSheets.cs:1289) | BOM 셀(1,3) `SetGridCellMargins(1, 3, 12f, 10f, 10f, 11f)` (left 11→12, bottom 10→11) |
| 3 | [Form1.DrawingSheets.cs:1290](A2Z/Form1.DrawingSheets.cs:1290) | tableInfo 셀(2,3) `SetGridCellMargins(2, 3, 12f, 10f, 10f, 13f)` (left 11→12, bottom 11→13) |

**풍선 회피 알고리즘 (수평 전용)**:
```
yShift = ((attempt % 4) / 2) × (balloonHalfH × 2.5)
if ((attempt % 4) == 3) yShift = -yShift
newOffset = perMemberOffset × (1 + (attempt / 4) × 0.15)
noteX = bom.CenterX + initDirX × newOffset
noteY = bom.CenterY + yShift
```

**회피 매트릭스 (attempt % 4)**:
| attempt | 거리 증가 | Y 슬롯 |
|---|---|---|
| 0 | +0% | 0 |
| 1 | +15%? (attempt/4=0 이라 0%) | 0 |
| 2 | +15% (attempt/4=0이라 0%) | +1×Y |
| 3 | +15% (attempt/4=0이라 0%) | −1×Y |
| 4 | +15% | 0 |
| 8 | +30% | 0 |

수정 — attempt/4가 *나누기 정수*라 1, 2, 3은 0이고 4부터 1. 즉 거리 증가 4 단계마다. 사이에 Y 슬롯 변화.

**테이블 마진 누적 매트릭스**:
| 셀 | left | bottom | 이동 의미 |
|---|---|---|---|
| BOM (1,3) | 12 | 11 | 오른쪽 2 + 위 1 |
| tableInfo (2,3) | 12 | 13 | 오른쪽 2 + 위 3 |

**검증 포인트** (사용자 사내 PC):
- ISO 풍선이 *부재 좌/우*에만 (위/아래 X)
- BOM이 *오른쪽으로 1, 위로 1* 추가 이동
- 도면정보가 *오른쪽 1, 위로 2* 추가 이동

---

## 2026-05-12 — ISO 풍선 외곽 부재 거리 절반 (셀 침범 방지)

**유형**: feat (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-006 / FB-004 (ISO 풍선 영역)

**사용자 사양 (2026-05-12)**: *"풍선을 모두 같은 길이로 바깥에 배치하니까 중앙 안쪽 부재 풍선은 알맞은데, 가장자리 부재는 셀을 나가서 풍선 표시됨. 외곽 부재 풍선 길이 반으로 줄이면 좋겠음."*

**판정 기준**: 부재 중심을 ISO 투영한 (bh, bv)가 모델 BBox 중심에서 **정규화 거리 > 0.5**면 외곽.

```
distH = |bh - modelCenterH|, distV = |bv - modelCenterV|
normalizedDist = max(distH / modelHalfH, distV / modelHalfV)  // 0~1
isOuter = (normalizedDist > 0.5)
perMemberOffset = isOuter ? baseOffsetDist × 0.5 : baseOffsetDist
```

**구현** ([Form1.DrawingSheets.cs:883~](A2Z/Form1.DrawingSheets.cs:883)):

| 위치 | 변경 |
|---|---|
| L883 (aabbPad 전) | `modelH/V_min/max_orig` + `modelCenterH/V_orig` + `modelHalfH/V_orig` 보존 (충돌 검사용 패딩 제외) |
| L918 (각 부재 처리) | 부재 중심 isoProject → `normalizedDist` 계산 → `perMemberOffset` 결정 |
| L919~920 (noteX/Y 초기 위치) | `baseOffsetDist` → `perMemberOffset` |
| L959 (회피 newOffset) | `baseOffsetDist` → `perMemberOffset` |

**효과**:
- 중앙 50% 영역 부재: 풍선 거리 그대로 (baseOffsetDist)
- 외곽 50% 영역 부재: 풍선 거리 절반 → 셀 안에 들어옴
- 회피 알고리즘도 perMemberOffset 기준 → 외곽 부재 회피도 절반 거리에서 시작

**검증 포인트** (사용자 사내 PC):
- 외곽 부재 풍선이 *셀 안*에 들어옴
- 중앙 부재 풍선 그대로 (모델 밖 적절 위치)
- 풍선 충돌 회피 정상 동작

**잔여**:
- 정규화 기준 0.5 조정 (0.3 또는 0.7)
- 가장자리 강도 단계화 (2단계 → 3단계 분류) — 필요 시

---

## 2026-05-12 — T-038+039 v9: ISO 잔존 이동 버그 차단 + Z뷰 추가 5% 축소

**유형**: fix + feat (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**사용자 질의 + 사양 (2026-05-12)**:
1. *"ISO 뷰도 이동하는게 있어?"* → **버그 발견**
2. *"Z평면도는 스케일 조금 더 줄여야겠다 5정도 더 줄여줘"* → 0.75 → 0.70

**버그 분석**:
- `_lastModelShiftCanvasX/Y`가 멤버 변수, `RenderSheetViewForDrawing` 시작부 초기화 X
- 4뷰 순서 **ISO → Z → Y → X**. ISO는 `ShowAllDimensions` 안 호출
- → ISO 처리 시점에 *이전 뷰/시트의 마지막 shiftXY 값* 잔존 → 의도 안 한 이동
- 첫 시트 ISO만 0 시작, 이후 시트는 이전 X뷰 값 잔존

**구현**:

| # | 위치 | 변경 |
|---|---|---|
| 1 | [Form1.DrawingSheets.cs:1503~](A2Z/Form1.DrawingSheets.cs:1503) | `RenderSheetViewForDrawing` 진입부에 `_lastModelShiftCanvasX = 0f; _lastModelShiftCanvasY = 0f;` 명시 초기화 |
| 2 | [Form1.DrawingSheets.cs:1907~](A2Z/Form1.DrawingSheets.cs:1907) | objId RescaleObject 분기 — `shrinkFactor = (viewDirection == "Z") ? 0.70f : 0.75f` 뷰별 차등 |

**스케일 매트릭스 (v9)**:
| 뷰 | 스케일 |
|---|---|
| ISO | 0.75 (변화 없음) |
| **Z (평면도)** | **0.70** (사용자 사양 — 5% 더 축소) |
| X / Y | 0.75 (변화 없음) |
| bgObjId (ISO Sheet 2+ 배경) | 0.75 (L1731, viewDirection="ISO"이라 분기 영향 X) |

**효과**:
- ISO 뷰가 *정확히* 셀 중앙(이동 0)에 위치
- Z뷰 모델 5% 더 작아 셀 안 여유 확보

**검증 포인트** (사용자 사내 PC):
- ISO 뷰 *정중앙* 배치 (이동 X)
- Z뷰 모델이 다른 뷰 대비 *살짝 작음*
- 4뷰 시각 균형

---

## 2026-05-12 — T-038+039 v8: 뷰별 차등 vShiftScale 공식

**유형**: fix (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**v7 검증 결과** (사용자 보고): Z뷰는 잘 이동, X뷰/Y뷰는 위로 더 이동 필요.

**v8 공식 — 뷰별 차등 vShiftScale**:

| 조건 | Z뷰 | X뷰 / Y뷰 |
|---|---|---|
| vPositive=true (외곽 위, 모델 아래 이동) | 0.25 | 0.25 |
| **vPositive=false (외곽 아래 = 치수 아래, 모델 위 이동)** | **0.5** | **0.75** |

H 이동: hShiftScale = 0.25 (모든 뷰), Y뷰 dx 부호 반전 유지.

**구현** ([Form1.Dimensions.cs:610~](A2Z/Form1.Dimensions.cs:610)):
```csharp
float vShiftScale;
if (vPositive)
    vShiftScale = 0.25f;
else
    vShiftScale = (viewDirection == "Z") ? 0.5f : 0.75f;
```

**검증 포인트** (사용자 사내 PC):
- X뷰·Y뷰 치수 아래 케이스: 모델이 *위로 더 이동* (v7 대비 1.5배)
- Z뷰: 변화 없음 (그대로)
- 라벨 가림 해소 확인

**잔여 (필요 시)**:
- 0.75도 부족하면 1.0 또는 더 키움
- 또는 *비례식* (모델 크기 기반)으로 동적 결정

---

## 2026-05-12 — T-038+039 v7 + BOM/tableInfo 미세 이동

**유형**: feat (사용자 사양 4건)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**사용자 사양 (2026-05-12) — 4건**:
1. BOM 테이블 오른쪽으로 1
2. 도면정보 테이블 오른쪽 + 위로 1
3. 짧은 축 보조선 절반 기준 1/3 → 1/2 (v3 부활 + v5 위아래 결합)
4. Y/X 뷰에서 치수 아래로 그려진 모델이 라벨 가림 → 위로 더 이동

**사용자 질문 답**: 현재(v6) 이동 조건 = `외곽 반대 방향 × canvasMaxOff × 0.25 (균등) + Y뷰 dx 반전`. *vPositive=false(치수 아래) 시 위 이동량 키움*으로 4번 해결.

**구현**:

| # | 위치 | 변경 |
|---|---|---|
| 1 | [Form1.DrawingSheets.cs:1287](A2Z/Form1.DrawingSheets.cs:1287) | `SetGridCellMargins(1, 3, 11, 10, 10, 10)` — BOM 셀 왼쪽 마진 +1 (오른쪽 1mm) |
| 2 | [Form1.DrawingSheets.cs:1288](A2Z/Form1.DrawingSheets.cs:1288) | `SetGridCellMargins(2, 3, 11, 10, 10, 11)` — tableInfo 왼쪽+하단 +1 (오른쪽 + 위로) |
| 3 | [Form1.Dimensions.cs:520~](A2Z/Form1.Dimensions.cs:520) | axisShortHalf 결정 — v5(hAxis_3d 무조건) + v3 부활(짧은 축 *1/2 이하*, 기존 1/3 변경). 두 조건 결합 (HashSet 자동 중복 제거) |
| 4 | [Form1.Dimensions.cs:610~](A2Z/Form1.Dimensions.cs:610) | ShiftScale 비대칭 — `vShiftScale = vPositive ? 0.25f : 0.5f`. 위 이동(외곽 아래) 시 2배. hShiftScale 0.25 유지 |

**v7 적용 매트릭스**:
| 조건 | V 이동량 | 의미 |
|---|---|---|
| vPositive=true (외곽 위, 모델 아래 이동) | × 0.25 | 라벨 안전 유지 |
| vPositive=false (외곽 아래 = 치수 아래, 모델 위 이동) | × 0.5 | 라벨 가림 해소 (2배 위로) |
| H 이동 | × 0.25 (Y뷰는 부호 반전) | 균등 |

**axisShortHalf 결합 규칙**:
1. `hAxis_3d` (위아래 보조선 빠지는 축) — 무조건 추가
2. `axisMaxes`에서 `kv.Value < globalMaxMax / 2f` — 짧은 축 (1/2 이하) 추가
3. HashSet이라 둘 다 해당하면 한 번만 (= 0.5배 한 번만)

**DiagLog v7**:
- `T-038+039 v7 view=X ... shortAxes=[X,Y] axisMaxes=[X=60,Y=1500]`
- `T-038+039 v7 ModelShift ... hShiftScale=0.25 vShiftScale=0.5 hSign=N → shiftXY=(N, N)`

**검증 포인트**:
- BOM/tableInfo 1mm 이동 (시각 확인)
- 짧은 축(1/2 이하)의 보조선 절반 — 이전 1/3 기준 케이스에서 효과 명확
- Y/X 뷰에서 치수 아래일 때 모델 *2배 위로 이동* → 라벨 가림 해소

---

## 2026-05-12 — T-038 step C: 라벨 영역 명시 차단 (SetGridCellMargins bottom=12mm)

**유형**: feat (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 (IN_PROGRESS, step C 완료)
**관련 FEEDBACK**: FB-004

**사용자 사양 (2026-05-12)**: *"명시적으로 침범하지 못하게는 못하려나?"* — v6의 이동량 축소(우회 방식)보다 *근본 차단* 원함.

**해결**: `Drawing2D.GridStructure.SetGridCellMargins(row, col, left, right, top, bottom)` SDK API로 *각 셀 하단 마진을 라벨 영역 크기만큼 키움*.
- 마진 영역은 `FitObjectToGridCellAspect` 의 *fit 대상에서 제외*
- 모델이 *마진 안에 그려질 수 없음* (SDK 보장)
- 라벨은 `RenderTemplateOnGridStructure`로 *셀 하단 마진 영역 안*에 그려짐 → 자연 분리

**구현** ([Form1.DrawingSheets.cs:1275~](A2Z/Form1.DrawingSheets.cs:1275)):

```csharp
vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);  // 기존 일괄

// 4뷰 셀 (1,1)(1,2)(2,1)(2,2) Bottom 마진 12mm — 라벨 영역 차단
const float LABEL_BOTTOM_MARGIN = 12f;
vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(1, 1, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(1, 2, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(2, 1, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(2, 2, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
```

**LABEL_BOTTOM_MARGIN = 12mm 산정**:
- 라벨 텍스트 높이 4mm (`Set2DViewCreateObjectItemTextHeight(4f)` L1340)
- 박스 패딩 + 보조선 일부 여유 → 12mm

**효과**:
- 모델이 셀 하단 12mm 안 그려짐 (FitObjectToGridCellAspect가 마진 제외 fit)
- `RescaleObject(0.75배)` 후도 영역 안 유지
- `MoveObject` v6 (ShiftScale=0.25) 이동도 *마진 영역 침범 X* (SDK 동작 가정)
- 라벨은 셀 하단 Center에 별도 그려져 *자연 분리*

**검증 포인트** (사용자 사내 PC):
- 모델이 *라벨 영역과 겹치지 않음* (12mm 분리)
- 모델 영역이 *step B-3 대비 살짝 작아짐* (마진 +2mm 효과)
- 보조선·치수도 마진 안 침범 (SDK 일관성)

**잔여 (검증 결과 따라)**:
- LABEL_BOTTOM_MARGIN 조정 (12 → 10 또는 15)
- MoveObject가 *마진 영역까지 이동*하면 → 별도 V- 이동 clamp 추가 필요

---

## 2026-05-12 — T-038+039 v6: Y뷰 dx 반전 + 이동량 × 0.25 (라벨 침범 방지)

**유형**: fix (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**v5 검증 결과** (사용자 보고):
1. Y뷰에서 모델이 *왼쪽으로* 이동했는데 보조선도 왼쪽 — 같은 방향(잘못)
2. 모델이 *라벨 영역까지 침범*해 라벨과 겹침 (라벨은 제자리 그대로)

**원인 진단**:
1. SDK Y+ 카메라 좌표계에서 *3D X+ = 화면 왼쪽* (오른손 좌표계 추정). 다른 뷰는 정상으로 추정.
2. ShiftScale 0.5도 큼 — 셀 하단 *라벨 영역* (~5~10mm)까지 모델이 침범

**v6 변경 (2가지)**:

| # | 위치 | 변경 |
|---|---|---|
| 1 | [Form1.Dimensions.cs:600~](A2Z/Form1.Dimensions.cs:600) | `ShiftScale 0.5f → 0.25f` (이동량 절반 추가 축소) |
| 2 | [Form1.Dimensions.cs:600~](A2Z/Form1.Dimensions.cs:600) | Y뷰 한정 `hSign = -1f` (dx 부호 반전) — X뷰·Z뷰는 그대로 |

**적용 공식**:
```
hSign = (viewDirection == "Y") ? -1f : 1f
_lastModelShiftCanvasX = (hPositive ? -canvasHOff : canvasHOff) * 0.25f * hSign
_lastModelShiftCanvasY = (vPositive ? -canvasVOff : canvasVOff) * 0.25f
```

**DiagLog v6**: `T-038+039 v6 ModelShift view=Y ... hSign=-1 → shiftXY=(N, N)`

**검증 포인트** (사용자 사내 PC):
- Y뷰 모델이 *보조선 반대 방향*으로 이동 (이전 왼쪽 → 오른쪽)
- 모든 뷰 모델이 *라벨 영역 침범 X* (이전보다 더 작은 이동)
- X뷰·Z뷰 이동 방향 정상 (검증)

**잔여**:
- X뷰·Z뷰가 정상이 아니면 → 해당 뷰도 `hSign` 매핑 추가
- 이동량 더 줄이거나 늘림 (ShiftScale 조정)
- vSign (V 방향) 검증 — 현재는 모든 뷰 통일

---

## 2026-05-12 — T-038+039 v5: 위아래 보조선 무조건 절반 + 이동량 × 0.5

**유형**: fix (사용자 사양 v5)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**v4 검증 결과** (사용자 보고):
- 모델이 *너무 많이 이동*
- 추가 사양: "X, Y, Z 모두 위아래로 표시되는 보조선은 조건부가 아니라 그냥 절반"

**v5 변경 — 두 가지**:

1. **axisShortHalf 결정 방식 교체** (조건부 짧은 축 → 무조건 화면 V 축)
   - 기존(v3): `axisMaxes` 비교 후 1/3 이하인 축
   - 새(v5): `hAxis_3d` 항상 포함 (viewDirection별: Z뷰→X, X뷰→Y, Y뷰→X)
   - 효과: 화면 V 방향 보조선(= hAxis_3d 축 dim의 보조선) 무조건 절반

2. **모델 이동량 × 0.5** (너무 많이 이동 해결)
   - `ShiftScale = 0.5f` 상수 추가
   - `_lastModelShiftCanvasX/Y` 계산에 곱셈

**구현**:

| 위치 | 변경 |
|---|---|
| [Form1.Dimensions.cs:520~](A2Z/Form1.Dimensions.cs:520) | v3 짧은 축 조건부 코드 제거 → viewDirection별 hAxis_3d 결정 후 axisShortHalf 추가 |
| [Form1.Dimensions.cs:600~](A2Z/Form1.Dimensions.cs:600) | 모델 이동량 계산에 `ShiftScale = 0.5f` 곱셈 |

**DiagLog v5**:
- `T-038+039 v5 view=Z maxDist=N canvasBase=N canvasLvl=N scale=N → ... verticalHalfAxis=X` (X뷰면 Y 등)
- `T-038+039 v5 ModelShift ... ShiftScale=0.5 → shiftXY=(N, N)`

**검증 포인트** (사용자 사내 PC):
- 화면 V 방향 보조선이 *항상 절반* (이전 짧은 축 조건과 무관)
- 모델 이동량이 *이전의 절반*
- 시각적으로 균형 잡혔는지 (셀 안 모델 + 치수 공간)

**잔여 (검증 결과 따라)**:
- 이동량 ShiftScale 추가 조정 (0.5 → 0.3 또는 0.7)
- 화면 H 방향 보조선도 절반 필요? (현재는 기존 길이)
- 가공도 동일 적용

---

## 2026-05-12 — T-038+039 v4: 보조선 반대 방향 모델 이동

**유형**: feat (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 / T-039 (IN_PROGRESS)

**사용자 사양 (2026-05-12)**: *"보조선이 나간 방향 반대쪽으로 그리드 안의 모델을 반대 방향의 보조선 길이만큼 이동"* — 셀 안 시각 균형. 모델이 한쪽에 쏠리지 않고 보조선·치수 영역과 함께 자연 배치.

**알고리즘**:
1. ShowAllDimensions에서 `axisPositiveOffset` + `canvasMaxOff` + `axisShortHalf` 결정
2. viewDirection별 3D 축 → 화면 H/V 매핑:
   - Z뷰: H=X, V=Y
   - X뷰: H=Y, V=Z
   - Y뷰: H=X, V=Z
3. 화면 H 외곽 = `vAxis_3d` dim의 positiveOffset / 화면 V 외곽 = `hAxis_3d` dim의 positiveOffset
4. 짧은 축이면 거리 절반 (v3 규칙 재사용)
5. 모델 이동량 = 외곽 *반대* 방향
6. `Drawing2D.Object2D.MoveObject(objId, dx, dy)` 호출 — RescaleObject 후

**구현**:

| 위치 | 변경 |
|---|---|
| [Form1.cs:115~](A2Z/Form1.cs:115) | 멤버 변수 `_lastModelShiftCanvasX, _lastModelShiftCanvasY` 신설 |
| [Form1.Dimensions.cs:500~](A2Z/Form1.Dimensions.cs:500) | `canvasMaxOff` 분기 밖 선언 — 분기 안에서 값 설정 |
| [Form1.Dimensions.cs:537~](A2Z/Form1.Dimensions.cs:537) | `_lastModelShift*` 초기화 |
| [Form1.Dimensions.cs:570~](A2Z/Form1.Dimensions.cs:570) | axisPositiveOffset 결정 후 모델 이동량 계산 + DiagLog |
| [Form1.DrawingSheets.cs:1731~](A2Z/Form1.DrawingSheets.cs:1731) | bgObjId RescaleObject 후 `MoveObject(bgObjId, dx, dy)` |
| [Form1.DrawingSheets.cs:1907~](A2Z/Form1.DrawingSheets.cs:1907) | objId RescaleObject 후 `MoveObject(objId, dx, dy)` |

**DiagLog v4**:
- `T-038+039 v4 ModelShift view=X hAxis=X vAxis=Y hPositive=N vPositive=N canvasH=N canvasV=N → shiftXY=(X, Y)`
- `T-038+039 v4 MoveObject objId=N dx=N dy=N`

**적용 범위**:
- 2D 출력 X/Y/Z 셀: 적용 (canvasScaleOverride > 0 + viewDirection != null)
- ISO 뷰: ShowAllDimensions 호출 X (CreateIsoBalloonNotes만) → _lastModelShift=0 → 이동 X
- 글로벌 X/Y/Z 뷰: canvasScaleOverride 미전달 → 이동 X (기존 동작)
- 가공도(MfgDrawing): 별도 코드 (이번 변경 영향 없음)

**검증 포인트** (사용자 사내 PC):
- 4뷰 모델이 *치수 영역 반대쪽으로* 이동했는지
- 시각적으로 *모델 + 치수가 셀 안 균형*잡혀 보이는지
- *카메라 축 → 화면 방향* 매핑 검증 (Y+ = 화면 위 가설). 매핑 잘못이면 *반대 방향으로 이동* — 부호 조정 필요

**잔여 (검증 결과 따라)**:
- 부호 조정 (각 뷰별 +/- 매핑) — DiagLog 보고 결정
- 가공도(MfgDrawing) 동일 적용

---

## 2026-05-12 — T-044 시초 + 텍스트 5배→2배 조정

**유형**: feat (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-044 시초 (홀 풍선 제작도 X / 가공도 O 정리)
**관련 FEEDBACK**: —

**사용자 사양 (2026-05-12)**:
1. 텍스트 5배 → **2배**로 조정 (이전 검증 OK, 5배 너무 컸음)
2. **홀/슬롯홀/EarthBoss 풍선** — ISO 빼고는 가공도에만 표현. 즉 *2D 출력의 X/Y/Z 셀*에는 표현 X

**구현**:

| # | 위치 | 변경 |
|---|---|---|
| 1a | [Form1.DrawingSheets.cs:1368](A2Z/Form1.DrawingSheets.cs:1368) | `MeasureTextHeight(25f → 10f)` (2배) |
| 1b | [Form1.DrawingSheets.cs:1935](A2Z/Form1.DrawingSheets.cs:1935) | 풍선 `TextHeight(26.25f → 10.5f)` (2배) |
| 2 | [Form1.Dimensions.cs:905~](A2Z/Form1.Dimensions.cs:905) | `foreach (var entry in balloonEntries)` 직전에 `if (forDrawing2D) balloonEntries.Clear();` 한 줄. 표시 차단 |

**balloonEntries 차단 동작**:
- 일반 시트 2D 출력 (`forDrawing2D=true`): 홀/슬롯/EarthBoss 풍선 *표시 안 함*
- 글로벌 X/Y/Z 뷰 (`forDrawing2D=false`, viewDirection 있음): 그대로 표시 (사용자 명시 없음 → 보수적 유지)
- 치수추출 (viewDirection=null): 그대로 표시 (3D 메인 뷰)
- ISO 뷰: 별도 `CreateIsoBalloonNotes` (영향 없음, 부재 번호 풍선만)
- 가공도: `Form1.MfgDrawing.cs` 자체 처리 (홀/슬롯 풍선 그대로)

**T-044 연결**: 회사 doc "홀 풍선 제작도 X / 가공도 O" 사양의 일부 해당. 정식 T-044 검증 시 EarthBoss·CIRCLE 풍선도 같은 정책 적용 결정.

**잔여 (사용자 추가 요청 시)**:
- 글로벌 X/Y/Z 뷰에서도 풍선 차단 — `forDrawing2D` 대신 `viewDirection != null` 조건
- 수집 자체 차단 (현재는 수집 후 Clear, 약간의 비용)

---

## 2026-05-12 — T-038+039 v3 + step B-3: 짧은 축 보조선 절반 + 모델 0.75배 + 텍스트 5배

**유형**: feat (사용자 사양 4건)
**커밋**: (이번 커밋)
**관련 TASK**: T-005 / T-038 / T-039 (모두 IN_PROGRESS)
**관련 FEEDBACK**: FB-002, FB-004

**사용자 사양 4건 (2026-05-12)**:
1. 모델 스케일 0.85 → **0.75**
2. ISO/평면도 라벨: 이미 셀 하단이라 skip
3. 수치 텍스트·풍선 **5배 키움** (코드 적용 검증용 임시)
4. **짧은 축 치수의 보조선 절반** — *"높이 max 500, 너비 max 60이면 60을 표현하려고 생기는 보조선의 길이를 줄임"*

**구현**:

| # | 위치 | 변경 |
|---|---|---|
| 1 | [Form1.DrawingSheets.cs:1731, 1907](A2Z/Form1.DrawingSheets.cs:1731) | `0.85f` → `0.75f` (bgObjId / objId 양쪽) |
| 3a | [Form1.DrawingSheets.cs:1368](A2Z/Form1.DrawingSheets.cs:1368) | `Set2DViewCreateObjectItemMeasureTextHeight(5f → 25f)` |
| 3b | [Form1.DrawingSheets.cs:1935](A2Z/Form1.DrawingSheets.cs:1935) | `Set2DViewCreateObjectItemTextHeight(5.25f → 26.25f)` (풍선) |
| 4a | [Form1.Dimensions.cs:497~](A2Z/Form1.Dimensions.cs:497) | `axisShortHalf` HashSet 신설 — `filteredDims.GroupBy(Axis).Max(Distance)` 계산 후 `< globalMaxMax / 3` 축을 짧은 축으로 식별 |
| 4b | [Form1.Dimensions.cs:585, 596, 612](A2Z/Form1.Dimensions.cs:585) | foreach level1/2/0 dims에서 `dim.Axis in axisShortHalf`면 `offset × 0.5f` |

**알고리즘 (v3 짧은 축 보조선 절반)**:
```
axisMaxes = filteredDims.GroupBy(Axis).ToDict(g.Max(Distance))
globalMax = axisMaxes.Max()
axisShortHalf = { axis | axisMaxes[axis] < globalMax / 3 }
```

foreach `DrawDimension` 호출 직전:
```
offsetForThisDim = axisShortHalf.Contains(dim.Axis) ? levelOffset * 0.5f : levelOffset
```

**DiagLog v3**: `T-038+039 v3 view=X maxDist=N canvasBase=N canvasLvl=N scale=N → baseOffset_3d=N levelSpacing_3d=N shortAxes=[X] axisMaxes=[X=500,Y=60]`

**검증 포인트** (사용자 사내 PC):
- 모델 셀의 약 75% 차지 (이전 85% → 더 줄어듦)
- 수치 텍스트·풍선이 *눈에 띄게 큼* (5배 — 너무 크면 줄일 예정)
- 짧은 축 (다른 축의 1/3 이하) 치수의 보조선이 *눈에 띄게 짧음* (절반)
- 사용자 케이스 (Z뷰 Y- 침범) — 짧은 축이 X면 X 치수 보조선 절반 → Y- 방향 영역 좁아짐 → 침범 해소 기대

**잔여**:
- 텍스트 크기 5배는 *임시 검증값* — 결과 보고 적정 배수로 조정
- 텍스트가 너무 크면 셀 침범 추가 가능 → 모델 스케일 추가 조정 또는 텍스트 배수 조정

---

## 2026-05-12 — T-038 step B-2: 모델 0.85배 (셀 가득 후 15% 안전 마진)

**유형**: fix (사용자 사양)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 (IN_PROGRESS)
**관련 FEEDBACK**: FB-004

**사용자 사양 (2026-05-12)**: *"너무 크게 잘리고 너무 커서 크기는 15프로 줄여보자."*

**현상 (step B 검증)**: `targetH = 0f` → 셀 100% 가득 → 보조선·풍선·라벨이 셀 밖으로 튀어나가 잘림.

**변경**:

| 위치 | 변경 |
|---|---|
| [Form1.DrawingSheets.cs:1704~](A2Z/Form1.DrawingSheets.cs:1704) | bgObjId 분기 — `if (targetHeight > 0)` 뒤에 `else { RescaleObject(bgObjId, curScale * 0.85f); }` 추가 |
| [Form1.DrawingSheets.cs:1879~](A2Z/Form1.DrawingSheets.cs:1879) | objId 분기 — 동일 패턴 |

**동작 변화**: `targetH = 0f` 그대로 + `FitObjectToGridCellAspect` 후 *추가 0.85배 RescaleObject* → 모델 85% 차지, 15% 마진(보조선/풍선/라벨용) 확보.

**검증 포인트**:
- 4뷰 모델이 step B 대비 *살짝 작아짐* (15%)
- 셀 밖 잘림 해소되는지
- 보조선·풍선·라벨이 셀 안에 들어오는지

**다음 단계 (사용자 결과에 따라)**:
- 여전히 잘림 → 추가 축소 (0.80, 0.75 등) 또는 step C 본격 (라벨/풍선/보조선 영역 동적 차감)
- 잘림 해소 → T-038+039 가공도 적용으로

---

## 2026-05-12 — T-038 step B: 모델 셀 가득 (targetH 40f → 0f)

**유형**: feat (사용자 사양 — T-038 본진 1차)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 (IN_PROGRESS)
**관련 FEEDBACK**: FB-004

**사용자 사양 (2026-05-12)**: *"각 그리드에 꽉 차게 하고 싶다. 모델은 꽉차면서 보조선 영역도 확보해야 — 단계별로 모델부터 키우자."*

**현상 (T-038+039 v2 push 후 사용자 스크린샷)**: 4뷰가 셀의 약 30%만 차지. 보조선 길이는 줄어들었으나 모델 자체가 작음.

**원인**: `Form1.DrawingSheets.cs:1372` `float targetH = 40f` 하드코딩. `RenderSheetViewForDrawing` → `FitObjectToGridCellAspect` 후 추가로 *세로 40mm* RescaleObject 호출 → 셀(약 128mm) 대비 30%로 축소.

**변경**:

| 위치 | 변경 |
|---|---|
| [Form1.DrawingSheets.cs:1372](A2Z/Form1.DrawingSheets.cs:1372) | `float targetH = 40f` → `float targetH = 0f` |

**동작 변화**: `RenderSheetViewForDrawing` L1702 `if (targetHeight > 0)` 분기 false → 추가 RescaleObject 건너뜀 → `FitObjectToGridCellAspect`만 사용 → 모델이 셀 비율 유지하며 가득 채움.

**기대 효과**:
- 4뷰 모델이 셀의 약 90~100% 차지
- 보조선 캔버스 절대 길이(10/20mm 또는 20/40mm)는 그대로 — 모델 가까이 그려짐
- 풍선·라벨 영역 충돌 가능성 있음 — 다음 step C에서 동적 마진 도입 예정

**다음 단계 (C — 사용자 결정)**:
- 라벨 영역(셀 하단 라벨 박스) + 풍선 영역 + 보조선 영역 차감
- 동적 targetH 계산 — 셀 가용 높이 = cellH - 라벨H - 풍선H - 보조선H
- 모델은 그 가용 영역 안에서 가득

**검증 포인트** (사용자 사내 PC):
- 4뷰 모델이 이전(스크린샷) 대비 *눈에 띄게* 큼
- 셀 밖으로 보조선·풍선·치수 텍스트가 *튀어나가는지* 확인 (튀어나가면 step C 필요)
- 라벨(예: "ISO", "LOOKING Z") 박스와 모델 겹치는지

---

## 2026-05-12 — T-038+T-039 v2: 치수 max 기반 보조선 길이 동적 분기

**유형**: feat (사용자 사양 v2 — v1 50/100mm 교체)
**커밋**: (이번 커밋)
**관련 TASK**: T-038 + T-039 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**사용자 사양 v2 (2026-05-12)**: *"각 뷰에서 치수를 표시할 때 뷰의 치수 중 가장 큰 치수를 기준으로 1000이 넘는 치수면 보조선 길이를 10mm, 20mm로 하고 500 이하면 20mm, 40mm."* — 큰 치수일수록 보조선 짧게 (시각 균형).

**v1 (50/100mm 고정)과의 차이**: 정적 → 동적. 뷰의 치수 max 기준 분기.

**구현 (v2 — v1 교체)**:

| 위치 | 변경 |
|---|---|
| [Form1.Dimensions.cs:378](A2Z/Form1.Dimensions.cs:378) | `ShowAllDimensions` 시그니처 **단순화** — v1의 `baseOffsetOverride / levelSpacingOverride` 두 파라미터 제거, `canvasScaleOverride = -1f` 하나로 통합 |
| [Form1.Dimensions.cs:497~](A2Z/Form1.Dimensions.cs:497) | 내부 분기 — `filteredDims.Max(d => d.Distance)` 후 `(maxDist > 1000f) ? 10f : 20f` (1단 캔버스 mm), `(maxDist > 1000f) ? 10f : 20f` (차분). 모델좌표 = canvasMm / canvasScale |
| [Form1.DrawingSheets.cs:1603](A2Z/Form1.DrawingSheets.cs:1603) | 호출자 단순화 — `EstimateFitScaleForCell` 후 `estScale`만 전달 (분기 로직 ShowAllDimensions 내부로 이관) |

**분기 매트릭스**:
| 치수 max | 1단 캔버스 | 2단 캔버스 | 1단 모델좌표 | 2단 모델좌표 |
|---|---|---|---|---|
| > 1000mm | 10mm | 20mm | 10/scale | 20/scale |
| ≤ 1000mm | 20mm | 40mm | 20/scale | 40/scale |

**다른 ShowAllDimensions 호출자**: 5곳 모두 `canvasScaleOverride` 생략 → default `-1f` → 기존 100/80mm 모델좌표 동작 보존.

**DiagLog**: `T-038+039 v2 view=X maxDist=N.N canvasBase=N canvasLvl=N scale=N.NNNN → baseOffset_3d=N.NN levelSpacing_3d=N.NN`

**검증 포인트** (사용자 사내 PC):
- 치수 1000mm 초과 부재 시트: 보조선 10mm/20mm 시각 도달
- 치수 1000mm 이하 부재 시트: 20mm/40mm 도달
- 큰·작은 부재 시트 보조선이 *시각적으로* 균형 (큰 부재일수록 짧음)

---

## 2026-05-12 — T-038+T-039: 일반 시트 보조선 길이 캔버스 절대 50/100mm 고정 (1차 PoC)

**유형**: feat (사용자 사양)
**커밋**: (이번 커밋 — T-005와 합쳐서)
**관련 TASK**: T-038 + T-039 (TODO → IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**사용자 사양 (2026-05-12)**: *"모델을 2D View에 표현한 후 2D View에서 첫 번째 체인치수는 모두 50mm로 고정하고 두번째 라인 전체 치수는 100mm로 고정."* 기준=보조선 끝점. 텍스트 마진(`AlignDistanceTextMargine`) 보정 X.

**문제**: 기존 `ShowAllDimensions` 내부 `baseOffset=100`, `levelSpacing=80`은 *3D 모델 좌표 mm*. 모델과 함께 RescaleObject로 스케일되어 *시각 길이가 모델 크기에 비례 변동*. 사용자는 *2D 캔버스 절대 mm*로 고정 원함.

**핵심 발견**: 현재 `RenderSheetViewForDrawing` 흐름이 `ShowAllDimensions` → `Create2DViewObjectWithModelHiddenLineAtCanvasOrigin` → `RescaleObject(objId, fitScale)` 순서. 즉 *치수 생성 시 실제 fitScale 미상*. 사전 추정 필요.

**구현 (1차 — 일반 시트만)**:

| 위치 | 변경 |
|---|---|
| [Form1.Dimensions.cs:378](A2Z/Form1.Dimensions.cs:378) | `ShowAllDimensions` 시그니처에 `baseOffsetOverride = -1f`, `levelSpacingOverride = -1f` 옵션 파라미터 추가 |
| [Form1.Dimensions.cs:493~495](A2Z/Form1.Dimensions.cs:493) | `baseOffset` / `levelSpacing` 변수에 override 우선 적용 (>0이면) |
| [Form1.DrawingSheets.cs:1498~](A2Z/Form1.DrawingSheets.cs:1498) | 신규 헬퍼 `EstimateFitScaleForCell(row, col, viewDirection, memberIndices)` — `GetGridCellWidth/Height` + margins 차감 후 모델 BBox 2D 투영 → `min((availW × 0.8) / modelW_2dProj, (availH × 0.8) / modelH_2dProj)` |
| [Form1.DrawingSheets.cs:1603](A2Z/Form1.DrawingSheets.cs:1603) | `ShowAllDimensions` 호출 직전 `estScale = EstimateFitScaleForCell(...)` → `baseOff = 50/scale`, `lvlSpace = 50/scale` (100-50=50 차분) 전달 |

**변환 식**:
- 1단 보조선 끝점 = 캔버스 50mm 목표 → 모델 좌표 mm offset = `50 / scale`
- 2단 보조선 끝점 = 캔버스 100mm 목표 → 차분 50mm → 모델 좌표 mm levelSpacing = `50 / scale`
- 즉 level1Offset = 50/scale, level2Offset = baseOffset + levelSpacing = 100/scale

**다른 ShowAllDimensions 호출자 영향**: 5곳 모두 override 인자 생략 → default `-1f` → 기존 동작(100/80) 그대로 보존. RenderSheetViewForDrawing L1603만 신규 동작.

**검증 메트릭 DiagLog**: `T-038+039 EstimateFitScaleForCell row=N col=N view=X cell=(W,H) model=(W,H) scale=N.NNNN`

**잔여 작업 (2차+)**:
- 가공도(MfgDrawing) `mfgChainOff1 = 100.0f * offFactor_3d` 식 동일 패턴 — 별도 commit 예정
- 사전 추정 scale vs 실제 RescaleObject scale 오차 측정 — 사용자 검증 후 조정

**검증 포인트** (사용자 사내 PC):
- 큰 부재·작은 부재 두 시트 비교 — 보조선 시각 길이가 *동일*하게 보이는지 (절대 50/100mm 도달)
- 사전 추정 오차가 시각적으로 받아들일 만한지 (대략 ±10% 이내 예상)
- DiagLog에서 viewDirection별 estimate scale 값 합리적인지

---

## 2026-05-12 — T-005 (FB-002): 보조선 외곽 방향 자동 판정 (중앙→Osnap 최장거리 쪽)

**유형**: feat (사용자 사양 — FB-002)
**커밋**: (이번 커밋)
**관련 TASK**: T-005 (TODO → IN_PROGRESS)
**관련 FEEDBACK**: FB-002
**관련 REQUEST**: —

**사용자 사양 (2026-05-12)**: *"모델 전체 뷰를 봤을 때 중앙을 기준으로 4분면으로 나누면 중앙에서 가장 먼 남아있는 Osnap이 있는 방향으로 치수를 그려준다. 상하·좌우 중 상이 더 멀고 좌가 더 멀면 위쪽·왼쪽으로 그린다."*

**기존 동작**: 모든 `axisPositiveOffset` 계산이 `avg(Osnap 좌표) >= 중앙` 비교 — *평균*만 따져 부재가 한쪽으로 쏠려 있어도 *외곽 자동 판정 안 됨*

**구현 핵심**: 헬퍼 `ComputePositiveOffsetByOsnapExtreme(IEnumerable<float> values, float modelCenter)` 신설. `omax - center` vs `center - omin` *부호 있는* 거리 비교 → 큰 쪽이 positive. Osnap이 한쪽에만 있는 케이스도 자동 처리.

**코드 변경**:

| 위치 | 변경 |
|---|---|
| [Form1.Dimensions.cs:1490~](A2Z/Form1.Dimensions.cs:1490) | 신규 헬퍼 `ComputePositiveOffsetByOsnapExtreme` |
| [Form1.Dimensions.cs:499~](A2Z/Form1.Dimensions.cs:499) | `axisPositiveOffset` (메인, 치수추출+2D 출력 공용) — avg → 헬퍼 |
| [Form1.MfgDrawing.cs:335~](A2Z/Form1.MfgDrawing.cs:335) | `mfgAxisPosOff` (가공도 메인) — avg → 헬퍼 |
| [Form1.MfgDrawing.cs:1057~](A2Z/Form1.MfgDrawing.cs:1057) | `mfgAxisPosOff` (가공도 보조) — avg → 헬퍼 |
| [Form1.MfgDrawing.cs:1192~](A2Z/Form1.MfgDrawing.cs:1192) | `mfgAxisPosOff_m` (MULTI) — avg → 헬퍼 |
| [Form1.MfgDrawing.cs:1707~](A2Z/Form1.MfgDrawing.cs:1707) | `eaAxisPosOff` (EA newDims 비길이축, `longestAxis = !isLShape` 오버라이드 유지) |

**호출자 시그니처 무변경**: `AddChainDimensionByAxis(positiveOffset)`은 그대로. `Dictionary<string, bool>` 사전 채우는 로직만 5곳 교체.

**검증 포인트** (사용자 사내 PC 실기):
- 부재가 모델 중앙 한쪽에 치우친 케이스에서 치수가 *그 반대쪽*(외곽)으로 빠지는지
- 양쪽 균등 분포 케이스에서 (max·min 거리 동일) 기본값(positive) 적용되는지
- EA 가공도에서 longestAxis 오버라이드는 그대로, 비길이축은 헬퍼로 자동
- 4경로(치수추출/글로벌/2D 출력/가공도) 모두 일관 동작

**관련 TASK**: T-005 (TODO → IN_PROGRESS, 실기 검증 대기)

---

## 2026-05-12 — REQ-002 / T-012: 엑셀 템플릿 PoC Step 3.5 (2D 도면 모드 진입 시퀀스 추가)

**유형**: fix (PoC 보완)
**커밋**: (이번 커밋)
**관련 TASK**: T-012 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-002

**Step 3 검증 결과** (사용자 사내 PC):
- Line 10/1539 추가 성공 (`shapeId=2`), JSON 파싱·ShapeDrawing.AddLine·Add2DObjectFromShapeDrawing 모두 DiagLog "OK"
- 그런데 캔버스에 셀 보이지 않음
- 사용자 지적: **"2D View에 템플릿 그리는 방법 자체가 애초에 잘못된거 아니야?"**

**원인 진단**: 사용자가 보여준 SDK 표준 코드 시퀀스 검토 결과, **2D 도면 모드 진입·캔버스 활성화 시퀀스가 통째로 누락**.

기존 `Form1.DrawingSheets.cs:1219`, `Form1.MfgDrawing.cs:655`에서 이미 사용 중인 정공법 시퀀스:
```
vizcore3d.ToolbarDrawing2D.Visible = true
vizcore3d.ViewMode = ViewKind.Both
vizcore3d.Drawing2D.View.SetCanvasSize(W, H)
vizcore3d.Drawing2D.View.SetSelectCanvas(idx)
vizcore3d.Drawing2D.Template.CrateTemplateBorder()
```

PoC가 이 시퀀스 없이 ShapeDrawing.AddLine + Add2DObjectFromShapeDrawing만 호출 → SDK가 *어떤 캔버스에 그릴지* 알 수 없어 결과 안 보임.

**Step 3.5 변경**:

| 위치 | 변경 |
|---|---|
| [Form1.ExcelTemplate.cs:65~85](A2Z/Form1.ExcelTemplate.cs:65) | JSON 파싱 *직전*에 2D 도면 모드 진입 시퀀스 추가. CanvasSize=(420, 297) A3 landscape (우리 데이터 355×227mm 안전 수용). `SetSelectCanvas(1)`. `CrateTemplateBorder()` 호출 (외곽 테두리). `GetCanvasSize` ref out 로 실제 크기 확인. 모든 단계 DiagLog 기록 |

**사용자 검증 대기** (사내 PC):
1. `git pull` + 빌드 + A2Z.exe 실행
2. "엑셀 PoC" 클릭 → InputBox에 **"1"** 입력 (Line 10개 시각 검증)
3. 2D View 캔버스 확인:
   - ✅ 셀 일부 보이면 → 좌표·렌더·캔버스 활성화 모두 정상. InputBox **"2"** 로 전체 그리기 진행
   - ❌ 여전히 안 보이면 → 좌표 스케일 / 카메라 시점 추가 진단 필요
   - 외곽 테두리(CrateTemplateBorder)가 캔버스에 보이는지도 같이 확인 — 그게 보이면 캔버스 활성화 성공 신호

**docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) Step 3 흐름에 2D 모드 진입 시퀀스 추가

---

## 2026-05-12 — REQ-002 / T-012: 엑셀 템플릿 PoC Step 3 (JSON 파싱 → 우리가 직접 렌더, 옵션 A 본진)

**유형**: feat (PoC)
**커밋**: (이번 커밋)
**관련 TASK**: T-012 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-002

**Step 2 검증 결과** (사용자 사내 PC):
- Reflection으로 internal `Draw2DViewTemplate(string)` / `Set2DViewDefaultTemplate(string)` 호출 — 모두 *예외 없이* "성공"으로 표시되지만 캔버스 빔
- 즉 SDK dll obfuscation 보호로 internal 메서드가 외부 호출 시 **silent fail** (void 반환, 내부 검증 실패)
- xlsx 경로, -1 등 시도 모두 같은 결과
- 추가 후보(JSON 경로, Template_0, SHI)도 같은 패턴일 가능성 매우 큼

**결론**: SDK의 사용자 추가 템플릿 자동 적용 API는 **외부에서 호출 불가**. SDK 자동 적용 경로 폐기. **옵션 A 본진(JSON 직접 파싱 + 우리 렌더)** 진입.

**옵션 A 전략 검증** (사용자 질의 "JSON 파싱해서 직접 그리면 원래 방식이랑 다른가?"에 대한 답):
- 엑셀 외부 관리 가치 **그대로 유지** (사용자 엑셀 편집 → SDK 분석 → 우리 렌더 3단계)
- 원래 GenerateSheetDrawing2D(코드 하드코딩) 대비 양식 수정 시 재빌드 X
- REQ-002의 "시나리오 2 (하이브리드 추천안)"과 일치

**SDK reflection 분석으로 발견한 핵심 public API**:

| 메서드 | 가시성 | 용도 |
|---|---|---|
| `ShapeDrawingManager.AddLine(List<Vertex3DItemCollection>, ...)` → int | PUBLIC | 3D 공간 라인 세그먼트 일괄 추가, ID 반환 |
| `Drawing2DObjectManager.Add2DObjectFromShapeDrawing(List<int>)` | PUBLIC | **3D ShapeDrawing → 2D 캔버스 변환 핵심** |
| `ShapeDrawingManager.Clear()` | PUBLIC | 기존 ShapeDrawing 제거 |
| `TextDrawingManager.Add(Vertex3D, Vector3D, Vector3D, float, Color, string)` | PUBLIC | 3D 텍스트 (Step 4 후보) |
| `NoteManager.AddNote2D(...)` | PUBLIC (단 VIZCore3DControl 미노출) | Step 4 별도 경로 탐색 필요 |

**Step 3 변경 — Line만 PoC (Text는 Step 4)**:

| 위치 | 변경 |
|---|---|
| [Form1.ExcelTemplate.cs](A2Z/Form1.ExcelTemplate.cs) | 전면 재작성 — JSON 자동 검색(`%APPDATA%\SOFTHILLS\VIZCore3D+.NET\Template\Template_0\*.json`) + `JavaScriptSerializer` 파싱 + InputBox 모드(1=Line 10개, 2=Line 전체, 0=Clear) + `ShapeDrawing.AddLine` → `Add2DObjectFromShapeDrawing` |
| [A2Z.csproj:48](A2Z/A2Z.csproj:48) | `<Reference Include="System.Web.Extensions" />` 추가 (JSON 직렬화) |

**Text 처리 보류 사유**: `vizcore3d.Note`가 VIZCore3DControl에 노출 안 됨 (PowerShell reflection 검증). Step 4에서 `TextDrawing.Add` (3D 텍스트 + 2D 변환) 또는 NoteManager 직접 인스턴스화 등 별도 탐색.

**JSON 파싱 데이터** (사용자 SHI Rev_01 export 기준):
- Line 1539 / Text 2201 / Image 4
- 좌표 단위 mm, 범위 X 0~355.6 / Y 0~227.3 (W/H 1.565, A4 비율 1.414 근접)

**사용자 검증 대기** (사내 PC):
1. `git pull` + 빌드 + A2Z.exe 실행
2. "엑셀 PoC" 클릭 → InputBox에 **"1"** 입력 (Line 10개 시각 검증)
3. 2D View 캔버스 확인:
   - ✅ 라인 일부 보임 → Step 4 (Text + 전체 그리기) 진행
   - 일부만 보임 → 좌표/스케일 분석
   - ❌ 안 보임 → ShapeDrawing이 *모델 좌표 공간*에 그려졌을 가능성. 카메라 시점 또는 모드 진입 필요

**docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) Step 3 흐름 + 핵심 SDK API 표 갱신

---

## 2026-05-12 — REQ-002 / T-012: 엑셀 템플릿 PoC Step 2 (Reflection 우회 호출)

**유형**: feat (PoC)
**커밋**: (이번 커밋)
**관련 TASK**: T-012 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-002

**Step 1.5 검증 결과** (사용자 사내 PC):
- `Set2DViewDefaultTemplate(int)` public 오버로드 인덱스 0~5+ 모두 시도
- 0/1/2 = DSME 내장 정상 / **3+ = 빈 페이지 outline만 (흰 박스+노란 박스)**
- 줌/팬/F키도 효과 X — 셀이 캔버스 어디에도 안 그려짐
- **SDK 설정 UI "확인" 적용도 동일 실패** → 호출 방법 문제 아님 / SDK 자체가 사용자 추가 템플릿을 public API로는 그리지 못함

**SDK dll Reflection 분석** (`lib/VIZCore3D+.NET.dll`):
- `Draw2DViewTemplate(string filePath)` **INTERNAL** ← 캔버스 직접 그리기 후보
- `Draw2DViewTemplate(string, int, int)` / `(string, int, int, int, int)` INTERNAL
- `Set2DViewDefaultTemplate(string filePath)` **INTERNAL** ← string 오버로드 존재 확인
- `ParseJson(string)` / `ReadJson()` INTERNAL
- `get_TemplatePath()` INTERNAL — SDK 데이터 폴더 경로

**SDK export 데이터** (`C:\Users\duddl\Desktop\Template`):
- `TemplateManagement.json` — Template_0(SHI), Template_1, Template_2 매핑 + index="22"
- 각 Template 폴더에 `사용자템플릿_엑셀_Rev_01.json` (458KB) — **셀 데이터 완벽** (Line 1539, Text 2201, Image 4, 단위 mm 좌표)

**Step 2 변경**: Reflection으로 internal 메서드 우회 호출.

| 위치 | 변경 |
|---|---|
| [Form1.ExcelTemplate.cs:1~](A2Z/Form1.ExcelTemplate.cs:1) | `using System.Reflection` 추가 |
| [Form1.ExcelTemplate.cs:50~140](A2Z/Form1.ExcelTemplate.cs:50) | 핸들러 전면 재작성 — (1) TemplatePath reflection 읽기 (2) InputBox로 ImportExcel 재실행 Y/N (3) InputBox로 filePath 입력 (4) `Draw2DViewTemplate(filePath)` reflection 호출 (5) `Set2DViewDefaultTemplate(string)` reflection fallback (6) 빈 입력 시 `Set2DViewDefaultTemplate(-1)` 캔버스 클리어 |

**사용자 검증 대기** (사내 PC):
1. SDK 설정 UI에서 SHI_* 누적 항목 삭제 (한 번만)
2. "엑셀 PoC" 버튼 클릭
3. InputBox 1: ImportExcel 재실행 — **N** (skip)
4. InputBox 2: filePath — 후보 (a)/(b)/(c) 차례로 시도
   - (a) `사용자템플릿_엑셀_Rev_01.xlsx` (기본값)
   - (b) `C:\Users\duddl\Desktop\Template\Template_0\사용자템플릿_엑셀_Rev_01.json`
   - (c) DiagLog에 출력된 SDK TemplatePath 안의 SHI 경로
5. 결과: 2D View 캔버스에 셀 그려지는 후보 찾기 → 그게 SDK의 진짜 적용 API

**docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) Step 2 흐름 + SDK reflection 분석 표 + 검증 결과 표 갱신

---

## 2026-05-12 — REQ-002 / T-012: 엑셀 템플릿 PoC Step 1.5 (Set2DViewDefaultTemplate 추가 + 인덱스 입력)

**유형**: feat (PoC)
**커밋**: (이번 커밋)
**관련 TASK**: T-012 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-002

**Step 1 검증 결과** (사용자 사내 PC):
- SDK 설정 다이얼로그 "사용자 템플릿" 탭의 트리뷰에 `SHI` 항목 등장 + 미리보기 정상 (4뷰/BOM/NOTE/도면정보 모두 그려진 상태)
- **메인 2D View 캔버스는 비어 있음** → ImportExcel만으로는 적용 안 됨 확인

**Step 1.5 변경**: 적용 호출 추가.
- `Set2DViewDefaultTemplate(string)` 외부 호출 시도 → 빌드 실패(`'string'에서 'int'로 변환 불가`). xml 명세에는 string 오버로드 존재하나 internal/protected로 외부 코드 호출 불가 확정.
- 대안: `Set2DViewDefaultTemplate(int)` 사용. 정확한 인덱스 미상(기본 DSME 3개 + 사용자 추가) → 사용자가 직접 시도하도록 `Microsoft.VisualBasic.Interaction.InputBox`로 인덱스 입력 받음 (기본 3).
- csproj에 `Microsoft.VisualBasic` 참조 추가.

**코드 변경**:

| 위치 | 변경 |
|---|---|
| [Form1.ExcelTemplate.cs:50~](A2Z/Form1.ExcelTemplate.cs:50) | ImportExcel 후 InputBox로 인덱스 입력 → `Set2DViewDefaultTemplate(int)` 호출 (try/catch). 결과 MessageBox에서 다른 인덱스 재시도 안내 |
| [A2Z.csproj:43](A2Z/A2Z.csproj:43) | `<Reference Include="Microsoft.VisualBasic" />` 추가 |

**사용자 검증 대기** (사내 PC):
- "엑셀 PoC" 버튼 클릭 → 인덱스 입력 (기본 3) → 2D View 캔버스에 SHI 그려지는지
- 안 보이면 다른 인덱스(0, 1, 2, 4, 5...) 순회 → SHI 적용되는 인덱스 발견 시 코드에 하드코딩

**docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) 갱신 (Step 1.5 흐름, SDK API 가시성 확정)

---

## 2026-05-12 — REQ-002 / T-012: 엑셀 템플릿 PoC Step 1 (ImportExcel 단독 검증)

**유형**: feat (PoC)
**커밋**: (이번 커밋)
**관련 TASK**: T-012 (TODO → IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-002

**배경**: 사용자가 `사용자템플릿_엑셀_Rev_01.xlsx`를 A4 가로 비율(W/H ≈ 1.41)로 준비. SDK `Drawing2DTemplateManager.ImportExcel(path)`이 외부 엑셀을 2D View 캔버스에 그릴 수 있는지 **시각 검증**부터 단독 PoC.

**전략**: 옵션 A — 기존 `GenerateSheetDrawing2D`(GridStructure 기반)는 그대로 유지하고, 새 partial class `Form1.ExcelTemplate.cs`에 독립 핸들러 신설. 새 디버그 버튼으로 호출. Step 1 시각 결과 보고 Step 2(셀 좌표 매핑) 진행 결정.

**코드 변경**:

| 위치 | 변경 |
|---|---|
| `A2Z/Form1.ExcelTemplate.cs` (신규) | `btnExcelTemplatePoC_Click` — 엑셀 경로 자동 탐색 → `vizcore3d.Drawing2D.Template.ImportExcel(path)` 호출 → DiagLog + MessageBox |
| [A2Z/Form1.Designer.cs:84](A2Z/Form1.Designer.cs:84), [:625](A2Z/Form1.Designer.cs:625), [:709~720](A2Z/Form1.Designer.cs:709), [:1325](A2Z/Form1.Designer.cs:1325) | `btnExcelTemplatePoC` 신규 (groupBox1 "작업" 끝, 텍스트 "엑셀 PoC"). groupBox1 너비 443 → 530으로 확장 |
| `A2Z/A2Z.csproj` | `Form1.ExcelTemplate.cs` Compile Include 추가 |
| `사용자템플릿_엑셀_Rev_01.xlsx` | 사용자 작성 — A4 가로 비율, 55컬럼 × 40행 |

**Step 1 검증 결과 (개발 PC 빌드)**:
- `templateDatas` 필드는 외부 접근 불가 (private/internal 확인) → Step 1 코드에서 덤프 제거
- A2Z.exe 빌드 성공

**사용자 검증 대기** (사내 PC):
- "엑셀 PoC" 버튼 클릭 → 2D View 캔버스에 엑셀 셀 구조(테두리·텍스트·라벨)가 그려지는지
- 안 그려지면 Step 2에서 추가 호출(`RenderTemplate` 등) 탐색

**docs**: [엑셀 템플릿 PoC.md](../기능/도면시트/엑셀 템플릿 PoC.md) 신규, [TASKS.md](TASKS.md) T-012 격상

---

## 2026-05-11 — T-040: 치수 텍스트 위치 13mm 임계 토글 (사용자 결정)

**유형**: fix (사용자 결정 — 외부 조건)
**커밋**: `acb867a`
**관련 TASK**: T-040
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 사용자 결정 *"치수가 보조선과 겹치는 현상 — 치수 13mm 이하면 바깥으로 빼버려, 기준 통일"*. T-058에서 모든 치수 일괄 `AlignDistanceTextPosition=2`(바깥)였는데, 긴 치수는 안쪽이 자연스러워 거리 기반 분기로 변경.

**구현 핵심**: `MeasureStyle.AlignDistanceTextPosition`은 글로벌 옵션이라 측정별 개별 지정 불가 (T-058 sdk-verifier 확인). 우회: 측정 추가 직전에 `dim.Distance` 검사해 `SetStyle` 동적 토글.

**코드 변경**:

| 위치 | 변경 |
|---|---|
| [Form1.Dimensions.cs:62~](A2Z/Form1.Dimensions.cs:62) `btnDimensionShowSelected_Click` foreach | dim별 토글 추가 |
| [Form1.Dimensions.cs:534~](A2Z/Form1.Dimensions.cs:534) `ShowAllDimensions` Level 1/2/0 | `applyTextPosition` 람다 + 세 foreach 모두 호출 |

**규칙**:
- `dim.Distance ≤ 13.0f` → `AlignDistanceTextPosition = 2` (보조선 바깥)
- `dim.Distance > 13.0f` → `AlignDistanceTextPosition = 1` (위)

**확인 필요 (실측)**: `SetStyle` 토글이 새로 추가되는 측정에만 적용되는지 (예상) vs 기존 측정도 갱신되는지 — SDK XML 명시 없음. 빌드 후 결과로 판정.

**docs**: `치수/X축 치수 표시.md` 변경 이력, `TASKS.md` T-040 체크

**빌드 검증**: A2Z.exe 생성 성공

---

## 2026-05-11 — T-040v 토글 취소: 2줄만 생성 (사용자 결정)

**유형**: fix (사용자 결정 — 외부 조건)
**커밋**: `4edd04f`
**관련 TASK**: T-040 (IN_PROGRESS, 1차 폐기)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 사용자 요청 *"수치는 부재간의 연쇄치수가 첫번째, 전체 치수가 두번째로 2줄만 생성되어야 한다고 해서 다시 취소해줘"*. 회사·도면 표준 기준 외부 조건. T-040v 1차(`66ac0bb`)의 i%2 토글(100mm/50mm)은 3줄 결과를 만들어 기준 위반.

**코드 변경** ([Form1.Dimensions.cs:537~553](A2Z/Form1.Dimensions.cs:537)):
- Level 1 foreach 단순 형태로 복원 (axis 그룹화 + 정렬 + i%2 토글 폐기)
- 모든 `level1Dims`에 단일 `level1Offset(100mm)` 적용
- level2 적응형 충돌 회피(`ApplySmartFiltering`이 텍스트 폭 초과 시 일부 dim을 `DisplayLevel=1`로 밀어내기)는 **유지** — 잠재적 3줄 가능성 있음

**검증 포인트**:
- 인접 치수가 같은 라인(100mm)에 일렬 배치, 전체 치수는 가장 바깥(180mm 또는 그 이상)
- ApplySmartFiltering 진단 로그(`level1>0`이면 level2 발생 — 2줄 위반 가능성 → 별도 결정 필요)

**잔여 결정 필요**: level2 적응형 폐기 여부. 폐기 시 텍스트 충돌 발생해도 무조건 한 줄에 배치 → 일부 짧은 치수 안 보일 수 있음 (`IsVisible=false` 분기)

**docs**: `치수/X축 치수 표시.md` 취소 이력 추가, `TASKS.md` T-040 갱신

---

## 2026-05-11 — REQ-005: 체인치수 행 선택 강조 + ChainDimensionData.MemberIndices

**유형**: feat (사용자 요청)
**커밋**: `21bed37`
**관련 TASK**: T-028 (디버깅 인프라 후속)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-005

**배경**: 사용자 요청 *"체인치수 목록에서 선택했을 때 치수나 Osnap을 강조할 수 있는지도 궁금"*. T-028 본진 (작업데이터 탭 ↔ 도면 데이터 통일) 후 디버깅 도구로서 lvDimension 활용 강화.

**변경**:

| 영역 | 파일·위치 | 내용 |
|---|---|---|
| 데이터 모델 | [Models.cs:50](A2Z/Models.cs:50) | `ChainDimensionData.MemberIndices` 필드 신규 (`List<int>`, default empty) |
| BBox 경로 | [Form1.GlobalViews.cs:286, 312](A2Z/Form1.GlobalViews.cs:286) | `ExtractInstallationDimensions`: 인접 경계 치수에 `[uniqueEntries[i].member.Index, uniqueEntries[i+1].member.Index]`, 전체 조립에 `[first.member.Index, last.member.Index]` |
| Osnap 경로 | [Form1.Dimensions.cs:2087, 2135](A2Z/Form1.Dimensions.cs:2087) | `ComputeViewDimensionsForMembers`: nodeOsnapMap 채워진 후 `coordKeyToMembers` 사전 구축 (좌표 키 → nodeIdx 집합). 결과 dim의 StartPoint/EndPoint 좌표 키로 lookup해 사후 채움 |
| 핸들러 | [Form1.Dimensions.cs:1490](A2Z/Form1.Dimensions.cs:1490) | `LvDimension_SelectedIndexChanged` 신규: 선택 행의 `MemberIndices` → `Color.RestoreColorAll` + `Object3D.Select` + `FlyToObject3d`. 다중 선택 지원, MemberIndices 비어있으면 skip |
| 가드 | [Form1.Dimensions.cs:1556](A2Z/Form1.Dimensions.cs:1556) | `_suppressDimSelChanged` 가드 — LvClash 흐름의 `SelectRelatedDimensionItems` 연쇄 트리거 방지 |
| 이벤트 등록 | [Form1.cs:202](A2Z/Form1.cs:202) | `lvDimension.SelectedIndexChanged += LvDimension_SelectedIndexChanged` |

**Plan agent 활용**: Plan C (강조+fit 패턴) — 점→부재 매핑 옵션 분석 + 좌표 사후 매핑 권장

**참고**: AddChainDimensionByAxis 시그니처는 변경 X (호출처 8곳 영향 회피). 좌표 사후 매핑으로 간접 채움.

**docs**: `치수/설치도 치수 추출.md` 변경 이력 추가, `tracking/REQUESTS.md` REQ-003~006 4건 등록

**검증 포인트** (사용자 실기):
- 시트 선택 → lvDimension 행 클릭 → 두 부재 빨간 강조 + fit
- 전체 길이(IsTotal=true) 행 클릭 시 첫·끝 부재 모두 fit
- Clash 행 선택 시 자동 lvDimension 선택돼도 카메라 안 흔들림 (가드)
- ComputeView 경로(일반 시트 2D 출력)에서도 MemberIndices 채워짐 (좌표 매핑 정확성 확인)

---

## 2026-05-11 — REQ-003/004: Osnap 컬럼 6개 축소 + 행 선택 강조

**유형**: feat (사용자 요청)
**커밋**: `86a533d`
**관련 TASK**: —
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-003 (Osnap 컬럼 축소), REQ-004 (Osnap 행 선택 강조)

**배경**: 사용자 요청 *"osnap 정리는 Osnap 좌표목록을 실제 사용하는 Osnap만 남기자는 의미였어. No, 축, 부재이름, X, Y, Z 만 남기면 될 거 같은데"* + *"Osnap이랑 체인치수 목록에서 선택했을 때 치수나 Osnap을 강조할 수 있는지도 궁금"*.

**변경**:

| 영역 | 파일·위치 | 내용 |
|---|---|---|
| 데이터 모델 | [Form1.cs:49](A2Z/Form1.cs:49) | `osnapPointsWithNames` 튜플 `(Vertex3D, string)` → `(Vertex3D, string, string axis)` 확장 |
| 시그니처 | [Form1.Dimensions.cs:1819](A2Z/Form1.Dimensions.cs:1819) | `MergeCoordinates` 시그니처 패스스루 (axis 미사용, 호환성만) |
| Add 호출 | [Form1.Drawing2D.cs:255~286](A2Z/Form1.Drawing2D.cs:255), [Form1.BOM.cs:558~587](A2Z/Form1.BOM.cs:558) | LINE: `EstimateOsnapLineAxis` 추정, POINT: `""` |
| 헬퍼 | [Form1.Drawing2D.cs:802~](A2Z/Form1.Drawing2D.cs:802) | `EstimateOsnapLineAxis(dynamic, dynamic)` — start→end 벡터 최대 성분 ("X"/"Y"/"Z") |
| nodeOsnapPts | [Form1.BOM.cs:552~586](A2Z/Form1.BOM.cs:552) | 2원소 유지 (`_lastCollectedNodeOsnapMap` 영향 차단 — `ComputeViewDimensionsForMembers` 시그니처 보존) |
| ListView 채우기 | [Form1.Drawing2D.cs:309~322](A2Z/Form1.Drawing2D.cs:309), [Form1.BOM.cs:597~610](A2Z/Form1.BOM.cs:597) | SubItems 순서 No/축/부재이름/X/Y/Z (홀사이즈/슬롯홀 제거) |
| Designer 컬럼 | [Form1.Designer.cs:465~471](A2Z/Form1.Designer.cs:465), [L512](A2Z/Form1.Designer.cs:512) | AddRange 6개 (`columnHeader15` 재활용 텍스트 "축" Width 40), `columnHeader16` AddRange에서 제외 (정의 orphan) |
| 이벤트 등록 | [Form1.cs:201](A2Z/Form1.cs:201) | `lvOsnap.SelectedIndexChanged += LvOsnap_SelectedIndexChanged` |
| 핸들러 | [Form1.Drawing2D.cs:822~](A2Z/Form1.Drawing2D.cs:822) | `LvOsnap_SelectedIndexChanged`: 선택 행 부재이름 → bomList 매핑 → 강조+fit. 다중 선택 지원 |
| 가드 | [Form1.Dimensions.cs:1554~](A2Z/Form1.Dimensions.cs:1554) | `_suppressOsnapSelChanged` 가드 — `LvClash_SelectedIndexChanged`의 `SelectRelatedOsnapItems` 연쇄 트리거 방지 (카메라 흔들림 회피) |

**SDK 영향**: `OsnapVertex3D.Start`/`End` 타입이 SDK XML에 명시되지 않아 `dynamic` 매개변수 사용. 런타임에 `X`/`Y`/`Z` 접근.

**Plan agent 활용**: Plan B (Osnap 작업) — 컬럼 축소 + 행 선택 강조 계획 수립

**docs**: `2D도면/Osnap 수집.md` 변경 이력 2건 추가

**검증 포인트** (사용자 실기):
- lvOsnap 6컬럼 표시 (No/축/부재이름/X/Y/Z)
- LINE osnap 행에 X/Y/Z 표기, POINT 행은 빈 축
- 단일/다중 선택 → 부재 빨간 강조 + 카메라 fit
- Clash 행 선택 시 자동 Osnap 선택돼도 카메라 안 흔들림 (가드 효과)

---

## 2026-05-11 — T-040v 1차: 치수 offset i%2 토글 + 진단로그 + UI 높이 + Clash 강조

**유형**: feat (사용자 요청 4건 묶음)
**커밋**: `66ac0bb`
**관련 TASK**: T-040 (IN_PROGRESS, 1차)
**관련 FEEDBACK**: —
**관련 REQUEST**: REQ-006 (Clash 행 선택 강조)

**배경**: 사용자 보고 (사진 + 직접 표현):
1. 짧은 치수 텍스트끼리 같은 라인에 그려져 숫자가 이어져 보임 → offset i%2 토글 요청 (AB 100mm / BC 50mm / CD 100mm ...)
2. `ApplySmartFiltering` 분리 효과를 "본 적 없다" → 작동 검증 진단 로그 요청
3. 체인치수 ListView 28번째 항목이 살짝 가려짐 → UI 높이 키우기
4. Clash Detection 결과 행 선택 시 두 부재 강조 + fit 요청 (BOM 행 선택 패턴 복제)

**코드 변경**:

| # | 작업 | 파일·위치 | 내용 |
|---|---|---|---|
| 1 | T-040v: Level 1 치수 offset i%2 토글 | [Form1.Dimensions.cs:537~556](A2Z/Form1.Dimensions.cs:537) | 같은 axis 내 측정축 좌표 순 정렬 → 짝수 i=`level1Offset(100mm)`, 홀수 i=`level1Offset*0.5(50mm)`. `level1Dims`만 영향, level0(전체)·level2 무관 |
| 2 | ApplySmartFiltering 진단 DiagLog | [Form1.Dimensions.cs:1326~](A2Z/Form1.Dimensions.cs:1326) | axis별 한 줄 (`axis=Z level0=N level1=N total=N hidden=N in=M`). result.AddRange 직후. logs/diag-yyyy-MM-dd.log |
| 3 | lvDimension UI 높이 +32px | [Form1.Designer.cs:303, 357](A2Z/Form1.Designer.cs:303) | `groupBox5.Size`: 188→220, `lvDimension.Size`: 162→194. Dock=Fill이라 부모 groupBox 같이 키워야 효과 발생 (Plan agent 통찰). 영향: groupBox3(Clash) 32px 축소 |
| 4 | REQ-006: Clash 행 선택 3D 강조+fit | [Form1.Dimensions.cs:1530~](A2Z/Form1.Dimensions.cs:1530) | `LvClash_SelectedIndexChanged` foreach 직후 + SelectRelatedOsnapItems 호출 직전. 단일 선택일 때만 `Color.RestoreColorAll` + `Object3D.Select([Index1, Index2])` + `FlyToObject3d`. `LvClash_DoubleClick` 동일 패턴 |

**Plan agent 3개 병렬 활용** (사용자 명시 "에이전트 여러개로 계획·검토"):
- Plan A: 치수 작업 (offset 토글, 진단, UI 높이) — 코드 위치·diff·영향 분석
- Plan B: Osnap 작업 (다음 commit)
- Plan C: 강조+fit 패턴 (Clash + 체인치수, 일부 이번 commit 처리)

**docs**: `치수/X축 치수 표시.md` + `치수/Clash 선택 시 치수 필터.md` 변경 이력 추가

**검증 포인트** (사용자 실기):
- 한 축 4개 이상 인접 치수에서 짝수/홀수 두 라인 시각 분산
- logs/diag-2026-05-11.log에 `ApplySmartFilter axis=X level0=N level1=N` 출력 확인 → level1 > 0이면 분리 작동 확정
- 29개 항목 있을 때 28번째까지 보이는지 (혹시 부족하면 추가 +16 가능)
- Clash 행 단일 클릭 → 두 부재 빨간 강조 + 카메라 fit. 다중 선택 시 fit 스킵 (가드)

**다음 commit 예정**: REQ-003 Osnap 컬럼 축소 + REQ-004 Osnap 행 선택 강조 / REQ-005 체인치수 행 선택 강조 (`ChainDimensionData.MemberIndices` 신규 필드)

---

## 2026-05-11 — T-028 진행: 체인치수 데이터 소스 통일 + 개별 부재 길이 블록 제거

**유형**: refactor (사용자 요청, T-028 본진 부분 진행)
**커밋**: `6c57e24`
**관련 TASK**: T-028 (IN_PROGRESS)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: 사용자 보고 *"도면 Looking Z에 12개, 작업데이터 탭 체인치수목록 17개. 다른 모델도 차이 있음. 디버깅을 작업데이터 탭으로 해야 한다"* + *"개별 부재 전체 길이 빼고, 체인치수목록을 도면 표시 치수와 똑같이 맞춰달라"*.

**원인 분석**:
- 작업데이터 탭(`chainDimensionList` / `lvDimension`)과 도면 측 측정이 **완전히 다른 알고리즘** 결과:
  - 작업데이터 탭 (2D 출력 시): `ExtractInstallationDimensions` (BBox 기반, [Form1.GlobalViews.cs:201](A2Z/Form1.GlobalViews.cs:201)) — 인접 경계 + 개별 부재 전체 길이 + 전체 조립
  - 도면 측: `ShowAllDimensions(viewDirection)` (Osnap 기반, [Form1.DrawingSheets.cs:1582](A2Z/Form1.DrawingSheets.cs:1582)) — `AddChainDimensionByAxis` 인접 쌍 + 전체
- 어제 사용자 의심 "BE 비인접 쌍" 정체 = `ExtractInstallationDimensions`의 **개별 부재 전체 길이** (부재가 mMin~mMax를 가로지르면 비인접 쌍처럼 보임)

**코드 변경**:
1. [Form1.GlobalViews.cs:287~346](A2Z/Form1.GlobalViews.cs:287) — "개별 부재 전체 길이" 블록 통째 제거 (foreach members 루프 + 중복 검사 + makePoint 추가). 짧은 회피 주석으로 교체. 인접 경계 + 전체 조립 치수만 남김
2. [Form1.DrawingSheets.cs:1242](A2Z/Form1.DrawingSheets.cs:1242) — `ExtractInstallationDimensions(sheet.MemberIndices)` → `ComputeViewDimensionsForMembers(sheet.MemberIndices, null, 0.5f)` 결과로 chainDimensionList + lvDimension 채우기 (LvDrawingSheet_SelectedIndexChanged 일반 시트 분기 L611~ 패턴 동일)

**효과**:
- 2D 출력 후 작업데이터 탭 항목 = 도면 3뷰 합집합 (Osnap 기반 동일 엔진)
- 사용자 디버깅: ListView ↔ 도면 1:1 매칭 가능
- 시트 선택 -2 분기 ExtractInstallationDimensions도 개별 부재 길이 빠진 결과 표시 (간접 영향)

**잔여 (다음 라운드)**:
- 설치도(-2) 분기 ComputeView로 완전 통일 옵션 (T-028 옵션 A 전환) — 사용자 확인 필요
- lvDimension UI 17번째 가려짐 — Form1.Designer.cs 크기 또는 부모 컨테이너 조정
- 진단성 강화: lvDimension에 ViewDirection 컬럼 추가 검토

**영향 범위**: 시트 2D 출력 + 시트 선택 자동 흐름. R1 docs 갱신 — `시트 2D 렌더.md` / `시트 선택.md` 변경 이력 추가.

---

## 2026-05-11 — T-037 2차: BOM 고정 폭 + 폰트 축소 시도

**유형**: fix
**커밋**: `6a7a1d9`
**관련 TASK**: T-037 (IN_PROGRESS, 2차)
**관련 FEEDBACK**: —
**관련 REQUEST**: —

**배경**: T-037 1차(`c635978`) 빌드 결과 — 여러 셀에서 wrap 잔존 (MATERIAL "IAL", SIZE "5x7.5", Q'TY "Y", T/W 5자 데이터 끝자, MA "A"). 헤더 자체도 셀 폭 안 들어가는 케이스 다수. **사용자 방침** *"테이블 열은 한 번 정해서 고정. 폭 미세조정 + 폰트 전체 축소 OK"* 확정.

**사전 처리**:
- `97c1cba` — T-037 1차 revert (사용자 "콘텐츠 맞춰 폭 변동 지양" 방침 반영)

**SDK 재검증** (sdk-verifier 2026-05-11):
- `Drawing2DObjectManager.Set2DViewCreateObjectItemTextHeight(float)` — XML 명시 범위는 일반 2D 드로잉 객체 텍스트 (Symbol/Point/Line/Polyline/...)
- `Drawing2DTemplateManager.RenderTemplateOnGridStructure` / `TemplateTableData` / `GridStructure` 일체 XML 미등록(internal) → **테이블 셀 적용 보장 SDK 문서로 확인 불가**
- 형제 네이밍(Item vs Measure 분리) 보면 테이블에는 미적용 가능성 높음
- 다만 internal API라 동작 미확정 → 실기 시도가 최종 판정

**코드 변경** ([A2Z/Form1.DrawingSheets.cs:1301](A2Z/Form1.DrawingSheets.cs:1301)):
1. **ColumnWidths 재고정** (1차 값 재적용, 콘텐츠 맞춤 X — 한 번 박음): No 5 / ITEM 20 / MATERIAL 12 / SIZE 14 / Q'TY 7 / T/W 8 / MA 5 / FA 6 (합 77mm)
2. **BOM 렌더 직전 폰트 축소** (L1317~): `Set2DViewCreateObjectItemTextHeight(4f)` → `RenderTemplateOnGridStructure` → `Set2DViewCreateObjectItemTextHeight(7f)` 기본 복원. 풍선용 글로벌 setter 패턴(L1835/1869) 동일 흐름

**빌드 결과로 판정될 2갈래**:
- 폰트 적용됨 → T-037 셀 텍스트 wrap 회피 완료 (DONE 후보)
- 폰트 미적용됨 → SDK 한계 최종 확정, **잔여 옵션** 검토:
  - 헤더 약자화 (사용자 결정 필요)
  - Drawing2D 원시 API로 셀 자체 그리기 (별도 큰 작업)

**영향 범위**: 2D 출력 BOM 테이블만. 흐름 변경 없는 상수 + setter 호출 2줄 → R1 docs 갱신 생략.

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
- 신설: [docs/기술 노트/치수 텍스트 위치.md](../기술 노트/치수 텍스트 위치.md) (T-058 통합 사양)
- 변경 이력: [선택 치수 표시.md](../기능/치수/선택 치수 표시.md), [메인 치수 추출.md](../기능/BOM/메인 치수 추출.md), [가공도 단일.md](../기능/가공도/가공도 단일.md)
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

**이유**: A2Z 일반 데이터셋(직립 H빔·플레이트·앵글)에서 `BBox.MaxZ == max(Osnap.Z)`가 성립해 정렬 결과 동등. 차이 발생 케이스(경사·곡면 Body)도 정렬 1~2칸 변동 수준으로 실용 영향 작음. 회사 회신은 [Sheet1 명명 기준.md](../기술 노트/Sheet1 명명 기준.md) § 7 단답을 그대로 사용 — "BBox 기준이지만 일반 형상에선 명세와 동일 결과"임을 설명. 차후 회사가 Osnap 자체를 강하게 요구하면 그때 신규 작업으로 변경(`Form1.BOM.cs:688` osnapList 1줄 교체) 진행.

**Tracking 갱신**:
- TASKS.md 검토 대기 항목 2 + T-056 본문 — 결정 반영
- Sheet1 명명 기준.md § 6 최종 결정 + § 9 변경 이력 추가

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
- 시트 자동 생성.md 단계 9·10 재기술, 분기 C 갱신, mermaid 그대로(흐름 자체는 동일), 변경 이력 2건

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
- 메인 치수 추출.md Section 7.5 신설 — 회사 doc "치수추출 버튼 앞뒤 로직" 의문 답변
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
- 메인 치수 추출.md (Section 7.5 "치수 캐시 라이프사이클" 신설)
- VIZCore3D 초기화.md (단계 3.5 + 상태 변화 + 변경 이력, T-050)
- 시트 자동 생성.md (변경 이력, T-052)

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
- `시트 자동 생성.md` 단계 9.3 + mermaid + 변경 이력 갱신

**T-055 — Osnap 기준점 검증 보고서 (회사 "완료 3" 의문 답변)**:
- 4경로 보조선 데이터 흐름 + 부재별/전체 풀 동시 적재 + X/Y/Z 뷰별 primary/secondary 매핑 + 4단 dedup(부재 → 전역 dimAxis → MergeCoordinates 0.5mm → keyToDim) 코드 트레이스 완료
- 결론 **부분 일치** — 핵심 의도(코너 우선 + 중복 제거 + 부재/전체 분리)는 모두 구현되었으나, 부재 단위에서 4코너가 아니라 1점만 남기는 점이 명세 문구와 다름
- 산출물: `docs/기술 노트/Osnap 기준.md` (회사 doc 갱신용 단답 포함)

**T-056 — Sheet1 Z-MAX 정렬 검증 보고서 (회사 "완료 5" + "수정 후 확인 필요 2" 의문 답변)**:
- 현재 코드는 `BBox.MaxZ`(Form1.BOM.cs:735) 기준 정렬, 회사 명세는 `max(Osnap.Z)` 기준 — 데이터 출처 차이
- 직립 H빔·평판 등 일반 철골 형상에선 두 값이 동등하여 정렬 결과 같음. 경사 부재·곡면 Body에서 수 mm 차이로 정렬 1~2칸 흔들림 가능
- 결론 **부분 일치** — 회사 답변에 따라 후속 작업(Form1.BOM.cs:688 osnapList 활용 한 줄 변경) 신설 가능
- 산출물: `docs/기술 노트/Sheet1 명명 기준.md`

**Tracking**:
- `TASKS.md` TODO → DONE 4건 이동
- `STATUS.md` 마지막 작업 / WIP / 다음 할 것 갱신
- `CHANGELOG.md` 본 항목 추가

**관련 docs (신규/갱신)**:
- 신규: `docs/기술 노트/치수 보조선 사양.md`, `Osnap 기준.md`, `Sheet1 명명 기준.md`
- 갱신: `docs/기능/도면시트/시트 자동 생성.md`, `docs/기능/가공도/가공도 단일.md`

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
- `가공도 단일.md` 4차 1~5단계 변경 이력 추가
- `시트 선택.md` 4차 3·4·5단계 변경 이력 추가

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
- docs: `가공도 단일.md` / `시트 선택.md` 변경 이력 각 1건
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
- docs `가공도 단일.md` 변경 이력 갱신
- MSBuild Debug 통과

**영향 범위**: 가공도 시트 선택 시 화면 전환 부드러움 + Z 최장축 90° 회전 후 화면 정합. 회전 로직 자체는 무변경

**추가 확인 필요**: 이 수정으로 "가로→세로 깜빡"이 사라지는데, 만약 **최종 결과가 여전히 세로**라면 `DiagLog T-036 MfgDrawing bom=... longestAxis=... use180=...` 로그 공유 필요. 회전 순서 자체를 재설계해야 할 수 있음

---

## 2026-04-23 — T-036 재해석: 가공도 시트 ISO 뷰 느낌 해결

**유형**: fix
**커밋**: `b0f8802`
**관련 TASK**: T-036
**배경**: 직전 커밋(`537f07c`)은 "Z 최장축 세로 배치"로 해석해 L215 180° 스킵 가드 추가. 사용자 실기 재보고 "45도 대각 ISO 뷰로 보게 된다" → Z 축 방향이 아닌 **카메라 방향 자체가 ISO**라는 다른 증상 확인

**원인 확정**: [LvDrawingSheet_SelectedIndexChanged](../기능/도면시트/시트 선택.md) 공통부의 `FlyToObject3d(sheet.MemberIndices, 1.2f)`가 이전 카메라 방향(예: 직전 글로벌 ISO 버튼 상태)을 **그대로 유지한 채 객체로 이동**. 그 후 호출되는 `ExecuteMfgDrawing`의 `MoveCamera(X/Y/Z_PLUS)`가 SDK 비동기 렌더 사이에 묻혀 덮어쓰지 못하는 현상으로 추정

**변경 사항**:
- [Form1.DrawingSheets.cs `LvDrawingSheet_SelectedIndexChanged` L542~](../../A2Z/Form1.DrawingSheets.cs): 가공도(-3) 시트일 땐 `FlyToObject3d` **스킵**. `ExecuteMfgDrawing`이 자체 카메라·FitToView·visibility를 모두 세팅하므로 충돌 제거
- [Form1.MfgDrawing.cs L254](../../A2Z/Form1.MfgDrawing.cs): 직전 커밋의 `if (use1803d && longestAxis != "Z")` 가드 **원복** → 원래 `if (use1803d)`. ISO 원인과 무관한 수정이고 Z 최장축 수직 뒤집기 효과를 잃게만 했기 때문. `use1803d` 변수의 블록 바깥 스코프 승격은 유지 (DiagLog 가시성)
- docs: `시트 선택.md` 변경 이력(T-036 재조정), `가공도 단일.md` 변경 이력(재해석·원복)
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

- docs: `글로벌 ISO.md` 변경 이력(T-034 후속) / `가공도 단일.md` 변경 이력(T-036 수정)
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
- docs 갱신: `메인 치수 추출.md` T-033 변경 이력 / `글로벌 ISO.md` T-034·T-035 상태 변화·이력 / `가공도 단일.md` T-036 변경 이력
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
- docs `메인 치수 추출.md` 단계 12·13 재기술 + 변경 이력
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
- docs `시트 선택.md` 분기 A 재기술 + 변경 이력

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
- docs `가공도 단일.md` 상태 변화 표(`View.RenderMode` 행) + 변경 이력 갱신

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
- docs `메인 치수 추출.md` 단계 14.5(3D 뷰 정리) 추가, 상태 변화에 `Review.Measure` 행 갱신, 변경 이력

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
- docs 갱신: `메인 치수 추출.md` 단계 13·변경 이력 / `시트 선택.md` 분기 A 재작성·변경 이력
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
- docs `메인 치수 추출.md` 단계 13.5 추가, 변경 이력 1건
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
- docs 3종 전면 갱신 — `메인 치수 추출.md` 흐름도 재작성 + 단계표 3섹션(btn/Clash 이벤트/Post) + 분기 C·D + E03/E04 / `간섭검사 완료 이벤트.md` 개요·단계 9·10·분기 B·E03·상태 변화 2열 / 사용자 매뉴얼 `치수 추출.md` 내부 흐름·분기·에러 ③
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
- docs: `메인 치수 추출.md` 단계 1.3 (xray clear) / `시트 자동 생성.md` 단계 9.5 (BOM 자동 수집) 추가, 변경 이력 각 1건
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
- docs 원복: `메인 치수 추출.md` 단계 1.5 · 분기 D · E04를 "비활성" 표기로 교체, 사용자 매뉴얼 `치수 추출.md` 에러 ③/단계 1-2 삭제 후 "향후 추가 예정" 예고 문구로 치환
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
- 개발자 문서 `메인 치수 추출.md`: 사전조건 항목 1건·단계 1.5·에러 E03 추가 (기존 E03은 E04로 재번호)
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
- docs: `메인 치수 추출.md` 단계표 10→13 재번호 + 분기 C 신설, `간섭검사 완료 이벤트.md` 단계 10 재기술 + 분기 A 수정
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
- docs 2종(`시트 선택.md`, `BOM 정보 선택.md`) 단계표·상태 변화·변경 이력 갱신

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
- 문서 `메인 치수 추출.md` 단계표를 10→12단계로 확장 (2·12단계에 오버레이 표시·해제 추가), 변경 이력 1건

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
- docs: `code-reference/form1-bom.md` 라이선스 항목 5곳(헤더 라인 수·핸들러 설명·헬퍼 표·필드 표·API 사용) 정리, `code-reference/form1-license.md` 신설, `기능/BOM/VIZCore3D 초기화.md` 단계표·E01 에러·관련 링크·변경 이력 갱신
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
  - 문서 `시트 자동 생성.md` 단계 10 설명·상태 변화 섹션·변경 이력 갱신
- **T-021 (`lvDrawingBOMInfo` 행 선택 핸들러)**: BOM 테이블 행 선택 시 해당 부재로 카메라만 fit
  - 가시성은 그대로 두고 `vizcore3d.View.FlyToObject3d(new List<int>{bodyIdx}, 1.2f)` — 현재 시트 맥락 유지
  - No. 컬럼 파싱 → `bomList[No-1].Index` Body 조회 (CollectBOMInfo의 `partIndexToBomNo` = `bi+1` 매핑과 일치)
  - 요약행(Row 0) · No 파싱 실패 · 범위 초과는 조용히 return, SDK 예외는 `DiagLog`로 기록
  - Form1.cs L166에 `lvDrawingBOMInfo.SelectedIndexChanged += LvDrawingBOMInfo_SelectedIndexChanged` 등록
  - 신규 문서 `BOM 정보 선택.md` (SHT-010) + `_인덱스.md` 등록 추가

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
- 문서 `시트 자동 생성.md` 전면 갱신: flowchart 재작성, 단계표 11단계로 확장(Part↔Body 매핑·인접 리스트·가공도·중복 제거 추가), 분기 B·C 재정의, 상태 변화 시트 수 공식, 변경 이력 한 줄
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
  - `docs/기능/BOM/초기화.md` (BOM-005)
  - `docs/사용자-매뉴얼/1.기본-작업/초기화.md`
- 문서 갱신:
  - `docs/기능/BOM/모델 열기.md` — Close 단계 추가, flowchart·step table·변경 이력
  - `docs/기능/BOM/_인덱스.md` — BOM-005 항목 + 의존성 다이어그램 재로드 화살표
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
- 개발자 문서(`docs/기능/`, `docs/code-reference/`)는 건드리지 않음

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
  - 카테고리 8개 (BOM/간섭검사/치수/2D도면/도면시트/글로벌뷰/가공도/attribute)
  - 핸들러 문서 48개 (버튼/이벤트 단위 Step-by-step 흐름)
  - 코드 레퍼런스 9개 (Form1.*.cs + Models.cs)
  - 최상위 가이드 5개 (README/용어집/파이프라인/템플릿/작성가이드)
- `.gitignore` 보강 (VS/.NET/NuGet/Claude Code 로컬 설정 등)
- `CLAUDE.md` — Claude Code 작업 규칙 R1~R7
- `docs/tracking/` — FEEDBACK/TASKS/CHANGELOG/sessions 4축 구조
- `.claude/commands/commit.md` — `/commit` 슬래시 커맨드 (docs 동기화 + CHANGELOG/TASKS 갱신 + 커밋)

**영향 범위**: 전체 저장소 구조 (코드 변경 없음)
