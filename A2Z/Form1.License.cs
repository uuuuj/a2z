using System;
using System.Collections.Generic;
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
        /// 라이선스 서버 후보 — 앞에서부터 순서대로 시도하고, 처음 성공한 곳을 이후 갱신에도 계속 쓴다.
        /// 로컬(127.0.0.1) 우선, 실패 시 사내 라이선스 서버로 폴백 (2026-07-27 사용자 지시).
        /// </summary>
        private static readonly (string Ip, int Port)[] LicenseServers =
        {
            ("127.0.0.1",      8901),   // 로컬 (기본)
            ("60.100.164.177", 8901),   // 사내 라이선스 서버 (폴백)
        };

        /// <summary>
        /// 마지막으로 인증에 성공한 <see cref="LicenseServers"/> 인덱스. 미인증이면 -1.
        /// 갱신 때 이 서버를 먼저 시도하고, 실패하면 나머지 후보로 다시 폴백한다.
        /// </summary>
        private int activeLicenseServerIndex = -1;

        /// <summary>
        /// 라이선스 갱신 타이머 (30분마다 갱신)
        /// </summary>
        private System.Windows.Forms.Timer licenseRefreshTimer;

        /// <summary>
        /// 라이선스 서버 연결 + 자동 갱신 타이머 시작.
        /// 후보를 순서대로 시도해 하나라도 성공하면 true, 전부 실패하면 MessageBox 표시 후 false 반환.
        /// </summary>
        private bool InitializeLicense()
        {
            List<string> failures;
            if (ConnectLicenseServer(out failures))
            {
                StartLicenseRefreshTimer();
                return true;
            }

            MessageBox.Show(
                "License Error: 라이선스 서버에 연결하지 못했습니다.\n\n" + string.Join("\n", failures),
                "라이선스 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        /// <summary>
        /// 후보 서버를 순서대로 시도한다. 직전 성공 서버가 있으면 그 서버부터 시도한다.
        /// 성공 시 <see cref="activeLicenseServerIndex"/>를 갱신하고 true 반환.
        /// </summary>
        /// <param name="failures">실패한 후보별 사유 (표시·로그용)</param>
        private bool ConnectLicenseServer(out List<string> failures)
        {
            failures = new List<string>();

            foreach (int i in GetLicenseServerOrder())
            {
                var server = LicenseServers[i];
                VIZCore3D.NET.Data.LicenseResults result;

                try
                {
                    result = vizcore3d.License.LicenseServer(server.Ip, server.Port);
                }
                catch (Exception ex)
                {
                    // 연결 자체가 예외로 죽어도 다음 후보를 시도한다.
                    failures.Add($"{server.Ip}:{server.Port} → 예외 {ex.Message}");
                    DiagLog($"[License] {server.Ip}:{server.Port} 예외 {ex.Message}");
                    continue;
                }

                if (result == VIZCore3D.NET.Data.LicenseResults.SUCCESS)
                {
                    activeLicenseServerIndex = i;
                    DiagLog($"[License] 인증 성공 {server.Ip}:{server.Port}" +
                            (failures.Count > 0 ? $" (폴백, 앞선 실패 {failures.Count}건)" : ""));
                    return true;
                }

                failures.Add($"{server.Ip}:{server.Port} → {result}");
                DiagLog($"[License] {server.Ip}:{server.Port} 실패 {result}");
            }

            activeLicenseServerIndex = -1;
            return false;
        }

        /// <summary>
        /// 시도 순서 — 직전 성공 서버가 있으면 그 서버를 맨 앞에 두고, 나머지는 정의 순서대로.
        /// </summary>
        private IEnumerable<int> GetLicenseServerOrder()
        {
            if (activeLicenseServerIndex >= 0 && activeLicenseServerIndex < LicenseServers.Length)
                yield return activeLicenseServerIndex;

            for (int i = 0; i < LicenseServers.Length; i++)
                if (i != activeLicenseServerIndex)
                    yield return i;
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
        /// 라이선스 갱신 타이머 이벤트 — 30분마다 서버 재연결.
        /// 직전 성공 서버가 죽어 있으면 다른 후보로 폴백한다. 전부 실패해도 앱은 계속 동작(다음 주기 재시도).
        /// </summary>
        private void LicenseRefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                List<string> failures;
                if (ConnectLicenseServer(out failures))
                {
                    var server = LicenseServers[activeLicenseServerIndex];
                    DiagLog($"[License] 갱신 성공 {server.Ip}:{server.Port}");
                }
                else
                {
                    DiagLog($"[License] 갱신 실패 — 후보 전부 불가 ({string.Join(" / ", failures)})");
                }
            }
            catch (Exception ex)
            {
                DiagLog($"[License] 갱신 오류: {ex.Message}");
            }
        }
    }
}
