/**
 * Multi-page visual layout with multi-section support.
 *
 * Single contentEditable canvas inside one page frame. When content exceeds
 * one page height, gap margins are injected on block elements at page
 * boundaries to create visual separation, and overlay divs render the
 * "page break" strips (gray gap with shadow edges and page numbers).
 *
 * Each section renders as its own "paper" element with per-section page
 * width and margins (as CSS padding + box-sizing: border-box). The page
 * frame is a transparent container at the maximum width across all sections.
 */

import type { RenderNode, SectionInfo } from '../bridge/types';
import { createReadOnlyDomNode } from './dom-renderer';

// US Letter at 96dpi (defaults)
const PAGE_WIDTH = 816;
const PAGE_HEIGHT = 1056;

// 1 inch margins at 96dpi
const MARGIN_TOP = 96;
const MARGIN_BOTTOM = 96;
const MARGIN_LEFT = 96;
const MARGIN_RIGHT = 96;

/** Height of the visible gap between pages (the gray strip). */
const GAP_HEIGHT = 24;

export interface PageLayoutConfig {
  pageWidth?: number;
  pageHeight?: number;
  marginTop?: number;
  marginBottom?: number;
  marginLeft?: number;
  marginRight?: number;
}

export interface DebugSectionSnapshot {
  index: number;
  // Raw doc values (twips, from engine)
  rawPageWidth: number;
  rawPageHeight: number;
  rawMarginTop: number;
  rawMarginBottom: number;
  rawMarginLeft: number;
  rawMarginRight: number;
  rawHeaderDistance: number;
  rawFooterDistance: number;
  // Applied values (px, after twips→px conversion)
  pxPageWidth: number;
  pxPageHeight: number;
  pxMarginTop: number;
  pxMarginBottom: number;
  pxMarginLeft: number;
  pxMarginRight: number;
  pxHeaderDistance: number;
  pxFooterDistance: number;
  pxContentWidth: number;
  pxContentHeight: number;
}

interface SectionConfig {
  pageWidth: number;
  pageHeight: number;
  marginTop: number;
  marginBottom: number;
  marginLeft: number;
  marginRight: number;
  contentWidth: number;
  contentHeight: number;
  breakType: string;
  headers?: Record<string, RenderNode[]>;
  footers?: Record<string, RenderNode[]>;
  titlePage: boolean;
  headerDistance: number;  // px
  footerDistance: number;  // px
  columnCount: number;
}

/** Info about a single page break for overlay rendering. */
interface BreakInfo {
  y: number;
  marginBottom: number;
  marginTop: number;
  pageWidth: number;
  /** Section index of the page ending at this break. */
  endingSectionIndex: number;
  /** 0-based page number within the ending section. */
  endingPageInSection: number;
  /** Section index of the page starting after this break. */
  startingSectionIndex: number;
  /** 0-based page number within the starting section. */
  startingPageInSection: number;
}

function twipsToPx(twips: number): number {
  return Math.round(twips * 96 / 1440);
}

export class PageLayout {
  private container: HTMLElement;
  private pagesWrapper: HTMLElement;
  private pageFrame: HTMLElement;
  private canvas: HTMLElement;
  private overlay: HTMLElement;
  private config: Required<PageLayoutConfig>;
  private sectionConfigs: SectionConfig[] = [];
  private rawSections: SectionInfo[] = [];
  private adjusting = false;
  private breakBottomYPositions: number[] = [];
  private breakPageBottomYPositions: number[] = [];
  pageSectionMap: number[] = [];

  /** Total number of pages after the last pagination. */
  pageCount = 1;

  constructor(container: HTMLElement, config: PageLayoutConfig = {}) {
    this.container = container;
    this.config = {
      pageWidth: config.pageWidth ?? PAGE_WIDTH,
      pageHeight: config.pageHeight ?? PAGE_HEIGHT,
      marginTop: config.marginTop ?? MARGIN_TOP,
      marginBottom: config.marginBottom ?? MARGIN_BOTTOM,
      marginLeft: config.marginLeft ?? MARGIN_LEFT,
      marginRight: config.marginRight ?? MARGIN_RIGHT,
    };

    this.pagesWrapper = document.createElement('div');
    this.pagesWrapper.className = 'pages-wrapper';

    // Page frame: transparent container (sections provide paper styling)
    this.pageFrame = document.createElement('div');
    this.pageFrame.className = 'page-frame';
    this.pageFrame.style.width = `${this.config.pageWidth}px`;
    this.pageFrame.style.minHeight = `${this.config.pageHeight}px`;

    // Canvas (contentEditable area) — full page width, sections handle margins
    this.canvas = document.createElement('div');
    this.canvas.className = 'editor-canvas';
    this.canvas.contentEditable = 'true';
    this.canvas.spellcheck = false;
    this.canvas.setAttribute('role', 'textbox');
    this.canvas.setAttribute('aria-multiline', 'true');
    this.canvas.style.width = `${this.config.pageWidth}px`;
    this.canvas.style.minHeight = `${this.config.pageHeight}px`;
    this.canvas.style.outline = 'none';
    this.canvas.style.padding = '0';

    // Overlay container for page-break visuals (sits on top of canvas, no pointer events)
    this.overlay = document.createElement('div');
    this.overlay.className = 'page-breaks-overlay';

    this.pageFrame.appendChild(this.canvas);
    this.pageFrame.appendChild(this.overlay);
    this.pagesWrapper.appendChild(this.pageFrame);
    this.container.appendChild(this.pagesWrapper);
  }

  get contentWidth(): number {
    return this.config.pageWidth - this.config.marginLeft - this.config.marginRight;
  }

  get contentHeight(): number {
    return this.config.pageHeight - this.config.marginTop - this.config.marginBottom;
  }

  getCanvas(): HTMLElement {
    return this.canvas;
  }

  getDebugSectionData(): DebugSectionSnapshot[] {
    return this.rawSections.map((raw, i) => {
      const sc = this.sectionConfigs[i] ?? this.sectionConfigs[0];
      return {
        index: i,
        rawPageWidth: raw.pageWidth,
        rawPageHeight: raw.pageHeight,
        rawMarginTop: raw.marginTop,
        rawMarginBottom: raw.marginBottom,
        rawMarginLeft: raw.marginLeft,
        rawMarginRight: raw.marginRight,
        rawHeaderDistance: raw.headerDistance ?? 720,
        rawFooterDistance: raw.footerDistance ?? 720,
        pxPageWidth: sc.pageWidth,
        pxPageHeight: sc.pageHeight,
        pxMarginTop: sc.marginTop,
        pxMarginBottom: sc.marginBottom,
        pxMarginLeft: sc.marginLeft,
        pxMarginRight: sc.marginRight,
        pxHeaderDistance: sc.headerDistance,
        pxFooterDistance: sc.footerDistance,
        pxContentWidth: sc.contentWidth,
        pxContentHeight: sc.contentHeight,
      };
    });
  }

  /**
   * Update page dimensions from sections metadata.
   * Frame becomes a transparent container at the max page width.
   * Sections handle their own paper styling and margins via inline padding.
   */
  updateFromSections(sections: SectionInfo[]): void {
    if (sections.length === 0) return;

    this.rawSections = sections;

    this.sectionConfigs = sections.map((s) => {
      const pw = twipsToPx(s.pageWidth);
      const ph = twipsToPx(s.pageHeight);
      const mt = twipsToPx(s.marginTop);
      const mb = twipsToPx(s.marginBottom);
      const ml = twipsToPx(s.marginLeft);
      const mr = twipsToPx(s.marginRight);
      return {
        pageWidth: pw,
        pageHeight: ph,
        marginTop: mt,
        marginBottom: mb,
        marginLeft: ml,
        marginRight: mr,
        contentWidth: pw - ml - mr,
        contentHeight: ph - mt - mb,
        breakType: s.breakType,
        headers: s.headers,
        footers: s.footers,
        titlePage: s.titlePage ?? false,
        headerDistance: twipsToPx(s.headerDistance ?? 720),
        footerDistance: twipsToPx(s.footerDistance ?? 720),
        columnCount: s.columnCount ?? 1,
      };
    });

    // Frame uses maximum page width across all sections
    const maxPageWidth = Math.max(...this.sectionConfigs.map((s) => s.pageWidth));

    const first = this.sectionConfigs[0];
    this.config.pageWidth = maxPageWidth;
    this.config.pageHeight = first.pageHeight;
    this.config.marginTop = first.marginTop;
    this.config.marginBottom = first.marginBottom;
    this.config.marginLeft = first.marginLeft;
    this.config.marginRight = first.marginRight;

    // Canvas and frame at maxPageWidth — no frame padding (sections handle margins)
    this.canvas.style.width = `${maxPageWidth}px`;
    this.canvas.style.minHeight = `${first.pageHeight}px`;

    this.pageFrame.style.width = `${maxPageWidth}px`;
    this.pageFrame.style.minHeight = `${first.pageHeight}px`;
    this.pageFrame.style.padding = '0';
  }

  /**
   * Recalculate pagination after every render.
   * Section-aware: handles different page heights per section,
   * forces page breaks at nextPage/evenPage/oddPage section boundaries,
   * and allows continuous flow for continuous breaks with matching dimensions.
   */
  updatePagination(): void {
    // 1. Clear any previously injected gap margins
    this.clearGapMargins();

    if (this.sectionConfigs.length > 1) {
      this.updatePaginationMultiSection();
    } else {
      this.updatePaginationSingleSection();
    }
  }

  /** Single-section pagination logic with section padding awareness. */
  private updatePaginationSingleSection(): void {
    const sc = this.sectionConfigs[0];
    const ch = sc?.contentHeight ?? this.contentHeight;
    const mb = sc?.marginBottom ?? this.config.marginBottom;
    const mt = sc?.marginTop ?? this.config.marginTop;
    const pw = sc?.pageWidth ?? this.config.pageWidth;

    // Sections now have padding (acting as page margins). Blocks inside
    // are offset by that padding. Compute where content starts.
    const sectionEl = this.canvas.querySelector('section') as HTMLElement | null;
    const paddingOffset = sectionEl
      ? sectionEl.offsetTop + sectionEl.clientTop + (sc?.marginTop ?? 0)
      : 0;

    // Effective content height: section scrollHeight minus its own padding
    const blockContentHeight = sectionEl
      ? sectionEl.scrollHeight - (sc?.marginTop ?? 0) - (sc?.marginBottom ?? 0)
      : this.canvas.scrollHeight;
    let pageCount = Math.max(1, Math.ceil(blockContentHeight / ch));

    if (pageCount <= 1) {
      this.pageCount = 1;
      this.breakBottomYPositions = [];
      this.breakPageBottomYPositions = [];
      this.pageSectionMap = [0];
      const ph = sc?.pageHeight ?? this.config.pageHeight;
      if (sectionEl && (!sc || sc.columnCount <= 1)) {
        sectionEl.style.minHeight = `${ph}px`;
      }
      this.renderOverlays([], ph);
      this.updateFrameHeight(ph);
      return;
    }

    for (let iteration = 0; iteration < 3; iteration++) {
      this.clearGapMargins();

      const children = this.getAllBlockChildren();
      let totalGap = 0;
      const breaks: BreakInfo[] = [];

      for (let page = 1; page < pageCount; page++) {
        const boundaryY = paddingOffset + ch * page + totalGap;
        const gapSize = mb + GAP_HEIGHT + mt;

        let target: HTMLElement | null = null;
        let tableRowSplit = false;
        let paraWasSplit = false;

        for (const child of children) {
          const childBottom = child.offsetTop + child.offsetHeight;
          if (childBottom > boundaryY) {
            if (child.tagName === 'TABLE') {
              const splitHeight = this.tryTableRowSplit(child, boundaryY, gapSize, ch);
              if (splitHeight !== null) {
                breaks.push({
                  y: boundaryY, marginBottom: mb, marginTop: mt, pageWidth: pw,
                  endingSectionIndex: 0, endingPageInSection: page - 1,
                  startingSectionIndex: 0, startingPageInSection: page,
                });
                totalGap += gapSize;
                tableRowSplit = true;
                break;
              }
            }
            // Try inline paragraph split for non-table blocks
            const isPara = child.tagName === 'P' || /^H[1-6]$/.test(child.tagName);
            if (isPara) {
              const paraSplitHeight = this.tryParaSplit(child, boundaryY, gapSize, ch);
              if (paraSplitHeight !== false) {
                breaks.push({
                  y: boundaryY, marginBottom: mb, marginTop: mt, pageWidth: pw,
                  endingSectionIndex: 0, endingPageInSection: page - 1,
                  startingSectionIndex: 0, startingPageInSection: page,
                });
                totalGap += gapSize;
                paraWasSplit = true;
                break;
              }
            }
            // Fallback: whole-block push
            if (child.offsetHeight > ch) continue;
            target = child;
            break;
          }
        }

        if (!tableRowSplit && !paraWasSplit && target) {
          breaks.push({
            y: boundaryY, marginBottom: mb, marginTop: mt, pageWidth: pw,
            endingSectionIndex: 0, endingPageInSection: page - 1,
            startingSectionIndex: 0, startingPageInSection: page,
          });
          if (!target.dataset.originalMarginTop) {
            target.dataset.originalMarginTop = target.style.marginTop || '';
          }
          const existing = parseFloat(target.style.marginTop) || 0;
          const extra = boundaryY - target.offsetTop + gapSize;
          target.style.marginTop = `${existing + extra}px`;
          // Compensate for CSS margin collapsing with previous sibling
          const expectedTop = boundaryY + gapSize;
          const actualTop = target.offsetTop;
          if (actualTop < expectedTop) {
            target.style.marginTop = `${existing + extra + (expectedTop - actualTop)}px`;
          }
          target.dataset.pageGap = 'true';
          totalGap += gapSize;
        }
      }

      // Recalculate page count from content height minus gaps
      const newBlockContentHeight = sectionEl
        ? sectionEl.scrollHeight - (sc?.marginTop ?? 0) - (sc?.marginBottom ?? 0)
        : this.canvas.scrollHeight;
      const rawContentHeight = newBlockContentHeight - totalGap;
      const recalcPages = Math.max(1, Math.ceil(rawContentHeight / ch));

      if (recalcPages === pageCount) {
        this.pageCount = pageCount;
        this.breakBottomYPositions = breaks.map((b) => b.y + b.marginBottom + GAP_HEIGHT + b.marginTop);
        this.breakPageBottomYPositions = breaks.map((b) => b.y + b.marginBottom);
        this.pageSectionMap = Array(this.pageCount).fill(0);
        const ph = sc?.pageHeight ?? this.config.pageHeight;
        const expectedHeight = ph * pageCount + GAP_HEIGHT * (pageCount - 1);
        if (sectionEl && (!sc || sc.columnCount <= 1)) {
          sectionEl.style.minHeight = `${expectedHeight}px`;
        }
        this.renderOverlays(breaks, expectedHeight);
        this.updateFrameHeight(expectedHeight);
        return;
      }

      pageCount = recalcPages;
    }

    this.renderCurrentState();
  }

  /**
   * Multi-section pagination.
   * Walks through section elements in DOM, applying per-section page heights.
   * Forces page breaks at section boundaries based on break type.
   */
  private updatePaginationMultiSection(): void {
    const sectionEls = Array.from(this.canvas.children).filter(
      (el) => (el as HTMLElement).tagName?.toLowerCase() === 'section',
    ) as HTMLElement[];

    if (sectionEls.length === 0) {
      this.updatePaginationSingleSection();
      return;
    }

    let prevBreakCount = -1;

    // Iterate up to 3 times (gap margins change layout)
    for (let iteration = 0; iteration < 3; iteration++) {
      this.clearGapMargins();

      const breaks: BreakInfo[] = [];
      let totalGap = 0;
      let currentPageContentUsed = 0; // how much of current page content area is used
      let prevSectionLastPage = 0; // last page number in the previous section

      for (let si = 0; si < sectionEls.length; si++) {
        const sectionEl = sectionEls[si];
        const sc = this.sectionConfigs[si] ?? this.sectionConfigs[0];
        const prevSc = si > 0 ? (this.sectionConfigs[si - 1] ?? this.sectionConfigs[0]) : sc;

        // Padding offset: blocks inside this section are offset by its padding
        const sectionPaddingOffset = sectionEl.clientTop + sc.marginTop;
        const sectionContentStart = sectionEl.offsetTop + sectionPaddingOffset;

        // Within-section page tracking (reset per section)
        let withinSectionPage = 0;
        let withinSectionGap = 0;

        // Handle section boundary (not the first section)
        if (si > 0) {
          const sameDimensions =
            sc.pageWidth === prevSc.pageWidth && sc.pageHeight === prevSc.pageHeight;
          const isContinuous = prevSc.breakType === 'continuous';

          if (isContinuous && sameDimensions) {
            // Continuous break with same page size: no forced page break
            // Content continues flowing on the same page
          } else {
            // Force a page break (nextPage, evenPage, oddPage, or continuous with different dims)
            const gapSize = prevSc.marginBottom + GAP_HEIGHT + sc.marginTop;

            // Find the first block child of this section
            const firstBlock = this.getFirstBlockChild(sectionEl);
            if (firstBlock) {
              // With base min-heights active, sectionEl.offsetTop is at a page
              // boundary. Position the overlay at the content area end of the
              // previous section's last page.
              const breakY = sectionEl.offsetTop - prevSc.marginBottom;
              breaks.push({
                y: breakY,
                marginBottom: prevSc.marginBottom,
                marginTop: sc.marginTop,
                pageWidth: Math.max(prevSc.pageWidth, sc.pageWidth),
                endingSectionIndex: si - 1,
                endingPageInSection: prevSectionLastPage,
                startingSectionIndex: si,
                startingPageInSection: 0,
              });

              // If firstBlock is an empty SB-holder paragraph (the WASM hasn't
              // been rebuilt with the absorption loop yet), it belongs visually
              // at the end of the previous section. Transfer the section-break
              // indicator to the previous section's last paragraph, hide the
              // holder, and apply the gap to the next real content block so
              // content is still pushed to the correct page.
              let gapTarget: HTMLElement | null = firstBlock;
              if (firstBlock.dataset.sectionBreak != null && firstBlock.textContent?.trim() === '') {
                // Transfer indicator to previous section's last block
                const prevBlocks = this.getBlockChildrenOf(sectionEls[si - 1]);
                const lastPrev = prevBlocks[prevBlocks.length - 1] ?? null;
                if (lastPrev && !lastPrev.dataset.sectionBreak) {
                  lastPrev.dataset.sectionBreak = prevSc.breakType ?? 'nextpage';
                  lastPrev.dataset.sbInjected = 'true';
                }
                // Hide the empty holder — it's now represented by lastPrev's ::after
                firstBlock.style.display = 'none';
                firstBlock.dataset.sbHolderHidden = 'true';
                // Apply gap to the next actual content block instead
                const allBlocks = this.getBlockChildrenOf(sectionEl);
                gapTarget = allBlocks.find(b => b !== firstBlock) ?? firstBlock;
              }

              // Inject gap margin on the gap target — just enough to push
              // content past the gap strip. No remainingOnPage needed because
              // base min-heights already fill the previous section's page.
              if (!gapTarget.dataset.originalMarginTop) {
                gapTarget.dataset.originalMarginTop = gapTarget.style.marginTop || '';
              }
              const existing = parseFloat(gapTarget.style.marginTop) || 0;
              const extra = breakY + gapSize - gapTarget.offsetTop;
              if (extra > 0) {
                gapTarget.style.marginTop = `${existing + extra}px`;
                gapTarget.dataset.pageGap = 'true';
                totalGap += gapSize;
              }
            }
            currentPageContentUsed = 0;
          }
        }

        // Paginate within this section
        const blocks = this.getBlockChildrenOf(sectionEl);
        for (const block of blocks) {
          if (block.dataset.pageGap) continue; // already processed as section boundary

          const blockBottom = block.offsetTop + block.offsetHeight;

          // Bug 1 fix: use withinSectionPage counter to correctly compute
          // the boundary for pages beyond the first within this section
          const pageContentBoundary =
            sectionContentStart +
            (sc.contentHeight - currentPageContentUsed) +
            sc.contentHeight * withinSectionPage +
            withinSectionGap;

          // Check if this block crosses the current page boundary
          if (blockBottom > pageContentBoundary) {
            const gapSize = sc.marginBottom + GAP_HEIGHT + sc.marginTop;

            // Don't push empty section-break holder paragraphs to the next page.
            // They must stay on the same page as the preceding content so the
            // indicator appears on the correct physical page.
            if (block.dataset.sectionBreak != null && block.textContent?.trim() === '') {
              continue;
            }

            if (block.tagName === 'TABLE') {
              const splitHeight = this.tryTableRowSplit(block, pageContentBoundary, gapSize, sc.contentHeight);
              if (splitHeight !== null) {
                breaks.push({
                  y: pageContentBoundary,
                  marginBottom: sc.marginBottom,
                  marginTop: sc.marginTop,
                  pageWidth: sc.pageWidth,
                  endingSectionIndex: si,
                  endingPageInSection: withinSectionPage,
                  startingSectionIndex: si,
                  startingPageInSection: withinSectionPage + 1,
                });
                totalGap += gapSize;
                withinSectionPage++;
                withinSectionGap += gapSize;
                currentPageContentUsed = 0;
                continue;
              }
            }

            // Try inline paragraph split for non-table blocks
            const isPara = block.tagName === 'P' || /^H[1-6]$/.test(block.tagName);
            if (isPara) {
              const paraSplitHeight = this.tryParaSplit(block, pageContentBoundary, gapSize, sc.contentHeight);
              if (paraSplitHeight !== false) {
                breaks.push({
                  y: pageContentBoundary,
                  marginBottom: sc.marginBottom,
                  marginTop: sc.marginTop,
                  pageWidth: sc.pageWidth,
                  endingSectionIndex: si,
                  endingPageInSection: withinSectionPage,
                  startingSectionIndex: si,
                  startingPageInSection: withinSectionPage + 1,
                });
                totalGap += gapSize;
                withinSectionPage++;
                withinSectionGap += gapSize;
                currentPageContentUsed = 0;
                continue;
              }
            }

            if (block.offsetHeight <= sc.contentHeight) {
              breaks.push({
                y: pageContentBoundary,
                marginBottom: sc.marginBottom,
                marginTop: sc.marginTop,
                pageWidth: sc.pageWidth,
                endingSectionIndex: si,
                endingPageInSection: withinSectionPage,
                startingSectionIndex: si,
                startingPageInSection: withinSectionPage + 1,
              });

              if (!block.dataset.originalMarginTop) {
                block.dataset.originalMarginTop = block.style.marginTop || '';
              }
              const existing = parseFloat(block.style.marginTop) || 0;
              const extra = pageContentBoundary - block.offsetTop + gapSize;
              block.style.marginTop = `${existing + extra}px`;
              // Compensate for CSS margin collapsing with previous sibling
              const expectedTop = pageContentBoundary + gapSize;
              const actualTop = block.offsetTop;
              if (actualTop < expectedTop) {
                block.style.marginTop = `${existing + extra + (expectedTop - actualTop)}px`;
              }
              block.dataset.pageGap = 'true';
              totalGap += gapSize;
              withinSectionPage++;
              withinSectionGap += gapSize;
              currentPageContentUsed = 0;
            }
          }
        }

        // Bug 3 fix: compute content used relative to section start minus
        // within-section gaps, then modulo by contentHeight
        if (blocks.length > 0) {
          const lastBlock = blocks[blocks.length - 1];
          const sectionBottom = lastBlock.offsetTop + lastBlock.offsetHeight;
          const effectiveContentHeight = sectionBottom - sectionContentStart - withinSectionGap;
          currentPageContentUsed = effectiveContentHeight % sc.contentHeight || sc.contentHeight;
        }

        prevSectionLastPage = withinSectionPage;
      }

      // Bug 4 fix: check stability by comparing break count across iterations
      if (breaks.length === prevBreakCount || breaks.length === 0) {
        this.pageCount = breaks.length + 1;

        // Save section positions before min-heights are applied
        const preOffsets = sectionEls.map((el) => el.offsetTop);
        this.applySectionMinHeights(sectionEls, breaks);

        // Recompute overlay positions: applySectionMinHeights may shift
        // section elements (e.g. a multi-page section grows to fill
        // complete pages), so break Y values must be updated.
        const postOffsets = sectionEls.map((el) => el.offsetTop);
        for (const brk of breaks) {
          if (brk.endingSectionIndex !== brk.startingSectionIndex) {
            // Section break: recompute at content area end of ending section
            const prevSc = this.sectionConfigs[brk.endingSectionIndex];
            brk.y = sectionEls[brk.startingSectionIndex].offsetTop - prevSc.marginBottom;
          } else {
            // Within-section break: shift by the section's offset change
            const si = brk.startingSectionIndex;
            brk.y += postOffsets[si] - preOffsets[si];
          }
        }

        this.breakBottomYPositions = breaks.map((b) => b.y + b.marginBottom + GAP_HEIGHT + b.marginTop);
        this.breakPageBottomYPositions = breaks.map((b) => b.y + b.marginBottom);
        this.pageSectionMap = [0, ...breaks.map((b) => b.startingSectionIndex)];
        // Compute expected total height from the last section's rendered position.
        // Reading offsetTop + offsetHeight forces a synchronous reflow after
        // applySectionMinHeights, giving the accurate value without relying on
        // canvas.scrollHeight which may overshoot due to gap margin rounding.
        const lastSectionEl = sectionEls[sectionEls.length - 1];
        const totalHeight = lastSectionEl.offsetTop + lastSectionEl.offsetHeight;
        this.renderOverlays(breaks, totalHeight);
        this.updateFrameHeight(this.canvas.scrollHeight);
        return;
      }

      prevBreakCount = breaks.length;
    }

    this.renderCurrentState();
  }

  /** Get all block-level children across all sections (or direct canvas children). */
  private getAllBlockChildren(): HTMLElement[] {
    const blocks: HTMLElement[] = [];
    for (const child of this.canvas.children) {
      const el = child as HTMLElement;
      const tag = el.tagName?.toLowerCase();
      if (tag === 'section') {
        blocks.push(...this.getBlockChildrenOf(el));
      } else if (tag === 'p' || tag?.match(/^h[1-6]$/) || tag === 'table') {
        blocks.push(el);
      }
    }
    return blocks;
  }

  /** Get block children of a section element. */
  private getBlockChildrenOf(sectionEl: HTMLElement): HTMLElement[] {
    const blocks: HTMLElement[] = [];
    for (const child of sectionEl.children) {
      const tag = (child as HTMLElement).tagName?.toLowerCase();
      if (tag === 'p' || tag?.match(/^h[1-6]$/) || tag === 'table') {
        blocks.push(child as HTMLElement);
      }
    }
    return blocks;
  }

  /** Get first block child of a section element. */
  private getFirstBlockChild(sectionEl: HTMLElement): HTMLElement | null {
    for (const child of sectionEl.children) {
      const tag = (child as HTMLElement).tagName?.toLowerCase();
      if (tag === 'p' || tag?.match(/^h[1-6]$/) || tag === 'table') {
        return child as HTMLElement;
      }
    }
    return null;
  }

  /**
   * Attempt to split a table at a row boundary at `boundaryY`.
   * Inserts a gap `<tr>` row before the first row that crosses the boundary.
   * Returns the gap row height if a split was performed; null if the caller
   * should fall back to treating the table as an atomic block.
   */
  private tryTableRowSplit(
    table: HTMLElement,
    boundaryY: number,
    gapSize: number,
    contentHeight: number,
  ): number | null {
    const rows = Array.from(
      table.querySelectorAll<HTMLElement>(':scope > tbody > tr, :scope > tr'),
    );
    const tableTop = table.offsetTop;

    for (let i = 0; i < rows.length; i++) {
      const row = rows[i];
      const rowTop = tableTop + row.offsetTop;
      const rowBottom = rowTop + row.offsetHeight;

      if (rowBottom > boundaryY) {
        // This row crosses the boundary
        if (i === 0) {
          // First row crosses — table starts past boundary; fall back to block treatment
          return null;
        }
        if (row.offsetHeight > contentHeight) {
          // Row taller than a full page — can't split here
          return null;
        }
        // Insert a gap row before this row
        const gapRowHeight = boundaryY - rowTop + gapSize;
        const colCount = Math.max(
          1,
          ...Array.from(
            table.querySelectorAll<HTMLElement>(':scope > tbody > tr, :scope > tr'),
          )
            .slice(0, i)
            .map((r) => r.querySelectorAll('td, th').length),
        );
        const gapRow = document.createElement('tr');
        gapRow.dataset.pageGapRow = 'true';
        gapRow.contentEditable = 'false';
        gapRow.style.height = `${gapRowHeight}px`;
        const td = document.createElement('td');
        td.setAttribute('colspan', String(colCount));
        td.style.cssText = 'padding:0;border:none;height:inherit';
        gapRow.appendChild(td);
        row.parentElement!.insertBefore(gapRow, row);
        return gapRowHeight;
      }
    }

    return null; // table fits on this page entirely
  }

  /**
   * Attempt to split a paragraph inline at `boundaryY` by inserting a
   * `<span data-para-page-gap>` gap span at the character boundary.
   * Returns the actual span height used if a gap span was inserted; false if
   * the caller should fall back to pushing the whole paragraph.
   */
  private tryParaSplit(
    para: HTMLElement,
    boundaryY: number,
    gapSize: number,
    contentHeight: number,
  ): number | false {
    // Guards — fall back to whole-para push
    if (para.dataset.keepLines === 'true') return false;
    if (para.dataset.pageBreakBefore === 'true') return false;
    if (para.offsetHeight === 0) return false;
    if (para.offsetHeight > contentHeight) return false;

    // Convert canvas-relative boundaryY to viewport Y for Range comparisons
    const canvasRect = this.canvas.getBoundingClientRect();
    const viewportBoundaryY = canvasRect.top + boundaryY;

    // Walk text nodes to find the split point
    const walker = document.createTreeWalker(
      para,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: (node: Node) => {
          // Skip text inside existing gap spans
          let ancestor: Node | null = node.parentNode;
          while (ancestor && ancestor !== para) {
            if (
              ancestor instanceof HTMLElement &&
              ancestor.dataset.paraPageGap === 'true'
            ) {
              return NodeFilter.FILTER_REJECT;
            }
            ancestor = ancestor.parentNode;
          }
          return NodeFilter.FILTER_ACCEPT;
        },
      },
    );

    const range = document.createRange();
    let splitNode: Node | null = null;
    let splitOffset = 0;

    let textNode: Text | null;
    while ((textNode = walker.nextNode() as Text | null) !== null) {
      const nodeLength = textNode.length;
      if (nodeLength === 0) continue;

      // Check if the entire node is above the boundary
      range.setStart(textNode, 0);
      range.setEnd(textNode, nodeLength);
      const fullRect = range.getBoundingClientRect();
      if (fullRect.bottom <= viewportBoundaryY) {
        // Entire node is above — keep going
        continue;
      }
      if (fullRect.top >= viewportBoundaryY) {
        // Entire node is below — split before offset 0
        splitNode = textNode;
        splitOffset = 0;
        break;
      }

      // Node straddles — binary search for the character boundary
      let lo = 0;
      let hi = nodeLength;
      while (lo < hi) {
        const mid = (lo + hi) >> 1;
        range.setStart(textNode, 0);
        range.setEnd(textNode, mid + 1);
        const midRect = range.getBoundingClientRect();
        if (midRect.bottom <= viewportBoundaryY) {
          lo = mid + 1;
        } else {
          hi = mid;
        }
      }

      // lo is the offset of the first character crossing the boundary
      splitOffset = lo;

      // Surrogate pair guard
      if (splitOffset > 0 && splitOffset < nodeLength) {
        const code = textNode.data.charCodeAt(splitOffset - 1);
        if (code >= 0xd800 && code <= 0xdbff) {
          splitOffset++;
        }
      }

      splitNode = textNode;
      break;
    }

    if (splitNode === null) return false;

    // Measure the canvas-relative top of the split line before DOM mutation.
    // The split line's top may be above boundaryY (the line straddles the boundary),
    // so we compensate — mirrors the gapRowHeight formula in tryTableRowSplit.
    const splitCharLen = (splitNode as Text).length;
    range.setStart(splitNode, splitOffset);
    range.setEnd(splitNode, Math.min(splitOffset + 1, splitCharLen));
    const splitCharRect = range.getBoundingClientRect();
    const splitLineTopCanvas = splitCharRect.top - canvasRect.top;
    const dynamicGapHeight = Math.max(0, boundaryY - splitLineTopCanvas + gapSize);

    // Insert the gap span at the split point
    range.setStart(splitNode, splitOffset);
    range.collapse(true);

    const gapSpan = document.createElement('span');
    gapSpan.dataset.paraPageGap = 'true';
    gapSpan.contentEditable = 'false';
    gapSpan.setAttribute('aria-hidden', 'true');
    gapSpan.style.cssText = `display:block;height:${dynamicGapHeight}px;font-size:0;line-height:0;pointer-events:none;user-select:none;`;

    range.insertNode(gapSpan);

    // Calibrate: force synchronous reflow, then check where the first text after
    // the gap span actually landed. Chrome inserts invisible cursor infrastructure
    // (struts, anonymous blocks) between a contentEditable=false block span and
    // the following text, causing the text to land lower than expected.
    void (gapSpan.offsetHeight); // force layout

    const walker2 = document.createTreeWalker(para, NodeFilter.SHOW_TEXT);
    let firstTextAfter: Text | null = null;
    while (true) {
      const node = walker2.nextNode() as Text | null;
      if (!node) break;
      // Only consider text that comes after the gap span in document order
      if (!(gapSpan.compareDocumentPosition(node) & Node.DOCUMENT_POSITION_FOLLOWING)) continue;
      // Skip text inside another gap span
      let anc: Node | null = node.parentNode;
      let inGap = false;
      while (anc && anc !== para) {
        if (anc instanceof HTMLElement && anc.dataset.paraPageGap === 'true') { inGap = true; break; }
        anc = anc.parentNode;
      }
      if (inGap) continue;
      if (node.length > 0) { firstTextAfter = node; break; }
    }

    if (firstTextAfter) {
      const afterRange = document.createRange();
      afterRange.setStart(firstTextAfter, 0);
      afterRange.setEnd(firstTextAfter, Math.min(1, firstTextAfter.length));
      const afterRect = afterRange.getBoundingClientRect();
      const actualTopCanvas = afterRect.top - canvasRect.top;
      const expectedTopCanvas = boundaryY + gapSize;
      const correction = expectedTopCanvas - actualTopCanvas;
      if (Math.abs(correction) > 0.5) {
        const correctedHeight = Math.max(0, dynamicGapHeight + correction);
        gapSpan.style.height = `${correctedHeight}px`;
        return correctedHeight;
      }
    }

    return dynamicGapHeight;
  }

  /** Remove gap margins from all children that had them injected. */
  private clearGapMargins(): void {
    const gapped = this.canvas.querySelectorAll<HTMLElement>('[data-page-gap]');
    for (const el of gapped) {
      el.style.marginTop = el.dataset.originalMarginTop || '';
      delete el.dataset.pageGap;
      delete el.dataset.originalMarginTop;
    }
    // Remove any gap rows injected into tables by tryTableRowSplit
    const gapRows = this.canvas.querySelectorAll<HTMLElement>('tr[data-page-gap-row]');
    for (const row of gapRows) {
      row.remove();
    }
    // Remove any gap spans injected into paragraphs by tryParaSplit
    const paraGapSpans = this.canvas.querySelectorAll<HTMLElement>('span[data-para-page-gap]');
    for (const span of paraGapSpans) {
      span.remove();
    }
    // Undo dynamically hidden SB-holder paragraphs (Fix C)
    const hiddenHolders = this.canvas.querySelectorAll<HTMLElement>('[data-sb-holder-hidden]');
    for (const el of hiddenHolders) {
      el.style.display = '';
      delete el.dataset.sbHolderHidden;
    }
    // Undo dynamically injected section-break markers (Fix C)
    const injected = this.canvas.querySelectorAll<HTMLElement>('[data-sb-injected]');
    for (const el of injected) {
      delete el.dataset.sectionBreak;
      delete el.dataset.sbInjected;
    }
    // Restore section minHeights to single-page height so position calculations
    // account for sections filling at least one full page (prevents double-counting
    // remainingOnPage in section break gap margins)
    const sectionEls = this.canvas.querySelectorAll<HTMLElement>('section');
    for (let i = 0; i < sectionEls.length; i++) {
      const sc = this.sectionConfigs[i] ?? this.sectionConfigs[0];
      if (sc && sc.columnCount <= 1) {
        sectionEls[i].style.minHeight = `${sc.pageHeight}px`;
      } else {
        sectionEls[i].style.minHeight = '';
      }
    }
  }

  /**
   * Set minHeight on each section element to ensure all pages render at full height.
   * Counts pages per section from break info (within-section breaks add a page).
   */
  private applySectionMinHeights(sectionEls: HTMLElement[], breaks: BreakInfo[]): void {
    const sectionPageCounts = new Array(sectionEls.length).fill(1);

    for (const brk of breaks) {
      if (brk.endingSectionIndex === brk.startingSectionIndex) {
        sectionPageCounts[brk.startingSectionIndex]++;
      }
    }

    for (let si = 0; si < sectionEls.length; si++) {
      const sc = this.sectionConfigs[si] ?? this.sectionConfigs[0];
      if (sc.columnCount > 1) {
        sectionEls[si].style.minHeight = '';
        continue;
      }
      const pages = sectionPageCounts[si];
      const minH = sc.pageHeight * pages + GAP_HEIGHT * (pages - 1);
      sectionEls[si].style.minHeight = `${minH}px`;
    }
  }

  /** Remove all page-break overlay elements. */
  private clearOverlays(): void {
    this.overlay.innerHTML = '';
  }

  /**
   * Resolve which header RenderNode[] to use for a given page.
   * Rules: first page of section + titlePage + "first" key → "first";
   *        even page number + "even" key → "even"; otherwise → "default".
   */
  private resolveHeader(sectionIndex: number, pageInSection: number): RenderNode[] | null {
    const sc = this.sectionConfigs[sectionIndex];
    if (!sc?.headers) return null;

    if (pageInSection === 0 && sc.titlePage && sc.headers['first']) {
      return sc.headers['first'];
    }
    // pageInSection is 0-based; page 1 (0-based) is the 2nd page = even in 1-based
    const pageNumber1Based = pageInSection + 1;
    if (pageNumber1Based % 2 === 0 && sc.headers['even']) {
      return sc.headers['even'];
    }
    return sc.headers['default'] ?? sc.headers['first'] ?? null;
  }

  /** Same as resolveHeader but for footers. */
  private resolveFooter(sectionIndex: number, pageInSection: number): RenderNode[] | null {
    const sc = this.sectionConfigs[sectionIndex];
    if (!sc?.footers) return null;

    if (pageInSection === 0 && sc.titlePage && sc.footers['first']) {
      return sc.footers['first'];
    }
    const pageNumber1Based = pageInSection + 1;
    if (pageNumber1Based % 2 === 0 && sc.footers['even']) {
      return sc.footers['even'];
    }
    return sc.footers['default'] ?? sc.footers['first'] ?? null;
  }

  /**
   * Render header or footer content inside a margin zone element.
   * Creates a positioned div, renders RenderNode children as read-only DOM,
   * then substitutes dynamic field values (PAGE, NUMPAGES) with correct numbers.
   */
  private renderHeaderFooterInZone(
    container: HTMLElement,
    nodes: RenderNode[],
    type: 'header' | 'footer',
    sc: SectionConfig,
    pageNumber: number,
    totalPages: number,
  ): void {
    const div = document.createElement('div');
    div.className = type === 'header' ? 'page-break-header-content' : 'page-break-footer-content';
    div.style.width = `${sc.contentWidth}px`;
    div.style.marginLeft = `${sc.marginLeft}px`;
    div.style.marginRight = `${sc.marginRight}px`;
    div.style.height = '100%';

    if (type === 'header') {
      div.style.paddingTop = `${sc.headerDistance}px`;
    } else {
      // Footer: w:footerDistance = distance from page bottom to footer content top.
      // The footer zone height = marginBottom. Content should start at (marginBottom - footerDistance) from zone top.
      div.style.paddingTop = `${Math.max(0, sc.marginBottom - sc.footerDistance)}px`;
    }

    for (const node of nodes) {
      div.appendChild(createReadOnlyDomNode(node));
    }

    // Substitute dynamic field values with correct page-specific numbers
    for (const el of div.querySelectorAll<HTMLElement>('[data-field="PAGE"]')) {
      el.textContent = String(pageNumber);
    }
    for (const el of div.querySelectorAll<HTMLElement>('[data-field="NUMPAGES"]')) {
      el.textContent = String(totalPages);
    }

    container.appendChild(div);
  }

  /** Render page-break strip overlays at computed positions. */
  private renderOverlays(breaks: BreakInfo[], totalHeight?: number): void {
    this.clearOverlays();

    // totalPages = number of page-break gaps + 1 (correct for 0 breaks too)
    const totalPages = breaks.length + 1;

    // Render first-page header overlay (before first break, at top of first section)
    // This is always page 1.
    if (this.sectionConfigs.length > 0) {
      const firstSc = this.sectionConfigs[0];
      const headerNodes = this.resolveHeader(0, 0);
      if (headerNodes) {
        const headerOverlay = document.createElement('div');
        headerOverlay.className = 'page-header-overlay';
        headerOverlay.style.top = '0px';
        headerOverlay.style.height = `${firstSc.marginTop}px`;
        headerOverlay.style.width = `${firstSc.pageWidth}px`;
        headerOverlay.style.left = '50%';
        headerOverlay.style.transform = 'translateX(-50%)';
        this.renderHeaderFooterInZone(headerOverlay, headerNodes, 'header', firstSc, 1, totalPages);
        this.overlay.appendChild(headerOverlay);
      }
    }

    for (let i = 0; i < breaks.length; i++) {
      const brk = breaks[i];
      const gapSize = brk.marginBottom + GAP_HEIGHT + brk.marginTop;

      // Position is already in frame coords (frame has no padding)
      const overlayY = brk.y;

      const line = document.createElement('div');
      line.className = 'page-break-line';
      line.style.top = `${overlayY}px`;
      line.style.height = `${gapSize}px`;
      line.style.width = `${brk.pageWidth}px`;
      line.style.left = '50%';
      line.style.transform = 'translateX(-50%)';

      // Bottom margin zone of page (i+1) — the page ending at this break
      const endingSc = this.sectionConfigs[brk.endingSectionIndex];
      const marginBottom = document.createElement('div');
      marginBottom.className = 'page-break-margin-bottom';
      marginBottom.style.height = `${brk.marginBottom}px`;
      if (endingSc) {
        marginBottom.style.width = `${endingSc.pageWidth}px`;
        marginBottom.style.margin = '0 auto';
      }
      const footerNodes = this.resolveFooter(brk.endingSectionIndex, brk.endingPageInSection);
      if (footerNodes && endingSc) {
        this.renderHeaderFooterInZone(marginBottom, footerNodes, 'footer', endingSc, i + 1, totalPages);
      }

      line.appendChild(marginBottom);

      // Gray gap between pages
      const gap = document.createElement('div');
      gap.className = 'page-break-gap';
      gap.style.height = `${GAP_HEIGHT}px`;
      line.appendChild(gap);

      // Top margin zone of page (i+2) — the page starting after this break
      const startingSc = this.sectionConfigs[brk.startingSectionIndex];
      const marginTop = document.createElement('div');
      marginTop.className = 'page-break-margin-top';
      marginTop.style.height = `${brk.marginTop}px`;
      if (startingSc) {
        marginTop.style.width = `${startingSc.pageWidth}px`;
        marginTop.style.margin = '0 auto';
      }
      const headerNodes = this.resolveHeader(brk.startingSectionIndex, brk.startingPageInSection);
      if (headerNodes && startingSc) {
        this.renderHeaderFooterInZone(marginTop, headerNodes, 'header', startingSc, i + 2, totalPages);
      }

      line.appendChild(marginTop);

      this.overlay.appendChild(line);
    }

    // Render last-page footer overlay (after last break, at bottom of last page).
    // Last page number = totalPages.
    if (this.sectionConfigs.length > 0) {
      const lastSi = this.sectionConfigs.length - 1;
      const lastSc = this.sectionConfigs[lastSi];
      const lastPageInSection = breaks.length > 0
        ? breaks[breaks.length - 1].startingPageInSection
        : 0;
      // Position at the bottom of the last page anchored to expected height,
      // not canvas.scrollHeight which may overshoot due to gap margin rounding.
      const footerY = (totalHeight ?? this.canvas.scrollHeight) - lastSc.marginBottom;
      const footerOverlay = document.createElement('div');
      footerOverlay.className = 'page-footer-overlay';
      footerOverlay.style.top = `${footerY}px`;
      footerOverlay.style.height = `${lastSc.marginBottom}px`;
      footerOverlay.style.width = `${lastSc.pageWidth}px`;
      footerOverlay.style.left = '50%';
      footerOverlay.style.transform = 'translateX(-50%)';
      const footerNodes = this.resolveFooter(lastSi, lastPageInSection);
      if (footerNodes) {
        this.renderHeaderFooterInZone(footerOverlay, footerNodes, 'footer', lastSc, totalPages, totalPages);
      }
      this.overlay.appendChild(footerOverlay);
    }
  }

  /** Update the page frame min-height to encompass all content. */
  private updateFrameHeight(expectedTotalHeight: number): void {
    const canvasHeight = this.canvas.scrollHeight;
    const minH = Math.max(this.config.pageHeight, expectedTotalHeight, canvasHeight);
    this.pageFrame.style.minHeight = `${minH}px`;
  }

  /** Fallback: render overlays from current DOM state after max iterations. */
  private renderCurrentState(): void {
    const children = this.getAllBlockChildren();
    const breaks: BreakInfo[] = [];

    for (const child of children) {
      if (child.dataset.pageGap) {
        // Determine which section this block belongs to
        const sectionEl = child.closest('section') as HTMLElement | null;
        const si = sectionEl ? parseInt(sectionEl.dataset.sectionIndex ?? '0') : 0;
        const sc = this.sectionConfigs[si] ?? this.sectionConfigs[0];
        const mb = sc?.marginBottom ?? this.config.marginBottom;
        const mt = sc?.marginTop ?? this.config.marginTop;
        const pw = sc?.pageWidth ?? this.config.pageWidth;

        const gapSize = mb + GAP_HEIGHT + mt;
        const actualMargin = parseFloat(child.style.marginTop) || gapSize;
        breaks.push({
          y: child.offsetTop - actualMargin,
          marginBottom: mb,
          marginTop: mt,
          pageWidth: pw,
          endingSectionIndex: si,
          endingPageInSection: 0,
          startingSectionIndex: si,
          startingPageInSection: 0,
        });
      }
    }

    // Apply section minHeights to ensure full-page rendering
    const sectionEls = Array.from(this.canvas.children).filter(
      (el) => (el as HTMLElement).tagName?.toLowerCase() === 'section',
    ) as HTMLElement[];
    if (sectionEls.length > 0) {
      this.applySectionMinHeights(sectionEls, breaks);
    }

    this.renderOverlays(breaks);
    this.updateFrameHeight(this.canvas.scrollHeight);
  }

  /**
   * Update page dimensions from document properties (twips from C# model).
   * Backwards-compatible wrapper for single-section documents.
   */
  updateFromDocProps(props: {
    pageWidth?: number;
    pageHeight?: number;
    marginTop?: number;
    marginBottom?: number;
    marginLeft?: number;
    marginRight?: number;
  }): void {
    if (props.pageWidth) this.config.pageWidth = twipsToPx(props.pageWidth);
    if (props.pageHeight) this.config.pageHeight = twipsToPx(props.pageHeight);
    if (props.marginTop) this.config.marginTop = twipsToPx(props.marginTop);
    if (props.marginBottom) this.config.marginBottom = twipsToPx(props.marginBottom);
    if (props.marginLeft) this.config.marginLeft = twipsToPx(props.marginLeft);
    if (props.marginRight) this.config.marginRight = twipsToPx(props.marginRight);

    // Canvas and frame at full page width — no frame padding
    this.canvas.style.width = `${this.config.pageWidth}px`;
    this.canvas.style.minHeight = `${this.config.pageHeight}px`;

    this.pageFrame.style.width = `${this.config.pageWidth}px`;
    this.pageFrame.style.minHeight = `${this.config.pageHeight}px`;
    this.pageFrame.style.padding = '0';
  }

  /**
   * Returns an array of gap zones in canvas-relative coordinates.
   * Each zone is a {top, bottom} range where the caret should not rest.
   */
  getGapZones(): Array<{ top: number; bottom: number }> {
    const zones: Array<{ top: number; bottom: number }> = [];
    const gapped = this.canvas.querySelectorAll<HTMLElement>('[data-page-gap]');

    for (const el of gapped) {
      const gapMargin = parseFloat(el.style.marginTop) || 0;
      if (gapMargin <= 0) continue;

      const zoneBottom = el.offsetTop;
      const zoneTop = zoneBottom - gapMargin;
      zones.push({ top: zoneTop, bottom: zoneBottom });
    }

    // Include gap zones from para gap spans inserted by tryParaSplit
    const paraGaps = this.canvas.querySelectorAll<HTMLElement>('span[data-para-page-gap]');
    for (const span of paraGaps) {
      zones.push({ top: span.offsetTop, bottom: span.offsetTop + span.offsetHeight });
    }

    return zones;
  }

  /**
   * If the collapsed caret sits inside a page-break gap zone, snap it
   * to the nearest visible position above or below the gap.
   */
  adjustCursorForPageBreaks(): void {
    if (this.adjusting) return;

    const sel = window.getSelection();
    if (!sel || !sel.isCollapsed || sel.rangeCount === 0) return;

    const range = sel.getRangeAt(0);
    if (!this.canvas.contains(range.startContainer)) return;

    const rect = range.getBoundingClientRect();
    if (!rect || (rect.top === 0 && rect.bottom === 0)) return;

    const canvasRect = this.canvas.getBoundingClientRect();
    const caretY = rect.top - canvasRect.top + this.canvas.scrollTop;

    const zones = this.getGapZones();
    for (const zone of zones) {
      if (caretY >= zone.top && caretY < zone.bottom) {
        const distToAbove = caretY - zone.top;
        const distToBelow = zone.bottom - caretY;
        const direction = distToAbove <= distToBelow ? 'up' : 'down';

        this.adjusting = true;
        try {
          this.moveCursorToVisiblePosition(
            direction === 'up' ? zone.top - 1 : zone.bottom + 1,
            direction,
          );
        } finally {
          this.adjusting = false;
        }
        return;
      }
    }
  }

  /**
   * Determine the 1-based page number currently visible at the top of the scroll area.
   */
  getCurrentPage(scrollArea: HTMLElement): number {
    const canvasRect = this.canvas.getBoundingClientRect();
    const scrollAreaRect = scrollArea.getBoundingClientRect();
    // Canvas-relative Y of the strict viewport top
    const viewportTop = scrollAreaRect.top - canvasRect.top;

    for (let i = 0; i < this.breakPageBottomYPositions.length; i++) {
      if (viewportTop < this.breakPageBottomYPositions[i]) {
        return i + 1;
      }
    }
    return this.pageCount;
  }

  /**
   * Returns the 1-based page number whose section should drive ruler dimensions.
   * Changes only when the full gap strip (bottom margin + gap + top margin) has
   * completely scrolled past the top of the viewport — i.e., the previous page
   * is no longer visible at all.
   */
  getPageForRuler(scrollArea: HTMLElement): number {
    const canvasRect = this.canvas.getBoundingClientRect();
    const scrollAreaRect = scrollArea.getBoundingClientRect();
    // Canvas-relative Y at top of viewport
    const viewportTop = scrollAreaRect.top - canvasRect.top;

    for (let i = 0; i < this.breakBottomYPositions.length; i++) {
      if (viewportTop < this.breakBottomYPositions[i]) {
        return i + 1;
      }
    }
    return this.pageCount;
  }

  /**
   * Returns the 1-based page where the text caret is currently located.
   * Uses DOM selection position, not scroll position.
   */
  getPageForCursor(): number {
    const sel = window.getSelection();
    if (!sel || sel.rangeCount === 0) return 1;
    const range = sel.getRangeAt(0);
    if (!this.canvas.contains(range.startContainer)) return 1;
    const rect = range.getBoundingClientRect();
    if (!rect || (rect.top === 0 && rect.bottom === 0)) return 1;
    const canvasRect = this.canvas.getBoundingClientRect();
    const caretY = rect.top - canvasRect.top;
    for (let i = 0; i < this.breakPageBottomYPositions.length; i++) {
      if (caretY < this.breakPageBottomYPositions[i]) return i + 1;
    }
    return this.pageCount;
  }

  /** Returns the 0-based section index where the text caret is currently located. */
  getSectionForCursor(): number {
    const page = this.getPageForCursor();
    return this.pageSectionMap[page - 1] ?? 0;
  }

  /**
   * Returns the ruler dimensions for the section containing the given 1-based page number.
   * All values are already in px (converted by updateFromSections).
   */
  getPageRulerDimensions(page: number): {
    pageWidth: number; pageHeight: number;
    marginLeft: number; marginRight: number;
    marginTop: number; marginBottom: number;
  } | null {
    if (this.sectionConfigs.length === 0) return null;
    const idx = this.pageSectionMap[page - 1] ?? 0;
    const sc = this.sectionConfigs[idx] ?? this.sectionConfigs[0];
    if (!sc) return null;
    return {
      pageWidth: sc.pageWidth, pageHeight: sc.pageHeight,
      marginLeft: sc.marginLeft, marginRight: sc.marginRight,
      marginTop: sc.marginTop, marginBottom: sc.marginBottom,
    };
  }

  /**
   * Returns the scroll-y of the given 1-based page's top edge (start of its
   * white paper area, just after the gray inter-page gap).
   * Uses the real break positions from the last pagination run — correct even
   * when different sections have different page heights.
   */
  getPageTopScrollY(page: number): number {
    const TOP_PAD = 20; // pages-wrapper padding-top
    if (page <= 1) return TOP_PAD;
    const idx = page - 2; // 0-based index into breakPageBottomYPositions
    if (idx < this.breakPageBottomYPositions.length) {
      // breakPageBottomYPositions[i] = canvas-y where gray gap starts (end of marginBottom)
      // + GAP_HEIGHT → canvas-y where next page's white top begins
      // + TOP_PAD   → converts canvas-y to scroll-y
      return this.breakPageBottomYPositions[idx] + GAP_HEIGHT + TOP_PAD;
    }
    return TOP_PAD;
  }

  /**
   * Total scrollable height of the editor (canvas content + pages-wrapper padding).
   * Use this for sizing the vertical ruler SVG accurately.
   */
  getTotalScrollHeight(): number {
    return this.canvas.scrollHeight + 40; // 20 top + 20 bottom padding
  }

  /**
   * Move the caret to a visible position at the given canvas-relative Y coordinate.
   */
  private moveCursorToVisiblePosition(
    targetCanvasY: number,
    direction: 'up' | 'down',
  ): void {
    const canvasRect = this.canvas.getBoundingClientRect();
    const screenY = targetCanvasY - this.canvas.scrollTop + canvasRect.top;
    const screenX = canvasRect.left + canvasRect.width / 2;

    const step = direction === 'up' ? -2 : 2;
    const limit = direction === 'up' ? canvasRect.top : canvasRect.bottom;

    for (
      let y = screenY;
      direction === 'up' ? y >= limit : y <= limit;
      y += step
    ) {
      if (typeof document.caretRangeFromPoint === 'function') {
        const probe = document.caretRangeFromPoint(screenX, y);
        if (probe && this.canvas.contains(probe.startContainer)) {
          const sel = window.getSelection();
          if (sel) {
            sel.removeAllRanges();
            sel.addRange(probe);
          }
          return;
        }
      }
    }
  }
}
