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
        private bool _suppressSheetSelectionHandler;

        #region 도면 시트 생성 (BFS)

        /// <summary>
        /// Clash 인접 리스트 기반 BFS로 도면 시트 생성
        /// </summary>
        private void GenerateDrawingSheets()
        {
            List<ChainDimensionData> initiallyComputedDimensions = chainDimensionList.ToList();
            drawingSheetList.Clear();
            lvDrawingSheet.Items.Clear();

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sheet 1: 전체 BOM 부재
            DrawingSheetData sheet1 = CreateFullDrawingSheetData();
            drawingSheetList.Add(sheet1);

            // BOM 인덱스 → BOM 이름 매핑
            Dictionary<int, string> bomIndexToName = new Dictionary<int, string>();
            HashSet<int> bomIndexSet = new HashSet<int>();
            foreach (var bom in bomList)
            {
                bomIndexToName[bom.Index] = bom.Name;
                bomIndexSet.Add(bom.Index);
            }

            // Part Index → Body Index 리스트 (역매핑)
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

            // Clash 인접 리스트 구축 (Part → Body 변환하여 Body 기반 매칭)
            Dictionary<int, HashSet<int>> adjacencyByIndex = new Dictionary<int, HashSet<int>>();
            foreach (var clash in clashList)
            {
                // Clash.Index1/Index2는 Part 인덱스 → Body 인덱스로 변환
                List<int> bodies1 = partToBodyIndices.ContainsKey(clash.Index1) ? partToBodyIndices[clash.Index1] : new List<int>();
                List<int> bodies2 = partToBodyIndices.ContainsKey(clash.Index2) ? partToBodyIndices[clash.Index2] : new List<int>();

                // 두 Part에 속한 모든 Body들 간에 연결 추가
                foreach (int bodyIdx1 in bodies1)
                {
                    foreach (int bodyIdx2 in bodies2)
                    {
                        if (bodyIdx1 == bodyIdx2) continue;

                        if (!adjacencyByIndex.ContainsKey(bodyIdx1))
                            adjacencyByIndex[bodyIdx1] = new HashSet<int>();
                        if (!adjacencyByIndex.ContainsKey(bodyIdx2))
                            adjacencyByIndex[bodyIdx2] = new HashSet<int>();

                        adjacencyByIndex[bodyIdx1].Add(bodyIdx2);
                        adjacencyByIndex[bodyIdx2].Add(bodyIdx1);
                    }
                }
            }

            // Sheet 2~: BOM의 **모든 부재가 각자 기준부재**가 되어 1-hop 이웃 시트를 생성한다.
            // (T-015, 2026-04-21): 이전에는 `appearedAsIncluded`로 "이미 다른 시트의 포함부재로 등장한
            // 부재는 기준부재로 못 쓰게" 막았으나, 사용자 의도는 "모든 부재가 자기 기준 시트를 가져야 함".
            // 1-2-3-4가 연쇄 Clash면 Sheet 2(기준 1), Sheet 3(기준 2), Sheet 4(기준 3), Sheet 5(기준 4) 4개 생성.
            // 단계 7의 Sheet 1 중복 제거는 그대로 유지되어 과잉 시트는 자동 정리된다.
            int sheetNumber = 2;

            foreach (var bom in bomList)
            {
                DrawingSheetData sheet = new DrawingSheetData();
                sheet.SheetNumber = sheetNumber;
                sheet.BaseMemberIndex = bom.Index;
                sheet.BaseMemberName = bom.Name;

                // 포함부재: 기준부재 자신
                sheet.MemberIndices.Add(bom.Index);
                sheet.MemberNames.Add(bom.Name);

                // 포함부재: Clash에서 기준부재와 연결된 모든 부재 (Index 기반, 1-hop)
                if (adjacencyByIndex.ContainsKey(bom.Index))
                {
                    foreach (int neighborIndex in adjacencyByIndex[bom.Index])
                    {
                        // 같은 시트 내 중복만 방지
                        if (!sheet.MemberIndices.Contains(neighborIndex))
                        {
                            sheet.MemberIndices.Add(neighborIndex);
                            if (bomIndexToName.ContainsKey(neighborIndex))
                                sheet.MemberNames.Add(bomIndexToName[neighborIndex]);
                        }
                    }
                }

                drawingSheetList.Add(sheet);
                sheetNumber++;
            }

            // 마지막 시트: 설치도 (모든 연결된 부재를 BFS로 탐색 - Index 기반)
            HashSet<int> installMemberIndices = new HashSet<int>();
            Queue<int> bfsQueue = new Queue<int>();

            // 첫 번째 BOM 부재부터 BFS 시작
            if (bomList.Count > 0 && adjacencyByIndex.Count > 0)
            {
                int startIndex = bomList[0].Index;
                bfsQueue.Enqueue(startIndex);
                installMemberIndices.Add(startIndex);

                while (bfsQueue.Count > 0)
                {
                    int current = bfsQueue.Dequeue();
                    if (adjacencyByIndex.ContainsKey(current))
                    {
                        foreach (int neighbor in adjacencyByIndex[current])
                        {
                            if (!installMemberIndices.Contains(neighbor))
                            {
                                installMemberIndices.Add(neighbor);
                                bfsQueue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            // BFS에 포함되지 않은 독립 부재도 추가 (Clash가 없는 부재)
            foreach (var bom in bomList)
            {
                installMemberIndices.Add(bom.Index);
            }

            DrawingSheetData installSheet = new DrawingSheetData();
            installSheet.SheetNumber = sheetNumber;
            installSheet.BaseMemberName = "설치도";
            installSheet.BaseMemberIndex = -2; // 설치도 식별자
            foreach (var bom in bomList)
            {
                if (installMemberIndices.Contains(bom.Index))
                {
                    installSheet.MemberIndices.Add(bom.Index);
                    installSheet.MemberNames.Add(bom.Name);
                }
            }
            PrepareInstallationConnectionData(installSheet);
            drawingSheetList.Add(installSheet);
            sheetNumber++;

            // 가공도 시트: BOM 부재를 한 줄씩 추가
            int mfgNo = 1;
            foreach (var bom in bomList)
            {
                DrawingSheetData mfgSheet = new DrawingSheetData();
                mfgSheet.SheetNumber = sheetNumber;
                mfgSheet.BaseMemberName = bom.Name;
                mfgSheet.BaseMemberIndex = -3; // 가공도 식별자
                mfgSheet.MemberIndices.Add(bom.Index);
                mfgSheet.MemberNames.Clear(); // 포함부재 비우기
                mfgSheet.MfgDrawingNo = mfgNo; // 가공도 번호
                drawingSheetList.Add(mfgSheet);
                sheetNumber++;
                mfgNo++;
            }

            // T-053 v2: 모든 시트 쌍에서 부재 구성 동일 시 중복 제거 (사용자 결정).
            // "포함부재가 같으면 기준부재가 달라도 같은 형상이다" — Sheet 2와 Sheet 5의 MemberIndices가
            // 동일하면 둘 다 같은 도면이므로 뒤쪽(Sheet 5)을 자동 제거. 첫 등장 순서 보존.
            // 단 Sheet 1(-1) / 설치도(-2) / 가공도(-3)는 의미상 별도 시트라 중복 검사 대상에서 제외 후 보존.
            // (Sheet 1 = 전체 도면 안내, 설치도 = 설치 가이드, 가공도 = 단일 부재 가공도)
            if (drawingSheetList.Count > 1)
            {
                var seenMemberKey = new HashSet<string>();
                drawingSheetList.RemoveAll(s =>
                {
                    // 의미가 다른 시트는 보존
                    if (s.BaseMemberIndex < 0) return false;
                    string memberKey = string.Join(",", s.MemberIndices.OrderBy(x => x));
                    if (seenMemberKey.Contains(memberKey)) return true;
                    seenMemberKey.Add(memberKey);
                    return false;
                });

                // 일반 시트들 사이뿐 아니라 Sheet 1과 동일 구성인 일반 시트도 제거 대상.
                // (Sheet 1은 위 RemoveAll에서 BaseMemberIndex==-1로 검사 제외되어 항상 보존됨)
                HashSet<int> sheet1Members = new HashSet<int>(drawingSheetList[0].MemberIndices);
                drawingSheetList.RemoveAll(s =>
                    s.BaseMemberIndex >= 0 &&
                    s.MemberIndices.Count == sheet1Members.Count &&
                    new HashSet<int>(s.MemberIndices).SetEquals(sheet1Members));
            }

            // T-053: 중복 시트 제거 후 SheetNumber 전체 재채번 (1부터 순차).
            // Sheet 1(-1) → 일반 시트(>=0) → 설치도(-2) → 가공도(-3) 순서는 보존되며 번호만 정합 유지.
            // 가공도 sheetLabel은 MfgDrawingNo를 사용하므로 표시 영향 없음 (데이터 일관성 목적).
            for (int i = 0; i < drawingSheetList.Count; i++)
            {
                drawingSheetList[i].SheetNumber = i + 1;
            }

            // BOM 인덱스 → item 번호 매핑 (bomList 순서 = ISO 풍선 번호 = BOM 정보 탭 No.)
            // T-014: 기준부재/포함부재 컬럼을 부재 이름 대신 item 번호로 표시
            Dictionary<int, int> bomIndexToItemNo = new Dictionary<int, int>();
            for (int i = 0; i < bomList.Count; i++)
            {
                bomIndexToItemNo[bomList[i].Index] = i + 1;
            }

            // 도면 리스트를 사용자에게 보여주기 전에 모든 일반/설치 시트의 치수와 모든 시트의 BOM을 준비한다.
            // 이후 시트 클릭은 SDK 재조회·치수 재계산 없이 캐시를 UI에 적용한다.
            ShowBusyOverlay("도면 조회 데이터 준비 중...");
            PrepareDrawingSheetDimensionCaches(initiallyComputedDimensions);
            PrepareDrawingSheetBomCaches();

            if (drawingSheetList.Count > 0 && drawingSheetList[0].DimensionsPrepared)
                ApplyPreparedDimensionsToUi(drawingSheetList[0]);

            // ListView 갱신
            foreach (var sheet in drawingSheetList)
            {
                string sheetLabel;
                if (sheet.BaseMemberIndex == -3) // 가공도
                    sheetLabel = $"가공도_{sheet.MfgDrawingNo}";
                else
                    sheetLabel = $"Sheet {sheet.SheetNumber}";

                // 기준부재 표시
                // T-014: item 번호로 표시. T-042 부분 적용 (2026-05-04): 일반/가공도 시트는
                // "1 (BOM이름)" 포맷으로 BOM 이름 병기. Sheet 1·설치도는 의미가 다른 시트라 그대로.
                string baseText;
                if (sheet.BaseMemberIndex == -1)        // Sheet 1
                {
                    baseText = "전체";
                }
                else if (sheet.BaseMemberIndex == -2)   // 설치도
                {
                    baseText = "설치도";
                }
                else if (sheet.BaseMemberIndex == -3)   // 가공도: 실제 기준은 MemberIndices[0]
                {
                    int mfgBomIdx = sheet.MemberIndices.Count > 0 ? sheet.MemberIndices[0] : -1;
                    int mfgItemNo;
                    baseText = bomIndexToItemNo.TryGetValue(mfgBomIdx, out mfgItemNo)
                        ? $"{mfgItemNo} ({sheet.BaseMemberName})"
                        : sheet.BaseMemberName;
                }
                else                                    // Sheet 2+ (개별 기준부재)
                {
                    int baseNo;
                    baseText = bomIndexToItemNo.TryGetValue(sheet.BaseMemberIndex, out baseNo)
                        ? $"{baseNo} ({sheet.BaseMemberName})"
                        : sheet.BaseMemberName;
                }

                // 포함부재 표시 (T-014: item 번호 오름차순, 가공도는 빈칸)
                // T-052: Sheet 1도 "전체" → "1, 2, 3, ..." 명시. 일반 시트와 동일 로직 사용
                string includedText;
                if (sheet.BaseMemberIndex == -3)        // 가공도
                {
                    includedText = "";
                }
                else
                {
                    // Sheet 1(-1) / 설치도(-2) / 일반(>=0) 모두 동일 처리
                    List<int> nums = new List<int>();
                    foreach (int idx in sheet.MemberIndices)
                    {
                        int n;
                        if (bomIndexToItemNo.TryGetValue(idx, out n))
                            nums.Add(n);
                    }
                    nums.Sort();
                    includedText = string.Join(", ", nums);
                }

                ListViewItem lvi = new ListViewItem(sheetLabel);
                lvi.SubItems.Add(baseText);
                lvi.SubItems.Add(includedText);

                // 가공도 앵글: 부재수 컬럼에 오른쪽→왼쪽 측면뷰 사등분 Osnap 정보 표시
                // 사분면번호(1분면합계,2분면합계,3분면합계,4분면합계) — 1:왼위 2:우위 3:우하 4:좌하
                string countText = sheet.MemberIndices.Count.ToString();
                if (sheet.BaseMemberIndex == -3 && sheet.MemberIndices.Count > 0)
                {
                    int bomIdx = sheet.MemberIndices[0];
                    BOMData bom = bomList.FirstOrDefault(b => b.Index == bomIdx);
                    if (bom != null && IsAngleFromSpref(bomIdx))
                    {
                        // 장축 로직으로 viewDirection 결정 (ExecuteMfgDrawing과 동일)
                        float sizeX = bom.MaxX - bom.MinX;
                        float sizeY = bom.MaxY - bom.MinY;
                        float sizeZ = bom.MaxZ - bom.MinZ;

                        string longestAxis;
                        if (sizeX >= sizeY && sizeX >= sizeZ) longestAxis = "X";
                        else if (sizeY >= sizeX && sizeY >= sizeZ) longestAxis = "Y";
                        else longestAxis = "Z";

                        string viewDir;
                        switch (longestAxis)
                        {
                            case "Y": viewDir = "X"; break;
                            default: viewDir = "Y"; break;
                        }

                        // 기존뷰 카메라 방향(useMinus/use180) 결정 — MfgDrawing과 동일 로직
                        // 기존뷰 화면축: viewDir X → 화면 Y,Z / viewDir Y → 화면 X,Z
                        float bbCH = 0f, bbCV = 0f, sumFH = 0f, sumFV = 0f;
                        int osnapCount = 0;
                        var osnapList = vizcore3d.Object3D.GetOsnapPoint(bomIdx);
                        var allPts = new List<float[]>();
                        if (osnapList != null)
                        {
                            foreach (var osnap in osnapList)
                            {
                                switch (osnap.Kind)
                                {
                                    case VIZCore3D.NET.Data.OsnapKind.LINE:
                                        if (osnap.Start != null) allPts.Add(new float[] { osnap.Start.X, osnap.Start.Y, osnap.Start.Z });
                                        if (osnap.End != null) allPts.Add(new float[] { osnap.End.X, osnap.End.Y, osnap.End.Z });
                                        break;
                                    case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    case VIZCore3D.NET.Data.OsnapKind.POINT:
                                        if (osnap.Center != null) allPts.Add(new float[] { osnap.Center.X, osnap.Center.Y, osnap.Center.Z });
                                        break;
                                }
                            }
                        }
                        osnapCount = allPts.Count;

                        // 기존뷰 화면축 기준 centroid/BB center로 useMinus/use180 계산
                        foreach (var p in allPts)
                        {
                            switch (viewDir)
                            {
                                case "X": sumFH += p[1]; sumFV += p[2]; break; // front: H=Y, V=Z
                                case "Y": sumFH += p[0]; sumFV += p[2]; break; // front: H=X, V=Z
                                default:  sumFH += p[0]; sumFV += p[1]; break;
                            }
                        }
                        switch (viewDir)
                        {
                            case "X": bbCH = (bom.MinY + bom.MaxY) / 2f; bbCV = (bom.MinZ + bom.MaxZ) / 2f; break;
                            case "Y": bbCH = (bom.MinX + bom.MaxX) / 2f; bbCV = (bom.MinZ + bom.MaxZ) / 2f; break;
                            default:  bbCH = (bom.MinX + bom.MaxX) / 2f; bbCV = (bom.MinY + bom.MaxY) / 2f; break;
                        }

                        bool hFlip = false, vFlip = false;
                        if (osnapCount > 0)
                        {
                            float centFH = sumFH / osnapCount;
                            float centFV = sumFV / osnapCount;
                            float openH = bbCH - centFH;
                            float openV = bbCV - centFV;
                            bool use180 = (openV > 0);
                            bool useMinus;
                            if (viewDir == "Y")
                            {
                                bool needRight = use180 ? (openH < 0) : (openH > 0);
                                useMinus = !needRight;
                            }
                            else
                            {
                                bool needRight = use180 ? (openH > 0) : (openH < 0);
                                useMinus = !needRight;
                            }
                            // 측면뷰 좌표 보정
                            // viewDir=X: 기본 측면(Y방향)의 screen right=-X → base H반전
                            // viewDir=Y: 기본 측면(X방향)의 screen right=+Y → base H정상
                            // useMinus가 측면 방향을 뒤집어 H를 토글, use180이 V를 반전
                            hFlip = (viewDir == "X") != useMinus;
                            vFlip = use180;
                        }

                        // 오른쪽 측면에서 봤을때 사분면별 Osnap 개수로 bend 위치 판별
                        // 1=좌상 2=우상 3=우하 4=좌하
                        float hCenter = 0f, vCenter = 0f;
                        switch (viewDir)
                        {
                            case "X": hCenter = (bom.MinX + bom.MaxX) / 2f; vCenter = (bom.MinZ + bom.MaxZ) / 2f; break;
                            case "Y": hCenter = (bom.MinY + bom.MaxY) / 2f; vCenter = (bom.MinZ + bom.MaxZ) / 2f; break;
                            default:  hCenter = (bom.MinX + bom.MaxX) / 2f; vCenter = (bom.MinY + bom.MaxY) / 2f; break;
                        }

                        int sq1 = 0, sq2 = 0, sq3 = 0, sq4 = 0;
                        foreach (var p in allPts)
                        {
                            float h, v;
                            switch (viewDir)
                            {
                                case "X": h = p[0]; v = p[2]; break; // side: H=X, V=Z
                                case "Y": h = p[1]; v = p[2]; break; // side: H=Y, V=Z
                                default:  h = p[0]; v = p[1]; break;
                            }
                            // 카메라 방향에 따른 좌표 보정
                            if (hFlip) h = 2 * hCenter - h;
                            if (vFlip) v = 2 * vCenter - v;

                            // 사분면 분류
                            if (h < hCenter && v >= vCenter) sq1++;       // 좌상
                            else if (h >= hCenter && v >= vCenter) sq2++;  // 우상
                            else if (h >= hCenter && v < vCenter) sq3++;   // 우하
                            else sq4++;                                     // 좌하
                        }

                        if (osnapCount > 0)
                        {
                            int total = sq1 + sq2 + sq3 + sq4;

                            // Osnap이 가장 적은 사분면 = 열린 방향(개구부)
                            int minVal = Math.Min(Math.Min(sq1, sq2), Math.Min(sq3, sq4));
                            int openQ;
                            if (sq3 == minVal) openQ = 3;       // 우하 우선 (정상 목표)
                            else if (sq4 == minVal) openQ = 4;
                            else if (sq2 == minVal) openQ = 2;
                            else openQ = 1;

                            // bend = 열린 방향의 대각 반대
                            int bendQ;
                            switch (openQ)
                            {
                                case 1: bendQ = 3; break;
                                case 2: bendQ = 4; break;
                                case 3: bendQ = 1; break;
                                default: bendQ = 2; break;
                            }

                            countText = $"Q{bendQ}({sq1},{sq2},{sq3},{sq4})={total}";
                        }
                        else
                        {
                            countText = $"Q?";
                        }
                    }
                }

                lvi.SubItems.Add(countText);
                lvi.Tag = sheet;
                lvDrawingSheet.Items.Add(lvi);
            }
        }

        private DrawingSheetData CreateFullDrawingSheetData()
        {
            DrawingSheetData sheet = new DrawingSheetData
            {
                SheetNumber = 1,
                BaseMemberIndex = -1
            };

            // 선택한 노드 이름 사용, 없으면 파일명 사용
            if (selectedAttributeNodeIndex != -1)
            {
                var selectedNode = vizcore3d.Object3D.FromIndex(selectedAttributeNodeIndex);
                sheet.BaseMemberName = (selectedNode != null && !string.IsNullOrEmpty(selectedNode.NodeName))
                    ? selectedNode.NodeName
                    : Path.GetFileNameWithoutExtension(currentFilePath);
            }
            else
            {
                sheet.BaseMemberName = !string.IsNullOrEmpty(currentFilePath)
                    ? Path.GetFileNameWithoutExtension(currentFilePath)
                    : "전체";
            }

            foreach (var bom in bomList)
            {
                sheet.MemberIndices.Add(bom.Index);
                sheet.MemberNames.Add(bom.Name);
            }

            return sheet;
        }

        private void PrepareDrawingSheetDimensionCaches(List<ChainDimensionData> initiallyComputedDimensions)
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var dimensionsByMemberSet = new Dictionary<string, List<ChainDimensionData>>();

            foreach (DrawingSheetData sheet in drawingSheetList)
            {
                // 가공도는 부재별 3D 장면·카메라·풍선을 만들어야 하므로 일반 체인 치수 캐시 대상이 아니다.
                if (sheet.BaseMemberIndex == -3)
                    continue;

                var swSheet = System.Diagnostics.Stopwatch.StartNew();
                string engine = sheet.BaseMemberIndex == -2 ? "INSTALL" : "OSNAP";
                string memberKey = engine + "|" + string.Join(",", sheet.MemberIndices.OrderBy(x => x));
                List<ChainDimensionData> prepared;
                bool reused = dimensionsByMemberSet.TryGetValue(memberKey, out prepared);
                bool reusedInitialResult = false;

                if (!reused)
                {
                    if (sheet.BaseMemberIndex == -1 && initiallyComputedDimensions != null &&
                        (initiallyComputedDimensions.Count > 0 || _autoProcessOsnapSuccess))
                    {
                        prepared = initiallyComputedDimensions.ToList();
                        reusedInitialResult = true;
                    }
                    else if (sheet.BaseMemberIndex == -2)
                    {
                        prepared = ComputeInstallationDimensions(sheet);
                    }
                    else
                    {
                        prepared = ComputeViewDimensionsForMembers(
                            sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap);
                    }
                    dimensionsByMemberSet[memberKey] = prepared;
                }

                sheet.PreparedDimensions.Clear();
                sheet.PreparedDimensions.AddRange(prepared);
                for (int i = 0; i < sheet.PreparedDimensions.Count; i++)
                    sheet.PreparedDimensions[i].No = i + 1;
                sheet.DimensionsPrepared = true;

                swSheet.Stop();
                DiagLog($"도면 시트 치수 사전 준비: sheet#={sheet.SheetNumber} " +
                    $"members={sheet.MemberIndices.Count} chain={sheet.PreparedDimensions.Count} " +
                    $"reused={reused || reusedInitialResult} elapsed={swSheet.ElapsedMilliseconds}ms");
            }

            swTotal.Stop();
            DiagLog($"도면 시트 치수 사전 준비 완료: sheets={drawingSheetList.Count} " +
                $"cacheSets={dimensionsByMemberSet.Count} elapsed={swTotal.ElapsedMilliseconds}ms");
        }

        private void ApplyPreparedDimensionsToUi(DrawingSheetData sheet)
        {
            if (sheet == null || !sheet.DimensionsPrepared) return;

            chainDimensionList.Clear();
            chainDimensionList.AddRange(sheet.PreparedDimensions);

            lvDimension.BeginUpdate();
            try
            {
                lvDimension.Items.Clear();
                foreach (ChainDimensionData dim in chainDimensionList)
                {
                    ListViewItem item = new ListViewItem(dim.No.ToString());
                    item.SubItems.Add(dim.Axis);
                    item.SubItems.Add(dim.ViewName);
                    item.SubItems.Add(((int)Math.Round(dim.Distance)).ToString());
                    item.SubItems.Add(dim.StartPointStr);
                    item.SubItems.Add(dim.EndPointStr);
                    item.Tag = dim;
                    lvDimension.Items.Add(item);
                }
            }
            finally
            {
                lvDimension.EndUpdate();
            }
        }

        /// <summary>
        /// 2D 출력용 치수 원본을 시트 유형에 맞게 반환한다.
        /// 설치도는 사전 준비한 전용 끝단→모서리 위치 치수를 유지하고,
        /// 일반 제작·조립도만 공용 Osnap 치수 엔진을 사용한다.
        /// </summary>
        private List<ChainDimensionData> GetDrawingSheetDimensionsFor2D(
            DrawingSheetData sheet, DrawingReferenceFrame drawingReferenceFrame = null)
        {
            if (sheet == null) return new List<ChainDimensionData>();
            if (sheet.BaseMemberIndex != -2)
            {
                return ComputeViewDimensionsForMembers(
                    sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap,
                    drawingReferenceFrame);
            }

            if (!sheet.DimensionsPrepared)
            {
                sheet.PreparedDimensions.Clear();
                sheet.PreparedDimensions.AddRange(ComputeInstallationDimensions(sheet));
                for (int i = 0; i < sheet.PreparedDimensions.Count; i++)
                    sheet.PreparedDimensions[i].No = i + 1;
                sheet.DimensionsPrepared = true;
                DiagLog($"설치도 2D 치수 fallback 계산: sheet#={sheet.SheetNumber} " +
                        $"chain={sheet.PreparedDimensions.Count}");
            }
            return sheet.PreparedDimensions.ToList();
        }

        /// <summary>
        /// 도면 시트 선택 시 X-Ray + 치수 표시 (UI 이벤트 → ApplySheetSelection 위임)
        /// </summary>
        private void LvDrawingSheet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSheetSelectionHandler)
            {
                DiagLog("LvDrawingSheet_SelectedIndexChanged SKIP (programmatic selection)");
                return;
            }

            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                // [T-016 진단 로그] 빈 선택 (이벤트 두 번 발생 패턴)
                DiagLog("LvDrawingSheet_SelectedIndexChanged SKIP (no selection)");
                return;
            }
            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            ApplySheetSelection(sheet);
        }

        /// <summary>
        /// 도면 시트 적용 본체 — Step A (2026-05-19): 수동 클릭(LvDrawingSheet_SelectedIndexChanged)과
        /// 자동 일괄 출력(ProcessSingleStruFull) 양쪽이 공통으로 호출하는 단일 진입점.
        /// 자동 경로의 lvi.Selected=true + Thread.Sleep(200) UI 트릭 제거를 위해 추출.
        /// 가시성·XRay·SilhouetteEdge·하이라이트·치수 분기·BOM 수집·카메라 회전 보존 모두 포함.
        /// </summary>
        public void ApplySheetSelection(DrawingSheetData sheet)
        {
            if (sheet == null || sheet.MemberIndices.Count == 0)
            {
                // [T-016 진단 로그] 무효 시트
                DiagLog($"LvDrawingSheet_SelectedIndexChanged SKIP (sheet null or empty)");
                return;
            }

            // [T-016 진단 로그] 진입
            DiagLog($"LvDrawingSheet_SelectedIndexChanged ENTER " +
                $"sheet#={sheet.SheetNumber} members={sheet.MemberIndices.Count} " +
                $"prevXray={xraySelectedNodeIndices?.Count ?? 0} prevChain={chainDimensionList?.Count ?? 0}");

            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swScene = System.Diagnostics.Stopwatch.StartNew();
            long sceneMs = 0;
            long dimensionMs = 0;
            long bomMs = 0;

            try
            {
                // 가공도 3D 미리보기가 유지 중인 참조축·화면 roll을 다른 시트 카메라에 넘기지 않는다.
                if (sheet.BaseMemberIndex != -3)
                    ResetMfgPreviewViewState("ApplySheetSelection/non-mfg");

                vizcore3d.BeginUpdate();
                vizcore3d.View.EnableAnimation = false;

                // X-Ray 모드 비활성화 (관련 부재만 완전히 표시하기 위해)
                if (vizcore3d.View.XRay.Enable)
                {
                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Enable = false;
                }

                // 이전 설치도에서 표시한 외부 연결 Part까지 포함해 장면을 완전히 초기화한다.
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);

                // 설치도는 선택 STRU와 직접 연결된 외부 Part까지 함께 표시한다.
                List<int> displayIndices = GetDrawingSheetDisplayIndices(sheet);
                vizcore3d.Object3D.Show(displayIndices, true);

                // 모서리(SilhouetteEdge) 표시
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 선택된 노드 인덱스 저장 (글로벌 뷰 버튼용)
                xraySelectedNodeIndices = displayIndices;

                // T-036 (2026-04-23): 가공도 시트는 ExecuteMfgDrawing이 자체 MoveCamera(X_PLUS/Y_PLUS/Z_PLUS)로
                // 카메라를 정면 뷰로 세팅하기 때문에, 여기서 FlyToObject3d를 먼저 호출하면 이전 ISO_PLUS 등의
                // 카메라 방향이 잔존한 상태로 화면 이동만 되어 "45도 대각 ISO 뷰 느낌"이 남음.
                // 가공도일 때 FlyToObject3d 스킵 → ExecuteMfgDrawing의 카메라/FitToView에 맡김.
                if (sheet.BaseMemberIndex != -3)
                {
                    vizcore3d.View.FlyToObject3d(displayIndices, 1.2f);
                }

                // 이전 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // 기존 풍선(Note) 제거
                vizcore3d.Review.Note.Clear();

                // T-022: 기준부재를 "선택상태"(빨간색 하이라이트)로 설정
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                int highlightIdx = -1;
                if (sheet.BaseMemberIndex == -3 && sheet.MemberIndices.Count > 0)
                    highlightIdx = sheet.MemberIndices[0];    // 가공도: 단일 부재
                else if (sheet.BaseMemberIndex >= 0)
                    highlightIdx = sheet.BaseMemberIndex;     // 일반 시트: 기준부재
                // Sheet 1(-1) · 설치도(-2)는 기준부재 개념이 없어 하이라이트 생략
                if (highlightIdx >= 0)
                    vizcore3d.Object3D.Select(new List<int> { highlightIdx }, true, false);

                vizcore3d.EndUpdate();
                swScene.Stop();
                sceneMs = swScene.ElapsedMilliseconds;

                // T-028: 시트 유형별 치수 분기
                //   가공도(-3): ExecuteMfgDrawing (기존 유지 — 단일 부재 가공도)
                //   설치도(-2): 선택 STRU 범위 + STRU측 Body 끝단→외부 연결 Body 접합측 모서리 치수
                //   그 외(Sheet 1, Sheet 2+): ComputeViewDimensionsForMembers (Osnap 기반, 2D 출력과 동일 엔진)
                var swDimension = System.Diagnostics.Stopwatch.StartNew();
                if (sheet.BaseMemberIndex == -3)
                {
                    ExecuteMfgDrawing(sheet.MemberIndices[0]);
                }
                else if (sheet.BaseMemberIndex == -2)
                {
                    if (sheet.DimensionsPrepared)
                        ApplyPreparedDimensionsToUi(sheet);
                    else
                        ExtractInstallationDimensions(sheet);
                }
                else
                {
                    if (sheet.DimensionsPrepared)
                    {
                        ApplyPreparedDimensionsToUi(sheet);
                    }
                    else
                    {
                        sheet.PreparedDimensions.Clear();
                        sheet.PreparedDimensions.AddRange(
                            ComputeViewDimensionsForMembers(sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap));
                        for (int i = 0; i < sheet.PreparedDimensions.Count; i++)
                            sheet.PreparedDimensions[i].No = i + 1;
                        sheet.DimensionsPrepared = true;
                        ApplyPreparedDimensionsToUi(sheet);
                    }

                    // T-030: 시트 선택 시 3D 뷰 치수 렌더링 제거 (T-029 정책 확장)
                    // chainDimensionList·lvDimension은 채우지만 ShowAllDimensions()는 호출하지 않음.
                    // 사용자가 글로벌 X/Y/Z 뷰 버튼을 눌러야 해당 뷰 치수가 3D 뷰에 등장.
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();

                    DiagLog($"T-030 시트 선택 자동 치수: sheet#={sheet.SheetNumber} members={sheet.MemberIndices.Count} chain={chainDimensionList.Count} (3D 미렌더)");
                }
                swDimension.Stop();
                dimensionMs = swDimension.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                // [T-016 진단 로그] silent catch 강화 (stack trace 포함)
                DiagLog($"LvDrawingSheet_SelectedIndexChanged FAIL " +
                    $"{ex.Message}\n{ex.StackTrace}");
            }

            var swBom = System.Diagnostics.Stopwatch.StartNew();
            if (sheet.BomPrepared)
                ApplyPreparedBomInfo(sheet);
            else
                CollectBOMInfo(false, sheet);
            swBom.Stop();
            bomMs = swBom.ElapsedMilliseconds;

            // (제거 2026-07-22) 가공도 시트 카메라 회전 재적용 블록 — 이중 적용 버그였음.
            //   ExecuteMfgDrawing이 이미 Z90/R180을 한 번 걸어두는데, 여기서 같은 회전을 또 걸어(상대 회전이라)
            //   Z-최장축·EA 부재가 두 배(90→180, 180→360)로 돌아갔음. 옛 SetCameraData(회전 리셋)와 짝이던
            //   재적용만 남아 발생. 카메라 방향은 코어의 MoveCamera가 잡고, 회전은 ExecuteMfgDrawing이 1회만 담당.
            //   누적 원복은 ExecuteMfgDrawing 진입부(_mfgPreviewNetRoll)가 처리.

            swTotal.Stop();
            DiagLog($"LvDrawingSheet_SelectedIndexChanged EXIT " +
                $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0} " +
                $"scene={sceneMs}ms dimension={dimensionMs}ms bom={bomMs}ms total={swTotal.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// BOM 정보 테이블(lvDrawingBOMInfo) 행 선택 시 해당 부재를 카메라 fit.
        /// 시트의 visibility는 건드리지 않고 카메라만 이동한다.
        /// </summary>
        private void LvDrawingBOMInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrawingBOMInfo.SelectedItems.Count == 0) return;
            ListViewItem row = lvDrawingBOMInfo.SelectedItems[0];

            // 요약행(Row 0)은 대응 부재가 없으므로 스킵 (No.는 "00", #67 — 인덱스로 걸러 값과 무관)
            if (row.Index == 0) return;

            // No. 컬럼 파싱 → bomList 순서(i+1)와 일치 (CollectBOMInfo 매핑 기준)
            int itemNo;
            if (!int.TryParse(row.SubItems[0].Text, out itemNo)) return;
            if (itemNo < 1 || itemNo > bomList.Count) return;

            int bodyIdx = bomList[itemNo - 1].Index;

            try
            {
                vizcore3d.BeginUpdate();

                // T-022: 선택 행 부재를 "선택상태"(빨간색)로
                vizcore3d.Object3D.Select(VIZCore3D.NET.Data.Object3dSelectionModes.DESELECT_ALL);
                vizcore3d.Object3D.Select(new List<int> { bodyIdx }, true, false);

                vizcore3d.View.FlyToObject3d(new List<int> { bodyIdx }, 1.2f);
                vizcore3d.EndUpdate();
            }
            catch (Exception ex)
            {
                DiagLog($"LvDrawingBOMInfo_SelectedIndexChanged FAIL {ex.Message}");
            }
        }

        /// <summary>
        /// 2D 도면에 복사하기 위해 3D View에 임시 생성한 치수와 보조선을 제거한다.
        /// </summary>
        private void Clear3DDimensionAnnotations()
        {
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();
        }

        /// <summary>
        /// 도면정보 탭 — 선택된 시트의 포함부재를 X-Ray 선택 + Osnap/치수 추출 + 방향 보기
        /// </summary>
        private void ApplyDrawingSheetView(string viewDirection)
        {
            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                MessageBox.Show("도면 시트를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            if (sheet == null || sheet.MemberIndices.Count == 0)
                return;

            List<int> displayIndices = GetDrawingSheetDisplayIndices(sheet);

            try
            {
                // ISO는 풍선만 보여야 하고, X/Y/Z도 직전 뷰의 치수를 먼저 비운 뒤 다시 그린다.
                Clear3DDimensionAnnotations();

                if (viewDirection == "ISO")
                {
                    // ISO: 전체 X-Ray 설정 + Osnap/치수 수집 + 풍선 표시
                    vizcore3d.BeginUpdate();

                    if (!vizcore3d.View.XRay.Enable)
                        vizcore3d.View.XRay.Enable = true;

                    vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                    vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                    // 설치도 2D의 fit·치수 기준은 선택 STRU로 고정한다.
                    // 연결 Part는 점선 맥락으로만 캡처하고 기준 BBox에는 포함하지 않는다.
                    xraySelectedNodeIndices = sheet.BaseMemberIndex == -2
                        ? new List<int>(sheet.MemberIndices)
                        : displayIndices;

                    vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                    vizcore3d.Object3D.Show(displayIndices, true);

                    vizcore3d.View.FlyToObject3d(displayIndices, 1.2f);
                    vizcore3d.Clash.ClearResultSymbol();

                    vizcore3d.EndUpdate();

                    if (sheet.BaseMemberIndex == -2)
                        ExtractInstallationDimensions(sheet);

                    // T-034 후속 (2026-04-23): BOM 테이블 행 선택 → 글로벌 ISO 버튼 경로에서
                    // 여기 분기 탐 → 실선으로 부재가 잘 보이도록 SMOOTH 모드로 교체
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                    // 선택된 부재에 맞춰 화면 조정 (반복 호출 시 줌 누적 방지)
                    vizcore3d.View.FlyToObject3d(displayIndices, 1.0f);

                    // 2D 출력과 동일한 ISO 풍선 로직 사용
                    vizcore3d.Review.Note.Clear();
                    CreateIsoBalloonNotes(sheet.MemberIndices);
                }
                else
                {
                    // X/Y/Z: 시트 선택 시 이미 수집된 Osnap/치수 데이터 재활용
                    // X-Ray 모드 유지 + 방향 전환 + 렌더모드 + 치수 표시
                    vizcore3d.BeginUpdate();

                    // X-Ray 모드 유지 (해당 부재만 보이도록)
                    if (!vizcore3d.View.XRay.Enable)
                        vizcore3d.View.XRay.Enable = true;

                    vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                    vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                    vizcore3d.View.SilhouetteEdge = true;
                    vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                    // 설치도 치수선 기준 BBox는 출력 fit과 동일하게 선택 STRU만 사용한다.
                    // 연결 Part 전체 BBox가 baseline을 밀어 보조선이 뷰마다 길어지는 것을 막는다.
                    xraySelectedNodeIndices = sheet.BaseMemberIndex == -2
                        ? new List<int>(sheet.MemberIndices)
                        : displayIndices;

                    vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                    vizcore3d.Object3D.Show(displayIndices, true);

                    vizcore3d.EndUpdate();

                    vizcore3d.Review.Note.Clear();
                    // T-034 후속 (2026-04-23): X/Y/Z 뷰 경로도 SMOOTH 실선으로 통일
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);

                    // 카메라 방향 설정
                    switch (viewDirection)
                    {
                        case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS); break;
                        case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS); break;
                        case "Z": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS); break;
                    }

                    // 선택된 부재에 맞춰 화면 조정
                    vizcore3d.View.FlyToObject3d(
                        sheet.BaseMemberIndex == -2 ? sheet.MemberIndices : displayIndices, 1.0f);
                    ShowAllDimensions(viewDirection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"도면 시트 뷰 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ISO 뷰 풍선 노트 생성 (3D 고정 오프셋 + 2D AABB 겹침 검사)
        /// ApplyDrawingSheetView("ISO")와 RenderSheetViewForDrawing 양쪽에서 공유
        /// </summary>
        private Dictionary<int, int> CreateIsoBalloonNotes(List<int> memberIndices, bool forDrawing2D = false)
        {
            Dictionary<int, int> nodeToNoteMap = new Dictionary<int, int>();
            if (bomList == null || bomList.Count == 0) return nodeToNoteMap;

            // ISO_PLUS 등각 투영 2D 근사
            Func<float, float, float, (float h, float v)> isoProject = (px, py, pz) =>
            {
                return (0.707f * (px - py), 0.408f * (px + py) + 0.816f * pz);
            };

            HashSet<int> sheetMemberSet = new HashSet<int>(memberIndices);

            // 모델 3D 중심 + 대각 계산
            float mCenterX = 0, mCenterY = 0, mCenterZ = 0;
            float mMinX = float.MaxValue, mMinY = float.MaxValue, mMinZ = float.MaxValue;
            float mMaxX = float.MinValue, mMaxY = float.MinValue, mMaxZ = float.MinValue;
            int memberCount = 0;
            foreach (var bom in bomList)
            {
                if (!sheetMemberSet.Contains(bom.Index)) continue;
                mMinX = Math.Min(mMinX, bom.MinX); mMinY = Math.Min(mMinY, bom.MinY); mMinZ = Math.Min(mMinZ, bom.MinZ);
                mMaxX = Math.Max(mMaxX, bom.MaxX); mMaxY = Math.Max(mMaxY, bom.MaxY); mMaxZ = Math.Max(mMaxZ, bom.MaxZ);
                mCenterX += bom.CenterX; mCenterY += bom.CenterY; mCenterZ += bom.CenterZ;
                memberCount++;
            }
            if (memberCount == 0) return nodeToNoteMap;
            mCenterX /= memberCount; mCenterY /= memberCount; mCenterZ /= memberCount;

            float isoDiag = (float)Math.Sqrt(
                (mMaxX - mMinX) * (mMaxX - mMinX) +
                (mMaxY - mMinY) * (mMaxY - mMinY) +
                (mMaxZ - mMinZ) * (mMaxZ - mMinZ));
            // T-064 (2026-05-14): ISO 풍선 거리 단축 — 옛 (200, 0.35) → (100, 0.22)
            // 사용자 사양: 도면리스트 뽑기 ISO 뷰 풍선이 모델에서 너무 멀어 PDF 시각 거슬림
            float baseOffsetDist = Math.Max(100f, isoDiag * 0.22f);

            // 모델 2D AABB (겹침 검사용)
            float[] cornersX = { mMinX, mMaxX };
            float[] cornersY = { mMinY, mMaxY };
            float[] cornersZ = { mMinZ, mMaxZ };
            float modelH_min = float.MaxValue, modelH_max = float.MinValue;
            float modelV_min = float.MaxValue, modelV_max = float.MinValue;
            foreach (float ccx in cornersX)
                foreach (float ccy in cornersY)
                    foreach (float ccz in cornersZ)
                    {
                        var p = isoProject(ccx, ccy, ccz);
                        modelH_min = Math.Min(modelH_min, p.h);
                        modelH_max = Math.Max(modelH_max, p.h);
                        modelV_min = Math.Min(modelV_min, p.v);
                        modelV_max = Math.Max(modelV_max, p.v);
                    }
            // 2026-05-12 사용자 사양: 외곽 부재 풍선 거리 절반 — 정규화 거리 계산용 원래 BBox 보존
            float modelH_min_orig = modelH_min, modelH_max_orig = modelH_max;
            float modelV_min_orig = modelV_min, modelV_max_orig = modelV_max;
            float modelHalfH_orig = (modelH_max_orig - modelH_min_orig) / 2f;
            float modelHalfV_orig = (modelV_max_orig - modelV_min_orig) / 2f;
            float modelCenterH_orig = (modelH_min_orig + modelH_max_orig) / 2f;
            float modelCenterV_orig = (modelV_min_orig + modelV_max_orig) / 2f;

            float aabbPad = Math.Max(modelH_max - modelH_min, modelV_max - modelV_min) * 0.05f;
            modelH_min -= aabbPad; modelH_max += aabbPad;
            modelV_min -= aabbPad; modelV_max += aabbPad;

            // 풍선 AABB 크기
            float balloonHalfW = 25f, balloonHalfH = 12f, balloonGap = 5f;
            List<(float minH, float minV, float maxH, float maxV)> placedAABBs =
                new List<(float, float, float, float)>();

            // BOM 테이블 # 번호 매핑: bom.Name → lvDrawingBOMInfo 행의 # 값
            Dictionary<string, string> bomNameToTableNo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int ri = 1; ri < lvDrawingBOMInfo.Items.Count; ri++)
            {
                ListViewItem lvi = lvDrawingBOMInfo.Items[ri];
                string no = lvi.SubItems[0].Text;       // # 번호
                string item = lvi.SubItems[1].Text;      // ITEM 이름
                if (!string.IsNullOrEmpty(item) && !bomNameToTableNo.ContainsKey(item))
                    bomNameToTableNo[item] = no;
            }

            foreach (var bom in bomList)
            {
                if (!sheetMemberSet.Contains(bom.Index)) continue;

                VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);

                // 2026-05-12 사용자 사양: 풍선은 좌/우만 향하게 (Y 성분 0)
                // 부재 중심의 X 방향으로만 풍선 배치 — 위/아래에는 배치 X
                float initDirX = (bom.CenterX > mCenterX) ? 1f : -1f;
                float initDirY = 0f;

                // 2026-05-12 사용자 사양: 부재 중심이 모델 BBox 절반 너머(외곽)면 풍선 거리 절반
                var bomProj = isoProject(bom.CenterX, bom.CenterY, bom.CenterZ);
                float dh = Math.Abs(bomProj.h - modelCenterH_orig);
                float dv = Math.Abs(bomProj.v - modelCenterV_orig);
                float normalizedDist = Math.Max(
                    modelHalfH_orig > 0.001f ? dh / modelHalfH_orig : 0f,
                    modelHalfV_orig > 0.001f ? dv / modelHalfV_orig : 0f);
                float perMemberOffset = (normalizedDist > 0.5f) ? baseOffsetDist * 0.5f : baseOffsetDist;

                // 3D 위치 후보 → 2D 투영 → AABB 검사 → 충돌 시 3D 회전
                float noteX = bom.CenterX + initDirX * perMemberOffset;
                float noteY = bom.CenterY + initDirY * perMemberOffset;
                float noteZ = bom.CenterZ;
                var projNote = isoProject(noteX, noteY, noteZ);

                bool positionFound = false;
                for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                {
                    float bMinH = projNote.h - balloonHalfW;
                    float bMaxH = projNote.h + balloonHalfW;
                    float bMinV = projNote.v - balloonHalfH;
                    float bMaxV = projNote.v + balloonHalfH;

                    // 모델 AABB와 겹침
                    bool insideModel = bMinH < modelH_max && bMaxH > modelH_min &&
                                       bMinV < modelV_max && bMaxV > modelV_min;

                    // 다른 풍선과 AABB 겹침
                    bool collidesPlaced = false;
                    if (!insideModel)
                    {
                        foreach (var placed in placedAABBs)
                        {
                            if (bMinH - balloonGap < placed.maxH && bMaxH + balloonGap > placed.minH &&
                                bMinV - balloonGap < placed.maxV && bMaxV + balloonGap > placed.minV)
                            { collidesPlaced = true; break; }
                        }
                    }

                    if (!insideModel && !collidesPlaced)
                    {
                        positionFound = true;
                    }
                    else
                    {
                        // 2026-05-12 사용자 사양: 회전 X (수평만), Y 슬롯으로만 회피 (위/아래 풍선 간격)
                        // attempt 0: 그대로, 1: 거리 +15%, 2: Y+슬롯, 3: Y-슬롯, 4: 거리 +30% Y+슬롯, ...
                        float newOffset = perMemberOffset * (1f + (attempt / 4) * 0.15f);
                        float yShift = ((attempt % 4) / 2) * (balloonHalfH * 2.5f);  // 0, 0, 1×Y, 1×Y
                        if ((attempt % 4) == 3) yShift = -yShift;                     // Y- 방향
                        noteX = bom.CenterX + initDirX * newOffset;
                        noteY = bom.CenterY + yShift;
                        noteZ = bom.CenterZ;
                        projNote = isoProject(noteX, noteY, noteZ);
                    }
                }

                placedAABBs.Add((projNote.h - balloonHalfW, projNote.v - balloonHalfH,
                                 projNote.h + balloonHalfW, projNote.v + balloonHalfH));

                VIZCore3D.NET.Data.Vertex3D notePos = new VIZCore3D.NET.Data.Vertex3D(noteX, noteY, noteZ);
                int id = vizcore3d.Review.Note.AddNoteSurface("TEMP", notePos, center);
                nodeToNoteMap[bom.Index] = id;

                // 풍선 텍스트를 BOM 테이블 # 번호와 일치시킴
                string balloonText;
                if (bomNameToTableNo.TryGetValue(bom.Name ?? "", out balloonText))
                {
                    // BOM 테이블의 # 번호 사용
                }
                else
                {
                    // 매핑 실패 시 bomList 내 순번 사용
                    balloonText = (bomList.IndexOf(bom) + 1).ToString();
                }
                VIZCore3D.NET.Data.NoteItem note = vizcore3d.Review.Note.GetItem(id);
                note.UpdateText(balloonText);
            }

            return nodeToNoteMap;
        }

        private void btnDrawingISO_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("ISO");
        }

        private void btnDrawingAxisX_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("X");
        }

        private void btnDrawingAxisY_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("Y");
        }

        private void btnDrawingAxisZ_Click(object sender, EventArgs e)
        {
            ApplyDrawingSheetView("Z");
        }

        /// <summary>
        /// "2D 출력" 버튼 클릭 — 선택된 도면시트 또는 전체 제작도의 3D 뷰 상태를 2D 도면으로 생성
        /// </summary>
        private void btnGenerateSheet2D_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrawingSheetData sheet = null;
            if (lvDrawingSheet.SelectedItems.Count > 0)
            {
                sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            }
            else
            {
                ListViewItem fullSheetItem = null;
                foreach (ListViewItem item in lvDrawingSheet.Items)
                {
                    DrawingSheetData candidate = item.Tag as DrawingSheetData;
                    if (candidate != null && candidate.BaseMemberIndex == -1)
                    {
                        fullSheetItem = item;
                        sheet = candidate;
                        break;
                    }
                }

                if (fullSheetItem != null)
                {
                    fullSheetItem.Selected = true;
                    fullSheetItem.Focused = true;
                    fullSheetItem.EnsureVisible();
                    DiagLog($"2D 출력 기본 대상: 전체 제작도 자동 선택 (members={sheet.MemberIndices.Count})");
                }
                else
                {
                    sheet = CreateFullDrawingSheetData();
                    DiagLog($"2D 출력 기본 대상: 임시 전체 제작도 생성 (members={sheet.MemberIndices.Count})");
                }
            }

            if (sheet == null || sheet.MemberIndices.Count == 0)
            {
                MessageBox.Show("유효한 시트 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GenerateSheetDrawing2D(sheet);
        }

        /// <summary>
        /// "PDF 출력" 버튼 클릭 — 2D 도면 캔버스(테두리 내부)만 PDF로 저장
        /// VIZCore3D 내장 Export2PDFBy2DView API 사용
        /// </summary>
        private void btnExportSheet2DPDF_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (vizcore3d.ViewMode != VIZCore3D.NET.Data.ViewKind.Both)
            {
                MessageBox.Show("먼저 '2D 출력' 버튼으로 2D 도면을 생성해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PDF 파일 (*.pdf)|*.pdf";
            dlg.FilterIndex = 1;
            dlg.FileName = $"Sheet2D_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                // 노란색 선택 테두리 제거
                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(dlg.FileName);

                MessageBox.Show($"PDF 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF 저장 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private enum DrawingSheetExportKind
        {
            Fabrication,
            Assembly,
            Installation
        }

        private void btnExportFabricationSheets_Click(object sender, EventArgs e)
        {
            ExportSheetsByKind(DrawingSheetExportKind.Fabrication);
        }

        private void btnExportAssemblySheets_Click(object sender, EventArgs e)
        {
            ExportSheetsByKind(DrawingSheetExportKind.Assembly);
        }

        private void btnExportInstallationSheets_Click(object sender, EventArgs e)
        {
            ExportSheetsByKind(DrawingSheetExportKind.Installation);
        }

        /// <summary>
        /// 이미 생성된 도면 목록에서 요청한 종류만 순서대로 2D 변환·PDF 저장한다.
        /// 정상 완료한 마지막 시트는 후속 확인과 수동 PDF 재출력을 위해 캔버스에 유지한다.
        /// </summary>
        private void ExportSheetsByKind(DrawingSheetExportKind exportKind)
        {
            string kindLabel = GetDrawingSheetExportKindLabel(exportKind);

            if (_cancelableOperationInProgress || !lvDrawingSheet.Enabled)
            {
                MessageBox.Show("다른 도면 작업이 진행 중입니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (bomList == null || bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.\n먼저 '치수 추출'을 실행해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lvDrawingSheet.Items.Count == 0)
            {
                MessageBox.Show("도면 시트 목록이 없습니다.\n먼저 '치수 추출'을 실행해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<KeyValuePair<ListViewItem, DrawingSheetData>> targetSheets =
                GetDrawingSheetExportTargets(exportKind);
            if (targetSheets.Count == 0)
            {
                MessageBox.Show($"{kindLabel} 시트가 없습니다.\n먼저 '치수 추출'을 실행해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Dictionary<Control, bool> previousEnabledStates = CaptureDrawingExportControlStates();
            List<string> failures = new List<string>();
            string saveDir = null;
            int successCount = 0;
            bool canceled = false;
            bool cancelableOperationStarted = false;

            try
            {
                SetDrawingExportControlsEnabled(false);
                BeginCancelableOperation();
                cancelableOperationStarted = true;
                ShowBusyOverlay($"{kindLabel} PDF 출력 준비 중...");

                saveDir = GetDefaultDrawingSaveDir();
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

                // [issue #116] 출력 후 로딩 무한 대기 추적 — 대상 개수부터 남긴다.
                DiagLog($"[{kindLabel} 출력] 시작 targets={targetSheets.Count} saveDir={saveDir}");

                for (int i = 0; i < targetSheets.Count; i++)
                {
                    ListViewItem item = targetSheets[i].Key;
                    DrawingSheetData sheet = targetSheets[i].Value;
                    bool sheetSucceeded = false;

                    try
                    {
                        ThrowIfCancellationRequested($"{kindLabel} {i + 1}/{targetSheets.Count} 시작 전");
                        ShowBusyOverlay($"{kindLabel} PDF 출력 {i + 1}/{targetSheets.Count}: {item.Text}");

                        SelectDrawingSheetItemForExport(item);
                        ApplySheetSelection(sheet);
                        ThrowIfCancellationRequested($"{kindLabel} {i + 1}/{targetSheets.Count} 선택 후");

                        GenerateSheetDrawing2D(sheet);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(200);
                        ThrowIfCancellationRequested($"{kindLabel} {i + 1}/{targetSheets.Count} 2D 생성 후");

                        string pdfFileName = BuildDrawingSheetPdfFileName(
                            sheet, item.Text, kindLabel, timeStamp);
                        string pdfPath = Path.Combine(saveDir, pdfFileName);

                        vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);

                        successCount++;
                        DiagLog($"[{kindLabel} 출력] PDF saved: {pdfPath}");
                        ThrowIfCancellationRequested($"{kindLabel} {i + 1}/{targetSheets.Count} PDF 저장 후");
                        sheetSucceeded = true;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{item.Text}: {ex.Message}");
                        DiagLog($"[{kindLabel} 출력] sheet#={sheet.SheetNumber} ERROR: {ex.Message}");
                    }
                    finally
                    {
                        bool keepFinalCanvas = i == targetSheets.Count - 1 && sheetSucceeded;
                        DiagLog($"[{kindLabel} 출력] 시트 {i + 1}/{targetSheets.Count} 종료 " +
                                $"ok={sheetSucceeded} keepFinalCanvas={keepFinalCanvas}");
                        if (!keepFinalCanvas)
                            CleanupDrawingSheetExportCanvas();
                    }
                }
                DiagLog($"[{kindLabel} 출력] 루프 종료 success={successCount} failures={failures.Count}");
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            catch (Exception ex)
            {
                failures.Add($"출력 준비: {ex.Message}");
                DiagLog($"[{kindLabel} 출력] FAIL: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // [issue #116] finally 각 단계를 개별로 남긴다 — 어느 호출에서 멈추는지 특정용.
                DiagLog($"[{kindLabel} 출력] finally 진입 canceled={canceled} started={cancelableOperationStarted}");
                if (cancelableOperationStarted)
                {
                    try { HideBusyOverlay(); } catch (Exception ex) { DiagLog($"[{kindLabel} 출력] HideBusyOverlay 실패 {ex.Message}"); }
                    DiagLog($"[{kindLabel} 출력] HideBusyOverlay 완료");
                    EndCancelableOperation();
                    DiagLog($"[{kindLabel} 출력] EndCancelableOperation 완료");
                }
                RestoreDrawingExportControlStates(previousEnabledStates);
                DiagLog($"[{kindLabel} 출력] 컨트롤 상태 복원 완료");
            }

            DiagLog($"[{kindLabel} 출력] 결과 표시 직전 success={successCount}");
            ShowDrawingSheetExportResult(kindLabel, saveDir, successCount, failures, canceled);
            DiagLog($"[{kindLabel} 출력] 결과 표시 완료 — 정상 종료");
        }

        private List<KeyValuePair<ListViewItem, DrawingSheetData>> GetDrawingSheetExportTargets(
            DrawingSheetExportKind exportKind)
        {
            var result = new List<KeyValuePair<ListViewItem, DrawingSheetData>>();
            foreach (ListViewItem item in lvDrawingSheet.Items)
            {
                DrawingSheetData sheet = item.Tag as DrawingSheetData;
                if (sheet == null || sheet.MemberIndices == null || sheet.MemberIndices.Count == 0)
                    continue;
                if (!MatchesDrawingSheetExportKind(sheet, exportKind))
                    continue;

                result.Add(new KeyValuePair<ListViewItem, DrawingSheetData>(item, sheet));
            }
            return result;
        }

        private bool MatchesDrawingSheetExportKind(
            DrawingSheetData sheet, DrawingSheetExportKind exportKind)
        {
            switch (exportKind)
            {
                case DrawingSheetExportKind.Fabrication:
                    return sheet.BaseMemberIndex == -1;
                case DrawingSheetExportKind.Assembly:
                    return sheet.BaseMemberIndex >= 0;
                case DrawingSheetExportKind.Installation:
                    return sheet.BaseMemberIndex == -2;
                default:
                    return false;
            }
        }

        private string GetDrawingSheetExportKindLabel(DrawingSheetExportKind exportKind)
        {
            switch (exportKind)
            {
                case DrawingSheetExportKind.Fabrication:
                    return "제작도";
                case DrawingSheetExportKind.Assembly:
                    return "조립도";
                case DrawingSheetExportKind.Installation:
                    return "설치도";
                default:
                    return "도면";
            }
        }

        private void SelectDrawingSheetItemForExport(ListViewItem item)
        {
            _suppressSheetSelectionHandler = true;
            try
            {
                while (lvDrawingSheet.SelectedItems.Count > 0)
                    lvDrawingSheet.SelectedItems[0].Selected = false;

                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
            finally
            {
                _suppressSheetSelectionHandler = false;
            }
        }

        private Dictionary<Control, bool> CaptureDrawingExportControlStates()
        {
            var result = new Dictionary<Control, bool>();
            Control[] controls =
            {
                btnExportFabricationSheets,
                btnExportAssemblySheets,
                btnExportInstallationSheets,
                btnMfgDrawingSheet,
                btnGenerateSheet2D,
                btnExportSheet2DPDF,
                btnExtractDrawingList,
                lvDrawingSheet
            };

            foreach (Control control in controls)
                result[control] = control.Enabled;

            return result;
        }

        private void SetDrawingExportControlsEnabled(bool enabled)
        {
            btnExportFabricationSheets.Enabled = enabled;
            btnExportAssemblySheets.Enabled = enabled;
            btnExportInstallationSheets.Enabled = enabled;
            btnMfgDrawingSheet.Enabled = enabled;
            btnGenerateSheet2D.Enabled = enabled;
            btnExportSheet2DPDF.Enabled = enabled;
            btnExtractDrawingList.Enabled = enabled;
            lvDrawingSheet.Enabled = enabled;
        }

        private void RestoreDrawingExportControlStates(Dictionary<Control, bool> previousStates)
        {
            if (previousStates == null)
                return;

            foreach (KeyValuePair<Control, bool> state in previousStates)
                state.Key.Enabled = state.Value;
        }

        private string BuildDrawingSheetPdfFileName(
            DrawingSheetData sheet,
            string sheetLabel,
            string kindLabel,
            string timeStamp)
        {
            string safeKind = SanitizeFileName(kindLabel);
            string safeBaseName = SanitizeFileName(
                string.IsNullOrWhiteSpace(sheet.BaseMemberName) ? "Unknown" : sheet.BaseMemberName.Trim());
            string safeSheetLabel = SanitizeFileName(
                string.IsNullOrWhiteSpace(sheetLabel) ? $"Sheet {sheet.SheetNumber}" : sheetLabel.Trim());

            var parts = new List<string> { safeKind };
            if (!string.Equals(safeBaseName, safeKind, StringComparison.OrdinalIgnoreCase))
                parts.Add(safeBaseName);
            parts.Add(safeSheetLabel);
            parts.Add(timeStamp);
            return string.Join("_", parts) + ".pdf";
        }

        private void CleanupDrawingSheetExportCanvas()
        {
            // [issue #116] 이 정리 단계가 무한 대기 1순위 후보다.
            //   GC.WaitForPendingFinalizers()를 UI 스레드에서 부르면, 파이널라이저가 UI 스레드로
            //   마샬링을 시도할 때 서로 기다리는 데드락이 난다 (VIZCore3D는 대형 네이티브 SDK라
            //   모델 교체 직후 대기 중인 파이널라이저가 많다). 단계별 로그로 지점을 특정한다.
            try
            {
                DiagLog("[정리] Clear2DView 진입");
                Clear2DView();
                DiagLog("[정리] Clear2DView 완료 — GC 진입");
                GC.Collect();
                DiagLog("[정리] GC.Collect 1 완료 — WaitForPendingFinalizers 진입");
                GC.WaitForPendingFinalizers();
                DiagLog("[정리] WaitForPendingFinalizers 완료");
                GC.Collect();
                Application.DoEvents();
                System.Threading.Thread.Sleep(100);
                DiagLog("[정리] 완료");
            }
            catch (Exception ex)
            {
                DiagLog($"[도면 종류별 출력] 2D 정리 경고: {ex.Message}");
            }
        }

        private void ShowDrawingSheetExportResult(
            string kindLabel,
            string saveDir,
            int successCount,
            List<string> failures,
            bool canceled)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine(canceled
                ? $"{kindLabel} 출력을 취소했습니다."
                : $"{kindLabel} 출력이 완료되었습니다.");
            message.AppendLine();
            message.AppendLine($"저장된 PDF: {successCount}개");
            if (!string.IsNullOrWhiteSpace(saveDir))
                message.AppendLine($"저장 위치: {saveDir}");

            if (failures.Count > 0)
            {
                message.AppendLine();
                message.AppendLine($"실패: {failures.Count}개");
                foreach (string failure in failures)
                    message.AppendLine($"- {failure}");
            }

            MessageBoxIcon icon = canceled || failures.Count > 0
                ? MessageBoxIcon.Warning
                : MessageBoxIcon.Information;
            MessageBox.Show(message.ToString(), $"{kindLabel} 출력 결과",
                MessageBoxButtons.OK, icon);
        }

        /// <summary>
        /// 파일명에 사용할 수 없는 문자 제거
        /// </summary>
        private string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        // P2 — 엑셀 템플릿 기반 도면 흐름 분기 플래그.
        // true (P2 기본): GenerateSheetDrawing2D_WithExcelTemplate (ImportExcelWithData + GetViewAreasFromExcel + fit)
        // false: 옛 직접 그리기 흐름 (안전 fallback, P4 정리 시 결정).
        private bool UseExcelTemplate = true;

        // issue #7 — 제작도 ISO 이웃 점선 Crop 여백 비율 (0=시트 부재 영역에 딱 맞춤, 클수록 붙은 주변 맥락이 더 보임).
        //   CropFit2DViewObjectByNodeIDs의 cropOffset로 전달. 실기 튜닝값 — "붙은 부위가 적당히 보이는" 값으로 조정.
        private const float IsoNeighborCropOffset = 0.5f;

        // ISO 리뷰와 실제 2D 객체 외곽 사이의 도면 고정 거리.
        private const float IsoReviewGapCanvas = 20f;

        // 제작도 ISO 연결 부재 이름의 지시선 길이(모델 월드 좌표, mm).
        // Target은 Clash HotPoint 그대로 두고 Label만 X축 방향으로 이동한다.
        private const float IsoNeighborNoteWorldOffset = 100f;

        private sealed class FabricationNeighborAssemblyNote
        {
            public int AssemblyIndex { get; set; }
            public string AssemblyName { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        /// <summary>
        /// 선택된 시트 부재만 대상으로 2D 도면 생성
        /// (ISO 풍선번호 + X/Y/Z 치수선 + BOM 테이블 + 도면정보)
        /// </summary>
        private void GenerateSheetDrawing2D(DrawingSheetData sheet)
        {
            try
            {
                GenerateSheetDrawing2DCore(sheet);
            }
            finally
            {
                // 2D 객체로 복사한 치수는 유지하고, 렌더링에 사용한 임시 3D 치수·보조선만 제거한다.
                Clear3DDimensionAnnotations();
            }
        }

        private void GenerateSheetDrawing2DCore(DrawingSheetData sheet)
        {
            string sheetKind = GetSheetKindLabel(sheet);

            // 사전 조건: 히든라인 모델 투영용 엣지 데이터 갱신 (ISO 방향 튀어나온 모서리 누락 방지)
            // 자동(ProcessSingleStruFull)·수동(btnGenerateSheet2D_Click) 모두 이 함수 통과 → 단일 지점에서 보장
            ProcessCancelableUiCheckpoint(
                $"{sheetKind} 2D 생성 중... 엣지 준비",
                $"{sheetKind} sheet#{sheet.SheetNumber} 엣지 생성 전");
            vizcore3d.Object3D.GenerateEdgeData();
            ProcessCancelableUiCheckpoint(
                $"{sheetKind} 2D 생성 중... 엣지 준비 완료",
                $"{sheetKind} sheet#{sheet.SheetNumber} 엣지 생성 후");

            // P2 — 엑셀 템플릿 분기
            if (UseExcelTemplate)
            {
                GenerateSheetDrawing2D_WithExcelTemplate(sheet);
                return;
            }

            try
            {
                vizcore3d.View.EnableAnimation = false;

                // ── 0. 기존 3D 어노테이션 모두 초기화 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // ── 0-1. UI ListView 초기화 (BOM 정보 — CollectBOMInfo에서 다시 수집) ──
                lvDrawingBOMInfo.Items.Clear();

                // ── 1. 2D 완전 초기화 ──
                Clear2DView();

                // 2D 패널 크기 조정
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

                // A4 용지 크기로 캔버스 새로 설정 (297 x 210mm, 가로)
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);

                // ── 2. 시트 부재 설정 (ApplyDrawingSheetView("ISO")와 동일한 흐름) ──
                vizcore3d.BeginUpdate();

                if (!vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = true;

                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                vizcore3d.View.XRay.Clear();
                vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
                vizcore3d.Clash.ClearResultSymbol();

                vizcore3d.EndUpdate();

                // 작업데이터 탭 체인치수 = 도면 표시 치수 통일.
                // 설치도는 PreparedDimensions의 전용 끝단→모서리 위치 치수, 그 외는 공용 Osnap 치수를 사용한다.
                {
                    // E1 (2026-05-18): _lastCollectedNodeOsnapMap 전달 — 본체 fallback으로 안전 보장
                    chainDimensionList.Clear();
                    lvDimension.Items.Clear();
                    chainDimensionList.AddRange(GetDrawingSheetDimensionsFor2D(sheet));
                    int dimNo = 1;
                    foreach (var dim in chainDimensionList)
                    {
                        dim.No = dimNo;
                        ListViewItem dlvi = new ListViewItem(dimNo.ToString());
                        dlvi.SubItems.Add(dim.Axis);
                        dlvi.SubItems.Add(dim.ViewName);
                        dlvi.SubItems.Add(((int)Math.Round(dim.Distance)).ToString());
                        dlvi.SubItems.Add(dim.StartPointStr);
                        dlvi.SubItems.Add(dim.EndPointStr);
                        dlvi.Tag = dim;
                        lvDimension.Items.Add(dlvi);
                        dimNo++;
                    }
                }

                // BOM 자동 수집
                // D1 (2026-05-18): sheet 명시 전달 — 함수 인자 그대로 위임
                CollectBOMInfo(false, sheet);

                // ── 3. 그리드 구조 먼저 생성 (CrateTemplateBorder가 그리드 필요) ──
                {
                    int selCanvas = 1;
                    vizcore3d.Drawing2D.View.SetSelectCanvas(selCanvas);
                    float tmpW = 0f, tmpH = 0f;
                    vizcore3d.Drawing2D.View.GetCanvasSize(ref tmpW, ref tmpH);
                    vizcore3d.Drawing2D.GridStructure.AddGridStructure(2, 3, tmpW, tmpH);
                    vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);

                    // T-038 step C (2026-05-12 사용자 사양): 4뷰 셀 하단 마진을 *라벨 영역 크기*만큼 키움.
                    // FitObjectToGridCellAspect가 마진 제외 fit → 모델이 라벨 영역 침범 불가 (명시적 방지).
                    // 라벨 텍스트 4mm + 박스 패딩 → 약 12mm. 보조선 일부 들어갈 여유 포함.
                    const float LABEL_BOTTOM_MARGIN = 12f;
                    // SetGridCellMargins(row, col, left, right, top, bottom)
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(1, 1, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(1, 2, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(2, 1, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(2, 2, 10f, 10f, 10f, LABEL_BOTTOM_MARGIN);

                    // 2026-05-13 T-037 4차: BOM 열 합 77→101mm 24mm 증가 → 왼쪽으로 24mm 이동 (left 12 → -12)
                    // SetGridCellMargins(row, col, left, right, top, bottom)
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(1, 3, -12f, 10f, 10f, 10f);  // BOM (left -12 = 셀 경계 밖 음수 마진)
                    vizcore3d.Drawing2D.GridStructure.SetGridCellMargins(2, 3, 12f, 10f, 10f, 13f);   // tableInfo (left 12, bottom 13)
                }

                // ── 4. 템플릿 생성 (외곽 테두리) ──
                // BOM/tableInfo는 셀 기반(RenderTemplateOnGridStructure)으로 이관되어
                // 이전에 절대좌표 앵커로 쓰던 bInfo는 더 이상 필요 없음.
                // 새 SDK(VIZCore3D+.NET) — CreateTemplateBorder()(스펠링 정정, xml 31246, 무인자 + 반환값)
                vizcore3d.Drawing2D.Template.CreateTemplateBorder();

                // BOM 최대 데이터 행수 — 셀 (1,3) 높이(약 95mm) 내 수용 한도
                const int BOM_MAX_DATA_ROWS = 14;

                // [표1] BOM 테이블 — 그리드 셀 (1,3) 상단 정렬 배치
                if (lvDrawingBOMInfo.Items.Count > 0)
                {
                    int totalItems = lvDrawingBOMInfo.Items.Count;
                    int displayRows = System.Math.Min(totalItems, BOM_MAX_DATA_ROWS);
                    bool truncated = totalItems > BOM_MAX_DATA_ROWS;
                    int tableRowCount = displayRows + 1 + (truncated ? 1 : 0);  // 헤더 + 데이터 + (생략 행)

                    VIZCore3D.NET.Data.TemplateTableData table1 = new VIZCore3D.NET.Data.TemplateTableData(tableRowCount, 8);
                    table1.SetText(0, 0, "No");
                    table1.SetText(0, 1, "ITEM");
                    table1.SetText(0, 2, "MATERIAL");
                    table1.SetText(0, 3, "SIZE");
                    table1.SetText(0, 4, "Q'TY");
                    table1.SetText(0, 5, "T/W");
                    table1.SetText(0, 6, "MA");
                    table1.SetText(0, 7, "FA");

                    for (int i = 0; i < displayRows; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i];
                        for (int col = 0; col < 8 && col < item.SubItems.Count; col++)
                        {
                            table1.SetText(i + 1, col, item.SubItems[col].Text);
                        }
                    }

                    if (truncated)
                    {
                        int lastRow = displayRows + 1;
                        table1.SetText(lastRow, 0, "…");
                        table1.SetText(lastRow, 1, string.Format("+{0}건 생략", totalItems - BOM_MAX_DATA_ROWS));
                    }

                    table1.IsTextWrapped = true;
                    // T-037 3차 (2026-05-13): 사용자 사양 6/20/17/30/8/9/6/5 (합 101mm)
                    table1.ColumnWidths = new Dictionary<int, int>()
                    {
                        { 0, 6 },    // No
                        { 1, 20 },   // ITEM
                        { 2, 17 },   // MATERIAL
                        { 3, 30 },   // SIZE
                        { 4, 8 },    // Q'TY
                        { 5, 9 },    // T/W
                        { 6, 6 },    // MA
                        { 7, 5 }     // FA
                    };

                    vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(1, 3,
                        VIZCore3D.NET.Data.GridVerticalAlignment.Top);
                    vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(1, 3,
                        VIZCore3D.NET.Data.GridHorizontalAlignment.Center);

                    // T-037 2차: BOM 셀 텍스트 폰트 축소 시도 (SDK 적용 보장 X — 실기 검증 필요)
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(4f);
                    vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(table1, 1, 3);
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);  // 기본 복원
                }

                // [표2] 도면정보 — 그리드 셀 (2,3) 하단 정렬 배치 (2행 2열: 1열 로고, 2열 텍스트)
                VIZCore3D.NET.Data.TemplateTableData tableInfo = new VIZCore3D.NET.Data.TemplateTableData(2, 2);
                string infoLogoPath = ResolveDrawingAssetPath("Logo.png");
                tableInfo.SetText(0, 0, infoLogoPath);
                tableInfo.SetText(0, 1, "Project Name:\nProject No:");
                tableInfo.SetText(1, 0, infoLogoPath);
                tableInfo.SetText(1, 1, "Title:");
                tableInfo.IsTextWrapped = true;
                // 열 너비 합 77mm (흰선 내부 폭 추가 축소, 기존 81→77)
                tableInfo.ColumnWidths = new Dictionary<int, int>() { { 0, 30 }, { 1, 47 } };

                vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(2, 3,
                    VIZCore3D.NET.Data.GridVerticalAlignment.Bottom);
                vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(2, 3,
                    VIZCore3D.NET.Data.GridHorizontalAlignment.Center);
                vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(tableInfo, 2, 3);

                // [라벨] 뷰 라벨은 모델 배치·크기조정·위치이동 후에 렌더링 (아래 MoveObject 이후)

                // 2D 모델 라인 두께 전역 설정 + 치수선/보조선 가늘게
                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;  // T-040 v5: 2.0→3.0 (모델 두드러지게)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);

                // 2D 치수 텍스트 크기 설정 (스케일 축소 후 가독성 유지를 위해 작게)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(10f);  // 2026-05-12: 5f→10f 2배 (사용자 확정)

                // ── 5. 4개 뷰 투영 + 스케일 조정 + 풍선/치수 변환 ──
                // T-038 (2026-05-12 사용자 사양): 모델을 셀 가득 — FitObjectToGridCellAspect만 사용.
                // targetH = 0f면 RescaleObject 추가 스케일 건너뜀 (RenderSheetViewForDrawing L1702 분기).
                // 차후 C단계: 라벨/보조선 영역 확보 위해 동적 targetH 도입 예정.
                float targetH = 0f;  // 모델 가득 (이전 40f → 셀의 30%만 차지하던 문제 해소)

                // [1,1] ISO — 풍선번호
                RenderSheetViewForDrawing(1, 1, "ISO", sheet, targetH);

                // [1,2] Z축 — 치수선+보조선+풍선
                RenderSheetViewForDrawing(1, 2, "Z", sheet, targetH);

                // [2,1] Y축 — 치수선+보조선+풍선
                RenderSheetViewForDrawing(2, 1, "Y", sheet, targetH);

                // [2,2] X축 — 치수선+보조선+풍선
                RenderSheetViewForDrawing(2, 2, "X", sheet, targetH);

                // ── 라벨 텍스트 (ORIENTATION 반영) ──
                string[] labelTexts = new string[4];
                labelTexts[0] = "  ISO  ";
                int orientMemberIdx = (sheet.MemberIndices != null && sheet.MemberIndices.Count > 0) ? sheet.MemberIndices[0] : -1;
                string[] viewDirs = { "Z", "Y", "X" };
                for (int vi = 0; vi < 3; vi++)
                {
                    if (orientMemberIdx >= 0)
                        labelTexts[vi + 1] = $"  {GetOrientationLabel(orientMemberIdx, viewDirs[vi])}  ";
                    else
                        labelTexts[vi + 1] = $"  Looking \"{viewDirs[vi]}\"  ";
                }

                // ── 6. 최종 렌더링 (모델 + 치수선 + 풍선 확정) ──
                vizcore3d.Drawing2D.Render();

                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // [라벨] A4 6등분 셀 하단 중앙에 배치
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                {
                    int[,] labelCells = { { 1, 1 }, { 1, 2 }, { 2, 1 }, { 2, 2 } };

                    for (int li = 0; li < 4; li++)
                    {
                        int lr = labelCells[li, 0];
                        int lc = labelCells[li, 1];

                        VIZCore3D.NET.Data.TemplateTableData labelTbl = new VIZCore3D.NET.Data.TemplateTableData(1, 1);
                        labelTbl.SetText(0, 0, labelTexts[li]);

                        vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(lr, lc,
                            VIZCore3D.NET.Data.GridVerticalAlignment.Bottom);
                        vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(lr, lc,
                            VIZCore3D.NET.Data.GridHorizontalAlignment.Center);
                        vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(labelTbl, lr, lc);
                    }
                }

                // 라벨 추가 후 재렌더링
                vizcore3d.Drawing2D.Render();

                // 3D 뷰 복원: 선택 부재만 보이게 (2D 렌더링 과정에서 전체 복원된 상태 → 원래대로)
                vizcore3d.BeginUpdate();
                vizcore3d.View.XRay.Enable = false;
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
                vizcore3d.EndUpdate();

                // 2D 뷰에서 마지막 생성된 객체의 선택(활성화) 해제
                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                // 2D 오토핏 (전체 캔버스 맞춤)
                vizcore3d.Drawing2D.View.SetCanvasResetViewPos(-1);

                // ── 7. 뷰어 크기 조정 (도면 완성 후 마지막에 수행) ──
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 3D=20%, 2D=80% — 오른쪽 2D 패널을 크게
                        if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                        {
                            vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.1);
                        }

                        // 패널 크기 변경 후 오토핏 재실행
                        vizcore3d.Drawing2D.View.SetCanvasResetViewPos(-1);

                        // 오토핏 후 3배 줌인 (모델 선택 → WM_MOUSEWHEEL → 선택 해제)
                        try
                        {
                            // 2D 오브젝트 선택 (줌 동작에 필요)
                            vizcore3d.Drawing2D.Object2D.SelectAllObjectBy2DView();

                            // 실제 2D 캔버스 핸들 찾기 (Panel2의 자식 컨트롤)
                            SplitterPanel panel2 = vizcore3d.SplitContainer.Panel2;
                            IntPtr hwnd = panel2.Controls.Count > 0
                                ? panel2.Controls[0].Handle
                                : panel2.Handle;

                            // 포커스 설정
                            SetFocus(hwnd);

                            // Panel2 중앙의 스크린 좌표 계산 (줌 기준점)
                            Point center = panel2.PointToScreen(
                                new Point(panel2.Width / 2, panel2.Height / 2));
                            int lParam = (center.Y << 16) | (center.X & 0xFFFF);

                            // 줌인: WHEEL_DELTA 양수 = 확대, 약 7회 → 약 3배
                            for (int z = 0; z < 7; z++)
                            {
                                IntPtr wParam = (IntPtr)(WHEEL_DELTA << 16);
                                SendMessage(hwnd, WM_MOUSEWHEEL, wParam, (IntPtr)lParam);
                            }

                            // 선택 해제
                            vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                            vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                        }
                        catch { }
                    }
                    catch { }
                }));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"2D 도면 생성 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// P2 — 엑셀 템플릿 기반 도면 생성 (Softhills 신 API 3종 활용).
        /// 사용자평소템플릿_엑셀_제작도.xlsx 활용 — 제작도/조립도/설치도 공통 (시트 종류 라벨만 다름).
        ///
        /// 흐름:
        ///   1) 캔버스 초기화 (옛 코드와 동일 — Clear2DView + ViewMode + SetCanvasSize)
        ///   2) 모델/치수 라인 두께 (옛 코드와 동일)
        ///   3) data Dictionary 구성 — {Input_N} 슬롯 치환 (도면정보 + BOM 8컬럼 × 15행)
        ///   4) ImportExcelWithData — 엑셀 자동 그리기 (BOM 테이블·도면정보·외곽 테두리 포함)
        ///   5) GetViewAreasFromExcel — {View_n} 영역 좌표 파싱
        ///   6) 각 View 영역에 모델 투영 (카메라 회전 + Create2DViewObjectWithModelHiddenLine + fit + MoveObjectTo)
        ///
        /// PoC 패턴 (btnExcelTemplatePoC_Click)을 메인 도면 흐름에 적용.
        /// 시트 부재는 *현재 visible*이라는 조건 — ProcessSingleStruFull/옵션B에서 격리·시트 선택 처리됨.
        /// </summary>
        private void GenerateSheetDrawing2D_WithExcelTemplate(DrawingSheetData sheet)
        {
            DrawingReferenceFrame drawingReferenceFrame = null;
            try
            {
                vizcore3d.View.EnableAnimation = false;

                // ── 0. 기존 3D 어노테이션 모두 초기화 ──
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // ── 1. 2D 완전 초기화 (옛 코드와 동일) ──
                Clear2DView();
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

                // A4 캔버스 — 엑셀이 더 큰 페이지면 ImportExcelWithData가 자동 조정 가능
                vizcore3d.Drawing2D.View.SetCanvasSize(297, 210);
                vizcore3d.Drawing2D.View.SetSelectCanvas(1);

                // 모델/치수 라인 두께
                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(10f);

                // ── 1.5. 시트 부재 가시성 격리 (옛 GenerateSheetDrawing2D 1234~1252와 동일) ──
                // T-064 P2 핫픽스 (2026-05-14): PoC는 사용자가 모델 전체 보이는 상태에서 호출 →
                // Create2DViewObjectWithModelHiddenLine이 전체 모델로 4면도 생성.
                // 자동 흐름은 ProcessSingleStruFull → 시트 적용 → 이 메서드 호출인데,
                // 가시성 격리·BOM·치수 단계가 없으면 빈 객체 4면도 → 캔버스에 엑셀 격자만 남음.
                vizcore3d.BeginUpdate();
                if (!vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = true;
                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;
                vizcore3d.View.XRay.Clear();
                vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);
                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
                vizcore3d.Clash.ClearResultSymbol();
                vizcore3d.EndUpdate();

                // ── 1.6. 치수 데이터 적용 ──
                // 설치도는 PreparedDimensions, 그 외 시트는 공용 Osnap 6조합 합집합을 사용한다.
                chainDimensionList.Clear();
                lvDimension.Items.Clear();
                // 제작도(Sheet 1)만 선택 영역의 가장 긴 수평 모서리로 참조축을 만든다.
                // 조립도·설치도는 현재 실기 결과를 보존하기 위해 기존 세계축 경로를 유지한다.
                if (sheet.BaseMemberIndex == -1)
                    drawingReferenceFrame = TryBuildDrawingReferenceFrame(sheet.MemberIndices);
                chainDimensionList.AddRange(
                    GetDrawingSheetDimensionsFor2D(sheet, drawingReferenceFrame));
                int dimNo = 1;
                foreach (var dim in chainDimensionList)
                {
                    dim.No = dimNo;
                    ListViewItem dlvi = new ListViewItem(dimNo.ToString());
                    dlvi.SubItems.Add(dim.Axis);
                    dlvi.SubItems.Add(dim.ViewName);
                    dlvi.SubItems.Add(((int)Math.Round(dim.Distance)).ToString());
                    dlvi.SubItems.Add(dim.StartPointStr);
                    dlvi.SubItems.Add(dim.EndPointStr);
                    dlvi.Tag = dim;
                    lvDimension.Items.Add(dlvi);
                    dimNo++;
                }

                // ── 1.7. BOM 정보 수집 (옛 본문 1278~1279 동일 — lvDrawingBOMInfo 채우기) ──
                // 이 호출이 lvDrawingBOMInfo의 8컬럼을 채워야 아래 data 매핑이 BOM 슬롯에 정상 적용됨.
                // D1 (2026-05-18): sheet 명시 전달 — _WithExcelTemplate 함수 인자 그대로 위임
                CollectBOMInfo(false, sheet);

                // ── 2. 엑셀 파일 경로 (실행 폴더 templates\ 우선 — 배포 패키지 대응) ──
                string xlsxPath = ResolveDrawingTemplatePath("제작도_도면_1.xlsx");
                if (!System.IO.File.Exists(xlsxPath))
                {
                    DiagLog($"P2 엑셀 파일 없음: {xlsxPath}");
                    throw new Exception($"엑셀 파일 없음: {xlsxPath}");
                }

                // ── 3. data Dictionary 구성 ({Input_N} 슬롯 치환) ──
                // 슬롯 컨벤션 (신 템플릿 제작도_도면_1 — BOM 20행 기준, 열별 20연속):
                //   1 = 프로젝트명, 2 = 선박번호, 3 = 도면종류
                //   4..23 = BOM No (20행), 24..43 = ITEM, 44..63 = MATERIAL, 64..83 = SIZE,
                //   84..103 = Q'TY, 104..123 = T/W, 124..143 = MA, 144..163 = FA
                Dictionary<int, string> data = new Dictionary<int, string>();
                // [2026-07-27] 빈 슬롯 선초기화(1~240 → ""/" ") 제거 — 벤더(소프트힐스) 안내 반영.
                //   ImportExcelWithData는 data에 값이 있으면(""·" " 포함) 치환하고, 값이 없을 때만 {Input}으로 남긴다.
                //   그리고 {Input}으로 남은 셀만 JSON에 TextBox로 생성되는데, RemoveEmptyTemplateBorders는
                //   그 TextBox가 있어야 괘선을 지운다. 선초기화가 전 슬롯을 채우면 {Input}이 하나도 안 남아
                //   괘선 제거가 통째로 무동작이었다 (SDK 1.0.26.727 전달 메일 — 전문 요약은 issue #60,
                //   원본 .eml은 gitignore로 로컬 전용).
                //   → 미기재 슬롯은 data에 키를 넣지 않고 {Input}으로 남긴다.
                //   ⚠ 부작용 2건 실기 확인 필요: ① 07-21 확정한 "PAINT/DP/TAG(165~169)·REV 첫 기재행(194~199)
                //   괘선 보존" 정책이 깨져 같이 지워질 수 있음 ② TextBox가 PDF에 {Input} 글자로 노출될 수 있음
                //   (선초기화의 원래 목적이 그 노출 방지였음). 재발 시 보존 슬롯만 " "로 되돌리는 게 1차 대응.
                //   슬롯: 4~163=BOM 1~20행, 164=Note내용, 165~168=PAINT/DP, 169=TAG NO,
                //   170~199=Rev 표, 200=Note 라벨(AW33), 201~240=BOM 21~25행.
                //   Note 라벨("Note : ")은 템플릿에서 제거됨 — 향후 Note 실데이터를 채울 땐 코드가 "Note : " 접두어까지 포함할 것.
                // 200(Note 라벨)은 노트가 없으면 키를 안 넣어 {Input}으로 남긴다 → 라벨칸 괘선까지 제거(2026-07-22 사용자).
                // Note(AW33 라벨 = {Input_200}, AY33 내용 = {Input_164}) — 노트가 있을 때만 라벨 "Note" + 내용 표시.
                //   [2026-07-22] Input 200+ 렌더 실기 통과 확인 완료(제작도 200~240 크래시 없음) → 테스트용
                //   임시값(data[200]="Note" 항상표시) 제거. 노트 입력 기능 미구현이라 현재는 미치환 상태로 둔다.
                //   입력 소스가 생기면 아래처럼 조건부로 채운다:
                // string sheetNote = <노트 입력 소스>;
                // if (!string.IsNullOrEmpty(sheetNote)) { data[200] = "Note"; data[164] = sheetNote; }
                // 도면정보 — TODO: tableInfo 또는 sheet 메타에서. 지금은 PoC 하드코딩 유지.
                data[1] = "CEDAR FLNG";
                data[2] = "SN2688";
                // 시트 종류 라벨 — 제작도/조립도/설치도/가공도 (GetSheetKindLabel: Form1.Stru.cs)
                data[3] = GetSheetKindLabel(sheet);
                // TAG NO(169) = STRU 단위 UDA "STRU" 값 (사용자 2026-07-21). 기준부재에서 조상 STRU까지 walk-up.
                //   값 없으면 초기 " "(공백, 괘선 보존) 유지.
                string struTag = GetStruUdaValue(sheet.BaseMemberIndex);
                if (!string.IsNullOrEmpty(struTag)) data[169] = struTag;
                // PAINT CODE(166) = 출력 시점에 STRU에서 한 번 조회해 모든 도면에 공유한 값.
                // UDA.Keys는 BeginUpdate 밖인 현재 데이터 구성 단계에서만 호출한다.
                // 값이 없으면 초기 " "(공백, 괘선 보존)을 유지한다.
                string paintCode = GetOrCacheDrawingPaintCode(sheet);
                if (!string.IsNullOrEmpty(paintCode)) data[166] = paintCode;
                // DP No(168) = 임시 "Test" (사용자 2026-07-21: 지금은 Test로)
                data[168] = "Test";
                // REV 표 첫 기재행(194~199) — REV.=0 / 출력일 / 나머지는 공백(괘선만 보존) (#64 Phase 1).
                //   이 경로는 제작도·조립도·설치도 3종이 공유하므로 한 번 호출로 모두 적용된다.
                //   미사용 이력행(170~193)은 키를 안 넣어 괘선이 지워진다. 헬퍼: Form1.ExcelTemplate.cs
                FillRevisionTable(data, BuildCurrentRevisionHistory());

                // BOM 8컬럼 × 25행 — lvDrawingBOMInfo Row 0(요약행)을 첫 행으로 포함한다 (#67).
                //   1~20행: 기존 태그(열별 20연속, 4~163). 21~25행: 신규 태그(201~240, 열별 5연속) — 2026-07-22 Input 200+ 확장.
                //   요약행이 1행을 쓰므로 데이터행 정원은 25 → 24행 (2026-07-28 사용자 확정).
                int bomMapped = 0;
                if (lvDrawingBOMInfo.Items.Count > 0)
                {
                    int n = Math.Min(lvDrawingBOMInfo.Items.Count, 25);
                    for (int i = 0; i < n; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i];
                        int cNo, cItem, cMat, cSize, cQty, cTw, cMa, cFa;
                        if (i < 20)
                        {
                            cNo = 4 + i;   cItem = 24 + i;  cMat = 44 + i;  cSize = 64 + i;
                            cQty = 84 + i; cTw = 104 + i;   cMa = 124 + i;  cFa = 144 + i;
                        }
                        else
                        {
                            int j = i - 20;   // 0~4 (BOM 21~25행)
                            cNo = 201 + j;  cItem = 206 + j; cMat = 211 + j; cSize = 216 + j;
                            cQty = 221 + j; cTw = 226 + j;   cMa = 231 + j;  cFa = 236 + j;
                        }
                        data[cNo]   = item.Text;                              // NO
                        data[cItem] = SafeSubItem(item, 1);                   // ITEM
                        data[cMat]  = SafeSubItem(item, 2);                   // MATERIAL
                        data[cSize] = SafeSubItem(item, 3);                   // SIZE
                        data[cQty]  = SafeSubItem(item, 4);                   // Q'TY
                        data[cTw]   = SafeSubItem(item, 5);                   // T/W
                        data[cMa]   = SafeSubItem(item, 6);                   // MA
                        data[cFa]   = SafeSubItem(item, 7);                   // FA
                    }
                    bomMapped = n;
                    if (lvDrawingBOMInfo.Items.Count - 1 > 25)
                        DiagLog($"P2 BOM {lvDrawingBOMInfo.Items.Count - 1}행 중 25행만 표시 (템플릿 한도)");
                }
                DiagLog($"P2 data 구성: kind='{data[3]}' BOM {bomMapped}행 (Input 총 {data.Count}개)");

                // ── 4. ImportExcelWithData — 엑셀 자동 그리기 + 데이터 치환 ──
                // 다중 이미지 매핑 (SDK 1.0.26.716 신규) — {Image_N} 태그에 파일 직접 매핑.
                //   1 = N 화살표(BOM 왼쪽 상단, AT3), 2 = ISO 화살표(프레임 좌상단, C3), 3 = CONTRACTOR 로고(AW53),
                //   4 = CLIENT 이미지(AW49, 옛 {View_6} 자리 — 2026-07-21 이미지로 전환).
                //   Value = [일반, 배경반전]. 옛 {Image}+Set2DViewTemplateMark는 신 SDK에서 무력화 확인(로고 미표시)되어
                //   {Image_3}로 통합 (2026-07-21). 옛 RenderTemplate 수동 배치(캘리브레이션)도 이 방식이 대체.
                var imageMapping = new Dictionary<int, string[]>
                {
                    { 1, new[] { ResolveDrawingAssetPath("North_Arrow.png"), ResolveDrawingAssetPath("North_Arrow.png") } },
                    { 2, new[] { ResolveDrawingAssetPath("ISO_North_Arrow.png"), ResolveDrawingAssetPath("ISO_North_Arrow.png") } },
                    { 3, new[] { ResolveDrawingAssetPath("Logo.png"), ResolveDrawingAssetPath("Logo.png") } },
                    { 4, new[] { ResolveDrawingAssetPath("ClientTestImage.png"), ResolveDrawingAssetPath("ClientTestImage.png") } },
                };
                var swTpl = System.Diagnostics.Stopwatch.StartNew();
                ProcessCancelableUiCheckpoint(
                    $"{GetSheetKindLabel(sheet)} 2D 생성 중... 템플릿 적용",
                    $"sheet#{sheet.SheetNumber} 템플릿 적용 전");
                vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data, imageMapping);
                swTpl.Stop();
                ProcessCancelableUiCheckpoint(
                    $"{GetSheetKindLabel(sheet)} 2D 생성 중... 템플릿 적용 완료",
                    $"sheet#{sheet.SheetNumber} 템플릿 적용 후");
                vizcore3d.Drawing2D.View.SetSelectCanvas(1);
                DiagLog($"P2 템플릿 적용 {swTpl.ElapsedMilliseconds}ms — {Path.GetFileName(xlsxPath)}");

                // 빈 칸 괘선 제거 (SDK 1.0.26.716) — 미기재 BOM 행 등 내용 없는 공백 셀의 테두리를 지운다.
                //   ⚠ 전역 동작(BOM만 선별 불가) — Rev 표·NOTE 등 다른 빈 칸 괘선도 함께 사라질 수 있음 (실기 확인).
                vizcore3d.Drawing2D.Object2D.RemoveEmptyTemplateBorders(0.1f, VIZCore3D.NET.Data.TemplateBorderRemoveMode.RowAndColumn);

                // ── 5. {View_n} 영역 좌표 — 매 출력 파싱 (작은 템플릿 ~수 ms).
                //   캐시 금지: 앱 켠 채 템플릿을 엑셀에서 고치면 옛 좌표를 재사용하는 버그가 있었음 (2026-07-19).
                var viewAreas = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
                if (viewAreas == null || viewAreas.Count == 0)
                {
                    DiagLog("P2 GetViewAreasFromExcel 비어있음 — 엑셀에 {View_N} 태그 없음");
                    return;
                }
                DiagLog($"P2 GetViewAreasFromExcel: {viewAreas.Count}개 영역");

                // 북쪽 화살표 2종은 {Image_1}/{Image_2} 태그 + imageMapping으로 Import 단계에서 처리 (2026-07-20).
                //   옛 View_5/View_7 수동 배치(PlaceImageInTemplateArea + RenderTemplate 캘리브레이션)는 폐기.
                //   View_6 = CLIENT 로고 예약(미사용) 유지.
                vizcore3d.Drawing2D.Render();

                // ── 6. View 인덱스 ↔ 카메라 매핑 (4면도 규약 — PoC와 동일) ──
                Dictionary<int, VIZCore3D.NET.Data.CameraDirection> cameraMap = new Dictionary<int, VIZCore3D.NET.Data.CameraDirection>
                {
                    { 1, VIZCore3D.NET.Data.CameraDirection.ISO_PLUS },   // ISO
                    { 2, VIZCore3D.NET.Data.CameraDirection.Z_MINUS  },   // LOOKING "Z"
                    { 3, VIZCore3D.NET.Data.CameraDirection.X_MINUS  },   // LOOKING "X"
                    { 4, VIZCore3D.NET.Data.CameraDirection.Y_MINUS  },   // LOOKING "Y"
                };

                const float margin = 5f;
                const float isoModelXOffset = 10f;
                const float templateYOffset = 15f;
                const float isoShrinkFactor = 0.70f;
                int viewsRendered = 0;

                // ── 7. 각 View 영역에 모델 + 치수/풍선 투영 ──
                // T-064 P2 본진 (2026-05-14): 메인 도면 엑셀 분기에 치수 그리기 이식.
                //   - ISO(Index=1): CreateIsoBalloonNotes + FromScreen 가시성 필터 → Add2DNoteFrom3DNote
                //   - Z/X/Y(Index=2/3/4): ShowAllDimensions(viewDir, true, estScale) → shapeDrawingIds + Add2DObjectFromShapeDrawing + Add2DMeasureFrom3DMeasure
                //   - 모델 fit + 사용자 사양 추가 shrink: Z=0.65 / X·Y·ISO=0.70 (라벨·보조선 영역 확보)
                // 옛 RenderSheetViewForDrawing(L1891~) 패턴을 viewArea 영역 기반으로 옮김.
                for (int i = 0; i < viewAreas.Count; i++)
                {
                    var p = viewAreas[i];
                    if (!cameraMap.TryGetValue(p.Index, out VIZCore3D.NET.Data.CameraDirection camDir))
                    {
                        if (p.Index <= 4)
                            DiagLog($"P2 View_{p.Index} 카메라 매핑 없음 — 스킵");
                        continue;
                    }

                    // viewArea Index → ShowAllDimensions의 viewDirection 문자열 매핑
                    string viewDir;
                    switch (p.Index)
                    {
                        case 1: viewDir = "ISO"; break;
                        case 2: viewDir = "Z"; break;
                        case 3: viewDir = "X"; break;
                        case 4: viewDir = "Y"; break;
                        default: continue;
                    }

                    ProcessCancelableUiCheckpoint(
                        $"{GetSheetKindLabel(sheet)} 2D 생성 중... {viewDir} 뷰 ({viewsRendered + 1}/{cameraMap.Count})",
                        $"sheet#{sheet.SheetNumber} {viewDir} 뷰 시작 전");

                    List<int> displayIndices = GetDrawingSheetDisplayIndices(sheet);

                    // 매 뷰마다 3D 어노테이션 초기화 (옛 RenderSheetViewForDrawing L1903~1905)
                    ReleaseActiveDrawingReferenceAxis(
                        $"sheet={sheet.SheetNumber} view={viewDir} start");
                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
                    _lastModelShiftCanvasX = 0f;
                    _lastModelShiftCanvasY = 0f;

                    // 시트 부재만 보이기 (X-Ray off)
                    vizcore3d.BeginUpdate();
                    if (vizcore3d.View.XRay.Enable) vizcore3d.View.XRay.Enable = false;
                    vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                    vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                    // 설치도 치수 baseline·배율 기준은 선택 STRU로 고정한다.
                    xraySelectedNodeIndices = sheet.BaseMemberIndex == -2
                        ? new List<int>(sheet.MemberIndices)
                        : displayIndices;
                    vizcore3d.EndUpdate();

                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                    if (drawingReferenceFrame != null)
                    {
                        bool referenceAxisActivated = ActivateDrawingReferenceAxis(
                            drawingReferenceFrame, camDir, sheet.SheetNumber, viewDir);
                        if (!referenceAxisActivated)
                        {
                            // 카메라가 세계축으로 폴백했으면 치수도 같은 세계축 목록으로 즉시 되돌린다.
                            drawingReferenceFrame = null;
                            chainDimensionList.Clear();
                            chainDimensionList.AddRange(
                                GetDrawingSheetDimensionsFor2D(sheet, null));
                            DiagLog($"[DrawingRefAxis] sheet={sheet.SheetNumber} " +
                                    $"참조축 전체 폴백 — 치수도 WorldAxis로 재계산");
                        }
                    }
                    else
                    {
                        vizcore3d.View.MoveCamera(camDir);
                        if (viewDir != "ISO" && sheet.MemberIndices != null && sheet.MemberIndices.Count > 0)
                            ApplyOrientationRotation(sheet.MemberIndices[0], viewDir);
                    }

                    // ── 두 겹 표현 대상 산출 ──
                    //   설치도(전 뷰): 선택 STRU 실선 + 직접 연결된 외부 Part만 점선, STRU 기준 CropFit
                    //   조립도(Sheet2+): 전체 구조를 띄우고 기준부재만 실선, 나머지 LONG_DASHED 점선 — 프레임 = 전체 fit
                    //   제작도(Sheet1): 시트 부재 실선 + 간섭으로 붙은 시트 밖 부재 점선, 점선은 CropFit으로 시트 부재 영역+여백만 남김 — 프레임 = 시트 fit
                    //   대상 없으면(이웃 0개·부재 1개뿐) 현행 단일 캡처 폴백.
                    List<int> isoDashedTargets = null;   // 점선 배경 대상 (설치도: 연결 Part / 조립도: 전체−기준 / 제작도: 시트 밖 이웃 Part)
                    List<int> isoSolidTargets = null;    // 실선 캡처 대상
                    bool isoFitByDashed = false;         // true = 점선(전체 배경) 기준 fit (조립도) / false = 실선 기준 (제작도)
                    if (sheet.BaseMemberIndex == -2 && sheet.InstallationContextIndices.Count > 0)
                    {
                        isoSolidTargets = new List<int>(sheet.MemberIndices);
                        isoDashedTargets = sheet.InstallationContextIndices
                            .Where(index => !sheet.MemberIndices.Contains(index))
                            .Distinct()
                            .ToList();
                        if (isoDashedTargets.Count == 0)
                            isoDashedTargets = null;
                        isoFitByDashed = false;
                    }
                    else if (viewDir == "ISO")
                    {
                        if (sheet.BaseMemberIndex >= 0 && bomList != null && bomList.Count > 1)   // 조립도
                        {
                            // #7 재오픈(㉠): 실선 대상 = 기준부재만 → BOM 테이블 부재 전체.
                            //   실선이 BOM 전체가 되면 점선 배경(전체−기준)이 비므로 점선 없이 단일 실선 캡처로 단순화.
                            isoSolidTargets = new List<int>();
                            foreach (var b in bomList) isoSolidTargets.Add(b.Index);
                            isoDashedTargets = null;   // 점선 배경 없음
                            isoFitByDashed = false;
                        }
                        else if (sheet.BaseMemberIndex == -1)   // 제작도
                        {
                            var neighborParts = GetClashNeighborPartsOutsideSheet(sheet.MemberIndices);
                            DiagLog($"P2 ISO 제작도 시트 밖 간섭 이웃 {neighborParts.Count}개" +
                                    (neighborParts.Count == 0 ? " — 단일 캡처 폴백 (간섭 데이터에 밖 부재 없음)" : ""));
                            if (neighborParts.Count > 0)
                            {
                                isoSolidTargets = sheet.MemberIndices;
                                isoDashedTargets = neighborParts;
                            }
                        }
                    }

                    // 카메라 fit — 조립도 두 겹만 전체(점선+실선) 기준. 설치도·제작도는 실선 시트 부재 기준.
                    if (isoDashedTargets != null && isoFitByDashed)
                    {
                        var flyAll = new List<int>(isoDashedTargets);
                        flyAll.AddRange(isoSolidTargets);
                        vizcore3d.View.FlyToObject3d(flyAll, 1.25f);
                    }
                    else if (viewDir == "ISO" && isoSolidTargets != null && isoSolidTargets.Count > 0)
                    {
                        // #7 재오픈(㉠): 조립도 단일 실선 — 실선 대상(BOM 전체) 기준 fit
                        vizcore3d.View.FlyToObject3d(isoSolidTargets, 1.25f);
                    }
                    else
                    {
                        vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.25f);
                    }

                    // ── 뷰별 3D 어노테이션 ──
                    List<int> shapeDrawingIds = null;
                    List<int> visibleNoteIds = null;

                    if (viewDir == "ISO")
                    {
                        // ISO 풍선 — 옛 RenderSheetViewForDrawing L1957~1991
                        vizcore3d.BeginUpdate();
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
                        vizcore3d.View.XRay.Enable = true;
                        vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                        vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                        vizcore3d.View.XRay.Clear();
                        vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                        vizcore3d.EndUpdate();

                        Dictionary<int, int> nodeToNoteMap = CreateIsoBalloonNotes(sheet.MemberIndices, true);

                        vizcore3d.View.EnableBoxSelectionFrontObjectOnly = true;
                        var visibleNodes = vizcore3d.Object3D.FromScreen(false, VIZCore3D.NET.Data.LeafNodeKind.BODY);
                        visibleNoteIds = new List<int>();
                        foreach (var node in visibleNodes)
                        {
                            int noteId;
                            if (nodeToNoteMap.TryGetValue(node.Index, out noteId) ||
                                nodeToNoteMap.TryGetValue(node.ParentIndex, out noteId))
                            {
                                if (!visibleNoteIds.Contains(noteId)) visibleNoteIds.Add(noteId);
                            }
                        }

                        // [issue #62] 부재번호 풍선 미출력 진단 — 세 지점 중 어디서 0이 되는지 구분한다.
                        //   notes=0        → CreateIsoBalloonNotes 단계 (bomList 비었거나 시트 부재가 BOM에 없음)
                        //   visibleNotes=0 → FromScreen 가시성 필터 단계 (2D 변환을 건너뜀 → 아래 폴백)
                        //   둘 다 >0인데 도면에 없으면 → 2D 변환·정합 단계
                        //   viewSize/splitter도 같이 남긴다 — FromScreen은 3D 뷰 화면을 훑는데 이 경로는
                        //   직전(2010행)에 SplitterDistance를 Width*0.2로 줄여 3D 패널을 좁힌 상태다.
                        string viewDiag;
                        try
                        {
                            System.Drawing.Size vs = vizcore3d.View.Size;
                            var sc = vizcore3d.SplitContainer;
                            viewDiag = $"viewSize={vs.Width}x{vs.Height} mode={vizcore3d.ViewMode} " +
                                       (sc != null ? $"splitter={sc.SplitterDistance}/{sc.Width}" : "splitter=null");
                        }
                        catch (Exception exDiag) { viewDiag = $"viewDiag 실패 {exDiag.Message}"; }

                        DiagLog($"P2 ISO 풍선 sheet#={sheet.SheetNumber} members={sheet.MemberIndices.Count} " +
                                $"notes={nodeToNoteMap.Count} visibleNodes={visibleNodes.Count} " +
                                $"visibleNotes={visibleNoteIds.Count} {viewDiag}");

                        // [issue #62] 가시성 필터가 빈손이면 풍선이 통째로 사라진다 (사내 실기: notes=3 visibleNodes=0).
                        //   이 필터는 "가려진 부재의 풍선을 빼는" 최적화지 필수 단계가 아니다.
                        //   0이면 시트 부재 전체 풍선으로 폴백한다 — 전부 없는 것보다 전부 있는 쪽이 맞다.
                        //   ⚠ 근본 원인(FromScreen이 왜 0인지)은 미규명. 위 viewSize/splitter 로그로 계속 추적할 것.
                        if (visibleNoteIds.Count == 0 && nodeToNoteMap.Count > 0)
                        {
                            visibleNoteIds.AddRange(nodeToNoteMap.Values.Distinct());
                            DiagLog($"P2 ISO 풍선 가시성 필터 빈손 — 시트 부재 전체 {visibleNoteIds.Count}개로 폴백 (issue #62)");
                        }

                        // 풍선 생성 후 시트 부재만 보이기 (2D 캡처 준비)
                        vizcore3d.BeginUpdate();
                        vizcore3d.View.XRay.Enable = false;
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                        vizcore3d.EndUpdate();
                    }
                    else if (sheet.BaseMemberIndex != -2)
                    {
                        // X/Y/Z 치수 — 옛 RenderSheetViewForDrawing L1995~2002
                        float availW = p.Width - 2f * margin;
                        float availH = p.Height - 2f * margin;
                        List<int> scaleReferenceIndices = sheet.BaseMemberIndex == -2
                            ? sheet.MemberIndices
                            : displayIndices;
                        float estScale = EstimateFitScaleForViewArea(
                            availW, availH, viewDir, scaleReferenceIndices,
                            drawingReferenceFrame: drawingReferenceFrame);
                        shapeDrawingIds = ShowAllDimensions(
                            viewDir, true, estScale,
                            keepCamera: drawingReferenceFrame != null,
                            drawingReferenceFrame: drawingReferenceFrame);
                        // 부재-부재 접합 각도 표시 — 수직·수평이 아닌 접합부 각도를 같은 측정→2D 파이프라인에 누적 (2026-06-23)
                        MarkNonRightAngles(sheet.MemberIndices, viewDir);
                    }

                    // ── 모델 캡처 — ISO 두 겹(점선 먼저 캡처 → 실선이 위) 또는 단일 (issue #7) ──
                    Action<int> fitAndPlaceObject = targetObjId =>
                    {
                        float fitW = p.Width - 2f * margin;
                        float fitH = p.Height - 2f * margin;
                        float objW = 0f, objH = 0f;
                        vizcore3d.Drawing2D.Object2D.GetObjectSize(targetObjId, ref objW, ref objH);
                        float objScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(targetObjId);
                        if (objW > 0f && objH > 0f && fitW > 0f && fitH > 0f)
                        {
                            float fitScale = Math.Min(fitW / objW, fitH / objH);
                            float shrinkFactor = (viewDir == "Z") ? 0.65f : isoShrinkFactor;
                            vizcore3d.Drawing2D.Object2D.RescaleObject(
                                targetObjId, objScale * fitScale * shrinkFactor);
                        }

                        float xOffset = (p.Index == 1) ? isoModelXOffset
                                      : (p.Index == 4) ? 20f
                                      : 0f;
                        float cx = p.X + p.Width / 2f;
                        float cy = p.Y + p.Height / 2f;
                        vizcore3d.Drawing2D.Object2D.MoveObjectTo(targetObjId, cx + xOffset, cy + templateYOffset);
                    };

                    int dashedObjId = -1;
                    if (isoDashedTargets != null)
                    {
                        // CropFit 예제 불변조건: Crop 기준 노드는 Crop 대상 2D 객체 안에도 들어 있어야 한다.
                        // 설치도·제작도는 "시트 부재 + 연결 Part"를 함께 점선 배경으로 캡처한 뒤 시트 부재로 Crop하고,
                        // 다음 캡처에서 시트 부재 실선을 위에 덮는다. 조립도는 기존 점선 대상만 캡처한다.
                        var dashedCaptureTargets = new List<int>(isoDashedTargets);
                        if (!isoFitByDashed && isoSolidTargets != null)
                        {
                            foreach (int targetIndex in isoSolidTargets)
                                if (!dashedCaptureTargets.Contains(targetIndex))
                                    dashedCaptureTargets.Add(targetIndex);
                        }

                        vizcore3d.BeginUpdate();
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(dashedCaptureTargets, true);
                        vizcore3d.EndUpdate();

                        dashedObjId = vizcore3d.Drawing2D.Object2D
                            .Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                        if (dashedObjId >= 0)
                        {
                            // 설치도·제작도: 시트 부재를 포함한 점선 배경을 시트 부재 영역 + 여백만 남기고 잘라냄.
                            //   조립도는 전체 구조를 배경으로 보여줘야 하므로 Crop 안 함 (isoFitByDashed=true).
                            //   Crop 기준인 isoSolidTargets가 dashedObjId 안에 포함된 상태여야 SDK 예제처럼 동작한다.
                            if (!isoFitByDashed)
                            {
                                vizcore3d.Drawing2D.Object2D.CropFit2DViewObjectByNodeIDs(
                                    dashedObjId, isoSolidTargets, IsoNeighborCropOffset);
                                string cropKind = sheet.BaseMemberIndex == -2 ? "설치도 연결 Part" : "제작도 이웃";
                                DiagLog($"P2 {viewDir} {cropKind} 점선 CropFit obj={dashedObjId} " +
                                        $"captured={dashedCaptureTargets.Count} cropNodes={isoSolidTargets.Count} " +
                                        $"offset={IsoNeighborCropOffset:F2}");
                            }

                            // LONG_DASHED — 소프트힐스 예제 순서대로 CropFit 뒤에 선 종류를 정의한다.
                            vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineType(dashedObjId,
                                VIZCore3D.NET.Data.Object2D_LineTypes.LONG_DASHED);
                            vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineThickness(dashedObjId, 0.15f);

                            // 설치도·제작도는 예제 순서대로 점선 정의·배치를 끝낸 뒤 실선을 캡처한다.
                            // 조립도는 이미 실기 정상인 기존 순서(두 객체 캡처 후 점선 배치)를 유지한다.
                            if (!isoFitByDashed)
                                fitAndPlaceObject(dashedObjId);
                        }
                        else
                        {
                            DiagLog($"P2 View_{p.Index} 점선 캡처 실패 — 실선 단일로 계속");
                        }

                        // 실선 대상만 보이기 (두 번째 캡처 준비)
                        vizcore3d.BeginUpdate();
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(isoSolidTargets, true);
                        vizcore3d.EndUpdate();
                    }
                    else if (viewDir == "ISO" && isoSolidTargets != null && isoSolidTargets.Count > 0)
                    {
                        // #7 재오픈(㉠): 조립도 단일 실선 캡처 전, 실선 대상(BOM 전체)만 표시
                        //   (앞 풍선 단계는 sheet.MemberIndices만 보였으므로 여기서 BOM 전체로 교체)
                        vizcore3d.BeginUpdate();
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(isoSolidTargets, true);
                        vizcore3d.EndUpdate();
                    }

                    int objId = vizcore3d.Drawing2D.Object2D
                        .Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                            VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                    if (objId < 0)
                    {
                        DiagLog($"P2 View_{p.Index} Object2D 생성 실패 objId={objId}" +
                                (dashedObjId >= 0 ? " (점선 캡처는 캔버스에 잔존)" : ""));
                        continue;
                    }

                    // 단일 캡처와 조립도는 기존 시점에 fit + 이동.
                    // 설치도·제작도 두 겹은 점선 캡처 직후 이미 배치 완료.
                    if (dashedObjId < 0)
                        fitAndPlaceObject(objId);
                    else if (isoFitByDashed)
                        fitAndPlaceObject(dashedObjId);

                    // ── 두 겹 정합 — 스케일 통일 후 Match2DObjectsTo3DObjectPosition (SDK 1.0.26.716, issue #7) ──
                    //   설치도·조립도·제작도 모두 예제 불변 순서인 Match(이동=실선, 기준=점선)를 사용한다.
                    if (dashedObjId >= 0)
                    {
                        float refScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(dashedObjId);
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, refScale);
                        bool matched = vizcore3d.Drawing2D.Object2D.Match2DObjectsTo3DObjectPosition(objId, dashedObjId);
                        float dashW = 0f, dashH = 0f;
                        vizcore3d.Drawing2D.Object2D.GetObjectSize(dashedObjId, ref dashW, ref dashH);
                        string layeredKind = sheet.BaseMemberIndex == -2 ? "설치도" : (isoFitByDashed ? "조립도" : "제작도");
                        DiagLog($"P2 {viewDir} 두겹 {layeredKind} dash={dashedObjId} solid={objId} " +
                                $"move={objId} ref={dashedObjId} refScale={refScale:F4} match={matched} dashSize=({dashW:F1}x{dashH:F1})");
                    }

                    // 설치도는 연결 Part CropFit과 모델 배치가 모두 끝난 뒤의 실측 배율로 치수·보조선을 만든다.
                    // BBox 추정 배율을 먼저 쓰면 실제 은선 투영/크롭 배율과 차이가 생겨 뷰마다 종이 길이가 달라진다.
                    if (viewDir != "ISO" && sheet.BaseMemberIndex == -2)
                    {
                        int scaleObjectId = dashedObjId >= 0 ? dashedObjId : objId;
                        float actualScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(scaleObjectId);
                        if (actualScale <= 0f || float.IsNaN(actualScale) || float.IsInfinity(actualScale))
                        {
                            float availW = p.Width - 2f * margin;
                            float availH = p.Height - 2f * margin;
                            actualScale = EstimateFitScaleForViewArea(
                                availW, availH, viewDir, sheet.MemberIndices);
                            DiagLog($"P2 설치도 실측 배율 fallback view={viewDir} scale={actualScale:F4}");
                        }

                        xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);
                        // keepCamera: 캡처는 MINUS 카메라(cameraMap)인데 ShowAllDimensions가 PLUS로 틀면
                        //   이후 Add2D 변환(보조선·치수)이 모델과 좌우 거울 반전 (2026-07-23 -X 뷰 치수 반대편 버그).
                        shapeDrawingIds = ShowAllDimensions(viewDir, true, actualScale, keepCamera: true);
                        DiagLog($"P2 설치도 실측 배율 보조선 view={viewDir} obj={scaleObjectId} " +
                                $"scale={actualScale:F4} dims={chainDimensionList.Count}");
                    }

                    // ── 보조선(ShapeDrawing) → 2D 추가 (X/Y/Z만, ISO는 보조선 없음) ──
                    if (shapeDrawingIds != null && shapeDrawingIds.Count > 0)
                    {
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);
                        vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
                        vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                    }

                    // ── 풍선 Note → 2D (ISO는 가시성 필터, X/Y/Z는 풍선 없음 → noteIds 빈 컬렉션) ──
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(10.5f);
                    var convertedNoteIndices = new List<int>();
                    if (visibleNoteIds != null && visibleNoteIds.Count > 0)
                    {
                        // SDK 1.0.26.723 예제 기준: 좌표 변환 기준이 될 ISO 실선 투영 객체를
                        // 활성화한 상태에서 3D 풍선을 2D로 변환한다.
                        vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.SelectObjectBy2DView(objId, 1);
                        try
                        {
                            vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
                        }
                        finally
                        {
                            vizcore3d.Drawing2D.Object2D.SelectObjectBy2DView(objId, 0);
                        }
                        convertedNoteIndices.AddRange(visibleNoteIds);
                        DiagLog($"P2 {viewDir} 풍선 2D 변환 {visibleNoteIds.Count}개 (기준 실선 obj={objId})");
                    }
                    else if (viewDir == "ISO")
                    {
                        // [issue #62] ISO인데 변환할 풍선이 없음 — 위 'P2 ISO 풍선' 로그로 어느 단계에서 0이 됐는지 확인.
                        DiagLog($"P2 {viewDir} 풍선 2D 변환 건너뜀 — visibleNoteIds 없음");
                    }
                    foreach (int nIdx in convertedNoteIndices)
                    {
                        try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(nIdx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                        catch { }
                    }
                    int createdIsoConnectionNameNotes = 0;

                    // 제작도 ISO: 연결 Part의 가장 가까운 상위 Assembly 이름을 실제 Clash 지점에 표시한다.
                    // 부재번호 풍선과 같은 3D 표면 노트 경로로 만든 뒤 점선 객체에 투영해야 영역 정렬에 포함된다.
                    if (viewDir == "ISO" && sheet.BaseMemberIndex == -1 &&
                        !isoFitByDashed && dashedObjId >= 0)
                    {
                        List<FabricationNeighborAssemblyNote> neighborNotes =
                            GetFabricationNeighborAssemblyNotes(sheet.MemberIndices);
                        int createdNeighborNotes = 0;

                        if (neighborNotes.Count > 0)
                        {
                            try
                            {
                                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                                vizcore3d.Drawing2D.Object2D.SelectObjectBy2DView(dashedObjId, 1);
                                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(10.5f);   // #44 부재번호 풍선과 동일 폰트

                                for (int noteOrder = 0; noteOrder < neighborNotes.Count; noteOrder++)
                                {
                                    FabricationNeighborAssemblyNote note = neighborNotes[noteOrder];
                                    float offsetDirection = (noteOrder % 2 == 0) ? 1f : -1f;
                                    var target = new VIZCore3D.NET.Data.Vertex3D(note.X, note.Y, note.Z);
                                    var label = new VIZCore3D.NET.Data.Vertex3D(
                                        note.X + IsoNeighborNoteWorldOffset * offsetDirection,
                                        note.Y,
                                        note.Z);

                                    try
                                    {
                                        int noteId = vizcore3d.Review.Note.AddNoteSurface(
                                            note.AssemblyName, label, target);
                                        if (noteId >= 0)
                                        {
                                            vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(new[] { noteId });
                                            createdNeighborNotes++;
                                            createdIsoConnectionNameNotes++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DiagLog($"P2 ISO 제작도 연결 이름 노트 실패 assembly='{note.AssemblyName}' " +
                                                $"point=({note.X:F1},{note.Y:F1},{note.Z:F1}) {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                DiagLog($"P2 ISO 제작도 연결 이름 노트 준비 실패 obj={dashedObjId} {ex.Message}");
                            }
                            finally
                            {
                                try { vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView(); }
                                catch { }
                            }
                        }

                        DiagLog($"P2 ISO 제작도 연결 이름 노트 obj={dashedObjId} " +
                                $"candidates={neighborNotes.Count} created={createdNeighborNotes}");
                    }

                    // 설치도: 접합 중심 A1/A2는 표시하지 않는다.
                    // ISO에만 연결 Part당 Assembly/Part 이름을 접합측 실제 모서리에 한 번 표시한다.
                    if (viewDir == "ISO" && sheet.BaseMemberIndex == -2 && dashedObjId >= 0 &&
                        sheet.InstallationConnections != null && sheet.InstallationConnections.Count > 0)
                    {
                        int createdConnectionNotes = 0;
                        var noteGroups = sheet.InstallationConnections
                            .Where(connection => connection != null)
                            .GroupBy(connection => new
                            {
                                connection.ConnectedAssemblyIndex,
                                connection.ConnectedPartIndex,
                                connection.ConnectedAssemblyName,
                                connection.ConnectedPartName
                            })
                            .OrderBy(group => group.Key.ConnectedAssemblyName)
                            .ThenBy(group => group.Key.ConnectedPartName)
                            .ToList();
                        try
                        {
                            vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                            vizcore3d.Drawing2D.Object2D.SelectObjectBy2DView(dashedObjId, 1);
                            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(10.5f);   // #44 부재번호 풍선과 동일 폰트

                            for (int noteOrder = 0; noteOrder < noteGroups.Count; noteOrder++)
                            {
                                var noteGroup = noteGroups[noteOrder];
                                InstallationConnectionData connection = noteGroup.First();
                                InstallationPlacementAnchor anchor = noteGroup
                                    .GroupBy(item => new
                                    {
                                        item.TargetPartIndex,
                                        item.TargetBodyIndex,
                                        item.ConnectedPartIndex,
                                        item.ConnectedBodyIndex
                                    })
                                    .Select(bodyGroup => BuildInstallationPlacementAnchor(bodyGroup))
                                    .Where(candidate => candidate != null)
                                    .OrderByDescending(candidate => candidate.MergedAreaCount)
                                    .ThenBy(candidate => candidate.ConnectedBodyIndex)
                                    .FirstOrDefault();
                                if (anchor == null)
                                {
                                    DiagLog($"설치도 ISO 연결 이름 생략 — 모서리 선별 실패 " +
                                            $"connectedPart={connection.ConnectedPartIndex}");
                                    continue;
                                }

                                VIZCore3D.NET.Data.Vector3D target = anchor.ConnectedCornerPoint;
                                float offset = IsoNeighborNoteWorldOffset *
                                    (noteOrder % 2 == 0 ? 1f : -1f);
                                VIZCore3D.NET.Data.Vector3D label = GetInstallationNoteLabelPoint(
                                    target, viewDir, offset);
                                string text = connection.ConnectedAssemblyName;   // #45 STRU 이름만 (A. 접두사·/Part 제거)
                                try
                                {
                                    var targetVertex = new VIZCore3D.NET.Data.Vertex3D(
                                        target.X, target.Y, target.Z);
                                    var labelVertex = new VIZCore3D.NET.Data.Vertex3D(
                                        label.X, label.Y, label.Z);
                                    int noteId = vizcore3d.Review.Note.AddNoteSurface(
                                        text, labelVertex, targetVertex);
                                    if (noteId >= 0)
                                    {
                                        vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(new[] { noteId });
                                        createdConnectionNotes++;
                                        createdIsoConnectionNameNotes++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DiagLog($"설치도 접합영역 노트 실패 label={connection.Label} view={viewDir} {ex.Message}");
                                }
                            }
                        }
                        finally
                        {
                            try { vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView(); }
                            catch { }
                        }
                        DiagLog($"설치도 ISO 연결 이름 노트 parts={noteGroups.Count} " +
                                $"areas={sheet.InstallationConnections.Count} created={createdConnectionNotes}");
                    }

                    // SDK 1.0.26.723: 부재번호 풍선과 연결부재 이름 라벨을 같은 모델 외곽 영역으로 정렬한다.
                    // 실제 표시 객체의 외곽은 기준점으로만 쓰고, 외곽과 라벨 사이 거리는 도면 고정 20mm로 둔다.
                    int isoReviewCount = convertedNoteIndices.Count + createdIsoConnectionNameNotes;
                    if (viewDir == "ISO" && isoReviewCount > 0)
                    {
                        const float sdkAlignOffset = 0f;

                        try
                        {
                            int alignObjectId = isoFitByDashed && dashedObjId >= 0
                                ? dashedObjId
                                : objId;
                            float objectWidth = 0f;
                            float objectHeight = 0f;
                            float objectCenterX = p.X + p.Width / 2f + isoModelXOffset;
                            float objectCenterY = p.Y + p.Height / 2f + templateYOffset;
                            vizcore3d.Drawing2D.Object2D.GetObjectSize(
                                alignObjectId, ref objectWidth, ref objectHeight);
                            vizcore3d.Drawing2D.Object2D.GetObjectCenter(
                                alignObjectId, ref objectCenterX, ref objectCenterY);

                            bool validObjectBounds =
                                objectWidth > 0f && objectHeight > 0f &&
                                !float.IsNaN(objectWidth) && !float.IsInfinity(objectWidth) &&
                                !float.IsNaN(objectHeight) && !float.IsInfinity(objectHeight) &&
                                !float.IsNaN(objectCenterX) && !float.IsInfinity(objectCenterX) &&
                                !float.IsNaN(objectCenterY) && !float.IsInfinity(objectCenterY);
                            if (validObjectBounds)
                            {
                                var rectMin = new VIZCore3D.NET.Data.Vertex3D(
                                    objectCenterX - objectWidth / 2f - IsoReviewGapCanvas,
                                    objectCenterY - objectHeight / 2f - IsoReviewGapCanvas,
                                    0f);
                                var rectMax = new VIZCore3D.NET.Data.Vertex3D(
                                    objectCenterX + objectWidth / 2f + IsoReviewGapCanvas,
                                    objectCenterY + objectHeight / 2f + IsoReviewGapCanvas,
                                    0f);

                                vizcore3d.Drawing2D.Object2D.Set2DViewAlignAreaReviewsPositionByOffset(
                                    rectMin, rectMax, sdkAlignOffset);
                                DiagLog($"P2 ISO 리뷰 영역 정렬 sheet={sheet.SheetNumber} " +
                                        $"balloons={convertedNoteIndices.Count} connectionNames={createdIsoConnectionNameNotes} " +
                                        $"basis=objectBounds obj={alignObjectId} size=({objectWidth:F1}x{objectHeight:F1}) " +
                                        $"rect=({rectMin.X:F1},{rectMin.Y:F1})~({rectMax.X:F1},{rectMax.Y:F1}) " +
                                        $"gapCanvas={IsoReviewGapCanvas:F1} sdkOffset={sdkAlignOffset:F1}");
                            }
                            else
                            {
                                DiagLog($"P2 ISO 리뷰 영역 정렬 WARN sheet={sheet.SheetNumber} " +
                                        $"invalidObjectBounds obj={alignObjectId} " +
                                        $"center=({objectCenterX:F1},{objectCenterY:F1}) size=({objectWidth:F1}x{objectHeight:F1})");
                            }
                        }
                        catch (Exception ex)
                        {
                            DiagLog($"P2 ISO 리뷰 영역 정렬 WARN sheet={sheet.SheetNumber} " +
                                    $"balloons={convertedNoteIndices.Count} connectionNames={createdIsoConnectionNameNotes} " +
                                    $"{ex.Message}");
                        }
                    }

                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);

                    // ── 치수(Measure) → 2D (X/Y/Z만) ──
                    //   작은 치수 텍스트는 시프트하지 않는다 — ShowAllDimensions가 치수선째 2단 승격 (2026-07-03 사용자 사양).
                    if (viewDir != "ISO")
                    {
                        var measureItems = vizcore3d.Review.Measure.Items;
                        var measureIds = new List<int>();
                        foreach (var m in measureItems)
                            if (m.Visible) measureIds.Add(m.ID);

                        if (measureIds.Count > 0)
                            vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
                    }

                    viewsRendered++;
                    ReleaseActiveDrawingReferenceAxis(
                        $"sheet={sheet.SheetNumber} view={viewDir} complete");
                    ProcessCancelableUiCheckpoint(
                        $"{GetSheetKindLabel(sheet)} 2D 생성 중... {viewDir} 뷰 완료 ({viewsRendered}/{cameraMap.Count})",
                        $"sheet#{sheet.SheetNumber} {viewDir} 뷰 완료 후");
                }

                // 정렬된 풍선·연결 이름 노트와 치수를 최종 캔버스에 반영한다.
                ProcessCancelableUiCheckpoint(
                    $"{GetSheetKindLabel(sheet)} 2D 생성 중... 최종 렌더",
                    $"sheet#{sheet.SheetNumber} 최종 렌더 전");
                vizcore3d.Drawing2D.Render();
                ProcessCancelableUiCheckpoint(
                    $"{GetSheetKindLabel(sheet)} 2D 생성 완료",
                    $"sheet#{sheet.SheetNumber} 최종 렌더 후");

                // 3D 뷰 기본(부드러운 음영) 복원 — 도면 생성 후 은선/X-Ray 잔존 방지 (2026-06-23)
                vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                if (vizcore3d.View.XRay.Enable) vizcore3d.View.XRay.Enable = false;

                DiagLog($"P2 GenerateSheetDrawing2D_WithExcelTemplate 완료 — sheet#={sheet.SheetNumber} views={viewsRendered}/{cameraMap.Count}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                DiagLog($"P2 GenerateSheetDrawing2D_WithExcelTemplate ERROR: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                ReleaseActiveDrawingReferenceAxis(
                    $"sheet={(sheet != null ? sheet.SheetNumber : -1)} finally");
            }
        }

        private const float DrawingReferenceAxisToleranceDegrees = 1.0f;
        private const float DrawingReferenceMinimumLineLength = 1.0f;

        /// <summary>
        /// VIZCore3D+.NET Demo의 '참조축 정렬 → 선택 부재 자동 정렬'과 같은 방식으로
        /// 선택 영역 안에서 가장 긴 LINE 모서리를 찾는다. 제작도는 Z-up 도면 규약이므로
        /// 선분의 XY 투영을 로컬 X축으로 사용하고 로컬 Y축은 수평 직교축으로 만든다.
        /// </summary>
        private DrawingReferenceFrame TryBuildDrawingReferenceFrame(List<int> memberIndices)
        {
            if (memberIndices == null || memberIndices.Count == 0) return null;

            double longest = DrawingReferenceMinimumLineLength;
            VIZCore3D.NET.Data.Vector3D longestStart = null;
            VIZCore3D.NET.Data.Vector3D longestEnd = null;
            int sourceNodeIndex = -1;
            var worldPoints = new List<VIZCore3D.NET.Data.Vector3D>();

            foreach (int nodeIndex in memberIndices.Distinct())
            {
                try
                {
                    var osnaps = vizcore3d.Object3D.GetOsnapPoint(nodeIndex);
                    if (osnaps == null) continue;
                    foreach (var osnap in osnaps)
                    {
                        if (osnap.Kind == VIZCore3D.NET.Data.OsnapKind.LINE &&
                            osnap.Start != null && osnap.End != null)
                        {
                            var start = new VIZCore3D.NET.Data.Vector3D(
                                osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                            var end = new VIZCore3D.NET.Data.Vector3D(
                                osnap.End.X, osnap.End.Y, osnap.End.Z);
                            worldPoints.Add(start);
                            worldPoints.Add(end);

                            double dx = end.X - start.X;
                            double dy = end.Y - start.Y;
                            double horizontalLength = Math.Sqrt(dx * dx + dy * dy);
                            if (horizontalLength > longest)
                            {
                                longest = horizontalLength;
                                longestStart = start;
                                longestEnd = end;
                                sourceNodeIndex = nodeIndex;
                            }
                        }
                        else if (osnap.Kind == VIZCore3D.NET.Data.OsnapKind.POINT &&
                                 osnap.Center != null)
                        {
                            worldPoints.Add(new VIZCore3D.NET.Data.Vector3D(
                                osnap.Center.X, osnap.Center.Y, osnap.Center.Z));
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiagLog($"[DrawingRefAxis] osnap WARN node={nodeIndex}: {ex.Message}");
                }
            }

            if (longestStart == null || longestEnd == null || worldPoints.Count < 2)
            {
                DiagLog("[DrawingRefAxis] 기준 선분 없음 → 기존 세계축 유지");
                return null;
            }

            double ux = (longestEnd.X - longestStart.X) / longest;
            double uy = (longestEnd.Y - longestStart.Y) / longest;
            // 같은 선분의 시작/끝 순서가 바뀌어도 카메라가 180° 뒤집히지 않도록 대표 부호를 고정한다.
            if ((Math.Abs(ux) >= Math.Abs(uy) && ux < 0.0) ||
                (Math.Abs(uy) > Math.Abs(ux) && uy < 0.0))
            {
                ux = -ux;
                uy = -uy;
            }

            double nearestWorldAxis = Math.Max(Math.Abs(ux), Math.Abs(uy));
            nearestWorldAxis = Math.Max(-1.0, Math.Min(1.0, nearestWorldAxis));
            float tiltDegrees = (float)(Math.Acos(nearestWorldAxis) * 180.0 / Math.PI);
            if (tiltDegrees <= DrawingReferenceAxisToleranceDegrees)
            {
                DiagLog($"[DrawingRefAxis] 세계축 정렬 상태 node={sourceNodeIndex} " +
                        $"nearest={tiltDegrees:F2}° → 기존 경로");
                return null;
            }

            VIZCore3D.NET.Data.Vector3D origin;
            try
            {
                var bounds = vizcore3d.Object3D.GetBoundBox(memberIndices, false);
                origin = bounds != null
                    ? new VIZCore3D.NET.Data.Vector3D(
                        (bounds.MinX + bounds.MaxX) / 2f,
                        (bounds.MinY + bounds.MaxY) / 2f,
                        (bounds.MinZ + bounds.MaxZ) / 2f)
                    : null;
            }
            catch
            {
                origin = null;
            }
            if (origin == null)
            {
                origin = new VIZCore3D.NET.Data.Vector3D(
                    worldPoints.Average(p => p.X),
                    worldPoints.Average(p => p.Y),
                    worldPoints.Average(p => p.Z));
            }

            var frame = new DrawingReferenceFrame
            {
                XAxis = new VIZCore3D.NET.Data.Vector3D((float)ux, (float)uy, 0f),
                YAxis = new VIZCore3D.NET.Data.Vector3D((float)-uy, (float)ux, 0f),
                ZAxis = new VIZCore3D.NET.Data.Vector3D(0f, 0f, 1f),
                Origin = origin,
                AlignmentAngleDegrees = (float)(Math.Atan2(uy, ux) * 180.0 / Math.PI),
                SourceNodeIndex = sourceNodeIndex,
                MinX = float.MaxValue,
                MinY = float.MaxValue,
                MinZ = float.MaxValue,
                MaxX = float.MinValue,
                MaxY = float.MinValue,
                MaxZ = float.MinValue
            };

            foreach (var worldPoint in worldPoints)
            {
                var local = DrawingReferenceWorldToLocal(
                    new VIZCore3D.NET.Data.Vertex3D(worldPoint.X, worldPoint.Y, worldPoint.Z),
                    frame);
                frame.MinX = Math.Min(frame.MinX, local.X);
                frame.MinY = Math.Min(frame.MinY, local.Y);
                frame.MinZ = Math.Min(frame.MinZ, local.Z);
                frame.MaxX = Math.Max(frame.MaxX, local.X);
                frame.MaxY = Math.Max(frame.MaxY, local.Y);
                frame.MaxZ = Math.Max(frame.MaxZ, local.Z);
            }

            DiagLog($"[DrawingRefAxis] frame node={sourceNodeIndex} longestXY={longest:F1} " +
                    $"angle={frame.AlignmentAngleDegrees:F2}° nearestWorld={tiltDegrees:F2}° " +
                    $"X=({frame.XAxis.X:F5},{frame.XAxis.Y:F5},{frame.XAxis.Z:F5}) " +
                    $"Y=({frame.YAxis.X:F5},{frame.YAxis.Y:F5},{frame.YAxis.Z:F5})");
            return frame;
        }

        private bool ActivateDrawingReferenceAxis(
            DrawingReferenceFrame frame,
            VIZCore3D.NET.Data.CameraDirection cameraDirection,
            int sheetNumber,
            string viewDirection)
        {
            if (frame == null) return false;
            ReleaseActiveDrawingReferenceAxis("replace");
            try
            {
                int referenceAxisId = vizcore3d.View.ReferenceAxis.Create(
                    frame.XAxis, frame.YAxis, frame.Origin,
                    $"제작도 시트축 {sheetNumber}");
                if (referenceAxisId < 0)
                    throw new InvalidOperationException("ReferenceAxis.Create가 유효하지 않은 ID를 반환했습니다.");

                _drawingActiveReferenceAxisId = referenceAxisId;
                vizcore3d.View.ReferenceAxis.Activate(referenceAxisId, true);
                vizcore3d.View.MoveCamera(cameraDirection);
                DiagLog($"[DrawingRefAxis] activate sheet={sheetNumber} view={viewDirection} " +
                        $"camera={cameraDirection} id={referenceAxisId}");
                return true;
            }
            catch (Exception ex)
            {
                ReleaseActiveDrawingReferenceAxis("activate failed");
                vizcore3d.View.MoveCamera(cameraDirection);
                DiagLog($"[DrawingRefAxis] activate FAIL sheet={sheetNumber} view={viewDirection} " +
                        $"→ 기존 세계축 폴백: {ex.Message}");
                return false;
            }
        }

        private void ReleaseActiveDrawingReferenceAxis(string reason)
        {
            if (_drawingActiveReferenceAxisId < 0) return;
            int referenceAxisId = _drawingActiveReferenceAxisId;
            _drawingActiveReferenceAxisId = -1;
            try { vizcore3d.View.ReferenceAxis.Reset(); }
            catch (Exception ex)
            {
                DiagLog($"[DrawingRefAxis] reset WARN id={referenceAxisId} reason={reason}: {ex.Message}");
            }
            try { vizcore3d.Review.Delete(referenceAxisId); }
            catch (Exception ex)
            {
                DiagLog($"[DrawingRefAxis] delete WARN id={referenceAxisId} reason={reason}: {ex.Message}");
            }
            DiagLog($"[DrawingRefAxis] release id={referenceAxisId} reason={reason}");
        }

        private VIZCore3D.NET.Data.Vertex3D DrawingReferenceWorldToLocal(
            VIZCore3D.NET.Data.Vertex3D world,
            DrawingReferenceFrame frame)
        {
            float dx = world.X - frame.Origin.X;
            float dy = world.Y - frame.Origin.Y;
            float dz = world.Z - frame.Origin.Z;
            return new VIZCore3D.NET.Data.Vertex3D(
                dx * frame.XAxis.X + dy * frame.XAxis.Y + dz * frame.XAxis.Z,
                dx * frame.YAxis.X + dy * frame.YAxis.Y + dz * frame.YAxis.Z,
                dx * frame.ZAxis.X + dy * frame.ZAxis.Y + dz * frame.ZAxis.Z);
        }

        private VIZCore3D.NET.Data.Vertex3D DrawingReferenceLocalToWorld(
            VIZCore3D.NET.Data.Vertex3D local,
            DrawingReferenceFrame frame)
        {
            return new VIZCore3D.NET.Data.Vertex3D(
                frame.Origin.X +
                    local.X * frame.XAxis.X + local.Y * frame.YAxis.X + local.Z * frame.ZAxis.X,
                frame.Origin.Y +
                    local.X * frame.XAxis.Y + local.Y * frame.YAxis.Y + local.Z * frame.ZAxis.Y,
                frame.Origin.Z +
                    local.X * frame.XAxis.Z + local.Y * frame.YAxis.Z + local.Z * frame.ZAxis.Z);
        }

        /// <summary>
        /// 시트의 실제 표시 대상. 설치도는 선택 STRU에 직접 연결된 외부 Part만 추가한다.
        /// </summary>
        private List<int> GetDrawingSheetDisplayIndices(DrawingSheetData sheet)
        {
            var result = new List<int>();
            if (sheet == null) return result;
            if (sheet.MemberIndices != null) result.AddRange(sheet.MemberIndices);
            if (sheet.BaseMemberIndex == -2 && sheet.InstallationContextIndices != null)
                result.AddRange(sheet.InstallationContextIndices);
            return result.Where(index => index >= 0).Distinct().ToList();
        }

        private VIZCore3D.NET.Data.Vector3D GetInstallationNoteLabelPoint(
            VIZCore3D.NET.Data.Vector3D target, string viewDirection, float offset)
        {
            switch (viewDirection)
            {
                case "X": return new VIZCore3D.NET.Data.Vector3D(target.X, target.Y + offset, target.Z);
                case "Y": return new VIZCore3D.NET.Data.Vector3D(target.X + offset, target.Y, target.Z);
                case "Z": return new VIZCore3D.NET.Data.Vector3D(target.X + offset, target.Y, target.Z);
                default: return new VIZCore3D.NET.Data.Vector3D(target.X + offset, target.Y, target.Z);
            }
        }

        /// <summary>
        /// 제작도(Sheet1) ISO 점선 대상 — Bounding Box 근접 후보에 대한 전용 Clash 결과의 연결 Part를 반환한다.
        /// 현재 시트가 검사 당시 제작 대상과 다르면 오래된 결과를 사용하지 않는다.
        /// </summary>
        private List<int> GetClashNeighborPartsOutsideSheet(List<int> sheetMemberIndices)
        {
            if (sheetMemberIndices == null || sheetMemberIndices.Count == 0 ||
                fabricationTargetBodyIndices == null ||
                fabricationNeighborPartIndices == null)
                return new List<int>();

            var sheetBodies = new HashSet<int>(sheetMemberIndices);
            if (!fabricationTargetBodyIndices.SetEquals(sheetBodies))
            {
                DiagLog($"P2 ISO 제작도 연결 결과 불일치: " +
                        $"tested={fabricationTargetBodyIndices.Count} sheet={sheetBodies.Count}");
                return new List<int>();
            }

            return fabricationNeighborPartIndices.OrderBy(index => index).ToList();
        }

        /// <summary>
        /// 제작도 연결 Clash 결과를 가장 가까운 상위 Assembly 단위로 묶어 이름과 대표 HotPoint를 반환한다.
        /// 같은 Assembly의 여러 Part가 닿아도 도면에는 이름을 한 번만 표시한다.
        /// </summary>
        private List<FabricationNeighborAssemblyNote> GetFabricationNeighborAssemblyNotes(
            List<int> sheetMemberIndices)
        {
            var notes = new List<FabricationNeighborAssemblyNote>();
            if (sheetMemberIndices == null || sheetMemberIndices.Count == 0 ||
                fabricationNeighborClashList == null || fabricationTargetBodyIndices == null ||
                fabricationTargetPartIndices == null || fabricationNeighborPartIndices == null)
                return notes;

            var sheetBodies = new HashSet<int>(sheetMemberIndices);
            if (!fabricationTargetBodyIndices.SetEquals(sheetBodies)) return notes;

            var addedAssemblyIndices = new HashSet<int>();
            foreach (ClashData clash in fabricationNeighborClashList)
            {
                if (clash == null || !clash.HasHotPoint) continue;

                bool firstIsTarget = fabricationTargetPartIndices.Contains(clash.Index1);
                bool secondIsTarget = fabricationTargetPartIndices.Contains(clash.Index2);
                if (firstIsTarget == secondIsTarget) continue;

                int neighborPartIndex = firstIsTarget ? clash.Index2 : clash.Index1;
                if (!fabricationNeighborPartIndices.Contains(neighborPartIndex)) continue;

                VIZCore3D.NET.Data.Node neighborPart = null;
                try { neighborPart = vizcore3d.Object3D.FromIndex(neighborPartIndex); }
                catch { }

                VIZCore3D.NET.Data.Node assembly = FindParentStru(neighborPart) ?? FindNearestParentAssembly(neighborPart);   // #45 연결부재 STRU 단위
                int assemblyIndex = assembly != null ? assembly.Index : neighborPartIndex;
                if (!addedAssemblyIndices.Add(assemblyIndex)) continue;

                string fallbackName = firstIsTarget ? clash.Name2 : clash.Name1;
                string assemblyName = assembly != null ? assembly.NodeName : null;
                if (string.IsNullOrWhiteSpace(assemblyName) && neighborPart != null)
                    assemblyName = neighborPart.NodeName;
                if (string.IsNullOrWhiteSpace(assemblyName))
                    assemblyName = string.IsNullOrWhiteSpace(fallbackName) ? $"Node_{neighborPartIndex}" : fallbackName;

                notes.Add(new FabricationNeighborAssemblyNote
                {
                    AssemblyIndex = assemblyIndex,
                    AssemblyName = assemblyName,
                    X = clash.XValue,
                    Y = clash.YValue,
                    Z = clash.ZValue
                });
            }

            return notes;
        }

        /// <summary>
        /// Part에서 부모 방향으로 올라가며 가장 가까운 Assembly 노드를 찾는다.
        /// </summary>
        private VIZCore3D.NET.Data.Node FindNearestParentAssembly(VIZCore3D.NET.Data.Node part)
        {
            if (part == null || part.Kind != VIZCore3D.NET.Data.NodeKind.PART) return null;

            int currentIndex = part.ParentIndex;
            var visited = new HashSet<int>();
            while (currentIndex >= 0 && visited.Add(currentIndex))
            {
                VIZCore3D.NET.Data.Node current;
                try { current = vizcore3d.Object3D.FromIndex(currentIndex); }
                catch { return null; }

                if (current == null) return null;
                if (current.Kind == VIZCore3D.NET.Data.NodeKind.ASSEMBLY) return current;
                if (current.ParentIndex == currentIndex) return null;

                currentIndex = current.ParentIndex;
            }

            return null;
        }

        /// <summary>
        /// #45: 연결부재가 속한 STRU 노드를 부모로 올라가며 찾는다.
        /// STRU 식별은 이미 수집된 _struNodeCache(모델 로드 시 CollectStruList) 집합으로 판정.
        /// STRU 조상이 없으면 null → 호출부에서 FindNearestParentAssembly로 폴백.
        /// </summary>
        private VIZCore3D.NET.Data.Node FindParentStru(VIZCore3D.NET.Data.Node part)
        {
            if (part == null || _struNodeCache == null || _struNodeCache.Count == 0) return null;

            var struIndexSet = new HashSet<int>();
            foreach (var s in _struNodeCache)
                if (s != null) struIndexSet.Add(s.Index);

            int currentIndex = part.ParentIndex;
            var visited = new HashSet<int>();
            while (currentIndex >= 0 && visited.Add(currentIndex))
            {
                VIZCore3D.NET.Data.Node current;
                try { current = vizcore3d.Object3D.FromIndex(currentIndex); }
                catch { return null; }
                if (current == null) return null;
                if (struIndexSet.Contains(current.Index)) return current;   // STRU 도달
                if (current.ParentIndex == currentIndex) return null;
                currentIndex = current.ParentIndex;
            }
            return null;
        }

        /// <summary>
        /// T-038+039: 그리드 셀(row, col)에 viewDirection으로 모델 배치 시 *예상* fit scale 추정.
        /// 사용자 사양: 보조선 끝점이 캔버스 절대 50mm(1단) / 100mm(2단) 고정 →
        /// 모델 좌표 보조선 offset = 50/scale, 100/scale.
        /// ShowAllDimensions가 RescaleObject보다 *먼저* 호출되므로 사전 추정 필요.
        /// 추정식 = min((cellW - margins) * 0.8 / modelW_2dProj, (cellH - margins) * 0.8 / modelH_2dProj).
        /// </summary>
        private float EstimateFitScaleForCell(int row, int col, string viewDirection, List<int> memberIndices)
        {
            float cellW = vizcore3d.Drawing2D.GridStructure.GetGridCellWidth(row, col);
            float cellH = vizcore3d.Drawing2D.GridStructure.GetGridCellHeight(row, col);
            float marginL = vizcore3d.Drawing2D.GridStructure.GetGridCellLeftMargin(row, col);
            float marginR = vizcore3d.Drawing2D.GridStructure.GetGridCellRightMargin(row, col);
            float marginT = vizcore3d.Drawing2D.GridStructure.GetGridCellTopMargin(row, col);
            float marginB = vizcore3d.Drawing2D.GridStructure.GetGridCellBottomMargin(row, col);
            float availW = cellW - marginL - marginR;
            float availH = cellH - marginT - marginB;

            // 모델 BBox 합 (bomList에서 memberIndices 부재만)
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            if (bomList != null && memberIndices != null && memberIndices.Count > 0)
            {
                var idxSet = new HashSet<int>(memberIndices);
                foreach (var b in bomList)
                {
                    if (!idxSet.Contains(b.Index)) continue;
                    if (b.MinX < minX) minX = b.MinX;
                    if (b.MaxX > maxX) maxX = b.MaxX;
                    if (b.MinY < minY) minY = b.MinY;
                    if (b.MaxY > maxY) maxY = b.MaxY;
                    if (b.MinZ < minZ) minZ = b.MinZ;
                    if (b.MaxZ > maxZ) maxZ = b.MaxZ;
                }
            }
            if (maxX == float.MinValue) return 1f;  // 안전장치

            float modelW, modelH;
            switch (viewDirection)
            {
                case "X": modelW = maxY - minY; modelH = maxZ - minZ; break;
                case "Y": modelW = maxX - minX; modelH = maxZ - minZ; break;
                default:  modelW = maxX - minX; modelH = maxY - minY; break;  // "Z"/null
            }
            if (modelW < 1e-3f || modelH < 1e-3f) return 1f;

            float scaleW = (availW * 0.8f) / modelW;
            float scaleH = (availH * 0.8f) / modelH;
            float scale = Math.Min(scaleW, scaleH);
            DiagLog($"T-038+039 EstimateFitScaleForCell row={row} col={col} view={viewDirection} cell=({cellW:F1},{cellH:F1}) model=({modelW:F1},{modelH:F1}) scale={scale:F4}");
            return scale > 0f ? scale : 1f;
        }

        /// <summary>
        /// 빌드 출력 폴더의 도면 리소스를 우선 사용하고, 개발 환경에서는 솔루션 루트 assets\ 를 fallback으로 사용한다.
        /// </summary>
        /// <summary>
        /// STRU 단위 UDA "STRU" 값 조회 (TAG NO용, 사용자 2026-07-21).
        /// 기준부재에서 부모로 최대 10단계 walk-up하며 "STRU" 키를 찾는다 (GetSprefValue와 동일 패턴).
        /// STRU 노드는 기준부재의 조상 어셈블리이므로 walk-up으로 도달한다. 없으면 "".
        /// </summary>
        private string GetStruUdaValue(int nodeIndex)
        {
            List<string> udaKeyList = null;
            try
            {
                var keys = vizcore3d.Object3D.UDA.Keys;
                if (keys != null && keys.Count > 0)
                    udaKeyList = new List<string>(keys);
            }
            catch { }

            if (udaKeyList == null) return "";

            int currentIdx = nodeIndex;
            for (int depth = 0; depth < 10; depth++)
            {
                if (currentIdx < 0) break;

                foreach (string key in udaKeyList)
                {
                    if (key.Trim().ToUpper() != "STRU") continue;
                    try
                    {
                        var val = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                        string valStr = (val != null) ? val.ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(valStr))
                            return valStr;
                    }
                    catch { }
                }

                try
                {
                    VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                    if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                    currentIdx = parentNode.ParentIndex;
                }
                catch { break; }
            }
            return "";
        }

        /// <summary>
        /// 기준부재에서 부모로 최대 10단계 올라가며 키 이름에 PNT가 포함된
        /// STRU UDA 값을 찾는다. 복수 후보는 실제 값과 함께 모두 로그에 남기고,
        /// 가장 먼저 발견된 비어 있지 않은 값을 PAINT CODE로 사용한다.
        /// </summary>
        private string GetStruPntUdaValue(int nodeIndex)
        {
            var pntKeys = new List<string>();
            try
            {
                var keys = vizcore3d.Object3D.UDA.Keys;
                if (keys != null)
                {
                    foreach (string key in keys)
                    {
                        if (!string.IsNullOrWhiteSpace(key) &&
                            key.IndexOf("PNT", StringComparison.OrdinalIgnoreCase) >= 0)
                            pntKeys.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[PAINT CODE] PNT UDA 키 조회 실패: {ex.Message}");
            }

            if (pntKeys.Count == 0)
            {
                DiagLog($"[PAINT CODE] PNT UDA 키 후보 없음: startNode={nodeIndex}");
                return "";
            }

            DiagLog($"[PAINT CODE] PNT UDA 키 후보: {string.Join(", ", pntKeys)}");
            int currentIdx = nodeIndex;
            for (int depth = 0; depth < 10 && currentIdx >= 0; depth++)
            {
                string selectedKey = "";
                string selectedValue = "";
                foreach (string key in pntKeys)
                {
                    string value = "";
                    try
                    {
                        var raw = vizcore3d.Object3D.UDA.FromIndex(currentIdx, key);
                        value = raw != null ? raw.ToString().Trim() : "";
                    }
                    catch (Exception ex)
                    {
                        DiagLog($"[PAINT CODE] UDA 읽기 실패: node={currentIdx} key='{key}' {ex.Message}");
                    }

                    DiagLog($"[PAINT CODE] 후보: depth={depth} node={currentIdx} key='{key}' value='{value}'");
                    if (string.IsNullOrEmpty(selectedValue) && !string.IsNullOrEmpty(value))
                    {
                        selectedKey = key;
                        selectedValue = value;
                    }
                }

                if (!string.IsNullOrEmpty(selectedValue))
                {
                    DiagLog($"[PAINT CODE] 선택: node={currentIdx} key='{selectedKey}' value='{selectedValue}'");
                    return selectedValue;
                }

                try
                {
                    VIZCore3D.NET.Data.Node parentNode = vizcore3d.Object3D.FromIndex(currentIdx);
                    if (parentNode == null || parentNode.ParentIndex == currentIdx) break;
                    currentIdx = parentNode.ParentIndex;
                }
                catch { break; }
            }

            DiagLog($"[PAINT CODE] 비어 있지 않은 PNT UDA 값 없음: startNode={nodeIndex}");
            return "";
        }

        /// <summary>
        /// 같은 도면 목록에서 생성되는 제작도·조립도·설치도·가공도가 PAINT CODE를 공유하도록
        /// 출력 시점에 한 번만 조회하고 모든 DrawingSheetData에 캐시한다.
        /// null은 미조회, 빈 문자열은 조회했지만 값 없음으로 구분해 빈 모델도 재조회하지 않는다.
        /// </summary>
        private string GetOrCacheDrawingPaintCode(DrawingSheetData sourceSheet, int preferredNodeIndex = -1)
        {
            List<DrawingSheetData> relatedSheets;
            if (sourceSheet != null && drawingSheetList != null && drawingSheetList.Contains(sourceSheet))
                relatedSheets = drawingSheetList.Where(item => item != null).ToList();
            else
                relatedSheets = sourceSheet != null
                    ? new List<DrawingSheetData> { sourceSheet }
                    : new List<DrawingSheetData>();

            DrawingSheetData cachedSheet = relatedSheets.FirstOrDefault(item => item.PaintCode != null);
            if (cachedSheet != null)
            {
                DiagLog($"[PAINT CODE] 도면 공용 캐시 재사용: sheets={relatedSheets.Count} " +
                        $"value='{cachedSheet.PaintCode}'");
                return cachedSheet.PaintCode;
            }

            int lookupNodeIndex = preferredNodeIndex > 0 ? preferredNodeIndex : -1;
            if (lookupNodeIndex < 0 && bomList != null && bomList.Count > 0)
                lookupNodeIndex = bomList[0].Index;
            if (lookupNodeIndex < 0 && sourceSheet != null && sourceSheet.MemberIndices.Count > 0)
                lookupNodeIndex = sourceSheet.MemberIndices[0];

            string paintCode = GetStruPntUdaValue(lookupNodeIndex);
            foreach (DrawingSheetData drawingSheet in relatedSheets)
                drawingSheet.PaintCode = paintCode;
            if (sourceSheet != null) sourceSheet.PaintCode = paintCode;

            DiagLog($"[PAINT CODE] 도면 공용 캐시 생성: startNode={lookupNodeIndex} " +
                    $"sheets={relatedSheets.Count} value='{paintCode}'");
            return paintCode;
        }

        /// <summary>
        /// 배포 패키지에서 엑셀 템플릿이 놓이는 실행 폴더 하위 폴더.
        /// csproj Content 항목의 Link 경로와 반드시 일치해야 한다 (A2Z.csproj).
        /// </summary>
        private const string TemplateOutputFolderName = "templates";

        /// <summary>
        /// 도면 리소스 경로 해결 — **실행 폴더 우선, 솔루션 폴더는 개발 편의용 폴백**.
        /// 배포 패키지(exe + 리소스만, .sln 없음)에서도 리소스를 찾게 하는 공통 기반이다.
        /// (2026-07-28 #71) 이전에는 GetSolutionPath()로 .sln을 찾아 레포 루트를 기준으로 삼았기에
        ///   .sln이 없는 배포 환경에서 템플릿·이미지를 전부 놓쳤다.
        /// </summary>
        /// <param name="fileName">파일명 (경로 없이)</param>
        /// <param name="outputSubDir">실행 폴더 기준 배포 표준 하위 폴더 (루트 배치면 null)</param>
        /// <param name="solutionSubDir">솔루션 폴더 기준 하위 폴더 (레포 루트 직속이면 null)</param>
        private string ResolveDrawingResourcePath(string fileName, string outputSubDir, string solutionSubDir)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // ── 1순위: 실행 폴더 — 배포 패키지에서 유일하게 유효한 경로 ──
            var outputCandidates = new List<string>();
            if (!string.IsNullOrEmpty(outputSubDir))
                outputCandidates.Add(Path.Combine(baseDir, outputSubDir, fileName));
            outputCandidates.Add(Path.Combine(baseDir, fileName));      // 하위 폴더 규칙 이전 배포본 호환
            if (!string.IsNullOrEmpty(solutionSubDir))
                outputCandidates.Add(Path.Combine(baseDir, solutionSubDir, fileName));

            foreach (string candidate in outputCandidates)
            {
                string full = Path.GetFullPath(candidate);
                if (File.Exists(full)) return full;
            }

            // ── 2순위: 솔루션 폴더 — 개발 PC 전용 폴백 (.sln 탐색 비용은 여기서만 발생) ──
            string devPath = Path.GetFullPath(string.IsNullOrEmpty(solutionSubDir)
                ? Path.Combine(GetSolutionPath(), fileName)
                : Path.Combine(GetSolutionPath(), solutionSubDir, fileName));
            if (File.Exists(devPath)) return devPath;

            // 어디에도 없음 — 배포 표준 위치를 돌려줘 에러 메시지가 "있어야 할 자리"를 가리키게 한다
            return Path.GetFullPath(outputCandidates[0]);
        }

        /// <summary>도면 이미지 리소스 — 실행 폴더 루트 → 실행 폴더 assets\ → 솔루션 assets\</summary>
        private string ResolveDrawingAssetPath(string fileName)
        {
            return ResolveDrawingResourcePath(fileName, null, "assets");
        }

        /// <summary>도면 엑셀 템플릿 — 실행 폴더 templates\ → 실행 폴더 루트 → 솔루션 루트</summary>
        private string ResolveDrawingTemplatePath(string fileName)
        {
            return ResolveDrawingResourcePath(fileName, TemplateOutputFolderName, null);
        }

        /// <summary>
        /// 엑셀의 이미지 전용 View 영역에 TemplateTableData 이미지 셀을 직접 렌더링한다.
        /// 이미지 렌더링에 실패해도 도면 출력은 계속하고 로그만 남긴다.
        /// </summary>
        private bool PlaceImageInTemplateArea(
            string imagePath,
            VIZCore3D.NET.Data.TemplateViewArea area,
            float margin = 1f)
        {
            if (area == null)
            {
                DiagLog($"P2 이미지 영역 없음: {Path.GetFileName(imagePath)}");
                return false;
            }
            if (!System.IO.File.Exists(imagePath))
            {
                DiagLog($"P2 이미지 파일 없음: {imagePath}");
                return false;
            }

            try
            {
                float availableWidth = Math.Max(1f, area.Width - (margin * 2f));
                float availableHeight = Math.Max(1f, area.Height - (margin * 2f));
                float targetHeight;
                float targetWidth;
                using (System.Drawing.Image image = System.Drawing.Image.FromFile(imagePath))
                {
                    if (image.Width <= 0 || image.Height <= 0)
                    {
                        DiagLog($"P2 이미지 크기 오류: {imagePath}");
                        return false;
                    }

                    float heightForAvailableWidth =
                        availableWidth * image.Height / image.Width;
                    targetHeight = Math.Min(availableHeight, heightForAvailableWidth);
                    targetWidth = targetHeight * image.Width / image.Height;
                }

                float centerX = area.X + (area.Width / 2f);
                float centerY = area.Y + (area.Height / 2f);
                var imageTable = new VIZCore3D.NET.Data.TemplateTableData(
                    1,
                    1,
                    VIZCore3D.NET.Data.TableHorizontalAnchor.Center,
                    VIZCore3D.NET.Data.TableVerticalAnchor.Middle);
                // RenderTemplate 배치 캘리브레이션 (2026-07-19 실측, PDF 3장 대조):
                //   지정 (X,Y) 대비 실제 이미지 좌상단이 (X−Cx, Y−Cy)에 그려지는데, Cx·Cy가 표 크기에
                //   선형 의존 — 두 이미지(11×11, 17×27)의 실측 4점 fit:
                //     Cx ≈ 28.4 − 0.333×표폭(ColumnWidths),  Cy ≈ 10.2 − 0.123×이미지높이
                //   이미지는 셀 안에서 좌측·상단 정렬이므로, 영역 중앙 배치는 좌상단 목표점(중앙−이미지 절반)에
                //   Cx·Cy를 더해 전달한다. ⚠ 앵커(Center/Middle)를 바꾸면 측정 무효.
                //   구 템플릿(프레임이 A1부터)에선 우연히 안 보였고 신 템플릿(여백 후 프레임)에서 드러남.
                int tableW = Math.Max(1, (int)Math.Floor(availableWidth));
                int imageH = Math.Max(1, (int)Math.Floor(targetHeight));
                float calibX = 28.4f - 0.333f * tableW;
                float calibY = 10.2f - 0.123f * imageH;
                imageTable.X = centerX + calibX - targetWidth / 2f;
                imageTable.Y = centerY + calibY + targetHeight / 2f;
                imageTable.ImageHeight = imageH;
                imageTable.ColumnWidths = new Dictionary<int, int>
                {
                    { 0, tableW }
                };
                imageTable.SetText(0, 0, imagePath);

                vizcore3d.Drawing2D.Template.RenderTemplate(imageTable);
                DiagLog(
                    $"P2 이미지 배치 완료: {Path.GetFileName(imagePath)} " +
                    $"path='{imagePath}' " +
                    $"area=View_{area.Index} center=({centerX:F1},{centerY:F1}) " +
                    $"보정후 XY=({imageTable.X:F1},{imageTable.Y:F1}) imgWH=({targetWidth:F1}x{targetHeight:F1}) " +
                    $"box=({availableWidth:F1}x{availableHeight:F1}) imageH={imageTable.ImageHeight}");
                return true;
            }
            catch (Exception ex)
            {
                DiagLog($"P2 이미지 배치 ERROR: {Path.GetFileName(imagePath)} — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// T-064 P2 본진 (2026-05-14): 엑셀 분기용 viewArea 기반 fit scale 추정.
        /// EstimateFitScaleForCell(GridStructure 셀 기반)과 동일 알고리즘이지만 입력을 viewArea 영역으로.
        /// 사용자 사양: Z=0.65 / X·Y=0.70 (모델 차지 비율). ShowAllDimensions 보조선 위치 계산 기준.
        /// 모델 RescaleObject 시점의 shrinkFactor와 동일 값을 유지해야 보조선이 모델 fit 결과와 일치.
        /// </summary>
        private float EstimateFitScaleForViewArea(
            float availW,
            float availH,
            string viewDirection,
            List<int> memberIndices,
            float fitFactorOverride = -1f,
            DrawingReferenceFrame drawingReferenceFrame = null)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            if (drawingReferenceFrame != null)
            {
                minX = drawingReferenceFrame.MinX; maxX = drawingReferenceFrame.MaxX;
                minY = drawingReferenceFrame.MinY; maxY = drawingReferenceFrame.MaxY;
                minZ = drawingReferenceFrame.MinZ; maxZ = drawingReferenceFrame.MaxZ;
            }
            else if (memberIndices != null && memberIndices.Count > 0)
            {
                try
                {
                    var bounds = vizcore3d.Object3D.GetBoundBox(memberIndices, false);
                    if (bounds != null)
                    {
                        minX = bounds.MinX; maxX = bounds.MaxX;
                        minY = bounds.MinY; maxY = bounds.MaxY;
                        minZ = bounds.MinZ; maxZ = bounds.MaxZ;
                    }
                }
                catch { }
            }
            if (maxX == float.MinValue) return 1f;

            float modelW, modelH;
            switch (viewDirection)
            {
                case "X": modelW = maxY - minY; modelH = maxZ - minZ; break;
                case "Y": modelW = maxX - minX; modelH = maxZ - minZ; break;
                default:  modelW = maxX - minX; modelH = maxY - minY; break;  // "Z"/null
            }
            if (modelW < 1e-3f || modelH < 1e-3f) return 1f;

            // 사용자 사양 (2026-05-14): Z=0.65 / X·Y=0.70 — RescaleObject shrinkFactor와 동일 유지
            float fitFactor = fitFactorOverride > 0f ? fitFactorOverride : ((viewDirection == "Z") ? 0.65f : 0.70f);
            float scaleW = (availW * fitFactor) / modelW;
            float scaleH = (availH * fitFactor) / modelH;
            float scale = Math.Min(scaleW, scaleH);
            DiagLog($"P2 EstimateFitScaleForViewArea view={viewDirection} area=({availW:F1},{availH:F1}) model=({modelW:F1},{modelH:F1}) fitFactor={fitFactor:F2} scale={scale:F4}");
            return scale > 0f ? scale : 1f;
        }

        /// <summary>
        /// 각 그리드 셀별로 3D 상태를 ApplyDrawingSheetView와 동일하게 적용 → 2D 투영
        /// ISO: 풍선번호(CreateIsoBalloonNotes), X/Y/Z: 치수선+보조선+풍선(ShowAllDimensions)
        /// 각 셀 크기의 90% 비율로 중앙 배치
        /// </summary>
        private int RenderSheetViewForDrawing(int row, int col, string viewDirection, DrawingSheetData sheet, float targetHeight = 0f)
        {
            // T-038+039 v9 (2026-05-12): 매 뷰 시작 시 _lastModelShift 초기화.
            // ShowAllDimensions 안 호출되는 ISO 뷰에서 *이전 뷰의 잔존 값*으로 이동되는 버그 차단.
            _lastModelShiftCanvasX = 0f;
            _lastModelShiftCanvasY = 0f;

            List<int> shapeDrawingIds = null;
            List<int> visibleNoteIds = null;  // ISO 뷰 풍선 가시성 필터링용
            int bgObjId = -1;  // Sheet 2+ ISO: 나머지 부재 배경 2D 객체 ID

            // 1. 3D 어노테이션 초기화 (매 뷰마다 새로 그리기)
            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

            // Sheet 2+ ISO 전체 표시 여부 판단
            bool isIsoFullView = (viewDirection == "ISO" && sheet.BaseMemberIndex >= 0);

            // 전체 BOM 인덱스 수집 (ISO 전체 표시용)
            List<int> allBomIndices = new List<int>();
            if (isIsoFullView && bomList != null)
            {
                foreach (var b in bomList)
                    allBomIndices.Add(b.Index);
            }

            // 2. 부재 표시
            vizcore3d.BeginUpdate();

            if (vizcore3d.View.XRay.Enable)
                vizcore3d.View.XRay.Enable = false;
            vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
            vizcore3d.Object3D.Show(sheet.MemberIndices, true);
            xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

            vizcore3d.EndUpdate();

            // 3. 렌더 모드 + 카메라 이동
            vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

            if (viewDirection == "ISO")
            {
                vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
            }
            else
            {
                switch (viewDirection)
                {
                    case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS); break;
                    case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS); break;
                    case "Z": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS); break;
                }

                // ORIENTATION UDA 기반 카메라 회전 (비-ISO 뷰만)
                if (sheet.MemberIndices != null && sheet.MemberIndices.Count > 0)
                    ApplyOrientationRotation(sheet.MemberIndices[0], viewDirection);
            }

            // 셀 크기의 80%로 표시 (줌팩터 1.25 = 1/0.8)
            if (isIsoFullView)
                vizcore3d.View.FlyToObject3d(allBomIndices, 1.25f);  // 전체 모델 기준 줌
            else
                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.25f);

            // 4. 뷰별 3D 어노테이션 추가
            if (viewDirection == "ISO")
            {
                // ISO 풍선 생성을 위해 X-Ray 모드 전환
                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
                vizcore3d.View.XRay.Enable = true;
                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                vizcore3d.View.XRay.Clear();
                vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                vizcore3d.EndUpdate();

                // bomList 기반 풍선 노트 생성 (공유 메서드 호출)
                Dictionary<int, int> nodeToNoteMap = CreateIsoBalloonNotes(sheet.MemberIndices, true);

                // 현재 카메라에서 보이는 노드 추출 + 보이는 풍선만 필터링
                vizcore3d.View.EnableBoxSelectionFrontObjectOnly = true;
                List<VIZCore3D.NET.Data.Node> visibleNodes = vizcore3d.Object3D.FromScreen(false, VIZCore3D.NET.Data.LeafNodeKind.BODY);
                visibleNoteIds = new List<int>();
                foreach (var node in visibleNodes)
                {
                    int noteId;
                    if (nodeToNoteMap.TryGetValue(node.Index, out noteId) || nodeToNoteMap.TryGetValue(node.ParentIndex, out noteId))
                    {
                        if (!visibleNoteIds.Contains(noteId))
                            visibleNoteIds.Add(noteId);
                    }
                }

                // 풍선 생성 후 시트 부재만 보이기 (2D 캡처 준비)
                vizcore3d.BeginUpdate();
                vizcore3d.View.XRay.Enable = false;
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                vizcore3d.EndUpdate();
            }
            else if (sheet.BaseMemberIndex != -2)
            {
                // X/Y/Z: ShowAllDimensions (forDrawing2D=true → 보조선 ShapeDrawing ID 수집)
                // T-038+039 v2: 치수 max 기반 동적 분기 — ShowAllDimensions가 내부에서 결정.
                //   max > 1000mm: 보조선 10/20mm / max ≤ 1000mm: 20/40mm (캔버스 절대 mm).
                // 호출자는 scale만 추정해 전달. ShowAllDimensions가 RescaleObject 전이라 사전 추정 필요.
                float estScale = EstimateFitScaleForCell(row, col, viewDirection,
                    isIsoFullView ? allBomIndices : sheet.MemberIndices);
                shapeDrawingIds = ShowAllDimensions(viewDirection, true, estScale);
            }

            // ── 5. 2패스 2D 투영 (조립도 ISO: 전체−기준 LONG_DASHED 점선 배경 + 기준부재만 실선 — issue #7) ──
            int objId;

            if (isIsoFullView)
            {
                // ── Pass 1: 전체 − 기준부재 → 점선 배경 ──
                var bgIndices = new List<int>();
                foreach (int ix in allBomIndices)
                    if (ix != sheet.BaseMemberIndex) bgIndices.Add(ix);

                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(bgIndices, true);
                vizcore3d.EndUpdate();

                bgObjId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                // 생성 후 점선 + 가는 선으로 변경 (LONG_DASHED — 소프트힐스 예제 기본 파선, issue #7)
                vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineType(bgObjId,
                    VIZCore3D.NET.Data.Object2D_LineTypes.LONG_DASHED);
                vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineThickness(bgObjId, 0.15f);

                // ── Pass 2: 기준부재만 → 전경 (실선, issue #7) ──
                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(new List<int> { sheet.BaseMemberIndex }, true);
                vizcore3d.EndUpdate();

                objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

                // bgObjId를 셀에 배치 + targetHeight 스케일 조정
                vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, bgObjId,
                    VIZCore3D.NET.Data.GridHorizontalAlignment.Center,
                    VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

                if (targetHeight > 0)
                {
                    float curW = 0f, curH = 0f;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(bgObjId, ref curW, ref curH);
                    if (curH > 0)
                    {
                        float ratio = targetHeight / curH;
                        float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                        vizcore3d.Drawing2D.Object2D.RescaleObject(bgObjId, curScale * ratio);

                        float afterW = 0f, afterH = 0f;
                        vizcore3d.Drawing2D.Object2D.GetObjectSize(bgObjId, ref afterW, ref afterH);

                        if (afterH > 30f)
                        {
                            float reRatio = 30f / afterH;
                            float afterScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(bgObjId, afterScale * reRatio);
                            vizcore3d.Drawing2D.Object2D.GetObjectSize(bgObjId, ref afterW, ref afterH);
                        }

                        if (afterW > 30f)
                        {
                            float wRatio = 30f / afterW;
                            float wScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(bgObjId, wScale * wRatio);
                        }
                    }
                }
                else
                {
                    // T-038 step B-3 (2026-05-12): 0.85 → 0.75 (텍스트·풍선 5배 키움 + 보조선 절반 규칙 도입과 같이 적용)
                    float curScaleBg = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                    vizcore3d.Drawing2D.Object2D.RescaleObject(bgObjId, curScaleBg * 0.75f);
                }

                // T-038+039 v4 (2026-05-12): 보조선 반대 방향 모델 이동 (ShowAllDimensions가 _lastModelShift* 채움)
                if (_lastModelShiftCanvasX != 0f || _lastModelShiftCanvasY != 0f)
                {
                    vizcore3d.Drawing2D.Object2D.MoveObject(bgObjId, _lastModelShiftCanvasX, _lastModelShiftCanvasY);
                    DiagLog($"T-038+039 v4 MoveObject bgObjId={bgObjId} dx={_lastModelShiftCanvasX:F1} dy={_lastModelShiftCanvasY:F1}");
                }

                // ── 두 겹 정합 — Match2DObjectsTo3DObjectPosition (SDK 1.0.26.716, issue #7) ──
                //   T-013 수동 정렬(WorldToScreen 3D→캔버스 변환 + 8꼭지점 비율 보정, 옵션 B) 전부 대체.
                //   스케일을 배경 최종 스케일로 통일한 뒤, SDK가 두 캡처의 3D 위치 기준으로 전경을 제 위치에 겹친다.
                float bgFinalScaleB = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                vizcore3d.Drawing2D.Object2D.RescaleObject(objId, bgFinalScaleB);
                bool matched = vizcore3d.Drawing2D.Object2D.Match2DObjectsTo3DObjectPosition(objId, bgObjId);
                DiagLog($"RenderSheet ISO Match bg={bgObjId} obj={objId} scale={bgFinalScaleB:F4} matched={matched}");
            }
            else
            {
                objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

                // 비-ISO 뷰: 단일 객체 셀 배치 + 스케일 조정
                vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, objId,
                    VIZCore3D.NET.Data.GridHorizontalAlignment.Center,
                    VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

                if (targetHeight > 0)
                {
                    float curW = 0f, curH = 0f;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref curW, ref curH);
                    if (curH > 0)
                    {
                        float ratio = targetHeight / curH;
                        float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, curScale * ratio);

                        float afterW = 0f, afterH = 0f;
                        vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref afterW, ref afterH);

                        if (afterH > 30f)
                        {
                            float reRatio = 30f / afterH;
                            float afterScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objId, afterScale * reRatio);
                            vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref afterW, ref afterH);
                        }

                        if (afterW > 30f)
                        {
                            float wRatio = 30f / afterW;
                            float wScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objId, wScale * wRatio);
                        }
                    }
                }
                else
                {
                    // T-038 step B-3 → v9 (2026-05-12 사용자 사양): Z뷰만 0.75 → 0.70 (5% 더 축소)
                    float shrinkFactor = (viewDirection == "Z") ? 0.70f : 0.75f;
                    float curScaleObj = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                    vizcore3d.Drawing2D.Object2D.RescaleObject(objId, curScaleObj * shrinkFactor);
                }

                // 설치도는 모델의 최종 2D 배율이 확정된 뒤 보조선을 생성해 종이 길이를 통일한다.
                if (sheet.BaseMemberIndex == -2)
                {
                    float actualScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                    if (actualScale <= 0f || float.IsNaN(actualScale) || float.IsInfinity(actualScale))
                    {
                        actualScale = EstimateFitScaleForCell(row, col, viewDirection, sheet.MemberIndices);
                        DiagLog($"설치도 구형 2D 실측 배율 fallback view={viewDirection} scale={actualScale:F4}");
                    }
                    // keepCamera: 캡처 시점 카메라(PLUS+ORIENTATION 회전)를 유지 — 내부 MoveCamera가
                    //   회전을 리셋해 Add2D 변환이 모델과 어긋나는 것 방지 (템플릿 경로와 동일 정책, 2026-07-23).
                    shapeDrawingIds = ShowAllDimensions(viewDirection, true, actualScale, keepCamera: true);
                    DiagLog($"설치도 구형 2D 실측 배율 보조선 view={viewDirection} obj={objId} " +
                            $"scale={actualScale:F4} dims={chainDimensionList.Count}");
                }

                // T-038+039 v4 (2026-05-12): 보조선 반대 방향 모델 이동 (ShowAllDimensions가 _lastModelShift* 채움)
                if (_lastModelShiftCanvasX != 0f || _lastModelShiftCanvasY != 0f)
                {
                    vizcore3d.Drawing2D.Object2D.MoveObject(objId, _lastModelShiftCanvasX, _lastModelShiftCanvasY);
                    DiagLog($"T-038+039 v4 MoveObject objId={objId} dx={_lastModelShiftCanvasX:F1} dy={_lastModelShiftCanvasY:F1}");
                }
            }

            // 7. 3D→2D 변환 (스케일 조정 후에 수행 — 풍선/치수가 조정된 모델에 맞게 배치)

            // 보조선(ShapeDrawing) → 2D 개체로 추가 (모델 실선보다 가늘게)
            if (shapeDrawingIds != null && shapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.1f);  // T-040 v6: 0.3→0.1 (극가는 보조선)
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            }

            // 풍선번호(Note) → 2D (텍스트 크기를 작게 설정하여 겹침 방지)
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(10.5f);  // 2026-05-12: 5.25f→10.5f 2배 (풍선 텍스트, 사용자 확정)
            List<int> convertedNoteIndices = new List<int>();
            if (visibleNoteIds != null)
            {
                // ISO: 가시성 필터링된 풍선만 2D로 변환
                if (visibleNoteIds.Count > 0)
                {
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
                    convertedNoteIndices.AddRange(visibleNoteIds);
                }
            }
            else
            {
                // 비-ISO 뷰: 모든 풍선 노트를 2D로 변환
                List<int> noteIds = new List<int>();
                List<VIZCore3D.NET.Data.NoteItem> notes = vizcore3d.Review.Note.Items;
                foreach (var note in notes)
                {
                    noteIds.Add(note.ID);
                }
                if (noteIds.Count > 0)
                {
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                    convertedNoteIndices.AddRange(noteIds);
                }
            }

            // 2D 노트 라벨을 원형 넘버링으로 변경
            foreach (int idx in convertedNoteIndices)
            {
                try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(idx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                catch { }
            }

            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f); // 풍선 텍스트 크기 복원

            // 치수선(Measure) → 2D
            List<int> measureIds = new List<int>();
            List<VIZCore3D.NET.Data.MeasureItem> measures = vizcore3d.Review.Measure.Items;
            foreach (var measure in measures)
            {
                if (measure.Visible)
                    measureIds.Add(measure.ID);
            }
            if (measureIds.Count > 0)
            {
                // 작은 치수 텍스트 시프트 폐기 — ShowAllDimensions가 치수선째 2단 승격 (2026-07-03 사용자 사양)
                vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
            }

            // 7. 시트 부재 표시 복원 (X-Ray 모드로 되돌리기)
            vizcore3d.BeginUpdate();
            vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
            vizcore3d.View.XRay.Enable = true;
            vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
            vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
            vizcore3d.View.XRay.Clear();
            vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
            vizcore3d.EndUpdate();

            return objId;
        }

        #endregion
    }
}
