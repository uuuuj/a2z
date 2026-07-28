# 작업 목록 — DONE (완료 이력)

> ⬅ [TASKS 인덱스](../TASKS.md)  ·  [TODO](./TODO.md) · [IN_PROGRESS](./IN_PROGRESS.md) · [BLOCKED](./BLOCKED.md) · [DONE](./DONE.md)

---

### T-012 — 엑셀 템플릿 하이브리드 실험 (PoC) — 폐기
- **생성일/착수일**: 2026-04-20 / 2026-05-12
- **상태**: 폐기 (2026-07-28)
- **완료 판정**: 실험 목적이 달성돼 메인 흐름에 흡수됨 — 남은 Step 2·3은 무의미해져 중단. PoC 버튼·핸들러·흐름 문서 제거 (커밋 `9a1c0b2`)
- **관련**: REQ-002, GitHub issue #71 (작업 중 발견)
- **배경**: SDK가 `ImportExcel`, `ImportExcelWithData`, `Draw2DViewTemplate`, `RenderTemplateOnGridStructure`를 제공 ([VIZCore3D.NET.xml:29219](../../../lib/VIZCore3D.NET.xml:29219)). 담당자가 엑셀로 양식을 관리할 수 있는지 실험. 과거 Phase 18(`790a02a`)에서 BOM 동적 행수 문제로 수동 구성으로 되돌린 이력이 있어 하이브리드로 재도전
- **진행 결과**:
  - [x] **Step 1 (2026-05-12)**: `btnExcelTemplatePoC` 핸들러 + `ImportExcel(path)` 단독 호출, 빌드 통과
  - [x] **확정**: `Drawing2DTemplateManager.templateDatas`는 private/internal이라 외부 접근 불가
  - [x] **Step 4**: `Set2DViewTemplateMark` + `ImportExcelWithData` + `GetViewAreasFromExcel` 3종 동시 검증 성공 → **이 패턴이 `GenerateSheetDrawing2D_WithExcelTemplate`로 이관돼 프로덕션 가동 중**
  - [–] Step 2·3(셀 좌표 직접 수집 + 수동 배치): Step 4가 SDK 자동 적용으로 해결해 불필요
  - [–] 결과 리포트 `docs/기술 노트/excel-template-experiment.md`: 미작성 — 결론이 코드와 [시트 2D 렌더.md](../../기능/도면시트/시트%202D%20렌더.md)에 반영됨
- **폐기 시점의 상태 (2026-07-28 확인)**:
  - 짝이던 구 템플릿 `사용자템플릿_엑셀_제작도.xlsx`(BOM 15행 체계)가 커밋 `0e94aa9`에서 삭제돼, 버튼을 눌러도 "엑셀 파일을 찾을 수 없습니다" 에러로만 끝났다
  - 애초에 화면에도 보이지 않았다 — 아래 영향 파일의 `groupBox1 너비 +87px`가 실제로 적용되지 않아 버튼(X=446~524)이 groupBox1(폭 443) 밖으로 잘렸다. `splitContainer1` 분할선을 오른쪽으로 85px 이상 끌었을 때만 노출
  - 현행 템플릿 `제작도_도면_1.xlsx`로 되살리려면 BOM 슬롯 240 체계 재매핑 + `{Image_1~4}` 매핑 전환이 필요한데, 그러면 메인 흐름과 거의 같은 코드가 하나 더 생긴다 → 폐기 선택
- **영향 파일**: A2Z/Form1.ExcelTemplate.cs, A2Z/Form1.Designer.cs(버튼 1개 — `groupBox1` 너비 확장은 미적용), A2Z/A2Z.csproj, docs/기능/도면시트/엑셀 템플릿 PoC.md (삭제, git 이력 보존)

---

### T-091 — 장시간 도면 작업 취소 응답성 개선
- **생성일/착수일**: 2026-07-24
- **상태**: DONE (2026-07-26)
- **완료 판정**: 2026-07-26 사내 실기 검증 정상 · GitHub issue #51 종료 · Excel No.84 완료
- **관련**: 사용자 직접 지시, GitHub issue #51
- **구현**:
  - [x] 처리 오버레이 버튼을 `취소`로 단순화하고 현재 SDK 호출 뒤 가장 가까운 안전 지점에서 중단됨을 안내
  - [x] 메인 치수 추출의 전체 BODY 대상 스캔과 매 부재 BOM·홀 정보·Osnap, 목록 구성 루프에 진행 수와 `Application.DoEvents` 체크포인트 추가
  - [x] 대상 BODY 5,000개 이상이면 예상 장시간과 STRU 격리를 안내하고 기본 `취소`인 사전 확인 표시
  - [x] 취소 시 부분 BOM·시트·2D·3D·치수·Osnap 상태를 정리하고 관련 도면 출력 컨트롤 원상 복원
  - [x] 제작도·조립도·설치도 2D 생성의 엣지·템플릿·ISO/Z/X/Y 뷰·최종 렌더 전후 취소 확인
  - [x] 수동/일괄 가공도의 페이지·템플릿·행별 장면·주/보조 뷰·최종 렌더·PDF 저장 전후 취소 확인
  - [x] 가공도 취소 시 미완성 Canvas 정리, 저장 완료 PDF 수와 중단 위치 유지
  - [x] Debug·Release 빌드 오류 0개 (기존 경고만 유지)
- **실기 확인 결과 (2026-07-26 사내 PC, 모두 정상)**:
  - [x] 구역 전체가 보이는 상태에서 치수 추출 시 BODY 진행 수가 갱신되고 5,000개 이상 경고가 먼저 뜨는지
  - [x] BOM 수집 중 취소 후 수초 내 종료되고 BOM·시트·2D·3D 임시 결과가 남지 않는지
  - [x] 제작도·조립도·설치도 종류별 출력에서 현재 뷰 완료 직후 다음 뷰/시트로 넘어가지 않는지
  - [x] 수동 가공도에서 행·EA 보조 뷰 사이 취소가 동작하고 저장 완료 PDF만 유지되는지
  - [x] 도면 일괄 출력 취소 요약의 PDF 수와 실제 파일 수가 일치하는지
- **영향 파일**: `A2Z/Form1.cs`, `A2Z/Form1.BOM.cs`, `A2Z/Form1.DrawingSheets.cs`, `A2Z/Form1.MfgDrawing.cs`, `A2Z/Form1.Stru.cs`, 관련 BOM·도면시트·가공도 흐름과 코드 레퍼런스

### T-089 — 가공도 풍선 종이 절대 정규화·EA 뷰별 독립 배정
- **생성일/착수일**: 2026-07-24
- **상태**: DONE (2026-07-26)
- **완료 판정**: 2026-07-26 사내 실기 검증 정상 · GitHub issue #46 종료 · Excel No.82 완료
- **관련**: 사용자 직접 지시, GitHub issue #46
- **구현**:
  - [x] Hole/SlotHole 관통축을 `ThicknessCenterTo - ThicknessCenterFrom` 우선으로 추출하고, 원형 홀 `CircleCenter`·SDK 로컬축 폴백을 교차검증
  - [x] EA 첫 번째·두 번째 실제 깊이축과 관통축을 비교해 뷰에 먼저 배정한 뒤 뷰별 규격·개수 그룹화
  - [x] 첫 번째·두 번째 `PendingNotes`를 독립 관리하고 각 캡처 직전 Note를 격리
  - [x] 모델 span 비례 EarthBoss 4분면 배치 제거, EarthBoss를 첫 번째 뷰 지연 생성으로 통합
  - [x] 캡처 후 확정된 `newScale`로 치수 외곽→풍선 6mm·풍선 행 8mm를 역산
  - [x] 풍선과 같은 화면 위·아래 쪽 치수선·문자 여백을 모델 fit 전에 예약해 잘림과 EA 반대 뷰 침범 방지
  - [x] EA 첫 번째·두 번째 풍선 목록의 최대 행 수와 최대 치수 외곽을 공통 예약해 한쪽 목록이 0건이어도 두 뷰의 모델 fit 높이를 동일하게 유지
  - [x] 풍선 글자 6mm·치수 글자 10mm는 2D 종이 절대값으로 유지
  - [x] Debug·Release 빌드 오류 0개 (기존 경고만 유지)
- **실기 확인 결과 (2026-07-26 사내 PC, 모두 정상)**:
  - [x] 크기 차가 큰 부재 5개를 한 페이지에 배치해 풍선 거리·행 간격·글자 크기가 같은지
  - [x] EA 첫 번째 목록이 비고 두 번째 목록에만 Hole/SlotHole이 있는 부재에서 두 번째 뷰에만 표시되는지
  - [x] 같은 EA 부재의 두 `[MfgAnnotationBudget]` 로그에서 `requested`·`reserved`·`fitHeight`가 같고 화면상 모델 배율도 같은지
  - [x] 같은 규격의 홀이 두 플랜지에 나뉜 경우 뷰별 개수가 각각 맞는지
  - [x] 세로 가로화·ORIENTATION·상하 미러 EA와 EarthBoss 위치에 회귀가 없는지
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, `A2Z/Models.cs`, `A2Z/Models/MfgViewPose.cs`, 가공도 시트·미리보기 흐름과 코드 레퍼런스

### T-087 — 도면 종류별 출력 버튼
- **생성일/착수일**: 2026-07-24
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #47 종료·Excel No.77 완료
- **관련**: 사용자 직접 지시, GitHub issue #47
- **구현**:
  - [x] 도면정보 탭에 제작도·조립도·설치도 버튼 추가, 가공도 출력 문구를 가공도로 변경
  - [x] `BaseMemberIndex` 기준으로 기존 시트 목록을 종류별 필터링
  - [x] 프로그램 시트 선택 이벤트 억제 후 `ApplySheetSelection` 1회 실행
  - [x] 출력 버튼과 시트 목록 재진입 차단, 취소 예외 분리와 원상 복원
  - [x] SDK 안정화 대기 유지, 마지막 성공 시트 캔버스만 유지
  - [x] 설치도 파일명 중복 방지와 종류별 완료·실패·취소 요약
  - [x] Debug·Release 빌드 오류 0개 (기존 경고만 유지)
- **사용자 확인 필요**:
  - [x] 제작도·조립도·설치도 버튼이 해당 종류의 PDF만 생성하는지
  - [x] 조립도 여러 장 연속 출력과 현재 작업 후 취소가 정상인지
  - [x] 마지막 성공 도면은 남고 실패·취소 캔버스는 정리되는지
  - [x] 기존 가공도·2D 출력·PDF 출력·도면 일괄 출력에 회귀가 없는지
- **영향 파일**: `A2Z/Form1.Designer.cs`, `A2Z/Form1.DrawingSheets.cs`, 도면 종류별 출력 흐름·도면시트 코드 레퍼런스

### T-086 — 가공도 홀·슬롯 최종 화면 하단 배치
- **생성일/착수일**: 2026-07-23
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #39 종료·Excel No.58·59 완료
- **관련**: 사용자 직접 지시, GitHub issue #39
- **원인 확인**:
  - [x] Hole/SlotHole Note를 `BuildMfgSceneCore`에서 회전 전 하단에 만든 뒤 `ProbeAndRollLandscape`가 화면축 90도를 추가해 회전 전 아래쪽이 최종 화면 왼쪽이 되는 순서 확인
  - [x] Z 최장축 자체가 아니라 임시 투영 결과 `probeH > probeW`가 실제 가로 전환 조건임을 코드·로그로 확인
  - [x] `LockZAxis=false`는 월드 Z축 해제가 아니라 카메라 화면축 회전 허용 설정임을 이슈 본문에 정정
- **구현**:
  - [x] Hole/SlotHole을 즉시 Review Note로 만들지 않고 `MfgViewPose.PendingHoleNotes`에 수집
  - [x] `ProbeAndRollLandscape` 완료 후 `AddMfgPendingHoleNotes`에서 최종 화면 하단에 생성
  - [x] ReferenceAxis 로컬축, ReferenceAxis 실패 폴백 ORIENTATION roll, 추가 90도 가로화 roll을 최종 화면 기저에 반영
  - [x] 월드 BBox 8개 꼭짓점을 최종 화면 수직축에 투영해 하단을 구하고 홀 중심의 화면 수평 위치를 유지
  - [x] 회전 없는 정상 부재는 기존 하단 배치 공식과 동일한 결과 유지
  - [x] Release 빌드 오류 0개 (기존 경고 7개)
- **사용자 확인 필요**:
  - [x] `가로전환(90°)` 로그가 나오는 홀·슬롯 부재에서 표시가 최종 도면 하단에 나오는지
  - [x] ORIENTATION/ReferenceAxis 부재에서도 리더가 수직이고 텍스트가 하단에 나오는지
  - [x] `가로유지` 정상 부재와 EA 첫 번째 뷰의 기존 출력에 회귀가 없는지
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, `A2Z/Models/MfgViewPose.cs`, 가공도 시트 흐름·코드 레퍼런스 문서

### T-085 — 도면 일괄 출력·치수 추출 중간 취소
- **생성일/착수일**: 2026-07-23
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #34 종료. 대용량 즉시 취소 응답성은 별도 작업으로 분리
- **관련**: 사용자 직접 지시, GitHub issue #34
- **구현**:
  - [x] 처리 오버레이를 메시지+`현재 작업 후 취소` 버튼 패널로 확장
  - [x] UI 스레드 협력적 취소 상태(`_cancelRequested`)와 재진입 차단 추가
  - [x] 메인 치수 추출의 BOM·비동기 간섭검사·Osnap·치수·시트 생성 전후 체크포인트 연결
  - [x] STRU 일괄 출력의 STRU·일반 시트·가공도 페이지 전후 체크포인트 연결
  - [x] 간섭검사 진행 중 취소는 현재 SDK 검사 완료 뒤 다음 ID를 시작하지 않도록 무창 큐 정리
  - [x] 취소 후 부분 시트·2D·3D·치수·Osnap 상태와 전체 BODY·버튼 복원
  - [x] 일괄 출력 취소 요약에 처리 완료/전체 STRU, 성공·실패·미처리, 실제 저장 PDF 수 표시
- **검증**:
  - [x] Debug Rebuild 오류 0개 (기존 경고 7개)
  - [x] BOM 수집·간섭검사·일반 PDF·가공도 페이지 각각에서 취소 버튼 실기 확인
  - [x] 취소 직후 재실행 시 이전 시트·치수·2D 객체가 남지 않는지 확인
  - [x] 완료된 PDF가 유지되고 요약 PDF 수와 실제 파일 수가 같은지 확인
- **영향 파일**: `A2Z/Form1.cs`, `Form1.BOM.cs`, `Form1.Clash.cs`, `Form1.Stru.cs`, `Form1.MfgDrawing.cs`, 관련 흐름·코드 레퍼런스 문서

### T-079 — 기울어진 가공도·제작도 참조축 자동 정렬
- **생성일/착수일**: 2026-07-22
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #19 종료·Excel No.21 완료. 문자 각도는 별도 API 대기로 분리
- **관련**: 사용자 직접 지시, GitHub issue #19, Softhills Demo `참조축 정렬 > 선택 부재 자동 정렬`
- **판정·구현**:
  - [x] 실기 4건 대조 결과(ORIENTATION 4/4, 기하 1/4)에 따라 ORIENTATION을 최종 판정의 1차 신호로 승격
  - [x] ORIENTATION이 비었거나 파싱 불가할 때만 LINE Osnap 길이 합 주축 판정을 폴백으로 사용
  - [x] `[참조축판정]`에 ORIENTATION·기하 결과·최종 판정 출처를 병기
  - [x] 전체 Osnap 수집 때 판정 결과를 함께 캐시하고 캐시 없는 진입만 SDK 직접 조회
  - [x] ORIENTATION 1도 초과 부재는 원문의 방향 두 축을 직교화하고 외적으로 나머지 축을 복원해 로컬 X/Y/Z 프레임 구성
  - [x] BBox 중심에 `ReferenceAxis.Create/Activate` 후 로컬축 기준 `MoveCamera`를 적용해 화면 roll로 세워지지 않던 부재 형상 정렬
  - [x] 정상 부재는 기존 월드축 카메라 유지, ReferenceAxis 생성 실패 시 기존 `ApplyOrientationRotation`으로 자동 폴백
  - [x] ORIENTATION 1도 초과 가공도 부재만 로컬 X/Y/Z 축 벡터를 정규화해 `AddCustomDistanceUserAxis`로 치수 생성
  - [x] 로컬 오프셋축 BBox 투영으로 치수선·보조선 위치를 맞추고 UserAxis 실패 시 기존 월드축 치수로 자동 폴백
  - [x] `Measure.Clear`가 참조축도 삭제하는 SDK 동작에 맞춰 Clear 전 Reset/Delete, PDF 행·EA 2차 뷰마다 참조축 재생성
  - [x] 3D 미리보기의 참조축·화면 roll을 다음 가공도 선택 또는 다른 시트 진입 때 원복
  - [x] 정상 부재는 선택 조건·호출 인자 기본값·월드축 API를 포함해 기존 경로 유지
  - [x] 별도 출력 폴더 Debug 빌드 오류 0건
  - [x] 사용자 재보고 이미지가 가공도 단일 행이 아니라 제작도 1번 시트의 4면도 출력임을 로그(`sheet#=1`, `kind='제작도'`, `GenerateSheetDrawing2D_WithExcelTemplate`)로 확인
  - [x] 제작도 시트 부재의 Osnap LINE 중 XY 투영 최장선을 기준으로 수평 ReferenceAxis 프레임 구성. 세계축과 1° 이내면 기존 경로 유지
  - [x] 제작도 ISO/Z/X/Y마다 ReferenceAxis 카메라 적용 후 Reset/Delete하고, 조립도·설치도에는 미적용
  - [x] 제작도 체인 치수 Osnap을 같은 로컬축으로 변환하고 `AddCustomDistanceUserAxis`로 치수·보조선 복원
  - [x] 참조축 생성 실패 시 카메라뿐 아니라 치수 목록도 WorldAxis로 재계산해 혼합 좌표 방지
  - [x] 제작도 참조축 확장 후 별도 출력 폴더 Debug 빌드 오류 0건
- **사용자 확인 필요**:
  - [x] 문제 브래킷 3D 미리보기에서 부재 형상이 먼저 수평/수직으로 바로 서는지
  - [x] `[MfgRefAxis] frame`·`activate`가 출력되고 다른 부재/시트 선택 때 `release`가 출력되는지
  - [x] 기울어진 일반 가공도 PDF에서 모델·치수선·보조선이 함께 수평/수직으로 보이는지
  - [x] `[MfgUserAxis]`와 `[DimAdd] ... mode=UserAxis`가 출력되고 치수값이 실제 길이와 맞는지
  - [x] 정상 부재가 이전 출력과 동일하고 `mode=WorldAxis`를 유지하는지
  - [x] 여러 부재를 연속 출력해 카메라 회전·치수 상태가 다음 행에 누적되지 않는지
  - [x] 일반 부재 확인 후 EA 두 뷰에서도 동일하게 검증
  - [x] 문제 어셈블리의 제작도 1번 시트 평면·정면·측면에서 45° 기울기가 사라지고 모델·치수·보조선이 함께 수평/수직인지
  - [x] 제작도 로그에 `[DrawingRefAxis] frame`(약 45°)과 ISO/Z/X/Y별 `activate/release`, `[DimAdd] ... mode=UserAxis`가 출력되는지
  - [x] 정상 제작도는 `[DrawingRefAxis] 세계축 정렬 상태` 뒤 기존 WorldAxis 출력을 유지하는지
  - [x] 조립도·설치도 출력이 이전과 동일한지
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, `A2Z/Form1.DrawingSheets.cs`, `A2Z/Form1.cs`, `A2Z/Models.cs`, `A2Z/Models/MfgViewPose.cs`, `A2Z/Form1.Dimensions.cs`, 가공도·시트 선택·시트 2D 렌더 흐름 문서, 가공도·도면시트 코드 레퍼런스

### T-078 — 도면 시트 3D 임시 치수 잔류 제거
- **생성일/착수일**: 2026-07-22
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #21·22 종료·Excel No.68·69 완료
- **관련**: 사용자 직접 지시, GitHub issue #21·#22
- **확인 결과**:
  - [x] ISO/X/Y/Z 버튼 중 X/Y/Z와 2D 도면은 동일한 `chainDimensionList`·`ShowAllDimensions(viewDirection)` 경로 사용
  - [x] 설치도 준비 목록에는 선택 STRU↔직접 연결 Part 접합 위치 필수 치수가 포함돼 X/Y/Z 미리보기와 2D 도면에 함께 표시
  - [x] ISO 분기는 `Review.Note`만 지워 직전 X/Y/Z의 `Review.Measure`·`ShapeDrawing`이 남는 원인 확인
  - [x] 2D 생성은 마지막 축 치수를 2D로 복사한 뒤 3D 임시 측정·보조선을 정리하지 않는 원인 확인
- **구현**:
  - [x] `Clear3DDimensionAnnotations()` 공통 헬퍼 추가
  - [x] ISO/X/Y/Z 공통 진입에서 직전 3D 치수·보조선 제거 — ISO는 풍선만 표시
  - [x] `GenerateSheetDrawing2D` 전체를 `try/finally`로 감싸 엑셀·fallback·예외 경로 모두 3D 임시 치수 제거
  - [x] 실행 중 A2Z가 기본 출력 EXE를 잠근 상태에서 별도 출력 폴더 Debug 빌드 오류 0건
- **사용자 확인 필요**:
  - [x] X/Y/Z → ISO 전환 시 ISO에 풍선만 남는지
  - [x] 설치도 X/Y/Z와 2D 도면의 연결 위치 치수가 동일하게 유지되는지
  - [x] 2D 도면 생성 직후 3D 치수가 비고, 다른 모델을 연속 출력해도 이전 치수가 이월되지 않는지
- **영향 파일**: `A2Z/Form1.DrawingSheets.cs`, ISO/X/Y/Z·시트 2D 렌더 흐름 문서, 도면시트 코드 레퍼런스

### T-077 — 가공도 3D 미리보기 형상 풍선 제거
- **생성일/착수일**: 2026-07-22
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #18 종료·Excel No.63 완료
- **관련**: 사용자 직접 지시, GitHub issue #18
- **배경**: 도면번호 목록에서 가공도 부재를 선택할 때 3D 화면에 Hole·SlotHole·EarthBoss 풍선이 표시돼 형상 확인을 방해한다. PDF에는 기존 가공 정보 풍선이 계속 필요하다.
- **구현**:
  - [x] 공통 `BuildMfgSceneCore`의 풍선 생성 유지
  - [x] 3D 미리보기 전용 `ExecuteMfgDrawing`에서 코어 호출 직후 Review Note 제거
  - [x] PDF `RenderMfgRowToViewArea` 경로 무변경으로 풍선 유지
  - [x] 사내 모델에서 일반/EA 3D 미리보기 풍선 미표시 확인
  - [x] 가공도 PDF의 Hole·SlotHole·EarthBoss 풍선 유지 확인
- **영향 파일**: `A2Z/Form1.MfgDrawing.cs`, 가공도 미리보기/PDF 흐름 문서, 가공도 코드 레퍼런스

### T-075 — 설치도 기준 Part 끝단→연결 Part 모서리 위치 치수
- **생성일/착수일**: 2026-07-21
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #12·29 종료·Excel No.57 완료. 연결부재·외부 Assembly 점선 표시(Excel No.17)는 2026-07-26 사내 실기 PDF에서 정상 확인
- **관련**: 사용자 직접 지시, GitHub issue #12
- **배경**: 설치도 목적은 외부 연결 Part가 선택 STRU측 실제 연결 Part의 끝에서 얼마나 떨어져 설치되는지 확인하는 것이다. 접합 중심·A1/A2는 이 위치 판단에 직접 쓸 수 없으므로, 실제 접촉 Body의 끝단과 접합측 모서리 Osnap을 치수 기준으로 사용한다.
- **구현**:
  - [x] 설치 문맥을 부모 Assembly 인덱스가 아니라 직접 연결 외부 Part 인덱스로 저장. 부모 Assembly는 ISO 이름 노트 문맥으로만 유지
  - [x] ISO/Z/X/Y에서 선택 STRU 실선 + 직접 연결 외부 Part만 점선으로 적용
  - [x] 설치도 4개 뷰를 선택 STRU 기준으로 fit하고, STRU 기준 CropFit으로 긴 연결 부재의 접합 주변만 남김
  - [x] Clash PART 쌍 하위 BODY 조합에서 `GetObjectCollisionLine`, `GetJunctionMesh`로 실제 접합 영역 산출
  - [x] 접합선·Mesh·HotPoint는 Target/Connected Body 쌍과 접합측 모서리 판정용 내부 자료로 유지
  - [x] 선택 STRU·연결 Assembly 전체 범위 치수를 모두 제거하고 실제 `Target 끝단 → Connected 모서리` 연결 거리만 유지
  - [x] Target Body LINE 방향 5도 군집·길이 합 최대 기준으로 길이축 판정, 가까운 끝단 Osnap 선별
  - [x] Connected Body LINE/POINT 중 접합영역과 가장 가까운 실제 모서리 선별
  - [x] 같은 Target/Connected Body 쌍의 A1/A2를 병합하고 `Target 끝단 → Connected 모서리` 필수 위치 치수 생성
  - [x] X/Y/Z의 A/A1/A2 접합점 기호 제거, ISO 이름은 연결 Part당 접합측 모서리에 1개
  - [x] 설치도 3D 미리보기·옛 2D·엑셀 2D 모두 같은 `PreparedDimensions` 사용 — 공용 Osnap 치수 덮어쓰기 제거
  - [x] 설치도 2D 모델 배치·CropFit·Match 후 실제 객체 배율로 치수·보조선 생성 — 뷰별 종이 길이 편차 제거
  - [x] 접합 형상 없는 Clearance/Proximity는 HotPoint fallback + 로그
  - [x] Debug 별도 출력 폴더 빌드 통과
  - [x] 사내 모델에서 치수가 선택 STRU측 실제 접촉 Body의 가까운 끝단에서 시작하는지 확인
  - [x] 연결 Part의 접합측 실제 모서리에서 치수가 끝나고 같은 Body 쌍 치수가 중복되지 않는지 확인
  - [x] 직교 뷰 A/A1/A2가 사라지고 ISO Assembly/Part 이름은 연결 Part당 하나인지 확인
  - [x] 긴 외부 Assembly가 있어도 선택 STRU가 충분히 크게 나오고 접합 주변만 남는지 확인
  - [x] 3D 미리보기와 PDF의 설치 위치 치수가 동일한지 확인
  - [x] X/Y/Z PDF에서 보조선 오프셋·시작 gap이 도면 기준으로 같은 길이인지 확인
- **영향 파일**: `A2Z/Form1.GlobalViews.cs`, `A2Z/Form1.DrawingSheets.cs`, `A2Z/Form1.Dimensions.cs`, `A2Z/Models.cs`, 설치도/Osnap 문서

### T-013 — ISO 뷰 점선·실선 분리와 3D 위치 정합
- **생성일**: 2026-04-20
- **착수일**: 2026-04-21
- **재개일**: 2026-07-21
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #7 종료·Excel No.15·16 완료
- **관련**: 사용자 피드백, GitHub issue #7
- **배경**: 조립도는 전체 구조 중 기준부재만 실선, 제작도는 시트 부재 실선과 붙어 있는 시트 밖 주변 부재를 점선으로 표시해야 한다. 옛 수동 정합 방식은 캡처 원점·좌표계 불일치로 실패했다.
- **구현**:
  - [x] `Match2DObjectsTo3DObjectPosition(실선, 점선)`으로 옛 WorldToScreen 수동 정합 교체
  - [x] 조립도: 전체−기준부재 LONG_DASHED 점선 + 기준부재 실선
  - [x] 제작도: 전체 Body BBox·실제 부모 Part 1회 캐시 → 3mm 근접 후보 선별 → 후보만 전용 그룹 간섭검사
  - [x] 제작도: 연결 결과를 내부 연결성 `clashList`와 분리하고 `lvClash`에 `[연결]` 목록 표시
  - [x] 제작도: Crop 대상 2D 객체에 시트 부재+연결 부재를 함께 넣고 시트 부재 노드 기준으로 긴 연결 부재 절단
  - [x] 제작도: 이웃 캡처 → CropFit → LONG_DASHED → 점선 fit → 실선 캡처 → Match 순서 적용
  - [x] 제작도: Clash HotPoint XYZ를 보존하고 연결 Part의 가장 가까운 부모 Assembly 이름을 `AddNoteSurface` → `Add2DNoteFrom3DNote`로 접촉점에 표시
  - [x] 조립도: 실기 정상인 기존 캡처·배치 순서 유지(제작도 전용 순서 변경에서 제외)
  - [x] C# Compile 오류 0건 (기존 경고 7건)
- **사용자 확인 필요**:
  - [x] 제작도 ISO에 붙어 있는 주변 부재가 점선으로 표시되는지
  - [x] 진단 로그의 BBox 캐시 시간·근접 후보 수·원본 Clash 결과·상대 Part 목록 확인
  - [x] Crop 범위가 붙은 부위 주변만 남기며 너무 좁거나 넓지 않은지
  - [x] 연결 어셈블리 이름이 실제 접촉점을 가리키고 중복·겹침 없이 표시되는지
  - [x] 실선과 점선의 3D 위치가 맞고 PDF에서도 LONG_DASHED로 출력되는지
- **영향 파일**: `A2Z/Form1.Clash.cs`, `A2Z/Form1.BOM.cs`, `A2Z/Form1.Stru.cs`, `A2Z/Form1.DrawingSheets.cs`, 관련 흐름·코드 레퍼런스 문서

### T-005 — 치수 배치를 Osnap 외곽 방향으로
- **생성일**: 2026-04-15
- **착수일**: 2026-05-12
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #8 종료·Excel No.49 완료
- **관련**: FB-002
- **사용자 사양 (2026-05-12)**: 모델 전체 뷰 중앙 기준 4분면 — 중앙에서 가장 먼 Osnap이 있는 방향으로 치수. 상/하·좌/우 각각 max·min 거리 비교로 외곽 판정
- **구현 핵심**: 헬퍼 `ComputePositiveOffsetByOsnapExtreme(values, modelCenter)` 신설. `omax - center` vs `center - omin` 부호 있는 거리 비교 → 큰 쪽이 positive. 기존 `avg >= center` 5곳 전부 교체. 한쪽 쏠림(omin/omax 모두 center 한쪽)도 부호 자동 처리
- **세부**:
  - [x] 헬퍼 `ComputePositiveOffsetByOsnapExtreme` 신설 — Form1.Dimensions.cs GetAxisValue 옆
  - [x] 5곳 적용 — Form1.Dimensions.cs:499(메인, 치수추출+2D 출력 공용) / Form1.MfgDrawing.cs:335(가공도 메인) / :1057(가공도 보조) / :1192(MULTI) / :1707(EA newDims 비길이축, longestAxis 오버라이드 유지)
  - [x] 빌드 통과 후 사용자 사내 PC에서 실기 — 부재가 모델 중앙 한쪽에 치우친 케이스에서 치수가 *그 반대쪽*(외곽)으로 빠지는지 확인
  - [x] docs/기능/치수/메인 치수 추출.md 갱신 (외곽 판정 알고리즘 섹션)
- **영향 파일**:
  - `A2Z/Form1.Dimensions.cs` (헬퍼 추가 + L499 패턴 교체)
  - `A2Z/Form1.MfgDrawing.cs` (4곳 패턴 교체)

### T-039 — 치수 생성 타이밍 재설계 + offset 고정 (2D 공간 기준)
- **생성일**: 2026-04-24
- **착수일**: 2026-05-12
- **상태**: DONE (2026-07-24)
- **완료 판정**: GitHub issue #8 종료·Excel No.50 완료
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
  - [x] **빌드 통과 후 사용자 사내 PC 실기 — 큰 치수 시트(>1000) 보조선 10/20mm, 작은 시트(≤1000) 20/40mm 도달 확인. DiagLog `T-038+039 v2 maxDist=N` 값 비교**
- **잔여 (2차 — 가공도 적용)** → 별도 계획서로 분리 진행: `docs/리팩토링/가공도-보조선-제작도통일.md` v2 (2026-06-03, Codex 1차 반영):
  - [x] 공용 헬퍼 `ComputeCanvasAbsoluteOffsets` 추출 + 제작도 교체 (동작 보존, `1aba8c7`)
  - [x] 가공도 `BuildMfgSceneCore(availW, availH)` + 캔버스 절대 5/10mm 분기 (`EstimateFitScaleForViewArea` fitFactor=1.0 추정). 빌드 통과
  - [x] **사내 검증 — 가공도 보조선 부재 크기 무관 일정 + 모델 정합. 회전(Z90) 부재 추정 오차 확인. 부족 시 실측 newScale 2차**
  - [x] EA 두 뷰·MULTI·`:1693`(FitObjectToGridCellAspect) 경로는 범위 외 (별도)
- **잔여 (3차 — 정확도 향상)**:
  - [x] 사전 추정 vs 실제 RescaleObject scale 차이 측정 → 오차 분석
  - [x] 큰 경우 2단계 렌더 (모델 먼저 → 실제 scale → 치수) 재설계
- **영향 파일**: A2Z/Form1.Dimensions.cs (시그니처+변수), A2Z/Form1.DrawingSheets.cs (헬퍼+호출)
- **선행**: T-038 (셀 크기 기반 모델 스케일)과 결합으로 진행 중

### T-090 — 문서 링크 경로 공백 일괄 인코딩
- **완료일**: 2026-07-24
- **커밋**: `05fc359`
- **관련**: 사용자 직접 지시, GitHub issue #52
- **결과**: `docs/**/*.md`의 로컬 마크다운 링크 경로에서 원문 공백 236건을 `%20`으로 일괄 인코딩했다. 기능 문서 135건, 사용자 매뉴얼 38건, 코드 참조 34건, tracking 19건, 파이프라인 10건이며 현재 저장소 기준 97개 파일이 변경됐다. fenced code block 안의 예시 3건과 외부 URL·앵커 전용 링크·기존 `%20`은 보존했고, 변경 대상 링크의 실제 파일 부재는 0건이다.

### T-088 — STRU 검색 버튼 전용 실행·치수 추출 분리
- **완료일**: 2026-07-24
- **커밋**: `a73dd16`
- **관련**: 사용자 직접 지시, GitHub issue #48
- **결과**: STRU 검색 버튼 문구를 `검색`으로 변경하고 Enter 자동 실행을 제거했다. 검색은 대상 STRU 격리·목록 선택·카메라 fit까지만 수행하며 기존 치수 추출 자동 호출을 제거했다. 같은 STRU를 다시 검색해도 직접 fit을 실행하며, 자동완성·완전일치 우선·부분일치·오류 안내는 유지한다. 검색창 위치 변경은 별도 UI 작업으로 남겨 기존 하단 배치를 유지한다.

### T-083 — ISO 부재번호 풍선 View 영역 자동 정렬
- **완료일**: 2026-07-23
- **커밋**: `2664b8e`, `9f13789`, `70a6da3`, `d139ef8`, `2d6726f`, `e873ff8`, `e79d129`
- **관련**: 사용자 직접 지시, GitHub issue #4 (완료), Softhills SDK 1.0.26.723 배포 예제
- **결과**: 부재번호 풍선과 제작도·설치도 연결 이름을 실제 2D 객체 외곽 + 캔버스 20mm, SDK offset 0 영역으로 함께 자동 정렬. 연결 이름의 실제 접합점 Target은 유지한다. 개별 연결 이름의 상단·하단 방향 강제는 공개 API에서 직접 지원되지 않아 GitHub issue #38(추후 요청)로 분리하고 기존 이슈를 닫음.

### T-082 — 모든 도면 PAINT CODE에 STRU PNT UDA 공통 표시
- **완료일**: 2026-07-23
- **커밋**: `1da8af7`, `8b46270`, `831b98a` (완료 상태 기록 `c92d851`)
- **관련**: 사용자 직접 지시, GitHub issue #28
- **결과**: 기준부재의 부모 방향에서 이름에 PNT가 포함된 STRU UDA 값을 한 번 확정해 제작도·조립도·설치도·가공도 모든 페이지의 PAINT CODE 칸에 동일하게 적용. 사내 실제 출력에서 적용을 사용자 확인해 이슈를 완료 처리함.

### T-081 — BOM ITEM 및 빈 후속 데이터 표시
- **완료일**: 2026-07-23
- **커밋**: `23271c6`, `3ca2dd4` (완료 상태 기록 `d033ddd`)
- **관련**: 사용자 직접 지시, GitHub issue #24
- **결과**: SPREF 키 없음·null·빈 문자열·공백은 ITEM `unset`으로 통일하고, `unset` 행의 뒤쪽 열 전체를 `-`로 표시. 정상 ITEM 행도 MATERIAL·SIZE·Q'TY·T/W·MA·FA 중 비어 있는 셀만 `-`로 표시하며 정상 값과 Support&Seat 합계 행은 유지. 제작도·조립도·설치도·가공도 공통 경로와 실제 출력에서 정상 동작을 사용자 확인해 이슈를 완료 처리함.

### T-080 — 2D 출력 미선택 시 전체 제작도 기본 출력
- **완료일**: 2026-07-22
- **커밋**: `7748bfd` (완료 상태 기록 `3a32c71`)
- **관련**: 사용자 직접 지시, GitHub issue #23
- **결과**: 시트 미선택 시 목록의 전체 제작도를 자동 선택해 출력하고, 목록에 전체 제작도가 없으면 BOM 전체 임시 제작도를 생성하도록 수정. 기존 선택 시트 출력과 모델·BOM 방어를 유지했으며 Debug 빌드 오류 0건 및 사용자 실기 해결 확인을 완료함.

### T-076 — 간섭검사 SDK 진행창 숨기기
- **완료일**: 2026-07-22
- **커밋**: `ffb8eb0`, `2e61e46` (완료 상태 기록 `592e40c`)
- **관련**: 사용자 직접 지시, GitHub issue #17
- **결과**: 전체 검사+progressForm 옵션이 없는 SDK 제약에 맞춰 ClashTest ID를 `PerformInterferenceCheck(id, false)`로 직렬 실행. 완료 이벤트 반환 후 Busy 해제와 후속 시작 성공을 비동기로 재시도하고, 마지막 완료 때만 결과·치수·시트를 생성하도록 수정. 사내 모델에서 SDK 진행창 미노출과 후속 검사·최종 생성 정상 동작을 사용자 확인함.


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
- **요약**: `docs/features/` → `docs/기능/` + 8개 카테고리 폴더 한글화(부재속성/BOM/간섭검사/치수/도면시트/2D도면/글로벌뷰/가공도) + 60개 파일 한글화. `docs/technical-notes/` → `docs/기술 노트/` + 4개 파일 한글화(치수 보조선 사양·치수 텍스트 위치·Osnap 기준·Sheet1 명명 기준). [docs/README.md](../../README.md) 이모지 제거 + "기준·사양" 진입점 섹션 신규 + 카테고리 표 한글화. [docs/_glossary.md](../../_glossary.md) 보조선/Osnap/Chain Dimension/Drawing Sheet 항목에 본진 링크 4개 추가. CLAUDE.md / .claude/commands/{commit,checkpoint}.md / .claude/hooks/docs-sync-reminder.sh 경로 참조 4곳 갱신. PowerShell 일괄 치환 3패스 + md-link-checker 3회 검증(잔존 0건). **`code-reference/` 영어 유지** (코드 파일 1:1 매핑 보존). 총 131개 파일 변경, 코드 변경 없음

### T-058 — 치수 Text 보조선 초과 시 우측 자동 배치
- **완료일**: 2026-05-06 (커밋 `pending`)
- **관련**: — (회사 doc 개발 요청 — 상 5)
- **요약**: sdk-verifier 검증 결과 `MeasureStyle.AlignDistanceTextPosition` enum (xml:9298, 0:아래/1:위/**2:바깥쪽**)로 글로벌 1줄 처리 가능. 기존 4곳 `=0` 설정 + EA 가공도 1곳 추가 = **5곳 동시 변경**: [Form1.Dimensions.cs:51](../../../A2Z/Form1.Dimensions.cs) (선택 치수 표시), [Form1.Dimensions.cs:448](../../../A2Z/Form1.Dimensions.cs) (`ShowAllDimensions` — 글로벌/시트/2D 출력 4경로 본진), [Form1.MfgDrawing.cs:325, 1050, 1703](../../../A2Z/Form1.MfgDrawing.cs) (가공도 메인/sub/EA). 좁은 치수에서 텍스트가 보조선 사이를 침범하는 문제 회피. SDK가 치수별 개별 위치 옵션을 제공하지 않아 글로벌 적용(모든 치수 항상 바깥쪽), 회사 원문 "초과할 때만"과는 차이 있으나 핵심 의도(침범 회피) 충족. T-039 선행 무관(스타일 옵션은 offset 기준 결정 전에도 적용 가능). 통합 사양: [docs/기술 노트/치수 텍스트 위치.md](../../기술%20노트/치수%20텍스트%20위치.md)

### T-042 — 도면시트 목록 "기준부재" 컬럼에 부재 이름 추가 표시
- **완료일**: 2026-05-04 (코드 커밋 `e09c945`, DONE 이동 커밋 `pending`)
- **관련**: — (회사 doc 긴급최우선 1, T-014 보강)
- **요약**: 일반 시트(`>=0`) + 가공도(-3) 기준부재 셀에 `"1"` → `"1 (BOM이름)"` 병기 (`Form1.DrawingSheets.cs` ListView 갱신 단계, `$"{itemNo} ({sheet.BaseMemberName})"`). 매핑 실패 시 `sheet.BaseMemberName` fallback. **Sheet 1(`-1`)은 사용자 결정으로 `"전체"` 그대로 유지**, 설치도(`-2`)도 `"설치도"` 유지 — 회사 원문 "Sheet1 : 전체Item(Item Node 이름)" 부분은 미적용. 회사 doc "긴급최우선 1" 처리 완료

### T-049 — 치수 추출 백엔드 로직 문서화 (사전 추출 vs 즉시 추출)
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급중 3)
- **요약**: [docs/기능/BOM/메인 치수 추출.md](../../기능/BOM/메인%20치수%20추출.md) Section 7.5 "치수 캐시 라이프사이클" 신설. `chainDimensionList`를 단일 진실 공급원으로 한 4경로(치수추출/글로벌 X/Y/Z/2D 출력/일반 시트/가공도) + 캐시 mermaid + 표 + 사용자 시각 단계별 흐름 + T-032 성능 최적화 연계. 회사 doc "치수추출 버튼 앞뒤 로직" 의문 답변. 코드 변경 없음, docs만

### T-050 — 3D View에서 X/Y/Z 축 표시 기능 추가
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급하 1)
- **요약**: sdk-verifier 결과 SDK 직접 지원 확인 — `vizcore3d.View.MarineAxis.Visible = true` 한 줄로 가능 (`MarineAxisManager`, XML L43019). [Form1.BOM.cs](../../../A2Z/Form1.BOM.cs) `Vizcore3d_OnInitializedVIZCore3D` 단계 3.5에 추가. 결과: 3D 뷰 좌측하단에 ISO X/Y/Z triad 표시. 회사 doc "도면 외 3D View 축 미확인" 문제 해소. 추가 미세 조정(Length/Position/SetText) 필요 시 같은 위치에 보강 가능

### T-052 — Sheet1 포함부재 표기 "전체" → "1, 2, 3, ..." 개별 item 번호
- **완료일**: 2026-05-02 (커밋 `79876e2`)
- **관련**: — (회사 doc 긴급하 3, T-014 보강)
- **요약**: [Form1.DrawingSheets.cs](../../../A2Z/Form1.DrawingSheets.cs) ListView 단계의 `BaseMemberIndex == -1` 분기 제거 → 일반 시트(`>=0`)와 동일 로직(`bomIndexToItemNo` 매핑 + 정렬 + `string.Join`)으로 통합. 결과: Sheet 1의 포함부재 셀이 "전체" → "1, 2, 3, ..., N" 명시. 설치도(`-2`)도 같은 분기로 들어가지만 이전부터 동일 처리. BOM 14건 초과 시 ListView 컬럼 폭은 사용자 실기 후 조정

### T-046 — 가공도 치수 보조선 이중쇄선 → 가는 실선 (확장: 모든 보조선 + gap)
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc 긴급상 10 + 사용자 확장 — 모든 경로 + 모델 표면 gap)
- **요약**: 4경로(가공도 메인/EA + 일반 시트 2D 출력 + 글로벌 X/Y/Z + 치수추출)의 보조선을 `DrawDimension` 단일 지점에서 일괄 처리. (1) `Form1.MfgDrawing.cs:1542, 1900` LineType `DASHED_DOUBLEDOTTED` → `SOLID` 통일 + 토글 패턴 제거. (2) `OffsetTowardLineEnd` 헬퍼 + `ExtensionLineGap = 10.0f` 상수 신설 (Form1.Dimensions.cs) — 보조선 시작점이 모델 표면에서 10mm 떨어져 시작 (사용자 실기 후 1mm → 10mm 상향). 통합 사양: [docs/기술 노트/치수 보조선 사양.md](../../기술%20노트/치수%20보조선%20사양.md)

### T-053 — 중복 Sheet 삭제 후 Sheet 번호 자동 재채번 (v2 확장 포함)
- **완료일**: 2026-05-02 (v1 커밋 `8081688`), 2026-05-04 (v2 커밋 `pending`)
- **관련**: — (회사 doc 긴급하 4)
- **v1 요약**: `GenerateDrawingSheets` 단계 9(Sheet 1 동일 구성 제거) 직후에 `for (int i; i < drawingSheetList.Count; i++) drawingSheetList[i].SheetNumber = i + 1` 일괄 재채번. 일반 시트 빠진 자리만큼 후속 시트(설치도·가공도)도 자동 정합
- **v2 확장 (2026-05-04)**: 자동 제거 범위를 "Sheet 1 동일 구성" 한정 → **"모든 일반 시트 쌍에서 부재 구성 동일 시 첫 등장만 살림"** 으로 확장 (사용자 결정: *"포함부재가 같으면 기준부재가 달라도 같은 형상이다"*). `MemberIndices.OrderBy` 정렬 키 + `HashSet<string>`로 첫 등장 추적. Sheet 1 / 설치도 / 가공도는 의미가 다른 시트라 검사 제외하고 보존. Sheet 1과 동일 구성인 일반 시트는 별도 RemoveAll로 추가 제거. [시트 자동 생성.md](../../기능/도면시트/시트%20자동%20생성.md) 단계 9·9.3 + 분기 C + 변경 이력 갱신

### T-055 — 검증 보고서: Osnap 기준점 코드 동작 확인
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc "완료 3" 의문 답변용)
- **요약**: 4경로 보조선 데이터 흐름 + 부재별/전체 풀 동시 적재 + X/Y/Z 뷰별 primary/secondary 매핑 + 4단 dedup(부재 → 전역 dimAxis → MergeCoordinates 0.5mm → keyToDim) 코드 트레이스 완료. 결론 **부분 일치** — 핵심 의도(코너 우선 + 중복 제거)는 모두 구현되었으나, 부재 단위에서 4코너가 아니라 1점만 남기는 점이 명세 문구와 다름. 산출물: [docs/기술 노트/Osnap 기준.md](../../기술%20노트/Osnap%20기준.md)

### T-056 — 검증 보고서: Sheet1 부재 이름 부여 기준 (Z-MAX 정렬)
- **완료일**: 2026-05-02 (커밋 `8081688`)
- **관련**: — (회사 doc "완료 5" + "수정 후 확인 필요 2" 의문 답변용)
- **요약**: 현재 코드는 `BBox.MaxZ` (Form1.BOM.cs:735) 기준 정렬, 회사 명세는 `max(Osnap.Z)` 기준 — 데이터 출처 차이. 직립 H빔·평판 등 일반 철골 형상에선 두 값이 동등하므로 정렬 결과 같음. 경사 부재·곡면 Body에서 수 mm 차이 발생 가능 (정렬 1~2칸 흔들림). 결론 **부분 일치** — 회사 답변에 따라 후속 작업(Form1.BOM.cs:688 osnapList 활용) 신설 가능. 산출물: [docs/기술 노트/Sheet1 명명 기준.md](../../기술%20노트/Sheet1%20명명%20기준.md)
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
  - 이벤트 등록 위치: [Form1.cs:166](../../../A2Z/Form1.cs:166)
  - 새 핸들러: [Form1.DrawingSheets.cs `LvDrawingBOMInfo_SelectedIndexChanged`](../../../A2Z/Form1.DrawingSheets.cs)
  - 신규 문서: [BOM 정보 선택.md (SHT-010)](../../기능/도면시트/BOM%20정보%20선택.md), `_인덱스.md` 등록 추가
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
  - 구현: [Form1.DrawingSheets.cs:215~281](../../../A2Z/Form1.DrawingSheets.cs:215) `bomIndexToItemNo` Dictionary + ListView 갱신 블록
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
  - **수정**: [Form1.DrawingSheets.cs:105~142](../../../A2Z/Form1.DrawingSheets.cs:105) `appearedAsIncluded` HashSet 선언·검사·추가 3곳 모두 제거. 주석도 T-015 결정 배경으로 교체
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
