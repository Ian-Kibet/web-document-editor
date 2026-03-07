using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:tcPr</summary>
public class TableCellProperties
{
    public int? Width { get; set; }                                     // w:tcW
    public TableVerticalAlignment? VerticalAlignment { get; set; }      // w:vAlign
    public int? GridSpan { get; set; }                                  // w:gridSpan
    public VerticalMergeType? VerticalMerge { get; set; }               // w:vMerge
    public string? Shading { get; set; }                                // w:shd @w:fill (hex color)
    public CellBorders? Borders { get; set; }                           // w:tcBorders
    public CellPadding? Padding { get; set; }                           // w:tcMar

    public TableCellProperties DeepClone() => new()
    {
        Width = Width,
        VerticalAlignment = VerticalAlignment,
        GridSpan = GridSpan,
        VerticalMerge = VerticalMerge,
        Shading = Shading,
        Borders = Borders?.DeepClone(),
        Padding = Padding?.DeepClone(),
    };
}
