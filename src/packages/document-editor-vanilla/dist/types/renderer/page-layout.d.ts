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
import type { SectionInfo } from '../bridge/types';
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
    rawPageWidth: number;
    rawPageHeight: number;
    rawMarginTop: number;
    rawMarginBottom: number;
    rawMarginLeft: number;
    rawMarginRight: number;
    rawHeaderDistance: number;
    rawFooterDistance: number;
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
export declare class PageLayout {
    private container;
    private pagesWrapper;
    private pageFrame;
    private canvas;
    private overlay;
    private config;
    private sectionConfigs;
    private rawSections;
    private adjusting;
    private breakBottomYPositions;
    private breakPageBottomYPositions;
    pageSectionMap: number[];
    /** Total number of pages after the last pagination. */
    pageCount: number;
    constructor(container: HTMLElement, config?: PageLayoutConfig);
    get contentWidth(): number;
    get contentHeight(): number;
    getCanvas(): HTMLElement;
    getDebugSectionData(): DebugSectionSnapshot[];
    /**
     * Update page dimensions from sections metadata.
     * Frame becomes a transparent container at the max page width.
     * Sections handle their own paper styling and margins via inline padding.
     */
    updateFromSections(sections: SectionInfo[]): void;
    /**
     * Recalculate pagination after every render.
     * Section-aware: handles different page heights per section,
     * forces page breaks at nextPage/evenPage/oddPage section boundaries,
     * and allows continuous flow for continuous breaks with matching dimensions.
     */
    updatePagination(): void;
    /** Single-section pagination logic with section padding awareness. */
    private updatePaginationSingleSection;
    /**
     * Multi-section pagination.
     * Walks through section elements in DOM, applying per-section page heights.
     * Forces page breaks at section boundaries based on break type.
     */
    private updatePaginationMultiSection;
    /** Get all block-level children across all sections (or direct canvas children). */
    private getAllBlockChildren;
    /** Get block children of a section element. */
    private getBlockChildrenOf;
    /** Get first block child of a section element. */
    private getFirstBlockChild;
    /**
     * Attempt to split a table at a row boundary at `boundaryY`.
     * Inserts a gap `<tr>` row before the first row that crosses the boundary.
     * Returns the gap row height if a split was performed; null if the caller
     * should fall back to treating the table as an atomic block.
     */
    private tryTableRowSplit;
    /**
     * Attempt to split a paragraph inline at `boundaryY` by inserting a
     * `<span data-para-page-gap>` gap span at the character boundary.
     * Returns the actual span height used if a gap span was inserted; false if
     * the caller should fall back to pushing the whole paragraph.
     */
    private tryParaSplit;
    /** Remove gap margins from all children that had them injected. */
    private clearGapMargins;
    /**
     * Set minHeight on each section element to ensure all pages render at full height.
     * Counts pages per section from break info (within-section breaks add a page).
     */
    private applySectionMinHeights;
    /** Remove all page-break overlay elements. */
    private clearOverlays;
    /**
     * Resolve which header RenderNode[] to use for a given page.
     * Rules: first page of section + titlePage + "first" key → "first";
     *        even page number + "even" key → "even"; otherwise → "default".
     */
    private resolveHeader;
    /** Same as resolveHeader but for footers. */
    private resolveFooter;
    /**
     * Render header or footer content inside a margin zone element.
     * Creates a positioned div, renders RenderNode children as read-only DOM,
     * then substitutes dynamic field values (PAGE, NUMPAGES) with correct numbers.
     */
    private renderHeaderFooterInZone;
    /** Render page-break strip overlays at computed positions. */
    private renderOverlays;
    /** Update the page frame min-height to encompass all content. */
    private updateFrameHeight;
    /** Fallback: render overlays from current DOM state after max iterations. */
    private renderCurrentState;
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
    }): void;
    /**
     * Returns an array of gap zones in canvas-relative coordinates.
     * Each zone is a {top, bottom} range where the caret should not rest.
     */
    getGapZones(): Array<{
        top: number;
        bottom: number;
    }>;
    /**
     * If the collapsed caret sits inside a page-break gap zone, snap it
     * to the nearest visible position above or below the gap.
     */
    adjustCursorForPageBreaks(): void;
    /**
     * Determine the 1-based page number currently visible at the top of the scroll area.
     */
    getCurrentPage(scrollArea: HTMLElement): number;
    /**
     * Returns the 1-based page number whose section should drive ruler dimensions.
     * Changes only when the full gap strip (bottom margin + gap + top margin) has
     * completely scrolled past the top of the viewport — i.e., the previous page
     * is no longer visible at all.
     */
    getPageForRuler(scrollArea: HTMLElement): number;
    /**
     * Returns the 1-based page where the text caret is currently located.
     * Uses DOM selection position, not scroll position.
     */
    getPageForCursor(): number;
    /** Returns the 0-based section index where the text caret is currently located. */
    getSectionForCursor(): number;
    /**
     * Returns the ruler dimensions for the section containing the given 1-based page number.
     * All values are already in px (converted by updateFromSections).
     */
    getPageRulerDimensions(page: number): {
        pageWidth: number;
        pageHeight: number;
        marginLeft: number;
        marginRight: number;
        marginTop: number;
        marginBottom: number;
    } | null;
    /**
     * Returns the scroll-y of the given 1-based page's top edge (start of its
     * white paper area, just after the gray inter-page gap).
     * Uses the real break positions from the last pagination run — correct even
     * when different sections have different page heights.
     */
    getPageTopScrollY(page: number): number;
    /**
     * Total scrollable height of the editor (canvas content + pages-wrapper padding).
     * Use this for sizing the vertical ruler SVG accurately.
     */
    getTotalScrollHeight(): number;
    /**
     * Move the caret to a visible position at the given canvas-relative Y coordinate.
     */
    private moveCursorToVisiblePosition;
}
//# sourceMappingURL=page-layout.d.ts.map