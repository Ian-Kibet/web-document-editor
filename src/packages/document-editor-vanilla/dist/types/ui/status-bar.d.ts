import type { RenderNode } from '../bridge/types';
/**
 * Status bar at the bottom of the editor showing page info, word count, and zoom.
 * Styled with Tailwind CSS.
 */
export declare class StatusBar {
    private el;
    private pageInfo;
    private wordCount;
    private zoomLabel;
    private currentPage;
    private totalPages;
    private words;
    constructor(container: HTMLElement);
    getElement(): HTMLElement;
    updatePageInfo(currentPage: number, totalPages: number): void;
    updateWordCount(renderTree: RenderNode[]): void;
    updateZoom(percent: number): void;
    private refreshLeft;
}
//# sourceMappingURL=status-bar.d.ts.map