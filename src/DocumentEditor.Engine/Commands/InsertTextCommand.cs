using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class InsertTextCommand : ICommand
{
    private readonly string _text;

    public InsertTextCommand(string text)
    {
        _text = text;
    }

    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;

        // Delete selection first if not collapsed
        if (!sel.IsCollapsed)
        {
            state = new DeleteSelectionCommand().Execute(state);
            sel = state.Selection;
        }

        var doc = state.Document;
        var pos = sel.Anchor;

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is null) return state;
        if (pos.InlineIndex < 0 || pos.InlineIndex >= para.Children.Count)
            return state;
        if (para.Children[pos.InlineIndex] is not Run run)
            return state;

        // Insert text at offset
        var text = run.Text;
        run.Text = text[..pos.Offset] + _text + text[pos.Offset..];

        // Calculate new absolute text offset before normalization
        var absoluteOffset = 0;
        for (var i = 0; i < pos.InlineIndex; i++)
        {
            if (para.Children[i] is Run r) absoluteOffset += r.Text.Length;
        }
        absoluteOffset += pos.Offset + _text.Length;

        ParagraphNormalizer.Normalize(para);

        // Recalculate position after normalization
        var accumulated = 0;
        for (var i = 0; i < para.Children.Count; i++)
        {
            if (para.Children[i] is not Run r) continue;
            var len = r.Text.Length;
            if (accumulated + len >= absoluteOffset)
            {
                pos.InlineIndex = i;
                pos.Offset = absoluteOffset - accumulated;
                break;
            }
            accumulated += len;
        }

        state.Selection = SelectionModel.Collapsed(pos);
        return state;
    }
}
