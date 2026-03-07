using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.RenderTree;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:sectPr — page layout properties for the final section</summary>
public class DocumentProperties
{
    // US Letter defaults in twips (1 inch = 1440 twips)
    public int PageWidth { get; set; } = 12240;     // 8.5"
    public int PageHeight { get; set; } = 15840;    // 11"
    public int MarginTop { get; set; } = 1440;      // 1"
    public int MarginBottom { get; set; } = 1440;
    public int MarginLeft { get; set; } = 1440;
    public int MarginRight { get; set; } = 1440;
    public Orientation Orientation { get; set; } = Orientation.Portrait;

    // Distance from page edge to header/footer content (twips, default 720 = 0.5")
    public int HeaderDistance { get; set; } = 720;
    public int FooterDistance { get; set; } = 720;

    // Column layout (w:cols)
    public int ColumnCount { get; set; } = 1;
    public int ColumnSpacing { get; set; } = 720;
    public bool ColumnSeparator { get; set; }

    // Whether section has different first-page header/footer (w:titlePg)
    public bool TitlePage { get; set; }

    // Pre-rendered header/footer content per type: "default", "first", "even"
    public Dictionary<string, List<RenderNode>>? Headers { get; set; }
    public Dictionary<string, List<RenderNode>>? Footers { get; set; }

    // Raw XML bytes for lossless round-trip export (not serialized to JSON)
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, byte[]>? HeaderFooterParts { get; set; }

    public DocumentProperties DeepClone() => new()
    {
        PageWidth = PageWidth,
        PageHeight = PageHeight,
        MarginTop = MarginTop,
        MarginBottom = MarginBottom,
        MarginLeft = MarginLeft,
        MarginRight = MarginRight,
        Orientation = Orientation,
        HeaderDistance = HeaderDistance,
        FooterDistance = FooterDistance,
        ColumnCount = ColumnCount,
        ColumnSpacing = ColumnSpacing,
        ColumnSeparator = ColumnSeparator,
        TitlePage = TitlePage,
        Headers = Headers,              // reference copy — RenderNodes immutable after construction
        Footers = Footers,              // reference copy — RenderNodes immutable after construction
        HeaderFooterParts = HeaderFooterParts, // reference copy — [JsonIgnore], never mutated
    };
}
