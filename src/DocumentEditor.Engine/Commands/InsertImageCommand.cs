using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class InsertImageCommand : ICommand
{
    private readonly ImageContent _image;

    public InsertImageCommand(ImageContent image)
    {
        _image = image;
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

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is null) return state;
        if (pos.InlineIndex < 0 || pos.InlineIndex >= para.Children.Count) return state;
        if (para.Children[pos.InlineIndex] is not Run currentRun) return state;

        // Split current run at cursor
        var textBefore = currentRun.Text[..pos.Offset];
        var textAfter = currentRun.Text[pos.Offset..];
        currentRun.Text = textBefore;

        // Create image run
        var imageRun = new Run
        {
            Properties = new RunProperties(),
            Content = [_image]
        };

        // Create run after image for cursor placement
        var afterRun = DocFactory.CreateRun(textAfter, new RunProperties
        {
            Bold          = currentRun.Properties.Bold,
            Italic        = currentRun.Properties.Italic,
            Underline     = currentRun.Properties.Underline,
            Strikethrough = currentRun.Properties.Strikethrough,
            FontFamily    = currentRun.Properties.FontFamily,
            FontSize      = currentRun.Properties.FontSize,
            Color         = currentRun.Properties.Color,
            Highlight     = currentRun.Properties.Highlight,
            VerticalAlign = currentRun.Properties.VerticalAlign,
        });

        var insertIdx = pos.InlineIndex + 1;
        para.Children.Insert(insertIdx, imageRun);
        para.Children.Insert(insertIdx + 1, afterRun);

        ParagraphNormalizer.Normalize(para);

        // Position cursor at start of run after image
        var afterRunIdx = para.Children.IndexOf(afterRun);
        if (afterRunIdx < 0) afterRunIdx = para.Children.Count - 1;

        state.Selection = SelectionModel.Collapsed(pos.BlockIndex, afterRunIdx, 0);
        return state;
    }
}
