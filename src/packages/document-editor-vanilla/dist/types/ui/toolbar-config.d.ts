import type { FormatState } from '../bridge/types';
/**
 * Lucide icon node format: array of [tag, attrs] pairs.
 * Matches the runtime format of icons exported from the lucide package.
 */
export type LucideIconDef = [string, Record<string, string>][];
export type ToolbarTheme = 'word' | 'gdocs' | 'compact';
export interface ToolbarButtonConfig {
    id: string;
    type: 'button' | 'toggle';
    icon: LucideIconDef;
    label?: string;
    tooltip: string;
    shortcut?: string;
    action: string;
    isActive?: (fs: FormatState) => boolean;
    isEnabled?: (fs: FormatState) => boolean;
}
export interface ToolbarSelectConfig {
    id: string;
    type: 'select';
    tooltip: string;
    options: Array<{
        value: string;
        label: string;
    }>;
    getValue: (fs: FormatState) => string;
    action: string;
    width?: string;
}
export interface ToolbarDropdownConfig {
    id: string;
    type: 'dropdown';
    icon: LucideIconDef;
    tooltip: string;
    options: Array<{
        label: string;
        value: string;
    }>;
    action: string;
}
export interface ToolbarComboConfig {
    id: string;
    type: 'combobox';
    tooltip: string;
    options: Array<{
        value: string;
        label: string;
    }>;
    getValue: (fs: FormatState) => string;
    action: string;
    width?: string;
    placeholder?: string;
}
export interface ToolbarSeparatorConfig {
    id: string;
    type: 'separator';
}
export type ToolbarItemConfig = ToolbarButtonConfig | ToolbarSelectConfig | ToolbarDropdownConfig | ToolbarComboConfig | ToolbarSeparatorConfig;
export interface ToolbarGroupConfig {
    id: string;
    items: ToolbarItemConfig[];
}
export interface ToolbarPreset {
    id: string;
    name: string;
    description: string;
    theme: ToolbarTheme;
    rows: ToolbarGroupConfig[][];
}
//# sourceMappingURL=toolbar-config.d.ts.map