# Roadmap

## Implemented (v0.1.0)

- OOXML document model: Paragraph, Run, Table (rows/cells), Hyperlink, Image, TextContent
- Paragraph properties: alignment, indent, spacing, numbering (bullets/numbered lists)
- Run properties: bold, italic, underline, strikethrough, font family, font size, color, highlight
- Table properties: borders, cell width, merge
- Commands: InsertText, DeleteBackward, DeleteForward, SplitParagraph, ToggleFormat, SetAlignment, SetParagraphStyle, SetFontFamily, SetFontSize, ToggleList, InsertTable, InsertHyperlink, DeleteSelection, PasteText, SetIndent, InsertBreak (page/line)
- Undo/redo with unlimited history (model snapshot strategy)
- .docx import via Open XML SDK
- .docx export via Open XML SDK
- Multi-section document support (different page sizes/margins per section)
- Multi-page layout with page gap visualization
- Horizontal and vertical SVG rulers (section-aware)
- Config-driven toolbar: Word, Google Docs, and Compact presets
- Collapsible sidebar: document outline (heading navigation) + statistics
- Status bar: page/word count, zoom controls
- Context menu: table cell borders
- Image insertion (paste, file picker) with basic resize handles
- Ctrl+scroll zoom, zoom in/out buttons
- Debug margin panel (doc vs. applied margins comparison)
- `mountEditor()` / `EditorInstance` API
- React wrapper component (`document-editor-react`)
- Changeset-based npm publish workflow

## Near-term (v0.x)

- **Headers and footers rendering** — display header/footer content from OOXML in the multi-page canvas
- **Find and replace** — text search with highlight and replace
- **Color picker** — font color and highlight color UI
- **More image properties** — alt text, wrapping style, precise resize
- **Table: insert/delete rows/columns** — context menu operations
- **Comments rendering** — display review comments (read-only initially)
- **Accessibility** — keyboard navigation, ARIA labels throughout UI

## Medium-term (v1.x)

- **Track changes skeleton** — show/hide revisions, accept/reject individual changes
- **Collaborative editing hooks** — event bus API for CRDT or OT integration
- **Vue wrapper** (`document-editor-vue`)
- **Svelte wrapper** (`document-editor-svelte`)
- **Plugin API** — extensible command and toolbar registration
- **Spell check integration** — plug in browser or custom spell-check provider
- **Localization** — i18n for UI strings

## Future

- Footnotes and endnotes rendering
- Table of contents generation and navigation
- Section breaks with different page layouts per section
- Mail merge / field codes
- PDF export (via headless browser or server-side)
- Page numbers and running headers in the canvas
- Custom styles panel (create and manage named styles)
