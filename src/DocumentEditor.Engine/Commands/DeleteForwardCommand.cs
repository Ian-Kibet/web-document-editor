using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class DeleteForwardCommand : ICommand
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
        if (para.Children[pos.InlineIndex] is not Run currentRun)
            return state;

        if (pos.Offset < currentRun.Text.Length)
        {
            // Delete char after cursor within current run
            var text = currentRun.Text;
            currentRun.Text = text[..pos.Offset] + text[(pos.Offset + 1)..];

            var absOffset = GetAbsoluteOffset(para, pos);
            ParagraphNormalizer.Normalize(para);
            RecalcPositionByAbsoluteOffset(para, pos, absOffset);
        }
        else if (pos.InlineIndex < para.Children.Count - 1)
        {
            // At end of run — delete first char of next run
            var nextIdx = pos.InlineIndex + 1;
            while (nextIdx < para.Children.Count && para.Children[nextIdx] is not Run)
                nextIdx++;

            if (nextIdx < para.Children.Count && para.Children[nextIdx] is Run nextRun && nextRun.Text.Length > 0)
            {
                nextRun.Text = nextRun.Text[1..];

                var absOffset = GetAbsoluteOffset(para, pos);
                ParagraphNormalizer.Normalize(para);
                RecalcPositionByAbsoluteOffset(para, pos, absOffset);
            }
        }
        else if (pos.Cell is not null)
        {
            // Inside a table cell: merge next cell paragraph if any
            var childList = CommandExecutor.ResolveChildList(doc, pos);
            var nextCellBlockIdx = pos.Cell.CellBlockIndex + 1;
            if (nextCellBlockIdx >= childList.Count) return state; // last para in cell — no-op
            if (childList[nextCellBlockIdx] is not Paragraph nextPara) return state;

            var absOffset = GetAbsoluteOffset(para, pos);
            foreach (var inline in nextPara.Children) para.Children.Add(inline);
            childList.RemoveAt(nextCellBlockIdx);
            ParagraphNormalizer.Normalize(para);
            RecalcPositionByAbsoluteOffset(para, pos, absOffset);
        }
        else if (pos.Cell is null && pos.BlockIndex < doc.Children.Count - 1)
        {
            // At end of top-level paragraph — merge next paragraph into current
            var nextBlockIdx = pos.BlockIndex + 1;

            if (doc.Children[nextBlockIdx] is not Paragraph nextPara)
                return state; // Next is Table — no-op

            var absOffset = GetAbsoluteOffset(para, pos);

            // Append next para's inlines to current para
            foreach (var inline in nextPara.Children)
            {
                para.Children.Add(inline);
            }

            // Remove next paragraph
            doc.Children.RemoveAt(nextBlockIdx);

            ParagraphNormalizer.Normalize(para);
            RecalcPositionByAbsoluteOffset(para, pos, absOffset);
        }
        // else: at end of document or last para in cell — no-op

        state.Selection = SelectionModel.Collapsed(pos);
        return state;
    }

    private static int GetAbsoluteOffset(Paragraph para, ModelPosition pos)
    {
        var absOffset = 0;
        for (var i = 0; i < pos.InlineIndex; i++)
        {
            if (para.Children[i] is Run r) absOffset += r.Text.Length;
        }
        absOffset += pos.Offset;
        return absOffset;
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
