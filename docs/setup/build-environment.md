---
title: 빌드 환경 셋업
last_updated: 2026-05-02
---

# 빌드 환경 셋업

A2Z 프로젝트를 새 PC에서 빌드하거나, 빌드가 깨졌을 때 복구하는 방법.

## TL;DR — 빌드가 안 될 때 30초 진단

PowerShell에서 레포 루트(`a2z\`) 기준:

```powershell
.\scripts\check-build-env.ps1
```

진단 결과의 지시에 따라 dll을 배치하면 됩니다.

---

## 의존성 두 종류

A2Z는 VIZCore3D SDK의 **두 dll**이 필요하고, 각각 다른 위치·다른 방식으로 csproj가 처리합니다.

### 1. 매니지드 dll: `VIZCore3D+.NET.dll`

| 항목 | 값 |
|---|---|
| 종류 | .NET 어셈블리 (Managed) |
| 요구 버전 | **1.0.26.325 이상** (코드가 신규 멤버 사용. `docs/README.md`의 SDK 버전과 동일) |
| 위치 | csproj가 절대 경로로 참조 — `..\..\..\..\내 드라이브\1. 회사\Digital Wave\Vibe 3D Lab\3. API 적용 참조 파일 (.dll)\03.VIZCore3D+.NET\VIZCore3D+.NET.dll` |
| 실효 경로 | `C:\Users\<사용자>\내 드라이브\1. 회사\...\03.VIZCore3D+.NET\VIZCore3D+.NET.dll` |
| 출처 | 회사 구글 드라이브 동기화 |
| 역할 | C#에서 호출하는 .NET API surface (`Drawing2DManager`, `View.FitToView`, `Data.TemplateTableData` 등) |

### 2. Native Interop dll: `VIZCore3D.NET.Interop.dll`

| 항목 | 값 |
|---|---|
| 종류 | Win32 Native DLL (Unmanaged) |
| 위치 | 레포 루트 — `a2z\VIZCore3D.NET.Interop.dll` |
| csproj 처리 | `<None Include="..\VIZCore3D.NET.Interop.dll">` + `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` (빌드 시 `bin\Debug\`로 자동 복사) |
| 출처 | SDK 패키지 함께 제공. **매니지드 dll과 같은 짝**으로 받아야 ABI 호환 |
| 역할 | 실제 3D 엔진 native 코드. 매니지드 dll이 P/Invoke로 호출 |

> **두 dll 모두 git에 커밋되지 않습니다** (`.gitignore`로 차단). 각 PC에서 별도 수급.

---

## 신규 PC 셋업 절차

1. 레포 클론 또는 `git pull`
2. **회사 구글 드라이브 동기화 확인** — `C:\Users\<사용자>\내 드라이브\1. 회사\Digital Wave\Vibe 3D Lab\3. API 적용 참조 파일 (.dll)\03.VIZCore3D+.NET\` 안에 `VIZCore3D+.NET.dll`이 1.0.26.325 이상으로 떠 있어야 함. 옛 버전이거나 placeholder면 동기화를 강제하거나 노트북·회사에서 직접 가져와 덮어씀
3. **Interop dll 복사** — 노트북·회사 SDK 패키지에서 `VIZCore3D.NET.Interop.dll`을 받아 레포 루트(`a2z\`)에 둠
4. `scripts\check-build-env.ps1` 실행 — 모든 항목 OK 확인
5. Visual Studio에서 `A2Z.sln` 열고 빌드

---

## 자주 발생하는 문제 (Troubleshooting)

### A. 컴파일 에러 `CS0234: 'VIZCore3D.NET.Data' 네임스페이스에 'XXX' 형식이 없습니다`

- **원인**: 매니지드 dll이 코드보다 옛 버전 (예: 1.0.26.130). 신규 멤버(`TemplateTableData`, `GridStructure`, `Drawing2D_ModelViewKind`, `RenderTemplateOnGridStructure`, `RescaleObject` 등)가 없음
- **확인**:
  ```powershell
  (Get-Item "C:\Users\$env:USERNAME\내 드라이브\1. 회사\Digital Wave\Vibe 3D Lab\3. API 적용 참조 파일 (.dll)\03.VIZCore3D+.NET\VIZCore3D+.NET.dll").VersionInfo.FileVersion
  ```
- **해결**: 노트북·회사에서 1.0.26.325 이상 dll을 가져와 위 경로에 덮어쓰기

### B. 빌드 에러 `MSB3030: VIZCore3D.NET.Interop.dll 파일을 찾을 수 없으므로 복사할 수 없습니다`

- **원인**: 레포 루트(`a2z\`)에 Interop dll 누락
- **확인**: `Test-Path .\VIZCore3D.NET.Interop.dll`
- **해결**: SDK 패키지 또는 노트북에서 `VIZCore3D.NET.Interop.dll`을 받아 레포 루트에 둠

### C. csproj diff에 절대 경로가 들어가 있음

- **원인**: Visual Studio가 dll을 찾지 못해 reference를 자동으로 다른 위치(GAC, 다른 PC의 절대 경로)로 갱신
- **해결 우선순위**:
  1. 임시 변경이면 `git checkout -- A2Z\A2Z.csproj`로 원복하고, dll을 csproj가 가리키는 정상 위치에 두기
  2. 의도적 경로 변경이면 모든 PC에서 동일 경로가 유효한지 확인 후 commit
  3. 경로가 한글·공백·괄호를 많이 포함하면 MSBuild가 깨질 수 있으므로 가능한 단순화

### D. 런타임 `EntryPointNotFoundException` 또는 `DllNotFoundException`

- **원인**: 매니지드 dll과 Interop dll의 **ABI(함수 시그니처) 버전 불일치**. 빌드는 통과해도 P/Invoke 시점에 깨짐
- **해결**: 두 dll을 **같은 SDK 패키지**에서 받은 짝으로 맞추기. 노트북에 정상 동작하는 짝이 있으면 둘 다 같이 가져오기

---

## 환경 동기화 체크리스트

새 PC로 옮길 때 / SDK 버전 갱신 시 / 빌드 사고 후 매번:

- [ ] 노트북·데스크톱 양쪽 매니지드 dll 버전이 **1.0.26.325 이상**, 또한 **동일 버전**
- [ ] Interop dll이 양쪽 a2z 루트에 같은 짝(같은 SDK 패키지 출처)으로 존재
- [ ] `git diff A2Z\A2Z.csproj` 결과가 비어있음 (PC별 절대 경로 갱신 없음)
- [ ] `scripts\check-build-env.ps1` 실행 결과 모든 항목 OK
- [ ] Visual Studio에서 빌드 성공
- [ ] 짧은 실행 테스트 (모델 1개 열고 닫기)로 런타임 ABI 호환 확인

---

## 관련 파일

- 자가 진단 스크립트: [`scripts/check-build-env.ps1`](../../scripts/check-build-env.ps1)
- 프로젝트 정보: [`docs/README.md`](../README.md) (SDK 버전 명시)
- csproj: [`A2Z/A2Z.csproj`](../../A2Z/A2Z.csproj)
- ignore 정책: [`.gitignore`](../../.gitignore)

## 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-05-02 | 최초 작성 — 데스크톱 빌드 사고 진단 후 정비 (매니지드 1.0.26.130→1.0.26.325 교체, Interop dll 누락 보강, 자가 진단 스크립트 도입) |
