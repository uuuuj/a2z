---
last_updated: 2026-07-23
---

# 빌드 환경 셋업 · 복구 가이드

A2Z 프로젝트를 새 PC에서 빌드하거나, 빌드가 깨졌을 때 복구하는 방법.

## TL;DR — 빌드가 안 될 때 30초 진단

PowerShell에서 레포 루트(`a2z\`) 기준:

```powershell
Get-ChildItem .\lib\*.dll | Select-Object Name, Length
(Get-Item .\lib\VIZCore3D+.NET.dll).VersionInfo.FileVersion
```

`lib\`에 DLL 6개가 있고 매니지드 dll 버전이 요구 버전(아래) 이상이면 클린 빌드가 됩니다. 대용량 2개가 없으면 아래 절차로 배치.

---

## 의존성 구조 (2026-07-12 lib\ 이관 후)

모든 VIZCore DLL은 **레포 루트 `lib\`** 에서 참조합니다. csproj가 빌드 시 전부 `bin\Debug\`로 복사하므로, bin·obj를 지워도 클린 빌드가 됩니다 (bin\Debug 의존 없음, 구글드라이브 절대 경로 없음).

| DLL | 종류 | git 커밋 | csproj 처리 |
|---|---|---|---|
| `VIZCore3D+.NET.dll` (~222MB) | .NET 매니지드 API | ❌ (100MB 한도) — **각 PC lib\에 별도 배치** | `<Reference>` HintPath + CopyLocal |
| `VIZCore3D.NET.Interop.dll` (~88MB) | Native 엔진 부트스트랩 사본 | ❌ — **각 PC lib\에 기존 파일 유지** | `<None>` + PreserveNewest 복사 후 723 관리 DLL이 실행 시 대응 버전 추출 |
| `ShdCore.dll` / `SQLite.Interop.dll` / `ShCux.dll` / `ShCuxRev.dll` | 보조 | ✅ 커밋됨 (자동 수급) | `<None>` + PreserveNewest 복사 |

- **요구 버전**: `VIZCore3D+.NET.dll` **1.0.26.723 이상** (2026-07-23부터 — ISO 풍선 영역 정렬 API 사용, 구버전은 해당 멤버가 없어 컴파일 불가). XML 문서(`lib\VIZCore3D.NET.xml`)도 같은 배포본으로 교체한다 (SDK 검증용, git 미커밋).
- **723 런타임 네이티브 정합**: 이번 배포 폴더는 관리 DLL·XML만 제공한다. 앱 시작 시 `ModuleInitializer.Run()`이 관리 DLL에 내장된 대응 Interop·ShdCore를 실행 폴더에 추출한다. 실행 후에도 구버전이 남거나 `EntryPointNotFoundException`이 발생하면 A2Z 프로세스를 종료하고 `bin\Debug`를 정리한 뒤 다시 빌드·실행한다.
- 배포 출처: 회사 구글 드라이브 `...\3. API 적용 참조 파일 (.dll)\03.VIZCore3D+.NET\` 의 버전 폴더.

## 신규 PC 셋업 절차

1. 레포 클론 또는 `git pull`
2. SDK 1.0.26.723 배포 폴더의 `VIZCore3D+.NET.dll`과 `VIZCore3D.NET.xml`을 **`a2z\lib\`** 에 복사
3. 내려받은 파일의 차단을 해제: `Unblock-File .\lib\VIZCore3D+.NET.dll, .\lib\VIZCore3D.NET.xml`
4. 기존 `lib\VIZCore3D.NET.Interop.dll`과 보조 DLL은 유지한다. 723 배포 폴더는 관리 DLL 내부에 대응 네이티브 엔진을 포함하며 `ModuleInitializer.Run()`이 실행 시 추출한다
5. Visual Studio에서 `A2Z.sln` 빌드 → `bin\Debug\`에 exe + DLL 6개 + 이미지(assets) 자동 생성

## 자주 발생하는 문제

| 증상 | 원인 → 해결 |
|---|---|
| `CS0234` / `CS1501` (멤버·시그니처 없음) | 매니지드 dll이 구버전 → lib\ dll을 요구 버전으로 교체 |
| `MSB3030` (Interop 복사 실패) | `lib\VIZCore3D.NET.Interop.dll` 누락 → 배치 |
| 런타임 `EntryPointNotFoundException` | 매니지드·Interop ABI 짝 불일치 → 같은 패키지 짝으로 |
| `MSB3027/3021` (exe 복사 실패, 잠김) | A2Z 앱이 실행 중 → 앱 종료 후 재빌드 |
| csproj diff에 절대 경로 등장 | VS가 참조를 임의 갱신 → `git checkout -- A2Z\A2Z.csproj` 후 lib\ 배치 확인 |

## 관련 파일

- csproj: [`A2Z/A2Z.csproj`](../../A2Z/A2Z.csproj)
- ignore 정책: [`.gitignore`](../../.gitignore) (대용량 DLL 차단·소형 DLL 허용 규칙)
- 도면 이미지 리소스: `assets\` (North_Arrow·ISO_North_Arrow·Logo — csproj Content로 실행 폴더 자동 복사)

## 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-07-23 | 요구 SDK를 1.0.26.723으로 상향하고 관리 DLL·XML 배치, 다운로드 차단 해제, 내장 네이티브 추출 방식 반영 |
| 2026-07-21 | **lib\ 이관 반영 전면 개정** — 구글드라이브 절대 경로·레포 루트 Interop·`scripts/check-build-env.ps1` 시절 내용 폐기 (스크립트도 구식화로 삭제). 요구 버전 1.0.26.716, 이미지 리소스 `assets\` 이동 반영 |
| 2026-05-02 | 최초 작성 — 데스크톱 빌드 사고 진단 후 정비 (자가 진단 스크립트 도입) |
