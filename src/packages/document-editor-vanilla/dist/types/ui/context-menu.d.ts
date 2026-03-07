import type { EngineResponse } from '../bridge/types';
import type { EngineBridge } from '../bridge/engine-bridge';
export declare class ContextMenu {
    private readonly canvas;
    private readonly engine;
    private readonly onResponse;
    private readonly menuEl;
    private readonly _onContextMenu;
    private readonly _onDocContextMenu;
    private readonly _onClickOutside;
    private readonly _onKeyDown;
    constructor(canvas: HTMLElement, engine: EngineBridge, onResponse: (r: EngineResponse) => void);
    destroy(): void;
    private handleContextMenu;
    private handleDocContextMenu;
    private handleClickOutside;
    private handleKeyDown;
    private buildMenu;
    private addItem;
    private addSeparator;
    private position;
    private hide;
}
//# sourceMappingURL=context-menu.d.ts.map