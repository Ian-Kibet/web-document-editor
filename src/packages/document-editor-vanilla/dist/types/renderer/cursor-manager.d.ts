import type { Position, Selection, SelectionResponse } from '../bridge/types';
/**
 * Map a DOM selection point (node + offset) to a model Position.
 *
 * Strategy: walk up from the target node to find the nearest element
 * with data-node-id, then find which block and inline that belongs to
 * by walking the parent chain.
 */
export declare function domToModelPosition(node: Node, offset: number, canvas: HTMLElement): Position | null;
/**
 * Map the full DOM selection to a model Selection (anchor + focus).
 */
export declare function domToModelSelection(canvas: HTMLElement): Selection | null;
/**
 * After re-render, restore the browser cursor to the model position.
 *
 * Find the DOM text node that corresponds to the model's inline position,
 * then call Selection.setBaseAndExtent().
 */
export declare function restoreCursor(selection: SelectionResponse, canvas: HTMLElement): void;
//# sourceMappingURL=cursor-manager.d.ts.map