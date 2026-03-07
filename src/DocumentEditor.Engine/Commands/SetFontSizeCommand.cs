using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class SetFontSizeCommand : ICommand
{
    private readonly int? _fontSizeHalfPoints;

    public SetFontSizeCommand(int? fontSizeHalfPoints)
    {
        _fontSizeHalfPoints = fontSizeHalfPoints;
    }

    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;
        var doc = state.Document;

        if (sel.IsCollapsed)
        {
            var pos = sel.Anchor;
            var para = CommandExecutor.ResolveParagraph(doc, pos);
            if (para is null || pos.InlineIndex >= para.Children.Count) return state;
            if (para.Children[pos.InlineIndex] is not Run run) return state;
            run.Properties.FontSize = _fontSizeHalfPoints;
            ParagraphNormalizer.Normalize(para);
            return state;
        }

        var (start, end) = SelectionHelper.Normalize(sel);
        var runs = CollectRunsInRange(doc, start, end);
        foreach (var r in runs)
            r.Properties.FontSize = _fontSizeHalfPoints;

        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(sel);
        for (var i = startBlock; i <= endBlock; i++)
        {
            if (doc.Children[i] is Paragraph p)
                ParagraphNormalizer.Normalize(p);
        }

        return state;
    }

    private static List<Run> CollectRunsInRange(DocxDocument doc, ModelPosition start, ModelPosition end)
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
                        SplitRunAt(para, i, end.Offset);
                        var middleRun = SplitRunAt(para, i, start.Offset);
                        if (middleRun is not null) runs.Add(middleRun);
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
}
