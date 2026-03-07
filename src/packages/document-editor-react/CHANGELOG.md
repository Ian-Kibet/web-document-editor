# document-editor-react

## 0.1.0

### Initial Release

- `DocumentEditor` component (`forwardRef`) with full editor mounted in a div
- `DocumentEditorHandle` ref API: `engine`, `exportDocx()`, `importDocx()`
- `useDocumentEditor()` hook for convenient ref + export/import bindings
- Props: `toolbarPreset`, `initialDocJson`, `storagePrefix`, `onReady`, `onError`, `className`, `style`
- Clean teardown via `useEffect` cleanup calling `instance.destroy()`
