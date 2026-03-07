import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export type ToolbarContext = {
    engine: EngineBridge;
    canvas: HTMLElement;
    onResponse: (r: EngineResponse) => void;
};
export type ToolbarAction = (ctx: ToolbarContext, value?: string) => Promise<void>;
export declare const TOOLBAR_ACTIONS: Record<string, ToolbarAction>;
//# sourceMappingURL=toolbar-registry.d.ts.map