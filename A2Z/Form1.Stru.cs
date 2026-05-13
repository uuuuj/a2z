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
        // P1 범위: 추출 + CheckedListBox 표시 + 전체선택/해제 + 행 선택 시 3D 강조 + 카메라 fit.
        // P2/P3 범위(미구현): 도면 리스트 뽑기, STRU별 자동 도면 생성, 일괄 PDF, 간섭검사 격리.

        private List<VIZCore3D.NET.Data.Node> _struNodeCache = new List<VIZCore3D.NET.Data.Node>();

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
        /// CheckedListBox 행 "선택"(체크와 별개) 시 해당 STRU의 BODY 부재 강조 + 카메라 fit.
        /// 체크박스는 출력 대상 표시용, 선택은 시각적 강조 전용으로 의미 분리.
        /// Designer에서 CheckOnClick=true 설정 — 마우스 클릭 1회로 체크와 선택이 동시 발생 (사용자 의도).
        /// 즉 사용자가 STRU 행을 한 번 클릭하면 (1) 체크 토글 + (2) 3D 강조·fit이 동시 트리거됨.
        /// </summary>
        private void ClbStruList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (clbStruList == null) return;
            int selectedIdx = clbStruList.SelectedIndex;
            if (selectedIdx < 0 || selectedIdx >= _struNodeCache.Count) return;

            var struNode = _struNodeCache[selectedIdx];
            try
            {
                // 재귀 모든 후손 (Object3DChildOption.ALL_CHILDREN + includeBody:true)
                // SDK xml 검증: VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN (line 4877)
                // Manager.Object3DManager.GetChildObject3d(int, Object3DChildOption, bool) (line 50132)
                var allDescendants = vizcore3d.Object3D.GetChildObject3d(
                    struNode.Index,
                    VIZCore3D.NET.Data.Object3DChildOption.ALL_CHILDREN,
                    true);  // includeBody
                if (allDescendants == null || allDescendants.Count == 0)
                {
                    DiagLog($"T-064 ClbStru '{struNode.NodeName ?? struNode.NodePath}' allDescendants=0");
                    return;
                }

                // BODY만 필터 — Node.Kind == NodeKind.BODY (xml line 9711 P:Node.Kind, line 4583 NodeKind.BODY)
                var memberIndices = allDescendants
                    .Where(b => b.Kind == VIZCore3D.NET.Data.NodeKind.BODY)
                    .Select(b => b.Index)
                    .ToList();
                DiagLog($"T-064 ClbStru '{struNode.NodeName ?? struNode.NodePath}' allDescendants={allDescendants.Count}, BODY={memberIndices.Count}");
                if (memberIndices.Count == 0) return;

                // 배치 갱신 가드 + 색상 초기화 + 선택 + 카메라 fit
                // SDK 정정: Color.RestoreColorAll은 Object3D 네임스페이스, FlyToObject3d는 View 직속
                // try/finally로 EndUpdate 보장 — BeginUpdate 후 예외 발생 시에도 UI 잠금 해제
                vizcore3d.BeginUpdate();
                try
                {
                    vizcore3d.Object3D.Color.RestoreColorAll();
                    vizcore3d.Object3D.Select(memberIndices, true, false);
                    vizcore3d.View.FlyToObject3d(memberIndices, 1.2f);
                }
                finally
                {
                    vizcore3d.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 ClbStruList_SelectedIndexChanged ERROR: {ex.Message}");
            }
        }
    }
}
