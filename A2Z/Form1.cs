using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VIZCore3D.NET;


namespace A2Z
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WHEEL_DELTA = 120;

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
        /// 부재 이름 입력 TextBox (3D 뷰어 위 오버레이)
        /// </summary>
        private TextBox txtMemberNameOverlay = null;

        /// <summary>
        /// 장시간 작업 중 표시하는 "처리 중..." 오버레이 라벨 (T-018)
        /// ShowBusyOverlay/HideBusyOverlay로 제어
        /// </summary>
        private Label busyOverlay = null;

        /// <summary>
        /// T-032: CollectAllOsnap이 마지막으로 수집한 부재별 Osnap 맵.
        /// ComputeViewDimensionsForMembers 호출 시 재사용해 GetOsnapPoint 중복 호출 방지.
        /// 시트 선택 자동 경로(다른 부재 집합)에서는 null 대신 빈 맵을 전달받고 내부에서 재구축.
        /// </summary>
        private Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>> _lastCollectedNodeOsnapMap
            = new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D, string)>>();

        /// <summary>
        /// T-036: ExecuteMfgDrawing이 Z 최장축 90° 회전 직후 저장한 카메라 스냅샷.
        /// LvDrawingSheet_SelectedIndexChanged 말미에 SetCameraData(false)로 복원해
        /// 외부 FitToView가 ScreenAxisRotation 회전을 리셋한 경우를 방어.
        /// </summary>
        private VIZCore3D.NET.Data.CameraData _mfgDrawingCameraSnapshot = null;

        /// <summary>
        /// Osnap 자동 처리 성공 여부
        /// </summary>
        private bool _autoProcessOsnapSuccess = false;

        /// <summary>
        /// 현재 풍선이 표시된 부재 인덱스 리스트
        /// </summary>
        private List<int> currentBalloonMemberIndices = null;

        /// <summary>
        /// 체인 치수 데이터 리스트
        /// </summary>
        private List<ChainDimensionData> chainDimensionList = new List<ChainDimensionData>();

        /// <summary>
        /// 진단 로그 파일 경로 — {exe 폴더}/logs/diag-{YYYY-MM-DD}.log
        /// Release 빌드에서도 동작해 다른 기기에서 재현한 이슈를 추적 가능.
        /// </summary>
        private static readonly string _diagLogPath =
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "logs",
                $"diag-{DateTime.Now:yyyy-MM-dd}.log");

        /// <summary>
        /// 진단 로그 출력 — 파일과 VS 출력창(Debug) 양쪽에 기록.
        /// 파일 쓰기 실패는 앱 흐름에 영향을 주지 않도록 삼킴.
        /// </summary>
        private static void DiagLog(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            try
            {
                string dir = System.IO.Path.GetDirectoryName(_diagLogPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(_diagLogPath, line + Environment.NewLine);
                }
            }
            catch { /* 로깅 실패는 무시 */ }
            System.Diagnostics.Debug.WriteLine(line);
        }

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
            lvDrawingBOMInfo.SelectedIndexChanged += LvDrawingBOMInfo_SelectedIndexChanged;

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
        /// 3D 뷰어 중앙에 "처리 중..." 오버레이 라벨 표시 (T-018).
        /// 장시간 블로킹 작업 전 호출 → 반드시 finally에서 HideBusyOverlay() 호출.
        /// </summary>
        private void ShowBusyOverlay(string message = "처리 중...")
        {
            if (busyOverlay == null)
            {
                busyOverlay = new Label();
                busyOverlay.AutoSize = false;
                busyOverlay.TextAlign = ContentAlignment.MiddleCenter;
                busyOverlay.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
                busyOverlay.ForeColor = Color.White;
                busyOverlay.BackColor = Color.FromArgb(45, 45, 48);
                busyOverlay.BorderStyle = BorderStyle.FixedSingle;
                busyOverlay.Size = new Size(260, 70);
                busyOverlay.Visible = false;
                panelViewer.Controls.Add(busyOverlay);
            }
            busyOverlay.Text = message;
            busyOverlay.Location = new Point(
                Math.Max(0, (panelViewer.ClientSize.Width - busyOverlay.Width) / 2),
                Math.Max(0, (panelViewer.ClientSize.Height - busyOverlay.Height) / 2));
            busyOverlay.BringToFront();
            busyOverlay.Visible = true;
            Application.DoEvents(); // 즉시 화면 갱신
        }

        /// <summary>
        /// 오버레이 해제 (T-018). ShowBusyOverlay와 쌍으로 사용.
        /// </summary>
        private void HideBusyOverlay()
        {
            if (busyOverlay != null)
            {
                busyOverlay.Visible = false;
            }
        }
    }
}
