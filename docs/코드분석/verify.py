# -*- coding: utf-8 -*-
"""
파일별/ 문서가 코드와 어긋나지 않는지 기계로 검증한다.

실행:
    python docs/코드분석/verify.py              # 전부
    python docs/코드분석/verify.py Form1.BOM    # 하나만

검사하는 것 — 사람이 볼 필요 없는 것들만
    1. 줄번호   문서의 `메서드명` (L123) 이 실제 선언 위치와 맞나 (±2줄)
    2. 파일:줄  참조한 줄 번호가 그 파일 길이를 넘지 않나
    3. SDK API  vizcore3d.….Xxx( 의 Xxx 가 VIZCore3D.NET.xml 에 있나
    4. 링크     문서가 건 상대 링크의 대상 파일이 있나
    5. 틀       mermaid 플로우차트 · 6절 제목 · 부록이 있나

원칙: **오탐을 내느니 놓친다.** 지적이 시끄러우면 아무도 안 본다.
      지역변수·파일명·범위 표기(L56~62)는 판단하지 않고 건너뛴다.

검사하지 않는 것 — 사람/AI 가 봐야 하는 것
    · 알고리즘 설명이 맞는가
    · 6절 판단(뗄 수 있다/없다)이 타당한가
    · 빠뜨린 게 있는가
  → 그건 교차검증(상대 문서를 상대가 읽는다)의 몫이다.
"""
import re
import sys
import collections
from pathlib import Path
from urllib.parse import unquote

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
SRC = ROOT / "A2Z"
DOCS = HERE / "파일별"
SDK_XML = ROOT / "VIZCore3D.NET.xml"

RE_METHOD = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)?'
    r'(?:private|public|protected|internal)\s+'
    r'(?:static\s+|async\s+|override\s+|virtual\s+|sealed\s+|partial\s+|extern\s+|unsafe\s+|new\s+)*'
    r'(?:[\w<>\[\],\.\?]+(?:\s*,\s*[\w<>\[\],\.\?]+)*\s+)'
    r'([A-Za-z_]\w*)\s*\('
)
RE_TYPE = re.compile(r'^\s*(?:public|internal|private|protected)?\s*'
                     r'(?:static\s+|sealed\s+|abstract\s+|readonly\s+)*'
                     r'(?:partial\s+)?(?:class|struct|enum|interface)\s+(\w+)')
KEYWORDS = {'if', 'while', 'for', 'foreach', 'switch', 'catch', 'using',
            'lock', 'return', 'get', 'set', 'yield', 'fixed', 'nameof'}


def load_code():
    """메서드/필드/타입 위치와 파일별 줄 목록"""
    lines, decl = {}, collections.defaultdict(list)   # name -> [(file, line)]
    for p in sorted(SRC.rglob("*.cs")):
        if any(x in p.parts for x in ("obj", "bin")):
            continue
        ls = p.read_text(encoding="utf-8", errors="replace").splitlines()
        lines[p.name] = ls
        for i, ln in enumerate(ls):
            m = RE_METHOD.match(ln)
            if m and m.group(1) not in KEYWORDS:
                decl[m.group(1)].append((p.name, i + 1))
            m = RE_TYPE.match(ln)
            if m:
                decl[m.group(1)].append((p.name, i + 1))
    # 필드·상수도 이름만 수집 (위치 검증 대상은 아님)
    names = set(decl)
    for p, ls in lines.items():
        for ln in ls:
            for m in re.finditer(r'(?:private|public|internal|protected)\s+'
                                 r'(?:static\s+|readonly\s+|const\s+)*'
                                 r'[\w<>\[\],\.\?\(\)]+\s+([_a-zA-Z]\w*)\s*(?:=|;|\{)', ln):
                names.add(m.group(1))
    return lines, decl, names


def load_sdk():
    if not SDK_XML.exists():
        return None
    txt = SDK_XML.read_text(encoding="utf-8", errors="replace")
    # 마지막 멤버 이름만 모은다 (매니저 프로퍼티 이름이 XML 클래스명과 다르므로)
    return {m.split('.')[-1]
            for m in re.findall(r'<member name="[MPFTE]:VIZCore3D\.NET\.([^"(]+)', txt)}


def doc_target_file(doc: Path):
    """문서 이름 -> 대상 .cs 파일명"""
    stem = doc.stem
    if stem == "Models":
        return ["Models.cs", "MfgViewPose.cs"]
    return [stem + ".cs"]


def check(doc: Path, lines, decl, names, sdk):
    text = doc.read_text(encoding="utf-8", errors="replace")
    targets = doc_target_file(doc)
    issues = []

    # ── 1. 줄번호 ─────────────────────────────────────────────────
    #   `이름` (L123) / `이름` L123 만 본다.
    #   `이름` L56~62 처럼 범위를 쓴 건 "그 메서드 안의 56~62줄" 이라는 뜻이라
    #   선언 위치와 비교하면 안 된다. 이름이 코드에 없으면 지역변수·파일명·SDK
    #   멤버일 수 있어 아예 건너뛴다 — 여기서 추측하면 오탐만 쌓인다.
    #   허용 표기는 셋뿐이다 — `Name` (L123) · `Name` L123 · `Name` (`File.cs` L123)
    #   사이에 다른 글자가 끼면(예: "`Name`을 호출한다. 바로 뒤 L978에서") 그 줄번호는
    #   선언이 아니라 호출 지점을 가리키므로 판단하지 않는다.
    PAT = re.compile(
        r'`([A-Za-z_]\w*)`\s*'
        r'(?:\(\s*`?(\w+\.cs)`?\s+)?'
        r'\(?L(\d+)\)?(?![\d~\-])')
    for m in PAT.finditer(text):
        name, fileref, ln = m.group(1), m.group(2), int(m.group(3))
        if name not in decl:
            continue
        want = [fileref] if fileref else targets
        hits = [(f, l) for f, l in decl[name] if f in want] or decl[name]
        if not any(abs(l - ln) <= 2 for _, l in hits):
            got = ", ".join(f"{f}:{l}" for f, l in hits[:3])
            issues.append(("줄번호", f"`{name}` 문서 L{ln} → 실제 선언 {got}"))

    # ── 2. 파일:줄 참조가 파일 길이를 넘지 않나 ────────────────────
    for f, ln in re.findall(r'(\w+\.cs)[ :]+L?(\d+)', text):
        if f in lines and int(ln) > len(lines[f]):
            issues.append(("줄번호", f"{f} L{ln} → 그 파일은 {len(lines[f])}줄뿐"))

    # ── 3. SDK API ────────────────────────────────────────────────
    #   vizcore3d.A.B.C(...) 에서 A·B 는 매니저 프로퍼티 이름이라 XML 의 클래스
    #   이름과 다르다 (예: vizcore3d.Drawing2D.Template.ImportExcelWithData →
    #   XML 은 Manager.Drawing2DTemplateManager.ImportExcelWithData). 그래서
    #   마지막 멤버 이름만 대조한다.
    if sdk:
        for api in sorted(set(re.findall(r'`?vizcore3d\.((?:[A-Za-z_]\w*\.)+[A-Za-z_]\w*)', text))):
            leaf = api.split('.')[-1]
            if leaf not in sdk:
                issues.append(("SDK", f"`vizcore3d.{api}` — XML 에 `{leaf}` 가 없다"))

    # ── 4. 링크 ───────────────────────────────────────────────────
    for link in re.findall(r'\]\((\.[^)#]+)\)', text):
        tgt = (doc.parent / unquote(link)).resolve()
        if not tgt.exists():
            issues.append(("링크", f"{link} → 대상 없음"))

    # ── 5. 틀 준수 ────────────────────────────────────────────────
    if "```mermaid" not in text:
        issues.append(("틀", "2절 mermaid 플로우차트가 없다"))
    if "다시 짠다면" not in text:
        issues.append(("틀", "6절 '책임과 결합 — 다시 짠다면' 이 없다"))
    else:
        for need in ("① ", "② ", "③ ", "④ "):
            if need not in text:
                issues.append(("틀", f"6절에 {need.strip()} 항목이 없다"))
                break
    if "지나가며" not in text and "부록" not in text:
        issues.append(("틀", "부록('지나가며 눈에 띈 것')이 없다 — 없으면 '없음'이라도 적을 것"))

    return issues


def main():
    only = sys.argv[1] if len(sys.argv) > 1 else None
    lines, decl, names = load_code()
    sdk = load_sdk()
    if sdk is None:
        print("⚠ VIZCore3D.NET.xml 없음 — SDK 검사 건너뜀 (.gitignore 대상이라 로컬에만 있다)")

    docs = sorted(DOCS.glob("*.md"))
    if only:
        docs = [d for d in docs if only.lower() in d.stem.lower()]
    if not docs:
        print("대상 문서 없음"); return

    total = 0
    for d in docs:
        issues = check(d, lines, decl, names, sdk)
        total += len(issues)
        mark = "✅" if not issues else "🔴"
        print(f"\n{mark} {d.name}  ({len(issues)}건)")
        by = collections.defaultdict(list)
        for k, msg in issues:
            by[k].append(msg)
        for k in ("줄번호", "SDK", "링크", "틀"):
            for msg in by.get(k, []):
                print(f"   [{k}] {msg}")

    print(f"\n{'─'*60}\n문서 {len(docs)}개 / 지적 {total}건")
    if total:
        print("\n줄번호·메서드는 코드가 바뀌어 밀린 것일 수 있다. 고치기 전에 실제 코드를 볼 것.")


if __name__ == "__main__":
    main()
