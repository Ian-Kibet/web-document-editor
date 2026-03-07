using System.Text.Json.Serialization;

namespace DocumentEditor.Engine.RenderTree;

/// <summary>
/// Lightweight node for JSON transfer to TypeScript.
/// The frontend walks this tree to create DOM elements.
/// </summary>
public class RenderNode
{
    /// <summary>Model node ID — used as data-node-id on DOM elements</summary>
    public string Id { get; set; } = "";

    /// <summary>HTML tag: "p", "h1"-"h4", "span", "a", "table", "tr", "td", "br", "tab"</summary>
    public string Tag { get; set; } = "";

    /// <summary>CSS inline styles (e.g. "font-weight" → "bold")</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Styles { get; set; }

    /// <summary>HTML attributes (e.g. "href" → "https://...")</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Attrs { get; set; }

    /// <summary>Text content for leaf nodes (span text, tab, etc.)</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>Child render nodes</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderNode>? Children { get; set; }
}
