# -*- coding: utf-8 -*-
"""발표.html → 발표.pptx (16:9, 슬라이드 1장 = 이미지 1장)

실행: python _발표-pptx빌드.py     (먼저 _발표-빌드.py로 발표.html을 만들어 둘 것)
흐름: 인쇄용 HTML 생성 → Chrome 헤드리스로 PDF 인쇄 → PyMuPDF로 2560×1440 래스터 → python-pptx로 조립

HTML 쪽 요소(컨테이너 쿼리 배치·SVG 도해·마커 오버레이)를 그대로 살리기 위해
텍스트를 다시 짜지 않고 렌더 결과를 그대로 넣는다. 따라서 PPT에서 글자 수정은 불가하며,
내용을 고칠 때는 HTML을 고치고 이 스크립트를 다시 돌린다.

필요: Chrome 또는 Edge, PyMuPDF, python-pptx
"""
import os, glob, shutil, subprocess, tempfile, io, sys
import fitz
from pptx import Presentation
from pptx.util import Inches

DECK = os.path.dirname(os.path.abspath(__file__))
SRC  = os.path.join(DECK, '발표.html')
OUT  = os.path.join(DECK, '발표.pptx')

# 슬라이드 순서와 같은 제목 — PPT 발표자 노트에 넣어 개요에서 장을 찾을 수 있게 한다
TITLES = ["표지", "목차", "프로그램 UI 구성요소", "실행 순서", "작업 순서", "도면 4종",
          "예시 모델과 부재 분할", "제작도", "조립도", "설치도", "가공도", "값의 출처",
          "도면별 Osnap 선별 기준", "Osnap 선별 엔진", "동률일 때의 우선순위",
          "치수 보조선 배치", "제작도와 가공도의 배율 차이", "치수 선별 기준",
          "각도 표시 기준", "설치 위치 치수 기준", "부재 목록 수집", "도면 BOM 표 작성"]

BROWSERS = [r'C:\Program Files\Google\Chrome\Application\chrome.exe',
            r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe']

# 화면용 CSS는 슬라이드를 뷰포트에 맞추므로, 인쇄용으로 13.333×7.5in에 고정한다
PRINT_CSS = '''<style>
@media print{
  @page{ size:13.333in 7.5in; margin:0; }
  *{ -webkit-print-color-adjust:exact !important; print-color-adjust:exact !important; }
  html,body{ margin:0; padding:0; background:#fff; }
  .deck{ height:auto !important; overflow:visible !important; scroll-snap-type:none !important; }
  .page{ display:block !important; width:13.333in !important; height:7.5in !important;
         min-height:0 !important; padding:0 !important; margin:0 !important;
         page-break-after:always; break-after:page; page-break-inside:avoid; break-inside:avoid; }
  .page:last-child{ page-break-after:auto; break-after:auto; }
  .slide{ width:13.333in !important; height:7.5in !important; aspect-ratio:auto !important;
          border-radius:0 !important; box-shadow:none !important; }
  #bar,#hud{ display:none !important; }
}
</style>
</head>'''


def main():
    browser = next((b for b in BROWSERS if os.path.exists(b)), None)
    if not browser:
        sys.exit('Chrome/Edge를 찾지 못했습니다.')

    tmp = tempfile.mkdtemp(prefix='deck-pptx-')
    try:
        html = io.open(SRC, encoding='utf-8').read().replace('</head>', PRINT_CSS, 1)
        page = os.path.join(tmp, 'print.html')
        io.open(page, 'w', encoding='utf-8', newline='\n').write(html)

        pdf = os.path.join(tmp, 'deck.pdf')
        subprocess.run([browser, '--headless=new', '--disable-gpu', '--no-sandbox',
                        '--no-pdf-header-footer', '--virtual-time-budget=20000',
                        '--print-to-pdf=' + pdf, 'file:///' + page.replace('\\', '/')],
                       check=True, capture_output=True)

        doc = fitz.open(pdf)
        if doc.page_count != len(TITLES):
            sys.exit('슬라이드 수(%d)와 제목 수(%d)가 다릅니다 — TITLES를 갱신하세요.'
                     % (doc.page_count, len(TITLES)))
        mat = fitz.Matrix(2560 / 960, 1440 / 540)          # 960×540pt → 2560×1440px
        shots = []
        for i, pg in enumerate(doc, 1):
            fp = os.path.join(tmp, 'slide-%02d.png' % i)
            pg.get_pixmap(matrix=mat, alpha=False).save(fp)
            shots.append(fp)

        prs = Presentation()
        prs.slide_width, prs.slide_height = Inches(13.333), Inches(7.5)
        blank = prs.slide_layouts[6]
        for fp, title in zip(shots, TITLES):
            s = prs.slides.add_slide(blank)
            s.shapes.add_picture(fp, 0, 0, width=prs.slide_width, height=prs.slide_height)
            s.notes_slide.notes_text_frame.text = title
        prs.save(OUT)
        print('=> %s (%d슬라이드, %.1f MB)' % (OUT, len(TITLES), os.path.getsize(OUT) / 1048576))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    main()
