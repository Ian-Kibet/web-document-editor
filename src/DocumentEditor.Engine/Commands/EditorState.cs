using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class EditorState
{
    public DocxDocument Document { get; set; } = new();
    public SelectionModel Selection { get; set; } = new();
}
