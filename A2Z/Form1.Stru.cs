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
        // ─── T-064 P1 + P2 본진: STRU 일괄 도면 출력 ───
        // STRU(Structure 단위) 식별 — 사용자 모델링 컨벤션 기반.
        // 모델트리: /E1(파일) → /E1(어셈블리) → /E1(어셈블리) → /M1(STRU) → FRMWORK 어셈블리들 → 부재.
        // 즉 STRU = 자식 중 NodeName이 "FRMWORK "로 시작하는 어셈블리가 있는 어셈블리.
        //
        // P1 범위 (구현됨):
        //   - 추출 + CheckedListBox 표시 + 전체선택/해제
        //   - 체크박스 클릭 → 3D 강조 토글 (다중 체크 강조 누적 유지, 카메라 fit 없음)
        //   - 행 선택 (이름 클릭) → 그 STRU로 카메라 fit (강조 변경 X, 체크 강조 유지)
        //
        // P2 본진 범위 (구현됨 — P2a PoC 폐기, 옵션 B 재설계):
        //   - [도면 일괄 출력] 버튼: 체크된 STRU 전체 순서대로 자동 반복
        //   - STRU별 흐름 = 사용자 평소 작업:
        //       (a) xraySelectedNodeIndices = STRU 후손 BODY → DetectClash() 호출
        //       (b) Clash_OnClashTestFinishedEvent 자동 콜백 → CompleteMainDimensionPostClash
        //           → GenerateDrawingSheets() → drawingSheetList + lvDrawingSheet 자동 채워짐
        //       (c) IsBusy 폴링으로 비동기 → 동기 흐름 모사
        //       (d) 옵션 B — lvDrawingSheet 행 자동 선택으로 LvDrawingSheet_SelectedIndexChanged 자동 트리거
        //           → 핸들러가 사용자 단서 "조립도/가공도 이름 클릭" 패턴을 그대로 자동화:
        //              가시성 격리·X-Ray 해제·SilhouetteEdge·카메라 fit·풍선·기준부재 하이라이트·
        //              시트 종류별 치수 추출 분기·BOM 수집을 모두 자동 처리.
        //           → 우리는 PDF 출력(Export2PDFBy2DView)과 시트 간 메모리 정리만 수행.
        //       (e) 시트 간·STRU 간 2D 메모리 정리 + GC
        //
        // 폐기된 P2a PoC 흐름:
        //   - 가시성 격리 (Show false/true) — 부모/자식 가시성 충돌로 무용
        //   - 페어 직접 생성 + DetectClash 우회 — 본진은 기존 DetectClash가 xraySelectedNodeIndices로 격리
        //   - P2aClash_OnFinished — 시트 생성 자동 흐름 활용으로 별도 핸들러 불필요
        //   - GenerateSheetDrawing2D / GenerateMfgDrawing2DAll 직접 호출 — 옵션 B 핸들러 자동 트리거로 대체

        private List<VIZCore3D.NET.Data.Node> _struNodeCache = new List<VIZCore3D.NET.Data.Node>();

        // #36/#48 STRU 이름 검색 입력창 (코드 생성, Designer 미사용).
        private System.Windows.Forms.TextBox txtStruSearch;

        // 가드 — 체크박스 클릭 시 WinForms가 SelectedIndexChanged도 발생시킴(MouseDown 순간).
        // ItemCheck에서 set, BeginInvoke로 큐 끝 해제. SelectedIndexChanged는 BeginInvoke 지연 후 검사 → 가드 on이면 fit 차단.
        private bool _suppressStruSelChanged = false;

        // 진행 가드 — STRU 일괄 도면 실행 중 같은 버튼 재진입 차단.
        // 이름은 P2a PoC 유산이지만 본진에서도 동일 역할 유지.
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
            var acSource = new System.Windows.Forms.AutoCompleteStringCollection();  // #36 검색 자동완성 소스
            foreach (var stru in _struNodeCache)
            {
                string display = GetStruDisplayName(stru);
                clbStruList.Items.Add(display, false);
                acSource.Add(display);
            }
            if (txtStruSearch != null)                       // #36 STRU 검색창 자동완성 갱신
                txtStruSearch.AutoCompleteCustomSource = acSource;
            if (lblStruTitle != null)
                lblStruTitle.Text = $"STRU 목록 ({_struNodeCache.Count}개)";
            DiagLog($"T-064 PopulateStruCheckList: {_struNodeCache.Count}개 항목 추가됨");
        }

        /// <summary>
        /// STRU 표시 이름 — NodeName → NodePath → "(Index N)". 목록 표시·검색 매칭 공통 사용.
        /// </summary>
        private string GetStruDisplayName(VIZCore3D.NET.Data.Node stru)
        {
            if (stru == null) return "";
            if (!string.IsNullOrEmpty(stru.NodeName)) return stru.NodeName;
            if (!string.IsNullOrEmpty(stru.NodePath)) return stru.NodePath;
            return $"(Index {stru.Index})";
        }

        /// <summary>
        /// #36 STRU 이름 검색 입력창을 코드로 생성해 groupBoxStru 하단에 붙인다.
        /// (Designer 손수정 회피 — 생성자에서 1회 호출.) 자동완성 소스는 PopulateStruCheckList에서 갱신.
        /// </summary>
        private void InitStruSearchUI()
        {
            if (groupBoxStru == null) return;

            var panelStruSearch = new System.Windows.Forms.Panel
            {
                Name = "panelStruSearch",
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 34
            };

            var lblStruSearch = new System.Windows.Forms.Label
            {
                Name = "lblStruSearch",
                AutoSize = true,
                Text = "STRU 검색",
                Location = new System.Drawing.Point(8, 9)
            };

            txtStruSearch = new System.Windows.Forms.TextBox
            {
                Name = "txtStruSearch",
                Location = new System.Drawing.Point(70, 6),
                Size = new System.Drawing.Size(258, 23),
                Anchor = System.Windows.Forms.AnchorStyles.Top
                       | System.Windows.Forms.AnchorStyles.Left
                       | System.Windows.Forms.AnchorStyles.Right,
                AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource
            };
            var btnStruSearch = new System.Windows.Forms.Button
            {
                Name = "btnStruSearch",
                Text = "검색",
                Location = new System.Drawing.Point(334, 5),
                Size = new System.Drawing.Size(95, 25),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnStruSearch.Click += BtnStruSearch_Click;

            panelStruSearch.Controls.Add(lblStruSearch);
            panelStruSearch.Controls.Add(txtStruSearch);
            panelStruSearch.Controls.Add(btnStruSearch);

            groupBoxStru.Controls.Add(panelStruSearch);
            // Dock 계층: Fill(clbStruList)이 맨 뒤여야 Top/Bottom 패널이 가장자리를 차지.
            panelStruSearch.BringToFront();
            if (clbStruList != null) clbStruList.SendToBack();
        }

        private void BtnStruSearch_Click(object sender, EventArgs e)
        {
            SearchStruByName(txtStruSearch != null ? txtStruSearch.Text : null);
        }

        /// <summary>
        /// #36/#48 STRU 이름으로 찾아 그 STRU만 격리하고 목록 선택·카메라 fit까지 수행한다.
        /// 격리 방식은 ProcessSingleStruFull(전체 BODY 숨김 → STRU BODY만 표시)과 동일 —
        /// 이후 공용 치수 추출을 별도로 실행해도 검색된 STRU만 대상이 되도록 격리를 유지한다.
        /// </summary>
        private void SearchStruByName(string name)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 파일을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string query = (name ?? "").Trim();
            if (query.Length == 0)
            {
                MessageBox.Show("검색할 STRU 이름을 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_struNodeCache == null || _struNodeCache.Count == 0)
            {
                MessageBox.Show("STRU 목록이 비어 있습니다. 모델을 먼저 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idx = FindStruIndexByName(query);
            if (idx < 0)
            {
                MessageBox.Show($"'{query}' 에 해당하는 STRU를 찾을 수 없습니다.", "검색 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var struNode = _struNodeCache[idx];

            // STRU 후손 BODY 수집
            var descendants = vizcore3d.Object3D.GetChildObject3d(
                struNode.Index, VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN, true);
            var memberIndices = descendants == null ? new List<int>()
                : descendants.Where(b => b.Kind == VIZCore3D.NET.Data.NodeKind.BODY).Select(b => b.Index).ToList();
            if (memberIndices.Count == 0)
            {
                MessageBox.Show($"'{GetStruDisplayName(struNode)}' STRU에 부재(BODY)가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 가시성 격리 — 전체 BODY 숨김 후 STRU BODY만 표시 (ProcessSingleStruFull 패턴)
            var allBodies = vizcore3d.Object3D.FromFilter(VIZCore3D.NET.Data.Object3dFilter.ALL_INCLUDE_BODY, false);
            var allBodyIndices = allBodies == null ? new List<int>()
                : allBodies.Where(n => n.Kind == VIZCore3D.NET.Data.NodeKind.BODY).Select(n => n.Index).ToList();
            vizcore3d.BeginUpdate();
            try
            {
                if (allBodyIndices.Count > 0) vizcore3d.Object3D.Show(allBodyIndices, false);
                vizcore3d.Object3D.Show(memberIndices, true);
            }
            finally { vizcore3d.EndUpdate(); }

            // 목록에서도 해당 STRU를 선택한다. 같은 항목을 재검색하면 SelectedIndexChanged가
            // 발생하지 않으므로 그 경우에만 직접 show+fit을 호출한다.
            if (clbStruList != null && idx < clbStruList.Items.Count)
            {
                bool selectionChanged = clbStruList.SelectedIndex != idx;
                clbStruList.SelectedIndex = idx;
                if (!selectionChanged)
                    PerformFlyToSelectedStru();
            }
            Application.DoEvents();

            DiagLog($"#48 STRU 검색 '{query}' → '{struNode.NodeName}' idx={idx} " +
                    $"bodies={memberIndices.Count} 격리·선택 완료");
        }

        /// <summary>
        /// 검색어로 _struNodeCache에서 STRU 인덱스를 찾는다. 완전일치(대소문자·앞뒤공백 무시) 우선, 없으면 부분일치 첫 매칭.
        /// </summary>
        private int FindStruIndexByName(string query)
        {
            if (_struNodeCache == null) return -1;
            // 1) 완전일치
            for (int i = 0; i < _struNodeCache.Count; i++)
                if (string.Equals(GetStruDisplayName(_struNodeCache[i]).Trim(), query, StringComparison.OrdinalIgnoreCase))
                    return i;
            // 2) 부분일치 (첫 매칭)
            for (int i = 0; i < _struNodeCache.Count; i++)
                if (GetStruDisplayName(_struNodeCache[i]).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            return -1;
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

                // STRU 부재 활성화 (2026-05-19, 사용자 사양: "최소한 그 부재는 활성화돼야 한다")
                //   - 다른 부재 가시성은 손대지 않음 (사용자: "활성화/비활성화 처리는 무거워 보임" 우려 반영 = Show 호출 최소화)
                //   - Show API 자체는 가벼운 SDK 호출 (visible 플래그만 설정)
                //   - 체크 강조(색상) 보존은 RestoreColorAll/Select 미호출로 그대로 유지
                vizcore3d.Object3D.Show(memberIndices, true);

                // 카메라 fit
                vizcore3d.View.FlyToObject3d(memberIndices, 1.2f);
                DiagLog($"T-064 ClbStru Select '{struNode.NodeName ?? struNode.NodePath}' show+fit BODY={memberIndices.Count}");
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 ClbStruList_SelectedIndexChanged ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// T-064 P2 본진 — [도면 일괄 출력] 버튼.
        /// 체크된 STRU 전체를 순서대로 자동 반복:
        ///   각 STRU에 대해 사용자 평소 작업(부재 선택 → 간섭검사 → 자동 시트 생성 → PDF) 수행.
        /// xraySelectedNodeIndices 격리로 STRU 후손 BODY만 검사 (기존 DetectClash 그대로 활용).
        /// 가시성 토글 없음 — Show false/true는 부모/자식 가시성 충돌 위험.
        ///
        /// 흐름:
        ///   1) 가드: 모델 열림 + 체크된 STRU ≥ 1개 + 재진입 차단
        ///   2) CheckedIndices 순서(리스트 표시 순서)대로 STRU 노드 수집
        ///   3) PDF 저장 폴더 선택 (FolderBrowserDialog)
        ///   4) 다중 STRU면 확인 팝업 (사용자 요구사항)
        ///   5) STRU별 ProcessSingleStruFull 호출 — 실패해도 다음 STRU 진행
        ///   6) STRU 간 2D 메모리 정리 + GC
        ///   7) finally: 진행 가드 해제 + UI 복원 + 결과 요약
        /// </summary>
        private void btnExtractDrawingList_Click(object sender, EventArgs e)
        {
            // 재진입 가드
            if (_p2aInProgress)
            {
                DiagLog("T-064 P2 이미 진행 중 — 재진입 무시");
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

            // 체크된 STRU 순서대로 수집 (CheckedIndices = 리스트 표시 순서)
            var checkedStrus = new List<VIZCore3D.NET.Data.Node>();
            foreach (int idx in clbStruList.CheckedIndices)
            {
                if (idx >= 0 && idx < _struNodeCache.Count)
                    checkedStrus.Add(_struNodeCache[idx]);
            }
            if (checkedStrus.Count == 0) return;

            // T-064 (2026-05-14) 사용자 사양: PDF 저장 폴더 고정 (Release/Debug 빌드 출력 폴더 하위 Drawings/)
            //   Application.StartupPath = 실행 중인 exe 위치 (bin\Debug 또는 bin\Release)
            //   FolderBrowserDialog 제거 — 매번 묻지 않고 자동 저장
            string saveDir = Path.Combine(Application.StartupPath, "Drawings");
            if (!Directory.Exists(saveDir))
            {
                try { Directory.CreateDirectory(saveDir); }
                catch (Exception ex)
                {
                    DiagLog($"T-064 PDF 저장 폴더 생성 실패: {saveDir} — {ex.Message}");
                    MessageBox.Show($"PDF 저장 폴더를 만들지 못했습니다.\n\n{saveDir}\n\n{ex.Message}",
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            DiagLog($"T-064 PDF 저장 폴더 (자동): {saveDir}");

            // 다중 STRU 확인 팝업 (사용자 요구사항 6번)
            if (checkedStrus.Count > 1)
            {
                var ret = MessageBox.Show(
                    $"선택된 {checkedStrus.Count}개 STRU의 도면 4종(제작/조립/설치/가공) PDF를 일괄 생성합니다.\n계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ret != DialogResult.Yes) return;
            }

            _p2aInProgress = true;
            BeginCancelableOperation();
            btnExtractDrawingList.Enabled = false;
            if (btnMainDimension != null)
                btnMainDimension.Enabled = false;
            ShowBusyOverlay("도면 일괄 출력 준비 중...");

            // ─── 자동 출력 진입 사전 초기화 (2026-05-19, 사용자 제안) ───
            //   증상: 수동 작업 잔재(drawingSheetList 시트)가 있으면 폴링 조건
            //         (Count > beforeSheetCount)이 영원히 미만족 → 60초 타임아웃.
            //   해결: 자동 출력 = "처음부터 다시" 의도 명확히. 모든 상태 깨끗한 시작.
            try
            {
                vizcore3d.BeginUpdate();
                try
                {
                    // (1) 전체 모델 표시 — STRU 격리 전 깨끗한 상태 보장
                    List<VIZCore3D.NET.Data.Node> allBodies =
                        vizcore3d.Object3D.GetPartialNode(false, false, true);
                    if (allBodies != null && allBodies.Count > 0)
                        vizcore3d.Object3D.Show(allBodies.Select(n => n.Index).ToList(), true);

                    // (2) 2D 캔버스 잔재 제거
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView(); } catch { }

                    // (3) 3D 풍선·치수·보조선 잔재 제거
                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
                }
                finally { vizcore3d.EndUpdate(); }

                // (4) 시트 잔재 — 60초 에러 직접 차단
                if (drawingSheetList != null) drawingSheetList.Clear();
                if (lvDrawingSheet != null) lvDrawingSheet.Items.Clear();

                // (5) 치수 잔재
                if (chainDimensionList != null) chainDimensionList.Clear();
                if (lvDimension != null) lvDimension.Items.Clear();

                // (6) BOM 정보 표 잔재
                if (lvDrawingBOMInfo != null) lvDrawingBOMInfo.Items.Clear();

                // (7) X-Ray 선택 잔재
                if (xraySelectedNodeIndices != null) xraySelectedNodeIndices.Clear();

                // (8) Osnap 캐시 — STRU 전환 시 부재 다르므로 fresh 시작 (E1 fallback이 보충하지만 명시 초기화)
                if (_lastCollectedNodeOsnapMap != null) _lastCollectedNodeOsnapMap.Clear();
                if (_udaValueCache != null) _udaValueCache.Clear();   // SPREF/ORIENTATION 캐시도 STRU 전환 시 초기화 (2026-07-22)

                DiagLog("자동 출력 진입 사전 초기화 완료 (모델 전체 표시 + 2D/3D 잔재 제거)");
            }
            catch (Exception ex)
            {
                DiagLog($"자동 출력 사전 초기화 ERROR: {ex.Message}");
                // 초기화 실패해도 본진 시도 — 안전망
            }

            // ─── P1: 엑셀 템플릿 init 1회 (검증 게이트 — 출력 결과 불변, no-op) ───
            // Set2DViewTemplateMark: 로고 매핑 1회
            // (2026-05-18) GenerateEdgeData 호출은 GenerateSheetDrawing2D 진입부로 이동
            //   사유: 수동(btnGenerateSheet2D_Click) 경로에도 동일 사전 조건 필요 — DRY + 단일 지점 보장
            try
            {
                string solutionPath = GetSolutionPath();
                string logoPath = System.IO.Path.Combine(solutionPath, "assets", "Logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    vizcore3d.Drawing2D.Template.Set2DViewTemplateMark(logoPath, logoPath);
                    DiagLog($"T-064 P1 엑셀 템플릿 init 완료 — logo={logoPath}");
                }
                else
                {
                    DiagLog($"T-064 P1 logo 파일 없음 — {logoPath} (Set2DViewTemplateMark 건너뜀)");
                }
            }
            catch (Exception initEx)
            {
                DiagLog($"T-064 P1 엑셀 템플릿 init 실패: {initEx.Message} (P2 진입 시 영향 가능)");
            }

            int successCount = 0, failCount = 0;
            var errors = new List<string>();
            int totalPdfCount = 0;
            bool cancelled = false;

            try
            {
                if (IsCancellationRequested("일괄 출력 준비 후"))
                    cancelled = true;

                for (int s = 0; s < checkedStrus.Count; s++)
                {
                    if (cancelled || IsCancellationRequested($"STRU {s + 1} 시작 전"))
                    {
                        cancelled = true;
                        break;
                    }

                    var stru = checkedStrus[s];
                    ShowBusyOverlay($"STRU 처리 {s + 1}/{checkedStrus.Count}: {stru.NodeName}");
                    Application.DoEvents();

                    try
                    {
                        int pdfCount = ProcessSingleStruFull(
                            stru,
                            saveDir,
                            savedCount => totalPdfCount += savedCount);
                        successCount++;
                        DiagLog($"T-064 STRU '{stru.NodeName}' 완료 — PDF {pdfCount}개 생성");
                    }
                    catch (OperationCanceledException ex)
                    {
                        cancelled = true;
                        DiagLog($"STRU '{stru.NodeName}' 중간 취소 — checkpoint={ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"{stru.NodeName}: {ex.Message}");
                        DiagLog($"T-064 STRU '{stru.NodeName}' ERROR: {ex.Message}\n{ex.StackTrace}");
                    }

                    // STRU 간 메모리 정리
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView(); } catch { }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);

                    if (cancelled || IsCancellationRequested($"STRU {s + 1} 처리 후"))
                    {
                        cancelled = true;
                        break;
                    }
                }
            }
            finally
            {
                if (cancelled || _cancelRequested)
                {
                    cancelled = true;
                    ClearCanceledOperationArtifacts();
                }

                // 모든 BODY 다시 표시 — 가시성 복원 (마지막 STRU 처리 후 사용자 일반 사용 흐름 복귀)
                try
                {
                    var allBodies = vizcore3d.Object3D.FromFilter(
                        VIZCore3D.NET.Data.Object3dFilter.ALL_INCLUDE_BODY, false);
                    if (allBodies != null)
                    {
                        var allBodyIndices = allBodies
                            .Where(n => n.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                            .Select(n => n.Index)
                            .ToList();
                        if (allBodyIndices.Count > 0)
                        {
                            vizcore3d.BeginUpdate();
                            try { vizcore3d.Object3D.Show(allBodyIndices, true); }
                            finally { vizcore3d.EndUpdate(); }
                            Application.DoEvents();
                            DiagLog($"T-064 최종 가시성 복원 — allBody={allBodyIndices.Count}");
                        }
                    }
                }
                catch (Exception ex) { DiagLog($"T-064 최종 가시성 복원 ERROR: {ex.Message}"); }

                // _p2aInProgress reset 지연 — race 방지 (사용자 보고: 결과 메시지박스 후 "자동 처리 완료" 팝업)
                // 시나리오: 마지막 STRU의 OnFinished 콜백이 *finally 진입 후*에 늦게 도착 가능.
                //   → reset이 콜백 도착 *전*이면 가드 통과 → "자동 처리 완료" 메시지박스 표시됨.
                //   → 500ms 추가 대기 + DoEvents로 큐의 콜백 처리 후 reset.
                System.Threading.Thread.Sleep(500);
                Application.DoEvents();

                _p2aInProgress = false;
                try { btnExtractDrawingList.Enabled = true; } catch { }
                try
                {
                    if (btnMainDimension != null)
                        btnMainDimension.Enabled = true;
                }
                catch { }
                try { HideBusyOverlay(); } catch { }
                EndCancelableOperation();
            }

            // 결과 요약
            int completedCount = successCount + failCount;
            int remainingCount = Math.Max(0, checkedStrus.Count - completedCount);
            string msg = cancelled
                ? $"STRU 일괄 도면 출력을 취소했습니다.\n\n" +
                  $"처리 완료: {completedCount}/{checkedStrus.Count}개 STRU\n" +
                  $"성공: {successCount}개\n실패: {failCount}개\n미처리: {remainingCount}개\n" +
                  $"생성된 PDF: {totalPdfCount}개\n\n현재 실행 중이던 작업 단위까지 마무리한 뒤 중단했습니다."
                : $"STRU 일괄 도면 출력 완료\n\n성공: {successCount}개 STRU (PDF {totalPdfCount}개)\n실패: {failCount}개";
            if (errors.Count > 0)
            {
                int maxShow = Math.Min(10, errors.Count);
                msg += $"\n\n실패 목록 (최대 10건):\n{string.Join("\n", errors.Take(maxShow))}";
                if (errors.Count > maxShow) msg += $"\n... +{errors.Count - maxShow}건";
            }
            MessageBox.Show(msg, cancelled ? "취소됨" : "완료", MessageBoxButtons.OK,
                cancelled || failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        /// <summary>
        /// T-064 P2 본진 — 단일 STRU 처리 (옵션 B 재설계).
        /// 부재 선택 → DetectClash 비동기 → 자동 시트 생성 → 시트별 PDF 출력.
        /// 사용자 평소 작업(부재 선택 → 간섭검사 → 자동 시트 + PDF) 흐름을 STRU 단위로 자동 반복.
        /// xraySelectedNodeIndices 격리로 STRU 후손 BODY만 검사 (가시성 토글 불필요).
        ///
        /// 옵션 B — lvDrawingSheet 행 자동 선택으로 LvDrawingSheet_SelectedIndexChanged 핸들러 자동 트리거.
        /// 사용자 단서 "현재 도면목록에서 조립도나 가공도 이름을 누르면 그 조립도/가공도 부재만 나오게 되어 있잖아"
        /// = 시트 행 클릭 시 핸들러가 가시성 격리·X-Ray·SilhouetteEdge·카메라 fit·풍선 정리·기준부재 하이라이트·
        ///   시트 종류별 치수 추출(가공도=ExecuteMfgDrawing / 설치도=ExtractInstallationDimensions / 일반=ComputeViewDimensionsForMembers)·
        ///   BOM 자동 수집을 *모두 자동 처리*. 우리는 PDF 출력만 수행.
        ///
        /// drawingSheetList 대신 lvDrawingSheet.Items를 순회 — UI 동기 보장 (GenerateDrawingSheets가 둘 다 갱신).
        /// </summary>
        private int ProcessSingleStruFull(
            VIZCore3D.NET.Data.Node struNode,
            string saveDir,
            Action<int> reportPdfSaved = null)
        {
            ThrowIfCancellationRequested("STRU 부재 수집 전");

            // 1) STRU 후손 BODY 수집
            var descendants = vizcore3d.Object3D.GetChildObject3d(
                struNode.Index,
                VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN,
                true);
            if (descendants == null || descendants.Count == 0)
                throw new Exception("STRU 후손 0건");
            var memberIndices = descendants
                .Where(b => b.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                .Select(b => b.Index)
                .ToList();
            if (memberIndices.Count == 0)
                throw new Exception("STRU 후손 BODY 0건");
            ThrowIfCancellationRequested("STRU 부재 수집 후");

            DiagLog($"T-064 STRU '{struNode.NodeName}' bodies={memberIndices.Count}");

            // ★ 옵션 A+B 결합: STRU 시작 시 가시성 격리
            // 사용자 평소 작업 시 다른 부재를 *숨김* 상태에서 치수추출 → CollectBOMData가 보이는 부재만 수집 → 연결성 정상.
            // 자동화에서도 동일 패턴 — 다른 STRU·다른 모델 부재를 숨김 → DetectClash → OnFinished에서 bomList에 STRU 부재만 → 정상 시트 생성.
            // 모수: Object3dFilter.ALL_INCLUDE_BODY + Kind==BODY 필터 (부모 PART/ASSEMBLY 안 건드림 — 시도 2의 부모/자식 충돌 회피).
            // VisibleOnly=false 기본 (기존 DetectClash 유지) — SDK가 가시성 재설정 안 함.
            var allBodies = vizcore3d.Object3D.FromFilter(
                VIZCore3D.NET.Data.Object3dFilter.ALL_INCLUDE_BODY, false);
            var allBodyIndices = (allBodies != null)
                ? allBodies.Where(n => n.Kind == VIZCore3D.NET.Data.NodeKind.BODY).Select(n => n.Index).ToList()
                : new List<int>();

            if (allBodyIndices.Count > 0)
            {
                vizcore3d.BeginUpdate();
                try
                {
                    vizcore3d.Object3D.Show(allBodyIndices, false);  // 전체 BODY 숨김
                    vizcore3d.Object3D.Show(memberIndices, true);     // STRU BODY만 표시
                }
                finally
                {
                    vizcore3d.EndUpdate();
                }
                Application.DoEvents();
                DiagLog($"T-064 STRU '{struNode.NodeName}' 가시성 격리 — allBody={allBodyIndices.Count}, STRU={memberIndices.Count}");
            }
            ThrowIfCancellationRequested("STRU 가시성 격리 후");

            // 2) xraySelectedNodeIndices 초기화 — 사용자 평소 btnMainDimension_Click(Form1.BOM.cs:347) 패턴 그대로.
            // 격리는 *가시성*으로만 수행 (CollectBOMData가 보이는 부재만 수집). xraySelectedNodeIndices 의존은
            // SDK 상태와 race 가능 (사용자 보고: 첫 클릭 T-023, 재클릭 시 일부만 동작 비결정 동작).
            xraySelectedNodeIndices.Clear();

            // ★ CollectBOMData 호출 — bomList를 STRU 부재만으로 갱신
            // (Form1.BOM.cs:345 주석: "xraySelectedNodeIndices가 CollectBOMData / DetectClash에서 필터로 쓰이며")
            // Clash_OnClashTestFinishedEvent의 IsSingleConnectedComponent(Form1.Clash.cs:506)가 bomList 기반
            // 연결성 그래프 검사를 수행하므로, bomList가 전체 모델 BOM이면 STRU 부재가 다른 부재들과 분리된
            // 그룹으로 카운트되어 컴포넌트 > 1 → T-023 메시지 + return → 시트 생성 안 됨.
            // 가시성 격리 + xraySelectedNodeIndices 설정 후 CollectBOMData 호출 → bomList에 STRU 부재만 →
            // 연결성 검사 시 STRU 부재끼리만 그래프 → 정상.
            ShowBusyOverlay($"BOM 수집 중: {struNode.NodeName}");
            ThrowIfCancellationRequested("STRU BOM 수집 전");
            bool bomCollected = CollectBOMData();
            DiagLog($"T-064 STRU '{struNode.NodeName}' CollectBOMData success={bomCollected}, bomList={bomList?.Count ?? 0}");
            ThrowIfCancellationRequested("STRU BOM 수집 후");

            // 3) DetectClash 호출 (비동기) — OnFinished가 자동으로 시트 생성·치수계산까지 진행
            ShowBusyOverlay($"간섭검사 실행 중: {struNode.NodeName}");
            ThrowIfCancellationRequested("STRU 간섭검사 시작 전");
            bool startResult = DetectClash(includeOutsideNeighbors: true);
            DiagLog($"T-064 STRU '{struNode.NodeName}' DetectClash startResult={startResult}");
            if (!startResult)
            {
                ThrowIfCancellationRequested("STRU 간섭검사 호출 후");

                // Clash 페어 0개 등 시작 실패 — 단일 부재 또는 모든 부재가 서로 멀어진 경우.
                // 그래도 GenerateDrawingSheets 직접 호출 시도 (Sheet 1 + 가공도라도 생성)
                ShowBusyOverlay($"도면 시트 생성 중: {struNode.NodeName}");
                GenerateDrawingSheets();
                ThrowIfCancellationRequested("STRU 도면 시트 생성 후");
            }
            else
            {
                // 4) 비동기 완료 폴링 — race 방지 패턴 (사용자 보고: 즉시 "시트 0건" + 뒤에서 자동 처리 완료)
                //   ① PerformInterferenceCheck() 반환 직후 IsBusy가 *아직 false* 가능 (SDK 비동기 시작 지연)
                //   ② IsBusy=false면 폴링 즉시 종료 → drawingSheetList 비어있음 → throw → race
                //   ③ 그 후 OnFinished 콜백 늦게 도착 → CompleteMainDimensionPostClash 실행 → "자동 처리 완료" 메시지박스
                //
                // 해결:
                //   - 폴링 진입 *전* 300ms sleep — SDK 비동기 스레드 시작 보장
                //   - drawingSheetList.Count > 0 종료 조건 (절대) — 진입부에서 Clear됐으므로 안전
                //   - 폴링 종료 *후* 추가 300ms sleep — OnFinished 후속 처리 완료 대기
                // (2026-05-19) 진입부 초기화로 잔재 0 보장 → `Count > beforeSheetCount` → `Count > 0` 단순화
                System.Threading.Thread.Sleep(300);  // SDK 비동기 시작 보장
                Application.DoEvents();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 60000)
                {
                    Application.DoEvents();
                    bool cancellationPending = IsCancellationRequested("STRU 간섭검사 완료 대기");
                    if (cancellationPending)
                        ShowBusyOverlay($"간섭검사 마무리 후 취소: {struNode.NodeName}");

                    System.Threading.Thread.Sleep(50);
                    if (cancellationPending && !vizcore3d.Clash.IsBusy)
                    {
                        System.Threading.Thread.Sleep(100);
                        Application.DoEvents();
                        throw new OperationCanceledException("STRU 간섭검사 완료 후");
                    }

                    // IsBusy=false AND 시트 1개 이상 채워졌으면 OnFinished 완료된 것
                    if (!vizcore3d.Clash.IsBusy &&
                        drawingSheetList != null &&
                        drawingSheetList.Count > 0)
                    {
                        System.Threading.Thread.Sleep(300);  // OnFinished 후속 작업 (시트 lvDrawingSheet 동기 등) 완료 대기
                        Application.DoEvents();
                        break;
                    }
                }
                if (sw.ElapsedMilliseconds >= 60000)
                {
                    DiagLog($"T-064 STRU '{struNode.NodeName}' TIMEOUT (60s) — sheets={drawingSheetList?.Count ?? 0}");
                    throw new Exception("간섭검사 60초 타임아웃");
                }
            }
            ThrowIfCancellationRequested("STRU 시트 출력 시작 전");

            // 5) drawingSheetList 채워졌는지 확인
            if (drawingSheetList == null || drawingSheetList.Count == 0)
                throw new Exception("시트 0건 — GenerateDrawingSheets 결과 비어있음");

            DiagLog($"T-064 STRU '{struNode.NodeName}' 시트 {drawingSheetList.Count}개 생성");

            // 6) STRU 폴더 생성
            string safeStruName = SanitizeFileName(struNode.NodeName ?? "STRU");
            string struSubDir = Path.Combine(saveDir, safeStruName);
            try { Directory.CreateDirectory(struSubDir); }
            catch (Exception ex) { DiagLog($"T-064 STRU '{struNode.NodeName}' 폴더 생성 실패: {ex.Message}"); }

            DiagLog($"T-064 STRU '{struNode.NodeName}' PDF 출력 시작 → {struSubDir}");

            string timeStamp = DateTime.Now.ToString("HHmmss");
            int pdfCount = 0;

            // ─── 7) 일반 시트 루프 (제작도/조립도/설치도) — 시트별 처리 ───
            // 사용자 평소 흐름: 시트 클릭 → "2D 출력" 버튼 → "PDF 출력" 버튼
            //   = lvi.Selected=true (핸들러 자동) → GenerateSheetDrawing2D(sheet) → Export2PDFBy2DView(file)
            for (int i = 0; i < lvDrawingSheet.Items.Count; i++)
            {
                ThrowIfCancellationRequested($"일반 시트 {i + 1} 시작 전");

                var lvi = lvDrawingSheet.Items[i];
                var sheet = lvi.Tag as DrawingSheetData;
                if (sheet == null || sheet.MemberIndices.Count == 0) continue;

                string sheetLabel = lvi.Text;
                if (sheetLabel.StartsWith("가공도")) continue;  // 가공도는 8단계에서 묶음 처리

                try
                {
                    ShowBusyOverlay(
                        $"PDF 출력 {i + 1}/{lvDrawingSheet.Items.Count}: {sheetLabel}");

                    // Step A (2026-05-19): UI 트릭(lvi.Selected=true + DoEvents + Sleep) 제거 →
                    //   ApplySheetSelection(sheet) 직접 호출. 시트당 200ms 단축 + 이벤트 타이밍 의존 제거.
                    //   UI 선택 표시는 시각 일관성 위해 유지 (사용자가 진행 중 어떤 시트 처리되는지 확인 가능)
                    foreach (ListViewItem sel in lvDrawingSheet.SelectedItems) sel.Selected = false;
                    lvi.Selected = true;
                    lvi.EnsureVisible();

                    // = LvDrawingSheet_SelectedIndexChanged 본체 (이벤트 시뮬 X, 메서드 직접 호출)
                    ApplySheetSelection(sheet);
                    ThrowIfCancellationRequested($"일반 시트 {i + 1} 선택 후");

                    // = btnGenerateSheet2D_Click 흐름 ("2D 출력" 버튼)
                    GenerateSheetDrawing2D(sheet);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                    ThrowIfCancellationRequested($"일반 시트 {i + 1} 2D 생성 후");

                    // = btnExportSheet2DPDF_Click 흐름 ("PDF 출력" 버튼) — SaveFileDialog 우회
                    string safeBaseName = SanitizeFileName(sheet.BaseMemberName ?? "Unknown");
                    string safeSheetLabel = SanitizeFileName(sheetLabel);
                    string pdfFile = $"{safeBaseName}_{safeSheetLabel}_{timeStamp}.pdf";
                    string pdfPath = Path.Combine(struSubDir, pdfFile);
                    vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                    vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                    vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                    DiagLog($"T-064 PDF saved: {pdfPath}");
                    pdfCount++;
                    reportPdfSaved?.Invoke(1);
                    ThrowIfCancellationRequested($"일반 시트 {i + 1} PDF 저장 후");

                    // 메모리 정리
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(); } catch { }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    DiagLog($"T-064 시트 #{sheet.SheetNumber} ({sheetLabel}) ERROR: {ex.Message}");
                }
            }

            ThrowIfCancellationRequested("일반 시트 출력 후");

            // ─── 8) 가공도 묶음 처리 — 검증된 수동 함수 재사용 (2026-07-23, #35) ───
            //   옛 P1 hard skip 제거. 수동 경로(btnMfgDrawingSheet_Click)와 동일한 GenerateMfgDrawingManual로
            //   실제 생성·저장. 저장 위치는 STRU 폴더(struSubDir), 결과 SuccessPdfs를 pdfCount에 합산.
            //   ⚠ 여러 STRU 연속 생성이라 가공도 크래시(#3) 핵심 검증 시나리오 — 재현 시 #3에 반영.
            var mfgSheets = new List<DrawingSheetData>();
            foreach (ListViewItem lvi in lvDrawingSheet.Items)
                if (lvi.Text.StartsWith("가공도"))
                {
                    var s = lvi.Tag as DrawingSheetData;
                    if (s != null && s.MemberIndices.Count > 0) mfgSheets.Add(s);
                }

            if (mfgSheets.Count > 0)
            {
                try
                {
                    ShowBusyOverlay($"가공도 PDF 출력 중: {struNode.NodeName}");
                    ThrowIfCancellationRequested("가공도 출력 시작 전");
                    var mfgResult = GenerateMfgDrawingManual(
                        mfgSheets,
                        struSubDir,
                        struNode.NodeName,
                        struNode.Index,
                        () => _cancelRequested);
                    pdfCount += mfgResult.SuccessPdfs;
                    reportPdfSaved?.Invoke(mfgResult.SuccessPdfs);
                    ThrowIfCancellationRequested("가공도 출력 후");
                    DiagLog($"T-064 STRU '{struNode.NodeName}' 가공도 {mfgResult.SuccessPdfs}개 저장" +
                        (mfgResult.TemplateMissing ? " (템플릿 누락)" : "") +
                        (mfgResult.Warnings.Count > 0 ? $" 경고 {mfgResult.Warnings.Count}건" : ""));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    DiagLog($"T-064 STRU '{struNode.NodeName}' 가공도 묶음 ERROR: {ex.Message}");
                }
                finally
                {
                    // 다음 STRU 전 메모리 정리 (2D 상태는 GenerateMfgDrawingManual finally가 이미 복원)
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            ThrowIfCancellationRequested("STRU 출력 완료 후");
            DiagLog($"T-064 STRU '{struNode.NodeName}' PDF 출력 완료 — {pdfCount}개 저장");
            return pdfCount;
        }

        /// <summary>
        /// 시트 종류 라벨 — BaseMemberIndex로 식별.
        ///   -1 = 제작도 (Sheet 1)
        ///   -2 = 설치도
        ///   -3 = 가공도
        ///    >= 0 = 조립도 (Sheet 2~N, 1-hop Clash 이웃)
        /// </summary>
        private string GetSheetKindLabel(DrawingSheetData sheet)
        {
            if (sheet.BaseMemberIndex == -1) return "제작도";
            if (sheet.BaseMemberIndex == -2) return "설치도";
            if (sheet.BaseMemberIndex == -3) return "가공도";
            return "조립도";  // BaseMemberIndex >= 0 (Sheet 2~N, 1-hop Clash 이웃)
        }
    }
}
