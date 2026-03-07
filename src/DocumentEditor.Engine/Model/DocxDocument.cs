using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

/// <summary>Root document node — maps to w:document</summary>
public class DocxDocument : IDocNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "document";
    public List<IBlockNode> Children { get; set; } = [];
    public DocumentProperties Properties { get; set; } = new();

    public DocxDocument DeepClone() => new()
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
