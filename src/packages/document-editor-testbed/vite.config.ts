import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function findFrameworkDir(wasmProjectPath: string): string | null {
  const debugDir = path.join(wasmProjectPath, 'bin', 'Debug');
  if (!fs.existsSync(debugDir)) return null;
  for (const entry of fs.readdirSync(debugDir)) {
    const candidate = path.join(debugDir, entry, 'wwwroot', '_framework');
    if (fs.existsSync(candidate)) return candidate;
  }
  return null;
}

const wasmProject = path.resolve(__dirname, '../../DocumentEditor.Wasm');

export default defineConfig({
  plugins: [react(), tailwindcss()],
  root: '.',
  resolve: {
    alias: {
      'document-editor-vanilla': path.resolve(__dirname, '../document-editor-vanilla/src/index.ts'),
      'document-editor-react': path.resolve(__dirname, '../document-editor-react/src/index.ts'),
    },
  },
  server: {
    port: 5174,
    proxy: {
      '/_framework': {
        target: 'http://localhost:5000',
        bypass(req, res) {
          const urlPath = (req.url ?? '').split('?')[0];
          if (!urlPath.endsWith('.pdb') && !urlPath.endsWith('.pdb.gz')) return;

          const frameworkDir = findFrameworkDir(wasmProject);
          const filename = path.basename(urlPath);
          const pdbFile = frameworkDir ? path.join(frameworkDir, filename) : null;

          if (pdbFile && fs.existsSync(pdbFile)) {
            const data = fs.readFileSync(pdbFile);
            res.writeHead(200, { 'Content-Type': 'application/octet-stream', 'Content-Length': String(data.length) });
            res.end(data);
          } else {
            res.writeHead(200, { 'Content-Length': '0' });
            res.end();
          }
          return false;
        },
      },
      '/_content': 'http://localhost:5000',
    },
  },
});
