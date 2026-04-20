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

            // Sheet 2~: BOM 순서대로 순회
            // appearedAsIncluded: 다른 시트의 포함부재에 나온 인덱스 (기준부재 스킵용)
            HashSet<int> appearedAsIncluded = new HashSet<int>();
            int sheetNumber = 2;

            foreach (var bom in bomList)
            {
                // 이미 다른 시트의 포함부재에 나온 부재면 기준부재로 스킵
                if (appearedAsIncluded.Contains(bom.Index))
                    continue;

                DrawingSheetData sheet = new DrawingSheetData();
                sheet.SheetNumber = sheetNumber;
                sheet.BaseMemberIndex = bom.Index;
                sheet.BaseMemberName = bom.Name;

                // 포함부재: 기준부재 자신
                sheet.MemberIndices.Add(bom.Index);
                sheet.MemberNames.Add(bom.Name);

                // 포함부재: Clash에서 기준부재와 연결된 모든 부재 (Index 기반)
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
                        // 포함부재로 등록 → 이후 기준부재로 선정되지 않음
                        appearedAsIncluded.Add(neighborIndex);
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

            // Sheet 1과 부재 구성이 동일한 상세도 시트 자동 제거
            if (drawingSheetList.Count > 1)
            {
                HashSet<int> sheet1Members = new HashSet<int>(drawingSheetList[0].MemberIndices);
                drawingSheetList.RemoveAll(s =>
                    s.BaseMemberIndex >= 0 &&
                    s.MemberIndices.Count == sheet1Members.Count &&
                    new HashSet<int>(s.MemberIndices).SetEquals(sheet1Members));
            }

            // ListView 갱신
            foreach (var sheet in drawingSheetList)
            {
                string sheetLabel;
                if (sheet.BaseMemberIndex == -3) // 가공도
                    sheetLabel = $"가공도_{sheet.MfgDrawingNo}";
                else
                    sheetLabel = $"Sheet {sheet.SheetNumber}";

                ListViewItem lvi = new ListViewItem(sheetLabel);
                lvi.SubItems.Add(sheet.BaseMemberName);
                lvi.SubItems.Add(sheet.BaseMemberIndex == -3 ? "" : string.Join(", ", sheet.MemberNames));

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
                return;

            DrawingSheetData sheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
            if (sheet == null || sheet.MemberIndices.Count == 0)
                return;

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

                // 선택된 노드로 화면 이동
                vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.2f);

                // 이전 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // 기존 풍선(Note) 제거
                vizcore3d.Review.Note.Clear();

                vizcore3d.EndUpdate();

                // 설치도 시트: 부재 바운딩박스 경계 기반 체인치수
                // 가공도 시트: 단일 부재 가공도 출력
                // 일반 시트: Osnap 기반 체인치수
                if (sheet.BaseMemberIndex == -3) // 가공도
                {
                    ExecuteMfgDrawing(sheet.MemberIndices[0]);
                }
                else
                {
                    // 설치도 개념: 부재 바운딩박스 기반 설치 치수 추출
                    // (부재 전체 길이 + 부재간 설치 위치 정보)
                    ExtractInstallationDimensions(sheet.MemberIndices);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"도면 시트 표시 중 오류: {ex.Message}");
            }

            // 선택된 시트 기준으로 BOM정보 자동 수집 (알람 없이)
            CollectBOMInfo(false);
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

                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);
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
                    vizcore3d.View.SetRenderMode(VIZCore3D.NET.Data.RenderModes.DASH_LINE);

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
            float baseOffsetDist = Math.Max(200f, isoDiag * 0.35f);

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

                // 3D 방향: 모델 중심 → 부재 중심 (XY 평면)
                float initDirX = bom.CenterX - mCenterX;
                float initDirY = bom.CenterY - mCenterY;
                float initDirLen = (float)Math.Sqrt(initDirX * initDirX + initDirY * initDirY);
                if (initDirLen < 0.001f) { initDirX = 1f; initDirY = 0f; initDirLen = 1f; }
                initDirX /= initDirLen;
                initDirY /= initDirLen;

                // 3D 위치 후보 → 2D 투영 → AABB 검사 → 충돌 시 3D 회전
                float noteX = bom.CenterX + initDirX * baseOffsetDist;
                float noteY = bom.CenterY + initDirY * baseOffsetDist;
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
                        // 3D XY 평면에서 회전 + 거리 증가
                        float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                        if (attempt % 2 == 1) rotAngle = -rotAngle;
                        float cosA = (float)Math.Cos(rotAngle);
                        float sinA = (float)Math.Sin(rotAngle);
                        float newOffset = baseOffsetDist * (1f + (attempt / 4) * 0.15f);
                        noteX = bom.CenterX + (cosA * initDirX - sinA * initDirY) * newOffset;
                        noteY = bom.CenterY + (sinA * initDirX + cosA * initDirY) * newOffset;
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
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lvDrawingSheet.Items.Count == 0)
            {
                MessageBox.Show("도면 시트가 없습니다. 먼저 '도면 생성'을 해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string saveDir = @"c:\";
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

                    // ListView에서 해당 항목 선택 (UI 동기화)
                    foreach (ListViewItem sel in lvDrawingSheet.SelectedItems)
                        sel.Selected = false;
                    lvi.Selected = true;
                    lvi.EnsureVisible();
                    Application.DoEvents();

                    string sheetLabel = lvi.Text; // "Sheet 1" 또는 "가공도_1"
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
                        System.Diagnostics.Debug.WriteLine($"[ALL PDF] {i + 1}/{totalCount} 저장: {pdfPath}");
                    }
                    catch (Exception pdfEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ALL PDF] {i + 1}/{totalCount} 실패: {pdfEx.Message}");
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

                MessageBox.Show($"PDF 일괄 출력 완료!\n\n총 {totalCount}개 중 {successCount}개 저장됨\n저장 경로: {saveDir}", "ALL PDF 출력 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ALL PDF 출력 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        /// <summary>
        /// 선택된 시트 부재만 대상으로 2D 도면 생성
        /// (ISO 풍선번호 + X/Y/Z 치수선 + BOM 테이블 + 도면정보)
        /// </summary>
        private void GenerateSheetDrawing2D(DrawingSheetData sheet)
        {
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

                // 설치도 치수 데이터 추출 (ApplyDrawingSheetView("ISO") 동일)
                ExtractInstallationDimensions(sheet.MemberIndices);

                // BOM 자동 수집
                CollectBOMInfo(false);

                // ── 3. 그리드 구조 먼저 생성 (CrateTemplateBorder가 그리드 필요) ──
                {
                    int selCanvas = 1;
                    vizcore3d.Drawing2D.View.SetSelectCanvas(selCanvas);
                    float tmpW = 0f, tmpH = 0f;
                    vizcore3d.Drawing2D.View.GetCanvasSize(ref tmpW, ref tmpH);
                    vizcore3d.Drawing2D.GridStructure.AddGridStructure(2, 3, tmpW, tmpH);
                    vizcore3d.Drawing2D.GridStructure.SetMargins(10, 10, 10, 10);
                }

                // ── 4. 템플릿 생성 (외곽 테두리) ──
                // BOM/tableInfo는 셀 기반(RenderTemplateOnGridStructure)으로 이관되어
                // 이전에 절대좌표 앵커로 쓰던 bInfo는 더 이상 필요 없음.
                vizcore3d.Drawing2D.Template.CrateTemplateBorder();

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
                    // 열 너비 합 77mm — 흰선 내부 폭 추가 축소 (RenderTemplateOnGridStructure가 셀 92.3mm 내부에 추가 패딩 둠)
                    table1.ColumnWidths = new Dictionary<int, int>()
                    {
                        { 0, 6 },   // No
                        { 1, 17 },  // ITEM
                        { 2, 11 },  // MATERIAL
                        { 3, 11 },  // SIZE
                        { 4, 8 },   // Q'TY
                        { 5, 12 },  // T/W
                        { 6, 6 },   // MA
                        { 7, 6 }    // FA
                    };

                    vizcore3d.Drawing2D.GridStructure.SetGridCellVerticalAlignment(1, 3,
                        VIZCore3D.NET.Data.GridVerticalAlignment.Top);
                    vizcore3d.Drawing2D.GridStructure.SetGridCellHorizontalAlignment(1, 3,
                        VIZCore3D.NET.Data.GridHorizontalAlignment.Center);
                    vizcore3d.Drawing2D.Template.RenderTemplateOnGridStructure(table1, 1, 3);
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
                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 2.0f;
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);

                // 2D 치수 텍스트 크기 설정 (스케일 축소 후 가독성 유지를 위해 작게)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(5f);

                // ── 5. 4개 뷰 투영 + 스케일 조정 + 풍선/치수 변환 ──
                // targetHeight: 세로 40mm 기준 스케일 조정 (RescaleObject → Note/Measure 변환 순서)
                float targetH = 40f;  // 목표 세로 크기 (mm)

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
        /// 각 그리드 셀별로 3D 상태를 ApplyDrawingSheetView와 동일하게 적용 → 2D 투영
        /// ISO: 풍선번호(CreateIsoBalloonNotes), X/Y/Z: 치수선+보조선+풍선(ShowAllDimensions)
        /// 각 셀 크기의 90% 비율로 중앙 배치
        /// </summary>
        private int RenderSheetViewForDrawing(int row, int col, string viewDirection, DrawingSheetData sheet, float targetHeight = 0f)
        {
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
                shapeDrawingIds = ShowAllDimensions(viewDirection, true);
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

                // objId 정렬: bgObjId와 동일 스케일 + 정확한 위치 겹치기
                float bgFinalScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(bgObjId);
                float objScaleBefore = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);
                vizcore3d.Drawing2D.Object2D.RescaleObject(objId, bgFinalScale);

                // bgObjId 최종 중심 + 원본 중심 차이(×스케일)로 objId 위치 계산
                float bgCX1 = 0f, bgCY1 = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(bgObjId, ref bgCX1, ref bgCY1);

                float objCX_afterScale = 0f, objCY_afterScale = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(objId, ref objCX_afterScale, ref objCY_afterScale);

                float targetX = bgCX1 + (objCX0 - bgCX0) * bgFinalScale;
                float targetY = bgCY1 + (objCY0 - bgCY0) * bgFinalScale;

                float moveX = targetX - objCX_afterScale;
                float moveY = targetY - objCY_afterScale;

                System.Diagnostics.Debug.WriteLine($"[2PASS DEBUG] ── 원본 중심 ──");
                System.Diagnostics.Debug.WriteLine($"  bgObjId 원본중심: ({bgCX0:F2}, {bgCY0:F2})");
                System.Diagnostics.Debug.WriteLine($"  objId   원본중심: ({objCX0:F2}, {objCY0:F2})");
                System.Diagnostics.Debug.WriteLine($"  중심 차이(원본): ({objCX0 - bgCX0:F2}, {objCY0 - bgCY0:F2})");
                System.Diagnostics.Debug.WriteLine($"[2PASS DEBUG] ── 스케일 ──");
                System.Diagnostics.Debug.WriteLine($"  objId 스케일(RescaleObject 전): {objScaleBefore:F6}");
                System.Diagnostics.Debug.WriteLine($"  bgFinalScale: {bgFinalScale:F6}");
                System.Diagnostics.Debug.WriteLine($"[2PASS DEBUG] ── 피팅 후 중심 ──");
                System.Diagnostics.Debug.WriteLine($"  bgObjId 최종중심: ({bgCX1:F2}, {bgCY1:F2})");
                System.Diagnostics.Debug.WriteLine($"  objId   스케일후중심: ({objCX_afterScale:F2}, {objCY_afterScale:F2})");
                System.Diagnostics.Debug.WriteLine($"[2PASS DEBUG] ── 이동 계산 ──");
                System.Diagnostics.Debug.WriteLine($"  타겟 좌표: ({targetX:F2}, {targetY:F2})");
                System.Diagnostics.Debug.WriteLine($"  이동량: ({moveX:F2}, {moveY:F2})");

                vizcore3d.Drawing2D.Object2D.MoveObject(objId, moveX, moveY);

                // 이동 후 확인
                float objCX_final = 0f, objCY_final = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectCenter(objId, ref objCX_final, ref objCY_final);
                System.Diagnostics.Debug.WriteLine($"[2PASS DEBUG] ── 이동 후 ──");
                System.Diagnostics.Debug.WriteLine($"  objId   최종중심: ({objCX_final:F2}, {objCY_final:F2})");
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
            }

            // 7. 3D→2D 변환 (스케일 조정 후에 수행 — 풍선/치수가 조정된 모델에 맞게 배치)

            // 보조선(ShapeDrawing) → 2D 개체로 추가 (모델 실선보다 가늘게)
            if (shapeDrawingIds != null && shapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            }

            // 풍선번호(Note) → 2D (텍스트 크기를 작게 설정하여 겹침 방지)
            vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemTextHeight(5.25f);
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
