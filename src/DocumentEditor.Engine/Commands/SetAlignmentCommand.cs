using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class SetAlignmentCommand : ICommand
{
    private readonly Alignment _alignment;

    public SetAlignmentCommand(Alignment alignment)
    {
        _alignment = alignment;
    }

    public EditorState Execute(EditorState state)
    {
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(state.Selection);
        var doc = state.Document;

        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is Paragraph para)
            {
                para.Properties.Alignment = _alignment;
            }
        }

        return state;
    }
}
