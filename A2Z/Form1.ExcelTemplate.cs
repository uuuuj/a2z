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
                DiagLog("[PoC-Excel-Step1.5] ImportExcel 호출 완료 — 등록만 됨 (Step 1 결과)");

                // Step 1.5 — Step 1에서 ImportExcel은 SDK 내부 "사용자 템플릿" 목록에만 등록되고
                // 2D View 캔버스에는 적용 안 됨을 사용자 사내 PC 검증으로 확인.
                // 적용을 위해 Set2DViewDefaultTemplate(int) 호출.
                // 인덱스: -1(빈), 0~2(기본 DSME 템플릿), 3 이상(사용자 추가). 정확한 인덱스 모르므로 사용자가 입력.
                // ※ string 오버로드는 internal/protected라 외부 호출 불가 (빌드 검증으로 확정).

                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Set2DViewDefaultTemplate에 사용할 인덱스 입력\n\n" +
                    "  -1 : 빈 템플릿\n" +
                    "  0~2 : 기본 DSME 템플릿\n" +
                    "  3 이상 : 사용자 추가 (Rev_01 적용 후보)\n\n" +
                    "여러 값 시도해 보세요. 안 보이면 다음 인덱스로.",
                    "PoC-Excel Step 1.5",
                    "3");

                if (string.IsNullOrEmpty(input)) return;
                if (!int.TryParse(input.Trim(), out int templateIdx))
                {
                    MessageBox.Show("정수 입력 필요", "PoC-Excel Step 1.5", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    vizcore3d.Drawing2D.Template.Set2DViewDefaultTemplate(templateIdx);
                    DiagLog($"[PoC-Excel-Step1.5] Set2DViewDefaultTemplate({templateIdx}) 호출 성공");
                }
                catch (Exception applyEx)
                {
                    DiagLog($"[PoC-Excel-Step1.5] Set2DViewDefaultTemplate({templateIdx}) 실패 {applyEx.GetType().Name}: {applyEx.Message}");
                }

                MessageBox.Show(
                    $"Set2DViewDefaultTemplate({templateIdx}) 호출.\n\n2D View 캔버스 확인:\n" +
                    "  - 엑셀 셀 구조가 그려졌으면 → 이 인덱스가 우리 SHI\n" +
                    "  - 안 보이거나 다른 템플릿(DSME 등)이면 → 다른 인덱스 시도\n\n" +
                    "버튼을 다시 눌러 다른 인덱스 입력 가능. logs/diag-yyyy-mm-dd.log 에 기록.",
                    "PoC-Excel Step 1.5",
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
