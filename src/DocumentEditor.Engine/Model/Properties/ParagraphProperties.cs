using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:pPr</summary>
public class ParagraphProperties
{
    public string? Style { get; set; }                  // w:pStyle
    public Alignment? Alignment { get; set; }           // w:jc
    public int? IndentLeft { get; set; }                // w:ind @w:left (twips)
    public int? IndentFirstLine { get; set; }           // w:ind @w:firstLine (twips)
    public int? IndentHanging { get; set; }             // w:ind @w:hanging (twips)
    public int? NumberingId { get; set; }               // w:numPr/w:numId
    public int? NumberingLevel { get; set; }            // w:numPr/w:ilvl (0-8)
    public string? NumberingFormat { get; set; }        // resolved numFmt: "bullet" | "decimal" | "lowerLetter" | ...
    public int? SpaceBefore { get; set; }               // w:spacing @w:before (twips)
    public int? SpaceAfter { get; set; }                // w:spacing @w:after (twips)
    public int? LineSpacing { get; set; }               // w:spacing @w:line (240ths of a line)
    public string? LineSpacingRule { get; set; }        // w:spacing @w:lineRule: "auto" | "exact" | "atleast"
    public bool KeepNext { get; set; }                  // w:keepNext
    public bool PageBreakBefore { get; set; }           // w:pageBreakBefore
    public bool? ContextualSpacing { get; set; }        // w:contextualSpacing

    // Effective run properties resolved from the paragraph's named style definition.
    // Null means "not defined by the style" — CSS defaults apply.
    public bool? StyleBold { get; set; }
    public bool? StyleItalic { get; set; }
    public int? StyleFontSize { get; set; }       // half-points (same units as RunProperties.FontSize)
    public string? StyleFontFamily { get; set; }  // e.g. "Calibri"
    public string? StyleColor { get; set; }       // hex without '#', e.g. "2F5496"

    /// <summary>
    /// When non-null, this paragraph is the last paragraph of a section.
    /// Matches OOXML structure where w:sectPr lives inside w:pPr.
    /// </summary>
    public SectionProperties? SectionBreak { get; set; }

    public ParagraphProperties DeepClone() => new()
    {
        Style = Style,
        Alignment = Alignment,
        IndentLeft = IndentLeft,
        IndentFirstLine = IndentFirstLine,
        IndentHanging = IndentHanging,
        NumberingId = NumberingId,
        NumberingLevel = NumberingLevel,
        NumberingFormat = NumberingFormat,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        LineSpacing = LineSpacing,
        LineSpacingRule = LineSpacingRule,
        KeepNext = KeepNext,
        PageBreakBefore = PageBreakBefore,
        ContextualSpacing = ContextualSpacing,
        StyleBold = StyleBold,
        StyleItalic = StyleItalic,
        StyleFontSize = StyleFontSize,
        StyleFontFamily = StyleFontFamily,
        StyleColor = StyleColor,
        SectionBreak = SectionBreak?.DeepClone(),
    };
}
