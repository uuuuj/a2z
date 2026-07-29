# -*- coding: utf-8 -*-
"""GitHub 이슈를 원천으로 개발 현황 Excel과 JSON 백업을 생성한다.

실행
  python scripts/build_status_xlsx.py               # Excel + JSON 둘 다 생성
  python scripts/build_status_xlsx.py --json-only   # 백업만
  python scripts/build_status_xlsx.py --out 경로.xlsx

이슈가 정본이고 Excel은 **읽기 전용 생성물**이다. Excel을 손으로 고치지 말 것 —
다음 실행에서 덮어써진다. 상태를 바꾸려면 이슈의 `상태:` 라벨을 고친다.

JSON 백업은 이슈가 GitHub 서버에만 있는 문제를 덮는다. 저장소에 파일로 남겨
clone만으로도 이력을 읽을 수 있게 한다.
"""
import io, os, re, json, subprocess, argparse, datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
XLSX = os.path.join(ROOT, '개발 현황.xlsx')
DUMP = os.path.join(ROOT, 'docs/tracking-archive/issues.json')

STATUS_ORDER = ['상태: 개발 중', '상태: 실기 확인', '상태: API 대기', '상태: API 요청 필요',
                '상태: 분석 필요', '상태: 개발 대기']
DRAW_ORDER = ['도면: 제작도', '도면: 조립도', '도면: 설치도', '도면: 가공도', '도면: 공통']


def fetch():
    """이슈 전체 — 본문·코멘트·라벨·타임스탬프까지."""
    fields = 'number,title,state,stateReason,labels,body,comments,createdAt,closedAt,updatedAt,url'
    r = subprocess.run(['gh', 'issue', 'list', '--state', 'all', '--limit', '1000',
                        '--json', fields], capture_output=True, text=True, encoding='utf-8')
    if r.returncode != 0:
        raise SystemExit('gh 조회 실패: %s' % (r.stderr or '')[:300])
    return json.loads(r.stdout or '[]')


def labels(i):
    return [x['name'] for x in i['labels']]


def pick(i, order, prefix):
    ls = labels(i)
    for want in order:
        if want in ls:
            return want.split(': ', 1)[1]
    for l in ls:
        if l.startswith(prefix):
            return l.split(': ', 1)[1]
    return ''


def meta(i):
    """이관·백필 때 심어둔 메타 블록에서 원본 정보를 읽는다."""
    out = {}
    for pat in (r'<!-- excel-meta(.*?)-->', r'<!-- tracking-meta(.*?)-->'):
        m = re.search(pat, i.get('body') or '', re.S)
        if not m:
            continue
        for line in m.group(1).split('\n'):
            if ':' in line:
                k, v = line.split(':', 1)
                out.setdefault(k.strip(), v.strip())
    return out


def clean_title(t):
    return re.sub(r'^(?:\[[^\]]+\]\s*)+', '', t).strip()


def rows(iss):
    out = []
    for i in sorted(iss, key=lambda x: x['number']):
        m = meta(i)
        closed = i['state'] == 'CLOSED'
        out.append({
            '번호': i['number'],
            '제목': clean_title(i['title']),
            '상태': ('완료' if i.get('stateReason') != 'NOT_PLANNED' else '미채택') if closed
                    else (pick(i, STATUS_ORDER, '상태: ') or '미분류'),
            '도면': pick(i, DRAW_ORDER, '도면: '),
            '생산 필수': 'O' if '생산 필수' in labels(i) else '',
            '대분류': m.get('대분류', ''),
            '원본 ID': m.get('id', '') or (('Excel No.' + m['no']) if m.get('no') else ''),
            '최초 구현일': m.get('최초구현', ''),
            '완료 확인일': m.get('완료확인', '') or (i.get('closedAt') or '')[:10],
            '생성': (i.get('createdAt') or '')[:10],
            '최근 갱신': (i.get('updatedAt') or '')[:10],
            '코멘트': len(i.get('comments') or []),
            '라벨': ', '.join(l for l in labels(i)
                            if not l.startswith(('상태: ', '도면: '))),
            'URL': i.get('url', ''),
        })
    return out


def summary(rs):
    tot = len(rs)
    done = sum(1 for r in rs if r['상태'] == '완료')
    skip = sum(1 for r in rs if r['상태'] == '미채택')
    live = tot - skip
    must = [r for r in rs if r['생산 필수'] == 'O']
    must_done = sum(1 for r in must if r['상태'] == '완료')
    by = {}
    for r in rs:
        if r['상태'] not in ('완료', '미채택'):
            by[r['상태']] = by.get(r['상태'], 0) + 1
    lines = [('전체 이슈', tot), ('완료', done), ('미채택', skip),
             ('유효 관리 대상', live), ('진행 중', live - done),
             ('진행률', ('%.1f%%' % (done * 100.0 / live)) if live else '-'),
             ('', ''),
             ('생산 필수 전체', len(must)), ('생산 필수 완료', must_done),
             ('생산 필수 잔여', len(must) - must_done),
             ('', '')]
    for k in sorted(by, key=lambda x: -by[x]):
        lines.append(('진행 중 · ' + k, by[k]))
    return lines


def write_xlsx(rs, path):
    import openpyxl
    from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
    from openpyxl.utils import get_column_letter
    wb = openpyxl.Workbook()

    # ── 현황 요약 ──
    ws = wb.active
    ws.title = '현황 요약'
    ws['A1'] = '개발 현황 (GitHub 이슈 자동 생성)'
    ws['A1'].font = Font(size=14, bold=True)
    ws['A2'] = '생성 시각 %s · 원천은 GitHub 이슈이며 이 파일은 읽기 전용 생성물입니다. 손으로 고치지 마십시오.' \
               % datetime.datetime.now().strftime('%Y-%m-%d %H:%M')
    ws['A2'].font = Font(size=9, color='7F7F7F')
    r = 4
    for k, v in summary(rs):
        if k:
            ws.cell(r, 1, k).font = Font(bold=True)
            ws.cell(r, 2, v)
        r += 1
    ws.column_dimensions['A'].width = 22
    ws.column_dimensions['B'].width = 14

    # ── 이슈 목록 ──
    ws2 = wb.create_sheet('이슈 목록')
    cols = list(rs[0].keys()) if rs else []
    head = Font(bold=True, color='FFFFFF')
    fill = PatternFill('solid', fgColor='2563EB')
    thin = Side(style='thin', color='D9D9D9')
    for c, name in enumerate(cols, 1):
        cell = ws2.cell(1, c, name)
        cell.font = head
        cell.fill = fill
        cell.alignment = Alignment(horizontal='center', vertical='center')
    done_fill = PatternFill('solid', fgColor='EAFAF4')
    must_fill = PatternFill('solid', fgColor='FDECEC')
    for ri, row in enumerate(rs, 2):
        for c, name in enumerate(cols, 1):
            cell = ws2.cell(ri, c, row[name])
            cell.border = Border(bottom=thin)
            cell.alignment = Alignment(vertical='top', wrap_text=(name == '제목'))
        if row['상태'] == '완료':
            ws2.cell(ri, 3).fill = done_fill
        elif row['생산 필수'] == 'O':
            ws2.cell(ri, 5).fill = must_fill
    width = {'번호': 7, '제목': 58, '상태': 12, '도면': 10, '생산 필수': 9, '대분류': 12,
             '원본 ID': 14, '최초 구현일': 12, '완료 확인일': 12, '생성': 11,
             '최근 갱신': 11, '코멘트': 8, '라벨': 26, 'URL': 42}
    for c, name in enumerate(cols, 1):
        ws2.column_dimensions[get_column_letter(c)].width = width.get(name, 14)
    ws2.freeze_panes = 'A2'
    ws2.auto_filter.ref = 'A1:%s%d' % (get_column_letter(len(cols)), len(rs) + 1)

    wb.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', default=XLSX)
    ap.add_argument('--json-only', action='store_true')
    a = ap.parse_args()

    iss = fetch()
    rs = rows(iss)
    print('이슈 %d건 조회' % len(iss))

    os.makedirs(os.path.dirname(DUMP), exist_ok=True)
    io.open(DUMP, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(iss, ensure_ascii=False, indent=1, sort_keys=True))
    print('백업 → %s' % os.path.relpath(DUMP, ROOT).replace('\\', '/'))

    if a.json_only:
        return
    write_xlsx(rs, a.out)
    print('Excel → %s' % os.path.relpath(a.out, ROOT).replace('\\', '/'))
    for k, v in summary(rs)[:6]:
        if k:
            print('   %-14s %s' % (k, v))


if __name__ == '__main__':
    main()
