using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Model;

/// <summary>Maps to w:hyperlink</summary>
public class Hyperlink : IInlineNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "hyperlink";
    public string Url { get; set; } = "";
    public string? Tooltip { get; set; }
    public List<Run> Children { get; set; } = [];

    public Hyperlink DeepClone() => new()
    {
        Id = Id,
        Url = Url,
        Tooltip = Tooltip,
        Children = Children.Select(r => r.DeepClone()).ToList(),
    };
}
