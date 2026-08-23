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
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_MINUS);
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
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_MINUS);
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

        // 성분 최소 임계(mm) — 끝단에 사실상 붙은(거리≈0) 위치는 그리지 않는다(끝단 근접 연결·어셈블리 틈 잔여).
        private const float InstallationMinComponent = 3.0f;
        // 접합 가로지름 임계 (issue #12, 2026-07-23 재설계) — 접합이 그 축으로 부재를 이 비율 이상 덮으면
        //   "연결부재가 그 축으로 부재를 가로지름(관통)"으로 보고 위치 치수를 생략한다. 미만이면 한 지점에
        //   국소적으로 붙은 것 → 위치 치수 표시. 크기 임계(부재가 긴가) 대신 접합 형태로 판정. 절반이 자연 분기점.
        private const float InstallationContactCrossCoverage = 0.5f;

        /// <summary>
        /// 채택된 긴 축 하나에 대한 위치 치수 성분. 축마다 기준 끝단이 다르므로 성분별로 끝단을 보관한다.
        /// </summary>
        private sealed class InstallationAxisComponent
        {
            public string Axis;
            public VIZCore3D.NET.Data.Vector3D TargetEndPoint;
            // 접합 가장자리(그 축에서 부재 끝단에 가까운 쪽) 좌표 — 위치 치수의 연결측 끝점.
            //   접합 한가운데(centroid)가 아니라 "연결부재가 닿기 시작하는 모서리"까지 재기 위함 (2026-07-23).
            public float ConnectionCoord;
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

            // 설치도 치수 baseline은 선택 STRU로 고정한다 (#63). 연결부재까지 넣으면
            //   상대 서포트 전체 BBox가 baseline을 밀어 보조선이 뷰마다 길어진다 —
            //   2D 출력 경로(Form1.DrawingSheets.cs)도 같은 기준을 쓴다.
            xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

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
                VIZCore3D.NET.Data.Node connectedAssembly = FindParentStru(connectedPart) ?? FindNearestParentAssembly(connectedPart);   // #45 연결부재 STRU 단위
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
            // #63: 접합한 Part 하나가 아니라 그 Part가 속한 서포트(STRU) 전체를 표시 대상으로 넓힌다.
            List<int> contextIndices = ExpandInstallationContextToStru(sheet, contextPartIndices);
            sheet.InstallationContextIndices.AddRange(contextIndices.OrderBy(index => index));

            DiagLog($"설치도 연결 영역 준비 완료: connectedParts={contextPartIndices.Count} " +
                    $"contextNodes={sheet.InstallationContextIndices.Count} " +
                    $"areas={sheet.InstallationConnections.Count} " +
                    $"fallback={sheet.InstallationConnections.Count(c => c.IsProximityFallback)}");
        }

        /// <summary>
        /// #63: 연결이 확인된 외부 Part를 그 Part가 속한 STRU 전체(하위 BODY 전부)로 넓힌다.
        ///
        /// 도면에는 접합한 부재 한 개가 아니라 상대 서포트 형상 전체가 점선으로 보여야 한다.
        /// 접합 판정·위치 치수·A/B/C 라벨은 종전대로 실제 접합 Part 기준을 쓰고 여기서는
        /// **표시 대상만** 넓힌다 — 이 목록은 점선 배경 캡처에만 들어간다.
        ///
        /// 반환값이 BODY 인덱스인 이유: sheet.MemberIndices가 STRU 후손 BODY 목록이라
        /// 같은 단위로 맞춰야 "시트 부재는 실선이니 점선에서 뺀다"는 걸러내기가 실제로 동작한다.
        /// STRU 조상을 못 찾은 Part는 종전대로 그 Part만 남긴다.
        ///
        /// 배율·화면 맞춤은 여기서 넓힌 범위와 무관하게 선택 STRU 기준으로 고정된다 —
        /// 캡처 뒤 CropFit이 "시트 부재 ± 여백"만 남기므로 화면 밖 연결부재는 잘려 나간다.
        /// </summary>
        private List<int> ExpandInstallationContextToStru(
            DrawingSheetData sheet, IEnumerable<int> connectedPartIndices)
        {
            var result = new HashSet<int>();
            if (connectedPartIndices == null) return result.ToList();

            var sheetBodies = new HashSet<int>(sheet.MemberIndices ?? new List<int>());
            var struBodyCache = new Dictionary<int, List<int>>();

            foreach (int partIndex in connectedPartIndices)
            {
                if (partIndex < 0) continue;

                VIZCore3D.NET.Data.Node part = null;
                try { part = vizcore3d.Object3D.FromIndex(partIndex); }
                catch { }

                VIZCore3D.NET.Data.Node stru = FindParentStru(part);
                if (stru == null)
                {
                    result.Add(partIndex);
                    continue;
                }

                List<int> struBodies;
                if (!struBodyCache.TryGetValue(stru.Index, out struBodies))
                {
                    struBodies = GetDescendantBodyIndices(stru.Index)
                        .Where(index => !sheetBodies.Contains(index))
                        .ToList();
                    struBodyCache[stru.Index] = struBodies;
                }

                if (struBodies.Count == 0)
                {
                    // STRU 하위가 전부 시트 부재라 넓힐 게 없다 — 종전대로 접합 Part만 남긴다.
                    result.Add(partIndex);
                    continue;
                }

                foreach (int bodyIndex in struBodies) result.Add(bodyIndex);
            }

            if (struBodyCache.Count > 0)
                DiagLog($"설치도 연결 STRU 확장 (#63): stru={struBodyCache.Count}개 " +
                        $"parts={connectedPartIndices.Count()} → nodes={result.Count} " +
                        $"[{string.Join(", ", struBodyCache.Select(p => $"{p.Key}:{p.Value.Count}body"))}]");

            return result.ToList();
        }

        /// <summary>
        /// Part 인덱스를 받아 → 그 Part에 속한 BODY 인덱스 목록을 돌려준다. 캐시·SDK 모두 못 찾으면 빈 목록.
        /// 제작도 근접 검사의 BODY→Part 캐시를 먼저 쓰고, 없으면 SDK로 하위 자식 중 BODY만 모은다.
        /// 설치도 연결 데이터 준비에서 간섭 Part 쌍을 BODY 쌍으로 넓힐 때 호출. SDK 예외는 조용히 삼킨다.
        /// </summary>
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

        /// <summary>
        /// 노드 인덱스를 받아 → 그 노드 자신(BODY일 때)과 모든 하위 BODY 인덱스를 중복 없이 돌려준다. 없으면 빈 목록.
        /// 설치도 점선 표시 범위를 상대 STRU 하위 BODY 전체로 넓힐 때 호출. SDK 예외는 조용히 삼킨다.
        /// </summary>
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

        /// <summary>
        /// 두 BODY 인덱스를 받아 → SDK 접합선을 1mm 이내로 이어진 접합영역으로 묶어 돌려준다. 접합선이 없으면 접합 Mesh, 둘 다 없으면 빈 목록.
        /// 영역의 각 점은 양쪽 BODY의 LINE/POINT Osnap에 3mm 이내면 스냅하고 같은 좌표는 하나로 합친다.
        /// 설치도 연결 데이터 준비에서 BODY 쌍마다 호출. SDK 조회 실패는 진단 로그만 남긴다.
        /// </summary>
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

        /// <summary>
        /// 접합 선분 목록을 받아 → 어느 점이든 1mm 이내로 맞닿는 선분끼리 한 접합영역으로 합쳐 돌려준다.
        /// 새 선분이 여러 영역에 동시에 닿으면 그 영역들까지 하나로 병합한다.
        /// 순수 좌표 계산. SDK·화면 상태를 건드리지 않는다.
        /// </summary>
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

        /// <summary>
        /// 설치 연결 목록을 받아 → 상대 Assembly·Part 단위로 묶어 이름순 정렬한 뒤 A, B, … Z, AA 순 라벨을 각 연결에 써 넣는다.
        /// 같은 상대 Part의 접합영역 여러 개는 같은 라벨을 받는다. 설치도 연결 데이터 준비 끝에 호출.
        /// </summary>
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

        /// <summary>
        /// 0부터 시작하는 순번을 받아 → 엑셀 열 이름 방식의 알파벳 라벨(A…Z, AA, AB…)을 돌려준다.
        /// 설치 연결 라벨 부여에서 사용.
        /// </summary>
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

        /// <summary>
        /// BODY 인덱스 목록과 한 점을 받아 → 점에서 경계상자까지 바깥 거리가 가장 짧은 BODY 인덱스를 돌려준다. 목록이 비면 -1.
        /// 경계상자는 제작도 근접 검사 캐시에서 읽고, 캐시에 없는 BODY는 건너뛴다(전부 없으면 첫 항목).
        /// 접합선·Mesh가 없을 때 간섭 HotPoint의 대표 BODY를 고르는 fallback에서 호출.
        /// </summary>
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

        /// <summary>
        /// BODY 인덱스들을 받아 → 각 BODY Osnap 중 LINE의 시작·끝점과 POINT의 중심을 모아 좌표 목록으로 돌려준다.
        /// CIRCLE 등 다른 종류는 제외. 중복 좌표는 합치지 않으므로 호출측이 정리한다.
        /// 조회 실패는 진단 로그만 남기고 그 BODY를 건너뛴다. 접합점 스냅·끝단 후보 수집에서 호출.
        /// </summary>
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

        /// <summary>
        /// BODY 인덱스를 받아 → 끝단 후보가 될 LINE/POINT Osnap 좌표를 중복 없이 돌려준다. 없으면 경계상자 8개 모서리로 대신한다.
        /// Osnap이 하나도 없으면 boundsFallback을 true로 올린 뒤 경계상자를 시도하므로,
        /// 경계상자마저 못 구해 빈 목록을 돌려줄 때도 boundsFallback은 true다.
        /// 설치 위치 기준점 계산에서 대상·상대 BODY 각각에 호출.
        /// </summary>
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

        /// <summary>
        /// 같은 BODY 쌍의 설치 연결들을 받아 → 대상 BODY 길이축·접합 중심·축별 끝단·접합 모서리를 담은 위치 기준점을 돌려준다. 못 구하면 null.
        /// 접합 extent가 부재 extent의 절반 이상인 축은 가로지르는 접합으로 보고 성분에서 뺀다. 진단 로그를 남긴다.
        /// 설치 위치 치수 계산과 설치도 ISO 연결 이름 위치 선정에서 호출.
        /// </summary>
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

            // 접합점이 전혀 없으면(교선·mesh·hotpoint 모두 실패) 위치를 특정할 수 없다 — 치수 생략.
            if (contactPoints.Count == 0) return null;

            // ── [2026-07-23 재설계] 위치 기준을 연결부재 osnap → 접합점(교선)으로 ──
            //   접합점은 두 부재가 물리적으로 맞닿는 교선이라 정의상 기준부재 표면 위에 있다. 연결부재가 큰
            //   부재라 그 osnap이 접합부에서 멀어도(147·950), 접합점 자체는 항상 기준부재 범위 안 → 위치 치수가
            //   도면을 벗어나는 게 구조적으로 불가능. 임계값(성분 상한·크기 비율)이 필요 없어진다.
            var contactCentroid = new VIZCore3D.NET.Data.Vector3D(
                (float)contactPoints.Average(p => (double)p.X),
                (float)contactPoints.Average(p => (double)p.Y),
                (float)contactPoints.Average(p => (double)p.Z));

            double memExtX = targetPoints.Max(p => (double)p.X) - targetPoints.Min(p => (double)p.X);
            double memExtY = targetPoints.Max(p => (double)p.Y) - targetPoints.Min(p => (double)p.Y);
            double memExtZ = targetPoints.Max(p => (double)p.Z) - targetPoints.Min(p => (double)p.Z);
            double conExtX = contactPoints.Max(p => (double)p.X) - contactPoints.Min(p => (double)p.X);
            double conExtY = contactPoints.Max(p => (double)p.Y) - contactPoints.Min(p => (double)p.Y);
            double conExtZ = contactPoints.Max(p => (double)p.Z) - contactPoints.Min(p => (double)p.Z);

            VIZCore3D.NET.Data.Vector3D mainEnd =
                SelectInstallationTargetEndForAxis(targetPoints, contactCentroid, worldAxis);

            InstallationPlacementAnchor best = new InstallationPlacementAnchor
            {
                TargetPartIndex = first.TargetPartIndex,
                TargetBodyIndex = first.TargetBodyIndex,
                ConnectedPartIndex = first.ConnectedPartIndex,
                ConnectedBodyIndex = first.ConnectedBodyIndex,
                Axis = worldAxis,
                TargetEndPoint = mainEnd,
                ConnectedCornerPoint = contactCentroid,     // 위치 기준 = 접합점(기준부재 위)
                TargetBoundsFallback = targetBoundsFallback,
                ConnectedBoundsFallback = connectedBoundsFallback,
                MainDirectionTotalLength = mainDirectionTotalLength,
                SecondDirectionTotalLength = secondDirectionTotalLength,
                MergedAreaCount = connections.Count
            };

            // ── 축 선택: 접합이 그 축으로 부재를 얼마나 덮는가 (coverage = 접합 extent / 부재 extent) ──
            //   [2026-07-23 재설계] 크기 임계(부재가 긴가) 대신 접합 형태로 판정한다.
            //   coverage ≥ CrossCoverage: 연결부재가 그 축으로 부재를 가로지름(관통) → 특정 위치 없음 → 생략.
            //   coverage < CrossCoverage: 한 지점에 국소적으로 붙음 → 위치 치수 표시.
            //   막대는 길이축만(단면축은 파이프가 관통해 100% 덮음), 판재는 길이+폭이 자연히 나온다.
            //   접합점은 기준부재 위라 위치는 항상 부재 span 안 → 상한·비율 임계값 불필요.
            string[] axesXYZ = { "X", "Y", "Z" };
            double[] memExt = { memExtX, memExtY, memExtZ };
            double[] conExt = { conExtX, conExtY, conExtZ };
            var placedAxes = new List<string>();
            var crossedAxes = new List<string>();
            for (int ai = 0; ai < 3; ai++)
            {
                string axis = axesXYZ[ai];
                double coverage = memExt[ai] > 1e-3 ? conExt[ai] / memExt[ai] : 1.0;
                if (coverage >= InstallationContactCrossCoverage)
                {
                    crossedAxes.Add($"{axis}={coverage:P0}");
                    continue;
                }
                VIZCore3D.NET.Data.Vector3D axisEnd = axis == worldAxis
                    ? mainEnd
                    : SelectInstallationTargetEndForAxis(targetPoints, contactCentroid, axis);
                // 접합측 모서리 (2026-07-23 실기): 접합이 그 축으로 구간(예: 100mm)일 때 한가운데(centroid)까지
                //   재면 의미가 없다. 접합 구간의 양 끝(min/max) 중 부재 끝단(axisEnd)에 가까운 쪽 = 연결부재가
                //   닿기 시작하는 모서리까지 잰다. (막대 30/판재 29 같은 실기 검증값 복원)
                double cMin = contactPoints.Min(p => (double)GetVectorAxisValue(p, axis));
                double cMax = contactPoints.Max(p => (double)GetVectorAxisValue(p, axis));
                double endV = GetVectorAxisValue(axisEnd, axis);
                float edge = (float)(Math.Abs(endV - cMax) <= Math.Abs(endV - cMin) ? cMax : cMin);
                best.AxisComponents.Add(new InstallationAxisComponent
                {
                    Axis = axis,
                    TargetEndPoint = axisEnd,
                    ConnectionCoord = edge
                });
                placedAxes.Add($"{axis}(덮음{coverage:P0},모서리{Math.Abs(endV - edge):F0})");
            }

            double mainComp = Math.Abs(
                GetVectorAxisValue(contactCentroid, worldAxis) - GetVectorAxisValue(mainEnd, worldAxis));
            DiagLog($"[설치치수축] targetPart={best.TargetPartIndex} " +
                    $"부재extent=(X={memExtX:F0},Y={memExtY:F0},Z={memExtZ:F0}) " +
                    $"접합extent=(X={conExtX:F0},Y={conExtY:F0},Z={conExtZ:F0}) 접합점={contactPoints.Count} " +
                    $"기준=({contactCentroid.X:F0},{contactCentroid.Y:F0},{contactCentroid.Z:F0}) " +
                    $"cross임계={InstallationContactCrossCoverage:P0} 위치축=[{string.Join(",", placedAxes)}]" +
                    (crossedAxes.Count > 0 ? $" 가로지름제외=[{string.Join(",", crossedAxes)}]" : ""));

            DiagLog($"[설치위치] targetPart={best.TargetPartIndex} targetBody={best.TargetBodyIndex} " +
                    $"connectedPart={best.ConnectedPartIndex} connectedBody={best.ConnectedBodyIndex} " +
                    $"axis={best.Axis} mainDir=({direction.X:F3},{direction.Y:F3},{direction.Z:F3}) " +
                    $"mainSum={best.MainDirectionTotalLength:F1} " +
                    $"secondSum={best.SecondDirectionTotalLength:F1} areas={best.MergedAreaCount} " +
                    $"targetEnd=({best.TargetEndPoint.X:F1},{best.TargetEndPoint.Y:F1},{best.TargetEndPoint.Z:F1}) " +
                    $"접합기준=({best.ConnectedCornerPoint.X:F1},{best.ConnectedCornerPoint.Y:F1},{best.ConnectedCornerPoint.Z:F1}) " +
                    $"mainComp={mainComp:F1} targetBBox={best.TargetBoundsFallback} " +
                    $"connectedBBox={best.ConnectedBoundsFallback}");
            return best;
        }

        /// <summary>
        /// 한 점·Osnap 좌표 목록·허용거리를 받아 → 가장 가까운 Osnap이 허용거리 안이면 그 좌표 복사본을, 아니면 원래 점을 돌려준다.
        /// 접합선·Mesh 점을 실제 모서리 좌표로 맞출 때 호출. 목록이 비면 원래 점 그대로.
        /// </summary>
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

        /// <summary>
        /// 두 3D 점을 받아 → 유클리드 직선거리를 돌려준다.
        /// 접합 선분 병합·Osnap 스냅·끝단 후보 정렬의 거리 비교에 공용으로 사용.
        /// </summary>
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

            // 위치 성분을 그 축이 화면에 실제로 보이는(=화면 평면에 있는) 직교 뷰에만 배정한다.
            //   그 축을 정면으로 마주보는 뷰(예: X 성분 ↔ -X 뷰)는 축이 깊이 방향이라 두 끝점이 화면상 겹치고
            //   보조선이 하나로 합쳐져 치수가 뭉개진다 (2026-07-23 실기 PDF로 확인) → 그 뷰에는 배정하지 않는다.
            //   ⚠ 세 뷰 모두 배정(전 시도)은 -X에서 형강 길이축 치수를 겹친 보조선으로 잘못 그렸음 — 폐기.
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
                    //   연결측 끝점은 접합 한가운데가 아니라 접합측 모서리(component.ConnectionCoord)를 쓴다 (2026-07-23).
                    VIZCore3D.NET.Data.Vector3D compStart = component.TargetEndPoint;
                    float conn = component.ConnectionCoord;
                    VIZCore3D.NET.Data.Vector3D compEnd;
                    switch (component.Axis)
                    {
                        case "X": compEnd = new VIZCore3D.NET.Data.Vector3D(conn, compStart.Y, compStart.Z); break;
                        case "Y": compEnd = new VIZCore3D.NET.Data.Vector3D(compStart.X, conn, compStart.Z); break;
                        default: compEnd = new VIZCore3D.NET.Data.Vector3D(compStart.X, compStart.Y, conn); break;
                    }
                    DiagLog($"[설치치수] {targetName}→{connection.ConnectedPartName} 축={component.Axis} " +
                            $"성분={Math.Abs(conn - GetVectorAxisValue(compStart, component.Axis)):F1}");
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

        /// <summary>
        /// 설치 위치 치수 한 건의 양 끝점·축·뷰·이름·부재 목록을 받아 → 체인치수 데이터로 만들어 결과 목록에 추가한다.
        /// 지정 축 성분 차이가 3mm 이하면 추가하지 않는다. 부재 목록의 음수 인덱스는 제거한다.
        /// 설치 위치 치수 계산에서 축 성분마다, 그 축이 보이는 뷰별로 호출.
        /// </summary>
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

        /// <summary>
        /// 3D 점과 축 문자열(X/Y/Z)을 받아 → 그 축의 좌표값을 돌려준다. 알 수 없는 축이면 0.
        /// 설치 위치 치수의 축별 투영·끝단 선정·중복 제거 키 생성에 공용으로 사용.
        /// </summary>
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
