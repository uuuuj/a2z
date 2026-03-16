using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        #region 글로벌 뷰 버튼 핸들러 (탭 공통) + 설치 치수

        /// <summary>
        /// 글로벌 ISO 버튼 - 현재 상황에 따라 적절한 동작 수행
        /// </summary>
        private void btnGlobalISO_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("ISO");
        }

        /// <summary>
        /// 글로벌 X축 버튼
        /// </summary>
        private void btnGlobalAxisX_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("X");
        }

        /// <summary>
        /// 글로벌 Y축 버튼
        /// </summary>
        private void btnGlobalAxisY_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Y");
        }

        /// <summary>
        /// 글로벌 Z축 버튼
        /// </summary>
        private void btnGlobalAxisZ_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Z");
        }

        /// <summary>
        /// 글로벌 뷰 적용 - 현재 탭과 선택 상태에 따라 적절한 뷰 표시
        /// </summary>
        private void ApplyGlobalView(string viewDirection)
        {
            try
            {
                // 도면정보 탭에서 시트가 선택된 경우 해당 시트 부재 기준으로 표시
                if (tabControlLeft.SelectedTab == tabPageDrawing && lvDrawingSheet.SelectedItems.Count > 0)
                {
                    ApplyDrawingSheetView(viewDirection);
                    return;
                }

                // X-Ray로 선택된 부재가 있는 경우 해당 부재 기준으로 표시
                if (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                {
                    ApplySelectedNodesView(viewDirection);
                    return;
                }

                // 기본: 전체 모델 기준으로 표시
                ApplyFullModelView(viewDirection);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"뷰 전환 중 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택된 부재 기준 뷰 표시 (X-Ray 선택 상태)
        /// </summary>
        private void ApplySelectedNodesView(string viewDirection)
        {
            vizcore3d.BeginUpdate();

            // X-Ray 모드 유지 (해당 부재만 보이도록)
            if (!vizcore3d.View.XRay.Enable)
                vizcore3d.View.XRay.Enable = true;

            vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
            vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
            vizcore3d.View.SilhouetteEdge = true;
            vizcore3d.View.SilhouetteEdgeColor = Color.Green;

            vizcore3d.View.XRay.Clear();
            vizcore3d.View.XRay.Select(xraySelectedNodeIndices, true);

            vizcore3d.EndUpdate();

            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

            // 카메라 방향 설정
            switch (viewDirection)
            {
                case "ISO":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                    break;
                case "X":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                    break;
                case "Y":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                    break;
                case "Z":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
                    break;
            }

            // 선택된 부재에 맞춰 화면 조정 (FlyToObject3d 사용)
            vizcore3d.View.FlyToObject3d(xraySelectedNodeIndices, 1.0f);

            // ISO는 풍선 표시, X/Y/Z는 치수 표시
            if (viewDirection == "ISO")
            {
                CreateIsoBalloonNotes(xraySelectedNodeIndices);
            }
            else
            {
                ShowAllDimensions(viewDirection);
            }
        }

        /// <summary>
        /// 전체 모델 기준 뷰 표시
        /// </summary>
        private void ApplyFullModelView(string viewDirection)
        {
            // X-Ray 모드 해제 (전체 모델 표시)
            if (vizcore3d.View.XRay.Enable)
            {
                vizcore3d.View.XRay.Clear();
                vizcore3d.View.XRay.Enable = false;
            }
            xraySelectedNodeIndices.Clear();

            RestoreAllPartsVisibility();
            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

            // 카메라 방향 설정
            switch (viewDirection)
            {
                case "ISO":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                    break;
                case "X":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                    break;
                case "Y":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                    break;
                case "Z":
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
                    break;
            }

            // 전체 모델에 맞춰 화면 조정 (한 번만 호출)
            vizcore3d.View.FitToView();

            // ISO는 풍선 표시, X/Y/Z는 치수 표시
            if (viewDirection == "ISO")
            {
                // 전체 모델: 모든 bomList 부재 인덱스 사용
                List<int> allIndices = new List<int>();
                if (bomList != null)
                    foreach (var bom in bomList) allIndices.Add(bom.Index);
                CreateIsoBalloonNotes(allIndices);
            }
            else
            {
                ShowAllDimensions(viewDirection);
            }
        }

        #endregion

        /// <summary>
        /// 설치도용 치수 추출 - 부재 바운딩박스 경계 기반 체인치수
        /// 각 부재의 Min/Max를 축별로 정렬하여 설치 위치를 표시
        /// </summary>
        private void ExtractInstallationDimensions(List<int> memberIndices)
        {
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.Review.Note.Clear();
            chainDimensionList.Clear();
            lvDimension.Items.Clear();

            // 포함된 부재의 BOM 데이터 수집
            List<BOMData> members = new List<BOMData>();
            foreach (int idx in memberIndices)
            {
                BOMData bom = bomList.FirstOrDefault(b => b.Index == idx);
                if (bom != null) members.Add(bom);
            }

            if (members.Count == 0) return;

            float tolerance = 1.0f;

            // 축별로 부재 경계값(Min, Max)을 수집하여 체인치수 생성
            string[] axes = { "X", "Y", "Z" };
            foreach (string axis in axes)
            {
                // 경계값과 해당 부재 정보를 함께 수집 (보조선 시작점 = 부재 바운딩박스 모서리)
                var boundaryEntries = new List<(float value, BOMData member)>();
                foreach (var m in members)
                {
                    float minVal = 0, maxVal = 0;
                    switch (axis)
                    {
                        case "X": minVal = m.MinX; maxVal = m.MaxX; break;
                        case "Y": minVal = m.MinY; maxVal = m.MaxY; break;
                        case "Z": minVal = m.MinZ; maxVal = m.MaxZ; break;
                    }
                    boundaryEntries.Add((minVal, m));
                    boundaryEntries.Add((maxVal, m));
                }

                // 오름차순 정렬 후 중복 제거
                boundaryEntries.Sort((a, b) => a.value.CompareTo(b.value));
                var uniqueEntries = new List<(float value, BOMData member)>();
                foreach (var entry in boundaryEntries)
                {
                    if (uniqueEntries.Count == 0 || Math.Abs(entry.value - uniqueEntries[uniqueEntries.Count - 1].value) > tolerance)
                        uniqueEntries.Add(entry);
                }

                if (uniqueEntries.Count < 2) continue;

                // 부재 바운딩박스 모서리 좌표로 치수 포인트 생성
                // 보조선이 부재 표면에서 시작하도록 부재의 실제 위치 사용
                Func<float, BOMData, VIZCore3D.NET.Data.Vector3D> makePoint = (val, m) =>
                {
                    switch (axis)
                    {
                        case "X": return new VIZCore3D.NET.Data.Vector3D(val, m.MinY, m.MinZ);
                        case "Y": return new VIZCore3D.NET.Data.Vector3D(m.MinX, val, m.MinZ);
                        default:  return new VIZCore3D.NET.Data.Vector3D(m.MinX, m.MinY, val);
                    }
                };

                // ===== 설치 위치 체인 치수 (인접 경계 간 순차) =====
                for (int i = 0; i < uniqueEntries.Count - 1; i++)
                {
                    float dist = Math.Abs(uniqueEntries[i].value - uniqueEntries[i + 1].value);
                    if (dist <= tolerance) continue;

                    var startPt = makePoint(uniqueEntries[i].value, uniqueEntries[i].member);
                    var endPt = makePoint(uniqueEntries[i + 1].value, uniqueEntries[i + 1].member);

                    chainDimensionList.Add(new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = dist,
                        StartPoint = startPt,
                        EndPoint = endPt,
                        StartPointStr = $"({startPt.X:F1}, {startPt.Y:F1}, {startPt.Z:F1})",
                        EndPointStr = $"({endPt.X:F1}, {endPt.Y:F1}, {endPt.Z:F1})"
                    });
                }

                // ===== 개별 부재 전체 길이 치수 (설치 참조용) =====
                // 전체 조립 치수 범위 미리 계산 (중복 방지용)
                float totalRangeMin = uniqueEntries[0].value;
                float totalRangeMax = uniqueEntries[uniqueEntries.Count - 1].value;

                foreach (var m in members)
                {
                    float mMin = 0, mMax = 0;
                    switch (axis)
                    {
                        case "X": mMin = m.MinX; mMax = m.MaxX; break;
                        case "Y": mMin = m.MinY; mMax = m.MaxY; break;
                        case "Z": mMin = m.MinZ; mMax = m.MaxZ; break;
                    }
                    float memberLen = Math.Abs(mMax - mMin);
                    if (memberLen <= tolerance) continue;

                    // 전체 조립 치수와 동일 범위면 스킵 (이중 표시 방지)
                    if (uniqueEntries.Count > 2 &&
                        Math.Abs(mMin - totalRangeMin) < tolerance &&
                        Math.Abs(mMax - totalRangeMax) < tolerance)
                        continue;

                    // 이미 동일한 범위의 체인치수가 있으면 스킵 (중복 방지)
                    bool isDuplicate = false;
                    foreach (var existing in chainDimensionList)
                    {
                        if (existing.Axis != axis || existing.IsTotal) continue;
                        float eStart = 0, eEnd = 0;
                        switch (axis)
                        {
                            case "X": eStart = existing.StartPoint.X; eEnd = existing.EndPoint.X; break;
                            case "Y": eStart = existing.StartPoint.Y; eEnd = existing.EndPoint.Y; break;
                            case "Z": eStart = existing.StartPoint.Z; eEnd = existing.EndPoint.Z; break;
                        }
                        float eMin = Math.Min(eStart, eEnd);
                        float eMax = Math.Max(eStart, eEnd);
                        if (Math.Abs(eMin - mMin) < tolerance && Math.Abs(eMax - mMax) < tolerance)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    if (isDuplicate) continue;

                    var memberStart = makePoint(mMin, m);
                    var memberEnd = makePoint(mMax, m);

                    chainDimensionList.Add(new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = memberLen,
                        StartPoint = memberStart,
                        EndPoint = memberEnd,
                        StartPointStr = $"({memberStart.X:F1}, {memberStart.Y:F1}, {memberStart.Z:F1})",
                        EndPointStr = $"({memberEnd.X:F1}, {memberEnd.Y:F1}, {memberEnd.Z:F1})"
                    });
                }

                // ===== 전체 조립 치수 (처음~끝, 순차가 2개 이상일 때) =====
                if (uniqueEntries.Count > 2)
                {
                    var first = uniqueEntries[0];
                    var last = uniqueEntries[uniqueEntries.Count - 1];
                    float totalDist = Math.Abs(first.value - last.value);

                    var totalStart = makePoint(first.value, first.member);
                    var totalEnd = makePoint(last.value, last.member);

                    chainDimensionList.Add(new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = totalDist,
                        StartPoint = totalStart,
                        EndPoint = totalEnd,
                        StartPointStr = $"({totalStart.X:F1}, {totalStart.Y:F1}, {totalStart.Z:F1})",
                        EndPointStr = $"({totalEnd.X:F1}, {totalEnd.Y:F1}, {totalEnd.Z:F1})",
                        IsTotal = true
                    });
                }
            }

            // ListView 갱신 및 치수 번호 설정
            int no = 1;
            foreach (var dim in chainDimensionList)
            {
                dim.No = no;
                ListViewItem lvi = new ListViewItem(no.ToString());
                lvi.SubItems.Add(dim.Axis);
                lvi.SubItems.Add(dim.ViewName);
                lvi.SubItems.Add(((int)Math.Round(dim.Distance)).ToString());
                lvi.SubItems.Add(dim.StartPointStr);
                lvi.SubItems.Add(dim.EndPointStr);
                lvi.Tag = dim;
                lvDimension.Items.Add(lvi);
                no++;
            }

            xraySelectedNodeIndices = new List<int>(memberIndices);
        }
    }
}
