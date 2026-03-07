import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export type RenderCallback = (response: EngineResponse) => void;
/**
 * SVG horizontal ruler with inch marks and draggable indent markers.
 * Positioned above the page canvas, aligned with page margins.
 */
export declare class HorizontalRuler {
    private el;
    private svg;
    private engine;
    private canvas;
    private onResponse;
    private pageWidth;
    private marginLeft;
    private marginRight;
    private zoom;
    private indentLeft;
    private indentFirstLine;
    constructor(container: HTMLElement, engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback);
    getElement(): HTMLElement;
    setZoom(zoom: number): void;
    syncScrollLeft(scrollLeft: number): void;
    updateDimensions(pageWidth: number, marginLeft: number, marginRight: number): void;
    updateIndents(indentLeft: number, indentFirstLine: number): void;
    private render;
    private addLine;
    private addTriangle;
}
//# sourceMappingURL=ruler-h.d.ts.map