using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class DeleteBackwardCommand : ICommand
{
    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;

        // Range selection → delete selection
        if (!sel.IsCollapsed)
        {
            return new DeleteSelectionCommand().Execute(state);
        }

        var doc = state.Document;
        var pos = sel.Anchor;

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is null) return state;

        if (pos.Offset > 0)
        {
            // Delete char before cursor within current run
            var run = (Run)para.Children[pos.InlineIndex];
            var text = run.Text;
            run.Text = text[..(pos.Offset - 1)] + text[pos.Offset..];
            pos.Offset--;
            ParagraphNormalizer.Normalize(para);
            RecalcPosition(para, pos, pos.InlineIndex);
        }
        else if (pos.InlineIndex > 0)
        {
            // At start of run but not first run — move to end of previous run, delete last char
            var prevIdx = pos.InlineIndex - 1;
            // Skip hyperlinks when looking for previous run
            while (prevIdx >= 0 && para.Children[prevIdx] is not Run)
                prevIdx--;

            if (prevIdx >= 0 && para.Children[prevIdx] is Run prevRun && prevRun.Text.Length > 0)
            {
                prevRun.Text = prevRun.Text[..^1];

                // Calculate new absolute offset
                var absOffset = 0;
                for (var i = 0; i <= prevIdx; i++)
                {
                    if (para.Children[i] is Run r) absOffset += r.Text.Length;
                }

                ParagraphNormalizer.Normalize(para);
                RecalcPositionByAbsoluteOffset(para, pos, absOffset);
            }
            else if (prevIdx >= 0 && para.Children[prevIdx] is Run imgRun
                && imgRun.Content.OfType<ImageContent>().Any())
            {
                // Image-only run: remove the whole run; cursor lands where the image was
                var absOffset = 0;
                for (var i = 0; i < prevIdx; i++)
                {
                    if (para.Children[i] is Run r) absOffset += r.Text.Length;
                }
                para.Children.RemoveAt(prevIdx);
                ParagraphNormalizer.Normalize(para);
                RecalcPositionByAbsoluteOffset(para, pos, absOffset);
            }
        }
        else if (pos.Cell is not null && pos.Cell.CellBlockIndex > 0)
        {
            // Inside a table cell: merge with previous cell paragraph
            var childList = CommandExecutor.ResolveChildList(doc, pos);
            var prevCellBlockIdx = pos.Cell.CellBlockIndex - 1;
            if (childList[prevCellBlockIdx] is not Paragraph prevPara) return state;

            prevPara.Properties.SectionBreak = null;
            var cursorAbsOffset = 0;
            foreach (var inline in prevPara.Children)
                if (inline is Run r) cursorAbsOffset += r.Text.Length;

            foreach (var inline in para.Children) prevPara.Children.Add(inline);
            childList.RemoveAt(pos.Cell.CellBlockIndex);
            ParagraphNormalizer.Normalize(prevPara);

            pos.Cell = new CellPath { RowIndex = pos.Cell.RowIndex, CellIndex = pos.Cell.CellIndex, CellBlockIndex = prevCellBlockIdx };
            RecalcPositionByAbsoluteOffset(prevPara, pos, cursorAbsOffset);
        }
        else if (pos.Cell is null && pos.BlockIndex > 0)
        {
            // At start of top-level paragraph — merge with previous paragraph
            var prevBlockIdx = pos.BlockIndex - 1;

            if (doc.Children[prevBlockIdx] is not Paragraph prevPara)
                return state; // Previous is Table — no-op

            // Clear section break on previous paragraph if present
            // (section boundary dissolves when paragraphs merge)
            prevPara.Properties.SectionBreak = null;

            // Record where cursor should go: end of previous para content
            var prevInlineCount = prevPara.Children.Count;
            var cursorAbsOffset = 0;
            foreach (var inline in prevPara.Children)
            {
                if (inline is Run r) cursorAbsOffset += r.Text.Length;
            }

            // Append current para's inlines to previous para
            foreach (var inline in para.Children)
            {
                prevPara.Children.Add(inline);
            }

            // Remove current paragraph
            doc.Children.RemoveAt(pos.BlockIndex);

            ParagraphNormalizer.Normalize(prevPara);

            // Reposition cursor
            pos.BlockIndex = prevBlockIdx;
            RecalcPositionByAbsoluteOffset(prevPara, pos, cursorAbsOffset);
        }
        // else: at start of document or start of first cell paragraph — no-op

        state.Selection = SelectionModel.Collapsed(pos);
        return state;
    }

    private static void RecalcPosition(Paragraph para, ModelPosition pos, int preferredInline)
    {
        if (preferredInline < para.Children.Count && para.Children[preferredInline] is Run)
            return; // Position is still valid

        // Find the run that contains our position
        RecalcPositionByAbsoluteOffset(para, pos, 0);
    }

    private static void RecalcPositionByAbsoluteOffset(Paragraph para, ModelPosition pos, int absOffset)
    {
        var accumulated = 0;
        for (var i = 0; i < para.Children.Count; i++)
        {
            if (para.Children[i] is not Run run) continue;
            var len = run.Text.Length;
            if (accumulated + len >= absOffset)
            {
                pos.InlineIndex = i;
                pos.Offset = absOffset - accumulated;
                return;
            }
            accumulated += len;
        }

        // Fallback: end of last run
        for (var i = para.Children.Count - 1; i >= 0; i--)
        {
            if (para.Children[i] is Run run)
            {
                pos.InlineIndex = i;
                pos.Offset = run.Text.Length;
                return;
            }
        }

        pos.InlineIndex = 0;
        pos.Offset = 0;
    }
}
