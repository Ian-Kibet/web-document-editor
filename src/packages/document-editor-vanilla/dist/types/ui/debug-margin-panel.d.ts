import type { DebugSectionSnapshot } from '../renderer/page-layout';
export declare class DebugMarginPanel {
    /** Width-transition outer wrapper (0 → 220px). Inserted into editorRow. */
    private wrapper;
    /** Fixed-width 220px content pane. */
    private inner;
    private body;
    private tabRow;
    private isOpen;
    private currentSection;
    private snapshots;
    private canvas;
    constructor(mountPoint: HTMLElement, canvas: HTMLElement);
    /** Returns the wrapper element (already inserted by caller). */
    getElement(): HTMLElement;
    toggle(): void;
    open(): void;
    close(): void;
    /** Called when cursor moves; updates the displayed section if it changed. */
    setCursorSection(index: number): void;
    update(snapshots: DebugSectionSnapshot[]): void;
    private render;
    /** Read computed styles on the nth <section> inside the canvas. */
    private measureSection;
    private sectionLabel;
    private row;
    /** twips → px → inches row, with optional DOM comparison. */
    private marginRow;
    private twipRow;
    private pxRow;
    private domCheckRow;
}
//# sourceMappingURL=debug-margin-panel.d.ts.map