# document-editor-vanilla

An OOXML-native web document editor with a C# WASM engine and vanilla TypeScript frontend. Reads and writes real `.docx` files via the Open XML SDK — no HTML-to-DOCX conversion.

## What it is

- **OOXML document model** in C# (compiled to WebAssembly via Blazor)
- **All edits go through the C# engine** — the DOM is never the source of truth
- **Real `.docx` I/O** via the Open XML SDK — import from Word, export back to Word
- **Multi-page layout**, rulers, toolbar (Word/Google Docs/Compact presets), sidebar, status bar
- **Vanilla TypeScript** — zero framework dependencies in the core

## Prerequisites

The editor requires the Blazor WASM runtime (`_framework/` directory) to be served alongside your app. See [WASM Runtime Setup](#wasm-runtime-setup).

## Installation

```bash
npm install document-editor-vanilla
```

For React:
```bash
npm install document-editor-react document-editor-vanilla
```

You must also import the editor styles once in your app root:

```ts
import 'document-editor-vanilla/styles';
```

## Quick Start

### Vanilla JS / TypeScript

```ts
import { mountEditor } from 'document-editor-vanilla';
import 'document-editor-vanilla/styles';

const instance = await mountEditor({
  container: document.getElementById('editor-root')!,
  toolbarPreset: 'word',
});

// Later: clean up
instance.destroy();
```

### React

```tsx
import { DocumentEditor, useDocumentEditor } from 'document-editor-react';
import 'document-editor-vanilla/styles';

function App() {
  const { ref, exportDocx } = useDocumentEditor();

  return (
    <div style={{ height: '100vh' }}>
      <button onClick={async () => {
        const bytes = await exportDocx();
        const blob = new Blob([bytes], { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'document.docx';
        a.click();
        URL.revokeObjectURL(url);
      }}>
        Export .docx
      </button>
      <DocumentEditor ref={ref} toolbarPreset="word" style={{ height: 'calc(100vh - 40px)' }} />
    </div>
  );
}
```

## Exporting .docx

```ts
const bytes: Uint8Array = await instance.engine.exportDocx();
const blob = new Blob([bytes], {
  type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
});
const url = URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = 'document.docx';
a.click();
URL.revokeObjectURL(url);
```

## Importing .docx

```ts
const fileInput = document.createElement('input');
fileInput.type = 'file';
fileInput.accept = '.docx';
fileInput.onchange = async () => {
  const file = fileInput.files?.[0];
  if (!file) return;
  const bytes = new Uint8Array(await file.arrayBuffer());
  await instance.engine.importDocx(bytes);
};
fileInput.click();
```

## Toolbar Presets

| Preset | Description |
|--------|-------------|
| `'word'` | Two-row Word-style toolbar with font controls (default) |
| `'gdocs'` | Single-row Google Docs-style toolbar |
| `'compact'` | Single-row compact toolbar for embedded use |

```ts
const instance = await mountEditor({
  container,
  toolbarPreset: 'gdocs',
});
```

## API Reference

### `mountEditor(options): Promise<EditorInstance>`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `container` | `HTMLElement` | required | DOM element to render the editor into |
| `initialDocJson` | `string` | — | Serialized document JSON to load |
| `storagePrefix` | `string` | `'documentEditor'` | localStorage key prefix |
| `toolbarPreset` | `'word' \| 'gdocs' \| 'compact'` | `'word'` | Toolbar layout |
| `onReady` | `(instance: EditorInstance) => void` | — | Called when editor is ready |
| `onError` | `(err: Error) => void` | — | Called if initialization fails |

### `EditorInstance`

| Member | Description |
|--------|-------------|
| `engine: EngineBridge` | Programmatic API for document operations |
| `destroy(): void` | Tears down the editor and removes all event listeners |

### `EngineBridge` methods

| Method | Returns | Description |
|--------|---------|-------------|
| `initialize()` | `Promise<EngineResponse>` | Load the default empty document |
| `exportDocx()` | `Promise<Uint8Array>` | Serialize document to .docx bytes |
| `importDocx(bytes)` | `Promise<EngineResponse>` | Load a .docx file |
| `insertText(text, sel)` | `Promise<EngineResponse>` | Insert text at selection |
| `splitParagraph(sel)` | `Promise<EngineResponse>` | Insert paragraph break |
| `deleteBackward(sel)` | `Promise<EngineResponse>` | Delete character before cursor |
| `deleteForward(sel)` | `Promise<EngineResponse>` | Delete character after cursor |
| `deleteSelection(sel)` | `Promise<EngineResponse>` | Delete selected range |
| `toggleFormat(prop, sel)` | `Promise<EngineResponse>` | Toggle bold/italic/underline/strikethrough |
| `setAlignment(align, sel)` | `Promise<EngineResponse>` | Set paragraph alignment |
| `setParagraphStyle(style, sel)` | `Promise<EngineResponse>` | Apply Heading1–6 or Normal |
| `setFontFamily(font, sel)` | `Promise<EngineResponse>` | Set font family |
| `setFontSize(pt, sel)` | `Promise<EngineResponse>` | Set font size in points |
| `undo()` | `Promise<EngineResponse>` | Undo last command |
| `redo()` | `Promise<EngineResponse>` | Redo last undone command |
| `getFormatState(sel)` | `Promise<FormatState>` | Query current format state |

## WASM Runtime Setup

The editor requires the Blazor WASM runtime files (`_framework/`) to be served at your site root. After building the .NET project (`dotnet publish`), copy the `_framework/` directory from the publish output.

### Vite

In `vite.config.ts`, copy the framework files to `public/`:

```ts
import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  // ... your config
  server: {
    proxy: {
      '/_framework': 'http://localhost:5000',
    },
  },
});
```

For production builds, copy `_framework/` to your Vite `public/` directory so it gets served at `/_framework`.

### Next.js

In `next.config.ts`, add rewrites to proxy `/_framework` to your .NET server, or serve the static `_framework/` files from the `public/` directory.

## Roadmap

See [ROADMAP.md](./ROADMAP.md) for upcoming features and long-term plans.

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for development setup and contribution guidelines.

## License

Apache-2.0
