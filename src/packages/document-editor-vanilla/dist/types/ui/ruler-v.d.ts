/**
 * SVG vertical ruler with inch marks.
 * Positioned to the left of the page canvas, scrolls vertically in sync.
 */
export declare class VerticalRuler {
    private el;
    private svg;
    private pageHeight;
    private marginTop;
    private marginBottom;
    private pageCount;
    private zoom;
    private activePage;
    private activePageTopScrollY;
    private totalScrollHeight;
    private readonly gapHeight;
    private readonly topPadding;
    constructor(container: HTMLElement);
    getElement(): HTMLElement;
    updateDimensions(pageHeight: number, marginTop: number, marginBottom: number, activePage?: number, activePageTopScrollY?: number): void;
    setTotalScrollHeight(h: number): void;
    setPageCount(count: number): void;
    setZoom(zoom: number): void;
    /**
     * Sync vertical scroll position with the page container.
     * Uses CSS transform so the ruler follows at any scroll depth,
     * not limited by SVG content height.
     */
    syncScroll(scrollTop: number): void;
    private updateSvgHeight;
    private render;
    private addLine;
}
//# sourceMappingURL=ruler-v.d.ts.map