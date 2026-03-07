import { useCallback, useRef } from 'react';
import type { DocumentEditorHandle } from './DocumentEditor';

/**
 * Convenience hook for controlling a DocumentEditor from a parent component.
 *
 * @example
 * ```tsx
 * function App() {
 *   const { ref, exportDocx, importDocx } = useDocumentEditor();
 *   return (
 *     <>
 *       <button onClick={exportDocx}>Export</button>
 *       <DocumentEditor ref={ref} />
 *     </>
 *   );
 * }
 * ```
 */
export function useDocumentEditor() {
  const ref = useRef<DocumentEditorHandle>(null);

  const exportDocx = useCallback(() => {
    if (!ref.current) throw new Error('DocumentEditor ref not attached');
    return ref.current.exportDocx();
  }, []);

  const importDocx = useCallback((bytes: Uint8Array) => {
    if (!ref.current) throw new Error('DocumentEditor ref not attached');
    return ref.current.importDocx(bytes);
  }, []);

  return { ref, exportDocx, importDocx };
}
