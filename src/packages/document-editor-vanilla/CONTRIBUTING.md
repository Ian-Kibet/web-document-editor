# Contributing to document-editor-vanilla

## Prerequisites

- **Node.js 20+**
- **.NET 10 SDK**
- **WASM tools workload**: `dotnet workload install wasm-tools`

## Local Development

Start the .NET WASM engine dev server (port 5000):

```bash
cd src/DocumentEditor.Wasm
dotnet run
```

In a separate terminal, start the Vite dev server (port 5173):

```bash
cd src/packages/document-editor-vanilla
npm install
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/_framework` to the .NET server.

## Architecture

```
Browser
  └── app-entry.ts          → calls mountEditor()
        └── index.ts         → mountEditor(): wires DOM, engine, UI
              ├── bridge/engine-bridge.ts  → JS ↔ C# WASM interop
              ├── renderer/dom-renderer.ts → RenderTree JSON → DOM
              ├── renderer/cursor-manager.ts → DOM ↔ model position mapping
              ├── input/input-handler.ts   → beforeinput → engine commands
              ├── ui/toolbar.ts            → config-driven toolbar
              └── ui/sidebar.ts            → outline + stats panel

C# WASM Engine (DocumentEditor.Engine)
  ├── Model/          → OOXML document model (Paragraph, Run, Table, …)
  ├── Commands/       → Immutable commands mutate the model
  ├── History/        → Undo/redo via document snapshots
  ├── Serialization/  → DocxImporter / DocxExporter via Open XML SDK
  ├── RenderTree/     → Model → lightweight JSON for TypeScript renderer
  └── Interop/        → EditorEngine.cs: public JS-callable API
```

**Key constraint:** The DOM is never the source of truth. Every edit goes through the C# engine, which returns a `RenderTree` that the TypeScript renderer uses to update the DOM.

## Adding a toolbar action

1. Add an entry to `TOOLBAR_ACTIONS` in `src/ui/toolbar-registry.ts`:

```ts
myAction: async ({ engine, canvas, onResponse }) => {
  const sel = domToModelSelection(canvas);
  if (!sel) return;
  const response = await engine.myEngineMethod(sel);
  onResponse(response);
},
```

2. Add the button config to the appropriate preset in `src/ui/toolbar-presets.ts`.

## Adding a C# command

1. Create `src/DocumentEditor.Engine/Commands/MyCommand.cs`:

```csharp
namespace DocumentEditor.Engine.Commands;

public class MyCommand : ICommand
{
    public EngineResponse Execute(WaveDocument doc, SelectionModel sel)
    {
        // mutate doc ...
        return EngineResponse.Ok(doc, sel);
    }
}
```

2. Add dispatch in `CommandExecutor.cs`.
3. Expose via `EditorEngine.cs` interop method.
4. Add corresponding TypeScript method to `engine-bridge.ts`.

## Changeset required before PR

Before opening a pull request, add a changeset:

```bash
npx changeset
```

Select the affected packages, choose the semver bump type, and write a short description. Commit the generated `.changeset/*.md` file with your changes.

## Running tests

```bash
# C# unit tests
dotnet test tests/DocumentEditor.Engine.Tests

# TypeScript type check
cd src/packages/document-editor-vanilla
npx tsc --noEmit
```
