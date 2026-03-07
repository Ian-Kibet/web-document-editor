using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Commands;

public class SetTableCellBordersCommand : ICommand
{
    private readonly string _cellId;
    private readonly CellBorders? _borders;

    public SetTableCellBordersCommand(string cellId, CellBorders? borders)
    {
        _cellId = cellId;
        _borders = borders;
    }

    public EditorState Execute(EditorState state)
    {
        var cell = TableCellFinder.Find(state.Document, _cellId);
        if (cell is null) return state;
        cell.Properties.Borders = _borders;
        return state;
    }
}
