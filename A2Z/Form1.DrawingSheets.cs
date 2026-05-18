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
        #region 도면 시트 생성 (BFS)

        /// <summary>
        /// Clash 인접 리스트 기반 BFS로 도면 시트 생성
        /// </summary>
        private void GenerateDrawingSheets()
        {
            drawingSheetList.Clear();
            lvDrawingSheet.Items.Clear();

            if (bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sheet 1: 전체 BOM 부재
            DrawingSheetData sheet1 = new DrawingSheetData();
            sheet1.SheetNumber = 1;

            // 선택한 노드 이름 사용, 없으면 파일명 사용
            if (selectedAttributeNodeIndex != -1)
            {
                var selectedNode = vizcore3d.Object3D.FromIndex(selectedAttributeNodeIndex);
                sheet1.BaseMemberName = (selectedNode != null && !string.IsNullOrEmpty(selectedNode.NodeName))
                    ? selectedNode.NodeName
                    : System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
            }
            else
            {
                sheet1.BaseMemberName = !string.IsNullOrEmpty(currentFilePath)
                    ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath)
                    : "전체";
            }
            sheet1.BaseMemberIndex = -1;
            foreach (var bom in bomList)
            {
                sheet1.MemberIndices.Add(bom.Index);
                sheet1.MemberNames.Add(bom.Name);
            }
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

            // T-025: 치수추출 직후 Sheet 1(전체) 기준 BOM 정보 자동 수집
            // lvDrawingSheet 선택 이벤트를 기다리지 않고 여기서 직접 채움 (visibility 등 부수효과 없이).
            if (drawingSheetList.Count > 0)
            {
                try
                {
                    CollectBOMInfo(false, drawingSheetList[0]);
                }
                catch (Exception ex)
                {
                    DiagLog($"GenerateDrawingSheets CollectBOMInfo FAIL {ex.Message}");
                }
            }

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

        /// <summary>
        /// 도면 생성 버튼 핸들러
        /// </summary>
        private void btnGenerateSheets_Click(object sender, EventArgs e)
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

            if (clashList.Count == 0)
            {
                MessageBox.Show("Clash 데이터가 없습니다. 먼저 Clash 검사를 수행해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GenerateDrawingSheets();
            MessageBox.Show($"도면 시트 {drawingSheetList.Count}개가 생성되었습니다.", "도면 생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 도면 시트 선택 시 X-Ray + 치수 표시
        /// </summary>
        private void LvDrawingSheet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                // [T-016 진단 로그] 빈 선택 (이벤트 두 번 발생 패턴)
                DiagLog($"LvDrawingSheet_SelectedIndexChanged SKIP (no selection)");
                return;
            }

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
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

            try
            {
                vizcore3d.BeginUpdate();

                // X-Ray 모드 비활성화 (관련 부재만 완전히 표시하기 위해)
                if (vizcore3d.View.XRay.Enable)
                {
                    vizcore3d.View.XRay.Clear();
                    vizcore3d.View.XRay.Enable = false;
                }

                // 모든 부재 숨기기
                List<int> allIndices = new List<int>();
                foreach (BOMData b in bomList)
                    allIndices.Add(b.Index);
                if (allIndices.Count > 0)
                    vizcore3d.Object3D.Show(allIndices, false);

                // 선택된 시트의 부재만 표시
                vizcore3d.Object3D.Show(sheet.MemberIndices, true);

                // 모서리(SilhouetteEdge) 표시
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 선택된 노드 인덱스 저장 (글로벌 뷰 버튼용)
                xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                // T-036 (2026-04-23): 가공도 시트는 ExecuteMfgDrawing이 자체 MoveCamera(X_PLUS/Y_PLUS/Z_PLUS)로
                // 카메라를 정면 뷰로 세팅하기 때문에, 여기서 FlyToObject3d를 먼저 호출하면 이전 ISO_PLUS 등의
                // 카메라 방향이 잔존한 상태로 화면 이동만 되어 "45도 대각 ISO 뷰 느낌"이 남음.
                // 가공도일 때 FlyToObject3d 스킵 → ExecuteMfgDrawing의 카메라/FitToView에 맡김.
                if (sheet.BaseMemberIndex != -3)
                {
                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
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

                // T-028: 시트 유형별 치수 분기
                //   가공도(-3): ExecuteMfgDrawing (기존 유지 — 단일 부재 가공도)
                //   설치도(-2): ExtractInstallationDimensions (BBox 기반, 부재 간 간격·전체 조립 치수)
                //   그 외(Sheet 1, Sheet 2+): ComputeViewDimensionsForMembers (Osnap 기반, 2D 출력과 동일 엔진)
                if (sheet.BaseMemberIndex == -3)
                {
                    ExecuteMfgDrawing(sheet.MemberIndices[0]);
                }
                else if (sheet.BaseMemberIndex == -2)
                {
                    // 설치도 BBox 분기 — 추후 옵션 A(완전 폐기)로 전환 가능
                    ExtractInstallationDimensions(sheet.MemberIndices);
                }
                else
                {
                    // 일반 시트 — 2D 출력과 동일한 Osnap 엔진 (3뷰 × 2축 = 6조합 + 중복 제거)
                    // E1 (2026-05-18): _lastCollectedNodeOsnapMap 전달 — 본체 fallback으로 안전 보장
                    chainDimensionList.Clear();
                    lvDimension.Items.Clear();
                    chainDimensionList.AddRange(
                        ComputeViewDimensionsForMembers(sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap));

                    int no = 1;
                    foreach (var dim in chainDimensionList)
                    {
                        dim.No = no;
                        ListViewItem lvi = new ListViewItem(no.ToString());
                        lvi.SubItems.Add(dim.Axis);
                        lvi.SubItems.Add(dim.ViewName);
                        lvi.SubItems.Add(((int)Math.Round(dim.Distance)).ToString());
                        lvi.SubItems.Add(dim.StartPointStr);
                        lvi.SubItems.Add(dim.EndPointStr);
                        lvi.Tag = dim;
                        lvDimension.Items.Add(lvi);
                        no++;
                    }

                    // T-030: 시트 선택 시 3D 뷰 치수 렌더링 제거 (T-029 정책 확장)
                    // chainDimensionList·lvDimension은 채우지만 ShowAllDimensions()는 호출하지 않음.
                    // 사용자가 글로벌 X/Y/Z 뷰 버튼을 눌러야 해당 뷰 치수가 3D 뷰에 등장.
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();

                    DiagLog($"T-030 시트 선택 자동 치수: sheet#={sheet.SheetNumber} members={sheet.MemberIndices.Count} chain={chainDimensionList.Count} (3D 미렌더)");
                }
            }
            catch (Exception ex)
            {
                // [T-016 진단 로그] silent catch 강화 (stack trace 포함)
                DiagLog($"LvDrawingSheet_SelectedIndexChanged FAIL " +
                    $"{ex.Message}\n{ex.StackTrace}");
            }

            // [T-016 진단 로그] 종료 (BOM 재수집 전 상태)
            DiagLog($"LvDrawingSheet_SelectedIndexChanged EXIT " +
                $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0}");

            // 선택된 시트 기준으로 BOM정보 자동 수집 (알람 없이)
            // D1 (2026-05-18): sheet 명시 전달 — lvDrawingSheet.SelectedItems[0] 묵시 의존 제거
            CollectBOMInfo(false, sheet);

            // T-036 (2026-04-23 3차 → 2026-04-24 4차 3·4단계 → 5단계): 가공도 시트 카메라 복원.
            //   진화 경로:
            //     - 3차: SetCameraData(snapshot)로 카메라 복원
            //     - 4차 3단계: SetCameraData + Rotate 재적용 (ScreenAxisRotation은 CameraData에 미포함)
            //     - 4차 4단계: BeginUpdate/EndUpdate로 감쌌으나 사용자 "카메라 이동 후 회전" 2단계 시각 잔존 보고
            //     - **4차 5단계 (현재)**: SetCameraData가 ScreenAxisRotation을 리셋하면서 paint를 동기 트리거 →
            //       BeginUpdate가 막지 못함. ExecuteMfgDrawing 이후 외부 FitToView가 모두 제거됐으므로
            //       카메라 위치는 변하지 않음 → **SetCameraData 호출 불필요**. ScreenAxisRotation만 재적용.
            //       (만약 회전이 ExecuteMfgDrawing 직후 그대로 유지된다면 이 블록 자체도 no-op)
            if (sheet.BaseMemberIndex == -3 && (_mfgDrawingZ90Applied || _mfgDrawingR180Applied))
            {
                try
                {
                    // BeginUpdate로 감싸 회전 적용 시점을 1회 paint로 통합
                    vizcore3d.BeginUpdate();

                    if (_mfgDrawingZ90Applied)
                    {
                        vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 90);
                    }
                    if (_mfgDrawingR180Applied)
                    {
                        vizcore3d.View.ScreenAxisRotation.LockZAxis = false;
                        vizcore3d.View.RotateCameraByScreenAxis(0, 0, 180);
                    }

                    vizcore3d.EndUpdate();

                    DiagLog($"T-036 카메라 회전 재적용: sheet#={sheet.SheetNumber} " +
                        $"Z90={_mfgDrawingZ90Applied} R180={_mfgDrawingR180Applied}");
                }
                catch (Exception ex)
                {
                    DiagLog($"T-036 카메라 회전 재적용 FAIL {ex.Message}");
                    try { vizcore3d.EndUpdate(); } catch { }
                }
            }
        }

        /// <summary>
        /// BOM 정보 테이블(lvDrawingBOMInfo) 행 선택 시 해당 부재를 카메라 fit.
        /// 시트의 visibility는 건드리지 않고 카메라만 이동한다.
        /// </summary>
        private void LvDrawingBOMInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrawingBOMInfo.SelectedItems.Count == 0) return;
            ListViewItem row = lvDrawingBOMInfo.SelectedItems[0];

            // 요약행(Row 0)은 No. 컬럼이 공란 — 스킵
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
        /// 도면정보 탭 - 선택된 시트의 포함부재를 X-Ray 선택 + Osnap/치수 추출 + 방향 보기
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

            try
            {
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
                    xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);
                    vizcore3d.Clash.ClearResultSymbol();

                    vizcore3d.EndUpdate();

                    // 설치도 개념: 부재 바운딩박스 기반 설치 치수 추출
                    ExtractInstallationDimensions(sheet.MemberIndices);

                    // T-034 후속 (2026-04-23): BOM 테이블 행 선택 → 글로벌 ISO 버튼 경로에서
                    // 여기 분기 탐 → 실선으로 부재가 잘 보이도록 SMOOTH 모드로 교체
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.SMOOTH);
                    vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.ISO_PLUS);
                    // 선택된 부재에 맞춰 화면 조정 (반복 호출 시 줌 누적 방지)
                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.0f);

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
                    xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);

                    vizcore3d.EndUpdate();

                    vizcore3d.Review.Note.Clear();
                    vizcore3d.Review.Measure.Clear();
                    vizcore3d.ShapeDrawing.Clear();
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
                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.0f);
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
        /// "2D 출력" 버튼 클릭 — 선택된 도면시트의 3D 뷰 상태를 2D 도면으로 생성
        /// </summary>
        private void btnGenerateSheet2D_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lvDrawingSheet.SelectedItems.Count == 0)
            {
                MessageBox.Show("도면 시트를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
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

        /// <summary>
        /// ALL 버튼 — 도면시트목록을 순회하며 2D/가공도 생성 + PDF 자동 저장
        /// </summary>
        private void btnExportAllPDF_Click(object sender, EventArgs e)
        {
            // 옛 동작 그대로 — saveDir 하드코딩 c:\, 완료 메시지박스 표시, 가공도도 시트별 PDF (groupMfgSheets=false).
            // T-064 P2 본진은 ExportAllSheetsToPdfCore(struSubDir, showSummary:false, groupMfgSheets:true)로 직접 호출.
            ExportAllSheetsToPdfCore(@"c:\", showSummary: true, groupMfgSheets: false);
        }

        /// <summary>
        /// lvDrawingSheet 모든 시트를 PDF로 일괄 출력 (공용 코어).
        /// 사용자 평소 btnExportAllPDF 흐름 + T-064 P2 본진 STRU 일괄 처리에서 공용 사용.
        /// </summary>
        /// <param name="saveDir">PDF 저장 폴더</param>
        /// <param name="showSummary">true면 완료/오류 메시지박스 표시 (옛 btnExportAllPDF 동작). false면 DiagLog만 (P2 본진).</param>
        /// <param name="groupMfgSheets">
        /// true면 가공도 시트들을 *모아서* GenerateMfgDrawing2DAll(List)로 1번에 출력 (btnMfgDrawingSheet 패턴, 8×3 그리드 1 PDF).
        /// false면 가공도도 시트별로 분리 PDF (옛 btnExportAllPDF 동작 유지).
        /// 사용자 지적: "가공도는 한 번에 뽑는 코드 기존에 있는데?" → P2 본진은 true로 호출.
        /// </param>
        /// <returns>PDF 출력 성공 개수</returns>
        private int ExportAllSheetsToPdfCore(string saveDir, bool showSummary, bool groupMfgSheets = false)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                if (showSummary) MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            if (lvDrawingSheet.Items.Count == 0)
            {
                if (showSummary) MessageBox.Show("도면 시트가 없습니다. 먼저 '도면 생성'을 해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            int totalCount = lvDrawingSheet.Items.Count;
            int successCount = 0;

            try
            {
                for (int i = 0; i < lvDrawingSheet.Items.Count; i++)
                {
                    ListViewItem lvi = lvDrawingSheet.Items[i];
                    DrawingSheetData sheet = lvi.Tag as DrawingSheetData;
                    if (sheet == null || sheet.MemberIndices.Count == 0)
                        continue;

                    string sheetLabel = lvi.Text; // "Sheet 1" 또는 "가공도_1"

                    // ★ groupMfgSheets=true면 가공도 시트는 루프 끝난 후 한 번에 처리 — 여기선 건너뜀
                    if (groupMfgSheets && sheetLabel.StartsWith("가공도"))
                        continue;

                    // ListView에서 해당 항목 선택 (UI 동기화)
                    foreach (ListViewItem sel in lvDrawingSheet.SelectedItems)
                        sel.Selected = false;
                    lvi.Selected = true;
                    lvi.EnsureVisible();
                    Application.DoEvents();

                    string baseMemberName = sheet.BaseMemberName ?? "Unknown";
                    // 파일명: 기준부재_도면번호_시분초
                    string timeStamp = DateTime.Now.ToString("HHmmss");
                    string safeBaseName = SanitizeFileName(baseMemberName);
                    string safeSheetLabel = SanitizeFileName(sheetLabel);
                    string pdfFileName = $"{safeBaseName}_{safeSheetLabel}_{timeStamp}.pdf";
                    string pdfPath = System.IO.Path.Combine(saveDir, pdfFileName);

                    if (sheetLabel.StartsWith("가공도"))
                    {
                        // 가공도 출력 로직
                        var mfgSheets = new List<DrawingSheetData> { sheet };
                        GenerateMfgDrawing2DAll(mfgSheets);
                    }
                    else
                    {
                        // 2D 출력 로직
                        GenerateSheetDrawing2D(sheet);
                    }

                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);

                    // PDF 출력
                    try
                    {
                        vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                        vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                        successCount++;
                        DiagLog($"[ALL PDF] {i + 1}/{totalCount} 저장: {pdfPath}");
                    }
                    catch (Exception pdfEx)
                    {
                        DiagLog($"[ALL PDF] {i + 1}/{totalCount} 실패: {pdfEx.Message}");
                    }

                    // ── 메모리 정리 (매 시트 처리 후) ──
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView(); } catch { }
                    try { vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(); }
                    catch { }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }

                // ★ groupMfgSheets=true: 가공도 시트들 *한 번에* 처리 (btnMfgDrawingSheet 패턴, 8×3 그리드 1 PDF)
                // 사용자 지적: "가공도는 한 번에 뽑는 코드 기존에 있는데?"
                if (groupMfgSheets)
                {
                    var mfgSheets = new List<DrawingSheetData>();
                    foreach (ListViewItem lvi in lvDrawingSheet.Items)
                    {
                        if (lvi.Text.StartsWith("가공도"))
                        {
                            var s = lvi.Tag as DrawingSheetData;
                            if (s != null && s.MemberIndices.Count > 0)
                                mfgSheets.Add(s);
                        }
                    }

                    if (mfgSheets.Count > 0)
                    {
                        try
                        {
                            DiagLog($"[ALL PDF] 가공도 묶음 처리 시작 — 시트 {mfgSheets.Count}개");
                            GenerateMfgDrawing2DAll(mfgSheets);
                            Application.DoEvents();
                            System.Threading.Thread.Sleep(300);

                            string timeStamp = DateTime.Now.ToString("HHmmss");
                            string pdfFileName = $"가공도_All_{timeStamp}.pdf";
                            string pdfPath = System.IO.Path.Combine(saveDir, pdfFileName);

                            vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                            vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();
                            vizcore3d.Drawing2D.Object2D.Export2PDFBy2DView(pdfPath);
                            successCount++;
                            DiagLog($"[ALL PDF] 가공도 묶음 PDF 저장: {pdfPath}");

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
                        catch (Exception mfgEx)
                        {
                            DiagLog($"[ALL PDF] 가공도 묶음 처리 ERROR: {mfgEx.Message}");
                        }
                    }
                    else
                    {
                        DiagLog($"[ALL PDF] groupMfgSheets=true이지만 가공도 시트 0건");
                    }
                }

                if (showSummary)
                    MessageBox.Show($"PDF 일괄 출력 완료!\n\n총 {totalCount}개 중 {successCount}개 저장됨\n저장 경로: {saveDir}", "ALL PDF 출력 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"ExportAllSheetsToPdfCore ERROR: {ex.Message}\n{ex.StackTrace}");
                if (showSummary)
                    MessageBox.Show($"ALL PDF 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return successCount;
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

        /// <summary>
        /// 선택된 시트 부재만 대상으로 2D 도면 생성
        /// (ISO 풍선번호 + X/Y/Z 치수선 + BOM 테이블 + 도면정보)
        /// </summary>
        private void GenerateSheetDrawing2D(DrawingSheetData sheet)
        {
            // 사전 조건: 히든라인 모델 투영용 엣지 데이터 갱신 (ISO 방향 튀어나온 모서리 누락 방지)
            // 자동(ProcessSingleStruFull)·수동(btnGenerateSheet2D_Click) 모두 이 함수 통과 → 단일 지점에서 보장
            vizcore3d.Object3D.GenerateEdgeData();

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

                // 2026-05-11: 작업데이터 탭 체인치수 = 도면 표시 치수 통일 (사용자 요청)
                //   이전: ExtractInstallationDimensions (BBox 기반) → 작업데이터 탭과 도면 측 데이터 불일치
                //   현재: ComputeViewDimensionsForMembers (Osnap 기반, 도면 측과 동일 엔진 — 3뷰×2축 6조합 합집합)
                {
                    // E1 (2026-05-18): _lastCollectedNodeOsnapMap 전달 — 본체 fallback으로 안전 보장
                    chainDimensionList.Clear();
                    lvDimension.Items.Clear();
                    chainDimensionList.AddRange(
                        ComputeViewDimensionsForMembers(sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap));
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
                tableInfo.SetText(0, 0, string.Format("{0}\\Logo.png", GetSolutionPath()));
                tableInfo.SetText(0, 1, "Project Name:\nProject No:");
                tableInfo.SetText(1, 0, string.Format("{0}\\Logo.png", GetSolutionPath()));
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
                // P2 자동 흐름은 ExportAllSheetsToPdfCore → 시트 선택 → 이 메서드 호출인데,
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

                // ── 1.6. 치수 데이터 계산 (옛 본문 1257~1276 동일 — Osnap 기반 6조합 합집합) ──
                // E1 (2026-05-18): _lastCollectedNodeOsnapMap 전달 — 본체 fallback으로 안전 보장
                chainDimensionList.Clear();
                lvDimension.Items.Clear();
                chainDimensionList.AddRange(
                    ComputeViewDimensionsForMembers(sheet.MemberIndices, null, 0.5f, _lastCollectedNodeOsnapMap));
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

                // ── 2. 엑셀 파일 경로 ──
                string solutionPath = GetSolutionPath();
                string xlsxPath = System.IO.Path.Combine(solutionPath, "사용자템플릿_엑셀_제작도.xlsx");
                if (!System.IO.File.Exists(xlsxPath))
                {
                    DiagLog($"P2 엑셀 파일 없음: {xlsxPath}");
                    throw new Exception($"엑셀 파일 없음: {xlsxPath}");
                }

                // ── 3. data Dictionary 구성 ({Input_N} 슬롯 치환) ──
                // 슬롯 컨벤션 (PoC와 동일):
                //   1 = 프로젝트명, 2 = 선박번호, 3 = 도면종류
                //   4..18 = BOM No (15행), 19..33 = ITEM, 34..48 = MATERIAL, 49..63 = SIZE,
                //   64..78 = Q'TY, 79..93 = T/W, 94..108 = MA, 109..123 = FA
                Dictionary<int, string> data = new Dictionary<int, string>();
                // 도면정보 — TODO: tableInfo 또는 sheet 메타에서. 지금은 PoC 하드코딩 유지.
                data[1] = "CEDAR FLNG";
                data[2] = "SN2688";
                // 시트 종류 라벨 — 제작도/조립도/설치도/가공도 (GetSheetKindLabel: Form1.Stru.cs)
                data[3] = GetSheetKindLabel(sheet);

                // BOM 8컬럼 × 15행 — lvDrawingBOMInfo Row 0(요약행) 제외
                int bomMapped = 0;
                if (lvDrawingBOMInfo.Items.Count > 1)
                {
                    int n = Math.Min(lvDrawingBOMInfo.Items.Count - 1, 15);
                    for (int i = 0; i < n; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i + 1];
                        data[4 + i]   = item.Text;                              // NO
                        data[19 + i]  = SafeSubItem(item, 1);                   // ITEM
                        data[34 + i]  = SafeSubItem(item, 2);                   // MATERIAL
                        data[49 + i]  = SafeSubItem(item, 3);                   // SIZE
                        data[64 + i]  = SafeSubItem(item, 4);                   // Q'TY
                        data[79 + i]  = SafeSubItem(item, 5);                   // T/W
                        data[94 + i]  = SafeSubItem(item, 6);                   // MA
                        data[109 + i] = SafeSubItem(item, 7);                   // FA
                    }
                    bomMapped = n;
                }
                DiagLog($"P2 data 구성: kind='{data[3]}' BOM {bomMapped}행 (Input 총 {data.Count}개)");

                // ── 4. ImportExcelWithData — 엑셀 자동 그리기 + 데이터 치환 ──
                vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data);
                vizcore3d.Drawing2D.View.SetSelectCanvas(1);
                DiagLog($"P2 ImportExcelWithData OK — {Path.GetFileName(xlsxPath)}");

                // ── 5. GetViewAreasFromExcel — {View_n} 영역 좌표 파싱 ──
                var viewAreas = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
                if (viewAreas == null || viewAreas.Count == 0)
                {
                    DiagLog("P2 GetViewAreasFromExcel 비어있음 — 엑셀에 {View_N} 태그 없음");
                    return;
                }
                DiagLog($"P2 GetViewAreasFromExcel: {viewAreas.Count}개 영역");

                // ── 6. View 인덱스 ↔ 카메라 매핑 (4면도 규약 — PoC와 동일) ──
                Dictionary<int, VIZCore3D.NET.Data.CameraDirection> cameraMap = new Dictionary<int, VIZCore3D.NET.Data.CameraDirection>
                {
                    { 1, VIZCore3D.NET.Data.CameraDirection.ISO_PLUS },   // ISO
                    { 2, VIZCore3D.NET.Data.CameraDirection.Z_MINUS  },   // LOOKING "Z"
                    { 3, VIZCore3D.NET.Data.CameraDirection.X_MINUS  },   // LOOKING "X"
                    { 4, VIZCore3D.NET.Data.CameraDirection.Y_MINUS  },   // LOOKING "Y"
                };

                const float margin = 5f;
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

                    // 매 뷰마다 3D 어노테이션 초기화 (옛 RenderSheetViewForDrawing L1903~1905)
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
                    xraySelectedNodeIndices = new List<int>(sheet.MemberIndices);
                    vizcore3d.EndUpdate();

                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
                    vizcore3d.View.MoveCamera(camDir);

                    if (viewDir != "ISO" && sheet.MemberIndices != null && sheet.MemberIndices.Count > 0)
                        ApplyOrientationRotation(sheet.MemberIndices[0], viewDir);

                    vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.25f);

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

                        // 풍선 생성 후 시트 부재만 보이기 (2D 캡처 준비)
                        vizcore3d.BeginUpdate();
                        vizcore3d.View.XRay.Enable = false;
                        vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                        vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                        vizcore3d.EndUpdate();
                    }
                    else
                    {
                        // X/Y/Z 치수 — 옛 RenderSheetViewForDrawing L1995~2002
                        float availW = p.Width - 2f * margin;
                        float availH = p.Height - 2f * margin;
                        float estScale = EstimateFitScaleForViewArea(availW, availH, viewDir, sheet.MemberIndices);
                        shapeDrawingIds = ShowAllDimensions(viewDir, true, estScale);
                    }

                    // ── 모델 4면도 캡처 ──
                    int objId = vizcore3d.Drawing2D.Object2D
                        .Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                            VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                    if (objId < 0)
                    {
                        DiagLog($"P2 View_{p.Index} Object2D 생성 실패 objId={objId}");
                        continue;
                    }

                    // ── 영역 fit + 추가 shrink (사용자 사양 2026-05-14: Z=0.65 / X·Y·ISO=0.70) ──
                    float fitW = p.Width - 2f * margin;
                    float fitH = p.Height - 2f * margin;
                    float objW = 0f, objH = 0f;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);
                    float objScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                    if (objW > 0f && objH > 0f && fitW > 0f && fitH > 0f)
                    {
                        float fitScale = Math.Min(fitW / objW, fitH / objH);
                        float shrinkFactor = (viewDir == "Z") ? 0.65f : 0.70f;
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, objScale * fitScale * shrinkFactor);
                    }

                    // ── 영역 중심으로 이동 ──
                    // ISO(View_1) X +10mm / Looking Y(View_4) X +20mm (사용자 사양 2026-05-14: Y뷰 추가 오른쪽)
                    // Z(View_2), X(View_3)는 그대로. 모두 Y +15mm 공통.
                    float xOffset = (p.Index == 1) ? 10f
                                  : (p.Index == 4) ? 20f
                                  : 0f;
                    float cx = p.X + p.Width / 2f;
                    float cy = p.Y + p.Height / 2f;
                    vizcore3d.Drawing2D.Object2D.MoveObjectTo(objId, cx + xOffset, cy + 15f);

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
                        vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
                        convertedNoteIndices.AddRange(visibleNoteIds);
                    }
                    foreach (int nIdx in convertedNoteIndices)
                    {
                        try { vizcore3d.Drawing2D.View.Set2DNoteLabelSnapBoxType(nIdx, VIZCore3D.NET.Data.SnapBoxType.CIRCLE); }
                        catch { }
                    }
                    vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(7f);

                    // ── 치수(Measure) → 2D (X/Y/Z만, ApplyParallelTextShift로 텍스트 시프트 후) ──
                    if (viewDir != "ISO")
                    {
                        var measureItems = vizcore3d.Review.Measure.Items;
                        var measureIds = new List<int>();
                        foreach (var m in measureItems)
                            if (m.Visible) measureIds.Add(m.ID);

                        if (measureIds.Count > 0)
                        {
                            ApplyParallelTextShift(viewDir,
                                vizcore3d.Drawing2D.Object2D.GetObjectScale(objId),
                                measureItems);
                            vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(measureIds.ToArray());
                        }
                    }

                    viewsRendered++;
                }

                DiagLog($"P2 GenerateSheetDrawing2D_WithExcelTemplate 완료 — sheet#={sheet.SheetNumber} views={viewsRendered}/{viewAreas.Count}");
            }
            catch (Exception ex)
            {
                DiagLog($"P2 GenerateSheetDrawing2D_WithExcelTemplate ERROR: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
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
        /// T-064 P2 본진 (2026-05-14): 엑셀 분기용 viewArea 기반 fit scale 추정.
        /// EstimateFitScaleForCell(GridStructure 셀 기반)과 동일 알고리즘이지만 입력을 viewArea 영역으로.
        /// 사용자 사양: Z=0.65 / X·Y=0.70 (모델 차지 비율). ShowAllDimensions 보조선 위치 계산 기준.
        /// 모델 RescaleObject 시점의 shrinkFactor와 동일 값을 유지해야 보조선이 모델 fit 결과와 일치.
        /// </summary>
        private float EstimateFitScaleForViewArea(float availW, float availH, string viewDirection, List<int> memberIndices)
        {
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
            float fitFactor = (viewDirection == "Z") ? 0.65f : 0.70f;
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
            else
            {
                // X/Y/Z: ShowAllDimensions (forDrawing2D=true → 보조선 ShapeDrawing ID 수집)
                // T-038+039 v2: 치수 max 기반 동적 분기 — ShowAllDimensions가 내부에서 결정.
                //   max > 1000mm: 보조선 10/20mm / max ≤ 1000mm: 20/40mm (캔버스 절대 mm).
                // 호출자는 scale만 추정해 전달. ShowAllDimensions가 RescaleObject 전이라 사전 추정 필요.
                float estScale = EstimateFitScaleForCell(row, col, viewDirection,
                    isIsoFullView ? allBomIndices : sheet.MemberIndices);
                shapeDrawingIds = ShowAllDimensions(viewDirection, true, estScale);
            }

            // ── 5. 2패스 2D 투영 (Sheet 2+ ISO: 나머지 부재 가는점선 + 시트 부재 굵은실선) ──
            int objId;

            if (isIsoFullView)
            {
                // ── Pass 1: 전체 BOM 부재 → 배경 (생성 후 점선으로 변경) ──
                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(allBomIndices, true);
                vizcore3d.EndUpdate();

                bgObjId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);
                // 생성 후 점선 + 가는 선으로 변경
                vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineType(bgObjId,
                    VIZCore3D.NET.Data.Object2D_LineTypes.DASHED_DOUBLEDOTTED);
                vizcore3d.Drawing2D.Object2D.Set2DViewObjectItemLineThickness(bgObjId, 0.15f);

                // ── Pass 2: 시트 부재만 → 전경 (실선) ──
                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, false);
                vizcore3d.Object3D.Show(sheet.MemberIndices, true);
                vizcore3d.EndUpdate();

                objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                    VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

                // 오리진에서 두 객체의 중심 차이 기록 (피팅 전)
                float bgCX0 = 0f, bgCY0 = 0f, objCX0 = 0f, objCY0 = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(bgObjId, ref bgCX0, ref bgCY0);
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(objId, ref objCX0, ref objCY0);

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

                // ── T-013 옵션 B — WorldToScreen 기반 3D→캔버스 좌표 변환 (2026-04-21) ──
                // 옵션 A 실패 확인: objId가 원점(0,0)에 objScale=0.005로 남아 거의 안 보임.
                // SDK가 두 2D 객체를 자동 매핑하지 않으므로, 3D BBox 중심을 WorldToScreen으로
                // 캔버스 좌표로 변환해 obj를 bg 내부 원래 위치에 배치한다.
                //
                // 알고리즘:
                //   1. 전체 BOM의 3D BBox 중심 + 시트 부재의 3D BBox 중심 계산 (bomList 사용)
                //   2. WorldToScreen(center3D, true)로 각각 캔버스 좌표 추출
                //   3. objId를 bgFinalScale과 동일 스케일로 축소
                //   4. objId 중심을 (bgCanvasCenter + 화면 좌표 차이)로 이동

                // 1. 전체 BOM의 3D BBox 중심
                float bgMinX3D = float.MaxValue, bgMinY3D = float.MaxValue, bgMinZ3D = float.MaxValue;
                float bgMaxX3D = float.MinValue, bgMaxY3D = float.MinValue, bgMaxZ3D = float.MinValue;
                foreach (int idx in allBomIndices)
                {
                    BOMData b = bomList.FirstOrDefault(x => x.Index == idx);
                    if (b == null) continue;
                    bgMinX3D = System.Math.Min(bgMinX3D, b.MinX); bgMaxX3D = System.Math.Max(bgMaxX3D, b.MaxX);
                    bgMinY3D = System.Math.Min(bgMinY3D, b.MinY); bgMaxY3D = System.Math.Max(bgMaxY3D, b.MaxY);
                    bgMinZ3D = System.Math.Min(bgMinZ3D, b.MinZ); bgMaxZ3D = System.Math.Max(bgMaxZ3D, b.MaxZ);
                }
                var bgCenter3D = new VIZCore3D.NET.Data.Vertex3D(
                    (bgMinX3D + bgMaxX3D) / 2f,
                    (bgMinY3D + bgMaxY3D) / 2f,
                    (bgMinZ3D + bgMaxZ3D) / 2f);

                // 2. 시트 부재의 3D BBox 중심
                float objMinX3D = float.MaxValue, objMinY3D = float.MaxValue, objMinZ3D = float.MaxValue;
                float objMaxX3D = float.MinValue, objMaxY3D = float.MinValue, objMaxZ3D = float.MinValue;
                foreach (int idx in sheet.MemberIndices)
                {
                    BOMData b = bomList.FirstOrDefault(x => x.Index == idx);
                    if (b == null) continue;
                    objMinX3D = System.Math.Min(objMinX3D, b.MinX); objMaxX3D = System.Math.Max(objMaxX3D, b.MaxX);
                    objMinY3D = System.Math.Min(objMinY3D, b.MinY); objMaxY3D = System.Math.Max(objMaxY3D, b.MaxY);
                    objMinZ3D = System.Math.Min(objMinZ3D, b.MinZ); objMaxZ3D = System.Math.Max(objMaxZ3D, b.MaxZ);
                }
                var objCenter3D = new VIZCore3D.NET.Data.Vertex3D(
                    (objMinX3D + objMaxX3D) / 2f,
                    (objMinY3D + objMaxY3D) / 2f,
                    (objMinZ3D + objMaxZ3D) / 2f);

                // 3. WorldToScreen으로 캔버스 좌표 추출 (VIZCore3D.NET.xml:63853)
                var bgScreenC = vizcore3d.View.WorldToScreen(bgCenter3D, true);
                var objScreenC = vizcore3d.View.WorldToScreen(objCenter3D, true);

                // 3-1. bg의 3D BBox 8개 꼭지점 → WorldToScreen → 원본 캔버스 BBox 계산
                //      (bgFinalScale 단독으론 스케일 체인 불일치 → bgCanvasSize / bgScreenBBox 비율 필요)
                var bgCornersScreen = new VIZCore3D.NET.Data.Vertex3D[8];
                bgCornersScreen[0] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMinX3D, bgMinY3D, bgMinZ3D), true);
                bgCornersScreen[1] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMaxX3D, bgMinY3D, bgMinZ3D), true);
                bgCornersScreen[2] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMinX3D, bgMaxY3D, bgMinZ3D), true);
                bgCornersScreen[3] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMinX3D, bgMinY3D, bgMaxZ3D), true);
                bgCornersScreen[4] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMaxX3D, bgMaxY3D, bgMinZ3D), true);
                bgCornersScreen[5] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMaxX3D, bgMinY3D, bgMaxZ3D), true);
                bgCornersScreen[6] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMinX3D, bgMaxY3D, bgMaxZ3D), true);
                bgCornersScreen[7] = vizcore3d.View.WorldToScreen(new VIZCore3D.NET.Data.Vertex3D(bgMaxX3D, bgMaxY3D, bgMaxZ3D), true);

                float bgScreenMinX = float.MaxValue, bgScreenMinY = float.MaxValue;
                float bgScreenMaxX = float.MinValue, bgScreenMaxY = float.MinValue;
                for (int k = 0; k < 8; k++)
                {
                    bgScreenMinX = System.Math.Min(bgScreenMinX, bgCornersScreen[k].X);
                    bgScreenMinY = System.Math.Min(bgScreenMinY, bgCornersScreen[k].Y);
                    bgScreenMaxX = System.Math.Max(bgScreenMaxX, bgCornersScreen[k].X);
                    bgScreenMaxY = System.Math.Max(bgScreenMaxY, bgCornersScreen[k].Y);
                }
                float bgScreenW = bgScreenMaxX - bgScreenMinX;
                float bgScreenH = bgScreenMaxY - bgScreenMinY;

                // 4. objId를 bgFinalScale로 맞추고 위치 이동
                float bgFinalScaleB = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                vizcore3d.Drawing2D.Object2D.RescaleObject(objId, bgFinalScaleB);

                float bgCX1B = 0f, bgCY1B = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(bgObjId, ref bgCX1B, ref bgCY1B);
                float objCXB = 0f, objCYB = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(objId, ref objCXB, ref objCYB);

                float dScreenX = objScreenC.X - bgScreenC.X;
                float dScreenY = objScreenC.Y - bgScreenC.Y;

                // T-013 옵션 B 재수정 (2차): bgFinalScale 단독 곱으론 부족.
                // 정확한 변환: dScreen(원본 캔버스 단위) → (bgCanvasSize / bgScreenBBox) 비율 → 현재 렌더 단위
                // 실측 검증: dScreen.Y=195.97 × (30.0/bgScreenH) ≈ 7.3mm (offsetRatio.Z=0.244 × 30=7.32mm와 일치)
                float bgCanvasWRef = 0f, bgCanvasHRef = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(bgObjId, ref bgCanvasWRef, ref bgCanvasHRef);
                float ratioX = bgScreenW > 0 ? bgCanvasWRef / bgScreenW : 1f;
                float ratioY = bgScreenH > 0 ? bgCanvasHRef / bgScreenH : 1f;
                float targetX = bgCX1B + dScreenX * ratioX;
                float targetY = bgCY1B + dScreenY * ratioY;
                float moveX = targetX - objCXB;
                float moveY = targetY - objCYB;

                vizcore3d.Drawing2D.Object2D.MoveObject(objId, moveX, moveY);

                // 이동 후 objId의 실제 최종 중심·크기 — MoveObject가 실제로 적용되었는지 검증용
                float objFinalCX = 0f, objFinalCY = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(objId, ref objFinalCX, ref objFinalCY);
                float objFinalW = 0f, objFinalH = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objFinalW, ref objFinalH);

                // BBox 크기와 상대 오프셋 비율 추가 — obj가 bg 내 어디에 있는지 판정용
                float bgW3D = bgMaxX3D - bgMinX3D;
                float bgH3D = bgMaxY3D - bgMinY3D;
                float bgD3D = bgMaxZ3D - bgMinZ3D;
                float objW3D = objMaxX3D - objMinX3D;
                float objH3D = objMaxY3D - objMinY3D;
                float objD3D = objMaxZ3D - objMinZ3D;
                float offsetRatioX = bgW3D > 0 ? (objCenter3D.X - bgCenter3D.X) / bgW3D : 0;
                float offsetRatioY = bgH3D > 0 ? (objCenter3D.Y - bgCenter3D.Y) / bgH3D : 0;
                float offsetRatioZ = bgD3D > 0 ? (objCenter3D.Z - bgCenter3D.Z) / bgD3D : 0;

                // bgObjId의 현재 캔버스 크기 (bgFinalScale 반영 후)
                float bgCanvasW = 0f, bgCanvasH = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(bgObjId, ref bgCanvasW, ref bgCanvasH);

                DiagLog($"RenderSheet ISO OPT-B2 bgObjId={bgObjId} objId={objId} " +
                    $"bg3D=({bgCenter3D.X:F1},{bgCenter3D.Y:F1},{bgCenter3D.Z:F1}) " +
                    $"obj3D=({objCenter3D.X:F1},{objCenter3D.Y:F1},{objCenter3D.Z:F1}) " +
                    $"bgSize3D=({bgW3D:F0}x{bgH3D:F0}x{bgD3D:F0}) " +
                    $"objSize3D=({objW3D:F0}x{objH3D:F0}x{objD3D:F0}) " +
                    $"offsetRatio=({offsetRatioX:F3},{offsetRatioY:F3},{offsetRatioZ:F3}) " +
                    $"bgScreen=({bgScreenC.X:F2},{bgScreenC.Y:F2}) " +
                    $"objScreen=({objScreenC.X:F2},{objScreenC.Y:F2}) " +
                    $"dScreen=({dScreenX:F2},{dScreenY:F2}) " +
                    $"bgScreenBBox=({bgScreenW:F2}x{bgScreenH:F2}) " +
                    $"bgCanvas=({bgCX1B:F2},{bgCY1B:F2}) bgCanvasSize=({bgCanvasW:F1}x{bgCanvasH:F1}) " +
                    $"ratio=({ratioX:F4},{ratioY:F4}) " +
                    $"target=({targetX:F2},{targetY:F2}) " +
                    $"move=({moveX:F2},{moveY:F2}) " +
                    $"objFinal=({objFinalCX:F2},{objFinalCY:F2}) objFinalSize=({objFinalW:F2}x{objFinalH:F2}) " +
                    $"scale={bgFinalScaleB:F4}");
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
                // T-040 v7 (2026-05-13): 평행 시프트 — 직각 시프트 폐기
                // 임계 maxEstDist/26 이하 치수를 인접 큰 dim 쪽 측정축 방향으로 슬라이드
                ApplyParallelTextShift(viewDirection,
                    vizcore3d.Drawing2D.Object2D.GetObjectScale(objId),
                    measures);

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
