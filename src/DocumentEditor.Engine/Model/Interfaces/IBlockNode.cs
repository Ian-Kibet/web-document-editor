using System.Text.Json.Serialization;

namespace DocumentEditor.Engine.Model.Interfaces;

[JsonDerivedType(typeof(Paragraph), "paragraph")]
[JsonDerivedType(typeof(Table), "table")]
public interface IBlockNode : IDocNode { }
