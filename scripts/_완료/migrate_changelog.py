# -*- coding: utf-8 -*-
"""docs/tracking/CHANGELOG.md 항목을 GitHub 이슈 코멘트로 이관한다.

실행
  python scripts/migrate_changelog.py --dry     # 대상만 출력
  python scripts/migrate_changelog.py           # 실제 코멘트 등록

매칭
  CHANGELOG 항목이 인용한 작업 ID(T-xxx·FB-xxx·REQ-xxx)와 `issue #N` 표기로
  대상 이슈를 찾는다. ID→이슈는 두 곳에서 읽는다.
    ① 이관된 이슈 본문의 <!-- tracking-meta --> 블록
    ② docs/tracking 문서의 `issue #N` 표기

작업 ID가 없거나(문서·잡무 커밋) 대상 이슈를 못 찾은 항목은 잔여 이슈 하나에
모아 코멘트로 남긴다 — CHANGELOG를 폐기해도 내용이 사라지지 않게 한다.
"""
import io, os, re, sys, glob, json, subprocess, argparse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CL = os.path.join(ROOT, 'docs/tracking/CHANGELOG.md')
MOVED = '2026-07-28'
LEFTOVER_TITLE = 'CHANGELOG 이관 잔여 — 작업번호 없는 커밋 기록'
IDPAT = r'(T-\d+|FB-\d+|REQ-\d+)'


def sh(*args):
    return subprocess.run(list(args), capture_output=True, text=True, encoding='utf-8')


def issues():
    return json.loads(sh('gh', 'issue', 'list', '--state', 'all', '--limit', '400',
                         '--json', 'number,title,body').stdout or '[]')


def id_to_issue(iss):
    live = {i['number'] for i in iss}
    out = {}
    def add(k, n):
        out.setdefault(k, set()).add(n)
    for i in iss:
        m = re.search(r'<!-- tracking-meta(.*?)-->', i.get('body') or '', re.S)
        if m:
            for t in re.findall(IDPAT, m.group(1)):
                add(t, i['number'])
    for f in glob.glob(os.path.join(ROOT, 'docs/tracking/tasks/*.md')) + \
             [os.path.join(ROOT, 'docs/tracking', x) for x in ('FEEDBACK.md', 'REQUESTS.md')]:
        if not os.path.exists(f):
            continue
        s = io.open(f, encoding='utf-8').read()
        hits = [(m.start(), m.group(1)) for m in re.finditer(r'^### ' + IDPAT, s, re.M)]
        for k, (a, tid) in enumerate(hits):
            b = hits[k + 1][0] if k + 1 < len(hits) else len(s)
            for n in re.findall(r'issue #(\d+)', s[a:b]):
                if int(n) in live:
                    add(tid, int(n))
    return out


def entries():
    s = io.open(CL, encoding='utf-8').read()
    idx = [m.start() for m in re.finditer(r'^## (20\d\d-\d\d-\d\d)', s, re.M)]
    out = []
    for k, a in enumerate(idx):
        b = idx[k + 1] if k + 1 < len(idx) else len(s)
        blk = s[a:b].rstrip()
        head = blk.split('\n')[0]
        # ⚠ 작업 ID는 `**관련 ...**:` 필드에서만 읽는다. 블록 전체에서 긁으면
        #   산문에 스친 T-xxx까지 잡혀 엉뚱한 이슈로 코멘트가 퍼진다(실측: 광범위 확산 14건 → 7건).
        rel = ' '.join(re.findall(r'^\*\*관련[^*]*\*\*:\s*(.+)$', blk, re.M))
        out.append(dict(date=re.match(r'^## (\S+)', head).group(1),
                        title=head[3:].strip(),
                        blk=blk,
                        ids=set(re.findall(IDPAT, rel)),
                        iss=set(int(x) for x in re.findall(r'issue #(\d+)', rel))))
    return out


def comment_body(e):
    # 최상위 헤딩을 한 단계 낮춰 코멘트 안에서 자연스럽게 보이게 한다.
    body = re.sub(r'^## ', '### ', e['blk'], count=1)
    return (body + '\n\n---\n*`docs/tracking/CHANGELOG.md`에서 이관 (%s). CHANGELOG는 폐기 예정입니다.*'
            % MOVED)


def ensure_leftover(dry):
    for i in issues():
        if i['title'].strip() == LEFTOVER_TITLE:
            return i['number']
    if dry:
        print('  (신설 예정) %s' % LEFTOVER_TITLE)
        return None
    body = ('작업 번호(T-xxx)가 붙지 않은 CHANGELOG 항목, 그리고 인용된 작업이 이슈로 남지 않은 항목을 '
            '내용 보존을 위해 이곳 코멘트로 모았습니다.\n\n'
            '대부분 문서·잡무 커밋이며 커밋 이력으로도 확인할 수 있습니다. '
            '개별 이슈로 관리할 성격이 아니라 이 이슈 하나에 모아 닫아 둡니다.\n\n'
            '*이관일 %s*' % MOVED)
    tmp = os.path.join(ROOT, '.tmp-leftover.md')
    io.open(tmp, 'w', encoding='utf-8', newline='\n').write(body)
    r = sh('gh', 'issue', 'create', '--title', LEFTOVER_TITLE, '--body-file', tmp,
           '--label', 'tracking-migrated')
    os.remove(tmp)
    url = (r.stdout or '').strip().split('\n')[-1]
    num = url.rsplit('/', 1)[-1]
    sh('gh', 'issue', 'close', num, '--reason', 'completed')
    print('  잔여 이슈 신설 → #%s' % num)
    return num


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dry', action='store_true')
    a = ap.parse_args()

    iss = issues()
    idmap = id_to_issue(iss)
    live = {i['number'] for i in iss}
    ent = entries()

    FANOUT = 5         # 이보다 많은 이슈로 퍼지는 항목은 개별 코멘트 대신 잔여로 보낸다
    plan = []          # (이슈번호, 항목)
    orphan = []
    wide = []
    for e in ent:
        tgt = {n for n in e['iss'] if n in live}
        for i in e['ids']:
            tgt |= idmap.get(i, set())
        if not tgt:
            orphan.append(e)
        elif len(tgt) > FANOUT:
            # 상태 일괄 동기화처럼 수십 개 작업을 한꺼번에 인용한 항목.
            # 이슈마다 붙이면 소음이라 잔여 이슈에 한 번만 남긴다.
            wide.append((e, len(tgt)))
            orphan.append(e)
        else:
            for n in sorted(tgt):
                plan.append((n, e))

    by = {}
    for n, e in plan:
        by.setdefault(n, []).append(e)
    print('CHANGELOG %d개 → 이슈 %d개에 코멘트 %d건 · 잔여 %d개(광범위 %d 포함)\n'
          % (len(ent), len(by), len(plan), len(orphan), len(wide)))
    if wide:
        print('[광범위 인용 — 잔여로 보냄]')
        for e, n in wide:
            print('  %s 이슈 %d개 인용  %s' % (e['date'], n, e['title'][:52]))
        print()

    if a.dry:
        for n in sorted(by, key=lambda x: -len(by[x]))[:12]:
            print('  #%-4s 코멘트 %2d건  (%s ...)' % (n, len(by[n]), by[n][0]['title'][:44]))
        print('  ...')
        print('\n[잔여 %d개 — 잔여 이슈로 모음]' % len(orphan))
        for e in orphan[:10]:
            print('  %s %s' % (e['date'], e['title'][:64]))
        ensure_leftover(True)
        return

    done = 0
    for n in sorted(by):
        for e in by[n]:
            tmp = os.path.join(ROOT, '.tmp-comment.md')
            io.open(tmp, 'w', encoding='utf-8', newline='\n').write(comment_body(e))
            r = sh('gh', 'issue', 'comment', str(n), '--body-file', tmp)
            os.remove(tmp)
            done += 1
            if r.returncode != 0:
                print('  !! #%s %s 실패: %s' % (n, e['date'], (r.stderr or '')[:120]))
        print('  #%-4s 코멘트 %d건 완료' % (n, len(by[n])))

    if orphan:
        num = ensure_leftover(False)
        for e in orphan:
            tmp = os.path.join(ROOT, '.tmp-comment.md')
            io.open(tmp, 'w', encoding='utf-8', newline='\n').write(comment_body(e))
            r = sh('gh', 'issue', 'comment', str(num), '--body-file', tmp)
            os.remove(tmp)
            done += 1
            if r.returncode != 0:
                print('  !! 잔여 %s 실패: %s' % (e['date'], (r.stderr or '')[:120]))
        print('  잔여 이슈 #%s 코멘트 %d건 완료' % (num, len(orphan)))

    print('\n코멘트 총 %d건 등록' % done)


if __name__ == '__main__':
    main()
