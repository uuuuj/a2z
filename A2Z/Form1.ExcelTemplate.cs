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
        ///   - Logo.png                              ({Image} 슬롯용)
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

        // ── 템플릿 JSON 사전변환 캐시 (신 1mm 그리드 템플릿의 ImportExcelWithData 병목 대응) ──
        //   신 템플릿(제작도_도면_1·가공도_도면_1)은 297×210=약 6만 셀이라 ImportExcelWithData(엑셀 통째 파싱)가
        //   출력마다 수십 초 → UI 멈춤. SDK ConvertExcelToJson으로 엑셀을 1회만 파싱해 JSON으로 굽고(inputData=null →
        //   {Input_N} 태그 텍스트 보존 시도), 이후 페이지마다 JSON 텍스트에서 태그만 치환 + ApplyTemplateFromJson
        //   (엑셀 파싱 없음)으로 그린다. 세션당 변환 1회, 이후 출력은 빠름.
        //
        //   자가 검증: null 변환 결과 JSON에 "{Input_" 토큰이 실제로 남는지 런타임 확인. 안 남으면(=태그가 값으로
        //   구워져 치환 불가) 치환 방식을 못 쓰므로 호출자가 기존 ImportExcelWithData로 폴백한다. 즉 blind 가정 없이
        //   동작 여부를 코드가 판정 → 사내 검증에서 [TplJson] 로그로 어느 경로를 탔는지 즉시 확인 가능.

        // xlsxPath → 태그 보존 JSON 경로. 값 "" = 태그 미보존(치환 불가, 폴백 확정). 키 없음 = 아직 미변환.
        private readonly Dictionary<string, string> _templateTagJsonCache = new Dictionary<string, string>();

        /// <summary>
        /// 엑셀 템플릿을 1회만 JSON으로 변환(태그 보존)해 캐시. 반환: 치환 가능한 JSON 경로, 불가/실패 시 null.
        /// </summary>
        private string EnsureTemplateTagJson(string xlsxPath)
        {
            if (_templateTagJsonCache.TryGetValue(xlsxPath, out string cached))
            {
                if (string.IsNullOrEmpty(cached)) return null;             // 태그 미보존으로 판정됨 → 폴백
                return File.Exists(cached) ? cached : null;
            }

            string jsonPath = Path.ChangeExtension(xlsxPath, ".tags.json");
            try
            {
                // 디스크 재사용 — 이전 세션이 만든 JSON이 엑셀보다 최신이고 태그가 남아있으면 변환 생략(앱 재시작 후 첫 출력도 빠름).
                if (File.Exists(jsonPath)
                    && File.GetLastWriteTimeUtc(jsonPath) >= File.GetLastWriteTimeUtc(xlsxPath))
                {
                    string diskTxt = File.ReadAllText(jsonPath);
                    if (diskTxt.IndexOf("{Input_", StringComparison.Ordinal) >= 0)
                    {
                        _templateTagJsonCache[xlsxPath] = jsonPath;
                        DiagLog($"[TplJson] 디스크 JSON 재사용(변환 생략) size={diskTxt.Length}B — {Path.GetFileName(jsonPath)}");
                        return jsonPath;
                    }
                    // 태그 미보존 JSON이 남아있으면 폴백 확정(재변환해도 결과 동일하므로 시간 낭비 회피)
                    DiagLog($"[TplJson] 디스크 JSON에 태그 없음 — ImportExcelWithData 폴백 확정");
                    _templateTagJsonCache[xlsxPath] = "";
                    return null;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                // inputData=null → 태그 치환 없이 원본 구조만 JSON으로 (태그가 텍스트로 남길 기대)
                string ret = vizcore3d.Drawing2D.Template.ConvertExcelToJson(xlsxPath, null, jsonPath);
                sw.Stop();

                if (string.IsNullOrEmpty(ret) || !File.Exists(jsonPath))
                {
                    DiagLog($"[TplJson] ConvertExcelToJson 실패 ret={(ret ?? "null")} — ImportExcelWithData 폴백");
                    _templateTagJsonCache[xlsxPath] = "";
                    return null;
                }

                string txt = File.ReadAllText(jsonPath);
                bool hasTags = txt.IndexOf("{Input_", StringComparison.Ordinal) >= 0;
                DiagLog($"[TplJson] convert once {sw.ElapsedMilliseconds}ms hasTags={hasTags} size={txt.Length}B → {Path.GetFileName(jsonPath)}");

                if (!hasTags)
                {
                    // 태그가 값으로 구워져 텍스트 치환 불가 → 폴백 확정 (전략 재검토 필요, 로그로 신호)
                    _templateTagJsonCache[xlsxPath] = "";
                    return null;
                }
                _templateTagJsonCache[xlsxPath] = jsonPath;
                return jsonPath;
            }
            catch (Exception ex)
            {
                DiagLog($"[TplJson] convert 예외 — ImportExcelWithData 폴백: {ex.Message}");
                _templateTagJsonCache[xlsxPath] = "";
                return null;
            }
        }

        /// <summary>
        /// 태그 보존 JSON에서 {Input_N}만 치환 후 ApplyTemplateFromJson으로 그린다.
        /// 성공 true, 폴백 필요(변환 불가·apply 실패) 시 false — 호출자가 ImportExcelWithData로 대체한다.
        /// </summary>
        private bool TryApplyTemplateFromJson(string xlsxPath, Dictionary<int, string> data)
        {
            string tagJson = EnsureTemplateTagJson(xlsxPath);
            if (tagJson == null) return false;

            try
            {
                string txt = File.ReadAllText(tagJson);
                // {Input_N} 은 닫는 중괄호까지 포함해 치환하므로 부분 토큰 오치환 없음
                //   (예: "{Input_1}" 은 "{Input_19}" 안에서 매칭되지 않음).
                foreach (var kv in data)
                    txt = txt.Replace("{Input_" + kv.Key + "}", JsonEscapeValue(kv.Value ?? ""));

                string appliedPath = Path.ChangeExtension(xlsxPath, ".applied.json");
                File.WriteAllText(appliedPath, txt);
                vizcore3d.Drawing2D.Template.ApplyTemplateFromJson(appliedPath);
                return true;
            }
            catch (Exception ex)
            {
                DiagLog($"[TplJson] ApplyTemplateFromJson 실패 — ImportExcelWithData 폴백: {ex.Message}");
                return false;
            }
        }

        /// <summary>JSON 문자열 값에 안전하게 들어가도록 이스케이프 (따옴표·역슬래시·개행).</summary>
        private static string JsonEscapeValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }

        // xlsxPath → {View_n} 영역 목록 (세션 캐시). View 좌표는 템플릿 고정이라 1회 파싱 후 재사용.
        //   GetViewAreasFromExcel도 큰 엑셀을 통째 파싱하므로 배치 출력 시 재파싱 비용이 큼.
        private readonly Dictionary<string, List<VIZCore3D.NET.Data.TemplateViewArea>> _viewAreasCache
            = new Dictionary<string, List<VIZCore3D.NET.Data.TemplateViewArea>>();

        /// <summary>{View_n} 영역을 세션 1회만 파싱해 캐시. 실패(빈 결과)는 캐시하지 않아 다음 호출에 재시도.</summary>
        private List<VIZCore3D.NET.Data.TemplateViewArea> GetViewAreasCached(string xlsxPath)
        {
            if (_viewAreasCache.TryGetValue(xlsxPath, out var cached))
                return cached;

            var swVa = System.Diagnostics.Stopwatch.StartNew();
            var list = vizcore3d.Drawing2D.Template.GetViewAreasFromExcel(xlsxPath);
            swVa.Stop();
            if (list != null && list.Count > 0)
            {
                _viewAreasCache[xlsxPath] = list;
                DiagLog($"[TplJson] GetViewAreas 1회 파싱 {swVa.ElapsedMilliseconds}ms {list.Count}개 캐시 — {Path.GetFileName(xlsxPath)}");
            }
            return list;
        }
    }
}
