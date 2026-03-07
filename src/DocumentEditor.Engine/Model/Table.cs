using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

/// <summary>Maps to w:tbl</summary>
public class Table : IBlockNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "table";
    public List<TableRow> Rows { get; set; } = [];
    public List<int> GridColumnWidths { get; set; } = [];
    public TableProperties Properties { get; set; } = new();

    public Table DeepClone() => new()
    {
        Id = Id,
        Properties = Properties.DeepClone(),
        GridColumnWidths = new List<int>(GridColumnWidths),
        Rows = Rows.Select(r => r.DeepClone()).ToList(),
    };
}

/// <summary>Maps to w:tr</summary>
public class TableRow : IDocNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "tableRow";
    public List<TableCell> Cells { get; set; } = [];
    public TableRowProperties Properties { get; set; } = new();

    public TableRow DeepClone() => new()
    {
        Id = Id,
        Properties = Properties.DeepClone(),
        Cells = Cells.Select(c => c.DeepClone()).ToList(),
    };
}

/// <summary>Maps to w:tc — contains block-level content</summary>
public class TableCell : IDocNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "tableCell";
    public List<IBlockNode> Children { get; set; } = [];
    public TableCellProperties Properties { get; set; } = new();

    public TableCell DeepClone() => new()
    {
        Id = Id,
        Properties = Properties.DeepClone(),
        Children = Children.Select(b => b switch
        {
            Paragraph p => (IBlockNode)p.DeepClone(),
            Table t => t.DeepClone(),
            _ => throw new InvalidOperationException($"Unknown IBlockNode: {b.GetType()}")
        }).ToList(),
    };
}
