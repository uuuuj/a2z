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

        private const float InstallationContactClusterTolerance = 1.0f;
        private const float InstallationContactSnapTolerance = 3.0f;
        private const float InstallationPlacementTieTolerance = 0.5f;

        // 설치도 위치 치수 축 게이트 (issue #12, 2026-07-23) — 부재가 유의미하게 긴 축만 치수 대상으로 채택해
        //   판 두께·법선(연결부재가 어셈블리 간격 ~1mm 떨어져 뻗는) 축을 배제한다. 실기 튜닝 전제(로그 기반).
        //   채택 조건: 축 extent ≥ 최대 extent × Ratio  AND  축 extent ≥ MinExtent(mm). 단, 주축은 무조건 채택.
        private const float InstallationAxisExtentRatio = 0.25f;
        private const float InstallationAxisMinExtent = 30.0f;
        // 성분 최소 임계(mm) — 축 게이트를 통과해도 남는 미소 성분(끝단 근접 연결·어셈블리 틈 잔여)을 이중 차단.
        private const float InstallationMinComponent = 3.0f;

        /// <summary>
        /// 채택된 긴 축 하나에 대한 위치 치수 성분. 축마다 기준 끝단이 다르므로 성분별로 끝단을 보관한다.
        /// </summary>
        private sealed class InstallationAxisComponent
        {
            public string Axis;
            public VIZCore3D.NET.Data.Vector3D TargetEndPoint;
        }

        /// <summary>
        /// 설치 위치 치수의 최종 기준점.
        /// 접합영역은 이 두 점을 고르는 내부 판정 자료로만 사용한다.
        /// Axis/TargetEndPoint는 주축(최장축) 성분이며, AxisComponents가 채택된 모든 긴 축 성분(축별 끝단 포함)을 담는다.
        /// </summary>
        private sealed class InstallationPlacementAnchor
        {
            public int TargetPartIndex;
            public int TargetBodyIndex;
            public int ConnectedPartIndex;
            public int ConnectedBodyIndex;
            public string Axis;
            public VIZCore3D.NET.Data.Vector3D TargetEndPoint;
            public VIZCore3D.NET.Data.Vector3D ConnectedCornerPoint;
            public bool TargetBoundsFallback;
            public bool ConnectedBoundsFallback;
            public double MainDirectionTotalLength;
            public double SecondDirectionTotalLength;
            public int MergedAreaCount;
            public List<InstallationAxisComponent> AxisComponents = new List<InstallationAxisComponent>();
        }

        /// <summary>
        /// 설치도용 치수 추출.
        /// 기준 STRU측 연결 Body 끝단→외부 연결 Body 접합측 모서리 위치만 표시한다.
        /// </summary>
        private void ExtractInstallationDimensions(DrawingSheetData sheet)
        {
            DiagLog($"ExtractInstallationDimensions ENTER " +
                $"members={sheet?.MemberIndices?.Count ?? 0} connections={sheet?.InstallationConnections?.Count ?? 0} " +
                $"prevChain={chainDimensionList?.Count ?? 0}");

            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.Review.Note.Clear();
            chainDimensionList.Clear();
            lvDimension.Items.Clear();

            chainDimensionList.AddRange(ComputeInstallationDimensions(sheet));

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

            xraySelectedNodeIndices = GetDrawingSheetDisplayIndices(sheet);

            // [T-016 진단 로그] 종료
            DiagLog($"ExtractInstallationDimensions EXIT " +
                $"chain={chainDimensionList.Count} xray={xraySelectedNodeIndices.Count}");
        }

        /// <summary>
        /// Clash PART 결과로 후보를 좁힌 뒤, BODY 조합별 공식 접합선/접합 Mesh로 실제 접합 영역을 구성한다.
        /// 접합선 끝점은 가까운 LINE/POINT Osnap에 스냅한다. CIRCLE은 설치 위치 기준에서 제외한다.
        /// </summary>
        private void PrepareInstallationConnectionData(DrawingSheetData sheet)
        {
            if (sheet == null) return;
            sheet.InstallationConnections.Clear();
            sheet.InstallationContextIndices.Clear();

            if (sheet.MemberIndices == null || sheet.MemberIndices.Count == 0 ||
                fabricationNeighborClashList == null || fabricationNeighborClashList.Count == 0)
                return;

            var sheetBodies = new HashSet<int>(sheet.MemberIndices);
            if (fabricationTargetBodyIndices == null || !fabricationTargetBodyIndices.SetEquals(sheetBodies))
            {
                DiagLog($"설치도 연결 데이터 생략: 검사 대상과 시트 대상 불일치 " +
                        $"tested={fabricationTargetBodyIndices?.Count ?? 0} sheet={sheetBodies.Count}");
                return;
            }

            var handledPartPairs = new HashSet<string>();
            foreach (ClashData clash in fabricationNeighborClashList)
            {
                bool firstIsTarget = fabricationTargetPartIndices.Contains(clash.Index1);
                bool secondIsTarget = fabricationTargetPartIndices.Contains(clash.Index2);
                if (firstIsTarget == secondIsTarget) continue;

                int targetPartIndex = firstIsTarget ? clash.Index1 : clash.Index2;
                int connectedPartIndex = firstIsTarget ? clash.Index2 : clash.Index1;
                string pairKey = targetPartIndex + "|" + connectedPartIndex;
                if (!handledPartPairs.Add(pairKey)) continue;

                var targetBodies = GetBodyIndicesForPart(targetPartIndex);
                var connectedBodies = GetBodyIndicesForPart(connectedPartIndex);
                if (targetBodies.Count == 0 || connectedBodies.Count == 0)
                {
                    DiagLog($"설치도 접합 BODY 탐색 실패: targetPart={targetPartIndex} connectedPart={connectedPartIndex}");
                    continue;
                }

                VIZCore3D.NET.Data.Node connectedPart = null;
                try { connectedPart = vizcore3d.Object3D.FromIndex(connectedPartIndex); }
                catch { }
                VIZCore3D.NET.Data.Node connectedAssembly = FindNearestParentAssembly(connectedPart);
                int assemblyIndex = connectedAssembly != null ? connectedAssembly.Index : connectedPartIndex;
                string assemblyName = connectedAssembly != null ? connectedAssembly.NodeName : null;
                if (string.IsNullOrWhiteSpace(assemblyName))
                    assemblyName = connectedPart != null && !string.IsNullOrWhiteSpace(connectedPart.NodeName)
                        ? connectedPart.NodeName
                        : (firstIsTarget ? clash.Name2 : clash.Name1);
                string partName = connectedPart != null && !string.IsNullOrWhiteSpace(connectedPart.NodeName)
                    ? connectedPart.NodeName
                    : (firstIsTarget ? clash.Name2 : clash.Name1);

                int createdForPair = 0;
                foreach (int targetBody in targetBodies)
                {
                    foreach (int connectedBody in connectedBodies)
                    {
                        BodyBoundsData targetBounds;
                        BodyBoundsData connectedBounds;
                        if (fabricationBodyBoundsCache.TryGetValue(targetBody, out targetBounds) &&
                            fabricationBodyBoundsCache.TryGetValue(connectedBody, out connectedBounds) &&
                            !BoundsOverlapWithinClearance(targetBounds, connectedBounds, 0.5f))
                            continue;

                        List<List<VIZCore3D.NET.Data.Vector3D>> contactAreas =
                            GetBodyContactAreas(targetBody, connectedBody);
                        foreach (var area in contactAreas)
                        {
                            sheet.InstallationConnections.Add(new InstallationConnectionData
                            {
                                TargetPartIndex = targetPartIndex,
                                TargetBodyIndex = targetBody,
                                ConnectedPartIndex = connectedPartIndex,
                                ConnectedBodyIndex = connectedBody,
                                ConnectedAssemblyIndex = assemblyIndex,
                                ConnectedPartName = partName,
                                ConnectedAssemblyName = assemblyName,
                                ContactPoints = area
                            });
                            createdForPair++;
                        }
                    }
                }

                // Clearance/Proximity 결과는 실제 접합선이 없을 수 있다. 이때만 대표점으로 위치를 남긴다.
                if (createdForPair == 0 && clash.HasHotPoint)
                {
                    var hotPoint = new VIZCore3D.NET.Data.Vector3D(clash.XValue, clash.YValue, clash.ZValue);
                    int targetBody = FindClosestBodyToPoint(targetBodies, hotPoint);
                    int connectedBody = FindClosestBodyToPoint(connectedBodies, hotPoint);
                    sheet.InstallationConnections.Add(new InstallationConnectionData
                    {
                        TargetPartIndex = targetPartIndex,
                        TargetBodyIndex = targetBody,
                        ConnectedPartIndex = connectedPartIndex,
                        ConnectedBodyIndex = connectedBody,
                        ConnectedAssemblyIndex = assemblyIndex,
                        ConnectedPartName = partName,
                        ConnectedAssemblyName = assemblyName,
                        IsProximityFallback = true,
                        ContactPoints = new List<VIZCore3D.NET.Data.Vector3D> { hotPoint }
                    });
                    DiagLog($"설치도 접합선 없음 — HotPoint 근접 표시 fallback: " +
                            $"targetPart={targetPartIndex} connectedPart={connectedPartIndex}");
                }
            }

            AssignInstallationConnectionLabels(sheet.InstallationConnections);
            var contextPartIndices = new HashSet<int>(sheet.InstallationConnections
                .Select(c => c.ConnectedPartIndex)
                .Where(index => index >= 0));
            if (fabricationNeighborPartIndices != null)
            {
                foreach (int neighborPartIndex in fabricationNeighborPartIndices)
                {
                    if (neighborPartIndex >= 0 && !fabricationTargetPartIndices.Contains(neighborPartIndex))
                        contextPartIndices.Add(neighborPartIndex);
                }
            }
            sheet.InstallationContextIndices.AddRange(contextPartIndices.OrderBy(index => index));

            DiagLog($"설치도 연결 영역 준비 완료: connectedParts={sheet.InstallationContextIndices.Count} " +
                    $"areas={sheet.InstallationConnections.Count} " +
                    $"fallback={sheet.InstallationConnections.Count(c => c.IsProximityFallback)}");
        }

        private List<int> GetBodyIndicesForPart(int partIndex)
        {
            var result = fabricationBodyToPartIndexCache
                .Where(pair => pair.Value == partIndex)
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (result.Count > 0) return result;

            try
            {
                var descendants = vizcore3d.Object3D.GetChildObject3d(
                    partIndex, VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN, true);
                if (descendants != null)
                    result.AddRange(descendants
                        .Where(node => node.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                        .Select(node => node.Index));
            }
            catch { }
            return result.Distinct().ToList();
        }

        private List<int> GetDescendantBodyIndices(int nodeIndex)
        {
            var result = new List<int>();
            VIZCore3D.NET.Data.Node node = null;
            try { node = vizcore3d.Object3D.FromIndex(nodeIndex); }
            catch { }
            if (node != null && node.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                result.Add(nodeIndex);

            try
            {
                var descendants = vizcore3d.Object3D.GetChildObject3d(
                    nodeIndex, VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN, true);
                if (descendants != null)
                    result.AddRange(descendants
                        .Where(item => item.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                        .Select(item => item.Index));
            }
            catch { }
            return result.Distinct().ToList();
        }

        private List<List<VIZCore3D.NET.Data.Vector3D>> GetBodyContactAreas(
            int targetBodyIndex, int connectedBodyIndex)
        {
            var segments = new List<List<VIZCore3D.NET.Data.Vector3D>>();
            try
            {
                var lines = vizcore3d.GeometryUtility.GetObjectCollisionLine(
                    targetBodyIndex, connectedBodyIndex);
                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (line == null || line.Start == null || line.End == null) continue;
                        segments.Add(new List<VIZCore3D.NET.Data.Vector3D>
                        {
                            new VIZCore3D.NET.Data.Vector3D(line.Start.X, line.Start.Y, line.Start.Z),
                            new VIZCore3D.NET.Data.Vector3D(line.End.X, line.End.Y, line.End.Z)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DiagLog($"설치도 접합선 조회 실패: body={targetBodyIndex}/{connectedBodyIndex} {ex.Message}");
            }

            var areas = MergeConnectedContactSegments(segments);
            if (areas.Count == 0)
            {
                try
                {
                    var mesh = vizcore3d.GeometryUtility.GetJunctionMesh(
                        targetBodyIndex, connectedBodyIndex, false);
                    if (mesh != null && mesh.Count > 0)
                    {
                        areas.Add(mesh.Select(point => new VIZCore3D.NET.Data.Vector3D(
                            point.X, point.Y, point.Z)).ToList());
                    }
                }
                catch (Exception ex)
                {
                    DiagLog($"설치도 접합 Mesh 조회 실패: body={targetBodyIndex}/{connectedBodyIndex} {ex.Message}");
                }
            }

            if (areas.Count > 0)
            {
                var osnaps = GetLinePointOsnaps(new[] { targetBodyIndex, connectedBodyIndex });
                for (int areaIndex = 0; areaIndex < areas.Count; areaIndex++)
                {
                    areas[areaIndex] = areas[areaIndex]
                        .Select(point => SnapToNearestOsnap(point, osnaps, InstallationContactSnapTolerance))
                        .GroupBy(point => $"{point.X:F3}|{point.Y:F3}|{point.Z:F3}")
                        .Select(group => group.First())
                        .ToList();
                }
            }
            return areas;
        }

        private List<List<VIZCore3D.NET.Data.Vector3D>> MergeConnectedContactSegments(
            List<List<VIZCore3D.NET.Data.Vector3D>> segments)
        {
            var areas = new List<List<VIZCore3D.NET.Data.Vector3D>>();
            foreach (var segment in segments)
            {
                var touching = areas.Where(area => area.Any(existing =>
                    segment.Any(point => Distance3D(existing, point) <= InstallationContactClusterTolerance))).ToList();
                if (touching.Count == 0)
                {
                    areas.Add(new List<VIZCore3D.NET.Data.Vector3D>(segment));
                }
                else
                {
                    touching[0].AddRange(segment);
                    for (int i = 1; i < touching.Count; i++)
                    {
                        touching[0].AddRange(touching[i]);
                        areas.Remove(touching[i]);
                    }
                }
            }
            return areas;
        }

        private void AssignInstallationConnectionLabels(List<InstallationConnectionData> connections)
        {
            int partOrder = 0;
            foreach (var partGroup in connections
                .GroupBy(connection => new
                {
                    connection.ConnectedAssemblyIndex,
                    connection.ConnectedAssemblyName,
                    connection.ConnectedPartIndex,
                    connection.ConnectedPartName
                })
                .OrderBy(group => group.Key.ConnectedAssemblyName)
                .ThenBy(group => group.Key.ConnectedPartName)
                .ThenBy(group => group.Key.ConnectedPartIndex))
            {
                string partLabel = ToAlphabeticLabel(partOrder++);
                foreach (InstallationConnectionData connection in partGroup)
                    connection.Label = partLabel;
            }
        }

        private string ToAlphabeticLabel(int index)
        {
            string label = "";
            int value = index + 1;
            while (value > 0)
            {
                value--;
                label = (char)('A' + value % 26) + label;
                value /= 26;
            }
            return label;
        }

        private int FindClosestBodyToPoint(List<int> bodyIndices, VIZCore3D.NET.Data.Vector3D point)
        {
            int closest = bodyIndices.Count > 0 ? bodyIndices[0] : -1;
            double best = double.MaxValue;
            foreach (int bodyIndex in bodyIndices)
            {
                BodyBoundsData bounds;
                if (!fabricationBodyBoundsCache.TryGetValue(bodyIndex, out bounds)) continue;
                double dx = point.X < bounds.MinX ? bounds.MinX - point.X : point.X > bounds.MaxX ? point.X - bounds.MaxX : 0;
                double dy = point.Y < bounds.MinY ? bounds.MinY - point.Y : point.Y > bounds.MaxY ? point.Y - bounds.MaxY : 0;
                double dz = point.Z < bounds.MinZ ? bounds.MinZ - point.Z : point.Z > bounds.MaxZ ? point.Z - bounds.MaxZ : 0;
                double score = dx * dx + dy * dy + dz * dz;
                if (score < best) { best = score; closest = bodyIndex; }
            }
            return closest;
        }

        private List<VIZCore3D.NET.Data.Vector3D> GetLinePointOsnaps(IEnumerable<int> bodyIndices)
        {
            var points = new List<VIZCore3D.NET.Data.Vector3D>();
            foreach (int bodyIndex in bodyIndices.Distinct())
            {
                try
                {
                    var osnaps = vizcore3d.Object3D.GetOsnapPoint(bodyIndex);
                    if (osnaps == null) continue;
                    foreach (var osnap in osnaps)
                    {
                        if (osnap.Kind == VIZCore3D.NET.Data.OsnapKind.LINE)
                        {
                            if (osnap.Start != null) points.Add(new VIZCore3D.NET.Data.Vector3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z));
                            if (osnap.End != null) points.Add(new VIZCore3D.NET.Data.Vector3D(osnap.End.X, osnap.End.Y, osnap.End.Z));
                        }
                        else if (osnap.Kind == VIZCore3D.NET.Data.OsnapKind.POINT && osnap.Center != null)
                        {
                            points.Add(new VIZCore3D.NET.Data.Vector3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z));
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiagLog($"설치도 Osnap 조회 실패: body={bodyIndex} {ex.Message}");
                }
            }
            return points;
        }

        private List<VIZCore3D.NET.Data.Vector3D> GetInstallationBodyPoints(
            int bodyIndex, out bool boundsFallback)
        {
            boundsFallback = false;
            var points = GetLinePointOsnaps(new[] { bodyIndex })
                .GroupBy(point => $"{point.X:F3}|{point.Y:F3}|{point.Z:F3}")
                .Select(group => group.First())
                .ToList();
            if (points.Count > 0) return points;

            boundsFallback = true;
            try
            {
                var bounds = vizcore3d.Object3D.GetBoundBox(new List<int> { bodyIndex }, false);
                if (bounds == null) return points;
                float[] xs = { bounds.MinX, bounds.MaxX };
                float[] ys = { bounds.MinY, bounds.MaxY };
                float[] zs = { bounds.MinZ, bounds.MaxZ };
                foreach (float x in xs)
                    foreach (float y in ys)
                        foreach (float z in zs)
                            points.Add(new VIZCore3D.NET.Data.Vector3D(x, y, z));
            }
            catch (Exception ex)
            {
                DiagLog($"설치 위치 Body BBox fallback 실패: body={bodyIndex} {ex.Message}");
            }
            return points;
        }

        /// <summary>
        /// 실제 접촉 Body의 길이축을 구한다.
        /// 가공도·비직각 접합에서 검증한 LINE 방향 5도 군집/길이 합 최대 기준을 재사용하고,
        /// LINE Osnap이 없을 때만 Body BBox 최장축으로 폴백한다.
        /// </summary>
        private bool TryGetInstallationMainAxis(
            int bodyIndex,
            out MfgAxisVector direction,
            out string worldAxis,
            out double mainDirectionTotalLength,
            out double secondDirectionTotalLength,
            out bool boundsFallback)
        {
            direction = new MfgAxisVector(0.0, 0.0, 0.0);
            worldAxis = "";
            mainDirectionTotalLength = 0.0;
            secondDirectionTotalLength = 0.0;
            boundsFallback = false;

            try
            {
                bool cacheHit;
                MfgAxisDetectionResult detected = GetMfgAxisDetection(bodyIndex, out cacheHit);
                if (detected != null && detected.Success)
                {
                    direction = detected.MainAxis;
                    worldAxis = detected.NearestWorldAxis;
                    mainDirectionTotalLength = detected.MainDirectionTotalLength;
                    secondDirectionTotalLength = detected.SecondDirectionTotalLength;
                    return true;
                }
            }
            catch (Exception ex)
            {
                DiagLog($"설치 위치 길이축 Osnap 판정 실패: body={bodyIndex} {ex.Message}");
            }

            boundsFallback = true;
            try
            {
                var bounds = vizcore3d.Object3D.GetBoundBox(new List<int> { bodyIndex }, false);
                if (bounds == null) return false;
                double sizeX = bounds.MaxX - bounds.MinX;
                double sizeY = bounds.MaxY - bounds.MinY;
                double sizeZ = bounds.MaxZ - bounds.MinZ;
                if (sizeX >= sizeY && sizeX >= sizeZ)
                {
                    direction = new MfgAxisVector(1.0, 0.0, 0.0);
                    worldAxis = "X";
                    mainDirectionTotalLength = sizeX;
                }
                else if (sizeY >= sizeX && sizeY >= sizeZ)
                {
                    direction = new MfgAxisVector(0.0, 1.0, 0.0);
                    worldAxis = "Y";
                    mainDirectionTotalLength = sizeY;
                }
                else
                {
                    direction = new MfgAxisVector(0.0, 0.0, 1.0);
                    worldAxis = "Z";
                    mainDirectionTotalLength = sizeZ;
                }
                return mainDirectionTotalLength > 0.0;
            }
            catch (Exception ex)
            {
                DiagLog($"설치 위치 길이축 BBox 판정 실패: body={bodyIndex} {ex.Message}");
                return false;
            }
        }

        private double ProjectInstallationPoint(
            VIZCore3D.NET.Data.Vector3D point, MfgAxisVector direction)
        {
            return point.X * direction.X + point.Y * direction.Y + point.Z * direction.Z;
        }

        private double GetInstallationPerpendicularDistance(
            VIZCore3D.NET.Data.Vector3D first,
            VIZCore3D.NET.Data.Vector3D second,
            MfgAxisVector direction)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            double dz = first.Z - second.Z;
            double along = dx * direction.X + dy * direction.Y + dz * direction.Z;
            double px = dx - along * direction.X;
            double py = dy - along * direction.Y;
            double pz = dz - along * direction.Z;
            return Math.Sqrt(px * px + py * py + pz * pz);
        }

        /// <summary>
        /// 월드 축(worldAxis)에 직교하는 평면에서 두 점의 거리 — 그 축 성분을 제외한 나머지 두 성분의 거리.
        /// 축별 끝단 후보를 연결 모서리에 가까운 순으로 정렬할 때 사용한다.
        /// </summary>
        private double PerpendicularDistanceInPlane(
            VIZCore3D.NET.Data.Vector3D a,
            VIZCore3D.NET.Data.Vector3D b,
            string worldAxis)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            switch (worldAxis)
            {
                case "X": return Math.Sqrt(dy * dy + dz * dz);
                case "Y": return Math.Sqrt(dx * dx + dz * dz);
                default: return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// 주어진 월드 축을 따라 Target Body의 끝단점을 고른다 (issue #12, 2026-07-23).
        /// BuildInstallationPlacementAnchor의 최장축 끝단 선정 로직을 임의 월드 축으로 일반화한 것.
        /// 축 좌표 MIN/MAX 중 연결 모서리에 가까운 쪽 끝단면을 잡고, 그 끝단면 후보(동률 허용오차 내)
        /// 가운데 연결 모서리와 축 직교 평면 거리가 가장 가까운 점을 반환한다.
        /// </summary>
        private VIZCore3D.NET.Data.Vector3D SelectInstallationTargetEndForAxis(
            List<VIZCore3D.NET.Data.Vector3D> targetPoints,
            VIZCore3D.NET.Data.Vector3D connectedCorner,
            string worldAxis)
        {
            double minProj = targetPoints.Min(p => (double)GetVectorAxisValue(p, worldAxis));
            double maxProj = targetPoints.Max(p => (double)GetVectorAxisValue(p, worldAxis));
            double cornerProj = GetVectorAxisValue(connectedCorner, worldAxis);
            double endProj = Math.Abs(cornerProj - minProj) <= Math.Abs(cornerProj - maxProj)
                ? minProj
                : maxProj;
            double nearestEndPlaneDistance = targetPoints
                .Min(p => Math.Abs(GetVectorAxisValue(p, worldAxis) - endProj));
            return targetPoints
                .Where(p => Math.Abs(GetVectorAxisValue(p, worldAxis) - endProj) <=
                            nearestEndPlaneDistance + InstallationPlacementTieTolerance)
                .OrderBy(p => PerpendicularDistanceInPlane(p, connectedCorner, worldAxis))
                .ThenBy(p => Distance3D(p, connectedCorner))
                .First();
        }

        private InstallationPlacementAnchor BuildInstallationPlacementAnchor(
            IEnumerable<InstallationConnectionData> sourceConnections)
        {
            List<InstallationConnectionData> connections = sourceConnections?
                .Where(connection => connection != null)
                .ToList() ?? new List<InstallationConnectionData>();
            if (connections.Count == 0) return null;

            InstallationConnectionData first = connections[0];
            bool targetBoundsFallback;
            bool connectedBoundsFallback;
            List<VIZCore3D.NET.Data.Vector3D> targetPoints =
                GetInstallationBodyPoints(first.TargetBodyIndex, out targetBoundsFallback);
            List<VIZCore3D.NET.Data.Vector3D> connectedPoints =
                GetInstallationBodyPoints(first.ConnectedBodyIndex, out connectedBoundsFallback);
            if (targetPoints.Count == 0 || connectedPoints.Count == 0) return null;

            MfgAxisVector direction;
            string worldAxis;
            double mainDirectionTotalLength;
            double secondDirectionTotalLength;
            bool axisBoundsFallback;
            if (!TryGetInstallationMainAxis(
                    first.TargetBodyIndex,
                    out direction,
                    out worldAxis,
                    out mainDirectionTotalLength,
                    out secondDirectionTotalLength,
                    out axisBoundsFallback))
                return null;
            targetBoundsFallback = targetBoundsFallback || axisBoundsFallback;

            List<VIZCore3D.NET.Data.Vector3D> contactPoints = connections
                .SelectMany(connection => connection.ContactPoints ?? new List<VIZCore3D.NET.Data.Vector3D>())
                .GroupBy(point => $"{point.X:F3}|{point.Y:F3}|{point.Z:F3}")
                .Select(group => group.First())
                .ToList();

            Func<VIZCore3D.NET.Data.Vector3D, double> contactDistance = point =>
            {
                if (contactPoints.Count > 0)
                    return contactPoints.Min(contact => Distance3D(point, contact));
                return targetPoints.Min(target => Distance3D(point, target));
            };
            double nearestContactDistance = connectedPoints.Min(contactDistance);
            List<VIZCore3D.NET.Data.Vector3D> cornerCandidates = connectedPoints
                .Where(point => contactDistance(point) <= nearestContactDistance + InstallationPlacementTieTolerance)
                .ToList();
            if (cornerCandidates.Count == 0) return null;

            double minProjection = targetPoints.Min(point => ProjectInstallationPoint(point, direction));
            double maxProjection = targetPoints.Max(point => ProjectInstallationPoint(point, direction));
            InstallationPlacementAnchor best = null;
            double bestAxisDistance = double.MaxValue;
            double bestPerpendicularDistance = double.MaxValue;

            foreach (VIZCore3D.NET.Data.Vector3D connectedCorner in cornerCandidates)
            {
                double cornerProjection = ProjectInstallationPoint(connectedCorner, direction);
                double endProjection = Math.Abs(cornerProjection - minProjection) <=
                                       Math.Abs(cornerProjection - maxProjection)
                    ? minProjection
                    : maxProjection;
                double nearestEndPlaneDistance = targetPoints.Min(point =>
                    Math.Abs(ProjectInstallationPoint(point, direction) - endProjection));
                List<VIZCore3D.NET.Data.Vector3D> endCandidates = targetPoints
                    .Where(point => Math.Abs(ProjectInstallationPoint(point, direction) - endProjection) <=
                                    nearestEndPlaneDistance + InstallationPlacementTieTolerance)
                    .ToList();
                VIZCore3D.NET.Data.Vector3D targetEnd = endCandidates
                    .OrderBy(point => GetInstallationPerpendicularDistance(point, connectedCorner, direction))
                    .ThenBy(point => Distance3D(point, connectedCorner))
                    .First();

                double axisDistance = Math.Abs(
                    GetVectorAxisValue(connectedCorner, worldAxis) -
                    GetVectorAxisValue(targetEnd, worldAxis));
                double perpendicularDistance =
                    GetInstallationPerpendicularDistance(targetEnd, connectedCorner, direction);
                if (best != null &&
                    (axisDistance > bestAxisDistance + 0.001 ||
                     (Math.Abs(axisDistance - bestAxisDistance) <= 0.001 &&
                      perpendicularDistance >= bestPerpendicularDistance)))
                    continue;

                bestAxisDistance = axisDistance;
                bestPerpendicularDistance = perpendicularDistance;
                best = new InstallationPlacementAnchor
                {
                    TargetPartIndex = first.TargetPartIndex,
                    TargetBodyIndex = first.TargetBodyIndex,
                    ConnectedPartIndex = first.ConnectedPartIndex,
                    ConnectedBodyIndex = first.ConnectedBodyIndex,
                    Axis = worldAxis,
                    TargetEndPoint = targetEnd,
                    ConnectedCornerPoint = connectedCorner,
                    TargetBoundsFallback = targetBoundsFallback,
                    ConnectedBoundsFallback = connectedBoundsFallback,
                    MainDirectionTotalLength = mainDirectionTotalLength,
                    SecondDirectionTotalLength = secondDirectionTotalLength,
                    MergedAreaCount = connections.Count
                };
            }

            if (best != null)
            {
                // ── 축 게이트 (issue #12, 2026-07-23) — 부재가 유의미하게 긴 축만 성분 치수로 채택 ──
                //   판 두께·법선 축(연결부재가 어셈블리 간격 ~1mm 떨어져 뻗는 방향)을 배제해, 겹쳐 보이는
                //   1mm 틈 치수와 설치 위치와 무관한 수평 성분을 함께 제거한다. 주축은 무조건 채택(회귀 방지).
                double extentX = targetPoints.Max(p => (double)p.X) - targetPoints.Min(p => (double)p.X);
                double extentY = targetPoints.Max(p => (double)p.Y) - targetPoints.Min(p => (double)p.Y);
                double extentZ = targetPoints.Max(p => (double)p.Z) - targetPoints.Min(p => (double)p.Z);
                double maxExtent = Math.Max(extentX, Math.Max(extentY, extentZ));
                string[] axesXYZ = { "X", "Y", "Z" };
                double[] extentsXYZ = { extentX, extentY, extentZ };
                var acceptedAxes = new List<string>();
                for (int ai = 0; ai < 3; ai++)
                {
                    bool isMain = axesXYZ[ai] == best.Axis;
                    bool pass = extentsXYZ[ai] >= maxExtent * InstallationAxisExtentRatio &&
                                extentsXYZ[ai] >= InstallationAxisMinExtent;
                    if (pass || isMain) acceptedAxes.Add(axesXYZ[ai]);
                }
                foreach (string axis in acceptedAxes)
                {
                    // 주축은 이미 확정된 끝단(실기 검증된 최장축 치수) 그대로, 나머지 축은 그 축 기준으로 재선정.
                    VIZCore3D.NET.Data.Vector3D axisEnd = axis == best.Axis
                        ? best.TargetEndPoint
                        : SelectInstallationTargetEndForAxis(targetPoints, best.ConnectedCornerPoint, axis);
                    best.AxisComponents.Add(new InstallationAxisComponent { Axis = axis, TargetEndPoint = axisEnd });
                }
                DiagLog($"[설치치수축] targetPart={best.TargetPartIndex} " +
                        $"extent=(X={extentX:F1},Y={extentY:F1},Z={extentZ:F1}) maxExt={maxExtent:F1} " +
                        $"main={best.Axis} ratio={InstallationAxisExtentRatio} minExt={InstallationAxisMinExtent} " +
                        $"accepted=[{string.Join(",", acceptedAxes)}]");

                DiagLog($"[설치위치] targetPart={best.TargetPartIndex} targetBody={best.TargetBodyIndex} " +
                        $"connectedPart={best.ConnectedPartIndex} connectedBody={best.ConnectedBodyIndex} " +
                        $"axis={best.Axis} mainDir=({direction.X:F3},{direction.Y:F3},{direction.Z:F3}) " +
                        $"mainSum={best.MainDirectionTotalLength:F1} " +
                        $"secondSum={best.SecondDirectionTotalLength:F1} areas={best.MergedAreaCount} " +
                        $"targetEnd=({best.TargetEndPoint.X:F1},{best.TargetEndPoint.Y:F1},{best.TargetEndPoint.Z:F1}) " +
                        $"connectedCorner=({best.ConnectedCornerPoint.X:F1},{best.ConnectedCornerPoint.Y:F1},{best.ConnectedCornerPoint.Z:F1}) " +
                        $"distance={bestAxisDistance:F1} targetBBox={best.TargetBoundsFallback} " +
                        $"connectedBBox={best.ConnectedBoundsFallback}");
            }
            return best;
        }

        private VIZCore3D.NET.Data.Vector3D SnapToNearestOsnap(
            VIZCore3D.NET.Data.Vector3D point,
            List<VIZCore3D.NET.Data.Vector3D> osnaps,
            float tolerance)
        {
            VIZCore3D.NET.Data.Vector3D nearest = null;
            double nearestDistance = double.MaxValue;
            foreach (var osnap in osnaps)
            {
                double distance = Distance3D(point, osnap);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = osnap;
                }
            }
            return nearest != null && nearestDistance <= tolerance
                ? new VIZCore3D.NET.Data.Vector3D(nearest.X, nearest.Y, nearest.Z)
                : point;
        }

        private double Distance3D(VIZCore3D.NET.Data.Vector3D a, VIZCore3D.NET.Data.Vector3D b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 설치도 치수를 UI 상태 변경 없이 계산한다.
        /// 실제 접촉한 STRU측 Body의 가까운 끝단→외부 연결 Body 접합측 모서리
        /// 위치 치수만 만든다.
        /// 접합영역 A1/A2와 연결 Assembly 전체 범위는 치수 끝점으로 사용하지 않는다.
        /// </summary>
        private List<ChainDimensionData> ComputeInstallationDimensions(DrawingSheetData sheet)
        {
            var result = new List<ChainDimensionData>();
            if (sheet == null || sheet.MemberIndices == null || sheet.MemberIndices.Count == 0)
                return result;

            var viewAxes = new Dictionary<string, string[]>
            {
                { "X", new[] { "Z", "Y" } },
                { "Y", new[] { "Z", "X" } },
                { "Z", new[] { "Y", "X" } }
            };

            var connectionGroups = sheet.InstallationConnections
                .Where(connection => connection != null)
                .GroupBy(connection => new
                {
                    connection.TargetPartIndex,
                    connection.TargetBodyIndex,
                    connection.ConnectedPartIndex,
                    connection.ConnectedBodyIndex
                });
            foreach (var connectionGroup in connectionGroups)
            {
                InstallationPlacementAnchor anchor = BuildInstallationPlacementAnchor(connectionGroup);
                if (anchor == null || string.IsNullOrEmpty(anchor.Axis)) continue;
                InstallationConnectionData connection = connectionGroup.First();
                string targetName = $"Part_{anchor.TargetPartIndex}";
                try
                {
                    var targetPart = vizcore3d.Object3D.FromIndex(anchor.TargetPartIndex);
                    if (targetPart != null && !string.IsNullOrWhiteSpace(targetPart.NodeName))
                        targetName = targetPart.NodeName;
                }
                catch { }

                // 설치 위치 치수는 "부재가 유의미하게 긴 축"에만 생성한다 (issue #12, 2026-07-23 사용자 확정).
                //   BuildInstallationPlacementAnchor의 축 게이트가 채택한 각 축 성분은 그 축 기준으로 재선정된
                //   끝단을 가진다 — 주축(예: 세로 30mm)에 더해 판형 부재의 폭 방향 성분이 평면도에 나온다.
                //   판 두께·법선 축(1mm 어셈블리 틈이 있는 연결부재 뻗는 방향)은 게이트에서 배제돼 생성 안 됨.
                //   뷰별 필터(viewAxes)가 각 뷰에서 보이는 축 성분만 표시하고, 미소 성분은 InstallationMinComponent가 거른다.
                foreach (var component in anchor.AxisComponents)
                {
                    // 성분 치수는 그 축으로만 벌어지도록 끝점을 투영한다 (2026-07-23, issue #12 실기 피드백).
                    //   원본 두 점(기준 끝단·연결 모서리)은 여러 축에서 동시에 벌어져 있어(예: X로 147·Z로 30),
                    //   그대로 쓰면 한 축 성분을 그릴 때 끝점이 다른 축으로도 벌어진 채 그려져 보조선이 부재를
                    //   가로지르고 연결부재 쪽까지 뻗어 큰 공백이 생긴다. 기준 끝단의 나머지 두 좌표를 공유하고
                    //   성분 축만 연결 모서리 좌표로 바꿔, 순수 축정렬 치수(짧고 부재에 붙는 보조선)로 만든다.
                    VIZCore3D.NET.Data.Vector3D compStart = component.TargetEndPoint;
                    VIZCore3D.NET.Data.Vector3D corner = anchor.ConnectedCornerPoint;
                    VIZCore3D.NET.Data.Vector3D compEnd;
                    switch (component.Axis)
                    {
                        case "X": compEnd = new VIZCore3D.NET.Data.Vector3D(corner.X, compStart.Y, compStart.Z); break;
                        case "Y": compEnd = new VIZCore3D.NET.Data.Vector3D(compStart.X, corner.Y, compStart.Z); break;
                        default: compEnd = new VIZCore3D.NET.Data.Vector3D(compStart.X, compStart.Y, corner.Z); break;
                    }
                    DiagLog($"[설치치수] {targetName}→{connection.ConnectedPartName} 축={component.Axis} " +
                            $"성분={Math.Abs(GetVectorAxisValue(corner, component.Axis) - GetVectorAxisValue(compStart, component.Axis)):F1}");
                    foreach (var view in viewAxes.Where(item => item.Value.Contains(component.Axis)))
                    {
                        AddInstallationDimension(result,
                            compStart,
                            compEnd,
                            component.Axis,
                            view.Key,
                            $"설치 {connection.Label} - {targetName} 끝단 → {connection.ConnectedPartName} 모서리",
                            false,
                            true,
                            new[] { anchor.TargetBodyIndex, anchor.ConnectedBodyIndex });
                    }
                }
            }

            var distinct = result
                .GroupBy(dim => $"{dim.ViewDirection}|{dim.Axis}|" +
                                $"{GetVectorAxisValue(dim.StartPoint, dim.Axis):F2}|" +
                                $"{GetVectorAxisValue(dim.EndPoint, dim.Axis):F2}|{dim.ViewName}")
                .Select(group => group.First())
                .ToList();
            for (int i = 0; i < distinct.Count; i++) distinct[i].No = i + 1;
            return distinct;
        }

        private void AddInstallationDimension(
            List<ChainDimensionData> result,
            VIZCore3D.NET.Data.Vector3D start,
            VIZCore3D.NET.Data.Vector3D end,
            string axis,
            string viewDirection,
            string viewName,
            bool isTotal,
            bool isRequired,
            IEnumerable<int> memberIndices)
        {
            float distance = Math.Abs(GetVectorAxisValue(end, axis) - GetVectorAxisValue(start, axis));
            // 미소 성분 차단 (issue #12, 2026-07-23) — 끝단 근접 연결·어셈블리 틈 잔여를 걸러 겹친 선 방지.
            if (distance <= InstallationMinComponent) return;
            result.Add(new ChainDimensionData
            {
                Axis = axis,
                ViewName = viewName,
                ViewDirection = viewDirection,
                Distance = distance,
                StartPoint = start,
                EndPoint = end,
                StartPointStr = $"({start.X:F1}, {start.Y:F1}, {start.Z:F1})",
                EndPointStr = $"({end.X:F1}, {end.Y:F1}, {end.Z:F1})",
                IsTotal = isTotal,
                IsRequired = isRequired,
                MemberIndices = memberIndices.Where(index => index >= 0).Distinct().ToList()
            });
        }

        private float GetVectorAxisValue(VIZCore3D.NET.Data.Vector3D point, string axis)
        {
            switch (axis)
            {
                case "X": return point.X;
                case "Y": return point.Y;
                case "Z": return point.Z;
                default: return 0f;
            }
        }
    }
}
