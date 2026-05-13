using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using VIZCore3D.NET.Data;

namespace A2Z
{
    public partial class Form1
    {
        /// <summary>
        /// REQ-002 / T-012 PoC Step 4 — Softhills 신 API 3종 (2026-05-13 배포) 동작 검증.
        /// Set2DViewTemplateMark + ImportExcelWithData + GetViewAreasFromExcel 한 번에 호출.
        /// 메인 도면 흐름(GenerateSheetDrawing2D)은 다른 에이전트가 수정 중이라 건드리지 않음.
        /// 이 핸들러로 엑셀 템플릿이 정상 적용되는지 사내 PC 시각 검증 → 결과 따라 메인 흐름 전환 결정.
        ///
        /// 의존 리소스 (솔루션 루트):
        ///   - 사용자템플릿_엑셀_제작도.xlsx  (메인 4면도 템플릿)
        ///   - Logo.png                       ({Image} 슬롯용)
        /// </summary>
        private void btnExcelTemplatePoC_Click(object sender, EventArgs e)
        {
            if (!vizcore3d.Model.IsOpen())
            {
                MessageBox.Show("먼저 모델 파일을 열어주세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string solutionPath = GetSolutionPath();
            string xlsxPath = Path.Combine(solutionPath, "사용자템플릿_엑셀_제작도.xlsx");
            string logoPath = Path.Combine(solutionPath, "Logo.png");

            if (!File.Exists(xlsxPath))
            {
                MessageBox.Show($"엑셀 파일을 찾을 수 없습니다.\n{xlsxPath}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(logoPath))
            {
                MessageBox.Show($"로고 파일을 찾을 수 없습니다.\n{logoPath}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DiagLog($"[PoC-Excel-Step4] 시작 xlsx={Path.GetFileName(xlsxPath)} logo={Path.GetFileName(logoPath)}");

            try
            {
                // 1. 히든라인 모델 투영을 위한 EdgeData 사전 생성
                vizcore3d.Object3D.GenerateEdgeData();
                DiagLog("[PoC-Excel-Step4] GenerateEdgeData OK");

                // 2. 모델/치수 라인 두께 (메인 도면과 동일 톤)
                vizcore3d.Drawing2D.Object2D.ModelLineThickness = 3.0f;
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureLineWidth(0.3f);
                vizcore3d.Drawing2D.Object2D.Set2DViewCreateObjectItemMeasureTextHeight(10f);

                // 3. data Dictionary 구성 (도면정보 하드코딩 + BOM 8컬럼)
                Dictionary<int, string> data = new Dictionary<int, string>();
                data[1] = "CEDAR FLNG";      // 프로젝트명 (임시 하드코딩 — 추후 출처 결정)
                data[2] = "SN2688";          // 선박번호
                data[3] = "DETAIL DRAWING";  // 도면종류

                // BOM 8컬럼 × 15행 — lvDrawingBOMInfo Row 0(요약행) 제외
                int bomMapped = 0;
                if (lvDrawingBOMInfo.Items.Count > 1)
                {
                    int n = Math.Min(lvDrawingBOMInfo.Items.Count - 1, 15);
                    for (int i = 0; i < n; i++)
                    {
                        ListViewItem item = lvDrawingBOMInfo.Items[i + 1];
                        data[4   + i] = item.Text;                              // NO
                        data[19  + i] = SafeSubItem(item, 1);                   // ITEM
                        data[34  + i] = SafeSubItem(item, 2);                   // MATERIAL
                        data[49  + i] = SafeSubItem(item, 3);                   // SIZE
                        data[64  + i] = SafeSubItem(item, 4);                   // Q'TY
                        data[79  + i] = SafeSubItem(item, 5);                   // T/W
                        data[94  + i] = SafeSubItem(item, 6);                   // MA
                        data[109 + i] = SafeSubItem(item, 7);                   // FA
                    }
                    bomMapped = n;
                }
                DiagLog($"[PoC-Excel-Step4] data 구성: 도면정보 3 + BOM {bomMapped}행 (Input 총 {data.Count}개)");

                // 4. 신 API #1 — 로고 매핑 ({Image} 셀들에 동일 로고)
                vizcore3d.Drawing2D.Template.Set2DViewTemplateMark(logoPath, logoPath);
                DiagLog("[PoC-Excel-Step4] Set2DViewTemplateMark OK");

                // 5. 신 API #2 — 엑셀 템플릿 적용 + {Input_N} 데이터 치환
                vizcore3d.Drawing2D.Template.ImportExcelWithData(xlsxPath, data);
                DiagLog("[PoC-Excel-Step4] ImportExcelWithData OK");

                vizcore3d.Drawing2D.View.SetSelectCanvas(1);

                // 6. 신 API #3 — {View_N} 영역 좌표 파싱
                var viewAreas = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
                if (viewAreas == null || viewAreas.Count == 0)
                {
                    DiagLog("[PoC-Excel-Step4] GetViewAreasFromExcel 비어있음");
                    MessageBox.Show("엑셀에서 {View_N} 영역을 찾지 못했습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DiagLog($"[PoC-Excel-Step4] GetViewAreasFromExcel {viewAreas.Count}개 영역 반환");

                // 7. {View_N} 인덱스 ↔ 카메라 방향 매핑 (메인 4면도 규약)
                Dictionary<int, CameraDirection> cameraMap = new Dictionary<int, CameraDirection>
                {
                    { 1, CameraDirection.ISO_PLUS },   // ISO
                    { 2, CameraDirection.Z_MINUS  },   // LOOKING "Z"
                    { 3, CameraDirection.X_MINUS  },   // LOOKING "X"
                    { 4, CameraDirection.Y_MINUS  },   // LOOKING "Y"
                };

                const float margin = 5f;
                int viewsRendered = 0;

                // 8. 각 View 영역에 모델 투영 (카메라 이동 → 객체 생성 → fit → 영역 중심 이동)
                for (int i = 0; i < viewAreas.Count; i++)
                {
                    var p = viewAreas[i];
                    if (!cameraMap.TryGetValue(p.Index, out CameraDirection camDir))
                    {
                        DiagLog($"[PoC-Excel-Step4] View_{p.Index} 카메라 매핑 없음 — 스킵");
                        continue;
                    }

                    vizcore3d.View.MoveCamera(camDir);

                    int objId = vizcore3d.Drawing2D.Object2D
                        .Create2DViewObjectWithModelHiddenLineAtCanvasOrigin(
                            Drawing2D_ModelViewKind.CURRENT);
                    if (objId < 0)
                    {
                        DiagLog($"[PoC-Excel-Step4] View_{p.Index} Object2D 생성 실패 objId={objId}");
                        continue;
                    }

                    // 영역 내 fit 계산
                    float fitW = p.Width - 2f * margin;
                    float fitH = p.Height - 2f * margin;
                    float objW = 0f, objH = 0f;
                    vizcore3d.Drawing2D.Object2D.GetObjectSize(objId, ref objW, ref objH);
                    float objScale = vizcore3d.Drawing2D.Object2D.GetObjectScale(objId);

                    if (objW > 0f && objH > 0f && fitW > 0f && fitH > 0f)
                    {
                        float fitScale = Math.Min(fitW / objW, fitH / objH);
                        vizcore3d.Drawing2D.Object2D.RescaleObject(objId, objScale * fitScale);
                    }

                    // 영역 중심으로 이동 (다운로드 샘플 패턴: Y에 +15 오프셋)
                    float cx = p.X + p.Width / 2f;
                    float cy = p.Y + p.Height / 2f;
                    vizcore3d.Drawing2D.Object2D.MoveObjectTo(objId, cx, cy + 15f);

                    DiagLog($"[PoC-Excel-Step4] View_{p.Index} 투영 OK dir={camDir} " +
                            $"area=({p.X:F1},{p.Y:F1},{p.Width:F1},{p.Height:F1}) " +
                            $"objSize=({objW:F1},{objH:F1}) objScale={objScale:F4}");
                    viewsRendered++;
                }

                // 9. 최종 렌더 + 캔버스 오토핏 + 선택 해제
                vizcore3d.Drawing2D.Render();
                vizcore3d.Drawing2D.View.SetCanvasResetViewPos(-1);
                vizcore3d.Drawing2D.Object2D.UnselectAllObjectBy2DView();
                vizcore3d.Drawing2D.Object2D.UnselectCurrentWorkObjectBy2DView();

                DiagLog($"[PoC-Excel-Step4] 완료 — Views {viewsRendered}/{viewAreas.Count}, BOM {bomMapped}행");

                MessageBox.Show(
                    $"엑셀 템플릿 PoC 적용 완료\n\n" +
                    $"  • 도면정보 (Input_1~3) : 3개 매핑\n" +
                    $"  • BOM 표               : {bomMapped}행 매핑\n" +
                    $"  • View 영역            : {viewsRendered}/{viewAreas.Count}개 렌더\n\n" +
                    "2D 캔버스에서 결과를 확인해주세요.",
                    "PoC Step 4 — 신 API 검증",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagLog($"[PoC-Excel-Step4] 오류 {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"PoC 오류: {ex.GetType().Name}\n{ex.Message}\n\nlogs/diag-yyyy-mm-dd.log 참고.",
                    "PoC Step 4 실패",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ListViewItem.SubItems[idx] 안전 조회 (인덱스 초과 시 빈 문자열).
        /// </summary>
        private static string SafeSubItem(ListViewItem item, int idx)
        {
            if (item == null || item.SubItems == null) return "";
            if (idx < 0 || idx >= item.SubItems.Count) return "";
            return item.SubItems[idx].Text ?? "";
        }
    }
}
