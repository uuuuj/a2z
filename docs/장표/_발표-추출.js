/* 발표.html의 렌더 결과를 슬라이드별 요소 트리로 추출해 로컬 수집 서버로 보낸다.
   슬라이드는 1333.33 x 750 px로 고정되므로 1px = 0.01in 이다. */
(function () {
  function px(v) { return parseFloat(v) || 0; }
  function vis(cs) { return cs.display !== 'none' && cs.visibility !== 'hidden' && px(cs.opacity) !== 0; }
  function solid(c) { return c && c !== 'rgba(0, 0, 0, 0)' && c !== 'transparent'; }

  function rectOf(el, base) {
    var r = el.getBoundingClientRect();
    return { x: r.left - base.left, y: r.top - base.top, w: r.width, h: r.height };
  }

  function borders(cs) {
    var sides = ['Top', 'Right', 'Bottom', 'Left'].map(function (s) {
      return { w: px(cs['border' + s + 'Width']), c: cs['border' + s + 'Color'], st: cs['border' + s + 'Style'] };
    });
    return sides;
  }

  function runsOf(el) {
    var out = [];
    function rec(node, inh) {
      for (var i = 0; i < node.childNodes.length; i++) {
        var n = node.childNodes[i];
        if (n.nodeType === 3) {
          var t = n.textContent.replace(/\s+/g, ' ');
          if (t) out.push({ t: t, fs: inh.fs, fw: inh.fw, color: inh.color, mono: inh.mono, bg: inh.bg });
        } else if (n.nodeType === 1) {
          if (n.tagName === 'BR') { out.push({ br: true }); continue; }
          var cs = getComputedStyle(n);
          if (!vis(cs)) continue;
          rec(n, {
            fs: px(cs.fontSize), fw: cs.fontWeight, color: cs.color,
            mono: /consolas|courier|monospace/i.test(cs.fontFamily),
            bg: solid(cs.backgroundColor) ? cs.backgroundColor : null
          });
        }
      }
    }
    var cs0 = getComputedStyle(el);
    rec(el, {
      fs: px(cs0.fontSize), fw: cs0.fontWeight, color: cs0.color,
      mono: /consolas|courier|monospace/i.test(cs0.fontFamily), bg: null
    });
    return out;
  }

  function isBlock(el) {
    var d = getComputedStyle(el).display;
    return !(d === 'inline' || d === 'contents');
  }

  function hasStructuralChild(el) {
    for (var i = 0; i < el.children.length; i++) {
      var c = el.children[i];
      if (c.tagName === 'IMG' || c.tagName === 'svg' || c.tagName === 'TABLE') return true;
      if (isBlock(c) && getComputedStyle(c).display !== 'inline-block') return true;
    }
    return false;
  }

  function tableData(el, base) {
    var rows = [];
    el.querySelectorAll('tr').forEach(function (tr) {
      var cs = getComputedStyle(tr);
      if (!vis(cs)) return;
      var cells = [];
      tr.querySelectorAll('th,td').forEach(function (td) {
        var c = getComputedStyle(td);
        cells.push({
          runs: runsOf(td), rect: rectOf(td, base), span: td.colSpan || 1,
          align: c.textAlign, bg: solid(c.backgroundColor) ? c.backgroundColor : null,
          fs: px(c.fontSize), fw: c.fontWeight, color: c.color,
          bb: { w: px(c.borderBottomWidth), c: c.borderBottomColor },
          head: td.tagName === 'TH'
        });
      });
      rows.push({ rect: rectOf(tr, base), cells: cells });
    });
    return { rect: rectOf(el, base), rows: rows };
  }

  function walk(el, base, out) {
    var cs = getComputedStyle(el);
    if (!vis(cs)) return;
    var r = rectOf(el, base);
    if (r.w <= 0.5 || r.h <= 0.5) return;

    if (el.tagName === 'IMG') {
      out.push({ type: 'img', rect: r, src: el.getAttribute('src'),
                 nw: el.naturalWidth, nh: el.naturalHeight, fit: cs.objectFit });
      return;
    }
    if (el.tagName === 'svg') { out.push({ type: 'svg', rect: r }); return; }
    if (el.tagName === 'TABLE') { out.push({ type: 'table', data: tableData(el, base) }); return; }

    var bs = borders(cs);
    var anyBorder = bs.some(function (b) { return b.w > 0 && b.st !== 'none'; });
    var bg = solid(cs.backgroundColor) ? cs.backgroundColor : null;
    var rad = px(cs.borderTopLeftRadius);
    var structural = hasStructuralChild(el);
    var text = el.textContent.replace(/\s+/g, ' ').trim();

    // 배경·테두리가 있고 글자도 있는 잎 → 도형 하나에 글자를 넣는다
    if (!structural && text) {
      out.push({
        type: 'text', rect: r, runs: runsOf(el),
        align: cs.textAlign, lh: cs.lineHeight === 'normal' ? px(cs.fontSize) * 1.2 : px(cs.lineHeight),
        fs: px(cs.fontSize), bg: bg, rad: rad, borders: anyBorder ? bs : null,
        cls: el.className && el.className.baseVal === undefined ? String(el.className) : ''
      });
      return;
    }
    if (bg || anyBorder) {
      out.push({ type: 'box', rect: r, bg: bg, rad: rad, borders: anyBorder ? bs : null,
                 cls: el.className && el.className.baseVal === undefined ? String(el.className) : '' });
    }
    // ::before 로 그린 작은 사각형(섹션 라벨 점 등)
    var bef = getComputedStyle(el, '::before');
    if (bef.content && bef.content !== 'none' && bef.content !== 'normal'
        && px(bef.width) > 0 && solid(bef.backgroundColor)) {
      out.push({ type: 'box', rect: { x: r.x, y: r.y + (r.h - px(bef.height)) / 2,
                                      w: px(bef.width), h: px(bef.height) },
                 bg: bef.backgroundColor, rad: px(bef.borderTopLeftRadius), borders: null, cls: 'pseudo' });
    }
    if (!structural && !text) return;
    // 구조 자식 사이에 낀 직접 텍스트 노드(예: 아이콘 + 제목)도 Range로 위치를 재서 살린다
    for (var k = 0; k < el.childNodes.length; k++) {
      var nd = el.childNodes[k];
      if (nd.nodeType !== 3) continue;
      var tx = nd.textContent.replace(/\s+/g, ' ');
      if (!tx.trim()) continue;
      var rg = document.createRange(); rg.selectNodeContents(nd);
      var rr = rg.getBoundingClientRect();
      if (rr.width <= 0.5 || rr.height <= 0.5) continue;
      out.push({ type: 'text',
                 rect: { x: rr.left - base.left, y: rr.top - base.top, w: rr.width, h: rr.height },
                 runs: [{ t: tx, fs: px(cs.fontSize), fw: cs.fontWeight, color: cs.color, mono: false }],
                 align: cs.textAlign,
                 lh: cs.lineHeight === 'normal' ? px(cs.fontSize) * 1.2 : px(cs.lineHeight),
                 fs: px(cs.fontSize), bg: null, rad: 0, borders: null, cls: 'textnode' });
    }
    for (var i = 0; i < el.children.length; i++) walk(el.children[i], base, out);
  }

  var slides = [];
  document.querySelectorAll('.slide').forEach(function (sl, idx) {
    var base = sl.getBoundingClientRect();
    var items = [];
    for (var i = 0; i < sl.children.length; i++) walk(sl.children[i], base, items);
    slides.push({ n: idx + 1, w: base.width, h: base.height, items: items });
  });

  fetch('http://127.0.0.1:8940/layout', {
    method: 'POST', mode: 'no-cors',
    headers: { 'Content-Type': 'text/plain' },
    body: JSON.stringify({ slides: slides })
  }).then(function () { document.title = 'EXTRACT-DONE'; });
})();
