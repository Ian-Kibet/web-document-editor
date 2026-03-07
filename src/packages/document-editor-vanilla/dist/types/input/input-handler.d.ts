import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export type RenderCallback = (response: EngineResponse) => void;
/**
 * Intercepts all user input via the beforeinput event.
 * ALWAYS prevents default — the C# engine is the single source of truth.
 */
export declare class InputHandler {
    private engine;
    private canvas;
    private onResponse;
    private processing;
    constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback);
    destroy(): void;
    private handleBeforeInput;
    private handleCompositionEnd;
}
//# sourceMappingURL=input-handler.d.ts.map