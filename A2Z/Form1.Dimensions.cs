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
                measureStyle.AlignDistanceTextPosition = 0;
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
        /// 각 부재에 원형 숫자 풍선(Balloon) 표시
        /// 부재별 겹치지 않는 위치에 번호가 있는 원형 마커를 배치
        /// viewDirection: "X", "Y", "Z", "ISO"
        /// </summary>
        /// <summary>
        /// 현재 풍선 표시 대상 부재 인덱스 (풍선 조정 다이얼로그 재호출 시 사용)
        /// </summary>

        private void ShowBalloonNumbers(string viewDirection, List<int> sheetMemberIndices = null)
        {
            vizcore3d.Review.Note.Clear();
            if (bomList == null || bomList.Count == 0) return;

            // 시트 부재 인덱스 저장 (풍선 조정 다이얼로그 재호출용)
            currentBalloonMemberIndices = sheetMemberIndices;

            // BOM정보가 미수집이면 자동 수집 (풍선 번호를 BOM No.와 일치시키기 위해)
            if (bomInfoNodeGroupMap.Count == 0)
            {
                CollectBOMInfo(false);
            }

            // 뷰 방향이 변경되면 수동 오버라이드 초기화
            if (currentBalloonView != viewDirection)
            {
                balloonOverrides.Clear();
                currentBalloonView = viewDirection;
            }

            try
            {
                // ===== 1. 전체 모델 바운딩박스 계산 =====
                float gMinX = float.MaxValue, gMinY = float.MaxValue, gMinZ = float.MaxValue;
                float gMaxX = float.MinValue, gMaxY = float.MinValue, gMaxZ = float.MinValue;
                foreach (var b in bomList)
                {
                    gMinX = Math.Min(gMinX, b.MinX); gMinY = Math.Min(gMinY, b.MinY); gMinZ = Math.Min(gMinZ, b.MinZ);
                    gMaxX = Math.Max(gMaxX, b.MaxX); gMaxY = Math.Max(gMaxY, b.MaxY); gMaxZ = Math.Max(gMaxZ, b.MaxZ);
                }
                float gcX = (gMinX + gMaxX) / 2f, gcY = (gMinY + gMaxY) / 2f, gcZ = (gMinZ + gMaxZ) / 2f;
                float sizeX = Math.Max(gMaxX - gMinX, 1f);
                float sizeY = Math.Max(gMaxY - gMinY, 1f);
                float sizeZ = Math.Max(gMaxZ - gMinZ, 1f);

                // ===== 2. 뷰 방향에 따른 투영 축 결정 =====
                int hAxis, vAxis, dAxis;
                switch (viewDirection)
                {
                    case "X": hAxis = 1; vAxis = 2; dAxis = 0; break;
                    case "Y": hAxis = 0; vAxis = 2; dAxis = 1; break;
                    default:  hAxis = 0; vAxis = 1; dAxis = 2; break;
                }

                Func<BOMData, int, float> getCenter = (b, ax) =>
                    ax == 0 ? b.CenterX : (ax == 1 ? b.CenterY : b.CenterZ);
                Func<BOMData, int, float> getMin = (b, ax) =>
                    ax == 0 ? b.MinX : (ax == 1 ? b.MinY : b.MinZ);
                Func<BOMData, int, float> getMax = (b, ax) =>
                    ax == 0 ? b.MaxX : (ax == 1 ? b.MaxY : b.MaxZ);

                float[] gCenter = { gcX, gcY, gcZ };
                float[] gSizeArr = { sizeX, sizeY, sizeZ };

                float modelSizeH = gSizeArr[hAxis];
                float modelSizeV = gSizeArr[vAxis];
                float modelCenterH = gCenter[hAxis];
                float modelCenterV = gCenter[vAxis];
                float modelDiag = (float)Math.Sqrt(modelSizeH * modelSizeH + modelSizeV * modelSizeV);

                // ===== 3. 풍선 배치 파라미터 =====
                float minBalloonDist = Math.Max(modelDiag * 0.04f, 20f);

                List<float[]> placed = new List<float[]>();

                // ===== 4. 표시할 풍선 결정 (시트 포함부재만, BOM No. 순번 사용) =====
                // balloonDisplayNumbers: key=bomList인덱스, value=BOM No. 순번
                var balloonDisplayNumbers = new Dictionary<int, int>();
                if (sheetMemberIndices != null && sheetMemberIndices.Count > 0)
                {
                    // 시트 포함부재 인덱스로 정확히 필터링
                    var sheetMemberSet = new HashSet<int>(sheetMemberIndices);
                    for (int i = 0; i < bomList.Count; i++)
                    {
                        int nodeIdx = bomList[i].Index;
                        if (sheetMemberSet.Contains(nodeIdx))
                        {
                            balloonDisplayNumbers[i] = i + 1; // BOM No. 순번 사용
                        }
                    }
                }
                else
                {
                    // 시트 미선택: 전체 부재에 개별 순번
                    for (int i = 0; i < bomList.Count; i++)
                    {
                        balloonDisplayNumbers[i] = i + 1;
                    }
                }

                // ===== 4-1. lvBOM "No." 칼럼은 변경하지 않음 (원래 순번 유지) =====

                // ===== 5. 각 부재별 풍선 배치 =====
                for (int i = 0; i < bomList.Count; i++)
                {
                    // 대표 아닌 부재는 스킵
                    if (!balloonDisplayNumbers.ContainsKey(i)) continue;

                    var bom = bomList[i];
                    float bx, by, bz;

                    // 수동 오버라이드가 있으면 그 위치 사용
                    if (balloonOverrides.ContainsKey(i))
                    {
                        bx = balloonOverrides[i][0];
                        by = balloonOverrides[i][1];
                        bz = balloonOverrides[i][2];
                    }
                    else if (viewDirection == "ISO")
                    {
                        // === ISO 뷰: 부재 중심→모델 중심 반대 방향으로, 부재 바로 옆에 배치 ===
                        float d3x = bom.CenterX - gcX;
                        float d3y = bom.CenterY - gcY;
                        float d3z = bom.CenterZ - gcZ;
                        float d3len = (float)Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);

                        if (d3len < 0.001f)
                        {
                            float angle = (float)(i * 2 * Math.PI / bomList.Count);
                            d3x = (float)Math.Cos(angle);
                            d3y = (float)Math.Sin(angle);
                            d3z = 0.3f;
                            d3len = (float)Math.Sqrt(d3x * d3x + d3y * d3y + d3z * d3z);
                        }
                        d3x /= d3len; d3y /= d3len; d3z /= d3len;

                        // 부재 가장자리까지 거리 + 개별 부재 크기 기반 오프셋
                        float mHalfX = (bom.MaxX - bom.MinX) / 2f;
                        float mHalfY = (bom.MaxY - bom.MinY) / 2f;
                        float mHalfZ = (bom.MaxZ - bom.MinZ) / 2f;
                        float memberDiag = (float)Math.Sqrt(mHalfX * mHalfX + mHalfY * mHalfY + mHalfZ * mHalfZ);
                        float memberOffset = Math.Max(memberDiag * 0.5f, 25f);
                        float edgeDist = Math.Abs(d3x) * mHalfX + Math.Abs(d3y) * mHalfY + Math.Abs(d3z) * mHalfZ;
                        float totalDist = edgeDist + memberOffset;

                        bx = bom.CenterX + d3x * totalDist;
                        by = bom.CenterY + d3y * totalDist;
                        bz = bom.CenterZ + d3z * totalDist;

                        // 충돌 검사 (다른 풍선 + 다른 부재 바운딩박스, 자기 자신 제외)
                        bool positionFound = false;
                        for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                        {
                            bool collision = false;

                            foreach (var pp in placed)
                            {
                                float dx = bx - pp[0], dy = by - pp[1], dz = bz - pp[2];
                                if (Math.Sqrt(dx * dx + dy * dy + dz * dz) < minBalloonDist)
                                { collision = true; break; }
                            }

                            if (!collision)
                            {
                                float pad = minBalloonDist * 0.3f;
                                foreach (var otherBom in bomList)
                                {
                                    if (otherBom == bom) continue; // 자기 자신 부재는 충돌 제외
                                    if (bx >= otherBom.MinX - pad && bx <= otherBom.MaxX + pad &&
                                        by >= otherBom.MinY - pad && by <= otherBom.MaxY + pad &&
                                        bz >= otherBom.MinZ - pad && bz <= otherBom.MaxZ + pad)
                                    { collision = true; break; }
                                }
                            }

                            if (!collision)
                            {
                                positionFound = true;
                            }
                            else
                            {
                                float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                                if (attempt % 2 == 1) rotAngle = -rotAngle;
                                float cosA = (float)Math.Cos(rotAngle);
                                float sinA = (float)Math.Sin(rotAngle);

                                // XY 평면에서 회전
                                float newDx = cosA * d3x - sinA * d3y;
                                float newDy = sinA * d3x + cosA * d3y;
                                float newDist = totalDist * (1f + (attempt / 6) * 0.1f);

                                bx = bom.CenterX + newDx * newDist;
                                by = bom.CenterY + newDy * newDist;
                                bz = bom.CenterZ + d3z * newDist;
                            }
                        }

                        // 자동 계산된 위치 저장
                        balloonOverrides[i] = new float[] { bx, by, bz };
                    }
                    else
                    {
                        // === 정사영 뷰 (X, Y, Z): 2D H-V 평면 기반, 부재 바로 옆에 배치 ===
                        float memberH = getCenter(bom, hAxis);
                        float memberV = getCenter(bom, vAxis);
                        float memberD = getCenter(bom, dAxis);
                        float memberHalfH = (getMax(bom, hAxis) - getMin(bom, hAxis)) / 2f;
                        float memberHalfV = (getMax(bom, vAxis) - getMin(bom, vAxis)) / 2f;

                        float dirH = memberH - modelCenterH;
                        float dirV = memberV - modelCenterV;
                        float dirLen = (float)Math.Sqrt(dirH * dirH + dirV * dirV);

                        if (dirLen < 0.001f)
                        {
                            float defaultAngle = (float)(i * 2 * Math.PI / bomList.Count);
                            dirH = (float)Math.Cos(defaultAngle);
                            dirV = (float)Math.Sin(defaultAngle);
                            dirLen = 1f;
                        }
                        dirH /= dirLen;
                        dirV /= dirLen;

                        // 개별 부재 크기 기반 오프셋
                        float memberDiag2D = (float)Math.Sqrt(memberHalfH * memberHalfH + memberHalfV * memberHalfV);
                        float memberOffset2D = Math.Max(memberDiag2D * 0.5f, 25f);
                        float edgeDist = Math.Abs(dirH) * memberHalfH + Math.Abs(dirV) * memberHalfV;
                        float totalOffset = edgeDist + memberOffset2D;
                        float bestH = memberH + dirH * totalOffset;
                        float bestV = memberV + dirV * totalOffset;

                        bool positionFound = false;
                        for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                        {
                            bool collision = false;
                            foreach (var pp in placed)
                            {
                                float dh = bestH - pp[0];
                                float dv = bestV - pp[1];
                                if (Math.Sqrt(dh * dh + dv * dv) < minBalloonDist)
                                { collision = true; break; }
                            }

                            if (!collision)
                            {
                                float pad = minBalloonDist * 0.3f;
                                foreach (var otherBom in bomList)
                                {
                                    if (otherBom == bom) continue; // 자기 자신 부재는 충돌 제외
                                    float oMinH = getMin(otherBom, hAxis) - pad;
                                    float oMaxH = getMax(otherBom, hAxis) + pad;
                                    float oMinV = getMin(otherBom, vAxis) - pad;
                                    float oMaxV = getMax(otherBom, vAxis) + pad;
                                    if (bestH >= oMinH && bestH <= oMaxH && bestV >= oMinV && bestV <= oMaxV)
                                    { collision = true; break; }
                                }
                            }

                            if (!collision)
                            {
                                positionFound = true;
                            }
                            else
                            {
                                float rotAngle = (float)((attempt / 2 + 1) * 10 * Math.PI / 180);
                                if (attempt % 2 == 1) rotAngle = -rotAngle;
                                float cosA = (float)Math.Cos(rotAngle);
                                float sinA = (float)Math.Sin(rotAngle);
                                float newDirH = cosA * dirH - sinA * dirV;
                                float newDirV = sinA * dirH + cosA * dirV;
                                float newOffset = totalOffset * (1f + (attempt / 6) * 0.1f);
                                bestH = memberH + newDirH * newOffset;
                                bestV = memberV + newDirV * newOffset;
                            }
                        }

                        float[] xyz = new float[3];
                        xyz[hAxis] = bestH;
                        xyz[vAxis] = bestV;
                        xyz[dAxis] = memberD;
                        bx = xyz[0]; by = xyz[1]; bz = xyz[2];

                        balloonOverrides[i] = new float[] { bx, by, bz };
                    }

                    placed.Add(new float[] { bx, by, bz });

                    VIZCore3D.NET.Data.Vertex3D balloonPos = new VIZCore3D.NET.Data.Vertex3D(bx, by, bz);

                    // 풍선 방향의 부재 바운딩박스 표면 점 계산 (부재에서 시작)
                    float anchorX = Math.Max(bom.MinX, Math.Min(bx, bom.MaxX));
                    float anchorY = Math.Max(bom.MinY, Math.Min(by, bom.MaxY));
                    float anchorZ = Math.Max(bom.MinZ, Math.Min(bz, bom.MaxZ));
                    VIZCore3D.NET.Data.Vertex3D memberAnchor = new VIZCore3D.NET.Data.Vertex3D(anchorX, anchorY, anchorZ);

                    VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                    style.UseSymbol = true;
                    style.SymbolText = balloonDisplayNumbers[i].ToString();
                    style.SymbolBackgroundColor = Color.Transparent;
                    style.SymbolFontColor = Color.Black;
                    style.SymbolFontBold = true;
                    style.SymbolSize = 8;
                    style.LineColor = Color.FromArgb(80, 80, 80);
                    style.LineWidth = 1;
                    style.ArrowColor = Color.FromArgb(80, 80, 80);
                    style.ArrowWidth = 5;
                    style.BackgroudTransparent = true;
                    style.FontBold = false;

                    vizcore3d.Review.Note.AddNoteSurface(" ", memberAnchor, balloonPos, style);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"풍선 번호 표시 오류: {ex.Message}");
            }
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
                ShowBalloonNumbers(currentBalloonView, currentBalloonMemberIndices);
                updateCurrentPos();
            };

            // 초기화 버튼
            Button btnReset = new Button { Text = "전체 초기화", Location = new Point(125, 240), Size = new Size(100, 35) };
            btnReset.Click += (s2, e2) =>
            {
                balloonOverrides.Clear();
                string savedView = currentBalloonView;
                currentBalloonView = ""; // 강제 재계산
                ShowBalloonNumbers(savedView, currentBalloonMemberIndices);
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
        private List<int> ShowAllDimensions(string viewDirection = null, bool forDrawing2D = false)
        {
            // 표시할 치수 필터링
            List<ChainDimensionData> displayList;
            bool useDirectChain = false; // Osnap 재추출 모드 (순차 체인 + 스마트 필터링 혼합)
            bool isInstallationMode = false; // 설치도 모드 (필터링 우회)
            if (viewDirection != null && osnapPointsWithNames != null && osnapPointsWithNames.Count > 0)
            {
                // 뷰 방향에 따라 Osnap 필터링을 재적용하여 체인치수 재추출
                // 왼쪽아래 Osnap부터 순차적으로 다음 Osnap까지 체인치수 표시
                float tolerance = 0.5f;
                var mergedPoints = MergeCoordinates(osnapPointsWithNames, tolerance);
                displayList = new List<ChainDimensionData>();

                List<string> visibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": visibleAxes.Add("Y"); visibleAxes.Add("Z"); break;
                    case "Y": visibleAxes.Add("X"); visibleAxes.Add("Z"); break;
                    case "Z": visibleAxes.Add("X"); visibleAxes.Add("Y"); break;
                }

                foreach (var axis in visibleAxes)
                {
                    displayList.AddRange(AddChainDimensionByAxis(mergedPoints, axis, tolerance, viewDirection));
                }
                useDirectChain = true; // Osnap 재추출 모드: 순차 체인 + 스마트 필터링
            }
            else if (viewDirection != null && chainDimensionList != null && chainDimensionList.Count > 0)
            {
                // 설치도 모드: 치수추출과 동일한 방식 (Osnap 수집 → MergeCoordinates → AddChainDimensionByAxis)
                List<string> visibleAxes = new List<string>();
                switch (viewDirection)
                {
                    case "X": visibleAxes.Add("Y"); visibleAxes.Add("Z"); break;
                    case "Y": visibleAxes.Add("X"); visibleAxes.Add("Z"); break;
                    case "Z": visibleAxes.Add("X"); visibleAxes.Add("Y"); break;
                }

                // 선택된 부재들의 Osnap 수집 (노드별 그룹 → 끝단만 유지)
                var nodeOsnapMap = new Dictionary<int, List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>>();
                if (xraySelectedNodeIndices != null && xraySelectedNodeIndices.Count > 0)
                {
                    List<VIZCore3D.NET.Data.Node> allBodyNodes = vizcore3d.Object3D.GetPartialNode(false, false, true);
                    if (allBodyNodes != null)
                    {
                        HashSet<int> selectedSet = new HashSet<int>(xraySelectedNodeIndices);
                        var bodyNodes = allBodyNodes.Where(n => selectedSet.Contains(n.Index)).ToList();
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

                // 축별 부재당 대표 Osnap 1개 선택 + 전체 Min/Max 보존
                // 수직축(Z→X/Y뷰, Y→Z뷰) → 위쪽(max) 선택
                // 수평축(Y→X뷰, X→Y/Z뷰) → 왼쪽(min) 선택
                int totalOsnapCount = nodeOsnapMap.Values.Sum(p => p.Count);
                if (totalOsnapCount >= 2)
                {
                    displayList = new List<ChainDimensionData>();
                    isInstallationMode = true;
                    float tolerance = 0.5f;

                    foreach (var axis in visibleAxes)
                    {
                        // 축별 값 추출 함수
                        Func<VIZCore3D.NET.Data.Vertex3D, float> getVal;
                        switch (axis)
                        {
                            case "X": getVal = p => p.X; break;
                            case "Y": getVal = p => p.Y; break;
                            default: getVal = p => p.Z; break;
                        }

                        // 위쪽(max) / 왼쪽(min) 결정
                        // X뷰: Z=수직→위쪽(max), Y=수평→왼쪽(min)
                        // Y뷰: Z=수직→위쪽(max), X=수평→왼쪽(min)
                        // Z뷰: Y=수직→위쪽(max), X=수평→왼쪽(min)
                        bool keepMax;
                        switch (viewDirection)
                        {
                            case "X": keepMax = (axis == "Z"); break;
                            case "Y": keepMax = (axis == "Z"); break;
                            case "Z": keepMax = (axis == "Y"); break;
                            default: keepMax = false; break;
                        }

                        var axisPoints = new List<(VIZCore3D.NET.Data.Vertex3D point, string nodeName)>();
                        float gMin = float.MaxValue, gMax = float.MinValue;
                        (VIZCore3D.NET.Data.Vertex3D point, string nodeName) gMinPt = (null, null);
                        (VIZCore3D.NET.Data.Vertex3D point, string nodeName) gMaxPt = (null, null);

                        foreach (var kvp in nodeOsnapMap)
                        {
                            var pts = kvp.Value;
                            if (pts.Count == 0) continue;

                            // 부재별 대표 Osnap 1개 선택 (위쪽=max / 왼쪽=min)
                            int bestIdx = 0;
                            float bestVal = keepMax ? float.MinValue : float.MaxValue;
                            for (int i = 0; i < pts.Count; i++)
                            {
                                float v = getVal(pts[i].point);
                                if (keepMax ? (v > bestVal) : (v < bestVal))
                                {
                                    bestVal = v;
                                    bestIdx = i;
                                }
                            }
                            axisPoints.Add(pts[bestIdx]);

                            // 전체 Min/Max 추적 (전체거리 체인치수 보존용)
                            for (int i = 0; i < pts.Count; i++)
                            {
                                float v = getVal(pts[i].point);
                                if (v < gMin) { gMin = v; gMinPt = pts[i]; }
                                if (v > gMax) { gMax = v; gMaxPt = pts[i]; }
                            }
                        }

                        // 전체 Min 포인트 추가 (대표 포인트와 중복 아닌 경우)
                        if (gMinPt.point != null)
                        {
                            float minV = getVal(gMinPt.point);
                            bool exists = axisPoints.Any(p => Math.Abs(getVal(p.point) - minV) < tolerance);
                            if (!exists) axisPoints.Add(gMinPt);
                        }
                        // 전체 Max 포인트 추가 (대표 포인트와 중복 아닌 경우)
                        if (gMaxPt.point != null)
                        {
                            float maxV = getVal(gMaxPt.point);
                            bool exists = axisPoints.Any(p => Math.Abs(getVal(p.point) - maxV) < tolerance);
                            if (!exists) axisPoints.Add(gMaxPt);
                        }

                        var mergedPoints = MergeCoordinates(axisPoints, tolerance);
                        displayList.AddRange(AddChainDimensionByAxis(mergedPoints, axis, tolerance, viewDirection));
                    }
                }
                else
                {
                    // Osnap 부족: 기존 chainDimensionList에서 해당 축만 필터링
                    displayList = chainDimensionList.Where(d => visibleAxes.Contains(d.Axis)).ToList();
                }
            }
            else if (chainDimensionList != null && chainDimensionList.Count > 0)
            {
                displayList = chainDimensionList;
            }
            else
            {
                return new List<int>();
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

                // ========== Dimension Filtering (순차 체인 + 스마트 필터링 혼합) ==========
                List<ChainDimensionData> filteredDims;
                if (isInstallationMode)
                {
                    // 설치도 모드: 필터링 우회 (이미 필요한 치수만 구성됨)
                    filteredDims = displayList;
                }
                else if (useDirectChain)
                {
                    // Osnap 재추출 순차 체인 + 스마트 필터링 혼합
                    // 순차 체인에서 추출된 치수 중 필요한 치수만 선택
                    filteredDims = ApplySmartFiltering(displayList, maxDimensionsPerAxis: 12, minTextSpace: 15.0f);
                }
                else
                {
                    // 일반 모드: 스마트 필터링 적용
                    filteredDims = ApplySmartFiltering(displayList, maxDimensionsPerAxis: 8, minTextSpace: 25.0f);
                }

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
                measureStyle.AlignDistanceTextPosition = 0;
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

                // 오프셋 (고정값)
                float baseOffset = 100.0f;
                float levelSpacing = 60.0f;

                List<VIZCore3D.NET.Data.Vertex3DItemCollection> extensionLines = new List<VIZCore3D.NET.Data.Vertex3DItemCollection>();

                // ========== 축별 체인치수 방향 결정 (중심에서 체인치수 위치 방향) ==========
                Dictionary<string, bool> axisPositiveOffset = new Dictionary<string, bool>();
                if (viewDirection != null)
                {
                    var axisGroups = filteredDims.Where(d => !d.IsTotal).GroupBy(d => d.Axis);
                    foreach (var grp in axisGroups)
                    {
                        string dimAxis = grp.Key;
                        string offsetAxis = GetRemainingAxis(viewDirection, dimAxis);

                        // 체인 포인트들의 오프셋축 평균값 계산
                        float sumOffsetVal = 0;
                        int count = 0;
                        foreach (var dim in grp)
                        {
                            sumOffsetVal += GetAxisValue(dim.StartPoint, offsetAxis);
                            sumOffsetVal += GetAxisValue(dim.EndPoint, offsetAxis);
                            count += 2;
                        }
                        float avgOffsetVal = count > 0 ? sumOffsetVal / count : 0;

                        // 모델 중심과 비교하여 방향 결정
                        float modelCenterOffset = 0;
                        switch (offsetAxis)
                        {
                            case "X": modelCenterOffset = modelCenterX; break;
                            case "Y": modelCenterOffset = modelCenterY; break;
                            case "Z": modelCenterOffset = modelCenterZ; break;
                        }
                        axisPositiveOffset[dimAxis] = avgOffsetVal >= modelCenterOffset;
                    }
                }

                // ========== Level-Based Layout ==========
                var level0Dims = filteredDims.Where(d => d.IsTotal && d.IsVisible).ToList();
                var level1Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel == 0).ToList();
                var level2Dims = filteredDims.Where(d => !d.IsTotal && d.IsVisible && d.DisplayLevel > 0).ToList();

                // 설치도 모드: Osnap 체인치수 100mm, 전체 길이 150mm 고정
                // 일반 모드: baseOffset(100) + levelSpacing(60) 기반
                float level1Offset = isInstallationMode ? 100.0f : baseOffset;
                float level2Offset = isInstallationMode ? 100.0f : baseOffset + levelSpacing;

                // Level 1 치수 (가장 안쪽 - Osnap 간 체인치수)
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
                float level0Offset = isInstallationMode ? 150.0f : baseOffset + (levelSpacing * maxLevelUsed);
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

                // --- 풍선 일괄 배치 (부재/홀 바로 옆에 배치, 겹침 방지) ---
                try
                {
                    Func<float, float, float, int, float> getComp = (x, y, z, axis) =>
                        axis == 0 ? x : (axis == 1 ? y : z);

                    // 모델 중심 (풍선 방향 결정용)
                    float[] modelCenterArr = { modelCenterX, modelCenterY, modelCenterZ };
                    float modelCenterH = modelCenterArr[bHAxis];
                    float modelCenterV = modelCenterArr[bVAxis];

                    float balloonOffset = 50f; // 부재 근처 초기 오프셋
                    float bomPad = 5f; // 부재 바운딩박스 패딩

                    // 부재 바운딩박스를 2D(H,V) 기준으로 미리 계산
                    Func<BOMData, int, float> getBomMin = (b, ax) =>
                        ax == 0 ? b.MinX : (ax == 1 ? b.MinY : b.MinZ);
                    Func<BOMData, int, float> getBomMax = (b, ax) =>
                        ax == 0 ? b.MaxX : (ax == 1 ? b.MaxY : b.MaxZ);

                    // 텍스트 크기 추정 (SIZE12 폰트 기준, mm 단위)
                    Func<string, (float w, float h)> estimateTextSize = (text) =>
                    {
                        float charWidth = 6f;
                        float lineHeight = 14f;
                        return (text.Length * charWidth, lineHeight);
                    };

                    // 배치된 풍선 텍스트 AABB 목록 (겹침 판정용)
                    List<(float h, float v, float halfW, float halfH)> placedTextBoxes = new List<(float, float, float, float)>();

                    foreach (var entry in balloonEntries)
                    {
                        try
                        {
                            // 기점의 2D 화면좌표 (수평, 수직, 깊이)
                            float originH = getComp(entry.ox, entry.oy, entry.oz, bHAxis);
                            float originV = getComp(entry.ox, entry.oy, entry.oz, bVAxis);
                            float originD = getComp(entry.ox, entry.oy, entry.oz, bDAxis);

                            // 모델 중심 → 홀 위치 방향으로 오프셋
                            float dirH = originH - modelCenterH;
                            float dirV = originV - modelCenterV;
                            float dirLen = (float)Math.Sqrt(dirH * dirH + dirV * dirV);

                            if (dirLen < 0.001f)
                            {
                                dirH = 1f; dirV = 0f; dirLen = 1f;
                            }
                            dirH /= dirLen;
                            dirV /= dirLen;

                            float candidateH = originH + dirH * balloonOffset;
                            float candidateV = originV + dirV * balloonOffset;

                            // 겹침 방지: 다른 풍선 + 부재 바운딩박스(2D 투영)와 겹치면 회전
                            bool positionFound = false;
                            for (int attempt = 0; attempt < 36 && !positionFound; attempt++)
                            {
                                bool collision = false;

                                // 다른 풍선과 AABB 겹침 검사
                                var candSize = estimateTextSize(entry.text);
                                float candHalfW = candSize.w / 2f;
                                float candHalfH = candSize.h / 2f;
                                foreach (var placed in placedTextBoxes)
                                {
                                    if (Math.Abs(candidateH - placed.h) < (candHalfW + placed.halfW) &&
                                        Math.Abs(candidateV - placed.v) < (candHalfH + placed.halfH))
                                    { collision = true; break; }
                                }

                                // 부재 바운딩박스(2D 투영)와 겹침 검사
                                if (!collision)
                                {
                                    foreach (var bom in bomList)
                                    {
                                        float bMinH = getBomMin(bom, bHAxis) - bomPad;
                                        float bMaxH = getBomMax(bom, bHAxis) + bomPad;
                                        float bMinV = getBomMin(bom, bVAxis) - bomPad;
                                        float bMaxV = getBomMax(bom, bVAxis) + bomPad;
                                        if (candidateH >= bMinH && candidateH <= bMaxH &&
                                            candidateV >= bMinV && candidateV <= bMaxV)
                                        { collision = true; break; }
                                    }
                                }

                                if (!collision)
                                {
                                    positionFound = true;
                                }
                                else
                                {
                                    float rotAngle = (float)((attempt / 2 + 1) * 15 * Math.PI / 180);
                                    if (attempt % 2 == 1) rotAngle = -rotAngle;
                                    float cosA = (float)Math.Cos(rotAngle);
                                    float sinA = (float)Math.Sin(rotAngle);
                                    float newDirH = cosA * dirH - sinA * dirV;
                                    float newDirV = sinA * dirH + cosA * dirV;
                                    float newOffset = balloonOffset * (1f + (attempt / 4) * 0.15f);
                                    candidateH = originH + newDirH * newOffset;
                                    candidateV = originV + newDirV * newOffset;
                                }
                            }

                            // 3D 좌표로 복원
                            float[] textXYZ = new float[3];
                            textXYZ[bHAxis] = candidateH;
                            textXYZ[bVAxis] = candidateV;
                            textXYZ[bDAxis] = originD;

                            VIZCore3D.NET.Data.Vertex3D arrowPos = new VIZCore3D.NET.Data.Vertex3D(
                                entry.ox, entry.oy, entry.oz);
                            VIZCore3D.NET.Data.Vertex3D textPos = new VIZCore3D.NET.Data.Vertex3D(
                                textXYZ[0], textXYZ[1], textXYZ[2]);

                            VIZCore3D.NET.Data.NoteStyle style = vizcore3d.Review.Note.GetStyle();
                            style.UseSymbol = false;
                            style.BackgroudTransparent = true;
                            style.UseTextBox = false;
                            style.FontBold = true;
                            style.FontSize = VIZCore3D.NET.Data.FontSizeKind.SIZE12;
                            style.FontColor = entry.color;
                            style.LineColor = entry.color;
                            style.LineWidth = 0;
                            style.ArrowColor = entry.color;
                            style.ArrowWidth = 0;

                            // 보조선 없는 풍선 (AddNote3D: 리더선 없이 텍스트만 배치)
                            vizcore3d.Review.Note.AddNote3D(entry.text, textXYZ[0], textXYZ[1], textXYZ[2], style);
                            var placedSize = estimateTextSize(entry.text);
                            placedTextBoxes.Add((candidateH, candidateV, placedSize.w / 2f, placedSize.h / 2f));
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
        private void DrawDimension(
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
            bool positiveOffset = false)
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
                    return;
            }

            // 치수 거리 계산
            float distance = 0;
            switch (axis)
            {
                case "X": distance = Math.Abs(endPoint.X - startPoint.X); break;
                case "Y": distance = Math.Abs(endPoint.Y - startPoint.Y); break;
                case "Z": distance = Math.Abs(endPoint.Z - startPoint.Z); break;
            }

            if (distance > 0.1f)
            {
                // === 모든 치수: AddCustomAxisDistance 사용 ===
                switch (axis)
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

            // 보조선 추가 (Osnap 위치 → 치수선 위치)
            // 실제 Osnap 좌표에서 치수선까지 직선으로 연결
            var extLine1 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine1.Add(originalStart);
            extLine1.Add(startVertex);
            extensionLines.Add(extLine1);

            var extLine2 = new VIZCore3D.NET.Data.Vertex3DItemCollection();
            extLine2.Add(originalEnd);
            extLine2.Add(endVertex);
            extensionLines.Add(extLine2);
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

        #endregion

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

                // 관련 Osnap 좌표 자동 선택
                SelectRelatedOsnapItems(relatedNodeNames, relatedBounds);

                // 관련 치수 자동 선택
                SelectRelatedDimensionItems(relatedBounds);
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
            try
            {
                // 현재 X-Ray/보이는 상태에 맞게 Osnap 재수집
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
            }
            catch (Exception ex)
            {
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

    }
}
