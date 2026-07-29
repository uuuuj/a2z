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
        /// REV 이력 표 기재행별 슬롯 시작 번호 — 엑셀 44행(첫 기재행) → 40행 순 (#64).
        /// 머리글이 45행이고 첫 기재행(44행)이 그 바로 위, 리비전이 올라가면 위로 쌓인다.
        /// 한 행은 6칸 연속: +0 REV. / +1 DATE / +2 DESCRIPTION / +3 DRAWN / +4 CHECKED / +5 APPROVED.
        /// 템플릿 `제작도_도면_1.xlsx`·`가공도_도면_1.xlsx` 공통.
        /// </summary>
        private static readonly int[] RevRowSlotBase = { 194, 188, 182, 176, 170 };

        /// <summary>
        /// 표제부 REV 이력 표(Input_170~199) 채우기 — 제작도·조립도·설치도·가공도 공통 (#64 Phase 1).
        /// history는 오름차순(옛→새): history[0] = REV 0이 첫 기재행(44행)이고 인덱스가 커질수록 윗 행.
        /// 템플릿 표가 5행이라 5건까지만 기재한다 (초과 시 누적 규칙은 Phase 3에서 확정).
        ///
        /// 괘선 규칙 (#33·#60): 기재행으로 쓰는 행은 6칸 전부 data에 키를 넣는다.
        ///   값이 비면 "" 대신 " "(공백 1칸)를 넣는다 — ImportExcelWithData는 값이 있으면 치환해
        ///   {Input}을 남기지 않고, RemoveEmptyTemplateBorders는 {Input}이 만든 TextBox가 있어야
        ///   괘선을 지운다. 즉 " "여야 그 칸 괘선이 산다.
        ///   반대로 미사용 이력행(170~193)은 키를 아예 넣지 않아 괘선이 지워지게 둔다
        ///   (2026-07-21 합의: 첫 기재행만 보존).
        /// </summary>
        private void FillRevisionTable(Dictionary<int, string> data, IList<RevisionEntry> history)
        {
            if (data == null || history == null) return;

            int rows = Math.Min(history.Count, RevRowSlotBase.Length);
            if (history.Count > RevRowSlotBase.Length)
                DiagLog($"[REV표] 이력 {history.Count}건 중 {RevRowSlotBase.Length}건만 기재 (템플릿 5행 한도)");

            for (int i = 0; i < rows; i++)
            {
                RevisionEntry entry = history[i];
                if (entry == null) continue;

                int b = RevRowSlotBase[i];
                data[b] = KeepBorder(entry.Rev);
                data[b + 1] = KeepBorder(entry.Date);
                data[b + 2] = KeepBorder(entry.Description);
                data[b + 3] = KeepBorder(entry.Drawn);
                data[b + 4] = KeepBorder(entry.Checked);
                data[b + 5] = KeepBorder(entry.Approved);
            }
        }

        /// <summary>
        /// 이번 출력의 REV 기재행 1건 (#64 Phase 1) — REV.=0 고정, DATE=출력일.
        /// DESCRIPTION 기본 문구는 미정(#64 결정 필요 ②), DRAWN/CHECKED/APPROVED는 입력 수단이
        /// 아직 없어(#64 결정 필요 ①) 모두 빈 값 → KeepBorder가 공백 1칸으로 바꿔 괘선만 남긴다.
        /// Phase 2에서 설정값을, Phase 3에서 이전 이력을 앞에 붙이면 그대로 확장된다.
        /// </summary>
        private List<RevisionEntry> BuildCurrentRevisionHistory()
        {
            return new List<RevisionEntry>
            {
                new RevisionEntry
                {
                    Rev = "0",
                    // InvariantCulture — PC 지역 설정의 달력(음력·일본 연호 등)에 좌우되지 않게 고정
                    Date = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    Description = "",   // TODO Phase 2/결정 ②: 기본 문구 확정 시 대입
                    Drawn = "",         // TODO Phase 2: 작성자 설정값
                    Checked = "",       // TODO Phase 2: 검도자 설정값
                    Approved = "",      // TODO Phase 2: 승인자 설정값
                }
            };
        }

        /// <summary>
        /// 빈 값을 괘선 보존용 공백 1칸으로 바꾼다 (사유는 FillRevisionTable 주석 참고).
        /// </summary>
        private static string KeepBorder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? " " : value.Trim();
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

        // ── (정리 2026-07-19) 템플릿 JSON 사전변환·View 영역 세션 캐시 제거 ──
        //   JSON 사전변환: 실측 ConvertExcelToJson 290초 + 태그 미보존(hasTags=False)로 무용 → 폐기.
        //   View 영역 캐시: 템플릿을 엑셀에서 수정해도 옛 좌표를 재사용하는 staleness 버그 유발 → 제거.
        //   근본 해법은 템플릿 자체를 작게(~4천 셀) 유지하는 것 — 파싱이 수 ms라 캐시 불필요.
        //   (큰 1mm 그리드 6만 셀 템플릿은 파싱 수십 초 + openpyxl 저장본 네이티브 크래시로 폐기됨)
    }
}
