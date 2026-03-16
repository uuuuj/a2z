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

                // ── 1. 2D 완전 초기화 ──
                Clear2DView();

                // 2D 패널 크기 조정
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

                // ── 2. 캔버스 + 그리드 구조 새로 생성 ──
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);  // A4 가로

                const int gridRows = 8;
                const int gridCols = 6;   // 라벨(1,3,5) + 모델(2,4,6)
                const int usableRowStart = 2;  // 2행부터
                const int usableRowEnd = 7;    // 7행까지
                const int rowsPerCol = usableRowEnd - usableRowStart + 1; // 6

                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                vizcore3d.Drawing2D.GridStructure.AddGridStructure(gridRows, gridCols, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);

                // ── 3. 템플릿 생성 (그리드 생성 후 호출) ──
                VIZCore3D.NET.Data.TemplateBorderInfo bInfo = vizcore3d.Drawing2D.Template.CrateTemplateBorder();

                // 도면정보 — 우측 하단 모서리에 Anchor 방식으로 배치
                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));
                table2.ImageHeight = 50;
                table2.IsTextWrapped = true;
                table2.ColumnWidths = new Dictionary<int, int>() { { 0, 15 }, { 1, 30 }, { 2, 10 }, { 3, 10 } };

                // 그리드 [gridRows, gridCols] 셀을 우측 하단 정렬 후 배치
                vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(gridRows, gridCols,
                    VIZCore3D.NET.Data.GridVerticalAlignment.Bottom);
                vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(gridRows, gridCols,
                    VIZCore3D.NET.Data.GridHorizontalAlignment.Right);
                vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(table2, gridRows, gridCols);

                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(7f);

                // ── 4. 각 가공도 시트를 열 우선 순서로 셀에 배치 (2~7행만 사용) ──
                // 라벨 칼럼(1,3,5) + 모델 칼럼(2,4,6) 구조
                const int modelGroupCount = 3;  // 3개 모델 그룹
                int maxSlots = rowsPerCol * modelGroupCount; // 18
                int count = Math.Min(mfgSheets.Count, maxSlots);
                for (int i = 0; i < count; i++)
                {
                    int modelGroup = i / rowsPerCol;              // 0, 1, 2
                    int rowInGroup = i % rowsPerCol;              // 0~5
                    int row = rowInGroup + usableRowStart;        // 2~7행
                    int labelCol = modelGroup * 2 + 1;            // 1, 3, 5
                    int modelCol = modelGroup * 2 + 2;            // 2, 4, 6

                    // 모델 2D 렌더링
                    RenderMfgViewForDrawing(row, modelCol, mfgSheets[i].MemberIndices[0]);

                    // 라벨 배치 (모델 Name)
                    try
                    {
                        BOMData labelBom = bomList.FirstOrDefault(b => b.Index == mfgSheets[i].MemberIndices[0]);
                        if (labelBom != null && !string.IsNullOrEmpty(labelBom.Name))
                        {
                            VIZCore3D.NET.Data.TemplateTableData labelTable = new VIZCore3D.NET.Data.TemplateTableData(1, 1);
                            labelTable.SetText(0, 0, labelBom.Name);
                            labelTable.IsTextWrapped = true;
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

            // EA 앵글 판별 및 위/아래 넓이 감지
            bool isEA = IsAngleFromSpref(bom.Index);
            bool isAboveWider = false;  // true: 위쪽 넓음 → 신규뷰 위, false: 아래쪽 넓음 → 신규뷰 아래
            bool isLShape = false;
            if (isEA && mfgOsnapWithNames.Count > 0)
            {
                // 뷰의 수직축 결정 (화면에서 위/아래 방향)
                // viewDirection Z → Y가 수직, viewDirection X/Y → Z가 수직
                string vertAxis;
                switch (viewDirection)
                {
                    case "Z": vertAxis = "Y"; break;
                    default:  vertAxis = "Z"; break;
                }
                float bbCenterVert = (vertAxis == "Y") ? (bom.MinY + bom.MaxY) / 2f :
                                                          (bom.MinZ + bom.MaxZ) / 2f;

                // 수직축 기준 위/아래 Osnap 간 거리(spread) 비교
                float aboveMin = float.MaxValue, aboveMax = float.MinValue;
                float belowMin = float.MaxValue, belowMax = float.MinValue;
                foreach (var pt in mfgOsnapWithNames)
                {
                    float val = (vertAxis == "Y") ? pt.point.Y : pt.point.Z;
                    if (val > bbCenterVert)
                    {
                        if (val < aboveMin) aboveMin = val;
                        if (val > aboveMax) aboveMax = val;
                    }
                    else
                    {
                        if (val < belowMin) belowMin = val;
                        if (val > belowMax) belowMax = val;
                    }
                }
                float aboveSpread = (aboveMax > aboveMin) ? aboveMax - aboveMin : 0f;
                float belowSpread = (belowMax > belowMin) ? belowMax - belowMin : 0f;

                // 위쪽이 넓으면 → 신규뷰 위, 아래쪽이 넓으면 → 신규뷰 아래
                isAboveWider = aboveSpread > belowSpread;
                // isLShape 호환 유지 (아래 넓음 = L자)
                isLShape = !isAboveWider;
            }

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

                    // EA 앵글: 길이방향 체인치수 방향 강제 오버라이드
                    // 아래 넓음(신규뷰 아래) → 기존뷰 치수를 위(positive)로
                    // 위쪽 넓음(신규뷰 위)   → 기존뷰 치수를 아래(negative)로
                    if (isEA && mfgAxisPosOff.ContainsKey(longestAxis))
                    {
                        mfgAxisPosOff[longestAxis] = !isAboveWider;
                    }

                    var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
                    const float mfgChainOff1 = 100.0f;  // 1단 체인치수 보조선 100mm
                    const float mfgChainOff2 = 200.0f;  // 2단 체인치수 보조선 200mm

                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                    {
                        // EA 앵글 L자: 기존뷰가 위 → 길이축 체인치수는 아래(신규뷰)에만 표시
                        if (isEA && isLShape && dim.Axis == longestAxis) continue;
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff1,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                    {
                        if (isEA && isLShape && dim.Axis == longestAxis) continue;
                        bool posOff = mfgAxisPosOff.ContainsKey(dim.Axis) && mfgAxisPosOff[dim.Axis];
                        DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, mfgChainOff2,
                            mfgGlobalMinX, mfgGlobalMinY, mfgGlobalMinZ, viewDirection, mfgExtLines,
                            mfgGlobalMaxX, mfgGlobalMaxY, mfgGlobalMaxZ, posOff);
                    }
                    foreach (var dim in mfgDimensions.Where(d => d.IsTotal && d.IsVisible))
                    {
                        if (isEA && isLShape && dim.Axis == longestAxis) continue;
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

                var mfgAxisPosOff_m = new Dictionary<string, bool>();
                foreach (var grp in allMfgDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                {
                    string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                    float sumV2 = 0; int cnt2 = 0;
                    foreach (var d in grp)
                    {
                        sumV2 += GetAxisValue(d.StartPoint, offAxis);
                        sumV2 += GetAxisValue(d.EndPoint, offAxis);
                        cnt2 += 2;
                    }
                    float avg2 = cnt2 > 0 ? sumV2 / cnt2 : 0;
                    float cv2 = offAxis == "X" ? mfgCX : offAxis == "Y" ? mfgCY : mfgCZ;
                    mfgAxisPosOff_m[grp.Key] = avg2 >= cv2;
                }

                // EA 앵글: 길이방향 체인치수 방향 오버라이드
                if (isEA && mfgAxisPosOff_m.ContainsKey(longestAxis))
                    mfgAxisPosOff_m[longestAxis] = !isAboveWider;

                const float mfgOff1 = 100.0f, mfgOff2 = 200.0f;
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
                float mfgTotalOff_m = maxTotalDist_m > 1000.0f ? 300.0f : 250.0f;

                foreach (var dim in allMfgDims.Where(d => d.IsVisible))
                {
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
                float oH_c = mfgMaxArr[bHAxis_m] / 2f + mfgMinArr[bHAxis_m] / 2f; // center H
                float oV_c = mfgMaxArr[bVAxis_m] / 2f + mfgMinArr[bVAxis_m] / 2f; // center V
                float depthVal = viewDirection == "X" ? bom.CenterX : viewDirection == "Y" ? bom.CenterY : bom.CenterZ;
                mfgBalloonEntries.Add((oH_c, oV_c, depthVal,
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

            // === EA 앵글: 길이방향 90° 회전 뷰 (L자를 펼쳐서 보는 뷰) ===
            if (isEA)
            {
                try
                {
                    List<int> eaShapeIds = new List<int>();

                    // 3D 어노테이션 초기화 (정면뷰 어노테이션은 이미 2D로 변환 완료)
                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();

                    // Z축 회전 복원 (적용된 경우)
                    if (longestAxis == "Z")
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, -90);

                    // 길이축 기준 90° 회전한 카메라 방향 결정
                    VIZCore3D.NET.Data.CameraDirection newCamDir;
                    string newViewDir;
                    bool needZRotation = false;
                    switch (longestAxis)
                    {
                        case "Y":
                            newCamDir = VIZCore3D.NET.Data.CameraDirection.Z_MINUS;
                            newViewDir = "Z";
                            break;
                        case "Z":
                            newCamDir = VIZCore3D.NET.Data.CameraDirection.X_MINUS;
                            newViewDir = "X";
                            needZRotation = true;
                            break;
                        default: // X
                            newCamDir = VIZCore3D.NET.Data.CameraDirection.Z_MINUS;
                            newViewDir = "Z";
                            break;
                    }

                    vizcore3d.View.MoveCamera(newCamDir);
                    vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                    if (needZRotation)
                    {
                        vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    }

                    // 신규뷰 체인치수 계산 및 그리기
                    if (hasDimensions && mfgOsnapWithNames.Count > 0)
                    {
                        float tol = 0.5f;
                        var newMerged = MergeCoordinates(mfgOsnapWithNames, tol);

                        List<string> newVisAxes = new List<string>();
                        switch (newViewDir)
                        {
                            case "X": newVisAxes.Add("Y"); newVisAxes.Add("Z"); break;
                            case "Y": newVisAxes.Add("X"); newVisAxes.Add("Z"); break;
                            default:  newVisAxes.Add("X"); newVisAxes.Add("Y"); break;
                        }

                        var newDims = new List<ChainDimensionData>();
                        foreach (var ax in newVisAxes)
                            newDims.AddRange(AddChainDimensionByAxis(newMerged, ax, tol, newViewDir));

                        if (newDims.Count > 0)
                        {
                            vizcore3d.BeginUpdate();

                            // 치수 스타일 (기존뷰와 동일)
                            VIZCore3D.NET.Data.MeasureStyle eaStyle = vizcore3d.Review.Measure.GetStyle();
                            eaStyle.Prefix = false;
                            eaStyle.Unit = false;
                            eaStyle.NumberOfDecimalPlaces = 0;
                            eaStyle.DX_DY_DZ = false;
                            eaStyle.Frame = false;
                            eaStyle.ContinuousDistance = false;
                            eaStyle.BackgroundTransparent = true;
                            eaStyle.FontColor = System.Drawing.Color.Cyan;
                            eaStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                            eaStyle.FontBold = true;
                            eaStyle.LineColor = System.Drawing.Color.Cyan;
                            eaStyle.LineWidth = 1;
                            eaStyle.ArrowColor = System.Drawing.Color.Cyan;
                            eaStyle.ArrowSize = 5;
                            eaStyle.AssistantLine = false;
                            eaStyle.AlignDistanceText = true;
                            eaStyle.AlignDistanceTextPosition = 0;
                            eaStyle.AlignDistanceTextMargine = 3;
                            vizcore3d.Review.Measure.SetStyle(eaStyle);

                            // 신규뷰 체인치수 방향: 길이축은 기존뷰와 반대
                            var eaAxisPosOff = new Dictionary<string, bool>();
                            eaAxisPosOff[longestAxis] = !isLShape;  // 기존뷰 반대
                            // 비길이축: 자동 계산
                            float eaCX = (bom.MinX + bom.MaxX) / 2f;
                            float eaCY = (bom.MinY + bom.MaxY) / 2f;
                            float eaCZ = (bom.MinZ + bom.MaxZ) / 2f;
                            foreach (var grp in newDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                            {
                                if (eaAxisPosOff.ContainsKey(grp.Key)) continue;
                                string offAx = GetRemainingAxis(newViewDir, grp.Key);
                                float sum = 0; int cnt = 0;
                                foreach (var d in grp)
                                {
                                    sum += GetAxisValue(d.StartPoint, offAx);
                                    sum += GetAxisValue(d.EndPoint, offAx);
                                    cnt += 2;
                                }
                                float avg = cnt > 0 ? sum / cnt : 0;
                                float cv = offAx == "X" ? eaCX : offAx == "Y" ? eaCY : eaCZ;
                                eaAxisPosOff[grp.Key] = avg >= cv;
                            }

                            float eaMinX = bom.MinX, eaMinY = bom.MinY, eaMinZ = bom.MinZ;
                            float eaMaxX = bom.MaxX, eaMaxY = bom.MaxY, eaMaxZ = bom.MaxZ;

                            // 전체길이 오프셋
                            float eaMaxTotalDist = 0f;
                            foreach (var td in newDims.Where(d => d.IsTotal && d.IsVisible))
                            {
                                float dist = 0f;
                                switch (td.Axis)
                                {
                                    case "X": dist = Math.Abs(td.EndPoint.X - td.StartPoint.X); break;
                                    case "Y": dist = Math.Abs(td.EndPoint.Y - td.StartPoint.Y); break;
                                    case "Z": dist = Math.Abs(td.EndPoint.Z - td.StartPoint.Z); break;
                                }
                                if (dist > eaMaxTotalDist) eaMaxTotalDist = dist;
                            }
                            float eaTotalOff = eaMaxTotalDist > 1000.0f ? 300.0f : 250.0f;

                            var eaExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
                            const float eaChainOff1 = 100.0f;
                            const float eaChainOff2 = 200.0f;

                            foreach (var dim in newDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                            {
                                // EA 앵글 ㄱ자: 신규뷰가 위 → 길이축 체인치수는 아래(기존뷰)에만 표시
                                if (!isLShape && dim.Axis == longestAxis) continue;
                                bool posOff = eaAxisPosOff.ContainsKey(dim.Axis) && eaAxisPosOff[dim.Axis];
                                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, eaChainOff1,
                                    eaMinX, eaMinY, eaMinZ, newViewDir, eaExtLines,
                                    eaMaxX, eaMaxY, eaMaxZ, posOff);
                            }
                            foreach (var dim in newDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                            {
                                if (!isLShape && dim.Axis == longestAxis) continue;
                                bool posOff = eaAxisPosOff.ContainsKey(dim.Axis) && eaAxisPosOff[dim.Axis];
                                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, eaChainOff2,
                                    eaMinX, eaMinY, eaMinZ, newViewDir, eaExtLines,
                                    eaMaxX, eaMaxY, eaMaxZ, posOff);
                            }
                            foreach (var dim in newDims.Where(d => d.IsTotal && d.IsVisible))
                            {
                                if (!isLShape && dim.Axis == longestAxis) continue;
                                bool posOff = eaAxisPosOff.ContainsKey(dim.Axis) && eaAxisPosOff[dim.Axis];
                                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, eaTotalOff,
                                    eaMinX, eaMinY, eaMinZ, newViewDir, eaExtLines,
                                    eaMaxX, eaMaxY, eaMaxZ, posOff);
                            }
                            if (eaExtLines.Count > 0)
                            {
                                int sid = vizcore3d.ShapeDrawing.AddLine(eaExtLines, -1, System.Drawing.Color.Cyan, 0.3f, true);
                                if (sid >= 0) eaShapeIds.Add(sid);
                            }

                            vizcore3d.EndUpdate();
                        }
                    }

                    // 신규뷰 2D 투영
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                    int topObjId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                        VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

                    // 같은 셀에 맞추기 + 기존 뷰와 동일 스케일
                    vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, topObjId,
                        VIZCore3D.NET.Data.GridHorizontalAlignment.Center,
                        VIZCore3D.NET.Data.GridVerticalAlignment.Middle);
                    float frontScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                    vizcore3d.Drawing2D.Object2D.RescaleObject(topObjId, frontScale);

                    // ShapeDrawing(보조선) → 2D
                    if (eaShapeIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.DASHED_DOUBLEDOTTED);
                        vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(eaShapeIds);
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
                    }

                    // Measure(치수선) → 2D
                    List<int> eaMeasureIds = new List<int>();
                    foreach (var m in vizcore3d.Review.Measure.Items)
                    {
                        if (m.Visible) eaMeasureIds.Add(m.ID);
                    }
                    if (eaMeasureIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.1f);
                        vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(eaMeasureIds.ToArray());
                    }

                    // 두 뷰 크기 확인
                    float fW = 0, fH = 0, tW = 0, tH = 0;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref fW, ref fH);
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(topObjId, ref tW, ref tH);

                    // 배치: 위쪽 넓으면(isAboveWider) → 신규뷰 위, 아래쪽 넓으면 → 신규뷰 아래
                    float moveAmount = (fH / 2f) + (tH / 2f);
                    if (isAboveWider)
                        vizcore3d.Drawing2D.Object2D.MoveObject(topObjId, 0, -moveAmount);  // 위로
                    else
                        vizcore3d.Drawing2D.Object2D.MoveObject(topObjId, 0, moveAmount);   // 아래로

                    // 두께 복원
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                }
                catch { }
            }

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

        #endregion
    }
}
