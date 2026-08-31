# -*- coding: utf-8 -*-
"""
docs/코드분석/자동생성/ 3개 문서를 코드에서 다시 뽑는다.

실행:
    python docs/코드분석/generate.py

만드는 것:
    자동생성/버튼별 코드 위치.md  — 화면 버튼 → 핸들러 → 파일:줄 → 곧바로 부르는 것
    자동생성/함수 목록.md         — 메서드 전수 (크기·불리는 곳·쓰는 SDK API)
    자동생성/파일 구조.md         — 파일 간 호출 관계 · 공유 상태 · 층 구조

원칙:
    - 자동생성/ 안의 파일은 손으로 고치지 않는다. 코드가 바뀌면 이 스크립트를 다시 돌린다.
    - 손으로 쓰는 문서는 파일별/ · 알고리즘/ · 판정/ 에 있다.

한계 (문서에도 같이 박아둔다):
    - 정규식 파싱이라 완전하지 않다. 람다·중첩 메서드·주석 처리된 코드에서 어긋난다.
    - 호출 횟수는 "같은 이름이 나타난 횟수"다. 오버로드·동명 지역변수를 구분하지 않는다.
    - 숫자를 그대로 발표에 쓰지 말고 정독으로 확인할 것.
"""
import re
import sys
import subprocess
import collections
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent                      # <repo>/
SRC = ROOT / "A2Z"
OUT = HERE / "자동생성"
OUT.mkdir(parents=True, exist_ok=True)

DESIGNER = "Form1.Designer.cs"

# ── 공통 정규식 ──────────────────────────────────────────────────────────────
RE_METHOD = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)?'
    r'(?:private|public|protected|internal)\s+'
    r'(?:static\s+|async\s+|override\s+|virtual\s+|sealed\s+|partial\s+|extern\s+|unsafe\s+|new\s+)*'
    r'(?:[\w<>\[\],\.\?]+(?:\s*,\s*[\w<>\[\],\.\?]+)*\s+)'
    r'([A-Za-z_]\w*)\s*\('
)
RE_FIELD = re.compile(
    r'^\s*(?:private|public|internal|protected)\s+'
    r'(?:static\s+|readonly\s+|const\s+|volatile\s+)*'
    r'(?:[\w<>\[\],\.\?\(\)]+\s+)'
    r'([_a-zA-Z]\w*)\s*(?:=[^=]|;)'
)
RE_TYPE = re.compile(
    r'^(\s*)(?:public|internal|private|protected)?\s*'
    r'(?:static\s+|sealed\s+|abstract\s+|readonly\s+)*'
    r'(?:partial\s+)?(class|struct|enum|interface)\s+(\w+)'
)
RE_API = re.compile(r'\bvizcore3d\.((?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*)\s*\(', re.I)

KEYWORDS = {'if', 'while', 'for', 'foreach', 'switch', 'catch', 'using',
            'lock', 'return', 'get', 'set', 'yield', 'fixed', 'nameof'}


def short(name):
    """Form1.Dimensions.cs -> Dimensions,  Form1.cs -> Form1"""
    if name == "Form1.cs":
        return "Form1"
    return name.replace("Form1.", "").replace(".cs", "")


def brace_end(lines, i, limit=6000):
    """i번 줄에서 시작하는 블록의 끝 줄 인덱스. 중괄호 균형으로 찾는다."""
    depth, started = 0, False
    for j in range(i, min(i + limit, len(lines))):
        for ch in lines[j]:
            if ch == '{':
                depth += 1
                started = True
            elif ch == '}':
                depth -= 1
                if started and depth == 0:
                    return j
        if not started and lines[j].rstrip().endswith(';'):
            return j          # 본문 없는 선언 (extern 등)
    return i


def nested_type_spans(lines):
    """중첩 타입(들여쓰기 8칸 이상) 구간. 그 안의 멤버는 '파일의 필드/메서드'가 아니다."""
    spans, i = [], 0
    while i < len(lines):
        m = RE_TYPE.match(lines[i])
        if m and len(m.group(1).expandtabs(4)) >= 8:
            end = brace_end(lines, i)
            spans.append((i, end, m.group(3), m.group(2)))
            i = end + 1
            continue
        i += 1
    return spans


def in_spans(idx, spans):
    return any(a <= idx <= b for a, b, _, _ in spans)


# ── 소스 읽기 ────────────────────────────────────────────────────────────────
def load():
    files = {}
    for p in sorted(SRC.rglob("*.cs")):
        if any(x in p.parts for x in ("obj", "bin", "Properties")):
            continue
        if p.name == "Program.cs":
            continue
        files[p.name] = p.read_text(encoding="utf-8", errors="replace").splitlines()
    return files


def code_stamp():
    """대상 코드의 마지막 커밋 날짜. 코드가 안 바뀌면 이 값도 안 바뀐다."""
    try:
        out = subprocess.run(
            ["git", "log", "-1", "--format=%ad %h", "--date=short", "--", "A2Z/"],
            cwd=str(ROOT), capture_output=True, text=True, timeout=15)
        return out.stdout.strip() or "(git 정보 없음)"
    except Exception:
        return "(git 정보 없음)"


HEADER = """<!-- 이 파일은 docs/코드분석/generate.py 가 만든다. 손으로 고치지 말 것. -->
> 🤖 **자동 생성 문서.** 손으로 고치지 마세요 — 다음 실행 때 덮어써집니다.
> 갱신: `python docs/코드분석/generate.py`
> 기준 코드: **{stamp}**
>
> ⚠ 정규식 파싱이라 완전하지 않습니다. 람다·중첩 메서드에서 어긋날 수 있고,
> 호출 횟수는 "같은 이름이 나타난 횟수"라 오버로드를 구분하지 않습니다.
> **숫자를 그대로 발표에 쓰지 말고 정독으로 확인하세요.**

"""


# ── 메서드 수집 ──────────────────────────────────────────────────────────────
def collect_methods(files):
    methods, owner = [], {}
    for fname, lines in files.items():
        if fname == DESIGNER:
            continue
        spans = nested_type_spans(lines)
        for i, ln in enumerate(lines):
            if in_spans(i, spans):
                continue
            m = RE_METHOD.match(ln)
            if not m or m.group(1) in KEYWORDS:
                continue
            end = brace_end(lines, i)
            body = "\n".join(lines[i:end + 1])
            methods.append(dict(file=fname, name=m.group(1), line=i + 1,
                                size=end - i + 1, body=body))
            owner.setdefault(m.group(1), fname)
    return methods, owner


def count_calls(files, name, home):
    pat = re.compile(r'(?<![\w.])' + re.escape(name) + r'\s*\(')
    hits = []
    for f, lines in files.items():
        c = len(pat.findall("\n".join(lines)))
        if f == home:
            c -= 1                      # 선언 자신 제외
        if c > 0:
            hits.append((f, c))
    return hits


# ── ① 버튼별 코드 위치 ───────────────────────────────────────────────────────
def gen_buttons(files, methods, owner, stamp):
    designer = "\n".join(files.get(DESIGNER, []))
    label = dict(re.findall(r'this\.(btn\w+)\.Text = "([^"]*)"', designer))
    handler = dict(re.findall(
        r'this\.(btn\w+)\.Click \+= new System\.EventHandler\(this\.(\w+)\)', designer))
    by_name = {m['name']: m for m in methods}
    ours = set(owner)

    rows = []
    for btn, h in handler.items():
        m = by_name.get(h)
        calls = []
        if m:
            for c in re.findall(r'(?<![\w.])(\w+)\s*\(', m['body']):
                if c in ours and c != h and c not in calls:
                    calls.append(c)
        rows.append(dict(label=label.get(btn, btn), btn=btn, h=h,
                         file=short(m['file']) if m else "?",
                         line=m['line'] if m else 0,
                         size=m['size'] if m else 0, calls=calls))
    rows.sort(key=lambda r: (r['file'], -r['size']))

    per = collections.Counter(r['file'] for r in rows)
    out = ["# 버튼별 코드 위치", "",
           HEADER.format(stamp=stamp),
           "화면에서 **버튼을 누르면 어느 파일 몇 줄로 가는지**를 적은 표입니다.",
           "코드를 읽기 전에 여기서 출발점을 고르세요.", "",
           f"버튼 **{len(rows)}개** · 담당 파일 **{len(per)}개**", "",
           "| 파일 | 버튼 수 |", "|---|---|"]
    for f, n in per.most_common():
        out.append(f"| {f} | {n}개 |")
    out += ["", "---", "",
            "| 화면 버튼 | 파일 | 줄 | 핸들러 크기 | 곧바로 부르는 것 |",
            "|---|---|---|---|---|"]
    cur = None
    for r in rows:
        if r['file'] != cur:
            cur = r['file']
        c = ", ".join(f"`{x}`" for x in r['calls'][:3]) + (" …" if len(r['calls']) > 3 else "")
        out.append(f"| **{r['label']}** | {r['file']} | {r['line']} | {r['size']}줄 | {c or '—'} |")

    out += ["", "---", "",
            "## 목록·이벤트 핸들러",
            "",
            "버튼 클릭 외의 핸들러입니다 — 목록 선택·더블클릭, 코드로 만든 컨트롤 등.",
            "배선 위치가 셋으로 갈립니다: **Designer**(자동 생성) · **Form1**(생성자에서 손으로) ·",
            "**해당 파일**(코드로 만든 컨트롤에 직접). 어디에도 없으면 죽은 코드입니다.", ""]
    ev = [m for m in methods
          if re.search(r'(_SelectedIndexChanged|_DoubleClick|_Click)$', m['name'])
          and m['name'] not in handler.values()]
    ev.sort(key=lambda m: -m['size'])
    # 연결 여부는 코드베이스 전체에서 `+= 핸들러` 배선을 찾는다.
    #   Designer.cs 의 `+= new System.EventHandler(this.X)` 뿐 아니라
    #   Stru.cs 의 `btnStruSearch.Click += BtnStruSearch_Click;` 처럼
    #   코드로 만든 컨트롤에 손으로 붙인 것도 있다. Form1.cs 만 보면 오탐이 난다.
    out += ["| 핸들러 | 파일 | 줄 | 크기 | 연결된 곳 |", "|---|---|---|---|---|"]
    for m in ev:
        wire = re.compile(
            r'\+=\s*(?:new\s+[\w\.]*EventHandler\s*\(\s*)?(?:this\.)?' + re.escape(m['name']) + r'(?![\w])')
        where = [f for f, lines in files.items() if wire.search("\n".join(lines))]
        wired = " · ".join(short(f) for f in where) if where else "🔴 **연결 안 됨**"
        out.append(f"| `{m['name']}` | {short(m['file'])} | {m['line']} | {m['size']}줄 | {wired} |")

    (OUT / "버튼별 코드 위치.md").write_text("\n".join(out) + "\n", encoding="utf-8")
    return len(rows), len(ev)


# ── ② 함수 목록 ──────────────────────────────────────────────────────────────
def gen_methods(files, methods, owner, stamp):
    for m in methods:
        m['callers'] = count_calls(files, m['name'], m['file'])
        m['apis'] = sorted(set(RE_API.findall(m['body'])))

    by_file = collections.defaultdict(list)
    for m in methods:
        by_file[m['file']].append(m)

    total = len(methods)
    orphan = [m for m in methods if not m['callers']]

    out = ["# 함수 목록", "",
           HEADER.format(stamp=stamp),
           "메서드 전수입니다. **크기가 큰 것 = 알고리즘이 있는 곳**이고,",
           "**불리는 곳이 없는 것 = 죽은 코드 후보**입니다 (이벤트 핸들러는 제외해야 함).", "",
           f"총 **{total}개** · 불리는 곳 없음 **{len(orphan)}개**", "", "---", ""]

    for fname in sorted(by_file, key=lambda f: -sum(m['size'] for m in by_file[f])):
        ms = sorted(by_file[fname], key=lambda m: -m['size'])
        tot = sum(m['size'] for m in ms)
        out += [f"## {fname}", "",
                f"메서드 **{len(ms)}개** · 합계 **{tot:,}줄** / 파일 {len(files[fname]):,}줄", "",
                "| 메서드 | 줄 | 크기 | 불리는 곳 | SDK API |", "|---|---|---|---|---|"]
        for m in ms:
            cal = ", ".join(f"{short(f)}×{c}" for f, c in m['callers']) or "**없음**"
            api = ", ".join(m['apis'][:3]) + (" …" if len(m['apis']) > 3 else "")
            out.append(f"| `{m['name']}` | {m['line']} | {m['size']} | {cal} | {api or '—'} |")
        out.append("")

    out += ["---", "", "## 불리는 곳이 없는 메서드", "",
            "이벤트 핸들러는 Designer나 생성자에서 연결되므로 정상입니다.",
            "**그 둘 다 아닌 것이 진짜 죽은 코드 후보**입니다.", "",
            "| 메서드 | 파일 | 줄 | 크기 |", "|---|---|---|---|"]
    for m in sorted(orphan, key=lambda m: -m['size']):
        out.append(f"| `{m['name']}` | {short(m['file'])} | {m['line']} | {m['size']}줄 |")

    (OUT / "함수 목록.md").write_text("\n".join(out) + "\n", encoding="utf-8")
    return total, len(orphan)


# ── ③ 파일 구조 ──────────────────────────────────────────────────────────────
def gen_structure(files, methods, owner, stamp):
    # 타입 선언
    types = collections.defaultdict(list)
    for fname, lines in files.items():
        for i, ln in enumerate(lines):
            m = RE_TYPE.match(ln)
            if m:
                types[m.group(3)].append((fname, i + 1, m.group(2)))
    form1_files = sorted({f for f, _, _ in types.get("Form1", [])})

    # 필드 — "여러 파일이 함께 쓰는 상태"만 센다.
    #   · partial class Form1 을 이루는 Form1*.cs 만 대상. Models.cs·MfgViewPose.cs 의
    #     멤버는 데이터 클래스의 속성이지 공유 상태가 아니다.
    #   · 중첩 타입(BodyBoundsData 등) 안쪽은 제외.
    #   · 메서드 선언은 제외 — `= new Xxx();` 로 끝나는 필드 초기화와 구분해야 하므로
    #     "괄호로 끝나면 메서드" 식 어림짐작 대신 메서드 정규식으로 판별한다.
    fields = {}
    for fname, lines in files.items():
        if fname == DESIGNER or not fname.startswith("Form1"):
            continue
        spans = nested_type_spans(lines)
        for i, ln in enumerate(lines):
            if in_spans(i, spans) or RE_METHOD.match(ln):
                continue
            m = RE_FIELD.match(ln)
            if m and m.group(1) not in KEYWORDS:
                fields.setdefault(m.group(1), (fname, i + 1))

    # 「값을 바꾸는 파일이 2곳 이상」인 필드만 센다.
    #   여러 파일이 '읽는' 것은 문제가 아니다 — SDK 핸들(vizcore3d)이 그렇다.
    #   추적을 어렵게 하는 건 여러 파일이 같은 필드의 '값을 바꾸는' 것이다.
    #   (2026-08-31 기준 변경. 이전 기준으로는 vizcore3d 가 1위로 올라왔다.)
    MUT = "Clear|Add|AddRange|Remove|RemoveAll|RemoveAt|Insert|Sort|Reverse"
    crossing = []
    for fld, (home, ln_) in fields.items():
        e = re.escape(fld)
        wpat = re.compile(
            r'(?<![\w.])' + e + r'\s*(?:\[[^\]]*\])?\s*(?:\+|-|\*|/|\||&|\^|\?\?)?=(?!=)'
            r'|(?<![\w.])' + e + r'\s*\.\s*(?:' + MUT + r')\s*\('
            r'|(?<![\w.])' + e + r'\s*(?:\+\+|--)')
        rpat = re.compile(r'(?<![\w.])' + e + r'(?![\w])')
        wfiles, wcount, rfiles = 0, 0, 0
        for f, lines in files.items():
            text = "\n".join(lines)
            w = len(wpat.findall(text))
            if w:
                wfiles += 1
                wcount += w
            if rpat.search(text):
                rfiles += 1
        if wfiles > 1:
            crossing.append((fld, home, wcount, wfiles, rfiles))
    crossing.sort(key=lambda x: (-x[3], -x[2]))

    # 호출 매트릭스
    edges = collections.Counter()
    for caller, lines in files.items():
        text = "\n".join(lines)
        for name, home in owner.items():
            if home == caller:
                continue
            c = len(re.findall(r'(?<![\w.])' + re.escape(name) + r'\s*\(', text))
            if c:
                edges[(caller, home)] += c
    inbound, outbound = collections.Counter(), collections.Counter()
    for (a, b), c in edges.items():
        outbound[a] += c
        inbound[b] += c

    out = ["# 파일 구조", "",
           HEADER.format(stamp=stamp),
           "**파일이 어떻게 나뉘어 있고, 서로 어떻게 부르는지**를 실측한 결과입니다.", "",
           "---", "",
           "## 1. 파일로는 나뉘어 있고, 클래스로는 하나다", "",
           f"`partial class Form1` 이 **{len(form1_files)}개 파일**에 쪼개져 있습니다.",
           "컴파일하면 **클래스 한 개**가 됩니다 — 즉 **파일 경계는 편집상의 구분이지 접근 제어 경계가 아닙니다.**",
           "어느 파일에서든 모든 필드·메서드에 제한 없이 접근됩니다.", "",
           "  " + " · ".join(short(f) for f in form1_files), "",
           f"Form1 밖 독립 타입: **{len(types) - 1}개**", "",
           "| 타입 | 종류 | 선언 위치 |", "|---|---|---|"]
    for k in sorted(t for t in types if t != "Form1"):
        f, l, kind = types[k][0]
        out.append(f"| `{k}` | {kind} | {f}:{l} |")

    out += ["", "---", "",
            "## 2. 공유 상태 — 여러 파일이 같은 필드의 값을 바꾼다", "",
            "> **여러 파일이 읽는 것 자체는 문제가 아니다.** SDK를 쓰는 프로그램이면",
            "> SDK 핸들은 모든 파일이 쓰는 게 당연하다 — `vizcore3d` 는 11개 파일이 읽지만",
            "> 값을 바꾸는 곳은 초기화 대입 1곳뿐이다. 추적을 어렵게 하는 건",
            "> **여러 파일이 같은 필드의 「값을 바꾸는」 것**이라 그 기준으로 센다.",
            "> (대입 `=` · 컬렉션 변형 `Clear`/`Add`/`Remove`/`Sort` 등 · 증감 `++`. 단순 읽기 제외)", "",
            f"`Form1` 필드 **{len(fields)}개** 중 **{len(crossing)}개**를 "
            f"**2개 이상의 파일이 값을 바꿉니다.**", "",
            "| 필드 | 선언 위치 | 값을 바꾸는 파일 | 바꾼 횟수 | 읽는 파일 |", "|---|---|---|---|---|"]
    for f, home, cnt, nfw, nfr in crossing[:30]:
        out.append(f"| `{f}` | {short(home)} | **{nfw}개** | {cnt} | {nfr}개 |")

    out += ["", "---", "",
            "## 3. 어느 파일이 어느 파일을 부르는가", "",
            "| 파일 | 남을 부름 | 남이 부름 | 성격 |", "|---|---|---|---|"]
    allf = sorted(set(list(inbound) + list(outbound)),
                  key=lambda x: -(inbound[x] + outbound[x]))
    for f in allf:
        i, o = inbound[f], outbound[f]
        if i > o * 2 and i > 15:
            role = "🔵 **남이 많이 씀** — 공용 부품"
        elif o > i * 2 and o > 15:
            role = "🟠 **남을 많이 씀** — 조립자"
        else:
            role = "⚪ 섞임"
        out.append(f"| {short(f)} | {o}회 | {i}회 | {role} |")

    out += ["", "### 가장 굵은 연결", "",
            "| 부르는 쪽 | → | 불리는 쪽 | 횟수 |", "|---|---|---|---|"]
    for (a, b), c in edges.most_common(15):
        out.append(f"| {short(a)} | → | {short(b)} | {c}회 |")

    out += ["", "---", "",
            "## 4. 층으로 보면", "",
            "강제된 경계는 없지만 역할은 갈려 있습니다.", "",
            "```",
            "조립자층   남을 많이 부르는 파일          ← 버튼 뒤",
            "중간층     부르기도 불리기도 하는 파일     ← 그리기·계산",
            "기반층     남이 많이 부르는 파일          ← 공용 상태·부품",
            "```", "",
            "**\"모듈화가 안 됐다\"가 아니라 \"층은 이미 있는데 컴파일러가 강제하지 않는다\"** 입니다.",
            "리팩토링은 없는 구조를 새로 짜는 게 아니라 **이미 있는 층을 진짜 클래스로 굳히는 일**입니다.", ""]

    (OUT / "파일 구조.md").write_text("\n".join(out) + "\n", encoding="utf-8")
    return len(form1_files), len(fields), len(crossing)


def main():
    files = load()
    stamp = code_stamp()
    methods, owner = collect_methods(files)

    nb, nev = gen_buttons(files, methods, owner, stamp)
    nm, north = gen_methods(files, methods, owner, stamp)
    nf, nfld, ncross = gen_structure(files, methods, owner, stamp)

    print(f"기준 코드: {stamp}")
    print(f"  버튼별 코드 위치.md — 버튼 {nb}개 + 목록 핸들러 {nev}개")
    print(f"  함수 목록.md       — 메서드 {nm}개 (불리는 곳 없음 {north}개)")
    print(f"  파일 구조.md       — partial 파일 {nf}개, 필드 {nfld}개 (여러 파일이 값 바꿈 {ncross}개)")
    print(f"→ {OUT}")


if __name__ == "__main__":
    main()
