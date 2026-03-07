using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class InsertTableCommand : ICommand
{
    private readonly int _rows;
    private readonly int _cols;

    public InsertTableCommand(int rows, int cols)
    {
        _rows = Math.Max(1, rows);
        _cols = Math.Max(1, cols);
    }

    public EditorState Execute(EditorState state)
    {
        var doc = state.Document;
        var pos = state.Selection.Anchor;

        var insertIdx = pos.BlockIndex + 1;

        // Insert table after current block
        var table = DocFactory.CreateTable(_rows, _cols);
        doc.Children.Insert(insertIdx, table);

        // Insert empty paragraph after table
        var emptyPara = DocFactory.CreateParagraph();
        doc.Children.Insert(insertIdx + 1, emptyPara);

        // Cursor at the post-table paragraph
        state.Selection = SelectionModel.Collapsed(insertIdx + 1, 0, 0);
        return state;
    }
}
