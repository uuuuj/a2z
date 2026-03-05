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
        /// 라이선스 갱신 타이머 (30분마다 갱신)
        /// </summary>
        private System.Windows.Forms.Timer licenseRefreshTimer;

        /// <summary>
        /// 부재 이름 입력 TextBox (3D 뷰어 위 오버레이)
        /// </summary>
        private TextBox txtMemberNameOverlay = null;

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
    }
}
