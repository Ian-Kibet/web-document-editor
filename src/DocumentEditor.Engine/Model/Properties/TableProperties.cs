using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:tblPr</summary>
public class TableProperties
{
    public int? Width { get; set; }                     // w:tblW
    public Alignment? Alignment { get; set; }           // w:jc
    public string? Style { get; set; }                  // w:tblStyle
    public int? IndentLeft { get; set; }                // w:tblInd
    public bool HasBorders { get; set; } = true;        // simplified border model
    public CellPadding? DefaultCellPadding { get; set; } // w:tblCellMar
    public int? CellSpacing { get; set; }                // w:tblCellSpacing (twips)

    public TableProperties DeepClone() => new()
    {
        Width = Width,
        Alignment = Alignment,
        Style = Style,
        IndentLeft = IndentLeft,
        HasBorders = HasBorders,
        DefaultCellPadding = DefaultCellPadding?.DeepClone(),
        CellSpacing = CellSpacing,
    };
}
