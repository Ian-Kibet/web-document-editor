using System.Text.Json.Serialization;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

/// <summary>Maps to w:r — a run of text with uniform formatting</summary>
public class Run : IInlineNode
{
    public string Id { get; set; } = IdGen.Next();
    public string NodeType => "run";
    public List<TextContent> Content { get; set; } = [];
    public RunProperties Properties { get; set; } = new();

    /// <summary>Set when this run is the cached result of a dynamic field (e.g. "PAGE", "NUMPAGES").</summary>
    public string? FieldType { get; set; }

    /// <summary>Convenience: gets/sets the text of the first TextPiece</summary>
    [JsonIgnore]
    public string Text
    {
        get
        {
            var piece = Content.OfType<TextPiece>().FirstOrDefault();
            return piece?.Text ?? "";
        }
        set
        {
            var piece = Content.OfType<TextPiece>().FirstOrDefault();
            if (piece is not null)
                piece.Text = value;
            else
                Content.Insert(0, new TextPiece { Text = value });
        }
    }

    public Run DeepClone() => new()
    {
        Id = Id,
        FieldType = FieldType,
        Properties = Properties.DeepClone(),
        Content = Content.Select(c => c switch
        {
            TextPiece tp => (TextContent)tp.DeepClone(),
            TabContent tab => tab.DeepClone(),
            BreakContent br => br.DeepClone(),
            ImageContent img => img.DeepClone(),
            _ => throw new InvalidOperationException($"Unknown TextContent: {c.GetType()}")
        }).ToList(),
    };
}
