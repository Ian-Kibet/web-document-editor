# document-editor-vanilla

## 0.1.0

### Initial Release

- OOXML-native document model (Paragraph, Run, Table, Hyperlink, Image)
- 13 editing commands: InsertText, DeleteBackward/Forward, SplitParagraph, ToggleFormat, SetAlignment, SetParagraphStyle, SetFontFamily, SetFontSize, ToggleList, InsertTable, InsertHyperlink, SetIndent
- Undo/redo via document snapshots (unlimited history)
- .docx import and export via Open XML SDK
- Multi-page layout with visual page separation
- Horizontal and vertical SVG rulers with margin indicators
- Config-driven toolbar with Word, Google Docs, and Compact presets
- Collapsible sidebar with document outline and word count stats
- Status bar with page count, word count, and zoom controls
- Context menu for table operations
- Image insertion and basic resize handles
- Ctrl+scroll zoom, zoom in/out buttons
- Paragraph marks (pilcrow) and grid lines toggles
- `mountEditor()` API for programmatic embedding
- `EditorInstance.destroy()` for clean teardown
