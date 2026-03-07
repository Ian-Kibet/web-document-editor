# document-editor-react

React component wrapper for [`document-editor-vanilla`](../document-editor-vanilla/README.md).

## Installation

```bash
npm install document-editor-react document-editor-vanilla
```

Import the editor styles once in your app root:

```ts
import 'document-editor-vanilla/styles';
```

## Usage

```tsx
import { DocumentEditor, useDocumentEditor } from 'document-editor-react';
import 'document-editor-vanilla/styles';

function App() {
  const { ref, exportDocx } = useDocumentEditor();

  const handleExport = async () => {
    const bytes = await exportDocx();
    const blob = new Blob([bytes], {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'document.docx';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      <button onClick={handleExport}>Export .docx</button>
      <DocumentEditor
        ref={ref}
        toolbarPreset="word"
        style={{ flex: 1 }}
        onReady={(handle) => console.log('ready', handle.engine)}
        onError={(err) => console.error('editor error', err)}
      />
    </div>
  );
}
```

## API

### `<DocumentEditor />`

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `ref` | `RefObject<DocumentEditorHandle>` | — | Ref for programmatic control |
| `toolbarPreset` | `'word' \| 'gdocs' \| 'compact'` | `'word'` | Toolbar layout |
| `initialDocJson` | `string` | — | Serialized document JSON |
| `storagePrefix` | `string` | `'documentEditor'` | localStorage key prefix |
| `className` | `string` | — | CSS class for container |
| `style` | `CSSProperties` | — | Inline styles for container |
| `onReady` | `(handle: DocumentEditorHandle) => void` | — | Called when ready |
| `onError` | `(err: Error) => void` | — | Called on init error |

### `DocumentEditorHandle` (via `ref`)

| Method | Description |
|--------|-------------|
| `engine` | The `EngineBridge` for programmatic operations |
| `exportDocx()` | Export current document as `Uint8Array` |
| `importDocx(bytes)` | Load a `.docx` `Uint8Array` |

### `useDocumentEditor()`

Returns `{ ref, exportDocx, importDocx }` — a convenience hook for controlling the editor from a parent component.

## License

Apache-2.0
