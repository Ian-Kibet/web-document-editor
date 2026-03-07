using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class ToggleListCommand : ICommand
{
    private readonly ListType _listType;

    public ToggleListCommand(ListType listType)
    {
        _listType = listType;
    }

    public EditorState Execute(EditorState state)
    {
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(state.Selection);
        var doc = state.Document;

        // Convention: NumberingId 1 = bullet, 2 = numbered
        int? targetNumId = _listType switch
        {
            ListType.Bullet => 1,
            ListType.Numbered => 2,
            _ => null
        };

        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is not Paragraph para) continue;

            if (para.Properties.NumberingId == targetNumId)
            {
                // Already this list type → remove numbering
                para.Properties.NumberingId = null;
                para.Properties.NumberingLevel = null;
            }
            else
            {
                // Set numbering
                para.Properties.NumberingId = targetNumId;
                para.Properties.NumberingLevel ??= 0;
            }
        }

        return state;
    }
}
