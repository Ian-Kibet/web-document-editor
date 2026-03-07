# Document Editor — Implementation Plan

## Overview

Build a web-based document editor called **Document Editor** that operates on an **OOXML-native document model**. The core document engine is written in **C# compiled to WebAssembly** (via .NET Blazor WASM or `dotnet wasmbrowser`), handling all document model mutations, serialization, and .docx file I/O. The frontend is a **TypeScript** application that renders the model to DOM and captures user input.

**The key architectural principle:** The DOM is never the source of truth. All edits go through the C# engine, which mutates the model and returns a render tree. The frontend renders that tree to DOM elements. Export produces a real `.docx` file via the Open XML SDK in C# — no HTML-to-DOCX conversion.

```
┌─────────────────────────────────────────────────────────────┐
│                     Browser (Frontend)                       │
│                                                              │
│  ┌──────────┐    ┌───────────┐    ┌────────────────────┐    │
│  │ Toolbar   │    │ Rulers    │    │  Page Canvas       │    │
│  │ (TS/HTML) │    │ (SVG)     │    │  (contentEditable) │    │
│  └────┬─────┘    └───────────┘    └─────────┬──────────┘    │
│       │                                      │               │
│       │         ┌────────────────┐           │               │
│       └────────►│  EditorBridge  │◄──────────┘               │
│                 │  (TypeScript)  │                            │
│                 └───────┬────────┘                            │
│                         │ JS Interop                         │
│                         ▼                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              C# WASM Engine                          │   │
│  │                                                      │   │
│  │  ┌─────────────┐  ┌──────────┐  ┌───────────────┐  │   │
│  │  │ DocumentModel│  │ Commands │  │ Open XML SDK  │  │   │
│  │  │ (OOXML types)│  │ (mutate) │  │ (serialize/   │  │   │
│  │  │              │  │          │  │  deserialize)  │  │   │
│  │  └─────────────┘  └──────────┘  └───────────────┘  │   │
│  │                                                      │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │   │
│  │  │ History  │  │Selection │  │ RenderTree       │  │   │
│  │  │ (undo/   │  │ Model    │  │ (JSON to TS)     │  │   │
│  │  │  redo)   │  │          │  │                  │  │   │
│  │  └──────────┘  └──────────┘  └──────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Document Engine | C# / .NET 8+ | Model, commands, serialization |
| WASM Runtime | Blazor WASM (standalone) or `wasm-experimental` | Run C# in browser |
| OOXML I/O | Open XML SDK (`DocumentFormat.OpenXml`) | Read/write .docx files |
| Frontend | TypeScript | DOM rendering, input handling, toolbar |
| Build | `dotnet` CLI + Vite (or webpack) | Build pipeline |
| Styling | CSS (no framework) | Editor UI |
| Rulers | SVG | Horizontal/vertical rulers |

---

## Project Structure

```
wave-editor/
├── Document Editor.sln
│
├── src/
│   ├── Document Editor.Engine/                  # C# class library → compiled to WASM
│   │   ├── Document Editor.Engine.csproj
│   │   ├── Model/
│   │   │   ├── WaveDocument.cs             # Root document node
│   │   │   ├── Paragraph.cs                # w:p
│   │   │   ├── Run.cs                      # w:r
│   │   │   ├── TextContent.cs              # w:t, w:tab, w:br
│   │   │   ├── Hyperlink.cs                # w:hyperlink
│   │   │   ├── Table.cs                    # w:tbl, w:tr, w:tc
│   │   │   ├── Properties/
│   │   │   │   ├── ParagraphProperties.cs  # w:pPr — alignment, indent, spacing, numbering
│   │   │   │   ├── RunProperties.cs        # w:rPr — bold, italic, font, size, color
│   │   │   │   ├── TableProperties.cs      # w:tblPr
│   │   │   │   ├── TableCellProperties.cs  # w:tcPr
│   │   │   │   └── DocumentProperties.cs   # Page size, margins, sections
│   │   │   ├── Enums/
│   │   │   │   ├── Alignment.cs            # left, center, right, both
│   │   │   │   ├── UnderlineType.cs
│   │   │   │   ├── HighlightColor.cs
│   │   │   │   └── BreakType.cs
│   │   │   └── Interfaces/
│   │   │       ├── IBlockNode.cs           # Paragraph | Table
│   │   │       ├── IInlineNode.cs          # Run | Hyperlink
│   │   │       └── IDocNode.cs             # Base: Id, Type
│   │   │
│   │   ├── Commands/
│   │   │   ├── CommandExecutor.cs          # Dispatches commands, pushes history
│   │   │   ├── InsertTextCommand.cs
│   │   │   ├── DeleteBackwardCommand.cs
│   │   │   ├── DeleteForwardCommand.cs
│   │   │   ├── SplitParagraphCommand.cs
│   │   │   ├── ToggleFormatCommand.cs      # Bold, italic, underline, strikethrough
│   │   │   ├── SetParagraphStyleCommand.cs # Heading1-6, Normal
│   │   │   ├── SetAlignmentCommand.cs
│   │   │   ├── ToggleListCommand.cs        # Bullet/numbered, indent level
│   │   │   ├── InsertTableCommand.cs
│   │   │   ├── InsertHyperlinkCommand.cs
│   │   │   ├── DeleteSelectionCommand.cs   # Range delete
│   │   │   ├── PasteTextCommand.cs
│   │   │   └── SetIndentCommand.cs         # Left indent, first-line indent
│   │   │
│   │   ├── Selection/
│   │   │   ├── SelectionModel.cs           # Anchor + Focus positions
│   │   │   ├── ModelPosition.cs            # BlockIndex, InlineIndex, Offset
│   │   │   └── SelectionHelper.cs          # Normalize, expand, collapse
│   │   │
│   │   ├── History/
│   │   │   ├── HistoryManager.cs           # Undo/redo stack
│   │   │   ├── HistoryEntry.cs             # Snapshot: doc clone + cursor position
│   │   │   └── DocumentCloner.cs           # Deep clone the model
│   │   │
│   │   ├── Serialization/
│   │   │   ├── DocxExporter.cs             # Model → .docx bytes (via Open XML SDK)
│   │   │   ├── DocxImporter.cs             # .docx bytes → Model (via Open XML SDK)
│   │   │   ├── StylesBuilder.cs            # Generate styles.xml part
│   │   │   ├── NumberingBuilder.cs          # Generate numbering.xml part
│   │   │   └── RelationshipsBuilder.cs     # Manage hyperlink/image relationships
│   │   │
│   │   ├── RenderTree/
│   │   │   ├── RenderNode.cs               # Lightweight node for JSON transfer to TS
│   │   │   ├── RenderTreeBuilder.cs        # Model → RenderTree (what TS needs to render DOM)
│   │   │   └── RenderDiff.cs               # (Future) diff previous tree vs new tree
│   │   │
│   │   └── Interop/
│   │       ├── EditorEngine.cs             # Public API surface exposed to JS
│   │       └── InteropContracts.cs         # DTOs for JS ↔ C# communication
│   │
│   ├── Document Editor.Wasm/                    # Blazor WASM host project
│   │   ├── Document Editor.Wasm.csproj
│   │   ├── Program.cs                      # WASM entry point, registers JS interop
│   │   └── wwwroot/
│   │       └── index.html                  # Minimal shell, loads the TS app
│   │
│   └── Document Editor.Frontend/               # TypeScript frontend
│       ├── package.json
│       ├── tsconfig.json
│       ├── vite.config.ts
│       ├── src/
│       │   ├── index.ts                    # Entry point, initializes editor
│       │   ├── bridge/
│       │   │   ├── engine-bridge.ts        # JS ↔ C# WASM interop wrapper
│       │   │   └── types.ts                # TypeScript types matching C# InteropContracts
│       │   ├── renderer/
│       │   │   ├── dom-renderer.ts         # RenderTree → DOM elements
│       │   │   ├── cursor-manager.ts       # DOM Selection ↔ Model position mapping
│       │   │   └── page-layout.ts          # Multi-page pagination, page frames
│       │   ├── ui/
│       │   │   ├── toolbar.ts              # Toolbar buttons and dropdowns
│       │   │   ├── sidebar.ts              # Sidebar panels (outline, stats, xml debug)
│       │   │   ├── ruler-h.ts              # Horizontal ruler with indent markers
│       │   │   ├── ruler-v.ts              # Vertical ruler
│       │   │   ├── status-bar.ts           # Footer status bar
│       │   │   └── zoom.ts                 # Zoom controls
│       │   ├── input/
│       │   │   ├── input-handler.ts        # beforeinput event → command dispatch
│       │   │   ├── keyboard-shortcuts.ts   # Ctrl+B, Ctrl+I, etc.
│       │   │   └── paste-handler.ts        # Paste plain text / rich text
│       │   ├── plugins/                    # Optional extensibility
│       │   │   ├── plugin-interface.ts
│       │   │   ├── find-replace.ts
│       │   │   └── word-count.ts
│       │   └── styles/
│       │       ├── editor.css              # Main editor styles
│       │       ├── toolbar.css
│       │       ├── pages.css               # Multi-page layout
│       │       └── rulers.css
│       └── public/
│           └── fonts/                      # Optional custom fonts
│
├── tests/
│   ├── Document Editor.Engine.Tests/
│   │   ├── Model/                          # Unit tests for model operations
│   │   ├── Commands/                       # Test each command
│   │   ├── Serialization/                  # Round-trip: model → docx → model
│   │   └── History/                        # Undo/redo tests
│   └── Document Editor.Frontend.Tests/          # (Optional) Playwright E2E
│
└── docs/
    ├── architecture.md
    ├── ooxml-mapping.md                    # Model property ↔ OOXML element reference
    └── interop-api.md                      # JS ↔ C# API contract
```

---

## Implementation Phases

| Phase | Title | Key Deliverable | Doc |
|-------|-------|----------------|-----|
| 1 | C# Document Model | IDocNode types, OOXML property mapping, DocFactory | [docs/phase1-document-model.md](docs/phase1-document-model.md) |
| 2 | Commands | 13 command classes, CommandExecutor, HistoryManager | [docs/phase2-commands.md](docs/phase2-commands.md) |
| 3 | Serialization | DocxExporter, DocxImporter via Open XML SDK | [docs/phase3-serialization.md](docs/phase3-serialization.md) |
| 4 | JS Interop | EditorEngine.cs, RenderTree, TypeScript EngineBridge | [docs/phase4-interop.md](docs/phase4-interop.md) |
| 5 | Frontend & File I/O | DOM renderer, input handler, cursor, toolbar, .docx export/import | [docs/phase5-frontend.md](docs/phase5-frontend.md) |
| 6 | Testing | Unit tests, OpenXML validation, manual checklist | [docs/phase6-testing.md](docs/phase6-testing.md) |

---

## Build & Run Commands

```bash
# Initial setup
dotnet new sln -n Document Editor
dotnet new classlib -n Document Editor.Engine -o src/Document Editor.Engine
dotnet new blazorwasm -n Document Editor.Wasm -o src/Document Editor.Wasm --empty
dotnet sln add src/Document Editor.Engine src/Document Editor.Wasm
dotnet add src/Document Editor.Wasm reference src/Document Editor.Engine
dotnet add src/Document Editor.Engine package DocumentFormat.OpenXml

# Frontend
cd src/Document Editor.Frontend
npm init -y
npm install typescript vite --save-dev

# Run
cd src/Document Editor.Wasm
dotnet run

# Test
dotnet test tests/Document Editor.Engine.Tests
```

---

## Key Constraints & Decisions

1. **Never use `document.execCommand()`** — all formatting goes through the C# engine
2. **Never let the browser modify the DOM** — always `e.preventDefault()` in `beforeinput`
3. **The model is the single source of truth** — after every command, re-render from the model
4. **RenderTree is the ONLY data that crosses the C# → TS boundary** — TypeScript never sees the raw model
5. **All .docx I/O uses the Open XML SDK** — no manual XML string building for export (the serializer we built in the prototype was for learning; production uses the SDK)
6. **History is model snapshots** — deep clone the document before each command, push to undo stack
7. **IDs are stable** — each model node has a unique ID that persists across re-renders, enabling cursor restoration
8. **Normalize after mutations** — after commands, merge adjacent runs with identical properties, remove empty runs (except one per paragraph)
9. **WASM binary size** — the Open XML SDK adds ~2-3MB to the WASM payload. Use trimming and lazy loading to minimize initial load. Consider loading the engine lazily after the UI shell renders.
10. **Performance** — for documents under 100 pages, full re-render on every keystroke is fine. For larger documents, implement differential rendering using `RenderDiff` (future optimization).

---

## What This Plan Does NOT Cover (Future Work)

- Images and embedded objects
- Headers and footers
- Page numbers in the document (via fields)
- Track changes / comments
- Styles panel (managing custom styles)
- Find and replace
- Print preview
- Collaborative editing
- Spell check integration
- Right-to-left text
- Footnotes and endnotes
- Table of contents generation
- Section breaks with different page layouts
