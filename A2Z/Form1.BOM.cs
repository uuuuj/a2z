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
            ResetFabricationNeighborSearchCache();

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
            // 라이선스 초기화 + 자동 갱신 타이머 (Form1.License.cs로 분리 — T-017)
            if (!InitializeLicense()) return;

            vizcore3d.ToolbarDrawing2D.Visible = true;
            //vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

            // 시작 시 모델트리 표시
            vizcore3d.ModelTreeVisible = true;

            // T-050: 3D View 좌측하단에 X/Y/Z 축 표시기(Marine Axis triad) 표시
            // 회사 doc 긴급하 1: "도면상에서는 축을 확인가능하나 3D View 창에서는 확인 불가" 해소
            vizcore3d.View.MarineAxis.Visible = true;

            // VIZCore3D 초기화 완료 후 간섭검사 이벤트 등록
            vizcore3d.Clash.OnClashTestFinishedEvent += Clash_OnClashTestFinishedEvent;

            // 3D 객체 선택 이벤트 등록 (부재 정보 탭용)
            vizcore3d.Object3D.OnObject3DSelected += Object3D_OnObject3DSelected;

            // 모서리(Edge) 데이터 생성 및 읽기 활성화 (파일 열기 전 설정 필요)
            vizcore3d.Model.GenerateEdgeData = true;
            vizcore3d.Model.LoadEdgeData = true;
            // 이미 로드된 객체의 엣지 데이터도 생성 (라인 없는 개체 대응)
            vizcore3d.Object3D.GenerateEdgeData();

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

                // SDK 권장 패턴: 같은 파일 재선택 시 중복 Open 방지를 위해 먼저 Close
                // (VIZCore3D.NET.xml 예제 L47297, L60261 참조)
                if (vizcore3d.Model.IsOpen())
                    vizcore3d.Model.Close();

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

                    // T-064 P1: STRU 목록 채우기
                    PopulateStruCheckList();
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
        /// 초기화 버튼 - 현재 로드된 파일을 재로드하여 모든 작업 상태(치수/풍선/시트/수동 조정 등)를 리셋
        /// </summary>
        private void btnResetToInitial_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath) || !vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 파일을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var answer = MessageBox.Show(
                "모든 작업 내용(BOM/치수/Clash/Osnap/도면 시트/풍선 조정)이 초기화되고\n" +
                "현재 파일이 처음 열었을 때 상태로 다시 로드됩니다.\n\n계속하시겠습니까?",
                "초기화",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            ResetToInitialState();
        }

        /// <summary>
        /// 현재 로드된 파일을 재로드하여 초기 상태로 복원한다.
        /// btnOpen_Click의 초기화 블록과 동일하되 balloonOverrides까지 포함.
        /// </summary>
        private void ResetToInitialState()
        {
            string path = currentFilePath;

            try
            {
                // 누적 상태 전면 초기화
                bomList.Clear();
                clashList.Clear();
                osnapPoints.Clear();
                osnapPointsWithNames.Clear();
                chainDimensionList.Clear();
                xraySelectedNodeIndices.Clear();
                drawingSheetList.Clear();
                bodyToPartNameMap.Clear();
                balloonOverrides.Clear();
                _autoProcessOsnapSuccess = false;
                lvBOM.Items.Clear();
                lvClash.Items.Clear();
                lvDrawingSheet.Items.Clear();
                lvOsnap.Items.Clear();
                lvDimension.Items.Clear();
                lvDrawingBOMInfo.Items.Clear();    // T-009: 도면정보 탭 BOM 목록
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();
                vizcore3d.Review.Note.Clear();
                // T-009: 은선 점선(DASH_LINE) 해제 — 글로벌 뷰/가공도/시트 생성 시 자동 적용되어 남음
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);

                // SDK 권장 패턴: 같은 경로를 다시 Open하기 전 기존 모델을 먼저 Close
                // (VIZCore3D.NET.xml 예제 L47297, L60261 참조)
                if (vizcore3d.Model.IsOpen())
                    vizcore3d.Model.Close();

                // 동일 파일 재로드
                bool result = vizcore3d.Model.Open(path);

                if (result)
                {
                    vizcore3d.View.FitToView();
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;
                    BuildBodyToPartNameMap();
                    // T-009 후속: Model.Open이 2D 뷰를 자동 복원하므로 Open 성공 후에 정리해야 효과 있음
                    Clear2DView();
                }
                else
                {
                    MessageBox.Show("파일 재로드에 실패했습니다. 파일을 다시 열어주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 예외 발생:\n\n{ex.Message}", "예외 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 메인 치수 추출 버튼 - Clash 검사 → Osnap 수집 → 치수 추출 → 치수 표시
        /// </summary>
        private void btnMainDimension_Click(object sender, EventArgs e)
        {
            // [T-016 진단 로그] 진입 시 상태
            DiagLog($"btnMainDimension ENTER " +
                $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0} " +
                $"osnap={osnapPointsWithNames?.Count ?? 0} bom={bomList?.Count ?? 0}");

            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 파일을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // T-026: "치수추출 버튼은 항상 현재 visible 기준" — 이전 시트 선택으로 남은
            // xraySelectedNodeIndices가 CollectBOMData / DetectClash에서 필터로 쓰이며
            // "이전 부재 1개" 결과가 반복되는 버그 방지.
            xraySelectedNodeIndices.Clear();

            // T-018: 장시간 작업 진행 오버레이 (BOM 수집 → Clash → 이벤트에서 Osnap/치수/시트)
            ShowBusyOverlay("BOM 수집 중...");

            try
            {
                // 0. BOM 데이터 수집 (매번 재수집하여 현재 가시성 반영)
                CollectBOMData();
                if (bomList.Count == 0)
                {
                    MessageBox.Show("BOM 데이터를 수집할 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    HideBusyOverlay();
                    return;
                }

                // 1. Clash 검사 먼저 수행 — 연결성 판정(T-023 v3)을 Clash 결과 기반으로 하려면
                //    Osnap/치수보다 Clash가 선행되어야 함. 치수 생성은 판정 통과 후에만.
                ShowBusyOverlay("간섭검사 실행 중...");
                _autoProcessOsnapSuccess = false;
                bool clashStarted = DetectClash(includeOutsideNeighbors: true);

                if (!clashStarted)
                {
                    // T-024: 단일 부재(쌍 0개) 또는 SDK 예외 → Clash 이벤트 미발동.
                    // 단일 부재는 "연결 성분 1개"로 간주하고 나머지 파이프라인 직접 수행.
                    CompleteMainDimensionPostClash(isSingleMember: true, clashTestCount: 0);
                }
                // clashStarted == true → Clash_OnClashTestFinishedEvent가 이어서
                //   clashList 수집 → 연결성 판정 → 통과 시 CompleteMainDimensionPostClash 호출
                //   오버레이는 그 경로에서 해제됨

                // [T-016 진단 로그] 진입 종료 (실제 파이프라인은 이벤트에서 이어짐)
                DiagLog($"btnMainDimension EXIT OK " +
                    $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0} " +
                    $"osnap={osnapPointsWithNames?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                // [T-016 진단 로그] 예외 종료
                DiagLog($"btnMainDimension EXIT FAIL " +
                    $"{ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"치수 추출 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                HideBusyOverlay();
            }
        }

        /// <summary>
        /// Clash 판정 이후(정상 경로는 Clash_OnClashTestFinishedEvent에서, 단일 부재는 btnMainDimension_Click에서)
        /// 공통적으로 수행할 Osnap 수집 → 체인 치수 계산 → 요약 MessageBox → 시트 생성.
        /// 오버레이 해제는 finally에서 보장.
        /// </summary>
        private void CompleteMainDimensionPostClash(bool isSingleMember, int clashTestCount)
        {
            try
            {
                // 1. Osnap 수집
                ShowBusyOverlay("Osnap 수집 중...");
                bool osnapSuccess = CollectAllOsnap();
                _autoProcessOsnapSuccess = osnapSuccess;

                // 2. 치수 추출 (T-028: 2D 출력 엔진과 동일 경로 / T-032: 성능 측정 + Osnap 맵 재사용)
                if (osnapSuccess && osnapPointsWithNames.Count > 0)
                {
                    ShowBusyOverlay("치수 계산 중...");
                    float tolerance = 0.5f;

                    // visible 부재 대상으로 공용 엔진 호출 — 3뷰(X/Y/Z) × 2축 = 6조합 치수 계산 + 중복 제거
                    List<int> visibleMembers = new List<int>();
                    foreach (var bom in bomList)
                    {
                        var real = vizcore3d.Object3D.FromIndex(bom.Index);
                        if (real != null && real.Visible)
                            visibleMembers.Add(bom.Index);
                    }

                    // T-032: CollectAllOsnap이 채운 _lastCollectedNodeOsnapMap을 전달해
                    //         ComputeViewDimensionsForMembers 내부 GetOsnapPoint 중복 호출 제거
                    var swCompute = System.Diagnostics.Stopwatch.StartNew();
                    chainDimensionList.Clear();
                    chainDimensionList.AddRange(
                        ComputeViewDimensionsForMembers(visibleMembers, null, tolerance, _lastCollectedNodeOsnapMap));
                    swCompute.Stop();

                    DiagLog($"T-032 치수 계산: visibleMembers={visibleMembers.Count} " +
                        $"osnapMapNodes={_lastCollectedNodeOsnapMap.Count} " +
                        $"chain={chainDimensionList.Count} " +
                        $"ComputeViewDimensionsForMembers={swCompute.ElapsedMilliseconds}ms");

                    // ListView에 추가 및 치수 번호 설정
                    lvDimension.Items.Clear();
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

                    // T-029: 치수추출 버튼은 chainDimensionList만 계산하고 3D 뷰 렌더링은 하지 않음.
                    // 실제 3D 뷰 치수 표시는 글로벌 X/Y/Z 뷰 버튼(ShowAllDimensions(viewDir))에서 수행.
                    // 이전 렌더 잔존 제거 (Review.Measure + ShapeDrawing) — "치수 없는 깨끗한 상태"로 마감.
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
                }

                // 3. 요약 MessageBox
                string clashLine;
                if (isSingleMember)
                    clashLine = "Clash: 검사 대상 부재가 1개 이하 (간섭검사 건너뜀)";
                else if (clashList.Count > 0)
                    clashLine = $"Clash: {clashList.Count}개 검출 (검사 쌍: {clashTestCount}개)";
                else
                    clashLine = $"Clash: 간섭 없음 (검사 쌍: {clashTestCount}개)";

                string summary = $"모델 로드 및 자동 처리 완료!\n\n" +
                    $"BOM: {bomList.Count}개\n" +
                    $"Osnap: {osnapPointsWithNames.Count}개\n" +
                    $"치수: {chainDimensionList.Count}개\n" +
                    clashLine;
                if (!_autoProcessOsnapSuccess)
                    summary += "\n\n* Osnap 수집 실패";

                // T-033: 순서 재배치 — 시트 생성 먼저, 그 뒤 오버레이 해제, 마지막 MessageBox.
                // 기존 순서(MessageBox → 시트 → finally 해제)는 "팝업 뜰 때 오버레이 잔존 + 팝업 닫힌 후 오버레이 2초 더"라는 사용자 체감 문제를 유발.

                // 3. 도면 시트 생성 (오버레이 유지, 내부에서 Sheet 1 BOM 자동 수집 — T-025)
                GenerateDrawingSheets();

                // 4. 오버레이 해제 (MessageBox 전에)
                HideBusyOverlay();

                // 5. 요약 MessageBox (오버레이 없이)
                // T-064 P2 본진 진행 중엔 메시지박스 차단 — 매 STRU마다 사용자가 OK 눌러야 하는 부담 회피.
                // P2 본진 끝나면 btnExtractDrawingList_Click에서 통합 결과 메시지박스 표시.
                if (!_p2aInProgress)
                    MessageBox.Show(summary, "자동 처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DiagLog($"CompleteMainDimensionPostClash EXIT OK " +
                    $"chain={chainDimensionList.Count} osnap={osnapPointsWithNames.Count} " +
                    $"clash={clashList.Count} sheets={drawingSheetList.Count} singleMember={isSingleMember}");
            }
            catch (Exception ex)
            {
                DiagLog($"CompleteMainDimensionPostClash FAIL {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"치수 추출 후속 처리 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideBusyOverlay();
            }
        }

        // AutoProcessModel → Clash 이벤트 간 상태 전달용 필드

        /// <summary>
        /// 전체 Osnap 수집 (내부 메서드).
        /// T-032: 같이 `_lastCollectedNodeOsnapMap`(부재별 Osnap 맵)도 채워 후속 `ComputeViewDimensionsForMembers` 호출 시 GetOsnapPoint 중복 호출 제거.
        /// </summary>
        private bool CollectAllOsnap()
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();
            lvOsnap.Items.Clear();
            _lastCollectedNodeOsnapMap.Clear();
            _mfgAxisDetectionCache.Clear();
            _udaValueCache.Clear();   // SPREF/ORIENTATION 캐시도 재수집 시 초기화 (2026-07-22)

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
                    bodyNodes = allBodyNodes.Where(n =>
                        selectedSet.Contains(n.Index) ||
                        (bodyToPartIndexMap.ContainsKey(n.Index) && selectedSet.Contains(bodyToPartIndexMap[n.Index]))
                    ).ToList();
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
                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList = CacheMfgAxisDetection(
                        node.Index,
                        vizcore3d.Object3D.GetOsnapPoint(node.Index));

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        // T-032: 부재별 Osnap 맵에도 추가 (ComputeViewDimensionsForMembers에서 재사용)
                        var nodeOsnapPts = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();

                        foreach (var osnap in osnapList)
                        {
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    // REQ-003: 축 추정 = start→end 최대 성분 (osnapPointsWithNames만 axis 포함)
                                    if (osnap.Start != null && osnap.End != null)
                                    {
                                        string lineAxis = EstimateOsnapLineAxis(osnap.Start, osnap.End);
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, partName, lineAxis));
                                        nodeOsnapPts.Add((startVertex, partName));
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, partName, lineAxis));
                                        nodeOsnapPts.Add((endVertex, partName));
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
                                        osnapPointsWithNames.Add((pointVertex, partName, ""));
                                        nodeOsnapPts.Add((pointVertex, partName));
                                    }
                                    break;
                            }
                        }

                        if (nodeOsnapPts.Count > 0)
                            _lastCollectedNodeOsnapMap[node.Index] = nodeOsnapPts;
                    }
                }

                // ListView에 추가
                if (osnapPoints.Count > 0)
                {
                    for (int i = 0; i < osnapPointsWithNames.Count; i++)
                    {
                        var item = osnapPointsWithNames[i];
                        // REQ-003: 컬럼 순서 No / 축 / 부재이름 / X / Y / Z
                        ListViewItem lvi = new ListViewItem((i + 1).ToString());
                        lvi.SubItems.Add(item.axis);
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

                // X-Ray 필터링: 프로그래밍 선택 → 수동 X-Ray → 전체
                List<VIZCore3D.NET.Data.Node> targetNodes;
                if (xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    targetNodes = allNodes.Where(n =>
                        selectedSet.Contains(n.Index) ||
                        (bodyToPartIndexMap.ContainsKey(n.Index) && selectedSet.Contains(bodyToPartIndexMap[n.Index]))
                    ).ToList();
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
            // 홀/슬롯홀 검출 = SDK GetNodeHoleInfo API (2026-06-23, 원기둥·Osnap 추측 휴리스틱 전면 교체).
            //   기존 휴리스틱(부정확)을 제거하고 API로 bom.Holes/SlotHoles를 채운다.
            //   BOM 표 홀사이즈/슬롯사이즈 문자열도 API 결과로 생성. (가공도 풍선도 같은 API 사용)
            try
            {
                foreach (var bom in bomList)
                {
                    bom.Holes.Clear();
                    bom.SlotHoles.Clear();
                    bom.HoleSize = "";
                    bom.SlotHoleSize = "";

                    GetMfgHolesFromApi(bom.Index, out var apiHoles, out var apiSlots);
                    if (apiHoles != null && apiHoles.Count > 0) bom.Holes = apiHoles;
                    if (apiSlots != null && apiSlots.Count > 0) bom.SlotHoles = apiSlots;

                    // 홀사이즈 문자열 (직경별 그룹 -> ØD x N)
                    if (bom.Holes.Count > 0)
                    {
                        var uniqueDiameters = bom.Holes
                            .Select(h => Math.Round(h.Diameter, 1)).Distinct().OrderBy(d => d).ToList();
                        var holeParts = new List<string>();
                        foreach (var diam in uniqueDiameters)
                        {
                            int count = bom.Holes.Count(h => Math.Abs(Math.Round(h.Diameter, 1) - diam) < 0.1);
                            holeParts.Add(count > 1 ? $"Ø{diam:F1}x{count}" : $"Ø{diam:F1}");
                        }
                        bom.HoleSize = string.Join(", ", holeParts);
                    }

                    // 슬롯사이즈 문자열 (스펙별 그룹 -> (W*L*D) x N)
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
                            slotParts.Add(grp.Count() > 1 ? $"{spec}*{grp.Count()}" : spec);
                        }
                        bom.SlotHoleSize = string.Join(", ", slotParts);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"홀 검출(API) 오류: {ex.Message}");
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

    }
}
