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
        /// <summary>
        /// 모델 파일이 위치한 솔루션 폴더 경로 반환
        /// </summary>
        public string GetSolutionPath()
        {
            string startPath = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo directory = new DirectoryInfo(startPath);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Any())
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            return startPath;
        }

        /// <summary>
        /// 2D 도면 생성 — 전체 BOM 부재를 대상으로 GenerateSheetDrawing2D 호출
        /// (도면정보 탭의 "2D 출력"과 동일한 신버전 로직: Hidden Line, 충돌방지 풍선, 보조선, 벡터 PDF 대응)
        /// </summary>
        private void btnGenerate2D_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (bomList == null || bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.\n먼저 BOM을 수집해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 전체 BOM 부재로 임시 DrawingSheetData 생성
            DrawingSheetData sheet = new DrawingSheetData();
            sheet.SheetNumber = 1;

            if (selectedAttributeNodeIndex != -1)
            {
                var selectedNode = vizcore3d.Object3D.FromIndex(selectedAttributeNodeIndex);
                sheet.BaseMemberName = (selectedNode != null && !string.IsNullOrEmpty(selectedNode.NodeName))
                    ? selectedNode.NodeName
                    : System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
            }
            else
            {
                sheet.BaseMemberName = !string.IsNullOrEmpty(currentFilePath)
                    ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath)
                    : "전체";
            }
            sheet.BaseMemberIndex = -1;

            foreach (var bom in bomList)
            {
                sheet.MemberIndices.Add(bom.Index);
            }

            GenerateSheetDrawing2D(sheet);
        }

        /// <summary>
        /// PDF 내보내기 — VIZCore3D 내장 Export2PDFBy2DView API 사용 (벡터 PDF)
        /// (도면정보 탭의 "PDF 출력"과 동일한 신버전 로직)
        /// </summary>
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (vizcore3d.ViewMode != VIZCore3D.NET.Data.ViewKind.Both)
            {
                MessageBox.Show("먼저 '2D 생성' 버튼으로 2D 도면을 생성해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PDF 파일 (*.pdf)|*.pdf";
            dlg.FilterIndex = 1;
            dlg.FileName = $"2D_Drawing_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                // 노란색 선택 테두리 제거
                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(dlg.FileName);

                MessageBox.Show($"PDF 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF 저장 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// BOM 리스트 더블클릭 - 노드 위치로 이동 및 강조
        /// </summary>
        private void LvBOM_DoubleClick(object sender, EventArgs e)
        {
            if (lvBOM.SelectedItems.Count == 0) return;

            try
            {
                BOMData bom = lvBOM.SelectedItems[0].Tag as BOMData;
                if (bom == null) return;

                // 노드 리스트 생성
                List<int> indices = new List<int>();
                indices.Add(bom.Index);

                // 노드 선택 (기존 선택 해제하고 새로 선택)
                vizcore3d.Object3D.Select(indices, true, true);

                // 선택된 노드로 카메라 이동
                vizcore3d.View.FlyToObject3d(indices, 1.2f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"노드 이동 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clash 리스트 더블클릭 - 충돌 지점 표시
        /// </summary>
        private void LvClash_DoubleClick(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0) return;

            try
            {
                ClashData clash = lvClash.SelectedItems[0].Tag as ClashData;
                if (clash == null) return;

                // 기존 선택/색상 초기화 후 두 노드만 선택
                vizcore3d.Object3D.Color.RestoreColorAll();
                List<int> indices = new List<int>();
                indices.Add(clash.Index1);
                indices.Add(clash.Index2);
                vizcore3d.Object3D.Select(indices, true, true);

                // 선택된 노드로 카메라 이동
                vizcore3d.View.FlyToObject3d(indices, 1.2f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clash 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 수집 및 화면 표시 (GetBodyVertices 사용)
        /// </summary>
        private void btnCollectOsnap_Click(object sender, EventArgs e)
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();

            try
            {
                // 이전 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // Body 노드 가져오기
                List<VIZCore3D.NET.Data.Node> allBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);


                if (allBodyNodes == null || allBodyNodes.Count == 0)
                {
                    MessageBox.Show("로드된 Body 노드가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // X-Ray 필터링: 프로그래밍 선택 → 수동 X-Ray → 전체
                List<VIZCore3D.NET.Data.Node> bodyNodes;
                bool isFilteredMode;

                if (xraySelectedNodeIndices.Count > 0)
                {
                    bodyNodes = allBodyNodes.Where(n => xraySelectedNodeIndices.Contains(n.Index)).ToList();
                    isFilteredMode = true;
                }
                else
                {
                    bodyNodes = allBodyNodes.Where(n =>
                    {
                        var realNode = vizcore3d.Object3D.FromIndex(n.Index);
                        return realNode != null && realNode.Visible;
                    }).ToList();
                    isFilteredMode = bodyNodes.Count < allBodyNodes.Count && bodyNodes.Count > 0;
                    if (bodyNodes.Count == 0) bodyNodes = allBodyNodes;
                }


                //MessageBox.Show(
                //    $"Count = {bodyNodes.Count}\n" +
                //    $"First Index = {bodyNodes[0].Index}\n" +
                //    $"First Name = {bodyNodes[0].NodeName}",
                //    "Debug"
                //);

                System.Text.StringBuilder debugSb = new System.Text.StringBuilder();
                debugSb.AppendLine("=== Osnap 수집 (GetOsnapPoint API) ===\n");
                if (isFilteredMode)
                {
                    debugSb.AppendLine($"[선택 항목만 보기 모드]");
                    debugSb.AppendLine($"전체 Body 노드: {allBodyNodes.Count}개");
                    debugSb.AppendLine($"선택된 부재에서 수집: {bodyNodes.Count}개\n");
                }
                else
                {
                    debugSb.AppendLine($"Body 노드: {bodyNodes.Count}개\n");
                }

                int lineCount = 0, circleCount = 0, pointCount = 0, surfaceCount = 0;

                // 각 Body의 Osnap 포인트 수집
                foreach (var node in bodyNodes)
                {
                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList =
                        vizcore3d.Object3D.GetOsnapPoint(node.Index);

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        foreach (var osnap in osnapList)
                        {
                            // Osnap 유형별 처리
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    // 선: 시작점과 끝점 추가
                                    if (osnap.Start != null)
                                    {
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, node.NodeName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, node.NodeName));
                                    }
                                    lineCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    // 곡면/원형: 치수에서 제외
                                    circleCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    // 점: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, node.NodeName));
                                    }
                                    pointCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.SURFACE:
                                    // 표면: 제외 (곡면 데이터가 많음)
                                    surfaceCount++;
                                    break;
                            }
                        }
                    }
                }

                debugSb.AppendLine($"Osnap 유형별 개수:");
                debugSb.AppendLine($"  - LINE: {lineCount}개");
                debugSb.AppendLine($"  - CIRCLE: {circleCount}개");
                debugSb.AppendLine($"  - POINT: {pointCount}개");
                debugSb.AppendLine($"  - SURFACE: {surfaceCount}개 (제외됨)");
                debugSb.AppendLine($"\n총 수집된 좌표: {osnapPoints.Count}개");





                // ListView에 좌표와 부재 이름 추가 (심볼 표시 제거)
                if (osnapPoints.Count > 0)
                {
                    lvOsnap.Items.Clear();
                    for (int i = 0; i < osnapPointsWithNames.Count; i++)
                    {
                        var item = osnapPointsWithNames[i];
                        ListViewItem lvi = new ListViewItem((i + 1).ToString());
                        lvi.SubItems.Add(item.nodeName);
                        lvi.SubItems.Add(item.point.X.ToString("F2"));
                        lvi.SubItems.Add(item.point.Y.ToString("F2"));
                        lvi.SubItems.Add(item.point.Z.ToString("F2"));
                        var matchBom = bomList?.FirstOrDefault(b => b.Name == item.nodeName);
                        var sizes = GetHoleOrSlotForPoint(matchBom, item.point.X, item.point.Y, item.point.Z);
                        lvi.SubItems.Add(sizes.holeSize);
                        lvi.SubItems.Add(sizes.slotHoleSize);
                        lvOsnap.Items.Add(lvi);
                    }

                    debugSb.AppendLine($"\nListView에 {osnapPointsWithNames.Count}개 좌표 추가됨");
                }
                else
                {
                    debugSb.AppendLine("\nOsnap 포인트를 찾을 수 없습니다.");
                    lvOsnap.Items.Clear();
                }

                // 디버그 정보 출력
                System.Diagnostics.Debug.WriteLine(debugSb.ToString());
                MessageBox.Show(debugSb.ToString(), "Osnap 수집 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Osnap 수집 후 자동으로 치수 추출
                if (osnapPointsWithNames.Count > 0)
                {
                    ExtractDimensionForSelectedNodes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 수집 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clash 리스트에서 선택한 항목만 뷰어에 표시
        /// </summary>
        private void btnClashShowSelected_Click(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0)
            {
                MessageBox.Show("Clash 항목을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // X-Ray 모드 활성화
                if (!vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = true;

                // X-Ray 옵션 설정
                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;

                // X-Ray 모드에서도 모서리(SilhouetteEdge) 항상 표시
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 이전 선택 해제
                vizcore3d.View.XRay.Clear();

                // 선택된 Clash 항목의 노드들만 표시
                List<int> selectedIndices = new List<int>();
                HashSet<string> relatedNodeNames = new HashSet<string>();
                List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> relatedBounds = new List<(float, float, float, float, float, float)>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        if (!selectedIndices.Contains(clash.Index1))
                            selectedIndices.Add(clash.Index1);
                        if (!selectedIndices.Contains(clash.Index2))
                            selectedIndices.Add(clash.Index2);

                        // 노드 이름 수집
                        if (!string.IsNullOrEmpty(clash.Name1))
                            relatedNodeNames.Add(clash.Name1);
                        if (!string.IsNullOrEmpty(clash.Name2))
                            relatedNodeNames.Add(clash.Name2);
                    }
                }

                // 선택된 노드만 표시
                vizcore3d.View.XRay.Select(selectedIndices, true);

                // X-Ray 모드에서 선택된 노드 인덱스 저장 (Osnap 수집 시 사용)
                xraySelectedNodeIndices = new List<int>(selectedIndices);

                // 선택된 노드로 화면 이동
                vizcore3d.View.FlyToObject3d(selectedIndices, 1.2f);

                // 이전 심볼 제거 후 새로 표시
                vizcore3d.Clash.ClearResultSymbol();

                // 선택된 충돌 지점에 심볼 표시
                List<VIZCore3D.NET.Data.Vertex3D> clashPoints = new List<VIZCore3D.NET.Data.Vertex3D>();
                List<VIZCore3D.NET.Data.ClashResultSymbols> symbols = new List<VIZCore3D.NET.Data.ClashResultSymbols>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        BOMData bom1 = bomList.FirstOrDefault(b => b.Index == clash.Index1);
                        BOMData bom2 = bomList.FirstOrDefault(b => b.Index == clash.Index2);

                        if (bom1 != null && bom2 != null)
                        {
                            float clashX = (Math.Max(bom1.MinX, bom2.MinX) + Math.Min(bom1.MaxX, bom2.MaxX)) / 2.0f;
                            float clashY = (Math.Max(bom1.MinY, bom2.MinY) + Math.Min(bom1.MaxY, bom2.MaxY)) / 2.0f;
                            float clashZ = (Math.Max(bom1.MinZ, bom2.MinZ) + Math.Min(bom1.MaxZ, bom2.MaxZ)) / 2.0f;

                            clashPoints.Add(new VIZCore3D.NET.Data.Vertex3D(clashX, clashY, clashZ));
                            symbols.Add(VIZCore3D.NET.Data.ClashResultSymbols.Triangle);

                            // 바운딩 박스 저장
                            relatedBounds.Add((
                                Math.Min(bom1.MinX, bom2.MinX),
                                Math.Max(bom1.MaxX, bom2.MaxX),
                                Math.Min(bom1.MinY, bom2.MinY),
                                Math.Max(bom1.MaxY, bom2.MaxY),
                                Math.Min(bom1.MinZ, bom2.MinZ),
                                Math.Max(bom1.MaxZ, bom2.MaxZ)
                            ));
                        }
                    }
                }

                if (clashPoints.Count > 0)
                {
                    vizcore3d.Clash.ShowResultSymbol(clashPoints, symbols, 10f, true, System.Drawing.Color.Yellow, false);
                }

                vizcore3d.EndUpdate();

                // 선택된 부재에 대해 자동으로 Osnap 수집 및 리스트 업데이트
                CollectOsnapForSelectedNodes(selectedIndices);

                // 선택된 부재에 대해 자동으로 치수 추출 및 리스트 업데이트
                ExtractDimensionForSelectedNodes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clash 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택된 노드에 대해서만 Osnap 수집 (자동 호출용)
        /// </summary>
        private void CollectOsnapForSelectedNodes(List<int> nodeIndices)
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();
            lvOsnap.Items.Clear();

            try
            {
                if (nodeIndices == null || nodeIndices.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[CollectOsnapForSelectedNodes] 노드 인덱스가 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 입력된 노드 인덱스: {string.Join(", ", nodeIndices)}");

                int lineCount = 0, circleCount = 0, pointCount = 0, surfaceCount = 0;

                // 각 노드 인덱스에 대해 직접 Osnap 포인트 수집
                foreach (int nodeIndex in nodeIndices)
                {
                    // 노드 정보 가져오기 (Part 풀네임 사용)
                    VIZCore3D.NET.Data.Node node = vizcore3d.Object3D.FromIndex(nodeIndex);
                    string nodeName = GetPartNameFromBodyIndex(nodeIndex, node != null ? node.NodeName : $"Node_{nodeIndex}");

                    System.Diagnostics.Debug.WriteLine($"  - 노드 인덱스 {nodeIndex}: {nodeName}");

                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList =
                        vizcore3d.Object3D.GetOsnapPoint(nodeIndex);

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"    → Osnap 포인트: {osnapList.Count}개");

                        foreach (var osnap in osnapList)
                        {
                            // Osnap 유형별 처리
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    // 선: 시작점과 끝점 추가
                                    if (osnap.Start != null)
                                    {
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, nodeName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, nodeName));
                                    }
                                    lineCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    // 곡면/원형: 치수에서 제외 (곡면 Osnap은 체인치수에 불필요)
                                    circleCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    // 점: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, nodeName));
                                    }
                                    pointCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.SURFACE:
                                    // 표면: 제외
                                    surfaceCount++;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    → Osnap 포인트 없음");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 총 수집: LINE={lineCount}, CIRCLE={circleCount}, POINT={pointCount}, SURFACE={surfaceCount}(제외)");
                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 총 좌표: {osnapPoints.Count}개");

                // ListView에 좌표와 부재 이름 추가 (심볼 표시 제거)
                if (osnapPoints.Count > 0)
                {
                    for (int i = 0; i < osnapPointsWithNames.Count; i++)
                    {
                        var item = osnapPointsWithNames[i];
                        ListViewItem lvi = new ListViewItem((i + 1).ToString());
                        lvi.SubItems.Add(item.nodeName);
                        lvi.SubItems.Add(item.point.X.ToString("F2"));
                        lvi.SubItems.Add(item.point.Y.ToString("F2"));
                        lvi.SubItems.Add(item.point.Z.ToString("F2"));
                        var matchBom = bomList?.FirstOrDefault(b => b.Name == item.nodeName);
                        var sizes = GetHoleOrSlotForPoint(matchBom, item.point.X, item.point.Y, item.point.Z);
                        lvi.SubItems.Add(sizes.holeSize);
                        lvi.SubItems.Add(sizes.slotHoleSize);
                        lvOsnap.Items.Add(lvi);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Clash 선택 부재] Osnap 자동 수집 완료: {nodeIndices.Count}개 부재에서 {osnapPoints.Count}개 좌표");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Osnap 자동 수집 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 노드에 대해서만 치수 추출 (자동 호출용)
        /// </summary>
        private void ExtractDimensionForSelectedNodes()
        {
            if (osnapPointsWithNames == null || osnapPointsWithNames.Count == 0)
            {
                return;
            }

            try
            {
                // 기존 측정 항목 제거
                vizcore3d.Review.Measure.Clear();
                chainDimensionList.Clear();
                lvDimension.Items.Clear();

                float tolerance = 0.5f;  // 허용 오차 0.5mm

                // 좌표 병합 (허용 오차 내 같은 좌표로 그룹화)
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(osnapPointsWithNames, tolerance);

                // X축 방향 체인 치수 (Y, Z가 같은 점들)
                var xDimensions = AddChainDimensionByAxis(mergedPoints, "X", tolerance);
                chainDimensionList.AddRange(xDimensions);

                // Y축 방향 체인 치수 (X, Z가 같은 점들)
                var yDimensions = AddChainDimensionByAxis(mergedPoints, "Y", tolerance);
                chainDimensionList.AddRange(yDimensions);

                // Z축 방향 체인 치수 (X, Y가 같은 점들)
                var zDimensions = AddChainDimensionByAxis(mergedPoints, "Z", tolerance);
                chainDimensionList.AddRange(zDimensions);

                // ListView에 추가 및 치수 번호 설정
                int no = 1;
                foreach (var dim in chainDimensionList)
                {
                    dim.No = no;  // 치수 데이터에 번호 저장
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

                System.Diagnostics.Debug.WriteLine($"[Clash 선택 부재] 치수 자동 추출: {chainDimensionList.Count}개 치수");

                // 치수 추출 후 자동으로 모든 치수 표시 (오프셋 + 보조선 스타일)
                ShowAllDimensions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"치수 자동 추출 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 전체 모델 보기 (X-Ray 모드 해제)
        /// </summary>
        private void btnClashShowAll_Click(object sender, EventArgs e)
        {
            try
            {
                vizcore3d.BeginUpdate();

                // 가공도 모드에서 숨긴 부재 복원
                RestoreAllPartsVisibility();

                // X-Ray 모드 해제
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // X-Ray 선택 노드 리스트 초기화
                xraySelectedNodeIndices.Clear();

                // 색상 복원
                vizcore3d.Object3D.Color.RestoreColorAll();

                // 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // 모서리(SilhouetteEdge) 복원
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 전체 화면 맞춤
                vizcore3d.View.FitToView();

                vizcore3d.EndUpdate();

                // 모든 치수 다시 표시
                ShowAllDimensions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전체 보기 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 좌표 추가 (마우스 클릭으로 좌표 선택)
        /// </summary>
        private void btnOsnapAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Osnap 픽킹 이벤트 등록
                vizcore3d.GeometryUtility.OnOsnapPickingItem -= GeometryUtility_OnOsnapPickingItem;
                vizcore3d.GeometryUtility.OnOsnapPickingItem += GeometryUtility_OnOsnapPickingItem;

                // Osnap 모드 활성화
                vizcore3d.GeometryUtility.ShowOsnap(false, true, true, true);

                MessageBox.Show("뷰어에서 원하는 위치를 클릭하면 Osnap 좌표가 추가됩니다.\n(꼭짓점, 선, 원 등)", "Osnap 추가 모드", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 추가 모드 활성화 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 픽킹 이벤트 핸들러
        /// </summary>
        private void GeometryUtility_OnOsnapPickingItem(object sender, VIZCore3D.NET.Event.EventManager.OsnapPickingItemEventArgs e)
        {
            if (e.Point == null) return;

            try
            {
                // 새 좌표 추가
                VIZCore3D.NET.Data.Vertex3D point = new VIZCore3D.NET.Data.Vertex3D(e.Point.X, e.Point.Y, e.Point.Z);
                string nodeName = "수동 추가";

                // 선택된 개체가 있으면 이름 가져오기
                List<VIZCore3D.NET.Data.Node> selectedNodes = vizcore3d.Object3D.FromFilter(VIZCore3D.NET.Data.Object3dFilter.SELECTED_TOP);
                if (selectedNodes.Count > 0)
                {
                    nodeName = selectedNodes[0].NodeName;
                }

                osnapPoints.Add(point);
                osnapPointsWithNames.Add((point, nodeName));

                // ListView에 추가
                int newIndex = osnapPointsWithNames.Count;
                ListViewItem lvi = new ListViewItem(newIndex.ToString());
                lvi.SubItems.Add(nodeName);
                lvi.SubItems.Add(point.X.ToString("F2"));
                lvi.SubItems.Add(point.Y.ToString("F2"));
                lvi.SubItems.Add(point.Z.ToString("F2"));
                var matchBom = bomList?.FirstOrDefault(b => b.Name == nodeName);
                var sizes = GetHoleOrSlotForPoint(matchBom, point.X, point.Y, point.Z);
                lvi.SubItems.Add(sizes.holeSize);
                lvi.SubItems.Add(sizes.slotHoleSize);
                lvOsnap.Items.Add(lvi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 좌표 추가 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 Osnap 좌표 삭제
        /// </summary>
        private void btnOsnapDelete_Click(object sender, EventArgs e)
        {
            if (lvOsnap.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 Osnap 좌표를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 선택된 항목 인덱스를 역순으로 정렬하여 삭제 (인덱스 변동 방지)
                List<int> indicesToRemove = new List<int>();
                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    indicesToRemove.Add(lvi.Index);
                }
                indicesToRemove.Sort();
                indicesToRemove.Reverse();

                foreach (int index in indicesToRemove)
                {
                    if (index < osnapPoints.Count)
                    {
                        osnapPoints.RemoveAt(index);
                    }
                    if (index < osnapPointsWithNames.Count)
                    {
                        osnapPointsWithNames.RemoveAt(index);
                    }
                    lvOsnap.Items.RemoveAt(index);
                }

                // 번호 재정렬
                for (int i = 0; i < lvOsnap.Items.Count; i++)
                {
                    lvOsnap.Items[i].Text = (i + 1).ToString();
                }

                MessageBox.Show($"{indicesToRemove.Count}개의 좌표가 삭제되었습니다.", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 삭제 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 Osnap 좌표를 뷰어에서 표시 (심볼 숨김, 회전 중심만 설정)
        /// </summary>
        private void btnOsnapShowSelected_Click(object sender, EventArgs e)
        {
            if (lvOsnap.SelectedItems.Count == 0)
            {
                MessageBox.Show("Osnap 좌표를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 형상 제거 후 선택 좌표 표시
                vizcore3d.ShapeDrawing.Clear();

                // 선택된 좌표에 빨간 구 표시 + 카메라 이동
                List<VIZCore3D.NET.Data.Vertex3D> spherePoints = new List<VIZCore3D.NET.Data.Vertex3D>();
                VIZCore3D.NET.Data.Vertex3D targetPoint = null;

                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    int index = lvi.Index;
                    if (index < osnapPoints.Count)
                    {
                        var pt = osnapPoints[index];
                        if (targetPoint == null) targetPoint = pt;
                        spherePoints.Add(pt);
                    }
                }

                // 빨간 구 마커 표시
                if (spherePoints.Count > 0)
                {
                    vizcore3d.ShapeDrawing.AddSphere(spherePoints, 0, System.Drawing.Color.Red, 5.0f, true);
                }

                // 선택된 좌표에 풍선(Note) 표시 - 좌표 + 홀사이즈 정보
                vizcore3d.Review.Note.Clear();
                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    int index = lvi.Index;
                    if (index >= osnapPoints.Count) continue;
                    var pt = osnapPoints[index];
                    string nodeName = lvi.SubItems.Count > 1 ? lvi.SubItems[1].Text : "";
                    string holeSize = lvi.SubItems.Count > 5 ? lvi.SubItems[5].Text : "";

                    // 풍선 텍스트: 부재명 + 좌표 + 홀사이즈
                    string balloonText = $"{nodeName}\n({pt.X:F1}, {pt.Y:F1}, {pt.Z:F1})";
                    if (!string.IsNullOrEmpty(holeSize))
                        balloonText += $"\n{holeSize}";

                    // 텍스트 위치: 좌표에서 약간 오프셋
                    VIZCore3D.NET.Data.Vertex3D arrowPos = pt;
                    VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(
                        pt.X + 30f, pt.Y + 30f, pt.Z + 30f);

                    VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                    style.UseSymbol = false;
                    style.BackgroudTransparent = true;
                    style.FontBold = true;
                    style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE10;
                    style.FontColor = Color.DarkBlue;
                    style.LineColor = Color.DarkBlue;
                    style.LineWidth = 1;
                    style.ArrowColor = Color.Red;
                    style.ArrowWidth = 3;

                    vizcore3d.Review.Note.AddNoteSurface(balloonText, textPos, arrowPos, style);
                }

                // 첫 번째 선택 좌표로 카메라 이동
                if (targetPoint != null)
                {
                    // 해당 좌표 근처의 노드를 찾아서 FlyTo
                    List<int> nearNodeIndices = new List<int>();
                    foreach (var bom in bomList)
                    {
                        if (targetPoint.X >= bom.MinX && targetPoint.X <= bom.MaxX &&
                            targetPoint.Y >= bom.MinY && targetPoint.Y <= bom.MaxY &&
                            targetPoint.Z >= bom.MinZ && targetPoint.Z <= bom.MaxZ)
                        {
                            nearNodeIndices.Add(bom.Index);
                        }
                    }

                    if (nearNodeIndices.Count > 0)
                    {
                        vizcore3d.View.FlyToObject3d(nearNodeIndices, 1.5f);
                    }
                    else
                    {
                        vizcore3d.View.SetPivotPosition(targetPoint);
                    }
                }

                vizcore3d.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 좌표 설정 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 풍선 지우기 버튼 - Note와 ShapeDrawing(구 마커) 모두 제거
        /// </summary>
        private void btnOsnapClearBalloon_Click(object sender, EventArgs e)
        {
            try
            {
                vizcore3d.Review.Note.Clear();
                vizcore3d.ShapeDrawing.Clear();
            }
            catch { }
        }

    }
}
