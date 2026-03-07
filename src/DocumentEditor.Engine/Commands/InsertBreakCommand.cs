using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class InsertBreakCommand : ICommand
{
    private readonly BreakType _breakType;

    public InsertBreakCommand(BreakType breakType) => _breakType = breakType;

    public EditorState Execute(EditorState state)
    {
        // Delete selection first if not collapsed
        if (!state.Selection.IsCollapsed)
        {
            state = new DeleteSelectionCommand().Execute(state);
        }

        var doc = state.Document;
        var pos = state.Selection.Anchor;

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is null) return state;
        if (pos.InlineIndex < 0 || pos.InlineIndex >= para.Children.Count) return state;
        if (para.Children[pos.InlineIndex] is not Run targetRun) return state;

        var textContent = targetRun.Content.OfType<TextPiece>().FirstOrDefault();
        var text = textContent?.Text ?? "";

        var breakRun = new Run
        {
            Properties = targetRun.Properties.DeepClone(),
            Content = [new BreakContent { BreakType = _breakType }]
        };

        if (text.Length > 0 && pos.Offset > 0 && pos.Offset < text.Length)
        {
            // Split the run at the cursor offset
            var before = new Run
            {
                Id = targetRun.Id,
                Properties = targetRun.Properties.DeepClone(),
                Content = [new TextPiece { Text = text[..pos.Offset] }]
            };
            var after = new Run
            {
                Properties = targetRun.Properties.DeepClone(),
                Content = [new TextPiece { Text = text[pos.Offset..] }]
            };

            para.Children.RemoveAt(pos.InlineIndex);
            para.Children.Insert(pos.InlineIndex, after);
            para.Children.Insert(pos.InlineIndex, breakRun);
            para.Children.Insert(pos.InlineIndex, before);

            // Cursor lands after the break run, at the start of the "after" run
            var newPos = new ModelPosition(pos.BlockIndex, pos.InlineIndex + 2, 0) { Cell = pos.Cell };
            state.Selection = SelectionModel.Collapsed(newPos);
        }
        else
        {
            // Insert break run after the current inline (at offset boundary)
            var insertIdx = pos.Offset == 0 ? pos.InlineIndex : pos.InlineIndex + 1;
            para.Children.Insert(insertIdx, breakRun);

            var newPos = new ModelPosition(pos.BlockIndex, insertIdx + 1, 0) { Cell = pos.Cell };
            state.Selection = SelectionModel.Collapsed(newPos);
        }

        ParagraphNormalizer.Normalize(para);
        return state;
    }
}
