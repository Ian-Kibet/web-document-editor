import { defineConfig } from 'vite';
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
const isLib = process.env.BUILD_MODE === 'lib';

export default defineConfig(isLib
  ? {
      // Library build: outputs ESM + types for npm consumers
      plugins: [
        tailwindcss(),
        {
          name: 'copy-wasm-framework',
          closeBundle() {
            const src = path.join(
              wasmProject, 'bin', 'Release', 'net10.0', 'publish', 'wwwroot', '_framework'
            );
            const dst = path.join(__dirname, 'dist', '_framework');
            if (!fs.existsSync(src)) {
              console.warn('[copy-wasm] publish output not found, skipping:', src);
              return;
            }
            fs.cpSync(src, dst, { recursive: true });
            console.log('[copy-wasm] Copied _framework → dist/_framework');
          },
        },
      ],
      build: {
        outDir: 'dist',
        emptyOutDir: false,
        sourcemap: true,
        lib: {
          entry: 'src/index.ts',
          formats: ['es'],
          fileName: 'index',
          cssFileName: 'styles',
        },
        rollupOptions: {
          // Externalize all dependencies — consumers bundle them
          external: [
            'lucide',
            'tailwindcss',
            '@tailwindcss/vite',
          ],
        },
      },
    }
  : {
      // App / dev server mode
      plugins: [tailwindcss()],
      root: '.',
      publicDir: 'public',
      build: {
        outDir: 'dist',
        sourcemap: true,
      },
      server: {
        port: 5173,
        // Proxy Blazor WASM requests to the .NET dev server
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
