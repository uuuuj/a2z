using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// BOM 정보 수집 버튼 클릭 - UDA에서 Item, Size, Matl, Weight를 가져와 그룹핑
        /// </summary>
        private void btnCollectBOMInfo_Click(object sender, EventArgs e)
        {
            CollectBOMInfo(true);
        }

        private void CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)
        {
            try
            {
                lvDrawingBOMInfo.Items.Clear();

                // Part 노드 가져오기 (Part 레벨에서 UDA 조회)
                List<VIZCore3D.NET.Data.Node> partNodes = vizcore3d.Object3D.GetPartialNode(false, true, false);
                if (partNodes == null || partNodes.Count == 0)
                {
                    // Part가 없으면 Body 노드로 시도
                    partNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                }

                if (partNodes == null || partNodes.Count == 0)
                {
                    if (showAlert) MessageBox.Show("로드된 모델이 없거나 노드를 찾을 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ★ 도면시트 부재만 필터링 (sheetOverride 우선, 없으면 ListView 선택)
                DrawingSheetData targetSheet = sheetOverride;
                if (targetSheet == null && lvDrawingSheet.SelectedItems.Count > 0)
                    targetSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;

                if (targetSheet != null && targetSheet.MemberIndices.Count > 0)
                {
                    var sheetBodySet = new HashSet<int>(targetSheet.MemberIndices);
                        List<VIZCore3D.NET.Data.Node> bodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                        var partIdxSorted = partNodes.Select(p => p.Index).OrderBy(x => x).ToList();
                        var allowedPartIndices = new HashSet<int>();

                        if (bodyNodes != null)
                        {
                            foreach (var body in bodyNodes)
                            {
                                if (!sheetBodySet.Contains(body.Index)) continue;
                                int lo = 0, hi = partIdxSorted.Count - 1;
                                int parentPart = -1;
                                while (lo <= hi)
                                {
                                    int mid = (lo + hi) / 2;
                                    if (partIdxSorted[mid] <= body.Index)
                                    {
                                        parentPart = partIdxSorted[mid];
                                        lo = mid + 1;
                                    }
                                    else hi = mid - 1;
                                }
                                if (parentPart >= 0) allowedPartIndices.Add(parentPart);
                            }
                        }

                        partNodes = partNodes.Where(p => allowedPartIndices.Contains(p.Index)).ToList();
                    }

                // UDA 키 목록 한번만 조회
                List<string> udaKeyList = null;
                try
                {
                    var keys = vizcore3d.Object3D.UDA.Keys;
                    if (keys != null && keys.Count > 0)
                        udaKeyList = new List<string>(keys);
                }
                catch { }

                // 각 Part 노드에서 SPREF/MATREF/GWEI 값 수집 (현재 노드에 없으면 부모로 올라가며 재조회)
                var rawBomItems = new List<Tuple<string, string, string, string, int>>();  // Item, Size, Material, Weight, NodeIndex
                double totalWeight = 0;

                foreach (var node in partNodes)
                {
                    string sprefVal = "";
                    string matrefVal = "";
                    string gweiVal = "";
                    string posStartVal = "";  // T-061: POSSTART UDA — 길이 계산용
                    string posEndVal = "";    // T-061: POSEND UDA

                    // 현재 노드부터 부모로 올라가며 UDA 조회 (최대 10단계)
                    int currentIdx = node.Index;
                    for (int depth = 0; depth < 10; depth++)
                    {
                        if (currentIdx < 0) break;

                        if (udaKeyList != null)
                        {
                            foreach (string key in udaKeyList)
                            {
                                string keyUpper = key.Trim().ToUpper();
                                try
                                {
                                    var val = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                                    string valStr = (val != null) ? val.ToString().Trim() : "";

                                    if (keyUpper == "SPREF" && string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(valStr))
                                        sprefVal = valStr;
                                    else if (keyUpper == "MATREF" && string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(valStr))
                                        matrefVal = valStr;
                                    else if (keyUpper == "GWEI" && string.IsNullOrEmpty(gweiVal) && !string.IsNullOrEmpty(valStr))
                                        gweiVal = valStr;
                                    else if (keyUpper == "POSSTART" && string.IsNullOrEmpty(posStartVal) && !string.IsNullOrEmpty(valStr))
                                        posStartVal = valStr;
                                    else if (keyUpper == "POSEND" && string.IsNullOrEmpty(posEndVal) && !string.IsNullOrEmpty(valStr))
                                        posEndVal = valStr;
                                }
                                catch { }
                            }
                        }

                        // 5개 값 모두 찾으면 중단
                        if (!string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(gweiVal)
                            && !string.IsNullOrEmpty(posStartVal) && !string.IsNullOrEmpty(posEndVal))
                            break;

                        // 부모 노드로 이동
                        try
                        {
                            VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                            if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                            currentIdx = parentNode.ParentIndex;
                        }
                        catch { break; }
                    }

                    // SPREF 파싱 (T-061): 첫 글자 "/" 제거 후 "/" 또는 ":" 중 먼저 나오는 위치에서 split
                    // 예: "/H300x250:SIZE" → ITEM="H300x250", rest="SIZE"
                    // 예: "/PART/ITEM/REST" → ITEM="PART", rest="ITEM/REST"
                    string itemVal = "";
                    string sizeVal = "";
                    if (!string.IsNullOrEmpty(sprefVal))
                    {
                        string sprefClean = sprefVal.StartsWith("/") ? sprefVal.Substring(1) : sprefVal;
                        int slashIdx = sprefClean.IndexOf('/');
                        int colonIdx = sprefClean.IndexOf(':');
                        int splitIdx;
                        if (slashIdx < 0 && colonIdx < 0) splitIdx = sprefClean.Length;
                        else if (slashIdx < 0) splitIdx = colonIdx;
                        else if (colonIdx < 0) splitIdx = slashIdx;
                        else splitIdx = Math.Min(slashIdx, colonIdx);

                        itemVal = sprefClean.Substring(0, splitIdx).Trim();
                        if (splitIdx < sprefClean.Length)
                            sizeVal = sprefClean.Substring(splitIdx + 1).Trim();
                    }

                    // T-061: POSSTART/POSEND로 길이 계산 → SIZE 뒤에 "xLENGTH" 형태로 추가
                    // 한 축만 다른 경우든 두/세 축 다른 경우든 일률 3D 거리 공식 (sqrt(dx²+dy²+dz²))
                    // POSSTART/POSEND가 비어 있으면 길이 추가 안 함 (SIZE 그대로)
                    if (!string.IsNullOrEmpty(posStartVal) && !string.IsNullOrEmpty(posEndVal))
                    {
                        float[] s = ParsePosString(posStartVal);
                        float[] e = ParsePosString(posEndVal);
                        float dxL = e[0] - s[0], dyL = e[1] - s[1], dzL = e[2] - s[2];
                        float length = (float)Math.Sqrt(dxL * dxL + dyL * dyL + dzL * dzL);
                        string lengthStr = length.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        sizeVal = string.IsNullOrEmpty(sizeVal) ? lengthStr : $"{sizeVal}x{lengthStr}";
                    }

                    // UDA에 SPREF가 없으면 노드 이름을 Item으로 사용
                    if (string.IsNullOrEmpty(itemVal))
                        itemVal = node.NodeName ?? "";

                    // MATREF 파싱: 첫 글자 "/" 제거 → MATERIAL 값
                    string materialVal = matrefVal;
                    if (!string.IsNullOrEmpty(materialVal) && materialVal.StartsWith("/"))
                        materialVal = materialVal.Substring(1);

                    // T/W 합계 계산 + 소수점 둘째자리 반올림
                    double w = 0;
                    string gweiDisplay = gweiVal;
                    if (!string.IsNullOrEmpty(gweiVal))
                    {
                        // 숫자 외 문자 제거 (단위 등), 소수점/부호/숫자만 남김
                        string numStr = new string(gweiVal.Where(c => char.IsDigit(c) || c == '.' || c == '-' || c == ',').ToArray());
                        // 쉼표를 소수점으로 변환 (로케일 대응)
                        numStr = numStr.Replace(',', '.');
                        if (double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w))
                            gweiDisplay = Math.Round(w, 2).ToString("F2");
                    }
                    totalWeight += w;

                    rawBomItems.Add(Tuple.Create(itemVal, sizeVal, materialVal, gweiDisplay, node.Index));
                }

                // bomInfoNodeGroupMap 구축: Body nodeIndex → groupNo 매핑
                bomInfoNodeGroupMap.Clear();
                List<VIZCore3D.NET.Data.Node> bodyNodesForMap = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (bodyNodesForMap != null && bodyNodesForMap.Count > 0)
                {
                    List<int> partIdxSorted = partNodes.Select(p => p.Index).OrderBy(x => x).ToList();

                    // 각 Part에 순차적으로 groupNo 부여 (Row 0은 요약행이므로 1부터)
                    var partToGroup = new Dictionary<int, int>();
                    int groupNo = 1;
                    foreach (var bomItem in rawBomItems)
                    {
                        partToGroup[bomItem.Item5] = groupNo;
                        groupNo++;
                    }

                    foreach (var body in bodyNodesForMap)
                    {
                        int parentPartIndex = -1;
                        int lo = 0, hi = partIdxSorted.Count - 1;
                        while (lo <= hi)
                        {
                            int mid = (lo + hi) / 2;
                            if (partIdxSorted[mid] <= body.Index)
                            {
                                parentPartIndex = partIdxSorted[mid];
                                lo = mid + 1;
                            }
                            else
                            {
                                hi = mid - 1;
                            }
                        }
                        if (parentPartIndex >= 0 && partToGroup.ContainsKey(parentPartIndex))
                        {
                            bomInfoNodeGroupMap[body.Index] = partToGroup[parentPartIndex];
                        }
                    }
                }

                // ListView에 채우기 (BOM정보 탭)
                lvDrawingBOMInfo.BeginUpdate();

                // Row 0: 요약행
                ListViewItem summaryRow = new ListViewItem("");                      // No.
                summaryRow.SubItems.Add("Support&Seat");                             // ITEM
                summaryRow.SubItems.Add("");                                         // MATERIAL
                summaryRow.SubItems.Add("");                                         // SIZE
                summaryRow.SubItems.Add("");                                         // Q'TY
                summaryRow.SubItems.Add(totalWeight > 0 ? Math.Round(totalWeight, 2).ToString("F2") : ""); // T/W
                summaryRow.SubItems.Add("F");                                        // MA
                summaryRow.SubItems.Add("F");                                        // FA
                lvDrawingBOMInfo.Items.Add(summaryRow);

                // bomList Body index → Part index → No 매핑 (인덱스 기반, 이름 불일치 방지)
                var partIndexToBomNo = new Dictionary<int, int>();
                for (int bi = 0; bi < bomList.Count; bi++)
                {
                    int partIdx = bodyToPartIndexMap.ContainsKey(bomList[bi].Index)
                        ? bodyToPartIndexMap[bomList[bi].Index]
                        : bomList[bi].Index;
                    if (!partIndexToBomNo.ContainsKey(partIdx))
                        partIndexToBomNo[partIdx] = bi + 1;
                }

                // Row 1~N: 개별 파트 행 (No.는 작업/데이터 BOM의 Part Index 기준)
                int fallbackNo = bomList.Count + 1;
                foreach (var bomItem in rawBomItems)
                {
                    int partIndex = bomItem.Item5; // Part index

                    // 작업/데이터 BOM에서 같은 Part Index의 No. 찾기
                    int matchedNo;
                    if (partIndexToBomNo.ContainsKey(partIndex))
                    {
                        matchedNo = partIndexToBomNo[partIndex];
                    }
                    else
                    {
                        matchedNo = fallbackNo++;
                    }

                    // BOM정보 탭
                    ListViewItem lvi = new ListViewItem(matchedNo.ToString()); // No. (작업/데이터 BOM 기준)
                    lvi.SubItems.Add(bomItem.Item1);                      // ITEM
                    lvi.SubItems.Add(bomItem.Item3);                      // MATERIAL
                    lvi.SubItems.Add(bomItem.Item2);                      // SIZE
                    lvi.SubItems.Add("1");                                // Q'TY
                    lvi.SubItems.Add(bomItem.Item4);                      // T/W
                    lvi.SubItems.Add("L");                                // MA
                    lvi.SubItems.Add("F");                                // FA
                    lvDrawingBOMInfo.Items.Add(lvi);
                }

                // No. 기준 오름차순 정렬 (첫 번째 요약행 제외, 1행부터 정렬)
                if (lvDrawingBOMInfo.Items.Count > 1)
                {
                    var dataRows = new List<ListViewItem>();
                    for (int ri = 1; ri < lvDrawingBOMInfo.Items.Count; ri++)
                        dataRows.Add((ListViewItem)lvDrawingBOMInfo.Items[ri].Clone());

                    dataRows.Sort((a, b) =>
                    {
                        int na = 0, nb = 0;
                        int.TryParse(a.Text, out na);
                        int.TryParse(b.Text, out nb);
                        return na.CompareTo(nb);
                    });

                    // 정렬된 행으로 교체 (요약행 유지)
                    while (lvDrawingBOMInfo.Items.Count > 1)
                        lvDrawingBOMInfo.Items.RemoveAt(lvDrawingBOMInfo.Items.Count - 1);

                    foreach (var row in dataRows)
                        lvDrawingBOMInfo.Items.Add(row);
                }

                lvDrawingBOMInfo.EndUpdate();

                if (showAlert) MessageBox.Show(string.Format("BOM 정보 {0}개 항목 수집 완료", rawBomItems.Count), "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (showAlert) MessageBox.Show("BOM 정보 수집 오류:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clash Detection 수행 (ClashManager API 사용)
        /// </summary>
        private bool DetectClash()
        {
            clashList.Clear();
            lvClash.Items.Clear();

            try
            {
                List<VIZCore3D.NET.Data.Node> allNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);

                if (allNodes == null || allNodes.Count == 0)
                {
                    return false;
                }

                // 가시성 필터링: 프로그래밍 선택 또는 FromIndex().Visible
                List<VIZCore3D.NET.Data.Node> targetNodes;
                if (xraySelectedNodeIndices.Count > 0)
                {
                    HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                    targetNodes = allNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
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

                vizcore3d.Clash.Clear();
                int clashCount = 0;

                for (int i = 0; i < targetNodes.Count; i++)
                {
                    for (int j = i + 1; j < targetNodes.Count; j++)
                    {
                        VIZCore3D.NET.Data.ClashTest pairClash = new VIZCore3D.NET.Data.ClashTest();
                        pairClash.Name = $"간섭검사_{targetNodes[i].NodeName}_vs_{targetNodes[j].NodeName}";
                        pairClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        pairClash.UseClearanceValue = true;
                        pairClash.ClearanceValue = 1.0f;
                        pairClash.UseRangeValue = true;
                        pairClash.RangeValue = 1.0f;
                        pairClash.UsePenetrationTolerance = true;
                        pairClash.PenetrationTolerance = 1.0f;
                        pairClash.VisibleOnly = false;
                        pairClash.BottomLevel = 0;
                        pairClash.GroupA = new List<VIZCore3D.NET.Data.Node> { targetNodes[i] };
                        pairClash.GroupB = new List<VIZCore3D.NET.Data.Node> { targetNodes[j] };

                        if (vizcore3d.Clash.Add(pairClash))
                        {
                            clashCount++;
                        }
                    }
                }

                if (clashCount == 0) return false;

                bool startResult = vizcore3d.Clash.PerformInterferenceCheck();
                return startResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clash 검사 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clash 검사 버튼
        /// </summary>
        private void btnClashDetection_Click(object sender, EventArgs e)
        {
            bool success = DetectClash();
            if (success)
            {
                MessageBox.Show("간섭검사를 시작합니다.\n완료되면 알림창이 표시됩니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("로드된 모델이 없거나 간섭검사 시작에 실패했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 간섭검사 완료 이벤트 핸들러
        /// </summary>
        private void Clash_OnClashTestFinishedEvent(object sender, VIZCore3D.NET.Event.EventManager.ClashEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Clash Finished] 이벤트 발생! ID: {e.ID}");

                // ClashTest 개수 확인
                int testCount = vizcore3d.Clash.ClashTestCount;
                System.Diagnostics.Debug.WriteLine($"현재 등록된 ClashTest 개수: {testCount}");

                clashList.Clear();
                lvClash.Items.Clear();

                // 모든 ClashTest 결과 수집
                for (int i = 0; i < testCount; i++)
                {
                    VIZCore3D.NET.Data.ClashTest clashTest = vizcore3d.Clash.Items[i];

                    if (clashTest == null) continue;

                    // 결과 조회 (PART 레벨로 그룹화)
                    var results = vizcore3d.Clash.GetResultItem(
                        clashTest,
                        VIZCore3D.NET.Manager.ClashManager.ResultGroupingOptions.PART
                    );

                    if (results != null && results.Count > 0)
                    {
                        foreach (var result in results)
                        {
                            ClashData clash = new ClashData();

                            // 노드 인덱스
                            clash.Index1 = result.NodeIndexA;
                            clash.Index2 = result.NodeIndexB;

                            // 노드 이름
                            clash.Name1 = !string.IsNullOrEmpty(result.NodeNameA) ? result.NodeNameA : "Unknown";
                            clash.Name2 = !string.IsNullOrEmpty(result.NodeNameB) ? result.NodeNameB : "Unknown";

                            // 간섭 위치 (HotPoint의 Z 값)
                            if (result.HotPoint != null)
                            {
                                clash.ZValue = result.HotPoint.Z;
                            }

                            // 중복 검사 (A-B와 B-A 동일 처리)
                            bool isDuplicate = clashList.Any(c =>
                                (c.Index1 == clash.Index1 && c.Index2 == clash.Index2) ||
                                (c.Index1 == clash.Index2 && c.Index2 == clash.Index1));

                            if (!isDuplicate)
                            {
                                clashList.Add(clash);
                            }
                        }
                    }
                }

                if (clashList.Count > 0)
                {
                    // Z값 기준으로 정렬 (높은 값부터 - 내림차순)
                    clashList.Sort((a, b) => b.ZValue.CompareTo(a.ZValue));

                    // ListView에 추가
                    foreach (var clash in clashList)
                    {
                        ListViewItem lvi = new ListViewItem(clash.Name1);
                        lvi.SubItems.Add(clash.Name2);
                        lvi.SubItems.Add(clash.ZValue.ToString("F2"));
                        lvi.Tag = clash;
                        lvClash.Items.Add(lvi);
                    }
                }

                // T-023 v3: 연결성 판정 — bomList의 부재들이 Clash 인접 그래프 기준
                // "한 덩어리(연결 성분 1개)"인가? 떨어진 부재가 하나라도 있으면 차단.
                // 이 판정이 통과해야만 Osnap/치수/요약/시트 생성으로 이어진다.
                int componentCount;
                if (!IsSingleConnectedComponent(out componentCount))
                {
                    HideBusyOverlay();
                    MessageBox.Show(
                        "치수 추출은 모든 부재가 **하나의 덩어리로 연결**되어 있을 때만 가능합니다.\n\n" +
                        $"현재: 서로 연결되지 않은 부재 그룹 {componentCount}개 발견 (Clash 인접 기준)\n\n" +
                        "해결 방법:\n" +
                        "- 떨어진 부재를 모델트리 체크박스로 숨기기\n" +
                        "- 한 덩어리만 남기고 다시 치수 추출",
                        "치수 추출 사전조건", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DiagLog($"btnMainDimension BLOCKED components={componentCount} (T-023 v3)");
                    return;
                }

                // 연결성 통과 → Osnap 수집 → 치수 계산 → 요약 → 시트 생성
                // 오버레이 해제는 CompleteMainDimensionPostClash의 finally에서 수행
                CompleteMainDimensionPostClash(isSingleMember: false, clashTestCount: testCount);
            }
            catch (Exception ex)
            {
                HideBusyOverlay();
                MessageBox.Show($"간섭검사 결과 처리 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// T-023 v3: Clash 인접 그래프 기준 연결 성분 수 계산.
        /// bomList가 모두 하나의 연결 성분에 속하면 true, 떨어진 부재가 있으면 false.
        /// componentCount out 파라미터로 발견된 연결 성분 수 반환.
        /// </summary>
        private bool IsSingleConnectedComponent(out int componentCount)
        {
            componentCount = 0;
            if (bomList == null || bomList.Count == 0) return false;
            if (bomList.Count == 1)
            {
                componentCount = 1;
                return true;
            }

            // Part → Body 역매핑 (Clash는 Part 인덱스, bomList는 Body 인덱스)
            Dictionary<int, List<int>> partToBodyIndices = new Dictionary<int, List<int>>();
            foreach (var bom in bomList)
            {
                if (bodyToPartIndexMap.ContainsKey(bom.Index))
                {
                    int partIdx = bodyToPartIndexMap[bom.Index];
                    if (!partToBodyIndices.ContainsKey(partIdx))
                        partToBodyIndices[partIdx] = new List<int>();
                    partToBodyIndices[partIdx].Add(bom.Index);
                }
            }

            // Clash 인접 리스트 구축 (Body 기반, 양방향)
            Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
            foreach (var clash in clashList)
            {
                List<int> bodies1 = partToBodyIndices.ContainsKey(clash.Index1) ? partToBodyIndices[clash.Index1] : new List<int>();
                List<int> bodies2 = partToBodyIndices.ContainsKey(clash.Index2) ? partToBodyIndices[clash.Index2] : new List<int>();

                foreach (int b1 in bodies1)
                {
                    foreach (int b2 in bodies2)
                    {
                        if (b1 == b2) continue;
                        if (!adjacency.ContainsKey(b1)) adjacency[b1] = new HashSet<int>();
                        if (!adjacency.ContainsKey(b2)) adjacency[b2] = new HashSet<int>();
                        adjacency[b1].Add(b2);
                        adjacency[b2].Add(b1);
                    }
                }
            }

            // BFS로 연결 성분 카운트 (≥ 2 발견 즉시 early exit)
            HashSet<int> visited = new HashSet<int>();
            foreach (var bom in bomList)
            {
                if (visited.Contains(bom.Index)) continue;

                componentCount++;
                if (componentCount > 1) return false;

                Queue<int> queue = new Queue<int>();
                queue.Enqueue(bom.Index);
                visited.Add(bom.Index);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    if (adjacency.ContainsKey(current))
                    {
                        foreach (int neighbor in adjacency[current])
                        {
                            if (!visited.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            return componentCount == 1;
        }

        /// <summary>
        /// T-061: POSSTART / POSEND UDA 문자열에서 3개 숫자 추출
        /// 예: "50mm S 1000.22mm U 500.00mm" → [50.0, 1000.22, 500.0]
        /// (S/U 토큰 의미 미정 — 일단 등장 순서 그대로 매칭)
        /// 숫자가 3개 미만이면 부족한 자리를 0으로 채움
        /// </summary>
        private float[] ParsePosString(string raw)
        {
            var nums = new List<float>();
            if (!string.IsNullOrEmpty(raw))
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"-?\d+(?:\.\d+)?");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    if (float.TryParse(m.Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                        nums.Add(v);
                    if (nums.Count >= 3) break;
                }
            }
            while (nums.Count < 3) nums.Add(0f);
            return new[] { nums[0], nums[1], nums[2] };
        }
    }
}
