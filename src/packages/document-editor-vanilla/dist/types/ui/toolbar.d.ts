import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse, FormatState } from '../bridge/types';
import type { ToolbarPreset } from './toolbar-config';
export type RenderCallback = (response: EngineResponse) => void;
export declare class Toolbar {
    private el;
    private ctx;
    private currentPreset;
    private itemElements;
    private stateUpdaters;
    private formatStateUpdaters;
    constructor(container: HTMLElement, engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback, preset?: ToolbarPreset);
    getElement(): HTMLElement;
    updateState(response: EngineResponse): void;
    updateFormatState(fs: FormatState): void;
    switchPreset(preset: ToolbarPreset): void;
    setItemVisible(id: string, visible: boolean): void;
    getHiddenItems(): string[];
    saveCustomization(): void;
    loadCustomization(): void;
    private renderPreset;
    private buildRow;
    private buildGroup;
    private buildItem;
    private buildButton;
    private buildSelect;
    private buildDropdown;
    private buildCombo;
    private buildInlineSeparator;
    private btnBase;
    private btnActive;
    private dropBase;
}
//# sourceMappingURL=toolbar.d.ts.map