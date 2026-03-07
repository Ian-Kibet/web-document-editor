/**
 * image-handles.ts — Selection overlay for inline images.
 *
 * Shows resize (8 cardinal handles) and rotate handles when an image is
 * clicked. Resize/rotate commits to the C# engine on mouseup.
 *
 * Usage: call attachImageHandles(canvas, scrollContainer, engine, onResponse)
 * once during editor init.
 */
import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
export declare function attachImageHandles(canvas: HTMLElement, scrollContainer: HTMLElement, engine: EngineBridge, onResponse: (r: EngineResponse) => void): void;
/** Programmatically hide handles (call after engine response re-renders) */
export declare function hideImageHandles(): void;
//# sourceMappingURL=image-handles.d.ts.map