using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.RenderTree;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>
/// Per-section page geometry. Used on ParagraphProperties.SectionBreak for mid-document
/// section breaks, and on DocumentProperties for the final section.
/// Maps to w:sectPr.
/// </summary>
public class SectionProperties
{
    public SectionBreakType BreakType { get; set; } = SectionBreakType.NextPage;
    public Orientation Orientation { get; set; } = Orientation.Portrait;

    // Page dimensions in twips (US Letter defaults)
    public int PageWidth { get; set; } = 12240;    // 8.5"
    public int PageHeight { get; set; } = 15840;   // 11"

    // Margins in twips (1" defaults)
    public int MarginTop { get; set; } = 1440;
    public int MarginBottom { get; set; } = 1440;
    public int MarginLeft { get; set; } = 1440;
    public int MarginRight { get; set; } = 1440;

    // Distance from page edge to header/footer content (twips, default 720 = 0.5")
    public int HeaderDistance { get; set; } = 720;
    public int FooterDistance { get; set; } = 720;

    // Column layout (w:cols)
    public int ColumnCount { get; set; } = 1;       // w:cols @w:num
    public int ColumnSpacing { get; set; } = 720;    // w:cols @w:space (twips)
    public bool ColumnSeparator { get; set; }         // w:cols @w:sep

    // Whether section has different first-page header/footer (w:titlePg)
    public bool TitlePage { get; set; }

    // Pre-rendered header/footer content per type: "default", "first", "even"
    public Dictionary<string, List<RenderNode>>? Headers { get; set; }
    public Dictionary<string, List<RenderNode>>? Footers { get; set; }

    // Raw XML bytes for lossless round-trip export (not serialized to JSON)
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, byte[]>? HeaderFooterParts { get; set; }

    public SectionProperties DeepClone() => new()
    {
        BreakType = BreakType,
        Orientation = Orientation,
        PageWidth = PageWidth,
        PageHeight = PageHeight,
        MarginTop = MarginTop,
        MarginBottom = MarginBottom,
        MarginLeft = MarginLeft,
        MarginRight = MarginRight,
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
