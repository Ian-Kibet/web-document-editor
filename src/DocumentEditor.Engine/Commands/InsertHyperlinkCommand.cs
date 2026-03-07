using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class InsertHyperlinkCommand : ICommand
{
    private readonly string _url;
    private readonly string _text;

    public InsertHyperlinkCommand(string url, string text)
    {
        _url = url;
        _text = text;
    }

    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;

        // Delete selection if not collapsed
        if (!sel.IsCollapsed)
        {
            state = new DeleteSelectionCommand().Execute(state);
            sel = state.Selection;
        }

        var doc = state.Document;
        var pos = sel.Anchor;

        if (doc.Children[pos.BlockIndex] is not Paragraph para) return state;
        if (para.Children[pos.InlineIndex] is not Run currentRun) return state;

        // Split current run at cursor
        var textBefore = currentRun.Text[..pos.Offset];
        var textAfter = currentRun.Text[pos.Offset..];

        currentRun.Text = textBefore;

        // Create hyperlink
        var hyperlink = DocFactory.CreateHyperlink(_url, _text);

        // Create run after hyperlink for cursor placement
        var afterRun = DocFactory.CreateRun(textAfter, new Model.Properties.RunProperties
        {
            Bold = currentRun.Properties.Bold,
            Italic = currentRun.Properties.Italic,
            Underline = currentRun.Properties.Underline,
            Strikethrough = currentRun.Properties.Strikethrough,
            FontFamily = currentRun.Properties.FontFamily,
            FontSize = currentRun.Properties.FontSize,
            Color = currentRun.Properties.Color,
            Highlight = currentRun.Properties.Highlight,
            VerticalAlign = currentRun.Properties.VerticalAlign,
        });

        // Insert hyperlink and after-run at correct position
        var insertIdx = pos.InlineIndex + 1;
        para.Children.Insert(insertIdx, hyperlink);
        para.Children.Insert(insertIdx + 1, afterRun);

        ParagraphNormalizer.Normalize(para);

        // Position cursor at start of run after hyperlink
        // Find the afterRun's index after normalization
        var afterRunIdx = para.Children.IndexOf(afterRun);
        if (afterRunIdx < 0)
        {
            // afterRun may have been merged — find by ID or position after hyperlink
            for (var i = 0; i < para.Children.Count; i++)
            {
                if (para.Children[i] is Hyperlink)
                {
                    // Cursor goes to next run after the hyperlink
                    if (i + 1 < para.Children.Count && para.Children[i + 1] is Run)
                    {
                        afterRunIdx = i + 1;
                        break;
                    }
                }
            }
        }

        if (afterRunIdx >= 0)
        {
            state.Selection = SelectionModel.Collapsed(pos.BlockIndex, afterRunIdx, 0);
        }
        else
        {
            state.Selection = SelectionModel.Collapsed(pos.BlockIndex, para.Children.Count - 1, 0);
        }

        return state;
    }
}
