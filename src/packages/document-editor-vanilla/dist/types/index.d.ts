import { EngineBridge } from './bridge/engine-bridge';
import './styles/main.css';
import './styles/editor.css';
import './styles/pages.css';
export type { EngineBridge } from './bridge/engine-bridge';
export type * from './bridge/types';
/**
 * Options for mounting the Document Editor into a container element.
 */
export interface EditorMountOptions {
    /** The DOM element the editor will be rendered into. */
    container: HTMLElement;
    /** Optional serialized document JSON to load on startup. */
    initialDocJson?: string;
    /** localStorage key prefix. Defaults to 'documentEditor'. */
    storagePrefix?: string;
    /** Toolbar layout preset. Defaults to 'word'. */
    toolbarPreset?: 'word' | 'gdocs' | 'compact';
    /** Called when the editor is fully initialized and ready. */
    onReady?: (instance: EditorInstance) => void;
    /** Called if initialization fails. */
    onError?: (err: Error) => void;
}
/**
 * Handle returned by mountEditor(). Use destroy() for cleanup.
 */
export interface EditorInstance {
    /** The engine bridge for programmatic document operations. */
    engine: EngineBridge;
    /** Tears down the editor: removes listeners, clears DOM, cleans up globals. */
    destroy(): void;
}
/**
 * Mount a Document Editor instance into the given container element.
 *
 * @param options - Mount configuration including container, preset, and callbacks.
 * @returns A promise that resolves to an EditorInstance once initialization is complete.
 *
 * @example
 * ```ts
 * import { mountEditor } from 'document-editor-vanilla';
 *
 * const instance = await mountEditor({
 *   container: document.getElementById('editor-root')!,
 *   toolbarPreset: 'word',
 * });
 *
 * // Export as .docx:
 * const bytes = await instance.engine.exportDocx();
 * ```
 */
export declare function mountEditor(options: EditorMountOptions): Promise<EditorInstance>;
//# sourceMappingURL=index.d.ts.map