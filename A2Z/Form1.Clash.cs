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
        private readonly Queue<int> _silentClashPendingTestIds = new Queue<int>();
        private bool _silentClashSequenceActive;
        private int _silentClashActiveTestId = -1;
        private int _silentClashSequenceTotal;
        private int _silentClashCompletedCount;

        /// <summary>
        /// BOM 정보 수집 버튼 클릭 - UDA에서 Item, Size, Matl, Weight를 가져와 그룹핑
        /// </summary>
        private void btnCollectBOMInfo_Click(object sender, EventArgs e)
        {
            CollectBOMInfo(true);
        }

        private sealed class DrawingBomPartData
        {
            public int PartIndex;
            public string Item;
            public string Size;
            public string Material;
            public string WeightDisplay;
            public double Weight;
        }

        private sealed class DrawingBomPreparationContext
        {
            public Dictionary<int, DrawingBomPartData> PartByIndex = new Dictionary<int, DrawingBomPartData>();
            public Dictionary<int, List<int>> PartToBodyIndices = new Dictionary<int, List<int>>();
            public Dictionary<int, int> PartToBomNo = new Dictionary<int, int>();
        }

        private sealed class DrawingBomSnapshot
        {
            public List<DrawingBomRowData> Rows = new List<DrawingBomRowData>();
            public Dictionary<int, int> NodeGroupMap = new Dictionary<int, int>();
        }

        private void CollectBOMInfo(bool showAlert = true, DrawingSheetData sheetOverride = null)
        {
            DrawingSheetData targetSheet = sheetOverride;
            if (targetSheet == null && lvDrawingSheet.SelectedItems.Count > 0)
                targetSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool cacheHit = targetSheet != null && targetSheet.BomPrepared;

                if (!cacheHit)
                {
                    IEnumerable<int> targetBodies = targetSheet?.MemberIndices;
                    DrawingBomPreparationContext context = BuildDrawingBomPreparationContext(targetBodies);
                    if (context.PartByIndex.Count == 0)
                    {
                        lvDrawingBOMInfo.Items.Clear();
                        if (showAlert)
                            MessageBox.Show("로드된 모델이 없거나 노드를 찾을 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DrawingBomSnapshot snapshot = BuildDrawingBomSnapshot(context, targetBodies);
                    if (targetSheet != null)
                        StorePreparedBomSnapshot(targetSheet, snapshot);
                    else
                        ApplyBomSnapshot(snapshot);
                }

                if (targetSheet != null)
                    ApplyPreparedBomInfo(targetSheet);

                sw.Stop();
                DiagLog($"BOM 정보 적용: sheet#={targetSheet?.SheetNumber ?? 0} " +
                    $"cacheHit={cacheHit} rows={lvDrawingBOMInfo.Items.Count} elapsed={sw.ElapsedMilliseconds}ms");

                if (showAlert)
                    MessageBox.Show($"BOM 정보 {Math.Max(0, lvDrawingBOMInfo.Items.Count - 1)}개 항목 수집 완료", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"CollectBOMInfo FAIL {ex.Message}\n{ex.StackTrace}");
                if (showAlert) MessageBox.Show("BOM 정보 수집 오류:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 도면 리스트를 표시하기 전에 모든 시트의 BOM 행을 한 번에 준비한다.
        /// Body→Part 매핑은 모델 로드 때 만든 bodyToPartIndexMap을 재사용하고 UDA는 Part별 한 번만 읽는다.
        /// </summary>
        private void PrepareDrawingSheetBomCaches()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var allSheetBodies = new HashSet<int>(drawingSheetList.SelectMany(s => s.MemberIndices));
            DrawingBomPreparationContext context = BuildDrawingBomPreparationContext(allSheetBodies);

            foreach (DrawingSheetData sheet in drawingSheetList)
            {
                DrawingBomSnapshot snapshot = BuildDrawingBomSnapshot(context, sheet.MemberIndices);
                StorePreparedBomSnapshot(sheet, snapshot);
            }

            if (drawingSheetList.Count > 0)
                ApplyPreparedBomInfo(drawingSheetList[0]);

            sw.Stop();
            DiagLog($"도면 시트 BOM 사전 준비: sheets={drawingSheetList.Count} " +
                $"parts={context.PartByIndex.Count} bodies={allSheetBodies.Count} elapsed={sw.ElapsedMilliseconds}ms");
        }

        private DrawingBomPreparationContext BuildDrawingBomPreparationContext(IEnumerable<int> targetBodyIndices)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var context = new DrawingBomPreparationContext();
            HashSet<int> targetBodySet = targetBodyIndices != null
                ? new HashSet<int>(targetBodyIndices)
                : null;

            var relevantPartIndices = new HashSet<int>();
            if (targetBodySet != null)
            {
                foreach (int bodyIndex in targetBodySet)
                {
                    int partIndex;
                    if (!bodyToPartIndexMap.TryGetValue(bodyIndex, out partIndex))
                        partIndex = bodyIndex;
                    relevantPartIndices.Add(partIndex);
                }
            }
            else
            {
                List<VIZCore3D.NET.Data.Node> allParts = vizcore3d.Object3D.GetPartialNode(false, true, false);
                if (allParts == null || allParts.Count == 0)
                    allParts = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (allParts != null)
                    foreach (var node in allParts) relevantPartIndices.Add(node.Index);
            }

            var wantedUdaKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var keys = vizcore3d.Object3D.UDA.Keys;
                if (keys != null)
                {
                    foreach (string key in keys)
                    {
                        string normalized = (key ?? "").Trim();
                        if (normalized.Equals("SPREF", StringComparison.OrdinalIgnoreCase) ||
                            normalized.Equals("MATREF", StringComparison.OrdinalIgnoreCase) ||
                            normalized.Equals("GWEI", StringComparison.OrdinalIgnoreCase) ||
                            normalized.Equals("POSSTART", StringComparison.OrdinalIgnoreCase) ||
                            normalized.Equals("POSEND", StringComparison.OrdinalIgnoreCase))
                        {
                            wantedUdaKeys[normalized] = key;
                        }
                    }
                }
            }
            catch { }

            foreach (int partIndex in relevantPartIndices.OrderBy(x => x))
            {
                VIZCore3D.NET.Data.Node node = null;
                try { node = vizcore3d.Object3D.FromIndex(partIndex); }
                catch { }
                if (node == null) continue;

                context.PartByIndex[partIndex] = ReadDrawingBomPartData(node, wantedUdaKeys);
            }

            foreach (var pair in bodyToPartIndexMap)
            {
                if (!relevantPartIndices.Contains(pair.Value)) continue;
                List<int> bodies;
                if (!context.PartToBodyIndices.TryGetValue(pair.Value, out bodies))
                {
                    bodies = new List<int>();
                    context.PartToBodyIndices[pair.Value] = bodies;
                }
                bodies.Add(pair.Key);
            }

            if (targetBodySet != null)
            {
                foreach (int bodyIndex in targetBodySet)
                {
                    int partIndex;
                    if (!bodyToPartIndexMap.TryGetValue(bodyIndex, out partIndex))
                        partIndex = bodyIndex;
                    List<int> bodies;
                    if (!context.PartToBodyIndices.TryGetValue(partIndex, out bodies))
                    {
                        bodies = new List<int>();
                        context.PartToBodyIndices[partIndex] = bodies;
                    }
                    if (!bodies.Contains(bodyIndex)) bodies.Add(bodyIndex);
                }
            }

            for (int i = 0; i < bomList.Count; i++)
            {
                int partIndex;
                if (!bodyToPartIndexMap.TryGetValue(bomList[i].Index, out partIndex))
                    partIndex = bomList[i].Index;
                if (!context.PartToBomNo.ContainsKey(partIndex))
                    context.PartToBomNo[partIndex] = i + 1;
            }

            sw.Stop();
            DiagLog($"BOM 준비 컨텍스트: targetBodies={targetBodySet?.Count ?? -1} " +
                $"parts={context.PartByIndex.Count} udaKeys={wantedUdaKeys.Count} elapsed={sw.ElapsedMilliseconds}ms");
            return context;
        }

        private DrawingBomPartData ReadDrawingBomPartData(
            VIZCore3D.NET.Data.Node node,
            Dictionary<string, string> udaKeys)
        {
            string sprefVal = "";
            string matrefVal = "";
            string gweiVal = "";
            string posStartVal = "";
            string posEndVal = "";

            int currentIdx = node.Index;
            for (int depth = 0; depth < 10 && currentIdx >= 0; depth++)
            {
                sprefVal = ReadDrawingBomUdaValue(currentIdx, "SPREF", sprefVal, udaKeys);
                matrefVal = ReadDrawingBomUdaValue(currentIdx, "MATREF", matrefVal, udaKeys);
                gweiVal = ReadDrawingBomUdaValue(currentIdx, "GWEI", gweiVal, udaKeys);
                posStartVal = ReadDrawingBomUdaValue(currentIdx, "POSSTART", posStartVal, udaKeys);
                posEndVal = ReadDrawingBomUdaValue(currentIdx, "POSEND", posEndVal, udaKeys);

                if (!string.IsNullOrEmpty(sprefVal) && !string.IsNullOrEmpty(matrefVal) && !string.IsNullOrEmpty(gweiVal) &&
                    !string.IsNullOrEmpty(posStartVal) && !string.IsNullOrEmpty(posEndVal))
                    break;

                try
                {
                    VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                    if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                    currentIdx = parentNode.ParentIndex;
                }
                catch { break; }
            }

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

            if (!string.IsNullOrEmpty(posStartVal) && !string.IsNullOrEmpty(posEndVal))
            {
                float[] start = ParsePosString(posStartVal);
                float[] end = ParsePosString(posEndVal);
                float dx = end[0] - start[0], dy = end[1] - start[1], dz = end[2] - start[2];
                float length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                string lengthText = length.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                sizeVal = string.IsNullOrEmpty(sizeVal) ? lengthText : $"{sizeVal}x{lengthText}";
            }

            if (string.IsNullOrWhiteSpace(itemVal)) itemVal = "unset";
            string materialVal = matrefVal;
            if (!string.IsNullOrEmpty(materialVal) && materialVal.StartsWith("/"))
                materialVal = materialVal.Substring(1);

            double weight = 0;
            string weightDisplay = gweiVal;
            if (!string.IsNullOrEmpty(gweiVal))
            {
                string number = new string(gweiVal.Where(c => char.IsDigit(c) || c == '.' || c == '-' || c == ',').ToArray()).Replace(',', '.');
                if (double.TryParse(number, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out weight))
                {
                    weightDisplay = Math.Round(weight, 2).ToString("F2");
                }
            }

            return new DrawingBomPartData
            {
                PartIndex = node.Index,
                Item = itemVal,
                Size = sizeVal,
                Material = materialVal,
                WeightDisplay = weightDisplay,
                Weight = weight
            };
        }

        private string ReadDrawingBomUdaValue(
            int nodeIndex,
            string normalizedKey,
            string currentValue,
            Dictionary<string, string> udaKeys)
        {
            if (!string.IsNullOrEmpty(currentValue)) return currentValue;
            string actualKey;
            if (!udaKeys.TryGetValue(normalizedKey, out actualKey)) return currentValue;
            try
            {
                var value = vizcore3d.Object3D.UDA.FromIndex(nodeIndex, actualKey);
                return value != null ? value.ToString().Trim() : currentValue;
            }
            catch
            {
                return currentValue;
            }
        }

        private DrawingBomSnapshot BuildDrawingBomSnapshot(
            DrawingBomPreparationContext context,
            IEnumerable<int> bodyIndices)
        {
            var relevantParts = new HashSet<int>();
            if (bodyIndices == null)
            {
                relevantParts.UnionWith(context.PartByIndex.Keys);
            }
            else
            {
                foreach (int bodyIndex in bodyIndices)
                {
                    int partIndex;
                    if (!bodyToPartIndexMap.TryGetValue(bodyIndex, out partIndex))
                        partIndex = bodyIndex;
                    if (context.PartByIndex.ContainsKey(partIndex))
                        relevantParts.Add(partIndex);
                }
            }

            List<DrawingBomPartData> parts = relevantParts
                .OrderBy(x => x)
                .Select(x => context.PartByIndex[x])
                .ToList();

            var snapshot = new DrawingBomSnapshot();
            double totalWeight = parts.Sum(p => p.Weight);
            snapshot.Rows.Add(new DrawingBomRowData
            {
                No = "",
                Item = "Support&Seat",
                Material = "",
                Size = "",
                Quantity = "",
                TotalWeight = totalWeight > 0 ? Math.Round(totalWeight, 2).ToString("F2") : "",
                Ma = "F",
                Fa = "F"
            });

            var partToGroup = new Dictionary<int, int>();
            for (int i = 0; i < parts.Count; i++)
                partToGroup[parts[i].PartIndex] = i + 1;

            foreach (var pair in partToGroup)
            {
                List<int> bodies;
                if (!context.PartToBodyIndices.TryGetValue(pair.Key, out bodies)) continue;
                foreach (int bodyIndex in bodies)
                    snapshot.NodeGroupMap[bodyIndex] = pair.Value;
            }

            int fallbackNo = bomList.Count + 1;
            var dataRows = new List<DrawingBomRowData>();
            foreach (DrawingBomPartData part in parts)
            {
                int no;
                if (!context.PartToBomNo.TryGetValue(part.PartIndex, out no))
                    no = fallbackNo++;
                dataRows.Add(new DrawingBomRowData
                {
                    No = no.ToString(),
                    Item = part.Item,
                    Material = part.Item == "unset" ? "-" : part.Material,
                    Size = part.Item == "unset" ? "-" : part.Size,
                    Quantity = part.Item == "unset" ? "-" : "1",
                    TotalWeight = part.Item == "unset" ? "-" : part.WeightDisplay,
                    Ma = part.Item == "unset" ? "-" : "L",
                    Fa = part.Item == "unset" ? "-" : "F"
                });
            }

            dataRows.Sort((a, b) =>
            {
                int na = 0, nb = 0;
                int.TryParse(a.No, out na);
                int.TryParse(b.No, out nb);
                return na.CompareTo(nb);
            });
            snapshot.Rows.AddRange(dataRows);
            return snapshot;
        }

        private void StorePreparedBomSnapshot(DrawingSheetData sheet, DrawingBomSnapshot snapshot)
        {
            sheet.PreparedBomRows.Clear();
            sheet.PreparedBomRows.AddRange(snapshot.Rows);
            sheet.PreparedBomNodeGroupMap.Clear();
            foreach (var pair in snapshot.NodeGroupMap)
                sheet.PreparedBomNodeGroupMap[pair.Key] = pair.Value;
            sheet.BomPrepared = true;
        }

        private void ApplyPreparedBomInfo(DrawingSheetData sheet)
        {
            if (sheet == null || !sheet.BomPrepared) return;
            var snapshot = new DrawingBomSnapshot();
            snapshot.Rows.AddRange(sheet.PreparedBomRows);
            foreach (var pair in sheet.PreparedBomNodeGroupMap)
                snapshot.NodeGroupMap[pair.Key] = pair.Value;
            ApplyBomSnapshot(snapshot);
        }

        private void ApplyBomSnapshot(DrawingBomSnapshot snapshot)
        {
            bomInfoNodeGroupMap.Clear();
            foreach (var pair in snapshot.NodeGroupMap)
                bomInfoNodeGroupMap[pair.Key] = pair.Value;

            lvDrawingBOMInfo.BeginUpdate();
            try
            {
                lvDrawingBOMInfo.Items.Clear();
                foreach (DrawingBomRowData row in snapshot.Rows)
                {
                    var item = new ListViewItem(row.No ?? "");
                    item.SubItems.Add(row.Item ?? "");
                    item.SubItems.Add(row.Material ?? "");
                    item.SubItems.Add(row.Size ?? "");
                    item.SubItems.Add(row.Quantity ?? "");
                    item.SubItems.Add(row.TotalWeight ?? "");
                    item.SubItems.Add(row.Ma ?? "");
                    item.SubItems.Add(row.Fa ?? "");
                    lvDrawingBOMInfo.Items.Add(item);
                }
            }
            finally
            {
                lvDrawingBOMInfo.EndUpdate();
            }
        }

        private void ResetFabricationNeighborSearchCache()
        {
            fabricationBodyBoundsCache.Clear();
            fabricationBodyToPartIndexCache.Clear();
            fabricationNeighborCacheSourceBodyCount = -1;
            ClearFabricationNeighborResults();
        }

        private void ClearFabricationNeighborResults()
        {
            fabricationNeighborClashList.Clear();
            fabricationNeighborPartIndices.Clear();
            fabricationTargetBodyIndices.Clear();
            fabricationTargetPartIndices.Clear();
        }

        /// <summary>
        /// 모델 Body별 Bounding Box와 실제 부모 Part를 한 번만 수집한다.
        /// 이후 제작 대상이 바뀌어도 캐시를 재사용하고, 모델 재로드 시 BuildBodyToPartNameMap에서 초기화한다.
        /// </summary>
        private bool EnsureFabricationNeighborSearchCache(List<VIZCore3D.NET.Data.Node> allBodyNodes)
        {
            if (allBodyNodes == null || allBodyNodes.Count == 0) return false;

            bool cacheComplete = fabricationNeighborCacheSourceBodyCount == allBodyNodes.Count &&
                                 fabricationBodyBoundsCache.Count > 0;
            if (cacheComplete) return true;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            fabricationBodyBoundsCache.Clear();
            fabricationBodyToPartIndexCache.Clear();
            fabricationNeighborCacheSourceBodyCount = allBodyNodes.Count;

            List<VIZCore3D.NET.Data.Node> partNodes =
                vizcore3d.Object3D.GetPartialNode(false, true, false);
            var partIndices = new HashSet<int>(
                partNodes != null ? partNodes.Select(p => p.Index) : Enumerable.Empty<int>());

            foreach (var body in allBodyNodes)
            {
                try
                {
                    var bbox = vizcore3d.Object3D.GetBoundBox(
                        new List<int> { body.Index }, false);
                    if (bbox != null)
                    {
                        fabricationBodyBoundsCache[body.Index] = new BodyBoundsData
                        {
                            MinX = bbox.MinX,
                            MinY = bbox.MinY,
                            MinZ = bbox.MinZ,
                            MaxX = bbox.MaxX,
                            MaxY = bbox.MaxY,
                            MaxZ = bbox.MaxZ
                        };
                    }

                    int partIndex = ResolveActualParentPartIndex(body, partIndices);
                    if (partIndex >= 0)
                        fabricationBodyToPartIndexCache[body.Index] = partIndex;
                }
                catch (Exception ex)
                {
                    DiagLog($"제작도 연결 후보 캐시 실패: body={body.Index} name='{body.NodeName}' {ex.Message}");
                }
            }

            sw.Stop();
            DiagLog($"제작도 연결 후보 캐시 완료: body={allBodyNodes.Count} " +
                    $"bbox={fabricationBodyBoundsCache.Count} part={fabricationBodyToPartIndexCache.Count} " +
                    $"elapsed={sw.ElapsedMilliseconds}ms");
            return fabricationBodyBoundsCache.Count > 0;
        }

        private int ResolveActualParentPartIndex(
            VIZCore3D.NET.Data.Node body,
            HashSet<int> partIndices)
        {
            if (body == null) return -1;
            if (partIndices.Contains(body.Index)) return body.Index;

            int currentIndex = body.ParentIndex;
            for (int depth = 0; depth < 20 && currentIndex >= 0; depth++)
            {
                if (partIndices.Contains(currentIndex)) return currentIndex;

                VIZCore3D.NET.Data.Node parent = vizcore3d.Object3D.FromIndex(currentIndex);
                if (parent == null || parent.ParentIndex == currentIndex) break;
                currentIndex = parent.ParentIndex;
            }

            int fallbackPartIndex;
            return bodyToPartIndexMap.TryGetValue(body.Index, out fallbackPartIndex)
                ? fallbackPartIndex
                : -1;
        }

        private bool BoundsOverlapWithinClearance(
            BodyBoundsData a,
            BodyBoundsData b,
            float clearance)
        {
            return a.MaxX + clearance >= b.MinX && b.MaxX + clearance >= a.MinX &&
                   a.MaxY + clearance >= b.MinY && b.MaxY + clearance >= a.MinY &&
                   a.MaxZ + clearance >= b.MinZ && b.MaxZ + clearance >= a.MinZ;
        }

        /// <summary>
        /// 전체 모델 중 제작 대상과 가까운 Body만 Bounding Box로 선별한다.
        /// Bounding Box는 광역 필터일 뿐이며, 최종 연결 여부는 선별된 후보에 대한 Clash 결과로 결정한다.
        /// </summary>
        private List<VIZCore3D.NET.Data.Node> GetFabricationNeighborCandidates(
            List<VIZCore3D.NET.Data.Node> allBodyNodes,
            List<VIZCore3D.NET.Data.Node> targetNodes)
        {
            var candidates = new List<VIZCore3D.NET.Data.Node>();
            if (!EnsureFabricationNeighborSearchCache(allBodyNodes))
            {
                DiagLog("제작도 연결 후보 선별 중단: Bounding Box 캐시 없음");
                return candidates;
            }

            fabricationTargetBodyIndices = new HashSet<int>(targetNodes.Select(n => n.Index));
            fabricationTargetPartIndices.Clear();
            foreach (var target in targetNodes)
            {
                int partIndex;
                if (fabricationBodyToPartIndexCache.TryGetValue(target.Index, out partIndex))
                    fabricationTargetPartIndices.Add(partIndex);
            }

            var targetBounds = targetNodes
                .Where(n => fabricationBodyBoundsCache.ContainsKey(n.Index))
                .Select(n => fabricationBodyBoundsCache[n.Index])
                .ToList();
            if (targetBounds.Count == 0)
            {
                DiagLog("제작도 연결 후보 선별 중단: 제작 대상 Bounding Box 없음");
                return candidates;
            }

            var aggregateBounds = new BodyBoundsData
            {
                MinX = targetBounds.Min(b => b.MinX),
                MinY = targetBounds.Min(b => b.MinY),
                MinZ = targetBounds.Min(b => b.MinZ),
                MaxX = targetBounds.Max(b => b.MaxX),
                MaxY = targetBounds.Max(b => b.MaxY),
                MaxZ = targetBounds.Max(b => b.MaxZ)
            };

            int aggregateHits = 0;
            foreach (var node in allBodyNodes)
            {
                if (fabricationTargetBodyIndices.Contains(node.Index)) continue;

                int partIndex;
                if (fabricationBodyToPartIndexCache.TryGetValue(node.Index, out partIndex) &&
                    fabricationTargetPartIndices.Contains(partIndex))
                    continue;

                BodyBoundsData candidateBounds;
                if (!fabricationBodyBoundsCache.TryGetValue(node.Index, out candidateBounds))
                    continue;
                if (!BoundsOverlapWithinClearance(
                    aggregateBounds, candidateBounds, FabricationNeighborClearance))
                    continue;

                aggregateHits++;
                if (targetBounds.Any(targetBoundsItem => BoundsOverlapWithinClearance(
                    targetBoundsItem, candidateBounds, FabricationNeighborClearance)))
                {
                    candidates.Add(node);
                }
            }

            DiagLog($"제작도 연결 후보 선별: all={allBodyNodes.Count} target={targetNodes.Count} " +
                    $"aggregateHit={aggregateHits} candidates={candidates.Count} " +
                    $"clearance={FabricationNeighborClearance:F1}mm");
            return candidates;
        }

        private void ResetSilentClashSequence()
        {
            _silentClashPendingTestIds.Clear();
            _silentClashSequenceActive = false;
            _silentClashActiveTestId = -1;
            _silentClashSequenceTotal = 0;
            _silentClashCompletedCount = 0;
        }

        private bool StartSilentClashSequence(IEnumerable<int> testIds)
        {
            ResetSilentClashSequence();

            foreach (int testId in testIds.Where(id => id >= 0).Distinct())
                _silentClashPendingTestIds.Enqueue(testId);

            if (_silentClashPendingTestIds.Count == 0)
                return false;

            _silentClashSequenceTotal = _silentClashPendingTestIds.Count;
            _silentClashSequenceActive = true;

            if (StartNextSilentClashTest())
                return true;

            ResetSilentClashSequence();
            return false;
        }

        private bool StartNextSilentClashTest()
        {
            if (!_silentClashSequenceActive || _silentClashPendingTestIds.Count == 0)
                return false;

            int nextTestId = _silentClashPendingTestIds.Peek();
            _silentClashActiveTestId = nextTestId;
            bool started = vizcore3d.Clash.PerformInterferenceCheck(
                _silentClashActiveTestId,
                false);

            if (started)
                _silentClashPendingTestIds.Dequeue();
            else
                _silentClashActiveTestId = -1;

            DiagLog($"간섭검사 무창 실행: id={nextTestId} " +
                    $"started={started} completed={_silentClashCompletedCount}/" +
                    $"{_silentClashSequenceTotal} pending={_silentClashPendingTestIds.Count}");
            return started;
        }

        /// <summary>
        /// 완료 이벤트 콜백이 반환된 뒤 SDK Busy 해제를 기다리고 다음 검사를 시작한다.
        /// </summary>
        private async void StartNextSilentClashTestAfterEvent()
        {
            int nextTestId = _silentClashPendingTestIds.Count > 0
                ? _silentClashPendingTestIds.Peek()
                : -1;

            try
            {
                const int maxBusyWaitCount = 40;
                for (int attempt = 0; attempt < maxBusyWaitCount; attempt++)
                {
                    if (!_silentClashSequenceActive || _silentClashPendingTestIds.Count == 0)
                        return;

                    if (!vizcore3d.Clash.IsBusy && StartNextSilentClashTest())
                        return;

                    await System.Threading.Tasks.Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                DiagLog($"간섭검사 무창 후속 실행 예외: id={nextTestId} {ex.Message}");
            }

            HandleSilentClashStartFailure(nextTestId);
        }

        private void HandleSilentClashStartFailure(int nextTestId)
        {
            DiagLog($"간섭검사 무창 후속 시작 실패: id={nextTestId}");
            ResetSilentClashSequence();
            HideBusyOverlay();

            if (_p2aInProgress)
            {
                // STRU 일괄 경로의 초기 시작 실패 처리와 동일하게 최소 시트 생성을 시도한다.
                GenerateDrawingSheets();
            }
            else
            {
                MessageBox.Show(
                    "간섭검사 후속 항목을 시작하지 못했습니다. 다시 실행해주세요.",
                    "간섭검사",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 단일 검사 완료 이벤트마다 다음 ID를 시작하고, 마지막 완료 때만 기존 결과 처리로 진입한다.
        /// </summary>
        private bool AdvanceSilentClashSequence(int finishedTestId)
        {
            if (!_silentClashSequenceActive)
                return true;

            if (finishedTestId != _silentClashActiveTestId)
            {
                DiagLog($"간섭검사 무창 완료 이벤트 무시: expected={_silentClashActiveTestId} " +
                        $"actual={finishedTestId}");
                return false;
            }

            _silentClashCompletedCount++;
            if (_silentClashPendingTestIds.Count == 0)
            {
                DiagLog($"간섭검사 무창 전체 완료: {_silentClashCompletedCount}/" +
                        $"{_silentClashSequenceTotal}");
                _silentClashSequenceActive = false;
                _silentClashActiveTestId = -1;
                return true;
            }

            // 이벤트 콜백 안에서는 SDK IsBusy가 아직 true일 수 있으므로 UI 메시지 큐로 넘긴다.
            _silentClashActiveTestId = -1;
            BeginInvoke(new Action(StartNextSilentClashTestAfterEvent));
            return false;
        }

        /// <summary>
        /// Clash Detection 수행 (ClashManager API 사용)
        /// </summary>
        private bool DetectClash(bool includeOutsideNeighbors = false)
        {
            clashList.Clear();
            lvClash.Items.Clear();
            ClearFabricationNeighborResults();

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
                var registeredTestIds = new List<int>();

                for (int i = 0; i < targetNodes.Count; i++)
                {
                    for (int j = i + 1; j < targetNodes.Count; j++)
                    {
                        VIZCore3D.NET.Data.ClashTest pairClash = new VIZCore3D.NET.Data.ClashTest();
                        pairClash.Name = $"간섭검사_{targetNodes[i].NodeName}_vs_{targetNodes[j].NodeName}";
                        pairClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        pairClash.UseClearanceValue = true;
                        pairClash.ClearanceValue = 3.0f;  // T-063: 1→3 (2mm 떨어진 부재까지 안전 커버)
                        pairClash.UseRangeValue = true;
                        pairClash.RangeValue = 3.0f;      // T-063: Clearance와 동일 — Range < Clearance면 검사 자체 X
                        pairClash.UsePenetrationTolerance = true;
                        pairClash.PenetrationTolerance = 1.0f;
                        pairClash.VisibleOnly = false;
                        pairClash.BottomLevel = 0;
                        pairClash.GroupA = new List<VIZCore3D.NET.Data.Node> { targetNodes[i] };
                        pairClash.GroupB = new List<VIZCore3D.NET.Data.Node> { targetNodes[j] };

                        if (vizcore3d.Clash.Add(pairClash))
                        {
                            clashCount++;
                            registeredTestIds.Add(pairClash.ID);
                        }
                    }
                }

                // 제작도 ISO 점선용: Bounding Box로 근처 후보를 먼저 줄이고 대상 대 후보만 그룹 검사한다.
                // 기존 targetNodes 내부 pair 검사는 시트 연결성 계산용으로 유지하며,
                // 이 검사 결과는 전용 컬렉션에 저장해 기존 clashList와 섞지 않는다.
                if (includeOutsideNeighbors && targetNodes.Count > 0)
                {
                    var neighborCandidates = GetFabricationNeighborCandidates(allNodes, targetNodes);
                    if (neighborCandidates.Count > 0)
                    {
                        VIZCore3D.NET.Data.ClashTest outsideClash = new VIZCore3D.NET.Data.ClashTest();
                        outsideClash.Name = FabricationNeighborClashTestName;
                        outsideClash.TestKind = VIZCore3D.NET.Data.ClashTest.ClashTestKind.GROUP_VS_GROUP;
                        outsideClash.UseClearanceValue = true;
                        outsideClash.ClearanceValue = FabricationNeighborClearance;
                        outsideClash.UseRangeValue = true;
                        outsideClash.RangeValue = FabricationNeighborClearance;
                        outsideClash.UsePenetrationTolerance = true;
                        outsideClash.PenetrationTolerance = 1.0f;
                        outsideClash.VisibleOnly = false;
                        outsideClash.BottomLevel = 0;
                        outsideClash.GroupA = targetNodes;
                        outsideClash.GroupB = neighborCandidates;

                        if (vizcore3d.Clash.Add(outsideClash))
                        {
                            clashCount++;
                            registeredTestIds.Add(outsideClash.ID);
                            DiagLog($"제작도 연결 간섭검사 추가: target={targetNodes.Count} " +
                                    $"candidates={neighborCandidates.Count}");
                        }
                    }
                }

                if (clashCount == 0) return false;

                return StartSilentClashSequence(registeredTestIds);
            }
            catch (Exception ex)
            {
                ResetSilentClashSequence();
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
            // T-064 P2 본진 — P2a PoC에서 사용하던 _p2aInProgress 가드 제거.
            // 본진은 이 핸들러의 자동 시트 생성 흐름(CompleteMainDimensionPostClash → GenerateDrawingSheets)을
            // *활용*해야 하므로 차단하면 안 됨.
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Clash Finished] 이벤트 발생! ID: {e.ID}");

                // 진행창 없는 단일-ID 실행은 테스트마다 완료 이벤트가 온다.
                // 다음 테스트를 이어서 실행하고, 마지막 이벤트에서만 아래 전체 결과를 한 번 처리한다.
                if (!AdvanceSilentClashSequence(e.ID))
                    return;

                // ClashTest 개수 확인
                int testCount = vizcore3d.Clash.ClashTestCount;
                System.Diagnostics.Debug.WriteLine($"현재 등록된 ClashTest 개수: {testCount}");

                clashList.Clear();
                fabricationNeighborClashList.Clear();
                fabricationNeighborPartIndices.Clear();
                lvClash.Items.Clear();

                // 모든 ClashTest 결과 수집
                for (int i = 0; i < testCount; i++)
                {
                    VIZCore3D.NET.Data.ClashTest clashTest = vizcore3d.Clash.Items[i];

                    if (clashTest == null) continue;
                    bool isFabricationNeighborTest =
                        string.Equals(clashTest.Name, FabricationNeighborClashTestName,
                            StringComparison.Ordinal);

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

                            // 간섭 위치 — 제작도 연결 부재 이름을 월드 좌표로 투영할 수 있도록 XYZ 전체를 보존한다.
                            if (result.HotPoint != null)
                            {
                                clash.XValue = result.HotPoint.X;
                                clash.YValue = result.HotPoint.Y;
                                clash.ZValue = result.HotPoint.Z;
                                clash.HasHotPoint = true;
                            }

                            List<ClashData> destination = isFabricationNeighborTest
                                ? fabricationNeighborClashList
                                : clashList;

                            // 중복 검사 (A-B와 B-A 동일 처리)
                            bool isDuplicate = destination.Any(c =>
                                (c.Index1 == clash.Index1 && c.Index2 == clash.Index2) ||
                                (c.Index1 == clash.Index2 && c.Index2 == clash.Index1));

                            if (!isDuplicate)
                            {
                                destination.Add(clash);
                            }

                            if (isFabricationNeighborTest)
                            {
                                bool firstIsTarget = fabricationTargetPartIndices.Contains(clash.Index1);
                                bool secondIsTarget = fabricationTargetPartIndices.Contains(clash.Index2);
                                if (firstIsTarget && !secondIsTarget)
                                    fabricationNeighborPartIndices.Add(clash.Index2);
                                else if (secondIsTarget && !firstIsTarget)
                                    fabricationNeighborPartIndices.Add(clash.Index1);
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

                if (fabricationNeighborClashList.Count > 0)
                {
                    fabricationNeighborClashList.Sort((a, b) => b.ZValue.CompareTo(a.ZValue));
                    foreach (var clash in fabricationNeighborClashList)
                    {
                        ListViewItem lvi = new ListViewItem("[연결] " + clash.Name1);
                        lvi.SubItems.Add(clash.Name2);
                        lvi.SubItems.Add(clash.ZValue.ToString("F2"));
                        lvi.Tag = clash;
                        lvClash.Items.Add(lvi);
                    }
                }

                string neighborNames = string.Join(", ", fabricationNeighborClashList
                    .Select(c => fabricationTargetPartIndices.Contains(c.Index1) ? c.Name2 : c.Name1)
                    .Distinct()
                    .Take(50));
                DiagLog($"제작도 연결 간섭검사 결과: raw={fabricationNeighborClashList.Count} " +
                        $"parts={fabricationNeighborPartIndices.Count}" +
                        (string.IsNullOrEmpty(neighborNames) ? "" : $" names=[{neighborNames}]"));

                // T-023 v3: 연결성 판정 — bomList의 부재들이 Clash 인접 그래프 기준
                // "한 덩어리(연결 성분 1개)"인가? 떨어진 부재가 하나라도 있으면 차단.
                // 이 판정이 통과해야만 Osnap/치수/요약/시트 생성으로 이어진다.
                int componentCount;
                if (!IsSingleConnectedComponent(out componentCount))
                {
                    HideBusyOverlay();
                    // T-064 P2 본진 진행 중엔 모달 차단 — 사용자 액션 대기로 흐름 정지·비결정 동작 방지.
                    if (!_p2aInProgress)
                    {
                        MessageBox.Show(
                            "치수 추출은 모든 부재가 **하나의 덩어리로 연결**되어 있을 때만 가능합니다.\n\n" +
                            $"현재: 서로 연결되지 않은 부재 그룹 {componentCount}개 발견 (Clash 인접 기준)\n\n" +
                            "해결 방법:\n" +
                            "- 떨어진 부재를 모델트리 체크박스로 숨기기\n" +
                            "- 한 덩어리만 남기고 다시 치수 추출",
                            "치수 추출 사전조건", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    DiagLog($"btnMainDimension BLOCKED components={componentCount} (T-023 v3, p2aInProgress={_p2aInProgress})");
                    return;
                }

                // 연결성 통과 → Osnap 수집 → 치수 계산 → 요약 → 시트 생성
                // 오버레이 해제는 CompleteMainDimensionPostClash의 finally에서 수행
                CompleteMainDimensionPostClash(isSingleMember: false, clashTestCount: testCount);
            }
            catch (Exception ex)
            {
                ResetSilentClashSequence();
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
