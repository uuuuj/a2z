# Form1.License.cs — 코드 레퍼런스

**경로**: `A2Z/Form1.License.cs` (약 71 라인)

**책임**: VIZCore3D 라이선스 서버 초기 연결 + 30분 주기 자동 갱신 타이머 관리. 기존 `Form1.BOM.cs`에서 분리 (T-017, 2026-04-22).

---

## 메서드

### <a id="InitializeLicense"></a>InitializeLicense
- **라인**: L22~L34
- **시그니처**: `private bool InitializeLicense()`
- **호출처**: [Form1.BOM.cs `Vizcore3d_OnInitializedVIZCore3D`](./form1-bom.md#vizcore3d-oninitialized) (L137)
- **핵심**: `License.LicenseServer("127.0.0.1", 8901)` → 실패 시 MessageBox + false, 성공 시 `StartLicenseRefreshTimer()` 호출 + true
- **반환**: 성공 여부 (호출처에서 실패 시 early return)

### <a id="StartLicenseRefreshTimer"></a>StartLicenseRefreshTimer
- **라인**: L39~L45
- **시그니처**: `private void StartLicenseRefreshTimer()`
- **핵심**: `Timer` 생성 → Interval 30분 → Tick 이벤트 구독 → Start

### <a id="LicenseRefreshTimer_Tick"></a>LicenseRefreshTimer_Tick
- **라인**: L50~L69
- **시그니처**: `private void LicenseRefreshTimer_Tick(object sender, EventArgs e)`
- **핵심**: `License.LicenseServer` 재호출 → 실패/성공/예외 모두 `Debug.WriteLine`만 (MessageBox 없음 — 작업 방해 방지)

---

## 필드

| 필드 | 타입 | 용도 |
|---|---|---|
| `licenseRefreshTimer` | `System.Windows.Forms.Timer` | 30분 주기 갱신 (Form1.License.cs L15) |

---

## VIZCore3D API 사용

- `vizcore3d.License.LicenseServer(ip, port)` — 초기 연결 + 갱신 모두 같은 API
- 반환값: `VIZCore3D.NET.Data.LicenseResults` (`SUCCESS` 외는 실패)

---

## 관련 문서
- 분리 전 위치: [Form1.BOM.cs](./form1-bom.md)
- 흐름 문서: [features/bom/vizcore3d-initialized.md](../features/bom/vizcore3d-initialized.md)
- 분리 배경: TASKS T-017 (2026-04-22 완료)
