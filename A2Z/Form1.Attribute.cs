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
        #region 부재 정보 탭 - 속성 조회 기능

        /// <summary>
        /// 3D 객체 선택 이벤트 핸들러
        /// </summary>
        private void Object3D_OnObject3DSelected(object sender, VIZCore3D.NET.Event.EventManager.Object3DSelectedEventArgs e)
        {
            // 선택된 노드 가져오기
            var selectedNodes = vizcore3d.Object3D.FromFilter(VIZCore3D.NET.Data.Object3dFilter.SELECTED_TOP);

            if (selectedNodes == null || selectedNodes.Count == 0)
            {
                ClearAttributeTable();
                return;
            }

            // 첫 번째 선택된 노드의 속성 표시
            var node = selectedNodes[0];
            selectedAttributeNodeIndex = node.Index;

            // 노드 정보 표시
            lblSelectedNode.Text = $"[{node.Index}] {node.NodeName}";

            // 속성 테이블 업데이트
            UpdateAttributeTable(node.Index);
        }

        /// <summary>
        /// 속성 테이블 업데이트
        /// </summary>
        private void UpdateAttributeTable(int nodeIndex)
        {
            dgvAttributes.Rows.Clear();

            try
            {
                // 1. 기본 노드 정보 추가
                AddBasicNodeInfo(nodeIndex);

                // 2. 바운딩 박스 정보 추가
                AddBoundingBoxInfo(nodeIndex);

                // 3. UDA (User Defined Attributes) 추가
                AddUDAInfo(nodeIndex);

                // 4. 지오메트리 속성 추가
                AddGeometryPropertyInfo(nodeIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"속성 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기본 노드 정보 추가
        /// </summary>
        private void AddBasicNodeInfo(int nodeIndex)
        {
            var node = vizcore3d.Object3D.FromIndex(nodeIndex);
            if (node == null) return;

            int rowNum = 1;

            // 구분선 추가
            AddSectionHeader("기본 정보");

            dgvAttributes.Rows.Add(rowNum++, "Node Index", nodeIndex.ToString());
            dgvAttributes.Rows.Add(rowNum++, "Node Name", node.NodeName);
            dgvAttributes.Rows.Add(rowNum++, "Node Type", node.Kind.ToString());

            // 노드 경로에서 부모 정보 추출 시도
            try
            {
                string nodePath = node.NodeName;
                if (nodePath.Contains("/"))
                {
                    string parentPath = nodePath.Substring(0, nodePath.LastIndexOf("/"));
                    dgvAttributes.Rows.Add(rowNum++, "Parent Path", parentPath);
                }
            }
            catch { }
        }

        /// <summary>
        /// 바운딩 박스 정보 추가
        /// </summary>
        private void AddBoundingBoxInfo(int nodeIndex)
        {
            List<int> indices = new List<int> { nodeIndex };
            var bbox = vizcore3d.Object3D.GetBoundBox(indices, false);

            if (bbox == null) return;

            AddSectionHeader("바운딩 박스 (Bounding Box)");

            int rowNum = dgvAttributes.Rows.Count + 1;

            dgvAttributes.Rows.Add(rowNum++, "Min X", bbox.MinX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Min Y", bbox.MinY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Min Z", bbox.MinZ.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max X", bbox.MaxX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max Y", bbox.MaxY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Max Z", bbox.MaxZ.ToString("F2"));

            // 크기 계산
            float sizeX = bbox.MaxX - bbox.MinX;
            float sizeY = bbox.MaxY - bbox.MinY;
            float sizeZ = bbox.MaxZ - bbox.MinZ;

            dgvAttributes.Rows.Add(rowNum++, "Size X", sizeX.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Size Y", sizeY.ToString("F2"));
            dgvAttributes.Rows.Add(rowNum++, "Size Z", sizeZ.ToString("F2"));

            // 중심점
            float centerX = (bbox.MinX + bbox.MaxX) / 2;
            float centerY = (bbox.MinY + bbox.MaxY) / 2;
            float centerZ = (bbox.MinZ + bbox.MaxZ) / 2;

            dgvAttributes.Rows.Add(rowNum++, "Center", $"({centerX:F2}, {centerY:F2}, {centerZ:F2})");
        }

        /// <summary>
        /// UDA (User Defined Attributes) 정보 추가
        /// </summary>
        private void AddUDAInfo(int nodeIndex)
        {
            try
            {
                // UDA 키 목록 가져오기
                var udaKeys = vizcore3d.Object3D.UDA.Keys;

                if (udaKeys == null || udaKeys.Count == 0)
                    return;

                AddSectionHeader("사용자 정의 속성 (UDA)");

                int rowNum = dgvAttributes.Rows.Count + 1;

                foreach (string key in udaKeys)
                {
                    try
                    {
                        // 해당 노드의 UDA 값 가져오기
                        var udaData = vizcore3d.Object3D.UDA.FromIndex(nodeIndex, key);

                        if (udaData != null && !string.IsNullOrEmpty(udaData.ToString()))
                        {
                            dgvAttributes.Rows.Add(rowNum++, key, udaData.ToString());
                        }
                    }
                    catch
                    {
                        // 해당 키에 대한 값이 없으면 스킵
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UDA 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 지오메트리 속성 정보 추가
        /// </summary>
        private void AddGeometryPropertyInfo(int nodeIndex)
        {
            try
            {
                // 지오메트리 속성이 있는지 확인
                var geomProps = vizcore3d.Object3D.GeometryProperty;
                if (geomProps == null) return;

                // 노드의 지오메트리 속성 가져오기
                var props = geomProps.FromIndex(nodeIndex);
                if (props == null) return;

                // 속성이 있으면 섹션 추가
                bool hasProps = false;

                // 리플렉션으로 속성 가져오기
                var propsType = props.GetType();
                var properties = propsType.GetProperties();

                foreach (var propInfo in properties)
                {
                    try
                    {
                        var value = propInfo.GetValue(props);
                        if (value != null && !string.IsNullOrEmpty(value.ToString()))
                        {
                            if (!hasProps)
                            {
                                AddSectionHeader("지오메트리 속성 (Geometry)");
                                hasProps = true;
                            }
                            int rowNum = dgvAttributes.Rows.Count + 1;
                            dgvAttributes.Rows.Add(rowNum, propInfo.Name, value.ToString());
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geometry Property 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 섹션 헤더 추가 (구분선)
        /// </summary>
        private void AddSectionHeader(string sectionName)
        {
            int rowIndex = dgvAttributes.Rows.Add("", $"━━ {sectionName} ━━", "");
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230);
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.Font = new Font("맑은 고딕", 9, FontStyle.Bold);
            dgvAttributes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
        }

        /// <summary>
        /// 속성 테이블 초기화
        /// </summary>
        private void ClearAttributeTable()
        {
            dgvAttributes.Rows.Clear();
            lblSelectedNode.Text = "3D 뷰어에서 부재를 선택하세요";
            selectedAttributeNodeIndex = -1;
        }

        /// <summary>
        /// 선택 해제 버튼 클릭
        /// </summary>
        private void btnClearSelection_Click(object sender, EventArgs e)
        {
            vizcore3d.Object3D.Select(new List<int>(), false, false);
            ClearAttributeTable();
        }

        /// <summary>
        /// CSV 내보내기 버튼 클릭
        /// </summary>
        private void btnExportAttributeCSV_Click(object sender, EventArgs e)
        {
            if (dgvAttributes.Rows.Count == 0)
            {
                MessageBox.Show("내보낼 속성이 없습니다.\n부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "CSV 파일 (*.csv)|*.csv";
            dlg.FileName = $"Attributes_{selectedAttributeNodeIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using (var writer = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                {
                    // 헤더
                    writer.WriteLine("No,Key,Value");

                    // 데이터
                    foreach (DataGridViewRow row in dgvAttributes.Rows)
                    {
                        string no = row.Cells["No"].Value?.ToString() ?? "";
                        string key = row.Cells["Key"].Value?.ToString() ?? "";
                        string value = row.Cells["Value"].Value?.ToString() ?? "";

                        // CSV 이스케이프
                        key = key.Contains(",") ? $"\"{key}\"" : key;
                        value = value.Contains(",") ? $"\"{value}\"" : value;

                        writer.WriteLine($"{no},{key},{value}");
                    }
                }

                MessageBox.Show($"CSV 저장 완료:\n{dlg.FileName}", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 저장 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 입력/편집 다이얼로그 표시
        /// </summary>
        private Tuple<string, string> ShowUdaInputDialog(string title, string defaultKey, string defaultValue, bool keyReadOnly = false)
        {
            Form dlg = new Form();
            dlg.Text = title;
            dlg.Size = new Size(350, 180);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            Label lblKey = new Label() { Text = "Key:", Location = new Point(15, 20), Size = new Size(50, 20) };
            TextBox txtKey = new TextBox() { Location = new Point(70, 17), Size = new Size(245, 22), Text = defaultKey, ReadOnly = keyReadOnly };
            Label lblValue = new Label() { Text = "Value:", Location = new Point(15, 55), Size = new Size(50, 20) };
            TextBox txtValue = new TextBox() { Location = new Point(70, 52), Size = new Size(245, 22), Text = defaultValue };

            Button btnOk = new Button() { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(155, 95), Size = new Size(75, 28) };
            Button btnCancel = new Button() { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(240, 95), Size = new Size(75, 28) };

            dlg.Controls.AddRange(new Control[] { lblKey, txtKey, lblValue, txtValue, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string key = txtKey.Text.Trim();
                string value = txtValue.Text.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("Key를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                return Tuple.Create(key, value);
            }
            return null;
        }

        /// <summary>
        /// 선택된 행이 UDA 섹션의 데이터 행인지 판별
        /// </summary>
        private bool IsUdaRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvAttributes.Rows.Count)
                return false;

            // 위로 탐색하여 가장 가까운 섹션 헤더 찾기
            for (int i = rowIndex - 1; i >= 0; i--)
            {
                string keyVal = dgvAttributes.Rows[i].Cells["Key"].Value?.ToString() ?? "";
                if (keyVal.Contains("━━"))
                {
                    return keyVal.Contains("사용자 정의 속성 (UDA)");
                }
            }
            return false;
        }

        /// <summary>
        /// UDA 추가 버튼 클릭
        /// </summary>
        private void btnUdaAdd_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = ShowUdaInputDialog("UDA 추가", "", "");
            if (result == null) return;

            try
            {
                vizcore3d.Object3D.UDA.Add(selectedAttributeNodeIndex, result.Item1, result.Item2, true);
                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 추가 완료: {result.Item1} = {result.Item2}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 추가 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 편집 버튼 클릭
        /// </summary>
        private void btnUdaEdit_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvAttributes.SelectedRows.Count == 0)
            {
                MessageBox.Show("편집할 UDA 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIndex = dgvAttributes.SelectedRows[0].Index;
            if (!IsUdaRow(rowIndex))
            {
                MessageBox.Show("UDA 항목만 편집할 수 있습니다.\nUDA 섹션의 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string oldKey = dgvAttributes.Rows[rowIndex].Cells["Key"].Value?.ToString() ?? "";
            string oldValue = dgvAttributes.Rows[rowIndex].Cells["Value"].Value?.ToString() ?? "";

            var result = ShowUdaInputDialog("UDA 편집", oldKey, oldValue);
            if (result == null) return;

            try
            {
                string newKey = result.Item1;
                string newValue = result.Item2;

                if (newKey != oldKey)
                {
                    vizcore3d.Object3D.UDA.UpdateKey(selectedAttributeNodeIndex, oldKey, newKey, true);
                }
                if (newValue != oldValue || newKey != oldKey)
                {
                    vizcore3d.Object3D.UDA.Update(selectedAttributeNodeIndex, newKey, newValue, true);
                }

                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 편집 완료: {newKey} = {newValue}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 편집 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UDA 삭제 버튼 클릭
        /// </summary>
        private void btnUdaDelete_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvAttributes.SelectedRows.Count == 0)
            {
                MessageBox.Show("삭제할 UDA 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rowIndex = dgvAttributes.SelectedRows[0].Index;
            if (!IsUdaRow(rowIndex))
            {
                MessageBox.Show("UDA 항목만 삭제할 수 있습니다.\nUDA 섹션의 행을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string key = dgvAttributes.Rows[rowIndex].Cells["Key"].Value?.ToString() ?? "";

            if (MessageBox.Show($"UDA '{key}'를 삭제하시겠습니까?", "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                vizcore3d.Object3D.UDA.Delete(selectedAttributeNodeIndex, key, true);
                UpdateAttributeTable(selectedAttributeNodeIndex);
                MessageBox.Show($"UDA 삭제 완료: {key}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDA 삭제 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// CSV 파일에서 UDA 일괄 가져오기
        /// CSV 형식: Key,Value (첫 행이 헤더면 자동 스킵)
        /// </summary>
        private void btnUdaImportCSV_Click(object sender, EventArgs e)
        {
            if (selectedAttributeNodeIndex < 0)
            {
                MessageBox.Show("부재를 먼저 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
            dlg.Title = "UDA CSV 파일 선택";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(dlg.FileName, System.Text.Encoding.UTF8);

                if (lines.Length == 0)
                {
                    MessageBox.Show("CSV 파일이 비어있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int startLine = 0;
                // 첫 행이 헤더인지 확인 (Key, Value 등의 문자가 포함되면 스킵)
                string firstLine = lines[0].Trim().ToLower();
                if (firstLine.Contains("key") || firstLine.Contains("value") || firstLine.Contains("속성"))
                {
                    startLine = 1;
                }

                int successCount = 0;
                int failCount = 0;
                List<string> errors = new List<string>();

                for (int i = startLine; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // CSV 파싱 (쉼표 구분, 따옴표 처리)
                    string[] parts = ParseCsvLine(line);

                    if (parts.Length < 2)
                    {
                        failCount++;
                        errors.Add($"Line {i + 1}: 컬럼 부족 - \"{line}\"");
                        continue;
                    }

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (string.IsNullOrEmpty(key))
                    {
                        failCount++;
                        errors.Add($"Line {i + 1}: Key가 비어있음");
                        continue;
                    }

                    try
                    {
                        vizcore3d.Object3D.UDA.Add(selectedAttributeNodeIndex, key, value, true);
                        successCount++;
                    }
                    catch
                    {
                        // Add 실패 시 Update 시도 (이미 존재하는 키일 수 있음)
                        try
                        {
                            vizcore3d.Object3D.UDA.Update(selectedAttributeNodeIndex, key, value, true);
                            successCount++;
                        }
                        catch (Exception ex2)
                        {
                            failCount++;
                            errors.Add($"Line {i + 1}: {key} - {ex2.Message}");
                        }
                    }
                }

                UpdateAttributeTable(selectedAttributeNodeIndex);

                string msg = $"CSV 가져오기 완료\n성공: {successCount}건, 실패: {failCount}건";
                if (errors.Count > 0)
                {
                    msg += "\n\n오류 상세:\n" + string.Join("\n", errors.GetRange(0, Math.Min(errors.Count, 10)));
                    if (errors.Count > 10)
                        msg += $"\n... 외 {errors.Count - 10}건";
                }
                MessageBox.Show(msg, "CSV 가져오기", MessageBoxButtons.OK,
                    failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 파일 읽기 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// CSV 한 줄 파싱 (따옴표 처리 포함)
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);
            return result.ToArray();
        }

        #endregion
    }
}
