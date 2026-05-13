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
        // ─── T-064 P1: STRU 목록 + 행 강조 ───
        // STRU(Structure 단위) 식별 — 사용자 모델링 컨벤션 기반.
        // 모델트리: /E1(파일) → /E1(어셈블리) → /E1(어셈블리) → /M1(STRU) → FRMWORK 어셈블리들 → 부재.
        // 즉 STRU = 자식 중 NodeName이 "FRMWORK "로 시작하는 어셈블리가 있는 어셈블리.
        // P1 범위:
        //   - 추출 + CheckedListBox 표시 + 전체선택/해제
        //   - 체크박스 클릭 → 3D 강조 토글 (다중 체크 강조 누적 유지, 카메라 fit 없음)
        //   - 행 선택 (이름 클릭) → 그 STRU로 카메라 fit (강조 변경 X, 체크 강조 유지)
        // P2/P3 범위(미구현): 도면 리스트 뽑기, STRU별 자동 도면 생성, 일괄 PDF, 간섭검사 격리.

        private List<VIZCore3D.NET.Data.Node> _struNodeCache = new List<VIZCore3D.NET.Data.Node>();

        // 가드 — 체크박스 클릭 시 WinForms가 SelectedIndexChanged도 발생시킴(MouseDown 순간).
        // ItemCheck에서 set, BeginInvoke로 큐 끝 해제. SelectedIndexChanged는 BeginInvoke 지연 후 검사 → 가드 on이면 fit 차단.
        private bool _suppressStruSelChanged = false;

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
    }
}
