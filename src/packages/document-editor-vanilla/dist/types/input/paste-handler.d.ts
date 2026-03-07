import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export type RenderCallback = (response: EngineResponse) => void;
/**
 * Handles paste events with explicit clipboard access.
 * The beforeinput handler covers most paste cases via insertFromPaste,
 * but this is a fallback for browsers that fire paste before beforeinput.
 */
export declare class PasteHandler {
    private engine;
    private canvas;
    private onResponse;
    constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback);
    destroy(): void;
    private handlePaste;
}
//# sourceMappingURL=paste-handler.d.ts.map