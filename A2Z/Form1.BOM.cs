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
        /// 홀 검출: 원기둥 Body의 높이가 다른 부재의 두께와 일치하면 홀로 판별
        /// GetCircleData를 사용하여 원형 데이터(지름, 중심)를 얻고,
        /// 원기둥의 높이와 부재 두께를 비교
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

                // 원기둥 vs 판재 분류: CircleRadius > 0이라도 바운딩박스 형태가 원기둥이 아니면 판재로 분류
                // (Angle 등 구조 부재가 홀 Osnap 때문에 CircleRadius > 0이 되는 경우 대응)
                List<BOMData> cylinderBodies = new List<BOMData>();
                List<BOMData> plateBodies = new List<BOMData>();

                foreach (var b in bomList)
                {
                    if (b.CircleRadius <= 0)
                    {
                        plateBodies.Add(b);
                        continue;
                    }

                    // 바운딩박스 치수 중 지름(2*CircleRadius)과 유사한 축이 2개 이상이면 원기둥
                    float diameter = b.CircleRadius * 2f;
                    float sizeX = Math.Abs(b.MaxX - b.MinX);
                    float sizeY = Math.Abs(b.MaxY - b.MinY);
                    float sizeZ = Math.Abs(b.MaxZ - b.MinZ);
                    float cylTol = Math.Max(tolerance * 2f, diameter * 0.2f); // 20% 오차 허용

                    int matchCount = 0;
                    if (Math.Abs(sizeX - diameter) < cylTol) matchCount++;
                    if (Math.Abs(sizeY - diameter) < cylTol) matchCount++;
                    if (Math.Abs(sizeZ - diameter) < cylTol) matchCount++;

                    if (matchCount >= 2)
                        cylinderBodies.Add(b); // 실제 원기둥 형태
                    else
                        plateBodies.Add(b); // Angle 등 원형 특성만 있는 구조 부재
                }

                if (plateBodies.Count == 0) return;

                if (cylinderBodies.Count > 0)
                foreach (var cylinder in cylinderBodies)
                {
                    // 원기둥의 바운딩 박스 크기
                    float cylSizeX = Math.Abs(cylinder.MaxX - cylinder.MinX);
                    float cylSizeY = Math.Abs(cylinder.MaxY - cylinder.MinY);
                    float cylSizeZ = Math.Abs(cylinder.MaxZ - cylinder.MinZ);

                    // 원기둥 높이 = 바운딩 박스의 최소 치수 (지름 방향이 아닌 축 방향)
                    // 원기둥은 지름 방향 2개가 비슷하고, 높이 방향이 다름
                    float[] cylDims = { cylSizeX, cylSizeY, cylSizeZ };
                    Array.Sort(cylDims); // 오름차순: [가장 작은, 중간, 가장 큰]

                    // 원기둥의 높이 = 가장 작은 값 (얇은 원기둥 = 홀)
                    // 단, 두 값이 비슷하면 (지름 방향) 나머지가 높이
                    float cylHeight;
                    string cylAxis; // 높이 축
                    if (Math.Abs(cylSizeX - cylSizeY) < tolerance && cylSizeZ < Math.Max(cylSizeX, cylSizeY))
                    {
                        cylHeight = cylSizeZ;
                        cylAxis = "Z";
                    }
                    else if (Math.Abs(cylSizeX - cylSizeZ) < tolerance && cylSizeY < Math.Max(cylSizeX, cylSizeZ))
                    {
                        cylHeight = cylSizeY;
                        cylAxis = "Y";
                    }
                    else if (Math.Abs(cylSizeY - cylSizeZ) < tolerance && cylSizeX < Math.Max(cylSizeY, cylSizeZ))
                    {
                        cylHeight = cylSizeX;
                        cylAxis = "X";
                    }
                    else
                    {
                        // 최소값을 높이로 간주
                        cylHeight = cylDims[0];
                        cylAxis = cylSizeX == cylDims[0] ? "X" : (cylSizeY == cylDims[0] ? "Y" : "Z");
                    }

                    float cylDiameter = cylinder.CircleRadius * 2f;

                    // 원기둥 중심
                    float cylCx = cylinder.CenterX;
                    float cylCy = cylinder.CenterY;
                    float cylCz = cylinder.CenterZ;

                    // GetCircleData로 더 정확한 지름과 중심 가져오기
                    try
                    {
                        var circleDataList = vizcore3d.GeometryUtility.GetCircleData(cylinder.Index);
                        if (circleDataList != null && circleDataList.Count > 0)
                        {
                            // 가장 큰 원형의 지름 사용
                            float maxDiam = 0f;
                            VIZCore3D.NET.Data.Vertex3D bestCenter = null;
                            foreach (var cd in circleDataList)
                            {
                                if (cd.Diameter > maxDiam)
                                {
                                    maxDiam = cd.Diameter;
                                    bestCenter = cd.Center;
                                }
                            }
                            if (maxDiam > 0) cylDiameter = maxDiam;
                            if (bestCenter != null)
                            {
                                cylCx = bestCenter.X;
                                cylCy = bestCenter.Y;
                                cylCz = bestCenter.Z;
                            }

                            // 원형이 2개이면 두 중심 사이 거리로 높이 재계산
                            if (circleDataList.Count >= 2)
                            {
                                var c1 = circleDataList[0].Center;
                                var c2 = circleDataList[1].Center;
                                float dist = (float)Math.Sqrt(
                                    (c2.X - c1.X) * (c2.X - c1.X) +
                                    (c2.Y - c1.Y) * (c2.Y - c1.Y) +
                                    (c2.Z - c1.Z) * (c2.Z - c1.Z));
                                if (dist > 0.1f) cylHeight = dist;

                                // 홀 중심 = 두 원의 중점
                                cylCx = (c1.X + c2.X) / 2f;
                                cylCy = (c1.Y + c2.Y) / 2f;
                                cylCz = (c1.Z + c2.Z) / 2f;
                            }
                        }
                    }
                    catch { }

                    // 각 판재 부재에 대해 홀 여부 검사 - 가장 작은(가까운) bbox에 할당
                    BOMData bestPlate = null;
                    float bestVolume = float.MaxValue;

                    foreach (var plate in plateBodies)
                    {
                        // 판재 두께 = 바운딩 박스의 최소 치수
                        float plateSizeX = Math.Abs(plate.MaxX - plate.MinX);
                        float plateSizeY = Math.Abs(plate.MaxY - plate.MinY);
                        float plateSizeZ = Math.Abs(plate.MaxZ - plate.MinZ);
                        float plateThickness = Math.Min(plateSizeX, Math.Min(plateSizeY, plateSizeZ));

                        // 원기둥 높이가 판재 최소 치수 이하인지 확인 (Angle 등 복합 단면 지원)
                        if (cylHeight > plateThickness + tolerance) continue;

                        // 원기둥 중심이 판재 바운딩 박스 안에 있는지 확인 (약간의 여유)
                        float margin = tolerance;
                        bool insideX = cylCx >= plate.MinX - margin && cylCx <= plate.MaxX + margin;
                        bool insideY = cylCy >= plate.MinY - margin && cylCy <= plate.MaxY + margin;
                        bool insideZ = cylCz >= plate.MinZ - margin && cylCz <= plate.MaxZ + margin;

                        if (insideX && insideY && insideZ)
                        {
                            // bbox 부피가 가장 작은 판재에 할당 (가장 근접한 부재)
                            float vol = plateSizeX * plateSizeY * plateSizeZ;
                            if (vol < bestVolume)
                            {
                                bestVolume = vol;
                                bestPlate = plate;
                            }
                        }
                    }

                    if (bestPlate != null)
                    {
                        bestPlate.Holes.Add(new HoleInfo
                        {
                            Diameter = cylDiameter,
                            CenterX = cylCx,
                            CenterY = cylCy,
                            CenterZ = cylCz,
                            CylinderBodyIndex = cylinder.Index
                        });
                    }
                }

                // --- 보조 홀 검출: 별도 원기둥 body가 없는 경우, Osnap CIRCLE로 판재 자체에서 홀 검출 ---
                // Osnap CIRCLE만 사용하여 완벽한 원기둥 형태의 홀만 인식 (곡면/필렛 제외)
                try
                {
                    foreach (var plate in plateBodies)
                    {
                        if (plate.Holes.Count > 0) continue; // 이미 원기둥 매칭으로 홀을 찾은 경우 스킵

                        var osnapList = vizcore3d.Object3D.GetOsnapPoint(plate.Index);
                        if (osnapList == null) continue;

                        // Osnap에서 CIRCLE 종류만 추출 (완벽한 원형만 해당)
                        var circles = new List<(float CenterX, float CenterY, float CenterZ, float Radius)>();
                        foreach (var osnap in osnapList)
                        {
                            if (osnap.Kind != VIZCore3D.NET.Data.OsnapKind.CIRCLE) continue;
                            if (osnap.Center == null || osnap.Start == null) continue;
                            float rdx = osnap.Start.X - osnap.Center.X;
                            float rdy = osnap.Start.Y - osnap.Center.Y;
                            float rdz = osnap.Start.Z - osnap.Center.Z;
                            float r = (float)Math.Sqrt(rdx * rdx + rdy * rdy + rdz * rdz);
                            if (r < 0.1f) continue; // 너무 작은 원 무시
                            circles.Add((osnap.Center.X, osnap.Center.Y, osnap.Center.Z, r));
                        }

                        if (circles.Count < 2) continue;

                        float plateSizeX = Math.Abs(plate.MaxX - plate.MinX);
                        float plateSizeY = Math.Abs(plate.MaxY - plate.MinY);
                        float plateSizeZ = Math.Abs(plate.MaxZ - plate.MinZ);
                        float plateMinDim = Math.Min(plateSizeX, Math.Min(plateSizeY, plateSizeZ));

                        // 같은 반지름의 동축 원형 쌍 = 홀
                        List<bool> used = new List<bool>(new bool[circles.Count]);
                        for (int i = 0; i < circles.Count; i++)
                        {
                            if (used[i]) continue;
                            var ci = circles[i];
                            for (int j = i + 1; j < circles.Count; j++)
                            {
                                if (used[j]) continue;
                                var cj = circles[j];

                                // 1) 같은 반지름 (= 같은 직경)
                                if (Math.Abs(ci.Radius - cj.Radius) > tolerance) continue;

                                float dx = Math.Abs(cj.CenterX - ci.CenterX);
                                float dy = Math.Abs(cj.CenterY - ci.CenterY);
                                float dz = Math.Abs(cj.CenterZ - ci.CenterZ);

                                // 2) 동축 검증: 정확히 1개 축만 의미 있는 거리, 나머지 2축은 거의 일치
                                float axisTol = tolerance;
                                int significantAxes = 0;
                                float holeDepth = 0f;
                                if (dx > axisTol) { significantAxes++; holeDepth = dx; }
                                if (dy > axisTol) { significantAxes++; holeDepth = dy; }
                                if (dz > axisTol) { significantAxes++; holeDepth = dz; }

                                if (significantAxes != 1) continue; // 정확히 1축만 떨어져야 동축

                                // 3) 홀 깊이가 판재 최소 치수 이하
                                if (holeDepth > plateMinDim + tolerance) continue;

                                // 4) 홀 중심 = 두 원의 중점
                                float hcx = (ci.CenterX + cj.CenterX) / 2f;
                                float hcy = (ci.CenterY + cj.CenterY) / 2f;
                                float hcz = (ci.CenterZ + cj.CenterZ) / 2f;

                                // 5) 중심이 판재 바운딩 박스 안에 있는지 확인
                                float m = tolerance;
                                if (hcx >= plate.MinX - m && hcx <= plate.MaxX + m &&
                                    hcy >= plate.MinY - m && hcy <= plate.MaxY + m &&
                                    hcz >= plate.MinZ - m && hcz <= plate.MaxZ + m)
                                {
                                    // 6) 홀 축 방향 결정
                                    string holeAxis;
                                    if (dx > axisTol) holeAxis = "X";
                                    else if (dy > axisTol) holeAxis = "Y";
                                    else holeAxis = "Z";

                                    // 7) 완전한 원형 검증: Osnap 포인트 8개 이상이 원을 따라 360° 분포해야 함
                                    if (!IsCompleteCircle(osnapList, hcx, hcy, hcz, ci.Radius, holeAxis, holeDepth, tolerance))
                                        continue;

                                    plate.Holes.Add(new HoleInfo
                                    {
                                        Diameter = ci.Radius * 2f,
                                        CenterX = hcx,
                                        CenterY = hcy,
                                        CenterZ = hcz,
                                        CylinderBodyIndex = -1
                                    });
                                    used[i] = true;
                                    used[j] = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 홀 중복 제거: 같은 직경 + 같은 중심 좌표(3축 모두 근접)의 홀은 1개로 카운팅
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
                            // 중심 간 거리가 허용 오차 이내면 동일 홀 (3축 모두 근접)
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                            if (dist < tolerance * 2) { isDuplicate = true; break; }
                        }
                        if (!isDuplicate) deduped.Add(hole);
                    }
                    bom.Holes = deduped;
                }

                // --- 슬롯홀 검출: 반원기둥 2개 + 사각기둥 1개 구조 검증 ---
                // 홀로 확정된 Osnap은 제외, 관통 검증 + LINE 사각기둥 검증
                try
                {
                    foreach (var plate in plateBodies)
                    {
                        var slotOsnapList = vizcore3d.Object3D.GetOsnapPoint(plate.Index);
                        if (slotOsnapList == null) continue;

                        // 판재 두께 계산 (홀 관통 검증용)
                        float plateSizeX = Math.Abs(plate.MaxX - plate.MinX);
                        float plateSizeY = Math.Abs(plate.MaxY - plate.MinY);
                        float plateSizeZ = Math.Abs(plate.MaxZ - plate.MinZ);
                        float plateMinDim = Math.Min(plateSizeX, Math.Min(plateSizeY, plateSizeZ));

                        // ★ 이미 확정된 홀 중심 목록 (이 원들은 슬롯홀 후보에서 제외)
                        var confirmedHoles = plate.Holes;

                        // CIRCLE Osnap 수집 - 확정 홀 근처 원은 제외
                        var slotCircles = new List<(float CX, float CY, float CZ, float R)>();
                        foreach (var osnap in slotOsnapList)
                        {
                            if (osnap.Kind != VIZCore3D.NET.Data.OsnapKind.CIRCLE) continue;
                            if (osnap.Center == null || osnap.Start == null) continue;
                            float rdx = osnap.Start.X - osnap.Center.X;
                            float rdy = osnap.Start.Y - osnap.Center.Y;
                            float rdz = osnap.Start.Z - osnap.Center.Z;
                            float r = (float)Math.Sqrt(rdx * rdx + rdy * rdy + rdz * rdz);
                            if (r < 0.1f) continue;

                            // ★ 확정된 홀 중심 근처의 원은 슬롯홀 후보에서 제외
                            bool nearHole = false;
                            foreach (var hole in confirmedHoles)
                            {
                                float dist = (float)Math.Sqrt(
                                    (osnap.Center.X - hole.CenterX) * (osnap.Center.X - hole.CenterX) +
                                    (osnap.Center.Y - hole.CenterY) * (osnap.Center.Y - hole.CenterY) +
                                    (osnap.Center.Z - hole.CenterZ) * (osnap.Center.Z - hole.CenterZ));
                                if (dist < hole.Diameter / 2f + tolerance)
                                {
                                    nearHole = true;
                                    break;
                                }
                            }
                            if (nearHole) continue;

                            slotCircles.Add((osnap.Center.X, osnap.Center.Y, osnap.Center.Z, r));
                        }

                        if (slotCircles.Count < 4) continue;

                        // LINE Osnap 수집 (사각기둥 직선 변 검증용)
                        var slotLines = new List<(float SX, float SY, float SZ, float EX, float EY, float EZ)>();
                        foreach (var osnap in slotOsnapList)
                        {
                            if (osnap.Kind != VIZCore3D.NET.Data.OsnapKind.LINE) continue;
                            if (osnap.Start == null || osnap.End == null) continue;
                            slotLines.Add((osnap.Start.X, osnap.Start.Y, osnap.Start.Z,
                                           osnap.End.X, osnap.End.Y, osnap.End.Z));
                        }

                        // 동축 쌍 검색 (같은 반지름, 1축만 차이) + 관통 검증
                        var coaxialPairs = new List<(float radius, float cx, float cy, float cz, string axis, float depth)>();
                        var pairCircleIndices = new List<(int ci, int cj)>();
                        for (int i = 0; i < slotCircles.Count; i++)
                        {
                            for (int j = i + 1; j < slotCircles.Count; j++)
                            {
                                if (Math.Abs(slotCircles[i].R - slotCircles[j].R) > tolerance) continue;
                                float ddx = Math.Abs(slotCircles[j].CX - slotCircles[i].CX);
                                float ddy = Math.Abs(slotCircles[j].CY - slotCircles[i].CY);
                                float ddz = Math.Abs(slotCircles[j].CZ - slotCircles[i].CZ);
                                int sigAxes = 0;
                                float depth = 0f;
                                string axis = "";
                                if (ddx > tolerance) { sigAxes++; depth = ddx; axis = "X"; }
                                if (ddy > tolerance) { sigAxes++; depth = ddy; axis = "Y"; }
                                if (ddz > tolerance) { sigAxes++; depth = ddz; axis = "Z"; }
                                if (sigAxes != 1) continue;

                                // 관통 검증: 깊이가 판재 두께 범위 내
                                if (depth > plateMinDim + tolerance) continue;
                                if (depth < plateMinDim * 0.5f) continue;

                                float pcx = (slotCircles[i].CX + slotCircles[j].CX) / 2f;
                                float pcy = (slotCircles[i].CY + slotCircles[j].CY) / 2f;
                                float pcz = (slotCircles[i].CZ + slotCircles[j].CZ) / 2f;
                                coaxialPairs.Add((slotCircles[i].R, pcx, pcy, pcz, axis, depth));
                                pairCircleIndices.Add((i, j));
                            }
                        }

                        if (coaxialPairs.Count < 2) continue;

                        // 완전한 원형인 동축 쌍은 일반 홀이므로 슬롯홀 후보에서 제외
                        var slotCandidatePairs = new List<int>();
                        for (int pi = 0; pi < coaxialPairs.Count; pi++)
                        {
                            var pair = coaxialPairs[pi];
                            var indices = pairCircleIndices[pi];
                            var c1 = slotCircles[indices.ci];
                            var c2 = slotCircles[indices.cj];

                            bool isCircle1Complete = IsCompleteCircle(slotOsnapList, c1.CX, c1.CY, c1.CZ, c1.R, pair.axis, pair.depth, tolerance);
                            bool isCircle2Complete = IsCompleteCircle(slotOsnapList, c2.CX, c2.CY, c2.CZ, c2.R, pair.axis, pair.depth, tolerance);

                            if (!isCircle1Complete && !isCircle2Complete)
                            {
                                slotCandidatePairs.Add(pi);
                            }
                        }

                        if (slotCandidatePairs.Count < 2) continue;

                        // 슬롯홀 감지: 같은 반지름 + 같은 축 + 횡방향 오프셋 + 사각기둥 검증
                        // ★ 각 원(circle index)은 한 번만 사용 (1슬롯홀 = 반원기둥2 + 사각기둥1)
                        var usedCircleIdx = new HashSet<int>();
                        var usedPairIdx = new HashSet<int>();
                        for (int pi = 0; pi < slotCandidatePairs.Count; pi++)
                        {
                            int p = slotCandidatePairs[pi];
                            if (usedPairIdx.Contains(p)) continue;
                            var pIndices = pairCircleIndices[p];
                            if (usedCircleIdx.Contains(pIndices.ci) || usedCircleIdx.Contains(pIndices.cj)) continue;

                            for (int qi = pi + 1; qi < slotCandidatePairs.Count; qi++)
                            {
                                int q = slotCandidatePairs[qi];
                                if (usedPairIdx.Contains(q)) continue;
                                var qIndices = pairCircleIndices[q];
                                if (usedCircleIdx.Contains(qIndices.ci) || usedCircleIdx.Contains(qIndices.cj)) continue;

                                if (Math.Abs(coaxialPairs[p].radius - coaxialPairs[q].radius) > tolerance) continue;
                                if (coaxialPairs[p].axis != coaxialPairs[q].axis) continue;
                                if (Math.Abs(coaxialPairs[p].depth - coaxialPairs[q].depth) > tolerance) continue;

                                // 횡방향 거리 계산
                                float lateralDist;
                                switch (coaxialPairs[p].axis)
                                {
                                    case "X":
                                        lateralDist = (float)Math.Sqrt(
                                            (coaxialPairs[q].cy - coaxialPairs[p].cy) * (coaxialPairs[q].cy - coaxialPairs[p].cy) +
                                            (coaxialPairs[q].cz - coaxialPairs[p].cz) * (coaxialPairs[q].cz - coaxialPairs[p].cz));
                                        break;
                                    case "Y":
                                        lateralDist = (float)Math.Sqrt(
                                            (coaxialPairs[q].cx - coaxialPairs[p].cx) * (coaxialPairs[q].cx - coaxialPairs[p].cx) +
                                            (coaxialPairs[q].cz - coaxialPairs[p].cz) * (coaxialPairs[q].cz - coaxialPairs[p].cz));
                                        break;
                                    default: // Z
                                        lateralDist = (float)Math.Sqrt(
                                            (coaxialPairs[q].cx - coaxialPairs[p].cx) * (coaxialPairs[q].cx - coaxialPairs[p].cx) +
                                            (coaxialPairs[q].cy - coaxialPairs[p].cy) * (coaxialPairs[q].cy - coaxialPairs[p].cy));
                                        break;
                                }

                                if (lateralDist < tolerance) continue;
                                if (lateralDist > coaxialPairs[p].radius * 5f) continue;

                                // 사각기둥 검증: LINE Osnap이 반원기둥 사이에 있는지
                                if (!HasSlotConnectingLines(slotLines, coaxialPairs[p], coaxialPairs[q], lateralDist, tolerance))
                                    continue;

                                float slotCx = (coaxialPairs[p].cx + coaxialPairs[q].cx) / 2f;
                                float slotCy = (coaxialPairs[p].cy + coaxialPairs[q].cy) / 2f;
                                float slotCz = (coaxialPairs[p].cz + coaxialPairs[q].cz) / 2f;

                                plate.SlotHoles.Add(new SlotHoleInfo
                                {
                                    Radius = coaxialPairs[p].radius,
                                    SlotLength = lateralDist,
                                    Depth = coaxialPairs[p].depth,
                                    CenterX = slotCx,
                                    CenterY = slotCy,
                                    CenterZ = slotCz
                                });

                                // ★ 사용된 원과 쌍 모두 마킹 (재사용 방지)
                                usedPairIdx.Add(p);
                                usedPairIdx.Add(q);
                                usedCircleIdx.Add(pIndices.ci);
                                usedCircleIdx.Add(pIndices.cj);
                                usedCircleIdx.Add(qIndices.ci);
                                usedCircleIdx.Add(qIndices.cj);
                                break;
                            }
                        }

                        // 슬롯홀 중복 제거: 같은 위치(중심 근접)의 슬롯홀은 1개로
                        if (plate.SlotHoles.Count > 1)
                        {
                            var dedupSlots = new List<SlotHoleInfo>();
                            foreach (var slot in plate.SlotHoles)
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
                            plate.SlotHoles = dedupSlots;
                        }

                        // 슬롯홀 위치와 겹치는 일반 홀 제거
                        if (plate.SlotHoles.Count > 0 && plate.Holes.Count > 0)
                        {
                            plate.Holes.RemoveAll(h =>
                            {
                                foreach (var slot in plate.SlotHoles)
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

                        // ★ 슬롯홀 사이즈 문자열: 동일 스펙 그룹핑 → *N 표기
                        if (plate.SlotHoles.Count > 0)
                        {
                            // 동일 스펙(반지름+길이+깊이) 기준 그룹핑
                            var slotGroups = plate.SlotHoles
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
                            plate.SlotHoleSize = string.Join(", ", slotParts);
                        }
                    }
                }
                catch { }

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
        /// 원형 완전성 검증: Osnap 포인트가 원 둘레를 따라 8개 이상 규칙적으로 분포하는지 확인
        /// 모서리 라운드/필렛은 부분 호(90° 등)만 커버하므로 걸러짐
        /// </summary>
        private bool IsCompleteCircle(
            List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList,
            float cx, float cy, float cz, float radius,
            string axis, float depth, float tolerance)
        {
            float radTol = Math.Max(tolerance, radius * 0.15f);
            float halfDepth = depth / 2f + tolerance;
            var angles = new List<double>();

            foreach (var osnap in osnapList)
            {
                // Start, End 포인트 수집
                var pointsToCheck = new List<VIZCore3D.NET.Data.Vertex3D>();
                if (osnap.Start != null) pointsToCheck.Add(osnap.Start);
                if (osnap.End != null) pointsToCheck.Add(osnap.End);

                foreach (var pt in pointsToCheck)
                {
                    float dist2D, distAxis;
                    double angle;

                    switch (axis)
                    {
                        case "X":
                            dist2D = (float)Math.Sqrt((pt.Y - cy) * (pt.Y - cy) + (pt.Z - cz) * (pt.Z - cz));
                            distAxis = Math.Abs(pt.X - cx);
                            angle = Math.Atan2(pt.Z - cz, pt.Y - cy);
                            break;
                        case "Y":
                            dist2D = (float)Math.Sqrt((pt.X - cx) * (pt.X - cx) + (pt.Z - cz) * (pt.Z - cz));
                            distAxis = Math.Abs(pt.Y - cy);
                            angle = Math.Atan2(pt.Z - cz, pt.X - cx);
                            break;
                        default: // "Z"
                            dist2D = (float)Math.Sqrt((pt.X - cx) * (pt.X - cx) + (pt.Y - cy) * (pt.Y - cy));
                            distAxis = Math.Abs(pt.Z - cz);
                            angle = Math.Atan2(pt.Y - cy, pt.X - cx);
                            break;
                    }

                    // 원 둘레 근처에 있고 홀 깊이 범위 안에 있는 포인트만 수집
                    if (Math.Abs(dist2D - radius) < radTol && distAxis <= halfDepth)
                    {
                        angles.Add(angle);
                    }
                }
            }

            // 최소 8개 포인트 필요
            if (angles.Count < 8) return false;

            // 각도 정렬 후 중복 제거 (0.01 rad ≈ 0.6° 이내는 동일 포인트)
            angles.Sort();
            var uniqueAngles = new List<double> { angles[0] };
            for (int k = 1; k < angles.Count; k++)
            {
                if (angles[k] - uniqueAngles[uniqueAngles.Count - 1] > 0.01)
                    uniqueAngles.Add(angles[k]);
            }

            if (uniqueAngles.Count < 8) return false;

            // 최대 각도 간격 계산 - 360° 전체를 커버하는지 확인
            double maxGap = 0;
            for (int k = 1; k < uniqueAngles.Count; k++)
                maxGap = Math.Max(maxGap, uniqueAngles[k] - uniqueAngles[k - 1]);
            // 처음과 마지막 사이 간격 (360° 감싸기)
            maxGap = Math.Max(maxGap, (2 * Math.PI) - uniqueAngles[uniqueAngles.Count - 1] + uniqueAngles[0]);

            // 최대 간격이 90° (π/2) 미만이어야 완전한 원형
            // 모서리 라운드는 90° 호만 커버하므로 나머지 270°가 빈 간격 → 걸러짐
            return maxGap < Math.PI / 2.0;
        }

        /// <summary>
        /// 슬롯홀 사각기둥 검증: 두 반원기둥 쌍 사이에 직선(LINE) Osnap이 존재하는지 확인
        /// 슬롯홀 = 반원기둥 2개 + 사각기둥 1개 구조이며, 사각기둥의 직선 변이
        /// LINE Osnap으로 검출되어야 진짜 슬롯홀로 인정
        /// </summary>
        private bool HasSlotConnectingLines(
            List<(float SX, float SY, float SZ, float EX, float EY, float EZ)> lines,
            (float radius, float cx, float cy, float cz, string axis, float depth) pair1,
            (float radius, float cx, float cy, float cz, string axis, float depth) pair2,
            float lateralDist, float tolerance)
        {
            // LINE Osnap이 없으면 사각기둥 구조가 아님 → 슬롯홀 아님
            if (lines.Count == 0) return false;

            // 슬롯 방향 벡터 (pair1 → pair2, 깊이 축 제외한 횡방향)
            float dirX = pair2.cx - pair1.cx;
            float dirY = pair2.cy - pair1.cy;
            float dirZ = pair2.cz - pair1.cz;
            float dirLen = (float)Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
            if (dirLen < 0.001f) return false;
            dirX /= dirLen; dirY /= dirLen; dirZ /= dirLen;

            // 슬롯 중심
            float midX = (pair1.cx + pair2.cx) / 2f;
            float midY = (pair1.cy + pair2.cy) / 2f;
            float midZ = (pair1.cz + pair2.cz) / 2f;

            int connectingLineCount = 0;
            float lineLenTol = lateralDist * 0.5f; // 길이 50% 허용 오차

            foreach (var line in lines)
            {
                // 직선의 방향 벡터
                float lx = line.EX - line.SX;
                float ly = line.EY - line.SY;
                float lz = line.EZ - line.SZ;
                float lineLen = (float)Math.Sqrt(lx * lx + ly * ly + lz * lz);
                if (lineLen < 0.1f) continue;

                // 직선 길이가 lateralDist와 유사한지 확인
                if (Math.Abs(lineLen - lateralDist) > lineLenTol) continue;

                // 직선 방향이 슬롯 방향과 평행한지 확인 (내적 절대값 ≈ 1)
                float lnx = lx / lineLen, lny = ly / lineLen, lnz = lz / lineLen;
                float dot = Math.Abs(lnx * dirX + lny * dirY + lnz * dirZ);
                if (dot < 0.8f) continue; // 약 37° 이내

                // 직선 중점이 슬롯 중심 근처에 있는지 확인
                float lmx = (line.SX + line.EX) / 2f;
                float lmy = (line.SY + line.EY) / 2f;
                float lmz = (line.SZ + line.EZ) / 2f;
                float distToMid = (float)Math.Sqrt(
                    (lmx - midX) * (lmx - midX) +
                    (lmy - midY) * (lmy - midY) +
                    (lmz - midZ) * (lmz - midZ));

                // 직선 중점이 슬롯 영역 안에 있어야 (반지름 + 깊이 범위)
                float maxDist = pair1.radius + pair1.depth + tolerance;
                if (distToMid > maxDist) continue;

                connectingLineCount++;
            }

            // 최소 2개의 연결 직선 필요 (상면/하면 각 1개, 또는 양쪽 각 1개)
            return connectingLineCount >= 2;
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
