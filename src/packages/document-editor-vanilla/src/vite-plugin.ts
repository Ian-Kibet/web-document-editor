import type { Plugin } from 'vite'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
// __dirname = dist/  →  _framework/ is a sibling
const frameworkDir = path.join(__dirname, '_framework')

const MIME: Record<string, string> = {
  '.js':   'application/javascript',
  '.wasm': 'application/wasm',
  '.json': 'application/json',
  '.css':  'text/css',
}

export function documentEditorVitePlugin(): Plugin {
  return {
    name: 'document-editor:blazor-framework',

    // Dev server: intercept /_framework/* and serve from this package's dist/_framework/
    configureServer(server) {
      server.middlewares.use('/_framework', (req, res, next) => {
        const urlPath = (req.url ?? '/').split('?')[0]
        const filePath = path.join(frameworkDir, urlPath)
        if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) return next()
        const ext = path.extname(filePath).toLowerCase()
        const data = fs.readFileSync(filePath)
        res.writeHead(200, {
          'Content-Type': MIME[ext] ?? 'application/octet-stream',
          'Content-Length': String(data.length),
          'Cache-Control': 'no-cache',
        })
        res.end(data)
      })
    },

    // Production build: copy _framework/ into the consumer's output directory
    writeBundle(options) {
      if (!fs.existsSync(frameworkDir)) return
      const outDir = options.dir ?? 'dist'
      const dst = path.join(outDir, '_framework')
      fs.cpSync(frameworkDir, dst, { recursive: true })
    },
  }
}
