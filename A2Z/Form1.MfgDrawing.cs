using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        #region 가공도 출력 - 단일 부재 치수 표시

        // P1 (2026-05-23): btnMfgDrawing_Click 폐기 — 작업/데이터 탭 작은 "가공도 출력" 버튼.
        //   사용자 결정: 가공도는 도면정보 탭의 큰 "가공도 출력" 버튼으로 통합.
        //   ExecuteMfgDrawing 함수 본체는 LvDrawingSheet 시트 선택 미리보기로 유지.

        /// <summary>
        /// 가공도 모드 해제 - 전체 부재 다시 보이기
        /// BOM 더블클릭, 축 버튼, 전체보기 등에서 호출 가능
        /// </summary>
        private void RestoreAllPartsVisibility()
        {
            // 모든 부재 표시 (숨겨진 부재 복원)
            List<int> allIndices = new List<int>();
            foreach (BOMData b in bomList)
                allIndices.Add(b.Index);

            if (allIndices.Count > 0)
                vizcore3d.Object3D.Show(allIndices, true);
        }

        // ═══════════════════════════════════════════════════════════════════
        // P2-helpers (2026-05-23): 가공도 수동 통합 함수용 헬퍼 일괄.
        // 계획서: docs/리팩토링/가공도-수동우선-재배선.md v7
        // 사용자 사양: BOM 표·도면정보 = 제작도 방식 그대로 (CollectBOMInfo + lvDrawingBOMInfo 재사용)
        // Codex 1~6차 검토 누적 반영 (B/N/M cleanup, snapshot, dict 검증, MakeUniquePdfPath 등)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 가공도 페이지 1개 데이터 — v7: 페이지당 5부재 (View_1~View_5).
        /// </summary>
        private sealed class MfgPage
        {
            public int PageIdx;                                     // 1-based
            public List<DrawingSheetData> Rows = new List<DrawingSheetData>();
        }

        /// <summary>
        /// 가공도 출력 결과 객체 (v7, Codex 6차 권고).
        /// 호출부가 받아 단일 MessageBox 또는 자동 일괄 요약 생성.
        /// </summary>
        private sealed class MfgDrawingResult
        {
            public int SuccessPdfs;               // 저장된 PDF 파일 수 (#119 묶음 저장 후로는 0 또는 1)
            public int SuccessPages;              // PDF에 담긴 도면 페이지 수 (#119)
            public string SavedPdfPath;           // 저장된 묶음 PDF 경로 (#119, 미저장이면 null)
            public int InsufficientBomPdfs;       // BOM 부족 상태에서 저장된 페이지 수 (성공 페이지 중 일부)
            public bool TemplateMissing;          // 가공도 엑셀 템플릿 누락 → PDF 0개
            public string TemplatePath;           // 실제 탐색한 템플릿 경로 (누락 안내 문구가 재계산하지 않도록)
            public int BomRows;                   // 실제 snapshot BOM 행 수
            public int ExpectedBomRows;           // 예상 BOM 행 수 = Min(allMfgBomIndices.Count, 15)
            public bool Canceled;                  // 안전 체크포인트에서 사용자 취소
            public string CancellationCheckpoint; // 실제 중단 위치
            public List<string> Warnings = new List<string>();   // 사용자에게 보일 경고 텍스트

            public bool HasIssues => Warnings.Count > 0 || InsufficientBomPdfs > 0 || TemplateMissing;
        }

        /// <summary>
        /// 취소 판정 함수·진행 문구·체크포인트명을 받아 → UI 메시지를 처리하고 취소 요청이면 OperationCanceledException을 던진다.
        /// 가공도 PDF 행 렌더링·페이지 루프의 SDK 호출 사이 경계마다 호출. 취소 판정 함수가 null이면 아무것도 안 한다.
        /// 취소 가능 구간 중이면 진행창 문구도 갱신하고, 아니면 DoEvents만 돌린다.
        /// </summary>
        private void CheckMfgCancellation(
            Func<bool> shouldCancel,
            string progressMessage,
            string checkpoint)
        {
            if (shouldCancel == null)
                return;

            if (_cancelableOperationInProgress)
                ProcessCancelableUiCheckpoint(progressMessage, checkpoint);
            else
                Application.DoEvents();

            if (shouldCancel())
                throw new OperationCanceledException(checkpoint);
        }

        /// <summary>
        /// 가공도 시트 목록을 페이지당 N개로 분할 (v7: rowsPerPage = 5).
        /// </summary>
        private List<MfgPage> SplitMfgIntoPages(List<DrawingSheetData> mfgSheets, int rowsPerPage = 5)
        {
            var pages = new List<MfgPage>();
            if (mfgSheets == null || mfgSheets.Count == 0) return pages;

            int total = mfgSheets.Count;
            int pageCount = (total + rowsPerPage - 1) / rowsPerPage;
            for (int p = 0; p < pageCount; p++)
            {
                var page = new MfgPage { PageIdx = p + 1 };
                int start = p * rowsPerPage;
                int end = Math.Min(start + rowsPerPage, total);
                for (int i = start; i < end; i++)
                    page.Rows.Add(mfgSheets[i]);
                pages.Add(page);
            }
            return pages;
        }

        /// <summary>
        /// lvDrawingBOMInfo의 BOM 행 데이터를 메모리로 1회 복사 (v7 Codex 4차 권고).
        /// 호출자: GenerateMfgDrawingManual 진입부 (CollectBOMInfo 직후 1회).
        /// 목적: 페이지 루프에서 live ListView 의존 끊기 → UI race 차단.
        /// </summary>
        /// <returns>각 행의 8컬럼 문자열 배열 리스트. Row 0(요약행)을 첫 원소로 포함한다 (#67).</returns>
        private List<string[]> SnapshotBomRows()
        {
            var rows = new List<string[]>();
            if (lvDrawingBOMInfo.Items.Count == 0) return rows;

            for (int i = 0; i < lvDrawingBOMInfo.Items.Count; i++)
            {
                ListViewItem item = lvDrawingBOMInfo.Items[i];
                rows.Add(new string[]
                {
                    item.Text,                      // [0] NO
                    SafeSubItem(item, 1),           // [1] ITEM
                    SafeSubItem(item, 2),           // [2] MATERIAL
                    SafeSubItem(item, 3),           // [3] SIZE
                    SafeSubItem(item, 4),           // [4] Q'TY
                    SafeSubItem(item, 5),           // [5] T/W
                    SafeSubItem(item, 6),           // [6] MA
                    SafeSubItem(item, 7),           // [7] FA
                });
            }
            return rows;
        }

        /// <summary>
        /// 가공도 페이지 1개의 ImportExcelWithData용 Dictionary 구성 (v7).
        /// 사용자 사양: BOM 표·도면정보 = 제작도 방식 그대로 (제작도 코드 패턴 차용).
        /// BOM 데이터는 호출자가 SnapshotBomRows로 1회 복사한 bomSnapshot 사용.
        /// </summary>
        private Dictionary<int, string> BuildMfgPageData(
            MfgPage page, int totalPages, string struName, string paintCode, string paintCode2,
            string struTag, string dpNo, List<string[]> bomSnapshot)
        {
            var data = new Dictionary<int, string>();

            // [2026-07-27] 빈 슬롯 선초기화(1~245 → ""/" ") 제거 — 벤더(소프트힐스) 안내 반영.
            //   ImportExcelWithData는 data에 값이 있으면(""·" " 포함) 치환하고, 값이 없을 때만 {Input}으로 남긴다.
            //   그리고 {Input}으로 남은 셀만 JSON에 TextBox로 생성되는데, RemoveEmptyTemplateBorders는
            //   그 TextBox가 있어야 괘선을 지운다. 선초기화가 전 슬롯을 채우면 {Input}이 하나도 안 남아
            //   괘선 제거가 통째로 무동작이었다 (SDK 1.0.26.727 전달 메일이 이 파일을 지목 — 전문 요약은
            //   issue #60, 원본 .eml은 gitignore로 로컬 전용).
            //   → 미기재 슬롯은 data에 키를 넣지 않고 {Input}으로 남긴다. 제작도(Form1.DrawingSheets.cs)도 동일 적용.
            //   ⚠ 부작용 2건 실기 확인 필요: ① 07-21 확정한 "PAINT/DP/TAG(165~169)·REV 첫 기재행(194~199)
            //   괘선 보존" 정책이 깨져 같이 지워질 수 있음 ② TextBox가 PDF에 {Input} 글자로 노출될 수 있음
            //   (선초기화의 원래 목적이 그 노출 방지였음). 재발 시 보존 슬롯만 " "로 되돌리는 게 1차 대응.
            //   슬롯: BOM(4~163)·Note(164)·PAINT/DP/TAG(165~169)·Rev 표(170~199)·BOM 21~25행(200~240)·부재명(241~245). View 1~5.

            // ── 도면정보 ──
            data[1] = "CEDAR FLNG";  // TODO: 프로젝트명 (T-043 tableInfo 결정 후)
            data[2] = "SN2688";       // TODO: 선박번호
            data[3] = totalPages > 1
                ? $"가공도 ({page.PageIdx}/{totalPages})"
                : "가공도";
            // 표제부 PAINT CODE(166 · 246) · DP No(168) · TAG NO(169) — 4종 도면이 같은 슬롯·같은 값을 쓴다.
            //   호출자가 부재 노드에서 한 번 조회한 값을 전 페이지가 재사용한다.
            //   값이 없어도 공백 1칸을 넣어 괘선을 남긴다 (#33 · #60) — 제작도 경로와 같은 규칙이다.
            //   165·167은 PAINT CODE 2와 DP No. 사이 예비 칸이라 늘 공백이다.
            data[165] = " ";
            data[166] = KeepBorder(paintCode);
            data[167] = " ";
            data[168] = KeepBorder(dpNo);
            data[169] = KeepBorder(struTag);
            data[246] = KeepBorder(paintCode2);

            // ── REV 표 첫 기재행 (Input_194~199) ──
            //   제작도와 같은 공통 헬퍼(Form1.ExcelTemplate.cs) — REV.=0 / 출력일 / 나머지 공백 (#64 Phase 1).
            //   다중 페이지도 같은 값이다. 미사용 이력행(170~193)은 키를 안 넣어 괘선이 지워진다.
            FillRevisionTable(data, BuildCurrentRevisionHistory());

            // ── 부재명 5칸 (Input_241~Input_245, 각 View 왼쪽 라벨) ──
            //   [2026-07-23] 195~199는 제작도처럼 REV 표 첫 기재행 전용으로 되돌리고, 부재명은 241~245로 분리
            //   (200+ 크래시 해소 후 가능). 번호 충돌·REV 표 부재명 누수 제거.
            for (int i = 0; i < page.Rows.Count && i < 5; i++)
            {
                var sheet = page.Rows[i];
                if (sheet.MemberIndices.Count == 0) continue;
                var bom = bomList.FirstOrDefault(b => b.Index == sheet.MemberIndices[0]);
                if (bom == null) continue;
                data[241 + i] = bom.Name ?? "";
            }

            // ── 우측 BOM 표 8컬럼 × 25행 (snapshot 사용) ──
            //   제작도(제작도_도면)와 완전히 동일한 슬롯 체계 — 1~20행=열별 20연속(4~163),
            //   21~25행=신규 태그(201~240, 열별 5연속). 2026-07-23 Input 200+ 확장(크래시 해소 후).
            //   snapshot[0]은 요약행이라 1행=요약행, 2~25행=데이터행 24개다 (#67). BOM 표는 전 페이지 동일 내용.
            int bomMapped = 0;
            if (bomSnapshot != null)
            {
                int n = Math.Min(bomSnapshot.Count, 25);
                for (int i = 0; i < n; i++)
                {
                    string[] row = bomSnapshot[i];
                    int cNo, cItem, cMat, cSize, cQty, cTw, cMa, cFa;
                    if (i < 20)
                    {
                        cNo = 4 + i;   cItem = 24 + i;  cMat = 44 + i;  cSize = 64 + i;
                        cQty = 84 + i; cTw = 104 + i;   cMa = 124 + i;  cFa = 144 + i;
                    }
                    else
                    {
                        int j = i - 20;   // 0~4 (BOM 21~25행)
                        cNo = 201 + j;  cItem = 206 + j; cMat = 211 + j; cSize = 216 + j;
                        cQty = 221 + j; cTw = 226 + j;   cMa = 231 + j;  cFa = 236 + j;
                    }
                    data[cNo]   = row[0];   // NO
                    data[cItem] = row[1];   // ITEM
                    data[cMat]  = row[2];   // MATERIAL
                    data[cSize] = row[3];   // SIZE
                    data[cQty]  = row[4];   // Q'TY
                    data[cTw]   = row[5];   // T/W
                    data[cMa]   = row[6];   // MA
                    data[cFa]   = row[7];   // FA
                }
                bomMapped = n;
                if (bomSnapshot.Count > 25)
                    DiagLog($"[BuildMfgPageData] BOM {bomSnapshot.Count}행 중 25행만 표시 (템플릿 한도)");
            }
            DiagLog($"[BuildMfgPageData] p{page.PageIdx}/{totalPages} BOM 매핑 {bomMapped}행 (snapshot)");

            return data;
        }

        /// <summary>
        /// 엑셀 템플릿에서 View_1~View_5 영역을 1회 캐시 (v7 Codex 3차 B1).
        /// SDK 정식: TemplateViewArea.Index = int (1..5).
        /// local dict 검증 통과 후에만 cache 대입 — invalid cache 잔존 차단.
        /// </summary>
        private void EnsureViewAreasCache(
            ref Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea> cache,
            string xlsxPath)
        {
            if (cache != null) return;

            var list = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
            if (list == null || list.Count == 0)
                throw new InvalidOperationException($"가공도 템플릿 View 영역 없음: {xlsxPath}");

            // 중복 인덱스 방어 — 템플릿 편집 중 같은 View_n 태그가 두 곳에 남으면 ToDictionary가 예외로 죽는다.
            //   (실사례 2026-07-20: 제작도 복제 시 북쪽 화살표 View_5가 부재 5번 칸과 중복) 첫 번째만 쓰고 경고.
            var dict = new Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea>();
            foreach (var a in list)
            {
                if (dict.ContainsKey(a.Index))
                {
                    DiagLog($"[EnsureViewAreasCache] WARN View_{a.Index} 태그 중복 — 첫 번째 위치 사용, 템플릿 확인 필요: {Path.GetFileName(xlsxPath)}");
                    continue;
                }
                dict[a.Index] = a;
            }
            for (int i = 1; i <= 5; i++)
                if (!dict.ContainsKey(i))
                    throw new InvalidOperationException($"가공도 템플릿 View_{i} 누락: {xlsxPath}");

            cache = dict;
            DiagLog($"[EnsureViewAreasCache] View_1~View_5 캐시 완료 (from {Path.GetFileName(xlsxPath)})");
        }

        /// <summary>
        /// 가공도 페이지 진입 시 캔버스 초기화 (v7).
        /// </summary>
        private void ResetCanvasForMfgPage()
        {
            // 묶음 출력 중이면 이전 페이지를 지우지 않고 새 캔버스를 덧붙인다 (#119).
            PrepareDrawingCanvas(297, 210);
        }

        /// <summary>
        /// 자동·수동 공통 PDF 저장 폴더. T-064 정책: Application.StartupPath/Drawings.
        /// </summary>
        private string GetDefaultDrawingSaveDir()
        {
            string dir = Path.Combine(Application.StartupPath, "Drawings");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// 현재 3D 장면을 은선 2D 캡처로 만들어 → viewArea에 실측 배율로 맞추고 치수·풍선·보조선을 2D로 옮긴다. 실패하면 false.
        /// PDF 어댑터가 1차 뷰·EA 2차 뷰마다 호출. DASH_LINE 렌더모드로 캡처하고 2D 개체 ID를 objId로 준다.
        /// 캡처 뒤 실패해도 objId는 남는다. 주석 몫을 먼저 예약하되 모델 높이 35% 보장, MirrorVertical이면 배치 전 상하 미러.
        /// </summary>
        private bool CaptureMfgSceneToViewArea(
            int rowIdx,
            BOMData bom,
            MfgViewPose pose,
            float areaX,
            float areaY,
            float areaWidth,
            float areaHeight,
            string viewLabel,
            out int objId)
        {
            objId = -1;

            if (areaWidth <= 0 || areaHeight <= 0
                || float.IsNaN(areaWidth) || float.IsNaN(areaHeight)
                || float.IsInfinity(areaWidth) || float.IsInfinity(areaHeight))
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} {viewLabel} area 비정상 W={areaWidth} H={areaHeight}");
                return false;
            }

            // 모델선 두께 — 제작도(DrawingSheets.cs:1636)와 동일 API. Set2DViewCreateObjectItemLineWidth와
            //   별개(이건 2D 개체 생성 라인폭). 라이브 가공도 캡처가 이걸 누락해 모델선이 가늘게 나오던 문제 교정 (전수조사 2026-07-01).
            vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            // 은선 API + DASH_LINE 렌더모드 — 제작도가 완주로 검증한 조합과 100% 동일 (2026-07-20 격리 4단계).
            //   신 템플릿에서 '은선 없는 캡처'와 'HLR 모드 + 은선 캡처' 모두 AccessViolation (벤더 문의 후보 2건).
            //   ⚠ DASH_LINE이라 은선이 점선으로 보일 수 있음 — 안정화 우선, '단면만' 사양은 벤더 수정 후 복원 예정.
            DiagLog($"[Capture] row={rowIdx} bom={bom.Index} {viewLabel} Create2DViewObject(DASH) 직전");   // AccessViolation 위치 특정용
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
            if (objId < 0)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} 2D 캡처 실패");
                return false;
            }

            // 2차 뷰 상하 미러 — 배치(Rescale/MoveObjectTo) '전'에 적용해, 미러 피벗이 어디든
            //   이후 MoveObjectTo가 최종 위치를 보정하게 한다 (미러가 객체를 이동시키던 문제 대응). 2026-07-02
            //   치수·보조선은 3D 좌표 반전(BuildEaSecondaryScene)으로 이미 정합.
            if (pose.MirrorVertical)
            {
                try
                {
                    vizcore3d.Drawing2D.Object2D.SelectObjectBy2DView(objId, 1);
                    vizcore3d.Drawing2D.Object2D.SetSelected3DMirrorBy2DView(true);   // true = 상/하 반전
                    vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                    DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} 상하 미러 적용(배치 전) objId={objId}");
                }
                catch (Exception ex)
                {
                    DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN 미러 실패: {ex.Message}");
                }
            }

            float objW = 0f, objH = 0f;
            vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);
            if (objW <= 0 || objH <= 0 || float.IsNaN(objW) || float.IsNaN(objH))
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} objSize 비정상 W={objW} H={objH}");
                return false;
            }

            // 치수·풍선을 모델 배치 후 바깥에 덧붙이므로, 먼저 종이 절대 주석 영역을 예약한다.
            // 예약 없이 모델을 슬롯 전체 높이에 맞추면 풍선이 행 경계 밖으로 잘리거나 EA 반대 뷰를 침범한다.
            PromoteMfgSmallPendingDimensions(pose);
            float annotationBudget = GetMfgAnnotationBudgetCanvas(pose);
            float minModelHeight = areaHeight * MfgMinModelAreaHeightRatio;
            float fitHeight = Math.Max(minModelHeight, areaHeight - annotationBudget);
            fitHeight = Math.Min(areaHeight, fitHeight);
            float reservedHeight = areaHeight - fitHeight;
            float modelCenterY = areaY + areaHeight / 2f +
                (pose.PlaceNotesAbove ? -reservedHeight / 2f : reservedHeight / 2f);

            float fitRatio = Math.Min(areaWidth / objW, fitHeight / objH);
            float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
            float newScale = curScale * fitRatio;
            if (fitRatio <= 0 || curScale <= 0 || newScale <= 0
                || float.IsNaN(fitRatio) || float.IsNaN(curScale) || float.IsNaN(newScale)
                || float.IsInfinity(fitRatio) || float.IsInfinity(curScale) || float.IsInfinity(newScale))
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} scale 비정상 fit={fitRatio} cur={curScale} new={newScale}");
                return false;
            }

            vizcore3d.Drawing2D.Object2D.RescaleObject(objId, newScale);
            vizcore3d.Drawing2D.Object2D.MoveObjectTo(
                objId,
                areaX + areaWidth / 2f,
                modelCenterY);
            DiagLog($"[MfgAnnotationBudget] row={rowIdx} bom={bom.Index} view={viewLabel} " +
                    $"requested={annotationBudget:F1} reserved={reservedHeight:F1} fitHeight={fitHeight:F1}/{areaHeight:F1} " +
                    $"shared={pose.SharedAnnotationBudgetCanvas.HasValue} " +
                    $"side={(pose.PlaceNotesAbove ? "above" : "below")} " +
                    $"capped={(annotationBudget > reservedHeight + 0.01f)}");

            // 보조선·치수를 '확정된 실측 배율(newScale)'로 지금 그린다 — 캔버스 절대 오프셋이 부재·뷰 무관 일정.
            //   (코어는 pose.PendingDims로 목록만 수집) 설계 §4.4 v2-c, 2026-07-01
            try { DrawMfgDimsAtScale(pose, bom, newScale); }
            catch (Exception ex) { DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN DimDraw 실패: {ex.Message}"); }

            // 각 캡처는 자신의 Pending Note만 책임진다. 첫 번째 목록이 비어도 두 번째 뷰는 독립적으로 생성한다.
            // 모델·치수와 같은 확정 newScale을 사용해 풍선 지시선 거리와 행 간격을 종이 절대값으로 맞춘다.
            try
            {
                vizcore3d.Review.Note.Clear();
                AddMfgPendingNotesAtScale(rowIdx, bom, pose, newScale, viewLabel);
            }
            catch (Exception ex)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN BalloonDraw 실패: {ex.Message}");
            }

            try
            {
                if (pose.ShapeDrawingIds != null && pose.ShapeDrawingIds.Count > 0)
                {
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(
                        VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
                    vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(pose.ShapeDrawingIds);
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN ShapeDrawing 실패: {ex.Message}");
            }

            try
            {
                var noteIds = vizcore3d.Review.Note.Items.Select(n => n.ID).ToList();
                if (noteIds.Count > 0)
                {
                    // 가공도 형상 풍선만 6mm로 변환하고 즉시 공용 기본값 7mm로 복원한다.
                    // 이 값은 2D 종이 절대 설정이므로 newScale로 다시 나누지 않는다.
                    try
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(MfgCanvasBalloonTextHeight);
                        vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                    }
                    finally
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);
                    }
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN Note 실패: {ex.Message}");
            }

            try
            {
                var measures = vizcore3d.Review.Measure.Items;
                var measureIds = measures.Where(m => m.Visible).Select(m => m.ID).ToList();
                if (measureIds.Count > 0)
                {
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);   // 제작도와 동일 (0.5→0.3)
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(MfgCanvasMeasureTextHeight);   // 제작도(DrawingSheets.cs:1638)와 동일 — 라이브 가공도 누락분
                    // 치수 텍스트 위치는 제작도와 동일하게 SDK 자동 정렬에 위임 (수동 배치 없음) — 가로=치수선 위, 세로=왼쪽(회사 표준).
                    //   (옛 수동 배치 ApplyParallelTextShift·PushMfgDimTextOutside는 m.Position 역추정이라 중심이 어긋나 폐기 — 2026-07)
                    vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN Measure 실패: {ex.Message}");
            }

            DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} OK objId={objId} " +
                $"newScale={newScale:F3} objW={objW:F1} objH={objH:F1} {(objW >= objH ? "가로" : "세로")}");
            return true;
        }

        /// <summary>
        /// 부재가 현재 카메라에서 세로로 잡히면 화면축 90° 회전으로 가로로 만든다.
        /// 판정은 추측(BBox·축 대응) 대신 **임시 캡처의 실제 크기(objW/objH)** = ground truth.
        /// 회전은 화면축 회전(RotateCameraByScreenAxis) — 이 방식이 2D 캡처에 반영됨이
        /// 확인됨(up-vector 방식은 미반영이라 폐기, 2026-06-29).
        /// 반환: 적용한 회전각(0 또는 90). 호출자는 캡처 후 -각도로 원복해 누적을 막아야 한다.
        /// 프레이밍(Fly)을 먼저 하고, 세로면 회전 후 다시 Fly(검증된 baseline 순서: roll→Fly→capture).
        /// 이미 가로면 회전 없이 프레이밍만(정상 뷰 무영향).
        /// </summary>
        private float ProbeAndRollLandscape(int bomIndex, float zoomRatio)
        {
            // [격리 5단계 2026-07-21] 캡처 직전 FlyToObject3d 제거 — 제작도(정상)는 MoveCamera만 쓰고 Fly가 없음.
            //   신 템플릿에서 'Fly 직후 캡처'가 AccessViolation 용의 (카메라 이동/애니메이션 중 캡처 진입 의심).
            //   W/H 판정은 직교 투영 비율이라 카메라 fit(줌)과 무관 → Fly 없이도 판정 동일.
            //   (카메라 방향은 BuildMfgSceneCore가 이미 설정, 배치·크기는 캡처 후 Rescale/MoveObjectTo가 처리)

            // 실제 투영 방향을 임시 캡처로 측정 (ground truth — 축 규약 추측 제거)
            int probe = -1;
            DiagLog($"[Orient] bom={bomIndex} probe 캡처 직전");   // AccessViolation 위치 특정용 (catch 불가 예외)
            try
            {
                // 은선 API + DASH_LINE 렌더모드 — 제작도가 완주로 검증한 조합과 100% 동일 (2026-07-20 격리 4단계).
                //   '은선 없는 캡처(WithModelAtCanvasOrigin)'와 'HLR 모드 + 은선 캡처' 모두 신 템플릿에서 AccessViolation.
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                DiagLog($"[Orient] bom={bomIndex} RenderMode OK — Create 직전");   // 사망 호출 정밀 특정용
                probe = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
            }
            catch { }

            if (probe < 0)
            {
                DiagLog($"[Orient] bom={bomIndex} probe 실패 → 회전 생략");
                return 0f;
            }

            float pw = 0f, ph = 0f;
            vizcore3d.Drawing2D.Object2D.GetObjectSize(probe, ref pw, ref ph);
            try { vizcore3d.Drawing2D.Object2D.DeleteObjectBy2DView(probe); } catch { }

            if (ph > pw + 0.001f)
            {
                // 세로 → 화면축 90° 회전으로 가로화 (roll 뒤 Fly는 격리 5단계에서 제거 — 프레이밍은 캡처와 무관)
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                DiagLog($"[Orient] bom={bomIndex} 가로전환(90°) probeW={pw:F2} probeH={ph:F2}");
                return 90f;
            }

            DiagLog($"[Orient] bom={bomIndex} 가로유지 probeW={pw:F2} probeH={ph:F2}");
            return 0f;
        }

        /// <summary>
        /// 현재 뷰의 Hole/SlotHole/EarthBoss 노트를 확정된 실측 배율로 도면 외곽에 생성한다.
        /// 치수선 최대 오프셋과 풍선 gap/행 간격을 모두 종이 절대값으로 공유해 부재·뷰별 편차를 없앤다.
        /// </summary>
        private void AddMfgPendingNotesAtScale(
            int rowIdx,
            BOMData bom,
            MfgViewPose pose,
            float newScale,
            string viewLabel)
        {
            if (bom == null || pose == null || pose.PendingNotes == null ||
                pose.PendingNotes.Count == 0)
                return;
            if (newScale <= 0f || float.IsNaN(newScale) || float.IsInfinity(newScale))
                return;

            var cameraAxes = vizcore3d.View.GetCameraAxis();
            if (cameraAxes == null || cameraAxes.Count < 3)
            {
                DiagLog($"[MfgBalloon] row={rowIdx} bom={bom.Index} view={viewLabel} WARN camera axis 없음");
                return;
            }

            var rawH = new VIZCore3D.NET.Data.Vector3D(
                cameraAxes[0].X, cameraAxes[0].Y, cameraAxes[0].Z);
            var rawV = new VIZCore3D.NET.Data.Vector3D(
                cameraAxes[1].X, cameraAxes[1].Y, cameraAxes[1].Z);
            var rawD = new VIZCore3D.NET.Data.Vector3D(
                cameraAxes[2].X, cameraAxes[2].Y, cameraAxes[2].Z);
            if (!TryNormalizeMfgVector(rawH, out var screenH) ||
                !TryNormalizeMfgVector(rawV, out var screenV) ||
                !TryNormalizeMfgVector(rawD, out var depth))
            {
                DiagLog($"[MfgBalloon] row={rowIdx} bom={bom.Index} view={viewLabel} WARN camera axis 비정상");
                return;
            }

            Func<VIZCore3D.NET.Data.Vertex3D, VIZCore3D.NET.Data.Vector3D, float> project =
                (point, axis) => point.X * axis.X + point.Y * axis.Y + point.Z * axis.Z;

            float modelMinH = float.MaxValue;
            float modelMaxH = float.MinValue;
            float modelMinV = float.MaxValue;
            float modelMaxV = float.MinValue;
            float[] xs = { bom.MinX, bom.MaxX };
            float[] ys = { bom.MinY, bom.MaxY };
            float[] zs = { bom.MinZ, bom.MaxZ };
            foreach (float x in xs)
            foreach (float y in ys)
            foreach (float z in zs)
            {
                var corner = new VIZCore3D.NET.Data.Vertex3D(x, y, z);
                float h = project(corner, screenH);
                float v = project(corner, screenV);
                modelMinH = Math.Min(modelMinH, h);
                modelMaxH = Math.Max(modelMaxH, h);
                modelMinV = Math.Min(modelMinV, v);
                modelMaxV = Math.Max(modelMaxV, v);
            }

            float gap = MfgCanvasBalloonGap / newScale;
            float rowSpacing = MfgCanvasBalloonRowSpacing / newScale;
            float dimEnvelope = Math.Max(0f, pose.DimensionEnvelopeOffset);
            float measureClearance = dimEnvelope > 0f
                ? (MfgCanvasMeasureTextHeight + MfgCanvasMeasureTextMargin) / newScale
                : 0f;
            bool placeAboveBeforeMirror = pose.MirrorVertical
                ? !pose.PlaceNotesAbove
                : pose.PlaceNotesAbove;
            float firstV = placeAboveBeforeMirror
                ? modelMaxV + dimEnvelope + measureClearance + gap
                : modelMinV - dimEnvelope - measureClearance - gap;
            float rowDirection = placeAboveBeforeMirror ? 1f : -1f;

            var ordered = pose.PendingNotes
                .Where(n => n != null && n.ArrowPosition != null)
                .OrderBy(n => project(n.ArrowPosition, screenH))
                .ThenBy(n => n.Text)
                .ToList();

            int added = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var pending = ordered[i];
                try
                {
                    float textH = project(pending.ArrowPosition, screenH);
                    float textV = firstV + rowDirection * rowSpacing * i;
                    float textD = project(pending.ArrowPosition, depth);
                    var textPosition = new VIZCore3D.NET.Data.Vertex3D(
                        screenH.X * textH + screenV.X * textV + depth.X * textD,
                        screenH.Y * textH + screenV.Y * textV + depth.Y * textD,
                        screenH.Z * textH + screenV.Z * textV + depth.Z * textD);

                    VIZCore3D.NET.Data.NoteStyle noteStyle = vizcore3d.Review.Note.GetStyle();
                    noteStyle.UseSymbol = false;
                    noteStyle.BackgroudTransparent = true;
                    noteStyle.FontBold = true;
                    noteStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                    noteStyle.FontColor = pending.Color;
                    noteStyle.LineColor = pending.Color;
                    noteStyle.LineWidth = 1;
                    noteStyle.ArrowColor = pending.Color;
                    noteStyle.ArrowWidth = 2;

                    vizcore3d.Review.Note.AddNoteSurface(
                        pending.Text, textPosition, pending.ArrowPosition, noteStyle);
                    added++;
                }
                catch (Exception ex)
                {
                    DiagLog($"[MfgBalloon] row={rowIdx} bom={bom.Index} view={viewLabel} " +
                            $"WARN text='{pending.Text}': {ex.Message}");
                }
            }

            DiagLog($"[MfgBalloon] row={rowIdx} bom={bom.Index} view={viewLabel} newScale={newScale:F4} " +
                    $"canvasGap={MfgCanvasBalloonGap:F1} canvasRow={MfgCanvasBalloonRowSpacing:F1} " +
                    $"modelGap={gap:F2} modelRow={rowSpacing:F2} dimEnvelope={dimEnvelope:F2} " +
                    $"measureClearance={measureClearance:F2} " +
                    $"boundsH={modelMinH:F1}..{modelMaxH:F1} boundsV={modelMinV:F1}..{modelMaxV:F1} " +
                    $"side={(pose.PlaceNotesAbove ? "above" : "below")} added={added}/{pose.PendingNotes.Count}");
        }

        /// <summary>
        /// 짧은 체인 치수를 2단으로 올리는 기존 규칙을 캡처 배율 계산 전에 확정한다.
        /// 치수 그리기와 주석 영역 예약이 같은 레벨 구성을 사용해야 종이 여백이 일치한다.
        /// </summary>
        private void PromoteMfgSmallPendingDimensions(MfgViewPose pose)
        {
            if (pose == null || pose.PendingDims == null || pose.PendingDims.Count == 0)
                return;
            if (pose.PromotedDimensionCount > 0)
                return;

            Func<MfgPendingDim, float> axisDistance = pd =>
                pd.Axis == "X" ? Math.Abs(pd.End.X - pd.Start.X)
                : pd.Axis == "Y" ? Math.Abs(pd.End.Y - pd.Start.Y)
                : Math.Abs(pd.End.Z - pd.Start.Z);

            float maxDistance = pose.PendingDims.Max(axisDistance);
            if (maxDistance <= 100f)
                return;

            float smallThreshold = maxDistance / 26f;
            foreach (var pending in pose.PendingDims)
            {
                if (pending.Level == 0 && axisDistance(pending) <= smallThreshold)
                {
                    pending.Level = 1;
                    pose.PromotedDimensionCount++;
                }
            }
        }

        /// <summary>
        /// 풍선을 놓는 화면 위/아래 방향과 같은 쪽에 실제로 존재하는 치수선의 종이 오프셋만 반환한다.
        /// 화면 오른쪽 치수까지 위/아래 풍선 여백에 포함하던 과대 계산을 피한다.
        /// </summary>
        private float GetMfgSameSideDimensionEnvelopeCanvas(MfgViewPose pose)
        {
            if (pose == null || pose.PendingDims == null || pose.PendingDims.Count == 0)
                return 0f;

            int maxLevel = pose.PendingDims.Any(d => d.Level == 1) ? 2 : 1;
            Func<MfgPendingDim, float> canvasOffset = pd =>
                pd.Level == 2
                    ? MfgCanvasBaseOff + MfgCanvasLvlSp * maxLevel
                    : pd.Level == 1
                        ? MfgCanvasBaseOff + MfgCanvasLvlSp
                        : MfgCanvasBaseOff;

            var cameraAxes = vizcore3d.View.GetCameraAxis();
            if (cameraAxes == null || cameraAxes.Count < 2)
                return pose.PendingDims.Max(canvasOffset);

            var rawScreenV = new VIZCore3D.NET.Data.Vector3D(
                cameraAxes[1].X, cameraAxes[1].Y, cameraAxes[1].Z);
            if (!TryNormalizeMfgVector(rawScreenV, out var screenV))
                return pose.PendingDims.Max(canvasOffset);

            bool placeAboveBeforeMirror = pose.MirrorVertical
                ? !pose.PlaceNotesAbove
                : pose.PlaceNotesAbove;
            float envelope = 0f;
            foreach (var pending in pose.PendingDims)
            {
                string offsetAxis = GetRemainingAxis(pose.ViewDirection, pending.Axis);
                VIZCore3D.NET.Data.Vector3D offsetVector;
                if (pose.UseReferenceAxis)
                {
                    offsetVector = offsetAxis == "X"
                        ? pose.ReferenceAxisX
                        : offsetAxis == "Y"
                            ? pose.ReferenceAxisY
                            : pose.ReferenceAxisZ;
                }
                else
                {
                    offsetVector = offsetAxis == "X"
                        ? new VIZCore3D.NET.Data.Vector3D(1f, 0f, 0f)
                        : offsetAxis == "Y"
                            ? new VIZCore3D.NET.Data.Vector3D(0f, 1f, 0f)
                            : new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f);
                }

                if (!TryNormalizeMfgVector(offsetVector, out var normalizedOffset))
                    continue;

                float direction = pending.PosOff ? 1f : -1f;
                float verticalDot = direction * (
                    normalizedOffset.X * screenV.X +
                    normalizedOffset.Y * screenV.Y +
                    normalizedOffset.Z * screenV.Z);
                bool sameSide = placeAboveBeforeMirror
                    ? verticalDot > 0.5f
                    : verticalDot < -0.5f;
                if (sameSide)
                    envelope = Math.Max(envelope, canvasOffset(pending));
            }

            return envelope;
        }

        /// <summary>
        /// 현재 뷰의 모델 슬롯에서 미리 확보할 주석 높이(종이 mm)를 계산한다.
        /// 같은 쪽 치수선·치수 문자·풍선 시작 간격·풍선 행 높이를 모두 포함한다.
        /// </summary>
        private float GetMfgAnnotationBudgetCanvas(MfgViewPose pose)
        {
            if (pose == null)
                return 0f;
            if (pose.SharedAnnotationBudgetCanvas.HasValue)
                return Math.Max(0f, pose.SharedAnnotationBudgetCanvas.Value);
            if (pose.PendingNotes == null)
                return 0f;

            int noteCount = pose.PendingNotes.Count(
                note => note != null && note.ArrowPosition != null);
            if (noteCount == 0)
                return 0f;

            float dimensionEnvelope = GetMfgSameSideDimensionEnvelopeCanvas(pose);
            return CalculateMfgAnnotationBudgetCanvas(noteCount, dimensionEnvelope);
        }

        /// <summary>
        /// EA 부재 pose를 받아 → 1차·2차 뷰 풍선 수 중 큰 쪽 기준으로 두 뷰가 공통 예약할 주석 높이(종이 mm)를 돌려준다. 풍선이 없으면 0.
        /// PDF 어댑터가 EA 부재 1차 캡처 전에 호출해 pose.SharedAnnotationBudgetCanvas에 저장한다.
        /// 2차 뷰 치수는 아직 없으므로 치수 영역은 최대 3단 오프셋으로 고정해 계산한다.
        /// </summary>
        private float GetMfgEaSharedAnnotationBudgetCanvas(MfgViewPose pose)
        {
            if (pose == null)
                return 0f;

            int primaryCount = pose.PendingNotes?.Count(
                note => note != null && note.ArrowPosition != null) ?? 0;
            int secondaryCount = pose.SecondaryPendingNotes?.Count(
                note => note != null && note.ArrowPosition != null) ?? 0;
            int sharedNoteCount = Math.Max(primaryCount, secondaryCount);
            if (sharedNoteCount == 0)
                return 0f;

            // 2차 뷰 치수는 1차 캡처 뒤에 수집되므로, 두 뷰에서 가능한 최대 3단 오프셋을 공통 예약한다.
            // 실제 풍선 위치는 각 뷰의 같은 쪽 치수 외곽을 사용하되 모델 fit 높이만 같게 유지한다.
            float sharedDimensionEnvelope = MfgCanvasBaseOff + MfgCanvasLvlSp * 2f;
            float sharedBudget = CalculateMfgAnnotationBudgetCanvas(
                sharedNoteCount, sharedDimensionEnvelope);
            DiagLog($"[MfgAnnotationBudget] EA shared primaryNotes={primaryCount} " +
                    $"secondaryNotes={secondaryCount} sharedNotes={sharedNoteCount} " +
                    $"dimensionEnvelope={sharedDimensionEnvelope:F1} budget={sharedBudget:F1}");
            return sharedBudget;
        }

        /// <summary>
        /// 풍선 개수와 같은 쪽 치수 영역 높이를 받아 → 치수 영역에 치수 문자 여유·풍선 간격·풍선 글자 높이·행 간격을 더한 값(종이 mm)을 돌려준다.
        /// 풍선이 0개면 0. 치수 영역이 있을 때만 치수 문자 높이+여백을 더하고, 행 간격은 풍선 수−1만큼 곱한다.
        /// </summary>
        private float CalculateMfgAnnotationBudgetCanvas(int noteCount, float dimensionEnvelope)
        {
            if (noteCount <= 0)
                return 0f;

            float measureClearance = dimensionEnvelope > 0f
                ? MfgCanvasMeasureTextHeight + MfgCanvasMeasureTextMargin
                : 0f;
            return dimensionEnvelope +
                   measureClearance +
                   MfgCanvasBalloonGap +
                   MfgCanvasBalloonTextHeight +
                   MfgCanvasBalloonRowSpacing * Math.Max(0, noteCount - 1);
        }

        /// <summary>
        /// 가공도 보조선·치수를 '모델 2D 캡처 + RescaleObject 후 확정된 실측 배율(newScale)'로 그린다.
        /// 추정 스케일이 아닌 실측을 써야 보조선 길이 = 캔버스 절대값으로 부재·뷰 무관 일정해진다.
        /// pose.PendingDims = BuildMfgSceneCore/BuildEaSecondaryScene가 수집한 그릴 목록(offset 미적용).
        /// 캡처 간 측정 격리: EA 1차/2차가 각자 캡처 직전에 호출되므로, 직전 뷰의 3D 측정·보조선을 먼저 비운다
        /// (이미 2D로 변환됐으므로 3D 원본 제거는 무해).
        /// </summary>
        private void DrawMfgDimsAtScale(MfgViewPose pose, BOMData bom, float newScale)
        {
            if (pose == null) return;
            pose.DimensionEnvelopeOffset = 0f;
            if (pose.PendingDims == null || pose.PendingDims.Count == 0) return;
            if (newScale <= 0f || float.IsNaN(newScale) || float.IsInfinity(newScale)) return;

            // 측정·보조선 초기화는 참조축 생성 전에 BuildMfgSceneCore/BuildEaSecondaryScene가 수행한다.
            // 여기서 Measure.Clear를 호출하면 활성 참조축까지 삭제되므로 캡처가 끝날 때까지 다시 지우지 않는다.
            pose.ShapeDrawingIds.Clear();

            // 실측 배율로 캔버스 절대 오프셋(가공도 9/18mm) 역산 — 제작도와 동일 정책(ComputeCanvasAbsoluteOffsets).
            ComputeCanvasAbsoluteOffsets(newScale, out float baseOff, out float lvlSp,
                MfgCanvasBaseOff, MfgCanvasLvlSp);

            // 짧은 체인 치수 승격은 캡처 전 PromoteMfgSmallPendingDimensions에서 확정했다.
            // 승격이 있으면 maxLevel 계산이 전체 치수를 자동으로 3단(off2)으로 민다.
            int maxLevel = pose.PendingDims.Any(d => d.Level == 1) ? 2 : 1;
            float off0 = baseOff, off1 = baseOff + lvlSp, off2 = baseOff + lvlSp * maxLevel;
            float sameSideCanvasEnvelope = GetMfgSameSideDimensionEnvelopeCanvas(pose);
            pose.DimensionEnvelopeOffset = sameSideCanvasEnvelope / newScale;
            // 보조선 시작 gap도 종이 절대 기준(2mm)으로 — 오프셋과 동일하게 실측 배율 역산 (2026-07-03).
            //   옛 모델좌표 고정 10mm는 부재·축척마다 종이 gap이 들쭉날쭉했음.
            float extGap = MfgCanvasExtGap / newScale;

            var extLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
            // 2단(Level 1) 텍스트 슬라이드 — 제작도와 동일 사양(종이 2.5mm, 치수선 방향만 SDK 반영).
            //   부호는 실제 카메라 투영 헬퍼: 가로(길이축) 치수=화면 오른쪽(MfgHeightToRight),
            //   세로(폭) 치수=화면 위(MfgAxisUpPositive). 호출 시점 카메라 = 캡처 시점과 동일(roll 원복 전).
            float lvl2SlideMag = MfgLvl2TextSlideCanvas / newScale;
            bool useOrientationUserAxis = pose.UseReferenceAxis;
            int userAxisDimCount = 0;
            foreach (var pd in pose.PendingDims)
            {
                float off = pd.Level == 2 ? off2 : (pd.Level == 1 ? off1 : off0);
                float slide = 0f;
                if (pd.Level == 1)
                {
                    bool slidePositive = pd.Axis == pose.LongestAxis
                        ? MfgHeightToRight(pd.Axis)
                        : MfgAxisUpPositive(pd.Axis);
                    slide = (slidePositive ? 1f : -1f) * lvl2SlideMag;
                }
                // alignExtToBaseline=false — 제작도와 동일하게 보조선을 osnap 점 그대로에서 시작.
                //   (true=박스 스냅은 대각(beveled) 부재의 실제 꼭짓점을 박스 모서리로 밀어내 대각을 무시했음.
                //    직사각 부재는 극점 osnap 좌표가 박스값과 같아 무영향. 2026-07-01)
                VIZCore3D.NET.Data.Vertex3D userMeasureAxis = null;
                VIZCore3D.NET.Data.Vertex3D userOffsetAxis = null;
                if (useOrientationUserAxis)
                {
                    string offsetAxis = GetRemainingAxis(pose.ViewDirection, pd.Axis);
                    userMeasureAxis = GetMfgOrientationAxisVector(pose, pd.Axis);
                    userOffsetAxis = GetMfgOrientationAxisVector(pose, offsetAxis);
                    if (userMeasureAxis != null && userOffsetAxis != null) userAxisDimCount++;
                }
                DrawDimension(pd.Start, pd.End, pd.Axis, off,
                    bom.MinX, bom.MinY, bom.MinZ, pose.ViewDirection, extLines,
                    bom.MaxX, bom.MaxY, bom.MaxZ, pd.PosOff, false, extGap, slide,
                    userMeasureAxis, userOffsetAxis);
            }
            if (extLines.Count > 0)
            {
                int shapeId = vizcore3d.ShapeDrawing.AddLine(extLines, -1, System.Drawing.Color.Blue, 0.15f, true);
                if (shapeId >= 0) pose.ShapeDrawingIds.Add(shapeId);
            }
            DiagLog($"[DrawMfgDims] bom={bom.Index} view={pose.ViewDirection} newScale={newScale:F4} " +
                $"off0={off0:F2} off1={off1:F2} off2={off2:F2} extGap={extGap:F2} " +
                $"sameSideEnvelopeCanvas={sameSideCanvasEnvelope:F1} dims={pose.PendingDims.Count} " +
                $"promoted={pose.PromotedDimensionCount} " +
                $"orientation={pose.OrientationAxis}/{pose.OrientationAngle:F1}° userAxisDims={userAxisDimCount}");
        }

        /// <summary>
        /// 세로(폭) 치수를 화면 오른쪽에 두는 positiveOffset을 **부재마다 실제 카메라 투영으로 계산**.
        /// (고정 표 폐기 — 모든 부재에 일반화 2026-06-30)
        /// 원리: 캡처는 최장 가시축(=치수 오프셋축=길이축)을 가로로 눕힌다. 그 길이축이 카메라의
        ///   우축/상축 중 어디에 더 정렬되는지로 회전 여부를 판정하고, 회전 시 화면-오른쪽은 상축이
        ///   된다(90° 회전). +오프셋축이 그 화면-오른쪽과 같은 방향이면 max(true)쪽이 오른쪽.
        /// 부호 calibration: eff 부호가 반대면 sRollSign(회전 시)·또는 반환 부호만 뒤집으면 전부 일관.
        /// </summary>
        private bool MfgHeightToRight(string offsetDir)
        {
            const float sRollSign = 1f;   // 회전 시 화면-오른쪽 = +상축(상축이 오른쪽으로 옴). 반대면 -1
            try
            {
                var axes = vizcore3d.View.GetCameraAxis();   // [0]=우, [1]=상, [2]=시선 (월드좌표)
                if (axes != null && axes.Count >= 3)
                {
                    var right = axes[0]; var up = axes[1];
                    float ox = offsetDir == "X" ? 1f : 0f;
                    float oy = offsetDir == "Y" ? 1f : 0f;
                    float oz = offsetDir == "Z" ? 1f : 0f;
                    float dotR = ox * right.X + oy * right.Y + oz * right.Z;
                    float dotU = ox * up.X + oy * up.Y + oz * up.Z;
                    bool rolled = Math.Abs(dotU) > Math.Abs(dotR);   // 길이축이 세로 → 회전 예정
                    float eff = rolled ? (dotU * sRollSign) : dotR;  // +오프셋축이 화면-오른쪽이면 양수
                    DiagLog($"[HeightDir] off={offsetDir} dotR={dotR:F2} dotU={dotU:F2} rolled={rolled} posOff={eff > 0}");
                    return eff > 0;
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// 해당 3D 축의 +방향이 캡처 화면의 '위'인지 판정 (가로화 회전 예정 반영).
        /// MfgHeightToRight와 동일 원리 — 카메라 세팅 후·화면회전 전에 호출.
        /// MfgUpRollSign: +90° 화면회전 시 새 상축 = -기존 우축 가정. 실기 반대면 부호만 반전. 2026-07-02
        /// </summary>
        private const float MfgUpRollSign = -1f;

        /// <summary>
        /// 3D 축 이름(X/Y/Z)을 받아 → 현재 카메라에서 그 축의 +방향이 캡처 화면의 '위'인지 판정한다. 판정 불가면 true.
        /// 카메라 세팅 후·가로화 회전 전에 부른다. 축이 지금 가로면 회전 후 세로가 된다고 보고 우축 성분에 부호를 곱한다.
        /// vizcore3d.View.GetCameraAxis()로 현재 카메라를 읽으므로 순수 계산이 아니다.
        /// </summary>
        private bool MfgAxisUpPositive(string axis)
        {
            try
            {
                var axes = vizcore3d.View.GetCameraAxis();
                if (axes != null && axes.Count >= 2)
                {
                    float ox = axis == "X" ? 1f : 0f, oy = axis == "Y" ? 1f : 0f, oz = axis == "Z" ? 1f : 0f;
                    var right = axes[0]; var up = axes[1];
                    float dotR = ox * right.X + oy * right.Y + oz * right.Z;
                    float dotU = ox * up.X + oy * up.Y + oz * up.Z;
                    bool willRoll = Math.Abs(dotR) > Math.Abs(dotU);   // 축이 지금 가로 → 회전 후 세로가 됨
                    float eff = willRoll ? (dotR * MfgUpRollSign) : dotU;
                    DiagLog($"[AxisUp] ax={axis} dotR={dotR:F2} dotU={dotU:F2} willRoll={willRoll} upPos={eff > 0}");
                    return eff > 0;
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// 가공도 보조선 시작 보정 — 치수 끝점의 '오프셋축' 좌표를, 같은 치수축 레벨(±tol)의 osnap 점들 중
        /// 치수선 쪽 극값으로 스냅한다. 직사각 부재는 그 극값이 외곽 모서리라 변화 없고,
        /// 대각(beveled) 부재는 실제 대각 꼭짓점이 되어 — 순수 osnap(보조선이 모델을 가로지름)과
        /// 박스 스냅(빈 공간에 뜸)의 문제를 동시에 해소한다. 측정값(치수축 성분)은 불변. 2026-07-02
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D SnapDimPointTowardDimLine(
            VIZCore3D.NET.Data.Vector3D p, string dimAxis, string offAxis, bool posOff,
            List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> osnapFull, float tol)
        {
            if (osnapFull == null || osnapFull.Count == 0) return p;
            Func<VIZCore3D.NET.Data.Vertex3D, string, float> vAx =
                (v, a) => a == "X" ? v.X : a == "Y" ? v.Y : v.Z;
            float pDim = GetAxisValue(p, dimAxis);

            bool found = false;
            float best = 0f;
            foreach (var o in osnapFull)
            {
                if (Math.Abs(vAx(o.point, dimAxis) - pDim) > tol) continue;
                float ov = vAx(o.point, offAxis);
                if (!found || (posOff ? ov > best : ov < best)) { best = ov; found = true; }
            }
            if (!found) return p;

            float before = GetAxisValue(p, offAxis);
            if (Math.Abs(best - before) > 0.5f)
                DiagLog($"[DimSnap] dim={dimAxis}@{pDim:F1} off={offAxis} {before:F1}→{best:F1} posOff={posOff}");

            switch (offAxis)
            {
                case "X": return new VIZCore3D.NET.Data.Vector3D(best, p.Y, p.Z);
                case "Y": return new VIZCore3D.NET.Data.Vector3D(p.X, best, p.Z);
                default:  return new VIZCore3D.NET.Data.Vector3D(p.X, p.Y, best);
            }
        }

        /// <summary>
        /// EA 앵글 부재와 1차 pose를 받아 → 2차 뷰 카메라를 세우고 길이·폭 체인치수를 PendingDims에 모은 새 pose를 돌려준다.
        /// PDF 어댑터가 1차 캡처 뒤에 호출. 1차 참조축·측정·풍선을 지우고 2차 전용 참조축을 새로 만든다.
        /// 코너가 슬롯 가운데를 향하도록 MirrorVertical 결정. 길이 치수는 위 슬롯 뷰 몫이라 스왑이면 건너뛴다.
        /// </summary>
        private MfgViewPose BuildEaSecondaryScene(
            BOMData bom,
            MfgViewPose primaryPose,
            bool secondaryAtTop = true)
        {
            var pose = new MfgViewPose
            {
                LongestAxis = primaryPose.LongestAxis,
                OrientationAxis = primaryPose.OrientationAxis,
                OrientationAngle = primaryPose.OrientationAngle,
                UseReferenceAxis = primaryPose.UseReferenceAxis,
                ReferenceAxisX = primaryPose.ReferenceAxisX,
                ReferenceAxisY = primaryPose.ReferenceAxisY,
                ReferenceAxisZ = primaryPose.ReferenceAxisZ,
                ReferenceAxisOrigin = primaryPose.ReferenceAxisOrigin,
                PlaceNotesAbove = secondaryAtTop,
                SharedAnnotationBudgetCanvas = primaryPose.SharedAnnotationBudgetCanvas
            };
            if (primaryPose.SecondaryPendingNotes != null)
                pose.PendingNotes.AddRange(primaryPose.SecondaryPendingNotes);

            // 1차 뷰 참조축·측정은 모두 정리한 뒤 2차 뷰 전용 참조축을 새로 만든다.
            ClearMfgViewAnnotations("BuildEaSecondaryScene");
            vizcore3d.Review.Note.Clear();

            // 두 번째 뷰는 독립 카메라에서 절대 방향으로 만든다.
            //   절대 방향(MoveCamera로 직교 프레임 재설정)이라 primary 회전을 되돌릴 필요가 없다 —
            //   MoveCamera가 깨끗한 직교 프레임을 다시 잡고, 거기서 절대 roll만 박는다(누적 0).
            if (primaryPose.LongestAxis == "Z")
            {
                pose.ViewDirection = "X";
                // 2번 뷰를 반대쪽(오른쪽)에서 — 첫 부재 두 뷰가 실제 접합 방향대로 이어지도록
                //   (X_MINUS는 '왼쪽에서 찍은 꼴'이 되어 1번 뷰와 정합 안 됨, 2026-06-29 검증 중)
                pose.CameraDirection = VIZCore3D.NET.Data.CameraDirection.X_PLUS;
                pose.ApplyZ90 = true;
            }
            else
            {
                pose.ViewDirection = "Z";
                pose.CameraDirection = VIZCore3D.NET.Data.CameraDirection.Z_MINUS;
            }

            pose.UsedMinusCamera = true;
            if (pose.UseReferenceAxis)
                ActivateMfgReferenceAxis(pose, bom, "EA-secondary");
            else
                vizcore3d.View.MoveCamera(pose.CameraDirection);
            // 가로화(세로면 회전)는 캡처 직전 호출자(RenderMfgRowToViewArea)에서 ProbeAndRollLandscape로 수행.

            // ── 2차 뷰 상하 미러 판정 — 코너가 최종 슬롯에서 가운데를 향하는지 (2026-07-02) ──
            //   위 슬롯이면 코너가 아래(cornerUp=false), 아래 슬롯이면 위(cornerUp=true)여야 함.
            //   어긋나면: 모델 = 2D 미러(SetSelected3DMirrorBy2DView). 치수·보조선은 SDK 2D 미러가 매핑까지 함께 뒤집으므로 별도 반전 불필요 (40c60ec).
            string secVertAxis = GetRemainingAxis(pose.ViewDirection, pose.LongestAxis);   // 2차 뷰 화면 세로축
            bool secAxisUp = MfgAxisUpPositive(secVertAxis);
            if (primaryPose.HasSecCorner)
            {
                bool secCornerUp = (primaryPose.SecCornerAtMax == secAxisUp);
                bool needUp = !secondaryAtTop;
                pose.MirrorVertical = (secCornerUp != needUp);
                DiagLog($"[EAMirror] bom={bom.Index} secAxis={primaryPose.SecCornerAxis} atMax={primaryPose.SecCornerAtMax} " +
                    $"axisUp={secAxisUp} cornerUp={secCornerUp} atTop={secondaryAtTop} → mirror={pose.MirrorVertical}");
            }

            var osnapPoints = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
            var osnapList = vizcore3d.Object3D.GetOsnapPoint(bom.Index);
            if (osnapList != null)
            {
                foreach (var osnap in osnapList)
                {
                    switch (osnap.Kind)
                    {
                        case VIZCore3D.NET.Data.OsnapKind.LINE:
                            if (osnap.Start != null)
                                osnapPoints.Add((new VIZCore3D.NET.Data.Vertex3D(
                                    osnap.Start.X, osnap.Start.Y, osnap.Start.Z), bom.Name));
                            if (osnap.End != null)
                                osnapPoints.Add((new VIZCore3D.NET.Data.Vertex3D(
                                    osnap.End.X, osnap.End.Y, osnap.End.Z), bom.Name));
                            break;
                        case VIZCore3D.NET.Data.OsnapKind.POINT:
                            if (osnap.Center != null)
                                osnapPoints.Add((new VIZCore3D.NET.Data.Vertex3D(
                                    osnap.Center.X, osnap.Center.Y, osnap.Center.Z), bom.Name));
                            break;
                    }
                }
            }

            if (bom.Holes != null)
            {
                foreach (var hole in bom.Holes)
                    osnapPoints.Add((new VIZCore3D.NET.Data.Vertex3D(
                        hole.CenterX, hole.CenterY, hole.CenterZ), bom.Name));
            }
            if (bom.SlotHoles != null)
            {
                foreach (var slot in bom.SlotHoles)
                    osnapPoints.Add((new VIZCore3D.NET.Data.Vertex3D(
                        slot.CenterX, slot.CenterY, slot.CenterZ), bom.Name));
            }

            // 은선 필터 '전' 원본 보존 — 보조선 시작점 레벨 스냅용.
            //   대각(beveled) 꼭짓점은 깊이 필터가 뒷면 플랜지째로 지울 수 있는데, 실루엣 외곽으로는
            //   도면에 그려지므로 스냅은 원본에서 찾는다. 2026-07-02
            var osnapFullSec = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>(osnapPoints);

            osnapPoints = FilterHiddenLineOsnap(
                osnapPoints,
                pose.ViewDirection,
                bom.MinX, bom.MaxX,
                bom.MinY, bom.MaxY,
                bom.MinZ, bom.MaxZ,
                true);
            if (osnapPoints.Count < 2)
                return pose;

            const float tolerance = 0.5f;
            // 2차 뷰도 제작도 4점 규칙 적용 (사용자 사양 2026-06-23) — 1차 뷰와 동일하게 극점만 남김.
            {
                var eaMap = new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>>
                    { { bom.Index, osnapPoints } };
                var filtered = FilterOsnapForDimAxis(eaMap, pose.LongestAxis, pose.ViewDirection, tolerance);
                if (filtered.Count >= 2) osnapPoints = filtered;
            }
            var mergedPoints = MergeCoordinates(osnapPoints, tolerance);
            var dimensions = AddChainDimensionByAxis(
                mergedPoints,
                pose.LongestAxis,
                tolerance,
                pose.ViewDirection);
            if (dimensions.Count == 0)
                return pose;

            VIZCore3D.NET.Data.MeasureStyle style = vizcore3d.Review.Measure.GetStyle();
            style.Prefix = false;
            style.Unit = false;
            style.NumberOfDecimalPlaces = 0;
            style.DX_DY_DZ = false;
            style.Frame = false;
            style.ContinuousDistance = false;
            style.BackgroundTransparent = true;
            style.FontColor = Color.Blue;
            style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
            style.FontBold = true;
            style.LineColor = Color.Blue;
            style.LineWidth = 1;
            style.ArrowColor = Color.Blue;
            style.ArrowSize = 5;
            style.AssistantLine = false;
            style.AlignDistanceText = true;   // 제작도와 동일 자동 정렬
            style.AlignDistanceTextMargine = MfgCanvasMeasureTextMargin;
            vizcore3d.Review.Measure.SetStyle(style);

            string offsetAxis = GetRemainingAxis(pose.ViewDirection, pose.LongestAxis);
            float modelCenter = offsetAxis == "X"
                ? (bom.MinX + bom.MaxX) / 2f
                : offsetAxis == "Y"
                    ? (bom.MinY + bom.MaxY) / 2f
                    : (bom.MinZ + bom.MaxZ) / 2f;
            var offsetValues = dimensions
                .Where(d => !d.IsTotal)
                .SelectMany(d => new[]
                {
                    GetAxisValue(d.StartPoint, offsetAxis),
                    GetAxisValue(d.EndPoint, offsetAxis)
                });
            bool positiveOffset = ComputePositiveOffsetByOsnapExtreme(offsetValues, modelCenter);

            // 길이 치수는 항상 페어 '바깥쪽'으로 — 위 슬롯이면 위, 아래 슬롯이면 아래 (2026-07-02).
            //   스왑·미러 도입으로 2차 뷰 슬롯이 부재마다 달라져, 기존 휴리스틱(외곽 osnap·Z최장 고정값)이
            //   치수를 뷰 사이(가운데)에 놓아 반대 뷰 모델과 겹치던 문제 제거.
            //   미러 시 좌표·PosOff가 나중에 반전되므로 수집(pre-mirror) 공간으로 환산해 지정.
            bool lenDimUpPre = pose.MirrorVertical ? !secondaryAtTop : secondaryAtTop;
            positiveOffset = (secAxisUp == lenDimUpPre);
            DiagLog($"[EALenDim] bom={bom.Index} axisUp={secAxisUp} mirror={pose.MirrorVertical} " +
                $"atTop={secondaryAtTop} → posOff={positiveOffset}");

            // 보조선·치수 '그리기'는 캡처 직후 실측 newScale로(DrawMfgDimsAtScale). 여기선 목록만 수집.
            //   길이 치수는 '위 슬롯 뷰'가 그린다 (사용자 사양 2026-07-02): 스왑이면 1차(위)가 이미 가짐 → 2차는 스킵.
            if (!primaryPose.SwapViews)
            {
                foreach (var dim in dimensions.Where(d => d.IsVisible))
                {
                    int lvl = dim.IsTotal ? 2 : (dim.DisplayLevel > 0 ? 1 : 0);
                    string offAxL = GetRemainingAxis(pose.ViewDirection, dim.Axis);
                    pose.PendingDims.Add(new MfgPendingDim
                    {
                        Start = SnapDimPointTowardDimLine(dim.StartPoint, dim.Axis, offAxL, positiveOffset, osnapFullSec, 1.0f),
                        End   = SnapDimPointTowardDimLine(dim.EndPoint,   dim.Axis, offAxL, positiveOffset, osnapFullSec, 1.0f),
                        Axis = dim.Axis, Level = lvl, PosOff = positiveOffset
                    });
                }
            }
            else
            {
                DiagLog($"[EALenDim] bom={bom.Index} 길이 치수 스킵 — 스왑으로 1차(위 슬롯)가 보유");
            }

            // 2차 뷰에 폭(높이) 치수 추가 — 폭 = 보이는 축 중 길이축이 아닌 것. 세로(폭)는 화면 오른쪽으로(MfgHeightToRight).
            string widthAxisSec;
            switch (pose.ViewDirection)
            {
                case "X": widthAxisSec = (pose.LongestAxis == "Y") ? "Z" : "Y"; break;
                case "Y": widthAxisSec = (pose.LongestAxis == "X") ? "Z" : "X"; break;
                default:  widthAxisSec = (pose.LongestAxis == "X") ? "Y" : "X"; break;
            }
            var widthDims = AddChainDimensionByAxis(mergedPoints, widthAxisSec, tolerance, pose.ViewDirection);
            if (widthDims.Count > 0)
            {
                string offAxW = GetRemainingAxis(pose.ViewDirection, widthAxisSec);
                bool posOffW = MfgHeightToRight(offAxW);
                // 세로(폭) 치수는 1단만 — 전체 치수(2단)는 체인과 중복이라 생략 (사용자 사양 2026-07-03).
                foreach (var dim in widthDims.Where(d => d.IsVisible && !d.IsTotal))
                {
                    int lvl = dim.DisplayLevel > 0 ? 1 : 0;
                    pose.PendingDims.Add(new MfgPendingDim
                    {
                        Start = SnapDimPointTowardDimLine(dim.StartPoint, dim.Axis, offAxW, posOffW, osnapFullSec, 1.0f),
                        End   = SnapDimPointTowardDimLine(dim.EndPoint,   dim.Axis, offAxW, posOffW, osnapFullSec, 1.0f),
                        Axis = dim.Axis, Level = lvl, PosOff = posOffW
                    });
                }
                DiagLog($"[EA Secondary] bom={bom.Index} 폭치수 수집 axis={widthAxisSec} dims={widthDims.Count} posOffW={posOffW}");
            }

            // 미러 시 치수 좌표는 반전하지 않는다 (2026-07-02 실기 확정):
            //   SetSelected3DMirrorBy2DView는 모델뿐 아니라 이후 Add2DMeasureFrom3DMeasure의 3D→2D 매핑까지
            //   함께 뒤집는다 — 22546(비미러)·22548(미러)이 같은 3D 배치에서 반대 화면쪽에 렌더됨으로 증명.
            //   따라서 원래 기하 기준으로 그대로 두면 모델과 함께 통째로 뒤집혀 정합. 사전 반전은 이중 반전이었음.
            //   ([EALenDim]의 pre-mirror 방향 환산만으로 충분)

            DiagLog($"[EA Secondary] bom={bom.Index} view={pose.ViewDirection} " +
                $"longest={pose.LongestAxis} dims={dimensions.Count} Z90={pose.ApplyZ90}");
            return pose;
        }

        /// <summary>
        /// 가공도 페이지의 한 행(BOM 1개) 렌더링.
        /// 일반 부재는 한 뷰, EA 부재는 같은 ViewArea를 위·아래 두 뷰로 분할한다.
        /// </summary>
        private bool RenderMfgRowToViewArea(int rowIdx, BOMData bom,
            VIZCore3D.NET.Data.TemplateViewArea area,
            Func<bool> shouldCancel = null,
            string progressPrefix = "가공도")
        {
            ClearMfgViewAnnotations("RenderMfgRow/start");
            vizcore3d.Review.Note.Clear();

            var createdObjectIds = new List<int>();
            bool success = false;
            try
            {
                if (area.Width <= 0 || area.Height <= 0
                    || float.IsNaN(area.Width) || float.IsNaN(area.Height)
                    || float.IsInfinity(area.Width) || float.IsInfinity(area.Height))
                {
                    DiagLog($"[RenderMfgRow] row={rowIdx} area 비정상 W={area.Width} H={area.Height}");
                    return false;
                }

                bool isEA = IsAngleFromSpref(bom.Index);
                float viewGap = 0f;   // 완전 밀착 (사용자 사양 2026-06-23) — EA 평면도·정면도 사이 간격 제거
                float viewHeight = isEA ? (area.Height - viewGap) / 2f : area.Height;

                CheckMfgCancellation(
                    shouldCancel,
                    $"{progressPrefix} 행 {rowIdx} 장면 준비 중...",
                    $"{progressPrefix} 행 {rowIdx} 장면 준비 전");
                var pose = BuildMfgSceneCore(bom.Index, isEA);
                CheckMfgCancellation(
                    shouldCancel,
                    $"{progressPrefix} 행 {rowIdx} 주 뷰 준비 중...",
                    $"{progressPrefix} 행 {rowIdx} 장면 준비 후");
                if (isEA)
                    pose.SharedAnnotationBudgetCanvas = GetMfgEaSharedAnnotationBudgetCanvas(pose);

                // DASH_LINE(은선 점선) 렌더모드 제거 — 은선 없는 캡처로 전환해 무의미 (2026-07-03).
                // 실루엣 엣지 끔 — 켜면 SDK가 모든 모서리를 균일 굵기로 통일해 모델선(2.0)이
                //   치수선·보조선과 구분이 사라진다(제작도는 캡처 직전 실루엣 미사용). 2026-06-29
                vizcore3d.View.SilhouetteEdge = false;

                // ── EA 두 뷰 상하 슬롯 — 스왑 판정은 코어(5-2a 직후)에서 수행 (길이 치수 배치가 의존) ──
                bool swapViews = isEA && pose.SwapViews;
                float primaryY = swapViews ? area.Y + viewHeight + viewGap : area.Y;
                float secondaryY = swapViews ? area.Y : area.Y + viewHeight + viewGap;
                // EA 페어는 풍선을 뷰 사이가 아닌 각 슬롯의 바깥쪽에 둔다. 단일 뷰는 기존처럼 아래쪽.
                pose.PlaceNotesAbove = isEA && swapViews;

                // 가로 배치 — 임시 캡처로 실제 세로/가로 측정 후 세로면 화면축 90° 회전.
                //   (DoEvents 제거 — 격리 5단계 2026-07-21: 네이티브 캡처 전후 메시지 펌프가 크래시 용의)
                float primRoll = ProbeAndRollLandscape(bom.Index, 1.25f);
                CheckMfgCancellation(
                    shouldCancel,
                    $"{progressPrefix} 행 {rowIdx} 주 뷰 캡처 중...",
                    $"{progressPrefix} 행 {rowIdx} 주 뷰 방향 확정 후");

                int primaryObjId;
                bool primaryOk = CaptureMfgSceneToViewArea(
                    rowIdx, bom, pose,
                    area.X, primaryY, area.Width, viewHeight,
                    isEA ? "EA primary" : "primary",
                    out primaryObjId);
                // 회전 원복 (누적 방지) — 캡처 성공/실패와 무관하게
                if (primRoll != 0f)
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, -primRoll);
                if (!primaryOk)
                {
                    if (primaryObjId >= 0)
                        createdObjectIds.Add(primaryObjId);
                    return false;
                }
                createdObjectIds.Add(primaryObjId);
                CheckMfgCancellation(
                    shouldCancel,
                    $"{progressPrefix} 행 {rowIdx} 주 뷰 완료",
                    $"{progressPrefix} 행 {rowIdx} 주 뷰 캡처 후");

                if (isEA)
                {
                    try
                    {
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 준비 중...",
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 시작 전");
                        var secondaryPose = BuildEaSecondaryScene(
                            bom, pose, !swapViews);   // 2차 뷰 슬롯: 기본=위, 스왑 시=아래

                        // 2차 뷰도 동일: 임시 캡처로 세로/가로 측정 후 세로면 90° 회전 (DoEvents 제거 — 격리 5단계)
                        float secRoll = ProbeAndRollLandscape(bom.Index, 1.25f);
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 캡처 중...",
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 방향 확정 후");

                        int secondaryObjId;
                        bool secondaryOk = CaptureMfgSceneToViewArea(
                            rowIdx, bom, secondaryPose,
                            area.X, secondaryY,
                            area.Width, viewHeight,
                            "EA secondary",
                            out secondaryObjId);
                        if (secRoll != 0f)
                            vizcore3d.View.RotateCameraByScreenAxis(0, 0, -secRoll);
                        if (secondaryOk && secondaryObjId >= 0)
                            createdObjectIds.Add(secondaryObjId);
                        if (!secondaryOk)
                        {
                            if (secondaryObjId >= 0)
                            {
                                try
                                {
                                    vizcore3d.Drawing2D.Object2D.DeleteObjectBy2DView(secondaryObjId);
                                }
                                catch { }
                            }
                            DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} WARN EA secondary 캡처 실패 — primary 유지");
                        }
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 완료",
                            $"{progressPrefix} 행 {rowIdx} 보조 뷰 캡처 후");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} WARN EA secondary 실패 — primary 유지: {ex.Message}");
                    }
                }

                CheckMfgCancellation(
                    shouldCancel,
                    $"{progressPrefix} 행 {rowIdx} 완료",
                    $"{progressPrefix} 행 {rowIdx} 완료 후");
                success = true;
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} ERROR: {ex.Message}");
                return false;
            }
            finally
            {
                if (!success)
                {
                    foreach (int objId in createdObjectIds)
                    {
                        try { vizcore3d.Drawing2D.Object2D.DeleteObjectBy2DView(objId); } catch { }
                    }
                    DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} 실패 objects={createdObjectIds.Count} cleanup");
                }

                vizcore3d.Review.Note.Clear();
                ClearMfgViewAnnotations("RenderMfgRow/finally");
            }
        }

        // ── 가공도 전용 치수 선별 규칙 (제작도와 독립) ──
        //   제작도는 ApplySmartFiltering을 직접 쓰지만, 가공도는 이 진입점을 통해
        //   파라미터·규칙을 독립적으로 조정·확장한다. 가공도는 단일 부재·단일 뷰라
        //   제작도식 "축당" 개념과 달라, 여기서 가공도 고유 규칙을 키울 수 있다.
        private const int MfgMaxDimensionsPerAxis = 8;   // 가공도 축당 최대 (제작도와 별도 값)
        private const float MfgMinTextSpace = 25.0f;     // 가공도 치수 텍스트 겹침 간격 (제작도와 별도 값)
        // 가공도 보조선 오프셋 — 종이(캔버스) 절대 mm. 제작도 5/5(1단 5·전체 10)와 별도로 줄임.
        //   사용자 사양 2026-06-23: "많이" → 1단 2·전체 4mm. ComputeCanvasAbsoluteOffsets에 인자로 전달.
        private const float MfgCanvasBaseOff = 9.0f;     // 1단 종이 mm (2→4→6→9, 2026-07-06: 세로 텍스트-모델 밀착 해소 1.5배 — 제작도와 동일 배율)
        private const float MfgCanvasLvlSp   = 9.0f;     // 단 간격 → 전체 = 9+9 = 18mm
        private const float MfgCanvasExtGap  = 2.0f;     // 보조선 시작 gap 종이 mm — 오프셋과 동일하게 종이 절대 기준 (2026-07-03, 옛 모델좌표 10mm 폐기)
        private const float MfgLvl2TextSlideCanvas = 2.5f;  // 2단(Level 1) 치수 텍스트 슬라이드 종이 mm — 가로=오른쪽/세로=위 (2026-07-21, 제작도와 동일 사양)
        private const float MfgCanvasBalloonGap = 6.0f;      // 치수 외곽→첫 풍선 문자 기준선 종이 mm
        private const float MfgCanvasBalloonRowSpacing = 8.0f; // 풍선 행 간격 종이 mm(6mm 문자 높이 + 2mm 여백)
        private const float MfgCanvasBalloonTextHeight = 6.0f; // Add2DNoteFrom3DNote 변환 글자 높이
        private const float MfgCanvasMeasureTextHeight = 10.0f; // Add2DMeasureFrom3DMeasure 변환 글자 높이
        private const int MfgCanvasMeasureTextMargin = 3;        // MeasureStyle.AlignDistanceTextMargine
        private const float MfgMinModelAreaHeightRatio = 0.35f; // 주석이 많아도 모델 영역을 최소 35% 보존


        // (제거 2026-07-19) 템플릿 JSON 사전변환 PoC — 실측 결과 ConvertExcelToJson 290초 + 태그 미보존으로
        //   접근법 자체 폐기. 템플릿을 작게(~4천 셀) 유지하면 ImportExcelWithData가 수백 ms라 JSON 불필요.

        /// <summary>
        /// 가공도 치수 선별 — 카메라가 보는 평면의 치수를 규칙 기반으로 추린다.
        /// 현재는 제작도 알고리즘(ApplySmartFiltering)을 가공도 전용 파라미터로 차용.
        /// 향후 가공도 고유 규칙(외곽 우선·특정 Osnap 보존·화면 전체 개수 등)은 여기서 분기·확장.
        /// </summary>
        private List<ChainDimensionData> FilterMfgDimensions(List<ChainDimensionData> dims)
        {
            return ApplySmartFiltering(dims, MfgMaxDimensionsPerAxis, MfgMinTextSpace);
        }

        // ── 홀/슬롯홀 추출 (GetNodeHoleInfo API) — 가공도 풍선 + BOM/제작도(DetectHoles) 공용 단일 출처 ──
        //   2026-06-23: 원기둥·Osnap 추측 휴리스틱 전면 제거 → 이 API가 홀/슬롯 검출의 유일 출처.
        //   DetectHoles(Form1.BOM.cs)가 bom.Holes/SlotHoles를 이걸로 채우고, 가공도 풍선은 부재별로 직접 호출.
        /// <summary>
        /// 가공도 전용 — GetNodeHoleInfo로 홀(CIRCLE)·슬롯홀(SLOT_HOLE)을 추출.
        /// ⚠ 슬롯 길이(SlotLength)·Size 의미는 SDK가 직접 안 줘 잠정 매핑 + 진단 로그로 실측 중.
        /// </summary>
        private void GetMfgHolesFromApi(int nodeIndex, out List<HoleInfo> holes, out List<SlotHoleInfo> slots)
        {
            holes = new List<HoleInfo>();
            slots = new List<SlotHoleInfo>();
            try
            {
                var nodeHoles = vizcore3d.GeometryUtility.GetNodeHoleInfo(nodeIndex);
                if (nodeHoles == null) return;
                foreach (var nh in nodeHoles)
                {
                    if (nh == null) continue;
                    if (nh.Center == null)
                    {
                        DiagLog($"[홀API] node={nodeIndex} WARN Center=null type={nh.HoleType}");
                        continue;
                    }

                    // NodeHoleItem 실제 타입 (빌드 역추론): Center=Vector3D, CircleCenter=List<Vector3D>, Size=Vector3D, Radius=float
                    var ccPts = nh.CircleCenter;
                    int ccN = ccPts?.Count ?? 0;
                    TryResolveMfgHoleThroughAxis(nh, out var throughAxis, out string throughSource,
                        out string throughValidation);

                    // [실측 로그] 실제 구조 확인용 — 슬롯 매핑은 이 로그를 보고 보정 예정
                    string ccStr = (ccN > 0)
                        ? string.Join(" ", ccPts.Where(p => p != null).Select(p => $"({p.X:F1},{p.Y:F1},{p.Z:F1})"))
                        : "-";
                    DiagLog($"[홀API] node={nodeIndex} type={nh.HoleType} Radius={nh.Radius:F2} " +
                        $"Center=({nh.Center.X:F1},{nh.Center.Y:F1},{nh.Center.Z:F1}) " +
                        $"Size={FormatMfgVector(nh.Size)} " +
                        $"AxisX={FormatMfgVector(nh.AxisX)} AxisY={FormatMfgVector(nh.AxisY)} " +
                        $"AxisZ={FormatMfgVector(nh.AxisZ)} " +
                        $"ThicknessFrom={FormatMfgVector(nh.ThicknessCenterFrom)} " +
                        $"ThicknessTo={FormatMfgVector(nh.ThicknessCenterTo)} " +
                        $"CircleCenterN={ccN}[{ccStr}] axisSource={throughSource} {throughValidation}");

                    if (nh.HoleType == VIZCore3D.NET.Data.NodeHoleItem.NodeHoleType.CIRCLE)
                    {
                        holes.Add(new HoleInfo
                        {
                            Diameter = nh.Radius * 2f,
                            CenterX = nh.Center.X,
                            CenterY = nh.Center.Y,
                            CenterZ = nh.Center.Z,
                            CylinderBodyIndex = nh.NodeIndex,
                            ThroughAxis = throughAxis,
                            ThroughAxisSource = throughSource
                        });
                    }
                    else if (nh.HoleType == VIZCore3D.NET.Data.NodeHoleItem.NodeHoleType.SLOT_HOLE)
                    {
                        if (nh.Size == null)
                        {
                            DiagLog($"[홀API] node={nodeIndex} WARN SLOT_HOLE Size=null");
                            continue;
                        }

                        // 잠정 매핑(실측 후 보정): 중심=Center, 길이=Size 최대축, 폭=Size 최소축
                        float ax = Math.Abs(nh.Size.X), ay = Math.Abs(nh.Size.Y), az = Math.Abs(nh.Size.Z);
                        float slotLen = Math.Max(ax, Math.Max(ay, az));
                        float slotWidth = Math.Min(ax, Math.Min(ay, az));
                        slots.Add(new SlotHoleInfo
                        {
                            Radius = slotWidth / 2f,
                            SlotLength = slotLen,
                            Depth = 0f,           // 실측 후 ThicknessCenter*로 보정
                            CenterX = nh.Center.X,
                            CenterY = nh.Center.Y,
                            CenterZ = nh.Center.Z,
                            ThroughAxis = throughAxis,
                            ThroughAxisSource = throughSource
                        });
                    }
                }
                DiagLog($"[홀API] node={nodeIndex} 결과 holes={holes.Count} slots={slots.Count}");
            }
            catch (Exception ex)
            {
                DiagLog($"[홀API] ERROR node={nodeIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// NodeHoleItem의 두께 중심점 차이를 홀 관통 방향으로 우선 사용한다.
        /// 원형 홀은 두께 중심점이 없을 때 CircleCenter 중 가장 먼 두 점의 차이를 사용한다.
        /// SDK XML은 AxisZ의 의미를 관통축으로 보장하지 않으므로, AxisX×AxisY와 평행할 때만 폴백한다.
        /// 슬롯의 CircleCenter는 장축일 수 있고 Size는 방향 계약이 없어 관통축 추정에 사용하지 않는다.
        /// </summary>
        private bool TryResolveMfgHoleThroughAxis(
            VIZCore3D.NET.Data.NodeHoleItem hole,
            out VIZCore3D.NET.Data.Vector3D throughAxis,
            out string source,
            out string validation)
        {
            throughAxis = null;
            source = "unresolved";
            validation = "thick=- circle=- axisZ=- crossXY=-";
            if (hole == null) return false;

            VIZCore3D.NET.Data.Vector3D thicknessAxis = null;
            if (hole.ThicknessCenterFrom != null && hole.ThicknessCenterTo != null)
            {
                TryNormalizeMfgVector(
                    SubtractMfgVector(hole.ThicknessCenterTo, hole.ThicknessCenterFrom),
                    out thicknessAxis);
            }

            VIZCore3D.NET.Data.Vector3D circleAxis = null;
            if (hole.HoleType == VIZCore3D.NET.Data.NodeHoleItem.NodeHoleType.CIRCLE &&
                hole.CircleCenter != null)
            {
                double maxDistanceSquared = 0.0;
                VIZCore3D.NET.Data.Vector3D farthestDifference = null;
                var centers = hole.CircleCenter.Where(point => point != null).ToList();
                for (int i = 0; i < centers.Count - 1; i++)
                {
                    for (int j = i + 1; j < centers.Count; j++)
                    {
                        var difference = SubtractMfgVector(centers[j], centers[i]);
                        double distanceSquared =
                            (double)difference.X * difference.X +
                            (double)difference.Y * difference.Y +
                            (double)difference.Z * difference.Z;
                        if (distanceSquared > maxDistanceSquared)
                        {
                            maxDistanceSquared = distanceSquared;
                            farthestDifference = difference;
                        }
                    }
                }
                TryNormalizeMfgVector(farthestDifference, out circleAxis);
            }

            TryNormalizeMfgVector(hole.AxisZ, out var axisZ);
            VIZCore3D.NET.Data.Vector3D crossXY = null;
            if (hole.AxisX != null && hole.AxisY != null)
                TryNormalizeMfgVector(CrossMfgVector(hole.AxisX, hole.AxisY), out crossXY);

            string thickText = FormatMfgVector(thicknessAxis);
            string circleText = FormatMfgVector(circleAxis);
            string axisZText = FormatMfgVector(axisZ);
            string crossText = FormatMfgVector(crossXY);
            string agreement = "-";
            if (thicknessAxis != null && axisZ != null)
                agreement = Math.Abs(DotMfgVector(thicknessAxis, axisZ)).ToString("F3");

            validation = $"thick={thickText} circle={circleText} axisZ={axisZText} " +
                         $"crossXY={crossText} thickAxisZ={agreement}";

            if (thicknessAxis != null)
            {
                throughAxis = thicknessAxis;
                source = "thickness";
                return true;
            }

            if (circleAxis != null)
            {
                throughAxis = circleAxis;
                source = "circle-center";
                return true;
            }

            if (axisZ != null && crossXY != null &&
                Math.Abs(DotMfgVector(axisZ, crossXY)) >= 0.95f)
            {
                throughAxis = axisZ;
                source = "axisZ-confirmed";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 벡터를 받아 → 소수 3자리 "(x,y,z)" 진단 로그용 문자열을 돌려준다. null이면 "-".
        /// </summary>
        private string FormatMfgVector(VIZCore3D.NET.Data.Vector3D vector)
        {
            return vector == null
                ? "-"
                : $"({vector.X:F3},{vector.Y:F3},{vector.Z:F3})";
        }

        /// <summary>
        /// pose와 뷰 방향(X/Y/Z)을 받아 → 그 뷰의 시선(깊이) 축 단위벡터를 돌려준다. 정규화 실패 시 null.
        /// 참조축을 쓰는 pose면 로컬 참조축을, 아니면 월드축을 쓴다.
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D GetMfgViewDepthAxis(MfgViewPose pose, string viewDirection)
        {
            var localX = new VIZCore3D.NET.Data.Vector3D(1f, 0f, 0f);
            var localY = new VIZCore3D.NET.Data.Vector3D(0f, 1f, 0f);
            var localZ = new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f);
            if (pose != null && pose.UseReferenceAxis &&
                pose.ReferenceAxisX != null && pose.ReferenceAxisY != null && pose.ReferenceAxisZ != null)
            {
                localX = pose.ReferenceAxisX;
                localY = pose.ReferenceAxisY;
                localZ = pose.ReferenceAxisZ;
            }

            VIZCore3D.NET.Data.Vector3D depth;
            switch (viewDirection)
            {
                case "X": depth = localX; break;
                case "Y": depth = localY; break;
                default: depth = localZ; break;
            }

            return TryNormalizeMfgVector(depth, out var normalized) ? normalized : null;
        }

        /// <summary>
        /// 1차 pose를 받아 → EA 2차 뷰 방향을 돌려준다. 최장축이 Z면 "X", 그 외는 "Z".
        /// </summary>
        private string GetMfgEaSecondaryViewDirection(MfgViewPose primaryPose)
        {
            return primaryPose != null && primaryPose.LongestAxis == "Z" ? "X" : "Z";
        }

        /// <summary>
        /// 홀·슬롯홀의 관통축과 중심을 받아 → 1차·2차 뷰 깊이축 중 어느 쪽에 더 나란한지로 풍선을 2차 뷰에 둘지 판정한다. 2차면 true.
        /// EA 부재가 아니거나 관통축·깊이축이 없으면 항상 1차(false). 판정 근거를 진단 로그에 남긴다.
        /// </summary>
        private bool AssignMfgFeatureToSecondary(
            BOMData bom,
            MfgViewPose pose,
            bool isEA,
            string featureKind,
            VIZCore3D.NET.Data.Vector3D throughAxis,
            string axisSource,
            VIZCore3D.NET.Data.Vertex3D center)
        {
            if (!isEA || pose == null || throughAxis == null)
            {
                DiagLog($"[MfgBalloonAssign] bom={bom.Index} kind={featureKind} axisSource={axisSource ?? "unresolved"} " +
                        "dot1=- dot2=- view=1");
                return false;
            }

            var primaryDepth = GetMfgViewDepthAxis(pose, pose.ViewDirection);
            string secondaryView = GetMfgEaSecondaryViewDirection(pose);
            var secondaryDepth = GetMfgViewDepthAxis(pose, secondaryView);
            if (primaryDepth == null || secondaryDepth == null)
            {
                DiagLog($"[MfgBalloonAssign] bom={bom.Index} kind={featureKind} axisSource={axisSource ?? "unresolved"} " +
                        $"primary={pose.ViewDirection} secondary={secondaryView} depthInvalid view=1");
                return false;
            }

            float dotPrimary = Math.Abs(DotMfgVector(throughAxis, primaryDepth));
            float dotSecondary = Math.Abs(DotMfgVector(throughAxis, secondaryDepth));
            bool useSecondary = dotSecondary > dotPrimary + 1e-4f;
            DiagLog($"[MfgBalloonAssign] bom={bom.Index} kind={featureKind} " +
                    $"center=({center.X:F1},{center.Y:F1},{center.Z:F1}) axisSource={axisSource ?? "unresolved"} " +
                    $"primary={pose.ViewDirection}:{dotPrimary:F3} secondary={secondaryView}:{dotSecondary:F3} " +
                    $"view={(useSecondary ? 2 : 1)}");
            return useSecondary;
        }

        /// <summary>
        /// 홀 목록과 대상 풍선 목록을 받아 → 지름(소수 1자리)별로 묶어 "Ø지름" 풍선을 추가한다. 2개 이상이면 "* N개"를 붙인다.
        /// 풍선 색은 녹색, 지시선은 그룹 첫 홀 중심. 입력이 null이면 아무것도 안 한다.
        /// </summary>
        private void AddMfgGroupedHoleNotes(
            IEnumerable<HoleInfo> holes,
            IList<MfgPendingNote> destination)
        {
            if (holes == null || destination == null) return;
            foreach (var group in holes.GroupBy(h => Math.Round(h.Diameter, 1)))
            {
                var hole = group.First();
                int count = group.Count();
                destination.Add(new MfgPendingNote
                {
                    Text = count > 1 ? $"\u00d8{group.Key:F1} * {count}개" : $"\u00d8{group.Key:F1}",
                    Color = Color.FromArgb(0, 160, 0),
                    ArrowPosition = new VIZCore3D.NET.Data.Vertex3D(
                        hole.CenterX, hole.CenterY, hole.CenterZ)
                });
            }
        }

        /// <summary>
        /// 슬롯홀 목록과 대상 풍선 목록을 받아 → 반지름·길이·깊이가 같은 것끼리 묶어 "R반지름/(폭*길이*깊이)" 풍선을 추가한다.
        /// 2개 이상이면 "* N개"를 붙이고, 폭은 반지름×2로 계산한다. 풍선 색은 보라, 지시선은 그룹 첫 슬롯 중심.
        /// 입력이 null이면 아무것도 안 한다.
        /// </summary>
        private void AddMfgGroupedSlotNotes(
            IEnumerable<SlotHoleInfo> slots,
            IList<MfgPendingNote> destination)
        {
            if (slots == null || destination == null) return;
            foreach (var group in slots.GroupBy(s =>
                $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}"))
            {
                var slot = group.First();
                int count = group.Count();
                float width = slot.Radius * 2f;
                destination.Add(new MfgPendingNote
                {
                    Text = count > 1
                        ? $"R{slot.Radius:F1}/({width:F0}*{slot.SlotLength:F0}*{slot.Depth:F0}) * {count}개"
                        : $"R{slot.Radius:F1}/({width:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})",
                    Color = Color.FromArgb(180, 0, 180),
                    ArrowPosition = new VIZCore3D.NET.Data.Vertex3D(
                        slot.CenterX, slot.CenterY, slot.CenterZ)
                });
            }
        }

        /// <summary>
        /// 부재·pose·홀·슬롯홀 목록을 받아 → 형상별로 1차/2차 뷰를 배정하고 묶음 풍선을 PendingNotes·SecondaryPendingNotes에 채운다.
        /// 장면 코어가 Osnap 수집 뒤에 호출. 기존 목록은 비우고 시작한다.
        /// 용도가 EBOS인 부재는 1차 뷰에만 "EarthBoss" 풍선을 부재 중심에 한 번 추가한다.
        /// </summary>
        private void BuildMfgPendingNotes(
            BOMData bom,
            MfgViewPose pose,
            bool isEA,
            IEnumerable<HoleInfo> holes,
            IEnumerable<SlotHoleInfo> slots)
        {
            if (bom == null || pose == null) return;
            pose.PendingNotes.Clear();
            pose.SecondaryPendingNotes.Clear();

            var primaryHoles = new List<HoleInfo>();
            var secondaryHoles = new List<HoleInfo>();
            foreach (var hole in holes ?? Enumerable.Empty<HoleInfo>())
            {
                var center = new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, hole.CenterY, hole.CenterZ);
                if (AssignMfgFeatureToSecondary(bom, pose, isEA, "Hole",
                    hole.ThroughAxis, hole.ThroughAxisSource, center))
                    secondaryHoles.Add(hole);
                else
                    primaryHoles.Add(hole);
            }

            var primarySlots = new List<SlotHoleInfo>();
            var secondarySlots = new List<SlotHoleInfo>();
            foreach (var slot in slots ?? Enumerable.Empty<SlotHoleInfo>())
            {
                var center = new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, slot.CenterY, slot.CenterZ);
                if (AssignMfgFeatureToSecondary(bom, pose, isEA, "SlotHole",
                    slot.ThroughAxis, slot.ThroughAxisSource, center))
                    secondarySlots.Add(slot);
                else
                    primarySlots.Add(slot);
            }

            AddMfgGroupedHoleNotes(primaryHoles, pose.PendingNotes);
            AddMfgGroupedSlotNotes(primarySlots, pose.PendingNotes);
            AddMfgGroupedHoleNotes(secondaryHoles, pose.SecondaryPendingNotes);
            AddMfgGroupedSlotNotes(secondarySlots, pose.SecondaryPendingNotes);

            // EarthBoss는 국소 형상이 아닌 부재 전체 용도 라벨이므로 부재당 한 번, 첫 번째 뷰에만 표시한다.
            if (string.Equals((bom.Purpose ?? "").Trim(), "EBOS", StringComparison.OrdinalIgnoreCase))
            {
                pose.PendingNotes.Add(new MfgPendingNote
                {
                    Text = "EarthBoss",
                    Color = Color.Blue,
                    ArrowPosition = new VIZCore3D.NET.Data.Vertex3D(
                        bom.CenterX, bom.CenterY, bom.CenterZ)
                });
            }

            DiagLog($"[MfgBalloonAssign] bom={bom.Index} isEA={isEA} " +
                    $"primaryNotes={pose.PendingNotes.Count} secondaryNotes={pose.SecondaryPendingNotes.Count} " +
                    $"primaryHoles={primaryHoles.Count} secondaryHoles={secondaryHoles.Count} " +
                    $"primarySlots={primarySlots.Count} secondarySlots={secondarySlots.Count}");
        }

        /// <summary>
        /// 가공도 공통 3D 장면 생성 코어.
        /// 미리보기와 PDF 행 렌더링이 공통으로 사용한다.
        /// 부재 격리·BBox·축 판별·카메라·Osnap·치수와 뷰별 형상 풍선 후보를 수집한다.
        /// ISO 부재번호 풍선과 원형 부재 반지름 풍선은 생성하지 않는다.
        /// 반환: MfgViewPose — 카메라 회전 의도(ApplyZ90/R180), 방향, 최장축 등 후속 적용 정보.
        ///
        /// 호출자별 후속 처리:
        ///   - ExecuteMfgDrawing (수동, 3D 뷰 유지): pose를 _lastMfgViewPose에 저장.
        ///     LvDrawingSheet_SelectedIndexChanged 후처리 회전이 참조.
        ///   - RenderMfgRowToViewArea (PDF): EA면 최장축 치수를 두 번째 뷰에 예약한다.
        /// </summary>
        private MfgViewPose BuildMfgSceneCore(
            int bomIndex,
            bool reserveLongestAxisForSecondary = false)
        {
            var pose = new MfgViewPose();

            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return pose;

            // ReferenceAxis는 Review.Measure.Clear에 함께 삭제되므로 모든 측정 초기화를 축 생성 전에 끝낸다.
            ClearMfgViewAnnotations("BuildMfgSceneCore");

            // ── 1. 부재 격리: 전체 숨김 → 해당 bom만 Show ──
            //   BeginUpdate/EndUpdate는 호출자(어댑터)가 자기 범위로 관리.
            vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
            List<int> targetIndices = new List<int> { bom.Index };
            vizcore3d.Object3D.Show(targetIndices, true);

            // ── 2. BBox + 최장축 판별 ──
            float sizeX = bom.MaxX - bom.MinX;
            float sizeY = bom.MaxY - bom.MinY;
            float sizeZ = bom.MaxZ - bom.MinZ;

            if (sizeX >= sizeY && sizeX >= sizeZ) pose.LongestAxis = "X";
            else if (sizeY >= sizeX && sizeY >= sizeZ) pose.LongestAxis = "Y";
            else pose.LongestAxis = "Z";

            // ── 3. 카메라 방향 결정 (PAD/PLATE 분기) ──
            bool isPadOrPlate = IsPadOrPlateFromSpref(bom.Index);
            string viewDirection;
            VIZCore3D.NET.Data.CameraDirection cameraDir;

            if (isPadOrPlate)
            {
                // PAD/PLATE: 최단축 방향 (평판을 정면에서 봄)
                string shortestAxis;
                if (sizeX <= sizeY && sizeX <= sizeZ) shortestAxis = "X";
                else if (sizeY <= sizeX && sizeY <= sizeZ) shortestAxis = "Y";
                else shortestAxis = "Z";

                switch (shortestAxis)
                {
                    case "X": viewDirection = "X"; cameraDir = VIZCore3D.NET.Data.CameraDirection.X_PLUS; break;
                    case "Y": viewDirection = "Y"; cameraDir = VIZCore3D.NET.Data.CameraDirection.Y_PLUS; break;
                    default:  viewDirection = "Z"; cameraDir = VIZCore3D.NET.Data.CameraDirection.Z_PLUS; break;
                }
            }
            else
            {
                // 일반: 최장축이 수평으로 보이는 방향
                switch (pose.LongestAxis)
                {
                    case "Y": viewDirection = "X"; cameraDir = VIZCore3D.NET.Data.CameraDirection.X_PLUS; break;
                    default:  viewDirection = "Y"; cameraDir = VIZCore3D.NET.Data.CameraDirection.Y_PLUS; break;
                }
            }

            vizcore3d.View.MoveCamera(cameraDir);
            pose.ViewDirection = viewDirection;
            pose.CameraDirection = cameraDir;
            pose.UsedMinusCamera = false;

            // ── 4. ORIENTATION UDA 기반 로컬 참조축 카메라 ──
            var (orientAxis_saved, orientAngle_saved) = ParseOrientation(bom.Index);
            pose.OrientationAxis = orientAxis_saved;
            pose.OrientationAngle = orientAngle_saved;
            if (TryBuildMfgOrientationReferenceFrame(bom, pose))
            {
                // 화면 roll이 아니라 활성 참조축 기준의 CameraDirection으로 시선 자체를 로컬축에 정렬한다.
                ActivateMfgReferenceAxis(pose, bom, "primary");
            }
            else
            {
                // 정상 부재와 ORIENTATION 파싱 불가 부재는 기존 카메라 동작을 그대로 유지한다.
                ApplyOrientationRotation(bom.Index, viewDirection);
            }

            // Z 최장축 시 90° 회전 결정 (실제 적용은 어댑터)
            //   ExecuteMfgDrawing에서 pose.LongestAxis=="Z"이면 RotateCameraByScreenAxis(0,0,90) (PDF는 ProbeAndRollLandscape가 실측 회전).
            //   B1b1a는 결정만, 어댑터가 적용.
            pose.ApplyZ90 = (pose.LongestAxis == "Z");

            // ── 5. Osnap 수집 (LINE/POINT만, CIRCLE 제외 — T-064 사양) ──
            //   B1b1b 추가 (2026-05-19): 자동 함수 본체 L1004~L1027 추출.
            //   [캐시 2026-07-22] 도면 리스트 뽑기 때 채운 부재별 Osnap 맵(T-032 E1) 재사용 — GetOsnapPoint가
            //   미리보기 클릭당 ~0.8s를 먹는 병목이라 hit면 생략. 맵도 CollectAllOsnap에서 LINE 양끝+POINT만
            //   담고 CIRCLE 제외라 결과 동일. 점은 복사해 캐시 원본 오염 방지, 이름은 bom.Name 재태깅.
            //   miss(맵 미수집·부재 없음)면 기존 GetOsnapPoint 직행 — 안전 폴백.
            var mfgOsnapWithNames = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
            List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> cachedOsnap = null;
            bool osnapCacheHit = _lastCollectedNodeOsnapMap != null &&
                                 _lastCollectedNodeOsnapMap.TryGetValue(bom.Index, out cachedOsnap) &&
                                 cachedOsnap.Count > 0;
            if (osnapCacheHit)
            {
                foreach (var (pt, _) in cachedOsnap)
                    mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(pt.X, pt.Y, pt.Z), bom.Name));
                DiagLog($"[Osnap] bom={bom.Index} name='{bom.Name}' 캐시 hit {mfgOsnapWithNames.Count}점 (GetOsnapPoint 생략)");
            }
            else
            {
                var osnapListMfg = CacheMfgAxisDetection(
                    bom.Index,
                    vizcore3d.Object3D.GetOsnapPoint(bom.Index));

                // P3 #3 진단 (2026-05-23): Osnap 수집 결과 추적 — 외곽 끝점이 잡혔는지 확인
                int rawLineCount = 0, rawPointCount = 0, rawCircleCount = 0;
                if (osnapListMfg != null)
                {
                    foreach (var o in osnapListMfg)
                    {
                        switch (o.Kind)
                        {
                            case VIZCore3D.NET.Data.OsnapKind.LINE: rawLineCount++; break;
                            case VIZCore3D.NET.Data.OsnapKind.POINT: rawPointCount++; break;
                            case VIZCore3D.NET.Data.OsnapKind.CIRCLE: rawCircleCount++; break;
                        }
                    }
                }
                DiagLog($"[Osnap] bom={bom.Index} name='{bom.Name}' BBox X[{bom.MinX:F2}~{bom.MaxX:F2}] Y[{bom.MinY:F2}~{bom.MaxY:F2}] Z[{bom.MinZ:F2}~{bom.MaxZ:F2}] " +
                    $"sizeX={bom.MaxX - bom.MinX:F2} sizeY={bom.MaxY - bom.MinY:F2} sizeZ={bom.MaxZ - bom.MinZ:F2} " +
                    $"rawOsnap LINE={rawLineCount} POINT={rawPointCount} CIRCLE={rawCircleCount} (캐시 miss)");

                if (osnapListMfg != null)
                {
                    foreach (var osnap in osnapListMfg)
                    {
                        switch (osnap.Kind)
                        {
                            case VIZCore3D.NET.Data.OsnapKind.LINE:
                                if (osnap.Start != null)
                                    mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z), bom.Name));
                                if (osnap.End != null)
                                    mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z), bom.Name));
                                break;
                            case VIZCore3D.NET.Data.OsnapKind.POINT:
                                if (osnap.Center != null)
                                    mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z), bom.Name));
                                break;
                            // T-064 (2026-05-14): CIRCLE Osnap 치수 추출 제외 (홀 과다 치수 회피)
                            case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                break;
                        }
                    }
                }
            }

            // 참조축 자동 정렬 1단계: 실제 카메라는 바꾸지 않고 LINE Osnap 기반 판정만 기록한다.
            LogMfgAxisDetection(bom);

            // 수집된 Osnap 점 좌표 로그 (X/Y/Z 별로 min/max 추적)
            if (mfgOsnapWithNames.Count > 0)
            {
                float minOX = float.MaxValue, maxOX = float.MinValue;
                float minOY = float.MaxValue, maxOY = float.MinValue;
                float minOZ = float.MaxValue, maxOZ = float.MinValue;
                foreach (var (pt, _) in mfgOsnapWithNames)
                {
                    if (pt.X < minOX) minOX = pt.X; if (pt.X > maxOX) maxOX = pt.X;
                    if (pt.Y < minOY) minOY = pt.Y; if (pt.Y > maxOY) maxOY = pt.Y;
                    if (pt.Z < minOZ) minOZ = pt.Z; if (pt.Z > maxOZ) maxOZ = pt.Z;
                }
                DiagLog($"[Osnap] bom={bom.Index} 수집 {mfgOsnapWithNames.Count}점 range X[{minOX:F2}~{maxOX:F2}] Y[{minOY:F2}~{maxOY:F2}] Z[{minOZ:F2}~{maxOZ:F2}] " +
                    $"BBox와 비교: X span {maxOX - minOX:F2}/{bom.MaxX - bom.MinX:F2} Y span {maxOY - minOY:F2}/{bom.MaxY - bom.MinY:F2} Z span {maxOZ - minOZ:F2}/{bom.MaxZ - bom.MinZ:F2}");
            }
            else
            {
                DiagLog($"[Osnap] bom={bom.Index} 수집 0점 (외곽 치수 불가)");
            }

            // ── 5-1. EA 앵글 카메라 보정 ──
            bool isEA = IsAngleFromSpref(bom.Index);
            bool isAboveWider = false;
            bool isMinusCameraSelected = false;
            bool isEAUse180 = false;

            if (isEA && mfgOsnapWithNames.Count > 0)
            {
                float bbCenterH = 0f, bbCenterV = 0f;
                float sumH = 0f, sumV = 0f;
                foreach (var pt in mfgOsnapWithNames)
                {
                    switch (viewDirection)
                    {
                        case "X": sumH += pt.point.Y; sumV += pt.point.Z; break;
                        case "Y": sumH += pt.point.X; sumV += pt.point.Z; break;
                        default:  sumH += pt.point.X; sumV += pt.point.Y; break;
                    }
                }
                float centroidH = sumH / mfgOsnapWithNames.Count;
                float centroidV = sumV / mfgOsnapWithNames.Count;
                switch (viewDirection)
                {
                    case "X": bbCenterH = (bom.MinY + bom.MaxY) / 2f; bbCenterV = (bom.MinZ + bom.MaxZ) / 2f; break;
                    case "Y": bbCenterH = (bom.MinX + bom.MaxX) / 2f; bbCenterV = (bom.MinZ + bom.MaxZ) / 2f; break;
                    default:  bbCenterH = (bom.MinX + bom.MaxX) / 2f; bbCenterV = (bom.MinY + bom.MaxY) / 2f; break;
                }

                float openH = bbCenterH - centroidH;
                float openV = bbCenterV - centroidV;
                bool use180 = (openV > 0);

                bool useMinus;
                if (viewDirection == "Y")
                {
                    bool needRight = use180 ? (openH < 0) : (openH > 0);
                    useMinus = !needRight;
                }
                else
                {
                    bool needRight = use180 ? (openH > 0) : (openH < 0);
                    useMinus = !needRight;
                }

                isMinusCameraSelected = useMinus;

                if (useMinus)
                {
                    switch (viewDirection)
                    {
                        case "X": pose.CameraDirection = VIZCore3D.NET.Data.CameraDirection.X_MINUS; break;
                        case "Y": pose.CameraDirection = VIZCore3D.NET.Data.CameraDirection.Y_MINUS; break;
                        default:  pose.CameraDirection = VIZCore3D.NET.Data.CameraDirection.Z_MINUS; break;
                    }
                    vizcore3d.View.MoveCamera(pose.CameraDirection);
                    if (!pose.UseReferenceAxis)
                        ApplyOrientationRotation(bom.Index, viewDirection);
                }

                isEAUse180 = use180;
                isAboveWider = false;
            }

            // 은선 필터 '전' 원본 보존 — 보조선 시작점 레벨 스냅(SnapDimPointTowardDimLine)용.
            //   대각(beveled) 꼭짓점은 깊이 필터(FilterHiddenLineOsnap)가 뒷면 플랜지째로 지울 수 있는데,
            //   실루엣 외곽으로는 도면에 그려지므로 스냅은 원본에서 찾는다(정사영이라 깊이는 2D 위치 무관). 2026-07-02
            var mfgOsnapFull = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>(mfgOsnapWithNames);

            // ── 5-2a. EA 접힘 모서리(코너) 방향 판정 — 두 뷰 상하 스왑용 (2026-07-02) ──
            //   1차 뷰 높이축(h)의 min/max 슬랩 중 '깊이축으로 두껍게 퍼진 쪽'(=반대 플랜지가 있는 쪽)이 코너.
            //   자유단 쪽은 깊이 방향으로 판 두께(~8mm)뿐이라 극명하게 갈린다.
            if (isEA && !string.IsNullOrEmpty(pose.LongestAxis))
            {
                string hAx = GetRemainingAxis(viewDirection, pose.LongestAxis);
                Func<VIZCore3D.NET.Data.Vertex3D, string, float> cAx =
                    (v, a) => a == "X" ? v.X : a == "Y" ? v.Y : v.Z;
                float hMin = float.MaxValue, hMax = float.MinValue;
                foreach (var o in mfgOsnapFull)
                {
                    float hv = cAx(o.point, hAx);
                    if (hv < hMin) hMin = hv;
                    if (hv > hMax) hMax = hv;
                }
                float band = (hMax - hMin) * 0.3f;
                if (band > 0.5f)
                {
                    float minLo = float.MaxValue, minHi = float.MinValue;
                    float maxLo = float.MaxValue, maxHi = float.MinValue;
                    foreach (var o in mfgOsnapFull)
                    {
                        float hv = cAx(o.point, hAx);
                        float dv = cAx(o.point, viewDirection);
                        if (hv <= hMin + band) { if (dv < minLo) minLo = dv; if (dv > minHi) minHi = dv; }
                        if (hv >= hMax - band) { if (dv < maxLo) maxLo = dv; if (dv > maxHi) maxHi = dv; }
                    }
                    float extMin = (minHi >= minLo) ? minHi - minLo : 0f;
                    float extMax = (maxHi >= maxLo) ? maxHi - maxLo : 0f;
                    pose.CornerAxis = hAx;
                    pose.CornerAtMax = extMax > extMin;
                    pose.HasCorner = Math.Abs(extMax - extMin) > 1.0f;
                    DiagLog($"[Corner] bom={bom.Index} hAx={hAx} extMin={extMin:F1} extMax={extMax:F1} " +
                        $"atMax={pose.CornerAtMax} has={pose.HasCorner}");
                }

                // 2차 뷰 높이축(=1차 뷰 깊이축=viewDirection) 기준 동일 판정 — 2차 뷰 상하 미러 결정용.
                {
                    string sAx = viewDirection;
                    float sMinV = float.MaxValue, sMaxV = float.MinValue;
                    foreach (var o in mfgOsnapFull)
                    {
                        float sv = cAx(o.point, sAx);
                        if (sv < sMinV) sMinV = sv;
                        if (sv > sMaxV) sMaxV = sv;
                    }
                    float sBand = (sMaxV - sMinV) * 0.3f;
                    if (sBand > 0.5f)
                    {
                        string dAx = GetRemainingAxis(viewDirection, pose.LongestAxis);   // 판별축 = 1차 높이축
                        float sMinLo = float.MaxValue, sMinHi = float.MinValue;
                        float sMaxLo = float.MaxValue, sMaxHi = float.MinValue;
                        foreach (var o in mfgOsnapFull)
                        {
                            float sv = cAx(o.point, sAx);
                            float dv = cAx(o.point, dAx);
                            if (sv <= sMinV + sBand) { if (dv < sMinLo) sMinLo = dv; if (dv > sMinHi) sMinHi = dv; }
                            if (sv >= sMaxV - sBand) { if (dv < sMaxLo) sMaxLo = dv; if (dv > sMaxHi) sMaxHi = dv; }
                        }
                        float sExtMin = (sMinHi >= sMinLo) ? sMinHi - sMinLo : 0f;
                        float sExtMax = (sMaxHi >= sMaxLo) ? sMaxHi - sMaxLo : 0f;
                        pose.SecCornerAxis = sAx;
                        pose.SecCornerAtMax = sExtMax > sExtMin;
                        pose.HasSecCorner = Math.Abs(sExtMax - sExtMin) > 1.0f;
                        DiagLog($"[Corner2] bom={bom.Index} sAx={sAx} extMin={sExtMin:F1} extMax={sExtMax:F1} " +
                            $"atMax={pose.SecCornerAtMax} has={pose.HasSecCorner}");
                    }
                }

                // ── 상하 스왑 판정 (코어에서 — 2026-07-02) ──
                //   캔버스 Y는 위로 증가: 1차 뷰 기본 슬롯=아래. 아래 뷰 코너는 위(가운데)를 향해야 함.
                //   치수 수집 전에 확정해야 '길이 치수를 위 슬롯 뷰에 배치'(사용자 사양)가 가능.
                if (pose.HasCorner)
                {
                    pose.CornerAxisUp = MfgAxisUpPositive(pose.CornerAxis);
                    bool cornerUpP = (pose.CornerAtMax == pose.CornerAxisUp);
                    pose.SwapViews = !cornerUpP;
                    DiagLog($"[EASlot] bom={bom.Index} cornerAxis={pose.CornerAxis} atMax={pose.CornerAtMax} " +
                        $"axisUp={pose.CornerAxisUp} cornerUp={cornerUpP} → swap={pose.SwapViews} (1차 기본=아래)");
                }
            }

            // ── 5-2. 은선 Osnap 필터링 (카메라 방향 결정 후 적용) ──
            mfgOsnapWithNames = FilterHiddenLineOsnap(mfgOsnapWithNames, viewDirection,
                bom.MinX, bom.MaxX, bom.MinY, bom.MaxY, bom.MinZ, bom.MaxZ, isMinusCameraSelected);

            // ── 5-3. 가공도 osnap 4점 선별 — 제작도와 동일 알고리즘(FilterOsnapForDimAxis) ──
            //   (사용자 사양 2026-06-23) 보는 뷰에서 가로축 max/min + 세로축 max/min 4 극점만 남기고
            //   중복·깊이 겹침 제거. 제작도와 통일 → EA 중간 station 폭주·치수 겹침 원천 제거.
            //   (홀 중앙점 등은 추후 이 집합에 더하기만 하면 됨)
            {
                var mfgOsnapMap = new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>>
                    { { bom.Index, mfgOsnapWithNames } };
                var visAxes4 = new List<string>();
                switch (viewDirection)
                {
                    case "X": visAxes4.Add("Y"); visAxes4.Add("Z"); break;
                    case "Y": visAxes4.Add("X"); visAxes4.Add("Z"); break;
                    default:  visAxes4.Add("X"); visAxes4.Add("Y"); break;
                }
                var fourPt = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
                foreach (var ax in visAxes4)
                    fourPt.AddRange(FilterOsnapForDimAxis(mfgOsnapMap, ax, viewDirection, 0.5f));
                mfgOsnapWithNames = fourPt;
            }

            // pose 갱신 — EA 분기 결과 반영
            pose.UsedMinusCamera = isMinusCameraSelected;
            pose.ApplyR180 = isEAUse180;

            // ── 6. 치수 계산 (MergeCoordinates + AddChainDimensionByAxis) ──
            //   B1b1c (2026-05-19): 자동 본체 L1217~L1369 추출.
            //   색상 통일 (사용자 사양 2026-05-19): Cyan → Blue (수동 스타일 채택).
            bool hasDimensions = mfgOsnapWithNames.Count > 0;
            var mfgDimensions = new List<ChainDimensionData>();
            // shapeDrawingIds → pose.ShapeDrawingIds (B3, 2026-05-19)

            if (hasDimensions)
            {
                float tolerance = 0.5f;
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(mfgOsnapWithNames, tolerance);

                List<string> mfgVisibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": mfgVisibleAxes.Add("Y"); mfgVisibleAxes.Add("Z"); break;
                    case "Y": mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Z"); break;
                    default:  mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Y"); break;
                }

                foreach (var ax in mfgVisibleAxes)
                    mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, ax, tolerance, viewDirection));

                // 치수 후처리 — osnap은 위 5-3에서 이미 4점으로 선별되어 치수가 최소(전체 폭·높이 수준).
                //   FilterMfgDimensions(축당 8개·겹침 회피)는 안전망(총치수 표시·중복 정리)으로 유지.
                mfgDimensions = FilterMfgDimensions(mfgDimensions);

                if (mfgDimensions.Count > 0)
                {
                    // 7. 치수 그리기 (수동 스타일 Blue, 사용자 사양 2026-05-19)
                    VIZCore3D.NET.Data.MeasureStyle mfgStyle = vizcore3d.Review.Measure.GetStyle();
                    mfgStyle.Prefix = false;
                    mfgStyle.Unit = false;
                    mfgStyle.NumberOfDecimalPlaces = 0;
                    mfgStyle.DX_DY_DZ = false;
                    mfgStyle.Frame = false;
                    mfgStyle.ContinuousDistance = false;
                    mfgStyle.BackgroundTransparent = true;
                    mfgStyle.FontColor = System.Drawing.Color.Blue;       // ← 통일 Blue (옛 Cyan)
                    mfgStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                    mfgStyle.FontBold = true;
                    mfgStyle.LineColor = System.Drawing.Color.Blue;       // ← 통일 Blue
                    mfgStyle.LineWidth = 1;
                    mfgStyle.ArrowColor = System.Drawing.Color.Blue;      // ← 통일 Blue
                    mfgStyle.ArrowSize = 5;
                    mfgStyle.AssistantLine = false;
                    mfgStyle.AlignDistanceText = true;   // 제작도와 동일 자동 정렬 (가로=위, 세로=왼쪽). 세로 텍스트 왼쪽이 회사 표준(정답)
                    mfgStyle.AlignDistanceTextMargine = MfgCanvasMeasureTextMargin;
                    vizcore3d.Review.Measure.SetStyle(mfgStyle);

                    float mfgGlobalMinX = bom.MinX, mfgGlobalMinY = bom.MinY, mfgGlobalMinZ = bom.MinZ;
                    float mfgGlobalMaxX = bom.MaxX, mfgGlobalMaxY = bom.MaxY, mfgGlobalMaxZ = bom.MaxZ;
                    float mfgCenterX = (mfgGlobalMinX + mfgGlobalMaxX) / 2f;
                    float mfgCenterY = (mfgGlobalMinY + mfgGlobalMaxY) / 2f;
                    float mfgCenterZ = (mfgGlobalMinZ + mfgGlobalMaxZ) / 2f;

                    // T-005: 중앙에서 가장 먼 Osnap 쪽이 외곽
                    var mfgAxisPosOff = new Dictionary<string, bool>();
                    foreach (var grp in mfgDimensions.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                    {
                        string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                        float centerVal = offAxis == "X" ? mfgCenterX : offAxis == "Y" ? mfgCenterY : mfgCenterZ;
                        var values = grp.SelectMany(d => new[]
                        {
                            GetAxisValue(d.StartPoint, offAxis),
                            GetAxisValue(d.EndPoint, offAxis)
                        });
                        mfgAxisPosOff[grp.Key] = ComputePositiveOffsetByOsnapExtreme(values, centerVal);
                    }

                    // EA 앵글: 체인치수 방향 강제 오버라이드
                    if (isEA)
                    {
                        // 길이 치수: 스왑(1차가 위 슬롯)이면 1차가 길이를 그리며 항상 화면 '위'(페어 바깥)로 —
                        //   posOff = (+길이오프셋축이 화면 위인가) = CornerAxisUp. 비스왑은 기존 휴리스틱. 2026-07-02
                        if (mfgAxisPosOff.ContainsKey(pose.LongestAxis))
                            mfgAxisPosOff[pose.LongestAxis] = pose.SwapViews ? pose.CornerAxisUp : !isAboveWider;
                        // 폭(세로) 치수를 화면 오른쪽으로 — 부재별 실제 카메라 투영으로 판정 (MfgHeightToRight)
                        foreach (string ax in new List<string>(mfgAxisPosOff.Keys))
                        {
                            if (ax != pose.LongestAxis)
                                mfgAxisPosOff[ax] = MfgHeightToRight(GetRemainingAxis(viewDirection, ax));
                        }
                    }

                    // 보조선·치수 '그리기'는 캡처 직후 확정된 실측 newScale로 수행(DrawMfgDimsAtScale).
                    //   추정 스케일(EstimateFitScaleForViewArea, BBox 기반)은 2D 은선 투영 실측과 달라
                    //   보조선 길이가 부재·뷰마다 어긋났음(설계 §4.4 v2-c). 여기선 그릴 목록만 수집(offset 미적용). 2026-07-01
                    foreach (var dim in mfgDimensions.Where(d => d.IsVisible))
                    {
                        // 길이 치수는 '위 슬롯 뷰'가 그린다 (사용자 사양 2026-07-02):
                        //   비스왑(2차가 위) → 1차는 길이 스킵(기존 예약). 스왑(1차가 위) → 1차가 길이 유지.
                        if (isEA && reserveLongestAxisForSecondary && !pose.SwapViews && dim.Axis == pose.LongestAxis) continue;
                        // 세로(폭) 치수는 1단만 — 전체 치수(2단)는 체인과 중복이라 생략 (사용자 사양 2026-07-03).
                        if (dim.IsTotal && dim.Axis != pose.LongestAxis) continue;
                        int lvl = dim.IsTotal ? 2 : (dim.DisplayLevel > 0 ? 1 : 0);
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        // 보조선 시작점 = 같은 레벨 osnap 중 치수선 쪽 극점 (대각 부재의 실제 꼭짓점 대응)
                        string offAxP = GetRemainingAxis(viewDirection, dim.Axis);
                        pose.PendingDims.Add(new MfgPendingDim
                        {
                            Start = SnapDimPointTowardDimLine(dim.StartPoint, dim.Axis, offAxP, posOff, mfgOsnapFull, 1.0f),
                            End   = SnapDimPointTowardDimLine(dim.EndPoint,   dim.Axis, offAxP, posOff, mfgOsnapFull, 1.0f),
                            Axis = dim.Axis, Level = lvl, PosOff = posOff
                        });
                    }
                }
            }

            // ── 8. 형상 풍선 후보를 뷰별로 독립 배정 ──
            // 홀/슬롯은 관통축과 각 뷰의 실제 깊이축을 비교한 뒤 뷰별로 그룹화한다.
            // Review Note 생성은 모델 캡처 후 newScale이 확정된 시점까지 지연한다.
            GetMfgHolesFromApi(bom.Index, out var mfgApiHoles, out var mfgApiSlots);
            BuildMfgPendingNotes(bom, pose, isEA, mfgApiHoles, mfgApiSlots);

            return pose;
        }

        /// <summary>
        /// 가공도 핵심 로직 (BOM Index를 받아서 가공도 출력)
        /// 시트 선택 3D 미리보기(LvDrawingSheet_SelectedIndexChanged)에서 사용.
        ///
        /// Step B2 (2026-05-19): 어댑터로 축소.
        /// 공통 3D 로직(부재 격리·카메라·ORIENTATION·Osnap·치수·풍선 정보 수집)은 BuildMfgSceneCore가 수행.
        /// 어댑터는 수동 전용 후처리만:
        ///   - X-Ray 끄기 + 선택 해제
        ///   - 코어 호출 → pose 받음
        ///   - 3D 뷰용 RenderMode = SMOOTH (코어는 미지정)
        ///   - SilhouetteEdge = Green
        ///   - EndUpdate 후 FitToView (카메라 fit)
        ///   - pose.ApplyZ90 / pose.ApplyR180 적용
        ///   - _lastMfgViewPose 저장 (시트 선택 후처리 회전이 참조)
        ///   - EndUpdate 후 카메라 스냅샷 (ScreenAxisRotation commit 후)
        ///
        /// 사용자 사양 (2026-07-22): 3D 미리보기에서는 형상 풍선을 제거한다.
        /// PDF 어댑터만 캡처 후 확정된 배율로 PendingNotes를 생성하므로 출력 풍선은 유지한다.
        /// </summary>
        private void ExecuteMfgDrawing(int bomIndex)
        {
            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return;

            MfgViewPose pose = null;
            bool shouldSnapshotCamera = false;

            // [MfgCam] 카메라 fit 격리 진단 (2026-07-22) — 도면번호 클릭 시 "엄청 가까이" 원인 특정용. 원인 확정 후 제거.
            //   entry(원복 후)→core(카메라 세팅 후)→fly(FlyTo 후)→rot(회전 후)→final(EndUpdate 후) 사이
            //   zoomRatio/depth/pivot이 어느 단계에서 무너지는지 본다. BeginUpdate 안 값은 commit 전일 수 있어 상대 변화로 판단.
            void CamLog(string tag)
            {
                try
                {
                    Func<VIZCore3D.NET.Data.Vertex3D, string> fmt =
                        v => v == null ? "null" : $"({v.X:F0},{v.Y:F0},{v.Z:F0})";
                    var cd = vizcore3d.View.GetCameraData();
                    string cam = cd == null
                        ? "cd=null"
                        : $"camZoom={cd.Zoom:F4} depth={cd.Depth:F1} pivot={fmt(cd.RotatePivot)} mc={fmt(cd.ModelCenter)}";
                    DiagLog($"[MfgCam:{tag}] zoomRatio={vizcore3d.View.ZoomRatio:F4} {cam} " +
                            $"split={(vizcore3d.SplitContainer != null ? vizcore3d.SplitContainer.SplitterDistance : -1)}/" +
                            $"{(vizcore3d.SplitContainer != null ? vizcore3d.SplitContainer.Width : -1)}");
                }
                catch (Exception ex) { DiagLog($"[MfgCam:{tag}] FAIL {ex.Message}"); }
            }

            vizcore3d.BeginUpdate();
            try
            {
                // 이전 선택상태(빨간색) 해제 + X-Ray 끄기 (수동 어댑터 전용)
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // ── 직전 미리보기가 건 화면축 회전 원복 (누적 차단) ──
                //   RotateCameraByScreenAxis는 상대(누적) 회전 — 미리보기는 PDF 경로처럼 되돌리는 곳이 없어
                //   Z-최장축(90°)·EA(180°) 부재를 연속 클릭할수록 카메라가 점점 틀어졌음 (2026-07-22 수정).
                //   MoveCamera(코어) '앞'에서 되돌리므로, MoveCamera가 자체 리셋해도 안전(덮어씀)·안 하면 여기서 상쇄.
                ResetMfgPreviewViewState("ExecuteMfgDrawing/entry");
                CamLog("entry");

                // ── 공통 코어 호출 ──
                //   부재 격리·BBox·축·카메라·ORIENTATION·Osnap·EA·치수·풍선 모두 코어가 수행.
                pose = BuildMfgSceneCore(bomIndex);
                CamLog("core");

                // 3D 미리보기에서는 Hole/SlotHole/EarthBoss 풍선을 표시하지 않는다.
                // PDF 경로(RenderMfgRowToViewArea)는 이 어댑터를 거치지 않아 풍선을 그대로 유지한다.
                vizcore3d.Review.Note.Clear();

                // ── 수동 어댑터 후처리: 3D 뷰용 SMOOTH 실선 + Silhouette ──
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // pose 저장 (진단·향후 참조용)
                _lastMfgViewPose = pose;
                shouldSnapshotCamera = pose.ApplyZ90 || pose.ApplyR180 ||
                                       pose.UsedMinusCamera || pose.UseReferenceAxis;

                DiagLog($"B2 ExecuteMfgDrawing bom={bom.Index} name=\"{bom.Name}\" " +
                    $"viewDir={pose.ViewDirection} longestAxis={pose.LongestAxis} " +
                    $"ApplyZ90={pose.ApplyZ90} ApplyR180={pose.ApplyR180} " +
                    $"UsedMinusCamera={pose.UsedMinusCamera} " +
                    $"orient={pose.OrientationAxis}/{pose.OrientationAngle:F0} " +
                    $"referenceAxis={pose.UseReferenceAxis}");
            }
            catch (Exception ex)
            {
                ResetMfgPreviewViewState("ExecuteMfgDrawing/error");
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                vizcore3d.EndUpdate();
            }

            // ── 카메라 프레이밍 — EndUpdate 후(커밋된 상태)에서 수행 (2026-07-22 fit 수정 2차) ──
            //   실측([MfgCam] fly 체크포인트): BeginUpdate 안에서 MoveCamera 직후 FlyToObject3d를 부르면
            //   회전 피벗만 부재로 옮겨지고 camZoom=0(퇴화 프레임) — 줌·거리를 못 잡아 부재 일부만 극단 확대됐음.
            //   부재만 격리된 상태이므로 커밋 후 FitToView(보이는 모델 화면 맞춤, 기본 여백 0.2)로 프레이밍.
            //   Z90/R180 회전도 커밋 후 적용 (총량은 _mfgPreviewNetRoll — 다음 진입 때 음수 원복, 누적 차단).
            if (pose != null)
            {
                vizcore3d.View.FitToView();
                CamLog("fit");

                float appliedRoll = 0f;
                if (pose.ApplyZ90)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    appliedRoll += 90f;
                }
                if (pose.ApplyR180)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                    appliedRoll += 180f;
                }
                _mfgPreviewNetRoll = appliedRoll;
                CamLog("rot");
            }

            // EndUpdate 이후 카메라 스냅샷 — ScreenAxisRotation commit 후
            //   (BeginUpdate 스코프 내에서는 회전이 commit 전 상태일 수 있음 — click-order 의존 버그 회피)
            if (shouldSnapshotCamera && pose != null)
            {
                System.Windows.Forms.Application.DoEvents();
                pose.CameraData = vizcore3d.View.GetCameraData();
                CamLog("afterDoEvents");
            }
        }

        /// <summary>
        /// 가공도 수동 통합 함수 v7. PDF 소유.
        /// 호출자:
        ///   - 수동: btnMfgDrawingSheet_Click → 결과 받아 단일 MessageBox로 표시
        ///   - 자동(P4a): ProcessSingleStruFull §8 → DiagLog만
        /// 사용자 사양:
        ///   - BOM 표·도면정보 = 제작도 방식 (CollectBOMInfo + lvDrawingBOMInfo 재사용)
        ///   - BOM 표 데이터 = 가공도 전체 부재 (모든 페이지에 동일)
        ///   - Shape/Note/Measure 일부 실패해도 PDF 만듬
        ///   - 출력 중 시트 목록 비활성화 (race 차단)
        /// 함수 내부 MessageBox 없음 (Codex 6차 권고 — 결과 객체로 정보 전달).
        /// </summary>
        private MfgDrawingResult GenerateMfgDrawingManual(
            List<DrawingSheetData> mfgSheets,
            string saveDir,
            string struName,
            int struIndex = 0,
            Func<bool> shouldCancel = null)
        {
            var result = new MfgDrawingResult();
            if (mfgSheets == null || mfgSheets.Count == 0) return result;

            // 실행 폴더 templates\ 우선 — 배포 패키지(.sln 없음)에서도 템플릿을 찾도록 (#71)
            string xlsxPath = ResolveDrawingTemplatePath("가공도_도면.xlsx");
            result.TemplatePath = xlsxPath;
            if (!File.Exists(xlsxPath))
            {
                DiagLog($"[GenMfgManual] 템플릿 누락: {xlsxPath}");
                result.TemplateMissing = true;
                result.Warnings.Add($"가공도 엑셀 템플릿 누락: {xlsxPath}");
                return result;
            }

            // v7 Codex 6차: try 진입 최상단 — 모든 mutable 작업 보호
            DrawingSheetData previousSelectedSheet = null;
            bool prevLvEnabled = lvDrawingSheet.Enabled;

            // #119: 페이지를 쌓아뒀다가 마지막에 PDF 1개로 저장한다.
            //   도면 일괄 출력이 이 함수를 품고 호출하면 그쪽 누적에 페이지만 얹고 저장은 넘긴다.
            bool ownsPdfAccumulation = false;
            string mergedPdfPath = null;
            // PDF 파일명(#49)과 표제부 TAG NO.(#120)가 같은 STRU 이름을 쓰도록 한 곳에서 잡는다.
            string mfgStruTag = "";
            string mfgDpNo = "";

            // P3 #2 패치 (2026-05-23, 사용자 보고):
            //   "가공도 출력 누르자 마자 다른 부재들이 보임" — 진입부 Show(ALL, true)가 BOM 채우기 위해
            //   모든 부재를 표시한 후 row 격리 사이의 중간 상태가 화면에 노출됨.
            //   해결: BeginUpdate/EndUpdate로 출력 전체를 감싸 화면 update 차단. finally의 격리 복원 후
            //   EndUpdate가 호출되어 사용자에겐 최종 격리 상태만 보임.
            // [격리 8단계 2026-07-21] BeginUpdate 임시 해제 — 사망 호출이 캡처 함수 자체로 특정됐고,
            //   제작도(정상)는 캡처가 BeginUpdate 스코프 밖. '갱신 중단 + 신 캔버스 + 캡처' 조합 용의.
            //   출력 중 화면 번쩍임(P3 #2 증상)이 일시 재발하나 크래시 판정 우선. 판정 후 재구성.
            // vizcore3d.BeginUpdate();

            try
            {
                // UI 잠금 (가장 먼저)
                lvDrawingSheet.Enabled = false;
                CheckMfgCancellation(
                    shouldCancel,
                    "가공도 출력 준비 중...",
                    "가공도 출력 초기화 전");

                if (lvDrawingSheet.SelectedItems.Count > 0)
                    previousSelectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;

                // ── 진입부 강제 초기화 8단계 ──
                ResetMfgPreviewViewState("GenerateMfgDrawingManual/start");
                vizcore3d.Review.Note.Clear();
                ClearMfgViewAnnotations("GenerateMfgDrawingManual/start");
                // #119: 여기서 이전 도면 잔재를 지우고 누적을 연다.
                //   이미 바깥(도면 일괄 출력)이 누적 중이면 지우지 않고 페이지만 이어붙인다.
                //   경로를 먼저 확정한다 — 누적을 연 뒤 예외가 나면 저장 경로 없이 닫히게 된다.
                //   파일명용 STRU 이름과 표제부 TAG NO.는 **출처가 다르다** (#49 vs #66).
                //   파일명 = 구조물 식별자(`STRU` 속성), TAG NO. = App.config `Uda.TagNo`가 가리키는 속성.
                //   출발 노드는 struIndex(STRU 노드)가 아니라 실제 부재여야 한다 — STRUCTURE 노드 자신은
                //   `STRU` 속성이 비어 있다(부재→구조물 역참조, #67).
                string mfgStruName = ResolveDrawingStruName(mfgSheets);
                DrawingSheetData firstTagSheet = mfgSheets.FirstOrDefault(item => item != null);
                mfgStruTag = GetTagNoValue(firstTagSheet);
                mfgDpNo = GetDpNoValue(firstTagSheet);
                DiagLog($"[TAG NO] 가공도 출력 공용값: value='{mfgStruTag}' (파일명용 STRU='{mfgStruName}')");
                mergedPdfPath = BuildMergedDrawingPdfPath(saveDir, mfgStruName, "가공도");
                ownsPdfAccumulation = BeginPdfPageAccumulation("가공도");
                if (vizcore3d.View.XRay.Enable) vizcore3d.View.XRay.Enable = false;
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                // DASH_LINE(은선 점선) 렌더모드 제거 — 은선 없는 캡처로 전환해 무의미 (2026-07-03). 출력 후 SMOOTH 복원은 유지.
                CheckMfgCancellation(
                    shouldCancel,
                    "가공도 BOM 준비 중...",
                    "가공도 출력 초기화 후");

                // ── BOM 표 1회 채우기 (가공도 전체 부재, 모든 페이지 동일) ──
                var allMfgBomIndices = mfgSheets
                    .Where(s => s.MemberIndices.Count > 0)
                    .Select(s => s.MemberIndices[0])
                    .Distinct()
                    .ToList();

                if (allMfgBomIndices.Count > 20)
                {
                    string msg = $"가공도 부재 {allMfgBomIndices.Count}개 — BOM 표 20행 초과, 21번째 이후 PDF 미표시";
                    DiagLog($"[GenMfgManual] WARN {msg}");
                    result.Warnings.Add(msg);
                }

                var syntheticSheet = new DrawingSheetData
                {
                    BaseMemberIndex = -3,
                    BaseMemberName = "가공도_묶음",
                    MemberIndices = allMfgBomIndices,
                    MfgDrawingNo = 0
                };
                CollectBOMInfo(false, syntheticSheet);
                CheckMfgCancellation(
                    shouldCancel,
                    "가공도 BOM 준비 완료",
                    "가공도 BOM 수집 후");

                var bomSnapshot = SnapshotBomRows();
                // SnapshotBomRows가 요약행을 포함하므로 기대치도 요약행 1행을 더한다 (#67).
                int expectedBomRows = Math.Min(allMfgBomIndices.Count, 20) + 1;
                result.BomRows = bomSnapshot.Count;
                result.ExpectedBomRows = expectedBomRows;

                if (bomSnapshot.Count != expectedBomRows)
                {
                    DiagLog($"[GenMfgManual] WARN BOM snapshot mismatch: {bomSnapshot.Count} vs 예상 {expectedBomRows}");
                    if (bomSnapshot.Count > expectedBomRows)
                        result.Warnings.Add($"BOM snapshot 초과 ({bomSnapshot.Count}행, 예상 {expectedBomRows}) — 요약행 포함 첫 25행만 사용");
                }

                bool bomSnapshotInsufficient = bomSnapshot.Count < expectedBomRows;
                if (bomSnapshotInsufficient)
                    DiagLog($"[GenMfgManual] WARN BOM 부족: {bomSnapshot.Count} < {expectedBomRows} (PDF 계속 생성)");

                // 다중 이미지 매핑 (SDK 1.0.26.716 신규) — {Image_1}=N 화살표(AT3), {Image_2}=ISO 화살표(C3),
                //   {Image_3}=CONTRACTOR 로고(AW53). Value = [일반, 배경반전].
                //   옛 {Image}+Set2DViewTemplateMark는 신 SDK에서 무력화 확인(로고 미표시) → {Image_3} 통합 (2026-07-21).
                var mfgImageMapping = new Dictionary<int, string[]>();
                AddImageSlotIfExists(mfgImageMapping, 1, "North_Arrow.png");
                AddImageSlotIfExists(mfgImageMapping, 2, "ISO_North_Arrow.png");
                AddImageSlotIfExists(mfgImageMapping, 3, "Logo.png");

                var pages = SplitMfgIntoPages(mfgSheets, 5);
                Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea> viewAreasCache = null;

                // 제작도·조립도·설치도와 같은 도면 목록 공용 PAINT CODE를 가공도 전 페이지가 재사용한다.
                DrawingSheetData firstMfgSheet = mfgSheets.FirstOrDefault(item => item != null);
                var paintCodes = GetOrCacheDrawingPaintCode(firstMfgSheet, struIndex);
                foreach (DrawingSheetData mfgSheet in mfgSheets)
                {
                    if (mfgSheet == null) continue;
                    mfgSheet.PaintCode = paintCodes.First;
                    mfgSheet.PaintCode2 = paintCodes.Second;
                }
                DiagLog($"[PAINT CODE] 가공도 출력 공용값: pages={pages.Count} " +
                        $"value='{paintCodes.First}' value2='{paintCodes.Second}'");


                foreach (var page in pages)
                {
                    string pageProgress = $"가공도 {page.PageIdx}/{pages.Count}페이지";
                    CheckMfgCancellation(
                        shouldCancel,
                        $"{pageProgress} 준비 중...",
                        $"{pageProgress} 시작 전");

                    int failedRows = 0;
                    int successRows = 0;
                    try
                    {
                        ResetCanvasForMfgPage();
                        var data = BuildMfgPageData(page, pages.Count, struName,
                            paintCodes.First, paintCodes.Second, mfgStruTag, mfgDpNo, bomSnapshot);
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{pageProgress} 템플릿 적용 중...",
                            $"{pageProgress} 템플릿 적용 전");
                        var swTpl = System.Diagnostics.Stopwatch.StartNew();
                        // 북쪽 화살표 2종은 {Image_1}/{Image_2} + mfgImageMapping으로 Import 단계에서 처리 (2026-07-20).
                        //   ⚠ 태그 번호 한계 주의 — View는 1~7, Input은 1~199까지만 (초과 시 SDK 메모리 손상 → 캡처 AccessViolation).
                        vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data, mfgImageMapping);
                        swTpl.Stop();
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{pageProgress} 템플릿 적용 완료",
                            $"{pageProgress} 템플릿 적용 후");
                        DiagLog($"[TplTime] 템플릿 적용 p{page.PageIdx}={swTpl.ElapsedMilliseconds}ms");
                        // 빈 칸 괘선 제거 (SDK 1.0.26.716) — 미기재 BOM 행 괘선 제거, 제작도와 동일 패턴.
                        vizcore3d.Drawing2D.Object2D.RemoveEmptyTemplateBorders(0.1f, VIZCore3D.NET.Data.TemplateBorderRemoveMode.RowAndColumn);
                        EnsureViewAreasCache(ref viewAreasCache, xlsxPath);

                        // 캔버스 선(先)렌더 — 제작도(정상 완주) 검증 시퀀스 정합 (격리 7단계 2026-07-21).
                        //   제작도는 import 직후 Drawing2D.Render()를 호출한 뒤 캡처하는데 가공도는 이게 없었음.
                        //   '그리지 않은 캔버스 + 첫 캡처' 조합이 신 템플릿에서 AccessViolation 용의.
                        vizcore3d.Drawing2D.Render();
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{pageProgress} 부재 뷰 생성 중...",
                            $"{pageProgress} 초기 렌더 후");

                        for (int i = 0; i < page.Rows.Count; i++)
                        {
                            CheckMfgCancellation(
                                shouldCancel,
                                $"{pageProgress} 부재 {i + 1}/{page.Rows.Count} 처리 중...",
                                $"{pageProgress} 행 {i + 1} 시작 전");

                            var sheet = page.Rows[i];
                            if (sheet.MemberIndices.Count == 0) { failedRows++; continue; }
                            var bom = bomList.FirstOrDefault(b => b.Index == sheet.MemberIndices[0]);
                            if (bom == null) { failedRows++; continue; }

                            var area = viewAreasCache[i + 1];
                            if (RenderMfgRowToViewArea(
                                i + 1,
                                bom,
                                area,
                                shouldCancel,
                                pageProgress)) successRows++;
                            else failedRows++;
                        }

                        if (failedRows > 0)
                            DiagLog($"[GenMfgManual] p{page.PageIdx} WARN failed={failedRows} success={successRows}");

                        if (successRows == 0)
                        {
                            DiagLog($"[GenMfgManual] p{page.PageIdx} SKIP — 모든 row 실패");
                            result.Warnings.Add($"p{page.PageIdx} 페이지 모든 row 실패, PDF 미저장");
                            continue;
                        }

                        CheckMfgCancellation(
                            shouldCancel,
                            $"{pageProgress} 페이지 마무리 중...",
                            $"{pageProgress} 최종 렌더 전");
                        vizcore3d.Drawing2D.Render();
                        vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                        // 묶지 않는 모드(기본)에서는 이 페이지를 바로 저장한다.
                        //   묶음 모드면 캔버스를 쌓아두고 finally에서 PDF 1개로 저장한다 (#119).
                        //   묶음은 모든 페이지의 2D 객체가 동시에 살아 있어야 해서 장수가 많으면
                        //   "보호된 메모리" 오류가 난다 — App.config `Pdf.MergePages` 참고.
                        if (!_pdfPageAccumulating)
                        {
                            string mfgPagePath = BuildMergedDrawingPdfPath(saveDir, mfgStruName, "가공도");
                            if (SaveCurrentDrawingToPdf(mfgPagePath))
                            {
                                result.SavedPdfPath = mfgPagePath;
                                result.SuccessPdfs++;
                            }
                        }
                        result.SuccessPages++;
                        if (bomSnapshotInsufficient) result.InsufficientBomPdfs++;

                        DiagLog($"[GenMfgManual] p{page.PageIdx}/{pages.Count} 페이지 완성 (누적 {result.SuccessPages}장)");
                        CheckMfgCancellation(
                            shouldCancel,
                            $"{pageProgress} 페이지 완료",
                            $"{pageProgress} 페이지 완료 후");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[GenMfgManual] p{page.PageIdx} ERROR: {ex.Message}");
                        result.Warnings.Add($"p{page.PageIdx} 페이지 ERROR: {ex.Message}");
                        // #119: 반쪽짜리 페이지가 PDF에 끼지 않도록 그 캔버스만 버린다.
                        DiscardCurrentPdfPage();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                result.Canceled = true;
                result.CancellationCheckpoint = ex.Message;
                DiagLog($"[GenMfgManual] 취소: {ex.Message}, 완성 페이지={result.SuccessPages}");
                // #119: 여기서 캔버스를 비우면 그때까지 그린 페이지가 사라진다.
                //   취소분까지 저장한 뒤 정리는 finally가 맡는다.
            }
            catch (Exception ex)
            {
                DiagLog($"[GenMfgManual] FATAL: {ex.Message}");
                result.Warnings.Add($"가공도 출력 FATAL: {ex.Message}");
            }
            finally
            {
                // #119: 쌓아둔 페이지를 PDF 1개로 저장한다. 취소·실패로 빠져나왔어도
                //   그때까지 그린 페이지는 남긴다. 바깥이 누적 주인이면 저장을 넘긴다.
                if (ownsPdfAccumulation)
                {
                    try
                    {
                        if (EndPdfPageAccumulation(mergedPdfPath))
                        {
                            result.SavedPdfPath = mergedPdfPath;
                            result.SuccessPdfs = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[GenMfgManual] 묶음 PDF 저장 실패: {ex.Message}");
                        result.Warnings.Add($"가공도 PDF 저장 실패: {ex.Message}");
                    }
                }




                ReleaseActiveMfgReferenceAxis("GenerateMfgDrawingManual/finally");

                // BOM UI 복원
                try
                {
                    if (previousSelectedSheet != null)
                    {
                        CollectBOMInfo(false, previousSelectedSheet);
                        DiagLog($"[GenMfgManual] BOM UI 복원: '{previousSelectedSheet.BaseMemberName}'");
                    }
                }
                catch (Exception ex)
                {
                    DiagLog($"[GenMfgManual] BOM UI 복원 실패: {ex.Message}");
                }
                // UI 잠금 해제
                lvDrawingSheet.Enabled = prevLvEnabled;

                // 가시성 복원 (사용자 보고 P3 #1, 2026-05-23):
                //   v7 옛 동작: RestoreAllPartsVisibility() → 출력 후 모든 부재 보임 (사용자가 보던 격리 깨짐)
                //   v7.1 새 동작: 선택 시트 있으면 그 시트의 부재만 격리 복원, 없으면 RestoreAll (폴백)
                try
                {
                    if (previousSelectedSheet != null && previousSelectedSheet.MemberIndices != null
                        && previousSelectedSheet.MemberIndices.Count > 0)
                    {
                        // 선택 시트의 부재만 격리 복원 (출력 전 사용자가 보던 미리보기 상태)
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(previousSelectedSheet.MemberIndices, true);
                        DiagLog($"[GenMfgManual] 가시성 복원: '{previousSelectedSheet.BaseMemberName}' 부재 {previousSelectedSheet.MemberIndices.Count}개만 격리");
                    }
                    else
                    {
                        // 선택 시트 없으면 모든 부재 보이게 (폴백)
                        RestoreAllPartsVisibility();
                        DiagLog("[GenMfgManual] 가시성 복원: RestoreAll (선택 시트 없음)");
                    }
                }
                catch (Exception ex)
                {
                    DiagLog($"[GenMfgManual] 가시성 복원 실패: {ex.Message}");
                }

                // 3D 뷰 기본(부드러운 음영) 복원 — 가공도 출력 후 은선/X-Ray 잔존 방지 (2026-06-23).
                //   EndUpdate(1826) 앞에 두어 격리+SMOOTH를 한 번에 commit.
                try
                {
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                    if (vizcore3d.View.XRay.Enable) vizcore3d.View.XRay.Enable = false;
                }
                catch { }

                // P3 #2: EndUpdate (BeginUpdate 짝) — [격리 8단계] Begin 해제와 짝 맞춰 임시 해제
                // try { vizcore3d.EndUpdate(); } catch { }
            }

            DiagLog($"[GenMfgManual] 완료 — Pages={result.SuccessPages} Pdf={result.SuccessPdfs} " +
                    $"BomShort={result.InsufficientBomPdfs} Warnings={result.Warnings.Count}");
            return result;
        }

        /// <summary>
        /// 도면정보 탭 - 가공도 출력 버튼 클릭 (v7 P2-integrate)
        /// 가공도 시트 묶음 수집 → GenerateMfgDrawingManual 호출 → 결과 받아 단일 MessageBox.
        /// </summary>
        private void btnMfgDrawingSheet_Click(object sender, EventArgs e)
        {
            if (_cancelableOperationInProgress || !lvDrawingSheet.Enabled)
            {
                MessageBox.Show(
                    "다른 도면 작업이 진행 중입니다.",
                    "알림",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var mfgSheets = new List<DrawingSheetData>();
            foreach (ListViewItem lvi in lvDrawingSheet.Items)
                if (lvi.Text.StartsWith("가공도"))
                {
                    var s = lvi.Tag as DrawingSheetData;
                    if (s != null && s.MemberIndices.Count > 0) mfgSheets.Add(s);
                }

            if (mfgSheets.Count == 0)
            {
                MessageBox.Show("가공도 시트가 없습니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string saveDir = GetDefaultDrawingSaveDir();
            Dictionary<Control, bool> previousEnabledStates =
                CaptureDrawingExportControlStates();
            MfgDrawingResult result;
            try
            {
                SetDrawingExportControlsEnabled(false);
                BeginCancelableOperation();
                ShowBusyOverlay("가공도 PDF 출력 준비 중...");
                result = GenerateMfgDrawingManual(
                    mfgSheets,
                    saveDir,
                    "manual",
                    struIndex: 0,
                    shouldCancel: () => _cancelRequested);
            }
            finally
            {
                HideBusyOverlay();
                EndCancelableOperation();
                RestoreDrawingExportControlStates(previousEnabledStates);
            }

            if (result.Canceled)
            {
                var canceledMessage = new System.Text.StringBuilder();
                canceledMessage.AppendLine("가공도 출력을 취소했습니다.");
                canceledMessage.AppendLine();
                // #119: 취소해도 그때까지 그린 페이지는 PDF 1개로 저장된다.
                if (!string.IsNullOrWhiteSpace(result.SavedPdfPath))
                {
                    canceledMessage.AppendLine($"PDF 1개에 도면 {result.SuccessPages}장 저장");
                    canceledMessage.AppendLine(result.SavedPdfPath);
                }
                else
                {
                    canceledMessage.AppendLine("저장된 PDF가 없습니다.");
                }
                if (!string.IsNullOrWhiteSpace(result.CancellationCheckpoint))
                    canceledMessage.AppendLine($"중단 위치: {result.CancellationCheckpoint}");

                MessageBox.Show(
                    canceledMessage.ToString(),
                    "취소됨",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // v7 Codex 6차 권고: 단일 MessageBox 통합
            if (result.TemplateMissing)
            {
                MessageBox.Show(
                    $"가공도 엑셀 템플릿 누락:\n{result.TemplatePath}\n\nPDF 생성 안 됨.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sb = new System.Text.StringBuilder();
            // #119: 도면 장수와 무관하게 PDF는 1개다.
            if (!string.IsNullOrWhiteSpace(result.SavedPdfPath))
            {
                sb.AppendLine($"가공도 PDF 1개에 도면 {result.SuccessPages}장 저장:");
                sb.AppendLine(result.SavedPdfPath);
            }
            else
            {
                sb.AppendLine("저장된 PDF가 없습니다.");
                sb.AppendLine(saveDir);
            }

            if (result.InsufficientBomPdfs > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ BOM 부족 페이지: {result.InsufficientBomPdfs}장");
                sb.AppendLine($"  (snapshot {result.BomRows}행 < 예상 {result.ExpectedBomRows}행)");
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("⚠️ 경고:");
                foreach (var w in result.Warnings)
                    sb.AppendLine($"  · {w}");
            }

            var icon = result.HasIssues ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(sb.ToString(), "가공도 출력 완료",
                MessageBoxButtons.OK, icon);
        }

        /// <summary>
        /// UDA에서 SPREF 값을 조회 (현재 노드 → 부모 10단계까지 탐색). 결과를 _udaValueCache에 캐시.
        /// </summary>
        private string GetSprefValue(int nodeIndex)
        {
            var cacheKey = (nodeIndex, "SPREF");
            if (_udaValueCache.TryGetValue(cacheKey, out string cached)) return cached;
            string result = GetSprefValueUncached(nodeIndex);
            _udaValueCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// 노드 인덱스를 받아 → UDA에서 SPREF 값을 찾아 돌려준다. 현재 노드에 없으면 부모로 최대 10단계 올라가며 찾고, 없으면 "".
        /// 캐시를 거치지 않고 SDK를 직접 조회한다. 보통은 캐시 적용 버전을 거쳐 들어온다.
        /// SDK 예외는 모두 삼키고 빈 문자열로 처리한다.
        /// </summary>
        private string GetSprefValueUncached(int nodeIndex)
        {
            List<string> udaKeyList = null;
            try
            {
                var keys = vizcore3d.Object3D.UDA.Keys;
                if (keys != null && keys.Count > 0)
                    udaKeyList = new List<string>(keys);
            }
            catch { }

            if (udaKeyList == null) return "";

            int currentIdx = nodeIndex;
            for (int depth = 0; depth < 10; depth++)
            {
                if (currentIdx < 0) break;

                foreach (string key in udaKeyList)
                {
                    if (key.Trim().ToUpper() != "SPREF") continue;
                    try
                    {
                        var val = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                        string valStr = (val != null) ? val.ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(valStr))
                            return valStr;
                    }
                    catch { }
                }

                try
                {
                    VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                    if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                    currentIdx = parentNode.ParentIndex;
                }
                catch { break; }
            }

            return "";
        }

        /// <summary>
        /// SPREF 값에 "PAD" 또는 "PLATE" 문자열이 포함되어 있는지 확인
        /// </summary>
        private bool IsPadOrPlateFromSpref(int nodeIndex)
        {
            string spref = GetSprefValue(nodeIndex);
            if (string.IsNullOrEmpty(spref)) return false;

            string upper = spref.ToUpper();
            return upper.Contains("PAD") || upper.Contains("PLATE");
        }

        /// <summary>
        /// SPREF 값의 왼쪽 2자리가 "EA"인지 확인 (앵글 부재 여부)
        /// SPREF 형식: "/EA100x75x10:SIZE" → "/" 제거 후 ":" 앞 부분의 첫 2자리 확인
        /// </summary>
        private bool IsAngleFromSpref(int nodeIndex)
        {
            string spref = GetSprefValue(nodeIndex);
            if (string.IsNullOrEmpty(spref)) return false;

            string clean = spref;
            if (clean.StartsWith("/"))
                clean = clean.Substring(1);

            // ":" 앞 부분 (ITEM) 추출
            int colonIdx = clean.IndexOf(':');
            string item = colonIdx >= 0 ? clean.Substring(0, colonIdx).Trim() : clean.Trim();

            return item.Length >= 2 && item.Substring(0, 2).ToUpper() == "EA";
        }

        /// <summary>
        /// 은선(Hidden Line) Osnap 필터링
        /// 뷰 방향 기준 뒷면(back surface)에 있는 Osnap 포인트 제거
        /// </summary>
        private List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> FilterHiddenLineOsnap(
            List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> osnapList,
            string viewDirection, float minX, float maxX, float minY, float maxY, float minZ, float maxZ,
            bool isMinusCamera = false)
        {
            if (osnapList.Count == 0) return osnapList;

            // 뷰 방향별 깊이축 범위
            float depthMin, depthMax;
            switch (viewDirection)
            {
                case "X": depthMin = minX; depthMax = maxX; break;
                case "Y": depthMin = minY; depthMax = maxY; break;
                default:  depthMin = minZ; depthMax = maxZ; break;
            }

            float depthRange = depthMax - depthMin;
            if (depthRange < 0.5f) return osnapList; // 두께가 거의 없는 평판 → 필터링 불필요

            // PLUS 카메라: 카메라가 -쪽(min)에 위치, +방향을 바라봄 → depthMax 근처가 뒷면(먼쪽)
            // MINUS 카메라: 카메라가 +쪽(max)에 위치, -방향을 바라봄 → depthMin 근처가 뒷면(먼쪽)
            float backThreshold;
            bool removeHigh; // true면 높은쪽 제거, false면 낮은쪽 제거
            if (isMinusCamera)
            {
                // MINUS: 카메라가 +쪽 → 뒷면 = depthMin 근처 → 낮은쪽 제거
                backThreshold = depthMin + depthRange * 0.15f;
                removeHigh = false;
            }
            else
            {
                // PLUS: 카메라가 -쪽 → 뒷면 = depthMax 근처 → 높은쪽 제거
                backThreshold = depthMax - depthRange * 0.15f;
                removeHigh = true;
            }

            var filtered = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
            foreach (var pt in osnapList)
            {
                float depth;
                switch (viewDirection)
                {
                    case "X": depth = pt.point.X; break;
                    case "Y": depth = pt.point.Y; break;
                    default:  depth = pt.point.Z; break;
                }

                // 뒷면 근처가 아닌 포인트만 유지
                if (removeHigh)
                {
                    if (depth < backThreshold)
                        filtered.Add(pt);
                }
                else
                {
                    if (depth > backThreshold)
                        filtered.Add(pt);
                }
            }

            // 뒷면 가시축 극점 복원 폐지 (사용자 사양 2026-07-03):
            //   은선 캡처 제거로 단면(보이는 외곽)만 출력하므로, 뒷면 osnap은 극점이어도 치수화하지 않는다.
            //   (옛 예외 954e4de — 은선 모드에서 먼쪽 플랜지 높이 누락 대응 — 는 은선 폐지로 근거 소멸)

            // 필터 후 포인트가 없으면 원본 유지
            return filtered.Count > 0 ? filtered : osnapList;
        }

        /// <summary>
        /// UDA에서 특정 Key 값을 조회 (현재 노드 → 부모 10단계까지 탐색). 결과를 _udaValueCache에 캐시.
        /// </summary>
        private string GetUdaValue(int nodeIndex, string keyName)
        {
            var cacheKey = (nodeIndex, keyName.Trim().ToUpper());
            if (_udaValueCache.TryGetValue(cacheKey, out string cached)) return cached;
            string result = GetUdaValueUncached(nodeIndex, keyName);
            _udaValueCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// 노드 인덱스와 UDA 키 이름을 받아 → 그 키의 값을 찾아 돌려준다. 현재 노드에 없으면 부모로 최대 10단계 올라가며 찾고, 없으면 "".
        /// 키 비교는 공백 제거·대문자 기준. 캐시를 거치지 않고 SDK를 직접 조회하므로 보통은 캐시 버전을 쓴다.
        /// </summary>
        private string GetUdaValueUncached(int nodeIndex, string keyName)
        {
            List<string> udaKeyList = null;
            try
            {
                var keys = vizcore3d.Object3D.UDA.Keys;
                if (keys != null && keys.Count > 0)
                    udaKeyList = new List<string>(keys);
            }
            catch { }

            if (udaKeyList == null) return "";

            string targetKey = keyName.Trim().ToUpper();
            int currentIdx = nodeIndex;
            for (int depth = 0; depth < 10; depth++)
            {
                if (currentIdx < 0) break;

                foreach (string key in udaKeyList)
                {
                    if (key.Trim().ToUpper() != targetKey) continue;
                    try
                    {
                        var val = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                        string valStr = (val != null) ? val.ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(valStr))
                            return valStr;
                    }
                    catch { }
                }

                try
                {
                    VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                    if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                    currentIdx = parentNode.ParentIndex;
                }
                catch { break; }
            }

            return "";
        }

        /// <summary>
        /// ORIENTATION UDA 파싱
        /// 형식: "is N and~" (회전없음), "is E 45~" (45도 회전)
        /// N = X방향, E = Y방향
        /// Returns: (orientAxis: "X"/"Y"/"", angle: 0/45/etc)
        /// </summary>
        private (string orientAxis, float angle) ParseOrientation(int nodeIndex)
        {
            string orientVal = GetUdaValue(nodeIndex, "ORIENTATION");
            if (string.IsNullOrEmpty(orientVal)) return ("", 0f);

            string upper = orientVal.Trim().ToUpper();

            // "IS" 이후 부분 추출
            int isIdx = upper.IndexOf("IS");
            if (isIdx < 0) return ("", 0f);
            string afterIs = upper.Substring(isIdx + 2).Trim();

            if (afterIs.Length == 0) return ("", 0f);

            // 방향 문자 추출 (N=X, E=Y, S=X, W=Y)
            string orientAxis = "";
            char dirChar = afterIs[0];
            switch (dirChar)
            {
                case 'N': orientAxis = "X"; break;
                case 'E': orientAxis = "Y"; break;
                case 'S': orientAxis = "X"; break;
                case 'W': orientAxis = "Y"; break;
                default: return ("", 0f);
            }

            string rest = afterIs.Substring(1).Trim();

            // "AND" → 회전 없음
            if (rest.StartsWith("AND")) return (orientAxis, 0f);

            // 숫자 추출 (방향 다음 숫자)
            string numStr = "";
            foreach (char c in rest)
            {
                if (char.IsDigit(c) || c == '.' || c == '-') numStr += c;
                else break;
            }
            float angle = 0f;
            if (!string.IsNullOrEmpty(numStr))
                float.TryParse(numStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out angle);

            return (orientAxis, angle);
        }

        /// <summary>
        /// ORIENTATION 기반 카메라 회전 적용
        /// </summary>
        private void ApplyOrientationRotation(int nodeIndex, string viewDirection)
        {
            var (orientAxis, orientAngle) = ParseOrientation(nodeIndex);

            if (orientAngle != 0f)
            {
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, orientAngle);
            }
        }

        /// <summary>
        /// ORIENTATION 기반 Looking 라벨 생성 (카메라 회전 없이 라벨만)
        /// 예: "Looking X 45 Y" 또는 "Looking \"X\""
        /// </summary>
        private string GetOrientationLabel(int nodeIndex, string viewDirection)
        {
            var (orientAxis, orientAngle) = ParseOrientation(nodeIndex);

            if (orientAngle != 0f)
                return $"Looking {viewDirection} {orientAngle:F0} {orientAxis}";
            else
                return $"Looking \"{viewDirection}\"";
        }

        // 참조축 자동 정렬 1단계 진단값. 이 단계에서는 카메라·모델 좌표계를 변경하지 않는다.
        private const double MfgAxisDirectionToleranceDegrees = 5.0;
        private const double MfgAxisTiltToleranceDegrees = 1.0;
        private const double MfgAxisMinimumLineLength = 0.001;

        private readonly Dictionary<int, MfgAxisDetectionResult> _mfgAxisDetectionCache
            = new Dictionary<int, MfgAxisDetectionResult>();

        private struct MfgAxisVector
        {
            public double X;
            public double Y;
            public double Z;

            public MfgAxisVector(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// 사유 문자열을 받아 → 활성 가공도 참조축을 해제한 뒤 3D 측정(치수)과 ShapeDrawing(보조선)을 모두 지운다.
        /// 장면 코어·EA 2차 뷰·PDF 행 렌더링의 시작과 끝마다 호출. 참조축이 Measure.Clear에 함께 지워지므로 축 생성 전에 부른다.
        /// </summary>
        private void ClearMfgViewAnnotations(string reason)
        {
            ReleaseActiveMfgReferenceAxis(reason + "/clear");
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
        }

        /// <summary>
        /// 사유 문자열을 받아 → 활성 가공도 참조축이 있으면 활성 ID를 먼저 -1로 비운 뒤 SDK 참조축을 리셋하고 리뷰 항목을 삭제한다.
        /// 활성 축이 없으면 아무것도 안 한다. 리셋·삭제 실패는 로그만 남기고 계속 간다.
        /// </summary>
        private void ReleaseActiveMfgReferenceAxis(string reason)
        {
            if (_mfgActiveReferenceAxisId < 0) return;

            int referenceAxisId = _mfgActiveReferenceAxisId;
            _mfgActiveReferenceAxisId = -1;
            try
            {
                vizcore3d.View.ReferenceAxis.Reset();
            }
            catch (Exception ex)
            {
                DiagLog($"[MfgRefAxis] reset WARN id={referenceAxisId} reason={reason}: {ex.Message}");
            }

            try
            {
                vizcore3d.Review.Delete(referenceAxisId);
            }
            catch (Exception ex)
            {
                DiagLog($"[MfgRefAxis] delete WARN id={referenceAxisId} reason={reason}: {ex.Message}");
            }

            DiagLog($"[MfgRefAxis] release id={referenceAxisId} reason={reason}");
        }

        /// <summary>
        /// pose의 로컬 참조축(X·Y·원점)과 부재를 받아 → SDK 참조축을 만들어 활성화하고 그 축 기준으로 카메라를 옮긴다. 성공 시 true.
        /// 기존 활성 축은 먼저 해제. 실패하면 참조축 사용을 끄고 월드 카메라+ORIENTATION 화면 회전으로 폴백해 false.
        /// 활성화 이후 CameraDirection은 참조축 기준으로 해석된다.
        /// </summary>
        private bool ActivateMfgReferenceAxis(MfgViewPose pose, BOMData bom, string stage)
        {
            if (pose == null || bom == null || !pose.UseReferenceAxis)
                return false;

            ReleaseActiveMfgReferenceAxis(stage + "/replace");
            try
            {
                int referenceAxisId = vizcore3d.View.ReferenceAxis.Create(
                    pose.ReferenceAxisX,
                    pose.ReferenceAxisY,
                    pose.ReferenceAxisOrigin,
                    $"가공도 부재축 {bom.Index}");
                if (referenceAxisId < 0)
                    throw new InvalidOperationException("ReferenceAxis.Create가 유효하지 않은 ID를 반환했습니다.");

                _mfgActiveReferenceAxisId = referenceAxisId;
                vizcore3d.View.ReferenceAxis.Activate(referenceAxisId, true);
                // Activate 이후의 CameraDirection은 활성 참조축 기준으로 해석된다.
                vizcore3d.View.MoveCamera(pose.CameraDirection);

                DiagLog($"[MfgRefAxis] activate stage={stage} bom={bom.Index} id={referenceAxisId} " +
                        $"view={pose.ViewDirection} camera={pose.CameraDirection} " +
                        $"X=({pose.ReferenceAxisX.X:F5},{pose.ReferenceAxisX.Y:F5},{pose.ReferenceAxisX.Z:F5}) " +
                        $"Y=({pose.ReferenceAxisY.X:F5},{pose.ReferenceAxisY.Y:F5},{pose.ReferenceAxisY.Z:F5}) " +
                        $"Z=({pose.ReferenceAxisZ.X:F5},{pose.ReferenceAxisZ.Y:F5},{pose.ReferenceAxisZ.Z:F5})");
                return true;
            }
            catch (Exception ex)
            {
                ReleaseActiveMfgReferenceAxis(stage + "/failed");
                pose.UseReferenceAxis = false;
                vizcore3d.View.MoveCamera(pose.CameraDirection);
                ApplyOrientationRotation(bom.Index, pose.ViewDirection);
                DiagLog($"[MfgRefAxis] activate FAIL stage={stage} bom={bom.Index} → 기존 화면 roll 폴백: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사유 문자열을 받아 → 직전 미리보기가 건 화면축 누적 회전을 되돌리고 활성 참조축을 해제한다.
        /// 미리보기 진입·오류, PDF 출력 시작, 비가공도 시트 선택 시 호출. _mfgPreviewNetRoll을 0으로 초기화한다.
        /// </summary>
        private void ResetMfgPreviewViewState(string reason)
        {
            if (_mfgPreviewNetRoll != 0f)
            {
                try
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, -_mfgPreviewNetRoll);
                }
                catch (Exception ex)
                {
                    DiagLog($"[MfgRefAxis] preview roll reset WARN reason={reason}: {ex.Message}");
                }
                _mfgPreviewNetRoll = 0f;
            }

            ReleaseActiveMfgReferenceAxis(reason);
        }

        /// <summary>
        /// 부재와 pose를 받아 → ORIENTATION UDA의 축 방향 문자열로 직교 로컬 참조축(X·Y·Z)과
        /// 부재 중심 원점을 pose에 세운다. 성공 시 true.
        /// 기울기 1° 이하·UDA 없음·쓸 축 조합 없음(Z만 읽힌 경우)이면 false.
        /// 두 축이면 직교화하고, X나 Y 하나뿐이면 월드축과 외적으로 보완한다.
        /// 장면 코어가 카메라 이동 직후 호출. 여기서는 pose만 채우고 SDK 참조축은 만들지 않는다.
        /// </summary>
        private bool TryBuildMfgOrientationReferenceFrame(BOMData bom, MfgViewPose pose)
        {
            if (bom == null || pose == null ||
                Math.Abs(pose.OrientationAngle) <= MfgAxisTiltToleranceDegrees)
                return false;

            string raw = GetUdaValue(bom.Index, "ORIENTATION");
            if (string.IsNullOrWhiteSpace(raw)) return false;

            VIZCore3D.NET.Data.Vector3D rawX, rawY, rawZ;
            bool hasX = TryParseMfgOrientationDirection(raw, "X", out rawX);
            bool hasY = TryParseMfgOrientationDirection(raw, "Y", out rawY);
            bool hasZ = TryParseMfgOrientationDirection(raw, "Z", out rawZ);

            VIZCore3D.NET.Data.Vector3D xAxis = null;
            VIZCore3D.NET.Data.Vector3D yAxis = null;
            VIZCore3D.NET.Data.Vector3D zAxis = null;

            if (hasY && hasZ)
            {
                if (!TryNormalizeMfgVector(rawY, out yAxis)) return false;
                rawZ = SubtractMfgVector(rawZ, ScaleMfgVector(yAxis, DotMfgVector(rawZ, yAxis)));
                if (!TryNormalizeMfgVector(rawZ, out zAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(yAxis, zAxis), out xAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(zAxis, xAxis), out yAxis)) return false;
            }
            else if (hasX && hasY)
            {
                if (!TryNormalizeMfgVector(rawX, out xAxis)) return false;
                rawY = SubtractMfgVector(rawY, ScaleMfgVector(xAxis, DotMfgVector(rawY, xAxis)));
                if (!TryNormalizeMfgVector(rawY, out yAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(xAxis, yAxis), out zAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(zAxis, xAxis), out yAxis)) return false;
            }
            else if (hasX && hasZ)
            {
                if (!TryNormalizeMfgVector(rawX, out xAxis)) return false;
                rawZ = SubtractMfgVector(rawZ, ScaleMfgVector(xAxis, DotMfgVector(rawZ, xAxis)));
                if (!TryNormalizeMfgVector(rawZ, out zAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(zAxis, xAxis), out yAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(xAxis, yAxis), out zAxis)) return false;
            }
            else if (hasY)
            {
                if (!TryNormalizeMfgVector(rawY, out yAxis)) return false;
                VIZCore3D.NET.Data.Vector3D up = new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f);
                if (Math.Abs(DotMfgVector(yAxis, up)) > 0.99f)
                    up = new VIZCore3D.NET.Data.Vector3D(1f, 0f, 0f);
                if (!TryNormalizeMfgVector(CrossMfgVector(yAxis, up), out xAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(xAxis, yAxis), out zAxis)) return false;
            }
            else if (hasX)
            {
                if (!TryNormalizeMfgVector(rawX, out xAxis)) return false;
                VIZCore3D.NET.Data.Vector3D up = new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f);
                if (Math.Abs(DotMfgVector(xAxis, up)) > 0.99f)
                    up = new VIZCore3D.NET.Data.Vector3D(0f, 1f, 0f);
                if (!TryNormalizeMfgVector(CrossMfgVector(up, xAxis), out yAxis)) return false;
                if (!TryNormalizeMfgVector(CrossMfgVector(xAxis, yAxis), out zAxis)) return false;
            }
            else
            {
                return false;
            }

            pose.ReferenceAxisX = xAxis;
            pose.ReferenceAxisY = yAxis;
            pose.ReferenceAxisZ = zAxis;
            pose.ReferenceAxisOrigin = new VIZCore3D.NET.Data.Vector3D(
                (bom.MinX + bom.MaxX) / 2f,
                (bom.MinY + bom.MaxY) / 2f,
                (bom.MinZ + bom.MaxZ) / 2f);
            pose.UseReferenceAxis = true;

            DiagLog($"[MfgRefAxis] frame bom={bom.Index} ORIENTATION='{raw}' " +
                    $"X=({xAxis.X:F5},{xAxis.Y:F5},{xAxis.Z:F5}) " +
                    $"Y=({yAxis.X:F5},{yAxis.Y:F5},{yAxis.Z:F5}) " +
                    $"Z=({zAxis.X:F5},{zAxis.Y:F5},{zAxis.Z:F5})");
            return true;
        }

        /// <summary>
        /// ORIENTATION 원문과 로컬축 이름을 받아 → "X IS N 45 E" 꼴을 정규식으로 읽어 월드 방향 단위벡터를 돌려준다. 못 읽으면 false.
        /// 각도가 있으면 1차 방위에 cos, 2차 방위에 sin을 곱해 합성한 뒤 정규화한다.
        /// </summary>
        private bool TryParseMfgOrientationDirection(
            string raw,
            string localAxis,
            out VIZCore3D.NET.Data.Vector3D direction)
        {
            direction = null;
            if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrEmpty(localAxis))
                return false;

            string pattern = $@"\b{localAxis}\s+IS\s+([NESWUD])(?:\s+([+-]?\d+(?:\.\d+)?)\s+([NESWUD]))?";
            System.Text.RegularExpressions.Match match =
                System.Text.RegularExpressions.Regex.Match(
                    raw.ToUpperInvariant(),
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (!match.Success) return false;

            VIZCore3D.NET.Data.Vector3D primary;
            if (!TryGetMfgCardinalDirection(match.Groups[1].Value[0], out primary))
                return false;

            if (!match.Groups[2].Success || !match.Groups[3].Success)
            {
                direction = primary;
                return true;
            }

            float degrees;
            if (!float.TryParse(
                match.Groups[2].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out degrees))
                return false;

            VIZCore3D.NET.Data.Vector3D secondary;
            if (!TryGetMfgCardinalDirection(match.Groups[3].Value[0], out secondary))
                return false;

            double radians = degrees * Math.PI / 180.0;
            direction = new VIZCore3D.NET.Data.Vector3D(
                primary.X * (float)Math.Cos(radians) + secondary.X * (float)Math.Sin(radians),
                primary.Y * (float)Math.Cos(radians) + secondary.Y * (float)Math.Sin(radians),
                primary.Z * (float)Math.Cos(radians) + secondary.Z * (float)Math.Sin(radians));
            return TryNormalizeMfgVector(direction, out direction);
        }

        /// <summary>
        /// 방위 문자(N/E/S/W/U/D)를 받아 → 대응하는 월드 단위벡터를 돌려준다. N=+X, E=+Y, U=+Z. 그 외 문자면 false.
        /// </summary>
        private bool TryGetMfgCardinalDirection(
            char direction,
            out VIZCore3D.NET.Data.Vector3D vector)
        {
            switch (direction)
            {
                case 'N': vector = new VIZCore3D.NET.Data.Vector3D(1f, 0f, 0f); return true;
                case 'E': vector = new VIZCore3D.NET.Data.Vector3D(0f, 1f, 0f); return true;
                case 'S': vector = new VIZCore3D.NET.Data.Vector3D(-1f, 0f, 0f); return true;
                case 'W': vector = new VIZCore3D.NET.Data.Vector3D(0f, -1f, 0f); return true;
                case 'U': vector = new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f); return true;
                case 'D': vector = new VIZCore3D.NET.Data.Vector3D(0f, 0f, -1f); return true;
                default: vector = null; return false;
            }
        }

        /// <summary>
        /// 두 벡터를 받아 → 내적(float)을 돌려준다.
        /// </summary>
        private float DotMfgVector(
            VIZCore3D.NET.Data.Vector3D a,
            VIZCore3D.NET.Data.Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// 두 벡터를 받아 → 외적 벡터를 돌려준다.
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D CrossMfgVector(
            VIZCore3D.NET.Data.Vector3D a,
            VIZCore3D.NET.Data.Vector3D b)
        {
            return new VIZCore3D.NET.Data.Vector3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        /// <summary>
        /// 벡터와 배율을 받아 → 각 성분에 배율을 곱한 새 벡터를 돌려준다.
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D ScaleMfgVector(
            VIZCore3D.NET.Data.Vector3D vector,
            float scale)
        {
            return new VIZCore3D.NET.Data.Vector3D(
                vector.X * scale,
                vector.Y * scale,
                vector.Z * scale);
        }

        /// <summary>
        /// 두 벡터를 받아 → 앞에서 뒤를 뺀 차 벡터를 돌려준다.
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D SubtractMfgVector(
            VIZCore3D.NET.Data.Vector3D a,
            VIZCore3D.NET.Data.Vector3D b)
        {
            return new VIZCore3D.NET.Data.Vector3D(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z);
        }

        /// <summary>
        /// 벡터를 받아 → 길이 1로 정규화한 벡터를 out으로 돌려준다. null·NaN·무한대·길이가 거의 0이면 false.
        /// </summary>
        private bool TryNormalizeMfgVector(
            VIZCore3D.NET.Data.Vector3D vector,
            out VIZCore3D.NET.Data.Vector3D normalized)
        {
            normalized = null;
            if (vector == null) return false;
            if (float.IsNaN(vector.X) || float.IsInfinity(vector.X) ||
                float.IsNaN(vector.Y) || float.IsInfinity(vector.Y) ||
                float.IsNaN(vector.Z) || float.IsInfinity(vector.Z))
                return false;

            double lengthSquared =
                (double)vector.X * vector.X +
                (double)vector.Y * vector.Y +
                (double)vector.Z * vector.Z;
            if (lengthSquared < 1e-10 || double.IsNaN(lengthSquared) || double.IsInfinity(lengthSquared))
                return false;

            float length = (float)Math.Sqrt(lengthSquared);
            normalized = new VIZCore3D.NET.Data.Vector3D(
                vector.X / length,
                vector.Y / length,
                vector.Z / length);
            return true;
        }

        /// <summary>
        /// pose와 로컬축 이름을 받아 → 참조축 사용 중이면 해당 로컬 참조축을 Vertex3D로 돌려준다. 축 이름이 틀리거나 축이 비었으면 null.
        /// 참조축 미사용 pose도 null. 치수 그리기에서 측정축·오프셋축을 로컬 기준으로 넘길 때 쓴다.
        /// </summary>
        private VIZCore3D.NET.Data.Vertex3D GetMfgOrientationAxisVector(
            MfgViewPose pose,
            string localAxis)
        {
            if (pose == null || !pose.UseReferenceAxis || string.IsNullOrEmpty(localAxis))
                return null;

            VIZCore3D.NET.Data.Vector3D vector;
            switch (localAxis)
            {
                case "X": vector = pose.ReferenceAxisX; break;
                case "Y": vector = pose.ReferenceAxisY; break;
                case "Z": vector = pose.ReferenceAxisZ; break;
                default: return null;
            }

            return vector == null
                ? null
                : new VIZCore3D.NET.Data.Vertex3D(vector.X, vector.Y, vector.Z);
        }

        private sealed class MfgAxisDirectionBin
        {
            public double WeightedX;
            public double WeightedY;
            public double WeightedZ;
            public double TotalLength;
            public int LineCount;

            public MfgAxisVector Direction
            {
                get { return NormalizeMfgAxisVector(new MfgAxisVector(WeightedX, WeightedY, WeightedZ)); }
            }

            /// <summary>
            /// 방향 단위벡터와 선 길이를 받아 → 길이 가중 누적 방향에 더하고 선 개수·총 길이를 갱신한다.
            /// 기존 누적 방향과 반대면 부호를 뒤집어 같은 쪽으로 모은다.
            /// 주축 판정에서 5° 이내 같은 방향 군집의 대표 방향을 만드는 데 쓴다.
            /// </summary>
            public void Add(MfgAxisVector direction, double length)
            {
                if (LineCount > 0 && DotMfgAxisVector(Direction, direction) < 0.0)
                    direction = new MfgAxisVector(-direction.X, -direction.Y, -direction.Z);

                WeightedX += direction.X * length;
                WeightedY += direction.Y * length;
                WeightedZ += direction.Z * length;
                TotalLength += length;
                LineCount++;
            }
        }

        private sealed class MfgAxisDetectionResult
        {
            public bool Success;
            public string FailureReason = "";
            public int LineCount;
            public int DirectionGroupCount;
            public MfgAxisVector MainAxis;
            public double MainDirectionTotalLength;
            public double SecondDirectionTotalLength;
            public MfgAxisVector LongestLineAxis;
            public double LongestLineLength;
            public string NearestWorldAxis = "";
            public double DeviationDegrees;
            public double MainVsLongestDegrees;

            public bool IsTilted
            {
                get { return Success && DeviationDegrees > MfgAxisTiltToleranceDegrees; }
            }
        }

        /// <summary>
        /// 전체 Osnap 수집 때 이미 받은 원본으로 가공도 주축 판정을 함께 캐시한다.
        /// 반환값은 호출자가 기존 수집 흐름을 그대로 이어가도록 입력 목록을 그대로 돌려준다.
        /// </summary>
        private List<VIZCore3D.NET.Data.OsnapVertex3D> CacheMfgAxisDetection(
            int nodeIndex,
            List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList)
        {
            _mfgAxisDetectionCache[nodeIndex] = DetectMfgAxis(osnapList);
            return osnapList;
        }

        /// <summary>
        /// 노드 인덱스를 받아 → 캐시된 주축 판정 결과를 돌려주고, 없으면 Osnap을 조회해 판정한 뒤 캐시에 넣는다. cacheHit로 캐시 여부 반환.
        /// 가공도 참조축 진단 로그와 설치도 길이축 판정이 같이 쓴다. 캐시 miss면 GetOsnapPoint가 돌아 느리다.
        /// </summary>
        private MfgAxisDetectionResult GetMfgAxisDetection(int nodeIndex, out bool cacheHit)
        {
            MfgAxisDetectionResult cached;
            if (_mfgAxisDetectionCache.TryGetValue(nodeIndex, out cached))
            {
                cacheHit = true;
                return cached;
            }

            cacheHit = false;
            List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList = vizcore3d.Object3D.GetOsnapPoint(nodeIndex);
            MfgAxisDetectionResult detected = DetectMfgAxis(osnapList);
            _mfgAxisDetectionCache[nodeIndex] = detected;
            return detected;
        }

        /// <summary>
        /// LINE Osnap 방향을 5도 허용 범위로 군집화하고, 군집별 선 길이 합이 가장 큰 방향을 주축으로 판정한다.
        /// 단일 최장선 방향도 별도로 보존해 실제 모델 로그에서 두 기준을 비교할 수 있게 한다.
        /// </summary>
        private MfgAxisDetectionResult DetectMfgAxis(
            IEnumerable<VIZCore3D.NET.Data.OsnapVertex3D> osnapList)
        {
            var result = new MfgAxisDetectionResult();
            var bins = new List<MfgAxisDirectionBin>();
            double directionCosTolerance = Math.Cos(MfgAxisDirectionToleranceDegrees * Math.PI / 180.0);

            if (osnapList != null)
            {
                foreach (var osnap in osnapList)
                {
                    if (osnap == null ||
                        osnap.Kind != VIZCore3D.NET.Data.OsnapKind.LINE ||
                        osnap.Start == null || osnap.End == null)
                        continue;

                    var line = new MfgAxisVector(
                        osnap.End.X - osnap.Start.X,
                        osnap.End.Y - osnap.Start.Y,
                        osnap.End.Z - osnap.Start.Z);
                    double length = LengthMfgAxisVector(line);
                    if (length < MfgAxisMinimumLineLength) continue;

                    MfgAxisVector direction = CanonicalizeMfgAxisVector(
                        new MfgAxisVector(line.X / length, line.Y / length, line.Z / length));
                    result.LineCount++;

                    if (length > result.LongestLineLength)
                    {
                        result.LongestLineLength = length;
                        result.LongestLineAxis = direction;
                    }

                    MfgAxisDirectionBin targetBin = null;
                    foreach (MfgAxisDirectionBin bin in bins)
                    {
                        if (Math.Abs(DotMfgAxisVector(bin.Direction, direction)) >= directionCosTolerance)
                        {
                            targetBin = bin;
                            break;
                        }
                    }

                    if (targetBin == null)
                    {
                        targetBin = new MfgAxisDirectionBin();
                        bins.Add(targetBin);
                    }
                    targetBin.Add(direction, length);
                }
            }

            result.DirectionGroupCount = bins.Count;
            if (bins.Count == 0)
            {
                result.FailureReason = "유효한 LINE Osnap 없음";
                return result;
            }

            List<MfgAxisDirectionBin> orderedBins = bins
                .OrderByDescending(bin => bin.TotalLength)
                .ToList();
            MfgAxisDirectionBin mainBin = orderedBins[0];
            result.MainAxis = CanonicalizeMfgAxisVector(mainBin.Direction);
            result.MainDirectionTotalLength = mainBin.TotalLength;
            result.SecondDirectionTotalLength = orderedBins.Count > 1 ? orderedBins[1].TotalLength : 0.0;

            double absX = Math.Abs(result.MainAxis.X);
            double absY = Math.Abs(result.MainAxis.Y);
            double absZ = Math.Abs(result.MainAxis.Z);
            double nearestDot;
            if (absX >= absY && absX >= absZ)
            {
                result.NearestWorldAxis = "X";
                nearestDot = absX;
            }
            else if (absY >= absX && absY >= absZ)
            {
                result.NearestWorldAxis = "Y";
                nearestDot = absY;
            }
            else
            {
                result.NearestWorldAxis = "Z";
                nearestDot = absZ;
            }

            result.DeviationDegrees = RadiansToDegrees(Math.Acos(ClampMfgAxisDot(nearestDot)));
            result.MainVsLongestDegrees = RadiansToDegrees(Math.Acos(ClampMfgAxisDot(
                Math.Abs(DotMfgAxisVector(result.MainAxis, result.LongestLineAxis)))));
            result.Success = true;
            return result;
        }

        /// <summary>
        /// 부재를 받아 → LINE Osnap 기반 주축 판정과 ORIENTATION UDA 파싱값을 비교한 "참조축판정" 진단 로그 한 줄을 남긴다.
        /// 장면 코어가 Osnap 수집 직후 호출. 카메라·좌표계는 바꾸지 않는다.
        /// ORIENTATION이 유효한 축으로 파싱될 때만 그 각도를, 아니면 기하 판정을 기울어짐 근거로 표기한다.
        /// </summary>
        private void LogMfgAxisDetection(BOMData bom)
        {
            bool cacheHit;
            MfgAxisDetectionResult result = GetMfgAxisDetection(bom.Index, out cacheHit);
            string orientationRaw = GetUdaValue(bom.Index, "ORIENTATION");
            var (orientationAxis, orientationAngle) = ParseOrientation(bom.Index);
            bool orientationAvailable = !string.IsNullOrWhiteSpace(orientationRaw) &&
                                        !string.IsNullOrEmpty(orientationAxis);
            bool orientationTilted = Math.Abs(orientationAngle) > MfgAxisTiltToleranceDegrees;
            string decisionSource = orientationAvailable ? "ORIENTATION" : "GEOMETRY_FALLBACK";
            bool decisionTilted = orientationAvailable ? orientationTilted : result.IsTilted;

            if (!result.Success)
            {
                DiagLog($"[참조축판정] bom={bom.Index} name='{bom.Name}' source={(cacheHit ? "cache" : "sdk")} " +
                    $"decisionSource={decisionSource} decision={(decisionTilted ? "틀어짐" : "정상")} " +
                    $"geometryFailure={result.FailureReason} ORIENTATION='{orientationRaw}' " +
                    $"parsed={orientationAxis}/{orientationAngle:F1}° threshold={MfgAxisTiltToleranceDegrees:F1}°");
                return;
            }

            string message = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[참조축판정] bom={0} name='{1}' source={2} LINE={3} groups={4} " +
                "main=({5:F5},{6:F5},{7:F5}) sumLen={8:F2} secondLen={9:F2} " +
                "nearest={10} deviation={11:F3}° threshold={12:F1}° geometryResult={13} " +
                "longest=({14:F5},{15:F5},{16:F5}) length={17:F2} mainVsLongest={18:F3}° " +
                "ORIENTATION='{19}' parsed={20}/{21:F1}° decisionSource={22} decision={23}",
                bom.Index,
                bom.Name,
                cacheHit ? "cache" : "sdk",
                result.LineCount,
                result.DirectionGroupCount,
                result.MainAxis.X,
                result.MainAxis.Y,
                result.MainAxis.Z,
                result.MainDirectionTotalLength,
                result.SecondDirectionTotalLength,
                result.NearestWorldAxis,
                result.DeviationDegrees,
                MfgAxisTiltToleranceDegrees,
                result.IsTilted ? "틀어짐" : "정상",
                result.LongestLineAxis.X,
                result.LongestLineAxis.Y,
                result.LongestLineAxis.Z,
                result.LongestLineLength,
                result.MainVsLongestDegrees,
                orientationRaw,
                orientationAxis,
                orientationAngle,
                decisionSource,
                decisionTilted ? "틀어짐" : "정상");
            DiagLog(message);
        }

        /// <summary>
        /// 축 벡터를 받아 → 길이 1로 정규화한 벡터를 돌려준다. 길이가 0이면 영벡터.
        /// </summary>
        private static MfgAxisVector NormalizeMfgAxisVector(MfgAxisVector vector)
        {
            double length = LengthMfgAxisVector(vector);
            if (length <= 0.0) return new MfgAxisVector();
            return new MfgAxisVector(vector.X / length, vector.Y / length, vector.Z / length);
        }

        /// <summary>
        /// 축 벡터를 받아 → 절댓값이 가장 큰 성분이 양수가 되도록 부호를 맞춘 벡터를 돌려준다.
        /// 같은 선의 양쪽 방향을 하나로 취급하기 위한 정규 표현.
        /// </summary>
        private static MfgAxisVector CanonicalizeMfgAxisVector(MfgAxisVector vector)
        {
            double absX = Math.Abs(vector.X);
            double absY = Math.Abs(vector.Y);
            double absZ = Math.Abs(vector.Z);
            bool invert = (absX >= absY && absX >= absZ && vector.X < 0.0) ||
                          (absY > absX && absY >= absZ && vector.Y < 0.0) ||
                          (absZ > absX && absZ > absY && vector.Z < 0.0);
            return invert
                ? new MfgAxisVector(-vector.X, -vector.Y, -vector.Z)
                : vector;
        }

        /// <summary>
        /// 축 벡터를 받아 → 길이를 돌려준다.
        /// </summary>
        private static double LengthMfgAxisVector(MfgAxisVector vector)
        {
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        }

        /// <summary>
        /// 두 축 벡터를 받아 → 내적(double)을 돌려준다.
        /// </summary>
        private static double DotMfgAxisVector(MfgAxisVector left, MfgAxisVector right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        /// <summary>
        /// 내적값을 받아 → −1~1 범위로 잘라 돌려준다. Acos에 넣기 전 부동소수 오차를 막는다.
        /// </summary>
        private static double ClampMfgAxisDot(double value)
        {
            return Math.Max(-1.0, Math.Min(1.0, value));
        }

        /// <summary>
        /// 라디안을 받아 → 도 단위로 바꿔 돌려준다.
        /// </summary>
        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        #endregion
    }
}
