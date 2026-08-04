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

        private const string FabricationNeighborClashTestName = "제작도_근접후보_간섭검사";
        private const float FabricationNeighborClearance = 3.0f;

        /// <summary>
        /// 제작도 점선용 연결 부재 검사 결과. 기존 대상 내부 연결성 검사 결과와 분리한다.
        /// </summary>
        private List<ClashData> fabricationNeighborClashList = new List<ClashData>();
        private HashSet<int> fabricationNeighborPartIndices = new HashSet<int>();
        private HashSet<int> fabricationTargetBodyIndices = new HashSet<int>();
        private HashSet<int> fabricationTargetPartIndices = new HashSet<int>();

        /// <summary>
        /// 제작도 연결 후보 광역 필터용 모델 Body 캐시. 모델을 다시 열 때 초기화한다.
        /// </summary>
        private struct BodyBoundsData
        {
            public float MinX;
            public float MinY;
            public float MinZ;
            public float MaxX;
            public float MaxY;
            public float MaxZ;
        }

        private Dictionary<int, BodyBoundsData> fabricationBodyBoundsCache =
            new Dictionary<int, BodyBoundsData>();
        private Dictionary<int, int> fabricationBodyToPartIndexCache = new Dictionary<int, int>();
        private int fabricationNeighborCacheSourceBodyCount = -1;

        /// <summary>
        /// Osnap 좌표 리스트
        /// </summary>
        private List<VIZCore3D.NET.Data.Vertex3D> osnapPoints = new List<VIZCore3D.NET.Data.Vertex3D>();

        /// <summary>
        /// Osnap 좌표와 부재 이름·축 리스트
        /// axis는 LINE osnap의 경우 시작→끝 벡터 최대 성분("X"/"Y"/"Z"), POINT/수동은 "" (REQ-003, 2026-05-11)
        /// </summary>
        private List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName, string axis)> osnapPointsWithNames = new List<(VIZCore3D.NET.Data.Vertex3D, string, string)>();

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
        /// 장시간 작업 중 표시하는 "처리 중..." 오버레이 패널 (T-018)
        /// ShowBusyOverlay/HideBusyOverlay로 제어
        /// </summary>
        private Panel busyOverlay = null;
        private Label busyOverlayMessage = null;
        private Button busyOverlayCancelButton = null;
        private string busyOverlayBaseMessage = "처리 중...";

        /// <summary>
        /// UI 스레드 장시간 작업의 협력적 취소 상태.
        /// SDK 단일 호출은 중단하지 않고 다음 체크포인트에서만 취소한다.
        /// </summary>
        private bool _cancelableOperationInProgress = false;
        private bool _cancelRequested = false;
        private string _lastCancellationCheckpoint = null;
        private bool _mainDimensionInProgress = false;

        /// <summary>
        /// T-032: CollectAllOsnap이 마지막으로 수집한 부재별 Osnap 맵.
        /// ComputeViewDimensionsForMembers 호출 시 재사용해 GetOsnapPoint 중복 호출 방지.
        /// 시트 선택 자동 경로(다른 부재 집합)에서는 null 대신 빈 맵을 전달받고 내부에서 재구축.
        /// </summary>
        private Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>> _lastCollectedNodeOsnapMap
            = new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D, string)>>();

        /// <summary>
        /// UDA 값 캐시 ((노드, 키) → 값). SPREF·ORIENTATION은 세션 내 불변인데,
        /// 조회 헬퍼(GetSprefValue/GetUdaValue)가 매번 UDA.Keys + 부모 10단계 트리 walk를 돌아 무거웠다.
        /// 가공도 미리보기는 한 부재당 이 walk를 5회(PAD·EA 판정 + ORIENTATION 3회) 반복 → 캐시로 1회로 축소.
        /// 도면 리스트 추출(모델/STRU 전환 지점) 시 osnap 맵과 함께 초기화. (2026-07-22 미리보기 지연 단축)
        /// </summary>
        private readonly Dictionary<(int nodeIndex, string key), string> _udaValueCache
            = new Dictionary<(int, string), string>();

        /// <summary>
        /// T-038+039 v4 (2026-05-12 사용자 사양): ShowAllDimensions가 계산한 *모델 이동량* (2D 캔버스 mm).
        /// "보조선이 나간 방향 반대쪽으로 그리드 안의 모델을 보조선 길이만큼 이동" — 화면 H/V 외곽 반대.
        /// RenderSheetViewForDrawing이 RescaleObject 후 `Drawing2D.Object2D.MoveObject(objId, dx, dy)` 호출에 사용.
        /// </summary>
        private float _lastModelShiftCanvasX = 0f;
        private float _lastModelShiftCanvasY = 0f;

        /// <summary>
        /// Step B2 (2026-05-19): 가공도 공통 코어 결과 보존.
        /// 옛 _mfgDrawingZ90Applied / _mfgDrawingR180Applied / _mfgDrawingCameraSnapshot 3 필드 통합.
        /// ExecuteMfgDrawing(수동)이 BuildMfgSceneCore 결과로 채움.
        /// </summary>
        private MfgViewPose _lastMfgViewPose = null;

        /// <summary>
        /// 직전 가공도 미리보기(ExecuteMfgDrawing)가 건 화면축 회전 총량(도).
        /// RotateCameraByScreenAxis는 상대(누적) 회전이라, 다음 미리보기 진입 때 이 값을 음수로 되돌려
        /// 클릭 간 카메라 누적 틀어짐을 차단한다 (2026-07-22 가공도 fit 수정).
        /// </summary>
        private float _mfgPreviewNetRoll = 0f;

        /// <summary>
        /// 현재 활성화한 가공도 부재 참조축 리뷰 ID.
        /// PDF는 뷰마다 Reset/Delete하고, 3D 미리보기는 다음 선택 또는 다른 시트 진입 때 정리한다.
        /// </summary>
        private int _mfgActiveReferenceAxisId = -1;

        /// <summary>
        /// 제작도 4면도 렌더 중 활성화한 시트 참조축 리뷰 ID.
        /// 뷰마다 Reset/Delete하며 조립도·설치도에는 만들지 않는다.
        /// </summary>
        private int _drawingActiveReferenceAxisId = -1;

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
            lvOsnap.SelectedIndexChanged += LvOsnap_SelectedIndexChanged;  // REQ-004 (2026-05-11)
            lvDimension.SelectedIndexChanged += LvDimension_SelectedIndexChanged;  // REQ-005 (2026-05-11)

            // #36/#48 STRU 이름 검색 입력창 (검색과 치수 추출 분리, 코드 생성)
            InitStruSearchUI();

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
        /// 3D 뷰어 중앙에 "처리 중..." 오버레이 표시 (T-018).
        /// 장시간 블로킹 작업 전 호출 → 반드시 finally에서 HideBusyOverlay() 호출.
        /// </summary>
        private void ShowBusyOverlay(string message = "처리 중...")
        {
            if (busyOverlay == null)
            {
                busyOverlay = new Panel();
                busyOverlay.BackColor = Color.FromArgb(45, 45, 48);
                busyOverlay.BorderStyle = BorderStyle.FixedSingle;
                busyOverlay.Size = new Size(360, 135);
                busyOverlay.Visible = false;

                busyOverlayMessage = new Label();
                busyOverlayMessage.AutoSize = false;
                busyOverlayMessage.TextAlign = ContentAlignment.MiddleCenter;
                busyOverlayMessage.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                busyOverlayMessage.ForeColor = Color.White;
                busyOverlayMessage.BackColor = Color.Transparent;
                busyOverlayMessage.Location = new Point(10, 8);
                busyOverlayMessage.Size = new Size(338, 72);
                busyOverlay.Controls.Add(busyOverlayMessage);

                busyOverlayCancelButton = new Button();
                busyOverlayCancelButton.Text = "취소";
                busyOverlayCancelButton.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                busyOverlayCancelButton.Size = new Size(160, 34);
                busyOverlayCancelButton.Location = new Point(99, 88);
                busyOverlayCancelButton.Visible = false;
                busyOverlayCancelButton.Click += BusyOverlayCancelButton_Click;
                busyOverlay.Controls.Add(busyOverlayCancelButton);

                panelViewer.Controls.Add(busyOverlay);
            }

            busyOverlayBaseMessage = message;
            UpdateBusyOverlayContents();
            busyOverlay.Location = new Point(
                Math.Max(0, (panelViewer.ClientSize.Width - busyOverlay.Width) / 2),
                Math.Max(0, (panelViewer.ClientSize.Height - busyOverlay.Height) / 2));
            busyOverlay.BringToFront();
            busyOverlay.Visible = true;
            Application.DoEvents(); // 즉시 화면 갱신
        }

        private void BusyOverlayCancelButton_Click(object sender, EventArgs e)
        {
            if (!_cancelableOperationInProgress || _cancelRequested)
                return;

            _cancelRequested = true;
            _lastCancellationCheckpoint = null;
            UpdateBusyOverlayContents();
            DiagLog("사용자가 취소를 요청함 — 현재 SDK 호출 완료 후 가장 가까운 안전 체크포인트에서 중단");
            Application.DoEvents();
        }

        private void UpdateBusyOverlayContents()
        {
            if (busyOverlayMessage == null || busyOverlayCancelButton == null)
                return;

            bool showCancel = _cancelableOperationInProgress;
            busyOverlay.Size = showCancel ? new Size(360, 135) : new Size(300, 80);
            busyOverlayMessage.Location = showCancel ? new Point(10, 8) : new Point(10, 4);
            busyOverlayMessage.Size = showCancel ? new Size(338, 72) : new Size(278, 70);
            busyOverlayCancelButton.Visible = _cancelableOperationInProgress;
            busyOverlayCancelButton.Enabled = _cancelableOperationInProgress && !_cancelRequested;
            busyOverlayCancelButton.Text = _cancelRequested ? "취소 요청됨" : "취소";
            busyOverlayMessage.Text = _cancelRequested
                ? $"{busyOverlayBaseMessage}\n현재 SDK 호출이 끝나는 즉시 안전하게 중단합니다."
                : busyOverlayBaseMessage;
        }

        /// <summary>
        /// 긴 관리 코드 루프에서 진행 문구를 갱신하고 UI 메시지 큐를 처리한다.
        /// SDK 단일 호출 중에는 개입하지 않고 호출 사이의 안전한 경계에서만 사용한다.
        /// </summary>
        private void ProcessCancelableUiCheckpoint(string message, string checkpoint)
        {
            if (!_cancelableOperationInProgress)
                return;

            if (!string.IsNullOrWhiteSpace(message))
            {
                busyOverlayBaseMessage = message;
                UpdateBusyOverlayContents();
            }

            Application.DoEvents();
            ThrowIfCancellationRequested(checkpoint);
        }

        private void BeginCancelableOperation()
        {
            _cancelRequested = false;
            _lastCancellationCheckpoint = null;
            _cancelableOperationInProgress = true;
        }

        private void EndCancelableOperation()
        {
            _cancelableOperationInProgress = false;
            _cancelRequested = false;
            _lastCancellationCheckpoint = null;
            UpdateBusyOverlayContents();
        }

        private bool IsCancellationRequested(string checkpoint)
        {
            if (!_cancelRequested)
                return false;

            if (!string.Equals(_lastCancellationCheckpoint, checkpoint, StringComparison.Ordinal))
            {
                _lastCancellationCheckpoint = checkpoint;
                DiagLog($"중간 취소 체크포인트 도달: {checkpoint}");
            }
            return true;
        }

        private void ThrowIfCancellationRequested(string checkpoint)
        {
            if (IsCancellationRequested(checkpoint))
                throw new OperationCanceledException(checkpoint);
        }

        /// <summary>
        /// 오버레이 해제 (T-018). ShowBusyOverlay와 쌍으로 사용.
        /// </summary>
        /// <summary>
        /// SDK가 띄우고 닫지 않은 진행창을 닫는다 (#116, 2026-08-04 사내 실기).
        ///
        /// 증상: 우리 흐름은 `결과 표시 완료 — 정상 종료`까지 찍히는데, 화면 가운데
        /// `Please Wait` / `Processing` 창만 계속 돈다. 그 문구는 우리 오버레이(한국어 `처리 중...`)가
        /// 아니라 `VIZCore3D.NET.Dialogs.ProgressDialog`의 것이다. 우리 코드는 이 창을 만들지 않는다 —
        /// SDK가 내부에서 열고 닫지 않은 것이다.
        ///
        /// 우리 로그가 끝까지 찍혔다는 건 이 창이 **모달이 아니라는** 뜻이다(모달이면 호출이 멈춰 있었을 것).
        /// 따라서 남은 창을 닫아도 진행 중인 작업을 끊지 않는다.
        /// </summary>
        private void CloseOrphanedSdkProgressDialog(string where)
        {
            try
            {
                // 닫으면서 순회하면 컬렉션이 바뀌므로 먼저 모은다.
                var stuck = new List<Form>();
                foreach (Form form in Application.OpenForms)
                {
                    if (form == null) continue;
                    string typeName = form.GetType().FullName ?? "";
                    if (typeName.StartsWith("VIZCore3D", StringComparison.OrdinalIgnoreCase)
                        && typeName.IndexOf("ProgressDialog", StringComparison.OrdinalIgnoreCase) >= 0)
                        stuck.Add(form);
                }

                foreach (Form form in stuck)
                {
                    DiagLog($"[진행창] SDK 진행창이 남아 있어 닫는다 ({where}): {form.GetType().FullName}");
                    try { form.Close(); } catch (Exception ex) { DiagLog($"[진행창] Close 실패: {ex.Message}"); }
                    try { form.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[진행창] 정리 실패({where}): {ex.Message}");
            }
        }

        private void HideBusyOverlay()
        {
            if (busyOverlay != null)
            {
                busyOverlay.Visible = false;
            }
        }
    }
}
