namespace DocumentEditor.Engine.Commands;

public class SetTableCellShadingCommand : ICommand
{
    private readonly string _cellId;
    private readonly string? _hexColor;

    public SetTableCellShadingCommand(string cellId, string? hexColor)
    {
        _cellId = cellId;
        // Strip '#' prefix if present
        _hexColor = hexColor?.TrimStart('#');
    }

    public EditorState Execute(EditorState state)
    {
        var cell = TableCellFinder.Find(state.Document, _cellId);
        if (cell is null) return state;
        cell.Properties.Shading = _hexColor;
        return state;
    }
}
