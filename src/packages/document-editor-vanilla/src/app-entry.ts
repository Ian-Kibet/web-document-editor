/**
 * Dev server bootstrap entry point.
 * This file is NOT included in the library build — it is only used
 * by Vite's dev server (and the Wasm wwwroot index.html).
 */
import { mountEditor } from './index';

mountEditor({
  container: document.getElementById('editor-root')!,
}).catch((err: Error) => {
  console.error('Document Editor failed to initialize:', err);
  const root = document.getElementById('editor-root');
  if (root) {
    root.innerHTML = `<div class="editor-loading" style="color:red">
      Failed to load Document Editor: ${err.message}
    </div>`;
  }
});
