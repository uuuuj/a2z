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
                lvi.SubItems.Add(sheet.MemberIndices.Count.ToString());
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
                    ShowBalloonNumbers("ISO", sheet.MemberIndices);
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

                // ── 1. 2D 초기화 (기존 도면 완전 삭제 후 새로 생성) ──
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;

                // 기존 2D 도면 개체 모두 삭제
                vizcore3d.Drawing2D.Object2D.DeleteAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.DeleteAllNonObjectBy2DView();

                // 기존 캔버스 모두 제거
                int canvasCount = vizcore3d.Drawing2D.View.GetCanvasCountBy2DView();
                for (int c = canvasCount; c >= 1; c--)
                {
                    vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(c);
                }

                // ViewMode 리셋으로 2D 뷰 완전 초기화
                vizcore3d.ToolbarDrawing2D.Visible = false;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Model3D;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;
                vizcore3d.ToolbarDrawing2D.Visible = true;

                // 2D 패널 크기 조정 (초기화 직후 설정 — 오른쪽 2D 패널을 크게)
                Application.DoEvents();
                if (vizcore3d.SplitContainer != null && vizcore3d.SplitContainer.Width > 0)
                {
                    vizcore3d.SplitContainer.SplitterDistance = (int)(vizcore3d.SplitContainer.Width * 0.2);
                    Application.DoEvents();
                }

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

                // ── 3. 템플릿 생성 ──
                vizcore3d.Drawing2D.Template.CreateTemplate();

                // [표1] BOM 테이블 (우측 상단) — No. 컬럼 축소 (헤더 축약 + ITEM/MATERIAL 패딩)
                if (lvDrawingBOMInfo.Items.Count > 0)
                {
                    VIZCore3D.NET.Data.TemplateTableData table1 = new VIZCore3D.NET.Data.TemplateTableData(lvDrawingBOMInfo.Items.Count + 1, 8);
                    table1.SetText(0, 0, "#");
                    table1.SetText(0, 1, "   ITEM   ");
                    table1.SetText(0, 2, "   MATERIAL   ");
                    table1.SetText(0, 3, "   SIZE   ");
                    table1.SetText(0, 4, "Q'TY");
                    table1.SetText(0, 5, "T/W");
                    table1.SetText(0, 6, "MA");
                    table1.SetText(0, 7, "FA");

                    for (int i = 0; i < lvDrawingBOMInfo.Items.Count; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i];
                        for (int col = 0; col < 8 && col < item.SubItems.Count; col++)
                        {
                            table1.SetText(i + 1, col, item.SubItems[col].Text);
                        }
                    }

                    table1.X = 310;
                    table1.Y = 0;
                    vizcore3d.Drawing2D.Template.AddTemplateItem(table1);
                }

                // [표2] 도면정보 (우측 하단)
                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));

                table2.X = 310;
                table2.Y = 200;
                vizcore3d.Drawing2D.Template.AddTemplateItem(table2);

                vizcore3d.Drawing2D.Template.RenderTemplate(30, 40);

                // ── 4. 그리드 구조 (2행 × 3열, 6등분) ──
                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                vizcore3d.Drawing2D.GridStructure.AddGridStructure(2, 3, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(30, 30, 30, 30);

                // 2D 라인 두께 설정: 모델 실선 굵게, 치수선/보조선 가늘게
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);

                // 2D 치수 텍스트 크기 설정 (기본값의 70%)
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(7f);

                // ── 5. 4개 뷰 투영 (3D에서 보이는 것과 동일하게) ──
                // [1,1] ISO — 풍선번호 (ApplyDrawingSheetView("ISO") 동일)
                int objIdISO = RenderSheetViewForDrawing(1, 1, "ISO", sheet, "ISO");

                // [1,2] Z축 — 치수선+보조선+풍선 (ApplyDrawingSheetView("Z") 동일)
                int objIdZ = RenderSheetViewForDrawing(1, 2, "Z", sheet, "Looking \"Z\"");

                // [2,1] Y축 — 치수선+보조선+풍선 (ApplyDrawingSheetView("Y") 동일)
                int objIdY = RenderSheetViewForDrawing(2, 1, "Y", sheet, "Looking \"Y\"");

                // [2,2] X축 — 치수선+보조선+풍선 (ApplyDrawingSheetView("X") 동일)
                int objIdX = RenderSheetViewForDrawing(2, 2, "X", sheet, "Looking \"X\"");

                // 4개 축 2D 오브젝트 크기 저장 (가로, 세로)
                float isoW = 0f, isoH = 0f;
                float zW = 0f, zH = 0f;
                float yW = 0f, yH = 0f;
                float xW = 0f, xH = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objIdISO, ref isoW, ref isoH);
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objIdZ, ref zW, ref zH);
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objIdY, ref yW, ref yH);
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objIdX, ref xW, ref xH);

                MessageBox.Show(
                    $"ISO: {isoW:F1} x {isoH:F1}\n" +
                    $"Z축: {zW:F1} x {zH:F1}\n" +
                    $"Y축: {yW:F1} x {yH:F1}\n" +
                    $"X축: {xW:F1} x {xH:F1}",
                    "2D 오브젝트 크기 (가로 x 세로)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // 세로 크기 조정 (각 축 개별 처리)
                // 1) 제일 큰 값이 40 미만 → 제일 긴 축을 40으로, 나머지도 같은 비율로 확대
                // 2) 40 이상 → 40으로 축소
                // 3) 15 미만 → 25로 확대
                float maxTarget = 40f;
                float minThreshold = 15f;
                float minTarget = 25f;

                float tallestH = Math.Max(Math.Max(isoH, zH), Math.Max(yH, xH));

                // 각 축의 objId와 높이를 배열로 관리
                int[] objIds = { objIdISO, objIdZ, objIdY, objIdX };
                float[] heights = { isoH, zH, yH, xH };

                if (tallestH > 0 && tallestH < maxTarget)
                {
                    // 1) 제일 큰 값이 40 미만 → 제일 긴 축 기준 비율로 전체 확대
                    float ratio = maxTarget / tallestH;
                    for (int i = 0; i < 4; i++)
                    {
                        if (heights[i] > 0)
                        {
                            float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objIds[i]);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objIds[i], curScale * ratio);
                        }
                    }
                }
                else if (tallestH >= maxTarget)
                {
                    // 2) 40 이상인 축 → 40으로 축소, 3) 15 미만인 축 → 25로 확대
                    for (int i = 0; i < 4; i++)
                    {
                        if (heights[i] >= maxTarget)
                        {
                            float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objIds[i]);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objIds[i], curScale * (maxTarget / heights[i]));
                        }
                        else if (heights[i] > 0 && heights[i] < minThreshold)
                        {
                            float curScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objIds[i]);
                            vizcore3d.Drawing2D.Object2D.RescaleObject(objIds[i], curScale * (minTarget / heights[i]));
                        }
                    }
                }

                // 4개 축 오브젝트 5% 확대
                //vizcore3d.Drawing2D.Object2D.RescaleObject(objIdISO, 0.05f);
                //vizcore3d.Drawing2D.Object2D.RescaleObject(objIdZ, 0.05f);
                //vizcore3d.Drawing2D.Object2D.RescaleObject(objIdY, 0.05f);
                //vizcore3d.Drawing2D.Object2D.RescaleObject(objIdX, 0.05f);

                // 4개 축 위치 미세 조정
                vizcore3d.Drawing2D.Object2D.MoveObject(objIdISO, 0, -5);
                vizcore3d.Drawing2D.Object2D.MoveObject(objIdZ, -15, -5);
                vizcore3d.Drawing2D.Object2D.MoveObject(objIdY, 0, 5);
                vizcore3d.Drawing2D.Object2D.MoveObject(objIdX, -15, 5);

                // ── 6. 최종 렌더링 ──
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
        /// ISO: 풍선번호(ShowBalloonNumbers), X/Y/Z: 치수선+보조선+풍선(ShowAllDimensions)
        /// 각 셀 크기의 90% 비율로 중앙 배치
        /// </summary>
        private int RenderSheetViewForDrawing(int row, int col, string viewDirection, DrawingSheetData sheet, string viewLabel = "")
        {
            List<int> shapeDrawingIds = null;
            List<int> visibleNoteIds = null;  // ISO 뷰 풍선 가시성 필터링용
            int labelNoteId = -1;  // 뷰 라벨 노트 ID (별도 변환용)

            // 1. 3D 어노테이션 초기화 (매 뷰마다 새로 그리기)
            vizcore3d.Review.Note.Clear();
            vizcore3d.Review.Measure.Clear();
            vizcore3d.ShapeDrawing.Clear();

            // 2. 시트 부재만 표시
            vizcore3d.BeginUpdate();

            // 모든 오브젝트 숨기기 → 시트 부재만 보이기 (2D 캡처 시 선택 부재만 포함 → 중앙 정렬)
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
            }

            // 셀 크기의 80%로 표시 (줌팩터 1.25 = 1/0.8)
            vizcore3d.View.FlyToObject3d(sheet.MemberIndices, 1.25f);

            // 4. 뷰별 3D 어노테이션 추가
            if (viewDirection == "ISO")
            {
                // ISO: btnGenerate2D_Click 동일 방식 — bomList 기반 AddNoteSurface + 가시성 필터링
                vizcore3d.BeginUpdate();
                vizcore3d.Object3D.Show(VIZCore3D.NET.Data.Object3DKind.ALL, true);
                vizcore3d.View.XRay.Enable = true;
                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;
                vizcore3d.View.XRay.Clear();
                vizcore3d.View.XRay.Select(sheet.MemberIndices, true);
                vizcore3d.EndUpdate();

                // bomList 기반 풍선 노트 생성 (모델 AABB 바깥 배치 + AABB 겹침 검사)
                Dictionary<int, int> nodeToNoteMap = new Dictionary<int, int>();
                if (bomList != null && bomList.Count > 0)
                {
                    // ISO_PLUS 등각 투영 2D 근사: 화면H = 0.707*(x-y), 화면V = 0.408*(x+y)+0.816*z
                    Func<float, float, float, (float h, float v)> isoProject = (px, py, pz) =>
                    {
                        return (0.707f * (px - py), 0.408f * (px + py) + 0.816f * pz);
                    };

                    // 시트 부재만 필터링
                    HashSet<int> sheetMemberSet = new HashSet<int>(sheet.MemberIndices);

                    // 시트 부재 기준 모델 3D 바운딩 박스 계산
                    float mMinX = float.MaxValue, mMinY = float.MaxValue, mMinZ = float.MaxValue;
                    float mMaxX = float.MinValue, mMaxY = float.MinValue, mMaxZ = float.MinValue;
                    int memberCount = 0;
                    foreach (var bom in bomList)
                    {
                        if (!sheetMemberSet.Contains(bom.Index)) continue;
                        mMinX = Math.Min(mMinX, bom.MinX); mMinY = Math.Min(mMinY, bom.MinY); mMinZ = Math.Min(mMinZ, bom.MinZ);
                        mMaxX = Math.Max(mMaxX, bom.MaxX); mMaxY = Math.Max(mMaxY, bom.MaxY); mMaxZ = Math.Max(mMaxZ, bom.MaxZ);
                        memberCount++;
                    }
                    if (memberCount == 0) goto SkipIsoBalloons;

                    // 모델 3D bbox의 8개 꼭짓점을 ISO 투영하여 2D AABB 구성
                    float[] cornersX = { mMinX, mMaxX };
                    float[] cornersY = { mMinY, mMaxY };
                    float[] cornersZ = { mMinZ, mMaxZ };
                    float modelH_min = float.MaxValue, modelH_max = float.MinValue;
                    float modelV_min = float.MaxValue, modelV_max = float.MinValue;
                    foreach (float cx in cornersX)
                        foreach (float cy in cornersY)
                            foreach (float cz in cornersZ)
                            {
                                var p = isoProject(cx, cy, cz);
                                modelH_min = Math.Min(modelH_min, p.h);
                                modelH_max = Math.Max(modelH_max, p.h);
                                modelV_min = Math.Min(modelV_min, p.v);
                                modelV_max = Math.Max(modelV_max, p.v);
                            }

                    // 모델 AABB에 마진 추가
                    float modelW = modelH_max - modelH_min;
                    float modelHt = modelV_max - modelV_min;
                    float aabbMargin = Math.Max(modelW, modelHt) * 0.08f;
                    modelH_min -= aabbMargin; modelH_max += aabbMargin;
                    modelV_min -= aabbMargin; modelV_max += aabbMargin;

                    float modelCenterH = (modelH_min + modelH_max) / 2f;
                    float modelCenterV = (modelV_min + modelV_max) / 2f;

                    // 풍선 크기 (AABB 겹침 검사용 반폭/반높이)
                    float balloonHalfW = 25f;
                    float balloonHalfH = 12f;
                    float balloonGap = 5f; // 풍선 간 최소 간격

                    // 배치된 풍선 AABB 목록
                    List<(float minH, float minV, float maxH, float maxV)> placedAABBs = new List<(float, float, float, float)>();

                    foreach (var bom in bomList)
                    {
                        if (!sheetMemberSet.Contains(bom.Index)) continue;

                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);

                        // 부재 중심의 2D 투영
                        var projBom = isoProject(bom.CenterX, bom.CenterY, bom.CenterZ);

                        // 모델 중심 → 부재 방향 (방사형 배치)
                        float dirH = projBom.h - modelCenterH;
                        float dirV = projBom.v - modelCenterV;
                        float dirLen = (float)Math.Sqrt(dirH * dirH + dirV * dirV);
                        if (dirLen < 0.001f) { dirH = 1f; dirV = 0f; dirLen = 1f; }
                        dirH /= dirLen;
                        dirV /= dirLen;

                        // 모델 AABB 바깥까지의 거리 계산 (ray-AABB exit)
                        float exitDist = 0f;
                        if (Math.Abs(dirH) > 0.001f)
                        {
                            float tH = dirH > 0 ? (modelH_max - projBom.h) / dirH : (modelH_min - projBom.h) / dirH;
                            exitDist = Math.Max(exitDist, tH);
                        }
                        if (Math.Abs(dirV) > 0.001f)
                        {
                            float tV = dirV > 0 ? (modelV_max - projBom.v) / dirV : (modelV_min - projBom.v) / dirV;
                            exitDist = Math.Max(exitDist, tV);
                        }
                        exitDist = Math.Max(exitDist, 0f) + balloonHalfW + balloonGap;

                        // 초기 후보: AABB 바깥
                        float candH = projBom.h + dirH * exitDist;
                        float candV = projBom.v + dirV * exitDist;

                        // AABB 겹침 검사 + 회전/거리 증가로 배치
                        bool positionFound = false;
                        for (int attempt = 0; attempt < 48 && !positionFound; attempt++)
                        {
                            // 풍선 AABB
                            float bMinH = candH - balloonHalfW;
                            float bMaxH = candH + balloonHalfW;
                            float bMinV = candV - balloonHalfH;
                            float bMaxV = candV + balloonHalfH;

                            // 모델 AABB와 겹침 검사
                            bool insideModel = bMinH < modelH_max && bMaxH > modelH_min &&
                                               bMinV < modelV_max && bMaxV > modelV_min;

                            // 다른 풍선과 AABB 겹침 검사
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
                                // 회전 + 거리 증가
                                float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                                if (attempt % 2 == 1) rotAngle = -rotAngle;
                                float cosA = (float)Math.Cos(rotAngle);
                                float sinA = (float)Math.Sin(rotAngle);
                                float newDirH = cosA * dirH - sinA * dirV;
                                float newDirV = sinA * dirH + cosA * dirV;
                                float newDist = exitDist * (1f + (attempt / 4) * 0.15f);
                                candH = projBom.h + newDirH * newDist;
                                candV = projBom.v + newDirV * newDist;
                            }
                        }

                        placedAABBs.Add((candH - balloonHalfW, candV - balloonHalfH,
                                         candH + balloonHalfW, candV + balloonHalfH));

                        // 2D 투영 좌표 → 3D 역산 (XY 평면 방향)
                        float initDirX = bom.CenterX - (mMinX + mMaxX) / 2f;
                        float initDirY = bom.CenterY - (mMinY + mMaxY) / 2f;
                        float initDirLen = (float)Math.Sqrt(initDirX * initDirX + initDirY * initDirY);
                        if (initDirLen < 0.001f) { initDirX = 1f; initDirY = 0f; initDirLen = 1f; }
                        initDirX /= initDirLen;
                        initDirY /= initDirLen;

                        // 3D 오프셋: 모델 대각선 기준 고정 거리 (2D AABB 방향만 활용)
                        float isoDiag = (float)Math.Sqrt(
                            (mMaxX - mMinX) * (mMaxX - mMinX) +
                            (mMaxY - mMinY) * (mMaxY - mMinY) +
                            (mMaxZ - mMinZ) * (mMaxZ - mMinZ));
                        float fixedOffset = Math.Max(200f, isoDiag * 0.35f);

                        // 2D 방향에서 회전 각도 추출하여 3D 방향에 적용
                        float offsetH = candH - projBom.h;
                        float offsetV = candV - projBom.v;
                        float angle2D = (float)Math.Atan2(offsetV, offsetH);
                        float cosA2 = (float)Math.Cos(angle2D);
                        float sinA2 = (float)Math.Sin(angle2D);
                        // 2D 각도를 3D XY 회전으로 적용
                        float noteX = bom.CenterX + (cosA2 * initDirX - sinA2 * initDirY) * fixedOffset;
                        float noteY = bom.CenterY + (sinA2 * initDirX + cosA2 * initDirY) * fixedOffset;
                        float noteZ = bom.CenterZ;

                        VIZCore3D.NET.Data.Vertex3D notePos = new VIZCore3D.NET.Data.Vertex3D(noteX, noteY, noteZ);
                        int id = vizcore3d.Review.Note.AddNoteSurface("TEMP", notePos, center);
                        nodeToNoteMap[bom.Index] = id;
                        VIZCore3D.NET.Data.NoteItem note = vizcore3d.Review.Note.GetItem(id);
                        note.UpdateText(id.ToString());
                    }
                }
                SkipIsoBalloons:

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

                // 풍선 생성 후 다시 시트 부재만 보이기 (2D 캡처용)
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

            // 4-1. 뷰 라벨: 2D 격자 하단 중앙에 직접 배치 (3D 노트 사용 안 함)
            // labelNoteId는 -1 유지 → 2D 변환 후 격자 좌표 기반으로 추가

            // 5. 은선 포함 2D 투영 (현재 3D 뷰를 은선 점선 포함하여 2D로 변환)
            int objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

            // 6등분 셀 중앙에 배치 후, 콘텐츠 영역에 맞도록 스케일 조정
            vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, objId,
                VIZCore3D.NET.Data.GridHorizontalAlignment.Center,
                VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

            {
                float cellW = vizcore3d.Drawing2D.GridStructure.GetGridCellWidth(row, col);
                float cellH = vizcore3d.Drawing2D.GridStructure.GetGridCellHeight(row, col);
                float marginL = vizcore3d.Drawing2D.GridStructure.GetGridCellLeftMargin(row, col);
                float marginR = vizcore3d.Drawing2D.GridStructure.GetGridCellRightMargin(row, col);
                float marginT = vizcore3d.Drawing2D.GridStructure.GetGridCellTopMargin(row, col);
                float marginB = vizcore3d.Drawing2D.GridStructure.GetGridCellBottomMargin(row, col);

                float contentW = cellW - marginL - marginR;
                float contentH = cellH - marginT - marginB;

                float objW = 0f, objH = 0f;
                vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);

                if (objW > 0 && objH > 0 && contentW > 0 && contentH > 0)
                {
                    // 콘텐츠 영역의 80%에 오브젝트가 맞도록 목표 스케일 계산
                    float targetW = contentW * 0.04f;
                    float targetH = contentH * 0.04f;
                    float scaleW = targetW / objW;
                    float scaleH = targetH / objH;
                    float fitScale = Math.Min(scaleW, scaleH);

                    if (fitScale > 0 && Math.Abs(fitScale - 1.0f) > 0.01f)
                    {
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, fitScale);
                    }
                }
            }

            // 6. 3D→2D 변환 (FitToGrid/Rescale 후에 수행)

            // 보조선(ShapeDrawing) → 2D 개체로 추가 (모델 실선보다 가늘게)
            if (shapeDrawingIds != null && shapeDrawingIds.Count > 0)
            {
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(shapeDrawingIds);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            }

            // 풍선번호(Note) → 2D
            if (visibleNoteIds != null)
            {
                // ISO: 가시성 필터링된 풍선만 2D로 변환
                if (visibleNoteIds.Count > 0)
                {
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
                }
            }
            else
            {
                // 비-ISO 뷰: 모든 풍선 노트 변환
                List<int> noteIds = new List<int>();
                List<VIZCore3D.NET.Data.NoteItem> notes = vizcore3d.Review.Note.Items;
                foreach (var note in notes)
                {
                    noteIds.Add(note.ID);
                }
                if (noteIds.Count > 0)
                {
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(noteIds.ToArray());
                }
            }

            // 뷰 라벨: 격자 셀 하단 중앙에 2D 노트로 직접 배치
            if (!string.IsNullOrEmpty(viewLabel))
            {
                float cellW2 = vizcore3d.Drawing2D.GridStructure.GetGridCellWidth(row, col);
                float cellH2 = vizcore3d.Drawing2D.GridStructure.GetGridCellHeight(row, col);

                // 격자 셀 좌상단 좌표 계산 (row, col은 1-based)
                float cellX = 0f;
                for (int c = 1; c < col; c++)
                    cellX += vizcore3d.Drawing2D.GridStructure.GetGridCellWidth(row, c);
                float cellY = 0f;
                for (int r = 1; r < row; r++)
                    cellY += vizcore3d.Drawing2D.GridStructure.GetGridCellHeight(r, col);

                // 하단 중앙: X=셀중앙, Y=셀하단+약간위(마진)
                float labelX = cellX + cellW2 / 2f;
                float labelY = cellY + cellH2 - 8f; // 하단에서 8 위
                VIZCore3D.NET.Data.Vertex3D lblPos = new VIZCore3D.NET.Data.Vertex3D(labelX, labelY, 0);

                VIZCore3D.NET.Data.NoteStyle lblStyle = vizcore3d.Review.Note.GetStyle();
                lblStyle.UseSymbol = false;
                lblStyle.BackgroudTransparent = true;
                lblStyle.UseTextBox = false;
                lblStyle.FontColor = Color.Black;
                lblStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE14;
                lblStyle.FontBold = true;
                lblStyle.LineWidth = 0;
                lblStyle.ArrowWidth = 0;

                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(0.01f);
                vizcore3d.Review.Note.AddNoteSurface(viewLabel, lblPos, lblPos, lblStyle);
                // 방금 추가한 노트를 2D로 변환
                var lastNotes = vizcore3d.Review.Note.Items;
                if (lastNotes.Count > 0)
                {
                    int lblId = lastNotes[lastNotes.Count - 1].ID;
                    vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(new int[] { lblId });
                }
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemLineWidth(2.0f);
            }

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
