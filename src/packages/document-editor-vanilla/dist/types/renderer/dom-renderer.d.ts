import type { RenderNode } from '../bridge/types';
/**
 * Render a RenderNode tree to DOM elements inside a container.
 * Uses keyed reconciliation (data-node-id) to avoid full DOM rebuilds on every keystroke.
 */
export declare function renderTree(nodes: RenderNode[], container: HTMLElement): void;
/**
 * Recursively create a DOM element from a RenderNode.
 * Each element gets data-node-id for cursor mapping.
 */
export declare function createDomNode(node: RenderNode): HTMLElement;
/**
 * Create a read-only DOM element from a RenderNode.
 * The root is non-editable and non-selectable (for header/footer display).
 */
export declare function createReadOnlyDomNode(node: RenderNode): HTMLElement;
//# sourceMappingURL=dom-renderer.d.ts.map