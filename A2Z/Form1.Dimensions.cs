using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// 선택한 치수를 뷰어에서 표시 (축에 따라 직각 방향으로 표시)
        /// </summary>
        private void btnDimensionShowSelected_Click(object sender, EventArgs e)
        {
            if (lvDimension.SelectedItems.Count == 0)
            {
                MessageBox.Show("치수를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 측정 항목 유지 (Clear 하지 않음) - 선택한 치수만 추가
                vizcore3d.Clash.ClearResultSymbol();
                // 보조선도 유지

                // 측정 스타일 설정 - 정수만 표시, 검은색
                VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
                measureStyle.Prefix = false;              // "Y축거리 = " 제거
                measureStyle.Unit = false;                // "mm" 제거
                measureStyle.NumberOfDecimalPlaces = 0;   // 소수점 없이 정수만 표시
                measureStyle.DX_DY_DZ = false;
                measureStyle.Frame = false;
                measureStyle.ContinuousDistance = false;
                measureStyle.BackgroundTransparent = false;
                measureStyle.BackgroundColor = System.Drawing.Color.White;
                measureStyle.FontColor = System.Drawing.Color.Blue;      // 검은색
                measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE14;
                measureStyle.FontBold = true;
                measureStyle.LineColor = System.Drawing.Color.Blue;      // 검은색
                measureStyle.LineWidth = 1;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;     // 검은색
                measureStyle.ArrowSize = 8;
                measureStyle.AlignDistanceText = true;
                measureStyle.AlignDistanceTextMargine = 3;
                vizcore3d.Review.Measure.SetStyle(measureStyle);

                // 축별 오프셋 카운터 (여러 치수 동시 표시 시 겹치지 않도록)
                Dictionary<string, int> axisOffsetCount = new Dictionary<string, int>
                {
                    { "X", 0 }, { "Y", 0 }, { "Z", 0 }
                };
                float offsetStep = 50.0f;  // 치수 간 간격

                // 선택된 치수만 표시 (축에 따라 오프셋 적용)
                foreach (ListViewItem lvi in lvDimension.SelectedItems)
                {
                    ChainDimensionData dim = lvi.Tag as ChainDimensionData;
                    if (dim != null)
                    {
                        float currentOffset = axisOffsetCount[dim.Axis] * offsetStep;
                        axisOffsetCount[dim.Axis]++;

                        // 오프셋 적용하여 치수 추가
                        VIZCore3D.NET.Data.Vertex3D startVertex;
                        VIZCore3D.NET.Data.Vertex3D endVertex;

                        switch (dim.Axis)
                        {
                            case "X":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X, dim.StartPoint.Y - currentOffset, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X, dim.EndPoint.Y - currentOffset, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                                break;
                            case "Y":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X - currentOffset, dim.StartPoint.Y, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X - currentOffset, dim.EndPoint.Y, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                                break;
                            case "Z":
                                startVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.StartPoint.X - currentOffset, dim.StartPoint.Y, dim.StartPoint.Z);
                                endVertex = new VIZCore3D.NET.Data.Vertex3D(
                                    dim.EndPoint.X - currentOffset, dim.EndPoint.Y, dim.EndPoint.Z);
                                vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                                break;
                        }
                    }
                }

                // 선택된 치수의 중심점을 회전 중심으로 설정
                if (lvDimension.SelectedItems.Count > 0)
                {
                    ChainDimensionData firstDim = lvDimension.SelectedItems[0].Tag as ChainDimensionData;
                    if (firstDim != null)
                    {
                        float centerX = (firstDim.StartPoint.X + firstDim.EndPoint.X) / 2.0f;
                        float centerY = (firstDim.StartPoint.Y + firstDim.EndPoint.Y) / 2.0f;
                        float centerZ = (firstDim.StartPoint.Z + firstDim.EndPoint.Z) / 2.0f;

                        VIZCore3D.NET.Data.Vertex3D center = new VIZCore3D.NET.Data.Vertex3D(centerX, centerY, centerZ);
                        vizcore3d.View.SetPivotPosition(center);
                    }
                }

                //vizcore3d.MouseControl = vizcore3d.MouseControl { 4, 14};

                //vizcore3d.MouseControl |= MouseControls.Down_Left;
                //vizcore3d.MouseControl |= MouseControls.Up_Left;
                vizcore3d.EndUpdate();

                MessageBox.Show($"{lvDimension.SelectedItems.Count}개의 치수가 표시되었습니다.", "치수 표시", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 표시 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 선택한 치수 삭제
        /// </summary>
        private void btnDimensionDelete_Click(object sender, EventArgs e)
        {
            if (lvDimension.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 치수를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 선택된 항목 인덱스를 역순으로 정렬하여 삭제
                List<int> indicesToRemove = new List<int>();
                foreach (ListViewItem lvi in lvDimension.SelectedItems)
                {
                    indicesToRemove.Add(lvi.Index);
                }
                indicesToRemove.Sort();
                indicesToRemove.Reverse();

                foreach (int index in indicesToRemove)
                {
                    if (index < chainDimensionList.Count)
                    {
                        chainDimensionList.RemoveAt(index);
                    }
                    lvDimension.Items.RemoveAt(index);
                }

                // 번호 재정렬 (ListView와 데이터 모두 갱신)
                for (int i = 0; i < lvDimension.Items.Count; i++)
                {
                    lvDimension.Items[i].Text = (i + 1).ToString();
                    if (i < chainDimensionList.Count)
                    {
                        chainDimensionList[i].No = i + 1;
                    }
                }

                // 뷰어의 측정 항목 갱신 (AddCustomAxisDistance API 사용)
                vizcore3d.Review.Measure.Clear();
                foreach (var dim in chainDimensionList)
                {
                    VIZCore3D.NET.Data.Vertex3D startVertex = new VIZCore3D.NET.Data.Vertex3D(
                        dim.StartPoint.X, dim.StartPoint.Y, dim.StartPoint.Z);
                    VIZCore3D.NET.Data.Vertex3D endVertex = new VIZCore3D.NET.Data.Vertex3D(
                        dim.EndPoint.X, dim.EndPoint.Y, dim.EndPoint.Z);

                    switch (dim.Axis)
                    {
                        case "X":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                            break;
                        case "Y":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                            break;
                        case "Z":
                            vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                            break;
                    }
                }

                MessageBox.Show($"{indicesToRemove.Count}개의 치수가 삭제되었습니다.\n남은 치수: {chainDimensionList.Count}개", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"치수 삭제 중 오류:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// X축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisX_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("X");
        }

        /// <summary>
        /// Y축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisY_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Y");
        }

        /// <summary>
        /// Z축 방향 보기 버튼 - 기존 호환용
        /// </summary>
        private void btnShowAxisZ_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("Z");
        }

        /// <summary>
        /// ISO 방향 보기 버튼 (등각 투영) - 기존 호환용
        /// </summary>
        private void btnShowISO_Click(object sender, EventArgs e)
        {
            ApplyGlobalView("ISO");
        }

        /// <summary>
        /// 풍선 위치 수동 조정 다이얼로그
        /// </summary>
        private void btnBalloonAdjust_Click(object sender, EventArgs e)
        {
            if (bomList == null || bomList.Count == 0)
            {
                MessageBox.Show("BOM 데이터가 없습니다. 먼저 BOM을 수집하세요.", "알림");
                return;
            }
            if (string.IsNullOrEmpty(currentBalloonView))
            {
                MessageBox.Show("먼저 뷰(ISO/X/Y/Z) 버튼을 클릭하여 풍선을 표시하세요.", "알림");
                return;
            }

            // BOM 리스트에서 선택된 항목 확인
            int selectedIdx = -1;
            if (lvBOM.SelectedItems.Count > 0)
            {
                selectedIdx = lvBOM.SelectedItems[0].Index;
            }

            // 다이얼로그 생성
            Form dlg = new Form();
            dlg.Text = "풍선 위치 조정";
            dlg.Size = new Size(380, 340);
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.StartPosition = FormStartPosition.CenterParent;

            // 부재 선택 콤보박스
            Label lblSelect = new Label { Text = "부재 선택:", Location = new Point(15, 15), AutoSize = true };
            ComboBox cmbMember = new ComboBox();
            cmbMember.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMember.Location = new Point(15, 38);
            cmbMember.Size = new Size(330, 25);
            for (int i = 0; i < bomList.Count; i++)
                cmbMember.Items.Add($"{i + 1}. {bomList[i].Name}");
            cmbMember.SelectedIndex = selectedIdx >= 0 && selectedIdx < bomList.Count ? selectedIdx : 0;

            // 현재 위치 표시
            Label lblCurrent = new Label { Text = "현재 풍선 위치:", Location = new Point(15, 75), AutoSize = true };
            Label lblCurrentPos = new Label { Location = new Point(15, 98), Size = new Size(330, 20), ForeColor = Color.Blue };

            // 오프셋 입력
            Label lblX = new Label { Text = "X 오프셋:", Location = new Point(15, 130), AutoSize = true };
            NumericUpDown nudX = new NumericUpDown();
            nudX.Location = new Point(100, 128); nudX.Size = new Size(120, 25);
            nudX.Minimum = -100000; nudX.Maximum = 100000; nudX.DecimalPlaces = 1; nudX.Increment = 10;

            Label lblY = new Label { Text = "Y 오프셋:", Location = new Point(15, 163), AutoSize = true };
            NumericUpDown nudY = new NumericUpDown();
            nudY.Location = new Point(100, 161); nudY.Size = new Size(120, 25);
            nudY.Minimum = -100000; nudY.Maximum = 100000; nudY.DecimalPlaces = 1; nudY.Increment = 10;

            Label lblZ = new Label { Text = "Z 오프셋:", Location = new Point(15, 196), AutoSize = true };
            NumericUpDown nudZ = new NumericUpDown();
            nudZ.Location = new Point(100, 194); nudZ.Size = new Size(120, 25);
            nudZ.Minimum = -100000; nudZ.Maximum = 100000; nudZ.DecimalPlaces = 1; nudZ.Increment = 10;

            // 현재 위치 업데이트 함수
            Action updateCurrentPos = () =>
            {
                int idx = cmbMember.SelectedIndex;
                if (idx >= 0 && balloonOverrides.ContainsKey(idx))
                {
                    var pos = balloonOverrides[idx];
                    lblCurrentPos.Text = $"X={pos[0]:F1}, Y={pos[1]:F1}, Z={pos[2]:F1}";
                }
                else
                {
                    lblCurrentPos.Text = "(자동 배치 - 위치 미정)";
                }
            };
            cmbMember.SelectedIndexChanged += (s2, e2) => { updateCurrentPos(); nudX.Value = 0; nudY.Value = 0; nudZ.Value = 0; };
            updateCurrentPos();

            // 적용 버튼
            Button btnApply = new Button { Text = "적용", Location = new Point(15, 240), Size = new Size(100, 35) };
            btnApply.Click += (s2, e2) =>
            {
                int idx = cmbMember.SelectedIndex;
                if (idx < 0) return;

                if (balloonOverrides.ContainsKey(idx))
                {
                    balloonOverrides[idx][0] += (float)nudX.Value;
                    balloonOverrides[idx][1] += (float)nudY.Value;
                    balloonOverrides[idx][2] += (float)nudZ.Value;
                }
                else
                {
                    // 부재 중심 + 오프셋으로 초기 위치 설정
                    var bom = bomList[idx];
                    balloonOverrides[idx] = new float[] {
                        bom.CenterX + (float)nudX.Value,
                        bom.CenterY + (float)nudY.Value,
                        bom.CenterZ + (float)nudZ.Value
                    };
                }

                nudX.Value = 0; nudY.Value = 0; nudZ.Value = 0;
                vizcore3d.Review.Note.Clear();
                CreateIsoBalloonNotes(currentBalloonMemberIndices);
                updateCurrentPos();
            };

            // 초기화 버튼
            Button btnReset = new Button { Text = "전체 초기화", Location = new Point(125, 240), Size = new Size(100, 35) };
            btnReset.Click += (s2, e2) =>
            {
                balloonOverrides.Clear();
                vizcore3d.Review.Note.Clear();
                CreateIsoBalloonNotes(currentBalloonMemberIndices);
                updateCurrentPos();
            };

            // 닫기 버튼
            Button btnClose = new Button { Text = "닫기", Location = new Point(235, 240), Size = new Size(100, 35) };
            btnClose.Click += (s2, e2) => dlg.Close();

            dlg.Controls.AddRange(new Control[] { lblSelect, cmbMember, lblCurrent, lblCurrentPos,
                lblX, nudX, lblY, nudY, lblZ, nudZ, btnApply, btnReset, btnClose });
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// 보조선 길이 정책 단일 출처 (2026-06-03 가공도 통일).
        /// 캔버스 절대 mm를 모델좌표 offset으로 역산. 기본 제작도 5/5(1단 5·전체 10mm).
        /// 가공도는 전용 상수(canvasBase/canvasLvl 인자)로 따로 줄인다 (2026-06-23: 1단 2·전체 4mm).
        /// 제작도(ShowAllDimensions)·가공도(BuildMfgSceneCore·EA 두번째 뷰) 공용 — 식은 한 곳, 값만 분기.
        /// canvasScale은 호출자가 0 초과를 보장.
        /// </summary>
        private void ComputeCanvasAbsoluteOffsets(
            float canvasScale, out float baseOffset, out float levelSpacing, out float canvasMaxOff,
            float canvasBase = 5f, float canvasLvl = 5f)
        {
            canvasMaxOff = canvasBase + canvasLvl;
            baseOffset   = canvasBase / canvasScale;
            levelSpacing = canvasLvl  / canvasScale;
        }

        /// <summary>
        /// 치수 표시 - Smart Dimension Filtering Algorithm 적용
        ///
        /// 적용된 알고리즘:
        /// 1. Priority-Based Filtering: 치수 크기/중요도에 따른 우선순위 할당
        /// 2. Greedy Label Placement: 겹침 방지하면서 우선순위 높은 순으로 배치
        /// 3. Smart Grouping: 연속된 짧은 치수들을 누적 치수로 병합
        /// 4. Multi-Level Layout: 레벨 기반 정렬로 깔끔한 배치
        ///
        /// viewDirection: null=모든 축, "X"/"Y"/"Z"=해당 단면 치수만
        /// </summary>
        private List<int> ShowAllDimensions(
            string viewDirection = null,
            bool forDrawing2D = false,
            float canvasScaleOverride = -1f)
        {
            // T-028: 치수 계산은 호출자가 chainDimensionList에 미리 채움 (치수추출·시트 선택·2D 출력 모두 동일).
            // 본 메서드는 chainDimensionList를 viewDirection 기준으로 필터링해 3D 뷰에 표시하는 역할만.
            if (chainDimensionList == null || chainDimensionList.Count == 0)
                return new List<int>();

            List<ChainDimensionData> displayList;
            if (string.IsNullOrEmpty(viewDirection))
            {
                // 전체 치수 표시 (치수추출 버튼 직후 기본)
                displayList = chainDimensionList;
            }
            else
            {
                // 뷰별 필터: ChainDimensionData.ViewDirection 필드 기준 (콤마 구분 "X,Y" 지원)
                displayList = chainDimensionList.Where(d =>
                    string.IsNullOrEmpty(d.ViewDirection) ||
                    d.ViewDirection.Split(',').Contains(viewDirection)
                ).ToList();
            }

            if (displayList.Count == 0) return new List<int>();

            List<int> shapeDrawingIds = new List<int>();

            try
            {
                vizcore3d.BeginUpdate();

                // 기존 측정 항목 및 보조선 제거
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // 카메라 방향 설정 (줌은 호출하는 쪽에서 담당)
                if (viewDirection != null)
                {
                    switch (viewDirection)
                    {
                        case "X": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.X_PLUS); break;
                        case "Y": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Y_PLUS); break;
                        case "Z": vizcore3d.View.MoveCamera(VIZCore3D.NET.Data.CameraDirection.Z_PLUS); break;
                    }
                }

                // T-028: 단일 경로 스마트 필터링 (isInstallationMode / useDirectChain 분기 제거)
                List<ChainDimensionData> filteredDims =
                    ApplySmartFiltering(displayList, maxDimensionsPerAxis: 8, minTextSpace: 25.0f);

                if (filteredDims.Count == 0)
                {
                    vizcore3d.EndUpdate();
                    return new List<int>();
                }

                // 측정 스타일 설정 (가독성 향상)
                VIZCore3D.NET.Data.MeasureStyle measureStyle = vizcore3d.Review.Measure.GetStyle();
                measureStyle.Prefix = false;
                measureStyle.Unit = false;
                measureStyle.NumberOfDecimalPlaces = 0;
                measureStyle.DX_DY_DZ = false;
                measureStyle.Frame = false;
                measureStyle.ContinuousDistance = false;
                measureStyle.BackgroundTransparent = true;
                measureStyle.FontColor = System.Drawing.Color.Blue;
                measureStyle.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE8;
                measureStyle.FontBold = true;
                measureStyle.LineColor = System.Drawing.Color.Blue;
                measureStyle.LineWidth = 1;
                measureStyle.ArrowColor = System.Drawing.Color.Blue;
                measureStyle.ArrowSize = 5;
                measureStyle.AssistantLine = false;
                measureStyle.AssistantLineStyle = VIZCore3D.NET.Data.MeasureStyle.AssistantLineType.SOLIDLINE;
                measureStyle.AlignDistanceText = true;
                measureStyle.AlignDistanceTextMargine = 3;
                vizcore3d.Review.Measure.SetStyle(measureStyle);

                // baseline 계산
                float globalMinX = float.MaxValue, globalMinY = float.MaxValue, globalMinZ = float.MaxValue;
                float globalMaxX = float.MinValue, globalMaxY = float.MinValue, globalMaxZ = float.MinValue;
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
                            globalMaxX = Math.Max(globalMaxX, bom.MaxX);
                            globalMaxY = Math.Max(globalMaxY, bom.MaxY);
                            globalMaxZ = Math.Max(globalMaxZ, bom.MaxZ);
                        }
                    }
                }
                if (globalMinX == float.MaxValue)
                {
                    foreach (var dim in filteredDims)
                    {
                        globalMinX = Math.Min(globalMinX, Math.Min(dim.StartPoint.X, dim.EndPoint.X));
                        globalMinY = Math.Min(globalMinY, Math.Min(dim.StartPoint.Y, dim.EndPoint.Y));
                        globalMinZ = Math.Min(globalMinZ, Math.Min(dim.StartPoint.Z, dim.EndPoint.Z));
                        globalMaxX = Math.Max(globalMaxX, Math.Max(dim.StartPoint.X, dim.EndPoint.X));
                        globalMaxY = Math.Max(globalMaxY, Math.Max(dim.StartPoint.Y, dim.EndPoint.Y));
                        globalMaxZ = Math.Max(globalMaxZ, Math.Max(dim.StartPoint.Z, dim.EndPoint.Z));
                    }
                }
                // 모델 중심 계산 (풍선 방향 결정용)
                float modelCenterX = (globalMinX + globalMaxX) / 2f;
                float modelCenterY = (globalMinY + globalMaxY) / 2f;
                float modelCenterZ = (globalMinZ + globalMaxZ) / 2f;

                // 오프셋 (3D/2D 동일 — 2D 변환 시 좌표가 함께 변환되므로 동일 값 사용)
                // 2026-06-03 갱신: 보조선 길이 = 캔버스 절대 1단=5mm / 2단=10mm 고정.
                //   ComputeCanvasAbsoluteOffsets(canvasBase=5, canvasLvl=5) 단일 출처 — 제작도·가공도 공용.
                //   모델좌표 변환: 캔버스 mm ÷ canvasScale → 출력물(PDF)에서 항상 동일 길이.
                //   (canvasScaleOverride ≤ 0인 3D 미리보기 경로만 모델좌표 fallback: baseOffset=100 / levelSpacing=80)
                float baseOffset, levelSpacing;

                // canvasMaxOff: 분기 밖 선언 (모델 이동량 계산이 axisPositiveOffset 결정 후 같은 값 사용)
                float canvasMaxOff = 0f;  // 2D 캔버스 mm. 1단 + 차분 = 2단

                if (canvasScaleOverride > 0f && filteredDims.Count > 0)
                {
                    // 보조선 길이 = 캔버스 절대 5/10mm. 공용 헬퍼 — 가공도와 동일 정책 (한 곳에서 관리).
                    ComputeCanvasAbsoluteOffsets(canvasScaleOverride, out baseOffset, out levelSpacing, out canvasMaxOff);

                    float maxDist = filteredDims.Max(d => d.Distance);
                    DiagLog($"보조선 헬퍼 view={viewDirection} maxDist={maxDist:F1} scale={canvasScaleOverride:F4} → baseOffset_3d={baseOffset:F2} levelSpacing_3d={levelSpacing:F2}");
                }
                else
                {
                    baseOffset = 100.0f;
                    levelSpacing = 80.0f;
                }

                // T-038+039 v4 (2026-05-12): 모델 이동량 사전 초기화. axisPositiveOffset 결정 후(아래) 계산.
                _lastModelShiftCanvasX = 0f;
                _lastModelShiftCanvasY = 0f;

                List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                // ========== 축별 체인치수 방향 결정 (T-005: 중앙에서 가장 먼 Osnap 쪽이 외곽) ==========
                Dictionary<string, bool> axisPositiveOffset = new Dictionary<string, bool>();
                if (viewDirection != null)
                {
                    var axisGroups = filteredDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis);
                    foreach (var grp in axisGroups)
                    {
                        string dimAxis = grp.Key;
                        string offsetAxis = GetRemainingAxis(viewDirection, dimAxis);

                        float modelCenterOffset = 0;
                        switch (offsetAxis)
                        {
                            case "X": modelCenterOffset = modelCenterX; break;
                            case "Y": modelCenterOffset = modelCenterY; break;
                            case "Z": modelCenterOffset = modelCenterZ; break;
                        }

                        var values = grp.SelectMany(d => new[]
                        {
                            GetAxisValue(d.StartPoint, offsetAxis),
                            GetAxisValue(d.EndPoint, offsetAxis)
                        });
                        axisPositiveOffset[dimAxis] = ComputePositiveOffsetByOsnapExtreme(values, modelCenterOffset);
                    }
                }

                // T-038+039 v4 (2026-05-12 사용자 사양): 보조선이 나간 방향 *반대*로 모델 이동
                // "보조선이 나간 방향 반대쪽으로 그리드 안의 모델을 보조선 길이만큼 이동"
                // axisPositiveOffset + canvasMaxOff 사용해 화면 H/V 외곽 방향·거리 계산
                if (canvasScaleOverride > 0f && viewDirection != null && canvasMaxOff > 0f)
                {
                    string hAxis_3d, vAxis_3d;
                    switch (viewDirection)
                    {
                        case "Z": hAxis_3d = "X"; vAxis_3d = "Y"; break;  // 화면 H=X, V=Y
                        case "X": hAxis_3d = "Y"; vAxis_3d = "Z"; break;
                        case "Y": hAxis_3d = "X"; vAxis_3d = "Z"; break;
                        default:  hAxis_3d = "X"; vAxis_3d = "Y"; break;
                    }

                    // 화면 H 방향 외곽: vAxis_3d dim → 보조선 hAxis_3d → 화면 H
                    // 화면 V 방향 외곽: hAxis_3d dim → 보조선 vAxis_3d → 화면 V
                    float canvasHOff = 0f, canvasVOff = 0f;
                    bool hPositive = false, vPositive = false;

                    if (axisPositiveOffset.ContainsKey(vAxis_3d))
                    {
                        hPositive = axisPositiveOffset[vAxis_3d];
                        canvasHOff = canvasMaxOff;
                    }
                    if (axisPositiveOffset.ContainsKey(hAxis_3d))
                    {
                        vPositive = axisPositiveOffset[hAxis_3d];
                        canvasVOff = canvasMaxOff;
                    }

                    // T-038+039 v8 (2026-05-12 사용자 사양 — 뷰별 차등 공식):
                    //   vPositive=true (외곽 위, 모델 아래 이동, 라벨 안전): 0.25 — 모든 뷰
                    //   vPositive=false (외곽 아래 = 치수 아래, 모델 위 이동):
                    //     - Z뷰(평면도): 0.5 (사용자 확인 OK)
                    //     - X뷰/Y뷰: 0.75 (더 위로 — 사용자 사양 "라벨 가림 해소 추가 보강")
                    // Y뷰 dx 부호 반전 (v6 유지)
                    float vShiftScale;
                    if (vPositive)
                        vShiftScale = 0.25f;
                    else
                        vShiftScale = (viewDirection == "Z") ? 0.5f : 0.75f;
                    const float hShiftScale = 0.25f;
                    float hSign = (viewDirection == "Y") ? -1f : 1f;
                    _lastModelShiftCanvasX = (hPositive ? -canvasHOff : canvasHOff) * hShiftScale * hSign;
                    _lastModelShiftCanvasY = (vPositive ? -canvasVOff : canvasVOff) * vShiftScale;
                    DiagLog($"T-038+039 v8 ModelShift view={viewDirection} hAxis={hAxis_3d} vAxis={vAxis_3d} hPositive={hPositive} vPositive={vPositive} canvasH={canvasHOff:F1} canvasV={canvasVOff:F1} hShiftScale={hShiftScale} vShiftScale={vShiftScale} hSign={hSign} → shiftXY=({_lastModelShiftCanvasX:F1}, {_lastModelShiftCanvasY:F1})");
                }

                // ========== Level-Based Layout ==========
                var level0Dims = filteredDims.Where(d => d.IsTotal && d.IsVisible).ToList();
                var level1Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0).ToList();
                var level2Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0).ToList();

                // T-028: 오프셋 단일화 (isInstallationMode 분기 제거) — baseOffset 기반 적응형
                float level1Offset = baseOffset;
                float level2Offset = baseOffset + levelSpacing;

                // T-040 (2026-05-13): AlignDistanceTextPosition 토글 폐기 — SetMeasureItemDistanceTextPos로 대체
                // (텍스트 위치 시프트는 RenderSheetViewForDrawing에서 2D 변환 직전에 수행)

                // Level 1 치수 (가장 안쪽 - Osnap 간 체인치수)
                // 2026-05-11: T-040v i%2 토글 취소 (사용자 결정 — "2줄만 생성: 연쇄치수 + 전체치수")
                foreach (var dim in level1Dims)
                {
                    bool posOff = axisPositiveOffset.ContainsKey(dim.Axis) && axisPositiveOffset[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        level1Offset, globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines,
                        globalMaxX, globalMaxY, globalMaxZ, posOff);
                }

                // Level 2 치수 (중간)
                foreach (var dim in level2Dims)
                {
                    bool posOff = axisPositiveOffset.ContainsKey(dim.Axis) && axisPositiveOffset[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        level2Offset, globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines,
                        globalMaxX, globalMaxY, globalMaxZ, posOff);
                }

                // Level 0 전체 치수 (가장 바깥 - 전체 길이)
                int maxLevelUsed = level2Dims.Count > 0 ? 2 : 1;
                float level0Offset = baseOffset + (levelSpacing * maxLevelUsed);
                foreach (var dim in level0Dims)
                {
                    bool posOff = axisPositiveOffset.ContainsKey(dim.Axis) && axisPositiveOffset[dim.Axis];
                    DrawDimension(dim.StartPoint, dim.EndPoint, dim.Axis,
                        level0Offset, globalMinX, globalMinY, globalMinZ,
                        viewDirection, extensionLines,
                        globalMaxX, globalMaxY, globalMaxZ, posOff);
                }

                // 보조선 그리기 — ShapeDrawing ID 수집
                if (extensionLines.Count > 0)
                {
                    if (forDrawing2D)
                    {
                        // 2D 모드: 검은색 가는 선 + ID 수집 (Add2DObjectFromShapeDrawing용)
                        int shapeId = vizcore3d.ShapeDrawing.AddLine(extensionLines, -1, System.Drawing.Color.Black, 0.15f, true);
                        shapeDrawingIds.Add(shapeId);
                    }
                    else
                    {
                        // 3D 모드: 연한 파란색
                        vizcore3d.ShapeDrawing.AddLine(extensionLines, -1, System.Drawing.Color.FromArgb(120, 120, 200), 0.5f, true);
                    }
                }

                // ========== 풍선 통합 배치 (겹침 방지: 동일 기점 5° 회전 + 보조선 연장) ==========
                float dimBaseline_OuterOffset = baseOffset + (levelSpacing * maxLevelUsed);
                // 모델 크기에 비례하여 풍선 오프셋 결정 (최소 100mm, 모델 대각 크기의 10%)
                // (풍선 배치를 부재 옆 방식으로 변경 - 모델 외곽 배치 파라미터 제거됨)

                // 뷰 방향별 축 매핑 (hAxis=수평, vAxis=수직, dAxis=깊이)
                int bHAxis, bVAxis, bDAxis;
                switch (viewDirection)
                {
                    case "X": bHAxis = 1; bVAxis = 2; bDAxis = 0; break; // H=Y, V=Z, D=X
                    case "Y": bHAxis = 0; bVAxis = 2; bDAxis = 1; break; // H=X, V=Z, D=Y
                    case "Z": bHAxis = 0; bVAxis = 1; bDAxis = 2; break; // H=X, V=Y, D=Z
                    default:  bHAxis = 1; bVAxis = 2; bDAxis = 0; break;
                }

                // 치수선 마지막 기준선 (수평축 방향)
                float[] globalMinArr = { globalMinX, globalMinY, globalMinZ };
                float dimBaselineH = globalMinArr[bHAxis] - dimBaseline_OuterOffset;

                // 풍선 항목 수집 (기점 좌표, 텍스트, 색상)
                List<(float ox, float oy, float oz, string text, Color color)> balloonEntries =
                    new List<(float, float, float, string, Color)>();

                // 선택된 노드 집합 (EBOS/CIRCLE/Hole/SlotHole 공통 필터링용)
                HashSet<int> xraySelectedSet = (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                    ? new HashSet<int>(xraySelectedNodeIndices) : null;

                // --- EBOS 풍선 수집 (선택된 시트 부재만) ---
                try
                {
                    string purposeKey = null;
                    var udaKeys = vizcore3d.Object3D.UDA.Keys;
                    if (udaKeys != null)
                    {
                        foreach (string k in udaKeys)
                        {
                            if (k.Trim().ToUpper() == "PURPOSE") { purposeKey = k; break; }
                        }
                    }
                    if (purposeKey != null)
                    {
                        var allNodes = vizcore3d.Object3D.GetPartialNode(true, true, true);
                        if (allNodes != null)
                        {
                            foreach (var node in allNodes)
                            {
                                try
                                {
                                    // 선택된 시트 부재만 필터링
                                    if (xraySelectedSet != null && !xraySelectedSet.Contains(node.Index)) continue;

                                    var val = vizcore3d.Object3D.UDA.FromIndex(node.Index, purposeKey);
                                    if (val == null || val.ToString().Trim().ToUpper() != "EBOS") continue;
                                    var bboxI = new List<int> { node.Index };
                                    var bbox = vizcore3d.Object3D.GetBoundBox(bboxI, false);
                                    if (bbox == null) continue;
                                    balloonEntries.Add((
                                        (bbox.MinX + bbox.MaxX) / 2f,
                                        (bbox.MinY + bbox.MaxY) / 2f,
                                        (bbox.MinZ + bbox.MaxZ) / 2f,
                                        "EarthBoss", Color.Blue));
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                // --- 원형(CIRCLE) 풍선 수집 (선택된 시트 부재만, 홀로 매칭된 원기둥은 제외) ---
                try
                {
                    // 홀로 매칭된 원기둥 Body Index 수집
                    HashSet<int> holeCylinderIndices = new HashSet<int>();
                    foreach (var b in bomList)
                    {
                        if (b.Holes != null)
                            foreach (var h in b.Holes)
                                holeCylinderIndices.Add(h.CylinderBodyIndex);
                    }
                    foreach (var bom in bomList)
                    {
                        if (bom.CircleRadius <= 0) continue;
                        if (holeCylinderIndices.Contains(bom.Index)) continue; // 홀 원기둥은 스킵
                        // 선택된 시트 부재만 필터링
                        if (xraySelectedSet != null && !xraySelectedSet.Contains(bom.Index)) continue;

                        // 바운딩박스 형태가 원기둥이 아닌 body는 원형 풍선 제외 (Angle 등)
                        float diameter = bom.CircleRadius * 2f;
                        float sX = Math.Abs(bom.MaxX - bom.MinX);
                        float sY = Math.Abs(bom.MaxY - bom.MinY);
                        float sZ = Math.Abs(bom.MaxZ - bom.MinZ);
                        float cTol = Math.Max(2f, diameter * 0.2f);
                        int mc = 0;
                        if (Math.Abs(sX - diameter) < cTol) mc++;
                        if (Math.Abs(sY - diameter) < cTol) mc++;
                        if (Math.Abs(sZ - diameter) < cTol) mc++;
                        if (mc < 2) continue; // 원기둥 형태가 아니면 스킵

                        balloonEntries.Add((bom.CenterX, bom.CenterY, bom.CenterZ,
                            $"R{bom.CircleRadius:F1}", Color.Red));
                    }
                }
                catch { }

                // --- 홀(Hole) 풍선 수집 (BOM 부재별 같은 직경 그룹핑, 선택 노드만) ---
                try
                {
                    foreach (var bom in bomList)
                    {
                        if (bom.Holes == null || bom.Holes.Count == 0) continue;
                        // 선택된 시트 부재만 필터링
                        if (xraySelectedSet != null && !xraySelectedSet.Contains(bom.Index)) continue;
                        // BOM별로 같은 직경 홀 그룹핑
                        var holeGroups = bom.Holes.GroupBy(h => Math.Round(h.Diameter, 1));
                        foreach (var grp in holeGroups)
                        {
                            int count = grp.Count();
                            string holeText = count > 1
                                ? $"\u00d8{grp.Key:F1} * {count}개"
                                : $"\u00d8{grp.Key:F1}";
                            // 대표 홀(첫 번째)에만 풍선 표시
                            var firstHole = grp.First();
                            balloonEntries.Add((firstHole.CenterX, firstHole.CenterY, firstHole.CenterZ,
                                holeText, Color.FromArgb(0, 160, 0)));
                        }
                    }
                }
                catch { }

                // --- 슬롯홀(SlotHole) 풍선 수집 (같은 사이즈 그룹핑, 1풍선/사이즈) ---
                try
                {
                    foreach (var bom in bomList)
                    {
                        if (bom.SlotHoles == null || bom.SlotHoles.Count == 0) continue;
                        if (xraySelectedSet != null && !xraySelectedSet.Contains(bom.Index)) continue;

                        // 같은 사이즈(반지름+길이+깊이) 슬롯홀 그룹핑
                        var slotGroups = bom.SlotHoles.GroupBy(s =>
                            $"{Math.Round(s.Radius, 1)}_{Math.Round(s.SlotLength, 0)}_{Math.Round(s.Depth, 0)}");
                        foreach (var grp in slotGroups)
                        {
                            var first = grp.First();
                            int count = grp.Count();
                            float slotWidth = first.Radius * 2f;
                            string slotText = count > 1
                                ? $"R{first.Radius:F1}/({slotWidth:F0}*{first.SlotLength:F0}*{first.Depth:F0}) * {count}개"
                                : $"R{first.Radius:F1}/({slotWidth:F0}*{first.SlotLength:F0}*{first.Depth:F0})";
                            balloonEntries.Add((first.CenterX, first.CenterY, first.CenterZ,
                                slotText, Color.FromArgb(180, 0, 180)));
                        }
                    }
                }
                catch { }

                // --- 풍선 일괄 배치 (가상 사각형 경계선 방식: 4분면, 체인치수 겹침 방지) ---
                try
                {
                    Func<float, float, float, int, float> getComp = (x, y, z, axis) =>
                        axis == 0 ? x : (axis == 1 ? y : z);

                    // 모델 전체 바운딩박스의 2D 경계
                    float[] globalMinArr2 = { globalMinX, globalMinY, globalMinZ };
                    float[] globalMaxArr2 = { globalMaxX, globalMaxY, globalMaxZ };
                    float modelMinH = globalMinArr2[bHAxis];
                    float modelMaxH = globalMaxArr2[bHAxis];
                    float modelMinV = globalMinArr2[bVAxis];
                    float modelMaxV = globalMaxArr2[bVAxis];

                    // ── 체인치수 실제 끝단 좌표 계산 (H/V 축 각 방향별 Max) ──
                    // 각 치수선은 baseline(모델 끝단) ± offset 위치에 그려지고,
                    // 치수선의 수평/수직 범위는 StartPoint~EndPoint의 해당 축 값
                    float dimExtMinH = modelMinH;  // 치수선이 차지하는 H축 최소
                    float dimExtMaxH = modelMaxH;  // 치수선이 차지하는 H축 최대
                    float dimExtMinV = modelMinV;  // 치수선이 차지하는 V축 최소
                    float dimExtMaxV = modelMaxV;  // 치수선이 차지하는 V축 최대

                    // 모든 치수 데이터(level0/1/2)를 순회하여 실제 끝단 좌표 추적
                    var allVisibleDims = filteredDims.Where(d => d.IsVisible).ToList();
                    foreach (var dim in allVisibleDims)
                    {
                        // 치수선의 수평(H축) 범위: 치수축 방향의 StartPoint~EndPoint
                        float dimStartH = 0, dimEndH = 0, dimStartV = 0, dimEndV = 0;
                        switch (bHAxis)
                        {
                            case 0: dimStartH = dim.StartPoint.X; dimEndH = dim.EndPoint.X; break;
                            case 1: dimStartH = dim.StartPoint.Y; dimEndH = dim.EndPoint.Y; break;
                            case 2: dimStartH = dim.StartPoint.Z; dimEndH = dim.EndPoint.Z; break;
                        }
                        switch (bVAxis)
                        {
                            case 0: dimStartV = dim.StartPoint.X; dimEndV = dim.EndPoint.X; break;
                            case 1: dimStartV = dim.StartPoint.Y; dimEndV = dim.EndPoint.Y; break;
                            case 2: dimStartV = dim.StartPoint.Z; dimEndV = dim.EndPoint.Z; break;
                        }

                        // 치수선이 오프셋축(수직 방향)으로 뻗은 위치 계산
                        string offsetAxis = GetRemainingAxis(viewDirection, dim.Axis);
                        bool posOff = axisPositiveOffset.ContainsKey(dim.Axis) && axisPositiveOffset[dim.Axis];

                        float dimOffset;
                        if (dim.IsTotal)
                            dimOffset = level0Offset;
                        else if (dim.DisplayLevel > 0)
                            dimOffset = level2Offset;
                        else
                            dimOffset = level1Offset;

                        // baseline(모델 끝단) ± offset = 치수선 위치
                        float baseline = 0;
                        switch (offsetAxis)
                        {
                            case "X": baseline = posOff ? globalMaxX : globalMinX; break;
                            case "Y": baseline = posOff ? globalMaxY : globalMinY; break;
                            case "Z": baseline = posOff ? globalMaxZ : globalMinZ; break;
                        }
                        float dimLinePos = posOff ? (baseline + dimOffset) : (baseline - dimOffset);

                        // 오프셋축이 H축인 경우 → 치수선이 수평 방향으로 뻗음
                        int offsetAxisIdx = offsetAxis == "X" ? 0 : (offsetAxis == "Y" ? 1 : 2);
                        if (offsetAxisIdx == bHAxis)
                        {
                            dimExtMinH = Math.Min(dimExtMinH, dimLinePos);
                            dimExtMaxH = Math.Max(dimExtMaxH, dimLinePos);
                        }
                        // 오프셋축이 V축인 경우 → 치수선이 수직 방향으로 뻗음
                        else if (offsetAxisIdx == bVAxis)
                        {
                            dimExtMinV = Math.Min(dimExtMinV, dimLinePos);
                            dimExtMaxV = Math.Max(dimExtMaxV, dimLinePos);
                        }

                        // 치수선 자체의 H/V 범위도 갱신
                        dimExtMinH = Math.Min(dimExtMinH, Math.Min(dimStartH, dimEndH));
                        dimExtMaxH = Math.Max(dimExtMaxH, Math.Max(dimStartH, dimEndH));
                        dimExtMinV = Math.Min(dimExtMinV, Math.Min(dimStartV, dimEndV));
                        dimExtMaxV = Math.Max(dimExtMaxV, Math.Max(dimStartV, dimEndV));
                    }

                    // ── 가상 사각형 경계선: 체인치수 끝단 바깥에 풍선 배치 영역 ──
                    float dimMargin = 30f; // 체인치수 끝단에서 추가 여유
                    float rectLeft  = dimExtMinH - dimMargin;   // 가상선 왼쪽
                    float rectRight = dimExtMaxH + dimMargin;   // 가상선 오른쪽

                    // 풍선 간 간격 (가상선 위에서 일정 간격으로 배치)
                    float modelSpan = Math.Max(modelMaxH - modelMinH, modelMaxV - modelMinV);
                    float balloonSpacing = Math.Max(20f, modelSpan * 0.04f);

                    // 텍스트 크기 추정 (SIZE8 기준, 2D 변환 시 TextHeight로 축소)
                    float textGap = Math.Max(4f, modelSpan * 0.006f);
                    Func<string, (float w, float h)> estimateTextSize = (text) =>
                    {
                        float charWidth = Math.Max(3f, modelSpan * 0.005f);
                        float lineHeight = Math.Max(7f, modelSpan * 0.009f);
                        return (text.Length * charWidth + textGap, lineHeight + textGap);
                    };

                    // ── 풍선 방향 분류: 시작점 기준 4분면 (왼쪽위/왼쪽아래/오른쪽위/오른쪽아래) ──
                    float modelCenterH2 = (modelMinH + modelMaxH) / 2f;
                    float modelCenterV2 = (modelMinV + modelMaxV) / 2f;

                    // 0=왼쪽위, 1=왼쪽아래, 2=오른쪽위, 3=오른쪽아래
                    List<(int quadrant, float originH, float originV, float originD, string text, Color color, float sortKey)>
                        sortedBalloons = new List<(int, float, float, float, string, Color, float)>();

                    // 형상 풍선은 가공도 전용이다.
                    // ISO 부재번호 풍선은 별도 CreateIsoBalloonNotes에서 처리한다.
                    // 일반/선택 X/Y/Z 뷰와 2D 제작도에서는 생성하지 않는다.
                    balloonEntries.Clear();

                    foreach (var entry in balloonEntries)
                    {
                        float oH = getComp(entry.ox, entry.oy, entry.oz, bHAxis);
                        float oV = getComp(entry.ox, entry.oy, entry.oz, bVAxis);
                        float oD = getComp(entry.ox, entry.oy, entry.oz, bDAxis);

                        // 시작점이 모델 중심 기준 4분면 판별
                        bool isLeft = oH <= modelCenterH2;
                        bool isTop  = oV >= modelCenterV2;

                        int quadrant;
                        float sortKey;
                        if (isLeft && isTop)
                        {
                            quadrant = 0; // 왼쪽위: 위→아래 순서
                            sortKey = -oV;
                        }
                        else if (isLeft && !isTop)
                        {
                            quadrant = 1; // 왼쪽아래: 아래→위 순서
                            sortKey = oV;
                        }
                        else if (!isLeft && isTop)
                        {
                            quadrant = 2; // 오른쪽위: 위→아래 순서
                            sortKey = -oV;
                        }
                        else
                        {
                            quadrant = 3; // 오른쪽아래: 아래→위 순서
                            sortKey = oV;
                        }
                        sortedBalloons.Add((quadrant, oH, oV, oD, entry.text, entry.color, sortKey));
                    }

                    // 왼쪽위→왼쪽아래→오른쪽위→오른쪽아래 순서, 각 분면 내에서 sortKey 순서
                    sortedBalloons.Sort((a, b) =>
                    {
                        int sc = a.quadrant.CompareTo(b.quadrant);
                        return sc != 0 ? sc : a.sortKey.CompareTo(b.sortKey);
                    });

                    // ── 각 분면별 배치 위치 추적 (체인치수 끝단 바깥에서 시작) ──
                    float leftTopNextV    = dimExtMaxV;     // 왼쪽위: 치수 최대V에서 아래로
                    float leftBotNextV    = dimExtMinV;     // 왼쪽아래: 치수 최소V에서 위로
                    float rightTopNextV   = dimExtMaxV;     // 오른쪽위: 치수 최대V에서 아래로
                    float rightBotNextV   = dimExtMinV;     // 오른쪽아래: 치수 최소V에서 위로

                    foreach (var balloon in sortedBalloons)
                    {
                        try
                        {
                            var candSize = estimateTextSize(balloon.text);
                            float textW = candSize.w;
                            float textH = candSize.h;

                            float textPosH, textPosV;

                            switch (balloon.quadrant)
                            {
                                case 0: // 왼쪽위 가상선
                                    textPosH = rectLeft;
                                    textPosV = leftTopNextV;
                                    leftTopNextV -= (textH + balloonSpacing);
                                    break;
                                case 1: // 왼쪽아래 가상선
                                    textPosH = rectLeft;
                                    textPosV = leftBotNextV;
                                    leftBotNextV += (textH + balloonSpacing);
                                    break;
                                case 2: // 오른쪽위 가상선
                                    textPosH = rectRight;
                                    textPosV = rightTopNextV;
                                    rightTopNextV -= (textH + balloonSpacing);
                                    break;
                                case 3: // 오른쪽아래 가상선
                                    textPosH = rectRight;
                                    textPosV = rightBotNextV;
                                    rightBotNextV += (textH + balloonSpacing);
                                    break;
                                default:
                                    textPosH = rectRight;
                                    textPosV = balloon.originV;
                                    break;
                            }

                            // 3D 좌표로 복원
                            float[] textXYZ = new float[3];
                            textXYZ[bHAxis] = textPosH;
                            textXYZ[bVAxis] = textPosV;
                            textXYZ[bDAxis] = balloon.originD;

                            // 보조선 시작점 (기점 → 3D 좌표 복원)
                            float[] arrowXYZ = new float[3];
                            arrowXYZ[bHAxis] = balloon.originH;
                            arrowXYZ[bVAxis] = balloon.originV;
                            arrowXYZ[bDAxis] = balloon.originD;
                            VIZCore3D.NET.Data.Vertex3D arrowPos = new VIZCore3D.NET.Data.Vertex3D(
                                arrowXYZ[0], arrowXYZ[1], arrowXYZ[2]);

                            VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(
                                textXYZ[0], textXYZ[1], textXYZ[2]);

                            VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                            style.UseSymbol = false;
                            style.BackgroudTransparent = true;
                            style.UseTextBox = false;
                            style.FontBold = true;
                            style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                            style.FontColor = balloon.color;
                            style.LineColor = balloon.color;
                            style.LineWidth = 1;
                            style.ArrowColor = balloon.color;
                            style.ArrowWidth = 2;

                            vizcore3d.Review.Note.AddNoteSurface(balloon.text, textPos, arrowPos, style);
                        }
                        catch { }
                    }
                }
                catch (Exception balloonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"풍선 배치 오류: {balloonEx.Message}");
                }

                vizcore3d.EndUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"치수 표시 오류: {ex.Message}");
            }

            return shapeDrawingIds;
        }

        /// <summary>
        /// 단일 치수 그리기 헬퍼 메서드
        /// </summary>
        private int DrawDimension(
            VIZCore3D.NET.Data.Vector3D startPoint,
            VIZCore3D.NET.Data.Vector3D endPoint,
            string axis,
            float offset,
            float globalMinX,
            float globalMinY,
            float globalMinZ,
            string viewDirection,
            List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines,
            float globalMaxX = 0,
            float globalMaxY = 0,
            float globalMaxZ = 0,
            bool positiveOffset = false,
            bool alignExtToBaseline = false)
        {
            // 원본 좌표
            VIZCore3D.NET.Data.Vertex3D originalStart = new VIZCore3D.NET.Data.Vertex3D(
                startPoint.X, startPoint.Y, startPoint.Z);
            VIZCore3D.NET.Data.Vertex3D originalEnd = new VIZCore3D.NET.Data.Vertex3D(
                endPoint.X, endPoint.Y, endPoint.Z);

            // 오프셋 방향 및 baseline 결정
            string offsetDir = "";
            float baseline = 0;

            if (viewDirection == "X" || viewDirection == null)
            {
                switch (axis)
                {
                    case "Z": offsetDir = "Y"; baseline = positiveOffset ? globalMaxY : globalMinY; break;
                    case "Y": offsetDir = "Z"; baseline = positiveOffset ? globalMaxZ : globalMinZ; break;
                    case "X": offsetDir = "Y"; baseline = positiveOffset ? globalMaxY : globalMinY; break;
                }
            }
            else if (viewDirection == "Y")
            {
                switch (axis)
                {
                    case "Z": offsetDir = "X"; baseline = positiveOffset ? globalMaxX : globalMinX; break;
                    case "X": offsetDir = "Z"; baseline = positiveOffset ? globalMaxZ : globalMinZ; break;
                }
            }
            else if (viewDirection == "Z")
            {
                switch (axis)
                {
                    case "Y": offsetDir = "X"; baseline = positiveOffset ? globalMaxX : globalMinX; break;
                    case "X": offsetDir = "Y"; baseline = positiveOffset ? globalMaxY : globalMinY; break;
                }
            }

            // baseline에서 오프셋 방향으로 치수 위치 계산 (중심에서 체인치수 방향)
            float offsetValue = positiveOffset ? (baseline + offset) : (baseline - offset);
            VIZCore3D.NET.Data.Vertex3D startVertex;
            VIZCore3D.NET.Data.Vertex3D endVertex;

            switch (offsetDir)
            {
                case "X":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(offsetValue, startPoint.Y, startPoint.Z);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(offsetValue, endPoint.Y, endPoint.Z);
                    break;
                case "Y":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(startPoint.X, offsetValue, startPoint.Z);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(endPoint.X, offsetValue, endPoint.Z);
                    break;
                case "Z":
                    startVertex = new VIZCore3D.NET.Data.Vertex3D(startPoint.X, startPoint.Y, offsetValue);
                    endVertex = new VIZCore3D.NET.Data.Vertex3D(endPoint.X, endPoint.Y, offsetValue);
                    break;
                default:
                    return -1;
            }

            // 치수 거리 계산
            float distance = 0;
            switch (axis)
            {
                case "X": distance = Math.Abs(endPoint.X - startPoint.X); break;
                case "Y": distance = Math.Abs(endPoint.Y - startPoint.Y); break;
                case "Z": distance = Math.Abs(endPoint.Z - startPoint.Z); break;
            }

            // P3 #3 진단 (2026-05-23): measure 좌표가 부재 BBox와 어떻게 매칭되는지 추적
            DiagLog($"[DrawDimension] axis={axis} dist={distance:F2} view={viewDirection} " +
                $"start=({startPoint.X:F2},{startPoint.Y:F2},{startPoint.Z:F2}) " +
                $"end=({endPoint.X:F2},{endPoint.Y:F2},{endPoint.Z:F2}) " +
                $"sv=({startVertex.X:F2},{startVertex.Y:F2},{startVertex.Z:F2}) " +
                $"ev=({endVertex.X:F2},{endVertex.Y:F2},{endVertex.Z:F2})");

            // T-040 v8: AddCustomAxisDistance 반환값(측정 ID) 받아 호출자에 전달
            int measureId = -1;
            if (distance > 0.1f)
            {
                switch (axis)
                {
                    case "X":
                        measureId = vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.X, startVertex, endVertex);
                        break;
                    case "Y":
                        measureId = vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Y, startVertex, endVertex);
                        break;
                    case "Z":
                        measureId = vizcore3d.Review.Measure.AddCustomAxisDistance(VIZCore3D.NET.Data.Axis.Z, startVertex, endVertex);
                        break;
                }
            }

            // 보조선 추가 (Osnap 위치 → 치수선 위치)
            // T-046: 모델 표면에서 ExtensionLineGap(10mm)만큼 떨어져 시작 (시각적 가독성)
            //        Osnap 좌표 → 치수선 방향 단위벡터 × gap만큼 이동 → 치수선까지 직선
            // 2026-06-23: gap을 보조선 길이의 절반 이하로 제한 — 오프셋이 짧으면(가공도 보조선 축소)
            //   고정 10mm gap이 보조선을 통째로 먹어 0으로 접혀 '아래쪽 보조선 누락'이 생기던 것 방지.
            // 가공도 전용(alignExtToBaseline): 보조선을 osnap 점이 아니라 모델 가장자리(baseline)에서 시작.
            //   → 모든 보조선 길이 = 오프셋 거리로 통일, 반대쪽 점이 부재를 가로지르는 문제 제거.
            //   (제작도는 false → 기존 osnap 점에서 시작, 무영향)
            VIZCore3D.NET.Data.Vertex3D extStart = originalStart, extEnd = originalEnd;
            if (alignExtToBaseline)
            {
                switch (offsetDir)
                {
                    case "X":
                        extStart = new VIZCore3D.NET.Data.Vertex3D(baseline, originalStart.Y, originalStart.Z);
                        extEnd = new VIZCore3D.NET.Data.Vertex3D(baseline, originalEnd.Y, originalEnd.Z);
                        break;
                    case "Y":
                        extStart = new VIZCore3D.NET.Data.Vertex3D(originalStart.X, baseline, originalStart.Z);
                        extEnd = new VIZCore3D.NET.Data.Vertex3D(originalEnd.X, baseline, originalEnd.Z);
                        break;
                    case "Z":
                        extStart = new VIZCore3D.NET.Data.Vertex3D(originalStart.X, originalStart.Y, baseline);
                        extEnd = new VIZCore3D.NET.Data.Vertex3D(originalEnd.X, originalEnd.Y, baseline);
                        break;
                }
            }

            float e1x = extStart.X - startVertex.X, e1y = extStart.Y - startVertex.Y, e1z = extStart.Z - startVertex.Z;
            float extLen1 = (float)Math.Sqrt(e1x * e1x + e1y * e1y + e1z * e1z);
            float gap1 = Math.Min(ExtensionLineGap, extLen1 * 0.5f);
            var extLine1 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine1.Add(OffsetTowardLineEnd(extStart, startVertex, gap1));
            extLine1.Add(startVertex);
            extensionLines.Add(extLine1);

            float e2x = extEnd.X - endVertex.X, e2y = extEnd.Y - endVertex.Y, e2z = extEnd.Z - endVertex.Z;
            float extLen2 = (float)Math.Sqrt(e2x * e2x + e2y * e2y + e2z * e2z);
            float gap2 = Math.Min(ExtensionLineGap, extLen2 * 0.5f);
            var extLine2 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine2.Add(OffsetTowardLineEnd(extEnd, endVertex, gap2));
            extLine2.Add(endVertex);
            extensionLines.Add(extLine2);

            return measureId;
        }

        /// <summary>
        /// T-046 보조선 gap (모델 좌표 mm). 모델 표면에서 보조선이 떨어져 시작하는 거리.
        /// 가공도 보조선 오프셋(100~300mm)에 비례해 시각적으로 명확히 보이는 10mm로 설정.
        /// 작은 부재라도 헬퍼의 안전장치(`distance >= len ? to`)로 역전 방지.
        /// </summary>
        private const float ExtensionLineGap = 10.0f;

        /// <summary>
        /// from 점에서 to 점 방향으로 distance만큼 이동한 점을 반환.
        /// distance가 |from-to|보다 크거나 같으면 to를 반환(보조선 역전 방지).
        /// T-046 보조선 gap 적용 — DrawDimension의 보조선 시작점 계산에 사용.
        /// </summary>
        private VIZCore3D.NET.Data.Vertex3D OffsetTowardLineEnd(
            VIZCore3D.NET.Data.Vertex3D from,
            VIZCore3D.NET.Data.Vertex3D to,
            float distance)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float dz = to.Z - from.Z;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-3f || distance >= len)
                return new VIZCore3D.NET.Data.Vertex3D(to.X, to.Y, to.Z);
            float ratio = distance / len;
            return new VIZCore3D.NET.Data.Vertex3D(
                from.X + dx * ratio,
                from.Y + dy * ratio,
                from.Z + dz * ratio);
        }

        /// <summary>
        /// 제작에 필요한 최소 치수만 선택 (중복 제거, 필수 치수만 유지)
        /// </summary>
        #region Smart Dimension Filtering Algorithm (스마트 치수 필터링 알고리즘)

        /// <summary>
        /// 치수 우선순위 계산 및 할당
        /// Priority-Based Filtering Algorithm 적용
        /// </summary>
        private void AssignDimensionPriorities(List<ChainDimensionData> dimensions)
        {
            if (dimensions == null || dimensions.Count == 0) return;

            // 축별로 그룹화하여 우선순위 계산
            var groupedByAxis = dimensions.GroupBy(d => d.Axis);

            foreach (var axisGroup in groupedByAxis)
            {
                var axisDims = axisGroup.ToList();
                if (axisDims.Count == 0) continue;

                // 거리값 통계 계산
                float maxDistance = axisDims.Max(d => d.Distance);
                float minDistance = axisDims.Min(d => d.Distance);
                float avgDistance = axisDims.Average(d => d.Distance);
                float range = maxDistance - minDistance;

                foreach (var dim in axisDims)
                {
                    if (dim.IsTotal)
                    {
                        // 전체 길이: 최고 우선순위
                        dim.Priority = 10;
                    }
                    else if (range > 0)
                    {
                        // 상대적 크기에 따른 우선순위 (정규화)
                        float normalizedSize = (dim.Distance - minDistance) / range;

                        if (normalizedSize >= 0.7f)
                        {
                            // 상위 30%: 주요 구간
                            dim.Priority = 8;
                        }
                        else if (normalizedSize >= 0.4f)
                        {
                            // 중간 30%: 중간 구간
                            dim.Priority = 5;
                        }
                        else if (normalizedSize >= 0.15f)
                        {
                            // 하위 25%: 작은 구간
                            dim.Priority = 3;
                        }
                        else
                        {
                            // 최하위 15%: 매우 작은 구간
                            dim.Priority = 1;
                        }
                    }
                    else
                    {
                        // 모든 치수가 같은 크기
                        dim.Priority = 5;
                    }
                }
            }
        }

        /// <summary>
        /// 스마트 치수 필터링: 겹침 방지 및 가독성 향상
        /// Greedy Label Placement Algorithm 기반
        /// </summary>
        /// <param name="dimensions">전체 치수 목록</param>
        /// <param name="maxDimensionsPerAxis">축당 최대 표시 치수 개수</param>
        /// <param name="minTextSpace">치수 텍스트 간 최소 간격 (mm)</param>
        /// <returns>필터링된 치수 목록</returns>
        private List<ChainDimensionData> ApplySmartFiltering(
            List<ChainDimensionData> dimensions,
            int maxDimensionsPerAxis = 6,
            float minTextSpace = 25.0f)
        {
            if (dimensions == null || dimensions.Count == 0)
                return new List<ChainDimensionData>();

            // 우선순위 할당
            AssignDimensionPriorities(dimensions);

            var result = new List<ChainDimensionData>();
            var groupedByAxis = dimensions.GroupBy(d => d.Axis);

            foreach (var axisGroup in groupedByAxis)
            {
                var axisDims = axisGroup.ToList();
                var selectedDims = new List<ChainDimensionData>();

                // 1단계: 전체 치수(IsTotal)는 무조건 포함
                var totalDims = axisDims.Where(d => d.IsTotal).ToList();
                selectedDims.AddRange(totalDims);

                // 2단계: 나머지 치수를 우선순위 순으로 정렬
                var sequentialDims = axisDims
                    .Where(d => !d.IsTotal)
                    .OrderByDescending(d => d.Priority)
                    .ThenByDescending(d => d.Distance)
                    .ToList();

                // 3단계: 연속된 짧은 치수 병합 (Smart Grouping)
                var mergedDims = MergeShortDimensions(sequentialDims, minTextSpace);

                // 4단계: Greedy 선택 - 겹침 방지하면서 우선순위 높은 순으로 선택
                var placedPositions = new List<(float start, float end)>();
                var level1Positions = new List<(float start, float end)>();

                // 텍스트 폭 추정: 치수 자릿수 기반 동적 계산
                Func<float, float> estimateDimTextWidth = (distance) =>
                {
                    int digits = Math.Max(1, distance.ToString("F0").Length);
                    return Math.Max(minTextSpace, digits * 5f + 10f);
                };

                foreach (var dim in mergedDims.OrderByDescending(d => d.Priority).ThenByDescending(d => d.Distance))
                {
                    if (selectedDims.Count(d => !d.IsTotal) >= maxDimensionsPerAxis - 1)
                        break;

                    float dimStart = GetAxisValue(dim.StartPoint, axisGroup.Key);
                    float dimEnd = GetAxisValue(dim.EndPoint, axisGroup.Key);
                    float dimMin = Math.Min(dimStart, dimEnd);
                    float dimMax = Math.Max(dimStart, dimEnd);

                    // 텍스트 중앙 위치 계산
                    float dimCenter = (dimMin + dimMax) / 2;
                    float dimTextWidth = estimateDimTextWidth(dim.Distance);

                    // 기존 배치된 치수와 텍스트 겹침 체크 (동적 폭)
                    bool hasOverlap = false;
                    foreach (var placed in placedPositions)
                    {
                        float placedCenter = (placed.start + placed.end) / 2;
                        float placedDist = placed.end - placed.start;
                        float placedTextWidth = estimateDimTextWidth(placedDist);
                        float minGap = (dimTextWidth + placedTextWidth) / 2f;

                        if (Math.Abs(dimCenter - placedCenter) < minGap)
                        {
                            hasOverlap = true;
                            break;
                        }
                    }

                    if (!hasOverlap)
                    {
                        dim.IsVisible = true;
                        dim.DisplayLevel = 0;
                        selectedDims.Add(dim);
                        placedPositions.Add((dimMin, dimMax));
                    }
                    else
                    {
                        // 겹치면 Level 1로 배정 (Level 1 내부에서도 2차 겹침 검사)
                        if (dim.Priority >= 5 && dim.DisplayLevel < 2)
                        {
                            bool level1Overlap = false;
                            foreach (var placed in level1Positions)
                            {
                                float placedCenter = (placed.start + placed.end) / 2;
                                float placedDist = placed.end - placed.start;
                                float placedTextWidth = estimateDimTextWidth(placedDist);
                                float minGap = (dimTextWidth + placedTextWidth) / 2f;
                                if (Math.Abs(dimCenter - placedCenter) < minGap)
                                { level1Overlap = true; break; }
                            }

                            if (!level1Overlap)
                            {
                                dim.DisplayLevel = 1;
                                dim.IsVisible = true;
                                selectedDims.Add(dim);
                                level1Positions.Add((dimMin, dimMax));
                            }
                            else
                            {
                                dim.IsVisible = false;
                            }
                        }
                        else
                        {
                            dim.IsVisible = false;
                        }
                    }
                }

                result.AddRange(selectedDims);

                // T-040 진단 (2026-05-11): 축별 필터링 결과 — 실제 Level 분리 동작 검증용
                int l0 = selectedDims.Count(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0);
                int l1 = selectedDims.Count(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0);
                int t  = selectedDims.Count(d => d.IsTotal);
                int h  = axisDims.Count(d => !d.IsVisible);
                DiagLog($"ApplySmartFilter axis={axisGroup.Key} level0={l0} level1={l1} total={t} hidden={h} in={axisDims.Count}");
            }

            return result;
        }

        /// <summary>
        /// 연속된 짧은 치수들을 하나의 누적 치수로 병합
        /// </summary>
        private List<ChainDimensionData> MergeShortDimensions(List<ChainDimensionData> dimensions, float minLength)
        {
            if (dimensions == null || dimensions.Count == 0)
                return new List<ChainDimensionData>();

            var result = new List<ChainDimensionData>();
            var shortGroup = new List<ChainDimensionData>();

            // 위치 순으로 정렬
            var sortedDims = dimensions.OrderByDescending(d =>
            {
                switch (d.Axis)
                {
                    case "X": return d.StartPoint.X;
                    case "Y": return d.StartPoint.Y;
                    case "Z": return d.StartPoint.Z;
                    default: return 0f;
                }
            }).ToList();

            foreach (var dim in sortedDims)
            {
                if (dim.Distance < minLength)
                {
                    // 짧은 치수 → 그룹에 추가
                    shortGroup.Add(dim);
                }
                else
                {
                    // 긴 치수 발견 → 이전 짧은 그룹 병합 후 추가
                    if (shortGroup.Count > 1)
                    {
                        var mergedDim = CreateMergedDimension(shortGroup);
                        if (mergedDim != null)
                            result.Add(mergedDim);
                    }
                    else if (shortGroup.Count == 1)
                    {
                        // 단일 짧은 치수는 그대로 추가 (우선순위 낮춤)
                        shortGroup[0].Priority = Math.Max(1, shortGroup[0].Priority - 2);
                        result.Add(shortGroup[0]);
                    }

                    shortGroup.Clear();
                    result.Add(dim);
                }
            }

            // 마지막 그룹 처리
            if (shortGroup.Count > 1)
            {
                var mergedDim = CreateMergedDimension(shortGroup);
                if (mergedDim != null)
                    result.Add(mergedDim);
            }
            else if (shortGroup.Count == 1)
            {
                shortGroup[0].Priority = Math.Max(1, shortGroup[0].Priority - 2);
                result.Add(shortGroup[0]);
            }

            return result;
        }

        /// <summary>
        /// 여러 짧은 치수를 하나의 병합 치수로 생성
        /// </summary>
        private ChainDimensionData CreateMergedDimension(List<ChainDimensionData> shortDims)
        {
            if (shortDims == null || shortDims.Count < 2)
                return null;

            string axis = shortDims[0].Axis;

            // 시작점과 끝점 결정 (전체 범위)
            VIZCore3D.NET.Data.Vector3D startPoint = shortDims[0].StartPoint;
            VIZCore3D.NET.Data.Vector3D endPoint = shortDims[shortDims.Count - 1].EndPoint;

            // 위치 순 정렬 후 처음과 끝 선택
            switch (axis)
            {
                case "X":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.X).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.X).First().EndPoint;
                    break;
                case "Y":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.Y).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.Y).First().EndPoint;
                    break;
                case "Z":
                    startPoint = shortDims.OrderByDescending(d => d.StartPoint.Z).First().StartPoint;
                    endPoint = shortDims.OrderBy(d => d.EndPoint.Z).First().EndPoint;
                    break;
            }

            float totalDistance = 0;
            switch (axis)
            {
                case "X": totalDistance = Math.Abs(startPoint.X - endPoint.X); break;
                case "Y": totalDistance = Math.Abs(startPoint.Y - endPoint.Y); break;
                case "Z": totalDistance = Math.Abs(startPoint.Z - endPoint.Z); break;
            }

            return new ChainDimensionData
            {
                Axis = axis,
                ViewName = shortDims[0].ViewName,
                Distance = totalDistance,
                StartPoint = startPoint,
                EndPoint = endPoint,
                StartPointStr = $"({startPoint.X:F1}, {startPoint.Y:F1}, {startPoint.Z:F1})",
                EndPointStr = $"({endPoint.X:F1}, {endPoint.Y:F1}, {endPoint.Z:F1})",
                IsTotal = false,
                IsMerged = true,
                Priority = 6  // 병합 치수는 중간 높은 우선순위
            };
        }

        /// <summary>
        /// 포인트에서 축 값 추출
        /// </summary>
        private float GetAxisValue(VIZCore3D.NET.Data.Vector3D point, string axis)
        {
            switch (axis)
            {
                case "X": return point.X;
                case "Y": return point.Y;
                case "Z": return point.Z;
                default: return 0f;
            }
        }

        /// <summary>
        /// T-005 (FB-002): 모델 중앙 기준 외곽 방향 자동 판정.
        /// 사용자 사양: "모델 전체 뷰를 봤을 때 중앙을 기준으로 4분면으로 나누면, 중앙에서 가장 먼
        /// 남아있는 Osnap이 있는 방향으로 치수를 그려준다."
        /// 동작: omax/omin = Osnap 좌표의 max/min. 중앙↔omax 거리 ≥ 중앙↔omin 거리면 positive(양수).
        /// 모든 Osnap이 한쪽에만 있어도 부호 있는 차이로 자동 정렬 (한쪽 거리만 양수가 됨).
        /// </summary>
        private bool ComputePositiveOffsetByOsnapExtreme(
            IEnumerable<float> offsetAxisValues, float modelCenter)
        {
            if (offsetAxisValues == null) return false;
            bool hasAny = false;
            float omax = float.MinValue;
            float omin = float.MaxValue;
            foreach (var v in offsetAxisValues)
            {
                if (v > omax) omax = v;
                if (v < omin) omin = v;
                hasAny = true;
            }
            if (!hasAny) return false;
            float distMaxSide = omax - modelCenter;
            float distMinSide = modelCenter - omin;
            return distMaxSide >= distMinSide;
        }

        #endregion

        /// <summary>
        /// REQ-005 (2026-05-11): lvDimension 행 선택 → 해당 치수의 두 부재 3D 강조 + 카메라 fit
        /// ChainDimensionData.MemberIndices 활용. 비어있으면 skip (가드).
        /// LvClash 흐름의 SelectRelatedDimensionItems 연쇄 트리거 가드: _suppressDimSelChanged
        /// </summary>
        private bool _suppressDimSelChanged = false;
        private void LvDimension_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressDimSelChanged) return;
            if (lvDimension.SelectedItems.Count == 0) return;

            HashSet<int> indexSet = new HashSet<int>();
            foreach (ListViewItem lvi in lvDimension.SelectedItems)
            {
                var dim = lvi.Tag as ChainDimensionData;
                if (dim == null || dim.MemberIndices == null) continue;
                foreach (int idx in dim.MemberIndices)
                    if (idx >= 0) indexSet.Add(idx);
            }
            if (indexSet.Count == 0) return;

            try
            {
                vizcore3d.BeginUpdate();
                var indices = indexSet.ToList();
                vizcore3d.Object3D.Color.RestoreColorAll();
                vizcore3d.Object3D.Select(indices, true, false);
                vizcore3d.View.FlyToObject3d(indices, 1.2f);
                vizcore3d.EndUpdate();
            }
            catch (Exception ex) { DiagLog($"REQ-005 LvDimension_SelectedIndexChanged FAIL {ex.Message}"); }
        }

        /// <summary>
        /// Clash 리스트 선택 변경 시 뷰어에서 해당 충돌 지점 표시 및 관련 Osnap/치수 자동 선택
        /// </summary>
        private void LvClash_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvClash.SelectedItems.Count == 0) return;

            try
            {
                // 관련 노드 이름 수집
                HashSet<string> relatedNodeNames = new HashSet<string>();
                // 관련 바운딩 박스 영역 수집
                List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> relatedBounds = new List<(float, float, float, float, float, float)>();

                foreach (ListViewItem lvi in lvClash.SelectedItems)
                {
                    ClashData clash = lvi.Tag as ClashData;
                    if (clash != null)
                    {
                        // 노드 이름 추가
                        if (!string.IsNullOrEmpty(clash.Name1))
                            relatedNodeNames.Add(clash.Name1);
                        if (!string.IsNullOrEmpty(clash.Name2))
                            relatedNodeNames.Add(clash.Name2);

                        BOMData bom1 = bomList.FirstOrDefault(b => b.Index == clash.Index1);
                        BOMData bom2 = bomList.FirstOrDefault(b => b.Index == clash.Index2);


                        if (bom1 != null && bom2 != null)
                        {
                            // 두 부재의 결합 바운딩 박스 저장
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

                // REQ-D (2026-05-11): 선택된 Clash 두 부재 3D 강조 + 카메라 fit (LvClash_DoubleClick 패턴)
                if (lvClash.SelectedItems.Count == 1)
                {
                    var clashHi = lvClash.SelectedItems[0].Tag as ClashData;
                    if (clashHi != null && clashHi.Index1 >= 0 && clashHi.Index2 >= 0)
                    {
                        try
                        {
                            vizcore3d.BeginUpdate();
                            vizcore3d.Object3D.Color.RestoreColorAll();
                            List<int> clashIdxs = new List<int> { clashHi.Index1, clashHi.Index2 };
                            vizcore3d.Object3D.Select(clashIdxs, true, false);
                            vizcore3d.View.FlyToObject3d(clashIdxs, 1.2f);
                            vizcore3d.EndUpdate();
                        }
                        catch (Exception ex) { DiagLog($"REQ-D LvClash 3D fit FAIL {ex.Message}"); }
                    }
                }

                // 관련 Osnap 좌표 자동 선택 (REQ-004 가드: LvOsnap_SelectedIndexChanged 연쇄 트리거 방지)
                _suppressOsnapSelChanged = true;
                try { SelectRelatedOsnapItems(relatedNodeNames, relatedBounds); }
                finally { _suppressOsnapSelChanged = false; }

                // 관련 치수 자동 선택 (REQ-005 가드: LvDimension_SelectedIndexChanged 연쇄 트리거 방지)
                _suppressDimSelChanged = true;
                try { SelectRelatedDimensionItems(relatedBounds); }
                finally { _suppressDimSelChanged = false; }
            }
            catch
            {
                // 선택 변경 중 오류는 무시
            }
        }

        /// <summary>
        /// Clash와 관련된 Osnap 항목 자동 선택
        /// </summary>
        private void SelectRelatedOsnapItems(HashSet<string> nodeNames, List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> bounds)
        {
            if (lvOsnap.Items.Count == 0) return;

            // 기존 선택 해제
            foreach (ListViewItem item in lvOsnap.SelectedItems)
            {
                item.Selected = false;
            }

            float tolerance = 1.0f; // 허용 오차

            // 관련 Osnap 항목 선택
            for (int i = 0; i < lvOsnap.Items.Count; i++)
            {
                ListViewItem lvi = lvOsnap.Items[i];

                // 부재 이름으로 매칭
                string osnapNodeName = lvi.SubItems.Count > 1 ? lvi.SubItems[1].Text : "";
                if (nodeNames.Contains(osnapNodeName))
                {
                    lvi.Selected = true;
                    continue;
                }

                // 좌표가 바운딩 박스 내에 있는지 확인
                if (i < osnapPoints.Count)
                {
                    var point = osnapPoints[i];
                    foreach (var bound in bounds)
                    {
                        if (point.X >= bound.MinX - tolerance && point.X <= bound.MaxX + tolerance &&
                            point.Y >= bound.MinY - tolerance && point.Y <= bound.MaxY + tolerance &&
                            point.Z >= bound.MinZ - tolerance && point.Z <= bound.MaxZ + tolerance)
                        {
                            lvi.Selected = true;
                            break;
                        }
                    }
                }
            }

            // 첫 번째 선택 항목으로 스크롤
            if (lvOsnap.SelectedItems.Count > 0)
            {
                lvOsnap.SelectedItems[0].EnsureVisible();
            }
        }

        /// <summary>
        /// Clash와 관련된 치수 항목 자동 선택
        /// </summary>
        private void SelectRelatedDimensionItems(List<(float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ)> bounds)
        {
            if (lvDimension.Items.Count == 0 || bounds.Count == 0) return;

            // 기존 선택 해제
            foreach (ListViewItem item in lvDimension.SelectedItems)
            {
                item.Selected = false;
            }

            float tolerance = 1.0f; // 허용 오차

            // 관련 치수 항목 선택
            foreach (ListViewItem lvi in lvDimension.Items)
            {
                ChainDimensionData dim = lvi.Tag as ChainDimensionData;
                if (dim == null) continue;

                // 치수의 시작점 또는 끝점이 바운딩 박스 내에 있는지 확인
                foreach (var bound in bounds)
                {
                    bool startInBound =
                        dim.StartPoint.X >= bound.MinX - tolerance && dim.StartPoint.X <= bound.MaxX + tolerance &&
                        dim.StartPoint.Y >= bound.MinY - tolerance && dim.StartPoint.Y <= bound.MaxY + tolerance &&
                        dim.StartPoint.Z >= bound.MinZ - tolerance && dim.StartPoint.Z <= bound.MaxZ + tolerance;

                    bool endInBound =
                        dim.EndPoint.X >= bound.MinX - tolerance && dim.EndPoint.X <= bound.MaxX + tolerance &&
                        dim.EndPoint.Y >= bound.MinY - tolerance && dim.EndPoint.Y <= bound.MaxY + tolerance &&
                        dim.EndPoint.Z >= bound.MinZ - tolerance && dim.EndPoint.Z <= bound.MaxZ + tolerance;

                    if (startInBound || endInBound)
                    {
                        lvi.Selected = true;
                        break;
                    }
                }
            }

            // 첫 번째 선택 항목으로 스크롤
            if (lvDimension.SelectedItems.Count > 0)
            {
                lvDimension.SelectedItems[0].EnsureVisible();
            }
        }


        /// <summary>
        /// 체인 치수 데이터 리스트
        /// </summary>

        /// <summary>
        /// 부재 이름 입력 TextBox를 3D 뷰어(panelViewer) 위에 오버레이로 표시
        /// </summary>
        private void ShowMemberNameOverlay(string initialName)
        {
            if (txtMemberNameOverlay == null)
            {
                txtMemberNameOverlay = new TextBox();
                txtMemberNameOverlay.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                txtMemberNameOverlay.BackColor = Color.FromArgb(45, 45, 48);
                txtMemberNameOverlay.ForeColor = Color.White;
                txtMemberNameOverlay.BorderStyle = BorderStyle.FixedSingle;
                txtMemberNameOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                txtMemberNameOverlay.Location = new Point(10, 5);
                txtMemberNameOverlay.Width = panelViewer.Width - 20;
                panelViewer.Controls.Add(txtMemberNameOverlay);
            }
            txtMemberNameOverlay.Text = initialName ?? "";
            txtMemberNameOverlay.BringToFront();
            txtMemberNameOverlay.Visible = true;
        }

        /// <summary>
        /// 체인 치수 추출 (MeasureManager API 사용)
        /// </summary>
        private void btnExtractDimension_Click(object sender, EventArgs e)
        {
            // [T-016 진단 로그] 진입 시 상태
            DiagLog($"btnExtractDimension ENTER " +
                $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0} " +
                $"osnap={osnapPointsWithNames?.Count ?? 0}");

            try
            {
                // 이전 선택 상태 초기화 → CollectAllOsnap이 현재 보이는 노드 기준으로 수집
                xraySelectedNodeIndices.Clear();

                // 이전 어노테이션 일괄 초기화
                vizcore3d.Review.Note.Clear();
                vizcore3d.Review.Measure.Clear();
                vizcore3d.ShapeDrawing.Clear();

                // BOM 데이터 초기화 후 현재 보이는 노드 기준으로 재수집
                CollectBOMData();

                // 현재 보이는 노드로 xraySelectedNodeIndices 갱신
                var visibleBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (visibleBodyNodes != null)
                {
                    foreach (var node in visibleBodyNodes)
                    {
                        var realNode = vizcore3d.Object3D.FromIndex(node.Index);
                        if (realNode != null && realNode.Visible)
                        {
                            xraySelectedNodeIndices.Add(node.Index);
                            // Part 인덱스도 추가 (BOM 필터링용)
                            if (bodyToPartIndexMap.ContainsKey(node.Index))
                                xraySelectedNodeIndices.Add(bodyToPartIndexMap[node.Index]);
                        }
                    }
                }

                // 현재 보이는 상태에 맞게 Osnap 재수집
                CollectAllOsnap();

                if (osnapPointsWithNames == null || osnapPointsWithNames.Count == 0)
                {
                    MessageBox.Show("먼저 Osnap 좌표를 수집해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 선택된 도면시트의 BaseMemberName을 가져와 TextBox 오버레이 표시
                string memberName = "";
                if (lvDrawingSheet.SelectedItems.Count > 0)
                {
                    DrawingSheetData selectedSheet = lvDrawingSheet.SelectedItems[0].Tag as DrawingSheetData;
                    if (selectedSheet != null)
                        memberName = selectedSheet.BaseMemberName ?? "";
                }
                ShowMemberNameOverlay(memberName);

                // 기존 측정 항목 제거
                vizcore3d.Review.Measure.Clear();
                chainDimensionList.Clear();
                lvDimension.Items.Clear();

                float tolerance = 0.5f;  // 허용 오차 0.5mm

                // 좌표 병합 (허용 오차 내 같은 좌표로 그룹화)
                // REQ-003 (2026-05-11): osnapPointsWithNames는 3원소(axis 포함)지만 MergeCoordinates는 2원소만 받음 → 변환
                var osnapPts2 = osnapPointsWithNames.Select(p => (p.point, p.nodeName)).ToList();
                List<VIZCore3D.NET.Data.Vector3D> mergedPoints = MergeCoordinates(osnapPts2, tolerance);

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

                // 결과 출력
                string result = $"체인 치수 추출 완료!\n\n" +
                               $"총 Osnap 좌표: {osnapPointsWithNames.Count}개\n" +
                               $"병합 후 좌표: {mergedPoints.Count}개\n\n" +
                               $"X축 방향 치수: {xDimensions.Count}개\n" +
                               $"Y축 방향 치수: {yDimensions.Count}개\n" +
                               $"Z축 방향 치수: {zDimensions.Count}개\n\n" +
                               $"총 치수: {chainDimensionList.Count}개";

                MessageBox.Show(result, "치수 추출 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 치수 추출 후 자동으로 모든 치수 표시 (오프셋 + 보조선 스타일)
                ShowAllDimensions();

                // [T-016 진단 로그] 정상 종료
                DiagLog($"btnExtractDimension EXIT OK " +
                    $"xray={xraySelectedNodeIndices?.Count ?? 0} chain={chainDimensionList?.Count ?? 0} " +
                    $"osnap={osnapPointsWithNames?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                // [T-016 진단 로그] 예외 종료
                DiagLog($"btnExtractDimension EXIT FAIL " +
                    $"{ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"치수 추출 중 오류:\n\n{ex.Message}\n\n{ex.StackTrace}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 좌표 병합 (허용 오차 내 같은 좌표로 그룹화)
        /// </summary>
        private List<VIZCore3D.NET.Data.Vector3D> MergeCoordinates(
            List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> points, float tolerance)
        {
            List<VIZCore3D.NET.Data.Vector3D> result = new List<VIZCore3D.NET.Data.Vector3D>();

            foreach (var pt in points)
            {
                float x = RoundToTolerance(pt.point.X, tolerance);
                float y = RoundToTolerance(pt.point.Y, tolerance);
                float z = RoundToTolerance(pt.point.Z, tolerance);

                // 중복 제거
                bool exists = result.Any(r =>
                    Math.Abs(r.X - x) < tolerance &&
                    Math.Abs(r.Y - y) < tolerance &&
                    Math.Abs(r.Z - z) < tolerance);

                if (!exists)
                {
                    result.Add(new VIZCore3D.NET.Data.Vector3D(x, y, z));
                }
            }

            return result;
        }

        /// <summary>
        /// 허용 오차 기준으로 좌표 반올림
        /// </summary>
        private float RoundToTolerance(float value, float tolerance)
        {
            return (float)(Math.Round(value / tolerance) * tolerance);
        }

        /// <summary>
        /// 축에 따른 뷰 이름 반환
        /// </summary>
        private string GetViewNameByAxis(string axis)
        {
            switch (axis)
            {
                case "X": return "측면도";
                case "Y": return "정면도";
                case "Z": return "평면도";
                default: return "";
            }
        }

        /// <summary>
        /// 특정 축 방향 체인 치수 추가
        /// 1. 같은 측정축 값의 포인트 중 필터축 최소값만 남김
        ///    Z치수→min Y, Y치수→min X, X치수→min Z
        /// 2. 큰 값에서 작은 값 순서로 순차 치수
        /// 3. 마지막에 전체 치수 (처음~끝)
        /// </summary>
        /// <summary>
        /// 뷰 방향과 치수축에서 나머지 보이는 축 반환 (필터축 결정용)
        /// 예: viewDir=X, dimAxis=Y → 나머지=Z (아래쪽 우선 필터)
        /// </summary>
        private string GetRemainingAxis(string viewDirection, string dimAxis)
        {
            string[] all = { "X", "Y", "Z" };
            foreach (var a in all)
            {
                if (a != viewDirection && a != dimAxis) return a;
            }
            return "X";
        }

        // ─── T-040 v12: v6 직각 시프트 베이스 + 임계 maxEstDist/26 + 인접 비교 부호 ───
        // 사양:
        //  - 임계: 측정값 ≤ maxEstDist / 26 (사용자 사양 — v11의 13 → 동적)
        //  - 시프트 거리: 캔버스 3mm
        //  - SDK measureItem 직접 순회 (가공도에도 동일 작동)
        //  - 직각 시프트 (가로 치수 → right, 세로 치수 → up)
        //  - shiftDir: SDK measure를 측정축별 그룹 후 측정축 중심 좌표 순 정렬 → 인접 dim의 estDist 비교
        //    · 양쪽 인접 큰 쪽 / 같음 +1 / 한쪽만 반대(체인 바깥) / 없음 skip
        //  - 뷰 max ≤ 100mm 시 전체 skip
        private void ApplyParallelTextShift(
            string viewDirection,
            float canvasScale,
            List<VIZCore3D.NET.Data.MeasureItem> measures)
        {
            if (viewDirection == "ISO") return;
            if (canvasScale <= 0.0001f) return;
            if (measures == null || measures.Count == 0) return;

            // 1차 패스: 각 측정의 MAIN 두 좌표 + dimAxis + dimCenter + estDist + 시프트용 posItem 수집
            var infos = new List<(VIZCore3D.NET.Data.MeasureItem m, char dimAxis, float dimCenter, float estDist, VIZCore3D.NET.Data.ReviewPosition textPos)>();
            float maxEstDist = 0f;
            foreach (var measure in measures)
            {
                if (!measure.Visible) continue;
                // 각도 측정은 두 점 간 거리 개념이 없어 시프트 대상에서 제외 (비-90° 각도 표시, 2026-06-23)
                if (measure.Kind == VIZCore3D.NET.Manager.ReviewManager.ReviewKind.RK_MEASURE_ANGLE ||
                    measure.Kind == VIZCore3D.NET.Manager.ReviewManager.ReviewKind.RK_MEASURE_SURFACE_ANGLE) continue;
                VIZCore3D.NET.Data.Vertex3D mp0 = null, mp1 = null;
                foreach (var pos in measure.Position)
                {
                    if (pos.Kind != VIZCore3D.NET.Data.ReviewPosition.DataKind.MAIN) continue;
                    if (pos.Position == null) continue;
                    if (mp0 == null) mp0 = pos.Position;
                    else { mp1 = pos.Position; break; }
                }
                if (mp0 == null || mp1 == null) continue;

                float ddx = mp0.X - mp1.X, ddy = mp0.Y - mp1.Y, ddz = mp0.Z - mp1.Z;
                float estDist = (float)Math.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);
                if (estDist > maxEstDist) maxEstDist = estDist;

                float adx = Math.Abs(ddx), ady = Math.Abs(ddy), adz = Math.Abs(ddz);
                char dimAxis = (adx >= ady && adx >= adz) ? 'X' : (ady >= adz ? 'Y' : 'Z');
                float dimCenter = (dimAxis == 'X') ? (mp0.X + mp1.X) / 2f
                                : (dimAxis == 'Y') ? (mp0.Y + mp1.Y) / 2f
                                : (mp0.Z + mp1.Z) / 2f;

                // 시프트 대상 posItem (Text 비어있지 않은 첫 항목)
                VIZCore3D.NET.Data.ReviewPosition textPos = null;
                foreach (var pos in measure.Position)
                {
                    if (string.IsNullOrEmpty(pos.Text)) continue;
                    if (pos.Position == null) continue;
                    textPos = pos;
                    break;
                }
                if (textPos == null) continue;

                infos.Add((measure, dimAxis, dimCenter, estDist, textPos));
            }

            if (maxEstDist <= 100f)
            {
                DiagLog($"T-040 TextShift view={viewDirection} skip (maxEstDist={maxEstDist:F1}mm <= 100mm)");
                return;
            }

            float threshold = maxEstDist / 26f;
            float modelShift = 3f / canvasScale;
            int shiftedCount = 0;

            // P3 #3 진단 (2026-05-23, 사용자 사내 검증):
            //   같은 축 chain dimension에서 작은 측정(6mm)이 잘못된 방향으로 시프트되어
            //   다른 측정(59mm)과 겹치는 문제. dimAxis·dimCenter·shiftDir 정확히 추적.
            DiagLog($"[TextShift] BEGIN view={viewDirection} canvasScale={canvasScale:F4} modelShift={modelShift:F2}mm threshold={threshold:F2}mm maxEstDist={maxEstDist:F2}mm infoCount={infos.Count}");
            foreach (var info in infos)
                DiagLog($"[TextShift]   info dimAxis={info.dimAxis} dimCenter={info.dimCenter:F2} estDist={info.estDist:F2} textPos=({info.textPos.Position.X:F2},{info.textPos.Position.Y:F2},{info.textPos.Position.Z:F2})");

            // 측정축별 그룹 → 측정축 중심 좌표 순 정렬 → 인접 식별
            foreach (var axisGrp in infos.GroupBy(x => x.dimAxis))
            {
                var sorted = axisGrp.OrderBy(x => x.dimCenter).ToList();
                DiagLog($"[TextShift] axis={axisGrp.Key} sorted({sorted.Count}): [{string.Join(", ", sorted.Select(s => $"{s.estDist:F1}@{s.dimCenter:F1}"))}]");

                for (int i = 0; i < sorted.Count; i++)
                {
                    var info = sorted[i];
                    if (info.estDist > threshold)
                    {
                        DiagLog($"[TextShift]   i={i} estDist={info.estDist:F2} > threshold={threshold:F2} → skip");
                        continue;
                    }

                    float? leftDist = (i > 0) ? sorted[i - 1].estDist : (float?)null;
                    float? rightDist = (i < sorted.Count - 1) ? sorted[i + 1].estDist : (float?)null;

                    int shiftDir;
                    string reason;
                    if (leftDist.HasValue && rightDist.HasValue)
                    {
                        if (leftDist.Value > rightDist.Value) { shiftDir = -1; reason = $"both L={leftDist:F1} > R={rightDist:F1} → -1 (left, 큰 쪽)"; }
                        else if (rightDist.Value > leftDist.Value) { shiftDir = +1; reason = $"both R={rightDist:F1} > L={leftDist:F1} → +1 (right, 큰 쪽)"; }
                        else { shiftDir = +1; reason = $"both equal → +1"; }
                    }
                    else if (leftDist.HasValue) { shiftDir = +1; reason = $"L={leftDist:F1} only → +1 (right, 빈 쪽)"; }
                    else if (rightDist.HasValue) { shiftDir = -1; reason = $"R={rightDist:F1} only → -1 (left, 빈 쪽)"; }
                    else { DiagLog($"[TextShift]   i={i} estDist={info.estDist:F2} both null → skip"); continue; }

                    DiagLog($"[TextShift]   i={i} estDist={info.estDist:F2} dimAxis={info.dimAxis} dimCenter={info.dimCenter:F2} shiftDir={shiftDir} reason={reason}");

                    var p = info.textPos.Position;
                    VIZCore3D.NET.Data.Vector3D shifted;
                    // 직각 시프트 (가로 → right, 세로 → up) × shiftDir
                    switch (viewDirection)
                    {
                        case "X":  // right=+Y, up=+Z
                            if (info.dimAxis == 'Y')
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X, p.Y + shiftDir * modelShift, p.Z);
                            else
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X, p.Y, p.Z + shiftDir * modelShift);
                            break;
                        case "Y":  // right=-X, up=+Z
                            if (info.dimAxis == 'X')
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X - shiftDir * modelShift, p.Y, p.Z);
                            else
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X, p.Y, p.Z + shiftDir * modelShift);
                            break;
                        case "Z":  // top: right=+X, up=+Y
                            if (info.dimAxis == 'X')
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X + shiftDir * modelShift, p.Y, p.Z);
                            else
                                shifted = new VIZCore3D.NET.Data.Vector3D(p.X, p.Y + shiftDir * modelShift, p.Z);
                            break;
                        default:
                            shifted = new VIZCore3D.NET.Data.Vector3D(p.X, p.Y, p.Z);
                            break;
                    }
                    DiagLog($"[TextShift]   → shifted to ({shifted.X:F2},{shifted.Y:F2},{shifted.Z:F2}) (was {p.X:F2},{p.Y:F2},{p.Z:F2})");
                    vizcore3d.Drawing2D.Measure.SetMeasureItemDistanceTextPos(info.m.ID, shifted);
                    shiftedCount++;
                }
            }

            DiagLog($"T-040 TextShift view={viewDirection} canvasScale={canvasScale:F4} modelShift={modelShift:F1}mm threshold={threshold:F1}mm maxEstDist={maxEstDist:F1}mm shifted={shiftedCount}");
            DiagLog($"[TextShift] END view={viewDirection} shifted={shiftedCount}/{infos.Count}");
        }

        private List<ChainDimensionData> AddChainDimensionByAxis(
            List<VIZCore3D.NET.Data.Vector3D> points, string axis, float tolerance,
            string viewDirection = null)
        {
            List<ChainDimensionData> dimensions = new List<ChainDimensionData>();

            if (points == null || points.Count < 2) return dimensions;

            // Step 1: 뷰 방향에 따른 필터축 결정 ("제일 아래 왼쪽" 우선)
            // 뷰에서 보이는 축 중 치수축이 아닌 축을 필터축으로 사용
            string filterAxisName;
            if (viewDirection != null)
            {
                // X뷰(Y-Z보임): Y치수→필터Z, Z치수→필터Y
                // Y뷰(X-Z보임): X치수→필터Z, Z치수→필터X
                // Z뷰(X-Y보임): X치수→필터Y, Y치수→필터X
                filterAxisName = GetRemainingAxis(viewDirection, axis);
            }
            else
            {
                // 기본: X→Z, Y→X, Z→Y
                switch (axis)
                {
                    case "X": filterAxisName = "Z"; break;
                    case "Y": filterAxisName = "X"; break;
                    default: filterAxisName = "Y"; break;
                }
            }

            // 같은 치수축 값의 포인트 중 필터축 최소값만 남김 (아래 왼쪽 우선)
            var grouped = new Dictionary<string, VIZCore3D.NET.Data.Vector3D>();
            foreach (var pt in points)
            {
                float dimValue = RoundToTolerance(GetAxisValue(pt, axis), tolerance);
                float filterValue = GetAxisValue(pt, filterAxisName);
                string key = dimValue.ToString("F1");

                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = pt;
                }
                else
                {
                    float existingFilterValue = GetAxisValue(grouped[key], filterAxisName);
                    if (filterValue < existingFilterValue)
                    {
                        grouped[key] = pt;
                    }
                }
            }

            // Step 2: 측정축 값 기준 오름차순 정렬 (중심에서 Osnap 위치 방향으로)
            var sortedPoints = grouped.Values
                .OrderBy(p => GetAxisValue(p, axis))
                .ToList();

            if (sortedPoints.Count < 2) return dimensions;

            // Step 3: 순차 치수 (인접 포인트 간 거리)
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                float distance = Math.Abs(
                    GetAxisValue(sortedPoints[i], axis) -
                    GetAxisValue(sortedPoints[i + 1], axis));

                if (distance > tolerance)
                {
                    ChainDimensionData dimData = new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        ViewDirection = viewDirection,  // T-028: 뷰 필터용
                        Distance = distance,
                        StartPoint = sortedPoints[i],
                        EndPoint = sortedPoints[i + 1],
                        StartPointStr = $"({sortedPoints[i].X:F1}, {sortedPoints[i].Y:F1}, {sortedPoints[i].Z:F1})",
                        EndPointStr = $"({sortedPoints[i + 1].X:F1}, {sortedPoints[i + 1].Y:F1}, {sortedPoints[i + 1].Z:F1})"
                    };
                    dimensions.Add(dimData);
                }
            }

            // Step 4: 축방향 전체 치수 (처음 ~ 끝) - 순차 치수가 2개 이상일 때
            if (sortedPoints.Count > 2)
            {
                var first = sortedPoints[0];
                var last = sortedPoints[sortedPoints.Count - 1];
                float totalDistance = Math.Abs(
                    GetAxisValue(first, axis) - GetAxisValue(last, axis));

                if (totalDistance > tolerance)
                {
                    ChainDimensionData totalDim = new ChainDimensionData
                    {
                        Axis = axis,
                        ViewName = GetViewNameByAxis(axis),
                        ViewDirection = viewDirection,  // T-028
                        Distance = totalDistance,
                        StartPoint = first,
                        EndPoint = last,
                        StartPointStr = $"({first.X:F1}, {first.Y:F1}, {first.Z:F1})",
                        EndPointStr = $"({last.X:F1}, {last.Y:F1}, {last.Z:F1})",
                        IsTotal = true
                    };
                    dimensions.Add(totalDim);
                }
            }

            return dimensions;
        }

        /// <summary>
        /// T-028: 2D 출력 엔진과 동일한 로직으로 주어진 부재 집합의 치수 계산.
        ///
        /// 흐름: nodeOsnapMap 구축 → (뷰×치수축 조합 루프) → FilterOsnapForDimAxis →
        ///       MergeCoordinates → AddChainDimensionByAxis(axis, viewDirection)
        ///
        /// 파라미터:
        ///   memberIndices: 대상 부재 Body 인덱스 리스트 (전체 visible 또는 시트 부재 등)
        ///   viewDirection: null → 3뷰(X/Y/Z) × 2축 = 6조합 모두 생성 / "X"/"Y"/"Z" → 해당 뷰 2축만
        ///   tolerance: MergeCoordinates 허용오차 (기본 0.5mm)
        ///
        /// 반환: 체인 치수 리스트. 같은 (Axis, StartPoint, EndPoint) 3자리 반올림 기준 중복 제거,
        ///       ViewDirection은 콤마 구분으로 누적 병합 (예: "X,Y" = X·Y 뷰 양쪽에 표시).
        ///
        /// 기준: ShowAllDimensions(viewDirection) 분기 ② 로직 = 2D 출력(GenerateSheetDrawing2D)에서 사용하는 것.
        ///
        /// T-032: `preBuiltNodeOsnapMap` 파라미터 — 이미 `CollectAllOsnap`에서 수집된 맵을 전달하면
        /// `GetOsnapPoint` 중복 호출을 피할 수 있음. null이면 내부에서 신규 구축(시트 선택 자동 경로).
        /// </summary>
        private List<ChainDimensionData> ComputeViewDimensionsForMembers(
            List<int> memberIndices, string viewDirection = null, float tolerance = 0.5f,
            Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>> preBuiltNodeOsnapMap = null)
        {
            List<ChainDimensionData> result = new List<ChainDimensionData>();
            if (memberIndices == null || memberIndices.Count == 0) return result;

            // 1. nodeOsnapMap 준비 — 하이브리드 (E1, 2026-05-18):
            //    (1) preBuilt 캐시에서 hit 부재만 빠르게 복사 (GetOsnapPoint 호출 없음)
            //    (2) 캐시에 없는 부재만 GetOsnapPoint로 보충 (fallback 보장)
            //    → 캐시 신선도 무관하게 항상 모든 memberIndices 부재 커버
            Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>> nodeOsnapMap =
                new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D, string)>>();

            HashSet<int> memberSet = new HashSet<int>(memberIndices);

            // (1) 빠른 경로 — preBuilt 캐시 hit
            int cacheHit = 0;
            if (preBuiltNodeOsnapMap != null && preBuiltNodeOsnapMap.Count > 0)
            {
                foreach (var kv in preBuiltNodeOsnapMap)
                {
                    if (memberSet.Contains(kv.Key))
                    {
                        nodeOsnapMap[kv.Key] = kv.Value;
                        cacheHit++;
                    }
                }
            }

            // (2) Fallback — 캐시에 없는 부재만 GetOsnapPoint로 보충
            var missingMembers = memberSet.Where(idx => !nodeOsnapMap.ContainsKey(idx)).ToList();
            int cacheMiss = missingMembers.Count;
            if (cacheMiss > 0)
            {
                List<VIZCore3D.NET.Data.Node> allBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                if (allBodyNodes != null && allBodyNodes.Count > 0)
                {
                    var missingSet = new HashSet<int>(missingMembers);
                    var bodyNodes = allBodyNodes.Where(n => missingSet.Contains(n.Index)).ToList();

                    foreach (var node in bodyNodes)
                    {
                        string partName = GetPartNameFromBodyIndex(node.Index, node.NodeName);
                        var pts = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
                        try
                        {
                            var osnapList = vizcore3d.Object3D.GetOsnapPoint(node.Index);
                            if (osnapList != null)
                            {
                                foreach (var osnap in osnapList)
                                {
                                    switch (osnap.Kind)
                                    {
                                        case VIZCore3D.NET.Data.OsnapKind.LINE:
                                            if (osnap.Start != null)
                                                pts.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Start.X, osnap.Start.Y, osnap.Start.Z), partName));
                                            if (osnap.End != null)
                                                pts.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.End.X, osnap.End.Y, osnap.End.Z), partName));
                                            break;
                                        case VIZCore3D.NET.Data.OsnapKind.POINT:
                                            if (osnap.Center != null)
                                                pts.Add((new VIZCore3D.NET.Data.Vertex3D(osnap.Center.X, osnap.Center.Y, osnap.Center.Z), partName));
                                            break;
                                    }
                                }
                            }
                        }
                        catch { }
                        if (pts.Count > 0)
                            nodeOsnapMap[node.Index] = pts;
                    }
                }
            }

            DiagLog($"E1 Osnap cache: hit={cacheHit} miss={cacheMiss} members={memberSet.Count} preBuilt={(preBuiltNodeOsnapMap?.Count ?? 0)}");

            if (nodeOsnapMap.Count == 0) return result;

            // REQ-005 (2026-05-11): 좌표 키 → 부재 인덱스 집합 사전 구축
            //   결과 dim의 StartPoint/EndPoint 좌표로 lookup해 MemberIndices 채움 (lvDimension 행 선택 강조용)
            //   tolerance 단위 반올림으로 동일 키 매핑
            Dictionary<string, HashSet<int>> coordKeyToMembers = new Dictionary<string, HashSet<int>>();
            foreach (var kv in nodeOsnapMap)
            {
                int nodeIdx = kv.Key;
                foreach (var pt in kv.Value)
                {
                    float rx = RoundToTolerance(pt.point.X, tolerance);
                    float ry = RoundToTolerance(pt.point.Y, tolerance);
                    float rz = RoundToTolerance(pt.point.Z, tolerance);
                    string ck = $"{rx:F1},{ry:F1},{rz:F1}";
                    if (!coordKeyToMembers.TryGetValue(ck, out var set))
                    {
                        set = new HashSet<int>();
                        coordKeyToMembers[ck] = set;
                    }
                    set.Add(nodeIdx);
                }
            }

            // 2. 처리할 뷰 목록
            string[] viewsToProcess = string.IsNullOrEmpty(viewDirection)
                ? new string[] { "X", "Y", "Z" }
                : new string[] { viewDirection };

            // 3. 뷰 × 치수축 조합별 치수 계산
            List<ChainDimensionData> raw = new List<ChainDimensionData>();
            foreach (string view in viewsToProcess)
            {
                List<string> visibleAxes = new List<string>();
                switch (view)
                {
                    case "X": visibleAxes.Add("Y"); visibleAxes.Add("Z"); break;
                    case "Y": visibleAxes.Add("X"); visibleAxes.Add("Z"); break;
                    case "Z": visibleAxes.Add("X"); visibleAxes.Add("Y"); break;
                }

                foreach (string axis in visibleAxes)
                {
                    var filteredPts = FilterOsnapForDimAxis(nodeOsnapMap, axis, view, tolerance);
                    var mergedPts = MergeCoordinates(filteredPts, tolerance);
                    raw.AddRange(AddChainDimensionByAxis(mergedPts, axis, tolerance, view));
                }
            }

            // 4. 중복 제거 (Axis, Start, End 3자리 반올림 기준) + ViewDirection 콤마 병합
            Dictionary<string, ChainDimensionData> keyToDim = new Dictionary<string, ChainDimensionData>();
            foreach (var dim in raw)
            {
                string key = $"{dim.Axis}|{dim.StartPoint.X:F1},{dim.StartPoint.Y:F1},{dim.StartPoint.Z:F1}|{dim.EndPoint.X:F1},{dim.EndPoint.Y:F1},{dim.EndPoint.Z:F1}";
                if (keyToDim.TryGetValue(key, out var existing))
                {
                    if (!string.IsNullOrEmpty(dim.ViewDirection) &&
                        (existing.ViewDirection == null || !existing.ViewDirection.Split(',').Contains(dim.ViewDirection)))
                    {
                        existing.ViewDirection = string.IsNullOrEmpty(existing.ViewDirection)
                            ? dim.ViewDirection
                            : $"{existing.ViewDirection},{dim.ViewDirection}";
                    }
                }
                else
                {
                    keyToDim[key] = dim;
                }
            }

            // REQ-005 (2026-05-11): 결과 dim의 StartPoint/EndPoint → coordKeyToMembers lookup → MemberIndices 채움
            foreach (var dim in keyToDim.Values)
            {
                var memberSetPerDim = new HashSet<int>();
                string skey = $"{dim.StartPoint.X:F1},{dim.StartPoint.Y:F1},{dim.StartPoint.Z:F1}";
                string ekey = $"{dim.EndPoint.X:F1},{dim.EndPoint.Y:F1},{dim.EndPoint.Z:F1}";
                if (coordKeyToMembers.TryGetValue(skey, out var sset))
                    foreach (var i in sset) memberSetPerDim.Add(i);
                if (coordKeyToMembers.TryGetValue(ekey, out var eset))
                    foreach (var i in eset) memberSetPerDim.Add(i);
                if (memberSetPerDim.Count > 0)
                    dim.MemberIndices = memberSetPerDim.ToList();
            }

            result.AddRange(keyToDim.Values);
            return result;
        }

        /// <summary>
        /// Osnap 필터링 공통 함수 — X/Y/Z 보기 모두 동일 규칙 적용
        ///
        /// 축 매핑:
        ///   X축 보기(YZ 평면): 주축=Z, 보조축=Y
        ///   Y축 보기(XZ 평면): 주축=Z, 보조축=X
        ///   Z축 보기(XY 평면): 주축=Y, 보조축=X
        ///
        /// 처리 순서:
        ///   1) 후보 수집 (nodeOsnapMap에서 dimAxis 방향 값 추출)
        ///   2) 필수점 A: 주축 최대 (동률 시 보조축 최대)
        ///   3) 필수점 B: 주축 최소 (동률 시 보조축 최대)
        ///   4) 부재별 1차 필터: 부재당 주축 최대 1개 (동률 시 보조축 최대)
        ///   5) 전역 주축 중복 제거: 같은 주축값 Osnap은 1개만 유지 (보조축 최대 우선)
        ///   6) 필수점 A/B 강제 포함
        /// </summary>
        private List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)> FilterOsnapForDimAxis(
            Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>> nodeOsnapMap,
            string dimAxis, string viewDirection, float tolerance)
        {
            // --- 축 매핑: 주축/보조축 결정 ---
            string primaryAxis, secondaryAxis;
            switch (viewDirection)
            {
                case "X": primaryAxis = "Z"; secondaryAxis = "Y"; break;
                case "Y": primaryAxis = "Z"; secondaryAxis = "X"; break;
                case "Z": primaryAxis = "Y"; secondaryAxis = "X"; break;
                default:  primaryAxis = "Z"; secondaryAxis = "X"; break;
            }

            Func<VIZCore3D.NET.Data.Vertex3D, float> getDim = VertexAxisGetter(dimAxis);
            Func<VIZCore3D.NET.Data.Vertex3D, float> getPri = VertexAxisGetter(primaryAxis);
            Func<VIZCore3D.NET.Data.Vertex3D, float> getSec = VertexAxisGetter(secondaryAxis);

            // --- Step 1: 후보 수집 (전체 플랫 리스트) ---
            var allCandidates = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName, int nodeIdx)>();
            foreach (var kvp in nodeOsnapMap)
            {
                foreach (var pt in kvp.Value)
                    allCandidates.Add((pt.point, pt.nodeName, kvp.Key));
            }

            int totalBeforeFilter = allCandidates.Count;
            if (allCandidates.Count < 2) return allCandidates.Select(c => (c.point, c.nodeName)).ToList();

            // --- Step 2: 필수점 4개 결정 ---
            // A: 주축 MAX (동률 시 보조축 MAX)
            var pointA = allCandidates
                .OrderByDescending(c => getPri(c.point))
                .ThenByDescending(c => getSec(c.point))
                .First();
            // B: 주축 MIN (동률 시 보조축 MAX)
            var pointB = allCandidates
                .OrderBy(c => getPri(c.point))
                .ThenByDescending(c => getSec(c.point))
                .First();
            // C: 보조축 MAX (동률 시 주축 MAX)
            var pointC = allCandidates
                .OrderByDescending(c => getSec(c.point))
                .ThenByDescending(c => getPri(c.point))
                .First();
            // D: 보조축 MIN (동률 시 주축 MAX)
            var pointD = allCandidates
                .OrderBy(c => getSec(c.point))
                .ThenByDescending(c => getPri(c.point))
                .First();

            // --- Step 4: 부재별 1차 필터 (부재(Part)당 주축 최대 1개, 동률 시 보조축 최대) ---
            // 키: nodeName (Part명) — 같은 Part 하위 Body가 여러 개여도 1개만 유지
            var perMember = new Dictionary<string, (VIZCore3D.NET.Data.Vertex3D point, string nodeName, int nodeIdx)>();
            foreach (var c in allCandidates)
            {
                string memberKey = c.nodeName ?? c.nodeIdx.ToString();
                if (!perMember.ContainsKey(memberKey))
                {
                    perMember[memberKey] = c;
                }
                else
                {
                    var existing = perMember[memberKey];
                    float existPri = getPri(existing.point);
                    float candPri = getPri(c.point);
                    if (candPri > existPri + tolerance ||
                        (Math.Abs(candPri - existPri) <= tolerance && getSec(c.point) > getSec(existing.point) + tolerance))
                    {
                        perMember[memberKey] = c;
                    }
                }
            }
            var afterMemberFilter = perMember.Values.ToList();
            int afterMemberCount = afterMemberFilter.Count;

            // --- Step 5: 전역 주축 중복 제거 (같은 dimAxis 값은 1개만, 보조축 최대 우선) ---
            var grouped = new Dictionary<string, (VIZCore3D.NET.Data.Vertex3D point, string nodeName, int nodeIdx)>();
            foreach (var c in afterMemberFilter)
            {
                float dimVal = RoundToTolerance(getDim(c.point), tolerance);
                string key = dimVal.ToString("F1");
                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = c;
                }
                else
                {
                    var existing = grouped[key];
                    if (getSec(c.point) > getSec(existing.point) + tolerance)
                    {
                        grouped[key] = c;
                    }
                }
            }
            var afterDedup = grouped.Values.ToList();
            int dedupRemoved = afterMemberCount - afterDedup.Count;

            // --- Step 6: 필수점 A/B/C/D 강제 포함 ---
            float tolCheck = tolerance;
            var requiredPoints = new[] { pointA, pointB, pointC, pointD };
            foreach (var req in requiredPoints)
            {
                bool exists = afterDedup.Any(c =>
                    Math.Abs(c.point.X - req.point.X) < tolCheck &&
                    Math.Abs(c.point.Y - req.point.Y) < tolCheck &&
                    Math.Abs(c.point.Z - req.point.Z) < tolCheck);
                if (!exists)
                    afterDedup.Add(req);
            }

            // --- Debug 로그 ---
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap] dimAxis={dimAxis}, view={viewDirection}, primary={primaryAxis}, secondary={secondaryAxis}");
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap]   필터 전: {totalBeforeFilter}개 → 부재별 필터 후: {afterMemberCount}개 → 중복제거: -{dedupRemoved}개 → 최종: {afterDedup.Count}개");
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap]   필수점 A (주축MAX): ({pointA.point.X:F1}, {pointA.point.Y:F1}, {pointA.point.Z:F1}) [{pointA.nodeName}]");
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap]   필수점 B (주축MIN): ({pointB.point.X:F1}, {pointB.point.Y:F1}, {pointB.point.Z:F1}) [{pointB.nodeName}]");
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap]   필수점 C (보조축MAX): ({pointC.point.X:F1}, {pointC.point.Y:F1}, {pointC.point.Z:F1}) [{pointC.nodeName}]");
            System.Diagnostics.Debug.WriteLine($"[FilterOsnap]   필수점 D (보조축MIN): ({pointD.point.X:F1}, {pointD.point.Y:F1}, {pointD.point.Z:F1}) [{pointD.nodeName}]");

            return afterDedup.Select(c => (c.point, c.nodeName)).ToList();
        }

        /// <summary>
        /// Vertex3D에서 축 값을 가져오는 Func 반환
        /// </summary>
        private Func<VIZCore3D.NET.Data.Vertex3D, float> VertexAxisGetter(string axis)
        {
            switch (axis)
            {
                case "X": return p => p.X;
                case "Y": return p => p.Y;
                case "Z": return p => p.Z;
                default: return p => 0f;
            }
        }

        // ── 부재-부재 접합 각도 표시 (제작도 X/Y/Z 뷰, 사용자 사양 2026-06-23) ──
        //   서로 다른 두 부재가 접합하는 곳에서, 두 부재의 길이축이 수직(90°)·수평/평행(0/180°)이
        //   아니면(= 틀어져 만나면) 그 사잇각을 표시한다. 한 부재 '내부' 모서리 각(ㄱ자 꺾임)은 표시 안 함.
        //   · 연결성·접합점: osnap 끝점 근접으로 자체 판정(간섭검사 clashList 상태에 의존 X).
        //   · 길이축: 부재 osnap 점군의 PCA 주성분(분산 최대 방향). 최원점쌍은 박스형 부재에서
        //     대각선이 잡혀 틀리므로 쓰지 않는다(멱승법으로 공분산 최대 고유벡터 계산).
        //   · 각도: 두 길이축의 실제 3D 사잇각(부재가 진짜 직각으로 만나는지 검증 목적). 그릴 수 없는
        //     (깊이축에 평행해 화면상 점이 되는) 뷰에서는 생략.
        //   ShowAllDimensions 직후 호출 → 같은 Review.Measure→2D 파이프라인을 그대로 탄다.
        private const float MarkAngleTol = 1.0f;        // 90° 배수 판정 공차(도)
        private const float MarkJunctionTol = 3.0f;     // 부재 접합 판정 — osnap 끝점 간 3D 거리 임계(mm)

        private void MarkNonRightAngles(List<int> memberIndices, string viewDirection)
        {
            if (memberIndices == null || memberIndices.Count < 2) return;
            if (string.IsNullOrEmpty(viewDirection) || viewDirection == "ISO") return;

            // 방향벡터를 뷰 평면 2축으로 투영 (깊이축 버림)
            (float h, float v) ProjDir(VIZCore3D.NET.Data.Vertex3D d)
            {
                switch (viewDirection)
                {
                    case "X": return (d.Y, d.Z);
                    case "Y": return (d.X, d.Z);
                    default:  return (d.X, d.Y);   // Z
                }
            }
            float Dist(VIZCore3D.NET.Data.Vertex3D p, VIZCore3D.NET.Data.Vertex3D q)
            {
                float dx = p.X - q.X, dy = p.Y - q.Y, dz = p.Z - q.Z;
                return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            // ── 1. 부재별 점군·중심·길이축 사전계산 (판형 부재는 길이방향 모호 → 제외) ──
            var members = new List<(int idx, int partIdx, List<VIZCore3D.NET.Data.Vertex3D> pts,
                VIZCore3D.NET.Data.Vertex3D centroid, VIZCore3D.NET.Data.Vertex3D dir)>();
            foreach (int idx in memberIndices)
            {
                if (IsPadOrPlateFromSpref(idx)) continue;
                int partIdx = bodyToPartIndexMap.ContainsKey(idx) ? bodyToPartIndexMap[idx] : -1;
                var osnaps = vizcore3d.Object3D.GetOsnapPoint(idx);
                if (osnaps == null) continue;
                var pts = new List<VIZCore3D.NET.Data.Vertex3D>();
                foreach (var o in osnaps)
                {
                    if (o.Kind == VIZCore3D.NET.Data.OsnapKind.LINE)
                    {
                        if (o.Start != null) pts.Add(new VIZCore3D.NET.Data.Vertex3D(o.Start.X, o.Start.Y, o.Start.Z));
                        if (o.End != null)   pts.Add(new VIZCore3D.NET.Data.Vertex3D(o.End.X, o.End.Y, o.End.Z));
                    }
                    else if (o.Kind == VIZCore3D.NET.Data.OsnapKind.POINT && o.Center != null)
                        pts.Add(new VIZCore3D.NET.Data.Vertex3D(o.Center.X, o.Center.Y, o.Center.Z));
                }
                if (pts.Count < 2) continue;

                // 길이 체크 + PCA 멱승법 초기값용: 최원점쌍 (이 자체는 길이축이 아님 — 박스형은 대각선)
                int fi = 0, fj = 1; float best = -1f;
                for (int i = 0; i < pts.Count; i++)
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        float d = Dist(pts[i], pts[j]);
                        if (d > best) { best = d; fi = i; fj = j; }
                    }
                if (best <= MarkJunctionTol) continue;   // 너무 짧아 방향 불안정
                float gx = pts[fj].X - pts[fi].X, gy = pts[fj].Y - pts[fi].Y, gz = pts[fj].Z - pts[fi].Z;
                float gl = (float)Math.Sqrt(gx * gx + gy * gy + gz * gz);
                if (gl < 1e-3f) continue;

                float cx = 0, cy = 0, cz = 0;
                foreach (var p in pts) { cx += p.X; cy += p.Y; cz += p.Z; }
                var centroid = new VIZCore3D.NET.Data.Vertex3D(cx / pts.Count, cy / pts.Count, cz / pts.Count);

                // 길이축 = 점군 공분산행렬의 최대 고유벡터 (PCA 주성분) — 분산이 가장 큰 방향.
                //   박스형 부재의 최원점쌍은 대각선이라 길이방향이 아니므로, 멱승법으로 PC1을 구한다.
                double cxx = 0, cyy = 0, czz = 0, cxy = 0, cxz = 0, cyz = 0;
                foreach (var p in pts)
                {
                    double ax = p.X - centroid.X, ay = p.Y - centroid.Y, az = p.Z - centroid.Z;
                    cxx += ax * ax; cyy += ay * ay; czz += az * az;
                    cxy += ax * ay; cxz += ax * az; cyz += ay * az;
                }
                double vx = gx / gl, vy = gy / gl, vz = gz / gl;   // 초기값 = 최원점쌍 방향(PC1 성분 보유)
                for (int it = 0; it < 32; it++)
                {
                    double nx = cxx * vx + cxy * vy + cxz * vz;
                    double ny = cxy * vx + cyy * vy + cyz * vz;
                    double nz = cxz * vx + cyz * vy + czz * vz;
                    double nn = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (nn < 1e-12) break;
                    vx = nx / nn; vy = ny / nn; vz = nz / nn;
                }
                var dir = new VIZCore3D.NET.Data.Vertex3D((float)vx, (float)vy, (float)vz);

                members.Add((idx, partIdx, pts, centroid, dir));
                DiagLog($"[각도축] 부재 {idx} part{partIdx} 점{pts.Count} PCA길이축=({dir.X:F2},{dir.Y:F2},{dir.Z:F2}) 최원거리={best:F1}");
            }
            if (members.Count < 2) return;

            // Clash(간섭검사) 인접 — 실제 형상 표면이 닿는 부재쌍(면접합 포함). osnap 끝점 거리와 무관.
            //   구조 부재는 중심선이 아닌 면끼리 붙어 끝점 osnap이 부재 폭만큼 벌어지므로, 표면 접촉 판정이 필요.
            var clashPairs = new HashSet<long>();
            foreach (var c in clashList)
            {
                int p1 = Math.Min(c.Index1, c.Index2), p2 = Math.Max(c.Index1, c.Index2);
                clashPairs.Add(((long)p1 << 32) | (uint)p2);
            }
            bool ClashAdj(int pa, int pb)
            {
                if (pa < 0 || pb < 0) return false;
                if (pa == pb) return true;                 // 같은 part의 다른 body → 연결
                int lo = Math.Min(pa, pb), hi = Math.Max(pa, pb);
                return clashPairs.Contains(((long)lo << 32) | (uint)hi);
            }
            if (clashList.Count == 0) DiagLog("[각도] WARN clashList 비어있음 — osnap 근접(3mm)만으로 접합 판정");

            // 접합점 기준 길이축을 '부재 본체(중심)' 쪽으로 정렬
            VIZCore3D.NET.Data.Vertex3D Orient(VIZCore3D.NET.Data.Vertex3D dir,
                VIZCore3D.NET.Data.Vertex3D centroid, VIZCore3D.NET.Data.Vertex3D j)
            {
                float dot = dir.X * (centroid.X - j.X) + dir.Y * (centroid.Y - j.Y) + dir.Z * (centroid.Z - j.Z);
                return dot < 0 ? new VIZCore3D.NET.Data.Vertex3D(-dir.X, -dir.Y, -dir.Z) : dir;
            }

            // ── 2. 부재쌍 → 접합 판정 → 실제 3D 사잇각 → 90배수 제외 → 마킹 ──
            VIZCore3D.NET.Data.MeasureStyle angStyle = vizcore3d.Review.Measure.GetStyle();
            angStyle.NumberOfDecimalPlaces = 0;
            angStyle.FontColor = System.Drawing.Color.Blue;
            angStyle.LineColor = System.Drawing.Color.Blue;
            angStyle.ArrowColor = System.Drawing.Color.Blue;

            int marked = 0, pairsConnected = 0;
            for (int a = 0; a < members.Count; a++)
            {
                for (int b = a + 1; b < members.Count; b++)
                {
                    var A = members[a]; var B = members[b];

                    // 접합점: 두 부재 osnap 점 최근접쌍 (≤ MarkJunctionTol)
                    float bestD = float.MaxValue;
                    VIZCore3D.NET.Data.Vertex3D pA = null, pB = null;
                    foreach (var p in A.pts)
                        foreach (var q in B.pts)
                        {
                            float d = Dist(p, q);
                            if (d < bestD) { bestD = d; pA = p; pB = q; }
                        }
                    // 연결 판정: Clash 표면접촉(면접합) 또는 osnap 끝점 근접(노드 일치)
                    bool isClash = ClashAdj(A.partIdx, B.partIdx);
                    bool connected = isClash || bestD <= MarkJunctionTol;
                    if (!connected) continue;                // 형상도 안 닿고 끝점도 멀면 접합 아님
                    pairsConnected++;
                    var junction = new VIZCore3D.NET.Data.Vertex3D(
                        (pA.X + pB.X) / 2f, (pA.Y + pB.Y) / 2f, (pA.Z + pB.Z) / 2f);

                    var dirA = Orient(A.dir, A.centroid, junction);
                    var dirB = Orient(B.dir, B.centroid, junction);

                    // 실제 3D 사잇각 (단위벡터 내적) — '진짜 직각으로 만나는지' 판정 기준
                    float dot3 = Math.Max(-1f, Math.Min(1f, dirA.X * dirB.X + dirA.Y * dirB.Y + dirA.Z * dirB.Z));
                    float theta = (float)(Math.Acos(dot3) * 180.0 / Math.PI);
                    float mm = theta % 90f;
                    bool isRight = (mm < MarkAngleTol || (90f - mm) < MarkAngleTol);
                    DiagLog($"[각도] view={viewDirection} {A.idx}×{B.idx} clash={isClash} 접합거리={bestD:F1} 3D각={theta:F1} 직각배수={isRight}");
                    if (isRight) continue;                    // 수직·수평(90배수) 제외

                    // 이 뷰에서 그릴 수 있나 — 두 축 모두 화면 평면에 충분히 투영돼야 (깊이축 평행 생략)
                    var (ah, av) = ProjDir(dirA);
                    var (bh, bv) = ProjDir(dirB);
                    if ((float)Math.Sqrt(ah * ah + av * av) < 0.05f) continue;
                    if ((float)Math.Sqrt(bh * bh + bv * bv) < 0.05f) continue;

                    // 마킹: 접합점 + 각 부재 본체 쪽으로 뻗은 가상점 (원본 3D, SDK가 카메라 투영)
                    float reachA = Math.Max(MarkJunctionTol * 3f, Dist(A.centroid, junction));
                    float reachB = Math.Max(MarkJunctionTol * 3f, Dist(B.centroid, junction));
                    var p1 = new VIZCore3D.NET.Data.Vertex3D(
                        junction.X + dirA.X * reachA, junction.Y + dirA.Y * reachA, junction.Z + dirA.Z * reachA);
                    var p2 = new VIZCore3D.NET.Data.Vertex3D(
                        junction.X + dirB.X * reachB, junction.Y + dirB.Y * reachB, junction.Z + dirB.Z * reachB);
                    int angId = vizcore3d.Review.Measure.AddCustom3PointAngle(junction, p1, p2);
                    if (angId >= 0) vizcore3d.Review.Measure.SetStyle(angId, angStyle);

                    // 실측 비교용: 3D각 vs 화면 투영각
                    float la = (float)Math.Sqrt(ah * ah + av * av), lb = (float)Math.Sqrt(bh * bh + bv * bv);
                    float dotp = Math.Max(-1f, Math.Min(1f, (ah * bh + av * bv) / (la * lb)));
                    float projTheta = (float)(Math.Acos(dotp) * 180.0 / Math.PI);
                    DiagLog($"[각도] view={viewDirection} 부재 {A.idx}×{B.idx} 접합거리={bestD:F2} 3D각={theta:F1} 투영각={projTheta:F1}");
                    marked++;
                }
            }
            DiagLog($"[각도] view={viewDirection} 부재={members.Count} 접합쌍={pairsConnected} 마킹={marked}");
        }

    }
}
