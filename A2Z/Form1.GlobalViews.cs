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
            // T-035: 글로벌 뷰 전환 시 이전 Object3D.Select 선택상태(빨간색) 해제
            vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);

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
            // T-034: DASH_LINE → SMOOTH (부재가 잘 보이도록 실선 모드)
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);

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
            // T-035: 글로벌 뷰 전환 시 이전 Object3D.Select 선택상태(빨간색) 해제
            vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);

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
            // T-034: DASH_LINE → SMOOTH (부재가 잘 보이도록 실선 모드)
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);

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
            // [T-016 진단 로그] 진입
            DiagLog($"ExtractInstallationDimensions ENTER " +
                $"members={memberIndices?.Count ?? 0} prevChain={chainDimensionList?.Count ?? 0}");

            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.Review.Note.Clear();
            chainDimensionList.Clear();
            lvDimension.Items.Clear();

            chainDimensionList.AddRange(ComputeInstallationDimensions(memberIndices));

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

            // [T-016 진단 로그] 종료
            DiagLog($"ExtractInstallationDimensions EXIT " +
                $"chain={chainDimensionList.Count} xray={xraySelectedNodeIndices.Count}");
        }

        /// <summary>
        /// 설치도 치수를 UI·SDK 상태 변경 없이 계산한다. 도면 리스트 표시 전 사전 준비에서 사용한다.
        /// </summary>
        private List<ChainDimensionData> ComputeInstallationDimensions(List<int> memberIndices)
        {
            var result = new List<ChainDimensionData>();
            var members = new List<BOMData>();
            if (memberIndices != null)
            {
                foreach (int index in memberIndices)
                {
                    BOMData bom = bomList.FirstOrDefault(b => b.Index == index);
                    if (bom != null) members.Add(bom);
                }
            }
            if (members.Count == 0) return result;

            const float tolerance = 1.0f;
            string[] axes = { "X", "Y", "Z" };
            foreach (string axis in axes)
            {
                var boundaryEntries = new List<(float value, BOMData member)>();
                foreach (BOMData member in members)
                {
                    float minValue = 0;
                    float maxValue = 0;
                    switch (axis)
                    {
                        case "X": minValue = member.MinX; maxValue = member.MaxX; break;
                        case "Y": minValue = member.MinY; maxValue = member.MaxY; break;
                        case "Z": minValue = member.MinZ; maxValue = member.MaxZ; break;
                    }
                    boundaryEntries.Add((minValue, member));
                    boundaryEntries.Add((maxValue, member));
                }

                boundaryEntries.Sort((a, b) => a.value.CompareTo(b.value));
                var uniqueEntries = new List<(float value, BOMData member)>();
                foreach (var entry in boundaryEntries)
                {
                    if (uniqueEntries.Count == 0 ||
                        Math.Abs(entry.value - uniqueEntries[uniqueEntries.Count - 1].value) > tolerance)
                    {
                        uniqueEntries.Add(entry);
                    }
                }
                if (uniqueEntries.Count < 2) continue;

                Func<float, BOMData, VIZCore3D.NET.Data.Vector3D> makePoint = (value, member) =>
                {
                    switch (axis)
                    {
                        case "X": return new VIZCore3D.NET.Data.Vector3D(value, member.MinY, member.MinZ);
                        case "Y": return new VIZCore3D.NET.Data.Vector3D(member.MinX, value, member.MinZ);
                        default: return new VIZCore3D.NET.Data.Vector3D(member.MinX, member.MinY, value);
                    }
                };

                for (int i = 0; i < uniqueEntries.Count - 1; i++)
                {
                    float distance = Math.Abs(uniqueEntries[i].value - uniqueEntries[i + 1].value);
                    if (distance <= tolerance) continue;
                    var start = makePoint(uniqueEntries[i].value, uniqueEntries[i].member);
                    var end = makePoint(uniqueEntries[i + 1].value, uniqueEntries[i + 1].member);
                    result.Add(new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = distance,
                        StartPoint = start,
                        EndPoint = end,
                        StartPointStr = $"({start.X:F1}, {start.Y:F1}, {start.Z:F1})",
                        EndPointStr = $"({end.X:F1}, {end.Y:F1}, {end.Z:F1})",
                        MemberIndices = new List<int>
                        {
                            uniqueEntries[i].member.Index,
                            uniqueEntries[i + 1].member.Index
                        }
                    });
                }

                if (uniqueEntries.Count > 2)
                {
                    var first = uniqueEntries[0];
                    var last = uniqueEntries[uniqueEntries.Count - 1];
                    float distance = Math.Abs(first.value - last.value);
                    var start = makePoint(first.value, first.member);
                    var end = makePoint(last.value, last.member);
                    result.Add(new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = distance,
                        StartPoint = start,
                        EndPoint = end,
                        StartPointStr = $"({start.X:F1}, {start.Y:F1}, {start.Z:F1})",
                        EndPointStr = $"({end.X:F1}, {end.Y:F1}, {end.Z:F1})",
                        IsTotal = true,
                        MemberIndices = new List<int> { first.member.Index, last.member.Index }
                    });
                }
            }

            for (int i = 0; i < result.Count; i++) result[i].No = i + 1;
            return result;
        }
    }
}
