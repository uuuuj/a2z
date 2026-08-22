# -*- coding: utf-8 -*-
"""docs/tracking 항목을 GitHub 이슈로 이관한다.

실행
  python scripts/migrate_tracking.py --dry          # 만들 내용만 출력
  python scripts/migrate_tracking.py --dry --full   # 본문까지 전부 출력
  python scripts/migrate_tracking.py                # 실제 생성

대상
  docs/tracking/tasks/*.md   T-xxx 작업 — 본문에 `issue #N` 표기가 없는 것만
  docs/tracking/FEEDBACK.md  FB-xxx 담당자 피드백
  docs/tracking/REQUESTS.md  REQ-xxx 내부 요청

완료·폐기·거절 항목은 만든 즉시 닫는다. 원문 블록을 본문에 그대로 담아
docs/tracking 없이도 내용이 남게 한다.
"""
import io, os, re, sys, glob, json, subprocess, argparse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MARK = 'tracking-migrated'          # 이관분 식별 라벨
MOVED = '2026-07-28'

# 상태 문자열 → (제목 접두사, 상태 라벨, 닫을지)
def status_of(raw, srcfile):
    s = (raw or '').strip()
    head = s.split('(')[0].strip().rstrip('—').strip()
    if head.startswith('DONE') or head.startswith('폐기') or head.startswith('REJECTED'):
        return (None, None, True)
    if head.startswith('API 대기'):
        return ('API 대기', '상태: API 대기', False)
    if head.startswith('API 요청 필요'):
        return ('API 요청 필요', '상태: API 요청 필요', False)
    if head.startswith('IN_PROGRESS'):
        return ('개발 중', '상태: 개발 중', False)
    if head.startswith('BLOCKED'):
        return ('분석 필요', '상태: 분석 필요', False)
    # FEEDBACK·REQUESTS는 "들어온 입력"의 기록이다. 수락(ACCEPTED)된 것은 대응 작업이
    # 별도 이슈로 존재하므로 입력 기록 자체는 닫는다. 아직 판단 전인 것만 열어둔다.
    if head.startswith('ACCEPTED'):
        return (None, None, True)
    if head.startswith('IN_REVIEW') or head.startswith('OPEN'):
        return ('분석 필요', '상태: 분석 필요', False)
    # TODO 및 그 외
    if 'DONE.md' in srcfile:
        return (None, None, True)
    return ('개발 대기', '상태: 개발 대기', False)


DRAW = {'제작도': '도면: 제작도', '조립도': '도면: 조립도',
        '설치도': '도면: 설치도', '가공도': '도면: 가공도'}


def parse(path, pat):
    """### <ID> — <제목> 블록을 잘라 반환."""
    s = io.open(path, encoding='utf-8').read()
    hits = [(m.start(), m.group(1), m.group(2)) for m in re.finditer(pat, s, re.M)]
    out = []
    for k, (a, tid, title) in enumerate(hits):
        b = hits[k + 1][0] if k + 1 < len(hits) else len(s)
        blk = s[a:b].rstrip()
        body = '\n'.join(blk.split('\n')[1:]).strip()   # 제목 줄 제거
        st = re.search(r'^\-\s*\*\*상태\*\*:\s*(.+)$', body, re.M)
        out.append(dict(id=tid, title=title.strip(), body=body,
                        raw_status=(st.group(1).strip() if st else ''),
                        src=os.path.relpath(path, ROOT).replace('\\', '/'),
                        has_issue=bool(re.search(r'issue #\d+', blk))))
    return out


def collect():
    items = []
    for f in sorted(glob.glob(os.path.join(ROOT, 'docs/tracking/tasks/*.md'))):
        items += parse(f, r'^### (T-\d+)\s*[—-]\s*(.+)$')
    for name, pat in (('FEEDBACK.md', r'^### (FB-\d+)\s*[—-]\s*(.+)$'),
                      ('REQUESTS.md', r'^### (REQ-\d+)\s*[—-]\s*(.+)$')):
        p = os.path.join(ROOT, 'docs/tracking', name)
        if os.path.exists(p):
            items += parse(p, pat)

    # ⚠ 원본에 ID 중복이 있다 (FB-001·REQ-001 각 2건 — "번호 재사용 금지" 규칙 위반).
    #   서로 다른 항목이므로 둘 다 이관하되, 메타의 id에 순번을 붙여 구분한다.
    seen = {}
    for it in items:
        seen[it['id']] = seen.get(it['id'], 0) + 1
        if seen[it['id']] > 1:
            it['dup'] = seen[it['id']]
            it['id_label'] = '%s (중복 %d번째)' % (it['id'], seen[it['id']])
        else:
            it['id_label'] = it['id']
    dups = {k for k, v in seen.items() if v > 1}
    for it in items:
        if it['id'] in dups and 'dup' not in it:
            it['id_label'] = '%s (중복 1번째)' % it['id']
    return items


def existing_titles():
    raw = subprocess.run(['gh', 'issue', 'list', '--state', 'all', '--limit', '400',
                          '--json', 'number,title'], capture_output=True, text=True,
                         encoding='utf-8').stdout
    out = {}
    for i in json.loads(raw or '[]'):
        t = re.sub(r'^\[[^\]]+\]\s*', '', i['title'])          # 상태 접두사 제거
        out[re.sub(r'[\s·—\-/()]+', '', t).lower()] = i['number']
    return out


def norm(t):
    return re.sub(r'[\s·—\-/()]+', '', t).lower()


def build(it):
    pre, label, close = status_of(it['raw_status'], it['src'])
    title = ('[%s] %s' % (pre, it['title'])) if pre else it['title']
    labels = [MARK]
    if label:
        labels.append(label)
    for k, v in DRAW.items():
        if k in it['title']:
            labels.append(v)
    body = ['<!-- tracking-meta',
            'id: %s' % it.get('id_label', it['id']),
            'source: %s' % it['src'],
            'moved: %s' % MOVED,
            '-->', '',
            it['body'], '',
            '---', '',
            '*`%s`의 `%s` 항목을 이관했습니다 (%s). 이 이슈가 정본이며 tracking 문서는 폐기 예정입니다.*'
            % (it['src'], it.get('id_label', it['id']), MOVED)]
    return title, '\n'.join(body), labels, close


def create(it, dry, full):
    title, body, labels, close = build(it)
    if dry:
        print('  %-7s %-6s %s' % (it['id'], '(closed)' if close else '(open)', title[:78]))
        print('          labels: %s' % ', '.join(labels))
        if full:
            print('  ' + '-' * 70)
            print(re.sub(r'^', '  | ', body, flags=re.M)[:1400])
            print('  ' + '-' * 70)
        return None
    tmp = os.path.join(ROOT, '.tmp-migrate-body.md')
    io.open(tmp, 'w', encoding='utf-8', newline='\n').write(body)
    cmd = ['gh', 'issue', 'create', '--title', title, '--body-file', tmp]
    for l in labels:
        cmd += ['--label', l]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8')
    os.remove(tmp)
    url = (r.stdout or '').strip().split('\n')[-1]
    num = url.rsplit('/', 1)[-1] if '/' in url else ''
    if not num:
        print('  !! %s 생성 실패: %s' % (it['id'], (r.stderr or '')[:160]))
        return None
    if close:
        subprocess.run(['gh', 'issue', 'close', num, '--reason', 'completed'],
                       capture_output=True, text=True, encoding='utf-8')
    print('  %-7s → #%-4s %s %s' % (it['id'], num, '(closed)' if close else '(open)  ', title[:60]))
    return num


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dry', action='store_true')
    ap.add_argument('--full', action='store_true', help='본문까지 출력')
    ap.add_argument('--only', default='', help='T-040,FB-001 처럼 ID 지정')
    a = ap.parse_args()

    items = collect()
    linked = [i for i in items if i['has_issue']]
    todo = [i for i in items if not i['has_issue']]
    if a.only:
        want = {s.strip() for s in a.only.split(',')}
        todo = [i for i in items if i['id'] in want]

    # 제목이 이미 같은 이슈가 있으면 중복이므로 제외
    ex = existing_titles()
    dup = [i for i in todo if norm(i['title']) in ex]
    todo = [i for i in todo if norm(i['title']) not in ex]

    print('tracking 항목 %d개 · 이슈 표기 있음 %d · 제목 중복 %d · 이번 대상 %d\n'
          % (len(items), len(linked), len(dup), len(todo)))
    if dup:
        print('[제목 중복 — 건너뜀]')
        for i in dup:
            print('  %-7s #%-4s %s' % (i['id'], ex[norm(i['title'])], i['title'][:66]))
        print()

    opened = [i for i in todo if not status_of(i['raw_status'], i['src'])[2]]
    closed = [i for i in todo if status_of(i['raw_status'], i['src'])[2]]
    print('[생성 대상] 열린 상태 %d건 · 닫을 것 %d건\n' % (len(opened), len(closed)))
    for i in todo:
        create(i, a.dry, a.full)


if __name__ == '__main__':
    main()
