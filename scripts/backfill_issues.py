# -*- coding: utf-8 -*-
"""개발 현황 Excel 항목을 GitHub 이슈로 옮긴다 (백필).

실행
  python scripts/backfill_issues.py --dry            # 만들 내용만 출력
  python scripts/backfill_issues.py --only 4,27      # 지정한 Excel No.만 생성
  python scripts/backfill_issues.py                  # 아직 이슈가 없는 항목 전부

이미 이슈가 연결된 Excel No.는 건너뛴다. 연결 정보는 docs/tracking/tasks/*.md의
`GitHub issue #N ... Excel No.M` 표기에서 읽는다.

완료·적용 제외 항목은 만든 즉시 닫는다. GitHub 등록·종료일은 옮긴 날짜가 되므로
실제 일자는 본문 이력 표에만 남는다.
"""
import io, os, re, sys, glob, json, subprocess, argparse
import openpyxl

ROOT  = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
XLSX  = os.path.join(ROOT, '2D 자동 제작도 개발 현황.xlsx')
MOVED = '2026-07-27'          # 이슈 관리로 옮긴 날

# Excel 현재 상태 → (제목 접두사, 상태 라벨, 닫을지)
STATUS = {
    '완료':          (None,             None,                  True),
    '적용 제외':      (None,             None,                  True),
    'API 대기':      ('API 대기',       '상태: API 대기',       False),
    'API 요청 필요':  ('API 요청 필요',  '상태: API 요청 필요',  False),
    '분석 필요':      ('분석 필요',      '상태: 분석 필요',      False),
    '개발 필요':      ('개발 대기',      '상태: 개발 대기',      False),
    '부분 구현':      ('개발 대기',      '상태: 개발 대기',      False),
    '개발 대기':      ('개발 대기',      '상태: 개발 대기',      False),
    '개발 중':        ('개발 중',        '상태: 개발 중',        False),
    '실기 검증 대기':  ('실기 확인',      '상태: 실기 확인',      False),
}

DRAW_LABEL = {'제작도': '도면: 제작도', '조립도': '도면: 조립도',
              '설치도': '도면: 설치도', '가공도': '도면: 가공도'}

COLS = ['no', 'kind', 'cat', 'name', 'must', 'design', 'code', 'prod',
        'status', 'value', 'more_dev', 'more_ana', 'trip', 'first', 'done', 'changed', 'note']


def load_rows():
    ws = openpyxl.load_workbook(XLSX, data_only=True).worksheets[1]
    out = []
    for r in ws.iter_rows(min_row=6, values_only=True):
        if not r[0] or not r[3]:
            continue
        d = {}
        for k, v in zip(COLS, r):
            if v is None:
                d[k] = ''
            elif hasattr(v, 'strftime'):
                d[k] = v.strftime('%Y-%m-%d')
            else:
                d[k] = str(v).strip()
        out.append(d)
    return out


def linked_nos():
    """이미 GitHub 이슈가 연결된 Excel No. 집합"""
    txt = ''.join(io.open(f, encoding='utf-8').read()
                  for f in glob.glob(os.path.join(ROOT, 'docs/tracking/tasks/*.md')))
    got = set()
    for m in re.finditer(r'Excel No\.([\d·,\s]+)', txt):
        if re.findall(r'issue #(\d+)', txt[max(0, m.start() - 400):m.start()]):
            got |= set(re.findall(r'\d+', m.group(1)))
    return got


def title(row):
    name = row['name']
    if len(name) < 10 and row['cat']:          # 'Q'TY' 처럼 짧은 이름은 대분류를 붙여 구분한다
        name = '%s %s' % (row['cat'], name)
    pre = STATUS.get(row['status'], (None,))[0]
    return '[%s] %s' % (pre, name) if pre else name


def labels(row):
    out = ['backfill']
    st = STATUS.get(row['status'], (None, None, False))[1]
    if st:
        out.append(st)
    kinds = [k for k in DRAW_LABEL if k in row['kind']]
    out.append(DRAW_LABEL[kinds[0]] if len(kinds) == 1 else '도면: 공통')
    if row['must'] == '○':
        out.append('생산 필수')
    return out


def body(row):
    meta = ['no: ' + row['no'], '도면: ' + row['kind'], '대분류: ' + row['cat'],
            '생산필수: ' + (row['must'] or '-')]
    for key, lab in (('first', '최초구현'), ('done', '완료확인'), ('changed', '최근변경')):
        if row[key]:
            meta.append('%s: %s' % (lab, row[key]))

    L = ['<!-- excel-meta', '\n'.join(meta), '-->', '',
         '**%s** · %s · 생산 필수 %s' % (row['kind'], row['cat'], row['must'] or '-'), '']
    if row['value']:
        L += ['## 현재 값 · 구현 내용', row['value'], '']
    if row['note']:
        L += ['## 근거 · 비고', row['note'], '']

    tbl = [('현재 상태', row['status']), ('API·코드 구현', row['code']),
           ('생산 출력 확인', row['prod']), ('추가 API·앱 개발', row['more_dev']),
           ('추가 분석', row['more_ana']), ('최초 구현일', row['first']),
           ('완료 확인일', row['done']), ('최근 변경일', row['changed'])]
    L += ['## 이력 · 판정', '| 항목 | 값 |', '|---|---|']
    L += ['| %s | %s |' % (k, v) for k, v in tbl if v]
    L += ['', '---',
          '*이슈 관리 도입(%s) 전에 개발 현황 Excel(No.%s)과 docs로 관리하던 항목을 옮긴 것입니다. '
          'GitHub 등록·종료일은 옮긴 날짜이며, 실제 일자는 위 이력 표를 따릅니다.*' % (MOVED, row['no'])]
    return '\n'.join(L)


def cleanup(num, clean_title):
    """닫힌 이슈에서 상태 라벨과 제목 접두사를 걷어낸다 (종료 이슈엔 활성 상태를 남기지 않는다)."""
    cur = subprocess.run(['gh', 'issue', 'view', num, '--json', 'title,labels'],
                         capture_output=True, text=True, encoding='utf-8').stdout
    if not cur:
        return
    j = json.loads(cur)
    cmd = ['gh', 'issue', 'edit', num]
    for l in j['labels']:
        if l['name'].startswith('상태: '):
            cmd += ['--remove-label', l['name']]
    if j['title'] != clean_title:
        cmd += ['--title', clean_title]
    if len(cmd) > 4:
        subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8')


def create(row, dry):
    t, b, ls = title(row), body(row), labels(row)
    close = STATUS.get(row['status'], (None, None, False))[2]
    if dry:
        print('=' * 72)
        print('제목  %s' % t)
        print('라벨  %s' % ', '.join(ls))
        print('처리  %s' % ('생성 후 즉시 닫음' if close else '열어 둠'))
        print('-' * 72)
        print(b)
        return None
    cmd = ['gh', 'issue', 'create', '--title', t, '--body', b]
    for l in ls:
        cmd += ['--label', l]
    url = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8').stdout.strip()
    num = url.rsplit('/', 1)[-1]
    if close:
        subprocess.run(['gh', 'issue', 'close', num,
                        '--comment', '이슈 관리 도입 전 완료된 항목이라 옮기면서 바로 닫습니다.'],
                       capture_output=True, text=True, encoding='utf-8')
        # 제목 동기화 Action이 `opened` 시점에 상태 라벨·접두사를 붙인다.
        # close 이벤트와 경합해 남는 경우가 있어 여기서 확정적으로 걷어낸다.
        cleanup(num, t)
    print('No.%-3s → #%-4s %s %s' % (row['no'], num, '(closed)' if close else '(open)  ', t))
    return num


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--dry', action='store_true')
    ap.add_argument('--only', default='')
    a = ap.parse_args()

    rows = load_rows()
    done = linked_nos()
    todo = [r for r in rows if r['no'] not in done]
    if a.only:
        want = {s.strip() for s in a.only.split(',')}
        todo = [r for r in rows if r['no'] in want]

    print('Excel %d건 · 이미 연결 %d건 · 이번 대상 %d건\n' % (len(rows), len(done), len(todo)))
    for r in todo:
        create(r, a.dry)


if __name__ == '__main__':
    main()
