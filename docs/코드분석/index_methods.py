# -*- coding: utf-8 -*-
"""메서드 색인 생성 — 전수를 한 장에 훑는 용도.

파싱은 **generate.py 를 그대로 가져다 쓴다** — 숫자가 갈리면 안 되므로.
상세 설명은 `교육/메서드 지도 — 파일별 역할과 연결.md`,
크기·호출처·SDK API 는 `자동생성/함수 목록.md`.

실행: python docs/코드분석/index_methods.py
"""
import io
import os
import re
import sys
import collections

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import generate as G   # noqa: E402  — 파싱 정본

OUT = os.path.join(HERE, '자동생성', '메서드 색인.md')

# 이름 → 역할. 위에서부터 먼저 맞는 것을 쓴다.
ROLES = [
    ('이벤트', r'.*_(Click|SelectedIndexChanged|ItemCheck|ItemChecked|TextChanged|KeyDown|KeyPress|KeyUp'
               r'|Load|Resize|Closing|FormClosed|Closed|Tick|CheckedChanged|MouseDown|MouseUp|MouseMove'
               r'|DoubleClick|Enter|Leave|Paint|Scroll|DrawItem|Event|Changed)$'),
    ('이벤트', r'^On[A-Z]'),
    ('이벤트', r'^Vizcore3d_|^Clash_|^Object3D_|^View_|^Lv[A-Z]|^Clb[A-Z]|^Tv[A-Z]'),
    ('저장',   r'^(Save|Export|Print|WriteTo|Emit|Publish)'),
    ('계산',   r'(Vector|Distance|Degrees|Radians|Tolerance|Overlap|Perpendicular|Bounds)'),
    ('계산',   r'^(Cross|Dot|Length|Clamp|Snap|Probe|Round)'),
    ('변환',   r'^(To[A-Z]|Parse|Format|Sanitize|Encode|Decode|Serialize)'),
    ('생성',   r'^(Add|Fill|Append|Emit)[A-Z]'),
    ('적용',   r'^(Activate|Release|Connect|Disconnect|Promote|Keep|Attach|Detach)'),
    ('흐름',   r'^(Advance|Finish|Throw|Abort)'),
    ('수집',   r'^(Cache|Snapshot|Store|Record)'),
    ('생성',   r'^(Build|Generate|Create|Render|Make|Compose|Draw|Insert|Place[A-Z])'),
    ('수집',   r'^(Collect|Gather|Populate|Accumulate|Harvest)'),
    ('계산',   r'^(Compute|Calculate|Estimate|Measure|Normalize|Convert|Transform|Project|Rotate'
               r'|Scale|Offset|Align|Fit|Rescale|Derive|Infer|Solve|Order|Sort|Group|Merge|Split|Dedup)'),
    ('판정',   r'^(Is|Has|Can|Should|Was|Are|Validate|Check|Detect|Try|Ensure|Verify|Match|Compare|Contains)'),
    ('정리',   r'^(Clear|Cleanup|Delete|Remove|Reset|Dispose|Flush|Discard|Prune|Cancel|Abort|Revert|Undo)'),
    ('조회',   r'^(Get|Find|Resolve|Lookup|Query|Pick|Search|Read|Fetch|Load|Extract|Filter|Choose|Select)'),
    ('적용',   r'^(Apply|Set|Assign|Update|Sync|Refresh|Restore|Toggle|Enable|Disable|Move|Adjust|Mark|Register)'),
    ('흐름',   r'^(Begin|End|Start|Stop|Complete|Process|Handle|Perform|Run|Execute|Continue|Next|Step|Wait|Poll|Retry)'),
    ('화면',   r'^(Show|Hide|Display|Init|Setup|Configure|Prepare|Layout|Focus|Scroll|Highlight|Fly|Zoom|Capture)'),
    ('로그',   r'^(Diag|Log|Trace|Dump|Report)'),
]

DESC = {
    '이벤트': '사용자 조작·SDK 콜백을 받는 진입점',
    '조회':   '값을 찾아 돌려준다',
    '생성':   '도면·장면·데이터를 만든다',
    '계산':   '좌표·크기·각도를 계산한다',
    '판정':   '참/거짓을 답한다',
    '적용':   '상태를 바꾼다',
    '흐름':   '단계를 진행·대기·중단시킨다',
    '화면':   '표시·초기화',
    '정리':   '지우고 되돌린다',
    '수집':   '여러 곳에서 모아 담는다',
    '저장':   '파일로 내보낸다',
    '변환':   '형식을 바꾼다 (문자열·표시값)',
    '로그':   '진단 기록',
    '기타':   '위 패턴에 안 맞는 것',
}


def role_of(name):
    for tag, pat in ROLES:
        if re.match(pat, name):
            return tag
    return '기타'


def main():
    files = G.load()
    methods, owner = G.collect_methods(files)
    stamp = G.code_stamp()

    allsrc = '\n'.join('\n'.join(v) for v in files.values())
    wired = set()
    wired |= set(re.findall(r'\+=\s*new\s+(?:System\.)?EventHandler\(\s*(?:this\.)?([A-Za-z_]\w*)', allsrc))
    wired |= set(re.findall(r'\+=\s*new\s+[\w.<>]*EventHandler[\w<>]*\(\s*(?:this\.)?([A-Za-z_]\w*)', allsrc))
    wired |= set(re.findall(r'\+=\s*(?:this\.)?([A-Za-z_]\w*)\s*;', allsrc))
    wired |= set(re.findall(r'new\s+Action\(\s*(?:this\.)?([A-Za-z_]\w*)\s*\)', allsrc))

    dup = {k for k, c in collections.Counter(m['name'] for m in methods).items() if c > 1}

    byfile = collections.OrderedDict()
    for m in methods:
        byfile.setdefault(m['file'], []).append(m)

    rolecnt = collections.Counter(role_of(m['name']) for m in methods)

    L = []
    L.append('# 메서드 색인')
    L.append('')
    L.append('<!-- docs/코드분석/index_methods.py 가 만든다. 손으로 고치지 말 것. -->')
    L.append('> 🤖 **자동 생성.** 갱신: `python docs/코드분석/index_methods.py`')
    L.append('> 파싱은 `generate.py` 를 그대로 쓴다 — [함수 목록](./함수%20목록.md) 과 **숫자가 항상 같다**.')
    L.append('> 기준 코드: **%s**' % stamp)
    L.append('>')
    L.append('> **이 문서는 색인이다** — 이름으로 빠르게 찾는 용도.')
    L.append('> 설명은 [메서드 지도](../교육/메서드%20지도%20—%20파일별%20역할과%20연결.md),')
    L.append('> 크기·호출처·SDK API 는 [함수 목록](./함수%20목록.md).')
    L.append('>')
    L.append('> ⚠ 정규식 파싱이라 완전하지 않다. 발표 전 정독으로 확인할 것.')
    L.append('')
    L.append('총 **%d개** · 파일 **%d개**' % (len(methods), len(byfile)))
    L.append('')
    L.append('## 역할별 분포')
    L.append('')
    L.append('| 역할 | 개수 | 무엇 |')
    L.append('|---|---:|---|')
    for k, c in rolecnt.most_common():
        L.append('| **%s** | %d | %s |' % (k, c, DESC.get(k, '')))
    L.append('')
    L.append('## 파일별 분포')
    L.append('')
    L.append('| 파일 | 메서드 | 본문 줄 | 100줄 이상 |')
    L.append('|---|---:|---:|---:|')
    for fn, v in sorted(byfile.items(), key=lambda x: -len(x[1])):
        big = sum(1 for m in v if m['size'] >= 100)
        L.append('| `%s` | %d | %s | %s |'
                 % (fn, len(v), format(sum(m['size'] for m in v), ','), big if big else '—'))
    L.append('')
    L.append('---')
    L.append('')

    for fn, v in sorted(byfile.items(), key=lambda x: -len(x[1])):
        L.append('## %s — %d개' % (fn, len(v)))
        L.append('')
        L.append('| 메서드 | 줄 | 크기 | 역할 | 배선 | 호출처 |')
        L.append('|---|---:|---:|---|:-:|---:|')
        for m in sorted(v, key=lambda x: -x['size']):
            n = m['name']
            hits = G.count_calls(files, n, owner.get(n, fn))
            c = sum(x[1] for x in hits)
            nm = ('**`%s`**' % n) if m['size'] >= 100 else ('`%s`' % n)
            if n in dup:
                nm += ' ⚠'
            L.append('| %s | %d | %d | %s | %s | %s |'
                     % (nm, m['line'], m['size'], role_of(n),
                        '⚡' if n in wired else '', c if c else '**0**'))
        L.append('')

    L.append('---')
    L.append('')
    L.append('## 읽는 법')
    L.append('')
    L.append('| 표시 | 뜻 |')
    L.append('|---|---|')
    L.append('| **굵은 이름** | 본문 100줄 이상 — **알고리즘이 들어 있는 곳** |')
    L.append('| ⚡ | 이벤트로 배선돼 있다 (`+=`). 코드가 직접 안 불러도 실행된다 |')
    L.append('| 호출처 **0** + ⚡ 없음 | **죽은 코드 후보.** 지우기 전 반드시 확인 → [검출 도구](../검출%20도구.md) |')
    L.append('| ⚠ | 같은 이름이 여러 파일에 있다. 호출처 계수가 부정확할 수 있다 |')
    L.append('')
    L.append('> 호출처 수는 **「같은 이름이 나타난 횟수」**다. 오버로드·동명 지역변수를 구분하지 않는다.')

    io.open(OUT, 'w', encoding='utf-8').write('\n'.join(L))
    sys.stdout.write('methods=%d files=%d\n' % (len(methods), len(byfile)))
    sys.stdout.write('roles=%s\n' % dict(rolecnt))


if __name__ == '__main__':
    main()
