---
title: Sheet1 부재 이름 부여 기준 (Z-MAX 정렬) 검증 보고서
last_updated: 2026-05-05
related_task: T-056
type: technical-note
---

# Sheet1 부재 이름 부여 기준 (Z-MAX 정렬) 검증 보고서 (T-056)

회사 doc "완료 5" + "수정 후 확인 필요 2"의 동일한 의문에 답하기 위한 코드 트레이스 결과.

## 1. 회사 의문 (원문)

> 전체 ITem의 기준 부재 이름 부여 기준 확인 필요 — GetPartialNode API를 사용하여 Node 리스트 생성 후 Osnap 추출 후 각 부재의 최상단 Osnap을 기준으로 Z-MAX 순서대로 내림차순 정렬. 근데 이거 맞게 처리된건지 확인 필요.

## 2. 결론 (한 줄)

**부분 일치** — 정렬 데이터 출처가 **`BBox.MaxZ`** 로 회사 명세의 **`max(Osnap.Z)`** 와 다름. 직립 H빔·평판 등 일반 철골 형상에서는 두 값이 사실상 동등하므로 정렬 결과가 같으나, 경사 부재·곡면 Body·자유형 가공품에서는 수 mm 단위 차이로 정렬 순서가 흔들릴 가능성이 있음. 일반적인 A2Z 데이터셋에서 영향 빈도는 **낮음~중간**.

## 3. 현재 코드의 정렬 기준

| 항목 | 값 |
|---|---|
| 정렬 키 필드 | `BOMData.MaxZ` (`Models.cs:67`, `float` 단일 스칼라) |
| 데이터 출처 | **BBox API** — `vizcore3d.Object3D.GetBoundBox(nodeIndices, false)` (`Form1.BOM.cs:666`) |
| MaxZ 대입 | `bom.MaxZ = bbox.MaxZ;` (`Form1.BOM.cs:675`) |
| 정렬 라인 | `bomList.Sort((a, b) => b.MaxZ.CompareTo(a.MaxZ));` (`Form1.BOM.cs:735`) |
| 정렬 방향 | 내림차순 (Z-MAX → Z-MIN) — 명세와 동일 |

같은 `CollectBOMData` 메서드 안에서 `GetOsnapPoint(node.Index)`도 호출되지만(`Form1.BOM.cs:688`), 이는 **CIRCLE 부재의 반지름(`bom.CircleRadius`) 산출 전용**이며 정렬 키에는 관여하지 않음.

## 4. 회사 명세 재진술

- 정렬 키: 각 부재의 **최상단 Osnap의 Z값** = `max(Osnap.Z for osnap in osnapList(node))`
- 데이터 출처: `GetPartialNode` → 노드별 `GetOsnapPoint` → 노드별 Osnap.Z의 최댓값
- 정렬 방향: Z-MAX 내림차순 (코드와 동일)

## 5. 두 기준의 동등성 분석

형식적 관계: BBox는 mesh vertex 전체의 AABB, Osnap은 형상의 특징점(VERTEX/CENTER/MID/CIRCLE 등) 부분집합. 따라서 **항상 `max(Osnap.Z) ≤ BBox.MaxZ`**, 일반적으로 부등호.

| 케이스 | BBox.MaxZ vs max(Osnap.Z) | 정렬 영향 |
|---|---|---|
| **직립 H빔 / I빔 / 채널 / 앵글** | 동등 (상단 플랜지 코너가 VERTEX Osnap) | 영향 없음 |
| **수평 평판 / 플레이트** | 동등 | 영향 없음 |
| **사각/직사각 부재** | 동등 (코너가 모두 VERTEX) | 영향 없음 |
| **직립 원기둥 (Z축)** | Osnap이 CIRCLE Center일 경우 차이 ≈ r (반지름)<br/>VERTEX Start.Z를 잡으면 동등 | 케이스별 |
| **경사 빔 (브레이싱·지붕재)** | BBox.MaxZ가 수 mm~수십 mm 더 큼<br/>(Osnap이 한 단계 아래 코너만 잡으면) | 1~2칸 흔들림 가능 |
| **곡면 Body (파이프 엘보, 라운드 캡)** | BBox는 곡면 외피 끝, Osnap은 CENTER/포인트 | 차이 발생 |
| **비대칭 자유형 가공품** | 최상단이 vertex가 아닌 면 위 점일 때 Osnap에 안 잡힘 | 차이 발생 |

**A2Z 일반 프로젝트의 절대다수가 직립 H빔·플레이트·앵글**이므로 두 기준이 같은 결과를 냄. 경사 부재가 다수 섞인 도면에서만 정렬 순서 1~2칸 변동 가능성.

## 6. 최종 결정 (2026-05-05)

**사용자 결정**: **현행 BBox.MaxZ 유지**.

**이유**: A2Z 일반 데이터셋(직립 H빔·플레이트·앵글 등)에서 두 기준의 정렬 결과가 동등하며, 차이가 발생하는 경사·곡면 케이스도 정렬 1~2칸 변동 수준으로 실용적 영향이 작음. 회사 회신 시 본 보고서 § 7 단답을 그대로 사용하여 "BBox 기준이지만 일반 형상에선 명세와 동일 결과"임을 설명. 차후 회사가 Osnap 기준 자체를 강하게 요구하면 그때 신규 작업으로 변경(`Form1.BOM.cs:688`의 `osnapList.Max(o => o.Vertex.Z)` 1줄 교체) 진행.

## 6.1 권장 액션 (옵션 — 참고용)

| 시나리오 | 권장 |
|---|---|
| 회사가 "Osnap 기준" 자체를 강하게 요구 | **Osnap 기준으로 변경**. `Form1.BOM.cs:688`에서 이미 `osnapList`를 가져오고 있어 `osnapList.Max(o => o.Vertex.Z)` 한 줄로 정렬 키 교체 가능. BBox는 fallback으로 유지 (osnap이 비었을 때) |
| 회사 의도가 "위→아래 순서 보장"이면 충분 | **현 BBox 기준 유지**. docs로만 회사에 회신 — "BBox가 일반 형상에서 Osnap.Z와 동등하며, A2Z 데이터셋에선 정렬 결과 차이가 거의 없음" |
| 결정 보류 | **본 보고서로 회사에 정확한 동작 알리고**, 차이 발생 시 변경 요청 받기 |

수정이 필요해질 경우 후속 작업 ID 신설 권장 (예: `T-057 — Sheet1 정렬 키를 Osnap.Z로 전환`).

## 7. 회사 doc 갱신용 단답

> 현재 코드는 `vizcore3d.Object3D.GetBoundBox`로 얻은 `BBox.MaxZ`를 키로 내림차순 정렬하고 있으며, 회사 명세의 "최상단 Osnap.Z" 기준과는 데이터 출처가 다릅니다. 다만 직립 H빔·평판 등 코너가 Osnap VERTEX로 잡히는 일반 형상에서는 `BBox.MaxZ == max(Osnap.Z)`가 성립해 정렬 결과가 동일합니다. 차이는 경사 부재·곡면 Body·자유형 가공품에서 수 mm 단위로 발생할 수 있으며, A2Z의 일반 철골 데이터셋에서는 정렬 순서가 뒤집힐 빈도가 낮습니다. 기능적으로 명세와 부분 일치 상태이므로, 회사가 Osnap 기준 자체를 요구하면 `Form1.BOM.cs:688`에서 이미 호출 중인 `osnapList`의 Z 최댓값으로 정렬 키를 교체하면 됩니다(BBox는 fallback). 그렇지 않다면 현재 구현 유지로 충분합니다.

## 8. 인용 코드

| 위치 | 역할 |
|---|---|
| `Form1.BOM.cs:631` | `vizcore3d.Object3D.GetPartialNode(false, false, true)` — Node 리스트 생성 |
| `Form1.BOM.cs:666` | `BoundBox3D bbox = vizcore3d.Object3D.GetBoundBox(nodeIndices, false);` |
| `Form1.BOM.cs:675` | `bom.MaxZ = bbox.MaxZ;` — BBox 데이터 대입 |
| `Form1.BOM.cs:688` | `var osnapList = vizcore3d.Object3D.GetOsnapPoint(node.Index);` — CircleRadius용, 정렬 미관여 |
| `Form1.BOM.cs:735` | `bomList.Sort((a, b) => b.MaxZ.CompareTo(a.MaxZ));` — 정렬 적용 |
| `Models.cs:67` | `public float MaxZ { get; set; }` — 필드 정의 |

## 9. 변경 이력

| 날짜 | 변경 |
|---|---|
| 2026-05-02 | 최초 작성 — 회사 doc 의문 답변용 (T-056) |
| 2026-05-05 | § 6 최종 결정 추가 — 사용자 BBox 유지 결정. 회사 회신은 § 7 단답 사용 |
