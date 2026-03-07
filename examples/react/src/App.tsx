import { useRef, useState } from 'react';
import { DocumentEditor } from 'document-editor-react';
import type { DocumentEditorHandle } from 'document-editor-react';
import type { EditorMountOptions } from 'document-editor-vanilla';

type Preset = NonNullable<EditorMountOptions['toolbarPreset']>;

const PRESETS: Preset[] = ['word', 'gdocs', 'compact'];

export function App() {
  const editorRef = useRef<DocumentEditorHandle>(null);
  const [ready, setReady] = useState(false);
  const [preset, setPreset] = useState<Preset>('word');
  const [editorKey, setEditorKey] = useState(0);

  function handlePresetChange(next: Preset) {
    setPreset(next);
    setReady(false);
    setEditorKey((k) => k + 1); // remount editor cleanly
  }

  async function handleExport() {
    if (!editorRef.current) return;
    const bytes = await editorRef.current.exportDocx();
    const blob = new Blob([bytes], {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'document.docx';
    a.click();
    URL.revokeObjectURL(url);
  }

  function handleImport() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.docx';
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file || !editorRef.current) return;
      const buffer = await file.arrayBuffer();
      await editorRef.current.importDocx(new Uint8Array(buffer));
    };
    input.click();
  }

  return (
    <div style={styles.root}>
      <div style={styles.header}>
        <span style={styles.title}>Document Editor</span>
        <div style={styles.controls}>
          <label style={styles.label}>
            Preset:
            <select
              style={styles.select}
              value={preset}
              onChange={(e) => handlePresetChange(e.target.value as Preset)}
            >
              {PRESETS.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>
          <button style={styles.button} onClick={handleImport} disabled={!ready}>
            Import .docx
          </button>
          <button style={styles.button} onClick={handleExport} disabled={!ready}>
            Export .docx
          </button>
        </div>
      </div>

      <div style={styles.editorWrapper}>
        <DocumentEditor
          key={editorKey}
          ref={editorRef}
          toolbarPreset={preset}
          onReady={() => setReady(true)}
          onError={(err) => console.error('Editor error:', err)}
          style={{ width: '100%', height: '100%' }}
        />
      </div>
    </div>
  );
}

const styles = {
  root: {
    display: 'flex',
    flexDirection: 'column' as const,
    height: '100dvh',
    fontFamily: 'system-ui, sans-serif',
    background: '#f5f5f5',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '8px 16px',
    background: '#fff',
    borderBottom: '1px solid #e0e0e0',
    flexShrink: 0,
  },
  title: {
    fontWeight: 600,
    fontSize: '15px',
    color: '#1a1a1a',
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  label: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    fontSize: '13px',
    color: '#555',
  },
  select: {
    fontSize: '13px',
    padding: '4px 6px',
    border: '1px solid #ccc',
    borderRadius: '4px',
    background: '#fff',
    cursor: 'pointer',
  },
  button: {
    fontSize: '13px',
    padding: '5px 12px',
    border: '1px solid #ccc',
    borderRadius: '4px',
    background: '#fff',
    cursor: 'pointer',
  },
  editorWrapper: {
    flex: 1,
    minHeight: 0,
    overflow: 'hidden',
  },
} as const;
