using System;
using System.Windows.Forms;
using VIZCore3D.NET;

namespace A2Z
{
    /// <summary>
    /// 라이선스 서버 연결 및 자동 갱신 전담 partial.
    /// 기존 Form1.BOM.cs에서 분리 (T-017, 2026-04-22).
    /// </summary>
    public partial class Form1
    {
        /// <summary>
        /// 라이선스 갱신 타이머 (30분마다 갱신)
        /// </summary>
        private System.Windows.Forms.Timer licenseRefreshTimer;

        /// <summary>
        /// 라이선스 서버 연결 + 자동 갱신 타이머 시작.
        /// 성공 시 true, 실패 시 MessageBox 표시 후 false 반환.
        /// </summary>
        private bool InitializeLicense()
        {
            VIZCore3D.NET.Data.LicenseResults result = vizcore3d.License.LicenseServer("127.0.0.1", 8901);

            if (result != VIZCore3D.NET.Data.LicenseResults.SUCCESS)
            {
                MessageBox.Show(string.Format("License Error: {0}", result), "라이선스 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            StartLicenseRefreshTimer();
            return true;
        }

        /// <summary>
        /// 라이선스 자동 갱신 타이머 시작 (30분 주기)
        /// </summary>
        private void StartLicenseRefreshTimer()
        {
            licenseRefreshTimer = new System.Windows.Forms.Timer();
            licenseRefreshTimer.Interval = 30 * 60 * 1000; // 30분 (밀리초)
            licenseRefreshTimer.Tick += LicenseRefreshTimer_Tick;
            licenseRefreshTimer.Start();
        }

        /// <summary>
        /// 라이선스 갱신 타이머 이벤트 — 30분마다 서버 재연결
        /// </summary>
        private void LicenseRefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                VIZCore3D.NET.Data.LicenseResults result = vizcore3d.License.LicenseServer("127.0.0.1", 8901);

                if (result != VIZCore3D.NET.Data.LicenseResults.SUCCESS)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 실패: {result}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 성공");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 라이선스 갱신 오류: {ex.Message}");
            }
        }
    }
}
