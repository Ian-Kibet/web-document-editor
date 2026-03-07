import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export type RenderCallback = (response: EngineResponse) => void;
/**
 * Keyboard shortcut handler.
 * Listens for keydown events and maps key combos to engine commands.
 */
export declare class KeyboardShortcuts {
    private engine;
    private canvas;
    private onResponse;
    private shortcuts;
    constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback);
    destroy(): void;
    private buildShortcuts;
    private handleKeyDown;
    private formatCmd;
    private alignCmd;
}
//# sourceMappingURL=keyboard-shortcuts.d.ts.map