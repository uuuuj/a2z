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

            // T-036 (2026-04-24 4차): 스냅샷 저장 여부 플래그 — try 블록 안에서 결정,
            //   finally 다음에 EndUpdate 후 GetCameraData() 호출. BeginUpdate 스코프 내부에서는
            //   ScreenAxisRotation이 commit 전 상태로 캡처돼 click-order 의존 버그 발생.
            bool shouldSnapshotMfgCamera = false;

            // T-036 (2026-04-23 재시도): 함수 전체를 BeginUpdate/EndUpdate로 감싸
            // 중간 카메라 회전(MoveCamera → FitToView → RotateCamera 여러 회)이 화면에 실시간
            // 노출돼 "가로 → 세로 깜빡" 현상이 보이던 문제 방지. 최종 결과만 화면에 반영.
            vizcore3d.BeginUpdate();

            try
            {
                // T-036: 가공도 진입 시 이전 선택상태(빨간색) 해제
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);

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

                // 4. 바운딩 박스로 축 크기 판별
                float sizeX = bom.MaxX - bom.MinX;
                float sizeY = bom.MaxY - bom.MinY;
                float sizeZ = bom.MaxZ - bom.MinZ;

                // PAD/PLATE 판별: SPREF 값에 PAD 또는 PLATE 포함 여부
                bool isPadOrPlate = IsPadOrPlateFromSpref(bom.Index);

                string longestAxis;
                if (sizeX >= sizeY && sizeX >= sizeZ)
                    longestAxis = "X";
                else if (sizeY >= sizeX && sizeY >= sizeZ)
                    longestAxis = "Y";
                else
                    longestAxis = "Z";

                string viewDirection;
                if (isPadOrPlate)
                {
                    // PAD/PLATE: 최단축 방향으로 카메라 설정 (평판을 정면에서 봄)
                    string shortestAxis;
                    if (sizeX <= sizeY && sizeX <= sizeZ)
                        shortestAxis = "X";
                    else if (sizeY <= sizeX && sizeY <= sizeZ)
                        shortestAxis = "Y";
                    else
                        shortestAxis = "Z";

                    switch (shortestAxis)
                    {
                        case "X":
                            viewDirection = "X";
                            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                            break;
                        case "Y":
                            viewDirection = "Y";
                            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                            break;
                        default: // Z
                            viewDirection = "Z";
                            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
                            break;
                    }
                }
                else
                {
                    // 기존 로직: 최장축이 수평으로 보이는 방향
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
                }

                // 5-1. ORIENTATION UDA 기반 카메라 회전
                ApplyOrientationRotation(bom.Index, viewDirection);

                // 6. 화면 맞춤 + 실선 모드 (T-031: 가공도 시트 선택 시 은선 처리 제거, SMOOTH 실선)
                vizcore3d.View.FitToView();
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
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

                // 7-1. EA 앵글 카메라 방향 보정 (L자가 펼쳐져 보이도록)
                // Osnap 무게중심은 L자 내부코너 쪽으로 편향됨
                // 열린 방향(BB중심 - 무게중심)이 화면 우하로 가도록 카메라 조정
                bool isMinusCamera3d = false;
                bool use1803d = false;  // T-036: DiagLog에서 접근 가능하도록 바깥 스코프로 승격
                bool isEA3d = IsAngleFromSpref(bom.Index);
                if (isEA3d && mfgOsnapWithNames.Count > 0)
                {
                    // 기존뷰 화면에 보이는 축: 수평(H)과 수직(V)
                    // viewDir "X" → H=Y, V=Z / viewDir "Y" → H=X, V=Z / viewDir "Z" → H=X, V=Y
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

                    // 열린 방향 = BB중심 - 무게중심 (무게중심 반대쪽이 열린 코너)
                    float openH = bbCenterH - centroidH;
                    float openV = bbCenterV - centroidV;

                    // 열린 방향이 화면 우하(+screenRight, -screenUp)로 가도록 카메라 조정
                    // PLUS: 수평축 변화 없음, MINUS: 수평축 뒤집힘
                    // 180°: 수평+수직 모두 뒤집힘
                    // → openH 부호로 MINUS 결정, openV 부호로 180° 결정
                    // 열린 방향이 아래로 → openV < 0 → 그대로, openV > 0 → 180° 필요
                    use1803d = (openV > 0);  // T-036: 바깥 스코프 변수에 할당

                    // 열린 방향이 오른쪽으로: 화면 좌표 기준
                    // 180° 적용 전 기준으로 판단 (180°는 수평도 뒤집으므로)
                    // 180° 미적용: openH > 0 = 오른쪽 → PLUS (Y viewDir) / MINUS (X/Z viewDir)
                    // 180° 적용:  openH 방향이 뒤집히므로 반대
                    bool useMinus3d;
                    if (viewDirection == "Y")
                    {
                        // Y_PLUS: screen-right = +H
                        bool needRight = use1803d ? (openH < 0) : (openH > 0);
                        useMinus3d = !needRight; // PLUS면 +H=오른쪽, needRight면 PLUS 유지
                    }
                    else
                    {
                        // X_PLUS/Z_PLUS: screen-right = -H
                        bool needRight = use1803d ? (openH > 0) : (openH < 0);
                        useMinus3d = !needRight;
                    }

                    isMinusCamera3d = useMinus3d;

                    if (useMinus3d)
                    {
                        switch (viewDirection)
                        {
                            case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_MINUS); break;
                            case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_MINUS); break;
                            default:  vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_MINUS); break;
                        }
                        ApplyOrientationRotation(bom.Index, viewDirection);
                        vizcore3d.View.FitToView();
                    }

                    // T-036 (2026-04-23 재조정): 사용자 "ISO 뷰 느낌" 문제는 LvDrawingSheet 공통 FlyToObject3d
                    // 잔존이 원인으로 판명 (아래 분리 처리). L215 180° 스킵 가드 원복 — 원래 180° 회전 복원.
                    // T-036 (2026-04-24 4차): Z 케이스에서 학습한 "회전 직후 FitToView 호출 절대 금지" 교훈을
                    //   R180 케이스에도 적용. 사용자 로그에서 Y 최장축 부재가 longestAxis=Y, R180Applied=True인데도
                    //   세로로 출력되는 현상 확정 → 이 FitToView가 ScreenAxisRotation 회전을 리셋하는 동일 메커니즘
                    if (use1803d)
                    {
                        vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                        // FitToView 제거 — 회전 리셋 방지
                    }
                }

                // 7-2. 은선 Osnap 필터링 (카메라 방향 결정 후 적용)
                mfgOsnapWithNames = FilterHiddenLineOsnap(mfgOsnapWithNames, viewDirection,
                    bom.MinX, bom.MaxX, bom.MinY, bom.MaxY, bom.MinZ, bom.MaxZ, isMinusCamera3d);

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
                mfgStyle.AlignDistanceTextMargine = 3;
                vizcore3d.Review.Measure.SetStyle(mfgStyle);

                float mfgGlobalMinX = bom.MinX, mfgGlobalMinY = bom.MinY, mfgGlobalMinZ = bom.MinZ;
                float mfgGlobalMaxX = bom.MaxX, mfgGlobalMaxY = bom.MaxY, mfgGlobalMaxZ = bom.MaxZ;
                float mfgCenterX = (mfgGlobalMinX + mfgGlobalMaxX) / 2f;
                float mfgCenterY = (mfgGlobalMinY + mfgGlobalMaxY) / 2f;
                float mfgCenterZ = (mfgGlobalMinZ + mfgGlobalMaxZ) / 2f;

                // 축별 치수선 방향 결정 (T-005: 중앙에서 가장 먼 Osnap 쪽이 외곽)
                var mfgAxisPosOff = new Dictionary<string, bool>();
                foreach (var grp in mfgDimensions.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                {
                    string offAxis = GetRemainingAxis(viewDirection, grp.Key);
                    float center = offAxis == "X" ? mfgCenterX : offAxis == "Y" ? mfgCenterY : mfgCenterZ;
                    var values = grp.SelectMany(d => new[]
                    {
                        GetAxisValue(d.StartPoint, offAxis),
                        GetAxisValue(d.EndPoint, offAxis)
                    });
                    mfgAxisPosOff[grp.Key] = ComputePositiveOffsetByOsnapExtreme(values, center);
                }

                var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                // 모델 가시 축 최소 크기 → 작은 모델이면 보조선 오프셋 50% 축소
                float visExt1_3d = 0f, visExt2_3d = 0f;
                switch (viewDirection)
                {
                    case "X": visExt1_3d = bom.MaxY - bom.MinY; visExt2_3d = bom.MaxZ - bom.MinZ; break;
                    case "Y": visExt1_3d = bom.MaxX - bom.MinX; visExt2_3d = bom.MaxZ - bom.MinZ; break;
                    default:  visExt1_3d = bom.MaxX - bom.MinX; visExt2_3d = bom.MaxY - bom.MinY; break;
                }
                float minVisExt_3d = Math.Min(visExt1_3d, visExt2_3d);
                float offFactor_3d = (minVisExt_3d < 100f) ? 0.5f : 1.0f;

                float mfgChainOff1 = 100.0f * offFactor_3d;  // 1단 체인치수 보조선
                float mfgChainOff2 = 200.0f * offFactor_3d;  // 2단 체인치수 보조선

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
                float mfgTotalOff = (maxTotalDist > 1000.0f ? 300.0f : 250.0f) * offFactor_3d;

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
                //     ※ 이 회전 직후 FitToView 호출 절대 금지 — ScreenAxisRotation 회전을 리셋해 Z가 다시 세로로 복구됨
                if (longestAxis == "Z")
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                }

                // T-036 (2026-04-24 4차): 스냅샷 저장 여부만 여기서 결정.
                //   실제 GetCameraData() 호출은 EndUpdate 이후로 미룸 (아래 finally 다음 블록).
                //   이유: BeginUpdate 스코프 내에서는 ScreenAxisRotation 회전이 commit 전 상태일 수 있음 →
                //   "첫 1~2번 클릭은 세로, 이후 클릭은 가로"라는 click-order 의존 버그의 원인.
                //   사용자 4차 테스트 로그(2026-04-24 01:28)에서 클릭 순서 8/9/10/11 → 8,9 세로, 10,11 가로 패턴 확인.
                shouldSnapshotMfgCamera = (longestAxis == "Z" || use1803d || isMinusCamera3d);

                // T-036 (2026-04-24 4차 3단계): SDK 검증 결과 CameraData는 ScreenAxisRotation을 별개로 관리.
                //   회전 플래그를 추적해 복원 시 재적용. Z90/R180 어느 쪽이 적용됐는지 정확히 기록.
                _mfgDrawingZ90Applied = (longestAxis == "Z");
                _mfgDrawingR180Applied = use1803d;

                // T-036 (2026-04-23 강화): 회전 단계별 상세 진단 로그
                //   ISO 뷰 느낌·세로 배치 원인 특정용 — 사용자 재현 시 이 라인 공유 요청
                DiagLog($"T-036 MfgDrawing bom={bom.Index} name=\"{bom.Name}\" " +
                    $"sizeXYZ=({sizeX:F0},{sizeY:F0},{sizeZ:F0}) " +
                    $"longestAxis={longestAxis} isPadOrPlate={isPadOrPlate} " +
                    $"viewDir={viewDirection} " +
                    $"use180={use1803d} useMinus={isMinusCamera3d} " +
                    $"Z90Applied={(longestAxis == "Z")} " +
                    $"R180Applied={(use1803d && longestAxis != "Z")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // T-036: BeginUpdate 짝 (예외 시에도 해제 보장)
                vizcore3d.EndUpdate();
            }

            // T-036 (2026-04-24 4차): EndUpdate 이후에 스냅샷 캡처 — 실제 commit된 카메라 상태 반영.
            //   Application.DoEvents()로 SDK 렌더링 파이프라인이 회전을 완전히 적용할 시간 확보.
            if (shouldSnapshotMfgCamera)
            {
                System.Windows.Forms.Application.DoEvents();
                _mfgDrawingCameraSnapshot = vizcore3d.View.GetCameraData();
            }
            else
            {
                _mfgDrawingCameraSnapshot = null;
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
            // P3 — 엑셀 템플릿 분기 (UseExcelTemplate은 Form1.DrawingSheets.cs:1289 정의)
            if (UseExcelTemplate)
            {
                GenerateMfgDrawing2DAll_WithExcelTemplate(mfgSheets);
                return;
            }

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
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);  // A4 가로

                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                // ── 3. 외곽 테두리 생성 (간단한 1x1 그리드로 깔끔한 A4 테두리) ──
                vizcore3d.Drawing2D.GridStructure.AddGridStructure(1, 1, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);
                // 새 SDK(VIZCore3D+.NET) — 옛 CrateTemplateBorder() 무인자가 CreateTemplateBorder()(스펠링 정정)로 이름 변경됨.
                // xml line 31246: CreateTemplateBorder() → returns TemplateBorderInfo (옛 무인자 호출과 동일 동작).
                // (옛 이름 CrateTemplateBorder는 새 시그니처 CrateTemplateBorder(TemplateBorderInfo)로 재정의 — void 반환, 우리 의도와 다름)
                VIZCore3D.NET.Data.TemplateBorderInfo bInfo = vizcore3d.Drawing2D.Template.CreateTemplateBorder();

                // ── 4. 모델 배치용 그리드 재생성 (8x6) ──
                const int gridRows = 8;
                const int gridCols = 6;   // 라벨(1,3,5) + 모델(2,4,6)
                const int usableRowStart = 2;  // 2행부터
                const int usableRowEnd = 7;    // 7행까지
                const int rowsPerCol = usableRowEnd - usableRowStart + 1; // 6

                vizcore3d.Drawing2D.GridStructure.AddGridStructure(gridRows, gridCols, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);

                // 도면정보 — A4 우측 하단 모서리에 Anchor 절대좌표 방식으로 배치
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
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(5f);

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

                    // 라벨 배치 (모델 Name) — 1행 텍스트, 가로 크기 50% 축소
                    try
                    {
                        BOMData labelBom = bomList.FirstOrDefault(b => b.Index == mfgSheets[i].MemberIndices[0]);
                        if (labelBom != null && !string.IsNullOrEmpty(labelBom.Name))
                        {
                            VIZCore3D.NET.Data.TemplateTableData labelTable = new VIZCore3D.NET.Data.TemplateTableData(1, 1);
                            labelTable.SetText(0, 0, labelBom.Name);
                            labelTable.IsTextWrapped = false;
                            labelTable.ColumnWidths = new Dictionary<int, int>() { { 0, 25 } };

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
        /// P3 — 가공도 엑셀 템플릿 기반 도면 생성 (PoC).
        /// 사용자템플릿_엑셀_가공도.xlsx 활용 — {View_N} 슬롯 N개에 가공도 시트의 단일 부재를 1:1 배치.
        ///
        /// 흐름:
        ///   1) 캔버스 초기화 (옛 코드와 동일 A4 가로 297×210)
        ///   2) data Dictionary — 도면정보 3개만 (가공도는 BOM 테이블 X)
        ///   3) ImportExcelWithData(xlsxPath, data) — 가공도 엑셀 자동 그리기
        ///   4) GetViewAreasFromExcel — {View_N} 영역 N개
        ///   5) 각 가공도 시트 → View_i 영역에 단일 부재 투영:
        ///      - 부재 격리 (Show toggle)
        ///      - 카메라: PoC 단순화 ISO_PLUS 고정 (사용자 검증 후 PAD/PLATE/UDA 동적 매핑 정밀화 가능)
        ///      - Create2DViewObjectWithModelHiddenLineAtCanvasOrigin + fit + MoveObjectTo
        ///   6) 가시성 복원
        ///
        /// 사용자 결정: P2 일반 시트 검증 통과 가정 → P3 가공도 PoC 진입.
        /// 결과 나쁘면 UseExcelTemplate=false로 즉시 롤백 (가공도 = 옛 8×3 그리드 유지).
        /// </summary>
        private void GenerateMfgDrawing2DAll_WithExcelTemplate(List<DrawingSheetData> mfgSheets)
        {
            if (mfgSheets == null || mfgSheets.Count == 0) return;

            try
            {
                vizcore3d.View.EnableAnimation = false;

                // ── 1. 캔버스 초기화 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                Clear2DView();
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);  // A4 가로 (가공도 엑셀 기준)
                vizcore3d.Drawing2D.View.SetSelectCanvas(1);

                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(10f);

                // ── 2. 엑셀 경로 ──
                string solutionPath = GetSolutionPath();
                string xlsxPath = System.IO.Path.Combine(solutionPath, "사용자템플릿_엑셀_가공도.xlsx");
                if (!System.IO.File.Exists(xlsxPath))
                {
                    DiagLog($"P3 가공도 엑셀 파일 없음: {xlsxPath}");
                    throw new Exception($"가공도 엑셀 파일 없음: {xlsxPath}");
                }

                // ── 3. data Dictionary — 도면정보 3개 (가공도는 BOM 테이블 X, 추가 슬롯은 엑셀 구조 확인 후) ──
                Dictionary<int, string> data = new Dictionary<int, string>();
                data[1] = "CEDAR FLNG";       // 프로젝트명 (TODO: tableInfo)
                data[2] = "SN2688";           // 선박번호
                data[3] = "가공도";

                DiagLog($"P3 가공도 data 구성: kind='{data[3]}' (Input {data.Count}개)");

                // ── 4. ImportExcelWithData ──
                vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data);
                vizcore3d.Drawing2D.View.SetSelectCanvas(1);
                DiagLog($"P3 ImportExcelWithData OK — {System.IO.Path.GetFileName(xlsxPath)}");

                // ── 5. GetViewAreasFromExcel ──
                var viewAreas = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
                if (viewAreas == null || viewAreas.Count == 0)
                {
                    DiagLog("P3 GetViewAreasFromExcel 비어있음 — 가공도 엑셀에 {View_N} 태그 없음");
                    return;
                }
                DiagLog($"P3 가공도 GetViewAreasFromExcel: {viewAreas.Count}개 영역 (mfgSheets={mfgSheets.Count}개)");

                // ── 6. 부재 격리용 — 전체 BOM BODY 인덱스 ──
                List<int> allBomIndices = new List<int>();
                foreach (BOMData b in bomList) allBomIndices.Add(b.Index);

                // ── 7. 가공도 시트 → View 영역 1:1 배치 ──
                const float margin = 5f;
                int viewsRendered = 0;
                int slotCount = Math.Min(mfgSheets.Count, viewAreas.Count);

                for (int i = 0; i < slotCount; i++)
                {
                    var sheet = mfgSheets[i];
                    if (sheet.MemberIndices.Count == 0) continue;
                    int bomIdx = sheet.MemberIndices[0];
                    var p = viewAreas[i];

                    try
                    {
                        // 부재 격리 — 단일 부재만 visible
                        if (allBomIndices.Count > 0)
                        {
                            vizcore3d.BeginUpdate();
                            try
                            {
                                vizcore3d.Object3D.Show(allBomIndices, false);
                                vizcore3d.Object3D.Show(new List<int> { bomIdx }, true);
                            }
                            finally { vizcore3d.EndUpdate(); }
                            Application.DoEvents();
                        }

                        // 카메라 — PoC 단순화: ISO_PLUS 고정 (검증 후 정밀화 가능)
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);

                        int objId = vizcore3d.Drawing2D.Object2D
                            .Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                        if (objId < 0)
                        {
                            DiagLog($"P3 View_{p.Index} (bom={bomIdx}) Object2D 생성 실패 objId={objId}");
                            continue;
                        }

                        // fit
                        float fitW = p.Width - 2f * margin;
                        float fitH = p.Height - 2f * margin;
                        float objW = 0f, objH = 0f;
                        vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);
                        float objScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                        if (objW > 0f && objH > 0f && fitW > 0f && fitH > 0f)
                        {
                            float fitScale = Math.Min(fitW / objW, fitH / objH);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objId, objScale * fitScale);
                        }

                        // 영역 중심으로 이동 (PoC 패턴 Y +15)
                        float cx = p.X + p.Width / 2f;
                        float cy = p.Y + p.Height / 2f;
                        vizcore3d.Drawing2D.Object2D.MoveObjectTo(objId, cx, cy + 15f);

                        viewsRendered++;
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"P3 가공도 시트 #{sheet.SheetNumber} View_{p.Index} (bom={bomIdx}) ERROR: {ex.Message}");
                    }
                }

                // ── 8. 가시성 복원 — 전체 BOM 다시 표시 ──
                if (allBomIndices.Count > 0)
                {
                    try
                    {
                        vizcore3d.BeginUpdate();
                        try { vizcore3d.Object3D.Show(allBomIndices, true); }
                        finally { vizcore3d.EndUpdate(); }
                        Application.DoEvents();
                    }
                    catch (Exception ex) { DiagLog($"P3 가시성 복원 ERROR: {ex.Message}"); }
                }

                DiagLog($"P3 가공도 완료 — views={viewsRendered}/{slotCount}, mfgSheets={mfgSheets.Count}, viewAreas={viewAreas.Count}");

                if (mfgSheets.Count > viewAreas.Count)
                    DiagLog($"P3 경고 — 가공도 시트({mfgSheets.Count}) > View 슬롯({viewAreas.Count}). 초과분은 PDF에 포함 안 됨.");
            }
            catch (Exception ex)
            {
                DiagLog($"P3 GenerateMfgDrawing2DAll_WithExcelTemplate ERROR: {ex.Message}\n{ex.StackTrace}");
                throw;
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

            // 3. 축 크기 판별 → 카메라 방향 결정
            float sizeX = bom.MaxX - bom.MinX;
            float sizeY = bom.MaxY - bom.MinY;
            float sizeZ = bom.MaxZ - bom.MinZ;

            // PAD/PLATE 판별: SPREF 값에 PAD 또는 PLATE 포함 여부
            bool isPadOrPlate = IsPadOrPlateFromSpref(bom.Index);

            string longestAxis;
            if (sizeX >= sizeY && sizeX >= sizeZ)
                longestAxis = "X";
            else if (sizeY >= sizeX && sizeY >= sizeZ)
                longestAxis = "Y";
            else
                longestAxis = "Z";

            string viewDirection;
            if (isPadOrPlate)
            {
                // PAD/PLATE: 최단축 방향으로 카메라 설정 (평판을 정면에서 봄)
                string shortestAxis;
                if (sizeX <= sizeY && sizeX <= sizeZ)
                    shortestAxis = "X";
                else if (sizeY <= sizeX && sizeY <= sizeZ)
                    shortestAxis = "Y";
                else
                    shortestAxis = "Z";

                switch (shortestAxis)
                {
                    case "X":
                        viewDirection = "X";
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                        break;
                    case "Y":
                        viewDirection = "Y";
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                        break;
                    default: // Z
                        viewDirection = "Z";
                        vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
                        break;
                }
            }
            else
            {
                // 기존 로직: 최장축이 수평으로 보이는 방향
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
            }

            // 3-1. ORIENTATION UDA 기반 카메라 회전
            var (orientAxis_saved, orientAngle_saved) = ParseOrientation(bom.Index);
            ApplyOrientationRotation(bom.Index, viewDirection);

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

            // EA 앵글 판별 및 기존뷰 카메라 방향 보정 (L자가 펼쳐져 보이도록)
            // Osnap 무게중심은 L자 내부코너 쪽으로 편향됨
            // 열린 방향(BB중심 - 무게중심)이 화면 우하로 가도록 카메라 조정
            bool isEA = IsAngleFromSpref(bom.Index);
            bool isAboveWider = false;
            bool isLShape = false;
            bool isMinusCameraSelected = false;
            bool isEAUse180 = false;

            if (isEA && mfgOsnapWithNames.Count > 0)
            {
                // 기존뷰 화면에 보이는 축으로 무게중심/BB중심 계산
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

                // 열린 방향 = BB중심 - 무게중심
                float openH = bbCenterH - centroidH;
                float openV = bbCenterV - centroidV;

                // 열린 방향이 화면 우하로 가도록 카메라 조정
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
                    vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
                }

                if (use180)
                {
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                    vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
                }

                isEAUse180 = use180;
                isAboveWider = false;
                isLShape = true;
            }

            // 5-1. 은선 Osnap 필터링 (카메라 방향 결정 후 적용)
            mfgOsnapWithNames = FilterHiddenLineOsnap(mfgOsnapWithNames, viewDirection,
                bom.MinX, bom.MaxX, bom.MinY, bom.MaxY, bom.MinZ, bom.MaxZ, isMinusCameraSelected);

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
                        // 길이방향: 신규뷰 반대쪽으로
                        // 아래 넓음(신규뷰 아래) → 기존뷰 길이축 치수를 위(positive)로
                        // 위쪽 넓음(신규뷰 위)   → 기존뷰 길이축 치수를 아래(negative)로
                        if (mfgAxisPosOff.ContainsKey(longestAxis))
                            mfgAxisPosOff[longestAxis] = !isAboveWider;

                        // 비길이축(측면): 신규뷰와 겹치지 않도록 방향 강제
                        // 신규뷰 아래(isLShape) → 비길이축 치수를 위(positive)로
                        // 신규뷰 위(!isLShape)  → 비길이축 치수를 아래(negative)로
                        foreach (string ax in new List<string>(mfgAxisPosOff.Keys))
                        {
                            if (ax != longestAxis)
                                mfgAxisPosOff[ax] = isLShape;  // isLShape=true → positive(위)
                        }
                    }

                    var mfgExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                    // 모델의 가시 축 최소 크기 계산 → 작은 모델이면 보조선 오프셋 50% 축소
                    float visExt1 = 0f, visExt2 = 0f;
                    switch (viewDirection)
                    {
                        case "X": visExt1 = bom.MaxY - bom.MinY; visExt2 = bom.MaxZ - bom.MinZ; break;
                        case "Y": visExt1 = bom.MaxX - bom.MinX; visExt2 = bom.MaxZ - bom.MinZ; break;
                        default:  visExt1 = bom.MaxX - bom.MinX; visExt2 = bom.MaxY - bom.MinY; break;
                    }
                    float minVisExtent = Math.Min(visExt1, visExt2);
                    float offFactor = (minVisExtent < 100f) ? 0.5f : 1.0f;

                    float mfgChainOff1 = 100.0f * offFactor;  // 1단 체인치수 보조선
                    float mfgChainOff2 = 200.0f * offFactor;  // 2단 체인치수 보조선
                    mfgTotalOff *= offFactor;

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
                        // Max(전체) 치수는 항상 표시
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
                    if (mfgAxisPosOff_m.ContainsKey(longestAxis))
                        mfgAxisPosOff_m[longestAxis] = !isAboveWider;
                    foreach (string ax in new List<string>(mfgAxisPosOff_m.Keys))
                    {
                        if (ax != longestAxis)
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

                    // 가로 최소 크기 체크: 20mm 미만이면 20mm로 스케일 조정
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

            // 12. 3D→2D 변환: ShapeDrawing(보조선) → 2D (0.5 굵기 + 가는 실선)
            // T-046: 가공도 보조선을 DASHED_DOUBLEDOTTED → SOLID 로 통일 (전 경로 일관성)
            if (shapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);  // T-040 v6: 0.5→0.1 (극가는 보조선 통일)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
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

                // 2D 노트 라벨을 원형 넘버링으로 변경
                foreach (int idx in noteIds)
                {
                    try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(idx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                    catch { }
                }
            }

            // Measure(치수선) → 2D (보조선과 동일 0.5 굵기)
            List<int> measureIds = new List<int>();
            List<VIZCore3D.NET.Data.MeasureItem> measures = vizcore3d.Review.Measure.Items;
            foreach (var measure in measures)
            {
                if (measure.Visible)
                    measureIds.Add(measure.ID);
            }
            if (measureIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.5f);

                // T-040 v11 (2026-05-13): v6 시점 직각 시프트로 복귀 — 가공도에도 적용
                // 헬퍼는 SDK measureItem 직접 순회라 chainDimensionList 무관
                ApplyParallelTextShift(viewDirection,
                    vizcore3d.Drawing2D.Object2D.GetObjectScale(objId),
                    measures);

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

                    // 기존뷰의 모든 스크린 회전 복원 후 신규뷰 카메라 방향 설정
                    // ScreenAxisRotation은 MoveCamera로 리셋되지 않으므로 역순으로 모두 해제
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;

                    // 1) Z 최장축 90° 회전 복원
                    if (longestAxis == "Z")
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, -90);

                    // 2) EA use180 회전 복원
                    if (isEAUse180)
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);

                    // 3) ORIENTATION 회전 복원
                    if (orientAngle_saved != 0f)
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, -orientAngle_saved);

                    // 신규뷰 카메라 방향: 항상 위에서 아래로 (Z_MINUS)
                    // Z 최장축일 경우 X_MINUS 사용
                    VIZCore3D.NET.Data.CameraDirection newCamDir;
                    string newViewDir;
                    bool needZRotation = false;
                    // 기존뷰 수평 뒤집힘 여부: useMinus는 카메라 방향으로 수평 뒤집기,
                    // use180은 스크린 회전으로 수평+수직 뒤집기 → XOR로 순수 수평 뒤집힘 판정
                    bool flipNewView = isMinusCameraSelected != isEAUse180;
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
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    }

                    // 기존뷰와 수평 방향 정렬: 기존뷰가 수평 뒤집힌 경우 신규뷰도 180° 회전
                    if (flipNewView)
                    {
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                        vizcore3d.View.FlyToObject3d(targetIndices, 1.25f);
                    }

                    // 신규뷰 체인치수 계산 및 그리기
                    // Hole/SlotHole 중심 Osnap 추가 (체인치수에 포함)
                    var eaOsnapWithHoles = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>(mfgOsnapWithNames);
                    if (bom.Holes != null)
                        foreach (var hole in bom.Holes)
                            eaOsnapWithHoles.Add((new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, hole.CenterY, hole.CenterZ), bom.Name));
                    if (bom.SlotHoles != null)
                        foreach (var slot in bom.SlotHoles)
                            eaOsnapWithHoles.Add((new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, slot.CenterY, slot.CenterZ), bom.Name));

                    if (eaOsnapWithHoles.Count > 0)
                    {
                        float tol = 0.5f;
                        var newMerged = MergeCoordinates(eaOsnapWithHoles, tol);

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
                            eaStyle.AlignDistanceTextMargine = 3;
                            vizcore3d.Review.Measure.SetStyle(eaStyle);

                            // 신규뷰 체인치수 방향: 길이축은 기존뷰와 반대
                            var eaAxisPosOff = new Dictionary<string, bool>();
                            eaAxisPosOff[longestAxis] = !isLShape;  // 기존뷰 반대
                            // 비길이축: 자동 계산
                            float eaCX = (bom.MinX + bom.MaxX) / 2f;
                            float eaCY = (bom.MinY + bom.MaxY) / 2f;
                            float eaCZ = (bom.MinZ + bom.MaxZ) / 2f;
                            // T-005: 중앙에서 가장 먼 Osnap 쪽이 외곽 (비길이축 자동)
                            foreach (var grp in newDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis))
                            {
                                if (eaAxisPosOff.ContainsKey(grp.Key)) continue;
                                string offAx = GetRemainingAxis(newViewDir, grp.Key);
                                float cv = offAx == "X" ? eaCX : offAx == "Y" ? eaCY : eaCZ;
                                var values = grp.SelectMany(d => new[]
                                {
                                    GetAxisValue(d.StartPoint, offAx),
                                    GetAxisValue(d.EndPoint, offAx)
                                });
                                eaAxisPosOff[grp.Key] = ComputePositiveOffsetByOsnapExtreme(values, cv);
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
                            // EA 신규뷰 가시 축 최소 크기 → 작은 모델이면 보조선 오프셋 50% 축소
                            float eaVisExt1 = 0f, eaVisExt2 = 0f;
                            switch (newViewDir)
                            {
                                case "X": eaVisExt1 = bom.MaxY - bom.MinY; eaVisExt2 = bom.MaxZ - bom.MinZ; break;
                                case "Y": eaVisExt1 = bom.MaxX - bom.MinX; eaVisExt2 = bom.MaxZ - bom.MinZ; break;
                                default:  eaVisExt1 = bom.MaxX - bom.MinX; eaVisExt2 = bom.MaxY - bom.MinY; break;
                            }
                            float eaMinVisExt = Math.Min(eaVisExt1, eaVisExt2);
                            float eaOffFactor = (eaMinVisExt < 100f) ? 0.5f : 1.0f;

                            float eaTotalOff = (eaMaxTotalDist > 1000.0f ? 300.0f : 250.0f) * eaOffFactor;

                            var eaExtLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();
                            float eaChainOff1 = 100.0f * eaOffFactor;
                            float eaChainOff2 = 200.0f * eaOffFactor;

                            foreach (var dim in newDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0))
                            {
                                // EA: 측면(비길이축) 치수는 정면뷰에만 표시 → 신규뷰에서 중복 방지
                                if (dim.Axis != longestAxis) continue;
                                // EA 앵글 ㄱ자: 신규뷰가 위 → 길이축 체인치수는 아래(기존뷰)에만 표시
                                if (!isLShape && dim.Axis == longestAxis) continue;
                                bool posOff = eaAxisPosOff.ContainsKey(dim.Axis) && eaAxisPosOff[dim.Axis];
                                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, eaChainOff1,
                                    eaMinX, eaMinY, eaMinZ, newViewDir, eaExtLines,
                                    eaMaxX, eaMaxY, eaMaxZ, posOff);
                            }
                            foreach (var dim in newDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0))
                            {
                                if (dim.Axis != longestAxis) continue;
                                if (!isLShape && dim.Axis == longestAxis) continue;
                                bool posOff = eaAxisPosOff.ContainsKey(dim.Axis) && eaAxisPosOff[dim.Axis];
                                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis, eaChainOff2,
                                    eaMinX, eaMinY, eaMinZ, newViewDir, eaExtLines,
                                    eaMaxX, eaMaxY, eaMaxZ, posOff);
                            }
                            foreach (var dim in newDims.Where(d => d.IsTotal && d.IsVisible))
                            {
                                if (dim.Axis != longestAxis) continue;
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

                    // EA 신규뷰 홀 풍선 배치
                    if (bom.Holes != null && bom.Holes.Count > 0)
                    {
                        try
                        {
                            var eaHoleGroups = bom.Holes.GroupBy(h => Math.Round(h.Diameter, 1));
                            float hBalloonOff = Math.Max(Math.Max(sizeX, Math.Max(sizeY, sizeZ)) * 0.3f, 50f);
                            int eaHoleBalloonIdx = 0;
                            foreach (var grp in eaHoleGroups)
                            {
                                int hCount = grp.Count();
                                string holeText = hCount > 1 ? $"\u00d8{grp.Key:F1} * {hCount}개" : $"\u00d8{grp.Key:F1}";
                                var hole = grp.First();
                                VIZCore3D.NET.Data.Vertex3D holeCenter = new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, hole.CenterY, hole.CenterZ);
                                float hOff = hBalloonOff + eaHoleBalloonIdx * 30f;
                                VIZCore3D.NET.Data.Vertex3D holeTextPos;
                                switch (newViewDir)
                                {
                                    case "X": holeTextPos = new VIZCore3D.NET.Data.Vertex3D(hole.CenterX, bom.MinY - hBalloonOff, bom.MaxZ + hOff); break;
                                    case "Y": holeTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - hBalloonOff, hole.CenterY, bom.MaxZ + hOff); break;
                                    default:  holeTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MinX - hBalloonOff, bom.MaxY + hOff, hole.CenterZ); break;
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
                                eaHoleBalloonIdx++;
                            }
                        }
                        catch { }
                    }

                    // EA 신규뷰 슬롯홀 풍선 배치
                    if (bom.SlotHoles != null && bom.SlotHoles.Count > 0)
                    {
                        try
                        {
                            var slotGroups = bom.SlotHoles.GroupBy(s =>
                                $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                            float sBalloonOff = Math.Max(Math.Max(sizeX, Math.Max(sizeY, sizeZ)) * 0.3f, 50f);
                            int eaSlotBalloonIdx = 0;
                            foreach (var grp in slotGroups)
                            {
                                var slot = grp.First();
                                int sCount = grp.Count();
                                float slotWidth = slot.Radius * 2f;
                                string slotText = sCount > 1
                                    ? $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0}) * {sCount}개"
                                    : $"R{slot.Radius:F1}/({slotWidth:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})";
                                VIZCore3D.NET.Data.Vertex3D slotCenter = new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, slot.CenterY, slot.CenterZ);
                                float sOff = sBalloonOff + eaSlotBalloonIdx * 30f;
                                VIZCore3D.NET.Data.Vertex3D slotTextPos;
                                switch (newViewDir)
                                {
                                    case "X": slotTextPos = new VIZCore3D.NET.Data.Vertex3D(slot.CenterX, bom.MaxY + sBalloonOff, bom.MaxZ + sOff); break;
                                    case "Y": slotTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MaxX + sBalloonOff, slot.CenterY, bom.MaxZ + sOff); break;
                                    default:  slotTextPos = new VIZCore3D.NET.Data.Vertex3D(bom.MaxX + sBalloonOff, bom.MaxY + sOff, slot.CenterZ); break;
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
                                eaSlotBalloonIdx++;
                            }
                        }
                        catch { }
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

                    // ShapeDrawing(보조선) → 2D (0.5 굵기 + 가는 실선)
                    // T-046: EA 두 번째 뷰 보조선도 가공도 메인과 동일 SOLID
                    if (eaShapeIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);  // T-040 v6: 0.5→0.1 (극가는 보조선 통일)
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineType(VIZCore3D.NET.Data.Object2D_LineTypes.SOLID);
                        vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(eaShapeIds);
                    }

                    // Measure(치수선) → 2D (0.5 굵기)
                    List<int> eaMeasureIds = new List<int>();
                    foreach (var m in vizcore3d.Review.Measure.Items)
                    {
                        if (m.Visible) eaMeasureIds.Add(m.ID);
                    }
                    if (eaMeasureIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.5f);
                        vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(eaMeasureIds.ToArray());
                    }

                    // EA 신규뷰 Note(풍선) → 2D
                    List<int> eaNoteIds = new List<int>();
                    foreach (var note in vizcore3d.Review.Note.Items)
                        eaNoteIds.Add(note.ID);
                    if (eaNoteIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(3.5f);
                        vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(eaNoteIds.ToArray());
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);

                        // 2D 노트 라벨을 원형 넘버링으로 변경
                        foreach (int idx in eaNoteIds)
                        {
                            try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(idx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                            catch { }
                        }
                    }

                    // 두 뷰 크기 확인
                    float fW = 0, fH = 0, tW = 0, tH = 0;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref fW, ref fH);
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(topObjId, ref tW, ref tH);

                    // 배치: 위쪽 넓으면(isAboveWider) → 신규뷰 위, 아래쪽 넓으면 → 신규뷰 아래
                    // flipNewView로 180° 회전시 수직도 뒤집히므로 배치 방향도 반전
                    float moveAmount = (fH / 2f) + (tH / 2f);
                    bool placeAbove = isAboveWider;
                    if (flipNewView) placeAbove = !placeAbove;
                    if (placeAbove)
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
