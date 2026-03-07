namespace DocumentEditor.Engine.Model.Properties;

/// <summary>Maps to w:trPr</summary>
public class TableRowProperties
{
    public int? Height { get; set; }            // w:trHeight
    public bool IsHeader { get; set; }          // w:tblHeader

    public TableRowProperties DeepClone() => new()
    {
        Height = Height,
        IsHeader = IsHeader,
    };
}
