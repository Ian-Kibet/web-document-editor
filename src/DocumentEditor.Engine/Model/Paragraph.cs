using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

/// <summary>Maps to w:p — invariant: always has at least one Run</summary>
public class Paragraph : IBlockNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "paragraph";
    public List<IInlineNode> Children { get; set; } = [new Run()];
    public ParagraphProperties Properties { get; set; } = new();

    public Paragraph DeepClone() => new()
    {
        Id = Id,
        Properties = Properties.DeepClone(),
        Children = Children.Select(inline => inline switch
        {
            Run r => (IInlineNode)r.DeepClone(),
            Hyperlink h => h.DeepClone(),
            _ => throw new InvalidOperationException($"Unknown IInlineNode: {inline.GetType()}")
        }).ToList(),
    };
}
