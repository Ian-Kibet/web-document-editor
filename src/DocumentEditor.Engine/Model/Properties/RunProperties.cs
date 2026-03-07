using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:rPr</summary>
public class RunProperties
{
    public bool Bold { get; set; }                              // w:b
    public bool Italic { get; set; }                            // w:i
    public UnderlineType? Underline { get; set; }               // w:u @w:val
    public bool Strikethrough { get; set; }                     // w:strike
    public string? FontFamily { get; set; }                     // w:rFonts @w:ascii
    public int? FontSize { get; set; }                          // w:sz @w:val (half-points: 24 = 12pt)
    public string? Color { get; set; }                          // w:color @w:val (hex without #)
    public HighlightColor? Highlight { get; set; }              // w:highlight @w:val
    public VerticalAlignType? VerticalAlign { get; set; }       // w:vertAlign @w:val
    public int? CharacterSpacing { get; set; }                  // w:spacing @w:val (twentieths of a point)

    /// <summary>
    /// Value equality for run merging (Phase 2). Named method to avoid overriding Equals.
    /// </summary>
    public bool ValueEquals(RunProperties? other)
    {
        if (other is null) return false;
        return Bold == other.Bold
            && Italic == other.Italic
            && Underline == other.Underline
            && Strikethrough == other.Strikethrough
            && FontFamily == other.FontFamily
            && FontSize == other.FontSize
            && Color == other.Color
            && Highlight == other.Highlight
            && VerticalAlign == other.VerticalAlign
            && CharacterSpacing == other.CharacterSpacing;
    }

    public RunProperties DeepClone() => new()
    {
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        FontFamily = FontFamily,
        FontSize = FontSize,
        Color = Color,
        Highlight = Highlight,
        VerticalAlign = VerticalAlign,
        CharacterSpacing = CharacterSpacing,
    };
}
