using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class SetParagraphStyleCommand : ICommand
{
    private readonly string? _style;

    public SetParagraphStyleCommand(string style)
    {
        // "Normal" maps to null
        _style = string.Equals(style, "Normal", StringComparison.OrdinalIgnoreCase) ? null : style;
    }

    public EditorState Execute(EditorState state)
    {
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(state.Selection);
        var doc = state.Document;

        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is Paragraph para)
            {
                para.Properties.Style = _style;
            }
        }

        return state;
    }
}
