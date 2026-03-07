using System.Text.Json.Serialization;
using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model;

/// <summary>Base class for inline text content: w:t, w:tab, w:br</summary>
[JsonDerivedType(typeof(TextPiece), "text")]
[JsonDerivedType(typeof(TabContent), "tab")]
[JsonDerivedType(typeof(BreakContent), "break")]
[JsonDerivedType(typeof(ImageContent), "image")]
public abstract class TextContent
{
    public abstract string ContentType { get; }
}

/// <summary>Maps to w:t — literal text</summary>
public sealed class TextPiece : TextContent
{
    public override string ContentType => "text";
    public string Text { get; set; } = "";
    public TextPiece DeepClone() => new() { Text = Text };
}

/// <summary>Maps to w:tab</summary>
public sealed class TabContent : TextContent
{
    public override string ContentType => "tab";
    public TabContent DeepClone() => new();
}

/// <summary>Maps to w:br</summary>
public sealed class BreakContent : TextContent
{
    public override string ContentType => "break";
    public BreakType BreakType { get; set; } = BreakType.TextWrapping;
    public BreakContent DeepClone() => new() { BreakType = BreakType };
}
