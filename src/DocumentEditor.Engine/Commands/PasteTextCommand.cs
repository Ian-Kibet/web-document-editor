namespace DocumentEditor.Engine.Commands;

public class PasteTextCommand : ICommand
{
    private readonly string _plainText;

    public PasteTextCommand(string plainText)
    {
        _plainText = plainText;
    }

    public EditorState Execute(EditorState state)
    {
        // Delete selection if not collapsed
        if (!state.Selection.IsCollapsed)
        {
            state = new DeleteSelectionCommand().Execute(state);
        }

        // Normalize line endings
        var text = _plainText.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = text.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            // Insert text for current line
            if (lines[i].Length > 0)
            {
                state = new InsertTextCommand(lines[i]).Execute(state);
            }

            // Split paragraph between lines (not after last)
            if (i < lines.Length - 1)
            {
                state = new SplitParagraphCommand().Execute(state);
            }
        }

        return state;
    }
}
