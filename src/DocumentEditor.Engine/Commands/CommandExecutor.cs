using DocumentEditor.Engine.History;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class CommandExecutor
{
    private readonly HistoryManager _history;

    public CommandExecutor() : this(new HistoryManager()) { }

    public CommandExecutor(HistoryManager history)
    {
        _history = history;
    }

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    public EditorState Execute(ICommand command, EditorState currentState)
    {
        _history.Push(currentState);
        return command.Execute(currentState);
    }

    public EditorState Undo(EditorState currentState) => _history.Undo(currentState);
    public EditorState Redo(EditorState currentState) => _history.Redo(currentState);

    /// <summary>
    /// Resolves the paragraph at the given position, handling both top-level and table-cell paths.
    /// Returns null if the position is invalid or doesn't point to a paragraph.
    /// </summary>
    public static Paragraph? ResolveParagraph(DocxDocument doc, ModelPosition pos)
    {
        if (pos.Cell is not null)
        {
            if (pos.BlockIndex < 0 || pos.BlockIndex >= doc.Children.Count) return null;
            if (doc.Children[pos.BlockIndex] is not Table table) return null;
            if (pos.Cell.RowIndex >= table.Rows.Count) return null;
            var row = table.Rows[pos.Cell.RowIndex];
            if (pos.Cell.CellIndex >= row.Cells.Count) return null;
            var cell = row.Cells[pos.Cell.CellIndex];
            if (pos.Cell.CellBlockIndex >= cell.Children.Count) return null;
            return cell.Children[pos.Cell.CellBlockIndex] as Paragraph;
        }
        if (pos.BlockIndex < 0 || pos.BlockIndex >= doc.Children.Count) return null;
        return doc.Children[pos.BlockIndex] as Paragraph;
    }

    /// <summary>
    /// Resolves the child list that contains the paragraph at pos.
    /// For table-cell positions, returns the cell's Children list.
    /// For top-level positions, returns doc.Children.
    /// </summary>
    public static List<IBlockNode> ResolveChildList(DocxDocument doc, ModelPosition pos)
    {
        if (pos.Cell is not null)
        {
            if (pos.BlockIndex >= 0 && pos.BlockIndex < doc.Children.Count
                && doc.Children[pos.BlockIndex] is Table table)
            {
                var row = pos.Cell.RowIndex < table.Rows.Count ? table.Rows[pos.Cell.RowIndex] : null;
                var cell = row is not null && pos.Cell.CellIndex < row.Cells.Count
                    ? row.Cells[pos.Cell.CellIndex]
                    : null;
                if (cell is not null) return cell.Children;
            }
        }
        return doc.Children;
    }
}
