import type { DebugSectionSnapshot } from '../renderer/page-layout';
import { Ruler, X } from 'lucide';

// ── Icon helper (same as sidebar.ts) ─────────────────────────────────────────

function createIconSvg(
  iconDef: [string, Record<string, string>][],
  size = 14,
): SVGSVGElement {
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg') as SVGSVGElement;
  svg.setAttribute('width', String(size));
  svg.setAttribute('height', String(size));
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('fill', 'none');
  svg.setAttribute('stroke', 'currentColor');
  svg.setAttribute('stroke-width', '1.75');
  svg.setAttribute('stroke-linecap', 'round');
  svg.setAttribute('stroke-linejoin', 'round');
  svg.style.pointerEvents = 'none';
  svg.style.flexShrink = '0';
  for (const [tag, attrs] of iconDef) {
    const el = document.createElementNS(ns, tag);
    for (const [key, val] of Object.entries(attrs)) {
      el.setAttribute(key, val);
    }
    svg.appendChild(el);
  }
  return svg;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function twipsToInches(twips: number): string {
  return (twips / 1440).toFixed(2);
}

function pxToInches(px: number): string {
  return (px / 96).toFixed(2);
}

function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string,
): HTMLElementTagNameMap[K] {
  const e = document.createElement(tag);
  if (className) e.className = className;
  return e;
}

// ── DebugMarginPanel ──────────────────────────────────────────────────────────

export class DebugMarginPanel {
  /** Width-transition outer wrapper (0 → 220px). Inserted into editorRow. */
  private wrapper: HTMLElement;
  /** Fixed-width 220px content pane. */
  private inner: HTMLElement;
  private body: HTMLElement;
  private tabRow: HTMLElement;

  private isOpen = false;
  private currentSection = 0;
  private snapshots: DebugSectionSnapshot[] = [];
  private canvas: HTMLElement;

  constructor(mountPoint: HTMLElement, canvas: HTMLElement) {
    this.canvas = canvas;

    // Outer wrapper — controls the width slide transition
    this.wrapper = mountPoint;
    this.wrapper.style.cssText =
      'overflow:hidden;width:0;transition:width 200ms ease;flex-shrink:0;border-left:1px solid #e5e7eb;';

    // 220px inner content
    this.inner = el('div');
    this.inner.style.cssText =
      'width:260px;height:100%;display:flex;flex-direction:column;background:#f9fafb;font-size:12px;';
    this.wrapper.appendChild(this.inner);

    // Header bar
    const header = el('div');
    header.style.cssText =
      'display:flex;align-items:center;justify-content:space-between;padding:6px 8px;background:white;border-bottom:1px solid #e5e7eb;flex-shrink:0;';
    const title = el('span');
    title.style.cssText = 'font-weight:600;color:#374151;display:flex;align-items:center;gap:4px;';
    title.appendChild(createIconSvg(Ruler as [string, Record<string, string>][], 12));
    title.append(' Margins');
    const closeBtn = el('button');
    closeBtn.type = 'button';
    closeBtn.style.cssText =
      'display:flex;align-items:center;color:#9ca3af;padding:2px;border-radius:3px;cursor:pointer;background:none;border:none;';
    closeBtn.appendChild(createIconSvg(X as [string, Record<string, string>][], 12));
    closeBtn.addEventListener('click', () => this.close());
    header.appendChild(title);
    header.appendChild(closeBtn);
    this.inner.appendChild(header);

    // Section tabs (hidden until >1 section)
    this.tabRow = el('div');
    this.tabRow.style.cssText =
      'display:none;padding:4px 8px;gap:4px;background:white;border-bottom:1px solid #e5e7eb;flex-shrink:0;flex-wrap:wrap;';
    this.inner.appendChild(this.tabRow);

    // Scrollable body
    this.body = el('div');
    this.body.style.cssText =
      'flex:1;overflow-y:auto;padding:8px;font-family:ui-monospace,monospace;';
    this.inner.appendChild(this.body);
  }

  /** Returns the wrapper element (already inserted by caller). */
  getElement(): HTMLElement {
    return this.wrapper;
  }

  toggle(): void {
    this.isOpen ? this.close() : this.open();
  }

  open(): void {
    this.isOpen = true;
    this.wrapper.style.width = '260px';
    this.render();
  }

  close(): void {
    this.isOpen = false;
    this.wrapper.style.width = '0';
  }

  /** Called when cursor moves; updates the displayed section if it changed. */
  setCursorSection(index: number): void {
    if (index === this.currentSection) return;
    this.currentSection = index;
    if (this.isOpen) this.render();
  }

  update(snapshots: DebugSectionSnapshot[]): void {
    this.snapshots = snapshots;
    // Clamp section index
    if (this.currentSection >= snapshots.length) {
      this.currentSection = 0;
    }
    if (this.isOpen) this.render();
  }

  // ── Private ────────────────────────────────────────────────────────────────

  private render(): void {
    const snap = this.snapshots[this.currentSection];

    // Rebuild section tabs
    this.tabRow.innerHTML = '';
    if (this.snapshots.length > 1) {
      this.tabRow.style.display = 'flex';
      this.snapshots.forEach((_, i) => {
        const tab = el('button');
        tab.type = 'button';
        tab.textContent = `§${i + 1}`;
        tab.style.cssText =
          `padding:2px 6px;border-radius:3px;font-size:11px;cursor:pointer;border:1px solid #d1d5db;` +
          (i === this.currentSection
            ? 'background:#3b82f6;color:white;border-color:#3b82f6;'
            : 'background:white;color:#374151;');
        tab.addEventListener('click', () => {
          this.currentSection = i;
          this.render();
        });
        this.tabRow.appendChild(tab);
      });
    } else {
      this.tabRow.style.display = 'none';
    }

    // Rebuild body
    this.body.innerHTML = '';

    if (!snap) {
      const empty = el('div');
      empty.style.color = '#9ca3af';
      empty.textContent = 'No section data.';
      this.body.appendChild(empty);
      return;
    }

    const dom = this.measureSection(this.currentSection);

    // Page
    this.body.appendChild(this.sectionLabel('Page'));
    this.body.appendChild(this.row(
      `${snap.pxPageWidth} × ${snap.pxPageHeight} px`,
      `${pxToInches(snap.pxPageWidth)}" × ${pxToInches(snap.pxPageHeight)}"`,
    ));

    // Margins
    this.body.appendChild(this.sectionLabel('Margins'));
    this.body.appendChild(this.marginRow('Top',    snap.rawMarginTop,    snap.pxMarginTop,    dom['padding-top']));
    this.body.appendChild(this.marginRow('Bottom', snap.rawMarginBottom, snap.pxMarginBottom, dom['padding-bottom']));
    this.body.appendChild(this.marginRow('Left',   snap.rawMarginLeft,   snap.pxMarginLeft,   dom['padding-left']));
    this.body.appendChild(this.marginRow('Right',  snap.rawMarginRight,  snap.pxMarginRight,  dom['padding-right']));

    // Header
    this.body.appendChild(this.sectionLabel('Header'));
    this.body.appendChild(this.twipRow('Distance', snap.rawHeaderDistance, snap.pxHeaderDistance));
    this.body.appendChild(this.pxRow('Zone height', snap.pxMarginTop));
    this.body.appendChild(this.pxRow('Content zone', snap.pxMarginTop - snap.pxHeaderDistance));

    // Footer
    this.body.appendChild(this.sectionLabel('Footer'));
    this.body.appendChild(this.twipRow('Distance', snap.rawFooterDistance, snap.pxFooterDistance));
    this.body.appendChild(this.pxRow('Zone height', snap.pxMarginBottom));
    this.body.appendChild(this.pxRow('Content zone', snap.pxMarginBottom - snap.pxFooterDistance));

    // Content area
    this.body.appendChild(this.sectionLabel('Content Area'));
    this.body.appendChild(this.pxRow('Width',  snap.pxContentWidth));
    this.body.appendChild(this.pxRow('Height', snap.pxContentHeight));

    // DOM measured
    this.body.appendChild(this.sectionLabel('DOM Measured'));
    this.body.appendChild(this.domCheckRow('padding-top',    snap.pxMarginTop,    dom['padding-top']));
    this.body.appendChild(this.domCheckRow('padding-bottom', snap.pxMarginBottom, dom['padding-bottom']));
    this.body.appendChild(this.domCheckRow('padding-left',   snap.pxMarginLeft,   dom['padding-left']));
    this.body.appendChild(this.domCheckRow('padding-right',  snap.pxMarginRight,  dom['padding-right']));
  }

  /** Read computed styles on the nth <section> inside the canvas. */
  private measureSection(index: number): Record<string, number> {
    const sections = this.canvas.querySelectorAll('section');
    const sectionEl = sections[index] ?? sections[0];
    if (!sectionEl) return {};
    const cs = getComputedStyle(sectionEl);
    return {
      'padding-top':    parseFloat(cs.paddingTop)    || 0,
      'padding-bottom': parseFloat(cs.paddingBottom) || 0,
      'padding-left':   parseFloat(cs.paddingLeft)   || 0,
      'padding-right':  parseFloat(cs.paddingRight)  || 0,
    };
  }

  // ── Row builders ───────────────────────────────────────────────────────────

  private sectionLabel(text: string): HTMLElement {
    const d = el('div');
    d.style.cssText =
      'color:#9ca3af;text-transform:uppercase;letter-spacing:0.05em;font-size:10px;' +
      'margin-top:10px;margin-bottom:2px;border-bottom:1px solid #e5e7eb;padding-bottom:2px;';
    d.textContent = text;
    return d;
  }

  private row(main: string, sub: string): HTMLElement {
    const d = el('div');
    d.style.cssText = 'display:flex;justify-content:space-between;padding:1px 0;';
    const m = el('span');
    m.style.color = '#1f2937';
    m.textContent = main;
    const s = el('span');
    s.style.color = '#6b7280';
    s.textContent = sub;
    d.appendChild(m);
    d.appendChild(s);
    return d;
  }

  /** twips → px → inches row, with optional DOM comparison. */
  private marginRow(label: string, twips: number, px: number, domPx?: number): HTMLElement {
    const d = el('div');
    d.style.cssText = 'padding:1px 0;';
    const top = el('div');
    top.style.cssText = 'display:flex;justify-content:space-between;';
    const lbl = el('span');
    lbl.style.color = '#374151';
    lbl.textContent = label;
    const val = el('span');
    val.style.cssText = 'color:#1f2937;font-weight:500;white-space:nowrap;';
    val.textContent = `${twips} tw → ${px} px → ${twipsToInches(twips)}"`;
    top.appendChild(lbl);
    top.appendChild(val);
    d.appendChild(top);

    if (domPx !== undefined) {
      const mismatch = Math.abs(domPx - px) > 0.5;
      const check = el('div');
      check.style.cssText = 'display:flex;justify-content:flex-end;font-size:10px;';
      const span = el('span');
      span.style.color = mismatch ? '#f97316' : '#16a34a';
      span.textContent = mismatch ? `DOM: ${domPx}px ⚠` : '✓';
      check.appendChild(span);
      d.appendChild(check);
    }
    return d;
  }

  private twipRow(label: string, twips: number, px: number): HTMLElement {
    const d = el('div');
    d.style.cssText = 'display:flex;justify-content:space-between;padding:1px 0;';
    const lbl = el('span');
    lbl.style.color = '#374151';
    lbl.textContent = label;
    const val = el('span');
    val.style.cssText = 'color:#1f2937;white-space:nowrap;';
    val.textContent = `${twips} tw → ${px} px → ${twipsToInches(twips)}"`;
    d.appendChild(lbl);
    d.appendChild(val);
    return d;
  }

  private pxRow(label: string, px: number): HTMLElement {
    const d = el('div');
    d.style.cssText = 'display:flex;justify-content:space-between;padding:1px 0;';
    const lbl = el('span');
    lbl.style.color = '#374151';
    lbl.textContent = label;
    const val = el('span');
    val.style.cssText = 'color:#1f2937;white-space:nowrap;';
    val.textContent = `${px} px (${pxToInches(px)}")`;
    d.appendChild(lbl);
    d.appendChild(val);
    return d;
  }

  private domCheckRow(prop: string, expected: number, actual: number): HTMLElement {
    const d = el('div');
    d.style.cssText = 'display:flex;justify-content:space-between;padding:1px 0;';
    const lbl = el('span');
    lbl.style.color = '#374151';
    lbl.textContent = prop;
    const val = el('span');
    const mismatch = Math.abs(actual - expected) > 0.5;
    val.style.cssText = mismatch
      ? 'color:#f97316;font-weight:600;white-space:nowrap;'
      : 'color:#16a34a;white-space:nowrap;';
    val.textContent = mismatch ? `${actual}px ⚠` : `${actual}px ✓`;
    d.appendChild(lbl);
    d.appendChild(val);
    return d;
  }
}
