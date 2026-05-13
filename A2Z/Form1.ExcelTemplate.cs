using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using VIZCore3D.NET.Data;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// REQ-002 / T-012 PoC Step 3 (옵션 A 본진) —
        /// SDK가 ImportExcel로 생성한 JSON을 우리가 직접 파싱하고,
        /// ShapeDrawing.AddLine + Drawing2D.Object2D.Add2DObjectFromShapeDrawing 으로 2D 캔버스에 직접 렌더.
        /// SDK 자동 적용(Step 1/2)이 사용자 추가 템플릿에 동작 안 함이 확정되어 우리가 렌더 책임.
        /// </summary>
        private void btnExcelTemplatePoC_Click(object sender, EventArgs e)
        {
            // 1. JSON 경로 자동 검색 — SDK 내부 폴더의 가장 오래된 SHI(Template_0)
            string sdkTemplateRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SOFTHILLS", "VIZCore3D+.NET", "Template");

            string jsonPath = null;
            string candidate = Path.Combine(sdkTemplateRoot, "Template_0", "사용자템플릿_엑셀_Rev_01.json");
            if (File.Exists(candidate))
            {
                jsonPath = candidate;
            }
            else if (Directory.Exists(sdkTemplateRoot))
            {
                // Template_0 이름 변경됐을 경우 최신 폴더 검색
                var folders = Directory.GetDirectories(sdkTemplateRoot, "Template_*")
                    .OrderBy(d => new DirectoryInfo(d).CreationTime)
                    .ToList();
                foreach (var folder in folders)
                {
                    var jsonFiles = Directory.GetFiles(folder, "*.json");
                    if (jsonFiles.Length > 0)
                    {
                        jsonPath = jsonFiles[0];
                        break;
                    }
                }
            }

            if (jsonPath == null)
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "SDK 변환 JSON 파일 선택";
                    ofd.Filter = "JSON files (*.json)|*.json";
                    ofd.InitialDirectory = sdkTemplateRoot;
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    jsonPath = ofd.FileName;
                }
            }

            DiagLog($"[PoC-Excel-Step3] 시작 jsonPath={jsonPath}");

            try
            {
                // ─── 2D 도면 모드 진입 시퀀스 (기존 GenerateSheetDrawing2D 패턴) ───
                // 사용자 지적: 이 시퀀스 누락으로 Step 3 이전 안 보였음
                try
                {
                    vizcore3d.ToolbarDrawing2D.Visible = true;
                    vizcore3d.ViewMode = VIZCore3D.NET.Data.ViewKind.Both;
                    // 우리 JSON 데이터 범위 약 355×227mm — A3 landscape(420×297) 안전 수용
                    vizcore3d.Drawing2D.View.SetCanvasSize(420, 297);
                    vizcore3d.Drawing2D.View.SetSelectCanvas(1);

                    float wCanvas = 0f, hCanvas = 0f;
                    vizcore3d.Drawing2D.View.GetCanvasSize(ref wCanvas, ref hCanvas);
                    DiagLog($"[PoC-Excel-Step3] 2D 모드 진입 완료. CanvasSize=({wCanvas}, {hCanvas})");

                    // 외곽 테두리 생성 (선택 — 시각 확인용)
                    // 새 SDK(VIZCore3D+.NET) — TemplateBorderInfo 인자 필수
                    vizcore3d.Drawing2D.Template.CrateTemplateBorder(new VIZCore3D.NET.Data.TemplateBorderInfo());
                    DiagLog("[PoC-Excel-Step3] CrateTemplateBorder 호출 완료 (새 SDK 시그니처 적용)");
                }
                catch (Exception modeEx)
                {
                    DiagLog($"[PoC-Excel-Step3] 2D 모드 진입 실패 {modeEx.GetType().Name}: {modeEx.Message}");
                }

                // 2. JSON 파싱
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var jsonText = File.ReadAllText(jsonPath);
                var data = serializer.Deserialize<Dictionary<string, object>>(jsonText);

                var lineArr = data.ContainsKey("Line") ? data["Line"] as ArrayList : null;
                var textArr = data.ContainsKey("Text") ? data["Text"] as ArrayList : null;
                var imageArr = data.ContainsKey("Image") ? data["Image"] as ArrayList : null;

                int lineCount = lineArr?.Count ?? 0;
                int textCount = textArr?.Count ?? 0;
                int imageCount = imageArr?.Count ?? 0;
                DiagLog($"[PoC-Excel-Step3] JSON 파싱 OK: Line={lineCount} Text={textCount} Image={imageCount}");

                // 3. InputBox — 시도 모드 선택
                string mode = Microsoft.VisualBasic.Interaction.InputBox(
                    "모드 선택:\n\n" +
                    "  4 : SDK 자동 적용 (Set2DViewDefaultTemplate) — 사용자 가설 검증, 권장\n" +
                    "  1 : Line 10개만 직접 그리기 (Step 3 검증)\n" +
                    "  2 : Line 전체 직접 그리기 (" + lineCount + "개)\n" +
                    "  0 : 기존 ShapeDrawing 모두 제거 (clear)",
                    "PoC-Excel Step 4",
                    "4");
                if (string.IsNullOrEmpty(mode)) return;
                mode = mode.Trim();

                if (mode == "0")
                {
                    try { vizcore3d.ShapeDrawing.Clear(); DiagLog("[PoC-Excel-Step4] ShapeDrawing.Clear 호출"); }
                    catch (Exception clearEx) { DiagLog($"[PoC-Excel-Step4] ShapeDrawing.Clear 실패 {clearEx.Message}"); }
                    MessageBox.Show("ShapeDrawing 제거 시도 완료. 캔버스 확인.", "PoC-Excel");
                    return;
                }

                if (mode == "4")
                {
                    // 사용자 가설 검증: Step 1/2 실패가 2D 모드 진입 누락 때문일 가능성
                    // 2D 모드 진입은 이미 위에서 완료. 여기서는 인덱스 입력 받아 적용만.
                    string idxInput = Microsoft.VisualBasic.Interaction.InputBox(
                        "Set2DViewDefaultTemplate에 사용할 인덱스:\n\n" +
                        "  -1 : 빈 템플릿\n" +
                        "  0~2 : DSME 내장\n" +
                        "  3+ : 사용자 추가 SHI (3이 가장 유력 — Template_0)\n\n" +
                        "여러 값 시도 가능. CrateTemplateBorder + ViewMode=Both 가 위에서 호출됐으니 이번엔 잘 그려질 가능성.",
                        "PoC-Excel Step 4 — SDK 자동 적용",
                        "3");
                    if (string.IsNullOrEmpty(idxInput)) return;
                    if (!int.TryParse(idxInput.Trim(), out int idx))
                    {
                        MessageBox.Show("정수 필요", "PoC-Excel");
                        return;
                    }

                    try
                    {
                        vizcore3d.Drawing2D.Template.Set2DViewDefaultTemplate(idx);
                        DiagLog($"[PoC-Excel-Step4] Set2DViewDefaultTemplate({idx}) 호출 완료");
                    }
                    catch (Exception applyEx)
                    {
                        DiagLog($"[PoC-Excel-Step4] Set2DViewDefaultTemplate({idx}) 실패 {applyEx.GetType().Name}: {applyEx.Message}");
                    }

                    MessageBox.Show(
                        $"Set2DViewDefaultTemplate({idx}) 호출 완료 — 2D 모드 진입 후 적용.\n\n" +
                        $"2D View 캔버스 확인:\n" +
                        $"  - SHI 셀 구조(4뷰/BOM/NOTE/도면정보) 보이면 → 사용자 가설 정답!\n" +
                        $"  - 빈 outline만 또는 DSME 보이면 → 다른 인덱스 시도\n" +
                        $"  - 아무것도 없으면 → 모드 진입 자체 실패 (DiagLog 확인)",
                        "PoC-Excel Step 4",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                int lineLimit = (mode == "1") ? 10 : lineCount;

                // 4. Line 그리기 — ShapeDrawing.AddLine(List<Vertex3DItemCollection>)
                int linesDrawn = 0;
                int shapeId = -1;
                if (lineArr != null && lineLimit > 0)
                {
                    var allLines = new List<Vertex3DItemCollection>();
                    for (int i = 0; i < Math.Min(lineLimit, lineArr.Count); i++)
                    {
                        var ln = lineArr[i] as Dictionary<string, object>;
                        if (ln == null) continue;

                        float minX = Convert.ToSingle(ln["minX"]);
                        float minY = Convert.ToSingle(ln["minY"]);
                        float maxX = Convert.ToSingle(ln["maxX"]);
                        float maxY = Convert.ToSingle(ln["maxY"]);

                        var seg = new Vertex3DItemCollection();
                        seg.Add(new Vertex3D(minX, minY, 0f));
                        seg.Add(new Vertex3D(maxX, maxY, 0f));
                        allLines.Add(seg);
                        linesDrawn++;
                    }

                    try
                    {
                        shapeId = vizcore3d.ShapeDrawing.AddLine(allLines, -1, System.Drawing.Color.Black, 0.3f, true);
                        DiagLog($"[PoC-Excel-Step3] ShapeDrawing.AddLine OK count={linesDrawn} shapeId={shapeId}");
                    }
                    catch (Exception lineEx)
                    {
                        DiagLog($"[PoC-Excel-Step3] ShapeDrawing.AddLine 실패 {lineEx.GetType().Name}: {lineEx.Message}");
                    }

                    // 3D ShapeDrawing → 2D 캔버스로 변환
                    if (shapeId > 0)
                    {
                        try
                        {
                            vizcore3d.Drawing2D.Object2D.Add2DObjectFromShapeDrawing(new List<int> { shapeId });
                            DiagLog($"[PoC-Excel-Step3] Add2DObjectFromShapeDrawing OK shapeId={shapeId}");
                        }
                        catch (Exception conv2dEx)
                        {
                            DiagLog($"[PoC-Excel-Step3] Add2DObjectFromShapeDrawing 실패 {conv2dEx.GetType().Name}: {conv2dEx.Message}");
                        }
                    }
                }

                MessageBox.Show(
                    $"PoC Step 3 결과 (Line만):\n" +
                    $"  - JSON: {Path.GetFileName(jsonPath)}\n" +
                    $"  - Line 추가: {linesDrawn}/{lineCount} (shapeId={shapeId})\n" +
                    $"  - Text({textCount}) / Image({imageCount}): 다음 단계\n\n" +
                    "2D View 캔버스 확인.\n" +
                    "  - 셀 테두리가 보이면 → 좌표·렌더 성공! Text/Image 단계 진행\n" +
                    "  - 안 보이면 → 좌표 단위 mismatch 또는 2D 모드 진입 필요\n" +
                    "  - 일부만 보이면 → 좌표 범위 OK, 일부 누락 원인 분석",
                    "PoC-Excel Step 3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"[PoC-Excel-Step3] 오류 {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"오류: {ex.GetType().Name}\n{ex.Message}\n\nlogs/diag-yyyy-mm-dd.log 참고.",
                    "PoC-Excel Step 3 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
