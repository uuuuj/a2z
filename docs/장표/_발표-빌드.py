# -*- coding: utf-8 -*-
"""docs/장표/*.html 를 발표용 단일 HTML(발표.html)로 통합한다.

실행: python _발표-빌드.py
입력: _deck.css · _발표-신규슬라이드.html · 02/03/11~17 장표 · img/*.png
출력: 발표.html — CSS 인라인 + 이미지 base64 임베드, 파일 하나로 완결
"""
import io, os, re, base64

DECK = os.path.dirname(os.path.abspath(__file__))
NEW  = os.path.join(DECK, '_발표-신규슬라이드.html')
OUT  = os.path.join(DECK, '발표.html')

def read(p): return io.open(p, encoding='utf-8').read()

# ---------- CSS 규칙 접두사 붙이기 (파일 간 셀렉터 충돌 차단) ----------
def prefix_css(css, pref):
    css = re.sub(r'/\*.*?\*/', '', css, flags=re.S)
    out, i, n = [], 0, len(css)
    while i < n:
        j = css.find('{', i)
        if j < 0: break
        k, depth = j, 0
        while k < n:
            if css[k] == '{': depth += 1
            elif css[k] == '}':
                depth -= 1
                if depth == 0: break
            k += 1
        sels = css[i:j].strip()
        body = css[j:k+1]
        if sels:
            new = ', '.join('%s %s' % (pref, s.strip()) for s in sels.split(',') if s.strip())
            out.append(new + body)
        i = k + 1
    return '\n'.join(out)

def split_pages(html):
    """<div class="deck"> 안의 최상위 <section class="page"> 블록들을 뽑는다."""
    ds = html.index('<div class="deck">') + len('<div class="deck">')
    de = html.rindex('</div>')
    inner = html[ds:de]
    starts = [m.start() for m in re.finditer(r'<section class="page[\s"]', inner)]
    pages = []
    for a, b in zip(starts, starts[1:] + [len(inner)]):
        chunk = inner[a:b].rstrip()
        if not chunk.endswith('</section>'):
            chunk = chunk[:chunk.rindex('</section>') + len('</section>')]
        pages.append(chunk)
    return pages

def clean_page(p, extra_cls):
    p = re.sub(r'\sid="s\d+"', '', p)                                  # 중복 id 제거
    p = re.sub(r'<a class="nav"[^>]*>.*?</a>\s*', '', p, flags=re.S)   # 개별 파일용 목차 링크 제거
    p = p.replace('<section class="page"', '<section class="page %s"' % extra_cls, 1)
    return p

# ---------- 이미지 base64 ----------
def img_data_uri(name):
    with open(os.path.join(DECK, 'img', name), 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')

# ---------- 신규 슬라이드 ----------
new_raw = read(NEW)
new_css = re.search(r'<style>(.*?)</style>', new_raw, re.S).group(1)
new_body = new_raw[new_raw.index('</style>') + len('</style>'):]
new_pages = split_pages('<div class="deck">' + new_body + '</div>')

# ---------- 기존 장표 이식 ----------
SOURCES = [
    ('02-작업-순서.html',        '사용 방법'),
    ('03-도면-4종.html',         '사용 방법'),
    ('11-룰북-Osnap.html',        '룰북'),
    ('12-룰북-보조선.html',       '룰북'),
    ('13-룰북-치수선별.html',     '룰북'),
    ('14-룰북-각도.html',         '룰북'),
    ('15-룰북-위치.html',         '룰북'),
    ('16-룰북-BOM-부재목록.html', '룰북'),
    ('17-룰북-BOM-도면표.html',   '룰북'),
]
imported, src_css = {}, []
for idx, (fn, sec) in enumerate(SOURCES):
    raw = read(os.path.join(DECK, fn))
    cls = 'src%02d' % idx
    st = re.search(r'<style>(.*?)</style>', raw, re.S)
    if st: src_css.append('/* %s */\n%s' % (fn, prefix_css(st.group(1), '.' + cls)))
    pages = [clean_page(p, cls).replace('<section class="page %s"' % cls,
             '<section class="page %s" data-sec="%s"' % (cls, sec), 1) for p in split_pages(raw)]
    imported[fn] = pages
    print('%-28s %d슬라이드' % (fn, len(pages)))

# ---------- 순서 조립 ----------
order = [
    new_pages[0],                       # 1  표지
    new_pages[1],                       # 2  목차
    new_pages[2],                       # 3  프로그램 UI 구성요소
    new_pages[3],                       # 4  실행 순서 (마커)
    *imported['02-작업-순서.html'],      # 5  작업 순서
    *imported['03-도면-4종.html'],       # 6  도면 4종
    new_pages[4],                       # 7  예시 모델 · 부재 분할
    new_pages[5],                       # 8  제작도
    new_pages[6],                       # 9  조립도
    new_pages[7],                       # 10 설치도
    new_pages[8],                       # 11 가공도
    new_pages[9],                       # 12 값의 출처
    *imported['11-룰북-Osnap.html'],
    *imported['12-룰북-보조선.html'],
    *imported['13-룰북-치수선별.html'],
    *imported['14-룰북-각도.html'],
    *imported['15-룰북-위치.html'],
    *imported['16-룰북-BOM-부재목록.html'],
    *imported['17-룰북-BOM-도면표.html'],
]
body = '\n\n'.join(order)

# 이미지 치환
for name in set(re.findall(r'\{\{IMG:(.*?)\}\}', body)):
    body = body.replace('{{IMG:%s}}' % name, img_data_uri(name))
    print('embed', name)

NAV_CSS = """
/* ===== 발표 통합본 · 조작 ===== */
.deck{ scroll-behavior:smooth; }
#bar{ position:fixed; left:0; bottom:0; height:4px; background:var(--accent); width:0; z-index:50; transition:width .18s ease; }
#hud{ position:fixed; right:14px; bottom:12px; z-index:50; display:flex; align-items:center; gap:10px;
      background:rgba(255,255,255,.92); border:1px solid var(--line); border-radius:20px; padding:5px 12px;
      font-size:12px; font-weight:800; color:var(--muted); box-shadow:0 2px 10px rgba(20,35,60,.12); }
#hud .sec{ color:var(--accent-ink); }
#hud b{ color:var(--ink); font-variant-numeric:tabular-nums; }
#hud .k{ font-weight:600; color:var(--faint); }
.toc .tc{ cursor:pointer; transition:box-shadow .15s, transform .15s; }
.toc .tc:hover{ box-shadow:0 3px 12px rgba(37,99,235,.18); transform:translateX(2px); }
@media print{
  #bar,#hud{ display:none; }
  .deck{ height:auto; overflow:visible; }
  .page{ min-height:auto; height:100vh; padding:0; page-break-after:always; }
  .slide{ width:100%; height:100%; border-radius:0; box-shadow:none; aspect-ratio:auto; }
}
"""

NAV_JS = """
(function(){
  var pages=[].slice.call(document.querySelectorAll('.page')), cur=0;
  var bar=document.getElementById('bar'), hud=document.getElementById('hud');
  function paint(){
    bar.style.width=((cur+1)/pages.length*100)+'%';
    hud.querySelector('.sec').textContent=pages[cur].dataset.sec||'';
    hud.querySelector('.now').textContent=cur+1;
  }
  function go(i){ cur=Math.max(0,Math.min(pages.length-1,i)); pages[cur].scrollIntoView({behavior:'smooth'}); paint(); }
  document.addEventListener('keydown',function(e){
    if(e.key==='ArrowRight'||e.key==='PageDown'||e.key===' '){ e.preventDefault(); go(cur+1); }
    else if(e.key==='ArrowLeft'||e.key==='PageUp'||e.key==='Backspace'){ e.preventDefault(); go(cur-1); }
    else if(e.key==='Home'){ e.preventDefault(); go(0); }
    else if(e.key==='End'){ e.preventDefault(); go(pages.length-1); }
  });
  document.addEventListener('click',function(e){
    if(e.target.closest('a,.tc')) return;
    go(e.clientX < window.innerWidth*0.22 ? cur-1 : cur+1);
  });
  var jump=[2,3,6,11,12];
  [].slice.call(document.querySelectorAll('.toc .tc')).forEach(function(el,i){
    el.addEventListener('click',function(e){ e.stopPropagation(); go(jump[i]); });
  });
  var io=new IntersectionObserver(function(es){
    es.forEach(function(en){ if(en.isIntersecting){ cur=pages.indexOf(en.target); paint(); } });
  },{threshold:.55});
  pages.forEach(function(p){ io.observe(p); });
  paint();
})();
"""

html = """<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>2D 자동 제작도 — 설명 장표 (발표 통합본)</title>
<style>
%s
%s
%s
%s
</style>
</head>
<body>
<div class="deck">
%s
</div>
<div id="bar"></div>
<div id="hud"><span class="sec"></span><b><span class="now">1</span> / %d</b><span class="k">← →</span></div>
<script>%s</script>
</body>
</html>
""" % (read(os.path.join(DECK, '_deck.css')), NAV_CSS, new_css, '\n'.join(src_css),
       body, len(order), NAV_JS)

io.open(OUT, 'w', encoding='utf-8', newline='\n').write(html)
print('\n=> %s  (%d슬라이드, %.1f MB)' % (OUT, len(order), os.path.getsize(OUT)/1048576))
