using System.Text.Json.Serialization;

namespace DocumentEditor.Engine.Model.Interfaces;

[JsonDerivedType(typeof(Run), "run")]
[JsonDerivedType(typeof(Hyperlink), "hyperlink")]
public interface IInlineNode : IDocNode { }
