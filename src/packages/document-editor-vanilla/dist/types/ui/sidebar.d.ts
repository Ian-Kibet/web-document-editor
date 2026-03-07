import type { RenderNode } from '../bridge/types';
/**
 * Collapsible sidebar with icon tabs: Outline, Statistics, XML Debug.
 * Styled with Tailwind CSS.
 */
export declare class Sidebar {
    private el;
    private tabsRow;
    private panelContainer;
    private outlinePanel;
    private statsPanel;
    private xmlPanel;
    private toggleBtn;
    private collapsed;
    private tabButtons;
    constructor(container: HTMLElement);
    getElement(): HTMLElement;
    toggle(): void;
    updateOutline(renderTree: RenderNode[]): void;
    updateStats(renderTree: RenderNode[]): void;
    updateXmlDebug(xml: string): void;
    private createTabBtn;
    private createPanel;
    private activateTab;
    private tabActiveClass;
    private tabInactiveClass;
}
//# sourceMappingURL=sidebar.d.ts.map