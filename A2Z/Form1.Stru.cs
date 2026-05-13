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
        // 모델트리에서 LEAF_ASSEMBLY 중 NodeName이 "/"로 시작하는 노드를 STRU로 식별.
        // P1 범위: 추출 + CheckedListBox 표시 + 전체선택/해제 + 행 선택 시 3D 강조 + 카메라 fit
        // P2/P3 범위(미구현): 도면 리스트 뽑기, STRU별 자동 도면 생성, 일괄 PDF, 간섭검사 격리.

        private List<VIZCore3D.NET.Data.Node> _struNodeCache = new List<VIZCore3D.NET.Data.Node>();

        /// <summary>
        /// 모델트리에서 STRU 단위 추출.
        /// LEAF_ASSEMBLY 중 NodeName 또는 NodePath가 "/"로 시작하는 노드 반환.
        /// 필터 결과 0건이면 fallback으로 모든 LEAF_ASSEMBLY 반환 (사용자가 패턴 확인 가능).
        /// </summary>
        private List<VIZCore3D.NET.Data.Node> CollectStruList()
        {
            try
            {
                // FromFilter(LEAF_ASSEMBLY, includeNodePath:true) — NodePath 채워서 받기
                var leafAssemblies = vizcore3d.Object3D.FromFilter(
                    VIZCore3D.NET.Data.Object3dFilter.LEAF_ASSEMBLY, true);
                if (leafAssemblies == null || leafAssemblies.Count == 0)
                {
                    DiagLog($"T-064 CollectStruList: LEAF_ASSEMBLY 결과 0건");
                    return new List<VIZCore3D.NET.Data.Node>();
                }

                // 진단 — 상위 20건 NodeName/NodePath/Kind/Depth 출력
                int diagCount = Math.Min(20, leafAssemblies.Count);
                for (int i = 0; i < diagCount; i++)
                {
                    var n = leafAssemblies[i];
                    DiagLog($"T-064 LeafAssy[{i}]: idx={n.Index} name='{n.NodeName}' path='{n.NodePath}' kind={n.Kind} depth={n.Depth}");
                }
                if (leafAssemblies.Count > diagCount)
                    DiagLog($"T-064 ...(추가 {leafAssemblies.Count - diagCount}건 생략)");

                // 필터 — NodeName 또는 NodePath가 "/"로 시작
                var struList = leafAssemblies
                    .Where(n =>
                        (!string.IsNullOrEmpty(n.NodeName) && n.NodeName.StartsWith("/")) ||
                        (!string.IsNullOrEmpty(n.NodePath) && n.NodePath.StartsWith("/")))
                    .OrderBy(n => n.NodeName ?? n.NodePath ?? "")
                    .ToList();

                // 필터 결과 0건 fallback
                if (struList.Count == 0)
                {
                    DiagLog($"T-064 \"/\" 필터 결과 0건 — 모든 LEAF_ASSEMBLY({leafAssemblies.Count}건) fallback 표시");
                    struList = leafAssemblies.OrderBy(n => n.NodeName ?? "").ToList();
                }

                DiagLog($"T-064 CollectStruList: leafAssy={leafAssemblies.Count}, struList={struList.Count}");
                return struList;
            }
            catch (Exception ex)
            {
                DiagLog($"T-064 CollectStruList ERROR: {ex.Message}\n{ex.StackTrace}");
                return new List<VIZCore3D.NET.Data.Node>();
            }
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
                // STRU(LEAF_ASSEMBLY) 하위 BODY 부재 수집
                var bodies = vizcore3d.Object3D.GetChildObject3d(
                    struNode.Index, VIZCore3D.NET.Data.NodeFilterKind.BODY);
                if (bodies == null || bodies.Count == 0)
                {
                    DiagLog($"T-064 ClbStru selected '{struNode.NodeName ?? struNode.NodePath}' bodies=0");
                    return;
                }
                var memberIndices = bodies.Select(b => b.Index).ToList();

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
