/**
 * Zoom controls for the editor canvas.
 */
export declare class ZoomController {
    private level;
    private target;
    private onZoomChange?;
    private static readonly MIN;
    private static readonly MAX;
    private static readonly STEP;
    constructor(target: HTMLElement, onChange?: (percent: number) => void);
    getLevel(): number;
    setLevel(percent: number): void;
    zoomIn(): void;
    zoomOut(): void;
    resetZoom(): void;
    private apply;
}
//# sourceMappingURL=zoom.d.ts.map