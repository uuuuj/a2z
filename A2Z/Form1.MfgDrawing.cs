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

        /// <summary>
        /// 가공도 출력 버튼 클릭
        /// 선택된 부재만 표시하고, 가장 긴 축이 좌우가 되는 시점에서 치수 표시
        /// </summary>
        private void btnMfgDrawing_Click(object sender, EventArgs e)
        {
            if (lvBOM.SelectedItems.Count == 0)
            {
                MessageBox.Show("BOM 리스트에서 부재를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BOMData bom = lvBOM.SelectedItems[0].Tag as BOMData;
            if (bom == null) return;

            ExecuteMfgDrawing(bom.Index);
        }

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

        /// <summary>
        /// 가공도 핵심 로직 (BOM Index를 받아서 가공도 출력)
        /// btnMfgDrawing_Click과 도면정보 탭 가공도 시트에서 공통 사용
        /// </summary>
        private void ExecuteMfgDrawing(int bomIndex)
        {
            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return;

            try
            {
                // 1. 기존 치수/보조선/풍선 모두 제거
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                vizcore3d.Review.Note.Clear();

                // 2. X-Ray 끄기
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // 3. 선택된 부재만 보이도록
                List<int> allIndices = new List<int>();
                foreach (BOMData b in bomList)
                    allIndices.Add(b.Index);
                vizcore3d.Object3D.Show(allIndices, false);

                List<int> targetIndices = new List<int> { bom.Index };
                vizcore3d.Object3D.Show(targetIndices, true);

                // 4. 바운딩 박스로 가장 긴 축 판별
                float sizeX = bom.MaxX - bom.MinX;
                float sizeY = bom.MaxY - bom.MinY;
                float sizeZ = bom.MaxZ - bom.MinZ;

                string longestAxis;
                if (sizeX >= sizeY && sizeX >= sizeZ)
                    longestAxis = "X";
                else if (sizeY >= sizeX && sizeY >= sizeZ)
                    longestAxis = "Y";
                else
                    longestAxis = "Z";

                // 5. 카메라: 최장축이 수평으로 보이는 방향으로 설정
                //    각 카메라에서 수평으로 보이는 축:
                //      Y_PLUS → X 수평, Z 수직  (X/Z 최장에 적합)
                //      X_PLUS → Y 수평, Z 수직  (Y 최장에 적합)
                //    Z 최장: Y_PLUS + 마지막에 90° 회전 → Z 수평
                string viewDirection;
                switch (longestAxis)
                {
                    case "Y":
                        viewDirection = "X";
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                        break;
                    default: // X 또는 Z (Z는 나중에 90° 회전)
                        viewDirection = "Y";
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                        break;
                }

                // 6. 화면 맞춤 + 은선 모드 (모든 조작 전에 기본 설정 완료)
                vizcore3d.View.FitToView();
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;
                // ※ Z 최장축 90° 회전은 모든 drawing 완료 후 마지막에 적용 (아래 참조)

                // 7. 해당 부재의 Osnap 수집
                var mfgOsnapWithNames = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
                var osnapListMfg = vizcore3d.Object3D.GetOsnapPoint(bom.Index);
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
                            case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                            case VIZCore3D.NET.Data.OsnapKind.POINT:
                                if (osnap.Center != null)
                                    mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z), bom.Name));
                                break;
                        }
                    }
                }

                if (mfgOsnapWithNames.Count == 0)
                {
                    vizcore3d.Object3D.Show(allIndices, true);
                    return;
                }

                // 8. 좌표 병합 + 뷰 방향 기준 visible 축만 체인치수 추출
                //    (X/Y/Z 버튼과 동일 로직 / 부재별 Osnap 1개 필터링 없음)
                float tolerance = 0.5f;
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(mfgOsnapWithNames, tolerance);

                List<string> mfgVisibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": mfgVisibleAxes.Add("Y"); mfgVisibleAxes.Add("Z"); break;
                    case "Y": mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Z"); break;
                    default:  mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Y"); break;
                }

                var mfgDimensions = new List<ChainDimensionData>();
                foreach (var ax in mfgVisibleAxes)
                    mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, ax, tolerance, viewDirection));

                if (mfgDimensions.Count == 0)
                {
                    vizcore3d.Object3D.Show(allIndices, true);
                    return;
                }

                // 9. 치수 그리기 (X/Y/Z 버튼 동일 방식: 파란색, 체인100mm, 전체150mm)
                vizcore3d.BeginUpdate();
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
                mfgStyle.FontColor = System.Drawing.Color.Blue;
                mfgStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                mfgStyle.FontBold = true;
                mfgStyle.LineColor = System.Drawing.Color.Blue;
                mfgStyle.LineWidth = 1;
                mfgStyle.ArrowColor = System.Drawing.Color.Blue;
                mfgStyle.ArrowSize = 5;
                mfgStyle.AssistantLine = false;
                mfgStyle.AlignDistanceText = true;
                mfgStyle.AlignDistanceTextPosition = 0;
                mfgStyle.AlignDistanceTextMargine = 3;
                vizcore3d.Review.Measure.SetStyle(mfgStyle);

                float mfgGlobalMinX = bom.MinX, mfgGlobalMinY = bom.MinY, mfgGlobalMinZ = bom.MinZ;
                float mfgGlobalMaxX = bom.MaxX, mfgGlobalMaxY = bom.MaxY, mfgGlobalMaxZ = bom.MaxZ;
                float mfgCenterX = (mfgGlobalMinX + mfgGlobalMaxX) / 2f;
                float mfgCenterY = (mfgGlobalMinY + mfgGlobalMaxY) / 2f;
                float mfgCenterZ = (mfgGlobalMinZ + mfgGlobalMaxZ) / 2f;

                // 축별 치수선 방향 결정 (모델 중심 기준 - 바깥쪽으로)
                var mfgAxisPosOff = new Dictionary<string, bool>();
                foreach (var grp in mfgDimensions.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                {
                    string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                    float sumV = 0; int cnt = 0;
                    foreach (var d in grp)
                    {
                        sumV += GetAxisValue(d.StartPoint, offAxis);
                        sumV += GetAxisValue(d.EndPoint, offAxis);
                        cnt += 2;
                    }
                    float avg = cnt > 0 ? sumV / cnt : 0;
                    float center = offAxis == "X" ? mfgCenterX : offAxis == "Y" ? mfgCenterY : mfgCenterZ;
                    mfgAxisPosOff[grp.Key] = avg >= center;
                }

                var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
                const float mfgChainOff1 = 100.0f;  // 1단 체인치수 보조선 100mm
                const float mfgChainOff2 = 200.0f;  // 2단 체인치수 보조선 200mm

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
                float mfgTotalOff = maxTotalDist > 1000.0f ? 300.0f : 250.0f;

                foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                {
                    bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff1,
                        mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                        mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                }
                foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                {
                    bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff2,
                        mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                        mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                }
                foreach (var dim in mfgDimensions.Where(d => d.IsTotal && d.IsVisible))
                {
                    bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgTotalOff,
                        mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                        mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                }
                if (mfgExtLines.Count > 0)
                    vizcore3d.ShapeDrawing.AddLine(mfgExtLines, -1, System.Drawing.Color.FromArgb(120, 120, 200), 0.5f, true);

                vizcore3d.EndUpdate();

                // 풍선 배치
                float modelDiag = (float)Math.Sqrt(sizeX * sizeX + sizeY * sizeY + sizeZ * sizeZ);
                float baseOffset = Math.Max(modelDiag * 0.35f, 70f);
                float lineSpacing = Math.Max(modelDiag * 0.08f, 20f);
                int balloonIdx = 0;

                // 반지름 풍선
                bool isTrueCylinder = false;
                if (bom.CircleRadius > 0)
                {
                    float diam = bom.CircleRadius * 2f;
                    float bsX = Math.Abs(bom.MaxX - bom.MinX);
                    float bsY = Math.Abs(bom.MaxY - bom.MinY);
                    float bsZ = Math.Abs(bom.MaxZ - bom.MinZ);
                    float ct = Math.Max(2f, diam * 0.2f);
                    int mCnt = 0;
                    if (Math.Abs(bsX - diam) < ct) mCnt++;
                    if (Math.Abs(bsY - diam) < ct) mCnt++;
                    if (Math.Abs(bsZ - diam) < ct) mCnt++;
                    isTrueCylinder = mCnt >= 2;
                }
                if (isTrueCylinder)
                {
                    try
                    {
                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);
                        float offH = baseOffset;
                        float offV = baseOffset + balloonIdx * lineSpacing;
                        VIZCore3D.NET.Data.Vertex3D textPos;
                        switch (viewDirection)
                        {
                            case "X": textPos = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.MinY - offH, bom.MaxZ + offV); break;
                            case "Y": textPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - offH, bom.CenterY, bom.MaxZ + offV); break;
                            default: textPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - offH, bom.MaxY + offV, bom.CenterZ); break;
                        }

                        VIZCore3D.NET.Data.NoteStyle circleStyle = vizcore3d.Review.Note.GetStyle();
                        circleStyle.UseSymbol = false;
                        circleStyle.BackgroudTransparent = true;
                        circleStyle.FontBold = true;
                        circleStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                        circleStyle.FontColor = Color.Red;
                        circleStyle.LineColor = Color.Red;
                        circleStyle.LineWidth = 1;
                        circleStyle.ArrowColor = Color.Red;
                        circleStyle.ArrowWidth = 3;

                        vizcore3d.Review.Note.AddNoteSurface($"R{bom.CircleRadius:F1}", textPos, center, circleStyle);
                        balloonIdx++;
                    }
                    catch { }
                }

                // 홀 풍선
                if (bom.Holes != null && bom.Holes.Count > 0)
                {
                    try
                    {
                        var mfgHoleGroups = bom.Holes.GroupBy(h => Math.Round(h.Diameter, 1));
                        foreach (var grp in mfgHoleGroups)
                        {
                            int count = grp.Count();
                            string holeText = count > 1 ? $"\u00d8{grp.Key:F1} * {count}개" : $"\u00d8{grp.Key:F1}";
                            var hole = grp.First();
                            VIZCore3D.NET.Data.Vertex3D holeCenter = new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, hole.CenterY, hole.CenterZ);

                            float hOffH = baseOffset;
                            float hOffV = baseOffset + balloonIdx * lineSpacing;
                            VIZCore3D.NET.Data.Vertex3D holeTextPos;
                            switch (viewDirection)
                            {
                                case "X": holeTextPos = new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, bom.MinY - hOffH, bom.MaxZ + hOffV); break;
                                case "Y": holeTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - hOffH, hole.CenterY, bom.MaxZ + hOffV); break;
                                default: holeTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - hOffH, bom.MaxY + hOffV, hole.CenterZ); break;
                            }

                            VIZCore3D.NET.Data.NoteStyle holeStyle = vizcore3d.Review.Note.GetStyle();
                            holeStyle.UseSymbol = false;
                            holeStyle.BackgroudTransparent = true;
                            holeStyle.FontBold = true;
                            holeStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                            holeStyle.FontColor = Color.FromArgb(0, 160, 0);
                            holeStyle.LineColor = Color.FromArgb(0, 160, 0);
                            holeStyle.LineWidth = 1;
                            holeStyle.ArrowColor = Color.FromArgb(0, 160, 0);
                            holeStyle.ArrowWidth = 3;

                            vizcore3d.Review.Note.AddNoteSurface(holeText, holeTextPos, holeCenter, holeStyle);
                            balloonIdx++;
                        }
                    }
                    catch { }
                }

                // 슬롯홀 풍선
                if (bom.SlotHoles != null && bom.SlotHoles.Count > 0)
                {
                    try
                    {
                        var slotGroups = bom.SlotHoles.GroupBy(s =>
                            $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                        foreach (var grp in slotGroups)
                        {
                            var slot = grp.First();
                            int count = grp.Count();
                            float slotWidth = slot.Radius * 2f;
                            string slotText = count > 1
                                ? $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0}) * {count}개"
                                : $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})";

                            VIZCore3D.NET.Data.Vertex3D slotCenter = new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, slot.CenterY, slot.CenterZ);
                            float sOffH = baseOffset;
                            float sOffV = baseOffset + balloonIdx * lineSpacing;
                            VIZCore3D.NET.Data.Vertex3D slotTextPos;
                            switch (viewDirection)
                            {
                                case "X": slotTextPos = new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, bom.MaxY + sOffH, bom.MaxZ + sOffV); break;
                                case "Y": slotTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MaxX + sOffH, slot.CenterY, bom.MaxZ + sOffV); break;
                                default: slotTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MaxX + sOffH, bom.MaxY + sOffV, slot.CenterZ); break;
                            }

                            VIZCore3D.NET.Data.NoteStyle slotStyle = vizcore3d.Review.Note.GetStyle();
                            slotStyle.UseSymbol = false;
                            slotStyle.BackgroudTransparent = true;
                            slotStyle.FontBold = true;
                            slotStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                            slotStyle.FontColor = Color.FromArgb(180, 0, 180);
                            slotStyle.LineColor = Color.FromArgb(180, 0, 180);
                            slotStyle.LineWidth = 1;
                            slotStyle.ArrowColor = Color.FromArgb(180, 0, 180);
                            slotStyle.ArrowWidth = 3;

                            vizcore3d.Review.Note.AddNoteSurface(slotText, slotTextPos, slotCenter, slotStyle);
                            balloonIdx++;
                        }
                    }
                    catch { }
                }

                // 10. Z가 최장축이면 90° 회전하여 Z를 수평으로 표시
                //     반드시 모든 drawing 완료 후 마지막에 적용해야 유지됨
                //     LockZAxis를 false로 유지 (true로 복원하면 렌더링 엔진이 회전을 리셋)
                if (longestAxis == "Z")
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 도면정보 탭 - 가공도 출력 버튼 클릭
        /// lvDrawingSheet에서 "가공도"로 시작하는 모든 시트를 수집하여 2D 일괄 출력
        /// </summary>
        private void btnMfgDrawingSheet_Click(object sender, EventArgs e)
        {
            var mfgSheets = new List<DrawingSheetData>();
            foreach (ListViewItem lvi in lvDrawingSheet.Items)
            {
                if (lvi.Text.StartsWith("가공도"))
                {
                    var s = lvi.Tag as DrawingSheetData;
                    if (s != null && s.MemberIndices.Count > 0)
                        mfgSheets.Add(s);
                }
            }

            if (mfgSheets.Count == 0)
            {
                MessageBox.Show("가공도 시트가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GenerateMfgDrawing2DAll(mfgSheets);
        }

        /// <summary>
        /// 가공도 시트 목록을 받아 8행×3열 그리드에 2D 일괄 출력
        /// GenerateSheetDrawing2D와 동일한 초기화 패턴, BOM 테이블 없이 도면정보만
        /// </summary>
        private void GenerateMfgDrawing2DAll(List<DrawingSheetData> mfgSheets)
        {
            try
            {
                vizcore3d.View.EnableAnimation = false;

                // ── 0. 기존 3D 어노테이션 모두 초기화 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // ── 1. 2D 초기화 (기존 도면 완전 삭제 후 새로 생성) ──
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

                vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView();

                int canvasCount = vizcore3d.Drawing2D.View.GetCanvasCountBy2DView();
                for (int c = canvasCount; c >= 1; c--)
                {
                    vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(c);
                }

                vizcore3d.ToolbarDrawing2D.Visible = false;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Model3D;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;
                vizcore3d.ToolbarDrawing2D.Visible = true;

                Application.DoEvents();
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

                // ── 2. 템플릿 생성 — BOM 없이 도면정보(표2)만 ──
                vizcore3d.Drawing2D.Template.CreateTemplate();

                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));

                table2.X = 310;
                table2.Y = 200;
                vizcore3d.Drawing2D.Template.AddTemplateItem(table2);

                vizcore3d.Drawing2D.Template.RenderTemplate(30, 40);

                // ── 3. 그리드 구조 (8행 × 3열, 1행/8행 비움, 2~7행 사용) ──
                // 채우기 순서: 열 우선 (위→아래, 좌→우)
                //   col1: 1~6  col2: 7~12  col3: 13~18  (최대 18개, 19~24 비움)
                const int gridRows = 8;
                const int gridCols = 3;
                const int usableRowStart = 2;  // 2행부터
                const int usableRowEnd = 7;    // 7행까지
                const int rowsPerCol = usableRowEnd - usableRowStart + 1; // 6

                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                vizcore3d.Drawing2D.GridStructure.AddGridStructure(gridRows, gridCols, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);

                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(7f);

                // ── 4. 각 가공도 시트를 열 우선 순서로 셀에 배치 (2~7행만 사용) ──
                int maxSlots = rowsPerCol * gridCols; // 18
                int count = Math.Min(mfgSheets.Count, maxSlots);
                for (int i = 0; i < count; i++)
                {
                    int col = (i / rowsPerCol) + 1;             // 0~5→col1, 6~11→col2, 12~17→col3
                    int row = (i % rowsPerCol) + usableRowStart; // 2~7행
                    RenderMfgViewForDrawing(row, col, mfgSheets[i].MemberIndices[0]);
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
        /// 가공도 셀 렌더 헬퍼: ExecuteMfgDrawing 치수/풍선 로직 + RenderSheetViewForDrawing 2D 캡처 패턴 결합
        /// </summary>
        private int RenderMfgViewForDrawing(int row, int col, int bomIndex)
        {
            BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIndex);
            if (bom == null) return -1;

            List<int> shapeDrawingIds = new List<int>();

            // 1. 3D 어노테이션 초기화
            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

            // 2. 부재 표시: XRay 끄기 → 전체 숨김 → 해당 bom만 Show
            vizcore3d.BeginUpdate();
            if (vizcore3d.View.XRay.Enable)
                vizcore3d.View.XRay.Enable = false;
            vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
            List<int> targetIndices = new List<int> { bom.Index };
            vizcore3d.Object3D.Show(targetIndices, true);
            vizcore3d.EndUpdate();

            // 3. 최장축 판별 → 카메라 방향 결정
            float sizeX = bom.MaxX - bom.MinX;
            float sizeY = bom.MaxY - bom.MinY;
            float sizeZ = bom.MaxZ - bom.MinZ;

            string longestAxis;
            if (sizeX >= sizeY && sizeX >= sizeZ)
                longestAxis = "X";
            else if (sizeY >= sizeX && sizeY >= sizeZ)
                longestAxis = "Y";
            else
                longestAxis = "Z";

            string viewDirection;
            switch (longestAxis)
            {
                case "Y":
                    viewDirection = "X";
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                    break;
                default:
                    viewDirection = "Y";
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                    break;
            }

            // 4. DASH_LINE + SilhouetteEdge + FlyToObject3d (은선 점선 포함 2D 캡처용)
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            vizcore3d.View.SilhouetteEdge = true;
            vizcore3d.View.SilhouetteEdgeColor = Color.Green;
            vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);

            // 5. Osnap 수집 → MergeCoordinates → 체인치수 추출
            var mfgOsnapWithNames = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
            var osnapListMfg = vizcore3d.Object3D.GetOsnapPoint(bom.Index);
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
                        case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                        case VIZCore3D.NET.Data.OsnapKind.POINT:
                            if (osnap.Center != null)
                                mfgOsnapWithNames.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z), bom.Name));
                            break;
                    }
                }
            }

            bool hasDimensions = mfgOsnapWithNames.Count > 0;
            float mfgTotalOff = 250.0f; // 기본값; hasDimensions 블록에서 갱신

            if (hasDimensions)
            {
                // 6. 좌표 병합 + 뷰 방향 기준 visible 축만 체인치수 추출
                float tolerance = 0.5f;
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(mfgOsnapWithNames, tolerance);

                List<string> mfgVisibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": mfgVisibleAxes.Add("Y"); mfgVisibleAxes.Add("Z"); break;
                    case "Y": mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Z"); break;
                    default:  mfgVisibleAxes.Add("X"); mfgVisibleAxes.Add("Y"); break;
                }

                var mfgDimensions = new List<ChainDimensionData>();
                foreach (var ax in mfgVisibleAxes)
                    mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, ax, tolerance, viewDirection));

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
                    // 7. 치수 그리기
                    vizcore3d.BeginUpdate();
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
                    mfgStyle.FontColor = System.Drawing.Color.Cyan;
                    mfgStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                    mfgStyle.FontBold = true;
                    mfgStyle.LineColor = System.Drawing.Color.Cyan;
                    mfgStyle.LineWidth = 1;
                    mfgStyle.ArrowColor = System.Drawing.Color.Cyan;
                    mfgStyle.ArrowSize = 5;
                    mfgStyle.AssistantLine = false;
                    mfgStyle.AlignDistanceText = true;
                    mfgStyle.AlignDistanceTextPosition = 0;
                    mfgStyle.AlignDistanceTextMargine = 3;
                    vizcore3d.Review.Measure.SetStyle(mfgStyle);

                    float mfgGlobalMinX = bom.MinX, mfgGlobalMinY = bom.MinY, mfgGlobalMinZ = bom.MinZ;
                    float mfgGlobalMaxX = bom.MaxX, mfgGlobalMaxY = bom.MaxY, mfgGlobalMaxZ = bom.MaxZ;
                    float mfgCenterX = (mfgGlobalMinX + mfgGlobalMaxX) / 2f;
                    float mfgCenterY = (mfgGlobalMinY + mfgGlobalMaxY) / 2f;
                    float mfgCenterZ = (mfgGlobalMinZ + mfgGlobalMaxZ) / 2f;

                    var mfgAxisPosOff = new Dictionary<string, bool>();
                    foreach (var grp in mfgDimensions.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                    {
                        string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                        float sumV = 0; int cnt = 0;
                        foreach (var d in grp)
                        {
                            sumV += GetAxisValue(d.StartPoint, offAxis);
                            sumV += GetAxisValue(d.EndPoint, offAxis);
                            cnt += 2;
                        }
                        float avg = cnt > 0 ? sumV / cnt : 0;
                        float centerVal = offAxis == "X" ? mfgCenterX : offAxis == "Y" ? mfgCenterY : mfgCenterZ;
                        mfgAxisPosOff[grp.Key] = avg >= centerVal;
                    }

                    var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
                    const float mfgChainOff1 = 100.0f;  // 1단 체인치수 보조선 100mm
                    const float mfgChainOff2 = 200.0f;  // 2단 체인치수 보조선 200mm

                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                    {
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff1,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                    {
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff2,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => d.IsTotal && d.IsVisible))
                    {
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgTotalOff,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    if (mfgExtLines.Count > 0)
                    {
                        int shapeId = vizcore3d.ShapeDrawing.AddLine(mfgExtLines, -1, System.Drawing.Color.Cyan, 0.3f, true);
                        if (shapeId >= 0) shapeDrawingIds.Add(shapeId);
                    }

                    vizcore3d.EndUpdate();
                }
            }

            // 8. 풍선 배치 — 수집 후 일괄 배치 (AABB 겹침 방지)
            float dimMaxOffset = mfgTotalOff + 20.0f;
            float lineSpacing = Math.Max(sizeX * 0.05f, 15f);
            float cos45 = 0.707f;

            // 뷰 방향별 축 매핑 (hAxis=화면 수평, vAxis=화면 수직)
            float hMin, hMax, vMin, vMax, hCenter, vCenter;
            switch (viewDirection)
            {
                case "X": hMin = bom.MinY; hMax = bom.MaxY; vMin = bom.MinZ; vMax = bom.MaxZ;
                          hCenter = bom.CenterY; vCenter = bom.CenterZ; break;
                case "Y": hMin = bom.MinX; hMax = bom.MaxX; vMin = bom.MinZ; vMax = bom.MaxZ;
                          hCenter = bom.CenterX; vCenter = bom.CenterZ; break;
                default:  hMin = bom.MinX; hMax = bom.MaxX; vMin = bom.MinY; vMax = bom.MaxY;
                          hCenter = bom.CenterX; vCenter = bom.CenterY; break;
            }

            // 4방향 후보: NE(+,+) NW(-,+) SE(+,-) SW(-,-)
            float[][] dirs45 = new float[][] {
                new float[] { +1f, +1f },
                new float[] { -1f, +1f },
                new float[] { +1f, -1f },
                new float[] { -1f, -1f }
            };

            // 텍스트 크기 추정 (SIZE8 폰트 기준, mm 단위)
            Func<string, (float w, float h)> mfgEstTextSize = (text) =>
            {
                return (text.Length * 5f, 10f);
            };

            // --- 풍선 항목 수집 (배치는 아래에서 일괄) ---
            List<(float originH, float originV, float depthVal, string text, Color color,
                  float arrowX, float arrowY, float arrowZ)> mfgBalloonEntries =
                new List<(float, float, float, string, Color, float, float, float)>();

            // 반지름 풍선 수집
            bool isTrueCylinder = false;
            if (bom.CircleRadius > 0)
            {
                float diam = bom.CircleRadius * 2f;
                float bsX = Math.Abs(bom.MaxX - bom.MinX);
                float bsY = Math.Abs(bom.MaxY - bom.MinY);
                float bsZ = Math.Abs(bom.MaxZ - bom.MinZ);
                float ct = Math.Max(2f, diam * 0.2f);
                int mCnt = 0;
                if (Math.Abs(bsX - diam) < ct) mCnt++;
                if (Math.Abs(bsY - diam) < ct) mCnt++;
                if (Math.Abs(bsZ - diam) < ct) mCnt++;
                isTrueCylinder = mCnt >= 2;
            }
            if (isTrueCylinder)
            {
                float depthVal = viewDirection == "X" ? bom.CenterX : viewDirection == "Y" ? bom.CenterY : bom.CenterZ;
                mfgBalloonEntries.Add((hCenter, vCenter, depthVal,
                    $"R{bom.CircleRadius:F1}", Color.Red,
                    bom.CenterX, bom.CenterY, bom.CenterZ));
            }

            // 홀 풍선 수집
            if (bom.Holes != null && bom.Holes.Count > 0)
            {
                try
                {
                    var mfgHoleGroups = bom.Holes.GroupBy(h => Math.Round(h.Diameter, 1));
                    foreach (var grp in mfgHoleGroups)
                    {
                        int hCount = grp.Count();
                        string holeText = hCount > 1 ? $"\u00d8{grp.Key:F1} * {hCount}개" : $"\u00d8{grp.Key:F1}";
                        var hole = grp.First();
                        float oH = viewDirection == "X" ? hole.CenterY : hole.CenterX;
                        float oV = viewDirection == "Z" ? hole.CenterY : hole.CenterZ;
                        float depthVal = viewDirection == "X" ? hole.CenterX : viewDirection == "Y" ? hole.CenterY : hole.CenterZ;
                        mfgBalloonEntries.Add((oH, oV, depthVal, holeText, Color.FromArgb(0, 160, 0),
                            hole.CenterX, hole.CenterY, hole.CenterZ));
                    }
                }
                catch { }
            }

            // 슬롯홀 풍선 수집
            if (bom.SlotHoles != null && bom.SlotHoles.Count > 0)
            {
                try
                {
                    var slotGroups = bom.SlotHoles.GroupBy(s =>
                        $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                    foreach (var grp in slotGroups)
                    {
                        var slot = grp.First();
                        int sCount = grp.Count();
                        float slotWidth = slot.Radius * 2f;
                        string slotText = sCount > 1
                            ? $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0}) * {sCount}개"
                            : $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})";
                        float oH = viewDirection == "X" ? slot.CenterY : slot.CenterX;
                        float oV = viewDirection == "Z" ? slot.CenterY : slot.CenterZ;
                        float depthVal = viewDirection == "X" ? slot.CenterX : viewDirection == "Y" ? slot.CenterY : slot.CenterZ;
                        mfgBalloonEntries.Add((oH, oV, depthVal, slotText, Color.FromArgb(180, 0, 180),
                            slot.CenterX, slot.CenterY, slot.CenterZ));
                    }
                }
                catch { }
            }

            // --- 풍선 일괄 배치 (4방향 × 여러 거리, AABB 겹침 방지) ---
            List<(float h, float v, float halfW, float halfH)> mfgPlacedBoxes =
                new List<(float, float, float, float)>();

            int mfgBalloonIdx = 0;
            foreach (var entry in mfgBalloonEntries)
            {
                try
                {
                    float baseDist = dimMaxOffset + mfgBalloonIdx * lineSpacing;
                    var textSz = mfgEstTextSize(entry.text);
                    float halfW = textSz.w / 2f;
                    float halfH = textSz.h / 2f;

                    float bestH = 0, bestV = 0;
                    float bestScore = float.MaxValue;
                    bool found = false;

                    // 4방향 × 5단계 거리 중 비충돌 + 원점 최근접 선택
                    for (int distMult = 0; distMult < 5 && !found; distMult++)
                    {
                        float tryDist = baseDist + distMult * lineSpacing;
                        foreach (var d in dirs45)
                        {
                            float cH = (d[0] > 0 ? hMax : hMin) + d[0] * tryDist * cos45;
                            float cV = (d[1] > 0 ? vMax : vMin) + d[1] * tryDist * cos45;

                            // AABB 겹침 검사
                            bool collision = false;
                            foreach (var placed in mfgPlacedBoxes)
                            {
                                if (Math.Abs(cH - placed.h) < (halfW + placed.halfW + 3f) &&
                                    Math.Abs(cV - placed.v) < (halfH + placed.halfH + 3f))
                                { collision = true; break; }
                            }

                            if (!collision)
                            {
                                float dh = cH - entry.originH;
                                float dv = cV - entry.originV;
                                float score = dh * dh + dv * dv;
                                if (score < bestScore)
                                {
                                    bestScore = score;
                                    bestH = cH;
                                    bestV = cV;
                                    found = true;
                                }
                            }
                        }
                    }

                    // 모든 시도 실패 시 기본 위치
                    if (!found)
                    {
                        float fallDist = baseDist;
                        bestH = hMax + fallDist * cos45;
                        bestV = vMax + fallDist * cos45;
                    }

                    // 3D 좌표 복원
                    float[] xyz = new float[3];
                    switch (viewDirection)
                    {
                        case "X": xyz[0] = entry.depthVal; xyz[1] = bestH; xyz[2] = bestV; break;
                        case "Y": xyz[0] = bestH; xyz[1] = entry.depthVal; xyz[2] = bestV; break;
                        default:  xyz[0] = bestH; xyz[1] = bestV; xyz[2] = entry.depthVal; break;
                    }

                    VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(xyz[0], xyz[1], xyz[2]);
                    VIZCore3D.NET.Data.Vertex3D arrowPos = new VIZCore3D.NET.Data.Vertex3D(
                        entry.arrowX, entry.arrowY, entry.arrowZ);

                    VIZCore3D.NET.Data.NoteStyle mfgStyle = vizcore3d.Review.Note.GetStyle();
                    mfgStyle.UseSymbol = false;
                    mfgStyle.BackgroudTransparent = true;
                    mfgStyle.FontBold = true;
                    mfgStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                    mfgStyle.FontColor = entry.color;
                    mfgStyle.LineColor = entry.color;
                    mfgStyle.LineWidth = 1;
                    mfgStyle.ArrowColor = entry.color;
                    mfgStyle.ArrowWidth = 2;

                    vizcore3d.Review.Note.AddNoteSurface(entry.text, textPos, arrowPos, mfgStyle);
                    mfgPlacedBoxes.Add((bestH, bestV, halfW, halfH));
                    mfgBalloonIdx++;
                }
                catch { }
            }

            // 9. Z가 최장축이면 90° 회전하여 Z를 수평으로 표시
            if (longestAxis == "Z")
            {
                vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
            }

            // 10. 2D 투영: 은선 포함 2D 변환 (모델 실선 = 굵게)
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            int objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

            // 11. 그리드 셀에 맞추기
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
                    float targetW = contentW * 0.04f;
                    float targetH = contentH * 0.04f;
                    float scaleW = targetW / objW;
                    float scaleH = targetH / objH;
                    float fitScale = Math.Min(scaleW, scaleH);

                    if (fitScale > 0 && Math.Abs(fitScale - 1.0f) > 0.01f)
                    {
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, fitScale);
                    }
                }
            }

            // 12. 3D→2D 변환: ShapeDrawing(보조선) → 2D (가늘게 + 대쉬더블돗트)
            if (shapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.DASHED_DOUBLEDOTTED);
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
            }

            // Note(풍선) → 2D (텍스트 높이 50% 축소)
            List<int> noteIds = new List<int>();
            List<VIZCore3D.NET.Data.NoteItem> notes = vizcore3d.Review.Note.Items;
            foreach (var note in notes)
            {
                noteIds.Add(note.ID);
            }
            if (noteIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(3.5f);
                vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);
            }

            // Measure(치수선) → 2D (보조선과 동일하게 얇게)
            List<int> measureIds = new List<int>();
            List<VIZCore3D.NET.Data.MeasureItem> measures = vizcore3d.Review.Measure.Items;
            foreach (var measure in measures)
            {
                if (measure.Visible)
                    measureIds.Add(measure.ID);
            }
            if (measureIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.1f);
                vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
            }

            // 다음 셀의 모델 실선을 위해 두께 복원
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);

            // 13. 부재 표시 복원
            vizcore3d.BeginUpdate();
            vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
            vizcore3d.View.XRay.Enable = true;
            vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
            vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
            vizcore3d.View.XRay.Clear();
            vizcore3d.EndUpdate();

            return objId;
        }

        #endregion
    }
}
