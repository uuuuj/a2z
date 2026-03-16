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

        private void CollectBOMInfo(bool showAlert = true)
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

                // ★ 도면시트 선택 시 해당 시트 부재만 필터링
                if (lvDrawingSheet.SelectedItems.Count > 0)
                {
                    DrawingSheetData selectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
                    if (selectedSheet != null && selectedSheet.MemberIndices.Count > 0)
                    {
                        var sheetBodySet = new HashSet<int>(selectedSheet.MemberIndices);
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
                                }
                                catch { }
                            }
                        }

                        // 3개 값 모두 찾으면 중단
                        if (!string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(gweiVal))
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

                    // SPREF 파싱: 첫 글자 "/" 제거 후 ":" 기준 split → [0]=ITEM, [1]=SIZE
                    string itemVal = "";
                    string sizeVal = "";
                    if (!string.IsNullOrEmpty(sprefVal))
                    {
                        string sprefClean = sprefVal;
                        if (sprefClean.StartsWith("/"))
                            sprefClean = sprefClean.Substring(1);
                        string[] parts = sprefClean.Split(':');
                        itemVal = parts[0].Trim();
                        if (parts.Length > 1)
                            sizeVal = parts[1].Trim();
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

                // 작업/데이터 탭 bomList(Name) → No. 매핑 구축 (부재이름 기준)
                var bomNameToNo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int bi = 0; bi < bomList.Count; bi++)
                {
                    string name = bomList[bi].Name ?? "";
                    if (!string.IsNullOrEmpty(name) && !bomNameToNo.ContainsKey(name))
                    {
                        bomNameToNo[name] = bi + 1; // bomList는 Z_Max 내림차순, No. = index+1
                    }
                }

                // Row 1~N: 개별 파트 행 (No.는 작업/데이터 BOM의 부재이름 기준)
                int fallbackNo = bomList.Count + 1; // 매칭 안될 경우 bomList 다음 번호부터
                foreach (var bomItem in rawBomItems)
                {
                    string itemName = bomItem.Item1; // ITEM (부재이름)

                    // 작업/데이터 BOM에서 같은 이름의 No. 찾기
                    int matchedNo;
                    if (!string.IsNullOrEmpty(itemName) && bomNameToNo.ContainsKey(itemName))
                    {
                        matchedNo = bomNameToNo[itemName];
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

                // 전체 요약 알림 (BOM + Osnap + 치수 + Clash 한 번에)
                string clashResult = clashList.Count > 0
                    ? $"Clash: {clashList.Count}개 검출 (검사 쌍: {testCount}개)"
                    : $"Clash: 간섭 없음 (검사 쌍: {testCount}개)";

                string summaryMessage = $"모델 로드 및 자동 처리 완료!\n\n" +
                    $"BOM: {bomList.Count}개\n" +
                    $"Osnap: {osnapPointsWithNames.Count}개\n" +
                    $"치수: {chainDimensionList.Count}개\n" +
                    clashResult;

                if (!_autoProcessOsnapSuccess)
                {
                    summaryMessage += "\n\n* Osnap 수집 실패";
                }

                MessageBox.Show(summaryMessage, "자동 처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Clash 완료 후 도면 시트 자동 생성
                if (clashList.Count > 0)
                {
                    GenerateDrawingSheets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"간섭검사 결과 처리 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
