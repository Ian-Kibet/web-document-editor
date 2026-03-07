/**
 * SVG vertical ruler with inch marks.
 * Positioned to the left of the page canvas, scrolls vertically in sync.
 */
export class VerticalRuler {
  private el: HTMLElement;
  private svg: SVGSVGElement;

  private pageHeight = 1056;
  private marginTop = 96;
  private marginBottom = 96;
  private pageCount = 1;
  private zoom = 1.0;
  private activePage = 1;
  private activePageTopScrollY = 20; // scroll-y of active page's top (default = page 1)
  private totalScrollHeight = 0;     // 0 = use formula fallback
  private readonly gapHeight = 24;   // matches GAP_HEIGHT in page-layout.ts
  private readonly topPadding = 20;  // matches pages.css .pages-wrapper padding

  constructor(container: HTMLElement) {
    this.el = document.createElement('div');
    this.el.className = 'ruler-v w-6 border-r border-gray-200 bg-gray-50 overflow-hidden flex-shrink-0 select-none';

    const ns = 'http://www.w3.org/2000/svg';
    this.svg = document.createElementNS(ns, 'svg');
    this.svg.setAttribute('width', '24');
    this.svg.classList.add('ruler-svg');

    this.el.appendChild(this.svg);
    container.appendChild(this.el);

    this.updateSvgHeight();
    this.render();
  }

  getElement(): HTMLElement {
    return this.el;
  }

  updateDimensions(
    pageHeight: number,
    marginTop: number,
    marginBottom: number,
    activePage?: number,
    activePageTopScrollY?: number,
  ): void {
    this.pageHeight = pageHeight;
    this.marginTop = marginTop;
    this.marginBottom = marginBottom;
    if (activePage !== undefined) this.activePage = activePage;
    if (activePageTopScrollY !== undefined) this.activePageTopScrollY = activePageTopScrollY;
    this.updateSvgHeight();
    this.render();
  }

  setTotalScrollHeight(h: number): void {
    this.totalScrollHeight = h;
    this.updateSvgHeight();
    this.render();
  }

  setPageCount(count: number): void {
    if (this.pageCount === count) return;
    this.pageCount = count;
    this.updateSvgHeight();
    this.render();
  }

  setZoom(zoom: number): void {
    if (this.zoom === zoom) return;
    this.zoom = zoom;
    this.updateSvgHeight();
    this.render();
  }

  /**
   * Sync vertical scroll position with the page container.
   * Uses CSS transform so the ruler follows at any scroll depth,
   * not limited by SVG content height.
   */
  syncScroll(scrollTop: number): void {
    this.svg.style.transform = `translateY(-${scrollTop}px)`;
  }

  private updateSvgHeight(): void {
    const total = this.totalScrollHeight > 0
      ? this.totalScrollHeight * this.zoom
      : (this.topPadding
         + this.pageHeight * this.pageCount
         + this.gapHeight * (this.pageCount - 1)
         + this.topPadding) * this.zoom;
    this.svg.setAttribute('height', String(total));
  }

  private render(): void {
    const ns = 'http://www.w3.org/2000/svg';
    const ppi = 96;
    const Z = this.zoom;
    this.svg.innerHTML = '';

    const totalH = this.totalScrollHeight > 0
      ? this.totalScrollHeight * Z
      : (this.topPadding
         + this.pageHeight * this.pageCount
         + this.gapHeight * (this.pageCount - 1)
         + this.topPadding) * Z;

    // Full gray background (margins, padding, gaps)
    const bg = document.createElementNS(ns, 'rect');
    bg.setAttribute('x', '0');
    bg.setAttribute('y', '0');
    bg.setAttribute('width', '24');
    bg.setAttribute('height', String(totalH));
    bg.setAttribute('fill', '#e0e0e0');
    this.svg.appendChild(bg);

    // Only draw content zone and inch marks for the active cursor page
    const p = this.activePage - 1;
    if (p >= 0 && p < this.pageCount) {
      const pageY = this.activePageTopScrollY * Z;
      const contentTop = pageY + this.marginTop * Z;
      const contentBottom = pageY + (this.pageHeight - this.marginBottom) * Z;

      // White content zone
      const contentBg = document.createElementNS(ns, 'rect');
      contentBg.setAttribute('x', '0');
      contentBg.setAttribute('y', String(contentTop));
      contentBg.setAttribute('width', '24');
      contentBg.setAttribute('height', String(contentBottom - contentTop));
      contentBg.setAttribute('fill', '#fff');
      this.svg.appendChild(contentBg);

      // Inch marks (0..11 for US Letter = 1056/96)
      const totalInches = this.pageHeight / ppi;
      const marginTopInches = this.marginTop / ppi;
      for (let i = 0; i <= totalInches; i++) {
        const y = pageY + i * ppi * Z;
        this.addLine(10, y, 24, y, '#666', 1);
        if (i < totalInches) {
          const relInch = i - marginTopInches;
          const isOrigin = Math.abs(relInch) < 0.01;
          const isAbove  = relInch < -0.01;

          const text = document.createElementNS(ns, 'text');
          text.setAttribute('x', '5');
          text.setAttribute('y', String(y + 3));
          text.setAttribute('text-anchor', 'middle');
          text.setAttribute('font-size', '9');
          text.setAttribute('fill', isOrigin || isAbove ? '#999' : '#666');
          text.textContent = isOrigin ? '0' : String(Math.round(Math.abs(relInch)));
          this.svg.appendChild(text);

          this.addLine(14, y + ppi * Z / 2, 24, y + ppi * Z / 2, '#999', 0.5);
          this.addLine(18, y + ppi * Z / 4, 24, y + ppi * Z / 4, '#bbb', 0.5);
          this.addLine(18, y + 3 * ppi * Z / 4, 24, y + 3 * ppi * Z / 4, '#bbb', 0.5);
        }
      }
    }
  }

  private addLine(
    x1: number, y1: number, x2: number, y2: number,
    stroke: string, width: number,
  ): void {
    const ns = 'http://www.w3.org/2000/svg';
    const line = document.createElementNS(ns, 'line');
    line.setAttribute('x1', String(x1));
    line.setAttribute('y1', String(y1));
    line.setAttribute('x2', String(x2));
    line.setAttribute('y2', String(y2));
    line.setAttribute('stroke', stroke);
    line.setAttribute('stroke-width', String(width));
    this.svg.appendChild(line);
  }
}
