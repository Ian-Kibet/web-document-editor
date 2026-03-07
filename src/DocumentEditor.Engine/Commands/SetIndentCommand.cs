using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class SetIndentCommand : ICommand
{
    private readonly int _leftDelta;
    private readonly int _firstLineDelta;

    public SetIndentCommand(int leftDelta, int firstLineDelta = 0)
    {
        _leftDelta = leftDelta;
        _firstLineDelta = firstLineDelta;
    }

    public EditorState Execute(EditorState state)
    {
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(state.Selection);
        var doc = state.Document;

        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is not Paragraph para) continue;

            var newLeft = Math.Max(0, (para.Properties.IndentLeft ?? 0) + _leftDelta);
            para.Properties.IndentLeft = newLeft == 0 ? null : newLeft;

            var newFirstLine = Math.Max(0, (para.Properties.IndentFirstLine ?? 0) + _firstLineDelta);
            para.Properties.IndentFirstLine = newFirstLine == 0 ? null : newFirstLine;
        }

        return state;
    }
}
