using System;
using System.IO;
using System.Linq;
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

            DiagLog($"[PoC-Excel-Step1] 시작 path={excelPath}");

            try
            {
                vizcore3d.Drawing2D.Template.ImportExcel(excelPath);
                DiagLog("[PoC-Excel-Step1] ImportExcel 호출 완료");

                // templateDatas는 private/internal 필드라 외부 접근 불가 확인됨.
                // Step 1은 시각 검증에 집중 — 2D View 캔버스 결과만 본다.
                // Step 2에서 셀 좌표를 받아오는 다른 public API(ParseJson 등) 탐색 예정.

                MessageBox.Show(
                    "ImportExcel 호출 완료.\n\n확인할 것:\n" +
                    "  1) 2D View 캔버스에 엑셀 셀 구조(테두리·텍스트·라벨)가 그려졌는지\n" +
                    "  2) 아무것도 안 보이면 별도 표시 호출(RenderTemplate 등)이 필요\n\n" +
                    "logs/diag-yyyy-mm-dd.log 에 호출 결과 기록됨.",
                    "PoC-Excel Step 1",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"[PoC-Excel-Step1] 오류 {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"오류: {ex.GetType().Name}\n{ex.Message}\n\n자세한 내용은 logs/diag-yyyy-mm-dd.log 참고.",
                    "PoC-Excel Step 1 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
