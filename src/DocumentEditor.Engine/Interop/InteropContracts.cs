using DocumentEditor.Engine.RenderTree;

namespace DocumentEditor.Engine.Interop;


/// <summary>
/// Response sent from C# engine to TypeScript after every command.
/// Serialized as JSON across the interop boundary.
/// </summary>
public class EngineResponse
{
    public List<RenderNode> RenderTree { get; set; } = [];
    public SelectionResponse Selection { get; set; } = new();
    public FormatState FormatState { get; set; } = new();
    public List<SectionInfoResponse> Sections { get; set; } = [];
    public bool CanUndo { get; set; }
    public bool CanRedo { get; set; }
}

/// <summary>
/// Cursor/selection position for the TypeScript side to restore after re-render.
/// </summary>
public class SelectionResponse
{
    public PositionResponse Anchor { get; set; } = new();
    public PositionResponse Focus { get; set; } = new();
    public bool IsCollapsed { get; set; } = true;
}

public class CellPathResponse
{
    public int RowIndex { get; set; }
    public int CellIndex { get; set; }
    public int CellBlockIndex { get; set; }
}

public class PositionResponse
{
    public int BlockIndex { get; set; }
    public int InlineIndex { get; set; }
    public int Offset { get; set; }
    public CellPathResponse? Cell { get; set; }
}

/// <summary>
/// Section geometry for the TypeScript side to handle multi-section pagination.
/// All dimension values are in twips.
/// </summary>
public class SectionInfoResponse
{
    public int Index { get; set; }
    public int StartBlockIndex { get; set; }
    public int EndBlockIndex { get; set; }
    public int PageWidth { get; set; }
    public int PageHeight { get; set; }
    public string Orientation { get; set; } = "portrait";
    public int MarginTop { get; set; }
    public int MarginBottom { get; set; }
    public int MarginLeft { get; set; }
    public int MarginRight { get; set; }
    public string BreakType { get; set; } = "nextPage";
    public Dictionary<string, List<RenderNode>>? Headers { get; set; }
    public Dictionary<string, List<RenderNode>>? Footers { get; set; }
    public bool TitlePage { get; set; }
    public int HeaderDistance { get; set; } = 720;
    public int FooterDistance { get; set; } = 720;
    public int ColumnCount { get; set; } = 1;
    public int ColumnSpacing { get; set; } = 720;
    public bool ColumnSeparator { get; set; }
}

public class CellBordersInput
{
    public CellBorderInput? Top    { get; set; }
    public CellBorderInput? Bottom { get; set; }
    public CellBorderInput? Left   { get; set; }
    public CellBorderInput? Right  { get; set; }
}

public class CellBorderInput
{
    public string Style { get; set; } = "single";  // "none"|"single"|"double"|"dotted"|"dashed"|"thick"
    public int Size { get; set; } = 4;              // 1/8th points
    public string? Color { get; set; } = "auto";   // hex without '#'
}

public class ImageInsertInput
{
    /// <summary>Base64-encoded image bytes</summary>
    public string ImageData { get; set; } = "";
    /// <summary>MIME type, e.g. "image/png"</summary>
    public string ContentMimeType { get; set; } = "image/png";
    /// <summary>Width in EMU (914400 = 1 inch)</summary>
    public long WidthEmu { get; set; }
    /// <summary>Height in EMU</summary>
    public long HeightEmu { get; set; }
    public string? AltText { get; set; }
    /// <summary>"Inline", "FloatLeft", "FloatRight", etc.</summary>
    public string WrapMode { get; set; } = "Inline";
}

/// <summary>
/// Current formatting state at cursor — drives toolbar button active states.
/// </summary>
public class FormatState
{
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikethrough { get; set; }
    public string? FontFamily { get; set; }
    public double? FontSize { get; set; }
    public string? Color { get; set; }
    public string? ParagraphStyle { get; set; }
    public string? Alignment { get; set; }
    public string? ListType { get; set; }
}
