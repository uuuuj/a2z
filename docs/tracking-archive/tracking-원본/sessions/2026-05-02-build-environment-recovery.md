# 2026-05-02 — 빌드 환경 정비 (PC 간 동기화 사고 + 대책 수립)

## 주제
데스크톱에서 빌드 실패 → 원인 분석(VIZCore3D dll 다중 동기화 문제) → 정상화 → 재발 방지 가이드·자가 진단 스크립트 정비.

## 배경
노트북에서 잘 빌드되던 프로젝트가 데스크톱에서 빌드 실패. 직접 원인은 셋이었는데, 모두 **vendor dll 의존성 PC 간 동기화 부족**에서 비롯.

## 진단 (3중 사고)

| # | 사고 | 증상 | 진짜 원인 |
|---|---|---|---|
| 1 | csproj가 노트북의 절대 경로로 갱신 | `git status: A2Z/A2Z.csproj modified` | 노트북에서 VS가 dll reference를 자동 갱신, 한글·공백·괄호 다수 절대 경로가 working tree에 잔존 |
| 2 | 매니지드 dll 버전 불일치 | `error CS0234` 다수 — `TemplateTableData`, `GridStructure`, `Drawing2D_ModelViewKind`, `RenderTemplateOnGridStructure`, `RescaleObject` 등 미존재 | 데스크톱 구글 드라이브의 dll이 1.0.26.130 (구버전). 코드는 1.0.26.325 이상의 신규 SDK 멤버 사용 |
| 3 | Interop dll 부재 | `error MSB3030: VIZCore3D.NET.Interop.dll 파일을 찾을 수 없으므로 복사할 수 없습니다` | csproj 새 항목 `<None Include="..\VIZCore3D.NET.Interop.dll">`이 요구하는 native dll이 데스크톱 a2z 루트에 없음 |

## 한 일

### 1. 정상화
- 사용자가 노트북에서 1.0.26.325 매니지드 dll을 데스크톱 구글 드라이브 03 폴더로 업로드 → 동기화 후 빌드 가능
- 데스크톱의 다른 a2z 사본(`HYI_2_agent-main\lib\VIZCore3D.NET.Interop.dll`)을 a2z 루트로 복사
- 빌드 성공 확인 — `A2Z.exe` 산출, 0 errors

### 2. 재발 방지 정비
- **신규** [`docs/setup/build-environment.md`](../../setup/build-environment.md) — 의존성 정의(매니지드 + Interop), 신규 PC 셋업 절차, 트러블슈팅 4 케이스(A~D), 환경 동기화 체크리스트
- **신규** [`scripts/check-build-env.ps1`](../../../scripts/check-build-env.ps1) — 매니지드 dll 버전·Interop dll 존재·csproj 무결성·로컬 csproj diff 자가 진단. 종료 코드로 CI/훅 연계 가능
- **갱신** [`docs/README.md`](../../README.md) — 개괄 섹션에 가이드 링크 추가
- **갱신** [`.gitignore`](../../../.gitignore) — vendor 바이너리 코멘트를 새 구조(매니지드 외부 참조 + Interop a2z 루트)에 맞게 수정
- **갱신** [`STATUS.md`](../../../STATUS.md) — 현재 상태(빌드 정상, Interop ABI 호환은 사용자 실기 검증 대기) 반영

### 3. 사용자 워크플로우 변경
- csproj가 절대 경로로 갱신되는 것은 막을 수 없음 (VS 동작). **사후 탐지가 핵심**
- `scripts\check-build-env.ps1`이 csproj diff 존재 여부를 경고로 출력 → 사용자가 사고 조기 인지 가능
- 새 PC 셋업·SDK 업그레이드 시마다 체크리스트 따라가는 흐름 정착

## 영향 범위
- **코드 변경**: 없음
- **csproj 변경**: 사용자가 노트북에서 적용한 절대 경로 + Interop 복사 항목 그대로 유지 (의도적, 본 세션은 건드리지 않음)
- **신규 파일**: `docs/setup/build-environment.md`, `scripts/check-build-env.ps1`, 본 세션 요약
- **갱신 파일**: `docs/README.md`, `.gitignore`, `STATUS.md`
- **잔재 파일**: `lib/VIZCore3D+.NET.dll` (1.0.26.130 옛 버전), `lib/VIZCore3D.NET.xml` — csproj는 더 이상 lib/ 미참조. 후속 정리 후보(아래 이어갈 지점 4번)

## 미정 / 한계

- **런타임 ABI 호환성 미검증**: 데스크톱 Interop dll은 다른 사본(`HYI_2_agent-main`)에서 가져왔으므로 매니지드 1.0.26.325와 함수 시그니처가 정확히 매치된다는 보장 없음. 빌드는 통과했으나 **실행 시 P/Invoke 단계에서 깨질 가능성**. 실기 확인 필요
- 노트북에서 환경 검증은 사용자가 pull 후 직접 진행

## 이어갈 지점

다음 세션 시작 시 확인할 것:

1. **빌드 산출물(A2Z.exe) 실행 테스트** — 모델 1개 열고 기본 동작 확인. P/Invoke 정상이면 OK
2. 만약 런타임 에러(`EntryPointNotFoundException` / `DllNotFoundException`)면 노트북의 `a2z\VIZCore3D.NET.Interop.dll`을 그대로 가져와 데스크톱 a2z 루트에 덮어쓰기
3. 정상 동작 확인 후 `/commit`으로 환경 정비 변경사항 커밋·push
4. 노트북에서 `git pull` 후 `.\scripts\check-build-env.ps1` 실행 → 환경 차이 확인
5. `lib/` 폴더 후속 정리 결정 — 통째로 삭제할지, 백업으로 유지할지 (현재 csproj 미참조)

## 참고 링크
- 가이드: [docs/setup/build-environment.md](../../setup/build-environment.md)
- 진단 스크립트: [scripts/check-build-env.ps1](../../../scripts/check-build-env.ps1)
- 관련 csproj: [A2Z/A2Z.csproj](../../../A2Z/A2Z.csproj)
