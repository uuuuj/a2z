using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        /// BOM정보 탭 그룹 매핑 (key: nodeIndex, value: BOM정보 탭 그룹 No)
        /// </summary>
        private Dictionary<int, int> bomInfoNodeGroupMap = new Dictionary<int, int>();

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

        /// <summary>
        /// 풍선 위치 수동 오버라이드 (키: BOM인덱스, 값: X,Y,Z)
        /// </summary>
        private Dictionary<int, float[]> balloonOverrides = new Dictionary<int, float[]>();

        /// <summary>
        /// 현재 풍선이 표시된 뷰 방향
        /// </summary>
        private string currentBalloonView = "";

        /// <summary>
        /// Body 인덱스 → 부모 Part 풀네임 매핑 캐시
        /// </summary>
        private Dictionary<int, string> bodyToPartNameMap = new Dictionary<int, string>();

        /// <summary>
        /// Body 인덱스 → 부모 Part 인덱스 매핑 캐시
        /// </summary>
        private Dictionary<int, int> bodyToPartIndexMap = new Dictionary<int, int>();

        /// <summary>
        /// 도면 시트 데이터 리스트
        /// </summary>
        private List<DrawingSheetData> drawingSheetList = new List<DrawingSheetData>();

        /// <summary>
        /// 라이선스 갱신 타이머 (30분마다 갱신)
        /// </summary>
        private System.Windows.Forms.Timer licenseRefreshTimer;

        /// <summary>
        /// 부재 이름 입력 TextBox (3D 뷰어 위 오버레이)
        /// </summary>
        private TextBox txtMemberNameOverlay = null;

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
            lvDrawingSheet.SelectedIndexChanged += LvDrawingSheet_SelectedIndexChanged;

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
                // 0. BOM 데이터가 없으면 먼저 수집
                if (bomList.Count == 0)
                {
                    CollectBOMData();
                    if (bomList.Count == 0)
                    {
                        MessageBox.Show("BOM 데이터를 수집할 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
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

                // X-Ray 모드가 활성화되어 있고 선택된 노드가 있으면 해당 노드만 사용
                List<VIZCore3D.NET.Data.Node> bodyNodes;
                if (vizcore3d.View.XRay.Enable && xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    bodyNodes = allBodyNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
                }
                else
                {
                    bodyNodes = allBodyNodes;
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
                                    if (osnap.Center != null)
                                    {
                                        var centerVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(centerVertex);
                                        osnapPointsWithNames.Add((centerVertex, partName));
                                    }
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

                // X-Ray 모드가 활성화되어 있고 선택된 노드가 있으면 해당 노드만 사용
                List<VIZCore3D.NET.Data.Node> targetNodes;
                if (vizcore3d.View.XRay.Enable && xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    targetNodes = allNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
                }
                else
                {
                    targetNodes = allNodes;
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

        private void CollectBOMInfo(bool showAlert = true)
        {
            try
            {
                // Part 노드 가져오기 (Part 레벨에서 UDA 조회)
                List<VIZCore3D.NET.Data.Node> partNodes = vizcore3d.Object3D.GetPartialNode(false, true, false);
                if (partNodes == null || partNodes.Count == 0)
                {
                    // Part가 없으면 Body 노드로 시도
                    partNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                }

                if (partNodes == null || partNodes.Count == 0)
                {
                    if (showAlert) MessageBox.Show("로드된 모델이 없거나 노드를 찾을 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // X-Ray 모드가 활성화되어 있고 선택된 노드가 있으면 해당 Part만 필터링
                if (vizcore3d.View.XRay.Enable && xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    // Body 노드 기준 선택이므로, 선택된 Body의 부모 Part만 허용
                    List<VIZCore3D.NET.Data.Node> bodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                    var partIdxSorted = partNodes.Select(p => p.Index).OrderBy(x => x).ToList();
                    var allowedPartIndices = new HashSet<int>();

                    if (bodyNodes != null)
                    {
                        foreach (var body in bodyNodes)
                        {
                            if (!selectedSet.Contains(body.Index)) continue;
                            int lo = 0, hi = partIdxSorted.Count - 1;
                            int parentPart = -1;
                            while (lo <= hi)
                            {
                                int mid = (lo + hi) / 2;
                                if (partIdxSorted[mid] <= body.Index)
                                {
                                    parentPart = partIdxSorted[mid];
                                    lo = mid + 1;
                                }
                                else hi = mid - 1;
                            }
                            if (parentPart >= 0) allowedPartIndices.Add(parentPart);
                        }
                    }

                    partNodes = partNodes.Where(p => allowedPartIndices.Contains(p.Index)).ToList();
                }

                // ★ 도면시트 선택 시 해당 시트 부재만 필터링
                if (lvDrawingSheet.SelectedItems.Count > 0)
                {
                    DrawingSheetData selectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
                    if (selectedSheet != null && selectedSheet.MemberIndices.Count > 0)
                    {
                        var sheetBodySet = new HashSet<int>(selectedSheet.MemberIndices);
                        List<VIZCore3D.NET.Data.Node> bodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                        var partIdxSorted = partNodes.Select(p => p.Index).OrderBy(x => x).ToList();
                        var allowedPartIndices = new HashSet<int>();

                        if (bodyNodes != null)
                        {
                            foreach (var body in bodyNodes)
                            {
                                if (!sheetBodySet.Contains(body.Index)) continue;
                                int lo = 0, hi = partIdxSorted.Count - 1;
                                int parentPart = -1;
                                while (lo <= hi)
                                {
                                    int mid = (lo + hi) / 2;
                                    if (partIdxSorted[mid] <= body.Index)
                                    {
                                        parentPart = partIdxSorted[mid];
                                        lo = mid + 1;
                                    }
                                    else hi = mid - 1;
                                }
                                if (parentPart >= 0) allowedPartIndices.Add(parentPart);
                            }
                        }

                        partNodes = partNodes.Where(p => allowedPartIndices.Contains(p.Index)).ToList();
                    }
                }

                // UDA 키 목록 한번만 조회
                List<string> udaKeyList = null;
                try
                {
                    var keys = vizcore3d.Object3D.UDA.Keys;
                    if (keys != null && keys.Count > 0)
                        udaKeyList = new List<string>(keys);
                }
                catch { }

                // 각 Part 노드에서 SPREF/MATREF/GWEI 값 수집 (현재 노드에 없으면 부모로 올라가며 재조회)
                var rawBomItems = new List<Tuple<string, string, string, string, int>>();  // Item, Size, Material, Weight, NodeIndex
                double totalWeight = 0;

                foreach (var node in partNodes)
                {
                    string sprefVal = "";
                    string matrefVal = "";
                    string gweiVal = "";

                    // 현재 노드부터 부모로 올라가며 UDA 조회 (최대 10단계)
                    int currentIdx = node.Index;
                    for (int depth = 0; depth < 10; depth++)
                    {
                        if (currentIdx < 0) break;

                        if (udaKeyList != null)
                        {
                            foreach (string key in udaKeyList)
                            {
                                string keyUpper = key.Trim().ToUpper();
                                try
                                {
                                    var val = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                                    string valStr = (val != null) ? val.ToString().Trim() : "";

                                    if (keyUpper == "SPREF" && string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(valStr))
                                        sprefVal = valStr;
                                    else if (keyUpper == "MATREF" && string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(valStr))
                                        matrefVal = valStr;
                                    else if (keyUpper == "GWEI" && string.IsNullOrEmpty(gweiVal) && !string.IsNullOrEmpty(valStr))
                                        gweiVal = valStr;
                                }
                                catch { }
                            }
                        }

                        // 3개 값 모두 찾으면 중단
                        if (!string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(gweiVal))
                            break;

                        // 부모 노드로 이동
                        try
                        {
                            VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                            if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                            currentIdx = parentNode.ParentIndex;
                        }
                        catch { break; }
                    }

                    // SPREF 파싱: 첫 글자 "/" 제거 후 ":" 기준 split → [0]=ITEM, [1]=SIZE
                    string itemVal = "";
                    string sizeVal = "";
                    if (!string.IsNullOrEmpty(sprefVal))
                    {
                        string sprefClean = sprefVal;
                        if (sprefClean.StartsWith("/"))
                            sprefClean = sprefClean.Substring(1);
                        string[] parts = sprefClean.Split(':');
                        itemVal = parts[0].Trim();
                        if (parts.Length > 1)
                            sizeVal = parts[1].Trim();
                    }

                    // UDA에 SPREF가 없으면 노드 이름을 Item으로 사용
                    if (string.IsNullOrEmpty(itemVal))
                        itemVal = node.NodeName ?? "";

                    // MATREF 파싱: 첫 글자 "/" 제거 → MATERIAL 값
                    string materialVal = matrefVal;
                    if (!string.IsNullOrEmpty(materialVal) && materialVal.StartsWith("/"))
                        materialVal = materialVal.Substring(1);

                    // T/W 합계 계산
                    double w = 0;
                    if (!string.IsNullOrEmpty(gweiVal))
                        double.TryParse(gweiVal, out w);
                    totalWeight += w;

                    // GWEI 소수점 둘째자리 반올림
                    string gweiDisplay = gweiVal;
                    if (!string.IsNullOrEmpty(gweiVal))
                    {
                        double gw;
                        if (double.TryParse(gweiVal, out gw))
                            gweiDisplay = Math.Round(gw, 2).ToString("F2");
                    }

                    rawBomItems.Add(Tuple.Create(itemVal, sizeVal, materialVal, gweiDisplay, node.Index));
                }

                // bomInfoNodeGroupMap 구축: Body nodeIndex → groupNo 매핑
                bomInfoNodeGroupMap.Clear();
                List<VIZCore3D.NET.Data.Node> bodyNodesForMap = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (bodyNodesForMap != null && bodyNodesForMap.Count > 0)
                {
                    List<int> partIdxSorted = partNodes.Select(p => p.Index).OrderBy(x => x).ToList();

                    // 각 Part에 순차적으로 groupNo 부여 (Row 0은 요약행이므로 1부터)
                    var partToGroup = new Dictionary<int, int>();
                    int groupNo = 1;
                    foreach (var bomItem in rawBomItems)
                    {
                        partToGroup[bomItem.Item5] = groupNo;
                        groupNo++;
                    }

                    foreach (var body in bodyNodesForMap)
                    {
                        int parentPartIndex = -1;
                        int lo = 0, hi = partIdxSorted.Count - 1;
                        while (lo <= hi)
                        {
                            int mid = (lo + hi) / 2;
                            if (partIdxSorted[mid] <= body.Index)
                            {
                                parentPartIndex = partIdxSorted[mid];
                                lo = mid + 1;
                            }
                            else
                            {
                                hi = mid - 1;
                            }
                        }
                        if (parentPartIndex >= 0 && partToGroup.ContainsKey(parentPartIndex))
                        {
                            bomInfoNodeGroupMap[body.Index] = partToGroup[parentPartIndex];
                        }
                    }
                }

                // ListView에 채우기 (도면정보 탭 BOM 테이블)
                lvDrawingBOMInfo.BeginUpdate();
                lvDrawingBOMInfo.Items.Clear();

                // Row 0: 요약행
                ListViewItem summaryRow = new ListViewItem("");                      // No.
                summaryRow.SubItems.Add("Support&Seat");                             // ITEM
                summaryRow.SubItems.Add("");                                         // MATERIAL
                summaryRow.SubItems.Add("");                                         // SIZE
                summaryRow.SubItems.Add("");                                         // Q'TY
                summaryRow.SubItems.Add(totalWeight > 0 ? Math.Round(totalWeight, 2).ToString("F2") : ""); // T/W
                summaryRow.SubItems.Add("F");                                        // MA
                summaryRow.SubItems.Add("F");                                        // FA
                lvDrawingBOMInfo.Items.Add(summaryRow);

                // Row 1~N: 개별 파트 행
                int no = 1;
                foreach (var bomItem in rawBomItems)
                {
                    ListViewItem lvi = new ListViewItem(no.ToString());   // No.
                    lvi.SubItems.Add(bomItem.Item1);                      // ITEM
                    lvi.SubItems.Add(bomItem.Item3);                      // MATERIAL
                    lvi.SubItems.Add(bomItem.Item2);                      // SIZE
                    lvi.SubItems.Add("1");                                // Q'TY
                    lvi.SubItems.Add(bomItem.Item4);                      // T/W
                    lvi.SubItems.Add("L");                                // MA
                    lvi.SubItems.Add("F");                                // FA
                    lvDrawingBOMInfo.Items.Add(lvi);

                    no++;
                }
                lvDrawingBOMInfo.EndUpdate();

                if (showAlert) MessageBox.Show(string.Format("BOM 정보 {0}개 항목 수집 완료", rawBomItems.Count), "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (showAlert) MessageBox.Show("BOM 정보 수집 오류:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // X-Ray 모드가 활성화되어 있고 선택된 노드가 있으면 해당 노드만 사용
                List<VIZCore3D.NET.Data.Node> targetNodes;
                if (vizcore3d.View.XRay.Enable && xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    targetNodes = allNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
                }
                else
                {
                    targetNodes = allNodes;
                }

                vizcore3d.Clash.Clear();
                int clashCount = 0;

                for (int i = 0; i < targetNodes.Count; i++)
                {
                    for (int j = i + 1; j < targetNodes.Count; j++)
                    {
                        VIZCore3D.NET.Data.ClashTest pairClash = new VIZCore3D.NET.Data.ClashTest();
                        pairClash.Name = $"간섭검사_{targetNodes[i].NodeName}_vs_{targetNodes[j].NodeName}";
                        pairClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        pairClash.UseClearanceValue = true;
                        pairClash.ClearanceValue = 1.0f;
                        pairClash.UseRangeValue = true;
                        pairClash.RangeValue = 1.0f;
                        pairClash.UsePenetrationTolerance = true;
                        pairClash.PenetrationTolerance = 1.0f;
                        pairClash.VisibleOnly = false;
                        pairClash.BottomLevel = 0;
                        pairClash.GroupA = new List<VIZCore3D.NET.Data.Node> { targetNodes[i] };
                        pairClash.GroupB = new List<VIZCore3D.NET.Data.Node> { targetNodes[j] };

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

                // Clash 완료 후 도면 시트 자동 생성
                if (clashList.Count > 0)
                {
                    GenerateDrawingSheets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"간섭검사 결과 처리 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
        /// 2D 도면 생성 - VIZCore3D Drawing2D Template API 사용
        /// 템플릿(BOM 테이블 + 도면 정보) 배치 및 렌더링
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
                vizcore3d.View.EnableAnimation = false;
                vizcore3d.Review.Note.Clear();

                // 2D 도면 모드 활성화
                vizcore3d.ToolbarDrawing2D.Visible = true;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

                // -------------------------------------------------------------------------
                // 템플릿 기능을 이용한 우측 테이블 배치 (BOM 목록 및 도면 정보)
                // -------------------------------------------------------------------------

                // 도면 템플릿 생성 프로세스 시작
                vizcore3d.Drawing2D.Template.CreateTemplate();

                // 노드 인덱스와 노트 ID를 매핑하기 위한 딕셔너리
                Dictionary<int, int> nodeToNoteMap = new Dictionary<int, int>();

                // BOM정보가 미수집이면 자동 수집
                if (lvDrawingBOMInfo.Items.Count == 0)
                {
                    CollectBOMInfo(false);
                }

                // ==========================================================
                // [표 1] BOM 정보 테이블 (우측 상단) — lvDrawingBOMInfo 기반
                // ==========================================================
                if (lvDrawingBOMInfo.Items.Count > 0)
                {
                    // 행: lvDrawingBOMInfo 항목 수 + 헤더(1), 열: 8 (No./ITEM/MATERIAL/SIZE/Q'TY/T/W/MA/FA)
                    VIZCore3D.NET.Data.TemplateTableData table1 = new VIZCore3D.NET.Data.TemplateTableData(lvDrawingBOMInfo.Items.Count + 1, 8);
                    table1.SetText(0, 0, "No.");
                    table1.SetText(0, 1, "ITEM");
                    table1.SetText(0, 2, "MATERIAL");
                    table1.SetText(0, 3, "SIZE");
                    table1.SetText(0, 4, "Q'TY");
                    table1.SetText(0, 5, "T/W");
                    table1.SetText(0, 6, "MA");
                    table1.SetText(0, 7, "FA");

                    for (int i = 0; i < lvDrawingBOMInfo.Items.Count; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i];
                        for (int col = 0; col < 8 && col < item.SubItems.Count; col++)
                        {
                            table1.SetText(i + 1, col, item.SubItems[col].Text);
                        }
                    }

                    // 표를 도면 캔버스의 우측 영역(310mm 지점)에 배치
                    table1.X = 310;
                    table1.Y = 0;
                    vizcore3d.Drawing2D.Template.AddTemplateItem(table1);
                }

                // 풍선 번호표 생성 (bomList 기반)
                if (bomList != null && bomList.Count > 0)
                {
                    foreach (var bom in bomList)
                    {
                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);
                        VIZCore3D.NET.Data.Vertex3D notePos = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX + 400, bom.CenterY, bom.CenterZ);

                        int id = vizcore3d.Review.Note.AddNoteSurface("TEMP", notePos, center);
                        nodeToNoteMap.Add(bom.Index, id);

                        VIZCore3D.NET.Data.NoteItem note = vizcore3d.Review.Note.GetItem(id);
                        note.UpdateText(id.ToString());
                    }
                }

                // ==========================================================
                // [표 2] 도면 정보 (우측 하단)
                // ==========================================================
                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));

                table2.X = 310;
                table2.Y = 200; // [표 1] 아래에 위치하도록 Y값 조정
                vizcore3d.Drawing2D.Template.AddTemplateItem(table2);

                // 작성된 템플릿(표, 로고 등)을 도면에 최종 렌더링 (로고 사이즈 지정)
                vizcore3d.Drawing2D.Template.RenderTemplate(60, 80);

                // -------------------------------------------------------------------------
                // 각 그리드 셀에 모델 투영 및 배치 (Grid 2x3 구조)
                // -------------------------------------------------------------------------

                // 2D 도면을 그릴 캔버스 선택 및 크기 확인
                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                // 2행 3열의 그리드 구조 생성 (우측 1열은 템플릿 테이블 영역)
                vizcore3d.Drawing2D.GridStructure.AddGridStructure(2, 3, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(15, 15, 15, 15);

                // 각 그리드 셀별로 다른 각도의 뷰를 투영
                // [1,1] ISO View
                RenderViewWithVisibleNotes(1, 1, VIZCore3D.NET.Data.CameraDirection.ISO_PLUS, nodeToNoteMap);

                // [1,2] TOP View (Z-)
                RenderViewWithVisibleNotes(1, 2, VIZCore3D.NET.Data.CameraDirection.Z_MINUS, nodeToNoteMap);

                // [2,1] LEFT View (X-)
                RenderViewWithVisibleNotes(2, 1, VIZCore3D.NET.Data.CameraDirection.X_MINUS, nodeToNoteMap);

                // [2,2] FRONT View (Y-)
                RenderViewWithVisibleNotes(2, 2, VIZCore3D.NET.Data.CameraDirection.Y_MINUS, nodeToNoteMap);

                // 2D 도면 최종 렌더링
                vizcore3d.Drawing2D.Render();

                MessageBox.Show("2D 도면 생성 완료!\n\n" +
                    "- ISO VIEW [1,1]\n" +
                    "- TOP VIEW [1,2]\n" +
                    "- LEFT VIEW [2,1]\n" +
                    "- FRONT VIEW [2,2]\n" +
                    "- BOM 목록 테이블 (우측 상단)\n" +
                    "- 도면 정보 테이블 (우측 하단)",
                    "2D 도면 생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"2D 도면 생성 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 특정 뷰 방향에서 보이는 노드만 계산하여 2D 도면에 투영하는 함수
        /// </summary>
        /// <param name="row">그리드 행 번호</param>
        /// <param name="col">그리드 열 번호</param>
        /// <param name="camDir">3D 카메라 이동 방향</param>
        /// <param name="map">부품 인덱스-노트 ID 매핑 딕셔너리</param>
        private void RenderViewWithVisibleNotes(int row, int col, VIZCore3D.NET.Data.CameraDirection camDir, Dictionary<int, int> map)
        {
            // 3D 카메라를 해당 각도로 이동
            vizcore3d.View.MoveCamera(camDir);

            vizcore3d.View.EnableBoxSelectionFrontObjectOnly = true;

            // 현재 카메라 각도에서 실제로 보이는 부품 리스트 추출
            List<VIZCore3D.NET.Data.Node> visibleNodes = vizcore3d.Object3D.FromScreen(false, VIZCore3D.NET.Data.LeafNodeKind.BODY);

            // 보이는 부품들 중 map에 등록된 부품의 번호표 ID만 수집
            List<int> visibleNoteIds = new List<int>();
            foreach (var node in visibleNodes)
            {
                if (map.ContainsKey(node.Index) || map.ContainsKey(node.ParentIndex))
                {
                    visibleNoteIds.Add(map[node.Index]);
                }
            }

            // 2D 모델 투영체 생성 (현재 3D 뷰 각도를 그대로 2D로 변환)
            int objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelAtCanvasOrigin(VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

            // 지정된 그리드 셀(row, col) 중앙에 모델을 꽉 차게 배치
            vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, objId, VIZCore3D.NET.Data.GridHorizontalAlignment.Center, VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

            // 보이는 부품의 번호표들만 도면에 투영
            if (visibleNoteIds.Count > 0)
            {
                vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
            }

            //측정이 표기가 필요한경우 (X/Y/Z 축 뷰에서만 치수 표시)
            if(camDir != VIZCore3D.NET.Data.CameraDirection.ISO_PLUS)
            {
                List<int> visibleMeasureIds = new List<int>();

                List<VIZCore3D.NET.Data.MeasureItem> listMeasure = vizcore3d.Review.Measure.Items;
                foreach(var measure in listMeasure)
                {
                    if(measure.Visible)
                        visibleMeasureIds.Add(measure.ID);
                }
                vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(visibleMeasureIds.ToArray());
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
            measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
            measureStyle.FontBold = true;
            measureStyle.LineColor = System.Drawing.Color.Black;
            measureStyle.LineWidth = 1;
            measureStyle.ArrowColor = System.Drawing.Color.Black;
            measureStyle.ArrowSize = 8;
            measureStyle.AssistantLine = true;
            measureStyle.AssistantLineStyle = VIZCore3D.NET.Data.MeasureStyle.AssistantLineType.SOLIDLINE;
            measureStyle.AlignDistanceText = true;
            measureStyle.AlignDistanceTextPosition = 0;
            measureStyle.AlignDistanceTextMargine = 3;
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

            // 부재 크기 기반 동적 오프셋 계산
            float dimMaxX = float.MinValue, dimMaxY = float.MinValue, dimMaxZ = float.MinValue;
            foreach (var dim in displayList)
            {
                dimMaxX = Math.Max(dimMaxX, Math.Max(dim.StartPoint.X, dim.EndPoint.X));
                dimMaxY = Math.Max(dimMaxY, Math.Max(dim.StartPoint.Y, dim.EndPoint.Y));
                dimMaxZ = Math.Max(dimMaxZ, Math.Max(dim.StartPoint.Z, dim.EndPoint.Z));
            }
            float extentX = dimMaxX - globalMinX;
            float extentY = dimMaxY - globalMinY;
            float extentZ = dimMaxZ - globalMinZ;
            float modelDiag = (float)Math.Sqrt(extentX * extentX + extentY * extentY + extentZ * extentZ);

            float baseOffset = Math.Max(modelDiag * 0.12f, 30f);   // 1단 체인 치수 오프셋
            float levelSpacing = Math.Max(modelDiag * 0.08f, 25f);  // 레벨 간 간격
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
                        float levelOffset = baseOffset + (levelSpacing * shortCount);

                        DrawDimension(firstPoint, dims[i].EndPoint, axis,
                            levelOffset, globalMinX, globalMinY, globalMinZ,
                            viewDirection, extensionLines);
                        lastDimWasShort = true;
                    }
                    else
                    {
                        // 정상 길이 치수 → Level 1에 그리기
                        DrawDimension(dims[i].StartPoint, dims[i].EndPoint, axis,
                            baseOffset, globalMinX, globalMinY, globalMinZ,
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
                float totalOffset = baseOffset + (levelSpacing * maxLevel);

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
                string name = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";  // No. 칼럼 추가로 Name은 SubItems[1]
                string type = "PART";
                string qty = "1";

                g.DrawString(item.Text, font, Brushes.Black, x + 3, rowY);  // No. 칼럼 값 사용

                g.DrawString(TruncateString(name, 18), font, Brushes.Black, x + colWidths[0] + 3, rowY);

                if (item.SubItems.Count > 2)
                {
                    string angle = item.SubItems[2].Text;  // No. 칼럼 추가로 Angle은 SubItems[2]
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
                measureStyle.LineWidth = 1;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;     // 검은색
                measureStyle.ArrowSize = 8;
                measureStyle.AlignDistanceText = true;
                measureStyle.AlignDistanceTextPosition = 0;
                measureStyle.AlignDistanceTextMargine = 3;
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

                // 번호 재정렬 (ListView와 데이터 모두 갱신)
                for (int i = 0; i < lvDimension.Items.Count; i++)
                {
                    lvDimension.Items[i].Text = (i + 1).ToString();
                    if (i < chainDimensionList.Count)
                    {
                        chainDimensionList[i].No = i + 1;
                    }
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
        /// X축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisX_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("X");
        }

        /// <summary>
        /// Y축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisY_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Y");
        }

        /// <summary>
        /// Z축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisZ_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Z");
        }

        /// <summary>
        /// ISO 방향 보기 버튼 (등각 투영) - 기존 호환용
        /// </summary>
        private void btnShowISO_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("ISO");
        }

        /// <summary>
        /// 각 부재에 원형 숫자 풍선(Balloon) 표시
        /// 부재별 겹치지 않는 위치에 번호가 있는 원형 마커를 배치
        /// viewDirection: "X", "Y", "Z", "ISO"
        /// </summary>
        private void ShowBalloonNumbers(string viewDirection)
        {
            vizcore3d.Review.Note.Clear();
            if (bomList == null || bomList.Count == 0) return;

            // BOM정보가 미수집이면 자동 수집 (풍선 번호를 BOM No.와 일치시키기 위해)
            if (bomInfoNodeGroupMap.Count == 0)
            {
                CollectBOMInfo(false);
            }

            // 뷰 방향이 변경되면 수동 오버라이드 초기화
            if (currentBalloonView != viewDirection)
            {
                balloonOverrides.Clear();
                currentBalloonView = viewDirection;
            }

            try
            {
                // ===== 1. 전체 모델 바운딩박스 계산 =====
                float gMinX = float.MaxValue, gMinY = float.MaxValue, gMinZ = float.MaxValue;
                float gMaxX = float.MinValue, gMaxY = float.MinValue, gMaxZ = float.MinValue;
                foreach (var b in bomList)
                {
                    gMinX = Math.Min(gMinX, b.MinX); gMinY = Math.Min(gMinY, b.MinY); gMinZ = Math.Min(gMinZ, b.MinZ);
                    gMaxX = Math.Max(gMaxX, b.MaxX); gMaxY = Math.Max(gMaxY, b.MaxY); gMaxZ = Math.Max(gMaxZ, b.MaxZ);
                }
                float gcX = (gMinX + gMaxX) / 2f, gcY = (gMinY + gMaxY) / 2f, gcZ = (gMinZ + gMaxZ) / 2f;
                float sizeX = Math.Max(gMaxX - gMinX, 1f);
                float sizeY = Math.Max(gMaxY - gMinY, 1f);
                float sizeZ = Math.Max(gMaxZ - gMinZ, 1f);

                // ===== 2. 뷰 방향에 따른 투영 축 결정 =====
                int hAxis, vAxis, dAxis;
                switch (viewDirection)
                {
                    case "X": hAxis = 1; vAxis = 2; dAxis = 0; break;
                    case "Y": hAxis = 0; vAxis = 2; dAxis = 1; break;
                    default:  hAxis = 0; vAxis = 1; dAxis = 2; break;
                }

                Func<BOMData, int, float> getCenter = (b, ax) =>
                    ax == 0 ? b.CenterX : (ax == 1 ? b.CenterY : b.CenterZ);
                Func<BOMData, int, float> getMin = (b, ax) =>
                    ax == 0 ? b.MinX : (ax == 1 ? b.MinY : b.MinZ);
                Func<BOMData, int, float> getMax = (b, ax) =>
                    ax == 0 ? b.MaxX : (ax == 1 ? b.MaxY : b.MaxZ);

                float[] gCenter = { gcX, gcY, gcZ };
                float[] gSizeArr = { sizeX, sizeY, sizeZ };

                float modelSizeH = gSizeArr[hAxis];
                float modelSizeV = gSizeArr[vAxis];
                float modelCenterH = gCenter[hAxis];
                float modelCenterV = gCenter[vAxis];
                float modelDiag = (float)Math.Sqrt(modelSizeH * modelSizeH + modelSizeV * modelSizeV);

                // ===== 3. 풍선 배치 파라미터 =====
                float minBalloonDist = Math.Max(modelDiag * 0.04f, 20f);

                List<float[]> placed = new List<float[]>();

                // ===== 4. 표시할 풍선 결정 (BOM정보 탭 그룹 기준) =====
                // balloonDisplayNumbers: key=bomList인덱스, value=표시할 번호
                var balloonDisplayNumbers = new Dictionary<int, int>();
                if (bomInfoNodeGroupMap.Count > 0)
                {
                    // BOM정보 탭 그룹 기준: 같은 그룹에서 첫 번째만 표시
                    var shownGroups = new HashSet<int>();
                    for (int i = 0; i < bomList.Count; i++)
                    {
                        int nodeIdx = bomList[i].Index;
                        if (bomInfoNodeGroupMap.TryGetValue(nodeIdx, out int grpNo))
                        {
                            if (!shownGroups.Contains(grpNo))
                            {
                                shownGroups.Add(grpNo);
                                balloonDisplayNumbers[i] = grpNo;
                            }
                        }
                    }
                }
                else
                {
                    // BOM정보 미수집: 기존처럼 개별 순번
                    for (int i = 0; i < bomList.Count; i++)
                    {
                        balloonDisplayNumbers[i] = i + 1;
                    }
                }

                // ===== 4-1. lvBOM "No." 칼럼을 풍선번호와 일치시키기 =====
                for (int i = 0; i < bomList.Count && i < lvBOM.Items.Count; i++)
                {
                    int nodeIdx = bomList[i].Index;
                    if (balloonDisplayNumbers.ContainsKey(i))
                    {
                        // 풍선이 표시되는 대표 부재: 풍선번호 사용
                        lvBOM.Items[i].Text = balloonDisplayNumbers[i].ToString();
                    }
                    else if (bomInfoNodeGroupMap.TryGetValue(nodeIdx, out int grpNo))
                    {
                        // 같은 그룹의 비대표 부재: 그룹번호 사용
                        lvBOM.Items[i].Text = grpNo.ToString();
                    }
                    else
                    {
                        // 매핑 없는 경우: 순번 유지
                        lvBOM.Items[i].Text = (i + 1).ToString();
                    }
                }

                // ===== 5. 각 부재별 풍선 배치 =====
                for (int i = 0; i < bomList.Count; i++)
                {
                    // 대표 아닌 부재는 스킵
                    if (!balloonDisplayNumbers.ContainsKey(i)) continue;

                    var bom = bomList[i];
                    float bx, by, bz;

                    // 수동 오버라이드가 있으면 그 위치 사용
                    if (balloonOverrides.ContainsKey(i))
                    {
                        bx = balloonOverrides[i][0];
                        by = balloonOverrides[i][1];
                        bz = balloonOverrides[i][2];
                    }
                    else if (viewDirection == "ISO")
                    {
                        // === ISO 뷰: 부재 중심→모델 중심 반대 방향으로, 부재 바로 옆에 배치 ===
                        float d3x = bom.CenterX - gcX;
                        float d3y = bom.CenterY - gcY;
                        float d3z = bom.CenterZ - gcZ;
                        float d3len = (float)Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);

                        if (d3len < 0.001f)
                        {
                            float angle = (float)(i * 2 * Math.PI / bomList.Count);
                            d3x = (float)Math.Cos(angle);
                            d3y = (float)Math.Sin(angle);
                            d3z = 0.3f;
                            d3len = (float)Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                        }
                        d3x /= d3len; d3y /= d3len; d3z /= d3len;

                        // 부재 가장자리까지 거리 + 개별 부재 크기 기반 오프셋
                        float mHalfX = (bom.MaxX - bom.MinX) / 2f;
                        float mHalfY = (bom.MaxY - bom.MinY) / 2f;
                        float mHalfZ = (bom.MaxZ - bom.MinZ) / 2f;
                        float memberDiag = (float)Math.Sqrt(mHalfX * mHalfX + mHalfY * mHalfY + mHalfZ * mHalfZ);
                        float memberOffset = Math.Max(memberDiag * 0.5f, 25f);
                        float edgeDist = Math.Abs(d3x) * mHalfX + Math.Abs(d3y) * mHalfY + Math.Abs(d3z) * mHalfZ;
                        float totalDist = edgeDist + memberOffset;

                        bx = bom.CenterX + d3x * totalDist;
                        by = bom.CenterY + d3y * totalDist;
                        bz = bom.CenterZ + d3z * totalDist;

                        // 충돌 검사 (다른 풍선 + 다른 부재 바운딩박스, 자기 자신 제외)
                        bool positionFound = false;
                        for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                        {
                            bool collision = false;

                            foreach (var pp in placed)
                            {
                                float dx = bx - pp[0], dy = by - pp[1], dz = bz - pp[2];
                                if (Math.Sqrt(dx * dx + dy * dy + dz * dz) < minBalloonDist)
                                { collision = true; break; }
                            }

                            if (!collision)
                            {
                                float pad = minBalloonDist * 0.3f;
                                foreach (var otherBom in bomList)
                                {
                                    if (otherBom == bom) continue; // 자기 자신 부재는 충돌 제외
                                    if (bx >= otherBom.MinX - pad && bx <= otherBom.MaxX + pad &&
                                        by >= otherBom.MinY - pad && by <= otherBom.MaxY + pad &&
                                        bz >= otherBom.MinZ - pad && bz <= otherBom.MaxZ + pad)
                                    { collision = true; break; }
                                }
                            }

                            if (!collision)
                            {
                                positionFound = true;
                            }
                            else
                            {
                                float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                                if (attempt % 2 == 1) rotAngle = -rotAngle;
                                float cosA = (float)Math.Cos(rotAngle);
                                float sinA = (float)Math.Sin(rotAngle);

                                // XY 평면에서 회전
                                float newDx = cosA * d3x - sinA * d3y;
                                float newDy = sinA * d3x + cosA * d3y;
                                float newDist = totalDist * (1f + (attempt / 6) * 0.1f);

                                bx = bom.CenterX + newDx * newDist;
                                by = bom.CenterY + newDy * newDist;
                                bz = bom.CenterZ + d3z * newDist;
                            }
                        }

                        // 자동 계산된 위치 저장
                        balloonOverrides[i] = new float[] { bx, by, bz };
                    }
                    else
                    {
                        // === 정사영 뷰 (X, Y, Z): 2D H-V 평면 기반, 부재 바로 옆에 배치 ===
                        float memberH = getCenter(bom, hAxis);
                        float memberV = getCenter(bom, vAxis);
                        float memberD = getCenter(bom, dAxis);
                        float memberHalfH = (getMax(bom, hAxis) - getMin(bom, hAxis)) / 2f;
                        float memberHalfV = (getMax(bom, vAxis) - getMin(bom, vAxis)) / 2f;

                        float dirH = memberH - modelCenterH;
                        float dirV = memberV - modelCenterV;
                        float dirLen = (float)Math.Sqrt(dirH * dirH + dirV * dirV);

                        if (dirLen < 0.001f)
                        {
                            float defaultAngle = (float)(i * 2 * Math.PI / bomList.Count);
                            dirH = (float)Math.Cos(defaultAngle);
                            dirV = (float)Math.Sin(defaultAngle);
                            dirLen = 1f;
                        }
                        dirH /= dirLen;
                        dirV /= dirLen;

                        // 개별 부재 크기 기반 오프셋
                        float memberDiag2D = (float)Math.Sqrt(memberHalfH * memberHalfH + memberHalfV * memberHalfV);
                        float memberOffset2D = Math.Max(memberDiag2D * 0.5f, 25f);
                        float edgeDist = Math.Abs(dirH) * memberHalfH + Math.Abs(dirV) * memberHalfV;
                        float totalOffset = edgeDist + memberOffset2D;
                        float bestH = memberH + dirH * totalOffset;
                        float bestV = memberV + dirV * totalOffset;

                        bool positionFound = false;
                        for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                        {
                            bool collision = false;
                            foreach (var pp in placed)
                            {
                                float dh = bestH - pp[0];
                                float dv = bestV - pp[1];
                                if (Math.Sqrt(dh * dh + dv * dv) < minBalloonDist)
                                { collision = true; break; }
                            }

                            if (!collision)
                            {
                                float pad = minBalloonDist * 0.3f;
                                foreach (var otherBom in bomList)
                                {
                                    if (otherBom == bom) continue; // 자기 자신 부재는 충돌 제외
                                    float oMinH = getMin(otherBom, hAxis) - pad;
                                    float oMaxH = getMax(otherBom, hAxis) + pad;
                                    float oMinV = getMin(otherBom, vAxis) - pad;
                                    float oMaxV = getMax(otherBom, vAxis) + pad;
                                    if (bestH >= oMinH && bestH <= oMaxH && bestV >= oMinV && bestV <= oMaxV)
                                    { collision = true; break; }
                                }
                            }

                            if (!collision)
                            {
                                positionFound = true;
                            }
                            else
                            {
                                float rotAngle = (float)((attempt / 2 + 1) * 10 * Math.PI / 180);
                                if (attempt % 2 == 1) rotAngle = -rotAngle;
                                float cosA = (float)Math.Cos(rotAngle);
                                float sinA = (float)Math.Sin(rotAngle);
                                float newDirH = cosA * dirH - sinA * dirV;
                                float newDirV = sinA * dirH + cosA * dirV;
                                float newOffset = totalOffset * (1f + (attempt / 6) * 0.1f);
                                bestH = memberH + newDirH * newOffset;
                                bestV = memberV + newDirV * newOffset;
                            }
                        }

                        float[] xyz = new float[3];
                        xyz[hAxis] = bestH;
                        xyz[vAxis] = bestV;
                        xyz[dAxis] = memberD;
                        bx = xyz[0]; by = xyz[1]; bz = xyz[2];

                        balloonOverrides[i] = new float[] { bx, by, bz };
                    }

                    placed.Add(new float[] { bx, by, bz });

                    VIZCore3D.NET.Data.Vertex3D balloonPos = new VIZCore3D.NET.Data.Vertex3D(bx, by, bz);
                    VIZCore3D.NET.Data.Vertex3D memberCenter = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);

                    VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                    style.UseSymbol = true;
                    style.SymbolText = balloonDisplayNumbers[i].ToString();
                    style.SymbolBackgroundColor = Color.Transparent;
                    style.SymbolFontColor = Color.Black;
                    style.SymbolFontBold = true;
                    style.SymbolSize = 8;
                    style.LineColor = Color.FromArgb(80, 80, 80);
                    style.LineWidth = 1;
                    style.ArrowColor = Color.FromArgb(80, 80, 80);
                    style.ArrowWidth = 5;
                    style.BackgroudTransparent = true;
                    style.FontBold = false;

                    vizcore3d.Review.Note.AddNoteSurface(" ", memberCenter, balloonPos, style);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"풍선 번호 표시 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 풍선 위치 수동 조정 다이얼로그
        /// </summary>
        private void btnBalloonAdjust_Click(object sender, EventArgs e)
        {
            if (bomList == null || bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다. 먼저 BOM을 수집하세요.", "알림");
                return;
            }
            if (string.IsNullOrEmpty(currentBalloonView))
            {
                MessageBox.Show("먼저 뷰(ISO/X/Y/Z) 버튼을 클릭하여 풍선을 표시하세요.", "알림");
                return;
            }

            // BOM 리스트에서 선택된 항목 확인
            int selectedIdx = -1;
            if (lvBOM.SelectedItems.Count > 0)
            {
                selectedIdx = lvBOM.SelectedItems[0].Index;
            }

            // 다이얼로그 생성
            Form dlg = new Form();
            dlg.Text = "풍선 위치 조정";
            dlg.Size = new Size(380, 340);
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.StartPosition = FormStartPosition.CenterParent;

            // 부재 선택 콤보박스
            Label lblSelect = new Label { Text = "부재 선택:", Location = new Point(15, 15), AutoSize = true };
            ComboBox cmbMember = new ComboBox();
            cmbMember.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMember.Location = new Point(15, 38);
            cmbMember.Size = new Size(330, 25);
            for (int i = 0; i < bomList.Count; i++)
                cmbMember.Items.Add($"{i + 1}. {bomList[i].Name}");
            cmbMember.SelectedIndex = selectedIdx >= 0 && selectedIdx < bomList.Count ? selectedIdx : 0;

            // 현재 위치 표시
            Label lblCurrent = new Label { Text = "현재 풍선 위치:", Location = new Point(15, 75), AutoSize = true };
            Label lblCurrentPos = new Label { Location = new Point(15, 98), Size = new Size(330, 20), ForeColor = Color.Blue };

            // 오프셋 입력
            Label lblX = new Label { Text = "X 오프셋:", Location = new Point(15, 130), AutoSize = true };
            NumericUpDown nudX = new NumericUpDown();
            nudX.Location = new Point(100, 128); nudX.Size = new Size(120, 25);
            nudX.Minimum = -100000; nudX.Maximum = 100000; nudX.DecimalPlaces = 1; nudX.Increment = 10;

            Label lblY = new Label { Text = "Y 오프셋:", Location = new Point(15, 163), AutoSize = true };
            NumericUpDown nudY = new NumericUpDown();
            nudY.Location = new Point(100, 161); nudY.Size = new Size(120, 25);
            nudY.Minimum = -100000; nudY.Maximum = 100000; nudY.DecimalPlaces = 1; nudY.Increment = 10;

            Label lblZ = new Label { Text = "Z 오프셋:", Location = new Point(15, 196), AutoSize = true };
            NumericUpDown nudZ = new NumericUpDown();
            nudZ.Location = new Point(100, 194); nudZ.Size = new Size(120, 25);
            nudZ.Minimum = -100000; nudZ.Maximum = 100000; nudZ.DecimalPlaces = 1; nudZ.Increment = 10;

            // 현재 위치 업데이트 함수
            Action updateCurrentPos = () =>
            {
                int idx = cmbMember.SelectedIndex;
                if (idx >= 0 && balloonOverrides.ContainsKey(idx))
                {
                    var pos = balloonOverrides[idx];
                    lblCurrentPos.Text = $"X={pos[0]:F1}, Y={pos[1]:F1}, Z={pos[2]:F1}";
                }
                else
                {
                    lblCurrentPos.Text = "(자동 배치 - 위치 미정)";
                }
            };
            cmbMember.SelectedIndexChanged += (s2, e2) => { updateCurrentPos(); nudX.Value = 0; nudY.Value = 0; nudZ.Value = 0; };
            updateCurrentPos();

            // 적용 버튼
            Button btnApply = new Button { Text = "적용", Location = new Point(15, 240), Size = new Size(100, 35) };
            btnApply.Click += (s2, e2) =>
            {
                int idx = cmbMember.SelectedIndex;
                if (idx < 0) return;

                if (balloonOverrides.ContainsKey(idx))
                {
                    balloonOverrides[idx][0] += (float)nudX.Value;
                    balloonOverrides[idx][1] += (float)nudY.Value;
                    balloonOverrides[idx][2] += (float)nudZ.Value;
                }
                else
                {
                    // 부재 중심 + 오프셋으로 초기 위치 설정
                    var bom = bomList[idx];
                    balloonOverrides[idx] = new float[] {
                        bom.CenterX + (float)nudX.Value,
                        bom.CenterY + (float)nudY.Value,
                        bom.CenterZ + (float)nudZ.Value
                    };
                }

                nudX.Value = 0; nudY.Value = 0; nudZ.Value = 0;
                ShowBalloonNumbers(currentBalloonView);
                updateCurrentPos();
            };

            // 초기화 버튼
            Button btnReset = new Button { Text = "전체 초기화", Location = new Point(125, 240), Size = new Size(100, 35) };
            btnReset.Click += (s2, e2) =>
            {
                balloonOverrides.Clear();
                string savedView = currentBalloonView;
                currentBalloonView = ""; // 강제 재계산
                ShowBalloonNumbers(savedView);
                updateCurrentPos();
            };

            // 닫기 버튼
            Button btnClose = new Button { Text = "닫기", Location = new Point(235, 240), Size = new Size(100, 35) };
            btnClose.Click += (s2, e2) => dlg.Close();

            dlg.Controls.AddRange(new Control[] { lblSelect, cmbMember, lblCurrent, lblCurrentPos,
                lblX, nudX, lblY, nudY, lblZ, nudZ, btnApply, btnReset, btnClose });
            dlg.ShowDialog(this);
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

                // 카메라 방향 설정 (줌은 호출하는 쪽에서 담당)
                if (viewDirection != null)
                {
                    switch (viewDirection)
                    {
                        case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS); break;
                        case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS); break;
                        case "Z": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS); break;
                    }
                }

                // ========== Smart Dimension Filtering 적용 ==========
                // 우선순위 기반 필터링 + 짧은 치수 병합 + 레벨 배치
                var filteredDims = ApplySmartFiltering(displayList, maxDimensionsPerAxis: 8, minTextSpace: 25.0f);

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
                measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                measureStyle.FontBold = true;
                measureStyle.LineColor = System.Drawing.Color.Blue;
                measureStyle.LineWidth = 1;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;
                measureStyle.ArrowSize = 5;
                measureStyle.AssistantLine = true;
                measureStyle.AssistantLineStyle = VIZCore3D.NET.Data.MeasureStyle.AssistantLineType.SOLIDLINE;
                measureStyle.AlignDistanceText = true;
                measureStyle.AlignDistanceTextPosition = 0;
                measureStyle.AlignDistanceTextMargine = 3;
                vizcore3d.Review.Measure.SetStyle(measureStyle);

                // baseline 계산
                float globalMinX = float.MaxValue, globalMinY = float.MaxValue, globalMinZ = float.MaxValue;
                float globalMaxX = float.MinValue, globalMaxY = float.MinValue, globalMaxZ = float.MinValue;
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
                            globalMaxX = Math.Max(globalMaxX, bom.MaxX);
                            globalMaxY = Math.Max(globalMaxY, bom.MaxY);
                            globalMaxZ = Math.Max(globalMaxZ, bom.MaxZ);
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
                        globalMaxX = Math.Max(globalMaxX, Math.Max(dim.StartPoint.X, dim.EndPoint.X));
                        globalMaxY = Math.Max(globalMaxY, Math.Max(dim.StartPoint.Y, dim.EndPoint.Y));
                        globalMaxZ = Math.Max(globalMaxZ, Math.Max(dim.StartPoint.Z, dim.EndPoint.Z));
                    }
                }
                // 모델 중심 계산 (풍선 방향 결정용)
                float modelCenterX = (globalMinX + globalMaxX) / 2f;
                float modelCenterY = (globalMinY + globalMaxY) / 2f;
                float modelCenterZ = (globalMinZ + globalMaxZ) / 2f;

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
                    vizcore3d.ShapeDrawing.AddLine(extensionLines, 0, System.Drawing.Color.FromArgb(80, 80, 200), 1.5f, true);
                }

                // ========== 풍선 통합 배치 (겹침 방지: 동일 기점 5° 회전 + 보조선 연장) ==========
                float dimBaseline_OuterOffset = baseOffset + (levelSpacing * maxLevelUsed);
                // 모델 크기에 비례하여 풍선 오프셋 결정 (최소 100mm, 모델 대각 크기의 10%)
                // (풍선 배치를 부재 옆 방식으로 변경 - 모델 외곽 배치 파라미터 제거됨)

                // 뷰 방향별 축 매핑 (hAxis=수평, vAxis=수직, dAxis=깊이)
                int bHAxis, bVAxis, bDAxis;
                switch (viewDirection)
                {
                    case "X": bHAxis = 1; bVAxis = 2; bDAxis = 0; break; // H=Y, V=Z, D=X
                    case "Y": bHAxis = 0; bVAxis = 2; bDAxis = 1; break; // H=X, V=Z, D=Y
                    case "Z": bHAxis = 0; bVAxis = 1; bDAxis = 2; break; // H=X, V=Y, D=Z
                    default:  bHAxis = 1; bVAxis = 2; bDAxis = 0; break;
                }

                // 치수선 마지막 기준선 (수평축 방향)
                float[] globalMinArr = { globalMinX, globalMinY, globalMinZ };
                float dimBaselineH = globalMinArr[bHAxis] - dimBaseline_OuterOffset;

                // 풍선 항목 수집 (기점 좌표, 텍스트, 색상)
                List<(float ox, float oy, float oz, string text, Color color)> balloonEntries =
                    new List<(float, float, float, string, Color)>();

                // --- EBOS 풍선 수집 ---
                try
                {
                    string purposeKey = null;
                    var udaKeys = vizcore3d.Object3D.UDA.Keys;
                    if (udaKeys != null)
                    {
                        foreach (string k in udaKeys)
                        {
                            if (k.Trim().ToUpper() == "PURPOSE") { purposeKey = k; break; }
                        }
                    }
                    if (purposeKey != null)
                    {
                        var allNodes = vizcore3d.Object3D.GetPartialNode(true, true, true);
                        if (allNodes != null)
                        {
                            foreach (var node in allNodes)
                            {
                                try
                                {
                                    var val = vizcore3d.Object3D.UDA.FromIndex(node.Index, purposeKey);
                                    if (val == null || val.ToString().Trim().ToUpper() != "EBOS") continue;
                                    var bboxI = new List<int> { node.Index };
                                    var bbox = vizcore3d.Object3D.GetBoundBox(bboxI, false);
                                    if (bbox == null) continue;
                                    balloonEntries.Add((
                                        (bbox.MinX + bbox.MaxX) / 2f,
                                        (bbox.MinY + bbox.MaxY) / 2f,
                                        (bbox.MinZ + bbox.MaxZ) / 2f,
                                        "EarthBoss", Color.Blue));
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                // --- 원형(CIRCLE) 풍선 수집 (홀로 매칭된 원기둥은 제외) ---
                try
                {
                    // 홀로 매칭된 원기둥 Body Index 수집
                    HashSet<int> holeCylinderIndices = new HashSet<int>();
                    foreach (var b in bomList)
                    {
                        if (b.Holes != null)
                            foreach (var h in b.Holes)
                                holeCylinderIndices.Add(h.CylinderBodyIndex);
                    }
                    foreach (var bom in bomList)
                    {
                        if (bom.CircleRadius <= 0) continue;
                        if (holeCylinderIndices.Contains(bom.Index)) continue; // 홀 원기둥은 스킵

                        // 바운딩박스 형태가 원기둥이 아닌 body는 원형 풍선 제외 (Angle 등)
                        float diameter = bom.CircleRadius * 2f;
                        float sX = Math.Abs(bom.MaxX - bom.MinX);
                        float sY = Math.Abs(bom.MaxY - bom.MinY);
                        float sZ = Math.Abs(bom.MaxZ - bom.MinZ);
                        float cTol = Math.Max(2f, diameter * 0.2f);
                        int mc = 0;
                        if (Math.Abs(sX - diameter) < cTol) mc++;
                        if (Math.Abs(sY - diameter) < cTol) mc++;
                        if (Math.Abs(sZ - diameter) < cTol) mc++;
                        if (mc < 2) continue; // 원기둥 형태가 아니면 스킵

                        balloonEntries.Add((bom.CenterX, bom.CenterY, bom.CenterZ,
                            $"R{bom.CircleRadius:F1}", Color.Red));
                    }
                }
                catch { }

                // --- 홀(Hole) 풍선 수집 (BOM 부재별 같은 직경 그룹핑, 선택 노드만) ---
                try
                {
                    // 선택된 노드 집합 (필터링용)
                    HashSet<int> selectedSet = (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                        ? new HashSet<int>(xraySelectedNodeIndices) : null;

                    foreach (var bom in bomList)
                    {
                        if (bom.Holes == null || bom.Holes.Count == 0) continue;
                        // 선택된 노드가 있으면 해당 노드의 홀만 표시
                        if (selectedSet != null && !selectedSet.Contains(bom.Index)) continue;
                        // BOM별로 같은 직경 홀 그룹핑
                        var holeGroups = bom.Holes.GroupBy(h => Math.Round(h.Diameter, 1));
                        foreach (var grp in holeGroups)
                        {
                            int count = grp.Count();
                            string holeText = count > 1
                                ? $"\u00d8{grp.Key:F1} * {count}개"
                                : $"\u00d8{grp.Key:F1}";
                            // 대표 홀(첫 번째)에만 풍선 표시
                            var firstHole = grp.First();
                            balloonEntries.Add((firstHole.CenterX, firstHole.CenterY, firstHole.CenterZ,
                                holeText, Color.FromArgb(0, 160, 0)));
                        }
                    }
                }
                catch { }

                // --- 슬롯홀(SlotHole) 풍선 수집 (같은 사이즈 그룹핑, 1풍선/사이즈) ---
                try
                {
                    HashSet<int> slotSelectedSet = (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                        ? new HashSet<int>(xraySelectedNodeIndices) : null;

                    foreach (var bom in bomList)
                    {
                        if (bom.SlotHoles == null || bom.SlotHoles.Count == 0) continue;
                        if (slotSelectedSet != null && !slotSelectedSet.Contains(bom.Index)) continue;

                        // 같은 사이즈(반지름+길이+깊이) 슬롯홀 그룹핑
                        var slotGroups = bom.SlotHoles.GroupBy(s =>
                            $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                        foreach (var grp in slotGroups)
                        {
                            var first = grp.First();
                            int count = grp.Count();
                            float slotWidth = first.Radius * 2f;
                            string slotText = count > 1
                                ? $"R{first.Radius:F1}/({slotWidth:F0}*{first.SlotLength:F0}*{first.Depth:F0}) * {count}개"
                                : $"R{first.Radius:F1}/({slotWidth:F0}*{first.SlotLength:F0}*{first.Depth:F0})";
                            balloonEntries.Add((first.CenterX, first.CenterY, first.CenterZ,
                                slotText, Color.FromArgb(180, 0, 180)));
                        }
                    }
                }
                catch { }

                // --- 풍선 일괄 배치 (부재/홀 바로 옆에 배치, 겹침 방지) ---
                try
                {
                    Func<float, float, float, int, float> getComp = (x, y, z, axis) =>
                        axis == 0 ? x : (axis == 1 ? y : z);

                    // 모델 중심 (풍선 방향 결정용)
                    float[] modelCenterArr = { modelCenterX, modelCenterY, modelCenterZ };
                    float modelCenterH = modelCenterArr[bHAxis];
                    float modelCenterV = modelCenterArr[bVAxis];

                    float balloonOffset = 40f; // 부재 근처 초기 오프셋
                    float minBalloonDist = 20f; // 풍선 간 최소 거리
                    float bomPad = 5f; // 부재 바운딩박스 패딩

                    // 부재 바운딩박스를 2D(H,V) 기준으로 미리 계산
                    Func<BOMData, int, float> getBomMin = (b, ax) =>
                        ax == 0 ? b.MinX : (ax == 1 ? b.MinY : b.MinZ);
                    Func<BOMData, int, float> getBomMax = (b, ax) =>
                        ax == 0 ? b.MaxX : (ax == 1 ? b.MaxY : b.MaxZ);

                    // 배치된 풍선 텍스트 위치 목록 (겹침 판정용)
                    List<(float h, float v)> placedTextPositions = new List<(float, float)>();

                    foreach (var entry in balloonEntries)
                    {
                        try
                        {
                            // 기점의 2D 화면좌표 (수평, 수직, 깊이)
                            float originH = getComp(entry.ox, entry.oy, entry.oz, bHAxis);
                            float originV = getComp(entry.ox, entry.oy, entry.oz, bVAxis);
                            float originD = getComp(entry.ox, entry.oy, entry.oz, bDAxis);

                            // 모델 중심 → 홀 위치 방향으로 오프셋
                            float dirH = originH - modelCenterH;
                            float dirV = originV - modelCenterV;
                            float dirLen = (float)Math.Sqrt(dirH * dirH + dirV * dirV);

                            if (dirLen < 0.001f)
                            {
                                dirH = 1f; dirV = 0f; dirLen = 1f;
                            }
                            dirH /= dirLen;
                            dirV /= dirLen;

                            float candidateH = originH + dirH * balloonOffset;
                            float candidateV = originV + dirV * balloonOffset;

                            // 겹침 방지: 다른 풍선 + 부재 바운딩박스(2D 투영)와 겹치면 회전
                            bool positionFound = false;
                            for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                            {
                                bool collision = false;

                                // 다른 풍선과 겹침 검사
                                foreach (var placed in placedTextPositions)
                                {
                                    float dh = candidateH - placed.h;
                                    float dv = candidateV - placed.v;
                                    if (Math.Sqrt(dh * dh + dv * dv) < minBalloonDist)
                                    { collision = true; break; }
                                }

                                // 부재 바운딩박스(2D 투영)와 겹침 검사
                                if (!collision)
                                {
                                    foreach (var bom in bomList)
                                    {
                                        float bMinH = getBomMin(bom, bHAxis) - bomPad;
                                        float bMaxH = getBomMax(bom, bHAxis) + bomPad;
                                        float bMinV = getBomMin(bom, bVAxis) - bomPad;
                                        float bMaxV = getBomMax(bom, bVAxis) + bomPad;
                                        if (candidateH >= bMinH && candidateH <= bMaxH &&
                                            candidateV >= bMinV && candidateV <= bMaxV)
                                        { collision = true; break; }
                                    }
                                }

                                if (!collision)
                                {
                                    positionFound = true;
                                }
                                else
                                {
                                    float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                                    if (attempt % 2 == 1) rotAngle = -rotAngle;
                                    float cosA = (float)Math.Cos(rotAngle);
                                    float sinA = (float)Math.Sin(rotAngle);
                                    float newDirH = cosA * dirH - sinA * dirV;
                                    float newDirV = sinA * dirH + cosA * dirV;
                                    float newOffset = balloonOffset * (1f + (attempt / 4) * 0.15f);
                                    candidateH = originH + newDirH * newOffset;
                                    candidateV = originV + newDirV * newOffset;
                                }
                            }

                            // 3D 좌표로 복원
                            float[] textXYZ = new float[3];
                            textXYZ[bHAxis] = candidateH;
                            textXYZ[bVAxis] = candidateV;
                            textXYZ[bDAxis] = originD;

                            VIZCore3D.NET.Data.Vertex3D arrowPos = new VIZCore3D.NET.Data.Vertex3D(
                                entry.ox, entry.oy, entry.oz);
                            VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(
                                textXYZ[0], textXYZ[1], textXYZ[2]);

                            VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                            style.UseSymbol = false;
                            style.BackgroudTransparent = true;
                            style.FontBold = true;
                            style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                            style.FontColor = entry.color;
                            style.LineColor = entry.color;
                            style.LineWidth = 1;
                            style.ArrowColor = entry.color;
                            style.ArrowWidth = 3;

                            vizcore3d.Review.Note.AddNoteSurface(entry.text, textPos, arrowPos, style);
                            placedTextPositions.Add((candidateH, candidateV));
                        }
                        catch { }
                    }
                }
                catch (Exception balloonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"풍선 배치 오류: {balloonEx.Message}");
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

            // 치수 거리 계산
            float distance = 0;
            switch (axis)
            {
                case "X": distance = Math.Abs(endPoint.X - startPoint.X); break;
                case "Y": distance = Math.Abs(endPoint.Y - startPoint.Y); break;
                case "Z": distance = Math.Abs(endPoint.Z - startPoint.Z); break;
            }

            if (distance > 0.1f)
            {
                // === 모든 치수: AddCustomAxisDistance 사용 ===
                // AlignDistanceTextPosition = 0 (치수선 중앙 위 배치) 설정으로
                // 텍스트가 치수선 위 모델 반대 방향에 표시
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
            }

            // 보조선 추가 (원본 → baseline 오프셋 위치) - 항상 표시
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
        /// 부재 이름 입력 TextBox를 3D 뷰어(panelViewer) 위에 오버레이로 표시
        /// </summary>
        private void ShowMemberNameOverlay(string initialName)
        {
            if (txtMemberNameOverlay == null)
            {
                txtMemberNameOverlay = new TextBox();
                txtMemberNameOverlay.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                txtMemberNameOverlay.BackColor = Color.FromArgb(45, 45, 48);
                txtMemberNameOverlay.ForeColor = Color.White;
                txtMemberNameOverlay.BorderStyle = BorderStyle.FixedSingle;
                txtMemberNameOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                txtMemberNameOverlay.Location = new Point(10, 5);
                txtMemberNameOverlay.Width = panelViewer.Width - 20;
                panelViewer.Controls.Add(txtMemberNameOverlay);
            }
            txtMemberNameOverlay.Text = initialName ?? "";
            txtMemberNameOverlay.BringToFront();
            txtMemberNameOverlay.Visible = true;
        }

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
                // 선택된 도면시트의 BaseMemberName을 가져와 TextBox 오버레이 표시
                string memberName = "";
                if (lvDrawingSheet.SelectedItems.Count > 0)
                {
                    DrawingSheetData selectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
                    if (selectedSheet != null)
                        memberName = selectedSheet.BaseMemberName ?? "";
                }
                ShowMemberNameOverlay(memberName);

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

            try
            {
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

                // 5. 카메라 방향 설정
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
                    default:
                        camDir = VIZCore3D.NET.Data.CameraDirection.Y_PLUS;
                        viewDirection = "Y";
                        needRotate90 = true;
                        break;
                }

                vizcore3d.View.MoveCamera(camDir);

                if (needRotate90)
                {
                    bool originalLockZ = vizcore3d.View.ScreenAxisRotation.LockZAxis;
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                    vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    vizcore3d.View.ScreenAxisRotation.LockZAxis = originalLockZ;
                }

                // 6. 화면 맞춤
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

                // 좌표 병합 + 치수 추출
                float tolerance = 5.0f;
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(mfgOsnapWithNames, tolerance);

                var mfgDimensions = new List<ChainDimensionData>();
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "X", tolerance));
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "Y", tolerance));
                mfgDimensions.AddRange(AddChainDimensionByAxis(mergedPoints, "Z", tolerance));

                if (mfgDimensions.Count == 0)
                {
                    vizcore3d.Object3D.Show(allIndices, true);
                    return;
                }

                // 전역 상태 임시 교체 후 치수 표시
                var savedChainDimList = new List<ChainDimensionData>(chainDimensionList);
                var savedXrayIndices = new List<int>(xraySelectedNodeIndices);

                chainDimensionList.Clear();
                chainDimensionList.AddRange(mfgDimensions);
                xraySelectedNodeIndices = new List<int>(targetIndices);

                AddDimensionsForView(viewDirection);

                chainDimensionList.Clear();
                chainDimensionList.AddRange(savedChainDimList);
                xraySelectedNodeIndices = new List<int>(savedXrayIndices);

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

                        vizcore3d.Review.Note.AddNoteSurface($"R{bom.CircleRadius:F1}", center, textPos, circleStyle);
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

                            vizcore3d.Review.Note.AddNoteSurface(slotText, slotCenter, slotTextPos, slotStyle);
                            balloonIdx++;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"가공도 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 도면정보 탭 - 가공도 출력 버튼 클릭
        /// 선택된 가공도 시트의 부재에 대해 가공도 출력 실행
        /// </summary>
        private void btnMfgDrawingSheet_Click(object sender, EventArgs e)
        {
            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                MessageBox.Show("도면정보에서 가공도 시트를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            if (sheet == null || sheet.MemberIndices.Count == 0)
            {
                MessageBox.Show("유효한 시트가 아닙니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sheet.BaseMemberIndex != -3)
            {
                MessageBox.Show("가공도 시트를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExecuteMfgDrawing(sheet.MemberIndices[0]);
        }

        #endregion

        #region 도면 시트 생성 (BFS)

        /// <summary>
        /// Clash 인접 리스트 기반 BFS로 도면 시트 생성
        /// </summary>
        private void GenerateDrawingSheets()
        {
            drawingSheetList.Clear();
            lvDrawingSheet.Items.Clear();

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sheet 1: 전체 BOM 부재
            DrawingSheetData sheet1 = new DrawingSheetData();
            sheet1.SheetNumber = 1;

            // 선택한 노드 이름 사용, 없으면 파일명 사용
            if (selectedAttributeNodeIndex != -1)
            {
                var selectedNode = vizcore3d.Object3D.FromIndex(selectedAttributeNodeIndex);
                sheet1.BaseMemberName = (selectedNode != null && !string.IsNullOrEmpty(selectedNode.NodeName))
                    ? selectedNode.NodeName
                    : System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
            }
            else
            {
                sheet1.BaseMemberName = !string.IsNullOrEmpty(currentFilePath)
                    ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath)
                    : "전체";
            }
            sheet1.BaseMemberIndex = -1;
            foreach (var bom in bomList)
            {
                sheet1.MemberIndices.Add(bom.Index);
                sheet1.MemberNames.Add(bom.Name);
            }
            drawingSheetList.Add(sheet1);

            // BOM 인덱스 → BOM 이름 매핑
            Dictionary<int, string> bomIndexToName = new Dictionary<int, string>();
            HashSet<int> bomIndexSet = new HashSet<int>();
            foreach (var bom in bomList)
            {
                bomIndexToName[bom.Index] = bom.Name;
                bomIndexSet.Add(bom.Index);
            }

            // Part Index → Body Index 리스트 (역매핑)
            Dictionary<int, List<int>> partToBodyIndices = new Dictionary<int, List<int>>();
            foreach (var bom in bomList)
            {
                if (bodyToPartIndexMap.ContainsKey(bom.Index))
                {
                    int partIdx = bodyToPartIndexMap[bom.Index];
                    if (!partToBodyIndices.ContainsKey(partIdx))
                        partToBodyIndices[partIdx] = new List<int>();
                    partToBodyIndices[partIdx].Add(bom.Index);
                }
            }

            // Clash 인접 리스트 구축 (Part → Body 변환하여 Body 기반 매칭)
            Dictionary<int, HashSet<int>> adjacencyByIndex = new Dictionary<int, HashSet<int>>();
            foreach (var clash in clashList)
            {
                // Clash.Index1/Index2는 Part 인덱스 → Body 인덱스로 변환
                List<int> bodies1 = partToBodyIndices.ContainsKey(clash.Index1) ? partToBodyIndices[clash.Index1] : new List<int>();
                List<int> bodies2 = partToBodyIndices.ContainsKey(clash.Index2) ? partToBodyIndices[clash.Index2] : new List<int>();

                // 두 Part에 속한 모든 Body들 간에 연결 추가
                foreach (int bodyIdx1 in bodies1)
                {
                    foreach (int bodyIdx2 in bodies2)
                    {
                        if (bodyIdx1 == bodyIdx2) continue;

                        if (!adjacencyByIndex.ContainsKey(bodyIdx1))
                            adjacencyByIndex[bodyIdx1] = new HashSet<int>();
                        if (!adjacencyByIndex.ContainsKey(bodyIdx2))
                            adjacencyByIndex[bodyIdx2] = new HashSet<int>();

                        adjacencyByIndex[bodyIdx1].Add(bodyIdx2);
                        adjacencyByIndex[bodyIdx2].Add(bodyIdx1);
                    }
                }
            }

            // Sheet 2~: BOM 순서대로 순회
            // appearedAsIncluded: 다른 시트의 포함부재에 나온 인덱스 (기준부재 스킵용)
            HashSet<int> appearedAsIncluded = new HashSet<int>();
            int sheetNumber = 2;

            foreach (var bom in bomList)
            {
                // 이미 다른 시트의 포함부재에 나온 부재면 기준부재로 스킵
                if (appearedAsIncluded.Contains(bom.Index))
                    continue;

                DrawingSheetData sheet = new DrawingSheetData();
                sheet.SheetNumber = sheetNumber;
                sheet.BaseMemberIndex = bom.Index;
                sheet.BaseMemberName = bom.Name;

                // 포함부재: 기준부재 자신
                sheet.MemberIndices.Add(bom.Index);
                sheet.MemberNames.Add(bom.Name);

                // 포함부재: Clash에서 기준부재와 연결된 모든 부재 (Index 기반)
                if (adjacencyByIndex.ContainsKey(bom.Index))
                {
                    foreach (int neighborIndex in adjacencyByIndex[bom.Index])
                    {
                        // 같은 시트 내 중복만 방지
                        if (!sheet.MemberIndices.Contains(neighborIndex))
                        {
                            sheet.MemberIndices.Add(neighborIndex);
                            if (bomIndexToName.ContainsKey(neighborIndex))
                                sheet.MemberNames.Add(bomIndexToName[neighborIndex]);
                        }
                        // 포함부재로 등록 → 이후 기준부재로 선정되지 않음
                        appearedAsIncluded.Add(neighborIndex);
                    }
                }

                drawingSheetList.Add(sheet);
                sheetNumber++;
            }

            // 마지막 시트: 설치도 (모든 연결된 부재를 BFS로 탐색 - Index 기반)
            HashSet<int> installMemberIndices = new HashSet<int>();
            Queue<int> bfsQueue = new Queue<int>();

            // 첫 번째 BOM 부재부터 BFS 시작
            if (bomList.Count > 0 && adjacencyByIndex.Count > 0)
            {
                int startIndex = bomList[0].Index;
                bfsQueue.Enqueue(startIndex);
                installMemberIndices.Add(startIndex);

                while (bfsQueue.Count > 0)
                {
                    int current = bfsQueue.Dequeue();
                    if (adjacencyByIndex.ContainsKey(current))
                    {
                        foreach (int neighbor in adjacencyByIndex[current])
                        {
                            if (!installMemberIndices.Contains(neighbor))
                            {
                                installMemberIndices.Add(neighbor);
                                bfsQueue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            // BFS에 포함되지 않은 독립 부재도 추가 (Clash가 없는 부재)
            foreach (var bom in bomList)
            {
                installMemberIndices.Add(bom.Index);
            }

            DrawingSheetData installSheet = new DrawingSheetData();
            installSheet.SheetNumber = sheetNumber;
            installSheet.BaseMemberName = "설치도";
            installSheet.BaseMemberIndex = -2; // 설치도 식별자
            foreach (var bom in bomList)
            {
                if (installMemberIndices.Contains(bom.Index))
                {
                    installSheet.MemberIndices.Add(bom.Index);
                    installSheet.MemberNames.Add(bom.Name);
                }
            }
            drawingSheetList.Add(installSheet);
            sheetNumber++;

            // 가공도 시트: BOM 부재를 한 줄씩 추가
            int mfgNo = 1;
            foreach (var bom in bomList)
            {
                DrawingSheetData mfgSheet = new DrawingSheetData();
                mfgSheet.SheetNumber = sheetNumber;
                mfgSheet.BaseMemberName = bom.Name;
                mfgSheet.BaseMemberIndex = -3; // 가공도 식별자
                mfgSheet.MemberIndices.Add(bom.Index);
                mfgSheet.MemberNames.Clear(); // 포함부재 비우기
                mfgSheet.MfgDrawingNo = mfgNo; // 가공도 번호
                drawingSheetList.Add(mfgSheet);
                sheetNumber++;
                mfgNo++;
            }

            // ListView 갱신
            foreach (var sheet in drawingSheetList)
            {
                string sheetLabel;
                if (sheet.BaseMemberIndex == -3) // 가공도
                    sheetLabel = $"가공도_{sheet.MfgDrawingNo}";
                else
                    sheetLabel = $"Sheet {sheet.SheetNumber}";

                ListViewItem lvi = new ListViewItem(sheetLabel);
                lvi.SubItems.Add(sheet.BaseMemberName);
                lvi.SubItems.Add(sheet.BaseMemberIndex == -3 ? "" : string.Join(", ", sheet.MemberNames));
                lvi.SubItems.Add(sheet.MemberIndices.Count.ToString());
                lvi.Tag = sheet;
                lvDrawingSheet.Items.Add(lvi);
            }
        }

        /// <summary>
        /// 도면 생성 버튼 핸들러
        /// </summary>
        private void btnGenerateSheets_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (clashList.Count == 0)
            {
                MessageBox.Show("Clash 데이터가 없습니다. 먼저 Clash 검사를 수행해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GenerateDrawingSheets();
            MessageBox.Show($"도면 시트 {drawingSheetList.Count}개가 생성되었습니다.", "도면 생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 도면 시트 선택 시 X-Ray + 치수 표시
        /// </summary>
        private void LvDrawingSheet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrawingSheet.SelectedItems.Count == 0)
                return;

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            if (sheet == null || sheet.MemberIndices.Count == 0)
                return;

            try
            {
                vizcore3d.BeginUpdate();

                // X-Ray 모드 비활성화 (관련 부재만 완전히 표시하기 위해)
                if (vizcore3d.View.XRay.Enable)
                {
                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Enable = false;
                }

                // 모든 부재 숨기기
                List<int> allIndices = new List<int>();
                foreach (BOMData b in bomList)
                    allIndices.Add(b.Index);
                if (allIndices.Count > 0)
                    vizcore3d.Object3D.Show(allIndices, false);

                // 선택된 시트의 부재만 표시
                vizcore3d.Object3D.Show(sheet.MemberIndices, true);

                // 모서리(SilhouetteEdge) 표시
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 선택된 노드 인덱스 저장 (글로벌 뷰 버튼용)
                xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                // 선택된 노드로 화면 이동
                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);

                // 이전 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // 기존 풍선(Note) 제거
                vizcore3d.Review.Note.Clear();

                vizcore3d.EndUpdate();

                // 설치도 시트: 부재 바운딩박스 경계 기반 체인치수
                // 가공도 시트: 단일 부재 가공도 출력
                // 일반 시트: Osnap 기반 체인치수
                if (sheet.BaseMemberIndex == -3) // 가공도
                {
                    ExecuteMfgDrawing(sheet.MemberIndices[0]);
                }
                else if (sheet.BaseMemberIndex == -2) // 설치도
                {
                    ExtractInstallationDimensions(sheet.MemberIndices);
                }
                else
                {
                    CollectOsnapForSelectedNodes(sheet.MemberIndices);
                    ExtractDimensionForSelectedNodes();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"도면 시트 표시 중 오류: {ex.Message}");
            }

            // 선택된 시트 기준으로 BOM정보 자동 수집 (알람 없이)
            CollectBOMInfo(false);
        }

        /// <summary>
        /// 도면정보 탭 - 선택된 시트의 포함부재를 X-Ray 선택 + Osnap/치수 추출 + 방향 보기
        /// </summary>
        private void ApplyDrawingSheetView(string viewDirection)
        {
            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                MessageBox.Show("도면 시트를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            if (sheet == null || sheet.MemberIndices.Count == 0)
                return;

            try
            {
                if (viewDirection == "ISO")
                {
                    // ISO: 전체 X-Ray 설정 + Osnap/치수 수집 + 풍선 표시
                    vizcore3d.BeginUpdate();

                    if (!vizcore3d.View.XRay.Enable)
                        vizcore3d.View.XRay.Enable = true;

                    vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                    vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                    xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
                    vizcore3d.Clash.ClearResultSymbol();

                    vizcore3d.EndUpdate();

                    if (sheet.BaseMemberIndex == -2)
                    {
                        ExtractInstallationDimensions(sheet.MemberIndices);
                    }
                    else
                    {
                        CollectOsnapForSelectedNodes(sheet.MemberIndices);
                        ExtractDimensionForSelectedNodes();
                    }

                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                    // 선택된 부재에 맞춰 화면 조정 (반복 호출 시 줌 누적 방지)
                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.0f);
                    ShowBalloonNumbers("ISO");
                }
                else
                {
                    // X/Y/Z: 시트 선택 시 이미 수집된 Osnap/치수 데이터 재활용
                    // X-Ray 모드 유지 + 방향 전환 + 렌더모드 + 치수 표시
                    vizcore3d.BeginUpdate();

                    // X-Ray 모드 유지 (해당 부재만 보이도록)
                    if (!vizcore3d.View.XRay.Enable)
                        vizcore3d.View.XRay.Enable = true;

                    vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                    vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                    xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                    vizcore3d.EndUpdate();

                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

                    // 카메라 방향 설정
                    switch (viewDirection)
                    {
                        case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS); break;
                        case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS); break;
                        case "Z": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS); break;
                    }

                    // 선택된 부재에 맞춰 화면 조정
                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.0f);
                    ShowAllDimensions(viewDirection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"도면 시트 뷰 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDrawingISO_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("ISO");
        }

        private void btnDrawingAxisX_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("X");
        }

        private void btnDrawingAxisY_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("Y");
        }

        private void btnDrawingAxisZ_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("Z");
        }

        #region 글로벌 뷰 버튼 핸들러 (탭 공통)

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
                ShowBalloonNumbers("ISO");
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
                ShowBalloonNumbers("ISO");
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

            if (members.Count < 2) return;

            float tolerance = 1.0f;

            // 축별로 부재 경계값(Min, Max)을 수집하여 체인치수 생성
            string[] axes = { "X", "Y", "Z" };
            foreach (string axis in axes)
            {
                // 각 부재의 Min, Max 경계값 수집
                List<float> boundaries = new List<float>();
                foreach (var m in members)
                {
                    float minVal = 0, maxVal = 0;
                    switch (axis)
                    {
                        case "X": minVal = m.MinX; maxVal = m.MaxX; break;
                        case "Y": minVal = m.MinY; maxVal = m.MaxY; break;
                        case "Z": minVal = m.MinZ; maxVal = m.MaxZ; break;
                    }
                    boundaries.Add(minVal);
                    boundaries.Add(maxVal);
                }

                // 중복 제거 후 정렬 (큰 값부터)
                List<float> unique = new List<float>();
                boundaries.Sort();
                foreach (float v in boundaries)
                {
                    if (unique.Count == 0 || Math.Abs(v - unique[unique.Count - 1]) > tolerance)
                        unique.Add(v);
                }
                unique.Reverse(); // 큰 값부터 (체인치수 방식)

                if (unique.Count < 2) continue;

                // 기준선 좌표 생성 (다른 축의 최소값 사용)
                float refVal1 = float.MaxValue, refVal2 = float.MaxValue;
                foreach (var m in members)
                {
                    switch (axis)
                    {
                        case "X": refVal1 = Math.Min(refVal1, m.MinY); refVal2 = Math.Min(refVal2, m.MinZ); break;
                        case "Y": refVal1 = Math.Min(refVal1, m.MinX); refVal2 = Math.Min(refVal2, m.MinZ); break;
                        case "Z": refVal1 = Math.Min(refVal1, m.MinX); refVal2 = Math.Min(refVal2, m.MinY); break;
                    }
                }

                // 순차 체인 치수 (인접 경계 간)
                for (int i = 0; i < unique.Count - 1; i++)
                {
                    float dist = Math.Abs(unique[i] - unique[i + 1]);
                    if (dist <= tolerance) continue;

                    VIZCore3D.NET.Data.Vector3D startPt, endPt;
                    switch (axis)
                    {
                        case "X":
                            startPt = new VIZCore3D.NET.Data.Vector3D(unique[i], refVal1, refVal2);
                            endPt = new VIZCore3D.NET.Data.Vector3D(unique[i + 1], refVal1, refVal2);
                            break;
                        case "Y":
                            startPt = new VIZCore3D.NET.Data.Vector3D(refVal1, unique[i], refVal2);
                            endPt = new VIZCore3D.NET.Data.Vector3D(refVal1, unique[i + 1], refVal2);
                            break;
                        default: // Z
                            startPt = new VIZCore3D.NET.Data.Vector3D(refVal1, refVal2, unique[i]);
                            endPt = new VIZCore3D.NET.Data.Vector3D(refVal1, refVal2, unique[i + 1]);
                            break;
                    }

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

                // 전체 치수 (처음~끝, 순차가 2개 이상일 때)
                if (unique.Count > 2)
                {
                    float totalDist = Math.Abs(unique[0] - unique[unique.Count - 1]);
                    VIZCore3D.NET.Data.Vector3D totalStart, totalEnd;
                    switch (axis)
                    {
                        case "X":
                            totalStart = new VIZCore3D.NET.Data.Vector3D(unique[0], refVal1, refVal2);
                            totalEnd = new VIZCore3D.NET.Data.Vector3D(unique[unique.Count - 1], refVal1, refVal2);
                            break;
                        case "Y":
                            totalStart = new VIZCore3D.NET.Data.Vector3D(refVal1, unique[0], refVal2);
                            totalEnd = new VIZCore3D.NET.Data.Vector3D(refVal1, unique[unique.Count - 1], refVal2);
                            break;
                        default: // Z
                            totalStart = new VIZCore3D.NET.Data.Vector3D(refVal1, refVal2, unique[0]);
                            totalEnd = new VIZCore3D.NET.Data.Vector3D(refVal1, refVal2, unique[unique.Count - 1]);
                            break;
                    }

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

            xraySelectedNodeIndices = new List<int>(memberIndices);
        }

        #endregion
    }

    /// <summary>
    /// 체인 치수 데이터 구조체
    /// </summary>
    public class ChainDimensionData
    {
        /// <summary>
        /// 치수 번호 (ListView와 일치)
        /// </summary>
        public int No { get; set; }
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
        public float CircleRadius { get; set; }
        public string Purpose { get; set; }
        public string HoleSize { get; set; }
        public List<HoleInfo> Holes { get; set; }
        public string SlotHoleSize { get; set; }
        public List<SlotHoleInfo> SlotHoles { get; set; }

        public BOMData()
        {
            Holes = new List<HoleInfo>();
            HoleSize = "";
            SlotHoles = new List<SlotHoleInfo>();
            SlotHoleSize = "";
        }
    }

    /// <summary>
    /// 홀 정보 구조체
    /// </summary>
    public class HoleInfo
    {
        public float Diameter { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public int CylinderBodyIndex { get; set; }
    }

    /// <summary>
    /// 슬롯홀 정보 구조체
    /// </summary>
    public class SlotHoleInfo
    {
        public float Radius { get; set; }
        public float SlotLength { get; set; }
        public float Depth { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
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

    /// <summary>
    /// 도면 시트 데이터 구조체
    /// </summary>
    public class DrawingSheetData
    {
        public int SheetNumber { get; set; }
        public string BaseMemberName { get; set; }
        public int BaseMemberIndex { get; set; }
        public List<int> MemberIndices { get; set; }
        public List<string> MemberNames { get; set; }
        public int MfgDrawingNo { get; set; }

        public DrawingSheetData()
        {
            MemberIndices = new List<int>();
            MemberNames = new List<string>();
        }
    }
}
