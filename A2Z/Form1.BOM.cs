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
        private void SetupBOMColumns()
        {
            lvBOM.Columns.Clear();
            lvBOM.Columns.Add("No.", 40);
            lvBOM.Columns.Add("부재 이름", 120);
            lvBOM.Columns.Add("각도", 60);
            lvBOM.Columns.Add("X_Center", 80);
            lvBOM.Columns.Add("Y_Center", 80);
            lvBOM.Columns.Add("Z_Center", 80);
            lvBOM.Columns.Add("X_Min", 70);
            lvBOM.Columns.Add("X_Max", 70);
            lvBOM.Columns.Add("Y_Min", 70);
            lvBOM.Columns.Add("Y_Max", 70);
            lvBOM.Columns.Add("Z_Min", 70);
            lvBOM.Columns.Add("Z_Max", 70);
            lvBOM.Columns.Add("원형", 50);
            lvBOM.Columns.Add("용도", 70);
            lvBOM.Columns.Add("홀사이즈", 100);
        }

        /// <summary>
        /// Body 인덱스 → 부모 Part 풀네임 매핑 구축
        /// Part 노드 리스트를 조회하여 각 Part의 하위 Body 인덱스를 매핑
        /// </summary>
        private void BuildBodyToPartNameMap()
        {
            bodyToPartNameMap.Clear();
            bodyToPartIndexMap.Clear();

            try
            {
                // Part 노드 가져오기
                List<VIZCore3D.NET.Data.Node> partNodes = vizcore3d.Object3D.GetPartialNode(false, true, false);
                if (partNodes == null || partNodes.Count == 0) return;

                // Body 노드 가져오기
                List<VIZCore3D.NET.Data.Node> bodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (bodyNodes == null || bodyNodes.Count == 0) return;

                // 각 Part 노드의 범위를 인덱스로 계산
                // Part 노드의 하위 Body를 찾기 위해 Part의 자식 Body를 매핑
                // Part 인덱스 기준으로 정렬하여 범위 매핑
                List<int> partIndices = new List<int>();
                Dictionary<int, string> partIndexToName = new Dictionary<int, string>();
                foreach (var part in partNodes)
                {
                    partIndices.Add(part.Index);
                    partIndexToName[part.Index] = part.NodeName;
                }
                partIndices.Sort();

                // 각 Body에 대해 가장 가까운 상위 Part를 찾기
                // Body 인덱스보다 작거나 같은 가장 큰 Part 인덱스가 부모
                foreach (var body in bodyNodes)
                {
                    int parentPartIndex = -1;
                    // 이진 탐색으로 body.Index보다 작거나 같은 최대 Part 인덱스 찾기
                    int lo = 0, hi = partIndices.Count - 1;
                    while (lo <= hi)
                    {
                        int mid = (lo + hi) / 2;
                        if (partIndices[mid] <= body.Index)
                        {
                            parentPartIndex = partIndices[mid];
                            lo = mid + 1;
                        }
                        else
                        {
                            hi = mid - 1;
                        }
                    }

                    if (parentPartIndex >= 0)
                    {
                        bodyToPartIndexMap[body.Index] = parentPartIndex;
                        if (partIndexToName.ContainsKey(parentPartIndex))
                        {
                            bodyToPartNameMap[body.Index] = partIndexToName[parentPartIndex];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BuildBodyToPartNameMap] 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Body 인덱스로부터 Part 풀네임 가져오기
        /// </summary>
        private string GetPartNameFromBodyIndex(int bodyIndex, string fallbackName)
        {
            if (bodyToPartNameMap.ContainsKey(bodyIndex))
                return bodyToPartNameMap[bodyIndex];
            return fallbackName;
        }

        /// <summary>
        /// 부재 정보 DataGridView 컬럼 설정
        /// </summary>
        private void SetupAttributeColumns()
        {
            dgvAttributes.Columns.Clear();
            dgvAttributes.Columns.Add("No", "No");
            dgvAttributes.Columns.Add("Key", "속성명 (Key)");
            dgvAttributes.Columns.Add("Value", "값 (Value)");
            dgvAttributes.Columns["No"].Width = 40;
            dgvAttributes.Columns["Key"].Width = 120;
            dgvAttributes.Columns["Value"].Width = 200;
            dgvAttributes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAttributes.Columns["No"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvAttributes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvAttributes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvAttributes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
            dgvAttributes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAttributes.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            dgvAttributes.EnableHeadersVisualStyles = false;
            dgvAttributes.ColumnHeadersHeight = 30;
            dgvAttributes.RowTemplate.Height = 24;
        }

        private void Vizcore3d_OnInitializedVIZCore3D(object sender, EventArgs e)
        {
            // 라이선스 설정
            VIZCore3D.NET.Data.LicenseResults result = vizcore3d.License.LicenseServer("127.0.0.1", 8901);

            if (result != VIZCore3D.NET.Data.LicenseResults.SUCCESS)
            {
                MessageBox.Show(string.Format("License Error: {0}", result), "라이선스 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 라이선스 자동 갱신 타이머 시작 (30분마다)
            StartLicenseRefreshTimer();

            vizcore3d.ToolbarDrawing2D.Visible = true;
            //vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

            // VIZCore3D 초기화 완료 후 간섭검사 이벤트 등록
            vizcore3d.Clash.OnClashTestFinishedEvent += Clash_OnClashTestFinishedEvent;

            // 3D 객체 선택 이벤트 등록 (부재 정보 탭용)
            vizcore3d.Object3D.OnObject3DSelected += Object3D_OnObject3DSelected;

            // 모서리(Edge) 데이터 생성 및 읽기 활성화 (파일 열기 전 설정 필요)
            vizcore3d.Model.GenerateEdgeData = true;
            vizcore3d.Model.LoadEdgeData = true;

        }

        /// <summary>
        /// 라이선스 자동 갱신 타이머 시작
        /// </summary>
        private void StartLicenseRefreshTimer()
        {
            licenseRefreshTimer = new System.Windows.Forms.Timer();
            licenseRefreshTimer.Interval = 30 * 60 * 1000; // 30분 (밀리초)
            licenseRefreshTimer.Tick += LicenseRefreshTimer_Tick;
            licenseRefreshTimer.Start();
        }

        /// <summary>
        /// 라이선스 갱신 타이머 이벤트
        /// </summary>
        private void LicenseRefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 라이선스 서버에 재연결하여 갱신
                VIZCore3D.NET.Data.LicenseResults result = vizcore3d.License.LicenseServer("127.0.0.1", 8901);

                if (result != VIZCore3D.NET.Data.LicenseResults.SUCCESS)
                {
                    // 갱신 실패 시 상태바나 로그에 표시 (MessageBox는 작업 방해할 수 있음)
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 실패: {result}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 성공");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 열기
        /// </summary>
        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "VIZCore3D 파일 (*.vizx;*.viz)|*.vizx;*.viz|VIZX 파일 (*.vizx)|*.vizx|VIZ 파일 (*.viz)|*.viz|모든 파일 (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.Title = "3D 모델 파일 열기";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                // 파일 존재 확인
                if (!System.IO.File.Exists(dlg.FileName))
                {
                    MessageBox.Show("선택한 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 파일 정보
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(dlg.FileName);
                string debugInfo = $"파일: {fileInfo.Name}\n크기: {fileInfo.Length / 1024.0:F2} KB\n확장자: {fileInfo.Extension}\n";

                // 기존 데이터 초기화
                bomList.Clear();
                clashList.Clear();
                osnapPoints.Clear();
                osnapPointsWithNames.Clear();
                chainDimensionList.Clear();
                xraySelectedNodeIndices.Clear();
                drawingSheetList.Clear();
                bodyToPartNameMap.Clear();
                _autoProcessOsnapSuccess = false;
                lvBOM.Items.Clear();
                lvClash.Items.Clear();
                lvDrawingSheet.Items.Clear();
                lvOsnap.Items.Clear();
                lvDimension.Items.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                vizcore3d.Review.Note.Clear();

                // 파일 열기
                bool result = vizcore3d.Model.Open(dlg.FileName);

                if (result)
                {
                    // 파일 경로 저장
                    currentFilePath = dlg.FileName;

                    // 뷰 맞추기
                    vizcore3d.View.FitToView();

                    // 전역 실루엣 엣지 활성화 및 색상 설정 (검정색) - 외곽선
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                    // Body → Part 이름 매핑 구축
                    BuildBodyToPartNameMap();
                }
                else
                {
                    MessageBox.Show($"파일 열기 실패\n\n{debugInfo}\n\n라이선스나 파일 형식을 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 열기 중 예외 발생:\n\n{ex.Message}", "예외 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 메인 치수 추출 버튼 - Clash 검사 → Osnap 수집 → 치수 추출 → 치수 표시
        /// </summary>
        private void btnMainDimension_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 파일을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 0. BOM 데이터 수집 (매번 재수집하여 현재 가시성 반영)
                CollectBOMData();
                if (bomList.Count == 0)
                {
                    MessageBox.Show("BOM 데이터를 수집할 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 1. Osnap 수집 (전체)
                bool osnapSuccess = CollectAllOsnap();

                // 2. 치수 추출 (Osnap이 있을 때만)
                if (osnapSuccess && osnapPointsWithNames.Count > 0)
                {
                    float tolerance = 0.5f;
                    List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(osnapPointsWithNames, tolerance);

                    chainDimensionList.Clear();

                    var xDimensions = AddChainDimensionByAxis(mergedPoints, "X", tolerance);
                    chainDimensionList.AddRange(xDimensions);

                    var yDimensions = AddChainDimensionByAxis(mergedPoints, "Y", tolerance);
                    chainDimensionList.AddRange(yDimensions);

                    var zDimensions = AddChainDimensionByAxis(mergedPoints, "Z", tolerance);
                    chainDimensionList.AddRange(zDimensions);

                    // ListView에 추가 및 치수 번호 설정
                    lvDimension.Items.Clear();
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

                    // 치수 자동 표시
                    ShowAllDimensions();
                }

                // 3. Clash 검사 (비동기 - 완료 이벤트에서 최종 알림)
                _autoProcessOsnapSuccess = osnapSuccess;
                DetectClash();
                // 알림은 Clash_OnClashTestFinishedEvent에서 한 번만 표시
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 추출 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AutoProcessModel → Clash 이벤트 간 상태 전달용 필드

        /// <summary>
        /// 전체 Osnap 수집 (내부 메서드)
        /// </summary>
        private bool CollectAllOsnap()
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();
            lvOsnap.Items.Clear();

            try
            {
                vizcore3d.Clash.ClearResultSymbol();
                List<VIZCore3D.NET.Data.Node> allBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);

                if (allBodyNodes == null || allBodyNodes.Count == 0)
                {
                    return false;
                }

                // 가시성 필터링: 프로그래밍 선택 또는 FromIndex().Visible
                List<VIZCore3D.NET.Data.Node> bodyNodes;
                if (xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    bodyNodes = allBodyNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
                }
                else
                {
                    bodyNodes = allBodyNodes.Where(n =>
                    {
                        var realNode = vizcore3d.Object3D.FromIndex(n.Index);
                        return realNode != null && realNode.Visible;
                    }).ToList();
                    if (bodyNodes.Count == 0) bodyNodes = allBodyNodes;
                }

                foreach (var node in bodyNodes)
                {
                    string partName = GetPartNameFromBodyIndex(node.Index, node.NodeName);
                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList = vizcore3d.Object3D.GetOsnapPoint(node.Index);

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        foreach (var osnap in osnapList)
                        {
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    if (osnap.Start != null)
                                    {
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, partName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, partName));
                                    }
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    // 곡면/원형: 치수에서 제외
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, partName));
                                    }
                                    break;
                            }
                        }
                    }
                }

                // ListView에 추가
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
                        // 홀사이즈/슬롯홀: 포인트 위치 기반으로 해당하는 것만 표시
                        var matchBom = bomList?.FirstOrDefault(b => b.Name == item.nodeName);
                        var sizes = GetHoleOrSlotForPoint(matchBom, item.point.X, item.point.Y, item.point.Z);
                        lvi.SubItems.Add(sizes.holeSize);
                        lvi.SubItems.Add(sizes.slotHoleSize);
                        lvOsnap.Items.Add(lvi);
                    }
                }

                return osnapPoints.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Osnap 수집 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// BOM 데이터 수집 (내부 메서드)
        /// </summary>
        private bool CollectBOMData()
        {
            bomList.Clear();
            lvBOM.Items.Clear();

            try
            {
                List<VIZCore3D.NET.Data.Node> allNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);

                if (allNodes == null || allNodes.Count == 0)
                {
                    return false;
                }

                // X-Ray 필터링: 프로그래밍 선택 → 수동 X-Ray → 전체
                List<VIZCore3D.NET.Data.Node> targetNodes;
                if (xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    targetNodes = allNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
                }
                else
                {
                    targetNodes = allNodes.Where(n =>
                    {
                        var realNode = vizcore3d.Object3D.FromIndex(n.Index);
                        return realNode != null && realNode.Visible;
                    }).ToList();
                    if (targetNodes.Count == 0) targetNodes = allNodes;
                }

                foreach (var node in targetNodes)
                {
                    BOMData bom = new BOMData();
                    bom.Name = GetPartNameFromBodyIndex(node.Index, node.NodeName);
                    bom.Index = node.Index;

                    List<int> nodeIndices = new List<int>();
                    nodeIndices.Add(node.Index);
                    VIZCore3D.NET.Data.BoundBox3D bbox = vizcore3d.Object3D.GetBoundBox(nodeIndices, false);

                    if (bbox != null)
                    {
                        bom.MinX = bbox.MinX;
                        bom.MinY = bbox.MinY;
                        bom.MinZ = bbox.MinZ;
                        bom.MaxX = bbox.MaxX;
                        bom.MaxY = bbox.MaxY;
                        bom.MaxZ = bbox.MaxZ;

                        bom.CenterX = (bbox.MinX + bbox.MaxX) / 2.0f;
                        bom.CenterY = (bbox.MinY + bbox.MaxY) / 2.0f;
                        bom.CenterZ = (bbox.MinZ + bbox.MaxZ) / 2.0f;
                    }

                    bom.RotationAngle = 0.0f;

                    // Osnap으로 원형(CIRCLE) 반지름 계산
                    bom.CircleRadius = 0f;
                    try
                    {
                        var osnapList = vizcore3d.Object3D.GetOsnapPoint(node.Index);
                        if (osnapList != null)
                        {
                            float maxRadius = 0f;
                            foreach (var osnap in osnapList)
                            {
                                if (osnap.Kind == VIZCore3D.NET.Data.OsnapKind.CIRCLE && osnap.Center != null && osnap.Start != null)
                                {
                                    float dx = osnap.Start.X - osnap.Center.X;
                                    float dy = osnap.Start.Y - osnap.Center.Y;
                                    float dz = osnap.Start.Z - osnap.Center.Z;
                                    float r = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                                    if (r > maxRadius) maxRadius = r;
                                }
                            }
                            bom.CircleRadius = maxRadius;
                        }
                    }
                    catch { }

                    // UDA PURPOSE 값 수집
                    bom.Purpose = "";
                    try
                    {
                        var udaKeys = vizcore3d.Object3D.UDA.Keys;
                        if (udaKeys != null)
                        {
                            foreach (string key in udaKeys)
                            {
                                if (key.Trim().ToUpper() == "PURPOSE")
                                {
                                    var val = vizcore3d.Object3D.UDA.FromIndex(node.Index, key);
                                    if (val != null && !string.IsNullOrEmpty(val.ToString()))
                                    {
                                        bom.Purpose = val.ToString().Trim();
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch { }

                    bomList.Add(bom);
                }

                // Z_Max 내림차순 정렬 (큰 값이 위로)
                bomList.Sort((a, b) => b.MaxZ.CompareTo(a.MaxZ));

                // 홀 검출 수행
                DetectHoles();

                // 정렬된 순서로 ListView 채우기 (No. 칼럼 포함)
                int bomNo = 1;
                foreach (var bom in bomList)
                {
                    ListViewItem lvi = new ListViewItem(bomNo.ToString());  // No. 칼럼
                    lvi.SubItems.Add(bom.Name);
                    lvi.SubItems.Add(bom.RotationAngle.ToString("F2"));
                    lvi.SubItems.Add(bom.CenterX.ToString("F2"));
                    lvi.SubItems.Add(bom.CenterY.ToString("F2"));
                    lvi.SubItems.Add(bom.CenterZ.ToString("F2"));
                    lvi.SubItems.Add(bom.MinX.ToString("F2"));
                    lvi.SubItems.Add(bom.MaxX.ToString("F2"));
                    lvi.SubItems.Add(bom.MinY.ToString("F2"));
                    lvi.SubItems.Add(bom.MaxY.ToString("F2"));
                    lvi.SubItems.Add(bom.MinZ.ToString("F2"));
                    lvi.SubItems.Add(bom.MaxZ.ToString("F2"));
                    lvi.SubItems.Add(bom.CircleRadius > 0 ? bom.CircleRadius.ToString("F1") : "");
                    lvi.SubItems.Add(bom.Purpose);
                    lvi.SubItems.Add(bom.HoleSize);
                    lvi.Tag = bom;
                    lvBOM.Items.Add(lvi);
                    bomNo++;
                }

                return bomList.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BOM 수집 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 홀/슬롯홀 검출: GetNodeHoleInfo API를 사용하여 각 부재의 홀 정보를 추출
        /// CIRCLE 유형은 원형 홀, SLOT_HOLE 유형은 슬롯홀로 분류
        /// </summary>
        private void DetectHoles(float tolerance = 1.0f)
        {
            try
            {
                // 모든 BOM 항목의 홀 정보 초기화
                foreach (var bom in bomList)
                {
                    bom.Holes.Clear();
                    bom.HoleSize = "";
                    bom.SlotHoles.Clear();
                    bom.SlotHoleSize = "";
                }

                // GetNodeHoleInfo API를 사용하여 홀/슬롯홀 검출
                foreach (var bom in bomList)
                {
                    try
                    {
                        var holeItems = vizcore3d.GeometryUtility.GetNodeHoleInfo(bom.Index);
                        if (holeItems == null || holeItems.Count == 0) continue;

                        foreach (var item in holeItems)
                        {
                            if ((int)item.HoleType == 0) // CIRCLE
                            {
                                // 원형 홀
                                bom.Holes.Add(new HoleInfo
                                {
                                    Diameter = item.Radius * 2f,
                                    CenterX = item.Center.X,
                                    CenterY = item.Center.Y,
                                    CenterZ = item.Center.Z,
                                    CylinderBodyIndex = -1
                                });
                            }
                            else if ((int)item.HoleType == 1) // SLOT_HOLE
                            {
                                // 슬롯홀: Depth = ThicknessCenterFrom/To 사이 거리
                                float depth = 0f;
                                if (item.ThicknessCenterFrom != null && item.ThicknessCenterTo != null)
                                {
                                    float dx = item.ThicknessCenterTo.X - item.ThicknessCenterFrom.X;
                                    float dy = item.ThicknessCenterTo.Y - item.ThicknessCenterFrom.Y;
                                    float dz = item.ThicknessCenterTo.Z - item.ThicknessCenterFrom.Z;
                                    depth = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                                }

                                // SlotLength: Size에서 가장 큰 값 - 2*Radius (전체 길이에서 양쪽 반원 제거)
                                float slotLength = 0f;
                                if (item.Size != null)
                                {
                                    float maxDim = Math.Max(Math.Abs(item.Size.X),
                                        Math.Max(Math.Abs(item.Size.Y), Math.Abs(item.Size.Z)));
                                    slotLength = maxDim - item.Radius * 2f;
                                    if (slotLength < 0) slotLength = maxDim; // fallback
                                }

                                // Depth가 0이면 Size에서 가장 작은 비-0 값 사용
                                if (depth < 0.01f && item.Size != null)
                                {
                                    float[] dims = { Math.Abs(item.Size.X), Math.Abs(item.Size.Y), Math.Abs(item.Size.Z) };
                                    Array.Sort(dims);
                                    depth = dims[0] > 0.01f ? dims[0] : dims[1];
                                }

                                bom.SlotHoles.Add(new SlotHoleInfo
                                {
                                    Radius = item.Radius,
                                    SlotLength = slotLength,
                                    Depth = depth,
                                    CenterX = item.Center.X,
                                    CenterY = item.Center.Y,
                                    CenterZ = item.Center.Z
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetNodeHoleInfo 오류 (Index: {bom.Index}): {ex.Message}");
                    }
                }

                // 홀 중복 제거: 같은 직경 + 같은 중심 좌표의 홀은 1개로 카운팅
                foreach (var bom in bomList)
                {
                    if (bom.Holes.Count <= 1) continue;
                    var deduped = new List<HoleInfo>();
                    foreach (var hole in bom.Holes)
                    {
                        bool isDuplicate = false;
                        foreach (var existing in deduped)
                        {
                            if (Math.Abs(Math.Round(hole.Diameter, 1) - Math.Round(existing.Diameter, 1)) > 0.1) continue;
                            float dx = Math.Abs(hole.CenterX - existing.CenterX);
                            float dy = Math.Abs(hole.CenterY - existing.CenterY);
                            float dz = Math.Abs(hole.CenterZ - existing.CenterZ);
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                            if (dist < tolerance * 2) { isDuplicate = true; break; }
                        }
                        if (!isDuplicate) deduped.Add(hole);
                    }
                    bom.Holes = deduped;
                }

                // 슬롯홀 중복 제거
                foreach (var bom in bomList)
                {
                    if (bom.SlotHoles.Count <= 1) continue;
                    var dedupSlots = new List<SlotHoleInfo>();
                    foreach (var slot in bom.SlotHoles)
                    {
                        bool isDup = false;
                        foreach (var existing in dedupSlots)
                        {
                            float dist = (float)Math.Sqrt(
                                (slot.CenterX - existing.CenterX) * (slot.CenterX - existing.CenterX) +
                                (slot.CenterY - existing.CenterY) * (slot.CenterY - existing.CenterY) +
                                (slot.CenterZ - existing.CenterZ) * (slot.CenterZ - existing.CenterZ));
                            if (dist < tolerance * 2 &&
                                Math.Abs(slot.Radius - existing.Radius) < tolerance &&
                                Math.Abs(slot.SlotLength - existing.SlotLength) < tolerance)
                            {
                                isDup = true;
                                break;
                            }
                        }
                        if (!isDup) dedupSlots.Add(slot);
                    }
                    bom.SlotHoles = dedupSlots;
                }

                // 슬롯홀 위치와 겹치는 일반 홀 제거
                foreach (var bom in bomList)
                {
                    if (bom.SlotHoles.Count > 0 && bom.Holes.Count > 0)
                    {
                        bom.Holes.RemoveAll(h =>
                        {
                            foreach (var slot in bom.SlotHoles)
                            {
                                float dist = (float)Math.Sqrt(
                                    (h.CenterX - slot.CenterX) * (h.CenterX - slot.CenterX) +
                                    (h.CenterY - slot.CenterY) * (h.CenterY - slot.CenterY) +
                                    (h.CenterZ - slot.CenterZ) * (h.CenterZ - slot.CenterZ));
                                if (dist < slot.SlotLength / 2f + slot.Radius + tolerance)
                                    return true;
                            }
                            return false;
                        });
                    }
                }

                // 슬롯홀 사이즈 문자열: 동일 스펙 그룹핑 → *N 표기
                foreach (var bom in bomList)
                {
                    if (bom.SlotHoles.Count > 0)
                    {
                        var slotGroups = bom.SlotHoles
                            .GroupBy(s => $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}")
                            .ToList();

                        var slotParts = new List<string>();
                        foreach (var grp in slotGroups)
                        {
                            var slot = grp.First();
                            float width = slot.Radius * 2f;
                            string spec = $"({width:F0}*{slot.SlotLength:F0}*{slot.Depth:F0})";
                            if (grp.Count() > 1)
                                slotParts.Add($"{spec}*{grp.Count()}");
                            else
                                slotParts.Add(spec);
                        }
                        bom.SlotHoleSize = string.Join(", ", slotParts);
                    }
                }

                // 홀사이즈 문자열 생성
                foreach (var bom in bomList)
                {
                    if (bom.Holes.Count > 0)
                    {
                        var uniqueDiameters = bom.Holes
                            .Select(h => Math.Round(h.Diameter, 1))
                            .Distinct()
                            .OrderBy(d => d)
                            .ToList();

                        List<string> holeParts = new List<string>();
                        foreach (var diam in uniqueDiameters)
                        {
                            int count = bom.Holes.Count(h => Math.Abs(Math.Round(h.Diameter, 1) - diam) < 0.1);
                            if (count > 1)
                                holeParts.Add($"\u00d8{diam:F1}x{count}");
                            else
                                holeParts.Add($"\u00d8{diam:F1}");
                        }
                        bom.HoleSize = string.Join(", ", holeParts);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"홀 검출 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Osnap 포인트가 홀/슬롯홀 중 어디에 가까운지 판별하여 해당 사이즈 문자열 반환
        /// 홀 근처면 (HoleSize, ""), 슬롯홀 근처면 ("", SlotHoleSize), 둘 다 아니면 ("", "")
        /// </summary>
        private (string holeSize, string slotHoleSize) GetHoleOrSlotForPoint(BOMData bom, float px, float py, float pz, float tolerance = 2.0f)
        {
            if (bom == null) return ("", "");

            // 가장 가까운 홀까지의 거리
            float minHoleDist = float.MaxValue;
            if (bom.Holes != null)
            {
                foreach (var hole in bom.Holes)
                {
                    float dist = (float)Math.Sqrt(
                        (px - hole.CenterX) * (px - hole.CenterX) +
                        (py - hole.CenterY) * (py - hole.CenterY) +
                        (pz - hole.CenterZ) * (pz - hole.CenterZ));
                    if (dist < minHoleDist) minHoleDist = dist;
                }
            }

            // 가장 가까운 슬롯홀까지의 거리
            float minSlotDist = float.MaxValue;
            if (bom.SlotHoles != null)
            {
                foreach (var slot in bom.SlotHoles)
                {
                    float dist = (float)Math.Sqrt(
                        (px - slot.CenterX) * (px - slot.CenterX) +
                        (py - slot.CenterY) * (py - slot.CenterY) +
                        (pz - slot.CenterZ) * (pz - slot.CenterZ));
                    if (dist < minSlotDist) minSlotDist = dist;
                }
            }

            // 둘 다 멀면 빈값
            bool nearHole = minHoleDist < float.MaxValue;
            bool nearSlot = minSlotDist < float.MaxValue;

            if (!nearHole && !nearSlot) return ("", "");
            if (nearHole && !nearSlot) return (bom.HoleSize, "");
            if (!nearHole && nearSlot) return ("", bom.SlotHoleSize);

            // 둘 다 존재 → 더 가까운 쪽만 표시
            if (minHoleDist <= minSlotDist)
                return (bom.HoleSize, "");
            else
                return ("", bom.SlotHoleSize);
        }

        /// <summary>
        /// BOM 데이터 수집 버튼
        /// </summary>
        private void btnCollectBOM_Click(object sender, EventArgs e)
        {
            bool success = CollectBOMData();
            if (success)
            {
                MessageBox.Show(string.Format("BOM 데이터 {0}개 수집 완료", bomList.Count), "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("로드된 모델이 없거나 BOM 수집에 실패했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
