import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
import { domToModelSelection } from '../renderer/cursor-manager';

export type RenderCallback = (response: EngineResponse) => void;

/**
 * SVG horizontal ruler with inch marks and draggable indent markers.
 * Positioned above the page canvas, aligned with page margins.
 */
export class HorizontalRuler {
  private el: HTMLElement;
  private svg: SVGSVGElement;
  private engine: EngineBridge;
  private canvas: HTMLElement;
  private onResponse: RenderCallback;

  // Page dimensions in px
  private pageWidth = 816;
  private marginLeft = 96;
  private marginRight = 96;
  private zoom = 1.0;

  // Current indent values in twips
  private indentLeft = 0;
  private indentFirstLine = 0;

  constructor(
    container: HTMLElement,
    engine: EngineBridge,
    canvas: HTMLElement,
    onResponse: RenderCallback,
  ) {
    this.engine = engine;
    this.canvas = canvas;
    this.onResponse = onResponse;

    this.el = document.createElement('div');
    this.el.className = 'ruler-h flex justify-center border-b border-gray-200 bg-gray-50 overflow-hidden flex-shrink-0 select-none';

    const ns = 'http://www.w3.org/2000/svg';
    this.svg = document.createElementNS(ns, 'svg');
    this.svg.setAttribute('width', String(this.pageWidth));
    this.svg.setAttribute('height', '24');
    this.svg.classList.add('ruler-svg');

    this.el.appendChild(this.svg);
    container.appendChild(this.el);

    this.render();
  }

  getElement(): HTMLElement {
    return this.el;
  }

  setZoom(zoom: number): void {
    if (this.zoom === zoom) return;
    this.zoom = zoom;
    this.svg.setAttribute('width', String(this.pageWidth * zoom));
    this.render();
  }

  syncScrollLeft(scrollLeft: number): void {
    this.svg.style.transform = `translateX(-${scrollLeft}px)`;
  }

  updateDimensions(pageWidth: number, marginLeft: number, marginRight: number): void {
    this.pageWidth = pageWidth;
    this.marginLeft = marginLeft;
    this.marginRight = marginRight;
    this.svg.setAttribute('width', String(pageWidth * this.zoom));
    this.render();
  }

  updateIndents(indentLeft: number, indentFirstLine: number): void {
    this.indentLeft = indentLeft;
    this.indentFirstLine = indentFirstLine;
    this.render();
  }

  private render(): void {
    const ns = 'http://www.w3.org/2000/svg';
    this.svg.innerHTML = '';

    const Z = this.zoom;
    const contentLeft = this.marginLeft * Z;
    const contentRight = (this.pageWidth - this.marginRight) * Z;

    // Background: gray for margin areas, white for content
    const bg = document.createElementNS(ns, 'rect');
    bg.setAttribute('x', '0');
    bg.setAttribute('y', '0');
    bg.setAttribute('width', String(this.pageWidth * Z));
    bg.setAttribute('height', '24');
    bg.setAttribute('fill', '#e0e0e0');
    this.svg.appendChild(bg);

    const contentBg = document.createElementNS(ns, 'rect');
    contentBg.setAttribute('x', String(contentLeft));
    contentBg.setAttribute('y', '0');
    contentBg.setAttribute('width', String(contentRight - contentLeft));
    contentBg.setAttribute('height', '24');
    contentBg.setAttribute('fill', '#fff');
    this.svg.appendChild(contentBg);

    // Inch marks: 96px per inch at 96dpi
    const ppi = 96;
    const totalInches = this.pageWidth / ppi;

    for (let i = 0; i <= totalInches; i++) {
      const x = i * ppi * Z;

      // Major tick
      this.addLine(x, 0, x, 14, '#666', 1);

      // Inch label
      if (i > 0 && i < totalInches) {
        const text = document.createElementNS(ns, 'text');
        text.setAttribute('x', String(x));
        text.setAttribute('y', '22');
        text.setAttribute('text-anchor', 'middle');
        text.setAttribute('font-size', '9');
        text.setAttribute('fill', '#666');
        text.textContent = String(i);
        this.svg.appendChild(text);
      }

      // Half-inch ticks
      if (i < totalInches) {
        this.addLine(x + ppi * Z / 2, 4, x + ppi * Z / 2, 14, '#999', 0.5);
        // Quarter-inch ticks
        this.addLine(x + ppi * Z / 4, 8, x + ppi * Z / 4, 14, '#bbb', 0.5);
        this.addLine(x + 3 * ppi * Z / 4, 8, x + 3 * ppi * Z / 4, 14, '#bbb', 0.5);
      }
    }

    // Indent markers (converted from twips to px)
    const twipsToPx = (twips: number) => twips * 96 / 1440;

    // First-line indent marker (downward triangle at top)
    const firstLineX = contentLeft + twipsToPx(this.indentLeft + this.indentFirstLine) * Z;
    this.addTriangle(firstLineX, 0, 'down', '#4285f4', 'first-line-indent');

    // Left indent marker (upward triangle at bottom)
    const leftIndentX = contentLeft + twipsToPx(this.indentLeft) * Z;
    this.addTriangle(leftIndentX, 18, 'up', '#4285f4', 'left-indent');

    // Right margin marker
    this.addTriangle(contentRight, 18, 'up', '#4285f4', 'right-margin');
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

  private addTriangle(
    cx: number, cy: number,
    direction: 'up' | 'down',
    fill: string,
    _markerType: string,
  ): void {
    const ns = 'http://www.w3.org/2000/svg';
    const polygon = document.createElementNS(ns, 'polygon');
    const size = 6;

    let points: string;
    if (direction === 'down') {
      points = `${cx - size},${cy} ${cx + size},${cy} ${cx},${cy + size}`;
    } else {
      points = `${cx - size},${cy + size} ${cx + size},${cy + size} ${cx},${cy}`;
    }

    polygon.setAttribute('points', points);
    polygon.setAttribute('fill', fill);
    polygon.style.cursor = 'ew-resize';

    // Make draggable
    let startX = 0;
    let startCx = cx;

    const onMouseMove = (e: MouseEvent) => {
      const dx = e.clientX - startX;
      const newCx = startCx + dx;
      // Recalculate points
      let newPoints: string;
      if (direction === 'down') {
        newPoints = `${newCx - size},${cy} ${newCx + size},${cy} ${newCx},${cy + size}`;
      } else {
        newPoints = `${newCx - size},${cy + size} ${newCx + size},${cy + size} ${newCx},${cy}`;
      }
      polygon.setAttribute('points', newPoints);
    };

    const onMouseUp = async (e: MouseEvent) => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);

      const dx = e.clientX - startX;
      const deltaTwips = Math.round(dx * 1440 / 96 / this.zoom);

      const sel = domToModelSelection(this.canvas);
      if (!sel) return;

      // Apply indent change
      const response = await this.engine.setIndent(deltaTwips, 0, sel);
      this.onResponse(response);
    };

    polygon.addEventListener('mousedown', (e) => {
      e.preventDefault();
      startX = e.clientX;
      startCx = cx;
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    });

    this.svg.appendChild(polygon);
  }
}
