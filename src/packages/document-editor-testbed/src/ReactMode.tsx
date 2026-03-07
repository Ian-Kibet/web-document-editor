import { useRef } from 'react';
import type { EngineBridge } from 'document-editor-vanilla';
import { DocumentEditor } from 'document-editor-react';
import type { DocumentEditorHandle } from 'document-editor-react';
import type { Preset } from './App';

interface ReactModeProps {
  preset: Preset;
  storagePrefix: string;
  onEngine: (engine: EngineBridge | null) => void;
}

export function ReactMode({ preset, storagePrefix, onEngine }: ReactModeProps) {
  const editorRef = useRef<DocumentEditorHandle>(null);

  return (
    <DocumentEditor
      ref={editorRef}
      toolbarPreset={preset}
      storagePrefix={storagePrefix}
      onReady={handle => onEngine(handle.engine)}
      style={{ width: '100%', height: '100%' }}
    />
  );
}
