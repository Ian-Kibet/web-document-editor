using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class DeleteSelectionCommand : ICommand
{
    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;
        if (sel.IsCollapsed) return state;

        var doc = state.Document;
        var (start, end) = SelectionHelper.Normalize(sel);

        if (start.BlockIndex == end.BlockIndex)
        {
            DeleteWithinParagraph(doc, start, end);
        }
        else
        {
            DeleteAcrossParagraphs(doc, start, end);
        }

        state.Selection = SelectionModel.Collapsed(start.Clone());
        return state;
    }

    private static void DeleteWithinParagraph(DocxDocument doc, ModelPosition start, ModelPosition end)
    {
        var para = (Paragraph)doc.Children[start.BlockIndex];

        if (start.InlineIndex == end.InlineIndex)
        {
            // Same run — just slice text
            var run = (Run)para.Children[start.InlineIndex];
            var text = run.Text;
            run.Text = text[..start.Offset] + text[end.Offset..];
        }
        else
        {
            // Trim start run
            var startRun = (Run)para.Children[start.InlineIndex];
            startRun.Text = startRun.Text[..start.Offset];

            // Trim end run
            var endRun = (Run)para.Children[end.InlineIndex];
            endRun.Text = endRun.Text[end.Offset..];

            // Remove intermediate inlines
            for (var i = end.InlineIndex - 1; i > start.InlineIndex; i--)
            {
                para.Children.RemoveAt(i);
            }
        }

        ParagraphNormalizer.Normalize(para);

        // Reposition cursor after normalization
        RepositionAfterNormalize(para, start);
    }

    private static void DeleteAcrossParagraphs(DocxDocument doc, ModelPosition start, ModelPosition end)
    {
        var startPara = (Paragraph)doc.Children[start.BlockIndex];
        var endPara = (Paragraph)doc.Children[end.BlockIndex];

        // Trim start paragraph: keep everything before cursor
        var startRun = (Run)startPara.Children[start.InlineIndex];
        startRun.Text = startRun.Text[..start.Offset];
        // Remove inlines after start position in start paragraph
        while (startPara.Children.Count > start.InlineIndex + 1)
        {
            startPara.Children.RemoveAt(startPara.Children.Count - 1);
        }

        // Trim end paragraph: keep everything after cursor
        var endRun = (Run)endPara.Children[end.InlineIndex];
        endRun.Text = endRun.Text[end.Offset..];
        // Remove inlines before end position in end paragraph
        for (var i = end.InlineIndex - 1; i >= 0; i--)
        {
            endPara.Children.RemoveAt(i);
        }

        // Merge remaining end para inlines into start para
        foreach (var inline in endPara.Children)
        {
            startPara.Children.Add(inline);
        }

        // Remove intermediate blocks and end paragraph
        for (var i = end.BlockIndex; i > start.BlockIndex; i--)
        {
            doc.Children.RemoveAt(i);
        }

        ParagraphNormalizer.Normalize(startPara);

        // Reposition cursor after normalization
        RepositionAfterNormalize(startPara, start);
    }

    private static void RepositionAfterNormalize(Paragraph para, ModelPosition pos)
    {
        // After normalization, runs may have merged. Find correct position by text offset.
        // The cursor stays at whatever the start position was pointing to.
        // After normalize, recalculate inline index and offset.
        var accumulated = 0;
        for (var i = 0; i < para.Children.Count; i++)
        {
            if (para.Children[i] is not Run run) continue;
            var len = run.Text.Length;
            if (i >= pos.InlineIndex || accumulated + len >= 0)
            {
                // Find the cursor: the text at start was trimmed, so cursor should be
                // at the boundary we created
                pos.InlineIndex = i;
                pos.Offset = Math.Min(pos.Offset, len);
                return;
            }
            accumulated += len;
        }

        // Fallback: put cursor at start of first run
        pos.InlineIndex = 0;
        pos.Offset = 0;
    }
}
