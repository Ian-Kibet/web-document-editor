using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.History;

public class HistoryEntry
{
    public DocxDocument Document { get; set; } = new();
    public SelectionModel Selection { get; set; } = new();
}
