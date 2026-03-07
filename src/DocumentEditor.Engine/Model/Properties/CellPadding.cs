namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Resolved inner cell padding in twips (from w:tcMar or w:tblCellMar).</summary>
public sealed class CellPadding
{
    public int Top    { get; set; }
    public int Bottom { get; set; }
    public int Left   { get; set; }
    public int Right  { get; set; }

    public CellPadding DeepClone() => new() { Top = Top, Bottom = Bottom, Left = Left, Right = Right };
}
