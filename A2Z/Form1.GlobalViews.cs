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

        /// <summary>
        /// 설치도용 치수 추출. 선택 STRU와 연결 Assembly의 전체 Osnap 범위 및 실제 접합 영역을 표시한다.
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
            sheet.InstallationContextIndices.AddRange(sheet.InstallationConnections
                .Select(c => c.ConnectedAssemblyIndex)
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => index));

            DiagLog($"설치도 연결 영역 준비 완료: assemblies={sheet.InstallationContextIndices.Count} " +
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
            int assemblyOrder = 0;
            foreach (var assemblyGroup in connections
                .GroupBy(connection => new
                {
                    connection.ConnectedAssemblyIndex,
                    connection.ConnectedAssemblyName
                })
                .OrderBy(group => group.Key.ConnectedAssemblyName)
                .ThenBy(group => group.Key.ConnectedAssemblyIndex))
            {
                string assemblyLabel = ToAlphabeticLabel(assemblyOrder++);
                var orderedAreas = assemblyGroup
                    .OrderByDescending(connection => GetContactCenter(connection).Z)
                    .ThenBy(connection => GetContactCenter(connection).Y)
                    .ThenBy(connection => GetContactCenter(connection).X)
                    .ToList();
                for (int i = 0; i < orderedAreas.Count; i++)
                    orderedAreas[i].Label = orderedAreas.Count == 1
                        ? assemblyLabel
                        : assemblyLabel + (i + 1);
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

        private VIZCore3D.NET.Data.Vector3D GetContactCenter(InstallationConnectionData connection)
        {
            if (connection == null || connection.ContactPoints == null || connection.ContactPoints.Count == 0)
                return new VIZCore3D.NET.Data.Vector3D();
            return new VIZCore3D.NET.Data.Vector3D(
                connection.ContactPoints.Average(point => point.X),
                connection.ContactPoints.Average(point => point.Y),
                connection.ContactPoints.Average(point => point.Z));
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

        private List<VIZCore3D.NET.Data.Vector3D> GetInstallationReferencePoints(IEnumerable<int> bodyIndices)
        {
            var indices = bodyIndices.Distinct().ToList();
            var points = GetLinePointOsnaps(indices);
            if (points.Count > 0) return points;

            try
            {
                var bounds = vizcore3d.Object3D.GetBoundBox(indices, false);
                if (bounds != null)
                {
                    float[] xs = { bounds.MinX, bounds.MaxX };
                    float[] ys = { bounds.MinY, bounds.MaxY };
                    float[] zs = { bounds.MinZ, bounds.MaxZ };
                    foreach (float x in xs)
                        foreach (float y in ys)
                            foreach (float z in zs)
                                points.Add(new VIZCore3D.NET.Data.Vector3D(x, y, z));
                    DiagLog($"설치도 Osnap 없음 — BBox fallback: nodes={indices.Count}");
                }
            }
            catch (Exception ex)
            {
                DiagLog($"설치도 BBox fallback 실패: {ex.Message}");
            }
            return points;
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
        /// 각 Assembly의 주축/보조축 전체 범위와 연결 Part 끝단→접합 영역→끝단 체인을 함께 만든다.
        /// </summary>
        private List<ChainDimensionData> ComputeInstallationDimensions(DrawingSheetData sheet)
        {
            var result = new List<ChainDimensionData>();
            if (sheet == null || sheet.MemberIndices == null || sheet.MemberIndices.Count == 0)
                return result;

            var referenceGroups = new List<(string name, List<int> bodies)>();
            referenceGroups.Add(("선택 STRU", new List<int>(sheet.MemberIndices)));
            foreach (int assemblyIndex in sheet.InstallationContextIndices)
            {
                var assembly = vizcore3d.Object3D.FromIndex(assemblyIndex);
                string name = assembly != null ? assembly.NodeName : $"Assembly_{assemblyIndex}";
                referenceGroups.Add((name, GetDescendantBodyIndices(assemblyIndex)));
            }

            var viewAxes = new Dictionary<string, string[]>
            {
                { "X", new[] { "Z", "Y" } },
                { "Y", new[] { "Z", "X" } },
                { "Z", new[] { "Y", "X" } }
            };

            foreach (var group in referenceGroups)
            {
                var points = GetInstallationReferencePoints(group.bodies);
                if (points.Count < 2) continue;
                foreach (var view in viewAxes)
                {
                    foreach (string axis in view.Value)
                    {
                        var start = points.OrderBy(point => GetVectorAxisValue(point, axis)).First();
                        var end = points.OrderByDescending(point => GetVectorAxisValue(point, axis)).First();
                        AddInstallationDimension(result, start, end, axis, view.Key,
                            $"설치 전체 - {group.name}", true, true, group.bodies);
                    }
                }
            }

            foreach (InstallationConnectionData connection in sheet.InstallationConnections)
            {
                var partBodies = GetBodyIndicesForPart(connection.ConnectedPartIndex);
                var partPoints = GetInstallationReferencePoints(partBodies);
                var contactPoints = connection.ContactPoints ?? new List<VIZCore3D.NET.Data.Vector3D>();
                if (partPoints.Count < 2 || contactPoints.Count == 0) continue;

                foreach (var view in viewAxes)
                {
                    foreach (string axis in view.Value)
                    {
                        var entries = new List<(float value, VIZCore3D.NET.Data.Vector3D point)>();
                        var partMin = partPoints.OrderBy(point => GetVectorAxisValue(point, axis)).First();
                        var partMax = partPoints.OrderByDescending(point => GetVectorAxisValue(point, axis)).First();
                        var contactMin = contactPoints.OrderBy(point => GetVectorAxisValue(point, axis)).First();
                        var contactMax = contactPoints.OrderByDescending(point => GetVectorAxisValue(point, axis)).First();
                        entries.Add((GetVectorAxisValue(partMin, axis), partMin));
                        entries.Add((GetVectorAxisValue(contactMin, axis), contactMin));
                        entries.Add((GetVectorAxisValue(contactMax, axis), contactMax));
                        entries.Add((GetVectorAxisValue(partMax, axis), partMax));

                        entries = entries.OrderBy(entry => entry.value).ToList();
                        var unique = new List<(float value, VIZCore3D.NET.Data.Vector3D point)>();
                        foreach (var entry in entries)
                        {
                            if (unique.Count == 0 || Math.Abs(entry.value - unique[unique.Count - 1].value) > 0.5f)
                                unique.Add(entry);
                        }

                        for (int i = 0; i < unique.Count - 1; i++)
                        {
                            AddInstallationDimension(result, unique[i].point, unique[i + 1].point,
                                axis, view.Key,
                                $"설치 {connection.Label} - {connection.ConnectedPartName}",
                                false, true,
                                new[] { connection.TargetBodyIndex, connection.ConnectedBodyIndex });
                        }
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
            if (distance <= 0.5f) return;
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
