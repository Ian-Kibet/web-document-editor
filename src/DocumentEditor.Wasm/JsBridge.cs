using Microsoft.JSInterop;
using DocumentEditor.Engine.Interop;

namespace DocumentEditor.Wasm;

/// <summary>
/// Thin JS interop wrapper around EditorEngine.
/// Each method is [JSInvokable] so TypeScript can call it via DotNet.invokeMethodAsync.
/// The Engine itself has no Blazor dependency — this class bridges the gap.
/// </summary>
public class JsBridge
{
    private readonly EditorEngine _engine;

    public JsBridge(EditorEngine engine)
    {
        _engine = engine;
    }

    // ─── Lifecycle ───────────────────────────────────────────────

    [JSInvokable]
    public string Initialize(string? initialDocJson = null)
        => _engine.Initialize(initialDocJson);

    // ─── Text Editing ────────────────────────────────────────────

    [JSInvokable]
    public string InsertText(string text, string selectionJson)
        => _engine.InsertText(text, selectionJson);

    [JSInvokable]
    public string DeleteBackward(string selectionJson)
        => _engine.DeleteBackward(selectionJson);

    [JSInvokable]
    public string DeleteForward(string selectionJson)
        => _engine.DeleteForward(selectionJson);

    [JSInvokable]
    public string SplitParagraph(string selectionJson)
        => _engine.SplitParagraph(selectionJson);

    [JSInvokable]
    public string DeleteSelection(string selectionJson)
        => _engine.DeleteSelection(selectionJson);

    [JSInvokable]
    public string PasteText(string text, string selectionJson)
        => _engine.PasteText(text, selectionJson);

    // ─── Formatting ──────────────────────────────────────────────

    [JSInvokable]
    public string ToggleFormat(string property, string selectionJson)
        => _engine.ToggleFormat(property, selectionJson);

    [JSInvokable]
    public string SetFontFamily(string? fontFamily, string selectionJson)
        => _engine.SetFontFamily(fontFamily, selectionJson);

    [JSInvokable]
    public string SetFontSize(double fontSizePt, string selectionJson)
        => _engine.SetFontSize(fontSizePt, selectionJson);

    [JSInvokable]
    public string SetParagraphStyle(string style, string selectionJson)
        => _engine.SetParagraphStyle(style, selectionJson);

    [JSInvokable]
    public string SetAlignment(string alignment, string selectionJson)
        => _engine.SetAlignment(alignment, selectionJson);

    [JSInvokable]
    public string ToggleList(string listType, string selectionJson)
        => _engine.ToggleList(listType, selectionJson);

    [JSInvokable]
    public string SetIndent(int leftDelta, int firstLineDelta, string selectionJson)
        => _engine.SetIndent(leftDelta, firstLineDelta, selectionJson);

    // ─── Insertions ──────────────────────────────────────────────

    [JSInvokable]
    public string InsertTable(int rows, int cols, string selectionJson)
        => _engine.InsertTable(rows, cols, selectionJson);

    [JSInvokable]
    public string InsertHyperlink(string url, string text, string selectionJson)
        => _engine.InsertHyperlink(url, text, selectionJson);

    [JSInvokable]
    public string SetTableCellBorders(string cellId, string? bordersJson, string selectionJson)
        => _engine.SetTableCellBorders(cellId, bordersJson, selectionJson);

    [JSInvokable]
    public string SetTableCellShading(string cellId, string? hexColor, string selectionJson)
        => _engine.SetTableCellShading(cellId, hexColor, selectionJson);

    [JSInvokable]
    public string InsertImage(string imageInfoJson, string selectionJson)
        => _engine.InsertImage(imageInfoJson, selectionJson);

    [JSInvokable]
    public string SetImageSize(string imageNodeId, long widthEmu, long heightEmu)
        => _engine.SetImageSize(imageNodeId, widthEmu, heightEmu);

    [JSInvokable]
    public string SetImageRotation(string imageNodeId, double degrees)
        => _engine.SetImageRotation(imageNodeId, degrees);

    [JSInvokable]
    public string SetImageWrapMode(string imageNodeId, string wrapMode)
        => _engine.SetImageWrapMode(imageNodeId, wrapMode);

    [JSInvokable]
    public string SetImagePosition(string imageNodeId, long horizontalOffsetEmu, long verticalOffsetEmu)
        => _engine.SetImagePosition(imageNodeId, horizontalOffsetEmu, verticalOffsetEmu);

    [JSInvokable]
    public string DeleteImageRun(string imageNodeId)
        => _engine.DeleteImageRun(imageNodeId);

    // ─── Sections ────────────────────────────────────────────────

    [JSInvokable]
    public string InsertSectionBreak(string breakType, string selectionJson)
        => _engine.InsertSectionBreak(breakType, selectionJson);

    [JSInvokable]
    public string RemoveSectionBreak(string selectionJson)
        => _engine.RemoveSectionBreak(selectionJson);

    [JSInvokable]
    public string SetPageOrientation(string orientation, string selectionJson)
        => _engine.SetPageOrientation(orientation, selectionJson);

    [JSInvokable]
    public string SetColumns(int columnCount, int spacing, string selectionJson)
        => _engine.SetColumns(columnCount, spacing, selectionJson);

    // ─── History ─────────────────────────────────────────────────

    [JSInvokable]
    public string Undo() => _engine.Undo();

    [JSInvokable]
    public string Redo() => _engine.Redo();

    // ─── File I/O ────────────────────────────────────────────────

    [JSInvokable]
    public byte[] ExportDocx() => _engine.ExportDocx();

    [JSInvokable]
    public string ImportDocx(byte[] docxBytes)
        => _engine.ImportDocx(docxBytes);

    // ─── Query ───────────────────────────────────────────────────

    [JSInvokable]
    public string GetFormatState(string selectionJson)
        => _engine.GetFormatState(selectionJson);
}
