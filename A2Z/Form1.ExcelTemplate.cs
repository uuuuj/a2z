using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// REQ-002 / T-012 PoC Step 1 — 엑셀 템플릿을 2D View 캔버스에 import 단독 검증.
        /// 기존 GenerateSheetDrawing2D는 건드리지 않고 별도 경로로 시각 결과만 확인.
        /// 호출 후 사내 PC에서 결과를 보고 Step 2(셀 좌표 매핑 → 모델 배치) 진행 여부 결정.
        /// </summary>
        private void btnExcelTemplatePoC_Click(object sender, EventArgs e)
        {
            string baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";

            // 후보 경로 (실행 폴더 → 솔루션 루트 → a2z 루트)
            string[] candidates = new[]
            {
                Path.Combine(baseDir, "사용자템플릿_엑셀_Rev_01.xlsx"),
                Path.Combine(baseDir, "..", "..", "..", "사용자템플릿_엑셀_Rev_01.xlsx"),
                Path.Combine(baseDir, "..", "..", "사용자템플릿_엑셀_Rev_01.xlsx"),
            };
            string excelPath = candidates
                .Select(p =>
                {
                    try { return Path.GetFullPath(p); }
                    catch { return null; }
                })
                .FirstOrDefault(p => p != null && File.Exists(p));

            if (excelPath == null)
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "엑셀 템플릿 선택 (PoC)";
                    ofd.Filter = "Excel files (*.xlsx)|*.xlsx";
                    ofd.InitialDirectory = baseDir;
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    excelPath = ofd.FileName;
                }
            }

            DiagLog($"[PoC-Excel-Step2] 시작 path={excelPath}");

            try
            {
                var templateManager = vizcore3d.Drawing2D.Template;
                var managerType = templateManager.GetType();

                // (1) TemplatePath 읽기 — SDK 데이터 폴더 위치 확인
                string sdkTemplatePath = "(unknown)";
                try
                {
                    var prop = managerType.GetProperty("TemplatePath",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        sdkTemplatePath = prop.GetValue(templateManager) as string ?? "(null)";
                    }
                }
                catch (Exception inner)
                {
                    sdkTemplatePath = $"(read failed: {inner.Message})";
                }
                DiagLog($"[PoC-Excel-Step2] SDK TemplatePath = {sdkTemplatePath}");

                // (2) ImportExcel 재실행 여부 — 누적 방지
                string reimportInput = Microsoft.VisualBasic.Interaction.InputBox(
                    "ImportExcel 재실행? (트리 누적 방지)\n\n" +
                    "  Y : 엑셀을 다시 import (트리에 새 항목 추가됨)\n" +
                    "  N : skip, 기존 등록 그대로 사용 (인덱스 시도만)",
                    "PoC-Excel Step 2",
                    "N");
                if (string.IsNullOrEmpty(reimportInput)) return;

                if (reimportInput.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    templateManager.ImportExcel(excelPath);
                    DiagLog("[PoC-Excel-Step2] ImportExcel 재호출 완료");
                }
                else
                {
                    DiagLog("[PoC-Excel-Step2] ImportExcel 재호출 skip");
                }

                // (3) 적용 시도 — filePath 후보 입력
                string defaultFp = excelPath;
                string filePathInput = Microsoft.VisualBasic.Interaction.InputBox(
                    "Draw2DViewTemplate(filePath) reflection 호출용 경로:\n\n" +
                    "후보:\n" +
                    "  (a) 원본 xlsx — 위 기본값\n" +
                    "  (b) export JSON — C:\\Users\\duddl\\Desktop\\Template\\Template_0\\사용자템플릿_엑셀_Rev_01.json\n" +
                    "  (c) SDK 내부 폴더 = " + sdkTemplatePath + "\n\n" +
                    "한 후보씩 시도. 빈값이면 reflection skip하고 int(-1)만 호출.",
                    "PoC-Excel Step 2",
                    defaultFp);

                if (string.IsNullOrEmpty(filePathInput))
                {
                    // skip → 빈 템플릿(-1)으로 캔버스 초기화 후 종료
                    try
                    {
                        templateManager.Set2DViewDefaultTemplate(-1);
                        DiagLog("[PoC-Excel-Step2] Set2DViewDefaultTemplate(-1) — 빈 템플릿 호출");
                    }
                    catch (Exception clearEx)
                    {
                        DiagLog($"[PoC-Excel-Step2] Set2DViewDefaultTemplate(-1) 실패 {clearEx.Message}");
                    }
                    return;
                }

                string filePath = filePathInput.Trim();

                // Reflection — Draw2DViewTemplate(string) 호출
                bool drawCalled = false;
                try
                {
                    var drawMethod = managerType.GetMethod("Draw2DViewTemplate",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(string) }, null);
                    if (drawMethod != null)
                    {
                        drawMethod.Invoke(templateManager, new object[] { filePath });
                        DiagLog($"[PoC-Excel-Step2] Draw2DViewTemplate(\"{filePath}\") reflection 호출 성공");
                        drawCalled = true;
                    }
                    else
                    {
                        DiagLog("[PoC-Excel-Step2] Draw2DViewTemplate(string) 메서드 못 찾음");
                    }
                }
                catch (Exception drawEx)
                {
                    var real = drawEx.InnerException ?? drawEx;
                    DiagLog($"[PoC-Excel-Step2] Draw2DViewTemplate reflection 실패 {real.GetType().Name}: {real.Message}");
                }

                // Reflection — Set2DViewDefaultTemplate(string) 호출 (fallback)
                bool setStringCalled = false;
                try
                {
                    var setMethod = managerType.GetMethod("Set2DViewDefaultTemplate",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(string) }, null);
                    if (setMethod != null)
                    {
                        setMethod.Invoke(templateManager, new object[] { filePath });
                        DiagLog($"[PoC-Excel-Step2] Set2DViewDefaultTemplate(\"{filePath}\") reflection 호출 성공");
                        setStringCalled = true;
                    }
                    else
                    {
                        DiagLog("[PoC-Excel-Step2] Set2DViewDefaultTemplate(string) 메서드 못 찾음");
                    }
                }
                catch (Exception setEx)
                {
                    var real = setEx.InnerException ?? setEx;
                    DiagLog($"[PoC-Excel-Step2] Set2DViewDefaultTemplate(string) reflection 실패 {real.GetType().Name}: {real.Message}");
                }

                MessageBox.Show(
                    $"Reflection 호출 결과:\n" +
                    $"  - SDK TemplatePath: {sdkTemplatePath}\n" +
                    $"  - filePath: {filePath}\n" +
                    $"  - Draw2DViewTemplate: {(drawCalled ? "호출됨" : "실패")}\n" +
                    $"  - Set2DViewDefaultTemplate(str): {(setStringCalled ? "호출됨" : "실패")}\n\n" +
                    "2D View 캔버스 확인. logs/diag-yyyy-mm-dd.log 에 상세 기록.",
                    "PoC-Excel Step 2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"[PoC-Excel-Step2] 오류 {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"오류: {ex.GetType().Name}\n{ex.Message}\n\n자세한 내용은 logs/diag-yyyy-mm-dd.log 참고.",
                    "PoC-Excel Step 2 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
