import {
  forwardRef,
  useEffect,
  useImperativeHandle,
  useRef,
} from 'react';
import type {
  EditorInstance,
  EditorMountOptions,
  EngineBridge,
} from 'document-editor-vanilla';
import { mountEditor } from 'document-editor-vanilla';

export interface DocumentEditorHandle {
  /** The underlying engine bridge for programmatic operations. */
  engine: EngineBridge;
  /** Export the current document as a .docx byte array. */
  exportDocx(): Promise<Uint8Array>;
  /** Import a .docx byte array and replace the current document. */
  importDocx(bytes: Uint8Array): Promise<void>;
}

export interface DocumentEditorProps {
  /** Called when the editor is fully initialized. */
  onReady?: (handle: DocumentEditorHandle) => void;
  /** Called if initialization fails. */
  onError?: (err: Error) => void;
  /** Optional serialized document JSON to load on startup. */
  initialDocJson?: string;
  /** CSS class name for the outer container div. */
  className?: string;
  /** Inline styles for the outer container div. */
  style?: React.CSSProperties;
  /** Toolbar layout preset. Defaults to 'word'. */
  toolbarPreset?: EditorMountOptions['toolbarPreset'];
  /** localStorage key prefix. Defaults to 'documentEditor'. */
  storagePrefix?: string;
}

export const DocumentEditor = forwardRef<DocumentEditorHandle, DocumentEditorProps>(
  function DocumentEditor(
    { onReady, onError, initialDocJson, className, style, toolbarPreset, storagePrefix },
    ref,
  ) {
    const containerRef = useRef<HTMLDivElement>(null);
    const instanceRef = useRef<EditorInstance | null>(null);
    const handleRef = useRef<DocumentEditorHandle | null>(null);

    useImperativeHandle(ref, () => ({
      get engine() {
        if (!instanceRef.current) throw new Error('DocumentEditor not yet initialized');
        return instanceRef.current.engine;
      },
      exportDocx(): Promise<Uint8Array> {
        if (!instanceRef.current) throw new Error('DocumentEditor not yet initialized');
        return instanceRef.current.engine.exportDocx();
      },
      importDocx(bytes: Uint8Array): Promise<void> {
        if (!instanceRef.current) throw new Error('DocumentEditor not yet initialized');
        return instanceRef.current.engine.importDocx(bytes).then(() => {});
      },
    }));

    useEffect(() => {
      if (!containerRef.current) return;
      let cancelled = false;

      mountEditor({
        container: containerRef.current,
        initialDocJson,
        storagePrefix,
        toolbarPreset,
        onReady(instance) {
          if (cancelled) return;
          instanceRef.current = instance;
          const handle: DocumentEditorHandle = {
            get engine() { return instance.engine; },
            exportDocx: () => instance.engine.exportDocx(),
            importDocx: (bytes) => instance.engine.importDocx(bytes).then(() => {}),
          };
          handleRef.current = handle;
          onReady?.(handle);
        },
        onError,
      }).catch((err: Error) => {
        if (!cancelled) onError?.(err);
      });

      return () => {
        cancelled = true;
        instanceRef.current?.destroy();
        instanceRef.current = null;
        handleRef.current = null;
      };
    }, []); // mount once — options are captured at mount time

    return (
      <div
        ref={containerRef}
        className={className}
        style={{ width: '100%', height: '100%', ...style }}
      />
    );
  },
);
