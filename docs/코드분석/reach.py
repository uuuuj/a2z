# -*- coding: utf-8 -*-
"""호출 그래프 도달성 — 진입점에서 출발해 못 닿는 메서드를 찾는다.

죽은 코드 검출 3종 중 세 번째. grep 이 못 잡는 것을 잡는다:
    · 정의는 있는데 아무 경로로도 도달할 수 없는 함수
    · 부르는 쪽이 이미 죽어서 연쇄로 죽은 함수

실행:
    python docs/코드분석/reach.py

    메서드 317개
    진입점 78개
    진입점에서 도달 가능: 317개 / 도달 불가(고아): 0개

⚠ **도구는 후보만 뽑는다.** 고아로 나와도 사람이 확인해야 한다.
   2026-08-24 최초 실행(보정 전)에서 고아가 여럿 나왔고 그중 **진짜 죽은 것은 2개**였다
   (`PlaceImageInTemplateArea` 83줄 · `GetOrientationLabel` 14줄 — 삭제 완료).
   나머지는 아래 오탐 4종이었다.

오탐 4종 — 2026-08-31 보정:
    1. 생성자에서 부르는 것      `public Form1()` 이 반환형이 없어 메서드로 안 잡혔다
    2. 이름만 넘기는 호출        `BeginInvoke(new Action(Name))` — 괄호가 안 붙어 호출로 안 보인다
    3. 프로퍼티 본문의 호출      `get { return Name(...); }`
    4. 정규식 오탐               `private static readonly (string, int)[] X =` 를 선언으로 오독
"""
import re, sys, collections
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')
SRC = Path(__file__).resolve().parents[2] / "A2Z"

RE_M = re.compile(r'^\s*(?:\[[^\]]*\]\s*)?(?:private|public|protected|internal)\s+'
                  r'(?:static\s+|async\s+|override\s+|virtual\s+|sealed\s+|partial\s+|extern\s+)*'
                  r'(?:[\w<>\[\],\.\?\(\)]+\s+)+([A-Za-z_]\w*)\s*\(')
# 생성자 — 반환형이 없다
RE_CTOR = re.compile(r'^\s*(?:public|private|protected|internal)\s+([A-Z]\w*)\s*\([^)]*\)\s*$')
KW = {'if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'return',
      'get', 'set', 'lock', 'readonly', 'const', 'new'}


def brace_end(ls, i):
    d, st = 0, False
    for j in range(i, min(i + 7000, len(ls))):
        for c in ls[j]:
            if c == '{':
                d += 1; st = True
            elif c == '}':
                d -= 1
                if st and d == 0:
                    return j
        if not st and ls[j].rstrip().endswith(';'):
            return j
    return i


# ── 1) 메서드 전수 + 본문 ──
methods, files, ctors = {}, {}, []
for p in sorted(SRC.glob("*.cs")):
    if p.name == "Form1.Designer.cs":
        continue
    ls = p.read_text(encoding="utf-8", errors="replace").splitlines()
    files[p.name] = ls
    for i, ln in enumerate(ls):
        m = RE_M.match(ln)
        if m and m.group(1) not in KW:
            e = brace_end(ls, i)
            body = re.sub(r'//.*', '', "\n".join(ls[i:e + 1]))
            methods.setdefault(m.group(1), (p.name, i + 1, e - i + 1, body))
            continue
        c = RE_CTOR.match(ln)                       # 보정 1 — 생성자
        if c and c.group(1) not in KW:
            e = brace_end(ls, i)
            ctors.append(re.sub(r'//.*', '', "\n".join(ls[i:e + 1])))
print(f"메서드 {len(methods)}개")

names = set(methods)


def calls_in(body):
    """본문에서 부르는 메서드 이름. 괄호가 붙은 호출 + 이름만 넘기는 참조."""
    found = {c for c in re.findall(r'\b([A-Za-z_]\w*)\s*\(', body) if c in names}
    # 보정 2 — new Action(Name) · new EventHandler(Name) 처럼 이름만 넘기는 것
    for c in re.findall(r'new\s+[\w\.<>]+\s*\(\s*([A-Za-z_]\w*)\s*\)', body):
        if c in names:
            found.add(c)
    return found


# ── 2) 호출 그래프 ──
calls = {n: calls_in(b) - {n} for n, (f, l, s, b) in methods.items()}

# ── 3) 진입점 = Designer 배선 + 코드 배선 + 생성자가 부르는 것 ──
des = (SRC / "Form1.Designer.cs").read_text(encoding="utf-8", errors="replace")
entries = set(re.findall(r'\+= new [\w\.]*EventHandler\(this\.(\w+)\)', des))
for f, ls in files.items():
    for ln in ls:
        m = re.search(r'\+=\s*(?:new [\w\.]+\()?(\w+)\s*\)?;', ln.strip())
        if m and m.group(1) in names:
            entries.add(m.group(1))
for cb in ctors:                                    # 보정 1 — 생성자 본문의 호출도 진입점
    entries |= calls_in(cb)
for f, ls in files.items():                         # 보정 3 — 프로퍼티 · 식 본문 멤버
    for i, ln in enumerate(ls):
        if re.search(r'\b(get|set)\s*[{=]|=>', ln):
            blk = chr(10).join(ls[i:brace_end(ls, i) + 1])
            entries |= calls_in(re.sub(r'//.*', '', blk))
entries &= names
print(f"진입점 {len(entries)}개")

# ── 4) 도달성 ──
seen = set()
stack = list(entries)
while stack:
    c = stack.pop()
    if c in seen:
        continue
    seen.add(c)
    stack.extend(calls.get(c, ()))

orphan = names - seen
print(f"\n진입점에서 도달 가능: {len(seen)}개 / 도달 불가(고아): {len(orphan)}개")
for o in sorted(orphan):
    f, l, s, _ = methods[o]
    print(f"   🔴 {o:38} {f}:{l} ({s}줄)")
if not orphan:
    print("   — 없음")
