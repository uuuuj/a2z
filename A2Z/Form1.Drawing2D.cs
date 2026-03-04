using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// 모델 파일이 위치한 솔루션 폴더 경로 반환
        /// </summary>
        public string GetSolutionPath()
        {
            string startPath = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo directory = new DirectoryInfo(startPath);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Any())
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            return startPath;
        }

        /// <summary>
        /// 2D 도면 생성 - VIZCore3D Drawing2D Template API 사용
        /// 템플릿(BOM 테이블 + 도면 정보) 배치 및 렌더링
        /// </summary>
        private void btnGenerate2D_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델을 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.View.EnableAnimation = false;
                vizcore3d.Review.Note.Clear();

                // 기존 2D 도면 초기화 (재생성 시 이전 캔버스/템플릿 제거)
                // 1) 2D 모드에서 캔버스 제거 (Model3D에서는 2D 접근 불가)
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;
                int canvasCount = vizcore3d.Drawing2D.View.GetCanvasCountBy2DView();
                for (int c = canvasCount; c >= 1; c--)
                {
                    vizcore3d.Drawing2D.View.RemoveCanvasBy2DView(c);
                }
                // 2) Drawing2D 툴바 토글로 템플릿 레이어 완전 초기화
                vizcore3d.ToolbarDrawing2D.Visible = false;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Model3D;
                vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;
                vizcore3d.ToolbarDrawing2D.Visible = true;

                // -------------------------------------------------------------------------
                // 템플릿 기능을 이용한 우측 테이블 배치 (BOM 목록 및 도면 정보)
                // -------------------------------------------------------------------------

                // 도면 템플릿 생성 프로세스 시작
                vizcore3d.Drawing2D.Template.CreateTemplate();

                // 노드 인덱스와 노트 ID를 매핑하기 위한 딕셔너리
                Dictionary<int, int> nodeToNoteMap = new Dictionary<int, int>();

                // BOM정보가 미수집이면 자동 수집
                if (lvDrawingBOMInfo.Items.Count == 0)
                {
                    CollectBOMInfo(false);
                }

                // ==========================================================
                // [표 1] BOM 정보 테이블 (우측 상단) — lvDrawingBOMInfo 기반
                // ==========================================================
                if (lvDrawingBOMInfo.Items.Count > 0)
                {
                    // 행: lvDrawingBOMInfo 항목 수 + 헤더(1), 열: 8 — No. 컬럼 축소 (헤더 축약 + ITEM/MATERIAL 패딩)
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

                    // 표를 도면 캔버스의 우측 영역(310mm 지점)에 배치
                    table1.X = 310;
                    table1.Y = 0;
                    vizcore3d.Drawing2D.Template.AddTemplateItem(table1);
                }

                // 풍선 번호표 생성 (bomList 기반)
                if (bomList != null && bomList.Count > 0)
                {
                    foreach (var bom in bomList)
                    {
                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX, bom.CenterY, bom.CenterZ);
                        VIZCore3D.NET.Data.Vertex3D notePos = new VIZCore3D.NET.Data.Vertex3D(bom.CenterX + 400, bom.CenterY, bom.CenterZ);

                        int id = vizcore3d.Review.Note.AddNoteSurface("TEMP", notePos, center);
                        nodeToNoteMap.Add(bom.Index, id);

                        VIZCore3D.NET.Data.NoteItem note = vizcore3d.Review.Note.GetItem(id);
                        note.UpdateText(id.ToString());
                    }
                }

                // ==========================================================
                // [표 2] 도면 정보 (우측 하단)
                // ==========================================================
                VIZCore3D.NET.Data.TemplateTableData table2 = new VIZCore3D.NET.Data.TemplateTableData(5, 4);
                table2.SetText(0, 0, "작성 일자"); table2.SetText(0, 1, DateTime.Now.ToString("yyyy-MM-dd (ddd)"));
                table2.SetText(1, 0, "소속");      table2.SetText(1, 1, "삼성중공업");
                table2.SetText(2, 0, "담당자");    table2.SetText(2, 1, "홍길동");
                table2.SetText(3, 0, "검수자");    table2.SetText(3, 1, "홍길동");
                table2.SetText(4, 0, "Image");     table2.SetText(4, 1, string.Format("{0}\\Logo.png", GetSolutionPath()));

                table2.X = 310;
                table2.Y = 200; // [표 1] 아래에 위치하도록 Y값 조정
                vizcore3d.Drawing2D.Template.AddTemplateItem(table2);

                // 작성된 템플릿(표, 로고 등)을 도면에 최종 렌더링 (로고 사이즈 지정)
                vizcore3d.Drawing2D.Template.RenderTemplate(60, 80);

                // -------------------------------------------------------------------------
                // 각 그리드 셀에 모델 투영 및 배치 (Grid 2x3 구조)
                // -------------------------------------------------------------------------

                // 2D 도면을 그릴 캔버스 선택 및 크기 확인
                int selectedCanvas = 1;
                vizcore3d.Drawing2D.View.SetSelectCanvas(selectedCanvas);
                float wCanvas = 0.0f, hCanvas = 0.0f;
                vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);

                // 2행 3열의 그리드 구조 생성 (우측 1열은 템플릿 테이블 영역)
                vizcore3d.Drawing2D.GridStructure.AddGridStructure(2, 3, wCanvas, hCanvas);
                vizcore3d.Drawing2D.GridStructure.SetMargins(15, 15, 15, 15);

                // 각 그리드 셀별로 다른 각도의 뷰를 투영
                // [1,1] ISO View
                RenderViewWithVisibleNotes(1, 1, VIZCore3D.NET.Data.CameraDirection.ISO_PLUS, nodeToNoteMap);

                // [1,2] TOP View (Z-)
                RenderViewWithVisibleNotes(1, 2, VIZCore3D.NET.Data.CameraDirection.Z_MINUS, nodeToNoteMap);

                // [2,1] LEFT View (X-)
                RenderViewWithVisibleNotes(2, 1, VIZCore3D.NET.Data.CameraDirection.X_MINUS, nodeToNoteMap);

                // [2,2] FRONT View (Y-)
                RenderViewWithVisibleNotes(2, 2, VIZCore3D.NET.Data.CameraDirection.Y_MINUS, nodeToNoteMap);

                // 2D 도면 최종 렌더링
                vizcore3d.Drawing2D.Render();

                MessageBox.Show("2D 도면 생성 완료!\n\n" +
                    "- ISO VIEW [1,1]\n" +
                    "- TOP VIEW [1,2]\n" +
                    "- LEFT VIEW [2,1]\n" +
                    "- FRONT VIEW [2,2]\n" +
                    "- BOM 목록 테이블 (우측 상단)\n" +
                    "- 도면 정보 테이블 (우측 하단)",
                    "2D 도면 생성", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"2D 도면 생성 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 특정 뷰 방향에서 보이는 노드만 계산하여 2D 도면에 투영하는 함수
        /// </summary>
        /// <param name="row">그리드 행 번호</param>
        /// <param name="col">그리드 열 번호</param>
        /// <param name="camDir">3D 카메라 이동 방향</param>
        /// <param name="map">부품 인덱스-노트 ID 매핑 딕셔너리</param>
        private void RenderViewWithVisibleNotes(int row, int col, VIZCore3D.NET.Data.CameraDirection camDir, Dictionary<int, int> map)
        {
            // 3D 카메라를 해당 각도로 이동
            vizcore3d.View.MoveCamera(camDir);

            vizcore3d.View.EnableBoxSelectionFrontObjectOnly = true;

            // 현재 카메라 각도에서 실제로 보이는 부품 리스트 추출
            List<VIZCore3D.NET.Data.Node> visibleNodes = vizcore3d.Object3D.FromScreen(false, VIZCore3D.NET.Data.LeafNodeKind.BODY);

            // 보이는 부품들 중 map에 등록된 부품의 번호표 ID만 수집
            List<int> visibleNoteIds = new List<int>();
            foreach (var node in visibleNodes)
            {
                int noteId;
                if (map.TryGetValue(node.Index, out noteId) || map.TryGetValue(node.ParentIndex, out noteId))
                {
                    visibleNoteIds.Add(noteId);
                }
            }

            // 2D 모델 투영체 생성 (현재 3D 뷰 각도를 그대로 2D로 변환)
            int objId = vizcore3d.Drawing2D.Object2D.Create2DViewObjectWithModelAtCanvasOrigin(VIZCore3D.NET.Data.Drawing2D_ModelViewKind.CURRENT);

            // 지정된 그리드 셀(row, col) 중앙에 모델을 꽉 차게 배치
            vizcore3d.Drawing2D.Object2D.FitObjectToGridCellAspect(row, col, objId, VIZCore3D.NET.Data.GridHorizontalAlignment.Center, VIZCore3D.NET.Data.GridVerticalAlignment.Middle);

            // 보이는 부품의 번호표들만 도면에 투영 (ISO 뷰에서만 풍선 표시)
            if (camDir == VIZCore3D.NET.Data.CameraDirection.ISO_PLUS && visibleNoteIds.Count > 0)
            {
                vizcore3d.Drawing2D.View.Add2DNoteFrom3DNote(visibleNoteIds.ToArray());
            }

            //측정이 표기가 필요한경우 (X/Y/Z 축 뷰에서만 치수 표시)
            if(camDir != VIZCore3D.NET.Data.CameraDirection.ISO_PLUS)
            {
                List<int> visibleMeasureIds = new List<int>();

                List<VIZCore3D.NET.Data.MeasureItem> listMeasure = vizcore3d.Review.Measure.Items;
                foreach(var measure in listMeasure)
                {
                    if(measure.Visible)
                        visibleMeasureIds.Add(measure.ID);
                }
                vizcore3d.Drawing2D.Measure.Add2DMeasureFrom3DMeasure(visibleMeasureIds.ToArray());
            }
        }

        /// <summary>
        /// 지정된 뷰 방향에 맞는 치수를 추가 (2D 도면 생성용 - 멀티레벨 로직)
        /// ShowAllDimensions와 동일한 baseline 기준 오프셋 + IsTotal 분리 + 짧은 치수 레벨 분리
        /// 색상만 Black(인쇄용)으로 적용
        /// </summary>
        private void AddDimensionsForView(string viewDirection)
        {
            if (chainDimensionList == null || chainDimensionList.Count == 0) return;

            // 뷰 방향에 따라 표시할 축 결정
            List<string> visibleAxes = new List<string>();
            switch (viewDirection)
            {
                case "X":
                    visibleAxes.Add("Y");
                    visibleAxes.Add("Z");
                    break;
                case "Y":
                    visibleAxes.Add("X");
                    visibleAxes.Add("Z");
                    break;
                case "Z":
                    visibleAxes.Add("X");
                    visibleAxes.Add("Y");
                    break;
            }

            // 해당 축들의 치수 필터링 (전체 표시 - 최소 치수 필터 없음)
            var displayList = chainDimensionList.Where(d => visibleAxes.Contains(d.Axis)).ToList();

            if (displayList.Count == 0) return;

            // 측정 스타일 설정 - 정수만 표시, 검은색(인쇄용), 배경 투명, 테두리 없음
            VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
            measureStyle.Prefix = false;
            measureStyle.Unit = false;
            measureStyle.NumberOfDecimalPlaces = 0;
            measureStyle.DX_DY_DZ = false;
            measureStyle.Frame = false;
            measureStyle.ContinuousDistance = false;
            measureStyle.BackgroundTransparent = true;
            measureStyle.FontColor = System.Drawing.Color.Black;
            measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
            measureStyle.FontBold = true;
            measureStyle.LineColor = System.Drawing.Color.Black;
            measureStyle.LineWidth = 1;
            measureStyle.ArrowColor = System.Drawing.Color.Black;
            measureStyle.ArrowSize = 8;
            measureStyle.AssistantLine = false;  // 커스텀 보조선(Osnap→치수선)만 사용
            measureStyle.AlignDistanceText = true;
            measureStyle.AlignDistanceTextPosition = 0;
            measureStyle.AlignDistanceTextMargine = 3;
            vizcore3d.Review.Measure.SetStyle(measureStyle);

            // 선택된 모델의 바운딩 박스에서 최소값 계산 (baseline 기준)
            float globalMinX = float.MaxValue, globalMinY = float.MaxValue, globalMinZ = float.MaxValue;
            if (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
            {
                foreach (int nodeIdx in xraySelectedNodeIndices)
                {
                    BOMData bom = bomList.FirstOrDefault(b => b.Index == nodeIdx);
                    if (bom != null)
                    {
                        globalMinX = Math.Min(globalMinX, bom.MinX);
                        globalMinY = Math.Min(globalMinY, bom.MinY);
                        globalMinZ = Math.Min(globalMinZ, bom.MinZ);
                    }
                }
            }
            // 선택된 노드가 없으면 치수 포인트에서 최소값 사용
            if (globalMinX == float.MaxValue)
            {
                foreach (var dim in chainDimensionList)
                {
                    globalMinX = Math.Min(globalMinX, Math.Min(dim.StartPoint.X, dim.EndPoint.X));
                    globalMinY = Math.Min(globalMinY, Math.Min(dim.StartPoint.Y, dim.EndPoint.Y));
                    globalMinZ = Math.Min(globalMinZ, Math.Min(dim.StartPoint.Z, dim.EndPoint.Z));
                }
            }

            // 부재 크기 기반 동적 오프셋 계산
            float dimMaxX = float.MinValue, dimMaxY = float.MinValue, dimMaxZ = float.MinValue;
            foreach (var dim in displayList)
            {
                dimMaxX = Math.Max(dimMaxX, Math.Max(dim.StartPoint.X, dim.EndPoint.X));
                dimMaxY = Math.Max(dimMaxY, Math.Max(dim.StartPoint.Y, dim.EndPoint.Y));
                dimMaxZ = Math.Max(dimMaxZ, Math.Max(dim.StartPoint.Z, dim.EndPoint.Z));
            }
            float extentX = dimMaxX - globalMinX;
            float extentY = dimMaxY - globalMinY;
            float extentZ = dimMaxZ - globalMinZ;
            float modelDiag = (float)Math.Sqrt(extentX * extentX + extentY * extentY + extentZ * extentZ);

            float baseOffset = Math.Max(modelDiag * 0.12f, 30f);   // 1단 체인 치수 오프셋
            float levelSpacing = Math.Max(modelDiag * 0.08f, 25f);  // 레벨 간 간격
            float minDimensionLength = 15.0f;  // 폰트가 들어갈 최소 치수선 길이

            // 보조선(Extension Line) 저장용 리스트
            List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

            // 축별로 그룹화하여 처리 (IsTotal 제외한 순차 치수만)
            var sequentialDims = displayList.Where(d => !d.IsTotal).GroupBy(d => d.Axis);
            var totalDims = displayList.Where(d => d.IsTotal).ToList();

            // 축별 최대 레벨 추적 (Total offset 계산용)
            Dictionary<string, int> axisMaxLevel = new Dictionary<string, int>
            {
                { "X", 1 }, { "Y", 1 }, { "Z", 1 }
            };

            // 축별 전체 치수 중복 여부 추적
            Dictionary<string, bool> axisTotalRedundant = new Dictionary<string, bool>();

            foreach (var axisGroup in sequentialDims)
            {
                string axis = axisGroup.Key;
                var dims = axisGroup.OrderBy(d =>
                {
                    switch (axis)
                    {
                        case "X": return -d.StartPoint.X;
                        case "Y": return -d.StartPoint.Y;
                        case "Z": return -d.StartPoint.Z;
                        default: return 0f;
                    }
                }).ToList();

                if (dims.Count == 0) continue;

                var firstPoint = dims[0].StartPoint;
                int shortCount = 0;  // 짧은 치수 발생 횟수 (레벨 결정용)
                bool lastDimWasShort = false;

                for (int i = 0; i < dims.Count; i++)
                {
                    if (dims[i].Distance < minDimensionLength)
                    {
                        // 짧은 치수 → Level 1에는 그리지 않음
                        // 처음(firstPoint)부터 이 짧은 치수 끝까지 누적 치수를 다음 레벨에 그리기
                        shortCount++;
                        float levelOffset = baseOffset + (levelSpacing * shortCount);

                        DrawDimension(firstPoint, dims[i].EndPoint, axis,
                            levelOffset, globalMinX, globalMinY, globalMinZ,
                            viewDirection, extensionLines);
                        lastDimWasShort = true;
                    }
                    else
                    {
                        // 정상 길이 치수 → Level 1에 그리기
                        DrawDimension(dims[i].StartPoint, dims[i].EndPoint, axis,
                            baseOffset, globalMinX, globalMinY, globalMinZ,
                            viewDirection, extensionLines);
                        lastDimWasShort = false;
                    }
                }

                axisMaxLevel[axis] = shortCount + 1;  // 사용된 최대 레벨

                // 마지막 치수가 짧으면 누적 치수(firstPoint→마지막EndPoint)가 전체 치수와 동일 → 중복
                axisTotalRedundant[axis] = lastDimWasShort;
            }

            // 전체 길이 치수 그리기 (IsTotal) - 최대 레벨 바깥에 (중복 시 스킵)
            foreach (var dim in totalDims)
            {
                // 마지막 순차 치수가 짧아서 누적 치수가 이미 전체를 커버하면 스킵
                if (axisTotalRedundant.ContainsKey(dim.Axis) && axisTotalRedundant[dim.Axis])
                    continue;

                int maxLevel = axisMaxLevel.ContainsKey(dim.Axis) ? axisMaxLevel[dim.Axis] : 1;
                float totalOffset = baseOffset + (levelSpacing * maxLevel);

                DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                    totalOffset, globalMinX, globalMinY, globalMinZ,
                    viewDirection, extensionLines);
            }

            // 보조선(Extension Line) 그리기 - 검은색(인쇄용)
            if (extensionLines.Count > 0)
            {
                vizcore3d.ShapeDrawing.AddLine(extensionLines, -1, System.Drawing.Color.Black, 0.5f, true);
            }
        }

        /// <summary>
        /// 2D 도면 폼 표시
        /// </summary>
        private void Show2DDrawingForm(Bitmap drawing)
        {
            Form drawingForm = new Form();
            drawingForm.Text = "2D 도면 - 4분할 뷰";
            drawingForm.Size = new System.Drawing.Size(drawing.Width + 40, drawing.Height + 80);
            drawingForm.StartPosition = FormStartPosition.CenterScreen;
            drawingForm.BackColor = System.Drawing.Color.White;

            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = new Bitmap(drawing);  // 복사본 생성
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.Location = new System.Drawing.Point(10, 10);

            // 저장 버튼
            Button btnSave = new Button();
            btnSave.Text = "이미지 저장";
            btnSave.Location = new System.Drawing.Point(10, drawing.Height + 15);
            btnSave.Size = new System.Drawing.Size(100, 25);
            btnSave.Click += (s, ev) =>
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "PNG 파일 (*.png)|*.png|JPEG 파일 (*.jpg)|*.jpg|BMP 파일 (*.bmp)|*.bmp";
                dlg.FileName = $"2D_Drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pictureBox.Image.Save(dlg.FileName);
                    MessageBox.Show($"저장 완료: {dlg.FileName}", "저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // 인쇄 버튼
            Button btnPrint = new Button();
            btnPrint.Text = "인쇄";
            btnPrint.Location = new System.Drawing.Point(120, drawing.Height + 15);
            btnPrint.Size = new System.Drawing.Size(80, 25);
            btnPrint.Click += (s, ev) =>
            {
                MessageBox.Show("인쇄 기능은 구현 예정입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            drawingForm.Controls.Add(pictureBox);
            drawingForm.Controls.Add(btnSave);
            drawingForm.Controls.Add(btnPrint);

            drawingForm.Show();
        }

        /// <summary>
        /// ISO 뷰에 부재 번호를 원 안에 표시하고 BOM 센터에서 지시선 시작
        /// </summary>
        private void DrawPartNumbersOnIsoView(Graphics g, int viewX, int viewY, int viewWidth, int viewHeight, Dictionary<int, PointF> bomScreenPositions)
        {
            if (bomList == null || bomList.Count == 0) return;
            if (bomScreenPositions == null || bomScreenPositions.Count == 0) return;

            Font numFont = new Font("Arial", 7, FontStyle.Bold);
            Pen circlePen = new Pen(System.Drawing.Color.Blue, 1);
            Pen linePen = new Pen(System.Drawing.Color.Blue, 1);

            int itemCount = Math.Min(bomList.Count, 20);
            int circleRadius = 10;

            float viewCenterX = viewX + viewWidth / 2.0f;
            float viewCenterY = viewY + viewHeight / 2.0f;

            for (int i = 0; i < itemCount; i++)
            {
                if (!bomScreenPositions.ContainsKey(i)) continue;

                int num = i + 1;

                PointF bomPos = bomScreenPositions[i];
                float bomScreenX = viewX + bomPos.X;
                float bomScreenY = viewY + bomPos.Y;

                bomScreenX = Math.Max(viewX + 20, Math.Min(viewX + viewWidth - 20, bomScreenX));
                bomScreenY = Math.Max(viewY + 20, Math.Min(viewY + viewHeight - 20, bomScreenY));

                float bubbleX, bubbleY;

                if (bomScreenX < viewCenterX)
                {
                    bubbleX = viewX + 20;
                }
                else
                {
                    bubbleX = viewX + viewWidth - 20;
                }

                bubbleY = viewY + 25 + (i % 10) * 25;

                g.DrawEllipse(circlePen, bubbleX - circleRadius, bubbleY - circleRadius,
                    circleRadius * 2, circleRadius * 2);

                string numText = num.ToString();
                SizeF textSize = g.MeasureString(numText, numFont);
                g.DrawString(numText, numFont, Brushes.Blue,
                    bubbleX - textSize.Width / 2, bubbleY - textSize.Height / 2);

                float dx = bomScreenX - bubbleX;
                float dy = bomScreenY - bubbleY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > 0)
                {
                    dx /= dist;
                    dy /= dist;
                }

                float lineStartX = bubbleX + dx * circleRadius;
                float lineStartY = bubbleY + dy * circleRadius;

                float bendX = lineStartX + (bomScreenX > bubbleX ? 15 : -15);
                float bendY = lineStartY;

                g.DrawLine(linePen, lineStartX, lineStartY, bendX, bendY);
                g.DrawLine(linePen, bendX, bendY, bomScreenX, bomScreenY);

                g.FillEllipse(Brushes.Blue, bomScreenX - 3, bomScreenY - 3, 6, 6);
            }

            numFont.Dispose();
            circlePen.Dispose();
            linePen.Dispose();
        }

        /// <summary>
        /// BOM 테이블 그리기
        /// </summary>
        private void DrawBOMTable(Graphics g, int x, int y, int width, int height, Font font, Font headerFont, Pen pen)
        {
            string[] headers = { "No", "Name", "Type", "Qty" };
            int[] colWidths = { 30, 130, 70, 35 };

            g.DrawRectangle(pen, x, y, width, height);

            g.FillRectangle(Brushes.LightGray, x + 1, y + 1, width - 2, 18);

            int colX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                g.DrawString(headers[i], headerFont, Brushes.Black, colX + 3, y + 3);
                colX += colWidths[i];
                if (i < headers.Length - 1)
                {
                    g.DrawLine(pen, x + GetColOffset(colWidths, i + 1), y, x + GetColOffset(colWidths, i + 1), y + height);
                }
            }

            g.DrawLine(pen, x, y + 18, x + width, y + 18);

            int rowHeight = 16;
            int rowY = y + 20;
            int maxRows = (height - 20) / rowHeight;

            for (int i = 0; i < lvBOM.Items.Count && i < maxRows; i++)
            {
                ListViewItem item = lvBOM.Items[i];
                string name = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";  // No. 칼럼 추가로 Name은 SubItems[1]
                string type = "PART";
                string qty = "1";

                g.DrawString(item.Text, font, Brushes.Black, x + 3, rowY);  // No. 칼럼 값 사용

                g.DrawString(TruncateString(name, 18), font, Brushes.Black, x + colWidths[0] + 3, rowY);

                if (item.SubItems.Count > 2)
                {
                    string angle = item.SubItems[2].Text;  // No. 칼럼 추가로 Angle은 SubItems[2]
                    type = string.IsNullOrEmpty(angle) || angle == "0" ? "PART" : "ASSEMBLY";
                }
                g.DrawString(type, font, Brushes.Black, x + colWidths[0] + colWidths[1] + 3, rowY);

                g.DrawString(qty, font, Brushes.Black, x + colWidths[0] + colWidths[1] + colWidths[2] + 3, rowY);

                rowY += rowHeight;
                if (rowY < y + height - 5)
                {
                    g.DrawLine(pen, x, rowY - 2, x + width, rowY - 2);
                }
            }
        }

        private int GetColOffset(int[] colWidths, int colIndex)
        {
            int offset = 0;
            for (int i = 0; i < colIndex && i < colWidths.Length; i++)
            {
                offset += colWidths[i];
            }
            return offset;
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 2) + "..";
        }

        /// <summary>
        /// 도면 정보 칸 (Title Block) 그리기
        /// </summary>
        private void DrawTitleBlock(Graphics g, int x, int y, int width, int height, Font font, Font titleFont, Pen pen)
        {
            g.DrawRectangle(new Pen(System.Drawing.Color.Black, 2), x, y, width, height);

            int rowHeight = height / 5;

            g.DrawRectangle(pen, x, y, width, rowHeight);
            g.DrawString("Company:", font, Brushes.Black, x + 5, y + 3);
            g.DrawString("SOFTHILLS Co., Ltd.", titleFont, Brushes.Black, x + 60, y + 3);

            g.DrawRectangle(pen, x, y + rowHeight, width, rowHeight);
            g.DrawString("Drawing:", font, Brushes.Black, x + 5, y + rowHeight + 3);
            string drawingName = !string.IsNullOrEmpty(currentFilePath) ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath) : "Untitled";
            g.DrawString(TruncateString(drawingName, 22), titleFont, Brushes.Black, x + 60, y + rowHeight + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 2, width, rowHeight);
            g.DrawString("DWG No:", font, Brushes.Black, x + 5, y + rowHeight * 2 + 3);
            g.DrawString($"DWG-{DateTime.Now:yyyyMMdd}-001", font, Brushes.Black, x + 60, y + rowHeight * 2 + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 3, width / 2, rowHeight);
            g.DrawString("Scale:", font, Brushes.Black, x + 5, y + rowHeight * 3 + 3);
            g.DrawString("1:50", font, Brushes.Black, x + 45, y + rowHeight * 3 + 3);

            g.DrawRectangle(pen, x + width / 2, y + rowHeight * 3, width / 2, rowHeight);
            g.DrawString("Date:", font, Brushes.Black, x + width / 2 + 5, y + rowHeight * 3 + 3);
            g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), font, Brushes.Black, x + width / 2 + 40, y + rowHeight * 3 + 3);

            g.DrawRectangle(pen, x, y + rowHeight * 4, width / 2, rowHeight);
            g.DrawString("Drawn:", font, Brushes.Black, x + 5, y + rowHeight * 4 + 3);
            g.DrawString("Engineer", font, Brushes.Black, x + 45, y + rowHeight * 4 + 3);

            g.DrawRectangle(pen, x + width / 2, y + rowHeight * 4, width / 2, rowHeight);
            g.DrawString("Appr:", font, Brushes.Black, x + width / 2 + 5, y + rowHeight * 4 + 3);
            g.DrawString("Manager", font, Brushes.Black, x + width / 2 + 40, y + rowHeight * 4 + 3);
        }

        /// <summary>
        /// PDF로 출력 (Microsoft Print to PDF 또는 인쇄 대화상자 사용)
        /// </summary>
        private void PrintToPDF(string pdfFileName)
        {
            try
            {
                using (System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument())
                {
                    printDoc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    printDoc.PrinterSettings.PrintToFile = true;
                    printDoc.PrinterSettings.PrintFileName = pdfFileName;

                    printDoc.DefaultPageSettings.Landscape = true;

                    printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20);

                    if (!printDoc.PrinterSettings.IsValid)
                    {
                        using (PrintDialog printDialog = new PrintDialog())
                        {
                            printDialog.Document = printDoc;
                            printDialog.AllowSomePages = false;
                            printDialog.AllowSelection = false;

                            if (printDialog.ShowDialog() == DialogResult.OK)
                            {
                                printDoc.PrinterSettings = printDialog.PrinterSettings;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    printDoc.PrintPage += (s, ev) =>
                    {
                        if (lastGeneratedDrawing != null)
                        {
                            float pageWidth = ev.PageBounds.Width - ev.PageSettings.Margins.Left - ev.PageSettings.Margins.Right;
                            float pageHeight = ev.PageBounds.Height - ev.PageSettings.Margins.Top - ev.PageSettings.Margins.Bottom;

                            float imgWidth = lastGeneratedDrawing.Width;
                            float imgHeight = lastGeneratedDrawing.Height;

                            float printScale = Math.Min(pageWidth / imgWidth, pageHeight / imgHeight);
                            float drawWidth = imgWidth * printScale;
                            float drawHeight = imgHeight * printScale;

                            float xPos = ev.PageSettings.Margins.Left + (pageWidth - drawWidth) / 2;
                            float yPos = ev.PageSettings.Margins.Top + (pageHeight - drawHeight) / 2;

                            ev.Graphics.DrawImage(lastGeneratedDrawing, xPos, yPos, drawWidth, drawHeight);
                        }
                        ev.HasMorePages = false;
                    };

                    printDoc.Print();

                    MessageBox.Show($"PDF 파일로 저장되었습니다.\n\n{pdfFileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show(
                    $"PDF 저장 중 오류가 발생했습니다.\n\n{ex.Message}\n\nPNG 파일로 대신 저장하시겠습니까?",
                    "PDF 저장 오류",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    string pngFileName = System.IO.Path.ChangeExtension(pdfFileName, ".png");
                    lastGeneratedDrawing.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{pngFileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// BOM 리스트 더블클릭 - 노드 위치로 이동 및 강조
        /// </summary>
        private void LvBOM_DoubleClick(object sender, EventArgs e)
        {
            if (lvBOM.SelectedItems.Count == 0) return;

            try
            {
                BOMData bom = lvBOM.SelectedItems[0].Tag as BOMData;
                if (bom == null) return;

                // 노드 리스트 생성
                List<int> indices = new List<int>();
                indices.Add(bom.Index);

                // 노드 선택 (기존 선택 해제하고 새로 선택)
                vizcore3d.Object3D.Select(indices, true, true);

                // 선택된 노드로 카메라 이동
                vizcore3d.View.FlyToObject3d(indices, 1.2f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"노드 이동 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clash 리스트 더블클릭 - 충돌 지점 표시
        /// </summary>
        private void LvClash_DoubleClick(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0) return;

            try
            {
                ClashData clash = lvClash.SelectedItems[0].Tag as ClashData;
                if (clash == null) return;

                // 기존 선택/색상 초기화 후 두 노드만 선택
                vizcore3d.Object3D.Color.RestoreColorAll();
                List<int> indices = new List<int>();
                indices.Add(clash.Index1);
                indices.Add(clash.Index2);
                vizcore3d.Object3D.Select(indices, true, true);

                // 선택된 노드로 카메라 이동
                vizcore3d.View.FlyToObject3d(indices, 1.2f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clash 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 수집 및 화면 표시 (GetBodyVertices 사용)
        /// </summary>
        private void btnCollectOsnap_Click(object sender, EventArgs e)
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();

            try
            {
                // 이전 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // Body 노드 가져오기
                List<VIZCore3D.NET.Data.Node> allBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);


                if (allBodyNodes == null || allBodyNodes.Count == 0)
                {
                    MessageBox.Show("로드된 Body 노드가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // X-Ray 필터링: 프로그래밍 선택 → 수동 X-Ray → 전체
                List<VIZCore3D.NET.Data.Node> bodyNodes;
                bool isFilteredMode;

                if (xraySelectedNodeIndices.Count > 0)
                {
                    bodyNodes = allBodyNodes.Where(n => xraySelectedNodeIndices.Contains(n.Index)).ToList();
                    isFilteredMode = true;
                }
                else
                {
                    bodyNodes = allBodyNodes.Where(n =>
                    {
                        var realNode = vizcore3d.Object3D.FromIndex(n.Index);
                        return realNode != null && realNode.Visible;
                    }).ToList();
                    isFilteredMode = bodyNodes.Count < allBodyNodes.Count && bodyNodes.Count > 0;
                    if (bodyNodes.Count == 0) bodyNodes = allBodyNodes;
                }


                //MessageBox.Show(
                //    $"Count = {bodyNodes.Count}\n" +
                //    $"First Index = {bodyNodes[0].Index}\n" +
                //    $"First Name = {bodyNodes[0].NodeName}",
                //    "Debug"
                //);

                System.Text.StringBuilder debugSb = new System.Text.StringBuilder();
                debugSb.AppendLine("=== Osnap 수집 (GetOsnapPoint API) ===\n");
                if (isFilteredMode)
                {
                    debugSb.AppendLine($"[선택 항목만 보기 모드]");
                    debugSb.AppendLine($"전체 Body 노드: {allBodyNodes.Count}개");
                    debugSb.AppendLine($"선택된 부재에서 수집: {bodyNodes.Count}개\n");
                }
                else
                {
                    debugSb.AppendLine($"Body 노드: {bodyNodes.Count}개\n");
                }

                int lineCount = 0, circleCount = 0, pointCount = 0, surfaceCount = 0;

                // 각 Body의 Osnap 포인트 수집
                foreach (var node in bodyNodes)
                {
                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList =
                        vizcore3d.Object3D.GetOsnapPoint(node.Index);

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        foreach (var osnap in osnapList)
                        {
                            // Osnap 유형별 처리
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    // 선: 시작점과 끝점 추가
                                    if (osnap.Start != null)
                                    {
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, node.NodeName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, node.NodeName));
                                    }
                                    lineCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    // 곡면/원형: 치수에서 제외
                                    circleCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    // 점: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, node.NodeName));
                                    }
                                    pointCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.SURFACE:
                                    // 표면: 제외 (곡면 데이터가 많음)
                                    surfaceCount++;
                                    break;
                            }
                        }
                    }
                }

                debugSb.AppendLine($"Osnap 유형별 개수:");
                debugSb.AppendLine($"  - LINE: {lineCount}개");
                debugSb.AppendLine($"  - CIRCLE: {circleCount}개");
                debugSb.AppendLine($"  - POINT: {pointCount}개");
                debugSb.AppendLine($"  - SURFACE: {surfaceCount}개 (제외됨)");
                debugSb.AppendLine($"\n총 수집된 좌표: {osnapPoints.Count}개");





                // ListView에 좌표와 부재 이름 추가 (심볼 표시 제거)
                if (osnapPoints.Count > 0)
                {
                    lvOsnap.Items.Clear();
                    for (int i = 0; i < osnapPointsWithNames.Count; i++)
                    {
                        var item = osnapPointsWithNames[i];
                        ListViewItem lvi = new ListViewItem((i + 1).ToString());
                        lvi.SubItems.Add(item.nodeName);
                        lvi.SubItems.Add(item.point.X.ToString("F2"));
                        lvi.SubItems.Add(item.point.Y.ToString("F2"));
                        lvi.SubItems.Add(item.point.Z.ToString("F2"));
                        var matchBom = bomList?.FirstOrDefault(b => b.Name == item.nodeName);
                        var sizes = GetHoleOrSlotForPoint(matchBom, item.point.X, item.point.Y, item.point.Z);
                        lvi.SubItems.Add(sizes.holeSize);
                        lvi.SubItems.Add(sizes.slotHoleSize);
                        lvOsnap.Items.Add(lvi);
                    }

                    debugSb.AppendLine($"\nListView에 {osnapPointsWithNames.Count}개 좌표 추가됨");
                }
                else
                {
                    debugSb.AppendLine("\nOsnap 포인트를 찾을 수 없습니다.");
                    lvOsnap.Items.Clear();
                }

                // 디버그 정보 출력
                System.Diagnostics.Debug.WriteLine(debugSb.ToString());
                MessageBox.Show(debugSb.ToString(), "Osnap 수집 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Osnap 수집 후 자동으로 치수 추출
                if (osnapPointsWithNames.Count > 0)
                {
                    ExtractDimensionForSelectedNodes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 수집 중 오류:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// PDF/PNG/JPEG 내보내기
        /// </summary>
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (lastGeneratedDrawing == null)
            {
                MessageBox.Show("먼저 2D 도면을 생성해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PDF 파일 (*.pdf)|*.pdf|PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg)|*.jpg";
            dlg.FilterIndex = 1;
            dlg.FileName = "2D_Drawing";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                string extension = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                if (extension == ".png")
                {
                    lastGeneratedDrawing.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    lastGeneratedDrawing.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    MessageBox.Show($"JPEG 파일로 저장되었습니다.\n\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (extension == ".pdf")
                {
                    PrintToPDF(dlg.FileName);
                }
                else
                {
                    lastGeneratedDrawing.Save(dlg.FileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show($"PNG 파일로 저장되었습니다.\n\n{dlg.FileName}.png", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 저장 중 오류가 발생했습니다.\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clash 리스트에서 선택한 항목만 뷰어에 표시
        /// </summary>
        private void btnClashShowSelected_Click(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0)
            {
                MessageBox.Show("Clash 항목을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // X-Ray 모드 활성화
                if (!vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = true;

                // X-Ray 옵션 설정
                vizcore3d.View.XRay.ColorType = VIZCore3D.NET.Data.XRayColorTypes.OBJECT_COLOR;
                vizcore3d.View.XRay.SelectionObject3DType = VIZCore3D.NET.Data.SelectionObject3DTypes.OPAQUE_OBJECT3D;

                // X-Ray 모드에서도 모서리(SilhouetteEdge) 항상 표시
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 이전 선택 해제
                vizcore3d.View.XRay.Clear();

                // 선택된 Clash 항목의 노드들만 표시
                List<int> selectedIndices = new List<int>();
                HashSet<string> relatedNodeNames = new HashSet<string>();
                List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> relatedBounds = new List<(float, float, float, float, float, float)>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        if (!selectedIndices.Contains(clash.Index1))
                            selectedIndices.Add(clash.Index1);
                        if (!selectedIndices.Contains(clash.Index2))
                            selectedIndices.Add(clash.Index2);

                        // 노드 이름 수집
                        if (!string.IsNullOrEmpty(clash.Name1))
                            relatedNodeNames.Add(clash.Name1);
                        if (!string.IsNullOrEmpty(clash.Name2))
                            relatedNodeNames.Add(clash.Name2);
                    }
                }

                // 선택된 노드만 표시
                vizcore3d.View.XRay.Select(selectedIndices, true);

                // X-Ray 모드에서 선택된 노드 인덱스 저장 (Osnap 수집 시 사용)
                xraySelectedNodeIndices = new List<int>(selectedIndices);

                // 선택된 노드로 화면 이동
                vizcore3d.View.FlyToObject3d(selectedIndices, 1.2f);

                // 이전 심볼 제거 후 새로 표시
                vizcore3d.Clash.ClearResultSymbol();

                // 선택된 충돌 지점에 심볼 표시
                List<VIZCore3D.NET.Data.Vertex3D> clashPoints = new List<VIZCore3D.NET.Data.Vertex3D>();
                List<VIZCore3D.NET.Data.ClashResultSymbols> symbols = new List<VIZCore3D.NET.Data.ClashResultSymbols>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        BOMData bom1 = bomList.FirstOrDefault(b => b.Index == clash.Index1);
                        BOMData bom2 = bomList.FirstOrDefault(b => b.Index == clash.Index2);

                        if (bom1 != null && bom2 != null)
                        {
                            float clashX = (Math.Max(bom1.MinX, bom2.MinX) + Math.Min(bom1.MaxX, bom2.MaxX)) / 2.0f;
                            float clashY = (Math.Max(bom1.MinY, bom2.MinY) + Math.Min(bom1.MaxY, bom2.MaxY)) / 2.0f;
                            float clashZ = (Math.Max(bom1.MinZ, bom2.MinZ) + Math.Min(bom1.MaxZ, bom2.MaxZ)) / 2.0f;

                            clashPoints.Add(new VIZCore3D.NET.Data.Vertex3D(clashX, clashY, clashZ));
                            symbols.Add(VIZCore3D.NET.Data.ClashResultSymbols.Triangle);

                            // 바운딩 박스 저장
                            relatedBounds.Add((
                                Math.Min(bom1.MinX, bom2.MinX),
                                Math.Max(bom1.MaxX, bom2.MaxX),
                                Math.Min(bom1.MinY, bom2.MinY),
                                Math.Max(bom1.MaxY, bom2.MaxY),
                                Math.Min(bom1.MinZ, bom2.MinZ),
                                Math.Max(bom1.MaxZ, bom2.MaxZ)
                            ));
                        }
                    }
                }

                if (clashPoints.Count > 0)
                {
                    vizcore3d.Clash.ShowResultSymbol(clashPoints, symbols, 10f, true, System.Drawing.Color.Yellow, false);
                }

                vizcore3d.EndUpdate();

                // 선택된 부재에 대해 자동으로 Osnap 수집 및 리스트 업데이트
                CollectOsnapForSelectedNodes(selectedIndices);

                // 선택된 부재에 대해 자동으로 치수 추출 및 리스트 업데이트
                ExtractDimensionForSelectedNodes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clash 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택된 노드에 대해서만 Osnap 수집 (자동 호출용)
        /// </summary>
        private void CollectOsnapForSelectedNodes(List<int> nodeIndices)
        {
            osnapPoints.Clear();
            osnapPointsWithNames.Clear();
            lvOsnap.Items.Clear();

            try
            {
                if (nodeIndices == null || nodeIndices.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[CollectOsnapForSelectedNodes] 노드 인덱스가 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 입력된 노드 인덱스: {string.Join(", ", nodeIndices)}");

                int lineCount = 0, circleCount = 0, pointCount = 0, surfaceCount = 0;

                // 각 노드 인덱스에 대해 직접 Osnap 포인트 수집
                foreach (int nodeIndex in nodeIndices)
                {
                    // 노드 정보 가져오기 (Part 풀네임 사용)
                    VIZCore3D.NET.Data.Node node = vizcore3d.Object3D.FromIndex(nodeIndex);
                    string nodeName = GetPartNameFromBodyIndex(nodeIndex, node != null ? node.NodeName : $"Node_{nodeIndex}");

                    System.Diagnostics.Debug.WriteLine($"  - 노드 인덱스 {nodeIndex}: {nodeName}");

                    List<VIZCore3D.NET.Data.OsnapVertex3D> osnapList =
                        vizcore3d.Object3D.GetOsnapPoint(nodeIndex);

                    if (osnapList != null && osnapList.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"    → Osnap 포인트: {osnapList.Count}개");

                        foreach (var osnap in osnapList)
                        {
                            // Osnap 유형별 처리
                            switch (osnap.Kind)
                            {
                                case VIZCore3D.NET.Data.OsnapKind.LINE:
                                    // 선: 시작점과 끝점 추가
                                    if (osnap.Start != null)
                                    {
                                        var startVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z);
                                        osnapPoints.Add(startVertex);
                                        osnapPointsWithNames.Add((startVertex, nodeName));
                                    }
                                    if (osnap.End != null)
                                    {
                                        var endVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z);
                                        osnapPoints.Add(endVertex);
                                        osnapPointsWithNames.Add((endVertex, nodeName));
                                    }
                                    lineCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.CIRCLE:
                                    // 곡면/원형: 치수에서 제외 (곡면 Osnap은 체인치수에 불필요)
                                    circleCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.POINT:
                                    // 점: 중심점 추가
                                    if (osnap.Center != null)
                                    {
                                        var pointVertex = new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z);
                                        osnapPoints.Add(pointVertex);
                                        osnapPointsWithNames.Add((pointVertex, nodeName));
                                    }
                                    pointCount++;
                                    break;

                                case VIZCore3D.NET.Data.OsnapKind.SURFACE:
                                    // 표면: 제외
                                    surfaceCount++;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    → Osnap 포인트 없음");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 총 수집: LINE={lineCount}, CIRCLE={circleCount}, POINT={pointCount}, SURFACE={surfaceCount}(제외)");
                System.Diagnostics.Debug.WriteLine($"[CollectOsnapForSelectedNodes] 총 좌표: {osnapPoints.Count}개");

                // ListView에 좌표와 부재 이름 추가 (심볼 표시 제거)
                if (osnapPoints.Count > 0)
                {
                    for (int i = 0; i < osnapPointsWithNames.Count; i++)
                    {
                        var item = osnapPointsWithNames[i];
                        ListViewItem lvi = new ListViewItem((i + 1).ToString());
                        lvi.SubItems.Add(item.nodeName);
                        lvi.SubItems.Add(item.point.X.ToString("F2"));
                        lvi.SubItems.Add(item.point.Y.ToString("F2"));
                        lvi.SubItems.Add(item.point.Z.ToString("F2"));
                        var matchBom = bomList?.FirstOrDefault(b => b.Name == item.nodeName);
                        var sizes = GetHoleOrSlotForPoint(matchBom, item.point.X, item.point.Y, item.point.Z);
                        lvi.SubItems.Add(sizes.holeSize);
                        lvi.SubItems.Add(sizes.slotHoleSize);
                        lvOsnap.Items.Add(lvi);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Clash 선택 부재] Osnap 자동 수집 완료: {nodeIndices.Count}개 부재에서 {osnapPoints.Count}개 좌표");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Osnap 자동 수집 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 노드에 대해서만 치수 추출 (자동 호출용)
        /// </summary>
        private void ExtractDimensionForSelectedNodes()
        {
            if (osnapPointsWithNames == null || osnapPointsWithNames.Count == 0)
            {
                return;
            }

            try
            {
                // 기존 측정 항목 제거
                vizcore3d.Review.Measure.Clear();
                chainDimensionList.Clear();
                lvDimension.Items.Clear();

                float tolerance = 0.5f;  // 허용 오차 0.5mm

                // 좌표 병합 (허용 오차 내 같은 좌표로 그룹화)
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(osnapPointsWithNames, tolerance);

                // X축 방향 체인 치수 (Y, Z가 같은 점들)
                var xDimensions = AddChainDimensionByAxis(mergedPoints, "X", tolerance);
                chainDimensionList.AddRange(xDimensions);

                // Y축 방향 체인 치수 (X, Z가 같은 점들)
                var yDimensions = AddChainDimensionByAxis(mergedPoints, "Y", tolerance);
                chainDimensionList.AddRange(yDimensions);

                // Z축 방향 체인 치수 (X, Y가 같은 점들)
                var zDimensions = AddChainDimensionByAxis(mergedPoints, "Z", tolerance);
                chainDimensionList.AddRange(zDimensions);

                // ListView에 추가 및 치수 번호 설정
                int no = 1;
                foreach (var dim in chainDimensionList)
                {
                    dim.No = no;  // 치수 데이터에 번호 저장
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

                System.Diagnostics.Debug.WriteLine($"[Clash 선택 부재] 치수 자동 추출: {chainDimensionList.Count}개 치수");

                // 치수 추출 후 자동으로 모든 치수 표시 (오프셋 + 보조선 스타일)
                ShowAllDimensions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"치수 자동 추출 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 전체 모델 보기 (X-Ray 모드 해제)
        /// </summary>
        private void btnClashShowAll_Click(object sender, EventArgs e)
        {
            try
            {
                vizcore3d.BeginUpdate();

                // 가공도 모드에서 숨긴 부재 복원
                RestoreAllPartsVisibility();

                // X-Ray 모드 해제
                if (vizcore3d.View.XRay.Enable)
                    vizcore3d.View.XRay.Enable = false;

                // X-Ray 선택 노드 리스트 초기화
                xraySelectedNodeIndices.Clear();

                // 색상 복원
                vizcore3d.Object3D.Color.RestoreColorAll();

                // 심볼 제거
                vizcore3d.Clash.ClearResultSymbol();

                // 모서리(SilhouetteEdge) 복원
                vizcore3d.View.SilhouetteEdge = true;
                vizcore3d.View.SilhouetteEdgeColor = Color.Green;

                // 전체 화면 맞춤
                vizcore3d.View.FitToView();

                vizcore3d.EndUpdate();

                // 모든 치수 다시 표시
                ShowAllDimensions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전체 보기 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 좌표 추가 (마우스 클릭으로 좌표 선택)
        /// </summary>
        private void btnOsnapAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Osnap 픽킹 이벤트 등록
                vizcore3d.GeometryUtility.OnOsnapPickingItem -= GeometryUtility_OnOsnapPickingItem;
                vizcore3d.GeometryUtility.OnOsnapPickingItem += GeometryUtility_OnOsnapPickingItem;

                // Osnap 모드 활성화
                vizcore3d.GeometryUtility.ShowOsnap(false, true, true, true);

                MessageBox.Show("뷰어에서 원하는 위치를 클릭하면 Osnap 좌표가 추가됩니다.\n(꼭짓점, 선, 원 등)", "Osnap 추가 모드", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 추가 모드 활성화 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Osnap 픽킹 이벤트 핸들러
        /// </summary>
        private void GeometryUtility_OnOsnapPickingItem(object sender, VIZCore3D.NET.Event.EventManager.OsnapPickingItemEventArgs e)
        {
            if (e.Point == null) return;

            try
            {
                // 새 좌표 추가
                VIZCore3D.NET.Data.Vertex3D point = new VIZCore3D.NET.Data.Vertex3D(e.Point.X, e.Point.Y, e.Point.Z);
                string nodeName = "수동 추가";

                // 선택된 개체가 있으면 이름 가져오기
                List<VIZCore3D.NET.Data.Node> selectedNodes = vizcore3d.Object3D.FromFilter(VIZCore3D.NET.Data.Object3dFilter.SELECTED_TOP);
                if (selectedNodes.Count > 0)
                {
                    nodeName = selectedNodes[0].NodeName;
                }

                osnapPoints.Add(point);
                osnapPointsWithNames.Add((point, nodeName));

                // ListView에 추가
                int newIndex = osnapPointsWithNames.Count;
                ListViewItem lvi = new ListViewItem(newIndex.ToString());
                lvi.SubItems.Add(nodeName);
                lvi.SubItems.Add(point.X.ToString("F2"));
                lvi.SubItems.Add(point.Y.ToString("F2"));
                lvi.SubItems.Add(point.Z.ToString("F2"));
                var matchBom = bomList?.FirstOrDefault(b => b.Name == nodeName);
                var sizes = GetHoleOrSlotForPoint(matchBom, point.X, point.Y, point.Z);
                lvi.SubItems.Add(sizes.holeSize);
                lvi.SubItems.Add(sizes.slotHoleSize);
                lvOsnap.Items.Add(lvi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 좌표 추가 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 Osnap 좌표 삭제
        /// </summary>
        private void btnOsnapDelete_Click(object sender, EventArgs e)
        {
            if (lvOsnap.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 Osnap 좌표를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 선택된 항목 인덱스를 역순으로 정렬하여 삭제 (인덱스 변동 방지)
                List<int> indicesToRemove = new List<int>();
                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    indicesToRemove.Add(lvi.Index);
                }
                indicesToRemove.Sort();
                indicesToRemove.Reverse();

                foreach (int index in indicesToRemove)
                {
                    if (index < osnapPoints.Count)
                    {
                        osnapPoints.RemoveAt(index);
                    }
                    if (index < osnapPointsWithNames.Count)
                    {
                        osnapPointsWithNames.RemoveAt(index);
                    }
                    lvOsnap.Items.RemoveAt(index);
                }

                // 번호 재정렬
                for (int i = 0; i < lvOsnap.Items.Count; i++)
                {
                    lvOsnap.Items[i].Text = (i + 1).ToString();
                }

                MessageBox.Show($"{indicesToRemove.Count}개의 좌표가 삭제되었습니다.", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 삭제 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 Osnap 좌표를 뷰어에서 표시 (심볼 숨김, 회전 중심만 설정)
        /// </summary>
        private void btnOsnapShowSelected_Click(object sender, EventArgs e)
        {
            if (lvOsnap.SelectedItems.Count == 0)
            {
                MessageBox.Show("Osnap 좌표를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 형상 제거 후 선택 좌표 표시
                vizcore3d.ShapeDrawing.Clear();

                // 선택된 좌표에 빨간 구 표시 + 카메라 이동
                List<VIZCore3D.NET.Data.Vertex3D> spherePoints = new List<VIZCore3D.NET.Data.Vertex3D>();
                VIZCore3D.NET.Data.Vertex3D targetPoint = null;

                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    int index = lvi.Index;
                    if (index < osnapPoints.Count)
                    {
                        var pt = osnapPoints[index];
                        if (targetPoint == null) targetPoint = pt;
                        spherePoints.Add(pt);
                    }
                }

                // 빨간 구 마커 표시
                if (spherePoints.Count > 0)
                {
                    vizcore3d.ShapeDrawing.AddSphere(spherePoints, 0, System.Drawing.Color.Red, 5.0f, true);
                }

                // 선택된 좌표에 풍선(Note) 표시 - 좌표 + 홀사이즈 정보
                vizcore3d.Review.Note.Clear();
                foreach (ListViewItem lvi in lvOsnap.SelectedItems)
                {
                    int index = lvi.Index;
                    if (index >= osnapPoints.Count) continue;
                    var pt = osnapPoints[index];
                    string nodeName = lvi.SubItems.Count > 1 ? lvi.SubItems[1].Text : "";
                    string holeSize = lvi.SubItems.Count > 5 ? lvi.SubItems[5].Text : "";

                    // 풍선 텍스트: 부재명 + 좌표 + 홀사이즈
                    string balloonText = $"{nodeName}\n({pt.X:F1}, {pt.Y:F1}, {pt.Z:F1})";
                    if (!string.IsNullOrEmpty(holeSize))
                        balloonText += $"\n{holeSize}";

                    // 텍스트 위치: 좌표에서 약간 오프셋
                    VIZCore3D.NET.Data.Vertex3D arrowPos = pt;
                    VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(
                        pt.X + 30f, pt.Y + 30f, pt.Z + 30f);

                    VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                    style.UseSymbol = false;
                    style.BackgroudTransparent = true;
                    style.FontBold = true;
                    style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE10;
                    style.FontColor = Color.DarkBlue;
                    style.LineColor = Color.DarkBlue;
                    style.LineWidth = 1;
                    style.ArrowColor = Color.Red;
                    style.ArrowWidth = 3;

                    vizcore3d.Review.Note.AddNoteSurface(balloonText, textPos, arrowPos, style);
                }

                // 첫 번째 선택 좌표로 카메라 이동
                if (targetPoint != null)
                {
                    // 해당 좌표 근처의 노드를 찾아서 FlyTo
                    List<int> nearNodeIndices = new List<int>();
                    foreach (var bom in bomList)
                    {
                        if (targetPoint.X >= bom.MinX && targetPoint.X <= bom.MaxX &&
                            targetPoint.Y >= bom.MinY && targetPoint.Y <= bom.MaxY &&
                            targetPoint.Z >= bom.MinZ && targetPoint.Z <= bom.MaxZ)
                        {
                            nearNodeIndices.Add(bom.Index);
                        }
                    }

                    if (nearNodeIndices.Count > 0)
                    {
                        vizcore3d.View.FlyToObject3d(nearNodeIndices, 1.5f);
                    }
                    else
                    {
                        vizcore3d.View.SetPivotPosition(targetPoint);
                    }
                }

                vizcore3d.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Osnap 좌표 설정 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 풍선 지우기 버튼 - Note와 ShapeDrawing(구 마커) 모두 제거
        /// </summary>
        private void btnOsnapClearBalloon_Click(object sender, EventArgs e)
        {
            try
            {
                vizcore3d.Review.Note.Clear();
                vizcore3d.ShapeDrawing.Clear();
            }
            catch { }
        }

    }
}
