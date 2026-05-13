using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        // ─── T-064 P1 + P2a: STRU 목록 + 도면 리스트 뽑기 PoC ───
        // STRU(Structure 단위) 식별 — 사용자 모델링 컨벤션 기반.
        // 모델트리: /E1(파일) → /E1(어셈블리) → /E1(어셈블리) → /M1(STRU) → FRMWORK 어셈블리들 → 부재.
        // 즉 STRU = 자식 중 NodeName이 "FRMWORK "로 시작하는 어셈블리가 있는 어셈블리.
        //
        // P1 범위 (구현됨):
        //   - 추출 + CheckedListBox 표시 + 전체선택/해제
        //   - 체크박스 클릭 → 3D 강조 토글 (다중 체크 강조 누적 유지, 카메라 fit 없음)
        //   - 행 선택 (이름 클릭) → 그 STRU로 카메라 fit (강조 변경 X, 체크 강조 유지)
        //
        // P2a 범위 (구현됨 — PoC):
        //   - [도면 리스트 뽑기] 버튼: 체크된 STRU 첫 번째 1개만 대상
        //   - 가시성 격리 (전체 BODY 숨김 → STRU 후손 BODY만 표시)
        //   - DetectClash 호출 (페어 직접 생성 — VisibleOnly=true)
        //   - 결과 DiagLog 출력 (clashList/시트 생성에는 반영 안 함)
        //   - 가시성 복원 (try/finally)
        //   - 기존 Clash_OnClashTestFinishedEvent 임시 해제 + P2a 전용 핸들러 등록 → 복원
        //
        // P2b/c 범위 (미구현): GenerateDrawingSheets 호출, 시트 채우기, 다중 STRU 루프, 확인 팝업, PDF 출력

        private List<VIZCore3D.NET.Data.Node> _struNodeCache = new List<VIZCore3D.NET.Data.Node>();

        // 가드 — 체크박스 클릭 시 WinForms가 SelectedIndexChanged도 발생시킴(MouseDown 순간).
        // ItemCheck에서 set, BeginInvoke로 큐 끝 해제. SelectedIndexChanged는 BeginInvoke 지연 후 검사 → 가드 on이면 fit 차단.
        private bool _suppressStruSelChanged = false;

        // ─── T-064 P2a 전용 필드 ───
        // P2a는 *기존* Clash_OnClashTestFinishedEvent (시트 생성까지 수행) 흐름을 *회피*해야 함.
        // 컨텍스트 분리: P2a는 PoC라 결과를 DiagLog만 — clashList/시트 생성에 반영 안 함 (사용자 메모리: 패턴 무비판 이식 금지).
        private VIZCore3D.NET.Data.Node _p2aClashStruNode = null;
        private DateTime _p2aClashStartTime;

        // 진행 가드 — P2a 실행 중 같은 버튼·간섭검사 버튼 재진입 차단 (위험 리뷰 #2/#7 대응).
        // true면 btnExtractDrawingList_Click 재진입 거부 + 사용자가 다른 흐름 트리거 시 UI 차단으로 격리 상태 유지.
        private bool _p2aInProgress = false;

        /// <summary>
        /// 모델트리에서 STRU 단위 추출 (T-064 STRU 일괄 도면).
        /// ASSEMBLY 전체 모수에서 룰 집합(union)으로 STRU 인덱스 추출.
        /// 현재 룰: RuleByFrameworkChildParent — FRMWORK 자식의 부모.
        /// 향후 룰 추가 가능 (UDA 마킹, depth, NameSlashPrefix 등 — 코드 주석 참고).
        /// 룰 매칭 0건이면 fallback으로 "/" 시작 + 공백 없는 어셈블리 표시 (디버그용 안전망).
        /// </summary>
        private List<VIZCore3D.NET.Data.Node> CollectStruList()
        {
            try
            {
                // FromFilter(ASSEMBLY, includeNodePath:true) — 모든 어셈블리 (Leaf 아님)
                var assemblies = vizcore3d.Object3D.FromFilter(
                    VIZCore3D.NET.Data.Object3dFilter.ASSEMBLY, true);
                if (assemblies == null || assemblies.Count == 0)
                {
                    DiagLog($"T-064 CollectStruList: ASSEMBLY 모수 0건");
                    return new List<VIZCore3D.NET.Data.Node>();
                }

                // 진단 — 어셈블리 상위 30건 NodeName/parentIdx/depth 출력
                int diagCount = Math.Min(30, assemblies.Count);
                for (int i = 0; i < diagCount; i++)
                {
                    var n = assemblies[i];
                    DiagLog($"T-064 Asm[{i}]: idx={n.Index} name='{n.NodeName}' parentIdx={n.ParentIndex} depth={n.Depth}");
                }
                if (assemblies.Count > diagCount)
                    DiagLog($"T-064 ...(추가 어셈블리 {assemblies.Count - diagCount}건 생략)");

                // STRU 식별 룰들 — union (HashSet으로 dedupe). 향후 룰 추가 가능.
                var struIndices = new HashSet<int>();
                foreach (var idx in RuleByFrameworkChildParent(assemblies))
                    struIndices.Add(idx);
                // 향후 추가 룰 예시 (현재 미구현):
                //   - RuleByUdaMarking: UDA에 "STRU"=true 마킹된 노드
                //   - RuleByDepthThreshold: 특정 깊이의 "/" 시작 어셈블리
                //   - RuleByNameSlashPrefix: NodeName이 "/" 시작이면서 후손 NodeName에 " /xxx" suffix 등장하는 패턴

                var struList = assemblies
                    .Where(n => struIndices.Contains(n.Index))
                    .OrderBy(n => n.NodeName ?? "")
                    .ToList();

                // Fallback: 룰 매칭 0건 → 디버그용 안전망 (NodeName "/" 시작 + 공백 없는 어셈블리)
                if (struList.Count == 0)
                {
                    DiagLog("T-064 모든 룰 매칭 0건 — fallback: NodeName \"/\" 시작 + 공백 없는 어셈블리 표시");
                    struList = assemblies
                        .Where(n =>
                            !string.IsNullOrEmpty(n.NodeName) &&
                            n.NodeName.StartsWith("/") &&
                            !n.NodeName.Contains(" "))
                        .OrderBy(n => n.NodeName)
                        .ToList();
                }

                DiagLog($"T-064 CollectStruList: assemblies={assemblies.Count}, struIndices={struIndices.Count}, struList={struList.Count}");
                return struList;
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 CollectStruList ERROR: {ex.Message}\n{ex.StackTrace}");
                return new List<VIZCore3D.NET.Data.Node>();
            }
        }

        /// <summary>
        /// STRU 식별 룰 1 — FRMWORK 자식의 부모 (T-064 STRU 일괄 도면).
        /// 사용자 모델링 컨벤션: STRU 바로 아래에 NodeName이 "FRMWORK "(대소문자 무시)로 시작하는
        /// 어셈블리 단위가 옴 (예: "FRMWORK 0 of STRUCTURE ..."). 그 부모 어셈블리가 STRU.
        /// 부모 트래버스 1단계만 사용 — 재귀 없음.
        /// </summary>
        private IEnumerable<int> RuleByFrameworkChildParent(List<VIZCore3D.NET.Data.Node> assemblies)
        {
            const string FRMWORK_PREFIX = "FRMWORK ";  // 뒤 공백 포함 — 단어 경계 표시
            int frameworkCount = 0;
            var parentIndices = new List<int>();
            foreach (var n in assemblies)
            {
                if (string.IsNullOrEmpty(n.NodeName)) continue;
                if (!n.NodeName.StartsWith(FRMWORK_PREFIX, StringComparison.OrdinalIgnoreCase)) continue;
                if (n.ParentIndex < 0) continue;
                frameworkCount++;
                parentIndices.Add(n.ParentIndex);
            }
            DiagLog($"T-064 RuleByFrameworkChildParent: FRMWORK 어셈블리={frameworkCount}건 → 부모 인덱스 yield");
            return parentIndices;
        }

        /// <summary>
        /// CheckedListBox(clbStruList)에 STRU 목록 채우기. 모델 로드 후 호출.
        /// 표시 우선순위: NodeName → NodePath → "(Index N)".
        /// </summary>
        public void PopulateStruCheckList()
        {
            if (clbStruList == null)
            {
                DiagLog($"T-064 PopulateStruCheckList: clbStruList == null (Designer 미적용?)");
                return;
            }
            clbStruList.Items.Clear();
            _struNodeCache = CollectStruList();
            foreach (var stru in _struNodeCache)
            {
                string display;
                if (!string.IsNullOrEmpty(stru.NodeName))
                    display = stru.NodeName;
                else if (!string.IsNullOrEmpty(stru.NodePath))
                    display = stru.NodePath;
                else
                    display = $"(Index {stru.Index})";
                clbStruList.Items.Add(display, false);
            }
            if (lblStruTitle != null)
                lblStruTitle.Text = $"STRU 목록 ({_struNodeCache.Count}개)";
            DiagLog($"T-064 PopulateStruCheckList: {_struNodeCache.Count}개 항목 추가됨");
        }

        /// <summary>
        /// "전체 선택/해제" 토글. 모두 체크되어 있으면 해제, 그 외는 모두 체크.
        /// </summary>
        private void btnSelectAllStru_Click(object sender, EventArgs e)
        {
            if (clbStruList == null || clbStruList.Items.Count == 0) return;
            bool allChecked = clbStruList.CheckedItems.Count == clbStruList.Items.Count;
            for (int i = 0; i < clbStruList.Items.Count; i++)
                clbStruList.SetItemChecked(i, !allChecked);
        }

        /// <summary>
        /// CheckedListBox 체크박스 클릭 시 호출 — 체크/해제에 따라 STRU의 BODY 부재를 3D에서 강조/해제.
        /// Designer에서 CheckOnClick=false 설정 — 체크박스 영역 클릭만 체크 토글 (이름 클릭은 선택만).
        /// 다중 체크 강조 유지: 매번 RestoreColorAll → 미래 체크된 STRU 전체의 BODY 합집합 → Select(true).
        /// 카메라 fit(FlyToObject3d) 호출 없음 — 사용자 요청 (체크 시 시점 변동 방지).
        /// ItemCheck는 체크 상태 변경 *직전*에 발생 — e.NewValue가 미래 상태이므로 CheckedIndices에 e.NewValue 반영해 합집합 계산.
        /// </summary>
        private void ClbStruList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // 가드 set — 같은 클릭으로 SelectedIndexChanged의 fit 차단
            _suppressStruSelChanged = true;
            try
            {
                ItemCheckCore(e);
            }
            finally
            {
                // BeginInvoke로 큐 끝에서 해제 — SelectedIndexChanged의 BeginInvoke 콜백 후 해제 보장
                if (this.IsHandleCreated)
                    this.BeginInvoke(new Action(() => _suppressStruSelChanged = false));
                else
                    _suppressStruSelChanged = false;
            }
        }

        private void ItemCheckCore(ItemCheckEventArgs e)
        {
            if (clbStruList == null) return;
            if (e.Index < 0 || e.Index >= _struNodeCache.Count) return;

            // ItemCheck는 체크 *직전* — e.NewValue로 미래 체크 set 계산
            var futureCheckedIdx = new HashSet<int>();
            foreach (int idx in clbStruList.CheckedIndices) futureCheckedIdx.Add(idx);
            if (e.NewValue == CheckState.Checked) futureCheckedIdx.Add(e.Index);
            else futureCheckedIdx.Remove(e.Index);

            try
            {
                // 미래 체크된 STRU들의 모든 후손 BODY 합집합
                var allBodyIndices = new HashSet<int>();
                foreach (int idx in futureCheckedIdx)
                {
                    if (idx < 0 || idx >= _struNodeCache.Count) continue;
                    var stru = _struNodeCache[idx];
                    var descendants = vizcore3d.Object3D.GetChildObject3d(
                        stru.Index,
                        VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN,
                        true);
                    if (descendants == null) continue;
                    foreach (var b in descendants)
                    {
                        if (b.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                            allBodyIndices.Add(b.Index);
                    }
                }

                DiagLog($"T-064 ItemCheck idx={e.Index} new={e.NewValue} futureCheckedSTRU={futureCheckedIdx.Count} totalBODY={allBodyIndices.Count}");

                // 배치 갱신 가드 + 전체 색 초기화 + 합집합 강조 (카메라 fit 없음)
                vizcore3d.BeginUpdate();
                try
                {
                    vizcore3d.Object3D.Color.RestoreColorAll();
                    if (allBodyIndices.Count > 0)
                        vizcore3d.Object3D.Select(allBodyIndices.ToList(), true, false);
                    // FlyToObject3d 의도적으로 호출 안 함 — 사용자 요청
                }
                finally
                {
                    vizcore3d.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 ClbStruList_ItemCheck ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// CheckedListBox 행 선택(이름 클릭) 시 카메라만 그 STRU로 fit. 강조(Select/Color)는 변경 안 함 — 체크 강조 유지.
        /// 체크박스 클릭 시 WinForms가 동일 행을 선택 상태로 만들면서 이 이벤트도 트리거함 →
        /// BeginInvoke로 한 메시지 사이클 지연 후 _suppressStruSelChanged 검사. ItemCheck가 가드를 set한 상태면 fit 차단.
        /// </summary>
        private void ClbStruList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 큐 지연 — ItemCheck가 같은 클릭으로 발생 중이면 가드가 set됨
            if (this.IsHandleCreated)
                this.BeginInvoke(new Action(PerformFlyToSelectedStru));
            else
                PerformFlyToSelectedStru();
        }

        private void PerformFlyToSelectedStru()
        {
            if (_suppressStruSelChanged) return;  // 체크박스 클릭으로 인한 SelectedIndexChanged면 fit 차단
            if (clbStruList == null) return;
            int selectedIdx = clbStruList.SelectedIndex;
            if (selectedIdx < 0 || selectedIdx >= _struNodeCache.Count) return;

            var struNode = _struNodeCache[selectedIdx];
            try
            {
                var descendants = vizcore3d.Object3D.GetChildObject3d(
                    struNode.Index,
                    VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN,
                    true);
                if (descendants == null || descendants.Count == 0)
                {
                    DiagLog($"T-064 ClbStru Select '{struNode.NodeName ?? struNode.NodePath}' descendants=0 (fit skip)");
                    return;
                }
                var memberIndices = descendants
                    .Where(b => b.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                    .Select(b => b.Index)
                    .ToList();
                if (memberIndices.Count == 0) return;

                // 카메라 fit만 — Select/RestoreColorAll 호출 없음 (체크 강조 보존)
                vizcore3d.View.FlyToObject3d(memberIndices, 1.2f);
                DiagLog($"T-064 ClbStru Select '{struNode.NodeName ?? struNode.NodePath}' fit BODY={memberIndices.Count}");
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 ClbStruList_SelectedIndexChanged ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// T-064 P2a — [도면 리스트 뽑기] 버튼 (PoC).
        /// 체크된 STRU 중 첫 번째 1개에 대해 가시성 격리 + 간섭검사 수행 + 결과 DiagLog.
        ///
        /// 흐름:
        ///   1) 가드: 모델 열림 확인, 체크된 STRU ≥ 1개 확인 (다중이면 첫 번째만 처리 — P2c에서 루프)
        ///   2) STRU 후손 BODY 인덱스 수집 + 전체 BODY 인덱스 수집
        ///   3) 가시성 격리: 전체 BODY 숨김 → STRU 후손 BODY만 표시
        ///   4) 기존 OnClashTestFinishedEvent 핸들러 임시 해제 → P2a 전용 핸들러 등록
        ///   5) STRU 후손 BODY끼리 페어 직접 생성 (VisibleOnly=true로 검사 격리 강화) + PerformInterferenceCheck
        ///   6) IsBusy 폴링 (최대 60초 타임아웃)
        ///   7) finally: 핸들러 원상 복원 + 가시성 복원
        ///
        /// 컨텍스트 분리 (사용자 메모리 - 패턴 무비판 이식 금지):
        ///   - 기존 DetectClash()를 재사용하지 않음 (VisibleOnly=false 고정이라 격리 의도와 충돌).
        ///   - 기존 Clash_OnClashTestFinishedEvent는 시트 생성까지 수행 — P2a PoC는 결과만 DiagLog.
        ///   - 따라서 페어를 P2a 내부에서 직접 만들고, 완료 핸들러도 P2a 전용으로 분리.
        /// </summary>
        private void btnExtractDrawingList_Click(object sender, EventArgs e)
        {
            // 진행 가드 — 위험 리뷰 #7 대응 (재진입 차단)
            if (_p2aInProgress)
            {
                DiagLog("T-064 P2a 이미 진행 중 — 재진입 무시");
                return;
            }
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("모델을 먼저 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (clbStruList == null || clbStruList.CheckedItems.Count == 0)
            {
                MessageBox.Show("처리할 STRU를 체크해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // P2a: 첫 번째 체크된 STRU만 처리 (다중은 P2c에서 루프)
            int firstCheckedListIdx = -1;
            for (int i = 0; i < clbStruList.Items.Count; i++)
            {
                if (clbStruList.GetItemChecked(i)) { firstCheckedListIdx = i; break; }
            }
            if (firstCheckedListIdx < 0 || firstCheckedListIdx >= _struNodeCache.Count)
            {
                DiagLog($"T-064 P2a 체크 STRU 인덱스 무효 idx={firstCheckedListIdx} cache={_struNodeCache.Count}");
                return;
            }
            if (clbStruList.CheckedItems.Count > 1)
            {
                DiagLog($"T-064 P2a 체크 {clbStruList.CheckedItems.Count}개 중 첫 번째만 처리 (P2a PoC, 다중은 P2c에서)");
            }

            var struNode = _struNodeCache[firstCheckedListIdx];

            // STRU 후손 전체 수집 (ALL_CHILDREN 재귀 — BODY만이 아니라 PART/ASSEMBLY 후손도 포함)
            // 사용자 보고 "가시성 격리 미작동" 대응: 부모 PART/ASSEMBLY가 안 숨겨지면 검사 대상 누락 가능
            var descendants = vizcore3d.Object3D.GetChildObject3d(
                struNode.Index,
                VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN,
                true);
            if (descendants == null || descendants.Count == 0)
            {
                DiagLog($"T-064 P2a STRU='{struNode.NodeName}' 후손 0건 (중단)");
                return;
            }
            // 표시 대상: STRU 본인 + 모든 후손 (BODY/PART/ASSEMBLY 다)
            var struVisibleIndices = new List<int> { struNode.Index };
            struVisibleIndices.AddRange(descendants.Select(n => n.Index));

            // ClashTest 페어용 — BODY만 추출
            var struBodyNodes = descendants
                .Where(b => b.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                .ToList();
            if (struBodyNodes.Count == 0)
            {
                DiagLog($"T-064 P2a STRU='{struNode.NodeName}' BODY 후손 0건 (중단)");
                return;
            }

            // 전체 노드 모수 — Object3dFilter.ALL (BODY/PART/ASSEMBLY 모두)
            // 다른 STRU·다른 모델 부재까지 다 숨겨야 격리 정확. 사용자 가설 "가시성 격리 미작동" 대응.
            var allNodes = vizcore3d.Object3D.FromFilter(
                VIZCore3D.NET.Data.Object3dFilter.ALL, false);
            var allNodeIndices = (allNodes != null)
                ? allNodes.Select(n => n.Index).ToList()
                : new List<int>();

            DiagLog($"T-064 P2a 시작 STRU='{struNode.NodeName}' struBODY={struBodyNodes.Count} struVisible={struVisibleIndices.Count} allNodes={allNodeIndices.Count}");

            // 진행 가드 set + UI 차단 (위험 리뷰 #2 대응: 사용자가 도중 모델 변경/다른 흐름 트리거 차단)
            // _p2aInProgress 가드는 Form1.Clash.cs:Clash_OnClashTestFinishedEvent 진입부에서 검사하여
            // 기존 흐름(시트 생성·사전조건 메시지)을 차단함. swap 대신 가드만 신뢰 (사용자 보고: swap 실패로 기존 흐름 호출됨).
            _p2aInProgress = true;
            btnExtractDrawingList.Enabled = false;
            ShowBusyOverlay($"STRU 격리·간섭검사 진행 중: {struNode.NodeName ?? "STRU"}");

            bool p2aHandlerRegistered = false;
            try
            {
                // 1) 가시성 격리 — 전체 노드(BODY/PART/ASSEMBLY) 숨김 후 STRU 본인+후손 표시
                //    BeginUpdate/EndUpdate 묶음 (응답성)
                vizcore3d.BeginUpdate();
                try
                {
                    if (allNodeIndices.Count > 0)
                        vizcore3d.Object3D.Show(allNodeIndices, false);
                    vizcore3d.Object3D.Show(struVisibleIndices, true);
                }
                finally
                {
                    vizcore3d.EndUpdate();
                }
                Application.DoEvents();

                // 2) P2a 결과 핸들러 등록 (기존 핸들러는 가드로 차단되므로 swap 불필요)
                vizcore3d.Clash.OnClashTestFinishedEvent += P2aClash_OnFinished;
                p2aHandlerRegistered = true;

                // 3) Clash 페어 생성 — STRU 후손 BODY끼리만, VisibleOnly=true로 검사 격리 강화
                _p2aClashStruNode = struNode;
                _p2aClashStartTime = DateTime.Now;

                vizcore3d.Clash.Clear();
                int pairCount = 0;
                for (int i = 0; i < struBodyNodes.Count; i++)
                {
                    for (int j = i + 1; j < struBodyNodes.Count; j++)
                    {
                        var pairClash = new VIZCore3D.NET.Data.ClashTest();
                        pairClash.Name = $"P2a_{struBodyNodes[i].NodeName}_vs_{struBodyNodes[j].NodeName}";
                        pairClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        pairClash.UseClearanceValue = true;
                        pairClash.ClearanceValue = 3.0f;  // T-063 기준 유지
                        pairClash.UseRangeValue = true;
                        pairClash.RangeValue = 3.0f;
                        pairClash.UsePenetrationTolerance = true;
                        pairClash.PenetrationTolerance = 1.0f;
                        pairClash.VisibleOnly = true;      // P2a 핵심 — 보이는 노드만 검사 (격리 강화)
                        pairClash.BottomLevel = 0;
                        pairClash.GroupA = new List<VIZCore3D.NET.Data.Node> { struBodyNodes[i] };
                        pairClash.GroupB = new List<VIZCore3D.NET.Data.Node> { struBodyNodes[j] };

                        if (vizcore3d.Clash.Add(pairClash))
                            pairCount++;
                    }
                }
                DiagLog($"T-064 P2a Clash 페어 {pairCount}개 등록 (VisibleOnly=true)");

                if (pairCount == 0)
                {
                    DiagLog($"T-064 P2a 페어 0개 — 검사 생략");
                    return;
                }

                // 4) 비동기 검사 시작
                bool startResult = vizcore3d.Clash.PerformInterferenceCheck();
                DiagLog($"T-064 P2a PerformInterferenceCheck startResult={startResult}");

                // 5) 완료 폴링 (최대 60초)
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (vizcore3d.Clash.IsBusy && sw.ElapsedMilliseconds < 60000)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                }
                if (vizcore3d.Clash.IsBusy)
                    DiagLog($"T-064 P2a TIMEOUT (60s) STRU='{struNode.NodeName}'");
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 P2a ERROR: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // P2a 핸들러 해제 (등록됐을 때만)
                if (p2aHandlerRegistered)
                {
                    try { vizcore3d.Clash.OnClashTestFinishedEvent -= P2aClash_OnFinished; } catch { }
                }
                // 가시성 복원 — 전체 노드 다시 표시 (BeginUpdate 묶음)
                try
                {
                    vizcore3d.BeginUpdate();
                    try
                    {
                        if (allNodeIndices.Count > 0)
                            vizcore3d.Object3D.Show(allNodeIndices, true);
                    }
                    finally
                    {
                        vizcore3d.EndUpdate();
                    }
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    DiagLog($"T-064 P2a 가시성 복원 ERROR: {ex.Message}");
                }
                _p2aClashStruNode = null;
                // 진행 가드 해제 + UI 차단 해제 (가드 해제는 핸들러 해제 후 — 가드 의존 흐름 안전)
                _p2aInProgress = false;
                try { btnExtractDrawingList.Enabled = true; } catch { }
                try { HideBusyOverlay(); } catch { }
                DiagLog($"T-064 P2a 종료");
            }
        }

        /// <summary>
        /// T-064 P2a — 간섭검사 완료 콜백 (PoC).
        /// 기존 Clash_OnClashTestFinishedEvent는 결과를 clashList → 시트 생성까지 처리하나,
        /// P2a는 PoC라 결과를 DiagLog만 출력. clashList/lvClash 등 기존 상태에는 영향 주지 않음.
        /// </summary>
        private void P2aClash_OnFinished(object sender, VIZCore3D.NET.Event.EventManager.ClashEventArgs e)
        {
            try
            {
                double elapsed = (DateTime.Now - _p2aClashStartTime).TotalSeconds;
                string struName = _p2aClashStruNode?.NodeName ?? "(null)";
                int testCount = vizcore3d.Clash.ClashTestCount;
                DiagLog($"T-064 P2a OnFinished STRU='{struName}' ID={e.ID} elapsed={elapsed:F2}s ClashTestCount={testCount}");

                int totalPairs = 0;
                for (int i = 0; i < testCount; i++)
                {
                    var clashTest = vizcore3d.Clash.Items[i];
                    if (clashTest == null) continue;

                    var results = vizcore3d.Clash.GetResultItem(
                        clashTest,
                        VIZCore3D.NET.Manager.ClashManager.ResultGroupingOptions.PART);
                    if (results == null || results.Count == 0) continue;

                    foreach (var r in results)
                    {
                        totalPairs++;
                        DiagLog($"T-064 P2a result[{totalPairs}] A_idx={r.NodeIndexA} A='{r.NodeNameA}' B_idx={r.NodeIndexB} B='{r.NodeNameB}'");
                        if (totalPairs >= 50)
                        {
                            DiagLog($"T-064 P2a result log 상한 50건 도달 — 이후 생략");
                            break;
                        }
                    }
                    if (totalPairs >= 50) break;
                }
                DiagLog($"T-064 P2a 결과 요약 STRU='{struName}' totalPairs={totalPairs}");
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 P2a OnFinished ERROR: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
