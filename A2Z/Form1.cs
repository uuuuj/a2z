using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VIZCore3D.NET;


namespace A2Z
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// VIZCore3D.NET 컨트롤
        /// </summary>
        private VIZCore3D.NET.VIZCore3DControl vizcore3d;

        /// <summary>
        /// BOM 데이터 리스트
        /// </summary>
        private List<BOMData> bomList = new List<BOMData>();

        /// <summary>
        /// Clash 데이터 리스트
        /// </summary>
        private List<ClashData> clashList = new List<ClashData>();

        /// <summary>
        /// Osnap 좌표 리스트
        /// </summary>
        private List<VIZCore3D.NET.Data.Vertex3D> osnapPoints = new List<VIZCore3D.NET.Data.Vertex3D>();

        /// <summary>
        /// Osnap 좌표와 부재 이름 리스트
        /// </summary>
        private List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> osnapPointsWithNames = new List<(VIZCore3D.NET.Data.Vertex3D, string)>();

        /// <summary>
        /// X-Ray 모드에서 선택된 노드 인덱스 리스트 (Clash 선택 항목만 보기에서 사용)
        /// </summary>
        private List<int> xraySelectedNodeIndices = new List<int>();

        /// <summary>
        /// 현재 열린 파일 경로
        /// </summary>
        private string currentFilePath = "";

        /// <summary>
        /// 마지막으로 생성된 2D 도면 이미지
        /// </summary>
        private System.Drawing.Bitmap lastGeneratedDrawing = null;

        /// <summary>
        /// 현재 선택된 노드 인덱스 (부재 정보 탭용)
        /// </summary>
        private int selectedAttributeNodeIndex = -1;

        public Form1()
        {
            InitializeComponent();

            // BOM ListView 컬럼 재구성
            SetupBOMColumns();

            // 부재 정보 DataGridView 컬럼 설정
            SetupAttributeColumns();

            // 이벤트 등록
            lvBOM.DoubleClick += LvBOM_DoubleClick;
            lvClash.DoubleClick += LvClash_DoubleClick;
            lvClash.SelectedIndexChanged += LvClash_SelectedIndexChanged;

            // VIZCore3D.NET 초기화
            VIZCore3D.NET.ModuleInitializer.Run();

            // VIZCore3D 컨트롤 생성
            vizcore3d = new VIZCore3D.NET.VIZCore3DControl();
            vizcore3d.Dock = DockStyle.Fill;
            panelViewer.Controls.Add(vizcore3d);

            // 초기화 이벤트 등록
            vizcore3d.OnInitializedVIZCore3D += Vizcore3d_OnInitializedVIZCore3D;


        }

        /// <summary>
        /// BOM ListView 컬럼 설정
        /// </summary>
        private void SetupBOMColumns()
        {
            lvBOM.Columns.Clear();
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
                _autoProcessOsnapSuccess = false;
                lvBOM.Items.Clear();
                lvClash.Items.Clear();
                lvOsnap.Items.Clear();
                lvDimension.Items.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

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

                    // BOM 수집만 수행
                    bool bomSuccess = CollectBOMData();
                    //string msg = bomSuccess
                    //    ? $"모델 로드 완료!\nBOM: {bomList.Count}개 수집\n\n'치수 추출' 버튼으로 치수 분석을 시작하세요."
                    //    : "모델 로드 완료! (BOM 수집 실패)";
                    //MessageBox.Show(msg, "파일 열기", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.\n먼저 파일을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
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

                    // ListView에 추가
                    lvDimension.Items.Clear();
                    int no = 1;
                    foreach (var dim in chainDimensionList)
                    {
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
        private bool _autoProcessOsnapSuccess = false;

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

                foreach (var node in allBodyNodes)
                {
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
                                        osnapPointsWithNames.Add((startVertex, node.NodeName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, node.NodeName));
                                    }
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    if (osnap.Center != null)
                                    {
                                        var centerVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(centerVertex);
                                        osnapPointsWithNames.Add((centerVertex, node.NodeName));
                                    }
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, node.NodeName));
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

                foreach (var node in allNodes)
                {
                    BOMData bom = new BOMData();
                    bom.Name = node.NodeName;
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
                    bomList.Add(bom);

                    ListViewItem lvi = new ListViewItem(bom.Name);
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
                    lvi.Tag = bom;
                    lvBOM.Items.Add(lvi);
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

        /// <summary>
        /// Clash Detection 수행 (ClashManager API 사용)
        /// </summary>
        private bool DetectClash()
        {
            clashList.Clear();
            lvClash.Items.Clear();

            try
            {
                List<VIZCore3D.NET.Data.Node> allNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);

                if (allNodes == null || allNodes.Count == 0)
                {
                    return false;
                }

                vizcore3d.Clash.Clear();
                int clashCount = 0;

                for (int i = 0; i < allNodes.Count; i++)
                {
                    for (int j = i + 1; j < allNodes.Count; j++)
                    {
                        VIZCore3D.NET.Data.ClashTest pairClash = new VIZCore3D.NET.Data.ClashTest();
                        pairClash.Name = $"간섭검사_{allNodes[i].NodeName}_vs_{allNodes[j].NodeName}";
                        pairClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        pairClash.UseClearanceValue = true;
                        pairClash.ClearanceValue = 1.0f;
                        pairClash.UseRangeValue = true;
                        pairClash.RangeValue = 1.0f;
                        pairClash.UsePenetrationTolerance = true;
                        pairClash.PenetrationTolerance = 1.0f;
                        pairClash.VisibleOnly = false;
                        pairClash.BottomLevel = 0;
                        pairClash.GroupA = new List<VIZCore3D.NET.Data.Node> { allNodes[i] };
                        pairClash.GroupB = new List<VIZCore3D.NET.Data.Node> { allNodes[j] };

                        if (vizcore3d.Clash.Add(pairClash))
                        {
                            clashCount++;
                        }
                    }
                }

                if (clashCount == 0) return false;

                bool startResult = vizcore3d.Clash.PerformInterferenceCheck();
                return startResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clash 검사 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clash 검사 버튼
        /// </summary>
        private void btnClashDetection_Click(object sender, EventArgs e)
        {
            bool success = DetectClash();
            if (success)
            {
                MessageBox.Show("간섭검사를 시작합니다.\n완료되면 알림창이 표시됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("로드된 모델이 없거나 간섭검사 시작에 실패했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 간섭검사 완료 이벤트 핸들러
        /// </summary>
        private void Clash_OnClashTestFinishedEvent(object sender, VIZCore3D.NET.Event.EventManager.ClashEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Clash Finished] 이벤트 발생! ID: {e.ID}");

                // ClashTest 개수 확인
                int testCount = vizcore3d.Clash.ClashTestCount;
                System.Diagnostics.Debug.WriteLine($"현재 등록된 ClashTest 개수: {testCount}");

                clashList.Clear();
                lvClash.Items.Clear();

                // 모든 ClashTest 결과 수집
                for (int i = 0; i < testCount; i++)
                {
                    VIZCore3D.NET.Data.ClashTest clashTest = vizcore3d.Clash.Items[i];

                    if (clashTest == null) continue;

                    // 결과 조회 (PART 레벨로 그룹화)
                    var results = vizcore3d.Clash.GetResultItem(
                        clashTest,
                        VIZCore3D.NET.Manager.ClashManager.ResultGroupingOptions.PART
                    );

                    if (results != null && results.Count > 0)
                    {
                        foreach (var result in results)
                        {
                            ClashData clash = new ClashData();

                            // 노드 인덱스
                            clash.Index1 = result.NodeIndexA;
                            clash.Index2 = result.NodeIndexB;

                            // 노드 이름
                            clash.Name1 = !string.IsNullOrEmpty(result.NodeNameA) ? result.NodeNameA : "Unknown";
                            clash.Name2 = !string.IsNullOrEmpty(result.NodeNameB) ? result.NodeNameB : "Unknown";

                            // 간섭 위치 (HotPoint의 Z 값)
                            if (result.HotPoint != null)
                            {
                                clash.ZValue = result.HotPoint.Z;
                            }

                            // 중복 검사 (A-B와 B-A 동일 처리)
                            bool isDuplicate = clashList.Any(c =>
                                (c.Index1 == clash.Index1 && c.Index2 == clash.Index2) ||
                                (c.Index1 == clash.Index2 && c.Index2 == clash.Index1));

                            if (!isDuplicate)
                            {
                                clashList.Add(clash);
                            }
                        }
                    }
                }

                if (clashList.Count > 0)
                {
                    // Z값 기준으로 정렬 (높은 값부터 - 내림차순)
                    clashList.Sort((a, b) => b.ZValue.CompareTo(a.ZValue));

                    // ListView에 추가
                    foreach (var clash in clashList)
                    {
                        ListViewItem lvi = new ListViewItem(clash.Name1);
                        lvi.SubItems.Add(clash.Name2);
                        lvi.SubItems.Add(clash.ZValue.ToString("F2"));
                        lvi.Tag = clash;
                        lvClash.Items.Add(lvi);
                    }
                }

                // 전체 요약 알림 (BOM + Osnap + 치수 + Clash 한 번에)
                string clashResult = clashList.Count > 0
                    ? $"Clash: {clashList.Count}개 검출 (검사 쌍: {testCount}개)"
                    : $"Clash: 간섭 없음 (검사 쌍: {testCount}개)";

                string summaryMessage = $"모델 로드 및 자동 처리 완료!\n\n" +
                    $"BOM: {bomList.Count}개\n" +
                    $"Osnap: {osnapPointsWithNames.Count}개\n" +
                    $"치수: {chainDimensionList.Count}개\n" +
                    clashResult;

                if (!_autoProcessOsnapSuccess)
                {
                    summaryMessage += "\n\n* Osnap 수집 실패";
                }

                MessageBox.Show(summaryMessage, "자동 처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"간섭검사 결과 처리 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 2D 도면 생성 - 전문 제조 도면 스타일 (4분할 뷰 + BOM 테이블 + 타이틀 블록)
        /// </summary>
        private void btnGenerate2D_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 2D 도면 모드 활성화
                vizcore3d.ToolbarDrawing2D.Visible = true;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

                // 캡처 크기 설정
                int captureWidth = 400;
                int captureHeight = 300;

                // 배경색 저장
                var bgMode = vizcore3d.View.BackgroundMode;
                var bgColor1 = vizcore3d.View.BackgroundColor1;
                var bgColor2 = vizcore3d.View.BackgroundColor2;

                // SilhouetteEdge 설정 저장
                bool origSilhouetteEdge = vizcore3d.View.SilhouetteEdge;
                System.Drawing.Color origSilhouetteColor = vizcore3d.View.SilhouetteEdgeColor;
                float origEdgeWidthRatio = vizcore3d.View.EdgeWidthRatio;

                // 배경색 흰색으로 변경
                vizcore3d.View.BackgroundMode = VIZCore3D.NET.Data.BackgroundModes.COLOR_ONE;
                vizcore3d.View.BackgroundColor1 = System.Drawing.Color.White;

                // 모든 객체를 검은색으로 설정 (DASH_LINE 모드에서 내부 모서리 포함 모든 선이 검은색으로 렌더링)
                vizcore3d.Object3D.Color.SetColor(System.Drawing.Color.Black);

                // 렌더링 모드를 DASH_LINE으로 설정 (은선 점선: 보이는 모서리 실선 + 숨겨진 모서리 점선)
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

                // 윤곽선(SilhouetteEdge) 활성화 및 검은색으로 설정
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = System.Drawing.Color.FromArgb(255, 0, 0, 0);

                // 모서리 굵기 비율 설정 (선명한 도면용)
                vizcore3d.View.EdgeWidthRatio = 50;

                // 모델 BoundBox 중심을 카메라 피벗으로 설정
                VIZCore3D.NET.Data.Vertex3D modelCenter = null;
                var boundBox = vizcore3d.Model.BoundBox;
                if (boundBox != null)
                {
                    float cx = (boundBox.MinX + boundBox.MaxX) / 2.0f;
                    float cy = (boundBox.MinY + boundBox.MaxY) / 2.0f;
                    float cz = (boundBox.MinZ + boundBox.MaxZ) / 2.0f;
                    modelCenter = new VIZCore3D.NET.Data.Vertex3D(cx, cy, cz);
                    vizcore3d.View.SetPivotPosition(modelCenter);
                }

                // ISO 뷰로 이동하여 BOM 스크린 좌표 미리 계산
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                vizcore3d.View.FitToView(0.0f, 0.0f);

                // BOM별 스크린 좌표 계산 (현재 뷰어 크기 기준)
                Dictionary<int, PointF> bomScreenPositions = new Dictionary<int, PointF>();
                int viewerWidth = vizcore3d.Width;
                int viewerHeight = vizcore3d.Height;

                if (bomList != null && bomList.Count > 0)
                {
                    for (int i = 0; i < bomList.Count; i++)
                    {
                        BOMData bom = bomList[i];
                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);
                        VIZCore3D.NET.Data.Vertex3D screenPos = vizcore3d.View.WorldToScreen(center, true);

                        // 뷰어 좌표를 캡처 이미지 좌표로 변환
                        float scaleX = (float)captureWidth / viewerWidth;
                        float scaleY = (float)captureHeight / viewerHeight;
                        bomScreenPositions[i] = new PointF(screenPos.X * scaleX, screenPos.Y * scaleY);
                    }
                }

                // 백그라운드 렌더링 모드 시작
                vizcore3d.View.BeginBackgroundRenderingMode(captureWidth, captureHeight);

                // 4개 방향 캡처
                Dictionary<string, System.Drawing.Image> viewImages = new Dictionary<string, System.Drawing.Image>();

                // 1. ISO 뷰 - 치수 없음
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                if (modelCenter != null) vizcore3d.View.SetPivotPosition(modelCenter);
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                vizcore3d.View.FitToView(0.0f, 0.0f);
                viewImages["ISO VIEW"] = vizcore3d.View.GetBackgroundRenderingImage();

                // 체인 치수가 있는 경우 각 뷰에 최소 치수 표시
                bool hasDimensions = (chainDimensionList != null && chainDimensionList.Count > 0);

                // 2. Z+ (평면도) - X,Y축 치수 표시
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                if (hasDimensions)
                {
                    AddDimensionsForView("Z");
                }
                if (modelCenter != null) vizcore3d.View.SetPivotPosition(modelCenter);
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
                vizcore3d.View.FitToView(0.0f, 0.0f);
                viewImages["PLAN (Z+)"] = vizcore3d.View.GetBackgroundRenderingImage();

                // 3. Y+ (정면도) - X,Z축 치수 표시
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                if (hasDimensions)
                {
                    AddDimensionsForView("Y");
                }
                if (modelCenter != null) vizcore3d.View.SetPivotPosition(modelCenter);
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS);
                vizcore3d.View.FitToView(0.0f, 0.0f);
                viewImages["FRONT (Y+)"] = vizcore3d.View.GetBackgroundRenderingImage();

                // 4. X+ (측면도) - Y,Z축 치수 표시
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                if (hasDimensions)
                {
                    AddDimensionsForView("X");
                }
                if (modelCenter != null) vizcore3d.View.SetPivotPosition(modelCenter);
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS);
                vizcore3d.View.FitToView(0.0f, 0.0f);
                viewImages["SIDE (X+)"] = vizcore3d.View.GetBackgroundRenderingImage();

                // 백그라운드 렌더링 모드 종료
                vizcore3d.View.EndBackgroundRenderingMode();

                // 객체 색상 복원
                vizcore3d.Object3D.Color.RestoreColor();

                // 배경색 복원
                vizcore3d.View.BackgroundMode = bgMode;
                vizcore3d.View.BackgroundColor1 = bgColor1;
                vizcore3d.View.BackgroundColor2 = bgColor2;

                // SilhouetteEdge 설정 복원
                vizcore3d.View.SilhouetteEdge = origSilhouetteEdge;
                vizcore3d.View.SilhouetteEdgeColor = origSilhouetteColor;
                vizcore3d.View.EdgeWidthRatio = origEdgeWidthRatio;

                // 렌더링 모드 복원
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH_EDGE);

                // 치수 정리 후 ISO 뷰로 복귀
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                vizcore3d.View.FitToView();

                // 도면 레이아웃 설정
                int viewAreaWidth = captureWidth * 2 + 30;
                int bomTableWidth = 280;
                int titleBlockHeight = 80;
                int totalWidth = viewAreaWidth + bomTableWidth + 20;
                int totalHeight = captureHeight * 2 + 80 + titleBlockHeight;

                using (Bitmap drawing2D = new Bitmap(totalWidth, totalHeight))
                using (Graphics g = Graphics.FromImage(drawing2D))
                {
                    g.Clear(System.Drawing.Color.White);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    Font titleFont = new Font("Arial", 10, FontStyle.Bold);
                    Font scaleFont = new Font("Arial", 8);
                    Font bomFont = new Font("Arial", 7);
                    Font bomHeaderFont = new Font("Arial", 7, FontStyle.Bold);
                    Font infoFont = new Font("Arial", 8);
                    Font infoTitleFont = new Font("Arial", 9, FontStyle.Bold);
                    Pen borderPen = new Pen(System.Drawing.Color.Black, 1);
                    Pen thickPen = new Pen(System.Drawing.Color.Black, 2);
                    int scale = 50;

                    // 도면 외곽선
                    g.DrawRectangle(thickPen, 5, 5, totalWidth - 10, totalHeight - 10);

                    // 도면 제목
                    g.DrawString("2D MANUFACTURING DRAWING", new Font("Arial", 14, FontStyle.Bold),
                        Brushes.Black, new PointF(viewAreaWidth / 2 - 100, 10));

                    int startY = 35;

                    // 1. 좌상단: ISO (치수 없음) + 부재 번호
                    if (viewImages.ContainsKey("ISO VIEW"))
                    {
                        g.DrawImage(viewImages["ISO VIEW"], 10, startY, captureWidth, captureHeight);
                        g.DrawRectangle(borderPen, 10, startY, captureWidth, captureHeight);
                        g.DrawString($"ISO VIEW (S=1:{scale})", titleFont, Brushes.Black, 15, startY + 5);

                        // ISO 뷰에 부재 번호 표시
                        DrawPartNumbersOnIsoView(g, 10, startY, captureWidth, captureHeight, bomScreenPositions);
                    }

                    // 2. 우상단 (뷰 영역): Z+ (평면도)
                    if (viewImages.ContainsKey("PLAN (Z+)"))
                    {
                        g.DrawImage(viewImages["PLAN (Z+)"], captureWidth + 20, startY, captureWidth, captureHeight);
                        g.DrawRectangle(borderPen, captureWidth + 20, startY, captureWidth, captureHeight);
                        g.DrawString($"PLAN VIEW - Z+ (S=1:{scale})", titleFont, Brushes.Black, captureWidth + 25, startY + 5);
                    }

                    // 3. 좌하단: Y+ (정면도)
                    if (viewImages.ContainsKey("FRONT (Y+)"))
                    {
                        g.DrawImage(viewImages["FRONT (Y+)"], 10, startY + captureHeight + 20, captureWidth, captureHeight);
                        g.DrawRectangle(borderPen, 10, startY + captureHeight + 20, captureWidth, captureHeight);
                        g.DrawString($"FRONT VIEW - Y+ (S=1:{scale})", titleFont, Brushes.Black, 15, startY + captureHeight + 25);
                    }

                    // 4. 우하단 (뷰 영역): X+ (측면도)
                    if (viewImages.ContainsKey("SIDE (X+)"))
                    {
                        g.DrawImage(viewImages["SIDE (X+)"], captureWidth + 20, startY + captureHeight + 20, captureWidth, captureHeight);
                        g.DrawRectangle(borderPen, captureWidth + 20, startY + captureHeight + 20, captureWidth, captureHeight);
                        g.DrawString($"SIDE VIEW - X+ (S=1:{scale})", titleFont, Brushes.Black, captureWidth + 25, startY + captureHeight + 25);
                    }

                    // ========== 우측 상단: BOM 테이블 ==========
                    int bomX = viewAreaWidth + 10;
                    int bomY = startY;
                    int bomHeight = captureHeight * 2 + 20 - titleBlockHeight;

                    DrawBOMTable(g, bomX, bomY, bomTableWidth - 10, bomHeight, bomFont, bomHeaderFont, borderPen);

                    // ========== 우측 하단: 도면 정보 칸 ==========
                    int infoX = viewAreaWidth + 10;
                    int infoY = startY + captureHeight * 2 + 20 - titleBlockHeight + 10;
                    int infoWidth = bomTableWidth - 10;
                    int infoHeight = titleBlockHeight + captureHeight - 10;

                    DrawTitleBlock(g, infoX, infoY, infoWidth, infoHeight, infoFont, infoTitleFont, borderPen);

                    titleFont.Dispose();
                    scaleFont.Dispose();
                    bomFont.Dispose();
                    bomHeaderFont.Dispose();
                    infoFont.Dispose();
                    infoTitleFont.Dispose();
                    borderPen.Dispose();
                    thickPen.Dispose();

                    // 마지막 생성된 도면 저장 (PDF 출력용)
                    if (lastGeneratedDrawing != null)
                    {
                        lastGeneratedDrawing.Dispose();
                    }
                    lastGeneratedDrawing = (Bitmap)drawing2D.Clone();

                    // 2D 도면 폼 표시
                    Show2DDrawingForm(drawing2D);
                }

                // 이미지 리소스 해제
                foreach (var img in viewImages.Values)
                {
                    img?.Dispose();
                }

                MessageBox.Show($"2D 도면 생성 완료!\n\n" +
                    $"- ISO VIEW (좌상단) - 부재 번호 포함\n" +
                    $"- PLAN VIEW - Z+ (우상단) - X,Y축 최소 치수 포함\n" +
                    $"- FRONT VIEW - Y+ (좌하단) - X,Z축 최소 치수 포함\n" +
                    $"- SIDE VIEW - X+ (우하단) - Y,Z축 최소 치수 포함\n" +
                    $"- BOM 테이블 + 도면 정보 칸", "2D 도면 생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"2D 도면 생성 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 지정된 뷰 방향에 맞는 치수를 추가 (2D 도면 생성용 - 멀티레벨 로직)
        /// ShowAllDimensions와 동일한 baseline 기준 오프셋 + IsTotal 분리 + 짧은 치수 레벨 분리
        /// 색상만 Black(인쇄용)으로 적용
        /// </summary>
        private void AddDimensionsForView(string viewDirection)
        {
            if (chainDimensionList == null || chainDimensionList.Count == 0) return;

            // 뷰 방향에 따라 표시할 축 결정
            List<string> visibleAxes = new List<string>();
            switch (viewDirection)
            {
                case "X":
                    visibleAxes.Add("Y");
                    visibleAxes.Add("Z");
                    break;
                case "Y":
                    visibleAxes.Add("X");
                    visibleAxes.Add("Z");
                    break;
                case "Z":
                    visibleAxes.Add("X");
                    visibleAxes.Add("Y");
                    break;
            }

            // 해당 축들의 치수 필터링 (전체 표시 - 최소 치수 필터 없음)
            var displayList = chainDimensionList.Where(d => visibleAxes.Contains(d.Axis)).ToList();

            if (displayList.Count == 0) return;

            // 측정 스타일 설정 - 정수만 표시, 검은색(인쇄용), 배경 투명, 테두리 없음
            VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
            measureStyle.Prefix = false;
            measureStyle.Unit = false;
            measureStyle.NumberOfDecimalPlaces = 0;
            measureStyle.DX_DY_DZ = false;
            measureStyle.Frame = false;
            measureStyle.ContinuousDistance = false;
            measureStyle.BackgroundTransparent = true;
            measureStyle.FontColor = System.Drawing.Color.Black;
            measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE10;
            measureStyle.FontBold = true;
            measureStyle.LineColor = System.Drawing.Color.Black;
            measureStyle.LineWidth = 2;
            measureStyle.ArrowColor = System.Drawing.Color.Black;
            measureStyle.ArrowSize = 8;
            measureStyle.AssistantLine = true;
            measureStyle.AssistantLineStyle = VIZCore3D.NET.Data.MeasureStyle.AssistantLineType.SOLIDLINE;
            measureStyle.AlignDistanceText = true;
            measureStyle.AlignDistanceTextPosition = 2;
            measureStyle.AlignDistanceTextMargine = 10;
            vizcore3d.Review.Measure.SetStyle(measureStyle);

            // 선택된 모델의 바운딩 박스에서 최소값 계산 (baseline 기준)
            float globalMinX = float.MaxValue, globalMinY = float.MaxValue, globalMinZ = float.MaxValue;
            if (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
            {
                foreach (int nodeIdx in xraySelectedNodeIndices)
                {
                    BOMData bom = bomList.FirstOrDefault(b => b.Index == nodeIdx);
                    if (bom != null)
                    {
                        globalMinX = Math.Min(globalMinX, bom.MinX);
                        globalMinY = Math.Min(globalMinY, bom.MinY);
                        globalMinZ = Math.Min(globalMinZ, bom.MinZ);
                    }
                }
            }
            // 선택된 노드가 없으면 치수 포인트에서 최소값 사용
            if (globalMinX == float.MaxValue)
            {
                foreach (var dim in chainDimensionList)
                {
                    globalMinX = Math.Min(globalMinX, Math.Min(dim.StartPoint.X, dim.EndPoint.X));
                    globalMinY = Math.Min(globalMinY, Math.Min(dim.StartPoint.Y, dim.EndPoint.Y));
                    globalMinZ = Math.Min(globalMinZ, Math.Min(dim.StartPoint.Z, dim.EndPoint.Z));
                }
            }

            float initialOffset = 5.0f;
            float minDimensionLength = 15.0f;  // 폰트가 들어갈 최소 치수선 길이

            // 보조선(Extension Line) 저장용 리스트
            List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

            // 축별로 그룹화하여 처리 (IsTotal 제외한 순차 치수만)
            var sequentialDims = displayList.Where(d => !d.IsTotal).GroupBy(d => d.Axis);
            var totalDims = displayList.Where(d => d.IsTotal).ToList();

            // 축별 최대 레벨 추적 (Total offset 계산용)
            Dictionary<string, int> axisMaxLevel = new Dictionary<string, int>
            {
                { "X", 1 }, { "Y", 1 }, { "Z", 1 }
            };


            // 축별 전체 치수 중복 여부 추적
            Dictionary<string, bool> axisTotalRedundant = new Dictionary<string, bool>();

            foreach (var axisGroup in sequentialDims)
            {
                string axis = axisGroup.Key;
                var dims = axisGroup.OrderBy(d =>
                {
                    switch (axis)
                    {
                        case "X": return -d.StartPoint.X;
                        case "Y": return -d.StartPoint.Y;
                        case "Z": return -d.StartPoint.Z;
                        default: return 0f;
                    }
                }).ToList();

                if (dims.Count == 0) continue;

                var firstPoint = dims[0].StartPoint;
                int shortCount = 0;  // 짧은 치수 발생 횟수 (레벨 결정용)
                bool lastDimWasShort = false;

                for (int i = 0; i < dims.Count; i++)
                {
                    if (dims[i].Distance < minDimensionLength)
                    {
                        // 짧은 치수 → Level 1에는 그리지 않음
                        // 처음(firstPoint)부터 이 짧은 치수 끝까지 누적 치수를 다음 레벨에 그리기
                        shortCount++;
                        float levelOffset = initialOffset * (shortCount + 1);

                        DrawDimension(firstPoint, dims[i].EndPoint, axis,
                            levelOffset, globalMinX, globalMinY, globalMinZ,
                            viewDirection, extensionLines);
                        lastDimWasShort = true;
                    }
                    else
                    {
                        // 정상 길이 치수 → Level 1에 그리기
                        DrawDimension(dims[i].StartPoint, dims[i].EndPoint, axis,
                            initialOffset, globalMinX, globalMinY, globalMinZ,
                            viewDirection, extensionLines);
                        lastDimWasShort = false;
                    }
                }

                axisMaxLevel[axis] = shortCount + 1;  // 사용된 최대 레벨

                // 마지막 치수가 짧으면 누적 치수(firstPoint→마지막EndPoint)가 전체 치수와 동일 → 중복
                axisTotalRedundant[axis] = lastDimWasShort;
            }

            // 전체 길이 치수 그리기 (IsTotal) - 최대 레벨 바깥에 (중복 시 스킵)
            foreach (var dim in totalDims)
            {
                // 마지막 순차 치수가 짧아서 누적 치수가 이미 전체를 커버하면 스킵
                if (axisTotalRedundant.ContainsKey(dim.Axis) && axisTotalRedundant[dim.Axis])
                    continue;

                int maxLevel = axisMaxLevel.ContainsKey(dim.Axis) ? axisMaxLevel[dim.Axis] : 1;
                float totalOffset = initialOffset * (maxLevel + 1);

                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                    totalOffset, globalMinX, globalMinY, globalMinZ,
                    viewDirection, extensionLines);
            }

            // 보조선(Extension Line) 그리기 - 검은색(인쇄용)
            if (extensionLines.Count > 0)
            {
                vizcore3d.ShapeDrawing.AddLine(extensionLines, 0, System.Drawing.Color.Black, 1.0f, true);
            }
        }

        /// <summary>
        /// 2D 도면 폼 표시
        /// </summary>
        private void Show2DDrawingForm(Bitmap drawing)
        {
            Form drawingForm = new Form();
            drawingForm.Text = "2D 도면 - 4분할 뷰";
            drawingForm.Size = new System.Drawing.Size(drawing.Width + 40, drawing.Height + 80);
            drawingForm.StartPosition = FormStartPosition.CenterScreen;
            drawingForm.BackColor = System.Drawing.Color.White;

            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = new Bitmap(drawing);  // 복사본 생성
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.Location = new System.Drawing.Point(10, 10);

            // 저장 버튼
            Button btnSave = new Button();
            btnSave.Text = "이미지 저장";
            btnSave.Location = new System.Drawing.Point(10, drawing.Height + 15);
            btnSave.Size = new System.Drawing.Size(100, 25);
            btnSave.Click += (s, ev) =>
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "PNG 파일 (*.png)|*.png|JPEG 파일 (*.jpg)|*.jpg|BMP 파일 (*.bmp)|*.bmp";
                dlg.FileName = $"2D_Drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pictureBox.Image.Save(dlg.FileName);
                    MessageBox.Show($"저장 완료: {dlg.FileName}", "저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // 인쇄 버튼
            Button btnPrint = new Button();
            btnPrint.Text = "인쇄";
            btnPrint.Location = new System.Drawing.Point(120, drawing.Height + 15);
            btnPrint.Size = new System.Drawing.Size(80, 25);
            btnPrint.Click += (s, ev) =>
            {
                MessageBox.Show("인쇄 기능은 구현 예정입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            drawingForm.Controls.Add(pictureBox);
            drawingForm.Controls.Add(btnSave);
            drawingForm.Controls.Add(btnPrint);

            drawingForm.Show();
        }

        /// <summary>
        /// ISO 뷰에 부재 번호를 원 안에 표시하고 BOM 센터에서 지시선 시작
        /// </summary>
        private void DrawPartNumbersOnIsoView(Graphics g, int viewX, int viewY, int viewWidth, int viewHeight, Dictionary<int, PointF> bomScreenPositions)
        {
            if (bomList == null || bomList.Count == 0) return;
            if (bomScreenPositions == null || bomScreenPositions.Count == 0) return;

            Font numFont = new Font("Arial", 7, FontStyle.Bold);
            Pen circlePen = new Pen(System.Drawing.Color.Blue, 1);
            Pen linePen = new Pen(System.Drawing.Color.Blue, 1);

            int itemCount = Math.Min(bomList.Count, 20);
            int circleRadius = 10;

            float viewCenterX = viewX + viewWidth / 2.0f;
            float viewCenterY = viewY + viewHeight / 2.0f;

            for (int i = 0; i < itemCount; i++)
            {
                if (!bomScreenPositions.ContainsKey(i)) continue;

                int num = i + 1;

                PointF bomPos = bomScreenPositions[i];
                float bomScreenX = viewX + bomPos.X;
                float bomScreenY = viewY + bomPos.Y;

                bomScreenX = Math.Max(viewX + 20, Math.Min(viewX + viewWidth - 20, bomScreenX));
                bomScreenY = Math.Max(viewY + 20, Math.Min(viewY + viewHeight - 20, bomScreenY));

                float bubbleX, bubbleY;

                if (bomScreenX < viewCenterX)
                {
                    bubbleX = viewX + 20;
                }
                else
                {
                    bubbleX = viewX + viewWidth - 20;
                }

                bubbleY = viewY + 25 + (i % 10) * 25;

                g.DrawEllipse(circlePen, bubbleX - circleRadius, bubbleY - circleRadius,
                    circleRadius * 2, circleRadius * 2);

                string numText = num.ToString();
                SizeF textSize = g.MeasureString(numText, numFont);
                g.DrawString(numText, numFont, Brushes.Blue,
                    bubbleX - textSize.Width / 2, bubbleY - textSize.Height / 2);

                float dx = bomScreenX - bubbleX;
                float dy = bomScreenY - bubbleY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > 0)
                {
                    dx /= dist;
                    dy /= dist;
                }

                float lineStartX = bubbleX + dx * circleRadius;
                float lineStartY = bubbleY + dy * circleRadius;

                float bendX = lineStartX + (bomScreenX > bubbleX ? 15 : -15);
                float bendY = lineStartY;

                g.DrawLine(linePen, lineStartX, lineStartY, bendX, bendY);
                g.DrawLine(linePen, bendX, bendY, bomScreenX, bomScreenY);

                g.FillEllipse(Brushes.Blue, bomScreenX - 3, bomScreenY - 3, 6, 6);
            }

            numFont.Dispose();
            circlePen.Dispose();
            linePen.Dispose();
        }

        /// <summary>
        /// BOM 테이블 그리기
        /// </summary>
        private void DrawBOMTable(Graphics g, int x, int y, int width, int height, Font font, Font headerFont, Pen pen)
        {
            string[] headers = { "No", "Name", "Type", "Qty" };
            int[] colWidths = { 30, 130, 70, 35 };

            g.DrawRectangle(pen, x, y, width, height);

            g.FillRectangle(Brushes.LightGray, x + 1, y + 1, width - 2, 18);

            int colX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                g.DrawString(headers[i], headerFont, Brushes.Black, colX + 3, y + 3);
                colX += colWidths[i];
                if (i < headers.Length - 1)
                {
                    g.DrawLine(pen, x + GetColOffset(colWidths, i + 1), y, x + GetColOffset(colWidths, i + 1), y + height);
                }
            }

            g.DrawLine(pen, x, y + 18, x + width, y + 18);

            int rowHeight = 16;
            int rowY = y + 20;
            int maxRows = (height - 20) / rowHeight;

            for (int i = 0; i < lvBOM.Items.Count && i < maxRows; i++)
            {
                ListViewItem item = lvBOM.Items[i];
                string name = item.Text;
                string type = "PART";
                string qty = "1";

                g.DrawString((i + 1).ToString(), font, Brushes.Black, x + 3, rowY);

                g.DrawString(TruncateString(name, 18), font, Brushes.Black, x + colWidths[0] + 3, rowY);

                if (item.SubItems.Count > 1)
                {
                    string angle = item.SubItems[1].Text;
                    type = string.IsNullOrEmpty(angle) || angle == "0" ? "PART" : "ASSEMBLY";
                }
                g.DrawString(type, font, Brushes.Black, x + colWidths[0] + colWidths[1] + 3, rowY);

                g.DrawString(qty, font, Brushes.Black, x + colWidths[0] + colWidths[1] + colWidths[2] + 3, rowY);

                rowY += rowHeight;
                if (rowY < y + height - 5)
                {
                    g.DrawLine(pen, x, rowY - 2, x + width, rowY - 2);
                }
            }
        }

        private int GetColOffset(int[] colWidths, int colIndex)
        {
            int offset = 0;
            for (int i = 0; i < colIndex && i < colWidths.Length; i++)
            {
                offset += colWidths[i];
            }
            return offset;
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 2) + "..";
        }

        /// <summary>
        /// 도면 정보 칸 (Title Block) 그리기
        /// </summary>
        private void DrawTitleBlock(Graphics g, int x, int y, int width, int height, Font font, Font titleFont, Pen pen)
        {
            g.DrawRectangle(new Pen(System.Drawing.Color.Black, 2), x, y, width, height);

            int rowHeight = height / 5;

            g.DrawRectangle(pen, x, y, width, rowHeight);
            g.DrawString("Company:", font, Brushes.Black, x + 5, y + 3);
            g.DrawString("SOFTHILLS Co., Ltd.", titleFont, Brushes.Black, x + 60, y + 3);

            g.DrawRectangle(pen, x, y + rowHeight, width, rowHeight);
            g.DrawString("Drawing:", font, Brushes.Black, x + 5, y + rowHeight + 3);
            string drawingName = !string.IsNullOrEmpty(currentFilePath) ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath) : "Untitled";
            g.DrawString(TruncateString(drawingName, 22), titleFont, Brushes.Black, x + 60, y + rowHeight + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 2, width, rowHeight);
            g.DrawString("DWG No:", font, Brushes.Black, x + 5, y + rowHeight * 2 + 3);
            g.DrawString($"DWG-{DateTime.Now:yyyyMMdd}-001", font, Brushes.Black, x + 60, y + rowHeight * 2 + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 3, width / 2, rowHeight);
            g.DrawString("Scale:", font, Brushes.Black, x + 5, y + rowHeight * 3 + 3);
            g.DrawString("1:50", font, Brushes.Black, x + 45, y + rowHeight * 3 + 3);

            g.DrawRectangle(pen, x + width / 2, y + rowHeight * 3, width / 2, rowHeight);
            g.DrawString("Date:", font, Brushes.Black, x + width / 2 + 5, y + rowHeight * 3 + 3);
            g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), font, Brushes.Black, x + width / 2 + 40, y + rowHeight * 3 + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 4, width / 2, rowHeight);
            g.DrawString("Drawn:", font, Brushes.Black, x + 5, y + rowHeight * 4 + 3);
            g.DrawString("Engineer", font, Brushes.Black, x + 45, y + rowHeight * 4 + 3);

            g.DrawRectangle(pen, x + width / 2, y + rowHeight * 4, width / 2, rowHeight);
            g.DrawString("Appr:", font, Brushes.Black, x + width / 2 + 5, y + rowHeight * 4 + 3);
            g.DrawString("Manager", font, Brushes.Black, x + width / 2 + 40, y + rowHeight * 4 + 3);
        }

        /// <summary>
        /// PDF로 출력 (Microsoft Print to PDF 또는 인쇄 대화상자 사용)
        /// </summary>
        private void PrintToPDF(string pdfFileName)
        {
            try
            {
                using (System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument())
                {
                    printDoc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    printDoc.PrinterSettings.PrintToFile = true;
                    printDoc.PrinterSettings.PrintFileName = pdfFileName;

                    printDoc.DefaultPageSettings.Landscape = true;

                    printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20);

                    if (!printDoc.PrinterSettings.IsValid)
                    {
                        using (PrintDialog printDialog = new PrintDialog())
                        {
                            printDialog.Document = printDoc;
                            printDialog.AllowSomePages = false;
                            printDialog.AllowSelection = false;

                            if (printDialog.ShowDialog() == DialogResult.OK)
                            {
                                printDoc.PrinterSettings = printDialog.PrinterSettings;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    printDoc.PrintPage += (s, ev) =>
                    {
                        if (lastGeneratedDrawing != null)
                        {
                            float pageWidth = ev.PageBounds.Width - ev.PageSettings.Margins.Left - ev.PageSettings.Margins.Right;
                            float pageHeight = ev.PageBounds.Height - ev.PageSettings.Margins.Top - ev.PageSettings.Margins.Bottom;

                            float imgWidth = lastGeneratedDrawing.Width;
                            float imgHeight = lastGeneratedDrawing.Height;

                            float printScale = Math.Min(pageWidth / imgWidth, pageHeight / imgHeight);
                            float drawWidth = imgWidth * printScale;
                            float drawHeight = imgHeight * printScale;

                            float xPos = ev.PageSettings.Margins.Left + (pageWidth - drawWidth) / 2;
                            float yPos = ev.PageSettings.Margins.Top + (pageHeight - drawHeight) / 2;

                            ev.Graphics.DrawImage(lastGeneratedDrawing, xPos, yPos, drawWidth, drawHeight);
                        }
                        ev.HasMorePages = false;
                    };

                    printDoc.Print();

                    MessageBox.Show($"PDF 파일로 저장되었습니다.\n\n{pdfFileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show(
                    $"PDF 저장 중 오류가 발생했습니다.\n\n{ex.Message}\n\nPNG 파일로 대신 저장하시겠습니까?",
                    "PDF 저장 오류",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    string pngFileName = System.IO.Path.ChangeExtension(pdfFileName, ".png");
                    lastGeneratedDrawing.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{pngFileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

                // X-Ray 모드가 활성화되어 있고 선택된 노드가 있으면 해당 노드만 사용
                List<VIZCore3D.NET.Data.Node> bodyNodes;
                bool isFilteredMode = vizcore3d.View.XRay.Enable && xraySelectedNodeIndices.Count > 0;



                if (isFilteredMode)
                {
                    // X-Ray 모드에서 선택된 노드만 필터링
                    bodyNodes = allBodyNodes.Where(n => xraySelectedNodeIndices.Contains(n.Index)).ToList();
                }
                else
                {
                    bodyNodes = allBodyNodes;
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
                                    // 원: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var centerVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(centerVertex);
                                        osnapPointsWithNames.Add((centerVertex, node.NodeName));
                                    }
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
        /// PDF/PNG/JPEG 내보내기
        /// </summary>
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (lastGeneratedDrawing == null)
            {
                MessageBox.Show("먼저 2D 도면을 생성해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PDF 파일 (*.pdf)|*.pdf|PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg)|*.jpg";
            dlg.FilterIndex = 1;
            dlg.FileName = "2D_Drawing";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                string extension = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                if (extension == ".png")
                {
                    lastGeneratedDrawing.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    lastGeneratedDrawing.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    MessageBox.Show($"JPEG 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (extension == ".pdf")
                {
                    PrintToPDF(dlg.FileName);
                }
                else
                {
                    lastGeneratedDrawing.Save(dlg.FileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{dlg.FileName}.png", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 저장 중 오류가 발생했습니다.\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    // 노드 정보 가져오기
                    VIZCore3D.NET.Data.Node node = vizcore3d.Object3D.FromIndex(nodeIndex);
                    string nodeName = node != null ? node.NodeName : $"Node_{nodeIndex}";

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
                                    // 원: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var centerVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(centerVertex);
                                        osnapPointsWithNames.Add((centerVertex, nodeName));
                                    }
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

                // ListView에 추가
                int no = 1;
                foreach (var dim in chainDimensionList)
                {
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
        /// 선택한 치수를 뷰어에서 표시 (축에 따라 직각 방향으로 표시)
        /// </summary>
        private void btnDimensionShowSelected_Click(object sender, EventArgs e)
        {
            if (lvDimension.SelectedItems.Count == 0)
            {
                MessageBox.Show("치수를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 측정 항목 유지 (Clear 하지 않음) - 선택한 치수만 추가
                vizcore3d.Clash.ClearResultSymbol();
                // 보조선도 유지

                // 측정 스타일 설정 - 정수만 표시, 검은색
                VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
                measureStyle.Prefix = false;              // "Y축거리 = " 제거
                measureStyle.Unit = false;                // "mm" 제거
                measureStyle.NumberOfDecimalPlaces = 0;   // 소수점 없이 정수만 표시
                measureStyle.DX_DY_DZ = false;
                measureStyle.Frame = false;
                measureStyle.ContinuousDistance = false;
                measureStyle.BackgroundTransparent = false;
                measureStyle.BackgroundColor = System.Drawing.Color.White;
                measureStyle.FontColor = System.Drawing.Color.Blue;      // 검은색
                measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE14;
                measureStyle.FontBold = true;
                measureStyle.LineColor = System.Drawing.Color.Blue;      // 검은색
                measureStyle.LineWidth = 2;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;     // 검은색
                measureStyle.ArrowSize = 8;
                vizcore3d.Review.Measure.SetStyle(measureStyle);

                // 축별 오프셋 카운터 (여러 치수 동시 표시 시 겹치지 않도록)
                Dictionary<string, int> axisOffsetCount = new Dictionary<string, int>
                {
                    { "X", 0 }, { "Y", 0 }, { "Z", 0 }
                };
                float offsetStep = 50.0f;  // 치수 간 간격

                // 선택된 치수만 표시 (축에 따라 오프셋 적용)
                foreach (ListViewItem lvi in lvDimension.SelectedItems)
                {
                    ChainDimensionData dim = lvi.Tag as ChainDimensionData;
                    if (dim != null)
                    {
                        float currentOffset = axisOffsetCount[dim.Axis] * offsetStep;
                        axisOffsetCount[dim.Axis]++;

                        // 오프셋 적용하여 치수 추가
                        VIZCore3D.NET.Data.Vertex3D startVertex;
                        VIZCore3D.NET.Data.Vertex3D endVertex;

                        switch (dim.Axis)
                        {
                            case "X":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X, dim.StartPoint.Y - currentOffset, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X, dim.EndPoint.Y - currentOffset, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                                break;
                            case "Y":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X - currentOffset, dim.StartPoint.Y, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X - currentOffset, dim.EndPoint.Y, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                                break;
                            case "Z":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X - currentOffset, dim.StartPoint.Y, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X - currentOffset, dim.EndPoint.Y, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                                break;
                        }
                    }
                }

                // 선택된 치수의 중심점을 회전 중심으로 설정
                if (lvDimension.SelectedItems.Count > 0)
                {
                    ChainDimensionData firstDim = lvDimension.SelectedItems[0].Tag as ChainDimensionData;
                    if (firstDim != null)
                    {
                        float centerX = (firstDim.StartPoint.X + firstDim.EndPoint.X) / 2.0f;
                        float centerY = (firstDim.StartPoint.Y + firstDim.EndPoint.Y) / 2.0f;
                        float centerZ = (firstDim.StartPoint.Z + firstDim.EndPoint.Z) / 2.0f;

                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(centerX, centerY, centerZ);
                        vizcore3d.View.SetPivotPosition(center);
                    }
                }

                //vizcore3d.MouseControl = vizcore3d.MouseControl { 4, 14};

                //vizcore3d.MouseControl |= MouseControls.Down_Left;
                //vizcore3d.MouseControl |= MouseControls.Up_Left;
                vizcore3d.EndUpdate();

                MessageBox.Show($"{lvDimension.SelectedItems.Count}개의 치수가 표시되었습니다.", "치수 표시", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 치수 삭제
        /// </summary>
        private void btnDimensionDelete_Click(object sender, EventArgs e)
        {
            if (lvDimension.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 치수를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 선택된 항목 인덱스를 역순으로 정렬하여 삭제
                List<int> indicesToRemove = new List<int>();
                foreach (ListViewItem lvi in lvDimension.SelectedItems)
                {
                    indicesToRemove.Add(lvi.Index);
                }
                indicesToRemove.Sort();
                indicesToRemove.Reverse();

                foreach (int index in indicesToRemove)
                {
                    if (index < chainDimensionList.Count)
                    {
                        chainDimensionList.RemoveAt(index);
                    }
                    lvDimension.Items.RemoveAt(index);
                }

                // 번호 재정렬
                for (int i = 0; i < lvDimension.Items.Count; i++)
                {
                    lvDimension.Items[i].Text = (i + 1).ToString();
                }

                // 뷰어의 측정 항목 갱신 (AddCustomAxisDistance API 사용)
                vizcore3d.Review.Measure.Clear();
                foreach (var dim in chainDimensionList)
                {
                    VIZCore3D.NET.Data.Vertex3D startVertex = new VIZCore3D.NET.Data.Vertex3D(
                        dim.StartPoint.X, dim.StartPoint.Y, dim.StartPoint.Z);
                    VIZCore3D.NET.Data.Vertex3D endVertex = new VIZCore3D.NET.Data.Vertex3D(
                        dim.EndPoint.X, dim.EndPoint.Y, dim.EndPoint.Z);

                    switch (dim.Axis)
                    {
                        case "X":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                            break;
                        case "Y":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                            break;
                        case "Z":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                            break;
                    }
                }

                MessageBox.Show($"{indicesToRemove.Count}개의 치수가 삭제되었습니다.\n남은 치수: {chainDimensionList.Count}개", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 삭제 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// X축 방향 보기 버튼 (YZ단면 - Y,Z축 치수만 표시)
        /// </summary>
        private void btnShowAxisX_Click(object sender, EventArgs e)
        {
            // 가공도 모드에서 숨긴 부재 복원
            RestoreAllPartsVisibility();
            // 치수 지우고 → 은선 점선 모드 → 카메라 맞추고 → 치수 그리기
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_MINUS);
            vizcore3d.View.FitToView();
            ShowAllDimensions("X");
        }

        /// <summary>
        /// Y축 방향 보기 버튼 (XZ단면 - X,Z축 치수만 표시)
        /// </summary>
        private void btnShowAxisY_Click(object sender, EventArgs e)
        {
            RestoreAllPartsVisibility();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_MINUS);
            vizcore3d.View.FitToView();
            ShowAllDimensions("Y");
        }

        /// <summary>
        /// Z축 방향 보기 버튼 (XY단면 - X,Y축 치수만 표시)
        /// </summary>
        private void btnShowAxisZ_Click(object sender, EventArgs e)
        {
            RestoreAllPartsVisibility();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
            vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS);
            vizcore3d.View.FitToView();
            ShowAllDimensions("Z");
        }

        /// <summary>
        /// 치수 표시 - Smart Dimension Filtering Algorithm 적용
        ///
        /// 적용된 알고리즘:
        /// 1. Priority-Based Filtering: 치수 크기/중요도에 따른 우선순위 할당
        /// 2. Greedy Label Placement: 겹침 방지하면서 우선순위 높은 순으로 배치
        /// 3. Smart Grouping: 연속된 짧은 치수들을 누적 치수로 병합
        /// 4. Multi-Level Layout: 레벨 기반 정렬로 깔끔한 배치
        ///
        /// viewDirection: null=모든 축, "X"/"Y"/"Z"=해당 단면 치수만
        /// </summary>
        private void ShowAllDimensions(string viewDirection = null)
        {
            if (chainDimensionList == null || chainDimensionList.Count == 0) return;

            // 표시할 치수 필터링
            List<ChainDimensionData> displayList;
            if (viewDirection != null)
            {
                List<string> visibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": visibleAxes.Add("Y"); visibleAxes.Add("Z"); break;
                    case "Y": visibleAxes.Add("X"); visibleAxes.Add("Z"); break;
                    case "Z": visibleAxes.Add("X"); visibleAxes.Add("Y"); break;
                }
                displayList = chainDimensionList.Where(d => visibleAxes.Contains(d.Axis)).ToList();
            }
            else
            {
                displayList = chainDimensionList;
            }

            if (displayList.Count == 0) return;

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 측정 항목 및 보조선 제거
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // ========== Smart Dimension Filtering Algorithm 적용 ==========
                // 축당 최대 5개 치수, 텍스트 간 최소 30mm 간격
                var filteredDims = ApplySmartFiltering(displayList, maxDimensionsPerAxis: 5, minTextSpace: 30.0f);

                if (filteredDims.Count == 0)
                {
                    vizcore3d.EndUpdate();
                    return;
                }

                // 측정 스타일 설정 (가독성 향상)
                VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
                measureStyle.Prefix = false;
                measureStyle.Unit = false;
                measureStyle.NumberOfDecimalPlaces = 0;
                measureStyle.DX_DY_DZ = false;
                measureStyle.Frame = false;
                measureStyle.ContinuousDistance = false;
                measureStyle.BackgroundTransparent = true;
                measureStyle.FontColor = System.Drawing.Color.Blue;
                measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                measureStyle.FontBold = true;
                measureStyle.LineColor = System.Drawing.Color.Blue;
                measureStyle.LineWidth = 2;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;
                measureStyle.ArrowSize = 10;
                measureStyle.AssistantLine = true;
                measureStyle.AssistantLineStyle = VIZCore3D.NET.Data.MeasureStyle.AssistantLineType.SOLIDLINE;
                measureStyle.AlignDistanceText = true;
                measureStyle.AlignDistanceTextPosition = 2;
                measureStyle.AlignDistanceTextMargine = 15;
                vizcore3d.Review.Measure.SetStyle(measureStyle);

                // baseline 계산
                float globalMinX = float.MaxValue, globalMinY = float.MaxValue, globalMinZ = float.MaxValue;
                if (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                {
                    foreach (int nodeIdx in xraySelectedNodeIndices)
                    {
                        BOMData bom = bomList.FirstOrDefault(b => b.Index == nodeIdx);
                        if (bom != null)
                        {
                            globalMinX = Math.Min(globalMinX, bom.MinX);
                            globalMinY = Math.Min(globalMinY, bom.MinY);
                            globalMinZ = Math.Min(globalMinZ, bom.MinZ);
                        }
                    }
                }
                if (globalMinX == float.MaxValue)
                {
                    foreach (var dim in filteredDims)
                    {
                        globalMinX = Math.Min(globalMinX, Math.Min(dim.StartPoint.X, dim.EndPoint.X));
                        globalMinY = Math.Min(globalMinY, Math.Min(dim.StartPoint.Y, dim.EndPoint.Y));
                        globalMinZ = Math.Min(globalMinZ, Math.Min(dim.StartPoint.Z, dim.EndPoint.Z));
                    }
                }

                // 동적 오프셋 (치수 개수에 따라 조정)
                float baseOffset = 100.0f;
                float levelSpacing = 60.0f;

                List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                // ========== Level-Based Layout ==========
                var level0Dims = filteredDims.Where(d => d.IsTotal && d.IsVisible).ToList();
                var level1Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0).ToList();
                var level2Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0).ToList();

                // Level 1 치수 (가장 안쪽)
                foreach (var dim in level1Dims)
                {
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        baseOffset, globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines);
                }

                // Level 2 치수 (중간)
                foreach (var dim in level2Dims)
                {
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        baseOffset + levelSpacing, globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines);
                }

                // Level 0 전체 치수 (가장 바깥)
                int maxLevelUsed = level2Dims.Count > 0 ? 2 : 1;
                foreach (var dim in level0Dims)
                {
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        baseOffset + (levelSpacing * maxLevelUsed), globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines);
                }

                // 보조선 그리기 (연한 색상)
                if (extensionLines.Count > 0)
                {
                    vizcore3d.ShapeDrawing.AddLine(extensionLines, 0, System.Drawing.Color.FromArgb(180, 100, 100), 0.5f, true);
                }

                vizcore3d.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"치수 표시 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 단일 치수 그리기 헬퍼 메서드
        /// </summary>
        private void DrawDimension(
            VIZCore3D.NET.Data.Vector3D startPoint,
            VIZCore3D.NET.Data.Vector3D endPoint,
            string axis,
            float offset,
            float globalMinX,
            float globalMinY,
            float globalMinZ,
            string viewDirection,
            List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines)
        {
            // 원본 좌표
            VIZCore3D.NET.Data.Vertex3D originalStart = new VIZCore3D.NET.Data.Vertex3D(
                startPoint.X, startPoint.Y, startPoint.Z);
            VIZCore3D.NET.Data.Vertex3D originalEnd = new VIZCore3D.NET.Data.Vertex3D(
                endPoint.X, endPoint.Y, endPoint.Z);

            // 오프셋 방향 및 baseline 결정
            string offsetDir = "";
            float baseline = 0;

            if (viewDirection == "X" || viewDirection == null)
            {
                switch (axis)
                {
                    case "Z": offsetDir = "Y"; baseline = globalMinY; break;
                    case "Y": offsetDir = "Z"; baseline = globalMinZ; break;
                    case "X": offsetDir = "Y"; baseline = globalMinY; break;
                }
            }
            else if (viewDirection == "Y")
            {
                switch (axis)
                {
                    case "Z": offsetDir = "X"; baseline = globalMinX; break;
                    case "X": offsetDir = "Z"; baseline = globalMinZ; break;
                }
            }
            else if (viewDirection == "Z")
            {
                switch (axis)
                {
                    case "Y": offsetDir = "X"; baseline = globalMinX; break;
                    case "X": offsetDir = "Y"; baseline = globalMinY; break;
                }
            }

            // baseline에서 -offset 방향으로 치수 위치 계산
            float offsetValue = baseline - offset;
            VIZCore3D.NET.Data.Vertex3D startVertex;
            VIZCore3D.NET.Data.Vertex3D endVertex;

            switch (offsetDir)
            {
                case "X":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(offsetValue, startPoint.Y, startPoint.Z);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(offsetValue, endPoint.Y, endPoint.Z);
                    break;
                case "Y":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(startPoint.X, offsetValue, startPoint.Z);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(endPoint.X, offsetValue, endPoint.Z);
                    break;
                case "Z":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(startPoint.X, startPoint.Y, offsetValue);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(endPoint.X, endPoint.Y, offsetValue);
                    break;
                default:
                    return;
            }

            // 치수 추가
            switch (axis)
            {
                case "X":
                    vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                    break;
                case "Y":
                    vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                    break;
                case "Z":
                    vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                    break;
            }

            // 보조선 추가 (원본 → baseline 오프셋 위치)
            var extLine1 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine1.Add(originalStart);
            extLine1.Add(startVertex);
            extensionLines.Add(extLine1);

            var extLine2 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine2.Add(originalEnd);
            extLine2.Add(endVertex);
            extensionLines.Add(extLine2);
        }

        /// <summary>
        /// 제작에 필요한 최소 치수만 선택 (중복 제거, 필수 치수만 유지)
        /// </summary>
        #region Smart Dimension Filtering Algorithm (스마트 치수 필터링 알고리즘)

        /// <summary>
        /// 치수 우선순위 계산 및 할당
        /// Priority-Based Filtering Algorithm 적용
        /// </summary>
        private void AssignDimensionPriorities(List<ChainDimensionData> dimensions)
        {
            if (dimensions == null || dimensions.Count == 0) return;

            // 축별로 그룹화하여 우선순위 계산
            var groupedByAxis = dimensions.GroupBy(d => d.Axis);

            foreach (var axisGroup in groupedByAxis)
            {
                var axisDims = axisGroup.ToList();
                if (axisDims.Count == 0) continue;

                // 거리값 통계 계산
                float maxDistance = axisDims.Max(d => d.Distance);
                float minDistance = axisDims.Min(d => d.Distance);
                float avgDistance = axisDims.Average(d => d.Distance);
                float range = maxDistance - minDistance;

                foreach (var dim in axisDims)
                {
                    if (dim.IsTotal)
                    {
                        // 전체 길이: 최고 우선순위
                        dim.Priority = 10;
                    }
                    else if (range > 0)
                    {
                        // 상대적 크기에 따른 우선순위 (정규화)
                        float normalizedSize = (dim.Distance - minDistance) / range;

                        if (normalizedSize >= 0.7f)
                        {
                            // 상위 30%: 주요 구간
                            dim.Priority = 8;
                        }
                        else if (normalizedSize >= 0.4f)
                        {
                            // 중간 30%: 중간 구간
                            dim.Priority = 5;
                        }
                        else if (normalizedSize >= 0.15f)
                        {
                            // 하위 25%: 작은 구간
                            dim.Priority = 3;
                        }
                        else
                        {
                            // 최하위 15%: 매우 작은 구간
                            dim.Priority = 1;
                        }
                    }
                    else
                    {
                        // 모든 치수가 같은 크기
                        dim.Priority = 5;
                    }
                }
            }
        }

        /// <summary>
        /// 스마트 치수 필터링: 겹침 방지 및 가독성 향상
        /// Greedy Label Placement Algorithm 기반
        /// </summary>
        /// <param name="dimensions">전체 치수 목록</param>
        /// <param name="maxDimensionsPerAxis">축당 최대 표시 치수 개수</param>
        /// <param name="minTextSpace">치수 텍스트 간 최소 간격 (mm)</param>
        /// <returns>필터링된 치수 목록</returns>
        private List<ChainDimensionData> ApplySmartFiltering(
            List<ChainDimensionData> dimensions,
            int maxDimensionsPerAxis = 6,
            float minTextSpace = 25.0f)
        {
            if (dimensions == null || dimensions.Count == 0)
                return new List<ChainDimensionData>();

            // 우선순위 할당
            AssignDimensionPriorities(dimensions);

            var result = new List<ChainDimensionData>();
            var groupedByAxis = dimensions.GroupBy(d => d.Axis);

            foreach (var axisGroup in groupedByAxis)
            {
                var axisDims = axisGroup.ToList();
                var selectedDims = new List<ChainDimensionData>();

                // 1단계: 전체 치수(IsTotal)는 무조건 포함
                var totalDims = axisDims.Where(d => d.IsTotal).ToList();
                selectedDims.AddRange(totalDims);

                // 2단계: 나머지 치수를 우선순위 순으로 정렬
                var sequentialDims = axisDims
                    .Where(d => !d.IsTotal)
                    .OrderByDescending(d => d.Priority)
                    .ThenByDescending(d => d.Distance)
                    .ToList();

                // 3단계: 연속된 짧은 치수 병합 (Smart Grouping)
                var mergedDims = MergeShortDimensions(sequentialDims, minTextSpace);

                // 4단계: Greedy 선택 - 겹침 방지하면서 우선순위 높은 순으로 선택
                var placedPositions = new List<(float start, float end)>();

                foreach (var dim in mergedDims.OrderByDescending(d => d.Priority).ThenByDescending(d => d.Distance))
                {
                    if (selectedDims.Count(d => !d.IsTotal) >= maxDimensionsPerAxis - 1)
                        break;

                    float dimStart = GetAxisValue(dim.StartPoint, axisGroup.Key);
                    float dimEnd = GetAxisValue(dim.EndPoint, axisGroup.Key);
                    float dimMin = Math.Min(dimStart, dimEnd);
                    float dimMax = Math.Max(dimStart, dimEnd);

                    // 텍스트 중앙 위치 계산
                    float dimCenter = (dimMin + dimMax) / 2;

                    // 기존 배치된 치수와 텍스트 겹침 체크
                    bool hasOverlap = false;
                    foreach (var placed in placedPositions)
                    {
                        float placedCenter = (placed.start + placed.end) / 2;

                        // 텍스트 간 최소 거리 확인
                        if (Math.Abs(dimCenter - placedCenter) < minTextSpace)
                        {
                            hasOverlap = true;
                            break;
                        }
                    }

                    if (!hasOverlap)
                    {
                        dim.IsVisible = true;
                        dim.DisplayLevel = 0;
                        selectedDims.Add(dim);
                        placedPositions.Add((dimMin, dimMax));
                    }
                    else
                    {
                        // 겹치면 다음 레벨로 배정 (최대 2레벨까지)
                        if (dim.Priority >= 5 && dim.DisplayLevel < 2)
                        {
                            dim.DisplayLevel = 1;
                            dim.IsVisible = true;
                            selectedDims.Add(dim);
                        }
                        else
                        {
                            dim.IsVisible = false;
                        }
                    }
                }

                result.AddRange(selectedDims);
            }

            return result;
        }

        /// <summary>
        /// 연속된 짧은 치수들을 하나의 누적 치수로 병합
        /// </summary>
        private List<ChainDimensionData> MergeShortDimensions(List<ChainDimensionData> dimensions, float minLength)
        {
            if (dimensions == null || dimensions.Count == 0)
                return new List<ChainDimensionData>();

            var result = new List<ChainDimensionData>();
            var shortGroup = new List<ChainDimensionData>();

            // 위치 순으로 정렬
            var sortedDims = dimensions.OrderByDescending(d =>
            {
                switch (d.Axis)
                {
                    case "X": return d.StartPoint.X;
                    case "Y": return d.StartPoint.Y;
                    case "Z": return d.StartPoint.Z;
                    default: return 0f;
                }
            }).ToList();

            foreach (var dim in sortedDims)
            {
                if (dim.Distance < minLength)
                {
                    // 짧은 치수 → 그룹에 추가
                    shortGroup.Add(dim);
                }
                else
                {
                    // 긴 치수 발견 → 이전 짧은 그룹 병합 후 추가
                    if (shortGroup.Count > 1)
                    {
                        var mergedDim = CreateMergedDimension(shortGroup);
                        if (mergedDim != null)
                            result.Add(mergedDim);
                    }
                    else if (shortGroup.Count == 1)
                    {
                        // 단일 짧은 치수는 그대로 추가 (우선순위 낮춤)
                        shortGroup[0].Priority = Math.Max(1, shortGroup[0].Priority - 2);
                        result.Add(shortGroup[0]);
                    }

                    shortGroup.Clear();
                    result.Add(dim);
                }
            }

            // 마지막 그룹 처리
            if (shortGroup.Count > 1)
            {
                var mergedDim = CreateMergedDimension(shortGroup);
                if (mergedDim != null)
                    result.Add(mergedDim);
            }
            else if (shortGroup.Count == 1)
            {
                shortGroup[0].Priority = Math.Max(1, shortGroup[0].Priority - 2);
                result.Add(shortGroup[0]);
            }

            return result;
        }

        /// <summary>
        /// 여러 짧은 치수를 하나의 병합 치수로 생성
        /// </summary>
        private ChainDimensionData CreateMergedDimension(List<ChainDimensionData> shortDims)
        {
            if (shortDims == null || shortDims.Count < 2)
                return null;

            string axis = shortDims[0].Axis;

            // 시작점과 끝점 결정 (전체 범위)
            VIZCore3D.NET.Data.Vector3D startPoint = shortDims[0].StartPoint;
            VIZCore3D.NET.Data.Vector3D endPoint = shortDims[shortDims.Count - 1].EndPoint;

            // 위치 순 정렬 후 처음과 끝 선택
            switch (axis)
            {
                case "X":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.X).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.X).First().EndPoint;
                    break;
                case "Y":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.Y).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.Y).First().EndPoint;
                    break;
                case "Z":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.Z).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.Z).First().EndPoint;
                    break;
            }

            float totalDistance = 0;
            switch (axis)
            {
                case "X": totalDistance = Math.Abs(startPoint.X - endPoint.X); break;
                case "Y": totalDistance = Math.Abs(startPoint.Y - endPoint.Y); break;
                case "Z": totalDistance = Math.Abs(startPoint.Z - endPoint.Z); break;
            }

            return new ChainDimensionData
            {
                Axis = axis,
                ViewName = shortDims[0].ViewName,
                Distance = totalDistance,
                StartPoint = startPoint,
                EndPoint = endPoint,
                StartPointStr = $"({startPoint.X:F1}, {startPoint.Y:F1}, {startPoint.Z:F1})",
                EndPointStr = $"({endPoint.X:F1}, {endPoint.Y:F1}, {endPoint.Z:F1})",
                IsTotal = false,
                IsMerged = true,
                Priority = 6  // 병합 치수는 중간 높은 우선순위
            };
        }

        /// <summary>
        /// 포인트에서 축 값 추출
        /// </summary>
        private float GetAxisValue(VIZCore3D.NET.Data.Vector3D point, string axis)
        {
            switch (axis)
            {
                case "X": return point.X;
                case "Y": return point.Y;
                case "Z": return point.Z;
                default: return 0f;
            }
        }

        #endregion

        /// <summary>
        /// Clash 리스트 선택 변경 시 뷰어에서 해당 충돌 지점 표시 및 관련 Osnap/치수 자동 선택
        /// </summary>
        private void LvClash_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0) return;

            try
            {
                // 관련 노드 이름 수집
                HashSet<string> relatedNodeNames = new HashSet<string>();
                // 관련 바운딩 박스 영역 수집
                List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> relatedBounds = new List<(float, float, float, float, float, float)>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        // 노드 이름 추가
                        if (!string.IsNullOrEmpty(clash.Name1))
                            relatedNodeNames.Add(clash.Name1);
                        if (!string.IsNullOrEmpty(clash.Name2))
                            relatedNodeNames.Add(clash.Name2);

                        BOMData bom1 = bomList.FirstOrDefault(b => b.Index == clash.Index1);
                        BOMData bom2 = bomList.FirstOrDefault(b => b.Index == clash.Index2);


                        if (bom1 != null && bom2 != null)
                        {
                            // 두 부재의 결합 바운딩 박스 저장
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

                // 관련 Osnap 좌표 자동 선택
                SelectRelatedOsnapItems(relatedNodeNames, relatedBounds);

                // 관련 치수 자동 선택
                SelectRelatedDimensionItems(relatedBounds);
            }
            catch
            {
                // 선택 변경 중 오류는 무시
            }
        }

        /// <summary>
        /// Clash와 관련된 Osnap 항목 자동 선택
        /// </summary>
        private void SelectRelatedOsnapItems(HashSet<string> nodeNames, List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> bounds)
        {
            if (lvOsnap.Items.Count == 0) return;

            // 기존 선택 해제
            foreach (ListViewItem item in lvOsnap.SelectedItems)
            {
                item.Selected = false;
            }

            float tolerance = 1.0f; // 허용 오차

            // 관련 Osnap 항목 선택
            for (int i = 0; i < lvOsnap.Items.Count; i++)
            {
                ListViewItem lvi = lvOsnap.Items[i];

                // 부재 이름으로 매칭
                string osnapNodeName = lvi.SubItems.Count > 1 ? lvi.SubItems[1].Text : "";
                if (nodeNames.Contains(osnapNodeName))
                {
                    lvi.Selected = true;
                    continue;
                }

                // 좌표가 바운딩 박스 내에 있는지 확인
                if (i < osnapPoints.Count)
                {
                    var point = osnapPoints[i];
                    foreach (var bound in bounds)
                    {
                        if (point.X >= bound.MinX - tolerance && point.X <= bound.MaxX + tolerance &&
                            point.Y >= bound.MinY - tolerance && point.Y <= bound.MaxY + tolerance &&
                            point.Z >= bound.MinZ - tolerance && point.Z <= bound.MaxZ + tolerance)
                        {
                            lvi.Selected = true;
                            break;
                        }
                    }
                }
            }

            // 첫 번째 선택 항목으로 스크롤
            if (lvOsnap.SelectedItems.Count > 0)
            {
                lvOsnap.SelectedItems[0].EnsureVisible();
            }
        }

        /// <summary>
        /// Clash와 관련된 치수 항목 자동 선택
        /// </summary>
        private void SelectRelatedDimensionItems(List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> bounds)
        {
            if (lvDimension.Items.Count == 0 || bounds.Count == 0) return;

            // 기존 선택 해제
            foreach (ListViewItem item in lvDimension.SelectedItems)
            {
                item.Selected = false;
            }

            float tolerance = 1.0f; // 허용 오차

            // 관련 치수 항목 선택
            foreach (ListViewItem lvi in lvDimension.Items)
            {
                ChainDimensionData dim = lvi.Tag as ChainDimensionData;
                if (dim == null) continue;

                // 치수의 시작점 또는 끝점이 바운딩 박스 내에 있는지 확인
                foreach (var bound in bounds)
                {
                    bool startInBound =
                        dim.StartPoint.X >= bound.MinX - tolerance && dim.StartPoint.X <= bound.MaxX + tolerance &&
                        dim.StartPoint.Y >= bound.MinY - tolerance && dim.StartPoint.Y <= bound.MaxY + tolerance &&
                        dim.StartPoint.Z >= bound.MinZ - tolerance && dim.StartPoint.Z <= bound.MaxZ + tolerance;

                    bool endInBound =
                        dim.EndPoint.X >= bound.MinX - tolerance && dim.EndPoint.X <= bound.MaxX + tolerance &&
                        dim.EndPoint.Y >= bound.MinY - tolerance && dim.EndPoint.Y <= bound.MaxY + tolerance &&
                        dim.EndPoint.Z >= bound.MinZ - tolerance && dim.EndPoint.Z <= bound.MaxZ + tolerance;

                    if (startInBound || endInBound)
                    {
                        lvi.Selected = true;
                        break;
                    }
                }
            }

            // 첫 번째 선택 항목으로 스크롤
            if (lvDimension.SelectedItems.Count > 0)
            {
                lvDimension.SelectedItems[0].EnsureVisible();
            }
        }


        /// <summary>
        /// 체인 치수 데이터 리스트
        /// </summary>
        private List<ChainDimensionData> chainDimensionList = new List<ChainDimensionData>();

        /// <summary>
        /// 체인 치수 추출 (MeasureManager API 사용)
        /// </summary>
        private void btnExtractDimension_Click(object sender, EventArgs e)
        {
            if (osnapPointsWithNames == null || osnapPointsWithNames.Count == 0)
            {
                MessageBox.Show("먼저 Osnap 좌표를 수집해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                // ListView에 추가
                int no = 1;
                foreach (var dim in chainDimensionList)
                {
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

                // 결과 출력
                string result = $"체인 치수 추출 완료!\n\n" +
                               $"총 Osnap 좌표: {osnapPointsWithNames.Count}개\n" +
                               $"병합 후 좌표: {mergedPoints.Count}개\n\n" +
                               $"X축 방향 치수: {xDimensions.Count}개\n" +
                               $"Y축 방향 치수: {yDimensions.Count}개\n" +
                               $"Z축 방향 치수: {zDimensions.Count}개\n\n" +
                               $"총 치수: {chainDimensionList.Count}개";

                MessageBox.Show(result, "치수 추출 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 치수 추출 후 자동으로 모든 치수 표시 (오프셋 + 보조선 스타일)
                ShowAllDimensions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 추출 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 좌표 병합 (허용 오차 내 같은 좌표로 그룹화)
        /// </summary>
        private List<VIZCore3D.NET.Data.Vector3D> MergeCoordinates(
            List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> points, float tolerance)
        {
            List<VIZCore3D.NET.Data.Vector3D> result = new List<VIZCore3D.NET.Data.Vector3D>();

            foreach (var pt in points)
            {
                float x = RoundToTolerance(pt.point.X, tolerance);
                float y = RoundToTolerance(pt.point.Y, tolerance);
                float z = RoundToTolerance(pt.point.Z, tolerance);

                // 중복 제거
                bool exists = result.Any(r =>
                    Math.Abs(r.X - x) < tolerance &&
                    Math.Abs(r.Y - y) < tolerance &&
                    Math.Abs(r.Z - z) < tolerance);

                if (!exists)
                {
                    result.Add(new VIZCore3D.NET.Data.Vector3D(x, y, z));
                }
            }

            return result;
        }

        /// <summary>
        /// 허용 오차 기준으로 좌표 반올림
        /// </summary>
        private float RoundToTolerance(float value, float tolerance)
        {
            return (float)(Math.Round(value / tolerance) * tolerance);
        }

        /// <summary>
        /// 축에 따른 뷰 이름 반환
        /// </summary>
        private string GetViewNameByAxis(string axis)
        {
            switch (axis)
            {
                case "X": return "측면도";
                case "Y": return "정면도";
                case "Z": return "평면도";
                default: return "";
            }
        }

        /// <summary>
        /// 특정 축 방향 체인 치수 추가
        /// 1. 같은 측정축 값의 포인트 중 필터축 최소값만 남김
        ///    Z치수→min Y, Y치수→min X, X치수→min Z
        /// 2. 큰 값에서 작은 값 순서로 순차 치수
        /// 3. 마지막에 전체 치수 (처음~끝)
        /// </summary>
        private List<ChainDimensionData> AddChainDimensionByAxis(List<VIZCore3D.NET.Data.Vector3D> points, string axis, float tolerance)
        {
            List<ChainDimensionData> dimensions = new List<ChainDimensionData>();

            if (points == null || points.Count < 2) return dimensions;

            // Step 1: 측정축 값으로 그룹화하고, 같은 값이면 필터축 최소값만 남김
            // Z치수 → 같은 Z면 min Y만 남김
            // Y치수 → 같은 Y면 min X만 남김
            // X치수 → 같은 X면 min Z만 남김
            var grouped = new Dictionary<string, VIZCore3D.NET.Data.Vector3D>();

            foreach (var pt in points)
            {
                float dimValue = 0;
                float filterValue = 0;

                switch (axis)
                {
                    case "X":
                        dimValue = RoundToTolerance(pt.X, tolerance);
                        filterValue = pt.Z;
                        break;
                    case "Y":
                        dimValue = RoundToTolerance(pt.Y, tolerance);
                        filterValue = pt.X;
                        break;
                    case "Z":
                        dimValue = RoundToTolerance(pt.Z, tolerance);
                        filterValue = pt.Y;
                        break;
                }

                string key = dimValue.ToString("F1");

                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = pt;
                }
                else
                {
                    // 기존 포인트의 필터축 값과 비교하여 더 작은 것만 유지
                    float existingFilterValue = 0;
                    switch (axis)
                    {
                        case "X": existingFilterValue = grouped[key].Z; break;
                        case "Y": existingFilterValue = grouped[key].X; break;
                        case "Z": existingFilterValue = grouped[key].Y; break;
                    }

                    if (filterValue < existingFilterValue)
                    {
                        grouped[key] = pt;
                    }
                }
            }

            // Step 2: 측정축 값 기준 내림차순 정렬 (큰 값부터)
            List<VIZCore3D.NET.Data.Vector3D> sortedPoints;
            switch (axis)
            {
                case "X":
                    sortedPoints = grouped.Values.OrderByDescending(p => p.X).ToList();
                    break;
                case "Y":
                    sortedPoints = grouped.Values.OrderByDescending(p => p.Y).ToList();
                    break;
                case "Z":
                    sortedPoints = grouped.Values.OrderByDescending(p => p.Z).ToList();
                    break;
                default:
                    sortedPoints = grouped.Values.ToList();
                    break;
            }

            if (sortedPoints.Count < 2) return dimensions;

            // Step 3: 큰 값에서 작은 값으로 순차 치수 그리기
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                float distance = 0;
                switch (axis)
                {
                    case "X": distance = Math.Abs(sortedPoints[i].X - sortedPoints[i + 1].X); break;
                    case "Y": distance = Math.Abs(sortedPoints[i].Y - sortedPoints[i + 1].Y); break;
                    case "Z": distance = Math.Abs(sortedPoints[i].Z - sortedPoints[i + 1].Z); break;
                }

                if (distance > tolerance)
                {
                    ChainDimensionData dimData = new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = distance,
                        StartPoint = sortedPoints[i],
                        EndPoint = sortedPoints[i + 1],
                        StartPointStr = $"({sortedPoints[i].X:F1}, {sortedPoints[i].Y:F1}, {sortedPoints[i].Z:F1})",
                        EndPointStr = $"({sortedPoints[i + 1].X:F1}, {sortedPoints[i + 1].Y:F1}, {sortedPoints[i + 1].Z:F1})"
                    };
                    dimensions.Add(dimData);
                }
            }

            // Step 4: 전체 치수 (처음 포인트 ~ 끝 포인트) - 순차 치수가 2개 이상일 때
            if (sortedPoints.Count > 2)
            {
                var first = sortedPoints[0];
                var last = sortedPoints[sortedPoints.Count - 1];

                float totalDistance = 0;
                switch (axis)
                {
                    case "X": totalDistance = Math.Abs(first.X - last.X); break;
                    case "Y": totalDistance = Math.Abs(first.Y - last.Y); break;
                    case "Z": totalDistance = Math.Abs(first.Z - last.Z); break;
                }

                if (totalDistance > tolerance)
                {
                    ChainDimensionData totalDim = new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        Distance = totalDistance,
                        StartPoint = first,
                        EndPoint = last,
                        StartPointStr = $"({first.X:F1}, {first.Y:F1}, {first.Z:F1})",
                        EndPointStr = $"({last.X:F1}, {last.Y:F1}, {last.Z:F1})",
                        IsTotal = true
                    };
                    dimensions.Add(totalDim);
                }
            }

            return dimensions;
        }

        #region 부재 정보 탭 - 속성 조회 기능

        /// <summary>
        /// 3D 객체 선택 이벤트 핸들러
        /// </summary>
        private void Object3D_OnObject3DSelected(object sender, VIZCore3D.NET.Event.EventManager.Object3DSelectedEventArgs e)
        {
            // 선택된 노드 가져오기
            var selectedNodes = vizcore3d.Object3D.FromFilter(VIZCore3D.NET.Data.Object3dFilter.SELECTED_TOP);

            if (selectedNodes == null || selectedNodes.Count == 0)
            {
                ClearAttributeTable();
                return;
            }

            // 첫 번째 선택된 노드의 속성 표시
            var node = selectedNodes[0];
            selectedAttributeNodeIndex = node.Index;

            // 노드 정보 표시
            lblSelectedNode.Text = $"[{node.Index}] {node.NodeName}";

            // 속성 테이블 업데이트
            UpdateAttributeTable(node.Index);
        }

        /// <summary>
        /// 속성 테이블 업데이트
        /// </summary>
        private void UpdateAttributeTable(int nodeIndex)
        {
            dgvAttributes.Rows.Clear();

            try
            {
                // 1. 기본 노드 정보 추가
                AddBasicNodeInfo(nodeIndex);

                // 2. 바운딩 박스 정보 추가
                AddBoundingBoxInfo(nodeIndex);

                // 3. UDA (User Defined Attributes) 추가
                AddUDAInfo(nodeIndex);

                // 4. 지오메트리 속성 추가
                AddGeometryPropertyInfo(nodeIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"속성 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기본 노드 정보 추가
        /// </summary>
        private void AddBasicNodeInfo(int nodeIndex)
        {
            var node = vizcore3d.Object3D.FromIndex(nodeIndex);
            if (node == null) return;

            int rowNum = 1;

            // 구분선 추가
            AddSectionHeader("기본 정보");

            dgvAttributes.Rows.Add(rowNum++, "Node Index", nodeIndex.ToString());
            dgvAttributes.Rows.Add(rowNum++, "Node Name", node.NodeName);
            dgvAttributes.Rows.Add(rowNum++, "Node Type", node.Kind.ToString());

            // 노드 경로에서 부모 정보 추출 시도
            try
            {
                string nodePath = node.NodeName;
                if (nodePath.Contains("/"))
                {
                    string parentPath = nodePath.Substring(0, nodePath.LastIndexOf("/"));
                    dgvAttributes.Rows.Add(rowNum++, "Parent Path", parentPath);
                }
            }
            catch { }
        }

        /// <summary>
        /// 바운딩 박스 정보 추가
        /// </summary>
        private void AddBoundingBoxInfo(int nodeIndex)
        {
            List<int> indices = new List<int> { nodeIndex };
            var bbox = vizcore3d.Object3D.GetBoundBox(indices, false);

            if (bbox == null) return;

            AddSectionHeader("바운딩 박스 (Bounding Box)");

            int rowNum = dgvAttributes.Rows.Count + 1;

            dgvAttributes.Rows.Add(rowNum++, "Min X", bbox.MinX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Min Y", bbox.MinY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Min Z", bbox.MinZ.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max X", bbox.MaxX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max Y", bbox.MaxY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max Z", bbox.MaxZ.ToString("F2"));

            // 크기 계산
            float sizeX = bbox.MaxX - bbox.MinX;
            float sizeY = bbox.MaxY - bbox.MinY;
            float sizeZ = bbox.MaxZ - bbox.MinZ;

            dgvAttributes.Rows.Add(rowNum++, "Size X", sizeX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Size Y", sizeY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Size Z", sizeZ.ToString("F2"));

            // 중심점
            float centerX = (bbox.MinX + bbox.MaxX) / 2;
            float centerY = (bbox.MinY + bbox.MaxY) / 2;
            float centerZ = (bbox.MinZ + bbox.MaxZ) / 2;

            dgvAttributes.Rows.Add(rowNum++, "Center", $"({centerX:F2}, {centerY:F2}, {centerZ:F2})");
        }

        /// <summary>
        /// UDA (User Defined Attributes) 정보 추가
        /// </summary>
        private void AddUDAInfo(int nodeIndex)
        {
            try
            {
                // UDA 키 목록 가져오기
                var udaKeys = vizcore3d.Object3D.UDA.Keys;

                if (udaKeys == null || udaKeys.Count == 0)
                    return;

                AddSectionHeader("사용자 정의 속성 (UDA)");

                int rowNum = dgvAttributes.Rows.Count + 1;

                foreach (string key in udaKeys)
                {
                    try
                    {
                        // 해당 노드의 UDA 값 가져오기
                        var udaData = vizcore3d.Object3D.UDA.FromIndex(nodeIndex, key);

                        if (udaData != null && !string.IsNullOrEmpty(udaData.ToString()))
                        {
                            dgvAttributes.Rows.Add(rowNum++, key, udaData.ToString());
                        }
                    }
                    catch
                    {
                        // 해당 키에 대한 값이 없으면 스킵
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UDA 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 지오메트리 속성 정보 추가
        /// </summary>
        private void AddGeometryPropertyInfo(int nodeIndex)
        {
            try
            {
                // 지오메트리 속성이 있는지 확인
                var geomProps = vizcore3d.Object3D.GeometryProperty;
                if (geomProps == null) return;

                // 노드의 지오메트리 속성 가져오기
                var props = geomProps.FromIndex(nodeIndex);
                if (props == null) return;

                // 속성이 있으면 섹션 추가
                bool hasProps = false;

                // 리플렉션으로 속성 가져오기
                var propsType = props.GetType();
                var properties = propsType.GetProperties();

                foreach (var propInfo in properties)
                {
                    try
                    {
                        var value = propInfo.GetValue(props);
                        if (value != null && !string.IsNullOrEmpty(value.ToString()))
                        {
                            if (!hasProps)
                            {
                                AddSectionHeader("지오메트리 속성 (Geometry)");
                                hasProps = true;
                            }
                            int rowNum = dgvAttributes.Rows.Count + 1;
                            dgvAttributes.Rows.Add(rowNum, propInfo.Name, value.ToString());
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geometry Property 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 섹션 헤더 추가 (구분선)
        /// </summary>
        private void AddSectionHeader(string sectionName)
        {
            int rowIndex = dgvAttributes.Rows.Add("", $"━━ {sectionName} ━━", "");
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230);
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.Font = new Font("맑은 고딕", 9, FontStyle.Bold);
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
        }

        /// <summary>
        /// 속성 테이블 초기화
        /// </summary>
        private void ClearAttributeTable()
        {
            dgvAttributes.Rows.Clear();
            lblSelectedNode.Text = "3D 뷰어에서 부재를 선택하세요";
            selectedAttributeNodeIndex = -1;
        }

        /// <summary>
        /// 선택 해제 버튼 클릭
        /// </summary>
        private void btnClearSelection_Click(object sender, EventArgs e)
        {
            vizcore3d.Object3D.Select(new List<int>(), false, false);
            ClearAttributeTable();
        }

        /// <summary>
        /// CSV 내보내기 버튼 클릭
        /// </summary>
        private void btnExportAttributeCSV_Click(object sender, EventArgs e)
        {
            if (dgvAttributes.Rows.Count == 0)
            {
                MessageBox.Show("내보낼 속성이 없습니다.\n부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "CSV 파일 (*.csv)|*.csv";
            dlg.FileName = $"Attributes_{selectedAttributeNodeIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using (var writer = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                {
                    // 헤더
                    writer.WriteLine("No,Key,Value");

                    // 데이터
                    foreach (DataGridViewRow row in dgvAttributes.Rows)
                    {
                        string no = row.Cells["No"].Value?.ToString() ?? "";
                        string key = row.Cells["Key"].Value?.ToString() ?? "";
                        string value = row.Cells["Value"].Value?.ToString() ?? "";

                        // CSV 이스케이프
                        key = key.Contains(",") ? $"\"{key}\"" : key;
                        value = value.Contains(",") ? $"\"{value}\"" : value;

                        writer.WriteLine($"{no},{key},{value}");
                    }
                }

                MessageBox.Show($"CSV 저장 완료:\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 저장 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 입력/편집 다이얼로그 표시
        /// </summary>
        private Tuple<string, string> ShowUdaInputDialog(string title, string defaultKey, string defaultValue, bool keyReadOnly = false)
        {
            Form dlg = new Form();
            dlg.Text = title;
            dlg.Size = new Size(350, 180);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            Label lblKey = new Label() { Text = "Key:", Location = new Point(15, 20), Size = new Size(50, 20) };
            TextBox txtKey = new TextBox() { Location = new Point(70, 17), Size = new Size(245, 22), Text = defaultKey, ReadOnly = keyReadOnly };
            Label lblValue = new Label() { Text = "Value:", Location = new Point(15, 55), Size = new Size(50, 20) };
            TextBox txtValue = new TextBox() { Location = new Point(70, 52), Size = new Size(245, 22), Text = defaultValue };

            Button btnOk = new Button() { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(155, 95), Size = new Size(75, 28) };
            Button btnCancel = new Button() { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(240, 95), Size = new Size(75, 28) };

            dlg.Controls.AddRange(new Control[] { lblKey, txtKey, lblValue, txtValue, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string key = txtKey.Text.Trim();
                string value = txtValue.Text.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("Key를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                return Tuple.Create(key, value);
            }
            return null;
        }

        /// <summary>
        /// 선택된 행이 UDA 섹션의 데이터 행인지 판별
        /// </summary>
        private bool IsUdaRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvAttributes.Rows.Count)
                return false;

            // 위로 탐색하여 가장 가까운 섹션 헤더 찾기
            for (int i = rowIndex - 1; i >= 0; i--)
            {
                string keyVal = dgvAttributes.Rows[i].Cells["Key"].Value?.ToString() ?? "";
                if (keyVal.Contains("━━"))
                {
                    return keyVal.Contains("사용자 정의 속성 (UDA)");
                }
            }
            return false;
        }

        /// <summary>
        /// UDA 추가 버튼 클릭
        /// </summary>
        private void btnUdaAdd_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = ShowUdaInputDialog("UDA 추가", "", "");
            if (result == null) return;

            try
            {
                vizcore3d.Object3D.UDA.Add(selectedAttributeNodeIndex, result.Item1, result.Item2, true);
                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 추가 완료: {result.Item1} = {result.Item2}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 추가 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 편집 버튼 클릭
        /// </summary>
        private void btnUdaEdit_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvAttributes.SelectedRows.Count == 0)
            {
                MessageBox.Show("편집할 UDA 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIndex = dgvAttributes.SelectedRows[0].Index;
            if (!IsUdaRow(rowIndex))
            {
                MessageBox.Show("UDA 항목만 편집할 수 있습니다.\nUDA 섹션의 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string oldKey = dgvAttributes.Rows[rowIndex].Cells["Key"].Value?.ToString() ?? "";
            string oldValue = dgvAttributes.Rows[rowIndex].Cells["Value"].Value?.ToString() ?? "";

            var result = ShowUdaInputDialog("UDA 편집", oldKey, oldValue);
            if (result == null) return;

            try
            {
                string newKey = result.Item1;
                string newValue = result.Item2;

                if (newKey != oldKey)
                {
                    vizcore3d.Object3D.UDA.UpdateKey(selectedAttributeNodeIndex, oldKey, newKey, true);
                }
                if (newValue != oldValue || newKey != oldKey)
                {
                    vizcore3d.Object3D.UDA.Update(selectedAttributeNodeIndex, newKey, newValue, true);
                }

                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 편집 완료: {newKey} = {newValue}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 편집 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 삭제 버튼 클릭
        /// </summary>
        private void btnUdaDelete_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvAttributes.SelectedRows.Count == 0)
            {
                MessageBox.Show("삭제할 UDA 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIndex = dgvAttributes.SelectedRows[0].Index;
            if (!IsUdaRow(rowIndex))
            {
                MessageBox.Show("UDA 항목만 삭제할 수 있습니다.\nUDA 섹션의 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string key = dgvAttributes.Rows[rowIndex].Cells["Key"].Value?.ToString() ?? "";

            if (MessageBox.Show($"UDA '{key}'를 삭제하시겠습니까?", "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                vizcore3d.Object3D.UDA.Delete(selectedAttributeNodeIndex, key, true);
                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 삭제 완료: {key}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 삭제 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// CSV 파일에서 UDA 일괄 가져오기
        /// CSV 형식: Key,Value (첫 행이 헤더면 자동 스킵)
        /// </summary>
        private void btnUdaImportCSV_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
            dlg.Title = "UDA CSV 파일 선택";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(dlg.FileName, System.Text.Encoding.UTF8);

                if (lines.Length == 0)
                {
                    MessageBox.Show("CSV 파일이 비어있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int startLine = 0;
                // 첫 행이 헤더인지 확인 (Key, Value 등의 문자가 포함되면 스킵)
                string firstLine = lines[0].Trim().ToLower();
                if (firstLine.Contains("key") || firstLine.Contains("value") || firstLine.Contains("속성"))
                {
                    startLine = 1;
                }

                int successCount = 0;
                int failCount = 0;
                List<string> errors = new List<string>();

                for (int i = startLine; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // CSV 파싱 (쉼표 구분, 따옴표 처리)
                    string[] parts = ParseCsvLine(line);

                    if (parts.Length < 2)
                    {
                        failCount++;
                        errors.Add($"Line {i + 1}: 컬럼 부족 - \"{line}\"");
                        continue;
                    }

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (string.IsNullOrEmpty(key))
                    {
                        failCount++;
                        errors.Add($"Line {i + 1}: Key가 비어있음");
                        continue;
                    }

                    try
                    {
                        vizcore3d.Object3D.UDA.Add(selectedAttributeNodeIndex, key, value, true);
                        successCount++;
                    }
                    catch
                    {
                        // Add 실패 시 Update 시도 (이미 존재하는 키일 수 있음)
                        try
                        {
                            vizcore3d.Object3D.UDA.Update(selectedAttributeNodeIndex, key, value, true);
                            successCount++;
                        }
                        catch (Exception ex2)
                        {
                            failCount++;
                            errors.Add($"Line {i + 1}: {key} - {ex2.Message}");
                        }
                    }
                }

                UpdateAttributeTable(selectedAttributeNodeIndex);

                string msg = $"CSV 가져오기 완료\n성공: {successCount}건, 실패: {failCount}건";
                if (errors.Count > 0)
                {
                    msg += "\n\n오류 상세:\n" + string.Join("\n", errors.GetRange(0, Math.Min(errors.Count, 10)));
                    if (errors.Count > 10)
                        msg += $"\n... 외 {errors.Count - 10}건";
                }
                MessageBox.Show(msg, "CSV 가져오기", MessageBoxButtons.OK,
                    failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 파일 읽기 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// CSV 한 줄 파싱 (따옴표 처리 포함)
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);
            return result.ToArray();
        }

        #endregion

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

            try
            {
                BOMData bom = lvBOM.SelectedItems[0].Tag as BOMData;
                if (bom == null) return;

                // 1. 기존 치수/보조선 제거 (축 버튼과 동일한 패턴)
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // 2. X-Ray 끄기 (켜져있으면)
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // 3. 선택된 부재만 보이도록: 전체 숨기기 → 대상만 보이기
                List<int> allIndices = new List<int>();
                foreach (BOMData b in bomList)
                    allIndices.Add(b.Index);

                vizcore3d.Object3D.Show(allIndices, false);  // 전체 숨기기

                List<int> targetIndices = new List<int> { bom.Index };
                vizcore3d.Object3D.Show(targetIndices, true); // 대상만 보이기

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

                // 5. 카메라 방향 설정 (축 버튼과 동일한 패턴: MoveCamera → FitToView)
                VIZCore3D.NET.Data.CameraDirection camDir;
                string viewDirection;
                bool needRotate90 = false;

                switch (longestAxis)
                {
                    case "X":
                        camDir = VIZCore3D.NET.Data.CameraDirection.Y_PLUS;
                        viewDirection = "Y";
                        break;
                    case "Y":
                        camDir = VIZCore3D.NET.Data.CameraDirection.X_PLUS;
                        viewDirection = "X";
                        break;
                    default: // Z
                        camDir = VIZCore3D.NET.Data.CameraDirection.Y_PLUS;
                        viewDirection = "Y";
                        needRotate90 = true;
                        break;
                }

                vizcore3d.View.MoveCamera(camDir);

                // Z축 최장: Z축 고정 해제 → 화면 90° 회전 → Z가 수평
                if (needRotate90)
                {
                    bool originalLockZ = vizcore3d.View.ScreenAxisRotation.LockZAxis;
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = originalLockZ;
                }

                // 6. 화면 맞춤 (축 버튼과 동일: 숨겨진 부재 제외, 보이는 부재만 FitToView)
                vizcore3d.View.FitToView();

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
                                {
                                    var sv = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                    mfgOsnapWithNames.Add((sv, bom.Name));
                                }
                                if (osnap.End != null)
                                {
                                    var ev = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                    mfgOsnapWithNames.Add((ev, bom.Name));
                                }
                                break;
                            case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                            case VIZCore3D.NET.Data.OsnapKind.POINT:
                                if (osnap.Center != null)
                                {
                                    var cv = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                    mfgOsnapWithNames.Add((cv, bom.Name));
                                }
                                break;
                        }
                    }
                }

                if (mfgOsnapWithNames.Count == 0)
                {
                    // 실패 시 전체 부재 다시 보이기
                    vizcore3d.Object3D.Show(allIndices, true);
                    MessageBox.Show("선택된 부재에서 Osnap 좌표를 수집하지 못했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 좌표 병합
                float tolerance = 5.0f;
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(mfgOsnapWithNames, tolerance);

                // 치수 추출 (3축 모두 - AddDimensionsForView가 뷰 방향에 맞게 필터링)
                var mfgDimensions = new List<ChainDimensionData>();
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "X", tolerance));
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "Y", tolerance));
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "Z", tolerance));

                if (mfgDimensions.Count == 0)
                {
                    vizcore3d.Object3D.Show(allIndices, true);
                    MessageBox.Show("치수를 추출하지 못했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 8. 전역 상태 임시 교체 후 AddDimensionsForView 호출
                var savedChainDimList = new List<ChainDimensionData>(chainDimensionList);
                var savedXrayIndices = new List<int>(xraySelectedNodeIndices);

                chainDimensionList.Clear();
                chainDimensionList.AddRange(mfgDimensions);
                xraySelectedNodeIndices = new List<int>(targetIndices);

                // 보조선 오프셋 + 치수선 표시 (기존 로직 재사용)
                AddDimensionsForView(viewDirection);

                // 전역 상태 복원
                chainDimensionList.Clear();
                chainDimensionList.AddRange(savedChainDimList);
                xraySelectedNodeIndices = new List<int>(savedXrayIndices);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 가공도 모드 해제 - 전체 부재 다시 보이기
        /// BOM 더블클릭, 축 버튼, 전체보기 등에서 호출 가능
        /// </summary>
        private void RestoreAllPartsVisibility()
        {
            List<int> allIndices = new List<int>();
            foreach (BOMData b in bomList)
                allIndices.Add(b.Index);

            if (allIndices.Count > 0)
                vizcore3d.Object3D.Show(allIndices, true);
        }

        #endregion
    }

    /// <summary>
    /// 체인 치수 데이터 구조체
    /// </summary>
    public class ChainDimensionData
    {
        public string Axis { get; set; }
        public string ViewName { get; set; }
        public float Distance { get; set; }
        public VIZCore3D.NET.Data.Vector3D StartPoint { get; set; }
        public VIZCore3D.NET.Data.Vector3D EndPoint { get; set; }
        public string StartPointStr { get; set; }
        public string EndPointStr { get; set; }
        public bool IsTotal { get; set; }

        /// <summary>
        /// 치수 우선순위 (높을수록 중요, 1~10)
        /// 10: 전체 길이, 8: 주요 구간(상위 30%), 5: 중간 구간, 3: 작은 구간, 1: 매우 작은 구간
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// 표시 레벨 (0: 기본, 1~n: 추가 레벨)
        /// </summary>
        public int DisplayLevel { get; set; } = 0;

        /// <summary>
        /// 표시 여부 (필터링 후 결정)
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 병합된 치수 여부 (여러 짧은 치수를 하나로 통합)
        /// </summary>
        public bool IsMerged { get; set; } = false;
    }

    /// <summary>
    /// BOM 데이터 구조체
    /// </summary>
    public class BOMData
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public float RotationAngle { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }
    }

    /// <summary>
    /// Clash 데이터 구조체
    /// </summary>
    public class ClashData
    {
        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public float ZValue { get; set; }
    }
}
