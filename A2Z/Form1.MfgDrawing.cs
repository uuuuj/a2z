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
            public int SuccessPdfs;               // 성공 저장 PDF 수
            public int InsufficientBomPdfs;       // BOM 부족 상태에서 저장된 PDF 수 (성공 PDF 중 일부)
            public bool TemplateMissing;          // 가공도 엑셀 템플릿 누락 → PDF 0개
            public int BomRows;                   // 실제 snapshot BOM 행 수
            public int ExpectedBomRows;           // 예상 BOM 행 수 = Min(allMfgBomIndices.Count, 15)
            public List<string> Warnings = new List<string>();   // 사용자에게 보일 경고 텍스트

            public bool HasIssues => Warnings.Count > 0 || InsufficientBomPdfs > 0 || TemplateMissing;
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
        /// <returns>각 행의 8컬럼 문자열 배열 리스트. Row 0(요약행) 제외.</returns>
        private List<string[]> SnapshotBomRows()
        {
            var rows = new List<string[]>();
            if (lvDrawingBOMInfo.Items.Count <= 1) return rows;

            for (int i = 1; i < lvDrawingBOMInfo.Items.Count; i++)
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
            MfgPage page, int totalPages, string struName, List<string[]> bomSnapshot)
        {
            var data = new Dictionary<int, string>();

            // 빈 슬롯 선초기화 — 미치환 {Input_N} 태그 노출 방지 (Codex 3차)
            //   ⚠ Input 번호는 199까지만 — 200 이상(dict 키·템플릿 태그 모두)은 SDK 내부 배열 범위를 넘겨
            //   import 시 메모리 손상 → 직후 캡처 AccessViolation (2026-07-20 실측 격리). View도 동일하게 1~7만.
            //   ""(빈칸) = RemoveEmptyTemplateBorders의 괘선 제거 대상, " "(공백) = 내용 있음 위장으로 괘선 보존.
            //   제거 대상: BOM(4~163)·Note(164)·Rev 위 4행(170~193)·부재명(195~199, 빈 밴드) — 제작도와 동일 정책(2026-07-21).
            //   보존: PAINT/DP/TAG(165~169)·194(Rev 첫 기재행의 REV. 칸 — 가공도는 195~199를 부재명이 사용).
            for (int k = 1; k <= 199; k++)
                data[k] = ((k >= 4 && k <= 164) || (k >= 170 && k <= 193) || k >= 195) ? "" : " ";

            // ── 도면정보 ──
            data[1] = "CEDAR FLNG";  // TODO: 프로젝트명 (T-043 tableInfo 결정 후)
            data[2] = "SN2688";       // TODO: 선박번호
            data[3] = totalPages > 1
                ? $"가공도 ({page.PageIdx}/{totalPages})"
                : "가공도";

            // ── 부재명 5칸 (Input_195~Input_199, 각 View 왼쪽 라벨) ──
            //   200~204 대역은 SDK 한계로 폐기(위 주석) → 가공도에서 항상 빈칸인 Rev 표 마지막 행
            //   태그(195~199)를 템플릿에서 제거하고 그 번호를 부재명이 사용 (2026-07-20).
            for (int i = 0; i < page.Rows.Count && i < 5; i++)
            {
                var sheet = page.Rows[i];
                if (sheet.MemberIndices.Count == 0) continue;
                var bom = bomList.FirstOrDefault(b => b.Index == sheet.MemberIndices[0]);
                if (bom == null) continue;
                data[195 + i] = bom.Name ?? "";
            }

            // ── 우측 BOM 표 8컬럼 × 20행 (Input_4~Input_163, snapshot 사용) ──
            //   제작도(제작도_도면_1)와 완전히 동일한 슬롯 체계 — 열별 20연속.
            int bomMapped = 0;
            if (bomSnapshot != null)
            {
                int n = Math.Min(bomSnapshot.Count, 20);
                for (int i = 0; i < n; i++)
                {
                    string[] row = bomSnapshot[i];
                    data[4 + i]   = row[0];   // NO
                    data[24 + i]  = row[1];   // ITEM
                    data[44 + i]  = row[2];   // MATERIAL
                    data[64 + i]  = row[3];   // SIZE
                    data[84 + i]  = row[4];   // Q'TY
                    data[104 + i] = row[5];   // T/W
                    data[124 + i] = row[6];   // MA
                    data[144 + i] = row[7];   // FA
                }
                bomMapped = n;
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
            Clear2DView();
            vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);
            vizcore3d.Drawing2D.View.SetSelectCanvas(1);
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
        /// PDF 파일명 충돌 방지 + sanitize + 길이 clamp + MAX_PATH 진단 (v7 Codex 4차).
        /// Form1.DrawingSheets.cs SanitizeFileName 재사용.
        /// </summary>
        private string MakeUniquePdfPath(string saveDir, string struName, int pageIdx, int totalPages, int struIndex)
        {
            string safeName = SanitizeFileName(struName ?? "manual");
            if (safeName.Length > 40) safeName = safeName.Substring(0, 40);

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string baseName = $"{safeName}_가공도_p{pageIdx}of{totalPages}_S{struIndex}_{ts}.pdf";
            string path = Path.Combine(saveDir, baseName);

            int n = 1;
            while (File.Exists(path) && n < 1000)
            {
                string alt = $"{Path.GetFileNameWithoutExtension(baseName)}_{n}.pdf";
                path = Path.Combine(saveDir, alt);
                n++;
            }
            if (path.Length > 240)
                DiagLog($"[MakeUniquePdfPath] WARN path 길이 {path.Length} (Windows MAX_PATH 260 임박): {path}");
            return path;
        }

        /// <summary>
        /// [임시 §5-1] 카메라 ± 반영 검증 — 같은 부재를 PLUS(위 슬롯)/MINUS(아래 슬롯)로 캡처해 별도 PDF 1장 저장.
        /// 두 그림이 완전히 같으면 캡처가 카메라 ±를 무시(옛 은선 캡처와 동일), 좌우 반전·선 차이가 있으면 반영.
        /// 홀 있는 부재를 우선 선택(홀 위치 반전이 눈에 잘 보임). 검증 완료 후 메서드째 제거 예정.
        /// </summary>
        private void RunMfgCameraSignProbe(
            List<DrawingSheetData> mfgSheets, string saveDir, string xlsxPath,
            ref Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea> viewAreasCache)
        {
            try
            {
                BOMData bom = null;
                foreach (var s in mfgSheets)
                {
                    if (s.MemberIndices.Count == 0) continue;
                    var b = bomList.FirstOrDefault(x => x.Index == s.MemberIndices[0]);
                    if (b == null) continue;
                    if (bom == null) bom = b;
                    if (b.Holes != null && b.Holes.Count > 0) { bom = b; break; }
                }
                if (bom == null) return;

                // 카메라 축 = 실사용 축과 동일 규칙: 최장축 Z면 X, 아니면 Z
                float sx = bom.MaxX - bom.MinX, sy = bom.MaxY - bom.MinY, sz = bom.MaxZ - bom.MinZ;
                string longest = (sz >= sx && sz >= sy) ? "Z" : (sx >= sy ? "X" : "Y");
                string camAxis = longest == "Z" ? "X" : "Z";
                var dirPlus = camAxis == "X"
                    ? VIZCore3D.NET.Data.CameraDirection.X_PLUS
                    : VIZCore3D.NET.Data.CameraDirection.Z_PLUS;
                var dirMinus = camAxis == "X"
                    ? VIZCore3D.NET.Data.CameraDirection.X_MINUS
                    : VIZCore3D.NET.Data.CameraDirection.Z_MINUS;

                ResetCanvasForMfgPage();
                var data = new Dictionary<int, string>();
                for (int k = 1; k <= 199; k++) data[k] = "";   // 200 이상 금지 (SDK 메모리 손상 — BuildMfgPageData 주석)
                data[3] = "카메라 ± 검증 (위=PLUS / 아래=MINUS)";
                data[195] = bom.Name ?? "";   // 신 템플릿 부재명 슬롯 (195~199 대역)
                var probeImageMapping = new Dictionary<int, string[]>
                {
                    { 1, new[] { ResolveDrawingAssetPath("North_Arrow.png"), ResolveDrawingAssetPath("North_Arrow.png") } },
                    { 2, new[] { ResolveDrawingAssetPath("ISO_North_Arrow.png"), ResolveDrawingAssetPath("ISO_North_Arrow.png") } },
                    { 3, new[] { ResolveDrawingAssetPath("Logo.png"), ResolveDrawingAssetPath("Logo.png") } },
                };
                vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data, probeImageMapping);
                EnsureViewAreasCache(ref viewAreasCache, xlsxPath);

                var area = viewAreasCache[1];
                float halfH = area.Height / 2f;
                var targets = new List<int> { bom.Index };

                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(targets, true);
                vizcore3d.View.SilhouetteEdge = false;

                foreach (var probe in new[]
                {
                    (dir: dirPlus,  label: "PLUS",  y: area.Y),
                    (dir: dirMinus, label: "MINUS", y: area.Y + halfH),
                })
                {
                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
                    vizcore3d.View.MoveCamera(probe.dir);
                    vizcore3d.View.FlyToObject3d(targets, 1.25f);
                    System.Windows.Forms.Application.DoEvents();
                    var pose = new MfgViewPose { ViewDirection = camAxis, LongestAxis = longest };
                    bool ok = CaptureMfgSceneToViewArea(0, bom, pose, area.X, probe.y, area.Width, halfH,
                        $"±검증 {probe.label}", out int probeObjId);
                    DiagLog($"[CamSignProbe] bom={bom.Index} axis={camAxis} {probe.label} ok={ok} objId={probeObjId}");
                }

                vizcore3d.Drawing2D.Render();
                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                string pdfPath = Path.Combine(saveDir, $"카메라부호검증_{DateTime.Now:HHmmss}.pdf");
                vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                DiagLog($"[CamSignProbe] 저장: {pdfPath} — 두 그림 동일이면 ± 무시 / 좌우 반전·차이면 반영");
            }
            catch (Exception ex)
            {
                DiagLog($"[CamSignProbe] 실패(본 출력 계속): {ex.Message}");
            }
        }

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

            float fitRatio = Math.Min(areaWidth / objW, areaHeight / objH);
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
                areaY + areaHeight / 2f);

            // 보조선·치수를 '확정된 실측 배율(newScale)'로 지금 그린다 — 캔버스 절대 오프셋이 부재·뷰 무관 일정.
            //   (코어는 pose.PendingDims로 목록만 수집) 설계 §4.4 v2-c, 2026-07-01
            try { DrawMfgDimsAtScale(pose, bom, newScale); }
            catch (Exception ex) { DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} WARN DimDraw 실패: {ex.Message}"); }

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
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(6.0f);   // 홀/슬롯 풍선 글자 키움 (3.5 → 6)
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);
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
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(10f);   // 제작도(DrawingSheets.cs:1638)와 동일 — 라이브 가공도 누락분
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
        /// 가공도 보조선·치수를 '모델 2D 캡처 + RescaleObject 후 확정된 실측 배율(newScale)'로 그린다.
        /// 추정 스케일이 아닌 실측을 써야 보조선 길이 = 캔버스 절대값(2/4mm)으로 부재·뷰 무관 일정해짐 (설계 §4.4 v2-c).
        /// pose.PendingDims = BuildMfgSceneCore/BuildEaSecondaryScene가 수집한 그릴 목록(offset 미적용).
        /// 캡처 간 측정 격리: EA 1차/2차가 각자 캡처 직전에 호출되므로, 직전 뷰의 3D 측정·보조선을 먼저 비운다
        /// (이미 2D로 변환됐으므로 3D 원본 제거는 무해).
        /// </summary>
        private void DrawMfgDimsAtScale(MfgViewPose pose, BOMData bom, float newScale)
        {
            if (pose == null || pose.PendingDims == null || pose.PendingDims.Count == 0) return;
            if (newScale <= 0f || float.IsNaN(newScale) || float.IsInfinity(newScale)) return;

            // 직전 뷰(EA 1차)의 3D 측정·보조선 제거 — 이 뷰 측정만 2D로 변환되게.
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            pose.ShapeDrawingIds.Clear();

            // 실측 배율로 캔버스 절대 오프셋(가공도 6/12mm) 역산 — 제작도와 동일 정책(ComputeCanvasAbsoluteOffsets).
            ComputeCanvasAbsoluteOffsets(newScale, out float baseOff, out float lvlSp, out _,
                MfgCanvasBaseOff, MfgCanvasLvlSp);

            // 작은 치수 단 승격 (사용자 사양 2026-07-03): 텍스트가 안 들어가는 작은 체인 치수는
            //   텍스트를 옮기지 않고 치수선째 위 단(Level 1)으로 올린다 — 제작도와 동일 규칙(뷰 최대/26, max>100mm).
            //   승격이 생기면 아래 maxLevel 계산이 전체 치수를 자동으로 3단(off2)으로 민다.
            Func<MfgPendingDim, float> axisDist = pd =>
                pd.Axis == "X" ? Math.Abs(pd.End.X - pd.Start.X)
                : pd.Axis == "Y" ? Math.Abs(pd.End.Y - pd.Start.Y)
                : Math.Abs(pd.End.Z - pd.Start.Z);
            float maxPdDist = 0f;
            foreach (var pd in pose.PendingDims)
            {
                float dpd = axisDist(pd);
                if (dpd > maxPdDist) maxPdDist = dpd;
            }
            int promoted = 0;
            if (maxPdDist > 100f)
            {
                float smallTh = maxPdDist / 26f;
                foreach (var pd in pose.PendingDims)
                    if (pd.Level == 0 && axisDist(pd) <= smallTh) { pd.Level = 1; promoted++; }
            }

            int maxLevel = pose.PendingDims.Any(d => d.Level == 1) ? 2 : 1;
            float off0 = baseOff, off1 = baseOff + lvlSp, off2 = baseOff + lvlSp * maxLevel;
            // 보조선 시작 gap도 종이 절대 기준(2mm)으로 — 오프셋과 동일하게 실측 배율 역산 (2026-07-03).
            //   옛 모델좌표 고정 10mm는 부재·축척마다 종이 gap이 들쭉날쭉했음.
            float extGap = MfgCanvasExtGap / newScale;

            var extLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
            // 2단(Level 1) 텍스트 슬라이드 — 제작도와 동일 사양(종이 2.5mm, 치수선 방향만 SDK 반영).
            //   부호는 실제 카메라 투영 헬퍼: 가로(길이축) 치수=화면 오른쪽(MfgHeightToRight),
            //   세로(폭) 치수=화면 위(MfgAxisUpPositive). 호출 시점 카메라 = 캡처 시점과 동일(roll 원복 전).
            float lvl2SlideMag = MfgLvl2TextSlideCanvas / newScale;
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
                DrawDimension(pd.Start, pd.End, pd.Axis, off,
                    bom.MinX, bom.MinY, bom.MinZ, pose.ViewDirection, extLines,
                    bom.MaxX, bom.MaxY, bom.MaxZ, pd.PosOff, false, extGap, slide);
            }
            if (extLines.Count > 0)
            {
                int shapeId = vizcore3d.ShapeDrawing.AddLine(extLines, -1, System.Drawing.Color.Blue, 0.15f, true);
                if (shapeId >= 0) pose.ShapeDrawingIds.Add(shapeId);
            }
            DiagLog($"[DrawMfgDims] bom={bom.Index} view={pose.ViewDirection} newScale={newScale:F4} " +
                $"off0={off0:F2} off1={off1:F2} off2={off2:F2} extGap={extGap:F2} dims={pose.PendingDims.Count} promoted={promoted}");
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

        private MfgViewPose BuildEaSecondaryScene(
            BOMData bom,
            MfgViewPose primaryPose,
            bool secondaryAtTop = true)
        {
            var pose = new MfgViewPose { LongestAxis = primaryPose.LongestAxis };

            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

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
            style.AlignDistanceTextMargine = 3;
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
            VIZCore3D.NET.Data.TemplateViewArea area)
        {
            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

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

                var pose = BuildMfgSceneCore(bom.Index, isEA);

                // DASH_LINE(은선 점선) 렌더모드 제거 — 은선 없는 캡처로 전환해 무의미 (2026-07-03).
                // 실루엣 엣지 끔 — 켜면 SDK가 모든 모서리를 균일 굵기로 통일해 모델선(2.0)이
                //   치수선·보조선과 구분이 사라진다(제작도는 캡처 직전 실루엣 미사용). 2026-06-29
                vizcore3d.View.SilhouetteEdge = false;

                // ── EA 두 뷰 상하 슬롯 — 스왑 판정은 코어(5-2a 직후)에서 수행 (길이 치수 배치가 의존) ──
                bool swapViews = isEA && pose.SwapViews;
                float primaryY = swapViews ? area.Y + viewHeight + viewGap : area.Y;
                float secondaryY = swapViews ? area.Y : area.Y + viewHeight + viewGap;

                // 가로 배치 — 임시 캡처로 실제 세로/가로 측정 후 세로면 화면축 90° 회전.
                //   (DoEvents 제거 — 격리 5단계 2026-07-21: 네이티브 캡처 전후 메시지 펌프가 크래시 용의)
                float primRoll = ProbeAndRollLandscape(bom.Index, 1.25f);

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

                if (isEA)
                {
                    try
                    {
                        var secondaryPose = BuildEaSecondaryScene(
                            bom, pose, !swapViews);   // 2차 뷰 슬롯: 기본=위, 스왑 시=아래

                        // 2차 뷰도 동일: 임시 캡처로 세로/가로 측정 후 세로면 90° 회전 (DoEvents 제거 — 격리 5단계)
                        float secRoll = ProbeAndRollLandscape(bom.Index, 1.25f);

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
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} WARN EA secondary 실패 — primary 유지: {ex.Message}");
                    }
                }

                success = true;
                return true;
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
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
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

        // [임시 §5-1] 카메라 ± 반영 검증 프로브 스위치 — 설계 docs/리팩토링/가공도-EA-카메라-넓은면-정규화.md.
        //   새 단면 캡처가 MoveCamera의 PLUS/MINUS를 반영하는지 사내 1회 확인용. 검증 후 프로브째 제거.
        //   2026-07-20 OFF: 신 템플릿에서 가공도 AccessViolation이 프로브 캡처 지점에서 지속(템플릿 규칙 정리 후에도)
        //   → 크래시 격리 위해 비활성. 본 페이지 루프가 통과하는지로 프로브 vs 캡처API 판별.
        private const bool MfgCameraSignProbeEnabled = false;

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
                    // NodeHoleItem 실제 타입 (빌드 역추론): Center=Vector3D, CircleCenter=List<Vector3D>, Size=Vector3D, Radius=float
                    var ccPts = nh.CircleCenter;
                    int ccN = ccPts?.Count ?? 0;

                    // [실측 로그] 실제 구조 확인용 — 슬롯 매핑은 이 로그를 보고 보정 예정
                    string ccStr = (ccN > 0) ? string.Join(" ", ccPts.Select(p => $"({p.X:F1},{p.Y:F1},{p.Z:F1})")) : "-";
                    DiagLog($"[홀API] node={nodeIndex} type={nh.HoleType} Radius={nh.Radius:F2} " +
                        $"Center=({nh.Center.X:F1},{nh.Center.Y:F1},{nh.Center.Z:F1}) " +
                        $"Size=({nh.Size.X:F1},{nh.Size.Y:F1},{nh.Size.Z:F1}) " +
                        $"CircleCenterN={ccN}[{ccStr}]");

                    if (nh.HoleType == VIZCore3D.NET.Data.NodeHoleItem.NodeHoleType.CIRCLE)
                    {
                        holes.Add(new HoleInfo
                        {
                            Diameter = nh.Radius * 2f,
                            CenterX = nh.Center.X,
                            CenterY = nh.Center.Y,
                            CenterZ = nh.Center.Z,
                            CylinderBodyIndex = nh.NodeIndex
                        });
                    }
                    else if (nh.HoleType == VIZCore3D.NET.Data.NodeHoleItem.NodeHoleType.SLOT_HOLE)
                    {
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
                            CenterZ = nh.Center.Z
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
        /// 가공도 공통 3D 장면 생성 코어.
        /// 미리보기와 PDF 행 렌더링이 공통으로 사용한다.
        /// 부재 격리·BBox·축 판별·카메라·Osnap·치수·풍선(Hole/SlotHole/EarthBoss) 생성.
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

            // ── 4. ORIENTATION UDA 기반 카메라 회전 ──
            var (orientAxis_saved, orientAngle_saved) = ParseOrientation(bom.Index);
            ApplyOrientationRotation(bom.Index, viewDirection);
            pose.OrientationAxis = orientAxis_saved;
            pose.OrientationAngle = orientAngle_saved;

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
                var osnapListMfg = vizcore3d.Object3D.GetOsnapPoint(bom.Index);

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
            bool isLShape = false;
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
                        case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_MINUS); break;
                        case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_MINUS); break;
                        default:  vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_MINUS); break;
                    }
                    ApplyOrientationRotation(bom.Index, viewDirection);
                }

                isEAUse180 = use180;
                isAboveWider = false;
                isLShape = true;
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
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();

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
                    mfgStyle.AlignDistanceTextMargine = 3;
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

            // ── 8. 풍선 4분면 배치 (B1b2 2026-05-19) ──
            //   자동 함수 본체 L1518~L1850 추출. 색상 Cyan → Blue 통일 (수동 스타일).
            //   Note.Clear()/noteIds.Clear()는 어댑터(B2/B3)에서 제거 — 코어는 vizcore3d.Review.Note.Add 호출.
            // 8. 풍선 배치 — 4분면 가상선 방식 + 체인치수 겹침 방지

            // 뷰 방향별 축 매핑 (hAxis=화면 수평, vAxis=화면 수직, dAxis=깊이)
            int bHAxis_m, bVAxis_m, bDAxis_m;
            switch (viewDirection)
            {
                case "X": bHAxis_m = 1; bVAxis_m = 2; bDAxis_m = 0; break; // H=Y, V=Z, D=X
                case "Y": bHAxis_m = 0; bVAxis_m = 2; bDAxis_m = 1; break; // H=X, V=Z, D=Y
                default:  bHAxis_m = 0; bVAxis_m = 1; bDAxis_m = 2; break; // H=X, V=Y, D=Z
            }

            float[] mfgMinArr = { bom.MinX, bom.MinY, bom.MinZ };
            float[] mfgMaxArr = { bom.MaxX, bom.MaxY, bom.MaxZ };
            float modelMinH_m = mfgMinArr[bHAxis_m];
            float modelMaxH_m = mfgMaxArr[bHAxis_m];
            float modelMinV_m = mfgMinArr[bVAxis_m];
            float modelMaxV_m = mfgMaxArr[bVAxis_m];

            // ── 체인치수 실제 끝단 좌표 계산 ──
            float dimExtMinH_m = modelMinH_m;
            float dimExtMaxH_m = modelMaxH_m;
            float dimExtMinV_m = modelMinV_m;
            float dimExtMaxV_m = modelMaxV_m;

            if (hasDimensions)
            {
                // Osnap에서 추출된 치수 데이터가 있으면 실제 치수선 끝단 추적
                float tolerance_m = 0.5f;
                var mergedPts_m = MergeCoordinates(mfgOsnapWithNames, tolerance_m);
                List<string> visAxes_m = new List<string>();
                switch (viewDirection)
                {
                    case "X": visAxes_m.Add("Y"); visAxes_m.Add("Z"); break;
                    case "Y": visAxes_m.Add("X"); visAxes_m.Add("Z"); break;
                    default:  visAxes_m.Add("X"); visAxes_m.Add("Y"); break;
                }
                var allMfgDims = new List<ChainDimensionData>();
                foreach (var ax in visAxes_m)
                    allMfgDims.AddRange(AddChainDimensionByAxis(mergedPts_m, ax, tolerance_m, viewDirection));

                // 축별 오프셋 방향 (이미 계산된 mfgAxisPosOff 활용 가능하지만, 안전을 위해 재참조)
                float mfgCX = (bom.MinX + bom.MaxX) / 2f;
                float mfgCY = (bom.MinY + bom.MaxY) / 2f;
                float mfgCZ = (bom.MinZ + bom.MaxZ) / 2f;

                // T-005: 중앙에서 가장 먼 Osnap 쪽이 외곽
                var mfgAxisPosOff_m = new Dictionary<string, bool>();
                foreach (var grp in allMfgDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                {
                    string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                    float cv2 = offAxis == "X" ? mfgCX : offAxis == "Y" ? mfgCY : mfgCZ;
                    var values = grp.SelectMany(d => new[]
                    {
                        GetAxisValue(d.StartPoint, offAxis),
                        GetAxisValue(d.EndPoint, offAxis)
                    });
                    mfgAxisPosOff_m[grp.Key] = ComputePositiveOffsetByOsnapExtreme(values, cv2);
                }

                // EA 앵글: 체인치수 방향 오버라이드 (풍선 위치 계산용)
                if (isEA)
                {
                    if (mfgAxisPosOff_m.ContainsKey(pose.LongestAxis))
                        mfgAxisPosOff_m[pose.LongestAxis] = !isAboveWider;
                    foreach (string ax in new List<string>(mfgAxisPosOff_m.Keys))
                    {
                        if (ax != pose.LongestAxis)
                            mfgAxisPosOff_m[ax] = isLShape;
                    }
                }

                // 모델 가시 축 최소 크기 → 작은 모델이면 보조선 오프셋 50% 축소
                float visExt1_m = 0f, visExt2_m = 0f;
                switch (viewDirection)
                {
                    case "X": visExt1_m = bom.MaxY - bom.MinY; visExt2_m = bom.MaxZ - bom.MinZ; break;
                    case "Y": visExt1_m = bom.MaxX - bom.MinX; visExt2_m = bom.MaxZ - bom.MinZ; break;
                    default:  visExt1_m = bom.MaxX - bom.MinX; visExt2_m = bom.MaxY - bom.MinY; break;
                }
                float minVisExt_m = Math.Min(visExt1_m, visExt2_m);
                float offFactor_m = (minVisExt_m < 100f) ? 0.5f : 1.0f;

                float mfgOff1 = 100.0f * offFactor_m, mfgOff2 = 200.0f * offFactor_m;
                float maxTotalDist_m = 0f;
                foreach (var td in allMfgDims.Where(d => d.IsTotal && d.IsVisible))
                {
                    float dist2 = 0f;
                    switch (td.Axis)
                    {
                        case "X": dist2 = Math.Abs(td.EndPoint.X - td.StartPoint.X); break;
                        case "Y": dist2 = Math.Abs(td.EndPoint.Y - td.StartPoint.Y); break;
                        case "Z": dist2 = Math.Abs(td.EndPoint.Z - td.StartPoint.Z); break;
                    }
                    if (dist2 > maxTotalDist_m) maxTotalDist_m = dist2;
                }
                float mfgTotalOff_m = (maxTotalDist_m > 1000.0f ? 300.0f : 250.0f) * offFactor_m;

                foreach (var dim in allMfgDims.Where(d => d.IsVisible))
                {
                    if (isEA && reserveLongestAxisForSecondary && dim.Axis == pose.LongestAxis)
                        continue;

                    float dimOff;
                    if (dim.IsTotal)
                        dimOff = mfgTotalOff_m;
                    else if (dim.DisplayLevel > 0)
                        dimOff = mfgOff2;
                    else
                        dimOff = mfgOff1;

                    string offAxis = GetRemainingAxis(viewDirection, dim.Axis);
                    bool posOff = mfgAxisPosOff_m.ContainsKey(dim.Axis) && mfgAxisPosOff_m[dim.Axis];
                    float baseline2 = 0;
                    switch (offAxis)
                    {
                        case "X": baseline2 = posOff ? bom.MaxX : bom.MinX; break;
                        case "Y": baseline2 = posOff ? bom.MaxY : bom.MinY; break;
                        case "Z": baseline2 = posOff ? bom.MaxZ : bom.MinZ; break;
                    }
                    float dimLinePos = posOff ? (baseline2 + dimOff) : (baseline2 - dimOff);

                    int offAxisIdx = offAxis == "X" ? 0 : (offAxis == "Y" ? 1 : 2);
                    if (offAxisIdx == bHAxis_m)
                    {
                        dimExtMinH_m = Math.Min(dimExtMinH_m, dimLinePos);
                        dimExtMaxH_m = Math.Max(dimExtMaxH_m, dimLinePos);
                    }
                    else if (offAxisIdx == bVAxis_m)
                    {
                        dimExtMinV_m = Math.Min(dimExtMinV_m, dimLinePos);
                        dimExtMaxV_m = Math.Max(dimExtMaxV_m, dimLinePos);
                    }

                    // 치수선 자체의 H/V 범위
                    float[] dimStartArr = { dim.StartPoint.X, dim.StartPoint.Y, dim.StartPoint.Z };
                    float[] dimEndArr = { dim.EndPoint.X, dim.EndPoint.Y, dim.EndPoint.Z };
                    dimExtMinH_m = Math.Min(dimExtMinH_m, Math.Min(dimStartArr[bHAxis_m], dimEndArr[bHAxis_m]));
                    dimExtMaxH_m = Math.Max(dimExtMaxH_m, Math.Max(dimStartArr[bHAxis_m], dimEndArr[bHAxis_m]));
                    dimExtMinV_m = Math.Min(dimExtMinV_m, Math.Min(dimStartArr[bVAxis_m], dimEndArr[bVAxis_m]));
                    dimExtMaxV_m = Math.Max(dimExtMaxV_m, Math.Max(dimStartArr[bVAxis_m], dimEndArr[bVAxis_m]));
                }
            }

            // ── 가상 사각형 경계선: 체인치수 끝단 바깥에 풍선 배치 ──
            float dimMargin_m = 30f;
            float rectLeft_m  = dimExtMinH_m - dimMargin_m;
            float rectRight_m = dimExtMaxH_m + dimMargin_m;

            float modelSpan_m = Math.Max(modelMaxH_m - modelMinH_m, modelMaxV_m - modelMinV_m);
            float balloonSpacing_m = Math.Max(20f, modelSpan_m * 0.04f);

            float textGap_m = Math.Max(4f, modelSpan_m * 0.006f);
            Func<string, (float w, float h)> mfgEstTextSize = (text) =>
            {
                float charWidth = Math.Max(3f, modelSpan_m * 0.005f);
                float lineHeight = Math.Max(7f, modelSpan_m * 0.009f);
                return (text.Length * charWidth + textGap_m, lineHeight + textGap_m);
            };

            // --- 풍선 항목 수집 ---
            List<(float originH, float originV, float depthVal, string text, Color color,
                  float arrowX, float arrowY, float arrowZ)> mfgBalloonEntries =
                new List<(float, float, float, string, Color, float, float, float)>();

            // EarthBoss 풍선 수집 (UDA PURPOSE=EBOS)
            if (string.Equals((bom.Purpose ?? "").Trim(), "EBOS",
                StringComparison.OrdinalIgnoreCase))
            {
                float[] earthBossArr = { bom.CenterX, bom.CenterY, bom.CenterZ };
                mfgBalloonEntries.Add((
                    earthBossArr[bHAxis_m], earthBossArr[bVAxis_m], earthBossArr[bDAxis_m],
                    "EarthBoss", Color.Blue,
                    bom.CenterX, bom.CenterY, bom.CenterZ));
            }

            // 가공도 풍선: 부재별로 GetNodeHoleInfo API 직접 호출 (최신 결과 보장).
            //   bom.Holes/SlotHoles도 2026-06-23부터 같은 API로 채워짐(DetectHoles) — 휴리스틱 폐지.
            GetMfgHolesFromApi(bom.Index, out var mfgApiHoles, out var mfgApiSlots);

            // 홀 풍선 수집 (API 추출)
            if (mfgApiHoles.Count > 0)
            {
                try
                {
                    var mfgHoleGroups = mfgApiHoles.GroupBy(h => Math.Round(h.Diameter, 1));
                    foreach (var grp in mfgHoleGroups)
                    {
                        int hCount = grp.Count();
                        string holeText = hCount > 1 ? $"\u00d8{grp.Key:F1} * {hCount}개" : $"\u00d8{grp.Key:F1}";
                        var hole = grp.First();
                        float[] holeArr = { hole.CenterX, hole.CenterY, hole.CenterZ };
                        float oH = holeArr[bHAxis_m];
                        float oV = holeArr[bVAxis_m];
                        float depthVal = holeArr[bDAxis_m];
                        mfgBalloonEntries.Add((oH, oV, depthVal, holeText, Color.FromArgb(0, 160, 0),
                            hole.CenterX, hole.CenterY, hole.CenterZ));
                    }
                }
                catch { }
            }

            // 슬롯홀 풍선 수집 (API 추출)
            if (mfgApiSlots.Count > 0)
            {
                try
                {
                    var slotGroups = mfgApiSlots.GroupBy(s =>
                        $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                    foreach (var grp in slotGroups)
                    {
                        var slot = grp.First();
                        int sCount = grp.Count();
                        float slotWidth = slot.Radius * 2f;
                        string slotText = sCount > 1
                            ? $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0}) * {sCount}개"
                            : $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})";
                        float[] slotArr = { slot.CenterX, slot.CenterY, slot.CenterZ };
                        float oH = slotArr[bHAxis_m];
                        float oV = slotArr[bVAxis_m];
                        float depthVal = slotArr[bDAxis_m];
                        mfgBalloonEntries.Add((oH, oV, depthVal, slotText, Color.FromArgb(180, 0, 180),
                            slot.CenterX, slot.CenterY, slot.CenterZ));
                    }
                }
                catch { }
            }

            // --- 풍선 일괄 배치 (4분면 가상선 방식 + 체인치수 겹침 방지) ---
            float modelCenterH_m = (modelMinH_m + modelMaxH_m) / 2f;
            float modelCenterV_m = (modelMinV_m + modelMaxV_m) / 2f;

            // 0=왼쪽위, 1=왼쪽아래, 2=오른쪽위, 3=오른쪽아래
            var mfgSortedBalloons = new List<(int quadrant, float originH, float originV, float depthVal,
                string text, Color color, float arrowX, float arrowY, float arrowZ, float sortKey)>();

            foreach (var entry in mfgBalloonEntries)
            {
                bool isLeft = entry.originH <= modelCenterH_m;
                bool isTop  = entry.originV >= modelCenterV_m;

                int quadrant;
                float sortKey;
                if (isLeft && isTop)       { quadrant = 0; sortKey = -entry.originV; }
                else if (isLeft && !isTop)  { quadrant = 1; sortKey = entry.originV; }
                else if (!isLeft && isTop)  { quadrant = 2; sortKey = -entry.originV; }
                else                        { quadrant = 3; sortKey = entry.originV; }

                mfgSortedBalloons.Add((quadrant, entry.originH, entry.originV, entry.depthVal,
                    entry.text, entry.color, entry.arrowX, entry.arrowY, entry.arrowZ, sortKey));
            }

            mfgSortedBalloons.Sort((a, b) =>
            {
                int sc = a.quadrant.CompareTo(b.quadrant);
                return sc != 0 ? sc : a.sortKey.CompareTo(b.sortKey);
            });

            // 각 분면별 V 시작점 (체인치수 끝단 바깥)
            float leftTopNextV_m  = dimExtMaxV_m;
            float leftBotNextV_m  = dimExtMinV_m;
            float rightTopNextV_m = dimExtMaxV_m;
            float rightBotNextV_m = dimExtMinV_m;

            foreach (var balloon in mfgSortedBalloons)
            {
                try
                {
                    var textSz = mfgEstTextSize(balloon.text);
                    float textW = textSz.w;
                    float textH = textSz.h;

                    float textPosH, textPosV;
                    bool isHoleOrSlot = !balloon.text.StartsWith("EarthBoss");
                    if (isHoleOrSlot)
                    {
                        // 홀/슬롯 풍선: 수직 아래로 — 홀 H 위치 그대로 두어 리더가 수직이 되고,
                        //   모델 아래(모델 바깥)로 빼낸다. (사용자 사양 2026-06-30, 방향이 위로면 부호 반전)
                        textPosH = balloon.originH;
                        textPosV = modelMinV_m - (modelMaxV_m - modelMinV_m) * 0.6f;
                    }
                    else
                    switch (balloon.quadrant)
                    {
                        case 0: // 왼쪽위
                            textPosH = rectLeft_m;
                            textPosV = leftTopNextV_m;
                            leftTopNextV_m -= (textH + balloonSpacing_m);
                            break;
                        case 1: // 왼쪽아래
                            textPosH = rectLeft_m;
                            textPosV = leftBotNextV_m;
                            leftBotNextV_m += (textH + balloonSpacing_m);
                            break;
                        case 2: // 오른쪽위
                            textPosH = rectRight_m;
                            textPosV = rightTopNextV_m;
                            rightTopNextV_m -= (textH + balloonSpacing_m);
                            break;
                        case 3: // 오른쪽아래
                            textPosH = rectRight_m;
                            textPosV = rightBotNextV_m;
                            rightBotNextV_m += (textH + balloonSpacing_m);
                            break;
                        default:
                            textPosH = rectRight_m;
                            textPosV = balloon.originV;
                            break;
                    }

                    // 3D 좌표 복원
                    float[] xyz = new float[3];
                    xyz[bHAxis_m] = textPosH;
                    xyz[bVAxis_m] = textPosV;
                    xyz[bDAxis_m] = balloon.depthVal;

                    VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(xyz[0], xyz[1], xyz[2]);
                    VIZCore3D.NET.Data.Vertex3D arrowPos = new VIZCore3D.NET.Data.Vertex3D(
                        balloon.arrowX, balloon.arrowY, balloon.arrowZ);

                    VIZCore3D.NET.Data.NoteStyle mfgNoteStyle = vizcore3d.Review.Note.GetStyle();
                    mfgNoteStyle.UseSymbol = false;
                    mfgNoteStyle.BackgroudTransparent = true;
                    mfgNoteStyle.FontBold = true;
                    mfgNoteStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                    mfgNoteStyle.FontColor = balloon.color;
                    mfgNoteStyle.LineColor = balloon.color;
                    mfgNoteStyle.LineWidth = 1;
                    mfgNoteStyle.ArrowColor = balloon.color;
                    mfgNoteStyle.ArrowWidth = 2;

                    vizcore3d.Review.Note.AddNoteSurface(balloon.text, textPos, arrowPos, mfgNoteStyle);
                }
                catch { }
            }

            return pose;
        }

        /// <summary>
        /// 가공도 핵심 로직 (BOM Index를 받아서 가공도 출력)
        /// 시트 선택 3D 미리보기(LvDrawingSheet_SelectedIndexChanged)에서 사용.
        ///
        /// Step B2 (2026-05-19): 어댑터로 축소.
        /// 공통 3D 로직(부재 격리·카메라·ORIENTATION·Osnap·치수·풍선)은 BuildMfgSceneCore가 수행.
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
        /// PDF 어댑터는 공통 코어가 생성한 Note를 그대로 2D 변환하므로 출력 풍선은 유지한다.
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
                if (_mfgPreviewNetRoll != 0f)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, -_mfgPreviewNetRoll);
                    _mfgPreviewNetRoll = 0f;
                }
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
                shouldSnapshotCamera = pose.ApplyZ90 || pose.ApplyR180 || pose.UsedMinusCamera;

                DiagLog($"B2 ExecuteMfgDrawing bom={bom.Index} name=\"{bom.Name}\" " +
                    $"viewDir={pose.ViewDirection} longestAxis={pose.LongestAxis} " +
                    $"ApplyZ90={pose.ApplyZ90} ApplyR180={pose.ApplyR180} " +
                    $"UsedMinusCamera={pose.UsedMinusCamera} " +
                    $"orient={pose.OrientationAxis}/{pose.OrientationAngle:F0}");
            }
            catch (Exception ex)
            {
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
            int struIndex = 0)
        {
            var result = new MfgDrawingResult();
            if (mfgSheets == null || mfgSheets.Count == 0) return result;

            string xlsxPath = Path.Combine(GetSolutionPath(), "가공도_도면_1.xlsx");
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

                if (lvDrawingSheet.SelectedItems.Count > 0)
                    previousSelectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;

                // ── 진입부 강제 초기화 8단계 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                Clear2DView();
                if (vizcore3d.View.XRay.Enable) vizcore3d.View.XRay.Enable = false;
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                // DASH_LINE(은선 점선) 렌더모드 제거 — 은선 없는 캡처로 전환해 무의미 (2026-07-03). 출력 후 SMOOTH 복원은 유지.

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

                var bomSnapshot = SnapshotBomRows();
                int expectedBomRows = Math.Min(allMfgBomIndices.Count, 20);
                result.BomRows = bomSnapshot.Count;
                result.ExpectedBomRows = expectedBomRows;

                if (bomSnapshot.Count != expectedBomRows)
                {
                    DiagLog($"[GenMfgManual] WARN BOM snapshot mismatch: {bomSnapshot.Count} vs 예상 {expectedBomRows}");
                    if (bomSnapshot.Count > expectedBomRows)
                        result.Warnings.Add($"BOM snapshot 초과 ({bomSnapshot.Count}행, 예상 {expectedBomRows}) — 첫 20행만 사용");
                }

                bool bomSnapshotInsufficient = bomSnapshot.Count < expectedBomRows;
                if (bomSnapshotInsufficient)
                    DiagLog($"[GenMfgManual] WARN BOM 부족: {bomSnapshot.Count} < {expectedBomRows} (PDF 계속 생성)");

                // 다중 이미지 매핑 (SDK 1.0.26.716 신규) — {Image_1}=N 화살표(AT3), {Image_2}=ISO 화살표(C3),
                //   {Image_3}=CONTRACTOR 로고(AW53). Value = [일반, 배경반전].
                //   옛 {Image}+Set2DViewTemplateMark는 신 SDK에서 무력화 확인(로고 미표시) → {Image_3} 통합 (2026-07-21).
                var mfgImageMapping = new Dictionary<int, string[]>
                {
                    { 1, new[] { ResolveDrawingAssetPath("North_Arrow.png"), ResolveDrawingAssetPath("North_Arrow.png") } },
                    { 2, new[] { ResolveDrawingAssetPath("ISO_North_Arrow.png"), ResolveDrawingAssetPath("ISO_North_Arrow.png") } },
                    { 3, new[] { ResolveDrawingAssetPath("Logo.png"), ResolveDrawingAssetPath("Logo.png") } },
                };

                var pages = SplitMfgIntoPages(mfgSheets, 5);
                Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea> viewAreasCache = null;

                // [임시 §5-1] 카메라 ± 검증 PDF 1장 — 본 페이지 출력 앞에 별도 저장 (검증 후 제거)
                if (MfgCameraSignProbeEnabled)
                    RunMfgCameraSignProbe(mfgSheets, saveDir, xlsxPath, ref viewAreasCache);

                foreach (var page in pages)
                {
                    int failedRows = 0;
                    int successRows = 0;
                    try
                    {
                        ResetCanvasForMfgPage();
                        var data = BuildMfgPageData(page, pages.Count, struName, bomSnapshot);
                        var swTpl = System.Diagnostics.Stopwatch.StartNew();
                        // 북쪽 화살표 2종은 {Image_1}/{Image_2} + mfgImageMapping으로 Import 단계에서 처리 (2026-07-20).
                        //   ⚠ 태그 번호 한계 주의 — View는 1~7, Input은 1~199까지만 (초과 시 SDK 메모리 손상 → 캡처 AccessViolation).
                        vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data, mfgImageMapping);
                        swTpl.Stop();
                        DiagLog($"[TplTime] 템플릿 적용 p{page.PageIdx}={swTpl.ElapsedMilliseconds}ms");
                        // 빈 칸 괘선 제거 (SDK 1.0.26.716) — 미기재 BOM 행 괘선 제거, 제작도와 동일 패턴.
                        vizcore3d.Drawing2D.Object2D.RemoveEmptyTemplateBorders(0.1f, VIZCore3D.NET.Data.TemplateBorderRemoveMode.RowAndColumn);
                        EnsureViewAreasCache(ref viewAreasCache, xlsxPath);

                        // 캔버스 선(先)렌더 — 제작도(정상 완주) 검증 시퀀스 정합 (격리 7단계 2026-07-21).
                        //   제작도는 import 직후 Drawing2D.Render()를 호출한 뒤 캡처하는데 가공도는 이게 없었음.
                        //   '그리지 않은 캔버스 + 첫 캡처' 조합이 신 템플릿에서 AccessViolation 용의.
                        vizcore3d.Drawing2D.Render();

                        for (int i = 0; i < page.Rows.Count; i++)
                        {
                            var sheet = page.Rows[i];
                            if (sheet.MemberIndices.Count == 0) { failedRows++; continue; }
                            var bom = bomList.FirstOrDefault(b => b.Index == sheet.MemberIndices[0]);
                            if (bom == null) { failedRows++; continue; }

                            var area = viewAreasCache[i + 1];
                            if (RenderMfgRowToViewArea(i + 1, bom, area)) successRows++;
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

                        vizcore3d.Drawing2D.Render();
                        vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                        string pdfPath = MakeUniquePdfPath(saveDir, struName, page.PageIdx, pages.Count, struIndex);
                        vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                        result.SuccessPdfs++;
                        if (bomSnapshotInsufficient) result.InsufficientBomPdfs++;

                        DiagLog($"[GenMfgManual] p{page.PageIdx}/{pages.Count} 저장: {pdfPath}");
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[GenMfgManual] p{page.PageIdx} ERROR: {ex.Message}");
                        result.Warnings.Add($"p{page.PageIdx} 페이지 ERROR: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[GenMfgManual] FATAL: {ex.Message}");
                result.Warnings.Add($"가공도 출력 FATAL: {ex.Message}");
            }
            finally
            {
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

            DiagLog($"[GenMfgManual] 완료 — Success={result.SuccessPdfs} BomShort={result.InsufficientBomPdfs} Warnings={result.Warnings.Count}");
            return result;
        }

        /// <summary>
        /// 도면정보 탭 - 가공도 출력 버튼 클릭 (v7 P2-integrate)
        /// 가공도 시트 묶음 수집 → GenerateMfgDrawingManual 호출 → 결과 받아 단일 MessageBox.
        /// </summary>
        private void btnMfgDrawingSheet_Click(object sender, EventArgs e)
        {
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
            var result = GenerateMfgDrawingManual(mfgSheets, saveDir, "manual", struIndex: 0);

            // v7 Codex 6차 권고: 단일 MessageBox 통합
            if (result.TemplateMissing)
            {
                MessageBox.Show(
                    $"가공도 엑셀 템플릿 누락:\n{Path.Combine(GetSolutionPath(), "가공도_도면_1.xlsx")}\n\nPDF 생성 안 됨.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"가공도 PDF {result.SuccessPdfs}개 저장:");
            sb.AppendLine(saveDir);

            if (result.InsufficientBomPdfs > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ BOM 부족 PDF: {result.InsufficientBomPdfs}개");
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

        #endregion
    }
}
