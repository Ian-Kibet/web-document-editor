import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { documentEditorVitePlugin } from 'document-editor-vanilla/vite';

export default defineConfig({
  plugins: [
    react(),
    documentEditorVitePlugin(),
  ],
});
