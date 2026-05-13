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
        //   - [도면 리스트 뽑기] 버튼: 체크된 STRU 전체 순서대로 자동 반복
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
        /// T-064 P2 본진 — [도면 리스트 뽑기] 버튼.
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

            // PDF 저장 폴더 선택
            string saveDir;
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = $"PDF 저장 폴더 선택 ({checkedStrus.Count}개 STRU)";
                dlg.SelectedPath = @"c:\";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                saveDir = dlg.SelectedPath;
            }

            // 다중 STRU 확인 팝업 (사용자 요구사항 6번)
            if (checkedStrus.Count > 1)
            {
                var ret = MessageBox.Show(
                    $"선택된 {checkedStrus.Count}개 STRU의 도면 4종(제작/조립/설치/가공) PDF를 일괄 생성합니다.\n계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ret != DialogResult.Yes) return;
            }

            _p2aInProgress = true;
            btnExtractDrawingList.Enabled = false;

            int successCount = 0, failCount = 0;
            var errors = new List<string>();
            int totalPdfCount = 0;

            try
            {
                for (int s = 0; s < checkedStrus.Count; s++)
                {
                    var stru = checkedStrus[s];
                    ShowBusyOverlay($"STRU 처리 {s + 1}/{checkedStrus.Count}: {stru.NodeName}");
                    Application.DoEvents();

                    try
                    {
                        int pdfCount = ProcessSingleStruFull(stru, saveDir);
                        successCount++;
                        totalPdfCount += pdfCount;
                        DiagLog($"T-064 STRU '{stru.NodeName}' 완료 — PDF {pdfCount}개 생성");
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
                }
            }
            finally
            {
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

                _p2aInProgress = false;
                try { btnExtractDrawingList.Enabled = true; } catch { }
                try { HideBusyOverlay(); } catch { }
            }

            // 결과 요약
            string msg = $"STRU 일괄 도면 출력 완료\n\n성공: {successCount}개 STRU (PDF {totalPdfCount}개)\n실패: {failCount}개";
            if (errors.Count > 0)
            {
                int maxShow = Math.Min(10, errors.Count);
                msg += $"\n\n실패 목록 (최대 10건):\n{string.Join("\n", errors.Take(maxShow))}";
                if (errors.Count > maxShow) msg += $"\n... +{errors.Count - maxShow}건";
            }
            MessageBox.Show(msg, "완료", MessageBoxButtons.OK,
                failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
        private int ProcessSingleStruFull(VIZCore3D.NET.Data.Node struNode, string saveDir)
        {
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
            bool bomCollected = CollectBOMData();
            DiagLog($"T-064 STRU '{struNode.NodeName}' CollectBOMData success={bomCollected}, bomList={bomList?.Count ?? 0}");

            // 3) DetectClash 호출 (비동기) — OnFinished가 자동으로 시트 생성·치수계산까지 진행
            bool startResult = DetectClash();
            DiagLog($"T-064 STRU '{struNode.NodeName}' DetectClash startResult={startResult}");
            if (!startResult)
            {
                // Clash 페어 0개 등 시작 실패 — 단일 부재 또는 모든 부재가 서로 멀어진 경우.
                // 그래도 GenerateDrawingSheets 직접 호출 시도 (Sheet 1 + 가공도라도 생성)
                GenerateDrawingSheets();
            }
            else
            {
                // 4) 비동기 완료 폴링 (최대 60초 — STRU 크기에 따라 조정 필요)
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (vizcore3d.Clash.IsBusy && sw.ElapsedMilliseconds < 60000)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                }
                if (vizcore3d.Clash.IsBusy)
                {
                    DiagLog($"T-064 STRU '{struNode.NodeName}' TIMEOUT (60s)");
                    throw new Exception("간섭검사 60초 타임아웃");
                }
            }

            // 5) drawingSheetList 채워졌는지 확인
            if (drawingSheetList == null || drawingSheetList.Count == 0)
                throw new Exception("시트 0건 — GenerateDrawingSheets 결과 비어있음");

            DiagLog($"T-064 STRU '{struNode.NodeName}' 시트 {drawingSheetList.Count}개 생성");

            // 6) 각 시트 → 2D 렌더 + PDF 출력
            int pdfCount = 0;
            string safeStruName = SanitizeFileName(struNode.NodeName ?? "STRU");
            string timeStamp = DateTime.Now.ToString("HHmmss");

            // 옵션 B — lvDrawingSheet.Items 순회 (UI 동기 보장).
            // MultiSelect=true (ListView 기본값, Designer.cs 미지정) → SelectedIndices.Clear()로 이전 선택 해제 필요.
            for (int i = 0; i < lvDrawingSheet.Items.Count; i++)
            {
                var lvi = lvDrawingSheet.Items[i];
                var sheet = lvi.Tag as DrawingSheetData;
                if (sheet == null) continue;

                try
                {
                    // ★ 옵션 B 핵심: 행 자동 선택 → LvDrawingSheet_SelectedIndexChanged 자동 트리거.
                    //   핸들러가 자동으로:
                    //     - X-Ray 비활성화 + SilhouetteEdge 활성화
                    //     - Show(bomList, false) + Show(sheet.MemberIndices, true) — 가시성 격리
                    //     - FlyToObject3d (가공도 제외) — 카메라 fit
                    //     - 풍선·Clash 심볼 정리 + 기준부재 빨간 하이라이트
                    //     - 시트 종류별 치수 추출 분기
                    //         가공도(-3) = ExecuteMfgDrawing
                    //         설치도(-2) = ExtractInstallationDimensions
                    //         일반(-1, >=0) = ComputeViewDimensionsForMembers
                    //     - lvDimension 채움 + CollectBOMInfo 자동 호출
                    lvDrawingSheet.SelectedIndices.Clear();  // MultiSelect=true 대응 — 이전 선택 해제
                    lvi.Selected = true;                      // 새 선택 → SelectedIndexChanged 동기 트리거
                    lvi.EnsureVisible();                      // 진행 표시 — 화면에 보이게 스크롤
                    Application.DoEvents();                   // 핸들러·치수 계산 완료 대기
                    System.Threading.Thread.Sleep(200);

                    // ★ 시트 종류별 처리 — 사용자 평소 버튼 흐름과 동일하게:
                    //   - 제작도(-1) / 조립도(≥0): btnGenerateSheet2D_Click 흐름 = GenerateSheetDrawing2D(sheet)
                    //   - 가공도(-3): btnMfgDrawing_Click 흐름 = ExecuteMfgDrawing(bomIndex). 단 옵션 B Selected=true가
                    //     LvDrawingSheet_SelectedIndexChanged 핸들러(Form1.DrawingSheets.cs:601)에서 이미
                    //     ExecuteMfgDrawing(sheet.MemberIndices[0]) 자동 호출 → 우리 추가 호출 불필요
                    //   - 설치도(-2): 사용자 의도 "아직 코드 없음" — skip
                    if (sheet.BaseMemberIndex == -2)
                    {
                        DiagLog($"T-064 STRU '{struNode.NodeName}' 설치도 시트(-2) skip — 사용자 의도: 구현 미완성");
                        continue;
                    }
                    if (sheet.BaseMemberIndex != -3)
                    {
                        // 제작도(-1) / 조립도(≥0): 2D 도면 렌더 직접 호출 (btnGenerateSheet2D_Click L1049와 동일)
                        GenerateSheetDrawing2D(sheet);
                    }
                    // 가공도(-3): 핸들러가 ExecuteMfgDrawing 자동 호출 — 추가 처리 없음
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);       // 2D 렌더 안정

                    // 시트 종류 라벨 + PDF 출력
                    string kindName = GetSheetKindLabel(sheet);  // 제작도/조립도/설치도/가공도
                    string pdfFile = $"{safeStruName}_{kindName}_Sheet{sheet.SheetNumber}_{timeStamp}.pdf";
                    string pdfPath = Path.Combine(saveDir, pdfFile);
                    vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                    vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                    vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                    DiagLog($"T-064 PDF saved: {pdfPath}");
                    pdfCount++;

                    // 시트 간 2D 메모리 정리
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView(); } catch { }
                }
                catch (Exception ex)
                {
                    DiagLog($"T-064 STRU '{struNode.NodeName}' sheet#{sheet.SheetNumber} ERROR: {ex.Message}");
                }
            }

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
