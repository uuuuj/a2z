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
            for (int k = 1; k <= 129; k++)
                data[k] = "";

            // ── 도면정보 ──
            data[1] = "CEDAR FLNG";  // TODO: 프로젝트명 (T-043 tableInfo 결정 후)
            data[2] = "SN2688";       // TODO: 선박번호
            data[3] = totalPages > 1
                ? $"가공도 ({page.PageIdx}/{totalPages})"
                : "가공도";

            // ── 좌측 5행 BOM 이름 (Input_5~Input_9) ──
            for (int i = 0; i < page.Rows.Count && i < 5; i++)
            {
                var sheet = page.Rows[i];
                if (sheet.MemberIndices.Count == 0) continue;
                var bom = bomList.FirstOrDefault(b => b.Index == sheet.MemberIndices[0]);
                if (bom == null) continue;
                data[5 + i] = bom.Name ?? "";
            }

            // ── 우측 BOM 표 8컬럼 × 15행 (Input_10~Input_129, snapshot 사용) ──
            // 제작도와 동일 매핑 패턴, 슬롯 번호만 +6 이동.
            int bomMapped = 0;
            if (bomSnapshot != null)
            {
                int n = Math.Min(bomSnapshot.Count, 15);
                for (int i = 0; i < n; i++)
                {
                    string[] row = bomSnapshot[i];
                    data[10 + i]  = row[0];   // NO
                    data[25 + i]  = row[1];   // ITEM
                    data[40 + i]  = row[2];   // MATERIAL
                    data[55 + i]  = row[3];   // SIZE
                    data[70 + i]  = row[4];   // Q'TY
                    data[85 + i]  = row[5];   // T/W
                    data[100 + i] = row[6];   // MA
                    data[115 + i] = row[7];   // FA
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

            var dict = list.ToDictionary(a => a.Index, a => a);
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

            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
            if (objId < 0)
            {
                DiagLog($"[RenderMfgRow] row={rowIdx} bom={bom.Index} {viewLabel} 2D 캡처 실패");
                return false;
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
                    ApplyParallelTextShift(
                        pose.ViewDirection,
                        vizcore3d.Drawing2D.Object2D.GetObjectScale(objId),
                        measures);
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
            var targets = new List<int> { bomIndex };
            vizcore3d.View.FlyToObject3d(targets, zoomRatio);

            // 실제 투영 방향을 임시 캡처로 측정 (ground truth — 축 규약 추측 제거)
            int probe = -1;
            try
            {
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
                // 세로 → 화면축 90° 회전으로 가로화
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                vizcore3d.View.FlyToObject3d(targets, zoomRatio);   // baseline 순서: roll 뒤 Fly
                DiagLog($"[Orient] bom={bomIndex} 가로전환(90°) probeW={pw:F2} probeH={ph:F2}");
                return 90f;
            }

            DiagLog($"[Orient] bom={bomIndex} 가로유지 probeW={pw:F2} probeH={ph:F2}");
            return 0f;
        }

        private MfgViewPose BuildEaSecondaryScene(
            BOMData bom,
            MfgViewPose primaryPose,
            float availW,
            float availH)
        {
            var pose = new MfgViewPose { LongestAxis = primaryPose.LongestAxis };

            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

            // 두 번째 뷰는 독립 카메라에서 절대 방향으로 만든다.
            //   절대 방향(ApplyAbsoluteCameraRoll)이라 primary 회전을 되돌릴 필요가 없다 —
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
            style.AlignDistanceText = true;
            style.AlignDistanceTextPosition = 2;   // 0:아래 1:위 2:바깥쪽 — 치수 숫자를 치수선 바깥(모델 반대편)으로
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

            // 회전(Z 최장) 2차 뷰: 길이 치수를 화면 위쪽으로 강제.
            //   +90 화면 회전으로 3D '외곽(positive)' 기준이 화면 위/아래와 어긋나, 길이가 두 뷰 사이(중간)로
            //   떨어진다(회전 안 하는 부재는 정상). 화면 기준 고정값 사용 — 부호는 사내 검증으로 확정.
            if (pose.LongestAxis == "Z")
                positiveOffset = false;   // 화면 위쪽 추정값 (위가 아니면 true로 뒤집기)

            float chainOff1 = 50f;
            float chainOff2 = 100f;
            float maxTotalDistance = dimensions
                .Where(d => d.IsTotal && d.IsVisible)
                .Select(d => Math.Abs(
                    GetAxisValue(d.EndPoint, d.Axis) - GetAxisValue(d.StartPoint, d.Axis)))
                .DefaultIfEmpty(0f)
                .Max();
            float totalOff = maxTotalDistance > 1000f ? 300f : 250f;

            float canvasScale = EstimateFitScaleForViewArea(
                availW,
                availH,
                pose.ViewDirection,
                new List<int> { bom.Index },
                1.0f);
            if (canvasScale > 0f)
            {
                ComputeCanvasAbsoluteOffsets(canvasScale, out float baseOff, out float levelSpacing, out _,
                    MfgCanvasBaseOff, MfgCanvasLvlSp);   // 가공도 전용 축소 (1단 2·전체 4mm)
                int maxLevel = dimensions.Any(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0) ? 2 : 1;
                chainOff1 = baseOff;
                chainOff2 = baseOff + levelSpacing;
                totalOff = baseOff + levelSpacing * maxLevel;
            }

            var extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
            foreach (var dim in dimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, chainOff1,
                    bom.MinX, bom.MinY, bom.MinZ, pose.ViewDirection, extensionLines,
                    bom.MaxX, bom.MaxY, bom.MaxZ, positiveOffset);
            foreach (var dim in dimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, chainOff2,
                    bom.MinX, bom.MinY, bom.MinZ, pose.ViewDirection, extensionLines,
                    bom.MaxX, bom.MaxY, bom.MaxZ, positiveOffset);
            foreach (var dim in dimensions.Where(d => d.IsTotal && d.IsVisible))
                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, totalOff,
                    bom.MinX, bom.MinY, bom.MinZ, pose.ViewDirection, extensionLines,
                    bom.MaxX, bom.MaxY, bom.MaxZ, positiveOffset);

            if (extensionLines.Count > 0)
            {
                int shapeId = vizcore3d.ShapeDrawing.AddLine(
                    extensionLines, -1, Color.Blue, 0.15f, true);   // 제작도와 동일 (0.3→0.15)
                if (shapeId >= 0)
                    pose.ShapeDrawingIds.Add(shapeId);
            }

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

                var pose = BuildMfgSceneCore(
                    bom.Index,
                    area.Width,
                    viewHeight,
                    isEA);

                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;
                // 가로 배치 — 임시 캡처로 실제 세로/가로 측정 후 세로면 화면축 90° 회전.
                float primRoll = ProbeAndRollLandscape(bom.Index, 1.25f);
                System.Windows.Forms.Application.DoEvents();

                int primaryObjId;
                bool primaryOk = CaptureMfgSceneToViewArea(
                    rowIdx, bom, pose,
                    area.X, area.Y, area.Width, viewHeight,
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
                            bom, pose, area.Width, viewHeight);

                        // 2차 뷰도 동일: 임시 캡처로 세로/가로 측정 후 세로면 90° 회전
                        float secRoll = ProbeAndRollLandscape(bom.Index, 1.25f);
                        System.Windows.Forms.Application.DoEvents();

                        int secondaryObjId;
                        bool secondaryOk = CaptureMfgSceneToViewArea(
                            rowIdx, bom, secondaryPose,
                            area.X, area.Y + viewHeight + viewGap,
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
        private const float MfgCanvasBaseOff = 2.0f;     // 1단 종이 mm
        private const float MfgCanvasLvlSp   = 2.0f;     // 단 간격 → 전체 = 2+2 = 4mm

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
        ///   - RenderMfgViewForDrawing (구형 미사용 경로): pose를 지역변수로만 사용한다.
        /// </summary>
        private MfgViewPose BuildMfgSceneCore(
            int bomIndex,
            float availW = -1f,
            float availH = -1f,
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
            //   ExecuteMfgDrawing / RenderMfgViewForDrawing 양쪽에서 pose.LongestAxis=="Z"이면 RotateCameraByScreenAxis(0,0,90).
            //   B1b1a는 결정만, 어댑터가 적용.
            pose.ApplyZ90 = (pose.LongestAxis == "Z");

            // ── 5. Osnap 수집 (LINE/POINT만, CIRCLE 제외 — T-064 사양) ──
            //   B1b1b 추가 (2026-05-19): 자동 함수 본체 L1004~L1027 추출.
            var mfgOsnapWithNames = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
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
                $"rawOsnap LINE={rawLineCount} POINT={rawPointCount} CIRCLE={rawCircleCount}");

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
            float mfgTotalOff = 250.0f;
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

                // 전체길이 치수가 1000mm 초과하면 보조선 300mm, 아니면 250mm
                float maxTotalDist = 0f;
                foreach (var td in mfgDimensions.Where(d => d.IsTotal && d.IsVisible))
                {
                    float dist = 0f;
                    switch (td.Axis)
                    {
                        case "X": dist = Math.Abs(td.EndPoint.X - td.StartPoint.X); break;
                        case "Y": dist = Math.Abs(td.EndPoint.Y - td.StartPoint.Y); break;
                        case "Z": dist = Math.Abs(td.EndPoint.Z - td.StartPoint.Z); break;
                    }
                    if (dist > maxTotalDist) maxTotalDist = dist;
                }
                mfgTotalOff = maxTotalDist > 1000.0f ? 300.0f : 250.0f;

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
                    mfgStyle.AlignDistanceText = true;
                    mfgStyle.AlignDistanceTextPosition = 2;   // 0:아래 1:위 2:바깥쪽 — 치수 숫자를 치수선 바깥(모델 반대편)으로
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
                        if (mfgAxisPosOff.ContainsKey(pose.LongestAxis))
                            mfgAxisPosOff[pose.LongestAxis] = !isAboveWider;
                        foreach (string ax in new List<string>(mfgAxisPosOff.Keys))
                        {
                            if (ax != pose.LongestAxis)
                                mfgAxisPosOff[ax] = isLShape;
                        }
                    }

                    var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                    // 보조선 길이 결정.
                    // 2D 출력(availW>0): 제작도와 동일 원리 — 캔버스 절대 5/10mm를 추정 fit scale로 역산 (공용 헬퍼).
                    //   가공도 칸은 모델 100% 채움 → fitFactor=1.0 (제작도는 0.65/0.70).
                    // 3D 미리보기(availW<=0): 기존 모델좌표 offFactor 유지.
                    float mfgChainOff1, mfgChainOff2;
                    float mfgCanvasScale = -1f;
                    if (availW > 0f && availH > 0f)
                        mfgCanvasScale = EstimateFitScaleForViewArea(availW, availH, viewDirection, new List<int> { bom.Index }, 1.0f);

                    if (mfgCanvasScale > 0f)
                    {
                        ComputeCanvasAbsoluteOffsets(mfgCanvasScale, out float mfgBaseOff, out float mfgLvlSp, out _,
                            MfgCanvasBaseOff, MfgCanvasLvlSp);   // 가공도 전용 축소 (1단 2·전체 4mm)
                        int mfgMaxLevel = mfgDimensions.Any(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0) ? 2 : 1;
                        mfgChainOff1 = mfgBaseOff;                          // DisplayLevel==0 (제작도 level1)
                        mfgChainOff2 = mfgBaseOff + mfgLvlSp;               // DisplayLevel>0  (제작도 level2)
                        mfgTotalOff  = mfgBaseOff + mfgLvlSp * mfgMaxLevel; // IsTotal         (제작도 level0)
                        DiagLog($"가공도 보조선 절대 bom={bom.Index} view={viewDirection} estScale={mfgCanvasScale:F4} off1={mfgChainOff1:F2} off2={mfgChainOff2:F2} total={mfgTotalOff:F2}");
                    }
                    else
                    {
                        // 기존 모델좌표 offFactor (3D 미리보기 경로)
                        float visExt1 = 0f, visExt2 = 0f;
                        switch (viewDirection)
                        {
                            case "X": visExt1 = bom.MaxY - bom.MinY; visExt2 = bom.MaxZ - bom.MinZ; break;
                            case "Y": visExt1 = bom.MaxX - bom.MinX; visExt2 = bom.MaxZ - bom.MinZ; break;
                            default:  visExt1 = bom.MaxX - bom.MinX; visExt2 = bom.MaxY - bom.MinY; break;
                        }
                        float minVisExtent = Math.Min(visExt1, visExt2);
                        float offFactor = (minVisExtent < 100f) ? 0.5f : 1.0f;
                        mfgChainOff1 = 50.0f * offFactor;
                        mfgChainOff2 = 100.0f * offFactor;
                        mfgTotalOff *= offFactor;
                    }

                    // 3 패스: level=0 / level>0 / IsTotal
                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                    {
                        if (isEA && reserveLongestAxisForSecondary && dim.Axis == pose.LongestAxis) continue;
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff1,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                    {
                        if (isEA && reserveLongestAxisForSecondary && dim.Axis == pose.LongestAxis) continue;
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff2,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => d.IsTotal && d.IsVisible))
                    {
                        if (isEA && reserveLongestAxisForSecondary && dim.Axis == pose.LongestAxis) continue;
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgTotalOff,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }

                    if (mfgExtLines.Count > 0)
                    {
                        int shapeId = vizcore3d.ShapeDrawing.AddLine(mfgExtLines, -1, System.Drawing.Color.Blue, 0.15f, true);  // 제작도와 동일 (0.3→0.15)
                        if (shapeId >= 0) pose.ShapeDrawingIds.Add(shapeId);
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
        /// btnMfgDrawing_Click과 도면정보 탭 가공도 시트에서 공통 사용.
        ///
        /// Step B2 (2026-05-19): 어댑터로 축소.
        /// 공통 3D 로직(부재 격리·카메라·ORIENTATION·Osnap·치수·풍선)은 BuildMfgSceneCore가 수행.
        /// 어댑터는 수동 전용 후처리만:
        ///   - X-Ray 끄기 + 선택 해제
        ///   - 코어 호출 → pose 받음
        ///   - 3D 뷰용 RenderMode = SMOOTH (코어는 미지정)
        ///   - SilhouetteEdge = Green
        ///   - FlyToObject3d (카메라 fit)
        ///   - pose.ApplyZ90 / pose.ApplyR180 적용
        ///   - _lastMfgViewPose 저장 (시트 선택 후처리 회전이 참조)
        ///   - EndUpdate 후 카메라 스냅샷 (ScreenAxisRotation commit 후)
        ///
        /// 사용자 사양 (2026-05-19): Note.Clear() 제거 — 3D 뷰에도 풍선 표시.
        /// 옛 T-064 사양 폐기 (memory/feedback_mfg_balloon_2026-05-19.md).
        /// </summary>
        private void ExecuteMfgDrawing(int bomIndex)
        {
            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return;

            MfgViewPose pose = null;
            bool shouldSnapshotCamera = false;

            vizcore3d.BeginUpdate();
            try
            {
                // 이전 선택상태(빨간색) 해제 + X-Ray 끄기 (수동 어댑터 전용)
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // ── 공통 코어 호출 ──
                //   부재 격리·BBox·축·카메라·ORIENTATION·Osnap·EA·치수·풍선 모두 코어가 수행.
                pose = BuildMfgSceneCore(bomIndex);

                // ── 수동 어댑터 후처리: 3D 뷰용 SMOOTH 실선 + Silhouette ──
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // FlyToObject3d
                List<int> targetIndices = new List<int> { bom.Index };
                vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);

                // pose.ApplyZ90 적용 (Z 최장축이면 90° 회전)
                if (pose.ApplyZ90)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                }

                // pose.ApplyR180 적용 (EA L자 열린 방향 정렬)
                if (pose.ApplyR180)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                }

                // pose 저장 — 시트 선택 후처리 회전이 참조
                _lastMfgViewPose = pose;
                shouldSnapshotCamera = pose.ApplyZ90 || pose.ApplyR180 || pose.UsedMinusCamera;

                // ⚠️ Note.Clear() 제거 (사용자 사양 2026-05-19)
                //   옛 T-064: vizcore3d.Review.Note.Clear() 호출했음. 사양 변경으로 제거.
                //   3D 뷰에도 풍선 표시.

                DiagLog($"B2 ExecuteMfgDrawing bom={bom.Index} name=\"{bom.Name}\" " +
                    $"viewDir={pose.ViewDirection} longestAxis={pose.LongestAxis} " +
                    $"ApplyZ90={pose.ApplyZ90} ApplyR180={pose.ApplyR180} " +
                    $"UsedMinusCamera={pose.UsedMinusCamera}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                vizcore3d.EndUpdate();
            }

            // EndUpdate 이후 카메라 스냅샷 — ScreenAxisRotation commit 후
            //   (BeginUpdate 스코프 내에서는 회전이 commit 전 상태일 수 있음 — click-order 의존 버그 회피)
            if (shouldSnapshotCamera && pose != null)
            {
                System.Windows.Forms.Application.DoEvents();
                pose.CameraData = vizcore3d.View.GetCameraData();
            }
        }

        /// <summary>
        /// 가공도 수동 통합 함수 v7. PDF 소유.
        /// 호출자:
        ///   - 수동: btnMfgDrawingSheet_Click → 결과 받아 단일 MessageBox로 표시
        ///   - 자동(P4a): ProcessSingleStruFull §8, ExportAllSheetsToPdfCore → DiagLog만
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

            string xlsxPath = Path.Combine(GetSolutionPath(), "사용자템플릿_엑셀_가공도.xlsx");
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
            vizcore3d.BeginUpdate();

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
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

                // ── BOM 표 1회 채우기 (가공도 전체 부재, 모든 페이지 동일) ──
                var allMfgBomIndices = mfgSheets
                    .Where(s => s.MemberIndices.Count > 0)
                    .Select(s => s.MemberIndices[0])
                    .Distinct()
                    .ToList();

                if (allMfgBomIndices.Count > 15)
                {
                    string msg = $"가공도 부재 {allMfgBomIndices.Count}개 — BOM 표 15행 초과, 16번째 이후 PDF 미표시";
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
                int expectedBomRows = Math.Min(allMfgBomIndices.Count, 15);
                result.BomRows = bomSnapshot.Count;
                result.ExpectedBomRows = expectedBomRows;

                if (bomSnapshot.Count != expectedBomRows)
                {
                    DiagLog($"[GenMfgManual] WARN BOM snapshot mismatch: {bomSnapshot.Count} vs 예상 {expectedBomRows}");
                    if (bomSnapshot.Count > expectedBomRows)
                        result.Warnings.Add($"BOM snapshot 초과 ({bomSnapshot.Count}행, 예상 {expectedBomRows}) — 첫 15행만 사용");
                }

                bool bomSnapshotInsufficient = bomSnapshot.Count < expectedBomRows;
                if (bomSnapshotInsufficient)
                    DiagLog($"[GenMfgManual] WARN BOM 부족: {bomSnapshot.Count} < {expectedBomRows} (PDF 계속 생성)");

                var pages = SplitMfgIntoPages(mfgSheets, 5);
                Dictionary<int, VIZCore3D.NET.Data.TemplateViewArea> viewAreasCache = null;

                foreach (var page in pages)
                {
                    int failedRows = 0;
                    int successRows = 0;
                    try
                    {
                        ResetCanvasForMfgPage();
                        var data = BuildMfgPageData(page, pages.Count, struName, bomSnapshot);
                        vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data);
                        EnsureViewAreasCache(ref viewAreasCache, xlsxPath);

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

                // P3 #2: EndUpdate (BeginUpdate 짝) — 최종 상태(격리 복원 후)를 한 번에 화면에 반영
                try { vizcore3d.EndUpdate(); } catch { }
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
                    $"가공도 엑셀 템플릿 누락:\n{Path.Combine(GetSolutionPath(), "사용자템플릿_엑셀_가공도.xlsx")}\n\nPDF 생성 안 됨.",
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
        /// 가공도 시트 목록을 받아 8행×3열 그리드에 2D 일괄 출력
        /// GenerateSheetDrawing2D와 동일한 초기화 패턴, BOM 테이블 없이 도면정보만
        /// </summary>
        private void GenerateMfgDrawing2DAll(List<DrawingSheetData> mfgSheets)
        {
            // T-064 P3 롤백 (2026-05-14): 가공도는 옛 GridStructure 흐름 유지.
            // 메인 도면(GenerateSheetDrawing2D)만 엑셀 템플릿(P2) 사용.

            try
            {
                vizcore3d.View.EnableAnimation = false;

                // ── 0. 기존 3D 어노테이션 모두 초기화 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // ── 1. 2D 완전 초기화 ──
                Clear2DView();

                // 2D 패널 크기 조정
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

                // ── 2. 캔버스 설정 ──
                // T-064 (2026-05-14 3차): 사용자 사양 — 가공도에 ISO/Looking 라벨이 잔존하는 문제 회피.
                //   메인 도면(GenerateSheetDrawing2D_WithExcelTemplate)이 그린 그리드 셀 라벨이 가공도 진입 시 잔존.
                //   캔버스 자체를 제거하고 새로 만들어 모든 잔존물 제거.
                try { vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(); } catch { }
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);  // A4 가로

                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                // ── 3. 외곽 테두리 생성 (간단한 1x1 그리드로 깔끔한 A4 테두리) ──
                // T-064 (2026-05-14 4차): 사용자 사양 "가공도 그냥 템플릿 안쓰는 버전으로 되돌리자"
                //   → 직전 commit 27aae40 (가공도 엑셀 템플릿 ImportExcelWithData) 롤백.
                //   → 옛 외곽 1×1 그리드 + CreateTemplateBorder + table2 (우측 하단 도면정보) 복귀.
                vizcore3d.Drawing2D.GridStructure.AddGridStructure(1, 1, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);
                VIZCore3D.NET.Data.TemplateBorderInfo bInfo = vizcore3d.Drawing2D.Template.CreateTemplateBorder();

                // ── 4. 모델 배치용 그리드 재생성 (4x6) ──
                // T-064 (2026-05-14 2차): 사용자 사양 — 상단 빈 행 제거 (gridRows 5→4, Row 1부터 사용)
                //   옛 1차(718e534): gridRows=5, usableRowStart=2 → Row 1이 빈 상단 여백 차지
                //   2차: gridRows=4, usableRowStart=1 → Row 1부터 모델 배치 → 상단 여백 사라짐
                //   한 페이지 부재 수는 12개로 유지 (rowsPerCol=4 × 3 모델 그룹)
                const int gridRows = 4;
                const int gridCols = 6;   // 라벨(1,3,5) + 모델(2,4,6)
                const int usableRowStart = 1;  // 1행부터 (옛 2)
                const int usableRowEnd = 4;    // 4행까지 (옛 5)
                const int rowsPerCol = usableRowEnd - usableRowStart + 1; // 4

                vizcore3d.Drawing2D.GridStructure.AddGridStructure(gridRows, gridCols, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);

                // 도면정보 — A4 우측 하단 모서리에 Anchor 절대좌표 방식으로 배치 (T-064 4차: 엑셀 템플릿 롤백 복귀)
                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));
                table2.ImageHeight = 50;
                table2.IsTextWrapped = true;
                table2.ColumnWidths = new Dictionary<int, int>() { { 0, 15 }, { 1, 30 }, { 2, 10 }, { 3, 10 } };

                // bInfo 좌표 기반 Anchor 방식: 우측 하단 모서리에 붙이기
                table2.HorizontalAnchor = VIZCore3D.NET.Data.TableHorizontalAnchor.Right;
                table2.VerticalAnchor = VIZCore3D.NET.Data.TableVerticalAnchor.Bottom;
                table2.X = bInfo.MaxX;   // 테두리 우측
                table2.Y = bInfo.MinY;   // 테두리 하단
                vizcore3d.Drawing2D.Template.RenderTemplate(table2);

                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;  // T-040 v5: 2.0→3.0 (모델 두드러지게)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                // T-064 (2026-05-14): 사용자 사양 — 가공도 치수 텍스트 키움 (5→8mm, 풍선 비활성화에 따른 보강)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(8f);

                // ── 4. 각 가공도 시트를 열 우선 순서로 셀에 배치 (2~7행만 사용) ──
                // 라벨 칼럼(1,3,5) + 모델 칼럼(2,4,6) 구조
                const int modelGroupCount = 3;  // 3개 모델 그룹
                int maxSlots = rowsPerCol * modelGroupCount; // 18
                int count = Math.Min(mfgSheets.Count, maxSlots);
                for (int i = 0; i < count; i++)
                {
                    int modelGroup = i / rowsPerCol;              // 0, 1, 2
                    int rowInGroup = i % rowsPerCol;              // 0~3
                    int row = rowInGroup + usableRowStart;        // 1~4행
                    // T-064 (2026-05-14 3차): 사용자 사양 — 라벨을 모델 오른쪽으로 이동 (옛 라벨 왼쪽 → 모델 왼쪽 / 라벨 오른쪽)
                    int modelCol = modelGroup * 2 + 1;            // 1, 3, 5 (옛 labelCol)
                    int labelCol = modelGroup * 2 + 2;            // 2, 4, 6 (옛 modelCol)

                    // 모델 2D 렌더링
                    RenderMfgViewForDrawing(row, modelCol, mfgSheets[i].MemberIndices[0]);

                    // 라벨 배치 (모델 Name) — 오른쪽 셀, 넓힘 + 줄바꿈 허용
                    try
                    {
                        BOMData labelBom = bomList.FirstOrDefault(b => b.Index == mfgSheets[i].MemberIndices[0]);
                        if (labelBom != null && !string.IsNullOrEmpty(labelBom.Name))
                        {
                            VIZCore3D.NET.Data.TemplateTableData labelTable = new VIZCore3D.NET.Data.TemplateTableData(1, 1);
                            labelTable.SetText(0, 0, labelBom.Name);
                            // T-064 (2026-05-14 3차): 사용자 사양 — 라벨 폭 25→40mm, 긴 이름은 줄바꿈 (IsTextWrapped=true)
                            labelTable.IsTextWrapped = true;
                            labelTable.ColumnWidths = new Dictionary<int, int>() { { 0, 40 } };

                            vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(row, labelCol,
                                VIZCore3D.NET.Data.GridVerticalAlignment.Middle);
                            vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(row, labelCol,
                                VIZCore3D.NET.Data.GridHorizontalAlignment.Center);
                            vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(labelTable, row, labelCol);
                        }
                    }
                    catch { }
                }

                // ── 5. 최종 렌더링 ──
                vizcore3d.Drawing2D.Render();

                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                vizcore3d.Drawing2D.View.SetCanvasResetViewPos(-1);

                // ── 6. 뷰어 크기 조정 ──
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                        {
                            vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.1);
                        }

                        vizcore3d.Drawing2D.View.SetCanvasResetViewPos(-1);

                        try
                        {
                            vizcore3d.Drawing2D.Object2D.SelectAllObjectBy2DView();

                            SplitterPanel panel2 = vizcore3d.SplitContainer.Panel2;
                            IntPtr hwnd = panel2.Controls.Count > 0
                                ? panel2.Controls[0].Handle
                                : panel2.Handle;

                            SetFocus(hwnd);

                            Point center = panel2.PointToScreen(
                                new Point(panel2.Width / 2, panel2.Height / 2));
                            int lParam = (center.Y << 16) | (center.X & 0xFFFF);

                            for (int z = 0; z < 7; z++)
                            {
                                IntPtr wParam = (IntPtr)(WHEEL_DELTA << 16);
                                SendMessage(hwnd, WM_MOUSEWHEEL, wParam, (IntPtr)lParam);
                            }

                            vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                            vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                        }
                        catch { }
                    }
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 2D 일괄 출력 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 가공도 셀 렌더 — 그리드 셀 안에 단일 부재 2D 캡처.
        /// Step B3 (2026-05-19): 어댑터로 축소.
        /// 공통 3D 로직(부재 격리·카메라·ORIENTATION·Osnap·치수·풍선)은 BuildMfgSceneCore가 수행.
        /// 자동 어댑터 후처리:
        ///   - DASH_LINE RenderMode (PDF 은선용)
        ///   - FlyToObject3d (카메라 fit)
        ///   - pose.ApplyZ90 / pose.ApplyR180 적용
        ///   - 2D 캔버스 변환 (Create2DViewObjectWithModelHiddenLineAtCanvasOrigin)
        ///   - 그리드 셀 fit (FitObjectToGridCellAspect + 30% scale + 20mm min)
        ///   - ShapeDrawing → 2D (pose.ShapeDrawingIds 사용)
        ///   - Note(풍선) → 2D (Add2DNoteFrom3DNote)
        ///   - Measure → 2D + ApplyParallelTextShift
        ///   - 두께 복원
        /// 
        /// Codex P3 제약 (2026-05-18): _lastMfgViewPose write X (지역변수만).
        /// 사용자 사양 (2026-05-19): noteIds.Clear() 제거 — PDF에 풍선 표시.
        /// 현재 수동 PDF 출력은 RenderMfgRowToViewArea를 사용하며, EA 상하 2뷰도 그 경로에서 처리한다.
        /// </summary>
        private int RenderMfgViewForDrawing(int row, int col, int bomIndex)
        {
            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return -1;

            // ── 공통 코어 호출 (지역변수 pose만, _lastMfgViewPose write X) ──
            var pose = BuildMfgSceneCore(bomIndex);

            // ── 자동 어댑터 후처리: DASH_LINE + FlyToObject3d ──
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            vizcore3d.View.SilhouetteEdge = true;
            vizcore3d.View.SilhouetteEdgeColor = Color.Green;

            List<int> targetIndices = new List<int> { bom.Index };
            vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);

            // pose.ApplyZ90 / pose.ApplyR180 적용
            if (pose.ApplyZ90)
            {
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
            }
            if (pose.ApplyR180)
            {
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
            }

            // ── 2D 변환: 은선 포함 ──
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            int objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

            // ── 그리드 셀 fit (30% target + 20mm min) ──
            vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, objId,
                VIZCore3D.NET.Data.GridHorizontalAlignment.Center,
                VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

            {
                float cellW = vizcore3d.Drawing2D.GridStructure.GetGridCellWidth(row, col);
                float cellH = vizcore3d.Drawing2D.GridStructure.GetGridCellHeight(row, col);
                float marginL = vizcore3d.Drawing2D.GridStructure.GetGridCellLeftMargin(row, col);
                float marginR = vizcore3d.Drawing2D.GridStructure.GetGridCellRightMargin(row, col);
                float marginT = vizcore3d.Drawing2D.GridStructure.GetGridCellTopMargin(row, col);
                float marginB = vizcore3d.Drawing2D.GridStructure.GetGridCellBottomMargin(row, col);

                float contentW = cellW - marginL - marginR;
                float contentH = cellH - marginT - marginB;

                float objW = 0f, objH = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);

                if (objW > 0 && objH > 0 && contentW > 0 && contentH > 0)
                {
                    float targetW = contentW * 0.30f;
                    float targetH = contentH * 0.30f;
                    float scaleW = targetW / objW;
                    float scaleH = targetH / objH;
                    float fitScale = Math.Min(scaleW, scaleH);

                    if (fitScale > 0 && Math.Abs(fitScale - 1.0f) > 0.01f)
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, fitScale);

                    float scaledW = 0f, scaledH = 0f;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref scaledW, ref scaledH);
                    if (scaledW > 0 && scaledW < 20f)
                    {
                        float currentScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                        float adjustRatio = 20f / scaledW;
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, currentScale * adjustRatio);
                    }
                }
            }

            // ── ShapeDrawing(보조선) → 2D ──
            //   pose.ShapeDrawingIds: 코어가 채운 보조선 ID 목록
            if (pose.ShapeDrawingIds != null && pose.ShapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(pose.ShapeDrawingIds);
            }

            // ── Note(풍선) → 2D ──
            //   ⚠️ noteIds.Clear() 제거 (사용자 사양 2026-05-19): PDF에 풍선 표시.
            //   옛 T-064 사양 폐기 (memory/feedback_mfg_balloon_2026-05-19.md).
            List<int> noteIds = new List<int>();
            List<VIZCore3D.NET.Data.NoteItem> notes = vizcore3d.Review.Note.Items;
            foreach (var note in notes)
                noteIds.Add(note.ID);

            if (noteIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(3.5f);
                vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);

                foreach (int idx in noteIds)
                {
                    try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(idx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                    catch { }
                }
            }

            // ── Measure(치수선) → 2D + ApplyParallelTextShift ──
            List<int> measureIds = new List<int>();
            List<VIZCore3D.NET.Data.MeasureItem> measures = vizcore3d.Review.Measure.Items;
            foreach (var measure in measures)
            {
                if (measure.Visible) measureIds.Add(measure.ID);
            }
            if (measureIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.5f);
                ApplyParallelTextShift(pose.ViewDirection,
                    vizcore3d.Drawing2D.Object2D.GetObjectScale(objId), measures);
                vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
            }

            // ── 두께 복원 (다음 셀용) ──
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);

            DiagLog($"B3 RenderMfgViewForDrawing row={row} col={col} bom={bom.Index} " +
                $"viewDir={pose.ViewDirection} longestAxis={pose.LongestAxis} " +
                $"ApplyZ90={pose.ApplyZ90} ApplyR180={pose.ApplyR180} " +
                $"shapeIds={(pose.ShapeDrawingIds?.Count ?? 0)} noteIds={noteIds.Count} measureIds={measureIds.Count} " +
                $"objId={objId}");

            return objId;
        }

        /// <summary>
        /// UDA에서 SPREF 값을 조회 (현재 노드 → 부모 10단계까지 탐색)
        /// </summary>
        private string GetSprefValue(int nodeIndex)
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

            // 필터 후 포인트가 없으면 원본 유지
            return filtered.Count > 0 ? filtered : osnapList;
        }

        /// <summary>
        /// UDA에서 특정 Key 값을 조회 (현재 노드 → 부모 10단계까지 탐색)
        /// </summary>
        private string GetUdaValue(int nodeIndex, string keyName)
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
