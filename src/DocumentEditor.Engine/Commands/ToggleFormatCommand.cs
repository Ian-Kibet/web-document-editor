using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class ToggleFormatCommand : ICommand
{
    private readonly string _property;

    public ToggleFormatCommand(string property)
    {
        _property = property.ToLowerInvariant();
    }

    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;
        var doc = state.Document;

        if (sel.IsCollapsed)
        {
            var pos = sel.Anchor;
            var para = CommandExecutor.ResolveParagraph(doc, pos);
            if (para is null) return state;
            if (para.Children[pos.InlineIndex] is not Run run) return state;

            var wordBounds = SelectionHelper.FindWordInterior(run.Text, pos.Offset);
            if (wordBounds is null)
            {
                // Not interior to a word — toggle entire run as before
                ToggleProperty(run.Properties);
                return state;
            }

            var (wordStart, wordEnd) = wordBounds.Value;

            // Construct synthetic selection over the word, reuse range-selection path
            var wordStartPos = new ModelPosition(pos.BlockIndex, pos.InlineIndex, wordStart) { Cell = pos.Cell };
            var wordEndPos = new ModelPosition(pos.BlockIndex, pos.InlineIndex, wordEnd) { Cell = pos.Cell };
            var wordRuns = CollectRunsInRange(doc, wordStartPos, wordEndPos);
            if (wordRuns.Count == 0) return state;

            var wordAllOn = wordRuns.All(r => GetProperty(r.Properties));
            foreach (var r in wordRuns)
                SetProperty(r.Properties, !wordAllOn);

            ParagraphNormalizer.Normalize(para);
            return state;
        }

        // Range selection: collect all affected runs
        var (start, end) = SelectionHelper.Normalize(sel);
        var runs = CollectRunsInRange(doc, start, end);

        if (runs.Count == 0) return state;

        // If ALL have property ON → turn OFF, else turn ON
        var allOn = runs.All(r => GetProperty(r.Properties));
        foreach (var run in runs)
        {
            SetProperty(run.Properties, !allOn);
        }

        // Normalize affected paragraphs
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(sel);
        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is Paragraph para)
                ParagraphNormalizer.Normalize(para);
        }

        return state;
    }

    private List<Run> CollectRunsInRange(DocxDocument doc, ModelPosition start, ModelPosition end)
    {
        var runs = new List<Run>();

        for (var b = start.BlockIndex; b <= end.BlockIndex; b++)
        {
            if (doc.Children[b] is not Paragraph para) continue;

            var inlineStart = (b == start.BlockIndex) ? start.InlineIndex : 0;
            var inlineEnd = (b == end.BlockIndex) ? end.InlineIndex : para.Children.Count - 1;

            for (var i = inlineStart; i <= inlineEnd; i++)
            {
                if (para.Children[i] is Run run)
                {
                    var isStartBoundary = b == start.BlockIndex && i == start.InlineIndex && start.Offset > 0;
                    var isEndBoundary = b == end.BlockIndex && i == end.InlineIndex && end.Offset < run.Text.Length;

                    if (isStartBoundary && isEndBoundary)
                    {
                        // Both boundaries in the same run: split at end first, then start
                        SplitRunAt(para, i, end.Offset);
                        var middleRun = SplitRunAt(para, i, start.Offset);
                        if (middleRun is not null) runs.Add(middleRun);
                        // Skip past the 2 new runs (middle + tail); for-loop will i++
                        inlineEnd += 2;
                        i += 2;
                        continue;
                    }

                    if (isStartBoundary)
                    {
                        var splitRun = SplitRunAt(para, i, start.Offset);
                        if (splitRun is not null) runs.Add(splitRun);
                        if (b == end.BlockIndex && end.InlineIndex >= i)
                        {
                            end = new ModelPosition(end.BlockIndex, end.InlineIndex + 1, end.Offset);
                            inlineEnd = end.InlineIndex;
                        }
                        continue;
                    }

                    if (isEndBoundary)
                    {
                        SplitRunAt(para, i, end.Offset);
                        runs.Add(run);
                        continue;
                    }

                    runs.Add(run);
                }
            }
        }

        return runs;
    }

    private static Run? SplitRunAt(Paragraph para, int inlineIndex, int offset)
    {
        var run = (Run)para.Children[inlineIndex];
        if (offset <= 0 || offset >= run.Text.Length) return null;

        var textBefore = run.Text[..offset];
        var textAfter = run.Text[offset..];

        run.Text = textBefore;

        var newRun = DocFactory.CreateRun(textAfter, new RunProperties
        {
            Bold = run.Properties.Bold,
            Italic = run.Properties.Italic,
            Underline = run.Properties.Underline,
            Strikethrough = run.Properties.Strikethrough,
            FontFamily = run.Properties.FontFamily,
            FontSize = run.Properties.FontSize,
            Color = run.Properties.Color,
            Highlight = run.Properties.Highlight,
            VerticalAlign = run.Properties.VerticalAlign,
        });

        para.Children.Insert(inlineIndex + 1, newRun);
        return newRun;
    }

    private void ToggleProperty(RunProperties props)
    {
        SetProperty(props, !GetProperty(props));
    }

    private bool GetProperty(RunProperties props)
    {
        return _property switch
        {
            "bold" => props.Bold,
            "italic" => props.Italic,
            "underline" => props.Underline is not null and not UnderlineType.None,
            "strikethrough" => props.Strikethrough,
            _ => false
        };
    }

    private void SetProperty(RunProperties props, bool value)
    {
        switch (_property)
        {
            case "bold":
                props.Bold = value;
                break;
            case "italic":
                props.Italic = value;
                break;
            case "underline":
                props.Underline = value ? UnderlineType.Single : null;
                break;
            case "strikethrough":
                props.Strikethrough = value;
                break;
        }
    }
}
